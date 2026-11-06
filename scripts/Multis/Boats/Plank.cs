/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts/Multis/Boats/Plank.cs
 *	ChangeLog:
 *	10/14/2026, Adam (FindDisembarkLocation)
 *	    OnMoveOver's disembark search is now SearchDisembark, shared with FindDisembarkLocation: a side-effect-free
 *	    query of where walking over the plank would put someone aboard (for Help Stuck). OnMoveOver is unchanged.
 *	1/19/2024, Adam (OnBoatTravel())
 *	    Added OnBoatTravel() to base creature so we check tis before boarding the boat
 *	    Minstrels for instance, hate boats!
 *  9/2/22, Yoar (WorldZone)
 *      - Added 'AllowDisembark' method to validate the disembark location.
 *      - Added WorldZone check in order to contain players within the world zone.
 *	8/28/22, Yoar
 *	    - Faction guards will no longer walk onto opened planks.
 *	    - Can no longer disembark into faction stronghold regions.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	13/Mar/2007, weaver
 *		Boat access exploit/bug fix.
 */

using Server.Factions;
using Server.Mobiles;
using Server.Multis;
using System;

namespace Server.Items
{
    public enum PlankSide { Port, Starboard }

    public class Plank : Item, ILockable
    {
        private BaseBoat? m_Boat;
        private PlankSide m_Side;
        private bool m_Locked;
        private uint m_KeyValue;

        private Timer? m_CloseTimer;

        public Plank(BaseBoat boat, PlankSide side, uint keyValue)
            : base(0x3EB1 + (int)side)
        {
            m_Boat = boat;
            m_Side = side;
            m_KeyValue = keyValue;
            m_Locked = true;

            Movable = false;
        }

        public Plank(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);//version

            writer.Write(m_Boat);
            writer.Write((int)m_Side);
            writer.Write(m_Locked);
            writer.Write(m_KeyValue);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Boat = reader.ReadItem() as BaseBoat;
                        m_Side = (PlankSide)reader.ReadInt();
                        m_Locked = reader.ReadBool();
                        m_KeyValue = reader.ReadUInt();

                        if (m_Boat == null)
                            Delete();

