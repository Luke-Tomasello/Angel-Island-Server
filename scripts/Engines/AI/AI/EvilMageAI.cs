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

/* Scripts/Engines/AI/AI/EvilMageAI.cs
 * CHANGELOG
 *	1/1/09, Adam
 *		Add new Serialization model for creature AI.
 *		BaseCreature will already be serializing this data when I finish the upgrade, so all you need to do is add your data to serialize. 
 *		Make sure to use the SaveFlags to optimize saves by not writing stuff that is non default.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  5/3/05, Kit
 *		Initial Creation
 */


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
    public class EvilMageAI : BaseAI
    {
        private DateTime m_NextCastTime;

        public EvilMageAI(BaseCreature m)
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

        public override bool SmartAI
        {
            get { return true; }
        }

        private const double HealChance = 0.10; // 10% chance to heal at gm magery
        private const double TeleportChance = 0.05; // 5% chance to teleport at gm magery
        private const double DispelChance = 0.75; // 75% chance to dispel at gm magery

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


        public override bool DoActionWander()
        {
            if (AcquireFocusMob(m_Mobile.RangePerception, m_Mobile.FightMode, false, false, true))
            {

                m_Mobile.DebugSay(DebugFlags.AI, "I am going to attack {0}", m_Mobile.FocusMob.Name);

                m_Mobile.Combatant = m_Mobile.FocusMob;
                Action = ActionType.Combat;
                m_NextCastTime = DateTime.UtcNow;
                return true;
            }
            else if (SmartAI && m_Mobile.Mana < m_Mobile.ManaMax)
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am going to meditate");

                m_Mobile.UseSkill(SkillName.Meditation);
            }
            else
            {
                m_Mobile.DebugSay(DebugFlags.AI, "I am wandering");

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

        public void RunTo(Mobile m)
        {
            if (!SmartAI)
            {
                if (!MoveTo(m, true, m_Mobile.RangeFight))
                    OnFailedMove();

                return;
            }

            if (m.Paralyzed || m.Frozen)
            {
                if (m_Mobile.InRange(m, 1))
                    RunFrom(m);
                else if (!m_Mobile.InRange(m, m_Mobile.RangeFight > 2 ? m_Mobile.RangeFight : 2) && !MoveTo(m, true, 1))
                    OnFailedMove();
            }
            else
            {
                if (!m_Mobile.InRange(m, m_Mobile.RangeFight))
                {
                    if (!MoveTo(m, true, 1))
                        OnFailedMove();
                }
                else if (m_Mobile.InRange(m, m_Mobile.RangeFight - 1))
                {
                    RunFrom(m);
                }
            }
        }

        public override void RunFrom(Mobile m)
        {
            Run((m_Mobile.GetDirectionTo(m) - 4) & Direction.Mask);
        }

        public override void OnFailedMove()
        {
            if (!m_Mobile.DisallowAllMoves && (SmartAI ? Utility.Random(4) == 0 : ScaleByMagery(TeleportChance) > Utility.RandomDouble()))
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

        public override void Run(Direction d)
        {
            if ((m_Mobile.Spell != null && m_Mobile.Spell.IsCasting) || m_Mobile.Paralyzed || m_Mobile.Frozen || m_Mobile.DisallowAllMoves)
                return;

            m_Mobile.Direction = d | Direction.Running;

            if (!DoMove(m_Mobile.Direction, true))
                OnFailedMove();
        }

        public virtual Spell GetRandomDamageSpell()
        {
            int maxCircle = (int)((m_Mobile.Skills[SkillName.Magery].Value + 20.0) / (100.0 / 7.0));

            if (maxCircle < 1)
                maxCircle = 1;

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
                    return new DispelSpell(m_Mobile, null);

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
                if (!m_Mobile.DisallowAllMoves && Utility.Random((int)m_Mobile.GetDistanceToSqrt(toDispel)) == 0)
                    spell = new TeleportSpell(m_Mobile, null);
                else if (Utility.Random(3) == 0 && !m_Mobile.InRange(toDispel, 3) && !toDispel.Paralyzed && !toDispel.Frozen)
                    spell = new ParalyzeSpell(m_Mobile, null);
                else
                    spell = new DispelSpell(m_Mobile, null);
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

                return GetRandomDamageSpell();
            }

            if (c.Int > 70 && m_Mobile.CanBeginAction(typeof(DefensiveSpell)))
                return new MagicReflectSpell(m_Mobile, null);

            if (c.Dex > 60 && m_Mobile.CanBeginAction(typeof(DefensiveSpell)))
                return new ReactiveArmorSpell(m_Mobile, null);


            Spell spell = null;

            int healChance = (m_Mobile.Hits == 0 ? m_Mobile.HitsMax : (m_Mobile.HitsMax / m_Mobile.Hits));

            if (m_Mobile.Summoned)
                healChance = 0;

            switch (Utility.Random(1 + healChance))
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
                //case 1: // Poison them
                //{
                //	if ( !c.Poisoned )
                //		spell = new PoisonSpell( m_Mobile, null );
                //
                //	break;
                //	}
                //case 2: // Deal some damage
                //	{
                //		spell = GetRandomDamageSpell();
                //
                //		break;
                //	}
                case 1: // Set up a combo
                    {
                        if (m_Mobile.Mana < 50 && m_Mobile.Mana > 15)
                        {
                            if (c.Paralyzed && !c.Poisoned)
                            {
                                if (c.Hits < 45)
                                    spell = new ExplosionSpell(m_Mobile, null);

                                if (c.Hits < 30)
                                    spell = new EnergyBoltSpell(m_Mobile, null);

                                m_Mobile.DebugSay(DebugFlags.AI, "I am going to meditate");

                                m_Mobile.UseSkill(SkillName.Meditation);
                            }
                            else if (!c.Poisoned)
                            {
                                spell = new ParalyzeSpell(m_Mobile, null);
                            }
                        }
                        else if (m_Mobile.Mana > 80)
                        {

                            m_Combo = 1;
                            spell = new ExplosionSpell(m_Mobile, null);

                        }

                        break;
                    }
            }

            return spell;
        }

        protected int m_Combo = -1;
        protected int m_Combo2 = -1;
        protected int m_Combo3 = -1;

        public virtual Spell DoCombo(Mobile c)
        {
            Spell spell = null;

            if (m_Combo == 0)
            {
                //m_Mobile.Say( "combo phase 0" );
                spell = new ExplosionSpell(m_Mobile, null);
                ++m_Combo; // Move to next spell
            }
            else if (m_Combo == 1)
            {
                //m_Mobile.Say( "combo phase 1" );
                spell = new ExplosionSpell(m_Mobile, null);
                ++m_Combo; // Move to next spell
            }
            else if (m_Combo == 2)
            {
                //m_Mobile.Say( "combo phase 2" );
                if (!c.Poisoned)
                    spell = new PoisonSpell(m_Mobile, null);

                ++m_Combo; // Move to next spell
            }

            else if (m_Combo == 3)
            {

                //m_Mobile.Say( "combo phase 3" );
                if (c.Poisoned)
                    spell = new WeakenSpell(m_Mobile, null);

                if (!c.Poisoned)
                    spell = new PoisonSpell(m_Mobile, null);

                ++m_Combo; // Move to next spell
            }

            else if (m_Combo == 4)
            {
                //	m_Mobile.Say( "combo phase 4 ebolt" );
                spell = new EnergyBoltSpell(m_Mobile, null);

                ++m_Combo; // Move to next spell
            }

            else if (m_Combo == 5)
            {
                //m_Mobile.Say( "combo phase 5" );
                if (c.Poisoned && c.Alive)
                {
                    spell = new HarmSpell(m_Mobile, null);

                    m_Combo = 5; // Move to next spell
                }

                if (!c.Poisoned)
                {
                    if (m_Mobile.Mana > 20)
                        spell = new EnergyBoltSpell(m_Mobile, null);

                    if (m_Mobile.Mana < 19)
                        spell = new LightningSpell(m_Mobile, null);
                    m_Combo = -1; // Reset combo state
                }

            }

            return spell;
        }

        public virtual Spell DoCombo2(Mobile c)
        {
            Spell spell = null;

            if (m_Combo2 == 0)
            {
                spell = new ExplosionSpell(m_Mobile, null);
                ++m_Combo2; // Move to next spell
            }
            else if (m_Combo2 == 1)
            {
                spell = new WeakenSpell(m_Mobile, null);
                ++m_Combo2; // Move to next spell
            }
            else if (m_Combo2 == 2)
            {
                if (!c.Poisoned)
                    spell = new PoisonSpell(m_Mobile, null);

                ++m_Combo2; // Move to next spell
            }

            if (m_Combo2 == 3 && spell == null)
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

                            ++m_Combo2; // Move to next spell

                            break;
                        }
                    case 1:
                        {
                            spell = new EnergyBoltSpell(m_Mobile, null);
                            m_Combo2 = -1; // Reset combo state
                            break;
                        }
                    case 2:
                        {
                            spell = new FlameStrikeSpell(m_Mobile, null);
                            m_Combo2 = -1; // Reset combo state
                            break;
                        }
                }
            }
            else if (m_Combo2 == 4 && spell == null)
            {
                spell = new MindBlastSpell(m_Mobile, null);
                m_Combo2 = -1;
            }

            return spell;
        }

        public virtual Spell DoCombo3(Mobile c)
        {
            Spell spell = null;

            if (m_Combo3 == 0)
            {
                spell = new ExplosionSpell(m_Mobile, null);
                ++m_Combo3; // Move to next spell
            }
            else if (m_Combo3 == 1)
            {
                spell = new WeakenSpell(m_Mobile, null);
                ++m_Combo3; // Move to next spell
            }
            else if (m_Combo3 == 2)
            {
                if (!c.Poisoned)
                    spell = new PoisonSpell(m_Mobile, null);

                ++m_Combo3; // Move to next spell
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

                            ++m_Combo3; // Move to next spell

                            break;
                        }
                    case 1:
                        {
                            spell = new EnergyBoltSpell(m_Mobile, null);
                            m_Combo3 = -1; // Reset combo state
                            break;
                        }
                    case 2:
                        {
                            spell = new FlameStrikeSpell(m_Mobile, null);
                            m_Combo3 = -1; // Reset combo state
                            break;
                        }
                }
            }
            else if (m_Combo == 4 && spell == null)
            {
                spell = new MindBlastSpell(m_Mobile, null);
                m_Combo3 = -1;
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
                else if (m_Combo != -1) // We are doing a spell combo
                {
                    spell = DoCombo(combatant);
                }
                else if ((combatant.Spell is HealSpell || combatant.Spell is GreaterHealSpell) && !combatant.Poisoned) // They have a heal spell out
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
                        RunTo(toDispel);
                }
                else
                {
                    RunTo(combatant);
                }

                if (spell != null && spell.Cast())
                {
                    TimeSpan delay;

                    if (SmartAI || (spell is DispelSpell))
                    {
                        delay = TimeSpan.FromSeconds(m_Mobile.ActiveSpeed);
                    }
                    if (spell is HarmSpell)
                    {
                        delay = TimeSpan.FromSeconds(m_Mobile.ActiveSpeed);
                    }

                    else
                    {
                        double del = ScaleByMagery(4.3);
                        double min = 6.0 - (del * 0.75);
                        double max = 6.0 - (del * 1.25);

                        delay = TimeSpan.FromSeconds(min + ((max - min) * Utility.RandomDouble()));
                    }

                    m_NextCastTime = DateTime.UtcNow + delay;
                }
            }
            else if (m_Mobile.Spell == null || !m_Mobile.Spell.IsCasting)
            {
                RunTo(combatant);
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
            }
            else
            {
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
            if (m_Mobile.Deleted || m_Mobile.Int < 95 || CanDispel(m_Mobile) || m_Mobile.AutoDispel)
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

        private static int[] m_Offsets = new int[]
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

        private void ProcessTarget(Target targ)
        {
            bool isDispel = (targ is DispelSpell.InternalTarget);
            bool isParalyze = (targ is ParalyzeSpell.InternalTarget);
            bool isTeleport = (targ is TeleportSpell.InternalTarget);
            bool teleportAway = false;

            Mobile toTarget;

            if (isDispel)
            {
                toTarget = FindDispelTarget(false);

                if (!SmartAI && toTarget != null)
                    RunTo(toTarget);
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
                        RunTo(toTarget);
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
                    RunTo(toTarget);
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