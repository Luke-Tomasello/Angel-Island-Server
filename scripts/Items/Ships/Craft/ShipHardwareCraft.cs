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

/* ShipHardwareCraft.cs
 * CHANGELOG:
 *  5/15/2026, Adam
 */

namespace Server.Items
{
    public class ShipHardwareCraft : BaseShipPartCraft
    {
        protected override ShipAssemblyParts ShipPart { get { return ShipAssemblyParts.Hardware; } }
        [Constructable]
        public ShipHardwareCraft()
            : base(1)
        {
            Name = "ship hardware";
        }

        public ShipHardwareCraft(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}