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

/* Scripts/Engines/ChampionSpawn/ChampLevelData.cs
 *	ChangeLog:
 *	3/30/2024, Adam
 *	    Add Cove_Orcs
 *	12/11/2023, Adam
 *	    Add Mini_Christmas
 *	9/26/21, Adam
 *	    Redo the Bob champ using our new PolymorphicBob.
 *	    PolymorphicBob works like the Doppelganger in that a single monster class Polymorphs into all the different levels we need.
 *	    This new modle replaces the Bob modle we have today where we use Template Mobiles (nughtmares, etc.,) for the different levels.
 *	    These new Bobs also carry the new GuardIgnore property which allows to spawn them in a guarded town for PK-Free events.
 *  9/21/21, Adam
 *      Add the Adam Ant champ spawn
 *      Add support for in ChampEngine for the special "Doppelganger" spawns. 
 *	6/26/08, Adam
 *		increase timeouts for the Vampire levels since this is a champ for warriors
 *	5/31/08, Adam
 *		Add SpawnTypes.Vampire
 *  4/4/07, Adam
 *      Change "BongMagi" to "BoneMagi" (lol)
 *  3/16/07, Adam
 *      Add new SpawnTypes.Pirate
 *	11/01/2006, plasms
 *		Decreased big champ MaxSpawn to 1/4
 *	10/29/2006, plasma
 *		 Increased AI Guard spawn range from 6 to 12
 *	10/28/2006, plasma
 *		Initial creation
 * 
 **/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Server.Utility;

namespace Server.Engines.ChampionSpawn
{
    //[Flags]
    //public enum SpawnFlags
    //{
    //    None = 0x0,
    //    FactoryMobile = None,   // 0x01, Turned off for now
    //    SpawnFar = 0x02,        // spawn to the outer rim of the range
    //    SpawnNear = 0x04,       // spawn nearer the spawner
    //}

    // Pla: Core champion spawn class.  
    public class ChampLevelData
    {
        // Enum of spawn types to provide nice [props 
        public enum SpawnTypes
        {
            None,
            Abyss,
            Arachnid,
            ColdBlood,
            ForestLord,
            FrozenHost,
            UnholyTerror,
            VerminHorde,
            Deceit_Mini,
            Destard_Mini,
            Hythloth_Mini,
            Ice_Mini,
            Wind_Mini,
            Wrong_Mini,
            AI_Escape,
            AI_Guard,
            Test,
            Test2,
            Pirate_Full_Sea,
            Bob,
            Vampire,
            KinCity,
            Doppelganger,
            GargoyleQueen,
            Christmas_Mini,
            Pirate_Full_Land,
            Pirate_Mini,
            Cove_Orcs,
            GOT_Full,
            GOT_Mini,
            Pirate_Micro,
            Pirate_Micro_Melee,
        }

        // Members
        public string[] m_Monsters;             // Monster array
        public string m_Captain;                // one of these per level if it exists
        public string[] m_Names;                // Names and titles are paired. 
        public string[] m_Titles;               //  name1 pairs with title1, name2 pairs with title2, etc.
        public int m_MaxKills;                  // max kills
        public int m_MaxMobs;                   // max mobiles at once
        public int m_MaxSpawn;                  // spawn amount
        public int m_MaxRange;                  // max range from center
        public TimeSpan m_SpawnDelay;           // spawn delay
        public TimeSpan m_ExpireDelay;          // level down delay
        public SpawnFlags m_Flags;              // default flags for this level

        // Properties

        // Constructors
        public ChampLevelData(int max_kills, int max_mobs, int max_spawn, int max_range, TimeSpan spawn_delay, TimeSpan expire_delay, SpawnFlags flags, String[] monsters, string captain = null, string[] names=null, string[] titles=null)
        {
            //assign values
            m_Monsters = monsters;
            m_Captain = captain;
            m_Names = names;
            m_Titles = titles;
            m_MaxKills = max_kills;
            m_MaxMobs = max_mobs;
            m_MaxSpawn = max_spawn;
            m_MaxRange = max_range;
            m_SpawnDelay = spawn_delay;
            m_ExpireDelay = expire_delay;
            m_Flags = flags;
        }

