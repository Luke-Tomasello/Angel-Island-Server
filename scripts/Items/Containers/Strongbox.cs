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

/* Scripts/Items/Containers/Strongbox.cs
 * CHANGELOG:
 *  8/18/2023, Adam
 *      Add a timer to delete the box after 48 hours
 *      Click label now correctly reports 'vendor recovery box'
 *      Box made 'Deco' so new items may be stored in it.
 *  7/7/2023, Adam (VendorRecoveryBox)
 *  When a vendor decays, if it is in a house, and has an owner, all goods and gold are dumped into a VendorRecoveryBox.
 *      The VendorRecoveryBox is like a Strongbox but decays in 2 days.
 *  10/17/21, Adam (Virtual StrongBoxes)
 *      create a new type of virtual strongbox that serves up a (NonLocalStorage) strongbox to the requesting player.
 *      Players cannot tell the difference, looks like a normal strongbox, but the visible (virtual) strongbox fetches
 *      the correct strongbox for the requesting player.
 *	7/7/06, Adam
 *		Move the cleanup routine(Validate()) to Heartbeat like all other standard cleanup
 *	3/12/05: Pixie
 *		Made ownerless StrongBoxes inaccessible to all players.
 */

using Server.Multis;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    [FlipableAttribute(0xe80, 0x9a8)]
    public class StrongBox : BaseContainer, IChopable
    {
        private Mobile m_Owner;
        private BaseHouse m_House;

        public override int DefaultGumpID { get { return 0x4B; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(16, 51, 168, 73); }
        }

        public StrongBox(Mobile owner, BaseHouse house)
            : base(0xE80)
        {
            m_Owner = owner;
            m_House = house;

            MaxItems = 25;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                m_Owner = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseHouse House
        {
            get
            {
                return m_House;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int MaxWeight
        {
            get
            {
                return 0;
            }
        }

        public StrongBox(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Owner);
            writer.Write(m_House);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        m_House = reader.ReadItem() as BaseHouse;

                        break;
                    }
            }
        }

        public override bool Decays
        {
            get
            {
                if (m_House != null && m_Owner != null && !m_Owner.Deleted)
                    return !m_House.IsCoOwner(m_Owner);
                else
                    return true;
            }
        }

        public override TimeSpan DecayTime
        {
            get
            {
                return TimeSpan.FromMinutes(30.0);
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_Owner != null)
                list.Add(1042887, m_Owner.Name); // a strong box owned by ~1_OWNER_NAME~
            else
                base.AddNameProperty(list);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Owner != null)
            {
                LabelTo(from, 1042887, m_Owner.Name); // a strong box owned by ~1_OWNER_NAME~

                if (CheckContentDisplay(from))
                    LabelTo(from, "({0} items, {1} stones)", TotalItems, TotalWeight);
            }
            else
            {
                base.OnSingleClick(from);
            }
        }

        public override bool IsAccessibleTo(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            if (m_Owner == null || m_Owner.Deleted)
            {
                return false;
            }

            if (m_House == null || m_House.Deleted)
            {
                return true;
            }

            return m == m_Owner && m_House.IsCoOwner(m) && base.IsAccessibleTo(m);
        }

        private void Chop(Mobile from)
        {
            Effects.PlaySound(Location, Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.
            Destroy();
        }

        public void OnChop(Mobile from)
        {
            if (m_House != null && !m_House.Deleted && m_Owner != null && !m_Owner.Deleted)
            {
                if (from == m_Owner || m_House.IsOwner(from))
                    Chop(from);
            }
            else
            {
                Chop(from);
            }
        }
    }

    [FlipableAttribute(0xe80, 0x9a8)]
    public class MemberStorage : BaseContainer, IChopable
    {
        private Mobile m_Owner;
        private BaseHouse m_House;

        public override int DefaultGumpID { get { return 0x4B; } }
        public override int DefaultDropSound { get { return 0x42; } }
        public override bool NonLocalStorage { get { return true; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(16, 51, 168, 73); }
        }

        public MemberStorage(Mobile owner, BaseHouse house)
            : base(0xE80)
        {
            m_Owner = owner;
            m_House = house;

            MaxItems = 25;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                m_Owner = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseHouse House
        {
            get
            {
                return m_House;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int MaxWeight
        {
            get
            {
                return 0;
            }
        }

        public MemberStorage(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Owner);
            writer.Write(m_House);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        m_House = reader.ReadItem() as BaseHouse;

                        break;
                    }
            }
        }

        public override bool Decays
        {
            get
            {
                if (m_House != null && m_Owner != null && !m_Owner.Deleted)
                    return !m_House.IsMember(m_Owner);
                else
                    return true;
            }
        }

        public override TimeSpan DecayTime
        {
            get
            {
                return TimeSpan.FromMinutes(30.0);
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (m_Owner != null)
                list.Add(1042887, m_Owner.Name); // a strong box owned by ~1_OWNER_NAME~
            else
                base.AddNameProperty(list);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Owner != null)
            {
                LabelTo(from, 1042887, m_Owner.Name); // a strong box owned by ~1_OWNER_NAME~

                if (CheckContentDisplay(from))
                    LabelTo(from, "({0} items, {1} stones)", TotalItems, TotalWeight);
            }
            else
            {
                base.OnSingleClick(from);
            }
        }

        public override bool IsAccessibleTo(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            if (m_Owner == null || m_Owner.Deleted)
            {
                return false;
            }

            if (m_House == null || m_House.Deleted)
            {
                return true;
            }

            return m == m_Owner && base.IsAccessibleTo(m);
        }

        public override void OnDelete()
        {
            if (m_House != null)
            {   // player may have already been removed from the house membership
                if (m_House.IsMember(m_Owner))
                    m_House.RemoveMember(m_Owner);
            }
            base.OnDelete();
        }
        private void Chop(Mobile from)
        {
            Effects.PlaySound(Location, Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.
            Destroy();
        }

        public void OnChop(Mobile from)
        {
            if (m_House != null && !m_House.Deleted && m_Owner != null && !m_Owner.Deleted)
            {
                if (from == m_Owner || m_House.IsOwner(from))
                    Chop(from);
            }
            else
            {
                Chop(from);
            }
        }
    }

    [DynamicFliping]
    [TinkerTrapable]
    [Flipable(0x9A8, 0xE80)]
    public class MultiUserStrongBox : MetalBox
    {
        #region ListMemberStorage
        public new static void Initialize()
        {
            Server.CommandSystem.Register("ListMemberStorage", AccessLevel.Administrator, new CommandEventHandler(ListMemberStorage_OnCommand));
        }
        [Usage("ListMemberStorage <target MultiUserMemberStorage>")]
        [Description("Lists the MemberStorage associated with the MultiUserMemberStorage.")]
        public static void ListMemberStorage_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the MultiUserMemberStorage to query...");
            e.Mobile.Target = new MUSBListTarget(); // Call our target
        }
        public class MUSBListTarget : Target
        {
            public MUSBListTarget()
                : base(17, true, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is MultiUserStrongBox musb)
                {
                    musb.ListMemberStorage(from);
                }
                else
                    from.SendMessage("That is not a MultiUserMemberStorage container.");
            }
        }
        #endregion ListMemberStorage

        private List<MemberStorage> m_database = new List<MemberStorage>();

        public MemberStorage FindMemberStorage(Mobile m)
        {
            foreach (MemberStorage s in m_database)
                if (s.Owner == m)
                    return s;
            return null;
        }

        public void ListMemberStorage(Mobile m)
        {
            if (m_database.Count == 0)
            {
                m.SendMessage(string.Format("There are no MemberStorage containers for this MultiUserMemberStorage."));
                return;
            }
            foreach (MemberStorage s in m_database)
                m.SendMessage(string.Format("Owner: {0}, House: {1}, MemberStorage {2}{3}", s.Owner, s.House, s, s.Deleted ? ", (Deleted)" : ""));
        }

        public bool AddMemberStorage(Mobile m, BaseHouse h)
        {
            if (FindMemberStorage(m) != null)
                return false;                                   // don't add a box if the player already owns one
            MemberStorage sb = new MemberStorage(m, h);
            m_database.Add(sb);                                 // add the player to the list of owned strongboxes (for this house.)
            sb.Visible = true;                                  // must be Visible to be opened
            sb.Movable = false;                                 // not movable
            sb.MoveToWorld(new Point3D(this.Location.X, this.Location.Y, 0), this.Map); // put it under the house
            return true;
        }
        public bool RemoveMemberStorage(Mobile m, BaseHouse h)
        {
            MemberStorage sb = FindMemberStorage(m);
            if (sb == null)
                return false;                                   // user has no member storage
            m_database.Remove(sb);                              // remove the player from the list of owned strongboxes (for this house.)
            sb.Delete();                                        // delete the storage and all items contained
            return true;
        }

        public override bool TryDropItem(Mobile from, Item dropped, bool sendFullMessage)
        {
            MemberStorage ms = FindMemberStorage(from);
            if (ms == null) return false;
            else return ms.TryDropItem(from, dropped, sendFullMessage);
        }

        public override void OnDelete()
        {
            foreach (MemberStorage s in m_database)
                if (s.Deleted == false)
                    s.Delete();

            m_database.Clear();

            base.OnDelete();
        }
        public override void OnSingleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                from.SendMessage("This MultiUserMemberStorage contains {0} MemberStorage containers", m_database.Count);
            else
            {
                MemberStorage sb = FindMemberStorage(from);
                if (sb != null)
                    sb.OnSingleClick(from);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                ListMemberStorage(from);
            else
            {
                MemberStorage sb = FindMemberStorage(from);
                if (sb != null)
                    sb.OnDoubleClick(from);
            }
        }

        [Constructable]
        public MultiUserStrongBox()
            : base()
        {
            Movable = false;
        }

        public MultiUserStrongBox(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.WriteItemList<MemberStorage>(m_database);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_database = reader.ReadItemList<MemberStorage>();
        }
    }

    [FlipableAttribute(0xe80, 0x9a8)]
    public class VendorRecoveryBox : StrongBox, IChopable
    {
        private DateTime m_deleteTime = DateTime.MinValue;
        public VendorRecoveryBox(Mobile owner, BaseHouse house, int maxItems)
            : base(owner, house)
        {
            MaxItems = maxItems;
            m_deleteTime = DateTime.UtcNow +
#if DEBUG
                TimeSpan.FromMinutes(2.0);
#else
                TimeSpan.FromHours(48.0);
#endif
            Timer.DelayCall(TimeSpan.FromMinutes(1), new TimerStateCallback(CheckDeleteTick), new object[] { null });
        }

        private void CheckDeleteTick(object state)
        {
            if (Deleted) return;

            if (DateTime.UtcNow > m_deleteTime)
            {
                Delete();
                return;
            }
            Timer.DelayCall(TimeSpan.FromMinutes(1), new TimerStateCallback(CheckDeleteTick), new object[] { null });
        }


        public VendorRecoveryBox(Serial serial)
                : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_deleteTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        m_deleteTime = reader.ReadDateTime();
                        goto case 0;
                    }
                case 0:
                    if (version == 0)
                        m_deleteTime = reader.ReadDateTime();
