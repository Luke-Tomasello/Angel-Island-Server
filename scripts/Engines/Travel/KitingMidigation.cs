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

/* scripts\Engines\Travel\KitingMidigation.cs
 * CHANGELOG:
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Items
{
    public partial class Teleporter : Item
    {
        private List<Teleporter> TeleporterFamily = new(); // our teleporter family
        const int MemoryTime = 60;                          // how long (seconds) we remember this player
        private Memory m_PlayerMemory = new Memory();       // memory used to remember if we a saw a player in the area
        private const int KitingThreshold = 4;              // in and out 4 times, and this guy is Kiting
        public override void WorldLoaded()
        {
            base.WorldLoaded();

            // not handling gates at this time
            if (IsGate)
                return;

            #region build our 'family' of teleporters
            IPooledEnumerable eable = this.Map.GetItemsInRange(this.Location, 3);
            foreach (Item item in eable)
            {
                if (item is null || item.Map is null || item.Map == Map.Internal)
                    continue;
                if (item is not Teleporter)
                    continue;
                Teleporter teleporter = item as Teleporter;
                if (Utility.IsDungeon(teleporter.Location) || Utility.IsDungeon(teleporter.PointDest))
                    TeleporterFamily.Add(item as Teleporter);
            }
            eable.Free();
            #endregion build our 'family' of teleporters
        }
        private void KM_OnAfterTeleport(Mobile m)
        {
            // just creatures
            if (m is BaseCreature bc)
                if (bc.PreferredFocus != null && bc.CanSee(bc.PreferredFocus))
                {
                    TimeSpan ts = TimeSpan.FromMinutes(2);
                    Timer.DelayCall(ts, new TimerStateCallback(KM_Reset), new object[] { bc, bc.PreferredFocus, 0 });
                    m.DebugSay(DebugFlags.Kiting, "I will head home in {0} minutes",
                        string.Format("{0:.##}", ts.TotalMinutes));
                }
        }
        private void KM_OnDoTeleport(Mobile m)
        {
            // just players
            if (!m.Player)
                return;

            // let our family of teleporters know about this player
            KM_NotifyRemember(m);

            // Check to see if this player has reached the Kiting Threshold
            if (KM_KitingThresholdReached(m))
            {
                // take some action
                if (KM_IsKiting(m))
                {
                    BaseCreature bc = KM_ClosestAdversiary(m);
                    if (bc is not null)
                    {
                        // step 1: now we're really pissed at you!
                        bc.PreferredFocus = m;
                        // step 2: grant a one time pass to use the teleporter
                        bc.SetCreatureBool(CreatureBoolTable.AntiKiting, true);
                        // step 3: set the destination to the teleporter to use
                        bc.TargetLocation = new Point2D(this.Location.X, this.Location.Y);
                        // In a couple minutes, go back to teleporter, and forget this guy
                        //Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(KM_Reset), new object[] { bc, m });
                        // finally: refresh our family with double the usual memory. This gives the mob
                        //  time to make his way to the teleporter and get after the player.
                        KM_NotifyRefresh(m);
                        bc.DebugSay(DebugFlags.Kiting, "I will target {0}", bc.PreferredFocus.Name == null ? bc.PreferredFocus : bc.PreferredFocus.Name);
                    }
                }
            }
        }
        public void KM_Refresh(Mobile m, double seconds)
        {
            if (m_PlayerMemory.Recall(m) == false)
            {   // we haven't seen this player yet - should not happen
                ; // debug break
            }
            else
            {   // refresh our recollection and increase memory time
                Memory.ObjectMemory om = m_PlayerMemory.Recall(m as object);
                m_PlayerMemory.Refresh(m, om.Context, seconds);
            }
        }
        public void KM_NotifyRefresh(Mobile m)
        {
            foreach (Teleporter teleporter in TeleporterFamily)
            {
                if (teleporter == null || teleporter.Deleted)
                    continue;

                // double the time to remember this guy.
                //  Note: this extended memory only lasts until the player causes a normal refresh by stepping 
                //  on the tele again. This extended memory is really just to give the NPC time to get to the tele
                //  and cross over.
                teleporter.KM_Refresh(m, TimeSpan.FromSeconds(MemoryTime * 2).TotalSeconds);
            }
        }
        private BaseCreature KM_ClosestAdversiary(Mobile m)
        {
            Mobile closest = null;
            uint distance = uint.MaxValue;
            PlayerMobile pm = m as PlayerMobile;
            // find the closest mobile we were fighting
            if (pm.Aggressors != null && pm.Aggressors.Count > 0)
                foreach (var info in pm.Aggressors)
                {
                    if (!info.Attacker.Player)
                    {
                        uint temp = (uint)this.GetDistanceToSqrt(info.Attacker);
                        if (temp < distance && temp < 20)
                        {
                            distance = temp;
                            closest = info.Attacker;
                        }
                    }
                    if (!info.Defender.Player)
                    {
                        uint temp = (uint)this.GetDistanceToSqrt(info.Defender);
                        if (temp < distance && temp < 20)
                        {
                            distance = temp;
                            closest = info.Defender;
                        }
                    }
                }

            if (pm.Aggressed != null && pm.Aggressed.Count > 0)
                foreach (var info in pm.Aggressed)
                {
                    if (!info.Attacker.Player)
                    {
                        uint temp = (uint)this.GetDistanceToSqrt(info.Attacker);
                        if (temp < distance && temp < 20)
                        {
                            distance = temp;
                            closest = info.Attacker;
                        }
                    }
                    if (!info.Defender.Player)
                    {
                        uint temp = (uint)this.GetDistanceToSqrt(info.Defender);
                        if (temp < distance && temp < 20)
                        {
                            distance = temp;
                            closest = info.Defender;
                        }
                    }
                }

            return closest as BaseCreature;
        }
        private bool KM_IsKiting(Mobile m)
        {
            PlayerMobile pm = m as PlayerMobile;
            if (pm.Aggressors != null && pm.Aggressors.Count > 0)
                foreach (var info in pm.Aggressors)
                    // if we are involved, and one of us isn't a player
                    if (info.Attacker == m || info.Defender == m && (!info.Attacker.Player || !info.Defender.Player))
                        return true;

            if (pm.Aggressed != null && pm.Aggressed.Count > 0)
                foreach (var info in pm.Aggressed)
                    // if we are involved, and one of us isn't a player
                    if (info.Attacker == m || info.Defender == m && (!info.Attacker.Player || !info.Defender.Player))
                        return true;

            return false;
        }
        public class PlayerContext
        {
            public int Visits = 0;
            public PlayerContext(int visits)
            {
                Visits = visits;
            }
        }
        public void KM_Remember(Mobile m)
        {
            if (m_PlayerMemory.Recall(m) == false)
            {   // we haven't seen this player yet
                m_PlayerMemory.Remember(m, new PlayerContext(1), TimeSpan.FromSeconds(MemoryTime).TotalSeconds);   // remember him for this long
                Utility.DebugOut("KM({1}): We don't remember {0}. Remembering...", RandomConsoleColor(this.Serial), m.Name, this.Serial.Value);
            }
            else
            {   // refresh our recollection and increment count
                Memory.ObjectMemory om = m_PlayerMemory.Recall(m as object);
                PlayerContext pc = om.Context as PlayerContext;
                m_PlayerMemory.Refresh(m, new PlayerContext(pc.Visits + 1));
                Utility.DebugOut("KM({1}): I remember {0}.", RandomConsoleColor(this.Serial), m.Name, this.Serial);
                Utility.DebugOut("KM({1}): {0} has visited {2} time(s).", RandomConsoleColor(this.Serial), m.Name, this.Serial, pc.Visits + 1);
            }
        }
        public void KM_NotifyRemember(Mobile m)
        {
            foreach (Teleporter teleporter in TeleporterFamily)
            {
                if (teleporter == null || teleporter.Deleted)
                    continue;

                teleporter.KM_Remember(m);
            }
        }
        public int KM_GetCount(Mobile m)
        {
            Memory.ObjectMemory om = m_PlayerMemory.Recall(m as object);
            if (om != null)
                return (om.Context as PlayerContext).Visits;
            else
                return 0;
        }
        private bool KM_KitingThresholdReached(Mobile m)
        {
            int count = 0;
            foreach (Teleporter teleporter in TeleporterFamily)
            {
                if (teleporter == null || teleporter.Deleted)
                    continue;

                int tmp = teleporter.KM_GetCount(m);
                if (tmp > count)
                    count = tmp;
            }

            return count > KitingThreshold;
        }
        private void KM_Reset(object state)
        {
            object[] aState = (object[])state;
            if (aState == null || aState.Length != 3)
                return;
            if (aState[0] == null || aState[1] == null)
                return;

            BaseCreature bc = aState[0] as BaseCreature;
            PlayerMobile pm = aState[1] as PlayerMobile;
            int count = (int)aState[2];

            if (bc == null || bc.Deleted || bc.AIObject == null || bc.Spawner == null || pm == null)
                return;

            // learn everything about this player
            MobileInfo info = bc.AIObject.GetMobileInfo(new MobileInfo(pm));

            // they are hidden or dead, but nearby, or, someone else can see me. Don't disappear right in front of them.
            // we will wait until they are gone before returning home or 15 minutes has elapsed
            if (info.hidden || info.range || KM_AnyoneWatching(bc))
            {
                if (bc.GetDistanceToSqrt(bc.Spawner) > bc.RangePerception && count < 30)
                {
                    Timer.DelayCall(TimeSpan.FromSeconds(30), new TimerStateCallback(KM_Reset), new object[] { bc, pm, ++count });
                    if (info.dead)
                        bc.DebugSay(DebugFlags.Kiting, "Good, I'm glad {0}'s dead.", pm.Female ? "she" : "he");
                    bc.DebugSay(DebugFlags.Kiting, "I want to go home, but someone is here: {0}", count);
                    return;
                }
                /* fall through */
            }

            /* okay, everyone's gone. time to clean things up */
            bc.DebugSay(DebugFlags.Kiting, "Okay, I will go home now");

            // step 1: forget them
            if (bc.PreferredFocus != null)
                bc.PreferredFocus = null;
            // step 2: remove a one time pass to use the teleporter
            bc.SetCreatureBool(CreatureBoolTable.AntiKiting, false);
            // step 3: clear the destination
            bc.TargetLocation = Point2D.Zero;
            // return home
            if (bc.Spawner != null)
                bc.Location = bc.Spawner.Location;
            // activate, or he will be in a deactivated state even though there may be other players nearby
            bc.AIObject.Activate();

            bc.DebugSay(DebugFlags.Kiting, "I am home now");
            return;
        }

        private bool KM_AnyoneWatching(BaseCreature bc)
        {
            IPooledEnumerable eable = bc.GetMobilesInRange(bc.RangePerception);
            foreach (Mobile m in eable)
                if (m is PlayerMobile pm)
                    if (pm.CanSee(bc))
                    {
                        eable.Free();
                        return true;
                    }
            eable.Free();
            return false;
        }
    }
}