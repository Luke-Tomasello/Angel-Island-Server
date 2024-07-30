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

/* Scripts/Spells/Base/Spell.cs
 *	ChangeLog:
 *	3/25/23, Yoar: IMagicItem interface
 *      Added support for IMagicItem interface
 *	1/6/23, Yoar: IWand interface
 *      Added support for IWand interface
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	8/28/21, Lib
 *	    changed DamageDelay default value to use CoreAI setting
 *	    refactored per-spell delay alterations (functionally equiv)
 *	3/20/10. adam
 *		reverting MB to standard 5th circle casting speed. 
 *		see damage-scale modifications to the spell
 *	8/31/07, Adam
 *		Change CheckSkill() to check against a max skill of 100 instead of 120
 *  12/31/06, Kit
 *      Update IsRestrictedSpell to pass mobile for new invalid range logging, added Exception logging
 *      to generic catch()
 *  10/22/06, Rhiannon
 *		Added logging of spellcasting by staff.
 *  08/19/06, Kit
 *		Fixed bug with RestrictCreatureMagic and region logic.
 *  06/25/06, Kit
 *		Added check for RestrictCreatureMagic and preventing casting. Added check for if controller
 *		has a non default MagicFailure Msg and to use it if so.
 *  06/24/06, Kit
 *		Added drdt checks to CheckBSequence/CheckHSequence, to prevent casting of harmfull/benifical
 *		spells into a region that disallows that spell. 
 *  06/03/06, Kit
 *		Added enum SpellDamageType and virtual function DamageType, defaults to returning None.
 *  05/15/06, Kit
 *		Added two new virtual functions MinDamage/MaxDamage for use in spell definitions instead of hardcoded value.
 *  04/17/06, Kit
 *		Added new check in ConsumeReagents to test if AI type of creature uses reagents or not.
 *	9/02/05, erlein
 *		Disturb(): Removed next spell delay for instances of "daggering" and [cancelspell
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *	5/05/05, Kit
 *  		Added additional null checks for drdt regions and spell casting
 *	4/29/05, Kit
 *		Added check for custom regions to CheckSequence()
 *	8/12/04, mith
 *		GetCastSkills(): modified to use pre-AoS values for min/max for each circle.
 *	6/27/04, Pix
 *		Fixed so magicresist skill is checked for gains even when target has >=100% and <=0%
 *		chance to resist.
 *	6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/28/04 changes by smerX
 *		Additional casting time for Cure spell (yes, more)
 *	5/5/04 changes by smerX
 *		Additional casting time for Cure spell
 *	4/22/04 changes by smerX
 *		Must wait for FC timer to finish if DisturbType.Hurt
 *	3/25/04 changes by smerX:
 *		Changed NextSpellDelay to 0.75
 *		Changed NextSpellDelay for Heal to 0.95
 *		Added 0.5 Casting time to MindBlast
 *		Made OnDisturbDelay()'s function dependant on DisruptType
 *		Changed formula for FC time ("delay = CastDelayBase + circleDelay - 2")
 *	3/25/04 changes by mith:
 *		modified CheckSkill calls to reflect max value of 100 instead of 120
 *	3/18/04 code changes by smerX:
 *		Changed NextSpellDelay to 1.0 seconds
 *		Removed AoS FC value from Casting time equasion
 *		Changed OnDisturbDelay()
 *		Amended m_Caster.NextSpellTime = DateTime.UtcNow + GetDisturbRecovery();
 */
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Spells.Second;
using Server.Targeting;
using System;

namespace Server.Spells
{
    public enum SpellDamageType
    {
        None,
        Fire,
        Energy,
        Electrical,
        Cold,
        Posion
    }

    public abstract class Spell : ISpell
    {
        private Mobile m_Caster;
        private Item m_Scroll;
        private SpellInfo m_Info;
        private SpellState m_State;
        private DateTime m_StartCastTime;

        public SpellState State { get { return m_State; } set { m_State = value; } }
        public Mobile Caster { get { return m_Caster; } }
        public SpellInfo Info { get { return m_Info; } }
        public string Name { get { return m_Info.Name; } }
        public string Mantra { get { return m_Info.Mantra; } }
        public SpellCircle Circle { get { return m_Info.Circle; } set { m_Info.Circle = value; } }
        public Type[] Reagents { get { return m_Info.Reagents; } }
        public Item Scroll { get { return m_Scroll; } }

        //private static TimeSpan NextSpellDelay = TimeSpan.FromSeconds( 1.0 );  // value changes
        private static TimeSpan NextSpellDelay;

        private static TimeSpan AnimateDelay = TimeSpan.FromSeconds(1.5);

        public virtual SkillName CastSkill { get { return SkillName.Magery; } }
        public virtual SkillName DamageSkill { get { return SkillName.EvalInt; } }

