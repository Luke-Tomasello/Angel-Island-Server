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

/* Items/Construction/Misc/Easle.cs
 * ChangeLog:
 *  11/14/21, Yoar
 *		Inheritance of BaseCraftableItem for carpentry craftables.
 *		Added EasleWithCanvas.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

namespace Server.Items
{
    [Furniture]
    [Flipable(0xF65, 0xF67, 0xF69)]
    public class Easle : BaseCraftableItem
    {
        [Constructable]
        public Easle()
            : base(0xF65)
        {
            Weight = 25.0;
        }

        public Easle(Serial serial)
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

            if (Weight == 10.0)
                Weight = 25.0;
        }
    }

    [Furniture]
    [Flipable(0xF66, 0xF68, 0xF6A)]
    public class EasleWithCanvas : BaseCraftableItem
    {
        [Constructable]
        public EasleWithCanvas()
            : this(0xF66)
        {
        }

        public EasleWithCanvas(int itemId) : base(itemId)
        {
            Weight = 25.0;
        }

        public EasleWithCanvas(Serial serial) : base(serial)
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