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

/* Scripts\Misc\Archive.cs
 * ChangeLog:
 *	1/3/09, Adam
 *		Created.
 */

using Server.Targeting;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public class Archive : Item
    {
        // archiver commands
        public static void Initialize()
        {
            Server.CommandSystem.Register("AddToArchive", AccessLevel.Administrator, new CommandEventHandler(OnAddToArchive));
            Server.CommandSystem.Register("RestoreFromArchive", AccessLevel.Administrator, new CommandEventHandler(OnRestoreFromArchive));
            Server.CommandSystem.Register("ReturnToArchive", AccessLevel.Administrator, new CommandEventHandler(OnReturnToArchive));
            Server.CommandSystem.Register("RemoveFromArchive", AccessLevel.Administrator, new CommandEventHandler(OnRemoveFromArchive));
            Server.CommandSystem.Register("DumpArchive", AccessLevel.Administrator, new CommandEventHandler(OnDumpArchive));
        }

        List<Mobile> m_mobiles = new List<Mobile>();
        List<Item> m_items = new List<Item>();

        [Constructable]
        public Archive()
            : base(0x9A8)   // chest graphic
        {
            Weight = 1.0;
            Name = "a unnamed world item archive";
            LootType = LootType.Blessed;
        }

        public Archive(Serial serial)
            : base(serial)
        {
        }

        public new void AddItem(Item item)
        {
            m_items.Add(item);
        }

        public void AddMobile(Mobile mobile)
        {
            m_mobiles.Add(mobile);
        }

        public int ObjectCount
        {
            get
            {
                return m_items.Count + m_mobiles.Count;
            }
        }

        public new List<Item> Items
        {
            get
            {
                return m_items;
            }
        }

        public List<Mobile> Mobiles
        {
            get
            {
                return m_mobiles;
            }
        }

        public override void OnDelete()
        {
            foreach (Item item in m_items)
            {
                if (item != null && !item.Deleted)
                {
                    item.Delete();
                }
            }

            // deleting spawners above will result in deleted mobiles here (which is fine)
            foreach (Mobile mobile in m_mobiles)
            {
                if (mobile != null && !mobile.Deleted)
                {
                    mobile.Delete();
                }
            }

            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            // version 0 
            writer.WriteMobileList<Mobile>(m_mobiles, true);
            writer.WriteItemList<Item>(m_items, true);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_mobiles = reader.ReadMobileList<Mobile>();
                        m_items = reader.ReadItemList<Item>();
                        break;
                    }
            }
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from) // Override double click of the deed to call our target
        {
            if (!IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("Usage: [AddToArchive | [RestoreFromArchive | [ReturnToArchive | [RemoveFromArchive | [DumpArchive");
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            // display information about current spawn
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                base.OnSingleClick(from);
                LabelTo(from, "Archive contains {0} items and {1} mobiles.", Items.Count, Mobiles.Count);
            }
            else
                base.OnSingleClick(from);
        }

        public static void CollectionBox_Callback(Mobile from, Map map, Point3D start, Point3D end, object state)
        {

            // Create rec and retrieve items within from bounding box callback
            // result
            Rectangle2D rect = new Rectangle2D(start.X, start.Y, end.X - start.X + 1, end.Y - start.Y + 1);
            IPooledEnumerable eable = map.GetObjectsInBounds(rect);
            Archive archive = state as Archive;

            if (archive == null || archive.Deleted)
                return;

            // temp list for the items & Mobules we want to archive
            ArrayList temp = new ArrayList();

            // Loop through and add objects returned
            foreach (object obj in eable)
            {
                Item item = obj as Item;
                Mobile mobile = obj as Mobile;
                if (item == null && mobile == null)
                    continue;

                // no deleted items
                if (item != null && (item.Deleted))
                    continue;

                // no deleted items or players
                if (mobile != null && (mobile.Deleted || mobile is Server.Mobiles.PlayerMobile))
                    continue;

                // add the item to the temp storage
                temp.Add(obj);
            }
            eable.Free();

            // items this pass
            int mCount = 0, iCount = 0;

            foreach (object obj in temp)
            {
                Item item = obj as Item;
                Mobile mobile = obj as Mobile;

                if (item != null)
                {   // move the item to the internal map at the original location
                    item.MoveToIntStorage(true);
                    // add the item to the archive
                    archive.AddItem(item);
                    iCount++;
                }

                else if (mobile != null)
                {   // move the mobile to the internal map at the original location
                    mobile.MoveToIntStorage(true);
                    // add the item to the archive
                    archive.AddMobile(mobile);
                    mCount++;
                }
            }

            from.SendMessage("{0} items and {1} mobiles added to this archive.", iCount, mCount);
            from.SendMessage("Archive now contains {0} objects total.", archive.ObjectCount);
        }

        [Usage("AddToArchive")]
        [Description("Adds items in the targeted rect to the specified archive.")]
        private static void OnAddToArchive(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Please target the archive you wish to add to.");
            e.Mobile.Target = new ArchiveAddTarget();
        }

        [Usage("RestoreFromArchive")]
        [Description("Restores all items stored in the targeted archive.")]
        private static void OnRestoreFromArchive(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Please target the archive you wish to restore from.");
            e.Mobile.Target = new ArchiveRestoreTarget();
        }

        [Usage("ReturnToArchive")]
        [Description("Returns all items to the targeted archive.")]
        private static void OnReturnToArchive(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Please target the archive you wish to rearchive.");
            e.Mobile.Target = new ArchiveReturnTarget();
        }

        [Usage("RemoveFromArchive")]
        [Description("Removed the targeted item from the targeted archive.")]
        private static void OnRemoveFromArchive(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Please target the archive to remove from.");
            e.Mobile.Target = new ArchiveRemoveTarget();
        }

        [Usage("DumpArchive")]
        [Description("Show the object serials from the targeted archive.")]
        private static void OnDumpArchive(CommandEventArgs e)
        {
            e.Mobile.SendMessage("Please target the archive to dump.");
            e.Mobile.Target = new ArchiveDumpTarget();
        }

        public class ArchiveDumpTarget : Target
        {
            public ArchiveDumpTarget()
                : base(1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                int mCount = 0, iCount = 0;
                if (target is Archive)
                {
                    Archive archive = target as Archive;
                    if (archive == null || archive.Deleted)
                        return;

                    foreach (Item item in archive.Items)
                    {
                        if (item != null && !item.Deleted)
                        {
                            from.SendMessage(item.Serial.ToString());
                            iCount++;
                        }
                    }

                    foreach (Mobile mobile in archive.Mobiles)
                    {
                        if (mobile != null && !mobile.Deleted)
                        {
                            from.SendMessage(mobile.Serial.ToString());
                            mCount++;
                        }
                    }
                }
                else
                {
                    from.SendMessage("This command only works on archives.");
                    return;
                }

                if (iCount + mCount == 0)
                    from.SendMessage("There are no objects in this archive to dump.");
                else
                    from.SendMessage("{0} items and {1} mobiles dumped from the archive.", iCount, mCount);
            }
        }

        public class ArchiveAddTarget : Target
        {
            public ArchiveAddTarget()
                : base(1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Archive)
                {
                    // Request a callback from a bounding box to establish 2D rect for use with command
                    BoundingBoxPicker.Begin(from, new BoundingBoxCallback(Archive.CollectionBox_Callback), target);
                }
                else
                {
                    from.SendMessage("This command only works on archives.");
                }
            }
        }

        public class ArchiveRestoreTarget : Target
        {
            public ArchiveRestoreTarget()
                : base(1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                int mCount = 0, iCount = 0;
                if (target is Archive)
                {
                    Archive archive = target as Archive;
                    if (archive == null || archive.Deleted)
                        return;

                    if (archive.Items.Count + archive.Mobiles.Count == 0)
                    {
                        from.SendMessage("There are no objects in this archive to restore.");
                        return;
                    }

                    foreach (Item item in archive.Items)
                    {
                        if (item != null && !item.Deleted && item.Map == Map.Internal)
                        {
                            item.RetrieveItemFromIntStorage(item.Location, from.Map);
                            iCount++;
                        }
                    }

                    foreach (Mobile mobile in archive.Mobiles)
                    {
                        if (mobile != null && !mobile.Deleted && mobile.Map == Map.Internal)
                        {
                            mobile.RetrieveMobileFromIntStorage(mobile.Location, from.Map);
                            mCount++;
                        }
                    }
                }
                else
                {
                    from.SendMessage("This command only works on archives.");
                    return;
                }

                if (iCount + mCount == 0)
                    from.SendMessage("No objects in this archive needed restoreing.");
                else
                    from.SendMessage("{0} items and {1} mobiles restored from the archive.", iCount, mCount);
            }
        }

        public class ArchiveReturnTarget : Target
        {
            public ArchiveReturnTarget()
                : base(1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                int mCount = 0, iCount = 0;
                if (target is Archive)
                {
                    Archive archive = target as Archive;
                    if (archive == null || archive.Deleted)
                        return;

                    foreach (Item item in archive.Items)
                    {
                        if (item != null && !item.Deleted && item.Map != Map.Internal)
                        {
                            item.MoveToIntStorage(true);
                            iCount++;
                        }
                    }

                    foreach (Mobile mobile in archive.Mobiles)
                    {
                        if (mobile != null && !mobile.Deleted && mobile.Map != Map.Internal)
                        {
                            mobile.MoveToIntStorage(true);
                            mCount++;
                        }
                    }
                }
                else
                {
                    from.SendMessage("This command only works on archives.");
                    return;
                }

                if (iCount + mCount == 0)
                    from.SendMessage("There are no objects outside the archive to return.");
                else
                    from.SendMessage("{0} items and {1} mobiles returned to the archive.", iCount, mCount);
            }
        }

        public class ArchiveRemoveTarget : Target
        {
            public ArchiveRemoveTarget()
                : base(1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Archive)
                {
                    from.SendMessage("Please target the object you with to remove from the archive.");
                    from.Target = new ArchiveRemoveObjectTarget(target as Archive);
                }
                else
                {
                    from.SendMessage("This command only works on archives.");
                }
            }
        }

        public class ArchiveRemoveObjectTarget : Target
        {
            Archive m_archive;
            public ArchiveRemoveObjectTarget(Archive archive)
                : base(12, false, TargetFlags.None)
            {
                m_archive = archive;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                if (target is Item && m_archive.Items.Contains(target as Item))
                {
                    m_archive.Items.Remove(target as Item);
                }
                else if (target is Mobile && m_archive.Mobiles.Contains(target as Mobile))
                {
                    m_archive.Mobiles.Remove(target as Mobile);
                }
                else
                {
                    from.SendMessage("This object does not exist in the archive.");
                    return;
                }

                from.SendMessage("The object was successfully removed from the archive.");
            }
        }
    }
}