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

/* Scripts/Accounting/Account.Seasons.cs
 * CHANGELOG
 *  6/9/26, Adam
 *  Created
 */

using Server.Engines.TerritoryControl;
using Server.Items;
using Server.Mobiles;
using System;
using System.Xml;

namespace Server.Accounting
{
    public partial class Account
    {
        private DateTime m_OrcEnemyUntil = DateTime.MinValue;

        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public DateTime OrcEnemyUntil
        {
            get { return m_OrcEnemyUntil; }
            set { m_OrcEnemyUntil = value; }
        }

        #region Helpers
        public static bool EnforceOrcMaskRestrictions(PlayerMobile? pm, bool hide = true, bool message = true, bool requireDistrict = true)
        {
            if (pm == null || pm.Deleted)
                return false;

            if (!(Core.RuleSets.SeasonsRules(2) || Core.RuleSets.UpheavalRules()))
                return false;

            if (pm.Account is not Account acct)
                return false;

            if (!acct.IsOrcEnemy)
                return false;

            if (requireDistrict && TerritorySystem.FindDistrict(pm) == null)
                return false;

            if (pm.FindItemOnLayer(Layer.Helm) is not OrcishKinMask mask)
                return false;

            pm.RemoveItem(mask);

            if (hide)
                pm.Hidden = true;

            if (pm.Backpack != null)
                pm.Backpack.DropItem(mask);
            else
                mask.MoveToWorld(pm.Location, pm.Map);

            if (message)
            {
                pm.SendMessage(0x35,
                    "The occupation forces know your face. Your disguise will not fool them for another {0}.",
                    FormatOrcEnemyDuration(acct));
            }

            return true;
        }
        public bool IsOrcEnemy
        {
            get
            {
                return (m_Flags & AccountFlags.OrcEnemy) != 0
                       && m_OrcEnemyUntil > DateTime.UtcNow;
            }
        }
        public void SetSeason2OrcEnemy(TimeSpan duration)
        {
            Flags |= AccountFlags.OrcEnemy;
            m_OrcEnemyUntil = DateTime.UtcNow + duration;
        }

        public void ClearOrcEnemy()
        {
            Flags &= ~AccountFlags.OrcEnemy;
            m_OrcEnemyUntil = DateTime.MinValue;
        }

        internal static string FormatOrcEnemyDuration(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                return "0 minutes";

            if (duration.TotalHours >= 1.0)
                return string.Format("{0}:{1:D2}",
                    (int)duration.TotalHours,
                    duration.Minutes);

            int minutes = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes));

            return string.Format("{0} minute{1}",
                minutes,
                minutes == 1 ? "" : "s");
        }
        internal static string FormatOrcEnemyDuration(Accounting.Account? acct)
        {
            if (acct == null)
                return "0 minutes";

            TimeSpan duration = acct.OrcEnemyUntil - DateTime.UtcNow;

            return FormatOrcEnemyDuration(duration);
        }
        #endregion Helpers

        #region I/O

        public void LoadSeasons(XmlElement node)
        {
            if (node == null)
                return;

            m_OrcEnemyUntil = Accounts.GetDateTime(
                Accounts.GetText(node["OrcEnemyUntil"], null),
                DateTime.MinValue);
        }

        public void SaveSeasons(XmlTextWriter xml)
        {
            if (xml == null)
                return;

            if (m_OrcEnemyUntil != DateTime.MinValue)
            {
                xml.WriteStartElement("OrcEnemyUntil");
                xml.WriteString(XmlConvert.ToString(m_OrcEnemyUntil, XmlDateTimeSerializationMode.Utc));
                xml.WriteEndElement();
            }
        }

        #endregion I/O
    }
}