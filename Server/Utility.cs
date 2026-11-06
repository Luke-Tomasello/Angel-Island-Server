/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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
 *  10/21/2026, Adam
 *      StackItems: the merged stack keeps the StoreBought mark if either part had it.
 *  10/6/2026
 *      Structural cleanup: members regrouped into family regions; cohesive families moved to
 *      Utility.*.cs partial files, and Memory / AdjustedDateTime / ShuffleExtensions moved to
 *      their own files. No behavior change.
 *  8/13/2024, Adam
 *      Add a notion of BuildInfoDir. This directory contains the "Core 6.info" file
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

using Scripts.Misc;
using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Regions;
using Server.Targeting;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using static Server.Items.DyeTubToning;
namespace Server
{

    public partial class Utility
    {
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

        #region Random & Distributions
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
        public static void RandomBytes(byte[] buffer)
        {
            //RandomImpl.NextBytes(buffer);
            Monitor.WriteLine("RandomBytes(byte[] buffer) not implemented", ConsoleColor.Red);
        }
        public static void Shuffle(IList list)
        {   //  Fisher-Yates shuffle
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = m_Random.Next(n + 1);
                object? value = list[k];
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
        private static Random m_Random = new Random();

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
        public static T Random<T>(IList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            if (list.Count == 0)
                throw new ArgumentException("Collection cannot be empty.", nameof(list));

            return list[m_Random.Next(list.Count)];
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
            Exponential? exp;

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

        #endregion Exponential Distribution
        #endregion Random & Distributions

        #region Hues & Appearance
        //}
        public static bool IsStandardHairHue(int hue)
        {
            hue &= 0x3FFF;                 // match creation normalization
            return hue >= 1102 && hue <= 1149;
        }
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
        #endregion Hair
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

        public static int RandomSpecialRedHue()
        {
            return Utility.RandomList(1640, 1641, 1642, 1643, 1644);
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
            while ((hue = RandomSlimeHue()) == (Core.RuleSets.SiegeRules() ? 0x88A5 : 0)) ;
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
            Brown,           // Brown            2013 - 2017 (5)  // NOTE: base=2013, shades=5
            DarkBlue,        // Dark Blue        1303 - 1308 (6)
            ForestGreen,     // Forest Green     1420 - 1426 (7)
            Pink,            // Pink             1619 - 1626 (8)
            Crimson,         // Crimson          1640 - 1644 (5) - renamed from "Red" to "Crimson"
            Olive,           // Olive            2001 - 2005 (5)

            DullCopper,      // Dull Copper      2419 - 2424 (6)
            ShadowIron,      // Shadow Iron      2406 - 2412 (7)
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
            new ColorInfo(1230, 6, "Violet"),          // Violet           1230 - 1235 (6)
            new ColorInfo(1501, 8, "Tan"),             // Tan              1501 - 1508 (8)
            new ColorInfo(2013, 5, "Brown"),           // Brown            2013 - 2017 (5)  // NOTE: base=2013, shades=5
            new ColorInfo(1303, 6, "Dark Blue"),       // Dark Blue        1303 - 1308 (6)
            new ColorInfo(1420, 7, "Forest Green"),    // Forest Green     1420 - 1426 (7)
            new ColorInfo(1619, 8, "Pink"),            // Pink             1619 - 1626 (8)
            new ColorInfo(1640, 5, "Crimson"),         // Crimson          1640 - 1644 (5)
            new ColorInfo(2001, 5, "Olive"),           // Olive            2001 - 2005 (5)

            new ColorInfo(2419, 6, "Dull Copper"),     // Dull Copper      2419 - 2424 (6)
            new ColorInfo(2406, 7, "Shadow Iron"),     // Shadow Iron      2406 - 2412 (7)
            new ColorInfo(2413, 6, "Copper"),          // Copper           2413 - 2418 (6)
            new ColorInfo(2414, 5, "Bronze"),          // Bronze           2414 - 2418 (5)
            new ColorInfo(2213, 6, "Gold"),            // Gold             2213 - 2218 (6)
            new ColorInfo(2425, 6, "Agapite"),         // Agapite          2425 - 2430 (6)
            new ColorInfo(2207, 6, "Verite"),          // Verite           2207 - 2212 (6)
            new ColorInfo(2219, 6, "Valorite"),        // Valorite         2219 - 2224 (6)

            new ColorInfo(2113, 6, "Red"),             // Red              2113 - 2118 (6)
            new ColorInfo(2119, 6, "Blue"),            // Blue             2119 - 2124 (6)
            new ColorInfo(2126, 5, "Green"),           // Green            2126 - 2130 (5)
                                                       // yellow is a duplicate of Gold above
            // new ColorInfo(2213, 6, "Yellow"),       // Yellow           2213 - 2218 (6)
        };

        // special dye tub colors
        public static int RandomSpecialHue(string key)
        {
            // Stable: never allow 0 to fall into the random path.
            int h = GetStableHashCode(key);
            if (h == 0)
                h = 1;

            return RandomSpecialHue(h);
        }

        public static int RandomSpecialHue(int hash = 0)
        {
            // Random selection when no hash provided.
            if (hash == 0)
            {
                ColorInfo ci0 = m_ColorTable[Utility.Random(m_ColorTable.Length)];
                return ci0.BaseHue + Utility.Random(ci0.Shades);
            }

            // Correctness: avoid Math.Abs(int.MinValue) overflow; force non-negative.
            int h = hash & 0x7fffffff;

            ColorInfo ci = m_ColorTable[h % m_ColorTable.Length];
            return ci.BaseHue + (h % ci.Shades);
        }

        public static int RandomSpecialHue(ColorSelect color, string key)
        {
            int h = GetStableHashCode(key);
            if (h == 0)
                h = 1;

            return RandomSpecialHue(color, h);
        }

        public static int RandomSpecialHue(ColorSelect color, int hash = 0)
        {
            ColorInfo ci = m_ColorTable[(int)color];

            if (hash == 0)
                return ci.BaseHue + Utility.Random(ci.Shades);

            int h = hash & 0x7fffffff;
            return ci.BaseHue + (h % ci.Shades);
        }

        public static int RewardHue(PlayerMobile player)
        {
            try
            {
                Accounting.Account? acct = (player.Account as Accounting.Account);
                return Utility.RandomSpecialHue(GetStableHashCode(acct.Username + player.Name + acct.Created.ToLongDateString()));
            }
            catch { }

            return 0;
        }
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
        #endregion Hues & Appearance

        #region Strings, Parsing & Formatting

        public static bool TryParseHexUInt(string text, out uint value)
        {
            value = 0;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();

            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);

            if (text.Length == 0)
                return false;

            return uint.TryParse(
                text,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out value);
        }

        public static string ToOrdinal(int n)
        {
            int abs = Math.Abs(n);
            int lastTwo = abs % 100;

            if (lastTwo >= 11 && lastTwo <= 13)
                return n + "th";

            switch (abs % 10)
            {
                case 1: return n + "st";
                case 2: return n + "nd";
                case 3: return n + "rd";
                default: return n + "th";
            }
        }

        public static bool StartsWithVowel(string text)
        {
            String vowels = "aeiou";
            if (vowels.IndexOf(text.ToLower().Substring(0, 1)) != -1)
            {
                return true; // Start char is vowel
            }
            return false;
        }
        public static bool StringToInt(string? s, ref int result)
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
        #region Text Macros
        public static string? ExpandMacros(Mobile from, Item? item, string text)
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
                Guilds.Guild? guild = pm.Guild as Guilds.Guild;
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
        #endregion Text Macros

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
        public static string[] GetCustomEnumNames(Type type)
        {
            object[] attrs = type.GetCustomAttributes(typeof(CustomEnumAttribute), false);

            if (attrs.Length == 0)
                return new string[0];

            CustomEnumAttribute? ce = attrs[0] as CustomEnumAttribute;

            if (ce == null)
                return new string[0];

            return ce.Names;
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
        public static string GetObjectDisplayName(object? thing, Type t)
        {
            Item? item = thing as Item;
            Mobile? mobile = thing as Mobile;
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
                List<string> components = new(path.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                if (components.Count < 9)
                    return path;

                List<string> left = components.Take<string>(components.Count / 2).ToList();
                List<string> right = components.Skip(components.Count / 2).Take<string>(components.Count).ToList();
                for (int ix = 0; ix < 100; ix++)
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
        #region Encodings
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
        #endregion Encodings

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
        #region RunUO Parsing

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

        /// <summary>
        /// Converts a string to int. Supports decimal and "0x" hex format.
        /// Returns defaultValue if conversion fails.
        /// </summary>
        public static int ToInt32(string? value, int defaultValue = 0)
        {
            if (string.IsNullOrWhiteSpace(value))
                return defaultValue;

            value = value.Trim();

            try
            {
                if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt32(value.Substring(2), 16);

                return int.TryParse(value, out int result) ? result : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Nullable version - returns null if conversion fails.
        /// </summary>
        public static int? ToInt32Nullable(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            value = value.Trim();

            try
            {
                if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    return Convert.ToInt32(value.Substring(2), 16);

                return int.TryParse(value, out int result) ? result : null;
            }
            catch
            {
                return null;
            }
        }
        public static uint ToUInt32(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                    return 0;

                if (value.StartsWith("0x"))
                {
                    return Convert.ToUInt32(value.Substring(2), 16);
                }
                else
                {
                    uint result = 0;
                    if (uint.TryParse(value, out result))
                        return result;

                    return result;
                }
            }
            catch
            {
                return 0;
            }
        }
        #endregion RunUO Parsing

        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(str))]
        public static string? Intern(string? str)
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

        public static int GetDefaultLabel(int itemID)
        {
            if (itemID < 0x4000)
                return 1020000 + itemID;
            else if (itemID < 0x8000)
                return 1078872 + itemID;
            else
                return 1084024 + itemID;
        }
        #region Camel Case

        public static bool LooksCamelCase(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            for (int i = 1; i < text.Length; i++)
                if (char.IsUpper(text[i]))
                    return true;

            return false;
        }
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
        #endregion Camel Case
        #endregion Strings, Parsing & Formatting

        #region Numbers, Bits & Collections
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

        public static unsafe float IntToFloatBits(int value)
        {
            return *(float*)(&value);
        }

        public static unsafe int FloatToIntBits(float value)
        {
            return *(int*)(&value);
        }
        #endregion Numbers, Bits & Collections

        #region Core Geometry

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
        public static object GetArrayCap(Array array, int index)
        {
            return GetArrayCap(array, index, null);
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

        public static bool RangeCheck(IPoint2D p1, IPoint2D p2, int range)
        {
            return (p1.X >= (p2.X - range))
                && (p1.X <= (p2.X + range))
                && (p1.Y >= (p2.Y - range))
                && (p2.Y <= (p2.Y + range));
        }
        #endregion Core Geometry

        #region Time
        public static string FormatShortTime(TimeSpan ts)
        {
            if (ts.TotalSeconds <= 0) return "a moment";

            if (ts.TotalMinutes >= 1)
                return string.Format("{0} minute{1}", (int)Math.Ceiling(ts.TotalMinutes), ts.TotalMinutes >= 2 ? "s" : "");

            return string.Format("{0} second{1}", (int)Math.Ceiling(ts.TotalSeconds), ts.TotalSeconds >= 2 ? "s" : "");
        }

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
        #endregion Time

        #region Identifiers, Hashing, Crypto & Serialization
        #region IO and File Identifiers
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
        #endregion IO and File Identifiers
        /// <summary>
        //  The GUID uniquely identifies a player/account, even after death
        /// </summary>
        /// <returns>uint uniquely identifying this player</returns>
        public static uint MakeGuid(string s1, string s2)
        {
            return (uint)Utility.GetStableHashCode(s1.ToLower() + s2.ToUpper(), version: 1);
        }
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
            string? type = reader.ReadString();
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
        public static void WriteObjectCfg(object o, string? fileName, bool overwrite)
        {
            if (o == null || fileName == null)
                return;

            LogHelper? logger = null;
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

        public static void EnsurePath(string path)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        #endregion Identifiers, Hashing, Crypto & Serialization

        #region Network, Host & Accounts
        public static PlayerMobile? ResolveFirstPlayerByAccountGuid(uint ownerGuid)
        {
            if (ownerGuid == 0)
                return null;

            foreach (Account acct in Accounts.Table.Values)
            {
                if (acct == null)
                    continue;

                if (acct.GUID != ownerGuid)
                    continue;

                // Found the account — return first valid mobile
                for (int i = 0; i < 5; i++)
                {
                    Mobile m = acct[i];

                    if (m is PlayerMobile pm && !pm.Deleted)
                        return pm;
                }

                // Account found, but no valid players
                return null;
            }

            // No account matched the GUID
            return null;
        }
        #region Shared Accounts
        public class AccountComparer : IComparer
        {
            public static readonly IComparer Instance = new AccountComparer();

            public AccountComparer()
            {
            }

            public int Compare(object? x, object? y)
            {
                if (x == null && y == null)
                    return 0;
                else if (x == null)
                    return -1;
                else if (y == null)
                    return 1;

                Account? a = x as Account;
                Account? b = y as Account;

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

            public int Compare(object? x, object? y)
            {
                DictionaryEntry a = (DictionaryEntry)x;
                DictionaryEntry b = (DictionaryEntry)y;

                ArrayList? aList = (ArrayList?)a.Value;
                ArrayList? bList = (ArrayList?)b.Value;

                return bList.Count - aList.Count;
            }
        }
        public static ArrayList GetAllSharedAccounts()
        {
            Hashtable table = new Hashtable();
            ArrayList? list;

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
                ArrayList? accts = (ArrayList?)de.Value;

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
        #endregion Shared Accounts

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


        public static string? GetHost()
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

        public static bool IsHostPrivate(string? host)
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

        public static bool IsHostPROD(string? host)
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

        public static bool IsHostTC(string? host)
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
        //SMD: merged in for runuo2.0 networking stuff
        public static int GetAddressValue(IPAddress address)
        {
#pragma warning disable 618
            return (int)address.Address;
#pragma warning restore 618
        }

        private static Dictionary<IPAddress, IPAddress>? _ipAddressTable;

        public static IPAddress Intern(IPAddress ipAddress)
        {
            if (_ipAddressTable == null)
            {
                _ipAddressTable = new Dictionary<IPAddress, IPAddress>();
            }

            IPAddress? interned;

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
        #endregion Network, Host & Accounts

        #region Console, Logging & Diagnostics
        public static void SimpleLog(string logFileName, Mobile sendMessage, string text)
        {
            if (sendMessage != null && text != null)
                sendMessage.SendMessage(text);
            SimpleLog(logFileName, text);
        }
        public static void SimpleLog(string logFileName, string? text, bool overwrite = false)
        {
            LogHelper logger = new LogHelper(logFileName, overwrite, sline: true);
            if (text != null)
                logger.Log(text);
            logger.Finish();
        }
        #region Breeding Tools

        private static Dictionary<Type, string> BCDefaultNameCache = new();
        public static void BreedingLog(BaseCreature? me, BaseCreature? you, string format, params object?[] args)
        {
            if (me != null && me.DebugMode != DebugFlags.None || you != null && you.DebugMode != DebugFlags.None)
            {   // How I feel about YOU
                string text = string.Format($"{DynName(me)} => {DynName(you)}: {string.Format(format, args)}");
                LogHelper logger = new LogHelper("Courtship.log", overwrite: false, sline: true);
                logger.Log(text);
                logger.Finish();
            }
        }
        public static string DynName(BaseCreature? bc)
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
                BaseCreature? temp = (BaseCreature?)Activator.CreateInstance(bc.GetType());
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
        #region Profiling

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
        #endregion Profiling
        #region Monitor
        public static class Monitor
        {
            public static void Write(string text, ConsoleColor color)
            {
                System.Console.Out.Flush();
                PushColor(color);
                System.Console.Write(text);
                PopColor();
                System.Console.Out.Flush();
            }
            public static void WriteLine(string format, ConsoleColor color, params object?[] args)
            {
                Monitor.WriteLine(string.Format(format, args), color);
            }
            public static void WriteLine(string text, ConsoleColor color)
            {
                System.Console.Out.Flush();
                PushColor(color);
                System.Console.WriteLine(text);
                if (m_ConsoleOutEcho != null)
                {
                    EnsurePath(m_ConsoleOutEcho);
                    File.AppendAllLines(m_ConsoleOutEcho, new string[] { text }, Encoding.UTF8);
                }
                PopColor();
                System.Console.Out.Flush();
            }

            public static void DebugOut(string text, ConsoleColor color = ConsoleColor.White)
            {
#if DEBUG
                Monitor.WriteLine(text, color);
#endif
            }
            private static string? m_ConsoleOutEcho = null;
            public static string? ConsoleOutEcho
            {
                get { return m_ConsoleOutEcho; }
                set { m_ConsoleOutEcho = value; }
            }
            public static void DebugOut(string format, ConsoleColor color = ConsoleColor.White, params object?[] args)
            {
                DebugOut(string.Format(format, args), color);
            }
            public static void ErrorOut(string text, ConsoleColor color)
            {
                Console.Error.Flush();
                PushColor(color);
                Console.Error.WriteLine(text);
                if (m_ConsoleOutEcho != null)
                {
                    EnsurePath(m_ConsoleOutEcho);
                    File.AppendAllLines(m_ConsoleOutEcho, new string[] { text });
                }
                PopColor();
                Console.Error.Flush();
            }
            public static void ErrorOut(string format, ConsoleColor color, params object?[] args)
            {
                ErrorOut(string.Format(format, args), color);
            }
        }
        #endregion Monitor

        public static string FileInfo([System.Runtime.CompilerServices.CallerFilePath] string filePath = "", [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            return string.Format("in {0} at line number {1}", GetShortPath(filePath), lineNumber);
        }
        #region Console Colors

        private static Stack<ConsoleColor> m_ConsoleColors = new Stack<ConsoleColor>();

        public static void ResetColor()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
        }
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
        #endregion Console Colors

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
        #endregion Console, Logging & Diagnostics

        #region Build & Version Info
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
        public static int BuildBuild()
        {
            try
            {
                // open our version info file
                string buildInfoFile = Path.Combine(Core.BuildInfoDir, "Core 6.info");
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the version
                string? line = sr.ReadLine();
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
                string buildInfoFile = Path.Combine(Core.BuildInfoDir, "Core 6.info");
                // open our version info file
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the build
                sr.ReadLine();
                //the next line of text will be the revision
                string? line = sr.ReadLine();
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
                string buildInfoFile = Path.Combine(Core.BuildInfoDir, "Core 6.info");
                // open our version info file
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the build
                sr.ReadLine();
                //the next line of text will be the revision
                sr.ReadLine();
                //the next line of text will be the major version
                string? line = sr.ReadLine();
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
                string buildInfoFile = Path.Combine(Core.BuildInfoDir, "Core 6.info");
                // open our version info file
                StreamReader sr = new StreamReader(buildInfoFile);
                //the first line of text will be the build
                sr.ReadLine();
                //the next line of text will be the revision
                sr.ReadLine();
                //the next line of text will be the major version
                sr.ReadLine();
                //the next line of text will be the minor version
                string? line = sr.ReadLine();
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
        #endregion Build & Version Info

        #region Inventory & Containers
        public static void EnsureBackpack(Mobile m)
        {
            if (m == null)
                return;

            if (m.Backpack == null)
            {
                Backpack pack = new Backpack();
                m.AddItem(pack);
            }
        }
        public static bool HasType(Mobile m, Type type)
        {
            if (m == null || type == null)
                return false;

            Container pack = m.Backpack;

            // no backpack
            if (pack == null)
                return false;

            ArrayList items = pack.GetDeepItems();

            foreach (Item item in items)
            {
                if (type.IsAssignableFrom(item.GetType()))
                    return true;
            }

            return false;
        }
        public static void AddBackpack(Mobile m, int hue = 0)
        {
            Container pack = m.Backpack;

            if (pack == null)
            {
                pack = new Backpack();
                pack.Movable = false;
                pack.Hue = hue;
                m.AddItem(pack);
            }
        }
        // Move all items from 'from' -> 'to', skipping any whose type matches the exclude list.
        // Exclusion is type-assignable: if exclude contains Container, all containers are skipped.
        public static void RecursiveMove(Container to, Container from, List<Type> excludeTypes)
        {
            if (to == null || from == null)
                return;

            // snapshot to avoid modifying the collection while iterating
            List<Item> snapshot = new List<Item>(from.Items);

            foreach (Item item in snapshot)
            {
                if (item == null || item.Deleted)
                    continue;

                // still present?
                if (!from.Items.Contains(item))
                    continue;

                // skip excluded types (exact match or assignable)
                if (IsExcluded(item, excludeTypes))
                    continue;

                from.RemoveItem(item);
                to.AddItem(item);
            }
        }

        private static bool IsExcluded(Item item, List<Type> excludeTypes)
        {
            if (excludeTypes == null || excludeTypes.Count == 0)
                return false;

            Type it = item.GetType();
            for (int i = 0; i < excludeTypes.Count; i++)
            {
                Type t = excludeTypes[i];
                if (t == null)
                    continue;

                // exclude if exact type or t is a base/interface of item's type
                if (t == it || t.IsAssignableFrom(it))
                    return true;
            }
            return false;
        }
        #region Secure Give
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
        public static Item? SecureGive(Mobile from, Item item)
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
        #endregion Secure Give
        #region Mobile Inventory Lists
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

        private static void CollectContainers(Container c, List<Item> result)
        {
            if (c == null || c.Deleted) return;

            foreach (var it in c.Items)
            {
                if (it is Container inner)
                {
                    result.Add(inner);
                    CollectContainers(inner, result);
                }
            }
        }
        public static List<Item> GetContainerItems(Container? c, bool recurse = true)
        {
            var result = new List<Item>();
            if (c == null || c.Deleted)
                return result;

            foreach (Item item in c.Items)
            {
                if (item == null || item.Deleted)
                    continue;

                result.Add(item);

                if (recurse && item is Container inner)
                    result.AddRange(GetContainerItems(inner, true));
            }

            return result;
        }
        #endregion Mobile Inventory Lists

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
                        if (in_list[jx].StoreBought)
                            in_list[ix].StoreBought = true;     // merchant provenance is sticky
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
        public static void DropHolding(Mobile m)
        {
            Item held = m.Dragging;
            if (held != null)
            {
                held.ClearBounce();
                if (m.Backpack != null)
                {
                    m.Backpack.DropItem(held);
                }
            }
            m.Dragging = null;
        }

        public static Item? InventoryFind(Mobile m, Type type, bool BackpackOnly = false)
        {
            List<Item> list = Inventory(m, BackpackOnly);
            foreach (Item item in list)
                if (type.IsAssignableFrom(item.GetType()))
                    return item;

            return null;
        }
        public static Item? InventoryFind(List<Item> list, Type type)
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
        #endregion Inventory & Containers

        #region Mobile Helpers
        /// <summary>
        /// Returns the most recent aggressor involved in combat with <paramref name="owner"/>,
        /// based on Aggressors and Aggressed lists. Returns null if no valid aggressor is found.
        /// This is intended for "who/what killed X" style messaging and ignores friendly-fire logic.
        /// </summary>
        public static Mobile? GetLastAggressor(Mobile owner)
        {
            if (owner == null)
                return null;

            Mobile? last = null;
            DateTime lastTime = DateTime.MinValue;

            // Someone who attacked 'owner'
            for (int i = 0; i < owner.Aggressors.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)owner.Aggressors[i];

                if (info == null || info.Attacker == null)
                    continue;

                if (info.LastCombatTime > lastTime)
                {
                    lastTime = info.LastCombatTime;
                    last = info.Attacker;
                }
            }

            // Someone whom 'owner' attacked (they may have delivered the final blow back)
            for (int i = 0; i < owner.Aggressed.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)owner.Aggressed[i];

                if (info == null || info.Defender == null)
                    continue;

                if (info.LastCombatTime > lastTime)
                {
                    lastTime = info.LastCombatTime;
                    last = info.Defender;
                }
            }

            return last;
        }
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
        #region Pets
        public static BaseCreature? FindPet(Mobile master, string petName)
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
        public static int CountPets(Mobile master, uint range, bool includeMount)
        {
            if (master == null)
                return 0;

            int count = 0;

            foreach (Mobile m in master.Followers)
            {
                BaseCreature? pet = m as BaseCreature;
                if (pet != null && ShouldIncludePet(master, pet, range, includeMount))
                    count++;
            }

            return count;
        }

        public static int CountPets(Mobile master)
        {
            return CountPets(master, uint.MaxValue, true);
        }

        public static bool CanStablePets(Mobile master, uint range, bool includeMount)
        {
            if (master == null)
                return true;

            int pets = CountPets(master, range, includeMount);

            if (pets > 0 && AnimalTrainer.Table.ContainsKey(master))
                if (AnimalTrainer.Table[master].Count + pets >= BaseCreature.GetMaxStabled(master))
                    return false;

            return true;
        }

        public static bool CanStablePets(Mobile master)
        {
            return CanStablePets(master, uint.MaxValue, true);
        }
        //internalize
        public static int InternalizePets(Mobile master, uint range = uint.MaxValue, bool dismount = true)
        {
            if (master == null)
                return 0;

            bool includeMount = dismount;

            if (dismount)
            {
                IMount mt = master.Mount;
                if (mt != null)
                    mt.Rider = null;
            }

            List<BaseCreature> pets = new List<BaseCreature>();

            foreach (Mobile m in master.Followers)
            {
                BaseCreature? pet = m as BaseCreature;
                if (pet != null && ShouldIncludePet(master, pet, range, includeMount))
                    pets.Add(pet);
            }

            int internalized = 0;

            for (int i = 0; i < pets.Count; i++)
            {
                BaseCreature pet = pets[i];
                pet.MoveToIntStorage(PreserveLocation: true);
                internalized++;
            }

            return internalized;
        }
        public static int StablePets(Mobile master, uint range = uint.MaxValue, bool dismount = true)
        {
            if (master == null)
                return 0;

            bool includeMount = dismount;

            if (dismount)
            {
                IMount mt = master.Mount;
                if (mt != null)
                    mt.Rider = null;
            }

            List<BaseCreature> pets = new List<BaseCreature>();

            foreach (Mobile m in master.Followers)
            {
                BaseCreature? pet = m as BaseCreature;
                if (pet != null && ShouldIncludePet(master, pet, range, includeMount))
                    pets.Add(pet);
            }

            int stabled = 0;

            for (int i = 0; i < pets.Count; i++)
            {
                BaseCreature pet = pets[i];

                if (AnimalTrainer.Table.ContainsKey(master))
                    if (AnimalTrainer.Table[master].Count >= BaseCreature.GetMaxStabled(master))
                        continue;

                StablePet(master, pet);
                ChargeStableFee(master, pet);
                stabled++;
            }

            return stabled;
        }
        private static bool ShouldIncludePet(Mobile master, BaseCreature pet, uint range, bool includeMount)
        {
            if (pet == null || master == null)
                return false;

            if (!pet.Controlled || pet.ControlMaster != master)
                return false;

            if (!CanStablePet(pet))
                return false;

            // Explicit policy: whether the current mount is eligible for counting/stabling.
            if (!includeMount && IsCurrentMount(master, pet))
                return false;

            // Preserve legacy intent: internal-map mount is "in range" when included.
            if (includeMount && IsInternalMount(master, pet))
                return true;

            return WithinRangeOrUnlimited(master, pet, range);
        }
        private static bool WithinRangeOrUnlimited(Mobile master, Mobile pet, uint range)
        {
            if (range == uint.MaxValue)
                return true;

            int r = (range > int.MaxValue) ? int.MaxValue : (int)range;
            return master.GetDistanceToSqrt(pet) <= r;
        }
        private static bool IsCurrentMount(Mobile master, BaseCreature pet)
        {
            if (master == null || pet == null)
                return false;

            IMount im = master.Mount;
            return (im is BaseCreature mount && ReferenceEquals(mount, pet));
        }

        private static bool IsInternalMount(Mobile master, BaseCreature pet)
        {
            if (!IsCurrentMount(master, pet))
                return false;

            return pet.Map == Map.Internal;
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

            if (bc.Summoned)
                return false;

            if ((bc is PackLlama || bc is PackHorse || bc is Beetle) && (bc.Backpack != null && bc.Backpack.Items.Count > 0))
                return false;

            if (bc is BaseEscortable || bc is BaseHire)
                return false;

            return true;
        }
        #endregion Pets
        #endregion Mobile Helpers

        #region Item Helpers
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
        public static void NormalizeOnLift(Item item)
        {
            if (item == null)
                return;

            if (BaseVendor.StandardPricingDictionary.ContainsKey(item.GetType()))
            {
                object? o = null;
                try
                {   // smash all custom properties
                    o = Activator.CreateInstance(item.GetType());
                    CopyProperties(o as Item, item);
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
                CopyProperties(no_draw, item);
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
        #endregion Item Helpers

        #region Location & Ownership

        public static bool IsTown(string regionName)
        {
            if (string.IsNullOrEmpty(regionName)) return false;

            //this will do for the time being, until all are drdt 
            switch (regionName)
            {
                case "Cove":
                case "Britain":
                case "Jhelom":
                case "Minoc":
                case "Trinsic":
                case "Vesper":
                case "Yew":
                case "Serpent's Hold":
                case "Skara Brae":
                case "Nujel'm":
                case "Moonglow":
                case "Magincia":
                case "Buccaneer's Den":
                case "Delucia":
                case "Papua":
                    return true;
            }

            return false;

        }
        #region Township
        public static List<ITownshipItem> TownshipItems(TownshipStone stone, List<Type>? filter = null)
        {
            List<ITownshipItem> list = new();
            TownshipRegion? tsr = null;
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
        #endregion Township
        #region Township & House Access
        public static bool DefaultTownshipAndHouseAccess(Item item, Mobile from)
        {
            bool valid = Server.Township.TownshipItemHelper.IsOwner(item, from);
            BaseHouse? house = BaseHouse.FindHouseAt(item);

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

            BaseHouse? house = BaseHouse.FindHouseAt(item);
            if (house != null && house.IsFriend(from))
                return true;

            return false;
        }
        public static bool InTownshipOrHouse(Item item)
        {
            TownshipRegion itsr = TownshipRegion.GetTownshipAt(item.GetWorldLocation(), item.Map);
            if (itsr != null)
                return true;

            BaseHouse? house = BaseHouse.FindHouseAt(item);
            if (house != null)
                return true;

            return false;
        }
        public static bool InTownshipOrHouse(Mobile m)
        {
            TownshipRegion itsr = TownshipRegion.GetTownshipAt(m.Location, m.Map);
            if (itsr != null)
                return true;

            BaseHouse? house = BaseHouse.FindHouseAt(m);
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
        #endregion Township & House Access
        public static bool AnyoneCanSeeMe(BaseCreature bc)
        {
            if (bc == null || bc.Map == null)
                return false;

            IPooledEnumerable eable = bc.Map.GetMobilesInRange(bc.Location, 13);

            try
            {
                foreach (Mobile mob in eable)
                {
                    PlayerMobile? pm = mob as PlayerMobile;

                    if (pm == null)
                        continue;

                    // Ignore hidden staff
                    if (pm.Hidden && pm.AccessLevel >= AccessLevel.Administrator)
                        continue;

                    return true;
                }

                return false;
            }
            finally
            {
                eable.Free();
            }
        }

        public static bool InHouse(Mobile m)
        {
            return GetHouseID(m.Location, m.Map) > 0;
        }
        public static bool InHouse(Point3D location, Map map)
        {
            return GetHouseID(location, map) > 0;
        }
        public static int GetHouseID(Mobile m)
        {
            return GetHouseID(m.Location, m.Map);
        }
        public static int GetHouseID(Point3D location, Map map)
        {
            ArrayList regions = Region.FindAll(location, map);
            foreach (Region rx in regions)
                if (rx is HouseRegion)
                    return rx.UId;
            return 0;
        }
        public static bool SameHouse(params int[] values)
        {
            if (values == null || values.Length == 0)
                return false;

            int first = values[0];

            if (first == 0)
                return false;

            for (int i = 1; i < values.Length; i++)
            {
                if (values[i] != first)
                    return false;
            }

            return true;
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
        #endregion Location & Ownership

        #region Spawning & Standard Objects
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

        public static bool SpawnerSpawns(Spawner spawner, Type type, List<Type>? exclude = null)
        {   // note: can't be used for RaresSpawners
            foreach (string name in spawner.ObjectNamesRaw)
            {
                string[] chunks = name.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (string typeName in chunks)
                {
                    Type? typeFound = ScriptCompiler.FindTypeByName(typeName);
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
        #region Standard Objects
        public static XmlDocument? OpenStandardObjects()
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
            XmlElement? root = doc["category"];

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
            string? text = type.FullName;
            int itemID = 0;
            if (HasParameterlessConstructor(type))
                itemID = GetItemID(type);

            XmlElement? root = doc["category"];

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
                                        Type? typeFound = ScriptCompiler.FindTypeByName(nodeText);
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
            object? o = Activator.CreateInstance(type);
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
            object? o = Activator.CreateInstance(type);
            if (o is Item item)
            {
                itemid = item.ItemID;
                item.Delete();
            }

            return itemid;
        }
        #endregion Standard Objects
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
        #endregion Spawning & Standard Objects

        #region Messaging & Sound
        #region Anti-Spam Output
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
                    item.SendMessageNearby(text, accesslevel, hue);
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
        #endregion Anti-Spam Output
        #region Sound
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
        #endregion Sound
        #endregion Messaging & Sound

        #region Game Content Data
        #region Loot
        public static List<Item> GetNoMagicScrollReplacementLoot(int circle)
        {
            circle = Math.Max(1, Math.Min(8, circle));

            List<Item> loot = new List<Item>();

            switch (Utility.Random(3))
            {
                case 0:
                    {
                        int gemCount = Utility.RandomMinMax(1, Math.Max(1, circle / 2));

                        for (int i = 0; i < gemCount; i++)
                            loot.Add(Loot.RandomGem());

                        break;
                    }

                case 1:
                    {
                        int reagentCount = Utility.RandomMinMax(circle * 2, circle * 5);
                        loot.AddRange(Loot.RandomReagent(reagentCount));
                        break;
                    }

                case 2:
                    {
                        int blankScrollCount = Utility.RandomMinMax(1, circle);
                        loot.Add(new BlankScroll(blankScrollCount));
                        break;
                    }
            }

            return Utility.StackItems(loot);
        }
        #endregion Loot
        #region Event Objects
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
        #endregion Event Objects
        #region UO Music
        public static Dictionary<Mobile, MusicName> SoundCanvas = new();
        public static bool IsUOMusic(string enum_name)
        {
            foreach (string name in Enum.GetNames(typeof(MusicName)))
                if (enum_name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }
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

        #endregion UO Music
        #region Global Linker Database
        public static List<KeyValuePair<IEntity, IEntity>> GlobalLinkerDatabase = new();
        public static List<IEntity> GetLinksFrom(Item item, Type IsAssignableTo)
        {
            List<IEntity> elements = new();
            IEntity? linked_entity = null;
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
        #endregion Global Linker Database
        #region Addon Deeds
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
        #endregion Addon Deeds
        #region Dogtag
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
            private static Rectangle2D rect = WorldMap.LostLandsWrap;
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
        #endregion Dogtag
        #region Utility Data Persistence

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
                Utility.Monitor.WriteLine("Error reading UtilityData.bin, using default values:", ConsoleColor.Red);
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
                Utility.Monitor.WriteLine("Error writing UtilityData.bin: {0}", ConsoleColor.Red, ex.ToString());
            }
        }
        #endregion Utility Data Persistence
        #endregion Game Content Data
    }
}