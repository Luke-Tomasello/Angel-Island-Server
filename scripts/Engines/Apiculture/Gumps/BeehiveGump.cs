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

/* scripts\Engines\Apiculture\Gumps\BeehiveGump.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Gumps;
using Server.Items;
using Server.Network;
using System;

namespace Server.Engines.Apiculture
{
    public class BeehiveGump : BaseApicultureGump
    {
        public enum PageType : byte
        {
            Main,
            Production
        }

        private enum ButtonType : byte
        {
            Close,
            Production,
            Help,
            PourAgility,
            PourPoison,
            PourCure,
            PourHeal,
            PourStrength,
            TakeWax,
            TakeHoney
        }

        private Beehive m_Hive;
        private PageType m_Page;

        public BeehiveGump(Mobile from, Beehive hive)
            : this(from, hive, PageType.Main)
        {
        }

        private BeehiveGump(Mobile from, Beehive hive, PageType page)
            : base(20, 20)
        {
            m_Hive = hive;
            m_Page = page;

            BeeColony colony = m_Hive.Colony;

            switch (m_Page)
            {
                case PageType.Main:
                    {
                        AddBackground(50, 50, 200, 150, 0xE10);

                        AddItem(45, 45, 0xCEF);
                        AddItem(45, 118, 0xCF0);
                        AddItem(211, 45, 0xCEB);
                        AddItem(211, 118, 0xCEC);

                        AddIcon(48, 47, true, 0, -1);
                        AddLabel(54, 47, 0x835, ((int)colony.Stage).ToString());

                        AddIcon(232, 47, true, 0, -1);
                        AddLabel(238, 47, GetGrowthColor(colony.GrowthResult), GetGrowthLabel(colony.GrowthResult));

                        AddIcon(48, 183, true, 0, (int)ButtonType.Help);
                        AddLabel(54, 183, 0x835, "?");

                        AddIcon(232, 183, true, 0, -1);

                        AddImage(110, 85, 0x589); // circle
                        AddItem(127, 108, 0x91A); // beehive

                        AddIcon(71, 67, false, 0x1422, (int)ButtonType.Production);

                        AddIcon(71, 91, false, 0x372, -1); // patch
                        if (colony.ParasiteLevel > 0)
                            AddLabel(95, 92, colony.ParasiteLevel == 1 ? 0x34 : 0x25, "-");

                        AddIcon(71, 115, false, 0x1AE4, -1); // skull
                        if (colony.DiseaseLevel > 0)
                            AddLabel(95, 116, colony.DiseaseLevel == 1 ? 0x34 : 0x25, "-");

                        HiveResourceStatus waterStatus = colony.ScaleWater();
                        AddIcon(71, 139, false, 0xFF8, -1); // pitcher of water
                        AddLabel(95, 140, GetResourceColor(waterStatus), GetResourceLabel(waterStatus));

                        HiveResourceStatus flowerStatus = colony.ScaleFlowers();
                        AddIcon(71, 163, false, 0xD08, -1); // lilypad
                        AddLabel(95, 164, GetResourceColor(flowerStatus), GetResourceLabel(flowerStatus));

                        AddIcon(209, 67, false, 0xF08, (int)ButtonType.PourAgility); // blue potion
                        AddLabel(196, 67, 0x835, colony.PotAgility.ToString());

                        AddIcon(209, 91, false, 0xF0A, (int)ButtonType.PourPoison); // green potion
                        AddLabel(196, 91, 0x835, colony.PotPoison.ToString());

                        AddIcon(209, 115, false, 0xF07, (int)ButtonType.PourCure); // orange potion
                        AddLabel(196, 115, 0x835, colony.PotCure.ToString());

                        AddIcon(209, 139, false, 0xF0C, (int)ButtonType.PourHeal); // yellow potion
                        AddLabel(196, 139, 0x835, colony.PotHeal.ToString());

                        AddIcon(209, 163, false, 0xF09, (int)ButtonType.PourStrength); // white potion
                        AddLabel(196, 163, 0x835, colony.PotStrength.ToString());

                        AddLabelCentered(115, 67, 70, 0x5C, GetStageLabel(colony.Stage, colony.Population));
                        AddLabelCentered(115, 164, 70, GetHealthColor(colony.HealthStatus), colony.HealthStatus.ToString());

                        break;
                    }
                case PageType.Production:
                    {
                        AddBackground(50, 50, 200, 150, 0xE10);

                        AddImage(60, 90, 0xE17);
                        AddImage(120, 90, 0xE17);
                        AddImage(60, 145, 0xE17);
                        AddImage(120, 145, 0xE17);

                        AddItem(45, 45, 0xCEF);
                        AddItem(45, 118, 0xCF0);
                        AddItem(211, 45, 0xCEB);
                        AddItem(211, 118, 0xCEC);

                        AddIcon(70, 67, false, 0x1870, (int)ButtonType.Close);

                        AddLabelCentered(115, 67, 70, 0x5C, "Production");

                        AddIcon(106, 116, false, 0x1422, -1); // beeswax
                        if (colony.Stage >= HiveStage.Producing)
                            AddLabel(132, 116, 0x835, colony.Wax.ToString());
                        else
                            AddLabel(132, 116, 0x25, "X");

                        AddIcon(178, 116, false, 0x9EC, -1); // honey
                        if (colony.Stage >= HiveStage.Producing)
                            AddLabel(204, 116, 0x835, colony.Honey.ToString());
                        else
                            AddLabel(204, 116, 0x25, "X");

                        AddIcon(71, 163, false, 0x1422, (int)ButtonType.TakeWax);
                        AddIcon(213, 163, false, 0x9EC, (int)ButtonType.TakeHoney);

                        break;
                    }
            }
        }

        private void AddIcon(int x, int y, bool menu, int itemID, int buttonID)
        {
            int gumpID = (menu ? 0xD2 : 0xD4);

            if (buttonID == -1)
                AddImage(x, y, gumpID);
            else
                AddButton(x, y, gumpID, gumpID, buttonID, GumpButtonType.Reply, 0);

            if (itemID > 0)
                AddItemCentered(x, y, 19, 20, itemID);
        }

        private static int GetGrowthColor(HiveGrowthResult result)
        {
            switch (result)
            {
                default:
                    return 0;
                case HiveGrowthResult.PopulationDown:
                    return 0x25; // red
                case HiveGrowthResult.PopulationUp:
                    return 0x43; // green
                case HiveGrowthResult.NotHealthy:
                    return 0x25; // red
                case HiveGrowthResult.LowResources:
                    return 0x34; // yellow
                case HiveGrowthResult.Grown:
                    return 0x5C; // blue
            }
        }

        private static string GetGrowthLabel(HiveGrowthResult result)
        {
            switch (result)
            {
                default:
                    return String.Empty;
                case HiveGrowthResult.PopulationDown:
                    return "-";
                case HiveGrowthResult.PopulationUp:
                    return "+";
                case HiveGrowthResult.NotHealthy:
                    return "!";
                case HiveGrowthResult.LowResources:
                    return "!";
                case HiveGrowthResult.Grown:
                    return "+";
            }
        }

        private static string GetStageLabel(HiveStage stage, int population)
        {
            switch (stage)
            {
                default:
                    return String.Empty;
                case HiveStage.Colonizing:
                case HiveStage.Stage2:
                    return "Colonizing";
                case HiveStage.Brooding:
                case HiveStage.Stage4:
                    return "Brooding";
                case HiveStage.Producing:
                    return String.Format("Colony : {0}K", 10 * population);
            }
        }

        private static int GetHealthColor(HiveHealthStatus status)
        {
            switch (status)
            {
                default:
                    return 0;
                case HiveHealthStatus.Dying:
                    return 0x25; // red
                case HiveHealthStatus.Sickly:
                    return 0x34; // yellow
                case HiveHealthStatus.Healthy:
                    return 0x43; // green
                case HiveHealthStatus.Thriving:
                    return 0x5C; // dark green
            }
        }

        private static int GetResourceColor(HiveResourceStatus status)
        {
            switch (status)
            {
                default:
                    return 0;
                case HiveResourceStatus.VeryLow:
                    return 0x25; // red
                case HiveResourceStatus.Low:
                case HiveResourceStatus.VeryHigh:
                    return 0x34; // yellow
                case HiveResourceStatus.High:
                    return 0x43; // green
            }
        }

        private static string GetResourceLabel(HiveResourceStatus status)
        {
            switch (status)
            {
                default:
                    return String.Empty;
                case HiveResourceStatus.VeryLow:
                case HiveResourceStatus.Low:
                    return "-";
                case HiveResourceStatus.High:
                case HiveResourceStatus.VeryHigh:
                    return "+";
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (!m_Hive.CanUse(from, false))
                return;

            switch ((ButtonType)info.ButtonID)
            {
                case ButtonType.Close:
                    {
                        if (m_Page != PageType.Main)
                            from.SendGump(new BeehiveGump(from, m_Hive, PageType.Main));

                        break;
                    }
                case ButtonType.Production:
                    {
                        from.SendGump(new BeehiveGump(from, m_Hive, PageType.Production));

                        break;
                    }
                case ButtonType.Help:
                    {
                        from.SendGump(new BeehiveGump(from, m_Hive, m_Page));

                        from.CloseGump(typeof(BeehiveHelpGump));
                        from.SendGump(new BeehiveHelpGump(0));

                        break;
                    }
                case ButtonType.PourAgility:
                    {
                        PourPotion(from, new PotionEffect[] { PotionEffect.AgilityGreater });

                        from.SendGump(new BeehiveGump(from, m_Hive, m_Page));

                        break;
                    }
                case ButtonType.PourPoison:
                    {
                        PourPotion(from, new PotionEffect[] { PotionEffect.PoisonGreater, PotionEffect.PoisonDeadly });

                        from.SendGump(new BeehiveGump(from, m_Hive, m_Page));

                        break;
                    }
                case ButtonType.PourCure:
                    {
                        PourPotion(from, new PotionEffect[] { PotionEffect.CureGreater });

                        from.SendGump(new BeehiveGump(from, m_Hive, m_Page));

                        break;
                    }
                case ButtonType.PourHeal:
                    {
                        PourPotion(from, new PotionEffect[] { PotionEffect.HealGreater });

                        from.SendGump(new BeehiveGump(from, m_Hive, m_Page));

                        break;
                    }
                case ButtonType.PourStrength:
                    {
                        PourPotion(from, new PotionEffect[] { PotionEffect.StrengthGreater });

                        from.SendGump(new BeehiveGump(from, m_Hive, m_Page));

                        break;
                    }
                case ButtonType.TakeWax:
                    {
                        TakeWax(from);

                        from.SendGump(new BeehiveGump(from, m_Hive, m_Page));

                        break;
                    }
                case ButtonType.TakeHoney:
                    {
                        TakeHoney(from);

                        from.SendGump(new BeehiveGump(from, m_Hive, m_Page));

                        break;
                    }
            }
        }

        private void PourPotion(Mobile from, PotionEffect[] effects)
        {
            Item item = FindPotion(from, effects);

            if (item == null)
            {
                from.SendLocalizedMessage(1061884); // You don't have any strong potions of that type in your pack.
            }
            else if (!ApicultureSystem.SkillCheck(from, ApicultureSystem.SwarmMinSkill, ApicultureSystem.SwarmMaxSkill))
            {
                from.SendMessage("Uh oh, you've angered the bees while working the hive!");

                BeeSwarm.BeginEffect(from);
            }
            else
            {
                m_Hive.Colony.Pour(from, item);
            }
        }

        private static Item FindPotion(Mobile from, PotionEffect[] effects)
        {
            if (from.Backpack == null)
                return null;

            foreach (Item item in from.Backpack.FindItemsByType(new Type[] { typeof(BasePotion), typeof(PotionKeg) }))
            {
                if (item is BasePotion)
                {
                    BasePotion potion = (BasePotion)item;

                    if (Array.IndexOf(effects, potion.PotionEffect) != -1)
                        return potion;
                }
                else
                {
                    PotionKeg keg = (PotionKeg)item;

                    if (keg.Held > 0 && Array.IndexOf(effects, keg.Type) != -1)
                        return keg;
                }
            }

            return null;
        }

        private void TakeWax(Mobile from)
        {
            Container pack = from.Backpack;

            if (pack == null)
                return;

            Item tool = pack.FindItemByType(typeof(HiveTool));

            if (tool == null)
            {
                m_Hive.SendMessageTo(from, false, "You need a hive tool to scrape the excess beeswax!");
            }
            else if (m_Hive.Colony.Wax < 1)
            {
                m_Hive.SendMessageTo(from, false, "There isn't enough excess wax in the hive to harvest!");
            }
            else if (!ApicultureSystem.SkillCheck(from, ApicultureSystem.SwarmMinSkill, ApicultureSystem.SwarmMaxSkill))
            {
                from.SendMessage("Uh oh, you've angered the bees while working the hive!");

                BeeSwarm.BeginEffect(from);
            }
            else
            {
                Item wax = new Beeswax(m_Hive.Colony.Wax);

                if (!from.PlaceInBackpack(wax))
                {
                    m_Hive.SendMessageTo(from, false, "There is not enough room in your backpack for the wax!");

                    wax.Delete();
                }
                else
                {
                    m_Hive.SendMessageTo(from, false, "You collect the excess beeswax and place it in your pack.");

                    m_Hive.Colony.Wax = 0;

                    if (ConsumeTool(tool))
                        from.SendMessage("You broke your hive tool.");
                }
            }
        }

        private void TakeHoney(Mobile from)
        {
            Container pack = from.Backpack;

            if (pack == null)
                return;

            Item tool = pack.FindItemByType(typeof(HiveTool));

            if (tool == null)
            {
                m_Hive.SendMessageTo(from, false, "You need a hive tool to extract the excess honey.");
            }
            else if (m_Hive.Colony.Honey < 3)
            {
                m_Hive.SendMessageTo(from, false, "There isn't enough honey in the hive to fill a bottle.");
            }
            else if (!pack.ConsumeTotal(typeof(Bottle), 1))
            {
                m_Hive.SendMessageTo(from, false, "You need an empty bottle to fill with honey.");
            }
            else if (!ApicultureSystem.SkillCheck(from, ApicultureSystem.SwarmMinSkill, ApicultureSystem.SwarmMaxSkill))
            {
                from.SendMessage("Uh oh, you've angered the bees while working the hive!");

                BeeSwarm.BeginEffect(from);
            }
            else
            {
                JarHoney honey = new JarHoney();

                if (!from.PlaceInBackpack(honey))
                {
                    m_Hive.SendMessageTo(from, false, "There is not enough room in your backpack for the honey!");

                    honey.Delete();

                    from.PlaceInBackpack(new Bottle()); // give back empty bottle
                }
                else
                {
                    m_Hive.SendMessageTo(from, false, "You fill a bottle with golden honey and place it in your pack.");

                    m_Hive.Colony.Honey -= 3;

                    if (ConsumeTool(tool))
                        from.SendMessage("You broke your hive tool.");
                }
            }
        }

        private static bool ConsumeTool(Item tool)
        {
            if (tool is IUsesRemaining)
            {
                IUsesRemaining toolWithUses = (IUsesRemaining)tool;

                if (--toolWithUses.UsesRemaining <= 0)
                {
                    tool.Delete();
                    return true;
                }
            }

            return false;
        }
    }
}