        public virtual bool RevealOnCast { get { return true; } }
        public virtual bool ClearHandsOnCast { get { return !(m_Scroll is IMagicItem); } }
        public virtual bool EquipSpellbookOnCast { get { return !Core.RuleSets.AngelIslandRules() && !Core.RuleSets.MortalisRules() && PublishInfo.CheckPublish(5) ? true : false; } }

        public virtual bool DelayedDamage { get { return false; } }

        //Min/Max damage
        public virtual int MinDamage { get { return 0; } }
        public virtual int MaxDamage { get { return 0; } }

        //spell damage type
        public virtual SpellDamageType DamageType { get { return SpellDamageType.None; } }

        public Spell(Mobile caster, Item scroll, SpellInfo info)
        {
            m_Caster = caster;
            m_Scroll = scroll;
            m_Info = info;
        }

        public virtual int GetNewAosDamage(int bonus, int dice, int sides)
        {
            int damage = Utility.Dice(dice, sides, bonus) * 100;
            int damageBonus = 0;

            int inscribeSkill = GetInscribeFixed(m_Caster);
            int inscribeBonus = (inscribeSkill + (1000 * (inscribeSkill / 1000))) / 200;
            damageBonus += inscribeBonus;

            int intBonus = Caster.Int / 10;
            damageBonus += intBonus;

            int sdiBonus = AosAttributes.GetValue(m_Caster, AosAttribute.SpellDamage);
            damageBonus += sdiBonus;

            damage = AOS.Scale(damage, 100 + damageBonus);

            int evalSkill = GetDamageFixed(m_Caster);
            int evalScale = 30 + ((9 * evalSkill) / 100);

            damage = AOS.Scale(damage, evalScale);

            return damage / 100;
        }

        public virtual double GetAosDamage(int min, int random, double div)
        {
            double scale = 1.0;

            scale += GetInscribeSkill(m_Caster) * 0.001;

            if (Caster.Player)
            {
                scale += Caster.Int * 0.001;
                scale += AosAttributes.GetValue(m_Caster, AosAttribute.SpellDamage) * 0.01;
            }

            int baseDamage = min + (int)(GetDamageSkill(m_Caster) / div);

            double damage = Utility.RandomMinMax(baseDamage, baseDamage + random);

            return damage * scale;
        }

        public virtual bool IsCasting { get { return m_State == SpellState.Casting; } }

        public virtual void OnCasterHurt()
        {
            if (IsCasting)
            {
                object o = ProtectionSpell.Registry[m_Caster];
                bool disturb = true;

                if (o != null && o is double)
                {
                    if (((double)o) > Utility.RandomDouble() * 100.0)
                        disturb = false;
                }

                if (disturb)
                    Disturb(DisturbType.Hurt, false, true);
            }
        }

        public virtual void OnCasterKilled()
        {
            Disturb(DisturbType.Kill);
        }

        public virtual void OnConnectionChanged()
        {
            FinishSequence();
        }

        public virtual bool OnCasterMoving(Direction d)
        {
            if (IsCasting && BlocksMovement)
            {
                m_Caster.SendLocalizedMessage(500111); // You are frozen and can not move.
                return false;
            }

            return true;
        }

        public virtual bool OnCasterEquiping(Item item)
        {
            // allow equipping of a spellbook to cast
            if (IsCasting && !item.AllowEquipedCast(m_Caster))
                Disturb(DisturbType.EquipRequest);

            return true;
        }

        public virtual bool OnCasterUsingObject(object o)
        {
            if (m_State == SpellState.Sequencing)
                Disturb(DisturbType.UseRequest);

            return true;
        }

        public virtual bool OnCastInTown(Region r)
        {
            return m_Info.AllowTown;
        }

        public virtual bool ConsumeReagents()
        {
            if (m_Scroll != null || (!m_Caster.Player && m_Caster is BaseCreature && ((BaseCreature)m_Caster).AIObject.UsesRegs == false))
                return true;

            if (AosAttributes.GetValue(m_Caster, AosAttribute.LowerRegCost) > Utility.Random(100))
                return true;

            #region Dueling
            if (Engines.ConPVP.DuelContext.IsFreeConsume(m_Caster))
                return true;
            #endregion

            Container pack = m_Caster.Backpack;

            if (pack == null)
                return false;

            if (pack.ConsumeTotal(m_Info.Reagents, m_Info.Amounts) == -1)
                return true;

            if (GetType().BaseType == typeof(Spell))
            {
                if (ArcaneGem.ConsumeCharges(m_Caster, 1 + (int)Circle))
                    return true;
            }

            return false;
        }

