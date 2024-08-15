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

/* Scripts/Items/Addons/BaseAddonDeed.cs
 * ChangeLog
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
 *      Push music system down to BaseAddon(deed)
 *  1/18/22, Yoar
 *      BaseAddonDeed no longer implement the ICraftable interface.
 *      The ICraftable interface provides CraftResource support for addon deeds. However, we
 *      don't want colored addons at this time. Note that, once the ICraftable *is*
 *      implemented, the "OnCraft" method must appropriately set the hue of those addons that
 *      *are* colored (example: stone furniture).
 *  1/17/22, Yoar
 *      Added support for township addons.
 *  1/11/22, Yoar
 *      Merge with RunUO
 *      - Added CraftResource field (currently unused)
 *  10/21/21 Yoar
 *      Rewrote AddonDeed: Renamed m_itemID to m_ComponentID and added the fields:
 *      m_Facing, m_Redeedable. By default, these addons are redeedable. All
 *      existing fishing rares of this type are made redeedable as well.
 *  9/03/06 Taran Kain
 *		Changed targeting process, removed BlockingDoors().
 *  9/01/06 Taran Kain
 *		Added call to new BaseAddon.OnPlaced() hook
 *	5/17/05, erlein
 *		Altered BlockingObject() to perform single test instance instead of one per addon type
 *		that doesn't block.
 *	9/19/04, mith
 *		InternalTarget.OnTarget(): Added call to ConfirmAddonPlacementGump to allow user to cancel placement without losing original deed.
 *	9/18/04, Adam
 *		Add the new function BlockingObject() to determine if something would block the door.
 * 		Pass the result of BlockingObject() to addon.CouldFit().
 *  8/5/04, Adam
 * 		Changed item to LootType.Regular from LootType.Newbied.
 */

using Server.Engines;
using Server.Gumps;
using Server.Misc;
using Server.Multis;
using Server.Targeting;
using Server.Township;
using System;
using System.IO;

