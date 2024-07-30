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

/* Items/Triggers/OfferingBox.cs
 * CHANGELOG:
 *  6/19/2024, Adam
 *      remove the OnDoubleClick processing as it's a container and needs the native 'open'
 * 	3/18/23, Yoar
 * 		Added HandleDragDrop: Automatically trigger the box on item drop.
 * 		Added HideContent: Hides the content display of the container.
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using Server.Commands;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    public class OfferingBox : BaseContainer, ITriggerable, ITrigger
    {
        public const int Limit = 6;

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        private Item m_Link;

        private List<ItemEntry> m_Entries; // note: may contain null values

        private bool m_RequiredItemsOnly;
        private bool m_ConsumeItems;
        private bool m_HandleDragDrop;
        private bool m_HideContent;

        private string m_AcceptMessage;
        private string m_RejectMessage;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        public List<ItemEntry> Entries
        {
            get { return m_Entries; }
            set { m_Entries = value; }
        }

        #region Entry Accessors

        private ItemEntry GetEntry(int index)
        {
            if (index >= 0 && index < m_Entries.Count)
                return m_Entries[index];

            return null;
        }

        private void SetEntry(int index, ItemEntry value)
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
                    ItemEntry e = new ItemEntry();

                    e.Amount = 0;

                    m_Entries[i] = e;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ItemEntry Entry1
        {
            get { return GetEntry(0); }
            set { SetEntry(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ItemEntry Entry2
        {
            get { return GetEntry(1); }
            set { SetEntry(1, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ItemEntry Entry3
        {
            get { return GetEntry(2); }
            set { SetEntry(2, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ItemEntry Entry4
        {
            get { return GetEntry(3); }
            set { SetEntry(3, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ItemEntry Entry5
        {
            get { return GetEntry(4); }
            set { SetEntry(4, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ItemEntry Entry6
        {
            get { return GetEntry(5); }
            set { SetEntry(5, value); }
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RequiredItemsOnly
        {
            get { return m_RequiredItemsOnly; }
            set { m_RequiredItemsOnly = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ConsumeItems
        {
            get { return m_ConsumeItems; }
            set { m_ConsumeItems = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HandleDragDrop
        {
            get { return m_HandleDragDrop; }
            set { m_HandleDragDrop = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HideContent
        {
            get { return m_HideContent; }
            set { m_HideContent = value; InvalidateProperties(); }
        }

        public override bool DisplaysContent
        {
            get { return (m_HideContent ? false : base.DisplaysContent); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string AcceptMessage
        {
            get { return m_AcceptMessage; }
            set { m_AcceptMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RejectMessage
        {
            get { return m_RejectMessage; }
            set { m_RejectMessage = value; }
        }

        [Constructable]
        public OfferingBox()
            : base(0x9A8)
        {
            Movable = false;

            m_Entries = new List<ItemEntry>();

            m_RequiredItemsOnly = true;
            m_ConsumeItems = true;

            m_RejectMessage = "That item is not requested.";

            EnsureEntries();
        }
        // we need standard double click processing here
        //public override void OnDoubleClick(Mobile m)
        //{
        //    if (m.AccessLevel >= AccessLevel.Administrator)
        //    {
        //        m.SendGump(new Server.Gumps.PropertiesGump(m, this));
        //    }
        //}
        public override bool IsAccessibleTo(Mobile m)
        {
            return true;
        }

        public override bool CanStore(Mobile m)
        {
            return true;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!CheckDrop(from, dropped))
                return false;

            if (!base.OnDragDrop(from, dropped))
                return false;

            if (m_AcceptMessage != null)
                from.SendMessage(m_AcceptMessage);

            if (m_HandleDragDrop)
                TriggerSystem.CheckTrigger(from, this);

            return true;
        }

        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            if (!CheckDrop(from, item))
                return false;

            if (!base.OnDragDropInto(from, item, p))
                return false;

            if (m_AcceptMessage != null)
                from.SendMessage(m_AcceptMessage);

            if (m_HandleDragDrop)
                TriggerSystem.CheckTrigger(from, this);

            return true;
        }

        private bool CheckDrop(Mobile from, Item dropped)
        {
            if (m_RequiredItemsOnly)
            {
                bool found = false;

                for (int i = 0; i < m_Entries.Count && !found; i++)
                {
                    ItemEntry e = m_Entries[i];

                    if (e == null || e.Amount <= 0)
                        continue;

                    if (e.CheckCondition(dropped))
                        found = true;
                }

                if (!found)
                {
                    if (m_RejectMessage != null)
                        from.SendMessage(m_RejectMessage);

                    return false;
                }
            }

            return true;
        }

        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return (CheckItems(false) && TriggerSystem.CanTrigger(from, m_Link));
        }

        public void OnTrigger(Mobile from)
        {
            if (CheckItems(m_ConsumeItems))
                TriggerSystem.CheckTrigger(from, m_Link);
        }

        private bool CheckItems(bool consume)
        {
            List<Item>[] allItems = new List<Item>[m_Entries.Count];

            for (int i = 0; i < m_Entries.Count; i++)
            {
                ItemEntry e = m_Entries[i];

                if (e == null || e.Amount <= 0)
                    continue;

                List<Item> curItems = allItems[i];

                int total = 0;

                for (int j = 0; j < Items.Count && total < e.Amount; j++)
                {
                    Item item = Items[j];

                    if (!e.CheckCondition(item))
                        continue;

                    if (curItems == null)
                        curItems = allItems[i] = new List<Item>();

                    curItems.Add(item);

                    total += item.Amount;
                }

                if (total < e.Amount)
                    return false;
            }

            if (!consume)
                return true;

            for (int i = 0; i < m_Entries.Count; i++)
            {
                ItemEntry e = m_Entries[i];

                if (e == null || e.Amount <= 0)
                    continue;

                List<Item> curItems = allItems[i];

                if (curItems == null)
                    continue; // sanity

                int toConsume = e.Amount;

                for (int j = 0; j < curItems.Count && toConsume > 0; j++)
                {
                    int amount = Math.Min(toConsume, curItems[j].Amount);

                    curItems[j].Consume(amount);

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
                list.Add(new TriggerSystem.ActivateCME(((ITriggerable)this).CanTrigger(from)));
                list.Add(new TriggerSystem.LinkCME());
            }
        }

        #region ITriggerable

        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return CanTrigger(from);
        }

        void ITriggerable.OnTrigger(Mobile from)
        {
            OnTrigger(from);
        }

        #endregion

        public OfferingBox(Serial serial)
            : base(serial)
        {
            m_Entries = new List<ItemEntry>();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_Event);

            // version 1
            writer.Write((bool)m_HandleDragDrop);
            writer.Write((bool)m_HideContent);

            writer.Write((Item)m_Link);

            writer.Write((int)m_Entries.Count);

            for (int i = 0; i < m_Entries.Count; i++)
            {
                if (m_Entries[i] != null)
                {
                    writer.Write((bool)true);

                    m_Entries[i].Serialize(writer);
                }
                else
                {
                    writer.Write((bool)false);
                }
            }

            writer.Write((bool)m_RequiredItemsOnly);
            writer.Write((bool)m_ConsumeItems);

            writer.Write((string)m_AcceptMessage);
            writer.Write((string)m_RejectMessage);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Event = reader.ReadString();
                        goto case 1;
                    }
                case 1:
                    {
                        m_HandleDragDrop = reader.ReadBool();
                        m_HideContent = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            if (reader.ReadBool())
                                m_Entries.Add(new ItemEntry(reader));
                            else
                                m_Entries.Add(null);
                        }

                        m_RequiredItemsOnly = reader.ReadBool();
                        m_ConsumeItems = reader.ReadBool();

                        m_AcceptMessage = reader.ReadString();
                        m_RejectMessage = reader.ReadString();

                        break;
                    }
            }

            EnsureEntries();
        }

        [PropertyObject]
        public class ItemEntry
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

            [CommandProperty(AccessLevel.GameMaster)]
            public int Amount
            {
                get { return m_Amount; }
                set { m_Amount = value; }
            }

            public ItemEntry()
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
                writer.Write((int)0); // version

                writer.Write((string)m_ConditionStr);
                writer.Write((int)m_Amount);
            }

            public ItemEntry(GenericReader reader)
            {
                int version = reader.ReadInt();

                Condition = reader.ReadString();
                m_Amount = reader.ReadInt();
            }

            public override string ToString()
            {
                return String.Format("x{0} {1}", m_Amount, m_ConditionStr);
            }
        }
    }
}