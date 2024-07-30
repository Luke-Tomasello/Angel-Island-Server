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

/* Items/Deeds/StorageTaxCredits.cs
 * ChangeLog:
 *	10/31/21, Yoar
 *	    Lockbox system cleanup. Rewrote target logic.
 *	05/5/07, Adam
 *      first time checkin
 */

using Server.Multis;        // HouseSign
using Server.Targeting;

namespace Server.Items
{
    public class StorageTaxCredits : Item // Create the item class which is derived from the base item class
    {
        private ushort m_Credits;
        public ushort Credits
        {
            get
            {
                return m_Credits;
            }
        }

        [Constructable]
        public StorageTaxCredits()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "tax credits: storage";
            LootType = LootType.Regular;

            // 30 credits: Cost is 1K each and decays at 1 per day
            m_Credits = 30 * 24;
        }

        public StorageTaxCredits(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Credits);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Credits = reader.ReadUShort();
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("Please target the house sign of the house to apply credits to.");
                from.Target = new InternalTarget(this);
            }
        }

        private class InternalTarget : Target
        {
            private StorageTaxCredits m_Deed;

            public InternalTarget(StorageTaxCredits deed)
                : base(2, false, TargetFlags.None)
            {
                m_Deed = deed;
            }

            protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
            {
                HouseSign sign = target as HouseSign;

                if (!m_Deed.IsChildOf(from.Backpack))
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
                else if (sign == null || sign.Structure == null)
                {
                    from.SendMessage("That is not a house sign.");
                }
                else if (!sign.Structure.IsFriend(from))
                {
                    from.SendLocalizedMessage(502094); // You must be in your house to do this.
                }
                else if (sign.Structure.StorageTaxCredits + m_Deed.Credits > BaseHouse.MaxStorageTaxCredits)
                {
                    from.SendMessage("That house cannot hold more credits.");
                }
                else
                {
                    sign.Structure.StorageTaxCredits += m_Deed.Credits;
                    from.SendMessage("Your total storage credits are {0}.", sign.Structure.StorageTaxCredits);
                    m_Deed.Delete();
                }
            }
        }
    }
}