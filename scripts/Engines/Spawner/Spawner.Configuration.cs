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

/* Scripts\Engines\Spawner\Spawner.Configuration.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using System;
using static Server.Utility;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        private SpawnerAttribs m_SpawnerAttribs = SpawnerAttribs.None;          // serialized
        private bool m_CoreSpawn = false;   // 'core' spawn is that spawn shown in https://uo.stratics.com/hunters/spawn/spawnmap.jpg We will use this as a guide to populate our world
        private UInt32 m_PatchID = 0;
        private ShardConfig m_Shard = ShardConfig.AngelIsland;
        private string m_Source = string.Empty;
        private bool m_NeedsReview = false;

        #region Spawner Attribs
        /// <summary>
        /// We have a tool ClassicRespawn which replaces AI Classic spawners with those defined by Nerun's Distro
        /// Nerun's Distro seems to be a well thought out Classic OSI spawn. 
        /// We set the flags below on spawners that have already been processed by the system. Otherwise we would be reprocessing the same spawners over and over again
        ///     should we run the tool a second time.
        /// </summary>
        [Flags]
        public enum SpawnerAttribs : UInt32
        {
            None = 0x0,                                 // no special attributes
            Dummy = 0x01,                               // dummy creature, doesn't move, doesn't take damage, no fight mode
            GuardIgnore = 0x02,                         // Creatures on this spawner are ignored by guards (Town Invasion)
            Running = 0x08,                             // to do - move spawner bool value to bitfield
            m_Group = 0x10,                             // to do - move spawner bool value to bitfield
            DynamicCopy = 0x20,                         // to do - move spawner bool value to bitfield
            IsConcentric = 0x40,                        // to do - move spawner bool value to bitfield
            CoreSpawn = 0x80,                           // to do - move spawner bool value to bitfield
            #region needs to be kept in sync with SpawnerModeAttribs below
            ModeAI = 0x100,                             // ignored if we run the ClassicRespawn again - new AI's Distro spawner
            ModeNeruns = 0x200,                         // ignored if we run the ClassicRespawn again - new Nerun's Distro spawner
            ModeLegacy = 0x400,                         // pre distro model. i.e., original AI spawners
            ModeMulti = 0x800,                          // multi spawners supported (non Nerun) (crow:magpie:raven)
            #endregion needs to be kept in sync with SpawnerModeAttribs below
            Replaced = 0x1000,                          // Set in ClassicRespawn: ignored if we run the ClassicRespawn again - has been replaced by Nerun's Distro
            Kept = 0x2000,                              // Set in ClassicRespawn: ignored if we run the ClassicRespawn again - has been kept in spite of Nerun's Distro
            Debug = 0x4000,                             // turns on spawner debugging messages
            StaticCorpse = 0x8000,                      // kills the mobile and sets the corpse to static - uses the spawners next spawn as the decay time
            External = 0x10000,                         // we are being 'triggered' externally. When this flag is set, we don't cleanup the mobile if this is an event spawner
            TemplateMode = 0x20000,                     // if zero, we are using the default 'CopyProperties' for templates, otherwise it's 'Dupe'
            TamableOverride = 0x40000,                  // We don't normally allow modified creatures to be tamable. Seers must explicitly enable this
            Robot = 0x80000,                            // robot creature - robot AI, not tame, but only wonders and stays (control orders.)
            BlockDamage = 0x100000,                     // creature does not take damage
            Team = 0x200000,                            // team has changed
            MissionCritical = 0x400000,                 // BlockDamage + MissionCritical == Hulk Smash (usually used with a way point.)
            DropsSterling = 0x800000,                   // creature drops sterling
            AutoHome = 0x1000000,                      // creature return to home when the last mobile has left the region
        }
        [Flags]
        public enum SpawnerModeAttribs : UInt32         // needs to be kept in sync with SpawnerAttribs above
        {
            None = 0x0,                                 // no special attributes
            ModeAI = 0x100,                             // ignored if we run the ClassicRespawn again - new AI's Distro spawner
            ModeNeruns = 0x200,                         // ignored if we run the ClassicRespawn again - new Nerun's Distro spawner
            ModeLegacy = 0x400,                         // pre distro model. i.e., original AI spawners
            ModeMulti = 0x800,                          // multi spawners supported (non Nerun) (crow:magpie:raven)
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual SpawnerModeAttribs Distro
        {
            get { return (SpawnerModeAttribs)(m_SpawnerAttribs & (SpawnerAttribs.ModeNeruns | SpawnerAttribs.ModeAI | SpawnerAttribs.ModeLegacy | SpawnerAttribs.ModeMulti)); }

            set
            {
                SpawnerModeAttribs oldSpawnerModeAttribs = Distro;
                if (value == SpawnerModeAttribs.ModeNeruns)
                {
                    SetFlag(SpawnerAttribs.ModeNeruns, true);
                    SetFlag(SpawnerAttribs.ModeAI, false);
                    SetFlag(SpawnerAttribs.ModeLegacy, false);
                    SetFlag(SpawnerAttribs.ModeMulti, false);
                }
                else if (value == SpawnerModeAttribs.ModeAI)
                {
                    SetFlag(SpawnerAttribs.ModeAI, true);
                    SetFlag(SpawnerAttribs.ModeNeruns, false);
                    SetFlag(SpawnerAttribs.ModeLegacy, false);
                    SetFlag(SpawnerAttribs.ModeMulti, false);
                }
                else if (value == SpawnerModeAttribs.ModeLegacy)
                {
                    SetFlag(SpawnerAttribs.ModeLegacy, true);
                    SetFlag(SpawnerAttribs.ModeNeruns, false);
                    SetFlag(SpawnerAttribs.ModeAI, false);
                    SetFlag(SpawnerAttribs.ModeMulti, false);
                }
                else if (value == SpawnerModeAttribs.ModeMulti)
                {
                    SetFlag(SpawnerAttribs.ModeMulti, true);
                    SetFlag(SpawnerAttribs.ModeNeruns, false);
                    SetFlag(SpawnerAttribs.ModeAI, false);
                    SetFlag(SpawnerAttribs.ModeLegacy, false);
                }
                else
                {
                    SetFlag(SpawnerAttribs.ModeAI, false);
                    SetFlag(SpawnerAttribs.ModeNeruns, false);
                    SetFlag(SpawnerAttribs.ModeLegacy, false);
                    SetFlag(SpawnerAttribs.ModeMulti, false);
                    Utility.Monitor.WriteLine(string.Format("Error: Invalid spawner designation for spawner {0} in {1}", this, Utility.FileInfo()), ConsoleColor.Red);
                }

                UpdateDisplay();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool ModeAI
        {
            get { return GetFlag(SpawnerAttribs.ModeAI); }
            set
            {
                bool oldModeAI = ModeAI;
                SpawnerModeAttribs oldSpawnerModeAttribs = Distro;
                if (value == true)
                    Distro = SpawnerModeAttribs.ModeAI;
                else
                    Distro &= ~SpawnerModeAttribs.ModeAI;

                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool ModeNeruns
        {
            get { return GetFlag(SpawnerAttribs.ModeNeruns); }
            set
            {
                bool oldModeNeruns = ModeNeruns;
                SpawnerModeAttribs oldSpawnerModeAttribs = Distro;
                if (value == true)
                    Distro = SpawnerModeAttribs.ModeNeruns;
                else
                    Distro &= ~SpawnerModeAttribs.ModeNeruns;

                InvalidateProperties();
            }
        }
        // don't make this a command property until we allow the GM to set the EntryCounts
        //  currently it's only set programmatically where we fill in the EntryCounts
        public virtual bool ModeMulti
        {
            get { return GetFlag(SpawnerAttribs.ModeMulti); }
            set
            {
                bool oldModeMulti = ModeMulti;
                SpawnerModeAttribs oldSpawnerModeAttribs = Distro;
                if (value == true)
                    Distro = SpawnerModeAttribs.ModeMulti;
                else
                    Distro &= ~SpawnerModeAttribs.ModeMulti;

                InvalidateProperties();
            }
        }
        public bool Replaced
        {
            get { return GetFlag(SpawnerAttribs.Replaced); }
            set { SetFlag(SpawnerAttribs.Replaced, value); }
        }
        public bool Kept
        {
            get { return GetFlag(SpawnerAttribs.Kept); }
            set { SetFlag(SpawnerAttribs.Kept, value); }
        }

        public bool GetFlag(SpawnerModeAttribs flag)
        {
            return (((SpawnerModeAttribs)m_SpawnerAttribs & flag) != 0);
        }
        public void SetFlag(SpawnerModeAttribs flag, bool value)
        {
            if (value)
                m_SpawnerAttribs |= (SpawnerAttribs)flag;
            else
                m_SpawnerAttribs &= ~((SpawnerAttribs)flag);
        }
        public bool GetFlag(SpawnerAttribs flag)
        {
            return ((m_SpawnerAttribs & flag) != 0);
        }
        public void SetFlag(SpawnerAttribs flag, bool value)
        {
            if (value)
                m_SpawnerAttribs |= flag;
            else
                m_SpawnerAttribs &= ~flag;
        }
        #endregion Spawner Attribs
        #region Shard Config
        [Flags]
        public enum ShardConfig : UInt32
        {
            None = 0x0,                                         // error
            Siege = 0x01,                                       // Siege is Core spawn (no special spawn)
            AngelIsland = 0x02,                                 // AngelIsland + special spawn
            Seasons = 0x04,                                     // Seasons
            Mortalis = 0x08,                                    // Mortalis is Siege spawn (no special spawn) but maybe some 'Mortalis only' spawns
            AllShards = 0x10 | Siege | AngelIsland |
                        Mortalis | Seasons | Upheaval,     // non-core, but good for all shards
            Upheaval = 0x20,
            Core = 0x100,                                       // standard UO spawn https://uo.stratics.com/hunters/spawn/spawnmap.jpg good for all shards
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool NeedsReview
        {
            get { return m_NeedsReview; }
            set
            {
                m_NeedsReview = value;
            }
        }
        private void _defaultSpawnMap()
        {
            if (m_CoreSpawn == true)
            {
                Source = "Pub15";       // denotes this as pub 15 standard spawn. 
            }
            else
            {
                Source = Core.Server;   // denotes this as a Server Special - non standard. )                               
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Source
        {
            get { return m_Source; }
            set
            {
                m_Source = value;
            }
        }
        /// <summary>
        /// What shard this spawner is targeting.
        /// AngelIsland is the default, and is the primary spawn config for all shards.
        /// Something like "Mortalis" will only spawn if the shard config is Mortalis
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual ShardConfig Shard
        {
            get { return m_Shard; }
            set
            {
                m_Shard = value;
                UpdateDisplay();
            }
        }
        /// <summary>
        /// Concentric controls how spawn are scattered. We use Concentric when spawning over large areas and we don't want clumping of 
        /// spawn too close to the spawner, or two far away. Concentric attempts to spread the spawn more evenly (could probably use a better algo.)
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Concentric
        {
            get { return (m_SpawnerFlags & SpawnFlags.Concentric) != 0; }
            set
            {
                // adam: Log the change to IsConcentric
                if (Concentric != value)
                {
                    SpawnFlags oldSpawnFlags = m_SpawnerFlags;

                    if (value)
                        m_SpawnerFlags |= SpawnFlags.Concentric;
                    else
                        m_SpawnerFlags &= ~SpawnFlags.Concentric;

                    InvalidateProperties();
                }
            }
        }
        /// <summary>
        /// All the 'core' spawners were derived from the official UO spawn map.
        /// https://uo.stratics.com/hunters/spawn/spawnmap.jpg
        /// if it is not 'core', it is an AngelIsland special spawn
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CoreSpawn
        {
            get { return m_CoreSpawn; }
            set
            {
                m_CoreSpawn = value;
            }
        }
        public void SetFlag(ShardConfig flag, bool value)
        {
            if (value)
                m_Shard |= flag;
            else
                m_Shard &= ~flag;

            UpdateDisplay();
        }
        public bool GetFlag(ShardConfig flag)
        {
            return ((m_Shard & flag) != 0);
        }
        #endregion Shard Config
        public static bool ShouldShardEnable(Spawner s)
        {
            return ShouldShardEnable(s.Shard);
        }
        public static bool ShouldShardEnable(ShardConfig shard)
        {
            if (shard == Spawner.ShardConfig.Core) return true;      // yup, it's exactly a 'core' spawner 
            if (shard == Spawner.ShardConfig.AllShards) return true; // Maybe not 'core', but should be available on all shards (Kin, Prison, etc.)
            else if (Core.RuleSets.AngelIslandRules() && shard.HasFlag(Spawner.ShardConfig.AngelIsland)) return true;
            else if (Core.RuleSets.SiegeRules() && shard.HasFlag(Spawner.ShardConfig.Siege)) return true;
            else if (Core.RuleSets.MortalisRules() && shard.HasFlag(Spawner.ShardConfig.Mortalis)) return true;
            else if (Core.RuleSets.SeasonsRules() && shard.HasFlag(Spawner.ShardConfig.Seasons)) return true;
            else if (Core.RuleSets.UpheavalRules() && shard.HasFlag(Spawner.ShardConfig.Upheaval)) return true;
            return false;
        }
        public virtual UInt32 PatchID
        {
            get { return m_PatchID; }
            set { m_PatchID = value; }
        }
    }
}