namespace Server.Items
{
    [NoSort]
    [Flipable(0x14F0, 0x14EF)]
    public abstract class BaseAddonDeed : Item//, ICraftable
    {
        public abstract BaseAddon Addon { get; }

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
        #region Music
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
                        SendSystemMessage(string.Format("Adding music {0}", item));
                        AddItem(item);
                    }
                }
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
        #endregion Music
        public BaseAddonDeed(int GraphicID)
            : base(GraphicID)
        {
            Weight = 1.0;

            if (!Core.RuleSets.AOSRules())
                LootType = LootType.Regular;
        }

        public BaseAddonDeed()
            : this(0x14F0)
        {

        }

        public BaseAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            writer.WriteEncodedInt(m_range_enter);
            writer.WriteEncodedInt(m_range_exit);
            writer.Write(m_time_between);

            // version 2
            writer.Write(m_RazorFile);

            // version 1
            writer.WriteEncodedInt((int)m_Resource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_range_enter = reader.ReadEncodedInt();
                        m_range_exit = reader.ReadEncodedInt();
                        m_time_between = reader.ReadInt();
                        goto case 2;
                    }
                case 2:
                    {
                        m_RazorFile = reader.ReadString();
                        goto case 1;
                    }
                case 1:
                    {
                        m_Resource = (CraftResource)reader.ReadEncodedInt();
                        break;
                    }
            }

            if (Weight == 0.0)
                Weight = 1.0;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsChildOf(from.Backpack))
            {
                if (!CanPlace(from))
                    return;

                from.SendMessage("Target where thy wouldst build thy addon, or target thyself to build it where thou'rt standing.");
                from.Target = new InternalTarget(this);
            }
            else
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
        }

        public void Place(IPoint3D p, Map map, Mobile from)
        {
            if (p == null || map == null || this.Deleted)
                return;

            if (IsChildOf(from.Backpack))
            {
                if (!CanPlace(from))
                    return;

                BaseAddon addon = this.Addon; // this creates an instance, don't use Addon (capital A) more than once!

                addon.Resource = this.Resource;

                if (addon.RetainDeedHue)
                    addon.Hue = this.Hue;

                addon.RazorFile = m_RazorFile;
                if (GetRSM() != null)
                    addon.AddItem(GetRSM());

                addon.DelayBetween = m_time_between;
                addon.RangeEnter = m_range_enter;
                addon.RangeExit = m_range_exit;

                Server.Spells.SpellHelper.GetSurfaceTop(ref p);

                TownshipBuilder.BuildFlag buildFlags = TownshipBuilder.BuildFlag.NeedsSurface;

                AddBuildFlags(ref buildFlags);

                TownshipBuilder.Options.Configure(buildFlags, TownshipSettings.DecorationClearance);

                TownshipBuilder.BuildResult result = TownshipBuilder.Check(from, new Point3D(p), map, addon);

                bool valid = false;
                BaseHouse house = null;

                if (result == TownshipBuilder.BuildResult.Valid)
                {
                    valid = true;
                }
                else if (TownshipAddonAttribute.Check(addon))
                {
                    TextDefinition.SendMessageTo(from, TownshipBuilder.GetMessage(result));
                }
                else
                {
                    AddonFitResult res = addon.CouldFit(p, map, from, ref house);

                    switch (res)
                    {
                        case AddonFitResult.Blocked:
                            from.SendLocalizedMessage(500269); break; // You cannot build that there.
                        case AddonFitResult.NotInHouse:
                            from.SendLocalizedMessage(500274); break; // You can only place this in a house that you own!
                        case AddonFitResult.DoorTooClose:
                            from.SendLocalizedMessage(500271); break; // You cannot build near the door.
                        case AddonFitResult.NoWall:
                            from.SendLocalizedMessage(500268); break; // This object needs to be mounted on something.
                        case AddonFitResult.DoorsNotClosed:
                            from.SendMessage("You must close all house doors before placing this."); break;
                    }

                    valid = (res == AddonFitResult.Valid);
                }

                if (valid)
                {
                    // set the 'preview' flag so packing up a house will ignore it (exploit prevention)
                    //  the bit is cleared once placed.
                    //addon.SetItemBool(ItemBoolTable.Preview, true);
                    BaseAddon.AddPreview(addon, from);

                    addon.MoveToWorld(new Point3D(p), map);

                    Delete();
#if false
                    if (house != null)
                        house.Addons.Add(addon);

                    if (house == null)
                        Server.Township.TownshipItemHelper.SetOwnership(addon, from);
#endif
                    addon.OnAfterPlace(from, house);
                    new ConfirmationObject(addon, from);
                    from.SendGump(new ConfirmAddonPlacementGump(from, addon, house));
                }
                else
                {
                    addon.Delete();
                }
            }
            else
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
        }

        public class ConfirmationObject
        {
            BaseAddon m_Addon;
            Mobile m_Mobile;
            int m_Count;
            Timer m_Timer = null;
            const int TimeOut = 10;
            public ConfirmationObject(BaseAddon addon, Mobile mobile)
            {
                m_Addon = addon;
                m_Mobile = mobile;
                m_Count = 0;
                m_Timer = Timer.DelayCall(delay: TimeSpan.FromSeconds(1), interval: TimeSpan.FromSeconds(1), count: 30, new TimerStateCallback(ConfirmationTick), new object[] { });
            }

            private void ConfirmationTick(object state)
            {
                bool check_addon = m_Addon != null && m_Addon.Deleted == false;
                bool check_mobile = m_Mobile != null && m_Mobile.Deleted == false;
                if (!check_addon || !check_mobile) { m_Timer.Stop(); return; }
                bool dead = m_Mobile.Dead;
                bool disconnected = m_Mobile.NetState == null || m_Mobile.NetState.Running == false;
                double distance_to = m_Addon.DistanceTo(m_Mobile);
                bool is_preview = m_Addon.GetItemBool(Item.ItemBoolTable.Preview);
                bool timeout = m_Count++ >= TimeOut;

                if (dead || disconnected || distance_to > 13 || timeout)
                    goto cleanup;

                if (!is_preview)
                {
                    // they accepted the placement
                    m_Timer.Stop();
                    return;
                }

                return;

            cleanup:
                m_Timer.Stop();
                BaseAddon.RemovePreview(m_Addon, m_Mobile);
                BaseAddon.ReturnDeed(m_Addon, m_Mobile);
                m_Mobile.CloseGump(typeof(ConfirmAddonPlacementGump));
            }
        }
        public virtual bool CanPlace(Mobile from)
        {
            if (from == null) return false;
            foreach (Mobile m in BaseAddon.PreviewAddons.Values)
                if (m == from)
                {
                    from.SendMessage("You cannot place an addon while previewing another.");
                    return false;
                }

            return true;
        }

        protected virtual void AddBuildFlags(ref TownshipBuilder.BuildFlag buildFlags)
        {
        }

        private class InternalTarget : Target
        {
            private BaseAddonDeed m_Deed;
            private bool m_SecondTime;
            private Point3D m_Point;

            public InternalTarget(BaseAddonDeed deed)
                : this(deed, false, Point3D.Zero)
            {
            }

            private InternalTarget(BaseAddonDeed deed, bool secondtime, Point3D point)
                : base(-1, true, TargetFlags.None)
            {
                m_Deed = deed;
                m_SecondTime = secondtime;
                m_Point = point;

                CheckLOS = false;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                IPoint3D p = targeted as IPoint3D;

                if (from == targeted)
                {
                    if (m_SecondTime)
                    {
                        p = m_Point;
                    }
                    else
                    {
                        from.SendMessage("Now, walk thyself away from this place and target thyself again.");
                        from.Target = new InternalTarget(m_Deed, true, from.Location);
                        return;
                    }
                }

                m_Deed.Place(p, from.Map, from);
            }
        }

        #region ICraftable Members

