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

/* Scripts/Engines/Harvest/Lumberjacking.cs
 * ChangeLog
 *  11/19/21, Yoar
 *      Added lumber resource maps.
 *  11/17/21, Yoar
 *      Now randomizing lumber veins.
 *  11/14/21, Yoar
 *      Added ML wood types.
 *  2010.06.10 - Pix
 *      Remove "Awaiting AFK lumberjacking check." message sent to player
 *	03/27/07, Pix
 *		Implemented RTT for AFK resource gathering thwarting.
 *  6/8/2004, Pulse
 *		Set Felucca resource gather to be equal to that of all other facets thereby removing
 *		the double harvest rate for wood. (Property lumber.ConsumedPerFeluccaHarvest affected)
*/
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Engines.Harvest
{
    public class Lumberjacking : HarvestSystem
    {
        private static Lumberjacking m_System;

        public static Lumberjacking System
        {
            get
            {
                if (m_System == null)
                    m_System = new Lumberjacking();

                return m_System;
            }
        }

        private HarvestDefinition m_Definition;

        public HarvestDefinition Definition
        {
            get { return m_Definition; }
        }

        private Lumberjacking()
        {
            HarvestResource[] res;
            HarvestVein[] veins;

            #region Lumberjacking
            HarvestDefinition lumber = new HarvestDefinition();

            // Resource banks are every 4x3 tiles
            lumber.BankWidth = 4;
            lumber.BankHeight = 3;

            // Every bank holds from 20 to 45 logs
            lumber.MinTotal = 20;
            lumber.MaxTotal = 45;

            // A resource bank will respawn its content every 20 to 30 minutes
            lumber.MinRespawn = TimeSpan.FromMinutes(20.0);
            lumber.MaxRespawn = TimeSpan.FromMinutes(30.0);

            // Skill checking is done on the Lumberjacking skill
            lumber.Skill = SkillName.Lumberjacking;

            // Set the list of harvestable tiles
            lumber.Tiles = m_TreeTiles;

            // Players must be within 2 tiles to harvest
            lumber.MaxRange = 2;

            // Ten logs per harvest action
            lumber.ConsumedPerHarvest = 10;
            // No longer harvest wood at twice the speed in Felucca
            //lumber.ConsumedPerFeluccaHarvest = 20;
            lumber.ConsumedPerFeluccaHarvest = 10;

            // The chopping effect
            lumber.EffectActions = new int[] { 13 };
            lumber.EffectSounds = new int[] { 0x13E };
            lumber.EffectCounts = new int[] { 1, 2, 2, 2, 3 };
            lumber.EffectDelay = TimeSpan.FromSeconds(1.6);
            lumber.EffectSoundDelay = TimeSpan.FromSeconds(0.9);

            lumber.NoResourcesMessage = 500493; // There's not enough wood here to harvest.
            lumber.FailMessage = 500495; // You hack at the tree for a while, but fail to produce any useable wood.
            lumber.OutOfRangeMessage = 500446; // That is too far away.
            lumber.PackFullMessage = 500497; // You can't place any wood into your backpack!
            lumber.ToolBrokeMessage = 500499; // You broke your axe.

#if false
            if (Core.RuleSets.MLRules())
            {
                res = new HarvestResource[]
                {
                    new HarvestResource(  00.0, 00.0, 100.0, 1072540, typeof( Log ) ),
                    new HarvestResource(  65.0, 25.0, 105.0, 1072541, typeof( OakLog ) ),
                    new HarvestResource(  80.0, 40.0, 120.0, 1072542, typeof( AshLog ) ),
                    new HarvestResource(  95.0, 55.0, 135.0, 1072543, typeof( YewLog ) ),
                    new HarvestResource( 100.0, 60.0, 140.0, 1072544, typeof( HeartwoodLog ) ),
                    new HarvestResource( 100.0, 60.0, 140.0, 1072545, typeof( BloodwoodLog ) ),
                    new HarvestResource( 100.0, 60.0, 140.0, 1072546, typeof( FrostwoodLog ) ),
                };

                veins = new HarvestVein[]
                {
                    new HarvestVein( 49.0, 0.0, res[0], null ),	// Ordinary Logs
                    new HarvestVein( 30.0, 0.5, res[1], res[0] ), // Oak
                    new HarvestVein( 10.0, 0.5, res[2], res[0] ), // Ash
                    new HarvestVein( 05.0, 0.5, res[3], res[0] ), // Yew
                    new HarvestVein( 03.0, 0.5, res[4], res[0] ), // Heartwood
                    new HarvestVein( 02.0, 0.5, res[5], res[0] ), // Bloodwood
                    new HarvestVein( 01.0, 0.5, res[6], res[0] ), // Frostwood
                };

                // TODO: ML bonus resources

                lumber.Resources = res;
                lumber.Veins = veins;

                lumber.RandomizeVeins = true;
            }
#else
            if (BaseLog.NewWoodTypes)
            {
                res = new HarvestResource[]
                {
                    new HarvestResource( 00.0, 00.0, 100.0, 1072540, typeof( Log ) ),
                    new HarvestResource( 65.0, 25.0, 105.0, 1072541, typeof( OakLog ) ),
                    new HarvestResource( 75.0, 35.0, 115.0, 1072542, typeof( AshLog ) ),
                    new HarvestResource( 80.0, 40.0, 120.0, 1072543, typeof( YewLog ) ),
                    new HarvestResource( 90.0, 50.0, 130.0, 1072544, typeof( HeartwoodLog ) ),
                    new HarvestResource( 95.0, 55.0, 135.0, 1072545, typeof( BloodwoodLog ) ),
                    new HarvestResource( 99.0, 59.0, 139.0, 1072546, typeof( FrostwoodLog ) ),
                };

                veins = new HarvestVein[]
                {
                    new HarvestVein( 49.0, 0.0, res[0], null ),	// Ordinary Logs
                    new HarvestVein( 16.4, 0.5, res[1], res[0] ), // Oak
                    new HarvestVein( 12.2, 0.5, res[2], res[0] ), // Ash
                    new HarvestVein( 10.2, 0.5, res[3], res[0] ), // Yew
                    new HarvestVein( 06.1, 0.5, res[4], res[0] ), // Heartwood
                    new HarvestVein( 04.1, 0.5, res[5], res[0] ), // Bloodwood
                    new HarvestVein( 02.0, 0.5, res[6], res[0] ), // Frostwood
                };

                lumber.Resources = res;
                lumber.Veins = veins;

                lumber.RandomizeVeins = true;
            }
#endif
            else
            {
                res = new HarvestResource[]
                {
                    new HarvestResource( 00.0, 00.0, 100.0, 500498, typeof( Log ) )
                };

                veins = new HarvestVein[]
                {
                    new HarvestVein( 100.0, 0.0, res[0], null )
                };
            }

            lumber.Resources = res;
            lumber.Veins = veins;

            m_Definition = lumber;
            Definitions.Add(lumber);
            #endregion
        }

        public override bool CheckResources(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, bool timed)
        {
            // TODO: Move to HarvestSystem?
            #region Harvest Map
            if (ResourceMap.Find(from, loc, from.Map, def) != null)
                return true;
            #endregion

            return base.CheckResources(from, tool, def, map, loc, timed);
        }

        public override HarvestResource MutateResource(Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestVein vein, HarvestResource primary, HarvestResource fallback)
        {
            // TODO: Move to HarvestSystem?
            #region Harvest Map
            ResourceMap resourceMap = ResourceMap.Find(from, loc, from.Map, def);

            if (resourceMap != null)
            {
                HarvestResource mapResource;

                if (resourceMap.MutateResource(from, def, out mapResource))
                    return mapResource;
            }
            #endregion

            return base.MutateResource(from, tool, def, map, loc, vein, primary, fallback);
        }

        public override Item Construct(Type type, Mobile from, Item tool, HarvestDefinition def, Map map, Point3D loc, HarvestResource resource)
        {
            Item item = base.Construct(type, from, tool, def, map, loc, resource);

            #region Carpenter's Axe

            if (tool is CarpentersAxe && item is BaseLog)
            {
                Item boards = null;

                switch (((BaseLog)item).Resource)
                {
                    case CraftResource.RegularWood: boards = new Board(item.Amount); break;
                    case CraftResource.OakWood: boards = new OakBoard(item.Amount); break;
                    case CraftResource.AshWood: boards = new AshBoard(item.Amount); break;
                    case CraftResource.YewWood: boards = new YewBoard(item.Amount); break;
                    case CraftResource.Heartwood: boards = new HeartwoodBoard(item.Amount); break;
                    case CraftResource.Bloodwood: boards = new BloodwoodBoard(item.Amount); break;
                    case CraftResource.Frostwood: boards = new FrostwoodBoard(item.Amount); break;
                }

                if (boards != null)
                {
                    item.Delete();
                    item = boards;
                }
            }

            #endregion

            return item;
        }

        public override void SendSuccessTo(Mobile from, Item item, HarvestResource resource)
        {
            #region Carpenter's Axe

            if (item is BaseBoard)
            {
                string message = null;

                switch (((BaseBoard)item).Resource)
                {
                    case CraftResource.RegularWood: message = "You chop some ordinary logs and cleve them into boards."; break;
                    case CraftResource.OakWood: message = "You chop some oak logs and cleve them into boards."; break;
                    case CraftResource.AshWood: message = "You chop some ash logs and cleve them into boards."; break;
                    case CraftResource.YewWood: message = "You chop some yew logs and cleve them into boards."; break;
                    case CraftResource.Heartwood: message = "You chop some heartwood logs and cleve them into boards."; break;
                    case CraftResource.Bloodwood: message = "You chop some bloodwood logs and cleve them into boards."; break;
                    case CraftResource.Frostwood: message = "You chop some frostwood logs and cleve them into boards."; break;
                }

                if (message != null)
                {
                    from.SendMessage(message);
                    return;
                }
            }

            #endregion

            base.SendSuccessTo(from, item, resource);
        }

        public override bool CheckHarvest(Mobile from, Item tool)
        {
            if (!base.CheckHarvest(from, tool))
                return false;

            if (tool.Parent != from)
            {
                from.SendLocalizedMessage(500487); // The axe must be equipped for any serious wood chopping.
                return false;
            }

            return true;
        }

        public override bool CheckHarvest(Mobile from, Item tool, HarvestDefinition def, object toHarvest)
        {
            if (from is PlayerMobile && !((PlayerMobile)from).RTT("AFK lumberjacking check."))
            {
                //from.SendMessage("Awaiting AFK lumberjacking check.");
                return false;
            }

            if (!base.CheckHarvest(from, tool, def, toHarvest))
                return false;

            if (tool.Parent != from)
            {
                from.SendLocalizedMessage(500487); // The axe must be equipped for any serious wood chopping.
                return false;
            }

            return true;
        }

        public override void OnBadHarvestTarget(Mobile from, Item tool, object toHarvest)
        {
            from.SendLocalizedMessage(500489); // You can't use an axe on that.
        }

        public static void Initialize()
        {
            Array.Sort(m_TreeTiles);
        }

        #region Tile lists
        private static int[] m_TreeTiles = new int[]
            {
                0x4CCA, 0x4CCB, 0x4CCC, 0x4CCD, 0x4CD0, 0x4CD3, 0x4CD6, 0x4CD8,
                0x4CDA, 0x4CDD, 0x4CE0, 0x4CE3, 0x4CE6, 0x4CF8, 0x4CFB, 0x4CFE,
                0x4D01, 0x4D41, 0x4D42, 0x4D43, 0x4D44, 0x4D57, 0x4D58, 0x4D59,
                0x4D5A, 0x4D5B, 0x4D6E, 0x4D6F, 0x4D70, 0x4D71, 0x4D72, 0x4D84,
                0x4D85, 0x4D86, 0x52B5, 0x52B6, 0x52B7, 0x52B8, 0x52B9, 0x52BA,
                0x52BB, 0x52BC, 0x52BD,

                0x4CCE, 0x4CCF, 0x4CD1, 0x4CD2, 0x4CD4, 0x4CD5, 0x4CD7, 0x4CD9,
                0x4CDB, 0x4CDC, 0x4CDE, 0x4CDF, 0x4CE1, 0x4CE2, 0x4CE4, 0x4CE5,
                0x4CE7, 0x4CE8, 0x4CF9, 0x4CFA, 0x4CFC, 0x4CFD, 0x4CFF, 0x4D00,
                0x4D02, 0x4D03, 0x4D45, 0x4D46, 0x4D47, 0x4D48, 0x4D49, 0x4D4A,
                0x4D4B, 0x4D4C, 0x4D4D, 0x4D4E, 0x4D4F, 0x4D50, 0x4D51, 0x4D52,
                0x4D53, 0x4D5C, 0x4D5D, 0x4D5E, 0x4D5F, 0x4D60, 0x4D61, 0x4D62,
                0x4D63, 0x4D64, 0x4D65, 0x4D66, 0x4D67, 0x4D68, 0x4D69, 0x4D73,
                0x4D74, 0x4D75, 0x4D76, 0x4D77, 0x4D78, 0x4D79, 0x4D7A, 0x4D7B,
                0x4D7C, 0x4D7D, 0x4D7E, 0x4D7F, 0x4D87, 0x4D88, 0x4D89, 0x4D8A,
                0x4D8B, 0x4D8C, 0x4D8D, 0x4D8E, 0x4D8F, 0x4D90, 0x4D95, 0x4D96,
                0x4D97, 0x4D99, 0x4D9A, 0x4D9B, 0x4D9D, 0x4D9E, 0x4D9F, 0x4DA1,
                0x4DA2, 0x4DA3, 0x4DA5, 0x4DA6, 0x4DA7, 0x4DA9, 0x4DAA, 0x4DAB,
                0x52BE, 0x52BF, 0x52C0, 0x52C1, 0x52C2, 0x52C3, 0x52C4, 0x52C5,
                0x52C6, 0x52C7
            };
        #endregion
    }
}