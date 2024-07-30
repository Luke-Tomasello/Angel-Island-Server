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

namespace Server.Engines.Quests.Necro
{
    public class VaultOfSecretsBarrier : Item
    {
        [Constructable]
        public VaultOfSecretsBarrier()
            : base(0x49E)
        {
            Movable = false;
            Visible = false;
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m.AccessLevel > AccessLevel.Player)
                return true;

            PlayerMobile pm = m as PlayerMobile;

            if (pm != null && pm.Profession == 4)
            {
                m.SendLocalizedMessage(1060188, "", 0x24); // The wicked may not enter!
                return false;
            }

            return base.OnMoveOver(m);
        }

        public VaultOfSecretsBarrier(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}