#if RunUO
        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            CraftContext context = craftSystem.GetContext(from);

            if (context == null || !context.DoNotColor)
            {
                Type resourceType = craftItem.Resources.GetAt(0).ItemType;

                if (resourceType == (craftItem.UseSubRes2 ? craftSystem.CraftSubRes2 : craftSystem.CraftSubRes).ResType && typeRes != null)
                    resourceType = typeRes;

                CraftResource res = CraftResources.GetFromType(resourceType);

                if (res != CraftResource.None)
                    Resource = res;
                else
                    Hue = resHue;
            }

            return quality;
        }
#endif

        #endregion
    }

    public class AddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new Addon(m_ComponentID, m_Facing, m_IsRedeedable); } }

        private int m_ComponentID;
        private Direction m_Facing;
        private bool m_IsRedeedable;

        [CommandProperty(AccessLevel.GameMaster)]
        public int ComponentID
        {
            get { return m_ComponentID; }
            set
            {
                if (m_ComponentID != value)
                {
                    m_ComponentID = value;

                    UpdateName();
                }
            }
        }

        public virtual void UpdateName()
        {
            ItemData id = TileData.ItemTable[m_ComponentID & 0x3FFF];

            if (m_Facing >= Direction.North && m_Facing < Direction.Mask)
                this.Name = string.Format("{0} ({1})", id.Name, m_Facing.ToString().ToLower());
            else
                this.Name = string.Format("{0}", id.Name);
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
        public AddonDeed(int itemID)
            : this(itemID, Direction.Mask, true)
        {
        }

        [Constructable]
        public AddonDeed(int itemID, Direction dir)
            : this(itemID, dir, true)
        {
        }

        [Constructable]
        public AddonDeed(int itemID, Direction dir, bool redeedable)
            : base()
        {
            m_ComponentID = itemID;
            m_Facing = dir;
            m_IsRedeedable = redeedable;
            UpdateName();
        }

        public AddonDeed(Serial serial)
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

                Convert(m_ComponentID, ref m_Facing, ref m_IsRedeedable);

                UpdateName();
            }
        }

        public static void Convert(int itemID, ref Direction facing, ref bool redeedable)
        {
            switch (itemID)
            {
                case 0x0DBB: redeedable = true; break; // 3515 (0x0DBB) Seaweed
                case 0x0C2E: redeedable = true; break; // 3118 (0x0C2E) debris
                case 0x154D: redeedable = true; break; // 5453 (0x054D) water barrel � empty or filled we don't have them on ai yet.
                case 0x0DC9: redeedable = true; break; // 3529 (0x0DC9) fishing net � unhued and oddly shaped
                case 0x1E9A: redeedable = true; break; // 7834 (0x1E9A) hook
                case 0x1E9D: redeedable = true; break; // 7837 (0x1E9D) pulleys
                case 0x1E9E: redeedable = true; break; // 7838 (0x1E9E) Pulley
                case 0x1EA0: redeedable = true; break; // 7840 (0x1EA0) Rope
                case 0x1EA9: redeedable = true; facing = Direction.South; break; // 7849 (0x1EA9) Winch � south
                case 0x1EAC: redeedable = true; facing = Direction.East; break; // 7852 (0x1EAC) Winch � east
                case 0x0FCD: redeedable = true; facing = Direction.South; break; // 4045 (0x0FCD) string of shells � south
                case 0x0FCE: redeedable = true; facing = Direction.South; break; // 4046 (0x0FCE) string of shells � south
                case 0x0FCF: redeedable = true; facing = Direction.South; break; // 4047 (0x0FCF) � string of shells � south
                case 0x0FD0: redeedable = true; facing = Direction.South; break; // 4048 (0x0FD0) � string of shells � south
                case 0x0FD1: redeedable = true; facing = Direction.East; break; // 4049 (0x0FD1) � string of shells � east
                case 0x0FD2: redeedable = true; facing = Direction.East; break; // 4050 (0x0FD2) � string of shells � east
                case 0x0FD3: redeedable = true; facing = Direction.East; break; // 4051 (0x0FD3) � string of shells � east
                case 0x0FD4: redeedable = true; facing = Direction.East; break; // 4052 (0x0FD4) � string of shells � east
                case 0x0E74: redeedable = true; break; // 3700 (0x0E74) Cannon Balls
                case 0x0C2C: redeedable = true; break; // 3116 (0x0C2C) Ruined Painting
                case 0x0C18: redeedable = true; break; // 3096 (0x0C18) Covered chair - (server birth on osi)
                case 0x1EA3: redeedable = true; facing = Direction.South; break; // 7843 (0x1EA3) net � south
                case 0x1EA4: redeedable = true; facing = Direction.East; break; // 7844 (0x1EA4) net � east
                case 0x1EA5: redeedable = true; facing = Direction.South; break; // 7845 (0x1EA5) net � south
                case 0x1EA6: redeedable = true; facing = Direction.East; break; // 7846 (0x1EA6) net � east
            }
        }
    }
}