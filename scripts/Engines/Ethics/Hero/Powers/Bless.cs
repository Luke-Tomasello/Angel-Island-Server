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

using Server.Spells;
using System;

namespace Server.Ethics.Hero
{
    public sealed class Bless : Power
    {
        public Bless()
        {
            m_Definition = new PowerDefinition(
                    15,
                    6,
                    "Bless",
                    "Erstok Ontawl",
                    ""
                );
        }

        public override void BeginInvoke(Player from)
        {
            from.Mobile.BeginTarget(12, true, Targeting.TargetFlags.None, new TargetStateCallback(Power_OnTarget), from);
            from.Mobile.SendMessage("Where do you wish to bless?");
        }

        private void Power_OnTarget(Mobile fromMobile, object obj, object state)
        {
            Player from = state as Player;

            IPoint3D p = obj as IPoint3D;

            if (p == null)
                return;

            if (!CheckInvoke(from))
                return;

            bool powerFunctioned = false;

            SpellHelper.GetSurfaceTop(ref p);

            foreach (Mobile mob in from.Mobile.GetMobilesInRange(6))
            {
                if (!SpellHelper.ValidIndirectTarget(from.Mobile, mob))
                    continue;

                if (mob.GetStatMod("Holy Bless") != null)
                    continue;

                if (!from.Mobile.CanBeBeneficial(mob, false))
                    continue;

                // A targeted area effect spell that gives a temporary boost to stats to all innocents in range, except for guild enemies. 
                int n = Notoriety.Compute(from.Mobile, mob);
                if (n == Notoriety.Criminal || n == Notoriety.Murderer || n == Notoriety.Enemy)
                    continue;

                from.Mobile.DoBeneficial(mob);

                mob.AddStatMod(new StatMod(StatType.All, "Holy Bless", 10, TimeSpan.FromMinutes(30.0)));

                mob.FixedParticles(0x373A, 10, 15, 5018, EffectLayer.Waist);
                mob.PlaySound(0x1EA);

                powerFunctioned = true;
            }

            if (powerFunctioned)
            {
                SpellHelper.Turn(from.Mobile, p);

                Effects.PlaySound(p, from.Mobile.Map, 0x299);

                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You consecrate the area.");

                FinishInvoke(from);
            }
            else
            {
                from.Mobile.FixedEffect(0x3735, 6, 30);
                from.Mobile.PlaySound(0x5C);
            }
        }
    }
}