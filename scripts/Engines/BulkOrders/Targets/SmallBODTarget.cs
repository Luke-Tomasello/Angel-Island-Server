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

using Server.Targeting;

namespace Server.Engines.BulkOrders
{
    public class SmallBODTarget : Target
    {
        private SmallBOD m_Deed;

        public SmallBODTarget(SmallBOD deed)
            : base(18, false, TargetFlags.None)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Deed.Deleted || !m_Deed.IsChildOf(from.Backpack))
                return;

            m_Deed.EndCombine(from, targeted);
        }
    }
}