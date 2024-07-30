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

using Server.Items;
using System;

namespace Server.Mobiles
{
    public class BeverageBuyInfo : GenericBuyInfo
    {
        private BeverageType m_Content;

        public BeverageBuyInfo(Type type, BeverageType content, int price, int amount, int itemID, int hue)
            : this(null, type, content, price, amount, itemID, hue)
        {
        }

        public BeverageBuyInfo(string name, Type type, BeverageType content, int price, int amount, int itemID, int hue)
            : base(name, type, price, amount, itemID, hue)
        {
            m_Content = content;

            if (type == typeof(Pitcher))
                Name = (1048128 + (int)content).ToString();
            else if (type == typeof(BeverageBottle))
                Name = (1042959 + (int)content).ToString();
            else if (type == typeof(Jug))
                Name = (1042965 + (int)content).ToString();
        }

        public override object GetObject()
        {
            return Activator.CreateInstance(Type, new object[] { m_Content });
        }
    }
}