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

/* Scripts\Engines\Township\TownshipRestorationDeeds.cs
 * CHANGELOG:
 *  1/31/2024, Adam
 *		Initial creation.
 *		Allows townships to be packed up and moved.
 */

using Server.Diagnostics;
using Server.Guilds;
using Server.Targeting;
using Server.Township;
using System;
using System.Collections.Generic;
using System.Linq;
using static Server.Items.TownshipStone;

namespace Server.Items
{
    public class TownshipItemRestorationDeed : Item
    {
        private Item m_Item;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public Item Item { get { return m_Item; } set { m_Item = value; } }

        private TownshipStone m_Stone;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public TownshipStone Stone { get { return m_Stone; } set { m_Stone = value; } }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public Guild Guild
        {
            get
            {
                if (m_Stone != null)
                    return m_Stone.Guild ?? null;
                return null;
            }
        }

        public override string DefaultName { get { return "a township item restoration deed"; } }
        private static bool UseDeedGraphic(Item item)
        { return item is BaseAddon || item is Server.Township.TownshipPlantItem; }
        private static bool IsAddon(Item item)
        { return UseDeedGraphic(item); }
        public TownshipItemRestorationDeed(TownshipStone stone, Item item)
            // assume the look if the item we are placing unless it's an addon
            : base(UseDeedGraphic(item) ? 0x14F0 : item.ItemID)
        {
            m_Stone = stone;
            m_Item = item;
            // assume the hue of the item we represent
            Hue = UseDeedGraphic(item) ? Township.TownshipSettings.RestorationHue : item.Hue;
            LootType = LootType.Blessed;
            m_Item.MoveToIntStorage();  // stash it away
        }

        public override void OnSingleClick(Mobile from)
        {
            try
            {
                if (m_Stone != null && m_Stone.Guild != null)
                {
                    if (m_Item is Server.Engines.Plants.StaticPlantItem plant)
                        LabelTo(from, string.Format("{0} {1} for [{2}] (deed)", plant.PlantHue, plant.PlantType, m_Stone.Guild.Abbreviation));
                    else
                        LabelTo(from, string.Format("{0} for [{1}] (deed)", m_Item.SafeName, m_Stone.Guild.Abbreviation));
                }
            }
            catch
            {
                this.SendSystemMessage("This deed is corrupted. Please contact a GM.");
                LogHelper logger = new LogHelper("TownshipItemRestorationDeed.log", overwrite: false, sline: true);
                logger.Log(string.Format("{0} is corrupt. Check Stone, Guild, and the underlying item.", this));
                logger.Finish();
            }
        }
        public override void OnDoubleClick(Mobile from)
        {
            #region Delay Razor from auto-opening things that have a container graphic
            // some/many of our deeds have a graphic that represent the thing they instantiate.
            //  If Razor knows that graphic to be a container, it 'double-clicks' it.
            if (this.ItemID != 0x14F0/*deed graphic*/)
            {
                TimeSpan ts2 = DateTime.UtcNow - LastMoved;
                if (ts2.TotalMilliseconds < 250)
                    return;
            }
            #endregion Delay Razor from auto-opening things that have a container graphic

            try
            {
                if (!IsChildOf(from.Backpack))
                    from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                else if (m_Stone.Guild != null && !m_Stone.Guild.IsMember(from))
                    from.SendMessage("Only guild members may place this {0}.", IsAddon(m_Item) ? "addon" : "item");
                else
                {
                    from.SendMessage("Ready to move, {0}", m_Item.SafeName);
                    MoveItemTarget.BeginMoveItem(m_Stone, from, this);
                }
            }
            catch
            {
                this.SendSystemMessage("This deed is corrupted. Please contact a GM.");
                LogHelper logger = new LogHelper("TownshipItemRestorationDeed.log", overwrite: false, sline: true);
                logger.Log(string.Format("{0} is corrupt. Check Stone, Guild, and the underlying item.", this));
                logger.Finish();
            }
        }
        public class MoveItemTarget : Target
        {
            public static void BeginMoveItem(TownshipStone stone, Mobile from, TownshipItemRestorationDeed package)
            {
                bool IsAddon = package.Item is BaseAddon;
                if ((stone != null && stone.Guild != null && stone.Guild.FixedGuildmaster == false))
                {
                    from.SendMessage("Target the spot within your township where you want to place this {0}.", IsAddon ? "addon" : "item");
                    from.Target = new MoveItemTarget(stone, package);
                }
                else
                    from.SendMessage("{0} is staff-owned, and as such, it may not be moved in this way.", stone.Guild.Name);
            }

