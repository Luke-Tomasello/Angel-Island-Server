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

/* Engines/Township/Craft/TownshipCraft.cs
 * CHANGELOG:
 *  3/24/22, Yoar
 *      Enabled marble/sandstone craftables.
 *      Added fence/gate craftables.
 *  3/24/22, Adam
 *      Inform the spacing code that rocks may be placed adjacent to one another
 *  3/23/22, Add rocks and trees
 *  3/17/22, Adam (ResourceDictionary)
 *      1. Make a copy and Remember the resource items used to craft.
 *      This is useful if you need the make use of the properties of the resource used to make the item.
 *      For example, when crafting a plot to grow plants in a township, we need the 'seed' used in the construction.
 *      2. add the dig sound for planting plants
 *      3. Add plantable PlantItems
 *      4. Rename group Botanicals ==> Gardening (we will be adding rocks and such)
 *  3/12/22, Adam
 *      Add Holly and Boxwood hedges
 *      Remove Holly for now.
 *      Add animate and special 'dig' sound for placing 'scaffold'
 * 1/17/22, Yoar
 *	    Added more walls/floors. TODO: Marble/sandstone craftables.
 * 11/24/21, Yoar
 *	    Added WelcomeMat.
 * 11/23/21, Yoar
 *	    Initial version.
 */

using Server.Engines.Craft;
using Server.Engines.CrownSterlingSystem;
using Server.Engines.Plants;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections.Generic;
namespace Server.Township
{
    public class DefTownshipCraft : CraftSystem
    {
        public override SkillName MainSkill
        {
            get { return SkillName.Carpentry; }
        }

        public override TextDefinition GumpTitle
        {
            get { return "<CENTER>TOWNSHIP BUILDER MENU</CENTER>"; }
        }

        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DefTownshipCraft();

