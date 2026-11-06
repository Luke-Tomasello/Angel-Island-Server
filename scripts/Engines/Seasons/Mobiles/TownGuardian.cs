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

/* Scripts\Engines\ChampionSpawn\Champs\Special\TownGuardian.cs
 * CHANGELOG
 *	7/12/2026, Adam
 *	    created - based on Barracoon
 */

using Server.Engines.Alignment;
using Server.Engines.ChampionSpawn;
using Server.Engines.DataRecorder;
using Server.Engines.TerritoryControl;
using Server.Items;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("a dead town guardian")]
    public class TownGuardian : BaseChampion
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.None }); } }
        public override bool AlwaysAttackable => true;

        private string m_districtName = null;
        public override ChampionSkullType SkullType { get { return ChampionSkullType.None; } }
        [CommandProperty(AccessLevel.GameMaster)]
        public override DefensiveSlayer SlayersDefensive { get; set; } = DefensiveSlayer.Repond;

        [CommandProperty(AccessLevel.GameMaster)]
        public string DistrictName
        {
            get => m_districtName;
            set => m_districtName = value;
        }
        [Constructable]
        public TownGuardian(string district)
            : base(AIType.AI_Melee, 0.175, 0.350)
        {
            //BaseSoundID = 0x45A; human?
            IOBAlignment = IOBAlignment.None;
            FightMode = FightMode.Aggressor;

            Title = Utility.RandomList("town guardian");
            BardImmune = (Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules()) ? false : true;

            SetStr(305, 425);
            SetDex(72, 150);
            SetInt(505, 750);

            SetHits(4200);
            SetStam(102, 300);

            SetDamage(25, 35);

            SetSkill(SkillName.MagicResist, 100.0);
            SetSkill(SkillName.Tactics, 97.6, 100.0);
            SetSkill(SkillName.Macing, 97.6, 100.0);
            SetSkill(SkillName.Wrestling, 97.6, 100.0);

            InitBody();
            InitOutfit();

            DistrictName = district;

            Fame = 22500;
            Karma = -22500;

            VirtualArmor = 70;
        }
        public override void InitBody()
        {
            this.Female = false;
            Body = 0x190;
            Hue = 33770;
            Name = NameList.RandomName("male");
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            Item hair = new LongHair();
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new Sandals());
            AddItem(new WarMace());
        }
        #region DraggingMitigation

        public override List<Mobile> GetDraggingMitigationHelpers()
        {
            List<Mobile> helpers = new List<Mobile>();
            // okay, now we take action against this bothersome individual!
            for (int ix = 0; ix < 2; ix++)
            {   // these will be our helpers
                if (Utility.RandomBool())
                    helpers.Add(new OrcishLord());
                else
                    helpers.Add(new OrcishMage());
            }
            return helpers;
        }
        #endregion DraggingMitigation

        public override bool OnBeforeDeath()
        {
            TerritoryDistrict? district = TerritorySystem.FindDistrict(DistrictName);

            if (district != null)
            {
                district.RestartDistrict();

                TerritoryMessenger.ReportDistrictLost(district.Name, district.Map);
            }

            return base.OnBeforeDeath();
        }

        public override void OnDeathLeaderBoards(List<DamageStore> list)
        {
            AwardDeathRewards(
                list,
                awardTownship: true,
                RecorderDomain.PvMPoints,
                RecorderDomain.LaunchEventPoints);
        }
        public override void DistributeLoot()
        {
            if (IsChampion)
            {
                if (this.Map != null)
                {   // give harrower loot, but in champ numbers
                    GiveMagicItems(this, magicItems: ChampLootPack.GetHarrowerMagicItems(), specialRewards: ChampLootPack.GetHarrowerSpecialRewards());
                    DoGoodies(this);
                }
            }
        }
        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            bool useMask = (Core.RuleSets.SeasonsRules(2, 3) || Core.RuleSets.UpheavalRules());

            if (useMask ||
                (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled))
            {
                if (m.Player &&
                    m.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                {
                    return false;
                }
            }

            return base.IsEnemy(m, filter);
        }
        public override void AggressiveAction(Mobile aggressor, bool criminal, object? source = null)
        {
            base.AggressiveAction(aggressor, criminal);

            //if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
            //{
            //    Item item = aggressor.FindItemOnLayer(Layer.Helm);

            //    if (item is OrcishKinMask)
            //    {
            //        AOS.Damage(aggressor, 50, new DamageContext { Source = this });
            //        item.Delete();
            //        aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
            //        aggressor.PlaySound(0x307);
            //    }
            //}
        }
        public override void GenerateLoot()
        {
            if (IsChampion)
            {
                if (Spawning) { }
                else
                {
                    switch (Utility.Random(3))
                    {
                        case 0:
                            {
                                Item deadOrc = new Item(0x3D64);
                                deadOrc.Name = Server.Items.Corpse.GetCorpseName(this);
                                PackItem(deadOrc);
                                break;
                            }
                        case 1:
                            {

                                PackItem(new XmlAddonDeed("a skull of the orc champion"));
                                break;
                            }
                        case 2:
                            {

                                PackItem(new CannonDeed());
                                break;
                            }

                    }
                }
            }
        }
        public override bool AlwaysMurderer { get { return false; } }
        public override Poison PoisonImmune { get { return Poison.Deadly; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(this); } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool ClickTitle { get { return false; } }

        public TownGuardian(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            // version 1
            writer.Write((int)1); // version

            writer.Write(m_districtName);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_districtName = reader.ReadString();
                        break;
                    }
            }
        }
    }
}