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

namespace Server.Ethics.Evil
{
    public sealed class VileBlade : Power
    {
        public VileBlade()
        {
            m_Definition = new PowerDefinition(
                    10,
                    4,
                    "Vile Blade",
                    "Velgo Reyam",
                    ""
                );
        }

        public override void BeginInvoke(Player from)
        {
            from.Mobile.BeginTarget(12, false, Targeting.TargetFlags.None, new TargetStateCallback(Power_OnTarget), from);
            from.Mobile.SendMessage("Target the item you wish to curse.");
        }

        private void Power_OnTarget(Mobile fromMobile, object obj, object state)
        {
            Player from = state as Player;

            Item item = obj as Item;

            if (item == null)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You may not curse that.");
                return;
            }

            if (item.Parent != from.Mobile)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You may only curse items you have equipped.");
                return;
            }

            bool canImbue = (item is BaseWeapon && item.Name == null);

            if (canImbue)
            {

                if (item.VileBlade && (item as BaseWeapon).PoisonCharges > 0 && !from.Mobile.Godly(AccessLevel.Administrator))
                {
                    from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "That has already been cursed.");
                    return;
                }

                if (!CheckInvoke(from))
                    return;

                from.Mobile.PlaySound(0x387);
                from.Mobile.FixedParticles(0x3779, 1, 15, 9905, 32, 2, EffectLayer.Head);
                from.Mobile.FixedParticles(0x37B9, 1, 14, 9502, 32, 5, (EffectLayer)255);
                new SoundEffectTimer(from.Mobile).Start();

                // Vile blade	Velgo Reyam	
                // This power actually works on any weapon (including maces and bows). 
                // It gives the weapon a few charges of powerful (level 5 !!) poison. Once a weapon has been made vile, 
                //	it can never again be used by a hero. 
                (item as BaseWeapon).Poison = Poison.Lethal;
                (item as BaseWeapon).PoisonCharges = 3;
                item.VileBlade = true;
                item.HolyBlade = false;

                FinishInvoke(from);
            }
            else
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You may not curse that.");
            }
        }

        private class SoundEffectTimer : Timer
        {
            private Mobile m_Mobile;

            public SoundEffectTimer(Mobile m)
                : base(TimeSpan.FromSeconds(0.75))
            {
                m_Mobile = m;
                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                m_Mobile.PlaySound(0xFA);
            }
        }
    }
}