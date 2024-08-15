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

/* Scripts\Engines\Township\Items\Gardening\FarmPlot.cs
 * CHANGELOG:
 *  8/26/23, Yoar
 *	    Initial version
 */

using Server.Items;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public enum CropType : byte
    {
        Cabbage,
        Carrot,
        Cotton,
        Flax,
        Lettuce,
        Onion,
        Pumpkin,
        Turnip,
        Wheat
    }

    public class FarmPlot : Item
    {
        public static TimeSpan GrowthDelay = TimeSpan.FromHours(4.0);

        private static readonly List<FarmPlot> m_Instances = new List<FarmPlot>();

        public static List<FarmPlot> Instances { get { return m_Instances; } }

        private static Timer m_Timer;

        public static void Initialize()
        {
            m_Timer = Timer.DelayCall(TimeSpan.FromMinutes(5.0), TimeSpan.FromMinutes(5.0), GrowAll);
        }

        public static void GrowAll()
        {
            for (int i = m_Instances.Count - 1; i >= 0; i--)
            {
                if (i >= m_Instances.Count)
                    continue; // sanity

                m_Instances[i].CheckGrowth();
            }
        }

        public override string DefaultName
        {
            get
            {
                if (HasSeeds)
                    return "a sown farm plot";
                else
                    return "a farm plot";
            }
        }

        private CropType m_Planted;
        private DateTime m_PlantedDate;
        private Mobile m_Planter;

        private Item m_FarmItem;

        [CommandProperty(AccessLevel.GameMaster)]
        public CropType Planted
        {
            get { return m_Planted; }
            set { m_Planted = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime PlantedDate
        {
            get { return m_PlantedDate; }
            set { m_PlantedDate = value; CheckGrowth(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan GrowsIn
        {
            get
            {
                TimeSpan ts = m_PlantedDate + GrowthDelay - DateTime.UtcNow;

                if (ts < TimeSpan.Zero)
                    ts = TimeSpan.Zero;

                return ts;
            }
            set { PlantedDate = m_PlantedDate - GrowthDelay + value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Planter
        {
            get { return m_Planter; }
            set { m_Planter = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Item FarmItem
        {
            get { return m_FarmItem; }
            set { m_FarmItem = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasSeeds
        {
            get { return (m_FarmItem != null && !m_FarmItem.Deleted && m_FarmItem is InternalItem); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasCrop
        {
            get { return (m_FarmItem != null && !m_FarmItem.Deleted && m_FarmItem is FarmableCrop); }
        }

        [Constructable]
        public FarmPlot()
            : base(0x32C9)
        {
            Movable = false;

            m_Instances.Add(this);
        }

        public void PlantSeeds(Mobile from, CropType type)
        {
            m_Planted = type;
            m_PlantedDate = DateTime.UtcNow;
            m_Planter = from;

            if (m_FarmItem != null)
                m_FarmItem.Delete();

            m_FarmItem = new InternalItem();
            m_FarmItem.MoveToWorld(Location + new Point3D(0, 0, 1), Map);
        }

        public void CheckGrowth()
        {
            if (Parent == null && HasSeeds && DateTime.UtcNow >= m_PlantedDate + GrowthDelay)
            {
                if (m_FarmItem != null)
                    m_FarmItem.Delete();

                m_FarmItem = ConstructCrop(GetItemType(m_Planted));

                if (m_FarmItem != null)
                    m_FarmItem.MoveToWorld(Location + new Point3D(0, 0, 1), Map);

                OnGrown();
            }
        }

        public virtual void OnGrown()
        {
        }

        private static readonly Type[] m_CropTypes = new Type[]
            {
                typeof(FarmableCabbage),
                typeof(FarmableCarrot),
                typeof(FarmableCotton),
                typeof(FarmableFlax),
                typeof(FarmableLettuce),
                typeof(FarmableOnion),
                typeof(FarmablePumpkin),
                typeof(FarmableTurnip),
                typeof(FarmableWheat),
            };

        public static Type GetItemType(CropType cropType)
        {
            int index = (int)cropType;

            if (index >= 0 && index < m_CropTypes.Length)
                return m_CropTypes[index];

            return null;
        }

        private static FarmableCrop ConstructCrop(Type type)
        {
            if (!typeof(FarmableCrop).IsAssignableFrom(type))
                return null;

            FarmableCrop crop;

            try
            {
                crop = (FarmableCrop)Activator.CreateInstance(type);
            }
            catch
            {
                crop = null;
            }

            return crop;
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            if (m_FarmItem != null)
                m_FarmItem.Location += (Location - oldLocation);
        }

        public override void OnMapChange()
        {
            if (m_FarmItem != null)
                m_FarmItem.Map = Map;
        }

        public override void OnAfterDelete()
        {
            if (m_FarmItem != null)
                m_FarmItem.Delete();

            m_Instances.Remove(this);
        }

        public FarmPlot(Serial serial)
            : base(serial)
        {
            m_Instances.Add(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((byte)m_Planted);
            writer.Write((DateTime)m_PlantedDate);
            writer.Write((Mobile)m_Planter);

            writer.Write((Item)m_FarmItem);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Planted = (CropType)reader.ReadByte();
                        m_PlantedDate = reader.ReadDateTime();
                        m_Planter = reader.ReadMobile();

                        m_FarmItem = reader.ReadItem();

                        break;
                    }
            }
        }

        private class InternalItem : Item
        {
            public override string DefaultName { get { return "seeds"; } }

            public InternalItem()
                : base(Utility.RandomBool() ? 0xC37 : 0xC38) // flowers
            {
                Movable = false;
                Hue = 1169; // brownlight
            }

            public InternalItem(Serial serial)
                : base(serial)
            {
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

    [Flipable(
        0x1039, 0x1045, 0x1039,
        0x103A, 0x1046, 0x103A)]
    public class PackOfFarmSeeds : Item, IHasQuantity
    {
        public const int MaxQuantity = 6;

        public override string DefaultName { get { return string.Format("a pack of {0} seeds", m_Type.ToString().ToLower()); } }

        private CropType m_Type;
        private int m_Quantity;

        [CommandProperty(AccessLevel.GameMaster)]
        public CropType Type
        {
            get { return m_Type; }
            set { m_Type = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Quantity
        {
            get { return m_Quantity; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > MaxQuantity)
                    value = MaxQuantity;

                if (m_Quantity != value)
                {
                    m_Quantity = value;

                    if (m_Quantity == 0)
                    {
                        Delete();
                    }
                    else if (m_Quantity == MaxQuantity)
                    {
                        switch (ItemID)
                        {
                            case 0x103A: ItemID = 0x1039; break;
                            case 0x1046: ItemID = 0x1045; break;
                        }
                    }
                    else
                    {
                        switch (ItemID)
                        {
                            case 0x1039: ItemID = 0x103A; break;
                            case 0x1045: ItemID = 0x1046; break;
                        }
                    }
                }
            }
        }

        [Constructable]
        public PackOfFarmSeeds()
            : this((CropType)Utility.RandomMinMax((int)CropType.Cabbage, (int)CropType.Wheat), MaxQuantity)
        {
        }

        [Constructable]
        public PackOfFarmSeeds(CropType type)
            : this(type, MaxQuantity)
        {
        }

        [Constructable]
        public PackOfFarmSeeds(CropType type, int quantity)
            : base(0x1045) // sack of flour
        {
            Hue = 1191; // ash
            m_Type = type;
            Quantity = quantity;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            LabelTo(from, string.Format("{0} square feet", 16 * m_Quantity));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                from.SendMessage("Target an empty farm plot to plant these seeds in.");
                from.BeginTarget(2, false, TargetFlags.None, OnTarget);
            }
        }

        private void OnTarget(Mobile from, object targeted)
        {
            if (Deleted || !IsChildOf(from.Backpack))
                return;

            FarmPlot farm = targeted as FarmPlot;

            if (farm == null || farm.HasCrop)
            {
                from.SendMessage("You may only plant this in an empty farm plot.");
            }
            else if (farm.HasSeeds)
            {
                from.SendMessage("That farm plot has been sown already.");
            }
            else
            {
                from.SendMessage("You sow the farm plot.");

                farm.PlantSeeds(from, m_Type);

                Quantity--;
            }
        }

        public PackOfFarmSeeds(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((byte)m_Type);
            writer.Write((int)m_Quantity);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Type = (CropType)reader.ReadByte();
                        m_Quantity = reader.ReadInt();

                        break;
                    }
            }
        }
    }
}

namespace Server.Township
{
    public class TownshipFarmPlot : FarmPlot, ITownshipItem, IChopable
    {
        #region Township Item

        public int HitsMax { get { return 0; } set { } }
        public int Hits { get { return 0; } set { } }

        public DateTime LastDamage { get { return DateTime.MinValue; } set { } }
        public DateTime LastRepair { get { return DateTime.MinValue; } set { } }

        public virtual void OnBuild(Mobile from)
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
        public TownshipFarmPlot()
            : base()
        {
            TownshipItemHelper.Register(this);
        }

        public override void OnGrown()
        {
            if (Planter != null && FarmItem != null)
                TownshipItemHelper.SetOwnership(FarmItem, Planter);
        }

        public override void OnDelete()
        {
            // clean up any grass tiles that were placed on top of this floor
            TownshipFloor.RemoveGrass(Location, Map, this);

            base.OnDelete();
        }

        public TownshipFarmPlot(Serial serial)
            : base(serial)
        {
            TownshipItemHelper.Register(this);
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