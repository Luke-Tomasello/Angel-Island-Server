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

/* Scripts/Multis/StaticHousing/StaticHouseHelper.cs
 *  Changelog:
 *  8/16/22, Adam
 *      Add string.IsNullOrEmpty checks for the XML convert functions .. saves always using exceptions to handle it.
 *  6/10/22, Yoar
 *      Added a little feedback message on [ReloadBlueprintCache
 *  6/7/22, Yoar
 *      - Removed old code related to static house region editing.
 *      - Moved [ToAddon command to a separate file in the Commands folder.
 *  6/5/22, Yoar
 *      - Disabled commands: WipeRegions, AddRegion, DumpRegions.
 *        Edit the static house's region rectangles via [props instead.
 *      - Moved StaticHouseHelper.TransferData to StaticHouse.
 *  6/4/22, Yoar
 *      Added support for separate static housing files, each file container a single design.
 *      These files can be found in the "StaticHousingProd" and "StaticHousingTest" folders.
 *  6/4/22, Yoar
 *      - More cleanups + refactoring.
 *      - Simplified GetFoundationID(int width, int height)... Now using maths!
 *      - Reorganized HouseBlueprint data structure: properties appear in the same order
 *        as their corresponding fields.
 *      - Added HouseBlueprint.ToXml(XMLElement xmlHouseEleme). This method now deals with
 *        exporting the blueprint to XML.
 *  6/3/22, Yoar
 *      Refactored HouseBlueprint data structure + constructor
 *      Added support for house doors
 *  4/30/22, Yoar
 *      Added additional/optional data which includes hue and name.
 *      Tiles additional data are added as addon components in the FixerAddon.
 *  9/17/21, Yoar
 *      Static housing revamp: Refactored completely. Now keeping two separate blueprint
 *      databases: One containing the production shard housing and one containing the
 *      Test Center static housing. Production shards do not load-in Test Center static
 *      housing.
 *	12/28/07 Taran Kain
 *		Added Doubled list and flag for doubled-up tiles
 *		Added FixerAddon object to add them back into a house after being filtered out
 *	8/11/07, Adam
 *		Replace 10000000 with PriceError constant
 *	8/2/07, Adam
 *		Add calls to new Assert() function to track down the Exceptions we are seeing:
 *			Log start : 8/1/2007 1:36:43 AM
 *			Botched Static Housing xml.
 *			Object reference not set to an instance of an object.
 *			at Server.Multis.StaticHousing.StaticHouseHelper.TransferData(StaticHouse sh)
 *		I'll remove these calls later, and my hunch is that house.Region is null.
 *	6/25/07, Adam
 *      - Major changes, please SR/MR for full details
 *		- Add new nodes to the recognized XML node list
 *  6/22/07, Adam
 *      Add BasePrice calculation function (based on plot size only)
 *  6/11/07, Pix
 *      Added GetAllStaticHouseDescriptions() and StaticHouseDescription class to help
 *      the architect get the deeds/etc for all the available houses.
 *  06/08/2007, plasma
 *      Initial creation
 */

