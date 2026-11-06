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

/* Scripts/Engines/ChampionSpawn/ChampEngine.cs
 * ChangeLog:
 *  10/28/2026, Adam (MaxLevels, version 19)
 *      MaxLevels (GM): the total number of spawn levels a run plays (0 = all). The remaining budget is spread evenly over the
 *      levels still ahead and always ends on the last spawn level; the boss level always runs last. Levels already run count
 *      toward the total (m_LevelsRun), so a change mid-run applies at the next advance. Levels are skipped, never removed.
 *      Start, advance, rollback, completion, Level and MaxLevels changes are written to Logs/ChampLevels.log.
 *  10/27/2026, Adam (DefinitionName, version 18)
 *      Champion definitions now come from Data/ChampSpawns.xml (ChampDefinitions). DefinitionName (a string) replaces
 *      SpawnType (the SpawnTypes enum, removed): setting it applies that definition (new levels; restarts the champ if it
 *      is running), as SpawnType did; an unknown name leaves the champ unchanged (reported). The serialized levels remain
 *      the champ's own snapshot and are never rebuilt on load.
 *      Version 18 saves the definition name where the SpawnTypes ordinal was; older saves map the ordinal through
 *      LegacyChampTypes. A champ with no definition (SpawnTypes.None) still becomes Abyss on load.
 *      [props offers the loaded definition names as a pick list for DefinitionName ([StringList]), as it did for the enum.
 *  3/31/2024, Adam (ITriggerable)
 *  11/14/2023, Adam (Slayer/Team)
 *      Allow the setting of a Slayer and Team properties on the mobile itself.
 *      This property allows the creature to hit with Slayer weapon damage for any weapon besides Fists.
 *      For now, this is useful for testing. For example: testing Creature vs Undead damage levels without having the modify each weapon the creature uses.
 *      See Also: The Slayer property in BaseCreature. (For having the mob with pseudo Slayer weapons.)
 *      Deep dive: While testing the Vampire champ spawn, I wanted to gage its relative strength to other champs. This is complicated by the fact
 *          that Silver weapons are needed to accurately asses the strength of the Vampire (undead) champ.
 *      With this system in place, you can for example, configure the BoB champ to be Slayer Silver. Then set the ChampEngine for Bob to Team 2.
 *      This will cause all mobs of the BoB champ to fight the Vampire champ with pseudo Silver weapons.
 *  11/9/2023, Adam (GetSurfaceTop)
 *      Add a local version of GetSurfaceTop to determine the Surface Top of the spawn location. 
 *      This is really only important for the champ as he needs to spawn on top of the platform (if there is one)
 *  8/2/2023, Adam (OnAfterSpawn())
 *      Call the base creatures OnAfterSpawn() so that alignment can be setup.
 *  9/21/21, Adam (CompareLevel())
 *      Add the Adam Ant champ spawn
 *      Add support for the special "Doppelganger" spawns. CompareLevel() doesn't work with the 
 *      "Doppelganger" spawns since each level looks exactly like the last
 *	8/12/10, adam
 *		Replace GetSpawnLocation() implementation with a shared version if Spawner.cs
 *	6/23/10, adam
 *		return LongTermMurders to Kills .. got changed in a global search and replace
 *	04/27/09, plasma
 *		Virtualised GetSpawnLocation, made IsNearPlayer protected.
 *	04/07/09, plasma
 *		Made PrepMob protected/virtual. Added RangeScale.
 *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *	07/27/2007, plasma
 *		- Commented out mobile factory property as now replaced with gump
 *		- Launch new mobile factory gump on doubleclick if Seer+
 *  4/2/07, Adam
 *      Add DebugDump() function to dump state when we get one of the following exceptions:
 *      Value cannot be null.
 *      Parameter name: type
 *      at System.Activator.CreateInstance(Type type, Boolean nonPublic)
 *      at Server.Engines.ChampionSpawn.ChampEngine.Spawn(Type[] types)
 *  3/17/07, Adam
 *      Add MobileFactory system. 
 *          - Allow the adding of MobileFactory (spawners) instead of raw creatures.
 *            This allows us to spawn custom creatures
 *          - Add a lvl_Error to the command properties so we can see if we are missing a factory
 *          - 
 *  02/11/2007, plasma
 *      Changed serialise to check for valid graphics, and if not to 
 *      reverse the graphics bool.
 *  01/13/2007, plasma
 *      Added NextSpawn property and ensured on deserialisation that the
 *      Delay is carried over rather than restarted.
 *  12/28/2006, plasma
 *      Virtualised RestartDelay property for ChampAngelIsland
 *	11/07/2006, plasma
 *		Fixed arraylist bug
 *	11/06/2006, plasma
 *		Serial cleanup
 *	11/01/2006, plasma
 *		Added ebb and flow system for smooth level transition
 *		Fixed null ref bug with free list
 *	10/29/2006, plasma
 *		Added WipeMonsters() into AdvanceLevel()
 *	10/29/2006, plasma
 *		Removed line that was causing navdest to not work
 *		Added serialisation for navdest :)				
 *	10/28/2006, plasma
 *		Initial creation
 * 
 **/
