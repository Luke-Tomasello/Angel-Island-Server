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

/* Scripts\Mobiles\Special\Christmas Elves\AI\SnowballFight.cs
 * ChangeLog
 * 12/12/23, Yoar
 *      Moved to a separate source file.
 */

using System.Collections.Generic;

namespace Server.Mobiles
{
    public interface ISnowballFight : IEntity
    {
        Mobile Combatant { get; set; }
        SnowballFight SnowballFight { get; set; }
    }

    public class SnowballFight : List<ISnowballFight>
    {
        public static int PoolSize = 100;
        public static int MaxRange = 5;
        public static int MaxCount = 4;

        private static SnowballFight[] m_Pool;
        private static int m_Index;

        public static SnowballFight Get()
        {
            if (m_Pool == null)
                m_Pool = new SnowballFight[PoolSize];

            for (int i = 0; i < m_Pool.Length; i++)
            {
                if (m_Index >= m_Pool.Length)
                    m_Index = 0;

                int index = m_Index++;

                SnowballFight state = m_Pool[index];

                if (state == null)
                    m_Pool[index] = state = new SnowballFight();
                else
                    state.Defrag();

                if (state.Count == 0)
                    return state;
            }

            return new SnowballFight();
        }

        public static void Join(Mobile m)
        {
            ISnowballFight myFight = m as ISnowballFight;

            if (myFight == null)
                return;

            if (myFight.SnowballFight != null)
                myFight.SnowballFight.Defrag();

            int myCount = (myFight.SnowballFight == null ? 0 : myFight.SnowballFight.Count);

            int distMin = int.MaxValue;
            ISnowballFight theirFight = null;

            foreach (Mobile checkMobile in m.GetMobilesInRange(MaxRange))
            {
                int dx = checkMobile.X - m.X;
                int dy = checkMobile.Y - m.Y;

                int dist = dx * dx + dy * dy;

                if (dist <= MaxRange * MaxRange && dist < distMin && checkMobile is ISnowballFight)
                {
                    ISnowballFight checkFight = (ISnowballFight)checkMobile;

                    if (checkFight.SnowballFight != null)
                        checkFight.SnowballFight.Defrag();

                    int checkCount = (checkFight.SnowballFight == null ? 0 : checkFight.SnowballFight.Count);

                    if (myCount + checkCount <= MaxCount)
                    {
                        distMin = dist;
                        theirFight = checkFight;
                    }
                }
            }

            if (theirFight != null)
            {
                int theirCount = (theirFight.SnowballFight == null ? 0 : theirFight.SnowballFight.Count);

                if (myCount == 0 && theirCount == 0)
                {
                    SnowballFight fight = myFight.SnowballFight = theirFight.SnowballFight = Get();

                    fight.Add(myFight);
                    fight.Add(theirFight);
                }
                else if (myCount < theirCount)
                {
                    SnowballFight fight = theirFight.SnowballFight;

                    if (myCount == 0)
                    {
                        myFight.SnowballFight = fight;

                        fight.Add(myFight);
                    }
                    else
                    {
                        for (int i = 0; i < myCount; i++)
                        {
                            myFight.SnowballFight = fight;

                            fight.Add(myFight);
                        }

                        myFight.SnowballFight.Clear();
                    }

                    for (int i = 0; i < theirCount; i++)
                    {
                        if (fight[i].Combatant == m)
                            fight[i].Combatant = null;
                    }
                }
            }
        }

        public static void Quit(Mobile m)
        {
            ISnowballFight mine = m as ISnowballFight;

            if (mine == null || mine.SnowballFight == null)
                return;

            SnowballFight fight = mine.SnowballFight;

            fight.Defrag();

            mine.SnowballFight = null;

            fight.Remove(mine);

            for (int i = 0; i < fight.Count; i++)
            {
                if (fight[i].Combatant == m)
                    fight[i].Combatant = null;
            }
        }

        public static bool IsFriend(Mobile m, Mobile other)
        {
            return GetRelation(m, other) == Relation.Friend;
        }

        public static bool IsEnemy(Mobile m, Mobile other)
        {
            return GetRelation(m, other) == Relation.Enemy;
        }

        private enum Relation : byte
        {
            Unrelated,
            Friend,
            Enemy,
        }

        private static Relation GetRelation(Mobile source, Mobile target)
        {
            ISnowballFight myFight = source as ISnowballFight;

            if (myFight == null || myFight.SnowballFight == null)
                return Relation.Unrelated;

            ISnowballFight theirFight = target as ISnowballFight;

            if (theirFight == null || theirFight.SnowballFight == null)
                return Relation.Unrelated;

            if (myFight.SnowballFight == theirFight.SnowballFight)
                return Relation.Friend;
            else
                return Relation.Enemy;
        }

        private SnowballFight()
            : base()
        {
        }

        public void Defrag()
        {
            int cx = 0;
            int cy = 0;

            for (int i = this.Count - 1; i >= 0; i--)
            {
                ISnowballFight ent = this[i];

                if (ent == null || ent.Deleted)
                {
                    this.RemoveAt(i);
                }
                else
                {
                    cx += ent.X;
                    cy += ent.Y;
                }
            }

            int count = this.Count;

            if (count != 0)
            {
                cx /= count;
                cy /= count;

                for (int i = count - 1; i >= 0; i--)
                {
                    ISnowballFight ent = this[i];

                    int dx = ent.X - cx;
                    int dy = ent.Y - cy;

                    int dist = dx * dx + dy * dy;

                    if (dist > MaxRange * MaxRange)
                    {
                        this.RemoveAt(i);
                        ent.SnowballFight = null;
                    }
                }
            }
        }
    }
}