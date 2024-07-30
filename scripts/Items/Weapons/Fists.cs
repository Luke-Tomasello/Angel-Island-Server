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

/* Scripts\Items\Weapons\Fists.cs
 * ChangeLog:
 *  5/6/23, Yoar
 *      Conditioned defensive wrestling for Pub16
 *      https://wiki.stratics.com/index.php?title=UO:Publish_Notes_from_2002-07-12_Siege_Perilous_Ruleset_Changes
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	6/8/10, adam
 *		make the class 'sealed' to prevent basing classes on this class since there is special cleanup logic
 *		associated with fists.
 *  1/06.06, Kit
 *		Adjusted DisarmEventSink to work with mobiles and not just players.
 *	7/19/04 smerX
 *		revised message for toggling abilities with quarterstaff.
 *  5/29/04 Pixie
 *		Changed so that anat+eval+20/2 has a max of 100 instead of 120.
	4/26/04 smerX
		Can no longer enable special ability with SpellState.Sequencing
	4/25/04 changes by smerX
		Added toggler for weapon abilities.
*/

using Server.Network;
using Server.Spells;
using System;

namespace Server.Items
{
    public sealed class Fists : BaseMeleeWeapon
    {
        public static void Initialize()
        {
            Mobile.DefaultWeapon = new Fists();

            EventSink.DisarmRequest += new DisarmRequestEventHandler(EventSink_DisarmRequest);
            EventSink.StunRequest += new StunRequestEventHandler(EventSink_StunRequest);
        }

        //		public override int AosStrengthReq{ get{ return 0; } }
        //		public override int AosMinDamage{ get{ return 1; } }
        //		public override int AosMaxDamage{ get{ return 4; } }
        //		public override int AosSpeed{ get{ return 50; } }
        //
        public override int OldMinDamage { get { return 1; } }
        public override int OldMaxDamage { get { return 8; } }
        public override int OldStrengthReq { get { return 0; } }
        public override int OldSpeed { get { return 30; } }

        public override int OldDieRolls { get { return 1; } }
        public override int OldDieMax { get { return 8; } }
        public override int OldAddConstant { get { return 0; } }

        public override int DefHitSound { get { return -1; } }
        public override int DefMissSound { get { return -1; } }

        public override SkillName DefSkill { get { return SkillName.Wrestling; } }
        public override WeaponType DefType { get { return WeaponType.Fists; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Wrestle; } }

        public Fists()
            : base(0)
        {
            Visible = false;
            Movable = false;
            Quality = WeaponQuality.Regular;
        }

        public Fists(Serial serial)
            : base(serial)
        {
        }

