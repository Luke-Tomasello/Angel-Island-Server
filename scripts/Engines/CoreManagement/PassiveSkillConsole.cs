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

/* Engines/CoreManagement/PassiveSkillConsole.cs
 * ChangeLog
 * 12/26/25, Adam
 *  Added Blocking properties for all passive hooks
 *	12/23/25, Adam
 *		Created.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    [NoSort]
    public class PassiveSkillConsole : BaseConsole
    {
        [Constructable]
        public PassiveSkillConsole()
            : base("Passive Skill Console")
        {
        }

        public PassiveSkillConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool PassiveTamingEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.PassiveTamingEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.PassiveTamingEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.PassiveTamingEnabled);
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public bool LogActualGains
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.LogActualGains);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.LogActualGains);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.LogActualGains);
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public bool LogPassiveGains
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.LogPassiveGains);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.LogPassiveGains);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.LogPassiveGains);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool GlobalBlocking
        {
            get => false; // momentary command
            set
            {
                CmdAttackBlock = value;
                CmdComeBlock = value;
                CmdGuardBlock = value;
                CmdFollowBlock = value;
                CmdPatrolBlock = value;
                CmdDefaultBlock = value;

                FeedingBlock = value;
                SkillMeditationBlock = value;
                SkillDefaultBlock = value;
                StatBlock = value;
                HealBlock = value;
                CureBlock = value;
                GaveDamageBlock = value;
            }
        }

        // Command cooldowns + blocks
        [CommandProperty(AccessLevel.GameMaster)]
        public static double CmdAttackCoolMs
        {
            get => CoreAI.CmdAttackCoolMs;
            set => CoreAI.CmdAttackCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool CmdAttackBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CmdAttackBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.CmdAttackBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.CmdAttackBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double CmdComeCoolMs
        {
            get => CoreAI.CmdComeCoolMs;
            set => CoreAI.CmdComeCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool CmdComeBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CmdComeBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.CmdComeBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.CmdComeBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double CmdGuardCoolMs
        {
            get => CoreAI.CmdGuardCoolMs;
            set => CoreAI.CmdGuardCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool CmdGuardBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CmdGuardBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.CmdGuardBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.CmdGuardBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double CmdFollowCoolMs
        {
            get => CoreAI.CmdFollowCoolMs;
            set => CoreAI.CmdFollowCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool CmdFollowBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CmdFollowBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.CmdFollowBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.CmdFollowBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double CmdPatrolCoolMs
        {
            get => CoreAI.CmdPatrolCoolMs;
            set => CoreAI.CmdPatrolCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool CmdPatrolBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CmdPatrolBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.CmdPatrolBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.CmdPatrolBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double CmdDefaultCoolMs
        {
            get => CoreAI.CmdDefaultCoolMs;
            set => CoreAI.CmdDefaultCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool CmdDefaultBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CmdDefaultBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.CmdDefaultBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.CmdDefaultBlock);
            }
        }

        // Passive hooks
        [CommandProperty(AccessLevel.GameMaster)]
        public static double FeedingCoolMs
        {
            get => CoreAI.FeedingCoolMs;
            set => CoreAI.FeedingCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool FeedingBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.FeedingBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.FeedingBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.FeedingBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SkillMeditationCoolMs
        {
            get => CoreAI.SkillMeditationCoolMs;
            set => CoreAI.SkillMeditationCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool SkillMeditationBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SkillMeditationBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SkillMeditationBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.SkillMeditationBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SkillDefaultCoolMs
        {
            get => CoreAI.SkillDefaultCoolMs;
            set => CoreAI.SkillDefaultCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool SkillDefaultBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SkillDefaultBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.SkillDefaultBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.SkillDefaultBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double StatCoolMs
        {
            get => CoreAI.StatCoolMs;
            set => CoreAI.StatCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool StatBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.StatBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.StatBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.StatBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double HealCoolMs
        {
            get => CoreAI.HealCoolMs;
            set => CoreAI.HealCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool HealBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.HealBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.HealBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.HealBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double CureCoolMs
        {
            get => CoreAI.CureCoolMs;
            set => CoreAI.CureCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool CureBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.CureBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.CureBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.CureBlock);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double GaveDamageCoolMs
        {
            get => CoreAI.GaveDamageCoolMs;
            set => CoreAI.GaveDamageCoolMs = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool GaveDamageBlock
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.GaveDamageBlock);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.GaveDamageBlock);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.GaveDamageBlock);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}