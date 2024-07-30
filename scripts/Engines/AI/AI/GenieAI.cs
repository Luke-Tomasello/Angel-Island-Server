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

/* Scripts/Engines/AI/AI/GenieAI.cs
 * ChangeLog:
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *	12/30/08, Adam
 *		Refactoring names and properties
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	6/04/05, Kit
 *		Ported to leaf class of HumanMageAI 
 *	4/01/04 Created by Kitaras
 */


using Server.Spells;
using Server.Spells.Fifth;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Second;
using Server.Spells.Sixth;
using Server.Spells.Third;
using Server.Targeting;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Mobiles
{
    public class GenieAI : HumanMageAI
    {

        public GenieAI(BaseCreature m)
            : base(m)
        {
        }

        public override void OnFailedMove()
        {
            if (!m_Mobile.DisallowAllMoves)
            {
                if (m_Mobile.Target != null)
                    m_Mobile.Target.Cancel(m_Mobile, TargetCancelType.Canceled);

                new GenieTeleport(m_Mobile, null).Cast();

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
        public void DoParticleEffect(Mobile m, Point3D from, Point3D to, Map fromMap, Map toMap)
        {
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y, from.Z + 4), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y, from.Z), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y, from.Z - 4), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X, from.Y + 1, from.Z + 4), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X, from.Y + 1, from.Z), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X, from.Y + 1, from.Z - 4), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y + 1, from.Z + 11), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y + 1, from.Z + 7), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y + 1, from.Z + 3), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y + 1, from.Z - 1), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(from.X + 1, from.Y, from.Z + 4), fromMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X + 1, to.Y, to.Z), toMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X + 1, to.Y, to.Z - 4), toMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X, to.Y + 1, to.Z + 4), toMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X, to.Y + 1, to.Z), toMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X, to.Y + 1, to.Z - 4), toMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X + 1, to.Y + 1, to.Z + 11), toMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X + 1, to.Y + 1, to.Z + 7), toMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X + 1, to.Y + 1, to.Z + 3), toMap, 0x3728, 13);
            Effects.SendLocationEffect(new Point3D(to.X + 1, to.Y + 1, to.Z - 1), toMap, 0x3728, 13);
            m.PlaySound(0x1FE);
        }

        //paralyze pets
        public virtual Spell DoParalyze(Mobile toParalyze)
        {
            Spell spell = null;

            BaseCreature c = (BaseCreature)toParalyze;

            if (spell == null)
            {
                if ((int)m_Mobile.GetDistanceToSqrt(toParalyze) == 1 || (int)m_Mobile.GetDistanceToSqrt(toParalyze) == 2) spell = new GenieTeleport(m_Mobile, null);
                else if (!m_Mobile.InRange(toParalyze, 3) && c.Controlled && c.ControlMaster != null && !c.Paralyzed)
                    spell = new ParalyzePetSpell(m_Mobile, null);
            }

            return spell;
        }

        public override Spell ChooseSpell(Mobile c)
        {

            Mobile com = m_Mobile.Combatant;
            Spell spell = null;

            int healChance = (m_Mobile.Hits == 0 ? m_Mobile.HitsMax : (m_Mobile.HitsMax / m_Mobile.Hits));

            switch (Utility.Random(1 + healChance))
            {
                default:
                case 0: // Heal ourself
                    {
                        if (!m_Mobile.Summoned)
                        {
                            m_Mobile.DebugSay(DebugFlags.AI, "I am going to heal myself");
                            if (m_Mobile.Hits < (m_Mobile.HitsMax - 50))
                                spell = new GreaterHealSpell(m_Mobile, null);
                            else if (m_Mobile.Hits < (m_Mobile.HitsMax - 10))
                                spell = new HealSpell(m_Mobile, null);
                        }

                        break;
                    }

                case 1: // Set up a combo
                    {
                        if (m_Mobile.Mana < 40 && m_Mobile.Mana > 15)
                        {
                            if (c.Paralyzed)
                            {
                                m_Mobile.DebugSay(DebugFlags.AI, "I am going to meditate");

                                m_Mobile.UseSkill(SkillName.Meditation);
                            }

                        }
                        else if (m_Mobile.Mana > 60)
                        {

                            Combo = 0;
                            m_Mobile.DebugSay(DebugFlags.AI, "I am going to combo and paralyze");
                            if (!c.Paralyzed)
                                spell = new ParalyzeSpell(m_Mobile, null);

                        }

                        break;
                    }
            }

            return spell;
        }

        public override Spell DoCombo(Mobile c)
        {
            Spell spell = null;

            if (Combo == 0)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am doing combo part 1");
                Map fromMap = m_Mobile.Map;
                Point3D from = m_Mobile.Location;

                Map toMap = c.Map;
                Point3D to = c.Location;

                if (toMap != null)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        Point3D loc = new Point3D(to.X - 4 + Utility.Random(8), to.Y - 4 + Utility.Random(9), to.Z);

                        if (toMap.CanSpawnLandMobile(loc))
                        {
                            to = loc;
                            break;
                        }
                        else
                        {
                            loc.Z = toMap.GetAverageZ(loc.X, loc.Y);

                            if (toMap.CanSpawnLandMobile(loc))
                            {
                                to = loc;
                                break;
                            }
                        }
                    }

                    m_Mobile.Map = toMap;
                    m_Mobile.Location = to;
                    //poof effect
                    DoParticleEffect(m_Mobile, from, to, fromMap, toMap);
                }

                spell = new LightningSpell(m_Mobile, null);
                ++Combo; // Move to next spell
            }
            else if (Combo == 1)
            {
                spell = new ParalyzeSpell(m_Mobile, null);
                ++Combo; // Move to next spell
            }
            else if (Combo == 2)
            {
                m_Mobile.Say("I bet your first wish would be that you had never rubbed the lamp!");
                if (!c.Paralyzed)
                {
                    spell = new ParalyzeSpell(m_Mobile, null);
                }
                else
                    spell = new LightningSpell(m_Mobile, null);
                ++Combo; // Move to next spell
            }

            else if (Combo == 3)
            {
                Map fromMap = m_Mobile.Map;
                Point3D from = m_Mobile.Location;

                Map toMap = c.Map;
                Point3D to = c.Location;

                if (toMap != null)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        Point3D loc = new Point3D(to.X - 4 + Utility.Random(8), to.Y - 4 + Utility.Random(9), to.Z);

                        if (toMap.CanSpawnLandMobile(loc))
                        {
                            to = loc;
                            break;
                        }
                        else
                        {
                            loc.Z = toMap.GetAverageZ(loc.X, loc.Y);

                            if (toMap.CanSpawnLandMobile(loc))
                            {
                                to = loc;
                                break;
                            }
                        }
                    }

                    m_Mobile.Map = toMap;
                    m_Mobile.Location = to;

                    DoParticleEffect(m_Mobile, from, to, fromMap, toMap);
                }
                spell = new LightningSpell(m_Mobile, null);
                ++Combo; // Move to next spell
            }

            else if (Combo == 4 && spell == null)
            {
                if (!c.Paralyzed)
                {
                    spell = new ParalyzeSpell(m_Mobile, null);
                }
                else
                    spell = new LightningSpell(m_Mobile, null);
                Combo = -1; // Move to next spell
            }

            return spell;
        }

        public override bool DoActionCombat(MobileInfo info)
        {
            Mobile combatant = info.target;
            m_Mobile.Warmode = true;

            if (combatant == null || info.gone || info.dead || info.hidden || info.fled)
            {
                // Our combatant is deleted, dead, hidden, or we cannot hurt them
                // Try to find another combatant

                if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
                {

                    m_Mobile.DebugSay(DebugFlags.AI, "Something happened to my combatant, so I am going to fight {0}", m_Mobile.FocusMob.Name);

                    m_Mobile.Combatant = combatant = m_Mobile.FocusMob;
                    m_Mobile.FocusMob = null;
                }
                else
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "Something happened to my combatant, and nothing is around. I am on guard.");
                    // 8/25/2023, Adam: unset everything or else the creature will just stand and spin (pissed off) there.
                    //  By unsetting, the creature returns to master, and resumes guard.
                    m_Mobile.Combatant = combatant = m_Mobile.FocusMob = null;
                    Action = ActionType.Guard;
                    return true;
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

            if (SmartAI && (combatant.Spell is MagicTrapSpell || combatant.Spell is MagicArrowSpell))
            {
                EnemyCountersPara = true;
            }

            if (m_Mobile.Paralyzed)
            {
                UseTrapPouch(m_Mobile);

            }
            TrapPouch(m_Mobile);

            if (SmartAI && !m_Mobile.StunReady && m_Mobile.Skills[SkillName.Wrestling].Value >= 80.0 && m_Mobile.Skills[SkillName.Anatomy].Value >= 80.0)
                EventSink.InvokeStunRequest(new StunRequestEventArgs(m_Mobile));

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
                    m_Mobile.DebugSay(DebugFlags.AI, "My combatant has fled, so I am on guard");
                    Action = ActionType.Guard;

                    return true;
                }
            }

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

            if (m_Mobile.Spell == null && DateTime.UtcNow > m_NextCastTime && m_Mobile.InRange(combatant, 12))
            {
                // We are ready to cast a spell
                Spell spell = null;
                Mobile toDispel = FindDispelTarget(true);
                Mobile toParalyze = FindParalyzeTarget(true);

                //try an cure with a pot first if the poison is serious or where in the middle of dumping
                if (UsesPotions && (m_Mobile.Poisoned && m_Mobile.Poison.Level >= 3) || m_Mobile.Poisoned && Combo != -1)
                {
                    DrinkCure(m_Mobile);
                }

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
                else if (toParalyze != null) // Something dispellable is attacking us
                {
                    spell = DoParalyze(toParalyze);
                }
                //take down reflect on are enemy if its up
                else if (combatant.MagicDamageAbsorb > 5)
                {
                    m_Mobile.DebugSay(DebugFlags.AI, "Takeing Down Reflect");
                    spell = new LightningSpell(m_Mobile, null);
                }
                else if (Combo != -1 && !m_Mobile.Paralyzed) // We are doing a spell combo
                {
                    spell = DoCombo(combatant);
                }
                else
                {
                    spell = ChooseSpell(combatant);
                }

                if (SmartAI && toDispel != null)
                {
                    if (m_Mobile.InRange(toDispel, 10))
                        RunFrom(toDispel);
                    else if (!m_Mobile.InRange(toDispel, 12))
                        RunTo(toDispel, CanRunAI);
                }

                if (toParalyze != null)
                {
                    if (m_Mobile.InRange((BaseCreature)toParalyze, 10))
                        RunFrom(toParalyze);
                    else if (!m_Mobile.InRange((BaseCreature)toParalyze, 12))
                        RunTo(toParalyze, CanRunAI);
                }

                else
                {
                    if (RegainingMana == false)
                        RunTo(combatant, CanRunAI);
                }

                if (spell != null && spell.Cast())
                {
                    TimeSpan delay;
                    //spell cast time is equal to the delay for the spells.
                    delay = spell.GetCastDelay() + spell.GetCastRecovery();

                    m_NextCastTime = DateTime.UtcNow + delay;
                }
            }
            else if ((m_Mobile.Spell == null || !m_Mobile.Spell.IsCasting) && RegainingMana == false)
            {
                RunTo(combatant, CanRunAI);
            }

            return true;
        }

        public Mobile FindParalyzeTarget(bool activeOnly)
        {
            if (m_Mobile.Deleted || m_Mobile.Int < 95 || CanDispel(m_Mobile))
                return null;

            if (activeOnly)
            {
                List<AggressorInfo> aggressed = m_Mobile.Aggressed;
                List<AggressorInfo> aggressors = m_Mobile.Aggressors;

                Mobile active = null;
                double activePrio = 0.0;

                Mobile comb = m_Mobile.Combatant;

                if (comb != null && !comb.Deleted && comb.Alive && !comb.IsDeadBondedPet && m_Mobile.InRange(comb, 12) && CanParalyze(comb))
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

                    if (m != comb && m.Combatant == m_Mobile && m_Mobile.InRange(m, 12) && CanParalyze(m))
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

                    if (m != comb && m.Combatant == m_Mobile && m_Mobile.InRange(m, 12) && CanParalyze(m))
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

                    if (comb != null && !comb.Deleted && comb.Alive && !comb.IsDeadBondedPet && CanParalyze(comb))
                    {
                        active = inactive = comb;
                        actPrio = inactPrio = m_Mobile.GetDistanceToSqrt(comb);
                    }

                    IPooledEnumerable eable = m_Mobile.GetMobilesInRange(12);
                    foreach (Mobile m in eable)
                    {
                        if (m != m_Mobile && CanParalyze(m))
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

        public bool CanParalyze(Mobile m)
        {
            return (m is BaseCreature && !m.Paralyzed && ((BaseCreature)m).Controlled == true && ((BaseCreature)m).ControlMaster != null && m_Mobile.CanBeHarmful(m, false));
        }

        public override void ProcessTarget(Target targ)
        {
            bool isDispel = (targ is DispelSpell.InternalTarget);
            bool isParalyze = (targ is ParalyzeSpell.InternalTarget);
            bool isTeleport = (targ is GenieTeleport.InternalTarget);
            bool teleportAway = false;
            bool isPet = (targ is ParalyzePetSpell.InternalTarget);

            Mobile toTarget;
            if (isDispel)
            {
                toTarget = FindDispelTarget(false);

                if (toTarget != null && m_Mobile.InRange(toTarget, 10))
                    RunFrom(toTarget);
            }

            if (isPet)
            {
                toTarget = FindParalyzeTarget(false);

                if (toTarget != null && m_Mobile.InRange(toTarget, 10))
                    RunFrom(toTarget);
            }
            else if ((isParalyze || isTeleport))
            {
                toTarget = FindDispelTarget(true);

                toTarget = FindParalyzeTarget(true);

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
                toTarget = m_Mobile.Combatant;

                if (toTarget != null)
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
                else if (isPet)
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

                for (int i = 0; i < m_Offsets.Length; i += 2)
                {
                    int x = m_Offsets[i], y = m_Offsets[i + 1];

                    Point3D p = new Point3D(px + x, py + y, 0);

                    LandTarget lt = new LandTarget(p, map);

                    if ((targ.Range == -1 || m_Mobile.InRange(p, targ.Range)) && m_Mobile.InLOS(lt) && map.CanSpawnLandMobile(px + x, py + y, lt.Z) && !SpellHelper.CheckMulti(p, map))
                    {
                        targ.Invoke(m_Mobile, lt);
                        return;
                    }
                }

                int teleRange = targ.Range;

                if (teleRange < 0)
                    teleRange = 12;

                for (int i = 0; i < 10; ++i)
                {
                    Point3D randomPoint = new Point3D(m_Mobile.X - teleRange + Utility.Random(teleRange * 2 + 1), m_Mobile.Y - teleRange + Utility.Random(teleRange * 2 + 1), 0);

                    LandTarget lt = new LandTarget(randomPoint, map);

                    if (m_Mobile.InLOS(lt) && map.CanSpawnLandMobile(lt.X, lt.Y, lt.Z) && !SpellHelper.CheckMulti(randomPoint, map))
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