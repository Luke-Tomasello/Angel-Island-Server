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

/* Engines/CoreManagement/ClientManagementConsole.cs
 * ChangeLog
 *  11/21/26, Adam (warnings in Release)
 *      WarnBadGMNCUO and WarnBadGMNRZR exist in Release builds too (they were Debug only, so production's console
 *      couldn't show or change them), marked [ResetAtStartup] like the rest.
 *  11/19/26, Adam (ResetAtStartup)
 *      The settings OwnerTools.ShardInit sets at every start are marked [ResetAtStartup] (ForceGMNCUO, ForceGMNRZR,
 *      WarnBadGMNCUO, WarnBadGMNRZR, the account limits): settable again, to test in game; staff are told a change lasts
 *      until the next restart, and the props gump shows them in yellow.
 *	8/26/2023, Adam
 *		Created.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    public class ClientManagementConsole : BaseConsole
    {
        [Constructable]
        public ClientManagementConsole()
            : base("Client Management Console")
        {
        }

        public ClientManagementConsole(Serial serial)
            : base(serial)
        {
        }

#if DEBUG
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static bool ForceGMNCUO
        {
            get => CoreAI.ForceGMNCUO;
            set => CoreAI.ForceGMNCUO = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static bool ForceGMNRZR
        {
            get => CoreAI.ForceGMNRZR;
            set => CoreAI.ForceGMNRZR = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static bool FakeBadGMNCUO
        {
            get => CoreAI.FakeBadGMNCUO;
            set => CoreAI.FakeBadGMNCUO = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static bool FakeBadGMNRZR
        {
            get => CoreAI.FakeBadGMNRZR;
            set => CoreAI.FakeBadGMNRZR = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static bool WarnBadGMNCUO
        {
            get => CoreAI.WarnBadGMNCUO;
            set => CoreAI.WarnBadGMNCUO = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static bool WarnBadGMNRZR
        {
            get => CoreAI.WarnBadGMNRZR;
            set => CoreAI.WarnBadGMNRZR = value;
        }
#else
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static bool ForceGMNCUO
        {
            get => CoreAI.ForceGMNCUO;
            set => CoreAI.ForceGMNCUO = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static bool ForceGMNRZR
        {
            get => CoreAI.ForceGMNRZR;
            set => CoreAI.ForceGMNRZR = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public static bool FakeBadGMNCUO
        {
            get => CoreAI.FakeBadGMNCUO;
            set => CoreAI.FakeBadGMNCUO = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public static bool FakeBadGMNRZR
        {
            get => CoreAI.FakeBadGMNRZR;
            set => CoreAI.FakeBadGMNRZR = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static bool WarnBadGMNCUO
        {
            get => CoreAI.WarnBadGMNCUO;
            set => CoreAI.WarnBadGMNCUO = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static bool WarnBadGMNRZR
        {
            get => CoreAI.WarnBadGMNRZR;
            set => CoreAI.WarnBadGMNRZR = value;
        }
#endif

        [CommandProperty(AccessLevel.Administrator)]
        public bool IPBinderEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.IPBinderEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.IPBinderEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.IPBinderEnabled);
            }
        }

        [CommandProperty(AccessLevel.Owner)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public int MaxConcurrentAddresses
        {
            get => CoreAI.MaxConcurrentAddresses;
            // configured in ShardInit() (a change here lasts until the next restart)
            set => CoreAI.MaxConcurrentAddresses = value;
        }

        [CommandProperty(AccessLevel.Owner)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public int MaxCharactersPerAccount
        {
            get => CoreAI.MaxCharactersPerAccount;
            // configured in ShardInit() (a change here lasts until the next restart)
            set => CoreAI.MaxCharactersPerAccount = value;
        }
        [CommandProperty(AccessLevel.Administrator)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static int MaxAccountsPerIP
        {
            get => CoreAI.MaxAccountsPerIP;
            // configured in ShardInit() (a change here lasts until the next restart)
            set => CoreAI.MaxAccountsPerIP = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        [ResetAtStartup("OwnerTools.ShardInit")]
        public static int MaxAccountsPerMachine
        {
            get => CoreAI.MaxAccountsPerMachine;
            // configured in ShardInit() (a change here lasts until the next restart)
            set => CoreAI.MaxAccountsPerMachine = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RazorNegFeaturesEnabled
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RazorNegWarnAndKick
        {
            get => CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
            set
            {
                if (value)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
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