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

/* Scripts\Engines\Alignment\AlignmentPersistence.cs
 * Changelog:
 *  4/14/23, Yoar
 *      Initial version.
 */

using Server.Items;
using System.Collections.Generic;

namespace Server.Engines.Alignment
{
    public class AlignmentPersistence : Item, IPersistence
    {
        private static AlignmentPersistence m_Instance;
        public Item GetInstance() { return m_Instance; }
        public static void EnsureExistence()
        {
            if (m_Instance == null)
            {
                m_Instance = new AlignmentPersistence();
                m_Instance.IsIntMapStorage = true;
            }
        }

        public override string DefaultName
        {
            get { return "Alignment Persistence - Internal"; }
        }

        [Constructable]
        public AlignmentPersistence()
            : base(0x1)
        {
            Movable = false;
        }

        public AlignmentPersistence(Serial serial)
            : base(serial)
        {
            m_Instance = this;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write((int)Alignment.Table.Length);

            for (int i = 0; i < Alignment.Table.Length; i++)
            {
                Alignment.WriteReference(writer, Alignment.Table[i]);
                Alignment.Table[i].State.Serialize(writer);
            }

            writer.Write((int)AlignmentPlayer.Table.Count);

            foreach (KeyValuePair<Mobile, AlignmentPlayer> kvp in AlignmentPlayer.Table)
            {
                writer.Write((Mobile)kvp.Key);
                kvp.Value.Serialize(writer);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        int stateCount = reader.ReadInt();

                        for (int i = 0; i < stateCount; i++)
                        {
                            Alignment alignment = Alignment.ReadReference(reader);
                            alignment.State.Deserialize(reader);
                        }

                        int playerCount = reader.ReadInt();

                        for (int i = 0; i < playerCount; i++)
                        {
                            Mobile m = reader.ReadMobile();

                            AlignmentPlayer player = new AlignmentPlayer(m);
                            player.Deserialize(reader);

                            if (m != null && !player.IsEmpty())
                                AlignmentPlayer.Table[m] = player;
                        }

                        break;
                    }
            }
        }
    }
}