        // Old AI formula
        public virtual bool CheckResisted(Mobile target)
        {
            int maxSkill = (1 + (int)Circle) * 10;
            maxSkill += (1 + ((int)Circle / 6)) * 25;

            // CheckSkill call modified to lower max to 100.
            if (target.Skills[SkillName.MagicResist].Value < maxSkill)
                target.CheckSkill(SkillName.MagicResist, 0.0, 100.0, contextObj: new object[2]);

            double n = GetResistPercent(target);

            n /= 100.0;

            if (n <= 0.0)
                return false;

            if (n >= 1.0)
                return true;

            return (n >= Utility.RandomDouble());
        }
        public virtual double GetResistPercentForCircle(Mobile target, SpellCircle circle)
        {
            double firstPercent = target.Skills[SkillName.MagicResist].Value / 5.0;
            double secondPercent = target.Skills[SkillName.MagicResist].Value - (((m_Caster.Skills[CastSkill].Value - 20.0) / 5.0) + (1 + (int)circle) * 5.0);

            double percent = (firstPercent > secondPercent ? firstPercent : secondPercent);

            if (!SpellHelper.OldResist)
                percent /= 2.0; // Seems should be about half of what stratics says.

            return percent;
        }

        public virtual double GetResistPercent(Mobile target)
        {
            return GetResistPercentForCircle(target, m_Info.Circle);
        }

        public virtual double GetInscribeSkill(Mobile m)
        {
            // There is no chance to gain
            // m.CheckSkill( SkillName.Inscribe, 0.0, 120.0 );

            return m.Skills[SkillName.Inscribe].Value;
        }

        public virtual int GetInscribeFixed(Mobile m)
        {
            // There is no chance to gain
            // m.CheckSkill( SkillName.Inscribe, 0.0, 120.0 );

            return m.Skills[SkillName.Inscribe].Fixed;
        }

        public virtual int GetDamageFixed(Mobile m)
        {
            m.CheckSkill(DamageSkill, 0.0, 100.0, contextObj: new object[2]);

            return m.Skills[DamageSkill].Fixed;
        }

        public virtual double GetDamageSkill(Mobile m)
        {
            // CheckSkill call modified to lower max to 100.
            m.CheckSkill(DamageSkill, 0.0, 100.0, contextObj: new object[2]);

            return m.Skills[DamageSkill].Value;
        }

        public virtual int GetResistFixed(Mobile m)
        {
            int maxSkill = (1 + (int)Circle) * 10;
            maxSkill += (1 + ((int)Circle / 6)) * 25;

            if (m.Skills[SkillName.MagicResist].Value < maxSkill)
                m.CheckSkill(SkillName.MagicResist, 0.0, 100.0, contextObj: new object[2]);

            return m.Skills[SkillName.MagicResist].Fixed;
        }

        public virtual double GetResistSkill(Mobile m)
        {
            int maxSkill = (1 + (int)Circle) * 10;
            maxSkill += (1 + ((int)Circle / 6)) * 25;

            // CheckSkill call modified to lower max to 100.
            if (m.Skills[SkillName.MagicResist].Value < maxSkill)
                m.CheckSkill(SkillName.MagicResist, 0.0, 100.0, contextObj: new object[2]);

            return m.Skills[SkillName.MagicResist].Value;
        }


        public double GetResistScaler(double newScalar)
        {
            return (SpellHelper.OldResist ? 0.5 : newScalar);
        }
        public virtual double GetDamageScalar(Mobile target)
        {
            double casterEI = m_Caster.Skills[DamageSkill].Value;
            double targetRS = target.Skills[SkillName.MagicResist].Value;
            double scalar;

            if (Core.RuleSets.AOSRules())
                targetRS = 0;

            // CheckSkill call modified to lower max to 100.
            m_Caster.CheckSkill(DamageSkill, 0.0, 100.0, contextObj: new object[2]);

            if (casterEI > targetRS)
                scalar = (1.0 + ((casterEI - targetRS) / 500.0));
            else
                scalar = (1.0 + ((casterEI - targetRS) / 200.0));

            // magery damage bonus, -25% at 0 skill, +0% at 100 skill, +5% at 120 skill
            scalar += (m_Caster.Skills[CastSkill].Value - 100.0) / 400.0;

            if (!target.Player && !target.Body.IsHuman && !Core.RuleSets.AOSRules())
                scalar *= 2.0; // Double magery damage to monsters/animals if not AOS

            if (target is BaseCreature)
                ((BaseCreature)target).AlterDamageScalarFrom(m_Caster, ref scalar);

            if (m_Caster is BaseCreature)
                ((BaseCreature)m_Caster).AlterDamageScalarTo(target, ref scalar);

            scalar *= GetSlayerDamageScalar(target);

            target.Region.SpellDamageScalar(m_Caster, target, ref scalar);

            return scalar;
        }

