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

/* ./Scripts/Mobiles/Animals/Mounts/SavageRidgeback.cs
 *	ChangeLog :
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
*/

namespace Server.Mobiles
{
    [CorpseName("a savage ridgeback corpse")]
    public class SavageRidgeback : BaseMount
    {
        [Constructable]
        public SavageRidgeback()
            : this("a savage ridgeback")
        {
        }
        // base(name, 0xDA, 0x3EA4, AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        [Constructable]
        public SavageRidgeback(string name)
            : base(name,
                  // on siege, we use the ostard graphic
                  Core.RuleSets.SiegeStyleRules() ? 0xDA : 188,
                  Core.RuleSets.SiegeStyleRules() ? 0x3EA4 : 0x3EB8,
                  AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            BaseSoundID = 0x3F3;

            // on siege, we use the ostard graphic and this yellow (0x88A5) hue.
            if (Core.RuleSets.SiegeStyleRules())
                Hue = 0x88A5 | 0x8000;

            SetStr(58, 100);
            SetDex(56, 75);
            SetInt(16, 30);

            SetHits(41, 54);
            SetMana(0);

            SetDamage(3, 5);

            SetSkill(SkillName.MagicResist, 25.3, 40.0);
            SetSkill(SkillName.Tactics, 29.3, 44.0);
            SetSkill(SkillName.Wrestling, 35.1, 45.0);

            Fame = 300;
            Karma = 0;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 83.1;
        }

        public override double GetControlChance(Mobile m)
        {
            return 1.0;
        }

        public override int Meat { get { return 1; } }
        public override int Hides { get { return 12; } }
        public override HideType HideType { get { return HideType.Spined; } }
        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        public SavageRidgeback(Serial serial)
            : base(serial)
        {
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
        }
    }
}