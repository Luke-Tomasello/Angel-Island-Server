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

/* Mobiles/Vendors/QuestGiver.cs
 * CHANGELOG:
 *  7/12/22, Adam
 *      Add verb "terminate" - does a complete reset of the quest (deletes all remembered items and keywords.)
 *      Rename old verb "has" to "given" - cheks to see if you have been "given" an item (you may no longer have it though.)
 *      Add verb "has" - does a physical check of the players backpack to see if you have a labeled item.
 *	11/1/10, Adam
 *		If the remembered item is deleted (or freeze dried) it's serialized as zero which is normal
 *		item bahavior. However, we were then trying to add a null ietm to the databasse and blowing things up.
 *		I added code to skil deleted items, but we should probably come up with a better way to handle this.
 *	1/30/10, Adam
 *		initial creation
 */

using CodingSeb.ExpressionEvaluator;
using Server.Diagnostics;
using Server.Engines;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Server.Mobiles
{
    public class QuestGiver : BaseVendor
    {
        private Mobile m_Owner;
        private Mobile m_From;

        #region QuestGiver Data
        // key value pairs
        // [keyword1, keyword2...] [action1, action2...]
        private Dictionary<string, object[]> m_KeywordDatabase = new Dictionary<string, object[]>(StringComparer.OrdinalIgnoreCase);
        // label database
        private Dictionary<Item, object[]> m_ItemDatabase = new Dictionary<Item, object[]>();
        // condition onreceive|give OldName == shovel SUCCESS else FAIL
        private List<KeyValuePair<string, object[]>> m_ConditionDatabase = new();
        // private keywords cannot be spoken aloud by the player like public keywords can
        private List<string> m_PrivateKeywords = new();
        // verb database
        private static List<string> m_VerbDatabase = new()
        {
            "has",         // does someone physically have the item in their possession? (different than "given")
            "random",
            "foreach",
            "emote",
            "sayto",
            "say",
            "give",         // give something to somebody
            "dupe",         // give a copy of something to somebody
            "given",        // was something given to someone? (different that "has")
            "terminate",
            "play",         // play a sound effect
            "keep",         // item disposition, keep, return, delete
            "return",       // item disposition, keep, return, delete
            "delete",       // item disposition, keep, return, delete
            "break",        // break from 'condition' execution
            "set",          // set the value of a 'context' variable - serialized
        };
        public class VariableInfo
        {
            [Flags]
            public enum Attributes
            {
                None = 0x00,
                ReadOnly = 0x01,     // can it be written? 
                Serialize = 0x02,   // will we save/restore it?
                System = 0x04,      // not cleared on an NPC reset
                Ephemeral,          // player and item properties - changes often
            }
            public Attributes Flags;
            public void SetFlag(Attributes flag, bool value)
            {
                if (value)
                    Flags |= flag;
                else
                    Flags &= ~flag;
            }

            public bool GetFlag(Attributes flag)
            {
                return ((Flags & flag) != 0);
            }
            public object Value;    // the value
            public VariableInfo(object value, Attributes flags)
            {
                Value = value;
                Flags = flags;
            }
        }
        // User and System variables. some serialized, some not, some read-only, some not.
        private Dictionary<string, VariableInfo> m_GlobalVariableDatabase = new Dictionary<string, VariableInfo>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<Serial, Dictionary<string, VariableInfo>> m_PlayerVariableDatabase =
            new Dictionary<Serial, Dictionary<string, VariableInfo>>();

        // If the QG has the 'dupe' action, it will need review
        public bool m_needsReview;

        private static Regex TokenOpToken = new Regex(@"^("".*?""|\b[0-9]+\b|\b[a-zA-Z][a-zA-Z0-9_\.]+\b)\s+(==|!=|<|>|<=|>=)\s+("".*?""|\b[0-9]+\b|\b[a-zA-Z][a-zA-Z0-9_\.]+\b)", RegexOptions.Compiled);
        private static Regex LogicalOps = new Regex(@"^\&\&|\|\|", RegexOptions.Compiled);
        private static Regex PCLabel = new Regex(@"^(\b[a-zA-Z][a-zA-Z_0-9]+\b)", RegexOptions.Compiled);
        private static Regex PreCAddLabel = new Regex(@"(\b[a-zA-Z]+\b)?\s?(add)\s+(label)\s+(\b[a-zA-Z][a-zA-Z0-9_\.]+\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex Keyword = new Regex(@"(\b[a-zA-Z]+\b)?\s?(add)\s+(keyword)\s+(\b[a-zA-Z][a-zA-Z0-9_\.]+\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex LRTPToken = new Regex(@"^("".*?""|\b[a-zA-Z]+[\p{P}][a-zA-Z]+\b|%.*?%|\b[0-9]+\b|\b[a-zA-Z0-9_]+\b|\b.+\b)", RegexOptions.Compiled);
        private static Regex LRTPPuncuation = new Regex(@"^(?![""%<>=\&\|])\p{P}", RegexOptions.Compiled);
        private static Regex LRTPWhatever = new Regex(@"^[^\s|$]+", RegexOptions.Compiled);
        private static Regex ValidVarName = new Regex(@"^[a-zA-Z][a-zA-Z0-9_\.]*$", RegexOptions.Compiled);
        private static Regex Label = new Regex(@"\s+(\b[a-zA-Z_]+[a-zA-Z_0-9]+\b)$", RegexOptions.Compiled);
        private static Regex LabelElseLabel = new Regex(@"\s+(\b[a-zA-Z_]+[a-zA-Z_0-9]+\b)\s+else\s+(\b[a-zA-Z_]+[a-zA-Z_0-9]+\b)$", RegexOptions.Compiled);
        private static Regex SpecialExcludes = new Regex(@"\b(?<!"")[a-zA-Z_]+[a-zA-Z_0-9]+(?![\w\s]*\w""|"")\b", RegexOptions.Compiled);
        private static Regex InvalidNamePatt = new Regex("[^-a-zA-Z0-9' ]", RegexOptions.Compiled);
        private static Regex PCAddLabel = new Regex(@"add\s+(public|private)\s+"".*?""|\b[a-zA-Z][a-zA-Z0-9_]+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex Condition = new Regex(@"condition\s+(onreceive|give)\s+\b([a-zA-Z]+)\b\s+(==|!=)\s+(""[^""]*""|\b[a-zA-Z0-9]+\b)\s+(\b[a-zA-Z_0-9]+\b)($|\s+(else)\s+(\b[a-zA-Z_0-9]+\b)+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex AddLabel = new Regex(@"add\s+(public|private)\s+\b[a-zA-Z][a-zA-Z0-9_]+\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        #endregion QuestGiver Data

        #region Variable Management
        Dictionary<string, VariableInfo> SelectDatabase(Mobile m)
        {
            if (m == null)
                return m_GlobalVariableDatabase;

            if (m_PlayerVariableDatabase.ContainsKey(m.Serial))
                return m_PlayerVariableDatabase[m.Serial];

            // this empty database will get packed out before serialization
            m_PlayerVariableDatabase.Add(m.Serial, new Dictionary<string, VariableInfo>());
            return m_PlayerVariableDatabase[m.Serial];

            return null;
        }
        private object GetVar(string varName, Mobile m = null)
        {   // select the database
            Dictionary<string, VariableInfo> db = SelectDatabase(m);
            // all variables will be lower case
            varName = varName.ToLower().Trim();
            if (db.ContainsKey(varName))
                return db[varName].Value;
            return null;
        }
        private VariableInfo GetVarInfo(string varName, Mobile m = null)
        {   // select the database
            Dictionary<string, VariableInfo> db = SelectDatabase(m);
            // all variables will be lower case
            varName = varName.ToLower().Trim();
            if (db.ContainsKey(varName))
                return db[varName];
            return null;
        }
        private bool DeleteVar(string varName, Mobile m = null)
        {   // select the database
            Dictionary<string, VariableInfo> db = SelectDatabase(m);
            if (db.ContainsKey(varName))
            {
                db.Remove(varName);
                return true;
            }
            return false;
        }
        private bool SetVar(string varName, object value, VariableInfo.Attributes flags, Mobile m = null)
        {
            string failReason = string.Empty;
            return SetVar(varName, value, flags, ref failReason, m);
        }
        private bool SetVar(string varName, object value, VariableInfo.Attributes flags, ref string failReason, Mobile m = null)
        {   // select the database
            Dictionary<string, VariableInfo> db = SelectDatabase(m);
            // all variables will be lower case
            varName = varName.ToLower().Trim();
            if (value is string) value = (value as string).ToString().ToLower();
            var match_condition = ValidVarName.Match(varName);
            if (!match_condition.Success)
            {
                failReason = string.Format("Bad format for variable name '{0}'", varName);
                return false;
            }
            else
            {
                if (db.ContainsKey(varName))
                {
                    if (value != null)
                        if (value.ToString().Length <= 128)
                            db[varName] = new VariableInfo(value, flags);
                        else
                        {
                            failReason = "string too long";
                            return false;
                        }
                }
                else
                {   // add new variable
                    if (db.Count > 768)
                    {
                        failReason = string.Format("Your database is full");
                        return false;
                    }
                    if (value != null)
                        if (value.ToString().Length <= 128)
                            db.Add(varName, new VariableInfo(value, flags));
                        else
                        {
                            failReason = "string too long";
                            return false;
                        }
                }
            }

            return true;
        }
        #endregion Variable Management

        #region QuestGiver Props
        [CommandProperty(AccessLevel.Owner)]
        public bool NeedsReview { get { return m_needsReview; } set { m_needsReview = value; QGLogger(null); InvalidateProperties(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Memory
        {
            get { return TimeSpan.FromMinutes((double)GetVar("memory")); }
            set { SetVar("memory", value.TotalMinutes, VariableInfo.Attributes.System | VariableInfo.Attributes.Serialize); InvalidateProperties(); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public int Distance
        {
            get { return (int)GetVar("distance"); }
            set { SetVar("distance", value, VariableInfo.Attributes.System | VariableInfo.Attributes.Serialize); InvalidateProperties(); }
        }
        #endregion QuestGiver Props

        #region Misc vendor stuff.
        protected ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }
        public override bool IsActiveVendor { get { return false; } }
        public override bool CanBeDamaged() { return false; }
        public override bool ShowFameTitle { get { return false; } }
        public override bool DisallowAllMoves { get { return true; } }
        public override bool ClickTitle { get { return true; } }
        public override bool CanTeach { get { return false; } }
        public override void InitSBInfo() { }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlayerMobile Owner
        {
            get { return (PlayerMobile)m_Owner; }
            set { m_Owner = value; }
        }

        [Constructable]
        public QuestGiver()
            : this(null)
        {
        }

        public QuestGiver(Mobile owner)
            : base("the quest giver")
        {
            m_Owner = owner;
            IsInvulnerable = true;
            CantWalkLand = true;
            InitStats(75, 75, 75);
            EmoteHue = Utility.RandomYellowHue();
        }

        public QuestGiver(Serial serial)
            : base(serial)
        {
        }
        #endregion Misc vendor stuff.
        private enum TokenTypes
        {
            String,
            Char,
        }
        private Dictionary<string, VariableInfo> PackedGlobalVariableDatabase()
        {   // only write those variables marked as Serializable
            Dictionary<string, VariableInfo> these = new();
            foreach (var kvp in m_GlobalVariableDatabase)
                if (kvp.Value.GetFlag(VariableInfo.Attributes.Serialize))
                    these.Add(kvp.Key, kvp.Value);

            return these;
        }
        private Dictionary<Serial, Dictionary<string, VariableInfo>> PackPlayerVariableDatabase()
        {
            Dictionary<Serial, Dictionary<string, VariableInfo>> PackedPlayerVariableDatabase =
            new Dictionary<Serial, Dictionary<string, VariableInfo>>();
            foreach (var kvp in m_PlayerVariableDatabase)
                if (kvp.Value.Count > 0)
                    PackedPlayerVariableDatabase.Add(kvp.Key, kvp.Value);

            return PackedPlayerVariableDatabase;
        }
        #region Region Serialize
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(6);    //version

            // version 6
            /*Dictionary<Serial, Dictionary<string, VariableInfo>>*/
            var packedPlayerVariableDatabase = PackPlayerVariableDatabase();
            writer.Write(packedPlayerVariableDatabase.Count);
            foreach (var kvp in packedPlayerVariableDatabase)
            {
                writer.Write(kvp.Key);

                writer.Write(kvp.Value.Count);
                foreach (var kvp2 in kvp.Value)
                {
                    writer.Write(kvp2.Key);
                    Utility.Writer(kvp2.Value.Value, writer);
                    writer.Write((int)kvp2.Value.Flags);
                }
            }

            // version 5
            var packedGlobalVariableDatabase = PackedGlobalVariableDatabase();
            writer.Write(packedGlobalVariableDatabase.Count);
            foreach (KeyValuePair<string, VariableInfo> kvp in packedGlobalVariableDatabase)
            {
                writer.Write(kvp.Key);
                Utility.Writer(kvp.Value.Value, writer);
                writer.Write((int)kvp.Value.Flags);
            }

            // version 4
            writer.Write(m_needsReview);

            // version 3
            writer.Write(m_ConditionDatabase.Count);
            foreach (KeyValuePair<string, object[]> kvp in m_ConditionDatabase)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Length);
                foreach (object o in kvp.Value)
                {
                    if (o is string)
                        writer.Write(o as string);
                    else
                        Console.WriteLine("Error: Trying to write unknown type in Quest Giver: {0}", o);
                }
            }

            writer.Write(m_PrivateKeywords.Count);
            foreach (var key in m_PrivateKeywords)
                writer.Write(key);

            // version 2
            // write the Alias database
            List<string[]> AliasDatabase = new List<string[]>();
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
            {
                string[] found = new string[0];
                if (FindKeywordAliases(kvp.Key, out found))
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

            // version 1 (obsoleted in version 5)
            //writer.Write(m_memory);
            //writer.Write(m_distance);

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
                    if (o is Field)
                    {
                        writer.Write((int)((Field)o));
                    }
                    else if (o is string)
                    {
                        writer.Write(o as string);
                    }
                    else
                        Console.WriteLine("Error: Trying to write unknown type in Quest Giver: {0}", o);
                }
            }

            //version 0:
            writer.Write(m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            // introduced in version 2 to fixup the aliases post-load
            List<string[]> AliasDatabase = new List<string[]>();

            switch (version)
            {
                case 6:
                    {
                        int count = reader.ReadInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            Serial serial = (Serial)reader.ReadInt();

                            int jcount = reader.ReadInt();
                            Dictionary<string, VariableInfo> tmp = new();
                            for (int jx = 0; jx < jcount; jx++)
                            {
                                string varName = reader.ReadString();
                                object value = Utility.Reader(reader);
                                int flags = reader.ReadInt();
                                tmp.Add(varName, new VariableInfo(value, (VariableInfo.Attributes)flags));
                            }

                            m_PlayerVariableDatabase.Add(serial, tmp);
                        }
                        goto case 5;
                    }
                case 5:
                    {
                        int count = reader.ReadInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            string varName = reader.ReadString();
                            object value = Utility.Reader(reader);
                            int flags = reader.ReadInt();
                            m_GlobalVariableDatabase.Add(varName, new VariableInfo(value, (VariableInfo.Attributes)flags));
                        }
                        goto case 4;
                    }
                case 4:
                    {
                        m_needsReview = reader.ReadBool();
                        goto case 3;
                    }
                case 3:
                    {
                        // read the conditions database
                        int cond_count = reader.ReadInt();
                        for (int ix = 0; ix < cond_count; ix++)
                        {
                            string key = reader.ReadString();
                            int nelts = reader.ReadInt();
                            List<string> values = new List<string>();
                            for (int jx = 0; jx < nelts; jx++)
                                values.Add(reader.ReadString());

                            m_ConditionDatabase.Add(new KeyValuePair<string, object[]>(key, values.ToArray()));
                        }

                        // read the private keywords database
                        int pd_count = reader.ReadInt();
                        for (int jx = 0; jx < pd_count; jx++)
                            m_PrivateKeywords.Add(reader.ReadString());

                        goto case 2;
                    }
                case 2:
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
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 5)
                        {   // moved to m_VariableDatabase
                            /*m_memory = */
                            reader.ReadDouble();
                            /*m_distance =*/
                            reader.ReadInt();
                        }

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
                                Field field = (Field)reader.ReadInt();
                                list.Add(field);
                                switch (field)
                                {
                                    case Field.Track:
                                        continue;

                                    case Field.Name:
                                        list.Add(reader.ReadString());
                                        jx++;
                                        continue;
                                }
                            }

                            if (key != null)
                                m_ItemDatabase.Add(key, list.ToArray());
                            else
                            {   // the key has been deleted.
                                // would should probably add 'status' strings to the QG so that he can tell
                                //	the owner what happened. For now just delete the item, i.e., don't add it.
                            }
                        }

                        goto case 0;
                    }
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
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

            NameHue = CalcInvulNameHue();

            // finally, initialize our variable database with system variables if they are not already set
            //if (m_VariableDatabase.ContainsKey("needsReview") == false)
            //m_VariableDatabase.Add("needsReview", new VariableInfo(true, VariableInfo.Attributes.System | VariableInfo.Attributes.ReadOnly));

            if (m_GlobalVariableDatabase.ContainsKey("memory") == false)
                // default: 30 minutes (how long we remember players. onenter)
                m_GlobalVariableDatabase.Add("memory", new VariableInfo((double)30, VariableInfo.Attributes.System | VariableInfo.Attributes.Serialize));

            if (m_GlobalVariableDatabase.ContainsKey("distance") == false)
                // how far until we talk to a player (distance to player)
                m_GlobalVariableDatabase.Add("distance", new VariableInfo(6, VariableInfo.Attributes.System | VariableInfo.Attributes.Serialize));

            if (m_GlobalVariableDatabase.ContainsKey("disposition") == false)
                // what to do when an object is dropped (default: return)
                //  a transient value that should not be preserved across saves. Only valid for the moment
                //  when something is dropped ("keep", "return", or "delete")
                m_GlobalVariableDatabase.Add("disposition", new VariableInfo("return", VariableInfo.Attributes.ReadOnly | VariableInfo.Attributes.System));
        }
        #endregion Serialize

        private Memory m_PlayerMemory = new Memory();       // memory used to remember if we a saw a player in the area
        public override void OnSee(Mobile m)
        {
            // yeah
            if (m is PlayerMobile == false)
                return;

            // sanity
            if (m.Deleted || m.Hidden || !m.Alive || m.AccessLevel > this.AccessLevel || !this.CanSee(m))
                return;

            // too far away
            if (this.GetDistanceToSqrt(m) > (int)GetVar("distance"))
                return;

            if (m_PlayerMemory.Recall(m) == false)
            {   // we havn't seen this player yet
                m_PlayerMemory.Remember(m, TimeSpan.FromSeconds((double)GetVar("memory") * 60).TotalSeconds);   // remember him for this long (*60 convert to minutes)
                bool found = m_KeywordDatabase.ContainsKey("onenter");                      // is OnEnter defined?
                if (found)                                                                  // if so execute!
                    OnSpeech(new SpeechEventArgs(m, "onenter", MessageType.Regular, SpeechHue, new int[0], true));
            }
            else
            {   // we remember this player so we won't 'onenter', but rather 'onreturn'
                bool found = m_KeywordDatabase.ContainsKey("onreturn");                     // is OnReturn defined?
                if (found)                                                                  // if so execute!
                    OnSpeech(new SpeechEventArgs(m, "onreturn", MessageType.Regular, SpeechHue, new int[0], true));
            }
        }

        private DateTime m_lastLook = DateTime.MinValue;
        public override void OnThink()
        {
            base.OnThink();

            // look around every 2 seconds
            if (DateTime.UtcNow > m_lastLook && AIObject != null)
            {   // remember players in the area
                AIObject.LookAround(RangePerception);
                m_lastLook = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);
            }
        }

        public override bool IsOwner(Mobile m)
        {
            return (m == m_Owner || m.AccessLevel >= AccessLevel.GameMaster);
        }

        public bool IsHomeOwner(Mobile m)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);
            if (house != null)
                return house.IsCoOwner(m);
            return false;
        }

        public override void InitBody()
        {
            Hue = Utility.RandomSkinHue();
            SpeechHue = 0x3B2;

            NameHue = CalcInvulNameHue();

            if (this.Female = Utility.RandomBool())
            {
                this.Body = 0x191;
                this.Name = NameList.RandomName("female");
            }
            else
            {
                this.Body = 0x190;
                this.Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Item item = new FancyShirt(Utility.RandomNeutralHue());
            item.Layer = Layer.InnerTorso;
            AddItem(item);
            AddItem(new LongPants(Utility.RandomNeutralHue()));
            AddItem(new BodySash(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new Cloak(Utility.RandomNeutralHue()));

            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            Container pack = new Backpack();
            pack.Movable = false;
            AddItem(pack);
        }

        public override bool CheckNonlocalDrop(Mobile from, Item item, Item target)
        {
            bool result = false;
            if (base.CheckNonlocalDrop(from, item, target))
                result = true;
            else if (IsOwner(from))
                result = true;

            if (result == true)
            {
                // We must wait until the item is added
                Timer.DelayCall(TimeSpan.Zero, new TimerStateCallback(NonLocalDropCallback), new object[] { from, item });
            }

            return result;
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            if (base.CheckNonlocalLift(from, item))
                return true;
            else if (IsOwner(from))
                return true;

            return false;
        }

        private string GetTrackedItemLabel(Item dropped)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
            {
                if (kvp.Key == dropped && GetField(kvp.Value, Field.Track) != null)
                    return GetField(kvp.Value, Field.Name) as string;
            }

            return null;
        }

        private bool TrackedItem(Item dropped)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
            {
                if (kvp.Key == dropped && GetField(kvp.Value, Field.Track) != null)
                    return true;
            }

            return false;
        }

        private bool UnTrackItem(Item dropped)
        {
            Item found = null;
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
            {
                if (kvp.Key == dropped && GetField(kvp.Value, Field.Track) != null)
                {
                    found = kvp.Key;
                    break;
                }
            }

            if (found != null)
            {
                m_ItemDatabase.Remove(found);
                return true;
            }
            return false;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            // do not allow players to interact with a QG under review
            if (Core.ReleasePhase == ReleasePhase.Production && m_needsReview == true && !IsOwner(from))
            {
                Say("Sorry, my script is pending review. Please check back later.");
                return false;
            }
            // manfred worried about obscure exploit potential here,
            // the quest item is a bank check for 1m gold. It's supposed to have a quest code of X, but the GM forgot to add the code and
            //      only 'type' check (OldName == gold).
            //  the player can drop any bank check worth 12g say, and the QG would accept it. 
            // The owner can add these items as rewards, just not quest items.
            if ((dropped is BankCheck || dropped is Gold) && !IsOwner(from))
            {
                Say("Sorry, I don't currently accept such items.");
                return false;
            }

            /// stocking the NPC
            if (IsOwner(from))
            {
                if (this.Backpack != null && this.Backpack.TryDropItem(from, dropped, false))
                {
                    OnItemGiven(from, dropped);
                    return true;
                }
                else
                {
                    SayTo(from, 503211); // I can't carry any more.
                    return false;
                }
            }
            // player dropping something
            else
            {
                try
                {
                    // load our memory with both the item's and player's properties
                    LoadVariables(dropped);
                    LoadVariables(from);
                    string failReason = string.Empty;

                    // default item dropped disposition
                    SetVar("disposition", "return", VariableInfo.Attributes.ReadOnly | VariableInfo.Attributes.System, ref failReason);
                    // 'dropped' ensures we are in the context of "onreceive" (OnDragDrop)
                    SetVar("dropped", dropped, VariableInfo.Attributes.ReadOnly, ref failReason);
                    // should we break from 'condition' execution?
                    SetVar("break", false, VariableInfo.Attributes.ReadOnly | VariableInfo.Attributes.System, ref failReason);

                    // are we even processing onreceive?
                    if (m_KeywordDatabase.ContainsKey("onreceive"))
                    {
                        foreach (var condition in m_ConditionDatabase)
                        {
                            if (condition.Key.Equals("onreceive"))
                            {
                                List<string> temp = condition.Value.Select(i => i.ToString()).ToList();
                                int error = 0;
                                string exp_result = string.Empty;

                                object o = ExpressionRun(from, temp, ref error, ref failReason);
                                if (o != null)
                                    exp_result = (string)o;

                                if (error != 0)
                                {
                                    if (m_Owner != null)
                                        m_Owner.SendMessage(failReason);
                                    return false;
                                }

                                // branch to the appropriate label
                                object[] tokens = new object[1] { exp_result };

                                // execute the actions by branching to the corresponding label
                                string match;
                                if (FindKeyPhrase(from, tokens, 0, out match) || FindKeyword(tokens, 0, out match) && m_KeywordDatabase[match].Length > 0)
                                {
                                    // execute the action for this keyword: branch
                                    {
                                        // begin execute 
                                        object[][] actions = SplitArray(m_KeywordDatabase[match], '|');
                                        int depth = 0;
                                        try { ExecuteActions(from, actions, ref depth); }
                                        catch
                                        {
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Excessive recursion detected for keyword ({0}).", match), from.NetState);
                                        }
                                    }
                                    // end execute
                                }

                                #region onreceive actions (not sure this is needed/appropriate)
                                // now execute the actions, but only if statement_truth == true
                                //  The actions include what to do with the item (keep, return, delete. return is the default)
                                //  This will be stored in the disposition
                                if (exp_result != null)
                                {
                                    tokens[0] = "onreceive";
                                    if (FindKeyPhrase(from, tokens, 0, out match) || FindKeyword(tokens, 0, out match) && m_KeywordDatabase[match].Length > 0)
                                    {
                                        // execute the verb for this keyword
                                        {
                                            // begin execute 
                                            object[][] actions = SplitArray(m_KeywordDatabase[match], '|');
                                            int depth = 0;
                                            try { ExecuteActions(from, actions, ref depth); }
                                            catch
                                            {
                                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Excessive recursion detected for keyword ({0}).", match), from.NetState);
                                            }
                                        }
                                        // end execute
                                    }
                                }
                                #endregion onreceive actions (not sure this is needed/appropriate)

                                // we stop executing if the user breaks from 'condition' execution
                                //  I.e., action break
                                if (CheckBreak())
                                    break;
                            }
                        }
                    }
                }
                finally
                {
                    // now delete the notion of the thing dropped
                    //  This is checked by 'disposition' processing "keep", "return" and "delete" to make sure
                    //  we are in a context (onreceive) where we may execute one of the dispositions.
                    DeleteVar("dropped");
                }
            }

            return DragDropDisposition(from, dropped);
        }

        private bool CheckBreak()
        {
            object o = (bool)GetVar("break");
            if (o is bool) return ((bool)o);
            else return false;
        }

        private bool DetermineExpressionTruth(object o, List<string> tokens)
        {
            bool foundElse = false;
            string match = o == null ? "" : o.ToString();
            foreach (string s in tokens)
            {
                if (s.Equals("else", StringComparison.OrdinalIgnoreCase))
                    foundElse = true;
                else if (match.Equals(s, StringComparison.OrdinalIgnoreCase) && foundElse)
                    return false;
                else if (match.Equals(s, StringComparison.OrdinalIgnoreCase) && !foundElse)
                    return true;
            }
            return false;
        }
        private object ExpressionRun(Mobile m, List<string> temp, ref int error, ref string failReason)
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            string[] str_tokens = temp.ToArray();
            string script = MakeString(m, str_tokens, 0);

            // sanity checking
            if (!(script[script.Length - 1] == '"' || Char.IsLetterOrDigit(script[script.Length - 1])))
            {
                error = -1;
                failReason = "general format error";
                return null;
            }

            // replace variables with constant values
            foreach (var str_token in str_tokens)
            {
                foreach (Dictionary<string, VariableInfo> db in GetDatabases(m))
                {
                    if (db.ContainsKey(str_token))
                    {   // remove the pluralization decoration from the returned string
                        if (db[str_token].Value is string)
                        {
                            if (str_token.Contains("OldName", StringComparison.OrdinalIgnoreCase))
                            {
                                object o = GetVar(str_token);
                                if (o is string)
                                    script = script.Replace(str_token, '"' + LookupOldName(o as string) + '"');
                                else
                                    script = script.Replace(str_token, '"' + str_token + '"');
                            }
                            else
                                script = script.Replace(str_token, '"' + db[str_token].Value.ToString() + '"');
                        }
                        else
                            script = script.Replace(str_token, db[str_token].Value.ToString());
                    }
                }
            }

            // the three constructs we will accept:
            // x = y SUCCESS else FAIL
            // -or-
            // x = y SUCCESS
            // -or-
            // x = y 

            var match_condition = LabelElseLabel.Match(script);
            if (!match_condition.Success)
            {
                match_condition = Label.Match(script);
                // make sure the token we're ending with is (not) a known keyword
                if (!match_condition.Success || !m_KeywordDatabase.ContainsKey(match_condition.Value.Trim()))
                {   // no labels here, it's a simple expression
                    script = "return " + script + " ;";
                    goto done_parsing;
                }
                // looks like the tail is a keyword/label
                string tail = Label.Replace(script, " ) return \"$1\";");
                script = "if ( " + tail;
            }
            else
            {
                string tail = LabelElseLabel.Replace(script, " ) return \"$1\"; else return \"$2\";");
                script = "if ( " + tail;
            }

        done_parsing:

            // now double quote all strings
            while (DoubleQuoteStrings(ref script))
                ;

            try
            {
                object o = evaluator.ScriptEvaluate(script);
                return o;
            }
            catch (Exception ex)
            {
                error = -1;
                failReason = ex.Message;
                return null;
            }
        }
        private List<Dictionary<string, VariableInfo>> GetDatabases(Mobile m)
        {
            List<Dictionary<string, VariableInfo>> list = new();
            list.Add(SelectDatabase(m));
            list.Add(SelectDatabase(null));
            return list;
        }
        private bool DoubleQuoteStrings(ref string script)
        {
            MatchCollection match_collection = SpecialExcludes.Matches(script);
            foreach (Match match in match_collection)
            {
                if (match.ToString().Equals("if") || match.ToString().Equals("else") || match.ToString().Equals("return"))
                    continue;
                else
                {
                    // replace apple with "apple"
                    script = script.Remove(match.Index, match.Length).Insert(match.Index, '"' + match.Value + '"');
                    return true;
                }
            }

            return false;
        }
        private class ConditionTest
        {
            public object[] Value;
            public bool ExpectedResult;
            public ConditionTest(string text, bool expectedResult)
            {
                Value = text.Split(' ').ToArray();
                ExpectedResult = expectedResult;
            }
        }
        private void RunConditionTests(Mobile m)
        {   // is this simple version of the condition tester, we don't support "double quoted strings"
            List<ConditionTest> list = new()
            {
                { new ConditionTest("\"Apple\" == \"apple\" || \"I am a banana\" == \"I am a banana\"" ,true) },

                { new ConditionTest("item.ItemID == 2512 && item.OldName == apple && item.Amount == 1",true) },
                // ---------------------------------------------------------- false ----VVV  
                { new ConditionTest("item.ItemID == 2512 && item.OldName == apple && item.Amount == 100 || item.Amount == 1",true) },
                // ---------------------------------------------------------- false ----VVV --- true --- V --- false ---V 
                { new ConditionTest("item.ItemID == 2512 && item.OldName == apple && item.Amount == 100 || item.Amount == 1 || item.Amount == 9",true) },
                // ----------------- false ------VVV - true ---- V 
                { new ConditionTest("item.ItemID == 0 || item.Amount == 1",true) },
                // test a dangling "&&"
                { new ConditionTest("item.ItemID == 0 &&",false) },
                // ---------------------------------------------------------- false ----VVV --- true --- V --- false ---V -- false ----V -- false ----V
                { new ConditionTest("item.ItemID == 2512 && item.OldName == apple && item.Amount == 100 || item.Amount == 1 || item.Amount == 9 && item.Amount == 17 && item.Amount == 19",true) },

                { new ConditionTest("flip != blah",true) },
                { new ConditionTest("3 != 4",true) },
                { new ConditionTest("\"3\" != 4",false) },          // incompatible terms
                { new ConditionTest("apple != 4",false) },          // incompatible terms
                { new ConditionTest("4 != apple",false) },          // incompatible terms
                { new ConditionTest("item.OldName != 4",false) },   // incompatible terms

                { new ConditionTest("1 < 2",true) },
                { new ConditionTest("2 > 1",true) },
                { new ConditionTest("1 <= 2",true) },
                { new ConditionTest("1 >= 2",false) },
                { new ConditionTest("apple < banana",false) },  // "Operator cannot be applied to operands of type 'string' and 'string'"
                { new ConditionTest("banana > apple",false) },  // "Operator cannot be applied to operands of type 'string' and 'string'"
                { new ConditionTest("apple <= banana",false) }, // "Operator cannot be applied to operands of type 'string' and 'string'"
                { new ConditionTest("apple >= banana",false) }, // "Operator cannot be applied to operands of type 'string' and 'string'"
            };

            Apple dropped = new Apple();
            // load our memory with the items properties
            LoadVariables(dropped);
            bool pass = true;
            foreach (var condition in list)
            {
                List<string> temp = condition.Value.Select(i => i.ToString()).ToList();
                int error = 0;
                string failReason = string.Empty;
                bool exp_result = false;

                object o = ExpressionRun(m, temp, ref error, ref failReason);
                if (o != null)
                    exp_result = (bool)o;

                if (error != 0)
                {
                    if (exp_result != condition.ExpectedResult)
                    {
                        m_Owner.SendMessage("Test failed:");
                        m_Owner.SendMessage("\t{0}", MakeString(m, condition.Value, 0));
                        pass = false;
                    }
                    else
                    {
                        m_Owner.SendMessage("{0}, but was expected for line:", failReason);
                        m_Owner.SendMessage("\t{0}", MakeString(m, condition.Value, 0));
                    }
                }
                else if (exp_result != condition.ExpectedResult)
                {
                    m_Owner.SendMessage("Test failed:");
                    m_Owner.SendMessage("\t{0}", MakeString(m, condition.Value, 0));
                    pass = false;
                }
            }

            // remove the item's properties from the table of variables
            //foreach (var kvp in kvps)
            //if (m_VariableDatabase.ContainsKey(kvp.Key))
            //m_VariableDatabase.Remove(kvp.Key);

            dropped.Delete();
            if (pass == true)
                m_Owner.SendMessage("All tests passed.");
        }
        private Dictionary<string, object> GetKVPPairs(object o)
        {
            Dictionary<string, object> table = new(StringComparer.OrdinalIgnoreCase);
            PropertyInfo[] props = o.GetType().GetProperties();
            for (int i = 0; i < props.Length; i++)
            {
                try
                {
                    if (props[i].CanRead)
                    {
                        // only integral and string types for now
                        object p = props[i].GetValue(o, null);
                        //       sbyte ,       byte ,       short ,       ushort ,       int ,       uint ,       long ,       ulong ,       char, and    string
                        if (p is sbyte || p is byte || p is short || p is ushort || p is int || p is uint || p is long || p is ulong || p is char || p is string)
                            table.Add(props[i].Name, props[i].GetValue(o, null));
                    }
                }
                catch
                {
                }
            }

            return table;
        }