using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Items.Triggers;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Engines.ChampionSpawn
{
    [Flags]
    public enum ChampGFX
    {
        None = 0x00,
        Altar = 0x20,
        Platform = 0x40,
    }

    // Plasma: Core champion spawn class.  
    public abstract class ChampEngine : Item, ITriggerable
    {
        public override Item Dupe(int amount)
        {
            ChampEngine? new_item = Activator.CreateInstance(this.GetType()) as ChampEngine;
            Utility.CopyProperties(this, new_item);

            return base.Dupe(new_item, amount);
        }
        public static List<ChampEngine> Instances = new List<ChampEngine>();
        #region Morph Mode
        private MorphMode m_MorphMode = MorphMode.Default;
        [CommandProperty(AccessLevel.Seer)]
        public MorphMode MorphMode { get { return m_MorphMode; } set { m_MorphMode = value; } }
        #endregion Morph Mode
        #region Sterling System
        private ushort m_SterlingMin = 0;
        private ushort m_SterlingMax = 0;
        [CommandProperty(AccessLevel.Seer)]
        public ushort SterlingMin { get { return m_SterlingMin; } set { m_SterlingMin = value; } }
        [CommandProperty(AccessLevel.Seer)]
        public ushort SterlingMax { get { return m_SterlingMax; } set { m_SterlingMax = value; } }

        private ushort m_BossSterlingMin = 0;
        private ushort m_BossSterlingMax = 0;
        [CommandProperty(AccessLevel.Seer)]
        public ushort BossSterlingMin { get { return m_BossSterlingMin; } set { m_BossSterlingMin = value; } }
        [CommandProperty(AccessLevel.Seer)]
        public ushort BossSterlingMax { get { return m_BossSterlingMax; } set { m_BossSterlingMax = value; } }
        #endregion Sterling System
        #region FDBackup/Restore
        private ChampGFX m_RestoreGFX;
        public ChampGFX RestoreGFX
        {
            get { return m_RestoreGFX; }
            set { m_RestoreGFX = value; }
        }
        #endregion FDBackup/Restore

        #region Kill Monitoring

        Item m_TriggerLinkPlayers;
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Item TriggerLinkPlayers
        {
            get { return m_TriggerLinkPlayers; }
            set { m_TriggerLinkPlayers = value; }
        }

        Item m_TriggerLinkCreature;
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Item TriggerLinkCreatures
        {
            get { return m_TriggerLinkCreature; }
            set { m_TriggerLinkCreature = value; }
        }
        public virtual void SpawnedMobileKilled(Mobile killed)
        {
            #region Trigger System
            if (m_TriggerLinkPlayers != null)
            {
                if (TriggerSystem.CheckEvent(this))
                {

                    if (killed is BaseCreature bc)
                    {   // find legit damagers
                        List<DamageStore> list = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);
                        // divvy up points between party members
                        SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);

                        // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
                        //  We therefore limit the damage points to no more than the creature's HitsMax
                        BaseCreature.ClipDamageStore(ref Results, bc);

                        foreach (Mobile m in Results.Keys)
                            if (m is PlayerMobile pm)
                            {
                                if (TriggerSystem.CanTrigger(pm, m_TriggerLinkPlayers))
                                {
                                    if (pm.KillQueue == null)
                                        pm.KillQueue = new();

                                    if (!pm.KillQueue.Contains(bc))
                                        pm.KillQueue.Enqueue(bc);

                                    TriggerSystem.CheckTrigger(pm, m_TriggerLinkPlayers);
                                }
                            }
                    }
                }
            }

            // now for the creature
            if (m_TriggerLinkCreature != null)
                if (TriggerSystem.CheckEvent(this))
                    if (killed is BaseCreature bc)
                    {
                        // find legit damagers
                        List<DamageStore> list = BaseCreature.GetLootingRights(bc.DamageEntries, bc.HitsMax);
                        // divvy up points between party members
                        SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);

                        // anti-cheezing: Players have found that healing a monster (or allowing it to heal itself,) can yield unlimited damage points.
                        //  We therefore limit the damage points to no more than the creature's HitsMax
                        BaseCreature.ClipDamageStore(ref Results, bc);

                        foreach (Mobile m in Results.Keys)
                            if (m is PlayerMobile pm)
                            {
                                if (pm.KillQueue == null)
                                    pm.KillQueue = new();

                                if (!pm.KillQueue.Contains(bc))
                                    pm.KillQueue.Enqueue(bc);
                            }

                        TriggerSystem.CheckTrigger(bc, m_TriggerLinkCreature);
                    }

            #endregion Trigger System
        }

        #endregion Kill Monitoring
        #region ITriggerable
        bool ITriggerable.CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return true;
        }
        public void OnTrigger(Mobile from)
        {
            Running = true;
        }
        #endregion ITriggerable
        #region GFX Management
        public void SetBool(ChampGFX flag, bool value)
        {
            if (value)
                ChampGFX = (m_ChampGFX | flag);
            else
                ChampGFX = (m_ChampGFX & ~flag);
        }

        public bool GetBool(ChampGFX flag)
        {
            return ((m_ChampGFX & flag) != 0);
        }
        public bool GetBool(ChampGFX flags, ChampGFX flag)
        {
            return ((flags & flag) != 0);
        }
        #endregion GFX Management
        // Members
        #region members

        private ChampSliceTimer? m_Slice;                    // Slice timer  
        private ChampRestartTimer? m_Restart;                // Restart timer	
        private DateTime m_End;                             // Holds next respawn time
        protected string m_DefinitionName = string.Empty;   // Which champion definition this is ( cold blood etc ), Data/ChampSpawns.xml
        protected ArrayList m_Monsters;                     // Mobile container
        protected ArrayList m_FreeMonsters;                 // Mobile container for old spawn
        protected int m_LevelCounter;                       // Current level
        protected int m_MaxLevels;                          // total spawn levels per run (0 = all)
        protected int m_LevelsRun = 1;                      // spawn levels run this run, including the current one
        protected DateTime m_ExpireTime;                    // Level down datetime
        protected DateTime m_SpawnTime;                     // Respawn datetime
        protected TimeSpan m_RestartDelay;                  // Restart delay
        protected bool m_bRestart;                          // Restart timer on/off
        public ChampGraphics? m_Graphics;                 // Graphics object
        protected int m_Kills;                              // Kill counter
        protected string m_NavDest;                         // Allow mobs to navigate!		
        protected double m_LevelScale;                      // Virtually scales the maxmobs and maxrange
        public ArrayList SpawnLevels;                       // Contains all the level data objects
        protected ChampGFX m_ChampGFX;                      // Altar & Skulls on/off

        public enum LevelErrors
        {
            None,               // no error on this level
            No_Factory,         // no factory to spawn mob
            No_Location         // can't find a good spawn location
        };
        private LevelErrors m_LevelError = LevelErrors.None;

        #endregion

        #region Command Properties

        #region OwnerTools JumpList
        public ArrayList Monsters { get { return m_Monsters; } }
        public ArrayList FreeMonsters { get { return m_FreeMonsters; } }
        #endregion OwnerTools JumpList

        private bool m_KillDatabase;
        [CommandProperty(AccessLevel.Administrator)]
        public bool KillDatabase
        {
            get { return m_KillDatabase; }
            set
            {
                m_KillDatabase = value;
            }
        }

        private DebugFlags m_DebugMobile = DebugFlags.None;  // not serialized
        [CommandProperty(AccessLevel.GameMaster)]
        public DebugFlags DebugMobile
        {
            get { return m_DebugMobile; }
            set
            {
                m_DebugMobile = value;
                UpdateMobileDebug(value);
            }
        }
        private void UpdateMobileDebug(DebugFlags value)
        {
            if (m_Monsters != null)
            {
                foreach (BaseCreature bc in m_Monsters)
                    bc.DebugMode = value;
            }

            if (m_FreeMonsters != null)
            {
                foreach (BaseCreature bc in m_FreeMonsters)
                    bc.DebugMode = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        [StringList(typeof(ChampDefinitions), nameof(ChampDefinitions.Names))]  // [props: pick from the loaded definitions
        public string DefinitionName                // champion definition.  Changing this will also restart the champ if active.
        {
            get { return (m_DefinitionName); }
            set
            {
                ChampDefinition? definition = ChampDefinitions.Find(value);
                if (definition == null)
                {
                    ChampDefinitions.ReportUnknown(value, this);
                    return;
                }
                m_DefinitionName = definition.Name;
                StopSlice();                // stop slice timer and create new spawn type
                SpawnLevels = definition.CreateLevels();
                // begin
                if (Running)
                    StartSpawn();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual ChampGFX ChampGFX
        {
            get { return m_ChampGFX; }
            set
            {
                if (m_ChampGFX == value)
                    return;

                bool bAlter = GetBool(value, ChampGFX.Altar);
                bool bPlatform = GetBool(value, ChampGFX.Platform);
                m_ChampGFX = value;

                if (bAlter || bPlatform)
                {
                    // Switch gfx on
                    if (m_Graphics != null) m_Graphics.Delete();
                    m_Graphics = new ChampGraphics(this);
                    m_Graphics.UpdateLocation();
                }
                else
                {
                    // switch em off!. and delete. and stuff.
                    if (m_Graphics != null)
                        m_Graphics.Delete();
                    m_Graphics = null;
                }
            }
        }

        public List<Item> GetItems()
        {
            List<Item> list = new();

            if (m_Graphics != null)
                list.AddRange(m_Graphics.GetItems());

            return list;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running
        {
            get { return base.IsRunning; }
            set
            {
                if (value == base.IsRunning)
                    return;

                // set active bool and call overridable activate code
                base.IsRunning = value;
                Activate();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RestartTimer
        {
            get { return m_bRestart; }
            set { m_bRestart = value; }
        }

        //pla 12/28/06:
        //virtualised for ChampAngelIsland
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan RestartDelay
        {
            get { return m_RestartDelay; }
            set
            {
                m_RestartDelay = value;
                if (m_Restart != null)
                    if (m_Restart.Running)
                        DoTimer(value);
            }
        }
        private double m_dActiveSpeed;          // Timer speed when active
        private double m_dPassiveSpeed;     // Timer speed when not active
        [CommandProperty(AccessLevel.GameMaster)]
        public double ActiveSpeed
        {
            get { return m_dActiveSpeed; }
            set
            {
                if (m_dActiveSpeed != value)
                    m_dActiveSpeed = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double PassiveSpeed
        {
            get { return m_dPassiveSpeed; }
            set
            {
                if (m_dPassiveSpeed != value)
                    m_dPassiveSpeed = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Level
        {
            get { return m_LevelCounter; }
            set
            {
                if (value < SpawnLevels.Count)
                {
                    // set new level....
                    // wipe monsters first !
                    WipeMonsters();
                    m_Kills = 0;
                    LogLevels(string.Format("Level set {0} -> {1}", m_LevelCounter, value));
                    m_LevelCounter = value;
                    m_LevelsRun = value + 1;
                    // reset level down time
                    m_ExpireTime = DateTime.UtcNow + Lvl_ExpireDelay;
                }
            }
        }

        // Total number of spawn levels a run plays (0 = all). The boss level always runs last.
        //  Takes effect at the next level advance: levels already run count toward the total.
        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxLevels
        {
            get { return m_MaxLevels; }
            set
            {
                int old = m_MaxLevels;
                m_MaxLevels = Math.Max(0, value);
                if (old != m_MaxLevels)
                    LogLevels(string.Format("MaxLevels {0} -> {1} (level {2}, played {3})", old, m_MaxLevels, m_LevelCounter, m_LevelsRun));
            }
        }

        // level advancement / rollback history, Logs/ChampLevels.log
        protected void LogLevels(string text)
        {
            try
            {
                LogHelper logger = new LogHelper("ChampLevels.log", overwrite: false, sline: true, quiet: true);
                logger.Log(LogType.Text, string.Format("{0} 0x{1:X} {2}: {3}", m_DefinitionName, Serial.Value, Location, text));
                logger.Finish();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

        // the level that follows the current one: the remaining budget (MaxLevels - levels run) is spread evenly over the
        //  spawn levels still ahead, ending on the last spawn level; with no budget left, the boss level.
        protected int NextLevel()
        {
            int boss = SpawnLevels.Count - 1;
            int ahead = boss - 1 - m_LevelCounter;      // spawn levels after the current one
            if (m_MaxLevels <= 0 || ahead <= 0)
                return m_LevelCounter + 1;

            int remaining = m_MaxLevels - m_LevelsRun;
            if (remaining <= 0)
                return boss;
            if (remaining >= ahead)
                return m_LevelCounter + 1;

            return m_LevelCounter + Math.Max(1, (int)Math.Round((double)ahead / remaining, MidpointRounding.AwayFromZero));
        }
        private void NotifyStaff()
        {
            int hue = Utility.RandomSpecialHue(hash: m_LevelCounter);
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.GameMaster, hue, string.Format("[{0}] Champ {1} is on level {2}", "System", m_DefinitionName, m_LevelCounter));
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Kills
        {
            // squirrels
            get { return m_Kills; }
            set
            {
                if (value <= Lvl_MaxKills && value >= 0)
                    m_Kills = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (!Running && m_Restart != null)
                    return m_End - DateTime.UtcNow;
                else
                    return TimeSpan.FromSeconds(0);
            }
            set
            {
                if (Running)
                    DoTimer(value);
            }
        }

        /// <summary>
        /// Gets or sets the range scale.
        /// </summary>
        /// <value>The range scale.</value>
        [CommandProperty(AccessLevel.GameMaster)]
        public double LevelScale
        {
            get { return m_LevelScale; }
            set { m_LevelScale = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public LevelErrors Lvl_LevelError           // level error - Any errors while spawning creatures?
        {
            get { return m_LevelError; }
            set { m_LevelError = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lvl_MaxKills         // Max Kills - How many kills are needed to level up
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxKills + Convert.ToInt32(Math.Floor((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxKills * m_LevelScale))); }
            set
            {
                if (value >= 0)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxKills = value;
            }
        }
        private int m_Team = 0;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Team         // Team : Useful for having champs fight mobiles / other champs
        {
            get { return m_Team; }
            set
            {
                if (value != m_Team)
                    m_Team = value;
            }
        }

        // Slayer : Useful for arming champs with slayer weapons
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual SlayerName SlayerOffensive { get; set; } = SlayerName.None;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual DefensiveSlayer SlayersDefensive { get; set; } = DefensiveSlayer.None;

        /// <summary>
        /// If true, the ChampEngine will override this creature's slayer settings.
        /// If false (default), the creature's own slayer properties are respected.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool SlayersOverride { get; set; } = false;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lvl_MaxRange         // Max Range : Max distance mobs will spawn 
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxRange + Convert.ToInt32(Math.Floor((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxRange * m_LevelScale))); }
            set
            {
                if (value >= 0)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxRange = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lvl_MaxSpawn     // Max Spawn : amount of mobs that will span in one respawn()
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxSpawn + Convert.ToInt32(Math.Floor((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxSpawn * m_LevelScale))); }
            set
            {
                if (value >= 0)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxSpawn = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Lvl_MaxMobs          // Max Mobs : Amount of mobs allowed onscreen at once
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxMobs + Convert.ToInt32(Math.Floor((((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxMobs * m_LevelScale))); }
            set
            {
                if (value >= 0)
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_MaxMobs = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Lvl_SpawnDelay      // SpawnDelay :  Delay inbetween respawn()
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_SpawnDelay; }
            set
            {
                ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_SpawnDelay = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Lvl_ExpireDelay     // ExpireDelay :  Delay before level down ...
        {
            get { return ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_ExpireDelay; }
            set { ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_ExpireDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Lvl_Monsters      // Monster array!  have this as a comma separated list
        {
            get
            {
                string temp = "";
                foreach (string s in ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Monsters)
                    temp = temp + s + ",";

                temp = temp.Substring(0, temp.Length - 1);
                return temp;
            }
            set
            {
                // create new props!  (if this goes wrong exceptions are caught)
                if (value != null)
                {
                    string[] temp = value.Split(',');
                    ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Monsters = new string[temp.GetLength(0)];
                    for (int i = 0; i < temp.GetLength(0); ++i)
                        ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Monsters[i] = temp[i].Trim();
                }
            }

        }


        [CommandProperty(AccessLevel.Seer)]
        public string NavDestination
        {
            get { return m_NavDest; }
            set { m_NavDest = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFinalLevel
        {
            get { return (m_LevelCounter == SpawnLevels.Count - 1 ? true : false); }
        }

        #endregion Command Properties

        #region Constructors
        public ChampEngine()
            : base(0xBD2)
        {
            // default constructor
            // assign initial values..
            Visible = false;
            Movable = false;
            m_ExpireTime = DateTime.UtcNow;
            m_SpawnTime = DateTime.UtcNow;
            m_RestartDelay = TimeSpan.FromMinutes(5);
            m_Monsters = new ArrayList();
            m_FreeMonsters = new ArrayList();
            SpawnLevels = new ArrayList();
            m_bRestart = false;

            //load default spawn just so there's no nulls on the [props
            DefinitionName = ChampNames.Abyss;

            Instances.Add(this);
        }

        public ChampEngine(Serial serial)
            : base(serial)
        {
            Instances.Add(this);
        }
        #endregion Constructors

        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)19);           // version

            // version 19 - MaxLevels
            writer.Write(m_MaxLevels);
            writer.Write(m_LevelsRun);

            // version 18 - definition name, written in place of the version 0 SpawnTypes ordinal (below)

            // version 17 - Spawn exclusion rect
            writer.Write(HasSpawnExclusionRect);

            if (HasSpawnExclusionRect)
            {
                writer.Write(SpawnExcludeStart);
                writer.Write(SpawnExcludeEnd);
            }

            // version 16
            writer.Write(SlayersOverride);

            // version 15
            writer.Write((int)SlayersDefensive);

            // version 14
            writer.Write(m_HomeRange);

            // version 13
            writer.Write(m_ClearPath);

            // version 12
            writer.Write((byte)m_MorphMode);

            // version 11
            writer.Write(m_SterlingMin);
            writer.Write(m_SterlingMax);
            writer.Write(m_BossSterlingMin);
            writer.Write(m_BossSterlingMax);

            // version 10
            writer.Write((int)m_RestoreGFX);

            // version 9
            writer.Write(m_TriggerLinkPlayers);
            writer.Write(m_TriggerLinkCreature);
            writer.Write(m_dActiveSpeed);
            writer.Write(m_dPassiveSpeed);

            // version 8
            writer.Write(m_KillDatabase);

            // version 7
            writer.Write(m_NavDest);

            // version 5
            writer.Write((int)SlayerOffensive);  // errors for this level
            writer.Write(m_Team);  // errors for this level

            #region version 4 (ChampGFX)
            if ((GetBool(ChampGFX.Platform) || GetBool(ChampGFX.Altar)) && !m_Graphics.IsHealthy())
            {
                m_Graphics.Delete();
                m_Graphics = null;
                m_ChampGFX = ChampGFX.None;
            }
            writer.Write((int)m_ChampGFX);
            if (GetBool(ChampGFX.Platform) || GetBool(ChampGFX.Altar))
            {
                m_Graphics.Serialize(writer);
            }
            #endregion version 4 (ChampGFX)

            // version 3
            writer.Write((double)m_LevelScale);  // errors for this level

            // version 2
            writer.Write((int)m_LevelError);  // errors for this level

            // version 1
            writer.WriteDeltaTime(m_End);       //Next spawn time

            // first up is the spawn data       // version 0
            writer.Write(m_DefinitionName);     // version 18: the definition name (was the SpawnTypes ordinal)
            writer.Write((int)SpawnLevels.Count);
            foreach (ChampLevelData data in SpawnLevels)
                data.Serialize(writer);

            //now the monsters + misc data
            writer.WriteMobileList(m_Monsters);
            writer.WriteMobileList(m_FreeMonsters);
            writer.Write((int)m_LevelCounter);
            writer.Write((int)m_Kills);
            writer.Write((DateTime)m_ExpireTime);

            //  obsolete in version 7
            //writer.Write((int)m_NavDest);

            // the bools
            // obsoleted in version 6
            //writer.Write((bool)Running);

            writer.Write((bool)m_bRestart);
            writer.Write(m_RestartDelay);
            // And finally if the restart timer is currently on or not, and the delay value.
            writer.Write(m_Restart != null);

        }
        public override void Deserialize(GenericReader reader)
        {
            TimeSpan ts = TimeSpan.Zero;

            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 19:
                    {
                        m_MaxLevels = reader.ReadInt();
                        m_LevelsRun = reader.ReadInt();
                        goto case 18;
                    }
                case 18:
                    {   // the definition name replaces the SpawnTypes ordinal (read in case 0)
                        goto case 17;
                    }
                case 17:
                    {
                        bool hasRect = reader.ReadBool();

                        if (hasRect)
                        {
                            SpawnExcludeStart = reader.ReadPoint3D();
                            SpawnExcludeEnd = reader.ReadPoint3D();
                        }
                        goto case 16;
                    }
                case 16:
                    {
                        SlayersOverride = reader.ReadBool();
                        goto case 15;
                    }
                case 15:
                    {
                        SlayersDefensive = (DefensiveSlayer)reader.ReadInt();
                        goto case 14;
                    }
                case 14:
                    {
                        m_HomeRange = reader.ReadShort();
                        goto case 13;
                    }
                case 13:
                    {
                        m_ClearPath = reader.ReadBool();
                        goto case 12;
                    }
                case 12:
                    {
                        m_MorphMode = (MorphMode)reader.ReadByte();
                        goto case 11;
                    }
                case 11:
                    {
                        m_SterlingMin = reader.ReadUShort();
                        m_SterlingMax = reader.ReadUShort();
                        m_BossSterlingMin = reader.ReadUShort();
                        m_BossSterlingMax = reader.ReadUShort();
                        goto case 10;
                    }
                case 10:
                    {
                        m_RestoreGFX = (ChampGFX)reader.ReadInt();
                        goto case 9;
                    }
                case 9:
                    {
                        m_TriggerLinkPlayers = reader.ReadItem();
                        m_TriggerLinkCreature = reader.ReadItem();
                        m_dActiveSpeed = reader.ReadDouble();
                        m_dPassiveSpeed = reader.ReadDouble();
                        goto case 8;
                    }
                case 8:
                    {
                        m_KillDatabase = reader.ReadBool();
                        goto case 7;
                    }
                case 7:
                    {
                        m_NavDest = reader.ReadString();
                        goto case 6;
                    }
                case 6:
                    {
                        goto case 5;
                    }
                case 5:
                    {
                        SlayerOffensive = (SlayerName)reader.ReadInt();
                        m_Team = reader.ReadInt();
                        goto case 4;
                    }
                case 4:
                    {
                        m_ChampGFX = (ChampGFX)reader.ReadInt();

                        if (m_ChampGFX == ChampGFX.None && m_RestoreGFX != ChampGFX.None)
                        {   // during FDBackup, we need to turn off the graphics, but we set restore to be the state we want
                            //  when this champ engine is FDRestored.
                            m_ChampGFX = m_RestoreGFX;
                            m_RestoreGFX = ChampGFX.None;
                            m_Graphics = new ChampGraphics(this);
                        }
                        else if (m_ChampGFX != ChampGFX.None)
                        {
                            m_Graphics = new ChampGraphics(this, reader);
                        }
                        goto case 3;
                    }
                case 3:
                    {
                        m_LevelScale = reader.ReadDouble();
                        goto case 2;
                    }
                case 2:
                    {
                        m_LevelError = (LevelErrors)reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        ts = reader.ReadDeltaTime() - DateTime.UtcNow;
                        goto case 0;
                    }
                case 0:
                    {

                        // read it all back in									
                        if (version >= 18)
                            m_DefinitionName = reader.ReadString() ?? string.Empty;
                        else    // a SpawnTypes ordinal
                            m_DefinitionName = LegacyChampTypes.ToName(reader.ReadInt()) ?? string.Empty;

                        int a = reader.ReadInt();
                        SpawnLevels = new ArrayList();

                        // create new level array through deserialise constructors
                        for (int i = 0; i < a; ++i)
                            SpawnLevels.Add(new ChampLevelData(reader));

                        m_Monsters = reader.ReadMobileList();
                        m_FreeMonsters = reader.ReadMobileList();
                        m_LevelCounter = reader.ReadInt();
                        if (version < 19)
                            m_LevelsRun = m_LevelCounter + 1;
                        m_Kills = reader.ReadInt();
                        m_ExpireTime = reader.ReadDateTime();

                        if (version < 7)
                            /*m_NavDest = (NavDestinations)*/
                            reader.ReadInt();

                        // the bools
                        if (version < 6)
                            Running = reader.ReadBool();

                        if (version < 4)
                        {
                            bool bGraphics = reader.ReadBool();

                            // if graphics were on remake them through deserialise constructor
                            if (bGraphics)
                            {
                                m_ChampGFX = ChampGFX.Altar | ChampGFX.Platform;
                                m_Graphics = new ChampGraphics(this, reader);
                            }
                        }

                        // and the restart...
                        m_bRestart = reader.ReadBool();
                        m_RestartDelay = reader.ReadTimeSpan();

                        if (reader.ReadBool() && !Running && m_bRestart)
                        {
                            // in this case the champ is actively in restart mode, so create new timer
                            //pla: 13/01/07
                            //changed so we don't lose time on restart
                            if (ts == TimeSpan.Zero)
                                DoTimer(m_RestartDelay);
                            else
                                DoTimer(ts);
                        }
                        else if (Running)
                        {
                            // if spawn was active then start the wheels turning...
                            StartSlice();
                        }

                        break;
                    }

            }

            if (string.IsNullOrEmpty(m_DefinitionName))     // no definition (SpawnTypes.None): as before, Abyss
                DefinitionName = ChampNames.Abyss;

            Timer.DelayCall(UpdateGFXTick);
        }
        private void UpdateGFXTick()
        {
            // Update graphics
            if (m_Graphics != null)
                m_Graphics.UpdateLocation();
        }
        #endregion Serialization

        #region Methods
        // Big switch! This has the effect of restarting a spawn too.
        protected virtual void StartSpawn()
        {
            Running = true;
            m_Kills = 0;
            m_LevelCounter = 0;
            m_LevelsRun = 1;
            LogLevels(string.Format("start ({0} spawn levels + boss, MaxLevels {1})", SpawnLevels.Count - 1, m_MaxLevels));
            WipeMonsters();
            StartSlice();
        }

        // Slice timer on / off		
        protected void StartSlice()
        {
            if (Deleted)
                return;

            // if there's a restart timer on, kill it.
            if (m_Restart != null)
            {
                m_Restart.Stop();
                m_Restart = null;
            }

            if (m_Slice != null)
                m_Slice.Stop();

            m_Slice = new ChampSliceTimer(this);
            m_Slice.Start();

            // reset level expire delay
            m_ExpireTime = DateTime.UtcNow + Lvl_ExpireDelay;
        }

        protected void StopSlice()
        {
            if (Deleted)
                return;

            if (m_Slice != null)
            {
                m_Slice.Stop();
                m_Slice = null;
            }
        }

        // this is called from the Active prop 
        protected virtual void Activate()
        {
            // for base champ we just want to start spawn if active
            if (Running)
                StartSpawn();
        }

        // This needs to be public so the restart timer can call it!
        public virtual void Restart()
        {
            if (Deleted)
                return;

            m_Restart = null;
            StartSpawn();
        }

        // Core stuff, slice, advancelevel, respawn, expire

        //OnSlice needs to be public so the slice timer can access it
        public virtual void OnSlice()
        {
            ArrayList ClearList = new ArrayList();
            // this is the champ heartbeat. 
            if (!Running || Deleted)
                return;

            // couple of null checks just in case!
            if (m_Monsters == null)
                m_Monsters = new ArrayList();

            if (m_FreeMonsters == null)
                m_FreeMonsters = new ArrayList();

            if (m_LevelCounter <= SpawnLevels.Count)
            {
                // Now clear out any dead mobs from the mob list
                for (int i = 0; i < m_Monsters.Count; ++i)
                {
                    if (((Mobile)m_Monsters[i]).Deleted)
                    {
                        // increase kills !
                        ++m_Kills;
                        //add to clear list!
                        ClearList.Add(m_Monsters[i]);
                    }
                }
                // Now remove those guys from the original list
                for (int i = 0; i < ClearList.Count; i++)
                    m_Monsters.Remove(ClearList[i]);

                ClearList.Clear();

                // Now clear out any dead mobs from the free list, don't add these to the kill count though
                for (int i = 0; i < m_FreeMonsters.Count; ++i)
                    if (((Mobile)m_FreeMonsters[i]).Deleted)
                        ClearList.Add(m_FreeMonsters[i]);

                // Now remove those guys from the original list
                for (int i = 0; i < ClearList.Count; i++)
                    m_FreeMonsters.Remove(ClearList[i]);

                //calculate percentage killed against max for this level
                double n = m_Kills / (double)Lvl_MaxKills;
                int percentage = (int)(n * 100);

                // level up if > 90%
                if (percentage > 90)
                {
                    AdvanceLevel();
                }
                else
                {
                    // level down if the time's up!
                    if (DateTime.UtcNow >= m_ExpireTime)
                        Expire();

                    // Call spawn top-up function
                    Respawn();
                }

                // Update altar/skulls if they're on
                if (GetBool(ChampGFX.Altar))
                    if (m_Graphics != null)
                        m_Graphics.Update();

            }
        }

        #region ClearMonsters
        [CommandProperty(AccessLevel.GameMaster)]
        public bool ClearMonsters
        {
            get { return false; }
            set { if (value) WipeMonsters(); }
        }

        public void WipeMonsters()
        {
            // Delete all mosters, clear arraylists..
            if (m_Monsters != null)
            {
                foreach (Mobile m in m_Monsters)
                    m.Delete();

                m_Monsters.Clear();
            }

            if (m_FreeMonsters != null)
            {
                foreach (Mobile m in m_FreeMonsters)
                    m.Delete();

                m_FreeMonsters.Clear();
            }
        }
        #endregion  ClearMonsters

        public List<Mobile> GetMobiles()
        {
            List<Mobile> list = new();

            if (m_Monsters != null)
            {
                foreach (Mobile m in m_Monsters)
                    if (m != null && !m.Deleted)
                        list.Add(m);
            }

            if (m_FreeMonsters != null)
            {
                foreach (Mobile m in m_FreeMonsters)
                    if (m != null && !m.Deleted)
                        list.Add(m);

            }

            return list;
        }
        #region FreezMonsters
        [CommandProperty(AccessLevel.GameMaster)]
        public bool FreezeMonsters
        {
            get { return false; }
            set { FreezeThemMonsters(value); }
        }

        public void FreezeThemMonsters(bool freeze)
        {
            // (un)Freeze) all mosters
            if (m_Monsters != null)
            {
                foreach (Mobile m in m_Monsters)
                    m.CantWalkLand = freeze;
            }

            if (m_FreeMonsters != null)
            {
                foreach (Mobile m in m_FreeMonsters)
                    m.CantWalkLand = freeze;
            }
        }
        #endregion  FreezeMonsters
        protected bool CompareLevel(int offset)
        {

            try //just in case!
            {
                // create compare strings
                string current = "";
                foreach (string s in ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Monsters)
                    current = current + s;

                string target = "";
                foreach (string s in ((ChampLevelData)SpawnLevels[m_LevelCounter + offset]).m_Monsters)
                    target = target + s;

                //return true if the strings match
                //  we special-case "Doppelganger" and "PolymorphicBob" since all levels look exactly the same.
                if (current.ToLower() == target.ToLower() && target.ToLower() != "Doppelganger".ToLower() && target.ToLower() != "PolymorphicBob".ToLower())
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;  // must have been out of range
            }
        }

        protected virtual void AdvanceLevel()
        {
            int kills = m_Kills;
            m_Kills = 0;

            if (!IsFinalLevel)
            {
                int next = NextLevel();     // MaxLevels may skip levels
                LogLevels(string.Format("advance {0} -> {1} (kills {2}/{3}; played {4}, MaxLevels {5})",
                    m_LevelCounter, next, kills, Lvl_MaxKills, m_LevelsRun + 1, m_MaxLevels));

                // If the next level is different, transition population gradually (no visible poof)
                if (CompareLevel(next - m_LevelCounter) == false)
                {
                    // Move ALL current mobs into the free list so they remain tracked and can be retired cleanly.
                    // Do NOT clear m_FreeMonsters here; it is the retirement pool.
                    foreach (Mobile m in m_Monsters)
                    {
                        if (m != null && !m.Deleted)
                            m_FreeMonsters.Add(m);
                    }

                    m_Monsters.Clear();
                }

                m_LevelCounter = next;
                ++m_LevelsRun;

                NotifyStaff();

                m_ExpireTime = DateTime.UtcNow + Lvl_ExpireDelay;
                m_SpawnTime = DateTime.UtcNow;
            }
            else
            {
                LogLevels(string.Format("complete: boss level {0} cleared (played {1}, MaxLevels {2})", m_LevelCounter, m_LevelsRun, m_MaxLevels));
                StopSlice();
                m_LevelCounter = 0;
                Running = false;

                if (GetBool(ChampGFX.Altar))
                    m_Graphics.Update();

                if (m_bRestart)
                    DoTimer(m_RestartDelay);

                UpdateMonitors("The champ is dead.");
            }

            UpdateMonitors(string.Format("level:{0}", m_LevelCounter));
        }

        private bool HaveCaptain()
        {
            if (m_Monsters != null)
                foreach (object o in m_Monsters)
                    if (o is Mobile m && m.GetType() == GetCaptainType())
                        return true;
            return false;
        }
        protected virtual void Respawn()
        {
            if (!Running || Deleted)
                return;

            if (DateTime.UtcNow < m_SpawnTime)
                return;

            // Check to see if we can spawn more monsters				
            int amount_spawned = 0;

            while (m_Monsters.Count + m_FreeMonsters.Count < Lvl_MaxMobs && amount_spawned < Lvl_MaxSpawn)
            {
                Mobile m;
                Type captain = GetCaptainType();
                if (captain != null && !HaveCaptain())
                    m = Spawn(captain); // spawn the level captain
                else
                    m = Spawn();        // random spawn

                if (m == null)
                    return;

                // Increase vars and place into the big wide world!  (old code!)
                ++amount_spawned;
                m_Monsters.Add(m);
                m.MoveToWorld(GetSpawnLocation(m), Map);
                PrepMob(m);

                if (m is BaseCreature bc)
                    bc.DeactivateIfAppropriate();
            }
            m_SpawnTime = DateTime.UtcNow + Lvl_SpawnDelay;


            // if free list has monsters in it, we convert them one a second
            // preferably away from the players
            if (m_FreeMonsters.Count > 0)
            {
                int victimIndex = -1;
                bool random = false;

                // Try to find a victim that cannot be seen by a player
                for (int i = 0; i < m_FreeMonsters.Count; ++i)
                {
                    Mobile? cand = (Mobile?)m_FreeMonsters[i];
                    if (cand == null || cand.Deleted)
                    {
                        m_FreeMonsters.RemoveAt(i--);
                        continue;
                    }

                    bool seen = false;
                    IPooledEnumerable eable = cand.GetMobilesInRange(15);
                    foreach (Mobile t in eable)
                    {
                        if (t is PlayerMobile)
                        {
                            seen = true;
                            break;
                        }
                    }
                    eable.Free();

                    if (!seen)
                    {
                        victimIndex = i;
                        random = false;
                        break;
                    }
                }

                // If we could not find one out of sight, pick one at random
                if (victimIndex == -1)
                {
                    random = true;
                    victimIndex = Utility.Random(m_FreeMonsters.Count);
                }

                Mobile? victim = (Mobile?)m_FreeMonsters[victimIndex];
                if (victim == null || victim.Deleted)
                {
                    m_FreeMonsters.RemoveAt(victimIndex);
                    return;
                }

                // If we are over cap, retire (delete) old mobs without replacement until we are under cap.
                // This prevents "carry forward everything" from stalling the new level forever.
                if (m_Monsters.Count + m_FreeMonsters.Count > Lvl_MaxMobs)
                {
                    m_FreeMonsters.RemoveAt(victimIndex);
                    victim.Delete();
                    return;
                }

                // Otherwise, attempt a normal replacement (spawn new, then delete old)
                Mobile n = Spawn();
                if (n == null)
                {
                    // Spawn failed. Do NOT orphan the victim; leave it tracked and try again next slice.
                    return;
                }

                // Success: now we can remove the victim from tracking
                m_FreeMonsters.RemoveAt(victimIndex);
                m_Monsters.Add(n);

                bool sameType = (n.CanSwim == victim.CanSwim && n.CantWalkLand == victim.CantWalkLand);

                if (!random && sameType && victim.GetDistanceToSqrt(Location) <= Lvl_MaxRange)
                    n.MoveToWorld(victim.Location, Map);
                else
                    n.MoveToWorld(GetSpawnLocation(n), Map);

                victim.Delete();
                PrepMob(n);
            }
        }
        protected virtual void Expire()
        {
            // Level down time - you just can't get the players these days !
            if (m_LevelCounter < SpawnLevels.Count)
            {
                double f = ((double)Kills) / ((double)Lvl_MaxKills);
                if (f * 100 < 20)
                {
                    // They didn't even get 20% !!, go back a level.....
                    if (Level > 0)
                    {
                        int run = m_LevelsRun;
                        int from = m_LevelCounter;
                        // if previous level is the same as the current, just decrease level counter
                        if (CompareLevel(-1) == true)
                            --m_LevelCounter;
                        else    //otherwise wipe mobs when leveling down
                            --Level;
                        m_LevelsRun = Math.Max(1, run - 1);
                        LogLevels(string.Format("rollback {0} -> {1} (expired: kills {2}/{3} under 20%; played {4}, MaxLevels {5})",
                            from, m_LevelCounter, Kills, Lvl_MaxKills, m_LevelsRun, m_MaxLevels));
                    }
                    Kills = 0;
                    InvalidateProperties();
                }
                else
                {
                    Kills = 0;
                }
                m_ExpireTime = DateTime.UtcNow + Lvl_ExpireDelay;
            }
        }
        protected virtual void PrepMob(Mobile m)
        {
            BaseCreature? bc = m as BaseCreature;
            if (bc != null)
            {
                bc.Tamable = false;

                // If this spawn uses navigation, Home is intentionally cleared so
                // navigation logic can drive movement.
                //
                // Otherwise, use the creature's actual spawn location as Home.
                // Using the champ engine location causes mobs to leash back toward
                // the engine center and can create clumping around structures.
                bc.Home = !string.IsNullOrEmpty(m_NavDest)
                    ? new Point3D()
                    : bc.Location;

                // HomeRange is a runtime override for normal champ spawn behavior.
                // If unset, fall back to the level-defined max range.
                bc.RangeHome = m_HomeRange >= 1 ? m_HomeRange : Lvl_MaxRange;

                bc.NavDestination = m_NavDest;
                bc.Tamable = false;             // can be overridden in Configure
                bc.Configure(m_LevelCounter, SpawnLevels != null ? SpawnLevels.Count : 0);   // tell the creature at what level they should perform
                bc.Team = m_Team;

                // Apply Offensive Slayer from the ChampEngine
                if (bc.SlayerOffensive == SlayerName.None || this.SlayersOverride)
                    bc.SlayerOffensive = this.SlayerOffensive;      // All weapons will deliver a slayer blow

                // Apply Defensive Slayers from the ChampEngine
                if (bc.SlayersDefensive == DefensiveSlayer.None || this.SlayersOverride)
                    bc.SlayersDefensive = this.SlayersDefensive;    // Creature is vulnerable to this slayer group

                bc.DebugMode = m_DebugMobile;   // set the debug mode
                bc.PassiveSpeed = m_dPassiveSpeed == 0.0 ? bc.PassiveSpeed : m_dPassiveSpeed;
                bc.ActiveSpeed = m_dActiveSpeed == 0.0 ? bc.ActiveSpeed : m_dActiveSpeed;
                {   // custom name and/or title
                    string? name = null;
                    string? title = null;
                    if (GetNameAndTitle(ref name, ref title))
                    {
                        if (!string.IsNullOrEmpty(name))
                        {
                            bc.Name = name;
                            // since names and titles are paird, a null title is okay
                            bc.Title = title;
                        }
                    }
                }
                // add creature Sterling
                if (bc.IsChampion || IsFinalLevel)
                {
                    bc.SterlingMin = m_BossSterlingMin;
                    bc.SterlingMax = m_BossSterlingMax;
                }
                else
                {
                    bc.SterlingMin = m_SterlingMin;
                    bc.SterlingMax = m_SterlingMax;
                }
                if (m_MorphMode != MorphMode.Default)
                    if (m_MorphMode == MorphMode.Melee && bc.AIObject is BaseAI ai && ai is not MeleeAI)
                        MakeMelee(bc, ai, (bc.IsChampion || IsFinalLevel));

                //if we have a navdestination as soon as we spawn start on it
                if (!string.IsNullOrEmpty(bc.NavDestination))
                    bc.AIObject.Think();

                if (bc.IsChampion || IsFinalLevel)
                {
                    bc.IsBoss = true;                                                       // used by our controllers to know when the boss (sometimes a champ) has spawned
                    UpdateMonitors(string.Format("Champion spawned at {0}", m.Location));   // sends staff messages
                }

                bc.OnAfterSpawn();
            }
        }
        public static void MakeMelee(BaseCreature bc, BaseAI ai, bool boss)
        {
            if (ai is MageAI && ai is not HybridAI)
            {
                bc.DropHeldItems();

                // brigand skills
                bc.SetSkill(SkillName.Fencing, 60.0, 82.5);
                bc.SetSkill(SkillName.Macing, 60.0, 82.5);
                bc.SetSkill(SkillName.Swords, 60.0, 82.5);
                bc.SetSkill(SkillName.Poisoning, 60.0, 82.5);
                bc.SetSkill(SkillName.MagicResist, 57.5, 80.0);
                bc.SetSkill(SkillName.Tactics, 60.0, 82.5);

                bc.PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);

                // select weapon
                switch (Utility.Random(7))
                {
                    case 0: bc.AddItem(new Longsword()); break;
                    case 1: bc.AddItem(new Cutlass()); break;
                    case 2: bc.AddItem(new Broadsword()); break;
                    case 3: bc.AddItem(new Axe()); break;
                    case 4: bc.AddItem(new Club()); break;
                    case 5: bc.AddItem(new Dagger()); break;
                    case 6: bc.AddItem(new Spear()); break;
                }
            }
            else if (ai is HybridAI)
            {; }   // do nothing, simply setting the ai to melee will do the trick

            bc.AI = AIType.AI_Melee;
        }
        //  pla: 13/01/2007
        /// <summary>
        /// stops and restarts the restart timer with a new delay
        /// </summary>
        /// <param name="delay"></param>
        public void DoTimer(TimeSpan delay)
        {
            if (Running)  //cant have a restart if the champ is on
                return;

            m_End = DateTime.UtcNow + delay;

            if (m_Restart != null)
                m_Restart.Stop();

            m_Restart = new ChampRestartTimer(this, delay);
            m_Restart.Start();
        }
        protected Mobile? Factory(Type type)
        {
            Mobile? mx = null;
            // look for a factory that knows how to spawn one of these
            foreach (Item ix in Items)
            {
                if (ix is Spawner)
                    if ((mx = (ix as Spawner).CreateRaw(type.Name) as Mobile) != null)
                        break;
            }

            // Experimental, property-based, error reporting.
            //  cannot find a factory to manufacture this creature
            if (mx == null) Lvl_LevelError = LevelErrors.No_Factory;

            return mx;
        }
        private void DebugDump()
        {
            LogHelper logger = new LogHelper("champSpawner.log", false);
            try
            {
                logger.Log(LogType.Text, string.Format("this = {0}", this));
                logger.Log(LogType.Text, string.Format("m_LevelCounter = {0}", m_LevelCounter));
                logger.Log(LogType.Text, string.Format("SpawnLevels.Count = {0}", SpawnLevels.Count));
                logger.Log(LogType.Text, string.Format("((ChampLevelData)SpawnLevels [m_LevelCounter]).Monsters.Length = {0}", ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Monsters.Length));
                //logger.Log(LogType.Text, "X = {0}", X);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            finally
            {
                logger.Finish();
            }
        }

        #region Mob spawn functions - Extracted from old code
        protected Mobile Spawn()
        {
            return Spawn(((ChampLevelData)SpawnLevels[m_LevelCounter]).GetRandomType());
        }
        protected Type GetCaptainType()
        {
            return ((ChampLevelData)SpawnLevels[m_LevelCounter]).GetCaptainType();
        }
        protected bool GetNameAndTitle(ref string name, ref string title)
        {
            string[]? names = ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Names;
            string[]? titles = ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Titles;

            if (names != null && names.Length > 0)
            {
                System.Diagnostics.Debug.Assert(names.Length == titles.Length); // they're pairs
                int index = Utility.Random(names.Length);
                name = names[index];

                if (titles.Length > 0 && index < titles.Length)
                    title = titles[index];
            }

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(title))
                return false;
            else
                return true;
        }
        protected Mobile Spawn(Type type)
        {
            return Spawn(new Type[] { type });
        }
        protected Mobile? Spawn(params Type[] types)
        {
            try
            {
                {
                    Mobile? m = Activator.CreateInstance(types[Utility.Random(types.Length)]) as Mobile;
                    if (m is BaseCreature bc)
                    {
                        bc.SpawnerEngine = this;
                        bc.GuardIgnore = true;
                    }
                    return m;
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                DebugDump();
                return null;
            }
        }
        #endregion Mob spawn functions - Extracted from old code
        private short m_HomeRange = -1;
        [CommandProperty(AccessLevel.GameMaster)]
        public short HomeRange { get { return m_HomeRange; } set { m_HomeRange = value; } }

        #region Spawn Location
        protected virtual Point3D GetSpawnLocation(Mobile m)
        {
            // Determine spawn range.
            //
            // Final champ levels ALWAYS respect the level-defined range
            // (typically 0, forcing the champ to spawn dead-center).
            //
            // Normal spawn waves may optionally use the runtime HomeRange override.
            int range = IsFinalLevel
                ? Lvl_MaxRange
                : (m_HomeRange >= 1 ? m_HomeRange : Lvl_MaxRange);

            // Default spawn origin is the champ engine location.
            Point3D location = Location;

            // Start with the level's configured spawn flags.
            //
            // Examples: some champ types may request SpawnFar or other special behavior.
            SpawnFlags sflags = ((ChampLevelData)SpawnLevels[m_LevelCounter]).m_Flags;

            // General champ-spawn behavior.
            sflags |= SpawnFlags.AvoidPlayers;

            if (m_ClearPath)
                sflags |= SpawnFlags.ClearPath;

            // Use smoother distribution around the selected origin.
            sflags |= SpawnFlags.Concentric;

            // Platform final champ adjustment.
            //
            // When the final champ is configured to spawn at center,
            // lift it onto the platform surface.
            //
            // FYI: GetSurfaceTop does not work correctly here.
            if (IsFinalLevel && range < 3 && m_ChampGFX.HasFlag(ChampGFX.Platform))
                location.Z += 5;

            // Normal spawn-wave exclusion handling.
            //
            // Spawn exclusion rects are used to prevent normal wave mobs from spawning
            // inside structures, behind doors, or in otherwise bad interior areas.
            //
            // Final champ placement intentionally ignores this restriction so the boss
            // can still spawn at the designed center/platform location.
            if (!IsFinalLevel && HasSpawnExclusionRect)
            {
                Rectangle3D blockedRect = SpawnExclusionRect;
                const int maxAttempts = 100;

                for (int i = 0; i < maxAttempts; i++)
                {
                    IPoint3D candidate = Spawner.GetSpawnPosition(Map, location, range, sflags, m);
                    Point3D p = (Point3D)candidate;

                    if (!blockedRect.Contains(p))
                        return p;
                }

                ; // break here to determine if we are failing
            }

            // Final fallback:
            //
            // Either:
            // - this is the final champ,
            // - no exclusion rect is configured, or
            // - all retry attempts landed inside the blocked rect.
            //
            // Return the normal spawn result rather than failing spawn entirely.
            IPoint3D px = Spawner.GetSpawnPosition(Map, location, range, sflags, m);

            return (Point3D)px;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D SpawnExcludeStart { get; set; }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D SpawnExcludeEnd { get; set; }

        private bool HasSpawnExclusionRect
        {
            get { return SpawnExcludeStart != Point3D.Zero || SpawnExcludeEnd != Point3D.Zero; }
        }

        private Rectangle3D SpawnExclusionRect
        {
            get
            {
                int x1 = Math.Min(SpawnExcludeStart.X, SpawnExcludeEnd.X);
                int y1 = Math.Min(SpawnExcludeStart.Y, SpawnExcludeEnd.Y);
                int z1 = Math.Min(SpawnExcludeStart.Z, SpawnExcludeEnd.Z);

                int x2 = Math.Max(SpawnExcludeStart.X, SpawnExcludeEnd.X);
                int y2 = Math.Max(SpawnExcludeStart.Y, SpawnExcludeEnd.Y);
                int z2 = Math.Max(SpawnExcludeStart.Z, SpawnExcludeEnd.Z);

                return new Rectangle3D(
                    new Point3D(x1, y1, z1),
                    new Point3D(x2, y2, z2));
            }
        }
        #endregion spawn Location

        private bool m_ClearPath = false;
        [CommandProperty(AccessLevel.GameMaster)]
        public bool ClearPath { get { return m_ClearPath; } set { m_ClearPath = value; } }
        public override void OnLocationChange(Point3D oldLoc)
        {   // public overrides from Item
            base.OnLocationChange(oldLoc);

            if (Deleted || m_ChampGFX == ChampGFX.None)
                return;

            // Update graphics
            if (m_Graphics != null)
                m_Graphics.UpdateLocation();
        }
        #region Kill Level
        private Timer? m_KillLevelTimer;
        private Queue<Mobile>? m_KillQueue;
        [CommandProperty(AccessLevel.Administrator)]
        public bool KillLevel
        {
            get { return false; }
            set
            {
                if (!value)
                    return;

                StartKillLevelSweep();
            }
        }

        private void StartKillLevelSweep()
        {
            // Stop any existing sweep
            if (m_KillLevelTimer != null)
            {
                m_KillLevelTimer.Stop();
                m_KillLevelTimer = null;
            }

            // Build a unique list (Monsters + FreeMonsters)
            List<Mobile> list = new List<Mobile>();

            if (Monsters != null)
            {
                foreach (Mobile m in Monsters)
                {
                    if (m != null && !m.Deleted && !list.Contains(m))
                        list.Add(m);
                }
            }

            // NOTE:
            // We intentionally do NOT include FreeMonsters in KillLevel sweeps.
            //
            // FreeMonsters represents the transition/retirement pool between levels.
            // The champ engine is responsible for gradually converting or deleting
            // those mobs during Respawn() to avoid visible "poofing" on level changes.
            //
            // Including FreeMonsters here would short-circuit that lifecycle and can
            // mask bookkeeping bugs (e.g., leaked or orphaned mobs). For testing
            // level transition integrity, we only kill the active Monsters list.

            //if (FreeMonsters != null)
            //{
            //    foreach (Mobile m in FreeMonsters)
            //    {
            //        if (m != null && !m.Deleted && !list.Contains(m))
            //            list.Add(m);
            //    }
            //}

            m_KillQueue = new Queue<Mobile>(list);

            // If nothing to do, bail
            if (m_KillQueue.Count == 0)
                return;

            // Kill one per second until queue exhausted
            m_KillLevelTimer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), KillLevelTick);
        }

        private void KillLevelTick()
        {
            // Drain until we find one killable target, or we run out.
            while (m_KillQueue != null && m_KillQueue.Count > 0)
            {
                Mobile m = m_KillQueue.Dequeue();

                if (m == null || m.Deleted || !m.Alive)
                    continue;

                try
                {
                    m.Kill(); // exactly one kill per tick
                }
                catch
                {
                    // swallow; keep sweep alive
                }

                return; // IMPORTANT: one per second
            }

            // Nothing left to kill: stop timer
            if (m_KillLevelTimer != null)
            {
                m_KillLevelTimer.Stop();
                m_KillLevelTimer = null;
            }

            m_KillQueue = null;
        }
        #endregion Kill Level
        public override void OnMapChange()
        {
            base.OnMapChange();

            if (Deleted || m_ChampGFX == ChampGFX.None)
                return;

            // update graphics
            if (m_Graphics != null)
                m_Graphics.UpdateLocation();
        }
        public override void OnAfterDelete()
        {
            // cleanup			
            //Graphics = false;
            ChampGFX = ChampGFX.None;
            WipeMonsters();
            if (Instances.Contains(this))
                Instances.Remove(this);
            base.OnAfterDelete();
        }
        public override void OnSingleClick(Mobile from)
        {
            // display information about current spawn
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                if (Running)
                    LabelTo(from, "{0} (Active; Level: {1} / {2}; Kills: {3}/{4})", m_DefinitionName, Level, SpawnLevels.Count - 1, m_Kills, Lvl_MaxKills);
                else
                {
                    LabelTo(from, "{0} (Inactive; Levels : {1}) ", m_DefinitionName, SpawnLevels.Count - 1);
                    if (m_Restart != null)
                        if (m_Restart.Running)
                            LabelTo(from, "Restart timer active...!");
                }

            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                from.SendGump(new PropertiesGump(from, this));
            //if (from.AccessLevel >= AccessLevel.Seer)
            //from.SendGump(new ChampMobileFactoryGump(this));
        }
        #endregion Methods


        // Slice and Restart timer classes for the ChampEngine.
        private class ChampRestartTimer : Timer
        {
            private ChampEngine m_Spawn;

            public ChampRestartTimer(ChampEngine spawn, TimeSpan delay)
                : base(delay)
            {
                m_Spawn = spawn;
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                // call restart code
                m_Spawn.Restart();
            }
        }

        private class ChampSliceTimer : Timer
        {
            private ChampEngine m_Spawn;

            public ChampSliceTimer(ChampEngine spawn)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Spawn = spawn;
                // update spawn every second
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                // pump the pump
                m_Spawn.OnSlice();
            }
        }
    } // class
}