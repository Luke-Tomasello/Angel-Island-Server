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

/* Scripts/Engines/ChampionSpawn/ChampLevelData.cs
 *	ChangeLog:
 *	10/27/2026, Adam
 *	    Champion definitions moved to Data/ChampSpawns.xml (Scripts/Engines/ChampionSpawn/ChampDefinitions.cs): CreateSpawn()
 *	    and the SpawnTypes enum are removed. The XML was generated from CreateSpawn() and verified level-for-level against it.
 *	    ChampLevelData stays the runtime (and serialized) form of a level; saved SpawnTypes ordinals are read through
 *	    LegacyChampTypes.
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
        // Members
        public string[] m_Monsters;             // Monster array
        public string? m_Captain;                // one of these per level if it exists
        public string[]? m_Names;                // Names and titles are paired. 
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
        public ChampLevelData(int max_kills, int max_mobs, int max_spawn, int max_range, TimeSpan spawn_delay, TimeSpan expire_delay, SpawnFlags flags, String[] monsters, string? captain = null, string[]? names = null, string[]? titles = null)
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
                        for (int ix = 0; ix < count; ix++)
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

        public Type? GetCaptainType()
        {
            if (!string.IsNullOrEmpty(m_Captain))
                return ScriptCompiler.FindTypeByName(m_Captain);
            else
                return null;
        }

    }

}
