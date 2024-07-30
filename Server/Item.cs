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

using Server.Diagnostics;
using Server.Items;
using Server.Items.Triggers;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    #region enums, flags and such
    /// <summary>
    /// Enumeration of item layer values.
    /// </summary>
    public enum Layer : byte
    {
        /// <summary>
        /// Invalid layer.
        /// </summary>
        Invalid = 0x00,
        /// <summary>
        /// First valid layer. Equivalent to <c>Layer.OneHanded</c>.
        /// </summary>
        FirstValid = 0x01,
        /// <summary>
        /// One handed weapon.
        /// </summary>
        OneHanded = 0x01,
        /// <summary>
        /// Two handed weapon or shield.
        /// </summary>
        TwoHanded = 0x02,
        /// <summary>
        /// Shoes.
        /// </summary>
        Shoes = 0x03,
        /// <summary>
        /// Pants.
        /// </summary>
        Pants = 0x04,
        /// <summary>
        /// Shirts.
        /// </summary>
        Shirt = 0x05,
        /// <summary>
        /// Helmets, hats, and masks.
        /// </summary>
        Helm = 0x06,
        /// <summary>
        /// Gloves.
        /// </summary>
        Gloves = 0x07,
        /// <summary>
        /// Rings.
        /// </summary>
        Ring = 0x08,
        /// <summary>
        /// Talismans.
        /// </summary>
        Talisman = 0x09,
        /// <summary>
        /// Gorgets and necklaces.
        /// </summary>
        Neck = 0x0A,
        /// <summary>
        /// Hair.
        /// </summary>
        Hair = 0x0B,
        /// <summary>
        /// Half aprons.
        /// </summary>
        Waist = 0x0C,
        /// <summary>
        /// Torso, inner layer.
        /// </summary>
        InnerTorso = 0x0D,
        /// <summary>
        /// Bracelets.
        /// </summary>
        Bracelet = 0x0E,
        /// <summary>
        /// Unused.
        /// </summary>
        Unused_xF = 0x0F,
        /// <summary>
        /// Beards and mustaches.
        /// </summary>
        FacialHair = 0x10,
        /// <summary>
        /// Torso, outer layer.
        /// </summary>
        MiddleTorso = 0x11,
        /// <summary>
        /// Earings.
        /// </summary>
        Earrings = 0x12,
        /// <summary>
        /// Arms and sleeves.
        /// </summary>
        Arms = 0x13,
        /// <summary>
        /// Cloaks.
        /// </summary>
        Cloak = 0x14,
        /// <summary>
        /// Backpacks.
        /// </summary>
        Backpack = 0x15,
        /// <summary>
        /// Torso, outer layer.
        /// </summary>
        OuterTorso = 0x16,
        /// <summary>
        /// Leggings, outer layer.
        /// </summary>
        OuterLegs = 0x17,
        /// <summary>
        /// Leggings, inner layer.
        /// </summary>
        InnerLegs = 0x18,
        /// <summary>
        /// Last valid non-internal layer. Equivalent to <c>Layer.InnerLegs</c>.
        /// </summary>
        LastUserValid = 0x18,
        /// <summary>
        /// Mount item layer.
        /// </summary>
        Mount = 0x19,
        /// <summary>
        /// Vendor 'buy pack' layer.
        /// </summary>
        ShopBuy = 0x1A,
        /// <summary>
        /// Vendor 'resale pack' layer.
        /// </summary>
        ShopResale = 0x1B,
        /// <summary>
        /// Vendor 'sell pack' layer.
        /// </summary>
        ShopSell = 0x1C,
        /// <summary>
        /// Bank box layer.
        /// </summary>
        Bank = 0x1D,
        /// <summary>
        /// Last valid layer. Equivalent to <c>Layer.Bank</c>.
        /// </summary>
        LastValid = 0x1D
    }
    /// <summary>
    /// Internal flags used to signal how the item should be updated and resent to nearby clients.
    /// </summary>
    [Flags]
    public enum ItemDelta
    {
        /// <summary>
        /// Nothing.
        /// </summary>
        None = 0x00000000,
        /// <summary>
        /// Resend the item.
        /// </summary>
        Update = 0x00000001,
        /// <summary>
        /// Resend the item only if it is equiped.
        /// </summary>
        EquipOnly = 0x00000002,
        /// <summary>
        /// Resend the item's properties.
        /// </summary>
        Properties = 0x00000004
    }
    /// <summary>
    /// Enumeration containing possible ways to handle item ownership on death.
    /// </summary>
    public enum DeathMoveResult
    {
        /// <summary>
        /// The item should be placed onto the corpse.
        /// </summary>
        MoveToCorpse,
        /// <summary>
        /// The item should remain equiped.
        /// </summary>
        RemainEquiped,
        /// <summary>
        /// The item should be placed into the owners backpack.
        /// </summary>
        MoveToBackpack,
    }
    /// <summary>
    /// Enumeration containing all possible light types. These are only applicable to light source items, like lanterns, candles, braziers, etc.
    /// </summary>
    public enum LightType
    {
        /// <summary>
        /// Window shape, arched, ray shining east.
        /// </summary>
        ArchedWindowEast,
        /// <summary>
        /// Medium circular shape.
        /// </summary>
        Circle225,
        /// <summary>
        /// Small circular shape.
        /// </summary>
        Circle150,
        /// <summary>
        /// Door shape, shining south.
        /// </summary>
        DoorSouth,
        /// <summary>
        /// Door shape, shining east.
        /// </summary>
        DoorEast,
        /// <summary>
        /// Large semicircular shape (180 degrees), north wall.
        /// </summary>
        NorthBig,
        /// <summary>
        /// Large pie shape (90 degrees), north-east corner.
        /// </summary>
        NorthEastBig,
        /// <summary>
        /// Large semicircular shape (180 degrees), east wall.
        /// </summary>
        EastBig,
        /// <summary>
        /// Large semicircular shape (180 degrees), west wall.
        /// </summary>
        WestBig,
        /// <summary>
        /// Large pie shape (90 degrees), south-west corner.
        /// </summary>
        SouthWestBig,
        /// <summary>
        /// Large semicircular shape (180 degrees), south wall.
        /// </summary>
        SouthBig,
        /// <summary>
        /// Medium semicircular shape (180 degrees), north wall.
        /// </summary>
        NorthSmall,
        /// <summary>
        /// Medium pie shape (90 degrees), north-east corner.
        /// </summary>
        NorthEastSmall,
        /// <summary>
        /// Medium semicircular shape (180 degrees), east wall.
        /// </summary>
        EastSmall,
        /// <summary>
        /// Medium semicircular shape (180 degrees), west wall.
        /// </summary>
        WestSmall,
        /// <summary>
        /// Medium semicircular shape (180 degrees), south wall.
        /// </summary>
        SouthSmall,
        /// <summary>
        /// Shaped like a wall decoration, north wall.
        /// </summary>
        DecorationNorth,
        /// <summary>
        /// Shaped like a wall decoration, north-east corner.
        /// </summary>
        DecorationNorthEast,
        /// <summary>
        /// Small semicircular shape (180 degrees), east wall.
        /// </summary>
        EastTiny,
        /// <summary>
        /// Shaped like a wall decoration, west wall.
        /// </summary>
        DecorationWest,
        /// <summary>
        /// Shaped like a wall decoration, south-west corner.
        /// </summary>
        DecorationSouthWest,
        /// <summary>
        /// Small semicircular shape (180 degrees), south wall.
        /// </summary>
        SouthTiny,
        /// <summary>
        /// Window shape, rectangular, no ray, shining south.
        /// </summary>
        RectWindowSouthNoRay,
        /// <summary>
        /// Window shape, rectangular, no ray, shining east.
        /// </summary>
        RectWindowEastNoRay,
        /// <summary>
        /// Window shape, rectangular, ray shining south.
        /// </summary>
        RectWindowSouth,
        /// <summary>
        /// Window shape, rectangular, ray shining east.
        /// </summary>
        RectWindowEast,
        /// <summary>
        /// Window shape, arched, no ray, shining south.
        /// </summary>
        ArchedWindowSouthNoRay,
        /// <summary>
        /// Window shape, arched, no ray, shining east.
        /// </summary>
        ArchedWindowEastNoRay,
        /// <summary>
        /// Window shape, arched, ray shining south.
        /// </summary>
        ArchedWindowSouth,
        /// <summary>
        /// Large circular shape.
        /// </summary>
        Circle300,
        /// <summary>
        /// Large pie shape (90 degrees), north-west corner.
        /// </summary>
        NorthWestBig,
        /// <summary>
        /// Negative light. Medium pie shape (90 degrees), south-east corner.
        /// </summary>
        DarkSouthEast,
        /// <summary>
        /// Negative light. Medium semicircular shape (180 degrees), south wall.
        /// </summary>
        DarkSouth,
        /// <summary>
        /// Negative light. Medium pie shape (90 degrees), north-west corner.
        /// </summary>
        DarkNorthWest,
        /// <summary>
        /// Negative light. Medium pie shape (90 degrees), south-east corner. Equivalent to <c>LightType.SouthEast</c>.
        /// </summary>
        DarkSouthEast2,
        /// <summary>
        /// Negative light. Medium circular shape (180 degrees), east wall.
        /// </summary>
        DarkEast,
        /// <summary>
        /// Negative light. Large circular shape.
        /// </summary>
        DarkCircle300,
        /// <summary>
        /// Opened door shape, shining south.
        /// </summary>
        DoorOpenSouth,
        /// <summary>
        /// Opened door shape, shining east.
        /// </summary>
        DoorOpenEast,
        /// <summary>
        /// Window shape, square, ray shining east.
        /// </summary>
        SquareWindowEast,
        /// <summary>
        /// Window shape, square, no ray, shining east.
        /// </summary>
        SquareWindowEastNoRay,
        /// <summary>
        /// Window shape, square, ray shining south.
        /// </summary>
        SquareWindowSouth,
        /// <summary>
        /// Window shape, square, no ray, shining south.
        /// </summary>
        SquareWindowSouthNoRay,
        /// <summary>
        /// Empty.
        /// </summary>
        Empty,
        /// <summary>
        /// Window shape, skinny, no ray, shining south.
        /// </summary>
        SkinnyWindowSouthNoRay,
        /// <summary>
        /// Window shape, skinny, ray shining east.
        /// </summary>
        SkinnyWindowEast,
        /// <summary>
        /// Window shape, skinny, no ray, shining east.
        /// </summary>
        SkinnyWindowEastNoRay,
        /// <summary>
        /// Shaped like a hole, shining south.
        /// </summary>
        HoleSouth,
        /// <summary>
        /// Shaped like a hole, shining south.
        /// </summary>
        HoleEast,
        /// <summary>
        /// Large circular shape with a moongate graphic embeded.
        /// </summary>
        Moongate,
        /// <summary>
        /// Unknown usage. Many rows of slightly angled lines.
        /// </summary>
        Strips,
        /// <summary>
        /// Shaped like a small hole, shining south.
        /// </summary>
        SmallHoleSouth,
        /// <summary>
        /// Shaped like a small hole, shining east.
        /// </summary>
        SmallHoleEast,
        /// <summary>
        /// Large semicircular shape (180 degrees), north wall. Identical graphic as <c>LightType.NorthBig</c>, but slightly different positioning.
        /// </summary>
        NorthBig2,
        /// <summary>
        /// Large semicircular shape (180 degrees), west wall. Identical graphic as <c>LightType.WestBig</c>, but slightly different positioning.
        /// </summary>
        WestBig2,
        /// <summary>
        /// Large pie shape (90 degrees), north-west corner. Equivalent to <c>LightType.NorthWestBig</c>.
        /// </summary>
        NorthWestBig2
    }
    /// <summary>
    /// Enumeration of an item's loot and steal state.
    /// </summary>

    #region LootType
    [Flags]
    public enum LootType : UInt32
    {
        Regular = 0x00,                                     /// Stealable. Lootable.
        UnStealable = 0x01,                                 /// Unstealable
        UnLootable = 0x02,                                  /// Unlootable
        Newbied = 0x04 | UnStealable | UnLootable,          /// Unstealable. Unlootable, unless owned by a murderer.
        Blessed = 0x08 | UnStealable | UnLootable,          /// Unstealable. Unlootable, always.
        Cursed = 0x10 | Regular,                            /// Stealable. UnLootable, always.
        Rare = 0x20 | UnStealable,                          /// UnStealable. Lootable, always.
        SiegeBlessed = 0x40 | UnStealable | UnLootable,     /// Unstealable. Unlootable, when had by owner.
        Smuggled = 0x80,                                    /// Loot "Smuggled" from a location that doesn't otherwise allow you to take items (Prison)
        // other flags go here
        Unspecified = 0x100,                                /// PackItem will use the item's LootType.
        ApiKey = 0x200                                      /// Prevents GMs from setting SiegeBlessed
    }

    #endregion LootType

    public enum AuditType
    {
        GoldLifted,             // looting a fallen monster, etc
        GoldDropBackpack,       //
        GoldDropBank,
        CheckDropBackpack,
        CheckDropBank,
    }

    public enum Article
    {
        None,
        A,
        An,
        The,
    }

    public class BounceInfo
    {
        public Map m_Map;
        public Point3D m_Location, m_WorldLoc;
        public object m_Parent;

        public BounceInfo(Item item)
        {
            m_Map = item.Map;
            m_Location = item.Location;
            m_WorldLoc = item.GetWorldLocation();
            m_Parent = item.Parent;
        }

        private BounceInfo(Map map, Point3D loc, Point3D worldLoc, object parent)
        {
            m_Map = map;
            m_Location = loc;
            m_WorldLoc = worldLoc;
            m_Parent = parent;
        }

        public static BounceInfo Deserialize(GenericReader reader)
        {
            if (reader.ReadBool())
            {
                Map map = reader.ReadMap();
                Point3D loc = reader.ReadPoint3D();
                Point3D worldLoc = reader.ReadPoint3D();

                object parent;

                Serial serial = reader.ReadInt();

                if (serial.IsItem)
                    parent = World.FindItem(serial);
                else if (serial.IsMobile)
                    parent = World.FindMobile(serial);
                else
                    parent = null;

                return new BounceInfo(map, loc, worldLoc, parent);
            }
            else
            {
                return null;
            }
        }

        public static void Serialize(BounceInfo info, GenericWriter writer)
        {
            if (info == null)
            {
                writer.Write(false);
            }
            else
            {
                writer.Write(true);

                writer.Write(info.m_Map);
                writer.Write(info.m_Location);
                writer.Write(info.m_WorldLoc);

                if (info.m_Parent is Mobile)
                    writer.Write((Mobile)info.m_Parent);
                else if (info.m_Parent is Item)
                    writer.Write((Item)info.m_Parent);
                else
                    writer.Write((Serial)0);
            }
        }
    }
    [Flags]
    public enum ExpandFlag
    {
        None = 0x000,

        Name = 0x001,
        Items = 0x002,
        Bounce = 0x004,
        Holder = 0x008,
        Blessed = 0x010,
        TempFlag = 0x020,
        SaveFlag = 0x040,
        Weight = 0x080,
        Spawner = 0x100
    }
    #endregion enums, flags and such
    public class Item : IPoint3D, IEntity, IHued
    {
        #region Logging
        public void LogChange(Mobile m, string prop, string from, string to)
        {
            if (m != null && m.Account != null)
            {
                // only record staff changes
                m_LastChange = string.Format("{0}: {1} from '{2}' to '{3}'", m, prop, from, to);
                m_LastTouched = DateTime.UtcNow;
            }
        }
        public void LogChange(Mobile m, string message)
        {
            if (m != null && m.Account != null)
            {
                // only record staff changes
                m_LastChange = string.Format($"{m}: {message}");
                m_LastTouched = DateTime.UtcNow;
            }
        }
        private string m_LastChange = string.Empty;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.System)]
        public string LastChange
        {
            get
            {
                return m_LastChange;
            }
            set
            {   // this is only exposed so that spawners can set this during the next patch.
                //  This write access should be removed after that.
                //  We don't update m_LastTouched here to indicate we really don't know the date of this last change
                m_LastChange = value;
            }
        }
        private DateTime m_LastTouched = DateTime.MinValue;
        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime LastTouched
        {
            get
            {
                return m_LastTouched;
            }
        }
        #endregion Logging
        private string m_Label;
        [CommandProperty(AccessLevel.GameMaster)]
        public string Label { get { return m_Label; } set { m_Label = value; } }
        #region Debug
        public void DebugLabelTo(string text, int hue = 0x3B2)
        {
            if (text == null || Debug == false || Map == Map.Internal || Map == null)
                return;
            IPooledEnumerable eable = this.Map.GetMobilesInRange(this.Location, 13);
            foreach (Mobile m in eable)
            {
                if (m is PlayerMobile pm && pm.CanSee(this))
                    LabelToHued(pm, text, hue);
            }
            eable.Free();
        }
        #endregion Debug
        #region Blink
        private const int BlinkCount = 3;
        private TimeSpan BlinkInterval = TimeSpan.FromSeconds(1.5);
        public void Blink(int new_hue = 0x47E /*true white*/)
        {
            if (!BlinkRunning)
                StartBlinkTimer(hue: this.Hue, new_hue: new_hue, visible: this.Visible, tick: 1, delay: TimeSpan.FromSeconds(0.5));
        }
        private bool BlinkRunning
        {
            get { return (m_BlinkTimer != null); }
        }

        private void StopBlinkTimer()
        {
            if (m_BlinkTimer != null)
            {
                m_BlinkTimer.Stop();
                m_BlinkTimer = null;
            }
        }

        private void StartBlinkTimer(int hue, int new_hue, bool visible, int tick, TimeSpan delay)
        {
            StopBlinkTimer();

            (m_BlinkTimer = new InternalBlinkTimer(this, hue, new_hue: new_hue, visible, tick, delay)).Start();
        }

        private void OnBlinkTick(int hue, int new_hue, bool visible, int tick)
        {
            if (tick % 2 == 0)
            {
                this.Hue = new_hue;
                this.Visible = true;        // visible
            }
            else
            {
                this.Hue = hue;             // normal color
                this.Visible = visible;     // normal state
            }

            if (tick >= BlinkCount)
            {
                this.Hue = hue;             // reset
                this.Visible = visible;     // reset
                StopBlinkTimer();
            }
        }
        private InternalBlinkTimer m_BlinkTimer;
        private class InternalBlinkTimer : Timer
        {
            private Item m_Item;
            private int m_Hue;
            private int m_NewHue;
            private bool m_Visible;
            private int m_Tick;
            public InternalBlinkTimer(Item item, int hue, int new_hue, bool visible, int tick, TimeSpan delay)
                : base(delay, item.BlinkInterval)
            {
                m_Item = item;
                m_Hue = hue;
                m_NewHue = new_hue;
                m_Visible = visible;
                m_Tick = tick;
            }

            protected override void OnTick()
            {
                m_Item.OnBlinkTick(m_Hue, m_NewHue, m_Visible, ++m_Tick);
            }
        }
        #endregion Blink
        #region Colorize
        public bool Colorize
        {
            set
            {
                if (value)
                {
                    Hue = Utility.RandomSpecialHue(GetType().ToString());
                    Visible = true;
                }
                else
                {
                    Visible = false;
                }
            }
        }
        #endregion Colorize
        #region Usage
        public virtual void Usage(Mobile from)
        {
            from.SendMessage("There is no usage information for this item.");
        }
        #endregion Usage
        #region RunUO2.6 Port compatibility functions
        public const int QuestItemHue = 0x4EA; // Hmmmm... "for EA"?
        public bool QuestItem
        {
            get { return false; }
#if false
            get { return GetFlag(ImplFlag.QuestItem); }
            set
            {
                SetFlag(ImplFlag.QuestItem, value);

                InvalidateProperties();

                ReleaseWorldPackets();

                Delta(ItemDelta.Update);
            }
#endif
        }
        #endregion RunUO2.6 Port compatibility functions
        /// <summary>
        /// Called after world load to provide a context-aware initialization
        /// </summary>
        public virtual void WorldLoaded()
        {
            return;
        }
        public void Notify(Notification notification, params object[] args)
        {
            switch (notification)
            {
                default:
                    return;
            }
        }
        public virtual bool HasAccess(Mobile from)
        {
            return true;
        }
        public virtual bool SetObjectProp(string name, string value, bool message = true)
        {   // this is will only set properties with the Command Property Attribute. This is intended.
            //  We don't want to give GM's a back door into the guts of the object.
            string result = Server.Commands.Properties.SetValue(World.GetSystemAcct(), this, name, value);
            if (message)
                this.SendSystemMessage(result);
            if (result == "Property has been set.")
                return true;
            else
                return false;
        }
        public virtual void OnAfterStolen(Mobile from)
        {
            SetItemBool(ItemBoolTable.MustSteal, false);
            return;
        }
        #region Monitors
        private List<Mobile> m_monitors = new List<Mobile>(0);   // staff that want to follow progress (not serialized)
        public List<Mobile> Monitors { get { return m_monitors; } }
        public virtual void UpdateMonitors(object status)
        {
            foreach (Mobile m in Monitors)
            {
                if (m != null && m.NetState != null)
                {
                    if (status is string)
                    {
                        m.SendMessage(status as string);
                    }
                }
            }
        }
        #endregion Monitors
        public int CompareTo(IEntity other)
        {
            if (other == null)
                return -1;

            return m_Serial.CompareTo(other.Serial);
        }

        public int CompareTo(Item other)
        {
            return this.CompareTo((IEntity)other);
        }

        public int CompareTo(object other)
        {
            if (other == null || other is IEntity)
                return this.CompareTo((IEntity)other);

            throw new ArgumentException();
        }


        public static readonly List<Item> EmptyItems = new List<Item>();

        private Serial m_Serial;
        private Point3D m_Location;
        private int m_ItemID;
        private int m_Hue;
        private int m_Amount;
        private Layer m_Layer;
        private string m_Name;
        private object m_Parent; // Mobile, Item, or null=World
        private List<Item> m_Items;
        private double m_Weight;
        private int m_TotalItems;
        private int m_TotalWeight;
        private int m_TotalGold;
        private Map m_Map;
        private LootType m_LootType;
        private DateTime m_LastMovedTime;   // 
        private DateTime m_LastAccessed;    // for freezedry stuff, not serialized
        private Direction m_Direction;
        private UInt32 m_QuestCode;

        private BounceInfo m_Bounce;

        private ItemDelta m_DeltaFlags;
        private Item.ItemBoolTable m_BoolTable;

        #region Packet caches
        private Packet m_WorldPacket;
        private Packet m_RemovePacket;

        private Packet m_OPLPacket;
        private ObjectPropertyList m_PropertyList;
        #endregion


        #region Spawner
        Spawner m_Spawner = null;

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner Spawner
        {
            get { return m_Spawner; }
            set { m_Spawner = value; }
        }
        #endregion Spawner

        #region Genesis 
        [Flags]
        public enum Genesis : byte
        {
            Unknown = 0x00,
            Monster = 0x01,     // monster corpse
            Chest = 0x02,     // treasure chests of some type (not treasure map, or dungeon chest)
            Player = 0x04,     // player created
            Scroll = 0x08,     // monster loot or chest loot converted to scroll
            Legacy = 0x10,     // legacy magic loot
            BOD = 0x20,     // BOD reward
            TChest = 0x40,      // treasure map chests 
            DChest = 0x80,      // Dungeon treasure chests 
        }
        private Genesis m_Genesis = Genesis.Unknown;
        [CommandProperty(AccessLevel.GameMaster)]
        public Genesis Origin { get { return m_Genesis; } set { m_Genesis = value; } }
        #endregion Genesis 

        // allow items to relay status back to the GM (see Utilities for a version without spam.)
        public void SendSystemMessage(string text, AccessLevel accesslevel = AccessLevel.GameMaster, int hue = 0x3B2)
        {
            if (text != null)
            {
                IPooledEnumerable eable = this.GetMobilesInRange(10);
                foreach (Mobile m in eable)
                    if (m.AccessLevel >= accesslevel)
                        m.SendMessage(hue, text);
                eable.Free();
            }
        }

        private double m_DropRate = 1.0;    // 100% drop rate by default. Not serialized if 100%

        [CommandProperty(AccessLevel.GameMaster)]
        public double DropRate
        {
            get
            {
                return m_DropRate;
            }

            set
            {
                m_DropRate = value;
            }
        }


