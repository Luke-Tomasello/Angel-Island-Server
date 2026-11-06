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

/* Scripts/Engines/BulkOrders/BulkOrderControl.cs
 * CHANGELOG:
 *  21-Jan-26, Yoar
 *      Added RewardGoldByCraftValue
 *  10/27/21, Yoar
 *      All setters now require admin access.
 *      Added AccountWideDelays.
 *  10/26/21, Yoar
 *      Added SmithEnabled, TailorEnabled.
 *  10/25/21, Yoar
 *      Initial version.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Engines.BulkOrders;
using Server.Gumps;
using System;

namespace Server.Engines.Consoles
{
    [NoSort]
    public class BulkOrderControl : BaseConsole
    {
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool SystemEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BulkOrdersEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.BulkOrdersEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.BulkOrdersEnabled);
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static BulkOrderFlags EnabledFlags
        {
            get => BulkOrderSystem.EnabledFlags;
            set => BulkOrderSystem.EnabledFlags = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool LargeEnabled
        {
            get => BulkOrderSystem.LargeEnabled;
            set => BulkOrderSystem.LargeEnabled = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool GoldRewards
        {
            get => BulkOrderSystem.GoldRewards;
            set => BulkOrderSystem.GoldRewards = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool FameRewards
        {
            get => BulkOrderSystem.FameRewards;
            set => BulkOrderSystem.FameRewards = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool RewardsGump
        {
            get => BulkOrderSystem.RewardsGump;
            set => BulkOrderSystem.RewardsGump = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Owner)]
        public GoldRewardModel GoldRewardModel
        {
            get => BulkOrderSystem.GoldRewardModel;
            set
            {
                BulkOrderSystem.GoldRewardModel = value;

                try
                {
                    if (RootParent is Mobile m)
                    {
                        string desc = value switch
                        {
                            GoldRewardModel.GoldCraftValue => "Effort-based model: material cost + skill difficulty + exceptional scaling.",
                            GoldRewardModel.GoldVendorHalf => "Vendor-based model: half of NPC resale value per item.",
                            _ => "Unknown gold reward model."
                        };

                        m.SendMessage(value == GoldRewardModel.GoldVendorHalf
                            ? "GoldRewardModel set to [OBSOLETE] {0}. {1}"
                            : "GoldRewardModel set to {0}. {1}", value, desc);
                    }
                }
                catch { }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static double GoldScalar
        {
            get => BulkOrderSystem.GlobalGoldScalar;
            set => BulkOrderSystem.GlobalGoldScalar = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static int SmallBankPerc
        {
            get => BulkOrderSystem.SmallBankPerc;
            set => BulkOrderSystem.SmallBankPerc = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static int LargeBankPerc
        {
            get => BulkOrderSystem.LargeBankPerc;
            set => BulkOrderSystem.LargeBankPerc = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool AccountWideDelays
        {
            get => BulkOrderSystem.AccountWideDelays;
            set => BulkOrderSystem.AccountWideDelays = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Owner)]
        public static bool OfferOnTurnIn
        {
            get => BulkOrderSystem.OfferOnTurnIn;
            set => BulkOrderSystem.OfferOnTurnIn = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Owner)]
        public static TimeSpan TinyDelay
        {
            get => BulkOrderSystem.TinyDelay;
            set => BulkOrderSystem.TinyDelay = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Owner)]
        public static TimeSpan SmallDelay
        {
            get => BulkOrderSystem.SmallDelay;
            set => BulkOrderSystem.SmallDelay = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Owner)]
        public static TimeSpan LargeDelay
        {
            get => BulkOrderSystem.LargeDelay;
            set => BulkOrderSystem.LargeDelay = value;
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static TimeSpan TurnInDelay
        {
            get => BulkOrderSystem.TurnInDelay;
            set => BulkOrderSystem.TurnInDelay = value;
        }

        [Constructable]
        public BulkOrderControl()
            : base("Bulk Order Control")
        {
        }

        public BulkOrderControl(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Counselor)
                from.SendGump(new PropertiesGump(from, this));
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}