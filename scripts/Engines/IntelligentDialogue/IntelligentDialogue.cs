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

/* Scripts\Engines\IntelligentDialogue\IntelligentDialogue.cs
 * CHANGELOG
 *  12/28/21, Adam
 *      Remove the sequential test of input text looking for a first best-fit.
 *      Replaced with a score-card model where all cases are checked and scored.
 *          this is on average, more expensive.
 *      Add 'memory' to system.
 *          now questions and responses are 'remembered' so that recieving the same question
 *          again results in a playback of the previous response. HUGE speedup (0.00 seconds)
 *  12/21/21, Adam
 *      Complete refactoring of the code.
 *      broke code up into the different request types: special, people, places and things.
 *      make quires run as a background thread.
 *      Add exception handling to this new thread.
 *      Added regression tests (attached at the endd of this file.)
 *      Use new Regions quick table.
 *      Because Angel Island renamed several regions, like Ocllo ==> Island Siege, 
 *      we load all Angel Island regions and classic UO regions to avoid player confusion.
 *  12/16/21, Adam
 *      First time checkin.
 *          The Intelligent Dialogue System (IDS) is used in a fuzzy match, quasi natural language processor
 *          the IDS allows players to query NPC for complex lookups of people, places and things.
 *          Some example Queries:
 *          Query: Where might I find the nearest bank?
 *          Query: where is the bank in magincia?
 *          Query: provisioner
 *          Query: moongate
 *        In all cases, the guard will give you the nearest location.
 *        But let's say you are standing at WBB and query: where is the bank in magincia?
 *        The guard will ignore the bank you are at, and instead direct you to the bank in magincia.
 *        Query: where is the bank in maginciia?
 *        You'll notice the typo in magincia? The IDS can still figure it out since it is using fuzzy matching and not straight string compares.
 *        Additionally, you'll notice we are searching for both places (bank in magincia), and professions (provisioner).
 *          Professions are a special case as we first find the nearest NPC of that profession, we then locate the building in which they reside.
 *          For instance, if you are at the bank in magincia and query for a tinker, the system will locate the nearest tinker and then direct you
 *          to "The Tic - Toc Shop" there in magincia.
 *        A final note on the IDS and specifically the patrolguards use; If you are asking about a 'bank' and the patrolguards is near a banker
 *        the guard will not respond and will defer to the banker. If you do wish to ask the patrolguard about a bank, perhaps in another city,
 *        Use the guards name. For example Query: Arkin, where is the bank in magincia?
 *      Guards are coded to answer the above quires, while the Animal Trainer understands quires such as:
 *          Query: Where might I find pigs?
 *          Query: Where might I find ogre lords?
 *          Query: Where are the nearest sheep?
 *        Most of the words are thrown away as the Animal Trainer will always return the nearest supply of the named creature.
 *        One could just as easily say "ogre lords"
 *        The system fully supports pluralization and singularization, so players may speak freely.
 */

