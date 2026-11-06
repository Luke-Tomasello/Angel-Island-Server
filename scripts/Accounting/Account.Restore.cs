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

/* Scripts/Accounting/Account.Restore.cs
 * CHANGELOG
 *  1/26/26, Adam
 *  Created
 */

using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Xml;

namespace Server.Accounting
{
    public partial class Account
    {
        /*
         * Death Snapshot (per-account)
         *
         * Purpose:
         * Persist a compact snapshot of a character's raw stats and base skills at death,
         * stored on the Account for later restoration.
         *
         * Storage model:
         * - Stats: fixed-size list of 3 ints, indexed by StatIndex (Str/Dex/Int).
         * - Skills: list of doubles indexed by (int)SkillName. The index is the contract.
         *
         * Memory policy:
         * - We keep the fields initialized to empty lists (never null) for robustness.
         * - LoadSnapshot and Snapshot will replace/resize lists as needed.
         * - SaveSnapshot writes nothing unless a valid snapshot is present.
         */

        private const int SnapshotStatCount = 3;
        private const int SnapshotDefaultSkillCapacity = 48;

        // Always non-null; empty means "no snapshot present".
        private List<double> m_Skills = new List<double>(0);
        private List<int> m_Stats = new List<int>(0);

        public enum StatIndex : byte
        {
            Str = 0,
            Dex = 1,
            Int = 2
        }

        [Usage("RestorePlayer <target>")]
        [Description("Restores the target player's stats/skills from the account snapshot (if present).")]
        public static void RestorePlayer(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Target the player to restore...");
                e.Mobile.Target = new RestorePlayerTarget(e.Mobile);
            }
            catch
            {
                // Intentionally suppressed; command handlers should not crash the server.
            }
        }

        private class RestorePlayerTarget : Target
        {
            public RestorePlayerTarget(Mobile caller)
                : base(-1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                PlayerMobile? pm = targeted as PlayerMobile;

                if (pm == null)
                {
                    from.SendMessage("That is not a player.");
                    return;
                }

                Account? acct = pm.Account as Account;

                if (acct == null)
                {
                    from.SendMessage("That player has no account object to restore from.");
                    return;
                }

                if (!acct.HasSnapshot)
                {
                    from.SendMessage("No saved stat/skill snapshot exists for that account.");
                    return;
                }

                // Restore stats (canonical ordering)
                pm.RawStr = acct.m_Stats[(int)StatIndex.Str];
                pm.RawDex = acct.m_Stats[(int)StatIndex.Dex];
                pm.RawInt = acct.m_Stats[(int)StatIndex.Int];

                // Restore skills (index by SkillName)
                int restoredSkills = 0;

                for (int i = 0; i < pm.Skills.Length; i++)
                {
                    Skill sk = pm.Skills[i];
                    int idx = (int)sk.SkillName;

                    if (idx < 0 || idx >= acct.m_Skills.Count)
                        continue;

                    sk.Base = acct.m_Skills[idx];
                    restoredSkills++;
                }

                // Clamp vitals after stat changes
                if (pm.Hits > pm.HitsMax) pm.Hits = pm.HitsMax;
                if (pm.Stam > pm.StamMax) pm.Stam = pm.StamMax;
                if (pm.Mana > pm.ManaMax) pm.Mana = pm.ManaMax;

                from.SendMessage(
                    "Restored {0}'s snapshot (Str/Dex/Int = {1}/{2}/{3}) and {4} skills.",
                    pm.Name, pm.RawStr, pm.RawDex, pm.RawInt, restoredSkills);
            }
        }

        // True only when we have enough data to perform a meaningful restore.
        private bool HasSnapshot
        {
            get { return m_Stats.Count >= SnapshotStatCount && m_Skills.Count > 0; }
        }

