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

/* Items/Spcial/Holiday/Winter/EternalEmbers.cs
 * CHANGELOG:
 * 01/21/06, Kit
 *		Added secure level check for co-owner/owner only to be able to toggle on/off.
 * 12/11/05, Kit
 *		Initial Creation
 */

using Server.Multis;

namespace Server.Items
{
    public class EternalEmbers : Item
    {
        private bool isLit;
        private SecureLevel m_Level;

        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_Level; }
            set { m_Level = value; }
        }

        [Constructable]
        public EternalEmbers()
            : base(0xDE1)
        {
            Stackable = false;
            Weight = 5.0;
            isLit = false;
            Name = "eternal embers";
            m_Level = SecureLevel.CoOwners;
        }

        public EternalEmbers(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((int)m_Level);

            writer.Write(isLit);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Level = (SecureLevel)reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        isLit = reader.ReadBool();
                        break;
                    }
            }

            if (version < 1)
            {
                m_Level = SecureLevel.CoOwners;
            }
        }

        public override void OnSectorActivate()
        {
            base.OnSectorActivate();

            if (isLit)
            {
                this.Light = LightType.Circle300;
            }
        }

        public override void OnSectorDeactivate()
        {
            base.OnSectorDeactivate();

            if (isLit)
            {
                this.Light = LightType.Empty;
            }
        }

        public override void OnAdded(object parent)
        {
            if (isLit)
            {
                isLit = false;
                this.ItemID = 0xDE1;
                this.Light = LightType.Empty;
            }

            base.OnAdded(parent);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            if (isLit)
            {
                isLit = false;
                this.ItemID = 0xDE1;
                this.Light = LightType.Empty;
            }

            base.OnMovement(m, oldLocation);
        }

        public bool CheckAccess(Mobile m)
        {
            BaseHouse house = BaseHouse.FindHouseAt(this);

            return (house != null && house.HasSecureAccess(m, m_Level));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!this.IsLockedDown)
            {
                from.SendMessage("this must be locked down to use");
                return;
            }

            if (!CheckAccess(from))
            {
                from.SendMessage("You cannot access this");
                return;
            }

            if (isLit)
            {
                this.ItemID = 0xDE1;
                this.Light = LightType.Empty;
                isLit = false;
            }
            else
            {
                this.ItemID = 0xDE3;
                this.Light = LightType.Circle300;

                isLit = true;
            }
        }
    }
}