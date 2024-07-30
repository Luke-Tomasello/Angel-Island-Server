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

/* Scripts\Engines\EventResources\Dragonglass\DragonglassCraft.cs
 * CHANGELOG:
 *  4/26/2024, Adam
 *      Initial version.
 */

using Server.Engines.EventResources;
using Server.Items;
using System;

namespace Server.Engines.Craft
{
    [EventCraft(CraftResource.Dragonglass)]
    public class DragonglassCraft : CraftSystem
    {
        public override SkillName MainSkill
        {
            get { return SkillName.Blacksmith; }
        }

        public override TextDefinition GumpTitle
        {
            get { return "<CENTER>DRAGONGLASS CRAFT MENU</CENTER>"; }
        }

        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DragonglassCraft();

                return m_CraftSystem;
            }
        }

        public override CraftECA ECA { get { return CraftECA.ChanceMinusSixtyToFourtyFive; } }

        public override double GetChanceAtMin(CraftItem item)
        {
            return 0.0; // 0%
        }

        public override bool SupportsEnchantedScrolls { get { return false; } }

        private DragonglassCraft()
            : base(1, 1, 1.25)
        {
            // we don't know the era/date this changed to the short-one-effect we have today
            // so when we are siege, we will go old-school
            if (Core.RuleSets.StandardShardRules())
            {
                MinCraftEffect = 2;
                MaxCraftEffect = 3;
                Delay = 2.2;
            }
        }

        public override TextDefinition CanCraft(Mobile from, BaseTool tool, Type itemType)
        {
            if (tool.Deleted || tool.UsesRemaining < 0)
                return 1044038; // You have worn out your tool!
            else if (!BaseTool.CheckTool(tool, from))
                return 1048146; // If you have a tool equipped, you must use that tool.
            else if (!BaseTool.CheckAccessible(tool, from))
                return 1044263; // The tool must be on your person to use.
            else if (!DefDragonglass.HasDragonglassForge(from, 2))
                return "You must be near an dragonglass forge to craft dragonglass items";
            else if (!DefBlacksmithy.HasAnvil(from, 2))
                return "You must be near an anvil to smith items.";
            else
                return 0;
        }

        public override void PlayCraftEffect(Mobile from, object obj = null)
        {
            if (Core.RuleSets.StandardShardRules())
            {
                if (from.Body.Type == BodyType.Human && !from.Mounted)
                    from.Animate(9, 5, 1, true, false, 0);
                new InternalTimer(from).Start();
            }
            else
            {
                from.PlaySound(0x2A);
            }
        }

        // Delay to synchronize the sound with the hit on the anvil
        private class InternalTimer : Timer
        {
            private Mobile m_From;

            public InternalTimer(Mobile from)
                : base(TimeSpan.FromSeconds(0.7))
            {
                m_From = from;
            }

            protected override void OnTick()
            {
                m_From.PlaySound(0x2A);
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

        public override void InitCraftList()
        {
            const double minSkill = 50.0;
            const double maxSkill = 100.0;
            const string resMessage = "You do not have sufficient Dragonglass to make that.";

            // Weapons
            AddCraft(typeof(Broadsword), 1044566, 1023934, minSkill, maxSkill, typeof(Dragonglass), "Dragonglass", 10, resMessage);
            AddCraft(typeof(Axe), 1044566, 1023913, minSkill, maxSkill, typeof(Dragonglass), "Dragonglass", 14, resMessage);
            AddCraft(typeof(Halberd), 1044566, 1025183, minSkill, maxSkill, typeof(Dragonglass), "Dragonglass", 20, resMessage);
            AddCraft(typeof(Kryss), 1044566, 1025121, minSkill, maxSkill, typeof(Dragonglass), "Dragonglass", 8, resMessage);
            AddCraft(typeof(Spear), 1044566, 1023938, minSkill, maxSkill, typeof(Dragonglass), "Dragonglass", 12, resMessage);
            AddCraft(typeof(Mace), 1044566, 1023932, minSkill, maxSkill, typeof(Dragonglass), "Dragonglass", 6, resMessage);
            AddCraft(typeof(WarHammer), 1044566, 1025177, minSkill, maxSkill, typeof(Dragonglass), "Dragonglass", 16, resMessage);
            AddCraft(typeof(Bow), 1044566, 1025042, minSkill, maxSkill, typeof(Dragonglass), "Dragonglass", 10, resMessage);

#if false
            // Shields
            AddCraft(typeof(Buckler),       1011080, 1027027, minSkill, maxSkill, typeof(Dragonglass), 1023977, 10, resMessage);
#endif

            MarkOption = true;
        }
    }
}