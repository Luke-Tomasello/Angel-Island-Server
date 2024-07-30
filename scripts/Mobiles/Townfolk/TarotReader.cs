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

/* Scripts/Mobiles/Townfolk/TarotReader.cs
 * ChangeLog
 *	10/17/23, Yoar
 *		Initial version.
 */

using Server.Gumps;

namespace Server.Mobiles
{
    public class TarotReader : Gypsy
    {
        public static int Range = 4;

        [Constructable]
        public TarotReader()
            : base()
        {
            Title = "the tarot reader";

            SetSkill(SkillName.SpiritSpeak, 65, 88);
        }

        private readonly Memory m_ShoutMemory = new Memory();
        private readonly Memory m_TalkMemory = new Memory();
        private readonly Memory m_ReadMemory = new Memory();

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (!m_ShoutMemory.Recall(m) && !m_TalkMemory.Recall(m) && !m_ReadMemory.Recall(m) && InRange(m, Range) && InLOS(m) && (!InRange(oldLocation, Range) || !InLOS(oldLocation)) && CanSee(m))
            {
                m_ShoutMemory.Remember(m, 120.0);

                string greeting = m_Greetings[Utility.Random(m_Greetings.Length)];

                SayTo(m, "{0} {1}, would you like me to read your cards?", greeting, m.Name);
            }

            base.OnMovement(m, oldLocation);
        }

        private static readonly string[] m_Greetings = new string[]
            {
                "Hi",
                "Hello",
                "Hail",
                "Greetings",
            };

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (InRange(e.Mobile, Range) && InLOS(e.Mobile))
            {
                if (m_TalkMemory.Recall(e.Mobile))
                {
                    e.Handled = true;
                    m_TalkMemory.Forget(e.Mobile);

                    if (!e.Speech.EndsWith("?"))
                    {
                        SayTo(e.Mobile, "That is not a question.");
                    }
                    else
                    {
                        m_ReadMemory.Remember(e.Mobile, 7200.0);
                        e.Mobile.CloseGump(typeof(CardGump));
                        e.Mobile.SendGump(new CardGump(e.Speech));
                    }
                }
                else if (Insensitive.Equals(e.Speech, "read cards") || Insensitive.Equals(e.Speech, "read my cards"))
                {
                    if (m_ReadMemory.Recall(e.Mobile) && e.Mobile.AccessLevel < AccessLevel.GameMaster)
                    {
                        SayTo(e.Mobile, "You must wait approximately one day before having another reading.");
                    }
                    else
                    {
                        e.Handled = true;
                        m_TalkMemory.Remember(e.Mobile, 60.0);
                        SayTo(e.Mobile, "Ask your question.");
                    }
                }
                else
                {
                    CardGump g = e.Mobile.FindGump(typeof(CardGump)) as CardGump;

                    if (g != null)
                    {
                        for (int i = 0; i < g.Cards.Length; i++)
                        {
                            Card c = g.Cards[i];

                            string search = Utility.SplitCamelCase(c.Type.ToString());

                            if (Insensitive.Contains(e.Speech, search))
                            {
                                e.Handled = true;

                                CardInfo info = GetInfo(c.Type);

                                if (info != null)
                                    SayTo(e.Mobile, c.Flipped ? info.FlippedCliloc : info.Cliloc);
                            }
                        }
                    }
                }
            }

            base.OnSpeech(e);
        }

        private class CardGump : Gump
        {
            private Card[] m_Cards;

            public Card[] Cards { get { return m_Cards; } }

            public CardGump(string question)
                : base(200, 200)
            {
                m_Cards = Shuffle(3);

                AddImage(0, 0, 0x7724);

                AddCard(28, 140, m_Cards[0]);
                AddHtmlLocalized(28, 115, 125, 20, 1076079, 0x7FFF, false, false); // The Past

                AddCard(171, 140, m_Cards[1]);
                AddHtmlLocalized(171, 115, 125, 20, 1076081, 0x7FFF, false, false); // The Question

                AddCard(314, 140, m_Cards[2]);
                AddHtmlLocalized(314, 115, 125, 20, 1076080, 0x7FFF, false, false); // The Future

                AddHtml(30, 32, 400, 25, Utility.FixHtml(question), true, false);
            }

            private void AddCard(int x, int y, Card c)
            {
                CardInfo info = GetInfo(c.Type);

                if (info != null)
                    AddImageTiled(x, y, 115, 180, c.Flipped ? info.FlippedGumpID : info.GumpID);
            }
        }

        private static Card[] Shuffle(int count)
        {
            for (int i = 0; i < count && i < m_Deck.Length; i++)
            {
                int j = Utility.Random(i, m_Deck.Length - i);

                Card temp = m_Deck[i];
                m_Deck[i] = m_Deck[j];
                m_Deck[j] = temp;

                m_Deck[i].Flipped = Utility.RandomBool();
            }

            Card[] result = new Card[count];

            for (int i = 0; i < count && i < m_Deck.Length; i++)
                result[i] = m_Deck[i];

            return result;
        }

        private static readonly Card[] m_Deck;

        static TarotReader()
        {
            m_Deck = new Card[10];

            for (int i = 0; i < m_Deck.Length; i++)
                m_Deck[i].Type = (CardType)i;
        }

        private static readonly CardInfo[] m_Table = new CardInfo[]
            {
                new CardInfo(0x7725, 1076063, 0x772F, 1076015),
                new CardInfo(0x7726, 1076060, 0x7730, 1076016),
                new CardInfo(0x7727, 1076061, 0x7731, 1076017),
                new CardInfo(0x7728, 1076057, 0x7732, 1076018),
                new CardInfo(0x7729, 1076062, 0x7733, 1076019),
                new CardInfo(0x772A, 1076059, 0x7734, 1076020),
                new CardInfo(0x772B, 1076058, 0x7735, 1076021),
                new CardInfo(0x772C, 1076065, 0x7736, 1076022),
                new CardInfo(0x772D, 1076064, 0x7737, 1076023),
                new CardInfo(0x772E, 1076066, 0x7738, 1076024),
            };

        private static CardInfo GetInfo(CardType type)
        {
            int index = (int)type;

            if (index >= 0 && index < m_Table.Length)
                return m_Table[index];

            return null;
        }

        private enum CardType
        {
            Death,
            WheelOfFortune,
            Justice,
            Fool,
            HangedMan,
            HighPriestess,
            Magus,
            Star,
            Tower,
            World,
        }

        private struct Card
        {
            public CardType Type;
            public bool Flipped;
        }

        private class CardInfo
        {
            public readonly int GumpID;
            public readonly int Cliloc;
            public readonly int FlippedGumpID;
            public readonly int FlippedCliloc;

            public CardInfo(int gumpID, int cliloc, int flippedGumpID, int flippedCliloc)
            {
                GumpID = gumpID;
                Cliloc = cliloc;
                FlippedGumpID = flippedGumpID;
                FlippedCliloc = flippedCliloc;
            }
        }

        public TarotReader(Serial serial)
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