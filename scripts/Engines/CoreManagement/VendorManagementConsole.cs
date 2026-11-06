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

/* Engines/CoreManagement/VendorManagementConsole.cs
 * ChangeLog
 *  4/29/22, Adam (RestockCharges)
 *      Add a bool to the Vendor Management Console to determine if RestockCharges should apply.
 *	1/19/08, Adam
 *		Created.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    public class VendorManagementConsole : BaseConsole
    {
        [Constructable]
        public VendorManagementConsole()
            : base("Vendor Management Console")
        {
        }

        public VendorManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.Owner)]
        public int BaseVendorFee
        {
            get => CoreAI.BaseVendorFee;
            set
            {
                if (value <= 0)
                    SendMessageNearby("Value may not be zero");
                else
                    CoreAI.BaseVendorFee = value;
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        public int DailySellGoldLimit
        {
            get => CoreAI.DailySellGoldLimit;
            set => CoreAI.DailySellGoldLimit = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool RestockCharges
        {
            get => CoreAI.RestockCharges;
            set => CoreAI.RestockCharges = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int GracePeriod
        {
            get => CoreAI.GracePeriod;
            set => CoreAI.GracePeriod = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public int ConnectionFloor
        {
            get => CoreAI.ConnectionFloor;
            set => CoreAI.ConnectionFloor = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public double Commission
        {
            get => CoreAI.Commission;
            set => CoreAI.Commission = value;
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