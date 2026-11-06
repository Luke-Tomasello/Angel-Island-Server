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

/* Engines/CoreManagement/SecurityManagementConsole.cs
 * CHANGELOG
 *  1/18/07 Taran Kain
 *      Initial version.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Misc;
using Server.Mobiles;
using System;

namespace Server.Engines.Consoles
{
    public class SecurityManagementConsole : BaseConsole
    {
        [CommandProperty(AccessLevel.Administrator)]
        public static TimeSpan MovementPacketThrottleThreshold
        {
            get => PlayerMobile.FastwalkThreshold;
            set
            {
                if (value >= TimeSpan.Zero)
                    PlayerMobile.FastwalkThreshold = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static bool MovementPacketThrottlingEnabled
        {
            get => PlayerMobile.FastwalkPrevention;
            set => PlayerMobile.FastwalkPrevention = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static double FastwalkTrapMemory
        {
            get => CoreAI.FastwalkTrapMemory;
            set => CoreAI.FastwalkTrapMemory = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static double FastwalkTrapFactor
        {
            get => CoreAI.FastwalkTrapFactor;
            set => CoreAI.FastwalkTrapFactor = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public static int FastwalkInfractionThreshold
        {
            get => CoreAI.FastwalkInfractionThreshold;
            set => CoreAI.FastwalkInfractionThreshold = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public static bool JailFastwalkers
        {
            get => CoreAI.JailFastwalkers;
            set => CoreAI.JailFastwalkers = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public static bool JailLogin
        {
            get => CoreAI.JailLogin;
            set => CoreAI.JailLogin = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static int PacketThrottleCountWarningThreshold
        {
            get => PlayerMobile.ThrottleRunWarningThreshold;
            set => PlayerMobile.ThrottleRunWarningThreshold = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static AccessLevel FastwalkAccessOverride
        {
            get => PlayerMobile.FastWalkAccessOverride;
            set => PlayerMobile.FastWalkAccessOverride = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static int FwdMaxStepsMount
        {
            get => Mobile.FwdMaxStepsMount;
            set
            {
                if (value >= 0)
                    Mobile.FwdMaxStepsMount = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static int FwdMaxStepsFoot
        {
            get => Mobile.FwdMaxStepsFoot;
            set
            {
                if (value >= 0)
                    Mobile.FwdMaxStepsFoot = value;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static bool FastWalkDetectionEnabled
        {
            get => Mobile.FwdEnabled;
            set => Mobile.FwdEnabled = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static bool FastWalkProtectionEnabled
        {
            get => Fastwalk.ProtectionEnabled;
            set => Fastwalk.ProtectionEnabled = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static bool FwdUOTDOverride
        {
            get => Mobile.FwdUOTDOverride;
            set => Mobile.FwdUOTDOverride = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static AccessLevel FwdAccessOverride
        {
            get => Mobile.FwdAccessOverride;
            set => Mobile.FwdAccessOverride = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static int FwdWarningThreshold
        {
            get => Fastwalk.WarningThreshold;
            set => Fastwalk.WarningThreshold = value;
        }

        [CommandProperty(AccessLevel.Administrator)]
        public static string ResetDefaults
        {
            get => "Enter \"yes\" to reset defaults.";
            set
            {
                if (Insensitive.Equals(value, "yes"))
                    SetDefaults();
            }
        }

        public static void Configure() => SetDefaults();

        public static void SetDefaults()
        {
            FastWalkDetectionEnabled = true;
            FastWalkProtectionEnabled = false;
            FwdAccessOverride = AccessLevel.GameMaster;
            FwdMaxStepsMount = 4;
            FwdMaxStepsFoot = 2;

            FastwalkTrapMemory = 10.0;
            FastwalkInfractionThreshold = 3;
            FastwalkTrapFactor = 1.2;

            FwdUOTDOverride = false;
            FwdWarningThreshold = 5;

            MovementPacketThrottlingEnabled = true;
            MovementPacketThrottleThreshold = TimeSpan.FromSeconds(0.1);
            PacketThrottleCountWarningThreshold = 5;
            FastwalkAccessOverride = AccessLevel.GameMaster;
        }

        [Constructable]
        public SecurityManagementConsole()
            : base("Security Management Console")
        {
        }

        public SecurityManagementConsole(Serial s)
            : base(s)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Administrator)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write(FwdMaxStepsFoot);
            writer.Write(FastWalkDetectionEnabled);
            writer.Write(FastWalkProtectionEnabled);
            writer.Write((int)FwdAccessOverride);
            writer.Write(FwdMaxStepsMount);
            writer.Write(FwdUOTDOverride);
            writer.Write(FwdWarningThreshold);

            writer.Write(MovementPacketThrottleThreshold);
            writer.Write(MovementPacketThrottlingEnabled);
            writer.Write(PacketThrottleCountWarningThreshold);
            writer.Write((int)FastwalkAccessOverride);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    FwdMaxStepsFoot = reader.ReadInt();
                    goto case 0;
                case 0:
                    FastWalkDetectionEnabled = reader.ReadBool();
                    FastWalkProtectionEnabled = reader.ReadBool();
                    FwdAccessOverride = (AccessLevel)reader.ReadInt();
                    FwdMaxStepsMount = reader.ReadInt();
                    FwdUOTDOverride = reader.ReadBool();
                    FwdWarningThreshold = reader.ReadInt();

                    MovementPacketThrottleThreshold = reader.ReadTimeSpan();
                    MovementPacketThrottlingEnabled = reader.ReadBool();
                    PacketThrottleCountWarningThreshold = reader.ReadInt();
                    FastwalkAccessOverride = (AccessLevel)reader.ReadInt();
                    break;

                default:
                    throw new Exception("Invalid Security Management Console save version.");
            }
        }
    }
}