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
using Server.Network;

namespace Server.Factions
{
    public class JoinStone : BaseSystemController
    {
        private Faction m_Faction;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Faction Faction
        {
            get { return m_Faction; }
            set
            {
                m_Faction = value;

                Hue = (m_Faction == null ? 0 : m_Faction.Definition.HueJoin);
                AssignName(m_Faction == null ? null : m_Faction.Definition.SignupName);
            }
        }

        public override string DefaultName { get { return "faction signup stone"; } }

        [Constructable]
        public JoinStone() : this(null)
        {
        }

        [Constructable]
        public JoinStone(Faction faction) : base(0xEDC)
        {
            Movable = false;
            Faction = faction;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Faction == null)
                return;

            if (NewGumps.FactionGumps.Enabled)
            {
                NewGumps.FactionGumps.OpenJoinGump(from, m_Faction);
                return;
            }

            if (!from.InRange(GetWorldLocation(), 2))
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            else if (FactionGump.Exists(from))
                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
            else if (Faction.Find(from) == null && from is PlayerMobile)
                from.SendGump(new JoinStoneGump((PlayerMobile)from, m_Faction));
        }

        public JoinStone(Serial serial) : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

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
                        Faction = Faction.ReadReference(reader);
                        break;
                    }
            }
        }
    }
}