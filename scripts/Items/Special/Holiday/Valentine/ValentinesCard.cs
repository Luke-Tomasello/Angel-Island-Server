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

/* Scripts\Items\Special\Holiday\Valentine\ValentinesCard.cs
 * ChangeLog:
 * 2/13/24, Yoar
 *      Added custom quotes
 * 2/9/24 Yoar
 *		Initial Creation. Based on OSI Valentines cards.
 *		Target another player to sign the card with your name and the target's name.
 *		You may then trade the card to the targeted player.
 */

using Server.Targeting;

namespace Server.Items
{
    [Flipable(0x0EBE, 0x0EBE)]
    public class ValentinesCard : Item
    {
        private static readonly string m_Unsigned = "___";

        private static readonly string[] m_Table = new string[]
            {
#if OSI
                "To my one true love, {0}. Signed: {1}",
                "You’ve pwnd my heart, {0}. Signed: {1}",
                "Happy Valentine’s Day, {0}. Signed: {1}",
                "Blackrock has driven me crazy... for {0}! Signed: {1}",
                "You light my Candle of Love, {0}! Signed: {1}",
#endif
                "You must have mastered begging, {0} because you've got me on my knees. Signed: {1}",
                "You must be a master fisherman, because you've reeled me in, {0}. Signed: {1}",
                "You must be a grandmaster thief, {0}, because you've stolen my heart. Signed: {1}",
                "You lockpicked my heart, {0}. Signed: {1}",
                "I choo-choo-choose you, {0}. Signed: {1}",
                "I've decoded so many maps, but I never found treasure until i found you, {0}. Signed: {1}",
                "I would solo the harrower just to be with you, {0}. Signed: {1}",
                "Be my valentine, {0}. Signed: {1}",
                "\"knock knock.\" \"who's there?\" \"olive.\" \"olive who?\" \"olive you, {0}!\" Signed: {1}",
                "You must be a scribe, {0}. 'Cause you've written your name all over my heart. Signed: {1}",
                "You shine brighter than the light spell, {0}. Signed: {1}",
                "My love for you is stronger than valorite, {0}. Signed: {1}",
                "You're the rare in my dungeon chest, {0}. Signed: {1}",
            };

        private static string RandomFormat()
        {
            if (m_Table.Length == 0)
                return null;

            return m_Table[Utility.Random(m_Table.Length)];
        }

        public override string DefaultName { get { return "a Valentine's card"; } }

        private string m_Format;
        private string m_Target;
        private string m_Signed;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Format
        {
            get { return m_Format; }
            set { m_Format = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Target
        {
            get { return m_Target; }
            set { m_Target = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Signed
        {
            get { return m_Signed; }
            set { m_Signed = value; }
        }

        [Constructable]
        public ValentinesCard()
            : base(0x0EBE)
        {
            Hue = 0xE8;
            LootType = LootType.Blessed;

            m_Format = RandomFormat();
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, m_Format, m_Target == null ? m_Unsigned : m_Target, m_Signed == null ? m_Unsigned : m_Signed);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Target != null)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("To whom do you wish to give this card?");
                from.BeginTarget(10, false, TargetFlags.None, new TargetCallback(OnTarget));
            }
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (Deleted || !IsChildOf(from.Backpack))
                return;

            Mobile target = targeted as Mobile;

            if (target == null)
            {
                from.SendMessage("You can't give a card to that!");
            }
            else if (!target.Player)
            {
                from.SendMessage("You can't possibly be THAT lonely!");
            }
            else if (target == from)
            {
                from.SendMessage("You can't give yourself a card, silly!");
            }
            else
            {
                from.SendMessage("You fill out the card. Hopefully the other person actually likes you...");

                m_Target = Misc.Titles.FormatShort(target);
                m_Signed = Misc.Titles.FormatShort(from);
            }
        }

        public ValentinesCard(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((string)m_Format);
            writer.Write((string)m_Target);
            writer.Write((string)m_Signed);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Format = Utility.Intern(reader.ReadString());
                        m_Target = Utility.Intern(reader.ReadString());
                        m_Signed = Utility.Intern(reader.ReadString());

                        break;
                    }
            }
        }
    }
}