            private TownshipStone m_Stone;
            private Item m_Item;
            private TownshipItemRestorationDeed m_Package;
            private MoveItemTarget(TownshipStone stone, TownshipItemRestorationDeed package)
                : base(-1, true, TargetFlags.None)
            {
                m_Stone = stone;
                m_Package = package;
                m_Item = package.Item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                bool IsAddon = m_Package.Item is BaseAddon;
                if (!m_Stone.CheckView(from, message: false, range_check: false))
                {
                    from.SendMessage("You must be within your township to place this.");
                    return;
                }
                if (!from.CheckAlive() || !m_Stone.CheckAccess(from, TownshipAccess.Leader))
                    return;

                Point3D loc = new Point3D(targeted as IPoint3D);

                TownshipBuilder.Options.Configure(TownshipBuilder.BuildFlag.IgnoreGuildedPercentage | TownshipBuilder.BuildFlag.NeedsSurface, TownshipSettings.DecorationClearance, m_Stone);

                if (TownshipBuilder.Validate(from, loc, from.Map, m_Stone))
                {
                    m_Item.MoveToWorld(loc, from.Map);
                    m_Item.IsIntMapStorage = false;
                    if (m_Item is TownshipPlantItem tsi)
                        RejuvenatePlantItem(tsi);
                    m_Package.Delete();
                    from.SendMessage("You have successfully moved the township {0}.", IsAddon ? "addon" : "item");
                }
            }
            private void RejuvenatePlantItem(TownshipPlantItem pitem)
            {
                if (pitem == null || pitem.PlantSystem == null)
                    return;

                if (pitem.PlantStatus == Engines.Plants.PlantStatus.BowlOfDirt)
                    pitem.PlantStatus = Engines.Plants.PlantStatus.Seed;

                pitem.PlantSystem.Water = 2;
            }
            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
            }
        }
        public override void OnDelete()
        {
            if (this.Item != null && this.Item.IsIntMapStorage)  // not ever been moved to world, delete it (rare)
            {
                this.Item.IsIntMapStorage = false;
                this.Item.Delete();
            }
            base.OnDelete();
        }
        public TownshipItemRestorationDeed(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version);

