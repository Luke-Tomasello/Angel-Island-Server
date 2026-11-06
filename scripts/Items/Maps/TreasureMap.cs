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

/* Scripts/Items/Maps/TreasureMap.cs
 * ChangeLog
 *  10/21/2026, Adam
 *  OnLoad: when there is no Angel Island Treasure.json, the generator prints the progress message
 *  ("Generating Treasure Map Locations. This may take a while...") instead of OnLoad. Loading is unchanged.
 *  1/31/2026, Adam
 * TreasureMap: remove inline location generation; delegate to TreasureMapLocations helper
 * Body:
 * Removes legacy treasure location generation logic from TreasureMap.cs and delegates all location loading/generation to a new helper
 * (static class TreasureMapLocations). TreasureMap is now a pure consumer of the precomputed Locations array, eliminating ad-hoc generation,
 * embedded constants, and shard-specific logic from the item class itself. This simplifies TreasureMap, reduces coupling, and makes the
 * location model easier to reason about, test, and evolve independently.
 * Model overview:
 * TreasureMap.cs no longer knows how locations are produced. It simply relies on TreasureMapLocations to provide a validated,
 * shuffled Point2D[] pool at startup. TreasureMapLocations owns the full lifecycle of treasure locations:
 * loading from cache, generating from world spawners when needed, filtering invalid regions/houses, deduping,
 * randomizing, and persisting results. This clean separation improves maintainability and avoids future brittleness as world rules or
 * placement heuristics evolve.
 *  5/5/2024, Adam (TreasureMapQuest)
 *      Add TreasureMapQuest maps. These maps have two distinct differences over regular treasure maps.
 *          1. They load their locations from a special table located in \data\Binary Map Data
 *          2. They require a RaresPack. One item from this rares pack is added to the usual loot (as specified by level.)
 * 6/12/23, Yoar
 *      Treasure maps can now be dug up by those who did not decode it, providing that they have enough cartography skill.
 *      https://www.uoguide.com/Publish_13
 * 1/11/23, Adam (GetRandomLocation())
 *      We now filter our dynamic treasure map locations for BaseMulti during the GetRandomLocation() call
 * 9/6/22, Adam
 *      remove the early exit after loading the standard OSI t-map locations if (!Core.RuleSets.AngelIslandRules())
 * 5/31/22, Adam (RemoveBadRegions)
 *  This filters out all dungeons + green acres and whatnot
 *  if (location.X >= 5120) continue;
 *	2/3/11, Adam
 *		Make extended map locations based upon Core.AngelIsland
 *	9/1/10, Adam
 *		Make treasure maps ReadOnly 
 *	8/27/10, adam
 *		Use Utility.BritWrap rects from boat nav to test for valid treasure map locations.
 *		Turns out that the rects defined in Utility.BritWrap are the two halves of the world excluding T2A and the dungeons
 *		which is what we want.
 *	8/12/10, adam
 *		Add tax collector spawn
 *	1/20/06, Adam
 *		Add new OnNPCBeginDig() function so that NPC's can dig treasure.
 *	5/07/05, Adam
 *		Remove hue and replace the the text 
 *			"for somewhere in Felucca" with "for somewhere in Ocllo"
 *		if the map is in Ocllo
 *	5/07/05, Kit
 *		Hued maps to faction orange if for withen ocllo
 *	4/17/05, Kitaras	
 *		Fixed bug regarding level 1 and 2 chests being set to themed
 *	4/14/04, Adam
 *		1. Put back lost change to treasure map drop rate
 *		2. Convert LootChance to a property, and attach it to CoreAI.TreasureMapDrop
 *	4/07/05, Kitaras
 *		Fixed static access variable issue
 *	4/03/05, Kitaras	
 *		Added check to spawn only one level 5 mob on themed chests
 *	3/30/05, Kitaras
 *		Redesigned system to use new TreasureThemes.cs control system for spawn generation.
 *	3/1/05, mith
 *		OnDoubleClick(): modified difficulty check so that if base Carto skill is less than midPoint, players gets failure message.
 *  12/05/04, Jade
 *      Reverted the chance to drop t-map back to 1%
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Changed percentage chance for creature to drop t-map from 1% to 3.5%
 *
 *      8/30/04, Lego Eater
 *               changed the on singleclick so tmaps displayed properly (sorry for spelling)
 *
 *
 */

using Server.Commands;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using static Server.Utility;

namespace Server.Items
{
    public class TreasureMap : MapItem
    {
        #region SETUP
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
        }

