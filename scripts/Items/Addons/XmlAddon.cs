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

/* Scripts/Items/Addons/XmlAddon.cs
 * CHANGELOG:
 *  4/9/2024, Adam (Save)
 *      When saving, backup any existing XML with the same name
 *  4/4/2024, Adam (Timed load of components)
 *      For more than a year we've seen periodic client crashes when loading very large addons.
 *      I've moved the loading to a timer which stops the crash. My theory is that we were overwhelming the client with packets.
 *  3/24/22, Adam (Clear)
 *      Add clear command to clear the cache.
 *      if you are editing the XML, you will need to clear the cache before you try to reload.
 *      Support for 'items' in Picker mode. (Currently statics and items are mutually exclusive.)
 *  3/23/22, Adam ([XmlAddon)
 *      Added a new 'Picker' mode. 
 *      It is very difficult to capture trees in a crowded area.
 *      Picker mode allows you to select the trunk of the tree, and the algo simply create a 2x2 rect around the tree, then passes this rect off to the main algo.
 *  3/23/22, Adam (GetData/GetFileName)
 *      Added a new version that takes the folder name so that we can load certain XML from specific locations.
 *      I.e., XMLTrees wil come from Data\xmlTrees or some such
 *  12/5/21, Yoar
 *        Additional flags are now written as an attribute on the Component element.
 *        Added support to save Item.Light on addon components.
 *  12/4/21, Yoar
 *        Added support to save Item.Hue/Item.Name on addon components.
 *  11/26/21, Yoar
 *        Initial Version.
 */

