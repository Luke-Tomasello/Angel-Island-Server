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

/* Scripts/Commands/Available.cs
 * Changelog
 *  11/27/06, Rhiannon
 *      Added check for whether guards are temporarily disabled.
 *      Added location descriptions for each CG.
 *      Added name filter to remove staff titles from names that include them.
 *	11/24/06, Rhiannon
 *		Initial creation.
 */
using Server.Items;
using Server.Network;
using Server.Regions;
using System;

namespace Server.Commands
{
    class Available
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("Available", AccessLevel.Counselor, new CommandEventHandler(AvailableMessage_OnCommand));
        }

        [Usage("Available")]
        [Description("Broadcasts a message announcing that a staffmember is available at a CG.")]
        public static void AvailableMessage_OnCommand(CommandEventArgs e)
        {
            string name = e.Mobile.Name;
            string staffName = GetStaffName(name);
            string title = "";
            Point3D location = e.Mobile.Location;
            string town = GetCG(location.X, location.Y);
            bool isGuarded = false;
            bool guardsDisabled = false;
            int TextHue = 0x482;

            if (town == "")
            {
                e.Mobile.SendMessage("You must be in a Counselors Guild to use this command.");
                return;
            }

            string place = DescribeLocation(e.Mobile.Map, location, town);

            switch (e.Mobile.AccessLevel)
            {
                case AccessLevel.Administrator: title = "Administrator "; break;
                case AccessLevel.Seer: title = "Seer "; break;
                case AccessLevel.GameMaster: title = "GM "; break;
                case AccessLevel.Counselor: title = "Counselor "; break;
                case AccessLevel.Owner:
                default: title = ""; break;
            }

            String message1 = string.Format("{0}{1} is presently holding court at {2} Counselor's Guild, located at {3}.", title, staffName, town, place);
            String message2 = string.Format("Please drop by to ask questions or just to chat.");
            String message3 = string.Format("{0} is not protected by guards. Enter at your own risk!", town);
            String message4 = string.Format("The guards in {0} are currently off duty. Enter at your own risk!", town);

            if (e.Mobile.Region is GuardedRegion)
            {
                isGuarded = true;
                if (((GuardedRegion)e.Mobile.Region).IsGuarded == false)
                    guardsDisabled = true;
            }

            foreach (NetState state in NetState.Instances)
            {
                Mobile m = state.Mobile;

                if (m != null)
                {
                    m.SendMessage(TextHue, message1);
                    m.SendMessage(TextHue, message2);
                    if (!isGuarded) m.SendMessage(TextHue, message3);
                    else if (guardsDisabled) m.SendMessage(TextHue, message4);
                }
            }
        }

        public static string GetStaffName(string name)
        {
            string staffName = "";
            string[] nameArray = name.Split(' ');

            switch (nameArray[0])
            {
                case "Counselor":
                case "GM":
                case "Seer":
                    {
                        for (int i = 1; i < nameArray.Length; i++)
                            staffName = staffName + nameArray[i] + " ";
                        staffName = staffName.Substring(0, staffName.Length - 1);
                        break;
                    }
                default:
                    {
                        staffName = name;
                        break;
                    }
            }
            return staffName;
        }

        public static string GetCG(int x, int y)
        {
            if (x >= 1496 && y >= 1512 && x < 1511 && y < 1527)
                return "Britain";
            if (x >= 1624 && y >= 1656 && x < 1639 && y < 1671)
                return "East Britain";
            if (x >= 1408 && y >= 3696 && x < 1431 && y < 3720)
                return "Jhelom";
            if (x >= 3712 && y >= 2168 && x < 3727 && y < 2183)
                return "Magincia";
            if (x >= 2427 && y >= 427 && x < 2442 && y < 442)
                return "Minoc";
            if (x >= 4468 && y >= 1118 && x < 4491 && y < 1133)
                return "Moonglow";
            if (x >= 3760 && y >= 1184 && x < 3783 && y < 1207)
                return "Nujel'm";
            if (x >= 3656 && y >= 2496 && x < 3671 && y < 2511)
                return "Ocllo";
            if (x >= 2934 && y >= 3342 && x < 2967 && y < 3367)
                return "Serpent's Hold";
            if (x >= 608 && y >= 2112 && x < 631 && y < 2143)
                return "Skara Brae";
            if (x >= 1874 && y >= 2827 && x < 1909 && y < 2845)
                return "Trinsic";
            if (x >= 1800 && y >= 2700 && x < 1817 && y < 2715)
                return "Northwest Trinsic";
            if (x >= 2823 && y >= 928 && x < 2855 && y < 959)
                return "Vesper";
            if (x >= 608 && y >= 976 && x < 623 && y < 983)
                return "Yew";
            else return "";
        }

        public static string DescribeLocation(Map map, Point3D p, String town)
        {
            string location;
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            bool valid = Sextant.Format(p, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
            else
                location = "????";

            if (!valid)
                location = string.Format("{0} {1}", p.X, p.Y);

            switch (town)
            {
                case "Britain": { location = location + ", north of The Sorcerer's Delight"; break; }
                case "East Britain": { location = location + ", southwest of East Britain Bank"; break; }
                case "Jhelom": { location = location + ", north of The Dueling Pit"; break; }
                case "Magincia": { location = location + ", south of the Bank of Magincia"; break; }
                case "Minoc": { location = location + ", in the northwest part of town"; break; }
                case "Moonglow": { location = location + ", just north of the First Bank of Moonglow"; break; }
                case "Nujel'm": { location = location + ", in the northeast part of town"; break; }
                case "Ocllo": { location = location + ", in the northern part of town, west of the Bank of Ocllo"; break; }
                case "Serpent's Hold": { location = location + ", north of the North Docks"; break; }
                case "Skara Brae": { location = location + ", northeast of the Bank of Skara Brae"; break; }
                case "Trinsic": { location = location + ", in the southern part of town, east of the Bank of Britania"; break; }
                case "Northwest Trinsic": { location = location + ", north of the West Trinsic Gate"; break; }
                case "Vesper": { location = location + ", in the southern part of town, east of The Ironwood Inn"; break; }
                case "Yew": { location = location + ", east of central Yew"; break; }
            }

            return location;
        }

    }
}