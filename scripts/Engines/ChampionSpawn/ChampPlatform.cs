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

/* Scripts/Engines/ChampionSpawn/ChampPlatform.cs
 *	ChangeLog
 *	10/28/2006, plasma
 *		Initial creation 
 */

using Server.Items;


namespace Server.Engines.ChampionSpawn
{
    public class ChampPlatform : BaseAddon
    {
        private ChampEngine m_Spawn;
        private bool m_Quiet;

        public ChampPlatform(bool bQuiet, ChampEngine spawn)
        {
            m_Spawn = spawn;
            m_Quiet = bQuiet;

            for (int x = -2; x <= 2; ++x)
                for (int y = -2; y <= 2; ++y)
                    AddComponent(0x750, x, y, -5);

            for (int x = -1; x <= 1; ++x)
                for (int y = -1; y <= 1; ++y)
                    AddComponent(0x750, x, y, 0);

            for (int i = -1; i <= 1; ++i)
            {
                AddComponent(0x751, i, 2, 0);
                AddComponent(0x752, 2, i, 0);

                AddComponent(0x753, i, -2, 0);
                AddComponent(0x754, -2, i, 0);
            }

            AddComponent(0x759, -2, -2, 0);
            AddComponent(0x75A, 2, 2, 0);
            AddComponent(0x75B, -2, 2, 0);
            AddComponent(0x75C, 2, -2, 0);
        }
        public override Item Dupe(int amount)
        {
            ChampPlatform new_addon = new ChampPlatform(m_Quiet, m_Spawn);
            return base.Dupe(new_addon, amount);
        }
        public void AddComponent(int id, int x, int y, int z)
        {
            AddonComponent ac = new AddonComponent(id);

            ac.Hue = 0x497;
            ac.Visible = (m_Quiet == true) ? false : true;

            AddComponent(ac, x, y, z);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

        }

        public ChampPlatform(Serial serial)
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