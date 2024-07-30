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
    public abstract class BaseEquipableLight : BaseLight
    {
        [Constructable]
        public BaseEquipableLight(int itemID)
            : base(itemID)
        {
            Layer = Layer.TwoHanded;
        }

        public BaseEquipableLight(Serial serial)
            : base(serial)
        {
        }

        public override void Ignite()
        {
            if (!(Parent is Mobile) && RootParent is Mobile)
            {
                Mobile holder = (Mobile)RootParent;

                if (holder.EquipItem(this))
                {
                    if (this is Candle)
                        holder.SendLocalizedMessage(502969); // You put the candle in your left hand.
                    else if (this is Torch)
                        holder.SendLocalizedMessage(502971); // You put the torch in your left hand.

                    base.Ignite();
                }
                else
                {
                    holder.SendLocalizedMessage(502449); // You cannot hold this item.
                }
            }
            else
            {
                base.Ignite();
            }
        }

        public override void OnAdded(object parent)
        {
            if (Burning && parent is Container)
                Douse();

            base.OnAdded(parent);
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