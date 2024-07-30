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

namespace Server.Misc
{
    public class Animations
    {
        public static void Initialize()
        {
            EventSink.AnimateRequest += new AnimateRequestEventHandler(EventSink_AnimateRequest);
        }

        private static void EventSink_AnimateRequest(AnimateRequestEventArgs e)
        {
            Mobile from = e.Mobile;

            int action;

            switch (e.Action)
            {
                case "bow": action = 32; break;
                case "salute": action = 33; break;
                default: return;
            }

            if (from.Alive && !from.Mounted && from.Body.IsHuman)
                from.Animate(action, 5, 1, true, false, 0);
        }
    }
}