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

/* Scripts/Skills/TasteID.cs
 * CHANGELOG
 *	4/13/23, Yoar. Fixes to Taste ID (confirmed on OSI):
 *		Reduced skill range from 2 to 1;
 *		Added PlaySound on tasting food;
 *		Fixed skill check range when tasting food;
 *		Fixed message that displays when you detect poison in food;
 *		Added chance to poison yourself if you fail tasting poisoned food;
 *		Added Potion, PotionKeg tasting.
 */

using Server.Items;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class TasteID
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.TasteID].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTarget();

            m.SendLocalizedMessage(502807); // What would you like to taste?

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(1, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is Mobile)
                {
                    from.SendLocalizedMessage(502816); // You feel that such an action would be inappropriate
                }
                else if (targeted is Food)
                {
                    Food food = (Food)targeted;

                    from.PlaySound(0x3A);

                    int minSkill = (food.Poison != null ? 25 : 0);

                    if (from.CheckTargetSkill(SkillName.TasteID, food, minSkill, minSkill + 50, new object[2] /*contextObj*/))
                    {
                        if (food.Poison != null)
                        {
                            food.SendLocalizedMessageTo(from, 1010599); // You sense a hint of foulness about that
                        }
                        else
                        {
                            // No poison on the food
                            from.SendLocalizedMessage(502823); // You cannot discern anything about this substance
                        }
                    }
                    else if (food.Poison != null && Utility.Random(5) == 0)
                    {
                        from.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 502822, from.NetState); // You did not detect the poison in time!
                        from.ApplyPoison(from, Poison.GetPoison(Utility.Random(4) == 0 ? 1 : 0));
                    }
                    else
                    {
                        // Skill check failed
                        from.SendLocalizedMessage(502823); // You cannot discern anything about this substance
                    }
                }
                else if (targeted is BasePotion)
                {
                    BasePotion potion = (BasePotion)targeted;

                    potion.SendLocalizedMessageTo(from, 502813); // You already know what kind of potion that is.
                    potion.SendLocalizedMessageTo(from, potion.LabelNumber);
                }
                else if (targeted is PotionKeg)
                {
                    PotionKeg keg = (PotionKeg)targeted;

                    if (keg.Held <= 0)
                    {
                        keg.SendLocalizedMessageTo(from, 502228); // There is nothing in the keg to taste!
                    }
                    else
                    {
                        keg.SendLocalizedMessageTo(from, 502229); // You are already familiar with this keg's contents.
                        keg.SendLocalizedMessageTo(from, keg.LabelNumber);
                    }
                }
                else
                {
                    // The target is not food or potion or potion keg.
                    from.SendLocalizedMessage(502820); // That's not something you can taste.
                }
            }
        }
    }
}