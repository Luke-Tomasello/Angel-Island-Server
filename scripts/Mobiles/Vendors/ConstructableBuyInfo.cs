/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Mobiles\Vendors\ConstructableBuyInfo.cs
 * ChangeLog
 *  5/16/2026, Adam
 *      first time checkin
 */

using System;

namespace Server.Mobiles
{
    public class ConstructableBuyInfo : GenericBuyInfo
    {
        private object[] m_Args;

        public ConstructableBuyInfo(
            string name,
            Type type,
            object[] args,
            int price,
            int amount,
            int itemID,
            int hue)
            : base(name, type, price, amount, itemID, hue)
        {
            m_Args = args;
        }

        public override object GetObject()
        {
            return Activator.CreateInstance(Type, m_Args);
        }
    }
}