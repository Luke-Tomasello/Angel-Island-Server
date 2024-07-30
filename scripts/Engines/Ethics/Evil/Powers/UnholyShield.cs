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

namespace Server.Ethics.Evil
{
    public sealed class UnholyShield : Power
    {
        public UnholyShield()
        {
            m_Definition = new PowerDefinition(
                    Core.NewEthics ? 40 : 20,
                    7,
                    "Unholy Shield",
                    "Velgo K'blac",
                    "Repels monsters for 1 hour."
                );
        }

        public override void BeginInvoke(Player from)
        {
            if (Core.NewEthics)
            {
                if (from.IsShielded)
                {
                    from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You are already under the protection of an unholy shield.");
                    return;
                }

                from.BeginShield();

                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You are now under the protection of an unholy shield.");
            }
            else
            {
                if (from.Mobile.CheckState(Mobile.ExpirationFlagID.MonsterIgnore))
                {
                    //from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You are already under the protection of an unholy shield.");
                    return;
                }

                // Question(9) on the boards - how long?
                // Answered: "Use this ability to make monsters ignore you for 1 hour."
                from.Mobile.ExpirationFlags.Add(new Mobile.ExpirationFlag(from.Mobile, Mobile.ExpirationFlagID.MonsterIgnore, TimeSpan.FromMinutes(60)));
            }

            FinishInvoke(from);
        }
    }
}