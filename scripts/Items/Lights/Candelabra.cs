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

using System;

namespace Server.Items
{
    public class Candelabra : BaseLight, IShipwreckedItem
    {
        public override int LitItemID { get { return 0xB1D; } }
        public override int UnlitItemID { get { return 0xA27; } }

        [Constructable]
        public Candelabra()
            : base(0xA27)
        {
            Duration = TimeSpan.Zero; // Never burnt out
            Burning = false;
            Light = LightType.Circle225;
            Weight = 3.0;
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (m_IsShipwreckedItem)
                list.Add(1041645); // recovered from a shipwreck
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_IsShipwreckedItem && !UseOldNames)
                LabelTo(from, 1041645); // recovered from a shipwreck
        }

        public override string GetOldSuffix()
        {
            string suffix = base.GetOldSuffix();

            if (m_IsShipwreckedItem)
                suffix = string.Concat(suffix, " recovered from a shipwreck");

            return suffix;
        }

        public Candelabra(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1);

            writer.Write((bool)m_IsShipwreckedItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_IsShipwreckedItem = reader.ReadBool();
                        break;
                    }
            }
        }

        #region IShipwreckedItem

        private bool m_IsShipwreckedItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsShipwreckedItem
        {
            get { return m_IsShipwreckedItem; }
            set { m_IsShipwreckedItem = value; InvalidateProperties(); }
        }

        #endregion
    }
}