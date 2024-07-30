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

/*
 * /Scripts/Items/Weapons/Ranged/BaseRanged.cs
 * CHANGE LOG:
 *  6/13/23, Yoar
 *      Magic accurate bows now give a archery skill bonus instead of a tactics skill bonus
 *      https://web.archive.org/web/20010801154352fw_/http://uo.stratics.com/content/arms-armor/magicarmsarmor.shtml
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	9/08/21, Yoar
 *		Added auto-stacking of shot ammo that falls to the ground
 *	5/13/06, Pix
 *		If you're shooting with poison, make sure they don't have a spell ready
 *	10/16/05, Pix
 *		Streamlined applied poison code.
 *	9/13/05, erlein
 *		Reverted OnSwing() and OnHit() to original rules.
 *	9/12/05, erlein
 *		Change OnSwing() and OnHit() to utilise new poisoning rules.
 *	7/26/05, Adam
 *		Massive AOS cleanout
 *	12/1/04, Adam
 *		add OnEquip() over ride to handle spell cancel when a bow is equipped
 *		(moved from BaseWeapon)
 *	10/22/2004 - Pixie
 *		Changed the "stand time" before allowing a shot to be 0.5 second (was 1.0 second).
 *   04/30/2004 - Pixie
 *       Added checks for the poisoncloth's poison to be null.  null is a valid value for poison
 *       - it means that it's not poisoned.  But we were using it without making sure it wasn't null.
 *   04/27/2004 - Pulse
 * 		Added a check during the OnSwing event to make sure that the player's backpack
 * 		exists before I try searching for any items in it.  This was causing a crash.
 * 		I also added code to the same event to check for weapon ammo before proceeding
 * 		with the poisoning code.
 * 	04/26/2004 - Pulse
 * 		Added poisoning skill based effect to using poisoned cloth to poison arrows
 * 		basically you have only your Poisoning skill chance to use the same level of
 * 		poison as the rag, otherwise you will use -1 level of poison.  If the rag
 * 		is lesser poisoned, -1 level will mean no poison at all
 *   04/17/2004 - Pulse
 * 		Added check for poison cloth in attackers backpack. If the ranged weapon is not poisoned
 * 		and the poison cloth is present, it checks to see if the cloth has charges
 * 		If the cloth has charges, the bow is poisoned for the length of one round
 * 		and the poison cloth charges are reduced by 1, when it reaches 0 it is destroyed.
 * 		The cloth has a delay property that is checked.  This is an additional delay
 * 		that can be used for balancing if needed.  If the delay > 0 it returns the timespan
 * 		which is the next time a swing event can happen for the ranged weapon.
 * 		Because the ranged weapon is now poisoned, this code will be skipped over and
 * 		the swing/hit process will continue as expected.
 * 		Also added the ability for the defender to be poisoned during the OnHit process
 * 		where there is a 50% chance of poisoning, same as melee weapons.
*/

using Server.Diagnostics;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using System;

namespace Server.Items
{
    public abstract class BaseRanged : BaseMeleeWeapon
    {
        public static bool AutoStackAmmo = true; // enables/disables auto-stacking of shot ammo that falls to the ground

        public abstract int EffectID { get; }
        public abstract Type AmmoType { get; }
        public abstract Item Ammo { get; }

        public override int DefHitSound { get { return 0x234; } }
        public override int DefMissSound { get { return 0x238; } }

        public override SkillName DefSkill { get { return SkillName.Archery; } }
        public override WeaponType DefType { get { return WeaponType.Ranged; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.ShootXBow; } }

        public override SkillName AccuracySkill { get { return SkillName.Archery; } }

        public BaseRanged(int itemID)
            : base(itemID)
        {
        }

        public BaseRanged(Serial serial)
            : base(serial)
        {
        }

        /// <summary>
        /// StandingDelay: denotes the minimum time (in seconds) an archer must stand still
        /// before being able to fire.
        /// </summary>
        //private double StandingDelay = 0.5;

