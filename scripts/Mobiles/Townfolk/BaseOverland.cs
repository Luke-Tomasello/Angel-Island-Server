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

/* Scripts/Mobiles/Townfolk/BaseOverland.cs
 * ChangeLog
 *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loop updated.
 *	4/14/08, Adam
 *		Add WipeOverlandMobs so that we can cleanup all the ones clogging the Town Crier messages on Test Center
 *			(where no one is killing/escorting them)
 *	1/26/07, Adam
 *      Remove RefreshLifespan() override and instead set this.Lifespan
 *	9/10,06, Adam
 *		- In MsgTimer.OnTick()
 *		RunUO timers can be queued such that they will OnTick() after they are stopped.
 *			because this code will effectively restart the timer, we need to block re-entry
 *			once the owning mobile gets deleted.
 *		- Generalize DescribeLocation() for public use
 *	9/6/06, Adam
 *		In NearbyMobile(), use "my" RangeHome for the mobile scan
 *  07/02/06, Kit
 *		InitBody/OutFit overrides, is now a base mobile function
 *	05/30/06, Adam
 *		- Filter 'this' out of the NearbyMobile() search.
 *		- add 'some rocks' to the text string to make it make sense
 *	02/11/06, Adam
 *		1. Make common the formatting of sextant coords.
 *		2. Add a call to NearbyMobile() to locate a nearby mobile to help describe my location
 *			if I'm in a cave/dungeon (and my sextant fails because we are not on the felucca map)
 *	1/25/06, Adam
 *		We need to check for alive in UnderAttack() because the RunUO implementation
 *		allows both Spell and Weapon damage to come AFTER death :\
 *		This was causing our message timer to restart and start spamming the Town Crier
 *	1/24/06, Adam
 *		1. override RefreshLifespan() to provide an average 4 hour lifespan
 *		2. Add MsgTimer() to automatically the town crier messages
 *	1/21/06, Adam
 *		kick off the town crier if we are turning on messages (Announce = true) from the [props
 *	1/18/06, Adam
 *		the default implementation of OverlandSystemMessage() now records the last message sent.
 *			derived classes should always call base.OverlandSystemMessage() when they are done processing.
 *	1/15/06, Adam
 *		Remove purposely imposed errors in DescribeLocation()
 *	1/13/06, Adam
 *		Promote to a true base class
 */

using Server.Diagnostics;			// log helper
using Server.Items;
using System;
using System.Collections;

namespace Server.Mobiles
{
    public class BaseOverland : BaseCreature
    {

        #region COMMANDS
        public new static void Initialize()
        {
            Server.CommandSystem.Register("WipeOverlandMobs", AccessLevel.Administrator, new CommandEventHandler(WipeOverlandMobs_OnCommand));
        }
        [Usage("WipeOverlandMobs")]
        [Description("Wipe all Overland Mobiles.")]
        public static void WipeOverlandMobs_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            try
            {
                from.SendMessage("Deleting all overland mobiles ... ");
                Utility.TimeCheck tc = new Utility.TimeCheck();
                tc.Start();
                int count = WipeOverlandMobs();
                tc.End();
                from.SendMessage("{0} mobiles deleted in:{1}", count, tc.TimeTaken);
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                System.Console.WriteLine("Exception Caught in WipeOverlandMobs: " + ex.Message);
                System.Console.WriteLine(ex.StackTrace);
            }

            return;
        }
        private static int WipeOverlandMobs()
        {
            int iDeleted = 0;

            ArrayList toDelete = new ArrayList();
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (m is BaseOverland)
                {
                    toDelete.Add(m);
                }
            }

            for (int i = 0; i < toDelete.Count; i++)
            {
                BaseOverland bo = toDelete[i] as BaseOverland;
                if (bo != null)
                {
                    iDeleted++;
                    bo.Delete();
                }
            }

