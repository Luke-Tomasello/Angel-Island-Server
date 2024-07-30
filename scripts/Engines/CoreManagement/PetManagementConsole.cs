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

/* Scripts\Engines\CoreManagement\PetManagementConsole.cs
 * ChangeLog
 *  9/22/2023, Adam
 *		initial check in
 */

using Server.Mobiles;

namespace Server.Items
{
    [NoSort]
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class PetManagementConsole : Item
    {
        [Constructable]
        public PetManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 0x534;
            Name = "Item Management Console";
        }

        public PetManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double ActiveSpeedOverride
        {
            get
            {
                return CoreAI.ActiveSpeedOverride;
            }
            set
            {
                CoreAI.ActiveSpeedOverride = value;
            }
        }
        public double PassiveSpeedOverride
        {
            get
            {
                return CoreAI.PassiveSpeedOverride;
            }
            set
            {
                CoreAI.PassiveSpeedOverride = value;
            }
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public double ActiveMountedSpeedOverride
        {
            get
            {
                return CoreAI.ActiveMountedSpeedOverride;
            }
            set
            {
                double oldOverride = CoreAI.ActiveMountedSpeedOverride;
                CoreAI.ActiveMountedSpeedOverride = value;
                // if we ever use ActiveMountedSpeedOverride anywhere besides BaseHire, we'll need to enumerate those as well
                foreach (BaseHire bh in BaseHire.Registry)
                    if (bh.CurrentSpeed == oldOverride)
                        bh.CurrentSpeed = CoreAI.ActiveMountedSpeedOverride;
            }
        }
        public double PassiveMountedSpeedOverride
        {
            get
            {
                return CoreAI.PassiveMountedSpeedOverride;
            }
            set
            {
                CoreAI.PassiveMountedSpeedOverride = value;
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