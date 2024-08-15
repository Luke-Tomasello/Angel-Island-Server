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

/* Items/Containers/KinRansomChest.cs
 * ChangeLog:
 *  4/14/22, Yoar
 *      Removed pointless Name override.
 *	5/23/10, Adam
 *		In CheckLift(), thwart lift macros by checking the per-player 'lift memory'
 *	11/12/08, Adam
 *		- Thwart �fast lifting� in CheckLift: �You thrust your hand into the chest but come up empty handed.�
 *	12/8/07, Pix
 *		Moved check up in PackMagicItem() so we don't create the item if we don't need it
 *			(and thus it's not left on the internal map)
 *  2/1/07, Adam
 *      - Modify CheckItemUse and CheckLift to disallow movment or use of items whilc the chest is locked.
 *      - Issue the user a message if the chest is locked when they try to move/use an item
 *	6/25/06, Adam
 *		Add check for a locked/trapped chest in CheckLift
 *		If the chest us still locked/trapped, do not allow the removal of items.
 *	6/16/06, Adam
 *		- add 25 tubs to 'seed the waters' for leather dye on the shard
 *		- add new constructor to 'fill' the chest (for testing)
 *		- fix constructor to set Name properly based on alignment
 *	6/10/06, Adam
 *		- Add enchanted scrolls
 *	6/8/06, Adam
 *		Initial version
 */

