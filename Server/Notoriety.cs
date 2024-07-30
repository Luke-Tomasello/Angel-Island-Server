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

/* Server/Notoriety.cs
 * CHANGELOG:
 *	8/01/07: Pix
 *		Added NotorietyBeneficialActsHandler so that we can inform players
 *		of consequences for beneficial actions on targets by huing them differently.
 */

namespace Server
{
    public delegate int NotorietyHandler(Mobile source, Mobile target);
    public delegate int NotorietyBeneficialActsHandler(Mobile source, Mobile target);

    public class Notoriety
    {
        public const int Innocent = 1;
        public const int Ally = 2;
        public const int CanBeAttacked = 3;
        public const int Criminal = 4;
        public const int Enemy = 5;
        public const int Murderer = 6;
        public const int Invulnerable = 7;

        private static NotorietyHandler m_Handler;
        private static NotorietyBeneficialActsHandler m_BeneficialHandler;

        public static NotorietyHandler Handler
        {
            get { return m_Handler; }
            set { m_Handler = value; }
        }

        public static NotorietyBeneficialActsHandler BeneficialActsHandler
        {
            get { return m_BeneficialHandler; }
            set { m_BeneficialHandler = value; }
        }

        private static int[] m_Hues = new int[]
            {
                0x000,
                0x059,
                0x03F,
                0x3B2,
                0x3B2,
                0x090,
                0x022,
                0x035
            };

        public static int[] Hues
        {
            get { return m_Hues; }
            set { m_Hues = value; }
        }

        public static int GetHue(int noto)
        {
            if (noto < 0 || noto >= m_Hues.Length)
                return 0;

            return m_Hues[noto];
        }

        public static int Compute(Mobile source, Mobile target)
        {
            int noto = m_Handler == null ? CanBeAttacked : m_Handler(source, target);

            return noto;
        }

        public static int GetBeneficialHue(Mobile source, Mobile target)
        {
            if (m_BeneficialHandler == null)
            {
                return 0;
            }
            else
            {
                return m_BeneficialHandler(source, target);
            }
        }
    }
}