using Server.Targeting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Items
{
    public static class XmlAddonSystem
    {
        public static string Folder = Path.Combine(Core.DataDirectory, "XmlAddon");

        private static string GetFileName(string id)
        {
            return GetFileName(Folder, id);
        }

        private static string GetFileName(string folder, string id)
        {
            return Path.Combine(folder, String.Format("{0}.xml", id));
        }

        private static readonly Dictionary<string, AddonData> m_Cache = new Dictionary<string, AddonData>();

        private static bool m_UseCaching = true;

        private static bool UseCaching { get { return m_UseCaching; } set { m_UseCaching = value; } }

        public static AddonData GetData(string id)
        {
            return GetData(Folder, id);
        }

        public static AddonData GetData(string folder, string id)
        {
            if (id == null)
                return null;

            AddonData data;

            if (m_Cache.TryGetValue(id, out data))
                return data;

            data = Load(GetFileName(folder, id));

            if (data != null && UseCaching)
                m_Cache[id] = data;

            return data;
        }

        public static void Initialize()
        {
            CommandSystem.Register("XmlAddon", AccessLevel.Seer, new CommandEventHandler(XmlAddon_OnCommand));
        }

        [Usage("XmlAddon <id> <key-value-pairs: name, items, statics, origin, sort, picker, caching, folder, flash>")]
        [Description("Exports the bounded items/statics as addon to XML.")]
        public static void XmlAddon_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0)
            {
                e.Mobile.SendMessage("Usage: XmlAddon <id> <key-value-pairs: name, items, statics, origin, sort, picker, caching, folder, flash>");
                return;
            }

            ExportState state = new ExportState();

            state.ID = e.Arguments[0];
            state.Name = state.ID;
            state.Items = true;
            state.Statics = false;
            state.Origin = AddonOrigin.NorthWest;
            state.Sort = true;
            state.Picker = false;
            state.Offset = 2;
            state.Caching = true;
            state.Flash = false;

            for (int i = 1; i < e.Length - 1; i += 2)
            {
                string key = e.Arguments[i];
                string val = e.Arguments[i + 1];

                switch (key.ToLower())
                {
                    case "name":
                        {
                            state.Name = val;
                            break;
                        }
                    case "items":
                        {
                            state.Items = Utility.ToBoolean(val);
                            break;
                        }
                    case "statics":
                        {
                            state.Statics = Utility.ToBoolean(val);
                            break;
                        }
                    case "origin":
                        {
                            AddonOrigin result;
                            if (Enum.TryParse(val, ignoreCase: true, out result))
                                state.Origin = result;
                            break;
                        }
                    case "sort":
                        {
                            state.Sort = Utility.ToBoolean(val);
                            break;
                        }
                    case "picker":
                        {
                            state.Picker = Utility.ToBoolean(val);
                            break;
                        }
                    case "offset":
                        {
                            state.Offset = Utility.ToInt32(val);
                            break;
                        }
                    case "folder":
                        {
                            Folder = Path.Combine(Core.DataDirectory, val);
                            break;
                        }
                    case "caching":
                        {
                            // if you are editing the XML, you will need to clear the cache before you try to reload.
                            m_Cache.Clear();
                            UseCaching = state.Caching = Utility.ToBoolean(val);
                            e.Mobile.SendMessage("Cache cleared");
                            break;
                        }
                    case "flash":
                        {
                            state.Flash = Utility.ToBoolean(val);
                            break;
                        }
                }
            }

            if (state.Picker)
            {
                e.Mobile.SendMessage("Target the center of the object.");
                e.Mobile.Target = new InternalTarget(state);
            }
            else
                BoundingBoxPicker.Begin(e.Mobile, XmlAddon_OnBoundPicked, state);
        }
        private class InternalTarget : Target
        {
            ExportState state;
            public InternalTarget(object obj)
                : base(-1, false, TargetFlags.None)
            {
                state = (ExportState)obj;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (state.Items)
                {
                    if (!(targeted is Item))
                    {
                        from.SendMessage("That is not an item");
                        return;
                    }
                    Point3D pxLeft = (targeted as Item).Location;
                    Point3D pxRight = (targeted as Item).Location;
                    int offset = state.Offset;
                    pxLeft.X -= offset; pxLeft.Y -= offset;
                    pxRight.X += offset; pxRight.Y += offset;

                    XmlAddon_OnBoundPicked(from, from.Map, pxLeft, pxRight, state);
                }
                else if (state.Statics)
                {
                    if (!(targeted is StaticTarget))
                    {
                        from.SendMessage("That is not a static");
                        return;
                    }
                    Point3D pxLeft = (targeted as StaticTarget).Location;
                    Point3D pxRight = (targeted as StaticTarget).Location;
                    int offset = state.Offset;
                    pxLeft.X -= offset; pxLeft.Y -= offset;
                    pxRight.X += offset; pxRight.Y += offset;

                    XmlAddon_OnBoundPicked(from, from.Map, pxLeft, pxRight, state);
                }
            }
        }
        private static void XmlAddon_OnBoundPicked(Mobile from, Map map, Point3D start, Point3D end, object obj)
        {
            if (map == null || map == Map.Internal)
                return; // sanity

            ExportState state = (ExportState)obj;

            Rectangle2D rect = new Rectangle2D(start, new Point2D(end.X + 1, end.Y + 1));

            List<AddonComponentEntry> list = new List<AddonComponentEntry>();

            if (state.Items)
            {
                foreach (Item item in map.GetItemsInBounds(rect))
                {
                    if (item.ItemID == 0x0 || !item.Visible || item is Addon)
                        continue;

                    AddonComponentEntry e = new AddonComponentEntry(item.ItemID, item.X, item.Y, item.Z);

                    if (item.Hue != 0)
                        e.Hue = item.Hue;

                    if (item.Name != item.DefaultName)
                        e.Name = item.Name;

                    if (item.Light != (LightType)0)
                        e.Light = item.Light;

                    list.Add(e);
                }
            }

            if (state.Statics)
            {
                for (int y = rect.Y; y < rect.Y + rect.Height; y++)
                {
                    for (int x = rect.X; x < rect.X + rect.Width; x++)
                    {
                        StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y);

                        foreach (StaticTile tile in tiles)
                            list.Add(new AddonComponentEntry(tile.ID - 0x4000, x, y, tile.Z));
                    }
                }
            }

            if (list.Count == 0)
            {
                from.SendMessage("Nothing to export.");
                return;
            }

            if (state.Flash)
                Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);

            AddonComponentEntry[] array = list.ToArray();

            int xMin = int.MaxValue;
            int xMax = int.MinValue;
            int yMin = int.MaxValue;
            int yMax = int.MinValue;
            int zMin = int.MaxValue;

            foreach (AddonComponentEntry entry in array)
            {
                xMin = Math.Min(xMin, entry.X);
                xMax = Math.Max(xMax, entry.X);
                yMin = Math.Min(yMin, entry.Y);
                yMax = Math.Max(yMax, entry.Y);
                zMin = Math.Min(zMin, entry.Z);
            }

            Point3D offset;

            switch (state.Origin)
            {
                case AddonOrigin.Target:
                    {
                        offset = new Point3D(start.X, start.Y, start.Z);
                        break;
                    }
                default:
                case AddonOrigin.NorthWest:
                    {
                        offset = new Point3D(-xMin, -yMin, -zMin);
                        break;
                    }
                case AddonOrigin.Center:
                    {
                        offset = new Point3D(-(xMin + xMax) / 2, -(yMin + yMax) / 2, -zMin);
                        break;
                    }
            }

            if (offset != Point3D.Zero)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    AddonComponentEntry c = array[i];

                    c.X += offset.X;
                    c.Y += offset.Y;
                    c.Z += offset.Z;
                }
            }

            if (state.Sort)
                Array.Sort(array, ComponentComparer.Instance);

            AddonData data = new AddonData(state.ID, state.Name, array);

            if (File.Exists(GetFileName(data.ID)))
                // backup the file we are about clobber
                File.Move(GetFileName(data.ID), GetFileName(data.ID +
                    string.Format(" Backup ({0})", Utility.GetStableHashCode(Core.TickCount.ToString())))
                    );

            Save(GetFileName(data.ID), data);

            if (state.Caching)
                m_Cache[data.ID] = data;

            if (from.Backpack != null)
                from.Backpack.DropItem(new XmlAddonDeed(data.ID));
        }

        private static void Save(string fileName, AddonData data)
        {
            XmlTextWriter writer = null;

            try
            {
                if (!Directory.Exists(Folder))
                    Directory.CreateDirectory(Folder);

                writer = new XmlTextWriter(fileName, Encoding.Default);
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument(true);

                data.Save(writer);
            }
            catch (Exception e)
            {
                Console.WriteLine("XmlAddon Error: {0}", e);
            }
            finally
            {
                if (writer != null)
                {
                    writer.WriteEndDocument();
                    writer.Close();
                }
            }
        }

        private static AddonData Load(string fileName)
        {
            if (!File.Exists(fileName))
                return null;

            AddonData data = null;

            XmlTextReader reader = null;

            try
            {
                reader = new XmlTextReader(fileName);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                data = new AddonData(reader);
            }
            catch (Exception e)
            {
                Console.WriteLine("XmlAddon Error: {0}", e);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }

            return data;
        }

        private enum AddonOrigin : byte
        {
            Target,
            NorthWest,
            Center,
        }

        private class ExportState
        {
            public string ID { get; set; }
            public string Name { get; set; }
            public bool Items { get; set; }
            public bool Statics { get; set; }
            public AddonOrigin Origin { get; set; }
            public bool Sort { get; set; }
            public bool Picker { get; set; }
            public int Offset { get; set; }
            public bool Caching { get; set; }
            public bool Flash { get; set; }
        }

        private class ComponentComparer : IComparer<AddonComponentEntry>
        {
            public static readonly IComparer<AddonComponentEntry> Instance = new ComponentComparer();

            public ComponentComparer()
            {
            }

            public int Compare(AddonComponentEntry a, AddonComponentEntry b)
            {
                int dz = a.Z.CompareTo(b.Z);

                if (dz != 0)
                    return dz;

                int dy = a.Y.CompareTo(b.Y);

                if (dy != 0)
                    return dy;

                return a.X.CompareTo(b.X);
            }
        }
    }

    public class AddonData
    {
        private string m_ID;
        private string m_Name;
        private AddonComponentEntry[] m_Components;

        public string ID { get { return m_ID; } }
        public string Name { get { return m_Name; } }
        public AddonComponentEntry[] Components { get { return m_Components; } }

        public AddonData(string id, string name, AddonComponentEntry[] components)
        {
            m_ID = id;
            m_Name = name;
            m_Components = components;
        }

        public void Save(XmlTextWriter writer)
        {
            try
            {
                writer.WriteStartElement("AddonData");
                writer.WriteAttributeString("version", "2");

                writer.WriteElementString("ID", m_ID);
                writer.WriteElementString("Name", m_Name);

                try
                {
                    writer.WriteStartElement("Components");
                    writer.WriteAttributeString("Count", m_Components.Length.ToString());

                    for (int i = 0; i < m_Components.Length; i++)
                        m_Components[i].Save(writer);
                }
                finally { writer.WriteEndElement(); }
            }
            finally { writer.WriteEndElement(); }
        }

        public AddonData(XmlTextReader reader)
        {
            int version = Utility.ToInt32(reader.GetAttribute("version"));

            reader.ReadStartElement("AddonData");

            switch (version)
            {
                case 2:
                case 1:
                case 0:
                    {
                        m_ID = reader.ReadElementString("ID");
                        m_Name = reader.ReadElementString("Name");

                        m_Components = new AddonComponentEntry[Utility.ToInt32(reader.GetAttribute("Count"))];

                        reader.ReadStartElement("Components");

                        for (int i = 0; i < m_Components.Length; i++)
                            m_Components[i] = new AddonComponentEntry(reader, version);

                        reader.ReadEndElement();
                        break;
                    }
            }

            reader.ReadEndElement();
        }
    }

    public class AddonComponentEntry
    {
        private int m_ItemID;
        private int m_X;
        private int m_Y;
        private int m_Z;
        private int m_Hue;
        private string m_Name;
        private LightType m_Light;

        public int ItemID { get { return m_ItemID; } }
        public int X { get { return m_X; } set { m_X = value; } }
        public int Y { get { return m_Y; } set { m_Y = value; } }
        public int Z { get { return m_Z; } set { m_Z = value; } }
        public int Hue { get { return m_Hue; } set { m_Hue = value; } }
        public string Name { get { return m_Name; } set { m_Name = value; } }
        public LightType Light { get { return m_Light; } set { m_Light = value; } }

        public AddonComponentEntry(int itemID, int x, int y, int z)
        {
            m_ItemID = itemID;
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        [Flags]
        private enum AdditionalFlag : byte
        {
            None = 0x00,

            Hue = 0x01,
            Name = 0x02,
            Light = 0x04,
        }

        public void Save(XmlTextWriter writer)
        {
            AdditionalFlag flags = AdditionalFlag.None;

            if (m_Hue != 0)
                flags |= AdditionalFlag.Hue;

            if (m_Name != null)
                flags |= AdditionalFlag.Name;

            if (m_Light != (LightType)0)
                flags |= AdditionalFlag.Light;

            try
            {
                writer.WriteStartElement("Component");
                writer.WriteAttributeString("Flags", String.Format("0x{0}", ((byte)flags).ToString("X2")));

                writer.WriteElementString("ItemID", m_ItemID.ToString());
                writer.WriteElementString("X", m_X.ToString());
                writer.WriteElementString("Y", m_Y.ToString());
                writer.WriteElementString("Z", m_Z.ToString());

                if (flags.HasFlag(AdditionalFlag.Hue))
                    writer.WriteElementString("Hue", m_Hue.ToString());

                if (flags.HasFlag(AdditionalFlag.Name))
                    writer.WriteElementString("Name", m_Name);

                if (flags.HasFlag(AdditionalFlag.Light))
                    writer.WriteElementString("Light", m_Light.ToString());
            }
            finally { writer.WriteEndElement(); }
        }

        public AddonComponentEntry(XmlTextReader reader, int version)
        {
            AdditionalFlag flags = AdditionalFlag.None;

            if (version >= 2)
                flags = (AdditionalFlag)Convert.ToByte(reader.GetAttribute("Flags").Substring(2), 16);

            reader.ReadStartElement("Component");

            m_ItemID = Utility.ToInt32(reader.ReadElementString("ItemID"));
            m_X = Utility.ToInt32(reader.ReadElementString("X"));
            m_Y = Utility.ToInt32(reader.ReadElementString("Y"));
            m_Z = Utility.ToInt32(reader.ReadElementString("Z"));

            if (version == 1)
                flags = (AdditionalFlag)Convert.ToByte(reader.ReadElementString("Flags").Substring(2), 16);

            if (flags.HasFlag(AdditionalFlag.Hue))
                m_Hue = Utility.ToInt32(reader.ReadElementString("Hue"));

            if (flags.HasFlag(AdditionalFlag.Name))
                m_Name = reader.ReadElementString("Name");

            if (flags.HasFlag(AdditionalFlag.Light))
            {
                LightType light;

                if (Enum.TryParse<LightType>(reader.ReadElementString("Light"), out light))
                    m_Light = light;
            }

            reader.ReadEndElement();
        }
    }

    public class XmlAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new XmlAddonDeed(m_AddonID, m_IsRedeedable, m_RetainsDeedHue); } }
        public override bool Redeedable { get { return m_IsRedeedable; } }
        public override bool RetainDeedHue { get { return m_RetainsDeedHue; } }

        private string m_AddonID;
        private bool m_IsRedeedable;
        private bool m_RetainsDeedHue;

        [CommandProperty(AccessLevel.GameMaster)]
        public string AddonID
        {
            get { return m_AddonID; }
            set { m_AddonID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRedeedable
        {
            get { return m_IsRedeedable; }
            set { m_IsRedeedable = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RetainsDeedHue
        {
            get { return m_RetainsDeedHue; }
            set { m_RetainsDeedHue = value; }
        }

        [Constructable]
        public XmlAddon(string addonID)
            : this(addonID, true, false)
        {
        }

        public XmlAddon(string addonID, bool redeedable, bool retainsDeedHue)
            : base()
        {
            m_AddonID = addonID;
            m_IsRedeedable = redeedable;
            m_RetainsDeedHue = retainsDeedHue;

            Build(this, XmlAddonSystem.Folder, m_AddonID);
        }
        public override Item Dupe(int amount)
        {
            XmlAddon new_xml_addon = new(m_AddonID, m_IsRedeedable, m_RetainsDeedHue);
            // can't remove the "dummy component", apparently XMLAddon depends on it :(
            //new_xml_addon.Components.Clear();
            return base.Dupe(new_xml_addon, amount);
        }
        public static void Build(BaseAddon addon, string folder, string id)
        {
            Build(addon, folder, id, typeof(AddonComponent));
        }

        public static void Build(BaseAddon addon, string folder, string id, Type componentType)
        {
            AddonData data = XmlAddonSystem.GetData(folder, id);

            if (data == null)
            {
                // dummy component
                addon.AddComponent(new AddonComponent(0x0, true), 0, 0, 0);
            }
            else
            {
                foreach (AddonComponentEntry e in data.Components)
                {
                    AddonComponent c = ConstructComponent(componentType, e.ItemID);

                    // failed to construct component
                    if (c == null)
                        continue;

                    if (e.Hue != 0)
                        c.Hue = e.Hue;

                    if (e.Name != null)
                        c.Name = e.Name;

                    if (e.Light != (LightType)0)
                        c.Light = e.Light;

                    addon.AddComponent(c, e.X, e.Y, e.Z);
                }
            }
        }
        private static AddonComponent ConstructComponent(Type type, int itemID)
        {
            // special case, we pass the deco flag
            if (type == typeof(AddonComponent))
                return new AddonComponent(itemID, true);

            if (!typeof(AddonComponent).IsAssignableFrom(type))
                return null;

            AddonComponent ac;

            try
            {
                ac = (AddonComponent)Activator.CreateInstance(type, itemID);
            }
            catch
            {
                ac = null;
            }

            return ac;
        }

        public static bool MatchByName(BaseAddon addon, string folder, string id)
        {
            AddonData data = XmlAddonSystem.GetData(folder, id);

            if (data == null || addon.Components.Count != data.Components.Length)
                return false;

            for (int i = 0; i < addon.Components.Count; i++)
            {
                AddonComponent c = addon.Components[i] as AddonComponent;
                AddonComponentEntry e = data.Components[i];

                if (
                    c == null ||
                    c.Offset.X != e.X ||
                    c.Offset.Y != e.Y ||
                    c.Offset.Z != e.Z ||
                    c.ItemID != e.ItemID ||
                    c.Hue != e.Hue)
                {
                    return false;
                }
            }

            return true;
        }

        public XmlAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_RetainsDeedHue);

            writer.Write((string)m_AddonID);
            writer.Write((bool)m_IsRedeedable);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_RetainsDeedHue = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        m_AddonID = reader.ReadString();
                        m_IsRedeedable = reader.ReadBool();
                        break;
                    }
            }
        }
    }

    public class XmlAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new XmlAddon(m_AddonID, m_IsRedeedable, m_RetainsDeedHue); } }

        private string m_AddonID;
        private bool m_IsRedeedable;
        private bool m_RetainsDeedHue;

        [CommandProperty(AccessLevel.GameMaster)]
        public string AddonID
        {
            get { return m_AddonID; }
            set { m_AddonID = value; UpdateName(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRedeedable
        {
            get { return m_IsRedeedable; }
            set { m_IsRedeedable = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RetainsDeedHue
        {
            get { return m_RetainsDeedHue; }
            set { m_RetainsDeedHue = value; }
        }

        [Constructable]
        public XmlAddonDeed()
            : this(null, true, false)
        {
        }

        [Constructable]
        public XmlAddonDeed(string addonID)
            : this(addonID, true, false)
        {
        }

        public XmlAddonDeed(string addonID, bool redeedable, bool retainsDeedHue)
            : base()
        {
            m_AddonID = addonID;
            m_IsRedeedable = redeedable;
            m_RetainsDeedHue = retainsDeedHue;

            UpdateName();
        }

        private void UpdateName()
        {
            AddonData data = XmlAddonSystem.GetData(m_AddonID);

            if (data != null)
                this.Name = data.Name;
        }

        public XmlAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((bool)m_RetainsDeedHue);

            writer.Write((string)m_AddonID);
            writer.Write((bool)m_IsRedeedable);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_RetainsDeedHue = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        m_AddonID = reader.ReadString();
                        m_IsRedeedable = reader.ReadBool();
                        break;
                    }
            }
        }
    }
}