using Server.Engines.DataRecorder;
using Server.Engines.IOBSystem;		// IOB stuffs
using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{
    [FlipableAttribute(0xE41, 0xE40)]
    public class KinRansomChest : LockableContainer
    {
        private IOBAlignment m_IOBAlignment;

        [CommandProperty(AccessLevel.GameMaster)]
        public override IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set
            {
                m_IOBAlignment = value;
                Name = string.Format("Ransom chest of the {0}", IOBSystem.GetIOBName(m_IOBAlignment));
            }
        }

        public override double TrapSensitivity { get { return 1.5; } }

        [Constructable]
        public KinRansomChest()
            : this(IOBAlignment.None, false)
        {
        }

        [Constructable]
        public KinRansomChest(bool bFill)
            : this(IOBAlignment.None, bFill)
        {
        }

        [Constructable]
        public KinRansomChest(IOBAlignment IOBAlignment, bool bFill)
            : base(0xE41)
        {
            this.IOBAlignment = IOBAlignment;
            this.Movable = false;
            this.TrapType = Utility.RandomBool() ? TrapType.PoisonTrap : TrapType.ExplosionTrap;
            this.TrapPower = 5 * 25;    // level 5
            this.TrapLevel = 5;
            this.Locked = true;
            this.RequiredSkill = 100;
            this.LockLevel = this.RequiredSkill - 10;
            this.MaxLockLevel = this.RequiredSkill + 40;

            if (bFill == true)
                KinRansomChest.Fill(this);
        }

        public override int DefaultGumpID { get { return 0x42; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        public static void Fill(LockableContainer cont)
        {
            // Gold, about 100K
            for (int ix = 0; ix < 100; ix++)
                cont.DropItem(new Gold(Utility.RandomMinMax(900, 1100)));

            // plus about 20 chances for magic jewelry and/or clothing
            for (int ix = 0; ix < 20; ix++)
            {
                PackMagicItem(cont, 3, 3, 0.20);
                PackMagicItem(cont, 3, 3, 0.10);
                PackMagicItem(cont, 3, 3, 0.05);
            }

            // drop some scrolls and weapons/armor
            for (int ix = 0; ix < 25; ++ix)
            {
                Item item;
                item = Loot.RandomArmorOrShieldOrWeapon();
                item = Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level5 /*5*/, 0.05, false);
                cont.DropItem(item);
            }

            // drop a few nice maps
            for (int ix = 0; ix < 5; ix++)
            {
                TreasureMap map = new TreasureMap(5, Map.Felucca);
                cont.DropItem(map);
            }

            // drop a few single-color leather dye tubs with 100 charges
            for (int ix = 0; ix < 25; ix++)
            {
                LeatherArmorDyeTub tub = new LeatherArmorDyeTub();
                cont.DropItem(tub);
            }

            // pack some other goodies
            TreasureMapChest.PackRegs(cont, 300);
            TreasureMapChest.PackGems(cont, 300);

        }

        public static void PackMagicItem(LockableContainer cont, int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            Item item = Loot.RandomClothingOrJewelry(must_support_magic: true);

            if (item == null)
                return;

            if (item is BaseClothing)
                ((BaseClothing)item).SetRandomMagicEffect(minLevel, maxLevel);
            else if (item is BaseJewel)
                ((BaseJewel)item).SetRandomMagicEffect(minLevel, maxLevel);

            cont.DropItem(item);
        }

        private ArrayList m_Lifted = new ArrayList();

        public override bool CheckItemUse(Mobile from, Item item)
        {   // get the normal "it is locked" message
            bool bResult = base.CheckItemUse(from, item);

            // if a Player had the chest open when we auto-load it, prevent them from using stuff untill it is opened leagally.
            if (bResult == true && item != this)
                if (from != null && from.AccessLevel == AccessLevel.Player)
                    if (this.Locked == true || this.TrapPower > 0)
                    {
                        from.SendMessage("The chest is locked, so you cannot access that.");
                        bResult = false;
                    }

            return bResult;
        }

        private DateTime lastLift = DateTime.UtcNow;
        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {   // Thwart lift macros
            if (LiftMemory.Recall(from))
            {   // throttle
                from.SendMessage("You thrust your hand into the chest but come up empty handed.");
                reject = LRReason.Inspecific;
                return false;
            }
            else
                LiftMemory.Remember(from, 1.8);

            // get the normal "it is locked" message
            bool bResult = base.CheckLift(from, item, ref reject);

            // if a Player had the chest open when we auto-load it, prevent them from taking stuff untill it is opened leagally.
            if (bResult == true && item != this)
                if (from != null && from.AccessLevel == AccessLevel.Player)
                    if (this.Locked == true || this.TrapPower > 0)
                    {
                        from.SendMessage("The chest is locked, so you cannot access that.");
                        bResult = false;
                    }

            return bResult;
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            bool notYetLifted = !m_Lifted.Contains(item);

            if (notYetLifted)
            {
                m_Lifted.Add(item);

                // we want to reveal the looter 50% of the time?
                double chance = 0.50;
                if ((from.Hidden) && (Utility.RandomDouble() < chance))
                {
                    from.SendMessage("You have been revealed!");
                    from.RevealingAction();
                }

            }

            base.OnItemLifted(from, item);
        }

        private static object[] m_Arguments = new object[1];

        private static void AddItems(Container cont, int[] amounts, Type[] types)
        {
            for (int i = 0; i < amounts.Length && i < types.Length; ++i)
            {
                if (amounts[i] > 0)
                {
                    try
                    {
                        m_Arguments[0] = amounts[i];
                        Item item = (Item)Activator.CreateInstance(types[i], m_Arguments);

                        cont.DropItem(item);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
        }

        public KinRansomChest(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
            writer.WriteItemList(m_Lifted, true);
            writer.Write((int)m_IOBAlignment);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        goto case 0;
                    }
                case 0:
                    {
                        m_Lifted = reader.ReadItemList();
                        m_IOBAlignment = (IOBAlignment)reader.ReadInt();
                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
        }

        public override void OnTelekinesis(Mobile from)
        {
            //Do nothing, telekinesis doesn't work on a TMap.
        }
        public override void LockPick(Mobile from)
        {
            base.LockPick(from);

            // lock picker scores BIG on this one.
            // call data recorder here to attrbute this picker with the 'earned gold' points
            DataRecorder.LockPick(from, this);
        }

        public override bool AutoResetTrap
        {
            /* 5/21/23, Yoar: Kin chest traps always auto-reset
             * Remove trap is necessary to open these chests
             */
            get { return true; }
        }
    }
}