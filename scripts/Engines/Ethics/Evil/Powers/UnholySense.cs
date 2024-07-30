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

/* Scripts\Engines\Ethics\Evil\Powers\UnholySense.cs
 * ChangeLog
 *  1/9/23, Yoar
 *      Rewrote spam trap using Mobile.BeginAction
 *  5/22/11, Adam
 *		- add a spam trap.
 *		Since thie power is 0 cost and since it enumerates all opposition, we want to curb spam loading the server
 */

using System;
using System.Text;

namespace Server.Ethics.Evil
{
    public sealed class UnholySense : Power
    {
        public UnholySense()
        {
            m_Definition = new PowerDefinition(
                    Core.NewEthics ? 1 : 0,
                    2,
                    "Detect Good",
                    "Drewrok Erstok",   // powerwords swapped in RunUO with ("Drewrok Velgo")
                    "Searches within a short range for heroes. The search range is proportional to your life force."
                );
        }

        public override void BeginInvoke(Player from)
        {
            // spam trap
            if (!BeginLock(from.Mobile))
                return;

            Ethic opposition = Ethic.Hero;

            int enemyCount = 0;

            int maxRange = 18 + from.Power;

            Player primary = null;

            foreach (Player pl in opposition.Players)
            {
                Mobile mob = pl.Mobile;

                if (mob == null || mob.Map != from.Mobile.Map || !mob.Alive)
                    continue;

                if (!mob.InRange(from.Mobile, Math.Max(18, maxRange - pl.Power)))
                    continue;

                if (primary == null || pl.Power > primary.Power)
                    primary = pl;

                ++enemyCount;
            }

            // different message for old ethics system when there are no enemies
            // we are not sure of the message when there are enemies, so we will go with the later ethics messages
            if (Core.OldEthics)
                if (enemyCount == 0)
                {
                    from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x59, false, "There are no enemies afoot.");
                    FinishInvoke(from);
                    return;
                }

            StringBuilder sb = new StringBuilder();

            sb.Append("You sense ");
            sb.Append(enemyCount == 0 ? "no" : enemyCount.ToString());
            sb.Append(enemyCount == 1 ? " enemy" : " enemies");

            if (primary != null)
            {
                sb.Append(", and a strong presense");

                switch (from.Mobile.GetDirectionTo(primary.Mobile))
                {
                    case Direction.West:
                        sb.Append(" to the west.");
                        break;
                    case Direction.East:
                        sb.Append(" to the east.");
                        break;
                    case Direction.North:
                        sb.Append(" to the north.");
                        break;
                    case Direction.South:
                        sb.Append(" to the south.");
                        break;

                    case Direction.Up:
                        sb.Append(" to the north-west.");
                        break;
                    case Direction.Down:
                        sb.Append(" to the south-east.");
                        break;
                    case Direction.Left:
                        sb.Append(" to the south-west.");
                        break;
                    case Direction.Right:
                        sb.Append(" to the north-east.");
                        break;
                }
            }
            else
            {
                sb.Append('.');
            }

            from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x59, false, sb.ToString());

            FinishInvoke(from);
        }

        public static bool BeginLock(Mobile m)
        {
            if (m.BeginAction(typeof(UnholySense)))
            {
                Timer.DelayCall(TimeSpan.FromSeconds(0.7), EndLock, m);
                return true;
            }

            return false;
        }

        public static void EndLock(object state)
        {
            Mobile m = state as Mobile;

            if (m != null)
                m.EndAction(typeof(UnholySense));
        }
    }
}