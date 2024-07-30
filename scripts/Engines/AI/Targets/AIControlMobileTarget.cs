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

using Server.Mobiles;
using Server.Targeting;
using System.Collections;

namespace Server.Targets
{
    public class AIControlMobileTarget : Target
    {
        private ArrayList m_List;
        private OrderType m_Order;

        public OrderType Order
        {
            get
            {
                return m_Order;
            }
        }

        public AIControlMobileTarget(BaseAI ai, OrderType order)
            : base(-1, false, TargetFlags.None)
        {
            m_List = new ArrayList();
            m_Order = order;

            AddAI(ai);
        }

        public void AddAI(BaseAI ai)
        {
            if (!m_List.Contains(ai))
                m_List.Add(ai);
        }

        protected override void OnTarget(Mobile from, object o)
        {
            if (o is Mobile)
            {
                for (int i = 0; i < m_List.Count; ++i)
                    ((BaseAI)m_List[i]).EndPickTarget(from, (Mobile)o, m_Order);
            }
        }
    }
}