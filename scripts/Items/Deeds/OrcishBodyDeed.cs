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

/* Items/Deeds/OrcishBodyDeed.cs
 * ChangeLog:
 *  2/13/05, Froste
 *      Created as proof of concept for player vendor IOB kin body modifications
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    public class OrcishBodyDeedTarget : Target // Create our targeting class (which we derive from the base target class)
    {
        private OrcishBodyDeed m_Deed;

        public OrcishBodyDeedTarget(OrcishBodyDeed deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is PlayerVendor)
            {
                PlayerVendor vendor = (PlayerVendor)target;
                if (vendor.IsOwner(from))
                {
                    vendor.Body = new Body(17);
                    vendor.Name = NameList.RandomName("orc");
                    m_Deed.Delete();
                }
                else
                {
                    from.SendMessage("That vendor does not work for you.");
                }
            }
            else
            {
                from.SendMessage("That is not a player vendor");
            }
        }
    }

    public class OrcishBodyDeed : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public OrcishBodyDeed()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "an orcish body deed";
        }

        public OrcishBodyDeed(Serial serial)
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

        //public override bool DisplayLootType{ get{ return false; } }

        public override void OnDoubleClick(Mobile from) // Override double click of the deed to call our target
        {

            if (!IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {

                from.SendMessage("Target the vendor you wish to change");
                from.Target = new OrcishBodyDeedTarget(this); // Call our target

            }

        }
    }
}