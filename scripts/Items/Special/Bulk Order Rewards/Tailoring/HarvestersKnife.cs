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

/* Scripts/Items/Special/Bulk Order Rewards/Tailoring/HarvestersKnife.cs
 * CHANGELOG:
 *  10/3/23, Yoar
 *      Initial commit
 */

namespace Server.Items
{
    [Flipable(0x2D20, 0x2D2C)]
    public class HarvestersKnife : ButcherKnife
    {
        public const int ResourceBonus = 20;

        public static void Scale(ref int value)
        {
            value += (ResourceBonus * value / 100); // round up
        }

        public override string DefaultName { get { return "Harvester's Knife"; } }

        [Constructable]
        public HarvestersKnife()
        {
            Hue = 1191;
        }

        public HarvestersKnife(Serial serial)
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

            reader.ReadInt();
        }
    }
}