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

/* Scripts/Commands/FindRunes.cs
 * CHANGELOG:
 *  8/26/22, Adam
 *      Add EventSink_HousePlaced to retarget runes whose destination is somewhere in the house (castle courtyard is the exploit) 
 *  7/6//08, Adam
 *      - total redesign to support searching for regions by name, type ,point, or uid
 *      - retarget now sets destination to 0, 0, 0 - a blocked location
 *      Usage: [Retarget <-regionName|-type|-point|-RegionUID> <name|type|point|uid>
 *      Usage: [FindRunes <-regionName|-type|-point|-RegionUID> <name|type|point|uid>
 *	03/28/05, erlein
 *		Modified calls to LogHelper() so only sends rune object and
 *		command specific info.
 *	03/25/05, erlein
 *		Integrated with LogHelper class.
 *	03/23/05, erlein
 *		Moved to /Scripts/Commands/FindRunes.cs (for Find* command normalization).
 *		Changed namespace to Server.Commands.
 *	3/7/05, Adam
 *		Add player's account name to output
 *	3/6/05: Pix
 *		Added [Retarget command to retarget runes for a region.
 *	3/5/05, Adam
 *		1. Replace call to IsBad() with IsInRegion() in runebook check
 *		2. remove old default code all together as it was:
 *			a. always executing
 *			b. IsBad appears to check for Dungeons as well as GA possibly giving false positives
 *			c. redundant, as we likely have a region already defined for GA
 *		3. fix command massage to give a better explanation of the command.
 *	3/5/05: Pix
 *		Added Moonstones!
 *	3/5/05: Pix
 *		Changed to be more generic.
 *		If you pass in a (case-sensitive) region name, it'll list the runes for that region.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Multis;
using System;
using System.Collections;

namespace Server.Commands
{
    public class FindRunes
    {
        public static void Initialize()
        {
            Server.CommandSystem.Register("FindRunes", AccessLevel.Administrator, new CommandEventHandler(FindRunes_OnCommand));
            Server.CommandSystem.Register("Retarget", AccessLevel.Administrator, new CommandEventHandler(RetargetRunes_OnCommand));
            EventSink.HousePlaced += new HousePlacedEventHandler(EventSink_HousePlaced);
        }

        /// <summary>
        /// Re Target any recall runes, RunBooks, and moonstones that are pointing to this house placement rect
        /// </summary>
        /// <param name="e"></param>
        private static void EventSink_HousePlaced(HousePlacedEventArgs e)
        {
            Item hs = World.FindItem(e.House);
            if (hs is BaseHouse bh)
            {
                foreach (Item item in World.Items.Values)
                {
                    if (item is RecallRune rune)
                    {
                        if (rune.Marked && rune.TargetMap != null && BaseHouse.FindHouseAt(rune.Target, e.Map, 16) != null && BaseHouse.FindHouseAt(rune.Target, e.Map, 16) == bh)
                        {   // re target necessary
                            rune.Target = bh.BanLocation;
                        }
                    }
                    else if (item is Moonstone stone)
                    {
                        if (stone.Marked && BaseHouse.FindHouseAt(stone.Destination, e.Map, 16) != null && BaseHouse.FindHouseAt(stone.Destination, e.Map, 16) == bh)
                        {   // re target necessary
                            stone.Destination = bh.BanLocation;
                        }
                    }
                    else if (item is Runebook book)
                    {
                        for (int i = 0; i < book.Entries.Count; ++i)
                        {
                            RunebookEntry entry = (RunebookEntry)book.Entries[i];

                            if (entry.Map != null && BaseHouse.FindHouseAt(entry.Location, e.Map, 16) != null && BaseHouse.FindHouseAt(entry.Location, e.Map, 16) == bh)
                            {   // re target necessary
                                entry.Location = bh.BanLocation;
                            }
                        }
                    }
                }
            }
        }

        [Usage("Retarget <-regionName|-type|-point|-RegionUID> <name|type|point|uid>")]
        [Description("Retargets runes, runebooks, or moonstones of the specified region to jail.")]
        public static void RetargetRunes_OnCommand(CommandEventArgs e)
        {
            ArrayList regionList = new ArrayList();
            if (HaveRegions(e, regionList))
            {
                try
                {
                    LogHelper Logger = new LogHelper("retarget.log", e.Mobile, true);

                    foreach (Item item in World.Items.Values)
                    {
                        if (item is RecallRune)
                        {
                            RecallRune rune = (RecallRune)item;

                            for (int ix = 0; ix < regionList.Count; ix++)
                            {
                                if (rune.Marked && rune.TargetMap != null && (regionList[ix] as Region).Contains(rune.Target))
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
                        }
                        else if (item is Moonstone)
                        {
                            Moonstone stone = (Moonstone)item;

                            for (int ix = 0; ix < regionList.Count; ix++)
                            {
                                if (stone.Marked && (regionList[ix] as Region).Contains(stone.Destination))
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
                        }
                        else if (item is Runebook)
                        {
                            Runebook book = (Runebook)item;

                            for (int ix = 0; ix < regionList.Count; ix++)
                            {
                                for (int i = 0; i < book.Entries.Count; ++i)
                                {
                                    RunebookEntry entry = (RunebookEntry)book.Entries[i];

                                    if (entry.Map != null && (regionList[ix] as Region).Contains(entry.Location))
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
                    }

                    Logger.Finish();
                    e.Mobile.SendMessage("DONE search for runes.");

                    return;
                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    e.Mobile.SendMessage("Exception in [Retarget -- see console.");
                    System.Console.WriteLine("Exception in [Retarget: {0}", exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                    return;
                }
            }
            else
            {
                e.Mobile.SendMessage("Usage: [Retarget <-regionName|-type|-point|-RegionUID> <name|type|point|uid>");
            }
        }

        [Usage("FindRunes <-regionName|-type|-point|-RegionUID> <name|type|point|uid>")]
        [Description("Lists runes, runebooks, or moonstones to the specified region.")]
        public static void FindRunes_OnCommand(CommandEventArgs e)
        {
            try
            {
                ArrayList regionList = new ArrayList();
                if (HaveRegions(e, regionList))
                {
                    LogHelper Logger = new LogHelper("findrunes.log", e.Mobile, true);

                    foreach (Item item in World.Items.Values)
                    {
                        if (item is RecallRune)
                        {
                            RecallRune rune = (RecallRune)item;

                            for (int ix = 0; ix < regionList.Count; ix++)
                            {
                                if (rune.Marked && rune.TargetMap != null && (regionList[ix] as Region).Contains(rune.Target))
                                {
                                    object root = item.RootParent;

                                    if (root is Mobile)
                                    {
                                        if (((Mobile)root).AccessLevel < AccessLevel.GameMaster)
                                            Logger.Log(LogType.Item, item, rune.Description);

                                    }
                                    else
                                    {
                                        Logger.Log(LogType.Item, item, rune.Description);
                                    }
                                }
                            }
                        }
                        else if (item is Moonstone)
                        {
                            Moonstone stone = (Moonstone)item;

                            for (int ix = 0; ix < regionList.Count; ix++)
                            {
                                if (stone.Marked && (regionList[ix] as Region).Contains(stone.Destination))
                                {
                                    object root = item.RootParent;

                                    if (root is Mobile)
                                    {
                                        if (((Mobile)root).AccessLevel < AccessLevel.GameMaster)
                                            Logger.Log(LogType.Item, item, stone.Description);
                                    }
                                    else
                                    {
                                        Logger.Log(LogType.Item, item, stone.Description);
                                    }
                                }
                            }
                        }
                        else if (item is Runebook)
                        {
                            Runebook book = (Runebook)item;

                            for (int ix = 0; ix < regionList.Count; ix++)
                            {
                                for (int i = 0; i < book.Entries.Count; ++i)
                                {
                                    RunebookEntry entry = (RunebookEntry)book.Entries[i];

                                    if (entry.Map != null && (regionList[ix] as Region).Contains(entry.Location))
                                    {
                                        object root = item.RootParent;

                                        if (root is Mobile)
                                        {
                                            if (((Mobile)root).AccessLevel < AccessLevel.GameMaster)
                                                Logger.Log(LogType.Item, item, string.Format("{0}:{1}:{2}",
                                                    i,
                                                    entry.Description,
                                                    book.Description));
                                        }
                                        else
                                        {
                                            Logger.Log(LogType.Item, item, string.Format("{0}:{1}:{2}",
                                                    i,
                                                    entry.Description,
                                                    book.Description));

                                        }
                                    }
                                }
                            }
                        }
                    }

                    Logger.Finish();
                    e.Mobile.SendMessage("DONE search for runes.");

                    return;
                }
                else
                {
                    e.Mobile.SendMessage("Usage: [FindRunes <-regionName|-type|-point|-RegionUID> <name|type|point|uid>");
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                e.Mobile.SendMessage("Exception in [findrunes -- see console.");
                System.Console.WriteLine("Exception in [findrunes: {0}", exc.Message);
                System.Console.WriteLine(exc.StackTrace);
                return;
            }
        }

        private static bool HaveRegions(CommandEventArgs e, ArrayList regionList)
        {
            if (e.ArgString == null || e.ArgString.Length == 0)
                return false;

            string argString = e.ArgString.ToLower();

            if (argString.Contains("-regionname"))
            {
                string[] name = argString.Split(new string[] { "-regionname" }, StringSplitOptions.RemoveEmptyEntries);
                if (name == null || name.Length == 0)
                    return false;

                name[0] = name[0].Trim();
                for (int ix = 0; ix < Region.Regions.Count; ix++)
                {
                    if ((Region.Regions[ix] as Region).Name != null && (Region.Regions[ix] as Region).Name.ToLower() == name[0])
                        regionList.Add(Region.Regions[ix]);
                }

                if (regionList.Count == 0)
                {
                    e.Mobile.SendMessage("No regions with a name of \"{0}\" found.", name[0]);
                    return true;
                }

                return true;
            }
            else if (argString.Contains("-type"))
            {
                string[] name = argString.Split(new string[] { "-type" }, StringSplitOptions.RemoveEmptyEntries);
                if (name == null || name.Length == 0)
                    return false;

                name[0] = name[0].Trim();
                for (int ix = 0; ix < Region.Regions.Count; ix++)
                {
                    if ((Region.Regions[ix] as Region).GetType().ToString().ToLower().Contains(name[0]))
                        regionList.Add(Region.Regions[ix]);
                }

                if (regionList.Count == 0)
                {
                    e.Mobile.SendMessage("No regions with a type of \"{0}\" found.", name[0]);
                    return true;
                }

                return true;
            }
            else if (argString.Contains("-point"))
            {
                string[] name = argString.Split(new string[] { "-point" }, StringSplitOptions.RemoveEmptyEntries);
                if (name == null || name.Length == 0)
                    return false;

                name[0] = name[0].Trim();

                string[] xy = name[0].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (xy == null || xy.Length != 2)
                {
                    e.Mobile.SendMessage("Pooly formatted point: \"{0}\".", name[0]);
                    return false;
                }

                int x, y;
                if (int.TryParse(xy[0], out x) == false || int.TryParse(xy[1], out y) == false)
                {
                    e.Mobile.SendMessage("Pooly formatted point: \"{0}\".", name[0]);
                    return false;
                }

                Point3D px = new Point3D(x, y, 0);
                for (int ix = 0; ix < Region.Regions.Count; ix++)
                {
                    if ((Region.Regions[ix] as Region).Contains(px))
                        regionList.Add(Region.Regions[ix]);
                }

                if (regionList.Count == 0)
                {
                    e.Mobile.SendMessage("No regions containing the point \"{0}\" found.", name[0]);
                    return true;
                }

                return true;
            }
            else if (argString.Contains("-regionuid"))
            {
                string[] name = argString.Split(new string[] { "-regionuid" }, StringSplitOptions.RemoveEmptyEntries);
                if (name == null || name.Length == 0)
                    return false;

                name[0] = name[0].Trim();
                int uid;
                if (int.TryParse(name[0], out uid) == false)
                {
                    e.Mobile.SendMessage("Bad UID format: \"{0}\".", name[0]);
                    return false;
                }
                for (int ix = 0; ix < Region.Regions.Count; ix++)
                {
                    if ((Region.Regions[ix] as Region).UId == uid)
                    {
                        regionList.Add(Region.Regions[ix]);
                        break;  // only one of these
                    }
                }

                if (regionList.Count == 0)
                {
                    e.Mobile.SendMessage("No regions with a UID of \"{0}\" found.", name[0]);
                    return true;
                }

                return true;
            }
            else
                return false;
        }

        /*
		public static bool IsInRegion( Point3D loc, string regionName )
		{
			Region reg = Region.GetByName( regionName, Map.Felucca );
			if( reg != null )
			{	
				if( reg.Contains(loc) )
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsBad( Point3D loc, Map map )
		{
			if ( loc.X < 0 || loc.Y < 0 || loc.X >= map.Width || loc.Y >= map.Height )
			{
				return true;
			}
			else if ( map == Map.Felucca || map == Map.Trammel )
			{
				if ( loc.X >= 5120 && loc.Y >= 0 && loc.X <= 6143 && loc.Y <= 2304 )
				{
					Region r = Region.Find( loc, map );

					// if ( !(r is FeluccaDungeon || r is TrammelDungeon) )
					if ( !(r is FeluccaDungeon) )
						return true;
				}
			}
			
			return false;
		}
		 */
    }
}

