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

/* Scripts/Multis/Tent/TentBag.cs
 * ChangeLog:
 *	2/27/10, Adam
 *		Replace the HousingPlacementTarget with a simple Target. This is because the MultiID for tents crashes old clients
 *		that try to do the neat dynamic display of the tent as the user moves it. Our simple Target 'just drops it here'
 *	08/03/06, weaver
 *		Added placement confirmation gump.
 *	07/22/06, weaver
 *		Made tent bags newbied on construction.
 *		Changed placement rules to equivalent of basic houses.
 *		Removed now obsolete HasAccountTent() function.
 *	05/23/06, weaver
 *		Removed 60 second placement delay message.
 *	05/07/06, weaver
 *		Renamed to "a rolled up tent".
 *	05/01/06, weaver
 *		Initial creation.
 */

using Server.Gumps;
using Server.Items;
using Server.Targeting;
using System.Collections;

/*
 * [TypeAlias("Scripts.Engines.BulkOrders.LargeBOD")]
    public abstract class LargeBOD : BaseBOD
 */
namespace Server.Multis.Deeds
{
    [TypeAlias("TentBag")]
    public class TentBag : HouseDeed, IDyable
    {
        public static new int m_price = 7900;
        public override int Price { get { return m_price; } }

        [Constructable]
        public TentBag()
            : base(2648, 0x3FFE, new Point3D(0, 4, 0))
        {
            Name = "a rolled up tent";
            Weight = 20.0;
            LootType = LootType.Newbied;
        }

        // Implement Dye() member... tent roofs reflect dyed bag colour

        public virtual bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;
            else if (RootParent is Mobile && from != RootParent)
                return false;

            Hue = sender.DyedHue;
            return true;
        }

        public TentBag(Serial serial)
            : base(serial)
        {
        }

        public override BaseHouse GetHouse(Mobile owner)
        {
            return new Tent(owner, Hue);
        }

        public override int LabelNumber { get { return 1041211; } }
        public override Rectangle2D[] Area { get { return Tent.AreaArray; } }

        // Override basic deed OnPlacement() so that tent specific text is used and a non tent multi id
        // based house placement check is performed

        // Also checks for account tents as opposed to houses


        // called by only by tents, they have their own implementation
        public override void OnPlacement(Mobile from, Point3D p)
        {
            if (Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else if (from.AccessLevel < AccessLevel.GameMaster && BaseHouse.HasAccountHouse(from))
            {
                from.SendMessage("You already own a house, you may not place a regular tent!");
            }
            else
            {
                ArrayList toMove;
                Point3D center = new Point3D(p.X - Offset.X, p.Y - Offset.Y, p.Z - Offset.Z);
                HousePlacementResult res = HousePlacement.Check(from, 0x64, center, out toMove, true);

                switch (res)
                {
                    case HousePlacementResult.Valid:
                        {
                            BaseHouse house = GetHouse(from);
                            house.MoveToWorld(center, from.Map);
                            house.Public = true;

                            Delete();

                            for (int i = 0; i < toMove.Count; ++i)
                            {
                                object o = toMove[i];

                                if (o is Mobile)
                                    ((Mobile)o).Location = house.BanLocation;
                                else if (o is Item)
                                    ((Item)o).Location = house.BanLocation;
                            }

                            from.SendGump(new TentPlaceGump(from, house));
                            break;
                        }
                    case HousePlacementResult.TentRegion:
                    case HousePlacementResult.BadItem:
                    case HousePlacementResult.BadLand:
                    case HousePlacementResult.BadStatic:
                    case HousePlacementResult.BadRegionHidden:
                        {
                            from.SendMessage("The tent could not be created here, Either something is blocking it, or it would not be on valid terrain.");
                            break;
                        }
                    case HousePlacementResult.NoSurface:
                        {
                            from.SendMessage("The tent could not be created here.  Part of the foundation would not be on any surface.");
                            break;
                        }
                    case HousePlacementResult.BadRegion:
                        {
                            from.SendMessage("Tents cannot be placed in this area.");
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

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.Target = new PlaceTarget(this);
            }
        }

        public class PlaceTarget : Target
        {
            TentBag m_tent;

            public PlaceTarget(TentBag tb)
                : base(-1, true, TargetFlags.None)
            {
                m_tent = tb;
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
                        m_tent.OnPlacement(from, p);
                    else if (reg is TreasureRegion)
                        from.SendLocalizedMessage(1043287); // The house could not be created here.  Either something is blocking the house, or the house would not be on valid terrain.
                    else
                        from.SendLocalizedMessage(501265); // Housing can not be created in this area.
                }
            }
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