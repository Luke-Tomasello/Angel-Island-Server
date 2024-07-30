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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Orc.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/10/04, Froste
 *      Implemented new random IOB drop system and changed drop change to 12%
 *	11/05/04, Pigpen
 *		Made changes for Implementation of IOBSystem. Changes include:
 *		Removed IsEnemy and Aggressive Action Checks. These are now handled in BaseCreature.cs
 *		Set Creature IOBAlignment to Orcish.
 *	9/19/04, Adam
 *		Add IOB drop 5%
 *  9/16/04, Pigpen
 * 		Added IOB Functionality to item OrcishKinHelm
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;
using Server.Misc;

namespace Server.Mobiles
{
    [CorpseName("an orcish corpse")]
    public class Orc : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Orc }); } }

        public override InhumanSpeech SpeechType { get { return InhumanSpeech.Orc; } }

        [Constructable]
        public Orc()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {

            BaseSoundID = 0x45A;
            IOBAlignment = IOBAlignment.Orcish;
            ControlSlots = 1;

            SetStr(96, 120);
            SetDex(81, 105);
            SetInt(36, 60);

            SetHits(58, 72);

            SetDamage(5, 7);

            SetSkill(SkillName.MagicResist, 50.1, 75.0);
            SetSkill(SkillName.Tactics, 55.1, 80.0);
            SetSkill(SkillName.Wrestling, 50.1, 70.0);

            Fame = 1500;
            Karma = -1500;

            InitBody();

            VirtualArmor = 28;
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override int TreasureMapLevel { get { return Core.RuleSets.AllServerRules() ? 1 : 0; } }
        public override int Meat { get { return 1; } }

        public override OppositionGroup OppositionGroup
        {
            get { return OppositionGroup.SavagesAndOrcs; }
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
                if (m.Player && m.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                    return false;

            return base.IsEnemy(m, filter);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            base.AggressiveAction(aggressor, criminal);

            if (!Core.RuleSets.KinSystemEnabled() && !AlignmentSystem.Enabled)
            {
                Item item = aggressor.FindItemOnLayer(Layer.Helm);

                if (item is OrcishKinMask)
                {
                    AOS.Damage(aggressor, 50, 0, 100, 0, 0, 0, item);
                    item.Delete();
                    aggressor.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                    aggressor.PlaySound(0x307);
                }
            }
        }

        public override void InitBody()
        {
            Name = NameList.RandomName("orc");
            Body = 17;
        }
        public override void InitOutfit()
        {

        }

        public Orc(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                switch (Utility.Random(20))
                {
                    case 0: PackItem(new Scimitar()); break;
                    case 1: PackItem(new Katana()); break;
                    case 2: PackItem(new WarMace()); break;
                    case 3: PackItem(new WarHammer()); break;
                    case 4: PackItem(new Kryss()); break;
                    case 5: PackItem(new Pitchfork()); break;
                }

                PackItem(new ThighBoots());
                PackGold(25, 50);

                switch (Utility.Random(3))
                {
                    case 0: PackItem(new Ribs()); break;
                    case 1: PackItem(new Shaft()); break;
                    case 2: PackItem(new Candle()); break;
                }

                if (0.2 > Utility.RandomDouble())
                    PackItem(new BolaBall());

                // Froste: 12% random IOB drop
                if (0.12 > Utility.RandomDouble())
                {
                    Item iob = Loot.RandomIOB();
                    PackItem(iob);
                }

                if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                {
                    // 30% boost to gold
                    PackGold(base.GetGold() / 3);
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20011217145644/uo.stratics.com/hunters/orc.shtml
                    // 0 to 50 Gold, Weapon, Thigh Boots, 1 Raw Ribs (carved)
                    // note: when you open the page for orcish lord from the 2002 stratics archive, you get this 2001 page

                    if (Spawning)
                    {
                        PackGold(0, 50);
                    }
                    else
                    {
                        switch (Utility.Random(20))
                        {
                            case 0: PackItem(new Scimitar()); break;
                            case 1: PackItem(new Katana()); break;
                            case 2: PackItem(new WarMace()); break;
                            case 3: PackItem(new WarHammer()); break;
                            case 4: PackItem(new Kryss()); break;
                            case 5: PackItem(new Pitchfork()); break;
                        }

                        PackItem(new ThighBoots());
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        switch (Utility.Random(20))
                        {
                            case 0: PackItem(new Scimitar()); break;
                            case 1: PackItem(new Katana()); break;
                            case 2: PackItem(new WarMace()); break;
                            case 3: PackItem(new WarHammer()); break;
                            case 4: PackItem(new Kryss()); break;
                            case 5: PackItem(new Pitchfork()); break;
                        }

                        PackItem(new ThighBoots());

                        switch (Utility.Random(3))
                        {
                            case 0: PackItem(new Ribs()); break;
                            case 1: PackItem(new Shaft()); break;
                            case 2: PackItem(new Candle()); break;
                        }

                        // http://www.uoguide.com/Savage_Empire
                        // http://uo.stratics.com/secrets/archive/orcsavage.shtml
                        // Bola balls have appeared as loot on Orc Bombers. Balls on Bombers are rather common, around a 50/50% chance of getting a ball or not. They are only appearing as loot on bombers.
                        if (PublishInfo.PublishDate >= Core.EraSAVE)
                            if (0.2 > Utility.RandomDouble())
                                PackItem(new BolaBall());
                    }

                    AddLoot(LootPack.Meager);
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
    }
}