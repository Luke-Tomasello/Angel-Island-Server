/***************************************************************************
 *
 *                   The Angel Island Music System (AIMS)
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* Scripts\Engines\AIMusicSystem\MusicBox\Gumps\MusicBox.LibraryGump.cs
 * Changelog
 *  11/19/26, Adam (public music boxes)
 *      A Public box's library is open (Look At) without a house: it can't be locked down outside one.
 *  11/15/26, Adam (the library)
 *      Created. "list" was made for a library of ten songs; with 130 it scrolls off the screen. The library window
 *      shows every approved song, 15 to a page: tabs All, Mine (bought, composed by you, free, or Napster you have),
 *      Free (price 0, and Napster), Premium (priced) and New (exactly what 'new' lists: NewReleases); a list of the
 *      authors on the left filters by one. Each row: Play (in full when you may, else a sample: PlayTrack, as
 *      "play <song>"), the title and author (arranged by), your status (Owned, Free, Napster, or the
 *      price) and Buy on a premium song you don't have (BuyTrack, as "buy <song>": the price prompt). Stop stops this
 *      box's song.
 *      Anyone near the box may look (the context menu's Look At, or "<box> library"/"browse": speech is answered for
 *      anyone for this one command). Play, Buy and Stop need what speaking to the box needs (CheckRights: house
 *      access), checked again when pressed, within 5 tiles; without access the window says so instead of the buttons.
 *      The buttons go through the box's own code: no rule here that the voice commands don't have.
 *      Made smaller after a first look (Adam): no instrument column, 11 songs a page (was 15), tighter rows; the
 *      authors (at most 14) use small arrows. 620 x 430 (was 700 x 520).
 *      Only where the box works (LibraryOpen: locked down in the world, or a minstrel's), as the other menu entries: a
 *      box in a backpack offered Look At and opened the window.
 *      A purchase redraws the buyer's open window where they were (RefreshLibrary, from the buy prompt): the song shows
 *      Owned at once (it waited for the next button).
 */

using Server.Gumps;
using Server.Misc;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Items.MusicLibrary;

namespace Server.Items
{
    public partial class MusicBox
    {
        /// <summary>Opens the library window for anyone near the box (looking is free; its buttons check access).</summary>
        /// <summary>Where the library may be looked at: where the box works (placed and locked down, or a minstrel's, carried),
        ///  as CheckRights' placement rule, without its house access (anyone may look).</summary>
        // a public box (in the conservatory, say) is open without a house: it can't be locked down outside one
        public bool LibraryOpen => Placement == BoxPlacement.Carried || (Placement == BoxPlacement.World && (IsLockedDown || Public));

        public void OpenLibrary(Mobile from)
        {
            if (from == null || from.Deleted || Deleted)
                return;
            if (!LibraryOpen)
            {
                from.SendLocalizedMessage(502692); // This must be in a house and be locked down to work.
                return;
            }
            if (!InSpeechRange(from))
            {
                SendMessage(from, "You are too far away from the music box.");
                return;
            }
            from.CloseGump(typeof(LibraryGump));
            from.SendGump(new LibraryGump(this, from, LibraryTab.All, null, 0));
        }

        /// <summary>Redraws this box's library window for the player, if it is open, where they were (tab, author, page): a
        ///  purchase (the buy prompt) shows the song Owned at once.</summary>
        public void RefreshLibrary(Mobile m)
        {
            if (m == null || m.Deleted || m.NetState == null || Deleted)
                return;
            LibraryGump? open = m.NetState.Gumps.OfType<LibraryGump>().LastOrDefault(g => g.Box == this);
            if (open == null)
                return;
            m.CloseGump(typeof(LibraryGump));
            m.SendGump(new LibraryGump(this, m, open.Tab, open.Author, open.Page));
        }

        public enum LibraryTab { All, Mine, Free, Premium, New }

        // a song's row in the library, for one player
        private sealed class LibraryRow
        {
            public RolledUpSheetMusic Track = null!;
            public MusicInfo Info = null!;
            public string Author = "";
            public bool Owned, Napster, NapsterAccess;
            public int Price;
            public bool Full => Owned || (Napster ? NapsterAccess : Price == 0);     // plays in full for this player
        }

