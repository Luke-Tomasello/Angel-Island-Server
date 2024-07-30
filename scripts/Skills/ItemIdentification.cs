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
        public static bool IdentifyItem(Mobile from, object o)
        {
            if (o is EnchantedScroll)
                o = (o as EnchantedScroll).Item;

            if (o is not Item)
                return false;

            Item itm = (Item)o;

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

            string idstr = "You determine that : ";

            if (itm.PlayerCrafted)
                idstr += "the item was crafted";
            else
                idstr += "the item is of an unknown origin";

            if (!Core.RuleSets.AOSRules())
                itm.OnSingleClick(from);

            /*if (itm.RareData > 0)
            {
                idstr += string.Format(" and is number {0} of a collection of {1}",
                                       itm.RareCurIndex,
                                      (itm.RareLastIndex - itm.RareStartIndex) + 1);
            }*/

            if (idstr != "")
            {
                from.SendMessage(idstr + ".");
            }

            return true;
        }


        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object o)
            {
                if (o is Item)
                {
                    if (from.CheckTargetSkill(SkillName.ItemID, o, 0, 100, new object[2] /*contextObj*/))
                        IdentifyItem(from, o);
                    else
                        from.SendLocalizedMessage(500353); // You are not certain...
                }
                else if (o is Mobile)
                    ((Mobile)o).OnSingleClick(from);
                else
                    from.SendLocalizedMessage(500353); // You are not certain...
            }
        }
    }
}