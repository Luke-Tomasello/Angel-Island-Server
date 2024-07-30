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

/* Scripts/Engines/ResourcePool/PaymentCheck.cs
 * ChangeLog
 *	03/02/05 Taran Kain
 *		Created.
 */

using Server.Items;

namespace Server.Engines.ResourcePool
{
    /// <summary>
    /// Summary description for PaymentCheck.
    /// </summary>
    public class PaymentCheck : BankCheck
    {
        public override void OnSingleClick(Mobile from)
        {
            from.Send(new Server.Network.AsciiMessage(Serial, ItemID, Server.Network.MessageType.Label, 0x3B2, 3, "", "For the sale of commodities: " + this.Worth)); // A bank check:
        }

        public PaymentCheck(int amount)
            : base(amount)
        {
        }

        public PaymentCheck(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
}