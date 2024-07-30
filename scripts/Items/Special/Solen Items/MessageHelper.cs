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

using Server.Network;

namespace Server
{
    public class MessageHelper
    {
        public static void SendLocalizedMessageTo(Item from, Mobile to, int number, int hue)
        {
            SendLocalizedMessageTo(from, to, number, "", hue);
        }

        public static void SendLocalizedMessageTo(Item from, Mobile to, int number, string args, int hue)
        {
            to.Send(new MessageLocalized(from.Serial, from.ItemID, MessageType.Regular, hue, 3, number, "", args));
        }
    }
}