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

namespace Server.Engines.Harvest
{
    public class HarvestSoundTimer : Timer
    {
        private Mobile m_From;
        private Item m_Tool;
        private HarvestSystem m_System;
        private HarvestDefinition m_Definition;
        private object m_ToHarvest, m_Locked;
        private bool m_Last;

        public HarvestSoundTimer(Mobile from, Item tool, HarvestSystem system, HarvestDefinition def, object toHarvest, object locked, bool last)
            : base(def.EffectSoundDelay)
        {
            m_From = from;
            m_Tool = tool;
            m_System = system;
            m_Definition = def;
            m_ToHarvest = toHarvest;
            m_Locked = locked;
            m_Last = last;
        }

        protected override void OnTick()
        {
            m_System.DoHarvestingSound(m_From, m_Tool, m_Definition, m_ToHarvest);

            if (m_Last)
                m_System.FinishHarvesting(m_From, m_Tool, m_Definition, m_ToHarvest, m_Locked);
        }
    }
}