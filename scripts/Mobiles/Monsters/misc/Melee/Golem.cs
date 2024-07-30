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

/* Scripts/Mobiles/Monsters/Misc/Melee/Golem.cs
 * ChangeLog
 *  10/31/23, Yoar
 *      Added m_StunTimers dictionary. Being stunned twice in quick succession now resets the stun duration of 5s.
 *  10/9/2023, Adam
 *      * For Siege II, we half the mana damage and direct damage to the master. (Yoar request)
 *      * defender has his WarMod set to false when hit with a stunning blow
 *      * "The link between Controller and Golem can be broken by moving far away from the Golem."
 *          This is accomplished by issuing an 'all stop' command to the golem 'link broken' if they are out of range (and the golem cannot feed off the master's mana)
 *      https://uo.stratics.com/database/view.php?db_content=hunters&id=197
 *  9/19/2023, Adam (Control Slots / Core.SiegeII_CFG)
 *      For siege II we are dropping the control slots from 3 to two
 *  9/10/2023, Adam (OnGaveMeleeAttack)
 *      Update to include RunUOs stunning blow
 *      Reduce control slots from 4 to 3
 *  6/3/2023, Adam
 *      Golems &&  Golem Controllers
 *      Golem Controllers: now can drop a magic weapon.
 *      Remove Arcane Gems
 *      Golems: Remove Arcane Gems, and Power Crystal
 *      Power Crystals and Clockwork Assemblies are not in-era, the Arcane Gems seem to be, but we will decide that at a later date.
 *      https://web.archive.org/web/20011006081648/http://uo.stratics.com/hunters/controller.shtml
 *      https://web.archive.org/web/20020806222514/uo.stratics.com/hunters/irongolem.shtml
 *      https://web.archive.org/web/20011204203728/http://uo.stratics.com/content/arms-armor/special.shtml#arcane
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 11 lines removed.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a golem corpse")]
    public class Golem : BaseCreature
    {
        public override bool IsScaredOfScaryThings { get { return false; } }
        public override bool IsScaryToPets { get { return true; } }

        public override bool IsBondable { get { return false; } }
        //public override FoodType FavoriteFood { get { return FoodType.None; } }

        //public override bool CanBeDistracted { get { return false; } }

        public override bool HasLoyalty { get { return false; } }

        [Constructable]
        public Golem()
            : this(false, 1.0)
        {
        }

        [Constructable]
        public Golem(bool summoned, double scalar)
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.4, 0.8)
        {
            Name = "a golem";
            Body = 752;
            BardImmune = true;

            if (summoned)
                Hue = 2101;

            SetStr((int)(251 * scalar), (int)(350 * scalar));
            SetDex((int)(76 * scalar), (int)(100 * scalar));
            SetInt((int)(101 * scalar), (int)(150 * scalar));

            SetHits((int)(151 * scalar), (int)(210 * scalar));

            SetDamage((int)(13 * scalar), (int)(24 * scalar));


            SetSkill(SkillName.MagicResist, (150.1 * scalar), (190.0 * scalar));
            SetSkill(SkillName.Tactics, (60.1 * scalar), (100.0 * scalar));
            SetSkill(SkillName.Wrestling, (60.1 * scalar), (100.0 * scalar));

            if (summoned)
            {
                Fame = 10;
                Karma = 10;
            }
            else
            {
                Fame = 3500;
                Karma = -3500;
            }

            // 4 control slots is standard OSI
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=197
            ControlSlots = Core.SiegeII_CFG ? 3 : 4;
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                if (!Summoned)
                {
                    PackItem(new IronIngot(Utility.RandomMinMax(13, 21)));

                    if (0.1 > Utility.RandomDouble())
                        PackItem(new PowerCrystal());

                    if (0.15 > Utility.RandomDouble())
                        PackItem(new ClockworkAssembly());

                    if (0.2 > Utility.RandomDouble())
                        PackItem(new ArcaneGem());

                    if (0.25 > Utility.RandomDouble())
                        PackItem(new Gears());
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020806222514/uo.stratics.com/hunters/irongolem.shtml
                    // 11-25 Ingots, Arcane Gems, Gems, Gears, Power Crystals, Clockwork Assembly
                    // https://web.archive.org/web/20011007074011/http://uo.stratics.com/hunters/irongolem.shtml
                    // 	11-25 Ingots, Arcane Gems, Gems, Gears
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {   // don't think you can summon these in this era, but better to be safe
                        if (!Summoned)
                        {
                            PackItem(new IronIngot(Utility.RandomMinMax(11, 25)));

                            // Found as loot on Controllers. Tailors can use an Arcane Gem on exceptional quality items to make Arcane clothing. Arcane Gems can be used to recharge Arcane Clothing that has run out of charges.
                            // 6/3/2023, Adam. These appear to be in our era, but we will need to decide if we want them on Siege
                            if (!Core.RuleSets.SiegeStyleRules())
                                if (0.2 > Utility.RandomDouble())
                                    PackItem(new ArcaneGem());

                            PackGem(1, .9);
                            PackGem(1, .05);

                            if (0.25 > Utility.RandomDouble())
                                PackItem(new Gears());

                            if (PublishInfo.PublishDate >= new System.DateTime(2002, 06, 03))
                                if (0.1 > Utility.RandomDouble())
                                    PackItem(new PowerCrystal());

                            if (PublishInfo.PublishDate >= new System.DateTime(2002, 06, 03))
                                if (0.15 > Utility.RandomDouble())
                                    PackItem(new ClockworkAssembly());

                        }
                    }
                }
                else
                {   // Standard RunUO
                    if (Spawning)
                    {
                        if (!Summoned)
                        {
                            PackItem(new IronIngot(Utility.RandomMinMax(13, 21)));

                            if (0.1 > Utility.RandomDouble())
                                PackItem(new PowerCrystal());

                            if (0.15 > Utility.RandomDouble())
                                PackItem(new ClockworkAssembly());

                            if (0.2 > Utility.RandomDouble())
                                PackItem(new ArcaneGem());

                            if (0.25 > Utility.RandomDouble())
                                PackItem(new Gears());
                        }
                    }
                }
            }
        }

        public override bool DeleteOnRelease { get { return true; } }

        public override int GetAngerSound()
        {
            return 541;
        }

        public override int GetIdleSound()
        {
            if (!Controlled)
                return 542;

            return base.GetIdleSound();
        }

        public override int GetDeathSound()
        {
            if (!Controlled)
                return 545;

            return base.GetDeathSound();
        }

        public override int GetAttackSound()
        {
            return 562;
        }

        public override int GetHurtSound()
        {
            if (Controlled)
                return 320;

            return base.GetHurtSound();
        }

        public override bool AutoDispel { get { return !Controlled; } }

        public static TimeSpan ColossalBlowCooldown = TimeSpan.FromSeconds(5.0);
        public static TimeSpan StunDuration = TimeSpan.FromSeconds(5.0);

        private static readonly Dictionary<Mobile, Timer> m_StunTimers = new Dictionary<Mobile, Timer>();

        private DateTime m_NextColossalBlow;

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (DateTime.UtcNow >= m_NextColossalBlow && 0.3 > Utility.RandomDouble())
            {
                m_NextColossalBlow = DateTime.UtcNow + ColossalBlowCooldown;

                defender.Animate(21, 6, 1, true, false, 0);
                this.PlaySound(0xEE);

                BaseWeapon weapon = this.Weapon as BaseWeapon;
                if (weapon != null)
                    weapon.OnHit(this, defender);

                if (defender.Alive && !m_StunTimers.ContainsKey(defender))
                {
                    StartTimer(defender);

                    defender.Frozen = true;
                    defender.Warmode = false;   // https://uo.stratics.com/database/view.php?db_content=hunters&id=197
                    defender.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1070696); // You have been stunned by a colossal blow!
                }
            }
        }

        private static void Recover_Callback(object state)
        {
            Mobile defender = state as Mobile;

            if (defender != null)
            {
                StopTimer(defender);

                defender.Frozen = false;
                defender.Combatant = null;
                defender.LocalOverheadMessage(Network.MessageType.Regular, 0x3B2, false, "You recover your senses.");
            }
        }

        private static void StartTimer(Mobile m)
        {
            m_StunTimers[m] = Timer.DelayCall(StunDuration, new TimerStateCallback(Recover_Callback), m);
        }

        private static void StopTimer(Mobile m)
        {
            Timer timer;

            if (m_StunTimers.TryGetValue(m, out timer))
            {
                timer.Stop();
                m_StunTimers.Remove(m);
            }
        }

        public override void OnDamage(int amount, Mobile from, bool willKill, object source_weapon)
        {
            if (Controlled || Summoned)
            {
                Mobile master = (this.ControlMaster);

                if (master == null)
                    master = this.SummonMaster;

                if (master != null && master.Player && master.Map == this.Map && master.InRange(Location, 20))
                {
                    //10/9/2023, Adam: reduce mana/damage by 50%
                    int mod_amount = Core.SiegeII_CFG ? amount / 2 : amount;

                    if (master.Mana >= mod_amount)
                    {
                        master.Mana -= mod_amount;
                    }
                    else
                    {   // the amount of damage we do to the master, less their remaining mana
                        mod_amount -= master.Mana;
                        master.Mana = 0;
                        master.Damage(mod_amount, this);
                    }
                }
                else
                {
                    //  When the mana of it's controller reaches zero, the controller will take damage when the Golem takes damage.
                    // ==> The link between Controller and Golem can be broken by moving far away from the Golem.
                    // https://uo.stratics.com/database/view.php?db_content=hunters&id=197
                    this.ControlOrder = OrderType.Stop;
                }
            }

            // we always do full damage to the target
            base.OnDamage(amount, from, willKill, source_weapon);
        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public Golem(Serial serial)
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