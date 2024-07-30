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

/* Scripts/Engines/AngelIsland/AILevelSystem/Mobiles/AngelofJustice.cs
 * ChangeLog
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	7/15/07, Adam
 *		- Update mob's STR and not hits. Updating hits 'heals' the creature, and we don't want that
 *			Basically, all players that attack the mob will increase it's STR
 *	6/15/06, Adam
 *		- Move dynamic threat stuff into common base class BaseDynamicThreat
 *	4/8/05, Adam
 *		add the VirtualArmor to the CoreAI global variables and make setable
 *		withing the CoreManagementConsole
 *	9/26/05, Adam
 *		More rebalancing of stats and skills
 *		Normalize with their assigned mob equivalents (pixie, orcish mage, lich, meer eternal)
 *	9/25/05, Adam
 *		Basic rebalancing of stats and skills
 *	9/16/04, Adam
 *		Minor tweaks to the AttackSkill calc.
 *	9/15/04, Adam
 *		Totally redesign the way stats and skills are calculated based on "Threat Analysis"
 *	9/11/04, Adam
 *		Remove gold from corpse
 *		Remove Treasure Map
 *	5/10/04, mith
 *		Modified the way we set this mob's hitpoints.
 *  4/29/04, mith
 *		Modified to use variables in CoreAI.
 */

using System;
using System.Collections;

namespace Server.Mobiles
{
    [CorpseName("an angel's corpse")]
    public class AngelofJustice : BaseDynamicThreat
    {
        [Constructable]
        public AngelofJustice()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Weakest, 10, 1, 0.2, 0.4)
        {
            Name = "Angel of Justice";
            Body = BodyGraphic();
            Hue = 0x481;
            BardImmune = true;
            BaseHits = CoreAI.SpiritBossHP;
            BaseVirtualArmor = CoreAI.SpiritBossVirtualArmor;

            Fame = 0;
            Karma = 0;

            m_NextAbilityTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(2, 5));

