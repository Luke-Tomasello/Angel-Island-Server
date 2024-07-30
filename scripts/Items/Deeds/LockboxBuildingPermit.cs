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

/* Items/Deeds/LockboxBuildingPermit.cs
 * ChangeLog:
 *	11/5/21, Yoar
 *	    Redid the data structure of lockboxes in BaseHouse. Instead of storing:
 *	    1. MaxLockboxes    : what is our current lockbox cap, including upgrades?
 *	    2. LockboxLimitMin : what is our base lockbox cap?
 *	    3. LockboxLimitMax : what is our fully upgraded lockbox cap?
 *	    we now store
 *	    1. MaxLockboxes    : what is our base lockbox cap?
 *	    2. BonusLockboxes  : how many *bonus* lockboxes can we place beyond the base lockbox cap?
 *	10/31/21, Yoar
 *	    Lockbox system cleanup. Rewrote target logic.
 *	05/5/07, Adam
 *      first time checkin
 */

using Server.Multis;        // HouseSign
using Server.Targeting;

namespace Server.Items
{
    public class LockboxBuildingPermit : Item // Create the item class which is derived from the base item class
    {
        [Constructable]
        public LockboxBuildingPermit()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "building permit: lockbox";
            LootType = LootType.Regular;
        }

        public LockboxBuildingPermit(Serial serial)
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

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("Please target the house sign of the house to build on.");
                from.Target = new InternalTarget(this);
            }
        }

        private class InternalTarget : Target
        {
            private LockboxBuildingPermit m_Deed;

            public InternalTarget(LockboxBuildingPermit deed)
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
                else if (!sign.Structure.IsOwner(from))
                {
                    from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                }
                else if (sign.Structure.BonusLockboxes >= sign.Structure.MaxBonusLockboxes)
                {
                    from.SendMessage("That house cannot hold more lockboxes.");
                }
                else
                {
                    // add an additional 5 days worth of credit
                    int toAdd = 5 * 24;
                    int maxAdd = BaseHouse.MaxStorageTaxCredits - (int)sign.Structure.StorageTaxCredits;
                    if (toAdd > maxAdd)
                        toAdd = maxAdd;
                    if (toAdd > 0)
                        sign.Structure.StorageTaxCredits += toAdd;

                    sign.Structure.BonusLockboxes++;
                    from.SendMessage("This house now allows {0} lockboxes.", sign.Structure.MaxLockboxes);
                    m_Deed.Delete();
                }
            }
        }
    }
}