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

/* Items/Deeds/VendorRenegotiationContract.cs
 * ChangeLog:
 *  1/15/00, Adam
 *		Initial Creation
 *		Convert a Player Vendor from (modified)OSI fees to a commission model
 */

using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Items
{
    public class VendorRenegotiationContractTarget : Target // Create our targeting class (which we derive from the base target class)
    {
        private VendorRenegotiationContract m_Deed;

        public VendorRenegotiationContractTarget(VendorRenegotiationContract deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target)
        {

            if (target is PlayerVendor)
            {
                PlayerVendor vendor = (PlayerVendor)target;
                if (vendor.IsOwner(from))
                {
                    if (vendor.PricingModel == PricingModel.Commission)
                    {
                        from.SendMessage("This vendor is already working on commission.");
                    }
                    else
                    {
                        vendor.PricingModel = PricingModel.Commission;
                        vendor.SayTo(from, string.Format("I shall now work for a minimum wage plus a {0}% comission.", ((int)(vendor.Commission * 100)).ToString()));
                        m_Deed.Delete();
                    }
                }

                else
                {
                    vendor.SayTo(from, "I do not work for thee! Only my master may renegotiate my contract.");
                }
            }
            else
            {
                from.SendMessage("Thou canst only renegotiate the contracts of thy own servants.");
            }
        }

    }


    public class VendorRenegotiationContract : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public VendorRenegotiationContract()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "a vendor renegotiation contract";
        }

        public VendorRenegotiationContract(Serial serial)
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

        public override void OnDoubleClick(Mobile from)
        {
            // Make sure deed is in pack
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            // Create target and call it
            from.SendMessage("Whose contract dost thou wish to renegotiate?");
            from.Target = new VendorRenegotiationContractTarget(this);
        }

    }

}