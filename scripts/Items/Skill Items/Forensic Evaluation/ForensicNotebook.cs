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

/* Scripts\Items\Skill Items\Forensic Evaluation\ForensicNotebook.cs
 * CHANGELOG:
 *  6/3/2026, Adam
 *      Added Detective / Forensic Lead Tracking system
 *      See Tracking.cs for a full explanation.
 *  6/2/2026, Adam
 *      First check in
 */

using Server.SkillHandlers;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Items
{
    public class ForensicNotebook : Item
    {
        private List<ForensicEvaluation.ForensicInfo> m_Entries;

        [CommandProperty(AccessLevel.GameMaster)]
        public int EntryCount
        {
            get { return m_Entries.Count; }
        }

        [Constructable]
        public ForensicNotebook()
            : base(0x2252)
        {
            Name = "a forensic notebook";
            Weight = 1.0;
            LootType = LootType.Regular;
            Layer = Layer.FirstValid;
            m_Entries = new List<ForensicEvaluation.ForensicInfo>();
        }

        public ForensicNotebook(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            Pack();

            base.OnSingleClick(from);

            LabelTo(from, "{0} forensic note{1}", m_Entries.Count, m_Entries.Count == 1 ? "" : "s");

            List<NotebookSubject> subjects = GetNotebookSubjects();

            if (subjects.Count == 0)
                return;

            LabelTo(from, "Notes on:");

            int shown = Math.Min(3, subjects.Count);

            for (int i = 0; i < shown; i++)
            {
                NotebookSubject subject = subjects[i];

                if (subject.NoteCount > 1)
                    LabelTo(from, "{0} ({1})", subject.Name, subject.NoteCount);
                else
                    LabelTo(from, subject.Name);
            }

            int remaining = subjects.Count - shown;

            if (remaining > 0)
                LabelTo(from, "{0} other{1}...", remaining, remaining == 1 ? "" : "s");
        }
        private class NotebookSubject
        {
            public string Name;
            public int OffenseRank;
            public int NoteCount;
        }

        private List<NotebookSubject> GetNotebookSubjects()
        {
            Dictionary<Mobile, NotebookSubject> subjects = new Dictionary<Mobile, NotebookSubject>();

            foreach (ForensicEvaluation.ForensicInfo info in m_Entries)
            {
                if (info == null || info.Expired)
                    continue;

                AddNotebookSubject(subjects, info.Killer, 0);

                if (info.Looters != null)
                {
                    foreach (Mobile looter in info.Looters)
                        AddNotebookSubject(subjects, looter, 1);
                }

                AddNotebookSubject(subjects, info.LockPicker, 2);
            }

            return subjects.Values
                .OrderBy(s => s.OffenseRank)
                .ThenByDescending(s => s.NoteCount)
                .ThenBy(s => s.Name)
                .ToList();
        }

        private static void AddNotebookSubject(Dictionary<Mobile, NotebookSubject> subjects, Mobile mobile, int offenseRank)
        {
            if (mobile == null || mobile.Deleted)
                return;

            NotebookSubject? subject;

            if (!subjects.TryGetValue(mobile, out subject))
            {
                subject = new NotebookSubject
                {
                    Name = mobile.Name ?? "(unknown)",
                    OffenseRank = offenseRank,
                    NoteCount = 0
                };

                subjects[mobile] = subject;
            }
            else if (offenseRank < subject.OffenseRank)
            {
                subject.OffenseRank = offenseRank;
            }

            subject.NoteCount++;
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            Container pack = from.Backpack;

            if (Parent != from && (pack == null || Parent != pack))
            {
                from.SendMessage("The notebook must be equipped or in your backpack (and not in a container within) to open.");
                return;
            }

            from.SendMessage("What corpse, person, or object do you wish to examine?");
            from.Target = new InternalTarget(this);
        }

        private void Examine(Mobile from, object target)
        {
            ForensicEvaluation.ForensicInfo info;
            ForensicEvaluation.ForensicResult result =
                ForensicEvaluation.GetForensicInformation(from, target, out info);

            switch (result)
            {
                case ForensicEvaluation.ForensicResult.Success:
                    {
                        if (IsDuplicateCorpse(info))
                        {
                            from.SendMessage("You already have notes on this corpse.");
                            break;
                        }

                        if (HasUsefulInformation(info))
                        {
                            m_Entries.Add(info);

                            from.SendMessage("You take note of the following:");

                            if (info.Messages.Count > 0)
                            {
                                foreach (string message in info.Messages)
                                    from.SendMessage(message);
                            }
                            else
                            {
                                SendSimpleSummary(from, info);
                            }
                        }
                        else
                        {
                            from.SendMessage("You examine the target, but find nothing worth recording.");
                        }

                        break;
                    }

                case ForensicEvaluation.ForensicResult.SkillFailure:
                    {
                        from.SendMessage("You fail to gather enough useful forensic information.");
                        SendMessages(from, info);
                        break;
                    }

                case ForensicEvaluation.ForensicResult.InsufficientSkill:
                    {
                        from.SendMessage("You lack the skill to make sense of the evidence.");
                        SendMessages(from, info);
                        break;
                    }

                case ForensicEvaluation.ForensicResult.NoInformation:
                    {
                        from.SendMessage("You find no useful forensic information.");
                        SendMessages(from, info);
                        break;
                    }

                case ForensicEvaluation.ForensicResult.InvalidTarget:
                default:
                    {
                        from.SendMessage("That is not a valid forensic target.");
                        SendMessages(from, info);
                        break;
                    }
            }
        }
        private bool IsDuplicateCorpse(ForensicEvaluation.ForensicInfo newInfo)
        {
            if (newInfo == null || newInfo.TimeOfDeath == DateTime.MinValue || newInfo.Killer == null)
                return false;

            return m_Entries.Any(existing =>
                existing != null &&
                !existing.Expired &&
                existing.Killer == newInfo.Killer &&
                existing.TimeOfDeath != DateTime.MinValue &&
                // The 5-second tolerance is there specifically to handle small timing differences caused by
                // serialization / deserialization order when the server loads.
                Math.Abs((existing.TimeOfDeath - newInfo.TimeOfDeath).TotalSeconds) < 5.0); // 5-second tolerance
        }
        private static bool HasUsefulInformation(ForensicEvaluation.ForensicInfo info)
        {
            if (info == null)
                return false;

            return info.HasBounty
                || info.IsThief
                || info.IsCriminal
                || info.Killer != null
                || info.LockPicker != null
                || (info.Looters != null && info.Looters.Count > 0)
                || (info.Messages != null && info.Messages.Count > 0);
        }

        private static void SendMessages(Mobile from, ForensicEvaluation.ForensicInfo info)
        {
            if (from == null || info == null || info.Messages == null)
                return;

            foreach (string message in info.Messages)
                from.SendMessage(message);
        }

        private static void SendSimpleSummary(Mobile from, ForensicEvaluation.ForensicInfo info)
        {
            if (info.Killer != null)
                from.SendMessage("They were killed by {0}.", info.Killer.Name);
            else if (info.LockPicker != null)
                from.SendMessage("This lock was opened by {0}.", info.LockPicker.Name);
            else if (info.Looters != null && info.Looters.Count > 0)
                from.SendMessage("The corpse appears to have been disturbed.");
            else
                from.SendMessage("You record a forensic observation.");
        }

        private class InternalTarget : Target
        {
            private readonly ForensicNotebook m_Notebook;

            public InternalTarget(ForensicNotebook notebook)
                : base(10, false, TargetFlags.None)
            {
                m_Notebook = notebook;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Notebook == null || m_Notebook.Deleted)
                    return;

                if (m_Notebook.Parent != from && (from.Backpack == null || m_Notebook.Parent != from.Backpack))
                {
                    from.SendMessage("The notebook must be equipped or in your backpack (and not in a container within) to use.");
                    return;
                }

                m_Notebook.Examine(from, targeted);
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                from.SendMessage("You close your forensic notebook.");
            }
        }

        #region API
        public void Pack()
        {
            m_Entries.RemoveAll(info => info == null || info.Expired);
        }

        public IEnumerable<Mobile> GetActiveSuspects()
        {
            Pack();

            return m_Entries
                .Where(info => info != null && !info.Expired && info.Killer != null && !info.Killer.Deleted)
                .Select(info => info.Killer)
                .Distinct();
        }

        public List<ForensicEvaluation.ForensicInfo> GetActiveLeads(Mobile? suspect)
        {
            Pack();

            if (suspect == null || suspect.Deleted)
                return new List<ForensicEvaluation.ForensicInfo>();

            return m_Entries
                .Where(info => info != null && !info.Expired && info.Killer == suspect)
                .ToList();
        }
        public bool HasLeadOn(Mobile? suspect)
        {
            return GetLeadCount(suspect) > 0;
        }

        public int GetLeadCount(Mobile? suspect)
        {
            return GetActiveLeads(suspect).Count;
        }

        public static ForensicNotebook? Find(Mobile from)
        {
            if (from == null)
                return null;

            // Equipped
            if (from.FindItemOnLayer(Layer.OneHanded) is ForensicNotebook notebook)
                return notebook;

            if (from.FindItemOnLayer(Layer.TwoHanded) is ForensicNotebook notebook2)
                return notebook2;

            // Backpack
            if (from.Backpack == null)
                return null;

            return from.Backpack.FindItemByType(typeof(ForensicNotebook), false) as ForensicNotebook;
        }

        public static ForensicNotebook? FindBest(Mobile from, Mobile? suspect)
        {
            // V1 can simply return Find(from).
            // Later this can scan multiple notebooks if needed.
            ForensicNotebook? notebook = Find(from);

            if (notebook == null || !notebook.HasLeadOn(suspect))
                return null;

            return notebook;
        }
        public double GetLeadStrengthMultiplier(Mobile? suspect)
        {
            int count = GetLeadCount(suspect);

            if (count <= 0)
                return 1.0;

            return Math.Min(1.0 + (count - 1) * 0.25, 2.0);
        }
        #endregion API

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Entries.Count);

            foreach (ForensicEvaluation.ForensicInfo info in m_Entries)
                info.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Entries = new List<ForensicEvaluation.ForensicInfo>();

            switch (version)
            {
                case 0:
                    {
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            ForensicEvaluation.ForensicInfo info = new ForensicEvaluation.ForensicInfo();
                            info.Deserialize(reader);
                            m_Entries.Add(info);
                        }

                        break;
                    }
            }

            Pack();
        }
    }
}