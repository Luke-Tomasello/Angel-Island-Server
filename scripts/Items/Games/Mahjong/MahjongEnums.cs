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

namespace Server.Engines.Mahjong
{
    public enum MahjongPieceDirection
    {
        Up,
        Left,
        Down,
        Right
    }

    public enum MahjongWind
    {
        North,
        East,
        South,
        West
    }

    public enum MahjongTileType
    {
        Dagger1 = 1,
        Dagger2,
        Dagger3,
        Dagger4,
        Dagger5,
        Dagger6,
        Dagger7,
        Dagger8,
        Dagger9,
        Gem1,
        Gem2,
        Gem3,
        Gem4,
        Gem5,
        Gem6,
        Gem7,
        Gem8,
        Gem9,
        Number1,
        Number2,
        Number3,
        Number4,
        Number5,
        Number6,
        Number7,
        Number8,
        Number9,
        North,
        East,
        South,
        West,
        Green,
        Red,
        White
    }
}