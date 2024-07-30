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

/* Scripts\Engines\CrownSterlingSystem\CrownSterlingReward.cs
 * CHANGELOG:
 *  6/25/2024, Adam
 *      Initial version.
 */

using System;

namespace Server.Engines.CrownSterlingSystem
{
    public class CrownSterlingReward
    {
        private object m_ItemO;
        private int m_Cost;
        private int m_Hue;
        private string m_Label;

        public int Cost { get { return m_Cost; } }
        public object ItemO { get { return m_ItemO; } }
        public int Hue { get { return m_Hue; } }
        public string Label { get { return m_Label; } }

        private int RandomOffset(string label)
        {   // add a random additional cost of up to 499 sterling to the baseCost.
            //  We use the label so that different facings will demand the same cost
            //  499 so that we do not enter the next base cost range (ranges jump 500 at a minimum)
            int hash = Utility.GetStableHashCode(CrownSterlingSystem.Normalize(label));
            return hash % 499;
        }

        public CrownSterlingReward(object itemO, int baseCost, int hue, string label)
        {
            m_ItemO = itemO;
            m_Hue = hue;
            m_Label = label;

            // if no label specified, grab the actual label
            if (m_Label == null)
                try
                {
                    if (m_ItemO is Item)
                    {
                        m_Label = ((Item)m_ItemO).OldSchoolName();
                        m_Label = CrownSterlingSystem.ClipName(m_Label.ToLower());
                    }
                    else
                    {
                        int itemID = (int)m_ItemO;
                        ItemData id = TileData.ItemTable[itemID & 0x3FFF];
                        string itemName = id.Name != null ? id.Name.ToLower() : null;

                        if (itemName != null)
                        {
                            itemName = itemName.Trim();

                            if (id.Flags.HasFlag(TileFlag.ArticleA) && id.Flags.HasFlag(TileFlag.ArticleAn))
                                itemName = "the " + itemName;
                            else if (id.Flags.HasFlag(TileFlag.ArticleA))
                                itemName = "a " + itemName;
                            else if (id.Flags.HasFlag(TileFlag.ArticleAn))
                                itemName = "an " + itemName;

                            m_Label = itemName;
                        }
                    }
                }
                catch
                {

                }

            if (m_Label == null)
                m_Label = "?";

            // add a random additional cost of up to 499 sterling to the baseCost.
            //  We use the label so that different facings will demand the same cost
            m_Cost = baseCost + RandomOffset(m_Label.ToLower());

            // grab the hue from the item
            if (hue == 0 && itemO is Item native_item && native_item is not CrownSterlingSystem.AddonFactory)
                m_Hue = native_item.Hue;

            // stash away
            if (m_ItemO is Item item)
            {   // by default, this item will be deleted on server restart because usually these inventory items are created on server up
                //  The exception is when vendors have a customized list, the vendor will clear the DeleteOnRestart flag
                item.SetItemBool(Item.ItemBoolTable.DeleteOnRestart, true);
                item.MoveToIntStorage();
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)1); // version

            // version 1
            writer.Write(m_ItemO is Item);

            if (m_ItemO is Item item)
                writer.Write(item);
            else
                writer.Write((int)m_ItemO);
            writer.Write(m_Cost);
            writer.Write(m_Hue);
            writer.Write(m_Label);
        }

        //public CrownSterlingReward(object itemO, int baseCost, int hue, string label)
        public CrownSterlingReward(GenericReader reader)
        {
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    {
                        bool isItem = reader.ReadBool();
                        if (isItem)
                            m_ItemO = reader.ReadItem();
                        else
                            m_ItemO = reader.ReadInt();
                        m_Cost = reader.ReadInt();
                        m_Hue = reader.ReadInt();
                        m_Label = reader.ReadString();
                        break;
                    }
            }
        }
        public override bool Equals(object obj)
        {
            return (obj is CrownSterlingReward && this.Equals((CrownSterlingReward)obj));
        }

        public bool Equals(CrownSterlingReward other)
        {   // we would like to flag pricing errors. I.e., things will be the same even if the price is different.
            bool result = m_Cost == other.Cost && m_Hue == other.Hue && m_Label == other.Label;
            if (result)
            {
                if (m_ItemO == null && other.ItemO == null)
                    return true;

                if (m_ItemO is Item item1)
                    if (other.ItemO is Item item2)
                        return item1 == item2;

                if (m_ItemO is int i1 && other.m_ItemO is int i2)
                    if (i1 == i2)
                        return true;

                return false;
            }
            else
                return false;
        }
        public override int GetHashCode()
        {
            int result = 0;
            result ^= m_Cost.GetHashCode();
            result ^= m_Hue.GetHashCode();
            if (m_Label != null)
                result ^= m_Label.GetHashCode();
            if (m_ItemO is Item item)
                result ^= item.GetHashCode();
            if (m_ItemO is int i)
                result ^= i.GetHashCode();

            return result;
        }
    }

    public abstract class BaseCrownReward
    {
        public BaseCrownReward()
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

    public class ItemCrownReward : BaseCrownReward
    {
        private Type m_Type;
        private object[] m_Args;

        public Type Type { get { return m_Type; } }
        public object[] Args { get { return m_Args; } }

        public ItemCrownReward(Type type, params object[] args)
        {
            m_Type = type;
            m_Args = args;
        }

        public override Item Construct()
        {
            return Construct(m_Type, m_Args);
        }
    }

    public class RandomReward : BaseCrownReward
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

    public class CustomCrownReward : BaseCrownReward
    {
        private ConstructCallback m_Constructor;
        private int m_Type;

        public ConstructCallback Constructor { get { return m_Constructor; } }
        public int Type { get { return m_Type; } }

        public CustomCrownReward(ConstructCallback constructor, int type)
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