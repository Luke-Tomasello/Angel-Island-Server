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
    public class PollinateTarget : Target
    {
        private PlantItem m_Plant;

        public PollinateTarget(PlantItem plant)
            : base(3, true, TargetFlags.None)
        {
            m_Plant = plant;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (!m_Plant.Deleted && m_Plant.PlantStatus < PlantStatus.DecorativePlant && from.InRange(m_Plant.GetWorldLocation(), 3))
            {
                if (!m_Plant.IsUsableBy(from))
                {
                    m_Plant.LabelTo(from, 1061856); // You must have the item in your backpack or locked down in order to use it.
                }
                else if (!PlantHueInfo.IsCrossable(m_Plant.PlantHue))
                {
                    m_Plant.LabelTo(from, 1053050); // You cannot gather pollen from a mutated plant!
                }
                else if (!m_Plant.PlantSystem.PollenProducing)
                {
                    m_Plant.LabelTo(from, 1053051); // You cannot gather pollen from a plant in this stage of development!
                }
                else if (m_Plant.PlantSystem.Health < PlantHealth.Healthy)
                {
                    m_Plant.LabelTo(from, 1053052); // You cannot gather pollen from an unhealthy plant!
                }
                else
                {
                    PlantItem targ = targeted as PlantItem;

                    if (targ == null || targ.PlantStatus >= PlantStatus.DecorativePlant || targ.PlantStatus <= PlantStatus.BowlOfDirt)
                    {
                        m_Plant.LabelTo(from, 1053070); // You can only pollinate other specially grown plants!
                    }
                    else if (!targ.IsUsableBy(from))
                    {
                        targ.LabelTo(from, 1061856); // You must have the item in your backpack or locked down in order to use it.
                    }
                    else if (!PlantHueInfo.IsCrossable(targ.PlantHue))
                    {
                        targ.LabelTo(from, 1053073); // You cannot cross-pollinate with a mutated plant!
                    }
                    else if (!targ.PlantSystem.PollenProducing)
                    {
                        targ.LabelTo(from, 1053074); // This plant is not in the flowering stage. You cannot pollinate it!
                    }
                    else if (targ.PlantSystem.Health < PlantHealth.Healthy)
                    {
                        targ.LabelTo(from, 1053075); // You cannot pollinate an unhealthy plant!
                    }
                    else if (targ.PlantSystem.Pollinated)
                    {
                        targ.LabelTo(from, 1053072); // This plant has already been pollinated!
                    }
                    else if (targ == m_Plant)
                    {
                        targ.PlantSystem.Pollinated = true;
                        targ.PlantSystem.SeedType = m_Plant.PlantType;
                        targ.PlantSystem.SeedHue = m_Plant.PlantHue;

                        targ.LabelTo(from, 1053071); // You pollinate the plant with its own pollen.
                    }
                    else
                    {
                        targ.PlantSystem.Pollinated = true;
                        targ.PlantSystem.SeedType = PlantTypeInfo.Cross(m_Plant.PlantType, targ.PlantType);
                        targ.PlantSystem.SeedHue = PlantHueInfo.Cross(m_Plant.PlantHue, targ.PlantHue);

                        targ.LabelTo(from, 1053076); // You successfully cross-pollinate the plant.
                    }
                }
            }
        }

        protected override void OnTargetFinish(Mobile from)
        {
            if (!m_Plant.Deleted && m_Plant.PlantStatus < PlantStatus.DecorativePlant && m_Plant.PlantStatus != PlantStatus.BowlOfDirt && from.InRange(m_Plant.GetWorldLocation(), 3) && m_Plant.IsUsableBy(from))
            {
                from.SendGump(new ReproductionGump(m_Plant));
            }
        }
    }
}