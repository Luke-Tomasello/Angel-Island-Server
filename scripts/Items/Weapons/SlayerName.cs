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

/* Scripts\Items\Weapons\SlayerName.cs
 * CHANGELOG
 * 6/25/23, Yoar
 *      Now using Cliloc data
 * 2010.05.22 - Pix
 *      Added utility class SlayerLabel for 'naming' the slayer enums.
 */

using Server.Text;

namespace Server.Items
{
    public enum SlayerName
    {
        None,
        Silver,
        OrcSlaying,
        TrollSlaughter,
        OgreTrashing,
        Repond,
        DragonSlaying,
        Terathan,
        SnakesBane,
        LizardmanSlaughter,
        ReptilianDeath,
        DaemonDismissal,
        GargoylesFoe,
        BalronDamnation,
        Exorcism,
        Ophidian,
        SpidersDeath,
        ScorpionsBane,
        ArachnidDoom,
        FlameDousing,
        WaterDissipation,
        Vacuum,
        ElementalHealth,
        EarthShatter,
        BloodDrinking,
        SummerWind,
        ElementalBan // Bane?
    }

    public static class SlayerLabel
    {
        public static int GetNumber(SlayerName name)
        {
            return 1017383 + (int)name; // Silver | ...
        }

        public static string GetString(SlayerName name)
        {
            int number = GetNumber(name);

            string str;

            if (Cliloc.Lookup.TryGetValue(number, out str))
                return str;

            return null;
        }

        public static string GetSuffix(SlayerName name)
        {
            switch (name)
            {
                // 6/25/23, Yoar: Added several special cases
                case SlayerName.Ophidian:
                    {
                        return "ophidian slaying";
                    }
                case SlayerName.Terathan:
                    {
                        return "terathan slaying";
                    }
                default:
                    {
                        string suffix = GetString(name);

                        if (suffix != null)
                            suffix = suffix.ToLower();

                        return suffix;
                    }
            }
        }
    }
}