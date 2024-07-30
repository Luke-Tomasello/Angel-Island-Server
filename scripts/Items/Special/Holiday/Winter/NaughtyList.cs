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

/* Scripts/Items/Special/Holiday/Christmas/NaughtyList.cs
  *	ChangeLog:
  *	12/12/23, Yoar
  *		Initial version.
  */

using Server.Accounting;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class NaughtyList : BaseBook
    {
        public static int NumNames = 8;

        public override double BookWeight { get { return 2.0; } }

        [Constructable]
        public NaughtyList()
            : base(0xFF1, 1, false)
        {
            Title = "Naughty List";
            Author = "Krampus";

            int playerIndex = Utility.Random(NumNames);

            for (int i = 0; i < NumNames; i++)
            {
                string name = null;

                if (i == playerIndex)
                    name = GetRandomPlayerName();

                if (name == null)
                {
                    if (Utility.RandomBool())
                        name = NameList.RandomName("female");
                    else
                        name = NameList.RandomName("male");
                }

                AddLine(name);
            }
        }

        public NaughtyList(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public static TimeSpan ActivityCutOff = TimeSpan.FromDays(14.0);

        private static readonly List<string> m_ActiveUsers = new List<string>();

        public static List<string> ActiveUsers { get { return m_ActiveUsers; } }

        public static new void Initialize()
        {
            ReloadAccountData();
        }

        public static void ReloadAccountData()
        {
            foreach (Account acct in Accounts.Table.Values)
            {
                if (acct.Count > 0 && DateTime.UtcNow < acct.LastLogin + ActivityCutOff)
                    m_ActiveUsers.Add(acct.Username);
            }
        }

        public static string GetRandomPlayerName()
        {
            int count = 0;

            for (int i = 0; i < m_ActiveUsers.Count; i++)
            {
                Account acct = Accounts.Table[m_ActiveUsers[i]] as Account;

                if (acct != null)
                {
                    for (int j = 0; j < acct.Length; j++)
                    {
                        Mobile m = acct[j];

                        if (m != null)
                            count++;
                    }
                }
            }

            if (count <= 0)
                return null;

            int rnd = Utility.Random(count);

            for (int i = 0; i < m_ActiveUsers.Count; i++)
            {
                Account acct = Accounts.Table[m_ActiveUsers[i]] as Account;

                if (acct != null)
                {
                    for (int j = 0; j < acct.Length; j++)
                    {
                        Mobile m = acct[j];

                        if (m != null)
                        {
                            if (rnd == 0)
                                return m.Name;
                            else
                                rnd--;
                        }
                    }
                }
            }

            return null;
        }
    }
}