        // what the player has, for the window (not the box's access rules: a visitor sees what they own too)
        private static bool OwnsTrack(Mobile m, RolledUpSheetMusic rsm, MusicInfo info) =>
            IsComposer(m, rsm, info) || info.Purchasers.Any(p => p.Purchaser == m);

        // the approved songs on a tab, by title (New: newest first), as rows for this player
        private List<LibraryRow> LibraryRows(Mobile m, LibraryTab tab)
        {
            IEnumerable<(RolledUpSheetMusic Track, MusicInfo Info)> songs = tab == LibraryTab.New
                ? NewReleases(out _, out int show).Take(show).Select(r => (r.Track, r.Info))
                : MusicLibrary.Repository.Where(kvp => kvp.Key != null && !kvp.Key.Deleted && kvp.Value != null && Approved(kvp.Key))
                    .Select(kvp => (kvp.Key, kvp.Value))
                    .OrderBy(s => s.Key.Title ?? "", StringComparer.OrdinalIgnoreCase);

            List<LibraryRow> rows = new List<LibraryRow>();
            foreach (var (track, info) in songs)
            {
                bool napster = info.Napster || track.Napster;
                bool owned = OwnsTrack(m, track, info) || m.AccessLevel >= AccessLevel.GameMaster;
                LibraryRow row = new LibraryRow
                {
                    Track = track,
                    Info = info,
                    Author = (track.Author ?? "").Trim(),
                    Owned = owned,
                    Napster = napster,
                    NapsterAccess = napster && owned,
                    Price = napster ? 0 : info.Price,
                };
                bool keep = tab switch
                {
                    LibraryTab.Mine => row.Full,
                    LibraryTab.Free => napster || info.Price == 0,
                    LibraryTab.Premium => !napster && info.Price > 0,
                    _ => true,
                };
                if (keep)
                    rows.Add(row);
            }
            return rows;
        }

        // a button pressed in the window: what speaking to the box needs (near it, CheckRights: house access), then the
        //  same work the voice command does
        private bool LibraryAction(Mobile m, bool startsPlayback)
        {
            if (Deleted || m == null || m.Deleted)
                return false;
            if (m.Map != Map && Placement == BoxPlacement.World || !InSpeechRange(m))
            {
                SendMessage(m, "You are too far away from the music box.");
                return false;
            }
            if (CheckRights(m, message: true) != Rights.okay)
                return false;

            MusicLibrary.ComposerVisit(m);
            RefreshPlayContext(m);
            if (m_playContext == null)
                return false;
            if (GlobalRepositoryChanged())
                m_playContext.RefreshPlayQueue();

            if (startsPlayback && PlayingElsewhere(m))
            {
                SendMessage(m, "You have initiated music in another context. You will need to cancel that or wait for it to finish.");
                return false;
            }
            return true;
        }

        private sealed class LibraryGump : Gump
        {
            public const int RowsPerPage = 11;
            private const int Width = 620, Height = 430, Left = 170, RowHeight = 22, AuthorHeight = 17, MaxAuthors = 14, Top = 96;
            private const int HueText = 1152, HueMuted = 946, HueActive = 53, HueOwned = 68, HuePrice = 1161, HueNapster = 88;

            // button IDs
            private const int ButtonTab = 10, ButtonPrev = 20, ButtonNext = 21, ButtonStop = 30, ButtonAuthor = 100,
                ButtonPlay = 1000, ButtonBuy = 3000;

            private readonly MusicBox m_Box;
            private readonly LibraryTab m_Tab;
            private readonly string? m_Author;
            private readonly int m_Page;
            private readonly List<LibraryRow> m_Shown;          // this page's rows, in order
            private readonly List<string> m_Authors;            // the left list, in order

            public MusicBox Box => m_Box;
            public LibraryTab Tab => m_Tab;
            public string? Author => m_Author;
            public int Page => m_Page;
            public int PageCount { get; }
            public int Total { get; }
            public IReadOnlyList<RolledUpSheetMusic> Shown => m_Shown.Select(r => r.Track).ToList();
            public bool CanUse { get; }

