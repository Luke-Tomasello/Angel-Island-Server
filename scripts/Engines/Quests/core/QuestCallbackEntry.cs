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

using Server.ContextMenus;

namespace Server.Engines.Quests
{
    public class QuestCallbackEntry : ContextMenuEntry
    {
        private QuestCallback m_Callback;

        public QuestCallbackEntry(int number, QuestCallback callback)
            : this(number, -1, callback)
        {
        }

        public QuestCallbackEntry(int number, int range, QuestCallback callback)
            : base(number, range)
        {
            m_Callback = callback;
        }

        public override void OnClick()
        {
            if (m_Callback != null)
                m_Callback();
        }
    }
}