#if false
        private bool IsNumber(string s)
        {
            int i = 0;
            return int.TryParse(s, out i);
        }
        private bool And(List<string> temp)
        {
            return temp.Count > 0 && temp[0].Equals("&&");
        }
        private bool Or(List<string> temp)
        {
            return temp.Count > 0 && temp[0].Equals("||");
        }
        private void Eat(ref List<string> temp, int count)
        {   // here we protect against an ill-formed statement. Example: "1 == 1 && 2 != 3 &&"
            // ------------------------------------------------------------------------------^- end of string!
            temp.RemoveRange(0, Math.Min(count,temp.Count));
        }
        protected bool QGExpEval(ref List<string> temp, ref int error, ref string failReason)
        {
            object term1 = LookupTerm(temp[0]);
            string eq_op = temp[1];
            object term2 = LookupTerm(temp[2]);
            /* determine the type to convert to
             * The first term with a known type will be the driver type (known property). 
             * We will try to convert the second term to the driver type
             * If neither are known, the user is comparing things which are not an item property.
             *  In this case, we will use the first term to determine the type.
             */
            Type driver = 
                // property values trump all other data types
                m_VariableDatabase.ContainsKey(temp[0]) ? 
                    term1.GetType() :
                m_VariableDatabase.ContainsKey(temp[2]) ? 
                    term2.GetType() : 
                    // I'm guessing here that numbers should trump strings
                IsNumber(temp[0]) ? 
                    typeof(int) :
                IsNumber(temp[1]) ?
                    typeof(int) :
                    // default
                typeof(string);
            bool mct = MakeCompatibleTerms(driver, ref term1, ref term2);
            if (mct == false)
            {
                error = -1;
                failReason = "incompatible terms";
                return false;
            }

            bool exp_result;
            // pick off the first expression
            {
                exp_result = QGExpEval(term1, temp[1], term2, ref error, ref failReason);
                if (error != 0)
                {
                    return false;
                }
                Eat(ref temp, 3);
            }
            /* When to stop processing:
             * A string of AND conditions exits as soon as the first AND fails.
             * A string of OR conditions exits as soon as the first OR passes.
             * We exit by gobbling-up all like operators (ANDs and ORs) once the above is known
             */
            if (And(temp) && exp_result == false)
            {   // exit current AND conditions as soon as truth or falsehood is known
                while (And(temp))
                {
                    Eat(ref temp, 1); // remove the "&&"
                    Eat(ref temp, 3); // remove the expression
                }
            }
            else if (And(temp))
            {
                Eat(ref temp, 1); // remove the "&&"
                exp_result = QGExpEval(ref temp, ref error, ref failReason);
                return exp_result;
            }
            
            if (Or(temp) && exp_result == false)
            {
                Eat(ref temp, 1); // remove the "||"
                exp_result = QGExpEval(ref temp, ref error, ref failReason);
                return exp_result;
            }
            else if (Or(temp) || And(temp))
            {   // exit current OR/AND conditions as soon as truth or falsehood is known
                while (Or(temp) || And(temp))
                {   // see note*
                    Eat(ref temp, 1); // remove the "||" or "&&"
                    Eat(ref temp, 3); // remove the expression
                }
            }

            /* Note*
             * The first true OR condition eats ALL ANDs and ORs when we don't have parenthesis
             * The QG and C# are in agreement in our evaluation.
             * 
                if (false || true || false && true)
                    ; // QG & C# compiler says this statement is true
                else
                    ;

                if (false || true || false && false)
                    ; // QG & C# compiler says this statement is true
                else
                    ;
            *
            */

            return exp_result;
        }
        protected object ConvertToType(Type typeTo, object term)
        {
            if (term.GetType() == typeTo)
                return term;

            var converter = System.ComponentModel.TypeDescriptor.GetConverter(term.GetType());
            try
            {
                object result = null;
                if (converter.CanConvertTo(typeTo))
                    result = converter.ConvertTo(term, typeTo);
                else if (term.GetType() == typeof(string))
                {   // ConvertTo won't convert strings to integral types, so we'll help'em out
                    //  so "1234" would be converted to 1234 and reprocessed to get the correct conversion (maybe signed, unsigned, byte, whatever.)
                    //  "apple" would fail, and you would get the null conversion (since "apple" cannot be converted to an int)
                    Int32 tmp = 0;
                    if (Int32.TryParse(term as string, out tmp))
                        return ConvertToType(typeTo, tmp);
                    // now for bool
                    bool btmp = false;
                    if (bool.TryParse(term as string, out btmp))
                        return ConvertToType(typeTo, btmp);
                }
                return result;
            }
            catch
            {
                return null;
            }
        }
        protected bool MakeCompatibleTerms(Type driver, ref object term1, ref object term2)
        {
            // type conversions we handle
            // sbyte , byte , short , ushort , int , uint , long , ulong , char , and string
            object result = null;

            result = ConvertToType(driver, term1);
            if (result == null)
                return false;
            else
                term1 = result;

            result = ConvertToType(driver, term2);
            if (result == null)
                return false;
            else
                term2 = result;

         return true;
        }
        protected object LookupTerm(string term)
        {   // see if this is a known item property
            //  if so, convert if from string to it's actual type
            if (m_VariableDatabase.ContainsKey(term))
                // this ugliness simply removes the pluralization decoration from the string, example: "apple%s%"
                if (m_VariableDatabase[term].Value.GetType() == typeof(string) && m_VariableDatabase[term].Value is string s && !string.IsNullOrEmpty(s))
                    return s.Split('%')[0];
                else
                    return m_VariableDatabase[term].Value;
            return term;
        }
        protected bool QGExpEval(object term1, string eq_op, object term2, ref int error, ref string failReason)
        {
            switch(eq_op)
            {
                case "==":
                    {
                        if (term1.GetType() == typeof(string) && term2.GetType() == typeof(string))
                            return (term1 as string).Equals(term2 as string, StringComparison.OrdinalIgnoreCase);
                        return term1.Equals(term2);
                    }
                case "!=":
                    {
                        if (term1.GetType() == typeof(string) && term2.GetType() == typeof(string))
                            return !(term1 as string).Equals(term2 as string, StringComparison.OrdinalIgnoreCase);
                        return !term1.Equals(term2);
                    }
                case "<":
                    {
                        if (term1.GetType() == typeof(string) && term2.GetType() == typeof(string))
                            return (term1 as string).CompareTo(term2 as string) < 0;

                        return Comparer.DefaultInvariant.Compare(term1, term2)  < 0;
                    }
                case ">":
                    {
                        if (term1.GetType() == typeof(string) && term2.GetType() == typeof(string))
                            return (term1 as string).CompareTo(term2 as string) > 0;

                        return Comparer.DefaultInvariant.Compare(term1, term2) > 0;
                    }
                case "<=":
                    {
                        int result = 0;
                        if (term1.GetType() == typeof(string) && term2.GetType() == typeof(string))
                            result = (term1 as string).CompareTo(term2 as string);
                        else
                            result = Comparer.DefaultInvariant.Compare(term1, term2);
                        return result <= 0;
                    }
                case ">=":
                    {
                        int result = 0;
                        if (term1.GetType() == typeof(string) && term2.GetType() == typeof(string))
                            result = (term1 as string).CompareTo(term2 as string);
                        else
                            result = Comparer.DefaultInvariant.Compare(term1, term2);
                        return result >= 0;
                    }
                default:
                    error = -1;
                    failReason = "Unknown operator";
                    return false;
            }

        }
        protected string NormalizeProperty(string s1)
        {
            int dmy = 0;
            if (int.TryParse(s1, out dmy))
                // it's a normal int
                return s1;
            
            // now try to parse something like DEADBEEF
            if (int.TryParse(s1, NumberStyles.HexNumber, null, out dmy))
                return dmy.ToString();

            // now try to parse something like 0XDEADBEEF
            if (s1.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                string temp= s1.Substring(2);
                if (int.TryParse(temp, NumberStyles.HexNumber, null, out dmy))
                    return dmy.ToString();
            }
            // not a number
            return s1.ToLower();
        }
