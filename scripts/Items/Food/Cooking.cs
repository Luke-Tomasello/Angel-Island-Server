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

/* Items/Food/Cooking.cs
 * ChangeLog:
 *  7/29/23, Yoar
 *      Added WheatSheaf
 *  4/4/23, Yoar
 *      Implemented old style double-click + targeted cooking.
 */

using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    public class UtilityItem
    {
        static public int RandomChoice(int itemID1, int itemID2)
        {
            int iRet = 0;
            switch (Utility.Random(2))
            {
                default:
                case 0: iRet = itemID1; break;
                case 1: iRet = itemID2; break;
            }
            return iRet;
        }
    }

    // ********** Dough **********
    public class Dough : Item, ICookableFood
    {
        [Constructable]
        public Dough()
            : base(0x103d)
        {
            Weight = 1.0;
        }

        public Dough(Serial serial)
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

            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private Dough m_Item;

            public InternalTarget(Dough item)
                : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted || !from.InRange(m_Item.GetWorldLocation(), 2))
                    return;

                if (targeted is Eggs)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new UnbakedQuiche());
                    from.AddToBackpack(new Eggshells());
                }
                else if (targeted is CheeseWheel)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new UncookedCheesePizza());
                }
                else if (targeted is Sausage)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new UncookedSausagePizza());
                }
                else if (targeted is Apple)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new UnbakedApplePie());
                }
                else if (targeted is Peach)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new UnbakedPeachCobbler());
                }
                else if (targeted is RawChickenLeg || targeted is RawLambLeg || targeted is RawRibs)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new UnbakedMeatPie());
                }
                else if (targeted is Pear)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new UnbakedFruitPie());
                }
                else if (targeted is Pumpkin)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new UnbakedPumpkinPie());
                }
                else if (CookableFood.IsHeatSource(targeted))
                {
                    CookableFood.BeginCook(from, m_Item, targeted);
                }
            }
        }

        #region ICookableFood

        int ICookableFood.CookingLevel { get { return 0; } }
        int ICookableFood.MaxCookingLevel { get { return 10; } }

        Food ICookableFood.Cook()
        {
            return new BreadLoaf();
        }

        #endregion
    }

    // ********** SweetDough **********
    public class SweetDough : Item, ICookableFood
    {
        public override int LabelNumber { get { return 1041340; } } // sweet dough

        [Constructable]
        public SweetDough()
            : base(0x103d)
        {
            Weight = 1.0;
            Hue = 150;
        }

        public SweetDough(Serial serial)
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

            if (Hue == 51)
                Hue = 150;
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

            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private SweetDough m_Item;

            public InternalTarget(SweetDough item)
                : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted || !from.InRange(m_Item.GetWorldLocation(), 2))
                    return;

                if (targeted is BowlFlour)
                {
                    m_Item.Consume();

                    ((Item)targeted).Delete();

                    from.AddToBackpack(new CakeMix());
                }
                else if (CookableFood.IsHeatSource(targeted))
                {
                    CookableFood.BeginCook(from, m_Item, targeted);
                }
            }
        }

        #region ICookableFood

        int ICookableFood.CookingLevel { get { return 0; } }
        int ICookableFood.MaxCookingLevel { get { return 10; } }

        Food ICookableFood.Cook()
        {
            return new Muffins();
        }

        #endregion
    }

    // ********** JarHoney **********
    public class JarHoney : Item
    {
        [Constructable]
        public JarHoney()
            : base(0x9ec)
        {
            Weight = 1.0;
            Stackable = true;
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new JarHoney(), amount);
        }

        public JarHoney(Serial serial)
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
            Stackable = true;
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

            from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private JarHoney m_Item;

            public InternalTarget(JarHoney item)
                : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted || !from.InRange(m_Item.GetWorldLocation(), 2))
                    return;

                if (targeted is Dough)
                {
                    m_Item.Consume();

                    ((Item)targeted).Consume();

                    from.AddToBackpack(new SweetDough());
                }
                else if (targeted is BowlFlour)
                {
                    m_Item.Consume();

                    ((Item)targeted).Delete();

                    from.AddToBackpack(new CookieMix());
                }
            }
        }
    }

    // ********** BowlFlour **********
    public class BowlFlour : Item, IPour
    {
        [Constructable]
        public BowlFlour()
            : base(0xa1e)
        {
            Weight = 1.0;
        }

        public BowlFlour(Serial serial)
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

        public bool Pour(Mobile from, BaseBeverage bev)
        {
            if (bev.Content == BeverageType.Water && bev.Quantity > 0)
            {
                Delete();
                bev.Quantity--;

                from.AddToBackpack(new EmptyWoodenBowl());
                from.AddToBackpack(new Dough());

                from.SendLocalizedMessage(500844); // You make some dough and put it in your backpack

                return true;
            }

            return false;
        }
    }

