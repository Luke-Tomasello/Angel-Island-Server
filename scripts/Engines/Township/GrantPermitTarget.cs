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

/* Scripts\Engines\Township\AddFundsTarget.cs
 * CHANGELOG:
 *  8/23/23, Yoar
 *      Initial version.
 */

using Server.Items;
using Server.Targeting;

namespace Server.Township
{
    public class GrantPermitTarget : Target
    {
        public static void BeginGrantPermit(TownshipStone stone, Mobile from)
        {
            from.SendMessage("Who do you wish to grant a building permit?");
            from.Target = new GrantPermitTarget(stone);
        }

        private TownshipStone m_Stone;

        private GrantPermitTarget(TownshipStone stone)
            : base(-1, false, TargetFlags.None)
        {
            m_Stone = stone;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.CoLeader))
                return;

            Mobile targ = targeted as Mobile;

            if (targ == null)
                return;

            if (m_Stone.IsEnemy(targ))
            {
                from.SendMessage("You can't grant a building permit to an enemy of the township!");
            }
            else if (m_Stone.AllowBuilding(targ))
            {
                from.SendMessage("But they are already permitted to build in the township.");
            }
            else
            {
                if (!m_Stone.BuildingPermits.Contains(targ))
                    m_Stone.BuildingPermits.Add(targ);

                from.SendMessage("{0} is now permitted to build in the township.", targ.Name);
                targ.SendMessage("You are now permitted to build in this township.");

                from.CloseGump(typeof(Gumps.TownshipGump));
                from.SendGump(new Gumps.TownshipGump(m_Stone, from, Gumps.TownshipGump.Page.BuildingPermits));
            }
        }

        protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
        {
            if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.CoLeader))
                return;

            from.CloseGump(typeof(Gumps.TownshipGump));
            from.SendGump(new Gumps.TownshipGump(m_Stone, from, Gumps.TownshipGump.Page.BuildingPermits));
        }
    }
}