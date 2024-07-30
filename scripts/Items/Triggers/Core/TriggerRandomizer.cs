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

/* Items/Triggers/Core/TriggerRandomizer.cs
 * CHANGELOG:
 *  4/5/2024, Adam (Refactor)
 *      Freeze Dry Backup and Restore require standard data types like items, mobiles, as linked-to-types.
 *      TriggerRandomizer introduced a new PropertyObject, incompatible with this system.
 *      Refactor: do away with the custom PropertyObject and revert to using simple Items.
 *  3/25/2024, Adam (shard crasher fix)
 *      Allocate a m_Links table in Deserialize()
 * 	3/22/24, Yoar
 * 		Initial version.
 */

using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    public class TriggerRandomizer : Item, ITriggerable
    {
        public const int Limit = 6;

        public override string DefaultName { get { return "Trigger Randomizer"; } }

        private List<Item> m_Links; // note: may contain null values

        [CopyableAttribute(CopyType.DoNotCopy)]
        public List<Item> Links
        {
            get { return m_Links; }
            set { m_Links = value; }
        }

        #region Entry Accessors

        private Item GetLink(int index)
        {
            if (index >= 0 && index < m_Links.Count)
                return m_Links[index];

            return null;
        }

        private void SetLink(int index, Item e)
        {
            if (index < 0 || index >= Limit)
                return;

            while (index >= m_Links.Count)
                m_Links.Add(null);

            m_Links[index] = e;
        }

        private void EnsureEntries()
        {
            while (m_Links.Count < Limit)
                m_Links.Add(null);

            //for (int i = 0; i < m_Links.Count && i < Limit; i++)
            //{
            //    if (m_Links[i] == null)
            //        m_Links[i] = new Item();
            //}
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link1
        {
            get { return GetLink(0); }
            set { SetLink(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link2
        {
            get { return GetLink(1); }
            set { SetLink(1, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link3
        {
            get { return GetLink(2); }
            set { SetLink(2, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link4
        {
            get { return GetLink(3); }
            set { SetLink(3, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link5
        {
            get { return GetLink(4); }
            set { SetLink(4, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link6
        {
            get { return GetLink(5); }
            set { SetLink(5, value); }
        }

        #endregion

        [Constructable]
        public TriggerRandomizer()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Links = new List<Item>();

            EnsureEntries();
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public virtual bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            if (m_Links.Count == 0)
                return false;

            bool any = false;

            for (int i = 0; i < m_Links.Count; i++)
            {
                if (TriggerSystem.CanTrigger(from, m_Links[i]))
                {
                    any = true;
                    break;
                }
            }

            return any;
        }

        public virtual void OnTrigger(Mobile from)
        {
            TriggerSystem.CheckTrigger(from, Randomize(from));
        }

        private Item Randomize(Mobile from)
        {
            List<Item> list = new List<Item>();
            for (int i = 0; i < m_Links.Count; i++)
                if (m_Links[i] != null)
                    list.Add(m_Links[i]);

            Utility.Shuffle(list);  // randomness

            foreach (Item item in list)
                if (TriggerSystem.CanTrigger(from, item))
                    return item;

#if false
            int total = 0;

            for (int i = 0; i < m_Links.Count; i++)
            {
                Item e = m_Links[i];

                if (e.Weight > 0 && TriggerSystem.CanTrigger(from, e))
                    total += e.Weight;
            }

            if (total == 0)
                return null;

            int rnd = Utility.Random(total);

            for (int i = 0; i < m_Links.Count; i++)
            {
                Item e = m_Links[i];

                if (e.Weight > 0 && TriggerSystem.CanTrigger(from, e.Link))
                {
                    if (rnd < e.Weight)
                        return e.Link;
                    else
                        rnd -= e.Weight;
                }
            }
#endif
            return null;
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
        }

        // ensure we deep-copy the link list
        public override Item Dupe(int amount)
        {
            TriggerRandomizer duped = new TriggerRandomizer();

            Utility.CopyProperties(duped, this);

            duped.Links = new List<Item>();

            foreach (Item e in m_Links)
                duped.Links.Add(e);

            return base.Dupe(duped, amount);
        }

        public TriggerRandomizer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.WriteItemList(m_Links);

            //writer.Write((int)m_Links.Count);

            //for (int i = 0; i < m_Links.Count; i++)
            //    m_Links[i].Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Links = new List<Item>();

            switch (version)
            {
                case 1:
                    {
                        m_Links = reader.ReadStrongItemList();
                        break;
                    }
                case 0:
                    {
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            /*m_Weight = */
                            reader.ReadInt();
                            m_Links.Add(reader.ReadItem());
                        }

                        break;
                    }
            }

            EnsureEntries();
        }
#if false
        [PropertyObject]
        public class Item
        {
            private int m_Weight;
            private Item m_Link;

            [CommandProperty(AccessLevel.GameMaster)]
            public int Weight
            {
                get { return m_Weight; }
                set { m_Weight = value; }
            }

            [CommandProperty(AccessLevel.GameMaster)]
            public Item Link
            {
                get { return m_Link; }
                set { m_Link = value; }
            }

            public Item()
                : this(1, null)
            {
            }

            public Item(int weight, Item link)
            {
                m_Weight = weight;
                m_Link = link;
            }

            public void Serialize(GenericWriter writer)
            {
                writer.Write((int)m_Weight);
                writer.Write((Item)m_Link);
            }

            public Item(GenericReader reader, int version)
            {
                m_Weight = reader.ReadInt();
                m_Link = reader.ReadItem();
            }

            public override string ToString()
            {
                return String.Format("x{0} {1}", m_Weight, m_Link);
            }
        }
#endif
    }
}