        public static void OnLoad()
        {
            /*
             * treasure map stuffs
             */
            // No database: LoadLocations generates one, and the generator prints its own progress message
            bool generating = !File.Exists(Path.Combine(Core.DataDirectory, "Angel Island Treasure.json"));
            if (!generating)
                Console.Write("Loading treasure map locations...");

            Utility.TimeCheck tc = new Utility.TimeCheck();
            tc.Start();
            int lcount = TreasureMapLocations.LoadLocations();
            tc.End();
            string s = string.Format("{0} Done. {1} locations {2} in: {3}", generating ? "Treasure map locations:" : "", lcount, generating ? "generated" : "loaded", tc.TimeTaken);
            Console.WriteLine(s);
        }
        #endregion SETUP

        public override bool ReadOnly { get { return true; } set {; } }
        #region DATA
        private int m_Level;
        private bool m_Completed;
        private Mobile m_Decoder;
        private Map m_Map;
        private Point2D m_ChestLocation;
        private bool m_Themed;
        private ChestThemeType m_type;

        #region Command Props
        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; InvalidateProperties(); } }

        //set theme type
        [CommandProperty(AccessLevel.GameMaster)]
        public ChestThemeType Theme { get { return m_type; } set { m_type = value; InvalidateProperties(); } }

        //set if map is themed or not
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Themed { get { return m_Themed; } set { m_Themed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Completed { get { return m_Completed; } set { m_Completed = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Decoder { get { return m_Decoder; } set { m_Decoder = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map ChestMap { get { return m_Map; } set { m_Map = value; InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point2D ChestLocation { get { return m_ChestLocation; } set { m_ChestLocation = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double LootChance { get { return CoreAI.TreasureMapDrop; } }
        #endregion Command Props
        public static Point2D[] Locations
        {
            get { return m_Locations; }
            set { m_Locations = value; }
        }

        private static Point2D[] m_Locations;
        #endregion DATA
        public virtual Point2D GetRandomLocation(Map map)
        {
            return GetRandomTreasureMapLocation(map);
        }
        public static Point2D GetRandomTreasureMapLocation(Map map)
        {
            if (m_Locations == null)
                TreasureMapLocations.LoadLocations();

            if (m_Locations.Length > 0)
                for (int test = 0; test < 100; test++)
                {   // make sure a witch hasn't dropped a house on poor Dorthy
                    Point2D p2d = m_Locations[Utility.Random(m_Locations.Length)];
                    BaseMulti? bm = BaseMulti.Find(new Point3D(p2d.X, p2d.Y, Utility.GetAverageZ(map, p2d.X, p2d.Y)), map);
                    if (bm == null)
                        return p2d;
                }

            Utility.Monitor.WriteLine(string.Format("Running out of valid treasure map locations: {0}. ", Utility.FileInfo()), ConsoleColor.Red);

            return Point2D.Zero;
        }

        #region CONSTRUCRORS
        // [add treasuremap <level>
        [Constructable]
        public TreasureMap(int level)
            : this(level, Map.Felucca)
        {
        }
        // [add treasuremap <level> <map>
        [Constructable]
        public TreasureMap(int level, Map map)
            : this(level, map, false, ChestThemeType.None)
        {
        }

        //[add treasuremap <level> <map> <theme>
        [Constructable]
        public TreasureMap(int level, Map map, ChestThemeType type)
            : this(level, map, true, type)
        {
        }

        [Constructable]
        public TreasureMap(int level, Map map, bool themed, ChestThemeType type)
        {
            if (level > 5 || level < 0)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(MapLevelErrorTick), new object?[] { null });
                m_Level = 0;    // Youthful treasure map ( < 100 gold)
            }
            else
                m_Level = level;
            m_Map = map;
            m_Themed = themed;
            m_type = type;

            Setup();
        }
        #region Map Level Error
        private void MapLevelErrorTick(object state)
        {
            this.SendMessageNearby("Illegal map level", AccessLevel.GameMaster, 0x33);
        }
        #endregion Map Level Error
        #endregion CONSTRUCRORS
        public virtual void Setup()
        {
            m_ChestLocation = GetRandomLocation(map: m_Map);

            Width = 300;
            Height = 300;

            int width = 600;
            int height = 600;

            int x1 = m_ChestLocation.X - Utility.RandomMinMax(width / 4, (width / 4) * 3);
            int y1 = m_ChestLocation.Y - Utility.RandomMinMax(height / 4, (height / 4) * 3);

            if (x1 < 0)
                x1 = 0;

            if (y1 < 0)
                y1 = 0;

            int x2 = x1 + width;
            int y2 = y1 + height;

            if (x2 >= 5120)
                x2 = 5119;

            if (y2 >= 4096)
                y2 = 4095;

            x1 = x2 - width;
            y1 = y2 - height;

            Bounds = new Rectangle2D(x1, y1, width, height);

            Protected = true;

            AddWorldPin(m_ChestLocation.X, m_ChestLocation.Y);

            return;
        }
        public virtual void ChestCreated(TreasureMapChest chest)
        {

        }
        public TreasureMap(Serial serial)
            : base(serial)
        {
        }
        #region DIGGING
        public void OnBeginDig(Mobile from)
        {
            if (m_Completed)
            {
                from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
            }
            else if (PublishInfo.Publish < 13.0 && from != m_Decoder)
            {
                from.SendLocalizedMessage(503016); // Only the person who decoded this map may actually dig up the treasure.
            }
            else if (PublishInfo.Publish >= 13.0 && from != m_Decoder && !HasRequiredSkill(from))
            {
                from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
            }
            else if (!from.CanBeginAction(typeof(TreasureMap)))
            {
                from.SendLocalizedMessage(503020); // You are already digging treasure.
            }
            else
            {
                from.SendLocalizedMessage(503033); // Where do you wish to dig?
                from.Target = new DigTarget(this);
            }
        }

        public void OnNPCBeginDig(Mobile from)
        {
            TreasureMap m_TMap = this;
            Point2D loc = m_TMap.m_ChestLocation;
            int x = loc.X, y = loc.Y;
            Map map = m_TMap.m_Map;
            int z = map.GetAverageZ(x, y);

            if (from.BeginAction(typeof(TreasureMap)))
            {
                new DigTimer(from, m_TMap, new Point3D(x, y, z - 14), map, z, m_TMap.m_type).Start();
            }
        }

        private class DigTarget : Target
        {
            private TreasureMap m_Map;

            public DigTarget(TreasureMap map)
                : base(6, true, TargetFlags.None)
            {
                m_Map = map;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Map.Deleted)
                    return;

                if (m_Map.m_Completed)
                {
                    from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
                }
                else if (PublishInfo.Publish < 13.0 && from != m_Map.m_Decoder)
                {
                    from.SendLocalizedMessage(503016); // Only the person who decoded this map may actually dig up the treasure.
                }
                else if (PublishInfo.Publish >= 13.0 && from != m_Map.m_Decoder && !m_Map.HasRequiredSkill(from))
                {
                    from.SendLocalizedMessage(503031); // You did not decode this map and have no clue where to look for the treasure.
                }
                else if (!from.CanBeginAction(typeof(TreasureMap)))
                {
                    from.SendLocalizedMessage(503020); // You are already digging treasure.
                }
                else
                {
                    IPoint3D? p = targeted as IPoint3D;

                    if (p is Item)
                        p = ((Item)p).GetWorldLocation();

                    int maxRange;
                    double skillValue = from.Skills[SkillName.Mining].Value;

                    if (skillValue >= 100.0)
                        maxRange = 4;
                    else if (skillValue >= 81.0)
                        maxRange = 3;
                    else if (skillValue >= 51.0)
                        maxRange = 2;
                    else
                        maxRange = 1;

                    Point2D loc = m_Map.m_ChestLocation;
                    int x = loc.X, y = loc.Y;
                    Map map = m_Map.m_Map;

                    if (map == from.Map && Utility.InRange(new Point3D(p), new Point3D(loc, 0), maxRange))
                    {
                        if (from.Location.X == loc.X && from.Location.Y == loc.Y)
                        {
                            from.SendLocalizedMessage(503030); // The chest can't be dug up because you are standing on top of it.
                        }
                        else if (map != null)
                        {
                            int z = map.GetAverageZ(x, y);

                            if (!Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.checkBlocksFit | Utility.CanFitFlags.checkMobiles | Utility.CanFitFlags.requireSurface))
                            {
                                from.SendLocalizedMessage(503021); // You have found the treasure chest but something is keeping it from being dug up.
                            }
                            else if (from.BeginAction(typeof(TreasureMap)))
                            {
                                new DigTimer(from, m_Map, new Point3D(x, y, z - 14), map, z, m_Map.m_type).Start();
                            }
                            else
                            {
                                from.SendLocalizedMessage(503020); // You are already digging treasure.
                            }
                        }
                    }
                    else if (Utility.InRange(new Point3D(p), from.Location, 8)) // We're close, but not quite
                    {
                        from.SendLocalizedMessage(503032); // You dig and dig but no treasure seems to be here.
                    }
                    else
                    {
                        from.SendLocalizedMessage(503035); // You dig and dig but fail to find any treasure.
                    }
                }
            }

        }

        private class DigTimer : Timer
        {
            private Mobile m_From;
            private TreasureMap m_TreasureMap;
            private Map m_Map;
            private TreasureMapChest m_Chest;
            private int m_Count;
            private int m_Z;
            private long m_NextSkillTime;
            private DateTime m_NextSpellTime;
            private long m_NextActionTime;
            private DateTime m_LastMoveTime;
            private ChestThemeType type;
            private bool themed;
            public DigTimer(Mobile from, TreasureMap treasureMap, Point3D p, Map map, int z, ChestThemeType m_type)
                : base(TimeSpan.Zero, TimeSpan.FromSeconds(1.0))
            {

                m_From = from;
                m_TreasureMap = treasureMap;
                m_Map = map;
                m_Z = z;
                type = m_type;
                themed = m_TreasureMap.m_Themed;

                if (Core.RuleSets.ThemedTreasureMaps())
                {
                    if (themed == false)
                        themed = TreasureTheme.GetIsThemed(m_TreasureMap.Level);

                    m_TreasureMap.m_Themed = themed;

                    if (themed == true && type == ChestThemeType.None)
                    {
                        type = (ChestThemeType)TreasureTheme.GetThemeType(m_TreasureMap.Level);
                    }
                }

                m_TreasureMap.m_type = type;
                m_Chest = new TreasureMapChest(from.Player ? from : null, m_TreasureMap.m_Level, themed, type);
                m_TreasureMap.ChestCreated(m_Chest);
                m_Chest.MoveToWorld(p, map);

                m_NextSkillTime = from.NextSkillTime;
                m_NextSpellTime = from.NextSpellTime;
                m_NextActionTime = from.NextActionTime;
                m_LastMoveTime = from.LastMoveTime;
            }

            protected override void OnTick()
            {
                if (m_NextSkillTime != m_From.NextSkillTime || m_NextSpellTime != m_From.NextSpellTime || m_NextActionTime != m_From.NextActionTime)
                {
                    Stop();
                    m_From.EndAction(typeof(TreasureMap));
                    m_Chest.Delete();
                }
                else if (m_LastMoveTime != m_From.LastMoveTime)
                {
                    m_From.SendLocalizedMessage(503023); // You cannot move around while digging up treasure. You will need to start digging anew.

                    Stop();
                    m_From.EndAction(typeof(TreasureMap));
                    m_Chest.Delete();
                }
                /*else if ( !m_Map.CanFit( m_Chest.X, m_Chest.Y, m_Z, 16, true, true ) )
				{
					m_From.SendLocalizedMessage( 503024 ); // You stop digging because something is directly on top of the treasure chest.

					Stop();
					m_From.EndAction( typeof( TreasureMap ) );
					m_Chest.Delete();
				}*/
                else
                {
                    m_From.RevealingAction();

                    m_Count++;

                    m_Chest.Location = new Point3D(m_Chest.Location.X, m_Chest.Location.Y, m_Chest.Location.Z + 1);
                    m_From.Direction = m_From.GetDirectionTo(m_Chest.GetWorldLocation());

                    if (m_Count == 14)
                    {
                        Stop();
                        m_From.EndAction(typeof(TreasureMap));

                        m_TreasureMap.Completed = true;

                        // checks to see if the map is a themed map and if so gets the theme type based on level of map
                        // and sends appropriate theme message/warning

                        // checks to see if the map is a themed map and already has a theme set
                        // and sends appropriate theme message/warning
                        if (themed == true && type != ChestThemeType.None) m_From.SendMessage(TreasureTheme.GetThemeMessage(type));

                        if (m_TreasureMap.Level >= 2)
                        {
                            //generates 1 of the highest mobs for pirate or undead iob chests
                            TreasureTheme.Spawn(m_Chest, m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, true, true);
                            //generates guardian spawn numbers based on if themed or not
                            for (int i = 0; i < TreasureTheme.GetGuardianSpawn(themed, type); ++i)
                            {
                                if (type == ChestThemeType.Undead || type == ChestThemeType.Pirate)
                                {
                                    //spawns rest of pirate or undead initial guardian spawn with our highest rank mobs appearing
                                    TreasureTheme.Spawn(m_Chest, m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, true, false);
                                }
                                else
                                {
                                    //not pirate or undead chest spawn as per normal random guardians
                                    TreasureTheme.Spawn(m_Chest, m_TreasureMap.Level, m_Chest.Location, m_Chest.Map, null, themed, type, false, false);
                                }
                            }

                            // 25% chance to spawn a tax collector on a regular chest, 100% on themed chests
                            if ((m_TreasureMap.Level > 3 &&
                                (Utility.RandomChance(25)) || themed == true) &&
                                Core.RuleSets.ThemedTreasureMaps() && Core.RuleSets.AngelIslandRules())
                            {
                                TaxCollector tc = new TaxCollector(m_Chest);
                                Point3D px = Spawner.GetSpawnPosition(m_Chest.Map, m_Chest.Location, 25, SpawnFlags.SpawnFar | SpawnFlags.AvoidPlayers, tc);
                                if (px != m_Chest.Location)
                                {   // got a good location
                                    tc.MoveToWorld(px, m_Chest.Map);
                                }

                                // if we get a tax collector, add a chance to get an additional rare. The chance is calc in the rare drop code
                                // this chest 'hides' another treasure
                                m_Chest.SetFlag(TreasureMapChest.iFlags.Hides, true);
                            }
                        }
                    }
                    else
                    {
                        if (m_From.Body.IsHuman && !m_From.Mounted)
                            m_From.Animate(11, 5, 1, true, false, 0);

                        new SoundTimer(m_From, 0x125 + (m_Count % 2)).Start();
                    }
                }
            }

            private class SoundTimer : Timer
            {
                private Mobile m_From;
                private int m_SoundID;

                public SoundTimer(Mobile from, int soundID)
                    : base(TimeSpan.FromSeconds(0.9))
                {
                    m_From = from;
                    m_SoundID = soundID;
                }

                protected override void OnTick()
                {
                    m_From.PlaySound(m_SoundID);
                }
            }
        }
        #endregion DIGGING
        #region CLICKING
        public virtual double GetReqSkillLevel(Mobile from)
        {
            switch (m_Level)
            {
                case 1: return 27.0;
                case 2: return 71.0;
                case 3: return 81.0;
                case 4: return 91.0;
                case 5: return 100.0;

                default: return 0.0;
            }
        }

        private bool HasRequiredSkill(Mobile from)
        {
            return (from.Skills[SkillName.Cartography].Value >= GetReqSkillLevel(from));
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (!m_Completed && m_Decoder == null)
            {
                double midPoint = GetReqSkillLevel(from);
                double minSkill = midPoint - 30.0;
                double maxSkill = midPoint + 30.0;

                if (from.Skills[SkillName.Cartography].Value < midPoint)
                {
                    from.SendLocalizedMessage(503013); // The map is too difficult to attempt to decode.
                    return;
                }

                if (from.CheckSkill(SkillName.Cartography, minSkill, maxSkill, contextObj: new object[2]))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503019); // You successfully decode a treasure map!
                    from.SendMessage("You may need a cartographer's sextant to locate this treasure.");
                    Decoder = from;

                    from.PlaySound(0x249);
                    base.OnDoubleClick(from);

                    if (m_Level > 3)
                        UsageReport(from, string.Format("level {0} map decoded", m_Level));
                }
                else
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 503018); // You fail to make anything of the map.
                }
            }
            else if (m_Completed)
            {
                from.SendLocalizedMessage(503014); // This treasure hunt has already been completed.
                base.OnDoubleClick(from);
            }
            else
            {
                from.SendMessage("You will need a cartographer's sextant to locate this treasure.");
                from.SendLocalizedMessage(503017); // The treasure is marked by the red pin. Grab a shovel and go dig it up!
                base.OnDoubleClick(from);
            }
        }

        public static void UsageReport(Mobile m, string text)
        {
            // Tell staff that an a player is using this system
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.TreasureMapUsageReport))
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Administrator,
                0x482,
                string.Format("At location: {0}, {1} ", m.Location, text));
        }

        public override int LabelNumber { get { return (m_Decoder != null ? 1041516 + m_Level : 1041510 + m_Level); } }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            list.Add(m_Map == Map.Felucca ? 1041502 : 1041503); // for somewhere in Felucca : for somewhere in Trammel

            if (m_Completed)
                list.Add(1041507, m_Decoder == null ? "someone" : m_Decoder.Name); // completed by ~1_val~
        }
        // changed this 
        // public override void OnSingleClick( Mobile from )
        //{
        //if ( m_Completed )
        //from.Send( new MessageLocalizedAffix( Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, string.Format( " completed by {0}", m_Decoder == null ? "someone" : m_Decoder.Name ), "" ) );
        //else if ( m_Decoder != null )
        //LabelTo( from, 1041516 + m_Level );
        //else
        //LabelTo( from, 1041522, string.Format( "#{0}\t \t#{1}", 1041510 + m_Level, m_Map == Map.Felucca ? 1041502 : 1041503 ) );
        //}
        //to this
        public override void OnSingleClick(Mobile from)
        {
            if (m_Completed)
                from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, string.Format(" completed by {0}", m_Decoder == null ? "someone" : m_Decoder.Name), ""));
            else if (m_Decoder != null)
            {   // non tattered
                // "an adeptly drawn treasure map";
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules() || (Core.RuleSets.SiegeRules() && PublishInfo.Publish >= 13))
                    LabelTo(from, 1041516 + m_Level);
                else
                    LabelTo(from, string.Format("a treasure map"));
            }
            else
            {   // tattered
                // "a tattered, adeptly drawn treasure map"
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisStyleRules() || (Core.RuleSets.SiegeRules() && PublishInfo.Publish >= 13))
                {
                    LabelTo(from, 1041510 + m_Level);
                    LabelTo(from, m_Map == Map.Felucca ? 1041502 : 1041503);
                }
                else
                {
                    LabelTo(from, string.Format("a tattered treasure map", m_Level));
                }
            }
        }
        #endregion CLICKING
        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
            writer.Write(m_Themed);
            writer.Write((int)m_type);

            writer.Write(m_Level);
            writer.Write(m_Completed);
            writer.Write(m_Decoder);
            writer.Write(m_Map);
            writer.Write(m_ChestLocation);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Themed = reader.ReadBool();
                        m_type = (ChestThemeType)reader.ReadInt();
                        goto case 0;
                    }

                case 0:
                    {
                        m_Level = (int)reader.ReadInt();
                        m_Completed = reader.ReadBool();
                        m_Decoder = reader.ReadMobile();
                        m_Map = reader.ReadMap();
                        m_ChestLocation = reader.ReadPoint2D();

                        break;
                    }
            }
            if (version < 1)
            {
                m_Themed = false;
                m_type = ChestThemeType.None;
            }
        }
        #endregion Serialization
    }

    public class TreasureMapQuest : TreasureMap
    {
        Item m_RaresPack;
        string m_MapFile;
        [CommandProperty(AccessLevel.GameMaster)]
        public Item RaresPack { get { return m_RaresPack; } /*set { m_RaresPack = value; }*/ }
        private string m_DecodedMsg = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public string DecodedMsg
        {
            get { return m_DecodedMsg; }
            set
            {
                m_DecodedMsg = value;
                SendMessageNearby("Hint: you can use the {decoder} macro to substitute in the decoder's name.");
            }
        }

        private string m_NotDecodedMsg = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public string NotDecodedMsg { get { return m_NotDecodedMsg; } set { m_NotDecodedMsg = value; } }

        private string m_CompletedMsg = null;
        [CommandProperty(AccessLevel.GameMaster)]
        public string CompletedMsg
        {
            get { return m_CompletedMsg; }
            set
            {
                m_CompletedMsg = value;
                SendMessageNearby("Hint: you can use the {decoder} macro to substitute in the decoder's name.");
            }
        }

        [Constructable]
        public TreasureMapQuest(int level, Map map, string MapFile, int RaresPackSerial)
            : base(level, map, false, ChestThemeType.None)
        {
            /* Fail vs ApplicationException
             * 1. Commandline [Add x y z
             *      Fail will generate a nice system message for the GM to see
             *      ApplicationException will generate template exception information issued by the CLR. You can't catch this exception without some fancy tricks.
             * 2. Created by the spawner (SpawnEngine.Build<Item>(value, ref reason))
             *      Fail generates a message likely on the internal or null map, so the GM never sees it
             *      ApplicationException will generate a nice system message for the GM to see
             * --- Conclusion ---
             * We'll just go with the ApplicationException since:
             * A. the most dangerous case is when the GM adds TreasureMapQuest to the LootString. We want the best possible message for this scenario.
             * B. if the GM issues the [Add x y z command line, we can survive with the template exception with the required param list
             * Final note: When using the Fail technique, the property doesn't get cleared like it does using the ApplicationException
             */
            m_MapFile = MapFile;
            if (!MapFileOK())
            {
                throw new ApplicationException(string.Format("Map file '{0}' could not be found", m_MapFile));
                //Timer.DelayCall(Fail, string.Format("Failure: Map file '{0}' could not be found", m_MapFile));
                goto done;
            }

            Item? pack = World.FindItem(RaresPackSerial);
            if (pack is Container cont)
            {
                if (cont.Items.Count == 0)
                {
                    throw new ApplicationException(string.Format("No rares in rare pack ({0})", RaresPackSerial));
                    //Timer.DelayCall(Fail, string.Format("Failure: No rares in rare pack ({0})", RaresPackSerial));
                    goto done;
                }
                m_RaresPack = Utility.DeepDupe(pack);
                m_RaresPack.MoveToIntStorage();
                base.Setup();
            }
            else
            {
                throw new ApplicationException(string.Format("No rares pack with serial {0} could be found", RaresPackSerial));
                //Timer.DelayCall(Fail, string.Format("Failure: No rares pack with serial {0} could be found", RaresPackSerial));
                goto done;
            }

            if (level > 1)
                Timer.DelayCall(Warning, "Warning: Only level 1 maps can be decoded by anyone. All other maps require the usual skill.");

            done:
            return;
        }
        public override void OnSingleClick(Mobile from)
        {
            if (Completed)
            {
                string decoder = string.Format("{0}", Decoder == null ? "someone" : Decoder.Name);
                if (!string.IsNullOrEmpty(m_CompletedMsg))
                    //from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, m_CompletedMsg.Replace("{decoder}", decoder), ""));
                    LabelTo(from, m_CompletedMsg.Replace("{decoder}", decoder));
                else
                    from.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Label, 0x3B2, 3, 1048030, "", AffixType.Append, string.Format(" completed by {0}", Decoder == null ? "someone" : Decoder.Name), ""));
            }
            else if (Decoder != null)
            {
                string decoder = string.Format("{0}", Decoder == null ? "someone" : Decoder.Name);
                if (!string.IsNullOrEmpty(m_DecodedMsg))
                    LabelTo(from, m_DecodedMsg.Replace("{decoder}", decoder));
                else
                    // non tattered
                    // "an adeptly drawn treasure map";
                    LabelTo(from, 1041516 + Level);
            }
            else
            {
                if (!string.IsNullOrEmpty(m_NotDecodedMsg))
                    LabelTo(from, m_NotDecodedMsg);
                else
                {
                    // tattered
                    // "a tattered, adeptly drawn treasure map"
                    LabelTo(from, 1041510 + Level);
                    LabelTo(from, Map == Map.Felucca ? 1041502 : 1041503);
                }
            }
        }
        public override void OnDelete()
        {
            base.OnDelete();
            if (m_RaresPack != null && !m_RaresPack.Deleted)
                m_RaresPack.Delete();
        }
        private void Warning(object o)
        {
            if (o is string s)
                SendMessageNearby(s, AccessLevel.GameMaster, 0x33);
        }
        //private void Fail(object o)
        //{
        //    if (o is string s)
        //        SendSystemMessage(s, AccessLevel.GameMaster, 0x33);

        //    this.Delete();
        //}
        private bool MapFileOK()
        {
            string path = string.Format("Binary Map Data/{0}", (ChestMap == Map.Felucca || ChestMap == Map.Trammel) ? "Britannia" : ChestMap.Name);
            return File.Exists(Path.Combine(Core.DataDirectory, path, m_MapFile + ".bin"));
        }
        private static Dictionary<string, List<Point2D>> LocationsTable = new Dictionary<string, List<Point2D>>(StringComparer.OrdinalIgnoreCase);
        public override Point2D GetRandomLocation(Map map)
        {
            Point2D point = Point2D.Zero;
            List<Point2D> locations;
            if (LocationsTable.ContainsKey(m_MapFile))
                locations = LocationsTable[m_MapFile];
            else
            {
                string path = string.Format("Binary Map Data/{0}", (ChestMap == Map.Felucca || ChestMap == Map.Trammel) ? "Britannia" : ChestMap.Name);
                locations = LocationsTable[m_MapFile] = LoadSpawnablePoints(Path.Combine(Core.DataDirectory, path, m_MapFile + ".bin"));
            }

            List<Point2D> list = new();
            for (int ix = 0; ix < locations.Count; ix++)
            {
                Point2D current = locations[Utility.Random(locations.Count)];
                if (Utility.CanSpawnLandMobile(map, new Point3D(current.X, current.Y, map.GetAverageZ(current.X, current.Y))))
                {
                    point = current;
                    break;
                }
            }

            return point;
        }
        public override double GetReqSkillLevel(Mobile from)
        {
            switch (base.Level)
            {   // level 1 maps can be decoded by anyone (whatever their skill is, that's enough)
                case 1: return from.Skills[SkillName.Cartography].Value;
                case 2: return 71.0;
                case 3: return 81.0;
                case 4: return 91.0;
                case 5: return 100.0;

                default: return 0.0;
            }
        }
        public List<Point2D> LoadSpawnablePoints(string pathname)
        {
            List<Point2D> list = new();
            if (File.Exists(pathname))
                Console.WriteLine("Spawnable points Loading...");
            else
            {
                Console.WriteLine("No spawnable points to Load.");
                return list;
            }
            try
            {
                Utility.TimeCheck tc = new Utility.TimeCheck();
                tc.Start();
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(pathname, FileMode.Open, FileAccess.Read)));

                int count = reader.ReadInt();
                for (int ix = 0; ix < count; ix++)
                    list.Add(reader.ReadPoint2D());

                reader.Close();
                tc.End();
                Console.WriteLine($"Loaded {list.Count} points in {tc.TimeTaken}");

                Console.WriteLine("Cleaning up points database...");
                tc.Start();
                List<Point2D> bad = new();
                foreach (var p2d in list)
                    if (BadLocation(p2d))
                        bad.Add(p2d);

                foreach (var p2d in bad)
                    list.Remove(p2d);

                tc.End();
                Console.WriteLine($"Removed {bad.Count} points, leaving in {list.Count} valid in {tc.TimeTaken}");
            }
            catch
            {
                Utility.Monitor.WriteLine($"Error reading {pathname}, using default values:", ConsoleColor.Red);
            }

            return list;
        }
        private static List<int> SnowTiles = new List<int>()
        {
            0x10C, 0x10F,
            0x114, 0x117,
            0x119, 0x11D,
            0x179, 0x18A,
            0x385, 0x38C,
            0x391, 0x394,
            0x39D, 0x3A4,
            0x3A9, 0x3AC,
            0x5BF, 0x5D6,
            0x5DF, 0x5E2,
            0x745, 0x748,
            0x751, 0x758,
            0x75D, 0x760,
            0x76D, 0x773
        };
        private bool BadLocation(Point2D loc)
        {
            Point3D p = new Point3D(loc.X, loc.Y, ChestMap.GetAverageZ(loc.X, loc.Y));

            LandTarget land = new LandTarget(p, ChestMap);
            if (land != null)
            {
                int tileID = land.TileID & 0x3FFF;
                bool contains = false;

                for (int i = 0; !contains && i < SnowTiles.Count; i += 2)
                    contains = (tileID >= SnowTiles[i] && tileID <= SnowTiles[i + 1]);

                if (contains)
                    return false;
            }

            return true;
        }
        public override void Setup() {; }
        public override void ChestCreated(TreasureMapChest chest)
        {
            // easy mode
            if (chest is ILockable lockable && base.Level == 1)
                lockable.Locked = false;

            if (m_RaresPack is Container c)
            {
                ArrayList alist = c.GetDeepItems();
                if (alist.Count > 0)
                {
                    Item? reward = (Item?)alist[Utility.Random(alist.Count)];
                    if (reward != null)
                    {
                        if ((reward = Utility.Dupe(reward)) != null)
                            ManagedDropItem(chest, reward);
                    }
                }
                return;
            }
            System.Diagnostics.Debug.Assert(false);
        }
        private void ManagedDropItem(Container c, Item item)
        {
            int before = c.Items.Count;     // TryDropItem is funky because it requires a Mobile from
            c.DropItem(item);               // this gives us nice placement withing the chest, AddItem does not (always upper right corner.)
            int after = c.Items.Count;
            if (before == after)            // check if we overflowed
                c.AddItem(item);            // force the add
        }
        #region Serialization
        public TreasureMapQuest(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write(m_DecodedMsg);
            writer.Write(m_NotDecodedMsg);
            writer.Write(m_CompletedMsg);

            // version 1
            writer.Write(m_RaresPack);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_DecodedMsg = reader.ReadString();
                        m_NotDecodedMsg = reader.ReadString();
                        m_CompletedMsg = reader.ReadString();

                        goto case 1;
                    }
                case 1:
                    {
                        m_RaresPack = reader.ReadItem();
                        break;
                    }
            }
        }
        #endregion Serialization
    }
}