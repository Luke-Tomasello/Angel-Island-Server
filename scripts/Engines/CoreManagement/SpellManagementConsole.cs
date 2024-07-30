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

using Server;
using Server.Items;

namespace Scripts.Engines.CoreManagement
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class SpellManagementConsole : Item
    {
        [Constructable]
        public SpellManagementConsole()
            : base(0x1F14)
        {
            Weight = 1.0;
            Hue = 68;
            Name = "Spell Management Console";

        }

        public SpellManagementConsole(Serial serial)
            : base(serial)
        {
        }
        public override void OnDoubleClick(Mobile from)
        {
            // GMs on TC, else the owner anywhere
            if ((Core.UOTC_CFG && from.AccessLevel >= AccessLevel.GameMaster) || from.AccessLevel >= AccessLevel.Owner)
            {
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
            else
            {
                from.SendMessage("You cannot use this here.");
                from.SendMessage("Deleting...");
                this.Delete();
                return;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellDefaultDelay
        {
            get
            {
                return CoreAI.SpellDefaultDelay;
            }

            set
            {
                //if (Core.UOTC)
                CoreAI.SpellDefaultDelay = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellAddlCastDelayCure
        {
            get
            {
                return CoreAI.SpellAddlCastDelayCure;
            }

            set
            {
                CoreAI.SpellAddlCastDelayCure = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellHealRecoveryOverride
        {
            get
            {
                return CoreAI.SpellHealRecoveryOverride;
            }

            set
            {
                CoreAI.SpellHealRecoveryOverride = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellCastDelayConstant
        {
            get
            {
                return CoreAI.SpellCastDelayConstant;
            }

            set
            {
                CoreAI.SpellCastDelayConstant = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellCastDelayCoeff
        {
            get
            {
                return CoreAI.SpellCastDelayCoeff;
            }

            set
            {
                CoreAI.SpellCastDelayCoeff = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static bool SpellUseRuoRecoveries
        {
            get
            {
                return CoreAI.SpellUseRuoRecoveries;
            }

            set
            {
                CoreAI.SpellUseRuoRecoveries = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellRuoRecoveryBase
        {
            get
            {
                return CoreAI.SpellRuoRecoveryBase;
            }

            set
            {
                CoreAI.SpellRuoRecoveryBase = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellRecoveryMin
        {
            get
            {
                return CoreAI.SpellRecoveryMin;
            }

            set
            {
                CoreAI.SpellRecoveryMin = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellFireballDmgDelay
        {
            get
            {
                return CoreAI.SpellFireballDmgDelay;
            }

            set
            {
                CoreAI.SpellFireballDmgDelay = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellFsDmgDelay
        {
            get
            {
                return CoreAI.SpellFsDmgDelay;
            }

            set
            {
                CoreAI.SpellFsDmgDelay = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public static double SpellLightningDmgDelay
        {
            get
            {
                return CoreAI.SpellLightningDmgDelay;
            }

            set
            {
                CoreAI.SpellLightningDmgDelay = value;
            }
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

        /*public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Administrator)
            {
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
            else
            {
                from.SendMessage("You are not authorized to use this console.");
            }
        }*/
    }
}