            public LibraryGump(MusicBox box, Mobile m, LibraryTab tab, string? author, int page)
                : base(60, 40)
            {
                m_Box = box;
                m_Tab = tab;
                m_Author = author;

                List<LibraryRow> all = box.LibraryRows(m, tab);
                m_Authors = all.GroupBy(r => r.Author, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.Key).Take(MaxAuthors).ToList();
                if (author != null && !m_Authors.Contains(author, StringComparer.OrdinalIgnoreCase))
                    m_Authors.Add(author);
                List<LibraryRow> rows = author == null ? all : all.Where(r => string.Equals(r.Author, author, StringComparison.OrdinalIgnoreCase)).ToList();

                Total = rows.Count;
                PageCount = Math.Max(1, (rows.Count + RowsPerPage - 1) / RowsPerPage);
                m_Page = Math.Clamp(page, 0, PageCount - 1);
                m_Shown = rows.Skip(m_Page * RowsPerPage).Take(RowsPerPage).ToList();
                CanUse = box.CheckRights(m, message: false) == Rights.okay;

                AddPage(0);
                AddBackground(0, 0, Width, Height, 9270);
                AddAlphaRegion(10, 10, Width - 20, Height - 20);
                AddLabel(20, 16, HueActive, string.Format("{0}: the music library", box.Name ?? "Music box"));

                // the tabs, and Stop while this box plays for you
                string[] names = { "All", "Mine", "Free", "Premium", "New" };
                for (int i = 0; i < names.Length; i++)
                {
                    int x = 20 + i * 100;
                    AddButton(x, 44, 4005, 4007, ButtonTab + i, GumpButtonType.Reply, 0);
                    AddLabel(x + 35, 45, (int)tab == i ? HueActive : HueText, names[i]);
                }
                if (CanUse && box.IsThisDevicePlaying() && box.m_playContext?.Mobile == m)
                {
                    AddButton(Width - 110, 44, 4017, 4019, ButtonStop, GumpButtonType.Reply, 0);
                    AddLabel(Width - 75, 45, HueText, "Stop");
                }

                // the authors
                AddLabel(20, Top - 22, HueMuted, "Authors");
                AddButton(22, Top + 4, 0x15E1, 0x15E5, ButtonAuthor, GumpButtonType.Reply, 0);
                AddLabel(40, Top, author == null ? HueActive : HueText, string.Format("All ({0})", all.Count));
                for (int i = 0; i < m_Authors.Count; i++)
                {
                    int y = Top + (i + 1) * AuthorHeight;
                    int count = all.Count(r => string.Equals(r.Author, m_Authors[i], StringComparison.OrdinalIgnoreCase));
                    AddButton(22, y + 4, 0x15E1, 0x15E5, ButtonAuthor + 1 + i, GumpButtonType.Reply, 0);
                    AddLabelCropped(40, y, Left - 50, 18, string.Equals(author, m_Authors[i], StringComparison.OrdinalIgnoreCase) ? HueActive : HueText,
                        string.Format("{0} ({1})", m_Authors[i].Length > 0 ? m_Authors[i] : "(no author)", count));
                }

                // the songs
                AddLabel(Left, Top - 22, HueMuted, "Song");
                AddLabel(Left + 300, Top - 22, HueMuted, "Yours");
                if (m_Shown.Count == 0)
                    AddLabel(Left, Top, HueMuted, "No songs here.");
                for (int i = 0; i < m_Shown.Count; i++)
                {
                    LibraryRow r = m_Shown[i];
                    int y = Top + i * RowHeight;
                    if (CanUse)
                        AddButton(Left, y, 4005, 4007, ButtonPlay + i, GumpButtonType.Reply, 0);
                    string arranger = "";
                    try { arranger = AimsHost.Current.Arranger(r.Track, r.Info) ?? ""; }
                    catch (Exception ex) { AimsLog.LogException(ex); }
                    string by = r.Author.Length == 0 ? "" : " by " + r.Author + (arranger.Length > 0 ? ", arr. " + arranger : "");
                    AddLabelCropped(Left + 35, y + 1, 260, 20, HueText, (r.Track.Title ?? "") + by);
                    if (r.Owned)
                        AddLabel(Left + 300, y + 1, HueOwned, "Owned");
                    else if (r.Napster)
                        AddLabel(Left + 300, y + 1, HueNapster, "Napster");
                    else if (r.Price == 0)
                        AddLabel(Left + 300, y + 1, HueOwned, "Free");
                    else
                    {
                        AddLabel(Left + 300, y + 1, HuePrice, string.Format("{0:N0} gold", r.Price));
                        if (CanUse)
                        {
                            AddButton(Left + 370, y, 4011, 4013, ButtonBuy + i, GumpButtonType.Reply, 0);
                            AddLabel(Left + 405, y + 1, HueText, "Buy");
                        }
                    }
                }

                // the pages, and how to use it
                int bottom = Height - 38;
                if (m_Page > 0)
                    AddButton(Left, bottom, 4014, 4016, ButtonPrev, GumpButtonType.Reply, 0);
                if (m_Page < PageCount - 1)
                    AddButton(Left + 200, bottom, 4005, 4007, ButtonNext, GumpButtonType.Reply, 0);
                AddLabel(Left + 40, bottom + 1, HueMuted, Total == 0 ? "Page 1 of 1"
                    : string.Format("{0}-{1} of {2}  (page {3} of {4})", m_Page * RowsPerPage + 1, m_Page * RowsPerPage + m_Shown.Count, Total, m_Page + 1, PageCount));
                AddLabelCropped(20, bottom - 26, Width - 40, 20, HueMuted, CanUse
                    ? "Play: in full if it's yours or free, else a sample.  Buy: you're asked to confirm the price."
                    : "You may look. Playing or buying needs access to this house.");
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile m = sender.Mobile;
                if (m == null || m_Box.Deleted)
                    return;
                Respond(m, info.ButtonID);
            }

