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

/* Scripts/Items/Addons/BaseAddon.cs
 * ChangeLog
 *  4/10/2024, Adam (Timed load of components)
 *      (Moved here from XMLAddon)
 *      For more than a year we've seen periodic client crashes when loading very large addons.
 *      I've moved the loading to a timer which stops the crash. My theory is that we were overwhelming the client with packets.
 *  3/28/2024, Adam (Preview Addons)
 *      Fix a bug in DistanceTo(Mobile m)
 *  3/11/2024, Adam
 *   Create a robust system for addon previews:
 *   Affected files: BaseAddon.cs, BaseAddonDeed.cs, and ConfirmAddonPlacement.cs
 *   * Remove the kooky CancelPacement logic (unneeded, and wrong as well.)
 *   Auto Cancel placement and return deed if
 *   a. Player died
 *   b. Player disconnected
 *   c. Player's distance to the addon > X
 *   d. Time to place the addon has timed out.
 *   e. if the server saved the preview, and it still exists on restart, delete the addon and return the deed.
 *    Disallow placement if:
 *    the player is currently previewing another addon.
 *  12/26/2023, Adam
 *      Push music system down to BaseAddon
 *  11/12/23, Yoar (IAddon)
 *      Added IAddon interface
 *  11/1/2023, Adam (Addons to go)
 *      When redeedable addons are 'dropped' they are automatically converted to their respective deed.
 *      This, of course, only applies addons that are movable. 
 *      Use Case: For our camps, we have an 'ore cart' addon. As usual, it is immovable.
 *          But if the player kills all guardians, the ore cart is made movable.
 *          When the player drops the ore cart into their backpack, it is automatically converted to deed form.
 *  1/27/22, Yoar
 *      Added the following virtual methods to BaseAddon:
 *      1. GetComponentProperties
 *      2. OnComponentSingleClick
 *      3. OnComponentDoubleClick
 *      These methods are called in AddonComponent.
 *  1/17/22, Yoar
 *      Added support for township addons.
 *  1/11/22, Yoar
 *      Merge with RunUO
 *      - Added CraftResource field (currently unused)
 *      - Renamed MakeDeed to Redeed
 *      - Rewrote CheckHouse; now takes ref BaseHouse parameter instead of ref ArrayList
 *      - Added IsWall checker (currently unused)
 *      - Added OnBeforeRedeed virtual method
 *      - Renamed OnPlaced virtual method to OnAfterPlace
 * 	11/14/21, Adam
 *		Allow overriding of the default behavior to create a component
 *		Addon(int itemID, Direction dir, bool redeedable, bool noCreate = false)
 *  10/21/21 Yoar
 *      Rewrote Addon: Renamed m_itemID to m_ComponentID and added the fields:
 *      m_Facing, m_Redeedable. By default, these addons are redeedable. All
 *      existing fishing rares of this type are made redeedable as well.
 *  9/17/21, Yoar
 *      Added virtual bool Redeedable() 
 *  3/7/07, Adam
 *      Make OnChop() virtual 
 *  9/03/06 Taran Kain
 *		Added BlocksDoors property.
 *  9/01/06 Taran Kain
 *		Virtualized CouldFit, added OnPlaced hook
 *	06/06/06, Adam
 *		Make AddComponent virtual to allow the override in derived classes.
 *		The immediate need is to make the components being added invisible.
 *	05/12/05, erlein
 *		Changed logic for access level vs addon placement checking to avoid
 *		GM and greater placed addons which are not tied to house they were placed
 *		within.
 *	9/18/04, Adam
 *		Check the new blocking flag in CouldFit() to see something would block the door.
 *		See new function BlockingObject() in BaseAddonDeed.cs
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */
//comented out line 77-103 added lines 73-76 for addon deletetion instead of redeeding