        public void Snapshot(Mobile m)
        {
            if (m == null || m.Deleted)
                return;

            // Stats: fixed-size, canonical order.
            m_Stats = new List<int>(SnapshotStatCount)
        {
            m.RawStr, // Str
            m.RawDex, // Dex
            m.RawInt  // Int
        };

            // Skills: indexed by (int)SkillName.
            // Size to the maximum SkillName index present on this Mobile.
            int maxSkillIndex = -1;

            for (int i = 0; i < m.Skills.Length; i++)
            {
                int idx = (int)m.Skills[i].SkillName;
                if (idx > maxSkillIndex)
                    maxSkillIndex = idx;
            }

            int needed = maxSkillIndex + 1;

            if (needed <= 0)
            {
                m_Skills = new List<double>(0);
                return;
            }

            List<double> skills = new List<double>(needed);
            for (int i = 0; i < needed; i++)
                skills.Add(0.0);

            for (int i = 0; i < m.Skills.Length; i++)
            {
                Skill sk = m.Skills[i];
                int idx = (int)sk.SkillName;

                if (idx >= 0 && idx < needed)
                    skills[idx] = sk.Base;
            }

            m_Skills = skills;
        }
        private double GetTotalSkill(Mobile m)
        {
            double total = 0;

            for (int i = 0; i < m.Skills.Length; i++)
                total += m.Skills[i].Base;

            return total;
        }
        private double GetSnapshotTotalSkill()
        {
            double total = 0;

            for (int i = 0; i < m_Skills.Count; i++)
                total += m_Skills[i];

            return total;
        }

        public void LoadSnapshot(XmlElement node)
        {
            // Default to "no snapshot"
            m_Stats = new List<int>(0);
            m_Skills = new List<double>(0);

            if (node == null)
                return;

            // ---- stats ----
            List<int> stats = new List<int>(SnapshotStatCount);
            XmlElement? statsNode = node["stats"];

            if (statsNode != null)
            {
                foreach (XmlElement stat in statsNode.GetElementsByTagName("stat"))
                {
                    try
                    {
                        stats.Add(int.Parse(Accounts.GetText(stat, null)));
                    }
                    catch (Exception ex)
                    {
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }

            // Require exactly the canonical stat count to accept the snapshot.
            if (stats.Count >= SnapshotStatCount)
            {
                // Normalize to canonical size in case extra nodes were present.
                if (stats.Count > SnapshotStatCount)
                    stats = stats.GetRange(0, SnapshotStatCount);

                m_Stats = stats;
            }

            // ---- skills ----
            List<double> skills = new List<double>(SnapshotDefaultSkillCapacity);
            XmlElement? skillsNode = node["skills"];

            if (skillsNode != null)
            {
                foreach (XmlElement skill in skillsNode.GetElementsByTagName("skill"))
                {
                    try
                    {
                        // Use invariant culture to match SaveSnapshot formatting.
                        string text = Accounts.GetText(skill, null);
                        skills.Add(double.Parse(text, System.Globalization.CultureInfo.InvariantCulture));
                    }
                    catch (Exception ex)
                    {
                        EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
                    }
                }
            }

            if (skills.Count > 0)
                m_Skills = skills;

            // If either side is missing, treat as no snapshot (avoid partial restores).
            if (!HasSnapshot)
            {
                m_Stats = new List<int>(0);
                m_Skills = new List<double>(0);
            }
        }

        public void SaveSnapshot(XmlTextWriter xml)
        {
            if (xml == null)
                return;

            // Write nothing unless we have a complete snapshot.
            if (!HasSnapshot)
                return;

            // ---- stats ----
            xml.WriteStartElement("stats");
            xml.WriteAttributeString("count", SnapshotStatCount.ToString());

            // Canonical order: Str, Dex, Int
            for (int i = 0; i < SnapshotStatCount; i++)
            {
                xml.WriteStartElement("stat");
                xml.WriteString(m_Stats[i].ToString());
                xml.WriteEndElement();
            }

            xml.WriteEndElement();

            // ---- skills ----
            xml.WriteStartElement("skills");
            xml.WriteAttributeString("count", m_Skills.Count.ToString());

            for (int i = 0; i < m_Skills.Count; i++)
            {
                xml.WriteStartElement("skill");
                xml.WriteString(m_Skills[i].ToString(System.Globalization.CultureInfo.InvariantCulture));
                xml.WriteEndElement();
            }

            xml.WriteEndElement();
        }
        private bool IsNewCharacter(PlayerMobile pm)
        {
            return (DateTime.UtcNow - pm.Created) < TimeSpan.FromMinutes(10);
        }

        internal bool IsSnapshotDowngrade(PlayerMobile pm)
        {
            // Age gate
            if (IsNewCharacter(pm))
                return true;

            // Power gate
            if (HasSnapshot)
            {
                double oldTotal = GetSnapshotTotalSkill();
                double newTotal = GetTotalSkill(pm);

                if (newTotal < oldTotal * 0.5)
                    return true;
            }

            return false;
        }

    }


}