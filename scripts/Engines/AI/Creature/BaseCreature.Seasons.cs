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

/* Scripts/Engines/AI/Creature/BaseCreature.Seasons.cs
 * ChangeLog
 *  6/1/26, Adam
 *      Created.
 */

using Server.Items;

namespace Server.Mobiles
{
    public partial class BaseCreature
    {
        public static void Configure()
        {
            EventSink.CreatureGenerateLoot += OnBeforeDeathCustomLoot;
        }

        private static void OnBeforeDeathCustomLoot(CreatureGenerateLootEventArgs e)
        {
            BaseCreature? bc = e.Mobile as BaseCreature;

            if (bc == null || bc.Deleted)
                return;

            if (Core.RuleSets.SeasonsRules(2))
                AddSeasonTwoReagents(bc);
        }
        private static void RemoveReagents(BaseCreature bc)
        {
            if (bc.Backpack == null)
                return;

            Item[] items = bc.Backpack.Items.ToArray();

            foreach (Item item in items)
            {
                if (item is BaseReagent)
                    item.Delete();
            }
        }
        private static void AddSeasonTwoReagents(BaseCreature bc)
        {
            if (bc == null || bc.Deleted)
                return;

            /* Season 2 reagent scarcity overlay.
             *
             * These drops are added after normal OSI loot generation so the base creature
             * loot tables remain clean and era-standard.
             *
             * Values were anchored against existing OSI-style PackReg drops and creature
             * Power values:
             *
             *   Ratman        Power  49.47 -> PackReg(1, 4)
             *   Lich          Power  98.37 -> PackReg(8, 12)
             *   FrostNymph    Power  94.56 -> PackReg(8, 12)
             *   PirateWench   Power 119.17 -> PackReg(8, 12)
             *   CouncilMember Power 228.10 -> PackReg(10, 20)
             *
             * BogThing and AncientLich were reviewed but not used as direct scaling
             * anchors: BogThing is a high-power non-caster with modest regs, while
             * AncientLich is a deliberate jackpot/outlier.
             *
             * Final values are lightly hand-tuned from the OSI anchors to preserve
             * Season 2 scarcity while giving players thematic reagent sources.
             */

            if (bc is GiantSpider)
            {
                RemoveReagents(bc);
                bc.PackItem(new SpidersSilk(Utility.RandomMinMax(1, 4)));
            }
            else if (bc is HellHound)
            {
                RemoveReagents(bc);
                bc.PackItem(new SulfurousAsh(Utility.RandomMinMax(3, 6)));
            }
            else if (bc is EarthElemental)
            {
                RemoveReagents(bc);
                bc.PackItem(new MandrakeRoot(Utility.RandomMinMax(4, 8)));
            }
            else if (bc is AirElemental)
            {
                RemoveReagents(bc);
                bc.PackItem(new BlackPearl(Utility.RandomMinMax(4, 8)));
            }
            else if (bc is FireElemental)
            {
                RemoveReagents(bc);
                bc.PackItem(new SulfurousAsh(Utility.RandomMinMax(4, 8)));
            }
            else if (bc is Corpser)
            {
                RemoveReagents(bc);
                bc.PackItem(new Ginseng(Utility.RandomMinMax(5, 9)));
            }
            else if (bc is Reaper)
            {
                RemoveReagents(bc);
                bc.PackItem(new Garlic(Utility.RandomMinMax(8, 12)));
            }
            else if (bc is PoisonElemental)
            {
                RemoveReagents(bc);
                bc.PackItem(new Nightshade(Utility.RandomMinMax(10, 20)));
            }
            else if (bc is BloodElemental)
            {
                RemoveReagents(bc);
                bc.PackItem(new Bloodmoss(Utility.RandomMinMax(12, 24)));
            }
        }
    }
}



