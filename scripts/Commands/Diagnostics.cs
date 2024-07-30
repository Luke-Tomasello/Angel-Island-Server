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

/* Scripts\Commands\Diagnostics.cs
 * ChangeLog
 *  3/31/2024, Adam (Push/Pop commands)
 *      Enhanced both Push and Pop commands to support all read/writable properties.
 *      Example: [Push Location, [Push Location, [Push Location, ...
 *      [Pop, [Pop, [Pop
 *  9/20/22, Adam (WorldLocation)
 *      Jump_Next now supports WorldLocation (contains the map)
 *  9/19/22. Adam (Jump_Previous)
 *      Add Jump_Previous to complement Jump_Next
 *      You can now move backward through your jumplist
 *  7/3/22, Adam 
 *      Update version string to show .NET 6
 *  1/19/22, (Jump_Next)
 *      Change map when jumping as needed
 *	10/18/10, Adam
 *		Made Next command AccessLevel.Counselor
 *	8/24/10, Adam
 *		Switch over from using the current hardware info the the most recent hardware info (hash)
 *		since the client doesn't send it every time, we will use the most recent.
 *	3/13/10, Adam
 *		Add Push/Pop to dynamically change access level
 *	3/10/10, Adam
 *		Add Jump List processing
 *  11/14/08, Adam
 *      In the Consumer Price Index (CPI) command, filter for dexxers - get highest weapon skill
 *	11/12/08, Adam
 *		Add Consumer Price Index (CPI) command.
 *		Also called from our Cron Scheduler
 *  1/12/08, Adam
 *      Add HIGH only filter (-ho)
 *  11/25/07, Adam
 *		Export LineOfSight() for use elsewhere
 *  11/15/07, Adam
 *      Fix order of precedence error by adding proper parentheses .. Doh!
 *  11/14/07, Adam
 *      Add more null checks to ProcessMatch(), include 'tracker' variable to locate exception.
 *  11/7/07, Adam
 *      Add more null checks to ProcessMatch()
 *	10/31/07, Adam
 *		More error checking to solve emailed exception in ProcessMatch
 *	9/14/07, Adam
 *		Add -hi switch to dump who has (valid) hardware info
 *		Remove hashtable node if there are no clients for that IP Address
 *	9/13/07, Adam
 *		Rename existing switches
 *		Add new multi-client switch -mc and have it rank the threat (LOW, MEDIUM, HIGH)
 *	9/12/07, Adam
 *		Add client monitoring core ClientMon
 *		Add some basic dumpping functions for the [ClientMon command
 *	8/11/07, Adam
 *		Add command [LookupTileInMCL to give the relative coords of a static tile within a house.
 *		This command is useful for determining the location of a bad or missing tile that needs replacement.
 *	8/2/07, Adam
 *		Add FindItemInArea command. This should be used with [area. Ex
 *		[area finditemInArea 7986
 *  3//26/07, Adam
 *      First time checkin
 *		Add 
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TextCopy;
using static Server.Item;

namespace Server.Commands
{

    public class Diagnostics
    {
        public static void Initialize()
        {
            TargetCommands.Register(new FindItemInArea());
            CommandSystem.Register("ShowMCLBounds", AccessLevel.GameMaster, new CommandEventHandler(ShowMCLBounds_OnCommand));
            CommandSystem.Register("FindItemInMCL", AccessLevel.GameMaster, new CommandEventHandler(FindItemInMCL_OnCommand));
            CommandSystem.Register("LookupTileInMCL", AccessLevel.GameMaster, new CommandEventHandler(LookupTileInMCL_OnCommand));
            CommandSystem.Register("StealthStaff", AccessLevel.Owner, new CommandEventHandler(StealthStaff_OnCommand));
            CommandSystem.Register("ClientMon", AccessLevel.Counselor, new CommandEventHandler(ClientMon_OnCommand));
            CommandSystem.Register("CPI", AccessLevel.Administrator, new CommandEventHandler(CPI_OnCommand));
            CommandSystem.Register("Next", AccessLevel.Counselor, new CommandEventHandler(Jump_Next));
            CommandSystem.Register("Prev", AccessLevel.Counselor, new CommandEventHandler(Jump_Previous));
            CommandSystem.Register("Sort", AccessLevel.Counselor, new CommandEventHandler(Jump_Sort));
            CommandSystem.Register("Push", AccessLevel.Seer, new CommandEventHandler(Push_Command));
            CommandSystem.Register("Pop", AccessLevel.Player, new CommandEventHandler(Pop_Command));
            CommandSystem.Register("ver", AccessLevel.Player, new CommandEventHandler(Version_OnCommand));

            EventSink.Login += new LoginEventHandler(EventSink_Login);
            EventSink.Logout += new LogoutEventHandler(EventSink_Logout);
            EventSink.Connected += new ConnectedEventHandler(EventSink_Connected);
            EventSink.Disconnected += new DisconnectedEventHandler(EventSink_Disconnected);
            EventSink.ShuttingDown += new ShuttingDownEventHandler(EventSink_ShuttingDown);
        }
        #region SHOW MCL RECT
        [Usage("ShowMCLBounds")]
        [Description("displays the bounding rect of an MCL.")]
        public static void ShowMCLBounds_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Target the house sign of the house which you wish to view.");
                e.Mobile.Target = new ShowMCLBoundsTarget();
            }
            catch (Exception err)
            {

                e.Mobile.SendMessage("Exception: " + err.Message);
            }
        }

        private class ShowMCLBoundsTarget : Target
        {
            public ShowMCLBoundsTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {

                int found = 0;
                int visited = 0;
                BaseHouse house = null;
                if (targ is HouseSign sign)
                {
                    house = sign.Structure;
                    if (house == null)
                        from.SendMessage(0x22, "That house sign does not point to a house");
                }
                if (targ is TentBackpack backpack)
                {
                    house = backpack.Structure;
                    if (house == null)
                        from.SendMessage(0x22, "That tent backpack does not point to a house");
                }

                if (house != null)
                {
                    MultiComponentList mcl = house.Components;
                    //get a copy of the location
                    Point3D location = new Point3D(house.Location.X, house.Location.Y, house.Location.Z);
                    //get the MCL rect
                    var rect = new Rectangle2D(house.Location.X + mcl.Min.X, house.Location.Y + mcl.Min.Y, mcl.Width, mcl.Height);
                    // copy the the clipboard
                    ClipboardService.SetText(rect.ToString());
                    // tell the caller
                    from.SendMessage("{0}", rect);
                    // flash it baby!
                    Server.Gumps.EditAreaGump.FlashArea(from, rect, from.Map);
                }
                else
                    from.SendMessage(0x22, "That is not a house sign or tent backpack");

                from.SendMessage(String.Format("{0} items visited, {1} items found.", visited, found));
            }
        }
        #endregion
        #region Version
        [Usage("Ver")]
        [Aliases("version")]
        [Description("Display Angel Island version info.")]
        public static void Version_OnCommand(CommandEventArgs args)
        {
            System.Reflection.Assembly m_Assembly = System.Reflection.Assembly.GetEntryAssembly();
            Version ver = m_Assembly.GetName().Version;
            string version = string.Format("{0}.{1} [Build {2}] [.NET 6, 64 bit]", Utility.BuildMajor(), Utility.BuildMinor(), Utility.BuildBuild());
            string text = string.Format("Angel Island core version {0}, launched March 2004.", version);
            args.Mobile.SendMessage(0x35, text);
        }
        #endregion Version
        #region Push/Pop
        private static Dictionary<Mobile, Stack<KeyValuePair<string, object>>> PropertyStack = new();
        public static bool PropertyStackContains(Mobile m, string prop)
        {
            if (PropertyStack.ContainsKey(m))
                foreach (var kvp in PropertyStack[m])
                    if (kvp.Key.Equals(prop, StringComparison.OrdinalIgnoreCase))
                        return true;
            return false;
        }
        public static void Push_Command(CommandEventArgs e)
        {
            Push(e.Mobile as PlayerMobile, e.GetString(0), out string reason);
            e.Mobile.SendMessage(reason);
        }
        public static bool Push(PlayerMobile pm, string prop, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (pm == null) return false;
                try
                {
                    object o = null;
                    PropertyInfo[] props = Utility.MobileRWProperties(pm);
                    for (int i = 0; i < props.Length; i++)
                    {
                        try
                        {
                            if (props[i].Name.Equals(prop, StringComparison.OrdinalIgnoreCase))
                            {
                                o = props[i].GetValue(pm, null);
                                break;
                            }
                        }
                        catch
                        {
                            reason = "Denied";
                        }
                    }

                    if (o == null)
                        throw new ArgumentException();

                    if (!PropertyStack.ContainsKey(pm))
                    {
                        Stack<KeyValuePair<string, object>> stack = new();
                        stack.Push(new KeyValuePair<string, object>(prop, o));
                        PropertyStack.Add(pm, stack);
                    }
                    else
                    {
                        if (PropertyStack[pm].Count > 128)
                        {
                            reason = "Stack is full.";
                            return false;
                        }

                        PropertyStack[pm].Push(new KeyValuePair<string, object>(prop, o));
                    }

                    reason = "Push ok.";
                }
                catch
                {
                    reason = "Usage: Push <property name>.";
                    return false;
                }
            }
            catch (Exception ex)
            {
                reason = "Push Exception.";
                LogHelper.LogException(ex);
                return false;
            }

            return true;
        }
        public static void Pop_Command(CommandEventArgs e)
        {
            Pop(e.Mobile as PlayerMobile, out string reason);
            e.Mobile.SendMessage(reason);
        }
        public static bool Pop(PlayerMobile pm, out string reason)
        {
            reason = string.Empty;
            try
            {
                if (pm == null)
                {
                    reason = "(null)";
                    return false;
                }
                if (!PropertyStack.ContainsKey(pm))
                {
                    reason = "Stack is empty.";
                    return false;
                }

                if (PropertyStack[pm].Count == 0)
                {
                    reason = "Stack is empty.";
                    return false;
                }
                else
                {
                    KeyValuePair<string, object> entry = PropertyStack[pm].Pop();
                    string property = entry.Key;
                    object o = entry.Value;
                    if (o != null)
                    {
                        PropertyInfo[] props = Utility.MobileRWProperties(pm);
                        for (int i = 0; i < props.Length; i++)
                        {
                            try
                            {
                                if (props[i].Name.Equals(property, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (property.Equals("AccessLevel", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // special internal property avoids elevation checks/exploits
                                        pm.AccessLevelInternal = (AccessLevel)o;
                                        break;
                                    }
                                    props[i].SetValue(pm, o, null);
                                    break;
                                }
                            }
                            catch
                            {
                                reason = string.Format("Pop failed for property {0}.", property);
                                return false;
                            }
                        }
                    }
                    else
                    {
                        reason = string.Format("Pop failed for property {0}.", property);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                reason = "Pop Exception.";
                LogHelper.LogException(ex);
                return false;
            }

            reason = "Pop ok.";
            return true;
        }
        public static void EventSink_ShuttingDown(ShuttingDownEventArgs e)
        {
            try
            {   // make sure staff don't save with a bunch of mods
                foreach (var kvp in PropertyStack)
                    while (kvp.Value.Count > 0)
                        Pop(kvp.Key as PlayerMobile, out string reason);

                World.Broadcast(0x35, true, "The server is shutting down...");
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
        #endregion Push/Pop
        public static void Jump_Next(CommandEventArgs e)
        {
            try
            {
                // sanity
                if (e == null || e.Mobile == null || e.Mobile is PlayerMobile == false)
                    return;

                int step = 0;
                if (!string.IsNullOrEmpty(e.ArgString))
                    int.TryParse(e.ArgString, out step);

                PlayerMobile pm = e.Mobile as PlayerMobile;

                if (pm.JumpIndex + step < 0)
                    pm.JumpIndex = 0;
                else
                    pm.JumpIndex += step;

                string region = "-null-";
                pm.LightLevel = 100;

                if (pm.JumpList != null && pm.JumpList.Count > 0)
                {
                    if (pm.JumpIndex < pm.JumpList.Count)
                    {
                        if (pm.JumpList[pm.JumpIndex] is Item)
                        {
                            if ((pm.JumpList[pm.JumpIndex] as Item).Map == Map.Internal)
                            {
                                e.Mobile.SendMessage("Item {0} is on the {1} map", (pm.JumpList[pm.JumpIndex] as Item), (pm.JumpList[pm.JumpIndex] as Item).Map);
                                pm.Location = (pm.JumpList[pm.JumpIndex] as Item).Location;
                                pm.JumpIndex++;
                            }
                            else
                            {
                                if ((pm.JumpList[pm.JumpIndex] as Item).RootParent != null)
                                {
                                    if ((pm.JumpList[pm.JumpIndex] as Item).RootParent is Item)
                                    {
                                        Item ix = (pm.JumpList[pm.JumpIndex] as Item).RootParent as Item;
                                        e.Mobile.Location = ix.Location;
                                        e.Mobile.Map = ix.Map;
                                    }
                                    else
                                    {
                                        Mobile mx = (pm.JumpList[pm.JumpIndex] as Item).RootParent as Mobile;
                                        e.Mobile.Location = mx.Location;
                                        e.Mobile.Map = mx.Map;
                                    }
                                }
                                else
                                {
                                    Item ix = (Item)pm.JumpList[pm.JumpIndex];
                                    e.Mobile.Location = ix.Location;
                                    e.Mobile.Map = ix.Map;
                                }
                                if (e.Mobile.Region != null && !string.IsNullOrEmpty(e.Mobile.Region.Name))
                                    region = e.Mobile.Region.Name;
                                if (pm.JumpList[pm.JumpIndex] is Item item && item.RootParent == null)
                                    e.Mobile.SendMessage(String.Format("there. ({0}/{1}) ({2}/{3}) : {4}", pm.JumpIndex, pm.JumpList.Count, region, pm.Map, item));
                                else
                                    e.Mobile.SendMessage(String.Format("there. ({0}/{1}) ({2}/{3}) : {4}", pm.JumpIndex, pm.JumpList.Count, region, pm.Map, ObjectAtLocation(e.Mobile, e.Mobile.Map, e.Mobile.Location)));
                                pm.JumpIndex++;
                            }
                        }
                        else if (pm.JumpList[pm.JumpIndex] is Mobile)
                        {
                            if ((pm.JumpList[pm.JumpIndex] as Mobile).Map == Map.Internal)
                            {
                                e.Mobile.SendMessage("Mobile {0} is on the {1} map", (pm.JumpList[pm.JumpIndex] as Mobile), (pm.JumpList[pm.JumpIndex] as Mobile).Map);
                                e.Mobile.Location = (pm.JumpList[pm.JumpIndex] as Mobile).Location;
                                pm.JumpIndex++;
                            }
                            else
                            {
                                Mobile ix = (Mobile)pm.JumpList[pm.JumpIndex];
                                e.Mobile.Location = ix.Location;
                                e.Mobile.Map = ix.Map;
                                if (e.Mobile.Region != null && !string.IsNullOrEmpty(e.Mobile.Region.Name))
                                    region = e.Mobile.Region.Name;
                                e.Mobile.SendMessage(String.Format("there. ({0}/{1}) ({2}/{3}) : {4}", pm.JumpIndex, pm.JumpList.Count, region, pm.Map, ObjectAtLocation(e.Mobile, e.Mobile.Map, e.Mobile.Location)));
                                pm.JumpIndex++;
                            }
                        }
                        else if (pm.JumpList[pm.JumpIndex] is Item.WorldLocation)
                        {
                            Point3D ix = new Point3D();
                            ix.X = ((WorldLocation)pm.JumpList[pm.JumpIndex]).X;
                            ix.Y = ((WorldLocation)pm.JumpList[pm.JumpIndex]).Y;
                            ix.Z = ((WorldLocation)pm.JumpList[pm.JumpIndex]).Z;

                            e.Mobile.Location = ix;
                            Map temp = Map.Maps[((WorldLocation)pm.JumpList[pm.JumpIndex]).M];
                            e.Mobile.Map = temp == Map.Internal ? e.Mobile.Map : temp;

                            if (e.Mobile.Region != null && !string.IsNullOrEmpty(e.Mobile.Region.Name)) region = e.Mobile.Region.Name;
                            e.Mobile.SendMessage(String.Format("there. ({0}/{1}) ({2}/{3}) : {4}", pm.JumpIndex, pm.JumpList.Count, region, pm.Map, ObjectAtLocation(e.Mobile, e.Mobile.Map, e.Mobile.Location)));
                            pm.JumpIndex++;
                        }
                        else if (pm.JumpList[pm.JumpIndex] is Point2D || pm.JumpList[pm.JumpIndex] is Point3D)
                        {
                            Point3D ix = new Point3D();
                            if (pm.JumpList[pm.JumpIndex] is Point2D)
                            {
                                ix.X = ((Point2D)pm.JumpList[pm.JumpIndex]).X;
                                ix.Y = ((Point2D)pm.JumpList[pm.JumpIndex]).Y;
                                ix.Z = pm.Map.GetAverageZ(ix.X, ix.Y);
                            }
                            else
                            {
                                ix.X = ((Point3D)pm.JumpList[pm.JumpIndex]).X;
                                ix.Y = ((Point3D)pm.JumpList[pm.JumpIndex]).Y;
                                ix.Z = ((Point3D)pm.JumpList[pm.JumpIndex]).Z;
                            }

                            e.Mobile.Location = ix;
                            if (e.Mobile.Region != null && !string.IsNullOrEmpty(e.Mobile.Region.Name))
                                region = e.Mobile.Region.Name;
                            e.Mobile.SendMessage(String.Format("there. ({0}/{1}) ({2}/{3}) : {4}", pm.JumpIndex, pm.JumpList.Count, region, pm.Map, ObjectAtLocation(e.Mobile, e.Mobile.Map, e.Mobile.Location)));
                            pm.JumpIndex++;
                        }
                        else pm.JumpIndex++;
                    }
                    else
                    {
                        e.Mobile.SendMessage("You are at the end of your jump list, returning to top.");
                        pm.JumpIndex = 0;
                    }
                }
                else
                {
                    e.Mobile.SendMessage("Your jump list is empty.");
                    Utility.Beep(e.Mobile, 0x03E);
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Jump List Exception.");
                LogHelper.LogException(ex);
            }
        }
        private static object ObjectAtLocation(Mobile m, Map map, Point3D px)
        {
            if (map != null)
                foreach (object o in map.GetItemsInRange(px, 0))
                    if (o is Mobile mob && mob == m)
                        continue;
                    else if (o is Item item && item.Deleted == false)
                    {
                        if (item.Location.X == px.X && item.Location.Y == px.Y)
                            return item;

                    }
                    else if (o is Mobile mob2 && mob2.Deleted == false)
                    {
                        if (mob2.Location.X == px.X && mob2.Location.Y == px.Y)
                            return mob2;
                    }
            return null;
        }
        [Usage("Prev")]
        [Description("Jump to the previous location in the jump list.")]
        public static void Jump_Previous(CommandEventArgs e)
        {
            try
            {
                // sanity
                if (e == null || e.Mobile == null || e.Mobile is PlayerMobile == false)
                    return;

                PlayerMobile pm = e.Mobile as PlayerMobile;

                if (pm.JumpList != null && pm.JumpList.Count > 0)
                {
                    if (pm.JumpIndex > 0)
                    {
                        pm.JumpIndex -= 2;
                        if (pm.JumpIndex < 0)
                            pm.JumpIndex = 0;
                        Jump_Next(e);
                    }
                    else
                    {
                        e.Mobile.SendMessage("You are at the beginning of your jump list, returning to top.");
                        pm.JumpIndex = 0;
                    }
                }
                else
                {
                    e.Mobile.SendMessage("Your jump list is empty.");
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Jump List Exception.");
                LogHelper.LogException(ex);
            }
        }

        [Usage("Sort -serial(default)| -creation -ascending(default) -descending")]
        [Description("Sort the jump list.")]
        public static void Jump_Sort(CommandEventArgs e)
        {
            try
            {
                // sanity
                if (e == null || e.Mobile == null || e.Mobile is PlayerMobile == false)
                    return;

                bool created = e.ArgString.Contains("created", StringComparison.OrdinalIgnoreCase);
                bool descending = e.ArgString.Contains("descending", StringComparison.OrdinalIgnoreCase);

                if (e.ArgString.Contains("help", StringComparison.OrdinalIgnoreCase))
                {
                    e.Mobile.SendMessage("Usage: [sort <-serial|-created> <-ascending|-descending>");
                    e.Mobile.SendMessage("Default: -serial -ascending");
                    return;
                }

                PlayerMobile pm = e.Mobile as PlayerMobile;

                if (pm.JumpList != null && pm.JumpList.Count > 0)
                {
                    // move all IEntities to their own list for ordering
                    List<IEntity> entities = new List<IEntity>();
                    for (int i = pm.JumpList.Count - 1; i >= 0; i--)
                    {
                        if (pm.JumpList[i] is IEntity)
                        {
                            entities.Add(pm.JumpList[i] as IEntity);
                            pm.JumpList.RemoveAt(i);
                        }
                    }

                    // now sort the list we know how to sort.
                    // Ascending means smallest to largest, 0 to 9, and / or A to Z and Descending means largest to smallest, 9 to 0, and / or Z to A.
                    List<IEntity> sorted_entities;
                    if (created)
                    {
                        if (descending)
                            sorted_entities = entities.OrderByDescending(e => e.Created).ToList();
                        else
                            sorted_entities = entities.OrderBy(e => e.Created).ToList();
                    }
                    else
                    {   // serial
                        if (descending)
                            sorted_entities = entities.OrderByDescending(e => e.Serial).ToList();
                        else
                            sorted_entities = entities.OrderBy(e => e.Serial).ToList();
                    }

                    pm.JumpList.InsertRange(0, sorted_entities);
                    pm.JumpIndex = 0;   // restart at the top

                    e.Mobile.SendMessage($"Your jump list has been sorted and contains {pm.JumpList.Count} entries.");
                }
                else
                {
                    e.Mobile.SendMessage("Your jump list is empty.");
                }
            }
            catch (Exception ex)
            {
                e.Mobile.SendMessage("Jump List Exception.");
                LogHelper.LogException(ex);
            }
        }
        // StealthStaff_OnCommand
        [Usage("ClientMon")]
        [Description("Provide logged in client statistics.")]
        public static void ClientMon_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Arguments.Length == 0)
                {
                    e.Mobile.SendMessage("Usage: ClientMon -ui|-ma|-mc|-hi|-ho");
                    e.Mobile.SendMessage("Where: -ui ... Unique IP addresses");
                    e.Mobile.SendMessage("Where: -ma ... total multi accounts");
                    e.Mobile.SendMessage("Where: -mc ... total multi clients");
                    e.Mobile.SendMessage("Where: -hi ... clients that have hardware info");
                    e.Mobile.SendMessage("Where: -ho ... HIGH only. use with -mc");
                    return;
                }

                if (e.ArgString.Contains("-hi"))
                {
                    int clients = 0;
                    int hardwareInfo = 0;
                    int badHardwareInfo = 0;
                    int nullHardwareInfo = 0;
                    foreach (DictionaryEntry de in ClientMon.list)
                    {
                        ArrayList al = de.Value as ArrayList;

                        if (al == null)
                            continue;

                        foreach (Account acct in al)
                        {
                            if (acct == null)
                                continue;   // bogus

                            PlayerMobile pm = GetActiveCharacter(acct);
                            if (pm == null)
                                continue;   // no longer logged in?

                            NetState ns = pm.NetState;
                            if (ns == null || ns.Address == null || ns.Mobile == null)
                                continue;   // still logging in?

                            Server.Accounting.Account pmAccount = (Server.Accounting.Account)ns.Account;
                            HardwareInfo pmHWInfo = pmAccount.HardwareInfo;

                            clients++;

                            if (pmHWInfo == null)
                                nullHardwareInfo++;
                            else
                            {
                                if (pmHWInfo.CpuClockSpeed == 0 || pmHWInfo.OSMajor == 0)
                                    badHardwareInfo++;
                                else
                                    hardwareInfo++;
                            }

                        }
                    }

                    e.Mobile.SendMessage(String.Format("{0} total clients, {1} with hardware info", clients, hardwareInfo));
                    e.Mobile.SendMessage(String.Format("{0} with null hardware info, {1} with bad hardware info", nullHardwareInfo, badHardwareInfo));
                }

                if (e.ArgString.Contains("-mc"))
                {
                    bool highOnly = false;
                    if (e.ArgString.Contains("-ho"))
                        highOnly = true;

                    foreach (DictionaryEntry de in ClientMon.list)
                    {
                        ArrayList al = de.Value as ArrayList;

                        if (al == null)     // bogus
                            continue;

                        if (al.Count < 2)   // no more than 1 account for this IP
                            continue;

                        PlayerMobile pm1 = null;
                        PlayerMobile pm2 = null;
                        ArrayList seen = new ArrayList();
                        foreach (Account acct1 in al)
                        {
                            if (acct1 == null)
                                continue;   // bogus

                            if (!TestCenter.Enabled && acct1.GetAccessLevel() > AccessLevel.Player)
                                continue;   // ignore staff

                            pm1 = GetActiveCharacter(acct1);
                            if (pm1 == null)
                                continue;   // logged out maybe?

                            seen.Add(acct1);

                            foreach (Account acct2 in al)
                            {
                                if (acct2 == null)
                                    continue;   // bogus

                                if (seen.Contains(acct2))
                                    continue;   // already processed

                                if (!TestCenter.Enabled && acct2.GetAccessLevel() > AccessLevel.Player)
                                    continue;   // ignore staff

                                pm2 = GetActiveCharacter(acct2);
                                if (pm2 == null)
                                    continue;   // logged out maybe?

                                // okay check the hardware and report
                                ProcessMatch(e.Mobile, highOnly, pm1, pm2);
                            }
                        }
                    }
                }

                if (e.ArgString.Contains("-ma"))
                {
                    int clients = 0;
                    int ipaddresses = 0;
                    foreach (DictionaryEntry de in ClientMon.list)
                    {
                        ArrayList al = de.Value as ArrayList;

                        if (al == null)
                            continue;

                        if (al.Count > 1)
                        {
                            clients += al.Count;
                            ipaddresses++;
                        }
                    }

                    if (clients == 0)
                        e.Mobile.SendMessage("There are no shared IP addresses");
                    else
                        e.Mobile.SendMessage(String.Format("There {2} {0} client{3} sharing {1} IP address{4}",
                            clients, ipaddresses,
                            clients == 1 ? "is" : "are",
                            clients == 1 ? "" : "s",
                            ipaddresses == 1 ? "" : "es"
                            ));
                }

                if (e.ArgString.Contains("-ui"))
                {
                    e.Mobile.SendMessage(String.Format("There {1} {0} unique IP address{2}",
                        ClientMon.list.Count,
                        ClientMon.list.Count == 1 ? "is" : "are",
                        ClientMon.list.Count == 1 ? "" : "es"
                        ));
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }

            e.Mobile.SendMessage("done.");
        }

        enum alert { LOW, MEDIUM, HIGH };
        private static void ProcessMatch(Mobile from, bool highOnly, PlayerMobile pm1, PlayerMobile pm2)
        {
            try
            {   // sanity
                if (from == null || pm1 == null || pm2 == null)
                    return; // wtf

                NetState ns1 = pm1.NetState;
                NetState ns2 = pm2.NetState;

                if (ns1 == null || ns2 == null)
                    return; // logged out / disconnected

                if (ns1.Address == null || ns1.Mobile == null || ns2.Address == null || ns2.Mobile == null)
                    return; // still logging in?

                if (ns1.Account == null || ns2.Account == null)
                    return; // error state .. ignore this account

                if (ns1.Account as Server.Accounting.Account == null || ns2.Account as Server.Accounting.Account == null)
                    return; // error state .. ignore this account

                Server.Accounting.Account pm1Account = (Server.Accounting.Account)ns1.Account;
                int pm1HWInfo = pm1Account.HardwareHash;    // most recent hardware info
                string pm1Name = string.Format("{0}/{1}", pm1Account.Username, ns1.Mobile.Name);

                Server.Accounting.Account pm2Account = (Server.Accounting.Account)ns2.Account;
                int pm2HWInfo = pm2Account.HardwareHash;    // most recent hardware info
                string pm2Name = string.Format("{0}/{1}", pm2Account.Username, ns2.Mobile.Name);

                alert alarm = alert.LOW;
                if (pm1HWInfo == 0 && pm2HWInfo == 0 && ns1.Version == ns2.Version)
                {
                    // unknown hardware, same client version
                    if (LineOfSight(pm1, pm2))
                        alarm = alert.MEDIUM;
                }
                else if (pm1HWInfo == 0 || pm2HWInfo == 0 && ns1.Version == ns2.Version)
                {
                    // unknown hardware, same client version
                    if (LineOfSight(pm1, pm2))
                        alarm = alert.MEDIUM;
                }
                // we don't care about 'bad hardware' here as long as it matches!
                /*else if ((pm1HWInfo != null && pm2HWInfo != null) && (pm1HWInfo.CpuClockSpeed == 0 || pm1HWInfo.OSMajor == 0 || pm2HWInfo.CpuClockSpeed == 0 || pm2HWInfo.OSMajor == 0))
				{
					// unknown hardware
					if (LineOfSight(pm1, pm2))
						alarm = alert.MEDIUM;
				}*/
                else if ((pm1HWInfo != 0 && pm2HWInfo != 0) && pm1HWInfo == pm2HWInfo /*Server.Commands.MultiClientCommand.IsSameHWInfo(pm1HWInfo, pm2HWInfo)*/)
                {
                    // same hardware
                    alarm = alert.MEDIUM;
                    if (LineOfSight(pm1, pm2) && ns1.Version == ns2.Version)
                        alarm = alert.HIGH;
                }
                else
                {
                    // different hardware
                    if (LineOfSight(pm1, pm2) && ns1.Version == ns2.Version)
                        alarm = alert.MEDIUM;
                }

                // caller wants to filter to HIGH alarms only
                if (highOnly == true && alarm != alert.HIGH)
                    return;

                from.SendMessage(String.Format("{0}, {1}, Alarm({2})", pm1Name, pm2Name, alarm.ToString()));
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        public static bool LineOfSight(Mobile from, Mobile to)
        {
            Point3D eyeFrom = from.Location;
            Point3D eyeTo = to.Location;

            eyeFrom.Z += 14;
            eyeTo.Z += 14;

            return Utility.LineOfSight(from.Map, eyeFrom, eyeTo, false) || Utility.LineOfSight(from.Map, eyeTo, eyeFrom, false);
        }

        private static PlayerMobile GetActiveCharacter(Account acct)
        {
            for (int i = 0; i < 5; ++i)
            {
                if (acct[i] == null)
                    continue;

                if (acct[i].NetState != null)
                    return acct[i] as PlayerMobile;
            }

            return null;
        }

        /* Global client monitor
		 * This monitor should be kept simple and generic. Any specific client tests should be written
		 * outside of this class and simply use this class as the core.
		 * Notes: All Logins also result in a Connect, and all Logouts also result in a Disconnect, but...
		 * A client disconnect does NOT result in a logout. The current structure of this list manager
		 * takes this into account.
		 */
        static public class ClientMon
        {
            static Hashtable m_list = new Hashtable();
            static public Hashtable list { get { return m_list; } }

            static public void AddAccount(Mobile m)
            {
                NetState state = m.NetState;
                int iphc = state.ToString().GetHashCode();          // hash code of IP address
                if (m_list.Contains(iphc))                          // if we have it already
                {
                    ArrayList al = m_list[iphc] as ArrayList;       // get the list of clients at this IP address
                    if (al.Contains(m.Account) == false)
                        al.Add(m.Account);                          // add another client at this IP
                }
                else
                {
                    m_list[iphc] = new ArrayList();                 // start a new list of clients at this IP address
                    ArrayList al = m_list[iphc] as ArrayList;       // get the list of clients at this IP address
                    al.Add(m.Account);                              // add another client at this IP
                }
            }

            static public void RemoveAccount(Mobile m)
            {
                foreach (DictionaryEntry de in list)
                {
                    ArrayList al = de.Value as ArrayList;

                    if (al == null)
                        continue;

                    // find account for this player 
                    foreach (Account acct in al)
                    {
                        if (acct == null)
                            continue;

                        for (int i = 0; i < 5; ++i)
                        {
                            if (acct[i] == null)
                                continue;

                            if (acct[i].Serial == m.Serial)
                            {
                                if (al.Contains(acct))
                                    al.Remove(acct);            // remove the client from this IP

                                if (al.Count == 0)
                                    if (list.Contains(de.Key))
                                        list.Remove(de.Key);        // remove this IP if there are no more clients

                                return;
                            }
                        }
                    }
                }
            }
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            if (e.Mobile != null && e.Mobile.Account != null && e.Mobile.NetState != null)
                try { ClientMon.AddAccount(e.Mobile); }
                catch (Exception ex) { LogHelper.LogException(ex); }
        }

        private static void EventSink_Logout(LogoutEventArgs e)
        {
            if (e.Mobile != null && e.Mobile.Account != null)
                try { ClientMon.RemoveAccount(e.Mobile); }
                catch (Exception ex) { LogHelper.LogException(ex); }
        }

        private static void EventSink_Connected(ConnectedEventArgs e)
        {
            if (e.Mobile != null && e.Mobile.Account != null && e.Mobile.NetState != null)
                try { ClientMon.AddAccount(e.Mobile); }
                catch (Exception ex) { LogHelper.LogException(ex); }
        }

        private static void EventSink_Disconnected(DisconnectedEventArgs e)
        {
            if (e.Mobile != null && e.Mobile.Account != null)
                try { ClientMon.RemoveAccount(e.Mobile); }
                catch (Exception ex) { LogHelper.LogException(ex); }
        }


        // StealthStaff_OnCommand
        [Usage("StealthStaff")]
        [Description("Finds all accounts with staff chars where the account is not marked as staff.")]
        public static void StealthStaff_OnCommand(CommandEventArgs e)
        {
            int found = 0;
            try
            {
                foreach (Account acct in Accounts.Table.Values)
                {
                    // down patching staff accounts to Player access
                    if ((e.Arguments.Length == 1 && e.Arguments[0] == "-u") == false)
                        if (acct == null || acct.AccessLevel > AccessLevel.Player)
                            continue;

                    int count = 0;
                    AccessLevel high = AccessLevel.Player;
                    for (int i = 0; i < 5; ++i)
                    {
                        if (acct[i] == null)
                            continue;

                        if (acct[i].AccessLevel > AccessLevel.Player)
                        {
                            if (count == 0)
                                e.Mobile.SendMessage(String.Format("Account:{0}", acct.ToString()));

                            if (acct[i].AccessLevel > high)
                                high = acct[i].AccessLevel;
                            count++;
                            found++;
                        }
                    }

                    // we have at least one character with acces > then the account
                    if (count > 0)
                    {   // patch the account with the level of the highest ranking character
                        if (e.Arguments.Length == 1 && e.Arguments[0] == "-p")
                            acct.AccessLevel = high;

                        // patch the account with the level of Player
                        if (e.Arguments.Length == 1 && e.Arguments[0] == "-u")
                            if (high < AccessLevel.Owner)
                                acct.AccessLevel = AccessLevel.Player;
                    }
                }
            }
            catch (Exception err)
            {
                e.Mobile.SendMessage("Exception: " + err.Message);
            }

            if (e.Arguments.Length == 1 && e.Arguments[0] == "-p")
                e.Mobile.SendMessage(String.Format("{0} characters processed.", found));
            else
                e.Mobile.SendMessage(String.Format("{0} characters found.", found));
        }

        [Usage("CPI")]
        [Description("Calculates the Consumer Price Index for Magic Weapons.")]
        public static void CPI_OnCommand(CommandEventArgs e)
        {
            try
            {
                string s1, s2;
                CPI_Worker(out s1, out s2);
                e.Mobile.SendMessage(s1);
                e.Mobile.SendMessage(s2);
                e.Mobile.SendMessage("Done.");
            }
            catch (Exception err)
            {
                e.Mobile.SendMessage("Exception: " + err.Message);
            }
        }

        public static void CPI_Worker(out string s1, out string s2)
        {
            s1 = s2 = "no data";
            try
            {
                //////////////////////////////////////////////
                // Calculate the percentage of players carrying > force magic weapons

                double players = 0;
                double players_magic = 0;
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerMobile == false) continue;
                    Container backpack = m.Backpack;
                    if (backpack != null)
                    {   // get players account
                        Server.Accounting.Account ax = m.Account as Server.Accounting.Account;
                        if (ax == null)
                            continue;

                        // have they logged in within the last 90 days?
                        TimeSpan delta = DateTime.UtcNow - ax.LastLogin;
                        if (delta.TotalDays > 90)
                            continue;

                        // filter for dexxers - get highest weapon skill
                        double sk1 = Math.Max(m.Skills[SkillName.Archery].Base, m.Skills[SkillName.Fencing].Base);
                        double sk2 = Math.Max(m.Skills[SkillName.Macing].Base, m.Skills[SkillName.Swords].Base);
                        double skill = Math.Max(sk1, sk2);
                        if (skill < 100) continue;

                        // record all players that have a weapon on them
                        players++;
                        ArrayList stuff = backpack.FindAllItems();
                        Item oneHanded = m.FindItemOnLayer(Layer.OneHanded);
                        Item twoHanded = m.FindItemOnLayer(Layer.TwoHanded);
                        if (oneHanded is BaseWeapon && (oneHanded as BaseWeapon).DamageLevel > WeaponDamageLevel.Force)
                            stuff.Add(oneHanded);
                        if (twoHanded is BaseWeapon && (twoHanded as BaseWeapon).DamageLevel > WeaponDamageLevel.Force)
                            stuff.Add(twoHanded);

                        // record all the players that have a high end magic weapon on them
                        foreach (Item ix in stuff)
                            if (ix is BaseWeapon && (ix as BaseWeapon).DamageLevel > WeaponDamageLevel.Force)
                            {   // we only count one high end weapon per player
                                players_magic++;
                                break;
                            }
                    }
                }

                if (players > 0)
                    s1 = String.Format("Of {0} GM dexxers, {1}% are equipping Power or higher.", (int)players, (int)((players_magic / players) * 100));

                //////////////////////////////////////////////
                // Calculate the average cost of > force weapons

                int total_magic = 0;
                int total_value = 0;
                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is PlayerVendor)
                    {
                        PlayerVendor pv = m as PlayerVendor;
                        Hashtable tab = pv.SellItems;
                        foreach (DictionaryEntry de in tab)
                        {
                            if (de.Value is VendorItem)
                            {
                                if (de.Key is Item)
                                {
                                    Item ix = (de.Key as Item);
                                    if (ix is BaseWeapon && (ix as BaseWeapon).DamageLevel > WeaponDamageLevel.Force)
                                    {
                                        total_magic++;
                                        total_value += (de.Value as VendorItem).Price;
                                    }
                                }

                            }
                        }
                    }
                }

                if (total_magic > 0)
                    s2 = String.Format("Of {0} high end magic weapons, {1} is the average vendor cost.", total_magic, total_value / total_magic);

            }
            catch (Exception err)
            {
                s1 = "Exception: " + err.Message;
            }
        }

        public class FindItemInArea : BaseCommand
        {
            public FindItemInArea()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "FindItemInArea" };
                ObjectTypes = ObjectTypes.Items;

                Usage = "FindItemInArea";
                Description = "Locates an item of ItemID in the area";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                int found = 0;
                int visited = 0;
                try
                {
                    Item item = obj as Item;
                    if (e.Arguments.Length == 0)
                    {
                        e.Mobile.SendMessage("Usage: FindItemInArea ItemID");
                        return;
                    }

                    int ItemID = 0;
                    if (int.TryParse(e.Arguments[0], out ItemID) == false)
                    {
                        e.Mobile.SendMessage("Usage: FindItemInArea decimal-ItemID");
                        return;
                    }

                    visited++;
                    if (item.ItemID == ItemID)
                    {
                        found++;
                        AddResponse(String.Format("Found Item {0} at {1}", item.ItemID, item.Location));
                    }
                }
                catch (Exception exe)
                {
                    LogHelper.LogException(exe);
                    e.Mobile.SendMessage(exe.Message);
                }

                AddResponse(String.Format("{0} items visited, {1} items found.", visited, found));
            }
        }

        // LookupTileInMCL
        [Usage("FindItemInMCL <ItemID>")]
        [Description("Finds an item by graphic ID within an MCL.")]
        public static void FindItemInMCL_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (e.Arguments.Length == 0)
                {
                    e.Mobile.SendMessage("Usage: FindItemInMCL ItemID");
                    return;
                }

                int ItemID = 0;
                if (int.TryParse(e.Arguments[0], out ItemID) == false)
                {
                    e.Mobile.SendMessage("Usage: FindItemInMCL decimal-ItemID");
                    return;
                }

                e.Mobile.SendMessage("Target the house sign of the house which you wish to search.");
                e.Mobile.Target = new FindItemInMCLTarget(ItemID);
            }
            catch (Exception err)
            {

                e.Mobile.SendMessage("Exception: " + err.Message);
            }
        }

        private class FindItemInMCLTarget : Target
        {
            int m_ItemID = 0;

            public FindItemInMCLTarget(int ItemID)
                : base(15, false, TargetFlags.None)
            {
                m_ItemID = ItemID;
            }

            protected override void OnTarget(Mobile from, object targ)
            {

                int found = 0;
                int visited = 0;
                if (targ is HouseSign)
                {
                    HouseSign sign = (HouseSign)targ;
                    if (sign.Structure != null)
                    {
                        if (sign.Structure is BaseHouse)
                        {
                            //ok, we got what we want :)
                            BaseHouse house = sign.Structure as BaseHouse;

                            //get a copy of the location
                            Point3D location = new Point3D(house.Location.X, house.Location.Y, house.Location.Z);

                            //Now we need to iterate through the components and see if we have one if these items
                            for (int i = 0; i < house.Components.List.Length; i++)
                            {
                                if ((house.Components.List[i].m_ItemID & 0x3FFF) == (m_ItemID & 0x3FFF))
                                {
                                    Point3D itemloc = new Point3D(
                                        location.X + house.Components.List[i].m_OffsetX,
                                        location.Y + house.Components.List[i].m_OffsetY,
                                        location.Z + house.Components.List[i].m_OffsetZ
                                        );
                                    from.SendMessage(String.Format("Item found at {0}", itemloc));
                                    found++;
                                }
                                visited++;
                            }
                        }
                    }
                    else
                        from.SendMessage(0x22, "That house sign does not point to a house");
                }
                else
                    from.SendMessage(0x22, "That is not a house sign");

                from.SendMessage(String.Format("{0} items visited, {1} items found.", visited, found));

            }
        }

        [Usage("LookupTileInMCL <Target>")]
        [Description("Finds an item by graphic ID within an MCL.")]
        public static void LookupTileInMCL_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.SendMessage("Target the house tile which you wish to lookup.");
                e.Mobile.Target = new LookupTileInMCLTarget();
            }
            catch (Exception err)
            {

                e.Mobile.SendMessage("Exception: " + err.Message);
            }
        }

        private class LookupTileInMCLTarget : Target
        {
            public LookupTileInMCLTarget()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {

                //int found = 0;
                //int visited = 0;
                if (targ is StaticTarget)
                {
                    Item item = new Item((targ as StaticTarget).ItemID);
                    item.Location = (targ as StaticTarget).Location;
                    item.Map = from.Map;
                    BaseHouse house = BaseHouse.FindHouseAt(item);
                    item.Delete();
                    if (house != null)
                    {
                        //get a copy of the location
                        Point3D location = new Point3D(house.Location.X, house.Location.Y, house.Location.Z);

                        //Now we need to iterate through the components and see if we have one if these items
                        for (int i = 0; i < house.Components.List.Length; i++)
                        {
                            Point3D RealTileloc = new Point3D(
                                    location.X + house.Components.List[i].m_OffsetX,
                                    location.Y + house.Components.List[i].m_OffsetY,
                                    location.Z + house.Components.List[i].m_OffsetZ
                                    );

                            if ((house.Components.List[i].m_ItemID & 0x3FFF) == (item.ItemID & 0x3FFF) && RealTileloc == item.Location)
                            {
                                Point3D itemloc = new Point3D(
                                    house.Components.List[i].m_OffsetX,
                                    house.Components.List[i].m_OffsetY,
                                    house.Components.List[i].m_OffsetZ
                                    );
                                from.SendMessage(String.Format("Item {0} Location {1}", (item.ItemID & 0x3FFF), itemloc));
                                //found++;
                                break;
                            }
                            //visited++;
                        }
                    }
                    else
                        from.SendMessage(0x22, "That tile is does not in a house");
                }
                else
                    from.SendMessage(0x22, "That is not a house tile");

                //from.SendMessage(String.Format("{0} items visited, {1} items found.", visited, found));
                from.SendMessage("done.");

            }
        }
    }
}