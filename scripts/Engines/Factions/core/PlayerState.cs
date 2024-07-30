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

using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Factions
{
    [PropertyObject]
    public class PlayerState : IComparable
    {
        private Mobile m_Mobile;
        private Faction m_Faction;
        private List<PlayerState> m_Owner;
        private int m_KillPoints;
        private DateTime m_LeaveBegin;
        private MerchantTitle m_MerchantTitle;
        private RankDefinition m_Rank;
        private List<SilverGivenEntry> m_SilverGiven;
        private bool m_IsActive;

        private Town m_Sheriff;
        private Town m_Finance;

        private DateTime m_LastHonorTime;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Mobile { get { return m_Mobile; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Faction Faction { get { return m_Faction; } }

        public List<PlayerState> Owner { get { return m_Owner; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public MerchantTitle MerchantTitle { get { return m_MerchantTitle; } set { m_MerchantTitle = value; Invalidate(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Town Sheriff { get { return m_Sheriff; } set { m_Sheriff = value; Invalidate(); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Town Finance { get { return m_Finance; } set { m_Finance = value; Invalidate(); } }

        public List<SilverGivenEntry> SilverGiven { get { return m_SilverGiven; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int KillPoints
        {
            get { return m_KillPoints; }
            set
            {
                if (m_KillPoints != value)
                {
                    if (value > m_KillPoints)
                    {
                        if (m_KillPoints <= 0)
                        {
                            if (value <= 0)
                            {
                                m_KillPoints = value;
                                Invalidate();
                                return;
                            }

                            m_Owner.Remove(this);
                            m_Owner.Insert(m_Faction.ZeroRankOffset, this);

                            m_RankIndex = m_Faction.ZeroRankOffset;
                            m_Faction.ZeroRankOffset++;
                        }
                        while ((m_RankIndex - 1) >= 0)
                        {
                            PlayerState p = m_Owner[m_RankIndex - 1] as PlayerState;
                            if (value > p.KillPoints)
                            {
                                m_Owner[m_RankIndex] = p;
                                m_Owner[m_RankIndex - 1] = this;
                                RankIndex--;
                                p.RankIndex++;
                            }
                            else
                                break;
                        }
                    }
                    else
                    {
                        if (value <= 0)
                        {
                            if (m_KillPoints <= 0)
                            {
                                m_KillPoints = value;
                                Invalidate();
                                return;
                            }

                            while ((m_RankIndex + 1) < m_Faction.ZeroRankOffset)
                            {
                                PlayerState p = m_Owner[m_RankIndex + 1] as PlayerState;
                                m_Owner[m_RankIndex + 1] = this;
                                m_Owner[m_RankIndex] = p;
                                RankIndex++;
                                p.RankIndex--;
                            }

                            m_RankIndex = -1;
                            m_Faction.ZeroRankOffset--;
                        }
                        else
                        {
                            while ((m_RankIndex + 1) < m_Faction.ZeroRankOffset)
                            {
                                PlayerState p = m_Owner[m_RankIndex + 1] as PlayerState;
                                if (value < p.KillPoints)
                                {
                                    m_Owner[m_RankIndex + 1] = this;
                                    m_Owner[m_RankIndex] = p;
                                    RankIndex++;
                                    p.RankIndex--;
                                }
                                else
                                    break;
                            }
                        }
                    }

                    m_KillPoints = value;
                    Invalidate();
                }
            }
        }

        private bool m_InvalidateRank = true;
        private int m_RankIndex = -1;

        public int RankIndex { get { return m_RankIndex; } set { if (m_RankIndex != value) { m_RankIndex = value; m_InvalidateRank = true; } } }

        public RankDefinition Rank
        {
            get
            {
                if (m_InvalidateRank)
                {
                    RankDefinition[] ranks = m_Faction.Definition.Ranks;
                    int percent;

                    if (m_Owner.Count == 1)
                        percent = 1000;
                    else if (m_RankIndex == -1)
                        percent = 0;
                    else
                        percent = ((m_Faction.ZeroRankOffset - m_RankIndex) * 1000) / m_Faction.ZeroRankOffset;

                    for (int i = 0; i < ranks.Length; i++)
                    {
                        RankDefinition check = ranks[i];

                        if (percent >= check.Required)
                        {
                            m_Rank = check;
                            m_InvalidateRank = false;
                            break;
                        }
                    }

                    Invalidate();
                }

                return m_Rank;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastHonorTime { get { return m_LastHonorTime; } set { m_LastHonorTime = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LeaveBegin { get { return m_LeaveBegin; } set { m_LeaveBegin = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsLeaving { get { return (m_LeaveBegin > DateTime.MinValue); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsActive { get { return m_IsActive; } set { m_IsActive = value; } }

        public bool CanGiveSilverTo(Mobile mob)
        {
            if (m_SilverGiven == null)
                return true;

            for (int i = 0; i < m_SilverGiven.Count; ++i)
            {
                SilverGivenEntry sge = m_SilverGiven[i];

                if (sge.IsExpired)
                    m_SilverGiven.RemoveAt(i--);
                else if (sge.GivenTo == mob)
                    return false;
            }

            return true;
        }

        public void OnGivenSilverTo(Mobile mob)
        {
            if (m_SilverGiven == null)
                m_SilverGiven = new List<SilverGivenEntry>();

            m_SilverGiven.Add(new SilverGivenEntry(mob));
        }

        public void Invalidate()
        {
            if (m_Mobile is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)m_Mobile;
                pm.InvalidateProperties();
                //pm.InvalidateMyRunUO();
            }
        }

        public void Attach()
        {
            if (m_Mobile is PlayerMobile)
                ((PlayerMobile)m_Mobile).FactionPlayerState = this;
        }

        public PlayerState(Mobile mob, Faction faction, List<PlayerState> owner)
        {
            m_Mobile = mob;
            m_Faction = faction;
            m_Owner = owner;

            Attach();
            Invalidate();
        }

        public PlayerState(GenericReader reader, Faction faction, List<PlayerState> owner)
        {
            m_Faction = faction;
            m_Owner = owner;

            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 1:
                    {
                        m_IsActive = reader.ReadBool();
                        m_LastHonorTime = reader.ReadDateTime();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Mobile = reader.ReadMobile();

                        m_KillPoints = reader.ReadEncodedInt();
                        m_MerchantTitle = (MerchantTitle)reader.ReadEncodedInt();

                        m_LeaveBegin = reader.ReadDateTime();

                        break;
                    }
            }

            Attach();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt((int)1); // version

            writer.Write(m_IsActive);
            writer.Write(m_LastHonorTime);

            writer.Write((Mobile)m_Mobile);

            writer.WriteEncodedInt((int)m_KillPoints);
            writer.WriteEncodedInt((int)m_MerchantTitle);

            writer.Write((DateTime)m_LeaveBegin);
        }

        public static PlayerState Find(Mobile mob)
        {
            if (mob is PlayerMobile)
                return ((PlayerMobile)mob).FactionPlayerState;

            return null;
        }

        public int CompareTo(object obj)
        {
            return ((PlayerState)obj).m_KillPoints - m_KillPoints;
        }

        public override string ToString()
        {
            return "...";
        }
    }
}