using Pluralize.NET;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Server.Engines
{
    public static class IntelligentDialogue
    {
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(Load);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(Save);
        }
        // don't add moongate here as we have our own moongate handler
        private static readonly List<string> m_specialsList = new List<string>() { "bank", "stables", "stable" };
        private static string MapProfession(string text)
        {
            // special maps
            switch (text)
            {
                case "moongate": text = "the gatekeeper"; break;
                case "bank": text = "the banker"; break;
                case "stables": text = "the animal trainer"; break;
                case "stable": text = "the animal trainer"; break;
            }

            return text;
        }
        private static readonly Dictionary<string, string> m_typos = new Dictionary<string, string>
        {   { "britian", "britain" },
            { "stabel", "stable" },
            { "stabels", "stables" },
        };
        private static string FixTypos(string text)
        {
            string[] chunks = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int ix = 0; ix < chunks.Length; ix++)
            {
                if (m_typos.ContainsKey(chunks[ix]))
                    chunks[ix] = m_typos[chunks[ix]];
            }

            text = null;
            for (int ix = 0; ix < chunks.Length; ix++)
            {
                text += chunks[ix] + " ";
            }

            return text.Trim();
        }
        private static readonly Dictionary<string, string> m_blackList = new Dictionary<string, string>
        {   // "find" is an especially bad noise word as it's just 1 character away from "wind"
            //  the fuzzy matcher will promote it to "wind" and cause lots of extra work, usually resulting in a bad match
            { "find", "locate" },
        };
        private static string BlackList(string text)
        {
            string[] chunks = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int ix = 0; ix < chunks.Length; ix++)
            {
                if (m_blackList.ContainsKey(chunks[ix]))
                    chunks[ix] = m_blackList[chunks[ix]];
            }

            text = null;
            for (int ix = 0; ix < chunks.Length; ix++)
            {
                text += chunks[ix] + " ";
            }

            return text.Trim();
        }
        private static void PatchSpecial(ref string text)
        {
            foreach (string s in m_specialsList)
            {
                string instring = s;
                string outstring = MapProfession(s);
                string temp = text.Replace(instring, outstring);
                text = text.Replace(s, MapProfession(s));
            }
        }
        private static bool PointInTown(Point3D point, string town)
        {

            if (town == null || !CacheFactory.RegionQuickTable.ContainsKey(town))
                return false;

            foreach (Region region in CacheFactory.RegionQuickTable[town])
            {
                if (region == null)
                    continue;
                if (!region.Contains(point))
                    continue;
                return true;
            }

            return false;
        }
        private static string GetProfessionalsNameByInventoryItem(Point3D location, string item)
        {
            int range = 20;

            if (m_runningBackground)
            {
                foreach (Mobile found in CacheFactory.MobilesQuickTable.Keys)
                    if (found != null && found is BaseVendor vendor)
                        if (vendor.GetDistanceToSqrt(location) <= range)
                            if (vendor.HasInventoryItem(item))
                                return found.Name;
            }
            else
            {
                IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(location, range);
                foreach (Mobile found in eable)
                    if (found != null && found is BaseVendor vendor)
                        if (vendor.HasInventoryItem(item))
                        {
                            eable.Free();
                            return found.Name;
                        }

                eable.Free();
            }

            return null;
        }
        private static string GetProfessionalsName(Point3D location, string title)
        {
            int range = 20;

            if (m_runningBackground)
            {
                foreach (Mobile found in CacheFactory.MobilesQuickTable.Keys)
                    if (found != null && found.Title != null)
                        if (found.GetDistanceToSqrt(location) <= range)
                            if (found.Title.ToLower().Contains(title.ToLower()))
                                return found.Name;
            }
            else
            {
                IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(location, range);
                foreach (Mobile found in eable)
                    if (found != null && found.Title != null)
                        if (found.Title.ToLower().Contains(title.ToLower()))
                        {
                            eable.Free();
                            return found.Name;
                        }

                eable.Free();
            }

            return null;
        }
        private static string GetProfessionalsShop(Point3D location)
        {
            Point3D closest = Point3D.Zero;
            string ShopName = null;
            foreach (KeyValuePair<string, List<Point3D>> record in CacheFactory.PlacesQuickTable)
                foreach (Point3D point3D in record.Value)
                    if (Utility.GetDistanceToSqrt(location, point3D) <= 20)
                    {
                        if (closest == Point3D.Zero)
                        {
                            closest = point3D;
                            ShopName = record.Key;
                        }
                        else if (Utility.GetDistanceToSqrt(point3D, location) < Utility.GetDistanceToSqrt(closest, location))
                        {
                            closest = point3D;
                            ShopName = record.Key;
                        }
                    }

            return ShopName;
        }
        public enum LocateType
        {
            Unknown,                // wtf are you talking about?
            Remembered,             // we remeber the purified version of this question
            // people
            ClosestNPC,             // nearest professional NPC like thief guildmaster
            SpecificNPC,            // nearest professional NPC like thief guildmaster in the specified town
            // places
            ClosestPlace,           // banks, stables, 
            SpecificPlace,          // tavern, pubs, etc     
            // things
            ClosestMoongate,        // nearest moongate
            SpecificMoongate,       // moongate in the specified town
            ClosestItem,            // nearest item (potion, deed, club, etc.)
            SpecificItem,           // item in the specified town
            // special
            ClosestSpecial,         // nearest special (banker, gatekeeper, animal trainer, etc..)
            SpecificSpecial,        // special in the specified town
        }
        public static string TextInit(Mobile m, string text)
        {
            // common setup
            text = NormalizeQuery(text);                                        // remove punctionation
            text = Regex.Replace(text, m.Name, "", RegexOptions.IgnoreCase);    // remove the NPCs name 
            text = FixTypos(text);                                              // fix common typos
            text = BlackList(text);                                             // remove noise words that give matcher grief
            text = text.Trim();                                                 // final cleanup
            return text;
        }
        public class likelihoodResult
        {
            private int m_score = 0;
            public int Score => m_score;
            private string m_text;
            public string Text => m_text;
            public likelihoodResult(int score, string text)
            {
                m_score = score;
                m_text = text;
            }
        }
        private static Dictionary<IntelligentDialogue.LocateType, likelihoodResult> m_scorecard = new Dictionary<IntelligentDialogue.LocateType, likelihoodResult>()
        {
            { LocateType.ClosestNPC, null },
            { LocateType.SpecificNPC, null } ,         // nearest professional NPC like thief guildmaster in the specified town
            // places
            {LocateType.ClosestPlace, null },          // banks, stables, 
            {LocateType.SpecificPlace, null },         // tavern, pubs, etc     
            // things
            {LocateType.ClosestMoongate, null },       // nearest moongate
            {LocateType.SpecificMoongate, null },      // moongate in the specified town
            {LocateType.ClosestItem, null },           // nearest item (potion, deed, club, etc.)
            {LocateType.SpecificItem, null },          // item in the specified town
            // special
            {LocateType.ClosestSpecial, null },        // nearest special (banker, gatekeeper, animal trainer, etc..)
            {LocateType.SpecificSpecial, null }
        };
        public static LocateType ComputeLikelihood(ref string outString)
        {
            // clear our score card
            foreach (KeyValuePair<LocateType, likelihoodResult> kvp in m_scorecard)
                m_scorecard[kvp.Key] = new likelihoodResult(0, string.Empty);

            string text = string.Empty;
            RollingHash rh = new RollingHash();

            for (; ; )
            { // LocateType.ClosestNPC
                text = outString;
                if (MentionsTown(text, .25) != null)
                    break;  // fail
                FixNPCArticle(ref text, CacheFactory.VendorTitleList);
                string result = FindString(CacheFactory.VendorTitleList, text, .25);
                if (result != null)
                    m_scorecard[LocateType.ClosestNPC] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                break;
            }

            for (; ; )
            { // LocateType.SpecificNPC
                text = outString;
                string town;
                if ((town = MentionsTown(text, .25)) == null)
                    break;  // fail
                FixNPCArticle(ref text, CacheFactory.VendorTitleList);
                string result = FindString(CacheFactory.VendorTitleList, text, .25);
                if (result != null)
                {
                    result += " " + town;
                    m_scorecard[LocateType.SpecificNPC] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                }
                break;
            }

            for (; ; )
            { // LocateType.ClosestMoongate
                text = outString;
                if (MentionsTown(text, .25) != null)
                    break;  // fail
                string result = FindString("moongate", text, .25);
                if (result != null)
                    m_scorecard[LocateType.ClosestMoongate] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                break;
            }

            for (; ; )
            { // LocateType.SpecificMoongate
                text = outString;
                string town;
                if ((town = MentionsTown(text, .25)) == null)
                    break;  // fail
                string result = FindString("moongate", text, .25);
                if (result != null)
                {
                    result += " " + town;
                    m_scorecard[LocateType.SpecificMoongate] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                }
                break;
            }

            for (; ; )
            { // LocateType.ClosestPlace
                text = outString;
                if (MentionsTown(text, .25) != null)
                    break;  // fail
                string result = FindString(CacheFactory.PlacesList, text, .25);
                if (result != null)
                    m_scorecard[LocateType.ClosestPlace] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                break;
            }

            for (; ; )
            { // LocateType.SpecificPlace
                text = outString;
                string town;
                if ((town = MentionsTown(text, .25)) == null)
                    break;  // fail
                string result = FindString(CacheFactory.PlacesList, text, .25);
                if (result != null)
                {
                    result += " " + town;
                    m_scorecard[LocateType.SpecificPlace] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                }
                break;
            }

            for (; ; )
            { // LocateType.ClosestItem
                text = outString;
                if (MentionsTown(text, .25) != null)
                    break;  // fail
                FixObjectArticle(ref text, CacheFactory.InventoryList);
                string result = FindString(CacheFactory.InventoryList, text, .25);
                if (result != null)
                    m_scorecard[LocateType.ClosestItem] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                break;
            }

            for (; ; )
            { // LocateType.SpecificItem
                text = outString;
                string town;
                if ((town = MentionsTown(text, .25)) == null)
                    break;  // fail
                FixObjectArticle(ref text, CacheFactory.InventoryList);
                string result = FindString(CacheFactory.InventoryList, text, .25);
                if (result != null)
                {
                    result += " " + town;
                    m_scorecard[LocateType.SpecificItem] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                }
                break;
            }

            for (; ; )
            { // LocateType.ClosestSpecial
                text = outString;
                if (MentionsTown(text, .25) != null)
                    break;  // fail
                // patch the special word like "stable" ==> "animal trainer" and retry as an NPC
                PatchSpecial(ref text);
                string result = FindString(CacheFactory.VendorTitleList, text, .25);
                if (result != null)
                    m_scorecard[LocateType.ClosestSpecial] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                break;
            }

            for (; ; )
            { // LocateType.SpecificSpecial
                text = outString;
                string town;
                if ((town = MentionsTown(text, .25)) == null)
                    break;  // fail
                // patch the special word like "stable" ==> "animal trainer" and retry as an NPC
                PatchSpecial(ref text);
                string result = FindString(CacheFactory.VendorTitleList, text, .25);
                if (result != null)
                {
                    result += " " + town;
                    m_scorecard[LocateType.SpecificSpecial] = new likelihoodResult(rh.FindCommonSubstring(text, result, true).Length, result);
                }
                break;
            }

            LocateType lresult = LocateType.Unknown;
            int bestScore = 0;
            string bestText = null;
            // add up the score card
            foreach (KeyValuePair<LocateType, likelihoodResult> kvp in m_scorecard)
                if (m_scorecard[kvp.Key].Score > bestScore)
                {
                    bestScore = m_scorecard[kvp.Key].Score;
                    lresult = kvp.Key;
                    bestText = m_scorecard[kvp.Key].Text;
                }

            // return our purified string and handler
            outString = bestText;

            // if we remember the purified version of this question, we can skip deeper processing
            if (outString != null)
                if (m_memory.ContainsKey(outString))
                {
                    if (m_memory[outString].TextOutput.Count > 0)
                        return LocateType.Remembered;
                    else
                    {
                        // the user entered a 'purfied' string, likes 'docks', and it was remembered in the main loop
                        //  but has not as yet had text associated with it.
                        Utility.Monitor.WriteLine("Cache2: Loading replies for: '{0}'.", ConsoleColor.Green, text);
                    }
                }
                else
                    m_memory[outString] = new MemoryObj();

            return lresult;
        }
        public static LocateType GetLocateType(ref string outString)
        {
            switch (ComputeLikelihood(ref outString))
            {
                case LocateType.Unknown: return LocateType.Unknown;
                case LocateType.Remembered: return LocateType.Remembered;
                case LocateType.ClosestSpecial: return LocateType.ClosestSpecial;
                case LocateType.SpecificSpecial: return LocateType.SpecificSpecial;

                case LocateType.ClosestNPC: return LocateType.ClosestNPC;
                case LocateType.SpecificNPC: return LocateType.SpecificNPC;

                case LocateType.ClosestMoongate: return LocateType.ClosestMoongate;
                case LocateType.SpecificMoongate: return LocateType.SpecificMoongate;

                case LocateType.ClosestPlace: return LocateType.ClosestPlace;
                case LocateType.SpecificPlace: return LocateType.SpecificPlace;

                case LocateType.ClosestItem: return LocateType.ClosestItem;
                case LocateType.SpecificItem: return LocateType.SpecificItem;
            }

            return LocateType.Unknown;
        }
        private static void DoClosestSpecial(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            PatchSpecial(ref text);
            DoClosestNPC(text, ConvoMob, ConvoPlayer);
            return;
        }
        private static void DoSpecificSpecial(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            PatchSpecial(ref text);
            DoSpecificNPC(text, ConvoMob, ConvoPlayer);
            return;
        }
        private static void DoClosestNPC(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            Point3D closest = Point3D.Zero;
            FixNPCArticle(ref text, CacheFactory.VendorTitleList);
            string NPCTitle = FindString(CacheFactory.VendorTitleList, text, .25);
            foreach (Point3D point in CacheFactory.VendorsQuickTable[NPCTitle])
            {
                if (closest == Point3D.Zero)
                {
                    closest = point;
                    continue;
                }
                // save the closest moongate
                if (Utility.GetDistanceToSqrt(ConvoMob.Location, point) < Utility.GetDistanceToSqrt(ConvoMob.Location, closest))
                    closest = point;
            }

            ConvoExit(closest, ConvoMob, ConvoPlayer, GetProfessionalsShop(closest), GetProfessionalsName(closest, NPCTitle) + " " + NPCTitle);
        }
        private static void DoSpecificNPC(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            Point3D closest = Point3D.Zero;
            FixNPCArticle(ref text, CacheFactory.VendorTitleList);
            string NPCTitle = FindString(CacheFactory.VendorTitleList, text, .25);
            string town = FindString(CacheFactory.TownsList, text, .25);
            foreach (Point3D point in CacheFactory.VendorsQuickTable[NPCTitle])
            {
                if (!PointInTown(point, town))
                    continue;

                if (closest == Point3D.Zero)
                {
                    closest = point;
                    continue;
                }
                // save the closest moongate
                if (Utility.GetDistanceToSqrt(ConvoMob.Location, point) < Utility.GetDistanceToSqrt(ConvoMob.Location, closest))
                    closest = point;
            }

            ConvoExit(closest, ConvoMob, ConvoPlayer, GetProfessionalsShop(closest), GetProfessionalsName(closest, NPCTitle));
        }
        private static void DoClosestMoongate(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            string NPCTitle = MapProfession("moongate");
            Point3D closest = Point3D.Zero;
            foreach (KeyValuePair<string, Point3D> kvp in CacheFactory.MoongatesQuickTable)
            {
                if (closest == Point3D.Zero)
                {
                    closest = kvp.Value;
                    continue;
                }
                // save the closest moongate
                if (Utility.GetDistanceToSqrt(ConvoMob.Location, kvp.Value) < Utility.GetDistanceToSqrt(ConvoMob.Location, kvp.Value))
                    closest = kvp.Value;
            }

            ConvoExit(closest, ConvoMob, ConvoPlayer, "a moongate", GetProfessionalsName(closest, NPCTitle));
        }
        private static void DoSpecificMoongate(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            string NPCTitle = MapProfession("moongate");
            string town = FindString(CacheFactory.TownsList, text, .25);
            Point3D closest = Point3D.Zero;
            foreach (KeyValuePair<string, Point3D> kvp in CacheFactory.MoongatesQuickTable)
            {
                // can't use PointInTown() here since moongates aren't in town
                //  fortunatly, our moongate quicktable has the point ready for us.
                if (kvp.Key.ToLower() == town.ToLower())
                {
                    closest = kvp.Value;
                    break;
                }
            }

            ConvoExit(closest, ConvoMob, ConvoPlayer, "a moongate", GetProfessionalsName(closest, NPCTitle));
        }
        private static void DoClosestPlace(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            Point3D closest = Point3D.Zero;
            string place = FindString(CacheFactory.PlacesList, text, .25);
            foreach (Point3D point in CacheFactory.PlacesQuickTable[place])
            {
                if (closest == Point3D.Zero)
                {
                    closest = point;
                    continue;
                }
                // save the closest moongate
                if (Utility.GetDistanceToSqrt(ConvoMob.Location, point) < Utility.GetDistanceToSqrt(ConvoMob.Location, closest))
                    closest = point;
            }

            ConvoExit(closest, ConvoMob, ConvoPlayer, place/*GetProfessionalsShop(closest)*/, null/*GetProfessionalsName(closest, Place)*/);
        }
        private static void DoSpecificPlace(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            Point3D closest = Point3D.Zero;
            string place = FindString(CacheFactory.PlacesList, text, .25);
            string town = FindString(CacheFactory.TownsList, text, .25);
            if (CacheFactory.DocksQuickTable.ContainsKey(place))
            {   // We handle this special case because we need to differentiate between docks and other 'in town' places since
                //  docks are often outside the town limits
                closest = CacheFactory.DocksQuickTable[place];
            }
            else
                foreach (Point3D point in CacheFactory.PlacesQuickTable[place])
                {
                    if (!PointInTown(point, town))
                        continue;

                    if (closest == Point3D.Zero)
                    {
                        closest = point;
                        continue;
                    }
                    // save the closest moongate
                    if (Utility.GetDistanceToSqrt(ConvoMob.Location, point) < Utility.GetDistanceToSqrt(ConvoMob.Location, closest))
                        closest = point;
                }

            ConvoExit(closest, ConvoMob, ConvoPlayer, place/*GetProfessionalsShop(closest)*/, null/*GetProfessionalsName(closest, Place)*/);
        }
        private static void DoClosestItem(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            Point3D closest = Point3D.Zero;
            FixObjectArticle(ref text, CacheFactory.InventoryList);
            string thing = FindString(CacheFactory.InventoryList, text, .25);
            foreach (Point3D point in CacheFactory.InventoryQuickTable[thing])
            {
                if (closest == Point3D.Zero)
                {
                    closest = point;
                    continue;
                }
                // save the closest moongate
                if (Utility.GetDistanceToSqrt(ConvoMob.Location, point) < Utility.GetDistanceToSqrt(ConvoMob.Location, closest))
                    closest = point;
            }

            ConvoExit(closest, ConvoMob, ConvoPlayer, GetProfessionalsShop(closest), GetProfessionalsNameByInventoryItem(closest, thing));
        }
        private static void DoSpecificItem(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            Point3D closest = Point3D.Zero;
            FixObjectArticle(ref text, CacheFactory.InventoryList);
            string thing = FindString(CacheFactory.InventoryList, text, .25);
            string town = FindString(CacheFactory.TownsList, text, .25);
            foreach (Point3D point in CacheFactory.InventoryQuickTable[thing])
            {
                if (!PointInTown(point, town))
                    continue;

                if (closest == Point3D.Zero)
                {
                    closest = point;
                    continue;
                }
                // save the closest moongate
                if (Utility.GetDistanceToSqrt(ConvoMob.Location, point) < Utility.GetDistanceToSqrt(ConvoMob.Location, closest))
                    closest = point;
            }

            ConvoExit(closest, ConvoMob, ConvoPlayer, GetProfessionalsShop(closest), GetProfessionalsNameByInventoryItem(closest, thing));
        }
        public static void ConvoExit(Point3D closest, Mobile ConvoMob, Mobile ConvoPlayer, string thing, string NPCName)
        {
            if (closest == Point3D.Zero)
                SayTo(ConvoMob, ConvoPlayer, "I'm sorry, I do not know anything about that.");
            else
            {
                // okay, we now have the closest things, tell the player about it
                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;
                bool dungeon = !Server.Items.Sextant.Format(closest, ConvoMob.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);
                string location;
                if (!dungeon)
                    location = Server.Items.Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                else
                    location = Region.Find(closest, ConvoMob.Map).Name;

                if (NPCName != null)
                    location = string.Format("Thou might find {0} at {1} near " + location, NPCName, (thing == null) ? string.Empty : thing);
                else
                    location = string.Format("Thou might find {0} near " + location, (thing == null) ? string.Empty : thing);
                if (dungeon)
                    location = location.Replace("near", "in dungeon");
                if (thing == null)
                    location = location.Replace("at ", "");
                SayTo(ConvoMob, ConvoPlayer, location);
                GiveDirectionalHint(ConvoMob, ConvoPlayer, new Point2D(closest.X, closest.Y));
            }
        }
        private static bool m_runningBackground = false;
        public class MemoryObj
        {
            private List<string> m_textOutput = new List<string>();
            public List<string> TextOutput => m_textOutput;
            private DateTime m_expiry = DateTime.UtcNow + TimeSpan.FromHours(2);
            public DateTime Expiry => m_expiry;
            public MemoryObj()
            { }
            public MemoryObj(List<string> chatter, DateTime expiry)
            { m_textOutput = chatter; m_expiry = expiry; }
        }
        private static Dictionary<string, MemoryObj> m_memory = new Dictionary<string, MemoryObj>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, MemoryObj> Memory => m_memory;
        private static DateTime m_lastMemoryCleanup = DateTime.UtcNow;
        public static void DoGenericLocate(Mobile m, SpeechEventArgs e, bool background)
        {   // are we running as a background task?
            // Note: Task.Factory.StartNew() is not guarnteed to create a new thread, but it can.
            // https://tinyurl.com/3auv3yyz
            m_runningBackground = background;

            Utility.TimeCheck tc = new Utility.TimeCheck();
            Utility.Monitor.WriteLine("DoGenericLocate... ", ConsoleColor.Yellow);
            tc.Start();

            try
            {
                // every 2 hours, remove expired memory records.
                //  we do this because mobiles expire, and we will need to refresh our output
                //  (like name of the mobile and such)
                if (DateTime.UtcNow > m_lastMemoryCleanup)
                {
                    List<string> list = new List<string>();
                    m_lastMemoryCleanup = DateTime.UtcNow + TimeSpan.FromHours(2);
                    foreach (KeyValuePair<string, MemoryObj> kvp in m_memory)
                    {
                        if (DateTime.UtcNow > kvp.Value.Expiry)
                            list.Add(kvp.Key);
                    }
                    foreach (string sx in list)
                        m_memory.Remove(sx);
                }

                // use our memory if we have one
                string rememberPrompt = e.Speech;
                if (m_memory.ContainsKey(rememberPrompt))
                {   // we've seen this query before, just replay our last response
                    foreach (string sx in m_memory[rememberPrompt].TextOutput)
                    {
                        m.SayTo(e.Mobile, sx);
                        if (IDSRegressionTest.RegressionRecorderEnabled)
                        {
                            if (IDSRegressionTest.Logger != null)
                                IDSRegressionTest.Logger.Log("=> " + sx);
                        }
                    }
                    Utility.Monitor.WriteLine("Cache1: We remembered '{0}', eliminating undo processing.", ConsoleColor.Cyan, rememberPrompt);
                    goto exit;
                }
                else
                {   // remember this question
                    m_memory[rememberPrompt] = new MemoryObj();
                }

                // okay, pick the handler and go!
                // figure out what kind of locate we are going to perform
                string text = TextInit(m, e.Speech);
                switch (GetLocateType(ref text))
                {
                    // specials
                    case LocateType.ClosestSpecial:
                        {
                            DoClosestSpecial(text, m, e.Mobile);
                            break;
                        }
                    case LocateType.SpecificSpecial:
                        {
                            DoSpecificSpecial(text, m, e.Mobile);
                            break;
                        }
                    // people
                    case LocateType.ClosestNPC:
                        {
                            DoClosestNPC(text, m, e.Mobile);
                            break;
                        }
                    case LocateType.SpecificNPC:
                        {
                            DoSpecificNPC(text, m, e.Mobile);
                            break;
                        }
                    // places
                    case LocateType.ClosestPlace:
                        {
                            DoClosestPlace(text, m, e.Mobile);
                            break;
                        }

                    case LocateType.SpecificPlace:
                        {
                            DoSpecificPlace(text, m, e.Mobile);
                            break;
                        }
                    // and things
                    case LocateType.ClosestMoongate:
                        {
                            DoClosestMoongate(text, m, e.Mobile);
                            break;
                        }
                    case LocateType.SpecificMoongate:
                        {
                            DoSpecificMoongate(text, m, e.Mobile);
                            break;
                        }
                    case LocateType.ClosestItem:
                        {
                            DoClosestItem(text, m, e.Mobile);
                            break;
                        }
                    case LocateType.SpecificItem:
                        {
                            DoSpecificItem(text, m, e.Mobile);
                            break;
                        }
                    case LocateType.Remembered:
                        {
                            foreach (string sx in m_memory[text].TextOutput)
                            {
                                m.SayTo(e.Mobile, sx);
                                if (IDSRegressionTest.RegressionRecorderEnabled)
                                {
                                    if (IDSRegressionTest.Logger != null)
                                        IDSRegressionTest.Logger.Log("=> " + sx);
                                }
                            }
                            Utility.Monitor.WriteLine("Cache2: We remembered '{0}', eliminating undo processing.", ConsoleColor.Green, text);
                            goto exit;
                        }
                    case LocateType.Unknown:
                    default:
                        ConvoExit(Point3D.Zero, m, e.Mobile, null, null);
                        break;
                }

                while (m_outputQueue.Count > 0)
                {   // remember how the guard answered for both the origianl text
                    //  and the purified version. This means two differently phrased questions may 
                    //  resolve to the same purified version, and we'll have an answer for all.
                    string chatter = m_outputQueue.Dequeue();
                    m_memory[rememberPrompt].TextOutput.Add(chatter);
                    if (text != null)
                        m_memory[text].TextOutput.Add(chatter);
                }
            }
            catch (Exception ex)
            {
                Utility.Monitor.WriteLine(ex.Message, ConsoleColor.Red);
                Diagnostics.LogHelper.LogException(ex);
            }

        exit:

            tc.End();
            Utility.Monitor.WriteLine("DoGenericLocate finished in {0} seconds (background thread).", ConsoleColor.Yellow, tc.TimeTaken);
        }
        public static void DoMobileLocate(Mobile m, SpeechEventArgs e, string format)
        {
            try
            {
                //Utility.ConsoleOut("DoMobileLocate... ", ConsoleColor.Yellow);
                //Utility.TimeCheck tc = new Utility.TimeCheck();
                //tc.Start();
                // they are asking about X, show them where the X is/are.
                Spawner closest = null;
                string thing = null;
                string text = NormalizeQuery(e.Speech);
                string[] split = text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string[] splitCopy = new string[split.Length];  // we will be reversing this array for our stack ops
                List<Spawner> shortListOfSpawners = new List<Spawner>();
                IPluralize ps = new Pluralizer();

                // When looking for mobiles, we need to handle both one-word searches ("ogre") and two-word searches ("ogre lord")
                //  and always prefer the two-word version.
                // first check the long match, then check the short match
                for (int ix = 0; ix < 2; ix++)
                {   // prepare our word list for stack operations
                    Array.Copy(split, splitCopy, split.Length);
                    Array.Reverse(splitCopy);
                    Stack<string> wordStack = new Stack<string>(splitCopy);

                    while (wordStack.Count > 0)
                    {
                        if (ix == 0 && wordStack.Count > 1)
                            // first check long match
                            thing = wordStack.Pop().ToLower() + wordStack.Peek().ToLower();
                        else
                            // next check short match
                            thing = wordStack.Pop().ToLower();
                        if (ps.IsPlural(thing))
                            thing = ps.Singularize(thing);
                        if (CacheFactory.SpawnedMobilesQuickTable.ContainsKey(thing))
                        {
                            List<Spawner> sortedList = new List<Spawner>(CacheFactory.SpawnedMobilesQuickTable[thing]);
                            // sort the list by distance
                            sortedList.Sort((e1, e2) =>
                            {
                                return e1.GetDistanceToSqrt(m.Location).CompareTo(e2.GetDistanceToSqrt(m.Location));
                            });
                            closest = sortedList[0];
                            goto done;
                        }
                    }
                }

            done:
                if (closest == null)
                    m.SayTo(e.Mobile, "I'm sorry, I do not know anything about that.");
                else
                {

                    // okay, we now have the closest things, tell the player about it
                    int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                    bool xEast = false, ySouth = false;
                    bool dungeon = !Server.Items.Sextant.Format(closest.Location, closest.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);
                    Type type = ScriptCompiler.FindTypeByName(thing);
                    if (type == null)
                    {
                        m.SayTo(e.Mobile, "I'm sorry, I do not know anything about that.");
                        return;
                    }
                    string location;
                    if (!dungeon)
                        location = Server.Items.Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                    else
                        location = Region.Find(closest.Location, m.Map).Name;
                    string output_name = Utility.SplitOnCase(type.Name).ToLower();
                    output_name = ps.Pluralize(output_name);
                    location = string.Format(format + location, output_name);
                    if (dungeon)
                        location = location.Replace("near", "in dungeon");
                    m.SayTo(e.Mobile, location);
                    GiveDirectionalHint(m, e.Mobile, new Point2D(closest.Location.X, closest.Location.Y));
                }
                //tc.End();
                //Utility.ConsoleOut("DoMobileLocate finished in {0} seconds (background thread).", ConsoleColor.Yellow, tc.TimeTaken);
            }
            catch (Exception ex)
            {
                Utility.Monitor.WriteLine(ex.Message, ConsoleColor.Red);
                Diagnostics.LogHelper.LogException(ex);
            }
        }
        public static void DoItemLocate(Mobile m, SpeechEventArgs e, Type type, string text)
        {
            try
            {
                //Utility.ConsoleOut("DoItemLocate... ", ConsoleColor.Yellow);
                //Utility.TimeCheck tc = new Utility.TimeCheck();
                //tc.Start();
                // they are asking about X, show them where the X is/are.
                Spawner closest = null;
                foreach (Spawner spawner in SpawnerCache.Spawners)
                {
                    if (spawner == null || spawner.Running == false || spawner.Map != Map.Felucca)
                        continue;

                    double check = 0;
                    if (spawner.Contains(type))
                    {   // okay, now find the Xs
                        check = m.GetDistanceToSqrt(spawner.Location);
                        if (closest == null)
                        {   // save the first spawner found
                            closest = spawner;
                            continue;
                        }
                        else if (check < m.GetDistanceToSqrt(closest.Location))
                        {   // update our closest spawner with this one
                            closest = spawner;
                            continue;
                        }
                    }
                }
                // okay, we now have the closest cotton plants, tell the player about it
                if (closest != null)
                {
                    int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                    bool xEast = false, ySouth = false;
                    if (Server.Items.Sextant.Format(closest.Location, closest.Map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth))
                    {
                        string location = Server.Items.Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                        location = string.Format(text + location, Utility.SplitOnCase(type.Name).ToLower());
                        m.SayTo(e.Mobile, location);
                        GiveDirectionalHint(m, e.Mobile, new Point2D(closest.Location.X, closest.Location.Y));
                    }
                }
                //tc.End();
                //Utility.ConsoleOut("DoItemLocate finished in {0} seconds (background thread).",ConsoleColor.Yellow, tc.TimeTaken);
            }
            catch (Exception ex)
            {
                Utility.Monitor.WriteLine(ex.Message, ConsoleColor.Red);
                Diagnostics.LogHelper.LogException(ex);
            }
        }
        /*
         * HELPER FUNCTIONS BELOW
         */
        #region Fuzzy Match Engine
        private static string MentionsTown(string text, double fuzz)
        {
            foreach (string town in CacheFactory.TownsList)
                if (FindString(town, text, .25) != null)
                    return town;
            return null;
        }
        private static string FindString(Dictionary<string, List<Point3D>>.KeyCollection words, string text, double fuzz)
        {
            List<MatchElement> return_elements = new List<MatchElement>();

            foreach (string word in words)
            {
                List<MatchElement> elements = null;

                if ((elements = _FindString(word, text, fuzz)) != null)
                    foreach (MatchElement element in elements)
                        return_elements.Add(element);
            }

            // process elements

            // finally
            string cbm = ComputeBestMatch(return_elements);
            return cbm;
        }
        private static string FindString(Dictionary<string, Point3D>.KeyCollection words, string text, double fuzz)
        {
            List<MatchElement> return_elements = new List<MatchElement>();

            foreach (string word in words)
            {
                List<MatchElement> elements = null;

                if ((elements = _FindString(word, text, fuzz)) != null)
                    foreach (MatchElement element in elements)
                        return_elements.Add(element);
            }

            // process elements

            // finally
            string cbm = ComputeBestMatch(return_elements);
            return cbm;
        }
        private static string FindString(string word, string text, double fuzz)
        {
            List<MatchElement> return_elements = _FindString(word, text, fuzz);

            // return the best element

            // finally
            string cbm = ComputeBestMatch(return_elements);
            return cbm;
        }
        private static List<MatchElement> _FindString(string word, string text, double fuzz)
        {
            // this is the funnel point
            // we will lower 'word' here
            word = word.ToLower();

            List<MatchElement> elements = FindStringEngine(word, text, fuzz);

            // return the best element

            return elements;
        }
        private static List<MatchElement> FindStringEngine(string word, string text, double fuzz)
        {
            List<MatchElement> elements = new List<MatchElement>();

            // do substring processing
            //  we we repeatedly remove the first word from the input string to eliminate
            //  noisy words (looking for the best match)
            List<string> tokens = GetCandidateList(word, text, fuzz);

            string substring = null;
            foreach (string token in tokens)
            {
                // build a substring
                substring += token + " ";

            }

            if (substring != null)
            {
                substring = substring.Trim();

                // test it, add it
                elements.Add(MatchString(word, substring, fuzz));
            }

            return elements;
        }
        public static List<string> GetCandidateList(string word, string text, double fuzz)
        {
            MatchElement element = null;
            List<string> tokens_text = new List<string>(text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            List<string> tokens_word = new List<string>(word.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            List<KeyValuePair<string, int>> tokens_out = new List<KeyValuePair<string, int>>();

            // assume the player got the correct number of words for their query - possibly with misspellings
            int pos = 0;
            foreach (string token_t in tokens_text)
            {
                foreach (string token_w in tokens_word)
                {
                    element = MatchString(token_w, token_t, fuzz);
                    string result = ComputeBestMatch(new List<MatchElement> { element });
                    if (result != null)
                        if (!tokens_out.Contains(new KeyValuePair<string, int>(result, pos)))
                            tokens_out.Add(new KeyValuePair<string, int>(result, pos));
                }
                pos++;
            }

            // okay, here we are handling the case where the player got the number of words wrong for their query, i.e.,
            // "guild master" instead of "guildmaster"
            // we will glue each pair of words together in order and retest
            pos = 0;
            if (tokens_text.Count - 2 > 0)
                for (int ix = 0; ix < tokens_text.Count; ix++)
                {
                    string token_t = tokens_text[ix] + tokens_text[ix + 1];

                    foreach (string token_w in tokens_word)
                    {
                        element = MatchString(token_w, token_t, fuzz);
                        string result = ComputeBestMatch(new List<MatchElement> { element });
                        if (result != null)
                            if (!tokens_out.Contains(new KeyValuePair<string, int>(result, pos)))
                                tokens_out.Add(new KeyValuePair<string, int>(result, pos));
                    }

                    if (ix == tokens_text.Count - 2)
                        break;
                    pos++;
                }

            List<string> stokens_out = new List<string>();
            foreach (KeyValuePair<string, int> pair in tokens_out)
                if (!stokens_out.Contains(pair.Key))
                    stokens_out.Add(pair.Key);

            return stokens_out;
        }
        public class MatchElement
        {
            private string m_word = null;
            private string m_text = null;
            private double m_fuzz = int.MaxValue;
            private int m_rank = int.MaxValue;
            private int m_subStringMatch = 0;
            private int m_score = 0;
            public MatchElement(string word, string text, double fuzz, int rank, int subStringMatch)
            {
                m_word = word;
                m_text = text;
                m_fuzz = fuzz;
                m_rank = rank;
                m_subStringMatch = subStringMatch;
                m_score = Rank;
            }
            public string Word { get { return m_word; } }
            public string Text { get { return m_text; } }
            public double Fuzz { get { return m_fuzz; } }
            public int Rank { get { return m_rank; } }
            public int SubStringMatch { get { return m_subStringMatch; } }
            public int Score { get { return m_score; } }
        }
        private static MatchElement MatchString(string word, string text, double fuzz)
        {
            RollingHash ps = new RollingHash();
            return new MatchElement(word, text, fuzz, StringDistance.LevenshteinDistance(word, text), ps.FindCommonSubstring(word, text, true).Length);
        }
        private static string ComputeBestMatch(List<MatchElement> elements)
        {
            List<MatchElement> tiebreaker_elements = new List<MatchElement>();
            if (elements != null && elements.Count > 0)
            {
                elements.Sort((e1, e2) =>
                {
                    return e1.Score.CompareTo(e2.Score);
                });

                foreach (MatchElement e in elements)
                {
                    if (e.Score == elements[0].Score)
                        tiebreaker_elements.Add(e);
                    else
                        break;
                }

                tiebreaker_elements.Sort((e1, e2) =>
                {
                    return e2.SubStringMatch.CompareTo(e1.SubStringMatch);
                });

                if (tiebreaker_elements[0].Score <= tiebreaker_elements[0].Fuzz * tiebreaker_elements[0].Text.Length)
                    return tiebreaker_elements[0].Word;
            }

            return null;
        }
        #endregion Fuzzy Match Engine
        public static bool WhenToSpeakRules(Mobile m, SpeechEventArgs e)
        {
            string spoken = e.Speech;

            bool theyAreClose = m.GetDistanceToSqrt(e.Mobile) <= 3;

            bool spokeMyName = spoken.ToLower().Contains(m.Name.ToLower());

            bool theySaidBank = spoken.ToLower().Contains("bank") || spoken.ToLower().Contains("withdraw") || spoken.ToLower().Contains("balance") || spoken.ToLower().Contains("check");

            bool otherPlayersAround = NearByExcluding(m, e.Mobile, typeof(PlayerMobile));

            bool bankerNearBy = NearBy(m, typeof(Banker));

            // if we are a guard, we are likely near a bank. If the player said something about 'bank'
            //  and they did not mention our name and there is a banker near by, we should STFU
            if (m is BaseGuard)
            {
                if (spokeMyName == true)
                    return true;    // always okay to respond if our name is spoken

                if (theySaidBank == false && otherPlayersAround == false && theyAreClose == true)
                    return true;    // must be talking to me

                if (theySaidBank == true && otherPlayersAround == false && theyAreClose == true && bankerNearBy == false)
                    return true;    // maybe be talking to me?

                return false;
            }
            else if (m is AnimalTrainer)
            {
                if (spokeMyName == true)
                    return true;    // always okay to respond if our name is spoken

                if (theySaidBank == false && otherPlayersAround == false && theyAreClose == true)
                    return true;    // must be talking to me

                if (theySaidBank == false && otherPlayersAround == true && theyAreClose == true)
                    return true;    // maybe be talking to me?

                if (theySaidBank == true)
                    return false;

                return false;
            }
            return true;
        }
        private static void FixNPCArticle(ref string text, Dictionary<string, List<Point3D>>.KeyCollection list)
        {
            PatchObjectArticle(ref text, list, "the");
        }
        private static void FixObjectArticle(ref string text, Dictionary<string, List<Point3D>>.KeyCollection list)
        {
            string the = text;
            string a = text;
            string an = text;
            PatchObjectArticle(ref the, list, "the");
            PatchObjectArticle(ref a, list, "a");
            PatchObjectArticle(ref an, list, "an");
            List<KeyValuePair<string, int>> match_list = new List<KeyValuePair<string, int>>();

            RollingHash rh = new RollingHash();
            foreach (string s in list)
            {
                match_list.Add(new KeyValuePair<string, int>(the, rh.FindCommonSubstring(the, s).Length));
                match_list.Add(new KeyValuePair<string, int>(a, rh.FindCommonSubstring(a, s).Length));
                match_list.Add(new KeyValuePair<string, int>(an, rh.FindCommonSubstring(an, s).Length));
            }

            if (match_list.Count > 0)
            {
                match_list.Sort((e1, e2) =>
                {
                    return e2.Value.CompareTo(e1.Value);
                });

                text = match_list[0].Key;
            }
            else
                text = string.Empty;
        }
        private static void PatchObjectArticle(ref string text, Dictionary<string, List<Point3D>>.KeyCollection list, string hint)
        {
            hint += " ";
            if (text.Contains(hint)) return; // no fix needed

            // Note: all NPC titles have "the " in the title. if the player omits the "the ", they lose 4 points in the fuzzy match.
            // help out the playter a bit.
            // when searching for an NPC like a 'healer' they aren't likely to add "the"
            //  Our fuzzy matcher will cut some slack, but missing "the " in the string "healer" is > than the .25 error rate
            // Note I don't want to soften the fuzzy matcher any more than it already is...
            //  The solution (and it's cheap) is to include "the " if it helps us gain the match.
            string[] tokens = text.Split(new char[] { ' ' });
            RollingHash rh = new RollingHash();
            Dictionary<string, int> dic = new Dictionary<string, int>();
            try
            {
                foreach (string s in list)
                {
                    // don't add 'the' if the target string does not contain 'the'
                    if (!(s.ToLower().Contains(hint)))
                        continue;

                    for (int ix = 0; ix < tokens.Length; ix++)
                    {   // for example, if token=="healer", add "the"
                        string[] copy = new string[tokens.Length];
                        Array.Copy(tokens, copy, tokens.Length);
                        // assemble string and insert "the "
                        string temp = string.Empty;
                        for (int jx = 0; jx < tokens.Length; jx++)
                        {
                            if (jx == ix) copy[jx] = hint + copy[jx];
                            temp += copy[jx] + " ";
                        }
                        temp = temp.Trim();
                        // save the string and it's match length
                        if (!dic.ContainsKey(temp))
                            dic.Add(temp, rh.FindCommonSubstring(s, temp, true).Length);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            // okay, our dictionary has the best matche(s)
            string best_string = string.Empty;
            int longest_match = 0;
            foreach (KeyValuePair<string, int> kvp in dic)
            {
                if (best_string == string.Empty)
                {
                    best_string = kvp.Key;
                    longest_match = kvp.Value;
                    continue;
                }
                if (kvp.Value > longest_match)
                {   // save the best string match
                    best_string = kvp.Key;
                    longest_match = kvp.Value;
                }
            }
            text = best_string;
            return;
        }
        private static bool NearByExcluding(Mobile m, Mobile exclude, Type t)
        {
            int range = 5;  // how close our player needs to be in range of another player (maybe they are talking)
            int count = 0;

            if (m_runningBackground)
            {
                Dictionary<Mobile, Point3D> dic = (t == typeof(PlayerMobile) ? CacheFactory.PlayersQuickTable : CacheFactory.MobilesQuickTable);
                foreach (Mobile found in dic.Keys)
                    if (found != null)
                        if (found.GetType().IsAssignableFrom(t) && found != exclude)
                            if (m.GetDistanceToSqrt(found) <= range)
                                count++;
            }
            else
            {
                IPooledEnumerable eable = m.Map.GetMobilesInRange(exclude.Location, range);
                foreach (Mobile found in eable)
                    if (found != null)
                        if (found.GetType().IsAssignableFrom(t) && found != exclude)
                            count++;

                eable.Free();
            }

            return count > 0;
        }
        private static bool NearBy(Mobile m, Type t)
        {
            int range = 13;
            if (m is BaseCreature bc)
                range = bc.RangePerception;

            if (m_runningBackground)
            {
                Dictionary<Mobile, Point3D> dic = (t == typeof(PlayerMobile) ? CacheFactory.PlayersQuickTable : CacheFactory.MobilesQuickTable);
                foreach (Mobile found in dic.Keys)
                    if (found != null)
                        if (found.GetType().IsAssignableFrom(t))
                            if (m.GetDistanceToSqrt(found) <= range)
                                return true;
            }
            else
            {
                IPooledEnumerable eable = m.Map.GetMobilesInRange(m.Location, range);
                foreach (Mobile found in eable)
                    if (found != null)
                        if (found.GetType().IsAssignableFrom(t))
                        {
                            eable.Free();
                            return true;
                        }

                eable.Free();
            }
            return false;
        }
        private static bool NearBy(Mobile m, ref Mobile target, Type t)
        {
            int range = 13;
            if (m is BaseCreature bc)
                range = bc.RangePerception;

            if (m_runningBackground)
            {
                Dictionary<Mobile, Point3D> dic = (t == typeof(PlayerMobile) ? CacheFactory.PlayersQuickTable : CacheFactory.MobilesQuickTable);
                foreach (Mobile found in dic.Keys)
                    if (found != null)
                        if (t.IsAssignableFrom(found.GetType()))
                            if (m.GetDistanceToSqrt(found) <= range)
                            {
                                target = found;
                                return true;
                            }
            }
            else
            {
                IPooledEnumerable eable = m.Map.GetMobilesInRange(m.Location, range);
                foreach (Mobile found in eable)
                    if (found != null)
                        if (t.IsAssignableFrom(found.GetType()))
                        {
                            eable.Free();
                            target = found;
                            return true;
                        }

                eable.Free();
            }

            target = null;
            return false;
        }
        private static string NormalizeQuery(string s)
        {
            // remove noise
            //  Our fuzzy matcher can really be thrown off by 'noise' so remove as much as possible.
            string text = s.ToLower();

            // chop it up and 
            string[] chunk = text.Split(new char[] { ' ', '.', '?', '-' }, StringSplitOptions.RemoveEmptyEntries);

            // rebuild our clean query
            text = "";
            foreach (string word in chunk)
                text += word + " ";
            text = text.Trim();
            return text;
        }

        private static void GiveDirectionalHint(Mobile from, Mobile to, Point2D location)
        {
            string direction = from.GetDirectionTo(location).ToString();
            if (direction == "Left") direction = "South-West";
            if (direction == "Right") direction = "North-East";
            if (direction == "Up") direction = "North-West";
            if (direction == "Down") direction = "South-East";
            if (direction == "Mask") direction = "North-West";  // same as up

            double distance = from.GetDistanceToSqrt(location);

            if (distance > 200)
                SayTo(from, to, string.Format("Tis a long journey {0} from here.", direction));
            else if (distance > 150)
                SayTo(from, to, string.Format("Tis quite a long distance {0} from here.", direction));
            else if (distance > 100)
                SayTo(from, to, string.Format("Tis a long way {0} from here.", direction));
            else if (distance > 75)
                SayTo(from, to, string.Format("Tis a fair distance {0} from there.", direction));
            else if (distance > 50)
                SayTo(from, to, string.Format("Tis quite a ways {0} from here.", direction));
            else if (distance > 40)
                SayTo(from, to, string.Format("Tis just a short way {0} from here.", direction));
            else if (distance > 20)
                SayTo(from, to, string.Format("Tis but a few steps {0} from here.", direction));
            else
                SayTo(from, to, string.Format("But you are here!"));
        }
        #region RollingHash
        public class RollingHash
        {
            private class RollingHashPowers
            {
                // _mod = prime modulus of polynomial hashing
                // any prime number over a billion should suffice
                internal const int _mod = (int)1e9 + 123;
                // _hashBase = base (point of hashing)
                // this should be a prime number larger than the number of characters used
                // in my use case I am only interested in ASCII (256) characters
                // for strings in languages using non-latin characters, this should be much larger
                internal const long _hashBase = 257;
                // _pow1 = powers of base modulo mod
                internal readonly List<int> _pow1 = new List<int> { 1 };
                // _pow2 = powers of base modulo 2^64
                internal readonly List<long> _pow2 = new List<long> { 1L };

                internal void EnsureLength(int length)
                {
                    if (_pow1.Capacity < length)
                    {
                        _pow1.Capacity = _pow2.Capacity = length;
                    }
                    for (int currentIndx = _pow1.Count - 1; currentIndx < length; ++currentIndx)
                    {
                        _pow1.Add((int)(_pow1[currentIndx] * _hashBase % _mod));
                        _pow2.Add(_pow2[currentIndx] * _hashBase);
                    }
                }
            }

            private class RollingHashedString
            {
                readonly RollingHashPowers _pows;
                readonly int[] _pref1; // Hash on prefix modulo mod
                readonly long[] _pref2; // Hash on prefix modulo 2^64

                // Constructor from string:
                internal RollingHashedString(RollingHashPowers pows, string s, bool caseInsensitive = false)
                {
                    _pows = pows;
                    _pref1 = new int[s.Length + 1];
                    _pref2 = new long[s.Length + 1];

                    const long capAVal = 'A';
                    const long capZVal = 'Z';
                    const long aADif = 'a' - 'A';

                    unsafe
                    {
                        fixed (char* c = s)
                        {
                            // Fill arrays with polynomial hashes on prefix
                            for (int i = 0; i < s.Length; ++i)
                            {
                                long v = c[i];
                                if (caseInsensitive && capAVal <= v && v <= capZVal)
                                {
                                    v += aADif;
                                }
                                _pref1[i + 1] = (int)((_pref1[i] + v * _pows._pow1[i]) % RollingHashPowers._mod);
                                _pref2[i + 1] = _pref2[i] + v * _pows._pow2[i];
                            }
                        }
                    }
                }

                // Rollingnomial hash of subsequence [pos, pos+len)
                // If mxPow != 0, value automatically multiply on base in needed power.
                // Finally base ^ mxPow
                internal Tuple<int, long> Apply(int pos, int len, int mxPow = 0)
                {
                    int hash1 = _pref1[pos + len] - _pref1[pos];
                    long hash2 = _pref2[pos + len] - _pref2[pos];
                    if (hash1 < 0)
                    {
                        hash1 += RollingHashPowers._mod;
                    }
                    if (mxPow != 0)
                    {
                        hash1 = (int)((long)hash1 * _pows._pow1[mxPow - (pos + len - 1)] % RollingHashPowers._mod);
                        hash2 *= _pows._pow2[mxPow - (pos + len - 1)];
                    }
                    return Tuple.Create(hash1, hash2);
                }
            }

            private readonly RollingHashPowers _rhp;
            public RollingHash(int longestLength = 0)
            {
                _rhp = new RollingHashPowers();
                if (longestLength > 0)
                {
                    _rhp.EnsureLength(longestLength);
                }
            }

            public string FindCommonSubstring(string a, string b, bool caseInsensitive = false)
            {
                // Calculate max neede power of base:
                int mxPow = Math.Max(a.Length, b.Length);
                _rhp.EnsureLength(mxPow);
                // Create hashing objects from strings:
                RollingHashedString hash_a = new RollingHashedString(_rhp, a, caseInsensitive);
                RollingHashedString hash_b = new RollingHashedString(_rhp, b, caseInsensitive);

                // Binary search by length of same subsequence:
                int pos = -1;
                int low = 0;
                int minLen = Math.Min(a.Length, b.Length);
                int high = minLen + 1;
                var tupleCompare = Comparer<Tuple<int, long>>.Default;
                while (high - low > 1)
                {
                    int mid = (low + high) / 2;
                    List<Tuple<int, long>> hashes = new List<Tuple<int, long>>(a.Length - mid + 1);
                    for (int i = 0; i + mid <= a.Length; ++i)
                    {
                        hashes.Add(hash_a.Apply(i, mid, mxPow));
                    }
                    hashes.Sort(tupleCompare);
                    int p = -1;
                    for (int i = 0; i + mid <= b.Length; ++i)
                    {
                        if (hashes.BinarySearch(hash_b.Apply(i, mid, mxPow), tupleCompare) >= 0)
                        {
                            p = i;
                            break;
                        }
                    }
                    if (p >= 0)
                    {
                        low = mid;
                        pos = p;
                    }
                    else
                    {
                        high = mid;
                    }
                }
                // Output answer:
                return pos >= 0
                    ? b.Substring(pos, low)
                    : string.Empty;
            }
        }
        #endregion RollingHash
        #region Levenshtein
        /// <summary>
        /// 2. Levenshtein Distance Algorithm:
        /// The Levenshtein distance is a string metric for measuring the difference between two sequences.
        /// The Levenshtein distance between two words is the minimum number of single-character edits (i.e.insertions, deletions or substitutions) required 
        /// to change one word into the other.It is named after Vladimir Levenshtein.
        /// https://www.csharpstar.com/csharp-string-distance-algorithm/
        /// </summary>
        public static class StringDistance
        {
            /// <summary>
            /// Compute the distance between two strings.
            /// </summary>
            public static int LevenshteinDistance(string s, string t)
            {
                int n = s.Length;
                int m = t.Length;
                int[,] d = new int[n + 1, m + 1];

                // Step 1
                if (n == 0)
                {
                    return m;
                }

                if (m == 0)
                {
                    return n;
                }

                // Step 2
                for (int i = 0; i <= n; d[i, 0] = i++)
                {
                }

                for (int j = 0; j <= m; d[0, j] = j++)
                {
                }

                // Step 3
                for (int i = 1; i <= n; i++)
                {
                    //Step 4
                    for (int j = 1; j <= m; j++)
                    {
                        // Step 5
                        int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                        // Step 6
                        d[i, j] = Math.Min(
                            Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                            d[i - 1, j - 1] + cost);
                    }
                }
                // Step 7
                return d[n, m];
            }
        }
        public static class Levenshtein
        {
            public static string BestEnumMatchPure(Type actualTypes, string what_to_match, out int score)
            {
                what_to_match = RemoveWhitespace(what_to_match).ToLower();
                score = 128;    // just some big number (fail)
                int distance = score;
                string found = null;

                // first see if the string matches.
                foreach (string enum_name in Enum.GetNames(actualTypes))
                {
                    distance = StringDistance.LevenshteinDistance(enum_name.ToLower(), what_to_match);
                    if (distance < score)
                    {
                        score = distance;
                        found = enum_name;
                    }

                }
                return found;
            }
            public static string BestEnumMatch(Type actualTypes, string whatVendorThinks, out int score)
            {   // first remove all whitespace
                whatVendorThinks = RemoveWhitespace(whatVendorThinks).ToLower();

                // first see if the string matches.
                foreach (string skill in Enum.GetNames(actualTypes))
                {
                    if (string.Compare(skill, whatVendorThinks, true) == 0)
                    { // the string matches the string name of the enum value. Just return it. 
                        score = 0; // perfect match
                        return whatVendorThinks;
                    }
                }

                // we didn't find an exact match, so we will try to figure out the best match.
                int body = 0;
                int head = 0;
                string found = null;
                foreach (string skill in Enum.GetNames(actualTypes))
                {
                    int new_body_score = LongestBodyMatch(whatVendorThinks, skill.ToLower());
                    int new_head_score = LongestHeadMatch(whatVendorThinks, skill.ToLower()); // head score out ranks body score

                    // >= body since we always prefer a head-match
                    if (new_head_score > head && new_head_score >= body)
                    {   // 
                        head = new_head_score;
                        found = skill;
                    }
                    else if (new_body_score > body && new_body_score >= head)
                    {   // 
                        body = new_body_score;
                        found = skill;
                    }
                    else if (new_body_score == body && head <= new_body_score)
                    {   // see if we can disambiguate between the two low scorers
                        // if we have a previous find, see which one rates lower in 'distance'
                        if (found != null)
                        {
                            int distance1 = StringDistance.LevenshteinDistance(skill.ToLower(), whatVendorThinks);
                            int distance2 = StringDistance.LevenshteinDistance(found.ToLower(), whatVendorThinks);
                            if (distance1 < distance2)
                                found = skill;
                        }
                        else
                            found = skill;
                    }
                }
                // a high score, say > 5, probably means this isn't the word they are looking for.
                //	the caller should reject high scores.
                score = StringDistance.LevenshteinDistance(found.ToLower(), whatVendorThinks);
                return found;
            }

            public static string BestListMatch(List<string> itemNames, string whatPlayerWants, out int score)
            {   // first remove all whitespace
                whatPlayerWants = RemoveWhitespace(whatPlayerWants).ToLower();

                // first see if the string matches.
                foreach (string temp in itemNames)
                {
                    if (string.Compare(temp, whatPlayerWants, true) == 0)
                    { // the string matches the string name of the enum value. Just return it. 
                        score = 0; // perfect match
                        return whatPlayerWants;
                    }
                }

                // we didn't find an exact match, so we will try to figure out the best match.
                int body = 0;
                int head = 0;
                int wants_len = whatPlayerWants.Length;
                string found = null;
                foreach (string temp in itemNames)
                {
                    int new_body_score = LongestBodyMatch(whatPlayerWants, temp.ToLower());
                    int new_head_score = LongestHeadMatch(whatPlayerWants, temp.ToLower()); // head score out ranks body score
                    int demerits = Math.Abs(temp.Length - wants_len);
                    new_body_score -= demerits;
                    new_head_score -= demerits;

                    // >= body since we always prefer a head-match
                    if (new_head_score > head && new_head_score >= body)
                    {   // 
                        head = new_head_score;
                        found = temp;
                    }
                    else if (new_body_score > body && new_body_score >= head)
                    {   // 
                        body = new_body_score;
                        found = temp;
                    }
                    else if (new_body_score == body && head <= new_body_score)
                    {   // see if we can disambiguate between the two low scorers
                        // if we have a previous find, see which one rates lower in 'distance'
                        if (found != null)
                        {
                            int distance1 = StringDistance.LevenshteinDistance(temp.ToLower(), whatPlayerWants);
                            int distance2 = StringDistance.LevenshteinDistance(found.ToLower(), whatPlayerWants);
                            if (distance1 < distance2)
                                found = temp;
                        }
                        else
                            found = temp;
                    }
                }
                // a high score, say > 5, probably means this isn't the word they are looking for.
                //	the caller should reject high scores.
                score = StringDistance.LevenshteinDistance(found.ToLower(), whatPlayerWants);
                return found;
            }

            public static int LongestHeadMatch(string source, string tocheck)
            {
                int matchlen = 0;
                for (int ix = 0; ix < source.Length && ix < tocheck.Length; ix++)
                {
                    if (Char.ToLower(source[ix]) == Char.ToLower(tocheck[ix]))
                        matchlen++;
                    else
                        break;
                }

                return matchlen;
            }

            public static int LongestBodyMatch(string source, string pattern)
            {
                for (int endOffset = pattern.Length; endOffset >= 1; endOffset--)
                {
                    string match = null;
                    if (source.IndexOf(match = pattern.Substring(0, endOffset)) != -1 || source.IndexOf(match = pattern.Substring(pattern.Length - endOffset + 1)) != -1)
                    {
                        return match.Length;
                    }
                }
                return 0;
            }
            public static string RemoveWhitespace(string input)
            {
                string temp = "";
                foreach (char cx in input)
                {
                    if (cx == ' ') continue;
                    temp += cx;
                }

                return temp;
            }
        }
        #endregion Levenshtein
        private static Queue<string> m_outputQueue = new Queue<string>();
        public static void SayTo(Mobile from, Mobile to, string text)
        {
            from.SayTo(to, text);
            if (IDSRegressionTest.RegressionRecorderEnabled)
            {
                if (IDSRegressionTest.Logger != null)
                    IDSRegressionTest.Logger.Log("=> " + text);
            }
            // save this for our memory cache
            m_outputQueue.Enqueue(text);
        }
        public static void LoadPlayerCache(Point3D location)
        {   // this is never called when we are running as a background thread:
            // Since our IDS runs in it's own thread, we can't safely enumerate mobiles
            //  we will create our own private cache
            CacheFactory.PlayersQuickTable.Clear();
            IPooledEnumerable eable = Map.Felucca.GetMobilesInRange(location, 20);
            foreach (Mobile found in eable)
                if (found != null && found is PlayerMobile)
                    CacheFactory.PlayersQuickTable.Add(found, found.Location);

            eable.Free();
        }
        public static void LoadMobilesCache()
        {   // this is never called when we are running as a background thread:
            // Since our IDS runs in it's own thread, we can't safely enumerate mobiles
            //  we will create our own private cache
            CacheFactory.MobilesQuickTable.Clear();
            foreach (Mobile mob in World.Mobiles.Values)
            {
                if (mob == null || mob is RentedVendor || mob.Map != Map.Felucca)
                    continue;

                if (mob is BaseVendor || mob is BaseGuard)
                    CacheFactory.MobilesQuickTable.Add(mob, mob.Location);
            }
            return;
        }
        #region Serialization
        public static void Load()
        {
            if (!File.Exists("Saves/IntelligentDialogue.bin"))
                return;

            Console.WriteLine("Intelligent Dialogue Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/IntelligentDialogue.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    /*
                     public class MemoryObj
                    {
                        private List<string> m_textOutput = new List<string>();
                        public List<string> TextOutput => m_textOutput;
                        private DateTime m_expiry = DateTime.UtcNow + TimeSpan.FromHours(2);
                        public DateTime Expiry => m_expiry;
                        public MemoryObj()
                        { }
                    } 
                    private static Dictionary<string, MemoryObj> m_memory = new Dictionary<string, MemoryObj>(StringComparer.OrdinalIgnoreCase);
                    */
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                string key = reader.ReadString();
                                int nChatter = reader.ReadInt();
                                List<string> list = new List<string>();
                                for (int jx = 0; jx < nChatter; jx++)
                                    list.Add(reader.ReadString());
                                DateTime dateTime = reader.ReadDateTime();
                                MemoryObj mo = new MemoryObj(list, dateTime);
                                m_memory.Add(key, mo);
                            }
                            break;
                        }

                    default:
                        {
                            throw new Exception("Invalid IntelligentDialogue.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading IntelligentDialogue.bin, using default values:", ConsoleColor.Red);
            }
        }
        public static void Save(WorldSaveEventArgs e)
        {
            Console.WriteLine("Intelligent Dialogue Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/IntelligentDialogue.bin", true);
                int version = 1;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 1:
                        {
                            writer.Write(m_memory.Count);
                            foreach (KeyValuePair<string, MemoryObj> kvp in m_memory)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value.TextOutput.Count);
                                for (int i = 0; i < kvp.Value.TextOutput.Count; i++)
                                    writer.Write(kvp.Value.TextOutput[i]);
                                writer.Write(kvp.Value.Expiry);
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing IntelligentDialogue.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }
}