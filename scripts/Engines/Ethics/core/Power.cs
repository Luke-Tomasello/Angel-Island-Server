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

namespace Server.Ethics
{
    public abstract class Power
    {
        protected PowerDefinition m_Definition;

        public PowerDefinition Definition { get { return m_Definition; } }

        public virtual bool CheckInvoke(Player from)
        {
            if (!from.Mobile.CheckAlive())
                return false;

            if (from.Power < m_Definition.Power)
            {   // from ciloc-1.txt
                from.Mobile.SendLocalizedMessage(501074); // You lack the necessary life force.
                return false;
            }

            // question(1): from message boards - ok
            // question(2): from message boards - ok
            if (Core.OldEthics && m_Definition.Sphere > from.Power / 10)
            {   // from ciloc-1.txt
                from.Mobile.SendLocalizedMessage(501073); // This power is beyond your ability.
                return false;
            }

            return true;
        }

        public abstract void BeginInvoke(Player from);

        public virtual void FinishInvoke(Player from)
        {
            from.Power -= m_Definition.Power;

            Factions.NewGumps.Ethics.EthicsGump.Update(from);
        }
    }
}