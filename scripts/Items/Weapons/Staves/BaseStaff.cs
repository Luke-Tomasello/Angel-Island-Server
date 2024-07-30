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

/* Items/Weapons/Staves/BaseStaff.cs
 * CHANGELOG
 *  4/16/23, Yoar
 *      Staves no longer do stam damage pre-Pub16
 *  3/29/23, Yoar
 *      Merge Pub5 moves with RunUO
 *  3/27/23, Yoar
 *      Moved magic effects to BaseWeapon
 *      Removed IWand interface
 *	1/6/23, Yoar: IWand fix
 *	    WandEffect is now None (= -1) by default
 *	    Grandfathered wands (version 1) have their wand effect removed
 *	1/6/23, Yoar: IWand interface
 *	    BaseStaff now implements IWand
 */

namespace Server.Items
{
    public abstract class BaseStaff : BaseMeleeWeapon
    {
        public override int DefHitSound { get { return 0x233; } }
        public override int DefMissSound { get { return 0x239; } }

        public override SkillName DefSkill { get { return SkillName.Macing; } }
        public override WeaponType DefType { get { return WeaponType.Staff; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Bash2H; } }

        public BaseStaff(int itemID)
            : base(itemID)
        {
        }

        public BaseStaff(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                case 2:
                case 1:
                    {
                        if (version < 3)
                        {
                            MagicEffect = BaseWand.LegacyWandEffect(reader.ReadByte());
                            MagicCharges = reader.ReadInt();
                        }

                        break;
                    }
            }

            if (version == 1 && MagicEffect == MagicItemEffect.Clumsy && MagicCharges == 0)
                MagicEffect = MagicItemEffect.None;
        }

        public override void OnHit(Mobile attacker, Mobile defender)
        {
            base.OnHit(attacker, defender);

            if (PublishInfo.Publish >= 16.0)
                defender.Stam -= Utility.Random(3, 3); // 3-5 points of stamina loss
        }
    }
}