        public virtual double GetSlayerDamageScalar(Mobile defender)
        {
            double scalar = 1.0;

            if (HolyWater.Slays(m_Caster, defender))
            {
                defender.FixedEffect(0x37B9, 10, 5);
                scalar = 2.0;
            }

            if (HolyWater.OppositionSuperSlays(m_Caster, defender))
                scalar = 2.0;

            if (CoveBrew.Slays(m_Caster, defender))
            {
                defender.FixedEffect(0x37B9, 10, 5);
                scalar = 2.0;
            }

            if (CoveBrew.OppositionSuperSlays(m_Caster, defender))
                scalar = 2.0;

            return scalar;
        }

        public virtual void DoFizzle()
        {
            m_Caster.LocalOverheadMessage(MessageType.Regular, 0x3B2, 502632); // The spell fizzles.

            if (m_Caster.Player)
            {
                if (Core.RuleSets.AOSRules())
                    m_Caster.FixedParticles(0x3735, 1, 30, 9503, EffectLayer.Waist);
                else
                    m_Caster.FixedEffect(0x3735, 6, 30);

                m_Caster.PlaySound(0x5C);
            }
        }

        private CastTimer m_CastTimer;
        private AnimTimer m_AnimTimer;

        public void Disturb(DisturbType type)
        {
            Disturb(type, true, false);
        }

        public virtual bool CheckDisturb(DisturbType type, bool firstCircle, bool resistable)
        {
            if (resistable && m_Scroll is IMagicItem)
                return false;

            return true;
        }

        public void Disturb(DisturbType type, bool firstCircle, bool resistable)
        {
            if (!CheckDisturb(type, firstCircle, resistable))
                return;

            if (m_State == SpellState.Casting)
            {
                if (!firstCircle && Circle == SpellCircle.First && !Core.RuleSets.AOSRules())
                    return;

                m_State = SpellState.None;
                m_Caster.Spell = null;

                OnDisturb(type, true);

                if (m_CastTimer != null)
                    m_CastTimer.Stop();

                if (m_AnimTimer != null)
                    m_AnimTimer.Stop();

                // 7/9/2021, Adam: Putting back the spell delay for daggering, reversing the code described below:
                // erl: removed next spell delay for "daggering" and [cancelspell
                // ..
                // line altered -
                // if ( type == DisturbType.EquipRequest || type == DisturbType.Hurt )
                // become -
                // if ( type == DisturbType.Hurt )
                // ..

                //if (type == DisturbType.Hurt)
                if (type == DisturbType.EquipRequest || type == DisturbType.Hurt)
                    m_Caster.NextSpellTime = DateTime.UtcNow + GetDisturbRecovery();
                else
                    TimeSpan.FromSeconds(GetDisturbRecovery().TotalSeconds);

            }
            else if (m_State == SpellState.Sequencing)
            {
                if (!firstCircle && Circle == SpellCircle.First && !Core.RuleSets.AOSRules())
                    return;

                m_State = SpellState.None;
                m_Caster.Spell = null;

                OnDisturb(type, false);

                Targeting.Target.Cancel(m_Caster);

                if (type == DisturbType.EquipRequest || type == DisturbType.Hurt)
                    m_Caster.NextSpellTime = DateTime.UtcNow + GetDisturbRecovery();
                else
                    TimeSpan.FromSeconds(GetDisturbRecovery().TotalSeconds);
            }
        }

        public virtual void DoHurtFizzle()
        {
            m_Caster.FixedEffect(0x3735, 6, 30);
            m_Caster.PlaySound(0x5C);
        }

        public virtual void OnDisturb(DisturbType type, bool message)
        {
            if (message)
                m_Caster.SendLocalizedMessage(500641); // Your concentration is disturbed, thus ruining thy spell.
        }

        public virtual bool CheckCast()
        {
            return true;
        }

        public virtual void SayMantra()
        {
            if (m_Scroll is IMagicItem)
                return;

            if (m_Info.Mantra != null && m_Info.Mantra.Length > 0 && (m_Caster.Player || m_Caster.Body.IsHuman))
                m_Caster.PublicOverheadMessage(MessageType.Spell, m_Caster.SpeechHue, true, m_Info.Mantra, false);
        }

        public virtual bool BlockedByHorrificBeast { get { return true; } }
        public virtual bool BlocksMovement { get { return true; } }
        public virtual bool CheckNextSpellTime { get { return !(m_Scroll is IMagicItem); } }

