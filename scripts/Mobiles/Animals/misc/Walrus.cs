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

/* ./Scripts/Mobiles/Animals/Misc/Walrus.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a walrus corpse")]
    public class Walrus : BaseCreature
    {
        [Constructable]
        public Walrus()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.3, 0.6)
        {
            Name = "a walrus";
            Body = 0xDD;
            BaseSoundID = 0xE0;

            SetStr(21, 29);
            SetDex(46, 55);
            SetInt(16, 20);

            SetHits(14, 17);
            SetMana(0);

            SetDamage(4, 10);

            SetSkill(SkillName.MagicResist, 15.1, 20.0);
            SetSkill(SkillName.Tactics, 19.2, 29.0);
            SetSkill(SkillName.Wrestling, 19.2, 29.0);

            Fame = 150;
            Karma = 0;

            VirtualArmor = 18;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 35.1;
        }

        public override int Meat { get { return 1; } }
        public override int Hides { get { return 12; } }
        public override FoodType FavoriteFood { get { return FoodType.Fish; } }

        public Walrus(Serial serial)
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