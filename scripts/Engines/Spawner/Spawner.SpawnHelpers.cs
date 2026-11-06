/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\Spawner\Spawner.SpawnHelpers.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Engines.Alignment;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        public static void MakeExhibit(BaseCreature bc)
        {
            bc.IsInvulnerable = true;
            bc.Blessed = false;
            bc.BardImmune = true;
            bc.FightMode = FightMode.None;
            if (bc.CanOverrideAI)
                bc.AI = AIType.AI_Animal;
            bc.Tamable = false;
            bc.HerdingImmune = true;
            bc.Fame = bc.Karma = 0;
            bc.IOBAlignment = IOBAlignment.None;
            bc.GuildAlignment = AlignmentType.None;
        }
        public static void MakeDummy(BaseCreature bc)
        {
            bc.BlockDamage = true;
            bc.SetMobileBool(Mobile.MobileBoolTable.TrainingMobile, true);   // necessary if you wish to gain skill off a BlockDamage creature
            bc.FightMode = FightMode.None;
            if (bc.CanOverrideAI)
                bc.AI = AIType.AI_Animal;
            bc.DamageMin = bc.DamageMax = 1;
            bc.Tamable = false;
            bc.HerdingImmune = true;
            bc.Fame = bc.Karma = 0;
            bc.IOBAlignment = IOBAlignment.None;
            bc.GuildAlignment = AlignmentType.None;
        }
        public bool HighPowerTameNearby()
        {// only spawn paragons if we are getting farmed by a highpower tamer/tame
            // the the tamer is closest enough to see the creature spawn, we can see him! (well, his pet)
            IPooledEnumerable eable = Map.GetMobilesInRange(this.Location, Map.MaxLOSDistance);
            foreach (Mobile m in eable)
            {
                // ignore staff
                if (m.AccessLevel > AccessLevel.Player)
                    continue;

                // if a creature 
                if (m is BaseCreature)
                {   // if controlled (summoned or tame)
                    if (((m as BaseCreature).Controlled && (m as BaseCreature).ControlMaster != null) || ((m as BaseCreature).Summoned && (m as BaseCreature).SummonMaster != null))
                    {   // only trap tame dragons and daemons for now
                        if (m is Dragon || m is Daemon)
                        {
                            eable.Free();
                            return true;
                        }
                    }
                }

            }
            eable.Free();

            return false;
        }
        public static bool ClearPathLand(Point3D location, Point3D goal, Map map, bool canOpenDoors = true)
        {
            // can we get there from here?
            // that is, could a human walk from the proposed treasure map location to this nearby spawner.
            Movement.MovementObject obj_start = new Movement.MovementObject(location, goal, map, canOpenDoors: canOpenDoors);
            if (!MovementPath.PathTo(obj_start))
                return false;   // can't get there from here
            else
                return true;
        }
        public static bool ClearPath(Point3D location, List<Point2D> goals, int z, Map map, bool canOpenDoors = true)
        {
            foreach (Point2D p in goals)
            {
                Point3D px = new Point3D(p.X, p.Y, z);
                if (ClearPathLand(location, px, map, canOpenDoors))
                    return true;
            }
            return false;
        }
    }
}
