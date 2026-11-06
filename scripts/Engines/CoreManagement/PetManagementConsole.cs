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

/* Scripts\Engines\CoreManagement\PetManagementConsole.cs
 * ChangeLog
 *  9/22/2023, Adam
 *		initial check in
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

using Server.Mobiles;

namespace Server.Engines.Consoles
{
    [NoSort]
    public class PetManagementConsole : BaseConsole
    {
        [Constructable]
        public PetManagementConsole()
            : base("Pet Management Console")
        {
        }

        public PetManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double ActiveSpeedOverride
        {
            get => CoreAI.ActiveSpeedOverride;
            set => CoreAI.ActiveSpeedOverride = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double PassiveSpeedOverride
        {
            get => CoreAI.PassiveSpeedOverride;
            set => CoreAI.PassiveSpeedOverride = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double ActiveMountedSpeedOverride
        {
            get => CoreAI.ActiveMountedSpeedOverride;
            set
            {
                double oldOverride = CoreAI.ActiveMountedSpeedOverride;
                CoreAI.ActiveMountedSpeedOverride = value;

                foreach (BaseHire bh in BaseHire.Registry)
                    if (bh.CurrentSpeed == oldOverride)
                        bh.CurrentSpeed = CoreAI.ActiveMountedSpeedOverride;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double PassiveMountedSpeedOverride
        {
            get => CoreAI.PassiveMountedSpeedOverride;
            set => CoreAI.PassiveMountedSpeedOverride = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double MountEnduranceFactor
        {
            get => CoreAI.MountEnduranceFactor;
            set => CoreAI.MountEnduranceFactor = value;
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}