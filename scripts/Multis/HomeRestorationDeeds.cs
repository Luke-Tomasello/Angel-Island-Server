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

/* Scripts\Multis\HomeRestorationDeeds.cs
 * CHANGELOG:
 *  2/1/2024, Adam
 *      Initial version.
 */

using Server.Diagnostics;
using Server.Engines.Plants;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Multis.BaseHouse;

namespace Server.Multis
{
    public class HomeItemRestorationDeed : Item
    {
        public enum LockdownType
        {
            None,
            Lockdown,
            Secure,
            Lockbox,
            Addon,
        }
        private Item m_Item;
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public Item Item { get { return m_Item; } set { m_Item = value; } }
        private LockdownType m_Type = LockdownType.None;
        public LockdownType Type { get { return m_Type; } set { m_Type = value; } }
        private BaseHouse m_House;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public BaseHouse House { get => m_House; set => m_House = value; }
        public override string DefaultName { get { return "a home item restoration deed"; } }
        private static bool UseDeedGraphic(Item item)
        { return item is BaseAddon || item is PlantItem; }
        private static bool IsAddon(Item item)
        { return UseDeedGraphic(item); }
        public HomeItemRestorationDeed(BaseHouse house, Item item, LockdownType type)
            // assume the look if the item we are placing unless it's an addon
            : base(UseDeedGraphic(item) ? 0x14F0 : item.ItemID)
        {
            m_House = house;
            m_Item = item;
            m_Type = type;
            // assume the hue of the item we represent
            Hue = UseDeedGraphic(item) ? HouseRestorationDeed.ItemRestorationHue : item.Hue;

            LootType = LootType.Blessed;
            m_Item.MoveToIntStorage();  // stash it away
        }
        public override Item Dupe(int amount)
        {   // no zero param constructor, so an Dupe is required
            Item new_item = Utility.DeepDupe(Item);
            HomeItemRestorationDeed new_deed = new HomeItemRestorationDeed(House, new_item, Type);
            return base.Dupe(new_deed, amount);
        }
        public override void OnSingleClick(Mobile from)
        {
            if (m_House != null && m_House.Sign != null)
            {
                SingleClickSay(from, m_House);
            }
            else if (BaseHouse.GetOwnership(from) == from)
            {
                SingleClickSay(from, BaseHouse.GetHouse(from));
            }
            else
                SingleClickSay(from, null);
        }
        private void SingleClickSay(Mobile from, BaseHouse house)
        {
            try
            {
                if (m_Item != null && !m_Item.Deleted && m_Item is PlantItem plant)
                    LabelTo(from, String.Format("{0} {1} for [{2}] (deed)", plant.PlantHue, Utility.SplitCamelCase(plant.PlantType.ToString()), house.Sign.Name));
                else
                    LabelTo(from, String.Format("{0} for [{1}] (deed)",
                        (m_Item != null && !m_Item.Deleted) ? m_Item.SafeName :
                            "Missing item",
                        (house != null && house.Sign != null && house.Sign.Name != null) ? house.Sign.Name :
                            "your next house"));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            #region Delay Razor from auto-opening things that have a container graphic
            // some/many of our deeds have a graphic that represent the thing they instantiate.
            //  If Razor knows that graphic to be a container, it 'double-clicks' it.
            if (this.ItemID != 0x14F0/*deed graphic*/)
            {
                TimeSpan ts2 = DateTime.UtcNow - LastMoved;
                if (ts2.TotalMilliseconds < 250)
                    return;
            }
            #endregion Delay Razor from auto-opening things that have a container graphic
            try
            {
                if (m_Item != null)
                {
                    if (!IsChildOf(from.Backpack))
                        from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                    else if (m_House != null && m_House.Owner != null && m_House.IsOwner(from))
                    {
                        from.SendMessage("Ready to move, {0}", m_Item.SafeName);
                        MoveItemTarget.BeginMoveItem(m_House, from, this);
                    }
                    else if (BaseHouse.GetOwnership(from) == from)
                    {
                        from.SendMessage("Ready to move, {0}", m_Item.SafeName);
                        MoveItemTarget.BeginMoveItem(m_House ?? BaseHouse.Find(from.Location, from.Map), from, this);
                    }
                    else if (BaseHouse.GetHouse(from) == null)
                        from.SendMessage("You must be in a house you own to place this {0}.", IsAddon(m_Item) ? "addon" : "item");
                    else
                        from.SendMessage("Only home owners may place this {0}.", IsAddon(m_Item) ? "addon" : "item");
                }
                else
                    from.SendMessage("Item missing.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public class MoveItemTarget : Target
        {
            public static void BeginMoveItem(BaseHouse house, Mobile from, HomeItemRestorationDeed package)
            {
                bool IsAddon = package.Item is BaseAddon;
                if (house != null && house.Owner != null && house.IsOwner(from) && house.IsStaffOwned == false)
                {
                    from.SendMessage("Target the spot within your house where you want to place this {0}.", IsAddon ? "addon" : "item");
                    from.Target = new MoveItemTarget(house, package);
                }
                //else if (BaseHouse.GetOwnership(from) == from)
                //{
                //    from.SendMessage("Target the spot within your house where you want to place this {0}.", IsAddon ? "addon" : "item");
                //    from.Target = new MoveItemTarget(house, package);
                //}
                else
                {
                    from.SendMessage("{0} is staff-owned, and as such, it may not be moved in this way.", house);
                    LogHelper logger = new LogHelper("Can't place house item.log", overwrite: false, sline: true);
                    logger.Log(
                        string.Format
                        ("house:{0}, house.Owner:{1}, house.IsOwner(from):{2}, house.IsStaffOwned:{3}",
                        house != null ? house : "-null-",
                        (house != null && house.Owner != null) ? house.Owner : "-null",
                        (house != null && house.Owner != null) ? house.IsOwner(from) : "-??-",
                        (house != null) ? house.IsStaffOwned : "-??-")
                        );
                    logger.Finish();
                }
            }

            private BaseHouse m_House;
            private Item m_Item;
            private HomeItemRestorationDeed m_Package;
            private MoveItemTarget(BaseHouse house, HomeItemRestorationDeed package)
                : base(-1, true, TargetFlags.None)
            {
                m_House = house;
                m_Package = package;
                m_Item = package.Item;
            }
            private static bool ValidTarget(object targeted, out Point3D loc)
            {
                loc = Point3D.Zero;
                IPoint3D p = targeted as IPoint3D;

                if (p == null)
                    return false;

                if (p is Item)
                    p = ((Item)p).GetWorldTop();
                else if (p is Mobile)
                    p = ((Mobile)p).Location;
                loc = new Point3D(p);
                return true;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                bool IsAddon = m_Package.Item is BaseAddon;
                if (!from.CheckAlive() || !(m_House != null && m_House.Owner != null && m_House.IsOwner(from) && m_House.IsStaffOwned == false))
                    return;

                Point3D loc = new Point3D();
                if (BaseHouse.ValidateOwnership(from) && ValidTarget(targeted, out loc))
                {
                    m_Item.MoveToWorld(loc, from.Map);
                    m_Item.IsIntMapStorage = false;
                    if (m_Item is BaseAddon ba)
                    {
                        if (ba.Components != null)
                            foreach (AddonComponent c in ba.Components)
                                c.IsIntMapStorage = false;
                    }
                    else if (m_House != null)
                    {
                        LogHelper logger = new LogHelper("Unpacking House.log", overwrite: false, sline: true);
                        if (m_Package.Type != LockdownType.Secure)
                        {
                            if (m_House.SumLockDownSecureCount < m_House.MaxLockDowns)
                            {
                                m_House.LockDown(from, m_Item); // lock it down
                                logger.Log(LogType.Item, m_Item, string.Format("{0} locked down item", from));
                            }
                            else
                            {
                                from.SendLocalizedMessage(1005377);//You cannot lock that down
                                logger.Log(LogType.Item, m_Item, string.Format("{0} Unable to lock down item", from));
                            }
                        }
                        else
                        {
                            if (m_House.SecureCount < m_House.MaxSecures)
                            {
                                m_House.AddSecure(from, m_Item); // secure it 
                                logger.Log(LogType.Item, m_Item, string.Format("{0} secured item", from));
                            }
                            else
                            {
                                from.SendLocalizedMessage(1005377);//You cannot lock that down
                                logger.Log(LogType.Item, m_Item, string.Format("{0} Unable secure item", from));
                            }
                        }
                        logger.Finish();
                    }
                    //from.Backpack.RemoveItem(m_Package);
                    //m_Package.MoveToIntStorage();
                    m_Package.Delete();
                    //Timer.DelayCall(TimeSpan.FromSeconds(Utility.RandomDouble(.75, 1.5)), new TimerStateCallback(DeleteTick), new object[] { m_Package });
                    from.SendMessage("You have successfully moved the {0}.", IsAddon ? "addon" : "item");
                }
                else
                    from.SendMessage("Only homeowners may place this here.");
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
            }
        }
        public override void OnDelete()
        {
            if (this.Item != null && this.Item.IsIntMapStorage)  // not ever been moved to world, delete it (rare)
            {
                this.Item.IsIntMapStorage = false;
                this.Item.Delete();
            }
            base.OnDelete();
        }
        public HomeItemRestorationDeed(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);

            // version 1
            writer.WriteEncodedInt((int)m_Type);

            // version 0
            writer.Write(m_House);
            writer.Write(m_Item);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Type = (LockdownType)reader.ReadEncodedInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_House = (BaseHouse)reader.ReadItem();
                        m_Item = reader.ReadItem();
                        break;
                    }
            }

        }
    }
    public class HouseNPCRestorationDeed : Item
    {
        private Mobile m_Mobile;
        public Mobile Mobile { get { return m_Mobile; } set { m_Mobile = value; } }
        private BaseHouse m_House;
        public override string DefaultName { get { return "a house NPC restoration deed"; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_House.Owner ?? null; }
        }

