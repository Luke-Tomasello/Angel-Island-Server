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

/* Scripts/Engines/AIMusicSystem/RSM/RolledUpSheetMusic.cs
 * Changelog
 *  11/8/26, Adam (2026 modernization, 10/25/26 to 11/8/26)
 *      Laid out for reading. [Dupe copies the whole sheet (it made a blank one) and the copy plays itself; a sheet
 *      without a composition plays nothing (HasComposition). GenHashCode no longer leaves a stray ManuscriptPaper in
 *      the world; CompareTo compares hashes (it could overflow). Serialize writes version 4 plainly (the same bytes).
 *      Portable AIMS: logging, the stable hash, Shuffle, LocalTimer and the Data folder go through AimsLog and AimsUtil
 *      (Host\), so this folder drops into other RunUO servers.
 *      AimsHost: account GUIDs and copies (CopySheet makes the copy). Angel Island only (#if ANGEL_ISLAND): Dupe and
 *      CanDelete; elsewhere the sheet keeps its own OwnerGUID (saved after the composition).
 *      Keeps its author (KeepsItsAuthor): a '# Author:' line in its notes no longer replaces it as it plays.
 *  4/24/2024, Adam (TrackID)
 *      Add a trackID for identifying a RSM by title, and author/owner (differs from HashCode which is a hash of the music.)
 *  2/26/22, Adam (ConcertMode)
 *      Add support for ConcertMode
 *  2/18/22, Adam (Napster)
 *      Add the notion of a 'Napster' model for music sharing
 *  2/4/22, Adam
 *      Set the Instrument to 'this' during construction and when playing a file
 *  1/26/22, Adam
 *      Update the hash system to use a persistent and deterministic hash.
 *  1/25/22, Adam
 *      Add the mobile/price to the persistent elements of the RolledUpSheetMusic. This is used to pay the owner when his music is purchased.
 *  1/20/22, Adam
 *      Add IComparable/IEquatable
 *      Set configuration parameters on creation instead of on use.
 *  1/16/22, Adam (Dupe)
 *      Added public properties for Author, Title, and Composition so that completed compositions can be duped.
 *  1/12/22, Adam
 *		Initial creation.
 */
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Misc
{
    /// <summary>
    /// A player's composition: the song (Composition) with its title, author, owner and price. It plays itself (a
    /// RazorInstrument whose Instrument is itself), and its identity is the hash of its music (HashCode): the music
    /// library is keyed by it, and two sheets with the same song are the same track.
    /// </summary>
    public class RolledUpSheetMusic : RazorInstrument, IComparable<RolledUpSheetMusic>, IEquatable<RolledUpSheetMusic>
    {
        private static readonly int[] graphicIDs =
        {
            // 8th
            0x1F65, 0x1F66, 0x1F67, 0x1F68, 0x1F69, 0x1F6A, 0x1F6B, 0x1F6C,
            // 5th
            0x1F4D, 0x1F4E, 0x1F4F, 0x1F50, 0x1F51, 0x1F52, 0x1F53, 0x1F54,
            // 1st
            0x1F2E, 0x1F2F, 0x1F30, 0x1F31, 0x1F32, 0x1F33, 0x1F2D, 0x1F34,
            // 4th
            0x1F45, 0x1F46, 0x1F47, 0x1F48, 0x1F49, 0x1F4A, 0x1F4B, 0x1F4C,
            // 2nd
            0x1F35, 0x1F36, 0x1F37, 0x1F38, 0x1F39, 0x1F3A, 0x1F3B, 0x1F3C,
            // 7th
            0x1F5D, 0x1F5E, 0x1F5F, 0x1F60, 0x1F61, 0x1F62, 0x1F63, 0x1F64,
            // 6th
            0x1F55, 0x1F56, 0x1F57, 0x1F58, 0x1F59, 0x1F5A, 0x1F5B, 0x1F5C,
            // 3rd
            0x1F3D, 0x1F3E, 0x1F3F, 0x1F40, 0x1F41, 0x1F42, 0x1F43, 0x1F44
        };

        private Mobile m_owner;             // the composer (paid when the track is bought)
        private string m_title;
        private int m_price;                // what the composer asks (the library's MusicInfo.Price is what is charged)
        private bool m_napster;             // shared player to player, not sold
        private string m_composition;       // the song: the music script
        private int m_hashCode;             // the hash of the music: this track's identity
#if !ANGEL_ISLAND
        private uint m_ownerGUID;           // the composer's account (Angel Island's core keeps OwnerGUID on every item)

        [CommandProperty(AccessLevel.GameMaster)]
        public uint OwnerGUID { get { return m_ownerGUID; } set { m_ownerGUID = value; } }
#endif

        #region Properties
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public Mobile Owner
        {
            get { return m_owner; }
            set
            {
                m_owner = value;

                // Identity is account-rooted; PlayerMobile.GUID is the portable handle.
                // Keep OwnerGUID synchronized with the assigned owner.
                OwnerGUID = value is PlayerMobile && !value.Deleted ? AimsHost.Current.AccountGuid(value) : 0;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public string Title { get { return m_title; } set { m_title = value; } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public int Price { get { return m_price; } set { m_price = value; } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool Napster { get { return m_napster; } set { m_napster = value; } }
        public string Composition { get { return m_composition; } set { m_composition = value; } }
        public bool HasComposition => !string.IsNullOrWhiteSpace(m_composition);
        // the author it was transcribed with (or given) stays: the notes' '# Author:' line doesn't replace it as it plays
        protected override bool KeepsItsAuthor => !string.IsNullOrEmpty(Author);
        #endregion Properties

        #region Identity
        [CommandProperty(AccessLevel.GameMaster)]
        public int HashCode { get { return m_hashCode; } set { m_hashCode = value; } }

        // staff: setting it (to anything) recomputes the hash from the composition
        [CommandProperty(AccessLevel.Owner)]
        [CopyableAttribute(CopyType.DoNotCopy)]     // a staff action; HashCode itself is copied
        public int GenHashCode
        {
            get { return m_hashCode; }
            set { m_hashCode = new ManuscriptPaper.LocationTarget(null).CompileHashCode(m_composition ?? ""); }
        }

        // the track as a title by an author for an owner (a remaster keeps it; its HashCode differs)
        public int TrackID
        {
            get
            {
                string text = (m_title ?? "") + (Author ?? "") + (Owner != null ? AimsHost.Current.AccountGuid(Owner).ToString() : "");
                return AimsUtil.GetStableHashCode(text.Replace(" ", "").ToLower());
            }
        }

        // Sheets with the same song are the same track: the music library (a Dictionary keyed by sheet) and the play
        //  lists compare them through IEquatable and GetHashCode. Equals(object) is deliberately not overridden: items in
        //  containers and the world are told apart by reference.
        public int CompareTo(RolledUpSheetMusic? other)
        {
            return other == null ? 1 : HashCode.CompareTo(other.HashCode);
        }

        public bool Equals(RolledUpSheetMusic? other)
        {
            return other != null && GetHashCode() == other.GetHashCode();
        }

        public override int GetHashCode()
        {
            return HashCode == 0 ? base.GetHashCode() : HashCode;
        }

        public override string ToString()
        {
            return Title;
        }
        #endregion Identity

        #region Construction
        [Constructable]
        public RolledUpSheetMusic()
            : this(1)
        {
        }

        [Constructable]
        public RolledUpSheetMusic(int amount)
            : base(graphicIDs[Utility.Random(graphicIDs.Length)], (int)InstrumentSoundType.Harp, (int)InstrumentSoundType.Harp + 1, null)
        {
            InitializeSheet();
            Amount = amount;
        }

        public RolledUpSheetMusic(string author, string title, string composition)
            : this(1)
        {
            Author = author;
            m_title = title;
            m_composition = composition;
        }

        public RolledUpSheetMusic(Mobile owner, InstrumentSoundType instrumentType, string author, string title, int price, string composition)
            : base(graphicIDs[Utility.Random(graphicIDs.Length)], (int)instrumentType, (int)instrumentType + 1, null)
        {
            InitializeSheet();
            Owner = owner;
            Author = author;
            m_title = title;
            m_price = price;
            m_composition = composition;
        }

        public RolledUpSheetMusic(Serial serial)
            : base(serial)
        {
        }

        private void InitializeSheet()
        {
            Name = "rolled up sheet music";
            RequiresBackpack = true;
            Instrument = this;                      // a sheet plays itself
        }
        #endregion Construction

        #region Item behavior
        protected override bool PlaysDefaultSong => false;  // a sheet plays its composition or nothing

#if ANGEL_ISLAND
        // a temporary copy (a music box's) may be cleaned up once it has stopped playing
        public override bool CanDelete()
        {
            return base.CanDelete() && !IsInstancePlaying;
        }
#endif

        public override void OnSingleClick(Mobile from)
        {
            if (Name != null)
                LabelTo(from, Name);
            if (m_title != null)
                LabelTo(from, m_title);
            if (Author != null)
                LabelTo(from, Author);
        }

        public override void OnDoubleClick(Mobile from)
        {   // pass our song to the base to play
            DebugOut("OnDoubleClick in RSM: begin");
            SongMemory = m_composition;
            Instrument = this;
            DebugOut(string.Format("OnDoubleClick in RSM: SongMemory: {0}", SongMemory?.Substring(0, Math.Min(SongMemory.Length, 25))));
            base.OnDoubleClick(from);
        }
        #endregion Item behavior

        #region Dupe
        public RolledUpSheetMusic CopySheet(int amount)
        {   // a complete, independent sheet: same music, attribution, hash and ownership; it plays itself.
            //  Runtime playback links (PlayItem, PlayMobile, AllDone, CheckSourceStatus) are left unset.
            RolledUpSheetMusic copy = new RolledUpSheetMusic(amount);
            copy.m_owner = m_owner;                 // not the Owner setter: that would recompute OwnerGUID
            copy.OwnerGUID = OwnerGUID;
            copy.m_title = m_title;
            copy.m_price = m_price;
            copy.m_napster = m_napster;
            copy.m_composition = m_composition;
            copy.m_hashCode = m_hashCode;
            copy.Author = Author;
            copy.SongName = SongName;
            copy.SongBytes = SongBytes;
            copy.SongFile = SongFile;
            copy.Tempo = Tempo;
            copy.NewTimer = NewTimer;
            copy.Prefetch = Prefetch;
            copy.ConcertMode = ConcertMode;
            copy.RequiresBackpack = RequiresBackpack;
            copy.SuccessSound = SuccessSound;
            copy.FailureSound = FailureSound;
            copy.Instrument = copy;
            return copy;
        }
#if ANGEL_ISLAND
        public override Item Dupe(int amount)
        {
            return base.Dupe(CopySheet(amount), amount);
        }
#endif
        #endregion Dupe

        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(4); // version

            writer.Write(m_napster);
            writer.Write(m_hashCode);
            writer.Write(m_owner);
            writer.WriteEncodedInt(m_price);
            writer.Write(m_title);
            writer.Write(m_composition);
#if !ANGEL_ISLAND
            writer.Write(m_ownerGUID);
#endif
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 4:     // version 4 no longer has the author (RazorInstrument keeps it)
                case 3:
                    {
                        m_napster = reader.ReadBool();
                        goto case 2;
                    }
                case 2:
                    {
                        m_hashCode = reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        // Don't be tempted to sync or clear OwnerGUID here.
                        // Mobile resolution may fail on load (deletion, migration, shard changes),
                        // but OwnerGUID is persistent identity metadata used for future relinking.
                        m_owner = reader.ReadMobile();
                        m_price = reader.ReadEncodedInt();
                        goto case 0;
                    }
                case 0:
                    {
                        if (version <= 3)
                            reader.ReadString();    // the old author (versions 0-3)
                        m_title = reader.ReadString();
                        m_composition = reader.ReadString();
#if !ANGEL_ISLAND
                        if (version >= 4)
                            m_ownerGUID = reader.ReadUInt();
#endif
                        break;
                    }
            }
        }
        #endregion Serialization
    }
}