        // this is called from the engine's deserialize to create a new set of 
        // levels based upon the serialized data
        public ChampLevelData(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        int count = reader.ReadInt();
                        List<string> list = new();
                        for(int ix=0; ix < count; ix++)
                            list.Add(reader.ReadString());
                        m_Names = list.ToArray();

                        count = reader.ReadInt();
                        list = new();
                        for (int ix = 0; ix < count; ix++)
                            list.Add(reader.ReadString());
                        m_Titles = list.ToArray();
                        goto case 3;
                    }
                case 3:
                    {
                        m_Captain = reader.ReadString();
                        goto case 2;
                    }
                case 2:
                    {
                        m_Flags = (SpawnFlags)reader.ReadInt();
                        goto case 0;    // skip case 1
                    }
                case 1:
                    {
                        bool unused = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        // read in a seriliased level !
                        m_Monsters = new string[reader.ReadInt()];
                        for (int i = 0; i < m_Monsters.Length; ++i)
                            m_Monsters[i] = reader.ReadString();

                        m_MaxKills = reader.ReadInt();
                        m_MaxMobs = reader.ReadInt();
                        m_MaxSpawn = reader.ReadInt();
                        m_MaxRange = reader.ReadInt();
                        m_SpawnDelay = reader.ReadTimeSpan();
                        m_ExpireDelay = reader.ReadTimeSpan();
                        break;
                    }

            }

        }

        public void Serialize(GenericWriter writer)
        {

            // Serialize level data
            writer.Write((int)4);               // version number

            // version 4
            if (m_Names == null)
                writer.Write(0);
            else
            {
                writer.Write(m_Names.Length);
                for (int jx = 0; jx < m_Names.Length; ++jx)
                    writer.Write(m_Names[jx]);
            }

            if (m_Titles == null)
                writer.Write(0);
            else
            {
                writer.Write(m_Titles.Length);
                for (int jx = 0; jx < m_Titles.Length; ++jx)
                    writer.Write(m_Titles[jx]);
            }

            // version 3
            writer.Write(m_Captain);

            // version 2
            writer.Write((int)m_Flags);             // spawn preferences

            // version 1
            //writer.Write(m_FactoryMobile);          // is this mobile created by a factory?

            writer.Write((int)m_Monsters.Length); // write amount of levels
            for (int i = 0; i < m_Monsters.Length; ++i)   // write monster array
                writer.Write((string)m_Monsters[i]);

            writer.Write(m_MaxKills);                   // write level data
            writer.Write(m_MaxMobs);
            writer.Write(m_MaxSpawn);
            writer.Write(m_MaxRange);
            writer.Write(m_SpawnDelay);
            writer.Write(m_ExpireDelay);

        }

        public Type GetRandomType()
        {
            // Select a monster at random from the array			
            return ScriptCompiler.FindTypeByName((string)m_Monsters[Utility.Random(m_Monsters.Length)]);
        }

        public Type GetCaptainType()
        {
            if (!string.IsNullOrEmpty(m_Captain))
                return ScriptCompiler.FindTypeByName(m_Captain);
            else
                return null;
        }

        // Static spawn generation funciton.
        // Create your spawns here!
        public static ArrayList CreateSpawn(SpawnTypes type)
        {
            ArrayList temp = new ArrayList();
            switch (type)
            {
                // Big champs first.
                // To emulate the original big champs exactly, we need 16 levels of spawn.
                // This is because the original champs were actually 16 levels, one for each red skull.
                // The spawns have 4 "big" levels split into 16 sub levels with this distribution:
                // Level 1 : 5 levels
                // Level 2 : 4 levels
                // Level 3 : 4 levels
                // Level 4 : 3 Levels					
                #region big champs
                case SpawnTypes.GOT_Full:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1 "
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "GotWarrior", "GotWolf", "GotZombie", "GotSkeleton" }, captain: "GotWhiteWalker"));

                        for (int i = 1; i <= 4; ++i)    //Level " 2 "
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "GotMage", "GotWarrior", "GotWolf", "GotZombie", "GotSkeleton", "GotBoneKnight" }, captain: "GotWhiteWalker"));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "GotMage", "GotWarrior", "GotBoneKnight" }, captain: "GotWhiteWalker"));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "GotMage", "GotWarrior" }, captain: "GotWhiteWalker"));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, 1, 1, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None,
                            new string[] { "GotNightKing" }));
                        break;
                    }
                case SpawnTypes.GOT_Mini:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "GotWarrior", "GotWolf", "GotZombie", "GotSkeleton" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "GotMage", "GotWarrior", "GotWolf", "GotZombie", "GotSkeleton", "GotBoneKnight" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "GotMage", "GotWarrior", "GotBoneKnight" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, 1, 8, TimeSpan.Zero, TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "GotWhiteWalker" }));
                        break;
                    }
                case SpawnTypes.Cove_Orcs:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1 "
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "Orc" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2 "
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "Orc", "OrcishMage", "OrcCaptain", "OrcCaptain", "OrcCaptain", }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "Orc", "OrcishMage", "OrcishLord", "OrcishLord", "OrcishLord", }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "Orc", "OrcishMage", "OrcCaptain", "OrcishLord", "OrcBomber", "OrcBrute", }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None,
                            new string[] { "Khartag" }));
                        break;
                    }
                case SpawnTypes.GargoyleQueen:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1 "
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "Imp" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2 "
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "Gargoyle" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "Gargoyle", "StoneGargoyle" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None,
                                new string[] { "Gargoyle", "FireGargoyle" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None,
                            new string[] { "QueenZhah" }));
                        break;
                    }
                case SpawnTypes.Doppelganger:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1 "
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Doppelganger" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2 "
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Doppelganger" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Doppelganger" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Doppelganger" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "AdamAnt" }));
                        break;
                    }
                case SpawnTypes.Abyss:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1 "
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Mongbat", "Imp" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2 "
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Gargoyle", "Harpy" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "FireGargoyle", "StoneGargoyle" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Daemon", "Succubus" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Semidar" }));
                        break;
                    }
                case SpawnTypes.Arachnid:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Scorpion", "GiantSpider" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "TerathanDrone", "TerathanWarrior" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "DreadSpider", "TerathanMatriarch" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PoisonElemental", "TerathanAvenger" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Mephitis" }));
                        break;
                    }
                case SpawnTypes.ColdBlood:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Lizardman", "Snake" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "LavaLizard", "OphidianWarrior" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Drake", "OphidianArchmage" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Dragon", "OphidianKnight" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Rikktor" }));
                        break;
                    }
                case SpawnTypes.ForestLord:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Pixie", "ShadowWisp" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Kirin", "Wisp" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Centaur", "Unicorn" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "EtherealWarrior", "SerpentineDragon" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "LordOaks" }));
                        break;
                    }
                case SpawnTypes.FrozenHost:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "FrostOoze", "FrostSpider" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "FrostTroll", "IceSerpent" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "SnowElemental", "FrostNymph" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "IceFiend", "WhiteWyrm" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Azothu" }));
                        break;
                    }
                case SpawnTypes.UnholyTerror:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level "1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Bogle", "Ghoul", "Shade", "Spectre", "Wraith" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "BoneMagi", "Mummy", "SkeletalMage" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "BoneKnight", "BoneMagiLord", "SkeletalKnight" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "BoneKnightLord", "RottingCorpse" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Neira" }));
                        break;
                    }
                case SpawnTypes.VerminHorde:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "GiantRat", "Slime" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Ratman", "DireWolf" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25, max_spawn: 6, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "HellHound", "RatmanMage" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17, max_spawn: 4, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "RatmanArcher", "SilverSerpent" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Barracoon" }));

                        break;
                    }

                case SpawnTypes.Bob:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PolymorphicBob" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PolymorphicBob" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25 / 2, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PolymorphicBob" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17 / 2, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PolymorphicBob" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "TheOneBob" }));

                        break;
                    }

                case SpawnTypes.Vampire:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 20, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "VampireBat" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 20, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "VampireBat", "WalkingDead" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25 / 2, 6, 20, TimeSpan.Zero, TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "WalkingDead", "WalkingDead" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17 / 2, 4, 20, TimeSpan.Zero, TimeSpan.FromMinutes(50), SpawnFlags.None, new string[] { "Vampire" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(60), SpawnFlags.None, new string[] { "VladDracula" }));

                        break;
                    }

                #endregion

                // Now for the mini champs.
                // These are just 3 levels with the fourth spawning a single mob, like a  champ
                #region mini champs
                case SpawnTypes.Deceit_Mini:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Skeleton" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "SkeletalKnight", "BoneKnight" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "SkeletalMage", "BoneMagi" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "BoneDemon", "SkeletalDragon" }));
                        break;
                    }
                case SpawnTypes.Destard_Mini:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Lizardman", "OphidianWarrior" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "Drake", "Wyvern" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Dragon", "OphidianKnight" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "AncientWyrm" }));
                        break;
                    }
                case SpawnTypes.Hythloth_Mini:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Imp", "HellHound" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "Gargoyle", "ChaosDaemon" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Succubus", "Daemon" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "Balron" }));
                        break;
                    }
                case SpawnTypes.Ice_Mini:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "FrostOoze", "FrostSpider" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "IceElemental", "FrostSpider" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "IceFiend", "FrostNymph" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(20), SpawnFlags.None, new string[] { "ArcticOgreLord" }));
                        break;
                    }
                case SpawnTypes.Wind_Mini:              // Wind has a different expire delay for level 4
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "EvilMage" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "Lich", "EvilMageLord" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "LichLord", "CouncilMember" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "AncientLich" }));
                        break;
                    }
                case SpawnTypes.Wrong_Mini:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "Zombie", "HeadlessOne", "Slime" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "GoreFiend", "FleshGolem" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "BloodElemental", "RottingCorpse" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "FleshRenderer" }));
                        break;
                    }
                case SpawnTypes.Christmas_Mini:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PolymorphicMisfitToy" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "PolymorphicMisfitToy", "PolymorphicToySoldier" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "PolymorphicToySoldier" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "AbominableSnowman" }));
                        break;
                    }
                #endregion

                #region Pirates!
                /*
                 * Increase range to 75 for water mobs
                 * Reduce the number of mobs spawned at once by 1/2 when Pirates are involved.
                 */
                case SpawnTypes.Pirate_Full_Sea:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 75, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.SpawnFar, new string[] { "WaterElemental", "SeaSerpent" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 75, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.SpawnFar, new string[] { "SeaSerpent", "Kraken" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25 / 2, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PirateDeckHand", "PirateWench" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17 / 2, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PirateWench", "Pirate" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "PirateChamp" }));

                        break;
                    }

                /*
                 * Reduce the number of mobs spawned at once by 1/2 when Pirates are involved.
                 */
                case SpawnTypes.Pirate_Full_Land:
                    {
                        for (int i = 1; i <= 5; ++i)    // Level " 1" 
                            temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 10, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "GiantRat", "PirateDeckHand" }));

                        for (int i = 1; i <= 4; ++i)    //Level " 2" 
                            temp.Add(new ChampLevelData(max_kills: 38, max_mobs: 38, max_spawn: 9, max_range: 24, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PirateWench", "BilgeRat", "Lancehead" }));

                        for (int i = 1; i <= 4; ++i)    // Level " 3 "
                            temp.Add(new ChampLevelData(max_kills: 25, max_mobs: 25 / 2, 6, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PirateDeckHand", "PirateWench" }));

                        for (int i = 1; i <= 3; ++i)    // Level " 4 "
                            temp.Add(new ChampLevelData(max_kills: 17, max_mobs: 17 / 2, 4, 24, TimeSpan.Zero, TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PirateWench", "Pirate" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "PirateChamp" }));

                        break;
                    }

                case SpawnTypes.Pirate_Mini:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PirateDeckHand" }));
                        temp.Add(new ChampLevelData(max_kills: 15, max_mobs: 4, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "PirateWench" }));
                        temp.Add(new ChampLevelData(max_kills: 16, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Pirate" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "RedBeard" }, 
                            names: new string[]  { "Captain Kidd", "Captain Edward Teach", "Captain Blackbeard",   "Calico Jack", "Sir Henry Morgan", "Captain Thomas Edwards" }, 
                            titles: new string[] { null,           null,                   null,                   null,          null,               null}));
                        break;
                    }
                case SpawnTypes.Pirate_Micro:
                    {
                        temp.Add(new ChampLevelData(max_kills: 9, max_mobs: 3, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PirateDeckHand" }));
                        temp.Add(new ChampLevelData(max_kills: 3, max_mobs: 2, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(15), SpawnFlags.None, new string[] { "PirateWench" }));
                        temp.Add(new ChampLevelData(max_kills: 2, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Pirate" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "RedBeard" }));
                        break;
                    }
                case SpawnTypes.Pirate_Micro_Melee:
                    {
                        temp.Add(new ChampLevelData(max_kills: 12, max_mobs: 5, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(10), SpawnFlags.None, new string[] { "PirateDeckHand" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 8, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Pirate" }));
                        break;
                    }
                #endregion Pirates!

                // Angel Island prison system spawns 			
                // These guys take some of their spawn level settings from statics in CoreAI
                #region AI Level system
                case SpawnTypes.AI_Escape:
                    {
                        temp.Add(new ChampLevelData(max_kills: CoreAI.SpiritFirstWaveNumber, max_mobs: 5, max_spawn: 5, max_range: 18, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(CoreAI.SpiritExpireDelay), SpawnFlags.None, new string[] { "Spirit" }));
                        temp.Add(new ChampLevelData(max_kills: CoreAI.SpiritSecondWaveNumber, max_mobs: 5, max_spawn: 5, max_range: 18, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(CoreAI.SpiritExpireDelay), SpawnFlags.None, new string[] { "VengefulSpirit" }));
                        temp.Add(new ChampLevelData(max_kills: CoreAI.SpiritThirdWaveNumber, max_mobs: 5, max_spawn: 5, max_range: 18, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(CoreAI.SpiritExpireDelay), SpawnFlags.None, new string[] { "Soul" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 18, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(CoreAI.SpiritExpireDelay), SpawnFlags.None, new string[] { "AngelofJustice" }));
                        break;
                    }
                case SpawnTypes.AI_Guard:
                    {
                        temp.Add(new ChampLevelData(max_kills: 5, max_mobs: 5, max_spawn: 5, max_range: 12, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(CoreAI.GuardSpawnExpireDelay), SpawnFlags.None, new string[] { "AIPostGuard" }));
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 12, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(CoreAI.GuardSpawnExpireDelay), SpawnFlags.None, new string[] { "AIGuardCaptain" }));
                        break;
                    }

                #endregion

                #region Kin

                //Kin city consists purely of golem controllers.  
                case SpawnTypes.KinCity:
                    {
                        temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 5, max_range: 40, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromHours(1), SpawnFlags.None, new string[] { "GolemController" }));
                        temp.Add(new ChampLevelData(max_kills: 40, max_mobs: 40, max_spawn: 5, max_range: 40, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromHours(1), SpawnFlags.None, new string[] { "GolemController" }));
                        break;
                    }

                #endregion

                #region plamsa's test stuff!
                case SpawnTypes.Test:
                    {
                        for (int i = 0; i < 36; ++i)
                            temp.Add(new ChampLevelData(max_kills: 10, max_mobs: 10, max_spawn: 5, max_range: 5, spawn_delay: TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "slime", "rat" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Barracoon", "Semidar", "Rikktor", "Neira", "LordOaks", "Mephitis" }));
                        break;
                    }
                case SpawnTypes.Test2:
                    {
                        temp.Add(new ChampLevelData(max_kills: 5, max_mobs: 3, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Dragon", "WhiteWyrm" }));
                        temp.Add(new ChampLevelData(max_kills: 300, max_mobs: 150, max_spawn: 1, max_range: 15, spawn_delay: TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "GiantRat", "Slime" }));
                        temp.Add(new ChampLevelData(max_kills: 100, max_mobs: 30, max_spawn: 5, max_range: 12, spawn_delay: TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Orc", "OrcishMage" }));
                        temp.Add(new ChampLevelData(max_kills: 4, max_mobs: 3, max_spawn: 1, max_range: 5, spawn_delay: TimeSpan.FromSeconds(12), TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "GolemController" }));

                        //Champion!
                        temp.Add(new ChampLevelData(max_kills: 1, max_mobs: 1, max_spawn: 1, max_range: 1, spawn_delay: TimeSpan.Zero, expire_delay: TimeSpan.FromMinutes(40), SpawnFlags.None, new string[] { "Barracoon", "Semidar", "Rikktor", "Neira", "LordOaks", "Mephitis" }));
                        break;
                    }
            }
            #endregion

            return temp;
        }

    }

}