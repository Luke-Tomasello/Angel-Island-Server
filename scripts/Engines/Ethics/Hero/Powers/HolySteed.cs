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

using System;

namespace Server.Ethics.Hero
{
    public sealed class HolySteed : Power
    {
        public HolySteed()
        {
            m_Definition = new PowerDefinition(
                    30,
                    5,
                    "Silver Steed",
                    "Trubechs Yeliab",
                    "Summons a silver steed you may use as a mount."
                );
        }

        public override void BeginInvoke(Player from)
        {
            if (from.Steed != null && from.Steed.Deleted)
                from.Steed = null;

            if (from.Steed != null)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You already have a holy steed.");
                return;
            }

            if ((from.Mobile.FollowerCount + 1) > from.Mobile.FollowersMax)
            {
                from.Mobile.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                return;
            }

            Mobiles.HolySteed steed = new Mobiles.HolySteed();

            if (Mobiles.BaseCreature.Summon(steed, from.Mobile, from.Mobile.Location, 0x217, TimeSpan.FromHours(1.0)))
            {
                from.Steed = steed;

                // update steed's Notoriety
                if (steed != null)
                    steed.Delta(MobileDelta.Noto);

                FinishInvoke(from);
            }
        }
    }
}