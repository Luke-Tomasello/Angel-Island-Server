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

/* Scripts\Spells\Seventh\MeteorSwarm.cs
 * ChangeLog:
 *  8/25/21, Adam (TargetExploitCheck)
 *      We apply late binding LOS checks to thwart exploitative targeting techniques
 *      See full explanation in BaseHouse.cs
 *	7/2/10, Adam
 *		add target 'under house' exploit check
 *		if ((target is Server.Targeting.LandTarget && Server.Multis.BaseHouse.FindHouseAt(((Server.Targeting.LandTarget)(target)).Location, Caster.Map, 16) != null))
 *			target cannot be seen
 * 	6/5/04, Pix
 * 		Merged in 1.0RC0 code.
 */

using Server.Multis;
using Server.Targeting;
using System.Collections;

namespace Server.Spells.Seventh
{
    public class MeteorSwarmSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Meteor Swarm", "Flam Kal Des Ylem",
                SpellCircle.Seventh,
                233,
                9042,
                false,
                Reagent.Bloodmoss,
                Reagent.MandrakeRoot,
                Reagent.SulfurousAsh,
                Reagent.SpidersSilk
            );

        public MeteorSwarmSpell(Mobile caster, Item scroll)
            : base(caster, scroll, m_Info)
        {
        }

        public override void OnCast()
        {
            Caster.Target = new InternalTarget(this);
        }

        public override bool DelayedDamage { get { return true; } }

        public void Target(IPoint3D p)
        {
            // adam: add target 'under house' exploit check
            if (!Caster.CanSee(p) || (p is LandTarget && BaseHouse.FindHouseAt(((LandTarget)(p)).Location, Caster.Map, 16) != null))
            {
                Caster.SendLocalizedMessage(500237); // Target can not be seen.
            }
            else if (SpellHelper.CheckTown(p, Caster) && CheckSequence())
            {
                SpellHelper.Turn(Caster, p);

                if (p is Item)
                    p = ((Item)p).GetWorldLocation();

                double damage;

                if (Core.RuleSets.AOSRules())
                    damage = GetNewAosDamage(48, 1, 5);
                else
                    damage = Utility.Random(27, 22);

                ArrayList targets = new ArrayList();

                Map map = Caster.Map;

                if (map != null)
                {
                    IPooledEnumerable eable = map.GetMobilesInRange(new Point3D(p), 2);

                    foreach (Mobile m in eable)
                    {
                        // See full explanation in BaseHouse.cs
                        if (BaseHouse.TargetExploitCheck(this.Name, Caster, m, new Point3D(p)) != BaseHouse.ExploitType.None)
                        {   // we've already sent the caster a message
                            FinishSequence();
                            return;
                        }
                        else if (BaseHouse.ProximityToDamage(Caster, m) <= 2)
                            if (Caster != m && SpellHelper.ValidIndirectTarget(Caster, m) && Caster.CanBeHarmful(m, false))
                                targets.Add(m);
                    }
                    eable.Free();
                }

                if (targets.Count > 0)
                {
                    Effects.PlaySound(p, Caster.Map, 0x160);

                    if (Core.RuleSets.AOSRules() && targets.Count > 1)
                        damage = (damage * 2) / targets.Count;
                    else if (!Core.RuleSets.AOSRules())
                        damage /= targets.Count;

                    for (int i = 0; i < targets.Count; ++i)
                    {
                        Mobile m = (Mobile)targets[i];

                        double toDeal = damage;

                        if (!Core.RuleSets.AOSRules() && CheckResisted(m))
                        {
                            toDeal *= GetResistScaler(0.5);

                            m.SendLocalizedMessage(501783); // You feel yourself resisting magical energy.
                        }

                        if (toDeal < 7)
                            toDeal = 7;

                        Caster.DoHarmful(m);
                        SpellHelper.Damage(this, m, toDeal, 0, 100, 0, 0, 0);

                        Caster.MovingParticles(m, 0x36D4, 7, 0, false, true, 9501, 1, 0, 0x100);
                    }
                }
            }

            FinishSequence();
        }

        #region TargetExploitCheck(m)
