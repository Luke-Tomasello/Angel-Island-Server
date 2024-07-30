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

using Server.Diagnostics;
using Server.Items;

namespace Server.Commands
{

    public class ComLogger
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("ComLogger", AccessLevel.Administrator, new CommandEventHandler(ComLogger_OnCommand));
        }

        [Usage("ComLogger")]
        [Description("Logs all commodity deeds in world info")]
        public static void ComLogger_OnCommand(CommandEventArgs e)
        {
            LogHelper Logger = new LogHelper("Commoditydeed.log", true);

            foreach (Item m in World.Items.Values)
            {

                if (m != null)
                {
                    if (m is CommodityDeed && ((CommodityDeed)m).Commodity != null)
                    {
                        string output = string.Format("{0}\t{1,-25}\t{2,-25}",
                            m.Serial + ",", ((CommodityDeed)m).Commodity + ",", ((CommodityDeed)m).Commodity.Amount);

                        Logger.Log(LogType.Text, output);
                    }
                }
            }
            Logger.Finish();
        }
    }
}