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

/* Engines/Crafting/Core/Resmelt.cs
 * CHANGELOG:
 *  12/16/21, Yoar
 *      Disabled Resmelt.cs. Resmelting is now handled in Recycle.cs.
 *	11/10/05, erlein
 *		Replaced SaveHue check with PlayerCrafted check.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

#if old
using Server.Items;
using Server.Targeting;
using System;

namespace Server.Engines.Craft
{
    public class Resmelt
    {
        public Resmelt()
        {
        }

        public static void Do(Mobile from, CraftSystem craftSystem, BaseTool tool)
        {
            int num = craftSystem.CanCraft(from, tool, null);

            if (num > 0)
            {
                from.SendGump(new CraftGump(from, craftSystem, tool, num));
            }
            else
            {
                from.Target = new InternalTarget(craftSystem, tool);
                from.SendLocalizedMessage(1044273); // Target an item to recycle.
            }
        }

        private class InternalTarget : Target
        {
            private CraftSystem m_CraftSystem;
            private BaseTool m_Tool;

            public InternalTarget(CraftSystem craftSystem, BaseTool tool)
                : base(2, false, TargetFlags.None)
            {
                m_CraftSystem = craftSystem;
                m_Tool = tool;
            }

            private bool Resmelt(Mobile from, Item item, CraftResource resource)
            {
                try
                {
                    if (CraftResources.GetType(resource) != CraftResourceType.Metal)
                        return false;

                    CraftResourceInfo info = CraftResources.GetInfo(resource);

                    if (info == null || info.ResourceTypes.Length == 0)
                        return false;

                    CraftItem craftItem = m_CraftSystem.CraftItems.SearchFor(item.GetType());

                    if (craftItem == null || craftItem.Resources.Count == 0)
                        return false;

                    CraftRes craftResource = craftItem.Resources.GetAt(0);

                    if (craftResource.Amount < 2)
                        return false; // Not enough metal to resmelt

                    Type resourceType = info.ResourceTypes[0];
                    Item ingot = (Item)Activator.CreateInstance(resourceType);

                    if (item is DragonBardingDeed || (item is BaseArmor && item.PlayerCrafted) || (item is BaseWeapon && item.PlayerCrafted) || (item is BaseClothing && item.PlayerCrafted))
                        ingot.Amount = craftResource.Amount / 2;
                    else
                        ingot.Amount = 1;

                    item.Delete();
                    from.AddToBackpack(ingot);

                    from.PlaySound(0x2A);
                    from.PlaySound(0x240);
                    return true;
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }

                return false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                int num = m_CraftSystem.CanCraft(from, m_Tool, null);

                if (num > 0)
                {
                    from.SendGump(new CraftGump(from, m_CraftSystem, m_Tool, num));
                }
                else
                {
                    bool success = false;
                    bool isStoreBought = false;

                    if (targeted is BaseArmor)
                    {
                        success = Resmelt(from, (BaseArmor)targeted, ((BaseArmor)targeted).Resource);
                        isStoreBought = !((Item)targeted).PlayerCrafted;
                    }
                    else if (targeted is BaseWeapon)
                    {
                        success = Resmelt(from, (BaseWeapon)targeted, ((BaseWeapon)targeted).Resource);
                        isStoreBought = !((Item)targeted).PlayerCrafted;
                    }
                    else if (targeted is DragonBardingDeed)
                    {
                        success = Resmelt(from, (DragonBardingDeed)targeted, ((DragonBardingDeed)targeted).Resource);
                        isStoreBought = false;
                    }

                    if (success)
                        from.SendGump(new CraftGump(from, m_CraftSystem, m_Tool, isStoreBought ? 500418 : 1044270)); // You melt the item down into ingots.
                    else
                        from.SendGump(new CraftGump(from, m_CraftSystem, m_Tool, 1044272)); // You can't melt that down into ingots.
                }
            }
        }
    }
}
#endif