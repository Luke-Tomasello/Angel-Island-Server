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

using Server.Network;

namespace Server.Targeting
{
    public abstract class MultiTarget : Target
    {
        private int m_MultiID;
        private Point3D m_Offset;

        public int MultiID
        {
            get
            {
                return m_MultiID;
            }
            set
            {
                m_MultiID = value;
            }
        }

        public Point3D Offset
        {
            get
            {
                return m_Offset;
            }
            set
            {
                m_Offset = value;
            }
        }

        public MultiTarget(int multiID, Point3D offset)
            : this(multiID, offset, 10, true, TargetFlags.None)
        {
        }

        public MultiTarget(int multiID, Point3D offset, int range, bool allowGround, TargetFlags flags)
            : base(range, allowGround, flags)
        {
            m_MultiID = multiID;
            m_Offset = offset;
        }

        public override Packet GetPacket()
        {
            return new MultiTargetReq(this);
        }
    }
}