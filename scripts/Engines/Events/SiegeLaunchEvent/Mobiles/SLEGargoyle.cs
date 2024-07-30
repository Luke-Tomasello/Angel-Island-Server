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

/* scripts\Engines\Events\SiegeLaunchEvent\Mobiles\SLEGargoyle.cs
 * ChangeLog
 *  1/3/23, Adam
 *		Created for Siege launch event (factions based)
 */
using Server.Factions;
using Server.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{

    [CorpseName("a gargoyle corpse")]
    public class SLEGargoyle : BaseCreature
    {
        // provide polymorphic allegiance of the opposing faction
        public override Faction FactionAllegiance { get { return GetCounterAllegiance(); } }
        Faction cachedAllegiance = null;
        private Faction GetCounterAllegiance()
        {
            if (cachedAllegiance == null)
                return Minax.Instance;
            else
                return cachedAllegiance;
        }
        public override void OnRegionChange(Region Old, Region New)
        {
            base.OnRegionChange(Old, New);
            if (this.Map == Map.Felucca)
                // assume the side of the opposing faction
                // wait for the regions to load...
                Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(Tick), new object[] { null });
        }
        private void Tick(object state)
        {
            List<Region> stronghold = new()
            {
                Region.FindByName("Minax", this.Map),
                Region.FindByName("Council of Mages", this.Map),
                Region.FindByName("True Britannians", this.Map),
                Region.FindByName("Shadowlords", this.Map)
            };
            bool allCool = true;
            foreach (Region rx in stronghold)
                if (rx == null)
                    allCool = false;

            if (allCool)
                cachedAllegiance = CounterAllegiance(this.Location, this.Map);
            else
                // wait for the regions to load
                Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(Tick), new object[] { null });
        }
        [Constructable]
        public SLEGargoyle()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Name = NameList.RandomName("gargoyle");
            Body = 4;
            BaseSoundID = 372;

            SetStr(146, 175);
            SetDex(76, 95);
            SetInt(81, 105);

            SetHits(88, 105);

            SetDamage(7, 14);

            SetSkill(SkillName.EvalInt, 70.1, 85.0);
            SetSkill(SkillName.Magery, 70.1, 85.0);
            SetSkill(SkillName.MagicResist, 70.1, 85.0);
            SetSkill(SkillName.Tactics, 50.1, 70.0);
            SetSkill(SkillName.Wrestling, 40.1, 80.0);

            Fame = 3500;
            Karma = -3500;

            VirtualArmor = 32;
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }
        public override int Meat { get { return 1; } }

        public SLEGargoyle(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGem();
                PackGem();
                PackGem();
                PackGem();
                PackGold(60, 100);
                PackScroll(1, 7);

                if (0.025 > Utility.RandomDouble())
                    PackItem(new GargoylesPickaxe());

                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20011205084450/uo.stratics.com/hunters/gargoyle.shtml
                    // 50 to 150 Gold, Potions, Arrows, Scrolls, Gems, "a gargoyle's pickaxe"

                    if (Spawning)
                    {
                        PackGold(50, 150);
                    }
                    else
                    {
                        PackPotion();
                        PackItem(new Arrow(Utility.RandomMinMax(1, 4)));
                        PackScroll(1, 6);
                        PackGem(Utility.RandomMinMax(1, 4));
                        PackItem(typeof(GargoylesPickaxe), 0.025);
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        if (0.025 > Utility.RandomDouble())
                            PackItem(new GargoylesPickaxe());
                    }

                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.MedScrolls);
                    AddLoot(LootPack.Gems, Utility.RandomMinMax(1, 4));
                }
            }
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

        public static Faction CounterAllegiance(Point3D point, Map map)
        {
            return null;
            List<Region> stronghold = new()
            {
                Region.FindByName("Minax", map),
                Region.FindByName("Council of Mages", map),
                Region.FindByName("True Britannians", map),
                Region.FindByName("Shadowlords", map)
            };
            Dictionary<string, Faction> lookup = new()
            {
                {"Minax", Minax.Instance},
                {"Council of Mages", CouncilOfMages.Instance },
                {"True Britannians", TrueBritannians.Instance },
                {"Shadowlords", Shadowlords.Instance }
            };
            Region best = null;
            double shortest = 0.0;
            foreach (Region region in stronghold)
            {
                if (region != null)
                    foreach (Rectangle3D r3d in region.Coords)
                    {
                        Rectangle2D r2d = new Rectangle2D(r3d.Start, r3d.End);
                        double dist = Utility.GetDistanceToSqrt(new Point3D(r2d.Center.X, r2d.Center.Y, 0), point);
                        if (best == null || dist < shortest)
                        {
                            best = region;
                            shortest = dist;
                        }
                    }
            }

            List<Faction> list = lookup.Where(x => x.Key != best.Name).Select(x => x.Value).ToList();
            return list[Utility.Random(list.Count)];
        }
    }
}