        public bool Cast()
        {
            m_StartCastTime = DateTime.UtcNow;

            if (Core.RuleSets.AOSRules() && m_Caster.Spell is Spell && ((Spell)m_Caster.Spell).State == SpellState.Sequencing)
                ((Spell)m_Caster.Spell).Disturb(DisturbType.NewCast);

            if (!m_Caster.CheckAlive())
            {
                return false;
            }
            else if (m_Caster.Spell != null && m_Caster.Spell.IsCasting)
            {
                m_Caster.SendLocalizedMessage(502642); // You are already casting a spell.
            }
            //else if ( BlockedByHorrificBeast && TransformationSpell.UnderTransformation( m_Caster, typeof( HorrificBeastSpell ) ) )
            //{
            //	m_Caster.SendLocalizedMessage( 1061091 ); // You cannot cast that spell in this form.
            //}
            else if (!(m_Scroll is IMagicItem) && (m_Caster.Paralyzed || m_Caster.Frozen))
            {
                m_Caster.SendLocalizedMessage(502643); // You can not cast a spell while frozen.
            }
            //else if ( CheckNextSpellTime && DateTime.UtcNow < m_Caster.NextSpellTime )
            else if (DateTime.UtcNow < m_Caster.NextSpellTime)
            {
                m_Caster.SendLocalizedMessage(502644); // You must wait for that spell to have an effect.
            }
            else if (MLDryad.UnderEffect(m_Caster))
            {
                m_Caster.SendLocalizedMessage(1072060); // You cannot cast a spell while calmed.
            }
            #region Dueling
            else if (m_Caster is PlayerMobile && ((PlayerMobile)m_Caster).DuelContext != null && !((PlayerMobile)m_Caster).DuelContext.AllowSpellCast(m_Caster, this))
            {
            }
            #endregion
            else if (m_Caster.Mana >= ScaleMana(GetMana()))
            {
                if (m_Caster.Spell == null && m_Caster.CheckSpellCast(this) && CheckCast() && m_Caster.Region.OnBeginSpellCast(m_Caster, this))
                {
                    m_State = SpellState.Casting;
                    m_Caster.Spell = this;

                    if (RevealOnCast)
                        m_Caster.RevealingAction();

                    SayMantra();

                    TimeSpan castDelay = this.GetCastDelay();
                    NextSpellDelay = TimeSpan.FromMilliseconds(CoreAI.SpellDefaultDelay);

                    // -- casting speed --
                    // ---- Individual spell timing alterations ----

                    // For FC ----
                    // adam: reverting MB to standard 5th circle casting speed. 
                    //	see damage-scale  modifications to the spell
                    //if ( m_Info.Name == "Mind Blast" )
                    //castDelay = this.GetCastDelay() + TimeSpan.FromSeconds( 0.45 );

                    // lib: refactored to switch/case
                    //if (m_Info.Name == "Cure")
                    //    castDelay = this.GetCastDelay() + TimeSpan.FromSeconds(0.02);

                    switch (m_Info.Name)
                    {
                        case "Cure":
                            castDelay += TimeSpan.FromMilliseconds(CoreAI.SpellAddlCastDelayCure);
                            break;
                        case "Heal":
                            NextSpellDelay = TimeSpan.FromMilliseconds(CoreAI.SpellHealRecoveryOverride);
                            break;
                    }

                    // lib: refactored to switch/case and default value
                    // For FCR ----
                    //if (m_Info.Name == "Heal")
                    //    NextSpellDelay = TimeSpan.FromSeconds(0.95);
                    //else
                    //    NextSpellDelay = TimeSpan.FromSeconds(0.80);
                    // ----

                    if (m_Caster.Body.IsHuman)
                    {
                        int count = (int)Math.Ceiling(castDelay.TotalSeconds / AnimateDelay.TotalSeconds);

                        if (count != 0)
                        {
                            m_AnimTimer = new AnimTimer(this, count);
                            m_AnimTimer.Start();
                        }

                        if (m_Info.LeftHandEffect > 0)
                            Caster.FixedParticles(0, 10, 5, m_Info.LeftHandEffect, EffectLayer.LeftHand);

                        if (m_Info.RightHandEffect > 0)
                            Caster.FixedParticles(0, 10, 5, m_Info.RightHandEffect, EffectLayer.RightHand);
                    }

                    if (ClearHandsOnCast)
                    {
                        m_Caster.ClearHands();

                        if (m_Scroll == null && EquipSpellbookOnCast)
                        {
                            Item item = m_Caster.FindItemOnLayer(Layer.OneHanded);
                            if (item is Spellbook == false)
                            {
                                //m_Caster.SendMessage("Equipping spell book.");
                                if (m_Caster.Backpack != null)
                                {
                                    Item spellbook = m_Caster.Backpack.FindItemByType(typeof(Spellbook), false);
                                    if (spellbook != null)
                                        m_Caster.EquipItem(spellbook);
                                }
                            }
                        }
                    }

                    m_CastTimer = new CastTimer(this, castDelay);

                    if (castDelay == TimeSpan.Zero && m_Scroll is IMagicItem)
                        m_CastTimer.DoTick();
                    else
                        m_CastTimer.Start();

                    OnBeginCast();

                    // If the caster is a staffmember, log the spellcasting.
                    if (m_Caster.AccessLevel > AccessLevel.Player)
                        Server.Commands.CommandLogging.LogCastSpell(m_Caster, this.Name);

                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                m_Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625); // Insufficient mana
            }

            return false;
        }