/*
			//OLD, default, no argument, just for green acres code below :-)

			foreach ( Item item in World.Items.Values )
			{
				if ( item is RecallRune )
				{
					RecallRune rune = (RecallRune)item;

					if ( rune.Marked && rune.TargetMap != null && IsBad( rune.Target, rune.TargetMap ) )
					{
						object root = item.RootParent;

						if ( root is Mobile )
						{
							if ( ((Mobile)root).AccessLevel < AccessLevel.GameMaster )
								e.Mobile.SendMessage( "Rune: '{4}' {0} [{1}]: {2} ({3})", item.GetWorldLocation(), item.Map, root.GetType().Name, ((Mobile)root).Name, rune.Description );
						}
						else
						{
							e.Mobile.SendMessage( "Rune: '{3}' {0} [{1}]: {2}", item.GetWorldLocation(), item.Map, root==null ? "(null)" : root.GetType().Name, rune.Description );
						}
					}
				}
				else if ( item is Runebook )
				{
					Runebook book = (Runebook)item;

					for ( int i = 0; i < book.Entries.Count; ++i )
					{
						RunebookEntry entry = (RunebookEntry)book.Entries[i];

						if ( entry.Map != null && IsBad( entry.Location, entry.Map ) )
						{
							object root = item.RootParent;

							if ( root is Mobile )
							{
								if ( ((Mobile)root).AccessLevel < AccessLevel.GameMaster )
									e.Mobile.SendMessage( "Runebook: '{6}' {0} [{1}]: {2} ({3}) ({4}:{5})", item.GetWorldLocation(), item.Map, root.GetType().Name, ((Mobile)root).Name, i, entry.Description, book.Description );
							}
							else
							{
								e.Mobile.SendMessage( "Runebook: '{5}' {0} [{1}]: {2} ({3}:{4})", item.GetWorldLocation(), item.Map, root==null ? "(null)" : root.GetType().Name, i, entry.Description, book.Description );
							}
						}
					}
				}
			}
*/