        public HouseNPCRestorationDeed(BaseHouse house, Mobile mobile)
            : base(0x14F0)                  // deed graphic
        {
            m_House = house;
            m_Mobile = mobile;
            Hue = HouseRestorationDeed.MobileRestorationHue;
            LootType = LootType.Blessed;
            m_Mobile.MoveToIntStorage();    // stash it away
        }

        public override void OnSingleClick(Mobile from)
        {

            if (m_House != null && m_House.Sign != null)
            {
                SingleClickSay(from, m_House);
            }
            else if (BaseHouse.GetOwnership(from) == from)
            {
                SingleClickSay(from, BaseHouse.GetHouse(from));
            }
            else
                SingleClickSay(from, null);
        }
        private void SingleClickSay(Mobile from, BaseHouse house)
        {
            try
            {
                LabelTo(from, String.Format("{0} for [{1}]",
                    (m_Mobile != null && !m_Mobile.Deleted) ? m_Mobile.SafeName :
                        "Missing mobile",
                    (house != null && house.Owner != null && house.Owner.Name != null) ? house.Owner.Name :
                        "your next house"));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            #region Delay Razor from auto-opening things that have a container graphic
            // some/many of our deeds have a graphic that represent the thing they instantiate.
            //  If Razor knows that graphic to be a container, it 'double-clicks' it.
            if (this.ItemID != 0x14F0/*deed graphic*/)
            {
                TimeSpan ts2 = DateTime.UtcNow - LastMoved;
                if (ts2.TotalMilliseconds < 250)
                    return;
            }
            #endregion Delay Razor from auto-opening things that have a container graphic

            try
            {
                if (!IsChildOf(from.Backpack))
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                else if (m_House != null && !m_House.IsOwner(from))
                    from.SendMessage("Only home owners may place this NPC.");
                else
                {
                    from.SendMessage("Ready to restore, {0}", m_Mobile.SafeName);
                    MoveItemTarget.BeginMoveItem(m_House, from, this);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public class MoveItemTarget : Target
        {
            public static void BeginMoveItem(BaseHouse house, Mobile from, HouseNPCRestorationDeed package)
            {
                if ((house != null && house.Owner != null && house.IsStaffOwned == false))
                {
                    from.SendMessage("Target the spot within your home where you want to place this NPC.");
                    from.Target = new MoveItemTarget(house, package);
                }
                else
                    from.SendMessage("{0} is staff-owned, and as such, it may not be moved in this way.", house);
            }

            private BaseHouse m_House;
            private Mobile m_Mobile;
            private HouseNPCRestorationDeed m_Package;
            private MoveItemTarget(BaseHouse stone, HouseNPCRestorationDeed package)
                : base(-1, true, TargetFlags.None)
            {
                m_House = stone;
                m_Package = package;
                m_Mobile = package.Mobile;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.CheckAlive() || !m_House.IsOwner(from))
                    return;

                Point3D loc = new Point3D(targeted as IPoint3D);
                BaseHouse bh = null;
                if ((bh = BaseHouse.Find(from.Location, from.Map)) != null && bh.Owner == from)
                {
                    m_Mobile.MoveToWorld(loc, from.Map);
                    m_Mobile.IsIntMapStorage = false;
                    m_Package.Delete();
                    from.SendMessage("You have successfully restored this home NPC.");
                }
                else
                    from.SendMessage("You do not own this house.");
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
            }
        }
        public override void OnDelete()
        {
            if (this.Mobile.IsIntMapStorage)  // not ever been moved to world, delete it
            {
                this.Mobile.IsIntMapStorage = false;
                this.Mobile.Delete();
            }
            base.OnDelete();
        }
        public HouseNPCRestorationDeed(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            // version 0
            writer.Write(m_House);
            writer.Write(m_Mobile);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_House = (BaseHouse)reader.ReadItem();
                        m_Mobile = reader.ReadMobile();
                        break;
                    }
            }

        }
    }
    public class HouseRestorationDeed : Item
    {
        public const int MobileRestorationHue = 0x333;     // purple
        public const int ItemRestorationHue = 0x46;        // green
        private BaseHouse m_House;
        private List<MovingCrate> m_MovingCrates;
        private Key m_Key;
        [CommandProperty(AccessLevel.GameMaster)]
        public override string DefaultName { get { return "a house restoration deed"; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public BaseHouse House { get { return m_House; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public Key Key => m_Key;
        [CommandProperty(AccessLevel.GameMaster)]
        public List<MovingCrate> MovingCrates => m_MovingCrates;
        public HouseRestorationDeed(BaseHouse house, List<MovingCrate> movingCrates, Key key)
            : base(0x14F0)
        {
            m_House = house;
            m_MovingCrates = movingCrates;
            Hue = HouseRestorationDeed.ItemRestorationHue;
            LootType = LootType.Blessed;
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_House != null && m_House.Owner != null)
                LabelTo(from, String.Format("a restoration deed for {0} [{1}]", m_House.SafeName, m_House.Owner.Name));
            else if (BaseHouse.GetOwnership(from) == from)
                LabelTo(from, String.Format("a restoration deed for {0} [{1}]", BaseHouse.GetOwnership(from).SafeName, BaseHouse.GetOwnership(from).Name));
            else
                LabelTo(from, String.Format("a house restoration deed"));
        }

        public override void OnDoubleClick(Mobile from)
        {
            try
            {
                if (!IsChildOf(from.Backpack))
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                else if (!BaseHouse.ValidateOwnership(from))
                    from.SendMessage("Only home owner may restore this home.");
                else if (m_House != null && m_House.Owner != null)
                {
                    from.SendMessage("Ready to move, {0}", m_House.SafeName);
                    MoveItemTarget.BeginMoveItem(BaseHouse.Find(from.Location, from.Map), from, this);
                }
                else if (BaseHouse.GetOwnership(from) == from)
                {
                    from.SendMessage("Ready to move, {0}", BaseHouse.GetOwnership(from).SafeName);
                    MoveItemTarget.BeginMoveItem(BaseHouse.Find(from.Location, from.Map), from, this);
                }
                else
                    System.Diagnostics.Debug.Assert(false);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public class MoveItemTarget : Target
        {
            public static void BeginMoveItem(BaseHouse house, Mobile from, HouseRestorationDeed package)
            {
                if ((house != null && house.Owner != null && house.IsStaffOwned == false))
                {
                    from.SendMessage("Target the spot within your house where you want to place the moving crates.");
                    from.Target = new MoveItemTarget(house, package);
                }
                else
                    from.SendMessage("{0} is staff-owned, and as such, it may not be moved in this way.", house);
            }

            private BaseHouse m_House;
            private HouseRestorationDeed m_Package;
            private MoveItemTarget(BaseHouse house, HouseRestorationDeed package)
                : base(-1, true, TargetFlags.None)
            {
                m_House = house;
                m_Package = package;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.CheckAlive() || !BaseHouse.ValidateOwnership(from))
                    return;

                Point3D[] locs = new Point3D[] { new Point3D(targeted as IPoint3D) };
                if (!Utility.CanSpawnLandMobile(from.Map, locs[0]))
                    locs[0] = Utility.GetPointNearby(from.Map, locs[0], max_dist: 2, avoid_doors: true, avoid: locs);   // moving crates
                if (!locs.Contains(Point3D.Zero))
                {
                    foreach (MovingCrate crate in m_Package.MovingCrates)
                    {
                        crate.MoveToWorld(locs[0], from.Map);
                        crate.IsIntMapStorage = false;
                        locs[0].Z += 3; // stacking
                    }
                    // unset this bool as now we will check against the existence of the crates
                    m_House.SetBaseHouseBool(BaseHouseBoolTable.IsPackedUp, false);
                    m_Package.Delete();
                    from.SendMessage("You have successfully restored your property for {0}.", m_House.Sign.Name);
                }
                else
                {
                    from.SendMessage("Unable to restore your property here for {0}.", m_House.Sign.Name);
                    from.SendMessage("You'll need an open tile not near a door to place.");
                }
            }
            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
            }
            private bool NearDoor(Point3D[] locs)
            {
                return false;
            }
        }
        public override void OnDelete()
        {
            if (m_MovingCrates != null)
                foreach (var crate in m_MovingCrates)
                    if (crate.Map == Map.Internal)
                        crate.Delete();
            base.OnDelete();
        }
        public HouseRestorationDeed(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version);

            // version 0
            writer.Write(m_House);
            writer.WriteItemList<MovingCrate>(m_MovingCrates);
            writer.Write(m_Key);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_House = (BaseHouse)reader.ReadItem();
                        m_MovingCrates = reader.ReadStrongItemList<MovingCrate>();
                        m_Key = (Key)reader.ReadItem();
                        break;
                    }
            }
        }
    }
}