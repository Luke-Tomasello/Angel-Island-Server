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

// IntelligentDialogue_Extras.cs
#if false
////////////////////////////////////////////////////////////////////////
// do proximity checking here
if (ArticlePrecedesNoun(tokens_out))
{
    foreach (KeyValuePair<string, int> pair in tokens_out)
        if (!stokens_out.Contains(pair.Key))
            stokens_out.Add(pair.Key);
}

private static bool ArticlePrecedesNoun(List<KeyValuePair<string, int>> words)
{   //limitation: we only check the first 'article' noun' combination.
    if (words.Count < 2)
        return true;
    List<string> articles = new List<string>() { "the", "a", "an" };
    int article = -1;
    int noun = -1;
    foreach (KeyValuePair<string, int> kvp in words)
        if (articles.Contains(kvp.Key))
            article = kvp.Value;
        else
            noun = kvp.Value;

    if (article >= 0 && noun >= 0)
    {// we have an article and a noun
     // where might I find the thief guild
     // this is matched by "wind" "the"
     // Why? "wind" matches "find" (1 fuzzy point difference) and "the" matches"the"
     // if the article is BEFORE the noun, all is cool, otherwise, fail
        return article < noun;
    }

    return true;
}

private static string Compress(string key, string target)
{
    string ComKey = "";
    foreach (char c in key)
    {
        if (c >= 'a' && c <= 'z')
            ComKey += c;
    }

    string ComTarget = "";
    foreach (char c in target)
    {
        if (c >= 'a' && c <= 'z')
            ComTarget += c;
    }

    RollingHash rh = new RollingHash();
    string temp = rh.FindCommonSubstring(ComKey, ComTarget, true);
    temp = ComTarget.Substring(ComTarget.IndexOf(temp));

    return temp;
}
private static string Compress(string key)
{
    string ComKey = "";
    foreach (char c in key)
        if (c >= 'a' && c <= 'z')
            ComKey += c;

    return ComKey;
}

        private static int LevenshteinWrapper(string text, string key)
        {
            /*
             * While Levenshtein is wonderfully helpful, you sometimes need to 
             * sometimes optimize the inputs to work for the application at hand.
             * In the case, (and appropriately so,) Levenshtein penalizes for words
             * *not* in the sentense. So for example, lets take the real world example
             * of "The Tic - Toc Shop". Players searching for tic toc won't find what they
             * are looking for since "the" and "shop" act as negative influences on the result.
             * We solve this (while still keeping the magic of Levenshtein) by trying a couple
             * of varations of common beginnings and endings - "the" and "shop"
             * Why even use Levenshtein if we're going to work around it's original design?
             *  Because Levenshtein will allow matching of phrases with typos in them. I.e.,
             *  Query: where is the bank in maginciia?
             * In this case Magicina is misspelled, but because of the way Levenshtein works
             * It is correctly matched.
             */

            // first, compress out all white spaces and punctuation.
            //  less characters means 1) faster matching, 2) less penalties
            //  (users are less likely to enter punctuation.)
            text = Compress(text);
            key = Compress(key);

            // track our results
            List<int> rankings = new List<int>();

            // rank the original input
            rankings.Add(StringDistance.LevenshteinDistance(Compress(text), Compress(key)));
            
            // now try with out common prefixes and suffixes
            if (!text.Contains("inn"))
                rankings.Add(StringDistance.LevenshteinDistance(Compress(text + "inn"), Compress(key)));

            if (!text.Contains("shop"))
                rankings.Add(StringDistance.LevenshteinDistance(Compress(text + "shop"), Compress(key)));

            if (!text.Contains("the"))
            {
                rankings.Add(StringDistance.LevenshteinDistance(Compress("the" + text), Compress(key)));

                if (!text.Contains("inn"))
                    rankings.Add(StringDistance.LevenshteinDistance(Compress("the" + text + "inn"), Compress(key)));

                if (!text.Contains("shop"))
                    rankings.Add(StringDistance.LevenshteinDistance(Compress("the" + text + "shop"), Compress(key)));
            }
            
            // sort our best fit list based on rank
            rankings.Sort((e1, e2) =>
            {
                return e1.CompareTo(e2);
            });

            // this is out best match
            return rankings[0];
        }

        class SimpleAsyncResult : IAsyncResult
        {
            object _state;


            public bool IsCompleted { get; set; }


            public WaitHandle AsyncWaitHandle { get; internal set; }


            public object AsyncState
            {
                get
                {
                    if (Exception != null)
                    {
                        throw Exception;
                    }
                    return _state;
                }
                internal set
                {
                    _state = value;
                }
            }


            public bool CompletedSynchronously { get { return IsCompleted; } }


            internal Exception Exception { get; set; }
        }
        class SimpleSyncObject : ISynchronizeInvoke
        {
            private readonly object _sync;


            public SimpleSyncObject()
            {
                _sync = new object();
            }


            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                var result = new SimpleAsyncResult();


                ThreadPool.QueueUserWorkItem(delegate {
                    result.AsyncWaitHandle = new ManualResetEvent(false);
                    try
                    {
                        result.AsyncState = Invoke(method, args);
                    }
                    catch (Exception exception)
                    {
                        result.Exception = exception;
                    }
                    result.IsCompleted = true;
                });


                return result;
            }


            public object EndInvoke(IAsyncResult result)
            {
                if (!result.IsCompleted)
                {
                    result.AsyncWaitHandle.WaitOne();
                }


                return result.AsyncState;
            }


            public object Invoke(Delegate method, object[] args)
            {
                lock (_sync)
                {
                    return method.DynamicInvoke(args);
                }
            }


            public bool InvokeRequired
            {
                get { return true; }
            }
        }

        private static void OnTimedEvent(Mobile m, Mobile target)
        {
            if (m_regressionText.Count == 0)
            {   // cleanup timer
                m_ProcessTimer.Stop();
                m_ProcessTimer.Running = false;
                m_ProcessTimer = null;
                // cleanup log file
                lock (mutex)
                {
                    m_logger.Finish();
                }
                // cleanup state
                lock (mutex)
                {
                    m_regressionRecorderEnabled = false;
                }
                return;
            }

            while (m_regressionText.Count > 0)
            {
                string text = m_regressionText.Dequeue();
                text = text.Trim();
                if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                {
                    lock (mutex)
                    {
                        m_logger.Log(Environment.NewLine);
                    }
                    Console.WriteLine();
                }
                else if (text[0] == '#')
                {   // just output the comment
                    lock (mutex)
                    {
                        m_logger.Log(text);
                    }
                    Console.WriteLine(text);
                }
                else
                {   // ok! We've got a command string

                    // remove comments
                    int index = text.IndexOf("//");
                    string head = text;
                    if (index >= 0) head = head.Substring(0, index).Trim();
                    // substitute this placeholder for the NPCs name.
                    head = head.Replace("<npc name>", target.Name);
                    m.Say(head);    // this is just for me, the mobile does not process this
                    target.OnSpeech(new SpeechEventArgs(m, head, Server.Network.MessageType.Regular, 0x3B2, new int[0], true));
                    lock (mutex)
                    {
                        m_logger.Log(text);
                    }
                    Console.WriteLine(text);
                    break;
                }
            }
        }
