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

/* Server/Utility.cs
 * ChangeLog:
 *  8/13/2024, Adam
 *      Add a notion of BuildInfoDir. This directory contains the "build.info" file
 *  2/8/2024, Adam
 *      CAUTION: CopyProperties
 *      CopyProperties does not work for all items.
 *      For example: effect controller (and probably others.)
 *      This is because Setting property value X, then setting property value Y may unset or change X.
 *      EffectController for instance: All of SourceItem, SourceMobile, and SourceNull operate on 
 *      IEntity m_Source. If SourceItem is set to some item, and CopyProperties correctly copies it to the 
 *      destination item, all is well. But when it tries to copy SourceMobile, SourceMobile returns null since
 *      m_Source is an Item and not a Mobile.
 *      When SourceMobile in the destination object gets set to null, so does SourceItem! funky.
 *      In this routine, I created a PatchProperty method that patches such ordering issues. But because I cannot know
 *      which property should take precedence, developers need to create the handler here. See #region Apply Patches.
 * 8/4/2023, Adam: 
 *      - double RandomMinMax(double minimum, double maximum)
 *      - TimeSpan RandomMinMax(TimeSpan min, TimeSpan max)
 *      - DateTime FutureTime (DateTime check, TimeSpan min, TimeSpan max)
 *      - TimeSpan DeltaTime(DateTime check, TimeSpan min, TimeSpan max)
 *  7/29/2023, Adam (GetPathArray (used by SmartTeleport in MageAI))
 *      I create a 'smart' version of a cached teleport table.
 *      1. Before assigning the task of teleporting, we first call CanTeleportToCombatant()
 *          This does three things:
 *          a. Determine if a path can be created to the target
 *          b. Cache the results of the 'path points' in MageAI.TeleportTable
 *          c. return true/false. If false, we don't bother teleporting (nonsense teleports of old.)
 *      2. If #1 above allows the teleport, the cached TeleportTable is consulted to get the 'best' teleport point.
 *          'best' is the furthest point from us that we still have LOS to.
 *  6/25/2023, Adam (Dupe)
 *      Unset and restore item's Parent before and after the dupe.
 *      Duping an item with the parent set results in side effects, including in container overload.
 *  6/18/2023, Adam (CopyPropertiesDerivedToBase())
 *      Add a new CopyProperties function that allows you to copy properties from a derived class to a base class.
 *      For instance, you can copy the common properties from an EventSpawner to a Spawner.
 *  4/20/23, Yoar
 *      Added GetDefaultLabel
 *  1/10/23, Adam (CanSpawnMobile)
 *      CanSpawnMobile now supports bool allowExotics. allowExotics goes beyond the usual landZ, landAvg, and landTop
 *          returned by GetAverageZ. Allow Exotics looks for a 'floor' or 'ceiling' below and above you which are also
 *          spawnable. This is a default parameter which defaults to 'false'
 *  1/4/23, Adam (TryParseTimeSpan())
 *      TimeSpan.TryParse returns funky values. Example:
 *      "1:20:30" correctly returns 1hr, 20m, and 30s
 *      "1200:20:30" incorrectly returns 1200ds, 0hrs, 20m, and 30s
 *      Our routine here returns what I believe are more consistent results.
 *      i.e., "1200:20:30" will return 50ds, 0hrs, 20m, and 30s
 *      Priority:
 *          Seconds, Minutes, Hours, Days, Milliseconds
 *  12/10/22, Adam (AccessLevelRemap())
 *      Enable AccessLevelRemap'ing for all shards including Debug and Test Center
 *      AccessLevelRemap() is called when a staff member tries to add a teleporter, moongate, or spawner.
 *      The remapping is to EventTeleporter, EventMoongate, or EventSpawner. This keeps the shard's core
 *      systems pure and doesn't ugly it up with a bunch of random and abandoned event objects.
 *      The Event objects must be explicitly be enabled and they will auto disable after the event.
 *  11/14/22, Adam (Inventory(Mobile m))
 *      Add a new inventory function to return a list of all items on the mobile.
 *      This function gets not only equipped items, but also a recursive list items from nested containers starting with the backpack.
 *  9/23/22, Adam
 *      Add a new 'World' class to handle basic world (map) size and 'location' checks.
 *  9/22/22, Adam (MaximumSpawnableRect)
 *      Have MaximumSpawnableRect use the average Z instead of the 'best' Z
 *  9/20/22, Adam (MaximumSpawnableRect/MakeHold)
 *      MaximumSpawnableRect: this beast is sort of a geometrical aberration in that it crosses the line between pure geometry and knowing whether a mobile can be spawned within.
 *          Grow the given rectangle in all directions in which a mobile may be spawned.
 *          Use caution: You should only call this in a closed area, otherwise your rectangle can grow quite large.
 *  8/23/22, Adam (IsTownRegion)
 *      Added IsTownRegion()
 *      Note: Region wasn't really the appropriate place for this since it explicitly checks for Regions.GuardedRegion
 *  8/22/22, Adam (SplitOnCase)
 *      Take a token string and insert spaces where capitalization changes.
 *      For instance, "AngelIsland" would be converted to "Angel Island"
 *      Useful for converting enums to text.
 *  8/17/22, Adam
 *      Move the GetSharedAccounts family of functions from AdminGump to here 
 *  8/16/22, Adam
 *      Add IsSpawnedItem()
 *      Add Item Dupe(object target)
 *  8/15/22, Adam (FileInfo())
 *      Add a nifty function FileInfo(). FileInfo() prints the FILE and LINE of the caller. Excellent for debug statements.
 *      Example: if (2+2 == 5) Utility.ConsoleOut(string.Format("Logic Error: {0}. ",Utility.FileInfo()), ConsoleColor.Red);
 *      Output: "Logic Error: in foo.c, line 2547"
 *  8/9/22, Adam
 *      Add public static T RandomEnumValue<T>() for getting a random enumeration value
 *  5/23/22, Adam (HasParameterlessConstructor)
 *      Add a function to test if an item has a Parameterless Constructor
 *  3/28/22, Adam (CalcRegionBounds)
 *      Calculate and return a list of points that define a regions bounds
 *  3/17/22, Yoar
 *      Removed the need for type arguments in Shuffle.
 *  1/27/22, Adam (GetStableHashCode)
 *      The C# runtime version of GetHashCode() generates a different hash everytime you run your program.
 *      The details are too lenghy to go into here, but if want to understand, here is an article
 *      https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
 *      In any case, the MusicBox Music system needs a deterministic hash, and so for this implementation
 *      I chose one from Stackoverflow: https://stackoverflow.com/questions/36845430/persistent-hashcode-for-strings/36845864#36845864
 *  12/26/21, Adam (GetObjectDisplayName)
 *      Wrote a routine to get an object's 'display name'
 *  12/22/21. Adam (GetShortPath())
 *      Added GetShortPath() function for clean display purposes.
 *	6/30/21, Adam
 *		move Levenshtein Distance Algorithm + helper functions to Intelligent Dialog class
 *  12/14/21, Adam (GetDistanceToSqrt)
 *      Add GetDistanceToSqrt functions that don't involve mobiles
 *  12/5/2021, Adam (BuildColor)
 *  public static ConsoleColor BuildColor(int bulidNumber)
 *      gives us a random color for *this* build.
 *      Allows a quick visual to ensure all servers are running the same build
 *  11/29/21, Adam
 *      Add ConsoleOut() methot that accepts a console foreground color.
 *      (restores the old color after the output)
 *  11/11/2021
 *      Added DayMonthCheck() and GetRootName()
 *  11/10/21, Adam (Shuffle)
 *      Added public static void Shuffle<T>(IList<T> list) so you can Shuffle a List<T>
 *  10/28/21, Yoar
 *      Added IntToFloatBits, FloatToIntBits: Cast an int to float and back while preserving the bits.
 *	6/30/21, Adam
 *		Added a Levenshtein Distance Algorithm + helper functions to lookup skill names.
 *		The Problem: Now with the ability to train skill by talking to the NPC, the NPC will tell you he 
 *			trains various skills. Often what they say (i.i., "maces") is not the name of the actual skill.
 *			our LDA helps us find a best match if the user says "train maces" our LDA translates that into 
 *			the correct format "train macing". There are many such cases.
 *			I also use this system in the test center 'set skill' for the same reasons.
 *	7/20/10, adam
 *		Add a 'Release' callback for the Memory system.
 *		Note: The Memory system is passive, that is it doesn't use any timers for cleaning up old objects, it
 *			simply cleans them up the next time it's called before processing the request. The one exception
 *			to this rule is when you specify a 'Release' callback to be notified when an object expires.
 *			In this case a timer is created to insure your callback is timely.
 *	7/11/10, adam
 *		Update our memoy system to support the notion of a memory context, i.e., what you remember about the thing
 *		For instance, if you are remembering the Mobile 'adam', you may want to remember the last know location in the context.
 *	6/21/10, adam
 *		Add a simple memory system for timed remembering of items and mobiles.
 *		See BaseAI.cs & Container.cs for an example.
 *	5/27/10, adam
 *		Refactor RandomChance() to take a double insead of an int so you can pass fractions like 10.4 would be a 10.4% chance
 * 3/21/10, adam
 *		Add function RescaleNumber to scale a number from an Old range to a new range. 
 *	5/11/08, Adam
 *		Added Profiler class
 *  3/25/08, Pix
 *		Added AdjustedDateTime class.
 *  5/8/07, Adam
 *      Add new data packing functions. e.g. Utility.GetUIntRight16()
 *  12/25/06, Adam
 *      Add an Elapsed() function to the TimeCheck class
 *  12/21/06, Adam
 *      Moved TimeCheck from Heartbeat.cs to Utility.cs
 *  12/19/06, Adam
 *      Add: GetHost(), IsHostPrivate(), IsHostPROD(), IsHostTC()
 *      These functions help to distinguish private servers from public.
 *      See: CrashGuard.cs
 *  3/28/06 Taran Kain
 *		Change IPAddress.Address (deprecated) references to use GetAddressBytes()
 *	3/18/06, Adam
 *		Move special dye tub colors here from the Harrower code
 * 		i.e., RandomSpecialHue()
 *	2/2/06, Adam
 *		Add DebugOut() functions.
 *		This functions only output if DEBUG is defined
 *	9/17/05, Adam
 *		add 'special' hues; i.e., RandomSpecialVioletHue() 
 *	7/26/05, Adam
 *		Massive AOS cleanout
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Spells;
using Server.Targeting;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static Server.Commands.Decorate;
using static Server.Items.DyeTubToning;
using static Server.Items.TownshipStone;
using CPA = Server.CommandPropertyAttribute;
namespace Server
{
    public class Utility
    {
        #region Z management
        public static bool BadZ(Mobile from)
        {   // am I maybe stuck in the floor?
            IPoint3D px = from.Location;
            Utility.GetSurfaceTop(from, ref px);
            return from.Location.Z != px.Z;
        }
        public static int GoodZ(Mobile from)
        {   // get the best Z
            IPoint3D px = from.Location;
            Utility.GetSurfaceTop(from, ref px);
            return px.Z;
        }
        public static bool CheatingZ(Mobile from)
        {   // if they are surrounded by movable objects, crates for example, assume a cheat
            IPooledEnumerable eable = from.Map.GetItemsInRange(from.Location, 1);
            foreach (Item item in eable)
                if (item != null && item.Deleted == false)
                    if (item.Movable && item.ItemData.Impassable && (item.Z + item.ItemData.Height) > from.Z && (from.Z + 16) > item.Z)
                        if (from.InRange(item.GetWorldLocation(), 1))
                        {
                            eable.Free();
                            return true;
                        }

            eable.Free();

            return false;
        }
        public static void GetSurfaceTop(Mobile from, ref IPoint3D p)
        {   // rise to the top of the stack. Usually GM created structures like stairs
            List<IPoint3D> list = new();
            IPooledEnumerable eable = from.Map.GetItemsInRange((Point3D)p, 0);
            foreach (Item thing in eable)
                if (thing != null && thing.Deleted == false)
                {
                    IPoint3D ipx = thing as IPoint3D;
                    SpellHelper.GetSurfaceTop(ref ipx);
                    list.Add(ipx);
                }

            eable.Free();

            if (list.Count > 0)
            {
                list.Sort((p, q) => q.Z.CompareTo(p.Z));
                p = list[0];
            }

            return;
        }
        #endregion Z management
        #region Shared Types
        [Flags]
        public enum SpawnFlags
        {
            None = 0,               // use spawner defaults
            SpawnFar = 0x01,        // spawn further out in our homerange
            Boat = 0x02,            // try to spawn on a boat
            AvoidPlayers = 0x04,    // try to avoid players - useful for champ spawns, Tax Collector, and Angry Miners, when we don't want players to see the spawn point
            ForceZ = 0x08,          // stay on this z
            ClearPath = 0x10,       // Expensive: must be a clear path back to the spawner (so we're not spawned in a wall)
            NoBlock = 0x20,         // Expensive: must not be blocked in at least one direction for 10? tiles
            Concentric = 0x40,      // uses our concentric algo even distribution
            AllowChangeZ = 0x80,     // unusual. allows spawns above/below the spawner (roof for instance)
        }
        public enum MorphMode : byte
        {
            Default,
            Melee,
        }
        #endregion Shared Types
        #region String Helpers
        public static bool StringToInt(string s, ref int result)
        {
            if (!string.IsNullOrEmpty(s))
            {
                try
                {
                    bool hex = s.Any(x => char.IsLetter(x));
                    s = s.Replace("0x", "", StringComparison.OrdinalIgnoreCase);
                    if (hex)
                        result = Convert.ToInt32(s, 16);
                    else
                        result = Convert.ToInt32(s);

                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
        #endregion String Helpers
        #region Logging
        public static void SimpleLog(string logFileName, Mobile sendMessage, string text)
        {
            if (sendMessage != null && text != null)
                sendMessage.SendMessage(text);
            SimpleLog(logFileName, text);
        }
        public static void SimpleLog(string logFileName, string text, bool overwrite = false)
        {
            LogHelper logger = new LogHelper(logFileName, overwrite, sline: true);
            if (text != null)
                logger.Log(text);
            logger.Finish();
        }
        #endregion Logging

        #region Breeding Tools
        private static Dictionary<Type, string> BCDefaultNameCache = new();
        public static void BreedingLog(BaseCreature me, BaseCreature you, string format, params object[] args)
        {
            if (me != null && me.DebugMode != DebugFlags.None || you != null && you.DebugMode != DebugFlags.None)
            {   // How I feel about YOU
                string text = string.Format($"{DynName(me)} => {DynName(you)}: {string.Format(format, args)}");
                LogHelper logger = new LogHelper("Courtship.log", overwrite: false, sline: true);
                logger.Log(text);
                logger.Finish();
            }
        }
        public static string DynName(BaseCreature bc)
        {
            string name = "(no participant)";
            if (bc == null || bc.ControlMaster == null)
                return name;

            if (BCDefaultNameCache.ContainsKey(bc.GetType()))
            {
                if (bc.Name == BCDefaultNameCache[bc.GetType()])
                    // Adam's Dragon
                    return bc.ControlMaster.Name + (bc.ControlMaster.Name.EndsWith("s") ? '\'' : "'s") + " " + bc.GetType().Name;
                else
                    // Viserion 
                    return bc.Name;
            }
            else
            {
                BaseCreature temp = (BaseCreature)Activator.CreateInstance(bc.GetType());
                if (temp is BaseCreature bc_temp)
                {
                    BCDefaultNameCache[bc.GetType()] = bc_temp.Name;
                    bc_temp.Delete();
                    return DynName(bc);
                }

                return name;
            }
        }
        #endregion Breeding Tools
        #region Text Helpers/Macros
        public static string ExpandMacros(Mobile from, Item item, string text)
        {   // support: {guild_short} {guild_long} {title} {name} {item.name}
            if (from == null || text == null)
                return null;
            string new_text = text;
            if (item != null)
                new_text = new_text.Replace("{item.name}", item.Name != null ? item.Name : item.OldSchoolName(), StringComparison.OrdinalIgnoreCase).Trim();
            new_text = new_text.Replace("{name}", from.Name, StringComparison.OrdinalIgnoreCase).Trim();
            string title = Titles.ComputeTitle(from, from).Trim();
            title = title.Replace(from.Name + ",", "", StringComparison.OrdinalIgnoreCase).Trim();
            new_text = new_text.Replace("{title}", title, StringComparison.OrdinalIgnoreCase).Trim();
            if (from is PlayerMobile pm && pm.Guild != null)
            {
                Guilds.Guild guild = pm.Guild as Guilds.Guild;
                if (!string.IsNullOrEmpty(guild.Abbreviation))
                    new_text = new_text.Replace("{guild_short}", guild.Abbreviation, StringComparison.OrdinalIgnoreCase).Trim();
                else
                    new_text = new_text.Replace("{guild_short}", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (!string.IsNullOrEmpty(guild.Name))
                    new_text = new_text.Replace("{guild_long}", guild.Name, StringComparison.OrdinalIgnoreCase).Trim();
                else
                    new_text = new_text.Replace("{guild_long}", "", StringComparison.OrdinalIgnoreCase).Trim();
            }

            return new_text.Trim();
        }
        #endregion Text Helpers/Macros
        #region Convert Event Objects to Permanent
        public static bool IsEventObject(Item item)
        {
            return event_object_types.Contains(item.GetType());
        }
        private static List<Type> restricted_object_types = new() { typeof(Teleporter), typeof(Sungate), typeof(Spawner), typeof(KeywordTeleporter), typeof(ConfirmationSungate) };
        public static bool IsRestrictedObject(Item item)
        {
            return restricted_object_types.Contains(item.GetType());
        }
        private static List<Type> event_object_types = new() { typeof(FishController), typeof(EventTeleporter), typeof(EventSungate), typeof(EventSpawner), typeof(EventKeywordTeleporter), typeof(EventConfirmationSungate) };
        // Make event object permanent
        public static bool Meop(Mobile from, object obj, bool activate, out string reason)
        {
            reason = string.Empty;
            LogHelper logger = new LogHelper("meop.log", overwrite: false, sline: true);
            try
            {
                if (obj is Item item && event_object_types.Contains(item.GetType()))
                {
                    if (item is FishController fc)
                    {
                        if (activate)
                        {
                            fc.Event.DurationOverride = true;
                            fc.Event.Countdown = TimeSpan.FromSeconds(30);
                            fc.Event.EventEnd = (DateTime.MaxValue - TimeSpan.FromDays(365)).ToString();
                            reason = string.Format("{0} activating in {1} seconds", fc.GetType().Name, string.Format("{0:N2}", fc.Event.Countdown.TotalSeconds));
                            logger.Log(LogType.Mobile, from, string.Format("Making {0} Permanent.", item));
                        }
                        else
                        {
                            fc.Event.DurationOverride = false;
                            fc.Event.EventStart = DateTime.MinValue.ToString();
                            fc.Event.EventEnd = DateTime.MinValue.ToString();
                            fc.Event.Duration = TimeSpan.FromSeconds(0);
                            fc.Event.Countdown = TimeSpan.FromSeconds(0);
                            reason = "deactivating...";
                        }
                        return true;
                    }
                    else if (item is EventTeleporter et)
                    {
                        if (activate)
                        {
                            et.DurationOverride = true;
                            et.DestinationOverride = true;
                            et.Countdown = TimeSpan.FromSeconds(30);
                            et.EventEnd = (DateTime.MaxValue - TimeSpan.FromDays(365)).ToString();
                            reason = string.Format("{0} activating in {1} seconds", et.GetType().Name, string.Format("{0:N2}", et.Countdown.TotalSeconds));
                            logger.Log(LogType.Mobile, from, string.Format("Making {0} Permanent.", item));
                        }
                        else
                        {
                            et.DurationOverride = false;
                            et.DestinationOverride = false;
                            et.EventStart = DateTime.MinValue.ToString();
                            et.EventEnd = DateTime.MinValue.ToString();
                            et.Duration = TimeSpan.FromSeconds(0);
                            et.Countdown = TimeSpan.FromSeconds(0);
                            reason = "deactivating...";
                        }
                        return true;
                    }
                    else if (item is EventSungate es)
                    {
                        if (activate)
                        {
                            es.DurationOverride = true;
                            es.DestinationOverride = true;
                            es.Countdown = TimeSpan.FromSeconds(30);
                            es.EventEnd = (DateTime.MaxValue - TimeSpan.FromDays(365)).ToString();
                            reason = string.Format("{0} activating in {1} seconds", es.GetType().Name, string.Format("{0:N2}", es.Countdown.TotalSeconds));
                            logger.Log(LogType.Mobile, from, string.Format("Making {0} Permanent.", item));
                        }
                        else
                        {
                            es.DurationOverride = false;
                            es.DestinationOverride = false;
                            es.EventStart = DateTime.MinValue.ToString();
                            es.EventEnd = DateTime.MinValue.ToString();
                            es.Duration = TimeSpan.FromSeconds(0);
                            es.Countdown = TimeSpan.FromSeconds(0);
                            reason = "deactivating...";
                        }
                        return true;
                    }
                    else if (item is EventSpawner esp)
                    {
                        if (activate)
                        {
                            esp.DurationOverride = true;
                            esp.Countdown = TimeSpan.FromSeconds(30);
                            esp.EventEnd = (DateTime.MaxValue - TimeSpan.FromDays(365)).ToString();
                            reason = string.Format("{0} activating in {1} seconds", esp.GetType().Name, string.Format("{0:N2}", esp.Countdown.TotalSeconds));
                            logger.Log(LogType.Mobile, from, string.Format("Making {0} Permanent.", item));
                        }
                        else
                        {
                            esp.DurationOverride = false;
                            esp.EventStart = DateTime.MinValue.ToString();
                            esp.EventEnd = DateTime.MinValue.ToString();
                            esp.Duration = TimeSpan.FromSeconds(0);
                            esp.Countdown = TimeSpan.FromSeconds(0);
                            reason = "deactivating...";
                        }
                        return true;
                    }
                    else if (item is EventKeywordTeleporter ekt)
                    {
                        if (activate)
                        {
                            ekt.DurationOverride = true;
                            ekt.DestinationOverride = true;
                            ekt.Countdown = TimeSpan.FromSeconds(30);
                            ekt.EventEnd = (DateTime.MaxValue - TimeSpan.FromDays(365)).ToString();
                            reason = string.Format("{0} activating in {1} seconds", ekt.GetType().Name, string.Format("{0:N2}", ekt.Countdown.TotalSeconds));
                            logger.Log(LogType.Mobile, from, string.Format("Making {0} Permanent.", item));
                        }
                        else
                        {
                            ekt.DurationOverride = false;
                            ekt.DestinationOverride = false;
                            ekt.EventStart = DateTime.MinValue.ToString();
                            ekt.EventEnd = DateTime.MinValue.ToString();
                            ekt.Duration = TimeSpan.FromSeconds(0);
                            ekt.Countdown = TimeSpan.FromSeconds(0);
                            reason = "deactivating...";
                        }
                        return true;
                    }
                    else if (item is EventConfirmationSungate ecs)
                    {
                        if (activate)
                        {
                            ecs.DurationOverride = true;
                            ecs.DestinationOverride = true;
                            ecs.Countdown = TimeSpan.FromSeconds(30);
                            ecs.EventEnd = (DateTime.MaxValue - TimeSpan.FromDays(365)).ToString();
                            reason = string.Format("{0} activating in {1} seconds", ecs.GetType().Name, string.Format("{0:N2}", ecs.Countdown.TotalSeconds));
                            logger.Log(LogType.Mobile, from, string.Format("Making {0} Permanent.", item));
                        }
                        else
                        {
                            ecs.DurationOverride = false;
                            ecs.DestinationOverride = false;
                            ecs.EventStart = DateTime.MinValue.ToString();
                            ecs.EventEnd = DateTime.MinValue.ToString();
                            ecs.Duration = TimeSpan.FromSeconds(0);
                            ecs.Countdown = TimeSpan.FromSeconds(0);
                            reason = "deactivating...";
                        }
                        return true;
                    }
                }
                else
                {
                    reason = "That is not an Event object";
                    return false;
                }
            }
            catch { }
            finally
            {
                logger.Finish();
            }

            return false;
        }
        #endregion
        #region UOMusic
        public static Dictionary<Mobile, MusicName> SoundCanvas = new();
        public static bool IsUOMusic(string enum_name)
        {
            foreach (string name in Enum.GetNames(typeof(MusicName)))
                if (enum_name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
        public static string GetUOMusicName(string enum_name)
        {
            foreach (string name in Enum.GetNames(typeof(MusicName)))
                if (enum_name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return name;

            return null;
        }
        #region Data
        // This table includes the music information gleaned from CUO plus the 'duration' information I compiled from AI/Siege's music files.
        // CUO and AI somewhat disagree on ordering, and looping, so we will load AI's music config file and do our best to correlate the two.
        //  See Also: LoadUOMusicInfo() in this file where we populate this table
        // name --- total miliseconds --- loop
        public static Dictionary<int, (double, bool)> UOMusicInfo = new()
        {
            { 0, ( 49319.1609, true) },		    /*OLDULT01*/
            { 1, ( 64888.1405, false) },		/*CREATE1*/
            { 2, ( 48979.5691, false) },		/*DRAGFLIT*/
            { 3, ( 57129.7732, true) },		    /*OLDULT02*/
            { 4, ( 57129.7732, true) },		    /*OLDULT03*/
            { 5, ( 83330.5895, true) },		    /*OLDULT04*/
            { 6, ( 83330.5895, true) },		    /*OLDULT05*/
            { 7, ( 130220.3854, true) },		/*OLDULT06*/
            { 8, ( 130220.3854, true) },		/*STONES2*/
            { 9, ( 67657.1201, true) },		    /*BRITAIN1*/
            { 10, ( 72097.9365, true) },		/*BRITAIN2*/
            { 11, ( 98377.1201, true) },		/*BUCSDEN*/
            { 12, ( 122200.7936, false) },		/*JHELOM*/
            { 13, ( 82364.0589, false) },		/*LBCASTLE*/
            { 14, ( 119170.5895, false) },		/*LINELLE*/
            { 15, ( 138684.0589, true) },		/*MAGINCIA*/
            { 16, ( 109426.916, true) },		/*MINOC*/
            { 17, ( 114076.712, true) },		/*OCLLO*/
            { 18, ( 88816.3038, false) },		/*SAMLETHE*/
            { 19, ( 102896.3038, true) },		/*SERPENTS*/
            { 20, ( 123742.0181, true) },		/*SKARABRA*/
            { 21, ( 95294.6712, true) },		/*TRINSIC*/
            { 22, ( 121730.5895, true) },		/*VESPER*/
            { 23, ( 125074.263, true) },		/*WIND*/
            { 24, ( 170213.8548, true) },		/*YEW*/
            { 25, ( 32888.1405, false) },		/*CAVE01*/
            { 26, ( 51330.5895, false) },		/*DUNGEON9*/
            { 27, ( 101355.0793, false) },		/*FOREST_A*/
            { 28, ( 103941.2018, false) },		/*INTOWN01*/
            { 29, ( 219924.8752, false) },		/*JUNGLE_A*/
            { 30, ( 101825.2834, false) },		/*MOUNTN_A*/
            { 31, ( 98089.7732, false) },		/*PLAINS_A*/
            { 32, ( 97697.9365, false) },		/*SAILING*/
            { 33, ( 230661.2018, false) },		/*SWAMP_A*/
            { 34, ( 59663.6507, false) },		/*TAVERN01*/
            { 35, ( 38034.263, false) },		/*TAVERN02*/
            { 36, ( 66324.8752, false) },		/*TAVERN03*/
            { 37, ( 40933.8548, false) },		/*TAVERN04*/
            { 38, ( 66716.712, false) },		/*COMBAT1*/
            { 39, ( 67030.1814, false) },		/*COMBAT2*/
            { 40, ( 34951.814, false) },		/*COMBAT3*/
            { 41, ( 32888.1405, false) },		/*APPROACH*/
            { 42, ( 65645.6916, false) },		/*DEATH*/
            { 43, ( 21159.1609, false) },		/*VICTORY*/
            { 44, ( 117524.8752, false) },		/*BTCASTLE*/
            { 45, ( 203284.8752, true) },		/*NUJELM*/
            { 46, ( 99448.1405, false) },		/*DUNGEON2*/
            { 47, ( 103131.4058, true) },		/*COVE*/
            { 48, ( 119484.0589, true) },		/*MOONGLOW*/
            { 49, ( 148427.619, true) },		/*Zento*/
            { 50, ( 88920.5895, true) },		/*TokunoDungeon*/
            { 51, ( 227447.9818, true) },		/*Taiko*/
            { 52, ( 120058.7528, true) },		/*dreadhornarea*/
            { 53, ( 321410.5895, true) },		/*elfcity*/
            { 54, ( 120163.2426, true) },		/*grizzledungeon*/
            { 55, ( 120058.7528, true) },		/*melisandeslair*/
            { 56, ( 120137.1201, true) },		/*paroxysmuslair*/
            { 57, ( 106109.3424, true) },		/*gwennoconversation*/
            { 58, ( 128548.4807, true) },		/*GoodEndGame*/
            { 59, ( 114024.3537, true) },		/*GoodVsEvil*/
            { 60, ( 126954.9659, true) },		/*greatearthserpents*/
            { 61, ( 74892.9705, true) },		/*humanoids_u9*/
            { 62, ( 68701.9954, true) },		/*MinocNegative*/
            { 63, ( 114285.6235, true) },		/*Paws*/
            { 64, ( 219088.8435, true) },		/*SelimsBar*/
            { 65, ( 285309.3424, true) },		/*serpentislecombat_u7*/
            { 66, ( 118256.2358, true) },		/*ValoriaShips*/
        };

        #endregion Data
        #endregion UOMusic
        #region Build Global Linker Database
        public static List<KeyValuePair<IEntity, IEntity>> GlobalLinkerDatabase = new();
        public static int BuildGlobalLinkerDatabase()
        {
            return GlobalLinkerDatabase.Count;
        }
        public static List<IEntity> GetLinksFrom(Item item, Type IsAssignableTo)
        {
            List<IEntity> elements = new();
            IEntity linked_entity = null;
            PropertyInfo[] props = Utility.ItemRWPropertyProperties(item, IsAssignableTo: IsAssignableTo);
            for (int i = 0; i < props.Length; i++)
                if (CanCopy(item.GetType(), props[i].Name))
                    //if (CanCopy(props[i]))
                    if ((linked_entity = (IEntity)props[i].GetValue(item, null)) != null)
                        elements.Add(linked_entity);

            return elements;
        }
        public static bool LinkedTo(IEntity ent)
        {
            if (ent is Mobile m)
                return m.GetMobileBool(Mobile.MobileBoolTable.IsLinked);
            else if (ent is AddonComponent ac)
                return ac.GetItemBool(Item.ItemBoolTable.IsLinked) || (ac.Addon != null && ac.Addon.GetItemBool(Item.ItemBoolTable.IsLinked));
            else if (ent is Item item)
                return item.GetItemBool(Item.ItemBoolTable.IsLinked);

            return false;
        }
        #endregion Build Global Linker Database
        #region Generic Deed getter
        public static bool IsRedeedableAddon(IEntity Entity, out Item deed)
        {   // we ignore the Redeedable property and get the deed anyway
            deed = null;
            if (!IsAddon(Entity)) return false;

            if (Entity is BaseAddon ba)
                deed = ba.Deed;

            else if (Entity is TrophyAddon ta)
                deed = ta.GetDeed();

            else if (Entity is BaseWallDecoration wd)
                deed = wd.Deed;

            return deed != null;
        }
        public static bool IsAddon(IEntity Entity)
        {
            return (Entity is BaseAddon || Entity is TrophyAddon || Entity is BaseWallDecoration);
        }
        #endregion Generic Deed getter
        #region Secure Give (Item to player)
        /* Usage:
            switch (Utility.SecureGive(from, key))
            {
                case Backpack:
                    {
                        from.SendMessage("The key to your moving crates was placed in your backpack.");
                        break;
                    }
                case BankBox:
                    {
                        from.SendMessage("The key to your moving crates was placed in your bank box.");
                        break;
                    }
                default:
                    {
                        from.SendMessage("The key to your moving crates dropped at your feet.");
                        break;
                    }
            }
         */
        public static Item SecureGive(Mobile from, Item item)
        {
            if (from.Backpack != null && from.Backpack.TryDropItem(Server.World.GetSystemAcct(), item, sendFullMessage: false))
                return from.Backpack;
            else if (from.BankBox != null && from.BankBox.TryDropItem(Server.World.GetSystemAcct(), item, sendFullMessage: false))
                return from.BankBox;
            else
            {   // drop it at their feet
                item.MoveToWorld(from.Location, from.Map);
                item.Movable = false;   // for safety. They will need to call a GM
                return null;
            }
        }
        #endregion
        #region Township helpers
        public static List<ITownshipItem> TownshipItems(TownshipStone stone, List<Type> filter = null)
        {
            List<ITownshipItem> list = new();
            TownshipRegion tsr = null;
            foreach (var tsi in TownshipItemHelper.AllTownshipItems)
                if (filter == null || filter.Contains(tsi.GetType()))
                    if ((tsr = TownshipRegion.GetTownshipAt((tsi as Item).GetWorldLocation(), (tsi as Item).Map)) != null)
                        if (tsr.TStone != null && tsr.TStone == stone)
                            list.Add(tsi);

            return list;
        }
        public static List<Item> AllTownshipItems()
        {
            List<Item> list = new();

            foreach (var tss in TownshipStone.AllTownshipStones)
            {
                if (tss is TownshipStone && tss.LockdownRegistry != null)
                    foreach (var kvp in tss.LockdownRegistry)
                        list.Add(kvp.Key);

                list.AddRange(tss.ItemRegistry.Table.Keys);
            }

            foreach (var tsi in TownshipItemHelper.AllTownshipItems)
                list.Add(tsi as Item);

            return list.Distinct().ToList();
        }
        #endregion Township helpers
        #region Dismount
        public static bool Dismount(Mobile mob)
        {
            bool takenAction = false;

            for (int i = 0; i < mob.Items.Count; ++i)
            {
                Item item = (Item)mob.Items[i];

                if (item is IMountItem)
                {
                    IMount mount = ((IMountItem)item).Mount;

                    if (mount != null)
                    {
                        mount.Rider = null;
                        takenAction = true;
                    }

                    if (mob.Items.IndexOf(item) == -1)
                        --i;
                }
            }

            for (int i = 0; i < mob.Items.Count; ++i)
            {
                Item item = (Item)mob.Items[i];

                if (item.Layer == Layer.Mount)
                {
                    takenAction = true;
                    item.Delete();
                    --i;
                }
            }

            return takenAction;
        }
        #endregion Dismount
        #region Anti spam output
        private static Memory AntiSpamOutput = new Memory();
        public static void SendSystemMessage(IEntity source, double second_timeout, string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            KeyValuePair<IEntity, string> kvp = new KeyValuePair<IEntity, string>(source, text);

            if (source is Item item)
            {
                string key = item.Serial.ToString() + text;
                if (AntiSpamOutput.Recall(key) == null)
                {   // don't remember this item+text
                    AntiSpamOutput.Remember(key, second_timeout);
                    item.SendSystemMessage(text, accesslevel, hue);
                }
                // else we remember this item+text
                // do nothing
            }
            else if (source is Mobile m)
            {
                string key = m.Serial.ToString() + text;
                if (AntiSpamOutput.Recall(key) == null)
                {   // don't remember this item+text
                    AntiSpamOutput.Remember(key, 5);
                    m.SendSystemMessage(text, accesslevel, hue);
                }
                // else we remember this item+text
                // do nothing
            }
        }
        public static void SendOverheadMessage(IEntity source, double second_timeout, string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            KeyValuePair<IEntity, string> kvp = new KeyValuePair<IEntity, string>(source, text);

            if (source is Item item)
            {
                string key = item.Serial.ToString() + text;
                if (AntiSpamOutput.Recall(key) == null)
                {   // don't remember this item+text
                    AntiSpamOutput.Remember(key, second_timeout);
                    //item.SendSystemMessage(text, accesslevel, hue);
                    item.PublicOverheadMessage(Network.MessageType.Regular, hue, true, text);
                }
                // else we remember this item+text
                // do nothing
            }
            else if (source is Mobile m)
            {
                string key = m.Serial.ToString() + text;
                if (AntiSpamOutput.Recall(key) == null)
                {   // don't remember this item+text
                    AntiSpamOutput.Remember(key, second_timeout);
                    //m.SendSystemMessage(text, accesslevel, hue);
                    m.PublicOverheadMessage(Network.MessageType.Regular, hue, true, text);
                }
                // else we remember this item+text
                // do nothing
            }
        }
        public static bool StringTooSoon(IEntity source, double second_timeout, string text)
        {
            KeyValuePair<IEntity, string> kvp = new KeyValuePair<IEntity, string>(source, text);

            if (source is Item item)
            {
                string key = item.Serial.ToString() + text;
                if (AntiSpamOutput.Recall(key) == null)
                {   // don't remember this item+text
                    AntiSpamOutput.Remember(key, second_timeout);
                    return false;
                }
                // else we remember this item+text
                // too soon
                return true;
            }
            else if (source is Mobile m)
            {
                string key = m.Serial.ToString() + text;
                if (AntiSpamOutput.Recall(key) == null)
                {   // don't remember this item+text
                    AntiSpamOutput.Remember(key, 5);
                    return false;
                }
                // else we remember this item+text
                // too soon
                return true;
            }

            return false;
        }
        #endregion
        #region LocalTimer
        public class LocalTimer
        {
            private long m_Timer = long.MaxValue;
            private long m_Timeout = 0;
            public bool Running { get { return m_Timer != long.MaxValue; } }
            public void Start(long millisecond_timeout) { m_Timer = Core.TickCount; m_Timeout = millisecond_timeout; }
            public void Start(double millisecond_timeout) { Start((long)millisecond_timeout); }
            public void Stop() { m_Timer = long.MaxValue; m_Timeout = 0; }
            public bool Triggered { get { return Core.TickCount > m_Timer + m_Timeout; } }
            public long Remaining => (m_Timer + m_Timeout) - Core.TickCount;
            public LocalTimer(long millisecond_timeout)
            {
                Start(millisecond_timeout: millisecond_timeout);
            }
            public LocalTimer(double millisecond_timeout)
            {
                Start((long)millisecond_timeout);
            }
            public LocalTimer()
            {

            }
        }
        #endregion Local Timer
        #region Mobile Cloning
        public static List<Item> GetBackpackItems(Mobile m)
        {
            List<Item> list = new List<Item>();
            if (m.Backpack != null && m.Backpack.Items != null && m.Backpack.Items.Count > 0)
                foreach (Item item in m.Backpack.Items)
                    list.Add(item);
            return list;
        }
        public static List<Item> GetEquippedItems(Mobile m)
        {
            List<Item> list = new List<Item>();

            list.Add(m.FindItemOnLayer(Layer.Shoes));
            list.Add(m.FindItemOnLayer(Layer.Pants));
            list.Add(m.FindItemOnLayer(Layer.Shirt));
            list.Add(m.FindItemOnLayer(Layer.Helm));
            list.Add(m.FindItemOnLayer(Layer.Gloves));
            list.Add(m.FindItemOnLayer(Layer.Neck));
            list.Add(m.FindItemOnLayer(Layer.Waist));
            list.Add(m.FindItemOnLayer(Layer.InnerTorso));
            list.Add(m.FindItemOnLayer(Layer.MiddleTorso));
            list.Add(m.FindItemOnLayer(Layer.Arms));
            list.Add(m.FindItemOnLayer(Layer.Cloak));
            list.Add(m.FindItemOnLayer(Layer.OuterTorso));
            list.Add(m.FindItemOnLayer(Layer.OuterLegs));
            list.Add(m.FindItemOnLayer(Layer.InnerLegs));
            list.Add(m.FindItemOnLayer(Layer.Bracelet));
            list.Add(m.FindItemOnLayer(Layer.Ring));
            list.Add(m.FindItemOnLayer(Layer.Earrings));
            list.Add(m.FindItemOnLayer(Layer.OneHanded));
            list.Add(m.FindItemOnLayer(Layer.TwoHanded));
            list.RemoveAll(item => item == null);
            return list;
        }
        public static List<Item> CopyLoot(Type source_type)
        {
            List<Item> list = new();
            BaseCreature src = null;
            try
            {
                if (!source_type.IsAssignableTo(typeof(BaseCreature))) return list;
                src = (BaseCreature)Activator.CreateInstance(source_type);
                if (src == null) return list;
                src.Kill();
                if (src.Corpse != null && src.Corpse.Items != null)
                    foreach (Item item in src.Corpse.Items)
                        list.Add(item);
            }
            catch
            {
                return list;
            }
            finally
            {
                if (src != null)
                    src.Delete();
            }

            return list;
        }
        public static bool CopyBody(Type source_type, BaseCreature dest)
        {
            BaseCreature src = null;
            try
            {
                if (!source_type.IsAssignableTo(typeof(BaseCreature))) return false;
                src = (BaseCreature)Activator.CreateInstance(source_type);
                if (src == null) return false;
                if (dest == null) return false;
                dest.Body = src.Body;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (src != null)
                    src.Delete();
            }

            return true;
        }
        public static bool CopyConstruction(Type source_type, BaseCreature dest)
        {
            BaseCreature src = null;
            try
            {
                if (!source_type.IsAssignableTo(typeof(BaseCreature))) return false;
                src = (BaseCreature)Activator.CreateInstance(source_type);
                if (src == null) return false;
                if (dest == null) return false;
                // See CopyBody()
                dest.AI = src.AI;
                dest.FightMode = src.FightMode;
                dest.FightStyle = src.FightStyle;
                dest.RangePerception = src.RangePerception;
                dest.RangeFight = src.RangeFight;
                dest.ActiveSpeed = src.ActiveSpeed;
                dest.PassiveSpeed = src.PassiveSpeed;
                dest.Fame = src.Fame;
                dest.Karma = src.Karma;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (src != null)
                    src.Delete();
            }

            return true;
        }
        public static bool CopyStats(Type source_type, BaseCreature dest, double stat_multiplier = 1, double damage_multiplier = 1, double skill_multiplier = 1)
        {
            BaseCreature src = null;
            try
            {
                if (!source_type.IsAssignableTo(typeof(BaseCreature))) return false;
                src = (BaseCreature)Activator.CreateInstance(source_type);
                if (src == null) return false;
                if (dest == null) return false;
                dest.SetStr((int)(src.RawStr * stat_multiplier));
                dest.SetDex((int)(src.RawDex * stat_multiplier));
                dest.SetInt((int)(src.RawInt * stat_multiplier));

                dest.SetHits((int)(src.HitsMax * stat_multiplier));
                dest.SetMana((int)(src.ManaMax * stat_multiplier));

                dest.VirtualArmor = (int)(src.VirtualArmor * stat_multiplier);

                dest.SetDamage((int)(src.DamageMin * damage_multiplier), (int)(src.DamageMax * damage_multiplier));

                if (src.Skills.Length != dest.Skills.Length)
                    return false;

                Skills source_skills = src.Skills;
                Skills dest_skills = dest.Skills;
                for (int i = 0; i < source_skills.Length; ++i)
                    dest_skills[i].Base = source_skills[i].Base * skill_multiplier;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (src != null)
                    src.Delete();
            }

            return true;
        }
        public static bool MultiplyStats(BaseCreature dest, double stat_multiplier = 1, double damage_multiplier = 1, double skill_multiplier = 1)
        {
            BaseCreature src = dest;
            try
            {
                if (src == null) return false;
                if (dest == null) return false;
                dest.SetStr((int)(src.RawStr * stat_multiplier));
                dest.SetDex((int)(src.RawDex * stat_multiplier));
                dest.SetInt((int)(src.RawInt * stat_multiplier));

                dest.SetHits((int)(src.HitsMax * stat_multiplier));
                dest.SetMana((int)(src.ManaMax * stat_multiplier));

                dest.VirtualArmor = (int)(src.VirtualArmor * stat_multiplier);

                dest.SetDamage((int)(src.DamageMin * damage_multiplier), (int)(src.DamageMax * damage_multiplier));

                if (src.Skills.Length != dest.Skills.Length)
                    return false;

                Skills source_skills = src.Skills;
                Skills dest_skills = dest.Skills;
                for (int i = 0; i < source_skills.Length; ++i)
                    dest_skills[i].Base = source_skills[i].Base * skill_multiplier;
            }
            catch
            {
                return false;
            }
            finally
            {

            }

            return true;
        }
        #endregion Mobile Cloning
        #region Pathing
        private static List<Direction> dirs = new()
        {
                Direction.North,
                Direction.Right,
                Direction.East,
                Direction.Down,
                Direction.South,
                Direction.Left,
                Direction.West,
                Direction.Up
        };
        public static List<Point2D> AllPoints(Point2D oldLocation)
        {
            List<Point2D> points = new();
            points.Add(oldLocation);

            foreach (Direction d in dirs)
            {
                int x = oldLocation.m_X, y = oldLocation.m_Y;
                switch (d & Direction.Mask)
                {
                    case Direction.North:
                        --y;
                        break;
                    case Direction.Right:
                        ++x;
                        --y;
                        break;
                    case Direction.East:
                        ++x;
                        break;
                    case Direction.Down:
                        ++x;
                        ++y;
                        break;
                    case Direction.South:
                        ++y;
                        break;
                    case Direction.Left:
                        --x;
                        ++y;
                        break;
                    case Direction.West:
                        --x;
                        break;
                    case Direction.Up:
                        --x;
                        --y;
                        break;
                }

                points.Add(new Point2D(x, y));
            }

            return points;
        }
        public static Point3D OffsetPoint(Point3D oldLocation, Direction d)
        {
            int x = oldLocation.m_X, y = oldLocation.m_Y;
            switch (d & Direction.Mask)
            {
                case Direction.North:
                    --y;
                    break;
                case Direction.Right:
                    ++x;
                    --y;
                    break;
                case Direction.East:
                    ++x;
                    break;
                case Direction.Down:
                    ++x;
                    ++y;
                    break;
                case Direction.South:
                    ++y;
                    break;
                case Direction.Left:
                    --x;
                    ++y;
                    break;
                case Direction.West:
                    --x;
                    break;
                case Direction.Up:
                    --x;
                    --y;
                    break;
            }

            return new Point3D(x, y, oldLocation.m_Z);
        }
        public static List<Point3D> SimplePath(Map map, Point3D startLocation, Point3D goal, Direction d)
        {
            List<Point3D> list = new();
            Point3D newLocation = startLocation;
            do
            {
                list.Add(newLocation = OffsetPoint(new Point3D(newLocation.X, newLocation.Y, map.GetAverageZ(newLocation.X, newLocation.Y)), d));
            } while (newLocation.X != goal.X || newLocation.Y != goal.Y);

            return list;
        }
        #endregion Pathing
        #region MobileInfo
        public class MobileInfo
        {
            public Mobile target;
            public bool unavailable = false;

            public bool dead = false;
            public bool fled = false;
            public bool gone = false;
            public bool hidden = false;
            public bool path = false;
            public bool range = false;

            public double distance = 0.0;
            public bool in_range = false;   // range
            public bool available = false;  // !unavailable
            public bool in_los = false;     // we don't factor in LOS here as the different AIs handle it differently
            public bool can_see = false;    // can we see the target?
            public bool alive = false;      // !dead
            public Mobile attacker = null;  // target
            public Mobile defender = null;  // target
            public MobileInfo(Mobile c)
            {
                target = c;
            }
        }
        public static MobileInfo GetMobileInfo(Mobile source, PathFollower path, MobileInfo info)
        {
            if (info.target != null)
            {   // we don't factor in 'in LOS' here as the different AIs handle it differently
                info.in_los = source.InLOS(info.target);
                info.can_see = source.CanSee(info.target);
                info.gone = info.target.Deleted || !source.CanBeHarmful(info.target, false) || info.target.Map != source.Map;
                info.dead = !info.target.Alive || info.target.IsDeadBondedPet || info.target is BaseCreature bc && bc.IsDeadPet;
                info.hidden = !info.gone && !info.dead && !info.can_see || info.target.Hidden;
                info.path = !info.gone && !info.dead && path != null;
                info.range = (source is BaseCreature) ? source.InRange(info.target, (source as BaseCreature).RangePerception) : info.can_see && info.in_los;
                info.fled = !info.dead && !info.gone && !info.hidden && !info.range;

                if (info.gone || info.dead || info.hidden || info.fled)
                    info.unavailable = true;

                info.distance = source.GetDistanceToSqrt(info.target);
            }
            else info.unavailable = true;

            // semantic convenience maps
            info.available = !info.unavailable;
            info.in_range = info.range;
            info.alive = !info.dead;
            info.attacker = info.defender = info.target;
            return info;
        }
        #endregion MobileInfo
        #region IO
        public static int GetGuidHandle()
        {
            Guid obj = Guid.NewGuid();
            int code = GetStableHashCode(obj.ToString());
            return code;
        }
        public static string GuidHandleToFileName(int handle)
        {
            int code = handle;

            // convert to hexavigesimal (base 26, A-Z)
            string hexavigesimal = IntToString(code, Enumerable.Range('A', 26).Select(x => (char)x).ToArray());

            return hexavigesimal;
        }
        //public static string MakeGUIDFileName(ref int handle)
        //{
        //    Guid obj = Guid.NewGuid();
        //    int code = GetStableHashCode(obj.ToString());
        //    handle = code;

        //    // convert to hexavigesimal (base 26, A-Z)
        //    string hexavigesimal = IntToString(code,Enumerable.Range('A', 26).Select(x => (char)x).ToArray());

        //    return hexavigesimal;
        //}
        public static string IntToString(int value, char[] baseChars)
        {
            // 32 is the worst cast buffer size for base 2 and int.MaxValue
            int i = 32;
            char[] buffer = new char[i];
            int targetBase = baseChars.Length;

            do
            {
                buffer[--i] = baseChars[value % targetBase];
                value = value / targetBase;
            }
            while (value > 0);

            char[] result = new char[32 - i];
            Array.Copy(buffer, i, result, 0, 32 - i);

            return new string(result);
        }
        public static byte PeekByte(GenericReader reader)
        {
            byte result = reader.ReadByte();
            reader.Seek(-1, System.IO.SeekOrigin.Current);
            return result;
        }
        public static string ValidFileName(string name, string replace = "")
        {   // disallow device names recognized by thre operating system
            if (name.ToLower().Equals("con") || name.ToLower().Equals("prn"))
                name = '_' + name;
            // make sure our 'name' doesn't have any illegal file name characters.
            return string.Join(replace, name.Split(System.IO.Path.GetInvalidFileNameChars()));
        }
        private static Serial m_FileSerial = Serial.Zero;
        public static string GameTimeFileStamp(bool id = false)
        {
            DateTime dt = AdjustedDateTime.GameTime;
            string temp = dt.ToString("MMMM dd yyyy" + ", " + "hh:mm:ss tt");
            if (id)
                // ids are used to distinguish file names that may resolve to the same name otherwise
                temp += string.Format(" ({0})", m_FileSerial++);
            return ValidFileName(temp, " ");
        }
        public static string GameTimeFileStamp(DateTime dt, bool id = false)
        {
            string temp = dt.ToString("MMMM dd yyyy" + ", " + "hh:mm:ss tt");
            if (id)
                // ids are used to distinguish file names that may resolve to the same name otherwise
                temp += string.Format(" ({0})", m_FileSerial++);
            return ValidFileName(temp, " ");
        }
        #endregion IO

        #region EXPERIMENTAL_TELEPORT_AI
        public static bool GetPathArray(Map map, Point3D start, Point3D _goal, ref Direction[] directions, ref Point3D[] points)
        {
            // can we get there from here?
            Movement.MovementObject obj_start = new Movement.MovementObject(start, _goal, null);
            IPoint3D goal = obj_start.Goal;

            // get the target surface
            Spells.SpellHelper.GetSurfaceTop(ref goal);
            obj_start.Goal = new Point3D(goal.X, goal.Y, goal.Z);

            // can we get there?
            MovementPath path = new MovementPath(obj_start);
            if (path != null && path.Success)
            {
                Stack<Point3D> stack = new();
                stack.Push(start);

                foreach (Direction d in path.Directions)
                {
                    Point3D pxn = stack.Peek();
                    switch (d & Direction.Mask)
                    {
                        case Direction.North:
                            --pxn.Y;
                            break;
                        case Direction.Right:
                            ++pxn.X;
                            --pxn.Y;
                            break;
                        case Direction.East:
                            ++pxn.X;
                            break;
                        case Direction.Down:
                            ++pxn.X;
                            ++pxn.Y;
                            break;
                        case Direction.South:
                            ++pxn.Y;
                            break;
                        case Direction.Left:
                            --pxn.X;
                            ++pxn.Y;
                            break;
                        case Direction.West:
                            --pxn.X;
                            break;
                        case Direction.Up:
                            --pxn.X;
                            --pxn.Y;
                            break;
                    }

                    int newZ = 0;
                    if (map.GetTopSurface(new Point3D(pxn.X, pxn.Y, map.GetAverageZ(pxn.X, pxn.Y)), out newZ) != null)
                        stack.Push(new Point3D(new Point3D(pxn.X, pxn.Y, newZ)));
                    else
                        ; // error
                }

                directions = path.Directions.ToArray();
                points = stack.Reverse<Point3D>().ToArray();
                return true;
            }
            else
                return false;
        }
        #endregion EXPERIMENTAL_TELEPORT_AI
        #region Hair
        public static int[] HairIDs = new int[]
            {
                0x2044, 0x2045, 0x2046,
                0x203C, 0x203B, 0x203D,
                0x2047, 0x2048, 0x2049,
                0x204A, 0x0000
            };

        public static int[] BeardIDs = new int[]
            {
                0x203E, 0x203F, 0x2040,
                0x2041, 0x204B, 0x204C,
                0x204D, 0x0000
            };
        #endregion Hair
        #region Strings

        public static bool Contains(string to_test, string pattern)
        {   // Hello sir, may I have a bulk order please? => hellosir,mayihaveabulkorderplease?
            return to_test.ToLower().Replace(" ", "").Contains(pattern.ToLower().Replace(" ", ""));
        }
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }
        #endregion Strings
        #region Properties
        public static List<string> GetObjectProperties(object o)
        {
            List<string> return_list = new();
            ArrayList list = BuildList(o);
            for (int index = 0; index < list.Count; index++)
            {
                if (o == null)
                {
                    ;
                }
                else if (o is Type)
                {
                    Type type = (Type)o;
                    return_list.Add(type.Name);
                }
                else if (o is PropertyInfo)
                {
                    PropertyInfo prop = (PropertyInfo)o;
                    return_list.Add(ValueToString(o, prop));
                }
            }

            return return_list;
        }
        private static ArrayList BuildList(object o)
        {
            Type type = o.GetType();

            PropertyInfo[] props = type.GetProperties(BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);

            ArrayList groups = GetGroups(type, props);
            ArrayList list = new ArrayList();

            for (int i = 0; i < groups.Count; ++i)
            {
                DictionaryEntry de = (DictionaryEntry)groups[i];
                ArrayList groupList = (ArrayList)de.Value;

                if (i != 0)
                    list.Add(null);

                list.Add(de.Key);
                list.AddRange(groupList);
            }

            return list;
        }
        private static Type typeofCPA = typeof(CPA);
        private static Type typeofObject = typeof(object);
        private static CPA GetCPA(PropertyInfo prop)
        {
            object[] attrs = prop.GetCustomAttributes(typeofCPA, false);

            if (attrs.Length > 0)
                return attrs[0] as CPA;
            else
                return null;
        }
        private static ArrayList GetGroups(Type objectType, PropertyInfo[] props)
        {
            Hashtable groups = new Hashtable();

            for (int i = 0; i < props.Length; ++i)
            {
                PropertyInfo prop = props[i];

                if (prop.CanRead)
                {
                    CPA attr = GetCPA(prop);

                    if (attr != null /*&& m_Mobile.AccessLevel >= attr.ReadLevel*/)
                    {
                        Type type = prop.DeclaringType;

                        while (true)
                        {
                            Type baseType = type.BaseType;

                            if (baseType == null || baseType == typeofObject)
                                break;

                            if (baseType.GetProperty(prop.Name, prop.PropertyType) != null)
                                type = baseType;
                            else
                                break;
                        }

                        ArrayList list = (ArrayList)groups[type];

                        if (list == null)
                            groups[type] = list = new ArrayList();

                        list.Add(prop);
                    }
                }
            }

            ArrayList sorted = new ArrayList(groups);

            sorted.Sort(new GroupComparer(objectType));

            return sorted;
        }
        private class GroupComparer : IComparer
        {
            private Type m_Start;

            public GroupComparer(Type start)
            {
                m_Start = start;
            }

            private static Type typeofObject = typeof(Object);

            private int GetDistance(Type type)
            {
                Type current = m_Start;

                int dist;

                for (dist = 0; current != null && current != typeofObject && current != type; ++dist)
                    current = current.BaseType;

                return dist;
            }

            public int Compare(object x, object y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null)
                    return -1;
                else if (y == null)
                    return 1;

                if (!(x is DictionaryEntry) || !(y is DictionaryEntry))
                    throw new ArgumentException();

                DictionaryEntry de1 = (DictionaryEntry)x;
                DictionaryEntry de2 = (DictionaryEntry)y;

                Type a = (Type)de1.Key;
                Type b = (Type)de2.Key;

                return GetDistance(a).CompareTo(GetDistance(b));
            }
        }
        public static string ValueToString(object obj, PropertyInfo prop)
        {
            try
            {
                if (prop == null)
                    return "!Null PropertyInfo!";

                if (obj == null)
                {
                    MethodInfo mi = prop.GetGetMethod(false);
                    if (mi == null) // don't expect this to *ever* get hit.. would only occur on set-only props
                        return "!Null Obj, MethodInfo!"; // or non-public ones
                    if (!mi.IsStatic)
                        return "-null-";
                    // okay, wtf is going on!
                    throw new ApplicationException(string.Format("{0} was null", prop.ToString()));
                }

                return ValueToString(prop.GetValue(obj, null));
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                return string.Format("!{0}!", e.GetType());
            }
        }
        public static string ValueToString(object o)
        {
            if (o == null)
            {
                return "-null-";
            }
            else if (o is AccessLevel)
            {
                return o.ToString();
            }
            else if (o is string)
            {
                return string.Format("\"{0}\"", (string)o);
            }
            else if (o is bool)
            {
                return o.ToString();
            }
            else if (o is char)
            {
                return string.Format("0x{0:X} '{1}'", (int)(char)o, (char)o);
            }
            else if (o is Serial)
            {
                Serial s = (Serial)o;

                if (s.IsValid)
                {
                    if (s.IsItem)
                    {
                        return string.Format("(I) 0x{0:X}", s.Value);
                    }
                    else if (s.IsMobile)
                    {
                        return string.Format("(M) 0x{0:X}", s.Value);
                    }
                }

                return string.Format("(?) 0x{0:X}", s.Value);
            }
            else if (o is byte || o is sbyte || o is short || o is ushort || o is int || o is uint || o is long || o is ulong)
            {
                return string.Format("{0} (0x{0:X})", o);
            }
            else if (o is Mobile)
            {
                return string.Format("(M) 0x{0:X} \"{1}\"", ((Mobile)o).Serial.Value, ((Mobile)o).Name);
            }
            else if (o is Item)
            {
                return string.Format("(I) 0x{0:X}", ((Item)o).Serial.Value);
            }
            else if (o is Type)
            {
                return ((Type)o).Name;
            }
            else
            {
                return o.ToString();
            }
        }

        #endregion Properties
        #region Beep
        public static void Beep(Mobile m, int soundID)
        {
            if (m != null && m.NetState != null)
            {
                Packet p = Packet.Acquire(new PlaySound(soundID, m));
                m.NetState.Send(p);
                Packet.Release(p);
            }
        }
        public sealed class PlaySound : Packet
        {
            public PlaySound(int soundID, IPoint3D target) : base(0x54, 12)
            {
                m_Stream.Write((byte)1); // flags
                m_Stream.Write((short)soundID);
                m_Stream.Write((short)0); // volume
                m_Stream.Write((short)target.X);
                m_Stream.Write((short)target.Y);
                m_Stream.Write((short)target.Z);
            }
        }
        #endregion Beep
        #region Percentages Increase & Decrease with Multipliers
        public static int Increase(int number, int percentage)
        {
            double @decimal = percentage * 0.01;
            double multiplier = 1 + @decimal;
            return (int)(number * multiplier);
        }
        public static int Decrease(int number, int percentage)
        {
            double @decimal = percentage * 0.01;
            double multiplier = 1 - @decimal;
            return (int)(number * multiplier);
        }
        public static double Increase(double number, double percentage)
        {
            double @decimal = percentage * 0.01;
            double multiplier = 1 + @decimal;
            return number * multiplier;
        }
        public static double Decrease(double number, double percentage)
        {
            double @decimal = percentage * 0.01;
            double multiplier = 1 - @decimal;
            return number * multiplier;
        }
        #endregion Percentages Increase & Decrease with Multipliers
        #region Crypto
        public class Crypto
        {
            //While an app specific salt is not the best practice for
            //password based encryption, it's probably safe enough as long as
            //it is truly uncommon. Also too much work to alter this answer otherwise.
            private static byte[] _salt = Encoding.ASCII.GetBytes(GetStableHashCode("game-master.net", version: 1).ToString());

            /// <summary>
            /// Encrypt the given string using AES.  The string can be decrypted using 
            /// DecryptStringAES().  The sharedSecret parameters must match.
            /// </summary>
            /// <param name="plainText">The text to encrypt.</param>
            /// <param name="sharedSecret">A password used to generate a key for encryption.</param>
            public static string EncryptStringAES(string plainText, string sharedSecret)
            {
                if (string.IsNullOrEmpty(plainText))
                    throw new ArgumentNullException("plainText");
                if (string.IsNullOrEmpty(sharedSecret))
                    throw new ArgumentNullException("sharedSecret");

                string outStr = null;                       // Encrypted string to return
                RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.

                try
                {
                    // generate the key from the shared secret and the salt
                    Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                    // Create a RijndaelManaged object
                    aesAlg = new RijndaelManaged();
                    aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

                    // Create a decryptor to perform the stream transform.
                    ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                    // Create the streams used for encryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        // prepend the IV
                        msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
                        msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                            {
                                //Write all data to the stream.
                                swEncrypt.Write(plainText);
                            }
                        }
                        outStr = Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
                finally
                {
                    // Clear the RijndaelManaged object.
                    if (aesAlg != null)
                        aesAlg.Clear();
                }

                // Return the encrypted bytes from the memory stream.
                return outStr;
            }

            /// <summary>
            /// Decrypt the given string.  Assumes the string was encrypted using 
            /// EncryptStringAES(), using an identical sharedSecret.
            /// </summary>
            /// <param name="cipherText">The text to decrypt.</param>
            /// <param name="sharedSecret">A password used to generate a key for decryption.</param>
            public static string DecryptStringAES(string cipherText, string sharedSecret)
            {
                if (string.IsNullOrEmpty(cipherText))
                    throw new ArgumentNullException("cipherText");
                if (string.IsNullOrEmpty(sharedSecret))
                    throw new ArgumentNullException("sharedSecret");

                // Declare the RijndaelManaged object
                // used to decrypt the data.
                RijndaelManaged aesAlg = null;

                // Declare the string used to hold
                // the decrypted text.
                string plaintext = null;

                try
                {
                    // generate the key from the shared secret and the salt
                    Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

                    // Create the streams used for decryption.                
                    byte[] bytes = Convert.FromBase64String(cipherText);
                    using (MemoryStream msDecrypt = new MemoryStream(bytes))
                    {
                        // Create a RijndaelManaged object
                        // with the specified key and IV.
                        aesAlg = new RijndaelManaged();
                        aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
                        // Get the initialization vector from the encrypted stream
                        aesAlg.IV = ReadByteArray(msDecrypt);
                        // Create a decrytor to perform the stream transform.
                        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))

                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
                finally
                {
                    // Clear the RijndaelManaged object.
                    if (aesAlg != null)
                        aesAlg.Clear();
                }

                return plaintext;
            }

            private static byte[] ReadByteArray(Stream s)
            {
                byte[] rawLength = new byte[sizeof(int)];
                if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
                {
                    throw new SystemException("Stream did not contain properly formatted byte array");
                }

                byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
                if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
                {
                    throw new SystemException("Did not read byte array properly");
                }

                return buffer;
            }
        }
        #endregion Crypto
        #region GUID
        /// <summary>
        //  The GUID uniquely identifies a player/account, even after death
        /// </summary>
        /// <returns>uint uniquely identifying this player</returns>
        public static uint MakeGuid(string s1, string s2)
        {
            return (uint)Utility.GetStableHashCode(s1.ToLower() + s2.ToUpper(), version: 1);
        }
        #endregion GUID
        #region Temp Objects
        public static Item MakeTempItem(Item item_to_dupe)
        {
            if (item_to_dupe == null) return null;
            Item new_item = Utility.Dupe(item_to_dupe);
            if (new_item == null) return null;
            (new_item as RolledUpSheetMusic).RequiresBackpack = false;
            new_item.SetItemBool(Item.ItemBoolTable.TempObject, true);  // mark as okay to delete
            new_item.IsStaffOwned = true;                       // this keeps other rules from deleting it
            return new_item;
        }
        public static Mobile MakeTempMobile(string name = null)
        {
            Mobile m = new Mobile();
            if (m != null)
            {
                m.Name = name ?? "music context object";
                m.SetMobileBool(Mobile.MobileBoolTable.TempObject, true); // mark as okay to delete
                m.IsStaffOwned = true;                          // this keeps other rules from deleting it
            }
            return m;
        }
        #endregion Temp Objects
        #region Item Functions
        public static bool ObjectInfo(object itemO, ref string name, ref int itemID)
        {
            try
            {
                ItemData id;
                bool haveData = false;
                if (itemO is Item item)
                {
                    name = item.OldSchoolName();
                    itemID = item.ItemID;
                    return true;
                }
                else if (itemO is StaticTarget st)
                {
                    id = TileData.ItemTable[st.ItemID & 0x3FFF];
                    name = st.Name;
                    itemID = st.ItemID;
                    haveData = true;
                }
                else if (itemO is int)
                {
                    id = TileData.ItemTable[itemID & 0x3FFF];
                    itemID = (int)itemO;
                    name = id.Name;
                    haveData = true;
                }
                else return false;

                if (haveData && name != null)
                {
                    name = name.Trim();

                    if (id.Flags.HasFlag(TileFlag.ArticleA) && id.Flags.HasFlag(TileFlag.ArticleAn))
                        name = "the " + name;
                    else if (id.Flags.HasFlag(TileFlag.ArticleA))
                        name = "a " + name;
                    else if (id.Flags.HasFlag(TileFlag.ArticleAn))
                        name = "an " + name;

                    return true;
                }
            }
            catch
            {

            }

            return false;
        }
        private static bool NearDoor(Map map, Point3D[] locs)
        {
            foreach (var point in locs)
                foreach (Item item in map.GetItemsInRange(point, 2))
                    if (item is BaseDoor)
                        return true;
            return false;
        }
        private static bool NearDoor(Map map, Point3D point)
        {
            foreach (Item item in map.GetItemsInRange(point, 2))
                if (item is BaseDoor)
                    return true;
            return false;
        }
        public static Point3D GetPointNearby(Map map, Point3D loc, int max_dist, bool avoid_doors = true, Point3D[] avoid = null)
        {
            if (avoid_doors == false)
                return GetPointNearby(map, loc, max_dist, avoid);
            else
            {
                for (int ix = 0; ix < 100; ix++)
                {
                    Point3D px = GetPointNearby(map, loc, max_dist, avoid);
                    if (!NearDoor(map, px))
                        return px;
                }

                return Point3D.Zero;
            }
        }
        private static Point3D GetPointNearby(Map map, Point3D loc, int max_dist, Point3D[] avoid = null)
        {
            Point3D px = Point3D.Zero;
            Point3D new_loc = Point3D.Zero;
            Keg temp = new Keg();
            try
            {
                for (int check_loc = 1; check_loc <= max_dist; check_loc++)
                {
                    if (avoid != null)
                        for (int ix = 0; ix < avoid.Length; ix++)
                        {
                            px = Spawner.GetSpawnPosition(map, loc, homeRange: check_loc, SpawnFlags.ForceZ, temp);
                            if (avoid.Contains(px))
                                continue;
                            else break;
                        }
                    else
                        px = Spawner.GetSpawnPosition(map, loc, homeRange: check_loc, SpawnFlags.ForceZ, temp);
                    if (px != loc)
                        return px;
                }

                return new_loc;
            }
            catch { }
            finally
            {
                if (temp != null)
                    temp.Delete();
            }

            return new_loc;
        }
        public static void NormalizeOnLift(Item item)
        {
            if (item == null)
                return;

            if (BaseVendor.StandardPricingDictionary.ContainsKey(item.GetType()))
            {
                object o = null;
                try
                {   // smash all custom properties
                    o = Activator.CreateInstance(item.GetType());
                    CopyProperties(item, o as Item);
                    (o as Item).Delete();
                }
                catch { }
                if (o == null)
                {
                    #region Fallback
                    item.Hue = 0;
                    if (item is BaseClothing bc)
                    {
                        bc.MagicCharges = 0;
                        bc.MagicEffect = MagicEquipEffect.None;
                    }
                    else if (item is BaseJewel bj)
                    {
                        bj.MagicCharges = 0;
                        bj.MagicEffect = MagicEquipEffect.None;
                    }
                    else if (item is BaseShield bs)
                    {
                        bs.MagicCharges = 0;
                        bs.MagicEffect = MagicEquipEffect.None;
                        bs.DurabilityLevel = ArmorDurabilityLevel.Regular;
                        bs.ProtectionLevel = ArmorProtectionLevel.Regular;
                        bs.Quality = ArmorQuality.Regular;
                    }
                    else if (item is BaseArmor ba)
                    {
                        ba.MagicCharges = 0;
                        ba.MagicEffect = MagicEquipEffect.None;
                        ba.DurabilityLevel = ArmorDurabilityLevel.Regular;
                        ba.ProtectionLevel = ArmorProtectionLevel.Regular;
                        ba.Quality = ArmorQuality.Regular;
                    }
                    else if (item is BaseWeapon bw) // includes wands
                    {
                        bw.MagicCharges = 0;
                        bw.MagicEffect = MagicItemEffect.None;
                        bw.DurabilityLevel = WeaponDurabilityLevel.Regular;
                        bw.AccuracyLevel = WeaponAccuracyLevel.Regular;
                        bw.DamageLevel = WeaponDamageLevel.Regular;
                        bw.Quality = WeaponQuality.Regular;
                    }
                    #endregion Fallback
                }
            }
            // if this is not a usual item, convert it to something usual
            else
            {
                // we don't know wtf this thing is
                Item no_draw = new Item(1);
                CopyProperties(item, no_draw);
                no_draw.Delete();
                Console.WriteLine("No Draw item {0} created", item.Serial);

                #region old
                //Item temp = null;
                //if (item is BaseClothing)
                //    switch (item.Layer)
                //    {
                //        default: { item.Delete(); break; }
                //        case Layer.Cloak: { temp = new Cloak(); item.ItemID = temp.ItemID; break; }
                //        //case Layer.Neck: { temp = new StuddedGorget(); item.ItemID = temp.ItemID; break; } 
                //        //case Layer.TwoHanded: { temp = new Buckler(); item.ItemID = temp.ItemID; break; } 
                //        case Layer.Gloves: { temp = new ClothGloves(); item.ItemID = temp.ItemID; break; }
                //        case Layer.Helm: { temp = new StrawHat(); item.ItemID = temp.ItemID; break; }
                //        //case Layer.Arms: { temp = new StuddedArms(); item.ItemID = temp.ItemID; break; } 

                //        case Layer.InnerLegs:
                //        case Layer.OuterLegs:
                //        case Layer.Pants: { temp = new ShortPants(); item.ItemID = temp.ItemID; break; }

                //        case Layer.InnerTorso:
                //        case Layer.OuterTorso:
                //        case Layer.Shirt: { temp = new Shirt(); item.ItemID = temp.ItemID; break; }
                //    }
                //else if (item is BaseShield)
                //{ temp = new Buckler(); item.ItemID = temp.ItemID; }
                //else if (item is BaseArmor)
                //    switch (item.Layer)
                //    {
                //        default: { item.Delete(); break; }
                //        case Layer.Neck: { temp = new StuddedGorget(); item.ItemID = temp.ItemID; break; }
                //        case Layer.TwoHanded: { temp = new Buckler(); item.ItemID = temp.ItemID; break; }
                //        case Layer.Gloves: { temp = new StuddedGloves(); item.ItemID = temp.ItemID; break; }
                //        case Layer.Helm: { temp = new LeatherCap(); item.ItemID = temp.ItemID; break; }
                //        case Layer.Arms: { temp = new StuddedArms(); item.ItemID = temp.ItemID; break; }

                //        case Layer.InnerLegs:
                //        case Layer.OuterLegs:
                //        case Layer.Pants: { temp = new StuddedLegs(); item.ItemID = temp.ItemID; break; }

                //        case Layer.InnerTorso:
                //        case Layer.OuterTorso:
                //        case Layer.Shirt: { temp = new StuddedChest(); item.ItemID = temp.ItemID; break; }
                //    }
                //else if (item is BaseWeapon)
                //{
                //    if (item.Layer == Layer.OneHanded)
                //        temp = new Katana();
                //    else
                //        temp = new Bardiche();

                //    item.ItemID = temp.ItemID;
                //}

                //if (temp != null)
                //    temp.Delete();
                #endregion old
            }
        }
        public static void TrackStaffOwned(Mobile from, Item dupe)
        {
            dupe.SetItemBool(Item.ItemBoolTable.StaffOwned, true);   // we can track this for illegal distribution
            LogHelper logger = new LogHelper("staff dupe.log", overwrite: false, sline: true);
            logger.Log(string.Format("{0} duped item {1}({2})", from, dupe, dupe.Serial));
            logger.Finish();
        }
        public static bool DefaultTownshipAndHouseAccess(Item item, Mobile from)
        {
            bool valid = Server.Township.TownshipItemHelper.IsOwner(item, from);
            BaseHouse house = BaseHouse.FindHouseAt(item);

            if (!valid && house != null && house.IsFriend(from))
                valid = true;

            if (valid)
                return true;
            return false;
        }
        public static bool IsTownshipOrHouse(Item item, Mobile from)
        {
            TownshipRegion itsr = TownshipRegion.GetTownshipAt(item.GetWorldLocation(), item.Map);
            TownshipRegion mtsr = TownshipRegion.GetTownshipAt(from.Location, from.Map);
            if (itsr == mtsr && itsr is TownshipRegion)
                return true;

            BaseHouse house = BaseHouse.FindHouseAt(item);
            if (house != null && house.IsFriend(from))
                return true;

            return false;
        }
        public static bool InTownshipOrHouse(Item item)
        {
            TownshipRegion itsr = TownshipRegion.GetTownshipAt(item.GetWorldLocation(), item.Map);
            if (itsr != null)
                return true;

            BaseHouse house = BaseHouse.FindHouseAt(item);
            if (house != null)
                return true;

            return false;
        }
        public static bool InTownshipOrHouse(Mobile m)
        {
            TownshipRegion itsr = TownshipRegion.GetTownshipAt(m.Location, m.Map);
            if (itsr != null)
                return true;

            BaseHouse house = BaseHouse.FindHouseAt(m);
            if (house != null)
                return true;

            return false;
        }
        public static bool InWorld(IEntity o)
        {
            if (o is Mobile mob)
                return !mob.Deleted && mob.Map != null && mob.Map != Map.Internal;
            if (o is Item item)
                return !item.Deleted && item.Map != null && item.Map != Map.Internal;
            return false;
        }
        public static void ReplaceItem(Item new_item, Item oldItem, bool copy_properties = true)
        {
            bool inContainer = false;
            bool isLockedDown = false;
            bool isSecure = false;
            bool notMovable = false;
            bool isHeld = false;
            bool isOneHanded = false;
            if (oldItem.Movable == false)
            { // handle locked down and secure 
                BaseHouse bh = BaseHouse.FindHouseAt(oldItem);
                if (bh != null && bh.IsLockedDown(oldItem))
                    isLockedDown = true;
                else if (bh != null && bh.IsSecure(oldItem))
                    isSecure = true;
                else
                    notMovable = true;
            }
            if (oldItem.Parent is Container)
                inContainer = true;
            if (oldItem.ParentContainer == null && oldItem.ParentMobile != null)
            {   // the player has this hammer equipped
                Item thing = oldItem.ParentMobile.FindItemOnLayer(Layer.OneHanded);
                if (thing == oldItem)
                    isOneHanded = isHeld = true;
                if (isHeld == false)
                    thing = oldItem.ParentMobile.FindItemOnLayer(Layer.TwoHanded);
                if (thing == oldItem)
                    isHeld = true;
            }

            if (copy_properties)
                // copy properties
                Utility.CopyProperties(new_item, oldItem);

            if (inContainer)
            {   // in a container
                new_item.Parent = null;   // addItem won't add an item it thinks it already contains.
                (oldItem.Parent as Container).DropItem(new_item);
                new_item.Location = oldItem.Location;
                new_item.Map = oldItem.Map;
                (oldItem.Parent as Container).RemoveItem(oldItem);
            }
            else if (isHeld)
            {   // held in hand
                if (isOneHanded)
                    oldItem.ParentMobile.AddToBackpack(oldItem.ParentMobile.FindItemOnLayer(Layer.OneHanded));
                else
                    oldItem.ParentMobile.AddToBackpack(oldItem.ParentMobile.FindItemOnLayer(Layer.TwoHanded));
                oldItem.ParentMobile.EquipItem(new_item);
            }
            else if (isLockedDown)
            {   // locked down in a house
                BaseHouse bh = BaseHouse.FindHouseAt(oldItem);
                if (bh != null)
                {
                    new_item.Movable = true;          // was set false in copy properties
                    bh.SetLockdown(oldItem, false);  // unlock their old item
                    bh.SetLockdown(new_item, true);   // lock down the new item
                }
            }
            else if (isSecure)
            {
                // we're cool. You can't secure a single item, the house code just 'locks it down'
            }
            else if (notMovable)
            {
                // okay as it is .. it's already movable == false from copy properties
            }
            else
                new_item.MoveToWorld(oldItem.Location, oldItem.Map);
        }
        public static List<Item> StackItems(List<Item> in_list)
        {
            List<Item> out_list = new List<Item>();

            for (int ix = 0; ix < in_list.Count; ix++)
            {
                for (int jx = 0; jx < in_list.Count; jx++)
                {
                    if (in_list[ix] == in_list[jx] || in_list[ix].ToDelete || in_list[jx].ToDelete)
                        // we're looking at ourself
                        continue;

                    if (Item.StackableRule(in_list[ix], in_list[jx]))
                    {   // we have two items that can be stacked
                        in_list[ix].LootType = (in_list[ix].LootType != in_list[jx].LootType ? LootType.Regular : in_list[ix].LootType);
                        in_list[jx].ToDelete = true;
                        in_list[ix].Amount += in_list[jx].Amount;
                    }
                }
            }

            // now compact
            foreach (Item item in in_list)
                if (item.ToDelete == true)
                    item.Delete();
                else
                    out_list.Add(item);

            return out_list;
        }
        public static int ReplaceLockdowns(ReplaceEntry[] table)
        {
            Dictionary<Item, ReplaceEntry> replaceDict = new Dictionary<Item, ReplaceEntry>();
            foreach (Item item in Server.World.Items.Values)
            {
                foreach (ReplaceEntry re in table)
                {
                    if (item.GetType() == re.SourceType && (re.SourceItemID == -1 || item.ItemID == re.SourceItemID))
                    {
                        replaceDict[item] = re;
                        break;
                    }
                }
            }
            int count = 0;
            foreach (KeyValuePair<Item, ReplaceEntry> kvp in replaceDict)
            {
                Item oldItem = kvp.Key;
                ReplaceEntry re = kvp.Value;
                Item new_item = null;
                try
                {
                    new_item = (Item)Activator.CreateInstance(re.TargetType, re.TargetArgs);
                }
                catch
                {
                }
                if (new_item != null)
                {
                    new_item.Amount = oldItem.Amount;
                    new_item.Hue = oldItem.Hue;
                    new_item.Movable = oldItem.Movable;
                    ReplaceLockdown(oldItem, new_item);
                    count++;
                }
            }
            return count;
        }
        public class ReplaceEntry
        {
            private Type m_SourceType;
            private int m_SourceItemID;
            private Type m_TargetType;
            private object[] m_TargetArgs;
            public Type SourceType { get { return m_SourceType; } }
            public int SourceItemID { get { return m_SourceItemID; } }
            public Type TargetType { get { return m_TargetType; } }
            public object[] TargetArgs { get { return m_TargetArgs; } }
            public ReplaceEntry(Type sourceType, int sourceItemID, Type targetType, object[] targetArgs)
            {
                m_SourceType = sourceType;
                m_SourceItemID = sourceItemID;
                m_TargetType = targetType;
                m_TargetArgs = targetArgs;
            }
        }
        private static void ReplaceLockdown(Item oldItem, Item new_item)
        {
            BaseHouse house = BaseHouse.FindHouseAt(oldItem);
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(oldItem.GetWorldLocation(), oldItem.Map);

            bool isHouseLockdown = (house != null && house.IsLockedDown(oldItem));
            bool isTownshipLockdown = (tsr != null && tsr.TStone != null && tsr.TStone.IsLockedDown(oldItem));
            bool isHouseAddon = (house != null && house.Addons.Contains(oldItem));

            Mobile townshipOwner = null;

            if (isTownshipLockdown)
            {
                LockDownContext context;

                if (tsr.TStone.LockdownRegistry.TryGetValue(oldItem, out context))
                    townshipOwner = context.Mobile;
            }

            if (isHouseLockdown)
                house.SetLockdown(oldItem, false);

            if (isTownshipLockdown)
            {
                tsr.TStone.LockdownRegistry.Remove(oldItem);
                oldItem.Movable = true;
            }

            oldItem.ReplaceWith(new_item);

            if (isHouseLockdown)
                house.SetLockdown(new_item, true);

            if (isTownshipLockdown)
            {
                tsr.TStone.LockdownRegistry[new_item] = new LockDownContext(townshipOwner);
                new_item.Movable = false;
            }

            if (isHouseAddon)
                house.Addons.Add(new_item);
        }
        #endregion Item Functions
        #region N Random elements with sum M
        public static int[] RandomNSumM(int n, int m)
        {
            /*
             * Just generate N random numbers, compute their sum, divide each one by the sum and multiply by M.
             * */
            if (n <= 0 || m <= 0) return new int[1];
            List<double> n_list = new();
            for (int ix = 0; ix < n; ix++)
                n_list.Add(Utility.RandomDouble());

            double sum = n_list.Sum();
            List<int> result_list = new();
            for (int jx = 0; jx < n_list.Count; jx++)
                result_list.Add((int)((n_list[jx] / sum) * m));

            int isum = m - result_list.Sum();
            if (isum > 0)
            {   // lost a few elements due to rounding, we will make them up here.
                int minimumValueIndex = result_list.IndexOf(result_list.Min());
                if (minimumValueIndex >= 0)
                    result_list[minimumValueIndex] += isum;
            }

            return result_list.ToArray();
        }
        #endregion N Random elements with sum M
        #region Dungeons
        public static bool TeleporterFromTo(Teleporter tp, List<Rectangle2D> from = null, List<Rectangle2D> to = null)
        {
            if (from != null)
                foreach (Rectangle2D rect in from)
                    if (rect.Contains(tp.Location))
                        return true;

            if (to != null)
                foreach (Rectangle2D rect in to)
                    if (rect.Contains(tp.PointDest))
                        return true;

            return false;
        }
        public static bool IsDungeonTeleporter(Teleporter tp)
        {
            if (tp.MapDest != Map.Felucca)
                // need to handle other map configurations
                return false;

            // first check to see if this teleporter is in a dungeon
            foreach (Region rx in Region.Regions)
                if (rx != null && rx.Map == Map.Felucca && rx.IsDungeonRules)
                    foreach (Rectangle3D r in rx.Coords)
                        if (r.Contains(tp.Location) || r.Contains(tp.PointDest))
                            return true;
            return false;
        }
        // 
        public static bool IsDungeonEnterExitTeleporter(Teleporter tp)
        {
            if (tp.MapDest != Map.Felucca)
                // need to handle other map configurations
                return false;

            // first check to see if this teleporter is in a dungeon
            foreach (Region rx in Region.Regions)
                if (rx != null && rx.Map == Map.Felucca && rx.IsDungeonRules)
                    foreach (Rectangle3D r in rx.Coords)
                        if ((Utility.IsDungeon(tp.Location) && !Utility.IsDungeon(tp.PointDest)) || (!Utility.IsDungeon(tp.Location) && Utility.IsDungeon(tp.PointDest)))
                            return true;
            return false;
        }
        public static bool IsDungeon(Point3D px)
        {
            // first check to see if this point is in a dungeon
            foreach (Region rx in Region.Regions)
                if (rx != null && rx.Map == Map.Felucca && rx.IsDungeonRules)
                    foreach (Rectangle3D r in rx.Coords)
                        if (r.Contains(px))
                            return true;
            return false;
        }
        #endregion Dungeons
        #region Pets
        public static BaseCreature FindPet(Mobile master, string petName)
        {
            foreach (Mobile m in Mobile.PetCache)
            {
                if (m is BaseCreature bc)
                    if (bc != null && (bc.Alive || bc.IsDeadBondedPet))
                        if (bc.Controlled && bc.ControlMaster == master && Insensitive.Equals(bc.Name, petName))
                            if (bc.IsAnyStabled == false)
                                return bc;
            }

            return null;
        }
        public static int CountPets(Mobile master, int range = -1)
        {
            List<BaseCreature> pets = new List<BaseCreature>();

            if (range == -1)
            {
                foreach (Mobile m in master.Followers)
                {
                    BaseCreature pet = m as BaseCreature;

                    if (pet != null && pet.Controlled && pet.ControlMaster == master && CanStablePet(pet))
                        pets.Add(pet);
                }
            }
            else
            {
                foreach (Mobile m in master.Followers)
                {
                    BaseCreature pet = m as BaseCreature;

                    if (pet != null && pet.Controlled && pet.ControlMaster == master && CanStablePet(pet))
                        if (master.GetDistanceToSqrt(pet) <= range || (master.Mount != null && (master.Mount as BaseCreature).Map == Map.Internal))
                            pets.Add(pet);
                }
            }
            // range -1 will pickup the mount (on the internal map,) otherwise count the mount
            return pets.Count;
        }
        public static bool CanStablePets(Mobile master, int range = -1)
        {
            if (CountPets(master, range) > 0)
                if (AnimalTrainer.Table.ContainsKey(master))
                    if (AnimalTrainer.Table[master].Count + CountPets(master, range) >= BaseCreature.GetMaxStabled(master))
                        return false; // You have too many pets in the stables!

            return true;
        }
        public static int StablePets(Mobile master, int range = -1)
        {
            List<BaseCreature> pets = new List<BaseCreature>();

            // dismount
            IMount mt = master.Mount;
            if (mt != null)
                mt.Rider = null;

            if (range == -1)
            {
                foreach (Mobile m in master.Followers)
                {
                    BaseCreature pet = m as BaseCreature;

                    if (pet != null && pet.Controlled && pet.ControlMaster == master && CanStablePet(pet))
                        pets.Add(pet);
                }
            }
            else
            {
                foreach (Mobile m in master.Followers)
                {
                    BaseCreature pet = m as BaseCreature;

                    if (pet != null && pet.Controlled && pet.ControlMaster == master && CanStablePet(pet))
                        if (master.GetDistanceToSqrt(pet) <= range)
                            pets.Add(pet);
                }
            }

            int stabled = 0;

            for (int i = 0; i < pets.Count; i++)
            {
                BaseCreature pet = pets[i];

                if (AnimalTrainer.Table.ContainsKey(master))
                    if (AnimalTrainer.Table[master].Count >= BaseCreature.GetMaxStabled(master))
                        continue; // You have too many pets in the stables!

                // stable pet and charge
                StablePet(master, pet);
                ChargeStableFee(master, pet);

                stabled++;
            }

            return stabled;
        }
        public static void StablePet(Mobile master, BaseCreature pet)
        {
            if (!pet.IsDeadBondedPet)
                pet.Resurrect();

            if (pet is IMount)
                ((IMount)pet).Rider = null;

            pet.ControlTarget = null;
            pet.ControlOrder = OrderType.Stay;

            pet.Internalize();

            pet.SetControlMaster(null);
            pet.SummonMaster = null;

            pet.IsAnimalTrainerStabled = true;

            if (AnimalTrainer.Table.ContainsKey(master))
            {
                if (!AnimalTrainer.Table[master].Contains(pet))
                    AnimalTrainer.Table[master].Add(pet);
            }
            else
            {
                AnimalTrainer.Table.Add(master, new List<BaseCreature>() { pet });
            }

            pet.LastStableChargeTime = DateTime.UtcNow;
        }
        public static bool ChargeStableFee(Mobile master, BaseCreature pet)
        {
            int charge = AnimalTrainer.UODayChargePerPet(master);

            if (master.BankBox != null && master.BankBox.ConsumeTotal(typeof(Gold), charge))
                return true;

            // they are probably being jailed, let them slide for this FIRST payment only
            pet.SetCreatureBool(CreatureBoolTable.StableHold, true);
            pet.StableBackFees = charge;
            master.SendMessage("Thou hast not the funds in thy bank account to stable this pet.");
            master.SendMessage("The stable master will keep a running account for {0} of owed fees.", pet.Name);

            return false;
        }
        public static bool CanStablePet(BaseCreature bc)
        {
            if (bc.IsAnyStabled)
                return false;

            if (bc.Summoned || bc.IOBFollower)
                return false;

            if ((bc is PackLlama || bc is PackHorse || bc is Beetle) && (bc.Backpack != null && bc.Backpack.Items.Count > 0))
                return false;

            if (bc is BaseEscortable || bc is BaseHire)
                return false;

            return true;
        }
        #endregion Pets
        #region Spawners
        public static int SpawnerIndexOf(Spawner spawner, string text)
        {
            for (int ix = 0; ix < spawner.ObjectNamesRaw.Count; ix++)
                if ((spawner.ObjectNamesRaw[ix] as string).ToLower().Contains(text.ToLower()))
                    return ix;
            return -1;
        }

        public static bool SpawnerSpawns(Spawner spawner, string text)
        {
            foreach (string name in spawner.ObjectNamesRaw)
                if (name.ToLower().Contains(text.ToLower()))
                    return true;
            return false;
        }

        public static bool SpawnerSpawns(Spawner spawner, Type type, List<Type> exclude = null)
        {   // note: can't be used for RaresSpawners
            foreach (string name in spawner.ObjectNamesRaw)
            {
                string[] chunks = name.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string typeName in chunks)
                {
                    Type typeFound = ScriptCompiler.FindTypeByName(typeName);
                    if (typeFound != null)
                    {
                        if (exclude != null && exclude.Contains(typeFound))
                            continue;
                        if (type.IsAssignableFrom(typeFound))
                            return true;
                    }
                }
            }

            return false;
        }
        #endregion Spawners
        #region Serialization Helper
        /*
            if (o.GetType() == typeof(string value);
            if (o.GetType() == typeof(DateTime value);
            if (o.GetType() == typeof(TimeSpan value);
            if (o.GetType() == typeof(decimal value);
            if (o.GetType() == typeof(long value);
            if (o.GetType() == typeof(ulong value);
            if (o.GetType() == typeof(int value);
            if (o.GetType() == typeof(uint value);
            if (o.GetType() == typeof(short value);
            if (o.GetType() == typeof(ushort value);
            if (o.GetType() == typeof(double value);
            if (o.GetType() == typeof(float value);
            if (o.GetType() == typeof(char value);
            if (o.GetType() == typeof(byte value);
            if (o.GetType() == typeof(sbyte value);
            if (o.GetType() == typeof(bool value);
         */
        private static string[] known_object_types = new[]
        {
            "string", "DateTime", "TimeSpan", "decimal", "long", "ulong", "int", "uint", "short", "ushort", "double", "float", "char","byte", "sbyte", "bool",
        };
        public static void Writer(object o, GenericWriter writer)
        {
            if (o.GetType() == typeof(string)) { writer.Write("string"); writer.Write(o as string); }
            else if (o.GetType() == typeof(DateTime)) { writer.Write("DateTime"); writer.Write((DateTime)o); }
            else if (o.GetType() == typeof(TimeSpan)) { writer.Write("TimeSpan"); writer.Write((TimeSpan)o); }
            else if (o.GetType() == typeof(decimal)) { writer.Write("decimal"); writer.Write((decimal)o); }
            else if (o.GetType() == typeof(long)) { writer.Write("long"); writer.Write((long)o); }
            else if (o.GetType() == typeof(ulong)) { writer.Write("ulong"); writer.Write((ulong)o); }
            else if (o.GetType() == typeof(int)) { writer.Write("int"); writer.Write((int)o); }
            else if (o.GetType() == typeof(uint)) { writer.Write("uint"); writer.Write((uint)o); }
            else if (o.GetType() == typeof(short)) { writer.Write("short"); writer.Write((short)o); }
            else if (o.GetType() == typeof(ushort)) { writer.Write("ushort"); writer.Write((ushort)o); }
            else if (o.GetType() == typeof(double)) { writer.Write("double"); writer.Write((double)o); }
            else if (o.GetType() == typeof(float)) { writer.Write("float"); writer.Write((float)o); }
            else if (o.GetType() == typeof(char)) { writer.Write("char"); writer.Write((char)o); }
            else if (o.GetType() == typeof(byte)) { writer.Write("byte"); writer.Write((byte)o); }
            else if (o.GetType() == typeof(sbyte)) { writer.Write("sbyte"); writer.Write((sbyte)o); }
            else if (o.GetType() == typeof(bool)) { writer.Write("bool"); writer.Write((bool)o); }
            else throw new ApplicationException("Unsupported data type");
        }
        /*
            if (o.GetType() == typeof(string)){}
            if (o.GetType() == typeof(DateTime)){}
            if (o.GetType() == typeof(TimeSpan)){}
            if (o.GetType() == typeof(decimal)){}
            if (o.GetType() == typeof(long)){}
            if (o.GetType() == typeof(ulong)){}
            if (o.GetType() == typeof(int)){}
            if (o.GetType() == typeof(uint)){}
            if (o.GetType() == typeof(short)){}
            if (o.GetType() == typeof(ushort)){}
            if (o.GetType() == typeof(double)){}
            if (o.GetType() == typeof(float)){}
            if (o.GetType() == typeof(char)){}
            if (o.GetType() == typeof(byte)){}
            if (o.GetType() == typeof(sbyte)){}
            if (o.GetType() == typeof(bool)){}
         */
        public static object Reader(GenericReader reader)
        {
            string type = reader.ReadString();
            if (known_object_types.Contains(type))
            {
                if (type == "string") { return reader.ReadString(); }
                else if (type == "DateTime") { return reader.ReadDateTime(); }
                else if (type == "TimeSpan") { return reader.ReadTimeSpan(); }
                else if (type == "decimal") { return reader.ReadDecimal(); }
                else if (type == "long") { return reader.ReadLong(); }
                else if (type == "ulong") { return reader.ReadULong(); }
                else if (type == "int") { return reader.ReadInt(); }
                else if (type == "uint") { return reader.ReadUInt(); }
                else if (type == "short") { return reader.ReadShort(); }
                else if (type == "ushort") { return reader.ReadUShort(); }
                else if (type == "double") { return reader.ReadDouble(); }
                else if (type == "float") { return reader.ReadFloat(); }
                else if (type == "char") { return reader.ReadChar(); }
                else if (type == "byte") { return reader.ReadByte(); }
                else if (type == "sbyte") { return reader.ReadSByte(); }
                else if (type == "bool") { return reader.ReadBool(); }
                else throw new ApplicationException("Unsupported data type");
            }
            throw new ApplicationException("Unsupported data type");
        }
        #endregion Serialization Helper
        #region Where
        public static bool AnyoneCanSeeMe(BaseCreature bc)
        {
            IPooledEnumerable eable = bc.Map.GetMobilesInRange(bc.Location, 13);
            foreach (Mobile mob in eable)
                if (mob is PlayerMobile) { eable.Free(); return true; }
            return false;
        }
        public static bool IsInTownBank(Item item)
        {
            IPooledEnumerable eable = item.Map.GetMobilesInRange(item.Location, 20);
            foreach (Mobile mob in eable)
                if (mob is Banker || mob is Minter)
                {
                    Region rx = Region.Find(mob.Location, mob.Map);
                    if (rx is not null && rx is GuardedRegion)
                    {
                        eable.Free();
                        return true;
                    }
                }
            eable.Free();
            return false;
        }
        public static bool InAHouse(Mobile m)
        {
            ArrayList regions = Region.FindAll(m.Location, m.Map);
            foreach (Region rx in regions)
                if (rx is HouseRegion)
                    return true;
            return false;
        }
        public static List<Point2D> SpawnablePoints(List<Point2D> points, Map map, int Z)
        {
            List<Point2D> list = new();
            for (int ix = 0; ix < points.Count; ix++)
            {
                Point2D px = points[ix];
                if (Utility.CanSpawnLandMobile(map, new Point2D(px.X, px.Y), Z))
                    list.Add(px);
            }
            return list;
        }
        public static bool NearPlayer(Map map, Point3D px, double range)
        {
            IPooledEnumerable eable = map.GetMobilesInRange(px, 15);
            foreach (Mobile m in eable)
            {
                if (m is PlayerMobile && m.AccessLevel <= AccessLevel.Player)
                {
                    if (Utility.GetDistanceToSqrt(px, m.Location) < range)
                    {
                        eable.Free();
                        return true;
                    }
                }
            }
            eable.Free();

            return false;
        }
        #endregion Where
        #region time

        public static DateTime DTOverflowHandler(DateTime dt, TimeSpan ts)
        {
            double max_dt_ticks = DateTime.MaxValue.Ticks;
            double check_time_ticks = dt.Ticks;
            double ts_ticks = ts.Ticks;
            if ((check_time_ticks + ts_ticks) > max_dt_ticks)
                return DateTime.MaxValue;   // overflow
            else return dt + ts;
        }
        public static bool TryParseTimeSpan(string text, out TimeSpan ts)
        {   // TimeSpan.TryParse returns funky values. Example:
            //  "1:20:30" correctly returns 1hr, 20m, and 30s
            //  "1200:20:30" incorrectly returns 1200ds, 0hrs, 20m, and 30s
            //  Our routine here returns what I believe are more consistent results.
            //  i.e., "1200:20:30" will return 50ds, 0hrs, 20m, and 30s
            // Priority:
            // Seconds, Minutes, Hours, Days, Milliseconds
            ts = default(TimeSpan);
            int[] values = IntParser(text);
            if (values.Length == 5)         // D:H:M:S:M
                ts = new TimeSpan(values[0], values[1], values[2], values[3], values[4]);
            else if (values.Length == 4)         // D:H:M:S:0
                ts = new TimeSpan(values[0], values[1], values[2], values[3], 0);
            else if (values.Length == 3)    // 0:H:M:S:0
                ts = new TimeSpan(0, values[0], values[1], values[2], 0);
            else if (values.Length == 2)    // 0:0:M:S:0
                ts = new TimeSpan(0, 0, values[0], values[1], 0);
            else if (values.Length == 1)    // 0:0:0:S:0
                ts = new TimeSpan(0, 0, 0, values[0], 0);
            else
                return false;

            return true;
        }
        #endregion time
        public static void DropHolding(Mobile m)
        {
            Item held = m.Holding;
            if (held != null)
            {
                held.ClearBounce();
                if (m.Backpack != null)
                {
                    m.Backpack.DropItem(held);
                }
            }
            m.Holding = null;
        }
        public static int GetRandomHue()
        {
            switch (Utility.Random(5))
            {
                default:
                case 0: return Utility.RandomBlueHue();
                case 1: return Utility.RandomGreenHue();
                case 2: return Utility.RandomRedHue();
                case 3: return Utility.RandomYellowHue();
                case 4: return Utility.RandomNeutralHue();
            }
        }
        public static int RandomBrightHue()
        {
            if (0.1 > Utility.RandomDouble())
                return Utility.RandomList(0x62, 0x71);

            return Utility.RandomList(0x03, 0x0D, 0x13, 0x1C, 0x21, 0x30, 0x37, 0x3A, 0x44, 0x59);
        }
        #region MobileInventory

        public static Item InventoryFind(Mobile m, Type type, bool BackpackOnly = false)
        {
            List<Item> list = Inventory(m, BackpackOnly);
            foreach (Item item in list)
                if (type.IsAssignableFrom(item.GetType()))
                    return item;

            return null;
        }
        public static Item InventoryFind(List<Item> list, Type type)
        {
            foreach (Item item in list)
                if (type.IsAssignableFrom(item.GetType()))
                    return item;

            return null;
        }
        public static bool InventoryHas(List<Item> list, Type type)
        {
            foreach (Item item in list)
                if (item.GetType() == type)
                    return true;

            return false;
        }
        public static bool InventoryHas(Mobile m, Type type, bool BackpackOnly = false)
        {
            List<Item> list = Inventory(m, BackpackOnly);
            foreach (Item item in list)
                if (type.IsAssignableFrom(item.GetType()))
                    return true;

            return false;
        }
        public static List<Item> Inventory(Mobile m, bool BackpackOnly = false)
        {
            var list = new List<Item>();
            if (m != null)
            {
                if (!BackpackOnly)
                    list.AddRange(m.Items);
                if (m.Backpack != null && m.Backpack.Items != null)
                    GetContainerItems(m.Backpack, list);
            }
            return list;
        }
        public static void GetContainerItems(Container c, List<Item> list)
        {
            if (c != null && c.Items != null)
            {
                list.AddRange(c.Items);
                foreach (Item item in c.Items)
                    if (item is Container cx)
                        GetContainerItems(cx, list);
            }
            return;
        }
        #endregion MobileInventory
        #region DogTag
        /* DogTags are a means of identifying an objects origin by their placement on the internal map.
         *  It's free, takes no memory or disk space. 
         *  typically, you will pass a string that identifies the 'owner'.
         *  For example: The Vendor system uses lists of items and mobiles as 'Display Objects'. 
         *      These display objects are managed by the DisplayCache. These random items and mobiles
         *      have no parent, and so their discovery gives no hint as to where they originated.
         *      Many objects on the internal map are defaulted to 0,0. 
         *      So in our example, we will 'dog tag' the objects owned by the DisplayCache.
         *      I.e., Point2D px = DogTag.Get(DisplayCache.GetType().FullName);
         *      Then we will x.MoveToWorld (px,Map.Internal); these objects.
         *      Then, certain tools (beyond the scope of this discussion,) can identify - with reasonable certainty -
         *      the objects at this location. I.e.,
         *      if (item.Location == DogTag.Get(DisplayCache.GetType().FullName))
         *          It's a DisplayCache object.
         */
        public static class Dogtag
        {
            private static Rectangle2D rect = World.LostLandsWrap;
            public static Point3D Get(string name)
            {
                int hash = Math.Abs(GetStableHashCode(name, version: 1));
                int row = hash % rect.Width + rect.Start.X;
                int col = hash % rect.Height + rect.Start.Y;
                return new Point3D(row, col, 0);
            }
            public static void Test()
            {
                for (int ix = 0; ix < 100000; ix++)
                {
                    Point3D px = Get(ix.ToString());
                    //Console.WriteLine("{0}", px);
                    if (rect.Contains(new Point2D(px.X, px.Y)))
                        continue;
                    else
                        Console.WriteLine("DogTag Test failed at {0}", px);
                }
            }
        }
        #endregion DogTag
        #region RunUO2.6 Port compatibility functions
        public static void RandomBytes(byte[] buffer)
        {
            //RandomImpl.NextBytes(buffer);
            ConsoleWriteLine("RandomBytes(byte[] buffer) not implemented", ConsoleColor.Red);
        }
        public static int GetObjectZ(object o)
        {
            if (o is LandTile)
                return ((LandTile)o).Z;

            if (o is StaticTile)
                return ((StaticTile)o).Z;

            if (o is Item)
                return ((Item)o).Z;

            return 0;
        }
        public static int GetObjectID(object o)
        {
            if (o is LandTile)
                return ((LandTile)o).ID;

            if (o is StaticTile)
                return ((StaticTile)o).ID;

            if (o is Item)
                return ((Item)o).ItemID;

            return 0;
        }
        [Flags]
        public enum CanFitFlags
        {
            none = 0x0000,
            checkBlocksFit = 0x0001,
            checkMobiles = 0x0002,
            requireSurface = 0x0004,
            ignoreDeadMobiles = 0x0008,
            canSwim = 0x0010,
            cantWalk = 0x0020,
        }
        const CanFitFlags FitFlagsDefault = CanFitFlags.checkMobiles | CanFitFlags.requireSurface;
        public static bool IsValidWater(Map map, int x, int y, int z)
        {
            Region region;
            if (map == null || (region = Region.Find(new Point3D(x, y, z), map)) == null)
                return false;

            if (!region.AllowSpawn() || !map.CanFit(x, y, z, 16, false, true, false))
                return false;

            return IsWater(map, x, y, z);
        }
        public static bool IsWater(Map map, int x, int y, int z)
        {
            LandTile landTile = map.Tiles.GetLandTile(x, y);

            if (landTile.Z == z && (TileData.LandTable[landTile.ID & TileData.MaxLandValue].Flags & TileFlag.Wet) != 0)
                return true;

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                StaticTile staticTile = staticTiles[i];

                if (staticTile.Z == z && (TileData.ItemTable[staticTile.ID & TileData.MaxItemValue].Flags & TileFlag.Wet) != 0)
                    return true;
            }

            return false;
        }
        public static bool CanSpawnWaterMobile(Map map, Point3D p)
        {
            return CanSpawnWaterMobile(map, p, CanFitFlags.none);
        }
        public static bool CanSpawnWaterMobile(Map map, Point3D p, CanFitFlags flags)
        {   // currently we do no processing of flags
            return IsValidWater(map, p.X, p.Y, p.Z);
        }
        public static bool CanSpawnLandMobile(Map map, Point3D p)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, p.X, p.Y, p.Z, FitFlagsDefault);
        }
        public static bool CanSpawnLandMobile(Map map, Point3D p, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, p.X, p.Y, p.Z, flags);
        }
        public static bool CanSpawnLandMobile(Map map, Point2D p, int z)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, p.X, p.Y, z, FitFlagsDefault);
        }
        public static bool CanSpawnLandMobile(Map map, Point2D p, int z, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, p.X, p.Y, z, flags);
        }
        public static bool CanSpawnLandMobile(Map map, int x, int y, int z)
        {
            if (map == null || map == Map.Internal) return false;
            return CanSpawnLandMobile(map, new Point3D(x, y, z), FitFlagsDefault);
        }
        public static bool CanSpawnLandMobile(Map map, int x, int y, int z, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            if (!Region.Find(new Point3D(x, y, z), map).AllowSpawn())
                return false;

            return CanFit(map, x, y, z, 16, flags);
        }

        public static bool CanFit(Map map, Point2D p, int z, int height)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, p.X, p.Y, z, height, FitFlagsDefault);
        }
        public static bool CanFit(Map map, Point3D p, int height)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, p.X, p.Y, p.Z, height, FitFlagsDefault);
        }
        public static bool CanFit(Map map, Point2D p, int z, int height, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, p.X, p.Y, z, height, flags);
        }
        public static bool CanFit(Map map, Point3D p, int height, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, p.X, p.Y, p.Z, height, flags);
        }
        public bool CanFit(Map map, int x, int y, int z, int height)
        {
            if (map == null || map == Map.Internal) return false;
            return CanFit(map, x, y, z, height, FitFlagsDefault);
        }
        public static bool CanFit(Map map, int x, int y, int z, int height, CanFitFlags flags)
        {
            if (map == null || map == Map.Internal) return false;
            bool checkBlocksFit = (flags & CanFitFlags.checkBlocksFit) != 0;
            bool checkMobiles = (flags & CanFitFlags.checkMobiles) != 0;
            bool requireSurface = (flags & CanFitFlags.requireSurface) != 0;
            bool ignoreDeadMobiles = (flags & CanFitFlags.ignoreDeadMobiles) != 0;
            bool canSwim = (flags & CanFitFlags.canSwim) != 0;
            bool cantWalk = (flags & CanFitFlags.cantWalk) != 0;

            if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                return false;

            bool hasSurface = false;

            LandTile lt = map.Tiles.GetLandTile(x, y);
            int lowZ = 0, avgZ = 0, topZ = 0;

            map.GetAverageZ(x, y, ref lowZ, ref avgZ, ref topZ);
            TileFlag landFlags = TileData.LandTable[lt.ID & 0x3FFF].Flags;

            if ((landFlags & TileFlag.Impassable) != 0 && avgZ > z && (z + height) > lowZ)
                return false;
            // can walk on land
            else if (!cantWalk && (landFlags & TileFlag.Impassable) == 0 && z == avgZ && !lt.Ignored)
                hasSurface = true;
            // can swim in water
            else if (canSwim && (landFlags & TileFlag.Impassable) != 0 && (landFlags & TileFlag.Wet) != 0 && z == avgZ && !lt.Ignored)
                hasSurface = true;

            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, true);

            bool surface, impassable;

            for (int i = 0; i < staticTiles.Length; ++i)
            {
                ItemData id = TileData.ItemTable[staticTiles[i].ID & 0x3FFF];
                surface = id.Surface;
                impassable = id.Impassable;

                if ((surface || impassable) && (staticTiles[i].Z + id.CalcHeight) > z && (z + height) > staticTiles[i].Z)
                    return false;
                else if (surface && !impassable && z == (staticTiles[i].Z + id.CalcHeight))
                    hasSurface = true;
            }

            Sector sector = map.GetSector(x, y);
            //Dictionary<Server.Serial, object>.ValueCollection items = sector.Items.Values, mobs = sector.Mobiles.Values;

            foreach (Item item in sector.Items)
            {
                if (item == null)
                    continue;

                if (item.ItemID < 0x4000 && item.AtWorldPoint(x, y))
                {
                    ItemData id = item.ItemData;
                    surface = id.Surface;
                    impassable = id.Impassable;

                    if ((surface || impassable || (checkBlocksFit && item.BlocksFit)) && (item.Z + id.CalcHeight) > z && (z + height) > item.Z)
                        return false;
                    else if (surface && !impassable && z == (item.Z + id.CalcHeight))
                        hasSurface = true;
                }
            }

            if (checkMobiles)
            {
                foreach (Mobile m in sector.Mobiles)
                {
                    if (m == null)
                        continue;

                    if (m.Alive == true || ignoreDeadMobiles == false)
                    {
                        if (m.Location.m_X == x && m.Location.m_Y == y)
                        {
                            if ((m.Z + 16) > z && (z + height) > m.Z)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            return !requireSurface || hasSurface;
        }
        public static bool LineOfSight(Map map, object from, object dest, bool strict)
        {
            if (from == dest || ((from is Mobile && ((Mobile)from).AccessLevel > AccessLevel.Player) && strict == false))
                return true;

            else if (dest is Item && from is Mobile && ((Item)dest).RootParent == from)
                return true;

            return map.LineOfSight(map.GetPoint(from, true), map.GetPoint(dest, false));
        }
        #endregion RunUO2.6 Port compatibility functions
        #region AI Special Mobiles and Items
        public static XmlDocument OpenStandardObjects()
        {
            string filePath = Path.Combine(Core.DataDirectory, "StandardObjects.xml");
            if (System.IO.File.Exists(filePath) == false)
            {
                Core.LoggerShortcuts.BootError(string.Format("Error while reading StandardObjects from \"{0}\".", Path.Combine(Core.DataDirectory, "StandardObjects.xml")));
                return null;
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);
            return doc;
        }
        public static bool StandardCreature(XmlDocument doc, string text /* "Server.Mobiles.Cat"*/)
        {
            XmlElement root = doc["category"];

            foreach (var obj in root.GetElementsByTagName("category"))
            {
                if (!obj.GetType().Equals(typeof(XmlElement)))
                    // filter comments
                    continue;

                XmlElement category = (XmlElement)obj;

                string mobiles = category.GetAttribute("title");
                if (mobiles != "Mobiles")
                    continue;
                else
                {
                    foreach (var obj2 in category.GetElementsByTagName("category"))
                    {
                        if (!obj2.GetType().Equals(typeof(XmlElement)))
                            // filter comments
                            continue;

                        XmlElement animals = (XmlElement)obj2;

                        if (animals.GetAttribute("title").ToLower() == "Age of Shadows".ToLower())
                        {   // nope, don't want any of these
                            continue;
                        }
                        else if (animals.GetAttribute("title").ToLower() == "Good".ToLower())
                        {   // exclude most "Good" creatures
                            int[] valid = { 6 }; // only the wisp is valid
                            int count = 0;
                            foreach (var obj3 in animals.ChildNodes)
                            {
                                if (!obj3.GetType().Equals(typeof(XmlElement)))
                                    // filter comments
                                    continue;

                                XmlElement node = (XmlElement)obj3;

                                if (valid.Contains(count) && node.GetAttribute("type").ToLower().Contains(text.ToLower()))
                                    return true; // found it

                                count++;
                            }

                            continue;
                        }
                        else if (animals.GetAttribute("title").ToLower() == "Evil".ToLower())
                        {   // exclude some "Evil" creatures
                            foreach (var obj4 in animals.GetElementsByTagName("category"))
                            {
                                if (!obj4.GetType().Equals(typeof(XmlElement)))
                                    // filter comments
                                    continue;

                                XmlElement evil = (XmlElement)obj4;

                                if (evil.GetAttribute("title").ToLower() == "Age of Shadows".ToLower())
                                {   // nope, don't want any of these
                                    continue;
                                }

                                foreach (var obj5 in evil.ChildNodes)
                                {
                                    if (!obj5.GetType().Equals(typeof(XmlElement)))
                                        // filter comments
                                        continue;

                                    XmlElement node = (XmlElement)obj5;

                                    if (node.GetAttribute("type").ToLower().Contains(text.ToLower()))
                                        return true; // found it

                                }
                            }

                            continue;
                        }
                        else
                        {
                            foreach (var obj6 in animals.ChildNodes)
                            {
                                if (!obj6.GetType().Equals(typeof(XmlElement)))
                                    // filter comments
                                    continue;

                                XmlElement node = (XmlElement)obj6;

                                if (node.GetAttribute("type").ToLower().Contains(text.ToLower()))
                                    return true; // found it
                            }
                        }
                    }
                }
            }

            return false;
        }
        public static bool StandardItem(XmlDocument doc, Type type /*type.FullName =  "Server.Items.Axle"*/, List<string> aliases)
        {
            string text = type.FullName;
            int itemID = 0;
            if (HasParameterlessConstructor(type))
                itemID = GetItemID(type);

            XmlElement root = doc["category"];

            foreach (var obj1 in root.GetElementsByTagName("category"))
            {
                if (!obj1.GetType().Equals(typeof(XmlElement)))
                    // filter comments
                    continue;

                XmlElement category = (XmlElement)obj1;

                string items = category.GetAttribute("title");
                if (items != "Items")
                    continue;
                else
                    foreach (var obj2 in category.GetElementsByTagName("category"))
                    {
                        if (!obj2.GetType().Equals(typeof(XmlElement)))
                            // filter comments
                            continue;

                        XmlElement things = (XmlElement)obj2;

                        string found = things.GetAttribute("title");
                        if (things.ChildNodes == null || things.ChildNodes.Count == 0)
                        {
                            // nope, don't want any of these
                            continue;
                        }
                        else
                            foreach (var obj3 in things.ChildNodes)
                            {
                                if (!obj3.GetType().Equals(typeof(XmlElement)))
                                    // filter comments
                                    continue;

                                XmlElement node = (XmlElement)obj3;

                                string nodeText = node.GetAttribute("type");
                                int gfx = 0;
                                int.TryParse(node.GetAttribute("gfx"), out gfx);
                                if (nodeText.ToLower().Contains(text.ToLower()) && (itemID == 0 || itemID == gfx))
                                {
                                    return true; // found it
                                }
                                else
                                {   // if we didn't find it, but the item ids match, record it.
                                    if (!string.IsNullOrEmpty(nodeText))
                                    {
                                        Type typeFound = ScriptCompiler.FindTypeByName(nodeText);
                                        if (typeFound != null)
                                            if (HasParameterlessConstructor(typeFound))
                                                if (GetItemID(typeFound) == itemID)
                                                    aliases.Add(string.Format("ItemID: {0}, Display Name: {1}", itemID, GetDisplayName(type)));
                                    }
                                }
                            }
                    }
            }

            return false;
        }
        private static string GetDisplayName(Type type)
        {
            string displayName = string.Empty;
            object o = Activator.CreateInstance(type);
            if (o is Item item)
            {
                displayName = GetObjectDisplayName(o, type);
                item.Delete();
            }
            return displayName;
        }
        private static int GetItemID(Type type)
        {
            int itemid = 0;
            object o = Activator.CreateInstance(type);
            if (o is Item item)
            {
                itemid = item.ItemID;
                item.Delete();
            }

            return itemid;
        }
        #endregion AI Special Mobiles and Items
        public static string[] GetCustomEnumNames(Type type)
        {
            object[] attrs = type.GetCustomAttributes(typeof(CustomEnumAttribute), false);

            if (attrs.Length == 0)
                return new string[0];

            CustomEnumAttribute ce = attrs[0] as CustomEnumAttribute;

            if (ce == null)
                return new string[0];

            return ce.Names;
        }
        public static class TeleporterRuleHelpers
        {
            // Britain
            public static bool FromBrit(Teleporter teleporter)
            { return World.BritMainLandWrap.Contains(teleporter.Location) || World.Dungeons.Contains(teleporter.Location) && teleporter.Map == Map.Felucca; }
            public static bool ToBrit(Teleporter teleporter)
            { return World.BritMainLandWrap.Contains(teleporter.PointDest) || World.Dungeons.Contains(teleporter.PointDest) && teleporter.MapDest == Map.Felucca; }

            // Lost Lands
            public static bool FromLostLands(Teleporter teleporter)
            { return World.LostLandsWrap.Contains(teleporter.Location) && teleporter.Map == Map.Felucca; }
            public static bool ToLostLands(Teleporter teleporter)
            { return World.LostLandsWrap.Contains(teleporter.PointDest) && teleporter.MapDest == Map.Felucca; }

            // "Terathan Keep"
            public static bool FromTerathanKeep(Teleporter teleporter)
            { return InRegion(teleporter.Location, teleporter.Map, "Terathan Keep") && teleporter.Map == Map.Felucca; }
            public static bool ToTerathanKeep(Teleporter teleporter)
            { return InRegion(teleporter.PointDest, teleporter.Map, "Terathan Keep") && teleporter.MapDest == Map.Felucca; }

            // "Khaldun"
            public static bool FromKhaldun(Teleporter teleporter)
            { return InRegion(teleporter.Location, teleporter.Map, "Khaldun") && teleporter.Map == Map.Felucca; }
            public static bool ToKhaldun(Teleporter teleporter)
            { return InRegion(teleporter.PointDest, teleporter.Map, "Khaldun") && teleporter.MapDest == Map.Felucca; }

            public static bool InRegion(Point3D px, Map map, string regionName)
            {
                ArrayList list = Region.FindAll(px, map);
                foreach (object o in list)
                    if (o is Region region)
                        if (!string.IsNullOrEmpty(region.Name))
                            if (region.Name.ToLower() == regionName.ToLower())
                                return true;
                return false;
            }
        }
        #region WipeRect
        [Flags]
        public enum WipeRectFlags
        {
            None = 0x00,
            Items = 0x01,
            Mobiles = 0x02,
            All = Items | Mobiles,
        }
        [Usage("WipeRect")]
        [Description("Wipes the rectangle of items and/or Mobiles.")]
        public static int WipeRect(Map map, Rectangle2D rect,
            int startingZ = 0, WipeRectFlags flags = WipeRectFlags.All,
            Type[] excludeByType = null, int[] excludeByID = null, int lenientZ = int.MinValue, string logFile = null)
        {
            if (lenientZ == int.MinValue)
                lenientZ = CalcZSlop(map, rect, startingZ, flags);

            IPooledEnumerable eable = map.GetObjectsInBounds(rect);
            List<object> list = new();
            foreach (object ix in eable)
            {   // list of objects to delete
                if ((flags & WipeRectFlags.Items) != 0)
                {
                    if (ix is Item item)
                        if (item != null && !item.Deleted && rect.Contains(item))
                            if (!ExcludeThisType(item, excludeByType) && !ExcludeThisID(item, excludeByID))
                                if (Math.Abs(item.Z - startingZ) <= lenientZ)
                                    list.Add(item);
                                else
                                    ; // debug break
                }
                else if ((flags & WipeRectFlags.Mobiles) != 0)
                {
                    if (ix is Mobile mob)
                        if (mob != null && !mob.Deleted && rect.Contains(mob))
                            if (!ExcludeThisType(mob, excludeByType) && !ExcludeThisID(mob, excludeByID))
                                if (Math.Abs(mob.Z - startingZ) <= lenientZ)
                                    list.Add(mob);
                }
            }
            eable.Free();

            foreach (object ix in list)
            {   // log in our standard cfg format
                WriteObjectCfg(ix, logFile, false);

                if (ix is Item item)
                    item.Delete();
                else if (ix is Mobile mob)
                    mob.Delete();
            }

            return list.Count;
        }
        public static int CalcZSlop(Map map, Rectangle2D rect, int startingZ, WipeRectFlags flags = WipeRectFlags.All)
        {   // see how many Zs we can include before changing 'floors'

            int floor = startingZ;
            int ceiling = floor;
            IPooledEnumerable eable = map.GetObjectsInBounds(rect);
            List<IEntity> list = new();
            foreach (object o in eable) { list.Add((IEntity)o); }
            eable.Free();
            foreach (IEntity o in list)
            {
                if (o is Mobile && (flags & WipeRectFlags.Mobiles) == 0)
                    continue;
                if (o is Item && (flags & WipeRectFlags.Items) == 0)
                    continue;

                int hc = HardCeiling(map, o.X, o.Y, startingZ);
                int hf = HardFloor(map, o.X, o.Y, startingZ);

                if (o.Z < hc)
                    if (o.Z > ceiling)
                        ceiling = o.Z;

                if (o.Z >= hf)
                    if (o.Z <= floor)
                        floor = o.Z;
            }

            return Math.Abs(floor - ceiling);
        }
        public static int HardCeiling(Map map, int x, int y, int z)
        {
            List<int> hardZBreaks = HardZBreaks(map, x, y, z);
            ;
            ;
            int stop = z;
            foreach (int ctr in hardZBreaks)
            {
                if (ctr > z)
                    return ctr;
            }
            ;
            // there is no ceiling here, so we are inclusive of this z (an all above)
            //return stop >= 0 ? ++stop : --stop;
            return stop >= 0 ? short.MaxValue : short.MinValue;
            //return stop + 1;
        }
        public static int HardFloor(Map map, int x, int y, int z)
        {
            List<int> hardZBreaks = HardZBreaks(map, x, y, z);
            ;
            ;
            int stop = z;
            foreach (int ctr in hardZBreaks)
            {
                if (ctr <= z)
                    return ctr;
            }
            ;
            // there is no floor here, so we are inclusive of this z
            return stop;
        }
        public static List<int> HardZBreaks(Map map, int x, int y, int z)
        {
            List<int> list = new();
            StaticTile[] staticTiles = map.Tiles.GetStaticTiles(x, y, true);
            foreach (StaticTile st in staticTiles) { list.Add(st.Z); }
            list.Add(map.Tiles.GetLandTile(x, y).Z);
            list.Sort();
            return list;
        }
        public static int GetAverageZ(Map map, int x, int y)
        {
            int landZ = 0, landAvg = 0, landTop = 0;
            map.GetAverageZ(x, y, ref landZ, ref landAvg, ref landTop);
            return landAvg;
        }
        private static bool ExcludeThisID(object obj, int[] exclude)
        {
            if (obj != null && exclude != null)
            {
                if (obj is Mobile mob)
                {
                    ErrorOut(string.Format("Warning: Unsure what the 'ID' of a mobile should be. Using BodyValue {0}.", mob.BodyValue), ConsoleColor.Red);
                    foreach (int type in exclude)
                        if (mob.BodyValue == type)
                            return true;
                }
                else if (obj is Item item)
                {
                    foreach (int type in exclude)
                        if (item.ItemID == type)
                            return true;
                }
            }

            return false;
        }
        private static bool ExcludeThisType(object obj, Type[] exclude)
        {
            if (obj != null && exclude != null)
            {
                if (obj is Mobile mob)
                {
                    foreach (Type type in exclude)
                        if (type.IsAssignableFrom(mob.GetType()))
                            return true;
                }
                else if (obj is Item item)
                {
                    foreach (Type type in exclude)
                        if (type.IsAssignableFrom(item.GetType()))
                            return true;
                }
            }

            return false;
        }
        #endregion WipeRect
        public static void WriteObjectCfg(object o, string fileName, bool overwrite)
        {
            if (o == null || fileName == null)
                return;

            LogHelper logger = null;
            logger = new LogHelper(fileName, overwrite, true, true);
            if (o is Mobile mob)
            {
                logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}",
                    mob.Serial.Value, mob.X, mob.Y, mob.Z, mob.Map
                    ));
            }
            else if (o is Item item)
            {
                logger.Log(LogType.Text, string.Format("{0} {1} {2} {3} {4}",
                    item.Serial.Value, item.X, item.Y, item.Z, item.Map
                    ));
            }
            logger.Finish();
        }
        public static int RefreshDeco(Map map, List<Rectangle2D> rects,
            int startingZ = 0, Type[] typeExclude = null, int[] idExclude = null, int lenientZ = 0)
        {
            int count = 0;
            if (rects == null)
                return count;

            foreach (Rectangle2D rect in rects)
                Utility.WipeRect(map, rect, startingZ, WipeRectFlags.Items, typeExclude, idExclude, lenientZ);

            // Felucca
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Felucca"), DecoMode.add, file: "*.cfg", listOnly: false,
                types: null, boundingRect: rects, exclude: typeExclude, maps: new Map[] { map });

            // Britannia
            Generate(Path.Combine(Core.DataDirectory, "Decoration/Britannia"), DecoMode.add, file: "*.cfg", listOnly: false,
                types: null, boundingRect: rects, exclude: typeExclude, maps: new Map[] { map });

            // count up the changes
            foreach (Item item in Server.World.Items.Values)
                if (item != null && item.Deleted == false && item.Map == map)
                    foreach (Rectangle2D rect in rects)
                        if (rect.Contains(item))
                            if (typeExclude == null || !Array.Exists(typeExclude, element => element == item.GetType()))
                                count++;
                            else
                                ;

            // Debug break point
            return count;
        }
        public static int CountItems(Map map, Rectangle2D rect)
        {
            int count = 0;
            IPooledEnumerable eable = map.GetItemsInBounds(rect);
            foreach (Item item in eable)
                if (item is Item && item.Deleted == false)
                    count++;
            eable.Free();
            return count;
        }
        public static int[] IntParser(string value)
        {
            List<int> list = new();
            try
            {
                value = Regex.Replace(value, "[^0-9-]", " ");
                string[] tokens = value.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                for (int ix = 0; ix < tokens.Length; ix++)
                    list.Add(Convert.ToInt32(tokens[ix]));

                return list.ToArray();
            }
            catch
            {
                return list.ToArray();
            }
        }
        #region Find Item
        public static List<Item> FindItemsAt(Point3D px, Map map, int lenientZ = 0)
        {
            List<Item> list = new();
            Point3D target = px;
            IPooledEnumerable eable = Map.Felucca.GetItemsInRange(target, 0);
            foreach (Item item in eable)
                if (item != null && !item.Deleted && item.Map == map)
                    if (Math.Abs(item.Z - px.Z) <= lenientZ)
                        list.Add(item);
            eable.Free();

            return list;
        }
        public static List<Item> FindItemAt(Point3D px, Map map, Type type, int lenientZ = 0)
        {
            List<Item> list = new();
            Point3D target = px;
            IPooledEnumerable eable = map.GetItemsInRange(target, 0);
            foreach (Item item in eable)
                if (item != null && !item.Deleted && item.Map == map)
                    if (type.IsAssignableFrom(item.GetType()) && Math.Abs(item.Z - px.Z) <= lenientZ)
                        list.Add(item);
            eable.Free();

            return list;
        }
        public static Item FindOneItemAt(Point3D px, Map map, Type type, int lenientZ = 0, bool warnMulti = true)
        {
            List<Item> list = FindItemAt(px, map, type, lenientZ);
            if (list.Count > 1 && warnMulti)
                Utility.ConsoleWriteLine(string.Format("Logic Error: Found more than one {0} where expected {1}. ", type.Name, px), ConsoleColor.Red);
            if (list.Count > 0)
                return list[0];
            return null;
        }
        public static List<Item> FindItemAt(Point3D px, Map map, int ItemID, int lenientZ = 0)
        {
            List<Item> list = new();
            Point3D target = px;
            IPooledEnumerable eable = Map.Felucca.GetItemsInRange(target, 0);
            foreach (Item item in eable)
                if (item != null && !item.Deleted && item.Map == map)
                    if (item.ItemID == ItemID && Math.Abs(item.Z - px.Z) <= lenientZ)
                        list.Add(item);
            eable.Free();

            return list;
        }
        public static Item FindOneItemAt(Point3D px, Map map, int ItemID, int lenientZ = 0, bool warnMulti = true)
        {
            List<Item> list = FindItemAt(px, map, ItemID, lenientZ);
            if (list.Count > 1 && warnMulti)
                Utility.ConsoleWriteLine(string.Format("Logic Error: Found more than one {0:X} where expected {1}. ", ItemID, px), ConsoleColor.Red);
            if (list.Count > 0)
                return list[0];
            return null;
        }
        #endregion Find Item
        #region Remap Staff Commands
        /// <summary>
        /// In order to protect the integrity of our map, We no longer allow staff to create Spawners, Moongates, or Teleporters.
        /// We remap these particular items to their 'Event' equivalents which are limited in duration.
        /// Note: we may add provisions for allowing staff to build these things in some 'world building mode'.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="args"></param>
        public static bool AccessLevelRemap(Mobile from, string[] args)
        {
            if (from == null || from.AccessLevel == AccessLevel.Owner)
                return false;

            // not for the production world.
            //if (Core.UOTC_CFG || Core.Debug)
            //    return false;

            return ItemRemap(args);
        }
        public static bool ItemRemap(string[] args)
        {
            bool remapped = false;
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].ToLower() == "spawner")
                    { args[i] = "EventSpawner"; remapped = true; }
                    if (args[i].ToLower() == "moongate")
                    { args[i] = "EventSungate"; remapped = true; }
                    if (args[i].ToLower() == "teleporter")
                    { args[i] = "EventTeleporter"; remapped = true; }
                    if (args[i].ToLower() == "confirmationmoongate")
                    { args[i] = "EventConfirmationSungate"; remapped = true; }
                }

            return remapped;
        }
        #endregion Remap Staff Commands

        public static void Initialize()
        {
            Server.CommandSystem.Register("SpawnableRect", AccessLevel.Owner, new CommandEventHandler(SpawnableRect_OnCommand));
            Server.CommandSystem.Register("ConvertMCToStandard", AccessLevel.Owner, new CommandEventHandler(ConvertMCToStandard_OnCommand));
        }
        #region ConvertMCToStandard
        [Usage("ConvertMCToStandard")]
        [Description("Converts an array of MovingCrates to standard crates.")]
        public static void ConvertMCToStandard_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Select the object area to dump...");
            BoundingBoxPicker.Begin(e.Mobile, new BoundingBoxCallback(ConvertArray_Callback), null);
        }
        private static void ConvertArray_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {
            string filename = state as string;

            // Create rec and retrieve items within from bounding box callback result
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);

            // locals
            try
            {
                // See what we got
                List<Item> WhatWeGot = new();                   // surface items we found
                #region WhatWeGot
                IPooledEnumerable eable = map.GetItemsInBounds(rect);
                foreach (object obj in eable)
                {
                    if (obj is Mobiles.PlayerMobile) continue;  // ignore the <target> mobile (staff member)
                    if (obj is Mobile) continue;                // for now, ignore all mobiles
                    if (obj is BaseHouse) continue;
                    if (obj is Item old_item)
                        WhatWeGot.Add(old_item);
                }
                eable.Free();
                #endregion WhatWeGot

                #region Check
                {
                    foreach (Item item in WhatWeGot)
                        if (item is not MovingCrate)
                        {
                            from.SendMessage("{0} is not a moving crate", item);
                            return;
                        }
                }
                #endregion Check

                #region Process
                {
                    foreach (MovingCrate mc in WhatWeGot)
                    {
                        List<Item> temp = new();
                        LargeCrate lc = new();
                        Utility.CopyPropertyIntersection(lc, mc);
                        lc.Name += " " + mc.Label;
                        from.SendMessage("Moving {0} items from {1} to {2}...", mc.Items.Count, mc, lc);
                        int intended = mc.Items.Count;
                        foreach (Item item in mc.Items)
                            temp.Add(item);

                        foreach (Item item in temp)
                        {
                            mc.RemoveItem(item);
                            lc.AddItem(item);
                        }
                        Debug.Assert(mc.Deleted);
                        Debug.Assert(lc.Items.Count == intended);
                        from.SendMessage("{0} items moved from {1} to {2}...", lc.Items.Count, mc, lc);
                        lc.UpdateAllTotals();
                        lc.MoveToWorld(lc.Location, lc.Map);
                    }
                }
                #endregion Process
            }
            catch
            {
                ;
            }
        }
        #endregion

        #region SpawnableRect
        [Usage("SpawnableRect")]
        [Description("Tests our SpawnableRect.")]
        public static void SpawnableRect_OnCommand(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Target the center of the rect to grow.");
            e.Mobile.Target = new SpawnableRectTarget(null);
        }
        public class SpawnableRectTarget : Target
        {
            public SpawnableRectTarget(object o)
                : base(12, true, TargetFlags.None)
            {

            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                LandTarget tl = targeted as LandTarget;
                Item it = targeted as Item;
                Mobile mt = targeted as Mobile;
                StaticTarget st = targeted as StaticTarget;
                Point3D loc = (tl != null) ? tl.Location : (mt != null) ? mt.Location : (it != null) ? it.Location : (st != null) ? st.Location : Point3D.Zero;

                if (loc != Point3D.Zero)
                {
                    Rectangle2D rect = new Rectangle2D(loc, new Point2D(loc.X + 1, loc.Y + 1));
                    rect = new SpawnableRect(rect).MaximumSpawnableRect(from.Map, 128, from.Map.GetAverageZ(rect.X, rect.Y));
                    from.SendMessage("Your max spawnable rect is {0}.", rect);
                }
                else
                    from.SendMessage("Can't grow a rect here.");
            }
        }
        public class SpawnableRect
        {
            Rectangle2D m_rect;
            public SpawnableRect(Rectangle2D rectToGrow)
            {
                m_rect = rectToGrow;
            }
            /// <summary>
            /// Grow the given rectangle in all directions in which a mobile may be spawned.
            /// Use caution: You should only call this in a closed area, otherwise your rectangle can grow quite large.
            /// </summary>
            /// <param name="map"></param>
            public Rectangle2D MaximumSpawnableRect(Map map, int max, int averageZ)
            {

                Rectangle2D change = new Rectangle2D();
                // we need to start without width(1) and height(1) because it will put us over our max
                Rectangle2D orig = new Rectangle2D(m_rect.X, m_rect.Y, Math.Min(m_rect.Width, 0), Math.Min(m_rect.Height, 0));
                do
                {   // we will compare the old and new rect and see if it changed.
                    change = m_rect;
                    // let's start with the right side of the rect.
                    //  We will walk down the Y axis pushing out to the right X if we can spawn a mobile there.
                    if (!HitMax(orig, max))
                        for (int newY = m_rect.Start.Y; newY < m_rect.End.Y; newY++)
                        {
                            for (int X = m_rect.End.X; ; X++)
                            {
                                Rectangle2D bump = new Rectangle2D(m_rect.Start, m_rect.End);
                                if (Utility.CanSpawnLandMobile(map, X, newY, averageZ))
                                {
                                    m_rect.End = new Point2D(X, m_rect.End.Y);
                                    if (!m_rect.Equals(bump))
                                        goto LeftSide;
                                }
                                else
                                    break;  // try the next Y
                            }
                        }
                    // now we will extend the left side
                    LeftSide:
                    if (!HitMax(orig, max))
                        for (int newY = m_rect.Start.Y; newY < m_rect.End.Y; newY++)
                        {
                            for (int X = m_rect.Start.X; ; X--)
                            {
                                Rectangle2D bump = new Rectangle2D(m_rect.Start, m_rect.End);
                                if (Utility.CanSpawnLandMobile(map, X, newY, averageZ))
                                {
                                    m_rect.Start = new Point2D(X, m_rect.Start.Y);
                                    if (!m_rect.Equals(bump))
                                        goto Top;
                                }
                                else
                                    break;  // try the next Y
                            }
                        }
                    // now we will extend the top 
                    Top:
                    if (!HitMax(orig, max))
                        for (int newX = m_rect.Start.X; newX < m_rect.End.X; newX++)
                        {
                            for (int Y = m_rect.Start.Y; ; Y--)
                            {
                                Rectangle2D bump = new Rectangle2D(m_rect.Start, m_rect.End);
                                if (Utility.CanSpawnLandMobile(map, newX, Y, averageZ))
                                {
                                    m_rect.Start = new Point2D(m_rect.Start.X, Y);
                                    if (!m_rect.Equals(bump))
                                        goto Bottom;
                                }

                                else
                                    break;  // try the next X
                            }
                        }
                    // now we will extend the bottom
                    Bottom:
                    if (!HitMax(orig, max))
                        for (int newX = m_rect.Start.X; newX < m_rect.End.X; newX++)
                        {
                            for (int Y = m_rect.End.Y; ; Y++)
                            {
                                Rectangle2D bump = new Rectangle2D(m_rect.Start, m_rect.End);
                                if (Utility.CanSpawnLandMobile(map, newX, Y, averageZ))
                                {
                                    m_rect.End = new Point2D(m_rect.End.X, Y);
                                    if (!m_rect.Equals(bump))
                                        goto Loop;
                                }
                                else
                                    break;  // try the next X
                            }
                        }
                    Loop:;

                    // if the rect changed size, say height, then there are new points to check for the left and right.
                    //  keep calling until there is no more growth.
                } while (!change.Equals(m_rect) && !HitMax(orig, max));

                return m_rect;
            }
            private bool HitMax(Rectangle2D orig, int max)
            {
                return (Math.Abs(m_rect.Width - orig.Width) >= max) || (Math.Abs(m_rect.Height - orig.Height) >= max);
            }
        }
        #endregion SpawnableRect
        /// <summary>
        /// Determine if we can spawn a mobile there, and return the Z(s)
        /// </summary>
        /// <param name="map"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        #region CanSpawnMobile Utilities
        public static List<int> GetHardZs(Map map, Point3D point, bool allowExotics = true)
        {
            // assume the spawner location is a valid z
            List<int> list = new List<int>();
            list.Add(point.Z);

            // get the standards
            int landZ = 0, landAvg = 0, landTop = 0, docks = 0;
            map.GetAverageZ(point.X, point.Y, ref landZ, ref landAvg, ref landTop);

            if (!list.Contains(landZ)) list.Add(landZ);
            if (!list.Contains(landAvg)) list.Add(landAvg);
            if (!list.Contains(landTop)) list.Add(landTop);

            // now for the exotics
            List<int> exotics = new();
            if (allowExotics)
                // see if we can find a Z on another floor
                exotics = Utility.HardZBreaks(map, point.X, point.Y, landAvg);

            foreach (int ix in exotics)
                if (!list.Contains(ix)) list.Add(ix);

            return list;
        }
        public static bool CanSpawnMobile(Map map, int x, int y, ref int z, bool allowExotics = false)
        {
            CanFitFlags flags = Utility.CanFitFlags.requireSurface | Utility.CanFitFlags.canSwim;
            return CanSpawnMobile(map, x, y, ref z, flags, allowExotics);
        }
        public static bool CanSpawnMobile(Map map, int x, int y, ref int z, CanFitFlags flags, bool allowExotics = false, List<int> Zs = null)
        {
            List<int> exotics = new();
            if (allowExotics)
                // see if we can find a Z on another floor
                // public static List<int> HardZBreaks(Map map, int x, int y, int z)
                exotics = Utility.HardZBreaks(map, x, y, Utility.GetAverageZ(map, x, y));

            int landZ = 0, landAvg = 0, landTop = 0, docks = 0;
            map.GetAverageZ(x, y, ref landZ, ref landAvg, ref landTop);

            Point2D px = new Point2D(x, y);
            if (Zs == null)
                Zs = new List<int>();

            // try the original location first
            if (CanSpawnLandMobile(map, px, z, flags))
            {
                if (!Zs.Contains(z))
                    Zs.Add(z);
            }

            if (CanSpawnLandMobile(map, px, landZ, flags))
            {
                z = landZ;
                if (!Zs.Contains(z))
                    Zs.Add(z);
            }

            if (CanSpawnLandMobile(map, px, landAvg, flags))
            {
                z = landAvg;
                if (!Zs.Contains(z))
                    Zs.Add(z);
            }

            if (CanSpawnLandMobile(map, px, landTop, flags))
            {
                z = landTop;
                if (!Zs.Contains(z))
                    Zs.Add(z);
            }

            // check the docks
            for (docks = -1; docks > -4; docks--)
                if (CanSpawnLandMobile(map, px, docks, flags))
                {
                    z = docks;
                    if (!Zs.Contains(z))
                        Zs.Add(z);
                }

            // now for the exotics
            if (allowExotics)
                foreach (int Z in exotics)
                    if (CanSpawnLandMobile(map, x, y, Z))
                    {
                        z = Z;
                        if (!Zs.Contains(z))
                            Zs.Add(z);
                    }

            // return the first one found, which should be the best / most normal
            if (Zs.Count > 0)
                z = Zs[0];

            #region Finish
            var Zlist = from num in Zs
                        orderby num descending
                        select num;

            foreach (int ix in Zlist)
                if (!Zs.Contains(ix))
                    Zs.Add(ix);
            #endregion Finish

            return Zs.Count > 0;
        }
        #endregion CanSpawnMobile Utilities
        public static bool IsTownVendor(Mobile m)
        {
            return m != null && m is BaseVendor bv && IsTownRegion(bv.Location, bv.Map == Map.Internal ? Map.Felucca : bv.Map);
        }
        public static bool IsTownRegion(Point3D p, Map map)
        {
            if (map == null)
                return false;

            Sector sector = map.GetSector(p);
            List<RegionRect> list = sector.RegionRects;

            for (int i = 0; i < list.Count; ++i)
            {
                Region region = list[i].Region;

                if (region.Contains(p))
                {
                    if (region is Regions.GuardedRegion)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public static bool IsGuardedRegion(Point3D p, Map map)
        {
            if (map == null)
                return false;

            Sector sector = map.GetSector(p);
            List<RegionRect> list = sector.RegionRects;

            for (int i = 0; i < list.Count; ++i)
            {
                Region region = list[i].Region;

                if (region.Contains(p))
                {
                    if (region is Regions.GuardedRegion && region.IsGuarded)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        #region GetSharedAccounts
        public class AccountComparer : IComparer
        {
            public static readonly IComparer Instance = new AccountComparer();

            public AccountComparer()
            {
            }

            public int Compare(object x, object y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null)
                    return -1;
                else if (y == null)
                    return 1;

                Account a = x as Account;
                Account b = y as Account;

                if (a == null || b == null)
                    throw new ArgumentException();

                AccessLevel aLevel, bLevel;
                bool aOnline, bOnline;

                a.GetAccountInfo(out aLevel, out aOnline);
                b.GetAccountInfo(out bLevel, out bOnline);

                if (aOnline && !bOnline)
                    return -1;
                else if (bOnline && !aOnline)
                    return 1;
                else if (aLevel > bLevel)
                    return -1;
                else if (aLevel < bLevel)
                    return 1;
                else
                    return Insensitive.Compare(a.Username, b.Username);
            }
        }
        private class SharedAccountComparer : IComparer
        {
            public static readonly IComparer Instance = new SharedAccountComparer();

            public SharedAccountComparer()
            {
            }

            public int Compare(object x, object y)
            {
                DictionaryEntry a = (DictionaryEntry)x;
                DictionaryEntry b = (DictionaryEntry)y;

                ArrayList aList = (ArrayList)a.Value;
                ArrayList bList = (ArrayList)b.Value;

                return bList.Count - aList.Count;
            }
        }
        public static ArrayList GetAllSharedAccounts()
        {
            Hashtable table = new Hashtable();
            ArrayList list;

            foreach (Account acct in Accounts.Table.Values)
            {
                IPAddress[] theirAddresses = acct.LoginIPs;

                for (int i = 0; i < theirAddresses.Length; ++i)
                {
                    list = (ArrayList)table[theirAddresses[i]];

                    if (list == null)
                        table[theirAddresses[i]] = list = new ArrayList();

                    list.Add(acct);
                }
            }

            list = new ArrayList(table);

            for (int i = 0; i < list.Count; ++i)
            {
                DictionaryEntry de = (DictionaryEntry)list[i];
                ArrayList accts = (ArrayList)de.Value;

                if (accts.Count == 1)
                    list.RemoveAt(i--);
                else
                    accts.Sort(AccountComparer.Instance);
            }

            list.Sort(SharedAccountComparer.Instance);

            return list;
        }
        public static ArrayList GetSharedAccounts(IPAddress ipAddress)
        {
            ArrayList list = new ArrayList();

            foreach (Account acct in Accounts.Table.Values)
            {
                IPAddress[] theirAddresses = acct.LoginIPs;
                bool contains = false;

                for (int i = 0; !contains && i < theirAddresses.Length; ++i)
                    contains = ipAddress.Equals(theirAddresses[i]);

                if (contains)
                    list.Add(acct);
            }

            list.Sort(AccountComparer.Instance);
            return list;
        }

        public static ArrayList GetSharedAccounts(IPAddress[] ipAddresses)
        {
            ArrayList list = new ArrayList();

            foreach (Account acct in Accounts.Table.Values)
            {
                IPAddress[] theirAddresses = acct.LoginIPs;
                bool contains = false;

                for (int i = 0; !contains && i < theirAddresses.Length; ++i)
                {
                    IPAddress check = theirAddresses[i];

                    for (int j = 0; !contains && j < ipAddresses.Length; ++j)
                        contains = check.Equals(ipAddresses[j]);
                }

                if (contains)
                    list.Add(acct);
            }

            list.Sort(AccountComparer.Instance);
            return list;
        }
        #endregion GetSharedAccounts
        public static int MakeStatic(Rectangle2D rect, Map map)
        {
            List<Item> list = new List<Item>();
            IPooledEnumerable eable = map.GetItemsInBounds(rect);
            foreach (Item item in eable)
                if (item is Item && item.Deleted == false)
                    list.Add(item);

            foreach (Item item in list)
                Utility.ReplaceWithStatic(item);
            return list.Count;
        }
        public static bool ReplaceWithStatic(object targ)
        {
            try
            {
                Item item = Dupe(targ);
                Static sItem = null;
                if (item is Item && targ is Item source)
                {
                    if (item is Addon)
                    {
                        if (item is Addon addon)
                        {
                            List<Item> addonItems = new();
                            foreach (AddonComponent comp in addon.Components)
                                addonItems.Add(Utility.Dupe(comp));

                            foreach (Item item1 in addonItems)
                                ReplaceWithStatic(item1);

                            addon.Delete();
                        }
                    }
                    else
                    {
                        // strip the derived class glop
                        sItem = new Static(source.ItemID);
                        foreach (var prop in typeof(Static).GetProperties())
                        {
                            if (prop.CanRead && prop.CanWrite)
                            {
                                PropertyInfo myPropInfo = typeof(Item).GetProperty(prop.Name);
                                prop.SetValue(sItem, myPropInfo.GetValue(source, null), null);
                            }
                        }
                    }

                    if (sItem != null)
                    {
                        sItem.MoveToWorld(source.Location, source.Map);
                        sItem.Movable = false;
                        item.Delete();
                        source.Delete();
                        return true;
                    }
                    else return false;
                }
                return false;
            }
            catch { return false; }
        }
        #region Dupe
        /*
         * if (TemplateMobile != null)
            {   // dupe the mobile
                Type type = TemplateMobile.GetType();
                object o = null;
                o = Activator.CreateInstance(type);
                Utility.CopyProperties((Mobile)o, TemplateMobile);  // copy properties
                Utility.CopySkills((Mobile)o, TemplateMobile);      // gets stats too
                Utility.WipeLayers((Mobile)o);
                Utility.CopyLayers((Mobile)o, TemplateMobile, movable: false);      // clothes, weapons and whatnot
                new_spawner.TemplateMobile = o as Mobile;
                new_spawner.TemplateMobile.SpawnerTempRefCount = 1;
                if (TemplateMobile.Backpack != null)
                {
                    Backpack pack = (Backpack)Utility.DeepDupe(TemplateMobile.Backpack);
                    Item old_back = new_spawner.TemplateMobile.FindItemOnLayer(Layer.Backpack);
                    if (old_back != null)
                        new_spawner.TemplateMobile.RemoveItem(old_back);

                    pack.Movable = false;
                    new_spawner.TemplateMobile.AddItem(pack);
                }
            }
         */
        public static Mobile Dupe(Mobile src_mobile)
        {
            Mobile old_mobile = src_mobile;
            Type t = old_mobile.GetType();
            Mobile new_mobile = null;
            try
            {
                if (HasParameterlessConstructor(old_mobile.GetType()))
                {
                    new_mobile = old_mobile.Dupe();
                }
                else
                {
                    return new_mobile;
                }
            }
            finally
            {
                ;
            }
            return new_mobile;
        }
        public static Item Dupe(object o)
        {
            if (o is not Item)
                return null;

            Item old_item = (Item)o;
            Type t = old_item.GetType();
            Item new_item = null;
            try
            {
                if (HasParameterlessConstructor(old_item.GetType()))
                {   // InformedDupe: The object has a dupe() override - use it
                    if (ItemOverrides(old_item, method_name: "Dupe", param_types: new Type[] { typeof(int) }))
                    {
                        if ((new_item = InformedDupe(old_item)) != null)
                            return new_item;
                    }
                    // RawDupe: The object does not have a dupe() override - use default
                    if ((new_item = RawDupe(old_item)) != null)
                        return new_item;
                }
                else
                {
                    // if this type overrides Dupe(), use it!
                    if (ItemOverrides(old_item, method_name: "Dupe", param_types: new Type[] { typeof(int) }))
                        return (new_item = old_item.Dupe(1));

                    new_item = ParamDupe(old_item.GetType(), old_item);
                    System.Diagnostics.Debug.Assert(new_item != null);
                    return new_item;
                }
            }
            finally
            {
                if (new_item != null)
                {
                    if (new_item is Container cont)
                        cont.UpdateAllTotals();
                    else
                        new_item.UpdateTotals();
                }
            }
            return null;
        }
        public static bool ItemOverrides(Item item, string method_name, Type[] param_types)
        {
            return (item.GetType().GetMethod(method_name, BindingFlags.Public | BindingFlags.Instance,
                    null,
                    CallingConventions.Any,
                    param_types,
                    null).DeclaringType == item.GetType());
        }
        private static Item InformedDupe(Item old_item)
        {
            if (old_item == null) return null;
            try
            {
                Item new_item = old_item.Dupe(1);           // call the native duping mechanic
                if (new_item != null)
                {                                           // no native dupe, revert to Raw dupe
                    CopyProperties(new_item, old_item);     // now get the properties
                    return new_item;
                }
                else
                    return null;
            }
            catch
            {
                Utility.ConsoleWriteLine(string.Format("Logic Error: {0}. ", Utility.FileInfo()), ConsoleColor.Red);
                return null;
            }
        }
        private static Item RawDupe(Item old_item)
        {
            if (old_item == null) return null;
            Type t = old_item.GetType();
            ConstructorInfo[] info = t.GetConstructors();
            foreach (ConstructorInfo c in info)
            {
                ParameterInfo[] paramInfo = c.GetParameters();

                if (paramInfo.Length == 0)
                {
                    object[] objParams = new object[0];

                    try
                    {
                        object o = c.Invoke(objParams);

                        if (o != null && o is Item)
                        {
                            Item new_item = (Item)o;
                            CopyProperties(new_item, old_item);  // now get the properties
                            return new_item;
                        }
                    }
                    catch
                    {
                        Utility.ConsoleWriteLine(string.Format("Logic Error: {0}. ", Utility.FileInfo()), ConsoleColor.Red);
                        return null;
                    }
                }
            }

            return null;
        }

        public static ArrayList Dupe(ArrayList src_items)
        {
            ArrayList theItems = new();
            if (src_items == null)
                return theItems;

            if (src_items.Count == 0)
                return theItems;

            foreach (object o in src_items)
                if (o is Item)
                    theItems.Add(Dupe(o));

            return theItems;
        }
        public static Item DeepDupe(Item old_item, bool raw = false, List<KeyValuePair<Item, Item>> dupeMap = null)
        {
            if (old_item == null) return null;
            Item new_item = null;

            if (HasParameterlessConstructor(old_item.GetType()))
            {
                // we use RawDupe here because sometimes items have their own dupe. When they do this,
                // they may 'dupe' internally referenced items. Maybe you want this, maybe not.
                if (raw)
                    new_item = RawDupe(old_item);
                else
                    new_item = Dupe(old_item);

                System.Diagnostics.Debug.Assert(new_item != null);

                if (new_item != null)
                {
                    if (dupeMap != null)
                        dupeMap.Add(new KeyValuePair<Item, Item>(old_item, new_item));

                    // not supported checker
                    List<Type> types = new();

                    // dupes and copies the items from the old item to the new item
                    if (old_item.PeekItems != null && old_item.PeekItems.Count > 0)
                        foreach (Item old_sub_item in old_item.PeekItems)
                        {
                            new_item.AddItem(DeepDupe(old_sub_item, raw, dupeMap));
                            // items that have a dupe override will already have duped their items
                            //if (old_item.NeedsSubItemsDuped())
                            //    new_item.AddItem(DeepDupe(old_sub_item, raw, dupeMap));
                            //else if (old_item is not Container)
                            //{
                            //    // music motion controller for instance (stashes sheet music in its item table)
                            //    foreach (Item new_sub_item in new_item.PeekItems)
                            //        if (new_sub_item.GetType() == old_sub_item.GetType())
                            //        {
                            //            types.Add(new_sub_item.GetType());
                            //            dupeMap.Add(new KeyValuePair<Item, Item>(old_sub_item, new_sub_item));
                            //        }
                            //}
                            //else
                            //{
                            //    //  Treasure chests, fillable containers, etc. generate their own loot, so don't dupe contents
                            //}
                        }
                    // we don't currently support items that may be stashing more than one of the same type of object in it's item table.
                    System.Diagnostics.Debug.Assert(types.Count < 2);
                }
            }
            else
            {

                // if this type overrides Dupe(), use it!
                if (ItemOverrides(old_item, method_name: "Dupe", param_types: new Type[] { typeof(int) }))
                {
                    new_item = old_item.Dupe(1);
                    if (new_item != null)
                    {
                        if (dupeMap != null)
                            dupeMap.Add(new KeyValuePair<Item, Item>(old_item, new_item));
                    }
                    return new_item;
                }

                // ParamDupe() handles a bunch of common items (like Statics) that don't have a zero parm constructor.
                //  We do our best to construct one of these objects from what is known about the source item
                new_item = ParamDupe(old_item.GetType(), old_item);
                if (new_item != null)
                {
                    if (dupeMap != null)
                        dupeMap.Add(new KeyValuePair<Item, Item>(old_item, new_item));

                    if (new_item is not Container || new_item is Container cont && cont.AutoFills == false)
                    {   // dupes and copies the items from the old item to the new item
                        if (old_item.PeekItems != null && old_item.PeekItems.Count > 0)
                            foreach (Item old_sub_item in old_item.PeekItems)
                                new_item.AddItem(DeepDupe(old_sub_item, raw, dupeMap));
                    }
                }
                System.Diagnostics.Debug.Assert(new_item != null);
            }
            return new_item;
        }
        #region Dupe Helpers
        public static List<Item> GetInternalItems(Item item, bool null_map = false)
        {
            List<Item> elements = new();

            // we will ignore Item properties and only focus in the derived class properties.
            Item linked_item = null;
            PropertyInfo[] props = Utility.ItemRWPropertyProperties(item, IsAssignableTo: typeof(Item));
            for (int i = 0; i < props.Length; i++)
                if ((linked_item = (Item)props[i].GetValue(item, null)) != null)
                    if ((!null_map && linked_item.Map == Map.Internal) || (null_map && linked_item.Map == null))
                        elements.Add(linked_item);

            return elements;
        }
        public static bool HasInternalItems(Item item)
        {
            List<Item> list = GetInternalItems(item);
            return list.Count > 0;
        }
        public static Item GetFirstInternalItem(Item item)
        {
            List<Item> list = GetInternalItems(item);
            if (list.Count > 0)
                return GetInternalItems(item)[0];
            return null;
        }
        #endregion Dupe Helpers
        public static Item CopyItem(Item item)
        {
            if (item == null) return null;
            Item new_item = DeepDupe(item, raw: true);
            List<string> prop_list_names = ItemRWPropertyNames(item, IsAssignableTo: typeof(Item));
            foreach (string prop_list_name in prop_list_names)
                SetItemProp(new_item, prop_list_name, CopyItem(GetItemProp(item, prop_list_name) as Item));

            return new_item;
        }
        public static Item ParamDupe(Type t, Item old_item)
        {
            Item new_item = ParamDupeInternal(t, old_item);
            if (new_item != null)
                CopyProperties(new_item, old_item);             // now get the properties
            return new_item;
        }
        private static Item ParamDupeInternal(Type t, Item old_item)
        {

            if (t == typeof(Static))
            {
                return new Static(old_item.ItemID);
            }
            else if (t == typeof(XmlAddon))
            {
                return (old_item as XmlAddon).Dupe(1);
            }
            else if (t == typeof(RaisableItem))
            {
                return (old_item as RaisableItem).Dupe(1);
            }
            else if (t == typeof(EffectItem))
            {
                return new EffectItem(old_item.ItemID);
            }
            else if (t == typeof(AddonComponent))
            {
                return new Static(old_item.ItemID);
            }
            else if (t == typeof(LocalizedStatic))
            {
                return new LocalizedStatic(old_item.ItemID);
            }
            else if (t == typeof(LocalizedSign))
            {
                return new LocalizedSign(old_item.ItemID, (old_item as LocalizedSign).LabelNumber);
            }
            else if (t == typeof(HintItem))
            {
                return new HintItem((old_item as HintItem).ItemID, (old_item as HintItem).Range, (old_item as HintItem).WarningNumber, (old_item as HintItem).HintNumber);
            }
            else if (t == typeof(WarningItem))
            {
                return new WarningItem((old_item as WarningItem).ItemID, (old_item as WarningItem).Range, (old_item as WarningItem).WarningNumber);
            }
            else if (t == typeof(RattanDoor))
            {
                return new RattanDoor((old_item as RattanDoor).Facing);
            }
            else if (t == typeof(LightWoodDoor))
            {
                return new LightWoodDoor((old_item as LightWoodDoor).Facing);
            }
            else if (t == typeof(DarkWoodDoor))
            {
                return new DarkWoodDoor((old_item as DarkWoodDoor).Facing);
            }
            else if (t == typeof(LightWoodGate))
            {
                return new LightWoodGate((old_item as LightWoodGate).Facing);
            }
            else if (t == typeof(StrongWoodDoor))
            {
                return new StrongWoodDoor((old_item as StrongWoodDoor).Facing);
            }
            else if (t == typeof(MetalDoor))
            {
                return new MetalDoor((old_item as MetalDoor).Facing);
            }
            else if (t == typeof(MetalDoor2))
            {
                return new MetalDoor2((old_item as MetalDoor2).Facing);
            }
            else if (t == typeof(BarredMetalDoor))
            {
                return new BarredMetalDoor((old_item as BarredMetalDoor).Facing);
            }
            else if (t == typeof(BarredMetalDoor2))
            {
                return new BarredMetalDoor2((old_item as BarredMetalDoor2).Facing);
            }
            else if (t == typeof(DarkWoodGate))
            {
                return new DarkWoodGate((old_item as DarkWoodGate).Facing);
            }
            else if (t == typeof(IronGateShort))
            {
                return new IronGateShort((old_item as IronGateShort).Facing);
            }
            else if (t == typeof(IronGate))
            {
                return new IronGate((old_item as IronGate).Facing);
            }
            else if (t == typeof(SecretWoodenDoor))
            {
                return new SecretWoodenDoor((old_item as SecretWoodenDoor).Facing);
            }
            else if (t == typeof(SecretDungeonDoor))
            {
                return new SecretDungeonDoor((old_item as SecretDungeonDoor).Facing);
            }
            else if (t == typeof(SecretStoneDoor1))
            {
                return new SecretStoneDoor1((old_item as SecretStoneDoor1).Facing);
            }
            else if (t == typeof(SecretStoneDoor3))
            {
                return new SecretStoneDoor3((old_item as SecretStoneDoor3).Facing);
            }
            else if (t == typeof(MediumWoodDoor))
            {
                return new MediumWoodDoor((old_item as MediumWoodDoor).Facing);
            }
            else if (t == typeof(DarkWoodHouseDoor))
            {
                return new DarkWoodHouseDoor((old_item as DarkWoodHouseDoor).Facing);
            }
            else if (t == typeof(BeverageBottle))
            {
                return new BeverageBottle((old_item as BeverageBottle).Content);
            }
            else if (t == typeof(Jug))
            {
                return new Jug((old_item as Jug).Content);
            }
            else if (t == typeof(ShipwreckedItem))
            {
                return new ShipwreckedItem(old_item.ItemID);
            }
            else if (t == typeof(DecorativeBow))
            {
                /*  bow type to item ID lookup
                    if (type == 0) ItemID = 5468;
                    if (type == 1) ItemID = 5469;
                    if (type == 2) ItemID = 5470;
                    if (type == 3) ItemID = 5471;
                 */
                Dictionary<int, int> Lookup = new()
                {
                    {5468,0 },
                    {5469,1 },
                    {5470,2 },
                    {5471,3 }
                };

                return new DecorativeBow(Lookup[old_item.ItemID]);
            }
            else if (t == typeof(TreasureMap))
            {
                return new TreasureMap((old_item as TreasureMap).Level);
            }
            else if (t == typeof(TreasureMapChest))
            {
                return new TreasureMapChest((old_item as TreasureMapChest).Level, (old_item as TreasureMapChest).ThemeType);
            }
            else if (t == typeof(DungeonTreasureChest))
            {
                return new DungeonTreasureChest((old_item as DungeonTreasureChest).Level);
            }
            else if (t == typeof(ChampionSkull))
            {
                return new ChampionSkull((old_item as ChampionSkull).Type);
            }
            else if (t == typeof(LargeDockedDragonBoat))
            {
                return new LargeDockedDragonBoat((old_item as LargeDockedDragonBoat).Boat);
            }
            else if (t == typeof(SmallDockedBoat))
            {
                return new SmallDockedBoat((old_item as SmallDockedBoat).Boat);
            }
            if (t == typeof(Server.Engines.Quests.Haven.Cannon))
            {
                return new Server.Engines.Quests.Haven.Cannon((old_item as Server.Engines.Quests.Haven.Cannon).CannonDirection);
            }
            else
            {   // need to handle
                LogHelper logger = new LogHelper("Dupe error.log", overwrite: false, sline: true, quiet: true);
                logger.Log(string.Format("Unable to dupe {0}", old_item));
                logger.Finish();
            }

            throw new ApplicationException(string.Format("Unable to create {0}/{1}", t, old_item));
        }
        #endregion Dupe
        #region CopyProperties
        #region Dynamic mobile creation
        public static string CompileMobileProperties(Mobile src_mobile)
        {
            string defintion = string.Empty;
            PropertyInfo[] props = MobileRWProperties(src_mobile);
            try
            {
                foreach (var prop in props)
                {
                    defintion += prop.Name;
                    defintion += " ";
                    object temp = prop.GetValue(src_mobile, null);
                    if (temp == null || string.IsNullOrEmpty(temp.ToString()))
                        defintion += Commands.Properties.PropNull;
                    else
                        defintion += temp.ToString();
                    defintion += ";";
                }
            }
            catch (Exception ex)
            {
                ;
            }
            return defintion;
        }
        public static void CopyMobileProperties(Mobile dest, string src)
        {
            if (string.IsNullOrEmpty(src)) return;
            Dictionary<string, string> keyValuePairs = ParseString(src);
            if (keyValuePairs == null) return;
            try
            {
                List<string> bad_props = new();
                foreach (var kvp in keyValuePairs)
                {
                    string result = Server.Commands.Properties.SetValue(dest, kvp.Key, kvp.Value);
                    if (result == "Property has been set.")
                        continue;
                    else
                    {
                        bad_props.Add(kvp.Key);
                        continue;
                    }
                }
            }
            catch { }
        }
        public static string CompileMobileSkills(Mobile src_mobile)
        {
            string defintion = string.Empty;

            Server.Skills this_skills = src_mobile.Skills;        // skills
            for (int i = 0; i < this_skills.Length; ++i)
            {
                defintion += this_skills[i].SkillName;
                defintion += " ";
                defintion += this_skills[i].Base.ToString();
                defintion += ";";
            }

            return defintion;
        }
        public static void CopyMobileSkills(Mobile dest_mobile, string src)
        {
            if (string.IsNullOrEmpty(src)) return;
            Dictionary<string, string> keyValuePairs = ParseString(src);
            if (keyValuePairs == null) return;
            try
            {
                Server.Skills this_skills = dest_mobile.Skills;
                int index = 0;
                foreach (var kvp in keyValuePairs)
                    this_skills[index++].Base = double.Parse(kvp.Value);
            }
            catch { }
        }
#nullable enable
        public static Dictionary<string, string>? ParseString(string src)
        {
            if (string.IsNullOrEmpty(src)) return null;
            Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
            try
            {
                string[] tokens = src.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string token in tokens)
                {
                    var spaceIndex = token.IndexOf(" ");
                    var keyword = token.Substring(0, spaceIndex);
                    var value = token.Substring(spaceIndex + 1);
                    keyValuePairs.Add(keyword, value);
                }
                return keyValuePairs;
            }
            catch { }
            return null;
        }
#nullable disable
        #endregion Dynamic mobile creation
        private static List<string> ItemPropertyNames = new();
        public static object GetItemProp(Item item, string prop_name, bool ignore_base_item_props = true)
        {
            if (item == null) return null;
            // we will ignore Item properties and only focus in the derived class properties.
            if (ignore_base_item_props == true && ItemPropertyNames.Count == 0)
            {
                PropertyInfo[] base_props = typeof(Item).GetProperties();
                for (int i = 0; i < base_props.Length; i++)
                    if (base_props[i].CanRead)
                        ItemPropertyNames.Add(base_props[i].Name);
            }

            Item internal_item = null;
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead)
                    if (props[i].Name.Equals(prop_name, StringComparison.OrdinalIgnoreCase))
                        if (ignore_base_item_props)
                        {
                            if (!ItemPropertyNames.Contains(props[i].Name))                 // not a base item property
                                return props[i].GetValue(item, null);
                        }
                        else
                            return props[i].GetValue(item, null);


            return null;
        }
        public static void SetItemProp(Item item, string prop_name, object value)
        {
            Item internal_item = null;
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (props[i].Name.Equals(prop_name, StringComparison.OrdinalIgnoreCase))
                    {
                        props[i].SetValue(item, value, null);
                        break;
                    }
            return;
        }
        private static bool PropertyIntersection(IEntity dest, IEntity src, out SortedList<string, PropertyInfo> dest_accepts, out SortedList<string, PropertyInfo> src_provides)
        {
            dest_accepts = new();
            foreach (PropertyInfo pi in dest.GetType().GetProperties())
                dest_accepts.Add(pi.Name, pi);

            src_provides = new();
            foreach (PropertyInfo pi in src.GetType().GetProperties())
            {
                CopyableAttribute ca = GetCA(src.GetType(), pi.Name);
                if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                    continue;
                src_provides.Add(pi.Name, pi);
            }

            //var src_props = src.GetType().GetProperties();
            //var dest_props = dest.GetType().GetProperties();
            //var srcNames = src_props.Select(p => p.Name).ToHashSet();
            //dest_props_list = dest_props.Where(p => srcNames.Contains(p.Name)).ToList();
            //src_props_list = src_props.Where(p => srcNames.Contains(p.Name)).ToList();
            return dest_accepts.Count > 0 && dest_accepts.Count > 0;
        }
        public static List<string> BaseRWItemPropertyNames(Type IsAssignableTo = null)
        {
            List<string> ItemPropertyNames = new();
            PropertyInfo[] props = typeof(Item).GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (IsAssignableTo != null && !props[i].PropertyType.IsAssignableTo(IsAssignableTo))     // holds one of these
                        continue;
                    else
                        ItemPropertyNames.Add(props[i].Name);

            return ItemPropertyNames;
        }
        public static PropertyInfo[] ItemRWPropertyProperties(Item item, Type IsAssignableTo = null)
        {
            List<PropertyInfo> ItemProperties = new();
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (IsAssignableTo != null && !props[i].PropertyType.IsAssignableTo(IsAssignableTo))     // holds one of these
                        continue;
                    else
                        ItemProperties.Add(props[i]);

            return ItemProperties.ToArray();
        }
        public static List<string> ItemRWPropertyNames(Item item, Type IsAssignableTo = null)
        {
            List<string> ItemProperties = new();
            //List<string> baseRWItemPropertyNames = BaseRWItemPropertyNames(IsAssignableTo);
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (IsAssignableTo != null && !props[i].PropertyType.IsAssignableTo(IsAssignableTo))     // holds one of these
                        continue;
                    else //if (!baseRWItemPropertyNames.Contains(props[i].Name))
                        ItemProperties.Add(props[i].Name);

            return ItemProperties;
        }
        public static PropertyInfo[] ItemRWProperties(Item item)
        {
            List<PropertyInfo> ItemProperties = new();
            PropertyInfo[] props = item.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    ItemProperties.Add(props[i]);

            return ItemProperties.ToArray();
        }
        public static PropertyInfo[] RegionRWProperties(Region region)
        {
            List<PropertyInfo> RegionProperties = new();
            PropertyInfo[] props = region.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    RegionProperties.Add(props[i]);

            return RegionProperties.ToArray();
        }
        public static PropertyInfo[] MobileRWProperties(Mobile mobile)
        {
            List<PropertyInfo> MobileProperties = new();
            PropertyInfo[] props = mobile.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                {
                    // some properties have side effects, they should be marked as Do Not Copy
                    //CopyableAttribute ca = GetCA(props[i]);
                    CopyableAttribute ca = GetCA(mobile.GetType(), props[i].Name);
                    if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                        continue;
                    else
                        MobileProperties.Add(props[i]);
                }

            return MobileProperties.ToArray();
        }
        public static PropertyInfo FindMobileRWProperty(Mobile mobile, string name)
        {
            PropertyInfo[] props = mobile.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
                if (props[i].CanRead && props[i].CanWrite)
                    if (props[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return props[i];
            return null;
        }
        public static void CopySkills(Mobile dest, Mobile src, bool include_stats = true)
        {
            Server.Skills src_skills = src.Skills;
            Server.Skills dest_skills = dest.Skills;
            for (int i = 0; i < src_skills.Length; ++i)
                dest_skills[i].Base = src_skills[i].Base;

            if (include_stats)
            {
                dest.RawDex = src.RawDex;
                dest.RawInt = src.RawInt;
                dest.RawStr = src.RawStr;

                dest.Stam = src.Stam;
                dest.Mana = src.Mana;
                dest.Hits = src.Hits;
            }

            dest.Karma = src.Karma;
            dest.Fame = src.Fame;
        }
        public static void SwapBackpack(Mobile dest, Mobile src, bool include_stats = true)
        {
            if (src.Backpack == null) return;
            if (dest.Backpack != null)
                dest.Backpack.Delete();

            Item new_backpack = DeepDupe(src.Backpack);
            dest.AddItem(new_backpack);
        }
        public static bool CanCopy(PropertyInfo pi)
        {
            CopyableAttribute ca = GetCA(pi.PropertyType, pi.Name);
            if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                return false;
            return true;
        }
        public static bool CanCopy(Type t, string name)
        {
            CopyableAttribute ca = GetCA(t, name);
            if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                return false;
            return true;
        }
        public static void CopyPropertyIntersection(IEntity dest, IEntity src)
        {
            SortedList<string, PropertyInfo> dest_accepts;
            SortedList<string, PropertyInfo> src_provides;
            bool okay = PropertyIntersection(dest, src, out dest_accepts, out src_provides);

            if (okay)
                try
                {
                    foreach (var dest_prop in dest_accepts)                                                         // for each dest property
                        if (src_provides.ContainsKey(dest_prop.Key))                                                // does source have that property?
                            if (dest_prop.Value.CanRead && dest_prop.Value.CanWrite)                                // can read/write dest?
                                if (src_provides[dest_prop.Key].CanRead && src_provides[dest_prop.Key].CanWrite)    // can read/write src?
                                                                                                                    //if (CanCopy(dest_prop.Value) && CanCopy(src_provides[dest_prop.Key]))       // marked as DoNotCopy
                                                                                                                    //if (CanCopy(dest_prop.Value) && CanCopy(src_provides[dest_prop.Key]))
                                    dest_prop.Value.SetValue(dest, src_provides[dest_prop.Key].GetValue(src, null), null); // do it baby!
                }
                catch (Exception ex)
                {
                    LogHelper.LogException(ex);
                }
        }
        public static void CopyProperties(Region dest, Region src)
        {
            Dictionary<string, object> PropertyOverwrite = new();

            #region CopyProperties
            {
                PropertyInfo[] props = RegionRWProperties(src);
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        // some properties have side effects, they should be marked as Do Not Copy
                        //CopyableAttribute ca = GetCA(props[i]);
                        CopyableAttribute ca = GetCA(src.GetType(), props[i].Name);

                        if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                            continue;

                        // copy the property, but a;so track if it gets changed by a subsequent Set
                        object o = props[i].GetValue(src, null);
                        props[i].SetValue(dest, o, null);
                        PropertyOverwrite.Add(props[i].Name, o);
                    }
                    catch
                    {
                        //Console.WriteLine( "Denied" );
                    }
                }
            }
            #endregion  CopyProperties

            #region Verify
            if (!Core.Debug)
                goto done;
            {
                PropertyInfo[] props = dest.GetType().GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead && props[i].CanWrite)
                        {
                            object o = props[i].GetValue(dest, null);
                            if (PropertyOverwrite.ContainsKey(props[i].Name))
                                if (!PropsToIgnore.Contains(props[i].Name))
                                    if (!Same(PropertyOverwrite[props[i].Name], o))
                                    {
                                        //Utility.ConsoleOut(string.Format("Copying properties of {0} is unreliable.", src), ConsoleColor.DarkRed);
                                        //Utility.ConsoleOut(string.Format("Offending property {0}.", props[i].Name), ConsoleColor.DarkRed);
                                        //Utility.ConsoleOut(string.Format("You will need a special case handler in CopyProperties.", src), ConsoleColor.DarkRed);
                                        ;// debug
                                    }
                                    else
                                        ;// debug
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }
        #endregion Verify

        done:
            return;
        }
        public static void CopyProperties(Mobile dest, Mobile src)
        {
            Dictionary<string, object> PropertyOverwrite = new();

            #region CopyProperties
            {
                PropertyInfo[] props = MobileRWProperties(src);
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        // some properties have side effects, they should be marked as Do Not Copy
                        //CopyableAttribute ca = GetCA(props[i]);
                        CopyableAttribute ca = GetCA(src.GetType(), props[i].Name);
                        if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                            continue;

                        // copy the property, but a;so track if it gets changed by a subsequent Set
                        object o = props[i].GetValue(src, null);
                        props[i].SetValue(dest, o, null);
                        PropertyOverwrite.Add(props[i].Name, o);
                    }
                    catch
                    {
                        //Console.WriteLine( "Denied" );
                    }
                }
            }
            #endregion  CopyProperties

            #region Verify
            {
                PropertyInfo[] props = dest.GetType().GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead && props[i].CanWrite)
                        {
                            object o = props[i].GetValue(dest, null);
                            if (PropertyOverwrite.ContainsKey(props[i].Name))
                                if (!PropsToIgnore.Contains(props[i].Name))
                                    if (!Same(PropertyOverwrite[props[i].Name], o))
                                    {
                                        Utility.ConsoleWriteLine(string.Format("Copying properties of {0} is unreliable.", src), ConsoleColor.DarkRed);
                                        Utility.ConsoleWriteLine(string.Format("Offending property {0}.", props[i].Name), ConsoleColor.DarkRed);
                                        Utility.ConsoleWriteLine(string.Format("You will need a special case handler in CopyProperties.", src), ConsoleColor.DarkRed);
                                        ;// debug
                                    }
                                    else
                                        ;// debug
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }
            #endregion Verify
        }
        // these properties are expected to be changing without side effect
        private static List<string> PropsToIgnore = new()
        { 
            // Items
            "LastMoved",    // set when you create a new object, so it will always be different
            "BoolTable",    // underlies Movable, Visible, etc.. 
            // Mobiles
            "Lifespan",
            "NextGrowth",
            "NextMating",
            "Virtues",
        };
        public static void CopyProperties(Item dest, Item src)
        {
            if (src == null || dest == null || src == dest)
                return;

            /* 2/8/2024, Adam
             * CAUTION: CopyProperties
             * CopyProperties does not work for all items.
             *  For example: effect controller (probably others.)
             *  This is because Setting property value X, then setting property value Y may unset or change X.
             *  EffectController for instance: All of SourceItem, SourceMobile, and SourceNull operate on 
             *  IEntity m_Source. If SourceItem is set to some item, and CopyProperties correctly copies it to the 
             *  destination item, all is well. But when it tries to copy SourceMobile, SourceMobile returns null, since
             *  m_Source is an Item and not a Mobile.
             *  When SourceMobile in the destination object gets set to null, so does SourceItem! funky.
             * The correct way to handle this us with [CopyableAttribute(CopyType.DoNotCopy)] property
             *  This prevents CopyProperties() from copying that property
             */
            Dictionary<string, object> PropertyOverwrite = new();

            #region CopyProperties
            {
                PropertyInfo[] props = ItemRWProperties(src);
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        // some properties have side effects, they should be marked as Do Not Copy
                        CopyableAttribute ca = GetCA(src.GetType(), props[i].Name);
                        if (ca != null && ca.CopyType == CopyType.DoNotCopy)
                            continue;

                        // copy the property, but also track if it gets changed by a subsequent Set
                        object o = props[i].GetValue(src, null);
                        props[i].SetValue(dest, o, null);
                        PropertyOverwrite.Add(props[i].Name, o);
                    }
                    catch
                    {
                        //Console.WriteLine( "Denied" );
                    }
                }
            }
            #endregion  CopyProperties

            #region  CopyProperties Override
            /* some items like BaseDoor do not dupe properly. E.g., OpenedID is calculated, so copying that 'calculated' value to the new door's
             * OpenedID gets you a wrong OpenedID when it is recalculated on the destination door. (ClosedID has the same problem.)
             * BaseDoor's CopyProperties corrects this odd behavior.
             * Unfortunately, calling the item's CopyProperties() will recopy the Item's properties, but this is only a handful of simple objects.
             */
            src.CopyProperties(dest);
            #endregion  CopyProperties Override

            #region Verify
            {
                PropertyInfo[] props = dest.GetType().GetProperties();
                for (int i = 0; i < props.Length; i++)
                {
                    try
                    {
                        if (props[i].CanRead && props[i].CanWrite)
                        {
                            object o = props[i].GetValue(dest, null);
                            if (PropertyOverwrite.ContainsKey(props[i].Name))
                                if (!PropsToIgnore.Contains(props[i].Name))
                                    if (!Same(PropertyOverwrite[props[i].Name], o))
                                    {
                                        //Utility.ConsoleOut(string.Format("Copying properties of {0} is unreliable.", src), ConsoleColor.DarkRed);
                                        //Utility.ConsoleOut(string.Format("Offending property {0}.", props[i].Name), ConsoleColor.DarkRed);
                                        //Utility.ConsoleOut(string.Format("You will need a special case handler in CopyProperties.", src), ConsoleColor.DarkRed);
                                        //break;
                                        ;
                                    }
                                    else
                                        ;// debug
                        }
                    }
                    catch
                    {
                        ;
                    }
                }
            }
            #endregion Verify
        }
        //private static CopyableAttribute GetCA(PropertyInfo prop)
        //{
        //    object[] attrs = prop.GetCustomAttributes(typeof(CopyableAttribute), inherit: false);

        //    if (attrs.Length > 0)
        //        return attrs[0] as CopyableAttribute;
        //    else
        //        return null;
        //}
        private static CopyableAttribute GetCA(Type t, string name)
        {   // Be advised: GetCustomAttributes do NOT carry forward to derived classes if the Property is virtual.
            //  The Microsoft docs say this, but it's somewhat unintuitive.
            //  Make sure any properties you are marking as DoNotCopy are NOT virtual.
            object attr = t
              .GetProperty(name)
              .GetCustomAttributes(typeof(CopyableAttribute), false)
              .FirstOrDefault();

            return attr as CopyableAttribute;
        }
        public static bool PatchProperty(Item dest, string property_name, object new_value)
        {
            bool patched = false;
            PropertyInfo[] props = dest.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead && props[i].CanWrite && props[i].Name == property_name)
                    {
                        props[i].SetValue(dest, new_value, null);
                        patched = true;
                        break;
                    }
                }
                catch
                {
                }
            }
            return patched;
        }
        public static bool Same(object o1, object o2)
        {
            if (o1 == null && o2 != null) return false;
            if (o1 != null && o2 == null) return false;
            if (o1 == null && o2 == null) return true;
            return o1.Equals(o2);
        }
        public static void WipeLayers(Mobile m)
        {
            try
            {
                Item[] items = new Item[21];
                items[0] = m.FindItemOnLayer(Layer.Shoes);
                items[1] = m.FindItemOnLayer(Layer.Pants);
                items[2] = m.FindItemOnLayer(Layer.Shirt);
                items[3] = m.FindItemOnLayer(Layer.Helm);
                items[4] = m.FindItemOnLayer(Layer.Gloves);
                items[5] = m.FindItemOnLayer(Layer.Neck);
                items[6] = m.FindItemOnLayer(Layer.Waist);
                items[7] = m.FindItemOnLayer(Layer.InnerTorso);
                items[8] = m.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = m.FindItemOnLayer(Layer.Arms);
                items[10] = m.FindItemOnLayer(Layer.Cloak);
                items[11] = m.FindItemOnLayer(Layer.OuterTorso);
                items[12] = m.FindItemOnLayer(Layer.OuterLegs);
                items[13] = m.FindItemOnLayer(Layer.InnerLegs);
                items[14] = m.FindItemOnLayer(Layer.Bracelet);
                items[15] = m.FindItemOnLayer(Layer.Ring);
                items[16] = m.FindItemOnLayer(Layer.Earrings);
                items[17] = m.FindItemOnLayer(Layer.OneHanded);
                items[18] = m.FindItemOnLayer(Layer.TwoHanded);
                items[19] = m.FindItemOnLayer(Layer.Hair);
                items[20] = m.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                    {
                        m.RemoveItem(items[i]);
                        items[i].Delete();
                    }
                }
            }
            catch (Exception exc)
            {
                System.Console.WriteLine("Send to Zen please: ");
                System.Console.WriteLine("Exception caught in Mobile.WipeLayers: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }
        [Flags]
        public enum CopyLayerFlags
        {
            Default,    // maintains source and destination symmetry
            Immovable,  // 
            Newbie,     //
        }
        public static void CopyLayers(Mobile dest, Mobile src, CopyLayerFlags flags)
        {   //wipe layers of dest mobile, copy identical layers from src to dest
            try
            {
                Utility.WipeLayers(dest);
                Item[] items = new Item[21];
                items[0] = src.FindItemOnLayer(Layer.Shoes);
                items[1] = src.FindItemOnLayer(Layer.Pants);
                items[2] = src.FindItemOnLayer(Layer.Shirt);
                items[3] = src.FindItemOnLayer(Layer.Helm);
                items[4] = src.FindItemOnLayer(Layer.Gloves);
                items[5] = src.FindItemOnLayer(Layer.Neck);
                items[6] = src.FindItemOnLayer(Layer.Waist);
                items[7] = src.FindItemOnLayer(Layer.InnerTorso);
                items[8] = src.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = src.FindItemOnLayer(Layer.Arms);
                items[10] = src.FindItemOnLayer(Layer.Cloak);
                items[11] = src.FindItemOnLayer(Layer.OuterTorso);
                items[12] = src.FindItemOnLayer(Layer.OuterLegs);
                items[13] = src.FindItemOnLayer(Layer.InnerLegs);
                items[14] = src.FindItemOnLayer(Layer.Bracelet);
                items[15] = src.FindItemOnLayer(Layer.Ring);
                items[16] = src.FindItemOnLayer(Layer.Earrings);
                items[17] = src.FindItemOnLayer(Layer.OneHanded);
                items[18] = src.FindItemOnLayer(Layer.TwoHanded);
                items[19] = src.FindItemOnLayer(Layer.Hair);
                items[20] = src.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                    {
                        Item new_item = null;
                        // some items don't have a Parameterless Constructor
                        //  i.e., StuddedGlovesOfMining don't have such a constructor
                        //  when we encounter such an item, just keep going
                        try { new_item = Dupe(items[i]); }
                        catch { /*keep going*/}
                        if (new_item != null)
                        {
                            // Default: first assume the properties of the source
                            new_item.Movable = items[i].Movable;
                            new_item.LootType = items[i].LootType;

                            // override
                            if (flags.HasFlag(CopyLayerFlags.Immovable))
                                new_item.Movable = false;
                            else
                                new_item.Movable = true;
                            if (flags.HasFlag(CopyLayerFlags.Newbie))
                                new_item.LootType = LootType.Newbied;
                            else
                                new_item.LootType = LootType.Regular;

                            // add it
                            dest.AddItem(new_item);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Send to Zen please: ");
                System.Console.WriteLine("Exception caught in Spawner.CopyLayers: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }

        }
        public static EquipmentPack DupeLayers(Mobile src)
        {
            EquipmentPack pack = null;
            if (HasLayers(src) > 0)
                pack = new();
            else
                return null;

            try
            {
                Item[] items = new Item[21];
                items[0] = src.FindItemOnLayer(Layer.Shoes);
                items[1] = src.FindItemOnLayer(Layer.Pants);
                items[2] = src.FindItemOnLayer(Layer.Shirt);
                items[3] = src.FindItemOnLayer(Layer.Helm);
                items[4] = src.FindItemOnLayer(Layer.Gloves);
                items[5] = src.FindItemOnLayer(Layer.Neck);
                items[6] = src.FindItemOnLayer(Layer.Waist);
                items[7] = src.FindItemOnLayer(Layer.InnerTorso);
                items[8] = src.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = src.FindItemOnLayer(Layer.Arms);
                items[10] = src.FindItemOnLayer(Layer.Cloak);
                items[11] = src.FindItemOnLayer(Layer.OuterTorso);
                items[12] = src.FindItemOnLayer(Layer.OuterLegs);
                items[13] = src.FindItemOnLayer(Layer.InnerLegs);
                items[14] = src.FindItemOnLayer(Layer.Bracelet);
                items[15] = src.FindItemOnLayer(Layer.Ring);
                items[16] = src.FindItemOnLayer(Layer.Earrings);
                items[17] = src.FindItemOnLayer(Layer.OneHanded);
                items[18] = src.FindItemOnLayer(Layer.TwoHanded);
                items[19] = src.FindItemOnLayer(Layer.Hair);
                items[20] = src.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null && HasParameterlessConstructor(items[i].GetType()))
                    {
                        Item new_item = null;
                        try { new_item = Dupe(items[i]); }
                        catch { /*keep going*/}
                        if (new_item != null)
                        {
                            // add it
                            pack.AddItem(new_item);
                        }
                    }
                }

            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

            return pack;
        }
        public static int HasLayers(Mobile src)
        {
            int count = 0;
            try
            {
                Item[] items = new Item[21];
                items[0] = src.FindItemOnLayer(Layer.Shoes);
                items[1] = src.FindItemOnLayer(Layer.Pants);
                items[2] = src.FindItemOnLayer(Layer.Shirt);
                items[3] = src.FindItemOnLayer(Layer.Helm);
                items[4] = src.FindItemOnLayer(Layer.Gloves);
                items[5] = src.FindItemOnLayer(Layer.Neck);
                items[6] = src.FindItemOnLayer(Layer.Waist);
                items[7] = src.FindItemOnLayer(Layer.InnerTorso);
                items[8] = src.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = src.FindItemOnLayer(Layer.Arms);
                items[10] = src.FindItemOnLayer(Layer.Cloak);
                items[11] = src.FindItemOnLayer(Layer.OuterTorso);
                items[12] = src.FindItemOnLayer(Layer.OuterLegs);
                items[13] = src.FindItemOnLayer(Layer.InnerLegs);
                items[14] = src.FindItemOnLayer(Layer.Bracelet);
                items[15] = src.FindItemOnLayer(Layer.Ring);
                items[16] = src.FindItemOnLayer(Layer.Earrings);
                items[17] = src.FindItemOnLayer(Layer.OneHanded);
                items[18] = src.FindItemOnLayer(Layer.TwoHanded);
                items[19] = src.FindItemOnLayer(Layer.Hair);
                items[20] = src.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                    if (items[i] != null && HasParameterlessConstructor(items[i].GetType()))
                        count++;
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
            }

            return count;
        }
        public static void LockLayers(Mobile src, bool movable)
        {
            try
            {
                Item[] items = new Item[21];
                items[0] = src.FindItemOnLayer(Layer.Shoes);
                items[1] = src.FindItemOnLayer(Layer.Pants);
                items[2] = src.FindItemOnLayer(Layer.Shirt);
                items[3] = src.FindItemOnLayer(Layer.Helm);
                items[4] = src.FindItemOnLayer(Layer.Gloves);
                items[5] = src.FindItemOnLayer(Layer.Neck);
                items[6] = src.FindItemOnLayer(Layer.Waist);
                items[7] = src.FindItemOnLayer(Layer.InnerTorso);
                items[8] = src.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = src.FindItemOnLayer(Layer.Arms);
                items[10] = src.FindItemOnLayer(Layer.Cloak);
                items[11] = src.FindItemOnLayer(Layer.OuterTorso);
                items[12] = src.FindItemOnLayer(Layer.OuterLegs);
                items[13] = src.FindItemOnLayer(Layer.InnerLegs);
                items[14] = src.FindItemOnLayer(Layer.Bracelet);
                items[15] = src.FindItemOnLayer(Layer.Ring);
                items[16] = src.FindItemOnLayer(Layer.Earrings);
                items[17] = src.FindItemOnLayer(Layer.OneHanded);
                items[18] = src.FindItemOnLayer(Layer.TwoHanded);
                items[19] = src.FindItemOnLayer(Layer.Hair);
                items[20] = src.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                        items[i].Movable = movable;
                }
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Exception caught in Spawner.LockLayers: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }
        public static List<Item> GetLayers(Mobile src)
        {
            List<Item> list = new();
            try
            {
                Item[] items = new Item[21];
                items[0] = src.FindItemOnLayer(Layer.Shoes);
                items[1] = src.FindItemOnLayer(Layer.Pants);
                items[2] = src.FindItemOnLayer(Layer.Shirt);
                items[3] = src.FindItemOnLayer(Layer.Helm);
                items[4] = src.FindItemOnLayer(Layer.Gloves);
                items[5] = src.FindItemOnLayer(Layer.Neck);
                items[6] = src.FindItemOnLayer(Layer.Waist);
                items[7] = src.FindItemOnLayer(Layer.InnerTorso);
                items[8] = src.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = src.FindItemOnLayer(Layer.Arms);
                items[10] = src.FindItemOnLayer(Layer.Cloak);
                items[11] = src.FindItemOnLayer(Layer.OuterTorso);
                items[12] = src.FindItemOnLayer(Layer.OuterLegs);
                items[13] = src.FindItemOnLayer(Layer.InnerLegs);
                items[14] = src.FindItemOnLayer(Layer.Bracelet);
                items[15] = src.FindItemOnLayer(Layer.Ring);
                items[16] = src.FindItemOnLayer(Layer.Earrings);
                items[17] = src.FindItemOnLayer(Layer.OneHanded);
                items[18] = src.FindItemOnLayer(Layer.TwoHanded);
                items[19] = src.FindItemOnLayer(Layer.Hair);
                items[20] = src.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                    if (items[i] != null)
                        list.Add(items[i]);
            }
            catch (Exception exc)
            {
                LogHelper.LogException(exc);
                System.Console.WriteLine("Send to Zen please: ");
                System.Console.WriteLine("Exception caught in Spawner.CopyLayers: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }

            return list;
        }
        #endregion CopyProperties
        public static bool IsSpawnedItem(Item item)
        {
            bool parentSpawned = false;
            object o = item.RootParent;
            if (item != null)
            {
                if (o != null && (o is Item rpi && rpi.Spawner != null) || (o is Mobile rpm && rpm.SpawnerLocation != Point3D.Zero))
                    parentSpawned = true;
                if (item.Spawner != null || parentSpawned)
                    return true;
            }

            return false;
        }
        public static bool HasParameterlessConstructor(Type t)
        {
            ConstructorInfo[] info = t.GetConstructors();

            foreach (ConstructorInfo c in info)
            {
                ParameterInfo[] paramInfo = c.GetParameters();

                if (paramInfo.Length == 0)
                    return true;
            }

            return false;
        }
        /// <summary>
        /// Calculate and return a list of points that define a regions bounds
        /// </summary>
        /// <param name="r"></param>
        /// <param name="m"></param>
        /// <param name="points"></param>
        public static void CalcRegionBounds(Rectangle2D r, Map m, ref List<Point3D> points)
        {
            if (m == Map.Internal || m == null)
                return;

            Point3D p1 = new Point3D(r.X, r.Y - 1, 0);              //So we don't need to create a new one each point
            Point3D p2 = new Point3D(r.X, r.Y + r.Height - 1, 0);   //So we don't need to create a new one each point

            points.Add(new Point3D(r.X - 1, r.Y - 1, m.GetAverageZ(r.X, r.Y - 1))); //Top Corner

            for (int x = r.X; x <= (r.X + r.Width - 1); x++)
            {
                p1.X = x;
                p2.X = x;

                p1.Z = m.GetAverageZ(p1.X, p1.Y);
                p2.Z = m.GetAverageZ(p2.X, p2.Y);

                points.Add(p1); //North bound
                points.Add(p2); //South bound
            }

            p1 = new Point3D(r.X - 1, r.Y - 1, 0);
            p2 = new Point3D(r.X + r.Width - 1, r.Y, 0);

            for (int y = r.Y; y <= (r.Y + r.Height - 1); y++)
            {
                p1.Y = y;
                p2.Y = y;

                p1.Z = m.GetAverageZ(p1.X, p1.Y);
                p2.Z = m.GetAverageZ(p2.X, p2.Y);

                points.Add(p1); //West Bound
                points.Add(p2); //East Bound
            }
        }
        public static void CalcRegionBounds(Region r, ref List<Point3D> points)
        {
            if (r == null || r.Coords.Count == 0)
                return;

            foreach (Rectangle3D rect3D in r.Coords)
            {
                Rectangle2D rect = new Rectangle2D(rect3D.Start, rect3D.End);
                CalcRegionBounds(rect, r.Map, ref points);
            }
        }
        #region Hashing
        public static int GetStableHashCode(string str, int version = 2)
        {
            switch (version)
            {
                case 1: return GetStableHashCodeV1(str);
                case 2: return GetStableHashCodeV2(str);
                default: throw new ArgumentException(string.Format("Version {0} not a supported hash generator", version));
            }
        }
        /*
         * 1/27/22, Adam (GetStableHashCode)
         * The C# runtime version of GetHashCode() generates a different hash every time you run your program.
         * The details are too lenghy to go into here, but if want to understand, here is an article
         * https://andrewlock.net/why-is-string-gethashcode-different-each-time-i-run-my-program-in-net-core/
         * In any case, the MusicBox Music system needs a deterministic hash, and so for this implementation
         * I chose one from Stackoverflow: https://stackoverflow.com/questions/36845430/persistent-hashcode-for-strings/36845864#36845864
         */
        public static int GetStableHashCodeV1(string str)
        {
            unchecked
            {
                int hash1 = 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length && str[i] != '\0'; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1 || str[i + 1] == '\0')
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        } // GetStableHashCodeV1
        /*
         * This is based on the Jenkins One At a Time Hash (as implemented and exhaustively tested by Bret Mulvey), 
         * as such it has excellent avalanching behaviour (a change of one bit in the input propagates to all bits of the output) 
         * which means the somewhat lazy modulo reduction in bits at the end is not a serious flaw for most uses 
         * (though you could do better with more complex behaviour)
         * https://stackoverflow.com/questions/548158/fixed-length-numeric-hash-code-from-variable-length-string-in-c-sharp
         */
        public static int GetStableHashCodeV2(string s) // GetStableHashCodeV2
        {
            const int MUST_BE_LESS_THAN = 100000000; // 8 decimal digits
            uint hash = 0;
            // if you care this can be done much faster with unsafe 
            // using fixed char* reinterpreted as a byte*
            foreach (byte b in System.Text.Encoding.Unicode.GetBytes(s))
            {
                hash += b;
                hash += (hash << 10);
                hash ^= (hash >> 6);
            }
            // final avalanche
            hash += (hash << 3);
            hash ^= (hash >> 11);
            hash += (hash << 15);
            // helpfully we only want positive integer < MUST_BE_LESS_THAN
            // so simple truncate cast is ok if not perfect
            return (int)(hash % MUST_BE_LESS_THAN);
        }

        public static int GetStableHashCode(int i)
        {
            return GetStableHashCode(i.ToString());
        }
        #endregion Hashing
        public static string GetObjectDisplayName(object thing, Type t)
        {
            Item item = thing as Item;
            Mobile mobile = thing as Mobile;
            string name = string.Empty;
            if (item != null)
            {
                name = item.Name;
                if (string.IsNullOrEmpty(name))
                {
                    if (item.LabelNumber != 0)
                    {
                        // it's a label
                        if (Text.Cliloc.Lookup.ContainsKey(item.LabelNumber))
                            name = Text.Cliloc.Lookup[item.LabelNumber];
                        else
                            name = t.Name;
                    }
                    else
                        name = t.Name;
                }
            }
            else if (mobile != null)
            {
                name = mobile.Name;
                if (string.IsNullOrEmpty(name))
                    name = t.Name;
            }
            else
                name = t.Name;

            return name;
        }
        public static string GetShortPath(string path, bool raw = false)
        {
            if (raw == false)
            { 
            string short_path = "";
            try
            {
                if (string.IsNullOrEmpty(path))
                    return short_path;

                string root = Path.Combine(Core.DataDirectory, "..");   // directory above Data is the root
                string temp = Path.GetFullPath(root);                   // this gives us an absolute path without all the '..\..\' stuff
                string[] split = temp.Split(new char[] { '\\', '/' });  // split the path into components
                root = split[split.Length - 1];                         // here is our true root folder

                split = path.Split(new char[] { '\\', '/' });           // now split up what was passed in
                Stack<string> stack = new Stack<string>(split);         // store these components in a stack (reverse order)
                Stack<string> out_stack = new Stack<string>();          // correctly ordered output stack
                while (stack.Count > 0)                                 // collect the components for our short path
                {
                    out_stack.Push(stack.Pop());
                    if (out_stack.Peek() == root)
                        break;
                }

                // now reassemble the short path
                while (out_stack.Count > 0)
                    short_path = Path.Combine(short_path, out_stack.Pop());
            }
            catch (Exception ex)
            {
                Diagnostics.LogHelper.LogException(ex);
            }

            return "...\\" + short_path;
            }
            else
            {   // example: C:\Users\luket\Documents\Software\Development\Product\Src\Angel Island.
                // to: C:\Users\luket\...\Src\Angel Island.
                // reduce >= 9 components to 6.
                List<string> components = new(path.Split(new char[] {'\\', '/'}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                if (components.Count < 9)
                    return path;

                List<string> left = components.Take<string>(9/2).ToList();
                List<string> right = components.Skip(9/2).Take<string>(9).ToList();
                for(int ix=0; ix < 100; ix++)
                {
                    if (left.Count + right.Count >= 6)
                        left.RemoveAt(left.Count - 1);
                    else break;

                    if (left.Count + right.Count >= 6)
                        right.RemoveAt(0);
                    else break;
                }

                left.Add("...");

                // rebuild new shortened path
                string new_path = string.Join("/", left);
                new_path += '/' + string.Join("/", right);

                return new_path;
            }
        }
        public static double GetDistanceToSqrt(Point3D p1, Point3D p2)
        {
            int xDelta = p1.m_X - p2.m_X;
            int yDelta = p1.m_Y - p2.m_Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }
        public static double GetDistanceToSqrt(Point2D p1, Point2D p2)
        {
            int xDelta = p1.m_X - p2.X;
            int yDelta = p1.m_Y - p2.Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }
        public static ConsoleColor BuildColor(int bulidNumber)
        {   // gives us a random color for *this* build.
            //  Allows a quick visual to ensure all servers are running the same build
            return RandomConsoleColor(bulidNumber);
        }
        public static ConsoleColor RandomConsoleColor(int seed)
        {
            var v = Enum.GetValues(typeof(ConsoleColor));
            ConsoleColor selected = (ConsoleColor)v.GetValue(seed % (v.Length - 1));
            if (selected == ConsoleColor.Black) selected = ConsoleColor.White;
            return selected;
        }
        #region AI Version Info
        public static int BuildBuild()
        {
            try
            {
                // open our version info file
                string buildInfoFile = Path.Combine(Core.BuildInfoDir, "build.info");
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the version
                string line = sr.ReadLine();
                sr.Close();
                int result = 0;
                int.TryParse(line, out result);
                return result;

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            return 0;
        }

        public static int BuildRevision()
        {
            try
            {
                string buildInfoFile = Path.Combine(Core.BuildInfoDir, "build.info");
                // open our version info file
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the build
                sr.ReadLine();
                //the next line of text will be the revision
                string line = sr.ReadLine();
                sr.Close();
                int result = 0;
                int.TryParse(line, out result);
                return result;

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            return 0;
        }

        public static int BuildMajor()
        {
            try
            {
                string buildInfoFile = Path.Combine(Core.BuildInfoDir, "build.info");
                // open our version info file
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the build
                sr.ReadLine();
                //the next line of text will be the revision
                sr.ReadLine();
                //the next line of text will be the major version
                string line = sr.ReadLine();
                sr.Close();
                int result = 0;
                int.TryParse(line, out result);
                return result;

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            return 0;
        }

        public static int BuildMinor()
        {
            try
            {
                string buildInfoFile = Path.Combine(Core.BuildInfoDir, "build.info");
                // open our version info file
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the build
                sr.ReadLine();
                //the next line of text will be the revision
                sr.ReadLine();
                //the next line of text will be the major version
                sr.ReadLine();
                //the next line of text will be the minor version
                string line = sr.ReadLine();
                sr.Close();
                int result = 0;
                int.TryParse(line, out result);
                return result;

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }
            return 0;
        }
        #endregion AI Version Info
        public static string GetRootName(int GraphicID)
        {
            string name = "";
            if ((TileData.ItemTable[GraphicID & 0x3FFF].Flags & TileFlag.ArticleA) != 0)
                name = "a " + TileData.ItemTable[GraphicID & 0x3FFF].Name;      // add the article to the item data
            else if ((TileData.ItemTable[GraphicID & 0x3FFF].Flags & TileFlag.ArticleAn) != 0)
                name = "an " + TileData.ItemTable[GraphicID & 0x3FFF].Name;     // add the article to the item data
            else
                name = TileData.ItemTable[GraphicID & 0x3FFF].Name;

            return name;
        }
        #region DayMonthCheck
        public enum DayMonthType
        {
            Before,         // inclusive
            Surrounding,    // inclusive
            After           // inclusive
        }
        /// <summary>
        /// Does a given day/month fall within the day range.
        /// For example, we want to spawn snowballs from December 15th +- 30 days
        /// We would ask the question DayMonthCheck(12,15,30)
        /// </summary>
        /// <param name="month"></param>
        /// <param name="day"></param>
        /// <param name="dayRange"></param>
        /// <returns></returns>
        public static bool DayMonthCheck(int month, int day, int dayRange, DayMonthType type)
        {
            DateTime start = new DateTime(DateTime.UtcNow.Year, month, day);
            DateTime end = start;
            if (type == DayMonthType.Surrounding)
            {
                start -= TimeSpan.FromDays(dayRange);
                end += TimeSpan.FromDays(dayRange);
            }
            else if (type == DayMonthType.Before)
            {
                start -= TimeSpan.FromDays(dayRange);
            }
            else if (type == DayMonthType.After)
            {
                end += TimeSpan.FromDays(dayRange);
            }
            if (DateTime.UtcNow >= start && DateTime.UtcNow <= end)
                return true;
            else
                return false;
        }
        #endregion DayMonthCheck
        public static void Shuffle(IList list)
        {   //  Fisher-Yates shuffle
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = m_Random.Next(n + 1);
                object value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static T RandomObjectMinMaxScaled<T>(T[] values)
        {
            int min = 0;
            int max = values.Length - 1;

            return values[RandomMinMaxScaled(min, max)];
        }

        public static T RandomEnumMinMaxScaled<T>(int min, int max)
        {
            var v = Enum.GetValues(typeof(T));
            int tabmax = v.Length - 1;
            if (max > tabmax) max = tabmax; else if (max < 0) max = 0;
            if (min < 0 || min > max) min = max;

            return (T)v.GetValue(RandomMinMaxScaled(min, max));
        }

        public static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(m_Random.Next(v.Length));
        }

        public static List<Item> ConvertToList(ArrayList old_style)
        {
            List<Item> new_style = new List<Item>();

            foreach (object o in old_style)
                if (o != null && o is Item)
                    new_style.Add(o as Item);

            return new_style;
        }

        public static class World
        {
            private static Rectangle2D[] m_BritWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 5120 - 32, 4096 - 32), new Rectangle2D(5136, 2320, 992, 1760) };
            private static Rectangle2D[] m_IlshWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 2304 - 32, 1600 - 32) };
            private static Rectangle2D[] m_TokunoWrap = new Rectangle2D[] { new Rectangle2D(16, 16, 1448 - 32, 1448 - 32) };
            private static Rectangle2D[] m_SolenCaves = new Rectangle2D[] { new Rectangle2D(5632, 1775, 305, 266) };
            private static Rectangle2D[] m_Dungeons = new Rectangle2D[] { new Rectangle2D(new Point2D(5120, 3), new Point2D(6143, 2303)) };
            private static Rectangle2D[] m_DaggerIsland = new Rectangle2D[] { new Rectangle2D(3865, 175, 432, 584) };


            // Rectangle to define Angel Island boundaries.
            // used to define where boats can be placed, and where they can sail.
            private static Rectangle2D m_AIRect = new Rectangle2D(140, 690, 270, 180);
            public static Rectangle2D AIRect { get { return m_AIRect; } }
            public static Rectangle2D[] BritWrap { get { return m_BritWrap; } }
            public static Rectangle2D BritMainLandWrap { get { return m_BritWrap[0]; } }
            public static Rectangle2D LostLandsWrap { get { return m_BritWrap[1]; } }
            public static Rectangle2D SolenCaves { get { return m_SolenCaves[0]; } }
            public static Rectangle2D Dungeons { get { return m_Dungeons[0]; } }
            public static Rectangle2D DaggerIsland { get { return m_DaggerIsland[0]; } }

            public static bool IsValidLocation(Point3D p, Map map)
            {
                Rectangle2D[] wrap = GetWrapFor(map);

                for (int i = 0; i < wrap.Length; ++i)
                {
                    if (wrap[i].Contains(p))
                        return true;
                }

                return false;
            }

            public static bool InaccessibleMapLoc(Point3D p, Map map)
            {
                RecallRune marker = new RecallRune();
                marker.MoveToWorld(p, map);

                IPooledEnumerable eable;
                eable = map.GetItemsInRange(p, 0);
                bool found = false;
                foreach (Item item in eable)
                {
                    if (item == null || item.Deleted || item is not RecallRune)
                        continue;

                    // yeah, GetItemsInRange() does not respect exact Z, not sure why
                    if (item.Z != p.Z)
                        continue;

                    found = true;
                    break;
                }
                eable.Free();

                marker.Delete();

                return !found;
            }

            public static Rectangle2D[] GetWrapFor(Map map)
            {
                if (map == Map.Ilshenar)
                    return m_IlshWrap;
                else if (map == Map.Tokuno)
                    return m_TokunoWrap;
                else
                    return m_BritWrap;
            }

            public static bool AreaContains(Rectangle2D[] area, Point2D p)
            {
                for (int i = 0; i < area.Length; i++)
                {
                    if (area[i].Contains(p))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Token
        /// Return a statistically unique integer token representing a string.
        /// The resulting token represents a case insensitive, space insensitive, and word order insensitive string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Int64</returns>
        public static Int64 Token(string str)
        {
            if (str == null)
                return 0;

            string[] tokens = str.ToLower().Split(new Char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            Int64 hash = 0;
            for (int ix = 0; ix < tokens.Length; ix++)
                hash += tokens[ix].GetHashCode();

            return hash;
        }

        public static bool Token(string str1, string str2)
        {
            return Token(str1) == Token(str2);
        }
        /// <summary>
        /// Take a token string and insert spaces where capitalization changes.
        /// For instance, "AngelIsland" would be converted to "Angel Island"
        /// Useful for converting enums to text.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string SplitOnCase(string s)
        {
            string new_string = "";
            string[] split = System.Text.RegularExpressions.Regex.Split(s, @"(?<!^)(?=[A-Z])");
            foreach (string se in split)
                new_string += se + " ";
            return new_string.Trim();
        }

        /// <summary>
        /// scale a number from an Old range to a new range. 
        /// in the example below we take the stat based damage from mind blast (1-45 hp) and scale it DamageRangeLow and DamageRangeHigh.
        /// where DamageRangeLow is some where around a lightning bolt and DamageRangeHigh was somewhere around an ebolt
        /// damage = (int) (((double)damage / ((45.0 - 1.0) / (DamageRangeHigh - DamageRangeLow))) + DamageRangeLow);
        /// </summary>
        /// <param name="n"></param>
        /// <param name="oRangeMin"></param>
        /// <param name="oRangeMax"></param>
        /// <param name="nRangeMin"></param>
        /// <param name="nRangeMax"></param>
        /// <returns></returns>
        public static double RescaleNumber(double n, double oRangeMin, double oRangeMax, double nRangeMin, double nRangeMax)
        {
            try { return (((double)n / ((oRangeMax - oRangeMin) / (nRangeMax - nRangeMin))) + nRangeMin); }
            catch { return 0.0; }
        }

        // data packing functions
        public static uint GetUIntRight16(uint value)
        {
            return value & 0x0000FFFF;
        }

        public static void SetUIntRight16(ref uint value, uint bits)
        {
            value &= 0xFFFF0000;   // clear bottom 16
            bits &= 0x0000FFFF;    // clear top 16
            value |= bits;         // Done
        }

        public static uint GetUIntByte3(uint value)
        {
            return (value & 0x00FF0000) >> 16;
        }

        public static void SetUIntByte3(ref uint value, uint bits)
        {
            // set byte #3
            uint byte3 = bits << 16;    // move it into position
            value &= 0xFF00FFFF;        // clear 3rd byte in dest
            byte3 &= 0x00FF0000;        // clear all but 3rd byte - safety
            value |= byte3;             // Done
        }

        public static uint GetUIntByte4(uint value)
        {
            return (value & 0xFF000000) >> 24;
        }

        public static void SetUIntByte4(ref uint value, uint bits)
        {
            // set byte #4
            uint byte4 = bits << 24;    // move it into position
            value &= 0x00FFFFFF;        // clear 4th byte in dest
            byte4 &= 0xFF000000;        // clear all but 4th byte - safety
            value |= byte4;             // Done
        }

        /* How to use the Profiler class
			// Create a class like this
			public class  CPUProfiler
			{	// 
				public static void Start()
				{	// add as many of these as you want
					Region.InternalExitProfile.ResetTimer();		
				}
				public static void StopAndSaveSnapShot()
				{	// add as many of these as you want
					System.Console.WriteLine("Region.InternalExit: {0:00.00} seconds", Region.InternalExitProfile.Elapsed());
				}
			}
		 
			// then create a Utility.Profiler object in the file you want to monitor
			public static Utility.Profiler InternalExitProfile = new Utility.Profiler();

			// then bracket the code you want to monitor	
			public void InternalExit( Mobile m )
			{
				InternalExitProfile.Start();
				// the code you want to monitor
				InternalExitProfile.End();
			}
		 
			// finally make the calls to start and stop and print the profile info
			CPUProfiler.Start();							// ** PROFILER START ** 
			// call some process that you want to profile
			CPUProfiler.StopAndSaveSnapShot();				// ** PROFILER STOP ** 
		*/
        /*public class  Profiler
		{
			private double m_Elapsed;
			Utility.TimeCheck m_tc;
			public void ResetTimer() { m_Elapsed = 0.0; }
			public double Elapsed() { return m_Elapsed; }
			public void Start() { m_tc = new Utility.TimeCheck(); m_tc.Start(); }
			public void End() { m_tc.End(); m_Elapsed += m_tc.Elapsed(); }
		}*/

        public class TimeCheck
        {
            private DateTime m_startTime;
            private TimeSpan m_span;

            public TimeCheck()
            {
            }

            public void Start()
            {
                m_startTime = DateTime.UtcNow;
            }

            public void End()
            {
                m_span = DateTime.UtcNow - m_startTime;
            }

            public double Elapsed()
            {
                TimeSpan tx = DateTime.UtcNow - m_startTime;
                return tx.TotalSeconds;
            }

            public string TimeTaken
            {
                get
                {
                    //return string.Format("{0:00}:{1:00}:{2:00.00}",
                    //	m_span.Hours, m_span.Minutes, m_span.Seconds);
                    return string.Format("{0:00.00} seconds", m_span.TotalSeconds);
                }
            }
        }

        public static void DebugOut(string text, ConsoleColor color = ConsoleColor.White)
        {
#if DEBUG
            ConsoleWriteLine(text, color);
#endif
        }
        private static string m_ConsoleOutEcho = null;
        public static string ConsoleOutEcho
        {
            get { return m_ConsoleOutEcho; }
            set { m_ConsoleOutEcho = value; }
        }
        public static void DebugOut(string format, ConsoleColor color = ConsoleColor.White, params object[] args)
        {
            DebugOut(string.Format(format, args), color);
        }
        public static void ConsoleWrite(string text, ConsoleColor color)
        {
            Console.Out.Flush();
            PushColor(color);
            Console.Write(text);
            PopColor();
            Console.Out.Flush();
        }
        public static void ConsoleWriteLine(string text, ConsoleColor color)
        {
            Console.Out.Flush();
            PushColor(color);
            Console.WriteLine(text);
            if (m_ConsoleOutEcho != null) File.AppendAllLines(m_ConsoleOutEcho, new string[] { text });
            PopColor();
            Console.Out.Flush();
        }
        public static void ConsoleWriteLine(string format, ConsoleColor color, params object[] args)
        {
            ConsoleWriteLine(string.Format(format, args), color);
        }
        public static void ErrorOut(string text, ConsoleColor color)
        {
            Console.Error.Flush();
            PushColor(color);
            Console.Error.WriteLine(text);
            if (m_ConsoleOutEcho != null) File.AppendAllLines(m_ConsoleOutEcho, new string[] { text });
            PopColor();
            Console.Error.Flush();
        }
        public static void ErrorOut(string format, ConsoleColor color, params object[] args)
        {
            ErrorOut(string.Format(format, args), color);
        }
        public static string FileInfo([System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return string.Format("in {0} at line number {1}", GetShortPath(filePath), lineNumber);
        }
        private static Random m_Random = new Random();
        private static Encoding m_UTF8, m_UTF8WithEncoding;

        public static Encoding UTF8
        {
            get
            {
                if (m_UTF8 == null)
                    m_UTF8 = new UTF8Encoding(false, false);

                return m_UTF8;
            }
        }

        public static Encoding UTF8WithEncoding
        {
            get
            {
                if (m_UTF8WithEncoding == null)
                    m_UTF8WithEncoding = new UTF8Encoding(true, false);

                return m_UTF8WithEncoding;
            }
        }

        public static void Separate(StringBuilder sb, string value, string separator)
        {
            if (sb.Length > 0)
                sb.Append(separator);

            sb.Append(value);
        }

        public static bool IsValidIP(string text)
        {
            bool valid = true;

            IPMatch(text, IPAddress.None, ref valid);

            return valid;
        }

        public static bool IPMatch(string val, IPAddress ip)
        {
            bool valid = true;

            return IPMatch(val, ip, ref valid);
        }

        public static string FixHtml(string str)
        {
            bool hasOpen = (str.IndexOf('<') >= 0);
            bool hasClose = (str.IndexOf('>') >= 0);
            bool hasPound = (str.IndexOf('#') >= 0);

            if (!hasOpen && !hasClose && !hasPound)
                return str;

            StringBuilder sb = new StringBuilder(str);

            if (hasOpen)
                sb.Replace('<', '(');

            if (hasClose)
                sb.Replace('>', ')');

            if (hasPound)
                sb.Replace('#', '-');

            return sb.ToString();
        }

        public static bool IPMatch(string val, IPAddress ip, ref bool valid)
        {
            valid = true;

            string[] split = val.Split('.');

            for (int i = 0; i < 4; ++i)
            {
                int lowPart, highPart;

                if (i >= split.Length)
                {
                    lowPart = 0;
                    highPart = 255;
                }
                else
                {
                    string pattern = split[i];

                    if (pattern == "*")
                    {
                        lowPart = 0;
                        highPart = 255;
                    }
                    else
                    {
                        lowPart = 0;
                        highPart = 0;

                        bool highOnly = false;
                        int lowBase = 10;
                        int highBase = 10;

                        for (int j = 0; j < pattern.Length; ++j)
                        {
                            char c = (char)pattern[j];

                            if (c == '?')
                            {
                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += 0;
                                }

                                highPart *= highBase;
                                highPart += highBase - 1;
                            }
                            else if (c == '-')
                            {
                                highOnly = true;
                                highPart = 0;
                            }
                            else if (c == 'x' || c == 'X')
                            {
                                lowBase = 16;
                                highBase = 16;
                            }
                            else if (c >= '0' && c <= '9')
                            {
                                int offset = c - '0';

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else if (c >= 'a' && c <= 'f')
                            {
                                int offset = 10 + (c - 'a');

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else if (c >= 'A' && c <= 'F')
                            {
                                int offset = 10 + (c - 'A');

                                if (!highOnly)
                                {
                                    lowPart *= lowBase;
                                    lowPart += offset;
                                }

                                highPart *= highBase;
                                highPart += offset;
                            }
                            else
                            {
                                valid = false;
                            }
                        }
                    }
                }

                int b = ip.GetAddressBytes()[i];

                if (b < lowPart || b > highPart)
                    return false;
            }

            return true;
        }

        public static bool IPMatchClassC(IPAddress ip1, IPAddress ip2)
        {
            for (int i = 0; i < 3; i++)
            {
                if (ip1.GetAddressBytes()[i] != ip2.GetAddressBytes()[i])
                    return false;
            }

            return true;
        }

        /*public static bool IPMatch( string val, IPAddress ip )
		{
			string[] split = val.Split( '.' );

			for ( int i = 0; i < split.Length; ++i )
			{
				int b = (byte)(ip.Address >> (i * 8));
				string s = split[i];

				if ( s == "*" )
					continue;

				if ( ToInt32( s ) != b )
					return false;
			}

			return true;
		}*/


        public static string GetHost()
        {
            try
            {
                return Dns.GetHostName();
            }
            catch
            {
                return null;
            }
        }

        public static bool IsHostPrivate(string host)
        {
            try
            {   // are we on some random developer's computer?
                if (IsHostPROD(host) || IsHostTC(host))
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHostPROD(string host)
        {
            try
            {   // host name of our "prod" server
                if (host == "sls-dd4p11")
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsHostTC(string host)
        {
            try
            {   // host name of our "Test Center" server
                if (host == "sls-ce9p3")
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        public static int InsensitiveCompare(string first, string second)
        {
            return Insensitive.Compare(first, second);
        }

        public static bool InsensitiveStartsWith(string first, string second)
        {
            return Insensitive.StartsWith(first, second);
        }

        public static bool ToBoolean(string value)
        {
            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return false;
            }
        }

        public static double ToDouble(string value)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0.0;
            }
        }

        public static TimeSpan ToTimeSpan(string value)
        {
            try
            {
                return TimeSpan.Parse(value);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        public static int ToInt32(string value)
        {
            try
            {
                if (value.StartsWith("0x"))
                {
                    return Convert.ToInt32(value.Substring(2), 16);
                }
                else
                {
                    int result = 0;
                    if (int.TryParse(value, out result))
                        return Convert.ToInt32(value);
                    return result;
                }
            }
            catch
            {
                return 0;
            }
        }

        //SMD: merged in for runuo2.0 networking stuff
        public static int GetAddressValue(IPAddress address)
        {
#pragma warning disable 618
            return (int)address.Address;
#pragma warning restore 618
        }

        public static string Intern(string str)
        {
            if (str == null)
                return null;
            else if (str.Length == 0)
                return string.Empty;

            return string.Intern(str);
        }

        public static void Intern(ref string str)
        {
            str = Intern(str);
        }

        private static Dictionary<IPAddress, IPAddress> _ipAddressTable;

        public static IPAddress Intern(IPAddress ipAddress)
        {
            if (_ipAddressTable == null)
            {
                _ipAddressTable = new Dictionary<IPAddress, IPAddress>();
            }

            IPAddress interned;

            if (!_ipAddressTable.TryGetValue(ipAddress, out interned))
            {
                interned = ipAddress;
                _ipAddressTable[ipAddress] = interned;
            }

            return interned;
        }

        public static void Intern(ref IPAddress ipAddress)
        {
            ipAddress = Intern(ipAddress);
        }

        private static Stack<ConsoleColor> m_ConsoleColors = new Stack<ConsoleColor>();

        public static void PushColor(ConsoleColor color)
        {
            try
            {
                lock (m_ConsoleColors)
                {
                    m_ConsoleColors.Push(Console.ForegroundColor);
                    Console.ForegroundColor = color;
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void PopColor()
        {
            try
            {
                lock (m_ConsoleColors)
                {
                    Console.ForegroundColor = m_ConsoleColors.Pop();
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        //SMD: end merge

        public static bool InRange(Point3D p1, Point3D p2, int range)
        {
            return (p1.m_X >= (p2.m_X - range))
                && (p1.m_X <= (p2.m_X + range))
                && (p1.m_Y >= (p2.m_Y - range))
                && (p1.m_Y <= (p2.m_Y + range));
        }

        public static bool InUpdateRange(Point3D p1, Point3D p2)
        {
            return (p1.m_X >= (p2.m_X - 18))
                && (p1.m_X <= (p2.m_X + 18))
                && (p1.m_Y >= (p2.m_Y - 18))
                && (p1.m_Y <= (p2.m_Y + 18));
        }

        public static bool InUpdateRange(Point2D p1, Point2D p2)
        {
            return (p1.m_X >= (p2.m_X - 18))
                && (p1.m_X <= (p2.m_X + 18))
                && (p1.m_Y >= (p2.m_Y - 18))
                && (p1.m_Y <= (p2.m_Y + 18));
        }

        public static bool InUpdateRange(IPoint2D p1, IPoint2D p2)
        {
            return (p1.X >= (p2.X - 18))
                && (p1.X <= (p2.X + 18))
                && (p1.Y >= (p2.Y - 18))
                && (p1.Y <= (p2.Y + 18));
        }

        public static Direction GetDirection(IPoint2D from, IPoint2D to)
        {
            int dx = to.X - from.X;
            int dy = to.Y - from.Y;

            int adx = Math.Abs(dx);
            int ady = Math.Abs(dy);

            if (adx >= ady * 3)
            {
                if (dx > 0)
                    return Direction.East;
                else
                    return Direction.West;
            }
            else if (ady >= adx * 3)
            {
                if (dy > 0)
                    return Direction.South;
                else
                    return Direction.North;
            }
            else if (dx > 0)
            {
                if (dy > 0)
                    return Direction.Down;
                else
                    return Direction.Right;
            }
            else
            {
                if (dy > 0)
                    return Direction.Left;
                else
                    return Direction.Up;
            }
        }
#if false
        public static bool CanMobileFit(int z, Tile[] tiles)
        {
            int checkHeight = 15;
            int checkZ = z;

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile  tile = tiles[i];

                if (((checkZ + checkHeight) > tile.Z && checkZ < (tile.Z + tile.Height))/* || (tile.Z < (checkZ + checkHeight) && (tile.Z + tile.Height) > checkZ)*/ )
                {
                    return false;
                }
                else if (checkHeight == 0 && tile.Height == 0 && checkZ == tile.Z)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsInContact(Tile check, Tile[] tiles)
        {
            int checkHeight = check.Height;
            int checkZ = check.Z;

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile  tile = tiles[i];

                if (((checkZ + checkHeight) > tile.Z && checkZ < (tile.Z + tile.Height))/* || (tile.Z < (checkZ + checkHeight) && (tile.Z + tile.Height) > checkZ)*/ )
                {
                    return true;
                }
                else if (checkHeight == 0 && tile.Height == 0 && checkZ == tile.Z)
                {
                    return true;
                }
            }

            return false;
        }
#endif
        public static object GetArrayCap(Array array, int index)
        {
            return GetArrayCap(array, index, null);
        }

        public static object GetArrayCap(Array array, int index, object emptyValue)
        {
            if (array.Length > 0)
            {
                if (index < 0)
                {
                    index = 0;
                }
                else if (index >= array.Length)
                {
                    index = array.Length - 1;
                }

                return array.GetValue(index);
            }
            else
            {
                return emptyValue;
            }
        }

        //4d6+8 would be: Utility.Dice( 4, 6, 8 )
        public static int Dice(int numDice, int numSides, int bonus)
        {
            int total = 0;
            for (int i = 0; i < numDice; ++i)
                total += Random(numSides) + 1;
            total += bonus;
            return total;
        }
        public static int RandomMinMaxScaled(int nelts)
        {   // used when you have an array and want to include all array elements
            return RandomMinMaxScaled(0, nelts - 1);
        }
        public static int RandomMinMaxScaled(int min, int max)
        {
            if (min == max)
                return min;

            if (min > max)
            {
                int hold = min;
                min = max;
                max = hold;
            }

            /* Example:
			 *    min: 1
			 *    max: 5
			 *  count: 5
			 *
			 * total = (5*5) + (4*4) + (3*3) + (2*2) + (1*1) = 25 + 16 + 9 + 4 + 1 = 55
			 *
			 * chance for min+0 : 25/55 : 45.45%
			 * chance for min+1 : 16/55 : 29.09%
			 * chance for min+2 :  9/55 : 16.36%
			 * chance for min+3 :  4/55 :  7.27%
			 * chance for min+4 :  1/55 :  1.81%
			 */

            int count = max - min + 1;
            int total = 0, toAdd = count;

            for (int i = 0; i < count; ++i, --toAdd)
                total += toAdd * toAdd;

            int rand = Utility.Random(total);
            toAdd = count;

            int val = min;

            for (int i = 0; i < count; ++i, --toAdd, ++val)
            {
                rand -= toAdd * toAdd;

                if (rand < 0)
                    break;
            }

            return val;
        }
        public static bool RandomChance(double percent)
        {
            return (percent / 100.0 >= Utility.RandomDouble());
        }

        public static bool Chance(double chance)
        {
            return (chance >= Utility.RandomDouble());
        }

        public static double RandomDouble()
        {
            return m_Random.NextDouble();
        }

        public static double RandomDouble(double min, double max)
        {
            return m_Random.NextDouble() * (max - min) + min;
        }

        public static int RandomList(params int[] list)
        {
            return list[m_Random.Next(list.Length)];
        }

        public static Item RandomList(params Item[] list)
        {
            return list[m_Random.Next(list.Length)];
        }

        public static T RandomList<T>(params T[] list)
        {
            return list[m_Random.Next(list.Length)];
        }

        public static bool RandomBool()
        {
            return (m_Random.Next(2) == 0);
        }

        public static int RandomMinMax(int min, int max)
        {
            if (min > max)
            {
                int copy = min;
                min = max;
                max = copy;
            }
            else if (min == max)
            {
                return min;
            }

            return min + m_Random.Next((max - min) + 1);
        }

        public static TimeSpan RandomMinMax(TimeSpan min, TimeSpan max)
        {
            return TimeSpan.FromMilliseconds(Utility.RandomMinMax(min.TotalMilliseconds, max.TotalMilliseconds));
        }
        public static DateTime FutureTime(DateTime check, TimeSpan min, TimeSpan max)
        {
            return (check + Utility.RandomMinMax(min, max));
        }
        public static TimeSpan DeltaTime(DateTime check, TimeSpan min, TimeSpan max)
        {
            return FutureTime(check, min, max) - check;
        }
        public static double RandomMinMax(double minimum, double maximum)
        {
            return m_Random.NextDouble() * (maximum - minimum) + minimum;
        }
        public static int Random(int from, int count)
        {
            if (count == 0)
            {
                return from;
            }
            else if (count > 0)
            {
                return from + m_Random.Next(count);
            }
            else
            {
                return from - m_Random.Next(-count);
            }
        }

        public static int Random(int count)
        {
            return m_Random.Next(count);
        }

        public static int RandomNondyedHue()
        {
            switch (Random(6))
            {
                case 0: return RandomPinkHue();
                case 1: return RandomBlueHue();
                case 2: return RandomGreenHue();
                case 3: return RandomOrangeHue();
                case 4: return RandomRedHue();
                case 5: return RandomYellowHue();
            }

            return 0;
        }

        public static int RandomPinkHue()
        {
            return Random(1201, 54);
        }

        public static int RandomBlueHue()
        {
            return Random(1301, 54);
        }

        public static int RandomGreenHue()
        {
            return Random(1401, 54);
        }

        public static int RandomOrangeHue()
        {
            return Random(1501, 54);
        }

        public static int RandomRedHue()
        {
            return Random(1601, 54);
        }

        public static int RandomYellowHue()
        {
            return Random(1701, 54);
        }

        public static int RandomNeutralHue()
        {
            return Random(1801, 108);
        }

        public static int RandomSpecialVioletHue()
        {
            return Utility.RandomList(1230, 1231, 1232, 1233, 1234, 1235);
        }

        public static int RandomSpecialTanHue()
        {
            return Utility.RandomList(1501, 1502, 1503, 1504, 1505, 1506, 1507, 1508);
        }

        public static int RandomSpecialBrownHue()
        {
            return Utility.RandomList(2012, 2013, 2014, 2015, 2016, 2017);
        }

        public static int RandomSpecialDarkBlueHue()
        {
            return Utility.RandomList(1303, 1304, 1305, 1306, 1307, 1308);
        }

        public static int RandomSpecialForestGreenHue()
        {
            return Utility.RandomList(1420, 1421, 1422, 1423, 1424, 1425, 1426);
        }

        public static int RandomSpecialPinkHue()
        {
            return Utility.RandomList(1619, 1620, 1621, 1622, 1623, 1624, 1625, 1626);
        }

        public static int RandomSpecialRedHue()
        {
            return Utility.RandomList(1640, 1641, 1642, 1643, 1644);
        }

        public static int RandomSpecialOliveHue()
        {
            return Utility.RandomList(2001, 2002, 2003, 2004, 2005);
        }

        public static int RandomSnakeHue()
        {
            return Random(2001, 18);
        }

        public static int RandomBirdHue()
        {
            return Random(2101, 30);
        }

        public static int RandomSlimeHue()
        {
            return Random(2201, 24);
        }

        public static int RandomOstardHue()
        {   // On Siege, 0x88A5 is Special yellow reserved for Savage Ridgebacks
            int hue = 0;
            while ((hue = RandomSlimeHue()) == (Core.RuleSets.SiegeStyleRules() ? 0x88A5 : 0)) ;
            return hue;
        }

        public static int RandomAnimalHue()
        {
            return Random(2301, 18);
        }
        public enum ColorSelect : int
        {
            Violet,          // Violet           1230 - 1235 (6)
            Tan,             // Tan              1501 - 1508 (8)
            Brown,           // Brown            2012 - 2017 (5)
            DarkBlue,       // Dark Blue        1303 - 1308 (6)
            ForestGreen,    // Forest Green     1420 - 1426 (7)
            Pink,            // Pink             1619 - 1626 (8)
            Crimson,         // Crimson          1640 - 1644 (5) - renamed from "Red" to "Crimson"
            Olive,           // Olive            2001 - 2005 (5)

            DullCopper,     // Dull Copper      2419 - 2424 (6)
            ShadowIron,     // Shadow Iron      2406 - 2412 (7)
            Copper,          // Copper           2413 - 2418 (6)
            Bronze,          // Bronze           2414 - 2418 (5)
            Gold,            // Gold             2213 - 2218 (6)
            Agapite,         // Agapite          2425 - 2430 (6)
            Verite,          // Verite           2207 - 2212 (6)
            Valorite,        // Valorite         2219 - 2224 (6)

            Red,             // Red              2113 - 2118 (6)
            Blue,            // Blue             2119 - 2124 (6)
            Green,           // Green            2126 - 2130 (5)
        }
        // special dye tub colors
        private static ColorInfo[] m_ColorTable = new ColorInfo[]
            {
                // special dye tub colors
                new ColorInfo( 1230, 6, "Violet" ),          // Violet           1230 - 1235 (6)
                new ColorInfo( 1501, 8, "Tan" ),             // Tan              1501 - 1508 (8)
                new ColorInfo( 2013, 5, "Brown" ),           // Brown            2012 - 2017 (5)
                new ColorInfo( 1303, 6, "Dark Blue" ),       // Dark Blue        1303 - 1308 (6)
                new ColorInfo( 1420, 7, "Forest Green" ),    // Forest Green     1420 - 1426 (7)
                new ColorInfo( 1619, 8, "Pink" ),            // Pink             1619 - 1626 (8)
                new ColorInfo( 1640, 5, "Crimson" ),         // Crimson          1640 - 1644 (5) - renamed from "Red" to "Crimson"
                new ColorInfo( 2001, 5, "Olive" ),           // Olive            2001 - 2005 (5)

                new ColorInfo( 2419, 6, "Dull Copper" ),     // Dull Copper      2419 - 2424 (6)
                new ColorInfo( 2406, 7, "Shadow Iron" ),     // Shadow Iron      2406 - 2412 (7)
                new ColorInfo( 2413, 6, "Copper" ),          // Copper           2413 - 2418 (6)
                new ColorInfo( 2414, 5, "Bronze" ),          // Bronze           2414 - 2418 (5)
                new ColorInfo( 2213, 6, "Gold" ),            // Gold             2213 - 2218 (6)
                new ColorInfo( 2425, 6, "Agapite" ),         // Agapite          2425 - 2430 (6)
                new ColorInfo( 2207, 6, "Verite" ),          // Verite           2207 - 2212 (6)
                new ColorInfo( 2219, 6, "Valorite" ),        // Valorite         2219 - 2224 (6)

                new ColorInfo( 2113, 6, "Red" ),             // Red              2113 - 2118 (6)
                new ColorInfo( 2119, 6, "Blue" ),            // Blue             2119 - 2124 (6)
                new ColorInfo( 2126, 5, "Green" ),           // Green            2126 - 2130 (5)
                // yellow is a duplicate of Gold above
                //new ColorInfo( 2213, 6, "Yellow" ),          // Yellow           2213 - 2218 (6)
            };
        // special dye tub colors
        public static int RandomSpecialHue(string key)
        {   // don't care what the color is, but always want it the same? this is your function.
            return RandomSpecialHue(GetStableHashCode(key));
        }
        public static int RandomSpecialHue(int hash = 0)
        {
            ColorInfo ci = m_ColorTable[hash == 0 ? Utility.Random(m_ColorTable.Length) : Math.Abs(hash) % m_ColorTable.Length];
            return ci.BaseHue + (hash == 0 ? Utility.Random(ci.Shades) : Math.Abs(hash) % ci.Shades);
        }
        public static int RandomSpecialHue(ColorSelect color, string key)
        {   // don't care what the color is, but always want it the same? this is your function.
            return RandomSpecialHue(color, GetStableHashCode(key));
        }
        public static int RandomSpecialHue(ColorSelect color, int hash = 0)
        {
            ColorInfo ci = m_ColorTable[(int)color];
            return ci.BaseHue + (hash == 0 ? Utility.Random(ci.Shades) : Math.Abs(hash) % ci.Shades);
        }
        public static ColorInfo GetColorInfo(ColorSelect color)
        {
            try
            {
                return m_ColorTable[(int)color];
            }
            catch
            {
                return m_ColorTable[0];
            }
        }
        #region Player/Account Reward Hue
        public static int RewardHue(PlayerMobile player)
        {
            List<int> used = new();
            foreach (var m in Server.World.Mobiles.Values)
                if (m is PlayerMobile pm && pm.AccessLevel == AccessLevel.Player)
                    used.Add(pm.RewardHue);

            try
            {
                Accounting.Account acct = (player.Account as Accounting.Account);
                // we try for uniqueness, but it's impossible.
                int[] hues = new int[] {
                    Utility.RandomSpecialHue(GetStableHashCodeV2(acct.Username)),
                    Utility.RandomSpecialHue(GetStableHashCodeV1(acct.Username)),
                    Utility.RandomSpecialHue(GetStableHashCodeV2(acct.Username + acct.Created.ToLongDateString())),
                    Utility.RandomSpecialHue(GetStableHashCodeV1(acct.Username + acct.Created.ToLongDateString())),
                    Utility.RandomSpecialHue(GetStableHashCodeV1(acct.Username) + GetStableHashCodeV2(acct.Username)),
                    Utility.RandomSpecialHue(GetStableHashCodeV1(acct.Username + acct.Created.ToLongDateString()) + GetStableHashCodeV2(acct.Username + acct.Created.ToLongDateString())),
                 };

                foreach (int hue in hues)
                    if (!used.Contains(hue))
                        return hue;

                // we did our best - punt
                return Utility.RandomSpecialHue();
            }
            catch { }

            return 0;
        }
        #endregion
        public static int RandomMetalHue()
        {
            return Random(2401, 30);
        }

        public static int RandomCraftMetalHue()
        {
            return RandomList(
                0x973,  // "Dull Copper"
                0x966,  // "Shadow Iron"
                0x96D,  // "Copper"
                0x972,  // "Bronze"
                0x8A5,  // "Gold"
                0x979,  // "Agapite"
                0x89F,  // "Verite"
                0x8AB   // "Valorite"
                );
        }

        /*
         * new CraftResourceInfo( 0x000, 1053109, "Iron",          CraftAttributeInfo.Blank,       CraftResource.Iron,             typeof( IronIngot ),        typeof( IronOre ),          typeof( Granite ) ),
                new CraftResourceInfo( 0x973, 1053108, "Dull Copper",   CraftAttributeInfo.DullCopper,  CraftResource.DullCopper,       typeof( DullCopperIngot ),  typeof( DullCopperOre ),    typeof( DullCopperGranite ) ),
                new CraftResourceInfo( 0x966, 1053107, "Shadow Iron",   CraftAttributeInfo.ShadowIron,  CraftResource.ShadowIron,       typeof( ShadowIronIngot ),  typeof( ShadowIronOre ),    typeof( ShadowIronGranite ) ),
                new CraftResourceInfo( 0x96D, 1053106, "Copper",        CraftAttributeInfo.Copper,      CraftResource.Copper,           typeof( CopperIngot ),      typeof( CopperOre ),        typeof( CopperGranite ) ),
                new CraftResourceInfo( 0x972, 1053105, "Bronze",        CraftAttributeInfo.Bronze,      CraftResource.Bronze,           typeof( BronzeIngot ),      typeof( BronzeOre ),        typeof( BronzeGranite ) ),
                new CraftResourceInfo( 0x8A5, 1053104, "Gold",          CraftAttributeInfo.Golden,      CraftResource.Gold,             typeof( GoldIngot ),        typeof( GoldOre ),          typeof( GoldGranite ) ),
                new CraftResourceInfo( 0x979, 1053103, "Agapite",       CraftAttributeInfo.Agapite,     CraftResource.Agapite,          typeof( AgapiteIngot ),     typeof( AgapiteOre ),       typeof( AgapiteGranite ) ),
                new CraftResourceInfo( 0x89F, 1053102, "Verite",        CraftAttributeInfo.Verite,      CraftResource.Verite,           typeof( VeriteIngot ),      typeof( VeriteOre ),        typeof( VeriteGranite ) ),
                new CraftResourceInfo( 0x8AB, 1053101, "Valorite", 
         */
        public static int ClipDyedHue(int hue)
        {
            if (hue < 2)
                return 2;
            else if (hue > 1001)
                return 1001;
            else
                return hue;
        }

        public static int RandomSpeechHue()
        {

            return RandomSpecialHue();
            /*
            // we used to call RandomDyedHue() for NPC speech which often lead to nearly unreadable text:
            //  lights yellows, pinks and other very light colors were virtually unreadable in one's journal. 
            //  I've picked this colors because they have a nice contrast and are easily readable.
            //  We can certainly add to this list as we wish.
            return Utility.RandomList(
                0x1DC1, 0x2D6, 0x265, 0x44, 0x12F1, 0x1B0, 0x851, 0xF1, 0x701,
                0x3261, 0x3C6, 0x1F71, 0x161, 0x169, 0x561, 0x2861, 0x801, 0x6B1,
                0x2691, 0x1281, 0x1461, 0x3CB, 0x22C1, 0x15A, 0x10F1, 0x291, 0x2BE,
                0x25F1, 0xCF1, 0x3351, 0x131, 0xC51, 0x14B, 0x26E, 0x7A, 0x5C, 0x741,
                0x1331, 0x282, 0xF11, 0xDE, 0x1C1, 0x308, 0x3301, 0x1EB1, 0x2641,
                0x6B1, 0x19D1, 0xCF1, 0x1F, 0x7A1, 0xCF, 0x1D71, 0x1F71, 0x1A61, 0xD9,
                0x31D1, 0x85, 0x308, 0x2501, 0x3531, 0x317, 0x1B91, 0x273, 0xEE, 0x6B1,
                0x1F61, 0x4F1, 0x18C1, 0x3DA1, 0x2721);*/
        }
        public static int RandomDyedHue()
        {
            return Random(2, 1000);
        }

        public static int ClipSkinHue(int hue)
        {
            if (hue < 1002)
                return 1002;
            else if (hue > 1058)
                return 1058;
            else
                return hue;
        }

        public static int RandomSkinHue()
        {
            return Random(1002, 57) | 0x8000;
        }

        public static int ClipHairHue(int hue)
        {
            if (hue < 1102)
                return 1102;
            else if (hue > 1149)
                return 1149;
            else
                return hue;
        }

        public static int RandomHairHue()
        {
            return Random(1102, 48);
        }

        private static SkillName[] m_AllSkills = new SkillName[]
            {
                SkillName.Alchemy,
                SkillName.Anatomy,
                SkillName.AnimalLore,
                SkillName.ItemID,
                SkillName.ArmsLore,
                SkillName.Parry,
                SkillName.Begging,
                SkillName.Blacksmith,
                SkillName.Fletching,
                SkillName.Peacemaking,
                SkillName.Camping,
                SkillName.Carpentry,
                SkillName.Cartography,
                SkillName.Cooking,
                SkillName.DetectHidden,
                SkillName.Discordance,
                SkillName.EvalInt,
                SkillName.Healing,
                SkillName.Fishing,
                SkillName.Forensics,
                SkillName.Herding,
                SkillName.Hiding,
                SkillName.Provocation,
                SkillName.Inscribe,
                SkillName.Lockpicking,
                SkillName.Magery,
                SkillName.MagicResist,
                SkillName.Tactics,
                SkillName.Snooping,
                SkillName.Musicianship,
                SkillName.Poisoning,
                SkillName.Archery,
                SkillName.SpiritSpeak,
                SkillName.Stealing,
                SkillName.Tailoring,
                SkillName.AnimalTaming,
                SkillName.TasteID,
                SkillName.Tinkering,
                SkillName.Tracking,
                SkillName.Veterinary,
                SkillName.Swords,
                SkillName.Macing,
                SkillName.Fencing,
                SkillName.Wrestling,
                SkillName.Lumberjacking,
                SkillName.Mining,
                SkillName.Meditation,
                SkillName.Stealth,
                SkillName.RemoveTrap,
				/*SkillName.Necromancy,
				SkillName.Focus,
				SkillName.Chivalry,
				SkillName.Bushido,
				SkillName.Ninjitsu*/
			};

        private static SkillName[] m_CombatSkills = new SkillName[]
            {
                SkillName.Archery,
                SkillName.Swords,
                SkillName.Macing,
                SkillName.Fencing,
                SkillName.Wrestling
            };

        private static SkillName[] m_CraftSkills = new SkillName[]
            {
                SkillName.Alchemy,
                SkillName.Blacksmith,
                SkillName.Fletching,
                SkillName.Carpentry,
                SkillName.Cartography,
                SkillName.Cooking,
                SkillName.Inscribe,
                SkillName.Tailoring,
                SkillName.Tinkering
            };

        public static SkillName RandomSkill()
        {
            return m_AllSkills[Utility.Random(m_AllSkills.Length - (Core.RuleSets.SERules() ? 0 : Core.RuleSets.AOSRules() ? 2 : 5))];
        }

        public static SkillName RandomCombatSkill()
        {
            return m_CombatSkills[Utility.Random(m_CombatSkills.Length)];
        }

        public static SkillName RandomCraftSkill()
        {
            return m_CraftSkills[Utility.Random(m_CraftSkills.Length)];
        }

        public static void FixPoints(ref Point3D top, ref Point3D bottom)
        {
            if (bottom.m_X < top.m_X)
            {
                int swap = top.m_X;
                top.m_X = bottom.m_X;
                bottom.m_X = swap;
            }

            if (bottom.m_Y < top.m_Y)
            {
                int swap = top.m_Y;
                top.m_Y = bottom.m_Y;
                bottom.m_Y = swap;
            }

            if (bottom.m_Z < top.m_Z)
            {
                int swap = top.m_Z;
                top.m_Z = bottom.m_Z;
                bottom.m_Z = swap;
            }
        }

        public static ArrayList BuildArrayList(IEnumerable enumerable)
        {
            IEnumerator e = enumerable.GetEnumerator();

            ArrayList list = new ArrayList();

            while (e.MoveNext())
            {
                list.Add(e.Current);
            }

            return list;
        }

        public static bool RangeCheck(IPoint2D p1, IPoint2D p2, int range)
        {
            return (p1.X >= (p2.X - range))
                && (p1.X <= (p2.X + range))
                && (p1.Y >= (p2.Y - range))
                && (p2.Y <= (p2.Y + range));
        }

        public static void FormatBuffer(TextWriter output, Stream input, int length)
        {
            output.WriteLine("        0  1  2  3  4  5  6  7   8  9  A  B  C  D  E  F");
            output.WriteLine("       -- -- -- -- -- -- -- --  -- -- -- -- -- -- -- --");

            int byteIndex = 0;

            int whole = length >> 4;
            int rem = length & 0xF;

            for (int i = 0; i < whole; ++i, byteIndex += 16)
            {
                StringBuilder bytes = new StringBuilder(49);
                StringBuilder chars = new StringBuilder(16);

                for (int j = 0; j < 16; ++j)
                {
                    int c = input.ReadByte();

                    bytes.Append(c.ToString("X2"));

                    if (j != 7)
                    {
                        bytes.Append(' ');
                    }
                    else
                    {
                        bytes.Append("  ");
                    }

                    if (c >= 0x20 && c < 0x80)
                    {
                        chars.Append((char)c);
                    }
                    else
                    {
                        chars.Append('.');
                    }
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }

            if (rem != 0)
            {
                StringBuilder bytes = new StringBuilder(49);
                StringBuilder chars = new StringBuilder(rem);

                for (int j = 0; j < 16; ++j)
                {
                    if (j < rem)
                    {
                        int c = input.ReadByte();

                        bytes.Append(c.ToString("X2"));

                        if (j != 7)
                        {
                            bytes.Append(' ');
                        }
                        else
                        {
                            bytes.Append("  ");
                        }

                        if (c >= 0x20 && c < 0x80)
                        {
                            chars.Append((char)c);
                        }
                        else
                        {
                            chars.Append('.');
                        }
                    }
                    else
                    {
                        bytes.Append("   ");
                    }
                }

                output.Write(byteIndex.ToString("X4"));
                output.Write("   ");
                output.Write(bytes.ToString());
                output.Write("  ");
                output.WriteLine(chars.ToString());
            }
        }

        public static bool NumberBetween(double num, int bound1, int bound2, double allowance)
        {
            if (bound1 > bound2)
            {
                int i = bound1;
                bound1 = bound2;
                bound2 = i;
            }

            return (num < bound2 + allowance && num > bound1 - allowance);
        }

        public static int RandomHair(bool female)   //Random hair doesn't include baldness
        {
            switch (Utility.Random(9))
            {
                case 0: return 0x203B;  //Short
                case 1: return 0x203C;  //Long
                case 2: return 0x203D;  //Pony Tail
                case 3: return 0x2044;  //Mohawk
                case 4: return 0x2045;  //Pageboy
                case 5: return 0x2047;  //Afro
                case 6: return 0x2049;  //Pig tails
                case 7: return 0x204A;  //Krisna
                default: return (female ? 0x2046 : 0x2048); //Buns or Receeding Hair
            }
        }

        public static int RandomFacialHair(bool female)
        {
            if (female)
                return 0;

            int rand = Utility.Random(7);

            return ((rand < 4) ? 0x203E : 0x2047) + rand;
        }

        public static void AssignRandomHair(Mobile m)
        {
            AssignRandomHair(m, true);
        }
        public static void AssignRandomHair(Mobile m, int hue)
        {
            m.HairItemID = RandomHair(m.Female);
            m.HairHue = hue;
        }
        public static void AssignRandomHair(Mobile m, bool randomHue)
        {
            m.HairItemID = RandomHair(m.Female);

            if (randomHue)
                m.HairHue = RandomHairHue();
        }

        public static void AssignRandomFacialHair(Mobile m)
        {
            AssignRandomFacialHair(m, true);
        }
        public static void AssignRandomFacialHair(Mobile m, int hue)
        {
            m.FacialHairHue = RandomFacialHair(m.Female);
            m.FacialHairHue = hue;
        }
        public static void AssignRandomFacialHair(Mobile m, bool randomHue)
        {
            m.FacialHairItemID = RandomFacialHair(m.Female);

            if (randomHue)
                m.FacialHairHue = RandomHairHue();
        }

        public static unsafe float IntToFloatBits(int value)
        {
            return *(float*)(&value);
        }

        public static unsafe int FloatToIntBits(float value)
        {
            return *(int*)(&value);
        }

        public static int GetDefaultLabel(int itemID)
        {
            if (itemID < 0x4000)
                return 1020000 + itemID;
            else if (itemID < 0x8000)
                return 1078872 + itemID;
            else
                return 1084024 + itemID;
        }

        #region Exponential Distribution

        public enum Exponent : int
        {
            d1_00 = 100,
            d0_50 = 50,
            d0_33 = 33,
            d0_25 = 25,
            d0_20 = 20,
            d0_15 = 15,
            d0_10 = 10,
        }
        public static int RandomMinMaxExp(int table_size, Exponent exponent)
        {
            return RandomMinMaxExp(min: 0, max: table_size - 1, exponent);
        }
        public static int RandomMinMaxExp(int min, int max, Exponent exponent)
        {
            if (min < 0)
                min = 0;

            if (max < 0)
                max = 0;

            if (min == max)
                return min;

            if (min > max)
            {
                int tmp = min;
                min = max;
                max = tmp;
            }

            int count = max - min + 1;

            Exponential exp = GetExponential(exponent);

            exp.Fill(count);

            double total = 0.0;

            for (int i = 0; i < count; i++)
                total += exp[i];

            double rnd = Utility.RandomDouble() * total;

            for (int i = 0; i < count; i++)
            {
                if (rnd < exp[i])
                    return min + i;
                else
                    rnd -= exp[i];
            }

            return min;
        }

        private static readonly Dictionary<Exponent, Exponential> m_Exponentials = new Dictionary<Exponent, Exponential>();

        private static Exponential GetExponential(Exponent exponent)
        {
            Exponential exp;

            if (m_Exponentials.TryGetValue(exponent, out exp))
                return exp;

            return m_Exponentials[exponent] = new Exponential((int)exponent / 100.0);
        }

        private class Exponential
        {
            private double m_Exponent;
            private double[] m_Values;

            public double this[int index] { get { return m_Values[index]; } }

            public Exponential(double exponent)
            {
                m_Exponent = exponent;
                m_Values = new double[0];
            }

            public void Fill(int size)
            {
                int start = m_Values.Length;

                if (m_Values.Length < size)
                {
                    double[] temp = m_Values;

                    m_Values = new double[size];

                    Array.Copy(temp, m_Values, temp.Length);
                }

                for (int i = start; i < m_Values.Length; i++)
                    m_Values[i] = Math.Exp(-m_Exponent * i);
            }
        }

        #endregion

        public static string SplitCamelCase(string str)
        {
            return System.Text.RegularExpressions.Regex.Replace(str, "([A-Z])", " $1", System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
        }

        public static string CamelCase(string input)
        {
            string[] words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }

        #region Serialization
        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(LoadUtilityData);
            EventSink.WorldSave += new WorldSaveEventHandler(SaveUtilityData);
            EventSink.WorldLoad += new WorldLoadEventHandler(LoadUOMusicInfo);
        }

        public static void LoadUOMusicInfo()
        {
            //System.Console.WriteLine("Loading music info...");
            //Utility.TimeCheck tc = new Utility.TimeCheck();
            //tc.Start();
            //LoadUOMusicInfoDefaults();
            //UOMusicDurationCompiler.MusicInfoCompiler.Compile();
            //tc.End();
            //Console.WriteLine("Music info loaded in {0}", tc.TimeTaken);
            //return;
        }
        public static void LoadUOMusicInfoDefaults()
        {
            //foreach (var name in (MusicName[])Enum.GetValues(typeof(MusicName)))
            //    UOMusicInfo[(int)name] = (0.0, false);
        }
        public static void LoadUtilityData()
        {
            if (!File.Exists("Saves/UtilityData.bin"))
                return;

            Console.WriteLine("Utility Data Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/UtilityData.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            m_FileSerial = (int)reader.ReadInt();
                            break;
                        }

                    default:
                        {
                            throw new Exception("Invalid UtilityData.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.ConsoleWriteLine("Error reading UtilityData.bin, using default values:", ConsoleColor.Red);
            }
        }
        public static void SaveUtilityData(WorldSaveEventArgs e)
        {
            Console.WriteLine("Utility Data Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/UtilityData.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(m_FileSerial);
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Utility.ConsoleWriteLine("Error writing UtilityData.bin: {0}", ConsoleColor.Red, ex.ToString());
            }
        }
        #endregion Serialization
    }
    #region Reversible Shuffle
    public static class ShuffleExtensions
    {
        public static int[] GetShuffleExchanges(int size, int key)
        {
            int[] exchanges = new int[size - 1];
            var rand = new Random(key);
            for (int i = size - 1; i > 0; i--)
            {
                int n = rand.Next(i + 1);
                exchanges[size - 1 - i] = n;
            }
            return exchanges;
        }

        public static string Shuffle(this string toShuffle, int key)
        {
            int size = toShuffle.Length;
            char[] chars = toShuffle.ToArray();
            var exchanges = GetShuffleExchanges(size, key);
            for (int i = size - 1; i > 0; i--)
            {
                int n = exchanges[size - 1 - i];
                char tmp = chars[i];
                chars[i] = chars[n];
                chars[n] = tmp;
            }
            return new string(chars);
        }

        public static string DeShuffle(this string shuffled, int key)
        {
            int size = shuffled.Length;
            char[] chars = shuffled.ToArray();
            var exchanges = GetShuffleExchanges(size, key);
            for (int i = 1; i < size; i++)
            {
                int n = exchanges[size - i - 1];
                char tmp = chars[i];
                chars[i] = chars[n];
                chars[n] = tmp;
            }
            return new string(chars);
        }
    }
    #endregion Reversible Shuffle
    #region Item and Mobile Memory
    /*	Item and Mobile Memory
	 *	The Memory system is passive, that is it doesn't use any timers for cleaning up old objects, it
	 *	simply cleans them up the next time it's called before processing the request. The one exception
	 *	to this rule is when you specify a 'Release' callback to be notified when an object expires.
	 *	In this case a timer is created to insure your callback is timely.
	 */
    public class Memory
    {
        public class ObjectMemory
        {
            private object m_context;
            public object Context                                   // return what we remember about this thing
            { get { return m_context; } set { m_context = value; } }
            private object m_object;
            public object Object { get { return m_object; } }       // return the thing we remember
            private double m_seconds;
            private DateTime m_Expiration;
            public DateTime RefreshTime { get { return DateTime.UtcNow + TimeSpan.FromSeconds(m_seconds); } }
            public DateTime Expiration { get { return m_Expiration; } set { m_Expiration = value; } }
            public TimerStateCallback m_OnReleaseEventHandler;
            public TimerStateCallback OnReleaseEventHandler { get { return m_OnReleaseEventHandler; } }

            public ObjectMemory(object ox, double seconds)
                : this(ox, null, seconds)
            {
            }

            public ObjectMemory(object ox, object context, double seconds)
                : this(ox, null, null, seconds)
            {
            }

            public ObjectMemory(object ox, object context, TimerStateCallback ReleaseEventHandler, double seconds)
            {
                m_object = ox;                                  // the thing to remember
                m_seconds = seconds;                            // how long to remember
                m_context = context;                            // what we remember about this object
                m_OnReleaseEventHandler = ReleaseEventHandler;  // release callback
                m_Expiration = RefreshTime;                     // when to delete
            }
        };

        private Hashtable m_MemoryCache = new Hashtable();
        public Hashtable MemoryCache => m_MemoryCache;
        private bool m_Tidying;

        public void TidyMemory()
        {   // we can reenter when we are called back via a user callback.
            //	For instance if the usercallback calls Recal() or anyother function that calls Tidy, we will reenter
            if (m_Tidying == false)
            {
                m_Tidying = true;
                // first clreanup the LOS cache
                ArrayList cleanup = new ArrayList();
                foreach (DictionaryEntry de in m_MemoryCache)
                {   // list expired elements
                    if (de.Value == null) continue;
                    if (DateTime.UtcNow > (de.Value as ObjectMemory).Expiration)
                        cleanup.Add(de.Key as object);
                }

                foreach (object ox in cleanup)
                {   // remove expired elements
                    if (ox == null) continue;
                    if (m_MemoryCache.Contains(ox))
                        Remove(ox);
                }
                m_Tidying = false;
            }
        }

        public void WipeMemory()
        {
            m_MemoryCache.Clear();
        }

        // you should be calling Forget() to remove objects.
        //	This is called internally
        public void Remove(object ox)
        {   // DO NOT CALL TIDY HERE
            if (m_MemoryCache.Contains(ox))
            {   // call user defined cleanup
                ObjectMemory om = m_MemoryCache[ox] as ObjectMemory;
                if (om.m_OnReleaseEventHandler != null)
                    om.m_OnReleaseEventHandler(om.Context);
                m_MemoryCache.Remove(ox);
            }
        }

        public int Count
        {
            get
            {
                TidyMemory();
                return m_MemoryCache.Count;
            }
        }

        public void Remember(object ox, double seconds)
        {   // preserve the previous context if there was one
            ObjectMemory om = Recall(ox);
            object temp = (om != null) ? om.Context : null;
            Remember(ox, temp, seconds);
        }

        public void Remember(object ox, object context, double seconds)
        {
            Remember(ox, context, null, seconds);
        }

        public void Remember(object ox, object context, TimerStateCallback releaseHandler, double seconds)
        {
            TidyMemory();
            if (ox == null) return;
            if (Recall(ox) != null)
            {   // we already know about this guy - just update with a temporary expiration.
                //  This expiration is temporary since any subsequent refreshes will revert to the stored m_seconds refresh.
                (m_MemoryCache[ox] as ObjectMemory).Expiration = DateTime.UtcNow + TimeSpan.FromSeconds(seconds);
                return;
            }
            m_MemoryCache[ox] = new ObjectMemory(ox, context, releaseHandler, seconds);

            if (releaseHandler != null)
                Timer.DelayCall(TimeSpan.FromSeconds(seconds), new TimerStateCallback(Remove), ox);
        }

        public void Forget(object ox)
        {
            TidyMemory();
            if (ox == null) return;
            if (m_MemoryCache.Contains(ox))
                Remove(ox);
        }

        public bool Recall(Mobile mx)
        {
            return Recall(mx as object) != null;
        }

        public ObjectMemory Recall(object ox)
        {
            TidyMemory();
            if (ox == null) return null;
            if (m_MemoryCache.Contains(ox))
                return m_MemoryCache[ox] as ObjectMemory;
            return null;
        }

        public void Refresh(object ox, object context, double? seconds = null)
        {
            TidyMemory();
            if (ox == null) return;
            if (m_MemoryCache.Contains(ox))
            {
                if (seconds == null)
                    (m_MemoryCache[ox] as ObjectMemory).Expiration = (m_MemoryCache[ox] as ObjectMemory).RefreshTime;
                else
                    (m_MemoryCache[ox] as ObjectMemory).Expiration = DateTime.UtcNow + TimeSpan.FromSeconds(seconds.GetValueOrDefault());
                (m_MemoryCache[ox] as ObjectMemory).Context = context;
            }
        }

        public void Refresh(object ox)
        {
            // preserve the previous context if there was one
            ObjectMemory om = Recall(ox);
            object temp = (om != null) ? om.Context : null;
            Refresh(ox, temp);
        }
    }
    #endregion Item and Mobile Memory

    #region GameTime
    // PST Is the Pacific Time Zone
    // In everyday usage, PST is often referred to as Pacific Time(PT) or the Pacific Time Zone.
    // This can add a bit of confusion as the term Pacific Time does not differentiate between standard time and Daylight Saving Time,
    // so Pacific Time switches between PST and PDT in areas that use DST during part of the year.
    // https://www.timeanddate.com/time/zones/pst
    public class AdjustedDateTime
    {
        private const int GameTimeOffset = -8;  // UTC -8 == pacific time

        public static string GameTimezone
        {
            get
            {
                var allTimeZones = TimeZoneInfo.GetSystemTimeZones();
                var newTimeZone = allTimeZones.FirstOrDefault(x => x.BaseUtcOffset == new TimeSpan(GameTimeOffset, 0, 0));
                var actual = newTimeZone.StandardName;
                return actual;
            }
        }
        public static string ServerTimezone
        {
            get
            {
                var allTimeZones = TimeZoneInfo.GetSystemTimeZones();
                var newTimeZone = allTimeZones.FirstOrDefault(x => x.BaseUtcOffset == new TimeSpan(0, 0, 0));
                var actual = newTimeZone.StandardName;
                return actual;
            }
        }

        public static DateTime LocalToUtc(string s)
        {
            try
            {
                DateTime temp = DateTime.Parse(s);
                return TimeZoneInfo.ConvertTimeToUtc(temp, TimeZoneInfo.Local);
            }
            catch
            { return DateTime.MinValue; }
        }

        // GameTimeSansDst is centered around a flattened Pacific Time.
        //  Flattening removes the effects of daylight savings time. 
        //  This flattening keeps game time linear which is important for Cron Jobs. E.g.,
        //  If you want something to fire every hour, a change in DST will mess that up.
        //  a side-effect of this is that part of the year,
        //  during DST, GameTimeSansDst and Pacific time will diverge by one hour.
        //  GameTimeSansDst should not be used for any display purposes, it's purely a timing function
        //  See: GameTime below
        public static DateTime GameTimeSansDst
        {   // Flatten time: time that is not adjusted for daylight savings time
            //  by adding 1 hour to the time when we enter DST
            get { return GetTime(GameTimeOffset, sansDST: true); }
        }
        // GameTime is the time we use for things like scheduled events and it's the time we display
        //  to the user ([time command,) and how the AES displays an events progression
        //  Note: the edge case here is where DST changes in the middle of an AES event. In this case
        //  we should continue to display GameTime, but maintain timers on GameTimeSansDst.
        // See: GameTimeSansDst above
        public static DateTime GameTime { get { return GetTime(GameTimeOffset, sansDST: false); } }
        // ServerTime is the time in whatever timezone the server is running in
        public static DateTime ServerTime { get { return DateTime.UtcNow; } }
        // CronTick is guaranteed to only tick once per minute
        static int lastCronMinute = DateTime.UtcNow.Minute;
        public static bool CronTick
        {
            get
            {
                int minute = DateTime.UtcNow.Minute;
                bool change = minute != lastCronMinute;
                lastCronMinute = minute;
                return change;
            }
        }
        public static DateTime GetTime(int offset, bool sansDST = false)
        {
            DateTime localTime = DateTime.UtcNow.AddHours(offset);
            if (!sansDST && TimeZoneInfo.Local.IsDaylightSavingTime(localTime))
                localTime = localTime.AddHours(1);

            // we need to switch the 'Kind' away from Utc
            return new DateTime(localTime.Ticks);
        }
    }
    #endregion GameTime
}