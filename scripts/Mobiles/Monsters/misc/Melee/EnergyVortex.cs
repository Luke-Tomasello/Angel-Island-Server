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

/* Scripts/Mobiles/Monsters/Misc/Melee/Energyvortex.cs
 * ChangeLog
 *  8/30/2023, Adam (attack master)
 *      Add FightMode.Master which directs AcquireFocusMob to allow attacking the master.
 *      Both Energyvortex and BladeSpirits have this flag. Keep in mind, both of these summons
 *      will prefer higher INT targets (Energyvortex) or higher STR targets (BladeSpirits) regardless of their master.
 *  5/19/23, Adam
 *      Add poison for SiegeStyleRules().
 *  11/8/22, Adam
 *      Reduce damage for SiegeStyleRules().
 *	2/9/11, adam
 *		Add Llama vortices
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  4/27/05, Kit
 *		Adjusted dispell difficulty
 *  4/27/05, Kit
 *		Adapted to use new ev/bs logic
 *  7,17,04, Old Salty
 * 		Changed ActiveSpeed to make EV's a little slower.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/7/04, mith
 *		Increased Damage from 14-17 to 25-30.
 */

/* Era Info
 *  Also, EVs here often do take a while to evaluate their targets, but if you cast it on a balron or something, and the balron doesn't have a target, then the Balron will insta-target the EV. If you cast it on something that currently has a target, and don't drop the EV on the right tile, often the EV will just walk around for a few seconds with no target (which can end up with the EV chasing you down)
 *  https://forums.uosecondage.com/viewtopic.php?t=19395
 *  You are probably casting it on the wrong types of monsters.
 *  If you cast an EV on a ogre, the EV will sit there and twiddle
 *  its thumbs, or come after you.. If you cast an EV on a dragon,
 *  it will rip the dragon to shreds if the dragon does not dispel it
 *  first. EVs thrive on intelligence, so they don't work near as well
 *  on creatures like trolls and ogres.
 *  https://groups.google.com/g/rec.games.computer.ultima.online/c/-t5hH5Balnc?pli=1
 *  invis
 *  Added on 2002-05-27 by Anonymous
 *  These things are great for when you're in a spot of bother but for god sake invis when you cast them or they'll come after you and you'll have even bigger problems on your hands :)
 *  https://groups.google.com/g/rec.games.computer.ultima.online/c/-t5hH5Balnc?pli=1
 */

using static Server.Utility;

namespace Server.Mobiles
{
    [CorpseName("an energy vortex corpse")]
    public class EnergyVortex : BaseCreature
    {
        public override bool DeleteCorpseOnDeath { get { return Summoned; } }
        public override bool AlwaysMurderer { get { return true; } } // Or Llama vortices will appear gray.

        public override double DispelDifficulty { get { return Core.RuleSets.AngelIslandRules() ? 56.0 : 80.0; } }
        public override double DispelFocus { get { return Core.RuleSets.AngelIslandRules() ? 45.0 : 20; } }

        [Constructable]
        public EnergyVortex()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest | FightMode.Int | FightMode.NoAllegiance, 6, 1, 0.199, 0.350)
        {
            Name = "an energy vortex";
            if (0.02 > Utility.RandomDouble()) // Tested on OSI, but is this right? Who knows.
            {
                // Llama vortex!
                Body = 0xDC;
                Hue = 0x76;
            }
            else
            {
                Body = 164;
            }

            SetStr(200);
            SetDex(200);
            SetInt(100);

            SetHits((Core.SE) ? 140 : 70);
            SetStam(250);
            SetMana(0);

            // The spells Blade Spirits and Energy Vortex have been weakened significantly
            //  https://www.uoguide.com/Siege_Perilous
            //  Don't know what the value should be, here's a guess
            if (Core.RuleSets.SiegeStyleRules())
                SetDamage(Decrease(number: 14, percentage: 10), Decrease(number: 17, percentage: 10));
            else
                // standard OSI
                SetDamage(14, 17);

            if (!Core.RuleSets.AngelIslandRules())
                SetSkill(SkillName.Poisoning, 60.1, 80.0);
            SetSkill(SkillName.MagicResist, 99.9);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 120.0);

            Fame = 0;
            Karma = 0;

            VirtualArmor = 40;
            ControlSlots = (Core.SE) ? 2 : 1;

        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }
        public override Poison HitPoison
        {
            get
            {
                return Core.RuleSets.AngelIslandRules() ? null :
                    // The spells Blade Spirits and Energy Vortex have been weakened significantly
                    //  https://www.uoguide.com/Siege_Perilous
                    Core.RuleSets.SiegeStyleRules() ? Poison.Greater : Poison.Deadly;
            }
        }
        public override bool SpeedOverrideOK { get { return true; } }

        public override int GetAngerSound()
        {
            return 0x15;
        }

        public override int GetAttackSound()
        {
            return 0x28;
        }

        public EnergyVortex(Serial serial)
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

            if (BaseSoundID == 263)
                BaseSoundID = 0;
        }
    }
}