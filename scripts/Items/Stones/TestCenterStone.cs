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

using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Craft;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Server.Items
{
    public class TestCenterStone : Item
    {
        const int MemoryTime = 30;          // how long (seconds) we remember this player
        public static void Initialize()
        {
            CommandSystem.Register("Whitelist", AccessLevel.GameMaster, new CommandEventHandler(Whitelist_OnCommand));
        }

        [Usage("Whitelist")]
        [Description("Adds an item to the Test Center Stone Whitelist.")]
        private static void Whitelist_OnCommand(CommandEventArgs e)
        {
            try
            {
                if (!ValidType(e.GetString(0)))
                {
                    e.Mobile.SendMessage("Item type was not found");
                    return;
                }
            ;
                Type t = ScriptCompiler.FindTypeByName(e.GetString(0));
                if (t == null)
                {
                    e.Mobile.SendMessage("Item type was not found");
                    return;
                }
                object o = Activator.CreateInstance(t);
                if (o is Item item)
                {
                    item.Delete();
                }
                else
                {
                    e.Mobile.SendMessage("Item type was not found");
                    return;
                }

                LogHelper logger = new LogHelper(Path.Combine(Core.DataDirectory, "TCItemsWhitelist.log"), false, true, true);
                logger.Log(e.GetString(0));
                logger.Finish();
                e.Mobile.SendMessage("{0} added", e.GetString(0));
            }
            catch
            {
                e.Mobile.SendMessage("Cannot construct {0}", e.GetString(0));
            }
        }

        private static List<string> TypeNames = new();
        private static bool ValidType(string typeName)
        {

            if (TypeNames.Count == 0)
            {
                Type[] types;
                Assembly[] asms = ScriptCompiler.Assemblies;

                ArrayList MatchTypes = new ArrayList();

                for (int i = 0; i < asms.Length; ++i)
                {
                    types = ScriptCompiler.GetTypeCache(asms[i]).Types;

                    foreach (Type t in types)
                    {

                        if (typeof(Item).IsAssignableFrom(t) &&             // must be an item
                                !typeof(BaseMulti).IsAssignableFrom(t) &&       // must not be a multi
                                !typeof(Multis.BaseBoat).IsAssignableFrom(t))   // must not be a base boat
                        {
                            if (!TypeNames.Contains(t.Name))
                                TypeNames.Add(t.Name);
                        }
                    }
                }
            }

            return TypeNames.Contains(typeName, StringComparer.OrdinalIgnoreCase);
        }
        [Constructable]
        [Aliases("GivingStone")]
        public TestCenterStone()
            : base(0xED4)
        {
            Movable = false;
            Hue = 0x2D1;
            Name = "giving stone";
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("Just tell me what you want:");
            from.SendMessage("Example: Bag of reagents");
            from.SendMessage("Example: 100 boards");
            from.SendMessage("Example: 100 golden ingots");
        }
        private void ParseText(string text, ref string thing, ref uint amount)
        {
            thing = null;
            amount = 1;
            try
            {   // [100] "thing"
                //  -or-
                // thing [100]
                string[] chunks = text.Split();
                uint temp = 0;

                if (chunks.Length == 2)
                {
                    bool numberFirst = uint.TryParse(chunks[0], out temp);
                    bool numberSecond = uint.TryParse(chunks[1], out temp);
                    if (numberFirst)
                    {
                        thing = chunks[1];
                        amount = temp;
                    }
                    else if (numberSecond)
                    {
                        thing = chunks[0];
                        amount = temp;
                    }
                }
                else
                {
                    // else, amount is 1
                    thing = text;
                }

                return;
            }
            catch
            {

            }
            return;
        }
        private List<string> ItemNames = new();
        public override bool HandlesOnSpeech => true;
        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            double distance = this.GetDistanceToSqrt(e.Mobile);
            if (distance > 3)
                return;

            #region Load Type Name Cache
            if (ItemNames.Count == 0)
            {   // all the items vendors sell
                foreach (var kvp in CacheFactory.InventoryQuickTable)
                    if (!ItemNames.Contains(kvp.Key.ToLower()))
                        ItemNames.Add(kvp.Key.ToLower());

                foreach (var type in CraftSystem.CraftableTypes)
                    if (!ItemNames.Contains(type.Name.ToLower()))
                        ItemNames.Add(type.Name.ToLower());
            }
            #endregion Load Type Name Cache

            #region Load Whitelist
            try
            {   // specifically allowed items not sold on vendors, like gold and and resources like iron ingots
                if (File.Exists(Path.Combine(Core.DataDirectory, "TCItemsWhitelist.log")))
                    foreach (var text in File.ReadLines(Path.Combine(Core.DataDirectory, "TCItemsWhitelist.log")))
                        if (!ItemNames.Contains(text.ToLower()))
                            ItemNames.Add(text.ToLower());
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return;
            }
            #endregion Load Whitelist
            try
            {
                uint amount = 0;
                uint worth = 0;
                string thing = null;
                #region Parse Text
                ParseText(e.Speech, ref thing, ref amount);
                bool isBankCheck = thing.Equals("bankcheck", StringComparison.OrdinalIgnoreCase);
                if (thing == null || amount == 0)
                {
                    e.Mobile.SendMessage("Usage: 100 boards");
                    return;
                }
                else
                {   // caps gold, and the rest
                    if (isBankCheck)
                    {
                        worth = (uint)Math.Min(Math.Abs(amount), 5000000);  // 5 million
                        amount = 1;
                    }
                    else
                        amount = (uint)Math.Min(Math.Abs(amount), 50000);
                }
                #endregion
                int score;
                string real_name = IntelligentDialogue.Levenshtein.BestListMatch(ItemNames, thing, out score);
                if (score > 5) // a 'distance' of > 5 is likely bad input (5 is a heuristically derived value)
                {
                    DontHave(e.Mobile, thing);
                    return;
                }

                if (ItemFlter(real_name))
                {
                    Type t = ScriptCompiler.FindTypeByName(real_name);
                    if (t == null)
                    {
                        DontHave(e.Mobile, thing);
                        return;
                    }
                    object o = Activator.CreateInstance(t);
                    if (o is Item item)
                    {
                        e.Mobile.SendMessage("I found {0}", item.GetType().Name);

                        if (item is BankCheck check)
                            check.Worth = (int)worth;

                        if (item.Stackable || amount == 1)
                            item.Amount = (int)amount;
                        else
                        {
                            Bag bag = new Bag();
                            amount = Math.Min(125, amount);
                            bag.AddItem(item);
                            for (int ix = 1; ix < amount; ix++)
                            {
                                item = (Item)Activator.CreateInstance(t);
                                if (item != null)
                                    if (!bag.TryDropItem(e.Mobile, item, false))
                                        break;
                            }
                            item = bag;
                        }
                        if (!e.Mobile.AddToBackpack(item))
                        {
                            e.Mobile.SendMessage("Your backpack is full, and I was unable to add {0}", item.GetType().Name);
                            item.Delete();
                        }

                    }
                    else
                    {
                        DontHave(e.Mobile, thing);
                        return;
                    }
                }
                else
                {
                    e.Mobile.SendMessage("Sorry, but you do not have access to that item.");
                }
            }
            catch
            {
                DontHave(e.Mobile, e.Speech);
                return;
            }
        }
        private void DontHave(Mobile m, string thing)
        {
            m.SendMessage("I don't seem to have {0}", thing);
        }
        private Memory m_PlayerMemory = new Memory();       // memory used to remember if we a saw a player in the area
        public override bool HandlesOnMovement => true;
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);
            // yeah
            if (m is PlayerMobile == false)
                return;

            // sanity
            if (m.Deleted || m.Hidden || !m.Alive)
                return;

            // too far away
            double distance = this.GetDistanceToSqrt(m);
            if (distance > 3)
                return;

            // if we're not busy casting, remember this player
            if (m_PlayerMemory.Recall(m) == false)
            {   // we haven't seen this player yet
                m_PlayerMemory.Remember(m, TimeSpan.FromSeconds(MemoryTime).TotalSeconds);   // remember him for this long
                OnDoubleClick(m);
            }
        }
        private bool ItemFlter(string typeName)
        {
            if (typeName.Contains("console", StringComparison.OrdinalIgnoreCase))
                return false;
            Type t = ScriptCompiler.FindTypeByName(typeName);
            if (t == null)
                return false;
            object o = Activator.CreateInstance(t);
            if (o is Item item)
            {
                if (item.Movable == false || item.Visible == false)
                    return false;

                item.Delete();
                return true;
            }
            else
                return false;

            return true;
        }
        public TestCenterStone(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}