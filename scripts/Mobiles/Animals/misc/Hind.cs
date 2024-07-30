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

/* ./Scripts/Mobiles/Animals/Misc/Hind.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 3 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a deer corpse")]
    public class Hind : BaseCreature
    {
        [Constructable]
        public Hind()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a hind";
            Body = 0xED;

            SetStr(21, 51);
            SetDex(47, 77);
            SetInt(17, 47);

            SetHits(15, 29);
            SetMana(0);

            SetDamage(4);

            SetSkill(SkillName.MagicResist, 15.0);
            SetSkill(SkillName.Tactics, 19.0);
            SetSkill(SkillName.Wrestling, 26.0);

            Fame = 300;
            Karma = 0;

            VirtualArmor = 8;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 23.1;
        }

        public override int Meat { get { return 5; } }
        public override int Hides { get { return 8; } }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        private static readonly CarveEntry[] m_RareCarvables = new CarveEntry[]
            {
                new CarveEntry(1, typeof(Item), 0x1E90, 0x1E91), // skinned deer
            };

        public override double RareCarvableChance { get { return 0.01; } }
        public override CarveEntry[] RareCarvables { get { return m_RareCarvables; } }

        public Hind(Serial serial)
            : base(serial)
        {
        }

        public override int GetAttackSound()
        {
            return 0x82;
        }

        public override int GetHurtSound()
        {
            return 0x83;
        }

        public override int GetDeathSound()
        {
            return 0x84;
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