        public abstract void OnCast();

        public virtual void OnBeginCast()
        {
        }

        private const double ChanceOffset = 20.0, ChanceLength = 100.0 / 7.0;

        public virtual void GetCastSkills(out double min, out double max)
        {
            int circle = (int)m_Info.Circle;

            if (m_Scroll != null)
                circle -= 2;

            if (circle < 0)
                circle = 0;

            min = 1.1; max = 40.1;
            switch (circle)
            {
                case 0: min = 1.1; max = 40.1; break;
                case 1: min = 6.1; max = 50.1; break;
                case 2: min = 16.1; max = 60.1; break;
                case 3: min = 26.1; max = 70.1; break;
                case 4: min = 36.1; max = 80.1; break;
                case 5: min = 51.8; max = 90.1; break;
                case 6: min = 66.1; max = 105.1; break;
                case 7: min = 80.1; max = 120.1; break;
            }

            //double avg = ChanceLength * circle;

            //min = avg - ChanceOffset;
            //max = avg + ChanceOffset;
        }

        // I inverted the way this works from RunUO.
        // RunUO had it so that if CheckFizzle() returned TRUE you DIDN'T fizzle :\
        // Now CheckFizzle() returns TRUE is you fizzle
        public virtual bool CheckFizzle()
        {
            if (m_Scroll is IMagicItem)
                return false;

            // when the spellbook is required for the cast (publish 5) and it's dropped durring the cast, fizzle
            if (m_Caster != null && m_Caster.Player)
                if (ClearHandsOnCast && m_Scroll == null && EquipSpellbookOnCast && m_Caster.FindItemOnLayer(Layer.OneHanded) is Spellbook == false)
                    return true;

            double minSkill, maxSkill;

            GetCastSkills(out minSkill, out maxSkill);

            return !Caster.CheckSkill(CastSkill, minSkill, maxSkill, contextObj: new object[2]);
        }

        private static int[] m_ManaTable = new int[] { 4, 6, 9, 11, 14, 20, 40, 50 };

        public virtual int GetMana()
        {
            if (m_Scroll is IMagicItem)
                return 0;

            return m_ManaTable[(int)Circle];
        }

        public virtual int ScaleMana(int mana)
        {
            double scalar = 1.0;

            //if ( !Necromancy.MindRotSpell.GetMindRotScalar( Caster, ref scalar ) )
            //scalar = 1.0;

            scalar -= (double)AosAttributes.GetValue(m_Caster, AosAttribute.LowerManaCost) / 100;

            return (int)(mana * scalar);
        }

        public virtual TimeSpan GetDisturbRecovery()
        {
            if (Core.RuleSets.AOSRules())
                return TimeSpan.Zero;

            double delay;
            var elapsed = (DateTime.UtcNow - m_StartCastTime).TotalSeconds;
            if (CoreAI.SpellUseRuoRecoveries)
            {
                // default (latest) RunUO formula
                delay = CoreAI.SpellRuoRecoveryBase - Math.Sqrt(elapsed / GetCastDelay().TotalSeconds);
            }
            else
            {
                // historical AI formula
                delay = GetCastDelay().TotalSeconds - elapsed;
            }

            if (delay < CoreAI.SpellRecoveryMin)
                delay = CoreAI.SpellRecoveryMin;

            return TimeSpan.FromSeconds(delay);
        }

        public virtual int CastRecoveryBase { get { return 6; } }

        //orig		public virtual int CastRecoveryCircleScalar{ get{ return 0; } }
        public virtual int CastRecoveryCircleScalar { get { return 1; } }

        public virtual int CastRecoveryFastScalar { get { return 1; } }
        public virtual int CastRecoveryPerSecond { get { return 4; } }
        public virtual int CastRecoveryMinimum { get { return 0; } }

        public virtual TimeSpan GetCastRecovery()
        {
            return NextSpellDelay;
        }

        public virtual int CastDelayBase { get { return 3; } }
        public virtual int CastDelayCircleScalar { get { return 1; } }
        public virtual int CastDelayFastScalar { get { return 1; } }
        public virtual int CastDelayPerSecond { get { return 4; } }
        public virtual int CastDelayMinimum { get { return 1; } }

