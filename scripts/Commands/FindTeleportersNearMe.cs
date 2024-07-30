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

/* Scripts\Commands\FindTeleportersNearMe.cs
 * ChangeLog
 *  5/29/2023
 *		Initial creation - designed to retrieve list of teleporters near GM
 *		distance passed as parameter.
*/
using Server.Diagnostics;
using Server.Items;
using Server.Mobiles;
using System.Collections;
using System.Collections.Generic;

namespace Server.Commands
{
    public class FindTeleportersNearMe
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindTeleportersNearMe", AccessLevel.GameMaster, new CommandEventHandler(FindTeleportersNearMe_OnCommand));
        }

        [Usage("FindTeleportersNearMe <tile range>")]
        [Description("Finds locations of all teleporters within distance.")]
        private static void FindTeleportersNearMe_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            int X = from.Location.X;
            int Y = from.Location.Y;

            int TileRange = arg.GetInt32(0);
            if (int.TryParse(arg.GetString(0), out TileRange) == false)
            {
                arg.Mobile.SendMessage("Usage: FindTeleportersNearMe <tile range>");
                return;
            }

            // Perform search and retrieve list of spawner matches
            LogHelper Logger = new LogHelper("FindTeleportersNearMe.log", from, true);
            List<Teleporter> teleportersList = Findteleporters(X, Y, TileRange);

            if (teleportersList.Count > 0)
            {
                PlayerMobile pm = arg.Mobile as PlayerMobile;

                // Have results so sort, loop and display message for each match
                teleportersList.Sort();

                // reset jump table
                pm.JumpIndex = 0;
                pm.JumpList = new ArrayList();

                foreach (Teleporter tp in teleportersList)
                {
                    pm.JumpList.Add(tp);
                    Logger.Log(LogType.Item, tp);
                }
            }

            arg.Mobile.SendMessage("Done. {0} teleporters located.", teleportersList.Count);
            Logger.Finish();
        }

        public static List<Teleporter> Findteleporters(int x, int y, int tilerange)
        {
            List<Teleporter> TeleporterList = new();
            foreach (Item item in World.Items.Values)
            {
                if (item is Teleporter tele)
                {
                    int spX = tele.Location.X - x;
                    int spY = tele.Location.Y - y;
                    if ((tilerange == 0) || ((sqr(tele.Location.X - x) <= sqr(tilerange) && sqr(tele.Location.Y - y) <= sqr(tilerange))))
                        TeleporterList.Add(tele);
                }
            }

            return (TeleporterList);
        }

        private static int sqr(int num)
        {
            return ((num * num));
        }
    }
}