#if false
        public static void Initialize()
        {
            Server.CommandSystem.Register("Ids", AccessLevel.Administrator, new CommandEventHandler(IDS_OnCommand));
        }
        private static Queue<string> m_regressionText = new Queue<string>();
        private static bool m_regressionRecorderEnabled = false;
        private static LogHelper m_logger = null;
        private static System.Timers.Timer m_idsRegressionTimer = null;
        [Usage("IDS")]
        [Description("Intelligent Dialogue System regression tests.")]
        public static void IDS_OnCommand(CommandEventArgs e)
        {
            if (m_idsRegressionTimer != null)
            {
                e.Mobile.SendMessage("Stopping IDS regression tests.");
                m_regressionText.Clear();
                m_idsRegressionTimer.Stop();
                m_idsRegressionTimer.Dispose();
                m_idsRegressionTimer = null;
                // cleanup log file
                m_logger.Finish();
                // cleanup state
                m_regressionRecorderEnabled = false;
            }
            else
            {
                e.Mobile.SendMessage("Starting IDS regression tests...");
                Mobile target = null;
                if (NearBy(e.Mobile, ref target, typeof(BaseGuard)))
                {
                    string filename = Path.Combine(Core.DataDirectory, "Intelligent Dialogue Regression Tests.txt");
                    foreach (string line in File.ReadAllLines(filename))
                        m_regressionText.Enqueue(line);
                    m_regressionRecorderEnabled = true;
                    m_logger = new LogHelper("ids regression results.log", true);
                    
                    m_idsRegressionTimer = new System.Timers.Timer();
                    m_idsRegressionTimer.SynchronizingObject = new SimpleSyncObject();
                    m_idsRegressionTimer.Elapsed += (sender, z) => { OnTimedEvent(e.Mobile, target); };
                    m_idsRegressionTimer.Interval = 5000;
                    m_idsRegressionTimer.Enabled = true;
                }
                else
                {
                    e.Mobile.SendMessage("There are no guards nearby.");
                }
            }
        }
        class SimpleAsyncResult : IAsyncResult
        {
            object _state;


            public bool IsCompleted { get; set; }


            public WaitHandle AsyncWaitHandle { get; internal set; }


            public object AsyncState
            {
                get
                {
                    if (Exception != null)
                    {
                        throw Exception;
                    }
                    return _state;
                }
                internal set
                {
                    _state = value;
                }
            }


            public bool CompletedSynchronously { get { return IsCompleted; } }


            internal Exception Exception { get; set; }
        }
        class SimpleSyncObject : ISynchronizeInvoke
        {
            private readonly object _sync;


            public SimpleSyncObject()
            {
                _sync = new object();
            }


            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                var result = new SimpleAsyncResult();


                ThreadPool.QueueUserWorkItem(delegate {
                    result.AsyncWaitHandle = new ManualResetEvent(false);
                    try
                    {
                        result.AsyncState = Invoke(method, args);
                    }
                    catch (Exception exception)
                    {
                        result.Exception = exception;
                    }
                    result.IsCompleted = true;
                });


                return result;
            }


            public object EndInvoke(IAsyncResult result)
            {
                if (!result.IsCompleted)
                {
                    result.AsyncWaitHandle.WaitOne();
                }


                return result.AsyncState;
            }


            public object Invoke(Delegate method, object[] args)
            {
                lock (_sync)
                {
                    return method.DynamicInvoke(args);
                }
            }


            public bool InvokeRequired
            {
                get { return true; }
            }
        }
        private static void OnTimedEvent(Mobile m, Mobile target)
        {
            if (m_regressionText.Count == 0)
            {   // cleanup timer
                m_idsRegressionTimer.Stop();
                m_idsRegressionTimer.Dispose();
                m_idsRegressionTimer = null;
                // cleanup log file
                m_logger.Finish();
                // cleanup state
                m_regressionRecorderEnabled = false;
                return;
            }

            while (m_regressionText.Count > 0)
            {
                string text = m_regressionText.Dequeue();
                text = text.Trim();
                if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text))
                {
                    m_logger.Log(Environment.NewLine);
                    Console.WriteLine();
                }
                else if (text[0] == '#')
                {   // just output the comment
                    m_logger.Log(text);
                    Console.WriteLine(text);
                }
                else
                {   // ok! We've got a command string

                    // remove comments
                    int index = text.IndexOf("//");
                    string head = text;
                    if (index >= 0) head = head.Substring(0, index).Trim();
                    // substitute this placeholder for the NPCs name.
                    head = head.Replace("<npc name>", target.Name);
                    m.Say(head);    // this is just for me, the mobile does not process this
                    target.OnSpeech(new SpeechEventArgs(m, head, Server.Network.MessageType.Regular, 0x3B2, new int[0], true));
                    m_logger.Log(text);
                    Console.WriteLine(text);
                    break;
                }
            }
        }
