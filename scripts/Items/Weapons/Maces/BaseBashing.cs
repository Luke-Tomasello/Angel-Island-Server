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

/* Scripts\Items\Weapons\Maces\BaseBashing.cs
 * ChangeLog:
 *  3/29/23, Yoar
 *      Merge Pub5 moves with RunUO
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 * 6/10/10, Adam
 *		Port OnHit logic to that of RunUO 2.0
 *			Bashing Damage is already handled BaseArmor.OnHit
 * 2/17/05 - Pix
 * 		Fixed another cast!
 * 	2/16/05 - Pix
 * 		Fixed BaseArmor cast.
 * 	4/22/04 Changes by smerX
 * 		Added Armor.HitPoints damage
 */

using Server.Targeting;

namespace Server.Items
{
    public abstract class BaseBashing : BaseMeleeWeapon
    {
        public override int DefHitSound { get { return 0x233; } }
        public override int DefMissSound { get { return 0x239; } }

        public override SkillName DefSkill { get { return SkillName.Macing; } }
        public override WeaponType DefType { get { return WeaponType.Bashing; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Bash1H; } }

        public BaseBashing(int itemID)
            : base(itemID)
        {
        }

        public BaseBashing(Serial serial)
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

        public override void OnHit(Mobile attacker, Mobile defender)
        {
            base.OnHit(attacker, defender);

            if (defender != null && defender.BlockDamage == false)
                defender.Stam -= Utility.Random(3, 3); // 3-5 points of stamina loss

            // Vile blade	Velgo Reyam	This power actually works on any weapon (including maces and bows). 
            // It gives the weapon a few charges of powerful (level 5 !!) poison . Once a weapon has been made vile, 
            //	it can never again be used by a hero . 
            if (Core.RuleSets.SiegeStyleRules())
                if (!Core.RuleSets.AOSRules() && Poison != null && PoisonCharges > 0)
                {
                    --PoisonCharges;

                    if (CheckHitPoison(attacker))
                    {
                        defender.ApplyPoison(attacker, MutatePoison(attacker, Poison));

                        OnHitPoison(attacker);
                    }
                }

        }

        public override double GetBaseDamage(Mobile attacker, Mobile defender)
        {
            double damage = base.GetBaseDamage(attacker, defender);
#if false
            // see BaseWeapon
            if (false && DoPub5Move(attacker, defender) && Engines.ConPVP.DuelContext.AllowSpecialAbility(attacker, "Crushing Blow", false))
            {
                damage *= 1.5;

                attacker.SendMessage("You deliver a crushing blow!"); // Is this not localized?
                attacker.PlaySound(0x11C);
            }
#endif
            return damage;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!HitMagicEffect && MagicEffect != MagicItemEffect.None)
            {
                base.OnDoubleClick(from);
            }
            else
            {
                from.SendLocalizedMessage(1010018); // What do you want to use this item on?

                from.Target = new MaceTarget(this);
            }
        }

        public class MaceTarget : Target
        {
            private Item m_Item;

            public MaceTarget(Item item)
                : base(2, false, TargetFlags.None)
            {
                m_Item = item;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (Township.TownshipItemHelper.CheckDamageTarget(from, targeted))
                    return;

                from.SendMessage("You can't use a mace on that.");
            }
        }
    }
}