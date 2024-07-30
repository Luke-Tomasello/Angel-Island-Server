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

/* Items/Containers/BaseTreasureChest.cs
 * ChangeLog:
 *   4/27/2004, pixie
 *     Changed so telekinesis doesn't trip the trap
 */

using System;

namespace Server.Items
{
    public abstract class BaseTreasureChest : LockableContainer
    {
        private TreasureLevel m_TreasureLevel;
        private short m_MaxSpawnTime = 60;
        private short m_MinSpawnTime = 10;
        private TreasureResetTimer m_ResetTimer;
        // We generate our own items on dupe, therefore DeepDupe need not dupe our items
        public override bool AutoFills { get { return true; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public TreasureLevel Level
        {
            get
            {
                return m_TreasureLevel;
            }
            set
            {
                m_TreasureLevel = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public short MaxSpawnTime
        {
            get
            {
                return m_MaxSpawnTime;
            }
            set
            {
                m_MaxSpawnTime = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public short MinSpawnTime
        {
            get
            {
                return m_MinSpawnTime;
            }
            set
            {
                m_MinSpawnTime = value;
            }
        }

        public BaseTreasureChest(int itemID)
            : base(itemID)
        {
            m_TreasureLevel = TreasureLevel.Level2;
            Locked = true;
            Movable = false;
            Name = "a locked treasure chest";

            Key key = (Key)FindItemByType(typeof(Key));

            if (key != null)
                key.Delete();

            SetLockLevel();
            GenerateTreasure();
        }

        public BaseTreasureChest(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
            writer.Write((byte)m_TreasureLevel);
            writer.Write(m_MinSpawnTime);
            writer.Write(m_MaxSpawnTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_TreasureLevel = (TreasureLevel)reader.ReadByte();
            m_MinSpawnTime = reader.ReadShort();
            m_MaxSpawnTime = reader.ReadShort();

            if (!Locked)
                StartResetTimer();
        }

        protected virtual void SetLockLevel()
        {
            switch (m_TreasureLevel)
            {
                case TreasureLevel.Level1:
                    this.RequiredSkill = this.LockLevel = 5;
                    break;

                case TreasureLevel.Level2:
                    this.RequiredSkill = this.LockLevel = 20;
                    break;

                case TreasureLevel.Level3:
                    this.RequiredSkill = this.LockLevel = 50;
                    break;

                case TreasureLevel.Level4:
                    this.RequiredSkill = this.LockLevel = 70;
                    break;

                case TreasureLevel.Level5:
                    this.RequiredSkill = this.LockLevel = 90;
                    break;
            }
        }

        private void StartResetTimer()
        {
            if (m_ResetTimer == null)
                m_ResetTimer = new TreasureResetTimer(this);
            else
                m_ResetTimer.Delay = TimeSpan.FromMinutes(Utility.Random(m_MinSpawnTime, m_MaxSpawnTime));

            m_ResetTimer.Start();
        }

        protected virtual void GenerateTreasure()
        {
            int MinGold = 1;
            int MaxGold = 2;

            switch (m_TreasureLevel)
            {
                case TreasureLevel.Level1:
                    MinGold = 100;
                    MaxGold = 300;
                    break;

                case TreasureLevel.Level2:
                    MinGold = 300;
                    MaxGold = 600;
                    break;

                case TreasureLevel.Level3:
                    MinGold = 600;
                    MaxGold = 900;
                    break;

                case TreasureLevel.Level4:
                    MinGold = 900;
                    MaxGold = 1200;
                    break;

                case TreasureLevel.Level5:
                    MinGold = 1200;
                    MaxGold = 5000;
                    break;
            }
            DropItem(new Gold(MinGold, MaxGold));
        }

        public override void LockPick(Mobile from)
        {
            base.LockPick(from);

            Name = "a treasure chest";
            StartResetTimer();
        }

        public void ClearContents()
        {
            for (int i = Items.Count - 1; i >= 0; --i)
                if (i < Items.Count)
                    ((Item)Items[i]).Delete();
        }

        public void Reset()
        {
            if (m_ResetTimer != null)
            {
                if (m_ResetTimer.Running)
                    m_ResetTimer.Stop();
            }

            Locked = true;
            Name = "a locked treasure chest";
            ClearContents();
            GenerateTreasure();
        }

        public override void OnTelekinesis(Mobile from)
        {
            //Do nothing, telekinesis doesn't work on a TMap.
        }

        public enum TreasureLevel
        {
            Level1,
            Level2,
            Level3,
            Level4,
            Level5
        };

        private class TreasureResetTimer : Timer
        {
            private BaseTreasureChest m_Chest;

            public TreasureResetTimer(BaseTreasureChest chest)
                : base(TimeSpan.FromMinutes(Utility.Random(chest.MinSpawnTime, chest.MaxSpawnTime)))
            {
                m_Chest = chest;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                m_Chest.Reset();
            }
        }
    }
}