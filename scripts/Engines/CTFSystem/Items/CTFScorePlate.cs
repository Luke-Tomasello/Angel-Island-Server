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

/* Scripts\Engines\CTFSystem\Items\CTFScorePlate.cs
 * CHANGELOG:
 * 4/10/10, adam
 *		initial framework.
 */

namespace Server.Engines
{
    public class CTFScorePlate : Item
    {
        private CTFControl.Team m_team;
        private CTFControl m_ctrl;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public CTFControl.Team Team { get { return m_team; } set { m_team = value; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        private CTFControl CTFControl { get { return m_ctrl; } set { m_ctrl = value; } }

        [Constructable]
        public CTFScorePlate()
            : base(0x1BC3)
        {
            Movable = false;
            Visible = true;
        }

        public CTFScorePlate(Serial serial)
            : base(serial)
        {
        }

        public void Setup(CTFControl ctrl, CTFControl.Team team)
        {
            m_ctrl = ctrl;
            m_team = team;
            Hue = ctrl.TeamColor(team);
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_ctrl != null && m_ctrl.Deleted == false)
            {
                m_ctrl.OnPlateMoveOver(m, m_team);
            }
            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            // version 1
            writer.Write(m_ctrl);
            writer.Write((int)m_team);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_ctrl = reader.ReadItem() as CTFControl;
                        m_team = (CTFControl.Team)reader.ReadInt();
                        goto default;
                    }

                default:
                    break;
            }
        }
    }
}