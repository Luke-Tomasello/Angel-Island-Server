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

namespace Server.Engines.Plants
{
    public class PlantPourTarget : Target
    {
        private PlantItem m_Plant;

        public PlantPourTarget(PlantItem plant)
            : base(3, true, TargetFlags.None)
        {
            m_Plant = plant;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!m_Plant.Deleted && from.InRange(m_Plant.GetWorldLocation(), 3) && targeted is Item)
            {
                m_Plant.Pour(from, (Item)targeted);
            }
        }

        protected override void OnTargetFinish(Mobile from)
        {
            if (!m_Plant.Deleted && m_Plant.PlantStatus < PlantStatus.DecorativePlant && from.InRange(m_Plant.GetWorldLocation(), 3) && m_Plant.IsUsableBy(from))
            {
                from.SendGump(new MainPlantGump(m_Plant));
            }
        }
    }
}