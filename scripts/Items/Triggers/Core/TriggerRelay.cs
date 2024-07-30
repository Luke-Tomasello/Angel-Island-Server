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

/* Items/Triggers/Core/TriggerRelay.cs
 * CHANGELOG:
 *  2/16/2024, Adam (override Dupe())
 *      Since TriggerRelay exposes it's List of links ('Links') as both Readable and Writable, CopyProperties correctly copies it.
 *      without this, 'duped' TriggerRelays will SHARE a Links list.
 *      Add an item to one, it adds it to the other. Change a value in one, it changes it in the other.
 * 	3/7/23, Yoar
 * 		Initial version, replaces the old TriggerController.
 */

using Server.ContextMenus;
using Server.Targeting;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    [TypeAlias("Server.Items.TriggerController")]
    public class TriggerRelay : Item, ITriggerable
    {
        public const int Limit = 6;

        public override string DefaultName { get { return "Trigger Relay"; } }

        private List<Item> m_Links; // note: may contain null values
        [CopyableAttribute(CopyType.DoNotCopy)]
        public List<Item> Links
        {
            get { return m_Links; }
            set { m_Links = value; }
        }

        #region Link Accessors

        public override Item Dupe(int amount)
        {
            TriggerRelay new_relay = new TriggerRelay();
            Utility.CopyProperties(new_relay, this);
            if (Links != null)
            {   // without this, 'duped' TriggerRelays will SHARE a Links list.
                //  Add an item to one, it adds it to the other. Change a value in one, it changes it in the other.
                new_relay.Links = new(Links);
            }

            return base.Dupe(new_relay, amount);
        }

        private Item GetLink(int index)
        {
            if (index >= 0 && index < m_Links.Count)
                return m_Links[index];

            return null;
        }

        private void SetLink(int index, Item value)
        {
            if (index < 0 || index >= Limit)
                return;

            while (index >= m_Links.Count)
                m_Links.Add(null);

            m_Links[index] = value;
        }

        private void AddLink(Item value)
        {
            int index = 0;

            while (index < m_Links.Count && index < Limit)
            {
                if (m_Links[index] == null || m_Links[index].Deleted)
                {
                    m_Links[index] = value;
                    return;
                }

                index++;
            }

            if (index < Limit)
                SetLink(index, value);
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
        public TriggerRelay()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_Links = new List<Item>();
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

            foreach (Item link in m_Links)
            {
                if (TriggerSystem.CanTrigger(from, link))
                {
                    any = true;
                    break;
                }
            }

            return any;
        }

        public virtual void OnTrigger(Mobile from)
        {
            foreach (Item link in m_Links)
                TriggerSystem.CheckTrigger(from, link);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
                list.Add(new LinkCME());
            }
        }

        private class LinkCME : ContextMenuEntry
        {
            public LinkCME()
                : base(3006173) // Bind
            {
            }

            public override void OnClick()
            {
                BeginTarget(Owner.From, (TriggerRelay)Owner.Target);
            }

            private static void BeginTarget(Mobile from, TriggerRelay link)
            {
                from.BeginTarget(-1, false, TargetFlags.None, new TargetStateCallback(OnTarget), link);
            }

            private static void OnTarget(Mobile from, object targeted, object state)
            {
                TriggerRelay link = (TriggerRelay)state;

                if (link.Links.Count >= Limit)
                    return;

                Item item = targeted as Item;

                if (item == null)
                {
                    from.SendLocalizedMessage(1149667); // Invalid target.
                    return;
                }

                if (item is AddonComponent)
                {
                    AddonComponent ac = (AddonComponent)item;

                    if (ac.Addon != null)
                        item = ac.Addon;
                }

                if (!link.Links.Contains(item))
                {
                    link.AddLink(item);

                    from.SendMessage("Link added.");
                }

                if (link.Links.Count < Limit)
                    BeginTarget(from, link);
            }
        }

        public TriggerRelay(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.WriteItemList(m_Links);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Links = reader.ReadStrongItemList();

                        break;
                    }
                #region Legacy
                case 1:
                    {
                        reader.ReadBool(); // ResetPerMobile

                        goto case 0;
                    }
                case 0:
                    {
                        m_Links = reader.ReadStrongItemList();
                        reader.ReadByte(); // TriggerFlags
                        reader.ReadInt(); // Range
                        new TEList(reader); // Keywords
                        reader.ReadByte(); // ConditionFlags
                        reader.ReadTimeSpan(); // ResetDelay

                        break;
                    }
                    #endregion
            }
        }
    }
}