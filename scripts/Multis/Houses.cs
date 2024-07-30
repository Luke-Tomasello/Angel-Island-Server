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

/* Scripts/Multis/Houses.cs
 * ChangeLog
 * 7/27/2023, Adam (Tower)
 *      set the 2nd and 3rd floor doors to NorthCW. This is so they secure the inner room
 * 11/08/10, Pix
 *      Changed secures/lockdowns for UOSP
 *	7/27/07, Adam
 *		Check SuppressRegion property in the bass class to turn on region suppression 
 *		This is useful for structures like the 'event castle' when you want the underlying
 *		custom region to shine through.
 *	5/13/04, mith
 *		All houses have been modified to take another variable (LockdownContainers) and 
 *		have had their number of secures reduced by half.
 */

using Server.Items;
using Server.Multis.Deeds;

namespace Server.Multis
{
    public class SmallOldHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-3, -3, 7, 7), new Rectangle2D(-1, 4, 3, 1) };

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public override int DefaultPrice { get { return 43800; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.TwoStoryFoundations[0]; } }

        public SmallOldHouse(Mobile owner, int id)
            : base(id, owner, 270, 2, 2)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 425;
                this.MaxSecuresRaw = 3;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoor(0, 3, 7, keyValue);

            SetSign(2, 4, 5);

            BanLocation = new Point3D(2, 4, 0);
        }

        public SmallOldHouse(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed()
        {
            switch (ItemID ^ 0x4000)
            {
                case 0x64: return new StonePlasterHouseDeed();
                case 0x66: return new FieldStoneHouseDeed();
                case 0x68: return new SmallBrickHouseDeed();
                case 0x6A: return new WoodHouseDeed();
                case 0x6C: return new WoodPlasterHouseDeed();
                case 0x6E:
                default: return new ThatchedRoofCottageDeed();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class GuildHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-7, -7, 14, 14), new Rectangle2D(-2, 7, 4, 1) };

        public override int DefaultPrice { get { return 144500; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.ThreeStoryFoundations[20]; } }
        public override int ConvertOffsetX { get { return -1; } }
        public override int ConvertOffsetY { get { return -1; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public GuildHouse(Mobile owner)
            : base(0x74, owner, 600, 4, 2)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 1100;
                this.MaxSecuresRaw = 8;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoors(-1, 6, 7, keyValue);

            SetSign(4, 8, 16);

            BanLocation = new Point3D(4, 8, 0);

            AddSouthDoor(-3, -1, 7);
            AddSouthDoor(3, -1, 7);
        }

        public GuildHouse(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new BrickHouseDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class TwoStoryHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-7, 0, 14, 7), new Rectangle2D(-7, -7, 9, 7), new Rectangle2D(-4, 7, 4, 1) };

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public override int DefaultPrice { get { return 192400; } }

        public TwoStoryHouse(Mobile owner, int id)
            : base(id, owner, 750, 5, 3)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 1370;
                this.MaxSecuresRaw = 10;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoors(-3, 6, 7, keyValue);

            SetSign(2, 8, 16);

            BanLocation = new Point3D(2, 8, 0);

            AddSouthDoor(-3, 0, 7);
            AddSouthDoor(id == 0x76 ? -2 : -3, 0, 27);
        }

        public TwoStoryHouse(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed()
        {
            switch (ItemID ^ 0x4000)
            {
                case 0x76: return new TwoStoryWoodPlasterHouseDeed();
                case 0x78:
                default: return new TwoStoryStonePlasterHouseDeed();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class Tower : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-7, -7, 16, 14), new Rectangle2D(-1, 7, 4, 2), new Rectangle2D(-11, 0, 4, 7), new Rectangle2D(9, 0, 4, 7) };

        public override int DefaultPrice { get { return 433200; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.ThreeStoryFoundations[37]; } }
        public override int ConvertOffsetY { get { return -1; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public Tower(Mobile owner)
            : base(0x7A, owner, 1150, 8, 4)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 2119;
                this.MaxSecuresRaw = 15;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoors(false, 0, 6, 6, keyValue);

            SetSign(5, 8, 16);

            BanLocation = new Point3D(5, 8, 0);

            AddSouthDoor(false, 3, -2, 6);
            AddEastDoor(false, 1, 4, 26, DoorFacing.NorthCW);
            AddEastDoor(false, 1, 4, 46, DoorFacing.NorthCW);
        }

        public Tower(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new TowerDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class Keep : BaseHouse//warning: ODD shape!
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-11, -11, 7, 8), new Rectangle2D(-11, 5, 7, 8), new Rectangle2D(6, -11, 7, 8), new Rectangle2D(6, 5, 7, 8), new Rectangle2D(-9, -3, 5, 8), new Rectangle2D(6, -3, 5, 8), new Rectangle2D(-4, -9, 10, 20), new Rectangle2D(-1, 11, 4, 1) };

        public override int DefaultPrice { get { return 665200; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public Keep(Mobile owner)
            : base(0x7C, owner, 1300, 9, 5)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 2625;
                this.MaxSecuresRaw = 18;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoors(false, 0, 10, 6, keyValue);

            SetSign(5, 12, 16);

            BanLocation = new Point3D(5, 13, 0);
        }

        public Keep(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new KeepDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class Castle : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-15, -15, 31, 31), new Rectangle2D(-1, 16, 4, 1) };

        public override int DefaultPrice { get { return 1022800; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public Castle(Mobile owner)
            : base(0x7E, owner, 1950, 14, 7)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 4076;
                this.MaxSecuresRaw = 28;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoors(false, 0, 15, 6, keyValue);

            SetSign(5, 17, 16);

            BanLocation = new Point3D(5, 17, 0);

            AddSouthDoors(false, 0, 11, 6, true);
            AddSouthDoors(false, 0, 5, 6, false);
            AddSouthDoors(false, -1, -11, 6, false);
        }

        public Castle(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new CastleDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class LargePatioHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-7, -7, 15, 14), new Rectangle2D(-5, 7, 4, 1) };

        public override int DefaultPrice { get { return 152800; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.ThreeStoryFoundations[29]; } }
        public override int ConvertOffsetY { get { return -1; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public LargePatioHouse(Mobile owner)
            : base(0x8C, owner, 600, 4, 2)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 1100;
                this.MaxSecuresRaw = 8;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoors(-4, 6, 7, keyValue);

            SetSign(1, 8, 16);

            BanLocation = new Point3D(1, 8, 0);

            AddEastDoor(1, 4, 7);
            AddEastDoor(1, -4, 7);
            AddSouthDoor(4, -1, 7);
        }

        public LargePatioHouse(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new LargePatioDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class LargeMarbleHouse : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-7, -7, 15, 14), new Rectangle2D(-6, 7, 6, 1) };

        public override int DefaultPrice { get { return 192000; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.ThreeStoryFoundations[29]; } }
        public override int ConvertOffsetY { get { return -1; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public LargeMarbleHouse(Mobile owner)
            : base(0x96, owner, 750, 5, 3)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 1370;
                this.MaxSecuresRaw = 10;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoors(false, -4, 3, 4, keyValue);

            SetSign(1, 8, 11);

            BanLocation = new Point3D(1, 8, 0);
        }

        public LargeMarbleHouse(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new LargeMarbleDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class SmallTower : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-3, -3, 8, 7), new Rectangle2D(2, 4, 3, 1) };

        public override int DefaultPrice { get { return 88500; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.TwoStoryFoundations[6]; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public SmallTower(Mobile owner)
            : base(0x98, owner, 300, 2, 2)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 580;
                this.MaxSecuresRaw = 4;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoor(false, 3, 3, 6, keyValue);

            SetSign(1, 4, 5);

            BanLocation = new Point3D(1, 4, 0);
        }

        public SmallTower(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new SmallTowerDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class LogCabin : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-3, -6, 8, 13) };

        public override int DefaultPrice { get { return 97800; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.TwoStoryFoundations[12]; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public LogCabin(Mobile owner)
            : base(0x9A, owner, 600, 4, 2)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 1100;
                this.MaxSecuresRaw = 8;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoor(1, 4, 8, keyValue);

            SetSign(5, 8, 20);

            BanLocation = new Point3D(5, 8, 0);

            AddSouthDoor(1, 0, 29);
        }

        public LogCabin(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new LogCabinDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class SandStonePatio : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-5, -4, 12, 8), new Rectangle2D(-2, 4, 3, 1) };

        public override int DefaultPrice { get { return 90900; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.TwoStoryFoundations[35]; } }
        public override int ConvertOffsetY { get { return -1; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public SandStonePatio(Mobile owner)
            : base(0x9C, owner, 450, 3, 2)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 850;
                this.MaxSecuresRaw = 6;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoor(-1, 3, 6, keyValue);

            SetSign(4, 6, 24);

            BanLocation = new Point3D(4, 6, 0);
        }

        public SandStonePatio(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new SandstonePatioDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class TwoStoryVilla : BaseHouse
    {
        public static Rectangle2D[] AreaArray = new Rectangle2D[] { new Rectangle2D(-5, -5, 11, 11), new Rectangle2D(2, 6, 4, 1) };

        public override int DefaultPrice { get { return 136500; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.TwoStoryFoundations[31]; } }

        public override Rectangle2D[] Area { get { return (base.SuppressRegion) ? base.Area : AreaArray; } }

        public TwoStoryVilla(Mobile owner)
            : base(0x9E, owner, 600, 4, 2)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 1100;
                this.MaxSecuresRaw = 8;
            }

            uint keyValue = CreateKeys(owner);

            AddSouthDoors(3, 1, 5, keyValue);

            SetSign(3, 8, 24);

            BanLocation = new Point3D(3, 8, 0);

            AddEastDoor(1, 0, 25);
            AddSouthDoor(-3, -1, 25);
        }

        public TwoStoryVilla(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed() { return new VillaDeed(); }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class SmallShop : BaseHouse
    {
        public override Rectangle2D[] Area { get { return (ItemID == 0x40A2 ? AreaArray1 : AreaArray2); } }

        public override int DefaultPrice { get { return 63000; } }

        public override HousePlacementEntry ConvertEntry { get { return HousePlacementEntry.TwoStoryFoundations[0]; } }

        public static Rectangle2D[] AreaArray1 = new Rectangle2D[] { new Rectangle2D(-3, -3, 7, 7), new Rectangle2D(-1, 4, 4, 1) };
        public static Rectangle2D[] AreaArray2 = new Rectangle2D[] { new Rectangle2D(-3, -3, 7, 7), new Rectangle2D(-2, 4, 3, 1) };

        public SmallShop(Mobile owner, int id)
            : base(id, owner, 275, 2, 2)
        {
            if (Core.RuleSets.SiegeStyleRules())
            {
                this.MaxLockDownsRaw = 425;
                this.MaxSecuresRaw = 3;
            }

            uint keyValue = CreateKeys(owner);

            BaseDoor door = MakeDoor(false, DoorFacing.EastCW);

            door.Locked = true;
            door.KeyValue = keyValue;

            if (door is BaseHouseDoor)
                ((BaseHouseDoor)door).Facing = DoorFacing.EastCCW;

            AddDoor(door, -2, 0, id == 0xA2 ? 24 : 27);

            Recorder(new Point3D(-2, 0, id == 0xA2 ? 24 : 27), null);

            //AddSouthDoor( false, -2, 0, 27 - (id == 0xA2 ? 3 : 0), keyValue );

            SetSign(3, 4, 7 - (id == 0xA2 ? 2 : 0));
            BanLocation = new Point3D(3, 4, 0);
        }

        public SmallShop(Serial serial)
            : base(serial)
        {
        }

        public override HouseDeed GetDeed()
        {
            switch (ItemID ^ 0x4000)
            {
                case 0xA0: return new StoneWorkshopDeed();
                case 0xA2:
                default: return new MarbleWorkshopDeed();
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);//version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}