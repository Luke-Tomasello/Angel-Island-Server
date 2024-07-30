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

/* Scripts/Mobiles/Monsters/Humanoid/Magic/EvilMage.cs
 * ChangeLog
 *  2/2/2024, Adam
 *      Remove the title. The Name includes the title already.
 *  12/03/06 Taran Kain
 *      Set Female to false. No trannies!
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *	4/13/05, Kit
 *		Switch to new region specific loot model
 *	12/25/04, Adam
 *		Change ControlSlots from 3 to 2
 *	12/11/04, Pix
 *		Changed ControlSlots for IOBF.
 *  11/16/04, Froste
 *      Changed IOBAlignment to Council
 *  11/10/04, Froste
 *      Implemented new random IOB drop system and changed drop change to 12%
 *	11/05/04, Pigpen
 *		Made changes for Implementation of IOBSystem. Changes include:
 *		Removed IsEnemy and Aggressive Action Checks. These are now handled in BaseCreature.cs
 *		Set Creature IOBAlignment to Undead.
 *	11/2/04, Adam
 *		Increase gold if this is IOB mobile resides in it's Stronghold (Wind)
 *	9/26/04, Adam
 *		Add 5% IOB drop (BloodDrenchedBandana)
 *	7/21/04, mith
 *		IsEnemy() and AggressiveAction() code added to support Brethren property of BloodDrenchedBandana.
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  10/6/04, Froste
 *		New Fall Fashions!!
 */

