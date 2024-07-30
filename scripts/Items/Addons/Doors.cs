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

/* Scripts/Items/Addons/Doors.cs
 * CHANGELOG
 *  6/2/22, Yoar
 *      Refactored slightly
 *      Removed house ref from BaseHouseDoorAddon
 *      Added additional null checks
 *	12/09/08, plasma
 *		Fix DarkWoodGateHouseDoorDeed as it was returning DarkWoodHouseDoorDeed on cancel
 *	10/16/07, plasma
 *		Uncomment ctor for original SecretStone (whoops)
 *  9/29/07, plasma
*      Added DarkWoodGateHouseDoorDeed, LightWoodHouseDoorDeed, LightWoodGateHouseDoorDeed, 
*      StrongWoodHouseDoorDeed, SmallIronGateHouseDoorDeed, SecretLightStoneHouseDoorDeed,
*      SecretLightWoodHouseDoorDeed, SecretDarkWoodHouseDoorDeed and RattanHouseDoorDeed
*      and their relevant addon classes
 *  3/7/07, Adam
 *      - remove OnChop and rely on the bass class implementation
 *      - add override for new baseclass method Chop()
 *  3/6/07, Adam
 *      Push IChopable down to BaseHouseDoor (remove from BaseHouseDoorComponent)
 *	2/27/07, Pix
 *		Added CellHouseDoorAddon and CellHouseDoorDeed
 *	12/19/06, Pix
 *		Fixed deserialization of BaseHouseDoorAddon.
 *		Added protection in OnLocationChanged.
 *		Added function to fix the bad save.
 *  12/05/06 Taran Kain
 *      Added iron gates.
 *  12/05/06 Taran Kain
 *      Fixed phantom-door issue.
 *  10/15/06, Rhiannon
 *		Set BaseHouseDoorComponent.Flip() to override instead of virtual
 *  9/05/06 Taran Kain
 *		Fixed OnChop
 *	9/03/06 Taran Kain
 *		Fixed point calculation in CouldFit
 *	9/02/06 Taran Kain
 *		Removed restriction in CouldFit that blocks placement next to existing doors.
 *	9/01/06 Taran Kain
 *		Initial version.
 */

using Server.Multis;
using System.Collections;

namespace Server.Items
{
    [FlipableAttribute]
    public abstract class BaseHouseDoorComponent : BaseHouseDoor
    {
        private BaseAddon m_Addon;

        public BaseAddon Addon
        {
            get { return m_Addon; }
            set { m_Addon = value; }
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get { return base.Hue; }
            set
            {
                base.Hue = value;

                if (m_Addon != null && m_Addon.ShareHue)
                    m_Addon.Hue = value;
            }
        }

        public BaseHouseDoorComponent(int closedID, int openedID, int customClosedID, int customOpenedID, int openedSound, int closedSound, DoorFacing facing, Point3D offset)
            : base(closedID, openedID, customClosedID, customOpenedID, openedSound, closedSound, facing, offset)
        {
            Movable = false;
        }

        public BaseHouseDoorComponent(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);

