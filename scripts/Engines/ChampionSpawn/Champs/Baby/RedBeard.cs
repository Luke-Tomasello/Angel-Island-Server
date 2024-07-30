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

/* Scripts\Engines\ChampionSpawn\Champs\Baby\RedBeard.cs
 * ChangeLog:
 *	7/19/2023, Adam
 *	    First time checkin
 *	    Copied from PirateChamp.cs
 */

using Server.Diagnostics;			// log helper
using Server.Items;
using Server.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Items.MusicBox;
using static Server.Utility;

namespace Server.Mobiles
{
    [CorpseName("corpse of a salty seadog")]
    public class RedBeard : Pirate
    {
        private MetalChest m_MetalChest = null;
        private RolledUpSheetMusic m_tune = null;
        private Timer m_tuneTimer = null;

        [Constructable]
        public RedBeard()
            : base(AIType.AI_Hybrid)
        {
            BardImmune = true;
            UsesHumanWeapons = true;
            UsesBandages = true;
            UsesPotions = true;
            CanRun = true;
            FightMode = FightMode.Aggressor;
            InitializeMusic();
        }
        public override void WorldLoaded()
        {
            base.WorldLoaded();
            InitializeMusic();
        }
        private void InitializeMusic()
        {
            if (Backpack != null)
            {
                m_tune = Backpack.FindItemByType(typeof(RolledUpSheetMusic)) as RolledUpSheetMusic;
                if (m_tune != null)
                    return;
            }

            object info = null;
            string song = ". . Yo-ho! (A Pirate's Life for Me)";
            if (MusicBox.FindSong(song.Split(), out info))
            {

                m_tune = ((KeyValuePair<RolledUpSheetMusic, MusicInfo>)info).Key;
                m_tune = (RolledUpSheetMusic)Utility.Dupe(m_tune);
                m_tune.LootType = LootType.Newbied;
                this.AddToBackpack(m_tune);
            }
        }
        public override void OnSee(Mobile m)
        {
            base.OnSee(m);
            if (m.Player && CanSee(m) && Combatant == null)
                if (m.Map != null && m.Map != Map.Internal)
                    if (m_tune != null && !m_tune.Deleted && m_tune.IsThisDevicePlaying(this) == false && m_tuneTimer == null)
                        m_tuneTimer = Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(PlayTune), new object[] { });

        }
        private void PlayTune(object state)
        {
            if (m_tune != null && !m_tune.Deleted)
                m_tune.OnDoubleClick(this);
            m_tuneTimer = null;
        }
        private void StopTune()
        {
            if (m_tune != null && m_tune.IsThisDevicePlaying(this))
                m_tune.OnDoubleClick(this);
        }
        public override void OnCombatantChange()
        {
            base.OnCombatantChange();
            if (Combatant != null)
                StopTune();
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
        public RedBeard(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            base.InitBody();
            Name = "Red Beard";
            Title = "the hoard guardian";
            Female = false;

        }

        public override void InitOutfit()
        {
            base.InitOutfit();
            // hat
            Item hat = FindItemOnLayer(Layer.Helm);
            if (hat != null)
                hat.Delete();

            AddItem(CaptainsHat("a pirate hat"));

            // weapon
            Item weapon = FindItemOnLayer(Layer.OneHanded);
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
            Item beard = FindItemOnLayer(Layer.FacialHair);
            if (beard != null)
                beard.Delete();

            beard = new MediumLongBeard(0x664);
            beard.Layer = Layer.FacialHair;
            beard.Movable = false;
            AddItem(beard);

            // pants
            Item pants = FindItemOnLayer(Layer.Pants);
            if (pants != null)
                pants.Hue = 0x01;

            // shirt
            Item shirt = FindItemOnLayer(Layer.Shirt);
            if (shirt != null)
                shirt.Delete();

            AddItem(new FancyShirt(0x64A));

            // shoes
            Item shoes = FindItemOnLayer(Layer.Shoes);
            if (shoes != null)
                shoes.Hue = 0x00;
        }
        public override bool OnBeforeDeath()
        {
            this.Say(true, "Heh! On to Davy Jones' lockarrr..");
            this.Say(true, "Ye'll not be gettin' me hoard!");

            Item weapon = FindItemOnLayer(Layer.OneHanded);
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
            Mobile target = caster;

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

                helper.Combatant = target;
            }
        }
        public override void DistributeLoot()
        {
            // do nothing. The Pirate champs drops a chest with all the good stuff.
            //  Makes it a second PvP battler to get the chest!
        }
        public override void GenerateLoot()
        {
            // build 'hoard' loot
            if (!Spawning)
                BuildChest();
        }
        private static int[] ChestColors = new int[]
        {
                0x973,  // "Dull Copper"
                0x966,  // "Shadow Iron"
                0x96D,  // "Copper"
                0x972,  // "Bronze"
                0x8A5,  // "Gold"
                0x979,  // "Agapite"
                0x89F,  // "Verite"
                0x8AB   // "Valorite"
         };
        public void BuildChest()
        {
            m_MetalChest = new MetalChest();
            m_MetalChest.Name = "Dead Man's Chest";
            m_MetalChest.Hue = ChestColors[Utility.GetStableHashCode(Name) % ChestColors.Length];
            m_MetalChest.Movable = false;

            DungeonTreasureChest.InitializeTrap(m_MetalChest, level: 4);

            // setup timed release logic
            string[] lines = new string[4];
            lines[0] = "Movable true";
            lines[1] = "TrapEnabled false";
            lines[2] = "TrapPower 0";
            lines[3] = "Locked true";

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
            LogHelper Logger = new LogHelper("RedBeard's Chest.log", false);

            // 25 piles * 600 = 15K gold
            for (int ix = 0; ix < 25; ix++)
            {   // force the separate piles
                Gold gold = new Gold(450, 600);
                gold.Stackable = false;
                m_MetalChest.DropItem(gold);
                gold.Stackable = true;
            }
            Logger.Log(string.Format("TotalGold: {0}", m_MetalChest.TotalGold));

            do
            {
                // "a smelly old mackerel"
                if (Utility.RandomChance(10))
                {
                    Item ii;
                    ii = new BigFish();
                    ii.Name = "a smelly old mackerel";
                    ii.Weight = 5;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // single gold ingot weight 12
                if (Utility.RandomChance(10 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(7145);
                    else
                        ii = new Item(7148);

                    ii.Weight = 12;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // 3 gold ingots 12*3
                if (Utility.RandomChance(5 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(7146);
                    else
                        ii = new Item(7149);

                    ii.Weight = 12 * 3;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // 5 gold ingots 12*5
                if (Utility.RandomChance(1 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(7147);
                    else
                        ii = new Item(7150);

                    ii.Weight = 12 * 5;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // single silver ingot weight 6
                if (Utility.RandomChance(10 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(7157);
                    else
                        ii = new Item(7160);

                    ii.Weight = 6;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // 3 silver ingots 6*3
                if (Utility.RandomChance(5 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(7158);
                    else
                        ii = new Item(7161);

                    ii.Weight = 6 * 3;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // 5 silver ingots 6*5
                if (Utility.RandomChance(1 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(7159);
                    else
                        ii = new Item(7162);

                    ii.Weight = 6 * 5;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // rolled map w1
                if (Utility.RandomChance(20 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(5357);
                    else
                        ii = new Item(5358);

                    ii.Weight = 1;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // ship plans
                if (Utility.RandomChance(10 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(5361);
                    else
                        ii = new Item(5362);

                    ii.Weight = 1;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // ship model
                if (Utility.RandomChance(5 * 2))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(5363);
                    else
                        ii = new Item(5364);

                    ii.Weight = 3;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }

                // "scale shield" w6
                if (Utility.RandomChance(1))
                {
                    Item ii;
                    if (Utility.RandomBool())
                        ii = new Item(7110);
                    else
                        ii = new Item(7111);

                    ii.Name = "scale shield";
                    ii.Weight = 6;
                    m_MetalChest.DropRare(ii);
                    RaresDropped++;
                    Logger.Log(LogType.Item, ii);
                }
            } while (RaresDropped == 0);

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

            //writer.Write((int)(m_tune != null ? m_tune.Serial : Serial.Zero));
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            if (base.Version == 0)
                return;

            int version = reader.ReadInt();
            /*Serial serial = (Serial)reader.ReadInt();
            if (serial != Serial.Zero)
                m_tune = World.FindItem(serial) as RolledUpSheetMusic;*/
        }
    }
}