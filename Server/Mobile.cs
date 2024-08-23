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

using Server.Accounting;
using Server.Commands;
using Server.ContextMenus;
using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
using Server.Guilds;
using Server.Gumps;
using Server.HuePickers;
using Server.Items;
using Server.Menus;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Spells;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static Server.Item;
namespace Server
{
    #region enums and helpers
    [Flags]
    public enum StatType
    {
        Str = 1,
        Dex = 2,
        Int = 4,
        All = 7
    }

    public enum StatLockType : byte
    {
        Up,
        Down,
        Locked
    }

    public delegate void TargetCallback(Mobile from, object targeted);
    public delegate void TargetStateCallback(Mobile from, object targeted, object state);

    public class TimedSkillMod : SkillMod
    {
        private DateTime m_Expire;

        public TimedSkillMod(SkillName skill, bool relative, double value, TimeSpan delay)
            : this(skill, relative, value, DateTime.UtcNow + delay)
        {
        }

        public TimedSkillMod(SkillName skill, bool relative, double value, DateTime expire)
            : base(skill, relative, value)
        {
            m_Expire = expire;
        }

        public override bool CheckCondition()
        {
            return (DateTime.UtcNow < m_Expire);
        }
    }

    public class EquipedSkillMod : SkillMod
    {
        private Item m_Item;
        private Mobile m_Mobile;

        public EquipedSkillMod(SkillName skill, bool relative, double value, Item item, Mobile mobile)
            : base(skill, relative, value)
        {
            m_Item = item;
            m_Mobile = mobile;
        }

        public override bool CheckCondition()
        {
            return (!m_Item.Deleted && !m_Mobile.Deleted && m_Item.Parent == m_Mobile);
        }
    }

    public class DefaultSkillMod : SkillMod
    {
        public DefaultSkillMod(SkillName skill, bool relative, double value)
            : base(skill, relative, value)
        {
        }

        public override bool CheckCondition()
        {
            return true;
        }
    }

    public class DamageEntry
    {
        private Mobile m_Damager;
        private int m_DamageGiven;
        private DateTime m_LastDamage;
        private List<DamageEntry> m_Responsible;

        public Mobile Damager { get { return m_Damager; } }
        public int DamageGiven { get { return m_DamageGiven; } set { m_DamageGiven = value; } }
        public DateTime LastDamage { get { return m_LastDamage; } set { m_LastDamage = value; } }
        public bool HasExpired { get { return (DateTime.UtcNow > (m_LastDamage + m_ExpireDelay)); } }
        public List<DamageEntry> Responsible { get { return m_Responsible; } set { m_Responsible = value; } }

        private TimeSpan m_ExpireDelay = TimeSpan.FromMinutes(2.0);

        public TimeSpan ExpireDelay
        {
            get { return m_ExpireDelay; }
            set { m_ExpireDelay = value; }
        }

        public DamageEntry(Mobile damager)
        {
            m_Damager = damager;
        }
    }

    public abstract class SkillMod
    {
        private Mobile m_Owner;
        private SkillName m_Skill;
        private bool m_Relative;
        private double m_Value;
        private bool m_ObeyCap;

        public SkillMod(SkillName skill, bool relative, double value)
        {
            m_Skill = skill;
            m_Relative = relative;
            m_Value = value;
        }

        public bool ObeyCap
        {
            get { return m_ObeyCap; }
            set
            {
                m_ObeyCap = value;

                if (m_Owner != null)
                {
                    Skill sk = m_Owner.Skills[m_Skill];

                    if (sk != null)
                        sk.Update();
                }
            }
        }

        public Mobile Owner
        {
            get
            {
                return m_Owner;
            }
            set
            {
                if (m_Owner != value)
                {
                    if (m_Owner != null)
                        m_Owner.RemoveSkillMod(this);

                    m_Owner = value;

                    if (m_Owner != value)
                        m_Owner.AddSkillMod(this);
                }
            }
        }

        public void Remove()
        {
            Owner = null;
        }

        public SkillName Skill
        {
            get
            {
                return m_Skill;
            }
            set
            {
                if (m_Skill != value)
                {
                    Skill oldUpdate = (m_Owner == null ? m_Owner.Skills[m_Skill] : null);

                    m_Skill = value;

                    if (m_Owner != null)
                    {
                        Skill sk = m_Owner.Skills[m_Skill];

                        if (sk != null)
                            sk.Update();
                    }

                    if (oldUpdate != null)
                        oldUpdate.Update();
                }
            }
        }

        public bool Relative
        {
            get
            {
                return m_Relative;
            }
            set
            {
                if (m_Relative != value)
                {
                    m_Relative = value;

                    if (m_Owner != null)
                    {
                        Skill sk = m_Owner.Skills[m_Skill];

                        if (sk != null)
                            sk.Update();
                    }
                }
            }
        }

        public bool Absolute
        {
            get
            {
                return !m_Relative;
            }
            set
            {
                if (m_Relative == value)
                {
                    m_Relative = !value;

                    if (m_Owner != null)
                    {
                        Skill sk = m_Owner.Skills[m_Skill];

                        if (sk != null)
                            sk.Update();
                    }
                }
            }
        }

        public double Value
        {
            get
            {
                return m_Value;
            }
            set
            {
                if (m_Value != value)
                {
                    m_Value = value;

                    if (m_Owner != null)
                    {
                        Skill sk = m_Owner.Skills[m_Skill];

                        if (sk != null)
                            sk.Update();
                    }
                }
            }
        }

        public abstract bool CheckCondition();
    }

    public class ResistanceMod
    {
        private Mobile m_Owner;
        private ResistanceType m_Type;
        private int m_Offset;

        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public ResistanceType Type
        {
            get { return m_Type; }
            set
            {
                if (m_Type != value)
                {
                    m_Type = value;

                    if (m_Owner != null)
                        m_Owner.UpdateResistances();
                }
            }
        }

        public int Offset
        {
            get { return m_Offset; }
            set
            {
                if (m_Offset != value)
                {
                    m_Offset = value;

                    if (m_Owner != null)
                        m_Owner.UpdateResistances();
                }
            }
        }

        public ResistanceMod(ResistanceType type, int offset)
        {
            m_Type = type;
            m_Offset = offset;
        }
    }

    public class StatMod
    {
        private StatType m_Type;
        private string m_Name;
        private double m_Offset;
        private TimeSpan m_Duration;
        private DateTime m_Added;

        public StatType Type { get { return m_Type; } }
        public string Name { get { return m_Name; } }
        public double Offset { get { return m_Offset; } }

        public bool HasElapsed()
        {
            if (m_Duration == TimeSpan.Zero)
                return false;

            return (DateTime.UtcNow - m_Added) >= m_Duration;
        }

        public StatMod(StatType type, string name, double offset, TimeSpan duration)
        {
            m_Type = type;
            m_Name = name;
            m_Offset = offset;
            m_Duration = duration;
            m_Added = DateTime.UtcNow;
        }
    }

    [CustomEnum(new string[] { "North", "Right", "East", "Down", "South", "Left", "West", "Up" })]
    public enum Direction : byte
    {
        North = 0x0,
        Right = 0x1,
        East = 0x2,
        Down = 0x3,
        South = 0x4,
        Left = 0x5,
        West = 0x6,
        Up = 0x7,
        Mask = 0x7,
        Running = 0x80,
        ValueMask = 0x87
    }

    [Flags]
    public enum MobileDelta
    {
        None = 0x00000000,
        Name = 0x00000001,
        Flags = 0x00000002,
        Hits = 0x00000004,
        Mana = 0x00000008,
        Stam = 0x00000010,
        Stat = 0x00000020,
        Noto = 0x00000040,
        Gold = 0x00000080,
        Weight = 0x00000100,
        Direction = 0x00000200,
        Hue = 0x00000400,
        Body = 0x00000800,
        Armor = 0x00001000,
        StatCap = 0x00002000,
        GhostUpdate = 0x00004000,
        Followers = 0x00008000,
        Properties = 0x00010000,
        TithingPoints = 0x00020000,
        Resistances = 0x00040000,
        WeaponDamage = 0x00080000,
        Hair = 0x00100000,
        FacialHair = 0x00200000,
        Race = 0x00400000,
        HealthbarYellow = 0x00800000,
        HealthbarPoison = 0x01000000,

        Attributes = 0x0000001C
    }

    // Add new Access Levels and prepare old ones for conversion.
    // Adam: when you update this, you MUST also update the names table GetAccessLevelName() && FormatAccessLevel()
    public enum AccessLevel
    {
        Player = 100,
        Reporter = 115,
        FightBroker = 130,
        Counselor = 145,
        GameMaster = 160,
        Seer = 175,
        Administrator = 205,
        Owner = 220,
        System = 300
    }

    public enum VisibleDamageType
    {
        None,
        Related,
        Everyone
    }

    public enum ResistanceType
    {
        Physical,
        Fire,
        Cold,
        Poison,
        Energy
    }

    public class MobileNotConnectedException : Exception
    {
        public MobileNotConnectedException(Mobile source, string message)
            : base(message)
        {
            this.Source = source.ToString();
        }
    }

    public delegate bool SkillCheckTargetHandler(Mobile from, SkillName skill, object target, double minSkill, double maxSkill);
    public delegate bool SkillCheckLocationHandler(Mobile from, SkillName skill, double minSkill, double maxSkill);

    public delegate bool SkillCheckDirectTargetHandler(Mobile from, SkillName skill, object target, double chance);
    public delegate bool SkillCheckDirectLocationHandler(Mobile from, SkillName skill, double chance);

    public delegate TimeSpan RegenRateHandler(Mobile from);

    public delegate bool AllowBeneficialHandler(Mobile from, Mobile target);
    public delegate bool AllowHarmfulHandler(Mobile from, Mobile target);

    public delegate Container CreateCorpseHandler(Mobile from, ArrayList initialContent, ArrayList equipedItems);

    public delegate void StatChangeHandler(Mobile from, StatType stat);

    public enum ApplyPoisonResult
    {
        Poisoned,
        Immune,
        HigherPoisonActive,
        Cured
    }
    #endregion enums and helpers
    /// <summary>
    /// Base class representing players, npcs, and creatures.
    /// </summary>
    public partial class Mobile : IEntity, IPoint3D, IHued
    {
        public virtual long SlowMovement()
        {   // called by movement AI so that individual creatures can be slowed
            return 0;   // milliseconds
        }

        public virtual bool SetObjectProp(string name, string value, bool message = true)
        {   // this is will only set properties with the Command Property Attribute. This is intended.
            //  We don't want to give GM's a back door into the guts of the object.
            string result = Server.Commands.Properties.SetValue(World.GetSystemAcct(), this, name, value);
            if (message)
                this.SendSystemMessage(result);
            if (result == "Property has been set.")
                return true;
            else
                return false;
        }
        #region CompareTo(...)
        public int CompareTo(IEntity other)
        {
            if (other == null)
                return -1;

            return m_Serial.CompareTo(other.Serial);
        }

        public int CompareTo(Mobile other)
        {
            return this.CompareTo((IEntity)other);
        }

        public int CompareTo(object other)
        {
            if (other == null || other is IEntity)
                return this.CompareTo((IEntity)other);

            throw new ArgumentException();
        }
        #endregion

        #region Mobile Flags
        [Flags]
        public enum MobileBoolTable
        {
            None = 0x00,
            IsTemplate = 0x01,          // is this a template mobile? 
            ToDelete = 0x02,            // is this mobile marked for deletion?
            StaffOwned = 0x04,          // is this mobile owned by staff?
            IsIntMapStorage = 0x08,     // Adam: Is Internal Map Storage?
            InvisibleShield = 0x10,     // under the effect of the NPC Invisible Shield Spell
            SuicideBomber = 0x20,       // did this player detonate an explosion on their person?
            IsInvulnerable = 0x40,      // is this mobile Invulnerable?
            BlockDamage = 0x80,         // block damage without being Invulnerable (training dummy or staff protection)
            NotGuardWackable = 0x100,   // is this criminal offense a guard whack offense?
            TempObject = 0x200,         // this is a temp item and can be cleaned up by Cron
            Incurable = 0x400,          // various mobiles use poison as a way to end their life (hire fighters.) It cannot be cured
            HideHidden = 0x800,         //Adam - hide hidden objects from staff (for making videos)
            IsLinked = 0x1000,          // this object is linked by another object. In this case, Cron cleanup will leave it alone
            TrainingMobile = 0x2000,    // If !CrashDummy and BlockDamage, no skill gains
            MissionCritical = 0x4000,   // If MissionCritical and BlockDamage, Hulk Smash!
        }
        public void SetMobileBool(MobileBoolTable flag, bool value)
        {
            if (value)
                m_BoolTable |= flag;
            else
                m_BoolTable &= ~flag;
        }

        public bool GetMobileBool(MobileBoolTable flag)
        {
            return ((m_BoolTable & flag) != 0);
        }

        public MobileBoolTable BoolTable { get { return m_BoolTable; } set { m_BoolTable = value; } }
        #endregion Mobile Flags

        #region Stable Management
        // the first time this stable charging system is run, this value will cause all pets in stables to be charged.
        //	after tis first run, m_LastStableChargeTime will be correctly set on server load and set at Claim/Stable
        private const double SecondsPerUOMinute = 5.0;
        private const double MinutesPerUODay = SecondsPerUOMinute * 24;
        private DateTime m_LastStableChargeTime = DateTime.UtcNow - TimeSpan.FromMinutes(MinutesPerUODay);
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastStableChargeTime { get { return m_LastStableChargeTime; } set { m_LastStableChargeTime = value; } }

        private int m_StableBackFees;
        public int StableBackFees { get { return m_StableBackFees; } set { m_StableBackFees = value; } }
        #endregion Stable Management

        #region ExpireStates
        public enum ExpirationFlagID
        {   // non Notoriety flags
            Invalid = 0,    // error
            EvilCrim,       // Evil attacks innocent, no gate or recall etc.
            NoPoints,       // insta-kill earns no points ([un]holy word)
            MonsterIgnore,  // evil power Monster Ignore
            ShieldIgnore,   // you were close to the hero when he cast, so you can pass through the shield

            // 75 on reserved Notoriety flags (sends notoriety update packets)
            BeginNotoFlags = 75,

            // Notoriety flags
            EvilNoto = 100,         // attack and Evil and become gray to Evils
            FallenHero = 101,       // Hero drops to 0 life force
        }
        private List<ExpirationFlag> m_expirationFlags = new List<ExpirationFlag>();
        public List<ExpirationFlag> ExpirationFlags { get { return m_expirationFlags; } }
        public class ExpirationFlag
        {
            private ExpirationFlagID m_flagID;  // what
            private DateTime m_start;           // when start
            private TimeSpan m_span;            // how long
            private Mobile m_mobile;            // me
            private Timer m_timer;              // associated timer
            public bool Expired { get { try { return DateTime.UtcNow > m_start + m_span; } catch { return true; } } }
            public ExpirationFlag(Mobile mobile, ExpirationFlagID state, TimeSpan span)
                : this(mobile, state, DateTime.UtcNow, span)
            {
            }
            public ExpirationFlag(Mobile mobile, ExpirationFlagID state, DateTime start, TimeSpan span)
            {
                m_flagID = state;               // what
                m_start = start;                // when start
                m_span = span;                  // how long
                m_mobile = mobile;              // hello, it's me
                if (Expired)                    // delete it now
                    m_timer = Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(StateCallback), this);
                else                            // delete on schedule
                    m_timer = Timer.DelayCall(m_span, new TimerStateCallback(StateCallback), this);

                // if we are setting a noto state, update what everyone sees
                if (state > ExpirationFlagID.BeginNotoFlags)
                {   // force the client to recognize the new notoriety
                    mobile.Delta(MobileDelta.Noto);
                }

                // the flag is set
                if (mobile.CheckState(m_flagID) == false)
                    mobile.OnFlagChange(this, true);

                mobile.CleanupOld(this, state); // stop duplicate timers
            }

            public ExpirationFlagID FlagID { get { return m_flagID; } }
            public DateTime Start { get { return m_start; } }
            public TimeSpan Span { get { return m_span; } }
            public Mobile Mobile { get { return m_mobile; } }
            public Timer Timer { get { return m_timer; } }
        }
        public void CleanupOld(ExpirationFlag fresh, ExpirationFlagID fid)
        {
            // stop duplicate timers
            ExpirationFlag found = null;
            foreach (ExpirationFlag old in m_expirationFlags)
            {
                if (old.FlagID == fid && old != fresh)
                {
                    old.Timer.Stop();
                    found = old;
                    break;
                }
            }

            // remove duplicate flags
            if (found != null)
            {
                // delete the noto flag 
                List<ExpirationFlag> temp = new List<ExpirationFlag>(found.Mobile.ExpirationFlags);
                found.Mobile.ExpirationFlags.Clear();
                foreach (ExpirationFlag es in temp)
                {
                    if (es == found)
                        continue;

                    found.Mobile.ExpirationFlags.Add(es);
                }
            }
        }
        public void RemoveState(ExpirationFlagID ef)
        {   // we remove a flag by simply adding a new one with a 'right now' time out
            this.ExpirationFlags.Add(new Mobile.ExpirationFlag(this, ef, TimeSpan.FromMinutes(0)));
        }
        public bool CheckState(ExpirationFlagID state)
        {
            foreach (ExpirationFlag es in m_expirationFlags)
            {
                if (es.FlagID == state)
                    return true;
            }

            return false;
        }
        public int GetStateCount
        {
            get
            {
                return m_expirationFlags.Count;
            }
        }
        private static void StateCallback(object state)
        {
            ExpirationFlag ef = state as ExpirationFlag;
            Mobile me = ef.Mobile;

            // delete the flag first
            List<ExpirationFlag> temp = new List<ExpirationFlag>(me.ExpirationFlags);
            me.ExpirationFlags.Clear();
            foreach (ExpirationFlag es in temp)
            {
                if (es == ef)
                {   // the flag is cleared
                    es.Mobile.OnFlagChange(es, false);
                    continue;
                }

                me.ExpirationFlags.Add(es);
            }

            // send noto update packets only for noto flags!
            if (ef.FlagID > ExpirationFlagID.BeginNotoFlags)
            {   // noto updates
                IPooledEnumerable eable = me.GetMobilesInRange(18);
                foreach (Mobile you in eable)
                {
                    if (me.m_NetState != null && me.CanSee(you) && Utility.InUpdateRange(me.m_Location, you.m_Location))
                    {
#if false
                        // I see your new noto (Mobile beholder, Mobile beheld)
                        me.m_NetState.Send(new MobileIncoming(me, you));

                        // you see my new noto (Mobile beholder, Mobile beheld)
                        if (you.m_NetState != null)
                            you.m_NetState.Send(new MobileIncoming(you, me));
#else

                        if (ObjectPropertyList.Enabled)
                        {   // doesn't seem to be needed (presumably SendEverything handles it)
                            //me.Send(you.OPLPacket);
                            //foreach (Item item in me.Items)
                            //me.Send(item.OPLPacket);

                            me.SendEverything();
                        }
                        else
                            // I see your new noto (Mobile beholder, Mobile beheld)
                            me.m_NetState.Send(new MobileIncoming(me, you));

                        if (you.m_NetState != null)
                        {
                            if (ObjectPropertyList.Enabled)
                            {   // doesn't seem to be needed (presumably SendEverything handles it)
                                //you.Send(me.OPLPacket);
                                //foreach (Item item in you.Items)
                                //you.Send(item.OPLPacket);

                                you.SendEverything();
                            }
                            else
                                // you see my new noto (Mobile beholder, Mobile beheld)
                                you.m_NetState.Send(new MobileIncoming(you, me));
                        }
#endif
                    }
                }
                eable.Free();
            }
        }
        public virtual void OnFlagChange(ExpirationFlag es, bool set)
        {
        }

        #endregion
        /// <summary>
        /// Overridable. virtual method indicating this temporary item has expired and can be cleaned up by the normal decay/cleanup Cron routines
        /// </summary>
        /// <returns>true if this method can be deleted</returns>
        public virtual bool CanDelete()
        {   // default expiration date. Override if you want a different date and possibly other conditions
            bool canDelete = GetMobileBool(MobileBoolTable.TempObject) && !Deleted && DateTime.UtcNow > Created + TimeSpan.FromMinutes(15);
            return canDelete;
        }
        public virtual bool CanOpenDoors
        {
            get
            {
                return false;
            }
        }

        public virtual bool CanMoveOverObstacles
        {
            get
            {
                return false;
            }
        }

        public virtual bool CanDestroyObstacles
        {
            get
            {
                // to enable breaking of furniture, 'return CanMoveOverObstacles;'
                return false;
            }
        }

        /// <summary>
        /// Check access of caller and allow if appropriate access and print a message
        /// </summary>
        /// <param name="GodlyAccess"></param>
        /// <returns></returns>
        public bool Godly(AccessLevel GodlyAccess)
        {
            if (AccessLevel >= GodlyAccess)
            {
                SendMessage("You invoke your godly powers.");
                return true;
            }

            return true;
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
        public bool DenyAccessPublicContainer
        {
            get
            {
                Accounting.Account acct = this.Account as Accounting.Account;
                if (acct != null)
                    return acct.GetFlag(Accounting.Account.AccountFlag.DenyAccessPublicContainer);
                return false;

            }
            set
            {
                Accounting.Account acct = this.Account as Accounting.Account;
                if (acct != null)
                    acct.SetFlag(Accounting.Account.AccountFlag.DenyAccessPublicContainer, value);
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsStaffOwned
        {
            get { return GetMobileBool(MobileBoolTable.StaffOwned); }
            set { SetMobileBool(MobileBoolTable.StaffOwned, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ToDelete
        {
            get { return GetMobileBool(MobileBoolTable.ToDelete); }
            set { SetMobileBool(MobileBoolTable.ToDelete, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool BlockDamage
        {
            get { return GetMobileBool(MobileBoolTable.BlockDamage); }
            set { SetMobileBool(MobileBoolTable.BlockDamage, value); InvalidateProperties(); }
        }
        //
        [CommandProperty(AccessLevel.GameMaster)]
        public bool MissionCritical
        {
            get { return GetMobileBool(MobileBoolTable.MissionCritical); }
            set { SetMobileBool(MobileBoolTable.MissionCritical, value); InvalidateProperties(); }
        }
        private static bool m_DragEffects = true;

        public static bool DragEffects
        {
            get { return m_DragEffects; }
            set { m_DragEffects = value; }
        }

        private static AllowBeneficialHandler m_AllowBeneficialHandler;
        private static AllowHarmfulHandler m_AllowHarmfulHandler;

        public static AllowBeneficialHandler AllowBeneficialHandler
        {
            get { return m_AllowBeneficialHandler; }
            set { m_AllowBeneficialHandler = value; }
        }

        public static AllowHarmfulHandler AllowHarmfulHandler
        {
            get { return m_AllowHarmfulHandler; }
            set { m_AllowHarmfulHandler = value; }
        }

        public virtual TimeSpan HitsRegenRate
        {
            get
            {
                return TimeSpan.FromSeconds(11.0);
            }
        }

        public virtual TimeSpan ManaRegenRate
        {
            get
            {
                if (m_ManaRegenRate != null)
                    return m_ManaRegenRate(this);

                else
                    return TimeSpan.FromSeconds(7.0);
            }
        }

        public virtual TimeSpan StamRegenRate
        {
            get
            {
                return TimeSpan.FromSeconds(7.0);
            }
        }

        private static RegenRateHandler m_ManaRegenRate;/*, m_HitsRegenRate, m_StamRegenRate;
		private static TimeSpan m_DefaultHitsRate, m_DefaultStamRate, m_DefaultManaRate;

		public static RegenRateHandler HitsRegenRateHandler
		{
			get{ return m_HitsRegenRate; }
			set{ m_HitsRegenRate = value; }
		}

		public static TimeSpan DefaultHitsRate
		{
			get{ return m_DefaultHitsRate; }
			set{ m_DefaultHitsRate = value; }
		}

		public static RegenRateHandler StamRegenRateHandler
		{
			get{ return m_StamRegenRate; }
			set{ m_StamRegenRate = value; }
		}

		public static TimeSpan DefaultStamRate
		{
			get{ return m_DefaultStamRate; }
			set{ m_DefaultStamRate = value; }
		}*/

        public static RegenRateHandler ManaRegenRateHandler
        {
            get { return m_ManaRegenRate; }
            set { m_ManaRegenRate = value; }
        }/*

		public static TimeSpan DefaultManaRate
		{
			get{ return m_DefaultManaRate; }
			set{ m_DefaultManaRate = value; }
		}

		public static TimeSpan GetHitsRegenRate( Mobile m )
		{
			if ( m_HitsRegenRate == null )
				return m_DefaultHitsRate;
			else
				return m_HitsRegenRate( m );
		}

		public static TimeSpan GetStamRegenRate( Mobile m )
		{
			if ( m_StamRegenRate == null )
				return m_DefaultStamRate;
			else
				return m_StamRegenRate( m );
		}

		public static TimeSpan GetManaRegenRate( Mobile m )
		{
			if ( m_ManaRegenRate == null )
				return m_DefaultManaRate;
			else
				return m_ManaRegenRate( m );
		}*/

        private class MovementRecord
        {
            public DateTime m_End;

            private static Queue m_InstancePool = new Queue();

            public static MovementRecord NewInstance(DateTime end)
            {
                MovementRecord r;

                if (m_InstancePool.Count > 0)
                {
                    r = (MovementRecord)m_InstancePool.Dequeue();

                    r.m_End = end;
                }
                else
                {
                    r = new MovementRecord(end);
                }

                return r;
            }

            private MovementRecord(DateTime end)
            {
                m_End = end;
            }

            public bool Expired()
            {
                bool v = (DateTime.UtcNow >= m_End);

                if (v)
                    m_InstancePool.Enqueue(this);

                return v;
            }
        }

        public event StatChangeHandler StatChange;

        private Serial m_Serial;
        private Map m_Map;
        private Point3D m_Location;
        private Direction m_Direction;
        private Body m_Body;
        private int m_Hue;
        private Poison m_Poison;
        private Timer m_PoisonTimer;
        private BaseGuild m_Guild;
        private string m_GuildTitle;
        private bool m_Criminal;
        private string m_Name;
        private int m_Kills, m_ShortTermMurders;
        private int m_SpeechHue, m_EmoteHue, m_WhisperHue, m_YellHue;
        private string m_Language;
        private NetState m_NetState;
        private bool m_Female, m_Warmode, m_Hidden, m_Blessed;
        private int m_StatCap;
        private int m_STRBonusCap;
        private int m_Str, m_Dex, m_Int;
        private int m_Hits, m_Stam, m_Mana;
        private int m_Fame, m_Karma;
        private AccessLevel m_AccessLevel = AccessLevel.Player; //hard code to New player level!
        private Skills m_Skills;
        private List<Item> m_Items;
        private bool m_Player;
        private string m_Title;
        private string m_Profile;
        private bool m_ProfileLocked;
        private int m_LightLevel;
        private int m_TotalGold, m_TotalWeight;
        private ArrayList m_StatMods;
        private ISpell m_Spell;
        private Target m_Target;
        private Prompt m_Prompt;
        private ContextMenu m_ContextMenu;
        private List<AggressorInfo> m_Aggressors, m_Aggressed;
        private Mobile m_Combatant;
        //private ArrayList m_PetStable;
        private bool m_AutoPageNotify;
        private bool m_Meditating;
        private bool m_CanHearGhosts;
        private bool m_CanSwim, m_CantWalkLand, m_CanFlyOver;
        private MobileBoolTable m_BoolTable;
        private bool m_DisplayGuildTitle;
        private Mobile m_GuildFealty;
        private DateTime m_NextSpellTime;
        private DateTime[] m_StuckMenuUses;
        private Timer m_ExpireCombatant;
        private Timer m_ExpireCriminal;
        private Timer m_ExpireAggrTimer;
        private Timer m_LogoutTimer;
        private Timer m_CombatTimer;
        private Timer m_ManaTimer, m_HitsTimer, m_StamTimer;
        private long m_NextSkillTime;
        private long m_NextActionTime; // Use, pickup, etc
        private DateTime m_NextActionMessage;
        private bool m_Paralyzed;
        private ParalyzedTimer m_ParaTimer;
        private InvisibleShieldTimer m_InvisibleShieldTimer;
        private SuicideBomberTimer m_SuicideBomberTimer;
        private bool m_Frozen;
        private FrozenTimer m_FrozenTimer;
        private int m_AllowedStealthSteps;
        private int m_Hunger;
        private int m_NameHue = -1;
        private Region m_Region;
        private bool m_DisarmReady, m_StunReady;
        private int m_BaseSoundID;
        private int m_VirtualArmor;
        private bool m_Squelched;
        private int m_MeleeDamageAbsorb;
        private int m_MagicDamageAbsorb;
        private int m_FollowerCount, m_FollowersMax;
        private ArrayList m_Actions;
        private Queue<MovementRecord> m_MoveRecords;
        private int m_WarmodeChanges = 0;
        private DateTime m_NextWarmodeChange;
        private WarmodeTimer m_WarmodeTimer;
        private int m_Thirst, m_BAC;
        private int m_VirtualArmorMod;
        private VirtueInfo m_Virtues;
        private object m_Party;
        private ArrayList m_SkillMods = new ArrayList(1);
        private Body m_BodyMod;
        private DateTime m_LastStatGain;
        private bool m_HasAbilityReady;
        private DateTime m_NextAbilityTime;
        private int[] FlyIDs = new int[] { };
        private uint m_GUID = 0;
        #region RunUO2.6 Port compatibility functions

        public int GetAOSStatus(int index)
        {
            //return (m_AOSStatusHandler == null) ? 0 : m_AOSStatusHandler(this, index);
            return 0;
        }
        public virtual void ToggleFlying() { }
        public virtual int PhysicalResistance { get { return 0; } }
        public virtual int FireResistance { get { return 0; } }
        public virtual int ColdResistance { get { return 0; } }
        public virtual int PoisonResistance { get { return 0; } }
        public virtual int EnergyResistance { get { return 0; } }
        #endregion RunUO2.6 Port compatibility functions
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual uint GUID
        {
            get { return m_GUID; }
            set { m_GUID = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextAbilityTime
        {
            get { return m_NextAbilityTime; }
            set { m_NextAbilityTime = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasAbilityReady
        {   // if we are not allowing players to toggle, then we are always ready, and we must rely on the 20-30% chance
            get { return m_HasAbilityReady || !Core.RuleSets.UseToggleSpecialAbility(); }
            set { m_HasAbilityReady = Core.RuleSets.UseToggleSpecialAbility() ? value : true; }
        }

        //array accessor for setting/retrieving fly tiles
        [CopyableAttribute(CopyType.DoNotCopy)]
        public int[] FlyArray
        {
            get { return FlyIDs; }
            set { FlyIDs = value; }
        }

        //virtual outfit and body init routines
        public virtual void InitOutfit()
        {
        }

        //name/sex
        public virtual void InitBody()
        {
        }

        //str/hits/virtual armor
        public virtual void InitClass()
        {
        }

        private static TimeSpan WarmodeSpamCatch = TimeSpan.FromSeconds(0.5);
        private static TimeSpan WarmodeSpamDelay = TimeSpan.FromSeconds(2.0);
        private const int WarmodeCatchCount = 4; // Allow four warmode changes in 0.5 seconds, any more will be delay for two seconds


        private ArrayList m_ResistMods;

        private int[] m_Resistances;

        public int[] Resistances { get { return m_Resistances; } }

        public virtual int BasePhysicalResistance { get { return 0; } }
        public virtual int BaseFireResistance { get { return 0; } }
        public virtual int BaseColdResistance { get { return 0; } }
        public virtual int BasePoisonResistance { get { return 0; } }
        public virtual int BaseEnergyResistance { get { return 0; } }

        public virtual void ComputeLightLevels(out int global, out int personal)
        {
            ComputeBaseLightLevels(out global, out personal);

            if (m_Region != null)
                m_Region.AlterLightLevel(this, ref global, ref personal);
        }

        public virtual void ComputeBaseLightLevels(out int global, out int personal)
        {
            global = 0;
            personal = m_LightLevel;
        }

        public virtual void CheckLightLevels(bool forceResend)
        {
        }
        /*
				[CommandProperty( AccessLevel.Counselor )]
				public virtual int PhysicalResistance
				{
					get{ return GetResistance( ResistanceType.Physical ); }
				}

				[CommandProperty( AccessLevel.Counselor )]
				public virtual int FireResistance
				{
					get{ return GetResistance( ResistanceType.Fire ); }
				}

				[CommandProperty( AccessLevel.Counselor )]
				public virtual int ColdResistance
				{
					get{ return GetResistance( ResistanceType.Cold ); }
				}

				[CommandProperty( AccessLevel.Counselor )]
				public virtual int PoisonResistance
				{
					get{ return GetResistance( ResistanceType.Poison ); }
				}

				[CommandProperty( AccessLevel.Counselor )]
				public virtual int EnergyResistance
				{
					get{ return GetResistance( ResistanceType.Energy ); }
				}
		*/

        public virtual void UpdateResistances()
        {
            if (m_Resistances == null)
                m_Resistances = new int[5] { int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

            bool delta = false;

            for (int i = 0; i < m_Resistances.Length; ++i)
            {
                if (m_Resistances[i] != int.MinValue)
                {
                    m_Resistances[i] = int.MinValue;
                    delta = true;
                }
            }

            if (delta)
                Delta(MobileDelta.Resistances);
        }

        /*
				public virtual int GetResistance( ResistanceType type )
				{
					if ( m_Resistances == null )
						m_Resistances = new int[5]{ int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

					int v = (int)type;

					if ( v < 0 || v >= m_Resistances.Length )
						return 0;

					int res = m_Resistances[v];

					if ( res == int.MinValue )
					{
						ComputeResistances();
						res = m_Resistances[v];
					}

					return res;
				}
		*/

        public ArrayList ResistanceMods
        {
            get { return m_ResistMods; }
            set { m_ResistMods = value; }
        }

        public virtual void AddResistanceMod(ResistanceMod toAdd)
        {
            if (m_ResistMods == null)
                m_ResistMods = new ArrayList(2);

            m_ResistMods.Add(toAdd);
            UpdateResistances();
        }

        public virtual void RemoveResistanceMod(ResistanceMod toRemove)
        {
            if (m_ResistMods != null)
            {
                m_ResistMods.Remove(toRemove);

                if (m_ResistMods.Count == 0)
                    m_ResistMods = null;
            }

            UpdateResistances();
        }

        private static int m_MaxPlayerResistance = 70;

        public static int MaxPlayerResistance { get { return m_MaxPlayerResistance; } set { m_MaxPlayerResistance = value; } }
        /*
				public virtual void ComputeResistances()
				{
					if ( m_Resistances == null )
						m_Resistances = new int[5]{ int.MinValue, int.MinValue, int.MinValue, int.MinValue, int.MinValue };

					for ( int i = 0; i < m_Resistances.Length; ++i )
						m_Resistances[i] = 0;

					m_Resistances[0] += this.BasePhysicalResistance;
					m_Resistances[1] += this.BaseFireResistance;
					m_Resistances[2] += this.BaseColdResistance;
					m_Resistances[3] += this.BasePoisonResistance;
					m_Resistances[4] += this.BaseEnergyResistance;

					for ( int i = 0; m_ResistMods != null && i < m_ResistMods.Count; ++i )
					{
						ResistanceMod mod = (ResistanceMod)m_ResistMods[i];
						int v = (int)mod.Type;

						if ( v >= 0 && v < m_Resistances.Length )
							m_Resistances[v] += mod.Offset;
					}

					for ( int i = 0; i < m_Items.Count; ++i )
					{
						Item item = (Item)m_Items[i];

						if ( item.CheckPropertyConfliction( this ) )
							continue;

						m_Resistances[0] += item.PhysicalResistance;
						m_Resistances[1] += item.FireResistance;
						m_Resistances[2] += item.ColdResistance;
						m_Resistances[3] += item.PoisonResistance;
						m_Resistances[4] += item.EnergyResistance;
					}

					for ( int i = 0; i < m_Resistances.Length; ++i )
					{
						int min = GetMinResistance( (ResistanceType)i );
						int max = GetMaxResistance( (ResistanceType)i );

						if ( max < min )
							max = min;

						if ( m_Resistances[i] > max )
							m_Resistances[i] = max;
						else if ( m_Resistances[i] < min )
							m_Resistances[i] = min;
					}
				}
		*/
        public virtual int GetMinResistance(ResistanceType type)
        {
            return int.MinValue;
        }

        public virtual int GetMaxResistance(ResistanceType type)
        {
            if (m_Player)
                return m_MaxPlayerResistance;

            return int.MaxValue;
        }

        public virtual void SendPropertiesTo(Mobile from)
        {
            from.Send(PropertyList);
        }

        public virtual void OnAosSingleClick(Mobile from)
        {
            ObjectPropertyList opl = this.PropertyList;

            if (opl.Header > 0)
            {
                int hue;

                if (m_NameHue != -1)
                    hue = m_NameHue;
                else if (m_AccessLevel > AccessLevel.Player)
                    hue = 11;
                else
                    hue = Notoriety.GetHue(Notoriety.Compute(from, this));

                from.Send(new MessageLocalized(m_Serial, Body, MessageType.Label, hue, 3, opl.Header, Name, opl.HeaderArgs));
            }
        }

        public virtual string ApplyNameSuffix(string suffix)
        {
            return suffix;
        }

        public virtual void AddNameProperties(ObjectPropertyList list)
        {
            string name = Name;

            if (name == null)
                name = string.Empty;

            string prefix = "";

            if (ShowFameTitle && (m_Player || m_Body.IsHuman) && m_Fame >= 10000)
                prefix = m_Female ? "Lady" : "Lord";

            string suffix = "";

            if (ClickTitle && Title != null && Title.Length > 0)
                suffix = Title;

            BaseGuild guild = m_Guild;

            if (guild != null && (m_Player || m_DisplayGuildTitle))
            {
                if (suffix.Length > 0)
                    suffix = string.Format("{0} [{1}]", suffix, Utility.FixHtml(guild.Abbreviation));
                else
                    suffix = string.Format("[{0}]", Utility.FixHtml(guild.Abbreviation));
            }

            suffix = ApplyNameSuffix(suffix);

            list.Add(1050045, "{0} \t{1}\t {2}", prefix, name, suffix); // ~1_PREFIX~~2_NAME~~3_SUFFIX~

            if (guild != null && (m_DisplayGuildTitle || (m_Player && guild.ForceGuildTitle)))
            {
                string title = GuildTitle;

                if (title == null)
                    title = "";
                else
                    title = title.Trim();

                if (title.Length > 0)
                    list.Add("{0}, {1} Guild{2}", Utility.FixHtml(title), Utility.FixHtml(guild.Name), guild.GetSuffix(this));
                else
                    list.Add(Utility.FixHtml(guild.Name));
            }
        }

        public virtual void GetProperties(ObjectPropertyList list)
        {
            AddNameProperties(list);
        }

        public virtual void GetChildProperties(ObjectPropertyList list, Item item)
        {
        }

        public virtual void GetChildNameProperties(ObjectPropertyList list, Item item)
        {
        }

        private void UpdateAggrExpire()
        {
            if (m_Deleted || (m_Aggressors.Count == 0 && m_Aggressed.Count == 0))
            {
                StopAggrExpire();
            }
            else if (m_ExpireAggrTimer == null)
            {
                m_ExpireAggrTimer = new ExpireAggressorsTimer(this);
                m_ExpireAggrTimer.Start();
            }
        }

        private void StopAggrExpire()
        {
            if (m_ExpireAggrTimer != null)
                m_ExpireAggrTimer.Stop();

            m_ExpireAggrTimer = null;
        }

        private void CheckAggrExpire()
        {
            for (int i = m_Aggressors.Count - 1; i >= 0; --i)
            {
                if (i >= m_Aggressors.Count)
                    continue;

                AggressorInfo info = (AggressorInfo)m_Aggressors[i];

                if (info.Expired)
                {
                    Mobile attacker = info.Attacker;
                    attacker.RemoveAggressed(this);

                    m_Aggressors.RemoveAt(i);
                    info.Free();

                    if (m_NetState != null && this.CanSee(attacker) && Utility.InUpdateRange(m_Location, attacker.m_Location))
                        //m_NetState.Send(new MobileIncoming(this, attacker));
                        m_NetState.Send(MobileIncoming.Create(m_NetState, this, attacker));
                }
            }

            for (int i = m_Aggressed.Count - 1; i >= 0; --i)
            {
                if (i >= m_Aggressed.Count)
                    continue;

                AggressorInfo info = (AggressorInfo)m_Aggressed[i];

                if (info.Expired)
                {
                    Mobile defender = info.Defender;
                    defender.RemoveAggressor(this);

                    m_Aggressed.RemoveAt(i);
                    info.Free();

                    if (m_NetState != null && this.CanSee(defender) && Utility.InUpdateRange(m_Location, defender.m_Location))
                        //m_NetState.Send(new MobileIncoming(this, defender));
                        m_NetState.Send(MobileIncoming.Create(m_NetState, this, defender));
                }
            }

            UpdateAggrExpire();
        }

        //public ArrayList PetStable { get { return m_PetStable; } set { m_PetStable = value; } }
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public VirtueInfo Virtues { get { return m_Virtues; } set { } }

        public object Party { get { return m_Party; } set { m_Party = value; } }
        public ArrayList SkillMods { get { return m_SkillMods; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int VirtualArmorMod
        {
            get
            {
                return m_VirtualArmorMod;
            }
            set
            {
                if (m_VirtualArmorMod != value)
                {
                    m_VirtualArmorMod = value;

                    Delta(MobileDelta.Armor);
                }
            }
        }

        /// <summary>
        /// Overridable. Virtual event invoked when <paramref name="skill" /> changes in some way.
        /// </summary>
        public virtual void OnSkillInvalidated(Skill skill)
        {
        }

        public virtual void UpdateSkillMods()
        {
            ValidateSkillMods();

            for (int i = 0; i < m_SkillMods.Count; ++i)
            {
                SkillMod mod = (SkillMod)m_SkillMods[i];

                Skill sk = m_Skills[mod.Skill];

                if (sk != null)
                    sk.Update();
            }
        }

        public virtual void ValidateSkillMods()
        {
            for (int i = 0; i < m_SkillMods.Count;)
            {
                SkillMod mod = (SkillMod)m_SkillMods[i];

                if (mod.CheckCondition())
                    ++i;
                else
                    InternalRemoveSkillMod(mod);
            }
        }

        public virtual void AddSkillMod(SkillMod mod)
        {
            if (mod == null)
                return;

            ValidateSkillMods();

            if (!m_SkillMods.Contains(mod))
            {
                m_SkillMods.Add(mod);
                mod.Owner = this;

                Skill sk = m_Skills[mod.Skill];

                if (sk != null)
                    sk.Update();
            }
        }

        public virtual void RemoveSkillMod(SkillMod mod)
        {
            if (mod == null)
                return;

            ValidateSkillMods();

            InternalRemoveSkillMod(mod);
        }

        private void InternalRemoveSkillMod(SkillMod mod)
        {
            if (m_SkillMods.Contains(mod))
            {
                m_SkillMods.Remove(mod);
                mod.Owner = null;

                Skill sk = m_Skills[mod.Skill];

                if (sk != null)
                    sk.Update();
            }
        }

        private class WarmodeTimer : Timer
        {
            private Mobile m_Mobile;
            private bool m_Value;

            public bool Value
            {
                get
                {
                    return m_Value;
                }
                set
                {
                    m_Value = value;
                }
            }

            public WarmodeTimer(Mobile m, bool value)
                : base(WarmodeSpamDelay)
            {
                m_Mobile = m;
                m_Value = value;
            }

            protected override void OnTick()
            {
                m_Mobile.Warmode = m_Value;
                m_Mobile.m_WarmodeChanges = 0;

                m_Mobile.m_WarmodeTimer = null;
            }
        }

        /// <summary>
        /// Overridable. Virtual event invoked when a client, <paramref name="from" />, invokes a 'help request' for the Mobile. Seemingly no longer functional in newer clients.
        /// </summary>
        public virtual void OnHelpRequest(Mobile from)
        {
        }

        public void DelayChangeWarmode(bool value)
        {
            if (m_WarmodeTimer != null)
            {
                m_WarmodeTimer.Value = value;
                return;
            }

            if (m_Warmode == value)
                return;

            DateTime now = DateTime.UtcNow, next = m_NextWarmodeChange;

            if (now > next || m_WarmodeChanges == 0)
            {
                m_WarmodeChanges = 1;
                m_NextWarmodeChange = now + WarmodeSpamCatch;
            }
            else if (m_WarmodeChanges == WarmodeCatchCount)
            {
                m_WarmodeTimer = new WarmodeTimer(this, value);
                m_WarmodeTimer.Start();

                return;
            }
            else
            {
                ++m_WarmodeChanges;
            }

            Warmode = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MeleeDamageAbsorb
        {
            get
            {
                return m_MeleeDamageAbsorb;
            }
            set
            {
                m_MeleeDamageAbsorb = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MagicDamageAbsorb
        {
            get
            {
                return m_MagicDamageAbsorb;
            }
            set
            {
                m_MagicDamageAbsorb = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SkillsTotal
        {
            get
            {
                return m_Skills == null ? 0 : m_Skills.Total;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SkillsCap
        {
            get
            {
                return m_Skills == null ? 0 : m_Skills.Cap;
            }
            set
            {
                if (m_Skills != null)
                    m_Skills.Cap = value;
            }
        }

        public bool InLOS(Mobile target)
        {
            if (m_Deleted || m_Map == null)
                return false;
            else if (target == this || m_AccessLevel > AccessLevel.Player)
                return true;

            return m_Map.LineOfSight(this, target);
        }

        // wea: new check to see if target is audible to another
        public bool IsAudibleTo(Mobile target)
        {
            if (m_Deleted || m_Map == null)
                return false;
            else if (target == this || m_AccessLevel > AccessLevel.Player)
                return true;

            return Utility.LineOfSight(m_Map, this, target, true);
        }

        public bool InLOS(object target)
        {
            if (m_Deleted || m_Map == null)
                return false;
            else if (target == this || m_AccessLevel > AccessLevel.Player)
                return true;
            else if (target is Item && ((Item)target).RootParent == this)
                return true;

            return m_Map.LineOfSight(this, target);
        }

        public bool InLOS(Point3D target)
        {
            if (m_Deleted || m_Map == null)
                return false;
            else if (m_AccessLevel > AccessLevel.Player)
                return true;

            return m_Map.LineOfSight(this, target);
        }
        private byte m_TempRefCount;
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Owner)]
        public byte SpawnerTempRefCount { get { return m_TempRefCount; } set { m_TempRefCount = value; } }
#if DEBUG
        [CommandProperty(AccessLevel.Administrator)]
#endif
        public bool SpawnerTempMob
        {
            get { return GetMobileBool(MobileBoolTable.IsTemplate); }
            set { SetMobileBool(MobileBoolTable.IsTemplate, value); InvalidateProperties(); }
        }

        #region Internal Map Storage 
        // we don't want staff setting this.
        //  To move a mobile to int map storage, use: [MoveToIntMapStorage
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool IsIntMapStorage
        {
            get { return GetMobileBool(MobileBoolTable.IsIntMapStorage); }
            set
            {
                int count = 0;
                SetDeepIntMapStorage(mob: this, value: value, count: ref count);
            }
        }
        private static int SetDeepIntMapStorage(Mobile mob, bool value, ref int count)
        {
            mob.SetMobileBool(MobileBoolTable.IsIntMapStorage, value);

            if (mob.Backpack != null)
                foreach (var check_item in mob.Backpack.GetDeepItems())
                    if (check_item is Item set_item)
                    {
                        set_item.SetItemBool(ItemBoolTable.IsIntMapStorage, value);
                        count++;
                    }

            if (mob.Items != null)
                foreach (var sub_item in mob.Items)
                    if (sub_item is Item set_sub_item)
                    {
                        set_sub_item.SetItemBool(ItemBoolTable.IsIntMapStorage, value);
                        count++;
                    }

            return count;
        }
        #endregion Internal Map Storage 

        [CommandProperty(AccessLevel.GameMaster)]
        public int BaseSoundID
        {
            get
            {
                return m_BaseSoundID;
            }
            set
            {
                m_BaseSoundID = value;
            }
        }

        public DateTime NextCombatTime
        {
            get
            {
                return m_NextCombatTime;
            }
            set
            {
                m_NextCombatTime = value;
            }
        }

        public bool BeginAction(object toLock)
        {
            if (m_Actions == null)
            {
                m_Actions = new ArrayList(2);

                m_Actions.Add(toLock);

                return true;
            }
            else if (!m_Actions.Contains(toLock))
            {
                m_Actions.Add(toLock);

                return true;
            }

            return false;
        }

        public bool CanBeginAction(object toLock)
        {
            return (m_Actions == null || !m_Actions.Contains(toLock));
        }

        public void EndAction(object toLock)
        {
            if (m_Actions != null)
            {
                m_Actions.Remove(toLock);

                if (m_Actions.Count == 0)
                    m_Actions = null;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int NameHue
        {
            get
            {
                return m_NameHue;
            }
            set
            {
                m_NameHue = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Hunger
        {
            get
            {
                return m_Hunger;
            }
            set
            {
                int oldValue = m_Hunger;

                if (oldValue != value)
                {
                    m_Hunger = value;

                    EventSink.InvokeHungerChanged(new HungerChangedEventArgs(this, oldValue));
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Thirst
        {
            get
            {
                return m_Thirst;
            }
            set
            {
                m_Thirst = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int BAC
        {
            get
            {
                return m_BAC;
            }
            set
            {
                m_BAC = value;
            }
        }

        private DateTime m_LastMoveTime;

        /// <summary>
        /// Gets or sets the number of steps this player may take when hidden before being revealed.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int AllowedStealthSteps
        {
            get
            {
                return m_AllowedStealthSteps;
            }
            set
            {
                m_AllowedStealthSteps = value;
            }
        }

        /* Logout:
		 * 
		 * When a client logs into mobile x
		 *  - if ( x is Internalized ) move x to logout location and map
		 * 
		 * When a client attached to a mobile disconnects
		 *  - LogoutTimer is started
		 *	   - Delay is taken from Region.GetLogoutDelay to allow insta-logout regions.
		 *     - OnTick : Location and map are stored, and mobile is internalized
		 * 
		 * Some things to consider:
		 *  - An internalized person getting killed (say, by poison). Where does the body go?
		 *  - Regions now have a GetLogoutDelay( Mobile m ); virtual function (see above)
		 */
        private Point3D m_LogoutLocation;
        private Map m_LogoutMap;

        public virtual TimeSpan GetLogoutDelay()
        {
            return Region.GetLogoutDelay(this);
        }

        private StatLockType m_StrLock, m_DexLock, m_IntLock;

        private Item m_LastHeld;
        /// <summary>
        /// LastHeld persists until the next item 'Holding' In this way, we can determine where the item came from.
        ///     equipping and NPC is a good example of this. Even if they are allowed to equip the NPC, we may want to reject the item
        /// </summary>
        public Item LastHeld
        {
            get
            {
                return m_LastHeld;
            }
        }
        private Item m_Holding;

        public Item Holding
        {
            get
            {
                return m_Holding;
            }
            set
            {
                if (m_Holding != value)
                {
                    if (m_Holding != null)
                    {
                        TotalWeight -= m_Holding.TotalWeight + m_Holding.PileWeight;

                        if (m_Holding.HeldBy == this)
                            m_Holding.HeldBy = null;
                    }

                    if (value != null && m_Holding != null)
                        DropHolding();

                    if (value != null)
                        m_LastHeld = value;

                    m_Holding = value;

                    if (m_Holding != null)
                    {
                        TotalWeight += m_Holding.TotalWeight + m_Holding.PileWeight;

                        if (m_Holding.HeldBy == null)
                            m_Holding.HeldBy = this;
                    }
                }
            }
        }

        public DateTime LastMoveTime
        {
            get
            {
                return m_LastMoveTime;
            }
            set
            {
                m_LastMoveTime = value;
            }
        }

        public void ConvictSuicideBomber(TimeSpan duration)
        {
            if (!GetMobileBool(MobileBoolTable.SuicideBomber))
            {
                SuicideBomber = true;

                m_SuicideBomberTimer = new SuicideBomberTimer(this, duration);
                m_SuicideBomberTimer.Start();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SuicideBomber
        {
            get
            {
                return GetMobileBool(MobileBoolTable.SuicideBomber);
            }
            set
            {
                if (GetMobileBool(MobileBoolTable.SuicideBomber) != value)
                {
                    SetMobileBool(MobileBoolTable.SuicideBomber, value);

                    //this.SendMessage(GetFlag(MobileFlags.SuicideBomber)
                    //? "."
                    //: ".");

                    if (m_SuicideBomberTimer != null)
                    {
                        m_SuicideBomberTimer.Stop();
                        m_SuicideBomberTimer = null;
                    }
                }
            }
        }

        public void Shield(TimeSpan duration)
        {
            if (!GetMobileBool(MobileBoolTable.InvisibleShield))
            {
                InvisibleShield = true;

                m_InvisibleShieldTimer = new InvisibleShieldTimer(this, duration);
                m_InvisibleShieldTimer.Start();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool InvisibleShield
        {
            get
            {
                return GetMobileBool(MobileBoolTable.InvisibleShield);
            }
            set
            {
                if (GetMobileBool(MobileBoolTable.InvisibleShield) != value)
                {
                    SetMobileBool(MobileBoolTable.InvisibleShield, value);

                    this.SendMessage(GetMobileBool(MobileBoolTable.InvisibleShield)
                        ? "You feel the weight of an invisible shield."
                        : "You feel the weight of an invisible shield lifted.");

                    if (m_InvisibleShieldTimer != null)
                    {
                        m_InvisibleShieldTimer.Stop();
                        m_InvisibleShieldTimer = null;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Paralyzed
        {
            get
            {
                return m_Paralyzed;
            }
            set
            {
                if (m_Paralyzed != value)
                {
                    m_Paralyzed = value;

                    this.SendLocalizedMessage(m_Paralyzed ? 502381 : 502382);

                    if (m_ParaTimer != null)
                    {
                        m_ParaTimer.Stop();
                        m_ParaTimer = null;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DisarmReady
        {
            get
            {
                return m_DisarmReady;
            }
            set
            {
                m_DisarmReady = value;
                //SendLocalizedMessage( value ? 1019013 : 1019014 );
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool StunReady
        {
            get
            {
                return m_StunReady;
            }
            set
            {
                m_StunReady = value;
                //SendLocalizedMessage( value ? 1019011 : 1019012 );
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Frozen
        {
            get
            {
                return m_Frozen;
            }
            set
            {
                if (m_Frozen != value)
                {
                    m_Frozen = value;

                    if (m_FrozenTimer != null)
                    {
                        m_FrozenTimer.Stop();
                        m_FrozenTimer = null;
                    }
                }
            }
        }

        public virtual void Paralyze(TimeSpan duration)
        {
            if (!m_Paralyzed)
            {
                Paralyzed = true;

                m_ParaTimer = new ParalyzedTimer(this, duration);
                m_ParaTimer.Start();
            }
        }

        public void Freeze(TimeSpan duration)
        {
            if (!m_Frozen)
            {
                m_Frozen = true;

                m_FrozenTimer = new FrozenTimer(this, duration);
                m_FrozenTimer.Start();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawStr" /> property.
        /// </summary>
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public StatLockType StrLock
        {
            get
            {
                return m_StrLock;
            }
            set
            {
                if (m_StrLock != value)
                {
                    m_StrLock = value;

                    if (m_NetState != null)
                        m_NetState.Send(new StatLockInfo(this));
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawDex" /> property.
        /// </summary>
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public StatLockType DexLock
        {
            get
            {
                return m_DexLock;
            }
            set
            {
                if (m_DexLock != value)
                {
                    m_DexLock = value;

                    if (m_NetState != null)
                        m_NetState.Send(new StatLockInfo(this));
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="StatLockType">lock state</see> for the <see cref="RawInt" /> property.
        /// </summary>
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public StatLockType IntLock
        {
            get
            {
                return m_IntLock;
            }
            set
            {
                if (m_IntLock != value)
                {
                    m_IntLock = value;

                    if (m_NetState != null)
                        m_NetState.Send(new StatLockInfo(this));
                }
            }
        }

        public override string ToString()
        {
            return string.Format("0x{0:X} \"{1}\"", m_Serial.Value, Name);
        }

        public long NextActionTime
        {
            get
            {
                return m_NextActionTime;
            }
            set
            {
                m_NextActionTime = value;
            }
        }

        public DateTime NextActionMessage
        {
            get
            {
                return m_NextActionMessage;
            }
            set
            {
                m_NextActionMessage = value;
            }
        }

        private static TimeSpan m_ActionMessageDelay = TimeSpan.FromSeconds(0.125);

        public static TimeSpan ActionMessageDelay
        {
            get { return m_ActionMessageDelay; }
            set { m_ActionMessageDelay = value; }
        }

        public virtual void SendSkillMessage()
        {
            if (DateTime.UtcNow < m_NextActionMessage)
                return;

            m_NextActionMessage = DateTime.UtcNow + m_ActionMessageDelay;

            SendLocalizedMessage(500118); // You must wait a few moments to use another skill.
        }

        public virtual void SendActionMessage()
        {
            if (DateTime.UtcNow < m_NextActionMessage)
                return;

            m_NextActionMessage = DateTime.UtcNow + m_ActionMessageDelay;

            SendLocalizedMessage(500119); // You must wait to perform another action.
        }

        public virtual void ClearHands()
        {
            ClearHand(FindItemOnLayer(Layer.OneHanded));
            ClearHand(FindItemOnLayer(Layer.TwoHanded));
        }

        public bool MoveToIntStorage()
        {
            return MoveToIntStorage(false);
        }

        public bool MoveToIntStorage(bool PreserveLocation)
        {
            if (Map == null)
                return false;

            IsIntMapStorage = true;
            if (PreserveLocation == true)
                MoveToWorld(Location, Map.Internal);
            else
                Internalize();
            return true;
        }

        public bool RetrieveMobileFromIntStorage(Point3D p, Map m)
        {
            if (Deleted == true || p == Point3D.Zero)
                return false;

            IsIntMapStorage = false;
            MoveToWorld(p, m);
            return true;
        }

        public virtual void ClearHand(Item item)
        {
            if (item != null && item.Movable && !item.AllowEquipedCast(this))
            {
                Container pack = this.Backpack;

                if (pack == null)
                    AddToBackpack(item);
                else
                    pack.DropItem(item);
            }
        }

        private static bool m_GlobalRegenThroughPoison = true;

        public static bool GlobalRegenThroughPoison
        {
            get { return m_GlobalRegenThroughPoison; }
            set { m_GlobalRegenThroughPoison = value; }
        }

        public virtual bool RegenThroughPoison { get { return m_GlobalRegenThroughPoison; } }

        public virtual bool CanRegenHits { get { return this.Alive && (RegenThroughPoison || !this.Poisoned); } }
        public virtual bool CanRegenStam { get { return this.Alive; } }
        public virtual bool CanRegenMana { get { return this.Alive; } }

        private class ManaTimer : Timer
        {
            private Mobile m_Owner;

            public ManaTimer(Mobile m)
                : base(m.ManaRegenRate, m.ManaRegenRate)
            {
                this.Priority = TimerPriority.FiftyMS;
                m_Owner = m;
            }

            protected override void OnTick()
            {
                if (m_Owner.CanRegenMana)// m_Owner.Alive )
                    m_Owner.Mana++;

                Delay = Interval = m_Owner.ManaRegenRate;
            }
        }

        private class HitsTimer : Timer
        {
            private Mobile m_Owner;

            public HitsTimer(Mobile m)
                : base(m.HitsRegenRate, m.HitsRegenRate)
            {
                this.Priority = TimerPriority.FiftyMS;
                m_Owner = m;
            }

            protected override void OnTick()
            {
                if (m_Owner.CanRegenHits)// m_Owner.Alive && !m_Owner.Poisoned )
                    m_Owner.Hits++;

                Delay = Interval = m_Owner.HitsRegenRate;
            }
        }

        private class StamTimer : Timer
        {
            private Mobile m_Owner;

            public StamTimer(Mobile m)
                : base(m.StamRegenRate, m.StamRegenRate)
            {
                this.Priority = TimerPriority.FiftyMS;
                m_Owner = m;
            }

            protected override void OnTick()
            {
                if (m_Owner.CanRegenStam)// m_Owner.Alive )
                    m_Owner.Stam++;

                Delay = Interval = m_Owner.StamRegenRate;
            }
        }

        private class LogoutTimer : Timer
        {
            private Mobile m_Mobile;

            public LogoutTimer(Mobile m)
                : base(TimeSpan.FromDays(1.0))
            {
                Priority = TimerPriority.OneSecond;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                if (m_Mobile.m_Map != Map.Internal)
                {
                    EventSink.InvokeLogout(new LogoutEventArgs(m_Mobile));

                    m_Mobile.m_LogoutLocation = m_Mobile.m_Location;
                    m_Mobile.m_LogoutMap = m_Mobile.m_Map;

                    m_Mobile.Internalize();
                }
            }
        }

        private class SuicideBomberTimer : Timer
        {
            private Mobile m_Mobile;

            public SuicideBomberTimer(Mobile m, TimeSpan duration)
                : base(duration)
            {
                this.Priority = TimerPriority.TwentyFiveMS;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                m_Mobile.SuicideBomber = false;
            }
        }

        private class InvisibleShieldTimer : Timer
        {
            private Mobile m_Mobile;

            public InvisibleShieldTimer(Mobile m, TimeSpan duration)
                : base(duration)
            {
                this.Priority = TimerPriority.TwentyFiveMS;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                m_Mobile.InvisibleShield = false;
            }
        }

        private class ParalyzedTimer : Timer
        {
            private Mobile m_Mobile;

            public ParalyzedTimer(Mobile m, TimeSpan duration)
                : base(duration)
            {
                this.Priority = TimerPriority.TwentyFiveMS;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                m_Mobile.Paralyzed = false;
            }
        }

        private class FrozenTimer : Timer
        {
            private Mobile m_Mobile;

            public FrozenTimer(Mobile m, TimeSpan duration)
                : base(duration)
            {
                this.Priority = TimerPriority.TwentyFiveMS;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                m_Mobile.Frozen = false;
            }
        }

        private class CombatTimer : Timer
        {
            private Mobile m_Mobile;
            public CombatTimer(Mobile m)
                : base(TimeSpan.FromSeconds(0.0), TimeSpan.FromSeconds(0.01), 0)
            {
                m_Mobile = m;

                if (!m_Mobile.m_Player && m_Mobile.m_Dex <= 100)
                    Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                if (DateTime.UtcNow > m_Mobile.m_NextCombatTime)
                {
                    Utility.MobileInfo combatantInfo = Utility.GetMobileInfo(m_Mobile, null, new Utility.MobileInfo(m_Mobile.Combatant));

                    // If no combatant, wrong map, one of us is a ghost, or cannot see, or deleted, then stop combat
                    if (m_Mobile.Dead || m_Mobile.Deleted || m_Mobile.IsDeadBondedPet || combatantInfo.unavailable)
                    {
                        m_Mobile.Combatant = null;
                        return;
                    }

                    IWeapon weapon = m_Mobile.Weapon;

                    if (!m_Mobile.InRange(combatantInfo.target, weapon.MaxRange))
                        return;

                    if (m_Mobile.InLOS(combatantInfo.target))
                    {
                        m_Mobile.RevealingAction();
                        m_Mobile.m_NextCombatTime = DateTime.UtcNow + weapon.OnSwing(m_Mobile, combatantInfo.target);

                        // Special: Adam Ant defends himself against all comers
                        if (combatantInfo.target.Player && combatantInfo.target.AccessLevel == AccessLevel.Owner && combatantInfo.target.Hidden == false)
                            combatantInfo.target.Combatant = m_Mobile;
                    }
                }
            }
        }

        private class ExpireCombatantTimer : Timer
        {
            private Mobile m_Mobile;

            public ExpireCombatantTimer(Mobile m)
                : base(TimeSpan.FromMinutes(1.0))
            {
                this.Priority = TimerPriority.FiveSeconds;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                m_Mobile.Combatant = null;
            }
        }

        private static TimeSpan m_ExpireCriminalDelay = TimeSpan.FromMinutes(2.0);

        public static TimeSpan ExpireCriminalDelay
        {
            get { return m_ExpireCriminalDelay; }
        }

        private class ExpireCriminalTimer : Timer
        {
            private Mobile m_Mobile;

            public ExpireCriminalTimer(Mobile m)
                : this(m, m_ExpireCriminalDelay)
            {
            }

            public ExpireCriminalTimer(Mobile m, TimeSpan howLong)
                : base(howLong)
            {
                this.Priority = TimerPriority.FiveSeconds;
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                m_Mobile.Criminal = false;
            }
        }

        private class ExpireAggressorsTimer : Timer
        {
            private Mobile m_Mobile;

            public ExpireAggressorsTimer(Mobile m)
                : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
            {
                m_Mobile = m;
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                if (m_Mobile.Deleted || (m_Mobile.Aggressors.Count == 0 && m_Mobile.Aggressed.Count == 0))
                    m_Mobile.StopAggrExpire();
                else
                    m_Mobile.CheckAggrExpire();
            }
        }

        private DateTime m_NextCombatTime;

        public long NextSkillTime
        {
            get
            {
                return m_NextSkillTime;
            }
            set
            {
                m_NextSkillTime = value;
            }
        }

        public List<AggressorInfo> Aggressors
        {
            get
            {
                return m_Aggressors;
            }
        }

        public List<AggressorInfo> Aggressed
        {
            get
            {
                return m_Aggressed;
            }
        }

        private int m_ChangingCombatant;

        public bool ChangingCombatant
        {
            get { return (m_ChangingCombatant > 0); }
        }

        public virtual void Attack(Mobile m)
        {
            if (CheckAttack(m))
                Combatant = m;
        }

        public virtual bool CheckAttack(Mobile m)
        {
            return (Utility.InUpdateRange(this, m) && CanSee(m) && InLOS(m));
        }

        /// <summary>
        /// Overridable. Gets or sets which Mobile that this Mobile is currently engaged in combat with.
        /// <seealso cref="OnCombatantChange" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Mobile Combatant
        {
            get
            {
                return m_Combatant;
            }
            set

            {
                if (m_Deleted)
                    return;

                if (m_Combatant != value && value != this)
                {
                    Mobile old = m_Combatant;

                    ++m_ChangingCombatant;
                    m_Combatant = value;

                    if ((m_Combatant != null && !CanBeHarmful(m_Combatant, false)) || !Region.OnCombatantChange(this, old, m_Combatant))
                    {
                        m_Combatant = old;
                        --m_ChangingCombatant;
                        return;
                    }

                    if (m_NetState != null)
                        m_NetState.Send(new ChangeCombatant(m_Combatant));

                    if (m_Combatant == null)
                    {
                        if (m_ExpireCombatant != null)
                            m_ExpireCombatant.Stop();

                        if (m_CombatTimer != null)
                            m_CombatTimer.Stop();

                        m_ExpireCombatant = null;
                        m_CombatTimer = null;
                    }
                    else
                    {
                        if (m_ExpireCombatant == null)
                            m_ExpireCombatant = new ExpireCombatantTimer(this);

                        m_ExpireCombatant.Start();

                        if (m_CombatTimer == null)
                            m_CombatTimer = new CombatTimer(this);

                        m_CombatTimer.Start();
                    }

                    if (m_Combatant != null && CanBeHarmful(m_Combatant, false))
                    {
                        DoHarmful(m_Combatant);

                        if (m_Combatant != null)
                            m_Combatant.PlaySound(m_Combatant.GetAngerSound());
                    }

                    // currently we only monitor combatant set to null
                    if (m_Combatant == null)
                        if (CombatantChangeMonitor.Contains(this.Serial))
                        {
#if DEBUG
                            Debugger.Break(); // debug break;
#endif
                        }

                    OnCombatantChange();
                    --m_ChangingCombatant;
                }
            }
        }
        public List<Serial> CombatantChangeMonitor = new(0);
        /// <summary>
        /// Overridable. Virtual event invoked after the <see cref="Combatant" /> property has changed.
        /// <seealso cref="Combatant" />
        /// </summary>
        public virtual void OnCombatantChange()
        {
        }

        // is my combatant is attacking me??
        public bool IsCombatantAnAggressor()
        {
            if (Combatant != null)
            {
                if (Combatant.Combatant == this)
                {
                    return true;
                }
            }
            return false;
        }
        public virtual bool IsAggressor(Mobile m, bool player = false)
        {
            // was this guy an Aggressor?
            List<AggressorInfo> list = this.Aggressors;
            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo ai = (AggressorInfo)list[i];
                if (m == ai.Attacker && ai.Expired == false && (player ? ai.Attacker.Player : true))
                    return true;
            }

            return false;
        }

        // called from BaseAI when an NPC 'sees' a mobile while 'LookingAround'
        // called in OnMovement when a player is spotted moving
        public virtual void OnSee(Mobile m)
        {
        }

        public double GetDistanceToSqrt(Point3D p)
        {
            int xDelta = m_Location.m_X - p.m_X;
            int yDelta = m_Location.m_Y - p.m_Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        public double GetDistanceToSqrt(Mobile m)
        {
            int xDelta = m_Location.m_X - m.m_Location.m_X;
            int yDelta = m_Location.m_Y - m.m_Location.m_Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        public double GetDistanceToSqrt(IPoint2D p)
        {
            int xDelta = m_Location.m_X - p.X;
            int yDelta = m_Location.m_Y - p.Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        public virtual void AggressiveAction(Mobile aggressor)
        {
            AggressiveAction(aggressor, false);
        }

        public virtual void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            if (aggressor == this)
                return;

            AggressiveActionEventArgs args = AggressiveActionEventArgs.Create(this, aggressor, criminal);

            EventSink.InvokeAggressiveAction(args);

            args.Free();

            if (Combatant == aggressor)
            {
                if (m_ExpireCombatant == null)
                    m_ExpireCombatant = new ExpireCombatantTimer(this);
                else
                    m_ExpireCombatant.Stop();

                m_ExpireCombatant.Start();
            }

            bool addAggressor = true;

            List<AggressorInfo> list = m_Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker == aggressor)
                {
                    info.Refresh();
                    info.CriminalAggression = criminal;
                    info.CanReportMurder = ReportableForMurder(aggressor, criminal);
                    addAggressor = false;
                    break;                  // Adam: exit when you know your answer
                }
            }

            list = aggressor.m_Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker == this)
                {
                    info.Refresh();
                    addAggressor = false;
                    break;                  // Adam: exit when you know your answer
                }
            }

            bool addAggressed = true;

            list = m_Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Defender == aggressor)
                {
                    info.Refresh();
                    addAggressed = false;
                    break;                  // Adam: exit when you know your answer
                }
            }

            list = aggressor.m_Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = list[i];

                if (info.Defender == this)
                {
                    info.Refresh();
                    info.CriminalAggression = criminal;
                    info.CanReportMurder = ReportableForMurder(aggressor, criminal);
                    addAggressed = false;
                    break;                  // Adam: exit when you know your answer
                }
            }

            bool setCombatant = false;

            if (addAggressor)
            {
                m_Aggressors.Add(AggressorInfo.Create(aggressor, this, criminal)); // new AggressorInfo( aggressor, this, criminal, true ) );

                if (this.CanSee(aggressor) && m_NetState != null)
                    //m_NetState.Send(new MobileIncoming(this, aggressor));
                    m_NetState.Send(MobileIncoming.Create(m_NetState, this, aggressor));

                if (Combatant == null)
                    setCombatant = true;

                UpdateAggrExpire();
            }

            if (addAggressed)
            {
                aggressor.m_Aggressed.Add(AggressorInfo.Create(aggressor, this, criminal)); // new AggressorInfo( aggressor, this, criminal, false ) );

                if (this.CanSee(aggressor) && m_NetState != null)
                    //m_NetState.Send(new MobileIncoming(this, aggressor));
                    m_NetState.Send(MobileIncoming.Create(m_NetState, this, aggressor));

                if (Combatant == null)
                    setCombatant = true;

                UpdateAggrExpire();
            }

            if (setCombatant && AcceptingCombatantChange())
                Combatant = aggressor;

            Region.OnAggressed(aggressor, this, criminal);
        }
        public virtual bool AcceptingCombatantChange()
        {
            return true;
        }
        public void RemoveAggressed(Mobile aggressed)
        {
            if (m_Deleted)
                return;

            List<AggressorInfo> list = m_Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Defender == aggressed)
                {
                    m_Aggressed.RemoveAt(i);
                    info.Free();

                    if (m_NetState != null && this.CanSee(aggressed))
                        //m_NetState.Send(new MobileIncoming(this, aggressed));
                        m_NetState.Send(MobileIncoming.Create(m_NetState, this, aggressed));

                    break;
                }
            }

            UpdateAggrExpire();
        }

        public bool CheckAggressed(Mobile aggressed)
        {
            if (m_Deleted)
                return false;

            List<AggressorInfo> list = m_Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Defender == aggressed)
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveAggressor(Mobile aggressor)
        {
            if (m_Deleted)
                return;

            List<AggressorInfo> list = m_Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker == aggressor)
                {
                    m_Aggressors.RemoveAt(i);
                    info.Free();

                    if (m_NetState != null && this.CanSee(aggressor))
                        //m_NetState.Send(new MobileIncoming(this, aggressor));
                        m_NetState.Send(MobileIncoming.Create(m_NetState, this, aggressor));

                    break;
                }
            }

            UpdateAggrExpire();
        }

        public bool CheckAggressor(Mobile aggressor)
        {
            if (m_Deleted)
                return false;

            List<AggressorInfo> list = m_Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker == aggressor)
                {
                    return true;
                }
            }

            return false;
        }

        public virtual bool ReportableForMurder(Mobile aggressor, bool criminal)
        {
            return criminal;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalGold
        {
            get
            {
                return m_TotalGold;
            }
            set
            {
                if (m_TotalGold != value)
                {
                    m_TotalGold = value;

                    Delta(MobileDelta.Gold);
                }
            }
        }
        // TODO needs to be serialized
        private int m_TithingPoints = 0;
        //[CommandProperty(AccessLevel.GameMaster)]
        public int TithingPoints
        {
            get
            {
                return m_TithingPoints;
            }
            set
            {
                if (m_TithingPoints != value)
                {
                    m_TithingPoints = value;

                    Delta(MobileDelta.TithingPoints);
                }
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int FollowerCount
        {
            get
            {
                return m_FollowerCount;
            }
            set
            {
                if (m_FollowerCount != value)
                {
                    m_FollowerCount = value;

                    Delta(MobileDelta.Followers);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FollowersMax
        {
            get
            {
                return m_FollowersMax;
            }
            set
            {
                if (m_FollowersMax != value)
                {
                    m_FollowersMax = value;

                    Delta(MobileDelta.Followers);
                }
            }
        }

        public virtual void UpdateTotals()
        {
            if (m_Items == null)
                return;

            int oldValue = m_TotalWeight;

            m_TotalGold = 0;
            m_TotalWeight = 0;

            for (int i = 0; i < m_Items.Count; ++i)
            {
                Item item = (Item)m_Items[i];

                item.UpdateTotals();

                if (!(item is BankBox))
                {
                    m_TotalGold += item.TotalGold;
                    m_TotalWeight += item.TotalWeight + item.PileWeight;
                }
            }

            if (m_Holding != null)
                m_TotalWeight += m_Holding.TotalWeight + m_Holding.PileWeight;

            OnWeightChange(oldValue);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalWeight
        {
            get
            {
                return m_TotalWeight;
            }
            set
            {
                int oldValue = m_TotalWeight;

                if (oldValue != value)
                {
                    m_TotalWeight = value;

                    Delta(MobileDelta.Weight);

                    OnWeightChange(oldValue);
                }
            }
        }

        public void ClearQuestArrow()
        {
            m_QuestArrow = null;
        }

        public void ClearTarget()
        {
            m_Target = null;
        }

        private bool m_TargetLocked;

        public bool TargetLocked
        {
            get
            {
                return m_TargetLocked;
            }
            set
            {
                m_TargetLocked = value;
            }
        }

        private class SimpleTarget : Target
        {
            private TargetCallback m_Callback;

            public SimpleTarget(int range, TargetFlags flags, bool allowGround, TargetCallback callback)
                : base(range, allowGround, flags)
            {
                m_Callback = callback;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Callback != null)
                    m_Callback(from, targeted);
            }
        }

        public Target BeginTarget(int range, bool allowGround, TargetFlags flags, TargetCallback callback)
        {
            Target t = new SimpleTarget(range, flags, allowGround, callback);

            this.Target = t;

            return t;
        }

        private class SimpleStateTarget : Target
        {
            private TargetStateCallback m_Callback;
            private object m_State;

            public SimpleStateTarget(int range, TargetFlags flags, bool allowGround, TargetStateCallback callback, object state)
                : base(range, allowGround, flags)
            {
                m_Callback = callback;
                m_State = state;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Callback != null)
                    m_Callback(from, targeted, m_State);
            }
        }

        public Target BeginTarget(int range, bool allowGround, TargetFlags flags, TargetStateCallback callback, object state)
        {
            Target t = new SimpleStateTarget(range, flags, allowGround, callback, state);

            this.Target = t;

            return t;
        }

        public Target Target
        {
            get
            {
                return m_Target;
            }
            set
            {
                Target oldTarget = m_Target;
                Target newTarget = value;

                if (oldTarget == newTarget)
                    return;

                m_Target = null;

                if (oldTarget != null && newTarget != null)
                    oldTarget.Cancel(this, TargetCancelType.Overriden);

                m_Target = newTarget;

                if (newTarget != null && m_NetState != null && !m_TargetLocked)
                    m_NetState.Send(newTarget.GetPacket());

                OnTargetChange();

                /*if ( m_Target != value )
				{
					if ( m_Target != null && value != null )
						m_Target.Cancel( this, TargetCancelType.Overriden );

					m_Target = value;

					if ( m_Target != null && m_NetState != null && !m_TargetLocked )
						m_NetState.Send( m_Target.GetPacket() );
					//m_NetState.Send( new TargetReq( m_Target ) );

					OnTargetChange();
				}*/
            }
        }

        /// <summary>
        /// Overridable. Virtual event invoked after the <see cref="Target">Target property</see> has changed.
        /// </summary>
        protected virtual void OnTargetChange()
        {
        }

        public ContextMenu ContextMenu
        {
            get
            {
                return m_ContextMenu;
            }
            set
            {
                m_ContextMenu = value;

                if (m_ContextMenu != null && m_NetState != null)
                {
                    // Old packet is preferred until assistants catch up
                    if (m_NetState.NewHaven && m_ContextMenu.RequiresNewPacket)
                        Send(new DisplayContextMenu(m_ContextMenu));
                    else
                        Send(new DisplayContextMenuOld(m_ContextMenu));
                }
            }
        }

        public virtual bool CheckContextMenuDisplay(IEntity target)
        {
            return true;
        }

        public Prompt Prompt
        {
            get
            {
                return m_Prompt;
            }
            set
            {
                Prompt oldPrompt = m_Prompt;
                Prompt newPrompt = value;

                if (oldPrompt == newPrompt)
                    return;

                m_Prompt = null;

                if (oldPrompt != null && newPrompt != null)
                    oldPrompt.OnCancel(this);

                m_Prompt = newPrompt;

                if (newPrompt != null)
                    Send(new UnicodePrompt(newPrompt));
            }
        }

        private bool InternalOnMove(Direction d)
        {
            if (!OnMove(d))
                return false;

            MovementEventArgs e = MovementEventArgs.Create(this, d);

            EventSink.InvokeMovement(e);

            bool ret = !e.Blocked;

            e.Free();

            return ret;
        }

        /// <summary>
        /// Overridable. Event invoked before the Mobile <see cref="Move">moves</see>.
        /// </summary>
        /// <returns>True if the move is allowed, false if not.</returns>
        protected virtual bool OnMove(Direction d)
        {
            if (m_Hidden && m_AccessLevel == AccessLevel.Player)
            {
                if (AllowedStealthSteps-- <= 0 || (d & Direction.Running) != 0 || this.Mounted)
                    RevealingAction();
            }

            return true;
        }

        //private static MobileMoving[] m_MovingPacketCache = new MobileMoving[8];
        private static Packet[] m_MovingPacketCache = new Packet[8];

        private bool m_Pushing;

        public bool Pushing
        {
            get
            {
                return m_Pushing;
            }
            set
            {
                m_Pushing = value;
            }
        }

        #region RUN_WALK_SPEED
        // changed from static so we can slow players dynamically like for instance, the flag carrier in capture the flag.
        // not serialized
        private TimeSpan m_WalkFoot = TimeSpan.FromSeconds(0.4);
        private TimeSpan m_RunFoot = TimeSpan.FromSeconds(0.2);
        private TimeSpan m_WalkMount = TimeSpan.FromSeconds(0.2);
        private TimeSpan m_RunMount = TimeSpan.FromSeconds(0.1);

        // so we can see what's going on
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Administrator)]
        public TimeSpan SpeedWalkFoot { get { return m_WalkFoot; } set { m_WalkFoot = value; } }
        [CommandProperty(AccessLevel.Administrator, AccessLevel.Administrator)]
        public TimeSpan SpeedRunFoot { get { return m_RunFoot; } set { m_RunFoot = value; } }

        public TimeSpan SpeedWalkMount { get { return m_WalkMount; } set { m_WalkMount = value; } }
        public TimeSpan SpeedRunMount { get { return m_RunMount; } set { m_RunMount = value; } }
        #endregion RUN_WALK_SPEED

        private DateTime m_EndQueue;

        private static ArrayList m_MoveList = new ArrayList();

        private static AccessLevel m_FwdAccessOverride = AccessLevel.GameMaster;
        private static bool m_FwdEnabled = true;
        private static bool m_FwdUOTDOverride = false;
        private static int m_FwdMaxStepsMount = 4;
        private static int m_FwdMaxStepsFoot = 2;

        public static AccessLevel FwdAccessOverride { get { return m_FwdAccessOverride; } set { m_FwdAccessOverride = value; } }
        public static bool FwdEnabled { get { return m_FwdEnabled; } set { m_FwdEnabled = value; } }
        public static bool FwdUOTDOverride { get { return m_FwdUOTDOverride; } set { m_FwdUOTDOverride = value; } }
        public static int FwdMaxStepsMount { get { return m_FwdMaxStepsMount; } set { m_FwdMaxStepsMount = value; } }
        public static int FwdMaxStepsFoot { get { return m_FwdMaxStepsFoot; } set { m_FwdMaxStepsFoot = value; } }

        public virtual void ClearFastwalkStack()
        {
            if (m_MoveRecords != null && m_MoveRecords.Count > 0)
                m_MoveRecords.Clear();

            m_EndQueue = DateTime.UtcNow;
        }
        public virtual bool CheckMovement(Direction d)
        {
            int newZ;
            return CheckMovement(d, out newZ);
        }
        public virtual bool CheckMovement(Direction d, out int newZ)
        {
#if false
            // 1/3/21, Adam: Smooth Doors hack mitigation - still working on the correct fix
            if (IsSmoothDoorExploit(d))
            {
                newZ = 0;
                return false;
            }
#endif
            // dunno if 'Zero' is the right answer.
            return Movement.Movement.CheckMovement(new Movement.MovementObject(this, this.Location), d, out newZ);
        }
#if false
        private bool IsSmoothDoorExploit(Direction d)
        {
            // first we want to calculate new and old positions.
            //  where they are/were and where that are headed.
            Point3D newLocation = m_Location;
            Point3D oldLocation = newLocation;
            int newZ;
            if (Movement.Movement.CheckMovement(new Movement.MovementObject(this, this.Location), d, out newZ))
            {
                int x = oldLocation.m_X, y = oldLocation.m_Y;
                int oldX = x, oldY = y;
                int oldZ = oldLocation.m_Z;
                switch (d & Direction.Mask)
                {
                    case Direction.North:
                        --y;
                        break;
                    case Direction.Right:
                        ++x;
                        --y;
                        break;
                    case Direction.East:
                        ++x;
                        break;
                    case Direction.Down:
                        ++x;
                        ++y;
                        break;
                    case Direction.South:
                        ++y;
                        break;
                    case Direction.Left:
                        --x;
                        ++y;
                        break;
                    case Direction.West:
                        --x;
                        break;
                    case Direction.Up:
                        --x;
                        --y;
                        break;
                }

                newLocation.m_X = x;
                newLocation.m_Y = y;
                newLocation.m_Z = newZ;

                // new location needs to be in the house
                if (!InHouse(newLocation))
                    return false;

                // one step to the right would put you out of the house
                //  this test ensures we are on that SE porch tile
                if (InHouse(new Point3D(oldLocation.X + 1, oldLocation.Y, this.Z)))
                    return false;

                // we need to be traveling up (diagonally)
                if (d != Direction.Up /*Mask*/)
                    return false;

                //Utility.ConsoleOut("Old Location: {0}", ConsoleColor.Red, oldLocation);
                //Utility.ConsoleOut("New Location: {0}", ConsoleColor.Red, newLocation);

                // is there an open door smack dab in front of us?
                foreach (Item item in GetItemsInRange(2))
                {
                    if (item is BaseDoor bd)
                    {
                        //Utility.ConsoleOut("Door is : {0}", ConsoleColor.Red, bd.Open ? "open" : "closed");
                        //Utility.ConsoleOut("Direction to Door is : {0}", ConsoleColor.Red, GetDirectionTo(item).ToString());
                        //Utility.ConsoleOut("old GetDistanceToSqrt to Door is : {0}", ConsoleColor.Red, Utility.GetDistanceToSqrt(oldLocation, bd.Location));
                        //Utility.ConsoleOut("new GetDistanceToSqrt to Door is : {0}", ConsoleColor.Red, Utility.GetDistanceToSqrt(newLocation, bd.Location));

                        if (bd.Open && GetDirectionTo(bd) == Direction.North)
                        {
                            //Utility.ConsoleOut("Blocking!", ConsoleColor.Red);
                            Diagnostics.LogHelper logger = new Diagnostics.LogHelper("SmoothDoorExploit.log", false, true);
                            logger.Log(string.Format("Blocked {0} from gaining access to house at {1}.", this, bd.Location));
                            logger.Finish();
                            return true;
                        }
                    }
                }
            }

            //Utility.ConsoleOut("Traveling: {0}", ConsoleColor.Red, d.ToString());
            return false;
        }

        private bool InHouse(Point3D location)
        {
            if (Server.Multis.BaseHouse.FindHouseAt(location, this.Map, 16) != null)
                return true;
            else
                return false;
        }
#endif
        //private int m_FastWalkCount = 0;
        public virtual bool Move(Direction d)
        {
            if (m_Deleted)
                return false;

            BankBox box = FindBankNoCreate();

            if (box != null && box.Opened)
                box.Close();

            Point3D newLocation = m_Location;
            Point3D oldLocation = newLocation;

            if ((m_Direction & Direction.Mask) == (d & Direction.Mask))
            {
                // We are actually moving (not just a direction change)

                if (m_Spell != null && !m_Spell.OnCasterMoving(d))
                    return false;

                if (m_Paralyzed || m_Frozen)
                {
                    SendLocalizedMessage(500111); // You are frozen and can not move.

                    return false;
                }

                int newZ;

                if (CheckMovement(d, out newZ))
                {
                    int x = oldLocation.m_X, y = oldLocation.m_Y;
                    int oldX = x, oldY = y;
                    int oldZ = oldLocation.m_Z;

                    switch (d & Direction.Mask)
                    {
                        case Direction.North:
                            --y;
                            break;
                        case Direction.Right:
                            ++x;
                            --y;
                            break;
                        case Direction.East:
                            ++x;
                            break;
                        case Direction.Down:
                            ++x;
                            ++y;
                            break;
                        case Direction.South:
                            ++y;
                            break;
                        case Direction.Left:
                            --x;
                            ++y;
                            break;
                        case Direction.West:
                            --x;
                            break;
                        case Direction.Up:
                            --x;
                            --y;
                            break;
                    }

                    newLocation.m_X = x;
                    newLocation.m_Y = y;
                    newLocation.m_Z = newZ;

                    m_Pushing = false;

                    Map map = m_Map;

                    if (map != null)
                    {
                        Sector oldSector = map.GetSector(oldX, oldY);
                        Sector newSector = map.GetSector(x, y);
                        ArrayList OnMoveOff = new ArrayList();
                        ArrayList OnMoveOver = new ArrayList();

                        if (oldSector != newSector)
                        {
                            foreach (Mobile m in oldSector.Mobiles)
                            {
                                if (m == null)
                                    continue;

                                if (m != this && m.X == oldX && m.Y == oldY && (m.Z + 15) > oldZ && (oldZ + 15) > m.Z)
                                    OnMoveOff.Add(m);
                            }

                            foreach (Mobile m in OnMoveOff)
                                if (!m.OnMoveOff(this))
                                    return false;
                            OnMoveOff.Clear();

                            foreach (Item item in oldSector.Items)
                            {
                                if (item == null)
                                    continue;

                                if (item.AtWorldPoint(oldX, oldY) && (item.Z == oldZ || ((item.Z + item.ItemData.Height) > oldZ && (oldZ + 15) > item.Z)))
                                    OnMoveOff.Add(item);
                            }

                            foreach (Item item in OnMoveOff)
                                if (!item.OnMoveOff(this))
                                    return false;

                            foreach (Mobile m in newSector.Mobiles)
                            {
                                if (m == null)
                                    continue;

                                if (m.X == x && m.Y == y && (m.Z + 15) > newZ && (newZ + 15) > m.Z)
                                    OnMoveOver.Add(m);
                            }

                            foreach (Mobile m in OnMoveOver)
                                if (!m.OnMoveOver(this))
                                    return false;
                            OnMoveOver.Clear();

                            foreach (Item item in newSector.Items)
                            {
                                if (item == null)
                                    continue;

                                if (item.AtWorldPoint(x, y) && (item.Z == newZ || ((item.Z + item.ItemData.Height) > newZ && (newZ + 15) > item.Z)))
                                    OnMoveOver.Add(item);
                            }

                            foreach (Item item in OnMoveOver)
                                if (!item.OnMoveOver(this))
                                    return false;

                            for (int ix = 0, jx = 0; true; ix++, jx++)
                            {
                                if (ix < OnMoveOff.Count)
                                    if (!(OnMoveOff[ix] as Item).OnMoveOff(this))
                                        return false;

                                if (jx < OnMoveOver.Count)
                                    if (!(OnMoveOver[jx] as Item).OnMoveOver(this))
                                        return false;

                                if (ix >= OnMoveOff.Count && jx >= OnMoveOver.Count)
                                    break;
                            }
                            OnMoveOver.Clear();
                            OnMoveOff.Clear(); // test
                        }
                        else
                        {

                            foreach (Mobile m in oldSector.Mobiles)
                            {
                                if (m == null)
                                    continue;

                                if (m != this && m.X == oldX && m.Y == oldY && (m.Z + 15) > oldZ && (oldZ + 15) > m.Z)
                                    OnMoveOff.Add(m);
                                else if (m.X == x && m.Y == y && (m.Z + 15) > newZ && (newZ + 15) > m.Z)
                                    OnMoveOver.Add(m);
                            }

                            for (int ix = 0, jx = 0; true; ix++, jx++)
                            {
                                if (ix < OnMoveOff.Count)
                                    if (!(OnMoveOff[ix] as Mobile).OnMoveOff(this))
                                        return false;

                                if (jx < OnMoveOver.Count)
                                    if (!(OnMoveOver[jx] as Mobile).OnMoveOver(this))
                                        return false;

                                if (ix >= OnMoveOff.Count && jx >= OnMoveOver.Count)
                                    break;
                            }
                            OnMoveOver.Clear();
                            OnMoveOff.Clear();

                            foreach (Item item in oldSector.Items)
                            {
                                if (item == null)
                                    continue;

                                if (item.AtWorldPoint(oldX, oldY) && (item.Z == oldZ || ((item.Z + item.ItemData.Height) > oldZ && (oldZ + 15) > item.Z)))
                                    OnMoveOff.Add(item);
                                else if (item.AtWorldPoint(x, y) && (item.Z == newZ || ((item.Z + item.ItemData.Height) > newZ && (newZ + 15) > item.Z)))
                                    OnMoveOver.Add(item);
                            }

                            for (int ix = 0, jx = 0; true; ix++, jx++)
                            {
                                if (ix < OnMoveOff.Count)
                                    if (!(OnMoveOff[ix] as Item).OnMoveOff(this))
                                        return false;

                                if (jx < OnMoveOver.Count)
                                    if (!(OnMoveOver[jx] as Item).OnMoveOver(this))
                                        return false;

                                if (ix >= OnMoveOff.Count && jx >= OnMoveOver.Count)
                                    break;
                            }
                            OnMoveOver.Clear();
                            OnMoveOff.Clear();
                        }

                        //if( !Region.CanMove( this, d, newLocation, oldLocation, m_Map ) )
                        //	return false;
                    }
                    else
                    {
                        return false;
                    }

                    if (!InternalOnMove(d))
                        return false;

                    if (m_FwdEnabled && m_NetState != null && m_AccessLevel < m_FwdAccessOverride && (!m_FwdUOTDOverride || !m_NetState.IsUOTDClient))
                    {
                        if (m_MoveRecords == null)
                            m_MoveRecords = new Queue<MovementRecord>(6);

                        while (m_MoveRecords.Count > 0)
                        {
                            MovementRecord r = m_MoveRecords.Peek();

                            if (r.Expired())
                                m_MoveRecords.Dequeue();
                            else
                                break;
                        }

                        int fwdMaxSteps;

                        if (Mounted)
                            fwdMaxSteps = m_FwdMaxStepsMount;
                        else
                            fwdMaxSteps = m_FwdMaxStepsFoot;

                        if (m_MoveRecords.Count >= fwdMaxSteps)
                        {
                            FastWalkEventArgs fw = new FastWalkEventArgs(m_NetState);
                            EventSink.InvokeFastWalk(fw);

                            if (fw.Blocked)
                                return false;
                        }

                        TimeSpan delay = ComputeMovementSpeed(d);

                        /*if ( Mounted )
							delay = (d & Direction.Running) != 0 ? m_RunMount : m_WalkMount;
						else
							delay = ( d & Direction.Running ) != 0 ? m_RunFoot : m_WalkFoot;*/

                        DateTime end;

                        if (m_MoveRecords.Count > 0)
                            end = m_EndQueue + delay;
                        else
                            end = DateTime.UtcNow + delay;

                        m_MoveRecords.Enqueue(MovementRecord.NewInstance(end));

                        m_EndQueue = end;
                    }

                    m_LastMoveTime = DateTime.UtcNow;
                }
                else
                {
                    return false;
                }

                DisruptiveAction();
            }

            if (m_NetState != null)
                m_NetState.Send(MovementAck.Instantiate(m_NetState.Sequence, this));//new MovementAck( m_NetState.Sequence, this ) );

            SetLocation(newLocation, false);
            SetDirection(d);

            if (m_Map != null)
            {
                IPooledEnumerable eable = m_Map.GetObjectsInRange(m_Location, Core.GlobalMaxUpdateRange);

                foreach (object o in eable)
                {
                    if (o == this)
                        continue;

                    if (o is Mobile)
                    {
                        m_MoveList.Add(o);
                    }
                    else if (o is Item)
                    {
                        Item item = (Item)o;

                        if (item.HandlesOnMovement)
                            m_MoveList.Add(item);
                    }
                }

                eable.Free();

                Packet[] cache = m_MovingPacketCache;

                for (int i = 0; i < cache.Length; ++i)
                    Packet.Release(ref cache[i]);

                for (int i = 0; i < m_MoveList.Count; ++i)
                {
                    object o = m_MoveList[i];

                    if (o is Mobile)
                    {
                        Mobile m = (Mobile)m_MoveList[i];
                        NetState ns = m.NetState;

                        if (ns != null && Utility.InUpdateRange(m_Location, m.m_Location) && m.CanSee(this))
                        {
                            int noto = Notoriety.Compute(m, this);
                            Packet p = cache[noto];

                            if (p == null)
                                cache[noto] = p = Packet.Acquire(new MobileMoving(this, noto));

                            ns.Send(p);
                        }

                        m.OnMovement(this, oldLocation);

                        EventSink.InvokeMovementObserved(new MovementObservedEventArgs(m, this, oldLocation));
                    }
                    else if (o is Item)
                    {
                        ((Item)o).OnMovement(this, oldLocation);
                    }
                }

                for (int i = 0; i < cache.Length; ++i)
                    Packet.Release(ref cache[i]);

                if (m_MoveList.Count > 0)
                    m_MoveList.Clear();
            }

            OnAfterMove(oldLocation);
            return true;
        }
        public virtual void OnAfterMove(Point3D oldLocation)
        {
        }

        public TimeSpan ComputeMovementSpeed()
        {
            return ComputeMovementSpeed(this.Direction, false);
        }
        public virtual TimeSpan ComputeMovementSpeed(Direction dir)
        {
            return ComputeMovementSpeed(dir, true);
        }
        public virtual TimeSpan ComputeMovementSpeed(Direction dir, bool checkTurning)
        {
            TimeSpan delay;

            if (Mounted)
                delay = (dir & Direction.Running) != 0 ? m_RunMount : m_WalkMount;
            else
                delay = (dir & Direction.Running) != 0 ? m_RunFoot : m_WalkFoot;

            return delay;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when a Mobile <paramref name="m" /> moves off this Mobile.
        /// </summary>
        /// <returns>True if the move is allowed, false if not.</returns>
        public virtual bool OnMoveOff(Mobile m)
        {
            return true;
        }
        public static void Initialize()
        {
            Server.CommandSystem.Register("IsBlocked", AccessLevel.Owner, new CommandEventHandler(IsBlocked_OnCommand));
            TargetCommands.Register(new FindAllPetsCommand());
            TargetCommands.Register(new EscalateCommand());
            TargetCommands.Register(new DeescalateCommand());
            TargetCommands.Register(new FindAllFollowersCommand());
            TargetCommands.Register(new GetAllFollowersCommand());
            TargetCommands.Register(new FindAllStabledCommand());
        }
        // escalate
        [Usage("IsBlocked numTiles")]
        [Description("Checks to see if a mobile is blocked.")]
        public static void IsBlocked_OnCommand(CommandEventArgs e)
        {
            int numTiles;
            if (e.ArgString == null || e.ArgString.Length == 0 || int.TryParse(e.ArgString, out numTiles) == false)
            {
                e.Mobile.SendMessage("No number of tiles specified. Defaulting to 10.");
                numTiles = 10;
            }
            e.Mobile.Target = new IsBlockedTarget(numTiles);
            e.Mobile.SendMessage("Target the mobile to check.");
        }
        private class IsBlockedTarget : Target
        {
            int m_numTiles;
            public IsBlockedTarget(int numTiles)
                : base(15, false, TargetFlags.None)
            {
                m_numTiles = numTiles;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is Mobile m)
                {
                    Utility.TimeCheck tc = new Utility.TimeCheck();
                    tc.Start();
                    from.SendMessage(string.Format("That mobile {0} blocked", m.IsBlocked(m_numTiles) ? "is" : "is not"));
                    tc.End();
                    from.SendMessage(string.Format("check took {0}", tc.TimeTaken));
                }
                else
                {
                    from.SendMessage("That is not a mobile.");
                }
                return;
            }
        }
        public bool IsBlocked(int numTiles)
        {
            Point3D here = Location;
            Point3D[] targets = new Point3D[]
            {
                new Point3D(here.X, here.Y-numTiles, here.Z),           // up
                new Point3D(here.X, here.Y+ numTiles, here.Z),          // down
                new Point3D(here.X-numTiles, here.Y, here.Z),           // left
                new Point3D(here.X+numTiles, here.Y, here.Z),           // right
                new Point3D(here.X+numTiles, here.Y-numTiles, here.Z),  // north
                new Point3D(here.X-numTiles, here.Y+numTiles, here.Z),  // south
                new Point3D(here.X+numTiles, here.Y+numTiles, here.Z),  // east
                new Point3D(here.X-numTiles, here.Y-numTiles, here.Z),  // west
            };
            foreach (Point3D there in targets)
            {
                // can we get there from here?
                Movement.MovementObject obj_start = new Movement.MovementObject(here, there, this.Map);
                if (MovementPath.PathTo(obj_start))
                {   // now check the opposite direction (you are standing on a tile (table) you cannot step off of)
                    obj_start = new Movement.MovementObject(there, here, this.Map);
                    if (MovementPath.PathTo(obj_start))
                        return false;   // can get there from here
                }
            }

            return true;
        }

        public virtual bool IsDeadBondedPet { get { return false; } }

        /// <summary>
        /// Overridable. Event invoked when a Mobile <paramref name="m" /> moves over this Mobile.
        /// </summary>
        /// <returns>True if the move is allowed, false if not.</returns>
        public virtual bool OnMoveOver(Mobile m)
        {
            if (m_Map == null || m_Deleted)
                return true;

            if ((m_Map.Rules & MapRules.FreeMovement) == 0)
            {
                if (!Alive || !m.Alive || IsDeadBondedPet || m.IsDeadBondedPet)
                    return true;
                else if (m_Hidden && m_AccessLevel > AccessLevel.Player)
                    return true;

                if (!m.m_Pushing)
                {
                    m.m_Pushing = true;

                    int number;

                    if (m.AccessLevel > AccessLevel.Player)
                    {
                        number = m_Hidden ? 1019041 : 1019040;
                    }
                    else
                    {
                        if (m.Stam == m.StamMax)
                        {
                            number = m_Hidden ? 1019043 : 1019042;
                            m.Stam -= 10;

                            m.RevealingAction();
                        }
                        else
                        {
                            return false;
                        }
                    }

                    m.SendLocalizedMessage(number);
                }
            }

            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile sees another Mobile, <paramref name="m" />, move.
        /// </summary>
        public virtual void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (CanSee(m))
                OnSee(m);
        }

        public ISpell Spell
        {
            get
            {
                return m_Spell;
            }
            set
            {
                if (m_Spell != null && value != null)
                    Console.WriteLine("Warning: Spell has been overwritten");

                m_Spell = value;
            }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool AutoPageNotify
        {
            get
            {
                return m_AutoPageNotify;
            }
            set
            {
                m_AutoPageNotify = value;
            }
        }

        public virtual void OnReportedForMurder(Mobile from)
        {
        }

        public virtual void CriminalAction(bool message)
        {
            if (m_Deleted)
                return;

            Criminal = true;

            m_Region.OnCriminalAction(this, message);
        }

        public void ClearStuckMenu()
        { m_StuckMenuUses = null; }
        public bool CanUseStuckMenu()
        {
            if (m_StuckMenuUses == null)
            {
                return true;
            }
            else
            {
                for (int i = 0; i < m_StuckMenuUses.Length; ++i)
                {
                    if ((DateTime.UtcNow - m_StuckMenuUses[i]) > TimeSpan.FromDays(1.0))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public virtual bool IsSnoop(Mobile from)
        {
            return (from != this);
        }

        /// <summary>
        /// Overridable. Any call to <see cref="Resurrect" /> will silently fail if this method returns false.
        /// <seealso cref="Resurrect" />
        /// </summary>
        public virtual bool CheckResurrect()
        {
            return true;
        }

        /// <summary>
        /// Overridable. Event invoked before the Mobile is <see cref="Resurrect">resurrected</see>.
        /// <seealso cref="Resurrect" />
        /// </summary>
        public virtual void OnBeforeResurrect()
        {
        }

        /// <summary>
        /// Overridable. Event invoked after the Mobile is <see cref="Resurrect">resurrected</see>.
        /// <seealso cref="Resurrect" />
        /// </summary>
        public virtual void OnAfterResurrect()
        {
        }

        public virtual void Resurrect()
        {
            if (!Alive)
            {
                if (!Region.OnResurrect(this))
                    return;

                if (!CheckResurrect())
                    return;

                OnBeforeResurrect();

                BankBox box = FindBankNoCreate();

                if (box != null && box.Opened)
                    box.Close();

                Poison = null;

                Warmode = false;

                Hits = 10;
                Stam = StamMax;
                Mana = 0;

                BodyMod = 0;
                Body = m_Female ? 0x191 : 0x190;

                ProcessDeltaQueue();

                for (int i = m_Items.Count - 1; i >= 0; --i)
                {
                    if (i >= m_Items.Count)
                        continue;

                    Item item = (Item)m_Items[i];

                    if (item.ItemID == 0x204E)
                        item.Delete();
                }

                this.SendIncomingPacket();
                this.SendIncomingPacket();

                OnAfterResurrect();

                //Send( new DeathStatus( false ) );
            }
        }

        private IAccount m_Account;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Owner)]
        public IAccount Account
        {
            get
            {
                return m_Account;
            }

            set { m_Account = value; }
        }

        private bool m_Deleted;

        public bool Deleted
        {
            get
            {
                return m_Deleted;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int VirtualArmor
        {
            get
            {
                return m_VirtualArmor;
            }
            set
            {
                if (m_VirtualArmor != value)
                {
                    m_VirtualArmor = value;

                    Delta(MobileDelta.Armor);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual double ArmorRating
        {
            get
            {
                return 0.0;
            }
        }

        public void DropHolding()
        {
            Item holding = m_Holding;

            if (holding != null)
            {
                if (!holding.Deleted && holding.Map == Map.Internal)
                    AddToBackpack(holding);

                Holding = null;
                holding.ClearBounce();
            }
        }

        public bool CheckHolding()
        {
            return Holding != null;
        }

        // generic, non specific event handler.
        //	can be defined in each leaf class
        public virtual void OnEvent(object eo)
        {
        }

        public virtual void Delete()
        {
            if (m_Deleted)
                return;

            if (!World.OnDelete(this))
                return;

            if (m_NetState != null)
                m_NetState.CancelAllTrades();

            if (m_NetState != null)
                m_NetState.Dispose();

            DropHolding();

            Region.InternalExit(this);

            OnDelete();

            for (int i = m_Items.Count - 1; i >= 0; --i)
                if (i < m_Items.Count)
                    ((Item)m_Items[i]).OnParentDeleted(this);

            SendRemovePacket();

            if (m_Guild != null)
                m_Guild.OnDelete(this);

            m_Deleted = true;

            if (m_Map != null)
            {
                m_Map.OnLeave(this);
                m_Map = null;
            }

            m_Hair = null;
            m_Beard = null;
            m_MountItem = null;

            World.RemoveMobile(this);

            OnAfterDelete();

            FreeCache();
        }

        /// <summary>
        /// Overridable. Virtual event invoked before the Mobile is deleted.
        /// </summary>
        public virtual void OnDelete()
        {
        }

        /// <summary>
        /// Overridable. Returns true if the player is alive, false if otherwise. By default, this is computed by: <c>!Deleted &amp;&amp; (!Player || !Body.IsGhost)</c>
        /// </summary>
        [CommandProperty(AccessLevel.Counselor)]
        public virtual bool Alive
        {
            get
            {   // Adam: Even though you may be tempted, do not add an IsDeadBondedPet to this test
                //	A dead bonded pet is still considered 'Alive'
                return !m_Deleted && (!m_Player || !m_Body.IsGhost);
            }
        }

        public virtual bool Dead { get { return !Alive; } }

        public virtual bool CheckSpellCast(ISpell spell)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile casts a <paramref name="spell" />.
        /// </summary>
        /// <param name="spell"></param>
        public virtual void OnSpellCast(ISpell spell)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked after <see cref="TotalWeight" /> changes.
        /// </summary>
        public virtual void OnWeightChange(int oldValue)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the <see cref="Skill.Base" /> or <see cref="Skill.BaseFixedPoint" /> property of <paramref name="skill" /> changes.
        /// </summary>
        public virtual void OnSkillChange(SkillName skill, double oldBase)
        {
        }
        public virtual void OnSkillChange(SkillName skill, double oldBase, double newBase)
        {
        }
        /// <summary>
        /// Overridable. Invoked after the mobile is deleted. When overriden, be sure to call the base method.
        /// </summary>
        public virtual void OnAfterDelete()
        {
            StopAggrExpire();

            CheckAggrExpire();

            if (m_PoisonTimer != null)
                m_PoisonTimer.Stop();

            if (m_HitsTimer != null)
                m_HitsTimer.Stop();

            if (m_StamTimer != null)
                m_StamTimer.Stop();

            if (m_ManaTimer != null)
                m_ManaTimer.Stop();

            if (m_CombatTimer != null)
                m_CombatTimer.Stop();

            if (m_ExpireCombatant != null)
                m_ExpireCombatant.Stop();

            if (m_LogoutTimer != null)
                m_LogoutTimer.Stop();

            if (m_ExpireCriminal != null)
                m_ExpireCriminal.Stop();

            if (m_WarmodeTimer != null)
                m_WarmodeTimer.Stop();

            if (m_ParaTimer != null)
                m_ParaTimer.Stop();

            if (m_FrozenTimer != null)
                m_FrozenTimer.Stop();

            if (m_AutoManifestTimer != null)
                m_AutoManifestTimer.Stop();

            if (m_InvisibleShieldTimer != null)
                m_InvisibleShieldTimer.Stop();

            if (m_SuicideBomberTimer != null)
                m_SuicideBomberTimer.Stop();
        }

        public virtual bool AllowSkillUse(SkillName name)
        {
            return true;
        }

        public virtual bool UseSkill(SkillName name)
        {
            return Skills.UseSkill(this, name);
        }

        public virtual bool UseSkill(int skillID)
        {
            return Skills.UseSkill(this, skillID);
        }

        public virtual void OnAfterUseSkill(SkillName name)
        {
        }
        public virtual void OnAfterUseSpell(Type name)
        {
        }

        private static CreateCorpseHandler m_CreateCorpse;

        public static CreateCorpseHandler CreateCorpseHandler
        {
            get { return m_CreateCorpse; }
            set { m_CreateCorpse = value; }
        }

        //plasma: default corpse/bones delay values

        /// <summary>
        /// Returns the length of time for the corpse to decay into bones
        /// </summary>
        /// <returns></returns>
        public virtual TimeSpan CorpseDecayTime() { return TimeSpan.FromMinutes(7.0); }

        /// <summary>
        /// Returns the length of time for the bones to decay
        /// </summary>
        /// <returns></returns>
        public virtual TimeSpan BoneDecayTime() { return TimeSpan.FromMinutes(7.0); }


        public virtual DeathMoveResult GetParentMoveResultFor(Item item)
        {
            return item.OnParentDeath(this);
        }

        public virtual DeathMoveResult GetInventoryMoveResultFor(Item item)
        {
            return item.OnInventoryDeath(this);
        }

        public virtual bool RetainPackLocsOnDeath { get { return Core.RuleSets.AOSRules(); } }

        public virtual void Kill()
        {
            if (!CanBeDamaged())
                return;
            else if (!Alive || IsDeadBondedPet)
                return;
            else if (m_Deleted)
                return;
            else if (!Region.OnDeath(this))
                return;
            else if (!OnBeforeDeath())
                return;

            BankBox box = FindBankNoCreate();

            if (box != null && box.Opened)
                box.Close();

            if (m_NetState != null)
                m_NetState.CancelAllTrades();

            if (m_Spell != null)
                m_Spell.OnCasterKilled();
            //m_Spell.Disturb( DisturbType.Kill );

            if (m_Target != null)
                m_Target.Cancel(this, TargetCancelType.Canceled);

            DisruptiveAction();

            Warmode = false;

            DropHolding();

            BandageContext.AbortBandage(this);

            Hits = 0;
            Stam = 0;
            Mana = 0;

            Poison = null;
            Combatant = null;

            if (Paralyzed)
            {
                Paralyzed = false;

                if (m_ParaTimer != null)
                    m_ParaTimer.Stop();
            }

            if (Frozen)
            {
                Frozen = false;

                if (m_FrozenTimer != null)
                    m_FrozenTimer.Stop();
            }

            if (InvisibleShield)
            {
                InvisibleShield = false;

                if (m_InvisibleShieldTimer != null)
                    m_InvisibleShieldTimer.Stop();
            }

            if (SuicideBomber)
            {
                SuicideBomber = false;

                if (m_SuicideBomberTimer != null)
                    m_SuicideBomberTimer.Stop();
            }

            ArrayList content = new ArrayList();
            ArrayList equip = new ArrayList();
            ArrayList moveToPack = new ArrayList();

            ArrayList itemsCopy = new ArrayList(m_Items);

            Container pack = this.Backpack;

            for (int i = 0; i < itemsCopy.Count; ++i)
            {
                Item item = (Item)itemsCopy[i];

                if (item == pack)
                    continue;

                DeathMoveResult res = GetParentMoveResultFor(item);

                switch (res)
                {
                    case DeathMoveResult.MoveToCorpse:
                        {
                            content.Add(item);
                            equip.Add(item);
                            break;
                        }
                    case DeathMoveResult.MoveToBackpack:
                        {
                            moveToPack.Add(item);
                            break;
                        }
                }
            }

            if (pack != null)
            {
                ArrayList packCopy = new ArrayList(pack.Items);

                for (int i = 0; i < packCopy.Count; ++i)
                {
                    Item item = (Item)packCopy[i];

                    DeathMoveResult res = GetInventoryMoveResultFor(item);

                    if (res == DeathMoveResult.MoveToCorpse)
                        content.Add(item);
                    else
                        moveToPack.Add(item);
                }

                for (int i = 0; i < moveToPack.Count; ++i)
                {
                    Item item = (Item)moveToPack[i];

                    if (RetainPackLocsOnDeath && item.Parent == pack)
                        continue;

                    pack.DropItem(item);
                }
            }

            Container c = (m_CreateCorpse == null ? null : m_CreateCorpse(this, content, equip));

            /*m_Corpse = c;

			for ( int i = 0; c != null && i < content.Count; ++i )
				c.DropItem( (Item)content[i] );

			if ( c != null )
				c.MoveToWorld( this.Location, this.Map );*/

            if (m_Map != null)
            {
                Packet animPacket = null;//new DeathAnimation( this, c );
                Packet remPacket = null;//this.RemovePacket;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state != m_NetState)
                    {
                        if (animPacket == null)
                            animPacket = Packet.Acquire(new DeathAnimation(this, c));

                        state.Send(animPacket);

                        if (!state.Mobile.CanSee(this))
                        {
                            if (remPacket == null)
                                remPacket = this.RemovePacket;

                            state.Send(remPacket);
                        }
                    }
                }

                Packet.Release(animPacket);

                eable.Free();
            }

            OnDeath(c);

            // Adam: added so we can paralyze ghosts via the region since Frozen is cleared after OnDeath() is called
            Region.OnAfterDeath(this);
        }

        private Container m_Corpse;

        [CommandProperty(AccessLevel.GameMaster)]
        public Container Corpse
        {
            get
            {
                if (m_Corpse == null || m_Corpse.Deleted == true)
                    return null;
                return m_Corpse;
            }
            set
            {
                m_Corpse = value;
            }
        }

        /// <summary>
        /// Overridable. Event invoked before the Mobile is <see cref="Kill">killed</see>.
        /// <seealso cref="Kill" />
        /// <seealso cref="OnDeath" />
        /// </summary>
        /// <returns>True to continue with death, false to override it.</returns>
        public virtual bool OnBeforeDeath()
        {
            return true;
        }

        /// <summary>
        /// Overridable. Event invoked after the Mobile is <see cref="Kill">killed</see>. Primarily, this method is responsible for deleting an NPC or turning a PC into a ghost.
        /// <seealso cref="Kill" />
        /// <seealso cref="OnBeforeDeath" />
        /// </summary>
        public virtual void OnDeath(Container c)
        {
            // Adam: set the corpse container
            m_Corpse = c;

            int sound = this.GetDeathSound();

            if (sound >= 0)
                Effects.PlaySound(this, this.Map, sound);

            if (!m_Player)
            {
                Delete();

                #region StaticCorpse
                TimeSpan decayTime = TimeSpan.FromHours(24.0);
                if (Spawner is Spawner spawner)
                {
                    decayTime = Utility.RandomMinMax(spawner.MinDelay, spawner.MinDelay);
                    if (spawner.StaticCorpse)
                    {
                        Corpse corpse = c as Corpse;
                        corpse.BeginDecay(decayTime);
                        corpse.StaticCorpse = true;
                        for (int i = 0; i < 3; i++)
                        {
                            Point3D p = new Point3D(Location);
                            p.X += Utility.RandomMinMax(-1, 1);
                            p.Y += Utility.RandomMinMax(-1, 1);
                            new Blood(Utility.Random(0x122A, 5), decayTime.TotalSeconds).MoveToWorld(p, c.Map);
                        }
                    }
                }
                #endregion StaticCorpse
            }
            else
            {
                Send(DeathStatus.Instantiate(true));

                Warmode = false;

                BodyMod = 0;
                Body = this.Female ? 0x193 : 0x192;

                Item deathShroud = new Item(0x204E);

                deathShroud.Movable = false;
                deathShroud.Layer = Layer.OuterTorso;

                AddItem(deathShroud);

                m_Items.Remove(deathShroud);
                m_Items.Insert(0, deathShroud);

                Poison = null;
                Combatant = null;

                Hits = 0;
                Stam = 0;
                Mana = 0;

                EventSink.InvokePlayerDeath(new PlayerDeathEventArgs(this));

                ProcessDeltaQueue();

                Send(DeathStatus.Instantiate(false));

                CheckStatTimers();
            }
        }

        public virtual int GetAngerSound()
        {
            if (m_BaseSoundID != 0)
                return m_BaseSoundID;

            return -1;
        }

        public virtual int GetIdleSound()
        {
            if (m_BaseSoundID != 0)
                return m_BaseSoundID + 1;

            return -1;
        }

        public virtual int GetAttackSound()
        {
            if (m_BaseSoundID != 0)
                return m_BaseSoundID + 2;

            return -1;
        }

        public virtual int GetHurtSound()
        {
            if (m_BaseSoundID != 0)
                return m_BaseSoundID + 3;

            return -1;
        }

        public virtual int GetDeathSound()
        {
            if (m_BaseSoundID != 0)
            {
                return m_BaseSoundID + 4;
            }
            else if (m_Body.IsHuman)
            {
                return Utility.Random(m_Female ? 0x314 : 0x423, m_Female ? 4 : 5);
            }
            else
            {
                return -1;
            }
        }

        private static char[] m_GhostChars = new char[2] { 'o', 'O' };

        public static char[] GhostChars { get { return m_GhostChars; } set { m_GhostChars = value; } }

        private static bool m_NoSpeechLOS;

        public static bool NoSpeechLOS { get { return m_NoSpeechLOS; } set { m_NoSpeechLOS = value; } }

        private static TimeSpan m_AutoManifestTimeout = TimeSpan.FromSeconds(5.0);

        public static TimeSpan AutoManifestTimeout { get { return m_AutoManifestTimeout; } set { m_AutoManifestTimeout = value; } }

        private Timer m_AutoManifestTimer;

        private class AutoManifestTimer : Timer
        {
            private Mobile m_Mobile;

            public AutoManifestTimer(Mobile m, TimeSpan delay)
                : base(delay)
            {
                m_Mobile = m;
            }

            protected override void OnTick()
            {
                if (!m_Mobile.Alive)
                    m_Mobile.Warmode = false;
            }
        }

        public virtual bool CheckTarget(Mobile from, Target targ, object targeted)
        {
            return true;
        }

        private static bool m_InsuranceEnabled;

        public static bool InsuranceEnabled
        {
            get { return m_InsuranceEnabled; }
            set { m_InsuranceEnabled = value; }
        }

        public virtual void Use(Item item)
        {
            if (item == null || item.Deleted)
                return;

            DisruptiveAction();

            if (m_Spell != null && !m_Spell.OnCasterUsingObject(item))
                return;

            object root = item.RootParent;
            bool okay = false;

            if (!Utility.InUpdateRange(this, item.GetWorldLocation()))
                item.OnDoubleClickOutOfRange(this);
            else if (!CanSee(item))
                item.OnDoubleClickCantSee(this);
            else if (!item.IsAccessibleTo(this))
            {
                Region reg = Region.Find(item.GetWorldLocation(), item.Map);

                if (reg == null || !reg.SendInaccessibleMessage(item, this))
                    item.OnDoubleClickNotAccessible(this);
            }
            else if (!CheckAlive(false))
                item.OnDoubleClickDead(this);
            else if (item.InSecureTrade)
                item.OnDoubleClickSecureTrade(this);
            else if (!AllowItemUse(item))
                okay = false;
            else if (!item.CheckItemUse(this, item))
                okay = false;
            else if (root != null && root is Mobile && ((Mobile)root).IsSnoop(this))
                item.OnSnoop(this);
            else if (m_Region.OnDoubleClick(this, item))
                okay = true;

            if (okay)
            {
                if (!item.Deleted)
                    item.OnItemUsed(this, item);

                if (!item.Deleted)
                    item.OnDoubleClick(this);
            }
        }

        public virtual void Use(Mobile m)
        {
            if (m == null || m.Deleted)
                return;

            DisruptiveAction();

            if (m_Spell != null && !m_Spell.OnCasterUsingObject(m))
                return;

            if (!Utility.InUpdateRange(this, m))
                m.OnDoubleClickOutOfRange(this);
            else if (!CanSee(m))
                m.OnDoubleClickCantSee(this);
            else if (!CheckAlive(false))
                m.OnDoubleClickDead(this);
            else if (m_Region.OnDoubleClick(this, m) && !m.Deleted)
                m.OnDoubleClick(this);
        }

        public virtual bool IsOwner(Mobile from)
        {
            return false;
        }

        public virtual void OnHit(Mobile attacker, Mobile defender)
        {
        }
        public virtual void OnMiss(Mobile attacker, Mobile defender)
        {
        }

        private bool CheckNonLocalStorage(Item item)
        {
            if (item.RootParent is Container cont)
                return cont.NonLocalStorage;
            return false;
        }
        private static int m_ActionDelay = 500;

        public static int ActionDelay
        {
            get { return m_ActionDelay; }
            set { m_ActionDelay = value; }
        }
        private bool CheckPublicContainer(Item item)
        {
            if (item.RootParent is Container && item.RootParent is not Server.Items.Corpse && Multis.BaseHouse.FindHouseAt(item) == null)
                return true;
            return false;
        }
        public virtual void Lift(Item item, int amount, out bool rejected, out LRReason reject)
        {
            rejected = true;
            reject = LRReason.Inspecific;

            if (item == null)
                return;

            Mobile from = this;
            NetState state = m_NetState;

            if (from.AccessLevel >= AccessLevel.GameMaster || Core.TickCount - from.NextActionTime >= 0)
            {
                if (from.CheckAlive())
                {
                    from.DisruptiveAction();

                    if (this.DenyAccessPublicContainer && CheckPublicContainer(item))
                    {
                        // AssHat Mitigation
                        Misc.GoodwillAsshat.ApplyPunishment(from, null, Poison.Lethal);
                        reject = LRReason.Inspecific;
                    }
                    else if (from.Holding != null)
                    {
                        reject = LRReason.AreHolding;
                    }
                    else if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(item.GetWorldLocation(), 2))
                    {
                        reject = LRReason.OutOfRange;
                    }
                    else if ((!from.CanSee(item) || !from.InLOS(item)) && !CheckNonLocalStorage(item))
                    {
                        reject = LRReason.OutOfSight;
                    }
                    else if (!item.VerifyMove(from) && item.GetItemBool(Item.ItemBoolTable.MustSteal))
                    {
                        reject = LRReason.TryToSteal;
                    }
                    else if (!item.VerifyMove(from))
                    {
                        reject = LRReason.CannotLift;
                    }
                    else if (item.InSecureTrade || !item.IsAccessibleTo(from))
                    {
                        reject = LRReason.CannotLift;
                    }
                    //					else if ( !item.CheckLift( from, item ) )
                    //					{
                    //						reject = LRReason.Inspecific;
                    //					}
                    else if (!item.CheckLift(from, item, ref reject))
                    {
                    }
                    else
                    {
                        object root = item.RootParent;

                        if (root != null && root is Mobile && !((Mobile)root).CheckNonlocalLift(from, item))
                        {   // if CheckNonlocalLift fails and you are the owner of this NPC, stealing is not message we want
                            if (((Mobile)root).IsOwner(from))
                                reject = LRReason.Inspecific;
                            else
                                reject = LRReason.TryToSteal;
                        }
                        else if (!from.OnDragLift(item) || !item.OnDragLift(from))
                        {
                            reject = LRReason.Inspecific;
                        }
                        else if (!from.CheckAlive())
                        {
                            reject = LRReason.Inspecific;
                        }
                        else
                        {
                            item.SetLastMoved();

                            if (amount == 0)
                                amount = 1;

                            if (amount > item.Amount)
                                amount = item.Amount;

                            int oldAmount = item.Amount;
                            item.Amount = amount;

                            if (amount < oldAmount)
                                item.Dupe(oldAmount - amount);

                            Map map = from.Map;

                            if (Mobile.DragEffects && map != null && (root == null || root is Item))
                            {
                                IPooledEnumerable eable = map.GetClientsInRange(from.Location);
                                Packet p = null;

                                foreach (NetState ns in eable)
                                {
                                    if (ns.Mobile != from && ns.Mobile.CanSee(from))
                                    {
                                        if (p == null)
                                        {
                                            IEntity src;

                                            if (root == null)
                                                src = new Entity(Serial.Zero, item.Location, map);
                                            else
                                                src = new Entity(((Item)root).Serial, ((Item)root).Location, map);

                                            p = Packet.Acquire(new DragEffect(src, from, item.ItemID, item.Hue, amount));
                                        }

                                        ns.Send(p);
                                    }
                                }

                                Packet.Release(p);

                                eable.Free();
                            }

                            Point3D fixLoc = item.Location;
                            Map fixMap = item.Map;
                            bool shouldFix = (item.Parent == null);

                            item.RecordBounce();
                            item.OnItemLifted(from, item);
                            item.Internalize();

                            from.Holding = item;

                            int liftSound = item.GetLiftSound(from);

                            if (liftSound != -1)
                                from.Send(new PlaySound(liftSound, from));

                            from.NextActionTime = Core.TickCount + m_ActionDelay;

                            if (fixMap != null && shouldFix)
                                fixMap.FixColumn(fixLoc.m_X, fixLoc.m_Y);

                            reject = LRReason.Inspecific;
                            rejected = false;
                        }
                    }
                }
                else
                {
                    reject = LRReason.Inspecific;
                }
            }
            else
            {
                SendActionMessage();
                reject = LRReason.Inspecific;
            }

            if (rejected && state != null)
            {
                state.Send(new LiftRej(reject));

                if (item.Parent is Item)
                {
                    if (state.ContainerGridLines/*.IsPost6017*/)
                    {
                        state.Send(new ContainerContentUpdate6017(item));
                    }
                    else
                    {
                        state.Send(new ContainerContentUpdate(item));
                    }
                }
                else if (item.Parent is Mobile)
                    state.Send(new EquipUpdate(item));
                else
                    item.SendInfoTo(state);

                if (ObjectPropertyList.Enabled && item.Parent != null)
                    state.Send(item.OPLPacket);
            }
        }

        public virtual void SendDropEffect(Item item)
        {
            if (Mobile.DragEffects)
            {
                Map map = m_Map;
                object root = item.RootParent;

                if (map != null && (root == null || root is Item))
                {
                    IPooledEnumerable eable = map.GetClientsInRange(m_Location);
                    Packet p = null;

                    foreach (NetState ns in eable)
                    {
                        if (ns.Mobile != this && ns.Mobile.CanSee(this))
                        {
                            if (p == null)
                            {
                                IEntity trg;

                                if (root == null)
                                    trg = new Entity(Serial.Zero, item.Location, map);
                                else
                                    trg = new Entity(((Item)root).Serial, ((Item)root).Location, map);

                                p = Packet.Acquire(new DragEffect(this, trg, item.ItemID, item.Hue, item.Amount));
                            }

                            ns.Send(p);
                        }
                    }

                    Packet.Release(p);

                    eable.Free();
                }
            }
        }

        public virtual bool Drop(Item to, Point3D loc)
        {
            Mobile from = this;
            Item item = from.Holding;

            if (item == null)
                return false;

            from.Holding = null;
            bool bounced = true;

            item.SetLastMoved();

            if (to == null || !item.DropToItem(from, to, loc))
                item.Bounce(from);
            else
                bounced = false;

            item.ClearBounce();

            if (!bounced)
                SendDropEffect(item);

            return !bounced;
        }

        public virtual bool Drop(Point3D loc)
        {
            Mobile from = this;
            Item item = from.Holding;

            if (item == null)
                return false;

            from.Holding = null;
            bool bounced = true;

            item.SetLastMoved();

            if (!item.DropToWorld(from, loc))
                item.Bounce(from);
            else
                bounced = false;

            item.ClearBounce();

            if (!bounced)
                SendDropEffect(item);

            return !bounced;
        }

        public virtual bool Drop(Mobile to, Point3D loc)
        {
            Mobile from = this;
            Item item = from.Holding;

            if (item == null)
                return false;

            from.Holding = null;
            bool bounced = true;

            item.SetLastMoved();

            if (to == null || !item.DropToMobile(from, to, loc))
                item.Bounce(from);
            else
                bounced = false;

            item.ClearBounce();

            if (!bounced)
                SendDropEffect(item);

            return !bounced;
        }

        private static object m_GhostMutateContext = new object();

        public virtual bool MutateSpeech(ArrayList hears, ref string text, ref object context)
        {
            if (Alive)
                return false;

            StringBuilder sb = new StringBuilder(text.Length, text.Length);

            for (int i = 0; i < text.Length; ++i)
            {
                if (text[i] != ' ')
                    sb.Append(m_GhostChars[Utility.Random(m_GhostChars.Length)]);
                else
                    sb.Append(' ');
            }

            text = sb.ToString();
            context = m_GhostMutateContext;
            return true;
        }

        public virtual void Manifest(TimeSpan delay)
        {
            Warmode = true;

            if (m_AutoManifestTimer == null)
                m_AutoManifestTimer = new AutoManifestTimer(this, delay);
            else
                m_AutoManifestTimer.Stop();

            m_AutoManifestTimer.Start();
        }

        public virtual bool CheckSpeechManifest()
        {
            if (Alive)
                return false;

            TimeSpan delay = m_AutoManifestTimeout;

            if (delay > TimeSpan.Zero && (!Warmode || m_AutoManifestTimer != null))
            {
                Manifest(delay);
                return true;
            }

            return false;
        }

        public virtual bool CheckHearsMutatedSpeech(Mobile m, object context)
        {
            if (context == m_GhostMutateContext)
                return (m.Alive && !m.CanHearGhosts);

            return true;
        }

        private void AddSpeechItemsFrom(ArrayList list, Container cont)
        {
            for (int i = 0; i < cont.Items.Count; ++i)
            {
                Item item = (Item)cont.Items[i];

                if (item.HandlesOnSpeech)
                    list.Add(item);

                if (item is Container)
                    AddSpeechItemsFrom(list, (Container)item);
            }
        }

        public class LocationComparer : IComparer
        {
            private static LocationComparer m_Instance;

            public static LocationComparer GetInstance(IPoint3D relativeTo)
            {
                if (m_Instance == null)
                    m_Instance = new LocationComparer(relativeTo);
                else
                    m_Instance.m_RelativeTo = relativeTo;

                return m_Instance;
            }

            private IPoint3D m_RelativeTo;

            public IPoint3D RelativeTo
            {
                get { return m_RelativeTo; }
                set { m_RelativeTo = value; }
            }

            public LocationComparer(IPoint3D relativeTo)
            {
                m_RelativeTo = relativeTo;
            }

            private int GetDistance(IPoint3D p)
            {
                int x = m_RelativeTo.X - p.X;
                int y = m_RelativeTo.Y - p.Y;
                int z = m_RelativeTo.Z - p.Z;

                x *= 11;
                y *= 11;

                return (x * x) + (y * y) + (z * z);
            }

            public int Compare(object x, object y)
            {
                IPoint3D a = x as IPoint3D;
                IPoint3D b = y as IPoint3D;

                return GetDistance(a) - GetDistance(b);
            }
        }

        public IPooledEnumerable GetItemsInRange(int range)
        {
            Map map = m_Map;

            if (map == null)
                return Map.NullEnumerable<Item>.Instance;

            return map.GetItemsInRange(m_Location, range);
        }

        public IPooledEnumerable GetObjectsInRange(int range)
        {
            Map map = m_Map;

            if (map == null)
                return Map.NullEnumerable<IEntity>.Instance;

            return map.GetObjectsInRange(m_Location, range);
        }

        public IPooledEnumerable GetMobilesInRange(int range)
        {
            Map map = m_Map;

            if (map == null)
                return Map.NullEnumerable<Mobile>.Instance;

            return map.GetMobilesInRange(m_Location, range);
        }

        public IPooledEnumerable GetClientsInRange(int range)
        {
            Map map = m_Map;

            if (map == null)
                return Map.NullEnumerable<NetState>.Instance;

            return map.GetClientsInRange(m_Location, range);
        }

        private static ArrayList m_Hears;
        private static ArrayList m_OnSpeech;

        public virtual void SendGuildChat(string text)
        {
        }

        public virtual void SendAlliedChat(string text)
        {
        }
        public virtual int AudibleRange => 15;
        public virtual void DoSpeech(string text, int[] keywords, MessageType type, int hue)
        {
            try
            {
                if (m_Deleted || CommandSystem.Handle(this, text))
                    return;

                int range = AudibleRange;

                switch (type)
                {
                    case MessageType.Regular: m_SpeechHue = hue; break;
                    case MessageType.Emote: m_EmoteHue = hue; break;
                    case MessageType.Whisper: m_WhisperHue = hue; range = 1; break;
                    case MessageType.Yell: m_YellHue = hue; range = 18; break;
                    case MessageType.Guild:
                        {
                            SendGuildChat(text);
                            return;
                        }
                    case MessageType.Alliance:
                        {
                            SendAlliedChat(text);
                            return;
                        }
                    default: type = MessageType.Regular; break;
                }

                SpeechEventArgs regArgs = new SpeechEventArgs(this, text, type, hue, keywords);

                EventSink.InvokeSpeech(regArgs);
                m_Region.OnSpeech(regArgs);
                OnSaid(regArgs);

                if (regArgs.Blocked)
                    return;

                text = regArgs.Speech;

                if (text == null || text.Length == 0)
                    return;

                if (m_Hears == null)
                    m_Hears = new ArrayList();
                else if (m_Hears.Count > 0)
                    m_Hears.Clear();

                if (m_OnSpeech == null)
                    m_OnSpeech = new ArrayList();
                else if (m_OnSpeech.Count > 0)
                    m_OnSpeech.Clear();

                ArrayList hears = m_Hears;
                ArrayList onSpeech = m_OnSpeech;

                if (m_Map != null)
                {
                    IPooledEnumerable eable = m_Map.GetObjectsInRange(m_Location, range);
                    List<object> list = new List<object>();
                    foreach (object o in eable)
                        list.Add(o);
                    eable.Free();

                    foreach (object o in list)
                    {
                        if (o is Mobile)
                        {
                            Mobile heard = (Mobile)o;

                            // wea: InLOS -> IsAudibleTo
                            // 1/12/2024, Adam: new property CanSeeGhosts == CanHearGhosts
                            //  The presumption is that if you can hear ghosts, the text should be passed along to those than can see ghosts
                            //  The MoongateWizard is one such mobile that can always see/hear ghosts
                            // old: if (heard.CanSee(this) && (m_NoSpeechLOS || !heard.Player || heard.IsAudibleTo(this)))
                            if ((heard.CanSee(this) || (heard.CanHearGhosts && !this.Alive)) && (m_NoSpeechLOS || !heard.Player || heard.IsAudibleTo(this)))
                            {
                                if (heard.m_NetState != null)
                                    hears.Add(heard);

                                if (heard.HandlesOnSpeech(this))
                                    onSpeech.Add(heard);

                                for (int i = 0; i < heard.Items.Count; ++i)
                                {
                                    Item item = (Item)heard.Items[i];

                                    if (item.HandlesOnSpeech)
                                        onSpeech.Add(item);

                                    if (item is Container)
                                        AddSpeechItemsFrom(onSpeech, (Container)item);
                                }
                            }
                        }
                        else if (o is Item)
                        {
                            if (((Item)o).HandlesOnSpeech)
                                onSpeech.Add(o);

                            if (o is Container)
                                AddSpeechItemsFrom(onSpeech, (Container)o);
                        }
                    }

                    object mutateContext = null;
                    string mutatedText = text;
                    SpeechEventArgs mutatedArgs = null;

                    if (MutateSpeech(hears, ref mutatedText, ref mutateContext))
                        mutatedArgs = new SpeechEventArgs(this, mutatedText, type, hue, new int[0]);

                    CheckSpeechManifest();

                    ProcessDelta();

                    Packet regp = null;
                    Packet mutp = null;

                    for (int i = 0; i < hears.Count; ++i)
                    {
                        Mobile heard = (Mobile)hears[i];

                        if (mutatedArgs == null || !CheckHearsMutatedSpeech(heard, mutateContext))
                        {
                            heard.OnSpeech(regArgs);

                            NetState ns = heard.NetState;

                            if (ns != null)
                            {
                                if (regp == null)
                                    regp = Packet.Acquire(new UnicodeMessage(m_Serial, Body, type, hue, 3, m_Language, Name, text));

                                ns.Send(regp);
                            }
                        }
                        else
                        {
                            //heard.OnSpeech( mutatedArgs );

                            NetState ns = heard.NetState;

                            if (ns != null)
                            {
                                if (mutp == null)
                                    mutp = Packet.Acquire(new UnicodeMessage(m_Serial, Body, type, hue, 3, m_Language, Name, mutatedText));

                                ns.Send(mutp);
                            }
                        }
                    }

                    Packet.Release(regp);
                    Packet.Release(mutp);

                    if (onSpeech.Count > 1)
                        onSpeech.Sort(LocationComparer.GetInstance(this));

                    // call each mobile's query processor (if a keyword was spoken)
                    DoQuery(regArgs, new List<IEntity>(onSpeech.Cast<IEntity>().ToList()));

                    for (int i = 0; i < onSpeech.Count; ++i)
                    {
                        object obj = onSpeech[i];

                        if (obj is Mobile)
                        {
                            Mobile heard = (Mobile)obj;

                            if (mutatedArgs == null || !CheckHearsMutatedSpeech(heard, mutateContext))
                                heard.OnSpeech(regArgs);
                            else
                                heard.OnSpeech(mutatedArgs);
                        }
                        else
                        {
                            Item item = (Item)obj;

                            item.OnSpeech(regArgs);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Diagnostics.LogHelper.LogException(ex);
            }
        }

        private static VisibleDamageType m_VisibleDamageType;

        public static VisibleDamageType VisibleDamageType
        {
            get { return m_VisibleDamageType; }
            set { m_VisibleDamageType = value; }
        }

        //Pix: added so that PlayerMobiles can clear their damage entries on death
        // (this is needed to make sure that after we distribute points in the kin system
        //  on death, we don't count damage from before the last death (if the next happens
        //  soon enough after))
        protected void ClearDamageEntries()
        {
            if (m_DamageEntries.Count > 0)
            {
                m_DamageEntries.Clear();
            }
        }

        private List<DamageEntry> m_DamageEntries;

        public List<DamageEntry> DamageEntries
        {
            get { return m_DamageEntries; }
        }

        public static Mobile GetDamagerFrom(DamageEntry de)
        {
            return (de == null ? null : de.Damager);
        }

        public static bool MostDamage(Mobile dead, Mobile claimant)
        {   // who did the most damage (may be more than one)
            bool allowSelf = false;
            DamageEntry mtd = dead.FindMostTotalDamageEntry(allowSelf);

            // see how much damage this guy did
            DamageEntry def = dead.FindDamageEntryFor(claimant);

            // if the most damage was 100 and Flip got 100, then give him the honor (handles a tie)
            return mtd != null && def != null && mtd.DamageGiven == def.DamageGiven && !def.HasExpired;
        }

        public Mobile FindMostRecentDamager(bool allowSelf)
        {
            return GetDamagerFrom(FindMostRecentDamageEntry(allowSelf));
        }

        public DamageEntry FindMostRecentDamageEntry(bool allowSelf)
        {
            for (int i = m_DamageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= m_DamageEntries.Count)
                    continue;

                DamageEntry de = (DamageEntry)m_DamageEntries[i];

                if (de.HasExpired)
                    m_DamageEntries.RemoveAt(i);
                else if (allowSelf || de.Damager != this)
                    return de;
            }

            return null;
        }

        public Mobile FindLeastRecentDamager(bool allowSelf)
        {
            return GetDamagerFrom(FindLeastRecentDamageEntry(allowSelf));
        }

        public DamageEntry FindLeastRecentDamageEntry(bool allowSelf)
        {
            for (int i = 0; i < m_DamageEntries.Count; ++i)
            {
                if (i < 0)
                    continue;

                DamageEntry de = (DamageEntry)m_DamageEntries[i];

                if (de.HasExpired)
                {
                    m_DamageEntries.RemoveAt(i);
                    --i;
                }
                else if (allowSelf || de.Damager != this)
                {
                    return de;
                }
            }

            return null;
        }

        public Mobile FindMostTotalDamger(bool allowSelf)
        {
            return GetDamagerFrom(FindMostTotalDamageEntry(allowSelf));
        }

        public DamageEntry FindMostTotalDamageEntry(bool allowSelf)
        {
            DamageEntry mostTotal = null;

            for (int i = m_DamageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= m_DamageEntries.Count)
                    continue;

                DamageEntry de = (DamageEntry)m_DamageEntries[i];

                if (de.HasExpired)
                    m_DamageEntries.RemoveAt(i);
                else if ((allowSelf || de.Damager != this) && (mostTotal == null || de.DamageGiven > mostTotal.DamageGiven))
                    mostTotal = de;
            }

            return mostTotal;
        }

        public Mobile FindLeastTotalDamger(bool allowSelf)
        {
            return GetDamagerFrom(FindLeastTotalDamageEntry(allowSelf));
        }

        public DamageEntry FindLeastTotalDamageEntry(bool allowSelf)
        {
            DamageEntry mostTotal = null;

            for (int i = m_DamageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= m_DamageEntries.Count)
                    continue;

                DamageEntry de = (DamageEntry)m_DamageEntries[i];

                if (de.HasExpired)
                    m_DamageEntries.RemoveAt(i);
                else if ((allowSelf || de.Damager != this) && (mostTotal == null || de.DamageGiven < mostTotal.DamageGiven))
                    mostTotal = de;
            }

            return mostTotal;
        }

        public DamageEntry FindDamageEntryFor(Mobile m)
        {
            for (int i = m_DamageEntries.Count - 1; i >= 0; --i)
            {
                if (i >= m_DamageEntries.Count)
                    continue;

                DamageEntry de = (DamageEntry)m_DamageEntries[i];

                if (de.HasExpired)
                    m_DamageEntries.RemoveAt(i);
                else if (de.Damager == m)
                    return de;
            }

            return null;
        }

        public virtual Mobile GetDamageMaster(Mobile damagee)
        {
            return null;
        }

        //Pix: Added this so DamageEntries can have special timeouts (for mobs like the Harrower)
        public virtual double DamageEntryExpireTimeSeconds
        {
            get { return 120.0; }
        }

        public virtual DamageEntry RegisterDamage(int amount, Mobile from)
        {
            DamageEntry de = FindDamageEntryFor(from);

            if (de == null)
            {
                de = new DamageEntry(from);
                de.ExpireDelay = TimeSpan.FromSeconds(DamageEntryExpireTimeSeconds);
            }

            de.DamageGiven += amount;
            de.LastDamage = DateTime.UtcNow;

            m_DamageEntries.Remove(de);
            m_DamageEntries.Add(de);

            Mobile master = from.GetDamageMaster(this);

            if (master != null)
            {
                List<DamageEntry> list = de.Responsible;

                if (list == null)
                    de.Responsible = list = new List<DamageEntry>();

                DamageEntry resp = null;

                for (int i = 0; i < list.Count; ++i)
                {
                    DamageEntry check = (DamageEntry)list[i];

                    if (check.Damager == master)
                    {
                        resp = check;
                        break;
                    }
                }

                if (resp == null)
                    list.Add(resp = new DamageEntry(master));

                resp.DamageGiven += amount;
                resp.LastDamage = DateTime.UtcNow;
            }

            return de;
        }

        private Mobile m_LastKiller;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile LastKiller
        {
            get { return m_LastKiller; }
            set { m_LastKiller = value; }
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile is <see cref="Damage">damaged</see>. It is called before <see cref="Hits">hit points</see> are lowered or the Mobile is <see cref="Kill">killed</see>.
        /// <seealso cref="Damage" />
        /// <seealso cref="Hits" />
        /// <seealso cref="Kill" />
        /// </summary>
        public virtual void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {   // tell the mobile that damaged us, how they did.
            //	this is implemented for debugging low-damage complaints.
            //	See implemention in PlayerMobile
            if (from != null)
                from.OnGaveDamage(amount, this, willKill, source_weapon);
            /*(else) Timer tick damage, from like poison doesn't have a 'from' mobile */
        }
        public virtual void OnGaveDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {   // see comment above
        }

        public virtual void Damage(int amount, object source_weapon)
        {
            Damage(amount, null, source_weapon);
        }
        public virtual void TrackedItemParentChanging(Item item)
        {
            // playermobile overrides this to know when a tracked item has changed parents.
            //  specifically, in Siege and Mortalis, we have Siege Blessed Items.
            //  We need to know when:
            //      1. it is dropped on the ground (no parent)
            //      2. given to another player, or placed in container (not their own backpack/bankbox.) (new parent)
            return;
        }
        public virtual bool CanBeDamaged()
        {
            return !m_Blessed && !IsInvulnerable;
        }

        #region HIT DAMAGE TEST
        /*private static double t_totalTimesHit = 0;
		private static double t_totalDamage = 0;*/
        #endregion HIT DAMAGE TEST

        public virtual void Damage(int amount, Mobile from, object source_weapon)
        {
            if (!CanBeDamaged())
                return;

            if (!Region.OnDamage(this, ref amount))
                return;

            if (amount > 0)
            {
                int oldHits = Hits;
                int newHits = oldHits - amount;

                if (m_Spell != null)
                    m_Spell.OnCasterHurt();

                //if ( m_Spell != null && m_Spell.State == SpellState.Casting )
                //	m_Spell.Disturb( DisturbType.Hurt, false, true );

                if (from != null)
                    RegisterDamage(amount, from);

                DisruptiveAction();

                Paralyzed = false;

                #region HIT DAMAGE TEST
                // show average damage
                /*{
					t_totalTimesHit++;
					t_totalDamage += (double)amount;
					Console.WriteLine("Average Hit damage = {0}", t_totalDamage / t_totalTimesHit);
				}*/
                #endregion HIT DAMAGE TEST

                ShowDamage(amount, from);

                OnDamage(amount, from, newHits < 0, source_weapon);

                if (BlockDamage == true)
                    ;   // take no damage
                else if (newHits < 0)
                {

                    m_LastKiller = from;

                    Hits = 0;

                    if (oldHits >= 0)
                        Kill();
                }
                else
                    Hits = newHits;

            }
        }

        public void ShowDamage(int amount, Mobile from)
        {
            switch (m_VisibleDamageType)
            {
                case VisibleDamageType.Related:
                    {
                        NetState ourState = m_NetState, theirState = (from == null ? null : from.m_NetState);

                        if (ourState == null)
                        {
                            Mobile master = GetDamageMaster(from);

                            if (master != null)
                                ourState = master.m_NetState;
                        }

                        if (theirState == null && from != null)
                        {
                            Mobile master = from.GetDamageMaster(this);

                            if (master != null)
                                theirState = master.m_NetState;
                        }

                        if (amount > 0 && (ourState != null || theirState != null))
                        {
                            Packet p = null;// = new DamagePacket( this, amount );
#if true
                            if (ourState != null)
                            {
                                if (ourState.DamagePacket)
                                    p = Packet.Acquire(new DamagePacket(this, amount));
                                else
                                    p = Packet.Acquire(new DamagePacketOld(this, amount));

                                ourState.Send(p);
                            }
#else
                                if (ourState != null)
                                {
                                    bool newPacket = (ourState.Version != null && ourState.Version >= DamagePacket.Version);

                                    if (newPacket)
                                        p = Packet.Acquire(new DamagePacket(this, amount));
                                    else
                                        p = Packet.Acquire(new DamagePacketOld(this, amount));

                                    ourState.Send(p);
                                }
#endif
#if true
                            if (theirState != null && theirState != ourState)
                            {
                                bool newPacket = theirState.DamagePacket;

                                if (newPacket && (p == null || !(p is DamagePacket)))
                                {
                                    Packet.Release(p);
                                    p = Packet.Acquire(new DamagePacket(this, amount));
                                }
                                else if (!newPacket && (p == null || !(p is DamagePacketOld)))
                                {
                                    Packet.Release(p);
                                    p = Packet.Acquire(new DamagePacketOld(this, amount));
                                }

                                theirState.Send(p);
                            }
#else
                                if (theirState != null && theirState != ourState)
                                {
                                    bool newPacket = (theirState.Version != null && theirState.Version >= DamagePacket.Version);

                                    if (newPacket && (p == null || !(p is DamagePacket)))
                                    {
                                        Packet.Release(p);
                                        p = Packet.Acquire(new DamagePacket(this, amount));
                                    }
                                    else if (!newPacket && (p == null || !(p is DamagePacketOld)))
                                    {
                                        Packet.Release(p);
                                        p = Packet.Acquire(new DamagePacketOld(this, amount));
                                    }

                                    theirState.Send(p);
                                }
#endif
                            Packet.Release(p);
                        }

                        break;
                    }
                case VisibleDamageType.Everyone:
                    {
                        SendDamageToAll(amount);
                        break;
                    }
            }
        }
        public virtual void SendDamageToAll(int amount)
        {
            if (amount < 0)
                return;

            Map map = m_Map;

            if (map == null)
                return;

            IPooledEnumerable eable = map.GetClientsInRange(m_Location);

            Packet pNew = null;
            Packet pOld = null;

            foreach (NetState ns in eable)
            {
                if (ns.Mobile.CanSee(this))
                {
                    if (ns.DamagePacket)
                    {
                        if (pNew == null)
                            pNew = Packet.Acquire(new DamagePacket(this, amount));

                        ns.Send(pNew);
                    }
                    else
                    {
                        if (pOld == null)
                            pOld = Packet.Acquire(new DamagePacketOld(this, amount));

                        ns.Send(pOld);
                    }
                }
            }

            Packet.Release(pNew);
            Packet.Release(pOld);

            eable.Free();
        }

        public void Heal(int amount)
        {
            if (!Alive || IsDeadBondedPet)
                return;

            if (!Region.OnHeal(this, ref amount))
                return;

            if ((Hits + amount) > HitsMax)
            {
                amount = HitsMax - Hits;
            }

            Hits += amount;

            if (amount > 0 && m_NetState != null)
                m_NetState.Send(new MessageLocalizedAffix(Serial.MinusOne, -1, MessageType.Label, 0x3B2, 3, 1008158, "", AffixType.Append | AffixType.System, amount.ToString(), ""));
        }

        public void UsedStuckMenu()
        {
            if (m_StuckMenuUses == null)
            {
                m_StuckMenuUses = new DateTime[2];
            }

            for (int i = 0; i < m_StuckMenuUses.Length; ++i)
            {
                if ((DateTime.UtcNow - m_StuckMenuUses[i]) > TimeSpan.FromDays(1.0))
                {
                    m_StuckMenuUses[i] = DateTime.UtcNow;
                    return;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Squelched
        {
            get
            {
                return m_Squelched;
            }
            set
            {
                m_Squelched = value;

                // probably a loudmouth, don't let them use global chat
                Server.Engines.Chat.ChatHelper.SetChatBan(this, value);
            }
        }
        #region Spawner
        /* Spawner
         * In an effort to identify where each mobile originated as well providing additional functionality, 
         * we associate the Spawner or Champ Engine that spawned this mobile.
         */
        private Item m_hSpawner;    // handle to either of a spawner or champ engine
        [CopyableAttribute(CopyType.DoNotCopy)] // we don't want to be associated with the same spawner/champ engine
        [CommandProperty(AccessLevel.GameMaster)]
        public Item SpawnerHandle
        {
            get { return m_hSpawner; }
            set { m_hSpawner = value; }
        }
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner Spawner
        {
            get { return m_hSpawner as Spawner; }
            set { m_hSpawner = value; }
        }
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public ChampEngine ChampEngine
        {
            get { return m_hSpawner as ChampEngine; }
            set { m_hSpawner = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Point3D SpawnerLocation { get { return (m_hSpawner == null) ? Point3D.Zero : m_hSpawner.Location; } }
        #endregion Spawner
        #region Save Flags
        [Flags]
        enum SaveFlags
        {
            None = 0x0,
            StableChargeDate = 0x01,
            StableBackFees = 0x02,
            CriminalTimer = 0x04,
            ExpirationFlags = 0x08,
            CriminalWarning = 0x10,
            HasGUID = 0x20,
            HasSpawner = 0x40,
            BaseMulti = 0x80,
            TempRefCount = 0x100,   // how many spawners point to this mobile
        }

        private SaveFlags m_SaveFlags = SaveFlags.None;

        private void SetFlag(SaveFlags flag, bool value)
        {
            if (value)
                m_SaveFlags |= flag;
            else
                m_SaveFlags &= ~flag;
        }

        private bool GetFlag(SaveFlags flag)
        {
            return ((m_SaveFlags & flag) != 0);
        }

        private void ReadSaveFlags(GenericReader reader, int version)
        {
            m_SaveFlags = SaveFlags.None;
            if (version >= 32)
                m_SaveFlags = (SaveFlags)reader.ReadInt();
        }

        private void WriteSaveFlags(GenericWriter writer)
        {
            m_SaveFlags = SaveFlags.None;
            SetFlag(SaveFlags.StableChargeDate, m_LastStableChargeTime != DateTime.MinValue ? true : false);
            SetFlag(SaveFlags.StableBackFees, m_StableBackFees != 0 ? true : false);
            SetFlag(SaveFlags.CriminalTimer, m_Criminal);
            SetFlag(SaveFlags.ExpirationFlags, GetStateCount > 0);
            SetFlag(SaveFlags.HasGUID, m_GUID != 0);
            SetFlag(SaveFlags.HasSpawner, m_hSpawner != null);
            SetFlag(SaveFlags.BaseMulti, m_BaseMulti != null);
            SetFlag(SaveFlags.TempRefCount, m_TempRefCount != 0);
            writer.Write((int)m_SaveFlags);
        }
        #endregion Save Flags

        public virtual void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();
            ReadSaveFlags(reader, version);                         // must always follow version

            DateTime CriminalTimerEnd = DateTime.UtcNow;           // version 34

            switch (version)
            {
                case 42:    // remove private PetStable (moved to AnimalTrainer)
                case 41:
                    {
                        if (GetFlag(SaveFlags.TempRefCount) == true)
                            m_TempRefCount = reader.ReadByte();
                        goto case 40;
                    }
                case 40:
                    {
                        if (GetFlag(SaveFlags.BaseMulti) == true)
                            m_BaseMulti = (BaseMulti)reader.ReadItem();
                        goto case 39;
                    }
                case 39:
                    {
                        if (GetFlag(SaveFlags.HasSpawner) == true)
                            m_hSpawner = reader.ReadItem();
                        goto case 38;
                    }
                case 38:
                    {   // patch the SaveFlags.HasGUID.
                        //  see version 37 below
                        goto case 37;
                    }
                case 37:
                    {
                        // Adam: v37: record the GUID for this mobile if it is non zero
                        //  currently only playermobiles have GUIDS

                        if (version < 38)
                        {
                            if (GetFlag((SaveFlags)0x04) == true)
                                m_GUID = reader.ReadUInt();
                        }
                        else
                        {
                            if (GetFlag(SaveFlags.HasGUID) == true)
                                m_GUID = reader.ReadUInt();
                        }

                        goto case 36;
                    }
                case 36:
                    {
                        m_sceneOfTheCrime = reader.ReadPoint3D();
                        goto case 35;
                    }
                case 35:
                    {
                        if (GetFlag(SaveFlags.ExpirationFlags) == true)
                        {
                            int count = reader.ReadInt();
                            if (count > 0)
                            {
                                for (int ix = 0; ix < count; ix++)
                                {
                                    ExpirationFlags.Add(new ExpirationFlag(this, (ExpirationFlagID)reader.ReadInt(), reader.ReadDateTime(), reader.ReadTimeSpan()));
                                }
                            }
                        }

                        goto case 34;
                    }

                case 34:
                    {
                        if (GetFlag(SaveFlags.CriminalTimer) == true)
                            CriminalTimerEnd = reader.ReadDateTime();

                        goto case 33;
                    }
                case 33:
                    {
                        if (GetFlag(SaveFlags.StableBackFees) == true)
                            m_StableBackFees = reader.ReadInt();

                        goto case 32;
                    }
                case 32:
                    {
                        if (GetFlag(SaveFlags.StableChargeDate) == true)
                            m_LastStableChargeTime = reader.ReadDeltaTime();
                        else
                            m_LastStableChargeTime = DateTime.MinValue;

                        goto case 31;
                    }
                case 31:
                    {
                        m_STRBonusCap = reader.ReadInt();
                        goto case 30;
                    }
                case 30:
                    {
                        int size = reader.ReadInt();
                        FlyIDs = new int[size];

                        for (int i = 0; i < size; i++)
                        {
                            FlyIDs[i] = reader.ReadInt();
                        }
                        goto case 29;
                    }
                case 29:
                    {
                        m_CanFlyOver = reader.ReadBool();
                        goto case 28;
                    }

                case 28:
                    {
                        m_LastStatGain = reader.ReadDeltaTime();

                        goto case 27;
                    }
                case 27:
                    {
                        m_BoolTable = (MobileBoolTable)reader.ReadInt();

                        goto case 26;
                    }
                case 26:
                case 25:
                case 24:
                    {
                        m_Corpse = reader.ReadItem() as Container;

                        goto case 23;
                    }
                case 23:
                    {
                        m_CreationTime = reader.ReadDateTime();

                        goto case 22;
                    }
                case 22: // Just removed followers
                case 21:
                    {
                        //m_PetStable = reader.ReadMobileList();
                        if (version < 42)
                        {
                            List<BaseCreature> list = reader.ReadMobileList<BaseCreature>();
                            if (list.Count > 0)
                            {
                                foreach (Mobile m in list)
                                    if (m is BaseCreature bc)
                                        bc.IsAnimalTrainerStabled = true;

                                AnimalTrainer.Table.Add(this, list);
                            }
                        }
                        goto case 20;
                    }
                case 20:
                    {
                        m_CantWalkLand = reader.ReadBool();

                        goto case 19;
                    }
                case 19: // Just removed variables
                case 18:
                    {
                        m_Virtues = new VirtueInfo(reader);

                        goto case 17;
                    }
                case 17:
                    {
                        m_Thirst = reader.ReadInt();
                        m_BAC = reader.ReadInt();

                        goto case 16;
                    }
                case 16:
                    {
                        m_ShortTermMurders = reader.ReadInt();

                        if (version <= 24)
                        {
                            reader.ReadDateTime();
                            reader.ReadDateTime();
                        }

                        goto case 15;
                    }
                case 15:
                    {
                        if (version < 22)
                            reader.ReadInt(); // followers

                        m_FollowersMax = reader.ReadInt();

                        goto case 14;
                    }
                case 14:
                    {
                        m_MagicDamageAbsorb = reader.ReadInt();

                        goto case 13;
                    }
                case 13:
                    {
                        m_GuildFealty = reader.ReadMobile();

                        goto case 12;
                    }
                case 12:
                    {
                        m_Guild = reader.ReadGuild();

                        goto case 11;
                    }
                case 11:
                    {
                        m_DisplayGuildTitle = reader.ReadBool();

                        goto case 10;
                    }
                case 10:
                    {
                        m_CanSwim = reader.ReadBool();

                        goto case 9;
                    }
                case 9:
                    {
                        m_Squelched = reader.ReadBool();

                        goto case 8;
                    }
                case 8:
                    {
                        m_Holding = reader.ReadItem();

                        goto case 7;
                    }
                case 7:
                    {
                        m_VirtualArmor = reader.ReadInt();

                        goto case 6;
                    }
                case 6:
                    {
                        m_BaseSoundID = reader.ReadInt();

                        goto case 5;
                    }
                case 5:
                    {
                        m_DisarmReady = reader.ReadBool();
                        m_StunReady = reader.ReadBool();

                        goto case 4;
                    }
                case 4:
                    {
                        if (version <= 25)
                        {
                            Poison.Deserialize(reader);

                            /*if ( m_Poison != null )
							{
								m_PoisonTimer = new PoisonTimer( this );
								m_PoisonTimer.Start();
							}*/
                        }

                        goto case 3;
                    }
                case 3:
                    {
                        m_StatCap = reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        m_NameHue = reader.ReadInt();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Hunger = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        //if (version < 21)
                        //    m_PetStable = new ArrayList();

                        if (version < 18)
                            m_Virtues = new VirtueInfo();

                        if (version < 11)
                            m_DisplayGuildTitle = true;

                        if (version < 3)
                            m_StatCap = 225;

                        if (version < 15)
                        {
                            m_FollowerCount = 0;
                            m_FollowersMax = 5;
                        }

                        m_Location = reader.ReadPoint3D();
                        m_Body = new Body(reader.ReadInt());
                        m_Name = reader.ReadString();
                        m_GuildTitle = reader.ReadString();
                        m_Criminal = reader.ReadBool();
                        m_Kills = reader.ReadInt();
                        m_SpeechHue = reader.ReadInt();
                        m_EmoteHue = reader.ReadInt();
                        m_WhisperHue = reader.ReadInt();
                        m_YellHue = reader.ReadInt();
                        m_Language = reader.ReadString();
                        m_Female = reader.ReadBool();
                        m_Warmode = reader.ReadBool();
                        m_Hidden = reader.ReadBool();
                        m_Direction = (Direction)reader.ReadByte();
                        m_Hue = reader.ReadInt();
                        m_Str = reader.ReadInt();
                        m_Dex = reader.ReadInt();
                        m_Int = reader.ReadInt();
                        m_Hits = reader.ReadInt();
                        m_Stam = reader.ReadInt();
                        m_Mana = reader.ReadInt();
                        m_Map = reader.ReadMap();
                        m_Blessed = reader.ReadBool();
                        m_Fame = reader.ReadInt();
                        m_Karma = reader.ReadInt();
                        m_AccessLevel = (AccessLevel)reader.ReadByte();

                        // Convert old bonus caps to 'no cap'
                        if (version < 31)
                            m_STRBonusCap = 0;

                        // Convert old access levels to new access levels
                        if (version < 31)
                        {
                            switch (m_AccessLevel)
                            {
                                case (AccessLevel)0: //OldPlayer = 0,
                                    {
                                        m_AccessLevel = AccessLevel.Player;
                                        break;
                                    }
                                case (AccessLevel)1: //OldCounselor = 1,
                                    {
                                        m_AccessLevel = AccessLevel.Counselor;
                                        break;
                                    }
                                case (AccessLevel)2: //OldGameMaster = 2,
                                    {
                                        m_AccessLevel = AccessLevel.GameMaster;
                                        break;
                                    }
                                case (AccessLevel)3: //OldSeer = 3,
                                    {
                                        m_AccessLevel = AccessLevel.Seer;
                                        break;
                                    }
                                case (AccessLevel)4: //OldAdministrator = 4,
                                    {
                                        m_AccessLevel = AccessLevel.Administrator;
                                        break;
                                    }
                            }
                        }

                        m_Skills = new Skills(this, reader);

                        int itemCount = reader.ReadInt();

                        m_Items = new List<Item>(itemCount);

                        for (int i = 0; i < itemCount; ++i)
                        {
                            Item item = reader.ReadItem();

                            if (item != null)
                                m_Items.Add(item);
                        }

                        m_Player = reader.ReadBool();
                        m_Title = reader.ReadString();
                        m_Profile = reader.ReadString();
                        m_ProfileLocked = reader.ReadBool();
                        if (version <= 18)
                        {
                            /*m_LightLevel =*/
                            reader.ReadInt();
                            /*m_TotalGold =*/
                            reader.ReadInt();
                            /*m_TotalWeight =*/
                            reader.ReadInt();
                        }
                        m_AutoPageNotify = reader.ReadBool();

                        m_LogoutLocation = reader.ReadPoint3D();
                        m_LogoutMap = reader.ReadMap();

                        m_StrLock = (StatLockType)reader.ReadByte();
                        m_DexLock = (StatLockType)reader.ReadByte();
                        m_IntLock = (StatLockType)reader.ReadByte();

                        m_StatMods = new ArrayList();

                        if (reader.ReadBool())
                        {
                            m_StuckMenuUses = new DateTime[reader.ReadInt()];

                            for (int i = 0; i < m_StuckMenuUses.Length; ++i)
                            {
                                m_StuckMenuUses[i] = reader.ReadDateTime();
                            }
                        }
                        else
                        {
                            m_StuckMenuUses = null;
                        }

                        if (m_Player && m_Map != Map.Internal)
                        {
                            m_LogoutLocation = m_Location;
                            m_LogoutMap = m_Map;

                            m_Map = Map.Internal;
                        }

                        if (m_Map != null)
                            m_Map.OnEnter(this);

                        if (m_Criminal)
                        {
                            // adam: criminal timers can be longer than the standard 2 minutes for things like PJUM
                            //	we now save/restore that value
                            if (m_ExpireCriminal == null)
                                m_ExpireCriminal = new ExpireCriminalTimer(this,
                                    (CriminalTimerEnd > DateTime.UtcNow) ? CriminalTimerEnd - DateTime.UtcNow : TimeSpan.FromSeconds(10)
                                );

                            m_ExpireCriminal.Start();
                        }

                        if (ShouldCheckStatTimers)
                            CheckStatTimers();

                        if (!m_Player && m_Dex <= 100 && m_CombatTimer != null)
                            m_CombatTimer.Priority = TimerPriority.FiftyMS;
                        else if (m_CombatTimer != null)
                            m_CombatTimer.Priority = TimerPriority.EveryTick;

                        m_Region = Region.Find(m_Location, m_Map);

                        m_Region.InternalEnter(this);

                        UpdateResistances();

                        break;
                    }
            }

            // remove trailing white spaces.
            // It's a problem when you use the [set name xyz - always adds a trailing white space.
            //  will get fixed separatly. (Update, fixed)
            if (m_Name != null && m_Name.EndsWith(" "))
                m_Name.TrimEnd();
            if (m_Title != null && m_Title.EndsWith(" "))
                m_Title.TrimEnd();

        }
        public virtual void Serialize(GenericWriter writer)
        {
            int version = 42;
            writer.Write(version);                  // version
            WriteSaveFlags(writer);                 // always follows version

            // version 42
            //  Move privately owned list of pets to AnimalTrainer

            // version 41
            if (GetFlag(SaveFlags.TempRefCount) == true)
                writer.Write(m_TempRefCount);

            // version 40
            if (GetFlag(SaveFlags.BaseMulti) == true)
                writer.Write(m_BaseMulti);

            // v39
            if (GetFlag(SaveFlags.HasSpawner) == true)
                writer.Write(m_hSpawner);

            // Adam: v37: record the GUID for this mobile if it is non zero
            //  currently only playermobiles have GUIDS
            if (GetFlag(SaveFlags.HasGUID) == true)
                writer.Write(m_GUID);

            // Adam: v36: Record the Scene Of The Crime for criminal actions
            writer.Write(m_sceneOfTheCrime);

            // Adam: v35: those states that don't require a timer (timer saver)
            if (GetFlag(SaveFlags.ExpirationFlags) == true)
            {
                writer.Write(GetStateCount);
                foreach (ExpirationFlag es in m_expirationFlags)
                {
                    writer.Write((int)es.FlagID);
                    writer.Write(es.Start);
                    writer.Write(es.Span);
                }
            }

            // version 34
            if (GetFlag(SaveFlags.CriminalTimer) == true)
                writer.Write(m_ExpireCriminal == null ? DateTime.UtcNow : m_ExpireCriminal.NextTick);

            // version 33
            if (GetFlag(SaveFlags.StableBackFees) == true)
                writer.Write(m_StableBackFees);

            // version 32
            if (GetFlag(SaveFlags.StableChargeDate) == true)
                writer.WriteDeltaTime(m_LastStableChargeTime);

            // version 31
            writer.Write(m_STRBonusCap);

            //write flytile array
            writer.Write(FlyIDs.Length);

            for (int i = 0; i < FlyIDs.Length; i++)
            {
                writer.Write(FlyIDs[i]);
            }

            writer.Write(m_CanFlyOver);

            writer.WriteDeltaTime(m_LastStatGain);

            writer.Write((int)m_BoolTable);

            writer.Write(m_Corpse);

            writer.Write(m_CreationTime);

            //writer.WriteMobileList(m_PetStable, true);

            writer.Write(m_CantWalkLand);

            VirtueInfo.Serialize(writer, m_Virtues);

            writer.Write(m_Thirst);
            writer.Write(m_BAC);

            writer.Write(m_ShortTermMurders);
            //writer.Write( m_ShortTermElapse );
            //writer.Write( m_LongTermElapse );

            //writer.Write( m_Followers );
            writer.Write(m_FollowersMax);

            writer.Write(m_MagicDamageAbsorb);

            writer.Write(m_GuildFealty);

            writer.Write(m_Guild);

            writer.Write(m_DisplayGuildTitle);

            writer.Write(m_CanSwim);

            writer.Write(m_Squelched);

            writer.Write(m_Holding);

            writer.Write(m_VirtualArmor);

            writer.Write(m_BaseSoundID);

            writer.Write(m_DisarmReady);
            writer.Write(m_StunReady);

            //Poison.Serialize( m_Poison, writer );

            writer.Write(m_StatCap);

            writer.Write(m_NameHue);

            writer.Write(m_Hunger);

            writer.Write(m_Location);
            writer.Write((int)m_Body);
            writer.Write(m_Name);
            writer.Write(m_GuildTitle);
            writer.Write(m_Criminal);
            writer.Write(m_Kills);
            writer.Write(m_SpeechHue);
            writer.Write(m_EmoteHue);
            writer.Write(m_WhisperHue);
            writer.Write(m_YellHue);
            writer.Write(m_Language);
            writer.Write(m_Female);
            writer.Write(m_Warmode);
            writer.Write(m_Hidden);
            writer.Write((byte)m_Direction);
            writer.Write(m_Hue);
            writer.Write(m_Str);
            writer.Write(m_Dex);
            writer.Write(m_Int);
            writer.Write(m_Hits);
            writer.Write(m_Stam);
            writer.Write(m_Mana);

            writer.Write(m_Map);

            writer.Write(m_Blessed);
            writer.Write(m_Fame);
            writer.Write(m_Karma);
            writer.Write((byte)m_AccessLevel);
            m_Skills.Serialize(writer);

            writer.Write(m_Items.Count);

            for (int i = 0; i < m_Items.Count; ++i)
                writer.Write((Item)m_Items[i]);

            writer.Write(m_Player);
            writer.Write(m_Title);
            writer.Write(m_Profile);
            writer.Write(m_ProfileLocked);
            //writer.Write( m_LightLevel );
            //writer.Write( m_TotalGold );
            //writer.Write( m_TotalWeight );
            writer.Write(m_AutoPageNotify);

            writer.Write(m_LogoutLocation);
            writer.Write(m_LogoutMap);

            writer.Write((byte)m_StrLock);
            writer.Write((byte)m_DexLock);
            writer.Write((byte)m_IntLock);

            if (m_StuckMenuUses != null)
            {
                writer.Write(true);

                writer.Write(m_StuckMenuUses.Length);

                for (int i = 0; i < m_StuckMenuUses.Length; ++i)
                {
                    writer.Write(m_StuckMenuUses[i]);
                }
            }
            else
            {
                writer.Write(false);
            }
        }

        public virtual bool ShouldCheckStatTimers { get { return true; } }

        public virtual void CheckStatTimers()
        {
            if (m_Deleted)
                return;

            if (Hits < HitsMax)
            {
                if (CanRegenHits)
                {
                    if (m_HitsTimer == null)
                        m_HitsTimer = new HitsTimer(this);

                    m_HitsTimer.Start();
                }
                else if (m_HitsTimer != null)
                {
                    m_HitsTimer.Stop();
                }
            }
            else
            {
                Hits = HitsMax;
            }

            if (Stam < StamMax)
            {
                if (CanRegenStam)
                {
                    if (m_StamTimer == null)
                        m_StamTimer = new StamTimer(this);

                    m_StamTimer.Start();
                }
                else if (m_StamTimer != null)
                {
                    m_StamTimer.Stop();
                }
            }
            else
            {
                Stam = StamMax;
            }

            if (Mana < ManaMax)
            {
                if (CanRegenMana)
                {
                    if (m_ManaTimer == null)
                        m_ManaTimer = new ManaTimer(this);

                    m_ManaTimer.Start();
                }
                else if (m_ManaTimer != null)
                {
                    m_ManaTimer.Stop();
                }
            }
            else
            {
                Mana = ManaMax;
            }
        }

        private DateTime m_CreationTime;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public DateTime Created
        {
            get
            {
                return m_CreationTime;
            }
            set
            {
                m_CreationTime = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LightLevel
        {
            get
            {
                return m_LightLevel;
            }
            set
            {
                if (m_LightLevel != value)
                {
                    m_LightLevel = value;

                    CheckLightLevels(false);

                    /*if ( m_NetState != null )
						m_NetState.Send( new PersonalLightLevel( this ) );*/
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public string Profile
        {
            get
            {
                return m_Profile;
            }
            set
            {
                m_Profile = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public bool ProfileLocked
        {
            get
            {
                return m_ProfileLocked;
            }
            set
            {
                m_ProfileLocked = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool Player
        {
            get
            {
                return m_Player;
            }
            set
            {
                m_Player = value;
                InvalidateProperties();

                if (!m_Player && m_Dex <= 100 && m_CombatTimer != null)
                    m_CombatTimer.Priority = TimerPriority.FiftyMS;
                else if (m_CombatTimer != null)
                    m_CombatTimer.Priority = TimerPriority.EveryTick;

                CheckStatTimers();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                m_Title = value;
                InvalidateProperties();
            }
        }

        public static string GetAccessLevelName(AccessLevel level)
        {
            switch (level)
            {
                case AccessLevel.Player: return "a player";
                case AccessLevel.Reporter: return "a reporter";
                case AccessLevel.FightBroker: return "a fight broker";
                case AccessLevel.Counselor: return "a counselor";
                case AccessLevel.GameMaster: return "a game master";
                case AccessLevel.Seer: return "a seer";
                case AccessLevel.Administrator: return "an administrator";
                case AccessLevel.Owner: return "an owner";
                case AccessLevel.System: return "the system";
                default: return "an invalid access level";
            }
        }

        public virtual bool CanPaperdollBeOpenedBy(Mobile from)
        {
            return (Body.IsHuman || Body.IsGhost || IsBodyMod);
        }

        public virtual void GetChildContextMenuEntries(Mobile from, ArrayList list, Item item)
        {
        }

        public virtual void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            if (m_Deleted)
                return;

            if (CanPaperdollBeOpenedBy(from))
                list.Add(new PaperdollEntry(this));

            if (from == this && Backpack != null && CanSee(Backpack) && CheckAlive(false))
                list.Add(new OpenBackpackEntry(this));
        }

        public void Internalize()
        {
            Map = Map.Internal;
        }

        public List<Item> Items
        {
            get
            {
                return m_Items;
            }
        }

        /// <summary>
        /// Overridable. Virtual event invoked when <paramref name="item" /> is <see cref="AddItem">added</see> from the Mobile, such as when it is equiped.
        /// <seealso cref="Items" />
        /// <seealso cref="OnItemRemoved" />
        /// </summary>
        public virtual void OnItemAdded(Item item)
        {
            if (this.Player == false)
                if (item.Origin == Item.Genesis.Unknown)
                    item.Origin = Item.Genesis.Monster;
                else
                    ;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when <paramref name="item" /> is <see cref="RemoveItem">removed</see> from the Mobile.
        /// <seealso cref="Items" />
        /// <seealso cref="OnItemAdded" />
        /// </summary>
        public virtual void OnItemRemoved(Item item)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when <paramref name="item" /> is becomes a child of the Mobile; it's worn or contained at some level of the Mobile's <see cref="Mobile.Backpack">backpack</see> or <see cref="Mobile.BankBox">bank box</see>
        /// <seealso cref="OnSubItemRemoved" />
        /// <seealso cref="OnItemAdded" />
        /// </summary>
        public virtual void OnSubItemAdded(Item item)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when <paramref name="item" /> is removed from the Mobile, its <see cref="Mobile.Backpack">backpack</see>, or its <see cref="Mobile.BankBox">bank box</see>.
        /// <seealso cref="OnSubItemAdded" />
        /// <seealso cref="OnItemRemoved" />
        /// </summary>
        public virtual void OnSubItemRemoved(Item item)
        {
        }

        public virtual void OnItemBounceCleared(Item item)
        {
        }

        public virtual void OnSubItemBounceCleared(Item item)
        {
        }

        public void AddItem(Item item, double newbieChance)
        {
            if (newbieChance <= Utility.RandomDouble())
                item.LootType = LootType.Newbied;
            AddItem(item);
        }

        public void AddItem(Item item, LootType type)
        {
            item.LootType = type;
            AddItem(item);
        }
        public virtual int MaxWeight { get { return int.MaxValue; } }
        public virtual void AddItem(Item item)
        {
            if (item == null || item.Deleted)
                return;

            if (item.Parent == this)
                return;
            else if (item.Parent is Mobile)
                ((Mobile)item.Parent).RemoveItem(item);
            else if (item.Parent is Item)
                ((Item)item.Parent).RemoveItem(item);
            else
                item.SendRemovePacket();

            item.Parent = this;
            item.Map = m_Map;

            m_Items.Add(item);

            if (!(item is BankBox))
            {
                TotalWeight += item.TotalWeight + item.PileWeight;
                TotalGold += item.TotalGold;
            }

            //item.IsIntMapStorage = this.IsIntMapStorage;

            item.Delta(ItemDelta.Update);

            item.OnAdded(this);
            OnItemAdded(item);

            if (item.PhysicalResistance != 0 || item.FireResistance != 0 || item.ColdResistance != 0 || item.PoisonResistance != 0 || item.EnergyResistance != 0)
                UpdateResistances();
        }

        private static IWeapon m_DefaultWeapon;

        public static IWeapon DefaultWeapon
        {
            get
            {
                return m_DefaultWeapon;
            }
            set
            {
                m_DefaultWeapon = value;
            }
        }

        public Item RequestItem(Type type)
        {
            // see if they are carrying one of these
            foreach (Item ix in Items)
                if (ix.GetType() == type)
                    return ix;

            return null;
        }

        public virtual bool ProcessItem(Item item)
        {
            return false;
        }

        public void RemoveItem(Item item)
        {
            if (item == null || m_Items == null)
                return;

            if (m_Items.Contains(item))
            {
                item.SendRemovePacket();

                int oldCount = m_Items.Count;

                m_Items.Remove(item);

                if (!(item is BankBox))
                {
                    TotalWeight -= item.TotalWeight + item.PileWeight;
                    TotalGold -= item.TotalGold;
                }

                //item.IsIntMapStorage = false;

                item.Parent = null;

                item.OnRemoved(this);
                OnItemRemoved(item);

                if (item.PhysicalResistance != 0 || item.FireResistance != 0 || item.ColdResistance != 0 || item.PoisonResistance != 0 || item.EnergyResistance != 0)
                    UpdateResistances();
            }
        }

        public virtual void Animate(int action, int frameCount, int repeatCount, bool forward, bool repeat, int delay)
        {
            Map map = m_Map;

            if (map != null)
            {
                ProcessDelta();

                Packet p = null;

                IPooledEnumerable eable = map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state.Mobile.CanSee(this))
                    {
                        state.Mobile.ProcessDelta();

                        if (p == null)
                            p = Packet.Acquire(new MobileAnimation(this, action, frameCount, repeatCount, forward, repeat, delay));

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void SendSound(int soundID)
        {
            if (soundID != -1 && m_NetState != null)
                Send(new PlaySound(soundID, this));
        }

        public void SendSound(int soundID, IPoint3D p)
        {
            if (soundID != -1 && m_NetState != null)
                Send(new PlaySound(soundID, p));
        }

        public virtual void PlaySound(int soundID)
        {
            if (soundID == -1)
                return;

            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state.Mobile.CanSee(this))
                    {
                        if (p == null)
                            p = Packet.Acquire(new PlaySound(soundID, this));

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }

        }
        public virtual Mobile Dupe()
        {
            Mobile mobile = null;
            mobile = (Mobile)Activator.CreateInstance(this.GetType());


            Utility.CopyProperties(mobile, this);           // copy properties

            ////mobile.Hidden = Hidden;                     // handled by booltable below
            //mobile.Direction = Direction;
            //mobile.Hue = Hue;
            ////mobile.ItemID = ItemID; ??
            //mobile.Location = Location;
            //mobile.Name = Name;
            //mobile.Map = Map;

            //mobile.BoolTable = m_BoolTable;               // copy over all our custom flags

            Server.Skills this_skills = this.Skills;        // skills
            Server.Skills mobile_skills = mobile.Skills;
            for (int i = 0; i < this_skills.Length; ++i)
                mobile_skills[i].Base = this_skills[i].Base;

            //mobile.Body = Body;                           // body

            //mobile.RawDex = this.RawDex;
            //mobile.RawInt = this.RawInt;
            //mobile.RawStr = this.RawStr;

            //mobile.Stam = this.Stam;
            //mobile.Mana = this.Mana;
            //mobile.Hits = this.Hits;

            //mobile.Karma = this.Karma;
            //mobile.Fame = this.Fame;

            // backpack
            if (Backpack != null)
            {
                Backpack pack = (Backpack)Utility.DeepDupe(Backpack);
                Item old_back = mobile.FindItemOnLayer(Layer.Backpack);
                if (old_back != null)
                    mobile.RemoveItem(old_back);

                pack.Movable = false;
                mobile.AddItem(pack);
            }

            // mobile.Delta(MobileDelta.??); // unsure

            return mobile;
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Counselor)]
        public Skills Skills
        {
            get
            {
                return m_Skills;
            }
            set
            {
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public AccessLevel AccessLevel
        {
            get
            {
                return AccessLevelInternal;
            }
            set
            {
                // Don't allow changing access level to Owner or System.
                if (value == AccessLevel.Owner || value == AccessLevel.System) return;

                AccessLevelInternal = value;
            }
        }

        // do not make a command, this is for programmatic use only.
        public AccessLevel AccessLevelInternal
        {
            get
            {
                return m_AccessLevel;
            }
            set
            {
                AccessLevel oldValue = m_AccessLevel;

                if (oldValue != value)
                {
                    m_AccessLevel = value;

                    Delta(MobileDelta.Noto);
                    InvalidateProperties();

                    SendMessage("Your access level has been changed. You are now {0}.", GetAccessLevelName(value));

                    ClearScreen();
                    SendEverything();

                    OnAccessLevelChanged(oldValue);
                }
            }
        }

        public virtual void OnAccessLevelChanged(AccessLevel oldLevel)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Fame
        {
            get
            {
                return m_Fame;
            }
            set
            {
                int oldValue = m_Fame;

                if (oldValue != value)
                {
                    m_Fame = value;

                    if (ShowFameTitle && (m_Player || m_Body.IsHuman) && (oldValue >= 10000) != (value >= 10000))
                        InvalidateProperties();

                    OnFameChange(oldValue);
                }
            }
        }

        public virtual void OnFameChange(int oldValue)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Karma
        {
            get
            {
                return m_Karma;
            }
            set
            {
                int old = m_Karma;

                if (old != value)
                {
                    m_Karma = value;
                    OnKarmaChange(old);
                }
            }
        }

        public virtual void OnKarmaChange(int oldValue)
        {
        }

        // Mobile did something which should unhide him
        public virtual void RevealingAction()
        {
            if (m_Hidden && m_AccessLevel == AccessLevel.Player)
                Hidden = false;

            DisruptiveAction(); // Anything that unhides you will also distrupt meditation
        }

        public void SayTo(Mobile to, bool ascii, string text)
        {
            //PrivateOverheadMessage(MessageType.Regular, m_SpeechHue, ascii, text, to.NetState);
            SayTo(to, ascii, m_SpeechHue, text);
        }
        public void SayTo(Mobile to, bool ascii, int hue, string text)
        {
            PrivateOverheadMessage(MessageType.Regular, hue, ascii, text, to.NetState);
        }
        public void SayTo(Mobile to, string text)
        {
            SayTo(to, false, text);
        }

        public void SayTo(Mobile to, string format, params object[] args)
        {
            SayTo(to, false, string.Format(format, args));
        }

        public void SayTo(Mobile to, bool ascii, string format, params object[] args)
        {
            SayTo(to, ascii, string.Format(format, args));
        }

        public void SayTo(Mobile to, int number)
        {
            to.Send(new MessageLocalized(m_Serial, Body, MessageType.Regular, m_SpeechHue, 3, number, Name, ""));
        }

        public void SayTo(Mobile to, int number, string args)
        {
            to.Send(new MessageLocalized(m_Serial, Body, MessageType.Regular, m_SpeechHue, 3, number, Name, args));
        }

        public void Say(bool ascii, string text)
        {
            PublicOverheadMessage(MessageType.Regular, m_SpeechHue, ascii, text);
        }

        public void Say(string text)
        {
            PublicOverheadMessage(MessageType.Regular, m_SpeechHue, false, text);
        }

        public void Say(string format, params object[] args)
        {
            Say(string.Format(format, args));
        }

        public void Say(int number, AffixType type, string affix, string args)
        {
            PublicOverheadMessage(MessageType.Regular, m_SpeechHue, number, type, affix, args);
        }

        public void Say(int number)
        {
            Say(number, "");
        }

        public void Say(int number, string args)
        {
            PublicOverheadMessage(MessageType.Regular, m_SpeechHue, number, args);
        }

        public void Emote(string text)
        {
            PublicOverheadMessage(MessageType.Emote, m_EmoteHue, false, text);
        }

        public void Emote(string format, params object[] args)
        {
            Emote(string.Format(format, args));
        }

        public void Emote(int number)
        {
            Emote(number, "");
        }

        public void Emote(int number, string args)
        {
            PublicOverheadMessage(MessageType.Emote, m_EmoteHue, number, args);
        }

        public void Whisper(string text)
        {
            PublicOverheadMessage(MessageType.Whisper, m_WhisperHue, false, text);
        }

        public void Whisper(string format, params object[] args)
        {
            Whisper(string.Format(format, args));
        }

        public void Whisper(int number)
        {
            Whisper(number, "");
        }

        public void Whisper(int number, string args)
        {
            PublicOverheadMessage(MessageType.Whisper, m_WhisperHue, number, args);
        }

        public void Yell(string text)
        {
            PublicOverheadMessage(MessageType.Yell, m_YellHue, false, text);
        }

        public void Yell(string format, params object[] args)
        {
            Yell(string.Format(format, args));
        }

        public void Yell(int number)
        {
            Yell(number, "");
        }

        public void Yell(int number, string args)
        {
            PublicOverheadMessage(MessageType.Yell, m_YellHue, number, args);
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool Blessed
        {
            get
            {
                return m_Blessed;
            }
            set
            {
                if (m_Blessed != value)
                {
                    m_Blessed = value;
                    if (Core.RuleSets.AnyAIShardRules())
                        Delta(MobileDelta.Flags);
                    else
                        Delta(MobileDelta.HealthbarYellow);

                    if (m_Blessed == true)
                    {
                        string text = string.Empty;
                        LogHelper logger = new LogHelper("Blessed creation.log", overwrite: false, sline: true);
                        text = logger.Format(LogType.Mobile, this, string.Format("set to blessed"));
                        logger.Log(text);
                        logger.Finish();
                        Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Owner, 0x482, text);
                    }
                }
            }
        }

        public void SendRemovePacket()
        {
            SendRemovePacket(true);
        }

        public void SendRemovePacket(bool everyone)
        {
            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state != m_NetState && (everyone || !state.Mobile.CanSee(this)))
                    {
                        if (p == null)
                            p = this.RemovePacket;

                        state.Send(p);
                    }
                }

                eable.Free();
            }
        }

        public void ClearScreen()
        {
            NetState ns = m_NetState;

            if (m_Map != null && ns != null)
            {
                IPooledEnumerable eable = m_Map.GetObjectsInRange(m_Location, Core.GlobalMaxUpdateRange);

                foreach (object o in eable)
                {
                    if (o is Mobile)
                    {
                        Mobile m = (Mobile)o;

                        if (m != this && Utility.InUpdateRange(m_Location, m.m_Location))
                            ns.Send(m.RemovePacket);
                    }
                    else if (o is Item)
                    {
                        Item item = (Item)o;

                        if (InRange(item.Location, item.GetUpdateRange(this)))
                            ns.Send(item.RemovePacket);
                    }
                }

                eable.Free();
            }
        }

        public bool Send(Packet p)
        {
            return Send(p, false);
        }

        public bool Send(Packet p, bool throwOnOffline)
        {
            if (m_NetState != null)
            {
                m_NetState.Send(p);
                return true;
            }
            else if (throwOnOffline)
            {
                throw new MobileNotConnectedException(this, "Packet could not be sent.");
            }
            else
            {
                return false;
            }
        }

        public bool SendHuePicker(HuePicker p)
        {
            return SendHuePicker(p, false);
        }

        public bool SendHuePicker(HuePicker p, bool throwOnOffline)
        {
            if (m_NetState != null)
            {
                p.SendTo(m_NetState);
                return true;
            }
            else if (throwOnOffline)
            {
                throw new MobileNotConnectedException(this, "Hue picker could not be sent.");
            }
            else
            {
                return false;
            }
        }

        // wea: Correctly remove gumps from the NetState object on CloseGump() (integrated RunUO fix).
        public Gump FindGump(Type type)
        {
            NetState ns = m_NetState;

            if (ns != null)
            {
                foreach (Gump gump in ns.Gumps)
                {
                    if (type.IsAssignableFrom(gump.GetType()))
                    {
                        return gump;
                    }
                }
            }

            return null;
        }

        public bool CloseGump(Type type)
        {
            if (m_NetState != null)
            {
                Gump gump = FindGump(type);
                if (gump != null)
                {
                    m_NetState.Send(new CloseGump(gump.TypeID, 0));
                    m_NetState.RemoveGump(gump);
                    gump.OnServerClose(m_NetState);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// CloseGumps - fix for newer clients not sending back closegump calls...
        /// </summary>
        /// <param name="type">type of gump to close</param>
        /// <returns></returns>
        public int CloseGumps(Type type)
        {
            int numberRemoved = 0;
            NetState ns = m_NetState;

            if (ns != null)
            {
                List<Gump> gumps = (List<Gump>)ns.Gumps;
                List<Gump> toremove = new List<Gump>();

                for (int i = 0; i < gumps.Count; ++i)
                {
                    if (gumps[i].GetType() == type)
                    {
                        ns.Send(new CloseGump(gumps[i].TypeID, 0));
                        toremove.Add(gumps[i]);
                        gumps[i].OnServerClose(m_NetState);
                    }
                }

                for (int i = 0; i < toremove.Count; ++i)
                {
                    try
                    {
                        gumps.Remove(toremove[i]);
                        numberRemoved++;
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }

            return numberRemoved;
        }

        public bool CloseAllGumps()
        {
            NetState ns = m_NetState;

            if (ns != null)
            {
                List<Gump> gumps = new List<Gump>(ns.Gumps);

                ns.ClearGumps();

                foreach (Gump gump in gumps)
                {
                    ns.Send(new CloseGump(gump.TypeID, 0));
                    gump.OnServerClose(ns);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool HasGump(Type type)
        {
            return HasGump(type, false);
        }

        public bool HasGump(Type type, bool throwOnOffline)
        {
            NetState ns = m_NetState;

            if (ns != null)
            {
                bool contains = false;
                //GumpCollection gumps = ns.Gumps;
                List<Gump> gumps = new List<Gump>(ns.Gumps);

                for (int i = 0; !contains && i < gumps.Count; ++i)
                    contains = (gumps[i].GetType() == type);

                return contains;
            }
            else if (throwOnOffline)
            {
                throw new MobileNotConnectedException(this, "Mobile is not connected.");
            }
            else
            {
                return false;
            }
        }

        public bool SendGump(Gump g)
        {
            return SendGump(g, false);
        }

        public bool SendGump(Gump g, bool throwOnOffline)
        {
            if (m_NetState != null)
            {
                g.SendTo(m_NetState);
                return true;
            }
            else if (throwOnOffline)
            {
                throw new MobileNotConnectedException(this, "Gump could not be sent.");
            }
            else
            {
                return false;
            }
        }

        public bool SendMenu(IMenu m)
        {
            return SendMenu(m, false);
        }

        public bool SendMenu(IMenu m, bool throwOnOffline)
        {
            if (m_NetState != null)
            {
                m.SendTo(m_NetState);
                return true;
            }
            else if (throwOnOffline)
            {
                throw new MobileNotConnectedException(this, "Menu could not be sent.");
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Overridable. Event invoked before the Mobile says something.
        /// <seealso cref="DoSpeech" />
        /// </summary>
        public virtual void OnSaid(SpeechEventArgs e)
        {
            if (m_Squelched)
            {
#if old_Squelched
				this.SendLocalizedMessage(500168); // You can not say anything, you have been squelched.
#else          // don't tip our hand, just let them think everyone can hear them, but are ignoring them.
                // nearby staff will see what is said though, like this: "Squelched: the spoken text"
                this.LocalOverheadMessage(e.Type, e.Hue, true, e.Speech);

                bool ascii = true;
                if (m_Map != null)
                {
                    Packet p = null;

                    IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                    foreach (NetState state in eable)
                    {   // tell staff this has been handles
                        if (state != m_NetState && state.Mobile.CanSee(this) && state.Mobile.AccessLevel > AccessLevel.Player)
                        {
                            if (p == null)
                            {
                                if (ascii)
                                    p = new AsciiMessage(m_Serial, Body, e.Type, e.Hue, 3, Name, string.Format("Squelched: {0}", e.Speech));
                                else
                                    p = new UnicodeMessage(m_Serial, Body, e.Type, e.Hue, 3, Language, Name, e.Speech);

                                p.Acquire();
                            }

                            state.Send(p);
                        }
                    }

                    Packet.Release(p);

                    eable.Free();
                }
#endif
                e.Blocked = true;
            }

            if (!e.Blocked)
                RevealingAction();
        }

        public virtual bool HandlesOnSpeech(Mobile from)
        {
            return false;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile hears speech. This event will only be invoked if <see cref="HandlesOnSpeech" /> returns true.
        /// <seealso cref="DoSpeech" />
        /// </summary>
        public virtual void OnSpeech(SpeechEventArgs e)
        {
        }
        #region OnQuery
        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile hears speech. This event will only be invoked if <see cref="HandlesOnSpeech" /> returns true.
        /// <seealso cref="DoSpeech" />
        /// </summary>
        public virtual void OnQuery(SpeechEventArgs e)
        {
        }
        private void DoQuery(SpeechEventArgs e, List<IEntity> list)
        {
            List<Mobile> trimmedList = new();
            // locate the named mobile
            foreach (IEntity ie in list)
                if (ie is Mobile m)  // it's an item
                {
                    if (m.HandlesQuery != null && m.HandlesQuery.Any(e.Speech.Contains))
                        if (e.WasNamed(m))
                        {
                            m.OnQuery(e);
                            e.Handled = true;
                            return;
                        }
                        else
                            trimmedList.Add(m);
                }

            // okay, no mobile was named, have all answer
            foreach (Mobile m in trimmedList)
                m.OnQuery(e);

            return;
        }
        public virtual string[] HandlesQuery { get { return null; } }
        #endregion OnQuery
#if true
        public void SendEverything()
        {
            NetState ns = m_NetState;

            if (m_Map != null && ns != null)
            {
                IPooledEnumerable<IEntity> eable = m_Map.GetObjectsInRange(m_Location, Core.GlobalMaxUpdateRange);
                Packet removeThis = null;
                foreach (IEntity o in eable)
                {
                    if (o is Item)
                    {
                        Item item = (Item)o;

                        if (CanSee(item) && InRange(item.Location, item.GetUpdateRange(this)))
                            item.SendInfoTo(ns);
                    }
                    else if (o is Mobile)
                    {
                        Mobile m = (Mobile)o;

                        if (CanSee(m) && Utility.InUpdateRange(m_Location, m.m_Location))
                        {
                            ns.Send(MobileIncoming.Create(ns, this, m));

                            if (ns.StygianAbyss)
                            {
                                if (m.Poisoned)
                                    ns.Send(new HealthbarPoison(m));

                                if (m.Blessed || m.YellowHealthbar)
                                    ns.Send(new HealthbarYellow(m));
                            }

                            if (m.IsDeadBondedPet)
                                ns.Send(new BondedStatus(0, m.m_Serial, 1));

                            if (ObjectPropertyList.Enabled)
                            {
                                ns.Send(m.OPLPacket);

                                //foreach ( Item item in m.m_Items )
                                //	ns.Send( item.OPLPacket );
                            }
                        }
                    }
                }

                eable.Free();
            }
        }

        public void HideHidden()
        {
            const int GetMaxUpdateRange = 18;
            IPooledEnumerable eable = this.Map.GetItemsInRange(this.Location, range: GetMaxUpdateRange);
            foreach (Item thing in eable)
                if (!thing.GetItemBool(ItemBoolTable.Visible))
                    thing.Hide();
            eable.Free();
        }
#else
        public void SendEverything()
        {
            NetState ns = m_NetState;

            if (m_Map != null && ns != null)
            {
                IPooledEnumerable eable = m_Map.GetObjectsInRange(m_Location, Core.GlobalMaxUpdateRange);

                foreach (object o in eable)
                {
                    if (o is Item)
                    {
                        Item item = (Item)o;

                        if (CanSee(item) && InRange(item.Location, item.GetUpdateRange(this)))
                            item.SendInfoTo(ns);
                    }
                    else if (o is Mobile)
                    {
                        Mobile m = (Mobile)o;

                        if (CanSee(m) && Utility.InUpdateRange(m_Location, m.m_Location))
                        {
                            ns.Send(new MobileIncoming(this, m));

                            if (m.IsDeadBondedPet)
                                ns.Send(new BondedStatus(0, m.m_Serial, 1));

                            if (ObjectPropertyList.Enabled)
                            {
                                ns.Send(m.OPLPacket);

                                //foreach ( Item item in m.m_Items )
                                //	ns.Send( item.OPLPacket );
                            }
                        }
                    }
                }

                eable.Free();
            }
        }
#endif
        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
#if true
        public Map Map
        {
            get
            {
                return m_Map;
            }
            set
            {
                if (m_Deleted)
                    return;

                if (m_Map != value)
                {
                    if (m_NetState != null)
                        m_NetState.ValidateAllTrades();

                    Map oldMap = m_Map;

                    if (m_Map != null)
                    {
                        m_Map.OnLeave(this);

                        ClearScreen();
                        SendRemovePacket();
                    }

                    for (int i = 0; i < m_Items.Count; ++i)
                        m_Items[i].Map = value;

                    m_Map = value;

                    UpdateRegion();

                    if (m_Map != null)
                        m_Map.OnEnter(this);

                    NetState ns = m_NetState;

                    if (ns != null && m_Map != null)
                    {
                        ns.Sequence = 0;
                        ns.Send(new MapChange(this));
                        ns.Send(new MapPatches());
                        ns.Send(SeasonChange.Instantiate(GetSeason(), true));

                        if (ns.StygianAbyss)
                            ns.Send(new MobileUpdate(this));
                        else
                            ns.Send(new MobileUpdateOld(this));

                        ClearFastwalkStack();
                    }

                    if (ns != null)
                    {
                        if (m_Map != null)
                            ns.Send(new ServerChange(this, m_Map));

                        ns.Sequence = 0;
                        ClearFastwalkStack();

                        ns.Send(MobileIncoming.Create(ns, this, this));

                        if (ns.StygianAbyss)
                        {
                            ns.Send(new MobileUpdate(this));
                            CheckLightLevels(true);
                            ns.Send(new MobileUpdate(this));
                        }
                        else
                        {
                            ns.Send(new MobileUpdateOld(this));
                            CheckLightLevels(true);
                            ns.Send(new MobileUpdateOld(this));
                        }
                    }

                    SendEverything();
                    SendIncomingPacket();

                    if (ns != null)
                    {
                        ns.Sequence = 0;
                        ClearFastwalkStack();

                        ns.Send(MobileIncoming.Create(ns, this, this));

                        if (ns.StygianAbyss)
                        {
                            ns.Send(SupportedFeatures.Instantiate(ns));
                            ns.Send(new MobileUpdate(this));
                            ns.Send(new MobileAttributes(this));
                        }
                        else
                        {
                            ns.Send(SupportedFeatures.Instantiate(ns));
                            ns.Send(new MobileUpdateOld(this));
                            ns.Send(new MobileAttributes(this));
                        }
                    }

                    OnMapChange(oldMap);
                }
            }
        }
#else
        public Map Map
        {
            get
            {
                return m_Map;
            }
            set
            {
                if (m_Deleted)
                    return;

                if (m_Map != value)
                {
                    if (m_NetState != null)
                        m_NetState.ValidateAllTrades();

                    Map oldMap = m_Map;

                    if (m_Map != null)
                    {
                        m_Map.OnLeave(this);

                        ClearScreen();
                        SendRemovePacket();
                    }

                    for (int i = 0; i < m_Items.Count; ++i)
                        ((Item)m_Items[i]).Map = value;

                    m_Map = value;

                    if (m_Map != null)
                        m_Map.OnEnter(this);

                    m_Region.InternalExit(this);

                    NetState ns = m_NetState;

                    if (m_Map != null)
                    {
                        Region old = m_Region;
                        m_Region = Region.Find(m_Location, m_Map);
                        OnRegionChange(old, m_Region);
                        m_Region.InternalEnter(this);

                        if (ns != null && m_Map != null)
                        {
                            ns.Sequence = 0;
                            ns.Send(new MapChange(this));
                            ns.Send(new MapPatches());
                            ns.Send(SeasonChange.Instantiate(GetSeason(), true));
                            ns.Send(new MobileUpdate(this));
                            ClearFastwalkStack();
                        }
                    }

                    if (ns != null)
                    {
                        if (m_Map != null)
                            Send(new ServerChange(this, m_Map));

                        ns.Sequence = 0;
                        ClearFastwalkStack();

                        Send(new MobileIncoming(this, this));
                        Send(new MobileUpdate(this));
                        CheckLightLevels(true);
                        Send(new MobileUpdate(this));
                    }

                    SendEverything();
                    SendIncomingPacket();

                    if (ns != null)
                    {
                        ns.Sequence = 0;
                        ClearFastwalkStack();

                        Send(new MobileIncoming(this, this));
                        Send(SupportedFeatures.Instantiate(ns.Account));
                        Send(new MobileUpdate(this));
                        Send(new MobileAttributes(this));
                    }

                    OnMapChange(oldMap);
                }
            }
        }
#endif
        public void ForceRegionReEnter(bool Exit)
        {//forces to find the region again and enter it
            if (m_Deleted)
                return;

            Region n = Region.Find(m_Location, m_Map);
            if (n != m_Region)
            {
                if (Exit)
                    m_Region.InternalExit(this);
                m_Region = n;
                OnRegionChange(n, m_Region);
                m_Region.InternalEnter(this);
                CheckLightLevels(false);
            }
        }
        public void UpdateRegion()
        {
            if (m_Deleted)
                return;

            Region newRegion = Region.Find(m_Location, m_Map);

            if (newRegion != m_Region)
            {
                Region.OnRegionChange(this, m_Region, newRegion);
                OnRegionChange(m_Region, newRegion);
                m_Region = newRegion;
                //OnRegionChange(m_Region, newRegion);
            }
        }
        /// <summary>
        /// Overridable. Virtual event invoked when <see cref="Map" /> changes.
        /// </summary>
        protected virtual void OnMapChange(Map oldMap)
        {
        }
        #region Beneficial Checks/Actions
        public virtual bool CanBeBeneficial(Mobile target)
        {
            return CanBeBeneficial(target, true, false);
        }

        public virtual bool CanBeBeneficial(Mobile target, bool message)
        {
            return CanBeBeneficial(target, message, false);
        }

        public virtual bool CanBeBeneficial(Mobile target, bool message, bool allowDead)
        {
            if (target == null)
                return false;

            if (m_Deleted || target.m_Deleted || !Alive || IsDeadBondedPet || (!allowDead && (!target.Alive || IsDeadBondedPet)))
            {
                if (message)
                    SendLocalizedMessage(1001017); // You can not perform beneficial acts on your target.

                return false;
            }

            if (target == this)
                return true;

            if ( /*m_Player &&*/ !Region.AllowBenificial(this, target))
            {
                // TODO: Pets
                //if ( !(target.m_Player || target.Body.IsHuman || target.Body.IsAnimal) )
                //{
                if (message)
                    SendLocalizedMessage(1001017); // You can not perform beneficial acts on your target.

                return false;
                //}
            }

            return true;
        }

        public virtual bool IsBeneficialCriminal(Mobile target)
        {
            if (this == target)
                return false;

            int n = Notoriety.Compute(this, target);

            return (n == Notoriety.Criminal || n == Notoriety.Murderer);
        }

        /// <summary>
        /// Overridable. Event invoked when the Mobile <see cref="DoBeneficial">does a beneficial action</see>.
        /// </summary>
        public virtual void OnBeneficialAction(Mobile target, bool isCriminal)
        {
            if (isCriminal)
            {
                if (!Core.RuleSets.SiegeStyleRules())
                    CriminalAction(false);
                else
                {
                    if (target.Criminal)
                        CriminalAction(message: false);
                    else if (target.Evil)
                    {
                        // they are and Evil outside of town, so make the healer crim
                        Criminal = true;
                    }
                    else if (target.Red)
                    {
                        // Healing a red character is a criminal offense, but will not get you guard whacked.
                        GuardWackable = false;
                        Criminal = true;
                    }
                }
            }

            /* We will assume for now that what the document meant was that healing them outside of town is a crim action (while they are red.)
			// BeneficialActions on Evils doesn't seem to fit in with the normal notoriety system
			// healing Evil is a criminal offense, but they are neither criminals (gray) or murders in town.
			else if (target.Evil)
			{
				// they are and Evil inside of town, so make the healer crim
				Criminal = true;
			}*/
        }

        public virtual void DoBeneficial(Mobile target)
        {
            if (target == null)
                return;

            OnBeneficialAction(target, IsBeneficialCriminal(target));

            Region.OnBenificialAction(this, target);
            target.Region.OnGotBenificialAction(this, target);
        }

        public virtual bool BeneficialCheck(Mobile target)
        {
            if (CanBeBeneficial(target, true))
            {
                DoBeneficial(target);
                return true;
            }

            return false;
        }
        #endregion

        #region Harmful Checks/Actions
        public virtual bool CanBeHarmful(Mobile target)
        {
            return CanBeHarmful(target, true);
        }

        public virtual bool CanBeHarmful(Mobile target, bool message)
        {
            return CanBeHarmful(target, message, false);
        }

        // wea: added overloaded version to handle instances where damage can be dealt 
        // after the incurrer's death

        public virtual bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness)
        {
            return CanBeHarmful(target, message, ignoreOurBlessedness, false);
        }

        public virtual bool CanBeHarmful(Mobile target, bool message, bool ignoreOurBlessedness, bool ignoreOurDeadness)
        {
            if (target == null)
                return false;

            if (m_Deleted ||
                (!ignoreOurDeadness && !Alive) ||
                (!ignoreOurBlessedness && m_Blessed) ||
                target.m_Deleted ||
                target.m_Blessed ||
                target.IsInvulnerable ||
                IsDeadBondedPet ||
                !target.Alive ||
                target.IsDeadBondedPet)
            {
                if (message)
                    SendLocalizedMessage(1001018); // You can not perform negative acts on your target.

                return false;
            }

            if (target == this)
                return true;

            // TODO: Pets
            if ( /*m_Player &&*/ !Region.AllowHarmful(this, target))//(target.m_Player || target.Body.IsHuman) && !Region.AllowHarmful( this, target )  )
            {
                if (message)
                    SendLocalizedMessage(1001018); // You can not perform negative acts on your target.

                return false;
            }

            return true;
        }
        #endregion Harmful Checks/Actions

        public virtual int Luck
        {
            get { return 0; }
        }

        public virtual bool IsHarmfulCriminal(Mobile target)
        {
            if (this == target)
                return false;

            return (Notoriety.Compute(this, target) == Notoriety.Innocent);
        }

        public bool RestartCombatTimers()
        {
            if (m_Combatant == null)
            {
                if (m_ExpireCombatant != null)
                {
                    m_ExpireCombatant.Stop();
                    m_ExpireCombatant.Flush();
                }

                if (m_CombatTimer != null)
                {
                    m_CombatTimer.Stop();
                    m_CombatTimer.Flush();
                }

                m_ExpireCombatant = null;
                m_CombatTimer = null;
            }
            else
            {
                if (m_ExpireCombatant != null)
                {
                    m_ExpireCombatant.Stop();
                    m_ExpireCombatant.Flush();
                }
                m_ExpireCombatant = new ExpireCombatantTimer(this);
                m_ExpireCombatant.Start();

                if (m_CombatTimer != null)
                {
                    m_CombatTimer.Stop();
                    m_CombatTimer.Flush();
                }
                m_CombatTimer = new CombatTimer(this);
                m_CombatTimer.Start();
            }

            return m_CombatTimer != null && m_ExpireCombatant != null;
        }

        /// <summary>
        /// Overridable. Event invoked when the Mobile <see cref="DoHarmful">does a harmful action</see>.
        /// </summary>
        public virtual void OnHarmfulAction(Mobile target, bool isCriminal, object source = null)
        {
            if (isCriminal)
                CriminalAction(false);
        }

        public virtual void DoHarmful(Mobile target)
        {
            DoHarmful(target, false);
        }

        public virtual void DoHarmful(Mobile target, bool indirect, object source = null)
        {
            if (target == null)
                return;

            bool isCriminal = IsHarmfulCriminal(target);

            OnHarmfulAction(target, isCriminal, source);
            target.AggressiveAction(this, isCriminal, source);

            Region.OnDidHarmful(this, target);
            target.Region.OnGotHarmful(this, target);

            if (!indirect)
                Combatant = target;

            if (m_ExpireCombatant == null)
                m_ExpireCombatant = new ExpireCombatantTimer(this);
            else
                m_ExpireCombatant.Stop();

            m_ExpireCombatant.Start();
        }

        public virtual bool HarmfulCheck(Mobile target)
        {
            if (CanBeHarmful(target))
            {
                DoHarmful(target);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets <see cref="System.Collections.ArrayList">a list</see> of all <see cref="StatMod">StatMod's</see> currently active for the Mobile.
        /// </summary>
        public ArrayList StatMods { get { return m_StatMods; } }

        protected void RemoveSkillModsOfType(Type type)
        {
            ArrayList al = new ArrayList();
            foreach (object o in m_SkillMods)
            {
                if (o.GetType() == type)
                {
                    al.Add(o);
                }
            }
            foreach (object o in al)
            {
                m_SkillMods.Remove(o);
            }
        }

        public void ClearStatMods()
        {
            while (m_StatMods.Count > 0)
            {
                StatMod check = (StatMod)m_StatMods[0];

                m_StatMods.RemoveAt(0);
                CheckStatTimers();
                Delta(MobileDelta.Stat | GetStatDelta(check.Type));
            }

            if (StatChange != null)
                StatChange(this, StatType.All);
        }

        public bool RemoveStatMod(string name)
        {
            for (int i = 0; i < m_StatMods.Count; ++i)
            {
                StatMod check = (StatMod)m_StatMods[i];

                if (check.Name == name)
                {
                    m_StatMods.RemoveAt(i);
                    CheckStatTimers();
                    Delta(MobileDelta.Stat | GetStatDelta(check.Type));
                    if (StatChange != null)
                        StatChange(this, check.Type);
                    return true;
                }
            }

            return false;
        }

        public StatMod GetStatMod(string name)
        {
            for (int i = 0; i < m_StatMods.Count; ++i)
            {
                StatMod check = (StatMod)m_StatMods[i];

                if (check.Name == name)
                    return check;
            }

            return null;
        }

        public void AddStatMod(StatMod mod)
        {
            for (int i = 0; i < m_StatMods.Count; ++i)
            {
                StatMod check = (StatMod)m_StatMods[i];

                if (check.Name == mod.Name)
                {
                    Delta(MobileDelta.Stat | GetStatDelta(check.Type));
                    m_StatMods.RemoveAt(i);
                    break;
                }
            }

            m_StatMods.Add(mod);
            Delta(MobileDelta.Stat | GetStatDelta(mod.Type));
            CheckStatTimers();

            if (StatChange != null)
                StatChange(this, mod.Type);
        }

        private MobileDelta GetStatDelta(StatType type)
        {
            MobileDelta delta = 0;

            if ((type & StatType.Str) != 0)
                delta |= MobileDelta.Hits;

            if ((type & StatType.Dex) != 0)
                delta |= MobileDelta.Stam;

            if ((type & StatType.Int) != 0)
                delta |= MobileDelta.Mana;

            return delta;
        }

        /// <summary>
        /// Computes the total modified offset for the specified stat type. Expired <see cref="StatMod" /> instances are removed.
        /// </summary>
        public double GetStatOffset(StatType type)
        {
            double offset = 0;

            // 12,7,2024: Adam, we had a load crash because m_StatMods was null, so I added this check.
            // Update, fixed
            //if (m_StatMods != null)
            for (int i = 0; i < m_StatMods.Count; ++i)
            {
                StatMod mod = (StatMod)m_StatMods[i];

                if (mod.HasElapsed())
                {
                    m_StatMods.RemoveAt(i);
                    Delta(MobileDelta.Stat | GetStatDelta(mod.Type));
                    CheckStatTimers();
                    if (StatChange != null)
                        StatChange(this, mod.Type);

                    --i;
                }
                else if ((mod.Type & type) != 0)
                {
                    offset += mod.Offset;
                }
            }
            //else
            //;

            return offset;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the <see cref="RawStr" /> changes.
        /// <seealso cref="RawStr" />
        /// <seealso cref="OnRawStatChange" />
        /// </summary>
        public virtual void OnRawStrChange(int oldValue)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when <see cref="RawDex" /> changes.
        /// <seealso cref="RawDex" />
        /// <seealso cref="OnRawStatChange" />
        /// </summary>
        public virtual void OnRawDexChange(int oldValue)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the <see cref="RawInt" /> changes.
        /// <seealso cref="RawInt" />
        /// <seealso cref="OnRawStatChange" />
        /// </summary>
        public virtual void OnRawIntChange(int oldValue)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the <see cref="RawStr" />, <see cref="RawDex" />, or <see cref="RawInt" /> changes.
        /// <seealso cref="OnRawStrChange" />
        /// <seealso cref="OnRawDexChange" />
        /// <seealso cref="OnRawIntChange" />
        /// </summary>
        public virtual void OnRawStatChange(StatType stat, int oldValue)
        {
            if (StatChange != null)
                StatChange(this, stat);
        }

        /// <summary>
        /// Gets or sets the base, unmodified, strength of the Mobile. Ranges from 1 to 65000, inclusive.
        /// <seealso cref="Str" />
        /// <seealso cref="StatMod" />
        /// <seealso cref="OnRawStrChange" />
        /// <seealso cref="OnRawStatChange" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int RawStr
        {
            get
            {
                return m_Str;
            }
            set
            {
                if (value < 1) value = 1;
                else if (value > 65000) value = 65000;

                if (m_Str != value)
                {
                    int oldValue = m_Str;

                    m_Str = value;
                    Delta(MobileDelta.Stat | MobileDelta.Hits);

                    if (Hits < HitsMax)
                    {
                        if (m_HitsTimer == null)
                            m_HitsTimer = new HitsTimer(this);

                        m_HitsTimer.Start();
                    }
                    else if (Hits > HitsMax)
                    {
                        Hits = HitsMax;
                    }

                    OnRawStrChange(oldValue);
                    OnRawStatChange(StatType.Str, oldValue);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int STRBonusCap
        {
            get { return m_STRBonusCap; }
            set { m_STRBonusCap = value; }
        }

        /// <summary>
        /// Gets or sets the effective strength of the Mobile. This is the sum of the <see cref="RawStr" /> plus any additional modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change. It ranges from 1 to 65000, inclusive.
        /// <seealso cref="RawStr" />
        /// <seealso cref="StatMod" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Str
        {
            get
            {
                int value = m_Str + (int)GetStatOffset(StatType.Str);

                if (value < 1) value = 1;
                else if (value > 65000) value = 65000;

                return value;
            }
            set
            {
                if (m_StatMods.Count == 0)
                    RawStr = value;
            }
        }

        public virtual int StrMax
        {
            get
            {
                return 100;
            }
            set
            {
            }
        }

        /// <summary>
        /// Gets or sets the base, unmodified, dexterity of the Mobile. Ranges from 1 to 65000, inclusive.
        /// <seealso cref="Dex" />
        /// <seealso cref="StatMod" />
        /// <seealso cref="OnRawDexChange" />
        /// <seealso cref="OnRawStatChange" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int RawDex
        {
            get
            {
                return m_Dex;
            }
            set
            {
                if (value < 1) value = 1;
                else if (value > 65000) value = 65000;

                if (m_Dex != value)
                {
                    int oldValue = m_Dex;

                    m_Dex = value;
                    Delta(MobileDelta.Stat | MobileDelta.Stam);

                    if (Stam < StamMax)
                    {
                        if (m_StamTimer == null)
                            m_StamTimer = new StamTimer(this);

                        m_StamTimer.Start();
                    }
                    else if (Stam > StamMax)
                    {
                        Stam = StamMax;
                    }

                    OnRawDexChange(oldValue);
                    OnRawStatChange(StatType.Dex, oldValue);
                }
            }
        }

        /// <summary>
        /// Gets or sets the effective dexterity of the Mobile. This is the sum of the <see cref="RawDex" /> plus any additional modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change. It ranges from 1 to 65000, inclusive.
        /// <seealso cref="RawDex" />
        /// <seealso cref="StatMod" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Dex
        {
            get
            {
                int value = m_Dex + (int)GetStatOffset(StatType.Dex);

                if (value < 1) value = 1;
                else if (value > 65000) value = 65000;

                return value;
            }
            set
            {
                if (m_StatMods.Count == 0)
                    RawDex = value;
            }
        }

        public virtual int DexMax
        {
            get
            {
                return 100;
            }
            set
            {
            }
        }

        /// <summary>
        /// Gets or sets the base, unmodified, intelligence of the Mobile. Ranges from 1 to 65000, inclusive.
        /// <seealso cref="Int" />
        /// <seealso cref="StatMod" />
        /// <seealso cref="OnRawIntChange" />
        /// <seealso cref="OnRawStatChange" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int RawInt
        {
            get
            {
                return m_Int;
            }
            set
            {
                if (value < 1) value = 1;
                else if (value > 65000) value = 65000;

                if (m_Int != value)
                {
                    int oldValue = m_Int;

                    m_Int = value;
                    Delta(MobileDelta.Stat | MobileDelta.Mana);

                    if (Mana < ManaMax)
                    {
                        if (m_ManaTimer == null)
                            m_ManaTimer = new ManaTimer(this);

                        m_ManaTimer.Start();
                    }
                    else if (Mana > ManaMax)
                    {
                        Mana = ManaMax;
                    }

                    OnRawIntChange(oldValue);
                    OnRawStatChange(StatType.Int, oldValue);
                }
            }
        }

        /// <summary>
        /// Gets or sets the effective intelligence of the Mobile. This is the sum of the <see cref="RawInt" /> plus any additional modifiers. Any attempts to set this value when under the influence of a <see cref="StatMod" /> will result in no change. It ranges from 1 to 65000, inclusive.
        /// <seealso cref="RawInt" />
        /// <seealso cref="StatMod" />
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Int
        {
            get
            {
                int value = m_Int + (int)GetStatOffset(StatType.Int);

                if (value < 1) value = 1;
                else if (value > 65000) value = 65000;

                return value;
            }
            set
            {
                if (m_StatMods.Count == 0)
                    RawInt = value;
            }
        }

        public virtual int IntMax
        {
            get
            {
                return 100;
            }
            set
            {
            }
        }
        /// <summary>
        /// Called when hit points have been restored.
        /// </summary>
        public virtual void FullyRecovered()
        {
            return;
        }
        /// <summary>
        /// Gets or sets the current hit point of the Mobile. This value ranges from 0 to <see cref="HitsMax" />, inclusive. When set to the value of <see cref="HitsMax" />, the <see cref="AggressorInfo.CanReportMurder">CanReportMurder</see> flag of all aggressors is reset to false, and the list of damage entries is cleared.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Hits
        {
            get
            {
                return m_Hits;
            }
            set
            {
                if (m_Deleted)
                    return;

                if (value < 0)
                {
                    value = 0;
                }
                else if (value >= HitsMax)
                {
                    value = HitsMax;

                    if (m_HitsTimer != null)
                        m_HitsTimer.Stop();

                    // 7/17/2023, Adam: If someone poisons you, they are responsible for your death until the poison wears off,
                    //  regardless if you can get a heal in there.
                    if (!Poisoned)
                        for (int i = 0; i < m_Aggressors.Count; i++)//reset reports on full HP
                            ((AggressorInfo)m_Aggressors[i]).CanReportMurder = false;

                    if (m_DamageEntries.Count > 0)
                        m_DamageEntries.Clear(); // reset damage entries on full HP

                }

                if (value < HitsMax)
                {
                    if (CanRegenHits)
                    {
                        if (m_HitsTimer == null)
                            m_HitsTimer = new HitsTimer(this);

                        m_HitsTimer.Start();
                    }
                    else if (m_HitsTimer != null)
                    {
                        m_HitsTimer.Stop();
                    }
                }

                if (m_Hits != value)
                {
                    m_Hits = value;
                    Delta(MobileDelta.Hits);
                    // notify our creature that they are now fully recovered
                    FullyRecovered();
                }
            }
        }

        /// <summary>
        /// Overridable. Gets the maximum hit point of the Mobile. By default, this returns: <c>50 + (<see cref="Str" /> / 2)</c>
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int HitsMax
        {
            get
            {
                if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish >= 13)
                {
                    // Hit Point Calculation
                    //	The following change will be made to the manner in which hit points are calculated for players.
                    //	Hit Points = (str/2) + 50
                    // Note: Any spells or effects that modify strength will also modify the target�s maximum hit points equally. For example, under the new formula, a player with 80 strength will have 90 hit points. However, if they drink a greater strength potion, their strength will be 80+20 (100) and their maximum hit points will be 90+20 (110).
                    return 50 + (Str / 2);
                }
                else
                {
                    return Str;
                }
            }
        }

        /// <summary>
        /// Gets or sets the current stamina of the Mobile. This value ranges from 0 to <see cref="StamMax" />, inclusive.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Stam
        {
            get
            {
                return m_Stam;
            }
            set
            {
                /*if (this.Mount != null && !Core.UOAI  && !Core.UOMO)
				{
					(this.Mount as Mobile).Stam = value;
					return;
				}*/

                if (m_Deleted)
                    return;

                if (value < 0)
                {
                    value = 0;
                }
                else if (value >= StamMax)
                {
                    value = StamMax;

                    if (m_StamTimer != null)
                        m_StamTimer.Stop();
                }

                if (value < StamMax)
                {
                    if (CanRegenStam)
                    {
                        if (m_StamTimer == null)
                            m_StamTimer = new StamTimer(this);

                        m_StamTimer.Start();
                    }
                    else if (m_StamTimer != null)
                    {
                        m_StamTimer.Stop();
                    }
                }

                if (m_Stam != value)
                {
                    m_Stam = value;
                    Delta(MobileDelta.Stam);
                }
            }
        }

        /// <summary>
        /// Overridable. Gets the maximum stamina of the Mobile. By default, this returns: <c><see cref="Dex" /></c>
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int StamMax
        {
            get
            {
                /*if (this.Mounted && !Core.UOAI  && !Core.UOMO)
					return (this.Mount as Mobile).StamMax; 
				else*/
                return Dex;
            }
        }

        /// <summary>
        /// Gets or sets the current stamina of the Mobile. This value ranges from 0 to <see cref="ManaMax" />, inclusive.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public int Mana
        {
            get
            {
                return m_Mana;
            }
            set
            {
                if (m_Deleted)
                    return;

                if (value < 0)
                {
                    value = 0;
                }
                else if (value >= ManaMax)
                {
                    value = ManaMax;

                    if (m_ManaTimer != null)
                        m_ManaTimer.Stop();

                    if (Meditating)
                    {
                        Meditating = false;
                        SendLocalizedMessage(501846); // You are at peace.
                    }
                }

                if (value < ManaMax)
                {
                    if (CanRegenMana)
                    {
                        if (m_ManaTimer == null)
                            m_ManaTimer = new ManaTimer(this);

                        m_ManaTimer.Start();
                    }
                    else if (m_ManaTimer != null)
                    {
                        m_ManaTimer.Stop();
                    }
                }

                if (m_Mana != value)
                {
                    m_Mana = value;
                    Delta(MobileDelta.Mana);
                }
            }
        }

        /// <summary>
        /// Overridable. Gets the maximum mana of the Mobile. By default, this returns: <c><see cref="Int" /></c>
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int ManaMax
        {
            get
            {
                return Int;
            }
        }

        public virtual int HuedItemID
        {
            get
            {
                return (m_Female ? 0x2107 : 0x2106);
            }
        }

        private int m_HueMod = -1;

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public int HueMod
        {
            get
            {
                return m_HueMod;
            }
            set
            {
                if (m_HueMod != value)
                {
                    m_HueMod = value;

                    Delta(MobileDelta.Hue);
                }
            }
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public virtual int Hue
        {
            get
            {
                if (m_HueMod != -1)
                    return m_HueMod;

                return m_Hue;
            }
            set
            {
                int oldHue = m_Hue;

                if (oldHue != value)
                {
                    m_Hue = value;

                    Delta(MobileDelta.Hue);
                }
            }
        }


        public void SetDirection(Direction dir)
        {
            m_Direction = dir;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Direction
        {
            get
            {
                return m_Direction;
            }
            set
            {
                if (m_Direction != value)
                {
                    m_Direction = value;

                    Delta(MobileDelta.Direction);
                    //ProcessDelta();
                }
            }
        }

        public virtual int GetSeason()
        {
            if (m_Map != null)
                return m_Map.Season;

            return 1;
        }

        public virtual int GetPacketFlags()
        {
            int flags = 0x0;

            if (m_Female)
                flags |= 0x02;

            if (m_Poison != null)
                flags |= 0x04;

            if (m_Blessed || m_YellowHealthbar)
                flags |= 0x08;

            if (m_Warmode)
                flags |= 0x40;

            if (m_Hidden)
                flags |= 0x80;

            return flags;
        }
        // Pre-7.0.0.0 Packet Flags
        public virtual int GetOldPacketFlags()
        {
            int flags = 0x0;

            if (m_Paralyzed || m_Frozen)
                flags |= 0x01;

            if (m_Female)
                flags |= 0x02;

            if (m_Poison != null)
                flags |= 0x04;

            if (m_Blessed || m_YellowHealthbar)
                flags |= 0x08;

            if (m_Warmode)
                flags |= 0x40;

            if (m_Hidden)
                flags |= 0x80;

            return flags;
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Female
        {
            get
            {
                return m_Female;
            }
            set
            {
                if (m_Female != value)
                {
                    m_Female = value;
                    Delta(MobileDelta.Flags);
                    OnGenderChanged(!m_Female);
                }
            }
        }

        public virtual void OnGenderChanged(bool oldFemale)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Warmode
        {
            get
            {
                return m_Warmode;
            }
            set
            {
                if (m_Deleted)
                    return;

                if (m_Warmode != value)
                {
                    if (m_AutoManifestTimer != null)
                    {
                        m_AutoManifestTimer.Stop();
                        m_AutoManifestTimer = null;
                    }

                    m_Warmode = value;
                    Delta(MobileDelta.Flags);

                    if (m_NetState != null)
                        Send(SetWarMode.Instantiate(value));

                    if (!m_Warmode)
                        Combatant = null;

                    if (!Alive)
                    {
                        if (value)
                            Delta(MobileDelta.GhostUpdate);
                        else
                            SendRemovePacket(false);
                    }
                }
            }
        }
        public virtual void OnHideChange(Mobile me, bool hidden)
        {

        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Hidden
        {
            get
            {
                return m_Hidden;
            }
            set
            {
                if (m_Hidden != value)
                {
                    m_Hidden = value;
                    //Delta( MobileDelta.Flags );

                    OnHiddenChanged();
                }
            }
        }
        public virtual void OnHiddenChanged()
        {
            m_AllowedStealthSteps = 0;

            if (m_Map != null)
            {
                IPooledEnumerable<NetState> eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (!state.Mobile.CanSee(this))
                    {
                        state.Send(this.RemovePacket);
                    }
                    else
                    {
                        state.Send(MobileIncoming.Create(state, state.Mobile, this));

                        if (IsDeadBondedPet)
                            state.Send(new BondedStatus(0, m_Serial, 1));

                        if (ObjectPropertyList.Enabled)
                        {
                            state.Send(OPLPacket);

                            //foreach ( Item item in m_Items )
                            //	state.Send( item.OPLPacket );
                        }
                    }
                }

                eable.Free();
            }
        }
        public virtual void OnConnected()
        {
        }

        public virtual void OnDisconnected()
        {
        }

        public virtual void OnNetStateChanged()
        {
        }

        public NetState NetState
        {
            get
            {
                if (m_NetState != null && m_NetState.Socket == null)
                    NetState = null;

                return m_NetState;
            }
            set
            {
                if (m_NetState != value)
                {
                    if (m_Map != null)
                        m_Map.OnClientChange(m_NetState, value, this);

                    if (m_Target != null)
                        m_Target.Cancel(this, TargetCancelType.Disconnected);

                    if (m_QuestArrow != null)
                        QuestArrow = null;

                    if (m_Spell != null)
                        m_Spell.OnConnectionChanged();

                    //if ( m_Spell != null )
                    //	m_Spell.FinishSequence();

                    if (m_NetState != null)
                        m_NetState.CancelAllTrades();

                    try { CloseAllGumps(); } catch (Exception e) { LogHelper.LogException(e); }

                    BankBox box = FindBankNoCreate();

                    if (box != null && box.Opened)
                        box.Close();

                    // REMOVED:
                    //m_Actions.Clear();

                    m_NetState = value;

                    if (m_NetState == null)
                    {
                        OnDisconnected();
                        EventSink.InvokeDisconnected(new DisconnectedEventArgs(this));

                        // Disconnected, start the logout timer

                        if (m_LogoutTimer == null)
                            m_LogoutTimer = new LogoutTimer(this);
                        else
                            m_LogoutTimer.Stop();

                        m_LogoutTimer.Delay = GetLogoutDelay();
                        m_LogoutTimer.Start();
                    }
                    else
                    {
                        OnConnected();
                        EventSink.InvokeConnected(new ConnectedEventArgs(this));

                        // Connected, stop the logout timer and if needed, move to the world

                        if (m_LogoutTimer != null)
                            m_LogoutTimer.Stop();

                        m_LogoutTimer = null;

                        if (m_Map == Map.Internal && m_LogoutMap != null)
                        {
                            Map = m_LogoutMap;
                            Location = m_LogoutLocation;
                        }
                    }

                    for (int i = m_Items.Count - 1; i >= 0; --i)
                    {
                        if (i >= m_Items.Count)
                            continue;

                        Item item = (Item)m_Items[i];

                        if (item is SecureTradeContainer)
                        {
                            for (int j = item.Items.Count - 1; j >= 0; --j)
                            {
                                if (j < item.Items.Count)
                                {
                                    ((Item)item.Items[j]).OnSecureTrade(this, this, this, false);
                                    AddToBackpack((Item)item.Items[j]);
                                }
                            }

                            item.Delete();
                        }
                    }

                    DropHolding();
                    OnNetStateChanged();
                }
            }
        }

        public virtual bool CanSee(object o)
        {
            if (o is Item)
            {
                return CanSee((Item)o);
            }
            else if (o is Mobile)
            {
                return CanSee((Mobile)o);
            }
            else
            {
                return true;
            }
        }

        public virtual bool CanSee(Item item)
        {
            if (m_Map == Map.Internal)
                return false;
            else if (item.Map == Map.Internal)
                return false;

            if (item.Parent != null)
            {
                if (item.Parent is Item)
                {
                    if (!CanSee((Item)item.Parent))
                        return false;
                }
                else if (item.Parent is Mobile)
                {
                    if (!CanSee((Mobile)item.Parent))
                        return false;
                }
            }

            if (item is BankBox)
            {
                BankBox box = item as BankBox;

                if (box != null && m_AccessLevel <= AccessLevel.Counselor && (box.Owner != this || !box.Opened))
                    return false;
            }
            else if (item is SecureTradeContainer)
            {
                SecureTrade trade = ((SecureTradeContainer)item).Trade;

                if (trade != null && trade.From.Mobile != this && trade.To.Mobile != this)
                    return false;
            }
            else if (item is Spawner)
            {   // 1/27/23, Adam: spawners become visible to staff when they move by them:
                //  To show the hue which reflects the spawner type/status
                //  Red=Pub 15 AI style spawner, Yellow=AllShards, Green=NeRun's Distro Spawner
                if (m_AccessLevel < AccessLevel.GameMaster || IsHideHidden)
                    return false;
            }

            return !item.Deleted && item.Map == m_Map && (item.Visible || (m_AccessLevel > AccessLevel.Counselor && !IsHideHidden));
        }
        private bool IsHideHidden
        {
            get { return GetMobileBool(MobileBoolTable.HideHidden); }
        }
        public virtual bool CanSee(Mobile m)
        {
            if (m_Deleted || m.m_Deleted || m_Map == Map.Internal || m.m_Map == Map.Internal)
                return false;

            return this == m || (
                m.m_Map == m_Map &&
                (!m.Hidden || m_AccessLevel > m.AccessLevel) &&
                (m.Alive || !Alive || m_AccessLevel > AccessLevel.Player || m.Warmode));
        }
        public virtual bool CanBeRenamedBy(Mobile from)
        {
            // Counselors cannot rename players
            if (from.m_AccessLevel == AccessLevel.Counselor && this.Player == true)
                return false;

            return (from.m_AccessLevel > m_AccessLevel);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Language
        {
            get
            {
                return m_Language;
            }
            set
            {
                m_Language = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpeechHue
        {
            get
            {
                return m_SpeechHue;
            }
            set
            {
                m_SpeechHue = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int EmoteHue
        {
            get
            {
                return m_EmoteHue;
            }
            set
            {
                m_EmoteHue = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WhisperHue
        {
            get
            {
                return m_WhisperHue;
            }
            set
            {
                m_WhisperHue = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int YellHue
        {
            get
            {
                return m_YellHue;
            }
            set
            {
                m_YellHue = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildTitle
        {
            get
            {
                return m_GuildTitle;
            }
            set
            {
                string old = m_GuildTitle;

                if (old != value)
                {
                    m_GuildTitle = value;

                    if (m_Guild != null && !m_Guild.Disbanded && m_GuildTitle != null)
                        this.SendLocalizedMessage(1018026, true, m_GuildTitle); // Your guild title has changed :

                    InvalidateProperties();

                    OnGuildTitleChange(old);
                }
            }
        }

        public virtual void OnGuildTitleChange(string oldTitle)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DisplayGuildTitle
        {
            get
            {
                return m_DisplayGuildTitle;
            }
            set
            {
                m_DisplayGuildTitle = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile GuildFealty
        {
            get
            {
                return m_GuildFealty;
            }
            set
            {
                m_GuildFealty = value;
            }
        }

        private string m_NameMod;

        [CommandProperty(AccessLevel.GameMaster)]
        public string NameMod
        {
            get
            {
                return m_NameMod;
            }
            set
            {
                if (m_NameMod != value)
                {
                    m_NameMod = value;
                    Delta(MobileDelta.Name);
                    InvalidateProperties();
                }
            }
        }

        private bool m_YellowHealthbar;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool YellowHealthbar
        {
            get
            {
                return m_YellowHealthbar;
            }
            set
            {
                m_YellowHealthbar = value;
                Delta(MobileDelta.HealthbarYellow);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string RawName
        {
            get { return m_Name; }
            set { Name = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name
        {
            get
            {
                if (m_NameMod != null)
                    return m_NameMod;

                return m_Name;
            }
            set
            {
                if (m_Name != value) // I'm leaving out the && m_NameMod == null
                {
                    m_Name = value;
                    Delta(MobileDelta.Name);
                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastStatGain
        {
            get
            {
                return m_LastStatGain;
            }
            set
            {
                m_LastStatGain = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseGuild Guild
        {
            get
            {
                return m_Guild;
            }
            set
            {
                BaseGuild old = m_Guild;

                if (old != value)
                {
                    if (value == null)
                        GuildTitle = null;

                    m_Guild = value;

                    Delta(MobileDelta.Noto);
                    InvalidateProperties();

                    OnGuildChange(old);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildName
        {
            get { return (m_Guild == null ? null : m_Guild.Name); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string GuildAbbreviation
        {
            get { return (m_Guild == null ? null : m_Guild.Abbreviation); }
        }

        public virtual void OnGuildChange(BaseGuild oldGuild)
        {
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison
        {
            get
            {
                return m_Poison;
            }
            set
            {
                /*if ( m_Poison != value && (m_Poison == null || value == null || m_Poison.Level < value.Level) )
				{*/
                m_Poison = value;
                Delta(MobileDelta.Flags);

                if (m_PoisonTimer != null)
                {
                    m_PoisonTimer.Stop();
                    m_PoisonTimer = null;
                }

                if (m_Poison != null)
                {
                    m_PoisonTimer = m_Poison.ConstructTimer(this);

                    if (m_PoisonTimer != null)
                        m_PoisonTimer.Start();
                }

                CheckStatTimers();
                /*}*/
            }
        }

        /// <summary>
        /// Overridable. Event invoked when a call to <see cref="ApplyPoison" /> failed because <see cref="CheckPoisonImmunity" /> returned false: the Mobile was resistant to the poison. By default, this broadcasts an overhead message: * The poison seems to have no effect. *
        /// <seealso cref="CheckPoisonImmunity" />
        /// <seealso cref="ApplyPoison" />
        /// <seealso cref="Poison" />
        /// </summary>
        private Memory m_mPoisonNoEffect = new Memory();
        public virtual void OnPoisonImmunity(Mobile from, Poison poison)
        {
            if (m_mPoisonNoEffect.Recall("PoisonNoEffect") == null)
            {   // anti-spam
                this.PublicOverheadMessage(MessageType.Emote, 0x3B2, true, "*The poison seems to have no effect*");
                m_mPoisonNoEffect.Remember("PoisonNoEffect", 4);
            }

        }

        /// <summary>
        /// Overridable. Virtual event invoked when a call to <see cref="ApplyPoison" /> failed because <see cref="CheckHigherPoison" /> returned false: the Mobile was already poisoned by an equal or greater strength poison.
        /// <seealso cref="CheckHigherPoison" />
        /// <seealso cref="ApplyPoison" />
        /// <seealso cref="Poison" />
        /// </summary>
        public virtual void OnHigherPoison(Mobile from, Poison poison)
        {
        }

        /// <summary>
        /// Overridable. Event invoked when a call to <see cref="ApplyPoison" /> succeeded. By default, this broadcasts an overhead message varying by the level of the poison. Example: * Zippy begins to spasm uncontrollably. *
        /// <seealso cref="ApplyPoison" />
        /// <seealso cref="Poison" />
        /// </summary>
        public virtual void OnPoisoned(Mobile from, Poison poison, Poison oldPoison)
        {
            if (poison != null)
            {
                this.LocalOverheadMessage(MessageType.Regular, 0x22, 1042857 + (poison.Level * 2));
                this.NonlocalOverheadMessage(MessageType.Regular, 0x22, 1042858 + (poison.Level * 2), Name);
            }
        }

        /// <summary>
        /// Overridable. Called from <see cref="ApplyPoison" />, this method checks if the Mobile is immune to some <see cref="Poison" />. If true, <see cref="OnPoisonImmunity" /> will be invoked and <see cref="ApplyPoisonResult.Immune" /> is returned.
        /// <seealso cref="OnPoisonImmunity" />
        /// <seealso cref="ApplyPoison" />
        /// <seealso cref="Poison" />
        /// </summary>
        public virtual bool CheckPoisonImmunity(Mobile from, Poison poison)
        {
            return false;
        }

        /// <summary>
        /// Overridable. Called from <see cref="ApplyPoison" />, this method checks if the Mobile is already poisoned by some <see cref="Poison" /> of equal or greater strength. If true, <see cref="OnHigherPoison" /> will be invoked and <see cref="ApplyPoisonResult.HigherPoisonActive" /> is returned.
        /// <seealso cref="OnHigherPoison" />
        /// <seealso cref="ApplyPoison" />
        /// <seealso cref="Poison" />
        /// </summary>
        public virtual bool CheckHigherPoison(Mobile from, Poison poison)
        {
            return (m_Poison != null && m_Poison.Level >= poison.Level);
        }

        /// <summary>
        /// Overridable. Attempts to apply poison to the Mobile. Checks are made such that no <see cref="CheckHigherPoison">higher poison is active</see> and that the Mobile is not <see cref="CheckPoisonImmunity">immune to the poison</see>. Provided those assertions are true, the <paramref name="poison" /> is applied and <see cref="OnPoisoned" /> is invoked.
        /// <seealso cref="Poison" />
        /// <seealso cref="CurePoison" />
        /// </summary>
        /// <returns>One of four possible values:
        /// <list type="table">
        /// <item>
        /// <term><see cref="ApplyPoisonResult.Cured">Cured</see></term>
        /// <description>The <paramref name="poison" /> parameter was null and so <see cref="CurePoison" /> was invoked.</description>
        /// </item>
        /// <item>
        /// <term><see cref="ApplyPoisonResult.HigherPoisonActive">HigherPoisonActive</see></term>
        /// <description>The call to <see cref="CheckHigherPoison" /> returned false.</description>
        /// </item>
        /// <item>
        /// <term><see cref="ApplyPoisonResult.Immune">Immune</see></term>
        /// <description>The call to <see cref="CheckPoisonImmunity" /> returned false.</description>
        /// </item>
        /// <item>
        /// <term><see cref="ApplyPoisonResult.Poisoned">Poisoned</see></term>
        /// <description>The <paramref name="poison" /> was successfully applied.</description>
        /// </item>
        /// </list>
        /// </returns>
        public virtual ApplyPoisonResult ApplyPoison(Mobile from, Poison poison)
        {
            if (poison == null)
            {
                CurePoison(from);
                return ApplyPoisonResult.Cured;
            }

            if (CheckHigherPoison(from, poison))
            {
                OnHigherPoison(from, poison);
                return ApplyPoisonResult.HigherPoisonActive;
            }

            if (CheckPoisonImmunity(from, poison))
            {
                OnPoisonImmunity(from, poison);
                return ApplyPoisonResult.Immune;
            }

            Poison oldPoison = m_Poison;
            this.Poison = poison;

            OnPoisoned(from, poison, oldPoison);

            return ApplyPoisonResult.Poisoned;
        }

        /// <summary>
        /// Overridable. Called from <see cref="CurePoison" />, this method checks to see that the Mobile can be cured of <see cref="Poison" />
        /// <seealso cref="CurePoison" />
        /// <seealso cref="Poison" />
        /// </summary>
        public virtual bool CheckCure(Mobile from)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when a call to <see cref="CurePoison" /> succeeded.
        /// <seealso cref="CurePoison" />
        /// <seealso cref="CheckCure" />
        /// <seealso cref="Poison" />
        /// </summary>
        public virtual void OnCured(Mobile from, Poison oldPoison)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when a call to <see cref="CurePoison" /> failed.
        /// <seealso cref="CurePoison" />
        /// <seealso cref="CheckCure" />
        /// <seealso cref="Poison" />
        /// </summary>
        public virtual void OnFailedCure(Mobile from)
        {
        }

        /// <summary>
        /// Overridable. Attempts to cure any poison that is currently active.
        /// </summary>
        /// <returns>True if poison was cured, false if otherwise.</returns>
        public virtual bool CurePoison(Mobile from)
        {
            if (CheckCure(from))
            {
                Poison oldPoison = m_Poison;
                this.Poison = null;

                OnCured(from, oldPoison);

                return true;
            }

            OnFailedCure(from);

            return false;
        }

        public virtual void OnBeforeSpawn(Point3D location, Map m)
        {
        }

        public virtual void OnAfterSpawn()
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Poisoned
        {
            get
            {
                return (m_Poison != null);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsBodyMod
        {
            get
            {
                return (m_BodyMod.BodyID != 0);
            }
        }
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Body BodyMod
        {
            get
            {
                return m_BodyMod;
            }
            set
            {
                if (m_BodyMod != value)
                {
                    m_BodyMod = value;

                    Delta(MobileDelta.Body);
                    InvalidateProperties();

                    CheckStatTimers();
                }
            }
        }

        private static int[] m_InvalidBodies = new int[]
            {
                32,
                95,
                156,
                197,
                198,
        };

        [CopyableAttribute(CopyType.DoNotCopy)]
        [Body, CommandProperty(AccessLevel.GameMaster)]
        public Body Body
        {
            get
            {
                if (IsBodyMod)
                    return m_BodyMod;

                return m_Body;
            }
            set
            {
                if (m_Body != value && !IsBodyMod)
                {
                    m_Body = SafeBody(value);

                    Delta(MobileDelta.Body);
                    InvalidateProperties();

                    CheckStatTimers();
                }
            }
        }

        public virtual int SafeBody(int body)
        {
            int delta = -1;

            for (int i = 0; delta < 0 && i < m_InvalidBodies.Length; ++i)
                delta = (m_InvalidBodies[i] - body);

            if (delta != 0)
                return body;

            return 0;
        }

        [Body, CommandProperty(AccessLevel.GameMaster)]
        public int BodyValue
        {
            get
            {
                return Body.BodyID;
            }
            set
            {
                Body = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Serial Serial
        {
            get
            {
                return m_Serial;
            }
        }

        Point3D IEntity.Location
        {
            get
            {
                return m_Location;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Point3D Location
        {
            get
            {
                return m_Location;
            }
            set
            {
                SetLocation(value, true);
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Point3D LogoutLocation
        {
            get
            {
                return m_LogoutLocation;
            }
            set
            {
                m_LogoutLocation = value;
            }
        }

        public Map LogoutMap
        {
            get
            {
                return m_LogoutMap;
            }
            set
            {
                m_LogoutMap = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Region Region
        {
            get
            {
                return m_Region;
            }
        }

        public void FreeCache()
        {
            Packet.Release(ref m_RemovePacket);
            Packet.Release(ref m_PropertyList);
            Packet.Release(ref m_OPLPacket);
        }

        private Packet m_RemovePacket;

        public Packet RemovePacket
        {
            get
            {
                if (m_RemovePacket == null)
                {
                    m_RemovePacket = new RemoveMobile(this);
                    m_RemovePacket.SetStatic();
                }

                return m_RemovePacket;
            }
        }

        private Packet m_OPLPacket;

        public Packet OPLPacket
        {
            get
            {
                if (m_OPLPacket == null)
                    m_OPLPacket = new OPLInfo(PropertyList);

                return m_OPLPacket;
            }
        }

        private ObjectPropertyList m_PropertyList;

        public ObjectPropertyList PropertyList
        {
            get
            {
                if (m_PropertyList == null)
                {
                    m_PropertyList = new ObjectPropertyList(this);

                    GetProperties(m_PropertyList);

                    m_PropertyList.Terminate();
                    m_PropertyList.SetStatic();
                }

                return m_PropertyList;
            }
        }

        public void ClearProperties()
        {
            Packet.Release(ref m_PropertyList);
            Packet.Release(ref m_OPLPacket);
        }

        public void InvalidateProperties()
        {
            if (!Core.RuleSets.AOSRules())
                return;

            if (m_Map != null && m_Map != Map.Internal && !World.Loading)
            {
                ObjectPropertyList oldList = m_PropertyList;
                Packet.Release(ref m_PropertyList);
                ObjectPropertyList newList = PropertyList;

                if (oldList == null || oldList.Hash != newList.Hash)
                {
                    Packet.Release(ref m_OPLPacket);
                    Delta(MobileDelta.Properties);
                }
            }
            else
            {
                ClearProperties();
            }
        }

        private int m_SolidHueOverride = -1;

        [CommandProperty(AccessLevel.GameMaster)]
        public int SolidHueOverride
        {
            get { return m_SolidHueOverride; }
            set { if (m_SolidHueOverride == value) return; m_SolidHueOverride = value; Delta(MobileDelta.Hue | MobileDelta.Body); }
        }
#if true
        public virtual void MoveToWorld(Point3D newLocation, Map map)
        {
            if (m_Deleted)
                return;

            if (m_Map == map)
            {
                SetLocation(newLocation, true);
                return;
            }

            BankBox box = FindBankNoCreate();

            if (box != null && box.Opened)
                box.Close();

            Point3D oldLocation = m_Location;
            Map oldMap = m_Map;

            Region oldRegion = m_Region;

            if (oldMap != null)
            {
                oldMap.OnLeave(this);

                ClearScreen();
                SendRemovePacket();
            }

            for (int i = 0; i < m_Items.Count; ++i)
                m_Items[i].Map = map;

            m_Map = map;

            m_Location = newLocation;

            NetState ns = m_NetState;

            if (m_Map != null)
            {
                m_Map.OnEnter(this);

                UpdateRegion();

                if (ns != null && m_Map != null)
                {
                    ns.Sequence = 0;
                    ns.Send(new MapChange(this));
                    ns.Send(new MapPatches());
                    ns.Send(SeasonChange.Instantiate(GetSeason(), true));

                    if (ns.StygianAbyss)
                        ns.Send(new MobileUpdate(this));
                    else
                        ns.Send(new MobileUpdateOld(this));

                    ClearFastwalkStack();
                }
            }
            else
            {
                UpdateRegion();
            }

            if (ns != null)
            {
                if (m_Map != null)
                    Send(new ServerChange(this, m_Map));

                ns.Sequence = 0;
                ClearFastwalkStack();

                ns.Send(MobileIncoming.Create(ns, this, this));

                if (ns.StygianAbyss)
                {
                    ns.Send(new MobileUpdate(this));
                    CheckLightLevels(true);
                    ns.Send(new MobileUpdate(this));
                }
                else
                {
                    ns.Send(new MobileUpdateOld(this));
                    CheckLightLevels(true);
                    ns.Send(new MobileUpdateOld(this));
                }
            }

            SendEverything();
            SendIncomingPacket();

            if (ns != null)
            {
                ns.Sequence = 0;
                ClearFastwalkStack();

                ns.Send(MobileIncoming.Create(ns, this, this));

                if (ns.StygianAbyss)
                {
                    ns.Send(SupportedFeatures.Instantiate(ns));
                    ns.Send(new MobileUpdate(this));
                    ns.Send(new MobileAttributes(this));
                }
                else
                {
                    ns.Send(SupportedFeatures.Instantiate(ns));
                    ns.Send(new MobileUpdateOld(this));
                    ns.Send(new MobileAttributes(this));
                }
            }

            OnMapChange(oldMap);
            OnLocationChange(oldLocation);

            if (m_Region != null)
                m_Region.OnLocationChanged(this, oldLocation);
        }
#else
        public virtual void MoveToWorld(Point3D newLocation, Map map)
        {
            if (m_Deleted)
                return;

            if (m_Map == map)
            {
                SetLocation(newLocation, true);
                return;
            }

            BankBox box = FindBankNoCreate();

            if (box != null && box.Opened)
                box.Close();

            Point3D oldLocation = m_Location;
            Map oldMap = m_Map;

            Region oldRegion = m_Region;

            if (oldMap != null)
            {
                oldMap.OnLeave(this);

                ClearScreen();
                SendRemovePacket();
            }

            for (int i = 0; i < m_Items.Count; ++i)
                ((Item)m_Items[i]).Map = map;

            m_Map = map;

            m_Region.InternalExit(this);

            m_Location = newLocation;

            NetState ns = m_NetState;

            if (m_Map != null)
            {
                m_Map.OnEnter(this);

                m_Region = Region.Find(m_Location, m_Map);
                OnRegionChange(oldRegion, m_Region);

                m_Region.InternalEnter(this);

                if (ns != null && m_Map != null)
                {
                    ns.Sequence = 0;
                    ns.Send(new MapChange(this));
                    ns.Send(new MapPatches());
                    ns.Send(SeasonChange.Instantiate(GetSeason(), true));
                    ns.Send(new MobileUpdate(this));
                    ClearFastwalkStack();
                }
            }

            if (ns != null)
            {
                if (m_Map != null)
                    Send(new ServerChange(this, m_Map));

                ns.Sequence = 0;
                ClearFastwalkStack();

                Send(new MobileIncoming(this, this));
                Send(new MobileUpdate(this));
                CheckLightLevels(true);
                Send(new MobileUpdate(this));
            }

            SendEverything();
            SendIncomingPacket();

            if (ns != null)
            {
                m_NetState.Sequence = 0;
                ClearFastwalkStack();

                Send(new MobileIncoming(this, this));
                Send(SupportedFeatures.Instantiate(ns.Account));
                Send(new MobileUpdate(this));
                Send(new MobileAttributes(this));
            }

            OnMapChange(oldMap);
            OnLocationChange(oldLocation);

            m_Region.OnLocationChanged(this, oldLocation);
        }
#endif
#if true
        public virtual void SetLocation(Point3D newLocation, bool isTeleport)
        {
            if (m_Deleted)
                return;

            Point3D oldLocation = m_Location;

            if (oldLocation != newLocation)
            {
                m_Location = newLocation;
                UpdateRegion();

                BankBox box = FindBankNoCreate();

                if (box != null && box.Opened)
                    box.Close();

                if (m_NetState != null)
                    m_NetState.ValidateAllTrades();

                if (m_Map != null)
                    m_Map.OnMove(oldLocation, this);

                if (isTeleport && m_NetState != null && (!m_NetState.HighSeas || !m_NoMoveHS))
                {
                    m_NetState.Sequence = 0;

                    if (m_NetState.StygianAbyss)
                        m_NetState.Send(new MobileUpdate(this));
                    else
                        m_NetState.Send(new MobileUpdateOld(this));

                    ClearFastwalkStack();
                }

                Map map = m_Map;

                if (map != null)
                {
                    // First, send a remove message to everyone who can no longer see us. (inOldRange && !inNewRange)

                    IPooledEnumerable<NetState> eable = map.GetClientsInRange(oldLocation);

                    foreach (NetState ns in eable)
                    {
                        if (ns != m_NetState && !Utility.InUpdateRange(newLocation, ns.Mobile.Location))
                        {
                            ns.Send(this.RemovePacket);
                        }
                    }

                    eable.Free();

                    NetState ourState = m_NetState;

                    // Check to see if we are attached to a client
                    if (ourState != null)
                    {
                        IPooledEnumerable<IEntity> eeable = map.GetObjectsInRange(newLocation, Core.GlobalMaxUpdateRange);

                        // We are attached to a client, so it's a bit more complex. We need to send new items and people to ourself, and ourself to other clients

                        foreach (IEntity o in eeable)
                        {
                            if (o is Item)
                            {
                                Item item = (Item)o;

                                int range = item.GetUpdateRange(this);
                                Point3D loc = item.Location;

                                if (!Utility.InRange(oldLocation, loc, range) && Utility.InRange(newLocation, loc, range) && CanSee(item))
                                    item.SendInfoTo(ourState);
                            }
                            else if (o != this && o is Mobile)
                            {
                                Mobile m = (Mobile)o;

                                if (!Utility.InUpdateRange(newLocation, m.m_Location))
                                    continue;

                                bool inOldRange = Utility.InUpdateRange(oldLocation, m.m_Location);

                                if (m.m_NetState != null && ((isTeleport && (!m.m_NetState.HighSeas || !m_NoMoveHS)) || !inOldRange) && m.CanSee(this))
                                {
                                    m.m_NetState.Send(MobileIncoming.Create(m.m_NetState, m, this));

                                    if (m.m_NetState.StygianAbyss)
                                    {
                                        if (m_Poison != null)
                                            m.m_NetState.Send(new HealthbarPoison(this));

                                        if (m_Blessed || m_YellowHealthbar)
                                            m.m_NetState.Send(new HealthbarYellow(this));
                                    }

                                    if (IsDeadBondedPet)
                                        m.m_NetState.Send(new BondedStatus(0, m_Serial, 1));

                                    if (ObjectPropertyList.Enabled)
                                    {
                                        m.m_NetState.Send(OPLPacket);

                                        //foreach ( Item item in m_Items )
                                        //m.m_NetState.Send( item.OPLPacket );
                                    }
                                }

                                if (!inOldRange && CanSee(m))
                                {
                                    ourState.Send(MobileIncoming.Create(ourState, this, m));

                                    if (ourState.StygianAbyss)
                                    {
                                        if (m.Poisoned)
                                            ourState.Send(new HealthbarPoison(m));

                                        if (m.Blessed || m.YellowHealthbar)
                                            ourState.Send(new HealthbarYellow(m));
                                    }

                                    if (m.IsDeadBondedPet)
                                        ourState.Send(new BondedStatus(0, m.m_Serial, 1));

                                    if (ObjectPropertyList.Enabled)
                                    {
                                        ourState.Send(m.OPLPacket);

                                        //foreach ( Item item in m.m_Items )
                                        //	ourState.Send( item.OPLPacket );
                                    }
                                }
                            }
                        }

                        eeable.Free();
                    }
                    else
                    {
                        eable = map.GetClientsInRange(newLocation);

                        // We're not attached to a client, so simply send an Incoming
                        foreach (NetState ns in eable)
                        {
                            if (((isTeleport && (!ns.HighSeas || !m_NoMoveHS)) || !Utility.InUpdateRange(oldLocation, ns.Mobile.Location)) && ns.Mobile.CanSee(this))
                            {
                                ns.Send(MobileIncoming.Create(ns, ns.Mobile, this));

                                if (ns.StygianAbyss)
                                {
                                    if (m_Poison != null)
                                        ns.Send(new HealthbarPoison(this));

                                    if (m_Blessed || m_YellowHealthbar)
                                        ns.Send(new HealthbarYellow(this));
                                }

                                if (IsDeadBondedPet)
                                    ns.Send(new BondedStatus(0, m_Serial, 1));

                                if (ObjectPropertyList.Enabled)
                                {
                                    ns.Send(OPLPacket);

                                    //foreach ( Item item in m_Items )
                                    //	ns.Send( item.OPLPacket );
                                }
                            }
                        }

                        eable.Free();
                    }
                }

                OnLocationChange(oldLocation);

                this.Region.OnLocationChanged(this, oldLocation);
            }
        }
#else
        public virtual void SetLocation(Point3D newLocation, bool isTeleport)
        {
            if (m_Deleted)
                return;

            Point3D oldLocation = m_Location;
            Region oldRegion = m_Region;

            if (oldLocation != newLocation)
            {
                m_Location = newLocation;

                BankBox box = FindBankNoCreate();

                if (box != null && box.Opened)
                    box.Close();

                if (m_NetState != null)
                    m_NetState.ValidateAllTrades();

                if (m_Map != null)
                    m_Map.OnMove(oldLocation, this);

                if (isTeleport && m_NetState != null)
                {
                    m_NetState.Sequence = 0;
                    m_NetState.Send(new MobileUpdate(this));
                    ClearFastwalkStack();
                }

                Map map = m_Map;

                if (map != null)
                {
                    // First, send a remove message to everyone who can no longer see us. (inOldRange && !inNewRange)
                    Packet removeThis = null;

                    IPooledEnumerable eable = map.GetClientsInRange(oldLocation);

                    foreach (NetState ns in eable)
                    {
                        if (ns != m_NetState && !Utility.InUpdateRange(newLocation, ns.Mobile.Location))
                        {
                            if (removeThis == null)
                                removeThis = this.RemovePacket;

                            ns.Send(removeThis);
                        }
                    }

                    eable.Free();

                    NetState ourState = m_NetState;

                    // Check to see if we are attached to a client
                    if (ourState != null)
                    {
                        eable = map.GetObjectsInRange(newLocation, Core.GlobalMaxUpdateRange);

                        // We are attached to a client, so it's a bit more complex. We need to send new items and people to ourself, and ourself to other clients
                        foreach (object o in eable)
                        {
                            if (o is Item)
                            {
                                Item item = (Item)o;

                                int range = item.GetUpdateRange(this);
                                Point3D loc = item.Location;

                                if (!Utility.InRange(oldLocation, loc, range) && Utility.InRange(newLocation, loc, range) && CanSee(item))
                                    item.SendInfoTo(ourState);
                            }
                            else if (o != this && o is Mobile)
                            {
                                Mobile m = (Mobile)o;

                                if (!Utility.InUpdateRange(newLocation, m.m_Location))
                                    continue;

                                bool inOldRange = Utility.InUpdateRange(oldLocation, m.m_Location);

                                if ((isTeleport || !inOldRange) && m.m_NetState != null && m.CanSee(this))
                                {
                                    m.m_NetState.Send(new MobileIncoming(m, this));

                                    if (IsDeadBondedPet)
                                        m.m_NetState.Send(new BondedStatus(0, m_Serial, 1));

                                    if (ObjectPropertyList.Enabled)
                                    {
                                        m.m_NetState.Send(OPLPacket);

                                        //foreach ( Item item in m_Items )
                                        //	m.m_NetState.Send( item.OPLPacket );
                                    }
                                }

                                if (!inOldRange && CanSee(m))
                                {
                                    ourState.Send(new MobileIncoming(this, m));

                                    if (m.IsDeadBondedPet)
                                        ourState.Send(new BondedStatus(0, m.m_Serial, 1));

                                    if (ObjectPropertyList.Enabled)
                                    {
                                        ourState.Send(m.OPLPacket);

                                        //foreach ( Item item in m.m_Items )
                                        //	ourState.Send( item.OPLPacket );
                                    }
                                }
                            }
                        }

                        eable.Free();
                    }
                    else
                    {
                        eable = map.GetClientsInRange(newLocation);

                        // We're not attached to a client, so simply send an Incoming
                        foreach (NetState ns in eable)
                        {
                            if ((isTeleport || !Utility.InUpdateRange(oldLocation, ns.Mobile.Location)) && ns.Mobile.CanSee(this))
                            {
                                ns.Send(new MobileIncoming(ns.Mobile, this));

                                if (IsDeadBondedPet)
                                    ns.Send(new BondedStatus(0, m_Serial, 1));

                                if (ObjectPropertyList.Enabled)
                                {
                                    ns.Send(OPLPacket);

                                    //foreach ( Item item in m_Items )
                                    //	ns.Send( item.OPLPacket );
                                }
                            }
                        }

                        eable.Free();
                    }
                }

                // the sector.Regions list is sorted on priority, so the first found is the highest priority
                m_Region = Region.Find(m_Location, m_Map);

                if (oldRegion != m_Region)
                {
                    oldRegion.InternalExit(this);
                    m_Region.InternalEnter(this);
                    OnRegionChange(oldRegion, m_Region);
                }

                OnLocationChange(oldLocation);

                CheckLightLevels(false);

                m_Region.OnLocationChanged(this, oldLocation);
            }
        }
#endif
        /// <summary>
        /// Overridable. Virtual event invoked when <see cref="Location" /> changes.
        /// </summary>
        protected virtual void OnLocationChange(Point3D oldLocation)
        {
        }

        private Item m_Hair, m_Beard;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Hair
        {
            get
            {
                if (m_Hair != null && !m_Hair.Deleted && m_Hair.Parent == this)
                    return m_Hair;

                return m_Hair = FindItemOnLayer(Layer.Hair);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Beard
        {
            get
            {
                if (m_Beard != null && !m_Beard.Deleted && m_Beard.Parent == this)
                    return m_Beard;

                return m_Beard = FindItemOnLayer(Layer.FacialHair);
            }
        }

        public bool HasFreeHand()
        {
            return FindItemOnLayer(Layer.TwoHanded) == null;
        }

        private IWeapon m_Weapon;

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual IWeapon Weapon
        {
            get
            {
                Item item = m_Weapon as Item;

                if (item != null && !item.Deleted && item.Parent == this && CanSee(item))
                    return m_Weapon;

                m_Weapon = null;

                item = FindItemOnLayer(Layer.OneHanded);

                if (item == null)
                    item = FindItemOnLayer(Layer.TwoHanded);

                if (item is IWeapon)
                    return (m_Weapon = (IWeapon)item);
                else
                    return GetDefaultWeapon();
            }
        }

        public virtual IWeapon GetDefaultWeapon()
        {
            return m_DefaultWeapon;
        }
        public Item FindItemByType(Type type)
        {
            foreach (Item item in Items)
                if (type.IsAssignableFrom(item.GetType()))
                    return item;
            return null;
        }

        private BankBox m_BankBox;
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public BankBox BankBox
        {
            get
            {
                if (m_BankBox != null && !m_BankBox.Deleted && m_BankBox.Parent == this)
                    return m_BankBox;

                m_BankBox = FindItemOnLayer(Layer.Bank) as BankBox;

                if (m_BankBox == null)
                    AddItem(m_BankBox = new BankBox(this));

                return m_BankBox;
            }
            set
            {
                m_BankBox = value;
            }
        }

        public BankBox FindBankNoCreate()
        {
            if (m_BankBox != null && !m_BankBox.Deleted && m_BankBox.Parent == this)
                return m_BankBox;

            m_BankBox = FindItemOnLayer(Layer.Bank) as BankBox;

            return m_BankBox;
        }

        private Container m_Backpack;
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public Container Backpack
        {
            get
            {
                if (m_Backpack != null && !m_Backpack.Deleted && m_Backpack.Parent == this)
                    return m_Backpack;

                return (m_Backpack = (FindItemOnLayer(Layer.Backpack) as Container));
            }
            set
            {
                if (FindItemOnLayer(Layer.Backpack) == null && value is Backpack)
                {
                    value.Movable = false;
                    AddItem(value);
                }
                else
                    m_Backpack = value;
            }
        }
        public virtual bool KeepsItemsOnDeath
        {
            get
            {
                return Region.KeepsItemsOnDeath() || m_AccessLevel > AccessLevel.Player;
            }
        }

        public Item FindItemOnLayer(Layer layer)
        {
            List<Item> eq = m_Items;
            int count = eq.Count;

            for (int i = 0; i < count; ++i)
            {
                Item item = (Item)eq[i];

                if (!item.Deleted && item.Layer == layer)
                {
                    return item;
                }
            }

            return null;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int X
        {
            get { return m_Location.m_X; }
            set { Location = new Point3D(value, m_Location.m_Y, m_Location.m_Z); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Y
        {
            get { return m_Location.m_Y; }
            set { Location = new Point3D(m_Location.m_X, value, m_Location.m_Z); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Z
        {
            get { return m_Location.m_Z; }
            set { Location = new Point3D(m_Location.m_X, m_Location.m_Y, value); }
        }

        public void MovingEffect(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode)
        {
            Effects.SendMovingEffect(this, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode);
        }

        public void MovingEffect(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes)
        {
            Effects.SendMovingEffect(this, to, itemID, speed, duration, fixedDirection, explodes, 0, 0);
        }

        public void MovingParticles(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, EffectLayer layer, int unknown)
        {
            Effects.SendMovingParticles(this, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, layer, unknown);
        }

        public void MovingParticles(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int hue, int renderMode, int effect, int explodeEffect, int explodeSound, int unknown)
        {
            Effects.SendMovingParticles(this, to, itemID, speed, duration, fixedDirection, explodes, hue, renderMode, effect, explodeEffect, explodeSound, (EffectLayer)255, unknown);
        }

        public void MovingParticles(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int effect, int explodeEffect, int explodeSound, int unknown)
        {
            Effects.SendMovingParticles(this, to, itemID, speed, duration, fixedDirection, explodes, effect, explodeEffect, explodeSound, unknown);
        }

        public void MovingParticles(IEntity to, int itemID, int speed, int duration, bool fixedDirection, bool explodes, int effect, int explodeEffect, int explodeSound)
        {
            Effects.SendMovingParticles(this, to, itemID, speed, duration, fixedDirection, explodes, 0, 0, effect, explodeEffect, explodeSound, 0);
        }

        public void FixedEffect(int itemID, int speed, int duration, int hue, int renderMode)
        {
            Effects.SendTargetEffect(this, itemID, speed, duration, hue, renderMode);
        }

        public void FixedEffect(int itemID, int speed, int duration)
        {
            Effects.SendTargetEffect(this, itemID, speed, duration, 0, 0);
        }

        public void FixedParticles(int itemID, int speed, int duration, int effect, int hue, int renderMode, EffectLayer layer, int unknown)
        {
            Effects.SendTargetParticles(this, itemID, speed, duration, hue, renderMode, effect, layer, unknown);
        }

        public void FixedParticles(int itemID, int speed, int duration, int effect, int hue, int renderMode, EffectLayer layer)
        {
            Effects.SendTargetParticles(this, itemID, speed, duration, hue, renderMode, effect, layer, 0);
        }

        public void FixedParticles(int itemID, int speed, int duration, int effect, EffectLayer layer, int unknown)
        {
            Effects.SendTargetParticles(this, itemID, speed, duration, 0, 0, effect, layer, unknown);
        }

        public void FixedParticles(int itemID, int speed, int duration, int effect, EffectLayer layer)
        {
            Effects.SendTargetParticles(this, itemID, speed, duration, 0, 0, effect, layer, 0);
        }

        public void BoltEffect(int hue)
        {
            Effects.SendBoltEffect(this, true, hue);
        }
#if true
        public void SendIncomingPacket()
        {
            if (m_Map != null)
            {
                IPooledEnumerable<NetState> eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state.Mobile.CanSee(this))
                    {
                        state.Send(MobileIncoming.Create(state, state.Mobile, this));

                        if (state.StygianAbyss)
                        {
                            if (m_Poison != null)
                                state.Send(new HealthbarPoison(this));

                            if (m_Blessed || m_YellowHealthbar)
                                state.Send(new HealthbarYellow(this));
                        }

                        if (IsDeadBondedPet)
                            state.Send(new BondedStatus(0, m_Serial, 1));

                        if (ObjectPropertyList.Enabled)
                        {
                            state.Send(OPLPacket);

                            //foreach ( Item item in m_Items )
                            //	state.Send( item.OPLPacket );
                        }
                    }
                }

                eable.Free();
            }
        }
#else
        public void SendIncomingPacket()
        {
            if (m_Map != null)
            {
                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state.Mobile.CanSee(this))
                    {
                        state.Send(new MobileIncoming(state.Mobile, this));

                        if (IsDeadBondedPet)
                            state.Send(new BondedStatus(0, m_Serial, 1));

                        if (ObjectPropertyList.Enabled)
                        {
                            state.Send(OPLPacket);

                            //foreach ( Item item in m_Items )
                            //	state.Send( item.OPLPacket );
                        }
                    }
                }

                eable.Free();
            }
        }
#endif
        public bool PlaceInBackpack(Item item)
        {
            if (item.Deleted)
                return false;

            Container pack = this.Backpack;

            return pack != null && pack.TryDropItem(this, item, false);
        }

        public bool AddToBackpack(Item item)
        {
            if (item.Deleted)
                return false;

            if (!PlaceInBackpack(item))
            {
                Point3D loc = m_Location;
                Map map = m_Map;

                if ((map == null || map == Map.Internal) && m_LogoutMap != null)
                {
                    loc = m_LogoutLocation;
                    map = m_LogoutMap;
                }

                item.MoveToWorld(loc, map);
                return false;
            }

            return true;
        }

        //		public virtual bool CheckLift( Mobile from, Item item )
        //		{
        //			return true;
        //        }
        public virtual bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            return true;
        }

        public virtual bool CheckNonlocalLift(Mobile from, Item item)
        {
            if (from == this || (from.AccessLevel > this.AccessLevel && from.AccessLevel >= AccessLevel.GameMaster))
                return true;

            return false;
        }

        public bool HasTrade
        {
            get
            {
                if (m_NetState != null)
                    return m_NetState.Trades.Count > 0;

                return false;
            }
        }

        public virtual bool CheckTrade(Mobile to, Item item, SecureTradeContainer cont, bool message, bool checkItems, int plusItems, int plusWeight)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Event invoked when a Mobile (<paramref name="from" />) drops an <see cref="Item"><paramref name="dropped" /></see> onto the Mobile.
        /// </summary>
        public virtual bool OnDragDrop(Mobile from, Item dropped)
        {
            if (from == this)
            {
                Container pack = this.Backpack;

                if (pack != null)
                    return dropped.DropToItem(from, pack, new Point3D(-1, -1, 0));

                return false;
            }
            else if (from.Player && this.Player && from.Alive && this.Alive && from.InRange(Location, 2))
            {
                NetState ourState = m_NetState;
                NetState theirState = from.m_NetState;

                if (ourState != null && theirState != null)
                {
                    SecureTradeContainer cont = theirState.FindTradeContainer(this);

                    if (!from.CheckTrade(this, dropped, cont, true, true, 0, 0))
                        return false;

                    if (cont == null)
                        cont = theirState.AddTrade(ourState);

                    cont.DropItem(dropped);

                    return true;
                }

                return false;
            }
            else
            {
                return false;
            }
        }

        public virtual bool CheckEquip(Item item)
        {
            for (int i = 0; i < m_Items.Count; ++i)
                if (((Item)m_Items[i]).CheckConflictingLayer(this, item, item.Layer) || item.CheckConflictingLayer(this, (Item)m_Items[i], ((Item)m_Items[i]).Layer))
                    return false;

            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile attempts to wear <paramref name="item" />.
        /// </summary>
        /// <returns>True if the request is accepted, false if otherwise.</returns>
        public virtual bool OnEquip(Item item)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile attempts to lift <paramref name="item" />.
        /// </summary>
        /// <returns>True if the lift is allowed, false if otherwise.</returns>
        /// <example>
        /// The following example demonstrates usage. It will disallow any attempts to pick up a pick axe if the Mobile does not have enough strength.
        /// <code>
        /// public override bool OnDragLift( Item item )
        /// {
        ///		if ( item is Pickaxe &amp;&amp; this.Str &lt; 60 )
        ///		{
        ///			SendMessage( "That is too heavy for you to lift." );
        ///			return false;
        ///		}
        ///		
        ///		return base.OnDragLift( item );
        /// }</code>
        /// </example>
        public virtual bool OnDragLift(Item item)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> into a <see cref="Container"><paramref name="container" /></see>.
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemInto(Item item, Container container, Point3D loc)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> directly onto another <see cref="Item" />, <paramref name="target" />. This is the case of stacking items.
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemOnto(Item item, Item target)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> into another <see cref="Item" />, <paramref name="target" />. The target item is most likely a <see cref="Container" />.
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemToItem(Item item, Item target, Point3D loc)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile attempts to give <paramref name="item" /> to a Mobile (<paramref name="target" />).
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemToMobile(Item item, Mobile target)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile attempts to drop <paramref name="item" /> to the world at a <see cref="Point3D"><paramref name="location" /></see>.
        /// </summary>
        /// <returns>True if the drop is allowed, false if otherwise.</returns>
        public virtual bool OnDroppedItemToWorld(Item item, Point3D location)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event when <paramref name="from" /> successfully uses <paramref name="item" /> while it's on this Mobile.
        /// <seealso cref="Item.OnItemUsed" />
        /// </summary>
        public virtual void OnItemUsed(Mobile from, Item item)
        {
        }

        public virtual bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            if (from == this || (from.AccessLevel > this.AccessLevel && from.AccessLevel >= AccessLevel.GameMaster))
                return true;

            return false;
        }

        public virtual bool CheckItemUse(Mobile from, Item item)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when <paramref name="from" /> successfully lifts <paramref name="item" /> from this Mobile.
        /// <seealso cref="Item.OnItemLifted" />
        /// </summary>
        public virtual void OnItemLifted(Mobile from, Item item)
        {
        }

        public virtual bool AllowItemUse(Item item)
        {
            return true;
        }

        public virtual bool AllowEquipFrom(Mobile mob)
        {
            return (mob == this || (mob.AccessLevel >= AccessLevel.GameMaster && mob.AccessLevel > this.AccessLevel));
        }

        public virtual bool EquipItem(Item item)
        {
            if (item == null || item.Deleted || !item.CanEquip(this))
                return false;

            //check region for equip requests.
            if (!Region.EquipItem(this, item))
                return false;

            if (CheckEquip(item) && OnEquip(item) && item.OnEquip(this))
            {
                if (m_Spell != null && !m_Spell.OnCasterEquiping(item))
                    return false;

                //if ( m_Spell != null && m_Spell.State == SpellState.Casting )
                //	m_Spell.Disturb( DisturbType.EquipRequest );

                AddItem(item);
                return true;
            }

            return false;
        }

        internal int m_TypeRef;

        public Mobile(Serial serial)
        {
            m_Region = Map.Internal.DefaultRegion;
            m_Serial = serial;
            m_Aggressors = new List<AggressorInfo>();
            m_Aggressed = new List<AggressorInfo>();
            m_NextSkillTime = Core.TickCount;
            m_DamageEntries = new List<DamageEntry>();

            Type ourType = this.GetType();
            m_TypeRef = World.m_MobileTypes.IndexOf(ourType);
#if true
            if (m_TypeRef == -1)
            {
                World.m_MobileTypes.Add(ourType);
                m_TypeRef = World.m_MobileTypes.Count - 1;
            }
#else
            if (m_TypeRef == -1)
                m_TypeRef = World.m_MobileTypes.Add(ourType);
#endif
        }
        //public Mobile(string name, double minute_delete_delay)
        //    : this()
        //{
        //    Name = name;
        //    Timer.DelayCall(TimeSpan.FromMinutes(minute_delete_delay), Delete);
        //}
        public Mobile()
        {
            m_Region = Map.Internal.DefaultRegion;
            m_Serial = Server.Serial.NewMobile;

            DefaultMobileInit();

            World.AddMobile(this);

            Type ourType = this.GetType();
            m_TypeRef = World.m_MobileTypes.IndexOf(ourType);

            if (m_TypeRef == -1)
                m_TypeRef = World.m_MobileTypes.Add(ourType);
        }

        public void DefaultMobileInit()
        {
            m_StatCap = 225;
            m_FollowersMax = (Core.RuleSets.SiegeStyleRules() && Core.SiegeII_CFG) ? 8 : Core.RuleSets.SiegeStyleRules() ? 16 : 5;
            m_Skills = new Skills(this);
            m_Items = new List<Item>(1);
            m_StatMods = new ArrayList(1);
            Map = Map.Internal;
            m_AutoPageNotify = true;
            m_Aggressors = new List<AggressorInfo>();
            m_Aggressed = new List<AggressorInfo>();
            m_Virtues = new VirtueInfo();
            //m_PetStable = new ArrayList(1);
            m_DamageEntries = new List<DamageEntry>();

            m_NextSkillTime = Core.TickCount;
            m_CreationTime = DateTime.UtcNow;
        }

        private static Queue<Mobile> m_DeltaQueue = new Queue<Mobile>();
        private static Queue<Mobile> m_DeltaQueueR = new Queue<Mobile>();

        private bool m_InDeltaQueue;
        private MobileDelta m_DeltaFlags;
        [CopyableAttribute(CopyType.DoNotCopy)] // I guess
        public MobileDelta DeltaFlags
        {
            get { return m_DeltaFlags; }
            set { m_DeltaFlags = value; }
        }

        public virtual void Delta(MobileDelta flag)
        {
#if true
            if (m_Map == null || m_Map == Map.Internal || m_Deleted)
                return;

            m_DeltaFlags |= flag;

            if (!m_InDeltaQueue)
            {
                m_InDeltaQueue = true;

                if (_processing)
                {
                    lock (m_DeltaQueueR)
                    {
                        m_DeltaQueueR.Enqueue(this);

                        try
                        {
                            using (StreamWriter op = new StreamWriter("delta-recursion.log", true))
                            {
                                op.WriteLine("# {0}", DateTime.UtcNow);
                                op.WriteLine(new System.Diagnostics.StackTrace());
                                op.WriteLine();
                            }
                        }
                        catch { }
                    }
                }
                else
                {
                    m_DeltaQueue.Enqueue(this);
                }
            }

            Core.Set();
#else
            if (m_Map == null || m_Map == Map.Internal || m_Deleted)
                return;

            m_DeltaFlags |= flag;

            if (!m_InDeltaQueue)
            {
                m_InDeltaQueue = true;

                m_DeltaQueue.Enqueue(this);
            }

            Core.Set();
#endif
        }

        private bool m_NoMoveHS;

        public bool NoMoveHS
        {
            get { return m_NoMoveHS; }
            set { m_NoMoveHS = value; }
        }

        public Direction GetDirectionTo(int x, int y)
        {
            int dx = m_Location.m_X - x;
            int dy = m_Location.m_Y - y;

            int rx = (dx - dy) * 44;
            int ry = (dx + dy) * 44;

            int ax = Math.Abs(rx);
            int ay = Math.Abs(ry);

            Direction ret;

            if (((ay >> 1) - ax) >= 0)
                ret = (ry > 0) ? Direction.Up : Direction.Down;
            else if (((ax >> 1) - ay) >= 0)
                ret = (rx > 0) ? Direction.Left : Direction.Right;
            else if (rx >= 0 && ry >= 0)
                ret = Direction.West;
            else if (rx >= 0 && ry < 0)
                ret = Direction.South;
            else if (rx < 0 && ry < 0)
                ret = Direction.East;
            else
                ret = Direction.North;

            return ret;
        }

        public Direction GetDirectionTo(Point2D p)
        {
            return GetDirectionTo(p.m_X, p.m_Y);
        }

        public Direction GetDirectionTo(Point3D p)
        {
            return GetDirectionTo(p.m_X, p.m_Y);
        }

        public Direction GetDirectionTo(IPoint2D p)
        {
            if (p == null)
                return Direction.North;

            return GetDirectionTo(p.X, p.Y);
        }
#if true
        public virtual void ProcessDelta()
        {
            Mobile m = this;
            MobileDelta delta;

            delta = m.m_DeltaFlags;

            if (delta == MobileDelta.None)
                return;

            MobileDelta attrs = delta & MobileDelta.Attributes;

            m.m_DeltaFlags = MobileDelta.None;
            m.m_InDeltaQueue = false;

            bool sendHits = false, sendStam = false, sendMana = false, sendAll = false, sendAny = false;
            bool sendIncoming = false, sendNonlocalIncoming = false;
            bool sendUpdate = false, sendRemove = false;
            bool sendPublicStats = false, sendPrivateStats = false;
            bool sendMoving = false, sendNonlocalMoving = false;
            bool sendOPLUpdate = ObjectPropertyList.Enabled && (delta & MobileDelta.Properties) != 0;

            bool sendHair = false, sendFacialHair = false, removeHair = false, removeFacialHair = false;

            bool sendHealthbarPoison = false, sendHealthbarYellow = false;

            if (attrs != MobileDelta.None)
            {
                sendAny = true;

                if (attrs == MobileDelta.Attributes)
                {
                    sendAll = true;
                }
                else
                {
                    sendHits = ((attrs & MobileDelta.Hits) != 0);
                    sendStam = ((attrs & MobileDelta.Stam) != 0);
                    sendMana = ((attrs & MobileDelta.Mana) != 0);
                }
            }

            if ((delta & MobileDelta.GhostUpdate) != 0)
            {
                sendNonlocalIncoming = true;
            }

            if ((delta & MobileDelta.Hue) != 0)
            {
                sendNonlocalIncoming = true;
                sendUpdate = true;
                sendRemove = true;
            }

            if ((delta & MobileDelta.Direction) != 0)
            {
                sendNonlocalMoving = true;
                sendUpdate = true;
            }

            if ((delta & MobileDelta.Body) != 0)
            {
                sendUpdate = true;
                sendIncoming = true;
            }

            /*if ( (delta & MobileDelta.Hue) != 0 )
				{
					sendNonlocalIncoming = true;
					sendUpdate = true;
				}
				else if ( (delta & (MobileDelta.Direction | MobileDelta.Body)) != 0 )
				{
					sendNonlocalMoving = true;
					sendUpdate = true;
				}
				else*/
            if ((delta & (MobileDelta.Flags | MobileDelta.Noto)) != 0)
            {
                sendMoving = true;
            }

            if ((delta & MobileDelta.HealthbarPoison) != 0)
            {
                sendHealthbarPoison = true;
            }

            if ((delta & MobileDelta.HealthbarYellow) != 0)
            {
                sendHealthbarYellow = true;
            }

            if ((delta & MobileDelta.Name) != 0)
            {
                sendAll = false;
                sendHits = false;
                sendAny = sendStam || sendMana;
                sendPublicStats = true;
            }

            if ((delta & (MobileDelta.WeaponDamage | MobileDelta.Resistances | MobileDelta.Stat |
                MobileDelta.Weight | MobileDelta.Gold | MobileDelta.Armor | MobileDelta.StatCap |
                MobileDelta.Followers | MobileDelta.TithingPoints | MobileDelta.Race)) != 0)
            {
                sendPrivateStats = true;
            }

            if ((delta & MobileDelta.Hair) != 0)
            {
                if (m.HairItemID <= 0)
                    removeHair = true;

                sendHair = true;
            }

            if ((delta & MobileDelta.FacialHair) != 0)
            {
                if (m.FacialHairItemID <= 0)
                    removeFacialHair = true;

                sendFacialHair = true;
            }

            Packet[][] cache = new Packet[2][] { new Packet[8], new Packet[8] };

            NetState ourState = m.m_NetState;

            if (ourState != null)
            {
                if (sendUpdate)
                {
                    ourState.Sequence = 0;

                    if (ourState.StygianAbyss)
                        ourState.Send(new MobileUpdate(m));
                    else
                        ourState.Send(new MobileUpdateOld(m));

                    ClearFastwalkStack();
                }

                if (sendIncoming)
                    ourState.Send(MobileIncoming.Create(ourState, m, m));

                if (ourState.StygianAbyss)
                {
                    if (sendMoving)
                    {
                        int noto = Notoriety.Compute(m, m);
                        ourState.Send(cache[0][noto] = Packet.Acquire(new MobileMoving(m, noto)));
                    }

                    if (sendHealthbarPoison)
                        ourState.Send(new HealthbarPoison(m));

                    if (sendHealthbarYellow)
                        ourState.Send(new HealthbarYellow(m));
                }
                else
                {
                    if (sendMoving || sendHealthbarPoison || sendHealthbarYellow)
                    {
                        int noto = Notoriety.Compute(m, m);
                        ourState.Send(cache[1][noto] = Packet.Acquire(new MobileMovingOld(m, noto)));
                    }
                }

                if (sendPublicStats || sendPrivateStats)
                {
                    ourState.Send(new MobileStatusExtended(m, m_NetState));
                }
                else if (sendAll)
                {
                    ourState.Send(new MobileAttributes(m));
                }
                else if (sendAny)
                {
                    if (sendHits)
                        ourState.Send(new MobileHits(m));

                    if (sendStam)
                        ourState.Send(new MobileStam(m));

                    if (sendMana)
                        ourState.Send(new MobileMana(m));
                }

                if (sendStam || sendMana)
                {
                    IParty ip = m_Party as IParty;

                    if (ip != null && sendStam)
                        ip.OnStamChanged(this);

                    if (ip != null && sendMana)
                        ip.OnManaChanged(this);
                }

                if (sendHair)
                {
                    if (removeHair)
                        ourState.Send(new RemoveHair(m));
                    else
                        ourState.Send(new HairEquipUpdate(m));
                }

                if (sendFacialHair)
                {
                    if (removeFacialHair)
                        ourState.Send(new RemoveFacialHair(m));
                    else
                        ourState.Send(new FacialHairEquipUpdate(m));
                }

                if (sendOPLUpdate)
                    ourState.Send(OPLPacket);
            }

            sendMoving = sendMoving || sendNonlocalMoving;
            sendIncoming = sendIncoming || sendNonlocalIncoming;
            sendHits = sendHits || sendAll;

            if (m.m_Map != null && (sendRemove || sendIncoming || sendPublicStats || sendHits || sendMoving || sendOPLUpdate || sendHair || sendFacialHair || sendHealthbarPoison || sendHealthbarYellow))
            {
                Mobile beholder;

                Packet hitsPacket = null;
                Packet statPacketTrue = null;
                Packet statPacketFalse = null;
                Packet deadPacket = null;
                Packet hairPacket = null;
                Packet facialhairPacket = null;
                Packet hbpPacket = null;
                Packet hbyPacket = null;

                IPooledEnumerable<NetState> eable = m.Map.GetClientsInRange(m.m_Location);

                foreach (NetState state in eable)
                {
                    beholder = state.Mobile;

                    if (beholder != m && beholder.CanSee(m))
                    {
                        if (sendRemove)
                            state.Send(this.RemovePacket);

                        if (sendIncoming)
                        {
                            state.Send(MobileIncoming.Create(state, beholder, m));

                            if (m.IsDeadBondedPet)
                            {
                                if (deadPacket == null)
                                    deadPacket = Packet.Acquire(new BondedStatus(0, m.m_Serial, 1));

                                state.Send(deadPacket);
                            }
                        }

                        if (state.StygianAbyss)
                        {
                            if (sendMoving)
                            {
                                int noto = Notoriety.Compute(beholder, m);

                                Packet p = cache[0][noto];

                                if (p == null)
                                    cache[0][noto] = p = Packet.Acquire(new MobileMoving(m, noto));

                                state.Send(p);
                            }

                            if (sendHealthbarPoison)
                            {
                                if (hbpPacket == null)
                                    hbpPacket = Packet.Acquire(new HealthbarPoison(m));

                                state.Send(hbpPacket);
                            }

                            if (sendHealthbarYellow)
                            {
                                if (hbyPacket == null)
                                    hbyPacket = Packet.Acquire(new HealthbarYellow(m));

                                state.Send(hbyPacket);
                            }
                        }
                        else
                        {
                            if (sendMoving || sendHealthbarPoison || sendHealthbarYellow)
                            {
                                int noto = Notoriety.Compute(beholder, m);

                                Packet p = cache[1][noto];

                                if (p == null)
                                    cache[1][noto] = p = Packet.Acquire(new MobileMovingOld(m, noto));

                                state.Send(p);
                            }
                        }

                        if (sendPublicStats)
                        {
                            if (m.CanBeRenamedBy(beholder))
                            {
                                if (statPacketTrue == null)
                                    statPacketTrue = Packet.Acquire(new MobileStatusCompact(true, m));

                                state.Send(statPacketTrue);
                            }
                            else
                            {
                                if (statPacketFalse == null)
                                    statPacketFalse = Packet.Acquire(new MobileStatusCompact(false, m));

                                state.Send(statPacketFalse);
                            }
                        }
                        else if (sendHits)
                        {
                            if (hitsPacket == null)
                                hitsPacket = Packet.Acquire(new MobileHitsN(m));

                            state.Send(hitsPacket);
                        }

                        if (sendHair)
                        {
                            if (hairPacket == null)
                            {
                                if (removeHair)
                                    hairPacket = Packet.Acquire(new RemoveHair(m));
                                else
                                    hairPacket = Packet.Acquire(new HairEquipUpdate(m));
                            }

                            state.Send(hairPacket);
                        }

                        if (sendFacialHair)
                        {
                            if (facialhairPacket == null)
                            {
                                if (removeFacialHair)
                                    facialhairPacket = Packet.Acquire(new RemoveFacialHair(m));
                                else
                                    facialhairPacket = Packet.Acquire(new FacialHairEquipUpdate(m));
                            }

                            state.Send(facialhairPacket);
                        }

                        if (sendOPLUpdate)
                            state.Send(this.OPLPacket);
                    }
                }

                Packet.Release(hitsPacket);
                Packet.Release(statPacketTrue);
                Packet.Release(statPacketFalse);
                Packet.Release(deadPacket);
                Packet.Release(hairPacket);
                Packet.Release(facialhairPacket);
                Packet.Release(hbpPacket);
                Packet.Release(hbyPacket);

                eable.Free();
            }

            if (sendMoving || sendNonlocalMoving || sendHealthbarPoison || sendHealthbarYellow)
            {
                for (int i = 0; i < cache.Length; ++i)
                    for (int j = 0; j < cache[i].Length; ++j)
                        Packet.Release(ref cache[i][j]);
            }
        }

        private static bool _processing = false;

        public static void ProcessDeltaQueue()
        {
            _processing = true;

            if (m_DeltaQueue.Count >= 512)
            {
                Parallel.ForEach(m_DeltaQueue, m => m.ProcessDelta());
                m_DeltaQueue.Clear();
            }
            else
            {
                while (m_DeltaQueue.Count > 0) m_DeltaQueue.Dequeue().ProcessDelta();
            }

            _processing = false;

            while (m_DeltaQueueR.Count > 0) m_DeltaQueueR.Dequeue().ProcessDelta();
        }

#else
        public virtual void ProcessDelta()
        {
            Mobile m = this;
            MobileDelta delta;

            delta = m.m_DeltaFlags;

            if (delta == MobileDelta.None)
                return;

            MobileDelta attrs = delta & MobileDelta.Attributes;

            m.m_DeltaFlags = MobileDelta.None;
            m.m_InDeltaQueue = false;

            bool sendHits = false, sendStam = false, sendMana = false, sendAll = false, sendAny = false;
            bool sendIncoming = false, sendNonlocalIncoming = false;
            bool sendUpdate = false, sendRemove = false;
            bool sendPublicStats = false, sendPrivateStats = false;
            bool sendMoving = false, sendNonlocalMoving = false;
            bool sendOPLUpdate = ObjectPropertyList.Enabled && (delta & MobileDelta.Properties) != 0;

            bool sendHair = false, sendFacialHair = false, removeHair = false, removeFacialHair = false;
            
            bool sendHealthbarPoison = false, sendHealthbarYellow = false;
            
            if (attrs != MobileDelta.None)
            {
                sendAny = true;

                if (attrs == MobileDelta.Attributes)
                {
                    sendAll = true;
                }
                else
                {
                    sendHits = ((attrs & MobileDelta.Hits) != 0);
                    sendStam = ((attrs & MobileDelta.Stam) != 0);
                    sendMana = ((attrs & MobileDelta.Mana) != 0);
                }
            }

            if ((delta & MobileDelta.GhostUpdate) != 0)
            {
                sendNonlocalIncoming = true;
            }

            if ((delta & MobileDelta.Hue) != 0)
            {
                sendNonlocalIncoming = true;
                sendUpdate = true;
                sendRemove = true;
            }

            if ((delta & MobileDelta.Direction) != 0)
            {
                sendNonlocalMoving = true;
                sendUpdate = true;
            }

            if ((delta & MobileDelta.Body) != 0)
            {
                sendUpdate = true;
                sendIncoming = true;
            }

            /*if ( (delta & MobileDelta.Hue) != 0 )
				{
					sendNonlocalIncoming = true;
					sendUpdate = true;
				}
				else if ( (delta & (MobileDelta.Direction | MobileDelta.Body)) != 0 )
				{
					sendNonlocalMoving = true;
					sendUpdate = true;
				}
				else*/
            if ((delta & (MobileDelta.Flags | MobileDelta.Noto)) != 0)
            {
                sendMoving = true;
            }

            if ((delta & MobileDelta.HealthbarPoison) != 0)
            {
                sendHealthbarPoison = true;
            }

            if ((delta & MobileDelta.HealthbarYellow) != 0)
            {
                sendHealthbarYellow = true;
            }

            if ((delta & MobileDelta.Name) != 0)
            {
                sendAll = false;
                sendHits = false;
                sendAny = sendStam || sendMana;
                sendPublicStats = true;
            }

            if ((delta & (MobileDelta.WeaponDamage | MobileDelta.Resistances | MobileDelta.Stat | 
                MobileDelta.Weight | MobileDelta.Gold | MobileDelta.Armor | MobileDelta.StatCap | 
                MobileDelta.Followers | MobileDelta.TithingPoints | MobileDelta.Race)) != 0)
            {
                sendPrivateStats = true;
            }

            if ((delta & MobileDelta.Hair) != 0)
            {
                if (m.HairItemID <= 0)
                    removeHair = true;

                sendHair = true;
            }

            if ((delta & MobileDelta.FacialHair) != 0)
            {
                if (m.FacialHairItemID <= 0)
                    removeFacialHair = true;

                sendFacialHair = true;
            }

            Packet[] cache = m_MovingPacketCache;

            if (sendMoving || sendNonlocalMoving)
            {
                for (int i = 0; i < cache.Length; ++i)
                    Packet.Release(ref cache[i]);
            }

            NetState ourState = m.m_NetState;

            if (ourState != null)
            {
                if (sendUpdate)
                {
                    ourState.Sequence = 0;
                    ourState.Send(new MobileUpdate(m));
                    ClearFastwalkStack();
                }

                if (sendIncoming)
                    ourState.Send(new MobileIncoming(m, m));

                if (sendMoving)
                {
                    int noto = Notoriety.Compute(m, m);
                    ourState.Send(cache[noto] = Packet.Acquire(new MobileMoving(m, noto)));
                }

                if (sendPublicStats || sendPrivateStats)
                {
                    ourState.Send(new MobileStatusExtended(m));
                }
                else if (sendAll)
                {
                    ourState.Send(new MobileAttributes(m));
                }
                else if (sendAny)
                {
                    if (sendHits)
                        ourState.Send(new MobileHits(m));

                    if (sendStam)
                        ourState.Send(new MobileStam(m));

                    if (sendMana)
                        ourState.Send(new MobileMana(m));
                }

                if (sendStam || sendMana)
                {
                    IParty ip = m_Party as IParty;

                    if (ip != null && sendStam)
                        ip.OnStamChanged(this);

                    if (ip != null && sendMana)
                        ip.OnManaChanged(this);
                }

                if (sendHair)
                {
                    if (removeHair)
                        ourState.Send(new RemoveHair(m));
                    else
                        ourState.Send(new HairEquipUpdate(m));
                }

                if (sendFacialHair)
                {
                    if (removeFacialHair)
                        ourState.Send(new RemoveFacialHair(m));
                    else
                        ourState.Send(new FacialHairEquipUpdate(m));
                }

                if (sendOPLUpdate)
                    ourState.Send(OPLPacket);
            }

            sendMoving = sendMoving || sendNonlocalMoving;
            sendIncoming = sendIncoming || sendNonlocalIncoming;
            sendHits = sendHits || sendAll;

            if (m.m_Map != null && (sendRemove || sendIncoming || sendPublicStats || sendHits || sendMoving || sendOPLUpdate || sendHair || sendFacialHair))
            {
                Mobile beholder;

                IPooledEnumerable eable = m.Map.GetClientsInRange(m.m_Location);

                Packet hitsPacket = null;
                Packet statPacketTrue = null, statPacketFalse = null;
                Packet deadPacket = null;
                Packet hairPacket = null, facialhairPacket = null;

                foreach (NetState state in eable)
                {
                    beholder = state.Mobile;

                    if (beholder != m && beholder.CanSee(m))
                    {
                        if (sendRemove)
                            state.Send(m.RemovePacket);

                        if (sendIncoming)
                        {
                            state.Send(new MobileIncoming(beholder, m));

                            if (m.IsDeadBondedPet)
                            {
                                if (deadPacket == null)
                                    deadPacket = Packet.Acquire(new BondedStatus(0, m.m_Serial, 1));

                                state.Send(deadPacket);
                            }
                        }

                        if (sendMoving)
                        {
                            int noto = Notoriety.Compute(beholder, m);

                            Packet p = cache[noto];

                            if (p == null)
                                cache[noto] = p = Packet.Acquire(new MobileMoving(m, noto));

                            state.Send(p);
                        }

                        if (sendPublicStats)
                        {
                            if (m.CanBeRenamedBy(beholder))
                            {
                                if (statPacketTrue == null)
                                    statPacketTrue = Packet.Acquire(new MobileStatusCompact(true, m));

                                state.Send(statPacketTrue);
                            }
                            else
                            {
                                if (statPacketFalse == null)
                                    statPacketFalse = Packet.Acquire(new MobileStatusCompact(false, m));

                                state.Send(statPacketFalse);
                            }
                        }
                        else if (sendHits)
                        {
                            if (hitsPacket == null)
                                hitsPacket = Packet.Acquire(new MobileHitsN(m));

                            state.Send(hitsPacket);
                        }

                        if (sendHair)
                        {
                            if (hairPacket == null)
                            {
                                if (removeHair)
                                    hairPacket = Packet.Acquire(new RemoveHair(m));
                                else
                                    hairPacket = Packet.Acquire(new HairEquipUpdate(m));
                            }

                            state.Send(hairPacket);
                        }

                        if (sendFacialHair)
                        {
                            if (facialhairPacket == null)
                            {
                                if (removeFacialHair)
                                    facialhairPacket = Packet.Acquire(new RemoveFacialHair(m));
                                else
                                    facialhairPacket = Packet.Acquire(new FacialHairEquipUpdate(m));
                            }

                            state.Send(facialhairPacket);
                        }

                        if (sendOPLUpdate)
                            state.Send(OPLPacket);
                    }
                }

                Packet.Release(hitsPacket);
                Packet.Release(statPacketTrue);
                Packet.Release(statPacketFalse);
                Packet.Release(deadPacket);
                Packet.Release(hairPacket);
                Packet.Release(facialhairPacket);

                eable.Free();
            }

            if (sendMoving || sendNonlocalMoving)
            {
                for (int i = 0; i < cache.Length; ++i)
                    Packet.Release(ref cache[i]);
            }
        }

        public static void ProcessDeltaQueue()
        {
            int count = m_DeltaQueue.Count;
            int index = 0;

            while (m_DeltaQueue.Count > 0 && index++ < count)
                m_DeltaQueue.Dequeue().ProcessDelta();
        }
#endif
        public bool Red
        {
            get { return this.LongTermMurders >= 5; }
        }
        public bool IsMurderer
        {
            get { return this.Red; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int LongTermMurders
        {
            get
            {
                return m_Kills;
            }
            set
            {
                int oldValue = m_Kills;

                if (m_Kills != value)
                {
                    m_Kills = value;

                    if (m_Kills < 0)
                        m_Kills = 0;

                    if ((oldValue >= 5) != (m_Kills >= 5))
                    {
                        Delta(MobileDelta.Noto);
                        InvalidateProperties();
                    }

                    OnKillsChange(oldValue);
                }
            }
        }

        public virtual void OnKillsChange(int oldValue)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int ShortTermMurders
        {
            get
            {
                return m_ShortTermMurders;
            }
            set
            {
                if (m_ShortTermMurders != value)
                {
                    m_ShortTermMurders = value;

                    if (m_ShortTermMurders < 0)
                        m_ShortTermMurders = 0;
                }
            }
        }

        public virtual bool Evil
        {
            get
            {
                // players my now be evil and this has notoriety consequences. To remain consistent, we need access in the Mobile class
                return false;
            }
        }

        public virtual bool Hero
        {
            get
            {
                // players my now be Hero and this has notoriety consequences. To remain consistent, we need access in the Mobile class
                return false;
            }
        }
        private Point3D m_sceneOfTheCrime = Point3D.Zero;
        public Point3D SceneOfTheCrime
        {
            get { return m_sceneOfTheCrime; }
            set { m_sceneOfTheCrime = value; }
        }
        #region WhackTimer
        private Timer m_CriminalWhackTime;
        public bool IsGuardWackable { get { return m_CriminalWhackTime != null; } }
        public bool GuardWackable { get { return !GetMobileBool(MobileBoolTable.NotGuardWackable); } set { SetMobileBool(MobileBoolTable.NotGuardWackable, !value); } }
        private void ResetCriminalWhackTime()
        {
            if (m_CriminalWhackTime == null)
                m_CriminalWhackTime = Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerStateCallback(WhackTick), new object[] { null });
            else
            {
                m_CriminalWhackTime.Stop();
                m_CriminalWhackTime.Flush();
                m_CriminalWhackTime = Timer.DelayCall(TimeSpan.FromSeconds(10), new TimerStateCallback(WhackTick), new object[] { null });
            }
        }
        private void WhackTick(object state)
        {
            m_CriminalWhackTime.Stop();
            m_CriminalWhackTime.Flush();
            m_CriminalWhackTime = null;
            if (this.Region != null && this.Region.IsGuarded)
                this.SendLocalizedMessage(502276); // Guards can no longer be called on you.
        }
        #endregion WhackTimer
        public void CriminalityBegin()
        {
            // adam: record where a crime takes place so that townspeople can't call guards on something they didn't see
            SceneOfTheCrime = Location;

            if (GuardWackable)
            {
                // Criminal Whack Timer tracks the 10 seconds whereby a player can be guard whacked.
                //  This is different than the 2 minute criminal timer.
                ResetCriminalWhackTime();

                if (this.Region != null && this.Region.IsGuarded /*&& !alreadWarned*/)
                    this.SendLocalizedMessage(502275); // Guards can now be called on you!
            }
            else
                this.SendLocalizedMessage(1005041); // You've committed a criminal act!!

            GuardWackable = true;
        }

        public void CriminalityEnd()
        {
            SceneOfTheCrime = Point3D.Zero;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public virtual bool Criminal
        {
            get
            {
                return m_Criminal;
            }
            set
            {
                if (m_Criminal != value)
                {
                    m_Criminal = value;
                    Delta(MobileDelta.Noto);
                    InvalidateProperties();
                }

                if (m_Criminal)
                {
                    if (m_ExpireCriminal == null)
                        m_ExpireCriminal = new ExpireCriminalTimer(this);
                    else
                    {   // adam: don't recreate unless the new timer is longer than the time remaining on the old timer.
                        // Serialization: we need to handle the case where we have an extended criminal timer
                        //	currently a restart will default to resetting this short timer.
                        try
                        {
                            if (DateTime.UtcNow + ExpireCriminalDelay > m_ExpireCriminal.NextTick)
                                m_ExpireCriminal.Stop();
                        }
                        catch (Exception ex)
                        {
                            LogHelper.LogException(ex);
                            m_ExpireCriminal.Stop();
                        }
                    }

                    if (m_ExpireCriminal.Running == false)
                        m_ExpireCriminal.Start();

                    // setup common Criminality flags and warnings
                    CriminalityBegin();
                }
                else if (m_ExpireCriminal != null)
                {
                    m_ExpireCriminal.Stop();
                    m_ExpireCriminal = null;

                    // cleanup common Criminality flags and warnings
                    CriminalityEnd();
                }
            }
        }

        public void MakeCriminal(TimeSpan howLong)
        {
            if (m_Criminal != true)
            {
                m_Criminal = true;
                Delta(MobileDelta.Noto);
                InvalidateProperties();
            }

            if (m_ExpireCriminal == null)
                m_ExpireCriminal = new ExpireCriminalTimer(this, howLong);
            else
            {   // adam: don't recreate unless the new timer is longer than the time remaining on the old timer.
                // Serialization: we need to handle the case where we have an extended criminal timer
                //	currently a restart will default to resetting this short timer.
                if (DateTime.UtcNow + howLong > m_ExpireCriminal.NextTick)
                {
                    m_ExpireCriminal.Stop();
                    m_ExpireCriminal = new ExpireCriminalTimer(this, howLong);
                }
            }

            if (m_ExpireCriminal.Running == false)
                m_ExpireCriminal.Start();
        }

        public bool CheckAlive()
        {
            return CheckAlive(true);
        }

        public bool CheckAlive(bool message)
        {
            if (!Alive)
            {
                if (message)
                    this.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019048); // I am dead and cannot do that.

                return false;
            }
            else
            {
                return true;
            }
        }

        public void PublicOverheadMessage(MessageType type, int hue, bool ascii, string text)
        {
            PublicOverheadMessage(type, hue, ascii, text, true);
        }

        public void PublicOverheadMessage(MessageType type, int hue, bool ascii, string text, bool noLineOfSight)
        {
            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    // wea: changed to a check to see if target is audible to the speaker 
                    // if ( state.Mobile.CanSee( this ) && (noLineOfSight || state.Mobile.InLOS( this )) )
                    if (state.Mobile.CanSee(this) && (noLineOfSight || state.Mobile.IsAudibleTo(this)))
                    {
                        if (p == null)
                        {
                            if (ascii)
                                p = new AsciiMessage(m_Serial, Body, type, hue, 3, Name, text);
                            else
                                p = new UnicodeMessage(m_Serial, Body, type, hue, 3, m_Language, Name, text);

                            p.Acquire();
                        }

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number)
        {
            PublicOverheadMessage(type, hue, number, "", true);
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number, string args)
        {
            PublicOverheadMessage(type, hue, number, args, true);
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number, string args, bool noLineOfSight)
        {
            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    // wea: InLOS -> IsAudibleTo
                    if (state.Mobile.CanSee(this) && (noLineOfSight || state.Mobile.IsAudibleTo(this)))
                    {
                        if (p == null)
                            p = Packet.Acquire(new MessageLocalized(m_Serial, Body, type, hue, 3, number, Name, args));

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number, AffixType affixType, string affix, string args)
        {
            PublicOverheadMessage(type, hue, number, affixType, affix, args, true);
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number, AffixType affixType, string affix, string args, bool noLineOfSight)
        {
            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    // wea: InLOS -> IsAudibleTo
                    if (state.Mobile.CanSee(this) && (noLineOfSight || state.Mobile.IsAudibleTo(this)))
                    {
                        if (p == null)
                            p = Packet.Acquire(new MessageLocalizedAffix(m_Serial, Body, type, hue, 3, number, Name, affixType, affix, args));

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void PrivateOverheadMessage(MessageType type, int hue, bool ascii, string text, NetState state)
        {
            if (state == null) return;

            if (ascii)
                state.Send(new AsciiMessage(m_Serial, Body, type, hue, 3, Name, text));
            else
                state.Send(new UnicodeMessage(m_Serial, Body, type, hue, 3, m_Language, Name, text));
        }

        public void PrivateOverheadMessage(MessageType type, int hue, int number, NetState state)
        {
            PrivateOverheadMessage(type, hue, number, "", state);
        }

        public void PrivateOverheadMessage(MessageType type, int hue, int number, string args, NetState state)
        {
            if (state == null)
                return;

            state.Send(new MessageLocalized(m_Serial, Body, type, hue, 3, number, Name, args));
        }

        public void LocalOverheadMessage(MessageType type, int hue, bool ascii, string text)
        {
            NetState ns = m_NetState;

            if (ns != null)
            {
                if (ascii)
                    ns.Send(new AsciiMessage(m_Serial, Body, type, hue, 3, Name, text));
                else
                    ns.Send(new UnicodeMessage(m_Serial, Body, type, hue, 3, m_Language, Name, text));
            }
        }

        public void LocalOverheadMessage(MessageType type, int hue, int number)
        {
            LocalOverheadMessage(type, hue, number, "");
        }

        public void LocalOverheadMessage(MessageType type, int hue, int number, string args)
        {
            NetState ns = m_NetState;

            if (ns != null)
                ns.Send(new MessageLocalized(m_Serial, Body, type, hue, 3, number, Name, args));
        }

        public void NonlocalOverheadMessage(MessageType type, int hue, int number)
        {
            NonlocalOverheadMessage(type, hue, number, "");
        }

        public void NonlocalOverheadMessage(MessageType type, int hue, int number, string args)
        {
            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state != m_NetState && state.Mobile.CanSee(this))
                    {
                        if (p == null)
                            p = Packet.Acquire(new MessageLocalized(m_Serial, Body, type, hue, 3, number, Name, args));

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void NonlocalOverheadMessage(MessageType type, int hue, bool ascii, string text)
        {
            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state != m_NetState && state.Mobile.CanSee(this))
                    {
                        if (p == null)
                        {
                            if (ascii)
                                p = new AsciiMessage(m_Serial, Body, type, hue, 3, Name, text);
                            else
                                p = new UnicodeMessage(m_Serial, Body, type, hue, 3, Language, Name, text);

                            p.Acquire();
                        }

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void NonlocalStaffOverheadMessage(MessageType type, int hue, int number)
        {
            NonlocalStaffOverheadMessage(type, hue, number, "");
        }

        public void NonlocalStaffOverheadMessage(MessageType type, int hue, int number, string args)
        {
            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state != m_NetState && state.Mobile.CanSee(this))
                    {
                        if (p == null)
                            p = Packet.Acquire(new MessageLocalized(m_Serial, Body, type, hue, 3, number, Name, args));

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void NonlocalStaffOverheadMessage(MessageType type, int hue, bool ascii, string text)
        {
            if (m_Map != null)
            {
                Packet p = null;

                IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location);

                foreach (NetState state in eable)
                {
                    if (state != m_NetState && state.Mobile.CanSee(this) && state.Mobile.AccessLevel > AccessLevel.Player)
                    {
                        if (p == null)
                        {
                            if (ascii)
                                p = new AsciiMessage(m_Serial, Body, type, hue, 3, Name, text);
                            else
                                p = new UnicodeMessage(m_Serial, Body, type, hue, 3, Language, Name, text);

                            p.Acquire();
                        }

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }
        public void SendLocalizedMessage(int number)
        {
            NetState ns = m_NetState;

            if (ns != null)
                ns.Send(MessageLocalized.InstantiateGeneric(number));
        }

        public void SendLocalizedMessage(int number, string args)
        {
            SendLocalizedMessage(number, args, 0x3B2);
        }

        public void SendLocalizedMessage(int number, string args, int hue)
        {
            if (hue == 0x3B2 && (args == null || args.Length == 0))
            {
                NetState ns = m_NetState;

                if (ns != null)
                    ns.Send(MessageLocalized.InstantiateGeneric(number));
            }
            else
            {
                NetState ns = m_NetState;

                if (ns != null)
                    ns.Send(new MessageLocalized(Serial.MinusOne, -1, MessageType.Regular, hue, 3, number, "System", args));
            }
        }

        public void SendLocalizedMessage(int number, bool append, string affix)
        {
            SendLocalizedMessage(number, append, affix, "", 0x3B2);
        }

        public void SendLocalizedMessage(int number, bool append, string affix, string args)
        {
            SendLocalizedMessage(number, append, affix, args);
        }

        public void SendLocalizedMessage(int number, bool append, string affix, string args, int hue)
        {
            NetState ns = m_NetState;

            if (ns != null)
                ns.Send(new MessageLocalizedAffix(Serial.MinusOne, -1, MessageType.Regular, hue, 3, number, "System", (append ? AffixType.Append : AffixType.Prepend) | AffixType.System, affix, args));
        }

        public void LaunchBrowser(string url)
        {
            if (m_NetState != null)
                m_NetState.LaunchBrowser(url);
        }
        private bool m_muteMessages = false;
        public bool MuteMessages
        {
            get { return m_muteMessages; }
            set { m_muteMessages = value; }
        }
        public void SendMessage(string text)
        {
            SendMessage(0x3B2, text);
        }

        public void SendMessage(string format, params object[] args)
        {
            SendMessage(0x3B2, string.Format(format, args));
        }

        public void SendMessage(int hue, string text)
        {
            if (m_muteMessages)
                return;

            NetState ns = m_NetState;

            if (ns != null)
                ns.Send(new UnicodeMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, "ENU", "System", text));
        }

        public void SendMessage(int hue, string format, params object[] args)
        {
            SendMessage(hue, string.Format(format, args));
        }

        public void SendAsciiMessage(string text)
        {
            SendAsciiMessage(0x3B2, text);
        }

        public void SendAsciiMessage(string format, params object[] args)
        {
            SendAsciiMessage(0x3B2, string.Format(format, args));
        }

        public void SendAsciiMessage(int hue, string text)
        {
            if (m_muteMessages)
                return;

            NetState ns = m_NetState;

            if (ns != null)
                ns.Send(new AsciiMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, "System", text));
        }

        public void SendAsciiMessage(int hue, string format, params object[] args)
        {
            SendAsciiMessage(hue, string.Format(format, args));
        }
        public void SendAsciiMessageTo(NetState ns, int hue, string text)
        {
            if (m_muteMessages)
                return;

            if (ns != null)
                ns.Send(new AsciiMessage(Serial.MinusOne, -1, MessageType.Regular, hue, 3, Name, text));
        }
        // allow mobiles to relay status back to the GM.
        public virtual void SendSystemMessage(string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            if (text != null)
            {
                IPooledEnumerable eable = this.GetMobilesInRange(10);
                foreach (Mobile m in eable)
                    if (m.AccessLevel >= accesslevel)
                        m.SendMessage(hue, text);
                eable.Free();
            }
        }
        public bool InRange(Point2D p, int range)
        {
            return (p.m_X >= (m_Location.m_X - range))
                && (p.m_X <= (m_Location.m_X + range))
                && (p.m_Y >= (m_Location.m_Y - range))
                && (p.m_Y <= (m_Location.m_Y + range));
        }

        public bool InRange(Point3D p, int range)
        {
            return (p.m_X >= (m_Location.m_X - range))
                && (p.m_X <= (m_Location.m_X + range))
                && (p.m_Y >= (m_Location.m_Y - range))
                && (p.m_Y <= (m_Location.m_Y + range));
        }

        public bool InRange(IPoint2D p, int range)
        {
            return (p.X >= (m_Location.m_X - range))
                && (p.X <= (m_Location.m_X + range))
                && (p.Y >= (m_Location.m_Y - range))
                && (p.Y <= (m_Location.m_Y + range));
        }

        public void InitStats(int str, int dex, int intel)
        {
            m_Str = str;
            m_Dex = dex;
            m_Int = intel;

            Hits = HitsMax;
            Stam = StamMax;
            Mana = ManaMax;

            Delta(MobileDelta.Stat | MobileDelta.Hits | MobileDelta.Stam | MobileDelta.Mana);
        }

        public virtual void DisplayPaperdollTo(Mobile to)
        {
            EventSink.InvokePaperdollRequest(new PaperdollRequestEventArgs(to, this));
        }

        private static bool m_DisableDismountInWarmode;

        public static bool DisableDismountInWarmode { get { return m_DisableDismountInWarmode; } set { m_DisableDismountInWarmode = value; } }

        /// <summary>
        /// Overridable. Event invoked when the Mobile is double clicked. By default, this method can either dismount or open the paperdoll.
        /// <seealso cref="CanPaperdollBeOpenedBy" />
        /// <seealso cref="DisplayPaperdollTo" />
        /// </summary>
        public virtual void OnDoubleClick(Mobile from)
        {
            if (this == from && (!m_DisableDismountInWarmode || !m_Warmode))
            {
                IMount mount = Mount;

                if (mount != null)
                {
                    mount.Rider = null;
                    return;
                }
            }

            if (CanPaperdollBeOpenedBy(from))
                DisplayPaperdollTo(from);
        }

        public virtual void OnDismount(Mobile mount)
        {
        }

        public virtual void OnMount(Mobile mount)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile is double clicked by someone who is over 18 tiles away.
        /// <seealso cref="OnDoubleClick" />
        /// </summary>
        public virtual void OnDoubleClickOutOfRange(Mobile from)
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the Mobile is double clicked by someone who can no longer see the Mobile. This may happen, for example, using 'Last Object' after the Mobile has hidden.
        /// <seealso cref="OnDoubleClick" />
        /// </summary>
        public virtual void OnDoubleClickCantSee(Mobile from)
        {
        }

        /// <summary>
        /// Overridable. Event invoked when the Mobile is double clicked by someone who is not alive. Similar to <see cref="OnDoubleClick" />, this method will show the paperdoll. It does not, however, provide any dismount functionality.
        /// <seealso cref="OnDoubleClick" />
        /// </summary>
        public virtual void OnDoubleClickDead(Mobile from)
        {
            if (CanPaperdollBeOpenedBy(from))
                DisplayPaperdollTo(from);
        }

        /// <summary>
        /// Overridable. Event invoked when the Mobile requests to open his own paperdoll via the 'Open Paperdoll' macro.
        /// </summary>
        public virtual void OnPaperdollRequest()
        {
            if (CanPaperdollBeOpenedBy(this))
                DisplayPaperdollTo(this);
        }

        private static int m_BodyWeight = 14;

        public static int BodyWeight { get { return m_BodyWeight; } set { m_BodyWeight = value; } }

        /// <summary>
        /// Overridable. Event invoked when <paramref name="from" /> wants to see this Mobile's stats.
        /// </summary>
        /// <param name="from"></param>
        public virtual void OnStatsQuery(Mobile from)
        {
            if (from.Map == this.Map && Utility.InUpdateRange(this, from) && from.CanSee(this))
                from.Send(new MobileStatus(from, this));

            if (from == this)
                Send(new StatLockInfo(this));

            IParty ip = m_Party as IParty;

            if (ip != null)
                ip.OnStatsQuery(from, this);
        }

        /// <summary>
        /// Overridable. Event invoked when <paramref name="from" /> wants to see this Mobile's skills.
        /// </summary>
        public virtual void OnSkillsQuery(Mobile from)
        {
            if (from == this)
                Send(new SkillUpdate(m_Skills));
        }

        /// <summary>
        /// Overridable. Virtual event invoked when <see cref="Region" /> changes.
        /// </summary>
        public virtual void OnRegionChange(Region Old, Region New)
        {
            if (Old.UId == 1 && Old.UId != New.UId)
                OnBirthRegion(New); // they are just being created on the internal map    
        }
        /// <summary>
        /// Overridable. Virtual event invoked when <see cref="Region" /> changes.
        /// </summary>
        public virtual void OnBirthRegion(Region Birth)
        {
            return;
        }

        private Item m_MountItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public IMount Mount
        {
            get
            {
                IMountItem mountItem = null;

                if (m_MountItem != null && !m_MountItem.Deleted && m_MountItem.Parent == this)
                    mountItem = (IMountItem)m_MountItem;

                if (mountItem == null)
                    m_MountItem = (mountItem = (FindItemOnLayer(Layer.Mount) as IMountItem)) as Item;

                return mountItem == null ? null : mountItem.Mount;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Mounted
        {
            get
            {
                return (Mount != null);
            }
        }

        private QuestArrow m_QuestArrow;

        public QuestArrow QuestArrow
        {
            get
            {
                return m_QuestArrow;
            }
            set
            {
                if (m_QuestArrow != value)
                {
                    if (m_QuestArrow != null)
                        m_QuestArrow.Stop();

                    m_QuestArrow = value;
                }
            }
        }

        public virtual bool Remembers(object o) { return false; }
        public virtual bool CanTarget { get { return true; } }

        // I don't know when shopkeepers got their title
        // Question(12) on the boards
        public virtual bool ClickTitle { get { return (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish > 8) ? true : false; } }

        private static bool m_DisableHiddenSelfClick = true;

        public static bool DisableHiddenSelfClick { get { return m_DisableHiddenSelfClick; } set { m_DisableHiddenSelfClick = value; } }

        private static bool m_AsciiClickMessage = true;

        public static bool AsciiClickMessage { get { return m_AsciiClickMessage; } set { m_AsciiClickMessage = value; } }

        private static bool m_GuildClickMessage = true;

        public static bool GuildClickMessage { get { return m_GuildClickMessage; } set { m_GuildClickMessage = value; } }

        public virtual bool ShowFameTitle { get { return true; } }//(m_Player || m_Body.IsHuman) && m_Fame >= 10000; } 

        /// <summary>
        /// Overridable. Event invoked when the Mobile is single clicked.
        /// </summary>
        public virtual void OnSingleClick(Mobile from)
        {
            if (m_Deleted)
                return;
            else if (AccessLevel == AccessLevel.Player && DisableHiddenSelfClick && Hidden && from == this)
                return;

            if (m_GuildClickMessage)
            {
                BaseGuild guild = m_Guild;

                if (guild != null && (m_DisplayGuildTitle || (m_Player && guild.ForceGuildTitle)))
                {
                    string title = GuildTitle;

                    if (title == null)
                        title = "";
                    else
                        title = title.Trim();

                    string text = string.Format(title.Length <= 0 ? "[{1}]{2}" : "[{0}, {1}]{2}", title, guild.Abbreviation, guild.GetSuffix(this));

                    PrivateOverheadMessage(MessageType.Regular, SpeechHue, true, text, from.NetState);
                }
            }

            int hue;

            if (m_NameHue != -1)
                hue = m_NameHue;
            else if (AccessLevel > AccessLevel.Player)
                hue = 11;
            else
            {
                int notoriety = Notoriety.Compute(from, this);
                hue = Notoriety.GetHue(notoriety);

                //PIX: if they're looking innocent, see if there
                // are any ill-effects from beneficial actions
                if (notoriety == Notoriety.Innocent)
                {
                    int namehue = Notoriety.GetBeneficialHue(from, this);
                    if (namehue != 0)
                    {
                        hue = namehue;
                    }
                }
            }


            string name = Name;

            if (name == null)
                name = string.Empty;

            string prefix = "";

            if (ShowFameTitle && (m_Player || m_Body.IsHuman) && m_Fame >= 10000)
                prefix = (m_Female ? "Lady" : "Lord");

            string suffix = "";

            if (ClickTitle && Title != null && Title.Length > 0)
                suffix = Title;

            suffix = ApplyNameSuffix(suffix);

            // June 2, 2001
            // http://martin.brenner.de/ultima/uo/news1.html
            // the (invulnerable) tag has been removed; invulnerable NPCs and players can now be identified by the yellow hue of their name
            // Adam: June 2, 2001 probably means Publish 12 which was July 24, 2001
            if (IsInvulnerable && !Core.RuleSets.AnyAIShardRules() && PublishInfo.Publish < 12)
                if ((this is BaseVendor || this is PlayerVendor))
                    suffix = string.Concat(suffix, " ", "(invulnerable)");

            string val;

            if (prefix.Length > 0 && suffix.Length > 0)
                val = string.Concat(prefix, " ", name, " ", suffix);
            else if (prefix.Length > 0)
                val = string.Concat(prefix, " ", name);
            else if (suffix.Length > 0)
                val = string.Concat(name, " ", suffix);
            else
                val = name;

            PrivateOverheadMessage(MessageType.Label, hue, m_AsciiClickMessage, val, from.NetState);
        }

        [CommandProperty(AccessLevel.Administrator, AccessLevel.Owner)]
        public virtual bool IsInvulnerable
        {
            get { return GetMobileBool(MobileBoolTable.IsInvulnerable); }
            set
            {   // 10/24/22, Adam: moving all the 'invulnerable' logic and notes here. We should handle derived 
                //  class specifics in those specific classes.
                // Publish 4
                // Any shopkeeper that is currently [invulnerable] will lose that status except for stablemasters.
                // IsInvulnerable = (!Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules() && PublishInfo.Publish < 8) ? false : true;
                // --
                // June 2, 2001
                // http://martin.brenner.de/ultima/uo/news1.html
                // the (invulnerable) tag has been removed; invulnerable NPCs and players can now be identified by the yellow hue of their name
                // Adam: June 2, 2001 probably means Publish 12 which was July 24, 2001
                SetMobileBool(MobileBoolTable.IsInvulnerable, value);
                NameHue = CalcInvulNameHue();
            }
        }
        // June 2, 2001
        // http://martin.brenner.de/ultima/uo/news1.html
        // the (invulnerable) tag has been removed; invulnerable NPCs and players can now be identified by the yellow hue of their name
        // Adam: June 2, 2001 probably means Publish 12 which was July 24, 2001
        // Adam: to allow us to restart the same world under a different rule set, we need to reset the name hue here
        public int CalcInvulNameHue()
        {
            if (IsInvulnerable && !Core.RuleSets.AOSRules() && (Core.RuleSets.AnyAIShardRules() || PublishInfo.Publish >= 12))
                return 0x35;
            else
                return -1;
        }

        public virtual void DisruptiveAction()
        {
            if (Meditating)
            {
                Meditating = false;
                SendLocalizedMessage(500134); // You stop meditating.
            }
        }

        public Item ShieldArmor
        {
            get
            {
                return FindItemOnLayer(Layer.TwoHanded) as Item;
            }
        }

        public Item NeckArmor
        {
            get
            {
                return FindItemOnLayer(Layer.Neck) as Item;
            }
        }

        public Item HandArmor
        {
            get
            {
                return FindItemOnLayer(Layer.Gloves) as Item;
            }
        }

        public Item HeadArmor
        {
            get
            {
                return FindItemOnLayer(Layer.Helm) as Item;
            }
        }

        public Item ArmsArmor
        {
            get
            {
                return FindItemOnLayer(Layer.Arms) as Item;
            }
        }

        public Item LegsArmor
        {
            get
            {
                Item ar = FindItemOnLayer(Layer.InnerLegs) as Item;

                if (ar == null)
                    ar = FindItemOnLayer(Layer.Pants) as Item;

                return ar;
            }
        }

        public Item ChestArmor
        {
            get
            {
                Item ar = FindItemOnLayer(Layer.InnerTorso) as Item;

                if (ar == null)
                    ar = FindItemOnLayer(Layer.Shirt) as Item;

                return ar;
            }
        }

        /// <summary>
        /// Gets or sets the maximum attainable value for <see cref="RawStr" />, <see cref="RawDex" />, and <see cref="RawInt" />.
        /// </summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int StatCap
        {
            get
            {
                return m_StatCap;
            }
            set
            {
                if (m_StatCap != value)
                {
                    m_StatCap = value;

                    Delta(MobileDelta.StatCap);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Meditating
        {
            get
            {
                return m_Meditating;
            }
            set
            {
                m_Meditating = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanSwim
        {
            get
            {
                return m_CanSwim;
            }
            set
            {
                m_CanSwim = value;
            }
        }

        public bool CantSwim
        {
            get
            {
                return !m_CanSwim;
            }
            set
            {
                m_CanSwim = !value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CanFlyOver
        {
            get
            {
                return m_CanFlyOver;
            }
            set
            {
                m_CanFlyOver = value;
            }
        }

        public bool CantFlyOver
        {
            get
            {
                return !m_CanFlyOver;
            }
            set
            {
                m_CanFlyOver = !value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool CantWalkLand
        {
            get
            {
                return m_CantWalkLand;
            }
            set
            {
                m_CantWalkLand = value;
            }
        }

        public bool CanWalkLand
        {
            get
            {
                return !m_CantWalkLand;
            }
            set
            {
                m_CantWalkLand = !value;
            }
        }
        public bool WaterOnly
        { get { return m_CantWalkLand && CanSwim; } }
        public bool LandOnly
        { get { return CanWalkLand && !CanSwim; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool CanHearGhosts
        {
            get
            {
                return m_CanHearGhosts || AccessLevel >= AccessLevel.Counselor;
            }
            set
            {
                m_CanHearGhosts = value;
            }
        }
        public bool CanSeeGhosts
        {
            get
            {
                return CanHearGhosts;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int RawStatTotal
        {
            get
            {
                return RawStr + RawDex + RawInt;
            }
        }

        public DateTime NextSpellTime
        {
            get
            {
                return m_NextSpellTime;
            }
            set
            {
                m_NextSpellTime = value;
            }
        }

        public Serial ReassignSerial(Serial s)
        {
            SendRemovePacket();
            World.RemoveMobile(this);
            m_Serial = s;
            World.AddMobile(this);
            //m_WorldPacket = null;
            //Delta(ItemDelta.Update);

            return m_Serial;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the sector this Mobile is in gets <see cref="Sector.Activate">activated</see>.
        /// </summary>
        public virtual void OnSectorActivate()
        {
        }

        /// <summary>
        /// Overridable. Virtual event invoked when the sector this Mobile is in gets <see cref="Sector.Deactivate">deactivated</see>.
        /// </summary>
        public virtual void OnSectorDeactivate()
        {
        }
        public int GetHueForNameInList()
        {
            switch (this.AccessLevel)
            {
                case AccessLevel.Owner: return 0x35;
                case AccessLevel.Administrator: return 0x516;
                case AccessLevel.Seer: return 0x144;
                case AccessLevel.GameMaster: return 0x21;
                case AccessLevel.Counselor: return 0x2;
                case AccessLevel.FightBroker: return 0x8AB;
                case AccessLevel.Reporter: return 0x979;
                case AccessLevel.Player:
                default:
                    {
                        // 8/10/22, Adam: I don't think Core.RedsInTown matters here. 
                        if (this.Red)
                            return 0x21;
                        else if (this.Criminal)
                            return 0x3B1;

                        return 0x58;
                    }
            }
        }

        #region CheckSkill

        public virtual bool CheckSkill(SkillName skillName, double minSkill, double maxSkill, object[] contextObj)
        {
            Skill skill = Skills[skillName];

            if (skill == null)
                return false;

            double chance = (skill.Value - minSkill) / (maxSkill - minSkill);

            contextObj[1] = new Point2D(Location.X, Location.Y);
            return CheckSkill(skill, chance, contextObj);
        }

        public virtual bool CheckSkill(SkillName skillName, double chance, object[] contextObj)
        {
            Skill skill = Skills[skillName];

            if (skill == null)
                return false;

            contextObj[1] = new Point2D(Location.X, Location.Y);
            return CheckSkill(skill, chance, contextObj);
        }

        public bool CheckTargetSkillTooHard(SkillName skillName, double minSkill, double maxSkill)
        {
            Skill skill = Skills[skillName];

            double chance = (skill.Value - minSkill) / (maxSkill - minSkill);

            if (chance < 0.0)
            {
                return true; // Too difficult
            }
            else if (chance >= 1.0)
            {
                return false; // No challenge
            }

            return false; // chance
        }
        public virtual bool CheckTargetSkill(SkillName skillName, object target, double minSkill, double maxSkill, object[] contextObj)
        {
            Skill skill = Skills[skillName];

            if (skill == null)
                return false;

            double chance = (skill.Value - minSkill) / (maxSkill - minSkill);

            return CheckSkill(skill, chance, contextObj);
        }

        public virtual bool CheckTargetSkill(SkillName skillName, object target, double chance, object[] contextObj)
        {
            Skill skill = Skills[skillName];

            if (skill == null)
                return false;

            return CheckSkill(skill, chance, contextObj);
        }

        protected virtual double GainChance(Skill skill, double chance, bool success)
        {
            double gc = 0.5;
            gc += (skill.Cap - skill.Base) / skill.Cap;
            gc /= 2;

            gc += (1.0 - chance) * (success ? 0.5 : 0.2);
            gc /= 2;

            gc *= skill.Info.GainFactor;

            if (gc < 0.01)
                gc = 0.01;

            return gc;
        }
        public virtual bool UseROTForSkill(Skill skill)
        {
            return false;
        }
        protected virtual bool CheckSkill(Skill skill, double chance, object[] contextObj)
        {
            if (skill == null)
                return false;
            if (Skills.Cap == 0)
                return false;

            if (chance < 0.0)
                return false; // Too difficult
            else if (chance >= 1.0)
                return true; // No challenge

            bool success = (chance >= Utility.RandomDouble());
            double gc = GainChance(skill, chance, success);

            if (Alive && AllowGain(skill, contextObj))
            {
                OnBeforeGain(skill, ref gc);

                if (skill.Base < 10.0 || Utility.RandomDouble() < gc)
                {
                    int oldBaseFixed = skill.BaseFixedPoint;

                    Gain(skill);

                    OnAfterGain(skill, oldBaseFixed);
                }
                else
                {
                    OnAfterFailedGain(skill, gc);
                }
            }

            return success;
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.GameMaster)]
        public bool TrainingMobile
        {
            get { return GetMobileBool(MobileBoolTable.TrainingMobile); }
            set
            {
                SetMobileBool(MobileBoolTable.TrainingMobile, value);
                SendSystemMessage(string.Format($"Skills can be gained {value}"));
            }
        }
        protected virtual bool AllowGain(Skill skill, object[] contextObj)
        {
            if (Region != null && Region.IsJailRules)
                return false;

            // you cannot gain skill from BlockDamage creatures unless it is a training mobile
            if (contextObj != null && contextObj[0] is BaseCreature bc)
                if (bc.BlockDamage == true && !bc.TrainingMobile)
                {
                    Utility.Monitor.DebugOut(string.Format($"Cannot gain skill from {contextObj[0]}"), ConsoleColor.DarkRed);
                    return false;
                }

            return true;
        }

        protected virtual void OnBeforeGain(Skill skill, ref double gc)
        {
        }

        protected virtual void OnAfterGain(Skill skill, double oldBase)
        {
        }
        protected virtual void OnAfterFailedGain(Skill skill, double gc)
        {
        }
        public virtual void Gain(Skill skill)
        {
            if (skill.Base < skill.Cap && skill.Lock == SkillLock.Up)
            {
                int toGain = 1;

                if (skill.Base <= 10.0)
                    toGain = Utility.Random(4) + 1;

                if ((Skills.Total / Skills.Cap) >= Utility.RandomDouble())//( skills.Total >= skills.Cap )
                {   // see if we need to lower something (Why do we use a random here?)
                    for (int i = 0; i < Skills.Length; ++i)
                    {
                        Skill toLower = Skills[i];

                        if (toLower != skill && toLower.Lock == SkillLock.Down && toLower.BaseFixedPoint >= toGain)
                        {
                            toLower.BaseFixedPoint -= toGain;
                            break;
                        }
                    }
                }

                if ((Skills.Total + toGain) <= Skills.Cap)
                {
                    skill.BaseFixedPoint += toGain;
                }
            }

            if (skill.Lock == SkillLock.Up)
                CheckStatGain(skill);
        }

        protected virtual void CheckStatGain(Skill skill)
        {
            if (StrLock == StatLockType.Up && StatGainChance(skill, Stat.Str) > Utility.RandomDouble())
            {
                //Console.WriteLine("GainStat(from, Stat.Str)");
                GainStat(Stat.Str);
            }
            else if (DexLock == StatLockType.Up && StatGainChance(skill, Stat.Dex) > Utility.RandomDouble())
            {
                //Console.WriteLine("GainStat(from, Stat.Dex)");
                GainStat(Stat.Dex);
            }
            else if (IntLock == StatLockType.Up && StatGainChance(skill, Stat.Int) > Utility.RandomDouble())
            {
                //Console.WriteLine("GainStat(from, Stat.Int)");
                GainStat(Stat.Int);
            }
        }

        public virtual double StatGainChance(Skill skill, Stat stat)
        {
#if false
            if (skill.SkillName == SkillName.ArmsLore)
                Console.WriteLine("Chance to gain Str {0}%, Chance to gain Dex {1}%, Chance to gain Int {2}%",
                        (skill.Info.StrGain / 20.3),
                        (skill.Info.DexGain / 20.3),
                        (skill.Info.IntGain / 20.3)
                        );
#endif
            switch (stat)
            {
                case Stat.Str:
                    return skill.Info.StrGain / 20.0;
                case Stat.Dex:
                    return skill.Info.DexGain / 20.0;
                case Stat.Int:
                    return skill.Info.IntGain / 20.0;
            }

            return 0.0;
        }

        public virtual bool CanLower(Stat stat)
        {
            switch (stat)
            {
                case Stat.Str: return (StrLock == StatLockType.Down && RawStr > 10 && StrMax != -1);
                case Stat.Dex: return (DexLock == StatLockType.Down && RawDex > 10 && DexMax != -1);
                case Stat.Int: return (IntLock == StatLockType.Down && RawInt > 10 && IntMax != -1);
            }

            return false;
        }

        public virtual bool CanRaise(Stat stat)
        {
            switch (stat)
            {
                case Stat.Str: return (StrLock == StatLockType.Up && RawStr < StrMax && StrMax != -1);
                case Stat.Dex: return (DexLock == StatLockType.Up && RawDex < DexMax && DexMax != -1);
                case Stat.Int: return (IntLock == StatLockType.Up && RawInt < IntMax && IntMax != -1);
            }

            return false;
        }

        public virtual void ValidateStatCap(Stat increased)
        {
            if (RawStatTotal <= StatCap || StatCap == -1)
                return; // no work to be done

            switch (increased)
            {
                case Stat.Str:
                    {
                        if (CanLower(Stat.Dex) && (RawDex < RawInt || !CanLower(Stat.Int)))
                            RawDex--;
                        else if (CanLower(Stat.Int))
                            RawInt--;
                        else
                            RawStr--;

                        break;
                    }
                case Stat.Dex:
                    {
                        if (CanLower(Stat.Str) && (RawStr < RawInt || !CanLower(Stat.Int)))
                            RawStr--;
                        else if (CanLower(Stat.Int))
                            RawInt--;
                        else
                            RawDex--;

                        break;
                    }
                case Stat.Int:
                    {
                        if (CanLower(Stat.Dex) && (RawDex < RawStr || !CanLower(Stat.Str)))
                            RawDex--;
                        else if (CanLower(Stat.Str))
                            RawStr--;
                        else
                            RawInt--;

                        break;
                    }
            }
        }
#if false
 protected virtual void IncreaseStat(Stat stat, bool atrophy)
        {
            if (!CanRaise(stat))
                return;

            switch (stat)
            {
                case Stat.Str:
                    {
                        int oldRawStr = RawStr;

                        RawStr++;

                        OnAfterStatGain(StatType.Str, oldRawStr);

                        break;
                    }
                case Stat.Dex:
                    {
                        int oldRawDex = RawDex;

                        RawDex++;

                        OnAfterStatGain(StatType.Dex, oldRawDex);

                        break;
                    }
                case Stat.Int:
                    {
                        int oldRawInt = RawInt;

                        RawInt++;

                        OnAfterStatGain(StatType.Int, oldRawInt);

                        break;
                    }
            }

            ValidateStatCap(stat); // TODO: Repeat until we're done?
        }
#else

        protected virtual void IncreaseStat(Stat stat, bool atrophy)
        {
            if (!CanRaise(stat))
                return;

            int oldValue = 0;

            switch (stat)
            {
                case Stat.Str:
                    {
                        oldValue = RawStr;
                        RawStr++;
                        break;
                    }
                case Stat.Dex:
                    {
                        oldValue = RawDex;
                        RawDex++;
                        break;
                    }
                case Stat.Int:
                    {
                        oldValue = RawInt;
                        RawInt++;
                        break;
                    }
            }

            ValidateStatCap(stat); // TODO: Repeat until we're done?

            switch (stat)
            {
                case Stat.Str:
                    {
                        if (oldValue < RawStr)
                            OnAfterStatGain(StatType.Str, oldValue);
                        break;
                    }
                case Stat.Dex:
                    {
                        if (oldValue < RawDex)
                            OnAfterStatGain(StatType.Dex, oldValue);
                        break;
                    }
                case Stat.Int:
                    {
                        if (oldValue < RawInt)
                            OnAfterStatGain(StatType.Int, oldValue);
                        break;
                    }
            }
        }
#endif
        protected virtual void OnAfterStatGain(StatType stat, int oldStatRaw)
        {
        }

        private static TimeSpan m_StatGainDelay = TimeSpan.FromMinutes(2.0);

        public static TimeSpan StatGainDelay
        {
            get { return m_StatGainDelay; }
            set { m_StatGainDelay = value; }
        }

        protected virtual void GainStat(Stat stat)
        {
            if ((LastStatGain + m_StatGainDelay) >= DateTime.UtcNow)
                return;

            LastStatGain = DateTime.UtcNow;

            bool atrophy = false;//( (from.RawStatTotal / (double)from.StatCap) >= Utility.RandomDouble() );

            IncreaseStat(stat, atrophy);
        }

        #endregion
        #region Hair
        //SMD: support hook for new runuo-2.0 base networking
        [CommandProperty(AccessLevel.GameMaster)]
        public int HairItemID
        {
            get
            {
                Item hair;
                if ((hair = FindItemOnLayer(Layer.Hair)) != null)
                {
                    return hair.ItemID;
                }

                return 0;
            }
            set
            {
                Item hair = FindItemOnLayer(Layer.Hair);

                if (hair == null && value > 0)
                {   // add new hair
                    hair = new Item(value);
                    hair.Layer = Layer.Hair;
                    hair.Movable = false;
                    AddItem(hair);
                }
                else if (value <= 0 && hair != null)
                {   // delete hair
                    hair.Delete();
                    hair = null;
                }
                else
                    // update hair
                    if (hair != null)
                        hair.ItemID = value;

                Delta(MobileDelta.Hair);
            }
        }

        //SMD: support hook for new runuo-2.0 base networking
        [CommandProperty(AccessLevel.GameMaster)]
        public int FacialHairItemID
        {
            get
            {
                Item hair;

                if ((hair = FindItemOnLayer(Layer.FacialHair)) != null)
                {
                    return hair.ItemID;
                }

                return 0;
            }
            set
            {
                Item hair = FindItemOnLayer(Layer.FacialHair);

                if (hair == null && value > 0)
                {   // add new facial hair
                    hair = new Item(value);
                    hair.Layer = Layer.FacialHair;
                    hair.Movable = false;
                    AddItem(hair);
                }
                else if (value <= 0 && hair != null)
                {   // remove facial hair
                    hair.Delete();
                    hair = null;
                }
                else
                    // update facial hair
                    if (hair != null)
                        hair.ItemID = value;

                Delta(MobileDelta.FacialHair);
            }
        }

        //SMD: support hook for new runuo-2.0 base networking
        [CommandProperty(AccessLevel.GameMaster)]
        public int HairHue
        {
            get
            {
                Item hair;

                if ((hair = FindItemOnLayer(Layer.Hair)) != null)
                {
                    return hair.Hue;
                }

                return 0;
            }
            set
            {
                Item hair = FindItemOnLayer(Layer.Hair);

                if (hair != null)
                {
                    hair.Hue = value;
                    Delta(MobileDelta.Hair);
                }
            }
        }

        //SMD: support hook for new runuo-2.0 base networking
        [CommandProperty(AccessLevel.GameMaster)]
        public int FacialHairHue
        {
            get
            {
                Item hair;

                if ((hair = FindItemOnLayer(Layer.FacialHair)) != null)
                {
                    return hair.Hue;
                }

                return 0;
            }
            set
            {
                Item hair = FindItemOnLayer(Layer.FacialHair);

                if (hair != null)
                {
                    hair.Hue = value;
                    Delta(MobileDelta.FacialHair);
                }
            }
        }
    }
    #endregion Hair
    public enum Stat
    {
        Str,
        Dex,
        Int
    }
}