#if false
    // ********** WoodenBowl **********
    public class WoodenBowl : Item
    {
        [Constructable]
        public WoodenBowl()
            : base(0x15f8)
        {
            Weight = 1.0;
        }

        public WoodenBowl(Serial serial)
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
#endif

    // ********** PitcherWater **********
    /*public class PitcherWater : Item
	{
		[Constructable]
		public PitcherWater() : base(Utility.Random( 0x1f9d, 2 ))
		{
			Weight = 1.0;
		}

		public PitcherWater( Serial serial ) : base( serial )
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
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !Movable )
				return;

			from.Target = new InternalTarget( this );
		}

		private class InternalTarget : Target
		{
			private PitcherWater m_Item;

			public InternalTarget( PitcherWater item ) : base( 1, false, TargetFlags.None )
			{
				m_Item = item;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( m_Item.Deleted ) return;

				if ( targeted is BowlFlour )
				{
					m_Item.Delete();
					((BowlFlour)targeted).Delete();

					from.AddToBackpack( new Dough() );
					from.AddToBackpack( new WoodenBowl() );
				}
			}
		}
	}*/

    // ********** SackFlour **********
    [TypeAlias("Server.Items.SackFlourOpen")]
    public class SackFlour : Item, IHasQuantity, IPour
    {
        private int m_Quantity;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Quantity
        {
            get { return m_Quantity; }
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > 20)
                    value = 20;

                m_Quantity = value;

                if (m_Quantity == 0)
                    Delete();
                else if (m_Quantity < 20 && (ItemID == 0x1039 || ItemID == 0x1045))
                    ++ItemID;
            }
        }

        [Constructable]
        public SackFlour()
            : base(0x1039)
        {
            Weight = 1.0;
            m_Quantity = 20;
        }

        public SackFlour(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

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
                        m_Quantity = reader.ReadInt();
                        break;
                    }
                case 0:
                    {
                        m_Quantity = 20;
                        break;
                    }
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
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

            if ((ItemID == 0x1039 || ItemID == 0x1045))
            {
                ++ItemID;
                return;
            }

            if (CookableFood.OldCooking)
                from.Target = new InternalTarget(this);
        }

        private class InternalTarget : Target
        {
            private SackFlour m_Item;

            public InternalTarget(SackFlour item)
                : base(1, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Item.Deleted || !from.InRange(m_Item.GetWorldLocation(), 2))
                    return;

                if (targeted is EmptyWoodenBowl)
                {
                    m_Item.Quantity--;

                    ((Item)targeted).Delete();

                    from.AddToBackpack(new BowlFlour());
                }
                else if (targeted is TribalBerry)
                {
                    if (from.Skills[SkillName.Cooking].Base >= 80.0)
                    {
                        m_Item.Quantity--;

                        ((Item)targeted).Consume();

                        from.AddToBackpack(new TribalPaint());

                        from.SendLocalizedMessage(1042002); // You combine the berry and the flour into the tribal paint worn by the savages.
                    }
                    else
                    {
                        from.SendLocalizedMessage(1042003); // You don't have the cooking skill to create the body paint.
                    }
                }
            }
        }

        public bool Pour(Mobile from, BaseBeverage bev)
        {
            if (bev.Content == BeverageType.Water && bev.Quantity > 0)
            {
                this.Quantity--;
                bev.Quantity--;

                from.AddToBackpack(new Dough());

                from.SendLocalizedMessage(500844); // You make some dough and put it in your backpack

                return true;
            }

            return false;
        }
    }

#if false
	// ********** SackFlourOpen **********
	public class SackFlourOpen : Item
	{
		public override int LabelNumber{ get{ return 1024166; } } // open sack of flour

		[Constructable]
		public SackFlourOpen() : base(UtilityItem.RandomChoice( 0x1046, 0x103a ))
		{
			Weight = 1.0;
		}

		public SackFlourOpen( Serial serial ) : base( serial )
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
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( !Movable )
				return;

			from.Target = new InternalTarget( this );
		}

		private class InternalTarget : Target
		{
			private SackFlourOpen m_Item;

			public InternalTarget( SackFlourOpen item ) : base( 1, false, TargetFlags.None )
			{
				m_Item = item;
			}

			protected override void OnTarget( Mobile from, object targeted )
			{
				if ( m_Item.Deleted ) return;

				if ( targeted is WoodenBowl )
				{
					m_Item.Delete();
					((WoodenBowl)targeted).Delete();

					from.AddToBackpack( new BowlFlour() );
				}
				else if ( targeted is TribalBerry )
				{
					if ( from.Skills[SkillName.Cooking].Base >= 80.0 )
					{
						m_Item.Delete();
						((TribalBerry)targeted).Delete();

						from.AddToBackpack( new TribalPaint() );

						from.SendLocalizedMessage( 1042002 ); // You combine the berry and the flour into the tribal paint worn by the savages.
					}
					else
					{
						from.SendLocalizedMessage( 1042003 ); // You don't have the cooking skill to create the body paint.
					}
				}
			}
		}
	}
#endif

    // ********** Eggshells **********
    public class Eggshells : Item
    {
        [Constructable]
        public Eggshells()
            : base(0x9b4)
        {
            Weight = 0.5;
        }

        public Eggshells(Serial serial)
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

    public class WheatSheaf : Item
    {
        [Constructable]
        public WheatSheaf() : this(1)
        {
        }

        [Constructable]
        public WheatSheaf(int amount) : base(7869)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = amount;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!Movable)
                return;

            from.BeginTarget(4, false, TargetFlags.None, new TargetCallback(OnTarget));
        }

        public virtual void OnTarget(Mobile from, object obj)
        {
            if (obj is AddonComponent)
                obj = (obj as AddonComponent).Addon;

            IFlourMill mill = obj as IFlourMill;

            if (mill != null)
            {
                int needs = mill.MaxFlour - mill.CurFlour;

                if (needs > this.Amount)
                    needs = this.Amount;

                mill.CurFlour += needs;
                Consume(needs);
            }
        }

        public WheatSheaf(Serial serial) : base(serial)
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
}