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

/* Scripts/Items/Construction/Chairs/Bench.cs
 * ChangeLog
 *  3/23/22, Yoar
 *      Added MarbleBench, SandstoneBench
 *  11/14/21, Yoar
 *		Inheritance of BaseCraftableItem for carpentry craftables.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    [Furniture]
    [Flipable(0xB2D, 0xB2C)]
    public class WoodenBench : BaseCraftableItem
    {
        [Constructable]
        public WoodenBench()
            : base(0xB2D)
        {
            Weight = 6;
        }

        public WoodenBench(Serial serial)
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

    [Furniture]
    [Flipable(0x45A, 0x459)]
    public class MarbleBench : BaseCraftableItem // TODO: Carpentry/masontry craftable?
    {
        [Constructable]
        public MarbleBench()
            : base(0x45A)
        {
            Weight = 6;
        }

        public MarbleBench(Serial serial)
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

    [Furniture]
    [Flipable(0x45C, 0x45B)]
    public class SandstoneBench : BaseCraftableItem // TODO: Carpentry/masontry craftable?
    {
        [Constructable]
        public SandstoneBench()
            : base(0x45C)
        {
            Weight = 6;
        }

        public SandstoneBench(Serial serial)
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