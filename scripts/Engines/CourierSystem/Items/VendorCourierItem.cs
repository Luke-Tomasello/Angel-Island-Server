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

/* Scripts\Items\misc\VendorCourierItem.cs
 * Changelog
 *  8/4/2026, Adam
 *  Created.
 */

using Server.Engines.CourierSystem;
using Server.Mobiles;
using System;
using static Server.Loot;

namespace Server.Items
{
    public enum CourierAssignmentType
    {
        None,
        Ledger,
        Letter,
        Manifest,
        Map,
        SupplyOrder,
        Parcel,
        CodedMessage
    }

    public class VendorCourierItem : Item
    {
        #region Fields

        private Serial m_CourierSerial;
        private Serial m_SourceVendorSerial;
        private Serial m_DestinationVendorSerial;
        private Serial m_RewardItemSerial;
        private bool m_Redeemed;

        private DateTime m_Expires;

        private ImbueLevel m_RewardLevel;
        private CourierAssignmentType m_AssignmentType;

        private string? m_DestinationDescription;

        #endregion

        #region Properties
        [CommandProperty(AccessLevel.GameMaster)]
        public Serial RewardItemSerial
        {
            get => m_RewardItemSerial;
            set => m_RewardItemSerial = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item RewardItem
        {
            get => World.FindItem(m_RewardItemSerial);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Redeemed
        {
            get => m_Redeemed;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasReward
        {
            get
            {
                Item reward = RewardItem;

                return reward != null && !reward.Deleted;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial CourierSerial
        {
            get => m_CourierSerial;
            set => m_CourierSerial = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial SourceVendorSerial
        {
            get => m_SourceVendorSerial;
            set => m_SourceVendorSerial = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial DestinationVendorSerial
        {
            get => m_DestinationVendorSerial;
            set => m_DestinationVendorSerial = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Courier
        {
            get => World.FindMobile(m_CourierSerial);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseVendor SourceVendor
        {
            get => World.FindMobile(m_SourceVendorSerial) as BaseVendor;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseVendor DestinationVendor
        {
            get => World.FindMobile(m_DestinationVendorSerial) as BaseVendor;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Expires
        {
            get => m_Expires;
            set => m_Expires = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan TimeRemaining
        {
            get
            {
                TimeSpan remaining = m_Expires - DateTime.UtcNow;

                return remaining > TimeSpan.Zero
                    ? remaining
                    : TimeSpan.Zero;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Expired
        {
            get
            {
                return
                    m_Expires != DateTime.MinValue &&
                    DateTime.UtcNow >= m_Expires;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ImbueLevel RewardLevel
        {
            get => m_RewardLevel;
            set => m_RewardLevel = CourierRewardCalculator.Normalize(value);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CourierAssignmentType AssignmentType
        {
            get => m_AssignmentType;
            set
            {
                m_AssignmentType = value;
                UpdateAppearance();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string? DestinationDescription
        {
            get => m_DestinationDescription;
            set => m_DestinationDescription = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasCourier
        {
            get => Courier != null;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasSourceVendor
        {
            get => SourceVendor != null;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasDestinationVendor
        {
            get => DestinationVendor != null;
        }

        #endregion

        #region Operations
        public void MarkRedeemed()
        {
            m_Redeemed = true;
        }

        public Item ReleaseReward()
        {
            Item reward = RewardItem;

            /*
             * Sever ownership before the courier item is deleted.
             * This prevents OnDelete() from deleting a successfully
             * released reward.
             */
            m_Redeemed = true;
            m_RewardItemSerial = Serial.MinusOne;

            return reward;
        }

        public void Invalidate()
        {
            MarkRedeemed();
            RewardItemSerial = Serial.MinusOne;
            Delete();
        }
        #endregion

        public override void OnDelete()
        {
            /*
             * Clear the accepting character's active-assignment reference.
             */
            Mobile? mobile = World.FindMobile(m_CourierSerial);

            if (mobile is PlayerMobile player)
                player.ClearActiveCourierAssignment(Serial);

            /*
             * Until redemption, this courier item owns the escrow reward.
             */
            if (!m_Redeemed)
            {
                Item reward = RewardItem;

                if (reward != null && !reward.Deleted)
                    reward.Delete();
            }

            m_RewardItemSerial = Serial.MinusOne;

            base.OnDelete();
        }

        #region Construction

        [Constructable]
        public VendorCourierItem()
            : base(0x0FF2) // simple book graphic for now
        {
            Weight = 1.0;
            Movable = true;

            m_CourierSerial = Serial.MinusOne;
            m_SourceVendorSerial = Serial.MinusOne;
            m_DestinationVendorSerial = Serial.MinusOne;
            m_RewardItemSerial = Serial.MinusOne;
            m_Redeemed = false;

            m_Expires = Created + TimeSpan.FromHours(12.0);

            m_RewardLevel = CourierRewardCalculator.Normalize(0);
            m_AssignmentType = CourierAssignmentType.Ledger;

            m_DestinationDescription = null;

            UpdateAppearance();
        }

        public VendorCourierItem(
            Mobile courier,
            BaseVendor source,
            BaseVendor destination,
            Item reward,
            ImbueLevel rewardLevel,
            TimeSpan duration,
            CourierAssignmentType assignmentType,
            string destinationDescription)
            : this()
        {
            m_CourierSerial = courier?.Serial ?? Serial.MinusOne;
            m_SourceVendorSerial = source?.Serial ?? Serial.MinusOne;
            m_DestinationVendorSerial = destination?.Serial ?? Serial.MinusOne;
            m_RewardItemSerial = reward?.Serial ?? Serial.MinusOne;

            m_Expires = DateTime.UtcNow + duration;
            m_RewardLevel = CourierRewardCalculator.Normalize(rewardLevel);
            m_AssignmentType = assignmentType;
            m_DestinationDescription = destinationDescription;

            UpdateAppearance();
        }

        public VendorCourierItem(Serial serial)
            : base(serial)
        {
        }

        #endregion

        #region Appearance

        private void UpdateAppearance()
        {
            switch (m_AssignmentType)
            {
                case CourierAssignmentType.Letter:
                    ItemID = 0x14ED;
                    Name = "a sealed letter";
                    break;

                case CourierAssignmentType.Manifest:
                    ItemID = 0x14EF;
                    Name = "a shipping manifest";
                    break;

                case CourierAssignmentType.Map:
                    ItemID = 0x14EB;
                    Name = "a rolled map";
                    break;

                case CourierAssignmentType.SupplyOrder:
                    ItemID = 0x0FF2;
                    Name = "a sealed supply order";
                    break;

                case CourierAssignmentType.Parcel:
                    ItemID = 0x0E3B;
                    Name = "a wrapped parcel";
                    break;

                case CourierAssignmentType.CodedMessage:
                    ItemID = 0x0EFA;
                    Name = "a coded message";
                    break;

                case CourierAssignmentType.Ledger:
                    ItemID = 0x0FF2;
                    Name = "a sealed merchant ledger";
                    break;

                default:
                    ItemID = 0x0FF2;
                    Name = "a sealed document";
                    break;
            }
        }

        #endregion

        #region Interaction

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (!string.IsNullOrWhiteSpace(m_DestinationDescription))
                LabelTo(from, $"For delivery to {m_DestinationDescription}");

            Mobile courier = Courier;

            if (courier != null)
                LabelTo(from, $"Entrusted to {courier.Name}");

            LabelTo(from, $"The pay is {CourierRewardCalculator.GetPayDescription(m_RewardLevel)}");

            if (Expired)
            {
                LabelTo(from, "This delivery has expired.");
            }
            else if (m_Expires != DateTime.MinValue)
            {
                LabelTo(from, FormatExpirationLabel());
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendMessage("That must be in your backpack to examine it.");
                return;
            }

            from.SendMessage(0x3B2, "Courier instructions:");

            BaseVendor recipient = DestinationVendor;

            if (recipient == null || recipient.Deleted)
            {
                from.SendMessage(
                    0x22,
                    "The intended recipient is no longer available. This delivery cannot be completed.");

                return;
            }

            if (!string.IsNullOrWhiteSpace(m_DestinationDescription))
            {
                from.SendMessage(
                    0x3B2,
                    $"Deliver this to {m_DestinationDescription}.");
            }
            else
            {
                BaseVendor destination = DestinationVendor;

                if (destination != null)
                {
                    from.SendMessage(
                        0x3B2,
                        $"Deliver this to {GetVendorDescription(destination)}.");
                }
                else
                {
                    from.SendMessage(
                        0x22,
                        "The intended recipient can no longer be identified.");
                }
            }

            Mobile courier = Courier;

            if (courier != null)
            {
                from.SendMessage(
                    0x3B2,
                    $"This delivery was entrusted to {courier.Name}.");
            }

            if (Expired)
            {
                from.SendMessage(
                    0x22,
                    "The delivery has expired.");
            }
            else
            {
                from.SendMessage(
                    0x3B2,
                    FormatExpirationSentence());
            }
        }

        private static string GetVendorDescription(BaseVendor vendor)
        {
            if (vendor == null)
                return "an unknown recipient";

            string name = string.IsNullOrWhiteSpace(vendor.Name)
                ? "an unnamed vendor"
                : vendor.Name;

            if (!string.IsNullOrWhiteSpace(vendor.Title))
                return $"{name}, {vendor.Title}";

            return name;
        }

        private string FormatExpirationLabel()
        {
            TimeSpan remaining = TimeRemaining;

            if (remaining.TotalHours >= 2.0)
                return $"Expires in approximately {(int)Math.Ceiling(remaining.TotalHours)} hours";

            if (remaining.TotalMinutes >= 2.0)
                return $"Expires in approximately {(int)Math.Ceiling(remaining.TotalMinutes)} minutes";

            return "Expires very soon";
        }

        private string FormatExpirationSentence()
        {
            TimeSpan remaining = TimeRemaining;

            if (remaining.TotalHours >= 2.0)
            {
                int hours = (int)Math.Ceiling(remaining.TotalHours);

                return $"The delivery expires in approximately {hours} hours.";
            }

            if (remaining.TotalMinutes >= 2.0)
            {
                int minutes = (int)Math.Ceiling(remaining.TotalMinutes);

                return $"The delivery expires in approximately {minutes} minutes.";
            }

            return "The delivery expires very soon.";
        }

        #endregion

        #region Serialization
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(1); // version

            writer.Write(m_CourierSerial);
            writer.Write(m_SourceVendorSerial);
            writer.Write(m_DestinationVendorSerial);

            writer.Write(m_Expires);

            writer.Write((int)m_RewardLevel);
            writer.Write((int)m_AssignmentType);

            writer.Write(m_DestinationDescription);

            writer.Write(m_RewardItemSerial);
            writer.Write(m_Redeemed);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_CourierSerial = reader.ReadInt();
                        m_SourceVendorSerial = reader.ReadInt();
                        m_DestinationVendorSerial = reader.ReadInt();

                        m_Expires = reader.ReadDateTime();

                        m_RewardLevel = CourierRewardCalculator.Normalize((ImbueLevel)reader.ReadInt());
                        m_AssignmentType =
                            (CourierAssignmentType)reader.ReadInt();

                        m_DestinationDescription = reader.ReadString();

                        m_RewardItemSerial = reader.ReadInt();
                        m_Redeemed = reader.ReadBool();

                        break;
                    }
            }

            if (m_Expires == DateTime.MinValue)
                m_Expires = DateTime.UtcNow + TimeSpan.FromHours(12.0);

            m_RewardLevel = CourierRewardCalculator.Normalize(m_RewardLevel);

            UpdateAppearance();
        }

        #endregion
    }
}