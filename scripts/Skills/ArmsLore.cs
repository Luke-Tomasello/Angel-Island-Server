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

/* Scripts/Skills/ArmsLore.cs
 * ChangeLog :
 *  4/25/23, Yoar
 *      Added weapon corrosion info
 *  11/9/08, Adam
 *      Replace old MaxHits and Hits with MaxHitPoints and HitPoints (RunUO 2.0 compatibility)
 *	10/15/05, erlein
 *		Fixed identification of weapons.
 *	7/13/05, erlein
 *		Moved check to ItemIdentification.cs
 *	7/13/05, erlein
 *		Added check for target being EnchantedItem. Identifies if skill is >=80.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;

namespace Server.SkillHandlers
{
    public class ArmsLore
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.ArmsLore].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTarget();

            m.SendLocalizedMessage(500349); // What item do you wish to get information about?

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(2, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseWeapon)
                {
                    if (from.CheckTargetSkill(SkillName.ArmsLore, targeted, 0, 100, new object[2] /*contextObj*/))
                    {
                        BaseWeapon weap = (BaseWeapon)targeted;

                        if (weap.MaxHitPoints != 0)
                        {
                            int hp = (int)((weap.HitPoints / (double)weap.MaxHitPoints) * 10);

                            if (hp < 0)
                                hp = 0;
                            else if (hp > 9)
                                hp = 9;

                            from.SendLocalizedMessage(1038285 + hp);
                        }

                        /* erl: this doesn't work :/

							int damage = (weap.MaxDamage + weap.MinDamage) / 2;

							if ( damage < 3 )
								damage = 0;
							else if ( damage < 6 )
								damage = 1;
							else if ( damage < 11 )
								damage = 2;
							else if ( damage < 16 )
								damage = 3;
							else if ( damage < 21 )
								damage = 4;
							else if ( damage < 26 )
								damage = 5;
							else
								damage = 6;
						*/

                        int damage = 0;

                        switch (weap.Quality)
                        {
                            case WeaponQuality.Low: damage = 0; break;
                            case WeaponQuality.Regular: damage = 1; break;
                            case WeaponQuality.Exceptional: damage = 4; break;
                        }

                        switch (weap.DamageLevel)
                        {
                            case WeaponDamageLevel.Ruin: damage = 2; break;
                            case WeaponDamageLevel.Might: damage = 3; break;
                            case WeaponDamageLevel.Force: damage = 4; break;
                            case WeaponDamageLevel.Power: damage = 5; break;
                            case WeaponDamageLevel.Vanquishing: damage = 6; break;
                        }

                        int hand = (weap.Layer == Layer.OneHanded ? 0 : 1);

                        WeaponType type = weap.Type;

                        if (type == WeaponType.Ranged)
                            from.SendLocalizedMessage(1038224 + (damage * 9));
                        else if (type == WeaponType.Piercing)
                            from.SendLocalizedMessage(1038218 + hand + (damage * 9));
                        else if (type == WeaponType.Slashing)
                            from.SendLocalizedMessage(1038220 + hand + (damage * 9));
                        else if (type == WeaponType.Bashing)
                            from.SendLocalizedMessage(1038222 + hand + (damage * 9));
                        else
                            from.SendLocalizedMessage(1038216 + hand + (damage * 9));

                        if (!Core.RuleSets.AOSRules() && weap.Corrosion != 0)
                        {
                            if (weap.Corrosion == 1)
                                from.SendLocalizedMessage(1038280); // It looks like this blade has a light corrosive element on it.
                            else if (weap.Corrosion == 2)
                                from.SendLocalizedMessage(1038281); // It looks like this blade has a corrosive element on it.
                            else if (weap.Corrosion == 3)
                                from.SendLocalizedMessage(1038282); // It looks as if this blade is covered with an acidic substance.
                            else if (weap.Corrosion == 4)
                                from.SendLocalizedMessage(1038283); // It looks as if a substance is eating away pieces of the blade.
                            else
                                from.SendLocalizedMessage(1038279); // It looks like acid is dripping from what is left of the blade.
                        }
                        else if (weap.Poison != null && weap.PoisonCharges > 0)
                            from.SendLocalizedMessage(1038284); // It appears to have poison smeared on it.
                    }
                    else
                    {
                        from.SendLocalizedMessage(500353); // You are not certain...
                    }
                }
                else if (targeted is BaseArmor)
                {
                    if (from.CheckTargetSkill(SkillName.ArmsLore, targeted, 0, 100, new object[2] /*contextObj*/))
                    {
                        BaseArmor arm = (BaseArmor)targeted;

                        if (arm.MaxHitPoints != 0)
                        {
                            int hp = (int)((arm.HitPoints / (double)arm.MaxHitPoints) * 10);

                            if (hp < 0)
                                hp = 0;
                            else if (hp > 9)
                                hp = 9;

                            from.SendLocalizedMessage(1038285 + hp);
                        }

                        if (arm.BaseArmorRating < 1)
                            from.SendLocalizedMessage(1038295); // This armor offers no defense against attackers.
                        else if (arm.BaseArmorRating < 6)
                            from.SendLocalizedMessage(1038296); // This armor provides almost no protection.
                        else if (arm.BaseArmorRating < 11)
                            from.SendLocalizedMessage(1038297); // This armor provides very little protection.
                        else if (arm.BaseArmorRating < 16)
                            from.SendLocalizedMessage(1038298); // This armor offers some protection against blows.
                        else if (arm.BaseArmorRating < 21)
                            from.SendLocalizedMessage(1038299); // This armor serves as sturdy protection.
                        else if (arm.BaseArmorRating < 26)
                            from.SendLocalizedMessage(1038300); // This armor is a superior defense against attack.
                        else if (arm.BaseArmorRating < 31)
                            from.SendLocalizedMessage(1038301); // This armor offers excellent protection.
                        else
                            from.SendLocalizedMessage(1038302); // This armor is superbly crafted to provide maximum protection.
                    }
                    else
                    {
                        from.SendLocalizedMessage(500353); // You are not certain...
                    }
                }
                else if (targeted is SwampDragon && ((SwampDragon)targeted).HasBarding)
                {
                    SwampDragon pet = (SwampDragon)targeted;

                    if (from.CheckTargetSkill(SkillName.ArmsLore, targeted, 0, 100, new object[2] { targeted, null } /*contextObj*/))
                    {
                        int perc = (4 * pet.BardingHP) / pet.BardingMaxHP;

                        if (perc < 0)
                            perc = 0;
                        else if (perc > 4)
                            perc = 4;

                        pet.PrivateOverheadMessage(MessageType.Regular, 0x3B2, 1053021 - perc, from.NetState);
                    }
                    else
                    {
                        from.SendLocalizedMessage(500353); // You are not certain...
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500352); // This is neither weapon nor armor.
                }
            }
        }
    }
}