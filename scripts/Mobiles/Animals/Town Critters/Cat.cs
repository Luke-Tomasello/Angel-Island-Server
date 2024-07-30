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

/* ./Scripts/Mobiles/Animals/Town Critters/Cat.cs
 *	ChangeLog :
 *	11/27/21, Adam (Mouser)
 *	    Mouser kills rats nearby that have been tamed and abandoned. 
 *	    Some players do this in mass quanties at places like WBB... Mouser is her to help
 *	    Description.
 *	    Looks like a tame becauseof name hue, but is not
 *	    Every 5 minutes picks a new target
 *	    Cannot be herded away
 *	    If led away and trapped, I will respawn !
 *	    I check (rat.Owners.Count > 0) to determine if the rat was previously tame
 *	    I make sure a Player is around to see this.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 2 lines removed.
*/

using System;

namespace Server.Mobiles
{
    [CorpseName("a cat corpse")]
    [TypeAlias("Server.Mobiles.Housecat")]
    public class Cat : BaseCreature
    {
        [Constructable]
        public Cat()
            : base(AIType.AI_Animal, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            Name = "a cat";
            Body = 0xC9;
            Hue = Utility.RandomAnimalHue();
            BaseSoundID = 0x69;

            SetStr(9);
            SetDex(35);
            SetInt(5);

            SetHits(6);
            SetMana(0);

            SetDamage(1);

            SetSkill(SkillName.MagicResist, 5.0);
            SetSkill(SkillName.Tactics, 4.0);
            SetSkill(SkillName.Wrestling, 5.0);

            Fame = 0;
            Karma = 150;

            VirtualArmor = 8;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = -0.9;
        }

        public override int Meat { get { return 1; } }
        public override FoodType FavoriteFood { get { return FoodType.Meat | FoodType.Fish; } }
        public override PackInstinct PackInstinct { get { return PackInstinct.Feline; } }

        public Cat(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }

    public class Mouser : Cat
    {
        [Constructable]
        public Mouser()
            : base()
        {
            AI = AIType.AI_Melee;
            Name = NameList.RandomName("cat");
            Body = 0xC9;
            Hue = Utility.RandomAnimalHue();
            BaseSoundID = 0x69;

            SetStr(88);
            SetDex(88);
            SetInt(49);

            SetHits(88);

            SetDamage(3);   // a rat is 10 HP, so we need to make it a fight!

            SetSkill(SkillName.Anatomy, 88, 99.1);
            SetSkill(SkillName.Tactics, 88, 99.1);
            SetSkill(SkillName.Wrestling, 88, 99.1);

            VirtualArmor = 88;

            Tamable = false;
            BardImmune = true;
            HerdingImmune = true;
            NameHue = Notoriety.GetHue(Notoriety.Innocent);
        }

        private DateTime m_lastThink = DateTime.UtcNow;
        private bool m_awayFromHome = false;
        public override void OnThink()
        {
            base.OnThink();

            if (DateTime.UtcNow > m_lastThink)
            {
                Rat target = null;      // who we will attack
                bool witness = false;   // make sure someone is around to see this
                m_lastThink = DateTime.UtcNow + TimeSpan.FromMinutes(5);
                IPooledEnumerable eable = this.GetMobilesInRange(RangePerception);
                foreach (Mobile m in eable)
                {
                    if (m is Rat rat)
                        // if we have a target and we're not fighting, and rat's not controlled, and was previously tame
                        if (target == null && Combatant == null && rat.Controlled == false && rat.Owners != null && rat.Owners.Count > 0)
                            target = rat;

                    if (m is PlayerMobile pm)
                        if (pm.AccessLevel == AccessLevel.Player && pm.NetState != null)
                            witness = true;

                    if (target != null && witness == true)
                        break;  // we got everything we need
                }
                eable.Free();

                // attack!
                if (target != null && witness == true)
                {
                    PreferredFocus = target;
                    this.DoHarmful(target);
                    target.DoHarmful(this);
                }

                // lets see if we've been trapped by some player away from our home
                if (Home != Point3D.Zero && GetDistanceToSqrt(Home) > RangeHome)
                {
                    if (m_awayFromHome == true)
                    {   // we've been away for 5 minutes... time to take some action!
                        Say("Meow!");
                        if (Spawner != null)
                            Spawner.Respawn();
                    }
                    else
                        m_awayFromHome = true;
                }
            }

        }
        public Mouser(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}