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

/* Scripts/Multis/Boats/Custom/Dinghy.cs
 * ChangeLog
 *  1/22/24, Yoar
 *      Initial version.
 */

namespace Server.Multis
{
    public class Dinghy : CustomBoat
    {
        #region Init Components

        static Dinghy()
        {
            MultiComponentList mcl;

            m_NorthComponents = mcl = new MultiComponentList(MultiComponentList.Empty);

            mcl.Center = new Point2D(1, 4);
            mcl.Resize(3, 8);

            mcl.Add(0x3E9B, -1, -3, 0);
            mcl.Add(0x3E9D, -1, -2, 0);
            mcl.Add(0x3E9F, -1, -1, 0);
            mcl.Add(0x3EA5, -1, 0, 0);
            mcl.Add(0x3EA7, -1, 1, 0);
            mcl.Add(0x3EB6, -1, 2, 0);
            mcl.Add(0x3E9A, 0, -4, 0);
            mcl.Add(0x3E98, 0, -3, 0);
            mcl.Add(0x3EAD, 0, -2, 0);
            mcl.Add(0x3EAC, 0, -1, 0);
            mcl.Add(0x3EAC, 0, 0, 0);
            mcl.Add(0x3EAC, 0, 1, 0);
            mcl.Add(0x3EAA, 0, 2, 0);
            mcl.Add(0x3EBC, 0, 3, 0);
            mcl.Add(0x3E9C, 1, -3, 0);
            mcl.Add(0x3E9E, 1, -2, 0);
            mcl.Add(0x3EA0, 1, -1, 0);
            mcl.Add(0x3EA6, 1, 0, 0);
            mcl.Add(0x3EA8, 1, 1, 0);
            mcl.Add(0x3EBD, 1, 2, 0);
            mcl.Add(0x3EAB, 1, 2, 0);

            m_EastComponents = mcl = new MultiComponentList(MultiComponentList.Empty);

            mcl.Center = new Point2D(4, 1);
            mcl.Resize(8, 3);

            mcl.Add(0x3E63, -4, 0, 0);
            mcl.Add(0x3E94, -3, -1, 0);
            mcl.Add(0x3E83, -3, 0, 0);
            mcl.Add(0x3E62, -3, 0, 0);
            mcl.Add(0x3E91, -3, 1, 0);
            mcl.Add(0x3E90, -2, -1, 0);
            mcl.Add(0x3E8F, -2, 0, 0);
            mcl.Add(0x3E8E, -2, 1, 0);
            mcl.Add(0x3E8D, -1, -1, 0);
            mcl.Add(0x3E8B, -1, 0, 0);
            mcl.Add(0x3E8C, -1, 1, 0);
            mcl.Add(0x3E7F, 0, -1, 0);
            mcl.Add(0x3E8B, 0, 0, 0);
            mcl.Add(0x3E7E, 0, 1, 0);
            mcl.Add(0x3E7D, 1, -1, 0);
            mcl.Add(0x3E8B, 1, 0, 0);
            mcl.Add(0x3E7C, 1, 1, 0);
            mcl.Add(0x3E67, 2, -1, 0);
            mcl.Add(0x3E83, 2, 0, 0);
            mcl.Add(0x3E66, 2, 1, 0);
            mcl.Add(0x3E69, 3, 0, 0);

            m_SouthComponents = mcl = new MultiComponentList(MultiComponentList.Empty);

            mcl.Center = new Point2D(1, 4);
            mcl.Resize(3, 8);

            mcl.Add(0x3E9B, -1, -3, 0);
            mcl.Add(0x3E9D, -1, -2, 0);
            mcl.Add(0x3E9F, -1, -1, 0);
            mcl.Add(0x3EA5, -1, 0, 0);
            mcl.Add(0x3EA7, -1, 1, 0);
            mcl.Add(0x3EB6, -1, 2, 0);
            mcl.Add(0x3EC4, 0, -4, 0);
            mcl.Add(0x3E98, 0, -3, 0);
            mcl.Add(0x3EC3, 0, -3, 0);
            mcl.Add(0x3EAD, 0, -2, 0);
            mcl.Add(0x3EAC, 0, -1, 0);
            mcl.Add(0x3EAC, 0, 0, 0);
            mcl.Add(0x3EAC, 0, 1, 0);
            mcl.Add(0x3E98, 0, 2, 0);
            mcl.Add(0x3EB4, 0, 3, 0);
            mcl.Add(0x3E9C, 1, -3, 0);
            mcl.Add(0x3E9E, 1, -2, 0);
            mcl.Add(0x3EA0, 1, -1, 0);
            mcl.Add(0x3EA6, 1, 0, 0);
            mcl.Add(0x3EA8, 1, 1, 0);
            mcl.Add(0x3EB5, 1, 2, 0);

            m_WestComponents = mcl = new MultiComponentList(MultiComponentList.Empty);

            mcl.Center = new Point2D(4, 1);
            mcl.Resize(8, 3);

            mcl.Add(0x3E95, -4, 0, 0);
            mcl.Add(0x3E94, -3, -1, 0);
            mcl.Add(0x3E83, -3, 0, 0);
            mcl.Add(0x3E91, -3, 1, 0);
            mcl.Add(0x3E90, -2, -1, 0);
            mcl.Add(0x3E8F, -2, 0, 0);
            mcl.Add(0x3E8E, -2, 1, 0);
            mcl.Add(0x3E8D, -1, -1, 0);
            mcl.Add(0x3E8B, -1, 0, 0);
            mcl.Add(0x3E8C, -1, 1, 0);
            mcl.Add(0x3E7F, 0, -1, 0);
            mcl.Add(0x3E8B, 0, 0, 0);
            mcl.Add(0x3E7E, 0, 1, 0);
            mcl.Add(0x3E7D, 1, -1, 0);
            mcl.Add(0x3E8B, 1, 0, 0);
            mcl.Add(0x3E7C, 1, 1, 0);
            mcl.Add(0x3E7B, 2, -1, 0);
            mcl.Add(0x3E7A, 2, 0, 0);
            mcl.Add(0x3E77, 2, 1, 0);
            mcl.Add(0x3E79, 2, 1, 0);
            mcl.Add(0x3E76, 3, 0, 0);
        }