        public override TimeSpan OnSwing(Mobile attacker, Mobile defender)
        {
            Container pack = attacker.Backpack;

            // Make sure we've been standing still for the standing delay (originally was: one second)
            if (DateTime.UtcNow > (attacker.LastMoveTime + TimeSpan.FromSeconds(CoreAI.StandingDelay))
                || (Core.RuleSets.AOSRules() && WeaponAbility.GetCurrentAbility(attacker) is MovingShot))
            {
                bool canSwing = true;

                if (canSwing && MLDryad.UnderEffect(attacker))
                    canSwing = false;

                #region Dueling
                if (attacker is PlayerMobile)
                {
                    PlayerMobile pm = (PlayerMobile)attacker;

                    if (pm.DuelContext != null && !pm.DuelContext.CheckItemEquip(attacker, this))
                        canSwing = false;
                }
                #endregion

                if (canSwing && attacker.HarmfulCheck(defender))
                {
                    // Check if weapon is poisoned
                    if (PoisonCharges == 0)
                    {
                        // Not poisoned
                        // Check to make sure players backpack exists
                        if (pack != null)
                        {
                            // check for arrows/bolts
                            Item Ammo;
                            Ammo = (Item)pack.FindItemByType(AmmoType, true);
                            if (Ammo != null)
                            {
                                if (Core.RuleSets.StandardShardRules())
                                {
                                    // no poisoned arrows on standard shards
                                }
                                else
                                {
                                    // check for poisoned cloth in backpack
                                    PoisonCloth PCloth = (PoisonCloth)pack.FindItemByType(typeof(PoisonCloth), false);
                                    if (PCloth != null)
                                    {
                                        // cloth found
                                        // check for charges
                                        if (PCloth.PoisonCharges > 0)
                                        {
                                            //Pix: 5/13/06 - make sure they don't have a spell ready
                                            Spell s = attacker.Spell as Spell;
                                            if (s != null && s.State == SpellState.Sequencing)
                                            {
                                                s.Disturb(DisturbType.EquipRequest, true, false);
                                                attacker.SendAsciiMessage("You break your concentration to poison an arrow.");
                                                attacker.FixedEffect(0x3735, 6, 30);
                                            }

                                            Poison poisonToApply = this.MutatePoison(attacker, PCloth.Poison);

                                            Poison = poisonToApply;
                                            PoisonCharges = 1;
                                            PCloth.PoisonCharges--;
                                            if (PCloth.Poison != null)
                                            {
                                                if (attacker.AccessLevel > AccessLevel.GameMaster)
                                                {
                                                    attacker.SendAsciiMessage("Applying poison level {0} to ammo", Poison.Name);
                                                }

                                                if (AmmoType == typeof(Arrow))
                                                {
                                                    attacker.SendAsciiMessage("You wipe an arrow with your poison soaked rag and prepare to fire it.");
                                                }
                                                else
                                                {
                                                    attacker.SendAsciiMessage("You wipe a bolt with your poison soaked rag and prepare to fire it.");
                                                }
                                            }
                                        }

                                        // If no charges are left in the cloth, remove it
                                        if (PCloth.PoisonCharges == 0)
                                        {
                                            attacker.SendAsciiMessage("The rag falls apart from use.");
                                            PCloth.Delete();
                                        }
                                        // Delay for time spent poisoning arrow
                                        if (PCloth.Delay > 0.0)
                                        {
                                            return TimeSpan.FromSeconds(PCloth.Delay);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    attacker.DisruptiveAction();
                    attacker.Send(new Swing(0, attacker, defender));
                    if (OnFired(attacker, defender))
                    {
                        if (CheckHit(attacker, defender))
                            OnHit(attacker, defender);
                        else
                            OnMiss(attacker, defender);

                        if (Parent is BaseCreature bc && bc.AIObject is IEntity parent)
                            parent.Notify(notification: Notification.AmmoStatus, this);
                    }

                    if (Core.RuleSets.AngelIslandRules())
                    {   // set weapon to be unpoisoned since ranged weapons on AI can only apply one
                        //  dose of poison at a time. 
                        Poison = null;
                        PoisonCharges = 0;
                    }
                }

                return GetDelay(attacker);
            }
            else
            {
                return TimeSpan.FromSeconds(0.25);
            }
        }

        public override void OnHit(Mobile attacker, Mobile defender)
        {
            if (attacker.Player && !defender.Player && (defender.Body.IsAnimal || defender.Body.IsMonster) && 0.4 >= Utility.RandomDouble())
            {
                if (!defender.AddToBackpack(Ammo) && AutoStackAmmo)
                    ItemStacker.StackAt(defender.Location, defender.Map, AmmoType);
            }

            base.OnHit(attacker, defender);

            if (!Core.RuleSets.AOSRules() && Poison != null && PoisonCharges > 0)
            {
                --PoisonCharges;

                if (CheckHitPoison(attacker))
                {
                    defender.ApplyPoison(attacker, Poison); // don't mutate poison, we've already done that

                    OnHitPoison(attacker);
                }
            }
        }

        public override bool OnEquip(Mobile from)
        {
            try
            {   // NPCs like barkeeps can be equipped with bows, so we
                //	guard against that case here
                if (from != null && from is PlayerMobile && from.Backpack != null)
                {
                    bool cancelSpell = false;
                    ISpell i = from.Spell;
                    Spell s = (Spell)i;

                    if (!Core.RuleSets.StandardShardRules()) //don't bother checking if we're Siege etc.
                    {
                        PoisonCloth pCloth = (PoisonCloth)from.Backpack.FindItemByType(typeof(Server.Items.PoisonCloth), false);
                        if (pCloth != null && pCloth.PoisonCharges > 0)
                            if (i != null && s.State == SpellState.Sequencing)
                                cancelSpell = true;

                        if (cancelSpell)
                        {
                            s.Disturb(DisturbType.EquipRequest, true, false);
                            from.SendAsciiMessage("You break your concentration to poison an arrow.");
                            from.FixedEffect(0x3735, 6, 30);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                if (from is PlayerMobile)
                {
                    PlayerMobile pm = (PlayerMobile)from;
                    Console.WriteLine("Exception" + ex + from.Name);
                }
                else
                    Console.WriteLine("Exception" + ex);
            }

            return base.OnEquip(from);
        }

        public override void OnMiss(Mobile attacker, Mobile defender)
        {
            if (attacker.Player && 0.4 >= Utility.RandomDouble())
            {
                Point3D loc = new Point3D(defender.X + Utility.RandomMinMax(-1, 1), defender.Y + Utility.RandomMinMax(-1, 1), defender.Z);

                Ammo.MoveToWorld(loc, defender.Map);

                if (AutoStackAmmo)
                    ItemStacker.StackAt(loc, defender.Map, AmmoType);
            }

            base.OnMiss(attacker, defender);
        }

        public virtual bool OnFired(Mobile attacker, Mobile defender)
        {
            Container pack = attacker.Backpack;

            if (attacker.Player && (pack == null || !pack.ConsumeTotal(AmmoType, 1)))
                return false;

            attacker.MovingEffect(defender, EffectID, 18, 1, false, false);

            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                case 1:
                    {
                        break;
                    }
                case 0:
                    {
                        /*m_EffectID =*/
                        reader.ReadInt();
                        break;
                    }
            }

            if (version < 2)
            {
                //WeaponAttributes.MageWeapon = 0;
                //WeaponAttributes.UseBestSkill = 0;
            }
        }
    }
}