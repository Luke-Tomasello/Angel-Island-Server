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

/* Scripts\Engines\AI\AI\BaseAI\InvestigativeAI.cs
 * CHANGELOG
 *  5/31/2023, Adam (CheckDifficulty(m)) (disabled)
 *      InvestigativeAI tracking uses skills I may have vs. their hide and stealth 
 *  1/18/22, Adam
 *      Don't even try to open locked doors.
 *          And when opening unlocked doors, make sure the monster and not the player is doing the opening
 *  1/9/22, Adam
 *      First time checkin.
 */

/* Description:
  The InvestigativeAI is a new AI tied into the Wander AI.
    InvestigativeAI is initialized during  AcquireFocusMobAI when there is a mobile we should attack, yet we do not have line-of-sight.
    InvestigativeAI creates a record of the path information to this mobile and saves it (it is not otherwise acted upon during AcquireFocusMobAI.)
    During Wandering AI, we process things like Way Points, and Navigation Points. We now also process the Investigative Memory in SomethingToInvestigate(). (this module.)
    SomethingToInvestigate looks at all the records created in AcquireFocusMobAI. If a record is found, all the usual checks happen (alive, hidden, etc.)
    If the ‘target’ is still there, we simply seed the Wander AI with the first/next direction to move in.
    It’s important to note that we are still not attacking as we still don’t have LOS. 
    This seeding of the Wander AI continues until one of: target no longer available (dead, hidden, moved,) no more path elements, or we acquire LOS.
    If, and only if, we achieve LOS, we enter combat mode.
    Player Experience: To the player, it looks as if the NPC is simply wondering around, which it is – just in a somewhat informed manner.  We can justify this ‘informed manner’ 
    by simply recognizing these creatures are not stupid, and not strictly restricted to the 4 tiles or whatever their spawner is set to. An intelligent creature 
    (like a lich for instance,) is curious, and will wander around his domain. If further RP explanation becomes mandatory, we can limit this behavior by basing it on creature 
    intelligence and explaining that these creatures have senses, like sound, smell, and noticing disturbances.
    Opening doors: By default, Wandering AI doesn’t open doors. Opening doors is part of the Pathing AI, however, The InvestigativeAI is not pathing per se, that is we don’t 
    give the creature a path via the traditional navigation system FastAStar, NavStar, etc. The InvestigativeAI is however designed to open doors (currently) 20% of the time. 
    The reasoning here is wholly believability. If a creature is wandering around, (under InvestigativeAI, and they are a mobile that CanOpenDoors,) it’s only reasonable they 
    wouldn’t always stop and turn around when confronted with a door. The current implementation takes this into account and allows them to open doors every so often.
    Player Experience: To the player it looks as if the NPC is aimlessly wandering around and happened to open a door. The NPC hasn’t yet targeted the Player, so there is 
    no urgency on the part of the NPC. All quite believable. 
    Take away: players shouldn’t notice a big difference unless they are doing something funky like standing right outside the lichlord room, knowing they are safe since 
    the doors are not open (LOS.)
    Another place where this comes into play is where players are simply around a corner, possibly lobbing blade spirits at a dragon that can’t LOS them.
    In the end: NPCs will now wander towards the player until it can attach LOS. Beware sleazy players!
 */

using Server.Items;
using Server.Spells.Sixth;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Utility;
namespace Server.Mobiles
{
    public abstract partial class BaseAI : SerializableObject
    {
        private Memory m_InvestigativeMemory = new Memory();// memory used to remember we a saw a player to attack, but didn't have LOS
        public Memory InvestigativeMemory { get { return m_InvestigativeMemory; } }
        public void InvestigativeMemoryWipe()
        {
            m_InvestigativeMemory = new Memory();
        }
        private bool SomethingToInvestigate(ref Direction dir)
        {
            List<Mobile> list = new List<Mobile>();
            foreach (DictionaryEntry de in InvestigativeMemory.MemoryCache)
            {
                if (de.Key is Mobile m /*&& CheckDifficulty(m)*/)
                    list.Add(m);
            }

            // closest mobile first
            list.Sort((m1, m2) => { return m1.GetDistanceToSqrt(m_Mobile).CompareTo(m2.GetDistanceToSqrt(m_Mobile)); });

            foreach (Mobile m in list)
            {
                Memory.ObjectMemory om = InvestigativeMemory.Recall(m as object);
                if (om != null && om.Context is KeyValuePair<MovementPath, int> kvp && kvp.Key is MovementPath mp && mp != null)
                {   // okay, extract the path to the mobile to investigate. Make sure he hasn't moved!
                    if (m.Alive && m.Hidden == false /*&& m.Location == mp.Goal Can no longer assume the mobile's location is the goal*/)
                    {
#if DEBUG
                        if (!m_Mobile.InLOS(m))
                            Utility.DebugOut("{0} cannot see {1}", ConsoleColor.Green, m_Mobile, m.Name);
                        else
                            Utility.DebugOut("{0} can see {1}", ConsoleColor.Red, m_Mobile, m.Name);
#endif
                        // see if this is our goal, often it's LOS
                        if (m_Mobile.IAIQuerySuccess(m))
                        {   // we can see them now!
                            InvestigativeMemory.Forget(m);
                            // Done!
                            // For aggressive creatures, we need not do anything. Acquire focus mob will do all the work
                            // But for instance, Patrol Guards, we may want to walk up to them and talk, emote, etc.
                            m_Mobile.IAIResult(m, canPath: true);
                        }
                        else if (kvp.Value < kvp.Key.Directions.Length)
                        {
                            Utility.DebugOut("{0} has {1} more directions to follow", ConsoleColor.Green, m_Mobile, kvp.Key.Directions.Length - kvp.Value);
                            if (m_Mobile.CheckMovement(kvp.Key.Directions[kvp.Value]))
                            {
                                dir = Success(m, kvp);
                                return true;
                            }
                            // we couldn't move to the next point, see if there is a door there we can open
                            Point3D point3D = m_Mobile.Location; //  NextPoint(kvp.Key.Directions[kvp.Value]);
                            if (CheckDoor(point3D))
                            {
                                if (DoorAt(point3D).Open == false)
                                {
                                    Utility.DebugOut("{0} opening the door", ConsoleColor.Red, m_Mobile);
                                    DoorAt(point3D).Use(m_Mobile);
                                }
                                else
                                    Utility.DebugOut("{0}: door already open", ConsoleColor.Red, m_Mobile);
                                // let's see if we can step through doorway
                                if (m_Mobile.CheckMovement(kvp.Key.Directions[kvp.Value]))
                                {
                                    dir = Success(m, kvp);
                                    return true;
                                }
                                else
                                {   // give up, unable to move to door location after opening
                                    Utility.DebugOut("{0} unable to move to door location after opening", ConsoleColor.Green, m_Mobile);
                                    GiveUp(m);
                                }
                            }
                            else
                            {   // give up, spot no longer valid
                                Utility.DebugOut("{0} give up, spot no longer valid", ConsoleColor.Green, m_Mobile);
                                GiveUp(m);
                            }
                        }
                        else
                        {
                            // ran out of locations to move to
                            Utility.DebugOut("{0} ran out of locations to move to", ConsoleColor.Red, m_Mobile);
                            GiveUp(m);
                        }
                    }
                    else
                    {
                        // he's dead, or hidden, or moved .. forget him.
                        Utility.DebugOut("{0} he's dead, or hidden, or moved .. forget him", ConsoleColor.Green, m_Mobile);
                        GiveUp(m);
                    }
                }
            }
            return false;
        }

