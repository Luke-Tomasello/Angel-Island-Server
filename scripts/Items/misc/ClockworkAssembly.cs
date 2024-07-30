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

/* Items/Misc/ClockworkAssembly.cs
 * ChangeLog:
 *  1/1/24, Yoar
 *      Golems that are crafted in the Winter Event area will be stablable at elf stablers.
 *  10/9/2023, Adam (Control Slots / Core.SiegeII_CFG)
 *      For siege II we are dropping the control slots to 3 from 4
 *      https://uo.stratics.com/database/view.php?db_content=hunters&id=197
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    [Flipable(0x1EAC, 0x1EA8)]
    public class ClockworkAssembly : Item
    {
        [Constructable]
        public ClockworkAssembly()
            : base(0x1EA8)
        {
            Weight = 5.0;
            Hue = 1102;
            Name = "clockwork assembly";
        }

        public ClockworkAssembly(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            double tinkerSkill = from.Skills[SkillName.Tinkering].Value;

            if (tinkerSkill < 60.0)
            {
                from.SendMessage("You must have at least 60.0 skill in tinkering to construct a golem.");
                return;
            }
            // 4 control slots is standard OSI
            // https://uo.stratics.com/database/view.php?db_content=hunters&id=197
            else if ((from.FollowerCount + (Core.SiegeII_CFG ? 3 : 4)) > from.FollowersMax)
            {
                from.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                return;
            }

            double scalar;

            if (tinkerSkill >= 100.0)
                scalar = 1.0;
            else if (tinkerSkill >= 90.0)
                scalar = 0.9;
            else if (tinkerSkill >= 80.0)
                scalar = 0.8;
            else if (tinkerSkill >= 70.0)
                scalar = 0.7;
            else
                scalar = 0.6;

            Container pack = from.Backpack;

            if (pack == null)
                return;

            int res = pack.ConsumeTotal(
                new Type[]
                {
                    typeof( PowerCrystal ),
                    typeof( IronIngot ),
                    typeof( BronzeIngot ),
                    typeof( Gears )
                },
                new int[]
                {
                    1,
                    50,
                    50,
                    5
                });

            switch (res)
            {
                case 0:
                    {
                        from.SendMessage("You must have a power crystal to construct the golem.");
                        break;
                    }
                case 1:
                    {
                        from.SendMessage("You must have 50 iron ingots to construct the golem.");
                        break;
                    }
                case 2:
                    {
                        from.SendMessage("You must have 50 bronze ingots to construct the golem.");
                        break;
                    }
                case 3:
                    {
                        from.SendMessage("You must have 5 gears to construct the golem.");
                        break;
                    }
                default:
                    {
                        Golem g = new Golem(true, scalar);

                        if (g.SetControlMaster(from))
                        {
                            Delete();

                            g.MoveToWorld(from.Location, from.Map);
                            from.PlaySound(0x241);

                            if (Misc.WinterEventSystem.Contains(from))
                                g.IsWinterHolidayPet = true;
                        }

                        break;
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