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

#if false


/* Scripts/Regions/SensoryRegion.cs
 * CHANGELOG
 *	11/1/10, Adam
 *		If the remembered item is deleted (or freeze dried) it's serialized as zero which is normal
 *		item bahavior. However, we were then trying to add a null ietm to the databasse and blowing things up.
 *		I added code to skil deleted items, but we should probably come up with a better way to handle this.
 *  10/30/10, Adam
 *      SensoryConsole vs SensoryZoneDeeds
 *      The SensoryConsole is used by GMm to create persistant zones, or zone databases that can be packed up and stored for reuse.
 *      The SensoryZoneDeeds are for use by both Players and staff. When used by a Player the zone must get placed in the house,
 *      and the zone controller gets added to the houses addons and is thus auto deleted when the house is deleted.
 *	10/15/10, Adam
 *		Create a new region type that process sensory information and provides a rich interface for
 *		world objects. This sensory region is built upon the TransparentRegion which Delegates all unhandled 
 *		events to the 'parent' region. 	The 'Parent' region being that region under this POINT with the 
 *		highest priority.
 */

using Server.Items;
using Server.Mobiles;
using Server.Prompts;
using Server.Regions;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Regions
{
    public class SensoryRegion : TransparentRegion
    {
        SensoryConsole m_console;
        Mobile m_From;
        private enum Disposition
        {
            Keep,
            Return,
            Delete
        }
        //private Disposition m_disposition = Disposition.Keep;
        public enum Field
        {
            Name,
            Track,
        }
        public SensoryRegion(SensoryConsole console, Map map)
            : base(map)
        {
            m_console = console;
        }

        #region OnEnter
        public override void OnEnter(Mobile m)
        {
            base.OnEnter(m);

            // yeah
            if (m is PlayerMobile == false)
                return;

            // sanity
            if (m.Deleted)
                return;

            if (m_console.PlayerMemory.Recall(m) == false /*&& this.GetDistanceToSqrt(m) <= m_distance*/)
            {   // we havn't seen this player yet
                m_console.PlayerMemory.Remember(m, m_console.Memory.TotalSeconds);          // remember him for this long
                bool found = m_console.KeywordDatabase.ContainsKey("onenter");              // is OnEnter defined?
                if (found)                                                                  // if so execute!
                    OnSpeech(new SpeechEventArgs(m, "onenter", Server.Network.MessageType.Regular, 0x3B2, new int[0], true));
            }
        }
        #endregion OnEnter

        #region OnSpeech
        public override void OnSpeech(SpeechEventArgs e)
        {
            try
            {
                #region setup
                Mobile from = e.Mobile;
                m_From = from;

                if (e.Handled)
                    return;

                // if this is a player owned sensory zone, allow configuration
                Multis.BaseHouse house = Multis.BaseHouse.FindHouseAt(from);
                bool bIsHouseOwner = false;
                if (house != null)
                {
                    bIsHouseOwner = house.IsCoOwner(from);
                }

                // compile speech into an array of strings and delimiters
                object[] tokens = Compile(e.Speech);

                // remove the optional handle, i.e., "zone"
                // Distinguishes between zones and Quest Giver NPC�s and TEST CENTER �set� commands
                tokens = RemoveHandle(tokens, "zone");

                #endregion setup

                #region COMMANDS (remember, clear)
                if (e.Handled == false && (tokens[0] as string).ToLower() == "remember")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;

                        // okay, process the special OnReceive keyword by prompting to target an item
                        e.Mobile.Target = new OnReceiveTarget(this);
                        e.Mobile.SendMessage("Target the item to remember.");
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "labels")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;
                        int count = m_console.ItemDatabase.Count;
                        m_console.ItemDatabase.Clear();
                        from.SendMessage(string.Format("Memory of {0} items cleared.", count));
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "keywords")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;
                        int count = m_console.KeywordDatabase.Count;
                        m_console.KeywordDatabase.Clear();
                        from.SendMessage(string.Format("Memory of {0} keywords cleared.", count));
                    }
                }
                if (e.Handled == false && (tokens[0] as string).ToLower() == "reset")
                {
                    if (IsOwner(from) || bIsHouseOwner)
                    {
                        e.Handled = true;
                        int count = m_console.ItemDatabase.Count;
                        m_console.ItemDatabase.Clear();
                        from.SendMessage(string.Format("Memory of {0} items cleared.", count));
                        count = m_console.KeywordDatabase.Count;
                        m_console.KeywordDatabase.Clear();
                        from.SendMessage(string.Format("Memory of {0} keywords cleared.", count));
                    }
                }
                #endregion

                #region GET & SET
                // process owner programming commands GET & SET
                if (e.Handled == false && (IsOwner(from) || bIsHouseOwner))
                {
                    if (tokens[0] is string && (tokens[0] as string).ToLower() == "get")
                    {   // does it look like a reasonable command?
                        if (tokens.Length > 1 && tokens[1] is string)
                        {
                            e.Handled = true;
                            switch ((tokens[1] as string).ToLower())
                            {
                                //case "distance":
                                //from.SendMessage("distance is set to {0} tiles.", m_distance);
                                //break;

                                case "memory":
                                    from.SendMessage("Memory set to {0} minutes.", m_console.Memory);
                                    break;
                            }
                        }
                    }

                    if (tokens[0] is string && (tokens[0] as string).ToLower() == "set")
                    {   // does it look like a reasonable command?
                        if (tokens.Length > 2 && tokens[1] is string && tokens[2] is string)
                        {
                            e.Handled = true;
                            switch ((tokens[1] as string).ToLower())
                            {
                                /*case "name":
									{
										// Pattern match for invalid characters
										Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");
										string text = e.Speech.Substring(e.Speech.IndexOf(tokens[2] as string, 0, StringComparison.CurrentCultureIgnoreCase)).Trim();
										if (InvalidPatt.IsMatch(text))
										{
											// Invalid chars
											from.SendMessage("You may only use numbers, letters, apostrophes, hyphens and spaces.");
										}
										else if (!Misc.NameVerification.Validate(text, 2, 16, true, true, true, 1, Misc.NameVerification.SpaceDashPeriodQuote))
										{
											// Invalid for some other reason
											from.SendMessage("That name is not allowed here.");
										}
										else if (true)
										{
											Name = text;
											from.SendMessage("Set.");
										}
										else
											from.SendMessage("Usage: set name <name string>");
									}
									break;*/

                                /*case "title":
									{
										// Pattern match for invalid characters
										Regex InvalidPatt = new Regex("[^-a-zA-Z0-9' ]");
										string text = e.Speech.Substring(e.Speech.IndexOf(tokens[2] as string, 0, StringComparison.CurrentCultureIgnoreCase)).Trim();
										if (InvalidPatt.IsMatch(text))
										{
											// Invalid chars
											from.SendMessage("You may only use numbers, letters, apostrophes, hyphens and spaces.");
										}
										else if (!Misc.NameVerification.Validate(text, 2, 16, true, true, true, 1, Misc.NameVerification.SpaceDashPeriodQuote))
										{
											// Invalid for some other reason
											from.SendMessage("That title is not allowed here.");
										}
										else if (true)
										{
											Title = text;
											from.SendMessage("Set.");
										}
										else
											from.SendMessage("Usage: set title <name string>");
									}
									break;*/

                                /*case "distance":
									{
										int result;
										// max 12 tiles
										if (int.TryParse(tokens[2] as string, out result) && result >= 0 && result < 12)
										{
											m_distance = result;
											from.SendMessage("Set.");
										}
										else
											from.SendMessage("Usage: set distance <number>");
									}
									break;*/

                                case "memory":
                                    {
                                        double result;
                                        // max 72 hours
                                        if (double.TryParse(tokens[2] as string, out result) && result > 0 && result < TimeSpan.FromHours(72).TotalMinutes)
                                        {
                                            m_console.Memory = TimeSpan.FromMinutes(result);
                                            m_console.PlayerMemory = new Memory();
                                            from.SendMessage("Set.");
                                        }
                                        else
                                            from.SendMessage("Usage: set memory <number>");
                                    }
                                    break;
                            }
                        }
                    }
                }
                #endregion

                #region owner programming commands (keywords)
                // process owner programming commands
                if (e.Handled == false && (IsOwner(from) || bIsHouseOwner))
                {
                    // we now have a compiled list of strings and tokens
                    // find out what the user is constructing
                    //
                    // if the user is inserting a keyword, make sure it doesn't already exist and insert it
                    // if the user is adding verbs, append to named keyword

                    bool understood = true;

                    // if the user is inserting a keyword, make sure it doesn't already exist and insert it
                    if (tokens[0] is string && ((tokens[0] as string).ToLower() == "keyword" || (tokens[0] as string).ToLower() == "add"))
                    {   // does it look like a reasonable command?
                        if (tokens.Length > 1 && tokens[1] is string)
                        {
                            e.Handled = true;

                            string good, bad;
                            bool ck = ComputeKeywords(tokens, 1, false, out good, out bad);

                            if (!ck)
                                from.SendMessage(string.Format("The following keyword(s) are already defined: {0}", bad));
                            else if ((tokens[1] as string).ToLower() == "onreceive")
                                from.SendMessage(string.Format("Usage: Add OnReceive.<item>"));
                            else if (m_console.KeywordDatabase.Count >= 256)
                                from.SendMessage(string.Format("Your keyword database is full."));
                            else
                            {
                                // okay, we don't have the keyword(s) yet, so lets add them (with a null action)
                                // remove the first token 'keywords'

                                object[][] chunks = SplitArray(tokens, '|');
                                string keyword = chunks[0][1] as string;

                                // shared placeholder for all of these keywords and aliases
                                object[] actions = new object[0];

                                object[] kwords = chunks[0];
                                for (int ix = 1; ix < kwords.Length; ix++)
                                {   // skip delimiters
                                    if (kwords[ix] is string)
                                        m_console.KeywordDatabase[(kwords[ix] as string).ToLower()] = actions;
                                }

                                // OPTIONAL: extract the actions
                                for (int ix = 1; ix < chunks.Length; ix++)
                                {
                                    object[] action = chunks[ix];
                                    // these are the actions
                                    OnSpeech(new SpeechEventArgs(from, "action " + keyword + " " + MakeString(action, 0), Server.Network.MessageType.Regular, 0x3B2, new int[0]));
                                }

                                from.SendMessage(string.Format("Okay."));
                            }
                        }
                        else
                            from.SendMessage(string.Format("Usage: '{0}' keyword1[, keyword2, keyword3].", tokens[0] as string));
                    }
                    else if (tokens[0] is string && ((tokens[0] as string).ToLower() == "action" || (tokens[0] as string).ToLower() == "verb"))
                    {
                        if (tokens.Length > 2 && tokens[1] is string && tokens[2] is string)
                        {
                            e.Handled = true;
                            if (m_console.KeywordDatabase.ContainsKey((tokens[1] as string).ToLower()))
                            {   // we have all the named keywords
                                // locate the verb
                                if (CheckVerb((tokens[2] as string).ToLower()))
                                {   // append the verb list to any existing verb list
                                    object[] verbList = m_console.KeywordDatabase[(tokens[1] as string).ToLower()]; // the action for keyword - never null
                                    object[] action;                                // the new action

                                    if (MakeString(tokens, 0).Length + MakeString(verbList, 0).Length > 768)
                                        from.SendMessage(string.Format("Action too long for this keyword."));
                                    else
                                    {
                                        // oldAction.Length + 1 for the new action delimiter + the new action length - 2
                                        // We remove the first two tokens: 'action' 'keyword'. then append to the action for this keyword
                                        bool delimiter = verbList.Length > 0;
                                        action = new object[verbList.Length + (delimiter ? 1 : 0) + tokens.Length - 2];
                                        Array.Copy(verbList, 0, action, 0, verbList.Length);
                                        if (delimiter) action[verbList.Length] = '|' as object;
                                        Array.Copy(tokens, 2, action, verbList.Length + (delimiter ? 1 : 0), tokens.Length - 2);

                                        // okay. Now we have a new action. We want to associate it with all keywords that share the same action
                                        List<string> tomod = new List<string>();
                                        foreach (KeyValuePair<string, object[]> kvp in m_console.KeywordDatabase)
                                        {
                                            if (kvp.Value == verbList)
                                            {   // this keyword has one of the shared actions
                                                tomod.Add(kvp.Key);
                                            }
                                        }
                                        foreach (string sx in tomod)
                                            m_console.KeywordDatabase[sx] = action;

                                        from.SendMessage(string.Format("Okay."));
                                    }
                                }
                                else
                                    from.SendMessage(string.Format("I do not know the verb {0}.", tokens[2] as string));
                            }
                            else
                                from.SendMessage(string.Format("I do not have the keyword(s) {0}.", tokens[1] as string));
                        }
                        else
                            from.SendMessage(string.Format("Usage: '{0}' 'keyword' 'verb' text.", tokens[0] as string));
                    }
                    else if (tokens.Length == 1 && tokens[0] is string && (tokens[0] as string).ToLower() == "list")
                    {   // list everything
                        OnSpeech(new SpeechEventArgs(from, "list keywords", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                        OnSpeech(new SpeechEventArgs(from, "list labels", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "list" && tokens[1] is string && (tokens[1] as string).ToLower() == "labels")
                    {   // list the labels

                        if (m_console.ItemDatabase.Count > 0)
                        {
                            foreach (KeyValuePair<Item, object[]> kvp in m_console.ItemDatabase)
                            {
                                if (kvp.Key == null)
                                    continue;

                                string where = GetItemLocation(kvp.Key);
                                if (World.FindItem(kvp.Key.Serial) == null)
                                    where = "location unknown";
                                from.SendMessage(string.Format("{0} [{1}]", GetField(kvp.Value, Field.Name), where));
                            }
                        }
                        else
                            from.SendMessage(string.Format("There are no labels defined."));
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "list" && tokens[1] is string && (tokens[1] as string).ToLower() == "keywords")
                    {   // list the keywords specified
                        if (tokens.Length > 2 && tokens[2] is string)
                        {   // loop over all of the keywords and delete it if it exists.
                            e.Handled = true;

                            string good, bad;
                            bool ck = ComputeKeywords(tokens, 2, true, out good, out bad);

                            if (!ck)
                                from.SendMessage(string.Format("The following keyword(s) are not defined: {0}", bad));

                            string[] good_array, bad_array;
                            ComputeKeywords(tokens, 2, true, out good_array, out bad_array);

                            List<string> remember = new List<string>();
                            foreach (string sx in good_array)
                            {
                                if (remember.Contains(sx) == false)
                                {   // add this keyword and aliases to the 'remember' array
                                    string aliases;                             // display list of keyword and aliases
                                    FindKeywordAliases(sx, out aliases);        // find 'em
                                    string[] aliases_array;                     // list of keyword and aliases
                                    FindKeywordAliases(sx, out aliases_array);  // find 'em
                                    foreach (string ux in aliases_array)        // remember that we have processed 'em
                                        remember.Add(ux);

                                    // tell the user what we are listing (all aliases are deleted with the keyword)
                                    from.SendMessage(string.Format("The following keywords: {0}", aliases));

                                    if (m_console.KeywordDatabase[aliases_array[0]].Length == 0)
                                        from.SendMessage(string.Format("Have associated actions: {0}.", "<null>"));
                                    else
                                    {
                                        from.SendMessage(string.Format("Have associated actions:"));
                                        object[][] actions = SplitArray(m_console.KeywordDatabase[aliases_array[0]], '|');
                                        foreach (object[] action in actions)
                                            from.SendMessage(string.Format("{0}", MakeString(action, 0)));
                                    }
                                }
                            }
                        }
                        else
                        {   // list all keywords and associated actions
                            if (m_console.KeywordDatabase.Count > 0)
                            {
                                List<string> good_list = new List<string>();
                                foreach (KeyValuePair<string, object[]> kvp in m_console.KeywordDatabase)
                                    good_list.Add(kvp.Key);

                                string[] good_array = good_list.ToArray();
                                List<string> remember = new List<string>();
                                foreach (string sx in good_array)
                                {
                                    if (remember.Contains(sx) == false)
                                    {   // add this keyword and aliases to the 'remember' array
                                        string aliases;                             // display list of keyword and aliases
                                        FindKeywordAliases(sx, out aliases);        // find 'em
                                        string[] aliases_array;                     // list of keyword and aliases
                                        FindKeywordAliases(sx, out aliases_array);  // find 'em
                                        foreach (string ux in aliases_array)        // remember that we have processed 'em
                                            remember.Add(ux);

                                        // tell the user what we are listing (all aliases are deleted with the keyword)
                                        from.SendMessage(string.Format("The following keywords: {0}", aliases));

                                        if (m_console.KeywordDatabase[aliases_array[0]].Length == 0)
                                            from.SendMessage(string.Format("Have associated actions: {0}.", "<null>"));
                                        else
                                        {
                                            from.SendMessage(string.Format("Have associated actions:"));
                                            object[][] actions = SplitArray(m_console.KeywordDatabase[aliases_array[0]], '|');
                                            foreach (object[] action in actions)
                                                from.SendMessage(string.Format("{0}", MakeString(action, 0)));
                                        }
                                    }
                                }
                            }
                            else
                                from.SendMessage(string.Format("There are no keywords defined."));
                        }
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "delete")
                    {
                        if ((tokens[1] as string).ToLower() == "zone")
                        {
                            if (tokens.Length == 2)
                            {   // loop over all of the labels and delete it if it exists.
                                e.Handled = true;

                                // if the console was added to the houses, remove it
                                if (house != null && house.Addons != null && house.Contains(new Point3D(m_console.Begin, 0)))
                                    if (house.Addons.Contains(m_console) == true)
                                        house.Addons.Remove(m_console);

                                m_console.Delete();

                                from.SendMessage(string.Format("zone deleted."));
                            }
                            else
                                from.SendMessage(string.Format("Usage: '{0}' zone.", tokens[0] as string));
                        }
                        else if ((tokens[1] as string).ToLower() == "label")
                        {
                            if (tokens.Length > 2 && tokens[2] is string)
                            {   // loop over all of the labels and delete it if it exists.
                                e.Handled = true;

                                List<Item> list = new List<Item>();
                                foreach (KeyValuePair<Item, object[]> kvp in m_console.ItemDatabase)
                                {
                                    if ((GetField(kvp.Value, Field.Name) as string).ToLower() == (tokens[2] as string).ToLower())
                                        list.Add(kvp.Key);
                                }
                                foreach (Item ix in list)
                                    m_console.ItemDatabase.Remove(ix);

                                from.SendMessage(string.Format("{0} '{1}' labels cleared.", list.Count, tokens[2] as string));
                            }
                            else
                                from.SendMessage(string.Format("Usage: '{0}' label <label>.", tokens[0] as string));
                        }
                        else if ((tokens[1] as string).ToLower() == "keyword")
                        {
                            if (tokens.Length > 2 && tokens[2] is string)
                            {   // loop over all of the keywords and delete it if it exists.
                                e.Handled = true;

                                string good, bad;
                                bool ck = ComputeKeywords(tokens, 2, true, out good, out bad);

                                if (!ck)
                                    from.SendMessage(string.Format("The following keyword(s) are not defined: {0}", bad));

                                string[] good_array, bad_array;
                                ComputeKeywords(tokens, 2, true, out good_array, out bad_array);

                                List<string> remember = new List<string>();
                                foreach (string sx in good_array)
                                {
                                    if (remember.Contains(sx) == false)
                                    {   // add this keyword and aliases to the 'remember' array
                                        string aliases;                             // display list of keyword and aliases
                                        FindKeywordAliases(sx, out aliases);        // find 'em
                                        string[] aliases_array;                     // list of keyword and aliases
                                        FindKeywordAliases(sx, out aliases_array);  // find 'em
                                        foreach (string ux in aliases_array)        // remember that we have processed 'em
                                            remember.Add(ux);

                                        // tell the user what we are deleting (all aliases are deleted with the keyword)
                                        from.SendMessage(string.Format("The following keywords have been deleted: {0}", aliases));

                                        // now remove each keyword and alias
                                        foreach (string dx in aliases_array)
                                            m_console.KeywordDatabase.Remove(dx);
                                    }
                                }
                            }
                            else
                                from.SendMessage(string.Format("Usage: '{0}' keyword <keyword1>[, <keyword2>, <keyword3>].", tokens[0] as string));
                        }
                    }

                    if (understood == false)
                        from.SendMessage(string.Format("I'm sorry. I do not understand."));
                }
                #endregion

                #region anyone talking - process keywords
                if (e.Handled == false)
                {
                    string match;
                    if (FindKeyPhrase(tokens, 0, out match) || FindKeyword(tokens, 0, out match) && m_console.KeywordDatabase[match].Length > 0)
                    {
                        e.Handled = true;
                        // execute the verb for this keyword

                        // do not allow standard players to access internal commands like 'OnEnter'
                        //	When e.Internal is true, it's the NPC dispatching the keyword and will be allowed
                        string akw;
                        if (AdminKeyword(tokens, 0, out akw) && !(IsOwner(from) || bIsHouseOwner || e.Internal))
                            from.SendMessage(string.Format("I'm sorry. You do not have access the {0} command.", match));
                        else
                        {   // begin execute 
                            object[][] actions = SplitArray(m_console.KeywordDatabase[match], '|');
                            int depth = 0;
                            try { ExecuteActions(from, actions, ref depth); }
                            catch
                            {
                                from.SendMessage(string.Format("Excessive recursion detected for keyword ({0}).", match));
                            }
                        }
                        // end execute
                    }
                    // else we don't recognize what was said, so we will simply ignore the player.
                }
                #endregion

                // lastly, call the underlying region to process usual stuffs
                base.OnSpeech(e);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }
        #endregion OnSpeech

        #region UTILS

        private object[] RemoveHandle(object[] tokens, string handle)
        {
            if (handle.ToLower() == (tokens[0] as string).ToLower())
            {
                List<object> list = new List<object>();
                for (int ix = 1; ix < tokens.Length; ix++)
                    list.Add(tokens[ix]);

                return list.ToArray();
            }

            return tokens;
        }

        protected string DirectionMacros(string text)
        {
            switch (text.ToLower())
            {
                case "northeast":
                case "ne":
                    return "right";
                case "east":
                case "e":
                    return "east";
                case "southeast":
                case "se":
                    return "down";
                case "south":
                case "s":
                    return "south";
                case "southwest":
                case "sw":
                    return "left";
                case "west":
                case "w":
                    return "west";
                case "northwest":
                case "nw":
                    return "up";
                case "north":
                case "n":
                    return "north";
                default:
                    return text;
            }
        }

        object[] TokenList(object[] list, int offset)
        {
            object[] out_array = new object[list.Length - offset];
            Array.Copy(list, offset, out_array, 0, out_array.Length);
            return out_array;
        }

        string[] TokenList(object[] list)
        {
            List<string> strlist = new List<string>();
            for (int ix = 0; ix < list.Length; ix++)
                if (list[ix] is string)
                    strlist.Add(list[ix] as string);
            return strlist.ToArray();
        }

        string[] KeywordList(string[] list)
        {
            List<string> strlist = new List<string>();
            for (int ix = 0; ix < list.Length; ix++)
                if (m_console.KeywordDatabase.ContainsKey(list[ix]))
                    strlist.Add(list[ix] as string);
            return strlist.ToArray();
        }

        protected void ExecuteActions(Mobile from, object[][] actions, ref int depth)
        {
            if (depth++ > 8)
            {   // prevent user-defined recursive patterns that would otherwise crash the server
                throw new ApplicationException("Excessive recursion detected");
            }

            // execute all actions.
            //	certain failures like syntax will abort the execution. other minor failures will allow us to continue.
            for (int ix = 0; ix < actions.Length; ix++)
            {
                object[] action = actions[ix];
                bool done = false;
                if (done == false)
                {
                    switch ((action[0] as string).ToLower())
                    {
                        // see if the user already has thie thing, and if so, branch
                        case "has":
                            { // zones don't have GIVE, so I don't think HAS makes sense.
                            }
                            break;

                        case "random":
                            {
                                string[] keys = KeywordList(TokenList(TokenList(action, 1)));
                                if (keys.Length > 0)
                                {
                                    actions[ix] = m_console.KeywordDatabase[keys[Utility.Random(keys.Length)]];
                                    ix--;
                                    continue;
                                }
                            }
                            break;

                        // foreach item [in direction] do
                        case "foreach":
                            {
                                // extract the direction to look if any
                                Direction dir = Direction.Down;
                                bool have_direction = false;
                                if (action.Length >= 3)
                                {
                                    // replace things like southeast with 'down' which is the UO enum value
                                    string test = DirectionMacros(action[1] as string);
                                    foreach (string sx in Enum.GetNames(typeof(Direction)))
                                    {
                                        if (test == sx.ToLower())
                                        {
                                            have_direction = true;
                                            dir = (Direction)Enum.Parse(typeof(Direction), test, true);
                                            break;
                                        }
                                    }
                                }

                                // check to see that we have enough arguments
                                if (action.Length < 2 || (action.Length < 3 && have_direction))
                                {
                                    from.SendMessage(string.Format("Usage: foreach [direction] verb <arguments>."));
                                    // no more actions if we fail with a syntax error
                                    done = true;
                                    break;
                                }

                                // build a new action by removing the setup parameters foreach & [direction]
                                List<object> list = new List<object>();
                                for (int jx = 0; jx < action.Length; jx++)
                                {
                                    object node = action[jx];
                                    if (jx == 0 && node as string == "foreach")
                                        continue;
                                    if (jx == 1 && have_direction)
                                        continue;

                                    list.Add(node);
                                }

                                // for each item, and if we have a direction: each item in that direction from us
                                foreach (KeyValuePair<Item, object[]> kvp in m_console.ItemDatabase)
                                {
                                    // if a direction was specified, only list items in that direction
                                    if (have_direction && from.GetDirectionTo(kvp.Key.Location) != dir)
                                        continue;

                                    // make a temp copy we can modify
                                    object[] temp = list.ToArray();

                                    // expand macros
                                    for (int ux = 0; ux < temp.Length; ux++)
                                        if (temp[ux] is string && (temp[ux] as string).Contains("%item%"))
                                            temp[ux] = GetField(kvp.Value, Field.Name);

                                    // build the new macro-expanded action and execute
                                    List<object[]> table = new List<object[]>();
                                    table.Add(temp);
                                    ExecuteActions(from, table.ToArray(), ref depth);
                                }
                            }
                            break;
                        /*case "keep":
							// we have no NPC that will 'keep'
							m_disposition = Disposition.Keep;
							break;
						case "return":
							// we have no NPC that will 'return'
							m_disposition = Disposition.Return;
							break;
						case "delete":
							// we have no NPC that will 'delete'
							m_disposition = Disposition.Delete;
							break;*/
                        case "emote":
                            // we have no NPC to 'emote'
                            //Emote(MakeString(action, 1));
                            from.SendMessage(MakeString(action, 1));
                            break;
                        case "sayto":
                            // we have no NPC that will 'sayTo'
                            //SayTo(m_From, MakeString(action, 1));
                            from.SendMessage(MakeString(action, 1));
                            break;
                        case "say":
                            // we have no NPC that will 'say'
                            //Say(MakeString(action, 1));
                            from.SendMessage(MakeString(action, 1));
                            break;
                        case "sayover":
                            {
                                // check to see that we have enough arguments
                                if (action.Length < 3)
                                {
                                    from.SendMessage(string.Format("Usage: SayOver <item name> <arguments>."));
                                    // no more actions if we fail with a syntax error
                                    done = true;
                                    break;
                                }

                                // okay, remove the second paramater which is the name of the item, then say the string
                                Item item = Lookup(action[1] as string);
                                if (item != null)
                                    item.PublicOverheadMessage(0, 0x3B2, false, MakeString(action, 2));
                                else
                                    from.SendMessage(string.Format("I know nothing about a {0}.", action[1]));
                            }
                            break;
                        case "give":
                            {
                                bool known = false;
                                bool given = false;
                                foreach (KeyValuePair<Item, object[]> kvp in m_console.ItemDatabase)
                                {
                                    string name = GetField(kvp.Value, Field.Name) as string;
                                    if (name == action[1] as string)
                                    {   // I had one at one time
                                        known = true;
                                        /*if (kvp.Key != null && kvp.Key.RootParent == this)
										{	// I have one now
											if (Backpack != null && from.Backpack != null)
											{
												Backpack.RemoveItem(kvp.Key);
												if (!from.Backpack.TryDropItem(from, kvp.Key, false))
												{
													this.SayTo(from, 503204);						// You do not have room in your backpack for this.
													kvp.Key.MoveToWorld(from.Location, from.Map);
												}
												given = true;
												break;
											}
										}*/
                                    }
                                }

                                if (given == false)
                                {
                                    if (known == false)
                                        from.SendMessage(string.Format("I know nothing about a {0}.", action[1]));
                                    else
                                        from.SendMessage(string.Format("I'm sorry. I no longer have a {0}.", action[1]));

                                    // no more actions if we fail the give
                                    done = true;
                                    break;
                                }

                            }
                            break;
                        default:
                            from.SendMessage(string.Format("{0} is an unknown verb.", action[0]));
                            break;
                    }
                }
            }

            return;
        }

        protected object[][] SplitArray(object[] tokens, Char splitChar)
        {
            List<object[]> list = new List<object[]>();
            List<object> objects = new List<object>();
            foreach (object o in tokens)
            {
                if (o is Char && (Char)o == splitChar)
                {
                    list.Add(objects.ToArray());
                    objects.Clear();
                    continue;
                }

                objects.Add(o);
            }

            if (objects.Count > 0)
                list.Add(objects.ToArray());

            return list.ToArray();
        }

        protected string MakeString(object[] tokens, int offset)
        {
            string temp = "";
            ExpansionStatus result;
            string match = null;
            if ((result = ExpandMacros(ref tokens, offset, ref match)) == ExpansionStatus.Okay)
                for (int ix = 0; ix < tokens.Length; ix++)
                {
                    if (temp.Length > 0 && tokens[ix] is string)
                        temp += ' ';

                    if (tokens[ix] is string)
                        temp += tokens[ix] as string;
                    else
                        temp += (Char)tokens[ix];       // add in punction like a comma (which was turned to a Char for parsing reasons
                                                        // it's now returned
                }
            else
            {
                if (result == ExpansionStatus.Unknown)                              // I never had one of these
                    temp = string.Format("I know nothing about a {0}.", match);
                else if (result == ExpansionStatus.HaveAll)                         // i've not given any of these out
                    temp = string.Format("The {0} is in my backpack.", match);
                else if (result == ExpansionStatus.BadField)                        // i have the item, but I don't know the field
                    temp = string.Format("Bad field used for: {0}.", match);
                else
                    temp = string.Format("I'm sorry. I'm at a loss looking for a {0}.", match);
            }


            return temp;
        }

        private string GetItemLocation(Item item)
        {
            Point3D px = item.GetWorldLocation();
            Map map = item.Map;
            return GetLocation(px, map);
        }

        private string GetLocation(Point3D px, Map map)
        {
            string location;
            int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
            bool xEast = false, ySouth = false;

            bool valid = Server.Items.Sextant.Format(px, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = Server.Items.Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
            else
                location = "????";

            if (!valid)
                location = string.Format("{0} {1}", px.X, px.Y);

            if (map != null)
            {
                Region reg = Region.Find(px, map);

                if (reg != map.DefaultRegion && reg.Name != null && reg.Name.Length > 0)
                {
                    location += (" in " + reg);
                }
            }

            return location;
        }

        protected enum ExpansionStatus
        {
            Okay,               // okay
            Unknown,            // I never knew of this item
            NoMore,             // I have no more
            HaveAll,            // I have all that there are
            Deleted,            // deleted or freeze dried
            BadField,           // I know the item, but there was a problem with the field
        }

        protected ExpansionStatus ExpandMacros(ref object[] tokens, int offset, ref string match)
        {
            List<object> list = new List<object>();
            for (int ix = offset; ix < tokens.Length; ix++)
            {
                string temp = tokens[ix] as string;

                if (temp == null)
                {   // a delimiter like '|'
                    list.Add(tokens[ix]);
                    continue;
                }

                temp = temp.Replace("%%", "\r");                                                // escape double '%' as a literal
                int name_start = temp.IndexOf('%');                                             // locate start of name
                int name_end = temp.LastIndexOf('%');                                           // locate end of name
                int field_start = temp.IndexOf('.');                                            // locate start of field
                int tail = temp.LastIndexOfAny(new char[] { '.', ',', '!', '?' });              // any tail delimiters?
                int field_end = (field_start == tail || tail < 0) ? temp.Length - 1 : tail - 1; //	end of field

                // %sword%.location
                if (name_start < name_end && field_start < field_end && name_start >= 0 && field_start >= 0)
                {   // okay, looks like a macro
                    // extract the name(sword) and field(location)
                    string name = temp.Substring(name_start + 1, (name_end - name_start) - 1);      // extract the name
                    string field = temp.Substring(field_start + 1, (field_end - field_start));      // extract the field
                    temp = temp.Replace(string.Format("%{0}%.{1}", name, field), "{0}");            // format string preserveing head and tail characters
                    temp = temp.Replace("\r", "%");                                                 // unescape literal '%'

                    // tell the user what we think we are dealing with
                    match = name;

                    /*if (name == "npc")
					{
						if (field == "location")
						{
							list.Add(string.Format(temp, GetLocation(this.Location, this.Map)));
						}
						else if (field == "name")
						{
							list.Add(string.Format(temp, this.Name));
						}
						else
							return ExpansionStatus.BadField;
					}
					else */
                    if (name == "pc")
                    {
                        if (field == "location")
                        {
                            list.Add(string.Format(temp, GetLocation(m_From.Location, m_From.Map)));
                        }
                        else if (field == "name")
                        {
                            list.Add(string.Format(temp, m_From.Name));
                        }
                        else
                            return ExpansionStatus.BadField;
                    }
                    else
                    {
                        // did we ever know of this item?
                        if (Lookup(name) == null)
                            return ExpansionStatus.Unknown;

                        // okay, we know of this item. See if we have given any out
                        Item item;

                        // look for an entry in our database that does not exist in our inventory
                        if ((item = Lookup(name, false)) == null)
                            // we've not given any out
                            return ExpansionStatus.HaveAll;

                        if (field == "location")
                        {
                            list.Add(string.Format(temp, GetItemLocation(item)));

                            // warn the player that they are being tracked
                            if (item.RootParent is PlayerMobile)
                            {
                                PlayerMobile pm = item.RootParent as PlayerMobile;
                                if (pm.Map != Map.Internal)
                                {
                                    string realName = item.Name;
                                    if (item.Name == null || item.Name.Length == 0)
                                        realName = item.ItemData.Name;

                                    pm.SendMessage("The {0} ({1}) you carry is being used to track you!", name, realName);
                                }
                            }
                        }
                        else
                            return ExpansionStatus.BadField;
                    }
                }
                else
                    list.Add(tokens[ix]);
            }

            tokens = list.ToArray();
            return ExpansionStatus.Okay;
        }

        public bool IsOwner(Mobile m)
        {
            return (m == m_console.Owner || m.AccessLevel >= AccessLevel.GameMaster);
        }

        protected bool ComputeKeywords(object[] tokens, int offset, bool defined, out string good, out string bad)
        {
            good = "";
            bad = "";
            string[] good_array;
            string[] bad_array;
            bool result = ComputeKeywords(tokens, offset, defined, out good_array, out bad_array);

            foreach (string gx in good_array)
            {
                if (good.Length > 0)
                    good += ", ";
                good += gx;
            }

            foreach (string bx in bad_array)
            {
                if (bad.Length > 0)
                    bad += ", ";
                bad += bx;
            }

            // true if all good (nothing bad)
            return result;
        }

        protected bool ComputeKeywords(object[] tokens, int offset, bool defined, out string[] good, out string[] bad)
        {
            good = new string[0];
            bad = new string[0];

            List<string> good_list = new List<string>();
            List<string> bad_list = new List<string>();

            if (tokens.Length > offset)
            {
                for (int ix = offset; ix < tokens.Length; ix++)
                {
                    // check for end of keywords
                    if (tokens[ix] is Char && (Char)tokens[ix] == '|')
                        break;

                    // probably a comma
                    if (tokens[ix] is string == false)
                        continue;

                    // do we know about this keyword?
                    if (m_console.KeywordDatabase.ContainsKey(tokens[ix] as string))
                    {   // the key word is known
                        if (defined == true)
                            good_list.Add(tokens[ix] as string);    // known and should be known
                        else
                            bad_list.Add(tokens[ix] as string);     // known and should not be known
                    }
                    else
                    {
                        if (defined == true)
                            bad_list.Add(tokens[ix] as string);     // not known and should be known
                        else
                            good_list.Add(tokens[ix] as string);    // not known and should not be known
                    }
                }
            }

            good = good_list.ToArray();
            bad = bad_list.ToArray();

            // true if all good (nothing bad)
            return !(bad.Length > 0);
        }

        protected object[] Compile(string input)
        {
            // compile the string into an array of objects
            List<object> list = new List<object>();
            string current = "";
            foreach (Char ch in input)
            {
                switch (ch)
                {
                    case '|':
                    case ',':
                    case ' ':
                        // add the current string.
                        if (current.Length > 0)
                        {
                            list.Add(current);
                            current = "";
                        }

                        // ignore white
                        if (ch == ' ')
                            continue;

                        // add the delimiter
                        list.Add(ch);
                        continue;

                    default:
                        current += ch;
                        continue;
                }
            }

            // add the tail string
            if (current.Length > 0)
            {
                list.Add(current);
                current = "";
            }

            return list.ToArray();
        }

        private object GetField(object[] tokens, Field field)
        {
            for (int ix = 0; ix < tokens.Length; ix++)
            {
                if (tokens[ix] is Field && (Field)tokens[ix] == field)
                {
                    switch (field)
                    {
                        case Field.Name:
                            int name_index = ix + 1;
                            if (name_index <= tokens.Length && tokens[name_index] is string)
                                return tokens[name_index];
                            else
                                return null;
                        //break;
                        case Field.Track:
                            return tokens[ix];
                            //break;
                    }
                }
            }

            return null;
        }

        private void OnItemGiven(Mobile from, Item item)
        {   // see if it already has a name
            if (m_console.ItemDatabase.ContainsKey(item) && GetField(m_console.ItemDatabase[item], Field.Name) != null)
            {
                from.SendMessage("Okay.");
                return;
            }
            from.Prompt = new LabelItemPrompt(this, item);
            from.SendMessage("What would you like to name this item?");
        }

        private bool CheckVerb(string verb)
        {
            switch (verb)
            {
                case "has":
                case "random":
                case "foreach":
                case "keep":
                case "return":
                case "delete":
                case "emote":
                case "sayto":
                case "say":
                case "sayover":
                case "give":
                    return true;
                default:
                    return false;
            }
        }

        public void LabelItem(Mobile from, Item item, string text)
        {
            if (m_console.ItemDatabase.Count >= 256)
            {   // hard stop
                from.SendMessage(string.Format("Your label database is full."));
                return;
            }

            // does it already have a label?
            if (m_console.ItemDatabase.ContainsKey(item) && GetField(m_console.ItemDatabase[item], Field.Name) != null)
                return;

            if (text == null || text.Length == 0)
            {
                if (item.Name != null && item.Name.Length > 0)
                    m_console.ItemDatabase[item] = AppendField(m_console.ItemDatabase.ContainsKey(item) ? m_console.ItemDatabase[item] : null, Field.Name, item.Name);
                else if (m_console.ItemDatabase.ContainsKey(item) && GetField(m_console.ItemDatabase[item], Field.Name) != null)
                    m_console.ItemDatabase[item] = AppendField(m_console.ItemDatabase.ContainsKey(item) ? m_console.ItemDatabase[item] : null, Field.Name, GetField(m_console.ItemDatabase[item], Field.Name));
                else
                    m_console.ItemDatabase[item] = AppendField(m_console.ItemDatabase.ContainsKey(item) ? m_console.ItemDatabase[item] : null, Field.Name, item.ItemData.Name);
            }
            else
                m_console.ItemDatabase[item] = AppendField(m_console.ItemDatabase.ContainsKey(item) ? m_console.ItemDatabase[item] : null, Field.Name, text);

            from.SendMessage(string.Format("Okay."));
        }

        // build a string suitable for display
        protected bool FindKeywordAliases(string keyword, out string found)
        {
            found = "";
            string[] found_array;

            bool result = FindKeywordAliases(keyword, out found_array);

            if (result)
                foreach (string sx in found_array)
                {
                    if (found.Length > 0)
                        found += ", ";
                    found += sx;
                }

            // we found keyword + N aliases
            return result;
        }

        public bool FindKeywordAliases(string keyword, out string[] found)
        {
            found = new string[0];

            if (m_console.KeywordDatabase.ContainsKey(keyword) == false)
                return false;

            List<string> list = new List<string>();
            list.Add(keyword);
            object[] actions = m_console.KeywordDatabase[keyword];

            foreach (KeyValuePair<string, object[]> kvp in m_console.KeywordDatabase)
                if (kvp.Key != keyword && kvp.Value == actions)
                    list.Add(kvp.Key);

            found = list.ToArray();

            // we found keyword + N aliases
            return found.Length > 0;
        }

        protected bool FindKeyPhrase(object[] tokens, int offset, out string found)
        {
            found = null;

            // check each keyword entry for a special dotted keyword phrase
            foreach (KeyValuePair<string, object[]> kvp in m_console.KeywordDatabase)
            {   // does it even look like a key phrase?
                if (kvp.Key.IndexOf('.') != -1)
                {   // we may have something
                    string[] temp = kvp.Key.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                    if (temp.Length <= 1)
                        continue;           // must be at least two keywords
                    for (int ix = 0; ix < temp.Length; ix++)
                    {
                        if (MatchPhraseKey(tokens, offset, temp[ix]))
                        {
                            if (ix == temp.Length - 1)
                            {
                                found = kvp.Key;    // the match
                                return true;        // we have matched all terms of this key phrase
                            }
                        }
                        else
                            break;          // oops, no match
                    }
                }
            }

            // not found
            return false;
        }

        protected bool MatchPhraseKey(object[] tokens, int offset, string match)
        {
            if (tokens.Length > offset && tokens[offset] is string)
            {
                for (int ix = offset; ix < tokens.Length; ix++)
                {
                    if (tokens[ix] is string)
                    {
                        // remove punctuation from token as "hello!" should match the keyword "hello"
                        string kword = tokens[ix] as string;
                        int ndx;
                        char[] delims = new char[] { '.', ',', '!' };
                        while ((ndx = kword.IndexOfAny(delims)) != -1)
                            kword = kword.Remove(ndx, 1);

                        // okay, we have a clean keyword, look it up
                        if (kword == match)
                            return true;
                    }
                    if (tokens[ix] is Char && (Char)tokens[ix] == '|')
                    {   // we're done .. this ('|') starts the action
                        return false;
                    }
                }
            }

            // not found
            return false;
        }

        protected bool FindKeyword(object[] tokens, int offset, out string found)
        {
            found = null;
            if (tokens.Length > offset && tokens[offset] is string)
            {
                for (int ix = offset; ix < tokens.Length; ix++)
                {
                    if (tokens[ix] is string)
                    {
                        // remove punctuation from token as "hello!" should match the keyword "hello"
                        string kword = tokens[ix] as string;
                        int ndx;
                        char[] delims = new char[] { '.', ',', '!' };
                        while ((ndx = kword.IndexOfAny(delims)) != -1)
                            kword = kword.Remove(ndx, 1);

                        // okay, we have a clean keyword, look it up
                        if (m_console.KeywordDatabase.ContainsKey(kword))
                        {
                            found = kword;
                            return true;
                        }
                    }
                    if (tokens[ix] is Char && (Char)tokens[ix] == '|')
                    {   // we're done .. this ('|') starts the action
                        return false;
                    }
                }
            }

            // not found
            return false;
        }

        private object[] AppendField(object[] tokens, Field field, object value)
        {
            List<object> list = new List<object>();

            int skip = 0;
            switch (field)
            {   // skilling fields is how to update an existing field. I.e., we never copy over the old field
                case Field.Name: skip = 1; break;
                case Field.Track: skip = 0; break;
            }

            if (tokens != null)
                for (int ix = 0; ix < tokens.Length; ix++)
                {
                    if (tokens[ix] is Field && (Field)tokens[ix] == field)
                    {
                        ix += skip;
                        continue;
                    }
                    list.Add(tokens[ix]);
                }

            list.Add(field);
            if (value != null)
                list.Add(value);

            return list.ToArray();
        }

        private Item Lookup(string name, bool exists)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_console.ItemDatabase)
                if (GetField(kvp.Value, Field.Track) != null && GetField(kvp.Value, Field.Name) as string == name)
                {   // deleted if freeze dried.
                    if (kvp.Key.Deleted)
                        continue;

                    // return it if it's the state (exists) we want
                    if (((Region)kvp.Key.RootParent == this) == exists)
                        return kvp.Key;
                }

            return null;
        }

        private Item Lookup(string name)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_console.ItemDatabase)
                if (GetField(kvp.Value, Field.Track) != null && GetField(kvp.Value, Field.Name) as string == name)
                    return kvp.Key;

            return null;
        }

        protected bool AdminKeyword(object[] tokens, int offset, out string found)
        {
            found = "";
            if (tokens.Length > offset && tokens[offset] is string)
            {
                // look at our current set of keywords skipping to offest (ignore 'keywords' etc.)
                for (int mx = offset; mx < tokens.Length; mx++)
                {
                    string s = tokens[mx] as string;
                    if (s == "onenter" || s == "onreceive")
                    {
                        // found it
                        found = s;
                        return true;
                    }
                }
            }

            // null unit
            return false;
        }

        public bool TrackItem(Mobile from, Item item)
        {
            if (m_console.ItemDatabase.Count >= 256)
            {   // hard stop
                from.SendMessage(string.Format("Your label database is full."));
                return false;
            }

            if (m_console.ItemDatabase.ContainsKey(item) && GetField(m_console.ItemDatabase[item], Field.Track) != null)
            {   // we're already tracking this item
                from.SendMessage(string.Format("I am already tracking this item."));
                return true;
            }

            // add the 'tracking' field
            m_console.ItemDatabase[item] = AppendField(m_console.ItemDatabase.ContainsKey(item) ? m_console.ItemDatabase[item] : null, Field.Track, null);

            if (GetField(m_console.ItemDatabase[item], Field.Name) == null)
                OnItemGiven(from, item);
            else
                from.SendMessage(string.Format("Okay."));
            return true;
        }

        public class LabelItemPrompt : Prompt
        {
            private SensoryRegion m_SensoryRegion;
            private Item m_item;

            public LabelItemPrompt(SensoryRegion sensoryRegion, Item item)
            {
                m_SensoryRegion = sensoryRegion;
                m_item = item;
            }

            public override void OnCancel(Mobile from)
            {
                OnResponse(from, "");
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 50)
                    text = text.Substring(0, 50);

                m_SensoryRegion.LabelItem(from, m_item, text);
            }
        }

        public class OnReceiveTarget : Target
        {
            private SensoryRegion m_SensoryRegion;

            public OnReceiveTarget(SensoryRegion sensoryRegion)
                : base(15, false, TargetFlags.None)
            {
                m_SensoryRegion = sensoryRegion;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is Item)
                    m_SensoryRegion.TrackItem(from, targ as Item);
                else
                    from.SendMessage(string.Format("That is not a valid item."));
                return;
            }
        }
        #endregion UTILS
    }
}

