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

/* Scripts\Engines\Invasion\InvasionPersistence.cs
 * Changelog:
 *  10/7/23, Yoar
 *      Initial version.
 */

using Server.Items;
using System.Collections.Generic;

namespace Server.Engines.Invasion
{
    public class InvasionPersistence : Item, IPersistence
    {
        private static InvasionPersistence m_Instance;
        public Item GetInstance() { return m_Instance; }
        public static void EnsureExistence()
        {
            if (m_Instance == null)
            {
                m_Instance = new InvasionPersistence();
                m_Instance.IsIntMapStorage = true;
            }
        }

        public override string DefaultName
        {
            get { return "Invasion Persistence - Internal"; }
        }

        [Constructable]
        public InvasionPersistence()
            : base(0x1)
        {
            Movable = false;
        }

        public InvasionPersistence(Serial serial)
            : base(serial)
        {
            m_Instance = this;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)InvasionSystem.Registry.Count);

            foreach (KeyValuePair<InvasionType, InvasionSystem> kvp in InvasionSystem.Registry)
            {
                writer.Write((int)kvp.Key);
                kvp.Value.State.Serialize(writer);
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            int count = reader.ReadInt();

            for (int i = 0; i < count; i++)
            {
                // TODO: Manage InvasionSystem removal
                InvasionType type = (InvasionType)reader.ReadInt();
                InvasionSystem.Registry[type].State.Deserialize(reader);
            }
        }
    }
}