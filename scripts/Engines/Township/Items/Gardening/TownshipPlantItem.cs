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

/* Scripts\Engines\Township\Items\Boticanals\TownshipPlantItem.cs
 * CHANGELOG:
 * 8/23/23, Yoar
 *      Complete refactor
 * 3/29/22, Adam
 *      remove all the silliness in trying to center XMLPlantAddons. I've recaptured all addons so that they have the proper offset (and place correctly.)
 *      Add the notion of display addons - addons that cannot be destroyed. (WorldDecoration)
 * 3/28/22, Adam
 *      Add ability to 'pour' liquor on a plant to remove all genetic color (hues)
 * 3/26/22, Adam
 *      1. cleanup comments
 *      2. replace checks for ItemID == -1 to check if the plant is a tree with calls to the new function IsXmlTree()
 *      3. replace the old  iTree.MoveToWorld(this.Location, this.Map) with (iTree as XmlPlantAddon).Center.MoveToWorld(this.Location, this.Map)
 *          The XMLPlantAddon now records the 'center' item and makes that available to the caller. By moving THAT item to world, any offsets in the Addon itself are ignored and the addon is perfectly centered.
 *      4. remove CoconutPalm2
 * 3/23/22, Adam
 *      Add XMLPlantAddon
 *      This is how we 'grow' trees. Once the 'plant' reaches maturity, the plant is hidden, and an addon is created
 *      This addon carries with it the plantItem and fields double clicks (etc.) to the plantItem
 *      Add trees
 * 3/20/22, Adam (PlantStatus)
 *      Clearly define the graphic for each stage of growth
 * 3/15/22, Adam
 *	    Initial creation
 */

using Server.Engines.Plants;
using Server.Items;
using Server.Network;
using Server.Regions;
using Server.Township;
using System;
using System.Collections;
using System.IO;

namespace Server.Township
{
    [TypeAlias("Server.Township.TownshipPlantItem")]
    public class TownshipPlantItem : StaticPlantItem, ITownshipItem, IChopable
    {
        #region Township Item

        public int HitsMax { get { return 0; } set { } }
        public int Hits { get { return 0; } set { } }

        public DateTime LastRepair { get { return DateTime.MinValue; } set { } }
        public DateTime LastDamage { get { return DateTime.MinValue; } set { } }

        public void OnBuild(Mobile m)
        {
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            TownshipItemHelper.OnLocationChange(this, oldLocation);
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            TownshipItemHelper.OnMapChange(this);
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (HitsMax > 0)
                TownshipItemHelper.Inspect(from, this);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            TownshipItemHelper.AddContextMenuEntries(this, from, list);
        }

        public void OnChop(Mobile from)
        {
            TownshipItemHelper.OnChop(this, from);
        }

        public virtual bool CanDestroy(Mobile m)
        {
            return TownshipItemHelper.IsOwner(this, m);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            TownshipItemHelper.Unregister(this);
        }

        #endregion

        [Constructable]
        public TownshipPlantItem()
            : this(false)
        {
        }

        [Constructable]
        public TownshipPlantItem(bool fertileDirt)
            : base(fertileDirt)
        {
            TownshipItemHelper.Register(this);
        }

        public override bool IsUsableBy(Mobile from)
        {
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(GetWorldLocation(), Map);

            // 9/6/23, Yoar: Allies can now always assist in plant growing
            if (tsr != null && tsr.TStone != null && tsr.TStone.GetItemOwner(this) != null && tsr.TStone.HasAccess(from, TownshipAccess.Ally))
                return true;

            return base.IsUsableBy(from);
        }

        public override void RebuildAddon(string addonID)
        {
            base.RebuildAddon(addonID);

            if (Addon != null)
            {
                Addon.IsTownshipItem = true;

                if (Planter != null)
                    TownshipItemHelper.SetOwnership(Addon, Planter);
            }
        }

