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

/* Scripts\Engines\Spawner\Spawner.Properties.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Diagnostics;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Mobiles.BaseCreature;
using static Server.Utility;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        private string m_Followers = null;

        /// <summary>
        /// Optional follower specification for mobiles spawned by this spawner.
        ///
        /// Syntax:
        ///     PackLlama
        ///         Spawn a single Pack Llama follower.
        ///
        ///     PackLlama|PackHorse|null
        ///         Choose one entry at random. In this example there is a 1/3 chance
        ///         of a Pack Llama, a 1/3 chance of a Pack Horse, and a 1/3 chance
        ///         of no follower.
        ///
        ///     PackLlama:PackHorse
        ///         Spawn all listed followers.
        ///
        ///     PackLlama:PackHorse|mongbat|null
        ///         Randomly choose one branch, then spawn all followers in that branch.
        ///
        /// Followers are spawned under the control of the primary mobile and will
        /// accompany it normally. If the primary mobile is killed, followers are
        /// released and become wild. If the primary mobile is deleted/despawned,
        /// followers are removed as part of cleanup.
        ///
        /// Only valid tamable BaseCreature types may be specified.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public string Followers
        {
            get => m_Followers;
            set
            {
                ValidateFollowerSpec(value);
                m_Followers = value;
            }
        }

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
        public ushort SterlingMax { get { return m_SterlingMax; } set { m_SterlingMax = value; SetFlag(SpawnerAttribs.DropsSterling, m_SterlingMax > 0); } }
        #endregion Sterling System

        [CommandProperty(AccessLevel.Administrator)]
        public bool AutoHome
        {
            get { return GetFlag(SpawnerAttribs.AutoHome); }
            set { SetFlag(SpawnerAttribs.AutoHome, value); }
        }
        // this spawner failed to spawn. Use [global to locate these spawners (not serialized)
        [CommandProperty(AccessLevel.Owner)]
        public bool Failed { get; set; } = false;
        [CommandProperty(AccessLevel.Owner)]
        public string? Failure { get; set; } = null;
        #region Variables
        public static int EdgeGravity = 1;  // see comments in RandomPointAdjust()
        private int m_Team;
        private int m_HomeRange;
        private int m_WalkRange = -1;                      // -1 indicates we will only use m_HomeRange
        private List<int> m_SlotCount;
        private TimeSpan m_MinDelay;
        private TimeSpan m_MaxDelay;
        private ArrayList m_ObjectNames = new ArrayList(0);
        private ArrayList m_Objects;
        private DateTime m_End;
        private InternalTimer m_Timer;
        private bool m_Group;
        private bool m_DynamicCopy;
        private bool m_Invulnerable = false;
        private WayPoint m_WayPoint;
        private Direction m_MobDirection;
        private string m_NavDest;
        private Mobile m_TemplateMobile;
        private Item m_TemplateItem;
        private Item m_LootPack;
        private Item m_CarvePack;
        private string m_CarveMessage;
        private bool m_CarveOverride;
        private DiceEntry m_GoldDice;
        private int m_GraphicID = 0;        // the spawner will us this alternative GraphicID (ItemID) if supplied.
        private string m_SetProp = null;    // set these properties on the spawned object
        private string m_SetSkill = null;   // set these skills on the spawned object
        private bool m_TemplateInternalize = true;
        private Item m_TriggerLinkPlayers;
        private Item m_TriggerLinkCreature;
        private int m_GoodiesRadius;
        private int m_GoodiesTotalMin;
        private int m_GoodiesTotalMax;
        #endregion
        private EquipmentDisposition m_EquipmentDisposition = EquipmentDisposition.None;
        [CommandProperty(AccessLevel.Seer)]
        public EquipmentDisposition MutateEquipment
        {
            get { return m_EquipmentDisposition; }
            set { m_EquipmentDisposition = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item TriggerLinkPlayers
        {
            get { return m_TriggerLinkPlayers; }
            set { m_TriggerLinkPlayers = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Item TriggerLinkCreatures
        {
            get { return m_TriggerLinkCreature; }
            set { m_TriggerLinkCreature = value; }
        }
        #region Goodies Props
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int GoodiesRadius
        {
            get { return m_GoodiesRadius; }
            set { m_GoodiesRadius = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int GoodiesTotalMin
        {
            get { return m_GoodiesTotalMin; }
            set
            {
                m_GoodiesTotalMin = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int GoodiesTotalMax
        {
            get { return m_GoodiesTotalMax; }
            set
            {
                m_GoodiesTotalMax = value;
            }
        }
        #endregion Goodies Props

        [CommandProperty(AccessLevel.Seer)]
        public virtual bool StaticCorpse
        {
            get { return GetFlag(SpawnerAttribs.StaticCorpse); }
            set
            {
                if (GetFlag(SpawnerAttribs.StaticCorpse) != value)
                {
                    SetFlag(SpawnerAttribs.StaticCorpse, value);
                }
            }
        }

        #region Spawn Metrics
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Administrator)]
        public double AvgPower
        {
            get
            {
                if (Objects == null || Objects.Count == 0)
                    return 0.0;

                double total = 0.0;
                int count = 0;

                foreach (object o in Objects)
                {
                    if (o is BaseCreature bc && !bc.Deleted)
                    {
                        total += bc.Power;
                        count++;
                    }
                }

                return count > 0 ? total / count : 0.0;
            }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Administrator)]
        public double TotalPower
        {
            get
            {
                if (Objects == null || Objects.Count == 0)
                    return 0.0;

                double total = 0.0;

                foreach (object o in Objects)
                {
                    if (o is BaseCreature bc && !bc.Deleted)
                        total += bc.Power;
                }

                return total;
            }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Administrator)]
        public double AvgDistanceToSpawner
        {
            get
            {
                if (Objects == null || Objects.Count == 0)
                    return 0.0;

                double total = 0.0;
                int count = 0;

                foreach (object o in Objects)
                {
                    if (o is BaseCreature bc && !bc.Deleted && bc.Map == Map)
                    {
                        total += Utility.GetDistanceToSqrt(bc.Location, Location);
                        count++;
                    }
                }

                return count > 0 ? total / count : 0.0;
            }
        }
        #endregion Spawn Metrics

        #region GraphicID
        [CommandProperty(AccessLevel.Seer)]
        public virtual int GraphicID { get { return m_GraphicID; } set { m_GraphicID = value; } }
        #endregion GraphicID

        [CommandProperty(AccessLevel.Seer)]
        public virtual string SetProp
        {
            get { return m_SetProp; }
            set
            {
                m_SetProp = value;
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public virtual string SetSkill
        {
            get { return m_SetSkill; }
            set
            {
                m_SetSkill = value;
            }
        }

        #region LootString
        //hold object for templated spawner use.
        private string m_LootString = null;
        [CommandProperty(AccessLevel.Seer)]
        public string LootString
        {
            get { return m_LootString; }
            set
            {
                if (m_LootString != value)
                {
                    if (value != null)
                    {
                        string reason = string.Empty;
                        try
                        {   // make sure it's valid at this level
                            Item result = SpawnEngine.Build<Item>(value, ref reason);
                            if (result == null)
                            {
                                SendMessageNearby(reason);
                                return;
                            }
                            else
                                result.Delete();
                        }
                        catch (Exception ex)
                        {   // don't think we can ever get here as SpawnEngine.Build handles all exceptions
                            SendMessageNearby(ex.Message);
                            return;
                        }
                    }

                    m_LootString = value;
                }
            }
        }
        #endregion LootString

        #region LootStringProps
        //hold object for templated spawner use.
        private string m_LootStringProps = null;
        [CommandProperty(AccessLevel.Seer)]
        public string LootStringProps
        {
            get { return m_LootStringProps; }
            set { m_LootStringProps = value; }
        }
        #endregion LootStringProps

        #region LootPack
        //hold object for templated spawner use.
        [CopyableAttribute(CopyType.DoNotRelink)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Item LootPack
        {
            get { return m_LootPack; }
            set
            {
                if (m_LootPack != value)
                {
                    if (m_LootPack != null)
                    {
                        m_LootPack.SpawnerTempRefCount--;
                        if (CanDeleteTemplate(m_LootPack))
                            m_LootPack.Delete();
                    }

                    m_LootPack = value;

                    if (m_LootPack != null)
                    {
                        if (m_TemplateInternalize)
                            m_LootPack.MoveToIntStorage(true);

                        m_LootPack.SpawnerTempRefCount++;
                    }
                }
            }
        }
        #endregion

        #region CarvePack
        //carve object for templated spawner use.
        [CopyableAttribute(CopyType.DoNotRelink)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Item CarvePack
        {
            get { return m_CarvePack; }
            set
            {
                if (m_CarvePack != value)
                {
                    if (m_CarvePack != null)
                    {
                        m_CarvePack.SpawnerTempRefCount--;
                        if (CanDeleteTemplate(m_CarvePack))
                            m_CarvePack.Delete();
                    }

                    m_CarvePack = value;

                    if (m_CarvePack != null)
                    {
                        if (m_TemplateInternalize)
                            m_CarvePack.MoveToIntStorage(true);

                        m_CarvePack.SpawnerTempRefCount++;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string CarveMessage
        {
            get { return m_CarveMessage; }
            set
            {
                m_CarveMessage = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CarveOverride
        {
            get { return m_CarveOverride; }
            set { m_CarveOverride = value; }
        }
        #endregion

        #region Artifacts
        private Item m_ArtifactPack;
        private int m_ArtifactCount;

        [CopyableAttribute(CopyType.DoNotRelink)]
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Item ArtifactPack
        {
            get { return m_ArtifactPack; }
            set
            {
                if (m_ArtifactPack != value)
                {
                    if (m_ArtifactPack != null)
                    {
                        m_ArtifactPack.SpawnerTempRefCount--;
                        if (CanDeleteTemplate(m_ArtifactPack))
                            m_ArtifactPack.Delete();
                    }

                    m_ArtifactPack = value;

                    if (m_ArtifactPack != null)
                    {
                        if (m_TemplateInternalize)
                            m_ArtifactPack.MoveToIntStorage(true);

                        m_ArtifactPack.SpawnerTempRefCount++;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int ArtifactCount
        {
            get { return m_ArtifactCount; }
            set
            {
                m_ArtifactCount = value;
            }
        }

        private void DistributeArtifacts(Mobile killed)
        {
            if (m_ArtifactPack == null || m_ArtifactCount <= 0)
                return;

            Dictionary<Mobile, int> dict = new Dictionary<Mobile, int>();
            int totalDamage = 0;

            foreach (DamageEntry de in killed.DamageEntries)
            {
                if (de.Damager == null || de.HasExpired)
                    continue;

                Mobile damager = de.Damager;
                int damage = de.DamageGiven;

                Mobile? master = damager.GetDamageMaster(killed);

                if (master != null)
                    damager = master;

                if (damage > 0 && damager.Player)
                {
                    if (dict.ContainsKey(damager))
                        dict[damager] += damage;
                    else
                        dict[damager] = damage;

                    totalDamage += damage;
                }
            }

            if (dict.Count == 0 || totalDamage <= 0)
                return;

            List<Mobile> eligible = new List<Mobile>();
            int curTotal = 0;
            List<Mobile> winners = new List<Mobile>();

            for (int i = 0; i < m_ArtifactCount; i++)
            {
                if (eligible.Count == 0)
                {
                    eligible.AddRange(dict.Keys);
                    curTotal = totalDamage;
                }

                int rnd = Utility.Random(curTotal);

                Mobile? winner = null;

                for (int j = 0; winner == null && j < eligible.Count; j++)
                {
                    Mobile m = eligible[j];

                    if (rnd < dict[m])
                        winner = m;
                    else
                        rnd -= dict[m];
                }

                if (winner != null)
                {
                    eligible.Remove(winner);
                    curTotal -= dict[winner];
                    winners.Add(winner);
                }
            }

            foreach (Mobile winner in winners)
            {
                Item[] artifacts = BaseCreature.GenerateTemplateLoot(m_ArtifactPack);

                if (artifacts.Length != 0)
                {
                    // select a random artifact and delete the others
                    int index = Utility.Random(artifacts.Length);

                    for (int i = 0; i < artifacts.Length; i++)
                    {
                        if (i != index)
                            artifacts[i].Delete();
                    }

                    Item artifact = artifacts[index];

                    winner.AddToBackpack(artifact);

                    winner.SendLocalizedMessage(1062317); // For your valor in combating the fallen beast, a special reward has been bestowed on you.

                    LogHelper logger = new LogHelper("SpawnerArtifacts.log", false, true);
                    logger.Log(LogType.Item, this, string.Format("{0} received an artifact: {1} ({2}).", winner, artifact, artifact.GetType().Name));
                    logger.Finish();
                }
            }
        }
        #endregion
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool GuardIgnore
        {
            get { return GetFlag(SpawnerAttribs.GuardIgnore); }
            set
            {
                SetFlag(SpawnerAttribs.GuardIgnore, value);
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Dummy
        {
            get { return GetFlag(SpawnerAttribs.Dummy); }
            set
            {
                SetFlag(SpawnerAttribs.Dummy, value);
                this.SendMessageNearby(string.Format("training dummy {0}", value.ToString()));
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public new virtual bool Debug
        {
            get { return GetFlag(SpawnerAttribs.Debug); }
            set
            {
                SetFlag(SpawnerAttribs.Debug, value);
                this.SendMessageNearby(string.Format("base.Debug set to {0}", value.ToString()));
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual DiceEntry GoldDice
        {
            get { return m_GoldDice; }
            set
            {
                m_GoldDice = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Invulnerable
        {
            get { return m_Invulnerable; }

            set
            {
                m_Invulnerable = value;
            }
        }
        private bool m_Exhibit = false;
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Exhibit
        {
            get { return m_Exhibit; }
            set
            {
                m_Exhibit = value;
            }
        }
        private UInt32 m_QuestCode;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual UInt32 QuestCodeValue
        {
            get { return m_QuestCode; }
            set
            {
                if (Engines.QuestCodes.QuestCodes.CheckQuestCode((ushort)value) || value == 0)
                {
                    m_QuestCode = value;
                }
                else
                {
                    this.SendMessageNearby("That quest code has not been allocated.");
                    this.SendMessageNearby("Use [QuestCodeAlloc to allocate a new quest code.");
                    throw new ApplicationException("That quest code has not been allocated.");
                }

            }
        }
        private double m_QuestCodeChance;
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double QuestCodeChance
        {
            get { return m_QuestCodeChance; }
            set
            {
                m_QuestCodeChance = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int ObjectCount
        {
            get { Defrag(); return (m_Objects == null ? 0 : m_Objects.Count); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsEmpty
        {
            get { return (ObjectCount == 0); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull
        {
            get { return (ObjectCount >= GetSlotCount(index: 0)); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual WayPoint WayPoint
        {
            get
            {
                return m_WayPoint;
            }
            set
            {
                m_WayPoint = value;
            }
        }

        public DebugFlags m_DebugMobile = DebugFlags.None;  // not serialized
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual DebugFlags DebugMobile
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
            foreach (object o in Objects)
                if (o is BaseCreature bc)
                    bc.DebugMode = value;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string NavDestination
        {
            get
            {
                return m_NavDest;
            }
            set
            {
                m_NavDest = value;
                UpdateMobileNav(value);
            }
        }
        private void UpdateMobileNav(string value)
        {
            foreach (object o in Objects)
                if (o is BaseCreature bc)
                    bc.NavDestination = value;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Direction MobileDirection
        {
            get
            {
                return m_MobDirection;
            }
            set
            {
                m_MobDirection = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Running
        {
            get { return base.IsRunning; }
            set
            {
                bool oldRunning = base.IsRunning;
                base.IsRunning = value;
                if (base.IsRunning)
                    Start();
                else
                    Stop();

                InvalidateProperties();
            }
        }

        /// <summary>
        /// HomeRange works differently for Concentric spawners.
        ///     Normally, HomeRange defines both how far the spawner can throw the creatures (items) AND the RangeHome of the creature (how far they can walk)
        ///     This model leads to clumping of creatures. 
        ///     Concentric spawners differ in that HomeRange defines ONLY how far the spawner can throw the creatures, but the creatures RangeHome is set
        ///     to a default value calculated by the 'circle' logic. Additionally, the creatures Home is set to the place where they were spawned.
        ///     This updating happens in OnAfterMobileSpawn(). The result is the creatures are scattered as per usual, but their landing spot becomes their new Home, 
        ///     and their RangeHome is restricted. 
        ///     The theory is that we should see far less clumping of creatures as their RangeHome will keep them near their spawn point.
        /// /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int HomeRange
        {
            get
            {
                return m_HomeRange;
            }
            set
            {
                m_HomeRange = value;
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int WalkRange
        {
            get { return m_WalkRange; }
            set
            {
                m_WalkRange = value;
            }
        }
        private static bool m_WalkRangeCalc = false;
        /// <summary>
        /// Calculates the maximum walking range for a mobile up to 128 tiles from the spawner.
        /// Useful for shopkeepers where you would like them to walk all around the shop without having to set each spawner manually
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool WalkRangeCalc
        {
            get { return m_WalkRangeCalc; }
            set
            {
                int oldWalkRange = m_WalkRange;
                if (value == true)
                {
                    int max = 128;                                                                  // give up at 128 tile radius from the spawner.
                    if (this.Map != null || this.Map != Map.Internal)
                    {
                        if (this.HomeRange > 0)
                        {
                            // can't set when in your backpack, another container, or an invalid map
                            Rectangle2D rect = new Rectangle2D(this.Location.X, this.Location.Y, 1, 1); // spawner sits atop a 1 tile rect
                            // blow up the rect to the max walkable area
                            rect = new Utility.SpawnableRect(rect).MaximumSpawnableRect(this.Map, 128, this.Map.GetAverageZ(rect.X, rect.Y));
                            m_WalkRangeCalc = value;                                                    // show this rect was auto sized
                            m_WalkRange = Math.Max(rect.Width, rect.Height);                            // set the walk range
                            if (rect.Width >= max || rect.Height >= max)                                // caution the user of the clipping
                            {
                                m_WalkRange = max;
                                this.SendMessageNearby("Maximum range reached.");
                            }
                        }
                        else
                        {
                            this.SendMessageNearby("Cannot calc walk range when HomeRange is zero.");
                        }
                    }
                    else
                    {
                        this.SendMessageNearby("This spawner must be placed on the ground before WalkRangeDynamic can be calculated.");
                        m_WalkRangeCalc = false;
                    }
                }
                else
                    m_WalkRangeCalc = false;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int Team
        {
            get { return m_Team; }
            set
            {
                m_Team = value;
                ApplyToCurrent(SpawnerAttribs.Team, m_Team);
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Robot
        {
            get { return GetFlag(SpawnerAttribs.Robot); }
            set
            {
                SetFlag(SpawnerAttribs.Robot, value);
                ApplyToCurrent(SpawnerAttribs.Robot, value);
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool BlockDamage
        {
            get { return GetFlag(SpawnerAttribs.BlockDamage); }
            set
            {
                SetFlag(SpawnerAttribs.BlockDamage, value);
                ApplyToCurrent(SpawnerAttribs.BlockDamage, value);
                InvalidateProperties();
                if (value)
                    SendMessageNearby("The creature will not take damage nor paralyze. It will also dispel fields.");
                else
                    SendMessageNearby("The creature will take normal damage.");
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool MissionCritical
        {
            get { return GetFlag(SpawnerAttribs.MissionCritical); }
            set
            {
                SetFlag(SpawnerAttribs.MissionCritical, value);
                ApplyToCurrent(SpawnerAttribs.MissionCritical, value);
                InvalidateProperties();
                SendMessageNearby("Setting MissionCritical + BlockDamage makes the creature virtually unstoppable.");
                SendMessageNearby("Hulk Smash!");
            }
        }

        private void ApplyToCurrent(SpawnerAttribs attrib, object value)
        {
            if (Objects != null)
                foreach (object o in Objects)
                    if (o is Mobile m)
                        switch (attrib)
                        {
                            case SpawnerAttribs.MissionCritical:
                                {
                                    m.SetMobileBool(Mobile.MobileBoolTable.MissionCritical, (bool)value);
                                    break;
                                }
                            case SpawnerAttribs.BlockDamage:
                                {
                                    m.SetMobileBool(Mobile.MobileBoolTable.BlockDamage, (bool)value);
                                    break;
                                }
                            case SpawnerAttribs.Robot:
                                {
                                    if (m is BaseCreature bc)
                                        bc.Robot = (bool)value;
                                    break;
                                }
                            case SpawnerAttribs.Team:
                                {
                                    if (m is BaseCreature bc)
                                        bc.Team = (int)value;
                                    break;
                                }
                        }

        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan MinDelay
        {
            get { return m_MinDelay; }
            set
            {
                if (value > m_MaxDelay)
                    m_MaxDelay = value * 2;

                m_MinDelay = value;
                ResetTimer();
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan MaxDelay
        {
            get { return m_MaxDelay; }
            set
            {
                m_MaxDelay = value;
                ResetTimer();
                InvalidateProperties();
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan NextSpawn
        {
            get
            {
                if (Running)
                    return m_End - DateTime.UtcNow < TimeSpan.Zero ? TimeSpan.Zero : m_End - DateTime.UtcNow;
                else
                    return TimeSpan.FromSeconds(0);
            }
            set
            {
                DoTimer(value);
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Group
        {
            get { return m_Group; }
            set
            {
                m_Group = value;
                InvalidateProperties();
            }
        }

        #region Schedule Respawn

        Timer? m_ScheduleRespawnTimer = null;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool ScheduleRespawn
        {
            get { return m_ScheduleRespawnTimer != null; }
            set
            {
                if (value)
                {
                    if (m_ScheduleRespawnTimer != null)
                        return;

                    // Random delay between 1s and 15s (in milliseconds)
                    int delayMs = Utility.RandomMinMax(1000, 15000);

                    m_ScheduleRespawnTimer = Timer.DelayCall(
                        TimeSpan.FromMilliseconds(delayMs),
                        ScheduleRespawnTick
                    );

                    InvalidateProperties();
                }
                else
                {
                    if (m_ScheduleRespawnTimer == null)
                        return;

                    m_ScheduleRespawnTimer.Stop();
                    m_ScheduleRespawnTimer = null;

                    InvalidateProperties();
                }
            }
        }

        private void ScheduleRespawnTick()
        {
            RemoveObjects();
            Respawn();

            m_ScheduleRespawnTimer = null;
        }

        #endregion Schedule Respawn
        #region Schedule Despawn

        Timer m_ScheduleDespawnTimer = null;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ScheduleDespawn
        {
            get { return m_ScheduleDespawnTimer != null; }
            set
            {
                if (value)
                {
                    if (m_ScheduleDespawnTimer != null)
                        return;

                    m_ScheduleDespawnTimer = Timer.DelayCall(
                        TimeSpan.FromSeconds(1),
                        new TimerStateCallback(ScheduleDespawnTick),
                        new object[] { });

                    InvalidateProperties();
                }
                else
                {
                    if (m_ScheduleDespawnTimer == null)
                        return;

                    m_ScheduleDespawnTimer.Stop();
                    m_ScheduleDespawnTimer.Flush();
                    m_ScheduleDespawnTimer = null;

                    InvalidateProperties();
                }
            }
        }

        private void ScheduleDespawnTick(object state)
        {
            Timer timer = m_ScheduleDespawnTimer;
            m_ScheduleDespawnTimer = null;

            if (timer != null)
            {
                timer.Stop();
                timer.Flush();
            }

            RemoveObjects();
        }

        #endregion Schedule Despawn

        private SpawnFlags m_SpawnerFlags = SpawnFlags.None;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
        public SpawnFlags SpawnerFlags
        {
            get { return m_SpawnerFlags; }
            set
            {
                SpawnFlags oldSpawnFlags = m_SpawnerFlags;
                if (Concentric)
                    m_SpawnerFlags = SpawnFlags.Concentric | value;
                else
                    m_SpawnerFlags = value;
            }
        }
    }
}
