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

/* Items/Deeds/WorkPermits.cs
 * ChangeLog:
 *  9/2/2023, Adam
 *      Ensure the 'friend' is inside the house with IsFriend(from, checkInside: true)
 *      Increase target range from 1 to 5
 *	4/2/08, Adam
 *      first time checkin
 */

using Server.Diagnostics;			// log helper
using Server.Multis;        // HouseSign
using Server.Targeting;
using System;

namespace Server.Items
{
    public abstract class BaseWorkPermit : Item
    {
        public BaseWorkPermit()
            : base(0x14F0)
        {
            Weight = 1.0;
            LootType = LootType.Regular;
        }

        public BaseWorkPermit(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);       // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.Backpack == null || !IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("Please target the house sign of the house to apply permit to.");
                from.Target = new WorkPermitTarget(this); // Call our target
            }
        }
    }

    public class WorkPermitTarget : Target
    {
        private BaseWorkPermit m_Deed;

        public WorkPermitTarget(BaseWorkPermit deed)
            : base(5, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        bool UpgradeCheck(Mobile from, BaseHouse house)
        {
            // see if the upgrade is really an *upgrade*
            //  handle the special case where the user has added taxable lockbox storage below
            if (house.MaximumBarkeepCount >= 255)
            {
                from.SendMessage("Fire regulations prohibit allowing any more barkeeps to work at this residence.");
                return false;
            }

            return true;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is HouseSign && (target as HouseSign).Structure != null)
            {
                HouseSign sign = target as HouseSign;
                LogHelper Logger = null;

                try
                {
                    if (sign.Structure.IsFriend(from, checkInside: true) == false)
                    {
                        from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        return;
                    }
                    else if (UpgradeCheck(from, (target as HouseSign).Structure) == false)
                    {
                        // filters out any oddball cases and askes the user to correct it
                    }
                    else
                    {
                        BaseHouse house = (target as HouseSign).Structure;
                        Logger = new LogHelper("WorkPermit.log", false);
                        Logger.Log(LogType.Item, house, String.Format("WorkPermit applied: {0}", m_Deed.ToString()));
                        house.MaximumBarkeepCount++;
                        from.SendMessage(String.Format("Permit Accepted. You may now employ up to {0} barkeepers.", house.MaximumBarkeepCount));
                        m_Deed.Delete();
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
                finally
                {
                    if (Logger != null)
                        Logger.Finish();
                }
            }
            else
            {
                from.SendMessage("That is not a house sign.");
            }
        }
    }

    public class BarkeepWorkPermit : BaseWorkPermit
    {
        [Constructable]
        public BarkeepWorkPermit()
        {
            Name = "work permit for a barkeep";
        }

        public BarkeepWorkPermit(Serial serial)
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

    }
}