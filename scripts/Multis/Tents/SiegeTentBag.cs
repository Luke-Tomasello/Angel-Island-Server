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

/* Scripts/Multis/Tent/SiegeTentBag.cs
 * ChangeLog:
 *	2/27/10, Adam
 *		Replace the HousingPlacementTarget with a simple Target. This is because the MultiID for tents crashes old clients
 *		that try to do the neat dynamic display of the tent as the user moves it. Our simple Target 'just drops it here'
 *	08/03/06, weaver
 *		Added placement confirmation gump.
 *	05/22/06, weaver
 *		Initial creation.
 */

using Server;
using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Multis.Deeds;
using Server.Targeting;
using System.Collections;

public class SiegeTentBag : HouseDeed, IDyable
{
    public override int Price { get { return 0; } }

    [Constructable]
    public SiegeTentBag()
        : base(2648, 0x3FFE, new Point3D(0, 4, 0))
    {
        Name = "a rolled up siege tent";
        Weight = 20.0;
        Hue = 877;
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

    public SiegeTentBag(Serial serial)
        : base(serial)
    {
    }

    public override BaseHouse GetHouse(Mobile owner)
    {
        return new SiegeTent(owner, Hue);
    }

    public override int LabelNumber { get { return 1041211; } }
    public override Rectangle2D[] Area { get { return SiegeTent.AreaArray; } }

    // Override basic deed OnPlacement() so that tent specific text is used and a non tent multi id
    // based house placement check is performed

    // Also checks for account tents as opposed to houses

    public override void OnPlacement(Mobile from, Point3D p)
    {
        if (Deleted)
        {   // exploit! (they passed the deed to someone else while the target cursor was up)
            LogHelper Logger = new LogHelper("HouseDeedExploit.log", false);
            Logger.Log(LogType.Mobile, from, string.Format("exploit! they passed the house deed to someone else while the target cursor was up"));
            Logger.Finish();
            // jail time
            Server.Commands.Jail.JailPlayer jt = new Server.Commands.Jail.JailPlayer(from as Server.Mobiles.PlayerMobile, 3, "They passed the house deed to someone else while the target cursor was up.", false);
            jt.GoToJail();

            // tell staff
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("Staff message from SYSTEM:"));
            Server.Commands.CommandHandlers.BroadcastMessage(AccessLevel.Counselor, 0x482, string.Format("{0} was sent to jail. They passed the house deed to someone else while the target cursor was up.", from as Server.Mobiles.PlayerMobile));
            return;
        }

        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else if (from.AccessLevel < AccessLevel.GameMaster && HasAccountSiegeTent(from))
        {
            from.SendMessage("You already own a siege tent, you may not place another!");
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
                        from.SendMessage("The siege tent could not be created here, Either something is blocking the house, or the house would not be on valid terrain.");
                        break;
                    }
                case HousePlacementResult.NoSurface:
                    {
                        from.SendMessage("The siege tent could not be created here.  Part of the foundation would not be on any surface.");
                        break;
                    }
                case HousePlacementResult.BadRegion:
                    {
                        from.SendMessage("Siege tents cannot be placed in this area.");
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

    public static bool HasAccountSiegeTent(Mobile m)
    {
        ArrayList list = BaseHouse.GetAccountHouses(m);

        if (list == null)
            return false;

        for (int i = 0; i < list.Count; ++i)
        {
            if (list[i] is SiegeTent)
                if (!((SiegeTent)list[i]).Deleted)
                    return true;
        }

        return false;
    }

    public override void OnDoubleClick(Mobile from)
    {
        if (!IsChildOf(from.Backpack))
        {
            from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }
        else
            from.Target = new PlaceTarget(this);
    }

    public class PlaceTarget : Target
    {
        SiegeTentBag m_tent;

        public PlaceTarget(SiegeTentBag tb)
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