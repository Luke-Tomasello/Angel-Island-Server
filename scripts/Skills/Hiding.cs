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

/* Scripts\Skills\Hiding.cs
 *	ChangeLog :
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 */

using Server.Items;
using Server.Multis;
using Server.Network;
using System;

namespace Server.SkillHandlers
{
    public class Hiding
    {
        public static void Initialize()
        {
            SkillInfo.Table[21].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (m.Target != null || m.Spell != null)
            {
                m.SendLocalizedMessage(501238); // You are busy doing something else and cannot hide.
                return TimeSpan.FromSeconds(1.0);
            }


            double bonus = 0.0;

            BaseHouse house = BaseHouse.FindHouseAt(m);

            if (house != null && house.IsFriend(m))
            {
                bonus = 100.0;
            }
            else if (!Core.RuleSets.AOSRules())
            {
                if (house == null)
                    house = BaseHouse.FindHouseAt(new Point3D(m.X - 1, m.Y, 127), m.Map, 16);

                if (house == null)
                    house = BaseHouse.FindHouseAt(new Point3D(m.X + 1, m.Y, 127), m.Map, 16);

                if (house == null)
                    house = BaseHouse.FindHouseAt(new Point3D(m.X, m.Y - 1, 127), m.Map, 16);

                if (house == null)
                    house = BaseHouse.FindHouseAt(new Point3D(m.X, m.Y + 1, 127), m.Map, 16);

                if (house != null)
                    bonus = 50.0;
            }

            int range = 18 - (int)(m.Skills[SkillName.Hiding].Value / 10);

            bool badCombat = (m.Combatant != null && m.InRange(m.Combatant.Location, range) && m.Combatant.InLOS(m));
            bool ok = (!badCombat /*&& m.CheckSkill( SkillName.Hiding, 0.0 - bonus, 100.0 - bonus )*/ );

            if (ok)
            {
                IPooledEnumerable eable = m.GetMobilesInRange(range);
                foreach (Mobile check in eable)
                {
                    if (check.InLOS(m) && check.Combatant == m)
                    {
                        badCombat = true;
                        ok = false;
                        break;
                    }
                }
                eable.Free();

                ok = (!badCombat && m.CheckSkill(SkillName.Hiding, 0.0 - bonus, 100.0 - bonus, contextObj: new object[2]));
            }

            if (badCombat)
            {
                m.RevealingAction();

                m.LocalOverheadMessage(MessageType.Regular, 0x22, 501237); // You can't seem to hide right now.

                return TimeSpan.FromSeconds(1.0);
            }
            else if (m.CheckState(Mobile.ExpirationFlagID.EvilCrim))
            {   // Evils that kill innocents are flagged with a special criminal flag the prevents them from gate/hide

                m.RevealingAction();

                // question(6)
                m.LocalOverheadMessage(MessageType.Regular, 0x22, 501237); // You can't seem to hide right now.

                return TimeSpan.FromSeconds(1.0);
            }
            else
            {
                if (ok)
                {
                    m.Hidden = true;

                    // We'll allow it. Seems like OSI was fixing a logic bug.
                    bool EraException = true;

                    // Publish 15
                    // Players who successfully use their hiding skill while under the effects of an invisibility spell
                    //  will no longer be revealed when the invisibility timer expires.
                    // https://www.uoguide.com/Publish_15
                    if (EraException || PublishInfo.Publish >= 15 || Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
                    {   // spell
                        Spells.Sixth.InvisibilitySpell.RemoveTimer(m);
                        // items
                        Server.Items.MagicEquipment.RemoveTimer(m, MagicEquipEffect.Invisibility);
                    }

                    m.LocalOverheadMessage(MessageType.Regular, 0x1F4, 501240); // You have hidden yourself well.
                }
                else
                {
                    m.RevealingAction();

                    m.LocalOverheadMessage(MessageType.Regular, 0x22, 501241); // You can't seem to hide here.
                }

                return TimeSpan.FromSeconds(10.0);
            }
        }
    }
}