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

/* Scripts/Items/SkillItems/Magical/Potions/Explosion Potions/BaseExplosionPotion.cs
 * ChangeLog:
 *  5/5/23, Yoar
 *      Added NewDamageRanges toggle. If disabled, explosion potion damage ranges are given according to:
 *      https://web.archive.org/web/20021002021857/http://uo.stratics.com/alchemy/potions/tactics/explosion.shtml
 *      Added AlchemyScaling toggle.
 *  4/26/23, Adam (CriminalBombing)
 *      We now assess the criminality of a bomb blast based on Notoriety. that is, you can bomb criminals,
 *          murderers, enemies, and non-tame animals.
 *      If the bomb blast should hit an innocent, and guards are called, either buy a player of NPC, 
 *          the usual repercussions will be had.
 *      It is worth noting, BaseVenrors now recognize a ticking bomb and will call guards accordingly.
 *      It is also worth noting that targeting alone (in town) no longer flags you as criminal.
 *  4/25/23, Adam (Armed prop)
 *      Add an 'armed' prop so if the player tries to place this on the ground after arming it,
 *      we can detect it and disallow the placement.
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *  7/20/21, Pix
 *      Added 'cooldown' option for explosion potion
 *	7/2/10, Adam
 *		add target 'under house' exploit check
 *		if ((target is Server.Targeting.LandTarget && Server.Multis.BaseHouse.FindHouseAt(((Server.Targeting.LandTarget)(target)).Location, Caster.Map, 16) != null))
 *			target cannot be seen
 *	6/30/10, adam
 *		o If the potion is not in your backpack, a skill check is made to make sure you are skilled enough
 *		o Before a potion explodes after you die, a skill check is made to make sure you are skilled enough
 *		o If you are exploding a potion in your backpack, in town, and 'invisible shield' hasn't caught it...
 *			do a skill check to see if you are skilled enough to pull it off otherwise the guard defuses it.
 *	6/30/10, adam
 *		finish up work started yesterday (SuicideBomber/InvisibleShield)
 *	6/29/10, adam
 *		If you blow up a potion in town you will be flagged as a SuicideBomber 
 *		If a guard sees a SuicideBomber he will cast InvisibleShield on him whereby nerfing all collateral damage
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/25/10, adam
 *		o Add messages to indicate how the guard blocked the potion
 *		o add flags to the class to support the notion of a dud
 *			a potion that is batted away by a guard is a dud
 *	6/24/10, adam
 *		o if you throw the potion in town and there is a guard on you...
 *			there is a skill check to throw successfully. if you fail, the guard may:
 *			a) block the throw entirely such that it stays in your backpack and explodes
 *			b) knock it from you hands such that it falls to your feet
 *			c) deflect it such that it goes flying
 *		o if the potion is exploded in your backpack && ur skillz < from.Skills.Alchemy.Value > 96.8, it only delivers 1 pt damage
 *		o guards are called when a potion is targeted (from an RP perspective 'thrown', if an NPC sees this they call guards.)
 *	6/23/10, adam
 *		if the potion is exploded in your backpack, it only delivers 1 pt damage
 *  05/01/06 Taran Kain
 *		Fixed that goddamn z-axis problem. Targets must be within (Range * 8) z-units of the explosion to be affected.
 *	4/4/06, weaver
 *		Added parameter to CanBeHarmful() check to allow harm after the 
 *		death of the potion thrower.
 *	6/3/05, Adam
 *		Add in ExplosionPotionThreshold to control the tossers 
 *		health requirement
 *	5/4/05, Adam
 *		Make the targeting based on your stam and HP.
 *		If you have taken any damage, either in HP or stamina, you will not be able to toss
 *		a heat seeking explosion potion, and the potion will resolve to the targeted X,Y instead.
 *	4/27/05, erlein
 *		Added a check on target location to see if mobile can be spawned there to
 *		resolve "through the wall" problem.
 *	4/26/05, Pix
 *		Made explode pots targetting method toggleable based on CoreAI/ConsoleManagement setting.
 *	4/24/05, Pix
 *		Change to how targeting works.
 *	4/18/05, Pix
 *		Now resets swing timer on use and on target.
 *	8/26/04, Pix
 *		Added additional random 0-2 second delay on explosion potions.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using Server.Network;
using Server.Spells;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Items
{
    public abstract class BaseExplosionPotion : BasePotion
    {
        /* 5/5/23, Yoar: Added old explosion potion damage ranges according to:
         * https://web.archive.org/web/20021002021857/http://uo.stratics.com/alchemy/potions/tactics/explosion.shtml
         * 
         * I'm not sure when explosion potion damage was increased. For now, I will assume this was on Pub16.
         * 
         * AI/MO will keep increased explosion potion damage regardless.
         */
        public static bool NewDamageRanges
        {
            get { return (PublishInfo.Publish >= 16.0 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()); }
        }

        /* 5/5/23, Yoar: Hearsay would have us know that explosion potion damage was *not* impacted by alchemy skill.
         * 
         * Again, for lack of conclusive source, I'm conditioning this to Pub16.
         * 
         * AI/MO will keep alchemy scaling regardless.
         */
        public static bool AlchemyScaling
        {
            get { return (PublishInfo.Publish >= 16.0 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()); }
        }

        public abstract int MinDamage { get; }
        public abstract int MaxDamage { get; }

        public override bool RequireFreeHand { get { return false; } }

        private static bool LeveledExplosion = false;   // Should explosion potions explode other nearby potions?
        private static bool InstantExplosion = false;   // Should explosion potions explode on impact?
        private const int ExplosionRange = 2;           // How long is the blast radius?

        [Flags]
        public enum PotFlags
        {
            None,
            Dud1pt,                                     // Dud1pt: when a guard smacks it out of your hands, it fails to dully detonate
            InBackpack                                  // InBackpack: exploding in backpack (Suicide bomber)
        }
        PotFlags m_Flags;

        public BaseExplosionPotion(PotionEffect effect)
            : base(0xF0D, effect)
        {
        }

        public BaseExplosionPotion(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write((int)m_Flags);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    m_Flags = (PotFlags)reader.ReadInt();
                    goto case 0;

                case 0:
                    break;
            }
        }

        public virtual object FindParent(Mobile from)
        {
            Mobile m = this.HeldBy;

            if (m != null && m.Holding == this)
                return m;

            object obj = this.RootParent;

            if (obj != null)
                return obj;

            if (Map == Map.Internal)
                return from;

            return this;
        }

        private Timer m_Timer;

        // users are simply those players that double clicked the potion - if it was on the ground, it may be more than one
        //	when it explodes, and users targets are canceled.
        //	A second player can double click a potion thrown at them and toss it elsewhere. 
        private ArrayList m_Users;

        public override void Drink(Mobile from)
        {
            bool on_the_ground = !this.IsChildOf(from.Backpack);

            if (Core.RuleSets.AOSRules() && (from.Paralyzed || from.Frozen || (from.Spell != null && from.Spell.IsCasting)))
            {
                from.SendLocalizedMessage(1062725); // You can not use a purple potion while paralyzed.
                return;
            }

            #region Dueling
            if (from.Region.IsPartOf(typeof(Engines.ConPVP.SafeZone)) && !Engines.ConPVP.DuelContext.IsActivelyDueling(from))
            {
                from.SendMessage("You may not use explosion potions in this area.");
                return;
            }
            #endregion

            // you must be skilled to detonate a bomb off the ground (or other non-backpack location)
            if (on_the_ground && !(ChanceBasedOnAbility(from) >= Utility.RandomDouble()))
            {
                from.SendMessage("You fail in your attempt to arm the purple potion.");
                return;
            }

            // if explosion potion throw delay is > 0, check it
            if (CoreAI.ExplosionPotionThrowDelay > 0.0)
            {
                if (from is PlayerMobile pm)
                {
                    if (pm.CanThrowNow() == false)
                    {
                        from.SendMessage("You fail to arm the purple potion.");
                        return;
                    }
                }
            }

            //reset from's swingtimer
            BaseWeapon weapon = from.Weapon as BaseWeapon;
            if (weapon != null)
            {
                from.NextCombatTime = DateTime.UtcNow + weapon.GetDelay(from);
            }

            // no target if lit on the ground
            if (!on_the_ground)
            {
                ThrowTarget targ = from.Target as ThrowTarget;

                if (targ != null && targ.Potion == this)
                    return;

                // we will also reveal on target. Tageting is now a criminal action
                from.RevealingAction();
            }

            if (m_Users == null)
                m_Users = new ArrayList();

            if (!m_Users.Contains(from))
                m_Users.Add(from);

            // no target if lit on the ground
            if (!on_the_ground)
                from.Target = new ThrowTarget(this);

            if (m_Timer == null)
            {
                // no target if lit on the ground
                if (!on_the_ground)
                    from.SendLocalizedMessage(500236);      // You should throw it now!
                int numberoftics = 3;                       //minimum
                numberoftics += Utility.Random(0, 3);       // add 0,1,or 2 tics to make the total time 3-5 seconds
                m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(0.75), TimeSpan.FromSeconds(1.0), numberoftics + 1, new TimerStateCallback(Detonate_OnTick), new object[] { from, numberoftics });
            }

            // lighting one of these babies on the ground in town makes you crim
            if (on_the_ground && this.InTown(from))
            {   // now that it's armed, have the NPC's Look Around to MAYBE see the criminal (this is how guards get called.)
                LookAround(from);
            }
        }
        private void LookAround(Mobile from)
        {
            Point3D location = this.Map != from.Map ? from.Location : this.Location;
            IPooledEnumerable eable = from.Map.GetMobilesInRange(location, this.GetMaxUpdateRange());
            foreach (Mobile witness in eable)
            {
                if (witness != null && !witness.Player)
                    if (witness.CanSee(this) && witness.InRange(location, this.GetUpdateRange(witness)))
                        witness.OnSee(from, this);
            }
            eable.Free();
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Armed
        {
            get { return m_Timer != null; }
        }
        private void Detonate_OnTick(object state)
        {
            if (Deleted)
                return;

            object[] states = (object[])state;
            Mobile from = (Mobile)states[0];
            int timer = (int)states[1];

            object parent = FindParent(from);

            if (timer == 0)
            {
                Point3D loc;
                Map map;

                if (parent is Item)
                {
                    Item item = (Item)parent;

                    loc = item.GetWorldLocation();
                    map = item.Map;
                }
                else if (parent is Mobile)
                {
                    Mobile m = (Mobile)parent;

                    loc = m.Location;
                    map = m.Map;
                }
                else
                {
                    return;
                }

                Explode(from, true, loc, map);
            }
            else
            {
                if (parent is Item)
                {
                    ((Item)parent).PublicOverheadMessage(MessageType.Regular, 0x22, false, timer.ToString());
                }
                else if (parent is Mobile)
                {
                    ((Mobile)parent).PublicOverheadMessage(MessageType.Regular, 0x22, false, timer.ToString());
                }

                states[1] = timer - 1;
            }
        }

        private void Reposition_OnTick(object state)
        {
            if (Deleted)
                return;

            object[] states = (object[])state;
            Mobile from = (Mobile)states[0];
            IPoint3D p = (IPoint3D)states[1];
            Map map = (Map)states[2];

            Point3D loc = new Point3D(p);

            if (InstantExplosion)
                Explode(from, true, loc, map);
            else
                MoveToWorld(loc, map);
        }

        public bool CheckHealth(int current, int max, double percent)
        {
            return ((double)current) >= (((double)max) * percent);
        }

        public double ChanceBasedOnAbility(Mobile from)
        {
            if (from.Skills.Alchemy.Value < 50)
                return 0.0;

            double chance = (((from.Skills.Alchemy.Value - 80.0) * 5.0) + ((from.Dex - 50.0) * 2.0)) / 2.66;
            return chance /= 100.0;
        }

        public bool InTown(Mobile from)
        {
            if (from != null && from.Region is Regions.GuardedRegion && (from.Region as Regions.GuardedRegion).IsGuarded)
                return true;
            else
                return false;
        }

        public bool IsGuardAttacking(Mobile m)
        {
            List<AggressorInfo> aggressors = m.Aggressed;

            if (aggressors.Count > 0)
            {
                for (int i = 0; i < aggressors.Count; ++i)
                {
                    AggressorInfo info = (AggressorInfo)aggressors[i];
                    Mobile defender = info.Defender;

                    if (defender != null && !defender.Deleted && defender.GetDistanceToSqrt(m) <= 2.0)
                    {
                        if (defender is Mobiles.BaseGuard)
                            return true;
                    }
                }
            }
            return false;
        }

        private class ThrowTarget : Target
        {
            private BaseExplosionPotion m_Potion;

            public BaseExplosionPotion Potion
            {
                get { return m_Potion; }
            }

            public ThrowTarget(BaseExplosionPotion potion)
                : base(12, true, TargetFlags.None)
            {
                m_Potion = potion;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                //reset from's swingtimer
                BaseWeapon weapon = from.Weapon as BaseWeapon;
                if (weapon != null)
                {
                    from.NextCombatTime = DateTime.UtcNow + weapon.GetDelay(from);
                }

                if (m_Potion.Deleted || m_Potion.Map == Map.Internal)
                    return;

                IPoint3D p = targeted as IPoint3D;

                if (p == null)
                    return;

                Map map = from.Map;

                if (map == null)
                    return;

                SpellHelper.GetSurfaceTop(ref p);

                #region Dueling
                if ((from.Region.IsPartOf(typeof(Engines.ConPVP.SafeZone)) || Region.Find(new Point3D(p), map).IsPartOf(typeof(Engines.ConPVP.SafeZone))) && !Engines.ConPVP.DuelContext.IsActivelyDueling(from))
                {
                    from.SendMessage("You may not use explosion potions in this area.");
                    return;
                }
                #endregion

                // erl: 04/27/05, spawn mobile check at target location
                if (!map.CanSpawnLandMobile(p.X, p.Y, p.Z) && !(p is Mobile))
                {
                    from.SendLocalizedMessage(501942);  // That location is blocked.
                    return;
                }

                // adam: add target 'under house' exploit check
                if (!from.CanSee(p) || (p is Server.Targeting.LandTarget && Server.Multis.BaseHouse.FindHouseAt(((Server.Targeting.LandTarget)(p)).Location, from.Map, 16) != null))
                {
                    from.SendLocalizedMessage(500237); // Target can not be seen.
                    return;
                }

                // reveal on throw
                from.RevealingAction();

                // throw timing
                if (from is PlayerMobile pm && CoreAI.ExplosionPotionThrowDelay > 0)
                {
                    pm.SetThrowDelay(CoreAI.ExplosionPotionThrowDelay);
                }

                // adam: criminal action on throw if any mobiles are to be affected but me
                if (m_Potion.InTown(from))
                {
                    if (m_Potion.CriminalBombing(from, new Point3D(p.X, p.Y, p.Z), map))
                        from.CriminalAction(true);
                }

                IEntity to;
                bool bMobile = CoreAI.ExplosionPotionTargetMethod == CoreAI.EPTM.MobileBased;

                // IN TOWN
                // if we're in town and the town uses smart guards, and a guard is on us and they have hit us at least once, ...
                if (m_Potion.InTown(from) && (from.Region as Regions.GuardedRegion).IsSmartGuards && from.Hits < from.HitsMax && m_Potion.IsGuardAttacking(from))
                {
                    if (m_Potion.ChanceBasedOnAbility(from) >= Utility.RandomDouble())
                    {   // cool, just throw as per normal
                        to = new Entity(Serial.Zero, new Point3D(p), map);
                        Effects.SendMovingEffect(from, to, m_Potion.ItemID & 0x3FFF, 7, 0, false, false, m_Potion.Hue, 0);
                        m_Potion.Internalize();
                        Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(m_Potion.Reposition_OnTick), new object[] { from, to, map });
                    }
                    else
                        switch (Utility.Random(3))
                        {
                            case 0: // You fumble and drop the potion to your feet.
                                m_Potion.SetFlag(PotFlags.Dud1pt);  // minimize collateral damage 
                                from.SendMessage("You fumble and drop the potion to your feet.");
                                to = from;
                                Effects.SendMovingEffect(from, to, m_Potion.ItemID & 0x3FFF, 7, 0, false, false, m_Potion.Hue, 0);
                                m_Potion.Internalize();
                                Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(m_Potion.Reposition_OnTick), new object[] { from, to, map });
                                break;
                            case 1: // The guard blocks you and the potion remains in your backpack!
                                from.SendMessage("The guard blocks you and the potion remains in your backpack!");
                                break;
                            case 2: // You attempt to toss the potion, but the guard bats it away.
                                m_Potion.SetFlag(PotFlags.Dud1pt);  // minimize collateral damage 
                                from.SendMessage("You attempt to toss the potion, but the guard bats it away.");
                                to = new Entity(Serial.Zero, Spawner.GetSpawnPosition(from.Map, from.Location, 7, SpawnFlags.None, m_Potion), map);
                                Effects.SendMovingEffect(from, to, m_Potion.ItemID & 0x3FFF, 7, 0, false, false, m_Potion.Hue, 0);
                                m_Potion.Internalize();
                                Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(m_Potion.Reposition_OnTick), new object[] { from, to, map });
                                break;
                        }
                }
                // OUT ON THE FIELD
                // Adam: You must be in top condition to target the player directly
                //if( bMobile && (from.Stam >= from.StamMax) && (from.Hits >= from.HitsMax) )
                else if (bMobile && m_Potion.CheckHealth(from.Stam, from.StamMax, CoreAI.ExplosionPotionThreshold) && m_Potion.CheckHealth(from.Hits, from.HitsMax, CoreAI.ExplosionPotionThreshold))
                {
                    if (p is Mobile)
                        to = (Mobile)p;
                    else
                        to = new Entity(Serial.Zero, new Point3D(p), map);
                    Effects.SendMovingEffect(from, to, m_Potion.ItemID & 0x3FFF, 7, 0, false, false, m_Potion.Hue, 0);
                    m_Potion.Internalize();
                    Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(m_Potion.Reposition_OnTick), new object[] { from, p, map });
                }
                else
                {
                    to = new Entity(Serial.Zero, new Point3D(p), map);
                    Effects.SendMovingEffect(from, to, m_Potion.ItemID & 0x3FFF, 7, 0, false, false, m_Potion.Hue, 0);
                    m_Potion.Internalize();
                    Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(m_Potion.Reposition_OnTick), new object[] { from, to, map });
                }
            }
        }

        public void Explode(Mobile from, bool direct, Point3D loc, Map map)
        {
            if (Deleted)
                return;

            // if the potion is exploded in your backpack, you are flagged as a SuicideBomber
            //	guards will cast InvisibleShield on you to nerf the bombs potential
            if (InTown(from) && (from.Holding == this || this.IsChildOf(from.Backpack)))
            {   // reset the timer
                from.SuicideBomber = false;
                from.ConvictSuicideBomber(TimeSpan.FromMinutes(2.0));
                SetFlag(PotFlags.InBackpack);
            }

            Delete();

            for (int i = 0; m_Users != null && i < m_Users.Count; ++i)
            {
                Mobile m = (Mobile)m_Users[i];
                ThrowTarget targ = m.Target as ThrowTarget;

                if (targ != null && targ.Potion == this)
                    Target.Cancel(m);
            }

            if (map == null)
                return;

            Effects.PlaySound(loc, map, 0x207);
            Effects.SendLocationEffect(loc, map, 0x36BD, 20);

            int alchemyBonus = 0;

            if (AlchemyScaling && direct)
                alchemyBonus = (int)(from.Skills.Alchemy.Value / (Core.RuleSets.AOSRules() ? 5 : 10));
#if false
            IPooledEnumerable eable = LeveledExplosion ? map.GetObjectsInRange(loc, ExplosionRange) : map.GetMobilesInRange(loc, ExplosionRange);
            ArrayList toExplode = new ArrayList();

            int toDamage = 0;

            foreach (object o in eable)
            {
                if (o is IPoint3D)
                {
                    IPoint3D i = o as IPoint3D;
                    if (Math.Abs(i.Z - this.Z) > ExplosionRange * 8)
                        continue;
                }

                if (o is Mobile)
                {
                    toExplode.Add(o);
                    ++toDamage;
                }
                else if (o is BaseExplosionPotion && o != this)
                {
                    toExplode.Add(o);
                }
            }

            eable.Free();
#else
            int toDamage = 0;
            ArrayList toExplode = new ArrayList(AffectedObjects(ref toDamage, loc, map));
#endif
            int min = Scale(from, MinDamage);
            int max = Scale(from, MaxDamage);

            //	a guard cast InvisibleShield on you since you're flagged as a SuicideBomber and the potion is in your backpack
            // the damage will be limited to you, others will take min damage
            if (from.InvisibleShield && GetFlag(PotFlags.InBackpack))
                from.SendMessage("The invisible shield muffles the explosion.");
            // guards have the ability to disarm the the potion if your skills are poor
            else if (IsGuardAttacking(from) && GetFlag(PotFlags.InBackpack) && !(ChanceBasedOnAbility(from) >= Utility.RandomDouble()))
            {
                SetFlag(PotFlags.Dud1pt);
                from.SendMessage("The guard reaches into your backpack and partially disables the potion.");
            }

            for (int i = 0; i < toExplode.Count; ++i)
            {
                object o = toExplode[i];

                if (o is Mobile)
                {
                    Mobile m = (Mobile)o;

                    // wea: added parameter to CanBeHarmful() check to allow harm after the 
                    // death of the potion thrower
                    bool ignoreOurDeadness = ChanceBasedOnAbility(from) >= Utility.RandomDouble();
                    if (from == null || (SpellHelper.ValidIndirectTarget(from, m) && from.CanBeHarmful(m, false, false, ignoreOurDeadness)))
                    {
                        if (from != null)
                            from.DoHarmful(m);

                        int damage = Utility.RandomMinMax(min, max);

                        damage += alchemyBonus;

                        if (!Core.RuleSets.AOSRules() && damage > 40)
                            damage = 40;
                        else if (Core.RuleSets.AOSRules() && toDamage > 2)
                            damage /= toDamage - 1;

                        //	a guard cast InvisibleShield on you since you're flagged as a SuicideBomber and the potion is in your backpack
                        // the damage will be limited to you, others will take min damage
                        if (m != from && from.InvisibleShield && GetFlag(PotFlags.InBackpack))
                            damage = 1;

                        // it's a dud (probably because a guard is on you and batted it away to some random spot.)
                        if (GetFlag(PotFlags.Dud1pt) == true)
                            damage = 1;

                        AOS.Damage(m, from, damage, 0, 100, 0, 0, 0, this);
                    }
                }
                else if (o is BaseExplosionPotion)
                {
                    BaseExplosionPotion pot = (BaseExplosionPotion)o;

                    pot.Explode(from, false, pot.GetWorldLocation(), pot.Map);
                }
            }
        }
        public bool CriminalBombing(Mobile from, Point3D loc, Map map)
        {
            int mobilesToDamage = 0;
            List<object> list = AffectedObjects(ref mobilesToDamage, loc, map);
            foreach (object obj in list)
            {
                if (obj != from)
                    if (obj is Mobile target)
                    {
                        int result = Server.Misc.NotorietyHandlers.MobileNotoriety(from, target);
                        switch (result)
                        {
                            case Notoriety.Innocent:
                                return true;
                            case Notoriety.Ally:
                                return true;
                            case Notoriety.CanBeAttacked:
                                continue;
                            case Notoriety.Criminal:
                                continue;
                            case Notoriety.Enemy:
                                continue;
                            case Notoriety.Murderer:
                                continue;
                            case Notoriety.Invulnerable:
                                continue;
                            default:
                                continue;
                        }
                    }
            }
            return false;
        }
        public List<object> AffectedObjects(ref int mobilesToDamage, Point3D loc, Map map, List<Mobile> ignore = null)
        {
            IPooledEnumerable eable = LeveledExplosion ? map.GetObjectsInRange(loc, ExplosionRange) : map.GetMobilesInRange(loc, ExplosionRange);
            List<object> toExplode = new();

            foreach (object o in eable)
            {
                if (o is IPoint3D)
                {
                    IPoint3D i = o as IPoint3D;
                    if (Math.Abs(i.Z - loc.Z) > ExplosionRange * 8)
                        continue;
                }

                if (o is Mobile)
                {
                    if (ignore is not null && ignore.Contains(o as Mobile))
                        continue;

                    // ignore hidden staff
                    if (o is PlayerMobile pm && pm.AccessLevel > AccessLevel.Player && pm.Hidden)
                        continue;

                    toExplode.Add(o);
                    ++mobilesToDamage;
                }
                else if (o is BaseExplosionPotion && o != this)
                {
                    toExplode.Add(o);
                }
            }

            eable.Free();

            return toExplode;
        }
        public bool GetFlag(PotFlags fb)
        {
            if ((m_Flags & fb) > 0) return true;
            else return false;
        }

        public void SetFlag(PotFlags fb)
        {
            m_Flags |= fb;
        }

        public void ClearFlag(PotFlags fb)
        {
            m_Flags &= ~fb;
        }
    }
}