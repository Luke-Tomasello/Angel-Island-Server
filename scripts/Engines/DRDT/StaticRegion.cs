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

/* Scripts/Engines/DRDT/StaticRegion.cs
 * CHANGELOG
 *  7/13/23, Yoar
 *      Added Season
 *  5/28/23, Yoar
 *      Added Town Ruleset
 *  4/15/23, Yoar
 *      Added support for new Alignment system.
 *  1/14/23, Yoar
 *      Added StaticIdx getter
 *  1/13/23, Adam (StaticRegionControl Controller())
 *      Check for null StaticRegion when enumerating Instances
 *  1/3/23, Yoar
 *      Added [ConvertToCustomRegion, [ConvertToStaticRegion commands
 *  1/1/23, Adam (LoadRegions)
 *      Because of a previous bug, Saves/StaticRegions exists, but is empty.
 *      Update LoadRegions to return the regions actually loaded. If zero, load the regions from Data\
 *  11/13/22, Adam (GetLogoutDelay)
 *      Siege: Murderers and criminals will not be able to kick/ban others from their home, nor will they be able to log out instantly
 *              https://www.uoguide.com/Siege_Perilous
 * 	9/23/22, Yoar
 * 		Added ShardConfig to enable/disable certain regions based on
 * 		shard configuration
 *  9/19/22, Yoar
 *      Added PatchCustomRegions
 *  9/19/22, Yoar
 *      Added StaticRegion.XmlDatabase
 *      Added StaticRegion.Registered to register/unregister the region
 *  9/15/22, Yoar
 *      Added XML serialization.
 *  9/14/22, Yoar
 *      Serves as a base class for CustomRegion. StaticRegion should be
 *      used for standard world-regioning as an alternative to RunUO's
 *      standard regions.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Spells;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace Server.Regions
{
    [Flags]
    public enum RegionFlag : ulong
    {
        None = 0x00000000,

        EnableHousing = 0x00000001,
        EnableStuckMenu = 0x00000002,
        ShowEnterMessage = 0x00000004,
        ShowExitMessage = 0x00000008,
        CannotEnter = 0x00000010,
        CanUsePotions = 0x00000020,
        EnableGuards = 0x00000040,
        NoMurderCounts = 0x00000080,
        CanRessurect = 0x00000100,
        IOBArea = 0x00000200,
        NoGateInto = 0x00000400,
        NoRecallInto = 0x00000800,
        ShowIOBMessage = 0x00001000,
        IsIsolated = 0x00002000, // wea: controls outside mobile visibility
        EnableMusic = 0x00004000,
        IsMagicIsolated = 0x00008000,
        RestrictCreatureMagic = 0x00010000,
        AllowTravelSpellsInRegion = 0x00020000,
        //OverrideMaxFollowers = 0x00040000,
        NoExternalHarmful = 0x00080000,
        NoGhostBlindness = 0x00100000, // adam: ghosts go blind in this region
        //UseHouseRules       = 0x00200000, // adam: implement some house region systems
        //UseDungeonRules     = 0x00400000, // adam: Convert IsDungeonRules to flag
        IsCaptureArea = 0x00800000, // plasma: factions sigil capture region
        BlockLooting = 0x01000000, // adam: disallow looting in this region
        //UseJailRules        = 0x02000000, // adam: implement jail region systems
        //UseGreenAcresRules  = 0x04000000, // adam: implement GreenAcres region systems
        //UseAngelIslandRules = 0x08000000, // adam: implement AngelIsland region systems
        EnableSmartGuards = 0x10000000, // adam: use smart guards against players bank griefing

        Default = EnableHousing | EnableStuckMenu | CanUsePotions |
            CanRessurect | EnableMusic,
    }

    public enum RegionRuleset : byte
    {
        Standard,
        Dungeon,
        House,
        Jail,
        GreenAcres,
        AngelIsland,
        Town,
    }

    public enum RegionVendorAccess : byte
    {
        UseDefault,
        AnyoneAccess,
        NobodyAccess,
    }

    public enum SeasonType : sbyte
    {
        Default = -1,

        Spring = 0,
        Summer,
        Fall,
        Winter,
        Desolation,
    }

    [PropertyObject]
    public class StaticRegion : GuardedRegion
    {
        private static readonly List<StaticRegion> m_XmlDatabase = new List<StaticRegion>();

        public static List<StaticRegion> XmlDatabase { get { return m_XmlDatabase; } }

        public static void Configure()
        {
            EventSink.WorldLoad += EventSink_OnWorldLoad;
            EventSink.WorldSave += EventSink_OnWorldSave;
        }

        public static new void Initialize()
        {
            EventSink.Login += new LoginEventHandler(EventSink_Login);

            CommandSystem.Register("ConvertToCustomRegion", AccessLevel.Administrator, ConvertToCustomRegion_OnCommand);
            CommandSystem.Register("ConvertToStaticRegion", AccessLevel.Administrator, ConvertToStaticRegion_OnCommand);
        }

        public static StaticRegion FindStaticRegion(Mobile m)
        {
            if (m == null || m.Deleted)
                return null;

            return FindStaticRegion(m.Location, m.Map);
        }

        public static StaticRegion FindStaticRegion(Item item)
        {
            if (item == null || item.Deleted)
                return null;

            return FindStaticRegion(item.GetWorldLocation(), item.Map);
        }

        public static StaticRegion FindStaticRegion(Point3D p, Map map)
        {
            return FindRegion<StaticRegion>(p, map);
        }

        protected static T FindRegion<T>(Point3D p, Map map) where T : Region
        {
            if (p == Point3D.Zero || map == null || map == Map.Internal)
                return null;

            foreach (Region region in map.GetSector(p).Regions)
            {
                if (region.Contains(p) && region is T)
                    return (T)region; // TODO: Return highest priority region?
            }

            return null;
        }
        public static StaticRegionControl Controller(StaticRegion reg)
        {
            for (int i = StaticRegionControl.Instances.Count - 1; i >= 0; i--)
            {
                StaticRegionControl rc = StaticRegionControl.Instances[i];

                if (rc.StaticRegion != null)
                    if (rc.StaticRegion.UId == reg.UId)
                        return rc;
            }

            return null;
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            StaticRegion sr = FindStaticRegion(e.Mobile);

            #region House Ruleset

            if (sr != null && sr.UseHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(e.Mobile);

                if (house != null && !house.Public && !house.IsFriend(e.Mobile))
                    e.Mobile.Location = house.BanLocation;
            }

            #endregion
        }

        public virtual bool IsDynamicRegion { get { return false; } } // dynamic regions are not written to XML file

        private RegionFlag m_Flags = RegionFlag.Default;
        private RegionRuleset m_Ruleset = RegionRuleset.Standard;

        private int m_LightLevel = -1;

        private HashSet<int> m_RestrictedSpells = new HashSet<int>();
        private HashSet<int> m_RestrictedSkills = new HashSet<int>();
        private HashSet<string> m_RestrictedItems = new HashSet<string>();
        private string m_RestrictedMagicMessage;

        private TimeSpan m_DefaultLogoutDelay = TimeSpan.FromMinutes(5.0);
        private TimeSpan m_InnLogoutDelay = TimeSpan.Zero;

        private IOBAlignment m_IOBAlignment;

        private int m_MaxFollowerSlots = -1;

        private RegionVendorAccess m_VendorAccess;

        private Spawner.ShardConfig m_ShardConfig;

        private AlignmentType m_GuildAlignment;

        private SeasonType m_Season = SeasonType.Default;

        [CommandProperty(AccessLevel.GameMaster)]
        public int StaticIdx
        {
            get { return m_XmlDatabase.IndexOf(this); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Registered
        {
            get { return Region.Regions.Contains(this); }
            set
            {
                if (Registered != value)
                {
                    if (value)
                        Region.AddRegion(this);
                    else
                        Region.RemoveRegion(this);

                    OnRegionRegistrationChanged();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public RegionFlag Flags
        {
            get { return m_Flags; }
            set { m_Flags = value; }
        }

        #region Flags

        public bool GetFlag(RegionFlag flag)
        {
            return ((m_Flags & flag) != 0);
        }

        public void SetFlag(RegionFlag flag, bool value)
        {
            if (value)
                m_Flags |= flag;
            else
                m_Flags &= ~flag;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool EnableHousing
        {
            get { return GetFlag(RegionFlag.EnableHousing); }
            set { SetFlag(RegionFlag.EnableHousing, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool EnableStuckMenu
        {
            get { return GetFlag(RegionFlag.EnableStuckMenu); }
            set { SetFlag(RegionFlag.EnableStuckMenu, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowEnterMessage
        {
            get { return GetFlag(RegionFlag.ShowEnterMessage); }
            set { SetFlag(RegionFlag.ShowEnterMessage, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowExitMessage
        {
            get { return GetFlag(RegionFlag.ShowExitMessage); }
            set { SetFlag(RegionFlag.ShowExitMessage, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CannotEnter
        {
            get { return GetFlag(RegionFlag.CannotEnter); }
            set { SetFlag(RegionFlag.CannotEnter, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanUsePotions
        {
            get { return GetFlag(RegionFlag.CanUsePotions); }
            set { SetFlag(RegionFlag.CanUsePotions, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool EnableGuards
        {
            get { return GetFlag(RegionFlag.EnableGuards); }
            set { SetFlag(RegionFlag.EnableGuards, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoMurderCounts
        {
            get { return GetFlag(RegionFlag.NoMurderCounts); }
            set { SetFlag(RegionFlag.NoMurderCounts, value); OnRegionRegistrationChanged(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanRessurect
        {
            get { return GetFlag(RegionFlag.CanRessurect); }
            set { SetFlag(RegionFlag.CanRessurect, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IOBZone
        {
            get { return GetFlag(RegionFlag.IOBArea); }
            set { SetFlag(RegionFlag.IOBArea, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoGateInto
        {
            get { return GetFlag(RegionFlag.NoGateInto); }
            set { SetFlag(RegionFlag.NoGateInto, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoRecallInto
        {
            get { return GetFlag(RegionFlag.NoRecallInto); }
            set { SetFlag(RegionFlag.NoRecallInto, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ShowIOBMessage
        {
            get { return GetFlag(RegionFlag.ShowIOBMessage); }
            set { SetFlag(RegionFlag.ShowIOBMessage, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsIsolated
        {
            get { return GetFlag(RegionFlag.IsIsolated); }
            set
            {
                if (IsIsolated != value)
                {
                    SetFlag(RegionFlag.IsIsolated, value);

                    // TODO: Update visibility
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool EnableMusic
        {
            get { return GetFlag(RegionFlag.EnableMusic); }
            set
            {
                if (EnableMusic != value)
                {
                    SetFlag(RegionFlag.EnableMusic, value);

                    foreach (Mobile m in Players.Values)
                    {
                        if (m.NetState != null)
                        {
                            if (value)
                                PlayMusic(m);
                            else
                                StopMusic(m);
                        }
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsMagicIsolated
        {
            get { return GetFlag(RegionFlag.IsMagicIsolated); }
            set { SetFlag(RegionFlag.IsMagicIsolated, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RestrictCreatureMagic
        {
            get { return GetFlag(RegionFlag.RestrictCreatureMagic); }
            set { SetFlag(RegionFlag.RestrictCreatureMagic, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AllowTravelSpellsInRegion
        {
            get { return GetFlag(RegionFlag.AllowTravelSpellsInRegion); }
            set { SetFlag(RegionFlag.AllowTravelSpellsInRegion, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool NoExternalHarmful
        {
            get { return GetFlag(RegionFlag.NoExternalHarmful); }
            set { SetFlag(RegionFlag.NoExternalHarmful, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool GhostBlindness
        {
            get { return !GetFlag(RegionFlag.NoGhostBlindness); }
            set { SetFlag(RegionFlag.NoGhostBlindness, !value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CaptureArea
        {
            get { return GetFlag(RegionFlag.IsCaptureArea); }
            set { SetFlag(RegionFlag.IsCaptureArea, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BlockLooting
        {
            get { return GetFlag(RegionFlag.BlockLooting); }
            set { SetFlag(RegionFlag.BlockLooting, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool EnableSmartGuards
        {
            get { return GetFlag(RegionFlag.EnableSmartGuards); }
            set { SetFlag(RegionFlag.EnableSmartGuards, value); }
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public RegionRuleset Ruleset
        {
            get { return m_Ruleset; }
            set { m_Ruleset = value; }
        }

        #region Rulesets

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseDungeonRules
        {
            get { return m_Ruleset == RegionRuleset.Dungeon; }
            set { m_Ruleset = RegionRuleset.Dungeon; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseHouseRules
        {
            get { return m_Ruleset == RegionRuleset.House; }
            set { m_Ruleset = RegionRuleset.House; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseJailRules
        {
            get { return m_Ruleset == RegionRuleset.Jail; }
            set { m_Ruleset = RegionRuleset.Jail; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseGreenAcresRules
        {
            get { return m_Ruleset == RegionRuleset.GreenAcres; }
            set { m_Ruleset = RegionRuleset.GreenAcres; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseAngelIslandRules
        {
            get { return m_Ruleset == RegionRuleset.AngelIsland; }
            set { m_Ruleset = RegionRuleset.AngelIsland; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UseTownRules
        {
            get { return m_Ruleset == RegionRuleset.Town; }
            set { m_Ruleset = RegionRuleset.Town; }
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public int LightLevel
        {
            get { return m_LightLevel; }
            set
            {
                if (m_LightLevel != value)
                {
                    m_LightLevel = value;

                    // TODO: Update light levels
                }
            }
        }

        public HashSet<int> RestrictedSpells
        {
            get { return m_RestrictedSpells; }
            set { m_RestrictedSpells = value; }
        }

        public HashSet<int> RestrictedSkills
        {
            get { return m_RestrictedSkills; }
            set { m_RestrictedSkills = value; }
        }

        public HashSet<string> RestrictedItems
        {
            get { return m_RestrictedItems; }
            set { m_RestrictedItems = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RestrictedMagicMessage
        {
            get { return m_RestrictedMagicMessage; }
            set { m_RestrictedMagicMessage = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public new TimeSpan DefaultLogoutDelay
        {
            get { return m_DefaultLogoutDelay; }
            set { m_DefaultLogoutDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public new TimeSpan InnLogoutDelay
        {
            get { return m_InnLogoutDelay; }
            set { m_InnLogoutDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public IOBAlignment IOBAlignment
        {
            get { return m_IOBAlignment; }
            set { m_IOBAlignment = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxFollowerSlots
        {
            get { return m_MaxFollowerSlots; }
            set { m_MaxFollowerSlots = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public RegionVendorAccess VendorAccess
        {
            get { return m_VendorAccess; }
            set { m_VendorAccess = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner.ShardConfig ShardConfig
        {
            get { return m_ShardConfig; }
            set { m_ShardConfig = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AlignmentType GuildAlignment
        {
            get { return m_GuildAlignment; }
            set { m_GuildAlignment = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SeasonType Season
        {
            get { return m_Season; }
            set
            {
                if (m_Season != value)
                {
                    m_Season = value;

                    int season = (m_Season == SeasonType.Default ? Map.Season : (int)m_Season);

                    Packet p = null;

                    foreach (Mobile m in Players.Values)
                    {
                        if (p == null)
                            p = Packet.Acquire(new SeasonChange(season));

                        m.Send(p);
                    }

                    Packet.Release(p);
                }
            }
        }

        public override bool IsGuarded { get { return EnableGuards; } set { EnableGuards = value; } }
        public override bool IsNoMurderZone { get { return NoMurderCounts; } }
        public override bool IsSmartGuards { get { return EnableSmartGuards; } set { EnableSmartGuards = value; } }
        public override bool IsDungeonRules { get { return UseDungeonRules; } }
        public override bool IsHouseRules { get { return UseHouseRules; } }
        public override bool IsJailRules { get { return UseJailRules; } }
        public override bool IsGreenAcresRules { get { return UseGreenAcresRules; } }
        public override bool IsAngelIslandRules { get { return UseAngelIslandRules; } }
        public override bool IsTownRules { get { return UseTownRules; } }

        private bool m_WasRegistered; // indicating whether the region was registered on serialization

        public bool WasRegistered
        {
            get { return m_WasRegistered; }
        }

        public StaticRegion(string prefix, string name, Map map, Type guardType)
            : base(prefix, name, map, guardType)
        {
            InitProps();
        }

        public StaticRegion()
            : base("", "Static Region", Map.Internal, typeof(WarriorGuard))
        {
            InitProps();
        }

        private void InitProps()
        {
            PriorityType = RegionPriorityType.High;
            LoadFromXml = false; // indicate that this region was not loaded from RunUO's "Data/Regions.xml" file
            m_ShardConfig = Spawner.ShardConfig.Core;
        }

        public virtual void OnRegionRegistrationChanged()
        {
            // TODO: Is there a more efficient way to do this?
            for (int i = StaticRegionControl.Instances.Count - 1; i >= 0; i--)
            {
                StaticRegionControl rc = StaticRegionControl.Instances[i];

                if (rc.StaticRegion == this)
                    rc.Update();
            }
        }

        public override bool AllowHousing(Point3D p)
        {
            if (IsJailRules || IsAngelIslandRules)
                return false;

            if (IsGreenAcresRules)
                return Server.Misc.TestCenter.Enabled; // we allow players to place houses on Green Acres on Test Center

            return EnableHousing;
        }

        public override bool CanUseStuckMenu(Mobile m, bool quiet = false)
        {
            if (IsAngelIslandRules || !EnableStuckMenu)
            {
                if (quiet == false)
                    m.SendMessage("You cannot use the stuck menu here.");
                return false;
            }

            return true;
        }

        public override bool AllowBenificial(Mobile from, Mobile target)
        {
            if (IsJailRules && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You may not do that in jail.");
                return false;
            }

            return base.AllowBenificial(from, target); ;
        }

        public override bool AllowHarmful(Mobile from, Mobile target)
        {
            if (IsJailRules && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You may not do that in jail.");
                return false;
            }

            return base.AllowHarmful(from, target);
        }

        public override bool OnResurrect(Mobile m)
        {
            if (!CanRessurect && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("You cannot ressurect here.");
                return false;
            }

            return true;
        }

        public override bool OnBeginSpellCast(Mobile from, ISpell s)
        {
            if (IsAngelIslandRules && (s is Spells.Third.TeleportSpell || s is Spells.Fourth.RecallSpell || s is Spells.Sixth.MarkSpell || s is Spells.Seventh.GateTravelSpell) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You cannot cast that spell here.");
                return false;
            }

            if (IsGreenAcresRules && (s is Spells.Fourth.RecallSpell || s is Spells.Sixth.MarkSpell || s is Spells.Seventh.GateTravelSpell) && from.AccessLevel == AccessLevel.Player)
            {
                from.SendMessage("You cannot cast that spell here.");
                return false;
            }

            if (IsJailRules && from.AccessLevel == AccessLevel.Player)
            {
                from.SendLocalizedMessage(502629); // You cannot cast spells here.
                return false;
            }

            if (IsRestrictedSpell(s) && (!(from is BaseCreature) || RestrictCreatureMagic) && from.AccessLevel == AccessLevel.Player)
            {
                if (RestrictedMagicMessage == null)
                    from.SendMessage("You cannot cast that spell here.");
                else
                    from.SendMessage(RestrictedMagicMessage);

                return false;
            }

            return base.OnBeginSpellCast(from, s);
        }

        public override bool OnSkillUse(Mobile m, int skill)
        {
            if (IsJailRules && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("You may not use skills in jail.");
                return false;
            }

            if (IsRestrictedSkill((SkillName)skill) && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("You cannot use that skill here.");
                return false;
            }

            return base.OnSkillUse(m, skill);
        }

        public override bool OnCombatantChange(Mobile from, Mobile Old, Mobile New)
        {
            if (IsJailRules && from.AccessLevel == AccessLevel.Player)
                return false;

            return base.OnCombatantChange(from, Old, New);
        }

        public static TimeSpan IOBMessageDelay = TimeSpan.FromMinutes(5.0);

        private DateTime m_IOBNextMessage;

        public override void OnEnter(Mobile m)
        {
            if (IsGreenAcresRules)
                return;

            PlayerMobile pm = m as PlayerMobile;

            Region lastRegionIn = null;

            if (pm != null)
                lastRegionIn = pm.LastRegionIn;

            #region Isolated Region

            // wea: If this is an isolated region, we're going to send sparklies where
            // mobiles will disappear (this happens as part of an IsIsolatedFrom() check,
            // not explicit packet removal here) + a sound effect if we had to do this
            // 
            // also send an incoming packet to all players within the region.

            if (IsIsolated)
            {
                int invissedmobiles = 0;

                foreach (Mobile otherMob in m.GetMobilesInRange(Core.GlobalMaxUpdateRange))
                {
                    // Regardless of whether this is mobile or playermobile,
                    // we need to send an incoming packet to each of the mobiles
                    // in the region

                    if (otherMob.Region == m.Region)
                    {
                        // We've just walked into this mobile's region, so send incoming packet
                        // if they're a playermobile

                        if (Utility.InUpdateRange(m.Location, otherMob.Location) && otherMob.CanSee(m))
                        {
                            // Send incoming packet to player if they're online
                            if (otherMob.NetState != null)
                                otherMob.NetState.Send(new MobileIncoming(otherMob, m));
                        }
                    }
                    else
                    {
                        // They're in a different region, so localise sparklies
                        // to us if we're a player mobile

                        if (otherMob.AccessLevel <= m.AccessLevel)
                        {
                            Packet particles = new LocationParticleEffect(EffectItem.Create(otherMob.Location, otherMob.Map, EffectItem.DefaultDuration), 0x376A, 10, 10, 0, 0, 2023, 0);

                            if (m.NetState != null)
                            {
                                m.Send(particles);
                                invissedmobiles++;
                            }
                        }
                    }
                }

                // Play a sound effect to go with it
                if (invissedmobiles > 0)
                {
                    if (m.NetState != null)
                        m.PlaySound(0x3C4);
                }

                if (m.NetState != null)
                {
                    m.ClearScreen();
                    m.SendEverything();
                }
            }

            #endregion

            // if were leaving a house and entering the region, don't play the enter msg
            if (m.Player)
                if (ShowEnterMessage && !(lastRegionIn is HouseRegion))
                {
                    m.SendMessage("You have entered {0}", this.Name);

                    if (NoMurderCounts)
                        m.SendMessage("This is a lawless area; you are freely attackable here.");
                }

            if (m_MaxFollowerSlots >= 0)
                m.FollowersMax = m_MaxFollowerSlots;

            #region IOB

            //if is a iob zone/region and a iob aligned mobile with a differnt alignment then the zone enters
            //find all players of the zones alignment and send them a message
            //plasma: refactored the send message code into its own method within KinSystem
            if (DateTime.UtcNow >= m_IOBNextMessage && IOBZone && ShowIOBMessage && pm != null && pm.IOBAlignment != IOBAlignment.None && pm.IOBAlignment != m_IOBAlignment && m.AccessLevel == AccessLevel.Player)
            {
                string regionName = this.Name;

                if (String.IsNullOrEmpty(regionName))
                    regionName = "your stronghold";

                IOBSystem.SendKinMessage(m_IOBAlignment, String.Format("Come quickly, the {0} are attacking {1}!", IOBSystem.GetIOBName(pm.IOBRealAlignment), regionName));

                m_IOBNextMessage = DateTime.UtcNow + IOBMessageDelay;
            }

            #endregion

            #region Alignment
            if (AlignmentSystem.Enabled)
                AlignmentSystem.OnEnterRegion(this, m);
            #endregion

            if (m_Season != SeasonType.Default)
                m.Send(new SeasonChange((int)m_Season, true));

            base.OnEnter(m);
        }

        public override void OnExit(Mobile m)
        {
            if (IsGreenAcresRules)
                return;

            #region Isolated Region

            // wea: If we're leaving an isolated region, we need
            // to send remove packets to all isolated playermobiles 
            // within

            if (IsIsolated)
            {
                // Send fresh state info if we're a player leaving an isolated region
                if (m.NetState != null)
                    m.SendEverything();

                int revealedmobiles = 0;

                foreach (Mobile otherMob in m.GetMobilesInRange(Core.GlobalMaxUpdateRange))
                {
                    if (m == otherMob)
                        continue;

                    // If they're in the region we're leaving, send a remove packet

                    if (otherMob.Region == this && !otherMob.CanSee(m)) // CanSee includes the isolation check
                    {
                        if (otherMob.NetState != null)
                            otherMob.Send(m.RemovePacket); // make us disappear to them
                    }

                    // They're going to become visible to us as a result of
                    // the SendEverything() call above, so as long as they're not us and
                    // are in our new region, send sparklies at their location 

                    // Also, send a sound at the end if there's at least one revealed

                    if (otherMob.Region != this && m.CanSee(otherMob) && m.AccessLevel == AccessLevel.Player)
                    {
                        // Create localized sparklies 
                        Packet particles = new LocationParticleEffect(EffectItem.Create(otherMob.Location, otherMob.Map, EffectItem.DefaultDuration), 0x376A, 10, 10, 0, 0, 2023, 0);

                        if (m.NetState != null)
                        {
                            m.Send(particles);
                            revealedmobiles++;
                        }
                    }
                }

                // If at least one was revealed, play a sound too
                if (revealedmobiles > 0)
                {
                    if (m.NetState != null)
                        m.PlaySound(0x3C4);
                }
            }

            #endregion

            //if were moving into a house dont play the exit msg
            if (m.Player)
                if (ShowExitMessage && !(m.Region is HouseRegion))
                    m.SendMessage("You have left {0}", this.Name);

            if (m_MaxFollowerSlots >= 0)
                m.FollowersMax = 5;

            #region Alignment
            if (AlignmentSystem.Enabled)
                AlignmentSystem.OnExitRegion(this, m);
            #endregion

            if (m_Season != SeasonType.Default)
                m.Send(new SeasonChange(Map.Season, true));

            base.OnExit(m);
        }

        public override bool SendInaccessibleMessage(Item item, Mobile from)
        {
            #region House Ruleset

            if (IsHouseRules)
            {
                if (item is Container)
                    item.SendLocalizedMessageTo(from, 501647); // That is secure.
                else
                    item.SendLocalizedMessageTo(from, 1061637); // You are not allowed to access this.

                return true;
            }

            #endregion

            return base.SendInaccessibleMessage(item, from);
        }

        public override bool CheckAccessibility(Item item, Mobile from)
        {
            #region House Ruleset

            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house != null)
                    return house.CheckAccessibility(item, from);
            }

            #endregion

            return base.CheckAccessibility(item, from);
        }

        private bool m_Recursion;

        public override void OnLocationChanged(Mobile m, Point3D oldLocation)
        {
            #region House Ruleset

            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(m);

                if (house != null)
                {
                    if (m_Recursion)
                        return;

                    m_Recursion = true;

                    if (m is BaseCreature && ((BaseCreature)m).NoHouseRestrictions)
                    {
                    }
                    else if (m is BaseCreature && ((BaseCreature)m).IsHouseSummonable && (BaseCreature.Summoning || house.IsInside(oldLocation, 16)))
                    {
                    }
                    else if ((house.Public || !house.IsAosRules) && house.IsBanned(m) && house.IsInside(m))
                    {
                        m.Location = house.BanLocation;
                        m.SendLocalizedMessage(501284); // You may not enter.
                    }
                    //Adam: no AOS rules here
                    /*else if ( house.IsAosRules && !house.Public && !house.HasAccess( m ) && house.IsInside( m ) )
					{
						m.Location = house.BanLocation;
						m.SendLocalizedMessage( 501284 ); // You may not enter.
					}*/
                    else if (house is HouseFoundation)
                    {
                        HouseFoundation foundation = (HouseFoundation)house;

                        if (foundation.Customizer != null && foundation.Customizer != m && house.IsInside(m) && m.AccessLevel < AccessLevel.GameMaster)
                            m.Location = house.BanLocation;
                    }

                    m_Recursion = false;
                }
            }

            #endregion
        }

        public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (IsAngelIslandRules && m is PlayerMobile && !((PlayerMobile)m).PrisonInmate && m.AccessLevel == AccessLevel.Player)
                return false;

            #region House Ruleset

            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(m);
                if (house != null)
                {
                    if (m is BaseCreature && ((BaseCreature)m).NoHouseRestrictions)
                    {
                    }
                    else if (m is BaseCreature && ((BaseCreature)m).IsHouseSummonable && (BaseCreature.Summoning || house.IsInside(oldLocation, 16)))
                    {
                    }
                    else if ((house.Public || !house.IsAosRules) && house.IsBanned(m) && house.IsInside(newLocation, 16))
                    {
                        m.Location = house.BanLocation;
                        m.SendLocalizedMessage(501284); // You may not enter.
                        return false;
                    }
                    //Adam: no AOS rules here
                    /*else if ( house.IsAosRules && !house.Public && !house.HasAccess( from ) && house.IsInside( newLocation, 16 ) )
					{
						from.SendLocalizedMessage( 501284 ); // You may not enter.
						return false;
					}*/
                    else if (house is HouseFoundation)
                    {
                        HouseFoundation foundation = (HouseFoundation)house;

                        if (foundation.Customizer != null && foundation.Customizer != m && house.IsInside(newLocation, 16) && m.AccessLevel < AccessLevel.GameMaster)
                            return false;
                    }
                }
            }

            #endregion

            if (CannotEnter && !Contains(oldLocation) && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("You cannot enter this area.");
                return false;
            }

            return true;
        }

        private static readonly Point3D m_AngelIslandExit = new Point3D(373, 870, 0);

        public override void OnMobileAdd(Mobile m)
        {
            base.OnMobileAdd(m);
            if (IsAngelIslandRules && m is PlayerMobile pm)
            {
                // Move to special Angel Island kick region
                if (!pm.PrisonInmate && !pm.PrisonVisitor && m.AccessLevel == AccessLevel.Player)
                    // kick them out
                    m.MoveToWorld(m_AngelIslandExit, m.Map);
            }
        }

        public override bool OnDecay(Item item)
        {
            #region House Ruleset

            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(item);

                if (house != null && (house.IsLockedDown(item) || house.IsSecure(item)) && house.IsInside(item))
                    return false;
            }

            #endregion

            return base.OnDecay(item);
        }

        public override TimeSpan GetLogoutDelay(Mobile m)
        {
            // Murderers and criminals will not be able to kick/ban others from their home, nor will they be able to log out instantly
            // https://www.uoguide.com/Siege_Perilous
            if (m.Criminal || m.IsMurderer && Core.RuleSets.SiegeStyleRules())
                return base.GetLogoutDelay(m);

            #region House Ruleset

            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(m);

                if (house != null)
                {
                    if ((house.IsFriend(m) && house.IsInside(m)) || TSInnKeeper.IsInsideTownshipInn(m, house))
                    {
                        for (int i = 0; i < m.Aggressed.Count; ++i)
                        {
                            AggressorInfo info = (AggressorInfo)m.Aggressed[i];

                            if (info.Defender.Player && (DateTime.UtcNow - info.LastCombatTime) < TimeSpan.FromSeconds(30.0))
                                return base.GetLogoutDelay(m);
                        }

                        return TimeSpan.Zero;
                    }

                    return base.GetLogoutDelay(m);
                }
            }

            #endregion

            if (m.AccessLevel >= AccessLevel.GameMaster)
                return TimeSpan.Zero;
            else if (m.Aggressors.Count == 0 && m.Aggressed.Count == 0 && IsInInn(m.Location))
                return m_InnLogoutDelay;
            else
                return m_DefaultLogoutDelay;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            #region House Ruleset

            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(e.Mobile);

                if (house != null)
                {
                    Mobile from = e.Mobile;

                    if (!from.Alive || !house.IsInside(from))
                        return;

                    bool isOwner = house.IsOwner(from);
                    bool isCoOwner = isOwner || house.IsCoOwner(from);
                    bool isFriend = isCoOwner || house.IsFriend(from);

                    if (!isFriend)
                        return;

                    if (e.HasKeyword(0x33)) // remove thyself
                    {
                        if (isFriend)
                        {
                            from.SendLocalizedMessage(501326); // Target the individual to eject from this house.
                            from.Target = new HouseKickTarget(house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.Speech.ToLower() == "i wish to make this decorative" && (Core.RuleSets.AngelIslandRules())) // i wish to make this decorative
                    {
                        if (!isFriend)
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                        else
                        {
                            from.SendMessage("Make what decorative?"); // 
                            from.Target = new HouseDecoTarget(true, house);
                        }
                    }
                    else if (e.Speech.ToLower() == "i wish to make this functional" && (Core.RuleSets.AngelIslandRules())) // i wish to make this functional
                    {
                        if (!isFriend)
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                        else
                        {
                            from.SendMessage("Make what functional?"); // 
                            from.Target = new HouseDecoTarget(false, house);
                        }
                    }
                    else if (e.HasKeyword(0x34)) // I ban thee
                    {
                        if (!isFriend)
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                        //Adam: no AOS rules here
                        /*else if ( !house.Public && house.IsAosRules )
						{
							from.SendLocalizedMessage( 1062521 ); // You cannot ban someone from a private house.  Revoke their access instead.
						}*/
                        else
                        {
                            from.SendLocalizedMessage(501325); // Target the individual to ban from this house.
                            from.Target = new HouseBanTarget(true, house);
                        }
                    }
                    else if (e.HasKeyword(0x23)) // I wish to lock this down
                    {
                        if (isCoOwner)
                        {
                            from.SendLocalizedMessage(502097); // Lock what down?
                            from.Target = new LockdownTarget(false, house);
                        }
                        else if (isFriend)
                        {
                            from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.HasKeyword(0x24)) // I wish to release this
                    {
                        if (isCoOwner)
                        {
                            from.SendLocalizedMessage(502100); // Choose the item you wish to release
                            from.Target = new LockdownTarget(true, house);
                        }
                        else if (isFriend)
                        {
                            from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.HasKeyword(0x25)) // I wish to secure this
                    {
                        if (isCoOwner)
                        {
                            from.SendLocalizedMessage(502103); // Choose the item you wish to secure
                            from.Target = new SecureTarget(false, house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.HasKeyword(0x26)) // I wish to unsecure this
                    {
                        if (isOwner)
                        {
                            from.SendLocalizedMessage(502106); // Choose the item you wish to unsecure
                            from.Target = new SecureTarget(true, house);
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    else if (e.HasKeyword(0x27)) // I wish to place a strong box
                    {
                        if (isOwner)
                        {
                            from.SendLocalizedMessage(502109); // Owners do not get a strongbox of their own.
                        }
                        else if (isFriend)
                        {
                            house.AddStrongBox(from);
                        }
                        else if (isFriend)
                        {
                            from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                    /* weaver: disallowed trash barrel placement by command
					 * 
					else if ( e.HasKeyword( 0x28 ) ) // I wish to place a trash barrel
					{
						if ( isCoOwner )
						{
							house.AddTrashBarrel( from );
						}
						else if ( isFriend )
						{
							from.SendLocalizedMessage( 1010587 ); // You are not a co-owner of this house.
						}
						else
						{
							from.SendLocalizedMessage( 502094 ); // You must be in your house to do this.
						}
					}*/
                    else if (e.Speech.ToLower() == "i wish to place my guild stone") // I wish to place a guild stone
                    {
                        if (isCoOwner)
                        {
                            // ask the playermobile to deal with this item request
                            Item item = from.RequestItem(typeof(Server.Items.Guildstone));
                            if (item == null)
                                from.SendMessage("You do not seem to have one of those.");
                            else
                            {   // ask the player mobile to place this guild stone
                                from.ProcessItem(item);
                            }
                        }
                        else if (isFriend)
                        {
                            from.SendLocalizedMessage(1010587); // You are not a co-owner of this house.
                        }
                        else
                        {
                            from.SendLocalizedMessage(502094); // You must be in your house to do this.
                        }
                    }
                }
            }

            #endregion
        }

        public override bool EquipItem(Mobile m, Item item)
        {
            if (IsRestrictedItem(item) && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("You cannot equip that item here.");
                return false;
            }

            return base.EquipItem(m, item);
        }

        public override bool OnDoubleClick(Mobile m, object o)
        {
            if (o is BasePotion && !CanUsePotions && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("You cannot drink potions here.");
                return false;
            }

            if (IsRestrictedItem(o.GetType()) && m.AccessLevel == AccessLevel.Player)
            {
                m.SendMessage("You cannot use that item here.");
                return false;
            }

            #region House Ruleset

            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(m);

                if (house != null)
                {
                    if (o is Container)
                    {
                        Container c = (Container)o;

                        SecureAccessResult res = house.CheckSecureAccess(m, c);

                        switch (res)
                        {
                            case SecureAccessResult.Insecure: break;
                            case SecureAccessResult.Accessible: return true;
                            case SecureAccessResult.Inaccessible: c.SendLocalizedMessageTo(m, 1010563); return false;
                        }
                    }
                }
            }

            #endregion

            return base.OnDoubleClick(m, o);
        }

        public override bool OnSingleClick(Mobile from, object o)
        {
            #region House Ruleset

            if (IsHouseRules)
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house != null)
                {
                    if (o is Item)
                    {
                        Item item = (Item)o;

                        if (house.IsLockedDown(item))
                            item.LabelTo(from, 501643); // [locked down]
                        else if (house.IsSecure(item))
                            item.LabelTo(from, 501644); // [locked down & secure]
                    }

                    return true;
                }
            }

            #endregion

            return base.OnSingleClick(from, o);
        }

        public override bool OnDeath(Mobile m)
        {
            if (NoMurderCounts)
            {
                foreach (AggressorInfo ai in m.Aggressors)
                    ai.CanReportMurder = false;
            }

            return base.OnDeath(m);
        }

        public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
            if (m_LightLevel >= 0)
            {
                global = m_LightLevel;
                return;
            }
            else if (IsJailRules)
            {
                global = LightCycle.JailLevel;
                return;
            }
            else if (IsDungeonRules)
            {
                global = LightCycle.DungeonLevel;
                return;
            }

            base.AlterLightLevel(m, ref global, ref personal);
        }

        public override bool CheckVendorAccess(BaseVendor vendor, Mobile from)
        {
            switch (m_VendorAccess)
            {
                default:
                case RegionVendorAccess.UseDefault: return base.CheckVendorAccess(vendor, from);
                case RegionVendorAccess.AnyoneAccess: return true;
                case RegionVendorAccess.NobodyAccess: return false;
            }
        }

        #region [Is/Set]Restricted[...]

        public bool IsRestrictedSpell(ISpell s)
        {
            return IsRestrictedSpell(s.GetType());
        }

        public bool IsRestrictedSpell(Type spellType)
        {
            return IsRestrictedSpell(Array.IndexOf(SpellRegistry.Types, spellType));
        }

        public bool IsRestrictedSpell(int spellID)
        {
            return m_RestrictedSpells.Contains(spellID);
        }

        public void SetRestrictedSpell(int spellID, bool value)
        {
            if (value)
                m_RestrictedSpells.Add(spellID);
            else
                m_RestrictedSpells.Remove(spellID);
        }

        public bool IsRestrictedSkill(SkillName skill)
        {
            return IsRestrictedSkill((int)skill);
        }

        public bool IsRestrictedSkill(int skillID)
        {
            return m_RestrictedSkills.Contains(skillID);
        }

        public void SetRestrictedSkill(int skill, bool value)
        {
            if (value)
                m_RestrictedSkills.Add(skill);
            else
                m_RestrictedSkills.Remove(skill);
        }

        public bool IsRestrictedItem(Item item)
        {
            return IsRestrictedItem(item.GetType());
        }

        public bool IsRestrictedItem(Type itemType)
        {
            return m_RestrictedItems.Contains(itemType.Name);
        }

        #endregion

        #region Serialization (Binary)

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)5); // version

            writer.Write((sbyte)m_Season);

            writer.Write((byte)m_GuildAlignment);

            writer.Write((uint)m_ShardConfig);

            writer.Write((bool)this.Registered);

            writer.Write((ulong)m_Flags);
            writer.Write((byte)m_Ruleset);

            writer.Write((int)m_LightLevel);

            writer.Write((int)m_RestrictedSpells.Count);

            foreach (int spellID in m_RestrictedSpells)
                writer.Write((int)spellID);

            writer.Write((int)m_RestrictedSkills.Count);

            foreach (int skillID in m_RestrictedSkills)
                writer.Write((int)skillID);

            writer.Write((int)m_RestrictedItems.Count);

            foreach (string typeName in m_RestrictedItems)
                writer.Write((string)typeName);

            writer.Write((string)m_RestrictedMagicMessage);

            writer.Write((TimeSpan)m_DefaultLogoutDelay);
            writer.Write((TimeSpan)m_InnLogoutDelay);

            writer.Write((int)m_IOBAlignment);

            writer.Write((int)m_MaxFollowerSlots);

            writer.Write((byte)m_VendorAccess);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 5:
                    {
                        m_Season = (SeasonType)reader.ReadSByte();
                        goto case 4;
                    }
                case 4:
                    {
                        m_GuildAlignment = (AlignmentType)reader.ReadByte();
                        goto case 3;
                    }
                case 3:
                    {
                        m_ShardConfig = (Spawner.ShardConfig)reader.ReadUInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_WasRegistered = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                case 0:
                    {
                        m_Flags = (RegionFlag)reader.ReadULong();
                        m_Ruleset = (RegionRuleset)reader.ReadByte();

                        m_LightLevel = reader.ReadInt();

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                            m_RestrictedSpells.Add(reader.ReadInt());

                        count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                            m_RestrictedSkills.Add(reader.ReadInt());

                        count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                            m_RestrictedItems.Add(reader.ReadString());

                        m_RestrictedMagicMessage = reader.ReadString();

                        m_DefaultLogoutDelay = reader.ReadTimeSpan();
                        m_InnLogoutDelay = reader.ReadTimeSpan();

                        m_IOBAlignment = (IOBAlignment)reader.ReadInt();

                        m_MaxFollowerSlots = reader.ReadInt();

                        m_VendorAccess = (RegionVendorAccess)reader.ReadByte();

                        break;
                    }
            }

            if (version < 1)
            {
                int iflags = (int)m_Flags;

                if ((iflags & 0x00040000) != 0)
                    iflags &= ~0x00040000;
                else
                    m_MaxFollowerSlots = -1;

                m_Flags = (RegionFlag)iflags;
            }

            if (version < 2)
                m_WasRegistered = true;
        }

        #endregion

        #region Serialization (XML)

        public virtual void Save(XmlElement xml)
        {
            Write(xml, "map", Map.Name);
            Write(xml, "priority", Priority);

            if (!String.IsNullOrEmpty(Prefix))
                Write(xml, "prefix", Prefix);

            Write(xml, "name", Name);

            if (m_ShardConfig != Spawner.ShardConfig.None)
                Write(xml, "shard", m_ShardConfig);

            if (!Registered)
                Write(xml, "registered", false);

            foreach (Rectangle3D rect in Coords)
            {
                XmlElement xmlRect = xml.OwnerDocument.CreateElement("rect");
                WriteRectangle3D(xmlRect, rect);
                xml.AppendChild(xmlRect);
            }

            foreach (Rectangle3D rect in InnBounds)
            {
                XmlElement xmlInnRect = xml.OwnerDocument.CreateElement("innrect");
                WriteRectangle3D(xmlInnRect, rect);
                xml.AppendChild(xmlInnRect);
            }

            if (Music != MusicName.Invalid)
            {
                XmlElement music = xml.OwnerDocument.CreateElement("music");
                Write(music, "name", Music);
                xml.AppendChild(music);
            }

            XmlElement go = xml.OwnerDocument.CreateElement("go");
            WritePoint3D(go, GoLocation);
            xml.AppendChild(go);

            ulong curBit = 0x1;
            for (int i = 0; i < 64; i++, curBit <<= 1)
            {
                if (((ulong)m_Flags & curBit) != 0)
                {
                    XmlElement flag = xml.OwnerDocument.CreateElement("flag");
                    Write(flag, "name", (RegionFlag)curBit);
                    xml.AppendChild(flag);
                }
            }

            if (m_Ruleset != RegionRuleset.Standard)
                Write(xml, "ruleset", m_Ruleset);

            if (m_LightLevel != -1)
                Write(xml, "lightlevel", m_LightLevel);

            foreach (int spellID in m_RestrictedSpells)
            {
                Type spellType = null;

                if (spellID >= 0 && spellID < SpellRegistry.Types.Length)
                    spellType = SpellRegistry.Types[spellID];

                XmlElement restrictedSpell = xml.OwnerDocument.CreateElement("restrictedspell");
                if (spellType != null)
                    Write(restrictedSpell, "name", spellType.Name);
                else
                    Write(restrictedSpell, "id", spellID);
                xml.AppendChild(restrictedSpell);
            }

            foreach (int skillID in m_RestrictedSkills)
            {
                XmlElement restrictedSkill = xml.OwnerDocument.CreateElement("restrictedskill");
                if (skillID >= 0 && skillID < SkillInfo.Table.Length)
                    Write(restrictedSkill, "name", (SkillName)skillID);
                else
                    Write(restrictedSkill, "id", skillID);
                xml.AppendChild(restrictedSkill);
            }

            foreach (string itemTypeName in m_RestrictedItems)
            {
                XmlElement restrictedItem = xml.OwnerDocument.CreateElement("restricteditem");
                Write(restrictedItem, "name", itemTypeName);
                xml.AppendChild(restrictedItem);
            }

            if (m_RestrictedMagicMessage != null)
                Write(xml, "restrictedmagicmessage", m_RestrictedMagicMessage);

            if (m_DefaultLogoutDelay != TimeSpan.FromMinutes(5.0))
                Write(xml, "defaultlogoutdelay", m_DefaultLogoutDelay);

            if (m_InnLogoutDelay != TimeSpan.Zero)
                Write(xml, "innlogoutdelay", m_InnLogoutDelay);

            if (m_IOBAlignment != IOBAlignment.None)
                Write(xml, "iobalignment", m_IOBAlignment);

            if (m_MaxFollowerSlots != -1)
                Write(xml, "maxfollowerslots", m_MaxFollowerSlots);

            if (m_VendorAccess != RegionVendorAccess.UseDefault)
                Write(xml, "vendoraccess", m_VendorAccess);

            if (m_GuildAlignment != AlignmentType.None)
                Write(xml, "guildalignment", m_GuildAlignment);

            if (m_Season != SeasonType.Default)
                Write(xml, "season", m_Season);
        }

        public virtual void Load(XmlElement xml)
        {
            Map map = null;
            if (ReadMap(xml, "map", ref map))
                Map = map;

            int priority = 0;
            if (ReadInt32(xml, "priority", ref priority, false))
                Priority = priority;

            string prefix = null;
            if (ReadString(xml, "prefix", ref prefix, false))
                Prefix = prefix;

            string name = null;
            if (ReadString(xml, "name", ref name, false))
                Name = name;

            m_WasRegistered = true;
            ReadBoolean(xml, "registered", ref m_WasRegistered, false);

            ReadEnum<Spawner.ShardConfig>(xml, "shard", ref m_ShardConfig, false);

            foreach (XmlElement rect in xml.GetElementsByTagName("rect"))
            {
                object o;

                if (rect.HasAttribute("zmin"))
                {
                    Rectangle3D rect3D = new Rectangle3D();
                    if (ReadRectangle3D(rect, ref rect3D))
                        Coords.Add(rect3D);
                }
                else
                {
                    Rectangle2D rect2D = new Rectangle2D();
                    if (ReadRectangle2D(rect, ref rect2D))
                        Coords.Add(ConvertTo3D(rect2D));
                }
            }

            foreach (XmlElement rect in xml.GetElementsByTagName("innrect"))
            {
                object o;

                if (rect.HasAttribute("zmin"))
                {
                    Rectangle3D rect3D = new Rectangle3D();
                    if (ReadRectangle3D(rect, ref rect3D))
                        InnBounds.Add(rect3D);
                }
                else
                {
                    Rectangle2D rect2D = new Rectangle2D();
                    if (ReadRectangle2D(rect, ref rect2D))
                        InnBounds.Add(ConvertTo3D(rect2D));
                }
            }

            MusicName music = MusicName.Invalid;
            if (ReadEnum<MusicName>(xml["music"], "name", ref music, false))
                Music = music;

            Point3D go = Point3D.Zero;
            if (ReadPoint3D(xml["go"], map, ref go, false))
                GoLocation = go;

            m_Flags = RegionFlag.None;
            foreach (XmlElement xmlFlag in xml.GetElementsByTagName("flag"))
            {
                RegionFlag flag = RegionFlag.None;
                if (ReadEnum<RegionFlag>(xmlFlag, "name", ref flag))
                    m_Flags |= flag;
            }

            ReadEnum<RegionRuleset>(xml, "ruleset", ref m_Ruleset, false);
            ReadInt32(xml, "lightlevel", ref m_LightLevel, false);

            foreach (XmlElement restrictedSpell in xml.GetElementsByTagName("restrictedspell"))
            {
                int spellID = -1;
                if (!ReadInt32(restrictedSpell, "id", ref spellID, false))
                {
                    Type spellType = null;
                    if (ReadType(restrictedSpell, "name", ref spellType))
                        spellID = Array.IndexOf(SpellRegistry.Types, spellType);
                }
                if (spellID != -1)
                    m_RestrictedSpells.Add(spellID);
            }

            foreach (XmlElement restrictedSkill in xml.GetElementsByTagName("restrictedskill"))
            {
                int skillID = -1;
                if (!ReadInt32(restrictedSkill, "id", ref skillID, false))
                {
                    SkillName skillName = (SkillName)0;
                    if (ReadEnum<SkillName>(restrictedSkill, "name", ref skillName))
                        skillID = (int)skillName;
                }
                if (skillID != -1)
                    m_RestrictedSkills.Add(skillID);
            }

            foreach (XmlElement restrictedItem in xml.GetElementsByTagName("restricteditem"))
            {
                string itemTypeName = null;
                if (ReadString(restrictedItem, "name", ref itemTypeName))
                    m_RestrictedItems.Add(itemTypeName);
            }

            ReadString(xml, "restrictedmagicmessage", ref m_RestrictedMagicMessage, false);
            ReadTimeSpan(xml, "defaultlogoutdelay", ref m_DefaultLogoutDelay, false);
            ReadTimeSpan(xml, "innlogoutdelay", ref m_InnLogoutDelay, false);
            ReadEnum<IOBAlignment>(xml, "iobalignment", ref m_IOBAlignment, false);
            ReadInt32(xml, "maxfollowerslots", ref m_MaxFollowerSlots, false);
            ReadEnum<RegionVendorAccess>(xml, "vendoraccess", ref m_VendorAccess, false);
            ReadEnum<AlignmentType>(xml, "guildalignment", ref m_GuildAlignment, false);
            ReadEnum<SeasonType>(xml, "season", ref m_Season, false);
        }

        // the following are XML read/write utility methods
        // these could be moved to Region.cs

        public static void Write(XmlElement xml, string attribute, object value)
        {
            if (value != null)
                xml.SetAttribute(attribute, value.ToString());
        }

        public static void WritePoint2D(XmlElement xml, Point2D value)
        {
            Write(xml, "x", value.X);
            Write(xml, "y", value.Y);
        }

        public static void WritePoint3D(XmlElement xml, Point3D value)
        {
            Write(xml, "x", value.X);
            Write(xml, "y", value.Y);
            Write(xml, "z", value.Z);
        }

        public static void WriteRectangle2D(XmlElement xml, Rectangle2D value)
        {
            Write(xml, "x", value.X);
            Write(xml, "y", value.Y);
            Write(xml, "width", value.Width);
            Write(xml, "height", value.Height);
        }

        public static void WriteRectangle3D(XmlElement xml, Rectangle3D value)
        {
            Write(xml, "x", value.Start.X);
            Write(xml, "y", value.Start.Y);
            Write(xml, "width", value.Width);
            Write(xml, "height", value.Height);
            Write(xml, "zmin", value.Start.Z);
            Write(xml, "zmax", value.End.Z);
        }

        public static string GetAttribute(XmlElement xml, string attribute, bool mandatory)
        {
            if (xml == null)
            {
                if (mandatory)
                    Console.WriteLine("Missing element for attribute '{0}'", attribute);

                return null;
            }
            else if (!xml.HasAttribute(attribute))
            {
                if (mandatory)
                    Console.WriteLine("Missing attribute '{0}' in element '{1}'", attribute, xml.Name);

                return null;
            }
            else
            {
                return xml.GetAttribute(attribute);
            }
        }

        public static bool ReadString(XmlElement xml, string attribute, ref string value)
        {
            return ReadString(xml, attribute, ref value, true);
        }

        public static bool ReadString(XmlElement xml, string attribute, ref string value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            value = s;
            return true;
        }

        public static bool ReadInt32(XmlElement xml, string attribute, ref int value)
        {
            return ReadInt32(xml, attribute, ref value, true);
        }

        public static bool ReadInt32(XmlElement xml, string attribute, ref int value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = XmlConvert.ToInt32(s);
            }
            catch
            {
                Console.WriteLine("Could not parse integer attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadBoolean(XmlElement xml, string attribute, ref bool value)
        {
            return ReadBoolean(xml, attribute, ref value, true);
        }

        public static bool ReadBoolean(XmlElement xml, string attribute, ref bool value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = XmlConvert.ToBoolean(s);
            }
            catch
            {
                Console.WriteLine("Could not parse boolean attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadDateTime(XmlElement xml, string attribute, ref DateTime value)
        {
            return ReadDateTime(xml, attribute, ref value, true);
        }

        public static bool ReadDateTime(XmlElement xml, string attribute, ref DateTime value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = XmlConvert.ToDateTime(s, XmlDateTimeSerializationMode.Local);
            }
            catch
            {
                Console.WriteLine("Could not parse DateTime attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadTimeSpan(XmlElement xml, string attribute, ref TimeSpan value)
        {
            return ReadTimeSpan(xml, attribute, ref value, true);
        }

        public static bool ReadTimeSpan(XmlElement xml, string attribute, ref TimeSpan value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
#if false
                value = XmlConvert.ToTimeSpan(s);
#else
                value = TimeSpan.Parse(s);
#endif
            }
            catch
            {
                Console.WriteLine("Could not parse TimeSpan attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadEnum<T>(XmlElement xml, string attribute, ref T value) where T : struct
        {
            return ReadEnum(xml, attribute, ref value, true);
        }

        public static bool ReadEnum<T>(XmlElement xml, string attribute, ref T value, bool mandatory) where T : struct // We can't limit the where clause to Enums only
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = (T)Enum.Parse(typeof(T), s, true);
            }
            catch
            {
                Console.WriteLine("Could not parse {0} enum attribute '{1}' in element '{2}'", typeof(T), attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadMap(XmlElement xml, string attribute, ref Map value)
        {
            return ReadMap(xml, attribute, ref value, true);
        }

        public static bool ReadMap(XmlElement xml, string attribute, ref Map value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            try
            {
                value = Map.Parse(s);
            }
            catch
            {
                Console.WriteLine("Could not parse Map attribute '{0}' in element '{1}'", attribute, xml.Name);
                return false;
            }

            return true;
        }

        public static bool ReadType(XmlElement xml, string attribute, ref Type value)
        {
            return ReadType(xml, attribute, ref value, true);
        }

        public static bool ReadType(XmlElement xml, string attribute, ref Type value, bool mandatory)
        {
            string s = GetAttribute(xml, attribute, mandatory);

            if (s == null)
                return false;

            Type type = ScriptCompiler.FindTypeByName(s, false);

            if (type == null)
            {
                Console.WriteLine("Could not find Type '{0}'", s);
                return false;
            }

            value = type;
            return true;
        }

        public static bool ReadPoint3D(XmlElement xml, Map map, ref Point3D value)
        {
            return ReadPoint3D(xml, map, ref value, true);
        }

        public static bool ReadPoint3D(XmlElement xml, Map map, ref Point3D value, bool mandatory)
        {
            int x = 0, y = 0, z = 0;

            bool xOk = ReadInt32(xml, "x", ref x, mandatory);
            bool yOk = ReadInt32(xml, "y", ref y, mandatory);
            bool zOk = ReadInt32(xml, "z", ref z, mandatory && map == null);

            if (xOk && yOk && (zOk || map != null))
            {
                if (!zOk)
                    z = map.GetAverageZ(x, y);

                value = new Point3D(x, y, z);
                return true;
            }

            return false;
        }

        public static bool ReadRectangle2D(XmlElement xml, ref Rectangle2D value)
        {
            return ReadRectangle2D(xml, ref value, true);
        }

        public static bool ReadRectangle2D(XmlElement xml, ref Rectangle2D value, bool mandatory)
        {
            int x1 = 0, y1 = 0, x2 = 0, y2 = 0;

            if (xml.HasAttribute("x"))
            {
                if (ReadInt32(xml, "x", ref x1, mandatory) &&
                    ReadInt32(xml, "y", ref y1, mandatory) &&
                    ReadInt32(xml, "width", ref x2, mandatory) &&
                    ReadInt32(xml, "height", ref y2, mandatory))
                {
                    x2 += x1;
                    y2 += y1;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (ReadInt32(xml, "x1", ref x1, mandatory) &&
                    ReadInt32(xml, "y1", ref y1, mandatory) &&
                    ReadInt32(xml, "x2", ref x2, mandatory) &&
                    ReadInt32(xml, "y2", ref y2, mandatory))
                {
                    // all ok
                }
                else
                {
                    return false;
                }
            }

            value = new Rectangle2D(new Point2D(x1, y1), new Point2D(x2, y2));
            return true;
        }

        public static bool ReadRectangle3D(XmlElement xml, ref Rectangle3D value)
        {
            return ReadRectangle3D(xml, ref value, true);
        }

        public static bool ReadRectangle3D(XmlElement xml, ref Rectangle3D value, bool mandatory)
        {
            Rectangle2D rect2D = new Rectangle2D();

            if (!ReadRectangle2D(xml, ref rect2D))
                return false;

            int z1 = sbyte.MinValue;
            int z2 = sbyte.MaxValue;

            ReadInt32(xml, "zmin", ref z1, false);
            ReadInt32(xml, "zmax", ref z2, false);

            value = new Rectangle3D(new Point3D(rect2D.Start, z1), new Point3D(rect2D.End, z2));
            return true;
        }

        #endregion

        #region Region Database

        public static string DataFileName { get { return Path.Combine(Core.DataDirectory, "StaticRegions.xml"); } }
        public static string SaveFileName = "Saves/StaticRegions.xml";

        private static void EventSink_OnWorldLoad()
        {
            LoadRegions();
        }

        private static void EventSink_OnWorldSave(WorldSaveEventArgs e)
        {
            SaveRegions();
        }

        public static void SaveRegions()
        {
            List<StaticRegion> toSave = new List<StaticRegion>();

            foreach (Region r in Region.Regions)
            {
                StaticRegion sr = r as StaticRegion;

                if (sr != null && !sr.IsDynamicRegion)
                    toSave.Add(sr);
            }

            SaveRegions(toSave, SaveFileName);
        }

        public static void SaveRegions(List<StaticRegion> toSave, string fileName)
        {
            try
            {
                Console.Write("Saving static regions...");

                int count = 0;

                XmlDocument xmlDoc = new XmlDocument();
                XmlElement xmlRoot = xmlDoc.CreateElement("StaticRegions");

                foreach (StaticRegion sr in toSave)
                {
                    XmlElement xmlElem = xmlDoc.CreateElement("region");
                    xmlElem.SetAttribute("type", sr.GetType().Name);
                    sr.Save(xmlElem);
                    xmlRoot.AppendChild(xmlElem);

                    count++;
                }

                xmlDoc.AppendChild(xmlRoot);
                xmlDoc.Save(fileName);

                Console.WriteLine("Done! Saved {0} static region(s).", count);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        public static void LoadRegions()
        {
            if (LoadRegions(SaveFileName, false) == 0)
                LoadRegions(DataFileName, true); // bad regions file, get them from Data
        }

        public static int LoadRegions(string fileName, bool initRegistration)
        {
            int count = 0;

            try
            {
                if (!File.Exists(fileName))
                    return 0;

                Console.Write("Loading static regions from \"{0}\"...\n", fileName);

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(fileName);

                XmlElement xmlRoot = xmlDoc["StaticRegions"];

                foreach (XmlElement xmlElem in xmlRoot.GetElementsByTagName("region"))
                {
                    if (xmlElem.GetAttribute("type") != "StaticRegion")
                        continue;

                    //Console.WriteLine("Reading {0}", xmlElem.GetAttribute("name"));

                    StaticRegion region = new StaticRegion();

                    region.Load(xmlElem);

                    if (initRegistration)
                        region.Registered = Spawner.ShouldShardEnable(region.ShardConfig);
                    else
                        region.Registered = region.WasRegistered;

                    m_XmlDatabase.Add(region);

                    count++;
                }

                Console.WriteLine("Done! Loaded {0} static region(s).", count);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }

            return count;
        }

        #endregion

        #region Region Controller Patcher

        public static int PatchRegionControllers()
        {
            int count = 0;

            for (int i = CustomRegionControl.Instances.Count - 1; i >= 0; i--)
            {
                CustomRegionControl crc = CustomRegionControl.Instances[i];

                if (crc.Parent == null)
                {
                    CustomRegion cr = crc.CustomRegion;

                    for (int j = 0; j < m_XmlDatabase.Count; j++)
                    {
                        StaticRegion sr = m_XmlDatabase[j];
                        // 1/1/23, Adam: removed MatchArea since I updated the areas in Data/StaticRegions.xml to match RunUO 2.6
                        //  ie., they won't match the controllers they are replacing.
                        if (cr.Name == sr.Name && cr.Map == sr.Map /*&& MatchArea(cr.Coords, sr.Coords)*/)
                        {
                            // only static-ify custom region controllers if the static region in question is registered
                            if (sr.Registered)
                            {
                                count++;
                                StaticRegionControl src = new StaticRegionControl();
                                src.ItemID = crc.ItemID;
                                src.Visible = crc.Visible;
                                src.StaticIdx = j;
                                src.MoveToWorld(crc.Location, crc.Map);
                                crc.Delete();
                            }

                            break;
                        }
                    }
                }
            }

            return count;
        }

        private static bool MatchArea(List<Rectangle3D> l, List<Rectangle3D> r)
        {
            if (l.Count != r.Count)
                return false;

            for (int i = 0; i < l.Count; i++)
            {
                Rectangle3D lRect = l[i];
                Rectangle3D rRect = r[i];

                if (lRect.Start != rRect.Start || lRect.End != rRect.End)
                    return false;
            }

            return true;
        }

        #endregion

        #region Static Region <-> Custom Region

        [Usage("ConvertToCustomRegion")]
        [Description("Converts static region to custom region.")]
        private static void ConvertToCustomRegion_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the static region controller.");
            e.Mobile.BeginTarget(-1, false, Targeting.TargetFlags.None, ConvertToCustomRegion_OnTarget);
        }

        private static void ConvertToCustomRegion_OnTarget(Mobile from, object targeted)
        {
            if (!(targeted is StaticRegionControl))
                from.SendMessage("That is not a static region controller.");
            else if (ConvertToCustom((StaticRegionControl)targeted) == null)
                from.SendMessage("Failed.");
            else
                from.SendMessage("Done.");
        }

        [Usage("ConvertToStaticRegion")]
        [Description("Converts custom region to static region.")]
        private static void ConvertToStaticRegion_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the custom region controller.");
            e.Mobile.BeginTarget(-1, false, Targeting.TargetFlags.None, ConvertToStaticRegion_OnTarget);
        }

        private static void ConvertToStaticRegion_OnTarget(Mobile from, object targeted)
        {
            if (!(targeted is CustomRegionControl))
                from.SendMessage("That is not a custom region controller.");
            else if (ConvertToStatic((CustomRegionControl)targeted) == null)
                from.SendMessage("Failed.");
            else
                from.SendMessage("Done.");
        }

        public static CustomRegionControl ConvertToCustom(StaticRegionControl src)
        {
            return (src.StaticRegion == null ? null : ConvertToCustom(src.StaticRegion, src.Location, src.Map));
        }

        public static CustomRegionControl ConvertToCustom(StaticRegion sr, Point3D controllerLoc, Map controllerMap)
        {
            CustomRegionControl crc = new CustomRegionControl();
            CustomRegion cr = crc.CustomRegion;

            bool registered = sr.Registered;
            sr.Registered = false;

            if (!CopyRegionProps(sr, cr))
            {
                crc.Delete();
                sr.Registered = registered;
                return null;
            }

            for (int i = StaticRegionControl.Instances.Count - 1; i >= 0; i--)
            {
                StaticRegionControl src = StaticRegionControl.Instances[i];

                if (src.StaticRegion == sr)
                    src.Delete();
            }

            m_XmlDatabase.Remove(sr);

            crc.MoveToWorld(controllerLoc, controllerMap);

            cr.Registered = registered;

            return crc;
        }

        public static StaticRegionControl ConvertToStatic(CustomRegionControl crc)
        {
            return (crc.CustomRegion == null ? null : ConvertToStatic(crc.CustomRegion));
        }

        public static StaticRegionControl ConvertToStatic(CustomRegion cr)
        {
            StaticRegion sr = new StaticRegion();

            bool registered = cr.Registered;
            cr.Registered = false;

            if (!CopyRegionProps(cr, sr))
            {
                cr.Registered = registered;
                return null;
            }

            m_XmlDatabase.Add(sr);

            StaticRegionControl src = new StaticRegionControl();

            src.StaticRegion = sr;
            src.MoveToWorld(cr.Controller.Location, cr.Controller.Map);

            sr.Registered = registered;

            cr.Controller.Delete();

            return src;
        }

        private static bool CopyRegionProps(StaticRegion src, StaticRegion trg)
        {
            try
            {
                PropertyInfo[] properties = typeof(StaticRegion).GetProperties(BindingFlags.Instance | BindingFlags.Public);

                foreach (PropertyInfo propertyInfo in properties)
                {
                    if (!propertyInfo.CanRead || !propertyInfo.CanWrite || propertyInfo.Name == "Registered")
                        continue;

                    propertyInfo.SetValue(trg, propertyInfo.GetValue(src));
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        #endregion
    }
}