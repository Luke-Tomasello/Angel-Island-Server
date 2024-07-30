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

/* Scripts\Items\misc\SilverNitrateCauldron.cs
 * Changelog
 *  11/11/2023, Adam
 *      created. 
 *      Allows players to temporarily convert their weapon to silver
 *      ** UNDER DEVELOPMENT **
 */
using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class SilverNitrateCauldron : BaseAddon
    {
        public class Context
        {
            public SlayerName Name;
            public DateTime Expiry;
            public Context(SlayerName name, DateTime expiry)
            {
                Name = name;
                Expiry = expiry;
            }
        }
        private static Dictionary<BaseWeapon, Context> Table = new();
        [Constructable]
        public SilverNitrateCauldron()
            : base()
        {
            ItemID = 0x975; // Cauldron
            Movable = false;
            Visible = true;
            // paint
            AddonComponent paint = new AddonComponent(0x970);
            paint.Hue = 2101;
            paint.Movable = false;
            paint.Visible = true;
            paint.Name = "silver nitrate";
            AddComponent(paint, 0, 0, 8);
        }

        public SilverNitrateCauldron(Serial serial)
            : base(serial)
        {
        }
        public override void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
            OnDoubleClick(from);
        }
        public override void OnDoubleClick(Mobile from)
        {
            PlayerMobile pm = (from as PlayerMobile);
            if (pm != null && pm.InRange(this.Location, 2))
            {
                Item weapon = pm.Weapon as Item;
                if (weapon == null || weapon is Fists || weapon is not BaseWeapon)
                {
                    from.SendMessage("You must equip the weapon you wish to imbue.");
                }
                else
                {
                    ;
                    BaseWeapon bw = weapon as BaseWeapon;
                    if (bw.Slayer == SlayerName.Silver)
                    {
                        from.SendMessage("That weapon is already silver and cannot be imbued.");
                        return;
                    }
                    else
                    {
                        if (!Table.ContainsKey(bw))
                        {
                            Table.Add(bw, new Context(bw.Slayer, DateTime.UtcNow + TimeSpan.FromMinutes(10)));
                            bw.Slayer = SlayerName.Silver;
                        }
                        else
                        {
                            // error - should have been defragged
                        }
                    ;

                    }
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}