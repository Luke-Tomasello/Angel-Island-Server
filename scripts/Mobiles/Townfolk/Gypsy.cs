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

/* Scripts/Mobiles/Townfolk/Gypsy.cs
 * ChangeLog
 *	2/16/11, adam
 *		don't allow profitable farming of blue townsfolk from a region which is usually guarded.
 *		Note: murders already only get 1/3 of creatures loot, so this is a double whammy for them
 *	4/2/10, adam
 *		Move instrument to GenerateLoot() so that it can be suppressed in BaseCreature.SuppressNormalLoot
 *  07/02/06, Kit
 *		InitOutFit/Body overrides
 *	5/26/04, Pixie
 *		Changed to use the new CollectBounty() call in BountyKeeper.
 *	5/23/04 created by smerX
 *
 */

using Server.BountySystem;
using Server.Diagnostics;
using Server.Items;
using System;

namespace Server.Mobiles
{
    public class Gypsy : BaseCreature
    {

        [Constructable]
        public Gypsy()
            : base(AIType.AI_Melee, FightMode.Aggressor, 22, 1, 0.2, 1.0)
        {
            InitBody();
            InitOutfit();
            Title = "the Gypsy";
        }

        public override void InitBody()
        {
            SetStr(90, 100);
            SetDex(90, 100);
            SetInt(15, 25);

            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 401;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 400;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            AddItem(new Shirt(Utility.RandomNeutralHue()));
            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new TwoPigTails(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new KrisnaHair(Utility.RandomHairHue())); break;
            }

            if (Core.RuleSets.AngelIslandRules())
                PackGold(26);
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                switch (Utility.Random(4))
                {
                    case 0: PackItem(new Drums()); break;
                    case 1: PackItem(new Harp()); break;
                    case 2: PackItem(new Lute()); break;
                    case 3: PackItem(new Tambourine()); break;
                }
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // no stratics loot page for this mob
                    // go with nothing for SP since RUNUO gives so much
                    if (Spawning)
                    {
                        if (Core.RuleSets.MortalisRules())
                            PackGold(250, 300);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    if (Spawning)
                    {
                        PackGold(250, 300);
                    }
                }
            }
        }

        public override bool OnBeforeDeath()
        {
            bool obd = base.OnBeforeDeath();

            // don't allow profitable farming of blue townsfolk from a region which is usually guarded.
            //	Note: murders already only get 1/3 of creatures loot, so this is a double whammy for them
            if (obd && Core.RuleSets.MortalisRules())
                if (!(this.Spawner != null && Region.Find(this.Spawner.Location, this.Spawner.Map) as Regions.GuardedRegion != null && Region.Find(this.Spawner.Location, this.Spawner.Map).IsGuarded))
                {
                    // first find out how much gold this creature is dropping
                    int MobGold = this.GetGold();

                    // reds get 1/3 of usual gold
                    int NewGold = MobGold / 3;

                    // first delete all dropped gold
                    Container pack = this.Backpack;
                    if (pack != null)
                    {
                        // how much gold is on the creature?
                        Item[] golds = pack.FindItemsByType(typeof(Gold), true);
                        foreach (Item g in golds)
                        {
                            pack.RemoveItem(g);
                            g.Delete();
                        }

                        this.PackGold(NewGold);
                    }
                }

            return obd;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            bool bReturn = false;
            try
            {
                if (dropped is Head)
                {
                    int result = 0;
                    int goldGiven = 0;
                    bReturn = BountyKeeper.CollectBounty((Head)dropped, from, this, ref goldGiven, ref result);
                    switch (result)
                    {
                        case -2:
                            Say("Pft.. I don't want that.");
                            break;
                        case -3:
                            Say("Haha, yeah right..");
                            Say("I'll take that head, you just run along now.");
                            break;
                        case -4: //good, gold given
                            Say(string.Format("I thank you for the business.  Here's the reward of {0} gold!", goldGiven));
                            break;
                        case -5:
                            Say(string.Format("I thank you for the business, but the bounty has already been collected."));
                            break;
                        default:
                            if (bReturn)
                            {
                                Say("I'll take that.");
                            }
                            else
                            {
                                Say("I don't want that.");
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Error (nonfatal) in Gypsy.OnDragDrop(): " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
            return bReturn;
        }

        public Gypsy(Serial serial)
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
    }
}