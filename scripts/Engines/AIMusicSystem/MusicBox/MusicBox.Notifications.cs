/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
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


/* Scripts\Engines\AIMusicSystem\MusicBox\MusicBox.Notifications.cs
 * Changelog
 *  11/6/26, Adam (2026 modernization, 10/29/26 to 11/6/26)
 *      Was MusicBox.Library.cs (svn move); the library itself moved to Library\MusicLibrary.cs. What stays is the
 *      music box's own: the "you sold music" notices, the music box registry and its 5-minute timer, and FindSong's
 *      speech form. Split from MusicBox.cs; its history before 10/29/26 is in MusicBox.cs.
 *      Portable AIMS: a sale goes to the shard through AimsHost.OnMusicSold; "Check the leaderboard!" is said only
 *      when the shard has one (AimsHost.HasLeaderboard).
 */

using Server.Misc;
using System;
using System.Collections.Generic;
using static Server.Items.MusicLibrary;

namespace Server.Items
{
    public partial class MusicBox
    {
        public record YouSoldMusic
        {
            public RolledUpSheetMusic? Rsm;
            public MusicInfo Gml;
            public Mobile You;
            public Mobile? To;
            public YouSoldMusic(Mobile m, Mobile to, RolledUpSheetMusic rsm, MusicInfo gml)
            {
                You = m;    // you sold music
                To = to;    // To
                Rsm = rsm;  // this music
                Gml = gml;  // and global music library entry
            }
            public YouSoldMusic(GenericReader reader)
            {
                int version = reader.ReadEncodedInt();
                switch (version)
                {
                    case 0:
                        {
                            Rsm = (RolledUpSheetMusic)reader.ReadItem();
                            Gml = new MusicInfo(reader);
                            You = reader.ReadMobile();
                            To = reader.ReadMobile();
                            break;
                        }
                }
            }
            public void Serialize(GenericWriter writer)
            {
                int version = 0;
                writer.WriteEncodedInt(version);
                switch (version)
                {
                    case 0:
                        {
                            writer.Write(Rsm);
                            Gml.Serialize(writer);
                            writer.Write(You);
                            writer.Write(To);
                            break;
                        }
                }
            }
        }
        #region PlayerNotify
        public static List<MusicBox> MusicBoxRegistry = new List<MusicBox>();
        public static void Initialize()
        {
            Timer.DelayCall(TimeSpan.FromMinutes(5), new TimerCallback(MaintenanceTimer));
        }
        private static void ProcessPlayerNotification(MusicBox mb, List<YouSoldMusic> playerNotificationList)
        {
            List<YouSoldMusic> notificationList = new List<YouSoldMusic>();
            if (playerNotificationList.Count > 0)
            {
                foreach (YouSoldMusic usm in playerNotificationList)
                {
                    if (usm.You != null && usm.You.NetState != null && usm.You.NetState.Running)
                        notificationList.Add(usm);
                }
            }

            foreach (YouSoldMusic usm in notificationList)
            {
                int competitorTotalSales = 0;
                int competitorSold = 0;
                MusicLibrary.CalculateCompetitorData(usm.You, out competitorTotalSales, out competitorSold);
                int yourTotalSales = 0;
                int youSold = 0;
                int credit = 0;
                MusicLibrary.CalculateSellerData(usm.You, out yourTotalSales, out youSold, out credit);
                playerNotificationList.Remove(usm);
                // the shard's leaderboards (AimsHost: Angel Island's data recorder: gold and one sale)
                AimsHost.Current.OnMusicSold(usm.You, usm.Gml.Price);
                usm.You.SendMessage(54, "You sold music to{0}!", (usm != null && usm.To != null) ? " " + usm.To.Name : "");
                if (yourTotalSales > competitorTotalSales)
                    usm.You.SendMessage(54, "You have made more profit than anyone else.");
                else if (youSold > competitorSold)
                    usm.You.SendMessage(54, "You are the best selling composer on the shard.");
                // the last message only on a shard with a leaderboard
                switch (Utility.Random(AimsHost.Current.HasLeaderboard ? 5 : 4))
                {
                    case 0:
                        {
                            usm.You.SendMessage(54, "Keep up the good work.");
                            break;
                        }
                    case 1:
                        {
                            usm.You.SendMessage(54, "Write more!");
                            break;
                        }
                    case 2:
                        {
                            usm.You.SendMessage(54, "Sell more!");
                            break;
                        }
                    case 3:
                        {
                            usm.You.SendMessage(54, "Be famous!");
                            break;
                        }
                    case 4:
                        {
                            usm.You.SendMessage(54, "Check the leaderboard!");
                            break;
                        }
                }
            }
        }
        public static void MaintenanceTimer()
        {
            // schedule the next tick
            Timer.DelayCall(TimeSpan.FromMinutes(5), new TimerCallback(MaintenanceTimer));

            // process player 'you sold music' notifications
            foreach (MusicBox mb in MusicBoxRegistry)
                foreach (var mc in mb.Profiles)
                    if (mc.Value.PlayerNotificationList.Count > 0)
                        ProcessPlayerNotification(mb, mc.Value.PlayerNotificationList);

            // deleted music boxes leave the registry (after their pending notices above have gone out)
            MusicBoxRegistry.RemoveAll(mb => mb == null || mb.Deleted);

            // the library's own maintenance: the review queue, staff notice, deleted tracks
            MusicLibrary.Maintain();
        }
        #endregion PlayerNotify

        #region Finding a song (speech)
        /// <summary>
        /// The speech commands' form of MusicLibrary.FindSong: tokens are [name] [command] [song words...] and
        /// SongName(tokens) is the title. Returns true if any track matches, with info set to the best
        /// KeyValuePair&lt;RolledUpSheetMusic, MusicLibrary.MusicInfo&gt;.
        /// </summary>
        public static bool FindSong(string[]? tokens, out object info)
        {   // the untyped form older patches call (Patches101_200); the music box uses TryFindSong
            info = null;
            if (!TryFindSong(tokens, out RolledUpSheetMusic track, out MusicInfo record))
                return false;

            info = new KeyValuePair<RolledUpSheetMusic, MusicInfo>(track, record);
            return true;
        }

        // the speech form, typed: the track and its library record
        private static bool TryFindSong(string[]? tokens, out RolledUpSheetMusic track, out MusicInfo info)
        {
            if (tokens != null && MusicLibrary.FindEntry(SongName(tokens), out KeyValuePair<RolledUpSheetMusic, MusicInfo> entry))
            {
                track = entry.Key;
                info = entry.Value;
                return true;
            }

            track = null!;
            info = null!;
            return false;
        }
        #endregion Finding a song (speech)
    }
}