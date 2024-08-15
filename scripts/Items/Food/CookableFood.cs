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

/* Items/Food/CookableFood.cs
 * ChangeLog:
 *  4/30/23, Yoar
 *      Reduced the cooking requirement for old-style cooking of meats/fish from 10 to 0.
 *      This ensures we can gain cooking even if we have 0 cooking skill points.
 *  4/20/23, Yoar
 *      Can now cook stacks of food. On failure, lose up to 50% of stack.
 *  4/4/23, Yoar
 *      Added ICookableFood interface.
 *      Added CookableFood.BeginCook that works for all items that implement ICookableFood.
 *      Refactored InternalTarget, InternalTimer.
 *      Added 'OldCooking' switch to enable old style double-click + targeted cooking.
 *  4/2/22, Yoar
 *      IsHeatSource now uses the HeatSources array from CraftItem
 *  2/14/22, Yoar
 *      Raw fish steaks are now a deedable commodity.
 *  11/25/21, Yoar
 *      Added CookableFoodItem: Fully-customizable cookable food.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Craft;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.Items
{
    public interface ICookableFood
    {
        int CookingLevel { get; }
        int MaxCookingLevel { get; }

        Food Cook();
    }

    public abstract class CookableFood : Item, ICookableFood
    {
        public static bool OldCooking { get { return Core.RuleSets.SiegeRules(); } }

        private int m_CookingLevel;

        [CommandProperty(AccessLevel.GameMaster)]
        public int CookingLevel
        {
            get
            {
                return m_CookingLevel;
            }
            set
            {
                m_CookingLevel = value;
            }
        }

        public int MaxCookingLevel { get { return 100; } }

        public CookableFood(int itemID, int cookingLevel)
            : base(itemID)
        {
            m_CookingLevel = cookingLevel;
        }

        public CookableFood(Serial serial)
            : base(serial)
        {
        }

        public abstract Food Cook();

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
                                  // Version 1
            writer.Write((int)m_CookingLevel);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_CookingLevel = reader.ReadInt();

                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!CookableFood.OldCooking)
                return;

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            if (!Movable)
            {
                from.SendLocalizedMessage(500685); // You can't use that, it belongs to someone else.
                return;
            }

            from.SendLocalizedMessage(500222); // What should I cook this on?
            from.Target = new InternalTarget(this);
        }

        public static bool IsHeatSource(object targeted)
        {
            int itemID;

            if (targeted is Item)
                itemID = ((Item)targeted).ItemID;
            else if (targeted is StaticTarget)
                itemID = ((StaticTarget)targeted).ItemID;
            else
                return false;

            int[] itemIDs = CraftItem.HeatSources;

            for (int i = 0; i < itemIDs.Length - 1; i += 2)
            {
                if (itemID >= itemIDs[i] && itemID <= itemIDs[i + 1])
                    return true;
            }

            return false;
        }

        private class InternalTarget : Target
        {
            private Item m_Item;

            public InternalTarget(Item item) : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted || !from.InRange(m_Item.GetWorldLocation(), 2))
                    return;

                if (CookableFood.IsHeatSource(targeted))
                    BeginCook(from, m_Item, targeted);
                else
                    from.SendLocalizedMessage(500690); // You can't cook on that.
            }
        }

        public static void BeginCook(Mobile from, Item item, object targeted)
        {
            if (from.BeginAction(typeof(CookableFood)))
            {
                from.PlaySound(0x225);

                (new InternalTimer(from, targeted as IPoint3D, from.Map, item)).Start();
            }
            else
            {
                from.SendLocalizedMessage(500119); // You must wait to perform another action
            }
        }

        private class InternalTimer : Timer
        {
            private Mobile m_From;
            private IPoint3D m_Point;
            private Map m_Map;
            private Item m_Item;

            public InternalTimer(Mobile from, IPoint3D p, Map map, Item item) : base(TimeSpan.FromSeconds(5.0))
            {
                m_From = from;
                m_Point = p;
                m_Map = map;
                m_Item = item;
            }

            protected override void OnTick()
            {
                m_From.EndAction(typeof(CookableFood));

                if (!(m_Item is ICookableFood))
                    return;

                ICookableFood cookable = (ICookableFood)m_Item;

                Food cookedFood = cookable.Cook();

#if false
				if ( m_From.Backpack == null || !m_From.Backpack.CheckHold( m_From, cookedFood, false ) )
				{
					m_From.SendLocalizedMessage( 500684 ); // You do not have room for the cooked food in your backpack!  You stop cooking.

					cookedFood.Delete();
					return;
				}
#endif

                if (m_From.Map != m_Map || (m_Point != null && m_From.GetDistanceToSqrt(GetWorldLocation(m_Point)) > 3) || !m_From.CheckSkill(SkillName.Cooking, cookable.CookingLevel, cookable.MaxCookingLevel, contextObj: new object[2]))
                {
                    m_From.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.

                    int toKeep = 0;

                    // lose up to 50% of resources based on skill value
                    toKeep += m_Item.Amount / 2;
                    toKeep += m_Item.Amount * Math.Max(0, Math.Min(1000, m_From.Skills[SkillName.Cooking].Fixed)) / 2000;

                    m_Item.Consume(m_Item.Amount - toKeep);

                    cookedFood.Delete();
                    return;
                }

                int cookedAmount = m_Item.Amount;

                if (cookedAmount > 60000)
                    cookedAmount = 60000;

                cookedFood.Amount = cookedAmount;

                if (Utility.RandomBool())
                    m_From.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500687); // Looks delicious.
                else
                    m_From.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500688); // Mmmm, smells good.

                if (m_From.AddToBackpack(cookedFood))
                {
                    m_From.SendLocalizedMessage(500689); // You put the cooked food into your backpack.
                    m_From.PlaySound(0x57);
                }

                m_Item.Consume(cookedAmount);
            }

            private static IPoint3D GetWorldLocation(IPoint3D p)
            {
                return (p is Item ? ((Item)p).GetWorldLocation() : p);
            }
        }
    }

    // ********** RawRibs **********
    public class RawRibs : CookableFood
    {
        [Constructable]
        public RawRibs()
            : this(1)
        {
        }

        [Constructable]
        public RawRibs(int amount)
            : base(0x9F1, 0)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public RawRibs(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new RawRibs(), amount);
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

            if (version < 1 && CookingLevel == 10)
                CookingLevel = 0;
        }

        public override Food Cook()
        {
            return new Ribs();
        }
    }


    // ********** RawLambLeg **********
    public class RawLambLeg : CookableFood
    {
        [Constructable]
        public RawLambLeg()
            : this(1)
        {
        }

        [Constructable]
        public RawLambLeg(int amount)
            : base(0x1609, 0)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public RawLambLeg(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new RawLambLeg(), amount);
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

            if (version < 1 && CookingLevel == 10)
                CookingLevel = 0;
        }

        public override Food Cook()
        {
            return new LambLeg();
        }
    }

    // ********** RawChickenLeg **********
    public class RawChickenLeg : CookableFood
    {
        [Constructable]
        public RawChickenLeg()
            : base(0x1607, 0)
        {
            Weight = 1.0;
            Stackable = true;
        }

        public RawChickenLeg(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new RawChickenLeg(), amount);
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

            if (version < 1 && CookingLevel == 10)
                CookingLevel = 0;
        }

        public override Food Cook()
        {
            return new ChickenLeg();
        }
    }


    // ********** RawBird **********
    public class RawBird : CookableFood
    {
        [Constructable]
        public RawBird()
            : this(1)
        {
        }

        [Constructable]
        public RawBird(int amount)
            : base(0x9B9, 0)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public RawBird(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new RawBird(), amount);
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

            if (version < 1 && CookingLevel == 10)
                CookingLevel = 0;
        }

        public override Food Cook()
        {
            return new CookedBird();
        }
    }


    // ********** UnbakedPeachCobbler **********
    public class UnbakedPeachCobbler : CookableFood
    {
        public override int LabelNumber { get { return 1041335; } } // unbaked peach cobbler

        [Constructable]
        public UnbakedPeachCobbler()
            : base(0x1042, 25)
        {
            Weight = 1.0;
        }

        public UnbakedPeachCobbler(Serial serial)
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
        public override Food Cook()
        {
            return new PeachCobbler();
        }
    }

    // ********** UnbakedFruitPie **********
    public class UnbakedFruitPie : CookableFood
    {
        public override int LabelNumber { get { return 1041334; } } // unbaked fruit pie

        [Constructable]
        public UnbakedFruitPie()
            : base(0x1042, 25)
        {
            Weight = 1.0;
        }

        public UnbakedFruitPie(Serial serial)
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

        public override Food Cook()
        {
            return new FruitPie();
        }
    }

    // ********** UnbakedMeatPie **********
    public class UnbakedMeatPie : CookableFood
    {
        public override int LabelNumber { get { return 1041338; } } // unbaked meat pie

        [Constructable]
        public UnbakedMeatPie()
            : base(0x1042, 25)
        {
            Weight = 1.0;
        }

        public UnbakedMeatPie(Serial serial)
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

        public override Food Cook()
        {
            return new MeatPie();
        }
    }

    // ********** UnbakedPumpkinPie **********
    public class UnbakedPumpkinPie : CookableFood
    {
        public override int LabelNumber { get { return 1041342; } } // unbaked pumpkin pie

        [Constructable]
        public UnbakedPumpkinPie()
            : base(0x1042, 25)
        {
            Weight = 1.0;
        }

        public UnbakedPumpkinPie(Serial serial)
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

        public override Food Cook()
        {
            return new PumpkinPie();
        }
    }

    // ********** UnbakedApplePie **********
    public class UnbakedApplePie : CookableFood
    {
        public override int LabelNumber { get { return 1041336; } } // unbaked apple pie

        [Constructable]
        public UnbakedApplePie()
            : base(0x1042, 25)
        {
            Weight = 1.0;
        }

        public UnbakedApplePie(Serial serial)
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

        public override Food Cook()
        {
            return new ApplePie();
        }
    }

    // ********** UncookedCheesePizza **********
    [TypeAlias("Server.Items.UncookedPizza")]
    public class UncookedCheesePizza : CookableFood
    {
        public override int LabelNumber { get { return 1041341; } } // uncooked cheese pizza

        [Constructable]
        public UncookedCheesePizza()
            : base(0x1083, 20)
        {
            Weight = 1.0;
        }

        public UncookedCheesePizza(Serial serial)
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

            if (ItemID == 0x1040)
                ItemID = 0x1083;

            if (Hue == 51)
                Hue = 0;
        }

        public override Food Cook()
        {
            return new CheesePizza();
        }
    }

    // ********** UncookedSausagePizza **********
    public class UncookedSausagePizza : CookableFood
    {
        public override int LabelNumber { get { return 1041337; } } // uncooked sausage pizza

        [Constructable]
        public UncookedSausagePizza()
            : base(0x1083, 20)
        {
            Weight = 1.0;
        }

        public UncookedSausagePizza(Serial serial)
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

        public override Food Cook()
        {
            return new SausagePizza();
        }
    }

#if false
	// ********** UncookedPizza **********
	public class UncookedPizza : CookableFood
	{
		[Constructable]
		public UncookedPizza() : base( 0x1083, 20 )
		{
			Weight = 1.0;
		}

		public UncookedPizza( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();

			if ( ItemID == 0x1040 )
				ItemID = 0x1083;

			if ( Hue == 51 )
				Hue = 0;
		}

		public override Food Cook()
		{
			return new Pizza();
		}
	}
#endif

    // ********** UnbakedQuiche **********
    public class UnbakedQuiche : CookableFood
    {
        public override int LabelNumber { get { return 1041339; } } // unbaked quiche

        [Constructable]
        public UnbakedQuiche()
            : base(0x1042, 25)
        {
            Weight = 1.0;
        }

        public UnbakedQuiche(Serial serial)
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

        public override Food Cook()
        {
            return new Quiche();
        }
    }

    // ********** Eggs **********
    public class Eggs : CookableFood, Server.Engines.Breeding.IEgg
    {
        [Constructable]
        public Eggs()
            : base(0x9B5, 15)
        {
            Weight = 0.5;
        }

        public Eggs(Serial serial)
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

        public override Food Cook()
        {
            return new FriedEggs();
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            Server.Engines.Breeding.BaseHatchableEgg.InspectEgg(from, this);
        }

        #region IEgg

        bool Server.Engines.Breeding.IEgg.IsFertile { get { return false; } }

        #endregion
    }

    // ********** BrightlyColoredEggs **********
    public class BrightlyColoredEggs : CookableFood
    {
        [Constructable]
        public BrightlyColoredEggs()
            : base(0x9B5, 15)
        {
            Name = "brightly colored eggs";
            Weight = 0.5;
            Hue = 3 + (Utility.Random(20) * 5);
        }

        public BrightlyColoredEggs(Serial serial)
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

        public override Food Cook()
        {
            return new FriedEggs();
        }
    }

    // ********** EasterEggs **********
    public class EasterEggs : CookableFood
    {
        public override int LabelNumber { get { return 1016105; } } // Easter Eggs

        [Constructable]
        public EasterEggs()
            : base(0x9B5, 15)
        {
            Weight = 0.5;
            Hue = 3 + (Utility.Random(20) * 5);
        }

        public EasterEggs(Serial serial)
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

        public override Food Cook()
        {
            return new FriedEggs();
        }
    }

    // ********** CookieMix **********
    public class CookieMix : CookableFood
    {
        [Constructable]
        public CookieMix()
            : base(0x103F, 20)
        {
            Weight = 1.0;
        }

        public CookieMix(Serial serial)
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

        public override Food Cook()
        {
            return new Cookies();
        }
    }

    // ********** CakeMix **********
    public class CakeMix : CookableFood
    {
        public override int LabelNumber { get { return 1041002; } } // cake mix

        [Constructable]
        public CakeMix()
            : base(0x103F, 40)
        {
            Weight = 1.0;
        }

        public CakeMix(Serial serial)
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

        public override Food Cook()
        {
            return new Cake();
        }
    }

    public class RawFishSteak : CookableFood, ICommodity
    {
        string ICommodity.Description
        {
            get { return string.Format(Amount == 1 ? "{0} raw fish steak" : "{0} raw fish steaks", Amount); }
        }

        [Constructable]
        public RawFishSteak()
            : this(1)
        {
        }

        [Constructable]
        public RawFishSteak(int amount)
            : base(0x097A, 0)
        {
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
        }

        public RawFishSteak(Serial serial)
            : base(serial)
        {
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new RawFishSteak(), amount);
        }

        public override Food Cook()
        {
            return new FishSteak();
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

            if (version < 1 && CookingLevel == 10)
                CookingLevel = 0;
        }
    }

    /// <summary>
    /// Yoar: Fully-customizable cookable food.
    /// </summary>
    public class CookableFoodItem : Food
    {
        public override bool HasEatEntry { get { return m_Cooked; } }

        private bool m_Cooked;
        private int m_CookingLevel;
        private int m_CookingTicks;
        private int m_CookedItemID;
        private int m_CookedHue;
        private string m_CookedName;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Cooked
        {
            get { return m_Cooked; }
            set
            {
                if (m_Cooked != value)
                {
                    m_Cooked = value;

                    if (m_Cooked)
                    {
                        if (m_CookedItemID != 0)
                            this.ItemID = m_CookedItemID;

                        if (m_CookedHue != -1)
                            this.Hue = m_CookedHue;

                        if (m_CookedName != null)
                            this.Name = m_CookedName;
                    }
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CookingLevel
        {
            get { return m_CookingLevel; }
            set { m_CookingLevel = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CookingTicks
        {
            get { return m_CookingTicks; }
            set { m_CookingTicks = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CookedItemID
        {
            get { return m_CookedItemID; }
            set { m_CookedItemID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CookedHue
        {
            get { return m_CookedHue; }
            set { m_CookedHue = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string CookedName
        {
            get { return m_CookedName; }
            set { m_CookedName = value; }
        }

        [Constructable]
        public CookableFoodItem()
            : this(1)
        {
        }

        [Constructable]
        public CookableFoodItem(int amount)
            : base(amount, 0x097A)
        {
            Weight = 0.1;
            FillFactor = 3;
            m_CookingLevel = 10;
            m_CookingTicks = 1;
            m_CookedItemID = 0x97B;
            m_CookedHue = -1;
        }

        public override Item Dupe(int amount)
        {
            CookableFoodItem copy = new CookableFoodItem();

            copy.m_Cooked = m_Cooked;
            copy.m_CookingLevel = m_CookingLevel;
            copy.m_CookingTicks = m_CookingTicks;
            copy.m_CookedItemID = m_CookedItemID;
            copy.m_CookedHue = m_CookedHue;
            copy.m_CookedName = m_CookedName;

            return base.Dupe(copy, amount);
        }

        public override bool StackWith(Mobile from, Item dropped, bool playSound)
        {
            CookableFoodItem other = dropped as CookableFoodItem;

            return other != null
                && other.m_Cooked == m_Cooked
                && other.m_CookingLevel == m_CookingLevel
                && other.m_CookingTicks == m_CookingTicks
                && other.m_CookedItemID == m_CookedItemID
                && other.m_CookedHue == m_CookedHue
                && other.m_CookedName == m_CookedName
                && base.StackWith(from, dropped, playSound);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Movable)
                return;

            if (m_Cooked)
            {
                base.OnDoubleClick(from);
                return;
            }

            if (m_CookingTicks > 0)
                from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private CookableFoodItem m_Item;

            public InternalTarget(CookableFoodItem item)
                : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted || !(targeted is IPoint3D) || !CookableFood.IsHeatSource(targeted))
                    return;

                if (!from.BeginAction(typeof(CookableFood))) // intentional: use CookableFood as the lock
                {
                    from.SendLocalizedMessage(500119); // You must wait to perform another action
                    return;
                }

                CookableFoodItem toCook = null;

                if (!m_Item.Stackable || m_Item.Amount == 0)
                {
                    toCook = m_Item;
                }
                else // let's take one item from the stack
                {
                    Item copy = m_Item.Dupe(1);

                    if (copy is CookableFoodItem)
                    {
                        toCook = (CookableFoodItem)copy;

                        m_Item.Consume();
                    }
                    else
                    {
                        copy.Delete();
                    }
                }

                if (toCook != null)
                {
                    toCook.MoveToIntStorage();

                    Point3D loc = new Point3D((IPoint3D)targeted);

                    from.Direction = from.GetDirectionTo(loc);
                    from.PlaySound(0x225);

                    (new InternalTimer(from, loc, from.Map, toCook)).Start();
                }
            }

            private class InternalTimer : Timer
            {
                private Mobile m_From;
                private Point3D m_Loc;
                private Map m_Map;
                private CookableFoodItem m_Item;
                private int m_Ticks;

                public InternalTimer(Mobile from, Point3D loc, Map map, CookableFoodItem item)
                    : base(TimeSpan.FromSeconds(5.0), TimeSpan.FromSeconds(5.0))
                {
                    m_From = from;
                    m_Loc = loc;
                    m_Map = map;
                    m_Item = item;
                }

                protected override void OnTick()
                {
                    bool fail = (m_From.Map != m_Map || m_From.GetDistanceToSqrt(m_Loc) > 3);

                    if (!fail)
                    {
                        if (++m_Ticks < m_Item.CookingTicks)
                        {
                            m_From.PlaySound(0x225); // still cooking...
                        }
                        else if (m_From.CheckSkill(SkillName.Cooking, m_Item.CookingLevel, 100.0, contextObj: new object[2]))
                        {
                            m_From.EndAction(typeof(CookableFood));
                            m_Item.Cooked = true;

                            if (m_Item.RetrieveItemFromIntStorage(m_From))
                                m_From.PlaySound(0x57);
                            else
                                m_Item.Delete();

                            Stop();
                            return;
                        }
                        else
                        {
                            fail = true; // we failed the skill check
                        }
                    }

                    if (fail)
                    {
                        m_From.EndAction(typeof(CookableFood));
                        m_From.SendLocalizedMessage(500686); // You burn the food to a crisp! It's ruined.
                        m_Item.Delete();

                        Stop();
                    }
                }
            }
        }

        public CookableFoodItem(Serial serial)
            : base(serial)
        {
        }

        [Flags]
        public enum SaveFlag : ushort
        {
            None = 0x0,

            Cooked = 0x01,
            CookingLevel = 0x02,
            CookingTicks = 0x04,
            CookedItemID = 0x08,
            CookedHue = 0x10,
            CookedName = 0x20,
        }

        private void SetSaveFlag(ref SaveFlag flags, SaveFlag flag, bool condition)
        {
            if (condition)
                flags |= flag;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            SaveFlag flags = SaveFlag.None;

            SetSaveFlag(ref flags, SaveFlag.Cooked, m_Cooked);
            SetSaveFlag(ref flags, SaveFlag.CookingLevel, m_CookingLevel != 10);
            SetSaveFlag(ref flags, SaveFlag.CookingTicks, m_CookingTicks != 1);
            SetSaveFlag(ref flags, SaveFlag.CookedItemID, m_CookedItemID != 0);
            SetSaveFlag(ref flags, SaveFlag.CookedHue, m_CookedHue != -1);
            SetSaveFlag(ref flags, SaveFlag.CookedName, m_CookedName != null);

            writer.Write((ushort)flags);

            if (flags.HasFlag(SaveFlag.CookingLevel))
                writer.Write((int)m_CookingLevel);

            if (flags.HasFlag(SaveFlag.CookingTicks))
                writer.Write((int)m_CookingTicks);

            if (flags.HasFlag(SaveFlag.CookedItemID))
                writer.Write((int)m_CookedItemID);

            if (flags.HasFlag(SaveFlag.CookedHue))
                writer.Write((int)m_CookedHue);

            if (flags.HasFlag(SaveFlag.CookedName))
                writer.Write((string)m_CookedName);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            SaveFlag flags = (SaveFlag)reader.ReadUShort();

            if (flags.HasFlag(SaveFlag.Cooked))
                m_Cooked = true;

            if (flags.HasFlag(SaveFlag.CookingLevel))
                m_CookingLevel = reader.ReadInt();
            else
                m_CookingLevel = 10;

            if (flags.HasFlag(SaveFlag.CookingTicks))
                m_CookingTicks = reader.ReadInt();
            else
                m_CookingTicks = 1;

            if (flags.HasFlag(SaveFlag.CookedItemID))
                m_CookedItemID = reader.ReadInt();

            if (flags.HasFlag(SaveFlag.CookedHue))
                m_CookedHue = reader.ReadInt();
            else
                m_CookedHue = -1;

            if (flags.HasFlag(SaveFlag.CookedName))
                m_CookedName = reader.ReadString();
        }
    }
}