        public bool CheckDifficulty(Mobile m)
        {
            Mobile from = m_Mobile;

            // always track invisibility spell 
            if (!m.Hidden || InvisibilitySpell.HasTimer(m))
                return true;

            int myMagery = from.Skills[SkillName.Magery].Fixed;
            int myAbilities =
                Math.Max(from.Skills[SkillName.EvalInt].Fixed,
                Math.Max(from.Skills[SkillName.DetectHidden].Fixed,
                from.Skills[SkillName.Tactics].Fixed));

            int hiding = m.Skills[SkillName.Hiding].Fixed;
            int stealth = m.Skills[SkillName.Stealth].Fixed;
            int divisor = hiding + stealth;

            int chance;
            if (divisor > 0)
                chance = 50 * (myMagery + myAbilities) / divisor;
            else
                chance = 100;

            return chance > Utility.Random(100);
        }
        private Direction Success(Mobile m, KeyValuePair<MovementPath, int> kvp)
        {
            // extract the movement info
            Memory.ObjectMemory om = InvestigativeMemory.Recall(m as object);
            // go here next!
            Direction dir = kvp.Key.Directions[kvp.Value];
            if (om != null)
            {   // update context
                // create a new context
                om.Context = new KeyValuePair<MovementPath, int>(kvp.Key, kvp.Value + 1);
                // refresh memory
                InvestigativeMemory.Refresh(m);
            }
            // done!
            return dir;
        }
        private void GiveUp(Mobile m, bool bounce = true)
        {
            InvestigativeMemory.Forget(m);
            m_Mobile.IAIResult(m, canPath: false);
            if (bounce)
            {
                m_Mobile.TargetLocation = RandomBounce();
                Utility.DebugOut("{0} bouncing to location {1}", ConsoleColor.Magenta, m_Mobile, m_Mobile.TargetLocation);
            }
        }
        private BaseDoor DoorAt(Point3D px)
        {
            List<BaseDoor> list = new();
            foreach (Item item in m_Mobile.GetItemsInRange(1))
                if (item is BaseDoor)
                    list.Add(item as BaseDoor);

            // closest door to the mobile first
            list.Sort((d1, d2) => { return m_Mobile.GetDistanceToSqrt(d1).CompareTo(m_Mobile.GetDistanceToSqrt(d2)); });

            if (list.Count > 0)
                return list[0];
            return null;
        }
        private bool CheckDoor(Point3D px)
        {
            if (m_Mobile.CanOpenDoors == false) return false;
            BaseDoor baseDoor = DoorAt(px);
            if (baseDoor == null) return false;
            if (baseDoor.Locked == true)
            {
#if DEBUG
                Utility.DebugOut("{0} door is locked", ConsoleColor.Magenta, m_Mobile);
#endif
                return false;
            }
#if DEBUG
            Utility.DebugOut("{0} chance at door", ConsoleColor.Magenta, m_Mobile);
#endif
            return Utility.Random(5) == 0;
        }
        private Point2D RandomBounce()
        {
            Point3D px = Spawner.GetSpawnPosition(m_Mobile.Map, m_Mobile.Location, homeRange: 4, SpawnFlags.ClearPath, m_Mobile);
            return new Point2D(px.X, px.Y);
        }
#if false
        private Point3D NextPoint(Direction d)
        {   // where we are
            Point3D point3D = m_Mobile.Location;
            // where we are going
            int x, y; x = point3D.X; y = point3D.Y;
            Movement.Movement.Offset(d, ref x, ref y);
            point3D.X = x; point3D.Y = y;
            // change in Z ?
            int newZ;
            m_Mobile.CheckMovement(d, out newZ);
            point3D.Z = newZ;
            return point3D;
        }
#endif
    }
}