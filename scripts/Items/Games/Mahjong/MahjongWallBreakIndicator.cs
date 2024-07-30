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
    public class MahjongWallBreakIndicator
    {
        public static MahjongPieceDim GetDimensions(Point2D position)
        {
            return new MahjongPieceDim(position, 20, 20);
        }

        private MahjongGame m_Game;
        private Point2D m_Position;

        public MahjongGame Game { get { return m_Game; } }
        public Point2D Position { get { return m_Position; } }

        public MahjongWallBreakIndicator(MahjongGame game, Point2D position)
        {
            m_Game = game;
            m_Position = position;
        }

        public MahjongPieceDim Dimensions
        {
            get { return GetDimensions(m_Position); }
        }

        public void Move(Point2D position)
        {
            MahjongPieceDim dim = GetDimensions(position);

            if (!dim.IsValid())
                return;

            m_Position = position;

            m_Game.Players.SendGeneralPacket(true, true);
        }

        public void Save(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write(m_Position);
        }

        public MahjongWallBreakIndicator(MahjongGame game, GenericReader reader)
        {
            m_Game = game;

            int version = reader.ReadInt();

            m_Position = reader.ReadPoint2D();
        }
    }
}