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

using Server.Mobiles;
using System.Collections;

namespace Server.Factions
{
    public abstract class BaseFactionVendor : BaseVendor
    {
        private Town m_Town;
        private Faction m_Faction;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Town Town
        {
            get { return m_Town; }
            set { Unregister(); m_Town = value; Register(); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Faction Faction
        {
            get { return m_Faction; }
            set { Unregister(); m_Faction = value; Register(); }
        }

        public void Register()
        {
            if (m_Town != null && m_Faction != null)
                m_Town.RegisterVendor(this);
        }

        public void Unregister()
        {
            if (m_Town != null)
                m_Town.UnregisterVendor(this);
        }

        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override void InitSBInfo()
        {
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            Unregister();
        }

        public override bool CheckVendorAccess(Mobile from)
        {
            return true;
        }

        public BaseFactionVendor(Town town, Faction faction, string title) : base(title)
        {
            Frozen = true;
            CantWalkLand = true;
            Female = false;
            BodyValue = 400;
            Name = NameList.RandomName("male");

            RangeHome = 0;

            m_Town = town;
            m_Faction = faction;
            Register();
        }

        public BaseFactionVendor(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            Town.WriteReference(writer, m_Town);
            Faction.WriteReference(writer, m_Faction);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Town = Town.ReadReference(reader);
                        m_Faction = Faction.ReadReference(reader);
                        Register();
                        break;
                    }
            }

            Frozen = true;
        }
    }
}