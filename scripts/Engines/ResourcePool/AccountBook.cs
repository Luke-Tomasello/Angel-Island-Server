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

/* Scripts/Engines/ResourcePool/AccountBook.cs
 * ChangeLog
 * 11/16/21, Yoar
 *      BBS overhaul.
 * 11/07/21, Yoar
 *      Replaced BaseBook.m_Copyable field with a virtual BaseBook.Copyable getter.
 * 6/29/21, Adam
 *	Set Name = "accounting book" to ensure the vendor displays the name correctly in the buy list
 *  04/27/05 TK
 *		Made resources sorted by name so it's easier to find them in book.
 *  02/07/05 TK
 *		Made accountbooks un-copyable.
 *  06/02/05 TK
 *		Removed a few lingering debug WriteLine's
 *	03/02/05 Taran Kain
 *		Created.
 */

using Server.Engines.ResourcePool;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server.Items
{
    public class AccountBook : BaseBook
    {
        public override bool Copyable { get { return false; } }

        [Constructable]
        public AccountBook()
            : base(0xFF1, String.Empty, String.Empty, 0, false)
        {
            Name = "an accounting book";
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, this.Name);
        }

        private static readonly StringBuilder m_Line = new StringBuilder();

        public override void OnDoubleClick(Mobile from)
        {
            Rewrite(from); // TODO: This is quite expensive... Add rewrite delay? Then cache book content per player

            base.OnDoubleClick(from);
        }

        private void Rewrite(Mobile from)
        {
            Title = "Accounts";
            Author = from.Name;

            ClearPages();

            List<ResourceData> rdList = new List<ResourceData>(ResourcePool.Resources.Values);

            rdList.Sort();

            for (int i = 0; i < rdList.Count; i++)
            {
                ResourceData rd = rdList[i];

                AddLine(string.Format("{0}:", rd.Name));
                AddLine(ResourcePool.DescribeInvestment(rd.Type, from));

                if (Pages[Pages.Length - 1].Lines.Length < 8)
                    AddLine("");
            }

            string[] history = ResourceLogger.GetHistory(from).Split(new char[] { '\n' });

            foreach (string transaction in history)
            {
                if (string.IsNullOrEmpty(transaction))
                    continue;

                string[] words = transaction.Split(new char[] { ' ' });

                m_Line.Clear();

                foreach (string word in words)
                {
                    if ((m_Line.Length + word.Length + 1) > 20)
                    {
                        AddLine(m_Line.ToString());

                        m_Line.Clear();
                    }

                    m_Line.Append(word);
                    m_Line.Append(" ");
                }

                AddLine(m_Line.ToString());
                AddLine("");
            }
        }

        public AccountBook(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version == 0 && this.Name == "accounting book")
                this.Name = "an accounting book";
        }
    }
}