            InitStats(BaseHits, BaseVirtualArmor);
        }
        public int BodyGraphic()
        {
            if (Core.T2A)
                return 4; //  Gargoyle (T2A doesn't have a meer graphic)
            else
                return 123; // meer eternal (LBR enabled)

            return 0;
        }
        public override void InitStats(int iHits, int iVirtualArmor)
        {
            // MEER ETERNAL - Stats
            // Adam: Setting Str and not hits makes hits and str equiv
            //	Don't set hits as it 'heals' the mob, we are instead increasing STR 
            //	which will bump hits too
            //SetStr( 416, 505 );
            SetStr(iHits);
            SetDex(146, 165);
            SetInt(566, 655);
            //SetHits(BaseHits);
            SetDamage(11, 13);

            SetSkill(SkillName.EvalInt, 90.1, 100.0);
            SetSkill(SkillName.Magery, 90.1, 100.0);
            SetSkill(SkillName.Meditation, 90.1, 100.0);
            SetSkill(SkillName.MagicResist, 150.5, 200.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 60.1, 80.0);

            VirtualArmor = iVirtualArmor;
        }

        public override bool InitialInnocent { get { return true; } }

        public override int GetHurtSound()
        {
            return 0x167;
        }

        public override int GetDeathSound()
        {
            return 0xBC;
        }

        public override int GetAttackSound()
        {
            return 0x28B;
        }

        private DateTime m_NextAbilityTime;

        private void DoAreaLeech()
        {
            m_NextAbilityTime += TimeSpan.FromSeconds(2.5);

            this.Say(true, "Beware, mortals!  You have provoked my wrath!");
            this.FixedParticles(0x376A, 10, 10, 9537, 33, 0, EffectLayer.Waist);

            Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(DoAreaLeech_Finish));
        }

        private void DoAreaLeech_Finish()
        {
            ArrayList list = new ArrayList();

            IPooledEnumerable eable = this.GetMobilesInRange(6);
            foreach (Mobile m in eable)
            {   // ignore hidden staff
                if (this.CanBeHarmful(m) && this.IsEnemy(m, RelationshipFilter.None) && !(base.IsStaff(m) && m.Hidden))
                    list.Add(m);
            }
            eable.Free();

            if (list.Count == 0)
            {
                this.Say(true, "Thou art but leaping back from the inevitable, mortal!");
            }
            else
            {
                double scalar;

                if (list.Count == 1)
                    scalar = 0.75;
                else if (list.Count == 2)
                    scalar = 0.50;
                else
                    scalar = 0.25;

                for (int i = 0; i < list.Count; ++i)
                {
                    Mobile m = (Mobile)list[i];

                    int damage = (int)(m.Hits * scalar);

                    damage += Utility.RandomMinMax(-5, 5);

                    if (damage < 1)
                        damage = 1;

                    m.MovingParticles(this, 0x36F4, 1, 0, false, false, 32, 0, 9535, 1, 0, (EffectLayer)255, 0x100);
                    m.MovingParticles(this, 0x0001, 1, 0, false, true, 32, 0, 9535, 9536, 0, (EffectLayer)255, 0);

                    this.DoHarmful(m);
                    this.Hits += AOS.Damage(m, this, damage, 100, 0, 0, 0, 0, this);
                }

                this.Say(true, "If I cannot cleanse thy soul, I will destroy it!");
            }
        }

        private void DoFocusedLeech(Mobile combatant, string message)
        {
            this.Say(true, message);

            Timer.DelayCall(TimeSpan.FromSeconds(0.5), new TimerStateCallback(DoFocusedLeech_Stage1), combatant);
        }

        private void DoFocusedLeech_Stage1(object state)
        {
            Mobile combatant = (Mobile)state;

            if (this.CanBeHarmful(combatant))
            {
                this.MovingParticles(combatant, 0x36FA, 1, 0, false, false, 1108, 0, 9533, 1, 0, (EffectLayer)255, 0x100);
                this.MovingParticles(combatant, 0x0001, 1, 0, false, true, 1108, 0, 9533, 9534, 0, (EffectLayer)255, 0);
                this.PlaySound(0x1FB);

                Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(DoFocusedLeech_Stage2), combatant);
            }
        }

        private void DoFocusedLeech_Stage2(object state)
        {
            Mobile combatant = (Mobile)state;

            if (this.CanBeHarmful(combatant))
            {
                combatant.MovingParticles(this, 0x36F4, 1, 0, false, false, 32, 0, 9535, 1, 0, (EffectLayer)255, 0x100);
                combatant.MovingParticles(this, 0x0001, 1, 0, false, true, 32, 0, 9535, 9536, 0, (EffectLayer)255, 0);

                this.PlaySound(0x209);
                this.DoHarmful(combatant);
                this.Hits += AOS.Damage(combatant, this, Utility.RandomMinMax(30, 40) - (Core.RuleSets.AOSRules() ? 0 : 10), 100, 0, 0, 0, 0, this);
            }
        }

        public override void OnThink()
        {
            if (DateTime.UtcNow >= m_NextAbilityTime)
            {
                Mobile combatant = this.Combatant;

                if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 12))
                {
                    m_NextAbilityTime = DateTime.UtcNow + TimeSpan.FromSeconds(Utility.RandomMinMax(10, 15));

                    int ability = Utility.Random(4);

                    switch (ability)
                    {
                        case 0: DoFocusedLeech(combatant, "Thou art well beyond redemption.."); break;
                        case 1: DoFocusedLeech(combatant, "I rebuke thee, murderer, and cleanse thy vile spirit of its tainted essence!"); break;
                        case 2: DoFocusedLeech(combatant, "I will unite thy very essence with the torment of the slain."); break;
                        case 3: DoAreaLeech(); break;
                            // TODO: Resurrect ability
                    }
                }
            }

            base.OnThink();
        }

        public override bool OnBeforeDeath()
        {
            this.Say("Enjoy your freedom, it shan't last long!");
            return base.OnBeforeDeath();
        }

        public AngelofJustice(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

    }
}