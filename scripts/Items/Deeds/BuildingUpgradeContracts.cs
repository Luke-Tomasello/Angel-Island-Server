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

/* Items/Deeds/BuildingUpgradeContracts.cs
 * ChangeLog:
 *  1/21/23, Yoar
 *      Added deluxe upgrade contracts. For towers/keeps/castles only.
 *	11/5/21, Yoar
 *	    Redid the data structure of lockboxes in BaseHouse. Instead of storing:
 *	    1. MaxLockboxes    : what is our current lockbox cap, including upgrades?
 *	    2. LockboxLimitMin : what is our base lockbox cap?
 *	    3. LockboxLimitMax : what is our fully upgraded lockbox cap?
 *	    we now store
 *	    1. MaxLockboxes    : what is our base lockbox cap?
 *	    2. BonusLockboxes  : how many *bonus* lockboxes can we place beyond the base lockbox cap?
 *	10/31/21, Yoar
 *	    Lockbox system cleanup.
 *	05/8/07, Adam
 *      first time checkin
 */

using Server.Diagnostics;
using Server.Multis;
using Server.Targeting;
using System;

namespace Server.Items
{
    public abstract class BaseUpgradeContract : Item
    {
        private uint m_LockdownData;

        public BaseUpgradeContract()
            : base(0x14F0)
        {
            Weight = 1.0;
            LootType = LootType.Regular;
        }

        public BaseUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public uint Lockdowns
        {
            get { return Utility.GetUIntRight16(m_LockdownData); }
            set { Utility.SetUIntRight16(ref m_LockdownData, value); }
        }

        public uint Secures
        {
            get { return Utility.GetUIntByte3(m_LockdownData); }
            set { Utility.SetUIntByte3(ref m_LockdownData, value); }
        }

        public uint LockBoxes
        {
            get { return Utility.GetUIntByte4(m_LockdownData); }
            set { Utility.SetUIntByte4(ref m_LockdownData, value); }
        }

        public abstract uint Price { get; }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_LockdownData);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_LockdownData = reader.ReadUInt();
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
                from.SendMessage("Please target the house sign of the house to apply this upgrade to.");
                from.Target = new UpgradeContractTarget(this); // Call our target
            }
        }

        public virtual bool CheckConditions(Mobile from, BaseHouse house)
        {
            return true;
        }
    }

    public class UpgradeContractTarget : Target
    {
        private BaseUpgradeContract m_Deed;

        public UpgradeContractTarget(BaseUpgradeContract deed)
            : base(1, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            HouseSign sign = target as HouseSign;

            if (sign != null && sign.Structure != null && !sign.Structure.Deleted)
            {
                BaseHouse house = sign.Structure;
                LogHelper Logger = null;

                try
                {
                    int addLockDowns = (int)m_Deed.Lockdowns - house.MaxLockDowns;
                    int addSecures = (int)m_Deed.Secures - house.MaxSecures;
                    int addLockboxes = (int)m_Deed.LockBoxes - house.MaxLockboxes;

                    if (house.IsFriend(from) == false)
                    {
                        from.SendLocalizedMessage(502094); // You must be in your house to do this.
                    }
                    else if (addLockDowns <= 0 && addSecures <= 0 && addLockboxes <= 0)
                    {
                        from.SendMessage("This contract cannot be used to upgrade your house any further.");
                    }
                    else if (m_Deed.CheckConditions(from, house))
                    {
                        if (m_Deed.Deleted)
                        {   // exploit! (they passed the deed to someone else while the target cursor was up)
                            Logger = new LogHelper("StorageUpgradeExploit.log", false);
                            Logger.Log(LogType.Mobile, from, string.Format("exploit! they passed the deed to someone else while the target cursor was up"));
                            Logger.Finish();
                            // jail time
                            Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(from as Mobiles.PlayerMobile, 3, "They passed the StorageUpgrade deed to someone else while the target cursor was up.", false);
                            jt.GoToJail();

                            // tell staff
                            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. They passed the StorageUpgrade deed to someone else while the target cursor was up.", from as Mobiles.PlayerMobile));
                            return;
                        }

                        Logger = new LogHelper("StorageUpgrade.log", false);
                        Logger.Log(LogType.Item, house, String.Format("Upgraded with: {0}", m_Deed.ToString()));

                        if (addLockDowns > 0)
                            house.BonusLockDowns += addLockDowns;

                        if (addSecures > 0)
                            house.BonusSecures += addSecures;

                        if (addLockboxes > 0)
                            house.BonusLockboxes += addLockboxes;

                        house.UpgradeCosts += m_Deed.Price;
                        from.SendMessage(String.Format("Upgrade complete with: {0} lockdowns, {1} secures, and {2} lockboxes.", house.MaxLockDowns, house.MaxSecures, house.MaxLockboxes));
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

    /*
	 * Class 1     500     3     3     82,562   - modest 
	 * Class 2     900     6     4     195,750  - moderate 
	 * Class 3     1300    9     5     498,900  - premium 
	 * Class 4     1950    14    7     767,100  - extravagant 
	 */
    public class ModestUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public ModestUpgradeContract()
        {
            Name = "modest storage upgrade contract";
            Lockdowns = 500;
            Secures = 3;
            LockBoxes = 3;
        }

        public ModestUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 82562; } }

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

    public class ModerateUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public ModerateUpgradeContract()
        {
            Name = "moderate storage upgrade contract";
            Lockdowns = 900;
            Secures = 6;
            LockBoxes = 4;
        }

        public ModerateUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 195750; } }

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

    public class PremiumUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public PremiumUpgradeContract()
        {
            Name = "premium storage upgrade contract";
            Lockdowns = 1300;
            Secures = 9;
            LockBoxes = 5;
        }

        public PremiumUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 498900; } }

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

    [TypeAlias("Server.Items.ExtravagantUpgradeContract")]
    public class LavishUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public LavishUpgradeContract()
        {
            Name = "lavish storage upgrade contract";
            Lockdowns = 1950;
            Secures = 14;
            LockBoxes = 7;
        }

        public LavishUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 767100; } }

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

    public class DeluxeUpgradeContract : BaseUpgradeContract
    {
        [Constructable]
        public DeluxeUpgradeContract()
        {
            Name = "deluxe storage upgrade contract";
            Lockdowns = 4076;
            Secures = 28;
            LockBoxes = 7;
        }

        public DeluxeUpgradeContract(Serial serial)
            : base(serial)
        {
        }

        public override uint Price { get { return 1603400; } }

        public override bool CheckConditions(Mobile from, BaseHouse house)
        {
            if (!(house is Tower || house is Keep || house is Castle))
            {
                from.SendMessage("You may only use this contract on a large tower, a keep or a castle.");
                return false;
            }

            return true;
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