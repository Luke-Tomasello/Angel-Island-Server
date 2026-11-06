/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

using Server.Multis;
using Server.Multis.Deeds;
using System;

namespace Server.Misc
{
    public class SiegeHelper
    {

        public static int SiegePriceRules(Type type, int price)
        {
            if (!Core.RuleSets.SiegeRules())
                return price;

            // Siege uses special NPC pricing rules.
            //
            // Most goods are sold at 3x normal shard price.
            // Houses and boats have special historical handling, so route them
            // through dedicated helpers to keep buy/sell policy centralized.
            if (typeof(HouseDeed).IsAssignableFrom(type))
                return ComputeHousingMarkupForSiege(price);

            if (typeof(BaseBoatDeed).IsAssignableFrom(type))
                return ComputeBoatMarkupForSiege(price);

            return price * 3;
        }

        public static int ComputeHousingMarkupForSiege(int price)
        {
            // Pub 13.6:
            // On Siege Perilous, house deeds purchased from NPC shopkeepers are
            // 10x the prices seen on normal shards. All other NPC shopkeeper sales
            // remain at 3x normal.
            //
            // 1/19/2024, Adam:
            // SiegeII intentionally uses 3x housing prices, even though that is not
            // era-accurate, in order to help spur the economy on a very small shard.
            //
            // Reference:
            // http://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
            if (Core.RuleSets.SiegeRules())
            {
                if (PublishInfo.Publish >= 13.6 && !Core.RuleSets.SiegeRules(2))
                    return price * 10;

                return price * 3;
            }

            return price;
        }

        public static int ComputeBoatMarkupForSiege(int price)
        {
            // Historical boat pricing is distinct from housing and deserves its own
            // helper so buy/sell code cannot drift apart.
            //
            // Publish 16 Update 1 notes that houses and boats on Siege Perilous and
            // Mugen became 9x regular shard prices, replacing earlier 10x / 30x
            // periods in later-era history.
            //
            // Our shard interpretation:
            // - Siege proper may use the later 9x boat pricing when operating at
            //   Publish 16 or later.
            // - SiegeII intentionally uses 3x boat pricing to encourage economic
            //   activity on a small shard, matching its housing policy.
            //
            // References:
            // https://www.uoguide.com/Publish_16#Update_1
            // https://www.uoguide.com/Publish_13.6_(Siege_Perilous_Shards_Only)
            if (Core.RuleSets.SiegeRules())
            {
                if (PublishInfo.Publish >= 16 && !Core.RuleSets.SiegeRules(2))
                    return price * 9;

                return price * 3;
            }

            return price;
        }
    }
}