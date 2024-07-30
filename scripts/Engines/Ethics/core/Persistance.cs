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

/* Scripts\Engines\Ethics\Core\Persistence.cs
 * ChangeLog:
 * 8/22/22, Adam
 * This 'item' was constantly being tagged by the IntMapItemCleanup code and deleted.
 *  We will mark it is 'safe' from cleanup in IntMapItemCleanup code typeof(EthicsPersistence)
 */
using Server.Items;

namespace Server.Ethics
{
    [Aliases("Server.Ethics.EthicsPersistance")]
    public class EthicsPersistence : Item, IPersistence
    {
        private static EthicsPersistence m_Instance;
        public Item GetInstance() { return m_Instance; }
        public static EthicsPersistence Instance { get { return m_Instance; } }

        public override string DefaultName
        {
            get { return "Ethics Persistence - Internal"; }
        }

        [Constructable]
        public EthicsPersistence()
            : base(1)
        {
            Movable = false;

            if (m_Instance == null || m_Instance.Deleted)
                m_Instance = this;
            else
                base.Delete();
        }

        public EthicsPersistence(Serial serial)
            : base(serial)
        {
            m_Instance = this;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            for (int i = 0; i < Ethics.Ethic.Ethics.Length; ++i)
                Ethics.Ethic.Ethics[i].Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        for (int i = 0; i < Ethics.Ethic.Ethics.Length; ++i)
                            Ethics.Ethic.Ethics[i].Deserialize(reader);

                        break;
                    }
            }
        }

        public override void Delete()
        {
        }
    }
}