namespace Server.Items
{
    [Server.Items.FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class SensoryConsole : Item
    {
        private SensoryRegion m_region;
        private Map m_map;
        private Mobile m_owner;
        // label database
        private Dictionary<Item, object[]> m_ItemDatabase = new Dictionary<Item, object[]>();
        public Dictionary<Item, object[]> ItemDatabase { get { return m_ItemDatabase; } }
        private Dictionary<string, object[]> m_KeywordDatabase = new Dictionary<string, object[]>();
        public Dictionary<string, object[]> KeywordDatabase { get { return m_KeywordDatabase; } }
        private double m_memory = 30;                       // how long we remember players in minutes default: 30 minutes
        private Memory m_PlayerMemory = new Memory();       // memory used to remember is a saw a player in the area
        public Memory PlayerMemory { get { return m_PlayerMemory; } set { m_PlayerMemory = value; } }
        public enum Error { None, SpanRegion, Unknown };
        private Error m_error = Error.None;

        [Constructable]
        public SensoryConsole()
            : base(0x1F14)
        {
            Name = "Sensory Console";
            Weight = 1.0;
            Hue = 2101;

            // initialize the empty region
            InitRegion();
        }

        public SensoryConsole(Serial s)
            : base(s)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (Region.Regions.Contains(m_region))
            {
                LabelTo(from, "{0}", Rectangle);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            // Players must use a the deed form to place a console
            if (from.AccessLevel < AccessLevel.GameMaster)
            {   // players cannot even see this console, but in the off chance a GM has left one lying around...
                from.SendMessage("You do not have access to this command.");
            }
            else
            {
                Region.RemoveRegion(m_region);
                from.SendMessage("Removing existing sensory zone.");

                from.Target = new OnPlacementTarget(this, true);
                from.SendMessage("Target the upper left corner of the new zone.");
            }
        }

        public override void OnDelete()
        {
            if (m_region != null)
                Region.RemoveRegion(m_region);

            base.OnDelete();
        }

        #region CommandProperty

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner { get { return m_owner; } set { m_owner = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map RegionMap
        {
            get { return m_map; }
            set { m_map = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Error PlacementError
        {
            get { return m_error; }
            set { m_error = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Memory
        {
            get { return TimeSpan.FromMinutes(m_memory); }
            set { m_memory = value.TotalMinutes; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle3D Rectangle
        {
            get
            {
                return m_region.Coords.Count > 0 ? m_region.Coords[0] : new Rectangle3D();
            }
            set
            {
                m_region.Coords.Clear();
                m_region.Coords.Add(value);
                UpdateRegion();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D Begin
        {
            get
            {
                return this.Rectangle.Start;
            }
            set
            {
                this.Rectangle = new Rectangle3D(value, this.End);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D End
        {
            get
            {
                return this.Rectangle.End;
            }
            set
            {
                this.Rectangle = new Rectangle3D(this.Begin, value);
            }
        }

        #endregion CommandProperty

        private Rectangle2D FixRect(Rectangle2D inr)
        {
            Point2D start = inr.Start;
            Point2D end = inr.End;
            // normalize bounding rect
            if (start.X > end.X)
            {
                int x = start.X;
                start.X = end.X;
                end.X = x;
            }
            if (start.Y > end.Y)
            {
                int y = start.Y;
                start.Y = end.Y;
                end.Y = y;
            }

            return new Rectangle2D(start, end);
        }

        public void InitRegion()
        {
            // region setup
            m_region = new SensoryRegion(this, Map.Felucca);
            m_region.Name = "";                                 // keep this nil elsewise we get enter/exit mesages
            m_region.Priority = Region.HighestPriority;
            m_region.Map = Map.Felucca;

            UpdateRegion();
        }

        private void UpdateRegion()
        {
            // remove the region
            Region.RemoveRegion(m_region);

            // clear the error
            m_error = Error.None;

            // add the region back, but only if both Start and End points are in the same region
            if (m_region.Coords.Count != 0)
            {
                Rectangle3D zone = m_region.Coords[0];
                Region begin = Region.Find(zone.Start, base.Map, m_region);
                Region end = Region.Find(zone.End, base.Map, m_region);
                if (begin == end)
                    Region.AddRegion(m_region);
                else
                    m_error = Error.SpanRegion;
            }
        }

        private enum TokenTypes
        {
            String,
            Char,
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(5); // version

            // version 5
            // write coords as Rectangle3D

            // version 4
            // write the Alias database
            List<string[]> AliasDatabase = new List<string[]>();
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
            {
                string[] found = new string[0];
                if (m_region.FindKeywordAliases(kvp.Key, out found))
                {   // we sort it so that all alias look alike
                    Array.Sort(found);
                    // does the database already contain this alias list?
                    bool Contains = false;
                    for (int ty = 0; ty < AliasDatabase.Count; ty++)
                    {
                        if (AliasDatabase[ty].Length == found.Length)
                        {
                            for (int uo = 0; uo < AliasDatabase[ty].Length; uo++)
                                if (AliasDatabase[ty][uo] == found[uo])
                                {   // if the last one matches, we have a matching set
                                    if (uo + 1 == AliasDatabase[ty].Length)
                                        Contains = true;
                                }
                                else
                                    break;
                        }
                    }
                    if (Contains == false)
                        AliasDatabase.Add(found);
                }
            }

            // number of aliased actions
            writer.Write(AliasDatabase.Count);
            for (int ii = 0; ii < AliasDatabase.Count; ii++)
            {
                // number of aliases    
                writer.Write(AliasDatabase[ii].Length);
                for (int oo = 0; oo < AliasDatabase[ii].Length; oo++)
                {   // write the aliases
                    writer.Write(AliasDatabase[ii][oo]);
                }
            }

            // version 3
            writer.Write(m_owner);

            // version 2
            writer.Write(m_memory);

            // version 1
            #region SPEECH COMMAND SYSTEM
            writer.Write(m_KeywordDatabase.Count);
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                foreach (object o in kvp.Value)
                {
                    if (o is string)
                    {
                        writer.Write((int)TokenTypes.String);
                        writer.Write(o as string);
                    }
                    else if (o is Char)
                    {
                        writer.Write((int)TokenTypes.Char);
                        writer.Write((char)o);
                    }
                    else
                        Console.WriteLine("Error: Trying to write unknown type in Quest Giver: {0}", o);
                }
            }

            writer.Write(m_ItemDatabase.Count);
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                foreach (object o in kvp.Value)
                {
                    if (o is SensoryRegion.Field)
                    {
                        writer.Write((int)((SensoryRegion.Field)o));
                    }
                    else if (o is string)
                    {
                        writer.Write(o as string);
                    }
                    else
                        Console.WriteLine("Error: Trying to write unknown type in Quest Giver: {0}", o);
                }
            }
            #endregion

            // version 0
            writer.Write(m_region.Coords.Count);
            for (int ix = 0; ix < m_region.Coords.Count; ix++)
                writer.Write((Rectangle3D)m_region.Coords[ix]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            // initialize the empty region
            InitRegion();

            // introduced in version 2 to fixup the aliases post-load
            List<string[]> AliasDatabase = new List<string[]>();

            switch (version)
            {
                case 5:
                case 4:
                    {
                        // read the alias database
                        int ad_size = reader.ReadInt();
                        for (int ii = 0; ii < ad_size; ii++)
                        {   // number of aliases in this array
                            int netls = reader.ReadInt();
                            List<string> temp = new List<string>();
                            for (int uu = 0; uu < netls; uu++)
                                temp.Add(reader.ReadString());
                            AliasDatabase.Add(temp.ToArray());
                        }
                        goto case 3;
                    }
                case 3:
                    {
                        m_owner = reader.ReadMobile();
                        goto case 2;
                    }
                case 2:
                    {
                        m_memory = reader.ReadDouble();
                        goto case 1;
                    }
                case 1:
                    {
                        #region SPEECH COMMAND SYSTEM
                        // read the keyword database
                        int kwdb_count = reader.ReadInt();
                        for (int ix = 0; ix < kwdb_count; ix++)
                        {
                            string key = reader.ReadString();
                            int obj_count = reader.ReadInt();
                            List<object> list = new List<object>();
                            for (int jx = 0; jx < obj_count; jx++)
                            {
                                switch ((TokenTypes)reader.ReadInt())
                                {
                                    case TokenTypes.Char:
                                        list.Add((Char)reader.ReadChar());
                                        continue;

                                    case TokenTypes.String:
                                        list.Add(reader.ReadString());
                                        continue;
                                }
                            }
                            m_KeywordDatabase.Add(key, list.ToArray());
                        }

                        // read the item database
                        int idb_count = reader.ReadInt();
                        for (int ix = 0; ix < idb_count; ix++)
                        {
                            Item key = reader.ReadItem();
                            int obj_count = reader.ReadInt();
                            List<object> list = new List<object>();
                            for (int jx = 0; jx < obj_count; jx++)
                            {
                                SensoryRegion.Field field = (SensoryRegion.Field)reader.ReadInt();
                                list.Add(field);
                                switch (field)
                                {
                                    case SensoryRegion.Field.Track:
                                        continue;

                                    case SensoryRegion.Field.Name:
                                        list.Add(reader.ReadString());
                                        jx++;
                                        continue;
                                }
                            }

                            if (key != null)
                                m_ItemDatabase.Add(key, list.ToArray());
                            else
                            {   // the key has been deleted.
                                // would should probably add 'status' strings to the SZ so that he can tell
                                //	the owner what happened. For now just delete the item, i.e., don't add it.
                            }
                        }
                        goto case 0;
                        #endregion
                    }
                case 0:
                    {
                        int rcount = reader.ReadInt();
                        for (int ix = 0; ix < rcount; ix++)
                        {
                            Rectangle3D rx;
                            if (version >= 5)
                                rx = reader.ReadRect3D();
                            else
                                rx = Region.ConvertTo3D(reader.ReadRect2D());
                            m_region.Coords.Add(rx);
                        }
                        goto default;
                    }

                default:
                    UpdateRegion();
                    break;
            }

            // okay, patch the keyword database aliases
            if (AliasDatabase.Count > 0)
            {
                for (int gg = 0; gg < AliasDatabase.Count; gg++)
                {
                    // grab the shared action from the first key
                    object[] shared_action = m_KeywordDatabase[AliasDatabase[gg][0]];
                    for (int yy = 1; yy < AliasDatabase[gg].Length; yy++)
                    {   // patch 'em!
                        m_KeywordDatabase[AliasDatabase[gg][yy]] = shared_action;
                    }
                }
            }
        }

        // GM placed Console, not auto deleted with house.
        public class OnPlacementTarget : Target
        {
            private SensoryConsole m_SensoryConsole;
            bool m_begin;

            public OnPlacementTarget(SensoryConsole sensoryConsole, bool begin)
                : base(15, false, TargetFlags.None)
            {
                m_SensoryConsole = sensoryConsole;
                m_begin = begin;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                Multis.BaseHouse house = Multis.BaseHouse.FindHouseAt(from);

                if (m_begin)    // first point
                {
                    m_SensoryConsole.Begin = new Point3D(targ as IPoint3D);
                    from.Target = new OnPlacementTarget(m_SensoryConsole, false);
                    from.SendMessage("Target the lower right corner of the new zone.");
                }
                else            // second point
                {
                    m_SensoryConsole.End = new Point3D(targ as IPoint3D);
                    if (m_SensoryConsole.PlacementError != Error.None)
                        from.SendMessage("Your sensory zone may not span multiple regions.");
                    else
                    {
                        from.SendMessage("Zone created.");
                        m_SensoryConsole.Owner = from;
                    }
                }
                return;
            }
        }
    }
}

namespace Server.Items
{
    public class SensoryZoneDeed : Item
    {
        [Constructable]
        public SensoryZoneDeed()
            : base(0x14F0)
        {
            Name = "a sensory zone deed";
            Weight = 1.0;
            LootType = LootType.Regular;
        }

        public SensoryZoneDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); //version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                SensoryConsole v = new SensoryConsole();
                from.Target = new OnPlacementTarget(v, this, true);
                from.SendMessage("Target the upper left corner of the new zone.");
            }
            else
            {
                Multis.BaseHouse house = Multis.BaseHouse.FindHouseAt(this);
                if (house == null || !house.IsOwner(from))
                {
                    from.SendLocalizedMessage(502092); // You must be in your house to do this.
                }
                else if (CanPlaceNewSensoryZone(house))
                {
                    SensoryConsole v = new SensoryConsole();
                    from.Target = new OnPlacementTarget(v, this, true);
                    from.SendMessage("Target the upper left corner of the new zone.");
                }
                else
                    from.SendMessage("You may not place any more sensory zones.");
            }
        }

        // GM or Player placed Console via deed, auto deleted with house.
        public class OnPlacementTarget : Target
        {
            private SensoryConsole m_SensoryConsole;
            bool m_begin;
            SensoryZoneDeed m_deed;

            public OnPlacementTarget(SensoryConsole sensoryConsole, SensoryZoneDeed deed, bool begin)
                : base(15, false, TargetFlags.None)
            {
                m_SensoryConsole = sensoryConsole;
                m_begin = begin;
                m_deed = deed;
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                m_SensoryConsole.Delete();
                base.OnTargetCancel(from, cancelType);
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                Multis.BaseHouse house = Multis.BaseHouse.FindHouseAt(from);
                if (from.AccessLevel < AccessLevel.GameMaster && house == null || (house != null && !house.Contains((targ as IPoint3D))))
                {
                    from.SendMessage("Target must be within your house.");
                    from.Target = new OnPlacementTarget(m_SensoryConsole, m_deed, m_begin);
                    if (m_begin)
                        from.SendMessage("Target the upper left corner of the new zone.");
                    else
                        from.SendMessage("Target the lower right corner of the new zone.");
                }
                else if (m_begin)
                {
                    m_SensoryConsole.Begin = new Point3D(targ as IPoint3D);
                    from.Target = new OnPlacementTarget(m_SensoryConsole, m_deed, false);
                    from.SendMessage("Target the lower right corner of the new zone.");
                }
                else
                {
                    m_SensoryConsole.End = new Point3D(targ as IPoint3D);
                    if (m_SensoryConsole.PlacementError != SensoryConsole.Error.None)
                    {
                        m_SensoryConsole.Delete();
                        from.SendMessage("Your sensory zone may not span multiple regions.");
                    }
                    else
                    {
                        if (m_deed.IsChildOf(from.Backpack))
                        {
                            from.SendMessage("Zone created.");
                            m_SensoryConsole.Owner = from;

                            m_SensoryConsole.MoveToWorld(new Point3D(m_SensoryConsole.End, (targ as IPoint3D).Z), from.Map);
                            m_SensoryConsole.Movable = false;
                            m_SensoryConsole.Visible = false;

                            // add the console to the houses list of managed items (auto deleted with the house)
                            if (house != null && house.Addons != null && house.Contains((targ as IPoint3D)))
                                if (house.Addons.Contains(m_SensoryConsole) == false)
                                    house.Addons.Add(m_SensoryConsole);

                            // delete the deed
                            m_deed.Delete();
                        }
                        else
                        {
                            m_SensoryConsole.Delete();
                            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                        }
                    }
                }
                return;
            }
        }

        public static bool CanPlaceNewSensoryZone(Multis.BaseHouse house)
        {
            if (house == null)
                return false;

            // 7 sensory zones
            // keyword database is 256 rows if 768 characters
            // so a single user can control 1,376,256 bytes of memory
            int avail = 7;

            // we don't care about sensory consoles that have been freeze dried since their 
            //	storage would have been deleted
            int count = 0;
            foreach (Region rx in Region.Regions)
            {
                if (rx is SensoryRegion)
                    for (int ix = 0; ix < rx.Coords.Count; ix++)
                    {
                        if (house.Contains(rx.Coords[ix].Start))
                            count++;
                    }
            }

            return (count < avail);
        }
    }
}
#endif