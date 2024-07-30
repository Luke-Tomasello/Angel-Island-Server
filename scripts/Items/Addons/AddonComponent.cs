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

/* Scripts/Items/Addons/AddonComponent.cs
 * ChangeLog
 *  11/1/2023, Adam (Addons to go)
 *      When redeedable addons are 'dropped' they are automatically converted to their respective deed.
 *      This, of course, only applies addons that are movable. 
 *      Use Case: For our camps, we have an 'ore cart' addon. As usual, it is immovable.
 *          But if the player kills all guardians, the ore cart is made movable.
 *          When the player drops the ore cart into their backpack, it is automatically converted to deed form.
 *  1/27/22, Yoar
 *      Added the following virtual methods to BaseAddon:
 *      1. GetComponentProperties
 *      2. OnComponentSingleClick
 *      3. OnComponentDoubleClick
 *      These methods are called in AddonComponent.
 *	9/15/10, Adam
 *		Add the new deco flag that allows addons to be moved with the interior decorator tool
 *  7/30/06, Kit
 *		Add Forge Bellows, Rise of the animated forges!
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using System;

namespace Server.Items
{
    [Server.Engines.Craft.Anvil]
    public class AnvilComponent : AddonComponent
    {
        [Constructable]
        public AnvilComponent(int itemID)
            : base(itemID)
        {
        }

        public AnvilComponent(Serial serial)
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

    [Server.Engines.Craft.Forge]
    public class ForgeComponent : AddonComponent
    {
        [Constructable]
        public ForgeComponent(int itemID)
            : base(itemID)
        {
        }

        public ForgeComponent(Serial serial)
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

    [Server.Engines.Craft.Forge]
    public class ForgeBellows : AddonComponent
    {
        private bool locked;

        [Constructable]
        public ForgeBellows(int itemID)
            : base(itemID)
        {
        }

        public ForgeBellows(Serial serial)
            : base(serial)
        {
        }

        public bool Locked
        {
            get { return locked; }
            set { locked = value; }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Locked)
            {
                try
                {
                    from.PlaySound(43);
                    this.ItemID += 3;
                    if (this.ItemID == 6537 || this.ItemID == 6525)
                    {
                        if (((AddonComponent)Addon.Components[1]) != null)
                            ((AddonComponent)Addon.Components[1]).ItemID += 3;
                    }
                    if (this.ItemID == 6549 || this.ItemID == 6561)
                    {
                        if (((AddonComponent)Addon.Components[2]) != null)
                            ((AddonComponent)Addon.Components[2]).ItemID += 3;
                    }

                    Locked = true;

                    new BellowTimer(this).Start();
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("catch I Item at {0}: Send to Zen please : ", this.Location);
                    System.Console.WriteLine("Exception caught in ForgeBellows.DoubleClick: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }


        private class BellowTimer : Timer
        {
            private ForgeBellows m_bellows;

            public BellowTimer(ForgeBellows bellow)
                : base(TimeSpan.FromSeconds(2.0))
            {
                Priority = TimerPriority.TwoFiftyMS;
                m_bellows = bellow;
            }

            protected override void OnTick()
            {
                try
                {
                    m_bellows.ItemID -= 3;
                    if (m_bellows.ItemID == 6534 || m_bellows.ItemID == 6522)
                    {
                        if (((AddonComponent)m_bellows.Addon.Components[1]) != null)
                            ((AddonComponent)m_bellows.Addon.Components[1]).ItemID -= 3;
                    }

                    if (m_bellows.ItemID == 6546 || m_bellows.ItemID == 6558)
                    {
                        if (((AddonComponent)m_bellows.Addon.Components[1]) != null)
                            ((AddonComponent)m_bellows.Addon.Components[2]).ItemID -= 3;
                    }

                    m_bellows.Locked = false;
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("catch I Item at {0}: Send to Zen please: ", m_bellows.Location);
                    System.Console.WriteLine("Exception caught in ForgeBellows.BellowTimer: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
            }
        }
    }

    public class LocalizedAddonComponent : AddonComponent
    {
        private int m_LabelNumber;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Number
        {
            get { return m_LabelNumber; }
            set { m_LabelNumber = value; InvalidateProperties(); }
        }

        public override int LabelNumber { get { return m_LabelNumber; } }

        [Constructable]
        public LocalizedAddonComponent(int itemID, int labelNumber)
            : base(itemID)
        {
            m_LabelNumber = labelNumber;
        }

        public LocalizedAddonComponent(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_LabelNumber);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_LabelNumber = reader.ReadInt();
                        break;
                    }
            }
        }
    }

    public class AddonComponent : Item, IChopable
    {
        private Point3D m_Offset;
        private BaseAddon m_Addon;
        private bool m_Deco;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Deco
        {
            get
            {
                return m_Deco;
            }
            set
            {
                m_Deco = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseAddon Addon
        {
            get
            {
                return m_Addon;
            }
            set
            {
                m_Addon = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Offset
        {
            get
            {
                return m_Offset;
            }
            set
            {
                m_Offset = value;
            }
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get
            {
                return base.Hue;
            }
            set
            {
                base.Hue = value;

                if (m_Addon != null && m_Addon.ShareHue)
                    m_Addon.Hue = value;
            }
        }
        public virtual bool NeedsWall { get { return false; } }
        public virtual Point3D WallPosition { get { return Point3D.Zero; } }

        [Constructable]
        public AddonComponent(int itemID)
            : base(itemID)
        {
            Movable = false;
            ApplyLightTo(this);
        }

        [Constructable]
        public AddonComponent(int itemID, bool deco)
            : this(itemID)
        {
            m_Deco = deco;
        }

        public AddonComponent(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Addon != null)
                m_Addon.GetComponentProperties(this, list);
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Addon != null)
                m_Addon.OnComponentSingleClick(this, from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Addon != null)
                m_Addon.OnComponentDoubleClick(this, from);
        }

        public void OnChop(Mobile from)
        {
            if (m_Addon != null && from.InRange(GetWorldLocation(), 3))
                m_Addon.OnChop(from);
            else
                from.SendLocalizedMessage(500446); // That is too far away.
        }

        public override void OnLocationChange(Point3D old)
        {
            if (m_Addon != null)
                m_Addon.Location = new Point3D(X - m_Offset.X, Y - m_Offset.Y, Z - m_Offset.Z);
        }

        public override void OnMapChange()
        {
            if (m_Addon != null)
                m_Addon.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Addon != null)
                m_Addon.Delete();
        }
        #region Addons TO GO
        public override bool DropToWorld(Mobile from, Point3D p)
        {
            if (m_Addon != null && !m_Addon.Deleted)
                return m_Addon.DropToWorld(from, p);
            return base.DropToWorld(from, p);
        }
        public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
        {
            if (m_Addon != null && !m_Addon.Deleted)
                return m_Addon.DropToMobile(from, target, p);
            return base.DropToMobile(from, target, p);
        }
        public override bool OnDroppedInto(Mobile from, Container target, Point3D p)
        {
            if (m_Addon != null && !m_Addon.Deleted)
                return m_Addon.OnDroppedInto(from, target, p);
            return base.OnDroppedInto(from, target, p);
        }
        #endregion Addons TO GO
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_Deco);

            // version 0
            writer.Write(m_Addon);
            writer.Write(m_Offset);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Deco = reader.ReadBool();
                        goto case 0;
                    }
                //break;
                case 0:
                    {
                        m_Addon = reader.ReadItem() as BaseAddon;
                        m_Offset = reader.ReadPoint3D();

                        if (m_Addon != null)
                            m_Addon.OnComponentLoaded(this);

                        ApplyLightTo(this);

                        break;
                    }
            }
        }

        public static void ApplyLightTo(Item item)
        {
            if ((item.ItemData.Flags & TileFlag.LightSource) == 0)
                return; // not a light source

            int itemID = item.ItemID;

            for (int i = 0; i < m_Entries.Length; ++i)
            {
                LightEntry entry = m_Entries[i];
                int[] toMatch = entry.m_ItemIDs;
                bool contains = false;

                for (int j = 0; !contains && j < toMatch.Length; ++j)
                    contains = (itemID == toMatch[j]);

                if (contains)
                {
                    item.Light = entry.m_Light;
                    return;
                }
            }
        }

        private static LightEntry[] m_Entries = new LightEntry[]
            {
                new LightEntry( LightType.WestSmall, 1122, 1123, 1124, 1141, 1142, 1143, 1144, 1145, 1146, 2347, 2359, 2360, 2361, 2362, 2363, 2364, 2387, 2388, 2389, 2390, 2391, 2392 ),
                new LightEntry( LightType.NorthSmall, 1131, 1133, 1134, 1147, 1148, 1149, 1150, 1151, 1152, 2352, 2373, 2374, 2375, 2376, 2377, 2378, 2401, 2402, 2403, 2404, 2405, 2406 ),
                new LightEntry( LightType.Circle300, 6526, 6538 )
            };

        private class LightEntry
        {
            public LightType m_Light;
            public int[] m_ItemIDs;

            public LightEntry(LightType light, params int[] itemIDs)
            {
                m_Light = light;
                m_ItemIDs = itemIDs;
            }
        }
    }
}