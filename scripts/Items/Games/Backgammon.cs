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
    [Flipable(0xE1C, 0xFAD)]
    public class Backgammon : BaseGameBoard
    {
        public override int DefaultGumpID { get { return 0x92E; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(0, 0, 282, 230); }
        }

        [Constructable]
        public Backgammon()
            : base(0xE1C)
        {
        }

        public override void CreatePieces()
        {
            for (int i = 0; i < 5; i++)
            {
                CreatePiece(new PieceWhiteChecker(this), 42, (17 * i) + 6);
                CreatePiece(new PieceBlackChecker(this), 42, (17 * i) + 119);

                CreatePiece(new PieceBlackChecker(this), 142, (17 * i) + 6);
                CreatePiece(new PieceWhiteChecker(this), 142, (17 * i) + 119);
            }

            for (int i = 0; i < 3; i++)
            {
                CreatePiece(new PieceBlackChecker(this), 108, (17 * i) + 6);
                CreatePiece(new PieceWhiteChecker(this), 108, (17 * i) + 153);
            }

            for (int i = 0; i < 2; i++)
            {
                CreatePiece(new PieceWhiteChecker(this), 223, (17 * i) + 6);
                CreatePiece(new PieceBlackChecker(this), 223, (17 * i) + 170);
            }
        }

        public Backgammon(Serial serial)
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