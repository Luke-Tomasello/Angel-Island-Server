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

/* Scripts\Engines\Township\MoveStoneTarget.cs
 * CHANGELOG:
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Gumps;
using Server.Items;
using Server.Targeting;

namespace Server.Township
{
    public class MoveStoneTarget : Target
    {
        public static void BeginMoveStone(TownshipStone stone, Mobile from)
        {
            if ((stone != null && stone.Guild != null && stone.Guild.FixedGuildmaster == false) || (from != null && from.AccessLevel > AccessLevel.GameMaster))
            {
                from.SendMessage("Target the spot within your township where you want your stone to reside.");
                from.Target = new MoveStoneTarget(stone);
            }
            else
                from.SendMessage("{0} is staff-owned, and as such, it may not be moved in this way.", stone.Guild.Name);
        }

        private TownshipStone m_Stone;

        private MoveStoneTarget(TownshipStone stone)
            : base(-1, true, TargetFlags.None)
        {
            m_Stone = stone;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Leader))
                return;

            Point3D loc = new Point3D(targeted as IPoint3D);

            TownshipBuilder.Options.Configure(TownshipBuilder.BuildFlag.AllowInsideHouse | TownshipBuilder.BuildFlag.IgnoreGuildedPercentage | TownshipBuilder.BuildFlag.NeedsSurface, TownshipSettings.DecorationClearance, m_Stone);

            if (TownshipBuilder.Validate(from, loc, from.Map, m_Stone))
            {
                m_Stone.MoveToWorld(loc);

                from.SendMessage("You have successfully moved the township stone.");
            }

            from.CloseGump(typeof(TownshipGump));
            from.SendGump(new TownshipGump(m_Stone, from));
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Leader))
                return;

            from.CloseGump(typeof(TownshipGump));
            from.SendGump(new TownshipGump(m_Stone, from));
        }
    }
}