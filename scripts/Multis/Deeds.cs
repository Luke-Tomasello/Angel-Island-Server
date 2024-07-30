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

/* Scripts/Multis/Deeds.cs
 * Changelog:
 *	6/5/22, Yoar
 *		Added a null check after the call to HouseDeed.GetHouse.
 *	2/27/10, Adam
 *		When placing a house over a tent, we need to Annex the tent
 *		Annex = deed it all and place the proceeds into the owners bank
 *	10/19/06, Adam
 *		Tower placement: not necessarily an exploit, but lets track it
 *	10/16/06, Adam
 *		Add flag to disable tower placement
 *	7/22/06, weaver
 *		Modified OnPlacement() to ignore Siege tents and count regular tents
 *	5/2/06, weaver
 *		Made HouseDeed.OnPlacement() virtual and modified to ignore tents
 * 	5/1/06, weaver
 *		Added overloaded HouseDeed constructor to handle deeds which
 *		explicitly specify their deed id 
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Targeting;
using System.Collections;

namespace Server.Multis.Deeds
{
    public class HousePlacementTarget : MultiTarget
    {
        private HouseDeed m_Deed;

        public HousePlacementTarget(HouseDeed deed)
            : base(deed.MultiID, deed.Offset)
        {
            m_Deed = deed;
        }

        protected override void OnTarget(Mobile from, object o)
        {
            IPoint3D ip = o as IPoint3D;

            if (ip != null)
            {
                if (ip is Item)
                    ip = ((Item)ip).GetWorldTop();

                Point3D p = new Point3D(ip);

                Region reg = Region.Find(new Point3D(p), from.Map);

                if (from.AccessLevel >= AccessLevel.GameMaster || reg.AllowHousing(p))
                    m_Deed.OnPlacement(from, p);
                else if (reg is TreasureRegion)
                    from.SendLocalizedMessage(1043287); // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
                else
                    from.SendLocalizedMessage(501265); // Housing can not be created in this area.
            }
        }
    }

    public abstract class HouseDeed : Item
    {

        private int m_MultiID;
        private Point3D m_Offset;

        public static int m_price = 0; // unused
        public abstract int Price { get; }

        [CommandProperty(AccessLevel.GameMaster)]
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

        [CommandProperty(AccessLevel.GameMaster)]
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

        // wea: explicit multi and deed id specifying constructor
        public HouseDeed(int did, int id, Point3D offset)
            : base(did)
        {
            Weight = 1.0;
            LootType = LootType.Newbied;
            m_Offset = offset;
            m_MultiID = id;
        }

        public HouseDeed(int id, Point3D offset)
            : base(0x14F0)
        {
            Weight = 1.0;
            LootType = LootType.Newbied;
            m_MultiID = id & 0x3FFF;
            m_Offset = offset;
        }

        public HouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version
            writer.Write(m_Offset);
            writer.Write(m_MultiID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Offset = reader.ReadPoint3D();
                        goto case 0;
                    }
                case 0:
                    {
                        m_MultiID = reader.ReadInt();
                        break;
                    }
            }

            if (Weight == 0.0)
                Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendLocalizedMessage(1010433); /* House placement cancellation could result in a
													   * 60 second delay in the return of your deed.
													   */

                from.Target = new HousePlacementTarget(this);
            }
        }

        public abstract BaseHouse GetHouse(Mobile owner);
        public abstract Rectangle2D[] Area { get; }

        // called by all house deeds but tents, they have their own implementation
        public virtual void OnPlacement(Mobile from, Point3D p)
        {

            if (Deleted)
            {   // exploit! (they passed the deed to someone else while the target cursor was up)
                LogHelper Logger = new LogHelper("HouseDeedExploit.log", false);
                Logger.Log(LogType.Mobile, from, string.Format("exploit! they passed the house deed to someone else while the target cursor was up"));
                Logger.Finish();
                // jail time
                Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(from as Mobiles.PlayerMobile, 3, "They passed the house deed to someone else while the target cursor was up.", false);
                jt.GoToJail();

                // tell staff
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
                Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. They passed the house deed to someone else while the target cursor was up.", from as Mobiles.PlayerMobile));
                return;
            }

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
            {
                // wea: modified to ignore *siege* tents

                int i = 0;
                ArrayList ahouses = new ArrayList();
                ahouses = BaseHouse.GetAccountHouses(from);

                if (ahouses != null)
                {
                    foreach (object o in ahouses)
                        if (!(o is SiegeTent))
                            i++;

                    if (i > 0)
                    {
                        from.SendLocalizedMessage(501271); // You already own a house or a regular tent, you may not place another!
                        return;
                    }
                }
            }

            ArrayList toMove;
            Point3D center = new Point3D(p.X - m_Offset.X, p.Y - m_Offset.Y, p.Z - m_Offset.Z);
            HousePlacementResult res = HousePlacement.Check(from, m_MultiID, center, out toMove);

            switch (res)
            {
                case HousePlacementResult.Valid:
                    {
                        BaseHouse house = GetHouse(from);

                        if (house == null)
                            break;

                        house.MoveToWorld(center, from.Map);

                        // retarget any runes/moonstones, etc. that are pointing here.
                        EventSink.InvokeHousePlaced(new HousePlacedEventArgs(from.Map, house.Serial));

                        Delete();

                        for (int i = 0; i < toMove.Count; ++i)
                        {
                            object o = toMove[i];

                            if (o is Tent && Core.RuleSets.TentAnnexation())
                                (o as Tent).Annex(house);
                            else if (o is Mobile)
                                ((Mobile)o).Location = house.BanLocation;
                            else if (o is Item)
                                ((Item)o).Location = house.BanLocation;
                        }

                        break;
                    }
                case HousePlacementResult.TentRegion:
                case HousePlacementResult.BadItem:
                case HousePlacementResult.BadLand:
                case HousePlacementResult.BadStatic:
                case HousePlacementResult.BadRegionHidden:
                    {
                        from.SendLocalizedMessage(1043287); // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
                        break;
                    }
                case HousePlacementResult.NoSurface:
                    {
                        from.SendMessage("The house could not be created here.  Part of the foundation would not be on any surface.");
                        break;
                    }
                case HousePlacementResult.BadArea:
                case HousePlacementResult.BadRegion:
                    {
                        from.SendLocalizedMessage(501265); // Housing cannot be created in this area.
                        break;
                    }
                case HousePlacementResult.BadRegionTownship:
                    {
                        from.SendMessage("You are not authorized to build in this township.");
                        break;
                    }
                case HousePlacementResult.NearTownship:
                    {
                        from.SendMessage("You are not authorized to build near this township.");
                        break;
                    }
            }
        }
    }

    public class StonePlasterHouseDeed : HouseDeed
    {
        public static new int m_price = 43800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public StonePlasterHouseDeed()
            : base(0x64, new Point3D(0, 4, 0))
        {
        }

        public StonePlasterHouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallOldHouse(owner, 0x64);
        }

        public override int LabelNumber { get { return 1041211; } }
        public override Rectangle2D[] Area { get { return SmallOldHouse.AreaArray; } }

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

    public class FieldStoneHouseDeed : HouseDeed
    {
        public static new int m_price = 43800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public FieldStoneHouseDeed()
            : base(0x66, new Point3D(0, 4, 0))
        {
        }

        public FieldStoneHouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallOldHouse(owner, 0x66);
        }

        public override int LabelNumber { get { return 1041212; } }
        public override Rectangle2D[] Area { get { return SmallOldHouse.AreaArray; } }

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

    public class SmallBrickHouseDeed : HouseDeed
    {
        public static new int m_price = 43800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public SmallBrickHouseDeed()
            : base(0x68, new Point3D(0, 4, 0))
        {
        }

        public SmallBrickHouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallOldHouse(owner, 0x68);
        }

        public override int LabelNumber { get { return 1041213; } }
        public override Rectangle2D[] Area { get { return SmallOldHouse.AreaArray; } }

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

    public class WoodHouseDeed : HouseDeed
    {
        public static new int m_price = 43800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public WoodHouseDeed()
            : base(0x6A, new Point3D(0, 4, 0))
        {
        }

        public WoodHouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallOldHouse(owner, 0x6A);
        }

        public override int LabelNumber { get { return 1041214; } }
        public override Rectangle2D[] Area { get { return SmallOldHouse.AreaArray; } }

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

    public class WoodPlasterHouseDeed : HouseDeed
    {
        public static new int m_price = 43800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public WoodPlasterHouseDeed()
            : base(0x6C, new Point3D(0, 4, 0))
        {
        }

        public WoodPlasterHouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallOldHouse(owner, 0x6C);
        }

        public override int LabelNumber { get { return 1041215; } }
        public override Rectangle2D[] Area { get { return SmallOldHouse.AreaArray; } }

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

    public class ThatchedRoofCottageDeed : HouseDeed
    {
        public static new int m_price = 43800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public ThatchedRoofCottageDeed()
            : base(0x6E, new Point3D(0, 4, 0))
        {
        }

        public ThatchedRoofCottageDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallOldHouse(owner, 0x6E);
        }

        public override int LabelNumber { get { return 1041216; } }
        public override Rectangle2D[] Area { get { return SmallOldHouse.AreaArray; } }

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

    public class BrickHouseDeed : HouseDeed
    {
        public static new int m_price = 144500;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public BrickHouseDeed()
            : base(0x74, new Point3D(-1, 7, 0))
        {
        }

        public BrickHouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new GuildHouse(owner);
        }

        public override int LabelNumber { get { return 1041219; } }
        public override Rectangle2D[] Area { get { return GuildHouse.AreaArray; } }

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

    public class TwoStoryWoodPlasterHouseDeed : HouseDeed
    {
        public static new int m_price = 192400;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public TwoStoryWoodPlasterHouseDeed()
            : base(0x76, new Point3D(-3, 7, 0))
        {
        }

        public TwoStoryWoodPlasterHouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new TwoStoryHouse(owner, 0x76);
        }

        public override int LabelNumber { get { return 1041220; } }
        public override Rectangle2D[] Area { get { return TwoStoryHouse.AreaArray; } }

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

    public class TwoStoryStonePlasterHouseDeed : HouseDeed
    {
        public static new int m_price = 192400;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public TwoStoryStonePlasterHouseDeed()
            : base(0x78, new Point3D(-3, 7, 0))
        {
        }

        public TwoStoryStonePlasterHouseDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new TwoStoryHouse(owner, 0x78);
        }

        public override int LabelNumber { get { return 1041221; } }
        public override Rectangle2D[] Area { get { return TwoStoryHouse.AreaArray; } }

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

    public class TowerDeed : HouseDeed
    {
        public static new int m_price = 433200;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public TowerDeed()
            : base(0x7A, new Point3D(0, 7, 0))
        {
        }

        public TowerDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new Tower(owner);
        }

        public override int LabelNumber { get { return 1041222; } }
        public override Rectangle2D[] Area { get { return Tower.AreaArray; } }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
                if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.TowerAllowed) == false)
                {
                    from.SendMessage("Tower placement temporarily disabled");
                    return;
                }
                else
                {
                    // not necessarily an exploit, but lets track it
                    RecordCheater.TrackIt(from, "Note: Tower placement attempt.", true);
                }

            base.OnDoubleClick(from);
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

    public class KeepDeed : HouseDeed
    {
        public static new int m_price = 665200;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public KeepDeed()
            : base(0x7C, new Point3D(0, 11, 0))
        {
        }

        public KeepDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new Keep(owner);
        }

        public override int LabelNumber { get { return 1041223; } }
        public override Rectangle2D[] Area { get { return Keep.AreaArray; } }

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

    public class CastleDeed : HouseDeed
    {
        public static new int m_price = 1022800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public CastleDeed()
            : base(0x7E, new Point3D(0, 16, 0))
        {
        }

        public CastleDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new Castle(owner);
        }

        public override int LabelNumber { get { return 1041224; } }
        public override Rectangle2D[] Area { get { return Castle.AreaArray; } }

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

    public class LargePatioDeed : HouseDeed
    {
        public static new int m_price = 152800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public LargePatioDeed()
            : base(0x8C, new Point3D(-4, 7, 0))
        {
        }

        public LargePatioDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new LargePatioHouse(owner);
        }

        public override int LabelNumber { get { return 1041231; } }
        public override Rectangle2D[] Area { get { return LargePatioHouse.AreaArray; } }

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

    public class LargeMarbleDeed : HouseDeed
    {
        public static new int m_price = 192000;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public LargeMarbleDeed()
            : base(0x96, new Point3D(-4, 7, 0))
        {
        }

        public LargeMarbleDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new LargeMarbleHouse(owner);
        }

        public override int LabelNumber { get { return 1041236; } }
        public override Rectangle2D[] Area { get { return LargeMarbleHouse.AreaArray; } }

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

    public class SmallTowerDeed : HouseDeed
    {
        public static new int m_price = 88500;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public SmallTowerDeed()
            : base(0x98, new Point3D(3, 4, 0))
        {
        }

        public SmallTowerDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallTower(owner);
        }

        public override int LabelNumber { get { return 1041237; } }
        public override Rectangle2D[] Area { get { return SmallTower.AreaArray; } }

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

    public class LogCabinDeed : HouseDeed
    {
        public static new int m_price = 97800;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public LogCabinDeed()
            : base(0x9A, new Point3D(1, 6, 0))
        {
        }

        public LogCabinDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new LogCabin(owner);
        }

        public override int LabelNumber { get { return 1041238; } }
        public override Rectangle2D[] Area { get { return LogCabin.AreaArray; } }

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

    public class SandstonePatioDeed : HouseDeed
    {
        public static new int m_price = 90900;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public SandstonePatioDeed()
            : base(0x9C, new Point3D(-1, 4, 0))
        {
        }

        public SandstonePatioDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SandStonePatio(owner);
        }

        public override int LabelNumber { get { return 1041239; } }
        public override Rectangle2D[] Area { get { return SandStonePatio.AreaArray; } }

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

    public class VillaDeed : HouseDeed
    {
        public static new int m_price = 136500;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public VillaDeed()
            : base(0x9E, new Point3D(3, 6, 0))
        {
        }

        public VillaDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new TwoStoryVilla(owner);
        }

        public override int LabelNumber { get { return 1041240; } }
        public override Rectangle2D[] Area { get { return TwoStoryVilla.AreaArray; } }

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

    public class StoneWorkshopDeed : HouseDeed
    {
        public static new int m_price = 60600;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public StoneWorkshopDeed()
            : base(0xA0, new Point3D(-1, 4, 0))
        {
        }

        public StoneWorkshopDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallShop(owner, 0xA0);
        }

        public override int LabelNumber { get { return 1041241; } }
        public override Rectangle2D[] Area { get { return SmallShop.AreaArray2; } }

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

    public class MarbleWorkshopDeed : HouseDeed
    {
        public static new int m_price = 63000;
        [CommandProperty(AccessLevel.GameMaster)]
        public override int Price { get { return m_price; } }

        [Constructable]
        public MarbleWorkshopDeed()
            : base(0xA2, new Point3D(-1, 4, 0))
        {
        }

        public MarbleWorkshopDeed(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new SmallShop(owner, 0xA2);
        }

        public override int LabelNumber { get { return 1041242; } }
        public override Rectangle2D[] Area { get { return SmallShop.AreaArray1; } }

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