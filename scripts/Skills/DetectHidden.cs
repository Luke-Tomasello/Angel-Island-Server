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

/* Scripts\Skills\DetectHidden.cs
 * Changelog
 * 5/15/23, Yoar
 *      Fixed house detect - Now looping over all mobiles in the house's bounds.
 * 5/12/23, Yoar
 *      Implemented passsive detect
 * 8/28/22, Yoar
 *      Faction members can now detect faction traps.
 * 11/23/21, adam
 *      Fix a logic bug for determining NextSkillTime
 *      Note: OnUse should not return the NextSkillTime, but rather a short pause, maybe 1 second. 
 *          The actual NextSkillTime is set after things like canceling the targeting cursor and LOS checks
 * 11/8/21, Yoar
 * 		Added IDetectable interface.
 * 5/11/08, Adam
 *		Upgrade code to use new region hashtables instead of ArrayLists
 * 12/14/07, plasma
 *  Changed in-house detection to use the housing region instead of a crazy 
 *  sized detection range.  This also fixed a bug that made it possible to
 *  have a full power DH check for 22 tiles.
 * 6/03/06, Kit
 *	Changed internal target to public for AI use.
 * 8/15/04, Old Salty
 * 	Added functionality for assessing whether a container is trapped, 
 *  and how likely the player will be to disarm it.
 */

