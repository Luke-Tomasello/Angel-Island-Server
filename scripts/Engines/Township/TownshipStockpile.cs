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

/* Scripts/Engines/Township/TownshipStockpile.cs
 * CHANGELOG:
 *  3/24/22, Yoar
 *      Added TownshipStockpileDeed: Holds shipment of resources. Double click & target TStone to deposit.
 *  3/24/22, Yoar
 *      Now accepting different granite types.
 *      Added support for marble, sandstone.
 *  3/12/22, Adam
 *      Add support for Fertile Dirt as a stockpile item
 *  2/14/22, Yoar
 *		Initial version.
 */

using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Township
{
    [PropertyObject]
    public class TownshipStockpile : CompactArray
    {
        [Flags]
        public enum StockFlag : uint
        {
            None = 0x00,

            Boards = 0x01,
            Ingots = 0x02,
            Granite = 0x04,
            FertileDirt = 0x08,
            Marble = 0x10,
            Sandstone = 0x20,
            Nightshade = 0x40,
        }

        public const int MaxQuantity = 10000000;

        private static readonly Type[][] m_LookupTable = new Type[][]
            {
                new Type[] { typeof(Board) },
                new Type[] { typeof(IronIngot) },
                new Type[] { typeof(BaseGranite) },
                new Type[] { typeof(FertileDirt) },
                new Type[] { typeof(Marble) },
                new Type[] { typeof(Sandstone) },
                new Type[] { typeof(Nightshade) },
            };

        public static StockFlag Identify(Item item)
        {
            return Identify(item.GetType());
        }

        private static readonly Type[] m_SingleType = new Type[1];

        public static StockFlag Identify(Type type)
        {
            m_SingleType[0] = type;

            return Identify(m_SingleType);
        }

        public static StockFlag Identify(Type[] types)
        {
            for (int i = 0; i < m_LookupTable.Length; i++)
            {
                Type[] lookupRow = m_LookupTable[i];

                for (int j = 0; j < lookupRow.Length; j++)
                {
                    Type lookupType = lookupRow[j];

                    for (int k = 0; k < types.Length; k++)
                    {
                        if (lookupType.IsAssignableFrom(types[k]))
                            return (StockFlag)(0x1 << i);
                    }
                }
            }

            return StockFlag.None;
        }

        public static string GetLabel(StockFlag stock)
        {
            switch (stock)
            {
                case StockFlag.FertileDirt:
                    return "fertile dirt";
            }

            return stock.ToString().ToLower();
        }

        private List<string> m_Log;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Boards
        {
            get { return this[(uint)StockFlag.Boards]; }
            set { this[(uint)StockFlag.Boards] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Ingots
        {
            get { return this[(uint)StockFlag.Ingots]; }
            set { this[(uint)StockFlag.Ingots] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Granite
        {
            get { return this[(uint)StockFlag.Granite]; }
            set { this[(uint)StockFlag.Granite] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FertileDirt
        {
            get { return this[(uint)StockFlag.FertileDirt]; }
            set { this[(uint)StockFlag.FertileDirt] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Marble
        {
            get { return this[(uint)StockFlag.Marble]; }
            set { this[(uint)StockFlag.Marble] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Sandstone
        {
            get { return this[(uint)StockFlag.Sandstone]; }
            set { this[(uint)StockFlag.Sandstone] = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Nightshade
        {
            get { return this[(uint)StockFlag.Nightshade]; }
            set { this[(uint)StockFlag.Nightshade] = value; }
        }

        public TownshipStockpile()
            : base()
        {
            m_Log = new List<string>();
        }

        #region Deposit Target

        public static void BeginDeposit(TownshipStone stone, Mobile from)
        {
            from.SendMessage("What do you wish to add to your township's stockpile?");
            from.Target = new DepositTarget(stone);
        }

        private class DepositTarget : Target
        {
            private TownshipStone m_Stone;

            public DepositTarget(TownshipStone stone)
                : base(2, false, TargetFlags.None)
            {
                CheckLOS = true;
                m_Stone = stone;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.AllowBuilding(from)) // TODO: Message if we're not allowed to build
                    return;

                m_Stone.Stockpile.CheckDeposit(from, targeted);

                from.Target = new DepositTarget(m_Stone);
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (!from.CheckAlive() || !m_Stone.CheckView(from) || !m_Stone.AllowBuilding(from)) // TODO: Message if we're not allowed to build
                    return;

                from.CloseGump(typeof(Gumps.TownshipGump));
                from.SendGump(new Gumps.TownshipGump(m_Stone, from, Gumps.TownshipGump.Page.Stockpile));
            }
        }

        public enum DepositResult
        {
            Success,
            Invalid,
            Full,
            InvalidContainer,
            NoPackyAccess,
        }

        public void CheckDeposit(Mobile from, object targeted)
        {
            Dictionary<StockFlag, int> dict = new Dictionary<StockFlag, int>();

            DepositResult result = DepositInternal(from, targeted, dict);

            if (result != DepositResult.Success)
            {
                from.SendMessage(GetMessage(result));
                return;
            }

            foreach (KeyValuePair<StockFlag, int> kvp in dict)
                from.SendMessage("You deposited {0} {1} to the township's stockpile.", kvp.Value, GetLabel(kvp.Key));
        }

        private DepositResult DepositInternal(Mobile from, object targeted, Dictionary<StockFlag, int> dict)
        {
            if (targeted is Container)
            {
                return DepositContainer(from, (Container)targeted, dict);
            }
            else if (targeted is CommodityDeed)
            {
                return DepositCommodityDeed(from, (CommodityDeed)targeted, dict);
            }
            else if (targeted is StockpileDeed)
            {
                return DepositStockpileDeed(from, (StockpileDeed)targeted, dict);
            }
            else if (targeted is Item)
            {
                return DepositItem(from, (Item)targeted, dict);
            }
            else if (targeted is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)targeted;

                if (bc.Backpack != null && (bc is PackHorse || bc is PackLlama || bc is Beetle || bc is HordeMinion))
                {
                    if (!PackAnimal.CheckAccess(bc, from))
                        return DepositResult.NoPackyAccess;
                    else
                        return DepositContainer(from, bc.Backpack, dict);
                }
                else
                {
                    return DepositResult.Invalid;
                }
            }
            else
            {
                return DepositResult.Invalid;
            }
        }

        private DepositResult DepositContainer(Mobile from, Container cont, Dictionary<StockFlag, int> dict)
        {
            bool any = false;

            List<Item> contents = new List<Item>(cont.Items);

            foreach (Item item in contents)
            {
                DepositResult result;

                if (item is CommodityDeed)
                    result = DepositCommodityDeed(from, (CommodityDeed)item, dict);
                else if (item is StockpileDeed)
                    result = DepositStockpileDeed(from, (StockpileDeed)item, dict);
                else
                    result = DepositItem(from, item, dict);

                if (result == DepositResult.Success)
                    any = true;
            }

            if (!any)
                return DepositResult.InvalidContainer;

            return DepositResult.Success;
        }

        private DepositResult DepositCommodityDeed(Mobile from, CommodityDeed deed, Dictionary<StockFlag, int> dict)
        {
            if (!deed.Movable)
                return DepositResult.Invalid;

            if (deed.Type == null || deed.CommodityAmount <= 0)
                return DepositResult.Invalid;

            StockFlag stock = Identify(deed.Type);

            if (stock == StockFlag.None)
                return DepositResult.Invalid;

            int deposit = DepositUpTo(from, stock, deed.CommodityAmount);

            if (deposit <= 0)
                return DepositResult.Full;

            deed.CommodityAmount -= deposit;

            if (deed.CommodityAmount <= 0)
                deed.Delete();

            if (dict.ContainsKey(stock))
                dict[stock] += deposit;
            else
                dict[stock] = deposit;

            return DepositResult.Success;
        }

        private DepositResult DepositStockpileDeed(Mobile from, StockpileDeed deed, Dictionary<StockFlag, int> dict)
        {
            if (!deed.Movable)
                return DepositResult.Invalid;

            if (deed.Stock == StockFlag.None || deed.Quantity <= 0)
                return DepositResult.Invalid;

            int deposit = DepositUpTo(from, deed.Stock, deed.Quantity);

            if (deposit <= 0)
                return DepositResult.Full;

            deed.Quantity -= deposit;

            if (deed.Quantity <= 0)
                deed.Delete();

            if (dict.ContainsKey(deed.Stock))
                dict[deed.Stock] += deposit;
            else
                dict[deed.Stock] = deposit;

            return DepositResult.Success;
        }

        private DepositResult DepositItem(Mobile from, Item item, Dictionary<StockFlag, int> dict)
        {
            if (!item.Movable || item.Amount <= 0)
                return DepositResult.Invalid;

            StockFlag stock = Identify(item);

            if (stock == StockFlag.None)
                return DepositResult.Invalid;

            int deposit = DepositUpTo(from, stock, item.Amount);

            if (deposit <= 0)
                return DepositResult.Full;

            item.Consume(deposit);

            if (dict.ContainsKey(stock))
                dict[stock] += deposit;
            else
                dict[stock] = deposit;

            return DepositResult.Success;
        }

        private static string GetMessage(DepositResult result)
        {
            switch (result)
            {
                case DepositResult.Invalid:
                    return "You can't add that to the township's stockpile!";
                case DepositResult.Full:
                    return "The stockpile is full!";
                case DepositResult.InvalidContainer:
                    return "Nothing in this container could be added to the township's stockpile.";
                case DepositResult.NoPackyAccess:
                    return "That is not your pack animal!";
            }

            return null;
        }

        #endregion

        public int DepositUpTo(Mobile from, StockFlag stock, int amount)
        {
            int toDeposit = Math.Min(MaxQuantity - this[(uint)stock], amount);

            if (toDeposit <= 0)
                return 0;

            this[(uint)stock] += toDeposit;

            if (from != null)
                AddLog("{0} deposited {2} {1}", from, GetLabel(stock), toDeposit);

            return toDeposit;
        }

        public int WithdrawUpTo(Mobile from, StockFlag stock, int amount)
        {
            int toWithdraw = Math.Min(this[(uint)stock], amount);

            if (toWithdraw <= 0)
                return 0;

            this[(uint)stock] -= toWithdraw;

            if (from != null)
                AddLog("{0} withdrew {2} {1}", from, GetLabel(stock), toWithdraw);

            return toWithdraw;
        }

        private void AddLog(string format, params object[] args)
        {
            while (m_Log.Count >= 10)
                m_Log.RemoveAt(0);

            m_Log.Add(String.Format(format, args));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write((int)m_Log.Count);

            for (int i = 0; i < m_Log.Count; i++)
                writer.Write((string)m_Log[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                            m_Log.Add(reader.ReadString());

                        break;
                    }
            }
        }

        public override string ToString()
        {
            return "...";
        }
    }

    [TypeAlias("Server.Township.TownshipStockpileDeed")]
    [Flipable(0x14EF, 0x14F0)]
    public class StockpileDeed : Item
    {
        public override string DefaultName
        {
            get { return String.Format("a shipment of {0:N0} {1}", m_Quantity, TownshipStockpile.GetLabel(m_Stock)); }
        }

        private TownshipStockpile.StockFlag m_Stock;
        private int m_Quantity;

        [CommandProperty(AccessLevel.GameMaster)]
        public TownshipStockpile.StockFlag Stock
        {
            get { return m_Stock; }
            set { m_Stock = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Quantity
        {
            get { return m_Quantity; }
            set { m_Quantity = value; }
        }

        [Constructable]
        public StockpileDeed()
            : this(TownshipStockpile.StockFlag.None, 0)
        {
        }

        [Constructable]
        public StockpileDeed(TownshipStockpile.StockFlag stock, int quantity)
            : base(0x14F0)
        {
            m_Stock = stock;
            m_Quantity = quantity;
        }

        public StockpileDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((uint)m_Stock);
            writer.Write((int)m_Quantity);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        m_Stock = (TownshipStockpile.StockFlag)reader.ReadUInt();
                        m_Quantity = reader.ReadInt();
                        break;
                    }
            }

            if (version < 1)
                Name = null;
        }
    }
}