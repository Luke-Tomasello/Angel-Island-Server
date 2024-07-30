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

/* Scripts\Engines\ConPVP\SafeZone.cs
 * Changelog:
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 */

using Server.Mobiles;
using Server.Regions;

namespace Server.Engines.ConPVP
{
    public class SafeZone : GuardedRegion
    {
        public static readonly int SafeZonePriority = HouseRegion.HousePriority + 1;

        private bool m_IsGuarded;

        public override bool IsGuarded
        {
            get { return m_IsGuarded; }
            set { m_IsGuarded = value; }
        }

        /*public override bool AllowReds{ get{ return true; } }*/

        public SafeZone(string name, Rectangle2D area, Point3D goloc, Map map, bool isGuarded) : base("", name, map, typeof(WarriorGuard))
        {
            Priority = SafeZonePriority;
            GoLocation = goloc;

            m_IsGuarded = isGuarded;

            Coords.Add(ConvertTo3D(area)); // set last such that the region is registered with the correct props
        }

        public override bool AllowHousing(Point3D p)
        {
            return false;
        }

        public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            if (m.Player && Factions.Sigil.ExistsOn(m))
            {
                m.SendMessage(0x22, "You are holding a sigil and cannot enter this zone.");
                return false;
            }

            if (m.Player && Engines.Alignment.TheFlag.ExistsOn(m))
            {
                m.SendMessage(0x22, "You are holding a flag and cannot enter this zone.");
                return false;
            }

            PlayerMobile pm = m as PlayerMobile;

            if (pm == null && m is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)m;

                if (bc.Summoned)
                    pm = bc.SummonMaster as PlayerMobile;
            }

            if (pm != null && pm.DuelContext != null && pm.DuelContext.StartedBeginCountdown)
                return true;

            if (DuelContext.CheckCombat(m))
            {
                m.SendMessage(0x22, "You have recently been in combat and cannot enter this zone.");
                return false;
            }

            return base.OnMoveInto(m, d, newLocation, oldLocation);
        }

        public override bool CanUseStuckMenu(Mobile m, bool quiet = false)
        {
            return false;
        }
    }
}