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

/* Scripts/Mobiles/Animals/Town Critters/Bird.cs
 * ChangeLog
 *  9/2/22, Adam
 *  Change the random names from
 *      case 0: Name = "a crow";        ==> case 0: Name = "a sparrow"; break;
 *      case 2: Name = "a magpie";      ==>  case 2: Name = "a finch"; break;
 *      case 1: Name = "a raven";       ==> case 1: Name = "a nuthatch";
 *  We now implement these actual birds.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 2 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Mobiles
{
    [CorpseName("a bird corpse")]
    public class Bird : BaseCreature
    {
        [Constructable]
        public Bird()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {

            Hue = Utility.RandomBirdHue();
            Name = NameList.RandomName("bird");

            Body = 6;
            BaseSoundID = 0x1B;

            VirtualArmor = Utility.RandomMinMax(0, 6);

            SetStr(10);
            SetDex(25, 35);
            SetInt(10);

            SetDamage(0);


            SetSkill(SkillName.Wrestling, 4.2, 6.4);
            SetSkill(SkillName.Tactics, 4.0, 6.0);
            SetSkill(SkillName.MagicResist, 4.0, 5.0);

            SetFameLevel(1);
            SetKarmaLevel(0);

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -6.9;
        }
        public override Characteristics MyCharacteristics { get { return Characteristics.Run | Characteristics.Fly; } }
        public override MeatType MeatType { get { return MeatType.Bird; } }
        public override int Meat { get { return 1; } }
        public override int Feathers { get { return 25; } }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        public Bird(Serial serial)
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

            if (Hue == 0)
                Hue = Utility.RandomBirdHue();
        }
    }

    [CorpseName("a bird corpse")]
    public class TropicalBird : BaseCreature
    {
        [Constructable]
        public TropicalBird()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Hue = Utility.RandomBirdHue();
            Name = "a tropical bird";

            Body = 6;
            BaseSoundID = 0xBF;

            VirtualArmor = Utility.RandomMinMax(0, 6);

            SetStr(10);
            SetDex(25, 35);
            SetInt(10);

            SetDamage(0);


            SetSkill(SkillName.Wrestling, 4.2, 6.4);
            SetSkill(SkillName.Tactics, 4.0, 6.0);
            SetSkill(SkillName.MagicResist, 4.0, 5.0);

            SetFameLevel(1);
            SetKarmaLevel(0);

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -6.9;
        }

        public override MeatType MeatType { get { return MeatType.Bird; } }
        public override int Meat { get { return 1; } }
        public override int Feathers { get { return 25; } }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        public TropicalBird(Serial serial)
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