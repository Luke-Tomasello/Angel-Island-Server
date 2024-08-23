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

/* Scripts/Engines/AI/AI/MageAI.cs
 * CHANGELOG
 *  10/19/2023, Adam(CanTeleportToLogicalTarget())
 *      In CanTeleportToLogicalTarget():  while the pathing method is a bit more expensive than SpawnOffsets(), the results are more believable as
 *          the creature will teleport to logical points along the 'path'.
 *      If this fails (maybe because we are simply blocked,) we revert back to SpawnOffsets()
 *      Fixes: teleport to master during 'all come' command
 *  8/15/2023, Adam (LogicalTarget(), CanTeleportToLogicalTarget())
 *      Replace the hardcoded Combatant when we are deciding if we should teleport. Often we are teleporting 
 *      to catchup with our master. LogicalTarget() tries to decern this difference. 
 *  8/2/2023, Adam (GetRandomDamageSpell)
 *      Cap maxCircle (as latest RunUO does,) at 8. This should reduce the number of flame strikes
 *  7/29/2023, Adam (SmartTeleport)
 *      I create a 'smart' version of a cached teleport table.
 *      1. Before assigning the task of teleporting, we first call CanTeleportToCombatant()
 *          This does three things:
 *          a. Determine if a path can be created to the target
 *          b. Cache the results of the 'path points' in MageAI.TeleportTable
 *          c. return true/false. If false, we don't bother teleporting (nonsense teleports of old.)
 *      2. If #1 above allows the teleport, the cached TeleportTable is consulted to get the 'best' teleport point.
 *          'best' is the furthest point from us that we still have LOS to.
 * 11/5/21, Adam
 *  Added a CheckTeleport() routine to be called before Teleport is even considered.
 *  a. make sure DisallowAllMoves == false
 *  b. make the sure the caster has sufficient mana for the cast
 *  c. if ((m_Mobile.Target is TeleportSpell.InternalTarget)) Don't cancel!
 * 7/26/2021, Adam: DoActionWander()
 *      We add the check to Home() here because we don't want our mage meditating out in the wilderness.
 *      go home first, then meditate. This is important for Guard type AIs
 * 7/24/21, adam
 *      in (SmartAI && m_Mobile.Mana < m_Mobile.ManaMax) we try to meditate.
 *      I added the clause that if we are hidden, we skip the meditation (revealing action.)
 *	7/11/10, adam
 *		o major reorganization of AI
 *			o push most smart-ai logic from the advanced magery classes down to baseAI so that we can use potions and bandages from 
 *				the new advanced melee class
 *			o remove plasma's preferred focus logic as it pertains to 'memory' .. here we should have simply Remember()'ed the 
 *				ConstantFocus mob instead of all the special cases
 *	4/9/10, adam
 *		Add SmartSpell function for the 8th level default spell behavior of high-level magical mobs
 *		Add a SmartSpellMCi for managing the 8th level default spell behavior of high-level magical mobs
 *	05/25/09, plasma
 *		Force "memory" to always remember a ConstantFocus mob
 *	1/10/09, Adam
 *		Total rewrite of 'reveal' implementation
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  7/15/07, Adam
 *		Bug fix: Creatures staying 'locked on' players outside of LOS (even while under attack)
 *		Redesign AI memory system to be simplier and more flexible (and fix above bug)
 *		The new system makes use of a memory 'heap' of previously Acquired Focus Mobs (AcquireFocusMob())
 *		Once acquired, they are added to the heap. At some future time if AcquireFocusMob() fails to 
 *		find anything, we call AcquireFocusMobFromMemory() to retrieve a Focus Mob From heap Memory.
 *		AcquireFocusMob() could probably have been extended to include the return of hidden mobs, but
 *		the fucntion is already fare too complex. the heap approach is simple and fits nicely within the system.
 *  08/30/06, Kit
 *		Reveal logic fix(prevent wasteful reveals)
 *  6/03/06, Kit
 *		MageAI Global upgrade try to reveal combatant that hides while fighting and attack them
 *		Uses either DH or Magery as needed and if DH or Magery is atleast 40 or 70 respectivily.
 *		Changed dispell requirement to > 80 int, was set to 95, a feeblemind would block dispel.
 *	9/26/05, Adam
 *		in OnFailedMove, add the check to DisallowAllMoves - we should not teleport!
 *	6/05/05, Kit
 *		Fixed problem with OnFailedMove and Come command causeing crash.
 *	6/04/05, Kit
 *		Revamped MageAI for new AI architecture.
 * 01/19/05, Pixie
 *		Fixed teleporting when under "come" command and right next to controller.
 *		Attempt at fix of changing to "come" when attacking something.
 * 12/15/04, Pixie
 *		Changed so that pets under the "come" command will teleport when they are unable to come further.
 *	6/30/04, Pixie
 *		Fixed all cases where mob should be casting archcure instead of cure
 *	6/9/04, Pixie
 *		Made AI be smart about which cure spell to cast.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

#define EXPERIMENTAL_TELEPORT_AI 

using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.Third;
using Server.Targeting;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Mobiles
{
    public class MageAI : BaseAI
    {
        public DateTime m_NextCastTime;

        public MageAI(BaseCreature m)
            : base(m)
        {
        }

        public override bool Think()
        {
            if (m_Mobile.Deleted)
                return false;

            Target targ = m_Mobile.Target;

            if (targ != null)
            {
                ProcessTarget(targ);

                return true;
            }
            else
            {
                return base.Think();
            }
        }

        public const double HealChance = 0.10; // 10% chance to heal at gm magery
        public const double TeleportChance = 0.05; // 5% chance to teleport at gm magery
        public const double DispelChance = 0.75; // 75% chance to dispel at gm magery

        public virtual double ScaleByMagery(double v)
        {
            return m_Mobile.Skills[SkillName.Magery].Value * v * 0.01;
        }

        //Pix: 12/15/04 - special case because MageAI teleports if needed while in combat
        // so we need to provide some mechanism for a pet to return to it's owner via teleport
        public override bool DoOrderCome()
        {
            Server.Point3D oldLocation = m_Mobile.Location;
            bool bReturn = base.DoOrderCome();

            Server.Point3D newLocation = m_Mobile.Location;

            if (oldLocation == newLocation && m_Mobile.GetDistanceToSqrt(m_Mobile.ControlMaster) > 2)
            {
                if (m_Mobile.Target != null)
                {
                    ProcessTarget(m_Mobile.Target);
                }
                else
                {
                    OnFailedMove();
                }
            }

            return bReturn;
        }

        public bool Home()
        {
            if (m_Mobile != null || !m_Mobile.Deleted)  // not deleted
                if (m_Mobile.Home != Point3D.Zero)      // we have a home
                    if (m_Mobile.GetDistanceToSqrt(m_Mobile.Home) <= m_Mobile.RangeHome)
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, "I am home");
                        return true;                    // we have arrived home
                    }

            if (m_Mobile.Home == Point3D.Zero)          // we have no home
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am home");
                return true;                            // so anywhere is home
            }

            m_Mobile.DebugSay(DebugFlags.AI, "I am not home");
            return false;                               // we are not home

        }

        public override bool DoActionWander()
        {
            m_Mobile.DebugSay(DebugFlags.AI, "I am wandering");

            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am going to attack {0}", m_Mobile.FocusMob.Name);
                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
                m_NextCastTime = DateTime.UtcNow;
                return true;
            }
            // 7/26/2021, Adam: We add the check to Home() here because we don't want our mage meditating out in the wilderness.
            //  go home first, then meditate. This is important for Guard type AIs
            else if (SmartAI && m_Mobile.Mana < m_Mobile.ManaMax && Home())
            {
                if (m_Mobile.Hidden == false)
                {
                    // We could meditate for a long time, therefore...
                    // the random chance is to reduce the frequency and to introduce the notion that the mobile didn't see you because they weren't paying attention
                    if (Utility.RandomChance(25))
                        LookAround();

                    m_Mobile.DebugSay(DebugFlags.AI, "I am going to meditate");
                    m_Mobile.UseSkill(SkillName.Meditation);
                }
                else
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "I was going to meditate, but I'm hidden");
                }
            }
            else
            {
                m_Mobile.Warmode = false;

                base.DoActionWander();

                if (m_Mobile.Poisoned)
                {
                    if (m_Mobile.Poison != null)
                    {
                        Spell curespell;

                        if ((((m_Mobile.Skills[SkillName.Magery].Value / (m_Mobile.Poison.Level + 1)) - 20) * 7.5) > 50)
                        {
                            curespell = new CureSpell(m_Mobile, null);
                        }
                        else
                        {
                            curespell = new ArchCureSpell(m_Mobile, null);
                        }

                        curespell.Cast();
                    }
                    else
                    {
                        new CureSpell(m_Mobile, null).Cast();
                    }
                }
                else if (!m_Mobile.Summoned && (SmartAI || (ScaleByMagery(HealChance) > Utility.RandomDouble())))
                {
                    if (m_Mobile.Hits < (m_Mobile.HitsMax - 50))
                    {
                        if (!new GreaterHealSpell(m_Mobile, null).Cast())
                            new HealSpell(m_Mobile, null).Cast();
                    }
                    else if (m_Mobile.Hits < (m_Mobile.HitsMax - 10))
                    {
                        new HealSpell(m_Mobile, null).Cast();
                    }
                }
            }

            return true;
        }

        private bool CheckTeleport()
        {
            if (m_Mobile.DisallowAllMoves)
                return false;   // can't teleport if DisallowAllMoves

            Spell spell = new TeleportSpell(m_Mobile, null);
            if (m_Mobile.Mana < spell.ScaleMana(spell.GetMana()))
            {   // no mana, don't even try.
                spell = null;
                return false;
            }

            if ((m_Mobile.Target is TeleportSpell.InternalTarget))
                return false;   // we're already teleporting

            return true;
        }
        public bool Spawnable()
        {
            Mobile target = m_Mobile.Combatant;
            return Spawnable(target.X, target.Y, target.Z);
        }

        public Mobile LogicalTarget()
        {
            return (m_Mobile.ControlOrder == OrderType.Come || m_Mobile.ControlOrder == OrderType.Follow) ? m_Mobile.ControlMaster : m_Mobile.Combatant;
        }
        public bool Spawnable(int x, int y, int z)
        {
            Mobile me = m_Mobile;
            Mobile target = LogicalTarget();
            if (target == null) return false;
            Point3D px = new Point3D(x, y, z);

            bool can = false;
            if (me.LandOnly)
                can = Utility.CanSpawnLandMobile(target.Map, px, Utility.CanFitFlags.none);
            else if (me.WaterOnly)
                can = Utility.CanSpawnWaterMobile(target.Map, px, Utility.CanFitFlags.none);
            else
                can = Utility.CanSpawnLandMobile(target.Map, px, Utility.CanFitFlags.none) || Utility.CanSpawnWaterMobile(target.Map, target.Location, Utility.CanFitFlags.none);

            return can;
        }
        private bool SpawnOffsets(Mobile target)
        {
            Mobile me = m_Mobile;
            if (target == null) return false;
            int px = target.X;
            int py = target.Y;
            Map map = target.Map;
            for (int i = 0; i < m_Offsets.Length; i += 2)
            {
                int x = m_Offsets[i], y = m_Offsets[i + 1];

                Point3D p = new Point3D(px + x, py + y, 0);

                LandTarget lt = new LandTarget(p, map);
                if (me.InRange(p, 12) && Spawnable(px + x, py + y, lt.Z) && !SpellHelper.CheckMulti(p, map))
                    return true;
            }

            return false;
        }
        public bool CanTeleportToLogicalTarget()
        {
            // while the pathing method is a bit more expensive than SpawnOffsets(), the results are more believable as
            //  the creature will teleport to logical points along the 'path'
            Mobile target = LogicalTarget();
            Mobile me = m_Mobile;
            if (target == null) return false;

            Direction[] directions = null;
            Point3D[] points = null;
            if (Utility.GetPathArray(me.Map, m_Mobile.Location, target.Location, ref directions, ref points))
            {
                TeleportTable = points;
                return true;
            }

            // okay, that didn't work. Maybe we're simply blocked
            if (SpawnOffsets(target))
            {
                TeleportTable = null;
                return true;
            }

            return false;
        }
        public override void OnFailedMove()
        {
            // Adam: add the check to DisallowAllMoves in CheckTeleport - we should not teleport!
            // Adam: Add CanTeleportToCombatant() returns true if it creates a teleport cache
#if EXPERIMENTAL_TELEPORT_AI
            if (LogicalTarget() != null && CheckTeleport() && CanTeleportToLogicalTarget())
#else
            if (m_Mobile.Combatant != null && CanTeleport(m_Mobile.Combatant) && CheckTeleport())
#endif
            {
                if (SmartAI ? Utility.Random(4) == 0 : ScaleByMagery(TeleportChance) > Utility.RandomDouble())
                {
                    if (m_Mobile.Target != null)
                        m_Mobile.Target.Cancel(m_Mobile, TargetCancelType.Canceled);

                    new TeleportSpell(m_Mobile, null).Cast();

                    m_Mobile.DebugSay(DebugFlags.AI, string.Format("I'm going to try teleporting to {0}", LogicalTarget().SafeName));
                }
            }

            else if (LogicalTarget() == null && CheckTeleport() && (SmartAI ? Utility.Random(4) == 0 : ScaleByMagery(TeleportChance) > Utility.RandomDouble()))
            {
                if (m_Mobile.Target != null)
                    m_Mobile.Target.Cancel(m_Mobile, TargetCancelType.Canceled);

                new TeleportSpell(m_Mobile, null).Cast();

                m_Mobile.DebugSay(DebugFlags.AI, "I am stuck, I'm going to try teleporting away");
            }

            else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "My move is blocked, so I am going to attack {0}", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am stuck");
            }
        }

        public virtual Spell GetRandomDamageSpell(Mobile c)
        {
            int maxCircle = (int)((m_Mobile.Skills[SkillName.Magery].Value + 20.0) / (100.0 / 7.0));

            if (maxCircle < 1)
                maxCircle = 1;
            else if (maxCircle > 8)
                maxCircle = 8;

            switch (Utility.Random(maxCircle * 2))
            {
                case 0:
                case 1: return new MagicArrowSpell(m_Mobile, null);
                case 2:
                case 3: return new HarmSpell(m_Mobile, null);
                case 4:
                case 5: return new FireballSpell(m_Mobile, null);
                case 6:
                case 7: return new LightningSpell(m_Mobile, null);
                case 8:
                case 9: return new MindBlastSpell(m_Mobile, null);
                case 10: return new EnergyBoltSpell(m_Mobile, null);
                case 11: return new ExplosionSpell(m_Mobile, null);
                default: return new FlameStrikeSpell(m_Mobile, null);
            }
        }

        public virtual Spell DoDispel(Mobile toDispel)
        {
            if (!SmartAI)
            {
                if (ScaleByMagery(DispelChance) > Utility.RandomDouble())
                {
                    if (toDispel is Daemon)
                        return new MassDispelSpell(m_Mobile, null);
                    else
                        return new DispelSpell(m_Mobile, null);
                }

                return ChooseSpell(toDispel);
            }

            Spell spell = null;

            if (!m_Mobile.Summoned && Utility.Random(0, 4 + (m_Mobile.Hits == 0 ? m_Mobile.HitsMax : (m_Mobile.HitsMax / m_Mobile.Hits))) >= 3)
            {
                if (m_Mobile.Hits < (m_Mobile.HitsMax - 50))
                    spell = new GreaterHealSpell(m_Mobile, null);
                else if (m_Mobile.Hits < (m_Mobile.HitsMax - 20))
                    spell = new HealSpell(m_Mobile, null);
            }

            if (spell == null)
            {
                if (CheckTeleport() && Utility.Random((int)m_Mobile.GetDistanceToSqrt(toDispel)) == 0)
                    spell = new TeleportSpell(m_Mobile, null);
                else if (Utility.Random(3) == 0 && !m_Mobile.InRange(toDispel, 3) && !toDispel.Paralyzed && !toDispel.Frozen)
                    spell = new ParalyzeSpell(m_Mobile, null);
                else
                {
                    if (toDispel is Daemon)
                        return new MassDispelSpell(m_Mobile, null);
                    else
                        return new DispelSpell(m_Mobile, null);
                }
            }

            return spell;
        }

        public virtual Spell ChooseSpell(Mobile c)
        {
            if (!SmartAI)
            {
                if (!m_Mobile.Summoned && ScaleByMagery(HealChance) > Utility.RandomDouble())
                {
                    if (m_Mobile.Hits < (m_Mobile.HitsMax - 50))
                        return new GreaterHealSpell(m_Mobile, null);
                    else if (m_Mobile.Hits < (m_Mobile.HitsMax - 10))
                        return new HealSpell(m_Mobile, null);
                }

                return GetRandomDamageSpell(c);
            }

            Spell spell = null;

            int healChance = (m_Mobile.Hits == 0 ? m_Mobile.HitsMax : (m_Mobile.HitsMax / m_Mobile.Hits));

            if (m_Mobile.Summoned)
                healChance = 0;

            switch (Utility.Random(4 + healChance))
            {
                default:
                case 0: // Heal ourself
                    {
                        if (!m_Mobile.Summoned)
                        {
                            if (m_Mobile.Hits < (m_Mobile.HitsMax - 50))
                                spell = new GreaterHealSpell(m_Mobile, null);
                            else if (m_Mobile.Hits < (m_Mobile.HitsMax - 10))
                                spell = new HealSpell(m_Mobile, null);
                        }

                        break;
                    }
                case 1: // Poison them
                    {
                        if (!c.Poisoned)
                            spell = new PoisonSpell(m_Mobile, null);

                        break;
                    }
                case 2: // Deal some damage
                    {
                        spell = GetRandomDamageSpell(c);

                        break;
                    }
                case 3: // Set up a combo
                    {
                        if (m_Mobile.Mana < 40 && m_Mobile.Mana > 15)
                        {
                            if (c.Paralyzed && !c.Poisoned)
                            {
                                m_Mobile.DebugSay(DebugFlags.AI, "I am going to meditate");

                                m_Mobile.UseSkill(SkillName.Meditation);
                            }
                            else if (!c.Poisoned)
                            {
                                spell = new ParalyzeSpell(m_Mobile, null);
                            }
                        }
                        else if (m_Mobile.Mana > 60)
                        {
                            if (Utility.Random(2) == 0 && !c.Paralyzed && !c.Frozen && !c.Poisoned)
                            {
                                m_Combo = 0;
                                spell = new ParalyzeSpell(m_Mobile, null);
                            }
                            else
                            {
                                m_Combo = 1;
                                spell = new ExplosionSpell(m_Mobile, null);
                            }
                        }

                        break;
                    }
            }

            return spell;
        }

        private int m_Combo = -1;
        public int Combo { get { return m_Combo; } set { m_Combo = value; } }

        public virtual Spell DoCombo(Mobile c)
        {
            Spell spell = null;

            if (m_Combo == 0)
            {
                spell = new ExplosionSpell(m_Mobile, null);
                ++m_Combo; // Move to next spell
            }
            else if (m_Combo == 1)
            {
                spell = new WeakenSpell(m_Mobile, null);
                ++m_Combo; // Move to next spell
            }
            else if (m_Combo == 2)
            {
                if (!c.Poisoned)
                    spell = new PoisonSpell(m_Mobile, null);

                ++m_Combo; // Move to next spell
            }

            if (m_Combo == 3 && spell == null)
            {
                switch (Utility.Random(3))
                {
                    default:
                    case 0:
                        {
                            if (c.Int < c.Dex)
                                spell = new FeeblemindSpell(m_Mobile, null);
                            else
                                spell = new ClumsySpell(m_Mobile, null);

                            ++m_Combo; // Move to next spell

                            break;
                        }
                    case 1:
                        {
                            spell = new EnergyBoltSpell(m_Mobile, null);
                            m_Combo = -1; // Reset combo state
                            break;
                        }
                    case 2:
                        {
                            spell = new FlameStrikeSpell(m_Mobile, null);
                            m_Combo = -1; // Reset combo state
                            break;
                        }
                }
            }
            else if (m_Combo == 4 && spell == null)
            {
                spell = new MindBlastSpell(m_Mobile, null);
                m_Combo = -1;
            }

            return spell;
        }

        /*public virtual bool OtherAttackers(Mobile cur)
		{
			//anything that hasnt attacked us in over a minute is low priority
			DateTime LowAttackIntrest = DateTime.UtcNow - TimeSpan.FromMinutes(1.0);
			//somehow current target just went null so forget about it.
			if(cur == null)
				return true;

			ArrayList aggressors = m_Mobile.Aggressors;
			if ( aggressors.Count > 0 )
			{
				for ( int i = 0; i < aggressors.Count; ++i )
				{
					AggressorInfo info = (AggressorInfo)aggressors[i];
					Mobile temp = info.Attacker;
				
					if(info.LastCombatTime > LowAttackIntrest && temp != null && temp != cur
						&& m_Mobile.CanSee(temp) && m_Mobile.InLOS(temp) &&
						temp.Alive && !temp.IsDeadBondedPet && m_Mobile.CanBeHarmful( temp, false ) && temp.Map == m_Mobile.Map)
						return true; //were being attacked by something else recently and we can still fight em
				}
			}
			return false; //nothing thats a big concern
		}*/

        public virtual bool DoProcessReveal(Mobile c)
        {
            m_Mobile.DebugSay(DebugFlags.AI, "I am going to try and reveal {0} from memory", c.Name);

            bool tryReveal = false;
            double ss = m_Mobile.Skills.DetectHidden.Value;
            double ts = c.Skills[SkillName.Hiding].Value;
            Memory.ObjectMemory om = ShortTermMemory.Recall(c as object);

            if (om == null)
                return tryReveal;

            // we don't reveal the mobile's current location, we reveal the last location we SAW the mobile at
            Point3D px = om.Context is Point3D ? (Point3D)om.Context : new Point3D();

            if (m_Mobile.Debug == true)
            {
                if ((m_Mobile.Skills.DetectHidden.Value >= 40 && ss >= ts) == false)
                    m_Mobile.DebugSay(DebugFlags.AI, "I am not skilled enough to use DetectHidden");

                if ((m_Mobile.Mana >= 30 && m_Mobile.Skills.Magery.Value >= 70) == false)
                    m_Mobile.DebugSay(DebugFlags.AI, "I am not skilled enough to use RevealSpell");
            }

            m_Mobile.DebugSay(DebugFlags.AI, "Doing reveal logic");
            if (m_Mobile.Skills.DetectHidden.Value >= 40 && ss >= ts)
            {
                //compute range
                double srcSkill = m_Mobile.Skills[SkillName.DetectHidden].Value;
                int range = (int)(srcSkill / 20.0);

                if (!m_Mobile.InRange(px, range))
                    RunTo(px, CanRunAI);
                else
                {
                    if (m_Mobile.Target != null && m_Mobile.Target.GetType() != typeof(DetectHidden.InternalTarget))
                        m_Mobile.Target.Cancel(m_Mobile, TargetCancelType.Canceled);

                    m_Mobile.UseSkill(SkillName.DetectHidden);

                    tryReveal = true;
                }
            }
            else if (m_Mobile.Mana >= 30 && m_Mobile.Skills.Magery.Value >= 70 && (m_Mobile.Spell == null || (m_Mobile.Spell != null && m_Mobile.Spell.GetType() != typeof(RevealSpell))) && DateTime.UtcNow >= m_Mobile.NextSpellTime)
            {
                int range = 1 + (int)(m_Mobile.Skills[SkillName.Magery].Value / 20.0);

                //cancel spell
                ISpell i = m_Mobile.Spell;
                if (i != null && i.IsCasting)
                {
                    Spell s = (Spell)i;
                    s.Disturb(DisturbType.EquipRequest, true, false);
                    m_Mobile.FixedEffect(0x3735, 6, 30);

                }
                m_Mobile.Spell = null;

                if (!m_Mobile.InRange(px, range))
                    RunTo(px, CanRunAI);
                else
                {
                    new RevealSpell(m_Mobile, null).Cast();
                    tryReveal = true;
                }

            }

            return tryReveal;
        }

        public override bool DoActionCombat(MobileInfo info)
        {
            m_Mobile.DebugSay(DebugFlags.AI, "doing MageAI base DoActionCombat()");
            Mobile combatant = info.target;
            m_Mobile.Warmode = true;

            // if we can reveal and our target just hid and we Recall them, lets try to reveal
            if (combatant != null && m_Mobile.CanReveal && combatant.Hidden && ShortTermMemory.Recall(combatant) && combatant.Alive && !combatant.IsDeadBondedPet && m_Mobile.CanBeHarmful(combatant, false) && !m_Mobile.Controlled)
            {   // we will keep retrying the reveal if there isn't a more pressing threat
                if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true) == false)
                    if (DoProcessReveal(combatant))
                        return true;
            }

            if (combatant == null || info.gone || info.dead || info.hidden || info.fled)
            {
                // Our combatant is deleted, dead, hidden, or we cannot hurt them
                // Try to find another combatant
                if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "Something happened to my m, so I am going to fight {0}", m_Mobile.FocusMob.Name);

                    m_Mobile.Combatant = combatant = m_Mobile.FocusMob;
                    m_Mobile.FocusMob = null;

                }
                else
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "Something happened to my m, and nothing is around. I am on guard.");
                    // 8/25/2023, Adam: unset everything or else the creature will just stand and spin (pissed off) there.
                    //  By unsetting, the creature returns to master, and resumes guard.
                    m_Mobile.Combatant = combatant = m_Mobile.FocusMob = null;
                    Action = ActionType.Guard;
                    return true;
                }

                if (!m_Mobile.InRange(combatant, m_Mobile.RangePerception))
                {
                    // They are somewhat far away, can we find something else?

                    if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                    {
                        m_Mobile.Combatant = m_Mobile.FocusMob;
                        m_Mobile.FocusMob = null;
                    }
                    else if (!m_Mobile.InRange(combatant, m_Mobile.RangePerception * 3))
                    {
                        m_Mobile.Combatant = null;
                    }

                    combatant = m_Mobile.Combatant;

                    if (combatant == null)
                    {
                        m_Mobile.DebugSay(DebugFlags.AI, "My m has fled, so I am on guard");
                        Action = ActionType.Guard;
                        return true;
                    }
                }
            }

            if (!m_Mobile.InLOS(combatant))
            {
                if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                {
                    m_Mobile.Combatant = combatant = m_Mobile.FocusMob;
                    m_Mobile.FocusMob = null;
                }
            }

            if (combatant != null)
            {
                if (SmartAI && !m_Mobile.StunReady && m_Mobile.Skills[SkillName.Wrestling].Value >= 80.0 && m_Mobile.Skills[SkillName.Anatomy].Value >= 80.0)
                    EventSink.InvokeStunRequest(new StunRequestEventArgs(m_Mobile));

                if (!m_Mobile.Controlled && !m_Mobile.Summoned)
                {
                    if (m_Mobile.Hits < m_Mobile.HitsMax * 20 / 100)
                    {
                        // We are low on health, should we flee?

                        bool flee = false;

                        if (m_Mobile.Hits < combatant.Hits)
                        {
                            // We are more hurt than them

                            int diff = combatant.Hits - m_Mobile.Hits;

                            flee = (Utility.Random(0, 100) > (10 + diff)); // (10 + diff)% chance to flee
                        }
                        else
                        {
                            flee = Utility.Random(0, 100) > 10; // 10% chance to flee
                        }

                        if (flee)
                        {

                            m_Mobile.DebugSay(DebugFlags.AI, "I am going to flee from {0}", combatant.Name);

                            Action = ActionType.Flee;
                            return true;
                        }
                    }
                }

                if (m_Mobile.Spell == null && DateTime.UtcNow > m_NextCastTime && m_Mobile.InRange(combatant, 12))
                {
                    // We are ready to cast a spell
                    m_Mobile.DebugSay(DebugFlags.Pursuit, "Pausing to cast on {0}", combatant);

                    Spell spell = null;
                    Mobile toDispel = FindDispelTarget(true);

                    if (m_Mobile.Poisoned) // Top cast priority is cure
                    {
                        spell = new CureSpell(m_Mobile, null);
                        try
                        {
                            if ((((m_Mobile.Skills[SkillName.Magery].Value / (m_Mobile.Poison.Level + 1)) - 20) * 7.5) > 50)
                            {
                                spell = new CureSpell(m_Mobile, null);
                            }
                            else
                            {
                                spell = new ArchCureSpell(m_Mobile, null);
                            }
                        }
                        catch
                        {
                            spell = new CureSpell(m_Mobile, null);
                        }
                    }
                    else if (toDispel != null) // Something dispellable is attacking us
                    {
                        spell = DoDispel(toDispel);
                    }
                    else if (SmartAI && m_Combo != -1) // We are doing a spell combo
                    {
                        spell = DoCombo(combatant);
                    }
                    else if (SmartAI && (combatant.Spell is HealSpell || combatant.Spell is GreaterHealSpell) && !combatant.Poisoned) // They have a heal spell out
                    {
                        spell = new PoisonSpell(m_Mobile, null);
                    }
                    else
                    {
                        spell = ChooseSpell(combatant);
                    }

                    // Now we have a spell picked
                    // Move first before casting

                    if (SmartAI && toDispel != null)
                    {
                        if (m_Mobile.InRange(toDispel, 10))
                            RunFrom(toDispel);
                        else if (!m_Mobile.InRange(toDispel, 12))
                            RunTo(toDispel, CanRunAI);
                    }
                    else
                    {
                        if (!m_Mobile.InRange(combatant, m_Mobile.RangeFight/* > 2 ? m_Mobile.RangeFight : 2*/))
                            RunTo(combatant, CanRunAI);
                    }

                    if (spell != null && spell.Cast())
                    {
                        TimeSpan delay;

                        if (SmartAI || (spell is DispelSpell))
                        {
                            delay = TimeSpan.FromSeconds(m_Mobile.ActiveSpeed);
                        }
                        else
                        {
                            double del = ScaleByMagery(3.0);
                            double min = 6.0 - (del * 0.75);
                            double max = 6.0 - (del * 1.25);

                            delay = TimeSpan.FromSeconds(min + ((max - min) * Utility.RandomDouble()));
                        }

                        m_NextCastTime = DateTime.UtcNow + delay;
                    }
                }
                else if (m_Mobile.Spell == null || !m_Mobile.Spell.IsCasting)
                {
                    m_Mobile.DebugSay(DebugFlags.Pursuit, "Running towards {0}", combatant);
                    if (!m_Mobile.InRange(combatant, m_Mobile.RangeFight/* > 2 ? m_Mobile.RangeFight : 2*/))
                        RunTo(combatant, CanRun());
                }
                return true;
            }

            return true;
        }
        public override bool DoActionGuard()
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am going to attack {0}", m_Mobile.FocusMob.Name);
                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
                return true;
            }
            else if (m_Mobile.CanReveal)
            {
                //	If we can Recall() a player via FindHiddenTarget, we will enter Combat mode and try to reveal them
                //	Keep in mind, CombatTimer.OnTick() will set the Combatant to null if it sees that the mobile is hidden, 
                //	for this reason, we will need to make this check again in DoActionCombat if the Combatant is null.
                Mobile mx = FindHiddenTarget();
                if (mx != null)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "G: Ah, I remembered {0}!", mx.Name);
                    m_Mobile.Combatant = m_Mobile.FocusMob = mx;
                    Action = ActionType.Combat;
                    return true;
                }
            }

            // do health maintenance
            if (m_Mobile.Poisoned)
            {
                try
                {
                    if ((((m_Mobile.Skills[SkillName.Magery].Value / (m_Mobile.Poison.Level + 1)) - 20) * 7.5) > 50)
                    {
                        new CureSpell(m_Mobile, null).Cast();
                    }
                    else
                    {
                        new ArchCureSpell(m_Mobile, null).Cast();
                    }
                }
                catch
                {
                    new CureSpell(m_Mobile, null).Cast();
                }
            }
            else if (!m_Mobile.Summoned && (SmartAI || (ScaleByMagery(HealChance) > Utility.RandomDouble())))
            {
                if (m_Mobile.Hits < (m_Mobile.HitsMax - 50))
                {
                    if (!new GreaterHealSpell(m_Mobile, null).Cast())
                        new HealSpell(m_Mobile, null).Cast();
                }
                else if (m_Mobile.Hits < (m_Mobile.HitsMax - 10))
                {
                    new HealSpell(m_Mobile, null).Cast();
                }
                else
                {
                    base.DoActionGuard();
                }
            }
            else
            {
                base.DoActionGuard();
            }

            return true;
        }

        public override bool DoActionFlee()
        {
            Mobile c = m_Mobile.Combatant;

            if ((m_Mobile.Mana > 20 || m_Mobile.Mana == m_Mobile.ManaMax) && m_Mobile.Hits > (m_Mobile.HitsMax / 2))
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am stronger now, my guard is up");
                Action = ActionType.Guard;
            }
            else if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "I am scared of {0}", m_Mobile.FocusMob.Name);

                RunFrom(m_Mobile.FocusMob);
                m_Mobile.FocusMob = null;

                if (m_Mobile.Poisoned && Utility.Random(0, 5) == 0)
                {
                    try
                    {
                        if ((((m_Mobile.Skills[SkillName.Magery].Value / (m_Mobile.Poison.Level + 1)) - 20) * 7.5) > 50)
                        {
                            new CureSpell(m_Mobile, null).Cast();
                        }
                        else
                        {
                            new ArchCureSpell(m_Mobile, null).Cast();
                        }
                    }
                    catch
                    {
                        new CureSpell(m_Mobile, null).Cast();
                    }
                }
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "Area seems clear, but my guard is up");

                Action = ActionType.Guard;
                m_Mobile.Warmode = true;
            }

            return true;
        }

        public Mobile FindDispelTarget(bool activeOnly)
        {
            if (m_Mobile.Deleted || m_Mobile.Int < 80 || CanDispel(m_Mobile) || m_Mobile.AutoDispel)
                return null;

            if (activeOnly)
            {
                List<AggressorInfo> aggressed = m_Mobile.Aggressed;
                List<AggressorInfo> aggressors = m_Mobile.Aggressors;

                Mobile active = null;
                double activePrio = 0.0;

                Mobile comb = m_Mobile.Combatant;

                if (comb != null && !comb.Deleted && comb.Alive && !comb.IsDeadBondedPet && m_Mobile.InRange(comb, 12) && CanDispel(comb))
                {
                    active = comb;
                    activePrio = m_Mobile.GetDistanceToSqrt(comb);

                    if (activePrio <= 2)
                        return active;
                }

                for (int i = 0; i < aggressed.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)aggressed[i];
                    Mobile m = (Mobile)info.Defender;

                    if (m != comb && m.Combatant == m_Mobile && m_Mobile.InRange(m, 12) && CanDispel(m))
                    {
                        double prio = m_Mobile.GetDistanceToSqrt(m);

                        if (active == null || prio < activePrio)
                        {
                            active = m;
                            activePrio = prio;

                            if (activePrio <= 2)
                                return active;
                        }
                    }
                }

                for (int i = 0; i < aggressors.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)aggressors[i];
                    Mobile m = (Mobile)info.Attacker;

                    if (m != comb && m.Combatant == m_Mobile && m_Mobile.InRange(m, 12) && CanDispel(m))
                    {
                        double prio = m_Mobile.GetDistanceToSqrt(m);

                        if (active == null || prio < activePrio)
                        {
                            active = m;
                            activePrio = prio;

                            if (activePrio <= 2)
                                return active;
                        }
                    }
                }

                return active;
            }
            else
            {
                Map map = m_Mobile.Map;

                if (map != null)
                {
                    Mobile active = null, inactive = null;
                    double actPrio = 0.0, inactPrio = 0.0;

                    Mobile comb = m_Mobile.Combatant;

                    if (comb != null && !comb.Deleted && comb.Alive && !comb.IsDeadBondedPet && CanDispel(comb))
                    {
                        active = inactive = comb;
                        actPrio = inactPrio = m_Mobile.GetDistanceToSqrt(comb);
                    }

                    IPooledEnumerable eable = m_Mobile.GetMobilesInRange(12);
                    foreach (Mobile m in eable)
                    {
                        if (m != m_Mobile && CanDispel(m))
                        {
                            double prio = m_Mobile.GetDistanceToSqrt(m);

                            if (!activeOnly && (inactive == null || prio < inactPrio))
                            {
                                inactive = m;
                                inactPrio = prio;
                            }

                            if ((m_Mobile.Combatant == m || m.Combatant == m_Mobile) && (active == null || prio < actPrio))
                            {
                                active = m;
                                actPrio = prio;
                            }
                        }
                    }
                    eable.Free();

                    return active != null ? active : inactive;
                }
            }

            return null;
        }

        public bool CanDispel(Mobile m)
        {
            return (m is BaseCreature && ((BaseCreature)m).Summoned && m_Mobile.CanBeHarmful(m, false) && !((BaseCreature)m).IsAnimatedDead);
        }

        public static int[] m_Offsets = new int[]
            {
                -1, -1,
                -1,  0,
                -1,  1,
                 0, -1,
                 0,  1,
                 1, -1,
                 1,  0,
                 1,  1,

                -2, -2,
                -2, -1,
                -2,  0,
                -2,  1,
                -2,  2,
                -1, -2,
                -1,  2,
                 0, -2,
                 0,  2,
                 1, -2,
                 1,  2,
                 2, -2,
                 2, -1,
                 2,  0,
                 2,  1,
                 2,  2
            };

        public virtual void ProcessTarget(Target targ)
        {
            bool isDispel = (targ is DispelSpell.InternalTarget);
            bool isMassDispel = (targ is MassDispelSpell.InternalTarget);
            bool isParalyze = (targ is ParalyzeSpell.InternalTarget);
            bool isTeleport = (targ is TeleportSpell.InternalTarget);
            bool teleportAway = false;
            bool isReveal = (targ is RevealSpell.InternalTarget || targ is DetectHidden.InternalTarget);

            Mobile toTarget;

            if (isReveal)
            {
                targ.Invoke(m_Mobile, m_Mobile);
            }
            if (isDispel)
            {
                toTarget = FindDispelTarget(false);

                if (!SmartAI && toTarget != null)
                    RunTo(toTarget, CanRunAI);
                else if (toTarget != null && m_Mobile.InRange(toTarget, 10))
                    RunFrom(toTarget);
            }
            if (isMassDispel)
            {
                toTarget = FindDispelTarget(false);

                if (!SmartAI && toTarget != null)
                    RunTo(toTarget, CanRunAI);
                else if (toTarget != null && m_Mobile.InRange(toTarget, 10))
                    RunFrom(toTarget);
            }
            else if (SmartAI && (isParalyze || isTeleport))
            {
                toTarget = FindDispelTarget(true);

                if (toTarget == null)
                {
                    toTarget = m_Mobile.Combatant;

                    if (toTarget != null)
                        RunTo(toTarget, CanRunAI);
                }
                else if (m_Mobile.InRange(toTarget, 10))
                {
                    RunFrom(toTarget);
                    teleportAway = true;
                }
                else
                {
                    teleportAway = true;
                }
            }
            else
            {
                if (m_Mobile.ControlOrder == OrderType.Come && isTeleport)
                {
                    toTarget = m_Mobile.ControlMaster;
                }
                else
                {
                    toTarget = m_Mobile.Combatant;
                }

                if (toTarget != null)
                    if (!m_Mobile.InRange(toTarget, m_Mobile.RangeFight/* > 2 ? m_Mobile.RangeFight : 2*/))
                        RunTo(toTarget, CanRunAI);
            }

            if ((targ.Flags & TargetFlags.Harmful) != 0 && toTarget != null)
            {
                if ((targ.Range == -1 || m_Mobile.InRange(toTarget, targ.Range)) && m_Mobile.CanSee(toTarget) && m_Mobile.InLOS(toTarget))
                {
                    targ.Invoke(m_Mobile, toTarget);
                }
                else if (isDispel)
                {
                    targ.Cancel(m_Mobile, TargetCancelType.Canceled);
                }
            }
            else if ((targ.Flags & TargetFlags.Beneficial) != 0)
            {
                targ.Invoke(m_Mobile, m_Mobile);
            }
            else if (isTeleport && toTarget != null)
            {
                Map map = m_Mobile.Map;

                if (map == null)
                {
                    targ.Cancel(m_Mobile, TargetCancelType.Canceled);
                    return;
                }

                int px, py;

                if (teleportAway)
                {
                    int rx = m_Mobile.X - toTarget.X;
                    int ry = m_Mobile.Y - toTarget.Y;

                    double d = m_Mobile.GetDistanceToSqrt(toTarget);

                    px = toTarget.X + (int)(rx * (10 / d));
                    py = toTarget.Y + (int)(ry * (10 / d));
                }
                else
                {
                    px = toTarget.X;
                    py = toTarget.Y;
                }

                // If teleporting to, try to get close to mobile
                for (int i = 0; i < m_Offsets.Length; i += 2)
                {
                    int x = m_Offsets[i], y = m_Offsets[i + 1];

                    Point3D p = new Point3D(px + x, py + y, 0);

                    LandTarget lt = new LandTarget(p, map);

                    if ((targ.Range == -1 || m_Mobile.InRange(p, targ.Range)) && m_Mobile.InLOS(lt) && Spawnable(px + x, py + y, lt.Z) && !SpellHelper.CheckMulti(p, map))
                    {
                        targ.Invoke(m_Mobile, lt);
                        return;
                    }
                }

                #region EXPERIMENTAL_TELEPORT_AI
                if (SmartTeleport(targ))
                    return;
                #endregion EXPERIMENTAL_TELEPORT_AI

                // begin crazy randomness
                int teleRange = targ.Range;

                if (teleRange < 0)
                    teleRange = 12;

                for (int i = 0; i < 10; ++i)
                {
                    Point3D randomPoint = new Point3D(m_Mobile.X - teleRange + Utility.Random(teleRange * 2 + 1), m_Mobile.Y - teleRange + Utility.Random(teleRange * 2 + 1), 0);

                    LandTarget lt = new LandTarget(randomPoint, map);

                    if (m_Mobile.InLOS(lt) && Spawnable(lt.X, lt.Y, lt.Z) && !SpellHelper.CheckMulti(randomPoint, map))
                    {
                        targ.Invoke(m_Mobile, new LandTarget(randomPoint, map));
                        return;
                    }
                }

                targ.Cancel(m_Mobile, TargetCancelType.Canceled);
            }
            else
            {
                targ.Cancel(m_Mobile, TargetCancelType.Canceled);
            }
        }

        public bool SmartTeleport(Server.Targeting.Target targ)
        {
            Map map = m_Mobile.Map;
            #region EXPERIMENTAL_TELEPORT_AI
            Direction[] directions = null;
            Point3D[] points = TeleportTable;   // try to use cached version
            if (points != null)
                Utility.Monitor.DebugOut("Using teleport cache", ConsoleColor.Cyan);
            else
                Utility.Monitor.DebugOut("No teleport cache available", ConsoleColor.Red);

            Mobile focus = LogicalTarget();
            Point3D goal = focus != null ? focus.Location : Point3D.Zero;
            if (goal != Point3D.Zero)
                if (points != null || Utility.GetPathArray(map, m_Mobile.Location, goal, ref directions, ref points))
                {   // get the last LOS point in our list, we will teleport there
                    Point3D? best = null;
                    foreach (Point3D point3D in points)
                    {
                        if (best == null)
                            best = point3D;
                        else if (m_Mobile.InLOS(point3D))
                            best = point3D;
                        else
                            break;
                    }

                    LandTarget lt = new LandTarget((Point3D)best, map);

                    if (Spawnable(lt.X, lt.Y, lt.Z) && !SpellHelper.CheckMulti((Point3D)best, map))
                    {
                        targ.Invoke(m_Mobile, new LandTarget((Point3D)best, map));
                        Utility.Monitor.DebugOut("Got a teleport location", ConsoleColor.Cyan);
                        return true;
                    }
                }
            #endregion EXPERIMENTAL_TELEPORT_AI
            return false;
        }

        #region Serialize
        private SaveFlags m_flags;

        [Flags]
        private enum SaveFlags
        {   // 0x00 - 0x800 reserved for version
            unused = 0x1000
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;                                // current version (up to 4095)
            m_flags = m_flags | (SaveFlags)version;         // save the version and flags
            writer.Write((int)m_flags);

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            m_flags = (SaveFlags)reader.ReadInt();              // grab the version an flags
            int version = (int)(m_flags & (SaveFlags)0xFFF);    // maskout the version

            // add your version specific stuffs here.
            // Make sure to use the SaveFlags for conditional Serialization
            switch (version)
            {
                default: break;
            }

        }
        #endregion Serialize
    }
}