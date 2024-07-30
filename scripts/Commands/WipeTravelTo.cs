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

/* Scripts/Commands/WipeTravelTo.cs
 * CHANGELOG:
 *  6/16/10, Adam
 *		Initial Version
 *		Retarget (to Zero) all moonstones and recall runes (including those in libraries) for a region.
 *		The region is attained by either targeting a HouseSign or TownshipStone. We can add other targets later.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Multis;
using Server.Targeting;
using System;

namespace Server.Commands
{

    public static class WipeTravelTo
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("WipeTravelTo", AccessLevel.Administrator, new CommandEventHandler(WipeTravelTo_OnCommand));
        }

        [Usage("WipeTravelTo")]
        [Description("wipe all runes, and moonstones to the given region.")]
        private static void WipeTravelTo_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new WipeTravelToTarget();
            e.Mobile.SendMessage("Target the house sign or township stone you wish to clean");
        }

        private class WipeTravelToTarget : Target
        {
            public WipeTravelToTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is HouseSign || target is TownshipStone)
                {
                    try
                    {
                        WipeMagicToTarget(from, target);
                    }
                    catch (Exception tse)
                    {
                        LogHelper.LogException(tse);
                    }
                }
                else
                {
                    from.SendMessage("That is neither a house sign nor township stone.");
                }
            }
        }

        public static void WipeMagicToTarget(Mobile from, object target)
        {
            try
            {
                Region region = null;
                if (target is HouseSign)
                {
                    BaseHouse bh = (target as HouseSign).Structure;
                    if (bh == null)
                    {
                        from.SendMessage("This house sign is not associated with any house.");
                        return;
                    }

                    region = bh.Region;
                    if (region == null)
                    {
                        from.SendMessage("This house is not associated with any region.");
                        return;
                    }
                }
                else if (target is TownshipStone)
                {
                    region = (target as TownshipStone).CustomRegion;
                    if (region == null)
                    {
                        from.SendMessage("This township stone is not associated with any region.");
                        return;
                    }
                }

                from.SendMessage("Searching for runes marked for this region");

                LogHelper Logger = new LogHelper("WipeMagicToTarget.log", from, false);

                foreach (Item item in World.Items.Values)
                {
                    if (item is RecallRune)
                    {
                        RecallRune rune = (RecallRune)item;

                        if (rune.Marked && rune.TargetMap != null && region.Contains(rune.Target))
                        {

                            object root = item.RootParent;

                            if (root is Mobile)
                            {
                                if (((Mobile)root).AccessLevel < AccessLevel.GameMaster)
                                {
                                    Logger.Log(LogType.Item, rune, rune.Description);
                                    rune.Target = new Point3D(0, 0, 0);
                                }
                            }
                            else
                            {
                                Logger.Log(LogType.Item, rune, rune.Description);
                                rune.Target = new Point3D(0, 0, 0);
                            }
                        }
                    }
                    else if (item is Moonstone)
                    {
                        Moonstone stone = (Moonstone)item;

                        if (stone.Marked && region.Contains(stone.Destination))
                        {
                            object root = item.RootParent;

                            if (root is Mobile)
                            {
                                if (((Mobile)root).AccessLevel < AccessLevel.GameMaster)
                                {
                                    Logger.Log(LogType.Item, stone, stone.Description);
                                    stone.Destination = new Point3D(0, 0, 0);
                                }
                            }
                            else
                            {
                                Logger.Log(LogType.Item, stone, stone.Description);
                                stone.Destination = new Point3D(0, 0, 0);
                            }
                        }
                    }
                    else if (item is Runebook)
                    {
                        Runebook book = (Runebook)item;

                        for (int i = 0; i < book.Entries.Count; ++i)
                        {
                            RunebookEntry entry = (RunebookEntry)book.Entries[i];

                            if (entry.Map != null && region.Contains(entry.Location))
                            {
                                object root = item.RootParent;

                                if (root is Mobile)
                                {
                                    if (((Mobile)root).AccessLevel < AccessLevel.GameMaster)
                                    {
                                        Logger.Log(LogType.Item, item, string.Format("{0}:{1}:{2}",
                                            i,
                                            entry.Description,
                                            book.Description));

                                        entry.Location = new Point3D(0, 0, 0);
                                    }
                                }
                                else
                                {
                                    Logger.Log(LogType.Item, item, string.Format("{0}:{1}:{2}",
                                        i,
                                        entry.Description,
                                        book.Description));

                                    entry.Location = new Point3D(0, 0, 0);
                                }
                            }
                        }
                    }
                }

                Logger.Finish();
                from.SendMessage("Done searching for runes withing house regions");

                // okay, now turn off all house security
                //int houseschecked =	Server.Multis.BaseHouse.SetSecurity(false);
                //e.Mobile.SendMessage("Setting {0} houses to insecure",houseschecked);

                return;
            }
            catch (Exception exc)
            {
                from.SendMessage("Exception in [findrunes -- see console.");
                System.Console.WriteLine("Exception in [findrunes: {0}", exc.Message);
                System.Console.WriteLine(exc.StackTrace);
                return;
            }

        }
    }
}