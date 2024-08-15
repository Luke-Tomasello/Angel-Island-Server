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

/* Server\Mobiles\MobileExtensions.cs
 * ChangeLog
 *  8/24/2023, Adam (BaseMulti to which we belong)
 *      Items and Mobiles now may contain a 'BaseMulti to which we belong'
 *      Currently, this is used for Camps.
 *      When a mobile is killed, or an item is stolen, the BaseMulti is informed.
 *      In the case of Camps, this puts the multi in a 'corrupted' state. (decay.)
 *  7/18/2023, Adam (RareAcquisitionLog/ValidateLocation)
 *      Call ValidateLocation to determine whether we should log or not.
 *  4/18/23, Adam (First time check in)
 *      This module will be housing all the extensions we have been adding to Mobile base class
 *      but tend to pollute that class. All of the Investigate AI hooks are as well as the new DebugSay() 
 *      functionally.
 */
using Server.Commands;
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server
{
    #region Debug
    [Flags]
    public enum DebugFlags : UInt32
    {
        None = 0x0,         // no debug output
        Speed = 0x01,       // debug output related to creature speed
        AI = 0x02,          // general AI - "I am wandering", etc.
        Mobile = 0x04,      // not the AI per se, but rather higher level mobile actions
        Movement = 0x08,    // low-level movement debugging
        See = 0x10,         // debug output for when a mobile 'sees' or is 'aware of' a player
        Combatant = 0x20,   // combat timer restart, etc.
        Aggression = 0x40,  // track a mobiles aggressors and aggressed, and damage dealt
        Barding = 0x80,     // display information regarding peace/provo/discord
        Kiting = 0x100,     // display the information regarding kiting mitigation
        Loyalty = 0x200,    // pet loyalty
        Pursuit = 0x400,    // Am I chasing, or pausing to cast a spell?
        Delay = 0x800,      // Do I get a bonus reduction in delay because I'm wild and chasing, or tame and following?
        Player = 0x1000,    // player character messages
        NavStar = 0x2000,   // track now the mobile is navigating
        Echo = 0x4000,   // echo output to console window
        Homing = 0x8000,    // proactive homing AI
        All = 0xFFFFFFFF    // all of it baby
    }
    #endregion Debug

    /// <summary>
    /// Extension features supporting the base class Mobile.
    /// </summary>
    public partial class Mobile : IEntity, IPoint3D, IHued
    {
        #region Notify
        public virtual void Notify(Notification notification, params object[] args)
        {   // resolve Clilocs here since when we use the Cliloc, we get new 'flat' ugly text, when we use ascii, we get the oldschool 'Gothic' text
            string name = string.Empty;
            bool hasArgs = args.Length > 1;
            bool isCliloc = hasArgs ? args[1] is int : false;
            bool isString = hasArgs ? args[1] is string : false;
            if (isCliloc)
            {
                if (Server.Text.Cliloc.Lookup.ContainsKey((int)args[1]))
                {
                    name = Server.Text.Cliloc.Lookup[(int)args[1]];
                    isString = true;
                }
            }
            else if (isString)
                name = args[1] as string;

            switch (notification)
            {
                case Notification.WeaponStatus:
                    WeaponStatus(args[0] as BaseWeapon, isString ? name : isCliloc ? (int)args[1] : args[1] as object);
                    break;
                case Notification.ArmorStatus:
                    ArmorStatus(args[0] as BaseArmor, isString ? name : isCliloc ? (int)args[1] : args[1] as object);
                    break;
                case Notification.ClothingStatus:
                    ClothingStatus(args[0] as BaseClothing, isString ? name : isCliloc ? (int)args[1] : args[1] as object);
                    break;
                case Notification.Destroyed:
                    Destroyed(args[0] as Item);
                    break;
                case Notification.AmmoStatus:
                    AmmoStatus(args[0] as Item);
                    break;
                default:
                    return;
            }
        }
        public virtual void WeaponStatus(BaseWeapon weapon, object o)
        {
        }
        public virtual void ArmorStatus(BaseArmor armor, object o)
        {
        }
        public virtual void ClothingStatus(BaseClothing armor, object o)
        {
        }
        public virtual void Destroyed(Item item)
        {
        }
        public virtual void AmmoStatus(Item ammo)
        {
        }
        #endregion Notify
        #region Mobile Properties
        public virtual bool Incurable { get { return GetMobileBool(MobileBoolTable.Incurable); } }
        public virtual bool CanLore { get { return false; } }
        private class EscalateCommand : BaseCommand
        {
            public EscalateCommand()
            {
                AccessLevel = AccessLevel.Owner;
                Supports = CommandSupport.AllMobiles;
                Commands = new string[] { "Escalate" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "Escalate";
                Description = "Escalate access level to System.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is PlayerMobile pm && pm.AccessLevel == AccessLevel.Owner)
                {
                    pm.AccessLevelInternal = AccessLevel.System;
                    Accounting.Account acct = pm.Account as Accounting.Account;
                    if (acct != null)
                        acct.AccessLevelInternal = AccessLevel.System;
                }
                else if (obj is not PlayerMobile)
                    LogFailure("That is not a player.");
                else
                    LogFailure("That player cannot be escalated.");
            }
        }
        private class DeescalateCommand : BaseCommand
        {
            public DeescalateCommand()
            {
                AccessLevel = AccessLevel.System;
                Supports = CommandSupport.AllMobiles;
                Commands = new string[] { "Deescalate" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "Deescalate";
                Description = "Deescalate access level to Owner.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is PlayerMobile pm && pm.AccessLevel == AccessLevel.System)
                {
                    pm.AccessLevelInternal = AccessLevel.Owner;
                    Accounting.Account acct = pm.Account as Accounting.Account;
                    if (acct != null)
                        acct.AccessLevelInternal = AccessLevel.Owner;
                }
                else if (obj is not PlayerMobile)
                    LogFailure("That is not a player.");
                else
                    LogFailure("That player cannot be deescalated.");
            }
        }
        #endregion Mobile Properties
        #region Pet Cache
        private static List<Mobile> m_CompositePetList = new List<Mobile>();
        public static List<Mobile> PetCache
        {
            get
            {
                List<Dictionary<Mobile, List<BaseCreature>>> table_table = new List<Dictionary<Mobile, List<BaseCreature>>>()
                {
                    StableMaster.Table,
                    ElfStabler.Table,
                    //ChickenCoopAddon.Table,
                    InnKeeper.Table,
                    AnimalTrainer.Table,
                };

                // build our composite list
                foreach (var master_table in table_table)
                    foreach (var kvp in master_table)
                        foreach (var mob in kvp.Value)
                            if (!m_CompositePetList.Contains(mob))
                                m_CompositePetList.Add(mob);

                // chicken coops are funky. They're instances, i.e., no global repository,
                // so we need to query each one individually.
                foreach (var coop in ChickenCoopAddon.CoopRegistry)
                    if (coop.Stabled != null)
                        foreach (var mob in coop.Stabled)
                            if (!m_CompositePetList.Contains(mob))
                                m_CompositePetList.Add(mob);

                // now add in the livestock
                foreach (var tss in TownshipStone.AllTownshipStones)
                    if (tss.Livestock != null)
                        foreach (var mob in tss.Livestock.Keys)
                            if (!m_CompositePetList.Contains(mob))
                                m_CompositePetList.Add(mob);

                // since we also add followers (not in a stable), we need to filter:
                //  1. Distinct
                //  2. not deleted
                m_CompositePetList = m_CompositePetList.Distinct().Where(m => m.Deleted == false).ToList();

                return m_CompositePetList;
            }
        }

        public List<Mobile> Followers
        {
            get
            {
                return Mobile.PetCache.Where(o => o is BaseCreature bc && bc.ControlMaster == this && !bc.IsAnyStabled && !bc.Deleted).ToList();
            }
        }
        public List<Mobile> Pets
        {
            get
            {
                // note the use of ControlMasterGUID. This is because the ControlMaster is null when stabled
                return Mobile.PetCache.Where(o => o is BaseCreature bc && bc.ControlMasterGUID == this.GUID && !bc.Deleted).ToList();
            }
        }
        public List<Mobile> Stabled
        {
            get
            {
                // note the use of ControlMasterGUID. This is because the ControlMaster is null when stabled
                return Mobile.PetCache.Where(o => o is BaseCreature bc && bc.IsAnyStabled && bc.ControlMasterGUID == this.GUID && !bc.Deleted).ToList();
            }
        }
        private class FindAllPetsCommand : BaseCommand
        {
            public FindAllPetsCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllNPCs;
                Commands = new string[] { "FindAllPets" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "FindAllPets";
                Description = "Find all pets associated with this mobile.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                string filename = "FindAllPets.log";
                Utility.SimpleLog(filename, null, overwrite: true);
                if (obj is Mobile target)
                {
                    if (e.Mobile is PlayerMobile staff)
                    {
                        staff.JumpIndex = 0;
                        staff.JumpList = new System.Collections.ArrayList();
                        Utility.SimpleLog(filename, sendMessage: staff, string.Format("---"));
                        foreach (Mobile m in target.Pets)
                            if (m is BaseCreature pet && !pet.Deleted && pet.ControlMasterGUID == target.GUID)
                            {
                                staff.JumpList.Add(pet);

                                if (pet.IsAnyStabled)
                                    Utility.SimpleLog(filename, sendMessage: staff, string.Format("{0}: Stable: {1}", pet, pet.GetStableMasterName));
                                else if (pet is BaseMount bm && bm.Rider == target)
                                    Utility.SimpleLog(filename, sendMessage: staff, string.Format("{0}: Location:{1} Map:{2} (mounted)", pet, pet.Location, pet.Map));
                                else
                                    Utility.SimpleLog(filename, sendMessage: staff, string.Format("{0}: Location:{1} Map:{2}", pet, pet.Location, pet.Map));

                                if (pet.StableConfigError)
                                    Utility.SimpleLog(filename, sendMessage: staff, string.Format("** Error** {0} in too many stables or otherwise misconfigured", pet));
                            }

                        if (staff.JumpList.Count > 0)
                            staff.SendMessage("Your jumplist has been loaded with {0} pets", staff.JumpList.Count);
                        else
                            staff.SendMessage("There are no pets associated with this mobile");
                    }
                }
                else
                    LogFailure("That is not a Mobile.");
            }
        }
        private class FindAllFollowersCommand : BaseCommand
        {
            public FindAllFollowersCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllNPCs;
                Commands = new string[] { "FindAllFollowers" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "FindAllFollowers";
                Description = "Find all Followers associated with this mobile.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                string filename = "FindAllFollowers.log";
                Utility.SimpleLog(filename, null, overwrite: true);
                if (obj is Mobile target)
                {
                    if (e.Mobile is PlayerMobile staff)
                    {
                        staff.JumpIndex = 0;
                        staff.JumpList = new System.Collections.ArrayList();
                        Utility.SimpleLog(filename, sendMessage: staff, string.Format("---"));
                        foreach (Mobile m in target.Followers)
                            if (m is BaseCreature pet && !pet.Deleted && pet.ControlMaster == target)
                            {
                                staff.JumpList.Add(pet);
                                Utility.SimpleLog(filename, sendMessage: staff, string.Format("{0} found at {1} on {2}. Controlled={3}", pet, pet.Location, pet.Map, pet.Controlled));
                            }

                        if (staff.JumpList.Count > 0)
                            staff.SendMessage("Your jumplist has been loaded with {0} followers", staff.JumpList.Count);
                        else
                            staff.SendMessage("There are no followers associated with this mobile");
                    }
                }
                else
                    LogFailure("That is not a Mobile.");
            }
        }
        private class GetAllFollowersCommand : BaseCommand
        {
            public GetAllFollowersCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllNPCs;
                Commands = new string[] { "GetAllFollowers" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "GetAllFollowers <target>";
                Description = "Bring all Followers associated with this mobile here.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                string filename = "GetAllFollowers.log";
                Utility.SimpleLog(filename, null, overwrite: true);
                if (obj is Mobile target)
                {
                    if (e.Mobile is PlayerMobile staff)
                    {
                        staff.JumpIndex = 0;
                        staff.JumpList = new System.Collections.ArrayList();
                        Utility.SimpleLog(filename, sendMessage: staff, string.Format("---"));
                        foreach (Mobile m in target.Followers)
                            if (m is BaseCreature pet && !pet.Deleted && pet.ControlMaster == target)
                            {
                                staff.JumpList.Add(pet);
                                Utility.SimpleLog(filename, sendMessage: staff, string.Format("{0} found at {1} on {2}. Controlled={3}", pet, pet.Location, pet.Map, pet.Controlled));
                            }

                        if (staff.JumpList.Count > 0)
                            staff.SendMessage("Your jumplist has been loaded with {0} followers", staff.JumpList.Count);
                        else
                            staff.SendMessage("There are no followers associated with this mobile");

                        foreach (Mobile m in target.Followers)
                            if (m is BaseCreature bc)
                                bc.MoveToWorld(staff.Location, staff.Map);
                    }
                }
                else
                    LogFailure("That is not a Mobile.");
            }
        }
        private class FindAllStabledCommand : BaseCommand
        {
            public FindAllStabledCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllNPCs;
                Commands = new string[] { "FindAllStabled" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "FindAllStabled";
                Description = "Find all stabled pets associated with this mobile.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                string filename = "FindAllStabled.log";
                Utility.SimpleLog(filename, null, overwrite: true);
                if (obj is Mobile target)
                {
                    if (e.Mobile is PlayerMobile staff)
                    {
                        staff.JumpIndex = 0;
                        staff.JumpList = new System.Collections.ArrayList();
                        Utility.SimpleLog(filename, sendMessage: staff, string.Format("---"));
                        foreach (Mobile m in target.Stabled)
                            if (m is BaseCreature pet && !pet.Deleted && pet.ControlMasterGUID == target.GUID)
                            {
                                staff.JumpList.Add(pet);
                                Utility.SimpleLog(filename, sendMessage: staff, string.Format("{0} found at {1} on {2}. Stabled={3}. Controlled={4}",
                                    pet, pet.Location, pet.Map,
                                    //pet.IsCoopStabled ? "chicken coop" : pet.IsElfStabled ? "elf stabled" : "animal trainer",
                                    pet.GetStableName,
                                    pet.Controlled));
                            }

                        if (staff.JumpList.Count > 0)
                            staff.SendMessage("Your jumplist has been loaded with {0} stabled pets", staff.JumpList.Count);
                        else
                            staff.SendMessage("There are no stabled pets associated with this mobile");
                    }
                }
                else
                    LogFailure("That is not a Mobile.");
            }
        }
        #endregion Pet Cache
        // we need this since we get two lifted packets, so we filter the second one
        private static Memory m_Recieved = new Memory();

        #region BaseMulti to which we belong
        private BaseMulti m_BaseMulti = null;
        public BaseCamp BaseCamp { get { return m_BaseMulti as BaseCamp; } set { m_BaseMulti = value; } }
        public BaseMulti BaseMulti { get { return m_BaseMulti; } set { m_BaseMulti = value; } }
        #endregion BaseMulti to which we belong

        #region Safe Name
        public string SafeName
        {
            get
            {
                return Name != null ? Name : GetType().Name;
            }
        }
        #endregion Safe Name
        #region Rare Acquisition Log
        public void RareAcquisitionLog(Item item, string context = "(none)")
        {
            if (!ValidateLocation(item) || m_Recieved.Recall(item) != null)
                return;

            m_Recieved.Remember(item, 1);
            LogHelper logger = new LogHelper("Rare Acquisition.log", false, true);
            logger.Log(LogType.Mobile, this);
            logger.Log(LogType.Item, item, string.Format("OldSchoolName: '{0}'", item.OldSchoolName() != null ? item.OldSchoolName() : "(unknown)"));
            if (context != "(none)")
                logger.Log(context);
            logger.Finish();
        }
        private static bool ValidateLocation(Item item)
        {
            if (item == null)
                return false;

            Item rootItem = item.RootParent as Item;
            Mobile rootMobile = item.RootParent as Mobile;
            Multis.BaseHouse bh = null;
            if (rootItem != null)
                bh = Multis.BaseHouse.FindHouseAt(rootItem);
            else if (rootMobile != null)
                bh = Multis.BaseHouse.FindHouseAt(rootMobile);

            if (rootMobile != null || bh != null)
                return false;

            return true;
        }
        #endregion Rare Acquisition Log
        #region SEE
        public virtual void OnSee(Mobile from, Item item)
        {

        }
        #endregion SEE
        #region Mobile To AI Signals
        public enum SignalType : UInt32
        {
            None,
            COMBAT_TIMER_TICK,
            COMBAT_TIMER_EXPIRED,
        }
        public virtual void OnSignal(SignalType signal)
        {   // called by the mobile to notify the AI something interesting has happened
            return;
        }
        #endregion Mobile To AI Signals
        #region Combat Timers
        public long NextCombatReset = 0;
        #region UNUSED
#if false
        public Timer GetCombatTimer
        {
            get { return m_CombatTimer; }
        }
        public Timer GetExpireCombatant
        {
            get { return m_ExpireCombatant; }
        }
        /*
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 10000; // Set the interval of the timer to 10 seconds
            timer.Elapsed += OnTimedEvent;
            timer.Enabled = true;

            DateTime targetTime = DateTime.UtcNow.AddMilliseconds(timer.Interval); // Set the target time to the next timer elapse
            TimeSpan remainingTime = targetTime - DateTime.UtcNow; // Get the remaining time by subtracting the current time from the target time
            Console.WriteLine("Time remaining: " + remainingTime.ToString()); // Display the remaining time

            private static void OnTimedEvent(Object source, ElapsedEventArgs e)
            {
                // Handle the timer event
            }
         */
        public TimeSpan GetTimeRemaining(Timer timer)
        {
            TimeSpan remainingTime = TimeSpan.Zero;
            if (timer != null && timer.Running)
            {
                DateTime now = DateTime.UtcNow;
                // Set the target time to the next timer elapse
                DateTime targetTime = now.AddMilliseconds(timer.Interval.TotalMilliseconds);
                // Get the remaining time by subtracting the current time from the target time
                remainingTime = targetTime - now;
            }
            return remainingTime;
        }
#endif
        #endregion UNUSED
        #endregion Combat Timers

        #region TargetLocation
        public long TargetLocationExpire = 0;
        #endregion TargetLocation

        #region Monitors
        private List<Mobile> m_monitors = new List<Mobile>(0);   // staff that want to follow progress (not serialized)
        public List<Mobile> Monitors { get { return m_monitors; } }
        public virtual void UpdateMonitors(object status)
        {
            foreach (Mobile m in Monitors)
            {
                if (m != null && m.NetState != null)
                {
                    if (status is string)
                    {
                        m.SendMessage(status as string);
                    }
                }
            }
        }
        #endregion Monitors

        #region InvestigativeAI
        public virtual bool IAIOkToInvestigate(Mobile playerToInvestigate)
        {   // Where should we go?
            return true;
        }
        public virtual Point3D IAIGetPoint(Mobile playerToInvestigate)
        {   // Where should we go?
            return playerToInvestigate.Location;
        }
        public virtual void IAIResult(Mobile m, bool canPath)
        {   // called by InvestigativeAI when we've found a mobile we're interested in

        }
        public virtual bool IAIQuerySuccess(Mobile m)
        {   // called by InvestigativeAI when wondering if we are done
            return true;
        }
        #endregion  InvestigativeAI

        #region Initialization
        /// <summary>
        /// Called after world load to provide a context-aware initialization
        /// </summary>
        public virtual void WorldLoaded()
        {
            return;
        }
        public virtual void Configure(int current_level, int max_levels)
        {   // allow some post spawn configuration
            //  Spawners can use this as well as the champ engine
            return;
        }
        #endregion Initialization

        #region Acquire Focus Mobile
        public virtual Mobile FocusDecisionAI(Mobile m)
        {   // make any final decisions about attacking this mobile
            // This function is called by BaseAI when we've decided who is an attack candidate.
            // we let let the mobile make the final decision. By default, we just attack the 
            //  mobile we were scheduled to attack.
            //  Currently only used by Champions which have a NavBeacon and an objective.
            return m;
        }
        #endregion Acquire Focus Mobile
        #region Debug Output
        public void SetFlag(DebugFlags flag, bool value)
        {
            if (value)
                m_DebugFlags |= flag;
            else
                m_DebugFlags &= ~flag;
        }

        public bool GetFlag(DebugFlags flag)
        {
            return ((m_DebugFlags & flag) != 0);
        }
        public void FlipFlag(DebugFlags flag)
        {
            SetFlag(flag, GetFlag(flag) ? false : true);
        }
        DebugFlags m_DebugFlags = DebugFlags.None;
        [CommandProperty(AccessLevel.GameMaster)]
        public DebugFlags DebugMode
        {
            get
            {
                return m_DebugFlags;
            }
            set
            {   // turn specific debug output
                m_DebugFlags = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Debug
        {
            get
            {
                return m_DebugFlags != DebugFlags.None;
            }
            set
            {
                if (value == true)
                    // turn all debug output
                    m_DebugFlags = DebugFlags.All;
                else
                    m_DebugFlags = DebugFlags.None;
            }
        }

        #region Anti-spam AI Output

        private static Memory ConsoleDebugMemory = new Memory();
        private static Memory OverheadDebugMemory = new Memory();
        protected class AntiSpamObject
        {
            public string LastString = null;
            public AntiSpamObject(string text)
            {
                LastString = text;
            }
        }
        public bool CheckString(string text)
        {
            return CheckString(memory: OverheadDebugMemory, text);
        }
        private bool CheckString(Memory memory, string text)
        {
            Memory.ObjectMemory om = memory.Recall(this as object);
            if (om == null)
            {   // if the time has elapsed, print it
                memory.Remember(this, new AntiSpamObject(text), 5/*seconds*/);
                return true;
            }
            else if (om.Context is AntiSpamObject aso)
            {   // if the strings are different, print it
                if (text != aso.LastString)
                {
                    memory.Forget(this);
                    memory.Remember(this, new AntiSpamObject(text), 5/*seconds*/);
                    return true;
                }
            }
            return false;
        }
        // Adam: Smart debug output. (anti spam implementation)
        //	Don't print the string unless it changes, or 5 seconds has passed.
        public void DebugOut(string text)
        {
            if (CheckString(OverheadDebugMemory, text))
            {
                if (this is BaseCreature)
                    this.NonlocalStaffOverheadMessage(MessageType.Regular, this.SpeechHue, false, text);
                else
                    this.SendMessage(text);

                UpdateMonitors(text);
            }
        }

        public void DebugSay(string text)
        {
            if (DebugMode != DebugFlags.None)
                DebugOut(text);
        }

        public void DebugSay(string format, params object[] args)
        {
            if (DebugMode != DebugFlags.None)
                DebugSay(string.Format(format, args));
        }

        public void DebugSay(DebugFlags flags, string text)
        {
            if ((m_DebugFlags & flags) != 0)
                DebugOut(text);

            text = string.Format("{0}:{1}", this, text);
            if ((m_DebugFlags & flags) != 0 && (m_DebugFlags & DebugFlags.Echo) != 0)
                if (CheckString(ConsoleDebugMemory, text))
                    Utility.ConsoleWriteLine(text, ConsoleColor.Green);
        }

        public void DebugSay(DebugFlags flags, string format, params object[] args)
        {
            if ((m_DebugFlags & flags) != 0)
                DebugSay(flags, string.Format(format, args));

            string text = string.Format("{0}:{1}", this, string.Format(format, args));
            if ((m_DebugFlags & flags) != 0 && (m_DebugFlags & DebugFlags.Echo) != 0)
                if (CheckString(ConsoleDebugMemory, text))
                    Utility.ConsoleWriteLine(text, ConsoleColor.Green);
        }

        #endregion Anti-spam AI Output
        #endregion Debug Output
    }
    public class TargetLocationMemory
    {
        public Point2D Location;
        public DateTime Timeout;
        public TargetLocationMemory(Point2D px)
        {
            Location = px;
            Timeout = DateTime.UtcNow + TimeSpan.FromSeconds(60);
        }

        public override int GetHashCode()
        {
            return Location.ToString().GetHashCode();
        }
    }
}