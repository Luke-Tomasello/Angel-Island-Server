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

/* ./Scripts/Mobiles/Animals/Felines/SnowLeopard.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a leopard corpse")]
    [TypeAlias("Server.Mobiles.Snowleopard")]
    public class SnowLeopard : BaseCreature
    {
        [Constructable]
        public SnowLeopard()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a snow leopard";
            Body = Utility.RandomList(64, 65);
            BaseSoundID = 0x73;

            SetStr(56, 80);
            SetDex(66, 85);
            SetInt(26, 50);

            SetHits(34, 48);
            SetMana(0);

            SetDamage(3, 9);

            SetSkill(SkillName.MagicResist, 25.1, 35.0);
            SetSkill(SkillName.Tactics, 45.1, 60.0);
            SetSkill(SkillName.Wrestling, 40.1, 50.0);

            Fame = 450;
            Karma = 0;

            VirtualArmor = 24;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 53.1;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run; } }
        public override int Meat { get { return 1; } }
        public override int Hides { get { return 8; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat | FoodType.Fish; } }
        public override PackInstinct PackInstinct { get { return PackInstinct.Feline; } }

        public SnowLeopard(Serial serial)
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