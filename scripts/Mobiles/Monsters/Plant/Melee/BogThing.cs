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

/* Scripts/Mobiles/Monsters/Plant/Melee/BogThing.cs
 * ChangeLog
 *  11/26/2023, Adam (Siege II)
 *      we don't use the tree of knowledge on Siege (no bonded pets) so we spawn Kukui Nuts here
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *  7/31/04, OldSalty
 * 		Added reaper-type sounds to this creature (line 44).
 *  6/9/04, OldSalty
 * 		Added Seeds to creatures pack consistent with RunUO 1.0
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    [CorpseName("a plant corpse")]
    public class BogThing : BaseCreature
    {
        [Constructable]
        public BogThing()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.3, 0.6)
        {
            Name = "a bog thing";
            Body = 780;
            BaseSoundID = 442;  //Added by Old Salty
            BardImmune = true;

            SetStr(801, 900);
            SetDex(46, 65);
            SetInt(36, 50);

            SetHits(481, 540);
            SetMana(0);

            SetDamage(10, 23);

            SetSkill(SkillName.MagicResist, 90.1, 95.0);
            SetSkill(SkillName.Tactics, 70.1, 85.0);
            SetSkill(SkillName.Wrestling, 65.1, 80.0);

            Fame = 8000;
            Karma = -8000;

            VirtualArmor = 28;

        }

        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public BogThing(Serial serial)
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

        public void SpawnBogling(Mobile m)
        {
            Map map = this.Map;

            if (map == null)
                return;

            Bogling spawned = new Bogling();

            spawned.Team = this.Team;

            bool validLocation = false;
            Point3D loc = this.Location;

            for (int j = 0; !validLocation && j < 10; ++j)
            {
                int x = X + Utility.Random(3) - 1;
                int y = Y + Utility.Random(3) - 1;
                int z = map.GetAverageZ(x, y);

                if (validLocation = Utility.CanFit(map, x, y, this.Z, 16, Utility.CanFitFlags.requireSurface))
                    loc = new Point3D(x, y, Z);
                else if (validLocation = Utility.CanFit(map, x, y, z, 16, Utility.CanFitFlags.requireSurface))
                    loc = new Point3D(x, y, z);
            }

            spawned.MoveToWorld(loc, map);

            spawned.Combatant = m;
        }

        public void EatBoglings()
        {
            ArrayList toEat = new ArrayList();

            IPooledEnumerable eable = this.GetMobilesInRange(2);
            foreach (Mobile m in eable)
            {
                if (m is Bogling)
                    toEat.Add(m);
            }
            eable.Free();

            if (toEat.Count > 0)
            {
                PlaySound(Utility.Random(0x3B, 2)); // Eat sound

                foreach (Mobile m in toEat)
                {
                    Hits += (m.Hits / 2);
                    m.Delete();
                }
            }
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (this.Hits > (this.HitsMax / 4))
            {
                if (0.25 >= Utility.RandomDouble())
                    SpawnBogling(attacker);
            }
            else if (0.25 >= Utility.RandomDouble())
            {
                EatBoglings();
            }
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                if (0.25 > Utility.RandomDouble())
                    PackItem(new Board(10));
                else
                    PackItem(new Log(10));

                PackReg(10, 15);
                PackGold(50, 100);
                PackItem(new Engines.Plants.Seed());
                PackItem(new Engines.Plants.Seed());
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020207053503/uo.stratics.com/hunters/bogling.shtml
                    // no page
                    // http://web.archive.org/web/20020806164704/uo.stratics.com/hunters/bogthing.shtml
                    // 100 - 150 Gold, Reagents, 10 Logs, 2 Seeds
                    if (Spawning)
                    {
                        PackGold(100, 150);
                    }
                    else
                    {
                        PackReg(10, 15);
                        PackItem(new Log(10));
                        if (Engines.Plants.PlantSystem.Enabled)
                        {
                            PackItem(new Engines.Plants.Seed());
                            PackItem(new Engines.Plants.Seed());
                        }

                        // we don't use the tree of knowledge on Siege (no bonded pets) so we spawn Kukui Nuts here
                        if (Core.RuleSets.SiegeRules() && Core.SiegeII_CFG)
                            if (Utility.Chance(0.20))
                                PackItem(new KukuiNut());
                    }
                }
                else
                {   // Standard RunUO
                    if (Spawning)
                    {
                        if (0.25 > Utility.RandomDouble())
                            PackItem(new Board(10));
                        else
                            PackItem(new Log(10));

                        PackReg(3);
                        if (Engines.Plants.PlantSystem.Enabled)
                        {
                            PackItem(new Engines.Plants.Seed());
                            PackItem(new Engines.Plants.Seed());
                        }
                    }

                    AddLoot(LootPack.Average, 2);
                }
            }
        }
    }
}