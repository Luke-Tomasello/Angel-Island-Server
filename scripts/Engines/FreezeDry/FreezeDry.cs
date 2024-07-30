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

/* Scripts\Commands\FreezeDry.cs
 * Changelog
 *  6/7/07, Adam
 *      Add FDStats command to dump the eligible vs FD'ed containers
 *	12/14/05 Taran Kain
 *		Initial version.
 */

namespace Server.Commands
{
    /// <summary>
    /// Summary description for FreezeDry.
    /// </summary>
    public class FreezeDry
    {
#if false
        public static void Initialize()
        {
            Server.CommandSystem.Register("FDStats", AccessLevel.Administrator, new CommandEventHandler(On_FDStats));
            Server.CommandSystem.Register("StartFDTimers", AccessLevel.Administrator, new CommandEventHandler(On_StartFDTimers));
            Server.CommandSystem.Register("RehydrateAll", AccessLevel.Administrator, new CommandEventHandler(On_RehydrateAll));
        }

        public static void On_FDStats(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Display FreezeDry status for all eligible containers...");

            DateTime start = DateTime.UtcNow;
            int containers = 0;
            int eligible = 0;
            int freezeDried = 0;
            int scheduled = 0;
            int orphans = 0;
            foreach (Item i in World.Items.Values)
            {
                if (i as Container != null)
                {
                    Container cx = i as Container;
                    containers++;
                    if (cx.CanFreezeDry == true)
                    {
                        eligible++;
                        if (cx.IsFreezeDried == true)
                            freezeDried++;
                    }

                    if (cx.IsFreezeScheduled == true)
                        scheduled++;

                    if (cx.CanFreezeDry == true && cx.IsFreezeDried == false && cx.IsFreezeScheduled == false)
                        orphans++;
                }
            }

            e.Mobile.SendMessage("Out of {0} eligible containers, {1} are freeze dried, {2} scheduled, and {3} orphans.", eligible, freezeDried, scheduled, orphans);
            DateTime end = DateTime.UtcNow;
            e.Mobile.SendMessage("Finished in {0}ms.", (end - start).TotalMilliseconds);
        }

        public static void On_StartFDTimers(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Starting FreezeTimers for all eligible containers...");

            DateTime start = DateTime.UtcNow;
            foreach (Item i in World.Items.Values)
                i.OnRehydrate();

            DateTime end = DateTime.UtcNow;
            e.Mobile.SendMessage("Finished in {0}ms.", (end - start).TotalMilliseconds);
        }

        public static void On_RehydrateAll(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Rehydrating all FreezeDried containers...");

            int count = 0;

            DateTime start = DateTime.UtcNow;
            //ArrayList al = new ArrayList(World.Items.Values);
            List<Item> al = new List<Item>(World.Items.Values);
            foreach (Item i in al)
            {
                if (i.IsFreezeDried)
                {
                    i.Rehydrate();
                    count++;
                }
            }

            DateTime end = DateTime.UtcNow;
            e.Mobile.SendMessage("{0} containers rehydrated in {1} seconds.", count, (end - start).TotalSeconds);
            e.Mobile.SendMessage("Rehydrate() averaged {0}ms per call.", (end - start).TotalMilliseconds / count);
        }
#endif
    }
}