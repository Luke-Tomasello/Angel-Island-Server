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

/* Scripts/Engines/BulkOrders/BulkOrderRewards.cs
 * CHANGELOG:
 *  11/22/23, Yoar
 *      Moved reward definitions to DefSmith, DefTailor
 *  9/30/23, Yoar
 *      Reworked smithing rewards for Siege.
 *      For now, we're only enabling small BODs.
 *      The highest number of points we can obtain is 800.
 *  10/26/21, Yoar
 *      Disabled tailor rewards.
 *	10/24/21, Adam
 *		Added, 
 *		+15 Ancient Hammer
 *		+20 Ancient Hammer
 *		Blacksmith Cooperative Membership
 *		Powder of Fortification
 *  10/24/21, Yoar
 *      - Added rock hammer. Mining with a rock hammer increases the chance of finding granite.
 *      - Increased the charges on resource maps.
 *  10/14/21, Yoar
 *      Initial version.
 *      
 *      These classes deals with the new reward mechanic (OSI Pub95) for BODs:
 *      - RewardCalculator.ComputePoints returns the worth of a BOD in bulk order points.
 *      - RewardCalculator.Rewards defines the rewards you can buy with your bulk order points.
 */

using System;

namespace Server.Engines.BulkOrders
{
    public class RewardEntry
    {
        private int m_Points;
        private int m_Weight;
        private BaseReward m_Reward;

        public int Points { get { return m_Points; } }
        public int Weight { get { return m_Weight; } }
        public BaseReward Reward { get { return m_Reward; } }

        public RewardEntry(int points, int weight, ConstructCallback constructor, int type = 0)
            : this(points, weight, new CustomReward(constructor, type))
        {
        }

        public RewardEntry(int points, int weight, BaseReward reward)
        {
            m_Points = points;
            m_Weight = weight;
            m_Reward = reward;
        }
    }

    public class RewardOption
    {
        private int m_Points;
        private int m_ItemID;
        private int m_Hue;
        private string m_Label;
        private BaseReward m_Reward;

        public int Points { get { return m_Points; } }
        public int ItemID { get { return m_ItemID; } }
        public int Hue { get { return m_Hue; } }
        public string Label { get { return m_Label; } }
        public BaseReward Reward { get { return m_Reward; } }

        public RewardOption(int points, int itemID, int hue, string label, ConstructCallback constructor, int type = 0)
            : this(points, itemID, hue, label, new CustomReward(constructor, type))
        {
        }

        public RewardOption(int points, int itemID, int hue, string label, BaseReward reward)
        {
            m_Points = points;
            m_ItemID = itemID;
            m_Hue = hue;
            m_Label = label;
            m_Reward = reward;
        }
    }

    public abstract class BaseReward
    {
        public BaseReward()
        {
        }

        public abstract Item Construct();

        protected static Item Construct(Type type, object[] args = null)
        {
            if (!typeof(Item).IsAssignableFrom(type))
                return null;

            Item item;

            try
            {
                item = (Item)Activator.CreateInstance(type, args);
            }
            catch
            {
                item = null;
            }

            return item;
        }
    }

    public class ItemReward : BaseReward
    {
        private Type m_Type;
        private object[] m_Args;

        public Type Type { get { return m_Type; } }
        public object[] Args { get { return m_Args; } }

        public ItemReward(Type type, params object[] args)
        {
            m_Type = type;
            m_Args = args;
        }

        public override Item Construct()
        {
            return Construct(m_Type, m_Args);
        }
    }

    public class RandomReward : BaseReward
    {
        private Type[] m_Types;

        public Type[] Types { get { return m_Types; } }

        public RandomReward(params Type[] types)
        {
            m_Types = types;
        }

        public override Item Construct()
        {
            if (m_Types.Length == 0)
                return null;

            return Construct(m_Types[Utility.Random(m_Types.Length)]);
        }
    }

    public delegate Item ConstructCallback(int type);

    public class CustomReward : BaseReward
    {
        private ConstructCallback m_Constructor;
        private int m_Type;

        public ConstructCallback Constructor { get { return m_Constructor; } }
        public int Type { get { return m_Type; } }

        public CustomReward(ConstructCallback constructor, int type)
        {
            m_Constructor = constructor;
            m_Type = type;
        }

        public override Item Construct()
        {
            Item item;

            try
            {
                item = m_Constructor(m_Type);
            }
            catch
            {
                item = null;
            }

            return item;
        }
    }
}