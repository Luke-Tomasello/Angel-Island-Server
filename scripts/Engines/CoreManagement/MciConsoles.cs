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

/* scripts\Engines\CoreManagement\EthicConsole.cs
 * CHANGELOG:
 *  1/10/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using Server.Spells;
using Server.Targeting;
using System;

namespace Server.Engines.Consoles
{
    public class DragonSpeedMCi : TuningConsole
    {
        // regular dragon
        private const double c_DragonActiveSpeed = 0.25;        // AI 1.0 default: 0.25
        private const double c_DragonPassiveSpeed = 0.5;        // AI 1.0 default: 0.5
        private static double m_DragonActiveSpeed = c_DragonActiveSpeed;
        private static double m_DragonPassiveSpeed = c_DragonPassiveSpeed;
        [CommandProperty(AccessLevel.Player)]
        public static double DragonActiveSpeed { get { return m_DragonActiveSpeed; } set { m_DragonActiveSpeed = value; } }
        [CommandProperty(AccessLevel.Player)]
        public static double DragonPassiveSpeed { get { return m_DragonPassiveSpeed; } set { m_DragonPassiveSpeed = value; } }

        // regular white wyrm
        private const double c_WyrmActiveSpeed = 0.2;       // AI 1.0 default: 0.2
        private const double c_WyrmPassiveSpeed = 0.4;      // AI 1.0 default: 0.4
        private static double m_WyrmActiveSpeed = c_WyrmActiveSpeed;
        private static double m_WyrmPassiveSpeed = c_WyrmPassiveSpeed;
        [CommandProperty(AccessLevel.Player)]
        public static double WyrmActiveSpeed { get { return m_WyrmActiveSpeed; } set { m_WyrmActiveSpeed = value; } }
        [CommandProperty(AccessLevel.Player)]
        public static double WyrmPassiveSpeed { get { return m_WyrmPassiveSpeed; } set { m_WyrmPassiveSpeed = value; } }

        // regular Mare
        private const double c_MareActiveSpeed = 0.2;       // AI 1.0 default: 0.2
        private const double c_MarePassiveSpeed = 0.4;      // AI 1.0 default: 0.4
        private static double m_MareActiveSpeed = c_MareActiveSpeed;
        private static double m_MarePassiveSpeed = c_MarePassiveSpeed;
        [CommandProperty(AccessLevel.Player)]
        public static double MareActiveSpeed { get { return m_MareActiveSpeed; } set { m_MareActiveSpeed = value; } }
        [CommandProperty(AccessLevel.Player)]
        public static double MarePassiveSpeed { get { return m_MarePassiveSpeed; } set { m_MarePassiveSpeed = value; } }

        public override bool HandlesOnSpeech { get { return true; } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (this.RootParent != e.Mobile)
                return;

            string text = e.Speech.ToLower().Trim();
            if (text == ".displayspeed")
            {
                e.Mobile.SendMessage("Please target the creature.");
                e.Mobile.Target = new SpeedDisplayTarget(this, 0);
            }
        }

        [Constructable]
        public DragonSpeedMCi()
        {
            Hue = 1533;
            Name = "Dragon Speed Management Console";
        }

        public DragonSpeedMCi(Serial serial)
            : base(serial)
        {
        }

        private class SpeedDisplayTarget : Target
        {
            public SpeedDisplayTarget(object ctfc, int SessionId)
                : base(12, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobiles.BaseCreature == false)
                {
                    from.SendMessage("You may only target creatures with this command");
                }
                else
                {
                    from.FlipFlag(DebugFlags.Speed);
                    from.SendMessage(string.Format("The settings is now {0}", from.GetFlag(DebugFlags.Speed)));
                }
            }
        }

        public override void RestoreDefaults()
        {
            m_DragonActiveSpeed = c_DragonActiveSpeed;
            m_DragonPassiveSpeed = c_DragonPassiveSpeed;
            m_WyrmActiveSpeed = c_WyrmActiveSpeed;
            m_WyrmPassiveSpeed = c_WyrmPassiveSpeed;
            m_MareActiveSpeed = c_MareActiveSpeed;
            m_MarePassiveSpeed = c_MarePassiveSpeed;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class SmartSpellMCi : TuningConsole
    {
        public enum SmartSpellLogic { Classic, Debuff, FSInhibitor }
        private const SmartSpellLogic c_UseLogic = SmartSpellLogic.Classic;     // AI 1.0 default: Classic
        private static SmartSpellLogic m_UseLogic = c_UseLogic;
        [CommandProperty(AccessLevel.Player)]
        public static SmartSpellLogic UseLogic { get { return m_UseLogic; } set { m_UseLogic = value; } }

        [Constructable]
        public SmartSpellMCi()
        {
            Hue = 1533;
            Name = "SmartSpell Management Console";
        }

        public SmartSpellMCi(Serial serial)
            : base(serial)
        {
        }

        public override void RestoreDefaults()
        {
            m_UseLogic = c_UseLogic;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class HarmMCi : TuningConsole
    {
        private const bool c_UseNewScale = true;        // AI 1.0 default: false
        private static bool m_UseNewScale = c_UseNewScale;
        [CommandProperty(AccessLevel.Player)]
        public static bool UseNewScale { get { return m_UseNewScale; } set { m_UseNewScale = value; } }

        // adam: new default value: XX min damage.
        private const double c_DamageRangeLow = 1;                      // AI 1.0 default was 1
        private static double m_DamageRangeLow = c_DamageRangeLow;
        [CommandProperty(AccessLevel.Player)]
        public static double DamageRangeLow { get { return m_DamageRangeLow; } set { m_DamageRangeLow = value; } }

        // adam: new default value: XX max damage
        private const double c_DamageRangeHigh = 15;                    // AI 1.0 default was 15
        private static double m_DamageRangeHigh = c_DamageRangeHigh;
        [CommandProperty(AccessLevel.Player)]
        public static double DamageRangeHigh { get { return m_DamageRangeHigh; } set { m_DamageRangeHigh = value; } }

        [Constructable]
        public HarmMCi()
        {
            Hue = 1533;
            Name = "Harm Management Console";
        }

        public HarmMCi(Serial serial)
            : base(serial)
        {
        }

        public override void RestoreDefaults()
        {
            m_UseNewScale = c_UseNewScale;
            m_DamageRangeLow = c_DamageRangeLow;
            m_DamageRangeHigh = c_DamageRangeHigh;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class MagicArrowMCi : TuningConsole
    {
        // adam: new default XX
        private const double c_DamageDelay = 1.2;       // Ai 1.0 was 0.65
        private static double m_DamageDelay = c_DamageDelay;
        [CommandProperty(AccessLevel.Player)]
        public static double DamageDelay { get { return m_DamageDelay; } set { m_DamageDelay = value; } }

        [Constructable]
        public MagicArrowMCi()
        {
            Hue = 1533;
            Name = "Magic Arrow Management Console";
        }

        public MagicArrowMCi(Serial serial)
            : base(serial)
        {
        }

        public override void RestoreDefaults()
        {
            m_DamageDelay = c_DamageDelay;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class MindBlastMCi : TuningConsole
    {
        // adam: new default value: true
        private const bool c_UseNewScale = true;        // AI 1.0 default: false
        private static bool m_UseNewScale = c_UseNewScale;
        [CommandProperty(AccessLevel.Player)]
        public static bool UseNewScale { get { return m_UseNewScale; } set { m_UseNewScale = value; } }

        // adam: new default value: 12 min damage.
        private const double c_DamageRangeLow = 12;
        //_LightningMin + (_LightningMax - _LightningMin) / 2;		// 4th circle lightning, median;
        private static double m_DamageRangeLow = c_DamageRangeLow;
        [CommandProperty(AccessLevel.Player)]
        public static double DamageRangeLow { get { return m_DamageRangeLow; } set { m_DamageRangeLow = value; } }

        // adam: new default value: 30 max damage
        private const double c_DamageRangeHigh = 30;
        //_eBoltMin + (_eBoltMax - _eBoltMin) / 2;					// 6th circle ebolt, median;
        private static double m_DamageRangeHigh = c_DamageRangeHigh;
        [CommandProperty(AccessLevel.Player)]
        public static double DamageRangeHigh { get { return m_DamageRangeHigh; } set { m_DamageRangeHigh = value; } }

        // adam: new default 1.55
        private const double c_DamageDelay = 1.55;      // Ai 1.0 was 0.65
        private static double m_DamageDelay = c_DamageDelay;
        [CommandProperty(AccessLevel.Player)]
        public static double DamageDelay { get { return m_DamageDelay; } set { m_DamageDelay = value; } }

        // the next 4 values are only for reference
        public const int _LightningMin = 9;
        [CommandProperty(AccessLevel.Player)]
        public static int LightningMin { get { return _LightningMin; } }

        public const int _LightningMax = 12;
        [CommandProperty(AccessLevel.Player)]
        public static int LightningMax { get { return _LightningMax; } }

        public const int _eBoltMin = 17;
        [CommandProperty(AccessLevel.Player)]
        public static int eBoltMin { get { return _eBoltMin; } }

        public const int _eBoltMax = 21;
        [CommandProperty(AccessLevel.Player)]
        public static int eBoltMax { get { return _eBoltMax; } }


        [Constructable]
        public MindBlastMCi()
        {
            Hue = 1533;
            Name = "Mind Blast Management Console";
        }

        public MindBlastMCi(Serial serial)
            : base(serial)
        {
        }

        public override void RestoreDefaults()
        {
            m_UseNewScale = c_UseNewScale;
            m_DamageRangeLow = c_DamageRangeLow;
            m_DamageRangeHigh = c_DamageRangeHigh;
            m_DamageDelay = c_DamageDelay;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class PoisonStickMCi : TuningConsole
    {
        private const SpellCircle c_SpellCircle = Spells.SpellCircle.Sixth; // Ai 1.0 was Spells.SpellCircle.Third
        private static SpellCircle m_SpellCircle = c_SpellCircle;
        [CommandProperty(AccessLevel.Player)]
        public static SpellCircle SpellCircle { get { return m_SpellCircle; } set { m_SpellCircle = value; } }

        [Constructable]
        public PoisonStickMCi()
        {
            Hue = 1533;
            Name = "Poison Stick Management Console";
        }

        public PoisonStickMCi(Serial serial)
            : base(serial)
        {
        }

        public override void RestoreDefaults()
        {
            m_SpellCircle = c_SpellCircle;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class StunPunchMCi : TuningConsole
    {
        private const int c_StamCost = 15;
        private static int m_StamCost = c_StamCost;
        [CommandProperty(AccessLevel.Player)]
        public static int StamCost { get { return m_StamCost; } set { m_StamCost = value; } }

        private const bool c_StamCostAlways = true;
        private static bool m_StamCostAlways = c_StamCostAlways;
        [CommandProperty(AccessLevel.Player)]
        public static bool StamCostAlways { get { return m_StamCostAlways; } set { m_StamCostAlways = value; } }

        private const double c_FreezeTime = 3.0;    // AI 1.0 default: 4.0
        private static double m_FreezeTime = c_FreezeTime;
        [CommandProperty(AccessLevel.Player)]
        public static double FreezeTime { get { return m_FreezeTime; } set { m_FreezeTime = value; } }

        [Constructable]
        public StunPunchMCi()
        {
            Hue = 1533;
            Name = "Stun Punch Management Console";
        }

        public StunPunchMCi(Serial serial)
            : base(serial)
        {
        }

        public override void RestoreDefaults()
        {
            m_StamCost = c_StamCost;
            m_StamCostAlways = c_StamCostAlways;
            m_FreezeTime = c_FreezeTime;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }


    [Flipable(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public abstract class TuningConsole : Item
    {
        public static bool Enabled
        {
            get { return Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules(); }
        }

        private Mobile? m_owner = null;                                              // cannot give to another player
        private DateTime m_expiration = DateTime.UtcNow + TimeSpan.FromHours(4.0);     // self destruct in 4 hours

        public TuningConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 1533;
            Name = "Tuning Console";
        }

        public TuningConsole(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            // any access level as long as it's TC
            if (Engines.TestCenterServer.Enabled || from.AccessLevel > AccessLevel.Player)
            {
                if (m_owner == null)
                    m_owner = from;

                if (m_owner != from)
                {
                    from.SendMessage("You are not authorized to use this console.");
                    from.SendMessage("Deleting...");
                    this.Delete();
                    return;
                }

                if (DateTime.UtcNow > m_expiration)
                {
                    from.SendMessage("This console has expired.");
                    from.SendMessage("Deleting...");
                    this.Delete();
                    return;
                }

                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
            else
            {
                from.SendMessage("You cannot use this here.");
                from.SendMessage("Deleting...");
                this.Delete();
                return;
            }
        }

        public abstract void RestoreDefaults();

        public override void OnDelete()
        {
            RestoreDefaults();
            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}