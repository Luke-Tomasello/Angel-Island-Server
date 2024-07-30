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

/* Items/Wands/WandTarget.cs
 * CHANGELOG:
 *  3/27/23, Yoar
 *      No longer needed
 *  3/27/23, Yoar
 *      Removed IWand interface
 *	1/6/23, Yoar: IWand interface
 *	    Rewrote WandTarget so that it supports IWand
 */

#if false
using Server.Items;
using System;

namespace Server.Targeting
{
    [Obsolete]
    public class WandTarget : Target
    {
        private BaseWand m_Wand;

        public WandTarget(BaseWand wand) : base(6, false, TargetFlags.None)
        {
            m_Wand = wand;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_Wand.Deleted || m_Wand.Parent != from || m_Wand.MagicEffect == MagicItemEffect.None || m_Wand.MagicCharges <= 0 || !from.CanBeginAction(MagicItems.GetLock(from, m_Wand)) || targeted is StaticTarget || targeted is LandTarget)
                return;

            if (m_Wand.OnWandTarget(from, targeted))
                MagicItems.ConsumeCharge(from, m_Wand);
        }
    }
}
#endif