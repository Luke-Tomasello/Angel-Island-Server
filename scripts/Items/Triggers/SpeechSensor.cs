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

/* Items/Triggers/SpeechSensor.cs
 * CHANGELOG:
 *  4/24/2024, Adam (HasKeyword)
 *      Add KeyWordMatchMode to HasKeyword to allow for AnyOf and AllOf processing
 *      Add a new Property MatchMode to utilize this new matching feature.
 * 	3/7/23, Yoar
 * 		Initial version.
 */

using Server.Network;
using System.Collections;

namespace Server.Items.Triggers
{
    public class SpeechSensor : Item, ITrigger
    {
        public override string DefaultName { get { return "Speech Sensor"; } }

        private string m_Event;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Event
        {
            get { return m_Event; }
            set { m_Event = value; }
        }
        private Item m_Link;

        private TextEntry m_Keyword;
        private int m_Range;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item Link
        {
            get { return m_Link; }
            set { m_Link = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TextEntry Keyword
        {
            get { return m_Keyword; }
            set { m_Keyword = value; }
        }
        KeywordController.KeyWordMatchMode m_MatchMode = KeywordController.KeyWordMatchMode.Standard;
        [CommandProperty(AccessLevel.GameMaster)]
        public KeywordController.KeyWordMatchMode MatchMode
        {
            get { return m_MatchMode; }
            set
            {
                m_MatchMode = value;
                switch (value)
                {
                    case KeywordController.KeyWordMatchMode.Standard:
                        {
                            SendSystemMessage("Using wild cards:");
                            SendSystemMessage(string.Format("To ignore leading text, use: *{0}", m_Keyword));
                            SendSystemMessage(string.Format("To also ignore trailing text, use: *{0}*", m_Keyword));
                            break;
                        }
                    case KeywordController.KeyWordMatchMode.AnyOf:
                        {
                            SendSystemMessage("AnyOf: Use a ';' to separate words or phrases.");
                            SendSystemMessage(string.Format("Any of the words '{0}' are required, any order, other words are ignored", m_Keyword));
                            break;
                        }
                    case KeywordController.KeyWordMatchMode.AllOf:
                        {
                            SendSystemMessage("AllOf: Use a ';' to separate words or phrases.");
                            SendSystemMessage(string.Format("All of the words '{0}' are required, any order, other words are ignored", m_Keyword));
                            break;
                        }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Range
        {
            get { return m_Range; }
            set { m_Range = value; }
        }

        [Constructable]
        public SpeechSensor()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public override bool HandlesOnSpeech { get { return true; } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e.Type != MessageType.Emote && !e.Mobile.Hidden &&
                Utility.InRange(e.Mobile.Location, this.GetWorldLocation(), m_Range) &&
                KeywordController.HasKeyword(e, m_Keyword, m_MatchMode))
                TriggerSystem.CheckTrigger(e.Mobile, m_Link);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                list.Add(new TriggerSystem.LinkCME());
        }

        public SpeechSensor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2
            writer.Write((int)m_MatchMode);

            // version 1
            writer.Write(m_Event);

            // version 0
            writer.Write((Item)m_Link);

            m_Keyword.Serialize(writer);
            writer.Write((int)m_Range);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_MatchMode = (KeywordController.KeyWordMatchMode)reader.ReadInt();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Event = reader.ReadString();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Link = reader.ReadItem();

                        m_Keyword = new TextEntry(reader);
                        m_Range = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}