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

/* Scripts\Spells\UnsummonTimer.cs
 * ChangeLog
 *	7/16/10, adam
 *		call the new virtual function OnBeforeDispel() to allow the summon to cleanup
 */

using Server.Mobiles;
using System;

namespace Server.Spells
{
    class UnsummonTimer : Timer
    {
        private BaseCreature m_Creature;
        private Mobile m_Caster;

        public UnsummonTimer(Mobile caster, BaseCreature creature, TimeSpan delay)
            : base(delay)
        {
            m_Caster = caster;
            m_Creature = creature;
            Priority = TimerPriority.OneSecond;
        }

        protected override void OnTick()
        {
            if (!m_Creature.Deleted)
            {
                m_Creature.OnBeforeDispel(m_Caster);
                m_Creature.Delete();
            }
        }
    }
}