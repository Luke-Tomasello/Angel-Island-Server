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

/* Scripts/Mobiles/Special/Christmas/WildHunt/Sleipnir.cs
 * ChangeLog
 *  1/1/24, Yoar
 *		Initial Version.
 */

namespace Server.Mobiles.WildHunt
{
    [CorpseName("a Sleipnir corpse")]
    public class Sleipnir : BaseMount
    {
        [Constructable]
        public Sleipnir()
            : base("Sleipnir", 0x11C, 0x3E92, AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            BaseSoundID = 0xA8;
            Hue = 2101;

            SetStr(800);
            SetDex(200);
            SetInt(100);

            SetHits(1000);

            SetDamage(11, 13);

            SetSkill(SkillName.MagicResist, 80.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 12000;
            Karma = -12000;

            VirtualArmor = 46;
        }

        public override bool HasBreath { get { return true; } } // fire breath enabled
        public override int BreathEffectHue { get { return 1151; } }
        public override int Meat { get { return 5; } }
        public override int Hides { get { return 10; } }
        public override HideType HideType { get { return HideType.Barbed; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat; } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public override void GenerateLoot()
        {
        }

        public Sleipnir(Serial serial)
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