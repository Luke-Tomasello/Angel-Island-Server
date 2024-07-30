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
    public class FactionStone : BaseSystemController
    {
        private Faction m_Faction;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public Faction Faction
        {
            get { return m_Faction; }
            set
            {
                m_Faction = value;

                AssignName(m_Faction == null ? null : m_Faction.Definition.FactionStoneName);
            }
        }

        public override string DefaultName { get { return "faction stone"; } }

        [Constructable]
        public FactionStone() : this(null)
        {
        }

        [Constructable]
        public FactionStone(Faction faction) : base(0xEDC)
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
                NewGumps.FactionGumps.OpenFactionGump(from, m_Faction);
                return;
            }

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
            }
            else if (FactionGump.Exists(from))
            {
                from.SendLocalizedMessage(1042160); // You already have a faction menu open.
            }
            else if (from is PlayerMobile)
            {
                Faction existingFaction = Faction.Find(from);

                if (existingFaction == m_Faction || from.AccessLevel >= AccessLevel.GameMaster)
                {
                    PlayerState pl = PlayerState.Find(from);

                    if (pl != null && pl.IsLeaving)
                        from.SendLocalizedMessage(1005051); // You cannot use the faction stone until you have finished quitting your current faction
                    else
                        from.SendGump(new FactionStoneGump((PlayerMobile)from, m_Faction));
                }
                else if (existingFaction != null)
                {
                    // TODO: Validate
                    from.SendLocalizedMessage(1005053); // This is not your faction stone!
                }
                else
                {
                    from.SendGump(new JoinStoneGump((PlayerMobile)from, m_Faction));
                }
            }
        }

        public FactionStone(Serial serial) : base(serial)
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