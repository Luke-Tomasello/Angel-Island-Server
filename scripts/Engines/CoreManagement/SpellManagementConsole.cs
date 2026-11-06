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

using Server;
using Server.Engines.Consoles;

namespace Scripts.Engines.CoreManagement
{
    public class SpellManagementConsole : BaseConsole
    {
        [Constructable]
        public SpellManagementConsole()
            : base("Spell Management Console")
        {
        }

        public SpellManagementConsole(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            // GMs on TC, else the owner anywhere
            if ((Core.GMNTC_CFG && from.AccessLevel >= AccessLevel.GameMaster) || from.AccessLevel >= AccessLevel.Owner)
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            else
            {
                from.SendMessage("You cannot use this here.");
                from.SendMessage("Deleting...");
                Delete();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellDefaultDelay
        {
            get => CoreAI.SpellDefaultDelay;
            set => CoreAI.SpellDefaultDelay = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellAddlCastDelayCure
        {
            get => CoreAI.SpellAddlCastDelayCure;
            set => CoreAI.SpellAddlCastDelayCure = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellHealRecoveryOverride
        {
            get => CoreAI.SpellHealRecoveryOverride;
            set => CoreAI.SpellHealRecoveryOverride = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellCastDelayConstant
        {
            get => CoreAI.SpellCastDelayConstant;
            set => CoreAI.SpellCastDelayConstant = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellCastDelayCoeff
        {
            get => CoreAI.SpellCastDelayCoeff;
            set => CoreAI.SpellCastDelayCoeff = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool SpellUseRuoRecoveries
        {
            get => CoreAI.SpellUseRuoRecoveries;
            set => CoreAI.SpellUseRuoRecoveries = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellRuoRecoveryBase
        {
            get => CoreAI.SpellRuoRecoveryBase;
            set => CoreAI.SpellRuoRecoveryBase = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellRecoveryMin
        {
            get => CoreAI.SpellRecoveryMin;
            set => CoreAI.SpellRecoveryMin = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellFireballDmgDelay
        {
            get => CoreAI.SpellFireballDmgDelay;
            set => CoreAI.SpellFireballDmgDelay = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellFsDmgDelay
        {
            get => CoreAI.SpellFsDmgDelay;
            set => CoreAI.SpellFsDmgDelay = value;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellLightningDmgDelay
        {
            get => CoreAI.SpellLightningDmgDelay;
            set => CoreAI.SpellLightningDmgDelay = value;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt(); // version
        }
    }
}