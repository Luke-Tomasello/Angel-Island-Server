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

/* Scripts/Items/Weapons/Knives/AIStinger.cs
 * Created 3/28/04 by mith, copied from Dagger.cs
 * ChangeLog
 *	10/22/04 - Pix
 *		Removed checks for CampfireRegion since camping has changed and that class no longer exists.
 *	9/24/04, Pix
 *		Removed the check is Serialize for changing the blessedness of a Stinger depending on Region.
 *	9/2/04, Pix
 *		Made it so AIStingers remove their blessedness when not in AngelIsland region.
 *	5/19/04, mith
 *		Renamed from "a special dagger" to "an Island Stinger".
 *	4/12/04 mith
 *		Modified Min/Max HPs and Min/Max Damage to use dynamic values defined in CoreAI.
 *	3/30/04 mith
 *		Made dagger blessed upon creation.
 *	3/29/04 mith
 *		Added GetUsedSkill(), overrides same method from BaseWeapon, allows us to have a UseBestSkill weapon without enabling AoS.
 *		Changed damage from range of 3-15 to 8-12. Request others suggest damage range.
 *		Added Name property in Constructable AIStinger: "a special dagger".
 */

namespace Server.Items
{
    [FlipableAttribute(0xF52, 0xF51)]
    public class AIStinger : BaseKnife
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.InfectiousStrike; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ShadowStrike; } }

        //		public override int AosStrengthReq{ get{ return 10; } }
        //		public override int AosMinDamage{ get{ return 10; } }
        //		public override int AosMaxDamage{ get{ return 11; } }
        //		public override int AosSpeed{ get{ return 56; } }
        //
        public override int OldMinDamage { get { return CoreAI.StingerMinDamage; } }
        public override int OldMaxDamage { get { return CoreAI.StingerMaxDamage; } }
        public override int OldStrengthReq { get { return 1; } }
        public override int OldSpeed { get { return 55; } }

        public override int OldDieRolls { get { return 3; } }
        public override int OldDieMax { get { return 5; } }
        public override int OldAddConstant { get { return 0; } }

        public override int InitMinHits { get { return CoreAI.StingerMinHP; } }
        public override int InitMaxHits { get { return CoreAI.StingerMaxHP; } }

        public override SkillName DefSkill { get { return SkillName.Fencing; } }
        public override SkillName GetUsedSkill(Mobile m, bool checkSkillAttrs)
        {
            // Copied from BaseWeapon.GetUsedSkill
            // This allows us to have a Mage/UseBestSkill weapon
            // Takes the greater of swords, fencing, macing, wrestling, magery, or archery
            //	and uses it for the applicable weapon skill of the weapon.
            SkillName sk;

            double swrd = m.Skills[SkillName.Swords].Value;
            double fenc = m.Skills[SkillName.Fencing].Value;
            double arch = m.Skills[SkillName.Archery].Value;
            double mcng = m.Skills[SkillName.Macing].Value;
            double mage = m.Skills[SkillName.Magery].Value;
            double wres = m.Skills[SkillName.Wrestling].Value;
            double val;

            sk = SkillName.Swords;
            val = swrd;

            if (fenc > val) { sk = SkillName.Fencing; val = fenc; }
            if (arch > val) { sk = SkillName.Archery; val = arch; }
            if (mcng > val) { sk = SkillName.Macing; val = mcng; }
            if (wres > val) { sk = SkillName.Wrestling; val = wres; }
            if (mage > val) { sk = SkillName.Magery; val = mage; }

            return sk;
        }

        public override WeaponType DefType { get { return WeaponType.Piercing; } }
        public override WeaponAnimation DefAnimation { get { return WeaponAnimation.Pierce1H; } }

        [Constructable]
        public AIStinger()
            : base(0xF52)
        {
            Name = "an Island Stinger";
            Weight = 1.0;
            LootType = LootType.Blessed;
        }

        public AIStinger(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            //make sure the stinger isn't blessed if it's not on AI
            /*
			if( Map != Map.Internal ) //only check this when we're not logged out
			{
				if( !IsOnAngelIsland() )
				{
					this.LootType = LootType.Regular;
				}
				else
				{
					this.LootType = LootType.Blessed;
				}
			}
			*/

            //make sure that it's not poisoned.
            this.PoisonCharges = 0;

            //do the serialize stuff, now that we've done all our checks

            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        private bool IsOnAngelIsland()
        {
            if (RootParent is Mobile)
            {
                Mobile m = (Mobile)RootParent;
                Region reg = m.Region;
                if (reg != null && reg.IsAngelIslandRules)
                {
                    return true;
                }
            }
            else if (RootParent is Backpack)
            {
                Backpack pack = (Backpack)RootParent;
                if (pack.Parent is Mobiles.PlayerMobile)
                {
                    Region reg = ((Mobiles.PlayerMobile)pack.Parent).Region;
                    if (reg != null && reg.IsAngelIslandRules)
                    {
                        return true;
                    }
                }
            }
            else if (RootParent is Item)
            {
                Item item = (Item)RootParent;
                Region reg = Region.Find(item.Location, item.Map);
                if (reg != null && reg.IsAngelIslandRules)
                {
                    return true;
                }
            }

            return false;
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}