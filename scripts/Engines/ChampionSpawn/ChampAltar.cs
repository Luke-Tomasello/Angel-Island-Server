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

/* Scripts/Engines/ChampionSpawn/ChampAltar.cs
 * ChangeLog
 *	10/28/2006, plasma
 *		Initial creation
 */

using Server.Items;

namespace Server.Engines.ChampionSpawn
{

    public class ChampAltar : PentagramAddon
    {
        private ChampEngine m_Spawn;
        private bool m_Quiet;

        public ChampAltar(bool bQuiet, ChampEngine spawn)
            : base(bQuiet)
        {
            m_Spawn = spawn;
            m_Quiet = bQuiet;
            Visible = true;
        }
        public override Item Dupe(int amount)
        {
            ChampAltar new_addon = new ChampAltar(m_Quiet, m_Spawn);
            return base.Dupe(new_addon, amount);
        }
        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
        }

        public ChampAltar(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_Spawn);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Spawn = reader.ReadItem() as ChampEngine;

                        if (m_Spawn == null)
                            Delete();

                        break;
                    }
            }
        }
    }
}