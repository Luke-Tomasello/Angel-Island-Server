/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Server\DupeUtility.cs
   * ChangeLog:
   *  5/11/2026, Adam
   *      Initial creation.
   *      Centralized item duplication infrastructure.
   *      Added shallow and deep recursive item duplication support.
   *      Added AddonToStatic flattening policy support.
   *      Added recursive containment reconstruction.
   *      Added logical item relinking infrastructure.
   *      Added XML-based portable item graph export/import.
   *      Added clipboard/file transfer support.
   *      Added type-specific XML extension hooks:
   *          Item.WriteDupeXml()
   *          Item.ReadDupeXml()
   */

using Server.Items;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Xml;
using TextCopy;

namespace Server
{
    public static class DupeUtility
    {
        [Flags]
        public enum DupeFlags
        {
            None = 0x00,

            // Flatten addon components into simple statics.
            // Useful for world mirroring and structural duplication where
            // full addon reconstruction is not desired.
            AddonToStatic = 0x01,

            // Recursively duplicate container contents.
            // Reserved for future deep-copy support.
            Deep = 0x02,
        }

        public static Item? Dupe(Item source, DupeFlags flags = DupeFlags.AddonToStatic)
        {
            if (flags.HasFlag(DupeFlags.Deep))
                return DeepDupe(source, flags);

            return ShallowDupe(source, flags);
        }

        private static Item? DeepDupe(Item source, DupeFlags flags)
        {
            if (source == null || source.Deleted)
                return null;

            try
            {
                Dictionary<Serial, Item> itemMap = new Dictionary<Serial, Item>();

                // Phase 1:
                // Recursively construct the containment tree while building
                // the old -> new remap table.
                Item? root = DeepDupeWorker(source, flags, itemMap);

                // Phase 2:
                // Repair logical item-to-item relationships now that the
                // entire duplicated object graph exists.
                RelinkItemReferences(itemMap);

                return root;
            }
            catch
            {
                return null;
            }
        }

        private static Item? DeepDupeWorker(Item source, DupeFlags flags, Dictionary<Serial, Item> itemMap)
        {
            if (source == null || source.Deleted)
                return null;

            // Perform normal shallow duplication first.
            Item? copy = ShallowDupe(source, flags);

            if (copy == null)
                return null;

            // Register remap entry for later logical relinking.
            itemMap[source.Serial] = copy;

            // Preserve relative/internal item position.
            copy.Location = source.Location;

            // Recursive containment duplication.
            //
            // Structural tree construction is explicit here and does NOT rely
            // on generalized relinking.
            if (source is Container srcContainer &&
                copy is Container dstContainer)
            {
                List<Item> children = new List<Item>(srcContainer.Items);

                foreach (Item child in children)
                {
                    if (child == null || child.Deleted)
                        continue;

                    Item? childCopy = DeepDupeWorker(child, flags, itemMap);

                    if (childCopy == null || childCopy.Deleted)
                        continue;

                    // Some legacy Dupe() implementations may auto-place the copy into
                    // the source parent/container. Deep duplication owns containment
                    // explicitly, so detach before inserting into the cloned container.
                    if (childCopy.Parent is Container parent)
                        parent.RemoveItem(childCopy);

                    dstContainer.AddItem(childCopy);

                    // Preserve original placement inside the container.
                    childCopy.Location = child.Location;
                }
            }

            return copy;
        }