        public TownshipPlantItem(Serial serial)
            : base(serial)
        {
            TownshipItemHelper.Register(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();
        }
    }
}

namespace Server.Engines.Plants
{
    [TypeAlias("Server.Township.BaseTSPlantItem")]
    public class StaticPlantItem : PlantItem
    {
        public const int DirtPatch = 0x914;

        private Mobile m_Planter;
        private PlantAddon m_Addon;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Planter
        {
            get { return m_Planter; }
            set { m_Planter = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public PlantAddon Addon
        {
            get { return m_Addon; }
            set { m_Addon = value; }
        }

        [Constructable]
        public StaticPlantItem()
            : this(false)
        {
        }

        [Constructable]
        public StaticPlantItem(bool fertileDirt)
            : base(fertileDirt)
        {
            ItemID = DirtPatch;
            Movable = false;
        }

        public override bool IsUsableBy(Mobile from)
        {
            return (from.AccessLevel >= AccessLevel.GameMaster || PlantStatus == PlantStatus.BowlOfDirt || from == m_Planter);
        }

        public override void OnNotUsable(Mobile from)
        {
            // TODO: More accurate messages based on the plant's status
            LabelTo(from, "This is not your garden!");
        }

        public override void PlantSeed(Mobile from, Seed seed)
        {
            if (PlantStatus >= PlantStatus.FullGrownPlant)
            {
                LabelTo(from, 1061919); // You must use a seed on a bowl of dirt!
            }
            else if (!IsUsableBy(from))
            {
                LabelTo(from, "This is not your garden!");
            }
            else if (PlantStatus != PlantStatus.BowlOfDirt)
            {
                if (PlantStatus >= PlantStatus.Plant)
                    LabelTo(from, "This patch of dirt already has a plant in it!");
                else if (PlantStatus >= PlantStatus.Sapling)
                    LabelTo(from, "This patch of dirt already has a sapling in it!");
                else
                    LabelTo(from, "This patch of dirt already has a seed in it!");
            }
            else if (PlantSystem.Water < 2)
            {
                LabelTo(from, "This dirt patch needs to be softened first.");
            }
            else
            {
                PlantType = seed.PlantType;
                PlantHue = seed.PlantHue;
                ShowType = seed.ShowType;

                seed.Delete();

                PlantStatus = PlantStatus.Seed;

                PlantSystem.Reset(false);

                m_Planter = from;

                LabelTo(from, "You plant the seed in a patch of dirt.");
            }
        }

        public override void Update()
        {
            if (PlantStatus >= PlantStatus.DeadTwigs)
            {
                ItemID = 0x1B9D; // ok
                Hue = PlantHueInfo.GetInfo(PlantHue).Hue;
            }
            else if (PlantStatus >= PlantStatus.FullGrownPlant)
            {
                if (TreeSeed.IsTree(PlantType))
                {
                    ItemID = 0x1; // nodraw

                    if (Parent == null)
                        RebuildAddon();
                }
                else
                {
                    ItemID = PlantTypeInfo.GetInfo(PlantType).ItemID;
                }

                Hue = PlantHueInfo.GetInfo(PlantHue).Hue;
            }
            else if (PlantStatus >= PlantStatus.Plant)
            {
                if (TreeSeed.IsTree(PlantType))
                    ItemID = 0xCE9; // sapling
                else
                    ItemID = Utility.RandomList(0xCB0, 0xCB6); // small plant

                Hue = PlantHueInfo.GetInfo(PlantHue).Hue;
            }
            else
            {
                // keep the patch of dirt graphic
                ItemID = DirtPatch;
                Hue = 0;
            }

            InvalidateProperties();
        }

        public void RebuildAddon()
        {
            RebuildAddon(GetAddonID(TreeSeed.GetTreeType(PlantType)));
        }

        public virtual void RebuildAddon(string addonID)
        {
            if (m_Addon != null)
            {
                m_Addon.PlantItem = null;
                m_Addon.Delete();
            }

            m_Addon = new PlantAddon(addonID, this, m_Planter);

            if (m_Addon != null)
            {
                m_Addon.OnBuild(m_Planter);
                m_Addon.MoveToWorld(Location, Map);
            }
        }

        #region Addon IDs

        private static readonly string[][] m_AddonIDs = new string[][]
            {
                new string[] { "AppleTree", "AppleNoFruitTree" },
                new string[] { "BananaTree", "SmallBananaTree", "YoungBananaTree" },
                new string[] {
                    "BareTree1",    "BareTree2",    "BareTree3",    "BareTree4",    "BareTree5",    "BareTree6",
                    "BareTree7",    "BareTree8",    "BareTree9",    "BareTree10",   "BareTree11",   "BareTree12",
                    "BareTree13",   "BareTree14",   "BareTree15",   "BareTree16",   "BareTree17",   "BareTree18",
                    "BareTree19",   "BareTree20",   "BareTree21",   "BareTree22",   "BareTree23",   "BareTree24",
                    "BareTree25",   "BareTree26",   "BareTree27",   "BareTree28",   "BareTree29",   "BareTree30",
                    "BareTree31",   "BareTree32",   "BareTree33" },
                new string[] { "Tree1", "Tree2", "Tree9" },
                new string[] { "CedarTree", "CedarTree2" },
                new string[] { "CoconutPalm" },
                new string[] { "CypressTree" },
                new string[] { "DatePalm" },
                new string[] { "Tree3", "Tree4", "Tree5", "Tree6", "Tree7", "Tree8" },
                new string[] { "OakTree", "YoungOakTree" },
                new string[] { "OhiiTree" },
                new string[] { "PeachTree", "PeachNoFruitTree" },
                new string[] { "PearFruitTree", "PearTree", "SmallPearFruitTree", "SmallPearTree" },
                new string[] { "Sapling" },
                new string[] { "SpiderTree" },
                new string[] { "WalnutTree", "WalnutTree2", "RedWalnutTree" },
                new string[] { "WillowTree", "RedWillowTree" },
                new string[] { "Yucca" },
            };

        public static string[] GetAddonIDs(TreeType treeType)
        {
            int index = (int)treeType;

            if (index >= 0 && index < m_AddonIDs.Length)
                return m_AddonIDs[index];

            return new string[0];
        }

        public static string GetAddonID(TreeType treeType)
        {
            int index;

            if (treeType == TreeType.BananaTree)
                index = Utility.Random(2);
            else if (treeType == TreeType.BareTree)
                index = Utility.Random(33);
            else if (treeType == TreeType.BirchTree)
                index = Utility.Random(2);
            else if (treeType == TreeType.CedarTree)
                index = Utility.Random(2);
            else if (treeType == TreeType.Madrone)
                index = Utility.Random(6);
            else if (treeType == TreeType.OakTree)
                index = Utility.Random(2);
            else if (treeType == TreeType.PeachTree)
                index = 1;
            else if (treeType == TreeType.PearTree)
                index = 2 * Utility.Random(2);
            else if (treeType == TreeType.WalnutTree)
                index = Utility.Random(2);
            else
                index = 0;

            string[] addonIDs = GetAddonIDs(treeType);

            if (index >= 0 && index < addonIDs.Length)
                return addonIDs[index];

            return null;
        }

        #endregion

        public override void Die()
        {
            new DeadTwigs().MoveToWorld(GetWorldLocation(), Map);

            Delete();
        }

        public override int LabelNumber
        {
            get
            {
                switch (PlantStatus)
                {
                    default:
                    case PlantStatus.Stage1:
                    case PlantStatus.Stage2:
                    case PlantStatus.Stage3:
                        {
                            return 1022321; // dirt patch
                        }
                    case PlantStatus.Plant:
                    case PlantStatus.Stage5:
                    case PlantStatus.Stage6:
                        {
                            if (TreeSeed.IsTree(PlantType))
                                return 1023305; // sapling
                            else
                                return 1023176; // sprouts
                        }
                    case PlantStatus.FullGrownPlant:
                    case PlantStatus.Stage8:
                    case PlantStatus.Stage9:
                        {
                            return base.LabelNumber;
                        }
                    case PlantStatus.DecorativePlant:
                        {
                            return 1061924; // a decorative plant
                        }
                    case PlantStatus.DeadTwigs:
                        {
                            return base.LabelNumber;
                        }
                }
            }
        }

        public override bool ValidGrowthLocation
        {
            get { return (Parent == null); }
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_Addon != null)
                m_Addon.Map = Map;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            if (m_Addon != null)
                m_Addon.Location += (Location - oldLocation);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_Addon != null)
                m_Addon.Delete();
        }

