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

namespace Server.Ethics.Hero
{
    public sealed class HolyBlade : Power
    {
        public HolyBlade()
        {
            m_Definition = new PowerDefinition(
                    10,
                    4,
                    "Holy Blade",
                    "Erstok Reyam",
                    ""
                );
        }

        public override void BeginInvoke(Player from)
        {
            from.Mobile.BeginTarget(12, false, Targeting.TargetFlags.None, new TargetStateCallback(Power_OnTarget), from);
            from.Mobile.SendMessage("Target the item you wish to bless.");
        }

        private void Power_OnTarget(Mobile fromMobile, object obj, object state)
        {
            Player from = state as Player;

            Item item = obj as Item;

            if (item == null)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You may not bless that.");
                return;
            }

            if (item.Parent != from.Mobile)
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You may only bless items you have equipped.");
                return;
            }

            bool canImbue = (item is BaseWeapon && item.Name == null);

            if (canImbue)
            {

                if (item.HolyBlade && !from.Mobile.Godly(AccessLevel.Administrator))
                {
                    from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "That has already been blessed.");
                    return;
                }

                if (!CheckInvoke(from))
                    return;

                int itemID, soundID;
                int whiteHue = 0x47E;

                switch ((item as BaseWeapon).Skill)
                {
                    case SkillName.Macing: itemID = 0xFB4; soundID = 0x232; break;
                    case SkillName.Archery: itemID = 0x13B1; soundID = 0x145; break;
                    default: itemID = 0xF5F; soundID = 0x56; break;
                }

                // no idea what effects to use here not the message, make something up
                from.Mobile.PlaySound(0x20C);
                from.Mobile.PlaySound(soundID);
                from.Mobile.FixedParticles(0x3779, 1, 30, 9964, 3, 3, EffectLayer.Waist);

                IEntity fromLoc = new Entity(Serial.Zero, new Point3D(from.Mobile.X, from.Mobile.Y, from.Mobile.Z), from.Mobile.Map);
                IEntity toLoc = new Entity(Serial.Zero, new Point3D(from.Mobile.X, from.Mobile.Y, from.Mobile.Z + 50), from.Mobile.Map);
                Effects.SendMovingParticles(fromLoc, toLoc, itemID, 1, 0, false, false, whiteHue, 3, 9501, 1, 0, EffectLayer.Head, 0x100);

                // Holy Blade	Erstok Reyam	
                // Effectively makes the weapon (including bows) have double damage against evils and undead. 
                item.HolyBlade = true;
                item.VileBlade = false;

                // don't want double damage to undead from being holy AND and additional double damage because it's silver
                if ((item as BaseWeapon).Slayer == SlayerName.Silver)
                    (item as BaseWeapon).Slayer = SlayerName.None;

                FinishInvoke(from);
            }
            else
            {
                from.Mobile.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, false, "You may not bless that.");
            }
        }
    }
}