        public override double GetDefendSkillValue(Mobile attacker, Mobile defender)
        {
            double wresValue = defender.Skills[SkillName.Wrestling].Value;

            double incrValue = 0;

            // 5/6/23, Yoar: Conditioned defensive wrestling for Pub16
            if (PublishInfo.Publish >= 16.0 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
            {
                double anatValue = defender.Skills[SkillName.Anatomy].Value;
                double evalValue = defender.Skills[SkillName.EvalInt].Value;
                incrValue = (anatValue + evalValue + 20.0) * 0.5;

                //Pix: this should be based off 100 skillcap, not 120
                if (incrValue > 100.0)//120.0 )
                    incrValue = 100.0;//120.0;
            }

            if (wresValue > incrValue)
                return wresValue;
            else
                return incrValue;
        }

        // default: 15
        private int StamCost { get { return Server.Items.Consoles.StunPunchMCi.StamCost; } }
        // default: true
        private bool StamCostAlways { get { return Server.Items.Consoles.StunPunchMCi.StamCostAlways; } }
        // default: 4.0
        private double FreezeTime { get { return Server.Items.Consoles.StunPunchMCi.FreezeTime; } }

        public override TimeSpan OnSwing(Mobile attacker, Mobile defender)
        {
            if (attacker.StunReady)
            {
                if (attacker.CanBeginAction(typeof(Fists)))
                {
                    if (attacker.Skills[SkillName.Anatomy].Value >= 80.0 && attacker.Skills[SkillName.Wrestling].Value >= 80.0)
                    {
                        if (attacker.Stam >= StamCost)
                        {
                            if (StamCostAlways == true)
                                attacker.Stam -= StamCost;

                            if (CheckMove(attacker, SkillName.Anatomy))
                            {
                                if (StamCostAlways == false)
                                    attacker.Stam -= StamCost;

                                StartMoveDelay(attacker);

                                attacker.StunReady = false;

                                attacker.SendLocalizedMessage(1004013); // You successfully stun your opponent!
                                defender.SendLocalizedMessage(1004014); // You have been stunned!

                                defender.Freeze(TimeSpan.FromSeconds(FreezeTime));
                            }
                            else
                            {
                                attacker.SendLocalizedMessage(1004010); // You failed in your attempt to stun.
                                defender.SendLocalizedMessage(1004011); // Your opponent tried to stun you and failed.
                            }
                        }
                        else
                        {
                            attacker.SendLocalizedMessage(1004009); // You are too fatigued to attempt anything.
                        }
                    }
                    else
                    {
                        attacker.SendLocalizedMessage(1004008); // You are not skilled enough to stun your opponent.
                        attacker.StunReady = false;
                    }
                }
            }
            else if (attacker.DisarmReady)
            {

                if (attacker.CanBeginAction(typeof(Fists)))
                {
                    if (defender.Player || defender.Body.IsHuman)
                    {
                        if (attacker.Skills[SkillName.ArmsLore].Value >= 80.0 && attacker.Skills[SkillName.Wrestling].Value >= 80.0)
                        {
                            if (attacker.Stam >= 15)
                            {
                                Item toDisarm = defender.FindItemOnLayer(Layer.OneHanded);

                                if (toDisarm == null || !toDisarm.Movable)
                                    toDisarm = defender.FindItemOnLayer(Layer.TwoHanded);

                                Container pack = defender.Backpack;

                                if (pack == null || toDisarm == null || !toDisarm.Movable)
                                {
                                    attacker.SendLocalizedMessage(1004001); // You cannot disarm your opponent.
                                }
                                else if (CheckMove(attacker, SkillName.ArmsLore))
                                {
                                    StartMoveDelay(attacker);

                                    attacker.Stam -= 15;
                                    attacker.DisarmReady = false;

                                    attacker.SendLocalizedMessage(1004006); // You successfully disarm your opponent!
                                    defender.SendLocalizedMessage(1004007); // You have been disarmed!

                                    pack.DropItem(toDisarm);
                                }
                                else
                                {
                                    attacker.Stam -= 15;

                                    attacker.SendLocalizedMessage(1004004); // You failed in your attempt to disarm.
                                    defender.SendLocalizedMessage(1004005); // Your opponent tried to disarm you but failed.
                                }
                            }
                            else
                            {
                                attacker.SendLocalizedMessage(1004003); // You are too fatigued to attempt anything.
                            }
                        }
                        else
                        {
                            attacker.SendLocalizedMessage(1004002); // You are not skilled enough to disarm your opponent.
                            attacker.DisarmReady = false;
                        }
                    }
                    else
                    {
                        attacker.SendLocalizedMessage(1004001); // You cannot disarm your opponent.
                    }
                }
            }

            return base.OnSwing(attacker, defender);
        }

        public override void OnMiss(Mobile attacker, Mobile defender)
        {
            base.PlaySwingAnimation(attacker);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            Delete();
        }

        #region Wrestling moves
        private static bool CheckMove(Mobile m, SkillName other)
        {
            double wresValue = m.Skills[SkillName.Wrestling].Value;
            double scndValue = m.Skills[other].Value;

            /* 40% chance at 80, 80
			 * 50% chance at 100, 100
			 * 60% chance at 120, 120
			 */

            double chance = (wresValue + scndValue) / 400.0;

            return (chance >= Utility.RandomDouble());
        }

        public static bool HasFreeHands(Mobile m)
        {
            Item item = m.FindItemOnLayer(Layer.OneHanded);

            if (item != null && !(item is Spellbook))
                return false;

            return m.FindItemOnLayer(Layer.TwoHanded) == null;
        }

        private static void EventSink_DisarmRequest(DisarmRequestEventArgs e)  // special abilities
        {

            Mobile m = e.Mobile;
            Item weapon = m.FindItemOnLayer(Layer.TwoHanded);
            double anat = m.Skills[SkillName.Anatomy].Value;
            double weapSkill = 0;

            if (HasFreeHands(m)) // primary ability for wres (disarm)
            {
                #region Dueling
                if (!Engines.ConPVP.DuelContext.AllowSpecialAbility(m, "Disarm", true))
                    return;
                #endregion

                double armsValue = m.Skills[SkillName.ArmsLore].Value;
                double wresValue = m.Skills[SkillName.Wrestling].Value;

                if (armsValue >= 80.0 && wresValue >= 80.0)
                {
                    m.DisruptiveAction();
                    m.DisarmReady = !m.DisarmReady;
                    m.SendLocalizedMessage(m.DisarmReady ? 1019013 : 1019014);
                }
                else
                {
                    m.SendLocalizedMessage(1004002); // You are not skilled enough to disarm your opponent.
                    m.DisarmReady = false;
                }

                return;

            }
            else if (weapon is BaseBashing)
            {
                weapSkill = m.Skills[SkillName.Macing].Value;
            }
            else if (weapon is BasePoleArm || weapon is BaseAxe)
            {
                weapSkill = m.Skills[SkillName.Swords].Value;
            }
            else if (weapon is BaseSpear)
            {
                weapSkill = m.Skills[SkillName.Fencing].Value;
            }
            else if (weapon is BaseRanged || weapon is BaseStaff)
            {
                m.HasAbilityReady = false;
                if (Core.RuleSets.UseToggleSpecialAbility())
                    m.SendMessage("You can not think of a special technique for this weapon.");
                return;
            }
            else
            {
                m.HasAbilityReady = false;
                if (Core.RuleSets.UseToggleSpecialAbility())
                    m.SendMessage("You must equip a two-handed weapon.");
                return;
            }


            if (weapSkill >= 80 && anat >= 80)
            {
                if (!m.HasAbilityReady && m.Mana < 15) // if they're toggling "on" and have < 15 mana
                {
                    if (Core.RuleSets.UseToggleSpecialAbility())
                        m.LocalOverheadMessage(MessageType.Emote, 0x3B2, false, "Insufficient mana.");
                    m.HasAbilityReady = false;
                    return;
                }


                ISpell i = m.Spell;
                Spell s = (Spell)i;

                if (!m.HasAbilityReady && i != null && s.State == SpellState.Sequencing)
                {
                    s.Disturb(DisturbType.EquipRequest, true, false);
                    if (Core.RuleSets.UseToggleSpecialAbility())
                        m.SendMessage("You break your concentration to ready a technique.");
                    m.FixedEffect(0x3735, 6, 30);
                    m.HasAbilityReady = true;
                    return;
                }

                if (m.NextAbilityTime > DateTime.UtcNow)
                {
                    if (Core.RuleSets.UseToggleSpecialAbility())
                        m.SendMessage("You must wait to perform a special ability");
                    m.HasAbilityReady = false;
                    return;
                }

                m.HasAbilityReady = !m.HasAbilityReady; // toggle ability flag "on" or "off"
                if (Core.RuleSets.UseToggleSpecialAbility())
                    m.SendMessage(m.HasAbilityReady ? "You get ready to use an advanced technique." : "You decide to save yourself the effort.");
            }
            else if (!HasFreeHands(m)) // last problem is skill being too low, sets false and tells player so
            {
                m.HasAbilityReady = false;
                if (Core.RuleSets.UseToggleSpecialAbility())
                    m.SendMessage("You aren't confident enough to attempt that.");
            }

        }

        private static void EventSink_StunRequest(StunRequestEventArgs e) // secondary ability for wres (stun)
        {
            Mobile m = e.Mobile;

            #region Dueling
            if (!Engines.ConPVP.DuelContext.AllowSpecialAbility(m, "Stun", true))
                return;
            #endregion

            double anatValue = m.Skills[SkillName.Anatomy].Value;
            double wresValue = m.Skills[SkillName.Wrestling].Value;

            if (!HasFreeHands(m))
            {
                m.SendLocalizedMessage(1004031); // You must have your hands free to attempt to stun your opponent.
                m.StunReady = false;
            }
            else if (anatValue >= 80.0 && wresValue >= 80.0)
            {
                m.DisruptiveAction();
                m.StunReady = !m.StunReady;
                m.SendLocalizedMessage(m.StunReady ? 1019011 : 1019012);
            }
            else
            {
                m.SendLocalizedMessage(1004008); // You are not skilled enough to stun your opponent.
                m.StunReady = false;
            }
        }

        private class MoveDelayTimer : Timer
        {
            private Mobile m_Mobile;

            public MoveDelayTimer(Mobile m)
                : base(TimeSpan.FromSeconds(10.0))
            {
                m_Mobile = m;

                Priority = TimerPriority.TwoFiftyMS;

                m_Mobile.BeginAction(typeof(Fists));
            }

            protected override void OnTick()
            {
                m_Mobile.EndAction(typeof(Fists));
            }
        }

        private static void StartMoveDelay(Mobile m)
        {
            new MoveDelayTimer(m).Start();
        }
        #endregion Wrestling moves
    }
}