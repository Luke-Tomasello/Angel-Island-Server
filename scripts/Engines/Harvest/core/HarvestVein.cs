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
    public class HarvestVein
    {
        private double m_VeinChance;
        private double m_ChanceToFallback;
        private HarvestResource m_PrimaryResource;
        private HarvestResource m_FallbackResource;

        public double VeinChance { get { return m_VeinChance; } set { m_VeinChance = value; } }
        public double ChanceToFallback { get { return m_ChanceToFallback; } set { m_ChanceToFallback = value; } }
        public HarvestResource PrimaryResource { get { return m_PrimaryResource; } set { m_PrimaryResource = value; } }
        public HarvestResource FallbackResource { get { return m_FallbackResource; } set { m_FallbackResource = value; } }

        public HarvestVein(double veinChance, double chanceToFallback, HarvestResource primaryResource, HarvestResource fallbackResource)
        {
            m_VeinChance = veinChance;
            m_ChanceToFallback = chanceToFallback;
            m_PrimaryResource = primaryResource;
            m_FallbackResource = fallbackResource;
        }
    }
}