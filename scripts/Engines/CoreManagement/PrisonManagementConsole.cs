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

/* Engines/CoreManagement/PrisonManagementConsole.cs
 * ChangeLog
 *	8/26/2023, Adam
 *		Created.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    public class PrisonManagementConsole : BaseConsole
    {
        [Constructable]
        public PrisonManagementConsole()
            : base("Prison Management Console")
        {
        }

        public PrisonManagementConsole(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritDepotBandies
        {
            get => CoreAI.SpiritDepotBandies;
            set => CoreAI.SpiritDepotBandies = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritDepotGHPots
        {
            get => CoreAI.SpiritDepotGHPots;
            set => CoreAI.SpiritDepotGHPots = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritDepotReagents
        {
            get => CoreAI.SpiritDepotReagents;
            set => CoreAI.SpiritDepotReagents = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritDepotTRPots
        {
            get => CoreAI.SpiritDepotTRPots;
            set => CoreAI.SpiritDepotTRPots = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritFirstWaveVirtualArmor
        {
            get => CoreAI.SpiritFirstWaveVirtualArmor;
            set => CoreAI.SpiritFirstWaveVirtualArmor = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritSecondWaveVirtualArmor
        {
            get => CoreAI.SpiritSecondWaveVirtualArmor;
            set => CoreAI.SpiritSecondWaveVirtualArmor = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritThirdWaveVirtualArmor
        {
            get => CoreAI.SpiritThirdWaveVirtualArmor;
            set => CoreAI.SpiritThirdWaveVirtualArmor = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SpiritBossVirtualArmor
        {
            get => CoreAI.SpiritBossVirtualArmor;
            set => CoreAI.SpiritBossVirtualArmor = value;
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