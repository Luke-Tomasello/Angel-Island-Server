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

/* Scripts/Skills/ForensicEval.cs
 * CHANGELOG:
 *  6/3/2026, Adam
 *      Added Detective / Forensic Lead Tracking system
 *      See Tracking.cs for a full explanation.
 *  2/12/06, Plasma
 *      Added perma grey detection and skill delay
 *	6/26/05, Kit
		Fixed spelling error
 *	4/13/05, Adam
 *		Fixed spelling error
 *	1/24/05, Darva
 *		Made staff names only visible to a evaluator with the same, or higher access.
 *	5/26/04, Pixie
 *		Changed to use utility functions in BountyKeeper instead of iterating
 *		through the bounties arraylist manually.
 */

using Server.Diagnostics;
using Server.Engines;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.SkillHandlers
{
    public class ForensicEvaluation
    {
        public enum ForensicResult
        {
            Success,
            SkillFailure,
            InvalidTarget,
            InsufficientSkill,
            NoInformation
        }

        public class ForensicInfo
        {
            // Strongly-typed flags for API consumers
            public bool HasBounty { get; set; }
            public int BountyAmount { get; set; }
            public bool HasLordBritishBonus { get; set; }
            public string? WhoWants { get; set; } = "an Independent Party";

            public bool IsThief { get; set; }
            public bool IsCriminal { get; set; }

            public Mobile Killer { get; set; }
            public List<Mobile> Looters { get; set; } = new List<Mobile>();
            public DateTime TimeOfDeath { get; set; } = DateTime.MinValue;
            public Mobile LockPicker { get; set; }

            // Forensic Evaluation does not currently use this field directly.
            // It exists to support consumers of ForensicInfo (e.g. ForensicNotebook).
            public DateTime Created { get; set; } = DateTime.UtcNow;
            public DateTime Expires { get; set; } = DateTime.UtcNow + TimeSpan.FromHours(48);
            public bool Expired { get { return DateTime.UtcNow >= Expires; } }

            // Player-facing messages (one per discovered piece of info)
            private readonly List<string> _messages = new List<string>();

            public IReadOnlyList<string> Messages => _messages;

            public void AddMessage(string? message)
            {
                if (!string.IsNullOrEmpty(message))
                    _messages.Add(message);
            }

            /// <summary>
            /// Helper for callers who want a specific message
            /// </summary>
            public string? GetMessage(string category)
            {
                return category.ToLower() switch
                {
                    "bounty" => HasBounty ? _messages.FirstOrDefault(m => m.Contains("wanted")) : null,
                    "thief" => IsThief ? "That person is a scoundrel!" : null,
                    "criminal" => IsCriminal ? "You identify them as a criminal!" : "They appear to be a law abiding citizen",
                    _ => null
                };
            }

            #region Serialization

            public void Serialize(GenericWriter writer)
            {
                writer.Write((int)1); // version

                writer.WriteDeltaTime(TimeOfDeath);

                // Bounty info
                writer.Write(HasBounty);
                writer.Write(BountyAmount);
                writer.Write(HasLordBritishBonus);
                writer.Write(WhoWants);

                // Criminal/Thief info
                writer.Write(IsThief);
                writer.Write(IsCriminal);

                // Corpse / Lockable info
                writer.Write(Killer);

                writer.Write(Looters.Count);
                foreach (Mobile m in Looters)
                    writer.Write(m);

                writer.Write(LockPicker);

                // Messages
                writer.Write(Messages.Count);
                foreach (string msg in Messages)
                    writer.Write(msg);

                // Persist auxiliary ForensicInfo state used by downstream consumers
                // such as ForensicNotebook and future detective systems.
                writer.Write(Created);
                writer.Write(Expires);
            }

            public void Deserialize(GenericReader reader)
            {
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            TimeOfDeath = reader.ReadDeltaTime();
                            goto case 0;
                        }
                    case 0:
                        {
                            HasBounty = reader.ReadBool();
                            BountyAmount = reader.ReadInt();
                            HasLordBritishBonus = reader.ReadBool();
                            WhoWants = reader.ReadString();

                            IsThief = reader.ReadBool();
                            IsCriminal = reader.ReadBool();

                            Killer = reader.ReadMobile();

                            Looters = new List<Mobile>();
                            int looterCount = reader.ReadInt();
                            for (int i = 0; i < looterCount; i++)
                                Looters.Add(reader.ReadMobile());

                            LockPicker = reader.ReadMobile();

                            // Messages
                            int msgCount = reader.ReadInt();
                            for (int i = 0; i < msgCount; i++)
                                AddMessage(reader.ReadString());

                            // Serialize client-owned metadata stored within ForensicInfo.
                            // The Forensic Evaluation skill does not currently consume these fields directly.
                            Created = reader.ReadDateTime();
                            Expires = reader.ReadDateTime();

                            break;
                        }
                }
            }

            #endregion
        }

        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Forensics].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new ForensicTarget();
            m.RevealingAction();
            m.SendLocalizedMessage(500906); // What would you like to evaluate?
            return TimeSpan.FromSeconds(1.0);
        }

        /// <summary>
        /// Public API: Perform Forensic Evaluation on a target
        /// </summary>
        public static ForensicResult GetForensicInformation(Mobile from, object target, out ForensicInfo info)
        {
            info = new ForensicInfo();
            try
            {
                if (target == null || from == null)
                {
                    info.AddMessage("You have specified an invalid target.");
                    return ForensicResult.InvalidTarget;
                }

                double skillValue = from.Skills[SkillName.Forensics].Value;

                // 10 second default delay (reset later on failure)
                from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(10.0).TotalMilliseconds;

                // === MOBILE TARGET ===
                if (target is Mobile targ)
                {
                    return EvaluateMobile(from, targ, skillValue, ref info);
                }
                // === CORPSE TARGET ===
                else if (target is Corpse corpse)
                {
                    return EvaluateCorpse(from, corpse, skillValue, ref info);
                }
                // === LOCKPICKABLE TARGET ===
                else if (target is ILockpickable lockable)
                {
                    return EvaluateLockable(from, lockable, ref info);
                }
                else
                {
                    info.AddMessage("You have specified an invalid target. Try again.");
                    return ForensicResult.InvalidTarget;
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                return ForensicResult.InvalidTarget;
            }
        }

        private static ForensicResult EvaluateMobile(Mobile from, Mobile targ, double skillValue, ref ForensicInfo info)
        {
            // === BOUNTY CHECK ===
            if (targ is PlayerMobile pm && skillValue > 80)
            {
                info.HasBounty = BountyKeeper.BountiesOnPlayer(pm) > 0;
                info.BountyAmount = BountyKeeper.RewardForPlayer(pm);
                info.HasLordBritishBonus = BountyKeeper.IsEligibleForLBBonus(pm);

                if (info.HasLordBritishBonus)
                {
                    info.BountyAmount += BountyKeeper.CurrentLBBonusAmount;
                    info.WhoWants = "Lord British";
                }

                int infoLevel = skillValue switch
                {
                    >= 100 => 3,
                    >= 98 => Utility.Random(2, 3),
                    >= 95 => Utility.Random(1, 2),
                    _ => 1
                };

                info.AddMessage(GetBountyMessage(info, infoLevel));
            }

            // === THIEVES GUILD + CRIMINAL CHECK ===
            double requiredFE = (targ is PlayerMobile p) ? p.Skills[SkillName.Stealing].Value / 2.0 : 0.0;

            if (from.CheckTargetSkill(SkillName.Forensics, targ, requiredFE, 100.0, new object?[2] { targ, null }))
            {
                if (targ is PlayerMobile player && player.NpcGuild == NpcGuild.ThievesGuild)
                {
                    info.IsThief = true;
                    info.AddMessage("That person is a scoundrel!");
                }

                EvaluateCriminalStatus(from, targ, ref info);
            }
            else if (!info.HasBounty && !info.IsThief)
            {
                info.AddMessage("You cannot gather enough evidence to reach a reliable conclusion on their criminal status");
            }

            return ForensicResult.Success;
        }

        private static void EvaluateCriminalStatus(Mobile from, Mobile targ, ref ForensicInfo info)
        {
            if (targ is not PlayerMobile pm)
                return;

            double ss = from.Skills[SkillName.Forensics].Value + Utility.Random(21) - 10;
            double ts = targ.Skills[SkillName.Stealing].Value + Utility.Random(21) - 10;

            if (from.AccessLevel >= targ.AccessLevel && ss >= ts)
            {
                info.IsCriminal = pm.PermaFlags.Count > 0;

                if (Utility.RandomDouble() < (0.01 * (100 - from.Skills[SkillName.Forensics].Value + 1)))
                    info.IsCriminal = !info.IsCriminal;

                string criminalMessage = info.IsCriminal
                    ? "You identify them as a criminal!"
                    : "They appear to be a law abiding citizen";

                info.AddMessage(criminalMessage);
            }
            else
            {
                if (!info.HasBounty && !info.IsThief)
                    info.AddMessage("You cannot gather enough evidence to reach a reliable conclusion on their criminal status");
            }
        }

        private static ForensicResult EvaluateCorpse(Mobile from, Corpse c, double skillValue, ref ForensicInfo info)
        {
            if (from.CheckTargetSkill(SkillName.Forensics, c, 25.0, 100.0, new object?[2] { c.Owner, null }))
            {
                info.Killer = c.Killer;
                info.Looters = c.Looters?.Cast<Mobile>().ToList() ?? new List<Mobile>();
                info.TimeOfDeath = c.TimeOfDeath;

                // Add useful messages for both direct FE and Notebook
                if (info.Killer != null)
                    info.AddMessage($"They were killed by {info.Killer.Name}.");

                if (info.Looters.Count > 0)
                {
                    var looterNames = string.Join(", ", info.Looters.Select(l => l.Name));
                    info.AddMessage($"This corpse has been looted by: {looterNames}");
                }
                else
                {
                    info.AddMessage("This corpse has not been looted.");
                }

                return ForensicResult.Success;
            }
            else
            {
                info.AddMessage("You cannot determine anything useful.");
                return ForensicResult.SkillFailure;
            }
        }

        private static ForensicResult EvaluateLockable(Mobile from, ILockpickable p, ref ForensicInfo info)
        {
            info.LockPicker = p.Picker;
            return ForensicResult.Success;
        }

        private static string GetBountyMessage(ForensicInfo info, int level)
        {
            if (!info.HasBounty)
                return "That person is not wanted.";

            return level switch
            {
                1 => "That person is wanted.",
                2 => $"That person is wanted by {info.WhoWants}.",
                3 => $"That person is wanted by {info.WhoWants} for {info.BountyAmount} GP!",
                _ => "You don't recall any bounties out on this person."
            };
        }

        // ===================================================================
        // Legacy Target Handler (keeps original behavior)
        // ===================================================================
        public class ForensicTarget : Target
        {
            public ForensicTarget() : base(10, false, TargetFlags.None) { }

            protected override void OnTarget(Mobile from, object target)
            {
                ForensicResult result = GetForensicInformation(from, target, out ForensicInfo info);

                if (result == ForensicResult.InvalidTarget)
                {
                    from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(1.0).TotalMilliseconds;
                    from.SendMessage(info.Messages.FirstOrDefault() ?? "You have specified an invalid target. Try again.");
                    from.Target = new ForensicTarget();
                    from.RevealingAction();
                    return;
                }

                // Send all discovered messages (this is what players will see)
                foreach (string msg in info.Messages)
                {
                    from.SendMessage(msg);
                }

                // 6/13/2026, Adam: turning off this duplicate messaging
                //// Corpse handling remains the same...
                //if (target is Corpse c && result == ForensicResult.Success)
                //{
                //    if (info.Killer != null)
                //        c.LabelTo(from, "They were killed by {0}", info.Killer.Name);
                //    else
                //        c.LabelTo(from, "They were killed by no one.");

                //    if (info.Looters.Count > 0)
                //    {
                //        StringBuilder sb = new StringBuilder();
                //        foreach (Mobile looter in info.Looters)
                //        {
                //            if (looter != null && looter.AccessLevel <= from.AccessLevel)
                //            {
                //                if (sb.Length > 0) sb.Append(", ");
                //                sb.Append(looter.Name);
                //            }
                //        }
                //        if (sb.Length > 0)
                //            c.LabelTo(from, 1042752, sb.ToString());
                //        else
                //            c.LabelTo(from, 501002);
                //    }
                //    else
                //    {
                //        c.LabelTo(from, 501002);
                //    }
                //}

            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                from.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(1.0).TotalMilliseconds;
            }
        }
    }
}