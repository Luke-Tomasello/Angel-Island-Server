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

/* Scripts/Commands/RehydrateWorld.cs
 * 	CHANGELOG:
 * 	2/23/06, Adam
 *	Initial Version
 */

namespace Server.Commands
{
    public class RehydrateWorld
    {
#if false
        public static void Initialize()
        {
            Server.CommandSystem.Register("RehydrateWorld", AccessLevel.Administrator, new CommandEventHandler(RehydrateWorld_OnCommand));
        }

        [Usage("RehydrateWorld")]
        [Description("Rehydrates the entire world.")]
        public static void RehydrateWorld_OnCommand(CommandEventArgs e)
        {
            // make it known			
            Server.World.Broadcast(0x35, true, "The world is rehydrating, please wait.");
            Console.WriteLine("World: rehydrating...");
            DateTime startTime = DateTime.UtcNow;

            LogHelper Logger = new LogHelper("RehydrateWorld.log", e.Mobile, true);

            // Extract property & value from command parameters
            ArrayList containers = new ArrayList();

            // Loop items and check vs. types
            foreach (Item item in World.Items.Values)
            {
                if (item is Container && item.Deleted == false)
                {
                    if ((item as Container).CanFreezeDry == true)
                        containers.Add(item);
                }
            }

            Logger.Log(LogType.Text,
                string.Format("{0} containers scheduled for Rehydration...",
                    containers.Count));

            int count = 0;
            for (int ix = 0; ix < containers.Count; ix++)
            {
                Container cont = containers[ix] as Container;

                if (cont != null)
                {
#if false
                    // Rehydrate it if necessary
                    if (cont.CanFreezeDry && cont.IsFreezeDried == true)
                        cont.Rehydrate();
#endif
                    count++;
                }
            }

            Logger.Log(LogType.Text,
                string.Format("{0} containers actually Rehydrated", count));

            Logger.Finish();

            //e.Mobile.SendAsciiMessage("{0} containers actually Rehydrated", count);

            DateTime endTime = DateTime.UtcNow;
            Console.WriteLine("done in {0:F1} seconds.", (endTime - startTime).TotalSeconds);
            Server.World.Broadcast(0x35, true, "World rehydration complete. The entire process took {0:F1} seconds.", (endTime - startTime).TotalSeconds);
        }
#endif
    }
}