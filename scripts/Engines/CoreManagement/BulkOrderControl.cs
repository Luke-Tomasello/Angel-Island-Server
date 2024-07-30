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

/* Scripts/Engines/BulkOrders/BulkOrderControl.cs
 * CHANGELOG:
 *  10/27/21, Yoar
 *      All setters now require admin access.
 *      Added AccountWideDelays.
 *  10/26/21, Yoar
 *      Added SmithEnabled, TailorEnabled.
 *  10/25/21, Yoar
 *      Initial version.
 */

using Server.Gumps;
using System;

namespace Server.Engines.BulkOrders
{
    [NoSort]
    public class BulkOrderControl : Item
    {
        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool SystemEnabled
        {
            get { return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BulkOrdersEnabled); }
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
            get { return BulkOrderSystem.EnabledFlags; }
            set { BulkOrderSystem.EnabledFlags = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool LargeEnabled
        {
            get { return BulkOrderSystem.LargeEnabled; }
            set { BulkOrderSystem.LargeEnabled = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool GoldRewards
        {
            get { return BulkOrderSystem.GoldRewards; }
            set { BulkOrderSystem.GoldRewards = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool FameRewards
        {
            get { return BulkOrderSystem.FameRewards; }
            set { BulkOrderSystem.FameRewards = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool RewardsGump
        {
            get { return BulkOrderSystem.RewardsGump; }
            set { BulkOrderSystem.RewardsGump = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool OfferOnTurnIn
        {
            get { return BulkOrderSystem.OfferOnTurnIn; }
            set { BulkOrderSystem.OfferOnTurnIn = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static double GoldScalar
        {
            get { return BulkOrderSystem.GoldScalar; }
            set { BulkOrderSystem.GoldScalar = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static int SmallBankPerc
        {
            get { return BulkOrderSystem.SmallBankPerc; }
            set { BulkOrderSystem.SmallBankPerc = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static int LargeBankPerc
        {
            get { return BulkOrderSystem.LargeBankPerc; }
            set { BulkOrderSystem.LargeBankPerc = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static bool AccountWideDelays
        {
            get { return BulkOrderSystem.AccountWideDelays; }
            set { BulkOrderSystem.AccountWideDelays = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static TimeSpan TinyDelay
        {
            get { return BulkOrderSystem.TinyDelay; }
            set { BulkOrderSystem.TinyDelay = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static TimeSpan SmallDelay
        {
            get { return BulkOrderSystem.SmallDelay; }
            set { BulkOrderSystem.SmallDelay = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static TimeSpan LargeDelay
        {
            get { return BulkOrderSystem.LargeDelay; }
            set { BulkOrderSystem.LargeDelay = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public static TimeSpan TurnInDelay
        {
            get { return BulkOrderSystem.TurnInDelay; }
            set { BulkOrderSystem.TurnInDelay = value; }
        }

        [Constructable]
        public BulkOrderControl()
            : base(0x1F14)
        {
            Name = "Bulk Order Control";
            Weight = 1.0;
            Hue = 0x44E;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Counselor)
                from.SendGump(new PropertiesGump(from, this));
        }

        public BulkOrderControl(Serial serial)
            : base(serial)
        {
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