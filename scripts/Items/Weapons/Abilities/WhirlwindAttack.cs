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

/* /sandbox/ai/Scripts/Items/Weapons/Abilities/WhirlwindAttack.cs
 *	ChangeLog :
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 */

using Server.Engines.PartySystem;
using Server.Spells;
using System.Collections;

namespace Server.Items
{
    /// <summary>
    /// A godsend to a warrior surrounded, the Whirlwind Attack allows the fighter to strike at all nearby targets in one mighty spinning swing.
    /// </summary>
    public class WhirlwindAttack : WeaponAbility
    {
        public WhirlwindAttack()
        {
        }

        public override int BaseMana { get { return 15; } }

        public override void OnHit(Mobile attacker, Mobile defender, int damage)
        {
            if (!Validate(attacker))
                return;

            ClearCurrentAbility(attacker);

            Map map = attacker.Map;

            if (map == null)
                return;

            BaseWeapon weapon = attacker.Weapon as BaseWeapon;

            if (weapon == null)
                return;

            if (!CheckMana(attacker, true))
                return;

            attacker.FixedEffect(0x3728, 10, 15);
            attacker.PlaySound(0x2A1);

            ArrayList list = new ArrayList();

            IPooledEnumerable eable = attacker.GetMobilesInRange(1);
            foreach (Mobile m in eable)
                list.Add(m);
            eable.Free();

            Party p = Party.Get(attacker);

            for (int i = 0; i < list.Count; ++i)
            {
                Mobile m = (Mobile)list[i];

                if (m != defender && m != attacker && SpellHelper.ValidIndirectTarget(attacker, m) && (p == null || !p.Contains(m)))
                {
                    if (m == null || m.Deleted || attacker.Deleted || m.Map != attacker.Map || !m.Alive || !attacker.Alive || !attacker.CanSee(m))
                        continue;

                    if (!attacker.InRange(m, weapon.MaxRange))
                        continue;

                    if (attacker.InLOS(m))
                    {
                        attacker.RevealingAction();

                        attacker.SendLocalizedMessage(1060161); // The whirling attack strikes a target!
                        m.SendLocalizedMessage(1060162); // You are struck by the whirling attack and take damage!

                        weapon.OnHit(attacker, m);
                    }
                }
            }
        }
    }
}