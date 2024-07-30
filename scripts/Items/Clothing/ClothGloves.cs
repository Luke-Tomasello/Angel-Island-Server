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

/* Engines/Crafting/DefTailoring.cs
 * CHANGELOG:
 *  7/18/06 Fixed name bug
 *  7/18/06 Created by Rhiannon.
 */

using Server.Network;
using System.Collections;

namespace Server.Items
{
    public abstract class BaseGloves : BaseClothing
    {
        public BaseGloves(int itemID)
            : this(itemID, 1001)
        {
        }

        public BaseGloves(int itemID, int hue)
            : base(itemID, Layer.Gloves, hue)
        {
        }

        public BaseGloves(Serial serial)
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

    [Flipable]
    public class ClothGloves : BaseGloves
    {
        [Constructable]
        public ClothGloves()
            : this(1001)
        {
        }

        [Constructable]
        public ClothGloves(int hue)
            : base(0x13C6, hue)
        {
            Weight = 1.0;
        }

        public ClothGloves(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            ArrayList attrs = new ArrayList();

            if (Quality == ClothingQuality.Exceptional)
                attrs.Add(new EquipInfoAttribute(1018305 - (int)Quality));

            int number;

            if (Name == null)
            {
                this.LabelTo(from, "cloth gloves");
                number = 1041000;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(number, Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));

            from.Send(new DisplayEquipmentInfo(this, eqInfo));
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