        private static Item? ShallowDupe(Item source, DupeFlags flags)
        {
            if (source == null || source.Deleted)
                return null;

            try
            {
                // Optional addon flattening policy.
                //
                // Used by systems such as MirrorFelucca where addon reconstruction
                // is intentionally collapsed into static world geometry.
                if (source is AddonComponent ac &&
                    flags.HasFlag(DupeFlags.AddonToStatic))
                {
                    Item flattened = new Static(ac.ItemID);

                    Utility.CopyProperties(source, flattened);

                    return flattened;
                }

                Item? copy = null;

                // 1. Prefer explicit type-specific duplication.
                // A Dupe override owns the full clone process, including property copy.
                if (HasDupeOverride(source))
                    return source.Dupe(1);

                // 2. Known constructor patterns.
                switch (source)
                {
                    case Static s:
                        copy = new Static(s.ItemID);
                        break;

                    case LocalizedSign s:
                        copy = new LocalizedSign(s.ItemID, s.LabelNumber);
                        break;

                    case Sign s:
                        copy = new Sign(s.ItemID);
                        break;

                    case Item item when HasParameterlessConstructor(item.GetType()):
                        copy = (Item)Activator.CreateInstance(item.GetType());
                        break;

                    case LockpickableDoor door:
                        copy = (Item)Activator.CreateInstance(
                            door.GetType(),
                            door.ClosedID - (2 * (int)door.Facing),
                            door.OpenedID - (2 * (int)door.Facing),
                            door.OpenedSound,
                            door.ClosedSound,
                            door.Facing);

                        break;

                    case BaseDoor door:
                        copy = (Item)Activator.CreateInstance(
                            door.GetType(),
                            door.Facing);

                        break;

                    default:
                        copy = new Static(source.ItemID);
                        break;
                }

                // 3. Failure.
                if (copy == null)
                    return null;

                // 4. Broad property copy with DoNotCopy support.
                if (copy is BaseDoor)
                {
                    Utility.CopyProperties(source, copy);
                    CopyDoorProperties(source as BaseDoor, copy as BaseDoor);
                }
                else
                {
                    Utility.CopyProperties(source, copy);
                }

                return copy;
            }
            catch
            {
                return null;
            }
        }

        private static void CopyDoorProperties(BaseDoor source, BaseDoor dest)
        {
            dest.CustomClosedID = source.CustomClosedID;
            dest.CustomOpenedID = source.CustomOpenedID;
            dest.Facing = source.Facing;
            dest.KeyValue = source.KeyValue;
            dest.Open = source.Open;
            dest.Locked = source.Locked;
            dest.OpenedID = (source.OpenedID - (2 * (int)source.Facing));
            dest.ClosedID = (source.ClosedID - (2 * (int)source.Facing));
            dest.OpenedSound = source.OpenedSound;
            dest.ClosedSound = source.ClosedSound;
            dest.Offset = source.Offset;
            dest.Link = source.Link;
        }

        private static bool HasDupeOverride(Item item)
        {
            var type = item.GetType();

            MethodInfo mi = type.GetMethod(
                "Dupe",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly,
                null,
                new[] { typeof(int) },
                null);

            return mi != null;
        }

        private static bool HasParameterlessConstructor(Type type)
        {
            return type.GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null) != null;
        }

