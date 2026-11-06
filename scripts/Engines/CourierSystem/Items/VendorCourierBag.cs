/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Items\misc\VendorCourierBag.cs
 * Changelog
 *  8/4/2026, Adam
 *  Created.
 */


using Server.Engines.CourierSystem;

namespace Server.Items
{
    public class VendorCourierBag : BaseContainer, IDyable
    {
        #region Fields

        private Loot.ImbueLevel m_RewardLevel;
        private CourierRewardType m_RewardType;

        #endregion

        #region Properties

        public override int DefaultGumpID
        {
            get { return 0x3D; }
        }

        public override int DefaultDropSound
        {
            get { return 0x48; }
        }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(29, 34, 108, 94); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Loot.ImbueLevel RewardLevel
        {
            get => m_RewardLevel;
            set => m_RewardLevel = CourierRewardCalculator.Normalize(value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CourierRewardType RewardType
        {
            get => m_RewardType;
            set => m_RewardType = value;
        }
        #endregion

        #region Construction

        [Constructable]
        public VendorCourierBag()
            : this(Loot.ImbueLevel.Level1, CourierRewardType.None)
        {
        }

        public VendorCourierBag(
            Loot.ImbueLevel rewardLevel,
            CourierRewardType rewardType)
            : base(0x0E76)
        {
            Weight = 2.0;
            Name = "a courier reward bag";

            m_RewardLevel =
                CourierRewardCalculator.Normalize(rewardLevel);

            m_RewardType = rewardType;
        }

        public VendorCourierBag(Serial serial)
            : base(serial)
        {
        }

        #endregion

        #region Operations

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;

            return true;
        }

        #endregion

        #region Serialization

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_RewardLevel);
            writer.Write((int)m_RewardType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_RewardLevel = CourierRewardCalculator.Normalize(
                            (Loot.ImbueLevel)reader.ReadInt());

                        m_RewardType = (CourierRewardType)reader.ReadInt();

                        break;
                    }
            }
        }

        #endregion
    }
}

