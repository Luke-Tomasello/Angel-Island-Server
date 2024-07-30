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

/* Scripts\Engines\ChampionSpawn\Champs\Seasonal\Fall\Items\MagicBox.cs
 * ChangeLog:
 *  10/1/07, Adam
 *      First time checkin
 */

namespace Server.Items
{

    [DynamicFliping]
    [TinkerTrapable]
    [Flipable(0x9A8, 0xE80)]
    public class MagicBox : LockableContainer
    {
        public override int DefaultGumpID { get { return 0x4B; } }
        public override int DefaultDropSound { get { return 0x42; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int MaxWeight { get { return m_MaxWeight; } }

        private int m_MaxWeight;

        [Constructable]
        public MagicBox()
            : base(0x9A8)
        {
            Name = "magic box";
            m_MaxWeight = Utility.RandomMinMax(900, 1024);
            MaxItems = Utility.RandomMinMax(900, 1024);
            Weight = 25.0;
        }

        public MagicBox(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write(m_MaxWeight);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_MaxWeight = reader.ReadInt();
        }
    }
}