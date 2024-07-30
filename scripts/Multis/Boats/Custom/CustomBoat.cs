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

/* Scripts/Multis/Boats/Custom/CustomBoat.cs
 * ChangeLog
 *  1/22/24, Yoar
 *      Initial version.
 *      These boats can take on a design state.
 */

using Server.Network;

namespace Server.Multis
{
    public abstract class CustomBoat : BaseBoat, IDesignState
    {
        protected abstract MultiComponentList NorthComponents { get; }
        protected abstract MultiComponentList EastComponents { get; }
        protected abstract MultiComponentList SouthComponents { get; }
        protected abstract MultiComponentList WestComponents { get; }

        private DesignState m_NorthState;
        private DesignState m_EastState;
        private DesignState m_SouthState;
        private DesignState m_WestState;

        public override MultiComponentList Components
        {
            get { return GetComponents(Facing); }
        }

        public override MultiComponentList GetComponents(Direction facing)
        {
            if (m_NorthState == null)
                UpdateDesign();

            switch (facing)
            {
                default:
                case Direction.North: return NorthComponents;
                case Direction.East: return EastComponents;
                case Direction.South: return SouthComponents;
                case Direction.West: return WestComponents;
            }
        }

        protected void UpdateDesign()
        {
            m_NorthState = new DesignState(this, NorthComponents);
            m_NorthState.OnRevised();

            m_EastState = new DesignState(this, EastComponents);
            m_EastState.OnRevised();

            m_SouthState = new DesignState(this, SouthComponents);
            m_SouthState.OnRevised();

            m_WestState = new DesignState(this, WestComponents);
            m_WestState.OnRevised();

            Delta(ItemDelta.Update);
        }

        public override int GetMaxUpdateRange()
        {
            return 24;
        }

        public override int GetUpdateRange(Mobile m)
        {
            return HouseFoundation.GetDefaultUpdateRange(Components);
        }

        public override void SendInfoTo(NetState state, bool sendOplPacket)
        {
            base.SendInfoTo(state, sendOplPacket);

            SendDesignGeneral(state);
        }

        public CustomBoat()
            : base()
        {
        }

        public override void OnAfterSetFacing(Direction oldFacing)
        {
            if (ItemID == GetItemID(oldFacing))
            {
                Delta(ItemDelta.Update);
                ProcessDelta();
            }
        }

        public CustomBoat(Serial serial)
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

        #region IDesignState

        private int m_LastRevision;

        public int LastRevision
        {
            get { return m_LastRevision; }
            set { m_LastRevision = value; }
        }

        public virtual void SendDesignGeneral(NetState state)
        {
            GetDesignState().SendGeneralInfoTo(state);
        }

        public virtual void SendDesignDetails(NetState state)
        {
            GetDesignState().SendDetailedInfoTo(state);
        }

        private DesignState GetDesignState()
        {
            switch (Facing)
            {
                default:
                case Direction.North: return m_NorthState;
                case Direction.East: return m_EastState;
                case Direction.South: return m_SouthState;
                case Direction.West: return m_WestState;
            }
        }

        #endregion
    }
}