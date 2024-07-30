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

/* Scripts/Engines/BulkOrders/BulkOrderContext.cs
 * CHANGELOG:
 *  11/22/23, Yoar
 *      Removed BulkOrderInstance.
 *      Contexts are now stored per BulkOrderSystem
 *  10/25/21, Yoar
 *      Added NextTurnIn
 *  10/14/21, Yoar
 *      Initial version.
 *      This class stores auxiliary player data related to the Bulk Order System.
 */

using System;

namespace Server.Engines.BulkOrders
{
    [PropertyObject]
    public class BulkOrderContext
    {
        private Mobile m_Owner;
        private DateTime m_NextBOD;
        private double m_Banked;
        private PendingReward m_Pending;
        private Item m_CurrentOffer;
        private DateTime m_NextTurnIn;
        private BankingSetting m_BankingSetting;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextBOD
        {
            get { return m_NextBOD; }
            set { m_NextBOD = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Banked
        {
            get { return m_Banked; }
            set { m_Banked = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PendingReward Pending
        {
            get { return m_Pending; }
            set { m_Pending = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item CurrentOffer
        {
            get { return m_CurrentOffer; }
            set { m_CurrentOffer = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime NextTurnIn
        {
            get { return m_NextTurnIn; }
            set { m_NextTurnIn = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BankingSetting BankingSetting
        {
            get { return m_BankingSetting; }
            set { m_BankingSetting = value; }
        }

        public BulkOrderContext(Mobile owner)
        {
            m_Owner = owner;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.WriteDeltaTime(m_NextBOD);
            writer.Write((double)m_Banked);
            m_Pending.Serialize(writer);
            writer.Write((Item)m_CurrentOffer);
            writer.WriteDeltaTime(m_NextTurnIn);
            writer.Write((byte)m_BankingSetting);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            m_NextBOD = reader.ReadDeltaTime();
            m_Banked = reader.ReadDouble();
            m_Pending = new PendingReward(reader);
            Item currentOffer = reader.ReadItem();
            m_NextTurnIn = reader.ReadDeltaTime();
            m_BankingSetting = (BankingSetting)reader.ReadByte();

            // make sure we clean up old BOD offers
            if (currentOffer != null)
                currentOffer.Delete();
        }

        public override string ToString()
        {
            return "...";
        }
    }

    [PropertyObject]
    public struct PendingReward
    {
        public static readonly PendingReward Zero = new PendingReward();

        private int m_Points;
        private bool m_Large;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Points { get { return m_Points; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Large { get { return m_Large; } }

        public PendingReward(int points, bool large)
        {
            m_Points = points;
            m_Large = large;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)m_Points);
            writer.Write((bool)m_Large);
        }

        public PendingReward(GenericReader reader)
        {
            m_Points = reader.ReadInt();
            m_Large = reader.ReadBool();
        }

        public override string ToString()
        {
            return "...";
        }
    }
}