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

/* Scripts/Items/Addons/TourneyStone/ItemCondition.cs
 * ChangeLog :
 * 	02/25/07, weaver
 *		Initial creation (code moved out of TournamentStoneAddon.cs)
 *
 */

using Server.Misc;
using System.Collections;


namespace Server.Items
{

    public class ItemCondition : RuleCondition
    {
        // Item type + amount kind of rule

        public override bool Guage(object o, ref ArrayList Fallthroughs)
        {
            if (o == null || o.GetType() != typeof(ArrayList))
                return false;

            // wea: 25/Feb/2007 Modified to receive CurrentHeld list as opposed
            // to individual HeldItem

            int RuleItemMatchQty = 0;

            foreach (HeldItem hi in ((ArrayList)o))
            {
                if (hi.m_Type == ItemType || hi.m_Type.IsSubclassOf(ItemType))
                    RuleItemMatchQty += hi.m_Count;
            }


            // Limit
            // -1	- Fall through : if the item count matches, all conditions pass
            // 0	- Require the count to be at least this
            // 1	- Limit the count to a maximum of this

            switch (Limit)
            {
                case -1:
                    if (RuleItemMatchQty >= Quantity)
                    {
                        foreach (HeldItem hi in ((ArrayList)o))
                        {
                            foreach (Item item in hi.Ref)
                            {
                                Fallthroughs.Add(item);
                            }
                        }
                    }
                    return true;
                case 1:
                    if (RuleItemMatchQty > Quantity)
                        break;
                    return true;
                case 0:
                    if (RuleItemMatchQty < Quantity)
                        break;
                    return true;
            }

            // FAILED!!

            ClassNameTranslator cnt = new ClassNameTranslator();
            Rule.FailTextDyn = string.Format("{0} x {1}", RuleItemMatchQty, cnt.TranslateClass(this.ItemType));

            return false;
        }
    }

}