        private static int RelinkItemReferences(Dictionary<Serial, Item> itemMap)
        {
            int count = 0;

            foreach (KeyValuePair<Serial, Item> kvp in itemMap)
            {
                Item? source = World.FindItem(kvp.Key);
                Item dest = kvp.Value;

                if (source == null || source.Deleted || dest == null || dest.Deleted)
                    continue;

                count += RelinkItemReferences(source, dest, itemMap);
            }

            return count;
        }
        private static int RelinkItemReferences(Item source, Item dest, Dictionary<Serial, Item> itemMap)
        {
            int count = 0;

            if (source == null || source.Deleted || dest == null || dest.Deleted)
                return 0;

            PropertyInfo[] props = Utility.ItemRWProperties(source);

            for (int i = 0; i < props.Length; i++)
            {
                PropertyInfo prop = props[i];

                try
                {
                    if (prop.GetIndexParameters().Length != 0)
                        continue;

                    CopyableAttribute ca = Utility.GetCA(source.GetType(), prop.Name);

                    if (ca != null && (ca.CopyType == CopyType.DoNotCopy || ca.CopyType == CopyType.DoNotRelink))
                        continue;

                    bool itemProperty = typeof(Item).IsAssignableFrom(prop.PropertyType);
                    bool entityProperty = typeof(IEntity).IsAssignableFrom(prop.PropertyType);

                    if (!itemProperty && !entityProperty)
                        continue;

                    object? sourceValue = prop.GetValue(source, null);

                    if (sourceValue == null)
                        continue;

                    Item? oldTarget = sourceValue as Item;

                    if (oldTarget == null && sourceValue is IEntity entity)
                        oldTarget = entity as Item;

                    if (oldTarget == null || oldTarget.Deleted)
                        continue;

                    if (!itemMap.TryGetValue(oldTarget.Serial, out Item? newTarget))
                        continue;

                    if (newTarget == null || newTarget.Deleted)
                        continue;

                    if (newTarget.Map != dest.Map)
                        continue;

                    if (!prop.PropertyType.IsAssignableFrom(newTarget.GetType()))
                        continue;

                    prop.SetValue(dest, newTarget, null);
                    count++;
                }
                catch
                {
                    // Ignore reflection/property side effects for now.
                }
            }

            return count;
        }
        #region XML
        public static bool DupeToClipboard(Item source, DupeFlags flags = DupeFlags.Deep)
        {
            string? xml = DupeToXml(source, flags);

            if (string.IsNullOrWhiteSpace(xml))
                return false;

            new Clipboard().SetText(xml);
            return true;
        }
        public static string? DupeToXml(Item source, DupeFlags flags = DupeFlags.Deep)
        {
            if (source == null || source.Deleted)
                return null;

            try
            {
                using (StringWriter sw = new StringWriter())
                using (XmlTextWriter xml = new XmlTextWriter(sw))
                {
                    xml.Formatting = Formatting.Indented;
                    xml.Indentation = 2;

                    xml.WriteStartDocument();

                    xml.WriteStartElement("DupeData");

                    xml.WriteAttributeString("Version", "1");
                    xml.WriteAttributeString("Flags", flags.ToString());

                    Dictionary<Serial, int> itemMap = new Dictionary<Serial, int>();

                    int nextId = 1;

                    WriteItemXml(
                        xml,
                        source,
                        flags,
                        itemMap,
                        ref nextId);

                    xml.WriteEndElement(); // </DupeData>

                    xml.WriteEndDocument();

                    xml.Flush();

                    return sw.ToString();
                }
            }
            catch
            {
                return null;
            }
        }
        public static Item? DupeFromXml(string xmlText)
        {
            if (string.IsNullOrWhiteSpace(xmlText))
                return null;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlText);

                XmlElement? root = doc["DupeData"];

                if (root == null)
                    return null;

                XmlElement? itemNode = root["Item"];

                if (itemNode == null)
                    return null;

                Dictionary<int, Item> itemMap = new Dictionary<int, Item>();

                Item? item = ReadItemXml(itemNode, itemMap);

                // Later:
                // Relink XML-exported logical references here.

