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

/* Scripts/Multis/HouseFoundation.cs
 * CHANGELOG
 * 6/5/22, Yoar
 *      Cleaned up DesignState serialization.
 *      No longer supporting 'm_Design == null' or 'm_Backup == null'.
 * 12/29/2021, Adam (GetBalance() GetCombinedBalance())
 *      Replace calls to GetBalance() with GetCombinedBalance() so as to support the new shared bankbox system
 *	9/17/21, Yoar
 *		Added IDesignState interface to support design states on any type of multi
 *	5/11/08, Adam
 *		Performance Conversion: Regions now use HashTables instead of ArrayLists
 *  12/17/07, Adam
 *      Add the 'new' keyword to Initialize() so as to not hide the one in BaseHouse
 *      i.e., new public static void Initialize()
 *	8/25/07, Adam
 *		make m_Signpost m_SignpostGraphic protected so we can set them from StaticHouse
 *  06/09/07, plasma
 *    - Fixed CompressionThread() problem properly.
 *  06/08/07, plasma
 *    - Changed design state scopes to protected
 *    - Prevent excepition occuring in StaticHouse construction within CompressionThread()
 *  5/1`0/07, Adam
 *      - Add CoreSEOverride to provide roof functionality for access level < GM
 *	5/9/04, Pix
 *		Partial RunUO2.0 merge... for roofs.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Server.Multis
{
    public enum FoundationType
    {
        Stone,
        DarkWood,
        LightWood,
        Dungeon,
        Brick
    }

    /* To insert a DesignState into a multi:
	 * 1. Implement the IDesignState interface
	 * 2. Override SendInfoTo to send general info
	*/
    public interface IDesignState
    {
        Serial Serial { get; } // Serial of the BaseMulti.
        int LastRevision { get; set; } // Latest revision number.
        void SendDesignGeneral(NetState state); // Send design general packet to observer from.
        void SendDesignDetails(NetState state); // Send design details packet to observer from.
    }

    public class HouseFoundation : BaseHouse, IDesignState
    {
        private DesignState m_Current;              // State which is currently visible.
        private DesignState m_Design;               // State of current design.
        private DesignState m_Backup;               // State at last user backup.
        private Item m_SignHanger;                  // Item hanging the sign.
        private Item m_Signpost;                    // Item supporting the hanger.
        private int m_SignpostGraphic;              // ItemID number of the chosen signpost.
        private int m_LastRevision;                 // Latest revision number.
        private ArrayList m_Fixtures;               // List of fixtures (teleporters and doors) associated with this house.
        private FoundationType m_Type;              // Graphic type of this foundation.
        private Mobile m_Customizer;                // Who is currently customizing this -or- null if not customizing.

        public FoundationType Type { get { return m_Type; } set { m_Type = value; } }
        public int LastRevision { get { return m_LastRevision; } set { m_LastRevision = value; } }
        public ArrayList Fixtures { get { return m_Fixtures; } }
        public Item SignHanger { get { return m_SignHanger; } }
        public Item Signpost { get { return m_Signpost; } }
        public int SignpostGraphic { get { return m_SignpostGraphic; } set { m_SignpostGraphic = value; } }
        public Mobile Customizer { get { return m_Customizer; } set { m_Customizer = value; } }

        public override bool IsAosRules
        {
            get
            {
                return true;
            }
        }

        public bool IsFixture(Item item)
        {
            return (m_Fixtures != null && m_Fixtures.Contains(item));
        }

        public override MultiComponentList Components
        {
            get
            {
                if (m_Current == null)
                    SetInitialState();

                return m_Current.Components;
            }
        }

        public override int GetMaxUpdateRange()
        {
            return 24;
        }

        public override int GetUpdateRange(Mobile m)
        {
            return GetDefaultUpdateRange(CurrentState.Components);
        }

        public static int GetDefaultUpdateRange(MultiComponentList mcl)
        {
            int w = mcl.Width;
            int h = mcl.Height - 1;
            int v = 18 + ((w > h ? w : h) / 2);

            if (v > 24)
                v = 24;
            else if (v < 18)
                v = 18;

            return v;
        }

        public DesignState CurrentState
        {
            get { if (m_Current == null) SetInitialState(); return m_Current; }
            set { m_Current = value; }
        }

        public DesignState DesignState
        {
            get { if (m_Design == null) SetInitialState(); return m_Design; }
            set { m_Design = value; }
        }

        public DesignState BackupState
        {
            get { if (m_Backup == null) SetInitialState(); return m_Backup; }
            set { m_Backup = value; }
        }

        public void SetInitialState()
        {
            // This is a new house, it has not yet loaded a design state
            m_Current = new DesignState(this, GetEmptyFoundation());
            m_Design = new DesignState(m_Current);
            m_Backup = new DesignState(m_Current);
        }

        protected override void AddFixedItems(HashSet<Item> list)
        {
            base.AddFixedItems(list);

            if (m_SignHanger != null && !m_SignHanger.Deleted)
                list.Add(m_SignHanger);

            if (m_Signpost != null && !m_Signpost.Deleted)
                list.Add(m_Signpost);

            if (m_Fixtures != null)
            {
                foreach (Item item in m_Fixtures)
                    list.Add(item);
            }
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_SignHanger != null)
                m_SignHanger.Delete();

            if (m_Signpost != null)
                m_Signpost.Delete();

            if (m_Fixtures == null)
                return;

            for (int i = 0; i < m_Fixtures.Count; ++i)
            {
                Item item = (Item)m_Fixtures[i];

                if (item != null)
                    item.Delete();
            }

            m_Fixtures.Clear();
        }

        public void ClearFixtures(Mobile from)
        {
            if (m_Fixtures == null)
                return;

            RemoveKeys(from);

            for (int i = 0; i < m_Fixtures.Count; ++i)
            {
                ((Item)m_Fixtures[i]).Delete();
                Doors.Remove(m_Fixtures[i]);
            }

            m_Fixtures.Clear();
        }

        public void AddFixtures(Mobile from, MultiTileEntry[] list)
        {
            if (m_Fixtures == null)
                m_Fixtures = new ArrayList();

            uint keyValue = 0;

            for (int i = 0; i < list.Length; ++i)
            {
                MultiTileEntry mte = list[i];
                int itemID = mte.m_ItemID & 0x3FFF;

                if (itemID >= 0x181D && itemID < 0x1829)
                {
                    HouseTeleporter tp = new HouseTeleporter(itemID);

                    AddFixture(tp, mte);
                }
                else
                {
                    BaseDoor door = null;

                    if (itemID >= 0x675 && itemID < 0x6F5)
                    {
                        int type = (itemID - 0x675) / 16;
                        DoorFacing facing = (DoorFacing)(((itemID - 0x675) / 2) % 8);

                        switch (type)
                        {
                            case 0: door = new GenericHouseDoor(facing, 0x675, 0xEC, 0xF3); break;
                            case 1: door = new GenericHouseDoor(facing, 0x685, 0xEC, 0xF3); break;
                            case 2: door = new GenericHouseDoor(facing, 0x695, 0xEB, 0xF2); break;
                            case 3: door = new GenericHouseDoor(facing, 0x6A5, 0xEA, 0xF1); break;
                            case 4: door = new GenericHouseDoor(facing, 0x6B5, 0xEA, 0xF1); break;
                            case 5: door = new GenericHouseDoor(facing, 0x6C5, 0xEC, 0xF3); break;
                            case 6: door = new GenericHouseDoor(facing, 0x6D5, 0xEA, 0xF1); break;
                            case 7: door = new GenericHouseDoor(facing, 0x6E5, 0xEA, 0xF1); break;
                        }
                    }
                    else if (itemID >= 0x314 && itemID < 0x364)
                    {
                        int type = (itemID - 0x314) / 16;
                        DoorFacing facing = (DoorFacing)(((itemID - 0x314) / 2) % 8);

                        switch (type)
                        {
                            case 0: door = new GenericHouseDoor(facing, 0x314, 0xED, 0xF4); break;
                            case 1: door = new GenericHouseDoor(facing, 0x324, 0xED, 0xF4); break;
                            case 2: door = new GenericHouseDoor(facing, 0x334, 0xED, 0xF4); break;
                            case 3: door = new GenericHouseDoor(facing, 0x344, 0xED, 0xF4); break;
                            case 4: door = new GenericHouseDoor(facing, 0x354, 0xED, 0xF4); break;
                        }
                    }
                    else if (itemID >= 0x824 && itemID < 0x834)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x824) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x824, 0xEC, 0xF3);
                    }
                    else if (itemID >= 0x839 && itemID < 0x849)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x839) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x839, 0xEB, 0xF2);
                    }
                    else if (itemID >= 0x84C && itemID < 0x85C)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x84C) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x84C, 0xEC, 0xF3);
                    }
                    else if (itemID >= 0x866 && itemID < 0x876)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x866) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x866, 0xEB, 0xF2);
                    }
                    else if (itemID >= 0xE8 && itemID < 0xF8)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0xE8) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0xE8, 0xED, 0xF4);
                    }
                    else if (itemID >= 0x1FED && itemID < 0x1FFD)
                    {
                        DoorFacing facing = (DoorFacing)(((itemID - 0x1FED) / 2) % 8);
                        door = new GenericHouseDoor(facing, 0x1FED, 0xEC, 0xF3);
                    }

                    if (door != null)
                    {
                        if (keyValue == 0)
                            keyValue = CreateKeys(from);

                        door.Locked = true;
                        door.KeyValue = keyValue;

                        AddDoor(door, mte.m_OffsetX, mte.m_OffsetY, mte.m_OffsetZ);
                        m_Fixtures.Add(door);
                    }
                }
            }

            for (int i = 0; i < m_Fixtures.Count; ++i)
            {
                Item fixture = (Item)m_Fixtures[i];

                if (fixture is HouseTeleporter)
                {
                    HouseTeleporter tp = (HouseTeleporter)fixture;

                    for (int j = 1; j <= m_Fixtures.Count; ++j)
                    {
                        HouseTeleporter check = m_Fixtures[(i + j) % m_Fixtures.Count] as HouseTeleporter;

                        if (check != null && check.ItemID == tp.ItemID)
                        {
                            tp.Target = check;
                            break;
                        }
                    }
                }
                else if (fixture is BaseHouseDoor)
                {
                    BaseHouseDoor door = (BaseHouseDoor)fixture;

                    if (door.Link != null)
                        continue;

                    DoorFacing linkFacing;
                    int xOffset, yOffset;

                    switch (door.Facing)
                    {
                        default:
                        case DoorFacing.WestCW: linkFacing = DoorFacing.EastCCW; xOffset = 1; yOffset = 0; break;
                        case DoorFacing.EastCCW: linkFacing = DoorFacing.WestCW; xOffset = -1; yOffset = 0; break;
                        case DoorFacing.WestCCW: linkFacing = DoorFacing.EastCW; xOffset = 1; yOffset = 0; break;
                        case DoorFacing.EastCW: linkFacing = DoorFacing.WestCCW; xOffset = -1; yOffset = 0; break;
                        case DoorFacing.SouthCW: linkFacing = DoorFacing.NorthCCW; xOffset = 0; yOffset = -1; break;
                        case DoorFacing.NorthCCW: linkFacing = DoorFacing.SouthCW; xOffset = 0; yOffset = 1; break;
                        case DoorFacing.SouthCCW: linkFacing = DoorFacing.NorthCW; xOffset = 0; yOffset = -1; break;
                        case DoorFacing.NorthCW: linkFacing = DoorFacing.SouthCCW; xOffset = 0; yOffset = 1; break;
                    }

                    for (int j = i + 1; j < m_Fixtures.Count; ++j)
                    {
                        BaseHouseDoor check = m_Fixtures[j] as BaseHouseDoor;

                        if (check != null && check.Link == null && check.Facing == linkFacing && (check.X - door.X) == xOffset && (check.Y - door.Y) == yOffset && (check.Z == door.Z))
                        {
                            check.Link = door;
                            door.Link = check;
                            break;
                        }
                    }
                }
            }
        }

        public void AddFixture(Item item, MultiTileEntry mte)
        {
            m_Fixtures.Add(item);
            item.MoveToWorld(new Point3D(X + mte.m_OffsetX, Y + mte.m_OffsetY, Z + mte.m_OffsetZ), Map);
        }

        public static void GetFoundationGraphics(FoundationType type, out int east, out int south, out int post, out int corner)
        {
            switch (type)
            {
                default:
                case FoundationType.DarkWood: corner = 0x0014; east = 0x0015; south = 0x0016; post = 0x0017; break;
                case FoundationType.LightWood: corner = 0x00BD; east = 0x00BE; south = 0x00BF; post = 0x00C0; break;
                case FoundationType.Dungeon: corner = 0x02FD; east = 0x02FF; south = 0x02FE; post = 0x0300; break;
                case FoundationType.Brick: corner = 0x0041; east = 0x0043; south = 0x0042; post = 0x0044; break;
                case FoundationType.Stone: corner = 0x0065; east = 0x0064; south = 0x0063; post = 0x0066; break;
            }
        }

        public static void ApplyFoundation(FoundationType type, MultiComponentList mcl)
        {
            int east, south, post, corner;

            GetFoundationGraphics(type, out east, out south, out post, out corner);

            int xCenter = mcl.Center.X;
            int yCenter = mcl.Center.Y;

            mcl.Add(post, 0 - xCenter, 0 - yCenter, 0);
            mcl.Add(corner, mcl.Width - 1 - xCenter, mcl.Height - 2 - yCenter, 0);

            for (int x = 1; x < mcl.Width; ++x)
            {
                mcl.Add(south, x - xCenter, 0 - yCenter, 0);

                if (x < mcl.Width - 1)
                    mcl.Add(south, x - xCenter, mcl.Height - 2 - yCenter, 0);
            }

            for (int y = 1; y < mcl.Height - 1; ++y)
            {
                mcl.Add(east, 0 - xCenter, y - yCenter, 0);

                if (y < mcl.Height - 2)
                    mcl.Add(east, mcl.Width - 1 - xCenter, y - yCenter, 0);
            }
        }

        public static void AddStairsTo(ref MultiComponentList mcl)
        {
            // copy the original..
            mcl = new MultiComponentList(mcl);

            mcl.Resize(mcl.Width, mcl.Height + 1);

            int xCenter = mcl.Center.X;
            int yCenter = mcl.Center.Y;
            int y = mcl.Height - 1;

            for (int x = 0; x < mcl.Width; ++x)
                mcl.Add(0x63, x - xCenter, y - yCenter, 0);
        }

        public MultiComponentList GetEmptyFoundation()
        {
            // Copy original foundation layout
            MultiComponentList mcl = new MultiComponentList(MultiData.GetComponents(ItemID));

            mcl.Resize(mcl.Width, mcl.Height + 1);

            int xCenter = mcl.Center.X;
            int yCenter = mcl.Center.Y;
            int y = mcl.Height - 1;

            ApplyFoundation(m_Type, mcl);

            for (int x = 1; x < mcl.Width; ++x)
                mcl.Add(0x751, x - xCenter, y - yCenter, 0);

            return mcl;
        }

        public override Rectangle2D[] Area
        {
            get
            {
                MultiComponentList mcl = Components;

                return new Rectangle2D[] { new Rectangle2D(mcl.Min.X, mcl.Min.Y, mcl.Width, mcl.Height) };
            }
        }

        public virtual void CheckSignHanger(int x, int y)
        {
            m_SignHanger = new Static(0xB98);
            m_SignHanger.MoveToWorld(new Point3D(X + x, Y + y, Z + 7), Map);
        }

        public virtual void CheckSignpost()
        {
            MultiComponentList mcl = this.Components;

            int x = mcl.Min.X;
            int y = mcl.Height - 2 - mcl.Center.Y;

            if (CheckWall(mcl, x, y))
            {
                if (m_Signpost != null)
                    m_Signpost.Delete();

                m_Signpost = null;
            }
            else if (m_Signpost == null)
            {
                m_Signpost = new Static(m_SignpostGraphic);
                m_Signpost.MoveToWorld(new Point3D(X + x, Y + y, Z + 7), Map);
            }
            else
            {
                m_Signpost.ItemID = m_SignpostGraphic;
                m_Signpost.MoveToWorld(new Point3D(X + x, Y + y, Z + 7), Map);
            }
        }

        public bool CheckWall(MultiComponentList mcl, int x, int y)
        {
            x += mcl.Center.X;
            y += mcl.Center.Y;

            if (x >= 0 && x < mcl.Width && y >= 0 && y < mcl.Height)
            {
                StaticTile[] tiles = mcl.Tiles[x][y];

                for (int i = 0; i < tiles.Length; ++i)
                {
                    StaticTile tile = tiles[i];

                    if (tile.Z == 7 && tile.Height == 20)
                        return true;
                }
            }

            return false;
        }

        public HouseFoundation(Mobile owner, int multiID, int maxLockdowns, int maxSecures, int maxLockCont)
            : base(multiID, owner, maxLockdowns, maxSecures, maxLockCont)
        {
            m_SignpostGraphic = 9;

            m_Fixtures = new ArrayList();

            int x = Components.Min.X;
            int y = Components.Height - 1 - Components.Center.Y;

            CheckSignHanger(x, y);

            CheckSignpost();

            SetSign(x, y, 7);

            BanLocation = new Point3D(x, y, 0);
        }

        public HouseFoundation(Serial serial)
            : base(serial)
        {
        }

        public void BeginCustomize(Mobile m)
        {
            if (!m.CheckAlive())
                return;

            DesignContext.Add(m, this);
            m.Send(new BeginHouseCustomization(this));

            if (m.NetState != null)
                SendInfoTo(m.NetState);

            DesignState.SendDetailedInfoTo(m.NetState);

            ArrayList list = new ArrayList(Region.Mobiles.Values);

            foreach (Mobile rem in list)
            {
                if (!(rem is PlayerVendor) && !(rem is PlayerBarkeeper) && rem != m && IsInside(rem))
                    rem.Location = BanLocation;
            }
        }

        public override void SendInfoTo(NetState state)
        {
            base.SendInfoTo(state);

            SendDesignGeneral(state);
        }

        public override void Serialize(GenericWriter writer)
        {
            int version = 7;
            writer.Write(version); // version

            writer.Write(m_Signpost);
            writer.Write((int)m_SignpostGraphic);

            writer.Write((int)m_Type);

            writer.Write(m_SignHanger);

            writer.Write((int)m_LastRevision);
            writer.WriteItemList(m_Fixtures, true);

            CurrentState.Serialize(writer);
            DesignState.Serialize(writer);
            BackupState.Serialize(writer);

            base.Serialize(writer);
        }

        private int m_DefaultPrice;
        public override int DefaultPrice { get { return m_DefaultPrice; } set { m_DefaultPrice = value; } }

        public override void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            // Yoar: In version 6, it was possible for instances of StaticHouse to have m_Design == null
            // and m_Backup == null. If either the m_Design or the m_Backup was null, they weren't
            // serialized. However, having the m_Design or the m_Backup be null complicates things...
            // For one, it may trigger the SetInitialState method. It's much easier if we set m_Design
            // and m_Backup to empty plots. We'll do this again in version 7.
            int designFlags = 0x3;

            switch (version)
            {
                case 7:
                case 6:
                    {
                        if (version < 7)
                            designFlags = reader.ReadInt();
                        goto case 5;
                    }
                case 5:
                case 4:
                    {
                        m_Signpost = reader.ReadItem();
                        m_SignpostGraphic = reader.ReadInt();
                        goto case 3;
                    }
                case 3:
                    {
                        m_Type = (FoundationType)reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_SignHanger = reader.ReadItem();
                        goto case 1;
                    }
                case 1:
                    {
                        if (version < 5)
                            m_DefaultPrice = reader.ReadInt();

                        goto case 0;
                    }
                case 0:
                    {
                        if (version < 3)
                            m_Type = FoundationType.Stone;

                        if (version < 4)
                            m_SignpostGraphic = 9;

                        m_LastRevision = reader.ReadInt();
                        m_Fixtures = reader.ReadItemList();

                        m_Current = new DesignState(this, reader);

                        if ((designFlags & 0x1) != 0)
                            m_Design = new DesignState(this, reader);

                        if ((designFlags & 0x2) != 0)
                            m_Backup = new DesignState(this, reader);

                        break;
                    }
            }

            // Yoar: Ensure that m_Design and m_Backup aren't null.
            if (m_Design == null)
                m_Design = new DesignState(this, GetEmptyFoundation());
            if (m_Backup == null)
                m_Backup = new DesignState(this, GetEmptyFoundation());

            base.Deserialize(reader);
        }

        public bool IsHiddenToCustomizer(Item item)
        {
            return (item == m_Signpost || (m_Fixtures != null && m_Fixtures.Contains(item)));
        }

        new public static void Initialize()
        {
            PacketHandlers.RegisterExtended(0x1E, true, new OnPacketReceive(QueryDesignDetails));

            PacketHandlers.RegisterEncoded(0x02, true, new OnEncodedPacketReceive(Designer_Backup));
            PacketHandlers.RegisterEncoded(0x03, true, new OnEncodedPacketReceive(Designer_Restore));
            PacketHandlers.RegisterEncoded(0x04, true, new OnEncodedPacketReceive(Designer_Commit));
            PacketHandlers.RegisterEncoded(0x05, true, new OnEncodedPacketReceive(Designer_Delete));
            PacketHandlers.RegisterEncoded(0x06, true, new OnEncodedPacketReceive(Designer_Build));
            PacketHandlers.RegisterEncoded(0x0C, true, new OnEncodedPacketReceive(Designer_Close));
            PacketHandlers.RegisterEncoded(0x0D, true, new OnEncodedPacketReceive(Designer_Stairs));
            PacketHandlers.RegisterEncoded(0x0E, true, new OnEncodedPacketReceive(Designer_Sync));
            PacketHandlers.RegisterEncoded(0x10, true, new OnEncodedPacketReceive(Designer_Clear));
            PacketHandlers.RegisterEncoded(0x12, true, new OnEncodedPacketReceive(Designer_Level));
            PacketHandlers.RegisterEncoded(0x13, true, new OnEncodedPacketReceive(Designer_Roof)); // Samurai Empire roof
            PacketHandlers.RegisterEncoded(0x14, true, new OnEncodedPacketReceive(Designer_RoofDelete)); // Samurai Empire roof
            PacketHandlers.RegisterEncoded(0x1A, true, new OnEncodedPacketReceive(Designer_Revert));

            CommandSystem.Register("DesignInsert", AccessLevel.GameMaster, new CommandEventHandler(DesignInsert_OnCommand));

            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
        }

        private static void EventSink_Speech(SpeechEventArgs e)
        {
            if (DesignContext.Find(e.Mobile) != null)
            {
                e.Mobile.SendLocalizedMessage(1061925); // You cannot speak while customizing your house.
                e.Blocked = true;
            }
        }

        public static void Designer_Sync(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client requested state synchronization
				 *  - Resend full house state
				 */

                DesignState design = context.Foundation.DesignState;

                // Resend full house state
                //design.SendDetailedInfoTo( state );
            }
        }

        public static void Designer_Clear(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client chose to clear the design
				 *  - Restore empty foundation
				 *     - Construct new design state from empty foundation
				 *     - Assign constructed state to foundation
				 *  - Update revision
				 *  - Update client with new state
				 */

                // Restore empty foundation : Construct new design state from empty foundation
                DesignState newDesign = new DesignState(context.Foundation, context.Foundation.GetEmptyFoundation());

                // Restore empty foundation : Assign constructed state to foundation
                context.Foundation.DesignState = newDesign;

                // Update revision
                newDesign.OnRevised();

                // Update client with new state
                context.Foundation.SendInfoTo(state);
                newDesign.SendDetailedInfoTo(state);
            }
        }

        public static void Designer_Restore(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client chose to restore design to the last backup state
				 *  - Restore backup
				 *     - Construct new design state from backup state
				 *     - Assign constructed state to foundation
				 *  - Update revision
				 *  - Update client with new state
				 */

                // Restore backup : Construct new design state from backup state
                DesignState backupDesign = new DesignState(context.Foundation.BackupState);

                // Restore backup : Assign constructed state to foundation
                context.Foundation.DesignState = backupDesign;

                // Update revision;
                backupDesign.OnRevised();

                // Update client with new state
                context.Foundation.SendInfoTo(state);
                backupDesign.SendDetailedInfoTo(state);
            }
        }

        public static void Designer_Backup(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client chose to backup design state
				 *  - Construct a copy of the current design state
				 *  - Assign constructed state to backup state field
				 */

                // Construct a copy of the current design state
                DesignState copyState = new DesignState(context.Foundation.DesignState);

                // Assign constructed state to backup state field
                context.Foundation.BackupState = copyState;
            }
        }

        public static void Designer_Revert(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client chose to revert design state to currently visible state
				 *  - Revert design state
				 *     - Construct a copy of the current visible state
				 *     - Freeze fixtures in constructed state
				 *     - Assign constructed state to foundation
				 *     - If a signpost is needed, add it
				 *  - Update revision
				 *  - Update client with new state
				 */

                // Revert design state : Construct a copy of the current visible state
                DesignState copyState = new DesignState(context.Foundation.CurrentState);

                // Revert design state : Freeze fixtures in constructed state
                copyState.FreezeFixtures();

                // Revert design state : Assign constructed state to foundation
                context.Foundation.DesignState = copyState;

                // Revert design state : If a signpost is needed, add it
                context.Foundation.CheckSignpost();

                // Update revision
                copyState.OnRevised();

                // Update client with new state
                context.Foundation.SendInfoTo(state);
                copyState.SendDetailedInfoTo(state);
            }
        }

        public void EndConfirmCommit(Mobile from, bool fromCommand = false)
        {
            int oldPrice = Price;
            int newPrice = oldPrice + 10000 + ((DesignState.Components.List.Length - CurrentState.Components.List.Length) * 500);
            int cost = newPrice - oldPrice;

            // no cost if we've used the [DesignCommit command
            if (fromCommand && from.AccessLevel >= AccessLevel.GameMaster)
                cost = 0;

            if (cost > 0)
            {
                if (Banker.CombinedWithdrawFromAllEnrolled(from, cost))
                {
                    from.SendLocalizedMessage(1060398, cost.ToString()); // ~1_AMOUNT~ gold has been withdrawn from your bank box.
                }
                else
                {
                    from.SendLocalizedMessage(1061903); // You cannot commit this house design, because you do not have the necessary funds in your bank box to pay for the upgrade.  Please back up your design, obtain the required funds, and commit your design again.
                    return;
                }
            }
            else if (cost < 0)
            {
                if (Banker.Deposit(from, -cost))
                    from.SendLocalizedMessage(1060397, (-cost).ToString()); // ~1_AMOUNT~ gold has been deposited into your bank box.
                else
                    return;
            }

            /* Client chose to commit current design state
				 *  - Commit design state
				 *     - Construct a copy of the current design state
				 *     - Clear visible fixtures
				 *     - Melt fixtures from constructed state
				 *     - Add melted fixtures from constructed state
				 *     - Assign constructed state to foundation
				 *  - Update house price
				 *  - Remove design context
				 *  - Notify the client that customization has ended
				 *  - Notify the core that the foundation has changed and should be resent to all clients
				 *  - Eject client from house
				 *  - If a signpost is needed, add it
				 */

            // Commit design state : Construct a copy of the current design state
            DesignState copyState = new DesignState(DesignState);

            // Commit design state : Clear visible fixtures
            ClearFixtures(from);

            // Commit design state : Melt fixtures from constructed state
            copyState.MeltFixtures();

            // Commit design state : Add melted fixtures from constructed state
            AddFixtures(from, copyState.Fixtures);

            // Commit design state : Assign constructed state to foundation
            CurrentState = copyState;

            // Update house price
            Price = newPrice - 10000;

            // Remove design context
            DesignContext.Remove(from);

            // Notify the client that customization has ended
            from.Send(new EndHouseCustomization(this));

            // Notify the core that the foundation has changed and should be resent to all clients
            Delta(ItemDelta.Update);
            ProcessDelta();
            CurrentState.SendDetailedInfoTo(from.NetState);

            // Eject client from house
            from.RevealingAction();

            from.MoveToWorld(BanLocation, Map);

            // If a signpost is needed, add it
            CheckSignpost();
        }

        public static void Designer_Commit(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                int oldPrice = context.Foundation.Price;
                int newPrice = oldPrice + 10000 + ((context.Foundation.DesignState.Components.List.Length - context.Foundation.CurrentState.Components.List.Length) * 500);
                int bankBalance = Banker.GetAccessibleBalance(from);

                from.SendGump(new ConfirmCommitGump(from, context.Foundation, bankBalance, oldPrice, newPrice));
            }
        }

        public static bool IsSignHanger(int itemID)
        {
            itemID &= 0x3FFF;

            return (itemID >= 0xB97 && itemID < 0xBA3);
        }

        public int MaxLevels
        {
            get
            {
                MultiComponentList mcl = this.Components;

                if (mcl.Width >= 14 || mcl.Height >= 14)
                    return 4;
                else
                    return 3;
            }
        }

        public static int GetLevelZ(int level)
        {
            switch (level)
            {
                default:
                case 1: return 07;
                case 2: return 27;
                case 3: return 47;
                case 4: return 67;
            }
        }

        public static int GetLevelZ(int level, HouseFoundation house)
        {
            if (level < 1 || level > house.MaxLevels)
                level = 1;

            return (level - 1) * 20 + 7;

            /*
			switch( level )
			{
				default:
				case 1: return 07;
				case 2: return 27;
				case 3: return 47;
				case 4: return 67;
			}
			 * */
        }

        public static int GetZLevel(int z, HouseFoundation house)
        {
            int level = (z - 7) / 20 + 1;

            if (level < 1 || level > house.MaxLevels)
                level = 1;

            return level;
        }

        private static ComponentVerification m_Verification;

        public static ComponentVerification Verification
        {
            get
            {
                if (m_Verification == null)
                    m_Verification = new ComponentVerification();

                return m_Verification;
            }
        }

        public static bool ValidPiece(int itemID)
        {
            return ValidPiece(itemID, false);
        }

        public static bool ValidPiece(int itemID, bool roof)
        {
            itemID &= 0x3FFF;

            //Adam: we do not have the SE expansion which probably marks roof tiles as 'TileFlag.Roof'
            //  but we still want to allow them. Presumably users using the Custom Housing tool cannot modify the available tiles
            //  so we should be safe.
            //  search for 'CoreSEOverride' in one other location in this file;
            bool CoreSEOverride = true;

            if (!roof && (TileData.ItemTable[itemID & 0x3FFF].Flags & TileFlag.Roof) != 0)
                return false;
            else if (!CoreSEOverride && !roof && (TileData.ItemTable[itemID & 0x3FFF].Flags & TileFlag.Roof) == 0)
                return false;

            return Verification.IsItemValid(itemID);
        }

        private static int[] m_BlockIDs = new int[]
            {
                0x3EE, 0x709, 0x71E, 0x721,
                0x738, 0x750, 0x76C, 0x788,
                0x7A3, 0x7BA
            };

        private static int[] m_StairSeqs = new int[]
            {
                0x3EF, 0x70A, 0x722, 0x739,
                0x751, 0x76D, 0x789, 0x7A4
            };

        private static int[] m_StairIDs = new int[]
            {
                0x71F, 0x736, 0x737, 0x749,
                0x7BB, 0x7BC
            };

        public static bool IsStairBlock(int id)
        {
            id &= 0x3FFF;
            int delta = -1;

            for (int i = 0; delta < 0 && i < m_BlockIDs.Length; ++i)
                delta = (m_BlockIDs[i] - id);

            return (delta == 0);
        }

        public static bool IsStair(int id, ref int dir)
        {
            id &= 0x3FFF;
            int delta = -4;

            for (int i = 0; delta < -3 && i < m_StairSeqs.Length; ++i)
                delta = (m_StairSeqs[i] - id);

            if (delta >= -3 && delta <= 0)
            {
                dir = -delta;
                return true;
            }

            delta = -1;

            for (int i = 0; delta < 0 && i < m_StairIDs.Length; ++i)
            {
                delta = (m_StairIDs[i] - id);
                dir = i % 4;
            }

            return (delta == 0);
        }

        public static bool DeleteStairs(MultiComponentList mcl, int id, int x, int y, int z)
        {
            int ax = x + mcl.Center.X;
            int ay = y + mcl.Center.Y;

            if (ax < 0 || ay < 0 || ax >= mcl.Width || ay >= (mcl.Height - 1) || z < 7 || ((z - 7) % 5) != 0)
                return false;

            if (IsStairBlock(id))
            {
                StaticTile[] tiles = mcl.Tiles[ax][ay];

                for (int i = 0; i < tiles.Length; ++i)
                {
                    StaticTile tile = tiles[i];

                    if (tile.Z == (z + 5))
                    {
                        id = tile.ID;
                        z = tile.Z;

                        if (!IsStairBlock(id))
                            break;
                    }
                }
            }

            int dir = 0;

            if (!IsStair(id, ref dir))
                return false;

            int height = ((z - 7) % 20) / 5;

            int xStart, yStart;
            int xInc, yInc;

            switch (dir)
            {
                default:
                case 0: // North
                    {
                        xStart = x;
                        yStart = y + height;
                        xInc = 0;
                        yInc = -1;
                        break;
                    }
                case 1: // West
                    {
                        xStart = x + height;
                        yStart = y;
                        xInc = -1;
                        yInc = 0;
                        break;
                    }
                case 2: // South
                    {
                        xStart = x;
                        yStart = y - height;
                        xInc = 0;
                        yInc = 1;
                        break;
                    }
                case 3: // East
                    {
                        xStart = x - height;
                        yStart = y;
                        xInc = 1;
                        yInc = 0;
                        break;
                    }
            }

            int zStart = z - (height * 5);

            for (int i = 0; i < 4; ++i)
            {
                x = xStart + (i * xInc);
                y = yStart + (i * yInc);

                for (int j = 0; j <= i; ++j)
                    mcl.RemoveXYZH(x, y, zStart + (j * 5), 5);

                ax = x + mcl.Center.X;
                ay = y + mcl.Center.Y;

                if (ax >= 1 && ax < mcl.Width && ay >= 1 && ay < mcl.Height - 1)
                {
                    StaticTile[] tiles = mcl.Tiles[ax][ay];

                    bool hasBaseFloor = false;

                    for (int j = 0; !hasBaseFloor && j < tiles.Length; ++j)
                        hasBaseFloor = (tiles[j].Z == 7 && (tiles[j].ID & 0x3FFF) != 1);

                    if (!hasBaseFloor)
                        mcl.Add(0x31F4, x, y, 7);
                }
            }

            return true;
        }

        public static void Designer_Delete(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client chose to delete a component
				 *  - Read data detailing which component to delete
				 *  - Verify component is deletable
				 *  - Remove the component
				 *  - If needed, replace removed component with a dirt tile
				 *  - Update revision
				 */

                // Read data detailing which component to delete
                int itemID = pvSrc.ReadInt32();
                int x = pvSrc.ReadInt32();
                int y = pvSrc.ReadInt32();
                int z = pvSrc.ReadInt32();

                // Verify component is deletable
                DesignState design = context.Foundation.DesignState;
                MultiComponentList mcl = design.Components;

                int ax = x + mcl.Center.X;
                int ay = y + mcl.Center.Y;

                if (IsSignHanger(itemID) || (z == 0 && ax >= 0 && ax < mcl.Width && ay >= 0 && ay < (mcl.Height - 1)))
                {
                    /* Component is not deletable
					 *  - Resend design state
					 *  - Return without further processing
					 */

                    design.SendDetailedInfoTo(state);
                    return;
                }

                // Remove the component
                if (!DeleteStairs(mcl, itemID, x, y, z))
                    mcl.Remove(itemID, x, y, z);

                // If needed, replace removed component with a dirt tile
                if (ax >= 1 && ax < mcl.Width && ay >= 1 && ay < mcl.Height - 1)
                {
                    StaticTile[] tiles = mcl.Tiles[ax][ay];

                    bool hasBaseFloor = false;

                    for (int i = 0; !hasBaseFloor && i < tiles.Length; ++i)
                        hasBaseFloor = (tiles[i].Z == 7 && (tiles[i].ID & 0x3FFF) != 1);

                    if (!hasBaseFloor)
                    {
                        // Replace with a dirt tile
                        mcl.Add(0x31F4, x, y, 7);
                    }
                }

                // Update revision
                design.OnRevised();
            }
        }

        public static void Designer_Stairs(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client chose to add stairs
				 *  - Read data detailing stair type and location
				 *  - Validate stair multi ID
				 *  - Add the stairs
				 *     - Load data describing the stair components
				 *     - Insert described components
				 *  - Update revision
				 */

                // Read data detailing stair type and location
                int itemID = pvSrc.ReadInt32();
                int x = pvSrc.ReadInt32();
                int y = pvSrc.ReadInt32();

                // Validate stair multi ID
                DesignState design = context.Foundation.DesignState;

                if (itemID < 0x1DB0 || itemID > 0x1DD7)
                {
                    /* Specified multi ID is not a stair
					 *  - Resend design state
					 *  - Return without further processing
					 */

                    design.SendDetailedInfoTo(state);
                    return;
                }

                // Add the stairs
                MultiComponentList mcl = design.Components;

                // Add the stairs : Load data describing stair components
                MultiComponentList stairs = MultiData.GetComponents(itemID);

                // Add the stairs : Insert described components
                int z = GetLevelZ(context.Level);

                for (int i = 0; i < stairs.List.Length; ++i)
                {
                    MultiTileEntry entry = stairs.List[i];

                    if ((entry.m_ItemID & 0x3FFF) != 1)
                        mcl.Add(entry.m_ItemID, x + entry.m_OffsetX, y + entry.m_OffsetY, z + entry.m_OffsetZ);
                }

                // Update revision
                design.OnRevised();
            }
        }

        [Usage("DesignInsert")]
        [Description("Inserts multiple targeted items into a customizable houses design.")]
        public static void DesignInsert_OnCommand(CommandEventArgs e)
        {
            e.Mobile.Target = new DesignInsertTarget(null);
            e.Mobile.SendMessage("Target an item to insert it into the house design.");
        }

        private class DesignInsertTarget : Target
        {
            private HouseFoundation m_Foundation;

            public DesignInsertTarget(HouseFoundation foundation)
                : base(-1, false, TargetFlags.None)
            {
                m_Foundation = foundation;
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (m_Foundation != null)
                {
                    from.SendMessage("Your changes have been committed. Updating...");

                    m_Foundation.Delta(ItemDelta.Update);
                }
            }

            protected override void OnTarget(Mobile from, object obj)
            {
                Item item = obj as Item;

                if (item == null)
                {
                    from.Target = new DesignInsertTarget(m_Foundation);
                    from.SendMessage("That is not an item. Try again.");
                }
                else
                {
                    HouseFoundation house = BaseHouse.FindHouseAt(item) as HouseFoundation;

                    if (house == null)
                    {
                        from.Target = new DesignInsertTarget(m_Foundation);
                        from.SendMessage("That item is not inside a customizable house. Try again.");
                    }
                    else if (m_Foundation != null && house != m_Foundation)
                    {
                        from.Target = new DesignInsertTarget(m_Foundation);
                        from.SendMessage("That item is not inside the current house; all targeted items must reside in the same house. You may cancel this target and repeat the command.");
                    }
                    else
                    {
                        DesignState state = house.CurrentState;
                        MultiComponentList mcl = state.Components;

                        int x = item.X - house.X;
                        int y = item.Y - house.Y;
                        int z = item.Z - house.Z;

                        if (x >= mcl.Min.X && y >= mcl.Min.Y && x <= mcl.Max.X && y <= mcl.Max.Y)
                        {
                            mcl.Add(item.ItemID, x, y, z);
                            item.Delete();

                            state.OnRevised();

                            state = house.DesignState;
                            mcl = state.Components;

                            if (x >= mcl.Min.X && y >= mcl.Min.Y && x <= mcl.Max.X && y <= mcl.Max.Y)
                            {
                                mcl.Add(item.ItemID, x, y, z);
                                state.OnRevised();
                            }

                            from.Target = new DesignInsertTarget(house);

                            if (m_Foundation == null)
                                from.SendMessage("The item has been inserted into the house design. Press ESC when you are finished.");
                            else
                                from.SendMessage("The item has been inserted into the house design.");

                            m_Foundation = house;
                        }
                        else
                        {
                            from.Target = new DesignInsertTarget(m_Foundation);
                            from.SendMessage("That item is not inside a customizable house. Try again.");
                        }
                    }
                }
            }
        }

        private static void TraceValidity(NetState state, int itemID)
        {
            try
            {
                using (StreamWriter op = new StreamWriter("comp_val.log", true))
                    op.WriteLine("{0}\t{1}\tInvalid ItemID 0x{2:X4}", state, state.Mobile, itemID);
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        public static void Designer_Build(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client chose to add a component
				 *  - Read data detailing component graphic and location
				 *  - Add component
				 *  - Update revision
				 */

                // Read data detailing component graphic and location
                int itemID = pvSrc.ReadInt32();
                int x = pvSrc.ReadInt32();
                int y = pvSrc.ReadInt32();

                // Add component
                DesignState design = context.Foundation.DesignState;
                MultiComponentList mcl = design.Components;

                int z = GetLevelZ(context.Level);

                if ((y + mcl.Center.Y) == (mcl.Height - 1))
                    z = 0; // Tiles placed on the far-south of the house are at 0 Z

                mcl.Add(itemID, x, y, z);

                // Update revision
                design.OnRevised();
            }
        }

        public static void Designer_Close(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client closed his house design window
				 *  - Remove design context
				 *  - Notify the client that customization has ended
				 *  - Refresh client with current visable design state
				 *  - Eject client from house
				 *  - If a signpost is needed, add it
				 */

                // Remove design context
                DesignContext.Remove(from);

                // Notify the client that customization has ended
                from.Send(new EndHouseCustomization(context.Foundation));

                // Refresh client with current visible design state
                context.Foundation.SendInfoTo(state);
                context.Foundation.CurrentState.SendDetailedInfoTo(state);

                // Eject client from house
                from.RevealingAction();

                from.MoveToWorld(context.Foundation.BanLocation, context.Foundation.Map);

                // If a signpost is needed, add it
                context.Foundation.CheckSignpost();
            }
        }

        public static void Designer_Level(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)
            {
                /* Client is moving to a new floor level
				 *  - Read data detailing the target level
				 *  - Validate target level
				 *  - Update design context with new level
				 *  - Teleport mobile to new level
				 *  - Update client
				 * 
				 * TODO: Proper validation for two-story homes
				 */

                // Read data detailing the target level
                int newLevel = pvSrc.ReadInt32();

                // Validate target level
                if (newLevel < 1 || newLevel > 4)
                    newLevel = 1;

                // Update design context with new level
                context.Level = newLevel;

                // Teleport mobile to new level
                from.Location = new Point3D(from.X, from.Y, context.Foundation.Z + GetLevelZ(newLevel));

                // Update client
                context.Foundation.SendInfoTo(state);
            }
        }

        public static void QueryDesignDetails(NetState state, PacketReader pvSrc)
        {
            Mobile from = state.Mobile;

            BaseMulti multi = World.FindItem(pvSrc.ReadInt32()) as BaseMulti;

            if (multi != null && from.Map == multi.Map && from.InRange(multi.GetWorldLocation(), 24) && from.CanSee(multi) && multi is IDesignState)
            {
                IDesignState foundation = (IDesignState)multi;

                foundation.SendDesignDetails(state);
            }
        }

        public static void Designer_Roof(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            //Adam: we do not have the SE expansion which allows us to place roofing tiles but we still want to allow them. 
            //  search for 'CoreSEOverride' in one other location in this file;
            bool CoreSEOverride = true;

            if (context != null && (CoreSEOverride || Core.RuleSets.SERules() || from.AccessLevel >= AccessLevel.GameMaster))
            {
                // Read data detailing component graphic and location
                int itemID = pvSrc.ReadInt32();
                int x = pvSrc.ReadInt32();
                int y = pvSrc.ReadInt32();
                int z = pvSrc.ReadInt32();

                // Add component
                DesignState design = context.Foundation.DesignState;

                if (from.AccessLevel < AccessLevel.GameMaster && !ValidPiece(itemID, true))
                {
                    TraceValidity(state, itemID);
                    design.SendDetailedInfoTo(state);
                    return;
                }

                MultiComponentList mcl = design.Components;

                if (z < -3 || z > 12 || z % 3 != 0)
                    z = -3;
                z += GetLevelZ(context.Level, context.Foundation);

                MultiTileEntry[] list = mcl.List;
                for (int i = 0; i < list.Length; i++)
                {
                    MultiTileEntry mte = list[i];

                    if (mte.m_OffsetX == x && mte.m_OffsetY == y && GetZLevel(mte.m_OffsetZ, context.Foundation) == context.Level && (TileData.ItemTable[mte.m_ItemID & 0x3FFF].Flags & TileFlag.Roof) != 0)
                        mcl.Remove(mte.m_ItemID, x, y, mte.m_OffsetZ);
                }

                mcl.Add(itemID, x, y, z);

                // Update revision
                design.OnRevised();
            }
        }

        public static void Designer_RoofDelete(NetState state, IEntity e, EncodedReader pvSrc)
        {
            Mobile from = state.Mobile;
            DesignContext context = DesignContext.Find(from);

            if (context != null)    //No need to check if core.SE if trying to remvoe something that shouldn't be able to be placed anyways
            {
                // Read data detailing which component to delete
                int itemID = pvSrc.ReadInt32();
                int x = pvSrc.ReadInt32();
                int y = pvSrc.ReadInt32();
                int z = pvSrc.ReadInt32();

                // Verify component is deletable
                DesignState design = context.Foundation.DesignState;
                MultiComponentList mcl = design.Components;

                if ((TileData.ItemTable[itemID & 0x3FFF].Flags & TileFlag.Roof) == 0)
                {
                    design.SendDetailedInfoTo(state);
                    return;
                }

                mcl.Remove(itemID, x, y, z);

                design.OnRevised();
            }
        }

        #region IDesignState

        public virtual void SendDesignGeneral(NetState state)
        {
            GetDesignState(state.Mobile).SendGeneralInfoTo(state);
        }

        public virtual void SendDesignDetails(NetState state)
        {
            GetDesignState(state.Mobile).SendDetailedInfoTo(state);
        }

        private DesignState GetDesignState(Mobile from)
        {
            DesignContext context = DesignContext.Find(from);

            if (context != null && context.Foundation == this)
                return DesignState;
            else
                return CurrentState;
        }

        #endregion
    }

    public class DesignState
    {
        private IDesignState m_Foundation;
        private MultiComponentList m_Components;
        private MultiTileEntry[] m_Fixtures;
        private int m_Revision;
        private Packet m_PacketCache;

        //public Packet PacketCache { get { return m_PacketCache; } set { m_PacketCache = value; } }
        public Packet PacketCache
        {
            get { return m_PacketCache; }
            set
            {
                if (m_PacketCache == value)
                    return;

                if (m_PacketCache != null)
                    m_PacketCache.Release();

                m_PacketCache = value;
            }
        }

        public IDesignState Foundation { get { return m_Foundation; } }
        public MultiComponentList Components { get { return m_Components; } set { m_Components = value; } }
        public MultiTileEntry[] Fixtures { get { return m_Fixtures; } }
        public int Revision { get { return m_Revision; } set { m_Revision = value; } }

        public DesignState(IDesignState foundation, MultiComponentList components)
        {
            m_Foundation = foundation;
            m_Components = components;
            m_Fixtures = new MultiTileEntry[0];
        }

        public DesignState(DesignState toCopy)
        {
            m_Foundation = toCopy.m_Foundation;

            m_Components = new MultiComponentList(toCopy.m_Components);

            m_Revision = toCopy.m_Revision;
            m_Fixtures = new MultiTileEntry[toCopy.m_Fixtures.Length];

            for (int i = 0; i < m_Fixtures.Length; ++i)
                m_Fixtures[i] = toCopy.m_Fixtures[i];
        }

        public DesignState(HouseFoundation foundation, GenericReader reader)
        {
            m_Foundation = foundation;

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Components = new MultiComponentList(reader);

                        int length = reader.ReadInt();

                        m_Fixtures = new MultiTileEntry[length];

                        for (int i = 0; i < length; ++i)
                        {
                            m_Fixtures[i].m_ItemID = reader.ReadUShort();
                            m_Fixtures[i].m_OffsetX = reader.ReadShort();
                            m_Fixtures[i].m_OffsetY = reader.ReadShort();
                            m_Fixtures[i].m_OffsetZ = reader.ReadShort();
                            m_Fixtures[i].m_Flags = reader.ReadInt();
                        }

                        m_Revision = reader.ReadInt();

                        break;
                    }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            m_Components.Serialize(writer);

            writer.Write((int)m_Fixtures.Length);

            for (int i = 0; i < m_Fixtures.Length; ++i)
            {
                MultiTileEntry ent = m_Fixtures[i];

                writer.Write((short)ent.m_ItemID);
                writer.Write((short)ent.m_OffsetX);
                writer.Write((short)ent.m_OffsetY);
                writer.Write((short)ent.m_OffsetZ);
                writer.Write((int)ent.m_Flags);
            }

            writer.Write((int)m_Revision);
        }

        public void OnRevised()
        {
            lock (this)
            {
                m_Revision = ++m_Foundation.LastRevision;

                if (m_PacketCache != null)
                    m_PacketCache.Release();

                m_PacketCache = null;
            }
        }

        public void SendGeneralInfoTo(NetState state)
        {
            if (state != null)
                state.Send(new DesignStateGeneral(m_Foundation, this));
        }

        public void SendDetailedInfoTo(NetState state)
        {
            if (state != null)
            {
                lock (this)
                {
                    if (m_PacketCache == null)
                        DesignStateDetailed.SendDetails(state, m_Foundation, this);
                    else
                        state.Send(m_PacketCache);
                }
            }
        }

        public void FreezeFixtures()
        {
            OnRevised();

            for (int i = 0; i < m_Fixtures.Length; ++i)
            {
                MultiTileEntry mte = m_Fixtures[i];

                m_Components.Add(mte.m_ItemID, mte.m_OffsetX, mte.m_OffsetY, mte.m_OffsetZ);
            }

            m_Fixtures = new MultiTileEntry[0];
        }

        public void MeltFixtures()
        {
            OnRevised();

            MultiTileEntry[] list = m_Components.List;
            int length = 0;

            for (int i = list.Length - 1; i >= 0; --i)
            {
                MultiTileEntry mte = list[i];

                if (IsFixture(mte.m_ItemID))
                    ++length;
            }

            m_Fixtures = new MultiTileEntry[length];

            for (int i = list.Length - 1; i >= 0; --i)
            {
                MultiTileEntry mte = list[i];

                if (IsFixture(mte.m_ItemID))
                {
                    m_Fixtures[--length] = mte;
                    m_Components.Remove(mte.m_ItemID, mte.m_OffsetX, mte.m_OffsetY, mte.m_OffsetZ);
                }
            }
        }

        public static bool IsFixture(int itemID)
        {
            itemID &= 0x3FFF;

            if (itemID >= 0x675 && itemID < 0x6F5)
                return true;
            else if (itemID >= 0x314 && itemID < 0x364)
                return true;
            else if (itemID >= 0x824 && itemID < 0x834)
                return true;
            else if (itemID >= 0x839 && itemID < 0x849)
                return true;
            else if (itemID >= 0x84C && itemID < 0x85C)
                return true;
            else if (itemID >= 0x866 && itemID < 0x876)
                return true;
            else if (itemID >= 0xE8 && itemID < 0xF8)
                return true;
            else if (itemID >= 0x1FED && itemID < 0x1FFD)
                return true;
            else if (itemID >= 0x181D && itemID < 0x1829)
                return true;

            return false;
        }
    }

    public class ConfirmCommitGump : Gump
    {
        private HouseFoundation m_Foundation;

        public ConfirmCommitGump(Mobile from, HouseFoundation foundation, int bankBalance, int oldPrice, int newPrice)
            : base(50, 50)
        {
            m_Foundation = foundation;

            AddPage(0);

            AddBackground(0, 0, 320, 320, 5054);

            AddImageTiled(10, 10, 300, 20, 2624);
            AddImageTiled(10, 40, 300, 240, 2624);
            AddImageTiled(10, 290, 300, 20, 2624);

            AddAlphaRegion(10, 10, 300, 300);

            AddHtmlLocalized(10, 10, 300, 20, 1062060, 32736, false, false); // <CENTER>COMMIT DESIGN</CENTER>

            AddHtmlLocalized(10, 40, 300, 140, (newPrice - oldPrice) <= bankBalance ? 1061898 : 1061903, 1023, false, true);

            AddHtmlLocalized(10, 190, 150, 20, 1061902, 32736, false, false); // Bank Balance:
            AddLabel(170, 190, 55, bankBalance.ToString());

            AddHtmlLocalized(10, 215, 150, 20, 1061899, 1023, false, false); // Old Value:
            AddLabel(170, 215, 90, oldPrice.ToString());

            AddHtmlLocalized(10, 235, 150, 20, 1061900, 1023, false, false); // Cost To Commit:
            AddLabel(170, 235, 90, newPrice.ToString());

            AddHtmlLocalized(10, 260, 150, 20, 1061901, 31744, false, false); // Your Cost:
            AddLabel(170, 260, 40, (newPrice - oldPrice).ToString());

            AddButton(10, 290, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddHtmlLocalized(45, 290, 55, 20, 1011036, 32767, false, false); // OKAY

            AddButton(170, 290, 4005, 4007, 0, GumpButtonType.Reply, 0);
            AddHtmlLocalized(195, 290, 55, 20, 1011012, 32767, false, false); // CANCEL
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info.ButtonID == 1)
                m_Foundation.EndConfirmCommit(sender.Mobile);
        }
    }

    public class DesignContext
    {
        private HouseFoundation m_Foundation;
        private int m_Level;

        public HouseFoundation Foundation { get { return m_Foundation; } }
        public int Level { get { return m_Level; } set { m_Level = value; } }

        public DesignContext(HouseFoundation foundation)
        {
            m_Foundation = foundation;
            m_Level = 1;
        }

        private static Hashtable m_Table = new Hashtable();

        public static Hashtable Table { get { return m_Table; } }

        public static DesignContext Find(Mobile from)
        {
            if (from == null)
                return null;

            return (DesignContext)m_Table[from];
        }

        public static bool Check(Mobile m)
        {
            if (Find(m) != null)
            {
                m.SendLocalizedMessage(1062206); // You cannot do that while customizing a house.
                return false;
            }

            return true;
        }

        public static void Add(Mobile from, HouseFoundation foundation)
        {
            if (from == null)
                return;

            DesignContext c = new DesignContext(foundation);

            m_Table[from] = c;

            if (from is PlayerMobile)
                ((PlayerMobile)from).DesignContext = c;

            foundation.Customizer = from;

            from.Hidden = true;
            from.Location = new Point3D(foundation.X, foundation.Y, foundation.Z + 7);

            NetState state = from.NetState;

            if (state == null)
                return;

            ArrayList fixtures = foundation.Fixtures;

            for (int i = 0; fixtures != null && i < fixtures.Count; ++i)
            {
                Item item = (Item)fixtures[i];

                state.Send(item.RemovePacket);
            }

            if (foundation.Signpost != null)
                state.Send(foundation.Signpost.RemovePacket);
        }

        public static void Remove(Mobile from)
        {
            if (from == null)
                return;

            DesignContext context = (DesignContext)m_Table[from];

            m_Table.Remove(from);

            if (from is PlayerMobile)
                ((PlayerMobile)from).DesignContext = null;

            if (context == null)
                return;

            context.Foundation.Customizer = null;

            NetState state = from.NetState;

            if (state == null)
                return;

            ArrayList fixtures = context.Foundation.Fixtures;

            for (int i = 0; fixtures != null && i < fixtures.Count; ++i)
            {
                Item item = (Item)fixtures[i];

                item.SendInfoTo(state);
            }

            if (context.Foundation.Signpost != null)
                context.Foundation.Signpost.SendInfoTo(state);
        }
    }

    public class BeginHouseCustomization : Packet
    {
        public BeginHouseCustomization(HouseFoundation house)
            : base(0xBF)
        {
            EnsureCapacity(17);

            m_Stream.Write((short)0x20);
            m_Stream.Write((int)house.Serial);
            m_Stream.Write((byte)0x04);
            m_Stream.Write((ushort)0x0000);
            m_Stream.Write((ushort)0xFFFF);
            m_Stream.Write((ushort)0xFFFF);
            m_Stream.Write((byte)0xFF);
        }
    }

    public class EndHouseCustomization : Packet
    {
        public EndHouseCustomization(HouseFoundation house)
            : base(0xBF)
        {
            EnsureCapacity(17);

            m_Stream.Write((short)0x20);
            m_Stream.Write((int)house.Serial);
            m_Stream.Write((byte)0x05);
            m_Stream.Write((ushort)0x0000);
            m_Stream.Write((ushort)0xFFFF);
            m_Stream.Write((ushort)0xFFFF);
            m_Stream.Write((byte)0xFF);
        }
    }

    public class DesignStateGeneral : Packet
    {
        public DesignStateGeneral(IDesignState house, DesignState state)
            : base(0xBF)
        {
            EnsureCapacity(13);

            m_Stream.Write((short)0x1D);
            m_Stream.Write((int)house.Serial);
            m_Stream.Write((int)state.Revision);
        }
    }

    public class DesignStateDetailed : Packet
    {
        public const int MaxItemsPerStairBuffer = 750;

        private static byte[][] m_PlaneBuffers;
        private static bool[] m_PlaneUsed;

        private static byte[][] m_StairBuffers;

        private static byte[] m_PrimBuffer = new byte[4];

        public void Write(int value)
        {
            m_PrimBuffer[0] = (byte)(value >> 24);
            m_PrimBuffer[1] = (byte)(value >> 16);
            m_PrimBuffer[2] = (byte)(value >> 8);
            m_PrimBuffer[3] = (byte)value;

            m_Stream.UnderlyingStream.Write(m_PrimBuffer, 0, 4);
        }

        public void Write(short value)
        {
            m_PrimBuffer[0] = (byte)(value >> 8);
            m_PrimBuffer[1] = (byte)value;

            m_Stream.UnderlyingStream.Write(m_PrimBuffer, 0, 2);
        }

        public void Write(byte value)
        {
            m_Stream.UnderlyingStream.WriteByte(value);
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            m_Stream.UnderlyingStream.Write(buffer, offset, size);
        }

        public static void Clear(byte[] buffer, int size)
        {
            for (int i = 0; i < size; ++i)
                buffer[i] = 0;
        }

        public DesignStateDetailed(int serial, int revision, int xMin, int yMin, int xMax, int yMax, MultiTileEntry[] tiles, bool planes)
            : base(0xD8)
        {
            EnsureCapacity(17 + (tiles.Length * 5));

            Write((byte)0x03); // Compression Type
            Write((byte)0x00); // Unknown
            Write((int)serial);
            Write((int)revision);
            Write((short)tiles.Length);
            Write((short)0); // Buffer length : reserved
            Write((byte)0); // Plane count : reserved

            int totalLength = 1; // includes plane count

            int width = (xMax - xMin) + 1;
            int height = (yMax - yMin) + 1;

            if (m_PlaneBuffers == null)
            {
                m_PlaneBuffers = new byte[9][];
                m_PlaneUsed = new bool[9];

                for (int i = 0; i < m_PlaneBuffers.Length; ++i)
                    m_PlaneBuffers[i] = new byte[0x400];

                m_StairBuffers = new byte[6][];

                for (int i = 0; i < m_StairBuffers.Length; ++i)
                    m_StairBuffers[i] = new byte[MaxItemsPerStairBuffer * 5];
            }
            else
            {
                for (int i = 0; i < m_PlaneUsed.Length; ++i)
                    m_PlaneUsed[i] = false;

                Clear(m_PlaneBuffers[0], width * height * 2);

                for (int i = 0; i < 4; ++i)
                {
                    Clear(m_PlaneBuffers[1 + i], (width - 1) * (height - 2) * 2);
                    Clear(m_PlaneBuffers[5 + i], width * (height - 1) * 2);
                }
            }

            int totalStairsUsed = 0;

            for (int i = 0; i < tiles.Length; ++i)
            {
                MultiTileEntry mte = tiles[i];
                int x = mte.m_OffsetX - xMin;
                int y = mte.m_OffsetY - yMin;
                int z = mte.m_OffsetZ;
                bool floor = (TileData.ItemTable[mte.m_ItemID & 0x3FFF].Height <= 0);
                int plane, size;

                if (!planes)
                    z = -1;

                switch (z)
                {
                    case 0: plane = 0; break;
                    case 7: plane = 1; break;
                    case 27: plane = 2; break;
                    case 47: plane = 3; break;
                    case 67: plane = 4; break;
                    default:
                        {
                            int stairBufferIndex = (totalStairsUsed / MaxItemsPerStairBuffer);
                            byte[] stairBuffer = m_StairBuffers[stairBufferIndex];

                            int byteIndex = (totalStairsUsed % MaxItemsPerStairBuffer) * 5;

                            stairBuffer[byteIndex++] = (byte)((mte.m_ItemID >> 8) & 0x3F);
                            stairBuffer[byteIndex++] = (byte)mte.m_ItemID;

                            stairBuffer[byteIndex++] = (byte)mte.m_OffsetX;
                            stairBuffer[byteIndex++] = (byte)mte.m_OffsetY;
                            stairBuffer[byteIndex++] = (byte)mte.m_OffsetZ;

                            ++totalStairsUsed;

                            continue;
                        }
                }

                if (plane == 0)
                {
                    size = height;
                }
                else if (floor)
                {
                    size = height - 2;
                    x -= 1;
                    y -= 1;
                }
                else
                {
                    size = height - 1;
                    plane += 4;
                }

                int index = ((x * size) + y) * 2;

                if (index < 0)
                    index = 0;

                m_PlaneUsed[plane] = true;
                m_PlaneBuffers[plane][index] = (byte)((mte.m_ItemID >> 8) & 0x3F);
                m_PlaneBuffers[plane][index + 1] = (byte)mte.m_ItemID;
            }

            int planeCount = 0;

            for (int i = 0; i < m_PlaneBuffers.Length; ++i)
            {
                if (!m_PlaneUsed[i])
                    continue;

                ++planeCount;

                int size = 0;

                if (i == 0)
                    size = width * height * 2;
                else if (i < 5)
                    size = (width - 1) * (height - 2) * 2;
                else
                    size = width * (height - 1) * 2;

                byte[] inflatedBuffer = m_PlaneBuffers[i];

                int deflatedLength = m_DeflatedBuffer.Length;
                ZLibError ce = Compression.Pack(m_DeflatedBuffer, ref deflatedLength, inflatedBuffer, size, ZLibQuality.Default);

                if (ce != ZLibError.Okay)
                {
                    Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);
                    deflatedLength = 0;
                    size = 0;
                }

                Write((byte)(0x20 | i));
                Write((byte)size);
                Write((byte)deflatedLength);
                Write((byte)(((size >> 4) & 0xF0) | ((deflatedLength >> 8) & 0xF)));
                Write(m_DeflatedBuffer, 0, deflatedLength);

                totalLength += 4 + deflatedLength;
            }

            int totalStairBuffersUsed = (totalStairsUsed + (MaxItemsPerStairBuffer - 1)) / MaxItemsPerStairBuffer;

            for (int i = 0; i < totalStairBuffersUsed; ++i)
            {
                ++planeCount;

                int count = (totalStairsUsed - (i * MaxItemsPerStairBuffer));

                if (count > MaxItemsPerStairBuffer)
                    count = MaxItemsPerStairBuffer;

                int size = count * 5;

                byte[] inflatedBuffer = m_StairBuffers[i];

                int deflatedLength = m_DeflatedBuffer.Length;
                ZLibError ce = Compression.Pack(m_DeflatedBuffer, ref deflatedLength, inflatedBuffer, size, ZLibQuality.Default);

                if (ce != ZLibError.Okay)
                {
                    Console.WriteLine("ZLib error: {0} (#{1})", ce, (int)ce);
                    deflatedLength = 0;
                    size = 0;
                }

                Write((byte)(9 + i));
                Write((byte)size);
                Write((byte)deflatedLength);
                Write((byte)(((size >> 4) & 0xF0) | ((deflatedLength >> 8) & 0xF)));
                Write(m_DeflatedBuffer, 0, deflatedLength);

                totalLength += 4 + deflatedLength;
            }

            m_Stream.Seek(15, System.IO.SeekOrigin.Begin);

            Write((short)totalLength); // Buffer length
            Write((byte)planeCount); // Plane count

            /*int planes = (tiles.Length + (MaxItemsPerPlane - 1)) / MaxItemsPerPlane;

			if ( planes > 255 )
				planes = 255;

			int totalLength = 0;

			m_Stream.Write( (byte) planes );
			++totalLength;

			int itemIndex = 0;

			for ( int i = 0; i < planes; ++i )
			{
				int byteIndex = 0;

				for ( int j = 0; j < MaxItemsPerPlane && itemIndex < tiles.Length; ++j, ++itemIndex )
				{
					MultiTileEntry e = tiles[itemIndex];

					m_InflatedBuffer[byteIndex++] = (byte)((e.m_ItemID >> 8) & 0x3F);
					m_InflatedBuffer[byteIndex++] = (byte)e.m_ItemID;
					m_InflatedBuffer[byteIndex++] = (byte)e.m_OffsetX;
					m_InflatedBuffer[byteIndex++] = (byte)e.m_OffsetY;
					m_InflatedBuffer[byteIndex++] = (byte)e.m_OffsetZ;
				}

				int deflatedLength = m_DeflatedBuffer.Length;
				ZLibError ce = ZLib.compress2( m_DeflatedBuffer, ref deflatedLength, m_InflatedBuffer, byteIndex, ZLibCompressionLevel.Z_DEFAULT_COMPRESSION );

				if ( ce != ZLibError.Z_OK )
				{
					Console.WriteLine( "ZLib error: {0} (#{1})", ce, (int)ce );
					deflatedLength = 0;
					byteIndex = 0;
				}

				m_Stream.Write( (byte) 0x00 );
				m_Stream.Write( (byte) byteIndex );
				m_Stream.Write( (byte) deflatedLength );
				m_Stream.Write( (byte) (((byteIndex >> 4) & 0xF0) | ((deflatedLength >> 8) & 0xF)) );
				m_Stream.Write( m_DeflatedBuffer, 0, deflatedLength );

				totalLength += 4 + deflatedLength;
			}

			m_Stream.Seek( 15, System.IO.SeekOrigin.Begin );
			m_Stream.Write( (short) totalLength ); // Buffer length*/
        }

        private static byte[] m_InflatedBuffer = new byte[0x2000];
        private static byte[] m_DeflatedBuffer = new byte[0x2000];

        private class SendQueueEntry
        {
            public NetState m_NetState;
            public int m_Serial, m_Revision;
            public int m_xMin, m_yMin, m_xMax, m_yMax;
            public DesignState m_Root;
            public MultiTileEntry[] m_Tiles;
            public bool m_Planes;

            public SendQueueEntry(NetState ns, IDesignState foundation, DesignState state)
            {
                m_NetState = ns;
                m_Serial = foundation.Serial;
                m_Revision = state.Revision;
                m_Root = state;

                MultiComponentList mcl = state.Components;

                m_xMin = mcl.Min.X;
                m_yMin = mcl.Min.Y;
                m_xMax = mcl.Max.X;
                m_yMax = mcl.Max.Y;

                m_Tiles = mcl.List;

                m_Planes = foundation is HouseFoundation; // only do "planes" compression for actual house foundations
            }
        }

        private static Queue m_SendQueue;
        private static AutoResetEvent m_Sync;
        private static Thread m_Thread;

        static DesignStateDetailed()
        {
            m_SendQueue = Queue.Synchronized(new Queue());
            m_Sync = new AutoResetEvent(false);

            m_Thread = new Thread(new ThreadStart(CompressionThread));
            m_Thread.Name = "AOS Compression Thread";
            m_Thread.Start();
        }

        public static void CompressionThread()
        {
            while (!Core.Closing)
            {
                m_Sync.WaitOne();

                while (m_SendQueue.Count > 0)
                {
                    SendQueueEntry sqe = (SendQueueEntry)m_SendQueue.Dequeue();

                    try
                    {
                        Packet p = null;

                        lock (sqe.m_Root)
                            p = sqe.m_Root.PacketCache;

                        if (p == null)
                        {
                            p = new DesignStateDetailed(sqe.m_Serial, sqe.m_Revision, sqe.m_xMin, sqe.m_yMin, sqe.m_xMax, sqe.m_yMax, sqe.m_Tiles, sqe.m_Planes);
                            p.SetStatic();

                            lock (sqe.m_Root)
                            {
                                if (sqe.m_Revision == sqe.m_Root.Revision)
                                    sqe.m_Root.PacketCache = p;
                            }
                        }

                        Timer.DelayCall(TimeSpan.Zero, new TimerStateCallback(SendPacket_Sandbox), new object[] { sqe.m_NetState, p });
                    }
                    catch (Exception e)
                    {
                        LogHelper.LogException(e);
                        Console.WriteLine(e);

                        try
                        {
                            using (StreamWriter op = new StreamWriter("dsd_exceptions.txt", true))
                                op.WriteLine(e);
                        }
                        catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                    }

                    //sqe.m_NetState.Send( new DesignStateDetailed( sqe.m_Serial, sqe.m_Revision, sqe.m_xMin, sqe.m_yMin, sqe.m_xMax, sqe.m_yMax, sqe.m_Tiles ) );
                }
            }
        }

        public static void SendPacket_Sandbox(object state)
        {
            object[] states = (object[])state;
            NetState ns = (NetState)states[0];
            Packet p = (Packet)states[1];

            ns.Send(p);
        }

        public static void SendDetails(NetState ns, IDesignState house, DesignState state)
        {
            m_SendQueue.Enqueue(new SendQueueEntry(ns, house, state));
            m_Sync.Set();
        }
    }
}