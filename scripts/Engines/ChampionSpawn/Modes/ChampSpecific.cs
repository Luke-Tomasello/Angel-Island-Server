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

/* Scripts/Engines/ChampionSpawn/Modes/ChampSpecific.cs
 *	ChangeLog:
 *	10/28/2006, plasma
 *		Initial creation
 * 
 **/
using System;

namespace Server.Engines.ChampionSpawn
{
    public class ChampSpecific : ChampEngine
    {
        [Constructable]
        public ChampSpecific()
            : base()
        {
            // just switch on gfx
            //Graphics = true;
            SetBool(ChampGFX.Altar, true);

            // and restart timer for 5 mins
            m_bRestart = true;
            m_RestartDelay = TimeSpan.FromMinutes(5);
        }


        public ChampSpecific(Serial serial)
            : base(serial)
        {
        }

        // #region serialize

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0: break;
            }
        }
        // #endregion

        public override void OnSingleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                // this is a gm, allow normal text from base and champ indicator
                LabelTo(from, "Specific Champ");
                base.OnSingleClick(from);
            }
        }
    }
}