#if false
                        m_deleteTime = DateTime.UtcNow +
#if DEBUG
                TimeSpan.FromMinutes(2.0);
#else
                TimeSpan.FromHours(48.0);
#endif
#endif
                    break;
            }


            Timer.DelayCall(TimeSpan.FromMinutes(1), new TimerStateCallback(CheckDeleteTick), new object[] { null });
        }

        public override bool Decays { get { return true; } }

        public override TimeSpan DecayTime
        {
            get
            {   // not useful unless the box gets made movable somehow (GM perhaps?)
#if DEBUG
                return TimeSpan.FromMinutes(2.0);
#else
                return TimeSpan.FromHours(48.0);
#endif
            }
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (base.Owner != null)
                list.Add(string.Format("a vendor recovery box owned by {0}", base.Owner.Name));
            else
                base.AddNameProperty(list);
        }

        public override void OnSingleClick(Mobile from)
        {
            if (base.Owner != null)
            {
                LabelTo(from, string.Format("a vendor recovery box owned by {0}", base.Owner.Name));

                if (CheckContentDisplay(from))
                    LabelTo(from, "({0} items, {1} stones)", TotalItems, TotalWeight);
            }
            else
            {
                base.OnSingleClick(from);
            }
        }

        public override bool IsAccessibleTo(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
            {
                return true;
            }

            if (base.Owner == null || base.Owner.Deleted)
            {   // maybe we should give access in this case? 
                //  It will decay soon enough, so probably not
                return false;
            }

            if (base.House == null || base.House.Deleted)
            {
                return true;
            }

            return m == base.Owner || base.IsAccessibleTo(m);
        }

        private void Chop(Mobile from)
        {
            Effects.PlaySound(Location, Map, 0x3B3);
            from.SendLocalizedMessage(500461); // You destroy the item.
            Destroy();
        }

        public new void OnChop(Mobile from)
        {
            if (base.House != null && !base.House.Deleted && base.Owner != null && !base.Owner.Deleted)
            {
                if (from == base.Owner || base.House.IsOwner(from))
                    Chop(from);
            }
            else
            {
                Chop(from);
            }
        }
    }
}