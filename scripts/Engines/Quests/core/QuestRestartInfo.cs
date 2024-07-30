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

using System;

namespace Server.Engines.Quests
{
    public class QuestRestartInfo
    {
        private Type m_QuestType;
        private DateTime m_RestartTime;

        public Type QuestType
        {
            get { return m_QuestType; }
            set { m_QuestType = value; }
        }

        public DateTime RestartTime
        {
            get { return m_RestartTime; }
            set { m_RestartTime = value; }
        }

        public void Reset(TimeSpan restartDelay)
        {
            if (restartDelay < TimeSpan.MaxValue)
                m_RestartTime = DateTime.UtcNow + restartDelay;
            else
                m_RestartTime = DateTime.MaxValue;
        }

        public QuestRestartInfo(Type questType, TimeSpan restartDelay)
        {
            m_QuestType = questType;
            Reset(restartDelay);
        }

        public QuestRestartInfo(Type questType, DateTime restartTime)
        {
            m_QuestType = questType;
            m_RestartTime = restartTime;
        }
    }
}