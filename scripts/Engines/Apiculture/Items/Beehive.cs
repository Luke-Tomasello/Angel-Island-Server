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

/* scripts\Engines\Apiculture\Items\Beehive.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Network;
using System;
using System.Collections;

namespace Server.Engines.Apiculture
{
    public class Beehive : Item, IChopable, ISecurable
    {
        private BeeColony m_Colony;
        private Item m_BeesItem;
        private BaseAddon m_Addon;

        private SecureLevel m_Level;

        [CommandProperty(AccessLevel.GameMaster)]
        public BeeColony Colony
        {
            get { return m_Colony; }
            set { m_Colony = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item BeesItem
        {
            get { return m_BeesItem; }
            set { m_BeesItem = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseAddon Addon
        {
            get { return m_Addon; }
            set { m_Addon = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_Level; }
            set { m_Level = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Check
        {
            get { return false; }
            set
            {
                if (value)
                    m_Colony.DoGrowth();
            }
        }

        [Constructable]
        public Beehive()
            : base(0x91A)
        {
            Movable = false;

            m_Colony = new BeeColony();
            m_Colony.Hive = this;
            m_Colony.Init();

            m_BeesItem = new InternalItem();

            m_Level = SecureLevel.CoOwners;

            InvalidateHive();

            ApicultureSystem.Hives.Add(this);
        }

        public void InvalidateHive()
        {
            if (m_BeesItem != null)
                m_BeesItem.Visible = (m_Colony.Stage != HiveStage.Empty && m_Colony.Population > 0);
        }

        public void HiveMessage(Mobile from, string format, params object[] args)
        {
            SendMessageTo(from, false, String.Format(format, args));
        }

        public void HiveMessage(Mobile from, int number, string args = "")
        {
            LabelTo(from, number, args);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (CanUse(from, true))
                from.SendGump(new BeehiveGump(from, this));
        }

        public bool CanUse(Mobile from, bool message)
        {
            if (!ApicultureSystem.Enabled)
                return false;

            if (!from.CheckAlive(message))
                return false;

            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
            {
                if (message)
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.

                return false;
            }
            else if (!IsAccessibleTo(from))
            {
                if (message)
                    HiveMessage(from, 502436); // That is not accessible.

                return false;
            }
            else if (m_Colony.Stage == HiveStage.Empty)
            {
                if (message)
                    HiveMessage(from, "That beehive is empty.  Use an axe to redeed it.");

                return false;
            }
            else if (BeeSwarm.UnderEffect(from))
            {
                if (message)
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You can't work the beehive while you are being swarmed!");

                return false;
            }
            else
            {
                return true;
            }
        }

        public override bool IsAccessibleTo(Mobile check)
        {
            if (m_Addon != null)
                return m_Addon.IsAccessibleTo(check);

            return base.IsAccessibleTo(check);
        }

        public override void OnLocationChange(Point3D oldLoc)
        {
            if (m_BeesItem != null)
                m_BeesItem.Location += (Location - oldLoc);
        }

        public override void OnMapChange()
        {
            if (m_BeesItem != null)
                m_BeesItem.Map = Map;
        }

        public override void OnAfterDelete()
        {
            if (m_BeesItem != null)
                m_BeesItem.Delete();

            ApicultureSystem.Hives.Remove(this);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            SetSecureLevelEntry.AddTo(from, this, list);
        }

        #region IChopable

        public void OnChop(Mobile from)
        {
            if (m_Addon != null)
            {
                if (!from.InRange(GetWorldLocation(), 3))
                    from.SendLocalizedMessage(500446); // That is too far away.
                else
                    m_Addon.OnChop(from);
            }
        }

        #endregion

        public Beehive(Serial serial)
            : base(serial)
        {
            m_Colony = new BeeColony();
            m_Colony.Hive = this;

            ApicultureSystem.Hives.Add(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_Level);

            m_Colony.Serialize(writer);
            writer.Write((Item)m_BeesItem);
            writer.Write((Item)m_Addon);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Colony.Deserialize(reader);
                        m_BeesItem = reader.ReadItem();
                        m_Addon = reader.ReadItem() as BaseAddon;

                        break;
                    }
            }

            if (version < 1)
                m_Level = SecureLevel.CoOwners;
        }

        public class InternalItem : Item
        {
            [Constructable]
            public InternalItem()
                : base(0x91B)
            {
                Movable = false;
            }

            public InternalItem(Serial serial)
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
}