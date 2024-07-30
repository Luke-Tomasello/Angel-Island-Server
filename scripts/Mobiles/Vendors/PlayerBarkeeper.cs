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

/* Scripts\Mobiles\Vendors\PlayerBarkeeper.cs
 * CHANGELOG 
 *  9/1/2023, Adam (PlayerBarkeeper 2.0)
 *      Rewrite the 'rumor' system and replace it with keyword-value pairs.
 *      The Barkeeper now supports 64 keyword-value pairs in stead of OSI's 3.
 *      Add the following command lines for keyword-value pair management:
 *          help
 *          list
 *          add <keyword> <rumor>
 *          delete <keyword>
 *          clear
 *      Also, keywords are searched within strings input by player.
 *  8/31/23, Yoar
 *      Increased character limit from 50 to 100
 *  5/25/23, Yoar
 *      Added "manage" speech option because Siege doesn't have context menus
 *	1/19/08, Adam
 *		make IsOwner an override of the new Mobile.cs virtual function
 *	06/28/06, Adam
 *		set IsInvulnerable = true
 *	04/05/06 Taran Kain
 *		Removed title menu, added direct option to sell/not sell food.
 *  6/10/05, Kit
 *		Allowed players to dress/undress barkeeps as per PlayerVendors
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.ContextMenus;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Prompts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
    public class PlayerBarkeeper : BaseVendor
    {
        // 8/31/23, Yoar: Increased character limit from 50 to 100
        public static int CharacterLimit = 100;
        private const int MaxRumors = 64;

        private Mobile m_Owner;
        private string m_TipMessage;

        private List<KeyValuePair<string, string>> m_Keywords = new();
        public List<KeyValuePair<string, string>> Keywords { get { return m_Keywords; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TipMessage
        {
            get { return m_TipMessage; }
            set { m_TipMessage = value; }
        }

        private bool m_IsActiveSeller;

        public override bool IsActiveBuyer { get { return false; } }
        public override bool IsActiveSeller
        {
            get
            {
                return m_IsActiveSeller;
            }
        }

        public void ToggleActiveSeller()
        {
            m_IsActiveSeller = !m_IsActiveSeller;

            LoadSBInfo();
        }

        public override bool DisallowAllMoves { get { return true; } }

        public override VendorShoeType ShoeType { get { return Utility.RandomBool() ? VendorShoeType.ThighBoots : VendorShoeType.Boots; } }

        public override bool GetGender()
        {
            return false; // always starts as male
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new HalfApron(RandomBrightHue()));

            Container pack = this.Backpack;

            if (pack != null)
                pack.Delete();

        }

        public override void InitBody()
        {
            base.InitBody();

            Hue = 0x83F4; // hue is not random

            Container pack = this.Backpack;

            if (pack != null)
                pack.Delete();
        }

        [Constructable]
        public PlayerBarkeeper()
            : this(null)
        {
        }


        public PlayerBarkeeper(Mobile owner)
            : base("the barkeeper")
        {
            m_Owner = owner;
            IsInvulnerable = true;

            LoadSBInfo();
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (InRange(from, 3))
                return true;

            return base.HandlesOnSpeech(from);
        }

        private Timer m_NewsTimer;

        private void ShoutNews_Callback(object state)
        {
            object[] states = (object[])state;
            TownCrierEntry tce = (TownCrierEntry)states[0];
            int index = (int)states[1];

            if (index < 0 || index >= tce.Lines.Length)
            {
                if (m_NewsTimer != null)
                    m_NewsTimer.Stop();

                m_NewsTimer = null;
            }
            else
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, tce.Lines[index]);
                states[1] = index + 1;
            }
        }

        public override bool OnBeforeDeath()
        {
            if (!base.OnBeforeDeath())
                return false;

            Item shoes = this.FindItemOnLayer(Layer.Shoes);

            if (shoes is Sandals)
                shoes.Hue = 0;

            return true;
        }
        List<string> Commands = new List<string>() { "manage", "help", "list", "add", "delete", "clear" };
        private string OwnerCommand(SpeechEventArgs e)
        {
            string[] chunks = e.Speech.Split();
            foreach (string cmd in Commands)
                if (chunks.Contains(cmd, StringComparer.OrdinalIgnoreCase))
                {
                    if (IsOwner(e.Mobile))
                    {
                        e.Handled = true;
                        return cmd;
                    }
                }
            return null;
        }
        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            if (!e.Handled && InRange(e.Mobile, 3))
            {
                if (!Core.RuleSets.ContextMenuRules())
                {
                    switch (OwnerCommand(e))
                    {
                        case null: break;   // not an owner command
                        case "manage":
                            {
                                BeginManagement(e.Mobile);
                                return;
                            }
                        case "help":
                            {
                                Help(e);
                                return;
                            }
                        case "list":
                            {
                                ListRumors(e);
                                return;
                            }
                        case "add":
                            {
                                AddRumor(e);
                                return;
                            }
                        case "delete":
                            {
                                DeleteRumor(e);
                                return;
                            }
                        case "clear":
                            {
                                ClearRumors(e);
                                return;
                            }
                    }
                }

                if (m_NewsTimer == null && e.HasKeyword(0x30)) // *news*
                {
                    TownCrierEntry tce = GlobalTownCrierEntryList.Instance.GetRandomEntry();

                    if (tce == null)
                    {
                        PublicOverheadMessage(MessageType.Regular, 0x3B2, 1005643); // I have no news at this time.
                    }
                    else
                    {
                        m_NewsTimer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(3.0), new TimerStateCallback(ShoutNews_Callback), new object[] { tce, 0 });

                        PublicOverheadMessage(MessageType.Regular, 0x3B2, 502978); // Some of the latest news!
                    }
                }

                string rumor = GetRumor(e.Speech);
                if (rumor != null)
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, false, rumor);
            }
        }
        private int GetActualRumorCount()
        {   // we need to account for the dummy values for the gump hack
            int count = 0;
            foreach (var kvp in m_Keywords)
                if (!string.IsNullOrEmpty(kvp.Key))
                    count++;
            return count;
        }
        private void Help(SpeechEventArgs e)
        {
            e.Mobile.SendMessage(string.Format("help"));
            e.Mobile.SendMessage(string.Format("list"));
            e.Mobile.SendMessage(string.Format("add <keyword> <rumor>"));
            e.Mobile.SendMessage(string.Format("delete <keyword>"));
            e.Mobile.SendMessage(string.Format("clear"));
        }
        private void ListRumors(SpeechEventArgs e)
        {
            if (ArgCount(e) != 1)
            {
                e.Mobile.SendMessage("Usage: list");
                return;
            }

            if (GetActualRumorCount() == 0)
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I have no rumors at this time.");
            foreach (var kvp in m_Keywords)
                if (!string.IsNullOrEmpty(kvp.Key) || !string.IsNullOrEmpty(kvp.Value))
                    e.Mobile.SendMessage(string.Format("{0}, {1}", kvp.Key, kvp.Value));
        }
        private void AddRumor(SpeechEventArgs e)
        {   // format: add <keyword> <rumor>

            string[] chunks = e.Speech.Split();
            string key = null;
            string value = string.Empty;
            if (m_Keywords.Count > MaxRumors)
            {
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Too many rumors!"));
                return;
            }
            else if (ArgCount(e) < 3)
            {
                e.Mobile.SendMessage("Usage: add <keyword> <rumor>");
                return;
            }
            for (int ix = 0; ix < chunks.Length; ix++)
                if (chunks[ix].Equals("add", StringComparison.OrdinalIgnoreCase))
                {
                    if (ix + 2 < chunks.Length)
                    {
                        key = chunks[ix + 1];
                        for (int jx = ix + 2; jx < chunks.Length; jx++)
                            value += chunks[jx] + " ";
                        break;
                    }
                }
            value = value.Trim();
            if (key == null || string.IsNullOrEmpty(value))
                e.Mobile.SendMessage("Usage: add <keyword> <rumor>");
            else
            {
                int index = GetRumorIndex(key);
                if (index >= 0)
                    // replace the existing rumor
                    m_Keywords.RemoveAt(index);

                m_Keywords.Add(new KeyValuePair<string, string>(key, value));
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, string.Format("Okay, {0}.", index == -1 ? "added" : "replaced"));
            }
        }
        private void DeleteRumor(SpeechEventArgs e)
        {   // format: delete <keyword> 
            string[] chunks = e.Speech.Split();
            string key = null;

            if (ArgCount(e) != 2)
            {
                e.Mobile.SendMessage("Usage: delete <keyword> ");
                return;
            }

            for (int ix = 0; ix < chunks.Length; ix++)
                if (chunks[ix].Equals("delete", StringComparison.OrdinalIgnoreCase))
                    key = ix + 1 < chunks.Length ? chunks[ix + 1] : null;

            if (key == null || GetRumor(key) == null)
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I do not have that rumor.");
            else
            {
                int index = GetRumorIndex(key);
                if (index == -1)
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "I do not have that rumor.");
                else
                {
                    m_Keywords.RemoveAt(index);
                    PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Okay, deleted.");
                }
            }
        }
        private int GetRumorIndex(string key)
        {
            int index = -1;
            for (int ix = 0; ix < m_Keywords.Count; ix++)
                if (m_Keywords[ix].Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    index = ix;
                    break;
                }

            return index;
        }
        private void ClearRumors(SpeechEventArgs e)
        {   // format: clear

            if (ArgCount(e) != 1)
            {
                e.Mobile.SendMessage("Usage: clear");
                return;
            }

            int count = GetActualRumorCount();
            if (count == 0)
                e.Mobile.SendMessage(string.Format("No rumors to clear."));
            else
            {
                m_Keywords.Clear();
                e.Mobile.SendMessage(string.Format("Cleared {0} rumors.", count));
            }
        }
        private string GetRumor(string text)
        {
            string[] chunks = text.Split();
            int index = -1;
            foreach (string chunk in chunks)
                if ((index = GetRumorIndex(chunk)) != -1)
                    return m_Keywords[index].Value;

            return null;
        }
        private int ArgCount(SpeechEventArgs e)
        {
            string[] chunks = e.Speech.Split();
            return WasNamed(e.Speech) ? chunks.Length - 1 : chunks.Length;
        }
        public bool WasNamed(string speech)
        {
            string name = this.Name;

            return (name != null && Insensitive.StartsWith(speech, name));
        }
        public override bool CheckGold(Mobile from, Item dropped)
        {
            if (dropped is Gold)
            {
                Gold g = (Gold)dropped;

                if (g.Amount > 50)
                {
                    PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I cannot accept so large a tip!", from.NetState);
                }
                else
                {
                    string tip = m_TipMessage;

                    if (tip == null || (tip = tip.Trim()).Length == 0)
                    {
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "It would not be fair of me to take your money and not offer you information in return.", from.NetState);
                    }
                    else
                    {
                        Direction = GetDirectionTo(from);
                        PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, tip, from.NetState);

                        g.Delete();
                        return true;
                    }
                }
            }

            return false;
        }
        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            if (IsOwner(from))
            {
                //RemoveInfo( item );
                return true;
            }
            else
            {
                SayTo(from, 503223);// If you'd like to purchase an item, just ask.
                return false;
            }
        }
        public override bool AllowEquipFrom(Mobile from)
        {
            if (IsOwner(from) && from.InRange(this, 3) && from.InLOS(this))
                return true;

            return base.AllowEquipFrom(from);
        }
        public override bool IsOwner(Mobile from)
        {
            if (from == null || from.Deleted || this.Deleted)
                return false;

            if (from.AccessLevel > AccessLevel.GameMaster)
                return true;

            return (m_Owner == from);
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (IsOwner(from) && from.InLOS(this))
                list.Add(new ManageBarkeeperEntry(from, this));
        }
        public void BeginManagement(Mobile from)
        {
            if (!IsOwner(from))
                return;

            from.SendGump(new BarkeeperGump(from, this));
        }
        public void Dismiss()
        {
            Delete();
        }
        public void BeginChangeRumor(Mobile from, int index)
        {
            //if (index < 0 || index >= m_Rumors.Length)
            if (index < 0 || index >= m_Keywords.Count)
                return;

            from.Prompt = new ChangeRumorMessagePrompt(this, index);
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "Say what news you would like me to tell our guests.", from.NetState);
        }
        public void EndChangeRumor(Mobile from, int index, string text)
        {
            if (index < 0 || index >= m_Keywords.Count)
                return;

            if (m_Keywords[index].Key == null)
                m_Keywords[index] = new KeyValuePair<string, string>(null, text);
            else
                m_Keywords[index] = new KeyValuePair<string, string>(m_Keywords[index].Key, text);

            from.Prompt = new ChangeRumorKeywordPrompt(this, index);
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "What keyword should a guest say to me to get this news?", from.NetState);
        }
        public void EndChangeKeyword(Mobile from, int index, string text)
        {
            if (index < 0 || index >= m_Keywords.Count)
                return;

            if (m_Keywords[index].Value == null)
                m_Keywords[index] = new KeyValuePair<string, string>(text, null);
            else
                m_Keywords[index] = new KeyValuePair<string, string>(text, m_Keywords[index].Value);

            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I'll pass on the message.", from.NetState);
        }
        public void RemoveRumor(Mobile from, int index)
        {
            if (index < 0 || index >= m_Keywords.Count)
                return;

            m_Keywords.RemoveAt(index);
        }
        public void BeginChangeTip(Mobile from)
        {
            from.Prompt = new ChangeTipMessagePrompt(this);
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "Say what you want me to tell guests when they give me a good tip.", from.NetState);
        }
        public void EndChangeTip(Mobile from, string text)
        {
            m_TipMessage = text;
            PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, "I'll say that to anyone who gives me a good tip.", from.NetState);
        }
        public void RemoveTip(Mobile from)
        {
            m_TipMessage = null;
        }
        public void BeginChangeAppearance(Mobile from)
        {
            from.CloseGump(typeof(PlayerVendorCustomizeGump));
            from.SendGump(new PlayerVendorCustomizeGump(this, from));
        }
        public void ChangeGender(Mobile from)
        {
            Female = !Female;

            if (Female)
            {
                Body = 401;
                Name = NameList.RandomName("female");

                Item beard = FindItemOnLayer(Layer.FacialHair);

                if (beard != null)
                    beard.Delete();
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }
        }
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }
        public override void InitSBInfo()
        {
            if (IsActiveSeller)
            {
                if (m_SBInfos.Count == 0)
                    m_SBInfos.Add(new SBPlayerBarkeeper());
            }
            else
            {
                m_SBInfos.Clear();
            }
        }
        public PlayerBarkeeper(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version;

            // version 2
            writer.WriteEncodedInt((int)m_Keywords.Count);
            foreach (var kvp in m_Keywords)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }

            // version 1
            writer.Write(m_IsActiveSeller);

            // version 0
            writer.Write((Mobile)m_Owner);

            // removed in version 2
            /*writer.WriteEncodedInt((int)m_Rumors.Length);

            for (int i = 0; i < m_Rumors.Length; ++i)
                BarkeeperRumor.Serialize(writer, m_Rumors[i]);*/

            writer.Write((string)m_TipMessage);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        int count = reader.ReadEncodedInt();
                        for (int ix = 0; ix < count; ix++)
                            m_Keywords.Add(new KeyValuePair<string, string>(reader.ReadString(), reader.ReadString()));

                        m_IsActiveSeller = reader.ReadBool();
                        m_Owner = reader.ReadMobile();
                        m_TipMessage = reader.ReadString();
                        break;
                    }
                case 1:
                    {
                        m_IsActiveSeller = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Owner = reader.ReadMobile();
                        /*
                        m_Rumors = new BarkeeperRumor[reader.ReadEncodedInt()];

                        for (int i = 0; i < m_Rumors.Length; ++i)
                            m_Rumors[i] = BarkeeperRumor.Deserialize(reader);*/

                        int count = reader.ReadEncodedInt();
                        for (int ix = 0; ix < count; ix++)
                        {
                            if (reader.ReadBool())
                            {
                                string message = reader.ReadString();
                                string key = reader.ReadString();
                                m_Keywords.Add(new KeyValuePair<string, string>(key, message));
                            }
                        }

                        m_TipMessage = reader.ReadString();

                        break;
                    }
            }
        }
    }
    public class ChangeRumorMessagePrompt : Prompt
    {
        private PlayerBarkeeper m_Barkeeper;
        private int m_RumorIndex;

        public ChangeRumorMessagePrompt(PlayerBarkeeper barkeeper, int rumorIndex)
        {
            m_Barkeeper = barkeeper;
            m_RumorIndex = rumorIndex;
        }

        public override void OnCancel(Mobile from)
        {
            OnResponse(from, "");
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (text.Length > PlayerBarkeeper.CharacterLimit)
                text = text.Substring(0, PlayerBarkeeper.CharacterLimit);

            m_Barkeeper.EndChangeRumor(from, m_RumorIndex, text);
        }
    }
    public class ChangeRumorKeywordPrompt : Prompt
    {
        private PlayerBarkeeper m_Barkeeper;
        private int m_RumorIndex;

        public ChangeRumorKeywordPrompt(PlayerBarkeeper barkeeper, int rumorIndex)
        {
            m_Barkeeper = barkeeper;
            m_RumorIndex = rumorIndex;
        }

        public override void OnCancel(Mobile from)
        {
            OnResponse(from, "");
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (text.Length > PlayerBarkeeper.CharacterLimit)
                text = text.Substring(0, PlayerBarkeeper.CharacterLimit);

            m_Barkeeper.EndChangeKeyword(from, m_RumorIndex, text);
        }
    }
    public class ChangeTipMessagePrompt : Prompt
    {
        private PlayerBarkeeper m_Barkeeper;

        public ChangeTipMessagePrompt(PlayerBarkeeper barkeeper)
        {
            m_Barkeeper = barkeeper;
        }

        public override void OnCancel(Mobile from)
        {
            OnResponse(from, "");
        }

        public override void OnResponse(Mobile from, string text)
        {
            if (text.Length > PlayerBarkeeper.CharacterLimit)
                text = text.Substring(0, PlayerBarkeeper.CharacterLimit);

            m_Barkeeper.EndChangeTip(from, text);
        }
    }
    public class ManageBarkeeperEntry : ContextMenuEntry
    {
        private Mobile m_From;
        private PlayerBarkeeper m_Barkeeper;

        public ManageBarkeeperEntry(Mobile from, PlayerBarkeeper barkeeper)
            : base(6151, 12)
        {
            m_From = from;
            m_Barkeeper = barkeeper;
        }

        public override void OnClick()
        {
            m_Barkeeper.BeginManagement(m_From);
        }
    }
    public class BarkeeperGump : Gump
    {
        private Mobile m_From;
        private PlayerBarkeeper m_Barkeeper;

        public void RenderBackground()
        {
            AddPage(0);

            AddBackground(30, 40, 585, 410, 5054);

            AddImage(30, 40, 9251);
            AddImage(180, 40, 9251);
            AddImage(30, 40, 9253);
            AddImage(30, 130, 9253);
            AddImage(598, 40, 9255);
            AddImage(598, 130, 9255);
            AddImage(30, 433, 9257);
            AddImage(180, 433, 9257);
            AddImage(30, 40, 9250);
            AddImage(598, 40, 9252);
            AddImage(598, 433, 9258);
            AddImage(30, 433, 9256);

            AddItem(30, 40, 6816);
            AddItem(30, 125, 6817);
            AddItem(30, 233, 6817);
            AddItem(30, 341, 6817);
            AddItem(580, 40, 6814);
            AddItem(588, 125, 6815);
            AddItem(588, 233, 6815);
            AddItem(588, 341, 6815);

            AddBackground(183, 25, 280, 30, 5054);

            AddImage(180, 25, 10460);
            AddImage(434, 25, 10460);
            AddImage(560, 20, 1417);

            AddHtml(223, 32, 200, 40, "BARKEEP CUSTOMIZATION MENU", false, false);
            AddBackground(243, 433, 150, 30, 5054);

            AddImage(240, 433, 10460);
            AddImage(375, 433, 10460);
        }

        public void RenderCategories()
        {
            AddPage(1);

            AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 2);
            AddHtml(170, 120, 200, 40, "Message Control", false, false);

            AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 8);
            AddHtml(170, 200, 200, 40, "Customize your barkeep", false, false);

            AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 3);
            AddHtml(170, 280, 200, 40, "Dismiss your barkeep", false, false);

            AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Reply, 0);
            AddHtml(290, 440, 35, 40, "Back", false, false);

            AddItem(574, 43, 5360);
        }

        public void RenderMessageManagement()
        {
            AddPage(2);

            AddButton(130, 120, 4005, 4007, 0, GumpButtonType.Page, 4);
            AddHtml(170, 120, 380, 20, "Add or change a message and keyword", false, false);

            AddButton(130, 200, 4005, 4007, 0, GumpButtonType.Page, 5);
            AddHtml(170, 200, 380, 20, "Remove a message and keyword from your barkeep", false, false);

            AddButton(130, 280, 4005, 4007, 0, GumpButtonType.Page, 6);
            AddHtml(170, 280, 380, 20, "Add or change your barkeeper's tip message", false, false);

            AddButton(130, 360, 4005, 4007, 0, GumpButtonType.Page, 7);
            AddHtml(170, 360, 380, 20, "Delete your barkeepers tip message", false, false);

            AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
            AddHtml(290, 440, 35, 40, "Back", false, false);

            AddItem(580, 46, 4030);
        }

        public void RenderDismissConfirmation()
        {
            AddPage(3);

            AddHtml(170, 160, 380, 20, "Are you sure you want to dismiss your barkeeper?", false, false);

            AddButton(205, 280, 4005, 4007, GetButtonID(0, 0), GumpButtonType.Reply, 0);
            AddHtml(240, 280, 100, 20, @"Yes", false, false);

            AddButton(395, 280, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtml(430, 280, 100, 20, "No", false, false);

            AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
            AddHtml(290, 440, 35, 40, "Back", false, false);

            AddItem(574, 43, 5360);
            AddItem(584, 34, 6579);
        }
        bool EmptyRumor(KeyValuePair<string, string> kvp)
        {
            if (string.IsNullOrEmpty(kvp.Value))
                return true;
            return false;
        }
        bool EmptyKeyword(KeyValuePair<string, string> kvp)
        {
            if (string.IsNullOrEmpty(kvp.Key))
                return true;
            return false;
        }
        public void RenderMessageManagement_Message_AddOrChange()
        {
            AddPage(4);

            AddHtml(250, 60, 500, 25, "Add or change a message", false, false);

            for (int i = 0; i < m_Barkeeper.Keywords.Count; ++i)
            {
                KeyValuePair<string, string> kvp = m_Barkeeper.Keywords[i];

                AddHtml(100, 70 + (i * 120), 50, 20, "Message", false, false);
                AddHtml(100, 90 + (i * 120), 450, 40, EmptyRumor(kvp) ? "No current message" : kvp.Value, true, false);
                AddHtml(100, 130 + (i * 120), 50, 20, "Keyword", false, false);
                AddHtml(100, 150 + (i * 120), 450, 40, EmptyKeyword(kvp) ? "None" : kvp.Key, true, false);

                AddButton(60, 90 + (i * 120), 4005, 4007, GetButtonID(1, i), GumpButtonType.Reply, 0);
            }

            AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
            AddHtml(290, 440, 35, 40, "Back", false, false);

            AddItem(580, 46, 4030);
        }

        public void RenderMessageManagement_Message_Remove()
        {
            AddPage(5);

            AddHtml(190, 60, 500, 25, "Choose the message you would like to remove", false, false);

            for (int i = 0; i < m_Barkeeper.Keywords.Count; ++i)
            {
                KeyValuePair<string, string> kvp = m_Barkeeper.Keywords[i];

                AddHtml(100, 70 + (i * 120), 50, 20, "Message", false, false);
                AddHtml(100, 90 + (i * 120), 450, 40, EmptyRumor(kvp) ? "No current message" : kvp.Value, true, false);
                AddHtml(100, 130 + (i * 120), 50, 20, "Keyword", false, false);
                AddHtml(100, 150 + (i * 120), 450, 40, EmptyKeyword(kvp) ? "None" : kvp.Key, true, false);

                AddButton(60, 90 + (i * 120), 4005, 4007, GetButtonID(2, i), GumpButtonType.Reply, 0);
            }

            AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
            AddHtml(290, 440, 35, 40, "Back", false, false);

            AddItem(580, 46, 4030);
        }

        private int GetButtonID(int type, int index)
        {
            return 1 + (index * 6) + type;
        }

        private void RenderMessageManagement_Tip_AddOrChange()
        {
            AddPage(6);

            AddHtml(250, 95, 500, 20, "Change this tip message", false, false);
            AddHtml(100, 190, 50, 20, "Message", false, false);
            AddHtml(100, 210, 450, 40, m_Barkeeper.TipMessage == null ? "No current message" : m_Barkeeper.TipMessage, true, false);

            AddButton(60, 210, 4005, 4007, GetButtonID(3, 0), GumpButtonType.Reply, 0);

            AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
            AddHtml(290, 440, 35, 40, "Back", false, false);

            AddItem(580, 46, 4030);
        }

        private void RenderMessageManagement_Tip_Remove()
        {
            AddPage(7);

            AddHtml(250, 95, 500, 20, "Remove this tip message", false, false);
            AddHtml(100, 190, 50, 20, "Message", false, false);
            AddHtml(100, 210, 450, 40, m_Barkeeper.TipMessage == null ? "No current message" : m_Barkeeper.TipMessage, true, false);

            AddButton(60, 210, 4005, 4007, GetButtonID(4, 0), GumpButtonType.Reply, 0);

            AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 2);
            AddHtml(290, 440, 35, 40, "Back", false, false);

            AddItem(580, 46, 4030);
        }

        private void RenderAppearanceCategories()
        {
            AddPage(8);

            AddButton(130, 120, 4005, 4007, GetButtonID(5, 0), GumpButtonType.Reply, 0);
            AddHtml(170, 120, 120, 20, m_Barkeeper.IsActiveSeller ? "Don't Sell Food" : "Sell Food", false, false);
            //			AddButton( 130, 120, 4005, 4007, GetButtonID( 5, 0 ), GumpButtonType.Reply, 0 );
            //			AddHtml( 170, 120, 120, 20, "Title", false, false );

            AddButton(130, 200, 4005, 4007, GetButtonID(5, 1), GumpButtonType.Reply, 0);
            AddHtml(170, 200, 120, 20, "Appearance", false, false);

            AddButton(130, 280, 4005, 4007, GetButtonID(5, 2), GumpButtonType.Reply, 0);
            AddHtml(170, 280, 120, 20, "Male / Female", false, false);

            AddButton(338, 437, 4014, 4016, 0, GumpButtonType.Page, 1);
            AddHtml(290, 440, 35, 40, "Back", false, false);

            AddItem(580, 44, 4033);
        }

        public BarkeeperGump(Mobile from, PlayerBarkeeper barkeeper)
            : base(0, 0)
        {
            m_From = from;
            m_Barkeeper = barkeeper;

            from.CloseGump(typeof(BarkeeperGump));

            /* Yoar:
             * Because the gumps were designed to always have 3 tip messages, we need to ensure our list has at least 3,
             * possibly empty entries. Once the gumps are redesigned, we can eliminate this 'fluffing' of the list
             */
            while (m_Barkeeper.Keywords.Count < 3)
                m_Barkeeper.Keywords.Add(new KeyValuePair<string, string>("", ""));

            RenderBackground();
            RenderCategories();
            RenderMessageManagement();
            RenderDismissConfirmation();
            RenderMessageManagement_Message_AddOrChange();
            RenderMessageManagement_Message_Remove();
            RenderMessageManagement_Tip_AddOrChange();
            RenderMessageManagement_Tip_Remove();
            RenderAppearanceCategories();
        }

        public override void OnResponse(NetState state, RelayInfo info)
        {
            if (!m_Barkeeper.IsOwner(m_From))
                return;

            int index = info.ButtonID - 1;

            if (index < 0)
                return;

            int type = index % 6;
            index /= 6;

            switch (type)
            {
                case 0: // Controls
                    {
                        switch (index)
                        {
                            case 0: // Dismiss
                                {
                                    m_Barkeeper.Dismiss();
                                    break;
                                }
                        }

                        break;
                    }
                case 1: // Change message
                    {
                        m_Barkeeper.BeginChangeRumor(m_From, index);
                        break;
                    }
                case 2: // Remove message
                    {
                        m_Barkeeper.RemoveRumor(m_From, index);
                        break;
                    }
                case 3: // Change tip
                    {
                        m_Barkeeper.BeginChangeTip(m_From);
                        break;
                    }
                case 4: // Remove tip
                    {
                        m_Barkeeper.RemoveTip(m_From);
                        break;
                    }
                case 5: // Appearance category selection
                    {
                        switch (index)
                        {
                            case 0:
                                {
                                    m_Barkeeper.ToggleActiveSeller();
                                    m_From.SendGump(new BarkeeperGump(m_From, m_Barkeeper));
                                    break;
                                }
                            case 1: m_Barkeeper.BeginChangeAppearance(m_From); break;
                            case 2: m_Barkeeper.ChangeGender(m_From); break;
                        }

                        break;
                    }
            }
        }
    }
}