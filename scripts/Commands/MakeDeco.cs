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

/* Scripts/Commands/MakeDeco.cs
 * ChangeLog
 *	05/10/05, erlein
 *		Fixed tab formatting :/
 *	05/10/05, erlein
 *      	* Added search of CustomRegion type regions to include other towns.
 *      	* Excluded all containers that have "ransom" in Name property.
 *      	* Excluded anything that IsLockedDown or IsSecure.
 *	05/10/05, erlein
 *      	Added check for region name "Ocllo Island"
 *	05/10/05, erlein
 *		Initial creation.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Regions;

namespace Server.Commands
{
    public class MakeDeco
    {

        public static void Initialize()
        {
            Server.CommandSystem.Register("MakeDeco", AccessLevel.Administrator, new CommandEventHandler(MakeDeco_OnCommand));
        }

        [Usage("MakeDeco")]
        [Description("Turns all appropriate external containers to deco only.")]
        private static void MakeDeco_OnCommand(CommandEventArgs arg)
        {
            Mobile from = arg.Mobile;

            from.SendMessage("Powerfull command, you will need to explicitly enable this command in MakeDeco.cs");
            if (from != null) // hack to avoid unreachable code complaint from compiler
                return;

            LogHelper Logger = new LogHelper("makedeco.log", from, true);

            // Loop through town regions and search out items
            // within

            foreach (Region reg in /*from.Map.Regions.Values*/ from.Map.RegionsSorted)
            {
                if (reg is GuardedRegion)
                {
                    foreach (Rectangle3D area in reg.Coords)
                    {
                        IPooledEnumerable eable = from.Map.GetItemsInBounds(new Rectangle2D(area.Start, area.End));

                        foreach (object obj in eable)
                        {
                            if (obj is Container)
                            {
                                Container cont = (Container)obj;

                                if (cont.Movable == false &&
                                    cont.PlayerCrafted == false &&
                                    cont.Name != "Goodwill" &&
                                    !(cont.RootParent is Mobile) &&
                                    !(cont is TrashBarrel) &&
                                    cont.Deco == false &&
                                    !(cont.IsLockedDown) &&
                                    !(cont.IsSecure))
                                {

                                    // Exclude ransom chests
                                    if (cont.Name != null && cont.Name.ToLower().IndexOf("ransom") >= 0)
                                        continue;

                                    // Found one
                                    cont.Deco = true;
                                    Logger.Log(LogType.Item, cont);
                                }
                            }
                        }

                        eable.Free();
                    }
                }
            }

            Logger.Finish();
        }
    }
}