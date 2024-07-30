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

/* Scripts/Mobiles/Monsters/Misc/Melee/PlagueBeast.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  9/26/04, Jade
 *      Decreased gold drop from (600, 1000) to (350, 500)
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a plague beast corpse")]
    public class PlagueBeast : BaseCreature
    {
        [Constructable]
        public PlagueBeast()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a plague beast";
            Body = 775;

            SetStr(302, 500);
            SetDex(80);
            SetInt(16, 20);

            SetHits(318, 404);

            SetDamage(20, 24);

            SetSkill(SkillName.MagicResist, 35.0);
            SetSkill(SkillName.Tactics, 100.0);
            SetSkill(SkillName.Wrestling, 100.0);

            Fame = 13000;
            Karma = -13000;

            VirtualArmor = 30;
        }

        // TODO: Poison attack

        public override void OnDamagedBySpell(Mobile caster)
        {
            if (caster != this && 0.25 > Utility.RandomDouble())
            {
                BaseCreature spawn = new PlagueSpawn(this);

                spawn.Team = this.Team;
                spawn.MoveToWorld(this.Location, this.Map);
                spawn.Combatant = caster;

                Say(1053034); // * The plague beast creates another beast from its flesh! *
            }

            base.OnDamagedBySpell(caster);
        }

        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            if (attacker != this && 0.25 > Utility.RandomDouble())
            {
                BaseCreature spawn = new PlagueSpawn(this);

                spawn.Team = this.Team;
                spawn.MoveToWorld(this.Location, this.Map);
                spawn.Combatant = attacker;

                Say(1053034); // * The plague beast creates another beast from its flesh! *
            }

            base.OnGotMeleeAttack(attacker);
        }

        public PlagueBeast(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(350, 500);
                PackGem();

                // TODO: jewelry, dungeon chest, healthy gland

                // Category 3 MID
                PackMagicItem(1, 2, 0.10);
                PackMagicItem(1, 2, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // page not found for 02 2002
                    // http://web.archive.org/web/20021015005145/uo.stratics.com/hunters/plaguebeast.shtml
                    // 600 - 1000 Gold, Gem, Armor, Magic Jewelry, Lootable Dungeon Chest, a healthy gland
                    if (Spawning)
                    {
                        PackGold(600, 1000);
                    }
                    else
                    {
                        PackGem(Utility.Random(1, 3));

                        PackMagicArmor(1, 2, 0.10);

                        PackMagicJewelry(1, 2, 0.10);

                        // http://web.archive.org/web/20021015005145/uo.stratics.com/hunters/plaguebeast.shtml
                        // "and in the chest i got 2 vanqs, one durable supreme acc silver broad of vanq, and a dur/vanq mace."
                        // In the later docs we see that it's a metal chest
                        // http://uo.stratics.com/database/view.php?db_content=hunters&id=276
                        // "When killed, its corpse may contain a metal chest if it has devoured enough corpses."

                        /* General Information
						 * This hideous creature will strike with poisonous attacks, and other slimy creatures will spawn out of it, 
						 * in the form of purple colored earth elementals, headless ones, people, gorillas, serpents and slimes, 
						 * all known as "plague spawn".
						 * It will devour the corpses of the ones it slayed, and will turn human corpses into a pile of bones instantly. 
						 * When killed, it's corpse may contain a dungeon chest, which can be looted, but not removed.
						 */

                        // we still need to verify the dopp rate and loot. For now just give a level 3 chest
                        // we will make this really rare since we need to verify so much
                        // note, the chest is not supposed to be movable, yet making it non movable prevents it from dropping
                        //	from the plaguebeast. *sigh*
                        //	also this chest is supposed to be related to the corpses eaten - more corpses, better loot
                        if (0.001 > Utility.RandomDouble() && false)
                        {
                            Container chest = new MetalGoldenChest();
                            TreasureMapChest.Fill((chest as LockableContainer), 3);
                            chest.Movable = true;
                            PackItem(chest);
                        }

                        // TODO: dungeon chest, healthy gland
                    }
                }
                else
                {   // Standard RunUO
                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Gems, Utility.Random(1, 3));
                    // TODO: dungeon chest, healthy gland
                }
            }
        }

        // TODO: Poison attack

        public override int GetIdleSound()
        {
            return 0x1BF;
        }

        public override int GetAttackSound()
        {
            return 0x1C0;
        }

        public override int GetHurtSound()
        {
            return 0x1C1;
        }

        public override int GetDeathSound()
        {
            return 0x1C2;
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