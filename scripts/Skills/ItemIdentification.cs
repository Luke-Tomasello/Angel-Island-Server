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

/* Scripts/Skills/ItemIdentification.cs
 * ChangeLog:
 *  4/17/07, Adam
 *      Add back support for EnchantedScrolls 
 *	3/13/07, weaver
 *		- Centralised identification functionality
 *		- Added RareData based rarity report on successful ID.
 *	11/4/05, weaver
 *		Added check and report on whether item identified is player crafted.
 *	8/11/05, weaver
 *		Added scale for chance to ID enchanted scroll, replacing fixed requirement.
 *	7/27/05, weaver
 *		Added call to skill check function for EnchantedItem types to ensure skill gains
 *		properly.
 *	7/14/05, Kit
 *		Changed Item id for enchateditem type to display propertys automatically after decoded.
 *	7/13/05, weaver
 *		Added check for EnchantedItem type on target of skill use. Reformatted
 *		this changelog too.
 *	05/11/04, Pulse
 *		Added "is BaseJewel" and "is BaseClothing" conditions to the OnTarget method to
 *		implement identifying of magic jewelry and clothes.
 */

using Server.Targeting;
using System;
using System.Collections;
using static Server.Item;

namespace Server.Items
{
    public class ItemIdentification
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.ItemID].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile from)
        {
            from.SendLocalizedMessage(500343); // What do you wish to appraise and identify?
            from.Target = new InternalTarget();

            return TimeSpan.FromSeconds(1.0);
        }

        // wea: 14/Mar/2007 Added rarity check
        public static bool IdentifyItem(Mobile from, object o, bool simple = false)
        {
            if (o is EnchantedScroll)
                o = (o as EnchantedScroll).Item;

            if (o is not Item)
                return false;

            Item item = (Item)o;

            if (o is BaseWeapon)
                ((BaseWeapon)o).Identified = true;
            else if (o is BaseArmor)
                ((BaseArmor)o).Identified = true;
            else if (o is BaseJewel)
                ((BaseJewel)o).Identified = true;
            else if (o is BaseClothing)
                ((BaseClothing)o).Identified = true;
            else if (o is BaseStatue)
                ((BaseStatue)o).Identified = true;

            //if (!Core.RuleSets.AOSRules())
            //    item.OnSingleClick(from);

            from.SendMessage(item.OldSchoolName());

            if (!simple)
            {
                // noise
                //from.SendMessage("You have successfully identified the item.");

                string? origin = GetOriginDescription(item);

                if (!string.IsNullOrEmpty(origin))
                    from.SendMessage(origin);
            }

            return true;
        }
        private static string? GetOriginDescription(Item item)
        {
            if (item == null)
                return null;

            if (item.PlayerCrafted || item.Origin.HasFlag(Genesis.Player))
                return "You determine that the item was crafted by a player.";

            if (item.Origin.HasFlag(Genesis.Monster))
                return "You determine that the item came from a monster.";

            if (item.Origin.HasFlag(Genesis.TChest))
                return "You determine that the item came from a treasure map chest.";

            if (item.Origin.HasFlag(Genesis.DChest))
                return "You determine that the item came from a dungeon chest.";

            if (item.Origin.HasFlag(Genesis.Chest))
                return "You determine that the item came from a treasure chest.";

            if (item.Origin.HasFlag(Genesis.Scroll))
                return "You determine that the item came from an enchanted scroll.";

            if (item.Origin.HasFlag(Genesis.BOD))
                return "You determine that the item was issued as a bulk order reward.";

            if (item.Origin.HasFlag(Genesis.Legacy))
                return "You determine that the item is legacy magic loot.";

            return "You determine that the item is of unknown origin.";
        }


        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Item item)
                {
                    if (item is Container container &&
                        from.Skills[SkillName.ItemID].Value >= 100.0 &&
                        item.Movable &&
                        item.RootParent == from)
                    {
                        ArrayList list = container.GetDeepItems();

                        int identified = 0;

                        foreach (object obj in list)
                        {
                            if (obj is Item child)
                            {
                                if (IdentifyItem(from, child, simple: true))
                                    identified++;
                            }
                        }

                        from.SendMessage("You identify {0} item{1}.", identified, identified == 1 ? "" : "s");
                        return;
                    }

                    if (from.CheckTargetSkill(SkillName.ItemID, o, minSkill: 0, maxSkill: 100, new object[2] /*contextObj*/))
                        IdentifyItem(from, o);
                    else
                        from.SendLocalizedMessage(500353); // You are not certain...
                }
                else if (o is Mobile)
                {
                    ((Mobile)o).OnSingleClick(from);
                }
                else
                {
                    from.SendLocalizedMessage(500353); // You are not certain...
                }
            }
        }
    }
}