        public virtual TimeSpan GetCastDelay()
        {
            if (m_Scroll is IMagicItem)
                return TimeSpan.Zero;

            if (!Core.RuleSets.AOSRules())
            {
                return TimeSpan.FromSeconds(CoreAI.SpellCastDelayConstant + (CoreAI.SpellCastDelayCoeff * (int)Circle));
            }

            int fc = AosAttributes.GetValue(m_Caster, AosAttribute.CastSpeed);

            if (ProtectionSpell.Registry.Contains(m_Caster))
                fc -= 2;

            int circleDelay = CastDelayCircleScalar * (1 + (int)Circle); // Note: Circle is 0-based so we must offset
            int fcDelay = -(CastDelayFastScalar * fc);

            int delay = CastDelayBase + circleDelay - 2; // sets FC -2

            if (delay < CastDelayMinimum)
                delay = CastDelayMinimum;

            return TimeSpan.FromSeconds((double)delay / CastDelayPerSecond);
        }

        public virtual void FinishSequence()
        {
            m_State = SpellState.None;

            if (m_Caster.Spell == this)
                m_Caster.Spell = null;

            if (m_Caster is PlayerMobile)
                m_Caster.OnAfterUseSpell(this.GetType());
        }

        public virtual int ComputeKarmaAward()
        {
            return 0;
        }

        public virtual bool CheckSequence()
        {
            #region Static Region
            if (m_Caster != null && m_Caster.Spell != null)
            {
                StaticRegion sr = StaticRegion.FindStaticRegion(m_Caster);
                if (sr != null && sr.IsRestrictedSpell(m_Caster.Spell) && ((m_Caster is BaseCreature && !sr.RestrictCreatureMagic) || m_Caster is PlayerMobile) && m_Caster.AccessLevel == AccessLevel.Player)
                {
                    m_State = SpellState.None;
                    if (m_Caster.Spell == this)
                        m_Caster.Spell = null;
                    Target.Cancel(m_Caster);
                    if (sr.RestrictedMagicMessage == null)
                        m_Caster.SendMessage("You cannot cast that spell here.");
                    else
                        m_Caster.SendMessage(sr.RestrictedMagicMessage);
                }
            }
            #endregion

            int mana = ScaleMana(GetMana());

            if (m_Caster.Deleted || !m_Caster.Alive || m_Caster.Spell != this || m_State != SpellState.Sequencing)
            {
                DoFizzle();
            }
            else if (m_Scroll != null && !(m_Scroll is Runebook) && (m_Scroll.Amount <= 0 || m_Scroll.Deleted || (m_Scroll.Layer != Layer.Invalid && m_Scroll.RootParent != m_Caster) || (m_Scroll is IMagicItem && (((IMagicItem)m_Scroll).MagicCharges <= 0 || (m_Scroll.Layer != Layer.Invalid && m_Scroll.Parent != m_Caster)))))
            {
                DoFizzle();
            }
            else if (!ConsumeReagents())
            {
                m_Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502630); // More reagents are needed for this spell.
            }
            else if (m_Caster.Mana < mana)
            {
                m_Caster.LocalOverheadMessage(MessageType.Regular, 0x22, 502625); // Insufficient mana for this spell.
            }
            else if (Core.RuleSets.AOSRules() && (m_Caster.Frozen || m_Caster.Paralyzed))
            {
                m_Caster.SendLocalizedMessage(502646); // You cannot cast a spell while frozen.
                DoFizzle();
            }
            // NPCs don't fizzle
            else if (!CheckFizzle() || SpellHelper.IsNPC(Caster))
            {
                m_Caster.Mana -= mana;

                if (m_Scroll is SpellScroll)
                    m_Scroll.Consume();
                else if (m_Scroll is IMagicItem)
                    MagicItems.ConsumeCharge(m_Caster, m_Scroll);

                if (m_Scroll is IMagicItem)
                {
                    bool m = m_Scroll.Movable;

                    m_Scroll.Movable = false;

                    if (ClearHandsOnCast)
                    {
                        m_Caster.ClearHands();
                    }

                    m_Scroll.Movable = m;
                }
                else
                {
                    if (ClearHandsOnCast)
                    {
                        m_Caster.ClearHands();
                    }
                }

                int karma = ComputeKarmaAward();

                if (karma != 0)
                    Misc.Titles.AwardKarma(Caster, karma, true);

                /*
				if ( TransformationSpell.UnderTransformation( m_Caster, typeof( VampiricEmbraceSpell ) ) )
				{
					bool garlic = false;

					for ( int i = 0; !garlic && i < m_Info.Reagents.Length; ++i )
						garlic = ( m_Info.Reagents[i] == Reagent.Garlic );

					if ( garlic )
					{
						m_Caster.SendLocalizedMessage( 1061651 ); // The garlic burns you!
						AOS.Damage( m_Caster, Utility.RandomMinMax( 17, 23 ), 100, 0, 0, 0, 0 );
					}
				}
				*/

                return true;
            }
            else
            {
                DoFizzle();
            }

