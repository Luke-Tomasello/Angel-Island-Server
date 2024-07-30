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

/* Scripts\Engines\CTFSystem\Items\CTFFlag.cs
 * CHANGELOG:
 * 4/10/10, adam
 *		initial framework.
 */

using Server.Items;
using System;

namespace Server.Engines
{
    public class CTFFlag : BlackStaff
    {
        public override string OldName { get { return "ctf flag"; } }
        private CTFControl m_ctrl;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        private CTFControl CTFControl { get { return m_ctrl; } set { m_ctrl = value; } }

        [Constructable]
        public CTFFlag()
            : base()
        {
            Weight = 6.0;
            Movable = false;
            Name = "ctf flag";
        }

        public CTFFlag(Serial serial)
            : base(serial)
        {
        }

        // force damage to FlagHPDamage
        public override int OldDieRolls { get { return 0; } }
        public override int OldDieMax { get { return 0; } }
        public override int OldAddConstant { get { return m_ctrl == null ? 70 : (m_ctrl.FlagHPDamage); } }

        public void Setup(CTFControl ctrl)
        {
            m_ctrl = ctrl;
            Hue = ctrl.FlagColor;
        }


        public override bool HandlesOnMovement { get { return true; } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (m_ctrl != null && this.ItemID == 0xDF1)
            {
                Point3D hot = this.Location;
                hot.Y--;
                if (m.Location == hot)
                    m_ctrl.OnFlagMoveOver(m);
            }
            else if (m_ctrl != null && this.ItemID == 0xDF0)
            {
                Point3D hot = this.Location;
                hot.X--;
                if (m.Location == hot)
                    m_ctrl.OnFlagMoveOver(m);
            }

        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_ctrl != null)
                m_ctrl.OnFlagMoveOver(m);
            return true;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);
            if (parent is Mobile)
                (parent as Mobile).SpeedRunFoot = TimeSpan.FromSeconds(0.2);    // speed up
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);
            if (parent is Mobile)
                (parent as Mobile).SpeedRunFoot = TimeSpan.FromSeconds(0.3); // slow down
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            // version 1
            writer.Write(m_ctrl);
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
                        goto default;
                    }

                default:
                    break;
            }
        }
    }
}