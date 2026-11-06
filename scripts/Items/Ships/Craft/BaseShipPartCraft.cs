/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* BaseShipPartCraft.cs
 * CHANGELOG:
 *  5/15/2026, Adam
 */

using Server.Engines.Craft;
using System;

namespace Server.Items
{
    public abstract class BaseShipPartCraft : Item, ICraftable
    {
        protected abstract ShipAssemblyParts ShipPart { get; }
        protected virtual BoatType? RequiredBoatType
        {
            get { return null; }
        }

        public BaseShipPartCraft(int itemID)
            : base(itemID)
        {
            Visible = false;
            Movable = false;
            Weight = 1.0;
        }

        public BaseShipPartCraft(Serial serial)
            : base(serial)
        {
        }

        public virtual int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            ShipPlans? plans = ShipPlans.Find(from, RequiredBoatType, ShipPart);

            if (plans != null)
            {
                plans.AddPart(ShipPart);
                Delete();
                from.SendMessage("You update the ship plans.");
            }

            return quality;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}