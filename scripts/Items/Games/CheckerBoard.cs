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

/* Items/Games/Backgammon.cs
 * CHANGELOG:
 *	11/14/21, Yoar
 *      Renamed board game related BaseBoard to BaseGameBoard.
 */

namespace Server.Items
{
    public class CheckerBoard : BaseGameBoard
    {
        public override int LabelNumber { get { return 1016449; } } // a checker board

        public override int DefaultGumpID { get { return 0x91A; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(0, 0, 282, 210); }
        }

        [Constructable]
        public CheckerBoard()
            : base(0xFA6)
        {
        }

        public override void CreatePieces()
        {
            for (int i = 0; i < 4; i++)
            {
                CreatePiece(new PieceWhiteChecker(this), (50 * i) + 45, 25);
                CreatePiece(new PieceWhiteChecker(this), (50 * i) + 70, 50);
                CreatePiece(new PieceWhiteChecker(this), (50 * i) + 45, 75);
                CreatePiece(new PieceBlackChecker(this), (50 * i) + 70, 150);
                CreatePiece(new PieceBlackChecker(this), (50 * i) + 45, 175);
                CreatePiece(new PieceBlackChecker(this), (50 * i) + 70, 200);
            }
        }

        public CheckerBoard(Serial serial)
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