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

using Server.Factions;
using System;

namespace Server.Ethics.Hero
{
    public sealed class HeroEthic : Ethic
    {
        public HeroEthic()
        {
            m_Definition = new EthicDefinition(
                    0x482,
                    "Hero", "(Hero)",
                    "I will defend the virtues",
                    "I invoke my good powers",
                    Core.NewEthics
                    ? new Power[]
                    {
                        new HolyItem(),
                        new HolySense(),
                        new SummonFamiliar(),
                        new HolyBless(),
                        new HolySteed(),
                        new HolyShield(),
                    }
                    : new Power[]
                    {
                        new HolySense(),
                        new HolyItem(),
                        new SummonFamiliar(),
                        new HolyBlade(),
                        new Bless(),
                        new HolyShield(),
                        new HolySteed(),
                        new HolyWord()
                    }
                );
        }

        public override bool IsEligible(Mobile mob)
        {
            if (Core.NewEthics)
            {
                if (mob.ShortTermMurders >= 5)
                    return false;

                Faction fac = Faction.Find(mob);

                return (fac is TrueBritannians || fac is CouncilOfMages);
            }
            else
            {
                return (DateTime.UtcNow >= mob.Created + TimeSpan.FromHours(24));
            }
        }

        public override void OnJoin(Mobile mob)
        {
            base.OnJoin(mob);

            // TODO: Old Ethic message?
        }
    }
}