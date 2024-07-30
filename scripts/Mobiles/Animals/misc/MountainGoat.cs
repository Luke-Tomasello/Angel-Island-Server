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

/* ./Scripts/Mobiles/Animals/Misc/MountainGoat.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a mountain goat corpse")]
    public class MountainGoat : BaseCreature
    {
        [Constructable]
        public MountainGoat()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a mountain goat";
            Body = 88;
            BaseSoundID = 0x99;

            SetStr(22, 64);
            SetDex(56, 75);
            SetInt(16, 30);

            SetHits(20, 33);
            SetMana(0);

            SetDamage(3, 7);

            SetSkill(SkillName.MagicResist, 25.1, 30.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 29.3, 44.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 10;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;
        }

        public override int Meat { get { return 2; } }
        public override int Hides { get { return 12; } }
        public override FoodType FavoriteFood { get { return FoodType.GrainsAndHay | FoodType.FruitsAndVegies; } }

        private static readonly CarveEntry[] m_RareCarvables = new CarveEntry[]
            {
                new CarveEntry(1, typeof(Item), 0x1E88, 0x1E89), // skinned goat
            };

        public override double RareCarvableChance { get { return 0.015; } }
        public override CarveEntry[] RareCarvables { get { return m_RareCarvables; } }

        public MountainGoat(Serial serial)
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