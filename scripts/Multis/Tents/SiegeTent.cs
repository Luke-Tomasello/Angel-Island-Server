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

/* Scripts/Multis/Tent/SiegeTent.cs
 * ChangeLog:
 *  9/3/2024, Adam
 *      Issue the following warning when a tent is placed:
 *      "Warning: Party members will be able to access your tent backpack."
 *	08/14/06, weaver
 *		Modified component construction to pass BaseHouse reference to the backpack.
 *	05/22/06, weaver
 *		Added initial 24 hour decay time.
 *		Added overrides to disable refreshing.
 *		Set default price to 0
 *	05/18/06, weaver
 *		Initial creation. 
 */

using Server.Items;
using Server.Multis.Deeds;
using System;

namespace Server.Multis
{

    public class SiegeTent : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-3, -3, 7, 7), new Rectangle2D(-1, 4, 3, 1) };
        public override Rectangle2D[] Area { get { return AreaArray; } }
        public override int DefaultPrice { get { return 0; } }
        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.TwoStoryFoundations[0]; } }

        private TentBedRoll m_TentBed;
        private TentBackpack m_TentPack;

        public TentBackpack TentPack
        {
            get
            {
                return m_TentPack;
            }
        }

        public TentBedRoll TentBed
        {
            get
            {
                return m_TentBed;
            }
        }

        private int m_RoofHue;
        [Constructable]
        public SiegeTent(Mobile owner, int Hue)
            : base(0xFFE, owner, 270, 2, 2)
        {
            m_RoofHue = Hue;

            // wea: this gets called after the base class, overriding it 
            DecayMinutesStored = 60 * 24;   // 24 hours!!

            BanLocation = new Point3D(2, 4, 0);
        }

        [Constructable]
        public SiegeTent()
            : this(World.GetAdminAcct(), 877)
        {

        }

        public SiegeTent(Serial serial)
            : base(serial)
        {
        }

        public void GenerateTent()
        {
            TentWalls walls = new TentWalls(TentStyle.Siege);
            TentRoof roof = new TentRoof(m_RoofHue);
            TentFloor floor = new TentFloor();

            walls.MoveToWorld(this.Location, this.Map);
            roof.MoveToWorld(this.Location, this.Map);
            floor.MoveToWorld(this.Location, this.Map);

            Addons.Add(walls);
            Addons.Add(roof);
            Addons.Add(floor);

            // Create tent bed
            m_TentBed = new TentBedRoll(this);
            m_TentBed.MoveToWorld(new Point3D(this.X, this.Y + 1, this.Z), this.Map);
            m_TentBed.Movable = false;

            // Create secure tent pack within the tent
            m_TentPack = new TentBackpack(this);
            m_TentPack.MoveToWorld(new Point3D(this.X - 1, this.Y - 1, this.Z), this.Map);
            SecureInfo info = new SecureInfo((Container)m_TentPack, SecureLevel.Anyone);
            m_TentPack.IsSecure = true;
            this.Secures.Add(info);
            m_TentPack.Movable = false;
            m_TentPack.Hue = m_RoofHue;

            if (this.Owner != null)
                Timer.DelayCall(TimeSpan.FromSeconds(1.5), new TimerStateCallback(WarnTick), new object[] { this.Owner, null });
        }
        private void WarnTick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is Mobile)
            {
                Mobile from = (Mobile)aState[0];
                from.SendMessage(0x22, "Warning: Party members will be able to access your tent backpack.");
            }
        }

        public override void MoveToWorld(Point3D location, Map map, Mobile responsible = null)
        {
            base.MoveToWorld(location, map);
            GenerateTent();
        }

        public override void OnDelete()
        {
            if (m_TentBed != null)
                m_TentBed.Delete();
            if (m_TentPack != null)
                m_TentPack.Delete();
            base.OnDelete();
        }

        public override HouseDeed GetDeed()
        {
            return new SiegeTentBag();
        }

        // Override standard decay handling so no refresh takes place

        public override void Refresh()
        {
            return;
        }

        public override void RefreshHouseOneDay()
        {
            return;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version

            writer.Write(m_TentBed);
            writer.Write(m_TentPack);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_TentBed = (TentBedRoll)reader.ReadItem();
            m_TentPack = (TentBackpack)reader.ReadItem();

            if (version == 0)
                BanLocation = new Point3D(2, 4, 0);
        }
    }
}