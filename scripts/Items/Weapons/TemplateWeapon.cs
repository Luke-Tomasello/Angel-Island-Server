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

/*	Scripts\Items\Weapons\TemplateWeapon.cs
 *	ChangeLog:
 *	8/6/10, adam
 *		initial creation
 *		used for the dynamic creation of generic non-flippable weapons
 */

namespace Server.Items
{
    public class TemplateWeapon : BaseMeleeWeapon
    {
        public override int OldStrengthReq { get { return 0; } }
        public override int OldSpeed { get { return 30; } }

        public override int OldDieRolls { get { return 1; } }
        public override int OldDieMax { get { return 8; } }
        public override int OldAddConstant { get { return 0; } }

        public override int DefHitSound { get { return -1; } }
        public override int DefMissSound { get { return -1; } }

        public override SkillName DefSkill { get { return SkillName.Wrestling; } }
        public override WeaponType DefType { get { return WeaponType.Fists; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Wrestle; } }

        public TemplateWeapon()
            : base(0)
        {
        }

        public TemplateWeapon(Serial serial)
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