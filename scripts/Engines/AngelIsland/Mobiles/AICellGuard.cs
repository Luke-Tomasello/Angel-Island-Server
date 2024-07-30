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

/* Scripts/Mobiles/Gaurds/AICellGuard.cs
 * Created 4/1/04 by mith
 * ChangeLog
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 1 lines removed.
 *  7/21/04, Adam
 *		1. Redo the setting of skills and setting of Damage 
 *  7/17/04, Adam
 *		1. Add NightSightScroll to drop
 *		2. Replace MindBlastScroll with FireballScroll
 * 4/12/04 mith
 *	Converted stats/skills to use dynamic values defined in CoreAI.
 * 4/10/04 changes by mith
 *	Added bag of reagents and scrolls to loot.
 * 4/1/04 changes by mith
 *	Changed starting skills to be from a range of 70-80 rather than flat 75.0.
 */
using Server.Items;

namespace Server.Mobiles
{
    public class AICellGuard : BaseAIGuard
    {
        [Constructable]
        public AICellGuard()
            : base()
        {
            InitStats(CoreAI.CellGuardStrength, 100, 100);

            // Set the BroadSword damage
            SetDamage(14, 25);

            SetSkill(SkillName.Anatomy, CoreAI.CellGuardSkillLevel);
            SetSkill(SkillName.Tactics, CoreAI.CellGuardSkillLevel);
            SetSkill(SkillName.Swords, CoreAI.CellGuardSkillLevel);
            SetSkill(SkillName.MagicResist, CoreAI.CellGuardSkillLevel);
        }

        public AICellGuard(Serial serial)
            : base(serial)
        {
        }
        private void AICrossShardLoot()
        {   // Angel Island creatures that exist on multiple shards use common loot
            //  i.e., that loot doesn't change across different shard configs
            DropWeapon(1, 1);
            DropWeapon(1, 1);

            DropItem(new BagOfReagents(CoreAI.CellGuardNumRegDrop));
            DropItem(new ParalyzeScroll());
            DropItem(new FireballScroll());
            DropItem(new NightSightScroll());
        }
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {   // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                AICrossShardLoot();
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                        AICrossShardLoot();
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        public override bool OnBeforeDeath()
        {
            return base.OnBeforeDeath();
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