                        break;
                    }
            }

            if (IsOpen)
            {
                m_CloseTimer = new CloseTimer(this);
                m_CloseTimer.Start();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseBoat? Boat { get { return m_Boat; } set { m_Boat = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlankSide Side { get { return m_Side; } set { m_Side = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Locked { get { return m_Locked; } set { m_Locked = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public uint KeyValue { get { return m_KeyValue; } set { m_KeyValue = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsOpen { get { return (ItemID == 0x3ED5 || ItemID == 0x3ED4 || ItemID == 0x3E84 || ItemID == 0x3E89); } }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Starboard { get { return (m_Side == PlankSide.Starboard); } }

        public void SetFacing(Direction dir)
        {
            // Normalize to cardinal
            dir = (Direction)((int)dir & 0x7);

            // TEMP NOTE:
            // We discovered that the original RunUO-style E/W ItemID mapping produces inward-opening behavior
            // on the north-side plank for both East and West facings under our current boat placement model.
            // We fix this by swapping the East/West ItemIDs (for both open and closed) while keeping
            // plank identity stable (port remains port, starboard remains starboard).
            //
            // If this ever regresses on a specific boat type, we will generalize further using geometry.

            if (IsOpen)
            {
                switch (dir)
                {
                    case Direction.North: ItemID = Starboard ? 0x3ED4 : 0x3ED5; break;

                    // Swapped compared to original:
                    // Original East: Starboard ? 0x3E84 : 0x3E89
                    // New East:      Starboard ? 0x3E89 : 0x3E84
                    case Direction.East: ItemID = Starboard ? 0x3E89 : 0x3E84; break;

                    case Direction.South: ItemID = Starboard ? 0x3ED5 : 0x3ED4; break;

                    // Swapped compared to original:
                    // Original West: Starboard ? 0x3E89 : 0x3E84
                    // New West:      Starboard ? 0x3E84 : 0x3E89
                    case Direction.West: ItemID = Starboard ? 0x3E84 : 0x3E89; break;
                }
            }
            else
            {
                switch (dir)
                {
                    case Direction.North: ItemID = Starboard ? 0x3EB2 : 0x3EB1; break;

                    // Swapped compared to original:
                    // Original East: Starboard ? 0x3E85 : 0x3E8A
                    // New East:      Starboard ? 0x3E8A : 0x3E85
                    case Direction.East: ItemID = Starboard ? 0x3E8A : 0x3E85; break;

                    case Direction.South: ItemID = Starboard ? 0x3EB1 : 0x3EB2; break;

                    // Swapped compared to original:
                    // Original West: Starboard ? 0x3E8A : 0x3E85
                    // New West:      Starboard ? 0x3E85 : 0x3E8A
                    case Direction.West: ItemID = Starboard ? 0x3E85 : 0x3E8A; break;
                }
            }
        }

        public void Open()
        {
            if (IsOpen || Deleted)
                return;

            if (m_CloseTimer != null)
                m_CloseTimer.Stop();

            m_CloseTimer = new CloseTimer(this);
            m_CloseTimer.Start();

            switch (ItemID)
            {
                case 0x3EB1: ItemID = 0x3ED5; break;
                case 0x3E8A: ItemID = 0x3E89; break;
                case 0x3EB2: ItemID = 0x3ED4; break;
                case 0x3E85: ItemID = 0x3E84; break;
            }

            if (m_Boat != null)
                m_Boat.Refresh();
        }

        public override bool OnMoveOver(Mobile from)
        {
            if (IsOpen)
            {
                if (from is BaseFactionGuard)
                    return false;

                if ((from.Direction & Direction.Running) != 0 || (m_Boat != null && !m_Boat.Contains(from)))
                    return true;

                Map map = Map;

                if (map == null)
                    return false;

                switch (SearchDisembark(from, map, ItemID, out Point3D loc))
                {
                    case DisembarkStep.Relocate:
                        from.Location = loc;
                        return false;

                    case DisembarkStep.RelocateTravelRestricted:
                        // wea: 13/Mar/2007 : Boat access exploit/bug fix
                        //  the travel check is asked again here only for its message (it fails); they are still moved
                        Server.Spells.SpellHelper.CheckTravel(from, map, loc, Server.Spells.TravelCheckType.TeleportTo);
                        from.Location = loc;
                        return false;

                    default:    // StepOff (walk on), StayOnPlank, None
                        return true;
                }
            }
            else
                return false;
        }

        // what walking over an open plank does for a mobile aboard (OnMoveOver), as a search
        private enum DisembarkStep
        {
            None,                       // no legal spot within 6 tiles: they stay on the plank
            StepOff,                    // a legal spot right beside the plank, about level: they walk off normally
            Relocate,                   // a legal spot further out: they are moved to it
            RelocateTravelRestricted,   // the average-Z spot further out, where teleport travel is restricted: moved
            StayOnPlank,                // the average-Z spot further out, where teleport travel is allowed: not moved (PARITY)
        }

        // the disembark search OnMoveOver carries out, side-effect free: outward from the plank in the direction
        //  it opens (itemID: its open ItemID), 1 to 6 tiles, at their Z -8..+8 and then at the average Z.
        //  PARITY: the 2007 travel check (weaver, Boat access exploit/bug fix) reads inverted: an average-Z spot
        //  further out is used only where teleport travel is *restricted*; where it is allowed they stay on the plank.
        //  Kept as it plays; FindDisembarkLocation reports what actually happens.
        private DisembarkStep SearchDisembark(Mobile from, Map map, int itemID, out Point3D loc)
        {
            int rx = 0, ry = 0;

            if (itemID == 0x3ED4)
                rx = 1;
            else if (itemID == 0x3ED5)
                rx = -1;
            else if (itemID == 0x3E84)
                ry = 1;
            else if (itemID == 0x3E89)
                ry = -1;

            for (int i = 1; i <= 6; ++i)
            {
                int x = X + (i * rx);
                int y = Y + (i * ry);
                int z;

                for (int j = -8; j <= 8; ++j)
                {
                    z = from.Z + j;

                    if (AllowDisembark(from, new Point3D(x, y, z), map))
                    {
                        loc = new Point3D(x, y, z);
                        return (i == 1 && j >= -2 && j <= 2) ? DisembarkStep.StepOff : DisembarkStep.Relocate;
                    }
                }

                z = map.GetAverageZ(x, y);

                if (AllowDisembark(from, new Point3D(x, y, z), map))
                {
                    loc = new Point3D(x, y, z);

                    if (i == 1)
                        return DisembarkStep.StepOff;

                    // the message-free form of OnMoveOver's travel check
                    if (Server.Spells.SpellHelper.CheckTravel(map, loc, Server.Spells.TravelCheckType.TeleportTo, from))
                        return DisembarkStep.StayOnPlank;

                    return DisembarkStep.RelocateTravelRestricted;
                }
            }

            loc = Point3D.Zero;
            return DisembarkStep.None;
        }

        // Side-effect free: where walking over this plank would put a mobile aboard, if it were open (they can
        //  open it: anyone aboard may, key or no key). The same search OnMoveOver carries out (SearchDisembark), so
        //  the two cannot disagree. False (and Point3D.Zero) when they would stay on the plank.
        //  Not considered: whether they can walk to the plank across the deck, and running (which skips the search).
        public bool FindDisembarkLocation(Mobile from, out Point3D loc)
        {
            loc = Point3D.Zero;

            Map map = Map;

            if (map == null || map == Map.Internal || Deleted)
                return false;

            DisembarkStep step = SearchDisembark(from, map, OpenItemID, out loc);

            if (step == DisembarkStep.StepOff || step == DisembarkStep.Relocate || step == DisembarkStep.RelocateTravelRestricted)
                return true;

            loc = Point3D.Zero;
            return false;
        }

        // the ItemID this plank has when open (Open's mapping)
        private int OpenItemID
        {
            get
            {
                switch (ItemID)
                {
                    case 0x3EB1: return 0x3ED5;
                    case 0x3E8A: return 0x3E89;
                    case 0x3EB2: return 0x3ED4;
                    case 0x3E85: return 0x3E84;
                    default: return ItemID;  // open already
                }
            }
        }

        private static bool AllowDisembark(Mobile m, Point3D p, Map map)
        {
            if (!Utility.CanFit(map, p.X, p.Y, p.Z, 16, Utility.CanFitFlags.requireSurface))
                return false;

            if (Server.Spells.SpellHelper.CheckMulti(p, map))
                return false;

            if (Region.Find(p, map).IsPartOf(typeof(Factions.StrongholdRegion)))
                return false;

            #region World Zone
            if (WorldZone.BlockTeleport(m, p, map))
                return false;
            #endregion

            return true;
        }

        public bool CanClose()
        {
            Map map = Map;

            if (map == null || Deleted)
                return false;

            IPooledEnumerable eable = this.GetObjectsInRange(0);
            foreach (object o in eable)
            {
                if (o != this)
                {
                    eable.Free();
                    return false;
                }
            }
            eable.Free();

            return true;
        }

        public void Close()
        {
            if (!IsOpen || !CanClose() || Deleted)
                return;

            if (m_CloseTimer != null)
                m_CloseTimer.Stop();

            m_CloseTimer = null;

            switch (ItemID)
            {
                case 0x3ED5: ItemID = 0x3EB1; break;
                case 0x3E89: ItemID = 0x3E8A; break;
                case 0x3ED4: ItemID = 0x3EB2; break;
                case 0x3E84: ItemID = 0x3E85; break;
            }

            if (m_Boat != null)
                m_Boat.Refresh();
        }

        public override void OnDoubleClickDead(Mobile from)
        {
            OnDoubleClick(from);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Boat == null)
                return;

            if (from.InRange(GetWorldLocation(), 8))
            {
                if (m_Boat.Contains(from))
                {
                    if (IsOpen)
                        Close();
                    else
                        Open();
                }
                else
                {
                    if (!IsOpen)
                    {
                        if (!Locked)
                        {
                            Open();
                        }
                        else if (from.AccessLevel >= AccessLevel.GameMaster)
                        {
                            from.LocalOverheadMessage(Network.MessageType.Regular, 0x00, 502502); // That is locked but your godly powers allow access
                            Open();
                        }
                        else
                        {
                            from.LocalOverheadMessage(Network.MessageType.Regular, 0x00, 502503); // That is locked.
                        }
                    }
                    else if (!Locked)
                    {
                        bool afraidOfBoats = (from is BaseCreature bc) && bc.OnBoatTravel() == false ? true : false;
                        if (afraidOfBoats == false)
                            from.Location = new Point3D(this.X, this.Y, this.Z + 3);
                    }
                    else if (from.AccessLevel >= AccessLevel.GameMaster)
                    {
                        from.LocalOverheadMessage(Network.MessageType.Regular, 0x00, 502502); // That is locked but your godly powers allow access
                        from.Location = new Point3D(this.X, this.Y, this.Z + 3);
                    }
                    else
                    {
                        from.LocalOverheadMessage(Network.MessageType.Regular, 0x00, 502503); // That is locked.
                    }
                }
            }
        }

        private class CloseTimer : Timer
        {
            private Plank m_Plank;

            public CloseTimer(Plank plank)
                : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
            {
                m_Plank = plank;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                m_Plank.Close();
            }
        }
    }
}