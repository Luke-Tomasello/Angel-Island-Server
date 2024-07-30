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

/* Engines/CoreManagement/VendorManagementConsole.cs
 * ChangeLog
 *  4/29/22, Adam (RestockCharges)
 *      Add a bool to the Vendor Management Console to determine if RestockCharges should apply.
 *	1/19/08, Adam
 *		Created.
 *		Player Vendor Management console for the global values stored in Engines/AngelIsland/CoreAI.cs
 */

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class VendorManagementConsole : Item
    {
        [Constructable]
        public VendorManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 51;
            Name = "Vendor Management Console";
        }

        public VendorManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool RestockCharges
        {
            get
            {
                return CoreAI.RestockCharges;
            }
            set
            {
                CoreAI.RestockCharges = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public int GracePeriod
        {
            get
            {
                return CoreAI.GracePeriod;
            }
            set
            {
                CoreAI.GracePeriod = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int ConnectionFloor
        {
            get
            {
                return CoreAI.ConnectionFloor;
            }
            set
            {
                CoreAI.ConnectionFloor = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double Commission
        {
            get
            {
                return CoreAI.Commission;
            }
            set
            {
                CoreAI.Commission = value;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

}