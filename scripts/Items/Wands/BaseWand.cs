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

/* Items/Wands/BaseWand.cs
 * CHANGE LOG
 *  5/30/23, Yoar
 *      Added damage, speed and hits stats
 *      Wands can now be used as macing weapons
 *  3/27/23, Yoar
 *      Moved magic effects to BaseWeapon
 *      Removed IWand interface
 *	1/6/23, Yoar: IWand interface
 *	    Added IWand interface and WandHelper helper class
 *	    BaseWand now implements IWand
 *	    Moved wand use logic to WandHelper
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 * 05/11/2004 - Pulse
 *	Corrected the OnSingleClick method to display the proper spell type for the wand.  
 */

using System;

namespace Server.Items
{
    /* Charged abilities have a limited amount of charges, and function as the spell of the same-name. 
		Armour, clothing and jewelry function automatically when worn. They may contain one of the following effects � 
		*Clumsiness 
		*Feeblemindedness 
		*Weakness 
		*Agility 
		*Cunning 
		*Strength 
		*Protection 
		*Curses 
		*Night Eyes 
		*Blessings 
		*Spell Reflection 
		*Invisibility 
		*Protection ((Armour only)) 
		*Teleportation ((Rings only)) 
	 * http://forums.uosecondage.com/viewtopic.php?f=9&t=4150
	 * "Night Eyes" supported here:
	 * http://uo.stratics.com/php-bin/show_content.php?content=31536
	 */
    [Obsolete]
    public enum WandEffect
    {
        None = -1,

        Clumsiness,
        Identification,
        Healing,
        Feeblemindedness,
        Weakness,
        MagicArrow,
        Harming,
        Fireball,
        GreaterHealing,
        Lightning,
        ManaDraining,

        // 12/7/2001, Adam: new non PvP Wands
        NightSight,
        CreateFood,
        MagicLock,
        Unlock
    }

    public abstract class BaseWand : BaseBashing
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.Dismount; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.Disarm; } }

        public override int AosStrengthReq { get { return 5; } }
        public override int AosMinDamage { get { return 9; } }
        public override int AosMaxDamage { get { return 11; } }
        public override int AosSpeed { get { return 40; } }

        public override int OldStrengthReq { get { return 0; } }
        public override int OldMinDamage { get { return 2; } }
        public override int OldMaxDamage { get { return 6; } }
        public override int OldSpeed { get { return 35; } }

        public override int InitMinHits { get { return 31; } }
        public override int InitMaxHits { get { return 110; } }

        public override bool HitMagicEffect { get { return false; } } // do magic on double-click rather than on hit

        [Obsolete]
        public BaseWand(WandEffect effect, int minCharges, int maxCharges)
            : this(LegacyWandEffect((int)effect), minCharges, maxCharges)
        {
        }

        public BaseWand(MagicItemEffect effect, int minCharges, int maxCharges)
            : base(Utility.RandomList(0xDF2, 0xDF3, 0xDF4, 0xDF5))
        {
            MagicEffect = effect;
            MagicCharges = Utility.RandomMinMax(minCharges, maxCharges);
        }

        public BaseWand(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        if (version < 1)
                        {
                            MagicEffect = LegacyWandEffect(reader.ReadInt());
                            MagicCharges = reader.ReadInt();
                        }

                        break;
                    }
            }
        }

        public static MagicItemEffect LegacyWandEffect(int index)
        {
            switch (index)
            {
                case 0: return MagicItemEffect.Clumsy;
                case 1: return MagicItemEffect.Identification;
                case 2: return MagicItemEffect.Heal;
                case 3: return MagicItemEffect.Feeblemind;
                case 4: return MagicItemEffect.Weaken;
                case 5: return MagicItemEffect.MagicArrow;
                case 6: return MagicItemEffect.Harm;
                case 7: return MagicItemEffect.Fireball;
                case 8: return MagicItemEffect.GreaterHeal;
                case 9: return MagicItemEffect.Lightning;
                case 10: return MagicItemEffect.ManaDrain;

                case 11: return MagicItemEffect.NightSight;
                case 12: return MagicItemEffect.CreateFood;
                case 13: return MagicItemEffect.MagicLock;
                case 14: return MagicItemEffect.Unlock;
            }

            return MagicItemEffect.None;
        }
    }
}