            // version 0
            writer.Write(m_Stone);
            writer.Write(m_Item);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Stone = (TownshipStone)reader.ReadItem();
                        m_Item = reader.ReadItem();
                        break;
                    }
            }

        }
    }
    public class TownshipLivestockRestorationDeed : Item
    {
        private Mobile m_Mobile;
        public Mobile Mobile { get { return m_Mobile; } set { m_Mobile = value; } }
        private TownshipStone m_Stone;
        public override string DefaultName { get { return "a township livestock restoration deed"; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Guild Guild
        {
            get { return m_Stone.Guild ?? null; }
        }

        public TownshipLivestockRestorationDeed(TownshipStone stone, Mobile mobile)
            : base(0x14F0)                  // deed graphic
        {
            m_Stone = stone;
            m_Mobile = mobile;
            Hue = TownshipSettings.RestorationHue;
            LootType = LootType.Blessed;
            m_Mobile.MoveToIntStorage();    // stash it away
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_Stone != null && m_Stone.Guild != null)
                LabelTo(from, string.Format("{0} for [{1}]", m_Mobile.SafeName, m_Stone.Guild.Abbreviation));
            else
                ;   // error
        }

        public override void OnDoubleClick(Mobile from)
        {
            #region Delay Razor from auto-opening things that have a container graphic
            // some/many of our deeds have a graphic that represent the thing they instantiate.
            //  If Razor knows that graphic to be a container, it 'double-clicks' it.
            if (this.ItemID != 0x14F0/*deed graphic*/)
            {
                TimeSpan ts2 = DateTime.UtcNow - LastMoved;
                if (ts2.TotalMilliseconds < 250)
                    return;
            }
            #endregion Delay Razor from auto-opening things that have a container graphic

            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else if (m_Stone.Guild != null && !m_Stone.Guild.IsMember(from))
                from.SendMessage("Only guild members may place this livestock.");
            else
            {
                from.SendMessage("Ready to restore, {0}", m_Mobile.SafeName);
                MoveItemTarget.BeginMoveItem(m_Stone, from, this);
            }
        }
        public class MoveItemTarget : Target
        {
            public static void BeginMoveItem(TownshipStone stone, Mobile from, TownshipLivestockRestorationDeed package)
            {
                if ((stone != null && stone.Guild != null && stone.Guild.FixedGuildmaster == false))
                {
                    from.SendMessage("Target the spot within your township where you want to place this livestock.");
                    from.Target = new MoveItemTarget(stone, package);
                }
                else
                    from.SendMessage("{0} is staff-owned, and as such, it may not be moved in this way.", stone.Guild.Name);
            }

            private TownshipStone m_Stone;
            private Mobile m_Mobile;
            private TownshipLivestockRestorationDeed m_Package;
            private MoveItemTarget(TownshipStone stone, TownshipLivestockRestorationDeed package)
                : base(-1, true, TargetFlags.None)
            {
                m_Stone = stone;
                m_Package = package;
                m_Mobile = package.Mobile;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.CheckAccess(from, TownshipAccess.Leader))
                    return;

                Point3D loc = new Point3D(targeted as IPoint3D);

                TownshipBuilder.Options.Configure(TownshipBuilder.BuildFlag.IgnoreGuildedPercentage | TownshipBuilder.BuildFlag.NeedsSurface, TownshipSettings.DecorationClearance, m_Stone);

                if (TownshipBuilder.Validate(from, loc, from.Map, m_Stone))
                {
                    m_Mobile.MoveToWorld(loc, from.Map);
                    m_Mobile.IsIntMapStorage = false;
                    if (m_Mobile is Mobiles.BaseCreature bc)
                        bc.Home = loc;
                    m_Package.Delete();
                    from.SendMessage("You have successfully restored this township livestock.");
                }
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
            }
        }
        public override void OnDelete()
        {
            if (this.Mobile.IsIntMapStorage)  // not ever been moved to world, delete it
            {
                this.Mobile.IsIntMapStorage = false;
                this.Mobile.Delete();
            }
            base.OnDelete();
        }
        public TownshipLivestockRestorationDeed(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version); // version

            // version 0
            writer.Write(m_Stone);
            writer.Write(m_Mobile);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Stone = (TownshipStone)reader.ReadItem();
                        m_Mobile = reader.ReadMobile();
                        break;
                    }
            }

        }
    }
    public class TownshipRestorationDeed : Item
    {
        private TownshipStone m_TownStone;
        private Guildstone m_GuildStone;
        private List<MovingCrate> m_MovingCrates;
        private Key m_Key;
        [CommandProperty(AccessLevel.GameMaster)]
        public override string DefaultName { get { return "a township restoration deed"; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public TownshipStone TownStone { get { return m_TownStone; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public Guildstone Guildstone { get { return m_GuildStone; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public Key Key => m_Key;
        [CommandProperty(AccessLevel.GameMaster)]
        public List<MovingCrate> MovingCrates => m_MovingCrates;
        [CommandProperty(AccessLevel.GameMaster)]
        public Guild Guild { get { return m_TownStone.Guild ?? null; } }
        public TownshipRestorationDeed(TownshipStone stone, List<MovingCrate> movingCrates, Key key)
            : base(0x14F0)
        {
            m_TownStone = stone;
            m_MovingCrates = movingCrates;
            Hue = Township.TownshipSettings.RestorationHue;
            m_GuildStone = m_TownStone.Guild.Guildstone as Guildstone;
            LootType = LootType.Blessed;
            m_GuildStone.MoveToIntStorage();    // stash 'em away
            m_TownStone.MoveToIntStorage();
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_TownStone != null && m_TownStone.Guild != null)
                LabelTo(from, string.Format("a restoration deed for {0} [{1}]", m_TownStone.SafeName, m_TownStone.Guild.Abbreviation));
            else
                ;   // error
        }

        public override void OnDoubleClick(Mobile from)
        {
            TownshipDeed.PlacementResult placement_result = TownshipDeed.CheckPlacement(from.Location, from.Map, from.Guild as Server.Guilds.Guild, radius: m_TownStone.GetRadius(), houseRequirement: TownshipDeed.HouseRequirementType.OwnedHouse);
            if (placement_result != TownshipDeed.PlacementResult.Success)
            {   // message them
                TownshipDeed.ProcessResult(from, placement_result);
                return;
            }
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else if (m_TownStone.Guild != null && !m_TownStone.Guild.IsMember(from))
                from.SendMessage("Only guild members may place this township.");
            else
            {
                from.SendMessage("Ready to move, {0}", m_TownStone.SafeName);
                // move the township region here
                m_TownStone.TownshipCenter = from.Location;
                MoveItemTarget.BeginMoveItem(m_TownStone, from, this);
            }
        }
        public class MoveItemTarget : Target
        {
            public static void BeginMoveItem(TownshipStone stone, Mobile from, TownshipRestorationDeed package)
            {
                if ((stone != null && stone.Guild != null && stone.Guild.FixedGuildmaster == false))
                {
                    from.SendMessage("Target the spot within your house where you want to place the guild stone for [{0}].", stone.Guild.Abbreviation);
                    from.Target = new MoveItemTarget(stone, package);
                }
                else
                    from.SendMessage("{0} is staff-owned, and as such, it may not be moved in this way.", stone.Guild.Name);
            }

            private TownshipStone m_TownshipStone;
            private Guildstone m_GuildStone;
            private TownshipRestorationDeed m_Package;
            private MoveItemTarget(TownshipStone stone, TownshipRestorationDeed package)
                : base(-1, true, TargetFlags.None)
            {
                m_TownshipStone = stone;
                m_Package = package;
                m_GuildStone = package.Guildstone;
            }
            private static bool ValidTarget(object targeted, out Point3D loc)
            {
                loc = Point3D.Zero;
                IPoint3D p = targeted as IPoint3D;

                if (p == null)
                    return false;

                if (p is Item)
                    p = ((Item)p).GetWorldTop();
                else if (p is Mobile)
                    p = ((Mobile)p).Location;
                loc = new Point3D(p);
                return true;
            }
            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.CheckAlive() || !m_TownshipStone.CheckView(from, message: true, range_check: false) || !m_TownshipStone.CheckAccess(from, TownshipAccess.Leader))
                    return;

                Point3D loc = new Point3D();
                if (!ValidTarget(targeted, out loc))
                    return;

                Point3D[] locs = new Point3D[] { loc, Point3D.Zero, Point3D.Zero };
                if (!Utility.CanSpawnLandMobile(from.Map, locs[0]))
                    locs[0] = Utility.GetPointNearby(from.Map, locs[0], max_dist: 2, avoid_doors: true, avoid: locs);   // townstone
                locs[1] = Utility.GetPointNearby(from.Map, locs[0], max_dist: 2, avoid: locs);                          // guild stone
                locs[2] = Utility.GetPointNearby(from.Map, locs[0], max_dist: 2, avoid: locs);                          // moving crates
                if (!locs.Contains(Point3D.Zero))
                {
                    m_TownshipStone.MoveToWorld(locs[0], from.Map);
                    m_GuildStone.MoveToWorld(locs[1], from.Map);

                    m_TownshipStone.IsIntMapStorage = false;
                    m_GuildStone.IsIntMapStorage = false;

                    foreach (MovingCrate crate in m_Package.MovingCrates)
                    {
                        crate.MoveToWorld(locs[2], from.Map);
                        crate.IsIntMapStorage = false;
                        locs[2].Z += 3; // stacking
                    }
                    // unset this bool as now we will check against the existence of the crates
                    m_TownshipStone.SetTownshipStoneBool(TownshipStoneBoolTable.IsPackedUp, false);
                    m_Package.Delete();
                    from.SendMessage("You have successfully moved the township for {0}.", m_TownshipStone.Guild.Name);
                }
                else
                {
                    from.SendMessage("Unable to build the township stones here for {0}.", m_TownshipStone.Guild.Name);
                    from.SendMessage("You'll need thee adjacent tiles and not near a door.");
                }
            }
            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
            }
        }
        public override void OnDelete()
        {
            if (this.m_GuildStone.IsIntMapStorage)  // not ever been moved to world, delete it (rare)
            {   // Warning! deleting the guild stones, deletes the town stone, and everything attached
                this.m_GuildStone.IsIntMapStorage = false;
                this.m_GuildStone.Delete();
            }
            if (m_MovingCrates != null)
                foreach (var crate in m_MovingCrates)
                    if (crate.Map == Map.Internal)
                        crate.Delete();
            base.OnDelete();
        }
        public TownshipRestorationDeed(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version);

            // version 0
            writer.Write(m_TownStone);
            writer.Write(m_GuildStone);
            writer.WriteItemList<MovingCrate>(m_MovingCrates);
            writer.Write(m_Key);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_TownStone = (TownshipStone)reader.ReadItem();
                        m_GuildStone = (Guildstone)reader.ReadItem();
                        m_MovingCrates = reader.ReadStrongItemList<MovingCrate>();
                        m_Key = (Key)reader.ReadItem();
                        break;
                    }
            }

        }
    }
}