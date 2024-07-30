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

/* Scripts/Items/Addons/TeleporterPairAddon.cs
 * ChangeLog
 *	10/05/06: Pix
 *		Removed new teleporter() call.
 *	9/28/06: Pix
 *		Sparkles are now created as non-movable. d'oh!
 *	9/28/06: Pix
 *		Now will teleport pets too.
 *	9/19/06: Pix
 *		Fixed a bug with placement (switched back to using Point3D instead of IPoint3D).
 *		Made teleporters separately dyable.
 *	9/14/06: Pix
 *		Added double-click to turn sparkles on and off.  Defaulted hue to normal.
 *	9/13/06: Pix
 *		Added CheckLOS=false to second target cursor
 *	9/13/06: Pix
 *		Initial Version.
 */

using Server.Multis;
using Server.Targeting;
using System;

namespace Server.Items
{
    //add addon components
    /*
# [add static 6174 on the spot the player requests
# [set hue 902 on that new tile you just placed
# [add teleporter on top of the tile.
# [add static 14201 on top of the teleporter.
# In the teleporter props, map destination needs to be set to Felucca, and 
		point destination needs to be set to the partner teleporter.
			 */

    public class TeleporterPairAddon : BaseAddon
    {
        public override BaseAddonDeed Deed { get { return new TeleporterAddonDeed(); } }

        [Constructable]
        public TeleporterPairAddon(IPoint3D first, IPoint3D second, Server.Map map)
        {
            //Set up all the addon components... FirstLocation will be the 0,0,0 reference point
            TeleporterAC tele1 = new TeleporterAC(second, map);
            tele1.Active = true;
            TeleporterAC tele2 = new TeleporterAC(first, map);
            tele2.Active = true;
            //AddonComponent spark1 = new AddonComponent(14201);
            //AddonComponent spark2 = new AddonComponent(14201);


            //First Teleporter
            AddComponent(tele1, 0, 0, 0);
            //AddComponent( spark1, 0, 0, 0 );
            //Second Teleporter
            AddComponent(tele2, second.X - first.X, second.Y - first.Y, second.Z - first.Z);
            //AddComponent( spark2, second.X-first.X, second.Y-first.Y, second.Z-first.Z );
        }

        public override bool ShareHue
        {
            get
            {
                return false;
            }
        }


        public TeleporterPairAddon(Serial serial)
            : base(serial)
        {
        }

        public void AddTeleporterPiece(AddonComponent component, int x, int y, int z)
        {
            AddComponent(component, x, y, z);
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

    public class TeleporterAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                if (FirstLocation == Point3D.Zero || SecondLocation == Point3D.Zero)
                {
                    return null;
                }
                TeleporterPairAddon tpa = new TeleporterPairAddon(FirstLocation, SecondLocation, m_Map);

                return tpa;
            }
        }