                return m_CraftSystem;
            }
        }

        public override double GetChanceAtMin(CraftItem item)
        {
            return 0.5; // 50%
        }

        private DefTownshipCraft()
            : base(1, 1, 1.25)
        {
        }

        public override TextDefinition CanCraft(Mobile from, BaseTool tool, Type itemType)
        {
            if (tool.Deleted || tool.UsesRemaining < 0)
                return 1044038; // You have worn out your tool!
            else if (!BaseTool.CheckAccessible(tool, from))
                return 1044263; // The tool must be on your person to use.
            else if (itemType != null && !typeof(TargetCraft).IsAssignableFrom(itemType) && itemType != typeof(RopeCraft))
            {
                TextDefinition buildMessage;

                if (!CouldFit(from, itemType, out buildMessage))
                    return buildMessage;
            }

            return 0;
        }

        private static bool CouldFit(Mobile from, Type itemType, out TextDefinition message)
        {
            TownshipBuilder.BuildFlag flags = TownshipBuilder.BuildFlag.IgnorePlacer | TownshipBuilder.BuildFlag.NeedsSurface;

            if (typeof(TownshipFloor).IsAssignableFrom(itemType) || typeof(FarmPlot).IsAssignableFrom(itemType))
                flags |= TownshipBuilder.BuildFlag.CheckFloors;

            if (TownshipSettings.NeedsGround)
                flags |= TownshipBuilder.BuildFlag.NeedsGround;

            TownshipBuilder.Options.Configure(flags, GetClearance(itemType));

            TownshipBuilder.BuildResult result = TownshipBuilder.Check(from, from.Location, from.Map, GetPlaceID(itemType));

            message = TownshipBuilder.GetMessage(result);

            return (result == TownshipBuilder.BuildResult.Valid);
        }

        public static int GetClearance(Type itemType)
        {
            if (itemType == typeof(WelcomeMat))
                return TownshipSettings.TeleporterClearance;

            if (HasType(m_IgnoreClearance, itemType))
                return 0;

            return TownshipSettings.DecorationClearance;
        }

        public static int GetPlaceID(Type itemType)
        {
            if (HasType(m_ShortItems, itemType))
                return 0x1;

            return 0x6;
        }

        private static readonly Type[] m_IgnoreClearance = new Type[]
            {
                typeof(FortificationWall),
                typeof(TownshipDoor),
                typeof(TownshipFloor),
                typeof(TownshipRock),
                typeof(TownshipPlantItem),
                typeof(PlantCraft),
                typeof(GrassCraft),
            };

        private static readonly Type[] m_ShortItems = new Type[]
            {
                typeof(TownshipFloor),
                typeof(TownshipRock),
                typeof(TownshipPlantItem),
                typeof(PlantCraft),
                typeof(GrassCraft),
                typeof(FarmPlot),
            };

        private static bool HasType(Type[] array, Type type)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].IsAssignableFrom(type))
                    return true;
            }

            return false;
        }

        public override void PlayCraftEffect(Mobile from, object obj = null)
        {
            if (obj != null && obj is CraftItem ci && (ci.GroupName == "Gardening" || ci.ItemType == typeof(GrassCraft)))
            {
                from.PlaySound(0x125); // dig sound

                if (!from.Mounted)
                    from.Animate(11, 5, 1, true, false, 0);
            }
            else
            {
                from.PlaySound(0x23D);
            }
        }

        public override TextDefinition PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken)
                from.SendLocalizedMessage(1044038); // You have worn out your tool

            if (failed)
            {
                if (lostMaterial)
                    return 1044043; // You failed to create the item, and some of your materials are lost.
                else
                    return 1044157; // You failed to create the item, but no materials were lost.
            }
            else
            {
                if (quality == 0)
                    return 502785; // You were barely able to make this item.  It's quality is below average.
                else if (makersMark && quality == 2)
                    return 1044156; // You create an exceptional quality item and affix your maker's mark.
                else if (quality == 2)
                    return 1044155; // You create an exceptional quality item.
                else
                    return 1044154; // You create the item.
            }
        }

        public override bool GiveItem(Mobile from, Item item)
        {
            Point3D loc = from.Location;

            loc.Z += TownshipBuilder.OffsetZ;

            if (item is FortificationWall || item is TownshipDoor)
            {
                Scaffold.PlaceAt(item, loc, from.Map);
                return true;
            }
            else if (item is ITownshipItem)
            {
                BuildItem(from, item, loc);
                return true;
            }

            return false;
        }

        public static void BuildItem(Mobile from, Item item, Point3D loc)
        {
            if (item is ITownshipItem)
                ((ITownshipItem)item).OnBuild(from);

            item.MoveToWorld(loc, from.Map);

            TownshipItemHelper.SetOwnership(item, from);
        }

        public static bool RequiresMasonry(CraftItem craftItem)
        {
            bool requiresCarpentry = false;

            foreach (CraftSkill craftSkill in craftItem.Skills)
            {
                if (craftSkill.SkillToMake == SkillName.Carpentry && craftSkill.MinSkill >= 100.0)
                {
                    requiresCarpentry = true;
                    break;
                }
            }

            // masonry requires 100 carpentry to learn
            // if the craftable requires less than 100 carpentry, we can't require masonry
            if (!requiresCarpentry)
                return false;

            foreach (CraftRes craftRes in craftItem.Resources)
            {
                if (craftRes.ItemType == typeof(Granite) || craftRes.ItemType == typeof(Marble) || craftRes.ItemType == typeof(Sandstone))
                    return true;
            }

            return false;
        }

        public override void InitCraftList()
        {
            int index;

            // BEGIN Gardening
            const string gardening = "Gardening";
            const double reqSkillGardening = 0.0;

            if (Engines.Plants.PlantSystem.Enabled)
            {
                AddCraft(typeof(TownshipPlantItem), gardening, "dirt patch", reqSkillGardening, reqSkillGardening, typeof(FertileDirt), 1023969, 10, 1042081, false);
                AddCraft(typeof(TownshipPlantItem), gardening, "dirt patch (fertile)", reqSkillGardening, reqSkillGardening, typeof(FertileDirt), 1023969, 50, 1042081, true);

                index = AddCraft(typeof(PlantCraft), gardening, "plant seed", reqSkillGardening, reqSkillGardening, typeof(FertileDirt), 1023969, 10, 1042081, false);
                AddRes(index, typeof(BaseBeverage), "Measures of water", 2, 1042081, item => ((BaseBeverage)item).Content == BeverageType.Water);

                index = AddCraft(typeof(PlantCraft), gardening, "plant seed (fertile)", reqSkillGardening, reqSkillGardening, typeof(FertileDirt), 1023969, 50, 1042081, true);
                AddRes(index, typeof(BaseBeverage), "Measures of water", 2, 1042081, item => ((BaseBeverage)item).Content == BeverageType.Water);

                AddCraft(typeof(UprootCraft), gardening, "uproot plant", reqSkillGardening, reqSkillGardening, typeof(PlantBowl), 1060834, 1, 1042081);
            }

            AddCraft(typeof(TownshipFarmPlot), gardening, "farm plot", reqSkillGardening, reqSkillGardening, typeof(FertileDirt), 1023969, 10, 1042081);

            // 8/21/23, Yoar: Do these work? Either way, we don't need them since we can grow hedges using the plant system
#if false
            index = AddCraft(typeof(BoxwoodHedge), gardening, "boxwood hedge", reqSkillGardening, reqSkillGardening, typeof(FertileDirt), 1023969, 10, 1042081, Boxwood.YoungBoxwood);
            AddRes(index, typeof(BaseBeverage), "Measures of water", 2, 1042081, item => ((BaseBeverage)item).Content == BeverageType.Water);
            AddRes(index, typeof(Boxwood), "Boxwood", 1, 1042081);
#endif

            AddCraft(typeof(Basalt), gardening, "basalt", reqSkillGardening, reqSkillGardening, typeof(BasaltDeed), "Basalt", 1, 1042081, 0x1363);
            AddCraft(typeof(Dacite), gardening, "dacite", reqSkillGardening, reqSkillGardening, typeof(DaciteDeed), "Dacite", 1, 1042081, 0x1364);
            AddCraft(typeof(Diabase), gardening, "diabase", reqSkillGardening, reqSkillGardening, typeof(DiabaseDeed), "Diabase", 1, 1042081, 0x136B);
            AddCraft(typeof(Diorite), gardening, "diorite", reqSkillGardening, reqSkillGardening, typeof(DioriteDeed), "Diorite", 1, 1042081, 0x1772);
            AddCraft(typeof(Gabbro), gardening, "gabbro", reqSkillGardening, reqSkillGardening, typeof(GabbroDeed), "Gabbro", 1, 1042081, 0x1773);
            AddCraft(typeof(Pegmatite), gardening, "pegmatite", reqSkillGardening, reqSkillGardening, typeof(PegmatiteDeed), "Pegmatite", 1, 1042081, 0x1775);
            AddCraft(typeof(Peridotite), gardening, "peridotite", reqSkillGardening, reqSkillGardening, typeof(PeridotiteDeed), "Peridotite", 1, 1042081, 0x1777);
            AddCraft(typeof(Rhyolite), gardening, "rhyolite", reqSkillGardening, reqSkillGardening, typeof(RhyoliteDeed), "Rhyolite", 1, 1042081, 0x1367);
            AddCraft(typeof(Gneiss), gardening, "Gneiss", reqSkillGardening, reqSkillGardening, typeof(GneissDeed), "Gneiss", 1, 1042081, 0x1774);
            AddCraft(typeof(Quartzite), gardening, "Quartzite", reqSkillGardening, reqSkillGardening, typeof(QuartziteDeed), "Quartzite", 1, 1042081, 0x177C);
            // END Gardening

            // BEGIN Dark Wood Walls
            AddCraft(typeof(WoodFortificationWall), 1060054, "dark wooden wall (south)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x007);
            AddCraft(typeof(WoodFortificationWall), 1060054, "dark wooden wall (east)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x008);
            AddCraft(typeof(WoodFortificationWall), 1060054, "dark wooden wall (corner)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x006);
            AddCraft(typeof(WoodFortificationWall), 1060054, "dark wooden wall (pole)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x009);

            AddCraft(typeof(WoodFortificationWall), 1060054, "half dark wooden wall (south)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x012);
            AddCraft(typeof(WoodFortificationWall), 1060054, "half dark wooden wall (east)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x011);
            AddCraft(typeof(WoodFortificationWall), 1060054, "half dark wooden wall (corner)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x010);
            AddCraft(typeof(WoodFortificationWall), 1060054, "half dark wooden wall (pole)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x013);
            // END Dark Wood Walls

            // BEGIN Light Wood Walls
            AddCraft(typeof(WoodFortificationWall), 1060055, "light wooden wall (south)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x0A8);
            AddCraft(typeof(WoodFortificationWall), 1060055, "light wooden wall (east)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x0A7);
            AddCraft(typeof(WoodFortificationWall), 1060055, "light wooden wall (corner)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x0A6);
            AddCraft(typeof(WoodFortificationWall), 1060055, "light wooden wall (pole)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x0A9);

            AddCraft(typeof(WoodFortificationWall), 1060055, "half light wooden wall (south)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x0B7);
            AddCraft(typeof(WoodFortificationWall), 1060055, "half light wooden wall (east)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x0B8);
            AddCraft(typeof(WoodFortificationWall), 1060055, "half light wooden wall (corner)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x0B6);
            AddCraft(typeof(WoodFortificationWall), 1060055, "half light wooden wall (pole)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x013); // there is no half pole for light wood walls - let's use the one from dark wood walls
            // END Light Wood Walls

            // BEGIN Fieldstone Walls
            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x01C);
            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x01B);
            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x01A);
            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x01D);

            AddCraft(typeof(StoneFortificationWall), 1060056, "half fieldstone wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x025);
            AddCraft(typeof(StoneFortificationWall), 1060056, "half fieldstone wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x026);
            AddCraft(typeof(StoneFortificationWall), 1060056, "half fieldstone wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x024);
            AddCraft(typeof(StoneFortificationWall), 1060056, "half fieldstone wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x027);

            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone arch (north)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x02A);
            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone arch (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x02C);
            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone arch (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x02B);
            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone arch (west)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x029);
            AddCraft(typeof(StoneFortificationWall), 1060056, "fieldstone arch (corner)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x028);

            AddCraft(typeof(StoneFortificationWall), 1060056, "ruined wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x281);
            AddCraft(typeof(StoneFortificationWall), 1060056, "ruined wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x282);
            AddCraft(typeof(StoneFortificationWall), 1060056, "half ruined wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x277);
            AddCraft(typeof(StoneFortificationWall), 1060056, "half ruined wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x27C);
            AddCraft(typeof(StoneFortificationWall), 1060056, "crumbled wall (north)", 100.0, 100.0, typeof(Granite), 1044514, 2, 1044513, 0x279);
            AddCraft(typeof(StoneFortificationWall), 1060056, "crumbled wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 2, 1044513, 0x27B);
            AddCraft(typeof(StoneFortificationWall), 1060056, "crumbled wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 2, 1044513, 0x27A);
            AddCraft(typeof(StoneFortificationWall), 1060056, "crumbled wall (west)", 100.0, 100.0, typeof(Granite), 1044514, 2, 1044513, 0x278);
            // END Fieldstone Walls

            // BEGIN Weathered Stone Walls
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered stone wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x0C8);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered stone wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x0C9);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered stone wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x0C7);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered stone wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x0CC);

            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (north)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0CE);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0D1);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0D0);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (west)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0CF);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (corner)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0CD);

            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (north, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0D7);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (east, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0DA);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (south, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0D9);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (west, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0D8);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered arch (corner, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0D4);

            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered buttress (north)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0D3);
            AddCraft(typeof(StoneFortificationWall), 1060057, "blue weathered buttress (west)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x0D2);

            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered stone wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x1D0);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered stone wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x1D1);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered stone wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x1CF);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered stone wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x1D2);

            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (north)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1D9);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1D6);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1D8);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (west)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1D7);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (corner)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1D5);

            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (north, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1DF);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (east, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1DC);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (south, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1DE);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (west, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1DD);
            AddCraft(typeof(StoneFortificationWall), 1060057, "brown weathered arch (corner, round)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x1DB);
            // END Weathered Stone Walls

            // BEGIN Grey Brick Walls
            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x034);
            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x035);
            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x033);
            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x036);

            AddCraft(typeof(StoneFortificationWall), 1060058, "half grey brick wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x03E);
            AddCraft(typeof(StoneFortificationWall), 1060058, "half grey brick wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x03F);
            AddCraft(typeof(StoneFortificationWall), 1060058, "half grey brick wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x03D);
            AddCraft(typeof(StoneFortificationWall), 1060058, "half grey brick wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x040);

            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick arch (north)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x046);
            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick arch (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x047);
            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick arch (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x049);
            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick arch (west)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x048);
            AddCraft(typeof(StoneFortificationWall), 1060058, "grey brick arch (corner)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x045);
            // END Grey Brick Walls

            // BEGIN Light Brick Walls
            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x058);
            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x057);
            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x059);
            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x05A);

            AddCraft(typeof(StoneFortificationWall), 1060059, "half light brick wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x05F);
            AddCraft(typeof(StoneFortificationWall), 1060059, "half light brick wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x060);
            AddCraft(typeof(StoneFortificationWall), 1060059, "half light brick wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x061);
            AddCraft(typeof(StoneFortificationWall), 1060059, "half light brick wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x062);

            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick arch (north)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x06E);
            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick arch (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x06F);
            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick arch (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x071);
            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick arch (west)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x070);
            AddCraft(typeof(StoneFortificationWall), 1060059, "light brick arch (corner)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x06D);
            // END Light Brick Walls

            // BEGIN Log Walls
            AddCraft(typeof(WoodFortificationWall), 1060061, "log wall (south)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x092);
            AddCraft(typeof(WoodFortificationWall), 1060061, "log wall (east)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x091);
            AddCraft(typeof(WoodFortificationWall), 1060061, "log wall (corner)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x090);
            AddCraft(typeof(WoodFortificationWall), 1060061, "log wall (pole)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x093);

            AddCraft(typeof(WoodFortificationWall), 1060061, "half log wall (south)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x09C);
            AddCraft(typeof(WoodFortificationWall), 1060061, "half log wall (east)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x09B);
            AddCraft(typeof(WoodFortificationWall), 1060061, "half log wall (corner)", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x09A);
            AddCraft(typeof(WoodFortificationWall), 1060061, "half log wall (pole)", 100.0, 100.0, typeof(Log), 1044041, 400, 1044351, 0x09D);
            // END Log Walls

            // BEGIN Palisades Walls
            const double reqSkillPalisade = 80.0;

            AddCraft(typeof(WoodFortificationWall), 1060062, "palisade (south)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 400, 1044351, 0x222);
            AddCraft(typeof(WoodFortificationWall), 1060062, "palisade (east)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 400, 1044351, 0x221);
            AddCraft(typeof(WoodFortificationWall), 1060062, "palisade (corner)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 400, 1044351, 0x223);

            AddCraft(typeof(WoodFortificationWall), 1060062, "half palisade (south)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 300, 1044351, 0x423);
            AddCraft(typeof(WoodFortificationWall), 1060062, "half palisade (east)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 300, 1044351, 0x424);
            AddCraft(typeof(WoodFortificationWall), 1060062, "half palisade (corner)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 300, 1044351, 0x425);

            AddCraft(typeof(WoodFortificationWall), 1060062, "palisade wall (south)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 400, 1044351, 0x228);
            AddCraft(typeof(WoodFortificationWall), 1060062, "palisade wall (east)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 400, 1044351, 0x227);
            AddCraft(typeof(WoodFortificationWall), 1060062, "palisade wall (corner)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 400, 1044351, 0x226);
            AddCraft(typeof(WoodFortificationWall), 1060062, "palisade wall (pole)", reqSkillPalisade, reqSkillPalisade, typeof(Log), 1044041, 400, 1044351, 0x229);
            // END Palisades Walls

            // BEGIN Tan Marble Walls
            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble wall (south)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 5, Marble.ResourceMessage, 0x105);
            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble wall (east)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 5, Marble.ResourceMessage, 0x106);
            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble wall (corner)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 5, Marble.ResourceMessage, 0x104);
            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble wall (pole)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 5, Marble.ResourceMessage, 0x107);

            AddCraft(typeof(StoneFortificationWall), 1060063, "half tan marble wall (south)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x10B);
            AddCraft(typeof(StoneFortificationWall), 1060063, "half tan marble wall (east)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x10C);
            AddCraft(typeof(StoneFortificationWall), 1060063, "half tan marble wall (corner)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x10A);
            AddCraft(typeof(StoneFortificationWall), 1060063, "half tan marble wall (pole)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x10D);

            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble arch (north)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x115);
            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble arch (east)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x114);
            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble arch (south)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x116);
            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble arch (west)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x113);
            AddCraft(typeof(StoneFortificationWall), 1060063, "tan marble arch (corner)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x112);
            // END Tan Marble Walls

            // BEGIN White Marble Walls
            AddCraft(typeof(StoneFortificationWall), 1060064, "white marble wall (south)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 5, Marble.ResourceMessage, 0x292);
            AddCraft(typeof(StoneFortificationWall), 1060064, "white marble wall (east)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 5, Marble.ResourceMessage, 0x293);
            AddCraft(typeof(StoneFortificationWall), 1060064, "white marble wall (corner)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 5, Marble.ResourceMessage, 0x291);
            AddCraft(typeof(StoneFortificationWall), 1060064, "white marble wall (pole)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 5, Marble.ResourceMessage, 0x294);

            AddCraft(typeof(StoneFortificationWall), 1060064, "half white marble wall (south)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2A2);
            AddCraft(typeof(StoneFortificationWall), 1060064, "half white marble wall (east)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2A3);
            AddCraft(typeof(StoneFortificationWall), 1060064, "half white marble wall (corner)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2A1);
            AddCraft(typeof(StoneFortificationWall), 1060064, "half white marble wall (pole)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2A4);

            AddCraft(typeof(StoneFortificationWall), 1060063, "white marble arch (north)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2B3);
            AddCraft(typeof(StoneFortificationWall), 1060063, "white marble arch (east)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2B2);
            AddCraft(typeof(StoneFortificationWall), 1060063, "white marble arch (south)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2B4);
            AddCraft(typeof(StoneFortificationWall), 1060063, "white marble arch (west)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2B1);
            AddCraft(typeof(StoneFortificationWall), 1060063, "white marble arch (corner)", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x2B0);
            // END White Marble Walls

            // BEGIN Sandstone Brick Walls
            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone brick wall (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x160);
            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone brick wall (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x15F);
            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone brick wall (corner)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x15E);
            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone brick wall (pole)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x161);

            AddCraft(typeof(StoneFortificationWall), 1060065, "half sandstone brick wall (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x16A);
            AddCraft(typeof(StoneFortificationWall), 1060065, "half sandstone brick wall (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x169);
            AddCraft(typeof(StoneFortificationWall), 1060065, "half sandstone brick wall (corner)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x168);
            AddCraft(typeof(StoneFortificationWall), 1060065, "half sandstone brick wall (pole)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x16B);

            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone arch (north)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x16E);
            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone arch (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x170);
            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone arch (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x16F);
            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone arch (west)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x16D);
            AddCraft(typeof(StoneFortificationWall), 1060065, "sandstone arch (corner)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x16C);
            // END Sandstone Brick Walls

            // BEGIN Sandstone and Mortar Walls
            AddCraft(typeof(StoneFortificationWall), 1060067, "sandstone wall (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x3C8);
            AddCraft(typeof(StoneFortificationWall), 1060067, "sandstone wall (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x3C9);
            AddCraft(typeof(StoneFortificationWall), 1060067, "sandstone wall (corner)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x3C7);
            AddCraft(typeof(StoneFortificationWall), 1060067, "sandstone wall (pole)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x3CA);

            AddCraft(typeof(StoneFortificationWall), 1060067, "half sandstone wall (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x3C0);
            AddCraft(typeof(StoneFortificationWall), 1060067, "half sandstone wall (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x3C1);
            AddCraft(typeof(StoneFortificationWall), 1060067, "half sandstone wall (corner)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x3BE);
            AddCraft(typeof(StoneFortificationWall), 1060067, "half sandstone wall (pole)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 3, Sandstone.ResourceMessage, 0x3C2);

            AddCraft(typeof(StoneFortificationWall), 1060067, "mortar wall (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x3DA);
            AddCraft(typeof(StoneFortificationWall), 1060067, "mortar wall (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x3DB);
            AddCraft(typeof(StoneFortificationWall), 1060067, "mortar wall (corner)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x3D9);
            AddCraft(typeof(StoneFortificationWall), 1060067, "mortar wall (pole)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 5, Sandstone.ResourceMessage, 0x3DC);

            AddCraft(typeof(StoneFortificationWall), 1060067, "crumbled ledge (north)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3B7);
            AddCraft(typeof(StoneFortificationWall), 1060067, "crumbled ledge (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3BB);
            AddCraft(typeof(StoneFortificationWall), 1060067, "crumbled ledge (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3BC);
            AddCraft(typeof(StoneFortificationWall), 1060067, "crumbled ledge (west)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3BD);

            AddCraft(typeof(StoneFortificationWall), 1060067, "ruined ledge (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3B9);
            AddCraft(typeof(StoneFortificationWall), 1060067, "ruined ledge (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3BA);
            AddCraft(typeof(StoneFortificationWall), 1060067, "ruined ledge (corner)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3B8);

            AddCraft(typeof(StoneFortificationWall), 1060067, "ruined half wall (north)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3C6);
            AddCraft(typeof(StoneFortificationWall), 1060067, "ruined half wall (east)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3C3);
            AddCraft(typeof(StoneFortificationWall), 1060067, "ruined half wall (south)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3C4);
            AddCraft(typeof(StoneFortificationWall), 1060067, "ruined half wall (west)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3C5);
            AddCraft(typeof(StoneFortificationWall), 1060067, "ruined half wall (corner)", 100.0, 100.0, typeof(Sandstone), Sandstone.ResourceName, 2, Sandstone.ResourceMessage, 0x3BF);
            // END Sandstone and Mortar Walls

            // BEGIN Dungeon Walls
            const string dungeonWalls = "Dungeon Walls";

            AddCraft(typeof(StoneFortificationWall), dungeonWalls, "dungeon wall (south)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x241);
            AddCraft(typeof(StoneFortificationWall), dungeonWalls, "dungeon wall (east)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x242);
            AddCraft(typeof(StoneFortificationWall), dungeonWalls, "dungeon wall (corner)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x243);
            AddCraft(typeof(StoneFortificationWall), dungeonWalls, "dungeon wall (pole)", 100.0, 100.0, typeof(Granite), 1044514, 5, 1044513, 0x244);

            AddCraft(typeof(StoneFortificationWall), dungeonWalls, "dungeon arch (north)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x24A);
            AddCraft(typeof(StoneFortificationWall), dungeonWalls, "dungeon arch (east)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x245);
            AddCraft(typeof(StoneFortificationWall), dungeonWalls, "dungeon arch (south)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x246);
            AddCraft(typeof(StoneFortificationWall), dungeonWalls, "dungeon arch (west)", 100.0, 100.0, typeof(Granite), 1044514, 3, 1044513, 0x249);
            // END Dungeon Walls

            // BEGIN Floors
            const double reqSkillFloors = 40.0;

            AddCraft(typeof(TownshipFloor), 1061018, "stone paver (light)", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x519);
            AddCraft(typeof(TownshipFloor), 1061018, "stone paver (overgrown)", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x51D);
            AddCraft(typeof(TownshipFloor), 1061018, "stone paver (dark)", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x521);
            AddCraft(typeof(TownshipFloor), 1061018, "sandstone paver", reqSkillFloors, reqSkillFloors, typeof(Sandstone), Sandstone.ResourceName, 1, Sandstone.ResourceMessage, 0x49D);
            AddCraft(typeof(TownshipFloor), 1061018, "sandstone floor (south)", reqSkillFloors, reqSkillFloors, typeof(Sandstone), Sandstone.ResourceName, 1, Sandstone.ResourceMessage, 0x525);
            AddCraft(typeof(TownshipFloor), 1061018, "sandstone floor (east)", reqSkillFloors, reqSkillFloors, typeof(Sandstone), Sandstone.ResourceName, 1, Sandstone.ResourceMessage, 0x529);
            AddCraft(typeof(TownshipFloor), 1061018, "dark sandstone floor (south)", reqSkillFloors, reqSkillFloors, typeof(Sandstone), Sandstone.ResourceName, 1, Sandstone.ResourceMessage, 0x52F);
            AddCraft(typeof(TownshipFloor), 1061018, "dark sandstone floor (east)", reqSkillFloors, reqSkillFloors, typeof(Sandstone), Sandstone.ResourceName, 1, Sandstone.ResourceMessage, 0x533);
            AddCraft(typeof(TownshipFloor), 1061018, "sandstone flagstones", reqSkillFloors, reqSkillFloors, typeof(Sandstone), Sandstone.ResourceName, 1, Sandstone.ResourceMessage, 0x500);
            AddCraft(typeof(TownshipFloor), 1061018, "light flagstones", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x57F);
            AddCraft(typeof(TownshipFloor), 1061018, "dark flagstones", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x4FC);
            AddCraft(typeof(TownshipFloor), 1061018, "bricks", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x4E2);
            AddCraft(typeof(TownshipFloor), 1061018, "bricks (south)", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x4E6);
            AddCraft(typeof(TownshipFloor), 1061018, "bricks (east)", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x4E8);
            AddCraft(typeof(TownshipFloor), 1061018, "marble floor", reqSkillFloors, reqSkillFloors, typeof(Marble), Marble.ResourceName, 1, Marble.ResourceMessage, 0x50D);
            AddCraft(typeof(TownshipFloor), 1061018, "marble paver", reqSkillFloors, reqSkillFloors, typeof(Marble), Marble.ResourceName, 1, Marble.ResourceMessage, 0x495);
            AddCraft(typeof(TownshipFloor), 1061018, "cave floor", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x53B);
            AddCraft(typeof(TownshipFloor), 1061018, "dirt floor", 0.0, 0.0, typeof(FertileDirt), "fertile dirt", 20, "You do not have sufficient dirt to make that.", 0x31F4);
            AddCraft(typeof(TownshipFloor), 1061018, "cobblestones", reqSkillFloors, reqSkillFloors, typeof(Granite), 1044514, 1, 1044513, 0x515);
            // END Floors

            // BEGIN Fences
            const double reqSkillFences = 40.0;

            AddCraft(typeof(FortificationWall), 1061686, "short iron fence (south)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, 0x84B);
            AddCraft(typeof(FortificationWall), 1061686, "short iron fence (east)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, 0x849);
            AddCraft(typeof(FortificationWall), 1061686, "short iron fence (corner)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, 0x84A);
            AddCraft(typeof(TownshipDoor), 1061686, "short iron gate (south, CW)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, DoorType.IronGateShort, DoorFacing.WestCW);
            AddCraft(typeof(TownshipDoor), 1061686, "short iron gate (south, CCW)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, DoorType.IronGateShort, DoorFacing.EastCCW);
            AddCraft(typeof(TownshipDoor), 1061686, "short iron gate (east, CW)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, DoorType.IronGateShort, DoorFacing.SouthCW);
            AddCraft(typeof(TownshipDoor), 1061686, "short iron gate (east, CCW)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, DoorType.IronGateShort, DoorFacing.NorthCCW);

            AddCraft(typeof(FortificationWall), 1061686, "tall iron fence (south)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, 0x823);
            AddCraft(typeof(FortificationWall), 1061686, "tall iron fence (east)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, 0x821);
            AddCraft(typeof(FortificationWall), 1061686, "tall iron fence (corner)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, 0x822);
            AddCraft(typeof(TownshipDoor), 1061686, "tall iron gate (south, CW)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, DoorType.IronGate, DoorFacing.WestCW);
            AddCraft(typeof(TownshipDoor), 1061686, "tall iron gate (south, CCW)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, DoorType.IronGate, DoorFacing.EastCCW);
            AddCraft(typeof(TownshipDoor), 1061686, "tall iron gate (east, CW)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, DoorType.IronGate, DoorFacing.SouthCW);
            AddCraft(typeof(TownshipDoor), 1061686, "tall iron gate (east, CCW)", reqSkillFences, reqSkillFences, typeof(IronIngot), 1044036, 200, 1044037, DoorType.IronGate, DoorFacing.NorthCCW);

            AddCraft(typeof(FortificationWall), 1061686, "light wooden fence (south)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x836);
            AddCraft(typeof(FortificationWall), 1061686, "light wooden fence (east)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x837);
            AddCraft(typeof(FortificationWall), 1061686, "light wooden fence (corner)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x835);
            AddCraft(typeof(FortificationWall), 1061686, "light wooden fence (pole)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x838);
            AddCraft(typeof(TownshipDoor), 1061686, "light wooden gate (south, CW)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, DoorType.LightWoodGate, DoorFacing.WestCW);
            AddCraft(typeof(TownshipDoor), 1061686, "light wooden gate (south, CCW)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, DoorType.LightWoodGate, DoorFacing.EastCCW);
            AddCraft(typeof(TownshipDoor), 1061686, "light wooden gate (east, CW)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, DoorType.LightWoodGate, DoorFacing.SouthCW);
            AddCraft(typeof(TownshipDoor), 1061686, "light wooden gate (east, CCW)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, DoorType.LightWoodGate, DoorFacing.NorthCCW);

            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (south)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x85D);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (east)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x85E);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (corner)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x85C);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (pole)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x85F);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (no pole, south)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x860);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (no pole, east)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x861);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (two poles, south)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x877);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (two poles, east)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x878);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (cross, south)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x863);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (cross, east)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x864);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (cross, corner)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x862);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (cross, pole)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x865);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (cross, two poles, south)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x88A);
            AddCraft(typeof(FortificationWall), 1061686, "wooden fence (cross, two poles, east)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, 0x88B);
            AddCraft(typeof(TownshipDoor), 1061686, "wooden gate (south, CW)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, DoorType.DarkWoodGate, DoorFacing.WestCW);
            AddCraft(typeof(TownshipDoor), 1061686, "wooden gate (south, CCW)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, DoorType.DarkWoodGate, DoorFacing.EastCCW);
            AddCraft(typeof(TownshipDoor), 1061686, "wooden gate (east, CW)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, DoorType.DarkWoodGate, DoorFacing.SouthCW);
            AddCraft(typeof(TownshipDoor), 1061686, "wooden gate (east, CCW)", reqSkillFences, reqSkillFences, typeof(Log), 1044041, 300, 1044351, DoorType.DarkWoodGate, DoorFacing.NorthCCW);
            // END Fences

            // 8/23/23, Yoar: Enabled Dock Craft
            if (true)
            {
                // BEGIN Docks
                const string docks = "Docks";
                const double reqSkillDocks = 70.0;
                const int ReqWoodDock = 250;

                index = AddCraft(typeof(DockCraft), docks, "dock (south)", reqSkillDocks, reqSkillDocks, typeof(Log), 1044041, ReqWoodDock, 1044351, true);
                SetItemID(index, 0x7CD);
                index = AddCraft(typeof(DockCraft), docks, "dock (east)", reqSkillDocks, reqSkillDocks, typeof(Log), 1044041, ReqWoodDock, 1044351, false);
                SetItemID(index, 0x7C9);
                index = AddCraft(typeof(ExtendDockCraft), docks, "extend dock (north)", reqSkillDocks, reqSkillDocks, typeof(Log), 1044041, ReqWoodDock, 1044351, ExtendDockCraft.ExtendType.North);
                SetItemID(index, 0x7CD);
                index = AddCraft(typeof(ExtendDockCraft), docks, "extend dock (east)", reqSkillDocks, reqSkillDocks, typeof(Log), 1044041, ReqWoodDock, 1044351, ExtendDockCraft.ExtendType.East);
                SetItemID(index, 0x7C9);
                index = AddCraft(typeof(ExtendDockCraft), docks, "extend dock (south)", reqSkillDocks, reqSkillDocks, typeof(Log), 1044041, ReqWoodDock, 1044351, ExtendDockCraft.ExtendType.South);
                SetItemID(index, 0x7CD);
                index = AddCraft(typeof(ExtendDockCraft), docks, "extend dock (west)", reqSkillDocks, reqSkillDocks, typeof(Log), 1044041, ReqWoodDock, 1044351, ExtendDockCraft.ExtendType.West);
                SetItemID(index, 0x7C9);
                index = AddCraft(typeof(PierFoundationCraft), docks, "pier (foundation)", reqSkillDocks, reqSkillDocks, typeof(Log), 1044041, 300, 1044351);
                SetItemID(index, 0x3AE);
                index = AddCraft(typeof(PierCraft), docks, "pier", reqSkillDocks, reqSkillDocks, typeof(Log), 1044041, 300, 1044351, 0x3A5);
                SetItemID(index, 0x3A5);
                index = AddCraft(typeof(RopeCraft), docks, "rope", reqSkillDocks, reqSkillDocks, typeof(Rope), "Rope", 1, 1044253);
                SetItemID(index, 0x3AA);
                // END Docks
            }

            // BEGIN Decorations
            // welcome mat
            index = AddCraft(typeof(WelcomeMat), 1044501, "welcome mat", 0.0, 0.0, typeof(Cloth), 1044286, 1000, 1044287);
            AddSkill(index, SkillName.Tailoring, 30.0, 30.0);

            // fire pit
            if (!CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.HomeDeco))
            {
                index = AddCraft(typeof(TownshipStatic), 1044501, 1024012, 0.0, 0.0, typeof(Log), 1044041, 300, 1044351, 0x0FAC);
                AddRes(index, typeof(Granite), 1044514, 1, 1044513);
            }

            // gruesome standard
            if (!CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.HomeDeco))
            {
                index = AddCraft(typeof(TownshipStatic), 1044501, 1021055, 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x41F);
                AddRes(index, typeof(Skull), "Skull", 1);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1021055, 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x420);
                AddRes(index, typeof(Skull), "Skull", 1);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1021055, 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x428);
                AddRes(index, typeof(Skull), "Skull", 1);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1021055, 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0x429);
                AddRes(index, typeof(Skull), "Skull", 1);
            }

            // gravestones - for now we will allow sale in both systems (the vendor has a greater selection)
            if (true || !CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.Gravestones))
            {
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xED4);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xED5);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xED6);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xED7);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xED8);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xED9);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xEDA);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xEDB);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xEDC);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xEDD);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023796, 100.0, 100.0, typeof(Granite), 1044514, 4, 1044513, 0xEDE);
            }

            // grave
            AddCraft(typeof(TownshipFloor), 1044501, 1023807, 100.0, 100.0, typeof(FertileDirt), "fertile dirt", 40, "You do not have sufficient dirt to make that.", 0xEDF);
            AddCraft(typeof(TownshipFloor), 1044501, 1023807, 100.0, 100.0, typeof(FertileDirt), "fertile dirt", 40, "You do not have sufficient dirt to make that.", 0xEE0);
            AddCraft(typeof(TownshipFloor), 1044501, 1023807, 100.0, 100.0, typeof(FertileDirt), "fertile dirt", 40, "You do not have sufficient dirt to make that.", 0xEE1);
            AddCraft(typeof(TownshipFloor), 1044501, 1023807, 100.0, 100.0, typeof(FertileDirt), "fertile dirt", 40, "You do not have sufficient dirt to make that.", 0xEE2);

            // spider webs
            if (!CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.HomeDeco))
            {
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023811, 0.0, 0.0, typeof(SpidersSilk), 1015007, 100, "You do not have sufficient spider's silk to make that.", 0xEE3);
                AddSkill(index, SkillName.Tailoring, 30.0, 30.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023811, 0.0, 0.0, typeof(SpidersSilk), 1015007, 100, "You do not have sufficient spider's silk to make that.", 0xEE4);
                AddSkill(index, SkillName.Tailoring, 30.0, 30.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023811, 0.0, 0.0, typeof(SpidersSilk), 1015007, 100, "You do not have sufficient spider's silk to make that.", 0xEE5);
                AddSkill(index, SkillName.Tailoring, 30.0, 30.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1023811, 0.0, 0.0, typeof(SpidersSilk), 1015007, 100, "You do not have sufficient spider's silk to make that.", 0xEE6);
                AddSkill(index, SkillName.Tailoring, 30.0, 30.0);
            }

            // dead trees - probably make more sense for a township
            if (true || !CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.HomeDeco))
            {
                AddCraft(typeof(TownshipStatic), 1044501, "dead tree", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0xCCA);
                AddCraft(typeof(TownshipStatic), 1044501, "dead tree", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0xCCB);
                AddCraft(typeof(TownshipStatic), 1044501, "dead tree", 100.0, 100.0, typeof(Log), 1044041, 300, 1044351, 0xCCC);
            }

            // garbage piles
            if (!CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.HomeDeco))
            {
                index = AddCraft(typeof(TownshipStatic), 1044501, 1024338, 30.0, 30.0, typeof(Shaft), 1044560, 100, 1044561, 0x10F2);
                AddRes(index, typeof(BlankScroll), 1044377, 20, 1044378);
                AddRes(index, typeof(Bottle), 1044529, 20, 500315);
                AddSkill(index, SkillName.Tinkering, 30.0, 30.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, 1024338, 30.0, 30.0, typeof(Shaft), 1044560, 100, 1044561, 0x10F3);
                AddRes(index, typeof(BlankScroll), 1044377, 20, 1044378);
                AddRes(index, typeof(Bottle), 1044529, 20, 500315);
                AddSkill(index, SkillName.Tinkering, 30.0, 30.0);
            }

            // tree stumps - probably make more sense for a township
            if (true || !CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.HomeDeco))
            {
                index = AddCraft(typeof(TownshipStatic), 1044501, "tree stump (south, axe)", 30.0, 30.0, typeof(Log), 1044041, 300, 1044351, 0xE56);
                AddSkill(index, SkillName.Lumberjacking, 60.0, 60.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, "tree stump (south)", 30.0, 30.0, typeof(Log), 1044041, 300, 1044351, 0xE57);
                AddSkill(index, SkillName.Lumberjacking, 60.0, 60.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, "tree stump (east, axe)", 30.0, 30.0, typeof(Log), 1044041, 300, 1044351, 0xE58);
                AddSkill(index, SkillName.Lumberjacking, 60.0, 60.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, "tree stump (east)", 30.0, 30.0, typeof(Log), 1044041, 300, 1044351, 0xE59);
                AddSkill(index, SkillName.Lumberjacking, 60.0, 60.0);
            }

            // glowing runes
            if (!CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.HomeDeco))
            {
                index = AddCraft(typeof(TownshipStatic), 1044501, "glowing rune (C)", 30.0, 30.0, typeof(RecallRune), 1044447, 1, 1044253, item => !((RecallRune)item).Marked, 0xE5C);
                AddRes(index, typeof(SpidersSilk), 1044360, 20, 1044368);
                AddRes(index, typeof(SulfurousAsh), 1044359, 20, 1044367);
                AddSkill(index, SkillName.Magery, 60.0, 60.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, "glowing rune (H)", 30.0, 30.0, typeof(RecallRune), 1044447, 1, 1044253, item => !((RecallRune)item).Marked, 0xE5F);
                AddRes(index, typeof(SpidersSilk), 1044360, 20, 1044368);
                AddRes(index, typeof(SulfurousAsh), 1044359, 20, 1044367);
                AddSkill(index, SkillName.Magery, 60.0, 60.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, "glowing rune (M)", 30.0, 30.0, typeof(RecallRune), 1044447, 1, 1044253, item => !((RecallRune)item).Marked, 0xE62);
                AddRes(index, typeof(SpidersSilk), 1044360, 20, 1044368);
                AddRes(index, typeof(SulfurousAsh), 1044359, 20, 1044367);
                AddSkill(index, SkillName.Magery, 60.0, 60.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, "glowing rune (O)", 30.0, 30.0, typeof(RecallRune), 1044447, 1, 1044253, item => !((RecallRune)item).Marked, 0xE65);
                AddRes(index, typeof(SpidersSilk), 1044360, 20, 1044368);
                AddRes(index, typeof(SulfurousAsh), 1044359, 20, 1044367);
                AddSkill(index, SkillName.Magery, 60.0, 60.0);
                index = AddCraft(typeof(TownshipStatic), 1044501, "glowing rune (W)", 30.0, 30.0, typeof(RecallRune), 1044447, 1, 1044253, item => !((RecallRune)item).Marked, 0xE68);
                AddRes(index, typeof(SpidersSilk), 1044360, 20, 1044368);
                AddRes(index, typeof(SulfurousAsh), 1044359, 20, 1044367);
                AddSkill(index, SkillName.Magery, 60.0, 60.0);
            }

            // misc.
            if (!CrownSterlingVendor.Sells(CrownSterlingSystem.RewardSet.HomeDeco))
                AddCraft(typeof(TownshipStatic), 1044501, "marble pedestal", 100.0, 100.0, typeof(Marble), Marble.ResourceName, 3, Marble.ResourceMessage, 0x1223);
            // END Decorations

            // BEGIN Grasses
            const string grasses = "Grasses";
            const double reqSkillGrasses = 0.0;

            index = AddCraft(typeof(GrassCraft), grasses, "grass (south)", reqSkillGrasses, reqSkillGrasses, typeof(Nightshade), 1044358, 4, 1044366, 0x1782);
            SetItemID(index, 0x1782);
            index = AddCraft(typeof(GrassCraft), grasses, "grass (east)", reqSkillGrasses, reqSkillGrasses, typeof(Nightshade), 1044358, 4, 1044366, 0x1783);
            SetItemID(index, 0x1783);
            index = AddCraft(typeof(GrassCraft), grasses, "grass (north)", reqSkillGrasses, reqSkillGrasses, typeof(Nightshade), 1044358, 4, 1044366, 0x1784);
            SetItemID(index, 0x1784);
            index = AddCraft(typeof(GrassCraft), grasses, "grass (west)", reqSkillGrasses, reqSkillGrasses, typeof(Nightshade), 1044358, 4, 1044366, 0x1785);
            SetItemID(index, 0x1785);
            index = AddCraft(typeof(GrassCraft), grasses, "grass (south-west)", reqSkillGrasses, reqSkillGrasses, typeof(Nightshade), 1044358, 4, 1044366, 0x1786);
            SetItemID(index, 0x1786);
            index = AddCraft(typeof(GrassCraft), grasses, "grass (north-west)", reqSkillGrasses, reqSkillGrasses, typeof(Nightshade), 1044358, 4, 1044366, 0x1787);
            SetItemID(index, 0x1787);
            index = AddCraft(typeof(GrassCraft), grasses, "grass (north-east)", reqSkillGrasses, reqSkillGrasses, typeof(Nightshade), 1044358, 4, 1044366, 0x1788);
            SetItemID(index, 0x1788);
            index = AddCraft(typeof(GrassCraft), grasses, "grass (south-east)", reqSkillGrasses, reqSkillGrasses, typeof(Nightshade), 1044358, 4, 1044366, 0x1789);
            SetItemID(index, 0x1789);
            // END Grasses

            Repair = true;
        }

        [CraftItemIDAttribute(0x914)]
        public class PlantCraft : CustomCraft
        {
            private bool m_FertileDirt;
            private Item m_Target;

            public PlantCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality, bool fertileDirt)
                : base(from, craftItem, craftSystem, typeRes, tool, quality)
            {
                m_FertileDirt = fertileDirt;
            }

            public override void EndCraftAction()
            {
                Item found = null;

                if (From.Backpack != null)
                {
                    foreach (Item item in From.Backpack.Items)
                    {
                        if (item is Seed)
                        {
                            // if we find 2 valid items, give the player a target instead
                            if (found != null)
                            {
                                found = null;
                                break;
                            }

                            found = item;
                        }
                    }
                }

                if (found != null)
                {
                    m_Target = found;
                    CraftItem.CompleteCraft(Quality, false, From, CraftSystem, TypeRes, Tool, this);
                }
                else
                {
                    From.SendMessage("Select the seed you wish to plant.");
                    From.Target = new SeedTarget(this);
                }
            }

            private class SeedTarget : Target
            {
                private PlantCraft m_Craft;

                public SeedTarget(PlantCraft craft)
                    : base(-1, false, TargetFlags.None)
                {
                    m_Craft = craft;
                }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    TextDefinition message;

                    if (m_Craft.Acquire(targeted, out message))
                        m_Craft.CraftItem.CompleteCraft(m_Craft.Quality, false, m_Craft.From, m_Craft.CraftSystem, m_Craft.TypeRes, m_Craft.Tool, m_Craft);
                    else
                        m_Craft.Failure(message);
                }

                protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
                {
                    if (cancelType == TargetCancelType.Canceled)
                        m_Craft.Failure(null);
                }
            }

            public override Item CompleteCraft(out TextDefinition message)
            {
                message = Verify(m_Target);

                if (message == null)
                {
                    Seed seed = (Seed)m_Target;

                    TownshipPlantItem plantItem = new TownshipPlantItem();

                    plantItem.PlantType = seed.PlantType;
                    plantItem.PlantHue = seed.PlantHue;
                    plantItem.ShowType = seed.ShowType;
                    plantItem.PlantStatus = PlantStatus.Seed;

                    plantItem.PlantSystem.Reset(false);
                    plantItem.PlantSystem.FertileDirt = m_FertileDirt;
                    plantItem.PlantSystem.Water = 2;

                    plantItem.Planter = From;

                    seed.Delete();

                    message = "You plant the seed in a patch of dirt.";
                    return plantItem;
                }

                return null;
            }

            private bool Acquire(object targeted, out TextDefinition message)
            {
                Item target = targeted as Item;

                message = Verify(target);

                if (message != null)
                    return false;

                m_Target = target;

                return true;
            }

            private TextDefinition Verify(Item item)
            {
                Seed seed = item as Seed;

                if (seed == null)
                    return "That is not a seed.";
                else if (!item.IsChildOf(From.Backpack))
                    return "The seed must be in your pack for you to use it.";

                return null;
            }

            private void Failure(TextDefinition message)
            {
                if (Tool != null && !Tool.Deleted && Tool.UsesRemaining > 0)
                    From.SendGump(new CraftGump(From, CraftSystem, Tool, message));
                else
                    TextDefinition.SendMessageTo(From, message);
            }
        }

        [CraftItemIDAttribute(0x1602)]
        public class UprootCraft : TargetCraft
        {
            public override int Range { get { return 2; } }
            public override bool AllowGround { get { return false; } }

            public UprootCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
                : base(from, craftItem, craftSystem, typeRes, tool, quality)
            {
            }

            protected override void OnBeginTarget()
            {
                From.SendMessage("Target the plant you wish to uproot.");
            }

            protected override bool ValidateTarget(object targeted, out TextDefinition message)
            {
                if (!(targeted is StaticPlantItem))
                {
                    message = "You cannot uproot that!";
                    return false;
                }

                StaticPlantItem plantItem = (StaticPlantItem)targeted;

                if (!plantItem.IsUsableBy(From))
                {
                    // TODO: More accurate messages based on the plant's status
                    message = "This is not your garden!";
                    return false;
                }

                if (plantItem.PlantStatus == PlantStatus.BowlOfDirt)
                {
                    message = "There is no plant in the patch of dirt.";
                    return false;
                }

                if (TreeSeed.IsTree(plantItem.PlantType))
                {
                    message = "You cannot uproot a tree into a bowl of dirt!";
                    return false;
                }

                // TODO: Would be nice if we could drop this requirement. But for this we'd need to copy the props of PlantSystem
                if (plantItem.PlantStatus != PlantStatus.DecorativePlant)
                {
                    message = "You can only uproot plants that are set to decorative mode.";
                    return false;
                }

                message = 0;
                return true;
            }

            protected override void OnCraft(object targeted, out TextDefinition message)
            {
                message = 0;

                StaticPlantItem oldPlant = (StaticPlantItem)targeted;

                PlantItem newPlant = new PlantItem();
                Utility.CopyProperties(newPlant, oldPlant);
                newPlant.Movable = true;
                From.AddToBackpack(newPlant);

                oldPlant.PlantStatus = PlantStatus.BowlOfDirt;

                oldPlant.PlantSystem.Reset(false);
                oldPlant.PlantSystem.Water = 0;

                oldPlant.Planter = null;
            }
        }

        public class GrassCraft : CustomCraft
        {
            private int m_ItemID;

            public GrassCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality, int itemID)
                : base(from, craftItem, craftSystem, typeRes, tool, quality)
            {
                m_ItemID = itemID;
            }

            public override void EndCraftAction()
            {
                if (!HasFloor())
                {
                    Failure("You may only build this on floor tiles.");
                    return;
                }

                if (HasGrass())
                {
                    Failure("The floor tile is already covered by a patch of grass.");
                    return;
                }

                CraftItem.CompleteCraft(Quality, false, From, CraftSystem, TypeRes, Tool, this);
            }

            private bool HasFloor()
            {
                foreach (Item item in From.GetItemsInRange(0))
                {
                    if (item.Z == From.Z && (item is TownshipFloor || item is TownshipFarmPlot))
                        return true;
                }

                return false;
            }

            private bool HasGrass()
            {
                foreach (Item item in From.GetItemsInRange(0))
                {
                    if (item.Z == From.Z && item.ItemID == m_ItemID && item is TownshipStatic)
                        return true;
                }

                return false;
            }

            public override Item CompleteCraft(out TextDefinition message)
            {
                message = null;

                return new TownshipStatic(m_ItemID);
            }

            private void Failure(TextDefinition message)
            {
                if (Tool != null && !Tool.Deleted && Tool.UsesRemaining > 0)
                    From.SendGump(new CraftGump(From, CraftSystem, Tool, message));
                else
                    TextDefinition.SendMessageTo(From, message);
            }
        }

        #region Repair

        public static TimeSpan RepairDelay { get { return Core.UOTC_CFG ? TimeSpan.FromMinutes(1.0) : TownshipSettings.WallRepairDelay; } }

        public static TextDefinition ProcessRepair(Mobile m, object targeted, bool usingDeed, ref bool toDelete) // TODO: Skill check? Repair contracts?
        {
            ITownshipItem tsi = targeted as ITownshipItem;

            if (tsi == null || tsi.HitsMax <= 0)
                return usingDeed ? 1061136 : 1044277; // You cannot repair that item with this type of repair contract. | That item cannot be repaired.

            CraftRes res = LookupPrimaryResource(targeted);

            if (res == null)
                return usingDeed ? 1061136 : 1044277; // You cannot repair that item with this type of repair contract. | That item cannot be repaired.

            //if (TownshipItemHelper.AFKCheck(m))
            //    return 0;

            int hitsPerc = 100 * tsi.Hits / tsi.HitsMax;

            if (hitsPerc >= 100)
                return 500423; // That is already in full repair.

            if (!m.CanBeginAction(typeof(RepairTimer)))
                return 500119; // You must wait to perform another action.

            if (DateTime.UtcNow < tsi.LastRepair + RepairDelay)
                return "That has already been repaired recently.";

            if (hitsPerc < 0)
                hitsPerc = 0;

            int consume = (100 - hitsPerc) * res.Amount / 100;

            consume /= 2; // half the craft cost

            if (consume < 1)
                consume = 1;

            if (!ConsumeResources(m.Backpack, CraftItem.GetEquivalentResources(res.ItemType), consume))
            {
                TextDefinition message;

                if (!TextDefinition.IsNullOrEmpty(res.Message))
                    message = res.Message;
                else
                    message = 1053098; // You do not have the required materials to make that.

                return message;
            }

            if (DateTime.UtcNow < tsi.LastRepair + TownshipSettings.WallRepairDelay)
                TownshipItemHelper.SendTCNotice(m);

            m.BeginAction(typeof(RepairTimer));

            m.Direction = m.GetDirectionTo(tsi);
            m.RevealingAction();
            m.Emote("*repairs the item*");
            m.Animate(11, 5, 1, true, false, 0);

            new RepairTimer(m, tsi).Start();

            return "You begin repairing the item.";
        }

        private static bool ConsumeResources(Container cont, Type[] equivTypes, int consume)
        {
            if (cont == null)
                return false;

            Item[][] allItems = new Item[equivTypes.Length][];

            int total = 0;

            for (int i = 0; i < equivTypes.Length && total < consume; i++)
            {
                allItems[i] = cont.FindItemsByType(equivTypes[i], true);

                for (int j = 0; j < allItems[i].Length && total < consume; j++)
                    total += allItems[i][j].Amount;
            }

            if (total < consume)
                return false;

            for (int i = 0; i < equivTypes.Length && allItems[i] != null && consume > 0; i++)
            {
                for (int j = 0; j < allItems[i].Length && consume > 0; j++)
                {
                    int toConsume = Math.Min(allItems[i][j].Amount, consume);

                    allItems[i][j].Consume(toConsume);

                    consume -= toConsume;
                }
            }

            return true;
        }

        private class RepairTimer : WorkTimer
        {
            private ITownshipItem m_Item;

            public RepairTimer(Mobile m, ITownshipItem toRepair)
                : base(m, TownshipSettings.WallRepairTicks)
            {
                m_Item = toRepair;
            }

            protected override bool Validate()
            {
                return m_Item.Hits < m_Item.HitsMax && DateTime.UtcNow >= m_Item.LastRepair + RepairDelay && Mobile.InRange(m_Item, 2);
            }

            protected override void OnWork()
            {
                Mobile.Direction = Mobile.GetDirectionTo(m_Item);
                Mobile.RevealingAction();
                Mobile.Emote("*repairs the item*");
                Mobile.Animate(11, 5, 1, true, false, 0);
            }

            protected override void OnFinished()
            {
                int hitsDiff = m_Item.HitsMax - m_Item.Hits;

                if (hitsDiff <= 0)
                    hitsDiff = 1;

                Mobile.SendLocalizedMessage(500425); // You repair the item.

                m_Item.LastRepair = DateTime.UtcNow;
                m_Item.HitsMax -= (hitsDiff / 2);
                m_Item.Hits = m_Item.HitsMax;

                Mobile.EndAction(typeof(RepairTimer));
            }

            protected override void OnFailed()
            {
                Mobile.EndAction(typeof(RepairTimer));
            }
        }

        #endregion

        public static CraftRes LookupPrimaryResource(object obj)
        {
            Type type = obj.GetType();

            if (obj is Item)
            {
                int itemID = ((Item)obj).ItemID;

                foreach (CraftItem ci in CraftSystem.CraftItems)
                {
                    if (ci.ItemType == type && ci.ItemID == itemID && ci.Resources.Count != 0)
                        return ci.Resources.GetAt(0);
                }
            }
            /*else if (targeted is Mobile)
            {
                // are there any mobiles we can damage/repair, like golems?
            }*/

            return null;
        }

        public enum CreditResult
        {
            Failed,
            Partial,
            Full,
        }

        public static CreditResult CreditTownship(TownshipStone ts, Item item)
        {
            int totalAmount = 0;
            int totalDeposited = 0;

            foreach (ResEntry res in LookupResources(item))
            {
                totalAmount += res.Amount;

                foreach (Type resType in CraftItem.GetEquivalentResources(res.Type))
                {
                    TownshipStockpile.StockFlag stock = TownshipStockpile.Identify(resType);

                    if (stock != TownshipStockpile.StockFlag.None)
                    {
                        totalDeposited += ts.Stockpile.DepositUpTo(null, stock, res.Amount);
                        break;
                    }
                }
            }

            if (totalDeposited <= 0)
                return CreditResult.Failed;
            else if (totalDeposited < totalAmount)
                return CreditResult.Partial;
            else
                return CreditResult.Full;
        }

        public static ResEntry[] LookupResources(Item item)
        {
            Type itemType = item.GetType();

            foreach (CraftItem ci in CraftSystem.CraftItems)
            {
                if (MatchCraftItem(ci, itemType, item.ItemID))
                {
                    List<ResEntry> list = new List<ResEntry>();

                    for (int i = 0; i < ci.Resources.Count; i++)
                    {
                        CraftRes craftRes = ci.Resources.GetAt(i);

                        list.Add(new ResEntry(craftRes.ItemType, craftRes.Amount));
                    }

                    return list.ToArray();
                }
            }

            return new ResEntry[0];
        }

        private static bool MatchCraftItem(CraftItem ci, Type itemType, int itemID)
        {
            if (ci.ItemType == typeof(DockCraft) || ci.ItemType == typeof(ExtendDockCraft))
            {
                return (itemType == typeof(TownshipStatic) && itemID >= 0x7C9 && itemID <= 0x7D0);
            }
            else if (ci.ItemType == typeof(PierFoundationCraft))
            {
                return (itemType == typeof(TownshipStatic) && itemID == 0x3AE);
            }
            else if (ci.ItemType == typeof(PierCraft))
            {
                return (itemType == typeof(TownshipStatic) && itemID == 0x3A5);
            }
            else if (ci.ItemType == typeof(GrassCraft))
            {
                return (itemType == typeof(TownshipStatic) && itemID >= 0x1782 && itemID <= 0x1789);
            }
            else if (ci.ItemType == typeof(TownshipFloor))
            {
                if (itemType == typeof(TownshipFloor) && ci.CraftArgs.Length == 1 && ci.CraftArgs[0] is int)
                {
                    int baseItemID = (int)ci.CraftArgs[0];

                    for (int i = 0; i < TownshipFloor.TileIDs.Length; i++)
                    {
                        int[] tileIDs = TownshipFloor.TileIDs[i];

                        if (Array.IndexOf(tileIDs, baseItemID) != -1 && Array.IndexOf(tileIDs, itemID) != -1)
                            return true;
                    }
                }
            }

            return (ci.ItemType == itemType && ci.ItemID == itemID);
        }

        public struct ResEntry
        {
            public static readonly ResEntry Empty = new ResEntry();

            public readonly Type Type;
            public readonly int Amount;

            public ResEntry(Type type, int amount)
            {
                Type = type;
                Amount = amount;
            }
        }
    }
}