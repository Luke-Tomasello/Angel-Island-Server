/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
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

/* Scripts/Mobiles/Animals/Mounts/SwampDragon.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a swamp dragon corpse")]
    public class SwampDragon : BaseMount
    {
        private bool m_BardingExceptional;
        private Mobile m_BardingCrafter;
        private int m_BardingHP;
        private bool m_HasBarding;
        private CraftResource m_BardingResource;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile BardingCrafter
        {
            get { return m_BardingCrafter; }
            set { m_BardingCrafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BardingExceptional
        {
            get { return m_BardingExceptional; }
            set { m_BardingExceptional = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BardingHP
        {
            get { return m_BardingHP; }
            set { m_BardingHP = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasBarding
        {
            get { return m_HasBarding; }
            set
            {
                m_HasBarding = value;

                if (m_HasBarding)
                {
                    Hue = CraftResources.GetHue(m_BardingResource);
                    BodyValue = 0x31F;
                    ItemID = 0x3EBE;
                }
                else
                {
                    Hue = 0x851;
                    BodyValue = 0x31A;
                    ItemID = 0x3EBD;
                }

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource BardingResource
        {
            get { return m_BardingResource; }
            set
            {
                m_BardingResource = value;

                if (m_HasBarding)
                    Hue = CraftResources.GetHue(value);

                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BardingMaxHP
        {
            get { return m_BardingExceptional ? 2500 : 1000; }
        }

        [Constructable]
        public SwampDragon()
            : this("a swamp dragon")
        {
        }

        [Constructable]
        public SwampDragon(string name)
            : base(name, 0x31A, 0x3EBD, AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            BaseSoundID = 0x16A;

            SetStr(201, 300);
            SetDex(66, 85);
            SetInt(61, 100);

            SetHits(121, 180);

            SetDamage(3, 4);

            SetSkill(SkillName.Anatomy, 45.1, 55.0);
            SetSkill(SkillName.MagicResist, 45.1, 55.0);
            SetSkill(SkillName.Tactics, 45.1, 55.0);
            SetSkill(SkillName.Wrestling, 45.1, 55.0);

            Fame = 2000;
            Karma = -2000;

            Hue = 0x851;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 93.9;
        }

        public override int GetIdleSound()
        {
            return 0x2CE;
        }

        public override int GetDeathSound()
        {
            return 0x2CC;
        }

        public override int GetHurtSound()
        {
            return 0x2D1;
        }

        public override int GetAttackSound()
        {
            return 0x2C8;
        }

        public override double GetControlChance(Mobile m)
        {
            return 1.0;
        }

        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override int Meat { get { return 19; } }
        public override int Hides { get { return 20; } }
        public override int Scales { get { return 5; } }
        public override ScaleType ScaleType { get { return ScaleType.Green; } }

        public SwampDragon(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_HasBarding && m_BardingExceptional && m_BardingCrafter != null)
                list.Add(1060853, m_BardingCrafter.Name); // armor exceptionally crafted by ~1_val~
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_BardingExceptional);
            writer.Write((Mobile)m_BardingCrafter);
            writer.Write((bool)m_HasBarding);
            writer.Write((int)m_BardingHP);
            writer.Write((int)m_BardingResource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_BardingExceptional = reader.ReadBool();
                        m_BardingCrafter = reader.ReadMobile();
                        m_HasBarding = reader.ReadBool();
                        m_BardingHP = reader.ReadInt();
                        m_BardingResource = (CraftResource)reader.ReadInt();
                        break;
                    }
            }

            if (Hue == 0 && !m_HasBarding)
                Hue = 0x851;

            if (BaseSoundID == -1)
                BaseSoundID = 0x16A;
        }
    }
}