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

/* Scripts\Engines\Breeding\GeneAttribute.cs
 * Changelog:
 * 10/20/21, Yoar: Breeding System overhaul
 *      Initial version.
 */

using System;

namespace Server.Mobiles
{
    public enum GeneVisibility
    {
        Invisible = 0,
        Tame = 1,
        Wild = 2
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class GeneAttribute : Attribute
    {
        public const double DefaultMinVariance = 0.001; // just a small number to ensure variation of ints

        private string m_Name;
        private double m_LowFactor, m_HighFactor;
        private double m_SpawnMin, m_SpawnMax;
        private double m_BreedMin, m_BreedMax;
        private GeneVisibility m_Visibility;
        private double m_MinVariance;

        public string Name { get { return m_Name; } }
        public double LowFactor { get { return m_LowFactor; } }
        public double HighFactor { get { return m_HighFactor; } }
        public double SpawnMin { get { return m_SpawnMin; } }
        public double SpawnMax { get { return m_SpawnMax; } }
        public double BreedMin { get { return m_BreedMin; } }
        public double BreedMax { get { return m_BreedMax; } }
        public GeneVisibility Visibility { get { return m_Visibility; } }
        public double MinVariance { get { return m_MinVariance; } }

        public GeneAttribute(string name, double spawnMin, double spawnMax, double breedMin, double breedMax)
            : this(name, 0.05, 0.05, spawnMin, spawnMax, breedMin, breedMax, GeneVisibility.Invisible, DefaultMinVariance)
        {
        }

        public GeneAttribute(string name, double spawnMin, double spawnMax, double breedMin, double breedMax, GeneVisibility visibility)
            : this(name, 0.05, 0.05, spawnMin, spawnMax, breedMin, breedMax, visibility, DefaultMinVariance)
        {
        }

        public GeneAttribute(string name, double spawnMin, double spawnMax, double breedMin, double breedMax, GeneVisibility visibility, double minVariance)
            : this(name, 0.05, 0.05, spawnMin, spawnMax, breedMin, breedMax, visibility, minVariance)
        {
        }

        public GeneAttribute(string name, double lowFactor, double highFactor, double spawnMin, double spawnMax, double breedMin, double breedMax)
            : this(name, lowFactor, highFactor, spawnMin, spawnMax, breedMin, breedMax, GeneVisibility.Invisible, DefaultMinVariance)
        {
        }

        public GeneAttribute(string name, double lowFactor, double highFactor, double spawnMin, double spawnMax, double breedMin, double breedMax, GeneVisibility visibility)
            : this(name, lowFactor, highFactor, spawnMin, spawnMax, breedMin, breedMax, visibility, DefaultMinVariance)
        {
        }

        public GeneAttribute(string name, double lowFactor, double highFactor, double spawnMin, double spawnMax, double breedMin, double breedMax, GeneVisibility visibility, double minVariance)
        {
            m_Name = name;
            m_LowFactor = lowFactor;
            m_HighFactor = highFactor;
            m_SpawnMin = spawnMin;
            m_SpawnMax = spawnMax;
            m_BreedMin = breedMin;
            m_BreedMax = breedMax;
            m_Visibility = visibility;
            m_MinVariance = minVariance;

            // sanity
            EnsureMinMax(ref m_SpawnMin, ref m_SpawnMax);
            EnsureMinMax(ref m_BreedMin, ref m_BreedMax);
            m_MinVariance = Math.Max(0.0, m_MinVariance);
        }

        private static void EnsureMinMax(ref double min, ref double max)
        {
            if (min > max)
            {
                double temp = min;
                min = max;
                max = temp;
            }
        }
    }
}