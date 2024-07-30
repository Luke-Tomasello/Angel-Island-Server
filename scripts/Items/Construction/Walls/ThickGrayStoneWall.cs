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

/****************************************
 * NAME    : Thick Gray Stone Wall      *
 * SCRIPT  : ThickGrayStoneWall.cs      *
 * VERSION : v1.00                      *
 * CREATOR : Mans Sjoberg (Allmight)    *
 * CREATED : 10-07.2002                 *
 * **************************************/

namespace Server.Items
{
    public enum ThickGrayStoneWallTypes
    {
        WestArch,
        NorthArch,
        SouthArchTop,
        EastArchTop,
        EastArch,
        SouthArch,
        Wall1,
        Wall2,
        Wall3,
        SouthWindow,
        Wall4,
        EastWindow,
        WestArch2,
        NorthArch2,
        SouthArchTop2,
        EastArchTop2,
        EastArch2,
        SouthArch2,
        SWArchEdge2,
        SouthWindow2,
        NEArchEdge2,
        EastWindow2
    }

    public class ThickGrayStoneWall : BaseWall
    {
        [Constructable]
        public ThickGrayStoneWall(ThickGrayStoneWallTypes type)
            : base(0x007A + (int)type)
        {
        }

        public ThickGrayStoneWall(Serial serial)
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