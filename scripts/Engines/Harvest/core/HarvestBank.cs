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

/*
 * CHANGELOG:
 * 
 * 11/17/21, Yoar
 *      Added HarvestDefinition.RandomizeVeins
 * 7/26/21 - Pix
 *      Added "Maximum" property so we can externally check if bank is 'full'
 */

using System;

namespace Server.Engines.Harvest
{
    public class HarvestBank
    {
        private int m_Current;
        private int m_Maximum;
        private DateTime m_NextRespawn;
        private HarvestVein m_Vein, m_DefaultVein;

        HarvestDefinition m_Definition;

        public HarvestDefinition Definition
        {
            get { return m_Definition; }
        }

        public int Current
        {
            get
            {
                CheckRespawn();
                return m_Current;
            }
        }

        public int Maximum
        {
            get
            {
                return m_Maximum;
            }
        }

        public HarvestVein Vein
        {
            get
            {
                CheckRespawn();
                return m_Vein;
            }
            set
            {
                m_Vein = value;
            }
        }

        public HarvestVein DefaultVein
        {
            get
            {
                CheckRespawn();
                return m_DefaultVein;
            }
        }

        public void CheckRespawn()
        {
            if (m_Current == m_Maximum || m_NextRespawn > DateTime.UtcNow)
                return;

            m_Current = m_Maximum;

            if (m_Definition.RandomizeVeins)
            {
                m_DefaultVein = m_Definition.GetVeinFrom(Utility.RandomDouble());
            }

            m_Vein = m_DefaultVein;
        }

        public void Consume(int amount)
        {
            CheckRespawn();

            if (m_Current == m_Maximum)
            {
                double min = m_Definition.MinRespawn.TotalMinutes;
                double max = m_Definition.MaxRespawn.TotalMinutes;
                double rnd = Utility.RandomDouble();

                m_Current = m_Maximum - amount;
                m_NextRespawn = DateTime.UtcNow + TimeSpan.FromMinutes(min + (rnd * (max - min)));
            }
            else
            {
                m_Current -= amount;
            }

            if (m_Current < 0)
                m_Current = 0;
        }

        public HarvestBank(HarvestDefinition def, HarvestVein defaultVein)
        {
            m_Maximum = Utility.RandomMinMax(def.MinTotal, def.MaxTotal);
            m_Current = m_Maximum;
            m_DefaultVein = defaultVein;
            m_Vein = m_DefaultVein;

            m_Definition = def;
        }
    }
}