            return false;
        }

        public bool CheckBSequence(Mobile target)
        {
            return CheckBSequence(target, false);
        }

        public bool CheckBSequence(Mobile target, bool allowDead)
        {
            #region Static Region
            if (m_Caster != null && m_Caster.Spell != null && target != null && m_Caster.Region != target.Region)
            {
                StaticRegion sr = StaticRegion.FindStaticRegion(target);
                if (sr != null && sr.IsRestrictedSpell(m_Caster.Spell) && ((m_Caster is BaseCreature && !sr.RestrictCreatureMagic) || m_Caster is PlayerMobile) && m_Caster.AccessLevel == AccessLevel.Player)
                {
                    m_State = SpellState.None;
                    if (m_Caster.Spell == this)
                        m_Caster.Spell = null;
                    Target.Cancel(m_Caster);
                    if (sr.RestrictedMagicMessage == null)
                        m_Caster.SendMessage("You cannot cast that spell here.");
                    else
                        m_Caster.SendMessage(sr.RestrictedMagicMessage);
                }
            }
            #endregion

            if (!target.Alive && !allowDead)
            {
                m_Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return false;
            }
            else if (Caster.CanBeBeneficial(target, true, allowDead) && CheckSequence())
            {
                Caster.DoBeneficial(target);
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool CheckHSequence(Mobile target)
        {
            #region Static Region
            if (m_Caster != null && m_Caster.Spell != null && target != null && m_Caster.Region != target.Region)
            {
                StaticRegion sr = StaticRegion.FindStaticRegion(target);
                if (sr != null && sr.IsRestrictedSpell(m_Caster.Spell) && ((m_Caster is BaseCreature && !sr.RestrictCreatureMagic) || m_Caster is PlayerMobile) && m_Caster.AccessLevel == AccessLevel.Player)
                {
                    m_State = SpellState.None;
                    if (m_Caster.Spell == this)
                        m_Caster.Spell = null;
                    Target.Cancel(m_Caster);
                    if (sr.RestrictedMagicMessage == null)
                        m_Caster.SendMessage("You cannot cast that spell here.");
                    else
                        m_Caster.SendMessage(sr.RestrictedMagicMessage);
                }
            }
            #endregion

            if (!target.Alive)
            {
                m_Caster.SendLocalizedMessage(501857); // This spell won't work on that!
                return false;
            }
            else if (Caster.CanBeHarmful(target) && CheckSequence())
            {
                Caster.DoHarmful(target);
                return true;
            }
            else
            {
                return false;
            }
        }

        private class AnimTimer : Timer
        {
            private Spell m_Spell;

            public AnimTimer(Spell spell, int count)
                : base(TimeSpan.Zero, AnimateDelay, count)
            {
                m_Spell = spell;

                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                if (m_Spell.State != SpellState.Casting || m_Spell.m_Caster.Spell != m_Spell)
                {
                    Stop();
                    return;
                }

                if (!m_Spell.Caster.Mounted && m_Spell.Caster.Body.IsHuman && m_Spell.m_Info.Action >= 0)
                    m_Spell.Caster.Animate(m_Spell.m_Info.Action, 7, 1, true, false, 0);

                if (!Running)
                    m_Spell.m_AnimTimer = null;
            }
        }

        private class CastTimer : Timer
        {
            private Spell m_Spell;

            public CastTimer(Spell spell, TimeSpan castDelay)
                : base(castDelay)
            {
                m_Spell = spell;

                Priority = TimerPriority.TwentyFiveMS;
            }

            // Yoar: Hacky, but I need the cast timer to tick instantly for magic items
            public void DoTick()
            {
                OnTick();
                Stop();
            }

            protected override void OnTick()
            {
                if (m_Spell.m_State == SpellState.Casting && m_Spell.m_Caster.Spell == m_Spell)
                {
                    m_Spell.m_State = SpellState.Sequencing;
                    m_Spell.m_CastTimer = null;
                    m_Spell.m_Caster.OnSpellCast(m_Spell);
                    m_Spell.m_Caster.Region.OnSpellCast(m_Spell.m_Caster, m_Spell);
                    m_Spell.m_Caster.NextSpellTime = DateTime.UtcNow + m_Spell.GetCastRecovery();// Spell.NextSpellDelay;

                    Target originalTarget = m_Spell.m_Caster.Target;

                    m_Spell.OnCast();

                    if (m_Spell.m_Caster.Player && m_Spell.m_Caster.Target != originalTarget && m_Spell.Caster.Target != null)
                        m_Spell.m_Caster.Target.BeginTimeout(m_Spell.m_Caster, TimeSpan.FromSeconds(30.0));

                    m_Spell.m_CastTimer = null;
                }
            }
        }
    }
}