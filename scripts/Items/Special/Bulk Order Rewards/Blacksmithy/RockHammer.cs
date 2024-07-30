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

/* Scripts/Items/Skill Items/Harvest Tools/ResourceMap.cs
 * CHANGELOG:
 *  10/24/21, Yoar
 *      Added rock hammers. Mining with a rock hammer increases the chance of finding granite.
 */

using Server.Engines.Harvest;

namespace Server.Items
{
    [FlipableAttribute(0xFB5, 0xFB4)]
    public class RockHammer : BaseHarvestTool
    {
        public static double GraniteChance = 0.33; // OSI: 0.50

        public override HarvestSystem HarvestSystem { get { return Mining.System; } }

        public override double DefaultWeight { get { return 5.0; } }

        [Constructable]
        public RockHammer()
            : this(500)
        {
        }

        [Constructable]
        public RockHammer(int usesRemaining)
            : base(usesRemaining, 0xFB5)
        {
            Hue = 0x973;
            Layer = Layer.OneHanded;
            Name = "a rock hammer";
        }

        public RockHammer(Serial serial)
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