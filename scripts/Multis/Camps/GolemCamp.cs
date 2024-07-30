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

/* Scripts/Multis/Camps/GolemCamp.cs
 * ChangeLog
 *  8/20/2023, Adam (GolemCamp)
 *      Created
 */

using Server.Items;
using Server.Mobiles;
using System.IO;

namespace Server.Multis
{
    public class GolemCamp : BaseCamp
    {
        [Constructable]
        public GolemCamp()
            : base(0x1F14)   // recall rune
        {
        }

        public override void AddComponents()
        {
            AddonData AddonData = XmlAddonSystem.GetData(Path.Combine(Core.DataDirectory, "XmlAddon/Camps"), "GolumCamp");

            // build the basic structure from the XML
            //  with a tweak here or there
            int ingots = 0;
            foreach (var c in AddonData.Components)
            {
                Item sitem = new Item(c.ItemID);
                switch (c.ItemID)
                {
                    case 0x1BF2:
                        {
                            if (ingots++ == 0)
                            {
                                sitem.Delete();
                                sitem = new BronzeIngot(50);
                            }
                            else
                            {
                                sitem.Delete();
                                sitem = new IronIngot(50);
                            }
                            break;
                        }
                    case 0x1F1C:
                        {   // power crystal
                            sitem.Delete();
                            sitem = new PowerCrystal();
                            break;
                        }
                    case 0x1EAC:
                        {   // clockwork assembly
                            sitem.Delete();
                            sitem = new ClockworkAssembly();
                            sitem.ItemID = 0x1EAC;
                            break;
                        }
                    case 0x1053:
                        {   // gears
                            sitem.Delete();
                            sitem = new Gears(5);
                            break;
                        }
                    case 0x5F14:
                        {   // recall rune
                            sitem.Map = Map.Internal;
                            sitem.Visible = false;
                            break;
                        }
                }

                sitem.Movable = false;
                AddItem(sitem, c.X, c.Y - 1, c.Z);
            }

            // add some baddies
            for (int ix = 0; ix < 4; ix++)
            {
                GolemController gc = new GolemController();
                AddMobile(gc, wanderRange: ix < 2 ? 2 : 5, ix < 2 ? 2 : 5, ix < 2 ? 2 : 5, 0);
            }
            for (int ix = 0; ix < 2; ix++)
            {
                Golem g = new Golem();
                AddMobile(g, wanderRange: 5, 5, 5, 0);
            }
            // enhance AI - everyone will focus on the Strongest, except two controllers will focus on the Weakest :>
            int count = 0;
            foreach (Mobile m in MobileComponents)
            {
                if (m is BaseCreature bc)
                {
                    if (bc is GolemController && count++ < 2)
                        bc.FightMode = FightMode.All | FightMode.Weakest;
                    else
                        bc.FightMode = FightMode.All | FightMode.Strongest;
                }
            }
        }
        public override void OnDeath(Mobile killed)
        {
            int needed = MobileComponents.Count - 1;
            foreach (Mobile m in MobileComponents)
                if (m == killed)
                    continue;
                else if (!m.Alive)
                    needed--;

            if (needed == 0)
                foreach (Item item in ItemComponents)
                    switch (item.ItemID)
                    {
                        case 0x1BF2:    // iron ingot / bronze ingot
                        case 0x1F1C:    // power crystal
                        case 0x1EAC:    // clockwork assembly
                        case 0x1053:    // gears
                            {
                                item.SetItemBool(ItemBoolTable.MustSteal, true);
                                continue;
                            }
                    }

            base.OnDeath(killed);
        }
        public override void OnAfterDelete()
        {
            base.OnAfterDelete();
        }

        public GolemCamp(Serial serial)
        : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

}