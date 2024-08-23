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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/AngryMiner.cs
 * ChangeLog
 *  7/28/21, Adam
 *		First time checkin
 *		AngryMiners is an expieriment in two areas. 
 *		    Coordinated Speech Dialog attempts to cordinate dialog between the miners by sharing the sequencing mechanism.
 *		        So for instance, minerA says phrase#1 and minerB wil say phrase#2. Additionally, I expieriment with timing certain
 *		        messages. For instance, most messages are just spoken at a defalult (yet still randomized) interval. But certain
 *		        messages attempt to seem like the miners are responding to one another. For example, messages 4 and 5 of the 'hidden'
 *		        dialog shortten this delay between yelps to make it appear as minerA is responding to minerB.
 *          Expended AI
 *		        To create a new AI, or AI subclass each time you want to make minor changes to behaviors is a little messy.
 *		        In this expieriment, I utilize some hooks I have placed into the AI to allow a mobile specific AI tweak.
 *		        For instance, I placed the hooks GoingHome() and ImHome() into the BaseAI. (Called from wandering.)
 *		        These hooks allow me to elicit particular speech if say we are wandering around, but cant see our combatant.
 *		        I use OnSee() to establish our ConstantFocus mob. We lock onto the first playerMobile that we see and they
 *		        become our preferred focus. Usually this is the miner, but sometimes it will someone there with them. This is fine.
 */

