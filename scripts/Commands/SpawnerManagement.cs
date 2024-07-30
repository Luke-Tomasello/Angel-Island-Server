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

/* Scripts\Engines\Spawner\SpawnerCapture.cs
 * CHANGELOG:
 *	9/8/2023, Adam
 *		Complete rewrite.
 *			Notion: Rather than explicitly state what spawner properties are written/read, this new model enumerated the writable/readable
 *				spawner properties, and writes/reads those.
 *			TODO: Some spawner props are not currently supported, like for instance Mobile and Item references, etc. Not hard, just:
 *			1. too lazy
 *			2. Probably not appropriate for a 'spawner template xml'
 *	8/30/22, Adam
 *		replace ObjectNames ==> ObjectNamesRaw
 *	3/3/11, adam
 *		Add the ability to name a spawned object which acts as the filter.
 *		For instance, "[savespawners " will save all spawners in the world, but "[savespawners cottonplant" will save all spawners that 
 *		spawn cotton plants. 
 *		Note: we now save these XML files in the data directory in order to recover certain spawn configurations.
 *	2/26/11, Adam
 *		Initial Version
 *		I commented out Kit's versions of the spawner management routines since he failed to take into account that the serial numbers 
 *		would collide on restore. 
 *		An XML version is also far more flexible since it also allows human hand editing
 */

using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using static Server.CoreAI;
using static Server.Mobiles.Spawner;
using static Server.Utility;

namespace Server.Commands
{
    public class SpawnerCapture
    {
        private static string GetPathname(string filename)
        {
            Directory.CreateDirectory(Path.Combine(Core.DataDirectory, "Spawners", "Shared"));
            return string.Format(Path.Combine(Core.DataDirectory, "Spawners", "Shared", "{0}.xml"), filename);
        }
        public static int SaveSpawners(List<Spawner> spawners, string filename, ref List<string> reasons)
        {
            string pathName = GetPathname(filename);
            reasons = new List<string>();
            Dictionary<Spawner, Dictionary<string, string>> masterTable = new();

            foreach (Spawner spawner in spawners)
            {
                Dictionary<string, string> map = new Dictionary<string, string>();
                PropertyInfo[] props = spawner.GetType().GetProperties();

                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead && props[i].CanWrite)
                        {
                            string value = null;
                            if (props[i].GetValue(spawner, null) != null)
                                value = props[i].GetValue(spawner, null).ToString();

                            if (!map.ContainsKey(props[i].Name))
                                map.Add(props[i].Name, value);
                            else
                                ;
                        }
                    }
                    catch
                    {
                        ;
                    }
                }

