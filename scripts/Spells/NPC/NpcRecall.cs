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

/* Scripts\Spells\Npc\NpcRecall.cs
 * ChangeLog:
 *	6/18/10, Adam
 *		Update region logic to reflect shift from static to new dynamic regions
 *	7/23/05, Adam
 *		Remove all Necromancy, and Chivalry nonsense
 *	5/10/05, Kit
 *		Initial creation
 */

using System;

namespace Server.Spells.Fourth
{
    public class NpcRecallSpell : Spell
    {
        private static SpellInfo m_Info = new SpellInfo(
                "Recall", "Kal Ort Por",
                SpellCircle.Fourth,
                239,
                9031,
                Reagent.BlackPearl,
                Reagent.Bloodmoss,
                Reagent.MandrakeRoot
            );

        private Point3D RecallLocation = new Point3D(0, 0, 0);
        private Map map;
        public NpcRecallSpell(Mobile caster, Item scroll)
            : this(caster, scroll, new Point3D(0, 0, 0))
        {
        }

        public NpcRecallSpell(Mobile caster, Item scroll, Point3D p)
            : base(caster, scroll, m_Info)
        {
            RecallLocation = p;
            map = caster.Map;
        }


        public override void OnCast()
        {
            Effect(RecallLocation, map, true);
        }

        public override bool CheckCast()
        {
            if (Caster.Criminal)
            {
                Caster.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
                return false;
            }
            else if (SpellHelper.CheckCombat(Caster))
            {
                Caster.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
                return false;
            }

            return SpellHelper.CheckTravel(Caster, TravelCheckType.RecallFrom);
        }

        public void Effect(Point3D loc, Map map, bool checkMulti)
        {
            if (map == null || (!Core.RuleSets.AOSRules() && Caster.Map != map))
            {
                Caster.SendLocalizedMessage(1005569); // You can not recall to another facet.
            }
            else if (!SpellHelper.CheckTravel(Caster, TravelCheckType.RecallFrom))
            {
            }
            else if (!SpellHelper.CheckTravel(Caster, map, loc, TravelCheckType.RecallTo))
            {
            }
            // 8/10/22, Adam: I don't think Core.RedsInTown matters here. 
            else if (Caster.Red && map != Map.Felucca)
            {
                Caster.SendLocalizedMessage(1019004); // You are not allowed to travel there.
            }
            else if (Caster.Criminal)
            {
                Caster.SendLocalizedMessage(1005561, "", 0x22); // Thou'rt a criminal and cannot escape so easily.
            }
            else if (SpellHelper.CheckCombat(Caster))
            {
                Caster.SendLocalizedMessage(1005564, "", 0x22); // Wouldst thou flee during the heat of battle??
            }
            else if (Server.Misc.WeightOverloading.IsOverloaded(Caster))
            {
                Caster.SendLocalizedMessage(502359, "", 0x22); // Thou art too encumbered to move.
            }
            else if (!map.CanSpawnLandMobile(loc.X, loc.Y, loc.Z))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if ((checkMulti && SpellHelper.CheckMulti(loc, map)))
            {
                Caster.SendLocalizedMessage(501942); // That location is blocked.
            }
            else if (CheckSequence())
            {
                if (SpellHelper.IsSpecialRegion(loc) || SpellHelper.IsSpecialRegion(Caster.Location))
                {
                    loc = new Point3D(5295, 1174, 0);
                }

                InternalTimer t = new InternalTimer(this, Caster, loc);
                t.Start();
            }

            FinishSequence();
        }

        private class InternalTimer : Timer
        {
            private Spell m_Spell;
            private Mobile m_Caster;
            private Point3D m_Location;

            public InternalTimer(Spell spell, Mobile caster, Point3D location)
                : base(TimeSpan.FromSeconds(0.75))
            {
                m_Spell = spell;
                m_Caster = caster;
                m_Location = location;

                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                // Since all we have is Felucca, we can assume Map.Felucca here
                m_Caster.PlaySound(0x1FC);
                m_Caster.MoveToWorld(m_Location, Map.Felucca);
                m_Caster.PlaySound(0x1FC);
            }
        }
    }
}