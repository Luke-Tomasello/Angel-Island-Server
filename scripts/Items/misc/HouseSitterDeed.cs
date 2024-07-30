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

/* Items/Misc/HouseSitterDeed.cs
 * CHANGELOG:
 *  11/12/04 - Jade
 *      Change spelling to make housesitter one word.
 *	11/7/04 - Pix
 *		Changed to be Regular loottype.
 *	11/6/04 - Pix
 *		Initial Version
 */

using Server.Mobiles;
using Server.Multis;

namespace Server.Items
{
    public class HouseSitterDeed : Item
    {
        [Constructable]
        public HouseSitterDeed()
            : base(0x14F0)
        {
            Weight = 1.0;
            LootType = LootType.Regular;
            Name = "a housesitter contract";//Jade: change spelling to housesitter.
        }

        public HouseSitterDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); //version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house == null)
                {
                    from.SendLocalizedMessage(503240);//Vendors can only be placed in houses.	
                }
                else if (!house.IsFriend(from) && (from.AccessLevel < AccessLevel.GameMaster))
                {
                    from.SendLocalizedMessage(503242); //You must ask the owner of this house to make you a friend in order to place this vendor here,
                }
                else if (!house.CanPlaceNewVendor())
                {
                    from.SendLocalizedMessage(503241); // You cannot place this vendor or barkeep.  Make sure the house is public or a shop and has sufficient storage available.
                }
                else
                {
                    Mobile v = new HouseSitter(from);
                    v.Direction = from.Direction & Direction.Mask;
                    v.MoveToWorld(from.Location, from.Map);

                    ((HouseSitter)v).SendStatusTo(from);

                    this.Delete();
                }
            }
        }
    }
}