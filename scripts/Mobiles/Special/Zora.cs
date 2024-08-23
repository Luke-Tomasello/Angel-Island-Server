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

/* Scripts\Mobiles\Special\Zora.cs
 * ChangeLog
 *  12/7/22, Adam (CanOverrideAI)
 *      Add a new CanOverrideAI property to BaseCreature which tells at least the 'Exhibit' Spawner
 *      if this creature may have its AI overridden. By default, the Exhibit spawner
 *          sets the AI to Animal.
 *  12/21/21, adam (vials)
 *      fixed the typo
 *  12/11/21, Adam (ZoraDatabaseDeserialize)
 *      Handle the case when we read back a quest object where the player is null
 *      In this case, we still create the quest object (with a default PlayerManager() object) 
 *      as it will get cleaned up by the system in due time, including any referenced items (instruments or weapons.)
 *  11/18/21, Adam
 *      Add support for slayer instruments
 *      Add PlayerNotify() to let the player know their weapon/instrument is ready.
 *      Fix a bug where Zora was excluding all instruments where the craft meteral was not 'none'
 *      Zora will now accept any metal as long as it isn't magic
 *  11/13/21, Adam (ZoraDatabaseDeserialize())
 *      Load the zora database if zora is ever respawned.
 * 11/6/21, Adam (ZoraDatabaseDeserialize)
 *  Check for null mobile before adding them back to the database
 *  11/2/2021, adam
 *      Zora now accepts RawFishSteak as well as FishSteak
 *      Added logging to OnDragDrop
 *      More keyword processing based on state
 *	10/26/10, adam
 *		created.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Server.Mobiles
{
    [CorpseName("a sea serpents corpse")]
    [TypeAlias("Server.Mobiles.Seaserpant")]
    public class Zora : SeaSerpent
    {
        private static bool m_lock = false;
        private bool m_valid = false;
        private bool m_databaseLoaded = false;
        // Zora is now an Exhibit creature, and as such, the spawner will try to set her AI => AI_Animal
        //  this is no good as she needs to be AIType.AI_Vendor for correctly processing speech.
        public override bool CanOverrideAI { get { return false; } }
        [Constructable]
        public Zora()
            : base()
        {
            if (m_lock == false)
            {
                m_lock = true;
                m_valid = true;
                Name = "Zora";
                // made 'Exhibit' by the spawner (no need to bless)
                AI = AIType.AI_Vendor;
                NameHue = 0x35;
                RangePerception = 20; // pond is like 18 across + one tile each side

                // This is only the case if Zora was 'respawned'. otherwise she loads the database during Deserialize
                if (m_databaseLoaded == false)
                    ZoraDatabaseDeserialize();

                Timer.DelayCall(TimeSpan.FromMinutes(5), new TimerCallback(PlayerNotify));
            }
            else
            {   // second functional copies of Zora are not allowed since they would be 
                //  sharing the same quest database
                Name = "Zora (faux)";
                //Blessed = true;
                IsInvulnerable = true;
                AI = AIType.AI_Vendor;
                NameHue = 0x612;
                RangePerception = 20; // pond is like 18 across + one tile each side
            }
        }
        public void PlayerNotify()
        {
            if (m_questerDatabase.Count > 0)
            {
                foreach (KeyValuePair<Mobile, Quest> kvp in m_questerDatabase)
                {
                    if (kvp.Key != null && kvp.Key.NetState != null)
                    {
                        if (!kvp.Value.Notified && kvp.Value.AllItems && kvp.Value.QuestWaitingPeriodOver())
                        {
                            kvp.Value.Notified = true;
                            kvp.Key.NetState.Send(new AsciiMessage(kvp.Key.Serial, kvp.Key.Body,
                                MessageType.Regular, 0, 3, kvp.Key.Name, string.Format("*You feel Zora may have finished your {0}*", kvp.Value.Type.ToString().ToLower())));
                        }
                    }
                }
            }
            Timer.DelayCall(TimeSpan.FromMinutes(5), new TimerCallback(PlayerNotify));
        }
        public enum MachineState
        {
            invalid,
            looking,                    // looking for someone to talk to
            approaching,                // approaching the player of current interest
            considering,                // sizing up player
            stingy,                     // when given less that 20 fish steaks
            interacting,                // we are interacting with player
            requestingSlayer,           // they's asked for a slayer
            requestingSlayerComplete,   // waiting for the player to return with the items
            requestedItemsComplete,     // all items have been delivered
            wantsSlayerNow,             // player has come back looking for their slayer
            idleTimeout,                // the player with Zora's attention has been idle too long.
            whatKindOfSlayer,           // the player needs to tell us what kind of slayer they want (weapon or instrument)
        };
        public class Quest
        {
            public enum QuestType
            {
                None,
                Weapon,
                Instrument
            }
            private bool m_notified = false;    // no need to serialize
            public bool Notified { get { return m_notified; } set { m_notified = value; } }
            private Quest.QuestType m_type = Quest.QuestType.None;
            public Quest.QuestType Type { get { return m_type; } set { m_type = value; } }
            private DateTime m_questStart;
            public DateTime QuestStart { get { return m_questStart; } set { m_questStart = value; } }
            private DateTime m_waitingStart = DateTime.MinValue;
            public DateTime WaitingStart { get { return m_waitingStart; } set { m_waitingStart = value; } }
            private bool m_hasBloodVials = false;
            public bool HasBloodVials { get { return m_hasBloodVials; } set { m_hasBloodVials = value; } }
            bool m_hasFishSteaks = false;
            public bool HasFishSteaks { get { return m_hasFishSteaks; } set { m_hasFishSteaks = value; } }
            public bool HasItem { get { return m_item != null; } }
            Item m_item = null;
            public Item Item { get { return m_item; } set { m_item = value; } }
            SlayerName m_slayerName = SlayerName.None;
            public SlayerName SlayerName { get { return m_slayerName; } set { m_slayerName = value; } }
            int m_soundID = 0;
            public int SoundID { get { return m_soundID; } set { m_soundID = value; } }
            public bool AllItems { get { return HasBloodVials && HasFishSteaks && HasItem; } }
            private PlayerManager m_playerManager = null;
            public PlayerManager PlayerManager { get { return m_playerManager; } }
            public Quest(PlayerManager pm)
            {
                m_questStart = DateTime.UtcNow;
                m_playerManager = pm;
                m_type = pm.Type;
            }
            ~Quest()
            {   // if they have not completed the quest, delete the weapon/instrument
                if (AllItems == false && m_item != null && m_item.Deleted == false)
                    m_item.Delete();
            }
            public void Update()
            {   // sync what the PlayerManager knows about the quest since we don't serialize the PlayerManager
                // More info: PlayerManagers exist before the quest is created and must carry some information
                //  about the upcoming quest, like for instance, what Type of quest it is. This data is moved to the quest
                //  once it's created, and back again during ZoraDatabaseDeserialize via quest.Update().
                this.PlayerManager.Type = this.Type;
            }
            public bool CheckWeapon(Items.BaseWeapon weapon)
            {
                // no uber slayers - they are already uber!
                // we will allow Quality.Exceptional and just remove that attribute)
                //  (since it's hard to craft regular once you reach high levels of skill.
                if (weapon.PlayerCrafted == false)
                    return false;
                if (weapon.Slayer != SlayerName.None)
                    return false;
                if (weapon.DamageLevel != WeaponDamageLevel.Regular)
                    return false;
                if (weapon.AccuracyLevel != WeaponAccuracyLevel.Regular)
                    return false;
                if (weapon.DurabilityLevel != WeaponDurabilityLevel.Regular)
                    return false;

                return true;
            }
            public bool CheckInstrument(Items.BaseInstrument instrument)
            {
                // no uber slayers - they are already uber!
                // we will allow Quality.Exceptional and just remove that attribute
                //  (since it's hard to craft regular once you reach high levels of skill.)
                if (instrument.PlayerCrafted == false)
                    return false;
                if (instrument.Slayer != SlayerName.None)
                    return false;

                return true;
            }
            public int CheckFishQuantity(Item fish)
            {
                if (fish is Items.Fish)
                    return fish.Amount * 4; // each fish has 4 steaks
                else if (fish is Items.FishSteak || fish is Items.RawFishSteak)
                    return fish.Amount;
                else
                    return 0;
            }
            public bool CheckFish(Item fish)
            {
                return CheckFishQuantity(fish) == 400;
            }
            public bool CheckBlood(Items.SlayerBlood blood)
            {
                if (blood.Amount != 16)
                    return false;
                if (blood.SlayerName == SlayerName.None)
                    return false;
                return true;
            }
            public bool QuestExpired()
            {   // 7 days to finish the quest
                return DateTime.UtcNow - m_questStart > TimeSpan.FromDays(7);
            }
            public bool QuestWaitingPeriodOver()
            {
                if (Core.UOTC_CFG || Core.Debug)
                    // 5-10 minute waiting period before you may pick up your magic item (TestCenter/Debug)
                    return DateTime.UtcNow > (m_waitingStart + TimeSpan.FromMinutes(Utility.RandomMinMax(5, 10)));

                // 2-4 hour waiting period before you may pick up your magic item
                return DateTime.UtcNow > (m_waitingStart + TimeSpan.FromMinutes(Utility.RandomMinMax(60 * 2, 60 * 4)));
            }
        }
        public class PlayerManager
        {
            #region context data
            // this data is learned before the quest starts, and is passed to the Quest object
            private Quest.QuestType m_type = Quest.QuestType.None;
            public Quest.QuestType Type { get { return m_type; } set { m_type = value; } }
            #endregion context data
            private Mobile m_player = null;                                                             // initial target to try an talk to
            public Mobile Player { get { return m_player; } }
            private Point3D m_oldLocation;
            public Point3D OldLocation { get { return m_oldLocation; } set { m_oldLocation = value; } }
            private DialogManager m_dialog;
            public DialogManager Dialog { get { return m_dialog; } }
            public PlayerManager(Mobile m, Mobile zora)
            {
                m_dialog = new DialogManager(m, zora);
                m_player = m;
                m_oldLocation = m_player.Location;
            }
            public PlayerManager()
            {   // empty
                m_dialog = new DialogManager(null, null);
                m_player = null;
                m_oldLocation = Point3D.Zero;
                m_type = Quest.QuestType.None;
            }
        }
        public class DialogManager
        {
            private Mobile m_player = null;
            private Mobile m_zora = null;
            private Dictionary<string, DateTime> m_saidDatabase = null;
            public Dictionary<string, DateTime> SaidDatabase { get { return m_saidDatabase; } }
            private DateTime m_lastSpeech;
            public DateTime LastSpeech { get { return m_lastSpeech; } }
            public DialogManager(Mobile m, Mobile zora)
            {
                m_saidDatabase = new Dictionary<string, DateTime>();
                m_lastSpeech = DateTime.UtcNow;
                m_player = m;
                m_zora = zora;
            }
            public bool Match(string text, string matches, string doesntMatch = null)
            {   // matches word|word|word but not word|word|word
                // more clear than regex, and far simplier syntax - less chance of syntactical errors

                // see if we can match one of these words (included word)
                bool foundMatch = false;
                string[] matchTab = matches.Split(new char[] { '|' });
                foreach (string sx in matchTab)
                    if (text.ToLower().Contains(sx.ToLower()))
                    {
                        foundMatch = true;
                        break;
                    }

                if (doesntMatch == null)
                    return foundMatch;

                // see if a word is found that negates the previous find (and doesn't include word)
                bool foundDoesntMatch = false;
                matchTab = doesntMatch.Split(new char[] { '|' });
                foreach (string sx in matchTab)
                    if (text.ToLower().Contains(sx.ToLower()))
                    {
                        foundDoesntMatch = true;
                        break;
                    }

                if (foundDoesntMatch == true)
                    return false;
                else
                    return foundMatch;
            }
            public bool HaveSaid(string text)
            {
                return m_saidDatabase.ContainsKey(text);
            }
            private string SubStringParser(string text)
            {
                int begin = text.IndexOf('[');
                int end = text.IndexOf(']');
                if (begin == -1 || end == -1)
                    return text;

                return text.Substring(begin + 1, end - 1);
            }
            private string StripFormatting(string text)
            {
                return text.Replace("]", "").Replace("[", "");
            }
            public bool MeteredDialog(string text, double delay, bool emote = false)
            {
                // Our metered dialog allows for substrings to be tracked instead of the whole string.
                // We use square brackets to denote the substring to *keep* Example:
                //  "[I can speak with you now] Adam Ant!"
                //  We some times include the name of the player, other times not, but we want to only
                //  track the bracked portion.
                string toRemember = SubStringParser(text);
                text = StripFormatting(text);
                if (m_zora is Zora zora)
                {
                    if (!m_saidDatabase.ContainsKey(toRemember))
                    {
                        if (emote == true)
                        {
                            m_zora.EmoteHue = 0x35;
                            m_zora.Emote(text);
                            zora.Logger(m_player, text);
                        }
                        else
                        {
                            m_zora.SayTo(m_player, text);
                            zora.Logger(m_player, text);
                        }

                        m_saidDatabase.Add(toRemember, DateTime.UtcNow);
                        m_lastSpeech = DateTime.UtcNow + TimeSpan.FromSeconds(delay);
                        // if we are talking, refresh the idle timeout (not the player's fault if we are blabbering!)
                        zora.RefreshIdleTimer();
                        return true;
                    }
                }
                else
                    return false;
                return false;
            }
            public bool SaidRecently(string text, TimeSpan ts)
            {
                foreach (KeyValuePair<string, DateTime> kvp in m_saidDatabase)
                    if (kvp.Key.Contains(text))
                        if (DateTime.UtcNow - kvp.Value <= ts)
                            return true;

                return false;
            }
        }
        // players that have joined the quest - serialized
        private Dictionary<Mobile, Quest> m_questerDatabase = new Dictionary<Mobile, Quest>();
        // players that may or may not have joined the quest - not serialized
        private Dictionary<Mobile, PlayerManager> m_allParticipantsQuesterDatabase = new Dictionary<Mobile, PlayerManager>();
        MachineState m_state = MachineState.looking;
        private Mobile m_target = null;
        private Mobile TargetMob { get { return m_target; } set { m_target = value; } }
        private Memory m_mobileIgnore = new Memory();
        private bool IsFish(Item item)
        {
            return (item is Items.FishSteak || item is Items.RawFishSteak || item is Items.Fish);
        }
        public int CheckFishQuantity(Item fish)
        {
            if (fish is Items.Fish)
                return fish.Amount * 4; // each fish has 4 steaks
            else if (fish is Items.FishSteak || fish is Items.RawFishSteak)
                return fish.Amount;
            else
                return 0;
        }
        public void RefreshIdleTimer()
        {   // called from DragDrop, while we are swimming, and talking.
            // don't penalize the player for idleness while we are doing these things
            ManageMachineState.Refresh();
        }
        public static class ManageMachineState
        {
            private static MachineState m_previousState = MachineState.invalid;
            private static Memory m_stateTimeout = new Memory();
            private static object m_semaphore = null;
            private static int m_idleTimeout = 2 * 60;                        // 2 minute idle timeiout. (If a player has been in this state this long, or we can't reach a player)
            public static void Refresh()
            {   // refresh the timeout on this pbject
                if (m_semaphore != null)                                    // certain interactions take longer than others
                    m_stateTimeout.Refresh(m_semaphore);                    // extend (refresh) the timeout on this object.
            }
            public static MachineState CheckTimeout(MachineState state)
            {
                if (state != m_previousState)
                {
                    if (m_semaphore != null)
                        m_stateTimeout.Forget(m_semaphore);                 // cleanup old semaphore
                    m_semaphore = null;                                     // tell the c# garbage collector it's cool to delete this
                    m_semaphore = new object();                             // semaphore                           
                    m_stateTimeout.Remember(m_semaphore, m_idleTimeout);    // set a new timeout
                    m_previousState = state;                                // record previous state
                    return state;                                           // all is well. Start a timer and just continue
                }
                else if (m_stateTimeout.Recall(m_semaphore) != null)
                {   // we have not yet timed out. Just keep going with this state
                    return state;
                }
                else if (state == MachineState.looking)
                {
                    // we can't go idle while looking
                    return MachineState.looking;
                }
                else
                {   // the player has been idle in this state too long. Time out, and enter timeout state
                    // entering the timeout state will simply ignore this player for 20 seconds.
                    return MachineState.idleTimeout;
                }
            }
        }
        public void Logger(Mobile m, string text)
        {
            LogHelper logger = new LogHelper("Zora.log", false, true);
            if (m != null)
                logger.Log(LogType.Text, string.Format("{0}: {1}", m, text));
            else
                logger.Log(LogType.Text, text);
            logger.Finish();
        }
        public override void OnThink()
        {
            switch (ManageMachineState.CheckTimeout(m_state))
            {
                case MachineState.looking:
                    m_state = DoLooking();
                    break;
                case MachineState.approaching:
                    m_state = DoApproaching();
                    break;
                case MachineState.considering:
                    m_state = DoConsidering();
                    break;
                case MachineState.stingy:
                    m_state = DoStingy();
                    break;
                case MachineState.interacting:
                    m_state = DoInteracting();
                    break;
                case MachineState.requestingSlayer:
                    m_state = DoRequestingSlayer();
                    break;
                case MachineState.requestingSlayerComplete:
                    m_state = DoRequestingSlayerComplete();
                    break;
                case MachineState.requestedItemsComplete:
                    m_state = DoRequestedItemsComplete();
                    break;
                case MachineState.wantsSlayerNow:
                    m_state = DoWantsSlayerNow();
                    break;
                case MachineState.idleTimeout:
                    m_state = DoIdleTimeout();
                    break;
                case MachineState.whatKindOfSlayer:
                    m_state = DoWhatKindOfSlayer();
                    break;
            }
        }
        MachineState DoIdleTimeout()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                pm.Dialog.SaidDatabase.Clear();
                return MachineState.looking;
            }

            DebugPrint("My target has been idle too long.");
            // so the player is just standing here keeping Zora from visiting other players.
            // add this player to the ignore memory so that we may service other customers.
            // we only use a very short timer since we only ignore them while the next mobile enumeration takes place 
            m_mobileIgnore.Remember(pm.Player, 5);

            // indicate to the player that we are bored and moving on
            pm.Dialog.MeteredDialog("*bored*", 0, true);

            ClearStateData();
            pm.Dialog.SaidDatabase.Clear();

            // just go back to 'looking'
            return MachineState.looking;
        }
        MachineState DoRequestedItemsComplete()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                pm.Dialog.SaidDatabase.Clear();
                return MachineState.looking;
            }
            if (pm.Player.Location != pm.OldLocation)
            {   // they moved, go find them
                DebugPrint("My target moved, I will try to find them.");
                pm.OldLocation = pm.Player.Location;    // their old location
                Home = pm.Player.Location;              // their location
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.requestedItemsComplete;
            }
            if ((int)this.GetDistanceToSqrt(pm.Player.Location) <= 3)
            {   // we are there
                DebugPrint(string.Format("I'm ready to chat with {0}.", pm.Player));
                if (DateTime.UtcNow > pm.Dialog.LastSpeech)
                {
                    string text = "You have completed your end of the bargan.";
                    if (pm.Dialog.MeteredDialog(text, 2))
                        return MachineState.requestedItemsComplete;

                    text = "Give me some time, I have many items to craft.";
                    if (pm.Dialog.MeteredDialog(text, 5))
                        return MachineState.requestedItemsComplete;

                    text = "When you return, don't forget fish steaks for ol' Zora.";
                    if (pm.Dialog.MeteredDialog(text, 3))
                        return MachineState.requestedItemsComplete;

                    text = "You know, because I'm always hungry!.";
                    if (pm.Dialog.MeteredDialog(text, 7))
                    {   // ignore this player for 20 seconds so we can find another player
                        m_mobileIgnore.Remember(pm.Player, 20);
                        return MachineState.requestedItemsComplete;
                    }

                    if (m_questerDatabase.ContainsKey(pm.Player))
                        m_questerDatabase[pm.Player].WaitingStart = DateTime.UtcNow;   // start the waiting period

                    return MachineState.looking;
                }
            }
            return MachineState.requestedItemsComplete;
        }
        MachineState DoWhatKindOfSlayer()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.looking;
            }
            if (pm.Player.Location != pm.OldLocation)
            {   // they moved, go find them
                DebugPrint("My target moved, I will try to find them.");
                pm.OldLocation = pm.Player.Location;    // their old location
                Home = pm.Player.Location;              // their location
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.approaching;
            }
            if ((int)this.GetDistanceToSqrt(pm.Player) <= 3)
            {   // we are there
                DebugPrint(string.Format("I am chatting with {0}.", pm.Player));
                string text = null;                     // find out what they want
                text = "Do you wish a weapon or an instrument?";
                pm.Dialog.MeteredDialog(text, 0);
            }
            return MachineState.interacting;
        }
        MachineState DoWantsSlayerNow()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                pm.Dialog.SaidDatabase.Clear();
                return MachineState.looking;
            }
            if (pm.Player.Location != pm.OldLocation)
            {   // they moved, go find them
                DebugPrint("My target moved, I will try to find them.");
                pm.OldLocation = pm.Player.Location;    // their old location
                Home = pm.Player.Location;              // their location
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.approaching;
            }
            if ((int)this.GetDistanceToSqrt(pm.Player.Location) <= 3)
            {   // we are there
                DebugPrint(string.Format("I'm ready to chat with {0}.", pm.Player));
                if (m_questerDatabase.ContainsKey(pm.Player))
                {
                    if (m_questerDatabase[pm.Player].QuestWaitingPeriodOver())
                    {   // the waiting pariod is over! Give them their weapon or instrument.
                        if (m_questerDatabase[pm.Player].Type == Quest.QuestType.Weapon)
                        {
                            (m_questerDatabase[pm.Player].Item as BaseWeapon).Slayer = m_questerDatabase[pm.Player].SlayerName;
                            (m_questerDatabase[pm.Player].Item as BaseWeapon).EquipSound = m_questerDatabase[pm.Player].SoundID;
                            (m_questerDatabase[pm.Player].Item as BaseWeapon).Crafter = this;
                            (m_questerDatabase[pm.Player].Item as BaseWeapon).Quality = WeaponQuality.Regular;
                        }
                        else
                        {
                            (m_questerDatabase[pm.Player].Item as BaseInstrument).Slayer = m_questerDatabase[pm.Player].SlayerName;
                            (m_questerDatabase[pm.Player].Item as BaseInstrument).Crafter = this;
                            (m_questerDatabase[pm.Player].Item as BaseInstrument).Quality = InstrumentQuality.Regular;
                        }
                        m_questerDatabase[pm.Player].Item.Map = pm.Player.Map;
                        m_questerDatabase[pm.Player].Item.Location = pm.Player.Location;
                        m_questerDatabase[pm.Player].Item.IsIntMapStorage = false;
                        string text = string.Format("Your {0} is ready!", m_questerDatabase[pm.Player].Type.ToString().ToLower());
                        this.SayTo(pm.Player, text);
                        this.Logger(pm.Player, text);
                        if (pm.Player.Backpack.TryDropItem(pm.Player, m_questerDatabase[pm.Player].Item, true) == false)
                        {
                            text = string.Format("Your backpack is full, so I left your {0} at your feet.", m_questerDatabase[pm.Player].Type.ToString().ToLower());
                            this.SayTo(pm.Player, text);
                            this.Logger(pm.Player, text);
                        }
                        text = "Come see me again and bring fish steaks, because I'm always hungry.";
                        this.SayTo(pm.Player, text);
                        this.Logger(pm.Player, text);
                        this.Logger(pm.Player, string.Format("{0}:{1}", m_questerDatabase[pm.Player].Type.ToString().ToLower(), m_questerDatabase[pm.Player].Item));
                        m_questerDatabase.Remove(pm.Player);
                        m_allParticipantsQuesterDatabase.Remove(pm.Player);
                        ClearStateData();
                        // ignore this player for 20 seconds so we can find another player
                        m_mobileIgnore.Remember(pm.Player, 20);
                    }
                    else
                    {
                        this.SayTo(pm.Player, string.Format("Your {0} is not yet ready. Come back later.", m_questerDatabase[pm.Player].Type.ToString().ToLower()));
                        ClearStateData();
                        // ignore this player for 20 seconds so we can find another player
                        m_mobileIgnore.Remember(pm.Player, 20);
                    }
                }
            }
            return MachineState.looking;
        }
        MachineState DoLooking()
        {
            m_allParticipantsQuesterDatabase = FindParticipants();
            if (m_allParticipantsQuesterDatabase.Count == 0)
                return MachineState.looking;

            // get the first and cloest player
            KeyValuePair<Mobile, PlayerManager> kvp = m_allParticipantsQuesterDatabase.First();
            TargetMob = kvp.Key;

            Home = TargetMob.Location;         // their location
            RangeHome = 0;                    // we should stay put
            DebugPrint(string.Format("I have a target {0}, I'll go talk to them.", TargetMob.Name));
            return MachineState.approaching;
        }
        MachineState DoApproaching()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                return MachineState.looking;
            }
            if ((int)this.GetDistanceToSqrt(pm.Player.Location) <= 3)
            {   // we are there
                DebugPrint(string.Format("I am inrange of {0}.", pm.Player));
                return MachineState.considering;
            }
            else if ((int)this.GetDistanceToSqrt(pm.Player.Location) < 6)
            {   // we are near
                if (m_questerDatabase.ContainsKey(pm.Player))
                {
                    if (m_questerDatabase[pm.Player].AllItems && m_questerDatabase[pm.Player].QuestWaitingPeriodOver() == true)
                    {
                        string text = string.Format("I've been waiting for you {0}!", pm.Player.Name);
                        pm.Dialog.MeteredDialog(text, 0);
                    }
                    if (m_questerDatabase[pm.Player].AllItems && m_questerDatabase[pm.Player].QuestWaitingPeriodOver() != true)
                    {
                        string text = "I know, I know.";
                        pm.Dialog.MeteredDialog(text, 0);
                        text = "Still working on it.";
                        pm.Dialog.MeteredDialog(text, 0);
                    }
                    else if (!m_questerDatabase[pm.Player].AllItems)
                    {
                        string text = "Oh, it's you.";
                        pm.Dialog.MeteredDialog(text, 0);
                        text = "Did you bring the rest of the items?";
                        pm.Dialog.MeteredDialog(text, 0);
                    }
                }
                return MachineState.considering;
            }
            if (pm.Player.Location != pm.OldLocation)
            {   // they moved, go find them
                DebugPrint("My target moved, I will try to find them.");
                pm.OldLocation = pm.Player.Location;    // their old location
                Home = pm.Player.Location;              // their location
                return MachineState.approaching;
            }
            else
            {   // just keep swimming!
                DebugPrint(string.Format("I'm swimming toward {0}.", pm.Player));
                // don't penalize the player for idleness while we are swimming
                //RefreshIdleTimer();
            }
            return MachineState.approaching;
        }
        MachineState DoConsidering()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                pm.Dialog.SaidDatabase.Clear();
                return MachineState.looking;
            }
            if (pm.Player.Location != pm.OldLocation)
            {   // they moved, go find them
                DebugPrint("My target moved, I will try to find them.");
                pm.OldLocation = pm.Player.Location;    // their old location
                Home = pm.Player.Location;              // their location
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.approaching;
            }
            if ((int)this.GetDistanceToSqrt(pm.Player.Location) <= 3)
            {   // we are there
                DebugPrint(string.Format("I'm ready to chat with {0}.", pm.Player));

                if (DateTime.UtcNow > pm.Dialog.LastSpeech)
                {
                    // for more lifelike speach, we don't use the player's name too often
                    //  note the square brackets .. see MeteredDialog() for an explanation.
                    string text = string.Format("[I can speak with you now]{0}.", pm.Dialog.SaidRecently(pm.Player.Name, TimeSpan.FromSeconds(10)) ? "" : " " + pm.Player.Name);
                    if (pm.Dialog.MeteredDialog(text, 4))
                        return MachineState.considering;

                    text = "I'm hungry.";
                    if (pm.Dialog.MeteredDialog(text, 2))
                        return MachineState.considering;
                }
            }
            return MachineState.considering;
        }
        MachineState DoStingy()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                pm.Dialog.SaidDatabase.Clear();
                return MachineState.looking;
            }
            if (pm.Player.Location != pm.OldLocation)
            {   // they moved, go find them
                DebugPrint("My target moved, I will try to find them.");
                pm.OldLocation = pm.Player.Location;    // their old location
                Home = pm.Player.Location;              // their location
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.approaching;
            }
            if ((int)this.GetDistanceToSqrt(pm.Player) <= 3)
            {   // we are there
                DebugPrint(string.Format("I am chatting with {0}.", pm.Player));
                pm.Dialog.MeteredDialog("*stingy*", 0, true);
            }

            pm.Dialog.SaidDatabase.Clear();         // start over
            return MachineState.considering;
        }
        MachineState DoInteracting()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.looking;
            }
            if (pm.Player.Location != pm.OldLocation)
            {   // they moved, go find them
                DebugPrint("My target moved, I will try to find them.");
                pm.OldLocation = pm.Player.Location;    // their old location
                Home = pm.Player.Location;              // their location
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.approaching;
            }
            if ((int)this.GetDistanceToSqrt(pm.Player) <= 3)
            {   // we are there
                DebugPrint(string.Format("I am chatting with {0}.", pm.Player));
                string text = null;
                if (m_questerDatabase.ContainsKey(pm.Player) && m_questerDatabase[pm.Player].AllItems)
                {
                    text = "I suppose you've come back for your slayer?";
                    pm.Dialog.MeteredDialog(text, 0);
                    return MachineState.wantsSlayerNow;
                }
                else
                {
                    text = "what is it you wish from me?";
                    pm.Dialog.MeteredDialog(text, 0);
                }
            }
            return MachineState.interacting;
        }
        MachineState DoRequestingSlayer()
        {
            PlayerManager pm = FindConversation();
            if (ValidateTarget(pm) == false)
            {
                // they moved away, resume looking
                DebugPrint("My target has moved away or is hidden.");
                ClearStateData();
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.looking;
            }
            if (pm.Player.Location != pm.OldLocation)
            {   // they moved, go find them
                DebugPrint("My target moved, I will try to find them.");
                pm.OldLocation = pm.Player.Location;    // their old location
                Home = pm.Player.Location;              // their location
                pm.Dialog.SaidDatabase.Clear();         // start over
                return MachineState.approaching;
            }
            if ((int)this.GetDistanceToSqrt(pm.Player) <= 3)
            {   // we are there
                DebugPrint(string.Format("I am chatting with {0}.", pm.Player));
                if (DateTime.UtcNow > pm.Dialog.LastSpeech)
                {
                    string text = string.Format("Oh, so it a slayer {0} you wish.", pm.Type.ToString().ToLower());
                    if (pm.Dialog.MeteredDialog(text, 2))
                        return MachineState.requestingSlayer;

                    text = "I can help you with this.";
                    if (pm.Dialog.MeteredDialog(text, 5))
                        return MachineState.requestingSlayer;

                    text = "Bring me 16 vials of blood from the creature you wish to slay.";
                    if (pm.Dialog.MeteredDialog(text, 3))
                        return MachineState.requestingSlayer;

                    text = "You must prove yourself worthy.";
                    if (pm.Dialog.MeteredDialog(text, 7))
                        return MachineState.requestingSlayer;

                    text = "You must also bring me 400 fish steaks, because I'm always hungry!";
                    if (pm.Dialog.MeteredDialog(text, 7))
                        return MachineState.requestingSlayer;

                    text = string.Format("Additionally, you must bring me the {0} you wish enchanted.", pm.Type.ToString().ToLower());
                    if (pm.Dialog.MeteredDialog(text, 4))
                        return MachineState.requestingSlayer;

                    text = "It cannot be magical, and must be crafted by the hand of man.";
                    if (pm.Dialog.MeteredDialog(text, 4))
                        return MachineState.requestingSlayer;

                    text = "Now be off with you. Do not return until you have acquired all the items I have requested.";
                    if (pm.Dialog.MeteredDialog(text, 4))
                    {
                        m_mobileIgnore.Remember(pm.Player, 20);
                        return MachineState.requestingSlayerComplete;
                    }
                }
            }

            return MachineState.requestingSlayer;
        }
        MachineState DoRequestingSlayerComplete()
        {
            DebugPrint(string.Format("The player request for a slayer is complete."));
            // do any cleanup here
            // place this player in the quester database
            PlayerManager pm = FindConversation();
            if (pm.Player != null)
            {
                if (m_questerDatabase.ContainsKey(pm.Player))
                {                                                           // should never happen unless we want to allow quest-stacking
                    DebugPrint(string.Format("Warning: Player {0} is already in the quester database.", pm.Player));
                    m_questerDatabase.Remove(pm.Player);
                }
                m_questerDatabase.Add(pm.Player, new Quest(pm));            // and enter specific dialog with a quester

            }
            ClearStateData();                                               // leave this location, resume swimming
            // okay, lets go back to looking
            return MachineState.looking;
        }
        PlayerManager FindConversation()
        {

            // first find the folks just interested in the quest
            foreach (KeyValuePair<Mobile, PlayerManager> kvp in m_allParticipantsQuesterDatabase)
                if (kvp.Key == TargetMob && IsPlayerAvailable(TargetMob))
                    if (m_questerDatabase.ContainsKey(kvp.Value.Player))
                        return m_questerDatabase[kvp.Value.Player].PlayerManager;
                    else
                        return kvp.Value;

            // empty player manager on failure.
            return new PlayerManager();
        }
        public override void OnDelete()
        {
            if (m_valid)
            {   // we hold the lock, so we need to release it
                m_lock = false;
            }
            base.OnDelete();
        }
        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.Mobile.InRange(this, this.RangePerception))
                e.Handled = true;

            this.Logger(e.Mobile, e.Speech);

            #region administrative commands
            if (e.Mobile.AccessLevel == AccessLevel.Owner)
            {
                if (e.Speech.ToLower().Contains("load database"))
                {
                    if (m_valid == false)
                    {
                        SayTo(e.Mobile, "I am only a copy of Zora and do not have access to the database.");
                    }
                    else if (m_databaseLoaded)
                    {
                        SayTo(e.Mobile, "The database has already been loaded. I will not load it again.");
                    }
                    else
                    {
                        // a new Zora needs to read the quest database
                        ZoraDatabaseDeserialize();
                        SayTo(e.Mobile, "Database loaded.");
                    }
                }
                else if (e.Speech.ToLower().Contains("who are you"))
                {
                    if (m_valid == false)
                    {
                        SayTo(e.Mobile, "I am not the real Zora, I am {0}.", Name);
                    }
                    else
                    {
                        SayTo(e.Mobile, "I am the real Zora, I am {0}.", Name);
                    }
                }
                else if (e.Speech.ToLower().Contains("relinquish control"))
                {
                    if (m_valid == false)
                    {
                        SayTo(e.Mobile, "I am not in control of the lock.");
                    }
                    else
                    {
                        m_valid = false;
                        m_lock = false;
                        Name = "Zora (faux)";
                        NameHue = 0x612;
                        SayTo(e.Mobile, "I have relinquished control of the lock.");
                    }
                }
                else if (e.Speech.ToLower().Contains("assume control"))
                {
                    if (m_valid == true)
                    {
                        SayTo(e.Mobile, "I am already in control of the lock.");
                    }
                    else if (m_lock == true)
                    {
                        SayTo(e.Mobile, "Another Zora already controls the lock.");
                    }
                    else
                    {
                        m_valid = true;
                        m_lock = true;
                        Name = "Zora";
                        NameHue = 0x35;
                        SayTo(e.Mobile, "I have assumed control of the lock.");
                    }
                } //assume control
            }
            #endregion administrative commands

            #region player dialog
            PlayerManager pm = FindConversation();
            if (pm.Player == e.Mobile && ValidateTarget(pm) == true)
            {
                if (m_state == MachineState.interacting)
                {
                    if (pm.Dialog.Match(e.Speech, "weapon|instrument"))
                    {   // ah, that's what we were looking for 
                        if (pm.Dialog.Match(e.Speech, "weapon"))
                            pm.Type = Quest.QuestType.Weapon;
                        else
                            pm.Type = Quest.QuestType.Instrument;
                        m_state = MachineState.requestingSlayer;
                    }
                    else if (pm.Dialog.Match(e.Speech, "slayer"))
                    {   // You need to be more specific
                        m_state = MachineState.whatKindOfSlayer;
                    }
                    else if (pm.Dialog.Match(e.Speech, "zora|help|quest|craft", "slayer"))
                    {   // trying to get them to ask for a "slayer"
                        SayTo(e.Mobile, "What is it you wish from me?");
                    }
                    else
                    {
                        if (Utility.RandomBool())
                            SayTo(e.Mobile, "Hmm?");
                        else
                            SayTo(e.Mobile, "?");
                    }
                }
                else if (m_state != MachineState.interacting)
                {   // we're probably 'approaching' or 'considering', just put them off untill we are 'interacting'
                    if (m_questerDatabase.ContainsKey(pm.Player))
                    {
                        if (pm.Dialog.Match(e.Speech, "zora|help|quest|slayer|craft"))
                        {
                            if (m_questerDatabase[pm.Player].AllItems && m_questerDatabase[pm.Player].QuestWaitingPeriodOver())
                            {   // shortcut to slayer - skips feeding requirement
                                m_state = MachineState.wantsSlayerNow;
                            }
                            else if (m_questerDatabase[pm.Player].AllItems)
                            {
                                SayTo(pm.Player, "You have completed your end of the bargan.");
                                SayTo(pm.Player, string.Format("Give me some time, I have many items to craft."));
                            }
                            else
                            {
                                SayTo(pm.Player, "I am waiting for the rest of the items I requested.");
                                if (!m_questerDatabase[pm.Player].HasBloodVials)
                                    SayTo(pm.Player, "Vials of blood.");
                                if (!m_questerDatabase[pm.Player].HasFishSteaks)
                                    SayTo(pm.Player, "Fish steaks.");
                                if (!m_questerDatabase[pm.Player].HasItem)
                                    SayTo(pm.Player, string.Format("{0}.", m_questerDatabase[pm.Player].Type.ToString().ToLower()));
                            }
                        }
                    }
                    else if (pm.Dialog.Match(e.Speech, "zora|help|quest|craft", "slayer"))
                    {   // trying to get fish steaks to enter MachineState.interacting
                        SayTo(e.Mobile, "Fish steaks always help my hearing.");
                    }
                    else if (pm.Dialog.Match(e.Speech, "slayer"))
                    {
                        SayTo(e.Mobile, "Indeed, this is something I can help you with.");
                        SayTo(e.Mobile, "I'm hungry!");
                    }
                    //else ignore anything else
                }
            }
            else if (pm.Player != e.Mobile)
            {
                if (pm.Dialog.Match(e.Speech, "zora|help|quest|slayer|craft"))
                    SayTo(e.Mobile, "You're going to have to wait your turn.");
                //else ignore anything else
            }
            #endregion player dialog
        }
        private bool BadItemDrop(PlayerManager pm, Item dropped)
        {
            if (pm.Type == Quest.QuestType.Weapon && dropped is BaseInstrument)
                return true;

            if (pm.Type == Quest.QuestType.Instrument && dropped is BaseWeapon)
                return true;

            if (pm.Type == Quest.QuestType.Weapon && dropped is BaseWeapon)
                if (m_questerDatabase[pm.Player].CheckWeapon(dropped as Items.BaseWeapon) == false)
                    return true;

            if (pm.Type == Quest.QuestType.Instrument && dropped is BaseInstrument)
                if (m_questerDatabase[pm.Player].CheckInstrument(dropped as Items.BaseInstrument) == false)
                    return true;

            if (pm.Type == Quest.QuestType.None)
                return true;    // should never happen

            return false;
        }
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            PlayerManager pm = FindConversation();
            if (pm.Player == from && ValidateTarget(pm) == true)
            {
                Logger(pm.Player, string.Format("OnDragDrop: {0}: Amount {1}", dropped, dropped.Amount));

                if (IsFish(dropped) && m_state == MachineState.considering && !m_questerDatabase.ContainsKey(from))
                {
                    // don't refresh the idle time here. we don't want griefiers feeding Zora just to keep her from interacting with 
                    //  other players. We will refresh below where the player is dropping meaningful items on zora
                    if (CheckFishQuantity(dropped) < 20)
                    {
                        PlaySound(GetAngerSound());
                        m_state = MachineState.stingy;
                    }
                    else
                    {
                        PlaySound(GetIdleSound());
                        m_state = MachineState.interacting;
                    }
                }
                else if (IsFish(dropped) && (m_state == MachineState.interacting || m_state == MachineState.requestingSlayer))
                {
                    // bounce the fish steaks back
                    return false;
                }
                else if (IsFish(dropped) && m_questerDatabase.ContainsKey(from))
                {
                    if (m_questerDatabase.ContainsKey(from) && m_questerDatabase[from].AllItems)
                    {
                        if (m_questerDatabase[from].CheckFishQuantity(dropped) < 20)
                        {
                            PlaySound(GetAngerSound());
                            m_state = MachineState.stingy;
                        }
                        else
                        {
                            PlaySound(GetIdleSound());
                            m_state = MachineState.wantsSlayerNow;
                        }
                    }
                    else if (m_questerDatabase[from].HasFishSteaks)
                    {
                        SayTo(from, "Your memory must be slipping. You have already given me the fish steaks.");
                        // bounce the fish steaks back
                        return false;
                    }
                    else
                    {
                        if (m_questerDatabase[from].CheckFish(dropped) == false)
                        {
                            SayTo(from, "I asked for 400 fish steaks!");
                            PlaySound(GetAngerSound());
                            // bounce the fish back
                            return false;
                        }
                        else
                        {
                            m_questerDatabase[from].HasFishSteaks = true;
                            PlaySound(GetIdleSound());
                            SayTo(from, "Thank you, I have taken this as payment.");
                            // if we are receiving meaningful items, refresh the idle timeout.
                            this.RefreshIdleTimer();
                            UpdateState(from);
                        }
                    }
                }
                else if (dropped is Items.SlayerBlood blood && m_questerDatabase.ContainsKey(from))
                {
                    if (m_questerDatabase[from].HasBloodVials)
                    {
                        SayTo(from, "Your memory must be slipping. You have already given me the vials of blood.");
                    }
                    else
                    {
                        if (m_questerDatabase[from].CheckBlood(dropped as Items.SlayerBlood) == false)
                        {
                            SayTo(from, "I asked for 16 vials of blood.");
                        }
                        else
                        {
                            m_questerDatabase[from].HasBloodVials = true;
                            m_questerDatabase[from].SlayerName = blood.SlayerName;
                            m_questerDatabase[from].SoundID = blood.EquipSoundID;
                            dropped.Delete();
                            if (m_questerDatabase[from].HasItem)
                            {
                                SayTo(from, string.Format("Oh, you wish a {1} of {0}. Good choice!", blood.SlayerName.ToString(), m_questerDatabase[from].Item.GetType().Name));
                            }
                            else
                            {
                                SayTo(from, string.Format("Oh, you wish a {0} type {1}. Good choice!", blood.SlayerName.ToString(), m_questerDatabase[pm.Player].Type.ToString().ToLower()));
                            }
                            SayTo(from, string.Format("Thank you, I will use this in the crafting of your {0}.", m_questerDatabase[pm.Player].Type.ToString().ToLower()));
                            // if we are receiving meaningful items, refresh the idle timeout.
                            this.RefreshIdleTimer();
                            UpdateState(from);
                            return true;
                        }
                    }
                }
                else if ((dropped is Items.BaseWeapon || dropped is Items.BaseInstrument) && m_questerDatabase.ContainsKey(from))
                {
                    if (m_questerDatabase[from].HasItem)
                    {
                        SayTo(from, string.Format("Your memory must be slipping. You have already given me the {0}.", pm.Type.ToString().ToLower()));
                    }
                    else
                    {
                        if (BadItemDrop(pm, dropped))
                        {
                            SayTo(from, string.Format("This is not the {0} I asked for.", pm.Type.ToString().ToLower()));
                        }
                        else
                        {
                            m_questerDatabase[from].Item = dropped;
                            dropped.MoveToIntStorage();
                            if (m_questerDatabase[from].HasBloodVials)
                            {
                                SayTo(from, string.Format("Oh, you wish a {1} of {0}. Good choice!", m_questerDatabase[from].SlayerName.ToString(), m_questerDatabase[from].Item.GetType().Name));
                            }
                            else
                            {
                                SayTo(from, string.Format("Oh, you wish a {0} slayer. Good choice!", m_questerDatabase[from].Item.GetType().Name));
                            }

                            SayTo(from, string.Format("Thank you, I will create what you have requested using this {0}.", m_questerDatabase[pm.Player].Type.ToString().ToLower()));
                            // if we are receiving meaningful items, refresh the idle timeout.
                            this.RefreshIdleTimer();
                            UpdateState(from);
                            return true;
                        }
                    }
                }
            }
            else
                SayTo(from, "Just a moment please.");

            return base.OnDragDrop(from, dropped);
        }
        public void UpdateState(Mobile from)
        {
            if (m_questerDatabase.ContainsKey(from))
            {
                if (m_questerDatabase[from].HasItem && m_questerDatabase[from].HasFishSteaks && m_questerDatabase[from].HasBloodVials)
                    m_state = MachineState.requestedItemsComplete;
                // else state remains unchanged
            }
        }
        public override bool CheckFeed(Mobile from, Item dropped)
        {
            if (!IsFish(dropped))
                return base.CheckFeed(from, dropped);

            Animate(17, 5, 1, true, false, 0);
            dropped.Delete();
            return true;
        }
        private void ClearStateData()
        {
            TargetMob = null;
            Home = GetHome();
            RangeHome = GetHomeRange();
        }
        private bool IsPlayerAvailable(Mobile m)
        {
            if (m != null && m.Player && m.NetState != null && m.Alive && !m.Hidden && GetDistanceToSqrt(m) <= RangePerception)
                return true;
            else
                return false;
        }
        private bool ValidateTarget(PlayerManager player)
        {
            if (!IsPlayerAvailable(player.Player) || (player.Player != TargetMob) ||
                m_allParticipantsQuesterDatabase.Count == 0 || !m_allParticipantsQuesterDatabase.ContainsKey(player.Player))
            {   // they moved away, resume looking
                return false;
            }
            return true;
        }
        private Point3D GetHome()
        {
            if (this.Spawner == null)
                return Home;
            else
                return this.Spawner.Location;
        }
        private int GetHomeRange()
        {
            if (this.Spawner == null)
                return RangeHome;
            else
                return this.Spawner.HomeRange;
        }
        private string lastText = null;
        private DateTime nextMessage = DateTime.UtcNow;
        private void DebugPrint(string text)
        {
            if (text == lastText && DateTime.UtcNow >= nextMessage)
            {
                Console.WriteLine(text);
                this.Logger(TargetMob, text);
                nextMessage = DateTime.UtcNow + TimeSpan.FromSeconds(5);
            }
            else if (text != lastText)
                Console.WriteLine(text);

            lastText = text;
        }
        public Dictionary<Mobile, PlayerManager> FindParticipants()
        {
            List<Mobile> participants = new List<Mobile>();
            IPooledEnumerable eable = this.GetMobilesInRange(RangePerception);
            foreach (Mobile m in eable)
            {
                if (IsPlayerAvailable(m))
                {
                    if (m_mobileIgnore.Recall(m))
                        continue;                   // ignore this player for a short while so we can talk to someone else
                    else
                        participants.Add(m);
                }
            }
            eable.Free();

            // sort the list by distance
            participants.Sort((e1, e2) =>
            {
                return e2.GetDistanceToSqrt(this.Location).CompareTo(e1.GetDistanceToSqrt(this.Location));
            });

            // closest at the top of the list
            participants.Reverse();

            // build our sorted database
            Dictionary<Mobile, PlayerManager> database = new Dictionary<Mobile, PlayerManager>();
            foreach (Mobile m in participants)
                if (m_questerDatabase.ContainsKey(m))                               // add questers first.
                    database.Add(m, m_questerDatabase[m].PlayerManager);            // reuse old dialog context    
                else if (m_allParticipantsQuesterDatabase.ContainsKey(m))           // now add Participants
                    database.Add(m, m_allParticipantsQuesterDatabase[m]);           // reuse old dialog context
                else
                    database.Add(m, new PlayerManager(m, this));                    // create new context

            return database;
        }
        public Zora(Serial serial)
            : base(serial)
        {
            if (m_lock == false)
            {
                m_lock = true;
                m_valid = true;
            }
            else
            {   // second functional copies of Zora are not allowed since they would be 
                //  sharing the same quest database
            }
        }
        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    {
                        // no data: Zora stores all her quest data in an external Zora.bin
                        //  you can certainly store mobile type stuff here, just no quest data.
                        //  This is because respawning, whether manual or automatic, must not delete existing quests.
                        writer.Write(m_valid);
                        writer.Write(m_lock);
                        writer.Write(Name);
                        writer.Write(NameHue);
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
            ZoraDatabaseSerialize();
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        // no data: Zora stores all her quest data in an external Zora.bin
                        //  you can certainly store mobile type stuff here, just no quest data.
                        //  This is because respawning, whether manual or automatic, must not delete existing quests.
                        m_valid = reader.ReadBool();
                        m_lock = reader.ReadBool();
                        Name = reader.ReadString();
                        NameHue = reader.ReadInt();
                        if (m_valid)
                            Timer.DelayCall(TimeSpan.FromMinutes(5), new TimerCallback(PlayerNotify));
                        goto case 0;
                    }
                case 0:
                    {
                        break;
                    }
            }
            ZoraDatabaseDeserialize();
        }
        public void ZoraDatabaseDeserialize()
        {
            if (!File.Exists("Saves/Zora.bin"))
                return;

            if (!m_valid)
            {
                Utility.Monitor.WriteLine("This copy of Zora may not access the Zora database ({0}:{1}).", ConsoleColor.Red, this.Map.ToString(), this.Location.ToString());
                return;
            }
            m_databaseLoaded = true;
            Console.WriteLine("Zora Database Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/Zora.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 2:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                Quest quest = null;
                                if (m != null)
                                    quest = new Quest(new PlayerManager(m, this));
                                else
                                    quest = new Quest(new PlayerManager());
                                quest.QuestStart = reader.ReadDateTime();
                                quest.WaitingStart = reader.ReadDateTime();
                                quest.HasBloodVials = reader.ReadBool();
                                quest.HasFishSteaks = reader.ReadBool();
                                quest.Item = reader.ReadItem();
                                quest.SlayerName = (SlayerName)reader.ReadInt();
                                quest.SoundID = reader.ReadInt();
                                quest.Type = (Quest.QuestType)reader.ReadInt();
                                if (quest.QuestExpired() == false)
                                {
                                    if (m != null)
                                    {
                                        quest.Update(); // done once after Deserialize to sync quest data and PlayerManager data
                                        m_questerDatabase.Add(m, quest);
                                    }
                                }
                                else if (quest.HasItem)
                                    quest.Item.Delete();
                            }
                            break;
                        }
                    case 1:
                        {
                            int count = reader.ReadInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Mobile m = reader.ReadMobile();
                                Quest quest = new Quest(new PlayerManager(m, this));
                                quest.QuestStart = reader.ReadDateTime();
                                quest.WaitingStart = reader.ReadDateTime();
                                quest.HasBloodVials = reader.ReadBool();
                                quest.HasFishSteaks = reader.ReadBool();
                                quest.Item = reader.ReadItem();
                                quest.SlayerName = (SlayerName)reader.ReadInt();
                                quest.SoundID = reader.ReadInt();
                                quest.Type = Quest.QuestType.Weapon;
                                if (quest.QuestExpired() == false)
                                {
                                    if (m != null)
                                        m_questerDatabase.Add(m, quest);
                                }
                                else if (quest.HasItem)
                                    quest.Item.Delete();
                            }
                            goto case 0;
                        }
                    case 0:
                        {
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid Zora.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Zora.bin, using default values:", ConsoleColor.Red);
            }
        }
        public void ZoraDatabaseSerialize()
        {
            if (!m_valid)
            {
                Utility.Monitor.WriteLine("This copy of Zora may not access the Zora database.", ConsoleColor.Red);
                return;
            }
            Console.WriteLine("Zora Database Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/Zora.bin", true);
                int version = 2;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 2:
                        {
                            writer.Write(m_questerDatabase.Count());
                            foreach (KeyValuePair<Mobile, Quest> kvp in m_questerDatabase)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value.QuestStart);
                                writer.Write(kvp.Value.WaitingStart);
                                writer.Write(kvp.Value.HasBloodVials);
                                writer.Write(kvp.Value.HasFishSteaks);
                                writer.Write(kvp.Value.Item);
                                writer.Write((int)kvp.Value.SlayerName);
                                writer.Write(kvp.Value.SoundID);
                                writer.Write((int)kvp.Value.Type);
                            }
                            break;
                        }
                    case 1:
                        {
                            writer.Write(m_questerDatabase.Count());
                            foreach (KeyValuePair<Mobile, Quest> kvp in m_questerDatabase)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value.QuestStart);
                                writer.Write(kvp.Value.WaitingStart);
                                writer.Write(kvp.Value.HasBloodVials);
                                writer.Write(kvp.Value.HasFishSteaks);
                                writer.Write(kvp.Value.Item);
                                writer.Write((int)kvp.Value.SlayerName);
                                writer.Write(kvp.Value.SoundID);
                            }
                            goto case 0;
                        }
                    case 0:
                        {
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Zora.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }
}