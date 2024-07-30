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

namespace Server.Items
{
    public interface IShipwreckedItem
    {
        bool IsShipwreckedItem { get; set; }
    }

    public class ShipwreckedItem : Item, IDyable, IShipwreckedItem
    {
        public ShipwreckedItem(int itemID)
            : base(itemID)
        {
            int weight = this.ItemData.Weight;

            if (weight >= 255)
                weight = 1;

            this.Weight = weight;
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            list.Add(1041645); // recovered from a shipwreck
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (!UseOldNames)
                LabelTo(from, 1041645); // recovered from a shipwreck
        }

        public override string GetOldSuffix()
        {
            return " recovered from a shipwreck";
        }

        public ShipwreckedItem(Serial serial)
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

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            if (ItemID >= 0x13A4 && ItemID <= 0x13AE)
            {
                Hue = sender.DyedHue;
                return true;
            }

            from.SendLocalizedMessage(sender.FailMessage);
            return false;
        }

        #region IShipwreckedItem

        bool IShipwreckedItem.IsShipwreckedItem
        {
            get { return true; }
            set { }
        }

        #endregion
    }
}