using Server.Commands;
using Server.Engines;
using Server.Misc;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Server.Items
{
    public enum AddonFitResult
    {
        Valid,
        Blocked,
        NotInHouse,
        DoorTooClose,
        NoWall,
        DoorsNotClosed
    }

    public interface IAddon
    {
        Item Deed { get; }
    }
    [NoSort]
    public class BaseAddon : Item, IChopable, IAddon
    {
        public static Dictionary<BaseAddon, Mobile> PreviewAddons = new();
        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(LoadPreviewAddons);
            EventSink.WorldSave += new Server.WorldSaveEventHandler(SavePreviewAddons);
        }

        #region Mondain's Legacy
        private CraftResource m_Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set
            {
                if (m_Resource != value)
                {
                    m_Resource = value;
                    Hue = CraftResources.GetHue(m_Resource);

                    InvalidateProperties();
                }
            }
        }
        #endregion
        #region Music Management
        private string m_RazorFile;
        [CommandProperty(AccessLevel.GameMaster)]
        public string RazorFile
        {
            get { return m_RazorFile; }
            set
            {
                if (value == null)
                    m_RazorFile = null;
                else if (!value.EndsWith(".rzr") || !File.Exists(Path.Combine(Core.DataDirectory, "song files", value)))
                    m_RazorFile = "Please enter: <name>.rzr";
                else
                {
                    CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(value);
                    SendSystemMessage(string.Format("Adding music {0}", data?.Song));

                    m_RazorFile = value;
                    // delete the rolled up sheet music if it exists
                    RolledUpSheetMusic rsm = GetRSM();
                    if (rsm != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", rsm));
                        RemoveItem(rsm);
                        rsm.Delete();
                    }
                }
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public virtual Item SheetMusic
        {
            get
            {
                foreach (Item item in PeekItems)
                    if (item is RolledUpSheetMusic rsm)
                        return rsm;
                return null;
            }
            set
            {
                Item musicObject = null;

                if (value != null && value is not RolledUpSheetMusic)
                {
                    this.SendSystemMessage("That is not rolled up sheet music");
                    return;
                }

                foreach (Item item in PeekItems)
                    if (item is RolledUpSheetMusic rsm)
                    {
                        musicObject = rsm;
                        break;
                    }

                if (musicObject != value)
                {
                    // delete an razor file reference if it exists
                    if (m_RazorFile != null)
                    {
                        if (GetRZR() != null)
                            SendSystemMessage(string.Format("Deleting music {0}", m_RazorFile));
                        m_RazorFile = null;
                    }
                    if (musicObject != null)
                    {
                        SendSystemMessage(string.Format("Deleting music {0}", musicObject));
                        RemoveItem(musicObject);
                        musicObject.Delete();
                    }

                    musicObject = value;

                    if (musicObject != null)
                    {
                        Item item = Utility.Dupe(musicObject);
                        item.SetItemBool(ItemBoolTable.StaffOwned, false);
                        SendSystemMessage(string.Format("Adding music {0}", item));
                        AddItem(item);
                    }
                }
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string NextPlay
        {
            get
            {
                object music = GetMusic();
                if (music != null && !IsPlayLocked())
                {
                    if (NextMusicTime.Triggered)
                        return "Now";
                    else
                        return string.Format("in {0} seconds", NextMusicTime.Remaining / 1000);
                }

                return "Unknown at this time";
            }
        }
        [CommandProperty(AccessLevel.Seer)]
        public string Song
        {
            get
            {
                CalypsoMusicPlayer.Metadata data = CalypsoMusicPlayer.GetMetaData(GetMusic());
                return data?.Song;
            }
        }
        private int m_time_between = 120000;                // twenty minutes until the next playback
        [CommandProperty(AccessLevel.Seer)]
        public int DelayBetween
        {
            get { return m_time_between; }
            set { m_time_between = value; }
        }
        private int m_range_enter = 5;                      // I get this close to trigger the device
        [CommandProperty(AccessLevel.Seer)]
        public int RangeEnter
        {
            get { return m_range_enter; }
            set { m_range_enter = value; }
        }
        private int m_range_exit = 15;                       // I get this far away and the music stops. -1 means no range checking
        [CommandProperty(AccessLevel.Seer)]
        public int RangeExit
        {
            get
            {
                this.SendSystemMessage("RangeExit: I get this far away and the music stops. -1 means no range checking");
                //this.SendSystemMessage("RangeExit: Currently disabled for BaseAddon");
                return m_range_exit;
            }
            set { m_range_exit = value; }
        }
        private RolledUpSheetMusic GetRSM()
        {
            foreach (Item item in PeekItems)
                if (item is RolledUpSheetMusic rsm)
                    return rsm;
            return null;
        }
        private string GetRZR()
        {
            if (m_RazorFile != null && m_RazorFile.EndsWith(".rzr") && File.Exists(Path.Combine(Core.DataDirectory, "song files", m_RazorFile)))
                return m_RazorFile;
            return null;
        }
        private object GetMusic()
        {
            object music = GetRSM();
            if (music == null)
                music = GetRZR();
            return music;
        }
        private CalypsoMusicPlayer m_MusicPlayer = new CalypsoMusicPlayer();
        private Utility.LocalTimer NextMusicTime = new Utility.LocalTimer();

        public override bool HandlesOnMovement { get { return GetMusic() != null; } }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {   // CanPlay blocks if: the player is already playing, OR another player nearby (15 tiles) is playing
            bool all_good = PlayMobile(m) && NextMusicTime.Triggered && !IsPlayLocked() && m.InRange(this, m_range_enter) && m.InLOS(this) && m_MusicPlayer.CanPlay(this);
            // check if this user is playing music elsewhere
            bool blocked = SystemMusicPlayer.IsUserBlocked(m_MusicPlayer.MusicContextMobile);
            if (all_good && !blocked)
            {
                object song = GetMusic();
                if (song != null)
                {
                    // some music devices use a fake mobile for music context. This will be the mobile we check for distance etc.
                    m_MusicPlayer.PlayerMobile = m;
                    m_MusicPlayer.RangeExit = m_range_exit;         // how far the mobile can be from this
                    //m_MusicPlayer.Play(m, this, song);
                    m_MusicPlayer.Play(Utility.MakeTempMobile(), this, song);
                    NextMusicTime.Stop();                           // just to block us. See EndMusic
                }
            }
            else if (Core.Debug && all_good && blocked)
                Utility.SendSystemMessage(m, 5, "Debug message: music not started because this player has initiated music elsewhere.");

            base.OnMovement(m, oldLocation);
        }
        private bool PlayMobile(Mobile m)
        {
            if (m is PlayerMobile pm)
            {
                if (pm.Hidden)
                {
                    if (pm.AccessLevel > AccessLevel.Player)
                    {
                        //if (!staff_notification.Recall(pm))
                        //{
                        //    SendSystemMessage("Hidden staff do not trigger music playback");
                        //    staff_notification.Remember(pm, 60);
                        //}
                    }
                    return false;
                }

                return true;
            }
            return false;
        }
        private void EndMusic(Mobile from)
        {
            NextMusicTime.Start(m_time_between);
        }
        private bool IsPlayLocked()
        {
            return !NextMusicTime.Running;
        }
        #endregion
        private ArrayList m_Components;

        public virtual void AddComponent(AddonComponent c, int x, int y, int z)
        {
            if (Deleted)
                return;

            // large addons will crash the client when we add them all at once 
            //  It's not 100% clear, but almost certainly,the client is overwhelmed with packets.
            //  But there is another issue that is less clear. I had a second character/client come up on this fort *after*
            //  It had been constructed, and he crashed too. Both clients that crashed were showing the 'hue picker gump' which
            //  looks like a client crash. This suggests a corrupted server state? 
            //  In any case, the timer technique of loading components seems like a good fix. No more crashes.
            Timer.DelayCall(TimeSpan.FromSeconds(.75), new TimerStateCallback(BuildTick), new object[] { this, c, x, y, z });

            m_Components.Add(c);
            c.Addon = this;
            c.Offset = new Point3D(x, y, z);
            c.MoveToWorld(new Point3D(X + x, Y + y, Z + z), Map.Internal);
        }
        private static void BuildTick(object state)
        {
            try
            {
                object[] aState = (object[])state;
                BaseAddon addon = aState[0] as BaseAddon;
                AddonComponent c = aState[1] as AddonComponent;
                int x = (int)aState[2];
                int y = (int)aState[3];
                int z = (int)aState[4];
                c.MoveToWorld(new Point3D(addon.X + x, addon.Y + y, addon.Z + z), addon.Map);
            }
            catch
            {
                ;
            }
        }
        public BaseAddon()
            : base(1)
        {
            Movable = false;
            Visible = false;

            m_Components = new ArrayList();
            NextMusicTime.Start(0);                 // we can play now
            m_MusicPlayer.AllDone = EndMusic;
        }
        public override Item Dupe(int amount)
        {
            BaseAddon new_addon = new BaseAddon();
            //new_addon.Components.Clear();
            //foreach(var component in this.Components)
            //    new_addon.Components.Add(component);
            return base.Dupe(new_addon, amount);
        }
        public override bool MoveToIntStorage()
        {
            if (Components != null)
                foreach (AddonComponent c in Components)
                    c.MoveToIntStorage();
            return base.MoveToIntStorage(false);
        }
        #region Addons TO GO
        public Item DropConverter()
        {
            if (Redeedable)
            {
                if (this.Deleted == false)
                {
                    Item item = Redeed(this);
                    this.Delete();
                    return item;
                }
            }

            return this;
        }
        public override bool DropToWorld(Mobile from, Point3D p)
        {
            bool result = base.DropToWorld(from, p);    // drop the original item
            if (result == true)
            {
                Item new_item = DropConverter();             // create the replacement, delete the original item
                if (new_item != this)                        // did we create a deed?
                    new_item.MoveToWorld(p, from.Map);       // replace the old item with the new
            }
            return result;
        }
        public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
        {
            bool result = base.DropToMobile(from, target, p);
            if (result == true)
            {
                Item new_item = DropConverter();             // create the replacement, delete the original item
                if (new_item != this)                        // did we create a deed?
                    from.AddToBackpack(new_item);            // replace the old item with the new
            }
            return result;
        }
        public override bool OnDroppedInto(Mobile from, Container target, Point3D p)
        {
            bool result = base.OnDroppedInto(from, target, p);
            if (result == true)
            {
                Item new_item = DropConverter();             // create the replacement, delete the original item
                if (new_item != this)                        // did we create a deed?
                    target.AddItem(new_item);                // replace the old item with the new
            }
            return result;
        }
        #endregion Addons TO GO
        public virtual bool RetainDeedHue { get { return false; } }
        public virtual bool BlocksDoors { get { return true; } }
        public virtual void OnChop(Mobile from)
        {
            bool valid = false;
            BaseHouse house = null;
            house = BaseHouse.FindHouseAt(this);

            if (!GetItemBool(Item.ItemBoolTable.Preview))
            {
                // not in a house, so is it a township?
                if (house == null)
                    valid = Server.Township.TownshipItemHelper.IsOwner(this, from);
                else if (CheckHouse(from, this.GetWorldLocation(), this.Map, 0, ref house))
                    valid = true;
            }

            if (valid)
            {
                Effects.PlaySound(GetWorldLocation(), Map, 0x3B3);
                from.SendLocalizedMessage(500461); // You destroy the item.

                if (Redeedable)
                {
                    BaseAddonDeed deed = Redeed(this) as BaseAddonDeed;
                    if (deed != null)
                    {
                        deed.RazorFile = m_RazorFile;
                        if (GetRSM() != null)
                            deed.AddItem(GetRSM());

                        deed.DelayBetween = m_time_between;
                        deed.RangeEnter = m_range_enter;
                        deed.RangeExit = m_range_exit;

                        from.AddToBackpack(deed);
                    }
                }

                if (house != null)
                    house.Addons.Remove(this);

                Delete();
            }
        }
        public virtual bool Redeedable { get { return false; } } // TODO: Core check?
        public virtual BaseAddonDeed Deed { get { return null; } }
        Item IAddon.Deed
        {
            get { return Deed; }
        }
        public static bool CanRedeed(Item item)
        {
            if (item is BaseAddon)
            {
                BaseAddon addon = (BaseAddon)item;

                return addon.Redeedable;
            }
            else if (item is IAddon)
            {
                return true;
            }

            return false;
        }
        public static Item Redeed(Item item)
        {
            Item deed = null;

            if (item is BaseAddon)
            {
                BaseAddon addon = (BaseAddon)item;

                deed = addon.Deed; // this creates an instance, don't use Deed (capital D) more than once!

                if (deed != null)
                {
                    if (deed is BaseAddonDeed)
                        ((BaseAddonDeed)deed).Resource = addon.Resource;

                    if (addon.RetainDeedHue)
                        deed.Hue = addon.GetComponentHue();

                    addon.OnBeforeRedeed(deed);
                }
            }
            else if (item is IAddon)
            {
                deed = ((IAddon)item).Deed;
            }

            // this is where RunUO would delete the addon
            // however, we delete the addon after calling the Redeed method
#if false
            if (deed != null)
                item.Delete();
#endif

            return deed;
        }
        private int GetComponentHue()
        {
            foreach (AddonComponent c in Components)
            {
                if (c.Hue != 0)
                    return c.Hue;
            }

            return 0;
        }
        public ArrayList Components
        {
            get
            {
                return m_Components;
            }
        }
        public BaseAddon(Serial serial)
            : base(serial)
        {
        }
        public virtual AddonFitResult CouldFit(IPoint3D p, Map map, Mobile from, ref BaseHouse house)
        {
            if (Deleted)
                return AddonFitResult.Blocked;

            foreach (AddonComponent c in m_Components)
            {
                Point3D p3D = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);

                Utility.CanFitFlags flags = Utility.CanFitFlags.checkMobiles;

                if (c.Z == 0)
                    flags |= Utility.CanFitFlags.requireSurface;

                if (!Utility.CanFit(map, p3D.X, p3D.Y, p3D.Z, c.ItemData.Height, flags))
                    return AddonFitResult.Blocked;
                else if (!CheckHouse(from, p3D, map, c.ItemData.Height, ref house))
                    return AddonFitResult.NotInHouse;

                // here is where RunUO would check wall requirements
                /*if (c.NeedsWall)
                {
                    Point3D wall = c.WallPosition;

                    if (!IsWall(p3D.X + wall.X, p3D.Y + wall.Y, p3D.Z + wall.Z, map))
                        return AddonFitResult.NoWall;
                }*/
            }

            if (house != null)
            {
                ArrayList doors = house.Doors;

                for (int i = 0; i < doors.Count; ++i)
                {
                    BaseDoor door = doors[i] as BaseDoor;

                    if (door != null && door.Open)
                        return AddonFitResult.DoorsNotClosed;

                    if (this.BlocksDoors)
                    {
                        Point3D doorLoc = door.GetWorldLocation();
                        int doorHeight = door.ItemData.CalcHeight;

                        foreach (AddonComponent c in m_Components)
                        {
                            Point3D addonLoc = new Point3D(p.X + c.Offset.X, p.Y + c.Offset.Y, p.Z + c.Offset.Z);
                            int addonHeight = c.ItemData.CalcHeight;

                            if (Utility.InRange(doorLoc, addonLoc, 1) && (addonLoc.Z == doorLoc.Z || ((addonLoc.Z + addonHeight) > doorLoc.Z && (doorLoc.Z + doorHeight) > addonLoc.Z)))
                                return AddonFitResult.DoorTooClose;
                        }
                    }
                }
            }

            return AddonFitResult.Valid;
        }
        public static bool CheckHouse(Mobile from, Point3D p, Map map, int height, ref BaseHouse house)
        {
            house = BaseHouse.FindHouseAt(p, map, height);

            // AI custom: GMs may place addons outside of houses
            // however, make sure that we have assigned 'house' if there is a house
            if (from.AccessLevel >= AccessLevel.GameMaster)
                return true;

            if (house == null || (from != null && !house.IsOwner(from)))
                return false;

            return true;
        }
        public static bool IsWall(int x, int y, int z, Map map)
        {
            if (map == null)
                return false;

            StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, true);

            for (int i = 0; i < tiles.Length; ++i)
            {
                StaticTile t = tiles[i];
                ItemData id = TileData.ItemTable[t.ID & 0x3FFF];

                if ((id.Flags & TileFlag.Wall) != 0 && (z + 16) > t.Z && (t.Z + t.Height) > z)
                    return true;
            }

            return false;
        }
        public virtual void OnComponentLoaded(AddonComponent c)
        {
        }
        public void InvalidateComponentProperties()
        {
            foreach (AddonComponent c in m_Components)
                c.InvalidateProperties();
        }
        public virtual void GetComponentProperties(AddonComponent c, ObjectPropertyList list)
        {
        }
        public virtual void OnComponentSingleClick(AddonComponent c, Mobile from)
        {
        }
        public virtual void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
        }
        public override void OnLocationChange(Point3D oldLoc)
        {
            if (Deleted)
                return;

            foreach (AddonComponent c in m_Components)
                c.Location = new Point3D(X + c.Offset.X, Y + c.Offset.Y, Z + c.Offset.Z);
        }
        public override void OnMapChange()
        {
            if (Deleted)
                return;

            foreach (AddonComponent c in m_Components)
                c.Map = Map;
        }
        /// <summary>
        /// Called after the addon is placed. Note: <paramref name="house"/> may be null.
        /// </summary>
        /// <param name="placer"></param>
        /// <param name="house"></param>
        public virtual void OnAfterPlace(Mobile placer, BaseHouse house)
        {
        }
        /// <summary>
        /// Called before the addon is deleted on redeed.
        /// </summary>
        /// <param name="deed"></param>
        public virtual void OnBeforeRedeed(Item deed)
        {
        }
        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            foreach (AddonComponent c in m_Components)
                c.Delete();
        }
        public virtual bool ShareHue { get { return true; } }
        [Hue, CommandProperty(AccessLevel.GameMaster)]
        public override int Hue
        {
            get
            {
                return base.Hue;
            }
            set
            {
                if (base.Hue != value)
                {
                    base.Hue = value;

                    if (!Deleted && this.ShareHue && m_Components != null)
                    {
                        foreach (AddonComponent c in m_Components)
                            c.Hue = value;
                    }
                }
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            writer.Write(m_RazorFile);
            writer.WriteEncodedInt(m_time_between);
            writer.WriteEncodedInt(m_range_enter);
            writer.WriteEncodedInt(m_range_exit);

            // version 2
            writer.WriteEncodedInt((int)m_Resource);

            // version 1
            writer.WriteItemList(m_Components);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_RazorFile = reader.ReadString();
                        m_time_between = reader.ReadEncodedInt();
                        m_range_enter = reader.ReadEncodedInt();
                        m_range_exit = reader.ReadEncodedInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_Resource = (CraftResource)reader.ReadEncodedInt();
                        goto case 1;
                    }
                case 1:
                case 0:
                    {
                        m_Components = reader.ReadItemList();
                        break;
                    }
            }

            if (version < 1 && Weight == 0)
                Weight = -1;

            NextMusicTime.Start(0);                     // we can play now
            m_MusicPlayer.AllDone = EndMusic;
        }

        #region Preview Addons
        public double DistanceTo(Mobile m)
        {
            List<double> list = new();
            list.Add(this.GetDistanceToSqrt(m));
            foreach (var component in Components)
                if (component is Item item)
                    list.Add(item.GetDistanceToSqrt(m));

            list.Sort();
            return list[0];
        }
        public static int ImpliedPreviewCancelation()
        {   // server restart, everything must go!
            int count = PreviewAddons.Count;
            foreach (var kvp in PreviewAddons)
            {
                System.Diagnostics.Debug.Assert(!kvp.Key.Deleted);
                System.Diagnostics.Debug.Assert(kvp.Value != null && !kvp.Value.Deleted);
                RemovePreview(kvp.Key, kvp.Value);
                ReturnDeed(kvp.Key, kvp.Value);
            }
            return count;
        }
        public static void ReturnDeed(BaseAddon addon, Mobile m)
        {
            System.Diagnostics.Debug.Assert(!addon.Deleted);
            Item deed = BaseAddon.Redeed(addon);
            System.Diagnostics.Debug.Assert(deed != null && m != null && !m.Deleted);

            if (deed != null && m != null && !m.Deleted)
                m.AddToBackpack(deed);

            addon.Delete();
        }
        public static void AddPreview(BaseAddon addon, Mobile m)
        {
            addon.SetItemBool(Item.ItemBoolTable.Preview, true);
            System.Diagnostics.Debug.Assert(!PreviewAddons.ContainsKey(addon));
            PreviewAddons.Add(addon, m);
        }
        public static void RemovePreview(BaseAddon addon, Mobile m)
        {   // ok, clear the exploit prevention flag (packing up a house and grabbing the 'preview' addon)
            addon.SetItemBool(Item.ItemBoolTable.Preview, false);
            System.Diagnostics.Debug.Assert(PreviewAddons.ContainsKey(addon));
            PreviewAddons.Remove(addon);
        }
        #endregion Preview Addons

        #region Serialization
        public static void LoadPreviewAddons()
        {
            if (!File.Exists("Saves/PreviewAddons.bin"))
                return;

            Console.WriteLine("Preview Addons Loading...");
            try
            {
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream("Saves/PreviewAddons.bin", FileMode.Open, FileAccess.Read)));
                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            int count = reader.ReadEncodedInt();
                            for (int ix = 0; ix < count; ix++)
                            {
                                Item addon = reader.ReadItem();
                                Mobile mobile = reader.ReadMobile();
                                if (addon is null || addon.Deleted || mobile == null || mobile.Deleted)
                                    continue;
                                PreviewAddons.Add(addon as BaseAddon, mobile);
                            }
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid PreviewAddons.bin savefile version.");
                        }
                }

                reader.Close();
            }
            catch
            {
                Utility.Monitor.WriteLine("Error reading Saves/PreviewAddons.bin, using default values:", ConsoleColor.Red);
            }
        }
        public static void SavePreviewAddons(WorldSaveEventArgs e)
        {
            Console.WriteLine("PreviewAddons Saving...");
            try
            {
                BinaryFileWriter writer = new BinaryFileWriter("Saves/PreviewAddons.bin", true);
                int version = 0;
                writer.Write((int)version); // version

                switch (version)
                {
                    case 0:
                        {
                            writer.WriteEncodedInt(PreviewAddons.Count);
                            foreach (var kvp in PreviewAddons)
                            {
                                writer.Write(kvp.Key);
                                writer.Write(kvp.Value);
                            }
                            break;
                        }
                }

                writer.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing Saves/PreviewAddons.bin");
                Console.WriteLine(ex.ToString());
            }
        }
        #endregion Serialization
    }

    public class Addon : BaseAddon
    {
        public override bool Redeedable { get { return m_IsRedeedable; } }
        public override BaseAddonDeed Deed { get { return new AddonDeed(m_ComponentID, m_Facing, m_IsRedeedable); } }

        private int m_ComponentID;
        private Direction m_Facing;
        private bool m_IsRedeedable;

        [CommandProperty(AccessLevel.GameMaster)]
        public int ComponentID
        {
            get { return m_ComponentID; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Direction Facing
        {
            get { return m_Facing; }
            set { m_Facing = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsRedeedable
        {
            get { return m_IsRedeedable; }
            set { m_IsRedeedable = value; }
        }

        [Constructable]
        public Addon(int itemID)
            : this(itemID, Direction.Mask, true)
        {
        }

        [Constructable]
        public Addon(int itemID, Direction dir)
            : this(itemID, dir, true)
        {
        }

        [Constructable]
        public Addon(int itemID, Direction dir, bool redeedable, bool noCreate = false)
            : base()
        {
            m_ComponentID = itemID;
            m_Facing = dir;
            m_IsRedeedable = redeedable;

            if (noCreate == false)
                AddComponent(new AddonComponent(m_ComponentID, true), 0, 0, 0);
        }

        public Addon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((byte)m_Facing);
            writer.Write((bool)m_IsRedeedable);

            writer.Write((int)m_ComponentID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Facing = (Direction)reader.ReadByte();
                        m_IsRedeedable = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    {
                        m_ComponentID = reader.ReadInt();
                        break;
                    }
            }

            if (version < 1)
            {
                m_Facing = Direction.Mask;

                AddonDeed.Convert(m_ComponentID, ref m_Facing, ref m_IsRedeedable);
            }
        }
    }

}