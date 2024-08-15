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

/* Items/Triggers/TollController.cs
 * CHANGELOG:
 * 	2/22/24, Yoar
 * 		Initial version.
 */

using Server.Commands;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    public class TollController : Item, ITriggerable, ITrigger
    {
        public const int Limit = 6;

        public override string DefaultName { get { return "Toll Controller"; } }
        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        private Item m_Link;
        private Item m_Else;

        private List<TollEntry> m_Entries; // note: may contain null values

        private bool m_ConsumeItems;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Else
        {
            get { return m_Else; }
            set { m_Else = value; }
        }

        private IEntity m_Source;
        [CopyableAttribute(CopyType.Copy)]  // CopyProperties will copy this one and not the DoNotCopy ones
        public IEntity Source { get { return m_Source; } set { m_Source = value; } }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile TargetMobile
        {
            get { return m_Source as Mobile; }
            set { m_Source = value; }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner TargetSpawner
        {
            get { return m_Source as Spawner; }
            set { m_Source = value; }
        }

        private int m_Range;
        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        public List<TollEntry> Entries
        {
            get { return m_Entries; }
            set { m_Entries = value; }
        }

        #region Entry Accessors

        private TollEntry GetEntry(int index)
        {
            if (index >= 0 && index < m_Entries.Count)
                return m_Entries[index];

            return null;
        }

        private void SetEntry(int index, TollEntry value)
        {
            if (index < 0 || index >= Limit)
                return;

            while (index >= m_Entries.Count)
                m_Entries.Add(null);

            m_Entries[index] = value;
        }

        private void EnsureEntries()
        {
            while (m_Entries.Count < Limit)
                m_Entries.Add(null);

            for (int i = 0; i < m_Entries.Count && i < Limit; i++)
            {
                if (m_Entries[i] == null)
                {
                    TollEntry e = new TollEntry();

                    e.Amount = 0;

                    m_Entries[i] = e;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TollEntry Entry1
        {
            get { return GetEntry(0); }
            set { SetEntry(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TollEntry Entry2
        {
            get { return GetEntry(1); }
            set { SetEntry(1, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TollEntry Entry3
        {
            get { return GetEntry(2); }
            set { SetEntry(2, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TollEntry Entry4
        {
            get { return GetEntry(3); }
            set { SetEntry(3, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TollEntry Entry5
        {
            get { return GetEntry(4); }
            set { SetEntry(4, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TollEntry Entry6
        {
            get { return GetEntry(5); }
            set { SetEntry(5, value); }
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ConsumeItems
        {
            get { return m_ConsumeItems; }
            set { m_ConsumeItems = value; }
        }

        [Constructable]
        public TollController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Entries = new List<TollEntry>();

            m_ConsumeItems = true;

            if (!Registry.ContainsKey(this))
                Registry.Add(this, null);

            EnsureEntries();
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return TriggerSystem.CanTrigger(from, CheckItems(from, false) ? m_Link : m_Else);
        }

        public void OnTrigger(Mobile from)
        {
            TriggerSystem.OnTrigger(from, CheckItems(from, m_ConsumeItems) ? m_Link : m_Else);
        }

        private bool CheckItems(Mobile from, bool consume)
        {
            Container pack = from.Backpack;

            if (pack == null)
                return false;

            List<Item>[] allItems = new List<Item>[m_Entries.Count];

            for (int i = 0; i < m_Entries.Count; i++)
            {
                TollEntry e = m_Entries[i];

                if (e == null || e.ItemType == null)
                    continue;

                List<Item> items = allItems[i];

                int total = 0;

                List<Item> WhereToSearch = new List<Item>();
                if (Registry.ContainsKey(this) && Registry[this] != null && Registry[this].Count > 0)
                    WhereToSearch = new List<Item>(Registry[this]);
                else
                    WhereToSearch = new List<Item>(pack.FindItemsByType(e.ItemType));

                foreach (Item item in WhereToSearch)
                {
                    if (!e.CheckCondition(item))
                        continue;

                    if (items == null)
                        items = allItems[i] = new List<Item>();

                    items.Add(item);

                    total += item.Amount;

                    if (total >= e.Amount)
                        break;
                }

                if (total < e.Amount)
                    return false;
            }

            if (!consume)
                return true;

            for (int i = 0; i < m_Entries.Count; i++)
            {
                TollEntry e = m_Entries[i];

                if (e == null)
                    continue;

                List<Item> items = allItems[i];

                if (items == null)
                    continue; // sanity

                int toConsume = e.Amount;

                for (int j = 0; j < items.Count && toConsume > 0; j++)
                {
                    int amount = Math.Min(toConsume, items[j].Amount);

                    items[j].Consume(amount);

                    toConsume -= amount;
                }
            }

            return true;
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        public TollController(Serial serial)
            : base(serial)
        {
            m_Entries = new List<TollEntry>();

            if (!Registry.ContainsKey(this))
                Registry.Add(this, null);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // v2
            if (m_Source is Item)
                writer.Write(m_Source as Item);
            else
                writer.Write(m_Source as Mobile);
            writer.Write(m_Range);

            // version 1
            writer.Write(m_Event);

            // version 0
            writer.Write((Item)m_Link);
            writer.Write((Item)m_Else);

            writer.Write((int)m_Entries.Count);

            for (int i = 0; i < m_Entries.Count; i++)
                m_Entries[i].Serialize(writer);

            writer.Write((bool)m_ConsumeItems);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Source = ReadEntity(reader);
                        m_Range = reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Event = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();
                        m_Else = reader.ReadItem();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                            m_Entries.Add(new TollEntry(reader, version));

                        m_ConsumeItems = reader.ReadBool();

                        break;
                    }
            }

            EnsureEntries();
        }
        private IEntity ReadEntity(GenericReader reader)
        {
            return World.FindEntity(reader.ReadInt());
        }
        public override void OnDelete()
        {
            if (Registry.ContainsKey(this))
                Registry.Remove(this);

            base.OnDelete();
        }
        private static Dictionary<TollController, List<Item>> Registry = new();
        public static void Initialize()
        {
            EventSink.OnDragDrop += EventSink_OnDragDrop;
        }
        private bool CheckRange(Point3D loc)
        {
            return (this.Z >= loc.Z - 8 && this.Z < loc.Z + 16) && Utility.InRange(this.GetWorldLocation(), loc, m_Range);
        }
        private void DoOnDragDrop(Mobile from, Mobile to)
        {
            if (from == null || to == null)
                return;

            if (from == null || from.Hidden && from.AccessLevel > AccessLevel.Player)
                return;

            if (to == TargetMobile || IsOnSpawner(to))
            {

                bool doTrigger = CheckRange(from.Location) && CheckRange(to.Location);

                if (doTrigger)
                    TriggerSystem.CheckTrigger(from, this);
            }
        }
        private bool IsOnSpawner(Mobile from)
        {
            if (from != null)
                if (TargetSpawner != null && TargetSpawner.Objects != null)
                    if (TargetSpawner.Objects.Contains(from))
                        return true;

            return false;
        }
        private static void EventSink_OnDragDrop(OnDragDropEventArgs e)
        {
            foreach (var tc in Registry)
                if (tc.Key != null && !tc.Key.Deleted)
                {
                    Registry[tc.Key] = new List<Item>() { e.Item };
                    tc.Key.DoOnDragDrop(e.From, e.To);
                    Registry[tc.Key] = null;
                }
        }

        [PropertyObject]
        public class TollEntry
        {
            private string m_ConditionStr;
            private ObjectConditional m_ConditionImpl;
            private int m_Amount;

            [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
            public string Condition
            {
                get { return m_ConditionStr; }
                set
                {
                    if (value != null)
                    {
                        try
                        {
                            string[] args = CommandSystem.Split(value);
                            m_ConditionImpl = ObjectConditional.Parse(null, ref args);
                            m_ConditionStr = value;
                        }
                        catch
                        {
                            m_ConditionImpl = null;
                            m_ConditionStr = null;
                        }
                    }
                    else
                    {
                        m_ConditionImpl = null;
                        m_ConditionStr = null;
                    }
                }
            }

            public Type ItemType
            {
                get { return (m_ConditionImpl != null ? m_ConditionImpl.Type : null); }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public int Amount
            {
                get { return m_Amount; }
                set { m_Amount = value; }
            }

            public TollEntry()
            {
                m_Amount = 1;
            }

            public bool CheckCondition(Item item)
            {
                if (m_ConditionImpl == null)
                    return false;

                bool okay;

                try
                {
                    okay = m_ConditionImpl.CheckCondition(item);
                }
                catch
                {
                    okay = false;
                }

                return okay;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((string)m_ConditionStr);
                writer.Write((int)m_Amount);
            }

            public TollEntry(GenericReader reader, int version)
            {
                Condition = reader.ReadString();
                m_Amount = reader.ReadInt();
            }

            public override string ToString()
            {
                return string.Format("x{0} {1}", m_Amount, m_ConditionStr);
            }
        }
    }
}