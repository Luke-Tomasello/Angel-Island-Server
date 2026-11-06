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

/* Server/Utility.YankPut.cs
 * ChangeLog:
 *  3/16/2026, Adam
 *      First time checkin - initial implementation of Yank/Put system for copying properties between objects via XML serialization.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
namespace Server
{

    public partial class Utility
    {
        #region Yank/Put
        // ====================== YANK / PUT HELPERS ======================

        public static string YankToXml(object target)
        {
            if (target == null)
                return "<YankData Error=\"Target is null\" />";

            using (StringWriter sw = new StringWriter())
            using (XmlTextWriter xml = new XmlTextWriter(sw))
            {
                xml.Formatting = Formatting.Indented;
                xml.Indentation = 2;

                xml.WriteStartDocument();
                xml.WriteStartElement("YankData");

                if (target is Mobile mobile)
                {
                    xml.WriteAttributeString("Type", "Mobile");
                    xml.WriteAttributeString("Class", mobile.GetType().FullName);
                    WriteMobileProperties(xml, mobile);
                    WriteLayers(xml, mobile);
                }
                else if (target is Item item)
                {
                    xml.WriteAttributeString("Type", "Item");
                    xml.WriteAttributeString("Class", item.GetType().FullName);
                    WriteItemProperties(xml, item);
                }
                else
                {
                    xml.WriteAttributeString("Error", $"Unsupported type: {target.GetType().FullName}");
                }

                xml.WriteEndElement(); // </YankData>
                xml.WriteEndDocument();

                return sw.ToString();
            }
        }

        private static void WriteMobileProperties(XmlTextWriter xml, Mobile m)
        {
            xml.WriteStartElement("Properties");
            PropertyInfo[] props = MobileRWProperties(m);

            foreach (PropertyInfo pi in props)
            {
                // Skip dangerous or irrelevant properties
                if (pi.Name == "Serial" || pi.Name == "Map" || pi.Name == "Location" ||
                    pi.Name == "X" || pi.Name == "Y" || pi.Name == "Z")
                    continue;

                try
                {
                    object? value = pi.GetValue(m, null);
                    if (value != null)
                    {
                        xml.WriteStartElement("Property");
                        xml.WriteAttributeString("Name", pi.Name);
                        xml.WriteAttributeString("Value", value.ToString());
                        xml.WriteAttributeString("Type", value.GetType().Name);
                        xml.WriteEndElement();
                    }
                }
                catch { }
            }
            xml.WriteEndElement(); // </Properties>
        }

        private static void WriteItemProperties(XmlTextWriter xml, Item item)
        {
            xml.WriteStartElement("Properties");
            PropertyInfo[] props = ItemRWProperties(item);

            foreach (PropertyInfo pi in props)
            {
                if (pi.Name == "Serial" || pi.Name == "Parent" || pi.Name == "Map" ||
                    pi.Name == "Location" || pi.Name == "X" || pi.Name == "Y" || pi.Name == "Z")
                    continue;

                try
                {
                    object? value = pi.GetValue(item, null);
                    if (value != null)
                    {
                        xml.WriteStartElement("Property");
                        xml.WriteAttributeString("Name", pi.Name);
                        xml.WriteAttributeString("Value", value.ToString());
                        xml.WriteAttributeString("Type", value.GetType().Name);
                        xml.WriteEndElement();
                    }
                }
                catch { }
            }
            xml.WriteEndElement(); // </Properties>
        }

        private static void WriteLayers(XmlTextWriter xml, Mobile m)
        {
            List<Item> layers = GetLayerItems(m, includeHair: true);
            if (layers.Count == 0) return;

            xml.WriteStartElement("Layers");

            foreach (Item item in layers)
            {
                if (item == null) continue;

                xml.WriteStartElement("Layer");
                xml.WriteAttributeString("Name", item.Layer.ToString());

                xml.WriteStartElement("Item");
                xml.WriteAttributeString("Type", item.GetType().FullName);
                xml.WriteAttributeString("Hue", item.Hue.ToString());
                if (!string.IsNullOrWhiteSpace(item.Name))
                    xml.WriteAttributeString("Name", item.Name);

                xml.WriteEndElement(); // </Item>
                xml.WriteEndElement(); // </Layer>
            }

            xml.WriteEndElement(); // </Layers>
        }

        // ===================================================================
        // PUT Helpers
        // ===================================================================

        public static void ApplyYankXml(object target, string xmlText)
        {
            if (target == null || string.IsNullOrWhiteSpace(xmlText))
                return;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlText);

            XmlElement? root = doc["YankData"];
            if (root == null) return;

            string type = root.GetAttribute("Type");

            if (type == "Mobile" && target is Mobile mobile)
                ApplyMobileProperties(mobile, root);
            else if (type == "Item" && target is Item item)
                ApplyItemProperties(item, root);
        }

        private static void ApplyMobileProperties(Mobile m, XmlElement root)
        {
            XmlElement? props = root["Properties"];
            if (props != null)
            {
                foreach (XmlElement p in props.GetElementsByTagName("Property"))
                {
                    string name = p.GetAttribute("Name");
                    string valueStr = p.GetAttribute("Value");

                    PropertyInfo pi = FindMobileRWProperty(m, name);
                    if (pi != null)
                    {
                        try
                        {
                            object? value = ConvertValue(valueStr, pi.PropertyType);
                            if (value != null)
                                pi.SetValue(m, value, null);
                        }
                        catch { }
                    }
                }
            }

            // Apply Layers
            XmlElement? layers = root["Layers"];
            if (layers != null)
            {
                WipeLayers(m);
                foreach (XmlElement layerNode in layers.GetElementsByTagName("Layer"))
                {
                    string layerName = layerNode.GetAttribute("Name");
                    XmlElement? itemNode = layerNode["Item"];
                    if (itemNode == null) continue;

                    string itemTypeStr = itemNode.GetAttribute("Type");
                    int hue = Utility.ToInt32(itemNode.GetAttribute("Hue"));
                    string name = itemNode.GetAttribute("Name");

                    Type itemType = Type.GetType(itemTypeStr) ?? ScriptCompiler.FindTypeByFullName(itemTypeStr);
                    if (itemType == null || !typeof(Item).IsAssignableFrom(itemType))
                        continue;

                    Item? newItem = Dupe(Activator.CreateInstance(itemType));
                    if (newItem == null) continue;

                    newItem.Hue = hue;
                    if (!string.IsNullOrWhiteSpace(name))
                        newItem.Name = name;

                    // Try to find the correct Layer
                    if (Enum.TryParse(layerName, out Layer layer))
                        m.AddItem(newItem);   // AddItem will handle layer assignment
                }
            }
        }

        private static void ApplyItemProperties(Item item, XmlElement root)
        {
            XmlElement? props = root["Properties"];
            if (props == null) return;

            foreach (XmlElement p in props.GetElementsByTagName("Property"))
            {
                string name = p.GetAttribute("Name");
                string valueStr = p.GetAttribute("Value");

                PropertyInfo pi = FindProperty(item.GetType(), name);
                if (pi != null && pi.CanWrite)
                {
                    try
                    {
                        object? value = ConvertValue(valueStr, pi.PropertyType);
                        if (value != null)
                            pi.SetValue(item, value, null);
                    }
                    catch { }
                }
            }
        }

        private static object? ConvertValue(string valueStr, Type targetType)
        {
            try
            {
                if (targetType == typeof(string)) return valueStr;
                if (targetType.IsEnum) return Enum.Parse(targetType, valueStr);
                return Convert.ChangeType(valueStr, targetType);
            }
            catch
            {
                return null;
            }
        }

        private static PropertyInfo FindProperty(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        }

        #endregion Yank/Put
    }
}