/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\ChampionSpawn\Champs\Seasonal\Spring\Mobiles\PirateChamp.cs	
 * ChangeLog:
 *	7/9/10, adam
 *		o Merge pirate class hierarchy (all pirates are now derived from class Pirate)
 *	1/1/09, Adam
 *		- Add potions and bandages
 *			Now uses real potions and real bandages
 *		- Cross heals is now turned off
 *		- Smart AI upgrade (adds healing with bandages)
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *  3/22/07, Adam
 *      Created; based largely on Neira stats
 */

using Server.Diagnostics;			// log helper
using Server.Engines.CrownSterlingSystem;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Utility;
namespace Server.Mobiles
{
    [CorpseName("corpse of a salty seadog")]
    public class PirateChamp : Pirate
    {
        private MetalChest m_MetalChest = null;
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }

        [Constructable]
        public PirateChamp()
            : base(AIType.AI_Hybrid)
        {
            BardImmune = true;
            UsesHumanWeapons = true;
            UsesBandages = true;
            UsesPotions = true;
            CanRun = true;
        }

        public override void InitClass()
        {

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4800);
            SetStam(102, 300);

            VirtualArmor = 30;

            SetDamage(25, 35);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 150.0);
            SetSkill(SkillName.Swords, 97.6, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);
            SetSkill(SkillName.Healing, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

        }
        #region DraggingMitigation
        public override List<Mobile> GetDraggingMitigationHelpers()
        {
            List<Mobile> helpers = new List<Mobile>();
            // okay, now we take action against this bothersome individual!
            for (int ix = 0; ix < 2; ix++)
            {   // these will be out helpers
                if (Utility.RandomBool())
                    helpers.Add(new PirateWench());
                else
                    helpers.Add(new Pirate());
            }
            return helpers;
        }
        #endregion DraggingMitigation
        public PirateChamp(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            base.InitBody();
            Name = "Red Beard";
            Title = "the hoard guardian";
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            // hat
            Item? hat = FindItemOnLayer(Layer.Helm);
            if (hat != null)
                hat.Delete();

            AddItem(CaptainsHat("a pirate hat"));

            // weapon
            Item? weapon = FindItemOnLayer(Layer.OneHanded);
            if (weapon is BaseSword bs)
            {
                bs.Slayer = SlayerName.DragonSlaying;
                bs.Name = "Hanger of Edward Teach";
                bs.Identified = true;
            }

            // sex
            BodyValue = 400;
            Female = false;

            // beard
            Item? beard = FindItemOnLayer(Layer.FacialHair);
            if (beard != null)
                beard.Delete();

            beard = new MediumLongBeard(0x664);
            beard.Layer = Layer.FacialHair;
            beard.Movable = false;
            AddItem(beard);

            // pants
            Item? pants = FindItemOnLayer(Layer.Pants);
            if (pants != null)
                pants.Hue = 0x01;

            // shirt
            Item? shirt = FindItemOnLayer(Layer.Shirt);
            if (shirt != null)
                shirt.Delete();

            AddItem(new FancyShirt(0x64A));

            // shoes
            Item? shoes = FindItemOnLayer(Layer.Shoes);
            if (shoes != null)
                shoes.Hue = 0x00;
        }
        public override bool OnBeforeDeath()
        {
            this.Say(true, "Heh! On to Davy Jones' lockarrr..");
            this.Say(true, "Ye'll not be gettin' me hoard!");

            Item? weapon = FindItemOnLayer(Layer.OneHanded);
            if (weapon is BaseSword bs)
            {
                bs.Movable = true;
                bs.LootType = LootType.Regular;
            }

            return base.OnBeforeDeath();
        }
        public override void OnDamagedBySpell(Mobile caster)
        {
            if (caster == this)
                return;

            // Adam: 12% chance to spawn a Bone Knight
            if (Utility.RandomChance(12))
                SpawnBoneKnight(caster);
        }

        public void SpawnBoneKnight(Mobile caster)
        {
            Mobile? target = caster;

            if (Map == null || Map == Map.Internal)
                return;

            int helpers = 0;
            ArrayList mobs = new ArrayList();
            IPooledEnumerable eable = this.GetMobilesInRange(10);
            foreach (Mobile m in eable)
            {
                if (m is BoneKnight)
                    ++helpers;

                if (m is PlayerMobile && m.Alive == true && m.Hidden == false && m.AccessLevel <= AccessLevel.Player)
                    mobs.Add(m);
            }
            eable.Free();

            if (helpers < 5)
            {
                BaseCreature helper = new BoneKnight();

                helper.Team = this.Team;
                helper.Map = Map;

                bool validLocation = false;

                // pick a random player to focus on
                //  if there are no players, we will stay with the caster
                if (mobs.Count > 0)
                    target = mobs[Utility.Random(mobs.Count)] as Mobile;

                for (int j = 0; !validLocation && j < 10; ++j)
                {
                    int x = target.X + Utility.Random(3) - 1;
                    int y = target.Y + Utility.Random(3) - 1;
                    int z = Map.GetAverageZ(x, y);

                    if (validLocation = Utility.CanFit(Map, x, y, this.Z, 16, Utility.CanFitFlags.requireSurface))
                        helper.Location = new Point3D(x, y, Z);
                    else if (validLocation = Utility.CanFit(Map, x, y, z, 16, Utility.CanFitFlags.requireSurface))
                        helper.Location = new Point3D(x, y, z);
                }

                if (!validLocation)
                    helper.Location = target.Location;

                helper.OnAfterSpawn();

                helper.Combatant = target;
            }
        }
        public override void DistributeLoot()
        {
            // do nothing. The Pirate champs drops a chest with all the good stuff.
            //  Makes it a second PvP battler to get the chest!
        }
        public override bool IsChampion { get { return true; } }
        public override void GenerateLoot()
        {
            if (IsChampion)
            {
                // build 'hoard' loot
                if (!Spawning)
                    BuildChest();
            }
        }

        public void BuildChest()
        {
            m_MetalChest = new MetalChest();
            m_MetalChest.Name = "Dead Man's Chest";
            m_MetalChest.Hue = Utility.RandomMetalHue();
            m_MetalChest.Movable = false;

            DungeonTreasureChest.InitializeTrap(m_MetalChest, level: 4);

            // setup timed release logic
            string[] lines = new string[4];
            lines[0] = "Movable true";
            lines[1] = "TrapEnabled false";
            lines[2] = "TrapPower 0";
            lines[3] = "Locked false";

            // the chest will become movable in 10-25 minutes
            DateTime SetTime = DateTime.UtcNow + TimeSpan.FromMinutes(Utility.RandomMinMax(10, 25));
            new TimedSet(m_MetalChest, SetTime, lines).MoveToIntStorage();

            // add loot
            FillChest();

            // move the chest to world;
            m_MetalChest.MoveToWorld(Spawner.GetSpawnPosition(Map, Location, 2, SpawnFlags.None, m_MetalChest), Map);

        }
        private void FillChest()
        {
            int RaresDropped = 0;
            LogHelper Logger = new LogHelper("PirateChampChest.log", false);

            // 25 piles * 1200 = 30K gold
            for (int ix = 0; ix < 25; ix++)
            {   // force the separate piles
                Gold gold = new Gold(800, 1200);
                gold.Stackable = false;
                m_MetalChest.DropItem(gold);
                gold.Stackable = true;
                // add some sterling
                Sterling sterling = new Sterling(80, 300);
                sterling.Stackable = false;
                m_MetalChest.DropItem(sterling);
                sterling.Stackable = true;
            }
            Logger.Log(string.Format("TotalGold: {0}", m_MetalChest.TotalGold));

            int targetRares = 3;

            while (RaresDropped < targetRares)
            {
                Item? ii = null;

                switch (Utility.Random(13)) // 13 case blocks
                {
                    case 0:
                        {
                            // rare cloth
                            ii = new UncutCloth(50);
                            // best ore hues (vet rewards) + really dark 'evil cloth' and of course, black
                            ii.Hue = Utility.RandomList(2213, 2219, 2207, 2425, 1109 /*dark evil*/, 0x01 /*black*/);
                            break;
                        }

                    case 1:
                        {
                            // "a smelly old mackerel"
                            ii = new BigFish();
                            ii.Name = "a smelly old mackerel";
                            ii.Weight = 5;
                            break;
                        }

                    case 2:
                        {
                            // single gold ingot weight 12
                            ii = Utility.RandomBool() ? new Item(7145) : new Item(7148);
                            ii.Weight = 12;
                            break;
                        }

                    case 3:
                        {
                            // 3 gold ingots 12*3
                            ii = Utility.RandomBool() ? new Item(7146) : new Item(7149);
                            ii.Weight = 12 * 3;
                            break;
                        }

                    case 4:
                        {
                            // 5 gold ingots 12*5
                            ii = Utility.RandomBool() ? new Item(7147) : new Item(7150);
                            ii.Weight = 12 * 5;
                            break;
                        }

                    case 5:
                        {
                            // single silver ingot weight 6
                            ii = Utility.RandomBool() ? new Item(7157) : new Item(7160);
                            ii.Weight = 6;
                            break;
                        }

                    case 6:
                        {
                            // 3 silver ingots 6*3
                            ii = Utility.RandomBool() ? new Item(7158) : new Item(7161);
                            ii.Weight = 6 * 3;
                            break;
                        }

                    case 7:
                        {
                            // 5 silver ingots 6*5
                            ii = Utility.RandomBool() ? new Item(7159) : new Item(7162);
                            ii.Weight = 6 * 5;
                            break;
                        }

                    case 8:
                        {
                            // rolled map w1
                            ii = Utility.RandomBool() ? new Item(5357) : new Item(5358);
                            ii.Weight = 1;
                            break;
                        }

                    case 9:
                        {
                            // ship plans
                            ii = Utility.RandomBool() ? new Item(5361) : new Item(5362);
                            ii.Weight = 1;
                            break;
                        }

                    case 10:
                        {
                            // ship model
                            ii = Utility.RandomBool() ? new Item(5363) : new Item(5364);
                            ii.Weight = 3;
                            break;
                        }

                    case 11:
                        {
                            // "scale shield" w6
                            ii = Utility.RandomBool() ? new Item(7110) : new Item(7111);
                            ii.Name = "scale shield";
                            ii.Weight = 6;
                            break;
                        }

                    case 12:
                        {
                            // BackpackRewardDeed
                            ii = new BackpackRewardDeed();
                            break;
                        }
                }

                if (ii != null)
                {
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }
            }

            // level 5 chest regs & gems
            TreasureMapChest.PackRegs(m_MetalChest, 5 * 10);
            TreasureMapChest.PackGems(m_MetalChest, 5 * 5);

            // level 5 magic items
            DungeonTreasureChest.PackMagicItem(m_MetalChest, 3, 3, 0.20);
            DungeonTreasureChest.PackMagicItem(m_MetalChest, 3, 3, 0.10);
            DungeonTreasureChest.PackMagicItem(m_MetalChest, 3, 3, 0.05);

            // an a level 5 treasure map
            m_MetalChest.DropItem(new TreasureMap(5, Map.Felucca));

            Logger.Log(LogType.Text, string.Format("There were a total of {0} rares dropped.", RaresDropped));
            Logger.Finish();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if (base.Version == 0)
                return;

            int version = reader.ReadInt();

        }
    }
}