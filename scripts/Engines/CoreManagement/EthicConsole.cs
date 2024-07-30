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

/* scripts\Engines\CoreManagement\EthicConsole.cs
 * CHANGELOG:
 *  1/10/23, Yoar
 *      Initial commit.
 */

using Server.Items;

namespace Server.Ethics
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class EthicConsole : Item
    {
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxLifeForce
        {
            get { return EthicConfig.MaxLifeForce; }
            set { EthicConfig.MaxLifeForce = value; }
        }

        [Constructable]
        public EthicConsole()
            : base(0x1F14)
        {
            Hue = 0x482;
            Name = "Ethic Settings Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public EthicConsole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }
    }
}