                return item;
            }
            catch
            {
                return null;
            }
        }
        private static Item? ConstructItemForXml(Type type)
        {
            try
            {
                if (HasParameterlessConstructor(type))
                    return (Item)Activator.CreateInstance(type);
            }
            catch
            {
            }

            return null;
        }
        private static void ApplyItemXmlProperties(Item item, XmlElement? propsNode)
        {
            if (item == null || propsNode == null)
                return;

            foreach (XmlElement propNode in propsNode.GetElementsByTagName("Property"))
            {
                string name = propNode.GetAttribute("Name");
                string typeName = propNode.GetAttribute("Type");
                string valueStr = propNode.GetAttribute("Value");

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                PropertyInfo? pi = FindProperty(item.GetType(), name);

                if (pi == null || !pi.CanWrite)
                    continue;

                try
                {
                    object value = ConvertXmlValue(
                        valueStr,
                        pi.PropertyType,
                        typeName);

                    if (value != null)
                        pi.SetValue(item, value, null);
                }
                catch
                {
                    // Intentionally tolerate malformed or unsupported properties.
                }
            }
        }
        private static object? ConvertXmlValue(string valueStr, Type targetType, string xmlTypeName)
        {
            try
            {
                if (targetType == typeof(string))
                    return valueStr;

                if (targetType.IsEnum)
                    return Enum.Parse(targetType, valueStr);

                if (targetType == typeof(bool))
                    return Utility.ToBoolean(valueStr);

                if (targetType == typeof(byte))
                    return Convert.ToByte(valueStr);

                if (targetType == typeof(sbyte))
                    return Convert.ToSByte(valueStr);

                if (targetType == typeof(short))
                    return Convert.ToInt16(valueStr);

                if (targetType == typeof(ushort))
                    return Convert.ToUInt16(valueStr);

                if (targetType == typeof(int))
                    return Convert.ToInt32(valueStr);

                if (targetType == typeof(uint))
                    return Convert.ToUInt32(valueStr);

                if (targetType == typeof(long))
                    return Convert.ToInt64(valueStr);

                if (targetType == typeof(ulong))
                    return Convert.ToUInt64(valueStr);

                if (targetType == typeof(float))
                    return Convert.ToSingle(valueStr);

                if (targetType == typeof(double))
                    return Convert.ToDouble(valueStr);

                if (targetType == typeof(decimal))
                    return Convert.ToDecimal(valueStr);

                if (targetType == typeof(DateTime))
                    return Convert.ToDateTime(valueStr);

                if (targetType == typeof(TimeSpan))
                    return TimeSpan.Parse(valueStr);

                if (targetType == typeof(Point3D))
                {
                    string[] parts = valueStr.Split(',');

                    if (parts.Length == 3)
                    {
                        return new Point3D(
                            Utility.ToInt32(parts[0]),
                            Utility.ToInt32(parts[1]),
                            Utility.ToInt32(parts[2]));
                    }
                }

                if (targetType == typeof(Point2D))
                {
                    string[] parts = valueStr.Split(',');

                    if (parts.Length == 2)
                    {
                        return new Point2D(
                            Utility.ToInt32(parts[0]),
                            Utility.ToInt32(parts[1]));
                    }
                }

                if (targetType == typeof(Color))
                    return Color.FromArgb(Convert.ToInt32(valueStr));

                // Fallback conversion path.
                return Convert.ChangeType(valueStr, targetType);
            }
            catch
            {
                return null;
            }
        }
        private static PropertyInfo? FindProperty(Type type, string name)
        {
            return type.GetProperty(
                name,
                BindingFlags.Public |
                BindingFlags.Instance);
        }
        public static Item? DupeFromClipboard()
        {
            string? xml = new Clipboard().GetText();

            if (string.IsNullOrWhiteSpace(xml))
                return null;

            return DupeFromXml(xml);
        }
        private static Item? ReadItemXml(XmlElement node, Dictionary<int, Item> itemMap)
        {
            if (node == null)
                return null;

            int id = Utility.ToInt32(node.GetAttribute("ID"));
            string typeName = node.GetAttribute("Type");

            if (id <= 0 || string.IsNullOrWhiteSpace(typeName))
                return null;

            Type? type = ScriptCompiler.FindTypeByFullName(typeName);

            if (type == null)
                type = Type.GetType(typeName);

            if (type == null || !typeof(Item).IsAssignableFrom(type))
                return null;

            Item? item = ConstructItemForXml(type);

            if (item == null)
                return null;

            itemMap[id] = item;

            item.Location = new Point3D(
                Utility.ToInt32(node.GetAttribute("X")),
                Utility.ToInt32(node.GetAttribute("Y")),
                Utility.ToInt32(node.GetAttribute("Z")));

            ApplyItemXmlProperties(item, node["Properties"]);

            // Type-specific dupe XML.
            // Complex items can restore private/internal state here.
            try
            {
                item.ReadDupeXml(node["Custom"]);
            }
            catch
            {
                // Intentionally tolerate custom import failures.
            }

            XmlElement? contents = node["Contents"];

            if (contents != null && item is Container container)
            {
                foreach (XmlElement childNode in contents.GetElementsByTagName("Item"))
                {
                    // Only direct child Item nodes.
                    if (childNode.ParentNode != contents)
                        continue;

                    Item? child = ReadItemXml(childNode, itemMap);

                    if (child == null)
                        continue;

                    container.AddItem(child);

                    child.Location = new Point3D(
                        Utility.ToInt32(childNode.GetAttribute("X")),
                        Utility.ToInt32(childNode.GetAttribute("Y")),
                        Utility.ToInt32(childNode.GetAttribute("Z")));
                }
            }

            return item;
        }

        private static void WriteItemXml(XmlTextWriter xml, Item item, DupeFlags flags, Dictionary<Serial, int> itemMap, ref int nextId)
        {
            if (item == null || item.Deleted)
                return;

            // Assign a stable export ID for later relinking/import.
            int id = nextId++;

            itemMap[item.Serial] = id;

            xml.WriteStartElement("Item");

            xml.WriteAttributeString("ID", id.ToString());
            xml.WriteAttributeString("Type", item.GetType().FullName);

            // Preserve relative/internal placement.
            xml.WriteAttributeString("X", item.Location.X.ToString());
            xml.WriteAttributeString("Y", item.Location.Y.ToString());
            xml.WriteAttributeString("Z", item.Location.Z.ToString());

            // -----------------------------------------------------------------
            // Properties
            // -----------------------------------------------------------------

            xml.WriteStartElement("Properties");

            PropertyInfo[] props = Utility.ItemRWProperties(item);

            foreach (PropertyInfo pi in props)
            {
                // Runtime/world ownership properties are intentionally excluded.
                switch (pi.Name)
                {
                    case "Serial":
                    case "Parent":
                    case "Map":
                    case "RootParent":
                    case "Location":
                    case "X":
                    case "Y":
                    case "Z":
                        continue;
                }

                try
                {
                    object? value = pi.GetValue(item, null);

                    if (value == null)
                        continue;

                    Type valueType = value.GetType();

                    // Skip unsupported/complex runtime types for now.
                    if (!IsSimpleXmlType(valueType))
                        continue;

                    xml.WriteStartElement("Property");

                    xml.WriteAttributeString("Name", pi.Name);
                    xml.WriteAttributeString("Type", valueType.FullName);
                    xml.WriteAttributeString("Value", value.ToString());

                    xml.WriteEndElement();
                }
                catch
                {
                    // Intentionally ignore problematic properties.
                }
            }

            xml.WriteEndElement(); // </Properties>

            // -----------------------------------------------------------------
            // Type-specific dupe XML
            // -----------------------------------------------------------------

            xml.WriteStartElement("Custom");

            try
            {
                item.WriteDupeXml(xml);
            }
            catch
            {
                // Intentionally tolerate custom export failures.
            }

            xml.WriteEndElement(); // </Custom>

            // -----------------------------------------------------------------
            // Deep contents
            // -----------------------------------------------------------------

            if (flags.HasFlag(DupeFlags.Deep) &&
                item is Container container &&
                container.Items.Count > 0)
            {
                xml.WriteStartElement("Contents");

                foreach (Item child in container.Items)
                    WriteItemXml(xml, child, flags, itemMap, ref nextId);

                xml.WriteEndElement(); // </Contents>
            }

            xml.WriteEndElement(); // </Item>
        }

        private static bool IsSimpleXmlType(Type type)
        {
            if (type.IsEnum)
                return true;

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.DateTime:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.String:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;

                default:
                    return false;
            }
        }


        #endregion XML
    }
}