using Server.Engines.Alignment;
using Server.Engines.IOBSystem;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("an evil mage corpse")]
    public class EvilMage : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Council }); } }

        // Blackthorn's Revenge is when we got the Todd McFarlane (crap) bodies
        //	Blackthorn's Revenge was release 2/12/2002 according to UOGuide.
        // Since Publish 15 was 1/9/2002, we can safely exclude Todd McFarlane bodies pre Publish 15
        private bool Blackthorns_Revenge = (PublishInfo.Publish > 15 && (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules()) == false);

        [Constructable]
        public EvilMage()
            : base(AIType.AI_Mage, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {

            Hue = Utility.RandomSkinHue();  //new skin color

            IOBAlignment = IOBAlignment.Council;
            ControlSlots = 2;

            SetStr(81, 105);
            SetDex(91, 115);
            SetInt(96, 120);

            SetHits(49, 63);

            SetDamage(5, 10);

            SetSkill(SkillName.EvalInt, 75.1, 100.0);
            SetSkill(SkillName.Magery, 75.1, 100.0);
            SetSkill(SkillName.MagicResist, 75.0, 97.5);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 20.2, 60.0);

            Fame = 2500;
            Karma = -2500;

            InitBody();
            InitOutfit();

            VirtualArmor = 16;
        }

        public override bool CanRummageCorpses { get { return Core.RuleSets.AngelIslandRules() ? true : true; } }
        public override bool AlwaysMurderer { get { return true; } }
        public override int Meat { get { return 1; } }

        public EvilMage(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            Name = NameList.RandomName("evil mage");
            Female = false;

            if (Blackthorns_Revenge)
                Body = 124;             // Todd McFarlane (crap) bodies
            else
                Body = 0x190;
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            if (Core.RuleSets.AngelIslandRules())
            {
                AddItem(new Sandals());

                Item EvilMageRobe = new Robe();
                EvilMageRobe.Hue = 0x1;
                EvilMageRobe.LootType = LootType.Newbied;
                AddItem(EvilMageRobe);

                Item BDB = new BloodDrenchedBandana();
                BDB.LootType = LootType.Newbied;
                AddItem(BDB);

                Item Cloak = new Cloak();
                Cloak.Hue = 0x1;
                Cloak.LootType = LootType.Newbied;
                AddItem(Cloak);

                Item Bracelet = new GoldBracelet();
                Bracelet.LootType = LootType.Newbied;
                AddItem(Bracelet);

                Item Ring = new GoldRing();
                Ring.LootType = LootType.Newbied;
                AddItem(Ring);

                Item hair = new LongHair();
                hair.Hue = 0x47E;
                hair.Layer = Layer.Hair;
                hair.Movable = false;
                AddItem(hair);

                Item beard = new Goatee();
                beard.Hue = 0x47E;
                beard.Movable = false;
                AddItem(beard);
            }
            else
            {
                if (Blackthorns_Revenge == false)
                {   // not Todd's graphics, so we need to dress
                    AddItem(new Robe(Utility.RandomRedHue())); // TODO: Proper hue

                    // Don't think we should drop the sandals .. stratics is unclear when it comes to clothes.
                    // http://web.archive.org/web/20020207054748/uo.stratics.com/hunters/evilmage.shtml
                    // 	Red Robe: 0 to 50 Gold, Scrolls (circles 1-7), Reagents
                    /*Sandals shoes = new Sandals();
					if (Core.UOSP || Core.UOMO)
						shoes.LootType = LootType.Newbied;
					AddItem(shoes);*/


                    /* Publish 4
					 * Shopkeeper Changes
					 * NPC shopkeepers will no longer have colored sandals. Evil NPC Mages will carry these items.
					 */
                    if (PublishInfo.Publish >= 4)
                    {
                        // http://forums.uosecondage.com/viewtopic.php?f=8&t=22266
                        // runuo.com/community/threads/evil-mage-hues.91540/
                        if (0.18 >= Utility.RandomDouble())
                            AddItem(new Shoes(Utility.RandomRedHue()));
                        else
                            AddItem(new Sandals(Utility.RandomRedHue()));
                    }
                    else
                        AddItem(new Sandals());
                }

                Item hair = null;
                switch (Utility.Random(4))
                {
                    case 0: //  bald
                        break;
                    case 1:
                        hair = new ShortHair();
                        break;
                    case 2:
                        hair = new LongHair();
                        break;
                    case 3:
                        hair = new ReceedingHair();
                        break;
                }

                if (hair != null)
                {
                    hair.Hue = Utility.RandomHairHue();
                    hair.Layer = Layer.Hair;
                    hair.Movable = false;
                    AddItem(hair);
                }

                Item beard = null;
                switch (Utility.Random(4))
                {
                    case 0: //  clean shaven
                        break;
                    case 1:
                        beard = new LongBeard();
                        break;
                    case 2:
                        beard = new ShortBeard();
                        break;
                    case 3:
                        beard = new MediumLongBeard();
                        break;
                    case 4:
                        beard = new MediumShortBeard();
                        break;
                }

                if (beard != null)
                {
                    beard.Hue = (hair != null) ? hair.Hue : Utility.RandomHairHue(); // do the drapes match the carpet?
                    beard.Movable = false;
                    beard.Layer = Layer.FacialHair;
                    AddItem(beard);
                }
            }
        }
        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackReg(6);
                PackScroll(2, 7);
                PackItem(new Robe(Utility.RandomPinkHue())); // Former AddItem moved to the loot section

                // pack the gold
                PackGold(50, 100);

                // Froste: 12% random IOB drop
                if (0.12 > Utility.RandomDouble())
                {
                    Item iob = Loot.RandomIOB();
                    PackItem(iob);
                }

                // Category 2 MID
                PackMagicItem(1, 1, 0.05);

                if (IOBRegions.GetIOBStronghold(this) == IOBAlignment)
                {
                    // 30% boost to gold
                    PackGold(base.GetGold() / 3);
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020207054748/uo.stratics.com/hunters/evilmage.shtml
                    // 	Red Robe: 0 to 50 Gold, Scrolls (circles 1-7), Reagents

                    if (Spawning)
                    {
                        PackGold(0, 50);
                    }
                    else
                    {
                        if (Blackthorns_Revenge == true)
                        {   // don't need to dress Todd's graphics - just add as loot
                            PackItem(new Robe(Utility.RandomRedHue())); // TODO: Proper hue
                                                                        // sandals removed (unsure)
                        }

                        PackScroll(1, 7);
                        PackReg(6);
                    }
                }
                else
                {
                    if (Spawning)
                    {
                        PackReg(6);

                        if (Blackthorns_Revenge == true)
                        {   // don't need to dress Todd's graphics - just add as loot
                            PackItem(new Robe(Utility.RandomRedHue())); // TODO: Proper hue
                            PackItem(new Sandals());
                        }
                    }

                    AddLoot(LootPack.Average);
                    AddLoot(LootPack.MedScrolls);
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