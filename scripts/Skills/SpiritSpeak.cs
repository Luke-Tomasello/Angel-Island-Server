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

/* Scripts\Skills\SpiritSpeak.cs
 * ChangeLog
 *	11/15/10, Adam
 *		make The Slayer and The Summoner based upon UOAI
 *	7/23/10. adam
 *		Add a variable check to filter SpiritSpeak UsageReports
 *	7/20/10, adam
 *		Add slayer damage bonus
 *		You have a 10 second window to pump up to ((m.Skills.SpiritSpeak.Value + (m.Dex * 2.0) + (m.Skills.Tactics.Value * 2.0)) / 5.0)
 *			bonus points of damage into the DamageAccumulator before it is transfered into the DamageCache
 *			where it is used by BaseWeapon. The DamageCache will hold that charge to 10 seconds.
 *			The DamageAccumulator cannot accept new bonus points until the DamageCache times-out
 *	7/17/10, adam
 *		Add healing of your summons via SS
 *			you have a 10 second window to pump the heal-meter up as high as 100 heal points before they are applied.
 *			A GM with 100 int will pump 25 heal points into the heal meter per SS; less than GM or 100 int will pump less.
 *			Distance from your target is also based on your SS. At GM you can be as far as 5 tiles, at 50 SS you must be within 2.5 tiles.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	8/31/07, Adam
 *		Change CheckSkill() to check against a max skill of 100 instead of 120
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells;
using System;
using System.Collections;

namespace Server.SkillHandlers
{
    class SpiritSpeak
    {
        public static void Initialize()
        {
            SkillInfo.Table[32].Callback = new SkillUseCallback(OnUse);
        }

        private static Memory m_HealSummonsMemory = new Memory();
        private static Memory m_SlayerDamageAccumulator = new Memory();
        private static Memory m_SlayerDamageCache = new Memory();
        public static Memory SlayerDamageCache { get { return m_SlayerDamageCache; } }

        public static TimeSpan OnUse(Mobile m)
        {
            #region AOS
            if (Core.RuleSets.AOSRules())
            {
                Spell spell = new SpiritSpeakSpell(m);

                spell.Cast();

                if (spell.IsCasting)
                    return TimeSpan.FromSeconds(5.0);

                return TimeSpan.Zero;
            }
            #endregion AOS

            m.RevealingAction();

            if (m.CheckSkill(SkillName.SpiritSpeak, 0, 100, contextObj: new object[2]))
            {
                #region ghosts
                // ghosts
                if (!m.CanHearGhosts)
                {
                    Timer t = new HearGhostsTimer(m);
                    double secs = m.Skills[SkillName.SpiritSpeak].Base / 50;
                    secs *= 90;
                    if (secs < 15)
                        secs = 15;

                    t.Delay = TimeSpan.FromSeconds(secs);//15seconds to 3 minutes
                    t.Start();
                    m.CanHearGhosts = true;
                }
                #endregion ghosts
                // 1/3/23, Adam: I think adding this template is not abhorrent WRT Siege
                //  but we will wait a bit and feel out the players
                if (Core.RuleSets.AngelIslandRules())
                {
                    #region summon healing
                    // summon healing
                    if (m.FollowerCount > 0)
                    {   // okay, we have some followers
                        if (m_HealSummonsMemory.Recall(m) == false)
                        {   // We don't remember this guy so create a new memory
                            double MaxHealed = ((m.Skills.SpiritSpeak.Value + (m.Int * 2.0) + (m.Skills.Meditation.Value * 2.0)) / 5.0);
                            HealSummonsTimer th = new HealSummonsTimer(new HealSummonsContext(m, MaxHealed));
                            m_HealSummonsMemory.Remember(m, th, 10);
                            th.Context.HealSummonsHP += th.Context.HealSummonsHPMax / 4.0;
                        }
                        else
                        {   // we remember this guy so update 
                            Memory.ObjectMemory om = m_HealSummonsMemory.Recall(m as object);
                            HealSummonsTimer th = (HealSummonsTimer)om.Context;
                            if (th is HealSummonsTimer && th.Context != null)
                                th.Context.HealSummonsHP += th.Context.HealSummonsHPMax / 4.0;
                        }
                    }
                    #endregion summon healing
                }
                // 1/3/23, Adam: I think adding this template is not abhorrent WRT Siege
                //  but we will wait a bit and feel out the players
                if (Core.RuleSets.AngelIslandRules())
                {
                    #region slayer damage bonus
                    // slayer damage bonus
                    if (CheckSlayer(m))
                    {   // okay, they are holding a slayer
                        if (SpiritSpeak.SlayerDamageCache.Recall(m) == false)
                        {   // no unused cached charge 
                            if (m_SlayerDamageAccumulator.Recall(m) == false)
                            {   // We don't remember this guy so create a new memory
                                double spiritStrike = ((m.Skills.SpiritSpeak.Value + (m.Dex * 2.0) + (m.Skills.Tactics.Value * 2.0)) / 5.0);
                                SlayerDamageTimer th = new SlayerDamageTimer(new SlayerDamageContext(m, spiritStrike));
                                m_SlayerDamageAccumulator.Remember(m, th, 10);
                                th.Context.SlayerDamageHP += th.Context.SlayerDamageHPMax / 4.0;
                                // (m_SlayerDamageAccumulator.Recall(m as object) as Memory.ObjectMemory).OnReleaseEventHandler = new Memory.OnReleaseEventHandler(th.Context.DoRelease);
                            }
                            else
                            {   // we remember this guy so update 
                                Memory.ObjectMemory om = m_SlayerDamageAccumulator.Recall(m as object);
                                SlayerDamageTimer th = (SlayerDamageTimer)om.Context;
                                if (th is SlayerDamageTimer && th.Context != null)
                                    th.Context.SlayerDamageHP += th.Context.SlayerDamageHPMax / 4.0;
                            }
                        }
                        else
                        {
                            Memory.ObjectMemory om = SpiritSpeak.SlayerDamageCache.Recall(m as object);
                            SlayerDamageContext sdc = om.Context as SlayerDamageContext;
                            if (sdc.SlayerDamageHP > 0)
                                m.SendMessage("Your weapon is already fully charged.");
                            // else they have discharged their weapon and must now wait
                        }
                    }
                    #endregion slayer damage bonus
                }

                m.PlaySound(0x24A);
                m.SendLocalizedMessage(502444);//You contact the neitherworld.
            }
            else
            {
                m.SendLocalizedMessage(502443);//You fail to contact the neitherworld.
                m.CanHearGhosts = false;
            }

            return TimeSpan.FromSeconds(1.0);
        }

        #region hear ghosts
        private class HearGhostsTimer : Timer
        {
            private Mobile m_Owner;
            public HearGhostsTimer(Mobile m)
                : base(TimeSpan.FromMinutes(2.0))
            {
                m_Owner = m;
                Priority = TimerPriority.FiveSeconds;
            }

            protected override void OnTick()
            {
                m_Owner.CanHearGhosts = false;
                m_Owner.SendLocalizedMessage(502445);//You feel your contact with the neitherworld fading.
            }
        }
        #endregion hear ghosts

        #region Slayer Boost
        private static bool CheckSlayer(Mobile attacker)
        {
            BaseWeapon atkWeapon = attacker.Weapon as BaseWeapon;
            if (atkWeapon == null)
                return false;

            SlayerEntry atkSlayer = SlayerGroup.GetEntryByName(atkWeapon.Slayer);

            return atkSlayer != null;
        }

        public class SlayerDamageContext
        {
            private Mobile m_Owner;
            public Mobile Owner { get { return m_Owner; } }
            private double m_SlayerDamageHP;
            private double m_SlayerDamageHPMax;
            public double SlayerDamageHPMax { get { return m_SlayerDamageHPMax; } }
            private bool bShown = false;
            public double SlayerDamageHP { get { return m_SlayerDamageHP; } set { m_SlayerDamageHP = value; DoCharge(); } }

            public void DoRelease(object stats)
            {   // the SlayerDamageHP will be zero if the player has used the charge
                if (SpiritSpeak.SlayerDamageCache.Recall(Owner) && SlayerDamageHP > 0)
                {
                    Effects.PlaySound(Owner.Location, Owner.Map, 0x1F8);
                    Owner.SendMessage("Your slayer loses it's charge.");
                }
                return;
            }

            private void DoCharge()
            {
                if (m_SlayerDamageHP >= m_SlayerDamageHPMax && bShown == false)
                {   // show the player that we are fully charged
                    bShown = true;
                    BaseWeapon weapon = Owner.Weapon as BaseWeapon;
                    int itemID, soundID;

                    switch (weapon.Skill)
                    {
                        case SkillName.Macing: itemID = 0xFB4; soundID = 0x232; break;
                        case SkillName.Archery: itemID = 0x13B1; soundID = 0x145; break;
                        default: itemID = 0xF5F; soundID = 0x56; break;
                    }

                    // ripped from ConsecrateWeaponSpell
                    //  no longer works, not even RunUO shows the overhead animation. I suspect newer clients don't support it? 
                    //  It used to work on Angel Island for charging a slayer weapon (Slayer Strike)
                    Owner.PlaySound(0x20C);
                    Owner.PlaySound(soundID);
                    Owner.FixedParticles(0x3779, 1, 30, 9964, 3, 3, EffectLayer.Waist);

                    IEntity from = new Entity(Serial.Zero, new Point3D(Owner.X, Owner.Y, Owner.Z), Owner.Map);
                    IEntity to = new Entity(Serial.Zero, new Point3D(Owner.X, Owner.Y, Owner.Z + 50), Owner.Map);
                    Effects.SendMovingParticles(from, to, itemID, 1, 0, false, false, 33, 3, 9501, 1, 0, EffectLayer.Head, 0x100);

                }
                else if (m_SlayerDamageHP < m_SlayerDamageHPMax)
                {   // show the player that we are chargeing
                    Owner.FixedEffect(14276, 10, 14, 4, 3);
                }
            }

            public SlayerDamageContext(Mobile m, double max)
            {
                m_Owner = m;
                m_SlayerDamageHPMax = max;
            }
        }

        public class SlayerDamageTimer : Timer
        {
            private SlayerDamageContext m_context;
            public SlayerDamageContext Context { get { return m_context; } }
            public SlayerDamageTimer(SlayerDamageContext context)
                : base(TimeSpan.FromSeconds(10.0))
            {
                m_context = context;
                Priority = TimerPriority.TwoFiftyMS;
                Start();
            }

            protected override void OnTick()
            {
                // cap the strike potential
                if (m_context.SlayerDamageHP > Context.SlayerDamageHPMax)
                    m_context.SlayerDamageHP = Context.SlayerDamageHPMax;

                if (m_context.Owner == null || m_context.Owner.Deleted || m_context.Owner.NetState == null || m_context.Owner.Map == Map.Internal)
                    return;

                // alert senior staff for analysis purposes
                //SpiritSpeak.UsageReport(m_context.Owner, "Spirit Speak: SlayerDamage");

                // apply the bonus
                Memory.ObjectMemory om = SpiritSpeak.SlayerDamageCache.Recall(Context.Owner as object);
                if (om != null)
                {   // delete the old cached bonus
                    SpiritSpeak.SlayerDamageCache.Forget(Context.Owner);
                }

                // you have 20 seconds to use this strike
                SpiritSpeak.SlayerDamageCache.Remember(Context.Owner, Context, new TimerStateCallback(Context.DoRelease), 20);
                Context.Owner.SendMessage("You have 20 seconds to utilize this slayer strike.");
            }
        }
        #endregion Slayer Boost

        #region Heal Summons
        public class HealSummonsContext
        {
            private Mobile m_Owner;
            public Mobile Owner { get { return m_Owner; } }
            private double m_HealSummonsHP;
            private double m_HealSummonsHPMax;
            public double HealSummonsHP { get { return m_HealSummonsHP; } set { m_HealSummonsHP = value; } }
            public double HealSummonsHPMax { get { return m_HealSummonsHPMax; } }

            public HealSummonsContext(Mobile m, double max)
            {
                m_Owner = m;
                m_HealSummonsHPMax = max;
            }
        }

        public class HealSummonsTimer : Timer
        {
            private HealSummonsContext m_context;
            public HealSummonsContext Context { get { return m_context; } }
            public HealSummonsTimer(HealSummonsContext context)
                : base(TimeSpan.FromSeconds(10.0))
            {
                m_context = context;
                Priority = TimerPriority.TwoFiftyMS;
                Start();
            }

            protected override void OnTick()
            {
                // cap the healing potential
                if (m_context.HealSummonsHP > m_context.HealSummonsHPMax)
                    m_context.HealSummonsHP = m_context.HealSummonsHPMax;

                if (m_context.Owner == null || m_context.Owner.Deleted || m_context.Owner.NetState == null || m_context.Owner.Map == Map.Internal)
                    return;

                // alert senior staff for analysis purposes
                //SpiritSpeak.UsageReport(m_context.Owner, "Spirit Speak: HealSummons");

                // apply the healing
                ArrayList pets = new ArrayList();
                foreach (Mobile m in World.Mobiles.Values)
                {   // it's a creature
                    if (m is BaseCreature)
                    {
                        BaseCreature bc = (BaseCreature)m;
                        // it's my creature
                        if (bc.Summoned && bc.SummonMaster == m_context.Owner)
                            // it's close
                            if (m_context.Owner.GetDistanceToSqrt(m) <= (m_context.Owner.Skills.SpiritSpeak.Value / 20.0))
                                pets.Add(bc);
                            else if (m_context.Owner.GetDistanceToSqrt(m) <= (m_context.Owner.Skills.SpiritSpeak.Value / 10.0))
                                m_context.Owner.SendMessage("You are too far away to heal your pet.");


                    }
                }

                for (int ix = 0; ix < pets.Count; ix++)
                {
                    BaseCreature pet = pets[ix] as BaseCreature;
                    if (pet.Hits < pet.HitsMax)
                    {
                        int delta = pet.HitsMax - pet.Hits;
                        if (delta < m_context.HealSummonsHP)
                        {   // heal this much
                            pet.Heal(delta);
                            m_context.HealSummonsHP -= delta;
                        }
                        else
                        {   // using the last of our heal potential
                            pet.Heal((int)m_context.HealSummonsHP);
                            m_context.HealSummonsHP = 0;
                        }

                        // heal
                        pet.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);
                        m_context.Owner.PlaySound(0x1E9);

                        if (m_context.HealSummonsHP == 0)
                            break;
                    }
                }
            }
        }
        #endregion Heal Summons

        #region SpiritSpeakSpell
        private class SpiritSpeakSpell : Spell
        {
            private static SpellInfo m_Info = new SpellInfo("Spirit Speak", "", SpellCircle.Second, 269);

            public override bool BlockedByHorrificBeast { get { return false; } }

            public SpiritSpeakSpell(Mobile caster)
                : base(caster, null, m_Info)
            {
            }

            public override bool ClearHandsOnCast { get { return false; } }

            public override TimeSpan GetCastDelay()
            {
                return TimeSpan.FromSeconds(1.0);
            }

            public override int GetMana()
            {
                return 0;
            }

            public override bool ConsumeReagents()
            {
                return true;
            }

            public override bool CheckFizzle()
            {
                return false;
            }

            public override bool CheckNextSpellTime { get { return false; } }

            public override void OnDisturb(DisturbType type, bool message)
            {
                Caster.NextSkillTime = Core.TickCount;

                base.OnDisturb(type, message);
            }

            public override bool CheckDisturb(DisturbType type, bool checkFirst, bool resistable)
            {
                if (type == DisturbType.EquipRequest || type == DisturbType.UseRequest)
                    return false;

                return true;
            }

            public override void SayMantra()
            {
                // Anh Mi Sah Ko
                Caster.PublicOverheadMessage(MessageType.Regular, 0x3B2, 1062074, "", false);
                Caster.PlaySound(0x24A);
            }

            public override void OnCast()
            {
                Corpse toChannel = null;

                IPooledEnumerable eable = Caster.GetItemsInRange(3);
                foreach (Item item in eable)
                {
                    if (item is Corpse && !((Corpse)item).Channeled)
                    {
                        toChannel = (Corpse)item;
                        break;
                    }
                }
                eable.Free();

                int max, min, mana, number;

                if (toChannel != null)
                {
                    min = 1 + (int)(Caster.Skills[SkillName.SpiritSpeak].Value * 0.25);
                    max = min + 4;
                    mana = 0;
                    number = 1061287; // You channel energy from a nearby corpse to heal your wounds.
                }
                else
                {
                    min = 1 + (int)(Caster.Skills[SkillName.SpiritSpeak].Value * 0.25);
                    max = min + 4;
                    mana = 10;
                    number = 1061286; // You channel your own spiritual energy to heal your wounds.
                }

                if (Caster.Mana < mana)
                {
                    Caster.SendLocalizedMessage(1061285); // You lack the mana required to use this skill.
                }
                else
                {
                    Caster.CheckSkill(SkillName.SpiritSpeak, 0.0, 100.0, contextObj: new object[2]);

                    if (Utility.RandomDouble() > (Caster.Skills[SkillName.SpiritSpeak].Value / 100.0))
                    {
                        Caster.SendLocalizedMessage(502443); // You fail your attempt at contacting the netherworld.
                    }
                    else
                    {
                        if (toChannel != null)
                        {
                            toChannel.Channeled = true;
                            toChannel.Hue = 0x835;
                        }

                        Caster.Mana -= mana;
                        Caster.SendLocalizedMessage(number);

                        if (min > max)
                            min = max;

                        Caster.Hits += Utility.RandomMinMax(min, max);

                        Caster.FixedParticles(0x375A, 1, 15, 9501, 2100, 4, EffectLayer.Waist);
                    }
                }

                FinishSequence();
            }
        }
        #endregion SpiritSpeakSpell

        public static void UsageReport(Mobile m, string text)
        {
            // Tell staff that an a player is using this system
            if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.SpiritSpeakUsageReport))
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Administrator,
                0x482,
                String.Format("At location: {0}, {1} ", m.Location, text));
        }
    }
}