#endif
        protected bool DragDropDisposition(Mobile from, Item dropped)
        {
            // what we do with the item dropped
            //  by default we return it execute
            string deleted_error = string.Format("QG Error: But item \"{0}\" had already been deleted.", dropped.GetRawOldName().Split('%')[0]);
            try
            {
                switch (GetVar("disposition"))
                {
                    default:
                    case "keep":
                        if (dropped.Deleted)
                        {
                            if (Owner != null)
                                Owner.SendMessage(deleted_error);
                            return false;
                        }
                        else if (this.Backpack != null && this.Backpack.TryDropItem(from, dropped, false))
                        {   // item placed in my backpack
                            return true;
                        }
                        else
                        {
                            SayTo(from, 503211); // I can't carry any more.
                            return false;
                        }
                    case "delete":
                        if (dropped.Deleted)
                        {
                            if (Owner != null)
                                Owner.SendMessage(deleted_error);
                            return false;
                        }
                        dropped.Delete();
                        return false;

                    case "return":
                        if (dropped.Deleted)
                        {
                            if (Owner != null)
                                Owner.SendMessage(deleted_error);
                            return false;
                        }
                        else
                            return false;
                }
            }
            finally
            {
                UnTrackItem(dropped);
            }
        }

        enum Field
        {
            Name,
            Track,
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
        public override bool HandlesOnSpeech(Mobile from)
        {
            return (from.GetDistanceToSqrt(this) <= 4);
        }
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
        private int GetDatabaseHashCode(Dictionary<string, object[]> db)
        {
            int hash = 0;
            foreach (var kvp in db)
                hash ^= GetKVPHashCode(kvp);
            return hash;
        }
        private int GetDatabaseHashCode(List<KeyValuePair<string, object[]>> db)
        {
            int hash = 0;
            foreach (var kvp in db)
                hash ^= GetKVPHashCode(kvp);
            return hash;
        }
        private int GetKVPHashCode(KeyValuePair<string, object[]> db)
        {
            int hash = db.Key.GetHashCode();
            foreach (var o in db.Value)
                hash ^= o.GetHashCode();
            return hash;
        }
        private bool HasAction(string action)
        {
            foreach (var kvp in m_KeywordDatabase)
                foreach (object o in kvp.Value)
                    if (o is string s)
                        if (s.Equals(action, StringComparison.OrdinalIgnoreCase))
                            return true;

            return false;
        }
        private void LoadVariables(object o)
        {
            // grab a table of key/value pairs that represents the items readable properties
            Dictionary<string, object> kvps = GetKVPPairs(o);
            // add the item's properties to a table of variables
            foreach (var kvp in kvps)
            {
                //if (m_VariableDatabase.ContainsKey(kvp.Key))
                //PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Warning: '{0}' already exists.", kvp.Key), from.NetState);
                SetVar((o is Item ? "item" : "player") + "." + kvp.Key, kvp.Value, VariableInfo.Attributes.ReadOnly | VariableInfo.Attributes.Ephemeral);
            }

            // Type is not available as a property, so we will add it
            SetVar((o is Item ? "item" : "player") + "." + "Type",
                o is Item ? (o as Item).GetType().Name : (o as Mobile).GetType().Name,
                VariableInfo.Attributes.ReadOnly | VariableInfo.Attributes.Ephemeral);
        }
        public override void OnSpeech(SpeechEventArgs e)
        {   // hash all elements of these databases
            int keywordDatabase = GetDatabaseHashCode(m_KeywordDatabase);
            int conditionDatabase = GetDatabaseHashCode(m_ConditionDatabase);

            // do not allow players to interact with a QG under review
            if (Core.ReleasePhase != ReleasePhase.Production || m_needsReview == false || IsOwner(e.Mobile))
                OnSpeechInternal(e);
            else
                Say("Sorry, my script is pending review. Please check back later.");

            // if the user is adding a "dupe" action, or editing a script that has "dupe" we will place it under review
            bool wasNeedingReview = m_needsReview;
            if (HasAction("dupe"))
            {   // if they didn't need a review...
                if (m_needsReview == false)
                {
                    if (keywordDatabase != GetDatabaseHashCode(m_KeywordDatabase))
                    {   // they need one now
                        m_needsReview = true;
                        if (e.Mobile.AccessLevel == AccessLevel.Owner)
                            e.Mobile.SendMessage("keywordDatabase changed");
                        // record the fact that they need a review
                        QGLogger(e.Mobile);
                    }

                    if (conditionDatabase != GetDatabaseHashCode(m_ConditionDatabase))
                    {   // they need one now
                        m_needsReview = true;
                        if (e.Mobile.AccessLevel == AccessLevel.Owner)
                            e.Mobile.SendMessage("conditionDatabase changed");
                        // record the fact that they need a review
                        QGLogger(e.Mobile);
                    }
                }
            }
            else
            {
                // clear the flag if they removed the dupe
                m_needsReview = false;
                if (wasNeedingReview)
                    // record the fact that they no longer need a review
                    QGLogger(e.Mobile);
            }
        }
        private void QGLogger(Mobile from)
        {
            LogHelper logger = new LogHelper(string.Format("QuestGiver({0})Review.log", this.Serial), true, true, true);
            logger.Log(LogType.Text, string.Format("At {0} {1} {2} review. (Owner={3}, From={4})",
                this.Location, this, m_needsReview ? "needs" : "no longer needs", Owner != null ? Owner : "<null>", from != null ? from : "(set via [props)"));
            logger.Finish();
        }
        // parses a statement that looks like: sissy add public|private foo [fox blip bloop]
        private void OnSpeechInternal(SpeechEventArgs e)
        {
            try
            {
                Mobile from = e.Mobile;
                m_From = from;

                if (e.Handled)
                    return;

                // load our memory with the mobile's properties
                LoadVariables(from);

                // normalize syntax - make friendly
                int pcDepth = 0;
                e.Speech = PreCompile(e.Speech, ref pcDepth);

                #region standard processing

                if (e.HasKeyword(0x3F) || (e.HasKeyword(0x174))) // status
                {
                    if (IsOwner(from) || IsHomeOwner(from))
                    {
                        if (Owner != null)
                            SayTo(from, "I am owned by {0}.", from.Name);
                        else
                            SayTo(from, "I am unowned.");
                        e.Handled = true;
                    }
                    else
                    {
                        SayTo(from, "I have nothing to say to you.");
                        e.Handled = true;
                    }
                }
                else if (e.HasKeyword(0x40) || (e.HasKeyword(0x175))) // dismiss
                {
                    if (IsOwner(from) || IsHomeOwner(from))
                    {
                        Dismiss(from);
                        e.Handled = true;
                    }
                }
                else if (e.HasKeyword(0x41) || (e.HasKeyword(0x176))) // cycle
                {
                    if (IsOwner(from) || IsHomeOwner(from))
                    {
                        this.Direction = this.GetDirectionTo(from);
                        e.Handled = true;
                    }
                }
                #endregion

                // compile speech into an array of strings and delimiters
                object[] tokens = Compile(e.Speech);

                // comment?
                if ((tokens[0] as string).StartsWith("#"))
                    return;

                // said my name?
                bool saidName = tokens[0].Equals(Name.ToLower());

                // face them if they talked to me
                if (saidName)
                    this.Direction = this.GetDirectionTo(from);

                // remove the optional handle, i.e., the quest giver's name
                // Distinguishes between zones and Quest Giver NPC�s and TEST CENTER �set� commands
                tokens = RemoveHandle(tokens, Name);

                // nothing to do
                if (tokens.Length == 0)
                    return;

                #region COMMANDS (remember, clear)
                if (e.Handled == false && (tokens[0] as string).ToLower() == "remember")
                {
                    if (IsOwner(from))
                    {
                        e.Handled = true;

                        // okay, process the special OnReceive keyword by prompting to target an item
                        e.Mobile.Target = new OnReceiveTarget(this);
                        e.Mobile.SendMessage("Target the item to remember.");
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "title")
                {
                    if (IsOwner(from))
                    {
                        e.Handled = true;
                        Title = null;
                        from.SendMessage("Title cleared.");
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "labels")
                {
                    if (IsOwner(from))
                    {
                        e.Handled = true;
                        int count = m_ItemDatabase.Count;
                        m_ItemDatabase.Clear();
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} items cleared.", count), from.NetState);
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "keywords")
                {
                    if (IsOwner(from))
                    {
                        e.Handled = true;
                        int count = m_KeywordDatabase.Count;
                        m_KeywordDatabase.Clear();
                        m_PrivateKeywords.Clear();
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} keywords cleared.", count), from.NetState);
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "conditions")
                {
                    if (IsOwner(from))
                    {
                        e.Handled = true;
                        int count = m_ConditionDatabase.Count;
                        m_ConditionDatabase.Clear();
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} conditions cleared.", count), from.NetState);
                    }
                }
                if (e.Handled == false && tokens.Length == 2 && (tokens[0] as string).ToLower() == "clear" && (tokens[1] as string).ToLower() == "variables")
                {
                    if (IsOwner(from))
                    {
                        e.Handled = true;
                        int count = 0;
                        List<string> varsToDelete = new();
                        foreach (var kvp in m_GlobalVariableDatabase)
                        {
                            if (!kvp.Value.GetFlag(VariableInfo.Attributes.System))
                            {
                                count++;
                                varsToDelete.Add(kvp.Key);
                            }
                        }
                        foreach (var var in varsToDelete)
                            m_GlobalVariableDatabase.Remove(var);
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} variables cleared.", count), from.NetState);
                    }
                }
                if (e.Handled == false && (tokens[0] as string).ToLower() == "reset")
                {
                    if (IsOwner(from))
                    {
                        e.Handled = true;
                        Reset(from);
                    }
                }
                if (e.Handled == false && (tokens[0] as string).ToLower() == "dumpscript")
                {   // recreate the script and dump it to disk so that it may be reviewed
                    if (from.AccessLevel == AccessLevel.Owner)
                    {
                        e.Handled = true;
                        LogHelper logger = new LogHelper(string.Format("QuestGiver({0}).log", this.Serial), true, true, true);

                        // list the label declarations
                        logger.Log(LogType.Text, string.Format("##\n## Label Declarations\n##"));
                        foreach (var kvp in m_KeywordDatabase)
                        {
                            bool isPrivate = m_PrivateKeywords.Contains(kvp.Key);
                            logger.Log(LogType.Text, string.Format("add {0} {1}", (isPrivate) ? "private" : "public", kvp.Key));
                        }
                        // list the conditions
                        logger.Log(LogType.Text, string.Format("##\n## Conditions\n##"));
                        foreach (var kvp in m_ConditionDatabase)
                        {
                            logger.Log(LogType.Text, string.Format("condition {0} {1}", kvp.Key, MakeString(from, kvp.Value, 0)));
                        }
                        // list the actions
                        logger.Log(LogType.Text, string.Format("##\n## Actions\n##"));
                        foreach (var kvp in m_KeywordDatabase)
                        {
                            string text = MakeString(from, kvp.Value, 0);
                            text = string.IsNullOrEmpty(text) ? "<null>" : text;
                            string[] lines = text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (string line in lines)
                                logger.Log(LogType.Text, string.Format("action {0} {1}", kvp.Key, line));
                        }

                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Done. {0} lines written.", logger.Count), from.NetState);
                        logger.Finish();
                    }
                }
                if (e.Handled == false && (tokens[0] as string).ToLower() == "vartest")
                {   // test our new variable system
                    if (from.AccessLevel == AccessLevel.Owner)
                    {
                        e.Handled = true;
                        string failReason = string.Empty;
                        SetVar("some_number", 9999, VariableInfo.Attributes.None, ref failReason);
                        object o = GetVar("some_number");

                        SetVar("system_var", "I am a system variable", VariableInfo.Attributes.System, ref failReason);
                        o = GetVar("system_var");
                        ; /* debug, check o here */

                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Done."), from.NetState);
                    }
                }
                if (e.Handled == false && (tokens[0] as string).ToLower() == "runconditiontests")
                {   // recreate the script and dump it to disk so that it may be reviewed
                    if (from.AccessLevel == AccessLevel.Owner)
                    {
                        e.Handled = true;
                        RunConditionTests(from);
                    }
                }
                if (e.Handled == false && (tokens[0] as string).ToLower() == "expressionevaluator")
                {
                    if (from.AccessLevel == AccessLevel.Owner)
                    {
                        e.Handled = true;
                        ExpressionEvaluator evaluator = new ExpressionEvaluator();
                        string script = "if ( memory + 1 > 25) return \"success\"; else return \"fail\";";
                        ;
                        string[] str_tokens = script.Split(' ');
                        foreach (var str_token in str_tokens)
                        {
                            Dictionary<string, VariableInfo> db = SelectDatabase(from);
                            if (db == null)
                                continue;

                            if (db.ContainsKey(str_token))
                                script = script.Replace(str_token, db[str_token].Value.ToString());
                        }

                        object o = evaluator.ScriptEvaluate(script);
                        ; /* debug: check o here */
                    }
                }
                #endregion

                #region GET & SET
                // process owner programming commands GET & SET
                if (e.Handled == false && IsOwner(from))
                {
                    if (tokens[0] is string && (tokens[0] as string).ToLower() == "get")
                    {   // does it look like a reasonable command?
                        if (tokens.Length > 1 && tokens[1] is string)
                        {
                            e.Handled = true;
                            switch ((tokens[1] as string).ToLower())
                            {
                                case "distance":
                                    from.SendMessage("distance is set to {0} tiles.", (int)GetVar("distance"));
                                    break;

                                case "memory":
                                    from.SendMessage("Memory set to {0} minutes.", (int)GetVar("memory"));
                                    break;
                            }
                        }
                    }

                    if (tokens[0] is string && (tokens[0] as string).ToLower() == "set")
                    {   // does it look like a reasonable command?
                        if (tokens.Length > 2 && tokens[1] is string && tokens[2] is string)
                        {
                            e.Handled = true;
                            List<string> temp = tokens.Select(i => i.ToString()).ToList();
                            temp.RemoveRange(0, 2);
                            int error = 0;
                            object exp_result = null;
                            string failReason = string.Empty;

                            object o = ExpressionRun(from, temp, ref error, ref failReason);
                            if (o != null)
                                exp_result = o.ToString();

                            if (error != 0)
                            {
                                if (m_Owner != null)
                                    m_Owner.SendMessage(failReason);
                            }
                            else
                                switch ((tokens[1] as string).ToLower())
                                {
                                    case "name":
                                        {
                                            // Pattern match for invalid characters

                                            string text = exp_result as string;
                                            if (InvalidNamePatt.IsMatch(text))
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
                                            //else
                                            //from.SendMessage("Usage: set name <name string>");
                                        }
                                        break;

                                    case "title":
                                        {
                                            // Pattern match for invalid characters
                                            string text = exp_result as string;
                                            if (InvalidNamePatt.IsMatch(text))
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
                                            //else
                                            //from.SendMessage("Usage: set title <name string>");
                                        }
                                        break;

                                    case "distance":
                                        {
                                            int result;
                                            // max 12 tiles
                                            string text = exp_result as string;
                                            if (int.TryParse(text, out result) && result >= 0 && result < 12)
                                            {
                                                SetVar("distance", result, VariableInfo.Attributes.System | VariableInfo.Attributes.Serialize);
                                                from.SendMessage("Set.");
                                            }
                                            else
                                                from.SendMessage("Usage: set distance <number>");
                                        }
                                        break;

                                    case "memory":
                                        {
                                            double result;
                                            // max 72 hours
                                            string text = exp_result as string;
                                            if (double.TryParse(text, out result) && result > 0 && result < TimeSpan.FromHours(72).TotalMinutes)
                                            {
                                                SetVar("memory", result, VariableInfo.Attributes.System | VariableInfo.Attributes.Serialize);
                                                m_PlayerMemory = new Memory();
                                                from.SendMessage("Set.");
                                            }
                                            else
                                                from.SendMessage("Usage: set memory <number>");
                                        }
                                        break;

                                    default:
                                        {
                                            SetVar((tokens[1] as string).ToLower(), o, VariableInfo.Attributes.None);
                                            from.SendMessage("Set.");
                                            break;
                                        }
                                }
                        }
                    }
                }
                #endregion

                #region owner programming commands (keywords)
                // process owner programming commands
                if (e.Handled == false && IsOwner(from))
                {
                    // we now have a compiled list of strings and tokens
                    // find out what the user is constructing
                    //
                    // if the user is inserting a keyword, make sure it doesn't already exist and insert it
                    // if the user is adding verbs, append to named keyword

                    // if the user is inserting a keyword, make sure it doesn't already exist and insert it
                    if (tokens[0] is string && ((tokens[0] as string).ToLower() == "keyword" || (tokens[0] as string).ToLower() == "add"))
                    {
                        var match_add = AddLabel.Match(MakeString(from, tokens, 0));
                        if (!match_add.Success)
                        {
                            // general format error
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' private|public keyword1 [keyword2 keyword3 ...].", tokens[0] as string), from.NetState);
                            return;
                        }

                        bool private_keyword = false;
                        string found;
                        if (tokens[1].ToString().ToLower() == "private")
                            private_keyword = true;
                        else if (AdminKeyword(tokens, 2, out found))
                        {
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("'{0}' is an administrative keyword and cannot be made public.", found), from.NetState);
                            // force to private
                            private_keyword = true;
                        }

                        // remove scope specifier (no array remove element?)
                        List<object> temp = new List<object>(tokens);
                        temp.RemoveAt(1);
                        tokens = temp.ToArray();

                        // does it look like a reasonable command?
                        if (tokens.Length > 1 && tokens[1] is string)
                        {
                            e.Handled = true;

                            string good, bad;
                            bool ck = ComputeKeywords(tokens, 1, false, out good, out bad);

                            if (!ck)
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keyword(s) are already defined: {0}", bad), from.NetState);
                            else if (m_KeywordDatabase.Count >= 256)
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Your keyword database is full."), from.NetState);
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
                                    {
                                        m_KeywordDatabase[(kwords[ix] as string).ToLower()] = actions;
                                        if (private_keyword)
                                            m_PrivateKeywords.Add((kwords[ix] as string).ToLower());
                                    }
                                }

                                // OPTIONAL: extract the actions
                                for (int ix = 1; ix < chunks.Length; ix++)
                                {
                                    object[] action = chunks[ix];
                                    // these are the actions
                                    OnSpeech(new SpeechEventArgs(from, "action " + keyword + " " + MakeString(from, action, 0), MessageType.Regular, SpeechHue, new int[0]));
                                }

                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
                            }
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' private|public keyword1 [keyword2 keyword3 ...].", tokens[0] as string), from.NetState);
                    }
                    else if (tokens[0] is string && ((tokens[0] as string).ToLower() == "action" || (tokens[0] as string).ToLower() == "verb"))
                    {
                        if (tokens.Length > 2 && tokens[1] is string && tokens[2] is string)
                        {
                            e.Handled = true;
                            // look for access control violations
                            if (CheckAccess(tokens))
                            {
                                if (m_KeywordDatabase.ContainsKey((tokens[1] as string).ToLower()))
                                {   // we have all the named keywords
                                    // locate the verb
                                    if (m_VerbDatabase.Contains((tokens[2] as string).ToLower()))
                                    {
                                        // append the verb list to any existing verb list
                                        object[] verbList = m_KeywordDatabase[(tokens[1] as string).ToLower()]; // the action for keyword - never null
                                        object[] action;                                // the new action

                                        if (MakeString(from, tokens, 0).Length + MakeString(from, verbList, 0).Length > 768)
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Action too long for this keyword."), from.NetState);
                                        else
                                        {
                                            // before we build the action, some verbs need validated arguments here at compile time.
                                            // we will do that here
                                            if (!ValidateVerbArgs(tokens[2], MakeString(from, tokens, 3)))
                                            {
                                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Invalid argument \"{0}\" for verb '{1}'.", MakeString(from, tokens, 3), tokens[2]), from.NetState);
                                            }
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
                                                foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
                                                {
                                                    if (kvp.Value == verbList)
                                                    {   // this keyword has one of the shared actions
                                                        tomod.Add(kvp.Key);
                                                    }
                                                }
                                                foreach (string sx in tomod)
                                                    m_KeywordDatabase[sx] = action;

                                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
                                            }
                                        }
                                    }
                                    else
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I do not know the verb {0}.", tokens[2] as string), from.NetState);
                                }
                                else
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I do not have the keyword(s) {0}.", tokens[1] as string), from.NetState);
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Only a GameMaster owned NPC may use the {0} command.", "dupe"), from.NetState);
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' 'keyword' 'verb' text.", tokens[0] as string), from.NetState);
                    }
                    else if (tokens[0] is string && ((tokens[0] as string).ToLower() == "condition"))
                    {
                        // looks like: condition onreceive type == shovel SUCCESS else FAIL
                        if (tokens.Length > 2/*(tokens.Length == 6  || tokens.Length == 8) && tokens[1] is string && tokens[2] is string*/)
                        {
                            e.Handled = true;
                            // you can create a condition to branch to a label (keyword)
                            //  -and-
                            // you can create a condition to establish a disposition (keep, delete, return)
                            if (m_KeywordDatabase.ContainsKey((tokens[1] as string).ToLower())/* || m_VerbDatabase.Contains((tokens[1] as string).ToLower())*/)
                            {   // we have all the named keywords
                                if ((tokens[1] as string).ToLower().Equals("onreceive") /*|| (tokens[1] as string).ToLower().Equals("give") || (tokens[1] as string).ToLower().Equals("dupe")*/)
                                {
                                    List<string> bad_labels = new();
                                    if (ParseCondition(e.Speech, out object[] condition, bad_labels))
                                    {
                                        object[] elements = new object[condition.Length - 2];
                                        Array.ConstrainedCopy(condition, 2, elements, 0, condition.Length - 2);
                                        m_ConditionDatabase.Add(new KeyValuePair<string, object[]>(tokens[1] as string, elements));

                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
                                    }
                                    else
                                    {
                                        if (bad_labels.Count > 0)
                                        {
                                            foreach (string label in bad_labels)
                                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I do not have the keyword(s) {0}.", label), from.NetState);
                                        }
                                        else
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Bad format for condition '{0}'.", e.Speech), from.NetState);
                                    }
                                }
                                else
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I do not have the term {0}.", tokens[1] as string), from.NetState);
                                // let's parse the condition.

                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I do not have the keyword(s) {0}.", tokens[1] as string), from.NetState);
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' 'keyword' 'verb' text.", tokens[0] as string), from.NetState);

                    }
                    else if (tokens.Length == 1 && tokens[0] is string && (tokens[0] as string).ToLower() == "list")
                    {   // list everything
                        OnSpeech(new SpeechEventArgs(from, "list keywords", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                        OnSpeech(new SpeechEventArgs(from, "list labels", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                        OnSpeech(new SpeechEventArgs(from, "list conditions", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                        OnSpeech(new SpeechEventArgs(from, "list variables", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                        OnSpeech(new SpeechEventArgs(from, "list player variables", Server.Network.MessageType.Regular, 0x3B2, new int[0], false));
                    }
                    else if (tokens.Length >= 3 &&
                        tokens[0] is string && (tokens[0] as string).ToLower() == "list" &&
                        tokens[1] is string && (tokens[1] as string).ToLower() == "player" &&
                        tokens[2] is string && (tokens[2] as string).ToLower() == "variables")
                    {   // list the variables

                        if (m_PlayerVariableDatabase.Count > 0)
                        {
                            foreach (var kvp in m_PlayerVariableDatabase)
                            {
                                foreach (var kvp_inner in kvp.Value)
                                {
                                    // don't list item and player variables - too many!
                                    if (kvp_inner.Value.GetFlag(VariableInfo.Attributes.Ephemeral))
                                        continue;

                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false,
                                        string.Format("{0} = {1}{2}",
                                        kvp_inner.Key,
                                        kvp_inner.Value.Value is string ? "\"" + kvp_inner.Value.Value + "\"" : kvp_inner.Value.Value,
                                        kvp_inner.Value.GetFlag(VariableInfo.Attributes.System) ? " (System)" : ""),
                                        from.NetState);
                                }
                            }
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("There are no variables defined."), from.NetState);
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "list" && tokens[1] is string && (tokens[1] as string).ToLower() == "variables")
                    {   // list the variables

                        if (m_GlobalVariableDatabase.Count > 0)
                        {
                            foreach (KeyValuePair<string, VariableInfo> kvp in m_GlobalVariableDatabase)
                            {   // don't list item and player variables - too many!
                                if (kvp.Value.GetFlag(VariableInfo.Attributes.Ephemeral))
                                    continue;

                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false,
                                    string.Format("{0} = {1}{2}",
                                    kvp.Key,
                                    kvp.Value.Value is string ? "\"" + kvp.Value.Value + "\"" : kvp.Value.Value,
                                    kvp.Value.GetFlag(VariableInfo.Attributes.System) ? " (System)" : ""),
                                    from.NetState);
                            }
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("There are no variables defined."), from.NetState);
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "list" && tokens[1] is string && (tokens[1] as string).ToLower() == "labels")
                    {   // list the labels

                        if (m_ItemDatabase.Count > 0)
                        {
                            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                            {
                                if (kvp.Key == null)
                                    continue;

                                string where = GetItemLocation(kvp.Key);
                                if (kvp.Key.RootParent == this)
                                    where = "in my backpack";
                                else if (World.FindItem(kvp.Key.Serial) == null)
                                    where = "location unknown";

                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0} [{1}]", GetField(kvp.Value, Field.Name), where), from.NetState);
                            }
                        }
                        else
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("There are no labels defined."), from.NetState);
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "list" && tokens[1] is string && (tokens[1] as string).ToLower() == "keywords")
                    {   // list the keywords specified
                        if (tokens.Length > 2 && tokens[2] is string)
                        {   // loop over all of the keywords and delete it if it exists.
                            e.Handled = true;

                            string good, bad;
                            bool ck = ComputeKeywords(tokens, 2, true, out good, out bad);

                            if (!ck)
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keyword(s) are not defined: {0}", bad), from.NetState);

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
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keywords: {0}", aliases), from.NetState);

                                    if (m_KeywordDatabase[aliases_array[0]].Length == 0)
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Have associated actions: {0}.", "<null>"), from.NetState);
                                    else
                                    {
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Have associated actions:"), from.NetState);
                                        object[][] actions = SplitArray(m_KeywordDatabase[aliases_array[0]], '|');
                                        foreach (object[] action in actions)
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0}", MakeString(from, action, 0)), from.NetState);
                                    }
                                }
                            }
                        }
                        else
                        {   // list all keywords and associated actions
                            if (m_KeywordDatabase.Count > 0)
                            {
                                List<string> good_list = new List<string>();
                                foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
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
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keywords: {0}", aliases), from.NetState);

                                        if (m_KeywordDatabase[aliases_array[0]].Length == 0)
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Have associated actions: {0}.", "<null>"), from.NetState);
                                        else
                                        {
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Have associated actions:"), from.NetState);
                                            object[][] actions = SplitArray(m_KeywordDatabase[aliases_array[0]], '|');
                                            foreach (object[] action in actions)
                                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0}", MakeString(from, action, 0)), from.NetState);
                                        }
                                    }
                                }
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("There are no keywords defined."), from.NetState);
                        }
                    }
                    else if (tokens.Length >= 2 && tokens[0] is string && (tokens[0] as string).ToLower() == "list" && tokens[1] is string && (tokens[1] as string).ToLower() == "conditions")
                    {   // list the keywords specified
                        if (tokens.Length > 2 && tokens[2] is string)
                        {   // loop over all of the conditions and display it if it exists.
                            e.Handled = true;

                            string good, bad;
                            bool ck = ComputeConditions(tokens, 2, true, out good, out bad);

                            if (!ck)
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keyword(s) are not defined: {0}", bad), from.NetState);

                            string[] good_array, bad_array;
                            ComputeConditions(tokens, 2, true, out good_array, out bad_array);

                            List<string> remember = new List<string>();
                            foreach (string sx in good_array)
                            {
                                foreach (var condition in m_ConditionDatabase)
                                    if (condition.Key == sx)
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0}: {1}", condition.Key, MakeString(from, condition.Value, 0)), from.NetState);
                            }
                        }
                        else
                        {   // list all keywords and associated actions
                            if (m_ConditionDatabase.Count > 0)
                            {
                                List<string> good_list = new List<string>();
                                foreach (KeyValuePair<string, object[]> kvp in m_ConditionDatabase)
                                    good_list.Add(kvp.Key);

                                string[] good_array = good_list.ToArray();
                                foreach (string sx in good_array)
                                {
                                    foreach (var condition in m_ConditionDatabase)
                                        if (condition.Key == sx)
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0}: {1}", condition.Key, MakeString(from, condition.Value, 0)), from.NetState);
                                }
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("There are no conditions defined."), from.NetState);
                        }
                    }
                    else if (tokens.Length >= 3 && tokens[0] is string && (tokens[0] as string).ToLower() == "delete")
                    {
                        if ((tokens[1] as string).ToLower() == "label")
                        {
                            if (tokens.Length > 2 && tokens[2] is string)
                            {   // loop over all of the labels and delete it if it exists.
                                e.Handled = true;

                                List<Item> list = new List<Item>();
                                foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                                {
                                    if ((GetField(kvp.Value, Field.Name) as string).ToLower() == (tokens[2] as string).ToLower())
                                        list.Add(kvp.Key);
                                }
                                foreach (Item ix in list)
                                    m_ItemDatabase.Remove(ix);

                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0} '{1}' labels cleared.", list.Count, tokens[2] as string), from.NetState);
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' label <label>.", tokens[0] as string), from.NetState);
                        }
                        else if ((tokens[1] as string).ToLower() == "keyword")
                        {
                            if (tokens.Length > 2 && tokens[2] is string)
                            {   // loop over all of the keywords and delete it if it exists.
                                e.Handled = true;

                                string good, bad;
                                bool ck = ComputeKeywords(tokens, 2, true, out good, out bad);

                                if (!ck)
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keyword(s) are not defined: {0}", bad), from.NetState);

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
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following keywords have been deleted: {0}", aliases), from.NetState);

                                        // now remove each keyword and alias
                                        foreach (string dx in aliases_array)
                                        {
                                            m_KeywordDatabase.Remove(dx);
                                            if (m_PrivateKeywords.Contains(dx))
                                                m_PrivateKeywords.Remove(dx);
                                        }

                                        // now remove corresponding conditions for that keyword
                                        if (m_ConditionDatabase.FindIndex(x => x.Key.Equals(sx, StringComparison.OrdinalIgnoreCase)) != -1)
                                            // tell the user what we are deleting (all conditions are deleted with the keyword)
                                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("The following conditions have been deleted:"), from.NetState);

                                        foreach (var condition in m_ConditionDatabase)
                                            if (condition.Key.Equals(sx, StringComparison.OrdinalIgnoreCase))
                                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0}", MakeString(from, condition.Value, 0)), from.NetState);

                                        m_ConditionDatabase.RemoveAll(x => x.Key.Equals(sx, StringComparison.OrdinalIgnoreCase));
                                    }
                                }
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' keyword1[, keyword2, keyword3].", tokens[0] as string), from.NetState);
                        }
                        else if ((tokens[1] as string).ToLower() == "variable")
                        {
                            if (tokens.Length > 2 && tokens[2] is string)
                            {   // loop over all of the keywords and delete it if it exists.
                                e.Handled = true;
                                VariableInfo inf = GetVarInfo((tokens[2] as string).ToLower().Trim());
                                if (inf == null)
                                {
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("'{0}' not found.", tokens[2] as string), from.NetState);
                                }
                                else if (inf.GetFlag(VariableInfo.Attributes.System))
                                {
                                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("'{0}' is a system variable.", tokens[2] as string), from.NetState);
                                }
                            }
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Usage: '{0}' variable.", tokens[0] as string), from.NetState);
                        }
                    }
                }
                #endregion

                // anyone talking - process keywords
                if (e.Handled == false)
                {
                    string match;
                    if (FindKeyPhrase(from, tokens, 0, out match) || FindKeyword(tokens, 0, out match) && m_KeywordDatabase[match].Length > 0)
                    {
                        e.Handled = true;
                        // execute the verb for this keyword

                        // do not allow standard players to access internal commands like 'OnEnter'
                        //	When e.Internal is true, it's the NPC dispatching the keyword and will be allowed
                        string pkw;
                        if ((AdminKeyword(tokens, 0, out pkw) || PrivateKeyword(tokens, 0, out pkw)) && !(IsOwner(from) || e.Internal))
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I'm sorry. You do not have access the {0} command.", pkw), from.NetState);
                        else
                        {
                            // begin execute 
                            object[][] actions = SplitArray(m_KeywordDatabase[match], '|');

                            int depth = 0;
                            try
                            {   // look for access control violations
                                if (CheckAccess(actions))
                                    ExecuteActions(from, actions, ref depth);
                                else
                                    from.SendMessage(0x35, string.Format("Only a GameMaster owned NPC may execute the {0} command.", "dupe"));
                            }
                            catch
                            {
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Excessive recursion detected for keyword ({0}).", match), from.NetState);
                            }
                        }
                        // end execute
                    }
                    // else we don't recognize what was said, so we will simply ignore the player.
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            /* Security section.
             * In this finally block, we 
             */
            finally
            {

            }
        }

        private void ErrorMessage(string message)
        {
            if (Owner != null)
                Owner.SendMessage(message);
        }
        private object EvaluateSctipt(Mobile m, string script)
        {
            ExpressionEvaluator evaluator = new ExpressionEvaluator();
            if (!script.EndsWith(';'))
                script += ';';
            string[] str_tokens = script.Split(' ');
            foreach (var str_token in str_tokens)
            {
                Dictionary<string, VariableInfo> db = SelectDatabase(m);
                if (db == null)
                    continue;

                if (db.ContainsKey(str_token))
                    script = script.Replace(str_token, db[str_token].Value.ToString());
            }

            object o = evaluator.ScriptEvaluate(script);
            return o;
        }
        private string MakeKey(string text)
        {
            if (text.Contains(' '))
                return "\"" + text + "\"";
            return text;
        }
        private bool ValidateVerbArgs(object o, string args)
        {
            if (o is string s && s.ToLower().Equals("play", StringComparison.OrdinalIgnoreCase))
            {
                if (Engines.PlaySoundEffect.ValidID(args.Trim()))
                    return true;
                return false;
            }

            return true;
        }
        protected bool CheckAccess(object[][] actions)
        {
            // handle access controlled actions like 'dupe'
            foreach (var check in actions)
                if (AccessControlledToken(check, "dupe") && !OwnerAccess(AccessLevel.GameMaster))
                    return false;
            return true;
        }
        protected bool CheckAccess(object[] actions)
        {
            // handle access controlled actions like 'dupe'
            if (AccessControlledToken(actions, "dupe") && !OwnerAccess(AccessLevel.GameMaster))
                return false;
            return true;
        }
        protected bool OwnerAccess(AccessLevel minAccessLevel)
        {
            if (Owner != null && Owner.AccessLevel >= minAccessLevel)
                return true;
            return false;
        }
        protected bool AccessControlledToken(object[] tokens, string token)
        {
            foreach (object o in tokens)
                if (o is string s && s.Equals(token, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }
        // parses a statement that looks like:
        // sissy condition onreceive OldName == shovel SUCCESS else FAIL
        // -or-
        // sissy condition onreceive OldName == shovel SUCCESS
        private int IndexOf(object[] objects, string token)
        {
            for (int index = 0; index < objects.Length; index++)
                if (objects[index] is string s)
                    if (s.Equals(token, StringComparison.OrdinalIgnoreCase))
                        return index;
            return -1;
        }
        private string GetChunk(string temp, Regex exp)
        {
            var match_condition = exp.Match(temp);
            if (!match_condition.Success)
                return null;

            return match_condition.Value;
        }
        private string[] GetChunks(string temp, Regex exp)
        {
            var match_condition = exp.Match(temp);
            if (!match_condition.Success)
                return null;

            List<string> chunks = new List<string>();

            for (int ix = 1; ix < match_condition.Groups.Count; ix++)
                chunks.Add(match_condition.Groups[ix].Value);

            return chunks.ToArray();
        }
        private string RemoveChunk(string temp, Regex exp)
        {
            return exp.Replace(temp, "", 1).Trim();
        }

        protected bool ParseCondition(string text, out object[] condition, List<string> bad_labels)
        {
            List<string> list = new();
            condition = list.ToArray();

            // remove the NPC name
            if (text.StartsWith(Name, StringComparison.OrdinalIgnoreCase))
                text = text.Substring(Name.Length);

            text = text.Trim();

            // get a modifiable copy
            string temp = text;
            string chunk;

            chunk = GetChunk(temp, new Regex("condition"));
            if (chunk != null)
            {
                list.Add(chunk);
                temp = RemoveChunk(temp, new Regex("condition"));
            }
            else
                return false;

            chunk = GetChunk(temp, new Regex("onreceive|give"));
            if (chunk != null)
            {
                list.Add(chunk);
                temp = RemoveChunk(temp, new Regex("onreceive|give"));
            }
            else
                return false;

            string[] chunks;
            chunks = GetChunks(temp, TokenOpToken);
            if (chunks.Length == 3)
            {
                foreach (string s in chunks)
                    list.Add(s);
                temp = RemoveChunk(temp, TokenOpToken);
            }
            else
                return false;

            while ((chunk = GetChunk(temp, LogicalOps)) != null)
            {
                list.Add(chunk.Trim());
                temp = RemoveChunk(temp, LogicalOps);

                chunks = GetChunks(temp, TokenOpToken);
                if (chunks.Length == 3)
                {
                    foreach (string s in chunks)
                        list.Add(s.Replace("\"", "").Trim());
                    temp = RemoveChunk(temp, TokenOpToken);
                }
                else
                    return false;
            }
            // get the success label
            chunk = GetChunk(temp, PCLabel);
            if (chunk != null)
            {
                list.Add(chunk.Trim());
                temp = RemoveChunk(temp, new Regex(chunk.Trim()));
                // okay, check the last keyword to make sure it's valid
                if (!m_KeywordDatabase.ContainsKey(list[list.Count - 1].ToLower()))
                    bad_labels.Add(list[list.Count - 1]);
            }
            else
                return false;

            // see if there is an 'else' clause
            chunk = GetChunk(temp, new Regex(@"else"));
            if (chunk != null)
            {
                list.Add(chunk.Trim());
                temp = RemoveChunk(temp, new Regex(chunk.Trim()));
            }
            else
            {   // we're done!
                condition = list.ToArray();
                return bad_labels.Count == 0;
            }

            // get the fail label
            chunk = GetChunk(temp, PCLabel);
            if (chunk != null)
            {
                list.Add(chunk.Trim());
                temp = RemoveChunk(temp, new Regex(chunk.Trim()));
                // okay, check the last keyword to make sure it's valid
                if (!m_KeywordDatabase.ContainsKey(list[list.Count - 1].ToLower()))
                    bad_labels.Add(list[list.Count - 1]);

                // now we're really done!
                condition = list.ToArray();
                return bad_labels.Count == 0;
            }
            else
                return false;
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
                if (m_KeywordDatabase.ContainsKey(list[ix]))
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
                        // see if the user already has already been "given" the thing, and if so, branch
                        //  see also "has"
                        case "given":
                            {   // Has <item_label> (branch to) <keyword>
                                if (action.Length == 3 && action[1] is string && action[2] is string)
                                {
                                    Memory.ObjectMemory om = m_PlayerMemory.Recall(from as object);
                                    // look to see if the player was given one of these yet
                                    if (om != null && om.Context != null)
                                    {
                                        if ((om.Context as List<string>).Contains((action[1] as string).ToLower()))
                                        {
                                            if (m_KeywordDatabase.ContainsKey((action[2] as string).ToLower()))
                                            {   // branch to this label and execute
                                                object[][] branch = SplitArray(m_KeywordDatabase[(action[2] as string).ToLower()], '|');
                                                ExecuteActions(from, branch, ref depth);
                                                return; // we're done now.
                                            }
                                            else
                                                from.SendMessage(string.Format("While executing 'has' label {0} not found.", (action[2] as string).ToLower()));
                                        }
                                    }
                                    else
                                        Console.WriteLine("ExecuteActions: <has> Context is null");
                                }
                                else
                                    from.SendMessage(string.Format("Usage: has <label> <keyword>."));

                            }
                            break;

                        // see if the user physically "has" the thing, and if so, branch
                        //  see also "given"
                        case "has":
                            {   // Has <item_label> (branch to) <keyword>
                                if (action.Length == 3 && action[1] is string && action[2] is string)
                                {   // look in the players back for the item with "label"
                                    if (CheckBackpack(from, (action[1] as string).ToLower()))
                                    {
                                        if (m_KeywordDatabase.ContainsKey((action[2] as string).ToLower()))
                                        {   // branch to this label and execute
                                            object[][] branch = SplitArray(m_KeywordDatabase[(action[2] as string).ToLower()], '|');
                                            ExecuteActions(from, branch, ref depth);
                                            return; // we're done now.
                                        }
                                        else
                                            from.SendMessage(string.Format("While executing 'has' label {0} not found.", (action[2] as string).ToLower()));
                                    }
                                }
                                else
                                    from.SendMessage(string.Format("Usage: has <label> <keyword>."));

                            }
                            break;

                        case "random":
                            {
                                string[] keys = KeywordList(TokenList(TokenList(action, 1)));
                                if (keys.Length > 0)
                                {
                                    actions[ix] = m_KeywordDatabase[keys[Utility.Random(keys.Length)]];
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
                                foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
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

                        case "keep":
                            if (GetVar("dropped") != null)  // 'dropped' ensures we are in the context of "onreceive" (OnDragDrop)
                                SetVar("disposition", "keep", VariableInfo.Attributes.System);
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Script Error: nothing to {0}.", (action[0] as string).ToLower()), m_From.NetState);
                            break;
                        case "return":
                            if (GetVar("dropped") != null)  // 'dropped' ensures we are in the context of "onreceive" (OnDragDrop)
                                SetVar("disposition", "return", VariableInfo.Attributes.System);
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Script Error: nothing to {0}.", (action[0] as string).ToLower()), m_From.NetState);
                            break;
                        case "delete":
                            if (GetVar("dropped") != null)  // 'dropped' ensures we are in the context of "onreceive" (OnDragDrop)
                                SetVar("disposition", "delete", VariableInfo.Attributes.System);
                            else
                                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Script Error: nothing to {0}.", (action[0] as string).ToLower()), m_From.NetState);
                            break;

                        case "emote":
                            Emote("*" + MakeString(from, action, 1, true) + "*");
                            break;
                        case "sayto":
                            SayTo(m_From, MakeString(from, action, 1, true));
                            break;
                        case "say":
                            Say(MakeString(from, action, 1, true));
                            break;
                        case "play":
                            Engines.PlaySoundEffect.Play(this, MakeString(from, action, 1));
                            break;
                        case "dupe":
                        case "give":
                            {
                                bool known = false;
                                bool given = false;
                                string name = null;
                                foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                                {
                                    if (action[1] is string)
                                    {
                                        name = GetField(kvp.Value, Field.Name) as string;
                                        if (name.ToLower() == (action[1] as string).ToLower())
                                        {   // I had one at one time
                                            known = true;
                                            if (kvp.Key != null && kvp.Key.RootParent == this)
                                            {   // I have one now
                                                if (Backpack != null && from.Backpack != null)
                                                {
                                                    Item item = kvp.Key;
                                                    if ((action[0] as string).ToLower() == "dupe")
                                                        item = Utility.Dupe(kvp.Key);
                                                    else
                                                        Backpack.RemoveItem(item);
                                                    if (!from.Backpack.TryDropItem(from, item, false))
                                                    {
                                                        this.SayTo(from, 503204);                       // You do not have room in your backpack for this.
                                                        item.MoveToWorld(from.Location, from.Map);
                                                    }
                                                    given = true;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                if (given == false)
                                {
                                    if (known == false)
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I know nothing about a {0}.", action[1]), from.NetState);
                                    else
                                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I'm sorry. I no longer have a {0}.", action[1]), from.NetState);

                                    // no more actions if we fail the give
                                    done = true;
                                    break;
                                }
                                else
                                {
                                    if (m_PlayerMemory.Recall(from) == false)
                                    {
                                        // we havn't seen this player yet
                                        object memory = GetVar("memory");
                                        m_PlayerMemory.Remember(from, TimeSpan.FromSeconds((double)(memory == null ? 30 : memory) * 60).TotalSeconds);   // remember him for this long (*60 convert to minutes)
                                    }
                                    else
                                        // refresh our memory of this player
                                        m_PlayerMemory.Refresh(from);

                                    // extract the context object
                                    Memory.ObjectMemory om = m_PlayerMemory.Recall(from as object);
                                    if (om.Context == null)
                                        om.Context = new List<string>();

                                    // now remember what was given to this player
                                    (om.Context as List<string>).Add(name.ToLower());
                                }

                            }
                            break;

                        case "terminate":
                            Reset();
                            break;
                        case "break":
                            SetVar("break", true, VariableInfo.Attributes.ReadOnly | VariableInfo.Attributes.System);
                            break;
                        case "set":
                            {
                                if (action.Length >= 4)
                                {
                                    object existing = GetVar(action[1] as string, from);
                                    if (existing == null)
                                    {   // we will create and seed this variable with a default value
                                        // if it looks like a string, give it a "" value. If a number, a 0 value
                                        if (MakeString(from, action, 2).Contains('"'))
                                            SetVar(action[1] as string, "", VariableInfo.Attributes.Serialize, from);
                                        else
                                            SetVar(action[1] as string, 0, VariableInfo.Attributes.Serialize, from);
                                    }

                                    object o = EvaluateSctipt(from, MakeString(from, action, 2));
                                    if (o != null)
                                        SetVar(action[1] as string, o, VariableInfo.Attributes.Serialize, from);
                                    else
                                        ErrorMessage(string.Format("Invalid expression '{0}'", MakeString(from, action, 2)));
                                }
                                else
                                    ErrorMessage(string.Format("General format error '{0}'", MakeString(from, action, 0)));
                            }
                            break;
                        default:
                            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("{0} is an unknown verb.", action[0]), from.NetState);
                            break;
                    }
                }
            }
        }
        private void Reset()
        {
            Reset(null);
        }
        private void Reset(Mobile from)
        {
            int count = 0;
            List<string> varsToDelete = new();
            foreach (var kvp in m_GlobalVariableDatabase)
            {
                if (!kvp.Value.GetFlag(VariableInfo.Attributes.System))
                {
                    count++;
                    varsToDelete.Add(kvp.Key);
                }
            }
            foreach (var var in varsToDelete)
                m_GlobalVariableDatabase.Remove(var);
            if (from is not null)
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} variables cleared.", count), from.NetState);
            count = m_PlayerVariableDatabase.Count;
            m_PlayerVariableDatabase.Clear();
            if (from is not null)
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} player variables cleared.", count), from.NetState);
            count = m_ItemDatabase.Count;
            m_ItemDatabase.Clear();
            if (from is not null)
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} items cleared.", count), from.NetState);
            count = m_ConditionDatabase.Count;
            m_ConditionDatabase.Clear();
            if (from is not null)
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} conditions cleared.", count), from.NetState);
            count = m_KeywordDatabase.Count;
            m_KeywordDatabase.Clear();
            m_PrivateKeywords.Clear();
            if (from is not null)
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} keywords cleared.", count), from.NetState);
            count = m_PlayerMemory.Count;
            m_PlayerMemory.WipeMemory();
            if (from is not null)
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Memory of {0} objects cleared.", count), from.NetState);
        }
        private bool CheckBackpack(Mobile from, string label)
        {   // check the players backpack for the item with "label"

            if (from != null && from.Backpack != null)
                foreach (Item ix in from.Backpack.Items)
                {
                    if (m_ItemDatabase.ContainsKey(ix))
                        if ((GetField(m_ItemDatabase[ix], Field.Name) as string).ToLower() == label.ToLower())
                            return true;
                }

            return false;
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

        protected string MakeString(Mobile m, object[] tokens, int offset, bool expandMacros = false)
        {
            string temp = "";
            if (expandMacros)
                ExpandMacros(m, ref tokens, offset);
#if false
            ExpansionStatus result;
            string match = null;
            if ((result = ExpandMacros(ref tokens, offset, ref match)) == ExpansionStatus.Okay)
                for (int ix = 0; ix < tokens.Length; ix++)
                {
                    if (temp.Length > 0 && tokens[ix] is string)
                        temp += ' ';

                    if (tokens[ix] is string)
                        temp += (tokens[ix] as string).Contains(' ') ? '"' + (tokens[ix] as string) + '"' : tokens[ix] as string;
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
#endif
            return GlueString(tokens, offset);
        }
        private string GlueString(object[] tokens, int offset)
        {
            string temp = "";
            for (int ix = offset; ix < tokens.Length; ix++)
            {
                if (tokens[ix].ToString().Length == 1 && Char.IsPunctuation(tokens[ix].ToString()[0]))
                    temp = temp.Trim() + tokens[ix].ToString() + " ";
                else
                    temp += tokens[ix].ToString() + " ";
            }
            return temp.Trim();
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

            bool valid = Sextant.Format(px, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

            if (valid)
                location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
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
        protected void ExpandMacros(Mobile m, ref object[] tokens, int offset)
        {
            for (int ix = offset; ix < tokens.Length; ix++)
            {
                string temp = tokens[ix].ToString();
                int first = temp.IndexOf('%');
                int last = temp.LastIndexOf('%');

                if (first != -1 && last != -1 && first < last)
                {
                    string prop = temp.Substring(first + 1, last - first - 1);
                    object o = GetVar(prop);    // first try to expand from the global database
                    if (o == null)
                        o = GetVar(prop, m);     // next try to expand from the specific player's database
                    if (o != null)
                        if (prop.Contains("oldname", StringComparison.OrdinalIgnoreCase))
                            tokens[ix] = LookupOldName(o.ToString()) + temp.Substring(last - first + 1);
                        else
                            tokens[ix] = o.ToString() + temp.Substring(last - first + 1);
                }
            }
        }
        private Dictionary<string, string> NameCache = new();
        private string LookupOldName(string text)
        {
            // remove the scope specifiers
            //text = text.Replace("player.", "", StringComparison.OrdinalIgnoreCase).Replace("item.", "", StringComparison.OrdinalIgnoreCase);

            // cache the names
            if (NameCache.ContainsKey(text))
                return NameCache[text];
            else
                NameCache.Add(text, null);

            Item item = null;
            try
            {
                if (CacheFactory.OldNameToTypeQuickTable.ContainsKey(text))
                {
                    item = (Item)Activator.CreateInstance(CacheFactory.OldNameToTypeQuickTable[text]);
                    if (item != null)
                    {
                        NameCache[text] = item.OldSchoolName();
                        return NameCache[text];
                    }
                }

                // give up
                NameCache[text] = text;
                return NameCache[text];
            }
            finally
            {
                if (item != null)
                    item.Delete();
            }
        }
#if false
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

                    if (name == "npc")
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
                    else if (name == "pc")
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
#endif
        private Item Lookup(string name, bool exists)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
                if (GetField(kvp.Value, Field.Track) != null && GetField(kvp.Value, Field.Name) as string == name)
                {   // deleted of freeze dried.
                    if (kvp.Key.Deleted)
                        continue;

                    // return it if it's the state (exists) we want
                    if (kvp.Key.RootParent == this == exists)
                        return kvp.Key;
                }

            return null;
        }

        private Item Lookup(string name)
        {
            foreach (KeyValuePair<Item, object[]> kvp in m_ItemDatabase)
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
                    if (s == "onenter" || s == "onreceive" || s == "onreturn" | s == "terminate")
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
        protected bool PrivateKeyword(object[] tokens, int offset, out string found)
        {
            found = "";
            if (tokens.Length > offset && tokens[offset] is string)
            {
                // look at our current set of keywords skipping to offest (ignore 'keywords' etc.)
                for (int mx = offset; mx < tokens.Length; mx++)
                {
                    string s = tokens[mx] as string;
                    if (m_PrivateKeywords.Contains(s))
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

        protected bool FindKeywordAliases(string keyword, out string[] found)
        {
            found = new string[0];

            if (m_KeywordDatabase.ContainsKey(keyword) == false)
                return false;

            List<string> list = new List<string>();
            list.Add(keyword);
            object[] actions = m_KeywordDatabase[keyword];

            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
                if (kvp.Key != keyword && kvp.Value == actions)
                    list.Add(kvp.Key);

            found = list.ToArray();

            // we found keyword + N aliases
            return found.Length > 0;
        }

        protected bool FindKeyPhrase(Mobile m, object[] tokens, int offset, out string found)
        {
            found = null;
            string text = MakeString(m, tokens, offset);
            ;
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
            {
                string key = kvp.Key.Replace("\"", "");
                if (text.Contains(key, StringComparison.OrdinalIgnoreCase))
                {
                    found = kvp.Key;
                    return true;
                }
            }
#if false
            // check each keyword entry for a special dotted keyword phrase
            foreach (KeyValuePair<string, object[]> kvp in m_KeywordDatabase)
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
#endif
            // not found
            return false;
        }
#if false
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
#endif
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
                        if (m_KeywordDatabase.ContainsKey(kword))
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
                    if (m_KeywordDatabase.ContainsKey(tokens[ix] as string))
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

        protected bool ComputeConditions(object[] tokens, int offset, bool defined, out string good, out string bad)
        {
            good = "";
            bad = "";
            string[] good_array;
            string[] bad_array;
            bool result = ComputeConditions(tokens, offset, defined, out good_array, out bad_array);

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

        protected bool ComputeConditions(object[] tokens, int offset, bool defined, out string[] good, out string[] bad)
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

                    // do we know about this condition?
                    if (m_ConditionDatabase.Any(x => x.Key.Equals(tokens[ix] as string)))
                    {   // the condition is known?
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

        // Here we can fix up syntax ugliness:
        //  Example1: convert to public|private keyword format from 'sissy add label XYZ' to 'sissy add private XYZ'
        //  Example2: convert to public|private keyword format from 'sissy add keyword QUEST' to 'sissy add public QUEST'
        protected string PreCompile(string input, ref int depth)
        {
            bool changed = false;
            depth++;

            #region sissy add label XYZ
            // convert to public|private keyword format from 'sissy add label XYZ' to 'sissy add private XYZ'
            var match_add_label = PreCAddLabel.Match(input);
            if (match_add_label.Success)
            {
                input = input.Replace("label", "private", StringComparison.OrdinalIgnoreCase);
                changed = true;
            }
            #endregion sissy add label XYZ

            #region sissy add keyword XYZ
            // convert to public|private keyword format from 'sissy add keyword QUEST' to 'sissy add public QUEST'
            var match_keyword = Keyword.Match(input);
            if (match_keyword.Success)
            {
                input = input.Replace("keyword", "public", StringComparison.OrdinalIgnoreCase);
                changed = true;
            }
            #endregion sissy add keyword XYZ

            // keep processing until there is no more changes
            if (changed == true && depth < 8)
                return PreCompile(input, ref depth);
            else
                return input;
        }
        #region StringToKeyPhrase (Obsolete)
#if false
        protected string StringToKeyPhrase(string text)
        {
            return text.Replace("\"", "").Replace(' ', '.').Trim();
        }
#endif
        #endregion StringToKeyPhrase (Obsolete)
        protected object[] Compile(string input)
        {   // compile the string into an array of objects

            List<object> list = new List<object>();

            LRTextParser(ref input, list);

            return list.ToArray();
        }
        /// <summary>
        /// LRTextParser (left-right) parses incoming test into logical units
        /// This parser will parse this sample text string into these logical units.
        ///     Please pay special attention how we handle 'tail punctuation' vs 'embedded punctuation'
        /// Example text: action nice_try say it's %item.OldName%, but not the one I was looking for.
        ///     action|nice_try|say|it's|%item.OldName%|,|but|not|the|one|I|was|looking|for|.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="list"></param>
        /// <returns>true if successful</returns>
        private void LRTextParser(ref string input, List<object> list)
        {
            // are we done?
            if (input.Equals(string.Empty))
                return;

            string chunk;
            // match one of the special tokens (must)
            while ((chunk = GetChunk(input, LRTPToken)) != null)
            {
                list.Add(chunk);
                input = RemoveChunk(input, LRTPToken);
                LRTextParser(ref input, list);
                return;
            }

            // match one of the 'tail punctuation' tokens (optional)
            while ((chunk = GetChunk(input, LRTPPuncuation)) != null)
            {
                list.Add(chunk);
                input = RemoveChunk(input, LRTPPuncuation);
                LRTextParser(ref input, list);
                return;
            }

            // match whatever token follows
            if ((chunk = GetChunk(input, LRTPWhatever)) != null)
            {
                list.Add(chunk.Trim());
                input = RemoveChunk(input, LRTPWhatever);
                LRTextParser(ref input, list);
                return;
            }
        }
#if false
        private bool LRTokenParser(ref string input, List<object> list)
        {
            Regex Token = new Regex(@"^("".*?""|\b[0-9]+\b|\b[a-zA-Z][a-zA-Z0-9_\.]+\b)");
            Regex Puncuation = new Regex(@"^(?![""])\p{P}");

            string chunk;

            chunk = GetChunk(input, Token);
            if (chunk != null)
            {
                list.Add(chunk);
                input = RemoveChunk(input, Token);
            }
            else
                return false;

            chunk = GetChunk(input, Puncuation);
            if (chunk != null)
            {
                list.Add(chunk);
                input = RemoveChunk(input, Puncuation);
            }

            if (input.Equals(string.Empty))
                return true;

            return LRTokenParser(ref input, list);
        }
#endif
        #region Standard Vendor Functions        
        protected ArrayList GetItems()
        {   // arraylist to get all items in vendor backpack used for destroying vendors
            ArrayList list = new ArrayList();

            foreach (Item item in this.Items)
            {
                if (item.Movable && item != this.Backpack)
                    list.Add(item);
            }

            if (this.Backpack != null)
            {
                list.AddRange(this.Backpack.Items);
            }

            return list;
        }
        public virtual void Destroy(bool toBackpack)
        {
            Item shoes = this.FindItemOnLayer(Layer.Shoes);

            if (shoes is Sandals)
                shoes.Hue = 0;

            ArrayList list = GetItems();

            // don't drop stuff owned by an administrator
            if (IsStaffOwned == false)
                if (list.Count > 0) // if you have items
                {
                    if (toBackpack && this.Map != Map.Internal) // Move to backpack
                    {
                        Container backpack = new Backpack();

                        foreach (Item item in list)
                        {
                            if (item.Movable != false) // only drop items which are moveable
                                backpack.DropItem(item);
                        }

                        backpack.MoveToWorld(this.Location, this.Map);
                    }
                }

            Delete();
        }
        public void Dismiss(Mobile from)
        {
            Container pack = this.Backpack;

            if (pack != null && pack.Items.Count > 0)
            {
                SayTo(from, 503229); // Thou canst replace me until thy removest all the item from my stock.
                return;
            }

            Destroy(pack != null);
        }
        private void NonLocalDropCallback(object state)
        {
            object[] aState = (object[])state;

            Mobile from = (Mobile)aState[0];
            Item item = (Item)aState[1];

            OnItemGiven(from, item);
        }
        public override bool AllowEquipFrom(Mobile from)
        {
            if (IsOwner(from) && from.InRange(this, 3) && from.InLOS(this))
                return true;

            return base.AllowEquipFrom(from);
        }
        #endregion Standard Vendor Functions
        #region Uninteresting QuestGiver Functions
        private void OnItemGiven(Mobile from, Item item)
        {   // see if it already has a name
            if (m_ItemDatabase.ContainsKey(item) && GetField(m_ItemDatabase[item], Field.Name) != null)
            {
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "Okay.", from.NetState);
                return;
            }
            from.Prompt = new LabelItemPrompt(this, item);
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "What would you like to name this item?", from.NetState);
        }
        public void LabelItem(Mobile from, Item item, string text)
        {
            if (m_ItemDatabase.Count >= 256)
            {   // hard stop
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Your label database is full."), from.NetState);
                return;
            }

            // does it already have a label?
            if (m_ItemDatabase.ContainsKey(item) && GetField(m_ItemDatabase[item], Field.Name) != null)
                return;

            if (text == null || text.Length == 0)
            {
                if (item.Name != null && item.Name.Length > 0)
                    m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Name, item.Name);
                //adam: what if m_ItemDatabase[item] == null? I.e., no item?
                else if (m_ItemDatabase.ContainsKey(item) && GetField(m_ItemDatabase[item], Field.Name) != null)
                    m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Name, GetField(m_ItemDatabase[item], Field.Name));
                else
                    m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Name, item.ItemData.Name);
            }
            else
                m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Name, text);

            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
        }
        private object[] AppendField(object[] tokens, Field field, object value)
        {
            List<object> list = new List<object>();

            int skip = 0;
            switch (field)
            {   // skipping fields is how to update an existing field. I.e., we never copy over the old field
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
        public bool TrackItem(Mobile from, Item item)
        {
            if (m_ItemDatabase.Count >= 256)
            {   // hard stop
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Your label database is full."), from.NetState);
                return false;
            }

            if (m_ItemDatabase.ContainsKey(item) && GetField(m_ItemDatabase[item], Field.Track) != null)
            {   // we're already tracking this item
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("I am already tracking this item."), from.NetState);
                return true;
            }

            // add the 'tracking' field
            m_ItemDatabase[item] = AppendField(m_ItemDatabase.ContainsKey(item) ? m_ItemDatabase[item] : null, Field.Track, null);

            if (GetField(m_ItemDatabase[item], Field.Name) == null)
                OnItemGiven(from, item);
            else
                PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay."), from.NetState);
            return true;
        }
        public class LabelItemPrompt : Prompt
        {
            private QuestGiver m_QuestGiver;
            private Item m_item;

            public LabelItemPrompt(QuestGiver questGiver, Item item)
            {
                m_QuestGiver = questGiver;
                m_item = item;
            }

            public override void OnCancel(Mobile from)
            {
                OnResponse(from, m_item.GetType().Name);
            }

            public override void OnResponse(Mobile from, string text)
            {
                if (text.Length > 50)
                    text = text.Substring(0, 50);

                m_QuestGiver.LabelItem(from, m_item, text);
            }
        }
        public class OnReceiveTarget : Target
        {
            private QuestGiver m_QuestGiver;

            public OnReceiveTarget(QuestGiver questGiver)
                : base(15, false, TargetFlags.None)
            {
                m_QuestGiver = questGiver;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is Item)
                    m_QuestGiver.TrackItem(from, targ as Item);
                else
                    m_QuestGiver.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("That is not a valid item."), from.NetState);
                return;
            }
        }
        #endregion Uninteresting QuestGiver Functions
    }
}