using Server.Engines.PartySystem;
using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public interface IDetectable
    {
        bool OnDetect(Mobile m);
    }

    public interface IPassiveDetectable
    {
        bool OnPassiveDetect(Mobile from);
    }

    public class DetectHidden
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.DetectHidden].Callback = new SkillUseCallback(OnUse);

            EventSink.Movement += EventSink_OnMovement;
            EventSink.MovementObserved += EventSink_OnMovementObserved;
        }

        public static TimeSpan OnUse(Mobile src)
        {
            src.SendLocalizedMessage(500819);//Where will you search?
            src.Target = new InternalTarget();

            //return TimeSpan.FromSeconds(CoreAI.DetectSkillDelay);
            return TimeSpan.FromSeconds(1.0); // Cannot use another skill for 1 second
        }

        public class InternalTarget : Target
        {
            private bool m_SetSkillTime = true;
            public InternalTarget()
                : base(12, true, TargetFlags.None)
            {
            }
            protected override void OnTargetFinish(Mobile from)
            {
                if (m_SetSkillTime)
                    from.NextSkillTime = Core.TickCount;
            }
            protected override void OnTarget(Mobile src, object targ)
            {
                bool foundAnyone = false;

                Point3D p;
                if (targ is Mobile)
                    p = ((Mobile)targ).Location;
                else if (targ is Item)
                    p = ((Item)targ).Location;
                else if (targ is IPoint3D)
                    p = new Point3D((IPoint3D)targ);
                else
                    p = src.Location;

                m_SetSkillTime = false;
                src.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(CoreAI.DetectSkillDelay).TotalMilliseconds;

                //Added functionality for assessing whether a container is trapped,
                //and how likely the player will be to disarm it. (Old Salty, 8/15/04)
                if (targ is TrapableContainer)
                {
                    TrapableContainer trap = (TrapableContainer)targ;
                    if (trap.TrapType == TrapType.None)
                    {
                        src.SendLocalizedMessage(502373); // That doesn't appear to be trapped
                        return;
                    }
                    else if (trap.TrapType != TrapType.None && src.CheckSkill(SkillName.DetectHidden, 0.0, 100.0, contextObj: new object[2]))
                    {
                        string level = "unknown";

                        switch (trap.TrapPower / 25)
                        {
                            case 1:
                                level = "It appears to be protected by a simple and obvious trap."; break;
                            case 2:
                                level = "It seems to be guarded by a somewhat clever trap."; break;
                            case 3:
                                level = "It is protected by a respectably complex trap."; break;
                            case 4:
                                level = "It appears to be guarded by an impressively devious trap."; break;
                            case 5:
                                level = "It is defended by a dauntingly intricate and dangerous trap."; break;
                        }

                        src.LocalOverheadMessage(0, 33, false, level);

                        double minSkill, maxSkill;

                        // 10/31/23, Yoar: Copied from RemoveTrap.cs
                        #region Skill Requirements
                        if (RemoveTrap.EraAI)
                        {
                            double bonus = 0.0;

                            /* AI/MO: Modify minskill with detect hidden skill. What this means is that
                             * with a trappower of 125 (level 5 tmap) and GM DH, min RT skill to unlock
                             * is 75 and max is 100, OR, rather, DH and RT need to be above a combined
                             * 175 to have a chance at disarming the trap. Level 4 power (100), they
                             * need to have above 150 total to have a chance.
                             */
                            if (src.Skills[SkillName.DetectHidden].Base > 50.0)
                                bonus = (src.Skills[SkillName.DetectHidden].Base - 50.0);

                            minSkill = trap.TrapPower - bonus;
                            maxSkill = trap.TrapPower - bonus + 50.0;
                        }
                        else
                        {
                            /* 6/7/23, Yoar: In RunUO, it's impossible to disarm level 5 chests. Is
                             * the RunUO implementation wrong? Let's instead use {-30, +10} as our
                             * difficulty offset, similar to how lockpicking works.
                             */
#if RunUO
                            minSkill = trap.TrapPower;
                            maxSkill = trap.TrapPower + 30.0;
#else
                            minSkill = trap.TrapPower - 30.0;
                            maxSkill = trap.TrapPower + 10.0;
#endif
                        }
                        #endregion

                        double RTskill = src.Skills[SkillName.RemoveTrap].Value;

                        if (RTskill <= minSkill)
                            src.SendMessage("You are baffled by the complexity of this trap.");
                        else if (RTskill <= (minSkill + maxSkill) / 2.0)
                            src.SendMessage("You have a fair chance at disarming this trap.");
                        else if (RTskill < maxSkill)
                            src.SendMessage("You have a very good chance at disarming this trap.");
                        else
                            src.SendMessage("You could disable this trap with ease.");
                    }

                    return;
                }
                //end addition by OldSalty

                double srcSkill = src.Skills[SkillName.DetectHidden].Value;
                int range = (int)(srcSkill / 10.0);

                if (!src.CheckSkill(SkillName.DetectHidden, 0.0, 100.0, contextObj: new object[2]))
                    range /= 2;

                //pla: remove house stuff from here as it's now implelemted additonally after the standard distance DH check


                //BaseHouse house = BaseHouse.FindHouseAt( p, src.Map, 16 );        
                //bool inHouse = ( house != null && house.IsFriend( src ) );

                //if ( inHouse )
                //	range = 22;

                if (range > 0)
                {
                    bool factionTraps = (Faction.Find(src) != null);

                    foreach (object obj in src.Map.GetObjectsInRange(p, range))
                    {
                        if (obj is IDetectable)
                        {
                            if (((IDetectable)obj).OnDetect(src))
                                foundAnyone = true;
                        }
                        else if (factionTraps && obj is BaseFactionTrap)
                        {
                            BaseFactionTrap trap = (BaseFactionTrap)obj;

                            if (src.CheckTargetSkill(SkillName.DetectHidden, trap, 80.0, 100.0, new object[2] /*contextObj*/))
                            {
                                src.SendLocalizedMessage(1042712, true, " " + (trap.Faction == null ? "" : trap.Faction.Definition.FriendlyName)); // You reveal a trap placed by a faction:

                                trap.Visible = true;
                                trap.BeginConceal();

                                foundAnyone = true;
                            }
                        }
                        else if (obj is Mobile && ((Mobile)obj).Hidden && src != obj)
                        {
                            Mobile trg = (Mobile)obj;

                            double ss = srcSkill + Utility.Random(21) - 10;
                            double ts = trg.Skills[SkillName.Hiding].Value + Utility.Random(21) - 10;

                            //pla: removed house bits from this check too
                            if (src.AccessLevel >= trg.AccessLevel && (ss >= ts) && trg.AccessLevel == AccessLevel.Player)
                            {
                                trg.RevealingAction();
                                trg.SendLocalizedMessage(500814); // You have been revealed!
                                foundAnyone = true;
                            }
                        }
                    }
                }

                //pla: if the mobile is in a friended house and targets a spot within the same house
                //then reveal all mobiles within the housing region regardless of skill checks
                BaseHouse house = BaseHouse.FindHouseAt(p, src.Map, 16);
                if (house != null && house.IsInside(src) && house.IsFriend(src))
                {
                    MultiComponentList mcl = house.Components;

                    foreach (Mobile m in src.Map.GetMobilesInBounds(new Rectangle2D(house.X + mcl.Min.X, house.Y + mcl.Min.Y, mcl.Width, mcl.Height)))
                    {
                        if (m.Hidden && m != src && house.Contains(m) && m.AccessLevel == AccessLevel.Player)
                        {
                            m.RevealingAction();
                            m.SendLocalizedMessage(500814); // You have been revealed!
                            foundAnyone = true;
                        }
                    }
                }

                if (!foundAnyone)
                {
                    src.SendLocalizedMessage(500817); // You can see nothing hidden there.
                }
            }
        }

        public static int PassiveDetectRange = 4;

        private static void EventSink_OnMovement(MovementEventArgs e)
        {
            Mobile observer = e.Mobile;

            if (!CanPassiveDetect(observer))
                return;

            int range = GetPassiveDetectRange(observer);

            if (range <= 0)
                return;

            foreach (object o in observer.GetObjectsInRange(range))
            {
                if (!observer.InLOS(o))
                    continue;

                // TODO: Passively detect mobiles here?

                if (o is IPassiveDetectable)
                {
                    IPassiveDetectable detect = (IPassiveDetectable)o;

                    detect.OnPassiveDetect(observer);
                }
            }
        }

        private static void EventSink_OnMovementObserved(MovementObservedEventArgs e)
        {
            Mobile observer = e.Observer;
            Mobile observed = e.Observed;

            if (!CanPassiveDetect(observer))
                return;

            Party party = Party.Get(observer);

            if (observer == observed || (party != null && party == Party.Get(observed)))
                return;

            if (observed is BaseCreature)
            {
                Mobile master = ((BaseCreature)observed).GetMaster();

                if (master != null && (observer == master || (party != null && party == Party.Get(master))))
                    return;
            }

            int range = GetPassiveDetectRange(observer);

            /* Rogues will find it a bit easier to sneak up on their marks
             * as the chance to be automatically revealed when near player-
             * characters while stealthed has been reduced to one tile.
             * https://www.uoguide.com/Siege_Perilous
             */
            if (Core.RuleSets.SiegeRules())
                range = 1;

            if (range <= 0 || !observed.Hidden || !observed.InRange(observer, range) || observed.AccessLevel > AccessLevel.Player)
                return;

            int observerSkill = observer.Skills[SkillName.DetectHidden].Fixed;
            int observedSkill = observer.Skills[SkillName.Stealth].Fixed;

            // just a guess...
            double chance = (observerSkill - observedSkill + 1000) / 20000.0;

            if (Utility.RandomDouble() < chance)
            {
                observed.RevealingAction();
                observed.SendLocalizedMessage(500814); // You have been revealed!
            }
        }

        public static bool CanPassiveDetect(Mobile m)
        {
            return (m.Alive && m.Skills[SkillName.DetectHidden].BaseFixedPoint >= 800);
        }

        public static int GetPassiveDetectRange(Mobile m)
        {
            int skillFixed = Math.Min(1000, m.Skills[SkillName.DetectHidden].Fixed);

            int range = ((skillFixed - 800) / 100) + 1;

            if (skillFixed >= 1000)
                range++;

            return range;
        }
    }
}