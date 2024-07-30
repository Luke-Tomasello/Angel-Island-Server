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

/* Scripts/Mobiles/Animals/Cows/Cow.cs
 * ChangeLog
 *  4/18/23, Yoar
 *      Cows are now milkable.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 2 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using System;

namespace Server.Mobiles
{
    [CorpseName("a cow corpse")]
    public class Cow : BaseCreature
    {
        private DateTime m_MilkedOn;
        private int m_Milk;

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime MilkedOn
        {
            get { return m_MilkedOn; }
            set { m_MilkedOn = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Milk
        {
            get { return m_Milk; }
            set { m_Milk = value; }
        }

        [Constructable]
        public Cow()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a cow";
            Body = Utility.RandomList(0xD8, 0xE7);
            BaseSoundID = 0x78;

            SetStr(30);
            SetDex(15);
            SetInt(5);

            SetHits(18);
            SetMana(0);

            SetDamage(1, 4);

            SetDamage(1, 4);

            SetSkill(SkillName.MagicResist, 5.5);
            SetSkill(SkillName.Tactics, 5.5);
            SetSkill(SkillName.Wrestling, 5.5);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 10;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 11.1;

            if (Core.RuleSets.AOSRules() && Utility.Random(1000) == 0) // 0.1% chance to have mad cows
                FightMode = FightMode.All | FightMode.Closest;
        }

        public override int Meat { get { return 8; } }
        public override int Hides { get { return 12; } }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        public override void OnDoubleClick(Mobile from)
        {
            base.OnDoubleClick(from);

            int random = Utility.Random(100);

            if (random < 5)
                Tip();
            else if (random < 20)
                PlaySound(120);
            else if (random < 40)
                PlaySound(121);
        }

        public void Tip()
        {
            PlaySound(121);
            Animate(8, 0, 3, true, false, 0);
        }

        public bool TryMilk(Mobile from)
        {
            if (!from.InLOS(this) || !from.InRange(Location, 2))
            {
#if false
                from.SendLocalizedMessage(1080400); // You can not milk the cow from this location.
#else
                from.SendMessage("You can not milk the cow from this location.");
#endif
            }
            else if (Controlled && ControlMaster != from)
            {
#if false
                from.SendLocalizedMessage(1071182); // The cow nimbly escapes your attempts to milk it.
#else
                from.SendMessage("The cow nimbly escapes your attempts to milk it.");
#endif
            }
            else if (m_Milk == 0 && m_MilkedOn + TimeSpan.FromDays(1) > DateTime.UtcNow)
            {
#if false
                from.SendLocalizedMessage(1080198); // This cow can not be milked now. Please wait for some time.
#else
                from.SendMessage("This cow can not be milked now. Please wait for some time.");
#endif
            }
            else
            {
                if (m_Milk == 0)
                    m_Milk = 4;

                m_MilkedOn = DateTime.UtcNow;
                m_Milk--;

                return true;
            }

            return false;
        }

        public Cow(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1);

            writer.Write((DateTime)m_MilkedOn);
            writer.Write((int)m_Milk);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_MilkedOn = reader.ReadDateTime();
                        m_Milk = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}