using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Server.Multis.StaticHousing
{
    /// <summary>
    /// Hosts functions to read/write/lookup house blueprints
    /// </summary>
    public static class StaticHouseHelper
    {
        public static string FileNameProd = Path.Combine(Core.DataDirectory, "StaticHousingProd.xml");
        public static string FileNameTest = Path.Combine(Core.DataDirectory, "StaticHousingTC.xml");

        // folders containing XML files of individual designs
        public static string FolderProd = Path.Combine(Core.DataDirectory, "StaticHousingProd");
        public static string FolderTest = Path.Combine(Core.DataDirectory, "StaticHousingTC");

        public static string ExportFile { get { return FileNameTest; } }
        public static string ExportFolder { get { return FolderTest; } }

        private static HouseBlueprintDatabase m_DatabaseProd;
        private static HouseBlueprintDatabase m_DatabaseTest;

        /// <summary>
        /// If enabled, we can place house foundations using the house placement tool.
        /// But, if this is not an AOS server, we can only build house foundations in the green acres.
        /// </summary>
        public static bool HouseFoundationsEnabled { get { return Core.UOTC_CFG || Core.RuleSets.AOSRules(); } }

        public static void Configure()
        {
            InitDatabases();
            LoadDatabases();
        }

        public static void Initialize()
        {
            CommandSystem.Register("ReloadBlueprintCache", AccessLevel.Administrator, new CommandEventHandler(OnReloadBlueprintCache));
        }

        [Usage("ReloadBlueprintCache")]
        [Description("Reload the blueprint cache.")]
        private static void OnReloadBlueprintCache(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Reloading static housing blueprints.");

            LoadDatabases();
        }

        private static void InitDatabases()
        {
            m_DatabaseProd = new HouseBlueprintDatabase(FileNameProd, FolderProd);
            m_DatabaseTest = new HouseBlueprintDatabase(FileNameTest, FolderTest);
        }

        private static void LoadDatabases()
        {
            m_DatabaseProd.ReloadAll();

            if (Core.UOTC_CFG)
                m_DatabaseTest.ReloadAll();

            GenerateHouseDeedInfo();
            GeneratePlacementEntries();
        }

        [Flags]
        public enum LookupFlags : byte
        {
            None = 0x0,
            Cache = 0x1,
            File = 0x2,
            Both = 0x3,
        }

        public static HouseBlueprint GetBlueprint(string houseID)
        {
            return GetBlueprint(houseID, LookupFlags.Both);
        }

        public static HouseBlueprint GetBlueprint(string houseID, LookupFlags flags)
        {
            HouseBlueprint blueprint = null;

            // let's exhaust all of our caches before we read any file
            if (flags.HasFlag(LookupFlags.Cache))
            {
                if (blueprint == null && Core.UOTC_CFG)
                    blueprint = m_DatabaseTest.Get(houseID, LookupFlags.Cache);

                if (blueprint == null)
                    blueprint = m_DatabaseProd.Get(houseID, LookupFlags.Cache);
            }

            if (flags.HasFlag(LookupFlags.File))
            {
                if (blueprint == null && Core.UOTC_CFG)
                    blueprint = m_DatabaseTest.Get(houseID, LookupFlags.File);

                if (blueprint == null)
                    blueprint = m_DatabaseProd.Get(houseID, LookupFlags.File);
            }

            return blueprint;
        }

        public static bool BlueprintExists(string houseID)
        {
            return BlueprintExists(houseID, LookupFlags.Both);
        }

        public static bool BlueprintExists(string houseID, LookupFlags flags)
        {
            bool exists = false;

            // let's exhaust all of our caches before we read any file
            if (flags.HasFlag(LookupFlags.Cache))
            {
                if (!exists && Core.UOTC_CFG)
                    exists = m_DatabaseTest.Contains(houseID, LookupFlags.Cache);

                if (!exists)
                    exists = m_DatabaseProd.Contains(houseID, LookupFlags.Cache);
            }

            if (flags.HasFlag(LookupFlags.File))
            {
                if (!exists && Core.UOTC_CFG)
                    exists = m_DatabaseTest.Contains(houseID, LookupFlags.File);

                if (!exists)
                    exists = m_DatabaseProd.Contains(houseID, LookupFlags.File);
            }

            return exists;
        }

        public static string GetNewHouseID()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(2007, 1, 1);

            return string.Concat("HID", ((uint)ts.TotalMinutes).ToString("X"));
        }

        public static string GetDescription(string houseID)
        {
            HouseBlueprint blueprint = GetBlueprint(houseID);

            if (blueprint == null)
                return "static house";

            return blueprint.Description;
        }

        public static int PriceError = 10000000; // 10 million if error

        public static int GetPrice(string houseID)
        {
            HouseBlueprint blueprint = GetBlueprint(houseID);

            if (blueprint == null)
                return PriceError;

            return blueprint.Price;
        }

        public static int GetFoundationPrice(int width, int height)
        {
            // http://i6.photobucket.com/albums/y236/squired/Custom-House-Pricing.jpg
            return ((height - 7) * 9200 + 2500) * (width - 7) + (height - 7) * 2500 + 65000;
        }

        public static FixerAddon BuildFixerAddon(HouseBlueprint blueprint)
        {
            List<TileEntry> patchTiles = new List<TileEntry>();

            foreach (TileEntry te in blueprint.TileList)
            {
                if (te.m_flags.HasFlag(TileType.Overlap) || te.m_flags.HasFlag(TileType.Patch))
                    patchTiles.Add(te);
            }

            return new FixerAddon(patchTiles);
        }

        public sealed class FixerAddon : BaseAddon
        {
            public override bool ShareHue { get { return false; } }

            public FixerAddon(List<TileEntry> tiles)
            {
                foreach (TileEntry te in tiles)
                {
                    AddonComponent c = new AddonComponent((int)te.m_id);

                    if (te.m_hue != 0)
                        c.Hue = te.m_hue;

                    if (te.m_name != null)
                        c.Name = te.m_name;

                    AddComponent(c, te.m_xOffset, te.m_yOffset, te.m_zOffset);
                }
            }

            public FixerAddon(Serial s)
                : base(s)
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

            public override void OnChop(Mobile from)
            {
                // cannot chop the fixer addon
            }
        }

        public static MultiComponentList BuildMCL(HouseBlueprint blueprint) // TODO: Cache the MCL?
        {
            MultiComponentList mcl = new MultiComponentList(MultiComponentList.Empty);

            List<MultiTileEntry> allTiles = new List<MultiTileEntry>();

            foreach (TileEntry te in blueprint.TileList)
            {
                if (te.m_flags.HasFlag(TileType.Overlap) || te.m_flags.HasFlag(TileType.Patch))
                    continue;

                allTiles.Add(new MultiTileEntry((ushort)te.m_id, te.m_xOffset, te.m_yOffset, te.m_zOffset, 0));
            }

            mcl.List = allTiles.ToArray();
            mcl.Center = new Point2D(-mcl.Min.X, -mcl.Min.Y);
            mcl.Width = (mcl.Max.X - mcl.Min.X) + 1;
            mcl.Height = (mcl.Max.Y - mcl.Min.Y) + 1;

            mcl.Tiles = new StaticTile[mcl.Width][][];

            for (int x = 0; x < mcl.Width; ++x)
            {
                mcl.Tiles[x] = new StaticTile[mcl.Height][];

                for (int y = 0; y < mcl.Height; ++y)
                    mcl.Tiles[x][y] = new StaticTile[0];
            }

            return mcl;
        }

        public static BaseDoor[] BuildDoors(HouseBlueprint blueprint)
        {
            List<BaseDoor> doors = new List<BaseDoor>();

            foreach (DoorEntry de in blueprint.DoorList)
            {
                DoorHelper.DoorInfo info = DoorHelper.GetInfo(de.m_doorType);

                BaseDoor door = new GenericHouseDoor(de.m_facing, info.BaseItemID, info.BaseSoundID, info.BaseSoundID + 7);

                door.Location = new Point3D(de.m_xOffset, de.m_yOffset, de.m_zOffset);

                foreach (BaseDoor link in doors)
                {
                    if (door.Z == link.Z && (
                        (door.X == link.X && (door.Y == link.Y - 1 || door.Y == link.Y + 1)) ||
                        (door.Y == link.Y && (door.X == link.X - 1 || door.X == link.X + 1))))
                    {
                        door.Link = link;
                        link.Link = door;
                    }
                }

                doors.Add(door);
            }

            return doors.ToArray();
        }

        private static StaticHouseDeedInfo[] m_HouseDeedInfo = new StaticHouseDeedInfo[0];

        public static StaticHouseDeedInfo[] HouseDeedInfo { get { return m_HouseDeedInfo; } }

        public static void GenerateHouseDeedInfo()
        {
            List<StaticHouseDeedInfo> list = new List<StaticHouseDeedInfo>();

            foreach (HouseBlueprint blueprint in m_DatabaseProd.Table.Values)
            {
                string description = string.Format("{0} by {1}", blueprint.Description, blueprint.OriginalOwnerName);

                list.Add(new StaticHouseDeedInfo(blueprint.ID, description, blueprint.Price));
            }

            m_HouseDeedInfo = list.ToArray();
        }

        public class StaticHouseDeedInfo
        {
            public readonly string ID;
            public readonly string Description;
            public readonly int Price;

            public StaticHouseDeedInfo(string id, string desc, int price)
            {
                ID = id;
                Description = desc;
                Price = price;
            }
        }

        private static IHousePlacementEntry[] m_PlacementEntriesProd = new IHousePlacementEntry[0];
        private static IHousePlacementEntry[] m_PlacementEntriesTest = new IHousePlacementEntry[0];

        public static IHousePlacementEntry[] PlacementEntriesProd { get { return m_PlacementEntriesProd; } }
        public static IHousePlacementEntry[] PlacementEntriesTest { get { return m_PlacementEntriesTest; } }

        private static void GeneratePlacementEntries()
        {
            m_PlacementEntriesProd = GeneratePlacementEntries(m_DatabaseProd);

            if (Core.UOTC_CFG)
                m_PlacementEntriesTest = GeneratePlacementEntries(m_DatabaseTest);
        }

        private static StaticHousePlacementEntry[] GeneratePlacementEntries(HouseBlueprintDatabase db)
        {
            List<StaticHousePlacementEntry> list = new List<StaticHousePlacementEntry>();

            foreach (HouseBlueprint blueprint in db.Table.Values)
                list.Add(new StaticHousePlacementEntry(blueprint.ID));

            return list.ToArray();
        }

        private class StaticHousePlacementEntry : IHousePlacementEntry
        {
            private string m_HouseID;

            public string HouseID { get { return m_HouseID; } }

            public int Cost { get { return GetPrice(m_HouseID); } }
            public int MultiID { get { return GetFoundationID(m_HouseID); } }
            public Point3D Offset { get { return new Point3D(0, 4, 0); } }

            public StaticHousePlacementEntry(string id)
            {
                m_HouseID = id;
            }

            public void AddToGump(Gump g, int y)
            {
                HouseBlueprint blueprint = GetBlueprint(m_HouseID);

                if (blueprint == null)
                {
                    g.AddHtml(50, y, 225, 20, string.Format("<BASEFONT COLOR=#FFFFFF>NOT FOUND ({0})</BASEFONT>", m_HouseID), false, false);
                    return;
                }

                string description = string.Format("<BASEFONT COLOR=#FFFFFF>{0} by {1}</BASEFONT>", blueprint.Description, blueprint.OriginalOwnerName);

                g.AddHtml(50, y, 225, 20, description, false, false);
                g.AddLabel(275, y, HousePlacementListGump.LabelHue, "2"); // TODO
                g.AddLabel(350, y, HousePlacementListGump.LabelHue, "270"); // TODO
                g.AddLabel(425, y, HousePlacementListGump.LabelHue, blueprint.Price.ToString());
            }

            public bool OnPlacement(Mobile from, Point3D p)
            {
                return HousePlacementEntry.OnPlacement(this, from, p);
            }

            public PreviewHouse GetPreview()
            {
                HouseBlueprint blueprint = GetBlueprint(m_HouseID);

                MultiComponentList mcl;

                if (blueprint == null)
                    mcl = MultiComponentList.Empty;
                else
                    mcl = BuildMCL(blueprint);

                return new DesignedPreviewHouse(GetFoundationID(m_HouseID), mcl);
            }

            public BaseHouse ConstructHouse(Mobile from)
            {
                HouseBlueprint blueprint = GetBlueprint(m_HouseID);

                if (blueprint == null)
                    return null;

                return new StaticHouse(from, blueprint);
            }
        }

        /// <summary>
        /// A house preview with a design state.
        /// </summary>
        private class DesignedPreviewHouse : PreviewHouse, IDesignState
        {
            private DesignState m_Design;

            public int LastRevision
            {
                get { return 0; }
                set { }
            }

            public DesignedPreviewHouse(int multiID, MultiComponentList mcl)
                : base(multiID)
            {
                m_Design = new DesignState(this, mcl);
            }

            public void SendDesignGeneral(NetState state)
            {
                m_Design.SendGeneralInfoTo(state);
            }

            public void SendDesignDetails(NetState state)
            {
                m_Design.SendDetailedInfoTo(state);
            }

            public override void SendInfoTo(NetState state, bool sendOplPacket)
            {
                base.SendInfoTo(state, sendOplPacket);

                SendDesignGeneral(state);
            }

            public DesignedPreviewHouse(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0); // version

                Delete();
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        /// <summary>
        /// Data structure for holding a house blueprint table
        /// </summary>
        private class HouseBlueprintDatabase
        {
            private string m_FileName;
            private string m_Folder;

            private Dictionary<string, HouseBlueprint> m_Table;

            public Dictionary<string, HouseBlueprint> Table { get { return m_Table; } }

            public HouseBlueprintDatabase(string fileName, string folder)
            {
                m_FileName = fileName;
                m_Folder = folder;
                m_Table = new Dictionary<string, HouseBlueprint>();
            }

            public HouseBlueprint Get(string houseID)
            {
                return Get(houseID, LookupFlags.Both);
            }

            public HouseBlueprint Get(string houseID, LookupFlags flags)
            {
                if (houseID == null)
                    return null;

                if (flags.HasFlag(LookupFlags.Cache))
                {
                    HouseBlueprint blueprint;

                    if (m_Table.TryGetValue(houseID, out blueprint))
                        return blueprint;
                }

                if (flags.HasFlag(LookupFlags.File))
                {
                    XmlElement xmlHouseElem = Load(houseID);

                    if (xmlHouseElem != null)
                        return m_Table[houseID] = new HouseBlueprint(xmlHouseElem);
                }

                return null;
            }

            public bool Contains(string houseID)
            {
                return Contains(houseID, LookupFlags.Both);
            }

            public bool Contains(string houseID, LookupFlags flags)
            {
                if (houseID == null)
                    return false;

                if (flags.HasFlag(LookupFlags.Cache))
                    return m_Table.ContainsKey(houseID);

                if (flags.HasFlag(LookupFlags.File))
                    return (Load(houseID) != null);

                return false;
            }

            public void ReloadAll()
            {
                m_Table.Clear();

                Console.WriteLine("Loading static housing blueprints from \"{0}\"...", Utility.GetShortPath(m_FileName));

                List<string> fileNames = new List<string>();

                if (File.Exists(m_FileName))
                    fileNames.Add(m_FileName);

                if (Directory.Exists(m_Folder))
                {
                    string[] subFiles = new string[0];

                    try
                    {
                        subFiles = Directory.GetFiles(m_Folder);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                    }

                    fileNames.AddRange(subFiles);
                }

                foreach (string fileName in fileNames)
                {
                    try
                    {
                        XmlDocument xmlDoc = new XmlDocument();

                        xmlDoc.Load(fileName);

                        foreach (XmlElement xmlHouseElem in xmlDoc["StaticHousing"].GetElementsByTagName("HouseID"))
                        {
                            HouseBlueprint blueprint = new HouseBlueprint(xmlHouseElem);

                            m_Table[blueprint.ID] = blueprint;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex, string.Format("Error while reading static housing blueprints from \"{0}\".", fileName));
                    }
                }
            }

            public XmlElement Load(string houseID)
            {
                List<string> fileNames = new List<string>();

                if (File.Exists(m_FileName))
                    fileNames.Add(m_FileName);

                if (Directory.Exists(m_Folder))
                {
                    string[] subFiles = new string[0];

                    try
                    {
                        subFiles = Directory.GetFiles(m_Folder);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex);
                    }

                    foreach (string fileName in subFiles)
                    {
                        if (Path.GetFileName(fileName).StartsWith(houseID))
                            fileNames.Add(fileName);
                    }
                }

                foreach (string fileName in fileNames)
                {
                    try
                    {
                        XmlDocument xmlDoc = new XmlDocument();

                        xmlDoc.Load(fileName);

                        foreach (XmlElement xmlHouseElem in xmlDoc["StaticHousing"].GetElementsByTagName("HouseID"))
                        {
                            if (xmlHouseElem["id"].InnerText == houseID)
                                return xmlHouseElem;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.LogException(ex, string.Format("Error while reading static housing blueprints from \"{0}\".", fileName));
                    }
                }

                return null;
            }
        }

        [Flags]
        public enum TileType
        {
            Normal = 0x00,
            Outside = 0x01, // used for tiles that outside the house plot such as steps
            Overlap = 0x02, // used for tiles that overlap with other tiles
            Patch = 0x04,   // used for tiles that fail to display and should be added back in via the fixer addon
        }

        public struct TileEntry
        {
            public readonly short m_xOffset;
            public readonly short m_yOffset;
            public readonly short m_zOffset;
            public readonly ushort m_id;
            public readonly ushort m_hue;
            public readonly string m_name;
            public readonly TileType m_flags;

            public TileEntry(short xOffset, short yOffset, short zOffset, ushort id, ushort hue, string name, TileType flags)
            {
                m_xOffset = xOffset;
                m_yOffset = yOffset;
                m_zOffset = zOffset;
                m_id = id;
                m_hue = hue;
                m_name = name;
                m_flags = flags;
            }
        }

        public struct DoorEntry
        {
            public readonly short m_xOffset;
            public readonly short m_yOffset;
            public readonly short m_zOffset;
            public readonly DoorType m_doorType;
            public readonly DoorFacing m_facing;

            public DoorEntry(short xOffset, short yOffset, short zOffset, DoorType doorType, DoorFacing facing)
            {
                m_xOffset = xOffset;
                m_yOffset = yOffset;
                m_zOffset = zOffset;
                m_doorType = doorType;
                m_facing = facing;
            }
        }

        /// <summary>
        /// Data structure for holding a house blueprint
        /// </summary>
        public class HouseBlueprint
        {
            private string m_ID;
            private double m_Version;
            private DateTime m_Capture;
            private string m_Description = "static house";
            private int m_Width;
            private int m_Height;
            private int m_Price = PriceError;
            private DateTime m_BuiltOn;
            private string m_OriginalOwnerName = "(unknown)";
            private Serial m_OriginalOwnerSerial = Serial.MinusOne;
            private string m_OriginalOwnerAccount = "(unknown)";
            private bool m_UseSignLocation;
            private Point3D m_SignLocation;
            private int m_SignHangerGraphic = 0xB98;
            private int m_SignpostGraphic = 0x9;
            private int m_SignGraphic = 0xBD2;
            private List<TileEntry> m_TileList = new List<TileEntry>();
            private List<DoorEntry> m_DoorList = new List<DoorEntry>();
            private List<Rectangle2D> m_Area = new List<Rectangle2D>();

            public string ID
            {
                get { return m_ID; }
                set { m_ID = value; }
            }

            /// <summary>
            /// Construction version of the house. NOT the version of the XML format. This version is displayed in the House Gump.
            /// </summary>
            public double Version
            {
                get { return m_Version; }
                set { m_Version = value; }
            }

            /// <summary>
            /// Capture date of this version of the house. displayed in the House Gump.
            /// </summary>
            public DateTime Capture
            {
                get { return m_Capture; }
                set { m_Capture = value; }
            }

            /// <summary>
            /// A deed name is constructed as follows: "deed to a " + sh.Description. Example: "deed to a marble house with patio".
            /// </summary>
            public string Description
            {
                get { return m_Description; }
                set { m_Description = value; }
            }

            public int Width
            {
                get { return m_Width; }
                set { m_Width = value; }
            }

            public int Height
            {
                get { return m_Height; }
                set { m_Height = value; }
            }

            /// <summary>
            /// Price of the house foundation (excluding design). This is the portion an NPC architect will refund.
            /// </summary>
            public int Price
            {
                get { return m_Price; }
                set { m_Price = value; }
            }

            /// <summary>
            /// Creation date of the captured house. Displayed in the House Gump as the revision date.
            /// </summary>
            public DateTime BuiltOn
            {
                get { return m_BuiltOn; }
                set { m_BuiltOn = value; }
            }

            public string OriginalOwnerName
            {
                get { return m_OriginalOwnerName; }
                set { m_OriginalOwnerName = value; }
            }

            /// <summary>
            /// Displayed in the House Gump as the 'designer's licence number'.
            /// </summary>
            public Serial OriginalOwnerSerial
            {
                get { return m_OriginalOwnerSerial; }
                set { m_OriginalOwnerSerial = value; }
            }

            public string OriginalOwnerAccount
            {
                get { return m_OriginalOwnerAccount; }
                set { m_OriginalOwnerAccount = value; }
            }

            public bool UseSignLocation
            {
                get { return m_UseSignLocation; }
                set { m_UseSignLocation = value; }
            }

            public Point3D SignLocation
            {
                get { return m_SignLocation; }
                set { m_SignLocation = value; }
            }

            public int SignHangerGraphic
            {
                get { return m_SignHangerGraphic; }
                set { m_SignHangerGraphic = value; }
            }

            public int SignpostGraphic
            {
                get { return m_SignpostGraphic; }
                set { m_SignpostGraphic = value; }
            }

            public int SignGraphic
            {
                get { return m_SignGraphic; }
                set { m_SignGraphic = value; }
            }

            public List<TileEntry> TileList
            {
                get { return m_TileList; }
            }

            public List<DoorEntry> DoorList
            {
                get { return m_DoorList; }
            }

            public List<Rectangle2D> Area
            {
                get { return m_Area; }
            }

            public HouseBlueprint()
            {
            }

            public void ToXml(XmlElement xmlHouseElem)
            {
                try
                {
                    const double elemVersion = 2.0; // version of the house element

                    xmlHouseElem.SetAttribute("Version", elemVersion.ToString());

                    XmlDocument xmlDoc = xmlHouseElem.OwnerDocument;

                    XmlElement xmlElem;

                    xmlElem = xmlDoc.CreateElement("id");
                    xmlElem.InnerText = m_ID;
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("Version");
                    xmlElem.InnerText = m_Version.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("Capture");
                    xmlElem.InnerText = m_Capture.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    if (!string.IsNullOrEmpty(m_Description))
                    {
                        xmlElem = xmlDoc.CreateElement("Description");
                        xmlElem.InnerText = m_Description;
                        xmlHouseElem.AppendChild(xmlElem);
                    }

                    xmlElem = xmlDoc.CreateElement("Width");
                    xmlElem.InnerText = m_Width.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("Height");
                    xmlElem.InnerText = m_Height.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("Price");
                    xmlElem.InnerText = m_Price.ToString();
                    xmlHouseElem.AppendChild(xmlElem);
                    XmlElement xmlMultiElem = xmlDoc.CreateElement("Multi");

                    xmlElem = xmlDoc.CreateElement("BuiltOn");
                    xmlElem.InnerText = m_BuiltOn.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("OriginalOwnerName");
                    xmlElem.InnerText = m_OriginalOwnerName.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("OriginalOwnerAccount");
                    xmlElem.InnerText = m_OriginalOwnerAccount.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("OriginalOwnerSerial");
                    xmlElem.InnerText = m_OriginalOwnerSerial.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("SignLocation");
                    xmlElem.InnerText = m_SignLocation.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("SignHangerGraphic");
                    xmlElem.InnerText = m_SignHangerGraphic.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("SignpostGraphic");
                    xmlElem.InnerText = m_SignpostGraphic.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    xmlElem = xmlDoc.CreateElement("SignGraphic");
                    xmlElem.InnerText = m_SignGraphic.ToString();
                    xmlHouseElem.AppendChild(xmlElem);

                    foreach (TileEntry te in m_TileList)
                    {
                        XmlElement xmlTileElem = xmlDoc.CreateElement("Graphic");

                        xmlElem = xmlDoc.CreateElement("id");
                        xmlElem.InnerText = te.m_id.ToString();
                        xmlTileElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("x");
                        xmlElem.InnerText = te.m_xOffset.ToString();
                        xmlTileElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("y");
                        xmlElem.InnerText = te.m_yOffset.ToString();
                        xmlTileElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("z");
                        xmlElem.InnerText = te.m_zOffset.ToString();
                        xmlTileElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("flags");
                        xmlElem.InnerText = string.Concat("0x", ((int)te.m_flags).ToString("X2"));
                        xmlTileElem.AppendChild(xmlElem);

                        if (te.m_hue != 0)
                        {
                            xmlElem = xmlDoc.CreateElement("hue");
                            xmlElem.InnerText = te.m_hue.ToString();
                            xmlTileElem.AppendChild(xmlElem);
                        }

                        if (te.m_name != null)
                        {
                            xmlElem = xmlDoc.CreateElement("name");
                            xmlElem.InnerText = te.m_name.ToString();
                            xmlTileElem.AppendChild(xmlElem);
                        }

                        xmlMultiElem.AppendChild(xmlTileElem);
                    }

                    xmlHouseElem.AppendChild(xmlMultiElem);

                    XmlElement xmlDoorsElem = xmlDoc.CreateElement("Doors");

                    foreach (DoorEntry de in m_DoorList)
                    {
                        XmlElement xmlDoorElem = xmlDoc.CreateElement("Door");

                        xmlElem = xmlDoc.CreateElement("x");
                        xmlElem.InnerText = de.m_xOffset.ToString();
                        xmlDoorElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("y");
                        xmlElem.InnerText = de.m_yOffset.ToString();
                        xmlDoorElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("z");
                        xmlElem.InnerText = de.m_zOffset.ToString();
                        xmlDoorElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("doorType");
                        xmlElem.InnerText = (de.m_doorType).ToString();
                        xmlDoorElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("facing");
                        xmlElem.InnerText = (de.m_facing).ToString();
                        xmlDoorElem.AppendChild(xmlElem);

                        xmlDoorsElem.AppendChild(xmlDoorElem);
                    }

                    xmlHouseElem.AppendChild(xmlDoorsElem);

                    XmlElement xmlRegionElem = xmlDoc.CreateElement("Region");

                    foreach (Rectangle2D rect in m_Area)
                    {
                        XmlElement xmlRectangleElem = xmlDoc.CreateElement("Rectangle2D");

                        xmlElem = xmlDoc.CreateElement("x");
                        xmlElem.InnerText = rect.X.ToString();
                        xmlRectangleElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("y");
                        xmlElem.InnerText = rect.Y.ToString();
                        xmlRectangleElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("width");
                        xmlElem.InnerText = rect.Width.ToString();
                        xmlRectangleElem.AppendChild(xmlElem);

                        xmlElem = xmlDoc.CreateElement("height");
                        xmlElem.InnerText = rect.Height.ToString();
                        xmlRectangleElem.AppendChild(xmlElem);

                        xmlRegionElem.AppendChild(xmlRectangleElem);
                    }

                    xmlHouseElem.AppendChild(xmlRegionElem);
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "Error while writing XML house element.");
                }
            }

            public HouseBlueprint(XmlElement xmlHouseElem)
            {
                try
                {
                    double elemVersion = ToDouble(xmlHouseElem.GetAttribute("Version"), 1.0);

                    foreach (XmlElement xmlElem in xmlHouseElem)
                    {
                        switch (xmlElem.Name.ToLower())
                        {
                            case "id":
                                {
                                    m_ID = xmlElem.InnerText;
                                    break;
                                }
                            case "version":
                                {
                                    m_Version = ToDouble(xmlElem.InnerText, 1.0);
                                    break;
                                }
                            case "capture":
                                {
                                    m_Capture = ToDateTime(xmlElem.InnerText);
                                    break;
                                }
                            case "description":
                                {
                                    m_Description = xmlElem.InnerText;
                                    break;
                                }
                            case "width":
                                {
                                    m_Width = ToInt32(xmlElem.InnerText);
                                    break;
                                }
                            case "height":
                                {
                                    m_Height = ToInt32(xmlElem.InnerText);
                                    break;
                                }
                            case "price":
                                {
                                    m_Price = ToInt32(xmlElem.InnerText, PriceError);
                                    break;
                                }
                            case "builton":
                                {
                                    m_BuiltOn = ToDateTime(xmlElem.InnerText);
                                    break;
                                }
                            case "originalownername":
                                {
                                    m_OriginalOwnerName = xmlElem.InnerText;
                                    break;
                                }
                            case "originalownerserial":
                                {
                                    m_OriginalOwnerSerial = ToInt32(xmlElem.InnerText, -1);
                                    break;
                                }
                            case "originalowneraccount":
                                {
                                    m_OriginalOwnerAccount = xmlElem.InnerText;
                                    break;
                                }
                            case "signlocation":
                                {
                                    // in earlier version, the sign location was prefixed with "(unused)"
                                    //  and the locations were given as absolute locations of the sign
                                    //  during capture... not useful!
                                    if (elemVersion >= 2.0)
                                    {
                                        string text = xmlElem.InnerText.Trim();

                                        if (text.StartsWith("(") && text.EndsWith(")") && text.Length > 2)
                                            text = text.Substring(1, text.Length - 2).Trim();

                                        string[] split = text.Split(',');

                                        int x = Convert.ToInt32(split[0].Trim());
                                        int y = Convert.ToInt32(split[1].Trim());
                                        int z = Convert.ToInt32(split[2].Trim());

                                        m_UseSignLocation = true;
                                        m_SignLocation = new Point3D(x, y, z);
                                    }

                                    break;
                                }
                            case "signhangergraphic":
                                {
                                    m_SignHangerGraphic = ToInt32(xmlElem.InnerText, 0xB98);
                                    break;
                                }
                            case "signpostgraphic":
                                {
                                    m_SignpostGraphic = ToInt32(xmlElem.InnerText, 0x9);
                                    break;
                                }
                            case "signgraphic":
                                {
                                    m_SignGraphic = ToInt32(xmlElem.InnerText, 0xBD2);
                                    break;
                                }
                            case "multi":
                                {
                                    foreach (XmlElement xmlTile in xmlElem)
                                    {
                                        short xOffset = ToInt16(xmlTile["x"].InnerText);
                                        short yOffset = ToInt16(xmlTile["y"].InnerText);
                                        short zOffset = ToInt16(xmlTile["z"].InnerText);
                                        ushort id = ToUInt16(xmlTile["id"].InnerText);
                                        ushort hue = 0;
                                        string name = null;

                                        if (xmlTile["hue"] != null)
                                            hue = ToUInt16(xmlTile["hue"].InnerText);

                                        if (xmlTile["name"] != null)
                                            name = xmlTile["name"].InnerText;

                                        TileType flags = (TileType)ToInt32(xmlTile["flags"].InnerText);

                                        m_TileList.Add(new TileEntry(xOffset, yOffset, zOffset, id, hue, name, flags));
                                    }

                                    break;
                                }
                            case "doors":
                                {
                                    foreach (XmlElement xmlDoor in xmlElem)
                                    {
                                        short xOffset = ToInt16(xmlDoor["x"].InnerText);
                                        short yOffset = ToInt16(xmlDoor["y"].InnerText);
                                        short zOffset = ToInt16(xmlDoor["z"].InnerText);

                                        DoorType doorType = (DoorType)0;
                                        DoorFacing facing = (DoorFacing)0;

                                        Enum.TryParse<DoorType>(xmlDoor["doorType"].InnerText, out doorType);
                                        Enum.TryParse<DoorFacing>(xmlDoor["facing"].InnerText, out facing);

                                        m_DoorList.Add(new DoorEntry(xOffset, yOffset, zOffset, doorType, facing));
                                    }

                                    break;
                                }
                            case "region":
                                {
                                    foreach (XmlElement rect in xmlElem)
                                    {
                                        int x = ToInt32(rect["x"].InnerText);
                                        int y = ToInt32(rect["y"].InnerText);
                                        int width = ToInt32(rect["width"].InnerText);
                                        int height = ToInt32(rect["height"].InnerText);

                                        Rectangle2D rectangle = new Rectangle2D(x, y, width, height);

                                        m_Area.Add(rectangle);
                                    }

                                    break;
                                }
                            default:
                                {
                                    Console.WriteLine("Unrecognized XML node \"{0}\".", xmlElem.Name.ToLower());
                                    break;
                                }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex, "Error while reading XML house element.");
                }
            }

            private static int ToInt32(string s, int defaultValue = default(int))
            {
                try
                {
                    if (string.IsNullOrEmpty(s))
                        return defaultValue;
                    if (s.StartsWith("0x"))
                        return Convert.ToInt32(s.Substring(2), 16);
                    else
                        return Convert.ToInt32(s);
                }
                catch
                {
                    return defaultValue;
                }
            }

            private static short ToInt16(string s, short defaultValue = default(short))
            {
                try
                {
                    if (string.IsNullOrEmpty(s))
                        return defaultValue;
                    if (s.StartsWith("0x"))
                        return Convert.ToInt16(s.Substring(2), 16);
                    else
                        return Convert.ToInt16(s);
                }
                catch
                {
                    return defaultValue;
                }
            }

            private static ushort ToUInt16(string s, ushort defaultValue = default(ushort))
            {
                try
                {
                    if (string.IsNullOrEmpty(s))
                        return defaultValue;

                    if (s.StartsWith("0x"))
                        return Convert.ToUInt16(s.Substring(2), 16);
                    else
                        return Convert.ToUInt16(s);
                }
                catch
                {
                    return defaultValue;
                }
            }

            private static double ToDouble(string s, double defaultValue = default(double))
            {
                try
                {
                    if (string.IsNullOrEmpty(s))
                        return defaultValue;
                    return Convert.ToDouble(s);
                }
                catch
                {
                    return defaultValue;
                }
            }

            private static DateTime ToDateTime(string s, DateTime defaultValue = default(DateTime))
            {
                try
                {
                    if (string.IsNullOrEmpty(s))
                        return defaultValue;
                    return Convert.ToDateTime(s);
                }
                catch
                {
                    return defaultValue;
                }
            }
        }

        public static int GetFoundationID(string houseID)
        {
            HouseBlueprint blueprint = GetBlueprint(houseID);

            if (blueprint != null)
                return GetFoundationID(blueprint.Width, blueprint.Height);

            return 0;
        }

        public static int GetFoundationID(int width, int height)
        {
            if (width < 7 || width > 18 || height < 7 || height > 18 || Math.Abs(width - height) > 5)
                return 0;

            return 5100 + 12 * (width - 7) + (height - 7);
        }

        public static bool IsValidFoundationSize(int width, int height)
        {
            return (GetFoundationID(width, height) != 0);
        }
    }
}