            writer.Write((Item)m_Addon);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Addon = reader.ReadItem() as BaseAddon;
                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Addon != null)
                m_Addon.Delete();
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            if (m_Addon != null)
                m_Addon.Location = Location;
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_Addon != null)
                m_Addon.Map = Map;
        }

        public override void Flip()
        {
            if (Facing == DoorFacing.NorthCW)
                Facing = DoorFacing.WestCW;
            else
                Facing = (DoorFacing)((int)Facing + 1);
        }

        protected override void Chop(Mobile from)
        {   // final effects and message played in OnChop()
            if (m_Addon != null)
                m_Addon.OnChop(from);
        }
    }

    public abstract class BaseHouseDoorAddon : BaseAddon
    {
        private BaseHouseDoorComponent m_Door;

        public BaseHouseDoorComponent Door
        {
            get { return m_Door; }
            set
            {
                if (m_Door != value)
                {
                    if (m_Door != null)
                        m_Door.Addon = null;

                    m_Door = value;

                    if (m_Door != null)
                        m_Door.Addon = this;
                }
            }
        }

        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get { return base.Hue; }
            set
            {
                if (base.Hue != value)
                {
                    base.Hue = value;

                    if (m_Door != null && this.ShareHue)
                        m_Door.Hue = value;
                }
            }
        }

        public BaseHouseDoorAddon()
            : base()
        {
        }

        public BaseHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override AddonFitResult CouldFit(IPoint3D p, Map map, Mobile from, ref BaseHouse house)
        {
            // rewritten, because we don't use the component list

            if (Deleted)
                return AddonFitResult.Blocked;

            if (!map.CanFit(p.X, p.Y, p.Z, m_Door.ItemData.Height))
                return AddonFitResult.Blocked;
            else if (!CheckHouse(from, new Point3D(p), map, m_Door.ItemData.Height, ref house))
                return AddonFitResult.NotInHouse;

            if (house != null)
            {
                ArrayList doors = house.Doors;

                for (int i = 0; i < doors.Count; ++i)
                {
                    BaseDoor door = doors[i] as BaseDoor;

                    if (door != null && door.Open)
                        return AddonFitResult.DoorsNotClosed;
                }
            }

            return AddonFitResult.Valid;
        }

        public override void OnAfterPlace(Mobile placer, BaseHouse house)
        {
            base.OnAfterPlace(placer, house);

            if (house != null)
                house.Doors.Add(m_Door);
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            if (m_Door != null)
                m_Door.Location = Location;
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_Door != null)
                m_Door.Map = Map;
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Door != null)
                m_Door.Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)2);

            //version 0
            writer.Write((Item)m_Door);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                case 1:
                case 0:
                    {
                        m_Door = reader.ReadItem() as BaseHouseDoorComponent;

                        if (version == 1)
                        {
                            /* Yoar: In version 1, we saved a ref to the house. I feel that we don't need this ref.
                             * This ref was only used to unregister the door addon from the house's addon list on deletion.
                             * However, addons are already unregistered from the house's addon list when they're chopped.
                             * This behavior is sufficient and consistent with other addons.
                             */
                            reader.ReadItem();
                        }

                        break;
                    }

            }
        }
    }

    #region DEFINED

    #region METAL HOUSE DOOR

    public class MetalHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class MetalHouseDoorComponent : BaseHouseDoorComponent
        {
            public MetalHouseDoorComponent(DoorFacing facing)
                : base(0x675, 0x676, -1, -1, 0xEC, 0xF3, facing, Point3D.Zero)
            {
            }

            public MetalHouseDoorComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer) // Default Serialize method
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader) // Default Deserialize method
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new MetalHouseDoorDeed();
            }
        }

        [Constructable]
        public MetalHouseDoorAddon()
            : base()
        {
            Door = new MetalHouseDoorComponent(DoorFacing.EastCCW);
        }

        public MetalHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class MetalHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new MetalHouseDoorAddon();
            }
        }

        [Constructable]
        public MetalHouseDoorDeed()
            : base()
        {
            Name = "metal house door";
        }

        public MetalHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region DARK WOOD DOOR

    public class DarkWoodHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class DarkWoodHouseDoorComponent : BaseHouseDoorComponent
        {
            public DarkWoodHouseDoorComponent(DoorFacing facing)
                : base(0x6A5, 0x6A6, -1, -1, 0xEA, 0xF1, facing, Point3D.Zero)
            {
            }

            public DarkWoodHouseDoorComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer) // Default Serialize method
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader) // Default Deserialize method
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new DarkWoodHouseDoorDeed();
            }
        }

        public DarkWoodHouseDoorAddon()
            : base()
        {
            Door = new DarkWoodHouseDoorComponent(DoorFacing.EastCCW);
        }

        public DarkWoodHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class DarkWoodHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new DarkWoodHouseDoorAddon();
            }
        }

        [Constructable]
        public DarkWoodHouseDoorDeed()
            : base()
        {
            Name = "dark wood house door";
        }

        public DarkWoodHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region DARK WOOD GATE

    public class DarkWoodGateHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class DarkWoodGateHouseDoorComponent : BaseHouseDoorComponent
        {
            public DarkWoodGateHouseDoorComponent(DoorFacing facing)
                : base(0x866, 0x867, -1, -1, 0xEB, 0xF2, facing, Point3D.Zero)
            {
            }

            public DarkWoodGateHouseDoorComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer) // Default Serialize method
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader) // Default Deserialize method
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new DarkWoodGateHouseDoorDeed();
            }
        }

        public DarkWoodGateHouseDoorAddon()
            : base()
        {
            Door = new DarkWoodGateHouseDoorComponent(DoorFacing.EastCCW);
        }

        public DarkWoodGateHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class DarkWoodGateHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new DarkWoodGateHouseDoorAddon();
            }
        }

        [Constructable]
        public DarkWoodGateHouseDoorDeed()
            : base()
        {
            Name = "dark wood gate house door";
        }

        public DarkWoodGateHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region IRON GATE DOOR

    public class IronGateHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class IronGateHouseDoorComponent : BaseHouseDoorComponent
        {
            public IronGateHouseDoorComponent(DoorFacing facing)
                : base(0x824, 0x825, -1, -1, 0xEC, 0xF3, facing, Point3D.Zero)
            {
            }

            public IronGateHouseDoorComponent(Serial s)
                : base(s)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                // version
                writer.Write((int)0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new IronGateHouseDoorDeed();
            }
        }

        public IronGateHouseDoorAddon()
            : base()
        {
            Door = new IronGateHouseDoorComponent(DoorFacing.EastCCW);
        }

        public IronGateHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class IronGateHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new IronGateHouseDoorAddon();
            }
        }

        [Constructable]
        public IronGateHouseDoorDeed()
            : base()
        {
            Name = "iron gate house door";
        }

        public IronGateHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
    //
    public class SmallIronGateHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class SmallIronGateHouseDoorComponent : BaseHouseDoorComponent
        {
            public SmallIronGateHouseDoorComponent(DoorFacing facing)
                : base(0x84C, 0x84D, -1, -1, 0xEC, 0xF3, facing, Point3D.Zero)
            {
            }

            public SmallIronGateHouseDoorComponent(Serial s)
                : base(s)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                // version
                writer.Write((int)0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SmallIronGateHouseDoorDeed();
            }
        }

        public SmallIronGateHouseDoorAddon()
            : base()
        {
            Door = new SmallIronGateHouseDoorComponent(DoorFacing.EastCCW);
        }

        public SmallIronGateHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SmallIronGateHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SmallIronGateHouseDoorAddon();
            }
        }

        [Constructable]
        public SmallIronGateHouseDoorDeed()
            : base()
        {
            Name = "small iron gate house door";
        }

        public SmallIronGateHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region CELL DOOR

    public class CellHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class CellHouseDoorComponent : BaseHouseDoorComponent
        {
            public CellHouseDoorComponent(DoorFacing facing)
                : base(0x685, 0x686, -1, -1, 0xEC, 0xF3, facing, Point3D.Zero)
            {
            }

            public CellHouseDoorComponent(Serial s)
                : base(s)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                // version
                writer.Write((int)0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new CellHouseDoorDeed();
            }
        }

        public CellHouseDoorAddon()
            : base()
        {
            Door = new CellHouseDoorComponent(DoorFacing.EastCCW);
        }

        public CellHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class CellHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new CellHouseDoorAddon();
            }
        }

        [Constructable]
        public CellHouseDoorDeed()
            : base()
        {
            Name = "cell house door";
        }

        public CellHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region STRONG WOOD DOOR

    public class StrongWoodHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class StrongWoodHouseDoorComponent : BaseHouseDoorComponent
        {
            public StrongWoodHouseDoorComponent(DoorFacing facing)
                : base(0x6E5, 0x6E6, -1, -1, 0xEA, 0xF1, facing, Point3D.Zero)
            {
            }

            public StrongWoodHouseDoorComponent(Serial s)
                : base(s)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                // version
                writer.Write((int)0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new StrongWoodHouseDoorDeed();
            }
        }

        public StrongWoodHouseDoorAddon()
            : base()
        {
            Door = new StrongWoodHouseDoorComponent(DoorFacing.EastCCW);
        }

        public StrongWoodHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class StrongWoodHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new StrongWoodHouseDoorAddon();
            }
        }

        [Constructable]
        public StrongWoodHouseDoorDeed()
            : base()
        {
            Name = "strong wood house door";
        }

        public StrongWoodHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region LIGHT WOOD DOOR

    public class LightWoodHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class LightWoodHouseDoorComponent : BaseHouseDoorComponent
        {
            public LightWoodHouseDoorComponent(DoorFacing facing)
                : base(0x6D5, 0x6D6, -1, -1, 0xEA, 0xF1, facing, Point3D.Zero)
            {
            }

            public LightWoodHouseDoorComponent(Serial s)
                : base(s)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                // version
                writer.Write((int)0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new LightWoodHouseDoorDeed();
            }
        }

        public LightWoodHouseDoorAddon()
            : base()
        {
            Door = new LightWoodHouseDoorComponent(DoorFacing.EastCCW);
        }

        public LightWoodHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class LightWoodHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new LightWoodHouseDoorAddon();
            }
        }

        [Constructable]
        public LightWoodHouseDoorDeed()
            : base()
        {
            Name = "light wood house door";
        }

        public LightWoodHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region LIGHT WOOD GATE

    public class LightWoodGateHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class LightWoodGateHouseDoorComponent : BaseHouseDoorComponent
        {
            public LightWoodGateHouseDoorComponent(DoorFacing facing)
                : base(0x839, 0x83A, -1, -1, 0xEB, 0xF2, facing, Point3D.Zero)
            {
            }

            public LightWoodGateHouseDoorComponent(Serial s)
                : base(s)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                // version
                writer.Write((int)0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new LightWoodGateHouseDoorDeed();
            }
        }

        public LightWoodGateHouseDoorAddon()
            : base()
        {
            Door = new LightWoodGateHouseDoorComponent(DoorFacing.EastCCW);
        }

        public LightWoodGateHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class LightWoodGateHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new LightWoodGateHouseDoorAddon();
            }
        }

        [Constructable]
        public LightWoodGateHouseDoorDeed()
            : base()
        {
            Name = "light wood gate house door";
        }

        public LightWoodGateHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region RATTAN DOOR

    public class RattanHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class RattanHouseDoorComponent : BaseHouseDoorComponent
        {
            public RattanHouseDoorComponent(DoorFacing facing)
                : base(0x695, 0x696, -1, -1, 0xEB, 0xF2, facing, Point3D.Zero)
            {
            }

            public RattanHouseDoorComponent(Serial s)
                : base(s)
            {
            }

            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                // version
                writer.Write((int)0);
            }

            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new RattanHouseDoorDeed();
            }
        }

        public RattanHouseDoorAddon()
            : base()
        {
            Door = new RattanHouseDoorComponent(DoorFacing.EastCCW);
        }

        public RattanHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class RattanHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new RattanHouseDoorAddon();
            }
        }

        [Constructable]
        public RattanHouseDoorDeed()
            : base()
        {
            Name = "rattan house door";
        }

        public RattanHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region SECRET STONE DOOR

    public class SecretStoneHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class SecretStoneHouseDoorComponent : BaseHouseDoorComponent
        {
            public SecretStoneHouseDoorComponent(DoorFacing facing)
                : base(0x324, 0x325, -1, -1, 0xED, 0xF4, facing, Point3D.Zero)
            {
            }

            public SecretStoneHouseDoorComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer) // Default Serialize method
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader) // Default Deserialize method
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SecretStoneHouseDoorDeed();
            }
        }

        public SecretStoneHouseDoorAddon()
            : base()
        {
            Door = new SecretStoneHouseDoorComponent(DoorFacing.EastCCW);
        }

        public SecretStoneHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SecretStoneHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SecretStoneHouseDoorAddon();
            }
        }

        [Constructable]
        public SecretStoneHouseDoorDeed()
            : base()
        {
            Name = "secret stone house door";
        }

        public SecretStoneHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region SECRET LIGHT STONE DOOR

    public class SecretLightStoneHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class SecretLightStoneHouseDoorComponent : BaseHouseDoorComponent
        {
            public SecretLightStoneHouseDoorComponent(DoorFacing facing)
                : base(0x354, 0x355, -1, -1, 0xED, 0xF4, facing, Point3D.Zero)
            {
            }

            public SecretLightStoneHouseDoorComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer) // Default Serialize method
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader) // Default Deserialize method
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SecretLightStoneHouseDoorDeed();
            }
        }

        public SecretLightStoneHouseDoorAddon()
            : base()
        {
            Door = new SecretLightStoneHouseDoorComponent(DoorFacing.EastCCW);
        }

        public SecretLightStoneHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SecretLightStoneHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SecretLightStoneHouseDoorAddon();
            }
        }

        [Constructable]
        public SecretLightStoneHouseDoorDeed()
            : base()
        {
            Name = "secret light stone house door";
        }

        public SecretLightStoneHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region SECRET LIGHT WOOD DOOR

    public class SecretLightWoodHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class SecretLightWoodHouseDoorComponent : BaseHouseDoorComponent
        {
            public SecretLightWoodHouseDoorComponent(DoorFacing facing)
                : base(0x344, 0x345, -1, -1, 0xED, 0xF4, facing, Point3D.Zero)
            {
            }

            public SecretLightWoodHouseDoorComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer) // Default Serialize method
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader) // Default Deserialize method
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SecretLightWoodHouseDoorDeed();
            }
        }

        public SecretLightWoodHouseDoorAddon()
            : base()
        {
            Door = new SecretLightWoodHouseDoorComponent(DoorFacing.EastCCW);
        }

        public SecretLightWoodHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SecretLightWoodHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SecretLightWoodHouseDoorAddon();
            }
        }

        [Constructable]
        public SecretLightWoodHouseDoorDeed()
            : base()
        {
            Name = "secret light wood house door";
        }

        public SecretLightWoodHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #region SECRET DARK WOOD DOOR

    public class SecretDarkWoodHouseDoorAddon : BaseHouseDoorAddon
    {
        [Flipable]
        private class SecretDarkWoodHouseDoorComponent : BaseHouseDoorComponent
        {
            public SecretDarkWoodHouseDoorComponent(DoorFacing facing)
                : base(0x334, 0x335, -1, -1, 0xED, 0xF4, facing, Point3D.Zero)
            {
            }

            public SecretDarkWoodHouseDoorComponent(Serial serial)
                : base(serial)
            {
            }

            public override void Serialize(GenericWriter writer) // Default Serialize method
            {
                base.Serialize(writer);

                writer.Write((int)0); // version
            }

            public override void Deserialize(GenericReader reader) // Default Deserialize method
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();
            }
        }

        public override BaseAddonDeed Deed
        {
            get
            {
                return new SecretDarkWoodHouseDoorDeed();
            }
        }

        public SecretDarkWoodHouseDoorAddon()
            : base()
        {
            Door = new SecretDarkWoodHouseDoorComponent(DoorFacing.EastCCW);
        }

        public SecretDarkWoodHouseDoorAddon(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class SecretDarkWoodHouseDoorDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new SecretDarkWoodHouseDoorAddon();
            }
        }

        [Constructable]
        public SecretDarkWoodHouseDoorDeed()
            : base()
        {
            Name = "secret dark wood house door";
        }

        public SecretDarkWoodHouseDoorDeed(Serial s)
            : base(s)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    #endregion

    #endregion
}