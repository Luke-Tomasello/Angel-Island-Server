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

/* Scripts\Engines\AngelIsland\AIPUtils.cs
 * ChangeLog
 * 8/31/21 Adam
 *	moved from CoreAI
 */

using Server.Items;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server
{
    public class AIPUtils
    {
        public static void KillPrisonPets(Mobile m)
        {
            if (m is PlayerMobile pm)
                foreach (var p in pm.Followers)
                    if (p is BaseCreature bc)
                        if (bc.GetCreatureBool(CreatureBoolTable.IsPrisonPet))
                            bc.Kill();
        }
        public static void Undress(Mobile m)
        {
            if (m != null && m.Backpack != null)
                try
                {
                    List<Item> items = new()
                    {
                        m.FindItemOnLayer(Layer.Shoes),
                        m.FindItemOnLayer(Layer.Pants),
                        m.FindItemOnLayer(Layer.Shirt),
                        m.FindItemOnLayer(Layer.Helm),
                        m.FindItemOnLayer(Layer.Gloves),
                        m.FindItemOnLayer(Layer.Neck),
                        m.FindItemOnLayer(Layer.Waist),
                        m.FindItemOnLayer(Layer.InnerTorso),
                        m.FindItemOnLayer(Layer.MiddleTorso),
                        m.FindItemOnLayer(Layer.Arms),
                        m.FindItemOnLayer(Layer.Cloak),
                        m.FindItemOnLayer(Layer.OuterTorso),
                        m.FindItemOnLayer(Layer.OuterLegs),
                        m.FindItemOnLayer(Layer.InnerLegs),
                        m.FindItemOnLayer(Layer.Bracelet),
                        m.FindItemOnLayer(Layer.Ring),
                        m.FindItemOnLayer(Layer.Earrings),
                        m.FindItemOnLayer(Layer.OneHanded),
                        m.FindItemOnLayer(Layer.TwoHanded),
                        //m.FindItemOnLayer(Layer.Hair),
                        //m.FindItemOnLayer(Layer.FacialHair),
                    };
                    foreach (Item item in items)
                    {
                        if (item != null && item is not DeathRobe)
                        {
                            m.RemoveItem(item);
                            m.Backpack.DropItem(item);
                        }
                    }
                }
                catch (Exception exc)
                {
                    System.Console.WriteLine("Send to Zen please: ");
                    System.Console.WriteLine("Exception caught in Mobile.WipeLayers: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
        }

        public static void DecolorizeRobe(Mobile m)
        {
            if (m.Items != null && m.Items.Count > 0)
            {
                foreach (Item item in m.Items)
                {
                    if (item is DeathRobe)
                        item.Hue = 0x0;
                }
            }
        }
        public static void DroptoBackpack(Mobile m)
        {
            Container backpack = m.Backpack;
            if (backpack == null)
                return;

            // put whatever you are holding in you backpack
            // (the 'drag' kind of holding)
            Item held = m.Holding;
            if (held != null)
            {
                held.ClearBounce();
                if (m.Backpack != null)
                {
                    m.Backpack.DropItem(held);
                }
            }
            m.Holding = null;

            // put whatever you are holding in your backpack
            // (actually in your hand kind of holding)
            Item weapon = m.Weapon as Item;
            if (weapon != null && weapon.Parent == m && !(weapon is Fists))
                backpack.DropItem(weapon);
        }
        public static void RolledInYourSleep(Mobile m)
        {

            Container backpack = m.Backpack;
            if (backpack == null)
                return;

            // drop everything held to backpack
            DroptoBackpack(m);

            ArrayList stuff = backpack.FindAllItems();
            if (stuff != null && stuff.Count > 0)
            {
                for (int ix = 0; ix < stuff.Count; ix++)
                {
                    Item item = stuff[ix] as Item;
                    // process items as follows
                    //	delete everything but stinger and spellbook. Sorry, rares collected gone now too :(

                    if (item is BaseWeapon && item is AIStinger == true)
                        continue;

                    if (item is Spellbook)
                        continue;

                    // don't delete the players backpack
                    if (item.Serial == backpack.Serial)
                        continue;

                    item.Delete();
                }
            }

            return;
        }
    }
}