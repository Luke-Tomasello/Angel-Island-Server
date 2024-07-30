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

using System;

namespace Server.Misc
{
    public class Broadcasts
    {
        public static void Initialize()
        {
            EventSink.Crashed += new CrashedEventHandler(EventSink_Crashed);
            EventSink.Shutdown += new ShutdownEventHandler(EventSink_Shutdown);
        }

        public static void EventSink_Crashed(CrashedEventArgs e)
        {
            try
            {
                World.Broadcast(0x35, true, "The server has crashed.");
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void EventSink_Shutdown(ShutdownEventArgs e)
        {
            try
            {
                World.Broadcast(0x35, true, "The server has shut down.");
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
    }
}