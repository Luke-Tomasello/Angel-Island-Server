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

/* Scripts\Skills\EvalInt.cs
 * ChangeLog
 *	8/31/07, Adam
 *		Change CheckTargetSkill() to check against a max skill of 100 instead of 120
 * 	8/13/04, mith
 *		InternalTarget.OnTarget(): modified formula to determine margin of error when using as targetted skill.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class EvalInt
    {
        public static void Initialize()
        {
            SkillInfo.Table[16].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new EvalInt.InternalTarget();

            m.SendLocalizedMessage(500906); // What do you wish to evaluate?

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (from == targeted)
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500910); // Hmm, that person looks really silly.
                }
                else if (targeted is TownCrier)
                {
                    ((TownCrier)targeted).PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500907, from.NetState); // He looks smart enough to remember the news.  Ask him about it.
                }
                else if (targeted is BaseVendor && ((BaseVendor)targeted).IsInvulnerable)
                {
                    ((BaseVendor)targeted).PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500909, from.NetState); // That person could probably calculate the cost of what you buy from them.
                }
                else if (targeted is Mobile)
                {
                    Mobile targ = (Mobile)targeted;

                    int marginOfError = (int)(from.Skills[SkillName.EvalInt].Value / 10);
                    if (marginOfError > 0)
                        marginOfError = (int)25 / marginOfError;
                    else
                        marginOfError = 25;

                    int intel = targ.Int + Utility.RandomMinMax(-marginOfError, +marginOfError);
                    int mana = ((targ.Mana * 100) / Math.Max(targ.ManaMax, 1)) + Utility.RandomMinMax(-marginOfError, +marginOfError);

                    int intMod = intel / 10;
                    int mnMod = mana / 10;

                    if (intMod > 10) intMod = 10;
                    else if (intMod < 0) intMod = 0;

                    if (mnMod > 10) mnMod = 10;
                    else if (mnMod < 0) mnMod = 0;

                    int body;

                    if (targ.Body.IsHuman)
                        body = targ.Female ? 11 : 0;
                    else
                        body = 22;

                    if (from.CheckTargetSkill(SkillName.EvalInt, targ, 0.0, 100.0, new object[2] { targ, null }/*contextObj*/))
                    {
                        targ.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1038169 + intMod + body, from.NetState); // He/She/It looks [slighly less intelligent than a rock.]  [Of Average intellect] [etc...]

                        if (from.Skills[SkillName.EvalInt].Base >= 76.0)
                            targ.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1038202 + mnMod, from.NetState); // That being is at [10,20,...] percent mental strength.
                    }
                    else
                    {
                        targ.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1038166 + (body / 11), from.NetState); // You cannot judge his/her/its mental abilities.
                    }
                }
                else if (targeted is Item)
                {
                    ((Item)targeted).SendLocalizedMessageTo(from, 500908, ""); // It looks smarter than a rock, but dumber than a piece of wood.
                }
            }
        }
    }
}