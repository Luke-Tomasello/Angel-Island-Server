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

/* Engines/CoreManagement/PrisonManagementConsole.cs
 * ChangeLog
 *	8/26/2023, Adam
 *		Created.
 *		Management console for the global values stored in Engines/AngelIsland/CoreAI.cs
 *		This console manages aspects of the prison system.
 */

namespace Server.Items
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class PrisonManagementConsole : Item
    {
        [Constructable]
        public PrisonManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 0x973;
            Name = "Prison Management Console";
        }

        public PrisonManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritDepotBandies
        {
            get
            {
                return CoreAI.SpiritDepotBandies;
            }
            set
            {
                CoreAI.SpiritDepotBandies = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritDepotGHPots
        {
            get
            {
                return CoreAI.SpiritDepotGHPots;
            }
            set
            {
                CoreAI.SpiritDepotGHPots = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritDepotReagents
        {
            get
            {
                return CoreAI.SpiritDepotReagents;
            }
            set
            {
                CoreAI.SpiritDepotReagents = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritDepotTRPots
        {
            get
            {
                return CoreAI.SpiritDepotTRPots;
            }
            set
            {
                CoreAI.SpiritDepotTRPots = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritFirstWaveVirtualArmor
        {
            get
            {
                return CoreAI.SpiritFirstWaveVirtualArmor;
            }
            set
            {
                CoreAI.SpiritFirstWaveVirtualArmor = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritSecondWaveVirtualArmor
        {
            get
            {
                return CoreAI.SpiritSecondWaveVirtualArmor;
            }
            set
            {
                CoreAI.SpiritSecondWaveVirtualArmor = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritThirdWaveVirtualArmor
        {
            get
            {
                return CoreAI.SpiritThirdWaveVirtualArmor;
            }
            set
            {
                CoreAI.SpiritThirdWaveVirtualArmor = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritBossVirtualArmor
        {
            get
            {
                return CoreAI.SpiritBossVirtualArmor;
            }
            set
            {
                CoreAI.SpiritBossVirtualArmor = value;
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