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

/* Scripts/Items/Addons/TourneyStone/RuleCondition.cs
 * ChangeLog :
 * 	02/25/07, weaver
 *		Initial creation (code moved out of TournamentStoneAddon.cs)
 *
 */

using Server.Commands;
using Server.Mobiles;
using System;
using System.Collections;
using System.Text.RegularExpressions;
namespace Server.Items
{
    public class PropertyCondition : RuleCondition
    {

        // Limitation on the playermobile property

        public override bool Guage(object o, ref ArrayList Fallthroughs)
        {
            if (!(o is Mobile) || o == null)
                return false;

            Mobile Player = (Mobile)o;

            if (Property.Length > 7)
            {
                if (Property.Substring(0, 6) == "skill ")
                {
                    // Looking for a skill

                    string SkillArg = Property.Substring(6);
                    string strSkill = ((SkillArg.Substring(0, 1)).ToUpper() + SkillArg.Substring(1));

                    for (int isk = 0; isk < 53; isk++)
                    {
                        // Fallthrough if the player matched a skill that allowed such
                        if (Fallthroughs.Contains(Player))
                            continue;

                        // Limit :
                        // -1	- Fall through : if the skill value matches or is less, passes all other conditions
                        // 0	- Require the skill to be at least this
                        // 1	- Limit the skill to this

                        if (Player.Skills[isk].SkillName.ToString() == strSkill)
                        {
                            switch (Limit)
                            {
                                case -1:
                                    if (Player.Skills[isk].Base > Quantity)
                                    {
                                        Fallthroughs.Add(Player);
                                        return true;
                                    }
                                    break;

                                case 1:
                                    if (Player.Skills[isk].Base > Quantity)
                                    {
                                        Rule.FailTextDyn = string.Format("Your skill in this is {0}.", Player.Skills[isk].Base);
                                        return false;
                                    }
                                    break;

                                case 0:
                                    if (Player.Skills[isk].Base < Quantity)
                                    {
                                        Rule.FailTextDyn = string.Format("Your skill in this is {0}.", Player.Skills[isk].Base);
                                        return false;
                                    }
                                    break;
                            }
                        }
                    }

                    return true;
                }
            }

            // Regular player property


            if (PropertyVal == "")
            {
                // This is a quantity match
                PlayerMobile FakeGM = new PlayerMobile();
                FakeGM.AccessLevel = AccessLevel.GameMaster;

                string sVal = Properties.GetValue(FakeGM, Player, Property);

                FakeGM.Delete();

                int iStrPos = Property.Length + 3;

                // Ascertain numeric value
                string sNum = "";
                while (sVal[iStrPos] != ' ')
                    sNum += sVal[iStrPos++];

                int iVal;

                try
                {
                    iVal = Convert.ToInt32(sNum);
                }
                catch (Exception exc)
                {
                    Console.WriteLine("TourneyStoneAddon: Exception - (trying to convert {1} to integer)", exc, sNum);
                    return true;
                }

                // Compare

                switch (Limit)
                {
                    case 1:
                        if (Quantity >= iVal)
                            return true;
                        break;
                    case 0:
                        if (Quantity <= iVal)
                            return true;
                        break;
                }

                return false;
            }
            else
            {
                // This is a text match
                Regex PattMatch = new Regex("= \"*" + PropertyVal, RegexOptions.IgnoreCase);

                PlayerMobile FakeGM = new PlayerMobile();
                FakeGM.AccessLevel = AccessLevel.GameMaster;

                if (PattMatch.IsMatch(Properties.GetValue(FakeGM, Player, Property)))
                {
                    FakeGM.Delete();
                    return false;
                }

                FakeGM.Delete();
            }

            return true;
        }
    }

}