#region QuestGiver Contract
namespace Server.Items
{
    public class QuestGiverContract : Item
    {
        [Constructable]
        public QuestGiverContract()
            : base(0x14F0)
        {
            Name = "a quest giver contract";
            Weight = 1.0;
            LootType = LootType.Regular;
        }

        public QuestGiverContract(Serial serial)
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
                from.SendLocalizedMessage(503248); // Your godly powers allow you to place this vendor whereever you wish.

                Mobile v = new QuestGiver(from);
                v.Direction = from.Direction & Direction.Mask;
                v.MoveToWorld(from.Location, from.Map);
                this.Delete();
            }
            else
            {
                BaseHouse house = BaseHouse.FindHouseAt(from);

                if (house == null || !house.IsOwner(from))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, false, "You are not the full owner of this house.");
                }
                else if (!CanPlaceNewQuestGiver(house.Region))
                {
                    from.SendMessage("You may not add any more quest givers to this house.");
                }
                else
                {
                    Mobile v = new QuestGiver(from);
                    v.Direction = from.Direction & Direction.Mask;
                    v.MoveToWorld(from.Location, from.Map);
                    this.Delete();
                }
            }
        }

        public bool CanPlaceNewQuestGiver(Region region)
        {
            if (region == null)
                return false;

            // 7 quest givers
            // keyword database is 256 rows if 768 characters
            // so a single user can control 1,376,256 bytes of memory
            int avail = 7;
            foreach (Mobile mx in region.Mobiles.Values)
            {
                if (avail <= 0)
                    break;

                if (mx is QuestGiver)
                    --avail;
            }

            return (avail > 0);
        }
    }
}
#endregion QuestGiver Contract