            return iDeleted;
        }
        #endregion COMMANDS

        public enum MsgState
        {
            None,
            InitialMsg,
            UnderAttackMsg,
            OnDeathMsg
        }

        private Timer m_Timer = null;
        private bool m_Announce = false;
        private MsgState m_LastMsg = MsgState.None;
        private DateTime m_MsgTime = DateTime.MinValue;

        [CommandProperty(AccessLevel.Administrator)]
        public bool Announce
        {
            get { return m_Announce; }
            set
            {   // make sure we are never called from the internal map
                if (value == true)
                    LogicError(this.Map == Map.Internal, "this.Map == Map.Internal");

                m_Announce = value;

                // kick off the town crier if we are turning on messages
                if (m_Announce == true)
                    OverlandSystemMessage((m_LastMsg == MsgState.None) ? MsgState.InitialMsg : LastMsg);
                else
                    RemoveTCEntry();
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public MsgState LastMsg
        {
            get { return m_LastMsg; }
            set { m_LastMsg = value; }
        }

        // town crier entry for telling the world about us
        private TownCrierEntry m_TownCrierMessage;

        private void RemoveTCEntry()
        {
            if (m_TownCrierMessage != null)
            {
                GlobalTownCrierEntryList.Instance.RemoveEntry(m_TownCrierMessage);
                m_TownCrierMessage = null;
            }

            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        // if this message is the same as the last message, and the timer is running
        //	ignore this redundant queue request.
        public bool RedundantTCEntry(MsgState message)
        {
            if (m_Timer == null) return false;  // if no timer, not redundant
            if (message != LastMsg) return false;   // if different msg, not redundant
            bool running = m_Timer.Running;         // if running, redundant
            if (running) DebugSay(DebugFlags.AI, "Redundant message detected");
            return running;                         // if redundant, ignore it
        }

        public void AddTCEntry(string[] lines, int duration)
        {
            if (lines[0].Length > 0)
            {
                // first, clear any existing message and timers
                RemoveTCEntry();

                // Setup the Town Crier
                m_TownCrierMessage = new TownCrierEntry(lines, TimeSpan.FromMinutes(duration), Serial.MinusOne);
                GlobalTownCrierEntryList.Instance.AddEntry(m_TownCrierMessage);

                // Setup our timer
                LogicError(m_Timer != null, "m_Timer != null");
                if (m_Timer != null)
                {
                    m_Timer.Stop();
                    m_Timer = null;
                }

                m_MsgTime = DateTime.UtcNow + TimeSpan.FromMinutes(duration);
                m_Timer = new MsgTimer(this, m_MsgTime);
                m_Timer.Start();

            }
        }

        public override void OnAfterDelete()
        {
            // delete the current messge unless it is a 'we died' message
            if (this.LastMsg != MsgState.OnDeathMsg)
                RemoveTCEntry();

            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }

            base.OnAfterDelete();
        }

        private class MsgTimer : Timer
        {
            private BaseOverland m_mobile;

            public MsgTimer(BaseOverland m, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_mobile = m;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {   // RunUO timers can be queued suchj that they will OnTick() after they are stopped.
                //	because this code will effectively restart the timer, we need to block re-entry
                //	once the owning mobile gets deleted.
                if (m_mobile == null || m_mobile.Deleted == true)
                    return;

                m_mobile.OverlandSystemMessage(MsgState.None);      // clear message state
                m_mobile.OverlandSystemMessage(MsgState.InitialMsg);// restart messages
                m_mobile.DebugSay(DebugFlags.AI, "OnTick: Restarting messages");
            }
        }

        public bool OverlandSystemMessage()
        {
            return OverlandSystemMessage(MsgState.InitialMsg, null);
        }

        public bool OverlandSystemMessage(MsgState state)
        {
            return OverlandSystemMessage(state, null);
        }

        public virtual bool OverlandSystemMessage(MsgState state, Mobile mob)
        {
            m_LastMsg = state;
            return Announce;
        }

        [Constructable]
        public BaseOverland()
            : base(AIType.AI_Melee, FightMode.Aggressor, DefaultRangePerception, 1, 0.2, 0.4)
        {
            InitBody();
            InitOutfit();

            // short lived because we want them to expire about the time the next one spawns
            const int MinHours = 3; const int MaxHours = 5;
            base.Lifespan = TimeSpan.FromMinutes(Utility.RandomMinMax(MinHours * 60, MaxHours * 60));
        }

        public BaseOverland(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }
        }

        public int GetRandomHue()
        {
            switch (Utility.Random(6))
            {
                default:
                case 0: return 0;
                case 1: return Utility.RandomBlueHue();
                case 2: return Utility.RandomGreenHue();
                case 3: return Utility.RandomRedHue();
                case 4: return Utility.RandomYellowHue();
                case 5: return Utility.RandomNeutralHue();
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            int hairHue = Utility.RandomHairHue();
            int cloakHue = GetRandomHue();

            if (Female)
                AddItem(new FancyDress(GetRandomHue()));
            else
                AddItem(new FancyShirt(GetRandomHue()));

            int lowHue = GetRandomHue();

            AddItem(new ShortPants(lowHue));

            if (Female)
                AddItem(new ThighBoots(lowHue));
            else
                AddItem(new Boots(lowHue));

            if (!Female)
                AddItem(new Mustache(hairHue));

            // they are color coordinated :P
            AddItem(new Cloak(cloakHue));
            AddItem(new FeatheredHat(cloakHue));

            AddItem(new BlackStaff());

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(hairHue)); break;
                case 1: AddItem(new LongHair(hairHue)); break;
                case 2: AddItem(new ReceedingHair(hairHue)); break;
                case 3: AddItem(new PonyTail(hairHue)); break;
            }
        }

        public PlayerMobile GetPlayerMobile(Mobile mob)
        {
            if (mob == null) return null;
            if (mob is PlayerMobile) return mob as PlayerMobile;
            if (mob is BaseCreature)
            {
                BaseCreature bc = mob as BaseCreature;
                if (bc.ControlMaster == null) return null;
                return bc.ControlMaster as PlayerMobile;
            }
            else
                // well then wtf is it?
                return null;
        }

        public PlayerMobile Villain(Mobile mob)
        {
            return GetPlayerMobile(mob);
        }

        new public PlayerMobile Hero(Mobile mob)
        {
            return GetPlayerMobile(mob);
        }

        private void UnderAttack(Mobile attacker)
        {
            // tell the world!
            //	We need to check for alive here because the RunUO implementation
            //	allows both Spell and Weapon damage to come AFTER death :\
            if (this.Alive == true && this.Deleted == false)
                OverlandSystemMessage(MsgState.UnderAttackMsg, attacker);
        }

        public override void OnDamagedBySpell(Mobile attacker)
        {
            base.OnDamagedBySpell(attacker);
            UnderAttack(attacker);
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);
            UnderAttack(attacker);
        }

        public override void OnDeath(Container c)
        {
            // add the on death message
            OverlandSystemMessage(MsgState.OnDeathMsg);
            base.OnDeath(c);
        }

        public virtual void OnEscortComplete()
        {   // cleanup any outstanding town crier messages 
            //	this comes into play when an escort is completed 
            //	and before it deletes itself
            RemoveTCEntry();
        }

        public string RelativeLocation()
        {   // need a nice relative description here
            return "outside of Britain";
        }

        #region DescribeLocation
        public static Region BestRegion(Point3D px, Map map)
        {
            ArrayList all = Region.FindAll(px, map);
            if (all is not null)
                foreach (Region rx in all)
                {
                    if (rx is not null)
                    {
                        if (string.IsNullOrEmpty(rx.Name))
                            continue;
                        else if (rx == map.DefaultRegion)
                            continue;
                        else if (rx.Name.Contains("DynRegion"))
                            continue;
                        else
                            return rx;
                    }
                }
            return null;
        }
        public static string DescribeLocation(object o)
        {

            Point3D oLocation;
            Region oRegion;
            Map oMap;
            if (o is Item)
            {
                oLocation = (o as Item).Location;
                oRegion = BestRegion((o as Item).Location, (o as Item).Map); //Region.Find((o as Item).Location, (o as Item).Map);
                oMap = (o as Item).Map;
            }
            else if (o is Mobile)
            {
                oLocation = (o as Mobile).Location;
                oRegion = BestRegion((o as Mobile).Location, (o as Mobile).Map); //(o as Mobile).Region;
                oMap = (o as Mobile).Map;
            }
            else
                return "Error!";

            string location = "";
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            Point3D p = new Point3D(oLocation);

            bool valid = Sextant.Format(p, oMap, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);

            if (!valid)
            {
                string mobName = NearbyMobile(oMap, oLocation);
                if (mobName != null)
                    location = string.Format("{0} in a dark cave.", mobName);
                else // heh, good luck finding this guy!
                    location = string.Format("{0} in a dark cave", "some rocks");
            }

            if (oMap != null)
            {
                if (oRegion != null && oRegion != oMap.DefaultRegion && oRegion.ToString() != "")
                {
                    location += (" in " + oRegion);
                }
            }

            location += ".";

            return location;
        }

        private static string NearbyMobile(Map map, Point3D location)
        {
            string lastResort = null;

            // use "my" RangeHome which is set to be the same as the homerange of the spawner
            //	where we were spawned. This should give us a better chance in finding a nearby mob
            IPooledEnumerable eable = map.GetMobilesInRange(location, 12);
            foreach (Mobile m in eable)
            {
                // ignore staff
                if (m.AccessLevel > AccessLevel.Player)
                    continue;

                // prefer a creature by returning it now
                if (m is BaseCreature)
                {
                    eable.Free();
                    return m.Name;
                }

                // last resort may be a player
                lastResort = m.Name;
            }
            eable.Free();

            // maybe a player
            return lastResort;
        }
        #endregion

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 3;
            writer.Write(version);              // version
            writer.WriteDeltaTime(m_MsgTime);   // version 3
            writer.Write((int)m_LastMsg);           // version 2
            writer.Write(m_Announce);               // version 1 - Are we being announced?

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_MsgTime = reader.ReadDeltaTime();
                    goto case 2;

                case 2:
                    m_LastMsg = (MsgState)reader.ReadInt();
                    goto case 1;

                case 1:
                    m_Announce = reader.ReadBool();
                    goto case 0;

                case 0:
                    break;
            }

            // kick off the message timer if we're announcing
            if (version >= 3)
            {
                if (m_Announce == true)
                {
                    m_Timer = new MsgTimer(this, m_MsgTime);
                    m_Timer.Start();
                }
            }

        }

        // timer callback to kickstart the town crier
        private void tcKickstart()
        {
            OverlandSystemMessage();
        }

        public virtual bool GateTravel { get { return true; } }

        // timer callback to complain about moongate travel
        public virtual void tcMoongate()
        {
            this.Say("I'm sorry, but magic scares me and I do not wish to travel this way.");
        }

        // BaseCreature OnMagicTravel() call this for gate travel:
        // OnMagicTravel() override calls tcMoongate() on a delayed callabck
        //	this is so the player escorting will see the message
        //	when they return through the gate to find their NPC
        public override TeleportResult OnMagicTravel()
        {
            if (GateTravel == false)
            {
                // complain about moongate travel
                Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(tcMoongate));
                return TeleportResult.AnyRejected;
            }

            return base.OnMagicTravel();
        }
        private void LogicError(bool error)
        {
            LogicError(error, "logic error");
        }

        private void LogicError(bool error, string text)
        {
            if (error)
            {
                Console.WriteLine("Logic Error in OverlandSystem: {0}", text);
                Console.WriteLine(new System.Diagnostics.StackTrace());
            }
        }
    }
}