                // add to our master table of spawners and property values
                masterTable.Add(spawner, map);
            }
            // process map here
            reasons.Add(string.Format("Processing {0} spawners...", masterTable.Count));
            ProcessTable(masterTable, pathName, ref reasons);
            return masterTable.Count;
        }
        private static void ProcessTable(Dictionary<Spawner, Dictionary<string, string>> masterTable, string filename, ref List<string> reasons)
        {
            using (StreamWriter op = new StreamWriter(filename))
            {
                XmlTextWriter xml = new XmlTextWriter(op);

                xml.Formatting = Formatting.Indented;
                xml.IndentChar = '\t';
                xml.Indentation = 1;

                xml.WriteStartDocument(true);
                xml.WriteStartElement("Spawners");

                // just to be complete..
                xml.WriteAttributeString("Count", masterTable.Count.ToString());

                // Now write each entry
                //foreach (Spawner spawner in list)
                //Save(spawner, xml);

                foreach (var kvp in masterTable)
                    Save(xml, spawner: kvp.Key, data: kvp.Value);

                //and end doc
                xml.WriteEndElement();
                xml.Close();
            }
        }
        private static void Save(XmlTextWriter xml, Spawner spawner, Dictionary<string, string> data)
        {
            // All count information is derived from ObjectNamesRaw, i.e., the text names of the objs to spawn
            if (data.ContainsKey("ObjectNamesRaw"))
                data.Remove("ObjectNamesRaw");
            if (data.ContainsKey("Count"))
                data.Remove("Count");
            if (data.ContainsKey("Counts"))
                data.Remove("Counts");
            try
            {
                xml.WriteStartElement("Properties");

                // handle object count independently 
                xml.WriteStartElement("ObjectCount");
                xml.WriteString(spawner.ObjectNamesRaw.Count.ToString());
                xml.WriteEndElement();

                for (int i = 0; i < spawner.ObjectNamesRaw.Count; i++)
                {
                    xml.WriteStartElement("Line" + i);
                    xml.WriteString(spawner.ObjectNames[i] as string);
                    xml.WriteEndElement();
                }

                foreach (var kvp in data)
                {
                    xml.WriteStartElement(kvp.Key);
                    xml.WriteString(kvp.Value);
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }
            catch
            {
                Utility.ConsoleWriteLine("Error saving a spawner entry!", ConsoleColor.Red);
            }
        }
        public static int LoadSpawners(List<Spawner> spawners, string filename, ref List<string> reasons)
        {
            string pathName = GetPathname(filename);
            reasons = new List<string>();

            if (!File.Exists(pathName))
                return 0;

            int count = 0;

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(pathName);
                XmlElement root = doc["Spawners"];
                int read_count = 0;
                count = XmlUtility.GetInt32(root.GetAttribute("Count"), 0);

                foreach (XmlElement entry in root.GetElementsByTagName("Properties"))
                {
                    try
                    {
                        // load in entry!
                        spawners.Add(LoadEntry(entry));
                    }
                    catch
                    {
                        Utility.ConsoleWriteLine("Warning: A Spawner entry load failed", ConsoleColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Utility.ConsoleWriteLine("Exception caught loading spawners.xml", ConsoleColor.Red);
                Utility.ConsoleWriteLine(ex.StackTrace, ConsoleColor.Red);
            }

            return spawners.Count;
        }
        private static Spawner LoadEntry(XmlNode xml)
        {
            GridSpawner spawner = new GridSpawner();

            try
            {
                PropertyInfo[] props = spawner.GetType().GetProperties();

                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead && props[i].CanWrite)
                        {
                            string value = null;
                            value = XmlUtility.GetText(xml[props[i].Name], null);
                            if (props[i].Name.Equals("ObjectNamesRaw"))
                            {
                                int objectCount = XmlUtility.GetInt32(XmlUtility.GetText(xml["ObjectCount"], "0"), 0);
                                spawner.ObjectNamesRaw = new ArrayList();

                                for (int jx = 0; jx < objectCount; jx++)
                                    spawner.ObjectNamesRaw.Add(XmlUtility.GetText(xml["Line" + jx], "?"));
                            }
                            else if (props[i].Name.Equals("Count"))
                            {   // don't set the count for a Grid Spawner as the grid Width/Height and Sparsity set it implicitly
                                continue;
                            }
                            else
                                switch (props[i].PropertyType.Name)
                                {
                                    case "Boolean":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? false : bool.Parse(value));
                                        break;
                                    case "Int32":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? 0 : Int32.Parse(value));
                                        break;
                                    case "UInt32":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? 0 : UInt32.Parse(value));
                                        break;
                                    case "Double":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? 0 : Double.Parse(value));
                                        break;
                                    case "String":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : value);
                                        break;
                                    case "Direction":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Enum.Parse<Direction>(value));
                                        break;
                                    case "SpawnerModeAttribs":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Enum.Parse<SpawnerModeAttribs>(value));
                                        break;
                                    case "ShardConfig":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Enum.Parse<ShardConfig>(value));
                                        break;
                                    case "WorldSize":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Enum.Parse<WorldSize>(value));
                                        break;
                                    case "TimeSpan":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : TimeSpan.Parse(value));
                                        break;
                                    case "SpawnFlags":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Enum.Parse<SpawnFlags>(value));
                                        break;
                                    case "LootType":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Enum.Parse<LootType>(value));
                                        break;
                                    case "DateTime":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : DateTime.Parse(value));
                                        break;
                                    case "Map":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Map.Parse(value));
                                        break;
                                    case "Layer":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Enum.Parse<Layer>(value));
                                        break;
                                    case "Point3D":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Point3D.Parse(value));
                                        break;
                                    case "Point2D":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Point2D.Parse(value));
                                        break;
                                    case "LightType":
                                        props[i].SetValue(spawner, string.IsNullOrEmpty(value) ? null : Enum.Parse<LightType>(value));
                                        break;

                                    // not supported
                                    case "Object":
                                    case "BaseMulti":
                                    case "BaseCamp":
                                    case "Spawner":
                                    case "ArrayList":
                                    case "NavDestinations":
                                    case "NavPoint":
                                    case "WayPoint":
                                    case "DiceEntry":
                                    case "Item":
                                    case "Mobile":
                                        props[i].SetValue(spawner, null);
                                        break;
                                    default:
                                        props[i].SetValue(spawner, null);
                                        break;
                                }
                        }
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogException(e);
                        Utility.ConsoleWriteLine("Exception caught loading spawners.xml", ConsoleColor.Red);
                        Utility.ConsoleWriteLine(e.StackTrace, ConsoleColor.Red);
                    }
                }

                return spawner;
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                Utility.ConsoleWriteLine("Exception caught loading spawners.xml", ConsoleColor.Red);
                Utility.ConsoleWriteLine(e.StackTrace, ConsoleColor.Red);
            }

            return null;
        }
    }
}