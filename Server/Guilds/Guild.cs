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

/* Server/Guilds/Guild.cs
 * CHANGELOG:
 *  4/30/23, Yoar
 *      Rewrote Suffix getter into GetSuffix(Mobile mob) method
 *  4/14/23, Yoar
 *      Moved GuildType to Scripts/Engines/Guilds/Guild.cs
 *      Added Suffix and ForceGuildTitle getters for the purpose of building Mobile guild titles
 * 12/14/05 Kit,
 *		Added Case search for names/abbreviatons to Prevent Orc oRc from working.
 */

using System.Collections;

namespace Server.Guilds
{
    [PropertyObject]
    public abstract class BaseGuild
    {
        private int m_Id;

        public BaseGuild(int Id)//serialization ctor
        {
            m_Id = Id;
            m_GuildList.Add(m_Id, this);
            if (m_Id + 1 > m_NextID)
                m_NextID = m_Id + 1;
        }

        public BaseGuild()
        {
            m_Id = m_NextID++;
            m_GuildList.Add(m_Id, this);
        }

        public int Id { get { return m_Id; } }

        public abstract void Deserialize(GenericReader reader);
        public abstract void Serialize(GenericWriter writer);

        public abstract string Abbreviation { get; set; }
        public abstract string Name { get; set; }
        public abstract bool ForceGuildTitle { get; }
        public abstract bool Disbanded { get; }
        public abstract void OnDelete(Mobile mob);
        public abstract string GetSuffix(Mobile mob);

        private static Hashtable m_GuildList = new Hashtable();
        private static int m_NextID = 1;

        public static Hashtable List
        {
            get
            {
                return m_GuildList;
            }
        }

        public static BaseGuild Find(int id)
        {
            return (BaseGuild)m_GuildList[id];
        }

        public static BaseGuild FindByName(string name)
        {
            foreach (BaseGuild g in m_GuildList.Values)
            {
                if (g.Name.ToLower() == name.ToLower())
                    return g;
            }

            return null;
        }

        public static BaseGuild FindByAbbrev(string abbr)
        {
            foreach (BaseGuild g in m_GuildList.Values)
            {
                if (g.Abbreviation.ToLower() == abbr.ToLower())
                    return g;
            }

            return null;
        }

        public static BaseGuild[] Search(string find)
        {
            string[] words = find.ToLower().Split(' ');
            ArrayList results = new ArrayList();

            foreach (BaseGuild g in m_GuildList.Values)
            {
                bool match = true;
                string name = g.Name.ToLower();
                for (int i = 0; i < words.Length; i++)
                {
                    if (name.IndexOf(words[i]) == -1)
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                    results.Add(g);
            }

            return (BaseGuild[])results.ToArray(typeof(BaseGuild));
        }

        public override string ToString()
        {
            return Name;
        }
    }
}