using Server.Items;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Mobiles
{
    public class AngryMiner : BaseCreature
    {
        Type m_resourceType = null;
        Point3D m_originalHome = Point3D.Zero;
        InstanceMessage m_instanceMessage = InstanceMessage.None;
        List<AngryMiner> m_compadres = new List<AngryMiner>();
        List<AngryMiner> Compadres { get { return CleanCompadres(); } }
        public class mState
        {
            AngryMiner m_me;
            public AngryMiner Me { get { return m_me; } }
            InstanceMessage m_message;
            public InstanceMessage Message { get { return m_message; } }
            PlayerMobile m_target;
            public PlayerMobile Target { get { return m_target; } }
            string m_text;
            public string Text { get { return m_text; } }
            public mState(AngryMiner me, InstanceMessage message, PlayerMobile target = null, string text = null)
            {
                m_me = me;
                m_message = message;
                m_target = target;
                m_text = text;
            }
        }
        public InstanceMessage iMessage { get { return m_instanceMessage; } set { m_instanceMessage = value; } }
        private static class AngryMinerDialog
        {
            static double m_defaultDelay = 10.0;
            private static AngryMiner GetRandomAngryMiner(PlayerMobile focus, List<AngryMiner> list)
            {   // only select from angry miners that are sufficiently close to our focus.
                List<AngryMiner> clean_list = new List<AngryMiner>();
                if (focus == null) return null;
                foreach (AngryMiner am in list)
                    if (am.GetDistanceToSqrt(focus) <= am.RangePerception)
                        clean_list.Add(am);

                return clean_list[Utility.Random(clean_list.Count)];
            }
            private class mRegion
            {
                List<Message> m_messageQueue = new List<Message>();
                public List<Message> MessageQueue { get { return m_messageQueue; } }
                int m_chatter_ndx = 0;
                public int chatter_ndx { get { return m_chatter_ndx; } set { m_chatter_ndx = value; } }
                int m_hidden_ndx = 0;
                public int hidden_ndx { get { return m_hidden_ndx; } set { m_hidden_ndx = value; } }
                int m_hit_ndx = 0;
                public int hit_ndx { get { return m_hit_ndx; } set { m_hit_ndx = value; } }
                int m_tracking_ndx = 0;
                public int tracking_ndx { get { return m_tracking_ndx; } set { m_tracking_ndx = value; } }
                int m_combatantKilled_ndx = 0;
                public int combatantKilled_ndx { get { return m_combatantKilled_ndx; } set { m_combatantKilled_ndx = value; } }
                int m_newCompadreInfo_ndx = 0;
                public int newCompadreInfo_ndx { get { return m_newCompadreInfo_ndx; } set { m_newCompadreInfo_ndx = value; } }
                private Dictionary<PlayerMobile, List<AngryMiner>> m_dictionary = new Dictionary<PlayerMobile, List<AngryMiner>>();
                public Dictionary<PlayerMobile, List<AngryMiner>> Dictionary { get { return m_dictionary; } }
                private Stack<double> m_delayOverride = new Stack<double>();
                public Stack<double> DelayOverride { get { return m_delayOverride; } }
                private DateTime m_nextIdleChat = DateTime.UtcNow;
                public DateTime NextIdleChat { get { return m_nextIdleChat; } set { m_nextIdleChat = value; } }
                public void ForceAcceptIdleMessage()
                {   // force the next message to be accepted. This is important for response type messages.
                    //  Most other messages don't matter.
                    m_nextIdleChat = DateTime.UtcNow;
                }
                public TimeSpan GetQueueDeltaTime(mState state)
                {
                    Message found = null;
                    foreach (Message m in m_messageQueue)
                    {
                        // state[1] is our InstanceMessage context - so it's our filter
                        if (m.State.Message == state.Message)
                        {
                            if (found == null)
                            {
                                found = m;
                                continue;
                            }

                            // select the message furthest out in time
                            if (m.Expiry > found.Expiry)
                                found = m;
                        }
                    }

                    if (found == null)
                        return TimeSpan.Zero;

                    return found.Expiry.Subtract(DateTime.UtcNow);
                }
                public void ClipNextMessageDelay(double delay)
                {
                    DelayOverride.Push(delay);
                }
                public Message GetMessage()
                {
                    Message found = null;
                    foreach (Message m in m_messageQueue)
                    {
                        // has the delay been met?
                        if (DateTime.UtcNow > m.Expiry)
                        {
                            if (found == null)
                            {
                                found = m;
                                continue;
                            }

                            if (m.Expiry < found.Expiry)
                                found = m;
                        }
                    }

                    if (found != null)
                        m_messageQueue.Remove(found);

                    return found;
                }
                public mRegion(PlayerMobile target, List<AngryMiner> list)
                {
                    m_dictionary.Add(target, list);
                }
            }
            private static Dictionary<PlayerMobile, mRegion> m_registry = new Dictionary<PlayerMobile, mRegion>();
            private static mRegion Find_mRegion(PlayerMobile target)
            {
                if (m_registry.ContainsKey(target))
                    return m_registry[target];
                return null;
            }
            private static mRegion RegisterMobile(PlayerMobile target, List<AngryMiner> list)
            {
                if (target == null)
                {
                    //Utility.PushColor(ConsoleColor.Red);
                    //Console.WriteLine("Cannot register target PlayerMobile. Maybe dead, or too far.");
                    //Utility.PopColor();
                    return null;
                }
                else
                {
                    //Utility.PushColor(ConsoleColor.Red);
                    //Console.WriteLine("registering target PlayerMobile {0}.", target);
                    //Utility.PopColor();
                }

                // first lookup our mRegion. An mRegion is a mobile centric region that contains the target mobile
                //  and all angry miners that are angry with him.
                mRegion mRegion;
                if (!m_registry.ContainsKey(target))
                {
                    mRegion = new mRegion(target, list);
                    m_registry.Add(target, mRegion);
                }
                else
                    mRegion = m_registry[target];

                // okay, we now have an mRegion

                // first register this playermobile and all the miners that are angry at him
                if (!mRegion.Dictionary.ContainsKey(target))
                {   // register
                    mRegion.Dictionary.Add(target, new List<AngryMiner>(list));
                }
                else
                {   // update the registration by adding the angry miners to the list of concerned angry miners
                    // 
                    foreach (AngryMiner am in list)
                        if (mRegion.Dictionary[target].Contains(am))
                            continue;
                        else
                            mRegion.Dictionary[target].Add(am);

                    //lines2.AddRange(lines3.Distinct());
                    //mRegion.Dictionary[target].AddRange(list.Distinct());
                }

                return mRegion;
            }
            // PostIdleMessage posts messages to 'region' oriented angry miners.
            //  A region is defined by the angry miners that have 'target' as their preferred focus, and target is within their RangePerception
            //  this method is dynamic since both targets and angry minors move in an out of eachothers RangePerception
            public static void PostIdleMessage(List<AngryMiner> list, double delay, mState state)
            {
                try
                {
                    PlayerMobile target = state.Target;
                    if (target == null)
                        return;

                    // first register this playermobile and all the miners that are angry at him
                    mRegion mRegion = RegisterMobile(target, list);

                    if (DateTime.UtcNow > mRegion.NextIdleChat)
                    {
                        // next idle chat time
                        mRegion.NextIdleChat = DateTime.UtcNow + TimeSpan.FromSeconds(AngryMinerDialog.GetChatterDelay / 2 + Utility.RandomMinMax(1, 3));

                        // Okay, now we have a list of angry miners that sould be concerned with target
                        AngryMiner me = GetRandomAngryMiner(target, mRegion.Dictionary[target]);
                        if (me == null)
                        {
                            Utility.Monitor.WriteLine("Cannot find an angry miner to handle this.", ConsoleColor.Red);
                            return;
                        }

                        if (mRegion.DelayOverride.Count > 0)
                            delay = mRegion.DelayOverride.Pop();

                        // enqueue the message
                        mRegion.MessageQueue.Add(new Message(me, delay, state));
                    }
                }
                catch { }
                finally
                {
                    // Process the chatter queue
                    AngryMinerDialog.DoChatter();
                }
            }
            private static void ExpireOldMessages(mRegion mRegion)
            {
                int delta = mRegion.MessageQueue.Count - 16;
                if (delta > 0)
                    // remove all these old messages from the tail of the queue
                    mRegion.MessageQueue.RemoveRange(16, delta);
            }
            public static void PostMessage(AngryMiner me, double delay, mState state)
            {
                // first register this playermobile and all the miners that are angry at him
                //  we don't have the miners in this context, so we will pass an empty list
                mRegion mRegion = RegisterMobile(state.Target, new List<AngryMiner>());
                if (mRegion != null)
                {
                    // add the seconds of the last messaage of this type to the delay
                    delay += mRegion.GetQueueDeltaTime(state).TotalSeconds;
                    // enqueue the message
                    mRegion.MessageQueue.Add(new Message(me, delay, state));
                    // remove old messages - we cap the queue at 16 messages outstanding
                    ExpireOldMessages(mRegion);
                }
                // process queue
                AngryMinerDialog.DoChatter();
            }
            public class Message
            {

                AngryMiner m_me;
                public AngryMiner Me { get { return m_me; } }
                mState m_state;
                public mState State { get { return m_state; } }
                DateTime m_expiry;
                public DateTime Expiry { get { return m_expiry; } set { m_expiry = value; } }
                public Message(AngryMiner me, double delay, mState state)
                {
                    m_me = me;
                    m_state = state;
                    m_expiry = DateTime.UtcNow + TimeSpan.FromSeconds(delay);
                }
            }
            public static double GetChatterDelay { get { return m_defaultDelay; } }
            static object m_flushObject;
            // Any time you call FlushUntil, you need to manually clear it with an InstanceMessage.Clear
            public static object FlushUntil { get { return (object)m_flushObject; } set { m_flushObject = (object)value; } }
            // FlushNow wipes all messages from the queue - now need to 'Clear' it
            public static void FlushNow(PlayerMobile target)
            {
                mRegion mRegion = RegisterMobile(target, new List<AngryMiner>());
                if (mRegion != null)
                {
                    mRegion.MessageQueue.Clear();
                }
            }
            static Dictionary<PlayerMobile, DateTime> m_doppelganger = new Dictionary<PlayerMobile, DateTime>();    // not serialized
            public static Dictionary<PlayerMobile, DateTime> Doppelganger { get { return m_doppelganger; } }
            public static void DoChatter()
            {
                try
                {
                    Message mx;
                    foreach (mRegion mRegion in m_registry.Values)
                        while ((mx = mRegion.GetMessage()) != null)
                        {
                            //Console.WriteLine("{1}: Queue depth: {0}", mRegion.MessageQueue.Count, mx.State.Target);
                            // unpack the variables
                            AngryMiner me = mx.Me;
                            Mobile target = mx.State.Target;
                            InstanceMessage context = mx.State.Message;
                            string format_string = null;


                            // COMMAND FLUSH UNTIL - Until we get a 'Clear' message
                            if (FlushUntil != null && ((InstanceMessage)FlushUntil).HasFlag(context))
                            {   // just throw all these messages away
                                //return;
                                continue;
                            }

                            // COMMAND CLEAR - 'Clear's and FlschUntil filter
                            if (context.HasFlag(InstanceMessage.Clear))
                            {
                                if (me.iMessage != InstanceMessage.RegularMessage)
                                {
                                    Utility.Monitor.WriteLine("{0}: Clearing {1} attribute.", ConsoleColor.Red, me.Name, me.iMessage.ToString());
                                }
                                else
                                {
                                    Utility.Monitor.WriteLine("{0}, Warning: attribute {1} already cleared.", ConsoleColor.Red, me.Name, me.iMessage.ToString());
                                }
                                // clear our handling of this message for the team
                                me.iMessage = InstanceMessage.RegularMessage;
                                FlushUntil = null;
                                continue;
                            }

                            /*
                             * Now we talk out loud based on context
                             */
                            if (context.HasFlag(InstanceMessage.NewCompadreInfo))
                            {
                                Mobile author = mx.State.Me;
                                if (author == null)
                                {
                                    Utility.Monitor.WriteLine("{0}: Bad format in {1}.", ConsoleColor.Red, me.Name, context.ToString());
                                    continue;
                                }

                                switch (mRegion.newCompadreInfo_ndx++)
                                {
                                    case 0:
                                        format_string = string.Format("Thank you {0}, I will keep an eye out for {1}.", author.Name, target.Name);
                                        break;
                                    case 1:
                                        Item held = me.FindItemOnLayer(Layer.OneHanded);
                                        format_string = string.Format("I will introduce {0} to {1}.", target.Name, (held != null) ? held.Name : "my fists");
                                        break;
                                    case 2:
                                        format_string = string.Format("{0}, hasn't a chance.", target.Name);
                                        break;
                                    case 3:
                                        format_string = string.Format("Roger that.");
                                        break;
                                    case 4:
                                        format_string = string.Format("Ah, I shall watch for {0}.", target.Name);
                                        break;
                                    case 5:
                                        format_string = string.Format("Step lightly {0}, we angry miners are on to you.", target.Name);
                                        break;
                                    default:
                                        format_string = string.Format("I'm on it.");
                                        mRegion.newCompadreInfo_ndx = 0;
                                        break;
                                }
                                me.Say(format_string);
                            }
                            else if (context.HasFlag(InstanceMessage.Yelp))
                            {
                                if (mx.State.Text != null)
                                    format_string = mx.State.Text;
                                else
                                {
                                    Utility.Monitor.WriteLine("{0}: Bad format in Yelp.", ConsoleColor.Red, me.Name);
                                }
                                me.Say(format_string);
                            }
                            else if (context.HasFlag(InstanceMessage.CombatantKilled))
                            {
                                switch (mRegion.combatantKilled_ndx++)
                                {
                                    case 0:
                                        if (target.LastKiller == me)
                                            format_string = string.Format("I put an end to that scoundrel {0}.", target.Name);
                                        else
                                            format_string = string.Format("You put an end to that scoundrel {0}.", target.Name);
                                        break;
                                    case 1:
                                        if (target.LastKiller == me)
                                            format_string = string.Format("{0} will be giving us no more trouble.", target.Name);
                                        else
                                            if (me.GetDistanceToSqrt(me.OriginalHome) > me.RangePerception / 2)
                                            format_string = string.Format("We should get back to the mine and look for more interlopers.");
                                        else
                                            format_string = string.Format("Our work here is done.");
                                        break;
                                    case 2:
                                        if (target.LastKiller == me)
                                            format_string = string.Format("I didn't really want to kill {0}, but {1} left me no choice.", target.Female ? "her" : "him", target.Female ? "she" : "he");
                                        else
                                        {
                                            format_string = string.Format("Hey, did you hear the one about the sleepy miner?.");
                                            mRegion.ClipNextMessageDelay(0);
                                            mRegion.ForceAcceptIdleMessage();   // make sure the next message is accepted.
                                        }
                                        break;
                                    case 3:
                                        format_string = string.Format("Yeah, {0} took a dirt nap!.", target.Female ? "she" : "he");
                                        mRegion.ClipNextMessageDelay(1 + Utility.Random(3));
                                        mRegion.ForceAcceptIdleMessage();   // make sure the next message is accepted.
                                        break;
                                    case 4:
                                        format_string = string.Format("Ah haha!");
                                        mRegion.ClipNextMessageDelay(1 + Utility.Random(3));
                                        mRegion.ForceAcceptIdleMessage();   // make sure the next message is accepted.
                                        break;
                                    default:
                                        format_string = string.Format("Haha haha!.");
                                        mRegion.combatantKilled_ndx = 0;
                                        break;
                                }
                                me.Say(format_string);
                            }
                            else if (context.HasFlag(InstanceMessage.Tracking))
                            {
                                Mobile author = mx.State.Me;
                                if (author == null)
                                {
                                    Utility.Monitor.WriteLine("{0}: Bad format in {1}.", ConsoleColor.Red, me.Name, context.ToString());
                                    continue;
                                }
                                switch (mRegion.tracking_ndx++)
                                {
                                    case 0:
                                        format_string = string.Format("Got it, on my way.");
                                        break;
                                    case 1:
                                        format_string = string.Format("Lead on {0}.", author.Name);
                                        break;
                                    case 2:
                                        format_string = string.Format("{0}, right behind you.", author.Name);
                                        break;
                                    case 3:
                                        format_string = string.Format("I have tracking too!");
                                        break;
                                    case 4:
                                        format_string = string.Format("Don't let him get away {0}!", author.Name);
                                        break;
                                    case 5:
                                        format_string = string.Format("Comming...");
                                        break;
                                    default:
                                        format_string = string.Format("Aye, on my way.");
                                        mRegion.tracking_ndx = 0;
                                        break;
                                }
                                me.Say(format_string);
                            }
                            else if (context.HasFlag(InstanceMessage.OnHit))
                            {
                                switch (mRegion.hit_ndx++)
                                {
                                    case 0:
                                        format_string = string.Format("Take that!");
                                        mRegion.ClipNextMessageDelay(2);    // we shorten the delay so that the next message looks like a response.
                                        mRegion.ForceAcceptIdleMessage();   // make sure the next message is accepted.
                                        break;
                                    case 1:
                                        format_string = string.Format("And that!");
                                        break;
                                    case 2:
                                        Item item = me.FindItemOnLayer(Layer.OneHanded);
                                        format_string = string.Format("Feel the power of {0}!", (item != null) ? item.Name : "my fist");
                                        break;
                                    case 3:
                                        if (target.Hits < target.Str / 3)
                                            format_string = string.Format("Almost there!");
                                        else
                                        {
                                            format_string = string.Format("{0}, it's not too late to repent.", target.Name);
                                            mRegion.ClipNextMessageDelay(2);    // we shorten the delay so that the next message looks like a response.
                                            mRegion.ForceAcceptIdleMessage();   // make sure the next message is accepted.
                                        }
                                        break;
                                    case 4:
                                        format_string = string.Format("It is too late.");
                                        break;
                                    case 5:
                                        format_string = string.Format("Take this message back home.");
                                        break;
                                    default:
                                        format_string = string.Format("Take up another trade maybe?");
                                        mRegion.hit_ndx = 0;
                                        break;
                                }
                                me.Say(format_string);
                            }
                            else if (context.HasFlag(InstanceMessage.RegularMessage))
                            {
                                if (target.Hidden)
                                {
                                    switch (mRegion.hidden_ndx++)
                                    {
                                        case 0:
                                            format_string = string.Format("Come out little poppet, show yourself.");
                                            break;
                                        case 1:
                                            format_string = string.Format("I know you're there {0}, show yourself.", target.Name);
                                            break;
                                        case 2:
                                            format_string = string.Format("Be not afeard {0}, I won't hurt you.", target.Name);
                                            break;
                                        case 3:
                                            format_string = string.Format("I have a signed document from Lord British himself.");
                                            mRegion.ClipNextMessageDelay(2);    // we shorten the delay so that the next message looks like a response.
                                            mRegion.ForceAcceptIdleMessage();   // make sure the next message is accepted.
                                            break;
                                        case 4:
                                            format_string = string.Format("Aye, a signed document. Come out and I will show it to thee.");
                                            break;
                                        case 5:
                                            format_string = string.Format("Hidden huh? Well, we'll match your hiding with my tracking.");
                                            break;
                                        default:
                                            format_string = string.Format("Come out!", target.Name);
                                            mRegion.hidden_ndx = 0;
                                            break;
                                    }
                                    me.Say(format_string);
                                }
                                else // not hidden
                                {
                                    switch (mRegion.chatter_ndx++)
                                    {
                                        case 0:
                                            format_string = string.Format("We own the rights to this mine {0}.", target.Name);
                                            mRegion.ClipNextMessageDelay(2);    // we shorten the delay so that the next message looks like a response.
                                            mRegion.ForceAcceptIdleMessage();   // make sure the next message is accepted.
                                            break;
                                        case 1:
                                            format_string = string.Format("Aye, you are trespassing.");
                                            break;
                                        case 2:
                                            format_string = string.Format("You have no right to be here.");
                                            break;
                                        case 3:
                                            format_string = string.Format("I'll show you what we do to trespassers!");
                                            break;
                                        default:
                                            Item held = me.FindItemOnLayer(Layer.OneHanded);
                                            format_string = string.Format("Come a little closer and I'll give you a taste of {0}!", (held != null) ? held.Name : "my fists");
                                            mRegion.chatter_ndx = 0;
                                            break;
                                    }
                                    me.Say(format_string);
                                }
                            }
                        }
                }
                catch
                { }
                finally
                { }
            }
        }
        [Constructable]
        public AngryMiner(string resourceType)
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest | FightMode.Weakest, 10, 1, 0.25, 0.5)
        {
            BardImmune = true;
            m_resourceType = ScriptCompiler.FindTypeByName(resourceType);
            if (m_resourceType == null)
            {
                m_resourceType = ScriptCompiler.FindTypeByName("IronOre");
                Diagnostics.LogHelper logger = new Diagnostics.LogHelper("AngryMinerCreationError.log", false);
                logger.Log(Diagnostics.LogType.Mobile, this, string.Format("Bad resource type '{0}' passed to constructor.", resourceType));
                logger.Finish();
            }
            SpeechHue = Utility.RandomSpeechHue();
            Hue = Utility.RandomSkinHue();

            if (Core.RuleSets.AngelIslandRules())
            {
                SetStr(96, 115);
                SetDex(86, 105);
                SetInt(51, 65);

                SetDamage(23, 27);

                SetSkill(SkillName.Swords, 73.2, 98.5);
                SetSkill(SkillName.Macing, 73.2, 98.5);
                SetSkill(SkillName.Anatomy, 73.2, 98.5);
                SetSkill(SkillName.Tactics, 73.2, 98.5);
                SetSkill(SkillName.MagicResist, 57.5, 95.0);
            }
            else
            {
                SetStr(86, 100);
                SetDex(81, 95);
                SetInt(61, 75);

                SetDamage(10, 23);

                SetSkill(SkillName.Fencing, 66.0, 97.5);
                SetSkill(SkillName.Macing, 65.0, 87.5);
                SetSkill(SkillName.MagicResist, 25.0, 47.5);
                SetSkill(SkillName.Swords, 65.0, 87.5);
                SetSkill(SkillName.Tactics, 65.0, 87.5);
                SetSkill(SkillName.Wrestling, 15.0, 37.5);
            }

            InitBody();
            InitOutfit();

            Fame = 1000;
            Karma = -1000;

            PackItem(new Bandage(Utility.RandomMinMax(10, 25)), lootType: LootType.UnStealable);

            // initialize our chatter
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(ChatterTick), new object[] { null });

            // we are always in our own Compadres list
            Compadres.Add(this);
        }
        [Constructable]
        public AngryMiner()
            : this("IronOre")
        {

        }
        public AngryMiner(Serial serial)
            : base(serial)
        {
            // initialize out chatter
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), new TimerStateCallback(ChatterTick), new object[] { null });
            // we are always in our own Compadres list
            Compadres.Add(this);
        }
        public override void OnDelete()
        {
            base.OnDelete();
        }
        public override Point3D Home
        {
            get
            {
                return base.Home;
            }
            set
            {
                base.Home = value;
                if (m_originalHome == Point3D.Zero)
                    m_originalHome = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile MyConstantFocus
        {
            get { return base.PreferredFocus; }
            set { base.PreferredFocus = value; }

        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D OriginalHome
        {
            get { return m_originalHome; }
            set { m_originalHome = value; }

        }
        [CommandProperty(AccessLevel.GameMaster)]
        public Type ResourceType
        {
            get { return m_resourceType; }
            set { m_resourceType = value; }

        }
        public override bool ClickTitle { get { return false; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : false; } }
        public override bool CanBandage { get { return Core.RuleSets.AngelIslandRules() ? true : base.CanBandage; } }
        private AngryMiner GetRandomCompadre()
        {
            List<AngryMiner> amList = Compadres;
            foreach (AngryMiner am in amList)
            {
                if (AWOLCompadre(am) || am == this)
                    continue;
                else return am;
            }
            return null;
        }
        public override bool Unprovokable
        {
            get
            {   // make sure we're heard
                AngryMinerDialog.FlushNow(GetFocusMob());
                switch (Utility.Random(3))
                {
                    case 0:
                        AngryMiner angryCompadre = GetRandomCompadre();
                        if (angryCompadre != null)
                        {
                            Yelp(1, string.Format("Hey {0}, we have a musician on our hands!", angryCompadre.Name));
                            angryCompadre.Yelp(2, "Oh. Do you know any Aerosmith?");
                        }
                        else
                            Yelp(1, "Your provocation is working!");
                        break;
                    case 1:
                        Yelp(1, "Oh, I'm provoked alright!");
                        break;
                    case 2:
                        Yelp(1, "I do not provoke so easily.");
                        break;
                }
                return BardImmune;
            }
        }
        public override bool Uncalmable
        {
            get
            {   // make sure we're heard
                AngryMinerDialog.FlushNow(GetFocusMob());
                switch (Utility.Random(3))
                {
                    case 0:
                        Yelp(1, string.Format("*I hear lovely music, and decide to keep beating on you with {0}*", MyWeapon));
                        break;
                    case 1:
                        Yelp(1, "That's just Charming.");
                        break;
                    case 2:
                        Yelp(1, "I prefer the chello.");
                        break;
                }
                return BardImmune;
            }
        }
        public override TimeSpan BandageDelay { get { return Core.RuleSets.AngelIslandRules() ? TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)) : base.BandageDelay; } }
        public override void InitBody()
        {
            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
                Title = "Angry Miner Guild";
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
                Title = "Angry Miner Guild";
            }
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            AddItem(new Shirt(Utility.RandomNeutralHue()));
            AddItem((Utility.RandomBool() ? (Item)(new LongPants(Utility.RandomNeutralHue())) : (Item)(new ShortPants(Utility.RandomNeutralHue()))));
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new HalfApron(Utility.RandomNeutralHue()));

            if (Utility.RandomBool())
            {
                GargoylesPickaxe gp = new GargoylesPickaxe();
                gp.Resource = CraftResources.GetFromType(m_resourceType);
                CraftResourceInfo cri = CraftResources.GetInfo(gp.Resource);
                if (cri != null)
                    gp.Hue = cri.Hue;
                gp.Movable = false;
                AddItem(gp);
            }
            else
            {
                SmallWarHammer ash = new SmallWarHammer();
                ash.Resource = CraftResources.GetFromType(m_resourceType);
                CraftResourceInfo cri = CraftResources.GetInfo(ash.Resource);
                if (cri != null)
                    ash.Hue = cri.Hue;
                ash.Movable = false;
                if (Name.EndsWith("s"))
                    ash.Name = Name + "' " + ash.Name;
                else
                    ash.Name = Name + "'s " + ash.Name;
                AddItem(ash);
            }
        }
        public override void OnHideChange(Mobile m, bool hidden)
        {
            base.OnHideChange(m, hidden);
            // If the mobile changed hidden/visible status, clear our chatter queue of now inappropriate messages.
            if (m == GetFocusMob())
                AngryMinerDialog.FlushNow(m as PlayerMobile);
        }
        public override void OnKilledBy(Mobile killed, List<Mobile> aggressors)
        {
            base.OnKilledBy(killed, aggressors);
            // too far away to be ours? (quick test filter)
            if (GetDistanceToSqrt(killed) > RangePerception)
                return;

            // was it a playermobile?
            if (!(killed is PlayerMobile))
                return;

            // now see if we were involved
            bool were_involved = false;
            Mobile killer = null;
            List<AngryMiner> amList = Compadres;
            foreach (AngryMiner am in amList)
                if (aggressors.Contains(am as Mobile))
                {
                    were_involved = true;
                    killer = am;
                    break;
                }
            if (were_involved == false)
                return;

            // see if this context is already being handled
            if (TeamCheckHandled(InstanceMessage.CombatantKilled) == true)
            {
                Console.WriteLine("{0}: This CombatantKilled is already being handled for {1}.", Name, killed.Name);
                // it's being handled
                return;
            }
            else
            {
                Utility.Monitor.WriteLine("{0}: This CombatantKilled has not been handled yet.", ConsoleColor.Red, Name);
            }

            // okay, we're handling this
            iMessage = InstanceMessage.CombatantKilled;
            #region Spawn Dead Miner
            // okay, the miner was killed by one of our Compadres
            // Spawn a dead miner doppelganger if one has not been spawned in 24 hours
            bool spawn = false;
            if (!AngryMinerDialog.Doppelganger.ContainsKey(killed as PlayerMobile))                                 // if we havn't seen him
            {                                                                                                       // add him now
                AngryMinerDialog.Doppelganger[killed as PlayerMobile] = DateTime.UtcNow + TimeSpan.FromHours(6);       // set a 6 hour expiry
                spawn = true;                                                                                       // we will spawn
            }
            else if (DateTime.UtcNow >= AngryMinerDialog.Doppelganger[killed as PlayerMobile])                         // if we have seen him, but the time out has expired
            {
                AngryMinerDialog.Doppelganger[killed as PlayerMobile] = DateTime.UtcNow + TimeSpan.FromHours(6);       // reset the timeout
                spawn = true;                                                                                       // we will spawn
            }
            if (spawn == true)                                                                                      // if we saw him, but the timeout has not expired, we will not spawn
            {                                                                                                       // this timeout logic prevents someone from generating hundreds of Doppelgangers
                // create the dead miner    
                MurderedMiner miner = new MurderedMiner();

                // copy the clothes, jewelry, everything
                Utility.CopyLayers(miner, killed, CopyLayerFlags.Default);

                // now everything else
                miner.Name = killed.Name;
                miner.Hue = killed.Hue;
                miner.Body = (killed.Female) ? 401 : 400;   // get the correct body
                miner.Female = killed.Female;               // get the correct death sound
                miner.Direction = killed.Direction;         // face them the correct way
                miner.LastKiller = killer;                  // let the world know who did them in
                miner.MoveToWorld(killed.Location, killed.Map);
            }
            #endregion

            if (Compadres.Count > 1)
            {
                AngryMinerDialog.FlushNow(killed as PlayerMobile);                   // clear the queue
                AngryMinerDialog.FlushUntil = InstanceMessage.RegularMessage;       // flush other unimportant messages as they arrive
            }

            Yelp(1, string.Format("So {0}, looks like you were no match for {1}.", killed.Name, MyWeapon));

            List<AngryMiner> list = Compadres;
            // now, tell our Angry Miners what has happened
            foreach (AngryMiner angryMiner in list)
            {
                if (angryMiner != this)
                    // direct everyone to the new location. Not exactly, but close
                    angryMiner.Home = Spawner.GetSpawnPosition(this.Map, killed.Location, 4, SpawnFlags.None, this);
                else
                {
                    // our tracker gets the excact location
                    angryMiner.Home = killed.Location;
                }

                // Okay, now send a message to all other angry miner so that they may comment
                if (angryMiner != this && !AWOLCompadre(angryMiner))
                    angryMiner.SendMessage(InstanceMessage.CombatantKilled, new mState(this, InstanceMessage.CombatantKilled, target: killed as PlayerMobile, text: null));
            }
            // now send a special chat system message to clear our 'CombatantKilled' flag
            AngryMinerDialog.PostMessage(this, AngryMinerDialog.GetChatterDelay, new mState(this, InstanceMessage.Clear, target: killed as PlayerMobile, text: null));
        }
        private string MyWeapon
        {
            get
            {
                Item item = this.FindItemOnLayer(Layer.OneHanded);
                if (item != null)
                    return item.Name;
                else
                    return "my fists";
            }
        }
        private void SyncHome(Mobile miner)
        {
            foreach (AngryMiner angryMiner in Compadres)
            {
                if (!AWOLCompadre(angryMiner))
                {
                    // direct everyone to the new location. Not exactly, but close
                    angryMiner.Home = Spawner.GetSpawnPosition(this.Map, miner.Location, 4, SpawnFlags.None, this);
                }
            }
        }
        private bool TakingXanax()
        {   // we need to tone down the AI for the dungeon version of AM's too many complaints
            // Basically TakingXanax helps AMs to forget their preferred focus more quickly
            if (this.Region != null && this.Region.IsDungeonRules)
                return true;
            else
                return false;
        }
        public override void OnSee(Mobile m)
        {
            // sync all angry miner's home with the last knon location of the preferred focus
            if (m is PlayerMobile && m == PreferredFocus && m.Alive)
                SyncHome(m);

            // lock onto the first player you see if our ConstantFocus is not available. It may not be our desired miner, but it will do.
            if ((PreferredFocus == null || PreferredFocus.NetState == null || !PreferredFocus.Alive || GetDistanceToSqrt(PreferredFocus) > 25) &&
                (m is PlayerMobile) && (m as PlayerMobile).Alive && !(m as PlayerMobile).Blessed)
                // our new ConstantFocus
                PreferredFocus = m;

            // if we see our target, they are no longer hiding, so clear the tracking flag
            if (PreferredFocus == m)
                TeamClearHandled(InstanceMessage.Tracking);

            // see if we have a new Compadre, and if so, add him to our list of Compadres and inform him
            //  of our preferred focus
            if (m is AngryMiner)
                if (!Compadres.Contains(m as AngryMiner))
                    DoNewCompadreInfo(m as AngryMiner);

            base.OnSee(m);
        }
        private bool MinimumAge(Mobile m, TimeSpan min)
        {
            TimeSpan ts = DateTime.UtcNow - m.Created;
            return ts >= min;
        }
        private void DoNewCompadreInfo(AngryMiner new_miner)
        {
            Compadres.Add(new_miner);                             // add our new Compadre

            // you are too new to be informing others of our preferred focus
            if (!MinimumAge(this, TimeSpan.FromMinutes(1)))
                return;

            // if we don't have a preferred focus, there is nothing to share
            // if our new miner has a preferred focus, he may not so new, but simply wondered out of range
            if (PreferredFocus != null && new_miner.PreferredFocus == null)
            {
                if (PreferredFocus.Hidden)
                {
                    // check with our team to see if anyone else is handling this
                    if (TeamCheckHandled(InstanceMessage.NewCompadreInfo) == true)
                    {
                        Console.WriteLine("{0}: This NewCompadreInfo is already being handled for {1}.", Name, new_miner.Name);
                        // it's being handled
                        return;
                    }
                    else
                    {
                        Utility.Monitor.WriteLine("{0}: This NewCompadreInfo has not been handled yet.", ConsoleColor.Red, Name);
                    }
                    // we're handling this message
                    iMessage = InstanceMessage.NewCompadreInfo;
                    AngryMinerDialog.FlushUntil = InstanceMessage.RegularMessage;       // flush other unimportant messages as they arrive
                    AngryMinerDialog.FlushNow(GetFocusMob());                           // clear the queue
                    // let this new Compadre know about our preferred focus
                    Yelp(1, string.Format("{0}, {1} is hiding around here somewhere.", new_miner.Name, PreferredFocus.Name));
                    // tell our new Compadre about our preferred focus, let them know who we are so that they can reply to us
                    new_miner.PreferredFocus = PreferredFocus;
                    new_miner.SendMessage(InstanceMessage.NewCompadreInfo, new mState(this, InstanceMessage.NewCompadreInfo, target: PreferredFocus as PlayerMobile, text: null));
                    // now send a special chat system message to clear our 'NewCompadreInfo' flag
                    AngryMinerDialog.PostMessage(this, AngryMinerDialog.GetChatterDelay, new mState(this, InstanceMessage.Clear, target: GetFocusMob(), text: null));
                }
            }
        }
        private void Yelp(int delay, string text)
        {
            AngryMinerDialog.PostMessage(this, delay, new mState(this, InstanceMessage.Yelp, target: GetFocusMob(), text: text));
        }
        public override void OnHit(Mobile attacker, Mobile defender)
        {
            base.OnHit(attacker, defender);
            if (attacker == this)
                AngryMinerDialog.PostMessage(this, 1.0, new mState(this, InstanceMessage.OnHit, target: GetFocusMob(), text: null));
        }
        DateTime m_lastThought = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        public override void OnThink()
        {
            base.OnThink();

            // every 5 minutes, check to see if we should give up and go home.
            //   our original home, where we were spawned - our camp
            if (DateTime.UtcNow > m_lastThought)
            {
                if (TakingXanax())
                    m_lastThought = DateTime.UtcNow + TimeSpan.FromSeconds(30);
                else
                    m_lastThought = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                if (GetFocusMob() == null)
                {   // no focus mob? Go home.

                    if (TakingXanax())
                        PreferredFocus = null;

                    Home = m_originalHome;
                    if (GetDistanceToSqrt(Home) > RangeHome)
                    {
                        Utility.Monitor.WriteLine("{0}: Giving up, going home. ({1})", ConsoleColor.Red, Name, Home);
                    }
                }
            }
        }
        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            base.AggressiveAction(aggressor, criminal, source);
            if (aggressor == null)
                return;
            if (source != null)
            {
                if (source.ToString().ToLower().Contains("discordance"))
                {
                    AngryMinerDialog.FlushNow(GetFocusMob());
                    switch (Utility.Random(3))
                    {
                        case 0:
                            AngryMiner angryCompadre = GetRandomCompadre();
                            if (angryCompadre != null)
                            {
                                Yelp(1, string.Format("{0}, we've got a bard here. What are we going to do with {1}?", angryCompadre.Name, aggressor.Female ? "her" : "him"));
                                angryCompadre.Yelp(2, string.Format("Oh I dunno, kill {0}?", aggressor.Female ? "her" : "him"));
                            }
                            else
                                Yelp(1, "Discordance? does anyone still use that?");
                            break;
                        case 1:
                            Yelp(1, string.Format("*{0} looks weak and helpless*", this.Name));
                            break;
                        case 2:
                            Yelp(1, string.Format("I'll take your discordance under consideration. Now meet {0}", MyWeapon));
                            break;
                    }
                }
            }
        }
        private PlayerMobile GetFocusMob()
        {
            PlayerMobile[] targets = new PlayerMobile[] { PreferredFocus as PlayerMobile, Combatant as PlayerMobile };
            foreach (PlayerMobile target in targets)
                // if our combatant is a living player, and we're close to our combatant!
                if (target != null && target.Alive && target.Player && NearOurCombatant(target))
                    return target;
            return null;
        }
        protected bool NearOurCombatant(Mobile focus)
        {
            if (focus == null) return false;
            if (focus.NetState == null) return false;
            if (TakingXanax() && focus.Hidden) return false;
            return this.GetDistanceToSqrt(focus) <= RangePerception;
        }
        protected bool FarFromOurCombatant(Mobile focus)
        {
            return !NearOurCombatant(focus);
        }
        protected bool AWOLCompadre(AngryMiner am)
        {
            return am == null || am.Deleted || GetDistanceToSqrt(am) > RangePerception;
        }
        private void ChatterTick(object state)
        {
            if (this.Deleted || !this.Alive)
                // done, no more timers
                return;

            // on each tick, process the chatter queue
            AngryMinerDialog.DoChatter();

            // do idle chatter
            AngryMinerDialog.PostIdleMessage(Compadres, AngryMinerDialog.GetChatterDelay, new mState(this, InstanceMessage.RegularMessage, target: GetFocusMob(), text: null));

            // and finally, schedule another chatter tick (this function)
            Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(ChatterTick), new object[] { null });
        }
        private bool TeamClearHandled(InstanceMessage filter)
        {
            List<AngryMiner> list = Compadres;

            // check to see if this message is being handled by anyone else
            int count = 0;
            foreach (AngryMiner angryMiner in list)
            {
                if (angryMiner.iMessage.HasFlag(filter))
                {
                    angryMiner.iMessage = InstanceMessage.RegularMessage;
                    count++;
                    if (count > 1)
                    {
                        Utility.Monitor.WriteLine("{0}: More than one angry miner contains flag {1}.", ConsoleColor.Red, this.Name, filter.ToString());
                    }
                }
            }

            if (count > 0)
            {
                Utility.Monitor.WriteLine("{0}: Clearing {1} attribute.", ConsoleColor.Red, this.Name, filter.ToString());
            }

            return count > 0;
        }
        private List<AngryMiner> CleanCompadres()
        {
            List<AngryMiner> list = new List<AngryMiner>();
            foreach (AngryMiner found in m_compadres)
                // if our old Compadre has been deleted, remove them from our list
                if (found.Deleted)
                    list.Add(found);

            // remove all deleted angry miners
            foreach (AngryMiner found in list)
                m_compadres.Remove(found);

            return m_compadres;
        }
        private bool TeamCheckHandled(InstanceMessage filter)
        {
            List<AngryMiner> list; list = Compadres;

            // first check to see if this message is being handled by anyone else
            foreach (AngryMiner angryMiner in list)
                if (angryMiner.iMessage.HasFlag(filter))
                    // already being handled
                    return true;

            return false;
        }
        static DateTime m_nextOnMovement = DateTime.UtcNow;
        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (DateTime.UtcNow > m_nextOnMovement)
            {
                m_nextOnMovement = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                DoOnMovement(m, oldLocation);
            }

            // build a list of nearby angry miners - Compadres
            //  We do this instead of always enumerating mobiles in the area as I assume this is faster, it's also free
            if (m is AngryMiner)
                if (!Compadres.Contains(m as AngryMiner))
                    Compadres.Add(m as AngryMiner);     // add this new member to our club
        }
        public void DoOnMovement(Mobile m, Point3D oldLocation)
        {
            #region setup

            // only want living hidden players, not blessed
            if (!((m is PlayerMobile) && (m as PlayerMobile).Hidden && (m as PlayerMobile).Alive && !(m as PlayerMobile).Blessed))
                return;

            // is this the guy we're looking for?
            if (m != PreferredFocus && m != Combatant)
                return;

            // no tracking if we're TakingXanax
            if (TakingXanax())
                return;

            Point3D location;
            Mobile miner;
            // we must have a preferred focus or combatant to pull this off
            if (PreferredFocus != null && PreferredFocus.Alive && PreferredFocus.Player)
            {
                location = PreferredFocus.Location;
                miner = PreferredFocus;
            }
            else if (Combatant != null && Combatant.Alive && Combatant.Player)
            {
                location = Combatant.Location;
                miner = Combatant;
            }
            else return;

            // our guy has 'tracking'
            // check and see if the miner is movong away from us. If so, we will alert the crew
            // Note Home is the last known location of the miner - updated periodically, whenever we see him.
            // Note: We also want to be close enough to legitimately track this guy.
            if (miner.GetDistanceToSqrt(Home) < 5 || this.GetDistanceToSqrt(miner) > this.RangePerception)
                return;

            // check with our team to see if anyone else is handling this
            if (TeamCheckHandled(InstanceMessage.Tracking) == true)
            {
                Console.WriteLine("{0}: This tracking is already being handled.", Name);
                // it's being handled
                return;
            }
            else
            {
                Utility.Monitor.WriteLine("{0}: This tracking has not been handled yet.", ConsoleColor.Red, Name);
            }

            // we're handling this message
            iMessage = InstanceMessage.Tracking;
            #endregion

            if (Compadres.Count > 1)
            {
                AngryMinerDialog.FlushNow(miner as PlayerMobile);                   // clear the queue
                AngryMinerDialog.FlushUntil = InstanceMessage.RegularMessage;       // flush other unimportant messages as they arrive
                Yelp(1, string.Format("Oye! I've got {0} on tracking. Follow me.", miner.Name));
            }
            else
                Yelp(1, string.Format("Oye! I got you on tracking {0}.", miner.Name));

            // now, tell our Angry Miners where to go and who to attack
            foreach (AngryMiner angryMiner in Compadres)
            {
                if (angryMiner != this)
                    // direct everyone to the new location. Not exactly, but close
                    angryMiner.Home = Spawner.GetSpawnPosition(this.Map, miner.Location, 4, SpawnFlags.None, this);
                else
                    // our tracker gets the excact location
                    angryMiner.Home = miner.Location;

                // let's boogie!
                if (angryMiner.AIObject != null)
                    angryMiner.AIObject.RunTo(miner, true);

                // tell them who their focus is
                angryMiner.PreferredFocus = miner;

                // Okay, now send a message to all other angry miners. Let them know 'I' am tracking the miner and that they should all follow me
                if (angryMiner != this && !AWOLCompadre(angryMiner))
                    angryMiner.SendMessage(InstanceMessage.Tracking, new mState(this, InstanceMessage.Tracking, target: miner as PlayerMobile, text: null));
            }
            // now send a special chat system message to clear our 'tracking' flag
            AngryMinerDialog.PostMessage(this, AngryMinerDialog.GetChatterDelay, new mState(this, InstanceMessage.Clear, target: GetFocusMob(), text: null));
        }
        #region Send/Receive Message
        [Flags]
        public enum InstanceMessage : uint
        {
            None = 0,
            RegularMessage = 0b0000_0000_0000_0001,
            Tracking = 0b0000_0000_0000_0010,
            Clear = 0b0000_0000_0000_0100,
            Joke = 0b0000_0000_0000_1000,
            OnHit = 0b0000_0000_0001_0000,
            CombatantKilled = 0b0000_0000_0010_0000,
            Yelp = 0b0000_0000_0100_0000,
            NewCompadreInfo = 0b0000_0000_1000_0000,
            All = uint.MaxValue
        }
        public void SendMessage(InstanceMessage msg, mState args)
        {
            ReceiveMessage(msg, args);
        }
        public void ReceiveMessage(InstanceMessage msg, mState args)
        {
            AngryMinerDialog.PostMessage(this, Utility.RandomMinMax(1, 3), args);
        }
        #endregion Send/Receive Message
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AllShards)
            {
                if (Spawning && !Core.RuleSets.AngelIslandRules())
                {
                    PackGold(100, 150);
                }
                else
                {
                    if (Core.RuleSets.AngelIslandRules())
                        PackGold(100, 150);

                    Item item;
                    if (Utility.Chance(0.05))
                    {
                        item = this.FindItemOnLayer(Layer.OneHanded);
                        if (item != null)
                        {
                            item.Movable = true;
                            item.LootType = LootType.Rare;
                            PackItem(item);
                        }
                    }

                    int count = Utility.RandomList(2, 3);
                    for (int ix = 0; ix < count; ix++)
                    {   // pack some ore
                        item = Activator.CreateInstance(m_resourceType) as Item;
                        if (item != null)
                            PackItem(item);
                    }
                }
            }
            else
            {   // doesn't apply - AI custom mob
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020207053514/uo.stratics.com/hunters/brigand.shtml
                    // 100 - 200 Gold, Clothing

                    if (Spawning)
                    {
                        PackGold(100, 200);
                    }
                }
                else
                {
                    AddLoot(LootPack.Average);
                }
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_originalHome);
            if (m_resourceType == null)
                writer.Write("AgapiteOre");
            else
                writer.Write(m_resourceType.Name);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_originalHome = reader.ReadPoint3D();
            string resourceType = reader.ReadString();
            m_resourceType = ScriptCompiler.FindTypeByName(resourceType);
        }
    }
}