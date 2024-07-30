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

/* Items/Triggers/BackpackController.cs
 * CHANGELOG:
 *  5/19/2024, Adam 
 * 		Initial version.
 */

using System.Collections;
using System.Collections.Generic;

namespace Server.Items.Triggers
{
    public class BackpackController : Item, ITrigger
    {
        public static void Initialize()
        {
            EventSink.ContainerAddItem += EventSink_OnContainerAddItem;
        }
        private static List<BackpackController> Registry = new();
        public override string DefaultName { get { return "Backpack Controller"; } }
        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        private Item m_Link;

        private int m_Range;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        [Constructable]
        public BackpackController()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            if (!Registry.Contains(this))
                Registry.Add(this);
        }
        public override void OnDelete()
        {
            if (Registry.Contains(this))
                Registry.Remove(this);

            base.OnDelete();
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }

        private bool CheckRange(Point3D loc)
        {
            return (this.Z >= loc.Z - 8 && this.Z < loc.Z + 16) && Utility.InRange(this.GetWorldLocation(), loc, m_Range);
        }
        private void DoContainerAddItem(Container c)
        {
            if (c == null || c is not Container)
                return;

            Mobile m = c.RootParent as Mobile;
            if (m == null || m.Hidden && m.AccessLevel > AccessLevel.Player)
                return;

            bool doTrigger = CheckRange(m.Location);

            if (doTrigger)
                TriggerSystem.CheckTrigger(m, m_Link);
        }
        private static void EventSink_OnContainerAddItem(ContainerAddItemEventArgs e)
        {
            foreach (BackpackController bc in Registry)
                if (bc != null && !bc.Deleted)
                    bc.DoContainerAddItem(e.Container);
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                list.Add(new TriggerSystem.LinkCME());
        }

        public BackpackController(Serial serial)
            : base(serial)
        {
            if (!Registry.Contains(this))
                Registry.Add(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_Event);

            // version 0
            writer.Write((Item)m_Link);
            writer.Write((int)m_Range);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Event = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();
                        m_Range = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}