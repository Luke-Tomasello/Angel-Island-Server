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

/* Scripts/Items/Addons/TourneyStone/ItemPropertyCondition.cs
 * ChangeLog :
 *  02/28/07, weaver
 *		Added safety check to ensure object being passed is correct.
 * 	02/25/07, weaver
 *		Initial creation (code moved out of TournamentStoneAddon.cs)
 *
 */

using Server.Commands;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace Server.Items
{

    public class ItemPropertyCondition : RuleCondition
    {
        // Limitation on the item property

        public override bool Guage(object o, ref ArrayList Fallthroughs)
        {
            if (o == null || !(o is HeldItem))          // wea: 28/Feb/2007 Added safety check
                return false;

            // We'll be dealing with a helditem object then
            HeldItem ipi = (HeldItem)o;

            // Limit
            // -1	- Fall through : if the item matches, passes all checks
            // 0	- Require the value to be at least this / require this property
            // 1	- Limit the value to a max of this / only this

            if (ipi.m_Type == ItemType || ipi.m_Type.IsSubclassOf(ItemType))
            {
                PlayerMobile FakeGM = new PlayerMobile();
                FakeGM.AccessLevel = AccessLevel.GameMaster;

                int FailCount = 0;

                foreach (Item item in ipi.Ref)
                {
                    if (Fallthroughs.Contains(item))
                        continue;       // Fallthrough

                    string PropTest = Properties.GetValue(FakeGM, item, Property);

                    if (PropertyVal != "")
                    {
                        Regex IPMatch = new Regex("= \"*" + PropertyVal, RegexOptions.IgnoreCase);
                        if (IPMatch.IsMatch(PropTest))
                        {
                            switch (Limit)
                            {
                                case -1:
                                    // Not required, but has fallthrough and matches so skip to next item reference
                                    Fallthroughs.Add(item);
                                    continue;
                                case 1:
                                    // It's limited to this and has matched, so that's fine
                                    continue;
                                case 0:
                                    // Required, matched, so fine
                                    continue;
                            }
                        }
                        else
                        {
                            switch (Limit)
                            {
                                case -1:
                                    // Not required, so don't worry but don't fallthrough either
                                    continue;
                                case 1:
                                    // It's limited to this and doesn't match, so it fails
                                    break;
                                case 0:
                                    // Required, not matched so not cool
                                    break;
                            }
                        }
                    }
                    else
                    {
                        // Ascertain numeric value
                        string sNum = "";
                        int iStrPos = Property.Length + 3;

                        while (PropTest[iStrPos] != ' ')
                            sNum += PropTest[iStrPos++];

                        int iCompareTo;

                        try
                        {
                            iCompareTo = Convert.ToInt32(sNum);
                        }
                        catch (Exception exc)
                        {
                            Console.WriteLine("TourneyStoneAddon: Exception - (trying to convert {1} to integer)", exc, sNum);
                            continue;
                        }

                        switch (Limit)
                        {
                            case 0:
                                if (iCompareTo >= Quantity)
                                    continue;
                                break;
                            case 1:
                                if (iCompareTo <= Quantity)
                                    continue;
                                break;
                            case -1:
                                if (iCompareTo <= Quantity)
                                {
                                    Fallthroughs.Add(item);
                                    continue;
                                }
                                break;
                        }
                    }

                    // FAILED!!! Otherwise we would have continued
                    FailCount++;

                } // Loop Item


                if (FailCount > 0)
                {
                    ClassNameTranslator cnt = new ClassNameTranslator();
                    Rule.FailTextDyn = string.Format("{0} x {1}", FailCount, cnt.TranslateClass(ipi.m_Type));

                    return false;

                }
                else
                    return true;
            }

            return true;
        }
    }

}