        public StaticPlantItem(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((Item)m_Addon);
            writer.Write((Mobile)m_Planter);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Addon = reader.ReadItem() as PlantAddon;

                        goto case 1;
                    }
                case 1:
                    {
                        m_Planter = reader.ReadMobile();

                        break;
                    }
            }

            if (version < 2 && PlantStatus >= PlantStatus.FullGrownPlant && TreeSeed.IsTree(PlantType))
            {
                ItemID = 0x1;
                Visible = true;
            }
        }
    }

    [TypeAlias("Server.Township.DeadTwigs")]
    public class DeadTwigs : Item
    {
        public override bool Decays { get { return true; } }
        public override TimeSpan DecayTime { get { return TimeSpan.FromHours(24); } }

        public DeadTwigs()
            : base(0x1B9D)
        {
            Movable = false;
        }

        public DeadTwigs(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("You dispose of the dead twigs.");

            this.Delete();
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

    // TODO: Split up in 2 classes: One for townships, one for world decoration?
    [TypeAlias("Server.Township.XmlPlantAddon")]
    public class PlantAddon : BaseAddon, ITownshipItem
    {
        private StaticPlantItem m_PlantItem;
        private Mobile m_Planter;
        private bool m_IsTownshipItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public StaticPlantItem PlantItem
        {
            get { return m_PlantItem; }
            set { m_PlantItem = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Planter
        {
            get { return m_Planter; }
            set { m_Planter = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsTownshipItem
        {
            get { return m_IsTownshipItem; }
            set
            {
                if (m_IsTownshipItem != value)
                {
                    if (m_IsTownshipItem)
                        TownshipItemHelper.Unregister(this);

                    m_IsTownshipItem = value;

                    if (m_IsTownshipItem)
                        TownshipItemHelper.Register(this);
                }
            }
        }

        #region Township Item stuff

        private int m_HitsMax;
        private int m_Hits;
        private DateTime m_LastDamage;
        private DateTime m_LastRepair;

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int HitsMax
        {
            get { return m_HitsMax; }
            set
            {
                if (value < 0)
                    value = 0;

                if (m_HitsMax != value)
                {
                    int perc;

                    if (m_HitsMax > 0)
                        perc = Math.Max(0, Math.Min(100, 100 * m_Hits / m_HitsMax));
                    else
                        perc = 100;

                    m_HitsMax = value;

                    this.Hits = perc * m_HitsMax / 100;
                }
            }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public int Hits
        {
            get { return m_Hits; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > m_HitsMax)
                    value = m_HitsMax;

                if (m_Hits != value)
                {
                    int hitsOld = m_Hits;

                    m_Hits = value;

                    OnHitsChanged(hitsOld);
                }
            }
        }

        protected void SetHits(int hits)
        {
            m_HitsMax = m_Hits = hits;
        }

        public virtual void OnHitsChanged(int hitsOld)
        {
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime LastDamage
        {
            get { return m_LastDamage; }
            set { m_LastDamage = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public TimeSpan LastDamageAgo
        {
            get
            {
                TimeSpan ts = DateTime.UtcNow - m_LastDamage;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set { this.LastDamage = DateTime.UtcNow - value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public DateTime LastRepair
        {
            get { return m_LastRepair; }
            set { m_LastRepair = value; }
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.GameMaster)]
        public TimeSpan LastRepairAgo
        {
            get
            {
                TimeSpan ts = DateTime.UtcNow - m_LastRepair;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set { this.LastRepair = DateTime.UtcNow - value; }
        }

        public virtual void OnBuild(Mobile from)
        {
            // 8/23/23, Yoar: Tree addons are no longer attackable
#if false
            int hits = 100;
#else
            int hits = 0;
#endif

            this.HitsMax = hits;
            this.Hits = hits;
            this.LastDamage = DateTime.UtcNow;
            this.LastRepair = DateTime.UtcNow;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);

            if (m_IsTownshipItem)
                TownshipItemHelper.OnLocationChange(this, oldLocation);
        }

        public override void OnMapChange()
        {
            base.OnMapChange();

            if (m_IsTownshipItem)
                TownshipItemHelper.OnMapChange(this);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (m_IsTownshipItem)
                TownshipItemHelper.AddContextMenuEntries(this, from, list);
        }

        public override void OnChop(Mobile from)
        {
            TownshipItemHelper.OnChop(this, from);
        }

        public virtual bool CanDestroy(Mobile m)
        {
            return (m == m_Planter || (m_IsTownshipItem && TownshipItemHelper.IsOwner(this, m)));
        }

        #endregion

        public static string DataFolder { get { return Path.Combine(Core.DataDirectory, "XmlTrees"); } }

        [Constructable]
        public PlantAddon(string addonID)
            : this(addonID, null, null)
        {
        }

        public PlantAddon(string addonID, StaticPlantItem plantItem, Mobile planter)
            : base()
        {
            m_PlantItem = plantItem;
            m_Planter = planter;

            XmlAddon.Build(this, DataFolder, addonID, typeof(TownshipAddonComponent));
        }

        public override void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
            if (m_IsTownshipItem && m_PlantItem != null && !m_PlantItem.Deleted && m_PlantItem.PlantStatus < PlantStatus.DecorativePlant)
            {
                if (!from.InRange(this.GetWorldLocation(), 2))
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                else if (!m_PlantItem.IsAccessibleTo(from))
                    from.SendLocalizedMessage(502436); // That is not accessible.
                else
                    m_PlantItem.OnDoubleClick(from);

                return;
            }

            base.OnComponentDoubleClick(c, from);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_PlantItem != null && !m_PlantItem.Deleted)
            {
                m_PlantItem.PlantStatus = PlantStatus.BowlOfDirt;
                m_PlantItem.PlantSystem.Reset(false);
                m_PlantItem.PlantSystem.Water = 0;
                m_PlantItem.Planter = null;
            }

            if (m_IsTownshipItem)
                TownshipItemHelper.Unregister(this);
        }

        public PlantAddon(Serial serial)
            : base(serial)
        {
            if (m_IsTownshipItem)
                TownshipItemHelper.Register(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)5); // version

            writer.Write((bool)m_IsTownshipItem);

            writer.Write((Mobile)m_Planter);

            writer.Write((int)m_HitsMax);
            writer.Write((int)m_Hits);
            writer.Write((DateTime)m_LastDamage);
            writer.Write((DateTime)m_LastRepair);

            writer.Write((Item)m_PlantItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 5:
                case 4:
                case 3:
                    {
                        if (version >= 4)
                            m_IsTownshipItem = reader.ReadBool();
                        else
                            m_IsTownshipItem = !reader.ReadBool(); // world decoration

                        goto case 2;
                    }
                case 2:
                    {
                        m_Planter = reader.ReadMobile();

                        goto case 1;
                    }
                case 1:
                    {
                        m_HitsMax = reader.ReadInt();
                        m_Hits = reader.ReadInt();
                        m_LastDamage = reader.ReadDateTime();
                        m_LastRepair = reader.ReadDateTime();

                        goto case 0;
                    }
                case 0:
                    {
                        m_PlantItem = reader.ReadItem() as StaticPlantItem;

                        if (version < 4)
                            reader.ReadString(); // addon ID

                        break;
                    }
            }

            if (version < 4 && m_PlantItem != null)
                m_PlantItem.Addon = this;

            // 8/23/23, Yoar: Tree addons are no longer attackable
            if (version < 5)
                HitsMax = 0;
        }
    }
}