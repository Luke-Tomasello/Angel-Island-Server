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

/* Server/Engines/EventResources/Obsidian/BlackrockCraft.cs
 * CHANGELOG:
 *  10/18/23, Yoar
 *      Initial version.
 */

using Server.Engines.EventResources;
using Server.Items;
using Server.Mobiles;
using System;

namespace Server.Engines.Craft
{
    [EventCraft(CraftResource.Obsidian)]
    public class BlackrockCraft : CraftSystem
    {
        public override SkillName MainSkill
        {
            get { return SkillName.Carpentry; }
        }

        public override TextDefinition GumpTitle
        {
            get { return "<CENTER>BLACKROCK CRAFT MENU</CENTER>"; }
        }

        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new BlackrockCraft();

                return m_CraftSystem;
            }
        }

        public override double GetChanceAtMin(CraftItem item)
        {
            return 0.0; // 0%
        }

        public override bool SupportsEnchantedScrolls { get { return false; } }

        private BlackrockCraft()
            : base(1, 1, 1.25)
        {
        }

        public override bool RetainsColorFrom(CraftItem item, Type type)
        {
            return true;
        }

        public override TextDefinition CanCraft(Mobile from, BaseTool tool, Type itemType)
        {
            if (tool.Deleted || tool.UsesRemaining < 0)
                return 1044038; // You have worn out your tool!
            else if (!BaseTool.CheckTool(tool, from))
                return 1048146; // If you have a tool equipped, you must use that tool.
            else if (!BaseTool.CheckAccessible(tool, from))
                return 1044263; // The tool must be on your person to use.
            else if (!DefObsidian.HasObsidianForge(from, 2))
                return "You must be near an obsidian forge to craft obsidian items";
            else if (!(from is PlayerMobile && ((PlayerMobile)from).Masonry && from.Skills[SkillName.Carpentry].Base >= 100.0))
                return 1044633; // You havent learned stonecraft.
            else
                return 0;
        }

        public override void PlayCraftEffect(Mobile from, object obj = null)
        {
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

        public override void InitCraftList()
        {
            const double minSkill = 50.0;
            const double maxSkill = 100.0;
            const string resName = "Blackrock";
            const string resMessage = "You do not have sufficient blackrock to make that.";

            // Decorations
            AddCraft(typeof(Vase), 1044501, 1022888, minSkill, maxSkill, typeof(Blackrock), resName, 1, resMessage);
            AddCraft(typeof(LargeVase), 1044501, 1022887, minSkill, maxSkill, typeof(Blackrock), resName, 3, resMessage);

            // Statues
            AddCraft(typeof(StatuePegasus), 1044503, 1044510, minSkill, maxSkill, typeof(Blackrock), resName, 4, resMessage);
            AddCraft(typeof(Bust), 1044503, 1024810, minSkill, maxSkill, typeof(Blackrock), resName, 4, resMessage);
        }
    }
}