        public Point3D FirstLocation = Point3D.Zero;
        public Point3D SecondLocation = Point3D.Zero;
        public Map m_Map;

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D first { get { return FirstLocation; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D second { get { return SecondLocation; } }

        [Constructable]
        public TeleporterAddonDeed()
        {
            Name = "teleporter addon";
        }

        public TeleporterAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public void ConfirmPlacement(Mobile from)
        {
            this.Place(FirstLocation, from.Map, from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                from.SendMessage("Target where you want the first teleporter to be.");
                from.Target = new FirstTarget(this);
            }
            else
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
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

        #region Targetting Classes

        private class FirstTarget : Target
        {
            private TeleporterAddonDeed m_Deed;

            public FirstTarget(TeleporterAddonDeed deed)
                : base(-1, true, TargetFlags.None)
            {
                m_Deed = deed;
                this.CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Point3D targettedPoint = new Point3D(targeted as IPoint3D);
                if (targettedPoint == Point3D.Zero)
                {
                    from.SendMessage("That is an invalid target.");
                    return;
                }

                m_Deed.FirstLocation = targettedPoint;
                m_Deed.m_Map = from.Map;

                if (m_Deed.IsChildOf(from.Backpack))
                {
                    from.SendMessage("Target where you want the second teleporter to be.");
                    from.Target = new SecondTarget(m_Deed);
                }
                else
                {
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                }
                return;
            }
        }

        private class SecondTarget : Target
        {
            private TeleporterAddonDeed m_Deed;

            public SecondTarget(TeleporterAddonDeed deed)
                : base(-1, true, TargetFlags.None)
            {
                m_Deed = deed;
                this.CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                Point3D targettedPoint = new Point3D(targeted as IPoint3D);
                if (targettedPoint == Point3D.Zero)
                {
                    from.SendMessage("That is an invalid target.");
                    return;
                }

                m_Deed.SecondLocation = targettedPoint;
                m_Deed.m_Map = from.Map;

                BaseHouse h1 = Server.Multis.BaseHouse.FindHouseAt(m_Deed.FirstLocation, from.Map, 2);
                BaseHouse h2 = Server.Multis.BaseHouse.FindHouseAt(m_Deed.SecondLocation, from.Map, 2);
                if (h1 == null || h2 == null || h1 != h2)
                {
                    from.SendMessage("The two targets are not in the same house.");
                    return;
                }

                m_Deed.ConfirmPlacement(from);
                return;
            }
        }

        #endregion
    }

    #region Teleporter AddonComponent

    /*
		 * This is a simplified teleporter since it needs to be based off AddonComponent
		 * It doesn't have much of the spiffy features of regular teleporters... it just
		 * teleports anything that steps on it.
		 * 
		 */
    public class TeleporterAC : AddonComponent
    {
        private bool m_Active;
        private Point3D m_PointDest;
        private Map m_MapDest;

        private bool m_SparkleOn;
        private Item m_Sparkle;

        public TeleporterAC()
            : base(6174)
        {
            Name = "teleporter";
            Movable = false;
            //Hue = 902;
            m_Active = false;
        }

        public TeleporterAC(IPoint3D location, Map map)
            : this()
        {
            m_PointDest = new Point3D(location);
            m_MapDest = map;
        }

        public TeleporterAC(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write(m_SparkleOn);
            writer.Write(m_Sparkle);

            writer.Write(m_Active);
            writer.Write(m_PointDest);
            writer.Write(m_MapDest);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_SparkleOn = reader.ReadBool();
                        m_Sparkle = reader.ReadItem();

                        if (m_Sparkle == null) //safety-checking
                        {
                            m_SparkleOn = false;
                        }
                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        m_PointDest = reader.ReadPoint3D();
                        m_MapDest = reader.ReadMap();

                        break;
                    }
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            if (m_Active)
            {
                if (!m.Deleted)
                {
                    Server.Mobiles.BaseCreature.TeleportPets(m, m_PointDest, m_MapDest);
                    m.MoveToWorld(m_PointDest, m_MapDest);
                    return false;
                }
            }

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            BaseHouse house = Server.Multis.BaseHouse.FindHouseAt(this);

            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                ToggleSparkle(from);
            }
            else if (house == null)
            {
                from.SendMessage("The house seems to not exist.");
            }
            else
            {
                if (house.IsOwner(from))
                {
                    ToggleSparkle(from);
                }
                else
                {
                    from.SendMessage("You cannot change this.");
                }
            }
        }

        private void ToggleSparkle(Mobile from)
        {
            try
            {
                if (m_SparkleOn)
                {
                    //delete sparkle
                    if (m_Sparkle != null)
                    {
                        m_Sparkle.Delete();
                        m_Sparkle = null;
                    }
                    //turn sparkle off
                    m_SparkleOn = false;
                }
                else
                {
                    //add sparkle
                    if (m_Sparkle != null)
                    {
                        m_Sparkle.Delete();
                        m_Sparkle = null;
                    }
                    if (m_Sparkle == null)
                    {
                        m_Sparkle = new Item(14201);
                        m_Sparkle.Movable = false;
                        m_Sparkle.MoveToWorld(this.Location, this.Map);
                    }
                    //turn sparkle 
                    m_SparkleOn = true;
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public override void OnDelete()
        {
            try
            {
                if (m_Sparkle != null)
                {
                    m_Sparkle.Delete();
                    m_Sparkle = null;
                }
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
            base.OnDelete();
        }


        [CommandProperty(AccessLevel.GameMaster)]
        public bool SparkleOn
        {
            //NOTE: set shouldn't be implemented - we need the code to delete the sparkle for us!
            get { return m_SparkleOn; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get
            {
                return m_Active;
            }
            set
            {
                m_Active = value;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Point3D PointDest
        {
            get { return m_PointDest; }
            set { m_PointDest = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Map MapDest
        {
            get { return m_MapDest; }
            set { m_MapDest = value; InvalidateProperties(); }
        }

    }
    #endregion

}