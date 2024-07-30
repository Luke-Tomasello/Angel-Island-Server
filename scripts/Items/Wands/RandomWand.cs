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

using System;

namespace Server.Items
{
    [Obsolete("Use Wand class instead")]
    public class RandomWand
    {
        public static BaseWand CreateWand()
        {
            return CreateRandomWand();
        }

        public static BaseWand CreateRandomWand()
        {
            switch (Utility.Random(11))
            {
                default:
                case 0: return new ClumsyWand();
                case 1: return new FeebleWand();
                case 2: return new FireballWand();
                case 3: return new GreaterHealWand();
                case 4: return new HarmWand();
                case 5: return new HealWand();
                case 6: return new IDWand();
                case 7: return new LightningWand();
                case 8: return new MagicArrowWand();
                case 9: return new ManaDrainWand();
                case 10: return new WeaknessWand();
            }
        }
    }
}