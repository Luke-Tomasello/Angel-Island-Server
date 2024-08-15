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

/* Items/Food/Beverage.cs
 * ChangeLog:
 *  12/7/2023, Adam (Fill_OnTarget)
 *      Allow the filling of beverage containers from BaseBeverageCauldron
 *  9/15/2023, Adam: 
 *      If locked down in a house or township we restrict pouring/drawing water to players that have access to this house or township item
 *  4/16/23, Yoar
 *      You can now fill water containers by targeting static water containers.
 *  4/3/23, Yoar
 *      Added IFill, IPour interfaces
 *  4/16/22, Yoar
 *      Added BaseBeverage.OnQuantityChange virtual method.
 *      The weight of casks now depends on the amount of liquid it holds.
 *  4/14/22, Yoar
 *      Removed OnSingleClick "hack" in BaseBeverage where "jug" would be replaced by "cask".
 *      Cask now overrides DefaultName to display its proper name.
 *      Casks are now refillable.
 *  3/29/22, Adam (Pour)
 *      Don't allow pouring of 'beverages' on XmlPlantAddons that are also WorldDecoration
 *  3/27/22, Adam (Casks)
 *      Add beverage 'casks'. These casks hold quantity 100.
 *      Cask are useful for gardeners that need a lot of water or liquor ;)
 *          Actually, the application of liquor to a plant (or decorative plant) is to bleach out all genetic colors (hue)
 *	3/4/10, Adam
 *		Redesign the Deserialize to fix an exception because we had two Deserializers.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Plants;
using Server.Engines.Quests;
using Server.Engines.Quests.Hag;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Items
{
    public enum BeverageType
    {
        Ale,
        Cider,
        Liquor,
        Milk,
        Wine,
        Water
    }

    public interface IHasQuantity
    {
        int Quantity { get; set; }
    }

    public interface IWaterSource : IHasQuantity
    {
    }

    public interface IFill
    {
        bool Fill(Mobile from, BaseBeverage bev);
    }

    public interface IPour
    {
        bool Pour(Mobile from, BaseBeverage bev);
    }

    // TODO: Flipable attributes

    [TypeAlias("Server.Items.BottleAle", "Server.Items.BottleLiquor", "Server.Items.BottleWine")]
    public class BeverageBottle : BaseBeverage
    {
        public override int BaseLabelNumber { get { return 1042959; } } // a bottle of Ale
        public override int MaxQuantity { get { return 5; } }
        public override bool Fillable { get { return false; } }
        public override int ComputeItemID()
        {
            if (!IsEmpty)
            {
                switch (Content)
                {
                    case BeverageType.Ale: return 0x99F;
                    case BeverageType.Cider: return 0x99F;
                    case BeverageType.Liquor: return 0x99B;
                    case BeverageType.Milk: return 0x99B;
                    case BeverageType.Wine: return 0x9C7;
                    case BeverageType.Water: return 0x99B;
                }
            }

            return 0;
        }

        [Constructable]
        public BeverageBottle(BeverageType type)
            : base(type)
        {
            Weight = 1.0;
        }

        public BeverageBottle(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }

    public class Cask : BaseBeverage
    {
        public override string DefaultName
        {
            get
            {
                if (!IsEmpty)
                    return string.Format("a cask of {0}", Content.ToString());
                else
                    return "a cask";
            }
        }

        public override int MaxQuantity { get { return 100; } }
        public override bool Fillable { get { return true; } }

        public override int ComputeItemID()
        {
            return 0x1940; // keg
        }

        public override void OnQuantityChange(int oldQuantity)
        {
            this.Weight += (this.Quantity - oldQuantity) * 0.8;
        }

        [Constructable]
        public Cask(BeverageType type)
            : base(type)
        {
            this.Weight = 1.0 + this.Quantity * 0.8;
        }

        public Cask(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            if (version < 2)
                this.Weight = 1.0 + this.Quantity * 0.8;
        }
    }

    public class Jug : BaseBeverage
    {
        public override int BaseLabelNumber { get { return 1042965; } } // a jug of Ale
        public override int MaxQuantity { get { return 10; } }
        public override bool Fillable { get { return false; } }

        public override int ComputeItemID()
        {
            if (!IsEmpty)
                return 0x9C8;

            return 0;
        }

        [Constructable]
        public Jug(BeverageType type)
            : base(type)
        {
            Weight = 1.0;
        }

        public Jug(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class CeramicMug : BaseBeverage
    {
        public override int BaseLabelNumber { get { return 1042982; } } // a ceramic mug of Ale
        public override int MaxQuantity { get { return 1; } }

        public override int ComputeItemID()
        {
            if (ItemID >= 0x995 && ItemID <= 0x999)
                return ItemID;
            else if (ItemID == 0x9CA)
                return ItemID;

            return 0x995;
        }

        [Constructable]
        public CeramicMug()
        {
            Weight = 1.0;
        }

        [Constructable]
        public CeramicMug(BeverageType type)
            : base(type)
        {
            Weight = 1.0;
        }

        public CeramicMug(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class PewterMug : BaseBeverage
    {
        public override int BaseLabelNumber { get { return 1042994; } } // a pewter mug with Ale
        public override int MaxQuantity { get { return 1; } }

        public override int ComputeItemID()
        {
            if (ItemID >= 0xFFF && ItemID <= 0x1002)
                return ItemID;

            return 0xFFF;
        }

        [Constructable]
        public PewterMug()
        {
            Weight = 1.0;
        }

        [Constructable]
        public PewterMug(BeverageType type)
            : base(type)
        {
            Weight = 1.0;
        }

        public PewterMug(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class Goblet : BaseBeverage
    {
        public override int BaseLabelNumber { get { return 1043000; } } // a goblet of Ale
        public override int MaxQuantity { get { return 1; } }

        public override int ComputeItemID()
        {
            if (ItemID == 0x99A || ItemID == 0x9B3 || ItemID == 0x9BF || ItemID == 0x9CB)
                return ItemID;

            return 0x99A;
        }

        [Constructable]
        public Goblet()
        {
            Weight = 1.0;
        }

        [Constructable]
        public Goblet(BeverageType type)
            : base(type)
        {
            Weight = 1.0;
        }

        public Goblet(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    [TypeAlias("Server.Items.MugAle", "Server.Items.GlassCider", "Server.Items.GlassLiquor",
         "Server.Items.GlassMilk", "Server.Items.GlassWine", "Server.Items.GlassWater")]
    public class GlassMug : BaseBeverage
    {
        public override int EmptyLabelNumber { get { return 1022456; } } // mug
        public override int BaseLabelNumber { get { return 1042976; } } // a mug of Ale
        public override int MaxQuantity { get { return 5; } }

        public override int ComputeItemID()
        {
            if (IsEmpty)
                return (ItemID >= 0x1F81 && ItemID <= 0x1F84 ? ItemID : 0x1F81);

            switch (Content)
            {
                case BeverageType.Ale: return (ItemID == 0x9EF ? 0x9EF : 0x9EE);
                case BeverageType.Cider: return (ItemID >= 0x1F7D && ItemID <= 0x1F80 ? ItemID : 0x1F7D);
                case BeverageType.Liquor: return (ItemID >= 0x1F85 && ItemID <= 0x1F88 ? ItemID : 0x1F85);
                case BeverageType.Milk: return (ItemID >= 0x1F89 && ItemID <= 0x1F8C ? ItemID : 0x1F89);
                case BeverageType.Wine: return (ItemID >= 0x1F8D && ItemID <= 0x1F90 ? ItemID : 0x1F8D);
                case BeverageType.Water: return (ItemID >= 0x1F91 && ItemID <= 0x1F94 ? ItemID : 0x1F91);
            }

            return 0;
        }

        [Constructable]
        public GlassMug()
        {
            Weight = 1.0;
        }

        [Constructable]
        public GlassMug(BeverageType type)
            : base(type)
        {
            Weight = 1.0;
        }

        public GlassMug(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }

    [TypeAlias("Server.Items.PitcherAle", "Server.Items.PitcherCider", "Server.Items.PitcherLiquor",
         "Server.Items.PitcherMilk", "Server.Items.PitcherWine", "Server.Items.PitcherWater",
         "Server.Items.GlassPitcher")]
    public class Pitcher : BaseBeverage
    {
        public override int BaseLabelNumber { get { return 1048128; } } // a Pitcher of Ale
        public override int MaxQuantity { get { return 5; } }

        public override int ComputeItemID()
        {
            if (IsEmpty)
            {
                if (ItemID == 0x9A7 || ItemID == 0xFF7)
                    return ItemID;

                return 0xFF6;
            }

            switch (Content)
            {
                case BeverageType.Ale:
                    {
                        if (ItemID == 0x1F96)
                            return ItemID;

                        return 0x1F95;
                    }
                case BeverageType.Cider:
                    {
                        if (ItemID == 0x1F98)
                            return ItemID;

                        return 0x1F97;
                    }
                case BeverageType.Liquor:
                    {
                        if (ItemID == 0x1F9A)
                            return ItemID;

                        return 0x1F99;
                    }
                case BeverageType.Milk:
                    {
                        if (ItemID == 0x9AD)
                            return ItemID;

                        return 0x9F0;
                    }
                case BeverageType.Wine:
                    {
                        if (ItemID == 0x1F9C)
                            return ItemID;

                        return 0x1F9B;
                    }
                case BeverageType.Water:
                    {
                        if (ItemID == 0xFF8 || ItemID == 0xFF9 || ItemID == 0x1F9E)
                            return ItemID;

                        return 0x1F9D;
                    }
            }

            return 0;
        }

        [Constructable]
        public Pitcher()
        {
            Weight = 2.0;
        }

        [Constructable]
        public Pitcher(BeverageType type)
            : base(type)
        {
            Weight = 2.0;
        }

        public Pitcher(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    [FlipableAttribute]
    public class SkullMug : BaseBeverage
    {
        public override int BaseLabelNumber { get { return 1042988; } } // a skull mug of Ale
        public override int MaxQuantity { get { return 1; } }

        public override int ComputeItemID()
        {
            return ((ItemID >= 4091 && ItemID <= 4094) ? ItemID : 4091);
        }

        public void Flip()
        {
            switch (ItemID)
            {
                case 4091: ItemID = 4092; break;
                case 4092: ItemID = 4091; break;
                case 4093: ItemID = 4094; break;
                case 4094: ItemID = 4093; break;
            }
        }

        [Constructable]
        public SkullMug()
            : base()
        {
        }

        [Constructable]
        public SkullMug(BeverageType type)
            : base(type)
        {
        }

        public SkullMug(Serial serial)
            : base(serial)
        {
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

    public abstract class BaseBeverage : Item, IHasQuantity
    {
        private BeverageType m_Content;
        private int m_Quantity;
        private Mobile m_Poisoner;
        private Poison m_Poison;

        public override int LabelNumber
        {
            get
            {
                int num = BaseLabelNumber;

                if (IsEmpty || num == 0)
                    return EmptyLabelNumber;

                return BaseLabelNumber + (int)m_Content;
            }
        }

        public virtual bool ShowQuantity { get { return (MaxQuantity > 1); } }
        public virtual bool Fillable { get { return true; } }
        public virtual bool Pourable { get { return true; } }

        public virtual int EmptyLabelNumber { get { return base.LabelNumber; } }
        public virtual int BaseLabelNumber { get { return 0; } }

        public abstract int MaxQuantity { get; }
        public abstract int ComputeItemID();

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsEmpty
        {
            get { return (m_Quantity <= 0); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool ContainsAlchohol
        {
            get { return (!IsEmpty && m_Content != BeverageType.Milk && m_Content != BeverageType.Water); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsFull
        {
            get { return (m_Quantity >= MaxQuantity); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Poison Poison
        {
            get { return m_Poison; }
            set { m_Poison = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Poisoner
        {
            get { return m_Poisoner; }
            set { m_Poisoner = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BeverageType Content
        {
            get { return m_Content; }
            set
            {
                m_Content = value;

                InvalidateProperties();

                int itemID = ComputeItemID();

                if (itemID > 0)
                    ItemID = itemID;
                else
                    Delete();
            }
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

                int oldQuantity = m_Quantity;

                m_Quantity = value;

                InvalidateProperties();

                int itemID = ComputeItemID();

                if (itemID > 0)
                {
                    ItemID = itemID;

                    if (m_Quantity != oldQuantity)
                        OnQuantityChange(oldQuantity);
                }
                else
                {
                    Delete();
                }
            }
        }

        public virtual void OnQuantityChange(int oldQuantity)
        {
        }

        public virtual int GetQuantityDescription()
        {
            int perc = (m_Quantity * 100) / MaxQuantity;

            if (perc <= 0)
                return 1042975; // It's empty.
            else if (perc <= 33)
                return 1042974; // It's nearly empty.
            else if (perc <= 66)
                return 1042973; // It's half full.
            else
                return 1042972; // It's full.
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (ShowQuantity)
                list.Add(GetQuantityDescription());
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (ShowQuantity)
                LabelTo(from, GetQuantityDescription());
        }

        public virtual bool ValidateUse(Mobile from, bool message)
        {
            if (Deleted)
                return false;

            if (!Movable && !Fillable)
            {
                Multis.BaseHouse house = Multis.BaseHouse.FindHouseAt(this);

                if (house == null || !house.IsLockedDown(this))
                {
                    if (message)
                        from.SendLocalizedMessage(502946, "", 0x59); // That belongs to someone else.

                    return false;
                }
            }

            if (from.Map != Map || !from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
            {
                if (message)
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.

                return false;
            }

            return true;
        }

        public virtual void Fill_OnTarget(Mobile from, object targ)
        {
            if (!IsEmpty || !Fillable || !ValidateUse(from, false))
                return;

            if (targ is IFill && ((IFill)targ).Fill(from, this))
                return;

            if (targ is BaseBeverageCauldron || targ is AddonComponent && (targ as AddonComponent).Addon is BaseBeverageCauldron)
            {
                BaseBeverageCauldron bev = null;
                if (targ is BaseBeverageCauldron)
                    bev = (BaseBeverageCauldron)targ;
                else
                    bev = (targ as AddonComponent).Addon as BaseBeverageCauldron;

                if (bev.IsEmpty || !bev.ValidateUse(from, true))
                    return;

                this.Content = bev.Content;
                this.Poison = null;// bev.Poison;
                this.Poisoner = null;//bev.Poisoner;

                if (bev.Quantity > this.MaxQuantity)
                {
                    this.Quantity = this.MaxQuantity;
                    bev.Quantity -= this.MaxQuantity;
                }
                else
                {
                    this.Quantity += bev.Quantity;
                    bev.Quantity = 0;
                }

                from.SendMessage("You fill the container with the contents of the cauldron.");
            }
            else if (targ is BaseBeverage)
            {
                BaseBeverage bev = (BaseBeverage)targ;

                if (bev.IsEmpty || !bev.ValidateUse(from, true))
                    return;

                this.Content = bev.Content;
                this.Poison = bev.Poison;
                this.Poisoner = bev.Poisoner;

                if (bev.Quantity > this.MaxQuantity)
                {
                    this.Quantity = this.MaxQuantity;
                    bev.Quantity -= this.MaxQuantity;
                }
                else
                {
                    this.Quantity += bev.Quantity;
                    bev.Quantity = 0;
                }
            }
            else if (targ is BaseWaterContainer)
            {
                BaseWaterContainer bwc = targ as BaseWaterContainer;
                if (Utility.IsTownshipOrHouse(bwc, from) && !bwc.HasAccess(from))
                {
                    from.SendLocalizedMessage(500364); // You can't use that, it belongs to someone else.
                }
                else if (Quantity == 0 || (Content == BeverageType.Water && !IsFull))
                {
                    int iNeed = Math.Min((MaxQuantity - Quantity), bwc.Quantity);

                    if (iNeed > 0 && !bwc.IsEmpty && !IsFull)
                    {
                        bwc.Quantity -= iNeed;

                        this.Content = BeverageType.Water;
                        this.Quantity += iNeed;

                        from.PlaySound(0x4E);
                    }
                }
            }
            else if (targ is Static)
            {
                Static stat = (Static)targ;

                if (Contains(m_WaterStatics, stat.ItemID))
                {
                    this.Content = BeverageType.Water;
                    this.Quantity = this.MaxQuantity;

                    from.SendLocalizedMessage(1010089); // You fill the container with water.
                }
            }
            else if (targ is Item)
            {
                Item item = (Item)targ;
                IWaterSource src;

                src = (item as IWaterSource);

                if (src == null && item is AddonComponent)
                    src = (((AddonComponent)item).Addon as IWaterSource);

                if (src == null || src.Quantity <= 0)
                    return;

                if (from.Map != item.Map || !from.InRange(item.GetWorldLocation(), 2) || !from.InLOS(item))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    return;
                }

                this.Content = BeverageType.Water;
                this.Poison = null;
                this.Poisoner = null;

                if (src.Quantity > this.MaxQuantity)
                {
                    this.Quantity = this.MaxQuantity;
                    src.Quantity -= this.MaxQuantity;
                }
                else
                {
                    this.Quantity += src.Quantity;
                    src.Quantity = 0;
                }

                from.SendLocalizedMessage(1010089); // You fill the container with water.
            }
            else if (targ is Cow) // TODO: Appropriate core check?
            {
                Cow cow = (Cow)targ;

                if (cow.TryMilk(from))
                {
                    this.Content = BeverageType.Milk;
                    this.Quantity = MaxQuantity;

#if false
                    from.SendLocalizedMessage(1080197); // You fill the container with milk.
#else
                    from.SendMessage("You fill the container with milk.");
#endif
                }
            }
            else if (targ is StaticTarget)
            {
                int itemID = ((StaticTarget)targ).ItemID;

                if (Contains(m_WaterStatics, itemID))
                {
                    this.Content = BeverageType.Water;
                    this.Quantity = this.MaxQuantity;

                    from.SendLocalizedMessage(1010089); // You fill the container with water.
                }
            }
            else if (targ is LandTarget)
            {
                int tileID = ((LandTarget)targ).TileID;

                PlayerMobile player = from as PlayerMobile;

                if (player != null)
                {
                    QuestSystem qs = player.Quest;

                    if (qs is WitchApprenticeQuest)
                    {
                        FindIngredientObjective obj = qs.FindObjective(typeof(FindIngredientObjective)) as FindIngredientObjective;

                        if (obj != null && !obj.Completed && obj.Ingredient == Ingredient.SwampWater)
                        {
                            bool contains = false;

                            for (int i = 0; !contains && i < m_SwampTiles.Length; i += 2)
                                contains = (tileID >= m_SwampTiles[i] && tileID <= m_SwampTiles[i + 1]);

                            if (contains)
                            {
                                Delete();

                                player.SendLocalizedMessage(1055035); // You dip the container into the disgusting swamp water, collecting enough for the Hag's vile stew.
                                obj.Complete();
                            }
                        }
                    }
                }
            }
        }

        private static int[] m_WaterStatics = new int[]
            {
                0x0B41, 0x0B44,
                0x0E7B, 0x0E7B,
                0x154D, 0x154D,
            };

        private static int[] m_SwampTiles = new int[]
            {
                0x9C4, 0x9EB,
                0x3D65, 0x3D65,
                0x3DC0, 0x3DD9,
                0x3DDB, 0x3DDC,
                0x3DDE, 0x3EF0,
                0x3FF6, 0x3FF6,
                0x3FFC, 0x3FFE,
            };

        private static bool Contains(int[] array, int value)
        {
            for (int i = 0; i < array.Length - 1; i += 2)
            {
                if (value >= array[i] && value <= array[i + 1])
                    return true;
            }

            return false;
        }

        #region Effects of achohol
        private static Hashtable m_Table = new Hashtable();

        public static void Initialize()
        {
            EventSink.Login += new LoginEventHandler(EventSink_Login);
        }

        private static void EventSink_Login(LoginEventArgs e)
        {
            CheckHeaveTimer(e.Mobile);
        }

        public static void CheckHeaveTimer(Mobile from)
        {
            if (from.BAC > 0 && from.Map != Map.Internal && !from.Deleted)
            {
                Timer t = (Timer)m_Table[from];

                if (t == null)
                {
                    if (from.BAC > 60)
                        from.BAC = 60;

                    t = new HeaveTimer(from);
                    t.Start();

                    m_Table[from] = t;
                }
            }
            else
            {
                Timer t = (Timer)m_Table[from];

                if (t != null)
                {
                    t.Stop();
                    m_Table.Remove(from);

                    from.SendLocalizedMessage(500850); // You feel sober.
                }
            }
        }

        private class HeaveTimer : Timer
        {
            private Mobile m_Drunk;

            public HeaveTimer(Mobile drunk)
                : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
            {
                m_Drunk = drunk;

                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (m_Drunk.Deleted || m_Drunk.Map == Map.Internal)
                {
                    Stop();
                    m_Table.Remove(m_Drunk);
                }
                else if (m_Drunk.Alive)
                {
                    if (m_Drunk.BAC > 60)
                        m_Drunk.BAC = 60;

                    // chance to get sober
                    if (10 > Utility.Random(100))
                        --m_Drunk.BAC;

                    // lose some stats
                    m_Drunk.Stam -= 1;
                    m_Drunk.Mana -= 1;

                    if (Utility.Random(1, 4) == 1)
                    {
                        if (!m_Drunk.Mounted)
                        {
                            // turn in a random direction
                            m_Drunk.Direction = (Direction)Utility.Random(8);

                            // heave
                            m_Drunk.Animate(32, 5, 1, true, false, 0);
                        }

                        // *hic*
                        m_Drunk.PublicOverheadMessage(Network.MessageType.Regular, 0x3B2, 500849);
                    }

                    if (m_Drunk.BAC <= 0)
                    {
                        Stop();
                        m_Table.Remove(m_Drunk);

                        m_Drunk.SendLocalizedMessage(500850); // You feel sober.
                    }
                }
            }
        }
        #endregion

        public virtual void Pour_OnTarget(Mobile from, object targ)
        {
            if (IsEmpty || !Pourable || !ValidateUse(from, false))
                return;

            if (targ is IPour && ((IPour)targ).Pour(from, this))
                return;

            if (targ is BaseBeverage)
            {
                BaseBeverage bev = (BaseBeverage)targ;

                if (!bev.ValidateUse(from, true))
                    return;

                if (bev.IsFull && bev.Content == this.Content)
                {
                    from.SendLocalizedMessage(500848); // Couldn't pour it there.  It was already full.
                }
                else if (!bev.IsEmpty)
                {
                    from.SendLocalizedMessage(500846); // Can't pour it there.
                }
                else
                {
                    bev.Content = this.Content;
                    bev.Poison = this.Poison;
                    bev.Poisoner = this.Poisoner;

                    if (this.Quantity > bev.MaxQuantity)
                    {
                        bev.Quantity = bev.MaxQuantity;
                        this.Quantity -= bev.MaxQuantity;
                    }
                    else
                    {
                        bev.Quantity += this.Quantity;
                        this.Quantity = 0;
                    }

                    from.PlaySound(0x4E);
                }
            }
            else if (from == targ)
            {
                if (from.Thirst < 20)
                    from.Thirst += 1;

                if (ContainsAlchohol)
                {
                    int bac = 0;

                    switch (this.Content)
                    {
                        case BeverageType.Ale: bac = 1; break;
                        case BeverageType.Wine: bac = 2; break;
                        case BeverageType.Cider: bac = 3; break;
                        case BeverageType.Liquor: bac = 4; break;
                    }

                    from.BAC += bac;

                    if (from.BAC > 60)
                        from.BAC = 60;

                    CheckHeaveTimer(from);

                    Misc.WinterEventSystem.OnDrinkAlcohol(from, bac);
                }

                from.PlaySound(Utility.RandomList(0x30, 0x2D6));

                if (m_Poison != null)
                    from.ApplyPoison(m_Poisoner, m_Poison);

                --Quantity;
            }
            else if (targ is BaseWaterContainer)
            {
                BaseWaterContainer bwc = targ as BaseWaterContainer;

                if (Content != BeverageType.Water)
                {
                    from.SendLocalizedMessage(500842); // Can't pour that in there.
                }
                else if (bwc.Items.Count != 0)
                {
                    from.SendLocalizedMessage(500841); // That has something in it.
                }
                /* 9/4/23, Yoar: On OSI, you can fill these anytime, anywhere.
                 * Let's restrict it to lockdowns only.
                */
                else if (!bwc.IsLockedDown)
                {
                    from.SendMessage("That has to be locked down in order to fill it with water.");
                }
                // 9/15/2023, Adam: We further restrict it to players that have access to this house or township item
                else if (!bwc.HasAccess(from))
                {
                    from.SendLocalizedMessage(500364); // You can't use that, it belongs to someone else.
                }
                else
                {
                    int itNeeds = Math.Min((bwc.MaxQuantity - bwc.Quantity), Quantity);

                    if (itNeeds > 0)
                    {
                        bwc.Quantity += itNeeds;
                        Quantity -= itNeeds;

                        from.PlaySound(0x4E);
                    }
                }
            }
            else if (targ is PlantItem)
            {
                ((PlantItem)targ).Pour(from, this);
            }
            else if (targ is AddonComponent && ((AddonComponent)targ).Addon is PlantAddon)
            {
                PlantAddon pa = (PlantAddon)((AddonComponent)targ).Addon;

                if (pa.PlantItem == null || pa.PlantItem.Deleted)
                    from.SendLocalizedMessage(500846); // Can't pour it there.
                else
                    pa.PlantItem.Pour(from, this);
            }
            else
            {
                from.SendLocalizedMessage(500846); // Can't pour it there.
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsEmpty)
            {
                if (!Fillable || !ValidateUse(from, true))
                    return;

                from.BeginTarget(-1, true, TargetFlags.None, new TargetCallback(Fill_OnTarget));
                SendLocalizedMessageTo(from, 500837); // Fill from what?
            }
            else if (Pourable && ValidateUse(from, true))
            {
                from.BeginTarget(-1, true, TargetFlags.None, new TargetCallback(Pour_OnTarget));
                from.SendLocalizedMessage(1010086); // What do you want to use this on?
            }
        }

        public static bool ConsumeTotal(Container pack, BeverageType content, int quantity)
        {
            return ConsumeTotal(pack, typeof(BaseBeverage), content, quantity);
        }

        public static bool ConsumeTotal(Container pack, Type itemType, BeverageType content, int quantity)
        {
            Item[] items = pack.FindItemsByType(itemType);

            // First pass, compute total
            int total = 0;

            for (int i = 0; i < items.Length; ++i)
            {
                BaseBeverage bev = items[i] as BaseBeverage;

                if (bev != null && bev.Content == content && !bev.IsEmpty)
                    total += bev.Quantity;
            }

            if (total >= quantity)
            {
                // We've enough, so consume it

                int need = quantity;

                for (int i = 0; i < items.Length; ++i)
                {
                    BaseBeverage bev = items[i] as BaseBeverage;

                    if (bev == null || bev.Content != content || bev.IsEmpty)
                        continue;

                    int theirQuantity = bev.Quantity;

                    if (theirQuantity < need)
                    {
                        bev.Quantity = 0;
                        need -= theirQuantity;
                    }
                    else
                    {
                        bev.Quantity -= need;
                        return true;
                    }
                }
            }

            return false;
        }

        public BaseBeverage()
        {
            ItemID = ComputeItemID();
        }

        public BaseBeverage(BeverageType type)
        {
            m_Content = type;
            m_Quantity = MaxQuantity;
            ItemID = ComputeItemID();
        }

        public BaseBeverage(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Poisoner);

            Poison.Serialize(m_Poison, writer);
            writer.Write((int)m_Content);
            writer.Write((int)m_Quantity);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Poisoner = reader.ReadMobile();
                        goto case 0;
                    }
                case 0:
                    {
                        m_Poison = Poison.Deserialize(reader);
                        m_Content = (BeverageType)reader.ReadInt();
                        m_Quantity = reader.ReadInt();
                        break;
                    }
            }
        }

    }
}