#if DEBUG
#if false
// wea: 13/Mar/2007 Added new m_RareData property + various
        // accessors to shift the int for its data
        private UInt32 m_RareData;

        [CommandProperty(AccessLevel.Owner)]
        public UInt32 RareData
        {
            get
            {
                return m_RareData;
            }

            set
            {
                m_RareData = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public UInt32 RareCurIndex
        {
            get
            {
                return (m_RareData > 0 ? m_RareData & 0xFF : 0);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public UInt32 RareStartIndex
        {
            get
            {
                return (m_RareData > 0 ? (m_RareData >> 8) & 0xFF : 0);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public UInt32 RareLastIndex
        {
            get
            {
                return (m_RareData >> 16) & 0xFF;
            }
        }
#endif
#else
#if false
        public UInt32 RareData
        {
            get
            {
                return m_RareData;
            }

            set
            {
                m_RareData = value;
            }
        }

        public UInt32 RareCurIndex
        {
            get
            {
                return (m_RareData > 0 ? m_RareData & 0xFF : 0);
            }
        }

        public UInt32 RareStartIndex
        {
            get
            {
                return (m_RareData > 0 ? (m_RareData >> 8) & 0xFF : 0);
            }
        }

        public UInt32 RareLastIndex
        {
            get
            {
                return (m_RareData >> 16) & 0xFF;
            }
        }
#endif
#endif

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsStaffOwned
        {
            get { return GetItemBool(ItemBoolTable.StaffOwned); }
            set { SetItemBool(ItemBoolTable.StaffOwned, value); InvalidateProperties(); }
        }
#if DEBUG
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Audited
        {
            get { return GetItemBool(ItemBoolTable.Audited); }
            set { SetItemBool(ItemBoolTable.Audited, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool OnSpawner
        {
            get { return GetItemBool(ItemBoolTable.OnSpawner); }
            set { SetItemBool(ItemBoolTable.OnSpawner, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ToDelete
        {
            get { return GetItemBool(ItemBoolTable.ToDelete); }
            set { SetItemBool(ItemBoolTable.ToDelete, value); InvalidateProperties(); }
        }
#else
        public bool Audited
        {
            get { return GetItemBool(ItemBoolTable.Audited); }
            set { SetItemBool(ItemBoolTable.Audited, value); InvalidateProperties(); }
        }

        public bool ToDelete
        {
            get { return GetItemBool(ItemBoolTable.ToDelete); }
            set { SetItemBool(ItemBoolTable.ToDelete, value); InvalidateProperties(); }
        }
#endif
        public bool TrapOnDelete
        {
            get { return GetItemBool(ItemBoolTable.TrapOnDelete); }
            set { SetItemBool(ItemBoolTable.TrapOnDelete, value); }
        }

        private int m_TempFlags, m_SavedFlags;

        public int TempFlags
        {
            get { return m_TempFlags; }
            set { m_TempFlags = value; }
        }

        public int SavedFlags
        {
            get { return m_SavedFlags; }
            set { m_SavedFlags = value; }
        }

        [CommandProperty(AccessLevel.Seer)]
        public virtual bool Debug
        {
            get
            {
                return GetItemBool(ItemBoolTable.Debug);
            }
            set
            {
                SetItemBool(ItemBoolTable.Debug, value);
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public Container ParentContainer
        {
            get
            {   // do not go up to the root
                return m_Parent as Container;
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public Mobile ParentMobile
        {
            get
            {
                return RootParent as Mobile;
            }
        }

        private Mobile m_HeldBy;

        /// <summary>
        /// The <see cref="Mobile" /> who is currently <see cref="Mobile.Holding">holding</see> this item.
        /// </summary>
        [CopyableAttribute(CopyType.DoNotCopy)]
        public Mobile HeldBy
        {
            get { return m_HeldBy; }
            set { m_HeldBy = value; }
        }

        [Flags]
        public enum ItemBoolTable : UInt64
        {
            None = 0x00000000,
            Visible = 0x00000001,
            Movable = 0x00000002,
            Deleted = 0x00000004,
            Stackable = 0x00000008,
            InQueue = 0x00000010,
            PlayerCrafted = 0x00000020, // Adam: Is this Player crafted?
            IsIntMapStorage = 0x00000040, // Adam: Is Internal Map Storage?
            Enchanted = 0x00000080, // Adam: This item was constructed from an Enchanted Scroll
            Debug = 0x00000100,
            IsTemplate = 0x00000200, // is this a template item (used in spawners) 
            ToDelete = 0x00000400, // Used by Cron. Item deletions happen in two passes: 1) mark it, 2) next tick if marked, delete
            NeighborInform = 0x00000800, // Will this item inform neighboring items of some action?
            HideAttributes = 0x00001000, // suppress all weapon/armor attributes?
            Audited = 0x00002000, // Adam: Wealth Tracker system use ONLY
            VileBlade = 0x00004000, // Adam: Ethics system
            HolyBlade = 0x00008000, // Adam: Ethics system
            StoreBought = 0x00010000, // Adam: purchased from an NPC (used in smelting)
            IsRunning = 0x00020000, // All apps can now use this book for your 'Running State'
            TrapOnDelete = 0x00040000, // Debuging: Set the breakpoint in Delete
            StaffOwned = 0x00080000, // Similar to IsIntMapStorage, StaffOwned instructs Cron to ignore this when deleting items
            OnSpawner = 0x00100000, // if we are currently on a spawner, we don't delete this item. SAee Also OnItemLifted, spawner.Spawn/Defrag
            MustSteal = 0x00200000, // This item may be stolen
            RemoveHueOnLift = 0x00400000, // we can now have mobiles with specially hued clothes (and on their corpse) without players getting them
            NoScroll = 0x00800000, // don't convert to scroll
            NormalizeOnLift = 0x01000000, // make special equipped items shard standard on lift
            TempObject = 0x02000000, // this is a temp item and can be cleaned up by Cron
            IsTsItemFreelyAccessible = 0x04000000, // when locked down, anyone can access this items
            DeleteOnLift = 0x08000000, // certain creatures (hire fighters) should have their original items deleted on lift
            Orphaned = 0x10000000, // this item has been explicitly orphaned which overrides checks in Cron cleanup forcing the delete (MountItem for example.)
            Preview = 0x20000000, // Example: This object is in preview mode and can't be 'packed up' via house packup code until placed at which time the flag will be cleared.
            DeleteOnRestart = 0x40000000, // This item will be deleted on server restart
            BSPackOnly = 0x80000000, // [FDBackup sets this bit for addons that we don't want [FDRestore to construct
            BSBackedUp = 0x100000000,// [FDBackup sets this flag on backed up items. Some items need to know this during [FDRestore
            IsLinked = 0x200000000,// this object is linked by another object. In this case, Cron cleanup will leave it alone
        }
        private class CompactInfo
        {
            public string m_Name;

            public List<Item> m_Items;
            public BounceInfo m_Bounce;

            public Mobile m_HeldBy;
            public Mobile m_BlessedFor;

            public ISpawner m_Spawner;

            public int m_TempFlags;
            public int m_SavedFlags;

            public double m_Weight = -1;
        }

        private CompactInfo m_CompactInfo;

        public ExpandFlag GetExpandFlags()
        {
            CompactInfo info = LookupCompactInfo();

            ExpandFlag flags = 0;

            if (info != null)
            {
                if (info.m_BlessedFor != null)
                    flags |= ExpandFlag.Blessed;

                if (info.m_Bounce != null)
                    flags |= ExpandFlag.Bounce;

                if (info.m_HeldBy != null)
                    flags |= ExpandFlag.Holder;

                if (info.m_Items != null)
                    flags |= ExpandFlag.Items;

                if (info.m_Name != null)
                    flags |= ExpandFlag.Name;

                if (info.m_Spawner != null)
                    flags |= ExpandFlag.Spawner;

                if (info.m_SavedFlags != 0)
                    flags |= ExpandFlag.SaveFlag;

                if (info.m_TempFlags != 0)
                    flags |= ExpandFlag.TempFlag;

                if (info.m_Weight != -1)
                    flags |= ExpandFlag.Weight;
            }

            return flags;
        }

        private CompactInfo LookupCompactInfo()
        {
            return m_CompactInfo;
        }

        private CompactInfo AcquireCompactInfo()
        {
            if (m_CompactInfo == null)
                m_CompactInfo = new CompactInfo();

            return m_CompactInfo;
        }

        private void ReleaseCompactInfo()
        {
            m_CompactInfo = null;
        }

        private void VerifyCompactInfo()
        {
            CompactInfo info = m_CompactInfo;

            if (info == null)
                return;

            bool isValid = (info.m_Name != null)
                            || (info.m_Items != null)
                            || (info.m_Bounce != null)
                            || (info.m_HeldBy != null)
                            || (info.m_BlessedFor != null)
                            || (info.m_Spawner != null)
                            || (info.m_TempFlags != 0)
                            || (info.m_SavedFlags != 0)
                            || (info.m_Weight != -1);

            if (!isValid)
                ReleaseCompactInfo();
        }
        public List<Item> LookupItems()
        {
            if (this is Container)
                return (this as Container).m_Items;

            CompactInfo info = LookupCompactInfo();

            if (info != null)
                return info.m_Items;

            return null;
        }

        public List<Item> AcquireItems()
        {
            if (this is Container)
            {
                Container cont = this as Container;

                if (cont.m_Items == null)
                    cont.m_Items = new List<Item>();

                return cont.m_Items;
            }

            CompactInfo info = AcquireCompactInfo();

            if (info.m_Items == null)
                info.m_Items = new List<Item>();

            return info.m_Items;
        }
        public void SetItemBool(Item.ItemBoolTable flag, bool value)
        {
            //if (flag == BoolTable.NoScroll)
            //    ;

            if (value)
                m_BoolTable |= flag;
            else
                m_BoolTable &= ~flag;
        }

        public bool GetItemBool(Item.ItemBoolTable flag)
        {
            return ((m_BoolTable & flag) != 0);
        }
        public void SetItemBools(Item.ItemBoolTable flags)
        {
            m_BoolTable = flags;
        }

        public Item.ItemBoolTable GetItemBools()
        {
            return m_BoolTable;
        }
        /// <summary>
        /// Used by CopyProperties to copy the items bools
        /// </summary>
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public Item.ItemBoolTable BoolTable
        {
            get { return m_BoolTable; }
            set { m_BoolTable = value; }
        }
        public BounceInfo GetBounce()
        {
            return m_Bounce;
        }

        public void RecordBounce()
        {
            m_Bounce = new BounceInfo(this);
        }

        public void ClearBounce()
        {
            BounceInfo bounce = m_Bounce;

            if (bounce != null)
            {
                m_Bounce = null;

                if (bounce.m_Parent is Item)
                {
                    Item parent = (Item)bounce.m_Parent;

                    if (!parent.Deleted)
                        parent.OnItemBounceCleared(this);
                }
                else if (bounce.m_Parent is Mobile)
                {
                    Mobile parent = (Mobile)bounce.m_Parent;

                    if (!parent.Deleted)
                        parent.OnItemBounceCleared(this);
                }
            }
        }
        public virtual bool IsRunning
        {
            get
            {
                return GetItemBool(ItemBoolTable.IsRunning);
            }
            set
            {
                SetItemBool(ItemBoolTable.IsRunning, value);
            }
        }
        /// <summary>
        /// Overridable. virtual method indicating this temporary item has expired and can be cleaned up by the normal decay/cleanup Cron routines
        /// </summary>
        /// <returns>true if this method can be deleted</returns>
        public virtual bool CanDelete()
        {    // default expiration date. Override if you want a different date and possibly other conditions
            bool canDelete = GetItemBool(ItemBoolTable.TempObject) && !Deleted && DateTime.UtcNow > Created + TimeSpan.FromMinutes(5);
            if (canDelete == false)
                // orphaned object, should be deleted
                canDelete = GetItemBool(ItemBoolTable.Orphaned);
            return canDelete;
        }
        /// <summary>
        /// Overridable. Virtual event invoked when a client, <paramref name="from" />, invokes a 'help request' for the Item. Seemingly no longer functional in newer clients.
        /// </summary>
        public virtual void OnHelpRequest(Mobile from)
        {
        }

        /// <summary>
        /// Overridable. Method checked to see if the item can be traded.
        /// </summary>
        /// <returns>True if the trade is allowed, false if not.</returns>
        public virtual bool AllowSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
            return true;
        }

        /// <summary>
        /// Overridable. Virtual event invoked when a trade has completed, either successfully or not.
        /// </summary>
        public virtual void OnSecureTrade(Mobile from, Mobile to, Mobile newOwner, bool accepted)
        {
        }

        /// <summary>
        /// Overridable. Method checked to see if the elemental resistances of this Item conflict with another Item on the <see cref="Mobile" />.
        /// </summary>
        /// <returns>
        /// <list type="table">
        /// <item>
        /// <term>True</term>
        /// <description>There is a confliction. The elemental resistance bonuses of this Item should not be applied to the <see cref="Mobile" /></description>
        /// </item>
        /// <item>
        /// <term>False</term>
        /// <description>There is no confliction. The bonuses should be applied.</description>
        /// </item>
        /// </list>
        /// </returns>
        public virtual bool CheckPropertyConfliction(Mobile m)
        {
            return false;
        }

        /// <summary>
        /// Overridable. Sends the <see cref="PropertyList">object property list</see> to <paramref name="from" />.
        /// </summary>
        public virtual void SendPropertiesTo(Mobile from)
        {
            from.Send(PropertyList);
        }

        /// <summary>
        /// Overridable. Adds the name of this item to the given <see cref="ObjectPropertyList" />. This method should be overriden if the item requires a complex naming format.
        /// </summary>
        public virtual void AddNameProperty(ObjectPropertyList list)
        {
            string name = this.Name;

            if (name == null)
            {   // it's one of our custom labels - can't bass thos the client
                if (LabelNumber < 500000 && Text.Cliloc.Lookup.ContainsKey(LabelNumber))
                {
                    if (m_Amount <= 1)
                        list.Add(Text.Cliloc.Lookup[LabelNumber]);
                    else
                        list.Add(1050039, "{0}\t{1}", m_Amount, Text.Cliloc.Lookup[LabelNumber]); // ~1_NUMBER~ ~2_ITEMNAME~
                }
                else
                {
                    if (m_Amount <= 1)
                        list.Add(LabelNumber);
                    else
                        list.Add(1050039, "{0}\t#{1}", m_Amount, LabelNumber); // ~1_NUMBER~ ~2_ITEMNAME~
                }
            }
            else
            {
                if (m_Amount <= 1)
                    list.Add(name);
                else
                    list.Add(1050039, "{0}\t{1}", m_Amount, name); // ~1_NUMBER~ ~2_ITEMNAME~
            }
        }

        /// <summary>
        /// Overridable. Adds the loot type of this item to the given <see cref="ObjectPropertyList" />. By default, this will be either 'blessed', 'cursed', or 'insured'.
        /// </summary>
        public virtual void AddLootTypeProperty(ObjectPropertyList list)
        {
            if (GetFlag(LootType.Blessed))
                list.Add(1038021); // blessed
            else if (GetFlag(LootType.Cursed))
                list.Add(1049643); // cursed
                                   //else if ( Insured )
                                   //list.Add( 1061682 ); // <b>insured</b>
        }
        /*
				/// <summary>
				/// Overridable. Adds any elemental resistances of this item to the given <see cref="ObjectPropertyList" />.
				/// </summary>
				public virtual void AddResistanceProperties( ObjectPropertyList list )
				{
					int v = PhysicalResistance;

					if ( v != 0 )
						list.Add( 1060448, v.ToString() ); // physical resist ~1_val~%

					v = FireResistance;

					if ( v != 0 )
						list.Add( 1060447, v.ToString() ); // fire resist ~1_val~%

					v = ColdResistance;

					if ( v != 0 )
						list.Add( 1060445, v.ToString() ); // cold resist ~1_val~%

					v = PoisonResistance;

					if ( v != 0 )
						list.Add( 1060449, v.ToString() ); // poison resist ~1_val~%

					v = EnergyResistance;

					if ( v != 0 )
						list.Add( 1060446, v.ToString() ); // energy resist ~1_val~%
				}
		*/
        /// <summary>
        /// Overridable. Adds header properties. By default, this invokes <see cref="AddNameProperty" />, <see cref="AddBlessedForProperty" /> (if applicable), and <see cref="AddLootTypeProperty" /> (if <see cref="DisplayLootType" />).
        /// </summary>
        public virtual void AddNameProperties(ObjectPropertyList list)
        {
            AddNameProperty(list);

            if (IsSecure)
                AddSecureProperty(list);
            else if (IsLockedDown)
                AddLockedDownProperty(list);

            if (m_BlessedFor != null && !m_BlessedFor.Deleted)
                AddBlessedForProperty(list, m_BlessedFor);

            if (DisplayLootType)
                AddLootTypeProperty(list);

            AppendChildNameProperties(list);
        }

        /// <summary>
        /// Overridable. Adds the "Locked Down & Secure" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddSecureProperty(ObjectPropertyList list)
        {
            list.Add(501644); // locked down & secure
        }

        /// <summary>
        /// Overridable. Adds the "Locked Down" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddLockedDownProperty(ObjectPropertyList list)
        {
            list.Add(501643); // locked down
        }

        /// <summary>
        /// Overridable. Adds the "Blessed for ~1_NAME~" property to the given <see cref="ObjectPropertyList" />.
        /// </summary>
        public virtual void AddBlessedForProperty(ObjectPropertyList list, Mobile m)
        {
            list.Add(1062203, "{0}", m.Name); // Blessed for ~1_NAME~
        }

        /// <summary>
        /// Overridable. Fills an <see cref="ObjectPropertyList" /> with everything applicable. By default, this invokes <see cref="AddNameProperties" />, then <see cref="Item.GetChildProperties">Item.GetChildProperties</see> or <see cref="Mobile.GetChildProperties">Mobile.GetChildProperties</see>. This method should be overriden to add any custom properties.
        /// </summary>
        public virtual void GetProperties(ObjectPropertyList list)
        {
            AddNameProperties(list);
        }

        /// <summary>
        /// Overridable. Event invoked when a child (<paramref name="item" />) is building it's <see cref="ObjectPropertyList" />. Recursively calls <see cref="Item.GetChildProperties">Item.GetChildProperties</see> or <see cref="Mobile.GetChildProperties">Mobile.GetChildProperties</see>.
        /// </summary>
        public virtual void GetChildProperties(ObjectPropertyList list, Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).GetChildProperties(list, item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).GetChildProperties(list, item);
        }

        /// <summary>
        /// Overridable. Event invoked when a child (<paramref name="item" />) is building it's Name <see cref="ObjectPropertyList" />. Recursively calls <see cref="Item.GetChildProperties">Item.GetChildNameProperties</see> or <see cref="Mobile.GetChildProperties">Mobile.GetChildNameProperties</see>.
        /// </summary>
        public virtual void GetChildNameProperties(ObjectPropertyList list, Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).GetChildNameProperties(list, item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).GetChildNameProperties(list, item);
        }

        public void Bounce(Mobile from)
        {
            LogHelper logger = null;
            try
            {
                if (m_Parent is Item)
                    ((Item)m_Parent).RemoveItem(this);
                else if (m_Parent is Mobile)
                    ((Mobile)m_Parent).RemoveItem(this);

                m_Parent = null;

                if (m_Bounce != null)
                {
                    object parent = m_Bounce.m_Parent;

                    if (parent is Item && !((Item)parent).Deleted)
                    {
                        Item p = (Item)parent;
                        object root = p.RootParent;

                        //Pixie: 01/05/2006
                        // Added bounceLocation check so that if the location of the parent or
                        // rootparent is out of range of the mobile, then we drop to mobile's feet instead.
                        Point3D bounceLocation = p.Location;
                        if (root != null)
                        {
                            if (root is Mobile)
                            {
                                bounceLocation = ((Mobile)root).Location;
                            }
                            else if (root is Item)
                            {
                                bounceLocation = ((Item)root).Location;
                            }
                        }

                        bool bounceLocationInRange = Utility.InRange(from.Location, bounceLocation, 3);

                        // 7/19/21, Adam: unwound this madness into intelligible rules, and added rule 3
                        /* if(bounceLocationInRange
                            && p.IsAccessibleTo(from)
                            && (!(root is Mobile) || ((Mobile)root).CheckNonlocalDrop(from, this, p))
                            // note to taran kain: probably should remove secure restriction here in the future!
                            && (!(p is Container) || (!((Container)p).IsSecure || (((Container)p).IsSecure && ((Container)p).CheckHold(from, this, false)))))*/

                        bool rule1 = bounceLocationInRange && p.IsAccessibleTo(from) && (!(root is Mobile) || ((Mobile)root).CheckNonlocalDrop(from, this, p));
                        bool rule2 = (!(p is Container) || (!((Container)p).IsSecure || (((Container)p).IsSecure && ((Container)p).CheckHold(from, this, false))));
                        bool rule3 = (!(p is Container) || (p as Container).Items.Count < (p as Container).MaxItems);                   // overloading items
                        bool rule4 = (!(p is Container) || !(root is Mobile) || ((root is Mobile) && !IsOverloaded(root as Mobile)));  // overloading weight


                        if (rule3 == false)
                        {   // log the possible exploit
                            if (logger == null)
                                logger = new LogHelper("BounceRecorder.log", false);
                            string text = string.Format("backpack item count:{0}, backpack maxitems:{1}", (p as Container).Items.Count, (p as Container).MaxItems);
                            Console.WriteLine("{0} " + text, (root as Mobile)/*can be null*/);
                            logger.Log(LogType.Mobile, (root as Mobile), text);
                        }

                        if (rule4 == false)
                        {   // log the possible exploit
                            if (logger == null)
                                logger = new LogHelper("BounceRecorder.log", false);
                            string text = string.Format("Player weight:{0}, Player max weight:{1}", GetWeight(root as Mobile), GetMaxWeight(root as Mobile));
                            Console.WriteLine("{0} " + text, (root as Mobile)/*can be null*/);
                            logger.Log(LogType.Mobile, (root as Mobile), text);
                        }

                        /*if (p is Container)
                            if ((p as Container).Items.Count < (p as Container).MaxItems)
                            {
                                Console.WriteLine("backpack item count:{0}, backpack maxitems:{1}", (p as Container).Items.Count , (p as Container).MaxItems);
                                rule3 = true;
                            }
                            else
                                rule3 = false;*/

                        if (rule1 && rule2 && rule3 && rule4)
                        {
                            Location = m_Bounce.m_Location;
                            p.AddItem(this);
                        }
                        else
                        {
                            MoveToWorld(from.Location, from.Map);
                            OnBouncedToWorld();
                        }
                    }
                    else if (parent is Mobile && !((Mobile)parent).Deleted)
                    {
                        if (!((Mobile)parent).EquipItem(this))
                        {
                            MoveToWorld(m_Bounce.m_WorldLoc, m_Bounce.m_Map);
                            OnBouncedToWorld();
                        }
                    }
                    else
                    {
                        MoveToWorld(m_Bounce.m_WorldLoc, m_Bounce.m_Map);
                        OnBouncedToWorld();
                    }
                }
                else
                {
                    MoveToWorld(from.Location, from.Map);
                    OnBouncedToWorld();
                }

                ClearBounce();
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
            finally
            {
                if (logger != null)
                    logger.Finish();
            }
        }

        public virtual void OnBouncedToWorld()
        {
            if (Map != null && Parent == null)
                Map.FixColumn(X, Y);
        }

        public static bool IsOverloaded(Mobile m)
        {
            if (!m.Player || !m.Alive || m.AccessLevel >= AccessLevel.GameMaster)
                return false;

            return ((Mobile.BodyWeight + m.TotalWeight) > (GetMaxWeight(m) + OverloadAllowance));
        }

        public const int OverloadAllowance = 4; // We can be four stones overweight without getting fatigued

        public static int GetWeight(Mobile m)
        {
            return (Mobile.BodyWeight + m.TotalWeight);
        }
        public static int GetMaxWeight(Mobile m)
        {
            return 40 + (int)(3.5 * m.Str);
        }

        /// <summary>
        /// Overridable. Method checked to see if this item may be equiped while casting a spell. By default, this returns false. It is overriden on spellbook and spell channeling weapons or shields.
        /// </summary>
        /// <returns>True if it may, false if not.</returns>
        /// <example>
        /// <code>
        ///	public override bool AllowEquipedCast( Mobile from )
        ///	{
        ///		if ( from.Int &gt;= 100 )
        ///			return true;
        ///		
        ///		return base.AllowEquipedCast( from );
        /// }</code>
        /// 
        /// When placed in an Item script, the item may be cast when equiped if the <paramref name="from" /> has 100 or more intelligence. Otherwise, it will drop to their backpack.
        /// </example>
        public virtual bool AllowEquipedCast(Mobile from)
        {
            return false;
        }

        public virtual bool CheckConflictingLayer(Mobile m, Item item, Layer layer)
        {
            return (m_Layer == layer);
        }

        public virtual bool CanEquip(Mobile m)
        {
            return (m_Layer != Layer.Invalid && m.FindItemOnLayer(m_Layer) == null);
        }

        public virtual void GetChildContextMenuEntries(Mobile from, ArrayList list, Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).GetChildContextMenuEntries(from, list, item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).GetChildContextMenuEntries(from, list, item);
        }

        public virtual void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).GetChildContextMenuEntries(from, list, this);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).GetChildContextMenuEntries(from, list, this);
        }

        public virtual bool VerifyMove(Mobile from)
        {
            return Movable;
        }

        public virtual DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (!Movable)
                return DeathMoveResult.RemainEquiped;
            else if (parent.KeepsItemsOnDeath)
                return DeathMoveResult.MoveToBackpack;
            else if (CheckBlessed(parent))
                return DeathMoveResult.MoveToBackpack;
            else if (CheckNewbied() && parent.LongTermMurders < 5)
                return DeathMoveResult.MoveToBackpack;
            else if (GetFlag(LootType.UnLootable))
                return DeathMoveResult.MoveToBackpack;
            else
                return DeathMoveResult.MoveToCorpse;
        }

        public virtual DeathMoveResult OnInventoryDeath(Mobile parent)
        {
            if (!Movable)
                return DeathMoveResult.MoveToBackpack;
            else if (parent.KeepsItemsOnDeath)
                return DeathMoveResult.MoveToBackpack;
            else if (CheckBlessed(parent))
                return DeathMoveResult.MoveToBackpack;
            else if (CheckNewbied() && parent.LongTermMurders < 5)
                return DeathMoveResult.MoveToBackpack;
            else if (GetFlag(LootType.UnLootable))
                return DeathMoveResult.MoveToBackpack;
            else
                return DeathMoveResult.MoveToCorpse;
        }

        /// <summary>
        /// Moves the Item to <paramref name="location" />. The Item does not change maps.
        /// </summary>
        public virtual void MoveToWorld(Point3D location)
        {
            MoveToWorld(location, m_Map);
        }

        public void LabelTo(Mobile to, int number)
        {
            to.Send(new MessageLocalized(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, number, "", ""));
        }

        public void LabelTo(Mobile to, int number, string args)
        {
            to.Send(new MessageLocalized(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, number, "", args));
        }

        public void LabelTo(Mobile to, string text)
        {
            to.Send(new UnicodeMessage(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", text));
        }

        public void LabelTo(Mobile to, string format, params object[] args)
        {
            LabelTo(to, String.Format(format, args));
        }

        public void LabelToAffix(Mobile to, int number, AffixType type, string affix)
        {
            to.Send(new MessageLocalizedAffix(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, number, "", type, affix, ""));
        }

        public void LabelToAffix(Mobile to, int number, AffixType type, string affix, string args)
        {
            to.Send(new MessageLocalizedAffix(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, number, "", type, affix, args));
        }

        public void LabelToHued(Mobile to, int number, int hue)
        {
            to.Send(new MessageLocalized(m_Serial, m_ItemID, MessageType.Label, hue, 3, number, "", ""));
        }

        public void LabelToHued(Mobile to, int number, int hue, string args)
        {
            to.Send(new MessageLocalized(m_Serial, m_ItemID, MessageType.Label, hue, 3, number, "", args));
        }

        public void LabelToHued(Mobile to, string text, int hue)
        {
            to.Send(new UnicodeMessage(m_Serial, m_ItemID, MessageType.Label, hue, 3, "ENU", "", text));
        }

        public void LabelToHued(Mobile to, string text, int hue, params object[] args)
        {
            LabelToHued(to, String.Format(text, args), hue);
        }

        public virtual void LabelLootTypeTo(Mobile to)
        {
            if (GetFlag(LootType.Blessed))
                LabelTo(to, 1041362); // (blessed)
            else if (GetFlag(LootType.Cursed))
                LabelTo(to, "(cursed)");
        }

        public bool AtWorldPoint(int x, int y)
        {
            return (m_Parent == null && m_Location.m_X == x && m_Location.m_Y == y);
        }

        public bool AtPoint(int x, int y)
        {
            return (m_Location.m_X == x && m_Location.m_Y == y);
        }

        /// <summary>
        /// Moves the Item to a given <paramref name="location" /> and <paramref name="map" />.
        /// </summary>
        public virtual void MoveToWorld(Point3D location, Map map, Mobile responsible = null)
        {
            if (Deleted)
                return;

            Point3D oldLocation = GetWorldLocation();
            Point3D oldRealLocation = m_Location;

            if (Core.RuleSets.SiegeStyleRules())
                // currently only Mortalis and Siege have tracked items
                if (GetFlag(LootType.SiegeBlessed))
                    if (location != Point3D.Zero)
                        if (this.BlessedFor != null)
                            this.BlessedFor.TrackedItemParentChanging(this);

            SetLastMoved();

            if (Parent is Mobile)
                ((Mobile)Parent).RemoveItem(this);
            else if (Parent is Item)
                ((Item)Parent).RemoveItem(this);

            if (m_Map != map)
            {
                Map old = m_Map;

                if (m_Map != null)
                {
                    m_Map.OnLeave(this);

                    if (oldLocation.m_X != 0)
                    {
                        Packet remPacket = null;

                        IPooledEnumerable eable = m_Map.GetClientsInRange(oldLocation, GetMaxUpdateRange());

                        foreach (NetState state in eable)
                        {
                            Mobile m = state.Mobile;

                            if (m.InRange(oldLocation, GetUpdateRange(m)))
                            {
                                if (remPacket == null)
                                    remPacket = this.RemovePacket;

                                state.Send(remPacket);
                            }
                        }

                        eable.Free();
                    }
                }

                m_Location = location;
                this.OnLocationChange(oldRealLocation);

                Packet.Release(ref m_WorldPacket);

                if (m_Items != null)
                {
                    for (int i = 0; i < m_Items.Count; ++i)
                        ((Item)m_Items[i]).Map = map;
                }

                m_Map = map;

                if (m_Map != null)
                    m_Map.OnEnter(this);

                OnMapChange();

                if (m_Map != null)
                {
                    IPooledEnumerable eable = m_Map.GetClientsInRange(m_Location, GetMaxUpdateRange());
                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(m_Location, GetUpdateRange(m)))
                            SendInfoTo(state);
                    }
                    eable.Free();

                    // don't waste time on these (think champ spawn)
                    if (this is not Blood && this is not Corpse)
                    {
                        int mobileUpdateRange = 5;
                        // 7/12/2023, Adam: In RunUO, only net states are enumerated, on our shards, we enumerate all mobiles
                        //  This is because certain of our mobiles care when an item is spawned/dropped nearby.
                        eable = m_Map.GetMobilesInRange(m_Location, mobileUpdateRange);
                        foreach (Mobile m in eable)
                        {
                            if (!m.Player && m.CanSee(this) && m.InRange(m_Location, mobileUpdateRange))
                            {
                                m.OnSee(responsible, this);
                            }
                        }
                        eable.Free();
                    }
                }

                RemDelta(ItemDelta.Update);

                if (old == null || old == Map.Internal)
                    InvalidateProperties();
            }
            else if (m_Map != null)
            {
                IPooledEnumerable eable;

                if (oldLocation.m_X != 0)
                {
                    Packet removeThis = null;

                    eable = m_Map.GetClientsInRange(oldLocation, GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (!m.InRange(location, GetUpdateRange(m)))
                        {
                            if (removeThis == null)
                                removeThis = this.RemovePacket;

                            state.Send(removeThis);
                        }
                    }

                    eable.Free();
                }

                Point3D oldInternalLocation = m_Location;

                m_Location = location;
                this.OnLocationChange(oldRealLocation);

                Packet.Release(ref m_WorldPacket);

                eable = m_Map.GetClientsInRange(m_Location, GetMaxUpdateRange());

                foreach (NetState state in eable)
                {
                    Mobile m = state.Mobile;

                    if (m.CanSee(this) && m.InRange(m_Location, GetUpdateRange(m)))
                        SendInfoTo(state);
                }

                eable.Free();

                m_Map.OnMove(oldInternalLocation, this);

                RemDelta(ItemDelta.Update);
            }
            else
            {
                Map = map;
                Location = location;
            }
        }

        Point3D IEntity.Location
        {
            get
            {
                return m_Location;
            }
        }

        /// <summary>
        /// Has the item been deleted?
        /// </summary>
        [CommandProperty(AccessLevel.Owner)]
        public bool Deleted
        {
            get { return GetItemBool(ItemBoolTable.Deleted); }
            set { if (value == true) this.Delete(); InvalidateProperties(); }
        }
        #region LootType
        [CommandProperty(AccessLevel.GameMaster)]
        public LootType LootType
        {
            get
            {
                return m_LootType;
            }
            set
            {
                if (m_LootType != value && value != LootType.SiegeBlessed)
                {
                    // ApiKey prevents GMs from setting SiegeBlessed. SiegeBlessed is managed by BlessedFor
                    if (value == (LootType.ApiKey | LootType.SiegeBlessed))
                        value = LootType.SiegeBlessed;

                    m_LootType = value;

                    if (DisplayLootType)
                        InvalidateProperties();
                }
            }
        }
        public void SetFlag(LootType flag, bool value)
        {
            if (value)
                m_LootType |= flag;
            else
                m_LootType &= ~flag;
        }

        public bool GetFlag(LootType flag)
        {
            return m_LootType.HasFlag(flag);
        }
        #endregion LootType
        private static TimeSpan m_DDT = TimeSpan.FromHours(1.0);

        public static TimeSpan DefaultDecayTime { get { return m_DDT; } set { m_DDT = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual TimeSpan DecayTime
        {
            get
            {
                return m_DDT;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Decays
        {
            get
            {
                return (Movable && Visible);
            }
        }

        public virtual bool OnDecay()
        {
            return (Decays && Parent == null && Map != Map.Internal && Region.Find(Location, Map).OnDecay(this));
        }

        public void SetLastMoved()
        {
            m_LastMovedTime = DateTime.UtcNow;
        }

        public DateTime LastMoved
        {
            get
            {
                return m_LastMovedTime;
            }
            set
            {
                m_LastMovedTime = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime LastAccessed
        {
            get
            {
                return m_LastAccessed;
            }
            set
            {
                m_LastAccessed = value;
            }
        }

        public virtual bool StackWith(Mobile from, Item dropped)
        {
            return StackWith(from, dropped, true);
        }

        /*public bool CanStackWith(Mobile from, Item dropped)
        {
            if (Stackable && dropped.Stackable && dropped.GetType() == GetType() && dropped.ItemID == ItemID && dropped.Name == Name && dropped.Hue == Hue && (dropped.Amount + Amount) <= 60000)
                return true;
            else
                return false;
        }*/
        [CommandProperty(AccessLevel.GameMaster)]
        public string TypeName
        {
            get
            {
                if (this.GetType().IsAssignableTo(typeof(StackableItem)))
                {   // emulate a type when we are a StackableItem
                    string text = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(GetBaseOldName());
                    return text.Replace(" ", "");
                }

                return this.GetType().Name;
            }
        }
        public virtual bool EquivelentItemID(int itemID)
        {   // we use to allow server-side stacking of items not supported by the client.
            //  See HealingHerbs for and example.
            //  Note, since splitting stacks is handled client-side, to split these stacks we can use scissors or some other server-side solution.
            List<int> ids = new() { ItemID };
            return (ids.Contains(itemID));
        }
        public static bool StackableRule(Item dest, Item src)
        {
            return (
                dest.Stackable && src.Stackable &&
                dest.GetType() == src.GetType() &&
                ((dest.ItemID == src.ItemID) || dest.EquivelentItemID(src.ItemID)) &&
                dest.Name == src.Name &&
                dest.Hue == src.Hue &&
                (dest.Amount + src.Amount) <= 60000 &&
                (dest.Movable || dest.IsLockedDown));
        }
        public virtual bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            if (StackableRule(this, dropped))
            {
                if (m_LootType != dropped.m_LootType)
                    m_LootType = LootType.Regular;

                Amount += dropped.Amount;
                dropped.Delete();

                if (playSound)
                {
                    int soundID = GetDropSound();

                    if (soundID == -1)
                        soundID = 0x42;

                    from.SendSound(soundID, GetWorldLocation());
                }

                return true;
            }

            return false;
        }

        public virtual bool OnDragDrop(Mobile from, Item dropped)
        {
            if (Parent is Container)
                return ((Container)Parent).OnStackAttempt(from, this, dropped);

            return StackWith(from, dropped);
        }

        public Rectangle2D GetGraphicBounds()
        {
            int itemID = m_ItemID;
            bool doubled = m_Amount > 1;

            if (itemID >= 0xEEA && itemID <= 0xEF2) // Are we coins?
            {
                int coinBase = (itemID - 0xEEA) / 3;
                coinBase *= 3;
                coinBase += 0xEEA;

                doubled = false;

                if (m_Amount <= 1)
                {
                    // A single coin
                    itemID = coinBase;
                }
                else if (m_Amount <= 5)
                {
                    // A stack of coins
                    itemID = coinBase + 1;
                }
                else // m_Amount > 5
                {
                    // A pile of coins
                    itemID = coinBase + 2;
                }
            }

            Rectangle2D bounds = ItemBounds.Table[itemID & 0x3FFF];

            if (doubled)
            {
                bounds.Set(bounds.X, bounds.Y, bounds.Width + 5, bounds.Height + 5);
            }

            return bounds;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Stackable
        {
            get { return GetItemBool(ItemBoolTable.Stackable); }
            set { SetItemBool(ItemBoolTable.Stackable, value); }
        }

        public Packet RemovePacket
        {
            get
            {
                if (m_RemovePacket == null)
                {
                    m_RemovePacket = new RemoveItem(this);
                    m_RemovePacket.SetStatic();
                }

                return m_RemovePacket;
            }
        }

        public Packet OPLPacket
        {
            get
            {
                if (m_OPLPacket == null)
                {
                    m_OPLPacket = new OPLInfo(PropertyList);
                    m_OPLPacket.SetStatic();
                }

                return m_OPLPacket;
            }
        }

        public ObjectPropertyList PropertyList
        {
            get
            {
                if (m_PropertyList == null)
                {
                    m_PropertyList = new ObjectPropertyList(this);

                    GetProperties(m_PropertyList);
                    AppendChildProperties(m_PropertyList);

                    m_PropertyList.Terminate();
                    m_PropertyList.SetStatic();
                }

                return m_PropertyList;
            }
        }

        public virtual void AppendChildProperties(ObjectPropertyList list)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).GetChildProperties(list, this);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).GetChildProperties(list, this);
        }

        public virtual void AppendChildNameProperties(ObjectPropertyList list)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).GetChildNameProperties(list, this);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).GetChildNameProperties(list, this);
        }

        public void ClearProperties()
        {
            Packet.Release(ref m_PropertyList);
            Packet.Release(ref m_OPLPacket);
        }

        public void InvalidateProperties()
        {
            if (!Core.RuleSets.AOSRules())
                return;

            if (m_Map != null && m_Map != Map.Internal && !World.Loading)
            {
                ObjectPropertyList oldList = m_PropertyList;
                m_PropertyList = null;
                ObjectPropertyList newList = PropertyList;

                if (oldList == null || oldList.Hash != newList.Hash)
                {
                    Packet.Release(ref m_OPLPacket);
                    Delta(ItemDelta.Properties);
                }
            }
            else
            {
                ClearProperties();
            }
        }

        public Packet WorldPacket
        {
            get
            {
                // This needs to be invalidated when any of the following changes:
                //  - ItemID
                //  - Amount
                //  - Location
                //  - Hue
                //  - Packet Flags
                //  - Direction

                if (m_WorldPacket == null)
                {
                    m_WorldPacket = new WorldItem(this);
                    m_WorldPacket.SetStatic();
                }

                return m_WorldPacket;
            }
        }
        #region BaseMulti to which we belong
        private BaseMulti m_BaseMulti = null;
        [CopyableAttribute(CopyType.DoNotCopy)]
        public BaseCamp BaseCamp { get { return m_BaseMulti as BaseCamp; } set { m_BaseMulti = value; } }
        [CopyableAttribute(CopyType.DoNotCopy)]
        public BaseMulti BaseMulti { get { return m_BaseMulti; } set { m_BaseMulti = value; } }
        public virtual bool CampComponent
        {

            get { return BaseCamp != null; }
        }
        #endregion BaseMulti to which we belong

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual bool Visible
        {
            get { return GetItemBool(ItemBoolTable.Visible); }
            set
            {
                if (GetItemBool(ItemBoolTable.Visible) != value)
                {
                    SetItemBool(ItemBoolTable.Visible, value);
                    Packet.Release(ref m_WorldPacket);

                    if (m_Map != null)
                    {
                        Packet removeThis = null;
                        Point3D worldLoc = GetWorldLocation();

                        IPooledEnumerable eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                        foreach (NetState state in eable)
                        {
                            Mobile m = state.Mobile;

                            if (!m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                            {
                                if (removeThis == null)
                                    removeThis = this.RemovePacket;

                                state.Send(removeThis);
                            }
                        }

                        eable.Free();
                    }

                    Delta(ItemDelta.Update);
                }
            }
        }
        public void Hide()
        {   // if it's invisible, remove the packets
            if (GetItemBool(ItemBoolTable.Visible) == false)
            {
                Packet.Release(ref m_WorldPacket);

                if (m_Map != null)
                {
                    Packet removeThis = null;
                    Point3D worldLoc = GetWorldLocation();

                    IPooledEnumerable eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (!m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                        {
                            if (removeThis == null)
                                removeThis = this.RemovePacket;

                            state.Send(removeThis);
                        }
                    }

                    eable.Free();
                }

                Delta(ItemDelta.Update);
            }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Movable
        {
            get { return GetItemBool(ItemBoolTable.Movable); }
            set
            {
                if (GetItemBool(ItemBoolTable.Movable) != value)
                {
                    SetItemBool(ItemBoolTable.Movable, value);
                    Packet.Release(ref m_WorldPacket);
                    Delta(ItemDelta.Update);
                }
            }
        }

        public virtual bool ForceShowProperties { get { return false; } }

        public virtual int GetPacketFlags()
        {
            int flags = 0;

            if (!Visible)
                flags |= 0x80;

            if (Movable || ForceShowProperties)
                flags |= 0x20;

            return flags;
        }

        public virtual bool OnMoveOff(Mobile m)
        {
            return true;
        }

        public virtual bool OnMoveOver(Mobile m)
        {
            return true;
        }

        public virtual bool HandlesOnMovement { get { return false; } }

        public virtual void OnMovement(Mobile m, Point3D oldLocation)
        {
        }

        public void Internalize()
        {
            MoveToWorld(Point3D.Zero, Map.Internal);
        }

        public virtual void OnMapChange()
        {
        }

        public virtual void OnRemoved(object parent)
        {
            if (parent != null && parent is Mobile mob && mob.Region != null)
                mob.Region.OnEquipmentRemoved(parent, this);
        }

        public virtual void OnAdded(object parent)
        {
            if (parent != null && parent is Mobile mob && mob.Region != null)
                mob.Region.OnEquipmentAdded(parent, this);
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public Map Map
        {
            get
            {
                return m_Map;
            }
            set
            {
                if (m_Map != value)
                {
                    Map old = m_Map;

                    if (m_Map != null && m_Parent == null)
                    {
                        m_Map.OnLeave(this);
                        SendRemovePacket();
                    }

                    //					if ( m_Items == null )
                    //						throw new Exception( String.Format( "Items array is null--are you calling the serialization constructor? Type={0}", GetType() ) );

                    if (m_Items != null)
                    {
                        for (int i = 0; i < m_Items.Count; ++i)
                            ((Item)m_Items[i]).Map = value;
                    }

                    m_Map = value;

                    if (m_Map != null && m_Parent == null)
                        m_Map.OnEnter(this);

                    Delta(ItemDelta.Update);

                    this.OnMapChange();

                    if (old == null || old == Map.Internal)
                        InvalidateProperties();
                }
            }
        }
        #region DynamicFeatures [Obsolete]
        /* Obsolete in version 14 of serialization
         * In version 15+ we can remove this code all together.
         * For now (version <= 14) we need the Type to read old values and move them to ImplFlags
         */
#if false
        [Flags, Obsolete]
        public enum FeatureBits_obsolete : UInt32
        {
            None = 0x0,
            CampComponent = 0x01,       // is this a camp addon component?
        };
        private FeatureBits_obsolete m_dynamicFeatures = FeatureBits_obsolete.None;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public FeatureBits_obsolete DynamicFeatures
        {
            get { return m_dynamicFeatures; }
            set { m_dynamicFeatures = value; }
        }
        public bool IsDynamicFeatureSet(FeatureBits_obsolete fb)
        {
            if ((m_dynamicFeatures & fb) > 0) return true;
            else return false;
        }

        public void SetDynamicFeature(FeatureBits_obsolete fb)
        {
            m_dynamicFeatures |= fb;
        }

        public void ClearDynamicFeature(FeatureBits_obsolete fb)
        {
            m_dynamicFeatures &= ~fb;
        }
#endif
        #endregion DynamicFeatures [Obsolete]
        #region SAVE FLAGS
        [Flags]
        private enum SaveFlag : UInt64
        {
            None = 0x00000000,
            Direction = 0x00000001,
            Bounce = 0x00000002,
            LootType = 0x00000004,
            LocationFull = 0x00000008,
            ItemID = 0x00000010,
            Hue = 0x00000020,
            Amount = 0x00000040,
            Layer = 0x00000080,
            Name = 0x00000100,
            Parent = 0x00000200,
            Items = 0x00000400,
            WeightNot1or0 = 0x00000800,
            Map = 0x00001000,
            Visible = 0x00002000,
            Movable = 0x00004000,
            Stackable = 0x00008000,
            WeightIs0 = 0x00010000,
            LocationSByteZ = 0x00020000,
            LocationShortXY = 0x00040000,
            LocationByteXY = 0x00080000,
            ImplFlags = 0x00100000,
            Created = 0x00200000,
            BlessedFor = 0x00400000,
            HeldBy = 0x00800000,
            IntWeight = 0x01000000,
            SavedFlags = 0x02000000,
            Origin = 0x04000000,            // Genesis of this item (corpse, treasure chest, etc.)
            BaseMulti = 0x08000000,         // BaseMulti to which we belong
            DropRate = 0x10000000,          // adam: custom drop rate? Used by the at least the Spawner / Loot Pack system
            Spawner = 0x20000000,           // adam: spawner that spawned this item 
            BoolTable = 0x40000000,         // table of bools
            QuestCode = 0x80000000,         // do we have a quest code to save?
            TempRefCount = 0x100000000,     // How many spawners point to this item
            HasLabel = 0x200000000,         // this object has a custom label
            LastTouched = 0x400000000,      // who last touched(modified) this item and when
        }
        private static void SetSaveFlag(ref SaveFlag flags, SaveFlag toSet, bool value)
        {
            if (value)
                flags |= toSet;
            else
                flags &= ~toSet;
        }

        private static bool GetSaveFlag(SaveFlag flags, SaveFlag toGet)
        {
            return ((flags & toGet) != 0);
        }
        private bool CalcBoolFlags()
        {
            /* BoolTable
             * Our bool table contains a bunch of bit flags which represent boolean values.
             */
            return m_BoolTable != ItemBoolTable.None;
        }
        private SaveFlag CalcSaveFlags()
        {
            SaveFlag flags = SaveFlag.None;
            #region Location Optimizer
            int x = m_Location.m_X, y = m_Location.m_Y, z = m_Location.m_Z;

            if (x != 0 || y != 0 || z != 0)
            {
                if (x >= short.MinValue && x <= short.MaxValue && y >= short.MinValue && y <= short.MaxValue && z >= sbyte.MinValue && z <= sbyte.MaxValue)
                {
                    if (x != 0 || y != 0)
                    {
                        if (x >= byte.MinValue && x <= byte.MaxValue && y >= byte.MinValue && y <= byte.MaxValue)
                            flags |= SaveFlag.LocationByteXY;
                        else
                            flags |= SaveFlag.LocationShortXY;
                    }

                    if (z != 0)
                        flags |= SaveFlag.LocationSByteZ;
                }
                else
                {
                    flags |= SaveFlag.LocationFull;
                }
            }
            #endregion

            if (m_Direction != Direction.North) flags |= SaveFlag.Direction;
            if (m_Bounce != null) flags |= SaveFlag.Bounce;
            if (m_LootType != LootType.Regular) flags |= SaveFlag.LootType;
            if (m_ItemID != 0) flags |= SaveFlag.ItemID;
            if (m_Hue != 0) flags |= SaveFlag.Hue;
            if (m_Amount != 1) flags |= SaveFlag.Amount;
            if (m_Layer != Layer.Invalid) flags |= SaveFlag.Layer;
            if (m_Name != null) flags |= SaveFlag.Name;
            if (m_Parent != null) flags |= SaveFlag.Parent;
            if (m_Items != null && m_Items.Count > 0) flags |= SaveFlag.Items;
            if (m_Map != Map.Internal) flags |= SaveFlag.Map;
            if (m_Created != DateTime.MinValue) flags |= SaveFlag.Created;
            if (m_BlessedFor != null && !m_BlessedFor.Deleted) flags |= SaveFlag.BlessedFor;
            if (m_HeldBy != null && !m_HeldBy.Deleted) flags |= SaveFlag.HeldBy;
            if (m_SavedFlags != 0) flags |= SaveFlag.SavedFlags;
            if (m_BaseMulti != null) flags |= SaveFlag.BaseMulti;
            if (m_DropRate != 1.0) flags |= SaveFlag.DropRate;      //Adam: Spawner Loot Packs
            if (m_Spawner != null) flags |= SaveFlag.Spawner;
            //if (m_dynamicFeatures != FeatureBits_obsolete.None) flags |= SaveFlag.DynamicFeatures;
            if (m_Genesis != Genesis.Unknown) flags |= SaveFlag.Origin;
            if (m_QuestCode != 0) flags |= SaveFlag.QuestCode;
            if (m_TempRefCount != 0) flags |= SaveFlag.TempRefCount;
            if (!string.IsNullOrEmpty(m_Label)) flags |= SaveFlag.HasLabel;
            if (!string.IsNullOrEmpty(m_LastChange)) flags |= SaveFlag.LastTouched;
            if (CalcBoolFlags()) flags |= SaveFlag.BoolTable;

            if (m_Weight == 0.0)
            {
                flags |= SaveFlag.WeightIs0;
            }
            else if (m_Weight != 1.0)
            {
                if (m_Weight == (int)m_Weight)
                    flags |= SaveFlag.IntWeight;
                else
                    flags |= SaveFlag.WeightNot1or0;
            }

            // finally, is there anything special to save
            if (flags != SaveFlag.None)
                flags |= SaveFlag.ImplFlags;

            return flags;
        }
        #endregion SAVE FLAGS
        #region LootType
        public static bool IsLootTypeSet(Item item, LootType lt)
        {
            if ((item.LootType & lt) > 0) return true;
            else return false;
        }

        public static void SetLootType(Item item, LootType lt)
        {
            item.LootType |= lt;
        }

        public static void ClearLootType(Item item, LootType lt)
        {
            item.LootType &= ~lt;
        }
        #endregion LootType
        private int CalcLastMovedTime()
        {
            /* begin last moved time optimization */
            long ticks = m_LastMovedTime.Ticks;
            long now = DateTime.UtcNow.Ticks;

            TimeSpan d;

            try { d = new TimeSpan(ticks - now); }
            catch { if (ticks < now) d = TimeSpan.MaxValue; else d = TimeSpan.MaxValue; }

            double minutes = -d.TotalMinutes;

            if (minutes < int.MinValue)
                minutes = int.MinValue;
            else if (minutes > int.MaxValue)
                minutes = int.MaxValue;

            return (int)minutes;
        }
        public virtual void Serialize(GenericWriter writer)
        {
            #region DO FIRST
            int version = 23;
            writer.Write(version); // version

            // determine what data we are to save
            SaveFlag flags = CalcSaveFlags();

            // Write the SaveFlags. Will tell us what we need to DeSerialize
            writer.Write((UInt64)flags);

            // version 21 - m_BoolTable is now a UInt64
            // version 16 - Item data that can be stored in a bool, like 'Movable'
            if (GetSaveFlag(flags, SaveFlag.BoolTable))
                // version 16
                //writer.WriteEncodedInt((int)m_BoolTable);
                // version 21
                writer.Write((UInt64)m_BoolTable);
            #endregion DO FIRST

            // version 23
            if (GetSaveFlag(flags, SaveFlag.LastTouched))
            {
                writer.Write(m_LastChange);
                writer.Write(m_LastTouched);
            }

            // version 22
            if (GetSaveFlag(flags, SaveFlag.HasLabel))
                writer.Write(m_Label);

            // version 21

            // version 20
            if (GetSaveFlag(flags, SaveFlag.TempRefCount))
            {
                writer.Write((byte)m_TempRefCount);
            }

            // version 19
            if (GetSaveFlag(flags, SaveFlag.Origin))
            {
                writer.Write((byte)m_Genesis);
            }

            // version 18
            if (GetSaveFlag(flags, SaveFlag.Spawner))
            {
                writer.Write(m_Spawner);
            }

            // version 17
            if (GetSaveFlag(flags, SaveFlag.BaseMulti))
            {
                writer.Write(m_BaseMulti);
            }

            // version 16 (replaced in version 18)
            /*if (GetSaveFlag(flags, SaveFlag.Spawner))
            {
                writer.Write(m_Spawner);
                writer.Write(m_SpawnerMap);
            }*/

            // version 15
            if (GetSaveFlag(flags, SaveFlag.QuestCode))
                writer.Write((UInt32)m_QuestCode);

            // version 14 - new LootType [Flags]
            if (GetSaveFlag(flags, SaveFlag.LootType))
                writer.Write((UInt32)m_LootType);

            // version 11
            if (GetSaveFlag(flags, SaveFlag.Created))
                writer.Write(m_Created);

            // version 10: Adam
            // Spawner Loot Packs
            if (GetSaveFlag(flags, SaveFlag.DropRate))
                writer.Write(m_DropRate);

            writer.WriteEncodedInt(CalcLastMovedTime());

            if (GetSaveFlag(flags, SaveFlag.Direction))
                writer.Write((byte)m_Direction);

            if (GetSaveFlag(flags, SaveFlag.Bounce))
                BounceInfo.Serialize(m_Bounce, writer);

            #region Location
            // here we write an optimized location
            // from a max of 3 ints, to two shorts, to two bytes, to a single byte
            if (GetSaveFlag(flags, SaveFlag.LocationFull))
            {
                writer.WriteEncodedInt(m_Location.m_X);
                writer.WriteEncodedInt(m_Location.m_Y);
                writer.WriteEncodedInt(m_Location.m_Z);
            }
            else
            {
                if (GetSaveFlag(flags, SaveFlag.LocationByteXY))
                {
                    writer.Write((byte)m_Location.m_X);
                    writer.Write((byte)m_Location.m_Y);
                }
                else if (GetSaveFlag(flags, SaveFlag.LocationShortXY))
                {
                    writer.Write((short)m_Location.m_X);
                    writer.Write((short)m_Location.m_Y);
                }

                if (GetSaveFlag(flags, SaveFlag.LocationSByteZ))
                    writer.Write((sbyte)m_Location.m_Z);
            }
            #endregion  Location

            if (GetSaveFlag(flags, SaveFlag.ItemID))
                writer.WriteEncodedInt((int)m_ItemID);

            if (GetSaveFlag(flags, SaveFlag.Hue))
                writer.WriteEncodedInt((int)m_Hue);

            if (GetSaveFlag(flags, SaveFlag.Amount))
                writer.WriteEncodedInt((int)m_Amount);

            if (GetSaveFlag(flags, SaveFlag.Layer))
                writer.Write((byte)m_Layer);

            if (GetSaveFlag(flags, SaveFlag.Name))
                writer.Write((string)m_Name);

            if (GetSaveFlag(flags, SaveFlag.Parent))
            {
                if (m_Parent is Mobile && !((Mobile)m_Parent).Deleted)
                    writer.Write(((Mobile)m_Parent).Serial);
                else if (m_Parent is Item && !((Item)m_Parent).Deleted)
                    writer.Write(((Item)m_Parent).Serial);
                else
                    writer.Write((int)Serial.MinusOne);
            }

            if (GetSaveFlag(flags, SaveFlag.Items))
                writer.WriteItemList(m_Items, false);

            if (GetSaveFlag(flags, SaveFlag.IntWeight))
                writer.WriteEncodedInt((int)m_Weight);

            else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                writer.Write((double)m_Weight);

            if (GetSaveFlag(flags, SaveFlag.Map))
                writer.Write((Map)m_Map);

            if (GetSaveFlag(flags, SaveFlag.BlessedFor))
                writer.Write(m_BlessedFor);

            if (GetSaveFlag(flags, SaveFlag.HeldBy))
                writer.Write(m_HeldBy);

            if (GetSaveFlag(flags, SaveFlag.SavedFlags))
                writer.WriteEncodedInt(m_SavedFlags);
        }
        public virtual void Deserialize(GenericReader reader)
        {
            #region DO FIRST
            int version = reader.ReadInt();

            SetLastMoved();

            // These flags tell us what to load
            SaveFlag flags = SaveFlag.None;
            if (version <= 19)
                flags = (SaveFlag)reader.ReadUInt();
            else
                flags = (SaveFlag)reader.ReadULong();

            if (version > 15)
            {   // > 15 we read the bool table
                if (GetSaveFlag(flags, SaveFlag.BoolTable))
                    if (version < 21)
                        // < 21, read the old style (small) bool table
                        m_BoolTable = (ItemBoolTable)reader.ReadEncodedInt();
                    else
                        // >= 21, read the new (large) bool table
                        m_BoolTable = (ItemBoolTable)reader.ReadULong();
            }
            #endregion DO FIRST

            switch (version)
            {
                case 23:
                    {
                        if (GetSaveFlag(flags, SaveFlag.LastTouched))
                        {
                            m_LastChange = reader.ReadString();
                            m_LastTouched = reader.ReadDateTime();
                        }
                        goto case 22;
                    }
                case 22:
                    {
                        if (GetSaveFlag(flags, SaveFlag.HasLabel))
                        {
                            m_Label = reader.ReadString();
                        }
                        goto case 21;
                    }
                case 21:    // m_BoolTable is now read as a UInt64
                case 20:
                    {
                        if (GetSaveFlag(flags, SaveFlag.TempRefCount))
                        {
                            m_TempRefCount = reader.ReadByte();
                        }
                        goto case 19;
                    }
                case 19:
                    {
                        if (GetSaveFlag(flags, SaveFlag.Origin))
                        {
                            m_Genesis = (Genesis)reader.ReadByte();
                        }
                        goto case 18;
                    }
                case 18:
                    {
                        if (GetSaveFlag(flags, SaveFlag.Spawner))
                        {
                            m_Spawner = (Spawner)reader.ReadItem();
                        }
                        goto case 17;
                    }
                case 17:
                    {
                        if (GetSaveFlag(flags, SaveFlag.BaseMulti))
                        {
                            m_BaseMulti = (BaseMulti)reader.ReadItem();
                        }
                        goto case 16;
                    }
                case 16:
                    {   // Complete Version Normalization
                        if (version < 18)
                            if (GetSaveFlag(flags, SaveFlag.Spawner))
                            {
                                reader.ReadPoint3D();
                                reader.ReadMap();
                            }

                        if (GetSaveFlag(flags, SaveFlag.QuestCode))
                            m_QuestCode = reader.ReadUInt();

                        if (GetSaveFlag(flags, SaveFlag.LootType))
                            m_LootType = (LootType)reader.ReadUInt();

                        if (GetSaveFlag(flags, SaveFlag.Created))
                            m_Created = reader.ReadDateTime();

                        // get the per item custom drop rate
                        if (GetSaveFlag(flags, SaveFlag.DropRate))
                            m_DropRate = reader.ReadDouble();

                        // last moved time
                        int minutes = reader.ReadEncodedInt();
                        try { LastMoved = DateTime.UtcNow - TimeSpan.FromMinutes(minutes); }
                        catch { LastMoved = DateTime.UtcNow; }

                        if (GetSaveFlag(flags, SaveFlag.Direction))
                            m_Direction = (Direction)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Bounce))
                            m_Bounce = BounceInfo.Deserialize(reader);

                        #region Location
                        int x = 0, y = 0, z = 0;

                        if (GetSaveFlag(flags, SaveFlag.LocationFull))
                        {
                            x = reader.ReadEncodedInt();
                            y = reader.ReadEncodedInt();
                            z = reader.ReadEncodedInt();
                        }
                        else
                        {
                            if (GetSaveFlag(flags, SaveFlag.LocationByteXY))
                            {
                                x = reader.ReadByte();
                                y = reader.ReadByte();
                            }
                            else if (GetSaveFlag(flags, SaveFlag.LocationShortXY))
                            {
                                x = reader.ReadShort();
                                y = reader.ReadShort();
                            }

                            if (GetSaveFlag(flags, SaveFlag.LocationSByteZ))
                                z = reader.ReadSByte();
                        }

                        m_Location = new Point3D(x, y, z);
                        #endregion  Location

                        if (GetSaveFlag(flags, SaveFlag.ItemID))
                            m_ItemID = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Hue))
                            m_Hue = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Amount))
                            m_Amount = reader.ReadEncodedInt();
                        else
                            m_Amount = 1;

                        if (GetSaveFlag(flags, SaveFlag.Layer))
                            m_Layer = (Layer)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Name))
                            m_Name = reader.ReadString();

                        if (GetSaveFlag(flags, SaveFlag.Parent))
                        {
                            Serial parent = reader.ReadInt();

                            if (parent.IsMobile)
                                m_Parent = World.FindMobile(parent);
                            else if (parent.IsItem)
                                m_Parent = World.FindItem(parent);
                            else
                                m_Parent = null;

                            if (m_Parent == null && (parent.IsMobile || parent.IsItem))
                                Delete();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Items))
                            m_Items = reader.ReadItemList<Item>();

                        #region Weight
                        if (GetSaveFlag(flags, SaveFlag.IntWeight))
                            m_Weight = reader.ReadEncodedInt();
                        else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                            m_Weight = reader.ReadDouble();
                        else if (GetSaveFlag(flags, SaveFlag.WeightIs0))
                            m_Weight = 0.0;
                        else
                            m_Weight = 1.0;
                        #endregion Weight

                        if (GetSaveFlag(flags, SaveFlag.Map))
                            m_Map = reader.ReadMap();
                        else
                            m_Map = Map.Internal;

                        if (GetSaveFlag(flags, SaveFlag.BlessedFor))
                            m_BlessedFor = reader.ReadMobile();

                        if (GetSaveFlag(flags, SaveFlag.HeldBy))
                            m_HeldBy = reader.ReadMobile();

                        if (GetSaveFlag(flags, SaveFlag.SavedFlags))
                            m_SavedFlags = reader.ReadEncodedInt();

                        if (m_Map != null && m_Parent == null)
                            m_Map.OnEnter(this);

                        // we're done
                        break;
                    }
                #region Old Serialization
                case 15:
                    {
                        if (GetSaveFlag(flags, SaveFlag.QuestCode))
                            m_QuestCode = reader.ReadUInt();

                        goto case 14;
                    }
                case 14:
                    {
                        if (GetSaveFlag(flags, SaveFlag.LootType))
                        {
                            m_LootType = (LootType)reader.ReadUInt();
                            flags &= ~SaveFlag.LootType;    // clear the save flag; prevents earlier versions from trying to read this
                        }
                        goto case 13;
                    }
                case 13:
                    {
                        /*if (version < 15)
                        {
                            if (GetSaveFlag(flags, SaveFlag.DynamicFeatures))
                                m_dynamicFeatures = (FeatureBits_obsolete)reader.ReadUInt();
                        }*/
                        goto case 12;
                    }
                case 12:
                    {
                        if (version < 16)
                        {
                            if (GetSaveFlag(flags, SaveFlag.Spawner))
                            {
                                reader.ReadPoint3D();
                                reader.ReadMap();
                            }
                        }

                        goto case 11;
                    }
                case 11:
                    {
                        #region Version 12 HACK
                        if (version < 16)
                        {
                            // We fucked up by writing this data in Serialize but forgot to bump the version number.
                            //	this is bad since we write the data SOMETIMES based upon whether we were created by a spawner.
                            //	the saving grace here is that version 11 + SaveFlag.SpawnerLocation should be sufficient to save the day
                            //		assuming unused bits in a variable are guarnteed to be 0 *crosses fingers*
                            if (version == 11 && GetSaveFlag(flags, SaveFlag.Spawner))
                            {
                                reader.ReadPoint3D();
                                reader.ReadMap();
                            }
                        }
                        #endregion Version 12 HACK

                        if (GetSaveFlag(flags, SaveFlag.Created))
                            m_Created = reader.ReadDateTime();
                        goto case 10;
                    }
                case 10:
                    {   // get the per item custom drop rate
                        if (GetSaveFlag(flags, SaveFlag.DropRate))
                            m_DropRate = reader.ReadDouble();
                    }
                    goto case 9;
                case 9:
                    goto case 8;
                case 8:
                    goto case 7;// change is at bottom of file after ImplFlags are read
                case 7:
                    goto case 6;
                case 6:
                    {
                        if (version < 7)
                        {
                            LastMoved = reader.ReadDeltaTime();
                        }
                        else
                        {
                            int minutes = reader.ReadEncodedInt();

                            try { LastMoved = DateTime.UtcNow - TimeSpan.FromMinutes(minutes); }
                            catch { LastMoved = DateTime.UtcNow; }
                        }

                        if (GetSaveFlag(flags, SaveFlag.Direction))
                            m_Direction = (Direction)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Bounce))
                            m_Bounce = BounceInfo.Deserialize(reader);

                        if (GetSaveFlag(flags, SaveFlag.LootType))
                        {
                            #region Version 14 LootType Patch
                            byte oldLootType = reader.ReadByte();
                            switch (oldLootType)
                            {
                                case 0: // Regular = 0,
                                    {
                                        m_LootType = LootType.Regular;
                                        break;
                                    }
                                case 1: // Newbied = 1,
                                    {
                                        m_LootType = LootType.Newbied;
                                        break;
                                    }
                                case 2: // Blessed = 2,
                                    {
                                        m_LootType = LootType.Blessed;
                                        break;
                                    }
                                case 3: // Cursed = 3,
                                    {
                                        m_LootType = LootType.Cursed;
                                        break;
                                    }
                                case 4: // Stealable = 4,
                                    {
                                        // swap the sense: Stealable ==> UnLootable
                                        m_LootType = LootType.UnLootable;
                                        break;
                                    }
                                case 5: // Rare = 5,
                                    {
                                        m_LootType = LootType.Rare;
                                        break;
                                    }
                                case 6: // Lootable = 6,
                                    {
                                        // swap the sense: Lootable ==> UnStealable
                                        m_LootType = LootType.UnStealable;
                                        break;
                                    }
                                case 100: // Unspecified = 0,
                                    {
                                        m_LootType = LootType.Unspecified;
                                        break;
                                    }
                            }
                            #endregion Version 14 LootType Patch
                        }

                        int x = 0, y = 0, z = 0;

                        if (GetSaveFlag(flags, SaveFlag.LocationFull))
                        {
                            x = reader.ReadEncodedInt();
                            y = reader.ReadEncodedInt();
                            z = reader.ReadEncodedInt();
                        }
                        else
                        {
                            if (GetSaveFlag(flags, SaveFlag.LocationByteXY))
                            {
                                x = reader.ReadByte();
                                y = reader.ReadByte();
                            }
                            else if (GetSaveFlag(flags, SaveFlag.LocationShortXY))
                            {
                                x = reader.ReadShort();
                                y = reader.ReadShort();
                            }

                            if (GetSaveFlag(flags, SaveFlag.LocationSByteZ))
                                z = reader.ReadSByte();
                        }

                        m_Location = new Point3D(x, y, z);

                        if (GetSaveFlag(flags, SaveFlag.ItemID))
                            m_ItemID = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Hue))
                            m_Hue = reader.ReadEncodedInt();

                        if (GetSaveFlag(flags, SaveFlag.Amount))
                            m_Amount = reader.ReadEncodedInt();
                        else
                            m_Amount = 1;

                        if (GetSaveFlag(flags, SaveFlag.Layer))
                            m_Layer = (Layer)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Name))
                            m_Name = reader.ReadString();

                        if (GetSaveFlag(flags, SaveFlag.Parent))
                        {
                            Serial parent = reader.ReadInt();

                            if (parent.IsMobile)
                                m_Parent = World.FindMobile(parent);
                            else if (parent.IsItem)
                                m_Parent = World.FindItem(parent);
                            else
                                m_Parent = null;

                            if (m_Parent == null && (parent.IsMobile || parent.IsItem))
                                Delete();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Items))
                            m_Items = reader.ReadItemList<Item>();
                        //else
                        //	m_Items = new ArrayList( 1 );

                        if (GetSaveFlag(flags, SaveFlag.IntWeight))
                            m_Weight = reader.ReadEncodedInt();
                        else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                            m_Weight = reader.ReadDouble();
                        else if (GetSaveFlag(flags, SaveFlag.WeightIs0))
                            m_Weight = 0.0;
                        else
                            m_Weight = 1.0;

                        if (GetSaveFlag(flags, SaveFlag.Map))
                            m_Map = reader.ReadMap();
                        else
                            m_Map = Map.Internal;

                        if (version < 16)
                        {
                            if (GetSaveFlag(flags, SaveFlag.Visible))
                                SetItemBool(ItemBoolTable.Visible, reader.ReadBool());
                            else
                                SetItemBool(ItemBoolTable.Visible, true);

                            if (GetSaveFlag(flags, SaveFlag.Movable))
                                SetItemBool(ItemBoolTable.Movable, reader.ReadBool());
                            else
                                SetItemBool(ItemBoolTable.Movable, true);

                            if (GetSaveFlag(flags, SaveFlag.Stackable))
                                SetItemBool(ItemBoolTable.Stackable, reader.ReadBool());

                            if (GetSaveFlag(flags, SaveFlag.ImplFlags))
                            {
                                m_BoolTable = (ItemBoolTable)reader.ReadEncodedInt();
                            }
                        }

                        // don't confuse ImplFlag.FreezeDried with SaveFlag.FreezeDried
                        // we check different flags because of a version quirk - ask Taran
                        /*if (GetBool(BoolTable.UNUSED2))
                        {
                            TotalWeight = reader.ReadInt();
                            TotalItems = reader.ReadInt();
                            TotalGold = reader.ReadInt();
                        }

                        if (GetSaveFlag(flags, SaveFlag.UNUSED2))
                        {
                            int count = reader.ReadInt();
                            m_SerializedContentsIdx = new byte[count];
                            for (int i = 0; i < count; i++)
                                m_SerializedContentsIdx[i] = reader.ReadByte();
                            count = reader.ReadInt();
                            m_SerializedContentsBin = new byte[count];
                            for (int i = 0; i < count; i++)
                                m_SerializedContentsBin[i] = reader.ReadByte();
                        }*/

                        if (GetSaveFlag(flags, SaveFlag.BlessedFor))
                            m_BlessedFor = reader.ReadMobile();

                        if (GetSaveFlag(flags, SaveFlag.HeldBy))
                            m_HeldBy = reader.ReadMobile();

                        if (GetSaveFlag(flags, SaveFlag.SavedFlags))
                            m_SavedFlags = reader.ReadEncodedInt();

                        //wea: 13/Mar/2007 Rare Factory
                        //if (GetSaveFlag(flags, SaveFlag.UNUSED3))
                        //m_RareData = (UInt32)reader.ReadInt();

                        if (m_Map != null && m_Parent == null)
                            m_Map.OnEnter(this);

                        break;
                    }
                case 5:
                    {
                        //SaveFlag flags = (SaveFlag)reader.ReadInt();

                        LastMoved = reader.ReadDeltaTime();

                        if (GetSaveFlag(flags, SaveFlag.Direction))
                            m_Direction = (Direction)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Bounce))
                            m_Bounce = BounceInfo.Deserialize(reader);

                        if (GetSaveFlag(flags, SaveFlag.LootType))
                            m_LootType = (LootType)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.LocationFull))
                            m_Location = reader.ReadPoint3D();

                        if (GetSaveFlag(flags, SaveFlag.ItemID))
                            m_ItemID = reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.Hue))
                            m_Hue = reader.ReadInt();

                        if (GetSaveFlag(flags, SaveFlag.Amount))
                            m_Amount = reader.ReadInt();
                        else
                            m_Amount = 1;

                        if (GetSaveFlag(flags, SaveFlag.Layer))
                            m_Layer = (Layer)reader.ReadByte();

                        if (GetSaveFlag(flags, SaveFlag.Name))
                            m_Name = reader.ReadString();

                        if (GetSaveFlag(flags, SaveFlag.Parent))
                        {
                            Serial parent = reader.ReadInt();

                            if (parent.IsMobile)
                                m_Parent = World.FindMobile(parent);
                            else if (parent.IsItem)
                                m_Parent = World.FindItem(parent);
                            else
                                m_Parent = null;

                            if (m_Parent == null && (parent.IsMobile || parent.IsItem))
                                Delete();
                        }

                        if (GetSaveFlag(flags, SaveFlag.Items))
                            m_Items = reader.ReadItemList<Item>();
                        //else
                        //	m_Items = new ArrayList( 1 );

                        if (GetSaveFlag(flags, SaveFlag.IntWeight))
                            m_Weight = reader.ReadEncodedInt();
                        else if (GetSaveFlag(flags, SaveFlag.WeightNot1or0))
                            m_Weight = reader.ReadDouble();
                        else if (GetSaveFlag(flags, SaveFlag.WeightIs0))
                            m_Weight = 0.0;
                        else
                            m_Weight = 1.0;

                        if (GetSaveFlag(flags, SaveFlag.Map))
                            m_Map = reader.ReadMap();
                        else
                            m_Map = Map.Internal;

                        if (version < 16)
                        {
                            if (GetSaveFlag(flags, SaveFlag.Visible))
                                SetItemBool(ItemBoolTable.Visible, reader.ReadBool());
                            else
                                SetItemBool(ItemBoolTable.Visible, true);

                            if (GetSaveFlag(flags, SaveFlag.Movable))
                                SetItemBool(ItemBoolTable.Movable, reader.ReadBool());
                            else
                                SetItemBool(ItemBoolTable.Movable, true);

                            if (GetSaveFlag(flags, SaveFlag.Stackable))
                                SetItemBool(ItemBoolTable.Stackable, reader.ReadBool());
                        }

                        if (m_Map != null && m_Parent == null)
                            m_Map.OnEnter(this);


                        break;
                    }
                case 4: // Just removed variables
                case 3:
                    {
                        m_Direction = (Direction)reader.ReadInt();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Bounce = BounceInfo.Deserialize(reader);
                        LastMoved = reader.ReadDeltaTime();

                        goto case 1;
                    }
                case 1:
                    {
                        m_LootType = (LootType)reader.ReadByte();//m_Newbied = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Location = reader.ReadPoint3D();
                        m_ItemID = reader.ReadInt();
                        m_Hue = reader.ReadInt();
                        m_Amount = reader.ReadInt();
                        m_Layer = (Layer)reader.ReadByte();
                        m_Name = reader.ReadString();

                        Serial parent = reader.ReadInt();

                        if (parent.IsMobile)
                            m_Parent = World.FindMobile(parent);
                        else if (parent.IsItem)
                            m_Parent = World.FindItem(parent);
                        else
                            m_Parent = null;

                        if (m_Parent == null && (parent.IsMobile || parent.IsItem))
                            Delete();

                        int count = reader.ReadInt();

                        if (count > 0)
                        {
                            m_Items = new List<Item>(count);

                            for (int i = 0; i < count; ++i)
                            {
                                Item item = reader.ReadItem();

                                if (item != null)
                                    m_Items.Add(item);
                            }
                        }

                        m_Weight = reader.ReadDouble();

                        if (version <= 3)
                        {
                            /*m_TotalItems =*/
                            reader.ReadInt();
                            /*m_TotalWeight =*/
                            reader.ReadInt();
                            /*m_TotalGold =*/
                            reader.ReadInt();
                        }

                        m_Map = reader.ReadMap();
                        SetItemBool(ItemBoolTable.Visible, reader.ReadBool());
                        SetItemBool(ItemBoolTable.Movable, reader.ReadBool());

                        if (version <= 3)
                            /*m_Deleted =*/
                            reader.ReadBool();

                        Stackable = reader.ReadBool();

                        if (m_Map != null && m_Parent == null)
                            m_Map.OnEnter(this);

                        break;
                    }
                    #endregion Old Serialization
            }

            if (m_HeldBy != null)
                Timer.DelayCall(TimeSpan.Zero, new TimerCallback(FixHolding_Sandbox));

            // fix name fields that end in " "
            if (Name != null && Name.EndsWith(" "))
                Name = Name.TrimEnd();
        }

        public IPooledEnumerable GetObjectsInRange(int range)
        {
            Map map = m_Map;

            if (map == null)
                return Map.NullEnumerable<IEntity>.Instance;

            if (m_Parent == null)
                return map.GetObjectsInRange(m_Location, range);

            return map.GetObjectsInRange(GetWorldLocation(), range);
        }

        public IPooledEnumerable GetItemsInRange(int range)
        {
            Map map = m_Map;

            if (map == null)
                return Map.NullEnumerable<Item>.Instance;

            if (m_Parent == null)
                return map.GetItemsInRange(m_Location, range);

            return map.GetItemsInRange(GetWorldLocation(), range);
        }

        public IPooledEnumerable GetMobilesInRange(int range)
        {
            Map map = m_Map;

            if (map == null)
                return Map.NullEnumerable<Mobile>.Instance;

            if (m_Parent == null)
                return map.GetMobilesInRange(m_Location, range);

            return map.GetMobilesInRange(GetWorldLocation(), range);
        }

        public IPooledEnumerable GetClientsInRange(int range)
        {
            Map map = m_Map;

            if (map == null)
                return Map.NullEnumerable<NetState>.Instance;

            if (m_Parent == null)
                return map.GetClientsInRange(m_Location, range);

            return map.GetClientsInRange(GetWorldLocation(), range);
        }

        private static int m_LockedDownFlag;
        private static int m_SecureFlag;

        public static int LockedDownFlag
        {
            get { return m_LockedDownFlag; }
            set { m_LockedDownFlag = value; }
        }

        public static int SecureFlag
        {
            get { return m_SecureFlag; }
            set { m_SecureFlag = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public virtual bool IsLockedDown
        {
            get { return GetTempFlag(m_LockedDownFlag); }
            set
            {
#if false
                if (!value)
                    CheckRehydrate();
#endif
                SetTempFlag(m_LockedDownFlag, value);
                InvalidateProperties();
            }
        }

        public virtual bool IsSecure
        {
            get { return GetTempFlag(m_SecureFlag); }
            set
            {
#if false
                if (!value)
                    CheckRehydrate();
#endif
                SetTempFlag(m_SecureFlag, value);
                InvalidateProperties();
            }
        }

        /*[CommandProperty(AccessLevel.Counselor)]
        public bool IsFreezeDried
        {
            get
            {
                return GetBool(BoolTable.FreezeDried);
            }
        }*/

        public bool GetTempFlag(int flag)
        {
            return ((m_TempFlags & flag) != 0);
        }

        public void SetTempFlag(int flag, bool value)
        {
            if (value)
                m_TempFlags |= flag;
            else
                m_TempFlags &= ~flag;
        }

        public bool GetSavedFlag(int flag)
        {
            return ((m_SavedFlags & flag) != 0);
        }

        public void SetSavedFlag(int flag, bool value)
        {
            if (value)
                m_SavedFlags |= flag;
            else
                m_SavedFlags &= ~flag;
        }

        private void FixHolding_Sandbox()
        {
            Mobile heldBy = m_HeldBy;

            if (heldBy != null)
            {
                if (m_Bounce != null)
                {
                    Bounce(heldBy);
                }
                else
                {
                    heldBy.Holding = null;
                    heldBy.AddToBackpack(this);
                    ClearBounce();
                }
            }
        }

        public virtual int GetMaxUpdateRange()
        {
            return 18;
        }

        public virtual int GetUpdateRange(Mobile m)
        {
            return 18;
        }

        public virtual void SendInfoTo(NetState state)
        {
            SendInfoTo(state, ObjectPropertyList.Enabled);
        }

        public virtual void SendInfoTo(NetState state, bool sendOplPacket)
        {
            state.Send(GetWorldPacketFor(state));

            if (sendOplPacket)
            {
                state.Send(OPLPacket);
            }
        }

        protected virtual Packet GetWorldPacketFor(NetState state)
        {
            return this.WorldPacket;
        }

        public void SetTotalGold(int value)
        {
            m_TotalGold = value;
        }

        public void SetTotalItems(int value)
        {
            m_TotalItems = value;
        }

        public void SetTotalWeight(int value)
        {
            m_TotalWeight = value;
        }

        public virtual bool IsVirtualItem { get { return false; } }

        public virtual void UpdateTotals()
        {
            //if (GetBool(BoolTable.UNUSED2))
            //return;

            m_LastAccessed = DateTime.UtcNow;

            m_TotalGold = 0;
            m_TotalItems = 0;
            m_TotalWeight = 0;

            if (m_Items == null)
                return;

            for (int i = 0; i < m_Items.Count; ++i)
            {
                Item item = (Item)m_Items[i];

                item.UpdateTotals();

                m_TotalGold += item.TotalGold;
                m_TotalItems += item.TotalItems;// + item.Items.Count;
                m_TotalWeight += item.TotalWeight + item.PileWeight;

                if (item.IsVirtualItem)
                    --m_TotalItems;
            }

            //if ( this is Gold )
            //	m_TotalGold += m_Amount;

            m_TotalItems += m_Items.Count;
        }

        public virtual int LabelNumber
        {
            get
            {
                return 1020000 + (m_ItemID & 0x3FFF);
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalGold
        {
            get
            {
                return m_TotalGold;
            }
            set
            {
                if (m_TotalGold != value)
                {
                    if (m_Parent is Item)
                    {
                        Item parent = (Item)m_Parent;

                        parent.TotalGold = (parent.TotalGold - m_TotalGold) + value;
                    }
                    else if (m_Parent is Mobile && !(this is BankBox))
                    {
                        Mobile parent = (Mobile)m_Parent;

                        parent.TotalGold = (parent.TotalGold - m_TotalGold) + value;
                    }

                    m_TotalGold = value;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalItems
        {
            get
            {
                return m_TotalItems;
            }
            set
            {
                if (m_TotalItems != value)
                {
                    if (m_Parent is Item)
                    {
                        Item parent = (Item)m_Parent;

                        parent.TotalItems = (parent.TotalItems - m_TotalItems) + value;
                        parent.InvalidateProperties();
                    }

                    m_TotalItems = value;
                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int TotalWeight
        {
            get
            {
                return m_TotalWeight;
            }
            set
            {
                if (m_TotalWeight != value)
                {
                    if (m_Parent is Item)
                    {
                        Item parent = (Item)m_Parent;

                        parent.TotalWeight = (parent.TotalWeight - m_TotalWeight) + value;
                        parent.InvalidateProperties();
                    }
                    else if (m_Parent is Mobile && !(this is BankBox))
                    {
                        Mobile parent = (Mobile)m_Parent;

                        parent.TotalWeight = (parent.TotalWeight - m_TotalWeight) + value;
                    }

                    m_TotalWeight = value;
                    InvalidateProperties();
                }
            }
        }

        public virtual double DefaultWeight
        {
            get
            {
                if (m_ItemID < 0 || m_ItemID >= 0x4000)
                    return 0;

                int weight = TileData.ItemTable[m_ItemID].Weight;

                if (weight == 255 || weight == 0)
                    weight = 1;

                return weight;
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public double Weight
        {
            get
            {
                return m_Weight;
            }
            set
            {
                if (m_Weight != value)
                {
                    int oldPileWeight = PileWeight;

                    m_Weight = value;

                    if (m_Parent is Item)
                    {
                        Item parent = (Item)m_Parent;

                        parent.TotalWeight = (parent.TotalWeight - oldPileWeight) + PileWeight;
                        parent.InvalidateProperties();
                    }
                    else if (m_Parent is Mobile && !(this is BankBox))
                    {
                        Mobile parent = (Mobile)m_Parent;

                        parent.TotalWeight = (parent.TotalWeight - oldPileWeight) + PileWeight;
                    }

                    InvalidateProperties();
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int PileWeight
        {
            get
            {
                return (int)Math.Ceiling(m_Weight * m_Amount);
            }
        }

        public virtual int HuedItemID
        {
            get
            {
                return (m_ItemID & 0x3FFF);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual int Hue
        {
            get
            {
                return m_Hue;
            }
            set
            {
                if (m_Hue != value)
                {
                    m_Hue = value;
                    Packet.Release(ref m_WorldPacket);

                    Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual Layer Layer
        {
            get
            {
                return m_Layer;
            }
            set
            {
                if (m_Layer != value)
                {
                    m_Layer = value;

                    Delta(ItemDelta.EquipOnly);
                }
            }
        }
        public List<Item> PeekItems
        {
            get { return m_Items != null ? new List<Item>(m_Items) : EmptyItems; }
        }
        public List<Item> Items
        {
            get
            {
#if false
                if (!World.Saving)
                    CheckRehydrate();
#endif
                List<Item> items = LookupItems();

                if (items == null)
                    items = EmptyItems;

                return items;
            }
        }

        public object RootParent
        {
            get
            {
                object p = m_Parent;

                while (p is Item)
                {
                    Item item = (Item)p;

                    if (item.m_Parent == null)
                    {
                        break;
                    }
                    else
                    {
                        p = item.m_Parent;
                    }
                }

                return p;
            }
        }
        public void AddItem(ref Item item)
        {
            if (item == null || item.Deleted || item.m_Parent == this)
            {
                return;
            }

            // allow any modifications to the item up to and including wholesale replacement
            item = OnBeforeItemAdded(item, this);

            AddItem(item);
        }
        public virtual void AddItem(Item item)
        {
            if (item == null || item.Deleted || item.m_Parent == this)
            {
                return;
            }

            if (Core.RuleSets.SiegeStyleRules())
            {   // currently only Mortalis and Siege have tracked items
                if (item.GetFlag(LootType.SiegeBlessed))
                {
                    if (item.BlessedFor != null && this.RootParent != item.BlessedFor)
                        item.BlessedFor.TrackedItemParentChanging(item);
                    // else they moved to/from backpack/bankbox
                }
            }
            if (item == this)
            {
                Console.WriteLine("Warning: Adding item to itself: [0x{0:X} {1}].AddItem( [0x{2:X} {3}] )", this.Serial.Value, this.GetType().Name, item.Serial.Value, item.GetType().Name);
                Console.WriteLine(new System.Diagnostics.StackTrace());
                return;
            }
            if (IsChildOf(item))
            {
                Console.WriteLine("Warning: Adding parent item to child: [0x{0:X} {1}].AddItem( [0x{2:X} {3}] )", this.Serial.Value, this.GetType().Name, item.Serial.Value, item.GetType().Name);
                Console.WriteLine(new System.Diagnostics.StackTrace());
                return;
            }
#if false
            CheckRehydrate();
#endif
            if (item.m_Parent is Mobile)
            {
                ((Mobile)item.m_Parent).RemoveItem(item);
            }
            else if (item.m_Parent is Item)
            {
                ((Item)item.m_Parent).RemoveItem(item);
            }
            else
            {
                item.SendRemovePacket();
            }

            item.Parent = this;
            item.Map = m_Map;

            if (m_Items == null)
                m_Items = new List<Item>(0);

            int oldCount = m_Items.Count;
            m_Items.Add(item);

            TotalItems = (TotalItems - oldCount) + m_Items.Count + item.TotalItems - (item.IsVirtualItem ? 1 : 0);
            TotalWeight += item.TotalWeight + item.PileWeight;
            TotalGold += item.TotalGold;

            if (item.m_Parent is Mobile m)
            {
                //item.IsIntMapStorage = m.IsIntMapStorage;
            }
            else if (item.m_Parent is Item i)
            {
                //item.IsIntMapStorage = i.IsIntMapStorage;
            }

            item.Delta(ItemDelta.Update);

            item.OnAdded(this);
            OnItemAdded(item);

            if (this is Container)
                EventSink.InvokeContainerAddItem(new ContainerAddItemEventArgs(this as Container, item));
        }

        private static List<Item> m_DeltaQueue = new List<Item>();

        public void Delta(ItemDelta flags)
        {
            if (m_Map == null || m_Map == Map.Internal)
                return;

            m_DeltaFlags |= flags;

            if (!GetItemBool(ItemBoolTable.InQueue))
            {
                SetItemBool(ItemBoolTable.InQueue, true);

                m_DeltaQueue.Add(this);
            }

            Core.Set();
        }

        public void RemDelta(ItemDelta flags)
        {
            m_DeltaFlags &= ~flags;

            if (GetItemBool(ItemBoolTable.InQueue) && m_DeltaFlags == ItemDelta.None)
            {
                SetItemBool(ItemBoolTable.InQueue, false);

                m_DeltaQueue.Remove(this);
            }
        }

        private bool m_NoMoveHS;

        public bool NoMoveHS
        {
            get { return m_NoMoveHS; }
            set { m_NoMoveHS = value; }
        }

#if true
        public void ProcessDelta()
        {
            ItemDelta flags = m_DeltaFlags;

            SetItemBool(ItemBoolTable.InQueue, false);
            m_DeltaFlags = ItemDelta.None;

            Map map = m_Map;

            if (map != null && !Deleted)
            {
                bool sendOPLUpdate = ObjectPropertyList.Enabled && (flags & ItemDelta.Properties) != 0;

                Container contParent = m_Parent as Container;

                if (contParent != null && !contParent.IsPublicContainer)
                {
                    if ((flags & ItemDelta.Update) != 0)
                    {
                        Point3D worldLoc = GetWorldLocation();

                        Mobile rootParent = contParent.RootParent as Mobile;
                        Mobile tradeRecip = null;

                        if (rootParent != null)
                        {
                            NetState ns = rootParent.NetState;

                            if (ns != null)
                            {
                                if (rootParent.CanSee(this) && rootParent.InRange(worldLoc, GetUpdateRange(rootParent)))
                                {
                                    if (ns.ContainerGridLines)
                                        ns.Send(new ContainerContentUpdate6017(this));
                                    else
                                        ns.Send(new ContainerContentUpdate(this));

                                    if (ObjectPropertyList.Enabled)
                                        ns.Send(OPLPacket);
                                }
                            }
                        }

                        SecureTradeContainer stc = this.GetSecureTradeCont();

                        if (stc != null)
                        {
                            SecureTrade st = stc.Trade;

                            if (st != null)
                            {
                                Mobile test = st.From.Mobile;

                                if (test != null && test != rootParent)
                                    tradeRecip = test;

                                test = st.To.Mobile;

                                if (test != null && test != rootParent)
                                    tradeRecip = test;

                                if (tradeRecip != null)
                                {
                                    NetState ns = tradeRecip.NetState;

                                    if (ns != null)
                                    {
                                        if (tradeRecip.CanSee(this) && tradeRecip.InRange(worldLoc, GetUpdateRange(tradeRecip)))
                                        {
                                            if (ns.ContainerGridLines)
                                                ns.Send(new ContainerContentUpdate6017(this));
                                            else
                                                ns.Send(new ContainerContentUpdate(this));

                                            if (ObjectPropertyList.Enabled)
                                                ns.Send(OPLPacket);
                                        }
                                    }
                                }
                            }
                        }

                        List<Mobile> openers = contParent.Openers;

                        if (openers != null)
                        {
                            lock (openers)
                            {
                                for (int i = 0; i < openers.Count; ++i)
                                {
                                    Mobile mob = openers[i];

                                    int range = GetUpdateRange(mob);

                                    if (mob.Map != map || !mob.InRange(worldLoc, range))
                                    {
                                        openers.RemoveAt(i--);
                                    }
                                    else
                                    {
                                        if (mob == rootParent || mob == tradeRecip)
                                            continue;

                                        NetState ns = mob.NetState;

                                        if (ns != null)
                                        {
                                            if (mob.CanSee(this))
                                            {
                                                if (ns.ContainerGridLines)
                                                    ns.Send(new ContainerContentUpdate6017(this));
                                                else
                                                    ns.Send(new ContainerContentUpdate(this));

                                                if (ObjectPropertyList.Enabled)
                                                    ns.Send(OPLPacket);
                                            }
                                        }
                                    }
                                }

                                if (openers.Count == 0)
                                    contParent.Openers = null;
                            }
                        }
                        return;
                    }
                }

                if ((flags & ItemDelta.Update) != 0)
                {
                    Packet p = null;
                    Point3D worldLoc = GetWorldLocation();

                    IPooledEnumerable<NetState> eable = map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                        {
                            if (m_Parent == null)
                            {
                                SendInfoTo(state, ObjectPropertyList.Enabled);
                            }
                            else
                            {
                                if (p == null)
                                {
                                    if (m_Parent is Item)
                                    {
                                        if (state.ContainerGridLines)
                                            state.Send(new ContainerContentUpdate6017(this));
                                        else
                                            state.Send(new ContainerContentUpdate(this));
                                    }
                                    else if (m_Parent is Mobile)
                                    {
                                        p = new EquipUpdate(this);
                                        p.Acquire();

                                        state.Send(p);
                                    }
                                }
                                else
                                {
                                    state.Send(p);
                                }

                                if (ObjectPropertyList.Enabled)
                                {
                                    state.Send(OPLPacket);
                                }
                            }
                        }
                    }

                    if (p != null)
                        Packet.Release(p);

                    eable.Free();
                    sendOPLUpdate = false;
                }
                else if ((flags & ItemDelta.EquipOnly) != 0)
                {
                    if (m_Parent is Mobile)
                    {
                        Packet p = null;
                        Point3D worldLoc = GetWorldLocation();

                        IPooledEnumerable<NetState> eable = map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                        foreach (NetState state in eable)
                        {
                            Mobile m = state.Mobile;

                            if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                            {
                                //if ( sendOPLUpdate )
                                //	state.Send( RemovePacket );

                                if (p == null)
                                    p = Packet.Acquire(new EquipUpdate(this));

                                state.Send(p);

                                if (ObjectPropertyList.Enabled)
                                    state.Send(OPLPacket);
                            }
                        }

                        Packet.Release(p);

                        eable.Free();
                        sendOPLUpdate = false;
                    }
                }

                if (sendOPLUpdate)
                {
                    Point3D worldLoc = GetWorldLocation();
                    IPooledEnumerable<NetState> eable = map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                            state.Send(OPLPacket);
                    }

                    eable.Free();
                }
            }
        }

        private static bool _processing = false;

        public static void ProcessDeltaQueue()
        {
            _processing = true;

            if (m_DeltaQueue.Count >= 512)
            {
                Parallel.ForEach(m_DeltaQueue, i => i.ProcessDelta());
            }
            else
            {
                for (int i = 0; i < m_DeltaQueue.Count; i++) m_DeltaQueue[i].ProcessDelta();
            }

            m_DeltaQueue.Clear();

            _processing = false;
        }
#else
        public void ProcessDelta()
        {
            ItemDelta flags = m_DeltaFlags;

            SetFlag(ImplFlag.InQueue, false);
            m_DeltaFlags = ItemDelta.None;

            Map map = m_Map;

            if (map != null && !Deleted)
            {
                bool sendOPLUpdate = ObjectPropertyList.Enabled && (flags & ItemDelta.Properties) != 0;

                Container contParent = m_Parent as Container;

                if (contParent != null && !contParent.IsPublicContainer)
                {
                    if ((flags & ItemDelta.Update) != 0)
                    {
                        Point3D worldLoc = GetWorldLocation();

                        Mobile rootParent = contParent.RootParent as Mobile;
                        Mobile tradeRecip = null;

                        if (rootParent != null)
                        {
                            NetState ns = rootParent.NetState;

                            if (ns != null)
                            {
                                if (rootParent.CanSee(this) && rootParent.InRange(worldLoc, GetUpdateRange(rootParent)))
                                {
                                    if (ns.Version >= new ClientVersion("6.0.1.7") /*ns.IsPost6017*/) //TODO
                                        ns.Send(new ContainerContentUpdate6017(this));
                                    else
                                        ns.Send(new ContainerContentUpdate(this));

                                    if (ObjectPropertyList.Enabled)
                                        ns.Send(OPLPacket);
                                }
                            }
                        }

                        SecureTradeContainer stc = this.GetSecureTradeCont();

                        if (stc != null)
                        {
                            SecureTrade st = stc.Trade;

                            if (st != null)
                            {
                                Mobile test = st.From.Mobile;

                                if (test != null && test != rootParent)
                                    tradeRecip = test;

                                test = st.To.Mobile;

                                if (test != null && test != rootParent)
                                    tradeRecip = test;

                                if (tradeRecip != null)
                                {
                                    NetState ns = tradeRecip.NetState;

                                    if (ns != null)
                                    {
                                        if (tradeRecip.CanSee(this) && tradeRecip.InRange(worldLoc, GetUpdateRange(tradeRecip)))
                                        {
                                            if (ns.ContainerGridLines)
                                                ns.Send(new ContainerContentUpdate6017(this));
                                            else
                                                ns.Send(new ContainerContentUpdate(this));

                                            if (ObjectPropertyList.Enabled)
                                                ns.Send(OPLPacket);
                                        }
                                    }
                                }
                            }
                        }

                        ArrayList openers = contParent.Openers;

                        if (openers != null)
                        {
                            for (int i = 0; i < openers.Count; ++i)
                            {
                                Mobile mob = (Mobile)openers[i];

                                int range = GetUpdateRange(mob);

                                if (mob.Map != map || !mob.InRange(worldLoc, range))
                                {
                                    openers.RemoveAt(i--);
                                }
                                else
                                {
                                    if (mob == rootParent || mob == tradeRecip)
                                        continue;

                                    NetState ns = mob.NetState;

                                    if (ns != null)
                                    {
                                        if (mob.CanSee(this))
                                        {
                                            if (ns.Version >= new ClientVersion("6.0.1.7") /*ns.IsPost6017*/) // TODO
                                                ns.Send(new ContainerContentUpdate6017(this));
                                            else
                                                ns.Send(new ContainerContentUpdate(this));

                                            if (ObjectPropertyList.Enabled)
                                                ns.Send(OPLPacket);
                                        }
                                    }
                                }
                            }

                            if (openers.Count == 0)
                                contParent.Openers = null;
                        }
                        return;
                    }
                }

                if ((flags & ItemDelta.Update) != 0)
                {
                    Packet p = null;
                    Point3D worldLoc = GetWorldLocation();

                    IPooledEnumerable eable = map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                        {
                            if (m_Parent == null)
                            {
                                SendInfoTo(state, ObjectPropertyList.Enabled);
                            }
                            else
                            {
                                if (p == null)
                                {
                                    if (m_Parent is Item)
                                    {
                                        if (ns.Version >= new ClientVersion("6.0.1.7") /*state.IsPost6017*/) // TODO
                                            state.Send(new ContainerContentUpdate6017(this));
                                        else
                                            state.Send(new ContainerContentUpdate(this));
                                    }
                                    else if (m_Parent is Mobile)
                                    {
                                        p = new EquipUpdate(this);
                                        p.Acquire();
                                        state.Send(p);
                                    }
                                }
                                else
                                {
                                    state.Send(p);
                                }

                                if (ObjectPropertyList.Enabled)
                                {
                                    state.Send(OPLPacket);
                                }
                            }
                        }
                    }

                    if (p != null)
                        Packet.Release(p);

                    eable.Free();
                    sendOPLUpdate = false;
                }
                else if ((flags & ItemDelta.EquipOnly) != 0)
                {
                    if (m_Parent is Mobile)
                    {
                        Packet p = null;
                        Point3D worldLoc = GetWorldLocation();

                        IPooledEnumerable eable = map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                        foreach (NetState state in eable)
                        {
                            Mobile m = state.Mobile;

                            if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                            {
                                //if ( sendOPLUpdate )
                                //	state.Send( RemovePacket );

                                if (p == null)
                                    p = Packet.Acquire(new EquipUpdate(this));

                                state.Send(p);

                                if (ObjectPropertyList.Enabled)
                                    state.Send(OPLPacket);
                            }
                        }

                        Packet.Release(p);

                        eable.Free();
                        sendOPLUpdate = false;
                    }
                }

                if (sendOPLUpdate)
                {
                    Point3D worldLoc = GetWorldLocation();
                    IPooledEnumerable eable = map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                    foreach (NetState state in eable)
                    {
                        Mobile m = state.Mobile;

                        if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                            state.Send(OPLPacket);
                    }

                    eable.Free();
                }
            }
        }

        public static void ProcessDeltaQueue()
        {
            int count = m_DeltaQueue.Count;

            for (int i = 0; i < m_DeltaQueue.Count; ++i)
            {
                ((Item)m_DeltaQueue[i]).ProcessDelta();

                if (i >= count)
                    break;
            }

            if (m_DeltaQueue.Count > 0)
                m_DeltaQueue.Clear();
        }
#endif
#if DEBUG
        private static List<Item> m_deleteMon = new List<Item>();
        public static List<Item> DeleteMon { get { return m_deleteMon; } }
#endif
        public virtual void OnDelete()
        {
            #region OBSOLETE
            // no longer useful since any mobile on the internal map has its backpack moved to IntMapStorage
            //  If that mobile is subsequently deleted, this logging will kick in. If we want to monitor IntMapStorage
            //  deletions, we'll need a m_deleteMon (below) or a specific trap
#if false
            if (this.IsIntMapStorage == true)
            {   // record that we are deleting such a thing and record who/why
                string output = string.Format("Deleting IsIntMapStorage item: {0}" + "\n" + new System.Diagnostics.StackTrace(), this);
                //Console.WriteLine(output);
                LogHelper logger = new LogHelper("IsIntMapStorage.log", false);
                logger.Log(LogType.Text, output);
                logger.Log(LogType.Item, this);
                logger.Finish();
            }
#endif
            #endregion OBSOLETE
#if DEBUG
            if (m_deleteMon.Contains(this))
            {   // record that we are deleting such a thing and record who/why
                string output = string.Format("Deleting monitored item: {0}" + "\n" + new System.Diagnostics.StackTrace(), this);
                //Console.WriteLine(output);
                LogHelper logger = new LogHelper("deleteMon.log", false);
                logger.Log(LogType.Text, output);
                logger.Log(LogType.Item, this);
                logger.Finish();
            }
#endif
        }

        public virtual void OnParentDeleted(object parent)
        {
            this.Delete();
        }

        public virtual void FreeCache()
        {
            Packet.Release(ref m_RemovePacket);
            Packet.Release(ref m_WorldPacket);
            Packet.Release(ref m_OPLPacket);
            Packet.Release(ref m_PropertyList);
        }
        public virtual void OnActionComplete(Mobile from, Item tool)
        {   // See IHasUsesRemaining (vs IUsesRemaining) for example use.
            // the main difference between the two is that IHasUsesRemaining is dynamic and can be managed at runtime.
        }
        public virtual void Delete()
        {
            if (GetItemBool(ItemBoolTable.TrapOnDelete))
                ;   // set your breakpoint here

            if (Deleted)
                return;

            if (!World.OnDelete(this))
                return;

            OnDelete();

            if (m_Items != null)
            {
                for (int i = m_Items.Count - 1; i >= 0; --i)
                {
                    if (i < m_Items.Count)
                        ((Item)m_Items[i]).OnParentDeleted(this);
                }
            }

            SendRemovePacket();

            SetItemBool(ItemBoolTable.Deleted, true);

            if (Parent is Mobile)
                ((Mobile)Parent).RemoveItem(this);
            else if (Parent is Item)
                ((Item)Parent).RemoveItem(this);

            ClearBounce();

            Map wasMap = m_Map;
            object wasParent = Parent;
            Point3D wasLocation = Location;

            if (m_Map != null)
            {
                m_Map.OnLeave(this);
                m_Map = null;
            }

            World.RemoveItem(this);

            OnAfterDelete();

            if (wasMap != null && wasMap != Map.Internal && wasParent == null)
                wasMap.FixColumn(wasLocation.X, wasLocation.Y);

            m_RemovePacket = null;
            m_WorldPacket = null;
            m_OPLPacket = null;
            m_PropertyList = null;
        }

        public void PublicOverheadMessage(MessageType type, int hue, bool ascii, string text)
        {
            if (m_Map != null)
            {
                Packet p = null;
                Point3D worldLoc = GetWorldLocation();

                IPooledEnumerable eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                foreach (NetState state in eable)
                {
                    Mobile m = state.Mobile;

                    if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                    {
                        if (p == null)
                        {
                            if (ascii)
                                p = new AsciiMessage(m_Serial, m_ItemID, type, hue, 3, this.Name, text);
                            else
                                p = new UnicodeMessage(m_Serial, m_ItemID, type, hue, 3, "ENU", this.Name, text);

                            p.Acquire();
                        }

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number)
        {
            PublicOverheadMessage(type, hue, number, "");
        }

        public void PublicOverheadMessage(MessageType type, int hue, int number, string args)
        {
            if (m_Map != null)
            {
                Packet p = null;
                Point3D worldLoc = GetWorldLocation();

                IPooledEnumerable eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                foreach (NetState state in eable)
                {
                    Mobile m = state.Mobile;

                    if (m.CanSee(this) && m.InRange(worldLoc, GetUpdateRange(m)))
                    {
                        if (p == null)
                            p = Packet.Acquire(new MessageLocalized(m_Serial, m_ItemID, type, hue, 3, number, this.Name, args));

                        state.Send(p);
                    }
                }

                Packet.Release(p);

                eable.Free();
            }
        }

        public void PrivateOverheadMessage(MessageType type, int hue, bool ascii, string text, NetState state)
        {
            if (state == null)
                return;

            if (ascii)
                state.Send(new AsciiMessage(m_Serial, m_ItemID, type, hue, 3, Name, text));
            else
                state.Send(new UnicodeMessage(m_Serial, m_ItemID, type, hue, 3, "ENU", Name, text));
        }

        public void PrivateOverheadMessage(MessageType type, int hue, int number, NetState state)
        {
            PrivateOverheadMessage(type, hue, number, "", state);
        }

        public void PrivateOverheadMessage(MessageType type, int hue, int number, string args, NetState state)
        {
            if (state == null)
                return;

            state.Send(new MessageLocalized(m_Serial, m_ItemID, type, hue, 3, number, Name, args));
        }

        public virtual void OnAfterDelete()
        {
        }

        public virtual void RemoveItem(Item item) // needs FreezeDried modifications
        {
            if (m_Items != null && m_Items.Contains(item))
            {
                item.SendRemovePacket();

                int oldCount = m_Items.Count;

                m_Items.Remove(item);

                TotalItems = (TotalItems - oldCount) + m_Items.Count - item.TotalItems + (item.IsVirtualItem ? 1 : 0);
                TotalWeight -= item.TotalWeight + item.PileWeight;
                TotalGold -= item.TotalGold;

                //item.IsIntMapStorage = false;

                item.Parent = null;

                item.OnRemoved(this);
                OnItemRemoved(item);
            }
        }
        public virtual Item Dupe(Item item, int amount)
        {
            // copy the properties, THEN set amount
            CopyProperties(item);
            item.Amount = amount;

            if (Parent is Mobile)
            {
                ((Mobile)Parent).AddItem(item);
            }
            else if (Parent is Item)
            {
                ((Item)Parent).AddItem(item);
            }

            item.Delta(ItemDelta.Update);

            return item;
        }
        public virtual void CopyProperties(Item item)
        {
            item.Visible = Visible;
            item.Movable = Movable;
            item.LootType = LootType;
            item.Direction = Direction;
            item.Hue = Hue;
            item.ItemID = ItemID;
            item.Location = Location;
            item.Layer = Layer;
            item.Name = Name;
            item.Weight = Weight;
            item.Amount = Amount;
            item.Map = Map;

            item.BoolTable = m_BoolTable; // copy over all our boolean flags
        }
        public virtual bool OnDragLift(Mobile from)
        {
            return true;
        }

        public virtual bool OnEquip(Mobile from)
        {
            return true;
        }

        public virtual void OnBeforeSpawn(Point3D location, Map m)
        {
        }

        public virtual void OnAfterSpawn()
        {

        }

        public virtual Item Dupe(int amount)
        {
            return Dupe(new Item(), amount);
        }

        public virtual int PhysicalResistance { get { return 0; } }
        public virtual int FireResistance { get { return 0; } }
        public virtual int ColdResistance { get { return 0; } }
        public virtual int PoisonResistance { get { return 0; } }
        public virtual int EnergyResistance { get { return 0; } }

        [CommandProperty(AccessLevel.Counselor)]
        public Serial Serial
        {
            get
            {
                return m_Serial;
            }
        }

        public virtual void OnLocationChange(Point3D oldLocation)
        {
        }

        //PIX: This was put in so that the contents of a corpse (which is the
        // ONLY place this should be called) is refreshed for newer (> 6.0.something)
        // clients - if this isn't done, then people coming in range of a corpse (other
        // than when it's freshly created) won't see what's equipped on the corpse.
        public void DoSpecialContainerUpdateForEquippingCorpses()
        {
            if (m_Map != null)
            {
                if (m_Parent is Item)
                {
                    Packet.Release(ref m_WorldPacket);

                    Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public virtual Point3D Location
        {
            get
            {
                return m_Location;
            }
            set
            {
                Point3D oldLocation = m_Location;

                if (oldLocation != value)
                {
                    if (m_Map != null)
                    {
                        if (m_Parent == null)
                        {
                            IPooledEnumerable eable;

                            if (m_Location.m_X != 0)
                            {
                                Packet removeThis = null;

                                eable = m_Map.GetClientsInRange(oldLocation, GetMaxUpdateRange());

                                foreach (NetState state in eable)
                                {
                                    Mobile m = state.Mobile;

                                    if (!m.InRange(value, GetUpdateRange(m)))
                                    {
                                        if (removeThis == null)
                                            removeThis = this.RemovePacket;

                                        state.Send(removeThis);
                                    }
                                }

                                eable.Free();
                            }

                            m_Location = value;
                            Packet.Release(ref m_WorldPacket);

                            SetLastMoved();

                            eable = m_Map.GetClientsInRange(m_Location, GetMaxUpdateRange());

                            foreach (NetState state in eable)
                            {
                                Mobile m = state.Mobile;

                                if (m.CanSee(this) && m.InRange(m_Location, GetUpdateRange(m)))
                                    SendInfoTo(state);
                            }

                            eable.Free();

                            RemDelta(ItemDelta.Update);
                        }
                        else if (m_Parent is Item)
                        {
                            m_Location = value;
                            Packet.Release(ref m_WorldPacket);

                            Delta(ItemDelta.Update);
                        }
                        else
                        {
                            m_Location = value;
                            Packet.Release(ref m_WorldPacket);
                        }

                        if (m_Parent == null)
                            m_Map.OnMove(oldLocation, this);
                    }
                    else
                    {
                        m_Location = value;
                        Packet.Release(ref m_WorldPacket);
                    }

                    this.OnLocationChange(oldLocation);
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public virtual int X
        {
            get { return m_Location.m_X; }
            set { Location = new Point3D(value, m_Location.m_Y, m_Location.m_Z); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public virtual int Y
        {
            get { return m_Location.m_Y; }
            set { Location = new Point3D(m_Location.m_X, value, m_Location.m_Z); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public virtual int Z
        {
            get { return m_Location.m_Z; }
            set { Location = new Point3D(m_Location.m_X, m_Location.m_Y, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public virtual int ItemID
        {
            get
            {
                return m_ItemID;
            }
            set
            {
                if (m_ItemID != value)
                {
                    m_ItemID = value;
                    Packet.Release(ref m_WorldPacket);

                    InvalidateProperties();
                    Delta(ItemDelta.Update);
                }
            }
        }

        public virtual string DefaultName
        {
            get { return null; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Name
        {
            get
            {
                if (m_Name != null)
                    return m_Name;

                return this.DefaultName;
            }
            set
            {
                if (value == null || value != DefaultName)
                {
                    m_Name = value;

                    InvalidateProperties();
                }
            }
        }
        public string SafeName
        {
            get
            {
                if (m_Name != null)
                    return m_Name;

                return DefaultName != null ? DefaultName : this.GetType().Name;
            }
        }

        //[CopyableAttribute(CopyType.DoNotCopy)]
        //public object ParentRaw
        //{
        //    get
        //    { return m_Parent; }
        //    //set
        //    //{ m_Parent = value; }
        //}

        [CopyableAttribute(CopyType.DoNotCopy)]
        public object Parent
        {
            get
            {
                return m_Parent;
            }
            set
            {
                if (m_Parent == value)
                    return;

                object oldParent = m_Parent;

                m_Parent = value;

                if (m_Map != null)
                {
                    if (oldParent != null && m_Parent == null)
                        m_Map.OnEnter(this);
                    else if (m_Parent != null)
                        m_Map.OnLeave(this);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public LightType Light
        {
            get
            {
                return (LightType)m_Direction;
            }
            set
            {
                if ((LightType)m_Direction != value)
                {
                    m_Direction = (Direction)value;
                    Packet.Release(ref m_WorldPacket);

                    Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Direction
        {
            get
            {
                return m_Direction;
            }
            set
            {
                if (m_Direction != value)
                {
                    m_Direction = value;
                    Packet.Release(ref m_WorldPacket);

                    Delta(ItemDelta.Update);
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public UInt32 QuestCode
        {
            get
            {
                return m_QuestCode;
            }
            set
            {
                if (Engines.QuestCodes.QuestCodes.CheckQuestCode((ushort)value) || value == 0)
                    m_QuestCode = value;
                else
                {
                    this.SendSystemMessage("That quest code has not been allocated.");
                    this.SendSystemMessage("Use [QuestCodeAlloc to allocate a new quest code.");
                    throw new ApplicationException("That quest code has not been allocated.");
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Amount
        {
            get
            {
                return m_Amount;
            }
            set
            {
                int oldValue = m_Amount;

                if (oldValue != value)
                {
                    int oldPileWeight = PileWeight;

                    m_Amount = value;
                    Packet.Release(ref m_WorldPacket);

                    if (m_Parent is Item)
                    {
                        Item parent = (Item)m_Parent;

                        parent.TotalWeight = (parent.TotalWeight - oldPileWeight) + PileWeight;
                    }
                    else if (m_Parent is Mobile && !(this is BankBox))
                    {
                        Mobile parent = (Mobile)m_Parent;

                        parent.TotalWeight = (parent.TotalWeight - oldPileWeight) + PileWeight;
                    }

                    OnAmountChange(oldValue);

                    Delta(ItemDelta.Update);

                    if (oldValue > 1 || value > 1)
                        InvalidateProperties();

                    if (!Stackable && m_Amount > 1)
                        Console.WriteLine("Warning: 0x{0:X}: Amount changed for non-stackable item '{2}'. ({1})", m_Serial.Value, m_Amount, GetType().Name);
                }
            }
        }

        protected virtual void OnAmountChange(int oldValue)
        {
        }

        public virtual bool HandlesOnSpeech { get { return false; } }

        public virtual void OnSpeech(SpeechEventArgs e)
        {
        }

        public virtual bool OnDroppedToMobile(Mobile from, Mobile target)
        {
            return true;
        }

        public virtual bool DropToMobile(Mobile from, Mobile target, Point3D p)
        {
            if (Deleted || from.Deleted || target.Deleted || from.Map != target.Map || from.Map == null || target.Map == null)
                return false;
            else if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(target.Location, 2))
                return false;
            else if (!from.CanSee(target) || !from.InLOS(target))
                return false;
            else if (!from.OnDroppedItemToMobile(this, target))
                return false;
            else if (!OnDroppedToMobile(from, target))
                return false;
            else if (!target.OnDragDrop(from, this))
                return false;
            else
                return true;
        }

        public virtual bool OnDroppedInto(Mobile from, Container target, Point3D p)
        {
            if (!from.OnDroppedItemInto(this, target, p))
                return false;

            return target.OnDragDropInto(from, this, p);
        }
        public virtual bool OnDroppedOnto(Mobile from, Item target)
        {
            if (Deleted || from.Deleted || target.Deleted || from.Map != target.Map || from.Map == null || target.Map == null)
                return false;
            else if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(target.GetWorldLocation(), 2))
                return false;
            else if ((!from.CanSee(target) || !from.InLOS(target)) && !CheckNonLocalStorage(target))
                return false;
            else if (!target.IsAccessibleTo(from))
                return false;
            else if (!from.OnDroppedItemOnto(this, target))
                return false;
            else
                return target.OnDragDrop(from, this);
        }

        public virtual bool NonLocalStorage { get { return false; } }

        private bool CheckNonLocalStorage(Item item)
        {

            if (item.RootParent is Container contParent)
                return contParent.NonLocalStorage;
            else if (item is Container cont)
                return cont.NonLocalStorage;

            return false;
        }

        public virtual bool DropToItem(Mobile from, Item target, Point3D p)
        {
            if (Deleted || from.Deleted || target.Deleted || from.Map != target.Map || from.Map == null || target.Map == null)
                return false;

            object root = target.RootParent;

            if (from.AccessLevel < AccessLevel.GameMaster && !from.InRange(target.GetWorldLocation(), 2))
                return false;
            else if ((!from.CanSee(target) || !from.InLOS(target)) && !CheckNonLocalStorage(target))
                return false;
            else if (!target.IsAccessibleTo(from))
                return false;
            else if (root is Mobile && !((Mobile)root).CheckNonlocalDrop(from, this, target))
                return false;
            else if (!from.OnDroppedItemToItem(this, target, p))
                return false;
            else if (target is Container && p.m_X != -1 && p.m_Y != -1)
                return OnDroppedInto(from, (Container)target, p);
            else
                return OnDroppedOnto(from, target);
        }

        public virtual bool OnDroppedToWorld(Mobile from, Point3D p)
        {
            return true;
        }

        public virtual int GetLiftSound(Mobile from)
        {
            return 0x57;
        }

        private static int m_OpenSlots;
        public virtual bool OnDropToItem(Mobile from, Point3D p)
        {
            return true;
        }
        private static List<Point3D> DungeonTeleCache = new();
        private bool BlocksDungeonTele(Mobile from, Point3D p)
        {
            if (!this.ItemData.Impassable)
                return false;

            if (DungeonTeleCache.Contains(p))
                return true;

            Map map = from.Map;
            IPooledEnumerable eable = map.GetItemsInRange(p, 3);
            foreach (Item item in eable)
            {
                if (item is Teleporter tele && (Utility.IsDungeonTeleporter(tele) || Utility.IsDungeonEnterExitTeleporter(tele)))
                {
                    DungeonTeleCache.Add(p);
                    eable.Free();
                    return true;
                }
            }
            eable.Free();
            return false;
        }
        private bool TrapsGoodwill(Mobile from, Point3D p)
        {
            if (this is TrapableContainer tc && tc.TrapEnabled)
            {
                Map map = from.Map;
                GoodwillAsshat.GoodwillCrate nearest = null;
                IPooledEnumerable eable = map.GetItemsInRange(p, 3);
                foreach (Item item in eable)
                {
                    if (item is GoodwillAsshat.GoodwillCrate cont)
                    {
                        if (nearest == null)
                            nearest = cont;
                        else if (from.GetDistanceToSqrt(cont) < from.GetDistanceToSqrt(nearest))
                            nearest = cont;
                    }
                }
                eable.Free();

                if (nearest != null)
                {
                    GoodwillAsshat.ApplyPunishment(from, nearest, Poison.Greater);
                    return true;
                }
            }
            return false;
        }
        public virtual bool DropToWorld(Mobile from, Point3D p)
        {
            if (Deleted || from.Deleted || from.Map == null)
                return false;
            else if (!from.InRange(p, 2))
                return false;
            else if (BlocksDungeonTele(from, p))
                return false;
            else if (TrapsGoodwill(from, p))
                return false;

            Map map = from.Map;

            if (map == null)
                return false;

            int x = p.m_X, y = p.m_Y;
            int z = int.MinValue;

            int maxZ = from.Z + 16;

            LandTile landTile = map.Tiles.GetLandTile(x, y);
            TileFlag landFlags = TileData.LandTable[landTile.ID & 0x3FFF].Flags;

            int landZ = 0, landAvg = 0, landTop = 0;
            map.GetAverageZ(x, y, ref landZ, ref landAvg, ref landTop);

            if (!landTile.Ignored && (landFlags & TileFlag.Impassable) == 0)
            {
                if (landAvg <= maxZ)
                    z = landAvg;
            }

            StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile tile = tiles[i];
                ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                if (!id.Surface)
                    continue;

                int top = tile.Z + id.CalcHeight;

                if (top > maxZ || top < z)
                    continue;

                z = top;
            }

            ArrayList items = new ArrayList();

            IPooledEnumerable eable = map.GetItemsInRange(p, 0);

            foreach (Item item in eable)
            {
                if (item is NoDropTarget)
                {   // 5/19/2023, Adam: simply disallow items to be dropped here
                    eable.Free();
                    return false;
                }
                if (item.ItemID >= 0x4000)
                    continue;

                items.Add(item);

                ItemData id = item.ItemData;

                if (!id.Surface)
                    continue;

                int top = item.Z + id.CalcHeight;

                if (top > maxZ || top < z)
                    continue;

                z = top;
            }

            eable.Free();

            if (z == int.MinValue)
                return false;

            if (z > maxZ)
                return false;

            m_OpenSlots = (1 << 20) - 1;

            int surfaceZ = z;

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile tile = tiles[i];
                ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                int checkZ = tile.Z;
                int checkTop = checkZ + id.CalcHeight;

                if (checkTop == checkZ && !id.Surface)
                    ++checkTop;

                int zStart = checkZ - z;
                int zEnd = checkTop - z;

                if (zStart >= 20 || zEnd < 0)
                    continue;

                if (zStart < 0)
                    zStart = 0;

                if (zEnd > 19)
                    zEnd = 19;

                int bitCount = zEnd - zStart;

                m_OpenSlots &= ~(((1 << bitCount) - 1) << zStart);
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[i];
                ItemData id = item.ItemData;

                int checkZ = item.Z;
                int checkTop = checkZ + id.CalcHeight;

                if (checkTop == checkZ && !id.Surface)
                    ++checkTop;

                int zStart = checkZ - z;
                int zEnd = checkTop - z;

                if (zStart >= 20 || zEnd < 0)
                    continue;

                if (zStart < 0)
                    zStart = 0;

                if (zEnd > 19)
                    zEnd = 19;

                int bitCount = zEnd - zStart;

                m_OpenSlots &= ~(((1 << bitCount) - 1) << zStart);
            }

            int height = ItemData.Height;

            if (height == 0)
                ++height;

            if (height > 30)
                height = 30;

            int match = (1 << height) - 1;
            bool okay = false;

            for (int i = 0; i < 20; ++i)
            {
                if ((i + height) > 20)
                    match >>= 1;

                okay = ((m_OpenSlots >> i) & match) == match;

                if (okay)
                {
                    z += i;
                    break;
                }
            }

            if (!okay)
                return false;

            height = ItemData.Height;

            if (height == 0)
                ++height;

            if (landAvg > z && (z + height) > landZ)
                return false;
            else if ((landFlags & TileFlag.Impassable) != 0 && landAvg > surfaceZ && (z + height) > landZ)
                return false;

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile tile = tiles[i];
                ItemData id = TileData.ItemTable[tile.ID & 0x3FFF];

                int checkZ = tile.Z;
                int checkTop = checkZ + id.CalcHeight;

                if (checkTop > z && (z + height) > checkZ)
                    return false;
                else if ((id.Surface || id.Impassable) && checkTop > surfaceZ && (z + height) > checkZ)
                    return false;
            }

            for (int i = 0; i < items.Count; ++i)
            {
                Item item = (Item)items[i];
                ItemData id = item.ItemData;

                int checkZ = item.Z;
                int checkTop = checkZ + id.CalcHeight;

                if ((item.Z + id.CalcHeight) > z && (z + height) > item.Z)
                    return false;
            }

            p = new Point3D(x, y, z);

            if (!from.InLOS(new Point3D(x, y, z + 1)))
                return false;
            else if (!from.OnDroppedItemToWorld(this, p))
                return false;
            else if (!OnDroppedToWorld(from, p))
                return false;

            int soundID = GetDropSound();

            MoveToWorld(p, from.Map, responsible: from);

            from.SendSound(soundID == -1 ? 0x42 : soundID, GetWorldLocation());

            return true;
        }

        public void SendRemovePacket()
        {
            if (!Deleted && m_Map != null)
            {
                Packet p = null;
                Point3D worldLoc = GetWorldLocation();

                IPooledEnumerable eable = m_Map.GetClientsInRange(worldLoc, GetMaxUpdateRange());

                foreach (NetState state in eable)
                {
                    Mobile m = state.Mobile;

                    if (m.InRange(worldLoc, GetUpdateRange(m)))
                    {
                        if (p == null)
                            p = this.RemovePacket;

                        state.Send(p);
                    }
                }

                eable.Free();
            }
        }

        public virtual int GetDropSound()
        {
            return -1;
        }
        public struct WorldLocation : IPoint3D
        {

            internal int m_X;
            internal int m_Y;
            internal int m_Z;
            internal byte m_M;

            public WorldLocation(Point3D px, Map m)
                : this(px.X, px.Y, px.Z, m)
            {
            }
            public WorldLocation(int x, int y, int z, Map m)
            {
                m_X = x;
                m_Y = y;
                m_Z = z;
                if (m != null)
                    m_M = (byte)m.MapIndex;
                else
                    m_M = 0xFF;
            }
            public WorldLocation(Point3D px, byte m)
            {
                m_X = px.X;
                m_Y = px.Y;
                m_Z = px.Z;
                m_M = m;
            }
            public WorldLocation()
            {
                m_X = 0;
                m_Y = 0;
                m_Z = 0;
                m_M = 0xFF;
            }

            [CommandProperty(AccessLevel.Counselor)]
            public int X
            {
                get
                {
                    return m_X;
                }
                set
                {
                    m_X = value;
                }
            }

            [CommandProperty(AccessLevel.Counselor)]
            public int Y
            {
                get
                {
                    return m_Y;
                }
                set
                {
                    m_Y = value;
                }
            }

            [CommandProperty(AccessLevel.Counselor)]
            public int Z
            {
                get
                {
                    return m_Z;
                }
                set
                {
                    m_Z = value;
                }
            }
            [CommandProperty(AccessLevel.Counselor)]
            public byte M
            {
                get
                {
                    return m_M;
                }
                set
                {
                    m_M = value;
                }
            }
            public override string ToString()
            {
                return String.Format("({0}, {1}, {2} {3})", m_X, m_Y, m_Z, Map.Maps[m_M]);
            }
            public static implicit operator Point3D(WorldLocation d) => new Point3D(d.X, d.Y, d.Z);
            public static explicit operator WorldLocation(Point3D b) => new WorldLocation(b.X, b.Y, b.Z, Map.Felucca);   // default to felucca.
        }
        public WorldLocation GetWorldLocation()
        {
            object root = RootParent;
            WorldLocation wl = new();

            if (root == null)
            {
                wl.X = m_Location.X;
                wl.Y = m_Location.Y;
                wl.Z = m_Location.Z;
                wl.M = (Map != null) ? (byte)Map.MapIndex : (byte)0x7F; /*Internal*/
            }
            else
            {
                wl.X = ((IEntity)root).Location.X;
                wl.Y = ((IEntity)root).Location.Y;
                wl.Z = ((IEntity)root).Location.Z;
                wl.M = (((IEntity)root).Map != null) ? (byte)((IEntity)root).Map.MapIndex : (byte)0x7F; /*Internal*/
            }

            return wl;
        }

        public virtual bool BlocksFit { get { return false; } }

        public Point3D GetSurfaceTop()
        {
            object root = RootParent;

            if (root == null)
                return new Point3D(m_Location.m_X, m_Location.m_Y, m_Location.m_Z + (ItemData.Surface ? ItemData.CalcHeight : 0));
            else
                return ((IEntity)root).Location;
        }

        public Point3D GetWorldTop()
        {
            object root = RootParent;

            if (root == null)
                return new Point3D(m_Location.m_X, m_Location.m_Y, m_Location.m_Z + ItemData.CalcHeight);
            else
                return ((IEntity)root).Location;
        }

        public void SendMessageTo(Mobile to, bool ascii, string message)
        {
            if (Deleted || !to.CanSee(this))
                return;

            if (ascii)
                to.Send(new AsciiMessage(m_Serial, m_ItemID, MessageType.Regular, 0x3B2, 3, "", message));
            else
                to.Send(new UnicodeMessage(m_Serial, m_ItemID, MessageType.Regular, 0x3B2, 3, "ENU", "", message));
        }

        public void SendMessageTo(Mobile to, bool ascii, int hue, string message)
        {
            if (Deleted || !to.CanSee(this))
                return;

            if (ascii)
                to.Send(new AsciiMessage(m_Serial, m_ItemID, MessageType.Regular, hue, 3, "", message));
            else
                to.Send(new UnicodeMessage(m_Serial, m_ItemID, MessageType.Regular, hue, 3, "ENU", "", message));
        }

        public void SendLocalizedMessageTo(Mobile to, int number)
        {
            if (Deleted || !to.CanSee(this))
                return;

            to.Send(new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", ""));
        }

        public void SendLocalizedMessageTo(Mobile to, int number, string args)
        {
            if (Deleted || !to.CanSee(this))
                return;

            to.Send(new MessageLocalized(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", args));
        }

        public void SendLocalizedMessageTo(Mobile to, int number, AffixType affixType, string affix, string args)
        {
            if (Deleted || !to.CanSee(this))
                return;

            to.Send(new MessageLocalizedAffix(Serial, ItemID, MessageType.Regular, 0x3B2, 3, number, "", affixType, affix, args));
        }

        public virtual void OnDoubleClick(Mobile from)
        {
        }

        public virtual void OnDoubleClickOutOfRange(Mobile from)
        {
        }

        public virtual void OnDoubleClickCantSee(Mobile from)
        {
        }

        public virtual void OnDoubleClickDead(Mobile from)
        {
            from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019048); // I am dead and cannot do that.
        }

        public virtual void OnDoubleClickNotAccessible(Mobile from)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        public virtual void OnDoubleClickSecureTrade(Mobile from)
        {
            from.SendLocalizedMessage(500447); // That is not accessible.
        }

        public virtual void OnSnoop(Mobile from)
        {
        }

        public bool InSecureTrade
        {
            get
            {
                return (GetSecureTradeCont() != null);
            }
        }

        public SecureTradeContainer GetSecureTradeCont()
        {
            object p = this;

            while (p is Item)
            {
                if (p is SecureTradeContainer)
                    return (SecureTradeContainer)p;

                p = ((Item)p).m_Parent;
            }

            return null;
        }
        public virtual Item OnBeforeItemAdded(Item item, object parent)
        {   // allow any modifications to the item up to and including wholesale replacement
            return item;
        }
        public virtual void OnItemAdded(Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).OnSubItemAdded(item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).OnSubItemAdded(item);
        }

        public virtual void OnItemRemoved(Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).OnSubItemRemoved(item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).OnSubItemRemoved(item);
        }

        public virtual void OnSubItemAdded(Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).OnSubItemAdded(item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).OnSubItemAdded(item);
        }

        public virtual void OnSubItemRemoved(Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).OnSubItemRemoved(item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).OnSubItemRemoved(item);
        }

        public virtual void OnItemBounceCleared(Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).OnSubItemBounceCleared(item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).OnSubItemBounceCleared(item);
        }

        public virtual void OnSubItemBounceCleared(Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).OnSubItemBounceCleared(item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).OnSubItemBounceCleared(item);
        }

        public virtual bool CheckTarget(Mobile from, Server.Targeting.Target targ, object targeted)
        {
            if (m_Parent is Item)
                return ((Item)m_Parent).CheckTarget(from, targ, targeted);
            else if (m_Parent is Mobile)
                return ((Mobile)m_Parent).CheckTarget(from, targ, targeted);

            return true;
        }

        public virtual bool IsAccessibleTo(Mobile check)
        {
            if (m_Parent is Item)
                return ((Item)m_Parent).IsAccessibleTo(check);

            Region reg = Region.Find(GetWorldLocation(), m_Map);

            return reg.CheckAccessibility(this, check);

            /*SecureTradeContainer cont = GetSecureTradeCont();

			if ( cont != null && !cont.IsChildOf( check ) )
				return false;

			return true;*/
        }

        public bool IsChildOf(object o)
        {
            return IsChildOf(o, false);
        }

        public bool IsChildOf(object o, bool allowNull)
        {
            object p = m_Parent;

            if ((p == null || o == null) && !allowNull)
                return false;

            if (p == o)
                return true;

            while (p is Item)
            {
                Item item = (Item)p;

                if (item.m_Parent == null)
                {
                    break;
                }
                else
                {
                    p = item.m_Parent;

                    if (p == o)
                        return true;
                }
            }

            return false;
        }

        public ItemData ItemData
        {
            get
            {
                return TileData.ItemTable[m_ItemID & 0x3FFF];
            }
        }

        public virtual void OnItemUsed(Mobile from, Item item)
        {
            if (m_Parent is Item)
                ((Item)m_Parent).OnItemUsed(from, item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).OnItemUsed(from, item);
        }

        public virtual bool CheckItemUse(Mobile from, Item item)
        {
            if (m_Parent is Item)
                return ((Item)m_Parent).CheckItemUse(from, item);
            else if (m_Parent is Mobile)
                return ((Mobile)m_Parent).CheckItemUse(from, item);
            else
                return true;
        }

        public virtual void OnItemLifted(Mobile from, Item item)
        {
            if (this.GetItemBool(ItemBoolTable.OnSpawner))
                this.SetItemBool(ItemBoolTable.OnSpawner, false);

            if (this.GetItemBool(ItemBoolTable.RemoveHueOnLift))
                this.Hue = 0;

            if (this.GetItemBool(ItemBoolTable.NormalizeOnLift))
                Utility.NormalizeOnLift(item);

            if (m_Parent is Item)
                ((Item)m_Parent).OnItemLifted(from, item);
            else if (m_Parent is Mobile)
                ((Mobile)m_Parent).OnItemLifted(from, item);

            if (from != null && item != null)
                if (item.GetFlag(LootType.Rare))
                    from.RareAcquisitionLog(item);
        }

        //		public virtual bool CheckLift( Mobile from, Item item )
        //		{
        //			if ( m_Parent is Item )
        //				return ((Item)m_Parent).CheckLift( from, item );
        //			else if ( m_Parent is Mobile )
        //				return ((Mobile)m_Parent).CheckLift( from, item );
        //			else
        //				return true;
        //		}

        public bool CheckLift(Mobile from)
        {
            LRReason reject = LRReason.Inspecific;

            return CheckLift(from, this, ref reject);
        }

        public virtual bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {
            if (m_Parent is Item)
                return ((Item)m_Parent).CheckLift(from, item, ref reject);
            else if (m_Parent is Mobile)
                return ((Mobile)m_Parent).CheckLift(from, item, ref reject);
            else if (GetItemBool(ItemBoolTable.MustSteal))
            {   // items can now be flagged as must steal
                reject = LRReason.TryToSteal;
                return false;
            }
            else
                return true;
        }


        public virtual bool CanTarget { get { return true; } }
        public virtual bool DisplayLootType { get { return true; } }

        public virtual void OnSingleClickContained(Mobile from, Item item)
        {
            try
            {
                if (m_Parent is Item)
                    ((Item)m_Parent).OnSingleClickContained(from, item);
            }
            catch (Exception ex)
            {
                Diagnostics.LogHelper.LogException(ex);
            }
        }
        public enum InformType
        {
            None,
            SingleClick
        }
        public bool NeighborInform(Mobile from, InformType type)
        {
            int informed = 0;
            if (this.Map != null && this.Map != Map.Internal)
            {
                List<Item> list = new();
                Point3D target = Location;
                IPooledEnumerable eable = Map.Felucca.GetItemsInRange(target, 0);
                foreach (Item item in eable)
                    if (item != null && !item.Deleted && item.Map == this.Map)
                        if (item.Z == target.Z)
                            list.Add(item);
                eable.Free();

                // you are my neighbor!
                // We will inform you something has just happened to me.
                foreach (Item item in list)
                    if (item == this)
                        continue;
                    else
                        informed += (item.Inform(from, type, this) == true) ? 1 : 0;
            }

            return informed > 0;
        }
        public virtual bool Inform(Mobile from, InformType type, Item item)
        {
            return false;
        }
        public virtual void OnAosSingleClick(Mobile from)
        {
            ObjectPropertyList opl = this.PropertyList;

            if (opl.Header > 0)
                from.Send(new MessageLocalized(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, opl.Header, this.Name, opl.HeaderArgs));
        }

        public static bool UseOldNames { get { return Core.RuleSets.AnyAIShardRules(); } }

        public virtual void OnSingleClick(Mobile from)
        {
            try
            {
                if (Deleted || !from.CanSee(this))
                    return;

                // we not tell immediate neighbors of at least this event.
                //  It allows us to process 'is a' events when we only have a 'have a' relationship.
                //  For instance, the teleporters 'have a' Sparkle. We would like to know when
                //  the sparkle is clicked so we can issue a message/name of the event.
                if (InformNeighbor && NeighborInform(from, InformType.SingleClick))
                    return;

                // blessed / cursed, etc.
                if (DisplayLootType)
                    LabelLootTypeTo(from);

                NetState ns = from.NetState;

                // Move away from the RunUO and probably new-school 'label' mechanism for labeling stuff.
                //	as it turns out, labels are less 'era accurate' than using the this.ItemData.Name.
                //	We like this.ItemData.Name because it gives us plural where LabelNumber does not.
                // For instance, the label for a board is 1027127. This maps to "board" IF we have plural, 
                //	we need to handle this ourself. If we use this.ItemData.Name it's done for us.
                if (ns != null)
                {   // give all shards old-school naming
                    if (UseOldNames)
                    {
                        ns.Send(new UnicodeMessage(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", OldSchoolName()));
                    }
                    else
                    {
                        string name = this.Name;

                        if (name == null)
                        {
                            if (m_Amount <= 1)
                                ns.Send(new MessageLocalized(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, LabelNumber, "", ""));
                            else
                                ns.Send(new MessageLocalizedAffix(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, LabelNumber, "", AffixType.Append, String.Format(" : {0}", m_Amount), ""));
                        }
                        else
                        {
                            ns.Send(new UnicodeMessage(m_Serial, m_ItemID, MessageType.Label, 0x3B2, 3, "ENU", "", name + (m_Amount > 1 ? " : " + m_Amount : "")));
                        }
                    }
                }

                if (this is ITrigger || this is ITriggerable)
                {
                    LabelTo(from, '(' + Serial.ToString() + ')');
                    if (!string.IsNullOrEmpty(m_Label))
                        LabelTo(from, '(' + m_Label + ')');
                }
            }
            catch (Exception ex)
            {
                Diagnostics.LogHelper.LogException(ex);
            }
        }

        #region Old-School Naming

        public virtual string OldSchoolName()
        {
            Article article;
            string baseName = GetBaseOldName(out article);

            string prefix = GetOldPrefix(ref article);
            string suffix = GetOldSuffix();

            string articleStr;

            if (this.Amount != 1)
                articleStr = this.Amount.ToString() + " ";
            else if (article == Article.A)
                articleStr = "a ";
            else if (article == Article.An)
                articleStr = "an ";
            else if (article == Article.The)
                articleStr = "the ";
            else
                articleStr = null;

            return String.Concat(articleStr, prefix, baseName, suffix);
        }

        /// <summary>
        /// Add prefixes such as "accurate", "magic", ...
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        public virtual string GetOldPrefix(ref Article article)
        {
            return null;
        }

        /// <summary>
        /// Add suffixes such as "vanquishing", "ghoul's touch", ...
        /// </summary>
        /// <returns></returns>
        public virtual string GetOldSuffix()
        {
            return null;
        }

        public string GetBaseOldName()
        {
            Article article;

            return GetBaseOldName(out article);
        }

        /// <summary>
        /// Old name without any prefixes/suffixes.
        /// </summary>
        /// <param name="article"></param>
        /// <returns></returns>
        public string GetBaseOldName(out Article article)
        {
            string oldName = this.OldName;

            if (oldName != null)
            {
                article = this.OldArticle;

                return SetPlural(oldName, this.Amount != 1);
            }

            string name = string.IsNullOrEmpty(this.Name) ? null : this.Name;

            if (name != null)
                return PopArticle(name, out article);

            int labelNumber = this.LabelNumber;

            if (labelNumber != Utility.GetDefaultLabel(this.ItemID))
            {
                string labelAsString;
                Server.Text.Cliloc.Lookup.TryGetValue(labelNumber, out labelAsString);

                if (!string.IsNullOrEmpty(labelAsString))
                    return PopArticle(labelAsString, out article);
            }

            ItemData id = this.ItemData;

            string itemName = id.Name;

            if (itemName != null)
                itemName = itemName.Trim();

            if (id.Flags.HasFlag(TileFlag.ArticleA) && id.Flags.HasFlag(TileFlag.ArticleAn))
                article = Article.The;
            else if (id.Flags.HasFlag(TileFlag.ArticleA))
                article = Article.A;
            else if (id.Flags.HasFlag(TileFlag.ArticleAn))
                article = Article.An;
            else
                article = Article.None;

            return SetPlural(itemName, this.Amount != 1);
        }

        public static string PopArticle(string str, out Article article)
        {
            if (Insensitive.StartsWith(str, "a "))
            {
                article = Article.A;

                if (str.Length == 2)
                    return String.Empty;

                return str.Substring(2);
            }

            if (Insensitive.StartsWith(str, "an "))
            {
                article = Article.An;

                if (str.Length == 3)
                    return String.Empty;

                return str.Substring(3);
            }

            if (Insensitive.StartsWith(str, "the "))
            {
                article = Article.The;

                if (str.Length == 4)
                    return String.Empty;

                return str.Substring(4);
            }

            article = Article.None;
            return str;
        }

        /// <summary>
        /// Parse UO's singular/plural format. Examples:
        /// - bread loa%ves/f%
        /// - pile%s% of wool
        /// - raw bird%s
        /// First tag denotes plural form.
        /// Second tag (if specified) denotes singular form.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="plural"></param>
        /// <returns></returns>
        public static string SetPlural(string str, bool plural)
        {
            int start = str.IndexOf('%');

            if (start == -1 || start + 1 >= str.Length)
                return str;

            int end = str.IndexOf('%', start + 1);

            if (end == -1)
                end = str.Length;

            int split = str.IndexOf('/', start + 1);

            string result = str.Substring(0, start);

            if (split == -1)
            {
                if (plural)
                    result += str.Substring(start + 1, end - (start + 1));
            }
            else
            {
                if (plural)
                    result += str.Substring(start + 1, split - (start + 1));
                else
                    result += str.Substring(split + 1, end - (split + 1));
            }

            if (end + 1 < str.Length)
                result += str.Substring(end + 1, str.Length - (end + 1));

            return result;
        }

        /// <summary>
        /// Raw old item name. Should not depend on the item's properties.
        /// </summary>
        /// <returns></returns>
        public string GetRawOldName()
        {
            string oldName = this.OldName;

            if (oldName != null)
                return oldName;

            int labelNumber = this.LabelNumber;

            if (labelNumber != Utility.GetDefaultLabel(this.ItemID))
            {
                string labelAsString;
                Server.Text.Cliloc.Lookup.TryGetValue(labelNumber, out labelAsString);

                return labelAsString;
            }

            string itemName = this.ItemData.Name;

            if (itemName != null)
                itemName = itemName.Trim();

            return itemName;
        }

        /// <summary>
        /// In case we need to add a custom old name to an item.
        /// </summary>
        public virtual string OldName
        {
            get { return null; }
        }

        /// <summary>
        /// In case we need to add a custom old name to an item.
        /// </summary>
        public virtual Article OldArticle
        {
            get { return Article.None; }
        }

        #endregion

        private static bool m_ScissorCopyLootType;

        public static bool ScissorCopyLootType
        {
            get { return m_ScissorCopyLootType; }
            set { m_ScissorCopyLootType = value; }
        }

        public virtual void ScissorHelper(Scissors scissors, Mobile from, Item new_item, int amountPerOldItem)
        {
            ScissorHelper(scissors, from, new_item, amountPerOldItem, true);
        }

        public virtual void ScissorHelper(Scissors scissors, Mobile from, Item new_item, int amountPerOldItem, bool carryHue)
        {
            int amount = Amount;

            if (amount > (60000 / amountPerOldItem)) // let's not go over 60000
                amount = (60000 / amountPerOldItem);

            Amount -= amount;

            int ourHue = new_item is Bandage ? 0 : Hue;
            Map thisMap = this.Map;
            object thisParent = this.m_Parent;
            Point3D worldLoc = this.GetWorldLocation();
            LootType type = this.LootType;

            if (Amount == 0)
                Delete();

            new_item.Amount = amount * amountPerOldItem;

            if (carryHue)
                new_item.Hue = ourHue;

            if (m_ScissorCopyLootType)
                new_item.LootType = type;

            if (!(thisParent is Container) || !((Container)thisParent).TryDropItem(from, new_item, false))
                new_item.MoveToWorld(worldLoc, thisMap);

            if (scissors != null)
                scissors.OnActionComplete(from, scissors);
        }

        public virtual void Consume()
        {
            Consume(1);
        }

        public virtual void Consume(int amount)
        {
            this.Amount -= amount;

            if (this.Amount <= 0)
                this.Delete();
        }

        public virtual void ReplaceWith(Item new_item)
        {
            if (m_Parent is Container)
            {
                ((Container)m_Parent).AddItem(new_item);
                new_item.Location = m_Location;
            }
            else
            {
                new_item.MoveToWorld(GetWorldLocation(), m_Map);
            }

            Delete();
        }

        private Mobile m_BlessedFor;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public bool StoreBought
        {
            get { return GetItemBool(ItemBoolTable.StoreBought); }
            set { SetItemBool(ItemBoolTable.StoreBought, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public bool PlayerCrafted
        {
            get { return GetItemBool(ItemBoolTable.PlayerCrafted); }
            set
            {
                Origin = (value == true) ? Genesis.Player : Origin = Genesis.Unknown;
                SetItemBool(ItemBoolTable.PlayerCrafted, value); InvalidateProperties();
            }
        }
        private byte m_TempRefCount;
        [CopyableAttribute(CopyType.DoNotCopy)]
        public byte SpawnerTempRefCount { get { return m_TempRefCount; } set { m_TempRefCount = value; } }
        public bool SpawnerTempItem
        {
            get { return GetItemBool(ItemBoolTable.IsTemplate); }
            set { SetItemBool(ItemBoolTable.IsTemplate, value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HideAttributes
        {
            get { return GetItemBool(ItemBoolTable.HideAttributes); }
            set { SetItemBool(ItemBoolTable.HideAttributes, value); }
        }
        public bool InformNeighbor
        {
            get { return GetItemBool(ItemBoolTable.NeighborInform); }
            set { SetItemBool(ItemBoolTable.NeighborInform, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool VileBlade
        {
            get { return GetItemBool(ItemBoolTable.VileBlade); }
            set { SetItemBool(ItemBoolTable.VileBlade, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HolyBlade
        {
            get { return GetItemBool(ItemBoolTable.HolyBlade); }
            set { SetItemBool(ItemBoolTable.HolyBlade, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime Created
        {
            get { return m_Created; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string BlessedForRaw
        {
            get { return m_BlessedFor == null ? "-null-" : m_BlessedFor.ToString(); }
        }
        /// <summary>
        /// BlessedFor must remain read-only via the props system as setting this value requires also unsetting another BlessedFor
        ///     item as well as setting new BlessedFor Item in the playermobile
        ///     This value can only be set programatically from PlayerMobile
        /// </summary>        
        [CopyableAttribute(CopyType.DoNotCopy)]
        public Mobile BlessedFor
        {
            get { return m_BlessedFor; }
            set
            {
                Mobile oldBlessedFor = m_BlessedFor;
                m_BlessedFor = value;
                /*
                // public static PropertyInfo GetPropertyInfo(Mobile from, ref object obj, string propertyName, bool reading, ref string failReason)
                //MethodBase.GetCurrentMethod().Name
                if (RootParent is Mobile m && oldBlessedFor != value)
                {
                    object o = this;
                    string failReason = string.Empty;
                    PropertyInfo p = Commands.Properties.GetPropertyInfo(null, ref o, "BlessedFor", true, ref failReason);
                    m.ItemPropertyValueChanging(m,this, p);
                }
                */
            }
        }
        public virtual bool CheckBlessed(object obj)
        {
            return CheckBlessed(obj as Mobile);
        }

        public virtual bool CheckBlessed(Mobile m)
        {
            if (GetFlag(LootType.Blessed) /*|| (Mobile.InsuranceEnabled && Insured)*/ )
                return true;

            return (m != null && m == m_BlessedFor);
        }

        public virtual bool CheckNewbied()
        {
            return (GetFlag(LootType.Newbied));
        }

        public virtual bool IsStandardLoot()
        {
            //if ( Mobile.InsuranceEnabled && Insured )
            //return false;

            if (m_BlessedFor != null)
                return false;

            return m_LootType == LootType.Regular;
        }
        public virtual string FormatName()
        {
            return this.ToString();
        }
        public override string ToString()
        {
            return String.Format("0x{0:X} \"{1}\"", m_Serial.Value, GetType().Name);
        }

        internal int m_TypeRef;
        private DateTime m_Created = DateTime.MinValue;

        public Item()
        {
            m_Serial = Serial.NewItem;

            Visible = true;
            Movable = true;
            Amount = 1;
            m_Map = Map.Internal;
            m_Created = DateTime.UtcNow;

            SetLastMoved();
            m_LastAccessed = DateTime.UtcNow;

            World.AddItem(this);

            Type ourType = this.GetType();
            m_TypeRef = World.m_ItemTypes.IndexOf(ourType);

            if (m_TypeRef == -1)
                m_TypeRef = World.m_ItemTypes.Add(ourType);

            //Console.WriteLine("{0}, {1}", this, m_TypeRef.ToString());
        }

        [Constructable]
        public Item(int itemID)
            : this()
        {
            m_ItemID = itemID;
        }

        public Item(Serial serial)
        {
            m_Serial = serial;

            Type ourType = this.GetType();
            m_TypeRef = World.m_ItemTypes.IndexOf(ourType);

            if (m_TypeRef == -1)
                m_TypeRef = World.m_ItemTypes.Add(ourType);

            m_LastAccessed = DateTime.UtcNow;
        }

        /// <summary>
        /// Do not call this function. Ask Taran Kain about it.
        /// </summary>
        public Serial ReassignSerial(Serial s)
        {
#if false
            CheckRehydrate();
#endif
            SendRemovePacket();
            World.RemoveItem(this);
            m_Serial = s;
            World.AddItem(this);
            m_WorldPacket = null;
            Delta(ItemDelta.Update);

            return m_Serial;
        }
        public virtual void OnSectorActivate()
        {
        }

        public virtual void OnSectorDeactivate()
        {
        }

        #region Internal Map Storage 
        // we don't want staff setting this.
        //  To move a mobile to int map storage, use: [MoveToIntMapStorage
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public bool IsIntMapStorage
        {
            get { return GetItemBool(ItemBoolTable.IsIntMapStorage); }
            set
            {
                SetItemBool(ItemBoolTable.IsIntMapStorage, value);
                //int count = 0;
                //SetDeepIntMapStorage(item: this, value: value, count: ref count);
            }
        }
        #region Obsolete
#if false
        private static int SetDeepIntMapStorage(Item item, bool value, ref int count)
        {
            item.SetItemBool(ItemBoolTable.IsIntMapStorage, value);
            

            foreach (var check_item in item.GetDeepItems())
                if (check_item is Item set_item)
                {
                    set_item.SetItemBool(ItemBoolTable.IsIntMapStorage, value);
                    count++;

                    foreach (var sub_item in set_item.PeekItems)
                        if (sub_item is Item set_sub_item)
                        {
                            set_sub_item.SetItemBool(ItemBoolTable.IsIntMapStorage, value);
                            count++;
                        }
                }
            return count;

            return 1;
        }
#endif
        #endregion Obsolete
        public virtual bool MoveToIntStorage()
        {
            return MoveToIntStorage(false);
        }

        public virtual bool MoveToIntStorage(bool PreserveLocation)
        {
            if (Map == null)
                return false;

            IsIntMapStorage = true;
            if (PreserveLocation == true)
                MoveToWorld(Location, Map.Internal);
            else
                Internalize();
            return true;
        }

        public virtual bool RetrieveItemFromIntStorage(Point3D p, Map m)
        {
            if (Deleted == true || p == Point3D.Zero)
                return false;

            IsIntMapStorage = false;
            MoveToWorld(p, m);
            return true;
        }

        // Moves the item to the mobiles backpack
        public virtual bool RetrieveItemFromIntStorage(Mobile m)
        {
            if (Deleted == true || m == null || m.Deleted == true || m.Map == Map.Internal)
                return false;

            IsIntMapStorage = false;
            m.AddToBackpack(this);
            return true;
        }

        // Moves the item to whatever container is passed to it, this should ignore item count/weight limit restrictions and simply place the item into the container. 
        public virtual bool RetrieveItemFromIntStorage(Container c)
        {
            if (Deleted == true || c == null || c.Deleted == true || c.Map == Map.Internal)
                return false;

            IsIntMapStorage = false;
            c.DropItem(this);
            return true;
        }
        #endregion Internal Map Storage 

        public double GetDistanceToSqrt(Point3D p)
        {
            int xDelta = m_Location.m_X - p.m_X;
            int yDelta = m_Location.m_Y - p.m_Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        public double GetDistanceToSqrt(Item i)
        {
            int xDelta = m_Location.m_X - i.m_Location.m_X;
            int yDelta = m_Location.m_Y - i.m_Location.m_Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        public double GetDistanceToSqrt(IPoint2D p)
        {
            int xDelta = m_Location.m_X - p.X;
            int yDelta = m_Location.m_Y - p.Y;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        public ArrayList GetDeepItems()
        {
            ArrayList items = new ArrayList(PeekItems);
            foreach (Item i in PeekItems)
            {
                items.AddRange(i.GetDeepItems());
            }

            return items;
        }

        /// <summary>
        /// Called after the item has been locked down or secured.
        /// </summary>
        public virtual void OnLockdown()
        {
        }

        /// <summary>
        /// Called after the item has been released.
        /// </summary>
        public virtual void OnRelease()
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsTSItemFreelyAccessible
        {
            get { return GetItemBool(ItemBoolTable.IsTsItemFreelyAccessible); }
            set { SetItemBool(ItemBoolTable.IsTsItemFreelyAccessible, value); }
        }
    }
    public class EmptyArrayList : ArrayList
    {
        public override bool IsReadOnly { get { return true; } }
        public override bool IsFixedSize { get { return true; } }

        private void OnPopulate()
        {
            Console.WriteLine("Warning: Attempted to populate a static empty ArrayList");
            Console.WriteLine(new System.Diagnostics.StackTrace());
        }

        public override int Add(object value)
        {
            OnPopulate();
            return -1;
        }

        public override void AddRange(ICollection c)
        {
            OnPopulate();
        }

        public override void InsertRange(int index, ICollection c)
        {
            OnPopulate();
        }

        public override void Insert(int index, object value)
        {
            OnPopulate();
        }

        public override void SetRange(int index, ICollection c)
        {
            OnPopulate();
        }
    }
}