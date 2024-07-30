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

/* Scripts\Engines\Alignment\IAlignmentItem.cs
 * Changelog:
 *  4/14/23, Yoar
 *      Initial version.
 */

namespace Server.Engines.Alignment
{
    public interface IAlignmentItem
    {
        public AlignmentType GuildAlignment { get; }
        public bool EquipRestricted { get; }
    }

    public static class AlignmentItem
    {
        public static bool CanEquip(Mobile mob, Item item, bool message)
        {
            IAlignmentItem alignmentItem = item as IAlignmentItem;

            if (alignmentItem == null)
                return true;

            AlignmentType alignment = alignmentItem.GuildAlignment;

            if (alignment == AlignmentType.None)
                return true;

            if (alignmentItem.EquipRestricted && AlignmentSystem.Find(mob) != alignment)
            {
                if (message)
                    mob.SendMessage("Only {0} aligned players may equip this item.", AlignmentSystem.GetName(alignment));

                return false;
            }

            return true;
        }
    }
}