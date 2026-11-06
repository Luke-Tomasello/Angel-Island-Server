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

/* Scripts/Mobiles/Animals/Mounts/FireSteed.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  10/15/04, Jade
 *      Added the missing VirtualArmor value.
 *	8/9/04, Adam
 *		Reduce Ash drop from 100-250, to 100-150, to 10-20
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/28/04, mith
 *		Changed minimum taming requirement to 98.0
 *		Changed hue to 1644, 0x66C.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a fire steed corpse")]
    public class FireSteed : BaseMount
    {
        [Constructable]
        public FireSteed()
            : this("a fire steed")
        {
        }

        [Constructable]
        public FireSteed(string name)
            : base(name, 0xBE, 0x3E9E, AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            BaseSoundID = 0xA8;

            SetStr(376, 400);
            SetDex(91, 120);
            SetInt(291, 300);

            SetHits(226, 240);

            SetDamage(11, 30);

            SetSkill(SkillName.MagicResist, 100.0, 120.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 20000;
            Karma = -20000;

            //Add VirtualArmor value
            VirtualArmor = 35;

            Tamable = true;
            ControlSlots = 2;
            Hue = 0x66C;
            MinTameSkill = 98.0;
        }

        public override bool HasBreath { get { return true; } } // fire breath enabled
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override PackInstinct PackInstinct { get { return PackInstinct.Daemon | PackInstinct.Equine; } }
        public override int Meat { get { return 19; } }
        public override MeatType MeatType
        {
            get { return MeatType.Ribs; }
        }

        public FireSteed(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=172&utm_source=chatgpt.com
            // 150-300 Sulfurous Ash, 15-30 Rubies
            // Carved: 19 Raw Ribs
            if (Spawning)
            {

            }
            else
            {
                PackItem(new SulfurousAsh(Utility.RandomMinMax(150, 300)));
                PackItem(new Ruby(Utility.RandomMinMax(15, 30)));
            }
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

            if (BaseSoundID <= 0)
                BaseSoundID = 0xA8;
        }
    }
}