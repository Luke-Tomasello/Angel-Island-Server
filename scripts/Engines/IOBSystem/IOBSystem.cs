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

/* Scripts/Engines/IOBSystem/IOBSystems.cs
 * CHANGELOG:
 *  3/29/23, Adam (IsEnemy)
 *      NPCs don't fight NPCs on shards other than Angel Island
 *	7/28/07, Pix
 *		Spelling fix.
 *	6/7/06, Pix
 *		Removed requirement for IOBAlignment based on IOBEquipped for PMs.
 *	10/04/05, Pix
 *		Added new GetIOBName() function.
 *	9/29/05, Adam
 *		Initial Version
 *		a. Add IsFriend and IsEnemy functions
 */

using Server.Mobiles;

namespace Server.Engines.IOBSystem
{
    public class IOBSystem
    {
        // A player's 'true' alignment (no longer requires that the IOB be equipped)
        public static IOBAlignment GetIOBAlignment(Mobile m)
        {
            if (m == null)
                return IOBAlignment.None;

            BaseCreature bc = m as BaseCreature;
            if (bc != null)
                return bc.IOBAlignment;

            PlayerMobile pm = m as PlayerMobile;
            if (pm != null) // && pm.IOBEquipped == true)
                return pm.IOBAlignment;

            return IOBAlignment.None;
        }

        //returns the description string for the alignment
        public static string GetIOBName(IOBAlignment a)
        {
            string name = "";

            switch (a)
            {
                case IOBAlignment.Brigand:
                    name = "Brigands";
                    break;
                case IOBAlignment.Council:
                    name = "Council";
                    break;
                case IOBAlignment.Good:
                    name = "Britannian Militia";
                    break;
                case IOBAlignment.None:
                    name = "Unaligned";
                    break;
                case IOBAlignment.Orcish:
                    name = "Orcs";
                    break;
                case IOBAlignment.Pirate:
                    name = "Pirates";
                    break;
                case IOBAlignment.Savage:
                    name = "Savages";
                    break;
                case IOBAlignment.Undead:
                    name = "Undead";
                    break;
                default:
                    name = a.ToString();
                    break;
            }

            return name;
        }

        // A player's 'true' alignment (no longer requires that the IOB be equipped)
        public static bool IsIOBAligned(Mobile m)
        {
            if (GetIOBAlignment(m) != IOBAlignment.None)
                return true;
            else
                return false;
        }

        public static bool IsEnemy(Mobile m1, Mobile m2)
        {
            // NPCs don't fight NPCs on shards other than Angel Island
            if (!m1.Player && !m2.Player && Core.RuleSets.SiegeStyleRules())
                return false;

            // can't be an IOB enemy if you're not aligned
            if (!IsIOBAligned(m1) || !IsIOBAligned(m2))
                return false;

            // must be an ememy if they are on different teams
            if (GetIOBAlignment(m1) != GetIOBAlignment(m2))
                return true;

            // not an IOB enemy
            return false;
        }

        public static bool IsFriend(Mobile m1, Mobile m2)
        {
            // can't be an IOB friend if you're not aligned
            if (!IsIOBAligned(m1) || !IsIOBAligned(m2))
                return false;

            // must be a friend if they are on the same teams
            if (GetIOBAlignment(m1) == GetIOBAlignment(m2))
                return true;

            // not an IOB friend
            return false;
        }

        public static void SendKinMessage(IOBAlignment align, string message)
        {
            SendKinMessage(new IOBAlignment[] { align }, message);
        }

        private static void SendKinMessage(IOBAlignment[] aligns, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            foreach (Mobile msg in World.Mobiles.Values)
            {
                if (msg != null && msg is PlayerMobile)
                {
                    PlayerMobile pm = (PlayerMobile)msg;
                    foreach (IOBAlignment align in aligns)
                        if (pm.IOBRealAlignment == align)
                        {
                            pm.SendMessage(message);
                            break;
                        }
                }
            }
        }
    }
}