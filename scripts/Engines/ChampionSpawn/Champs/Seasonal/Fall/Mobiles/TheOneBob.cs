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

/* Scripts\Engines\ChampionSpawn\Champs\Seasonal\Fall\Mobiles\TheOneBob.cs	
 * ChangeLog:
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

using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class TheOneBob : BaseCreature
    {
        private TimeSpan m_SpeechDelay = TimeSpan.FromSeconds(45.0); // time between speech
        public DateTime m_NextSpeechTime;

        [Constructable]
        public TheOneBob()
            : base(AIType.AI_Hybrid, FightMode.All | FightMode.Weakest, 10, 1, 0.2, 0.4)
        {
            BardImmune = true;
            FightStyle = FightStyle.Melee | FightStyle.Magic | FightStyle.Smart | FightStyle.Bless | FightStyle.Curse;
            UsesHumanWeapons = false;
            UsesBandages = true;
            UsesPotions = true;
            CanRun = true;
            CanReveal = true; // magic and smart

            SpeechHue = Utility.RandomSpeechHue();
            Hue = 33770;

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4800);
            SetStam(102, 300);

            SetDamage(25, 35);

            SetSkill(SkillName.EvalInt, 120.0);
            SetSkill(SkillName.Magery, 120.0);
            SetSkill(SkillName.Meditation, 120.0);
            SetSkill(SkillName.MagicResist, 150.0);
            SetSkill(SkillName.Macing, 97.6, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            Fame = 22500;
            Karma = -22500;

            InitBody();
            InitOutfit();

            VirtualArmor = 30;

            m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;

            PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
            PackStrongPotions(6, 12);
            PackItem(new Pouch(), lootType: LootType.UnStealable);

            Club club = new Club();
            club = (Club)Loot.ImbueWeaponOrArmor(noThrottle: true, club, Loot.ImbueLevel.Level6 /*6*/, 0, true);
            club.LootType = LootType.UnStealable;
            if (club != null)
                AddItem(club);
        }
        #region DraggingMitigation
        public override List<Mobile> GetDraggingMitigationHelpers()
        {
            List<Mobile> helpers = new List<Mobile>();
            // okay, now we take action against this bothersome individual!
            for (int ix = 0; ix < 2; ix++)
            {   // these will be out helpers
                if (Utility.RandomBool())
                    helpers.Add(new GolemController()); // these should be dressed like bobs!
                else
                    helpers.Add(new GolemController());
            }
            return helpers;
        }
        #endregion DraggingMitigation
        public TheOneBob(Serial serial)
            : base(serial)
        {
        }

        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 5 : 0; } }
        public override bool AlwaysMurderer { get { return true; } }
        // this prevents players from dragging this champ to town where he can do destruction.
        [CommandProperty(AccessLevel.GameMaster)]
        public override bool GuardIgnore { get { return this.Region != null && this.Region.UId == m_birthRegion; } }
        public override bool CanRummageCorpses { get { return Core.RuleSets.AllShards ? true : false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool ClickTitle { get { return true; } }

        private int m_birthRegion;
        public override void OnBirthRegion(Region Birth)
        {
            if (m_birthRegion != 0)
                ; // debug break
            m_birthRegion = Birth.UId;
            return;
        }
        public override void InitBody()
        {
            this.Female = false;
            Body = 0x190;
            Name = "The One Bob";
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            Robe robe = new Robe(23);
            AddItem(robe);
        }
        public override void OnThink()
        {
            if (DateTime.UtcNow >= m_NextSpeechTime)
            {
                Mobile combatant = this.Combatant;

                if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 8))
                {
                    int phrase = Utility.Random(5);

                    switch (phrase)
                    {
                        case 0: this.Say(true, "Bring it."); break;
                        case 1: this.Say(true, "As long as you die in the end, I win."); break;
                        case 2: this.Say(true, "You're lucky I don't have purple potions."); break;
                        case 3: this.Say(true, "Bobs > you newbs"); break;
                        case 4: this.Say(true, "Yeah, lol"); break;
                    }

                    m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;
                }

                base.OnThink();
            }

        }
        public override void OnDamagedBySpell(Mobile caster)
        {
            if (caster == this)
                return;

            // Adam: 12% chance to spawn a bob
            if (Utility.RandomChance(12))
                SpawnBob(caster);
        }
        public void SpawnBob(Mobile caster)
        {
            Mobile target = caster;

            if (Map == null || Map == Map.Internal)
                return;

            int helpers = 0;
            ArrayList mobs = new ArrayList();
            IPooledEnumerable eable = this.GetMobilesInRange(10);
            foreach (Mobile m in eable)
            {
                if (m is Bob)
                    ++helpers;

                if (m is PlayerMobile && m.Alive == true && m.Hidden == false && m.AccessLevel <= AccessLevel.Player)
                    mobs.Add(m);
            }
            eable.Free();

            if (helpers < 5)
            {
                BaseCreature helper = new Bob();

                helper.Team = this.Team;
                helper.Map = Map;
                helper.GuardIgnore = true;

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
        public override int GoldSplashPile { get { return Utility.RandomMinMax(800, 1200); } }

        public override void GenerateLoot()
        {
            if (IsChampion)
            {
                if (Spawning)
                {
                    // No at spawn loot
                }
                else
                {
                    int phrase = Utility.Random(2);
                    switch (phrase)
                    {
                        case 0: this.Say(true, "Bobs for Bob!"); break;
                        case 1: this.Say(true, "Bob will rise!"); break;
                    }

                    // make the magic key
                    Key key = new Key(KeyType.Magic);
                    key.KeyValue = Key.RandomValue();

                    // make the magic box
                    MagicBox MagicBox = new MagicBox();
                    MagicBox.Movable = true;
                    MagicBox.KeyValue = key.KeyValue;
                    MagicBox.DropItem(key);

                    PackItem(MagicBox);

                    // add bob's pillow
                    Item pillow = new Item(Utility.RandomList(5029, 5030, 5031, 5032, 5033, 5034, 5035, 5036, 5037, 5038));
                    pillow.Hue = 23;
                    pillow.Name = "Pillow of The One Bob";
                    pillow.Weight = 1.0;

                    PackItem(pillow);
                }
            }
        }
        public override void Damage(int amount, Mobile from, object source_weapon)
        {
            Mobile combatant = this.Combatant;

            if (combatant != null && combatant.Map == this.Map && combatant.InRange(this, 8))
            {
                if (Utility.RandomBool())
                {

                    /*                    int phrase = Utility.Random(4);

										switch (phrase)
										{
											case 0: this.Say(true, "Har! The mackerel wiggles!"); break;
											case 1: this.Say(true, "Ye stink like a rotten clam! Bring it on yet!?"); break;
											case 2: this.Say(true, "Arr, treacherous monkey!"); break;
											case 3: this.Say(true, "Ye'll not get my swag!"); break;
										}
                    
										m_NextSpeechTime = DateTime.UtcNow + m_SpeechDelay;*/
                }
            }

            base.Damage(amount, from, source_weapon);
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            for (int i = 0; i < list.Count; ++i)
            {
                if (list[i] is ContextMenus.PaperdollEntry)
                    list.RemoveAt(i--);
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    writer.Write(m_birthRegion);
                    goto case 0;
                case 0:
                    break;
            }

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    m_birthRegion = reader.ReadInt();
                    goto case 0;
                case 0:
                    break;
            }
        }
    }
}