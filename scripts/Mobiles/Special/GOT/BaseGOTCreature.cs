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

/* Scripts\Mobiles\Special\GOT\BaseGOTCreature.cs
 * ChangeLog
 *  4/27/2024, Adam 
 *		Created.
 */

/*
 * In Game of Thrones, the undead have names, including the wights, which are the reanimated corpses of humans or animals raised by the White Walkers. 
 * The wights are similar to zombies and are intent on killing under the direction of the White Walkers. The White Walkers are also known as Others in the books.
 * The White Walkers are ice monsters that were once human. They can turn dead and decomposed bodies into wights, which are reanimated corpses of humans. 
 * Wights raised by the White Walkers were often referred to collectively as the Army of the Dead, or simply the dead. 
 * The White Walkers' ultimate goal is the end of every living thing in existence, which they plan to achieve by killing the Three-Eyed Raven and creating an endless winter to eclipse the known world.
 */
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class BaseGOTCreature : BaseCreature
    {
        #region CreatureCreateParms
        public enum GOTType
        {
            Humanoid_Warrior,
            Humanoid_Mage,
            Wolf,
            Zombie,
            Skeleton,
            BoneKnight,
            NightKing       // boss
        }
        public enum GOTClass
        {
            Creature,       // wolves, zombies, etc.
            Human,
            Wight,          // army
            WhiteWalker,    // captians
            NightKing       // boss
        }
        public class CreatureCreateParms
        {
            public GOTType m_GOTType;
            public AIType m_Ai;
            public FightMode m_Mode;
            public int m_iRangePerception;
            public int m_iRangeFight;
            public double m_dActiveSpeed;
            public double m_dPassiveSpeed;
            public CreatureCreateParms(AIType ai, FightMode mode, int iRangePerception, int iRangeFight, double dActiveSpeed, double dPassiveSpeed)
            {
                m_Ai = ai;
                m_Mode = mode;
                m_iRangePerception = iRangePerception;
                m_iRangeFight = iRangeFight;
                m_dActiveSpeed = dActiveSpeed;
                m_dPassiveSpeed = dPassiveSpeed;
            }
        }
        #endregion CreatureCreateParms
        public override bool ClickTitle { get { return false; } }
        protected static CreatureCreateParms DefaultWarriorCreate = new CreatureCreateParms(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5);
        protected static CreatureCreateParms DefaultMageCreate = new CreatureCreateParms(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5);
        protected static CreatureCreateParms DefaultHybridCreate = new CreatureCreateParms(AIType.AI_Hybrid, FightMode.All | FightMode.Weakest, 10, 1, 0.175, 0.350);

        public BaseGOTCreature(GOTType type, GOTClass @class, CreatureCreateParms parms)
            : base(parms.m_Ai, parms.m_Mode, parms.m_iRangePerception, parms.m_iRangeFight, parms.m_dActiveSpeed, parms.m_dPassiveSpeed)
        {
            SusceptibleTo = CraftResource.Dragonglass;
            BaseSoundID = 471;                          // they all (but wolves,) make zombie noises
            SpeechHue = Utility.RandomSpeechHue();      // for debugging emotes
            switch (type)
            {
                case GOTType.Humanoid_Warrior:
                    {
                        DefaultWarriorInit(@class);
                        break;
                    }
                case GOTType.Humanoid_Mage:
                    {
                        DefaultMageInit(@class);
                        break;
                    }
                case GOTType.Wolf:
                    {
                        DefaultWolfInit();
                        break;
                    }
                case GOTType.Zombie:
                    {
                        DefaultZombieInit();
                        break;
                    }
                case GOTType.Skeleton:
                    {
                        DefaultSkeletonInit();
                        break;
                    }
                case GOTType.BoneKnight:
                    {
                        DefaultBoneKnightInit();
                        break;
                    }
                case GOTType.NightKing:
                    {
                        DefaultNightKingInit();
                        break;
                    }
            }
        }
        public override Mobile FocusDecisionAI(Mobile m)
        {   // we encountered another GOT creature. If we are on a team and have a different team than them, conscript them
            if (m is BaseGOTCreature bgc)
            {
                if (this.Team > 0)
                    ;// this.Emote("* Looking for candidates... *");

                if (this.Team != 0 && bgc.Team != this.Team && !string.IsNullOrEmpty(this.NavDestination))
                {
                    this.PlaySound(this.GetAngerSound());
                    bgc.PlaySound(bgc.GetIdleSound());
                    bgc.Team = this.Team;
                    bgc.NavDestination = this.NavDestination;
                    //bgc.Emote("* I've been Conscripted! *");
                    // we won't attack this guy, we conscripted them
                    return null;
                }
                else if (bgc.Team != this.Team)
                {   // we are open to being conscripted
                    if (bgc.Team == 0)
                        ;// bgc.Emote("* Awaiting conscription... *");
                    // so don't attack
                    return null;
                }
            }

            return m;
        }
        public virtual void DoRewardArmor(double chance)
        { }
        public override bool Rummage()
        {
            Corpse toRummage = null;

            IPooledEnumerable eable = this.GetItemsInRange(2);
            foreach (Item item in eable)
            {
                if (item is Corpse c && c.Items.Count > 0 && c.StaticCorpse == false)
                {
                    if (CanReanimateCorpses && c.Reanimated == false)
                        // don't rummage corpses we are able to reanimate
                        continue;

                    toRummage = (Corpse)item;
                    break;
                }
            }
            eable.Free();

            if (toRummage == null)
                return false;

            Container pack = this.Backpack;

            if (pack == null)
                return false;

            List<Item> items = toRummage.Items;

            bool rejected;
            LRReason reason;

            foreach (Item item in items)
                if (item is BaseWeapon || item is BaseArmor || item is BaseReagent || item is Container)
                {
                    Lift(item, item.Amount, out rejected, out reason);

                    if (!rejected && Drop(this, new Point3D(-1, -1, 0)))
                    {
                        // *rummages through a corpse and takes an item*
                        //PublicOverheadMessage(MessageType.Emote, 0x3B2, 1008086);
                        this.PublicOverheadMessage(MessageType.Emote, 0x3B2, false, string.Format("*rummages through a corpse and takes {0}*", item.OldSchoolName()));
                        return true;
                    }
                }

            // no good stuff, just revert to classic rummage
            base.Rummage();

            return false;
        }
        private void DefaultWarriorInit(GOTClass @class, bool weapons = true)
        {   // brigand
            SetStr(86, 100);
            SetDex(81, 95);
            SetInt(61, 75);

            SetDamage(10, 23);

            SetSkill(SkillName.Fencing, 66.0, 97.5);
            SetSkill(SkillName.Macing, 65.0, 87.5);
            SetSkill(SkillName.MagicResist, 25.0, 47.5);
            SetSkill(SkillName.Swords, 65.0, 87.5);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 15.0, 37.5);

            InitBody(@class);
            InitOutfit(@class);
            if (weapons)
                InitWeaponry(@class);
            DrabLayers(this);

            Fame = 1000;
            Karma = -1000;
        }
        private void DefaultMageInit(GOTClass @class, bool weapons = false)
        {
            if (@class == GOTClass.WhiteWalker)
            {   // lich lord
                SetStr(416, 505);
                SetDex(146, 165);
                SetInt(566, 655);

                SetHits(250, 303);

                SetDamage(11, 13);

                SetSkill(SkillName.EvalInt, 90.1, 100.0);
                SetSkill(SkillName.Magery, 90.1, 100.0);
                SetSkill(SkillName.MagicResist, 150.5, 200.0);
                SetSkill(SkillName.Tactics, 50.1, 70.0);
                SetSkill(SkillName.Wrestling, 60.1, 80.0);
            }
            else if (@class == GOTClass.NightKing)
            {   // champion class
                SetStr(305, 425);
                SetDex(72, 150);
                SetInt(505, 750);
                SetHits(4200);
                SetStam(102, 300);

                SetDamage(25, 35);

                // he's a hybrid
                SetSkill(SkillName.EvalInt, 100.0, 110.0);
                SetSkill(SkillName.Magery, 100.0, 110.0);
                SetSkill(SkillName.Swords, 100.0, 125.0);
                SetSkill(SkillName.Tactics, 100.0, 125.0);
                SetSkill(SkillName.Anatomy, 100.0, 125.0);
                SetSkill(SkillName.Poisoning, 60.0, 82.5);
                SetSkill(SkillName.MagicResist, 83.5, 92.5);
            }
            else
            {
                // orcish mage
                SetStr(116, 150);
                SetDex(91, 115);
                SetInt(161, 185);

                SetHits(70, 90);

                SetDamage(4, 14);

                SetSkill(SkillName.EvalInt, 60.1, 72.5);
                SetSkill(SkillName.Magery, 60.1, 72.5);
                SetSkill(SkillName.MagicResist, 60.1, 75.0);
                SetSkill(SkillName.Tactics, 50.1, 65.0);
                SetSkill(SkillName.Wrestling, 40.1, 50.0);
            }

            InitBody(@class);
            InitOutfit(@class);
            if (weapons)
                InitWeaponry(@class);
            DrabLayers(this);

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 30;
        }
        private void DefaultWolfInit()
        {
            DefaultWarriorInit(GOTClass.Creature, weapons: false);
            Name = "a winter wolf";
            Body = Utility.RandomList(34, 37);
            BaseSoundID = 0xE5;

            Fame = 450;
            Karma = 0;

            VirtualArmor = 16;
        }
        private void DefaultZombieInit()
        {
            DefaultWarriorInit(GOTClass.Creature, weapons: false);
            Body = 3;

            Fame = 600;
            Karma = -600;

            VirtualArmor = 18;
        }
        private void DefaultSkeletonInit()
        {
            DefaultWarriorInit(GOTClass.Creature, weapons: false);
            Body = Utility.RandomList(50, 56);
            BaseSoundID = 0x48D;

            Fame = 450;
            Karma = -450;

            VirtualArmor = 16;
        }
        private void DefaultBoneKnightInit()
        {
            DefaultWarriorInit(GOTClass.Creature, weapons: false);
            Body = 57;
            BaseSoundID = 451;

            Fame = 3000;
            Karma = -3000;

            VirtualArmor = 40;
        }
        private void DefaultNightKingInit()
        {
            DefaultMageInit(GOTClass.NightKing, weapons: true);

            BardImmune = true;
            FightStyle = FightStyle.Melee | FightStyle.Magic | FightStyle.Smart | FightStyle.Bless | FightStyle.Curse;
            UsesHumanWeapons = false;
            UsesBandages = true;
            UsesPotions = true;
            CanRun = true;
            CanReveal = true; // magic and smart

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 70;

            PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
            PackStrongPotions(6, 12);
            PackItem(new Pouch(), lootType: LootType.UnStealable);
        }
        public override bool AlwaysMurderer { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanRummageCorpses { get { return true; } }
        public virtual bool CanReanimateCorpses { get { return false; } }
        public override bool CanBandage { get { return Body.IsHuman ? true : base.CanBandage; } }
        public override TimeSpan BandageDelay { get { return base.BandageDelay; } }

        #region Reanimation
        private const double ChanceToReanimate = 0.5; // 50%
        private const double MinutesToNextReanimateMin = 1.0;
        private const double MinutesToNextReanimateMax = 4.0;
        private const double MinutesToNextChanceMin = 0.25;
        private const double MinutesToNextChanceMax = 0.75;
        private DateTime m_NextReanimateTime;
        #endregion Reanimation

        public BaseGOTCreature(Serial serial)
            : base(serial)
        {
        }
        public override void OnThink()
        {
            base.OnThink();
            if (CanReanimateCorpses && !Summoned && !Controlled && DateTime.UtcNow >= m_NextReanimateTime)
            {
                double min, max;

                if (ChanceToReanimate >= Utility.RandomDouble() && Reanimate())
                {
                    min = MinutesToNextReanimateMin;
                    max = MinutesToNextReanimateMax;
                }
                else
                {
                    min = MinutesToNextChanceMin;
                    max = MinutesToNextChanceMax;
                }

                double delay = min + (Utility.RandomDouble() * (max - min));
                m_NextReanimateTime = DateTime.UtcNow + TimeSpan.FromMinutes(delay);
            }

            if (Utility.Chance(0.05))
                CheckConscript();
        }
        public void CheckConscript()
        {
            Mobile candidate = null;
            IPooledEnumerable eable = this.Map.GetMobilesInRange(this.Location, 5);
            foreach (Mobile m in eable)
                if (m is BaseGOTCreature bc && bc.Team != this.Team)
                {
                    candidate = m;
                    break;
                }
            eable.Free();

            if (candidate != null)
                FocusDecisionAI(candidate);
        }
        private static Utility.LocalTimer ExtraNightKing = new Utility.LocalTimer(0);      // right away
        public bool Reanimate()
        {
            Corpse toReanimate = null;

            IPooledEnumerable eable = this.GetItemsInRange(2);
            foreach (Item item in eable)
            {
                if (item is Corpse c && c.StaticCorpse == false)
                {
                    if (c.Owner != null && c.Reanimated == false && (c.Owner is BaseGOTCreature || c.Owner is PlayerMobile))
                    {
                        toReanimate = (Corpse)item;
                        c.Reanimated = true;

                        this.SetDirection(GetDirectionTo(c));
                        this.PlaySound(0x24A);  // spirit speak
                        this.Animate(action: 269, frameCount: 7, repeatCount: 1, forward: true, repeat: false, delay: 0);

                        Type type;
                        if (c.Owner is PlayerMobile)
                            type = typeof(GotWhiteWalker);
                        else if (toReanimate.Owner.GetType() == typeof(GotNightKing))
                        {   // rarely we will allow this to happen: one in 10, and capped at one per hour
                            if (Utility.Random(10) == 0 && ExtraNightKing.Triggered)
                            {   // don't worry, NoKillAwards for this guy. Packloot ok
                                type = toReanimate.Owner.GetType(); // Night King!
                                // yeah, only one per hour
                                ExtraNightKing.Stop();
                                ExtraNightKing.Start(TimeSpan.FromHours(1).TotalMilliseconds);
                            }
                            else
                                type = typeof(GotWhiteWalker);
                        }
                        else
                            type = toReanimate.Owner.GetType();

                        object o = Activator.CreateInstance(type);
                        if (o is BaseGOTCreature bgc)
                        {
                            if (type == typeof(GotNightKing))
                                bgc.NoKillAwards = true;    // no double champ loot

                            // when reanimating a Night King, but we're not allowed, we need to make sure the name is correct.
                            bgc.Name = (type == typeof(GotWhiteWalker)) ? "a white walker" : toReanimate.Name;

                            if (bgc.Body.IsHuman)
                            {
                                HoodedShroudOfShadows shroud = new HoodedShroudOfShadows(0);
                                shroud.LootType = LootType.Newbied;
                                bgc.AddItem(shroud);
                            }
                            bgc.Team = this.Team;
                            if (!string.IsNullOrEmpty(this.NavDestination))
                                bgc.NavDestination = this.NavDestination;
                            bgc.MoveToWorld(toReanimate.Location, toReanimate.Map);
                            bgc.PlaySound(0x214);               // resurrection sound
                            bgc.FixedEffect(0x376A, 10, 16);    // resurrection effect
                        }
                    }

                    break;
                }
            }
            eable.Free();

            return false;
        }
        public void InitBody(GOTClass @class)
        {
            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                if (Utility.Chance(0.2))
                    Name = NameList.RandomName("Female Wight Names");
                else
                    Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                if (Utility.Chance(0.2))
                    Name = NameList.RandomName("Male Wight Names");
                else
                    Name = NameList.RandomName("male");
            }

            // now override
            if (@class == GOTClass.NightKing)
            {   // night king is always male
                Body = 0x190;
                Name = "Night King";
            }
            else if (@class == GOTClass.WhiteWalker)
                Name = "a white walker";
        }
        public void InitOutfit(GOTClass @class)
        {
            if (@class == GOTClass.Creature)
                // skeletons, wolves and whatnot
                return;

            Utility.WipeLayers(this);

            if (Female)
            {
                Item hair = new Item(0x203C);   // long hair
                hair.Layer = Layer.Hair;
                hair.Movable = false;
                AddItem(hair);

                AddItem(Utility.RandomBool() ? new Skirt() : new ShortPants());
            }
            else
            {   // one in 5 is bald
                if (Utility.Random(5) > 0)
                {
                    Item hair = new Item(0x2048);   // receding hair
                    hair.Layer = Layer.Hair;
                    hair.Movable = false;
                    AddItem(hair);
                }

                AddItem(new ShortPants());
            }

            AddItem(Utility.RandomBool() ? new Boots() : new Shoes());

            if (@class == GOTClass.NightKing)
            {
                Crown crown = new Crown();
                crown.Name = "crown of the night king";
                crown.LootType = LootType.Rare;
                AddItem(crown);
            }

            if (@class == GOTClass.WhiteWalker || @class == GOTClass.NightKing)
            {
                ChainChest chestArmor = new ChainChest();
                chestArmor.Resource = CraftResource.Dragonglass;
                chestArmor.DurabilityLevel = ArmorDurabilityLevel.Indestructible;
                // items with a loot type don't get a 'drab' hue
                if (@class == GOTClass.WhiteWalker)
                    chestArmor.LootType = LootType.Newbied;     // white walkers chest don't drop
                else
                    chestArmor.LootType = LootType.Rare;        // night kings chest drops

                AddItem(chestArmor);
            }
            else if (Utility.RandomBool())
                AddItem(new Shirt());
        }
        public void InitWeaponry(GOTClass @class)
        {
            if (@class == GOTClass.NightKing)
            {
                Halberd hally = new Halberd();
                hally.Name = "Arc Asunder";
                hally.MagicEffect = MagicItemEffect.Harm;
                hally.MagicCharges = 10;
                hally.Resource = CraftResource.Dragonglass;
                hally.HideAttributes = true;
                AddItem(hally);
            }
            else if (@class == GOTClass.Human)
                switch (Utility.Random(7))
                {
                    case 0: AddItemManaged(new Longsword()); break;
                    case 1: AddItemManaged(new Cutlass()); break;
                    case 2: AddItemManaged(new Broadsword()); break;
                    case 3: AddItemManaged(new Axe()); break;
                    case 4: AddItemManaged(new Club()); break;
                    case 5: AddItemManaged(new Dagger()); break;
                    case 6: AddItemManaged(new Spear()); break;
                }
        }
        private void AddItemManaged(Item item)
        {
            if (item != null)
            {
                if (Utility.Chance(0.8))
                    item.SetItemBool(Item.ItemBoolTable.NormalizeOnLift, true);
                else
                    ;

                AddItem(item);
            }
        }
        public static void DrabLayers(Mobile m)
        {
            try
            {
                Item[] items = new Item[21];
                items[0] = m.FindItemOnLayer(Layer.Shoes);
                items[1] = m.FindItemOnLayer(Layer.Pants);
                items[2] = m.FindItemOnLayer(Layer.Shirt);
                items[3] = m.FindItemOnLayer(Layer.Helm);
                items[4] = m.FindItemOnLayer(Layer.Gloves);
                items[5] = m.FindItemOnLayer(Layer.Neck);
                items[6] = m.FindItemOnLayer(Layer.Waist);
                items[7] = m.FindItemOnLayer(Layer.InnerTorso);
                items[8] = m.FindItemOnLayer(Layer.MiddleTorso);
                items[9] = m.FindItemOnLayer(Layer.Arms);
                items[10] = m.FindItemOnLayer(Layer.Cloak);
                items[11] = m.FindItemOnLayer(Layer.OuterTorso);
                items[12] = m.FindItemOnLayer(Layer.OuterLegs);
                items[13] = m.FindItemOnLayer(Layer.InnerLegs);
                items[14] = m.FindItemOnLayer(Layer.Bracelet);
                items[15] = m.FindItemOnLayer(Layer.Ring);
                items[16] = m.FindItemOnLayer(Layer.Earrings);
                items[17] = m.FindItemOnLayer(Layer.OneHanded);
                items[18] = m.FindItemOnLayer(Layer.TwoHanded);
                items[19] = m.FindItemOnLayer(Layer.Hair);
                items[20] = m.FindItemOnLayer(Layer.FacialHair);
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null && items[i].LootType == LootType.Regular)
                    {

                        items[i].Hue = Utility.RandomList(966, 958, 954);
                    }
                }
            }
            catch (Exception exc)
            {
                System.Console.WriteLine("Exception caught in Mobile.DrabLayers: " + exc.Message);
                System.Console.WriteLine(exc.StackTrace);
            }
        }
        public override void GenerateLoot()
        {
            switch (this)
            {
                case GotWhiteWalker:
                    {   // modified lich lord loot
                        if (Spawning)
                        {
                            PackGold(400, 700);
                        }
                        else
                        {
                            DoRewardArmor(0.24);
                            PackMagicEquipment(2, 3);
                            PackMagicItem(2, 3, 0.10);
                            PackGem(1, .9);
                            PackGem(1, .05);
                            PackScroll(4, 7, .9);
                            PackScroll(4, 7, .05);
                        }
                    }
                    break;
                case GotNightKing:
                    {   // modified balron loot
                        if (Spawning)
                        {
                            PackGold(800, 1200);
                        }
                        else
                        {
                            DoRewardArmor(1);           // 100% chance
                            PackMagicStuff(2, 3, 0.05); // TODO: no idea the level, but we'll guess the highest level
                            PackMagicStuff(2, 3, 0.05);
                            PackScroll(1, 6, .9);
                            PackScroll(1, 6, .5);
                            PackReg(1, .9);
                            PackReg(2, .5);
                        }
                    }
                    break;
                case GotMage:
                case GotWarrior:
                case GotBoneKnight:
                    {   // brigand
                        if (Spawning)
                            PackGold(100, 200);
                    }
                    break;
                default:
                    {
                        if (Spawning)
                            PackGold(50, 75);
                    }
                    break;
            }

        }

        #region Serialization
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
        #endregion Serialization
    }
    public class GotWarrior : BaseGOTCreature
    {
        [Constructable]
        public GotWarrior()
            : base(GOTType.Humanoid_Warrior, GOTClass.Human, DefaultWarriorCreate)
        {
        }

        #region Serialization
        public GotWarrior(Serial serial)
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
        #endregion Serialization
    }
    public class GotMage : BaseGOTCreature
    {
        [Constructable]
        public GotMage()
            : base(GOTType.Humanoid_Mage, GOTClass.Human, DefaultMageCreate)
        {
        }

        #region Serialization
        public GotMage(Serial serial)
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
        #endregion Serialization
    }
    public class GotWhiteWalker : BaseGOTCreature
    {
        public override bool CanReanimateCorpses { get { return true; } }

        [Constructable]
        public GotWhiteWalker()
            : base(GOTType.Humanoid_Mage, GOTClass.WhiteWalker, DefaultMageCreate)
        {
        }

        #region Serialization
        public GotWhiteWalker(Serial serial)
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
        #endregion Serialization
    }
    public class GotWolf : BaseGOTCreature
    {
        public override bool CanRummageCorpses { get { return false; } }
        [Constructable]
        public GotWolf()
            : base(GOTType.Wolf, GOTClass.Creature, DefaultWarriorCreate)
        {
        }

        #region Serialization
        public GotWolf(Serial serial)
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
        #endregion Serialization
    }
    public class GotZombie : BaseGOTCreature
    {
        [Constructable]
        public GotZombie()
            : base(GOTType.Zombie, GOTClass.Creature, DefaultWarriorCreate)
        {
        }

        #region Serialization
        public GotZombie(Serial serial)
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
        #endregion Serialization
    }
    public class GotSkeleton : BaseGOTCreature
    {
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        [Constructable]
        public GotSkeleton()
            : base(GOTType.Skeleton, GOTClass.Creature, DefaultWarriorCreate)
        {
        }

        #region Serialization
        public GotSkeleton(Serial serial)
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
        #endregion Serialization
    }
    public class GotBoneKnight : BaseGOTCreature
    {
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        [Constructable]
        public GotBoneKnight()
            : base(GOTType.BoneKnight, GOTClass.Creature, DefaultWarriorCreate)
        {
        }

        #region Serialization
        public GotBoneKnight(Serial serial)
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
        #endregion Serialization
    }
    public class GotNightKing : BaseGOTCreature
    {
        public override AuraType MyAura { get { return AuraType.Ice; } }
        public override bool CanReanimateCorpses { get { return true; } }

        [Constructable]
        public GotNightKing()
            : base(GOTType.NightKing, GOTClass.NightKing, DefaultHybridCreate)
        {
        }

        private Utility.LocalTimer AnnounceDelay = new Utility.LocalTimer(3000);   // 3 seconds, give the first message
        private int message = 0;
        public override void OnThink()
        {
            base.OnThink();

            if (AnnounceDelay.Triggered)
            {
                AnnounceDelay.Start(20000); // 20 seconds, give the next message
                message++;

                switch (message)
                {
                    case 1:
                        {
                            Decree("I shall bring an eternal night and erase all life and memory");
                            break;
                        }
                    case 2:
                        {
                            Decree("I made them all");
                            break;
                        }
                    case 3:
                        {
                            Decree("I am the afterlife, join me");
                            break;
                        }
                    case 4:
                        {
                            Decree($"Kill {RandomPlayer()}, and I will accept thee");
                            break;
                        }
                    case 5:
                        {
                            if (BiggestProblem() is PlayerMobile pm && Available(pm))
                                Decree($"Kill {pm.Name}, and ye shall live forever with me");
                            break;
                        }
                }
            }

            // he's a hybrid, so he may be casting
            BaseWeapon bw = FindItemOnLayer(Layer.TwoHanded) as BaseWeapon;
            if (bw != null && bw.MagicCharges == 0)
                bw.MagicCharges = 10;
        }
        private bool Available(Mobile m)
        {
            return m is PlayerMobile pm && pm.Alive && this.GetDistanceToSqrt(pm) < 13;
        }
        private string RandomPlayer()
        {
            IPooledEnumerable eable = this.Map.GetMobilesInRange(this.Location, 20);
            foreach (Mobile m in eable)
            {
                if (m is PlayerMobile pm && Available(pm))
                {
                    eable.Free();
                    return pm.Name;
                }
            }
            eable.Free();

            return "the humans";
        }
        private Mobile BiggestProblem()
        {   // top damager
            List<DamageStore> list = GetLootingRights(this.DamageEntries, this.HitsMax);
            SortedList<Mobile, int> Results = BaseCreature.ProcessDamageStore(list);
            foreach (var kvp in Results)
                if (Available(kvp.Key))
                    return kvp.Key;

            return null;
        }
        private void Decree(string text)
        {
            for (int i = 0; i < NetState.Instances.Count; ++i)
            {
                NetState compState = NetState.Instances[i];
                if (compState.Mobile is Mobile m)
                    if (this.GetDistanceToSqrt(compState.Mobile) < 100)
                        // say overhead to all near by, those further away will get a nice system message from the "Night King"
                        //this.SayTo(compState.Mobile, ascii: true, hue: 0, String.Format(text));
                        this.SendAsciiMessageTo(compState, hue: 0, String.Format(text));
            }
        }
        public override bool OnBeforeDeath()
        {
            #region add armor if it was destroyed
            BaseArmor bm = FindItemOnLayer(Layer.InnerTorso) as BaseArmor;
            if (bm == null)
            {
                ChainChest chestArmor = new ChainChest();
                chestArmor.Resource = CraftResource.Dragonglass;
                chestArmor.DurabilityLevel = ArmorDurabilityLevel.Indestructible;
                PackItem(chestArmor);
            }
            #endregion add armor if it was destroyed
            #region seek revenge on the biggest problem
            if (BiggestProblem() is PlayerMobile pm && Available(pm))
            {
                Decree($"You will be your own undoing {pm.Name}!");
                Revenge(pm);
            }
            #endregion seek revenge on the biggest problem
            return base.OnBeforeDeath();
        }
        public void Revenge(Mobile m)
        {
            this.PlaySound(0x24A);  // spirit speak

            Type type = typeof(GotWhiteWalker);
            object o = Activator.CreateInstance(type);
            if (o is BaseGOTCreature bgc)
            {
                bgc.Name = m.Name;
                bgc.FocusMob = m;
                bgc.PreferredFocus = m;
                bgc.Combatant = m;
                bgc.Team = this.Team;
                if (bgc.Body.IsHuman)
                {
                    HoodedShroudOfShadows shroud = new HoodedShroudOfShadows(0);
                    shroud.LootType = LootType.Newbied;
                    bgc.AddItem(shroud);
                }
                bgc.MoveToWorld(m.Location, m.Map);
                bgc.PlaySound(0x214);               // resurrection sound
                bgc.FixedEffect(0x376A, 10, 16);    // resurrection effect
            }
        }
        public override void DoRewardArmor(double chance)
        {
            if (Utility.Chance(chance))
            {
                switch (Utility.Random(10))
                {
                    case 0: PackItem(new WhiteWalkerArmor(), no_scroll: true); break;     // female chest
                    case 1: PackItem(new WhiteWalkerArms(), no_scroll: true); break;      // arms
                    case 2: PackItem(new WhiteWalkerTunic(), no_scroll: true); break;     // male chest
                    case 3: PackItem(new WhiteWalkerGloves(), no_scroll: true); break;    // gloves
                    case 4: PackItem(new WhiteWalkerGorget(), no_scroll: true); break;    // gorget
                    case 5: PackItem(new WhiteWalkerLeggings(), no_scroll: true); break;  // legs
                    case 6: PackItem(new WhiteWalkerHelmet(), no_scroll: true); break;    // helm
                    case 7: PackItem(new WhiteWalkerBustier(), no_scroll: true); break;   //bustier
                    case 8: PackItem(new WhiteWalkerShorts(), no_scroll: true); break;    //shorts
                    case 9: PackItem(new WhiteWalkerSkirt(), no_scroll: true); break;    //skirt
                }
            }
        }
        public override int GoldSplashPile { get { return Utility.RandomMinMax(800, 1200); } }
        public override void DistributeLoot()
        {
            if (this.Map != null)
            {
                BaseChampion.GiveMagicItems(this, magicItems: ChampLootPack.GetChampMagicItems(), specialRewards: ChampLootPack.GetChampSpecialRewards());
                BaseChampion.DoGoodies(this);
            }
        }
        public override bool CanDeactivate
        {   // I am a boss. I speak to others telepathically so that they know my presence - I do not deactivate
            get { return false; }
        }
        #region Serialization
        public GotNightKing(Serial serial)
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
        #endregion Serialization
    }
}