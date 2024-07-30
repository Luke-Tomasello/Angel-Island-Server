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

/* scripts\Misc\WinterEventSystem.cs
 * CHANGELOG:
 *  4/14/23, Yoar
 *      Initial commit.
 */

using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Server.Misc
{
    public static class WinterEventSystem
    {
        private static readonly Rectangle2D m_DaggerIsland = new Rectangle2D(3720, 0, 680, 860);

        private static Timer m_Timer;
        private static readonly Dictionary<Mobile, WinterEventContext> m_Contexts = new Dictionary<Mobile, WinterEventContext>();

        public static Timer Timer { get { return m_Timer; } }
        public static Dictionary<Mobile, WinterEventContext> Contexts { get { return m_Contexts; } }

        public static bool Enabled = false;
        public static Map Facet = Map.Trammel;
        public static int WeatherInterval = 15;
        public static int MaxWarmth = 100;
        public static int HeatRange = 20;
        public static int ColdTick = 5;
        public static int HeatTick = 25;
        public static Rectangle2D TownBounds = new Rectangle2D(4010, 522, 79, 73);
        public static int AlcoholHeat = 5;
        public static int WildHuntArmyScalar = 100;
        public static int WildHuntHitsScalar = 100;

        public static void Configure()
        {
            EventSink.WorldLoad += EventSink_WorldLoad;
            EventSink.WorldSave += EventSink_WorldSave;
        }

        public static void Initialize()
        {
            WinterEventPersistence.EnsureExistence();

            TargetCommands.Register(new WinterContextCommand());

            Defrag();

            CheckTimer();
        }

        private static void EventSink_WorldLoad()
        {
            Load();
        }

        private static void EventSink_WorldSave(WorldSaveEventArgs e)
        {
            Save();
        }

        #region Commands

        private class WinterContextCommand : BaseCommand
        {
            public WinterContextCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "WinterContext" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "WinterContext";
                Description = "Displays the winter event context for a targeted mobile.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                Mobile m = obj as Mobile;

                if (m == null)
                    return; // sanity

                WinterEventContext context = GetContext(m, false);

                if (context == null)
                {
                    LogFailure("They have no winter event context.");
                    return;
                }

                e.Mobile.SendGump(new PropertiesGump(e.Mobile, context));
            }
        }

        #endregion

        public static bool Contains(Mobile m)
        {
            return Contains(m.Location, m.Map);
        }

        public static bool Contains(Item item)
        {
            return Contains(item.GetWorldLocation(), item.Map);
        }

        public static bool Contains(Point3D loc, Map map)
        {
            if (!Enabled)
                return false;

            return (map == Facet && m_DaggerIsland.Contains(loc));
        }

        public static void HandleLocationChance(Mobile m, Point3D oldLocation)
        {
            if (!Enabled)
                return;

            HandlePositionChange(m, oldLocation, m.Map);
        }

        public static void HandleMapChange(Mobile m, Map oldMap)
        {
            if (!Enabled)
                return;

            HandlePositionChange(m, m.Location, oldMap);
        }

        private static void HandlePositionChange(Mobile m, Point3D oldLocation, Map oldMap)
        {
            if (!Contains(oldLocation, oldMap) && Contains(m))
            {
                WinterEventContext context = GetContext(m, true);

                if (context != null)
                    context.Warmth = MaxWarmth;
            }
        }

        public static void OnDrinkAlcohol(Mobile m, int bac)
        {
            if (!Enabled)
                return;

            WinterEventContext context = GetContext(m, false);

            if (context != null && context.Warmth < MaxWarmth)
            {
                m.LocalOverheadMessage(MessageType.Regular, 0x54F, false, "You feel all warm and fuzzy from drinking the alcoholic beverage.");

                context.Warmth += bac * AlcoholHeat;
            }
        }

        public static void CheckTimer()
        {
            if (Enabled)
                StartTimer();
            else
                StopTimer();
        }

        private static void ProcessTick()
        {
            foreach (KeyValuePair<Mobile, WinterEventContext> kvp in m_Contexts)
                ProcessTick(kvp.Value);

            Defrag();
        }

        private static void ProcessTick(WinterEventContext context)
        {
            Mobile m = context.Owner;

            if (m.Map != Map.Internal && DateTime.UtcNow >= context.NextColdTick)
            {
                if (Contains(m) && m.Alive && (m.AccessLevel == AccessLevel.Player || !m.Hidden))
                {
                    context.NextColdTick = DateTime.UtcNow + TimeSpan.FromSeconds(WeatherInterval);

                    if (TownBounds.Contains(m))
                    {
                        context.Warmth += HeatTick;

                        if (context.WasCold)
                            m.SendMessage("You warm yourself in town.");

                        context.WasCold = false;
                    }
                    else if (FindHeat(m))
                    {
                        context.Warmth += HeatTick;

                        if (context.WasCold)
                            m.SendMessage("You warm yourself by the fire.");

                        context.WasCold = false;
                    }
                    else
                    {
                        int cold = ColdTick;

                        if (CountWarmClothing(m) > 0)
                        {
                            cold /= 2;

                            if (cold < 1)
                                cold = 1;
                        }

                        if (context.Warmth <= 0)
                        {
                            m.LocalOverheadMessage(MessageType.Regular, 0x556, false, "You are freezing!");

                            if (cold > m.Hits)
                            {
                                m.Kill();

                                context.Warmth = MaxWarmth;
                                context.WasCold = false;
                            }
                            else
                            {
                                m.Hits -= cold;
                            }
                        }
                        else
                        {
                            int oldIndex = GetWarningIndex(context.Warmth);

                            context.Warmth -= cold;

                            int newIndex = GetWarningIndex(context.Warmth);

                            if (!context.WasCold || newIndex != oldIndex)
                                m.SendMessage(GetWarning(newIndex));
                        }

                        context.WasCold = true;
                    }
                }
                else
                {
                    context.Warmth = MaxWarmth;
                    context.WasCold = false;
                }
            }
        }

        public static bool FindHeat(Mobile m)
        {
            return Find(m, Engines.Craft.CraftItem.HeatSources, 4);
        }

        #region Find

        private static bool Find(Mobile m, int[] itemIDs, int range)
        {
            Map map = m.Map;

            if (map == null)
                return false;

            foreach (Item item in map.GetItemsInRange(m.Location, range))
            {
                if (item.Visible && item.Z + 16 > m.Z && m.Z + 16 > item.Z && Find(item.ItemID, itemIDs) && m.InLOS(item))
                    return true;
            }

            for (int dx = -range; dx <= range; ++dx)
            {
                for (int dy = -range; dy <= range; ++dy)
                {
                    int x = m.X + dx;
                    int y = m.Y + dy;

                    StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

                    for (int i = 0; i < tiles.Length; ++i)
                    {
                        int z = tiles[i].Z;
                        int id = tiles[i].ID & 0x3FFF;

                        if (z + 16 > m.Z && m.Z + 16 > z && Find(id, itemIDs) && m.InLOS(new Point3D(x, y, z)))
                            return true;
                    }
                }
            }

            return false;
        }

        private static bool Find(int itemID, int[] itemIDs)
        {
            bool contains = false;

            for (int i = 0; !contains && i < itemIDs.Length; i += 2)
                contains = (itemID >= itemIDs[i] && itemID <= itemIDs[i + 1]);

            return contains;
        }

        #endregion

        private static int CountWarmClothing(Mobile m)
        {
            int count = 0;

            foreach (Item item in m.Items)
            {
                if (ContainsType(m_WarmClothingTypes, item.GetType()))
                    count++;
            }

            return count;
        }

        private static readonly Type[] m_WarmClothingTypes = new Type[]
            {
                typeof(FurBoots),
                typeof(FurCape),
                typeof(FurSarong),
            };

        private static bool ContainsType(Type[] types, Type type)
        {
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].IsAssignableFrom(type))
                    return true;
            }

            return false;
        }

        private static readonly string[] m_Warnings = new string[]
            {
                "You feel extremely cold!",
                "You feel very cold.",
                "You feel pretty cold.",
                "You feel quite chilly.",
            };

        private static int GetWarningIndex(int warmth)
        {
            if (MaxWarmth <= 0 || m_Warnings.Length == 0)
                return -1;

            return (warmth + 1) / (MaxWarmth / m_Warnings.Length);
        }

        private static string GetWarning(int index)
        {
            if (index >= 0 && index < m_Warnings.Length)
                return m_Warnings[index];

            return null;
        }

        public static WinterEventContext GetContext(Mobile m, bool create)
        {
            WinterEventContext context;

            if (!m_Contexts.TryGetValue(m, out context) && create)
                m_Contexts[m] = context = new WinterEventContext(m);

            return context;
        }

        private static void Defrag()
        {
            List<Mobile> toRemove = null;

            foreach (KeyValuePair<Mobile, WinterEventContext> kvp in m_Contexts)
            {
                if (!Contains(kvp.Key) && kvp.Value.IsEmpty())
                {
                    if (toRemove == null)
                        toRemove = new List<Mobile>();

                    toRemove.Add(kvp.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (Mobile m in toRemove)
                    m_Contexts.Remove(m);
            }
        }

        public static void StartTimer()
        {
            StopTimer();

            (m_Timer = new InternalTimer()).Start();
        }

        public static void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private class InternalTimer : Timer
        {
            public InternalTimer()
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
            }

            protected override void OnTick()
            {
                if (Enabled)
                    ProcessTick();
                else
                    StopTimer();
            }
        }

        #region Save/Load

        private const string FilePath = "Saves/WinterEventSystem.xml";

        public static void Save()
        {
            try
            {
                Console.WriteLine("WinterEventSystem Saving...");

                string directoryName = Path.GetDirectoryName(FilePath);

                if (!String.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);

                XmlTextWriter writer = new XmlTextWriter(FilePath, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("WinterEventSystem");
                writer.WriteAttributeString("version", "3");

                try
                {
                    // version 3

                    writer.WriteElementString("WildHuntArmyScalar", WildHuntArmyScalar.ToString());
                    writer.WriteElementString("WildHuntHitsScalar", WildHuntHitsScalar.ToString());

                    // version 2

                    writer.WriteElementString("TownBounds", TownBounds.ToString());
                    writer.WriteElementString("AlcoholHeat", AlcoholHeat.ToString());

                    // version 1

                    writer.WriteElementString("WeatherInterval", WeatherInterval.ToString());
                    writer.WriteElementString("MaxWarmth", MaxWarmth.ToString());
                    writer.WriteElementString("HeatRange", HeatRange.ToString());
                    writer.WriteElementString("ColdTick", ColdTick.ToString());
                    writer.WriteElementString("HeatTick", HeatTick.ToString());

                    // version 0

                    writer.WriteElementString("Enabled", Enabled.ToString());
                    writer.WriteElementString("Facet", FormatFacet(Facet));
                }
                finally
                {
                    writer.WriteEndDocument();
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private static void Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return;

                Console.WriteLine("WinterEventSystem Loading...");

                XmlTextReader reader = new XmlTextReader(FilePath);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int version = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("WinterEventSystem");

                bool weaponEnchantments = true, statueEnchantments = true;

                switch (version)
                {
                    case 3:
                        {
                            WildHuntArmyScalar = Int32.Parse(reader.ReadElementString("WildHuntArmyScalar"));
                            WildHuntHitsScalar = Int32.Parse(reader.ReadElementString("WildHuntHitsScalar"));

                            goto case 2;
                        }
                    case 2:
                        {
                            TownBounds = Rectangle2D.Parse(reader.ReadElementString("TownBounds"));
                            AlcoholHeat = Int32.Parse(reader.ReadElementString("AlcoholHeat"));

                            goto case 1;
                        }
                    case 1:
                        {
                            WeatherInterval = Int32.Parse(reader.ReadElementString("WeatherInterval"));
                            MaxWarmth = Int32.Parse(reader.ReadElementString("MaxWarmth"));
                            HeatRange = Int32.Parse(reader.ReadElementString("HeatRange"));
                            ColdTick = Int32.Parse(reader.ReadElementString("ColdTick"));
                            HeatTick = Int32.Parse(reader.ReadElementString("HeatTick"));

                            goto case 0;
                        }
                    case 0:
                        {
                            Enabled = Boolean.Parse(reader.ReadElementString("Enabled"));
                            Facet = ParseFacet(reader.ReadElementString("Facet"));

                            break;
                        }
                }

                reader.ReadEndElement();

                reader.Close();
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private static string FormatFacet(Map facet)
        {
            return (facet == null ? String.Empty : facet.ToString());
        }

        private static Map ParseFacet(string value)
        {
            return (String.IsNullOrEmpty(value) ? null : Map.Parse(value));
        }

        #endregion
    }

    [PropertyObject]
    public class WinterEventContext
    {
        private Mobile m_Owner;
        private int m_Warmth;
        private DateTime m_NextColdTick;
        private bool m_WasCold;

        public Mobile Owner
        {
            get { return m_Owner; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Warmth
        {
            get { return m_Warmth; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > WinterEventSystem.MaxWarmth)
                    value = WinterEventSystem.MaxWarmth;

                m_Warmth = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextColdTick
        {
            get { return m_NextColdTick; }
            set { m_NextColdTick = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool WasCold
        {
            get { return m_WasCold; }
            set { m_WasCold = value; }
        }

        public WinterEventContext(Mobile owner)
        {
            m_Owner = owner;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write((int)m_Warmth);
            writer.WriteDeltaTime(m_NextColdTick);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Warmth = reader.ReadInt();
                        m_NextColdTick = reader.ReadDeltaTime();

                        break;
                    }
            }
        }

        public bool IsEmpty()
        {
            return (m_Warmth >= WinterEventSystem.MaxWarmth && DateTime.UtcNow >= m_NextColdTick);
        }

        public override string ToString()
        {
            return "...";
        }
    }

    public class WinterEventPersistence : Item, IPersistence
    {
        private static WinterEventPersistence m_Instance;
        public Item GetInstance() { return m_Instance; }
        public static void EnsureExistence()
        {
            if (m_Instance == null)
            {
                m_Instance = new WinterEventPersistence();
                m_Instance.IsIntMapStorage = true;
            }
        }

        public override string DefaultName
        {
            get { return "Winter Event Persistence - Internal"; }
        }

        [Constructable]
        public WinterEventPersistence()
            : base(0x1)
        {
            Movable = false;
        }

        public WinterEventPersistence(Serial serial)
            : base(serial)
        {
            m_Instance = this;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write((int)WinterEventSystem.Contexts.Count);

            foreach (KeyValuePair<Mobile, WinterEventContext> kvp in WinterEventSystem.Contexts)
            {
                writer.Write((Mobile)kvp.Key);
                kvp.Value.Serialize(writer);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            int count = reader.ReadInt();

            for (int i = 0; i < count; i++)
            {
                Mobile m = reader.ReadMobile();

                WinterEventContext context = new WinterEventContext(m);

                context.Deserialize(reader);

                if (m != null)
                    WinterEventSystem.Contexts[m] = context;
            }
        }
    }

    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class WinterEventConsole : Item
    {
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool Enabled
        {
            get { return WinterEventSystem.Enabled; }
            set
            {
                if (WinterEventSystem.Enabled != value)
                {
                    WinterEventSystem.Enabled = value;

                    WinterEventSystem.CheckTimer();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Map Facet
        {
            get { return WinterEventSystem.Facet; }
            set { WinterEventSystem.Facet = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int WeatherInterval
        {
            get { return WinterEventSystem.WeatherInterval; }
            set { WinterEventSystem.WeatherInterval = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxWarmth
        {
            get { return WinterEventSystem.MaxWarmth; }
            set { WinterEventSystem.MaxWarmth = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int HeatRange
        {
            get { return WinterEventSystem.HeatRange; }
            set { WinterEventSystem.HeatRange = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int ColdTick
        {
            get { return WinterEventSystem.ColdTick; }
            set { WinterEventSystem.ColdTick = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int HeatTick
        {
            get { return WinterEventSystem.HeatTick; }
            set { WinterEventSystem.HeatTick = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public Rectangle2D TownBounds
        {
            get { return WinterEventSystem.TownBounds; }
            set { WinterEventSystem.TownBounds = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int WildHuntArmyScalar
        {
            get { return WinterEventSystem.WildHuntArmyScalar; }
            set { WinterEventSystem.WildHuntArmyScalar = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int WildHuntHitsScalar
        {
            get { return WinterEventSystem.WildHuntHitsScalar; }
            set
            {
                if (value != WinterEventSystem.WildHuntHitsScalar)
                {
                    Mobiles.WildHunt.WildHunt.UnscaleHits();

                    WinterEventSystem.WildHuntHitsScalar = value;

                    Mobiles.WildHunt.WildHunt.ScaleHits();
                }
            }
        }

        [Constructable]
        public WinterEventConsole()
            : base(0x1F14)
        {
            Hue = 1154;
            Name = "Winter Event Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new PropertiesGump(from, this));
        }

        public WinterEventConsole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }
    }
}