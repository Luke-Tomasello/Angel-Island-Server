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

using Server.Items;
using System;

namespace Server.Engines.Plants
{
    public class PlantResourceInfo
    {
        private static PlantResourceInfo[] m_ResourceList = new PlantResourceInfo[]
            {
                new PlantResourceInfo( PlantType.ElephantEarPlant, PlantHue.BrightRed, typeof( RedLeaves ) ),
                new PlantResourceInfo( PlantType.PonytailPalm, PlantHue.BrightRed, typeof( RedLeaves ) ),
                new PlantResourceInfo( PlantType.CenturyPlant, PlantHue.BrightRed, typeof( RedLeaves ) ),
                new PlantResourceInfo( PlantType.Poppies, PlantHue.BrightOrange, typeof( OrangePetals ) ),
                new PlantResourceInfo( PlantType.Bulrushes, PlantHue.BrightOrange, typeof( OrangePetals ) ),
                new PlantResourceInfo( PlantType.PampasGrass, PlantHue.BrightOrange, typeof( OrangePetals ) ),
                new PlantResourceInfo( PlantType.SnakePlant, PlantHue.BrightGreen, typeof( GreenThorns ) ),
                new PlantResourceInfo( PlantType.BarrelCactus, PlantHue.BrightGreen, typeof( GreenThorns ) )
            };

        public static PlantResourceInfo GetInfo(PlantType plantType, PlantHue plantHue)
        {
            foreach (PlantResourceInfo info in m_ResourceList)
            {
                if (info.PlantType == plantType && info.PlantHue == plantHue)
                    return info;
            }

            return null;
        }

        private PlantType m_PlantType;
        private PlantHue m_PlantHue;
        private Type m_ResourceType;

        public PlantType PlantType { get { return m_PlantType; } }
        public PlantHue PlantHue { get { return m_PlantHue; } }
        public Type ResourceType { get { return m_ResourceType; } }

        private PlantResourceInfo(PlantType plantType, PlantHue plantHue, Type resourceType)
        {
            m_PlantType = plantType;
            m_PlantHue = plantHue;
            m_ResourceType = resourceType;
        }

        public Item CreateResource()
        {
            return (Item)Activator.CreateInstance(m_ResourceType);
        }
    }
}