        #endregion

        public override int Depth { get { return 20; } }

        public override int NorthID { get { return 0x13EC; } }
        public override int EastID { get { return 0x13EC; } }
        public override int SouthID { get { return 0x13EC; } }
        public override int WestID { get { return 0x13EC; } }

        public override int HoldDistance { get { return 0; } }
        public override int TillerManDistance { get { return 0; } }

        public override Point2D StarboardOffset { get { return new Point2D(1, 0); } }
        public override Point2D PortOffset { get { return new Point2D(-1, 0); } }

        public override Point3D MarkOffset { get { return new Point3D(0, 0, 3); } }

        public override BaseDockedBoat DockedBoat { get { return null; } }

        private static readonly MultiComponentList m_NorthComponents;
        private static readonly MultiComponentList m_EastComponents;
        private static readonly MultiComponentList m_SouthComponents;
        private static readonly MultiComponentList m_WestComponents;

        protected override MultiComponentList NorthComponents { get { return m_NorthComponents; } }
        protected override MultiComponentList EastComponents { get { return m_EastComponents; } }
        protected override MultiComponentList SouthComponents { get { return m_SouthComponents; } }
        protected override MultiComponentList WestComponents { get { return m_WestComponents; } }

        [Constructable]
        public Dinghy()
            : base()
        {
            RemoveBoatFixtures();
        }

        private void RemoveBoatFixtures()
        {
            if (TillerMan != null)
            {
                TillerMan.Boat = null;
                TillerMan.Delete();
            }

            if (Hold != null)
            {
                Hold.Boat = null;
                Hold.Delete();
            }

            if (PPlank != null)
            {
                PPlank.Boat = null;
                PPlank.Delete();
            }

            if (SPlank != null)
            {
                SPlank.Boat = null;
                SPlank.Delete();
            }
        }

        public Dinghy(Serial serial)
            : base(serial)
        {
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }
    }
}