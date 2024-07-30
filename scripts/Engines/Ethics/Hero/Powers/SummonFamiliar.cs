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

using Server.Mobiles;
using System;

namespace Server.Ethics.Hero
{
    public sealed class SummonFamiliar : Power
    {
        public SummonFamiliar()
        {
            m_Definition = new PowerDefinition(
                    Core.NewEthics ? 10 : 5,
                    3,
                    "Summon Familiar",
                    "Trubechs Vingir",
                    "Summons a silver wolf you may use as a pet."
                );
        }

        public override void BeginInvoke(Player from)
        {
            if (from.Familiar != null && from.Familiar.Deleted)
                from.Familiar = null;

            if (from.Familiar != null)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You already have a holy familiar.");
                return;
            }

            if ((from.Mobile.FollowerCount + 1) > from.Mobile.FollowersMax)
            {
                from.Mobile.SendLocalizedMessage(1049645); // You have too many followers to summon that creature.
                return;
            }

            HolyFamiliar familiar = new HolyFamiliar();

            if (Mobiles.BaseCreature.Summon(familiar, from.Mobile, from.Mobile.Location, 0x217, TimeSpan.FromHours(1.0)))
            {
                from.Familiar = familiar;

                // update familiar's Notoriety
                if (familiar != null)
                    familiar.Delta(MobileDelta.Noto);

                FinishInvoke(from);
            }
        }
    }
}