#if UNUSED
        public bool TargetExploitCheck(Mobile caster, Mobile m, Point3D target_loc)
        {
            Server.Multis.BaseHouse hm = Server.Multis.BaseHouse.FindHouseAt(m);
            if (hm == null)
                return false;

            ArrayList c = hm.Region.Coords;
            Point2D p = new Point2D(m.Location);
            for (int i = 0; i < c.Count; i++)
            {
                if (c[i] is Rectangle2D)
                    if (((Rectangle2D)c[i]).Contains(m.Location))
                    {
                        Rectangle2D rect = (Rectangle2D)c[i];

        #region logging
                        // TargetExploitCheck of player
                        LogHelper Logger = new LogHelper("MeteorSwarmExploit.log", false);
                        Point3D target;
                        // DistanceToTarget target of cast
                        target = target_loc;
                        Logger.Log(LogType.Text, string.Format("Attacker {0} distance to target: {1}", caster, caster.GetDistanceToSqrt(target)));
                        Logger.Log(LogType.Text,string.Format("Victim {0} distance to target: {1}", m, m.GetDistanceToSqrt(target)));

                        if (caster.Map.LineOfSight(caster, target_loc, true))
                            Logger.Log(LogType.Text, string.Format("Caster see: {0}", target_loc));
                        else
                            Logger.Log(LogType.Text, string.Format("Caster cannot see: {0}", target_loc));

                        Logger.Finish();
        #endregion logging

                        // must be able to see what you are targeting
                        if (caster.Map.LineOfSight(caster, target_loc, true) == false)
                        {   // Target can not be seen.

                            caster.SendLocalizedMessage(500237); // Target can not be seen.
                            // exploit! They somehow circumvented the LOS checks. Probably done on the client, and and then fudged packets on the way back to the server.
                            //  We double down on the LOS checks here, server-side
                            //Logger = new LogHelper("MeteorSwarmExploit.log", false);
                            //Logger.Log(LogType.Mobile, caster, string.Format("exploit! They somehow circumvented the LOS checks in MeteorSwarm."));
                            //Logger.Finish();
                            // jail time
                            //Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Tick), new object[] { caster, "They somehow circumvented the LOS checks in MeteorSwarm." });
                            //Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                            //Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. They somehow circumvented the LOS checks in MeteorSwarm.", caster as Mobiles.PlayerMobile));
                            return true;
                        }

                        // 12 is the max range on the spell, 2 is the area effect from the targeted item
                        if (caster.GetDistanceToSqrt(target) + m.GetDistanceToSqrt(target) > (12+2))
                        {   // That is too far away.
                            caster.SendLocalizedMessage(500446); // That is too far away.
                            // exploit! They somehow circumvented the range checks. Probably done on the client, and and then fudged packets on the way back to the server.
                            //  We double down on the range checks here, server-side
                            //Logger = new LogHelper("MeteorSwarmExploit.log", false);
                            //Logger.Log(LogType.Mobile, caster, string.Format("exploit! They somehow circumvented the range checks in MeteorSwarm."));
                            //Logger.Finish();
                            // jail time
                            //Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(Tick), new object[] { caster, "They somehow circumvented the range checks in MeteorSwarm." });
                            //Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                            //Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. They somehow circumvented the range checks in MeteorSwarm.", caster as Mobiles.PlayerMobile));
                            return true;
                        }
                    }
            }

            return false;
        }
        private void Tick(object state)
        {
            object[] aState = (object[])state;
            Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(aState[0] as Mobiles.PlayerMobile, 3, aState[1] as string, false);
            jt.GoToJail();
        }
#endif
        #endregion TargetExploitCheck(m)

        private class InternalTarget : Target
        {
            private MeteorSwarmSpell m_Owner;

            public InternalTarget(MeteorSwarmSpell owner)
                : base(12, true, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object o)
            {
                IPoint3D p = o as IPoint3D;

                if (p != null)
                    m_Owner.Target(p);
            }

            protected override void OnTargetFinish(Mobile from)
            {
                m_Owner.FinishSequence();
            }
        }
    }
}