#endif
public static int WordCount(string text)
        {
            return text.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
        
            // throw away all words not available an any of our dictionaries
            Queue<string> queue = new Queue<string>();
            string[] words = text.Split(new char[] { ' ' });
            foreach (string word in words)
            {
                string fuzzy = null;
                if (!CacheFactory.UniqueWords.Contains(word.ToLower()))
                {   // we don't want to throw away typos lile 'provisionir'
                    fuzzy = FindString(CacheFactory.UniqueWords, text, .25);
                    if (fuzzy != null)
                        if (!queue.Contains(fuzzy))
                            queue.Enqueue(fuzzy);
                }
                else
                    if (!queue.Contains(word))
                        queue.Enqueue(word);
            }

            if (queue.Count == 0)
                return LocateType.Unknown;

            // rebuild our 
            string outString = string.Empty;
            while(queue.Count > 0)
                outString+= queue.Dequeue() + " ";

            outString = outString.Trim();
            
            /*
            // pick the handler
            if (ClosestSpecial(text, m, e.Mobile)) return LocateType.ClosestSpecial;
            if (SpecificSpecial(text, m, e.Mobile)) return LocateType.SpecificSpecial;

            if (ClosestNPC(text, m, e.Mobile)) return LocateType.ClosestNPC;
            if (SpecificNPC(text, m, e.Mobile)) return LocateType.SpecificNPC;

            if (ClosestMoongate(text, m, e.Mobile)) return LocateType.ClosestMoongate;
            if (SpecificMoongate(text, m, e.Mobile)) return LocateType.SpecificMoongate;

            if (ClosestPlace(text, m, e.Mobile)) return LocateType.ClosestPlace;
            if (SpecificPlace(text, m, e.Mobile)) return LocateType.SpecificPlace;

            if (ClosestItem(text, m, e.Mobile)) return LocateType.ClosestItem;
            if (SpecificItem(text, m, e.Mobile)) return LocateType.SpecificItem;
            */
            private static bool ClosestSpecial(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {   // no town mentioned

            // patch the special word like "stable" ==> "animal trainer" and retry as an NPC
            PatchSpecial(ref text);

            // look for the NPC
            if (FindString(CacheFactory.VendorTitleList, text, .25) != null && MentionsTown(text, .25) == null)
                return true;    // patch worked, we'll try to match now

            return false;
        }
        private static bool SpecificSpecial(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {   // town mentioned

            // patch the special word like "stable" ==> "animal trainer" and retry as an NPC
            PatchSpecial(ref text);

            // look for the NPC
            if (FindString(CacheFactory.VendorTitleList, text, .25) != null && MentionsTown(text, .25) != null)
                return true;    // patch worked, we'll try to match now

            return false;
        }
        private static bool ClosestNPC(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            // mentions 'thief guildmaster' (or something close) and not a town
            FixNPCArticle(ref text, CacheFactory.VendorTitleList);
            return FindString(CacheFactory.VendorTitleList, text, .25) != null && MentionsTown(text, .25) == null;
        }
        private static bool SpecificNPC(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            // mentions 'thief guildmaster' (or something close) and a town
            FixNPCArticle(ref text, CacheFactory.VendorTitleList);
            return FindString(CacheFactory.VendorTitleList, text, .25) != null && MentionsTown(text, .25) != null;
        }

        private static bool ClosestMoongate(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            // mentions 'moongate' (or something close) and not a town
            return FindString("moongate", text, .25) != null && MentionsTown(text, .25) == null;
        }
        private static bool SpecificMoongate(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            // mentions 'moongate' (or something close) and a town
            return FindString("moongate", text, .25) != null && MentionsTown(text, .25) != null;
        }
        private static bool ClosestPlace(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            // mentions 'rusty anchor' (or something close) and not a town
            return FindString(CacheFactory.PlacesList, text, .25) != null && MentionsTown(text, .25) == null;
        }
        private static bool SpecificPlace(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            // mentions 'rusty anchor' (or something close) and in a town
            return FindString(CacheFactory.PlacesList, text, .25) != null && MentionsTown(text, .25) != null;
        }
        private static bool ClosestItem(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            // mentions 'clean bandage' (or something close) and not a town
            FixObjectArticle(ref text, CacheFactory.InventoryList);
            return FindString(CacheFactory.InventoryList, text, .25) != null && MentionsTown(text, .25) == null;
        }
        private static bool SpecificItem(string text, Mobile ConvoMob, Mobile ConvoPlayer)
        {
            // mentions 'clean bandage' (or something close) and in a town
            FixObjectArticle(ref text, CacheFactory.InventoryList);
            return FindString(CacheFactory.InventoryList, text, .25) != null && MentionsTown(text, .25) != null;
        }

        private static string FindString(List<string> words, string text, double fuzz)
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

        private static List<string> m_uniqueWords = new List<string>(); 
        public static List<string> UniqueWords { get { return m_uniqueWords; } }
        private static void CalcUniqueWords()
        {
            foreach (string sentence in m_placesQuickTable.Keys)
            {
                string[] words = sentence.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (!m_uniqueWords.Contains(word.ToLower()))
                        m_uniqueWords.Add(word.ToLower());
                }
            }

            foreach (string sentence in m_vendorsQuickTable.Keys)
            {
                string[] words = sentence.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (!m_uniqueWords.Contains(word.ToLower()))
                        m_uniqueWords.Add(word.ToLower());
                }
            }

            foreach (string sentence in m_moongatesQuickTable.Keys)
            {
                string[] words = sentence.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (!m_uniqueWords.Contains(word.ToLower()))
                        m_uniqueWords.Add(word.ToLower());
                }
            }

            foreach (string sentence in m_docksQuickTable.Keys)
            {
                string[] words = sentence.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (!m_uniqueWords.Contains(word.ToLower()))
                        m_uniqueWords.Add(word.ToLower());
                }
            }

            foreach (string sentence in m_regionQuickTable.Keys)
            {
                string[] words = sentence.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (!m_uniqueWords.Contains(word.ToLower()))
                        m_uniqueWords.Add(word.ToLower());
                }
            }

            foreach (string sentence in m_inventoryQuickTable.Keys)
            {
                string[] words = sentence.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (!m_uniqueWords.Contains(word.ToLower()))
                        m_uniqueWords.Add(word.ToLower());
                }
            }

            foreach (string sentence in TownsList)
            {
                string[] words = sentence.Split(' ',StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (!m_uniqueWords.Contains(word.ToLower()))
                        m_uniqueWords.Add(word.ToLower());
                }
            }
        }
#endif