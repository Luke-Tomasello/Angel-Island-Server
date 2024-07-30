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

/* ./Scripts/Mobiles/Animals/Misc/Pig.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 2 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a pig corpse")]
    public class Pig : BaseCreature
    {
        [Constructable]
        public Pig()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a pig";
            Body = 0xCB;
            BaseSoundID = 0xC4;

            SetStr(20);
            SetDex(20);
            SetInt(5);

            SetHits(12);
            SetMana(0);

            SetDamage(2, 4);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 5.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 150;
            Karma = 0;

            VirtualArmor = 12;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 11.1;
        }

        public override int Meat { get { return 1; } }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        private static readonly CarveEntry[] m_RareCarvables = new CarveEntry[]
            {
                new CarveEntry(1, typeof(Item), 0x1E8A), // lard
                new CarveEntry(1, typeof(Item), 0x1E8C, 0x1E8D), // pig's feet
                new CarveEntry(1, typeof(Item), 0x1E8E, 0x1E8F), // pig's head
            };

        public override double RareCarvableChance { get { return 0.015; } }
        public override CarveEntry[] RareCarvables { get { return m_RareCarvables; } }

        public Pig(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}