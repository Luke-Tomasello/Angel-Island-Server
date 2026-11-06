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


/* Scripts\Engines\AIMusicHost\AngelIslandAimsHost.cs
 * Changelog
 *  11/6/26, Adam (11/5/26 to 11/6/26)
 *      Angel Island's AimsHost: wires the music system (Scripts\Engines\AIMusicSystem) to Angel Island's systems.
 *      It lives outside the AIMS folder, so the AIMS folder drops into other RunUO servers unchanged; a shard without
 *      one gets AimsHost's defaults. Every member does what AIMS did here before it was portable:
 *      Logging: LogHelper.LogException, Utility.Monitor, and log files through LogHelper. Staff messages:
 *      Item.SendMessageNearby and Utility.SendSystemMessage. Folders: Core.DataDirectory and Core.LogsDirectory.
 *      Identity: the account's GUID (Mobile.AcctGUID), households by accounts shared by IP. Music Karma:
 *      PlayerMobile.NpcGuildPoints; the music filter: PlayerMobile.FilterMusic. Purchases: all enrolled bank accounts.
 *      Copies: Utility.Dupe, staff copies tracked (Utility.TrackStaffOwned), temporary items (TempObject, staff owned:
 *      cron's safety net), internal storage (MoveToIntStorage), the system account. Settings: Test Center
 *      (Core.GMNTC_CFG), [play access (CoreAI.PlayAccessLevel). Sales: the data recorder's leaderboards (music gold and
 *      one sale).
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Engines.DataRecorder;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;

namespace Server
{
    public class AngelIslandAimsHost : AimsHost
    {
        #region Logging
        public override void LogException(Exception ex, string? message)
        {
            if (message != null)
                LogHelper.LogException(ex, message);
            else
                LogHelper.LogException(ex);
        }

        public override void WriteLine(string text, ConsoleColor color)
        {
            Utility.Monitor.WriteLine(text, color);
        }

        public override void WriteLog(string fileName, Mobile? about, IEnumerable<object> lines)
        {
            LogHelper logger = new LogHelper(fileName, false, false);
            if (about != null)
                logger.Log(LogType.Mobile, about);
            foreach (object line in lines)
                logger.Log(line);
            logger.Finish();
        }
        #endregion Logging

        #region Messages
        public override void SendMessageNearby(Item item, string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            item.SendMessageNearby(text, accesslevel, hue);
        }

        public override void SendSystemMessage(IEntity source, double second_timeout, string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            Utility.SendSystemMessage(source, second_timeout, text, accesslevel, hue);
        }
        #endregion Messages

        #region Files
        public override string DataDirectory
        {
            get { return Core.DataDirectory; }
        }

        public override string LogsDirectory
        {
            get { return Core.LogsDirectory; }
        }
        #endregion Files

        #region Identity
        public override uint AccountGuid(IAccount? acct)
        {
            return (acct as Account)?.GUID ?? 0;
        }

        // no Deleted check: a sheet's TrackID uses its owner's GUID as it is
        public override uint AccountGuid(Mobile? m)
        {
            return m != null ? m.AcctGUID : 0;
        }

        // accounts shared by IP (the owner's last game login); the caller catches and fails closed
        public override bool IsHousehold(Mobile owner, Mobile other)
        {
            if (owner.Account is not Account acct)
                return false;

            IPAddress ip = acct.LastGAMELogin;

            if (ip == null)
                return false;

            ArrayList list = Utility.GetSharedAccounts(ip);

            if (list == null || list.Count == 0)
                return false;

            if (other.Account == null)
                return false;

            return list.Contains(other.Account);
        }
        #endregion Identity

        #region Player data
        public override double GetMusicKarma(Mobile m)
        {
            return m is PlayerMobile pm ? pm.NpcGuildPoints : 0.0;
        }

        public override void SetMusicKarma(Mobile m, double value)
        {
            if (m is PlayerMobile pm)
                pm.NpcGuildPoints = value;
        }

        public override bool GetFilterMusic(Mobile m)
        {
            return m is PlayerMobile pm && pm.FilterMusic;
        }

        public override void SetFilterMusic(Mobile m, bool value)
        {
            if (m is PlayerMobile pm)
                pm.FilterMusic = value;
        }
        #endregion Player data

        #region Gold
        public override bool Withdraw(Mobile buyer, int amount)
        {
            return Banker.CombinedWithdrawFromAllEnrolled(buyer, amount);
        }
        #endregion Gold

        #region Items
        public override Item? Dupe(Item item)
        {
            return Utility.Dupe(item);
        }

        // TrackStaffOwned marks the copy staff owned and logs it (staff dupe.log)
        public override void MarkStaffOwned(Mobile staff, Item copy)
        {
            Utility.TrackStaffOwned(staff, copy);
        }

        public override void MarkTemporary(Item item)
        {
            item.SetItemBool(Item.ItemBoolTable.TempObject, true);  // okay to delete
            item.IsStaffOwned = true;                               // keeps other rules from deleting it
        }

        public override void MoveToInternal(Item item)
        {
            item.MoveToIntStorage();
        }

        public override Mobile? SystemMobile
        {
            get { return World.GetSystemAcct(); }
        }
        #endregion Items

        #region Settings
        public override bool IsTestCenter
        {
            get { return Core.GMNTC_CFG; }
        }

        public override AccessLevel PlayAccessLevel
        {
            get { return CoreAI.PlayAccessLevel; }
        }
        #endregion Settings

        #region Events
        // the leaderboards: gold earned and one more sale
        public override void OnMusicSold(Mobile composer, int price)
        {
            DataRecorder.Add(composer, RecorderDomain.MusicGold, price);
            DataRecorder.Add(composer, RecorderDomain.MusicPoints, 1);
        }

        public override bool HasLeaderboard
        {
            get { return true; }
        }
        #endregion Events
    }
}
