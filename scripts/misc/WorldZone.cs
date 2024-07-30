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

/* Misc/WorldZone.cs
 * CHANGELOG:
 *  10/7/22, Adam
 *      Port to Runuo 2.6
 *  9/22/22, Adam
 *      Update calling signature for Decorate.Generate()
 *  9/7/22, Yoar
 *      Disabled the WorldZone system in favor of PushBackSpawner. We've decided that we'd
 *      rather soft block players from visiting certain areas than hard blocking them.
 *  9/6/22, Yoar
 *      Added calls to EventSink.InvokeWorldZoneActive
 *  9/6/22, Yoar
 *      Added DecorationFolder
 *  9/6/22, Yoar
 *      Disabled Configure, Initialize and event handlers for all shards but Island-Siege.
 *  9/4/22, Yoar
 *      Added WorldZoneMarker: Dynamically generates border markers along the zone boundaries.
 *  9/4/22, Yoar (WorldZone)
 *      Initial version.
 *      Restrict the playable area.
 */

using Server.Commands;
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server
{
    /// <summary>
    /// Discontinued!
    /// TODO:
    /// o Restrict SOS locations
    /// o Restrict sea chart locations
    /// o Restrict escort/sea gypsy destinations
    /// o Add guardzone regions to moongates (have "[moongen" deal with this?)
    /// o Fix jail/prison exit TPs
    /// ...
    /// </summary>
    public class WorldZone
    {
        public static bool Enabled { get { return false; } }

        private static WorldZone m_ActiveZone;

        public static WorldZone ActiveZone
        {
            get { return m_ActiveZone; }
            set
            {
                if (m_ActiveZone != value)
                {
                    if (m_ActiveZone != null)
                        DeactivateZone(m_ActiveZone);

                    m_ActiveZone = value;

                    if (m_ActiveZone != null)
                        ActivateZone(m_ActiveZone);
                }
            }
        }

        public static void Configure()
        {
            if (!Enabled)
                return;

            EventSink.WorldLoad += EventSink_OnWorldLoad;
            EventSink.WorldSave += EventSink_OnWorldSave;
        }

        private static void EventSink_OnWorldLoad()
        {
            if (!Enabled)
                return;

            LoadConfig();
        }

        private static void EventSink_OnWorldSave(WorldSaveEventArgs e)
        {
            if (!Enabled)
                return;

            SaveConfig();
        }

        public static void Initialize()
        {
            if (!Enabled)
                return;

            EventSink.Movement += EventSink_Movement;

            CommandSystem.Register("WorldZone", AccessLevel.GameMaster, WorldZone_OnCommand);
            CommandSystem.Register("WorldZoneLoad", AccessLevel.Administrator, WorldZoneLoad_OnCommand);
            CommandSystem.Register("WorldZoneClear", AccessLevel.Administrator, WorldZoneClear_OnCommand);

#if DEBUG
            // for testing purposes only
            if (Enabled)
            {
                WorldZone islandSiege = new WorldZone(
                    "island-siege",
                    Map.Felucca,
                    1, // one marker for every tile
                    0x0000, // no marker
                    0x03AE, // pier
                    new Rectangle3D[] // area
                        {
                            new Rectangle3D(new Point3D(3530,   0, sbyte.MinValue), new Point3D(5120,  780, sbyte.MaxValue)),
                            new Rectangle3D(new Point3D(3290, 780, sbyte.MinValue), new Point3D(5120, 4096, sbyte.MaxValue)),
                        },
                    new Rectangle2D[] // wrap bounds
                        {
                            new Rectangle2D(new Point2D(3290 + 24, 0 + 16), new Point2D(5120 - 16, 4096 - 16)),
                        },
                    new CityInfo[] // starting cities
                        {
                            new CityInfo( "Ocllo", "Ocllo Bank", 3679, 2515, 0, Map.Felucca ),
                        },
                    new PMList(1012001, 1012013, Map.Felucca, new PMEntry[] // public moongates
                        {
                            new PMEntry(new Point3D(3746, 2505, 47), "Ocllo", String.Empty),
                            new PMEntry(new Point3D(3563, 2139, 34), 1012010, 1005396), // Magincia
                            new PMEntry(new Point3D(4467, 1283,  5), 1012003, 1005389), // Moonglow
                            new PMEntry(new Point3D(3569, 1228,  0), "Nujel'm", String.Empty),
                        }),
                    new Point3D[] // unstuck locations
                        {
                            new Point3D(3737, 2588, 40), // Ocllo farms
                        },
                    new Point2D[] // strandedness locations
                        {
                            new Point2D(4512, 3936),
                            new Point2D(4440, 3120),
                            new Point2D(4192, 3672),
                            new Point2D(4720, 3472),
                            new Point2D(3744, 2768),
                            new Point2D(3480, 2432),
                            new Point2D(3560, 2136),
                            new Point2D(3792, 2112),
                            new Point2D(4576, 1456),
                            new Point2D(4680, 1152),
                            new Point2D(4304, 1104),
                            new Point2D(4496,  984),
                            new Point2D(4248,  696),
                            new Point2D(4040,  616),
                            new Point2D(3896,  248),
                            new Point2D(4176,  384),
                            new Point2D(3672, 1104),
                            new Point2D(3520, 1152),
                            new Point2D(3720, 1360),
                            new Point2D(3448,  208),
                            new Point2D(3432,  608),
                        },
                    "Decoration/Island Siege");
                SaveZone(islandSiege);
                ActiveZone = LoadZone("island-siege");
            }
#endif
        }

        private static void EventSink_Movement(MovementEventArgs e)
        {
            if (!Enabled)
                return;

            if (BlockMovement(e.Mobile, e.Direction))
                e.Blocked = true;
        }

        #region Commands

        [Usage("WorldZone")]
        [Description("Opens the properties gump for the currently active world zone.")]
        public static void WorldZone_OnCommand(CommandEventArgs e)
        {
            if (m_ActiveZone == null)
                e.Mobile.SendMessage("There is currently no world zone activated.");
            else
                e.Mobile.SendMessage("Active world zone: \"{0}\".", m_ActiveZone.ID);
        }

        [Usage("WorldZoneLoad <id>")]
        [Description("Loads the world zone given by the specified id.")]
        public static void WorldZoneLoad_OnCommand(CommandEventArgs e)
        {
            if (e.Arguments.Length == 0)
            {
                e.Mobile.SendMessage("Usage: LoadWorldZone <id>");
                return;
            }

            string arg = e.GetString(0);

            WorldZone zone = LoadZone(arg);

            if (zone == null)
            {
                e.Mobile.SendMessage("No world zone \"{0}\" found.", arg);
            }
            else
            {
                e.Mobile.SendMessage("World zone \"{0}\" activated.", zone.ID);
                ActiveZone = zone;
            }
        }

        [Usage("WorldZoneClear")]
        [Description("Unloads the current world zone.")]
        public static void WorldZoneClear_OnCommand(CommandEventArgs e)
        {
            if (m_ActiveZone == null)
            {
                e.Mobile.SendMessage("There is currently no world zone activated.");
            }
            else
            {
                e.Mobile.SendMessage("World zone \"{0}\" deactivated.", m_ActiveZone.ID);
                ActiveZone = null;
            }
        }

        #endregion

        private static void ActivateZone(WorldZone zone)
        {
            Console.WriteLine("Activating world zone: \"{0}\".", zone.ID);

            WorldZoneMarker.DeleteAll();
            int count = WorldZoneMarker.Generate(zone);
            Console.WriteLine("WorldZone: Generated {0} world zone markers.", count);

            PublicMoongate.DeleteAll();
            if (zone.PublicMoongateList != null)
            {
                count = PublicMoongate.Generate(zone.PublicMoongateList);
                Console.WriteLine("WorldZone: Generated {0} public moongates.", count);
            }

            if (zone.DecorationFolder != null)
            {
                Decorate.Generate(Path.Combine(Core.DataDirectory, zone.DecorationFolder), Decorate.DecoMode.delete, maps: new Map[] { zone.Map });
                Decorate.Generate(Path.Combine(Core.DataDirectory, zone.DecorationFolder), Decorate.DecoMode.add, maps: new Map[] { zone.Map });
            }
        }

        private static void DeactivateZone(WorldZone zone)
        {
            Console.WriteLine("Deactivating world zone: \"{0}\".", zone.ID);

            WorldZoneMarker.DeleteAll();
            PublicMoongate.DeleteAll();

            PublicMoongate.GenerateAll();

            if (zone.DecorationFolder != null)
                Decorate.Generate(Path.Combine(Core.DataDirectory, zone.DecorationFolder), Decorate.DecoMode.delete, maps: new Map[] { zone.Map });
        }

        public static bool IsInside(Point3D loc, Map map)
        {
            WorldZone zone = m_ActiveZone;

            if (zone == null || zone.Map == null)
                return false;

            return (zone.Map == map && AreaContains(zone.Area, loc));
        }

        public static bool IsOutside(Point3D loc, Map map)
        {
            WorldZone zone = m_ActiveZone;

            if (zone == null || zone.Map == null)
                return false;

            return (zone.Map != map || !AreaContains(zone.Area, loc));
        }

        public static bool BlockMovement(Mobile from, Direction d)
        {
            if (from.AccessLevel > AccessLevel.Player)
                return false;

            WorldZone zone = m_ActiveZone;

            if (zone == null || zone.Map == null || zone.Map != from.Map || !AreaContains(zone.Area, from))
                return false;

            int x = from.X;
            int y = from.Y;
            Movement.Movement.Offset(d, ref x, ref y);

            return !AreaContains(zone.Area, new Point3D(x, y, from.Z));
        }

        public static bool BlockTeleport(Mobile from, Point3D loc, Map map)
        {
            if (from.AccessLevel > AccessLevel.Player)
                return false;

            WorldZone zone = m_ActiveZone;

            if (zone == null || zone.Map == null || zone.Map != from.Map || !AreaContains(zone.Area, from))
                return false;

            return (zone.Map != map || !AreaContains(zone.Area, loc));
        }

        private static bool AreaContains(Rectangle3D[] area, IPoint3D loc)
        {
            foreach (Rectangle3D rect in area)
            {
                if (rect.Contains(loc))
                    return true;
            }

            return false;
        }

        private string m_ID;
        private Map m_Map;
        private int m_MarkerOffset, m_MarkerIDWater, m_MarkerIDLand;
        private Rectangle3D[] m_Area;
        private Rectangle2D[] m_WrapBounds;
        private CityInfo[] m_StartingCities;
        private PMList m_PublicMoongateList;
        private Point3D[] m_UnstuckLocations;
        private Point2D[] m_StrandednessLocations;
        private string m_DecorationFolder;

        public string ID
        {
            get { return m_ID; }
            set { m_ID = value; }
        }

        public Map Map
        {
            get { return m_Map; }
            set { m_Map = value; }
        }

        public int MarkerOffset
        {
            get { return m_MarkerOffset; }
            set { m_MarkerOffset = value; }
        }

        public int MarkerIDLand
        {
            get { return m_MarkerIDLand; }
            set { m_MarkerIDLand = value; }
        }

        public int MarkerIDWater
        {
            get { return m_MarkerIDWater; }
            set { m_MarkerIDWater = value; }
        }

        public Rectangle3D[] Area
        {
            get { return m_Area; }
            set { m_Area = value; }
        }

        public Rectangle2D[] WrapBounds
        {
            get { return m_WrapBounds; }
            set { m_WrapBounds = value; }
        }

        public CityInfo[] StartingCities
        {
            get { return m_StartingCities; }
            set { m_StartingCities = value; }
        }

        public PMList PublicMoongateList
        {
            get { return m_PublicMoongateList; }
            set { m_PublicMoongateList = value; }
        }

        public Point3D[] UnstuckLocations
        {
            get { return m_UnstuckLocations; }
            set { m_UnstuckLocations = value; }
        }

        public Point2D[] StrandednessLocations
        {
            get { return m_StrandednessLocations; }
            set { m_StrandednessLocations = value; }
        }

        public string DecorationFolder
        {
            get { return m_DecorationFolder; }
            set { m_DecorationFolder = value; }
        }

        public WorldZone(
            string id,
            Map map,
            int markerOffset,
            int markerIDLand,
            int markerIDWater,
            Rectangle3D[] area,
            Rectangle2D[] wrapBounds,
            CityInfo[] startingCities,
            PMList publicMoongateList,
            Point3D[] unstuckLocations,
            Point2D[] strandednessLocations,
            string decorationFolder)
        {
            m_ID = id;
            m_Map = map;
            m_MarkerOffset = markerOffset;
            m_MarkerIDLand = markerIDLand;
            m_MarkerIDWater = markerIDWater;
            m_Area = area;
            m_WrapBounds = wrapBounds;
            m_StartingCities = startingCities;
            m_PublicMoongateList = publicMoongateList;
            m_UnstuckLocations = unstuckLocations;
            m_StrandednessLocations = strandednessLocations;
            m_DecorationFolder = decorationFolder;
        }

        private static readonly PMList[] m_PublicMoongateLists = new PMList[1];

        public PMList[] GetPMLists()
        {
            m_PublicMoongateLists[0] = m_PublicMoongateList;

            return m_PublicMoongateLists;
        }

        #region Save/Load Zone

        private static string DataDirectory
        {
            get { return Path.Join(Core.DataDirectory, "WorldZone"); }
        }

        public static void SaveZone(WorldZone zone, Mobile from = null)
        {
            try
            {
                if (!Directory.Exists(DataDirectory))
                    Directory.CreateDirectory(DataDirectory);

                XmlDocument xmlDoc = new XmlDocument();

                XmlElement root = xmlDoc.CreateElement("WorldZone");

                root.AppendChild(XmlWriteText(xmlDoc, "ID", zone.ID));
                root.AppendChild(XmlWriteText(xmlDoc, "Map", zone.Map.Name));
                root.AppendChild(XmlWriteText(xmlDoc, "MarkerOffset", zone.MarkerOffset.ToString()));
                root.AppendChild(XmlWriteText(xmlDoc, "MarkerIDLand", zone.MarkerIDLand.ToString()));
                root.AppendChild(XmlWriteText(xmlDoc, "MarkerIDWater", zone.MarkerIDWater.ToString()));

                XmlElement xmlElem = xmlDoc.CreateElement("Area");

                foreach (Rectangle3D rect in zone.Area)
                    xmlElem.AppendChild(XmlWriteRectangle3D(xmlDoc, rect));

                root.AppendChild(xmlElem);

                xmlElem = xmlDoc.CreateElement("WrapBounds");

                foreach (Rectangle2D rect in zone.WrapBounds)
                    xmlElem.AppendChild(XmlWriteRectangle2D(xmlDoc, rect));

                root.AppendChild(xmlElem);

                xmlElem = xmlDoc.CreateElement("StartingCities");

                foreach (CityInfo city in zone.StartingCities)
                    xmlElem.AppendChild(XmlWriteCityInfo(xmlDoc, city));

                root.AppendChild(xmlElem);

                if (zone.PublicMoongateList != null)
                    root.AppendChild(xmlElem.AppendChild(XmlWritePMList(xmlDoc, zone.PublicMoongateList)));

                xmlElem = xmlDoc.CreateElement("UnstuckLocations");

                foreach (Point3D p in zone.UnstuckLocations)
                    xmlElem.AppendChild(XmlWritePoint3D(xmlDoc, p));

                root.AppendChild(xmlElem);

                xmlElem = xmlDoc.CreateElement("StrandedLocations");

                foreach (Point2D p in zone.StrandednessLocations)
                    xmlElem.AppendChild(XmlWritePoint2D(xmlDoc, p));

                root.AppendChild(xmlElem);

                root.AppendChild(XmlWriteText(xmlDoc, "DecorationFolder", zone.DecorationFolder));

                xmlDoc.AppendChild(root);

                xmlDoc.Save(Path.Combine(DataDirectory, String.Concat(zone.ID, ".xml")));
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));

                if (from != null)
                    from.SendMessage("Error: {0}", ex.Message);
            }
        }

        public static WorldZone LoadZone(string zoneID, Mobile from = null)
        {
            WorldZone zone = null;

            try
            {
                string fileName = Path.Combine(DataDirectory, String.Concat(zoneID, ".xml"));

                if (System.IO.File.Exists(fileName))
                {
                    XmlDocument xmlDoc = new XmlDocument();

                    xmlDoc.Load(fileName);

                    XmlElement root = xmlDoc["WorldZone"];

                    string id = null;
                    Map map = null;
                    int markerOffset = 0, markerIDLand = 0, markerIDWater = 0;
                    Rectangle3D[] area = new Rectangle3D[0];
                    Rectangle2D[] wrapBounds = new Rectangle2D[0];
                    CityInfo[] startingCities = new CityInfo[0];
                    PMList publicMoongateList = null;
                    Point3D[] unstuckLocations = new Point3D[0];
                    Point2D[] strandednessLocations = new Point2D[0];
                    string decorationFolder = null;

                    foreach (XmlElement xmlElem in root)
                    {
                        switch (xmlElem.Name)
                        {
                            case "ID":
                                {
                                    id = XmlReadText(xmlElem);
                                    break;
                                }
                            case "Map":
                                {
                                    map = Map.Parse(XmlReadText(xmlElem));
                                    break;
                                }
                            case "MarkerOffset":
                                {
                                    markerOffset = int.Parse(XmlReadText(xmlElem));
                                    break;
                                }
                            case "MarkerIDLand":
                                {
                                    markerIDLand = int.Parse(XmlReadText(xmlElem));
                                    break;
                                }
                            case "MarkerIDWater":
                                {
                                    markerIDWater = int.Parse(XmlReadText(xmlElem));
                                    break;
                                }
                            case "Area":
                                {
                                    List<Rectangle3D> list = new List<Rectangle3D>();

                                    foreach (XmlElement xmlSubElem in xmlElem)
                                        list.Add(XmlReadRectangle3D(xmlSubElem));

                                    area = list.ToArray();
                                    break;
                                }
                            case "WrapBounds":
                                {
                                    List<Rectangle2D> list = new List<Rectangle2D>();

                                    foreach (XmlElement xmlSubElem in xmlElem)
                                        list.Add(XmlReadRectangle2D(xmlSubElem));

                                    wrapBounds = list.ToArray();
                                    break;
                                }
                            case "StartingCities":
                                {
                                    List<CityInfo> list = new List<CityInfo>();

                                    foreach (XmlElement xmlSubElem in xmlElem)
                                        list.Add(XmlReadCityInfo(xmlSubElem));

                                    startingCities = list.ToArray();
                                    break;
                                }
                            case "PMList":
                                {
                                    publicMoongateList = XmlReadPMList(xmlElem);
                                    break;
                                }
                            case "UnstuckLocations":
                                {
                                    List<Point3D> list = new List<Point3D>();

                                    foreach (XmlElement xmlSubElem in xmlElem)
                                        list.Add(XmlReadPoint3D(xmlSubElem));

                                    unstuckLocations = list.ToArray();
                                    break;
                                }
                            case "StrandedLocations":
                                {
                                    List<Point2D> list = new List<Point2D>();

                                    foreach (XmlElement xmlSubElem in xmlElem)
                                        list.Add(XmlReadPoint2D(xmlSubElem));

                                    strandednessLocations = list.ToArray();
                                    break;
                                }
                            case "DecorationFolder":
                                {
                                    decorationFolder = XmlReadText(xmlElem);
                                    break;
                                }
                        }
                    }

                    zone = new WorldZone(
                        id,
                        map,
                        markerOffset,
                        markerIDLand,
                        markerIDWater,
                        area,
                        wrapBounds,
                        startingCities,
                        publicMoongateList,
                        unstuckLocations,
                        strandednessLocations,
                        decorationFolder);
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));

                if (from != null)
                    from.SendMessage("Error: {0}", ex.Message);
            }

            return zone;
        }

        #endregion

        #region Save/Load Config

        private const string ConfigFileName = "Saves/WorldZoneConfig.xml";

        public static void SaveConfig()
        {
            try
            {
                string configDirectory = Path.GetDirectoryName(ConfigFileName);

                if (!Directory.Exists(configDirectory))
                    Directory.CreateDirectory(configDirectory);

                XmlDocument xmlDoc = new XmlDocument();

                XmlElement root = xmlDoc.CreateElement("WorldZoneConfig");

                root.AppendChild(XmlWriteText(xmlDoc, "ActiveZoneID", m_ActiveZone?.ID));

                xmlDoc.AppendChild(root);

                xmlDoc.Save(ConfigFileName);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        public static void LoadConfig()
        {
            string activeZoneID = null;

            try
            {
                string fileName = Path.Combine(ConfigFileName);

                if (System.IO.File.Exists(fileName))
                {
                    XmlDocument xmlDoc = new XmlDocument();

                    xmlDoc.Load(fileName);

                    XmlElement root = xmlDoc["WorldZoneConfig"];

                    foreach (XmlElement xmlElem in root)
                    {
                        switch (xmlElem.Name)
                        {
                            case "ActiveZoneID":
                                {
                                    activeZoneID = XmlReadText(xmlElem);
                                    break;
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            if (activeZoneID != null)
                ActiveZone = LoadZone(activeZoneID);
        }

        #endregion

        // TODO: Move to some common source file?
        #region XML Utility

        private static XmlElement XmlWriteText(XmlDocument xmlDoc, string elemName, string text)
        {
            XmlElement xmlElem = xmlDoc.CreateElement(elemName);
            xmlElem.InnerText = text;
            return xmlElem;
        }

        private static string XmlReadText(XmlElement xmlElem)
        {
            return xmlElem.InnerText;
        }

        private static XmlElement XmlWritePoint2D(XmlDocument xmlDoc, Point2D p)
        {
            XmlElement xmlElem = xmlDoc.CreateElement("Point2D");
            xmlElem.SetAttribute("X", p.X.ToString());
            xmlElem.SetAttribute("Y", p.Y.ToString());
            return xmlElem;
        }

        private static Point2D XmlReadPoint2D(XmlElement xmlElem)
        {
            int x = int.Parse(xmlElem.GetAttribute("X"));
            int y = int.Parse(xmlElem.GetAttribute("Y"));
            return new Point2D(x, y);
        }

        private static XmlElement XmlWritePoint3D(XmlDocument xmlDoc, Point3D p)
        {
            XmlElement xmlElem = xmlDoc.CreateElement("Point3D");
            xmlElem.SetAttribute("X", p.X.ToString());
            xmlElem.SetAttribute("Y", p.Y.ToString());
            xmlElem.SetAttribute("Z", p.Z.ToString());
            return xmlElem;
        }

        private static Point3D XmlReadPoint3D(XmlElement xmlElem)
        {
            int x = int.Parse(xmlElem.GetAttribute("X"));
            int y = int.Parse(xmlElem.GetAttribute("Y"));
            int z = int.Parse(xmlElem.GetAttribute("Z"));
            return new Point3D(x, y, z);
        }

        private static XmlElement XmlWriteRectangle2D(XmlDocument xmlDoc, Rectangle2D rect)
        {
            XmlElement xmlElem = xmlDoc.CreateElement("Rectangle2D");
            xmlElem.SetAttribute("X", rect.Start.X.ToString());
            xmlElem.SetAttribute("Y", rect.Start.Y.ToString());
            xmlElem.SetAttribute("Width", rect.Width.ToString());
            xmlElem.SetAttribute("Height", rect.Height.ToString());
            return xmlElem;
        }

        private static Rectangle2D XmlReadRectangle2D(XmlElement xmlElem)
        {
            int x = int.Parse(xmlElem.GetAttribute("X"));
            int y = int.Parse(xmlElem.GetAttribute("Y"));
            int width = int.Parse(xmlElem.GetAttribute("Width"));
            int height = int.Parse(xmlElem.GetAttribute("Height"));
            return new Rectangle2D(x, y, width, height);
        }

        private static XmlElement XmlWriteRectangle3D(XmlDocument xmlDoc, Rectangle3D rect)
        {
            XmlElement xmlElem = xmlDoc.CreateElement("Rectangle3D");
            xmlElem.SetAttribute("X", rect.Start.X.ToString());
            xmlElem.SetAttribute("Y", rect.Start.Y.ToString());
            xmlElem.SetAttribute("Z", rect.Start.Z.ToString());
            xmlElem.SetAttribute("Width", rect.Width.ToString());
            xmlElem.SetAttribute("Height", rect.Height.ToString());
            xmlElem.SetAttribute("Depth", rect.Depth.ToString());
            return xmlElem;
        }

        private static Rectangle3D XmlReadRectangle3D(XmlElement xmlElem)
        {
            int x = int.Parse(xmlElem.GetAttribute("X"));
            int y = int.Parse(xmlElem.GetAttribute("Y"));
            int z = int.Parse(xmlElem.GetAttribute("Z"));
            int width = int.Parse(xmlElem.GetAttribute("Width"));
            int height = int.Parse(xmlElem.GetAttribute("Height"));
            int depth = int.Parse(xmlElem.GetAttribute("Depth"));
            return new Rectangle3D(x, y, z, width, height, depth);
        }

        private static XmlElement XmlWriteCityInfo(XmlDocument xmlDoc, CityInfo cityInfo)
        {
            XmlElement xmlElem = xmlDoc.CreateElement("CityInfo");
            xmlElem.SetAttribute("City", cityInfo.City);
            xmlElem.SetAttribute("Building", cityInfo.Building);
            xmlElem.SetAttribute("X", cityInfo.X.ToString());
            xmlElem.SetAttribute("Y", cityInfo.Y.ToString());
            xmlElem.SetAttribute("Z", cityInfo.Z.ToString());
            xmlElem.SetAttribute("Map", cityInfo.Map.Name);
            return xmlElem;
        }

        private static CityInfo XmlReadCityInfo(XmlElement xmlElem)
        {
            string city = xmlElem.GetAttribute("City");
            string building = xmlElem.GetAttribute("Building");
            int x = int.Parse(xmlElem.GetAttribute("X"));
            int y = int.Parse(xmlElem.GetAttribute("Y"));
            int z = int.Parse(xmlElem.GetAttribute("Z"));
            Map map = Map.Parse(xmlElem.GetAttribute("Map"));
            return new CityInfo(city, building, x, y, z, map);
        }

        private static XmlElement XmlWritePMList(XmlDocument xmlDoc, PMList list)
        {
            XmlElement xmlElem = xmlDoc.CreateElement("PMList");
            xmlElem.SetAttribute("Number", list.Number.ToString());
            xmlElem.SetAttribute("SelNumber", list.SelNumber.ToString());
            xmlElem.SetAttribute("Map", list.Map.Name);
            XmlElement xmlSubElem = xmlDoc.CreateElement("Entries");
            foreach (PMEntry entry in list.Entries)
                xmlSubElem.AppendChild(XmlWritePMEntry(xmlDoc, entry));
            xmlElem.AppendChild(xmlSubElem);
            return xmlElem;
        }

        private static PMList XmlReadPMList(XmlElement xmlElem)
        {
            TextEntry number = TextEntry.Parse(xmlElem.GetAttribute("Number"));
            TextEntry selNumber = TextEntry.Parse(xmlElem.GetAttribute("SelNumber"));
            Map map = Map.Parse(xmlElem.GetAttribute("Map"));
            List<PMEntry> entries = new List<PMEntry>();
            foreach (XmlElement xmlSubElem in xmlElem["Entries"])
                entries.Add(XmlReadPMEntry(xmlSubElem));
            return new PMList(number, selNumber, map, entries.ToArray());
        }

        private static XmlElement XmlWritePMEntry(XmlDocument xmlDoc, PMEntry entry)
        {
            XmlElement xmlElem = xmlDoc.CreateElement("PMEntry");
            xmlElem.SetAttribute("X", entry.Location.X.ToString());
            xmlElem.SetAttribute("Y", entry.Location.Y.ToString());
            xmlElem.SetAttribute("Z", entry.Location.Z.ToString());
            xmlElem.SetAttribute("Number", entry.Number.ToString());
            xmlElem.SetAttribute("Description", entry.Description.ToString());
            return xmlElem;
        }

        private static PMEntry XmlReadPMEntry(XmlElement xmlElem)
        {
            int x = int.Parse(xmlElem.GetAttribute("X"));
            int y = int.Parse(xmlElem.GetAttribute("Y"));
            int z = int.Parse(xmlElem.GetAttribute("Z"));
            TextEntry number = TextEntry.Parse(xmlElem.GetAttribute("Number"));
            TextEntry description = TextEntry.Parse(xmlElem.GetAttribute("Description"));
            return new PMEntry(new Point3D(x, y, z), number, description);
        }

        #endregion
    }

    public class WorldZoneMarker : Item
    {
        public static int Generate(WorldZone zone)
        {
            Map map = zone.Map;

            if (map == null || map == Map.Internal)
                return 0;

            int offset = zone.MarkerOffset;

            if (offset <= 0 || (zone.MarkerIDLand <= 0 && zone.MarkerIDWater <= 0))
                return 0; // we don't want markers

            List<Line2D> perimeter = CalculateOuterPerimeter(ConvertToArea2D(zone.Area));

            int added = 0;

            foreach (Line2D border in perimeter)
            {
                GenerateAt(zone, border.Start, map, ref added);

                int shift = (border.Length % offset) / 2;

                if (shift == 0)
                    shift = offset;

                for (int i = shift; i < border.Length - 1; i += offset)
                    GenerateAt(zone, border[i], map, ref added);

                GenerateAt(zone, border.End, map, ref added);
            }

            return added;
        }

        private static void GenerateAt(WorldZone zone, Point2D p2D, Map map, ref int count)
        {
            int x = p2D.X, y = p2D.Y;

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                return;

            int z;

            object surface = GetTopSurface(map, x, y, out z);

            if (surface == null)
                return;

            int id = 0;
            if (surface is LandTile)
                id = ((LandTile)surface).ID;
            else if (surface is StaticTile)
                id = ((StaticTile)surface).ID;

            Point3D p3D = new Point3D(x, y, z);

            foreach (Item item in map.GetItemsInRange(p3D, 0))
            {
                if (item.Z == z && item is WorldZoneMarker)
                    return;
            }

            if (!Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.checkBlocksFit))
                return;

            int tileID = (id & 0x3FFF);

            bool isWater =
                (tileID >= 0x00A8 && tileID <= 0x00AB) ||
                (tileID >= 0x0136 && tileID <= 0x0137) ||
                (tileID >= 0x1796 && tileID <= 0x17B2);

            int itemID = (isWater ? zone.MarkerIDWater : zone.MarkerIDLand);

            if (itemID > 0)
            {
                new WorldZoneMarker(itemID).MoveToWorld(p3D, map);
                count++;
            }
        }

        // as of Runuo 2.6, there are no more 'Tile', so we can't return such a thing.
        // we will rewrite as follows
        private static object GetTopSurface(Map map, int x, int y, out int surfaceZ)
        {
            object surface = null;
            surfaceZ = 0;

            LandTile landTile = map.Tiles.GetLandTile(x, y);

            if (!landTile.Ignored)
            {
                surface = landTile;
                surfaceZ = map.GetAverageZ(x, y);
            }

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, false);

            foreach (StaticTile tile in staticTiles)
            {
                ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                if (id.Surface || id.Flags.HasFlag(TileFlag.Wet))
                {
                    int tileZ = tile.Z + id.CalcHeight;

                    if (tileZ >= surfaceZ) // accept equal case to obtain the last drawn tile
                    {
                        surface = tile;
                        surfaceZ = tileZ;
                    }
                }
            }

            return surface;
        }

        public static int DeleteAll()
        {
            List<Item> list = new List<Item>();

            foreach (Item item in World.Items.Values)
            {
                if (item is WorldZoneMarker)
                    list.Add(item);
            }

            foreach (Item item in list)
                item.Delete();

            return list.Count;
        }

        // TODO: Move to a common geometry/maths file?
        #region Geometry

        public static Rectangle2D[] ConvertToArea2D(Rectangle3D[] area3D)
        {
            Rectangle2D[] area2D = new Rectangle2D[area3D.Length];

            for (int i = 0; i < area3D.Length; i++)
                area2D[i] = new Rectangle2D(area3D[i].Start, area3D[i].End);

            return area2D;
        }

        private static List<Line2D> CalculateOuterPerimeter(Rectangle2D[] area)
        {
            Rectangle2D[] rects = new Rectangle2D[area.Length];

            // 1. Pad the rectangles by 1 on the top and left, we want the *outer* perimeter!
            for (int i = 0; i < area.Length; i++)
                rects[i] = new Rectangle2D(area[i].Start - new Point2D(1, 1), area[i].End);

            List<Line2D> perimeter = new List<Line2D>();

            // 2. explode rectangles into line segments
            for (int i = 0; i < rects.Length; i++)
                Explode(rects[i], perimeter);

            // 3. remove duplicate line segments
            for (int i = 0; i < perimeter.Count; i++)
            {
                for (int j = i + 1; j < perimeter.Count; j++)
                {
                    if (perimeter[i] == perimeter[j])
                        perimeter.RemoveAt(j--);
                }
            }

            // 4. split intersecting line segments into non-intersecting line segments
            for (int i = 0; i < perimeter.Count; i++)
            {
                for (int j = i + 1; j < perimeter.Count; j++)
                {
                    if (Split(perimeter[i], perimeter[j], perimeter))
                    {
                        perimeter.RemoveAt(j);
                        perimeter.RemoveAt(i--);
                        break;
                    }
                }
            }

            // 5. remove line segments that fully overlap with one of the rectangles
            for (int i = perimeter.Count - 1; i >= 0; i--)
            {
                Line2D line = perimeter[i];

                Point2D p1 = line.Start;
                Point2D p2 = line.Start + line.Offset;

                foreach (Rectangle2D rect in rects)
                {
                    int x1 = rect.Start.X, x2 = rect.End.X;
                    int y1 = rect.Start.Y, y2 = rect.End.Y;

                    bool fullyOverlap;

                    if (line.Offset.Align == Align.X)
                        fullyOverlap = (p1.Y > y1 && p1.Y < y2 && p1.X >= x1 && p2.X <= x2);
                    else
                        fullyOverlap = (p1.X > x1 && p1.X < x2 && p1.Y >= y1 && p2.Y <= y2);

                    if (fullyOverlap)
                    {
                        perimeter.RemoveAt(i);
                        break;
                    }
                }
            }

            // 6. join line segments together
            for (int i = 0; i < perimeter.Count; i++)
            {
                for (int j = i + 1; j < perimeter.Count; j++)
                {
                    if (Join(perimeter[i], perimeter[j], perimeter))
                    {
                        perimeter.RemoveAt(j);
                        perimeter.RemoveAt(i--);
                        break;
                    }
                }
            }

            return perimeter;
        }

        private static void Explode(Rectangle2D rect, List<Line2D> outList)
        {
            int x1 = rect.Start.X, x2 = rect.End.X;
            int y1 = rect.Start.Y, y2 = rect.End.Y;

            outList.Add(new Line2D(new Point2D(x1, y1), new Offset2D(Align.X, x2 - x1)));
            outList.Add(new Line2D(new Point2D(x1, y2), new Offset2D(Align.X, x2 - x1)));
            outList.Add(new Line2D(new Point2D(x1, y1), new Offset2D(Align.Y, y2 - y1)));
            outList.Add(new Line2D(new Point2D(x2, y1), new Offset2D(Align.Y, y2 - y1)));
        }

        private static bool Split(Line2D line1, Line2D line2, List<Line2D> outList)
        {
            if (line1.Offset.Align != line2.Offset.Align)
            {
                Line2D hLine = (line1.Offset.Align == Align.X) ? line1 : line2;
                Line2D vLine = (line1.Offset.Align == Align.X) ? line2 : line1;

                int xMin = hLine.Start.X, xMax = hLine.End.X, xInt = vLine.Start.X;
                int yMin = vLine.Start.Y, yMax = vLine.End.Y, yInt = hLine.Start.Y;

                bool intersect = (xInt > xMin && xInt < xMax && yInt > yMin && yInt < yMax);

                if (intersect)
                {
                    outList.Add(new Line2D(new Point2D(xMin, yInt), new Offset2D(Align.X, xInt - xMin)));
                    outList.Add(new Line2D(new Point2D(xInt, yInt), new Offset2D(Align.X, xMax - xInt)));
                    outList.Add(new Line2D(new Point2D(xInt, yMin), new Offset2D(Align.Y, yInt - yMin)));
                    outList.Add(new Line2D(new Point2D(xInt, yInt), new Offset2D(Align.Y, yMax - yInt)));
                    return true;
                }
            }
            else if (line1.Offset.Align == Align.X)
            {
                if (line1.Start.Y == line2.Start.Y)
                {
                    if (line1.Start.X > line2.Start.X)
                        Swap(ref line1, ref line2);

                    int x1Min = line1.Start.X, x1Max = line1.End.X;
                    int x2Min = line2.Start.X, x2Max = line2.End.X;

                    bool overlap = (x2Min >= x1Min && x2Min < x1Max);

                    if (overlap)
                    {
                        if (x2Min - x1Min != 0)
                            outList.Add(new Line2D(new Point2D(x1Min, line1.Start.Y), new Offset2D(Align.X, x2Min - x1Min)));

                        if (x1Max - x2Min != 0)
                            outList.Add(new Line2D(new Point2D(x2Min, line1.Start.Y), new Offset2D(Align.X, x1Max - x2Min)));

                        outList.Add(new Line2D(new Point2D(x1Max, line1.Start.Y), new Offset2D(Align.X, x2Max - x1Max)));
                        return true;
                    }
                }
            }
            else if (line1.Offset.Align == Align.Y)
            {
                if (line1.Start.X == line2.Start.X)
                {
                    if (line1.Start.Y > line2.Start.Y)
                        Swap(ref line1, ref line2);

                    int y1Min = line1.Start.Y, y1Max = line1.End.Y;
                    int y2Min = line2.Start.Y, y2Max = line2.End.Y;

                    bool overlap = (y2Min >= y1Min && y2Min < y1Max);

                    if (overlap)
                    {
                        if (y2Min - y1Min != 0)
                            outList.Add(new Line2D(new Point2D(line1.Start.X, y1Min), new Offset2D(Align.Y, y2Min - y1Min)));

                        if (y1Max - y2Min != 0)
                            outList.Add(new Line2D(new Point2D(line1.Start.X, y2Min), new Offset2D(Align.Y, y1Max - y2Min)));

                        outList.Add(new Line2D(new Point2D(line1.Start.X, y1Max), new Offset2D(Align.Y, y2Max - y1Max)));
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool Join(Line2D line1, Line2D line2, List<Line2D> outList)
        {
            if (line1.Offset.Align != line2.Offset.Align)
            {
                // lines with different alignments cannot be joined, do nothing
            }
            else if (line1.Offset.Align == Align.X)
            {
                if (line1.Start.Y == line2.Start.Y)
                {
                    if (line1.Start.X > line2.Start.X)
                        Swap(ref line1, ref line2);

                    int x1Min = line1.Start.X, x1Max = line1.End.X;
                    int x2Min = line2.Start.X, x2Max = line2.End.X;

                    bool connect = (x1Max == x2Min);

                    if (connect)
                    {
                        outList.Add(new Line2D(new Point2D(x1Min, line1.Start.Y), new Offset2D(Align.X, x2Max - x1Min)));
                        return true;
                    }
                }
            }
            else if (line1.Offset.Align == Align.Y)
            {
                if (line1.Start.X == line2.Start.X)
                {
                    if (line1.Start.Y > line2.Start.Y)
                        Swap(ref line1, ref line2);

                    int y1Min = line1.Start.Y, y1Max = line1.End.Y;
                    int y2Min = line2.Start.Y, y2Max = line2.End.Y;

                    bool connect = (y1Max == y2Min);

                    if (connect)
                    {
                        outList.Add(new Line2D(new Point2D(line1.Start.X, y1Min), new Offset2D(Align.Y, y2Max - y1Min)));
                        return true;
                    }
                }
            }

            return false;
        }

        private static void Swap<T>(ref T t1, ref T t2)
        {
            T temp = t1;
            t1 = t2;
            t2 = temp;
        }

        private enum Align : byte { X, Y }

        private struct Line2D
        {
            public readonly Point2D Start;
            public readonly Offset2D Offset;

            public int Length { get { return Offset.Value + 1; } }
            public Point2D End { get { return Start + Offset; } }

            public Point2D this[int index]
            {
                get
                {
                    if (index < 0 || index >= Length)
                        throw new IndexOutOfRangeException();

                    return Start + new Offset2D(Offset.Align, index);
                }
            }

            public Line2D(Point2D start, Offset2D offset)
            {
                Start = start;
                Offset = offset;
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Line2D))
                    return false;

                Line2D other = (Line2D)obj;

                return (this.Start == other.Start && this.Offset == other.Offset);
            }

            public override int GetHashCode()
            {
                return (Start.GetHashCode() ^ Offset.GetHashCode());
            }

            public static bool operator ==(Line2D l, Line2D r)
            {
                return (l.Start == r.Start && l.Offset == r.Offset);
            }

            public static bool operator !=(Line2D l, Line2D r)
            {
                return (l.Start != r.Start || l.Offset != r.Offset);
            }

            public override string ToString()
            {
                return String.Concat(Start.ToString(), '+', Offset.ToPoint2D().ToString());
            }
        }

        private struct Offset2D
        {
            public readonly Align Align;
            public readonly int Value;

            public Offset2D(Align align, int value)
            {
                Align = align;
                Value = value;
            }

            public Point2D ToPoint2D()
            {
                return (Align == Align.X ? new Point2D(Value, 0) : new Point2D(0, Value));
            }

            public override bool Equals(object obj)
            {
                if (!(obj is Offset2D))
                    return false;

                Offset2D other = (Offset2D)obj;

                return (this.ToPoint2D() == other.ToPoint2D());
            }

            public override int GetHashCode()
            {
                return (Align.GetHashCode() ^ Value.GetHashCode());
            }

            public static bool operator ==(Offset2D l, Offset2D r)
            {
                return (l.ToPoint2D() == r.ToPoint2D());
            }

            public static bool operator !=(Offset2D l, Offset2D r)
            {
                return (l.ToPoint2D() != r.ToPoint2D());
            }

            public static Point2D operator +(Point2D p, Offset2D o)
            {
                return (p + o.ToPoint2D());
            }

            public static Point2D operator -(Point2D p, Offset2D o)
            {
                return (p - o.ToPoint2D());
            }

            public override string ToString()
            {
                return ToPoint2D().ToString();
            }
        }

        #endregion

        [Constructable]
        public WorldZoneMarker(int itemID)
            : base(itemID)
        {
            Movable = false;
        }

        public WorldZoneMarker(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }
    }
}