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

/* Scripts/Engines/Plants/MiscMobiles/GiantIceWorm.cs
 *	ChangeLog :
 *  10/19/2023, Adam
 *      Since I hate the neon hue of this creature, and because I don't to disappoint the players that found it, (unintentionally in game,)
 *      I morph the hue to a nice mottled hue when the worm in not in the snow. This makes everyone happy since we won't have to see these
 *      crazy looking creatures in town or in the wild - unless rarely in the snow.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
*/

using Server.Targeting;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a giant ice worm corpse")]
    public class GiantIceWorm : BaseCreature
    {
        public override bool SubdueBeforeTame { get { return true; } }

        [Constructable]
        public GiantIceWorm()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Body = 89;
            Name = "a giant ice worm";
            BaseSoundID = 0xDC;

            SetStr(216, 245);
            SetDex(76, 100);
            SetInt(66, 85);

            SetHits(130, 147);

            SetDamage(7, 17);

            SetSkill(SkillName.Poisoning, 75.1, 95.0);
            SetSkill(SkillName.MagicResist, 45.1, 60.0);
            SetSkill(SkillName.Tactics, 75.1, 80.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            Fame = 4500;
            Karma = -4500;

            VirtualArmor = 40;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 71.1;
        }

        private static List<int> SnowTiles = new List<int>()
        {
            0x10C, 0x10F,
            0x114, 0x117,
            0x119, 0x11D,
            0x179, 0x18A,
            0x385, 0x38C,
            0x391, 0x394,
            0x39D, 0x3A4,
            0x3A9, 0x3AC,
            0x5BF, 0x5D6,
            0x5DF, 0x5E2,
            0x745, 0x748,
            0x751, 0x758,
            0x75D, 0x760,
            0x76D, 0x773
        };
        protected override void OnLocationChange(Point3D oldLocation)
        {
            int hue = 0xB8F;    // I'm not in the snow, so I assume a mottled hue
            LandTarget land = new LandTarget(this.Location, this.Map);
            if (land != null)
            {
                int tileID = land.TileID & 0x3FFF;
                bool contains = false;

                for (int i = 0; !contains && i < SnowTiles.Count; i += 2)
                    contains = (tileID >= SnowTiles[i] && tileID <= SnowTiles[i + 1]);

                if (contains)
                    hue = 0;
            }

            Hue = hue;

            base.OnLocationChange(oldLocation);
        }
        public override Poison PoisonImmune { get { return Poison.Greater; } }

        public override Poison HitPoison { get { return Poison.Greater; } }

        public override FoodType FavoriteFood { get { return FoodType.Meat; } }

        public GiantIceWorm(Serial serial)
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