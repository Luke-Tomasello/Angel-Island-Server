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

/* Scripts/Mobiles/Monsters/Misc/Melee/BladeSpirits.cs
 * ChangeLog
 *  8/30/2023, Adam (attack master)
 *      Add FightMode.Master which directs AcquireFocusMob to allow attacking the master.
 *      Both Energyvortex and BladeSpirits have this flag. Keep in mind, both of these summons
 *      will prefer higher INT targets (Energyvortex) or higher STR targets (BladeSpirits) regardless of their master.
 *  5/19/23, Adam
 *      Add poison for SiegeStyleRules().
 *  11/8/22, Adam
 *      Reduce damage for SiegeStyleRules().
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 8 lines removed.
 * 4/27/05, Kit
 *	Adjusted dispel difficulty
 *  4/27/05, Kit
 *	Adapted to use new ev/bs logic
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

/* Era Info
 *  Also, EVs here often do take a while to evaluate their targets, but if you cast it on a balron or something, and the balron doesn't have a target, 
 *      then the Balron will insta-target the EV. If you cast it on something that currently has a target, and don't drop the EV on the right tile, 
 *      often the EV will just walk around for a few seconds with no target (which can end up with the EV chasing you down)
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
 *  These things are great for when you're in a spot of bother but for god sake invis when you cast them or they'll come after you and you'll 
 *      have even bigger problems on your hands :)
 *  https://groups.google.com/g/rec.games.computer.ultima.online/c/-t5hH5Balnc?pli=1
 */

/* Blade Spirits
 *  Also, on OSI blade spirits would attack the target on screen with the most dexterity, while EV's would go for highest intelligence. 
 *  I dunno if that's the case here or not. I don't think it is, though. I think it's just closest target
 *  https://www.uoforum.com/threads/ev-s-and-blade-spirit.44788/
 *  Summons a whirling pillar of blades that selects a Target to attack based off its combat strength and proximity.
 *  It attack any mobile around base on the amount of Strength and Tactics! 
 *  https://uodemiseguide.spokland.com/character/skill/magery/blade_spirits
 *  ---
 *  Adam's Notes: Historically we have used DEX, uoforum agrees with this, so this is how we will go.
 *  Also, it appears to attack any mobiles, not filtered by party/guild, etc
 */
using static Server.Utility;

namespace Server.Mobiles
{
    [CorpseName("a blade spirit corpse")]
    public class BladeSpirits : BaseCreature
    {
        public override bool DeleteCorpseOnDeath { get { return Core.RuleSets.AOSRules(); } }
        public override bool IsHouseSummonable { get { return true; } }

        public override double DispelDifficulty { get { return Core.RuleSets.AngelIslandRules() ? 56.0 : 0; } }
        public override double DispelFocus { get { return Core.RuleSets.AngelIslandRules() ? 45.0 : 20.0; } }

        [Constructable]
        public BladeSpirits()
            /*  Also, on OSI blade spirits would attack the target on screen with the most dexterity, while EV's would go for highest intelligence. 
             *  I dunno if that's the case here or not. I don't think it is, though.I think it's just closest target
             *  https://www.uoforum.com/threads/ev-s-and-blade-spirit.44788/
             */
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest | FightMode.Dex | FightMode.NoAllegiance, 10, 1, 0.2, 0.4)
        {
            Name = "a blade spirit";
            Body = 574;

            SetStr(150);
            SetDex(150);
            SetInt(100);

            SetHits((Core.SE) ? 160 : 80);
            SetStam(250);
            SetMana(0);

            // The spells Blade Spirits and Energy Vortex have been weakened significantly
            //  https://www.uoguide.com/Siege_Perilous
            //  Don't know what the value should be, here's a guess
            if (Core.RuleSets.SiegeStyleRules())
                SetDamage(Decrease(number: 10, percentage: 10), Decrease(number: 14, percentage: 10));
            else
                // standard OSI
                SetDamage(10, 14);

            if (!Core.RuleSets.AngelIslandRules())
                SetSkill(SkillName.Poisoning, 60.1, 80.0);
            SetSkill(SkillName.MagicResist, 70.0);
            SetSkill(SkillName.Tactics, 90.0);
            SetSkill(SkillName.Wrestling, 90.0);

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
                   Core.RuleSets.SiegeStyleRules() ? Poison.Regular : Poison.Greater;
            }
        }

        public override int GetAngerSound()
        {
            return 0x23A;
        }

        public override int GetAttackSound()
        {
            return 0x3B8;
        }

        public override int GetHurtSound()
        {
            return 0x23A;
        }

        public BladeSpirits(Serial serial)
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