            /// <summary>The window's answer to a button (harnesses call it directly); the window is sent again unless closed.</summary>
            public void Respond(Mobile m, int button)
            {
                if (button <= 0)
                    return;     // closed

                LibraryTab tab = m_Tab;
                string? author = m_Author;
                int page = m_Page;

                if (button >= ButtonTab && button < ButtonTab + 5)
                {
                    tab = (LibraryTab)(button - ButtonTab);
                    author = null;
                    page = 0;
                }
                else if (button == ButtonPrev)
                    page--;
                else if (button == ButtonNext)
                    page++;
                else if (button == ButtonAuthor)
                {
                    author = null;
                    page = 0;
                }
                else if (button > ButtonAuthor && button <= ButtonAuthor + m_Authors.Count)
                {
                    author = m_Authors[button - ButtonAuthor - 1];
                    page = 0;
                }
                else if (button == ButtonStop)
                {
                    if (m_Box.LibraryAction(m, startsPlayback: false) && m_Box.IsThisDevicePlaying())
                        m_Box.OnDoubleClick(m);     // as "stop"
                }
                else if (button >= ButtonPlay && button < ButtonPlay + m_Shown.Count)
                {
                    RolledUpSheetMusic track = m_Shown[button - ButtonPlay].Track;
                    if (!track.Deleted && m_Box.LibraryAction(m, startsPlayback: true))
                    {
                        m_Box.m_playContext.AutoPlay = false;           // as "play <song>"
                        m_Box.StopForNewSong(preservePlaylist: false);
                        m_Box.PlayTrack(m, track);
                    }
                }
                else if (button >= ButtonBuy && button < ButtonBuy + m_Shown.Count)
                {
                    LibraryRow r = m_Shown[button - ButtonBuy];
                    if (!r.Track.Deleted && m_Box.LibraryAction(m, startsPlayback: false))
                        m_Box.BuyTrack(m, r.Track, r.Info);
                }
                else
                    return;

                if (m_Box.Deleted || m.Deleted || m.NetState == null)
                    return;
                m.CloseGump(typeof(LibraryGump));
                m.SendGump(new LibraryGump(m_Box, m, tab, author, page));
            }
        }

        /// <summary>For harnesses: the window a player would see (not sent).</summary>
        public Gump LibraryWindow(Mobile m, LibraryTab tab, string? author = null, int page = 0) => new LibraryGump(this, m, tab, author, page);

        /// <summary>For harnesses: press a button on a window made by LibraryWindow.</summary>
        public static void LibraryPress(Gump window, Mobile m, int button) => (window as LibraryGump)?.Respond(m, button);
    }
}
