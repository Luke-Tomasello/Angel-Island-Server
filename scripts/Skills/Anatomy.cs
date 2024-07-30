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

/* ChangeLog
 *	8/13/04, mith
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
    public class Anatomy
    {
        public static void Initialize()
        {
            SkillInfo.Table[1].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new Anatomy.InternalTarget();

            m.SendLocalizedMessage(500321); // Whom shall I examine?

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
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 500324); // You know yourself quite well enough already.
                }
                else if (targeted is TownCrier)
                {
                    ((TownCrier)targeted).PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500322, from.NetState); // This person looks fine to me, though he may have some news...
                }
                else if (targeted is BaseVendor && ((BaseVendor)targeted).IsInvulnerable)
                {
                    ((BaseVendor)targeted).PrivateOverheadMessage(MessageType.Regular, 0x3B2, 500326, from.NetState); // That can not be inspected.
                }
                else if (targeted is Mobile)
                {
                    Mobile targ = (Mobile)targeted;

                    int marginOfError = (int)(from.Skills[SkillName.Anatomy].Value / 10);
                    if (marginOfError > 0)
                        marginOfError = (int)25 / marginOfError;
                    else
                        marginOfError = 25;

                    int str = targ.Str + Utility.RandomMinMax(-marginOfError, +marginOfError);
                    int dex = targ.Dex + Utility.RandomMinMax(-marginOfError, +marginOfError);
                    int stm = ((targ.Stam * 100) / Math.Max(targ.StamMax, 1)) + Utility.RandomMinMax(-marginOfError, +marginOfError);

                    int strMod = str / 10;
                    int dexMod = dex / 10;
                    int stmMod = stm / 10;

                    if (strMod < 0) strMod = 0;
                    else if (strMod > 10) strMod = 10;

                    if (dexMod < 0) dexMod = 0;
                    else if (dexMod > 10) dexMod = 10;

                    if (stmMod > 10) stmMod = 10;
                    else if (stmMod < 0) stmMod = 0;

                    if (from.CheckTargetSkill(SkillName.Anatomy, targ, 0, 100, new object[2] { targ, null }/*contextObj*/))
                    {
                        targ.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1038045 + (strMod * 11) + dexMod, from.NetState); // That looks [strong] and [dexterous].

                        if (from.Skills[SkillName.Anatomy].Base >= 65.0)
                            targ.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1038303 + stmMod, from.NetState); // That being is at [10,20,...] percent endurance.
                    }
                    else
                    {
                        targ.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1042666, from.NetState); // You can not quite get a sense of their physical characteristics.
                    }
                }
                else if (targeted is Item)
                {
                    ((Item)targeted).SendLocalizedMessageTo(from, 500323, ""); // Only living things have anatomies!
                }
            }
        }
    }
}