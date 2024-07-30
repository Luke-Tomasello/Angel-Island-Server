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

/* Engines/Harvest/Core/HarvestTarget.cs
 * CHANGELOG:
 *  8/6/2023, Adam
 *      Disallow the destruction of containers in your backpack if it is not empty (exploit)
 *	11/14/21, Yoar
 *	    Added IAxe interface.
 *	11/8/21, Yoar
 *		Added call to XMarksTheSpot item.
 *	9/3/04, Pix
 *		Now when you chop a trapped container, it executes the trap.
 *	6/27/04, mith
 *		Set CheckLOS false if harvest type is mining. This allows players to mine the tops of mountains.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Engines.Quests;
using Server.Engines.Quests.Hag;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Engines.Harvest
{
    public class HarvestTarget : Target
    {
        private Item m_Tool;
        private HarvestSystem m_System;

        public HarvestTarget(Item tool, HarvestSystem system)
            : base(-1, true, TargetFlags.None)
        {
            m_Tool = tool;
            m_System = system;

            DisallowMultis = true;

            if (system is Mining)
                CheckLOS = false;
        }

        protected override void OnTarget(Mobile from, object targeted)
        {
            if (m_System is Mining && targeted is StaticTarget)
            {
                int itemID = ((StaticTarget)targeted).ItemID;

                // grave
                if (itemID == 0xED3 || itemID == 0xEDF || itemID == 0xEE0 || itemID == 0xEE1 || itemID == 0xEE2 || itemID == 0xEE8)
                {
                    PlayerMobile player = from as PlayerMobile;

                    if (player != null)
                    {
                        QuestSystem qs = player.Quest;

                        if (qs is WitchApprenticeQuest)
                        {
                            FindIngredientObjective obj = qs.FindObjective(typeof(FindIngredientObjective)) as FindIngredientObjective;

                            if (obj != null && !obj.Completed && obj.Ingredient == Ingredient.Bones)
                            {
                                player.SendLocalizedMessage(1055037); // You finish your grim work, finding some of the specific bones listed in the Hag's recipe.
                                obj.Complete();

                                return;
                            }
                        }
                    }
                }
            }

            if (Township.TownshipItemHelper.CheckDamageTarget(from, targeted))
                return;

            if (m_System is Lumberjacking && targeted is IChopable)
                ((IChopable)targeted).OnChop(from);
            else if (m_System is Lumberjacking && targeted is IAxe && m_Tool is BaseAxe)
            {
                IAxe obj = (IAxe)targeted;
                Item item = (Item)targeted;

                if (!item.IsChildOf(from.Backpack))
                    from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
                else if (obj.Axe(from, (BaseAxe)m_Tool))
                    from.PlaySound(0x13E);
            }
            else if (m_System is Lumberjacking && FurnitureAttribute.Check(targeted as Item))
                DestroyFurniture(from, (Item)targeted);
            else if (m_System is Mining && targeted is TreasureMap)
                ((TreasureMap)targeted).OnBeginDig(from);
            else if (!XMarksTheSpot.BeginDig(from, m_Tool, targeted))
                m_System.StartHarvesting(from, m_Tool, targeted);
        }

        private void DestroyFurniture(Mobile from, Item item)
        {
            if (!from.InRange(item.GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            if (!item.IsChildOf(from.Backpack) && !item.Movable)
            {
                from.SendLocalizedMessage(500462); // You can't destroy that while it is here.
                return;
            }

            // 8/6/2023, Adam: Disallow the destruction of containers in your backpack (exploit)
            if (ExplodingContainerExploit(from, item))
            {
                from.SendMessage("You can't destroy that while it still contains items.");
                return;
            }

            from.SendLocalizedMessage(500461); // You destroy the item.
            Effects.PlaySound(item.GetWorldLocation(), item.Map, 0x3B3);

            if (item is TrapableContainer)
            {
                ((TrapableContainer)item).ExecuteTrap(from);
            }

            if (item is Container)
                ((Container)item).Destroy();
            else
                item.Delete();
        }

        public static bool ExplodingContainerExploit(Mobile from, Item item)
        {
            return item.IsChildOf(from.Backpack) && item.Movable && item is BaseContainer bc && !bc.Empty;
        }
    }
}