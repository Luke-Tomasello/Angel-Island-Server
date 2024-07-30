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

/* Engines/CoreManagement/ClientManagementConsole.cs
 * ChangeLog
 *	8/26/2023, Adam
 *		Created.
 *		Management console for the global values stored in Engines/AngelIsland/CoreAI.cs
 *		This console manages aspects of the client software enforcement.
 */

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class ClientManagementConsole : Item
    {
        [Constructable]
        public ClientManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 0x973;
            Name = "Client Management Console";
        }

        public ClientManagementConsole(Serial serial)
            : base(serial)
        {
        }

#if DEBUG
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static bool ForceGMNCUO
        {
            get
            {
                return CoreAI.ForceGMNCUO;
            }
            set
            {
                CoreAI.ForceGMNCUO = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static bool ForceGMNRZR
        {
            get
            {
                return CoreAI.ForceGMNRZR;
            }
            set
            {
                CoreAI.ForceGMNRZR = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static bool FakeBadGMNCUO
        {
            get
            {
                return CoreAI.FakeBadGMNCUO;
            }
            set
            {
                CoreAI.FakeBadGMNCUO = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static bool FakeBadGMNRZR
        {
            get
            {
                return CoreAI.FakeBadGMNRZR;
            }
            set
            {
                CoreAI.FakeBadGMNRZR = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static bool WarnBadGMNCUO
        {
            get
            {
                return CoreAI.WarnBadGMNCUO;
            }
            set
            {
                CoreAI.WarnBadGMNCUO = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static bool WarnBadGMNRZR
        {
            get
            {
                return CoreAI.WarnBadGMNRZR;
            }
            set
            {
                CoreAI.WarnBadGMNRZR = value;
            }
        }
#else
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public static bool ForceGMNCUO
        {
            get
            {
                return CoreAI.ForceGMNCUO;
            }
            set
            {
                CoreAI.ForceGMNCUO = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public static bool ForceGMNRZR
        {
            get
            {
                return CoreAI.ForceGMNRZR;
            }
            set
            {
                CoreAI.ForceGMNRZR = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public static bool FakeBadGMNCUO
        {
            get
            {
                return CoreAI.FakeBadGMNCUO;
            }
            set
            {
                CoreAI.FakeBadGMNCUO = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public static bool FakeBadGMNRZR
        {
            get
            {
                return CoreAI.FakeBadGMNRZR;
            }
            set
            {
                CoreAI.FakeBadGMNRZR = value;
            }
        }
#endif

        [CommandProperty(AccessLevel.Administrator)]
        public bool IPBinderEnabled
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.IPBinderEnabled);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.IPBinderEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.IPBinderEnabled);
            }
        }
        [CommandProperty(AccessLevel.Owner)]
        public int MaxConcurrentAddresses
        {
            get { return CoreAI.MaxConcurrentAddresses; }
            set { CoreAI.MaxConcurrentAddresses = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public static int MaxAccountsPerIP
        {
            get { return CoreAI.MaxAccountsPerIP; }
            set { CoreAI.MaxAccountsPerIP = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public static int MaxAccountsPerMachine
        {
            get { return CoreAI.MaxAccountsPerMachine; }
            set { CoreAI.MaxAccountsPerMachine = value; }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RazorNegFeaturesEnabled
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.RazorNegotiateFeaturesEnabled);
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool RazorNegWarnAndKick
        {
            get
            {
                return CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
            }
            set
            {
                if (value == true)
                    CoreAI.SetDynamicFeature(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
                else
                    CoreAI.ClearDynamicFeature(CoreAI.FeatureBits.RazorNegotiateWarnAndKick);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
            {
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

}