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

/* Items/Containers/DungeonTreasureChest.cs
 * ChangeLog:
 *  8/14/2023, Adam (LifeSpan(TimeSpan life))
 *      Add LifeSpan so Camps can control when the chest decays.
 *  6/21/2023, Adam
 *      Cap chest level at 4 for all shards but Angel Island
 *  5/27/2023, Adam
 *      Turn off chest guardians if not Angel Island.
 *  5/19/23, Adam (SiegeStyleRules())
 *   Add these items to dungeon chests
 *   https://web.archive.org/web/20020808081919/http://uo.stratics.com:80/thb/info/dungeon/dungeonguide.shtml 
 *  12/22/21, Adam (IsIntMapStorage)
 *      Set the chest to m_Item.IsIntMapStorage = false before deleting (this is on for XmarksTheSpot chests)
 *      This was done to eliminate unnecessary logging (of IsIntMapStorage items being deleted.)
 *	11/8/21, Yoar
 *	    Added call to XMarksTheSpot.Bury: Dungeon treasure chests may now be buried on spawn.
 *	7/14/10, adam
 *		o revert multi-piles of gold until we can balance the lift-risk
 *		o soften the pirates magic ability (based on level) so that he's got less of a chance at casting reveal
 *		o soften the pirates detect hidden skill (based on level)
 *	7/11/10, adam
 *		o swap out local copy of 'lift memory' and replace it with the shared version in Utilities
 *		o Split gold into level*2 piles to increase the chance of getting revealed
 *	5/23/10, Adam
 *		In CheckLift(), thwart lift macros by checking the per-player 'lift memory'
 *	3/5/09, Adam
 *		MonsterStatuette drops cut in 1/2
 *		skin cream drops cut by 3/4
 *	11/12/08, Adam
 *		- Set the Detect Hidden of the chest Guardian to match the players hiding so that they have a fighting chance
 *		- Thwart �fast lifting� in CheckLift: �You thrust your hand into the chest but come up empty handed.�
 *		- Remove IOB checks from the CheckThief() and CheckGuardian() functions
 *		  - CheckThief() now checks the IOB alignment in the calling logic
 *		  - CheckGuardian() does not care if you are aligned or not
 *  11/11/08. Adam
 *      - Fix a bug in CheckGuardian() that was preventing any chance of being caught
 *      - turn on Reveal and Run for the Guardian (Uses memory)
 *      - Switch fight mode to Aggressor mode for Guardians
 *	11/10/08, Adam
 *		Reduce the drop level of level 5 chests about 50%
 *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *	12/8/07, Pix
 *		Moved check up in PackMagicItem() so we don't create the item if we don't need it
 *			(and thus it's not left on the internal map)
 *  2/28/07, Adam
 *      Allow either explosion OR poison for the trap
 *  2/1/07, Adam
 *      - repair reveal logic for OnItemLifted to be CheckGuardian% OR normal reveal%, not both.
 *      - Reduce the chance for the guardian to catch you by a small amount based on the number of monsters around you.
 *  1/30/07, Adam
 *      Add the new SkinHueCreme for level 4&5 chests
 *  1/28/07, Adam
 *      Have the Guardian recall away (decay) when the chest decays
 *  1/26/07, Adam
 *      - new dynamic property system
 *      - Add new Dynamic Properties for identification and speach text
 *	1/25/07, Adam
 *      Set the new BaseCreature.LifespanMinutes to change the lifespan and refresh scope
 *	1/25/07, Adam
 *		- add a new short lived, non-aligned pirate to guard this pirate booty
 *		- 30% of the time, for each item lifted, you will incur the wrath of the guardian
 *      - the guardian's stats is proportioned to the chest level
 *	1/9/06, Adam
 *		It's no longer required that an aligned player be wearing their IOB to get caught stealing from their kin.
 *		Detail: Removed the requirement for pm.IOBEquipped in CheckThief()
 *	4/24/05, Adam
 *		I adjusted the low-end of the gold generation to greater than high-end of the previous chest level.
 *		(level 5 gold will always be greater than level 4.)
 *		MIN is now 75% of max
 *	4/17/04, Adam
 *		Cleanup monster statue drop
 *	03/28/04, Kitaras
 *		Added Check to CheckThief to prevent controled pets with iob alignment
 *		from setting off "you have been noticed stealing from you kin"
 *	12/05/04, Adam
 *		Crank down chest loot MIN so as to decrease daily take home
 *  12/05/04, Jade
 *      Changed chance to drop a t-map to 1%
 *	11/20/04, Adam
 *		1. add level 0 (trainer chests)
 *		2. Add CheckThief() method to OnItemLifted() to see if you are stealing from your kin!
 *	11/10/04, Adam
 *		change treasure map chest loot to (level * 1000) / 3 MAX
 *	10/14/04, Adam
 *		Increase difficulty:
 *		change TrapSensitivity = 1.0 to TrapSensitivity = 1.5
 *			This will for example make a 10% chance to set off the trap 15% given:
 *			100 trap power and 20 disarm skill
 *		we want to reveal the looter about level * 3% of the time (per item looted)
 *		for chest levels 1-5, this works out to: 3%, 6%, 9%, 12%, 15%
 *  8/9/04, Pixie
 *		Changed the damage done when the trap is tripped on a disarm failure.
 *  8/8/04, Pixie
 *		Added functionality for tripping the trap if you fail to disarm it.
 *	8/3/04, Adam
 *		we want to reveal the looter about level * 2.5% of the time (per item looted)
 *		for chest levels 1-5, this works out to: 2.5%, 5%, 7.5%, 10%, 12.5%
 *	7/11/05, Adam
 *		1. Decrease tmap drops from 20% to 5%
 *		2. Decrease statue drops from 5%, 6%, and 7% ==> 3%, 4%, and 5%
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *	7/3/04, Adam - Magic Item Drop (MID)
 *		1. Set MID to level number * 70% chance to drop.
 *		2. Scale MID Intensity based on chest level.
 *		3. Bonus drop: level number * 7% chance to drop
 *			(level 3 intensity item.)
 *	7/3/04, Adam
 *		1. Change monster statue drop from 20% to max 7%
 *		level 3 = 5%, level 4 = 6%, level 5 = 7%
 *	6/29/04, Adam
 *		1. Changed to drop scrolls appropriate for the level.
 *		Added PackScroll procedures
 *		2. give a 2.5% chance to reveal the looter (per item removed)
 *		3. Add the "You have been revealed!" message
 *		4. Only show the message if the looter is hidden
 *	6/27/04, adam
 *		Massive cleanup: remove weapons and armor, Add tmaps, monster statues
 *			and magic jewelry, and magic clothing, ...
 *	6/25/04, adam
 *		Copy from TreasureMapChest and update to be correct levels for dungeons
 *		(should be a subclass)
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	5/19/04, pixie
 *		Modifies so the trap resets when it is tripped.
 *		Now the only way to access the items inside is by removing the
 *		trap with the Remove Trap skill.
 *	4/30/04, mith
 *		modified the chances for getting high end weapons/armor based on treasure chest level.
 *   4/27/2004, pixie
 *     Changed so telekinesis doesn't trip the trap
 */

using Server.Diagnostics;
using Server.Engines.Alignment;
using Server.Engines.DataRecorder;
using Server.Engines.PartySystem;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Fourth;
using System;
using System.Collections;
using static Server.Utility;

namespace Server.Items
{
    [FlipableAttribute(0xE41, 0xE40)]
    public class DungeonTreasureChest : LockableContainer
    {
        #region AI Loot
        // 23 unique statues 
        static MonsterStatuetteType[] level3 = new MonsterStatuetteType[]
        {
            MonsterStatuetteType.Crocodile,
            MonsterStatuetteType.Daemon,
            MonsterStatuetteType.Dragon,
            MonsterStatuetteType.EarthElemental,
            MonsterStatuetteType.Ettin,
            MonsterStatuetteType.Gargoyle,
            MonsterStatuetteType.Gorilla
        };

        static MonsterStatuetteType[] level4 = new MonsterStatuetteType[]
        {
            MonsterStatuetteType.Lich,
            MonsterStatuetteType.Lizardman,
            MonsterStatuetteType.Ogre,
            MonsterStatuetteType.Orc,
            MonsterStatuetteType.Ratman,
            MonsterStatuetteType.Skeleton,
            MonsterStatuetteType.Troll
        };

        static MonsterStatuetteType[] level5 = new MonsterStatuetteType[]
        {
            MonsterStatuetteType.Cow,
            MonsterStatuetteType.Zombie,
            MonsterStatuetteType.Llama,
            MonsterStatuetteType.Ophidian,
            MonsterStatuetteType.Reaper,
            MonsterStatuetteType.Mongbat,
            MonsterStatuetteType.Gazer,
            MonsterStatuetteType.FireElemental,
            MonsterStatuetteType.Wolf
        };

        static MonsterStatuetteType[][] m_monsters = new MonsterStatuetteType[][]
            {
                level3, level4, level5
            };
        #endregion AI Loot

        private int m_Level;
        private DateTime m_DeleteTime;
        private Timer m_Timer;
        private Mobile m_Owner;
        private Mobile m_Guardian;
        private Item m_XItem;
        private bool m_HasStartedQuickDecay = false;

        // We generate our own items on dupe, therefore DeepDupe need not dupe our items
        public override bool AutoFills { get { return true; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; } }

        //[CommandProperty(AccessLevel.GameMaster)]
        //public Mobile Owner { get { return m_Owner; } set { m_Owner = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Guardian { get { return m_Guardian; } set { m_Guardian = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime DeleteTime { get { return m_DeleteTime; } }

        public override double TrapSensitivity { get { return 1.5; } }

        [Constructable]
        public DungeonTreasureChest(int level)
            : this(null, level)
        {
        }

        public DungeonTreasureChest(Mobile owner, int level)
            : base(0xE41)
        {
            m_Owner = owner;
            m_Level = Core.RuleSets.AngelIslandRules() ? level : level > 4 ? 4 : level;

            // adam: usual decay time is 3 hours
            //	See also: ExecuteTrap() where decay starts
            m_DeleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(3.0 * 60.0);

            m_Timer = new DeleteTimer(this, m_DeleteTime);
            m_Timer.Start();

            InitializeTrap(this, m_Level);
            Fill(this, m_Level);
        }

        public TimeSpan Lifespan
        {
            get { return m_DeleteTime - DateTime.UtcNow; }
            set
            {
                if (m_Timer != null)
                {
                    if (m_Timer.Running)
                    {
                        m_Timer.Flush();
                        m_Timer.Stop();
                        m_Timer = null;
                    }
                }

                m_DeleteTime = DateTime.UtcNow + value;
                m_Timer = new DeleteTimer(this, m_DeleteTime);
                m_Timer.Start();
            }
        }
        public override int DefaultGumpID { get { return 0x42; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }
        public static int StandardGold(int level)
        {
            /* https://web.archive.org/web/20011218055051/http://uo.stratics.com:80/thb/info/dungeon/dungeonguide.shtml
            level 1 chest requires 52 lockpicking and contains from 0 to 60 gp
            level 2 chest requires 72 lockpicking and contains from 98 to 125 gp
            level 3 chest requires 84 lockpicking and contains from 200 to 350 gp
            level 4 chest requires 92 lockpicking and contains from 500 to 900 gp
            */
            switch (level)
            {
                case 0: return 0;
                case 1: return Utility.RandomMinMax(0, 60);
                case 2: return Utility.RandomMinMax(98, 125);
                case 3: return Utility.RandomMinMax(200, 350);
                default: // we have dungeon chest spawners that generate level 5 and 6. We map to 4 for Siege
                case 4: return Utility.RandomMinMax(500, 900);
            }
        }
        public static void InitializeTrap(LockableContainer cont, int level)
        {
            bool UberChest = false;
            if (level >= 6) { level = 5; UberChest = true; }
            int trainerLevel = level > 0 ? 0 : Utility.RandomMinMax(0, 2);
            cont.Movable = false;
            cont.TrapType = level > 0 ?
                Utility.RandomList<TrapType>(new TrapType[] { TrapType.PoisonTrap, TrapType.ExplosionTrap }) :
                Utility.RandomList<TrapType>(new TrapType[] { TrapType.PoisonTrap, TrapType.ExplosionTrap, TrapType.DartTrap });
            cont.TrapPower = level > 0 ? level * 25 : trainerLevel;
            cont.TrapLevel = level > 0 ? level : trainerLevel;
            cont.Locked = true;
            const int TrainerStart = 0;   // the magic number at which a new player has a chance to pick at zero skill

            switch (level)
            {
                // Adam: add level 0 (trainer chests). Roughly a 1 in 3 chance at getting a chest you have a chance to pick at zero skill
                //  The player will be able to gain on these trainer chests all the way up to Level 1 chests.
                //  Somewhere around ~50 skill, the player will quit gaining on these chests.
                case 0: cont.RequiredSkill = Core.RuleSets.AngelIslandRules() ? Utility.RandomMinMax(30, 37) : Utility.RandomMinMax(TrainerStart, TrainerStart + 3); break;
                case 1: cont.RequiredSkill = 52; break;
                case 2: cont.RequiredSkill = 72; break;
                case 3: cont.RequiredSkill = 84; break;
                case 4: cont.RequiredSkill = 92; break;
                case 5: cont.RequiredSkill = 100; break;
            }

            if (trainerLevel > 0)
            {
                cont.LockLevel = cont.RequiredSkill;
                cont.MaxLockLevel = cont.RequiredSkill + 40;
            }
            else
            {
                cont.LockLevel = cont.RequiredSkill - 10;
                cont.MaxLockLevel = cont.RequiredSkill + 40;
            }
        }
        public static void Fill(LockableContainer cont, int level)
        {
            bool UberChest = false;
            if (level >= 6) { level = 5; UberChest = true; }

            // adam: change treasure map chest loot MIN-MAX so as to decrease daily take home
            if (level != 0)
            {
                int amount = 0;

                if (Core.RuleSets.AngelIslandRules())
                {
                    amount = Utility.RandomMinMax(
                            (int)(((double)((level * 1000) / 3)) * .75), // min is 75% of MAX
                            (level * 1000) / 3);
                    if (UberChest)
                        amount += 1000;
                }
                else
                    // Siege etc.
                    amount = StandardGold(level);

                int[] table = null;
                if (amount < 500)
                    table = new int[1] { amount };
                else
                    table = Utility.RandomNSumM(level, amount);

                #region Debug Verifier
#if false
                Utility.Monitor.DebugOut(string.Format("Dropping:{0} piles totaling:{1} Match:{2}", table.Length, amount, table.Sum() == amount), ConsoleColor.Green);
                if (table.Sum() != amount)
                    Utility.Monitor.DebugOut(string.Format("Table:{0} != amount:{1}", table.Length, amount), ConsoleColor.Red);
#endif
                #endregion Debug Verifier

                for (int ix = 0; ix < table.Length; ix++)
                    cont.DropItem(new Gold(table[ix]));
            }

            if (Core.RuleSets.AngelIslandRules())
            {
                // skin tone creme for level 4 & 5 chests
                if (Utility.RandomDouble() < 0.05 && level > 3)
                {
                    cont.DropItem(new SkinHueCreme());
                }

                // adam: scrolls * 3 and not 5
                for (int i = 0; i < level * 3; ++i)
                {
                    int minCircle = level;
                    int maxCircle = (level + 3);
                    PackScroll(cont, minCircle, maxCircle);
                }

                // plus "level chances" for magic jewelry & clothing
                switch (level)
                {
                    case 0: // Adam: trainer chest
                    case 1: // none
                        break;
                    case 2:
                        PackMagicItem(cont, 1, 1, 0.05);
                        break;
                    case 3:
                        PackMagicItem(cont, 1, 2, 0.10);
                        PackMagicItem(cont, 1, 2, 0.05);
                        break;
                    case 4:
                        PackMagicItem(cont, 2, 3, 0.10);
                        PackMagicItem(cont, 2, 3, 0.05);
                        PackMagicItem(cont, 2, 3, 0.02);
                        break;
                    case 5:
                        PackMagicItem(cont, 3, 3, 0.10);
                        PackMagicItem(cont, 3, 3, 0.05);
                        PackMagicItem(cont, 3, 3, 0.02);
                        break;
                }

                // TreasureMap( int level, Map map
                //	5% chance to get a treasure map
                //  Changed chance for tmap to 1%
                if (level != 0)
                    if (Utility.RandomDouble() < 0.01)
                    {
                        int mlevel = level;

                        //	20% chance to get a treasure map one level better than the level of this chest
                        if (Utility.RandomDouble() < 0.20)
                            mlevel += (level < 5) ? 1 : 0;  // bump up the map level by one

                        TreasureMap map = new TreasureMap(mlevel, Map.Felucca);
                        cont.DropItem(map);             // drop it baby!
                    }

                // if You're doing a level 3, 4, or 5 chest you have a 1.5%, 2%, or 2.5% chance to get a monster statue
                double chance = 0.00 + (((double)level) * 0.005);
                if ((level > 3) && (Utility.RandomDouble() < chance))
                {
                    int ndx = level - 3;
                    MonsterStatuette mx =
                        new MonsterStatuette(m_monsters[ndx][Utility.Random(m_monsters[ndx].Length)]);
                    mx.LootType = LootType.Regular;                 // not blessed
                    cont.DropItem(mx);                          // drop it baby!
                }

                #region RareFactoryItem
                if (UberChest)
                {
                    cont.DropItem(Loot.RareFactoryItem(.5, Loot.RareType.DungeonChestDropL6));
                }
                #endregion RareFactoryItem

                TreasureMapChest.PackRegs(cont, level * 10);
                TreasureMapChest.PackGems(cont, level * 5);
            }
            else
            {   // standard Shards
                //https://web.archive.org/web/20020808081919/http://uo.stratics.com:80/thb/info/dungeon/dungeonguide.shtml
                switch (level)
                {
                    default: break; // only levels 1-4 on normal shards.
                    case 1:
                        if (Utility.Random(3) != 0)
                        {   // normal level one
                            // 10 bolts, gems, non-magical weapons, armor, clothing and jewelry.
                            cont.DropItem(new Bolt(10)); TreasureMapChest.PackGems(cont, level * 10);
                            cont.DropItem(Loot.RandomWeapon()); if (Utility.Random(3) == 0) cont.DropItem(Loot.RandomWeapon());
                            cont.DropItem(Loot.RandomArmor()); if (Utility.Random(3) == 0) cont.DropItem(Loot.RandomArmor());
                            cont.DropItem(Loot.RandomClothing()); if (Utility.Random(3) == 0) cont.DropItem(Loot.RandomClothing());
                            cont.DropItem(Loot.RandomJewelry());
                        }
                        else
                        {   // Hybrid level one
                            // 5 bolts, shoes, sandals, candles, bottles of ale or liquor, jugs of cider.
                            cont.DropItem(new Bolt(5)); cont.DropItem(Utility.RandomBool() ? new Shoes() : new Sandals());
                            cont.DropItem(new Candle());
                            for (int ix = Utility.RandomMinMax(1, 2); ix > 0; ix--)
                                cont.DropItem(Utility.RandomBool() ? new FillableBvrge(1, typeof(BeverageBottle), BeverageType.Ale).Construct() : new FillableBvrge(1, typeof(BeverageBottle), BeverageType.Liquor).Construct());
                            for (int ix = Utility.RandomMinMax(1, 2); ix > 0; ix--)
                                cont.DropItem(new FillableBvrge(1, typeof(Jug), BeverageType.Cider).Construct());
                        }
                        break;
                    case 2:
                        {   // 10 arrows, reagents, scrolls(level 1 to 6), potions, gems
                            cont.DropItem(new Arrow(10));
                            TreasureMapChest.PackRegs(cont, level * 5 /*?*/);
                            for (int ix = Utility.RandomMinMax(1, level + 1)/*?*/; ix > 0; ix--)
                                PackScroll(cont, minCircle: 1, maxCircle: 6);
                            for (int ix = Utility.RandomMinMax(1, level + 1)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomPotion());
                            TreasureMapChest.PackGems(cont, level * 3/*?*/);
                        }
                        break;
                    case 3:
                        {   // 10 arrows, reagents, scroll (level 1 to 7), potion, gems, magic wands, magic armor, weapons, clothing and jewelry.
                            cont.DropItem(new Arrow(10));
                            TreasureMapChest.PackRegs(cont, level * 5 /*?*/);
                            for (int ix = Utility.RandomMinMax(1, level + 1)/*?*/; ix > 0; ix--)
                                PackScroll(cont, minCircle: 1, maxCircle: 7);
                            for (int ix = Utility.RandomMinMax(1, level + 1)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomPotion());
                            TreasureMapChest.PackGems(cont, level * 3/*?*/);
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicWand(minLevel: 1, maxLevel: 2, chance: ix - 1 == 0 ? CoreAI.MagicWandDropChance : CoreAI.MagicWandDropChance / 2));
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicArmor(minLevel: 2, maxLevel: 4, chance: ix - 1 == 0 ? 0.5 : 0.2));
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicWeapon(minLevel: 2, maxLevel: 4, chance: ix - 1 == 0 ? 0.5 : 0.2));
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicClothing(minLevel: 1, maxLevel: 2, chance: ix - 1 == 0 ? 0.1 : 0.5));
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicJewelry(minLevel: 1, maxLevel: 2, chance: ix - 1 == 0 ? 0.1 : 0.5));
                        }
                        break;
                    case 4:
                        {
                            // reagents, scrolls(level 1 to 7), blank scrolls, gems, magic wands, magic armor, weapons, clothing and jewelry, crystal balls.
                            TreasureMapChest.PackRegs(cont, level * 5 /*?*/);
                            for (int ix = Utility.RandomMinMax(1, level + 1)/*?*/; ix > 0; ix--)
                                PackScroll(cont, minCircle: 1, maxCircle: 7);
                            cont.DropItem(new BlankScroll(Utility.RandomMinMax(1, level + 1)));
                            TreasureMapChest.PackGems(cont, level * 3/*?*/);
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicWand(minLevel: 2, maxLevel: 3, chance: ix - 1 == 0 ? CoreAI.MagicWandDropChance : CoreAI.MagicWandDropChance / 2));
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicArmor(minLevel: 3, maxLevel: 5, chance: ix - 1 == 0 ? 0.5 : 0.2));
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicWeapon(minLevel: 3, maxLevel: 5, chance: ix - 1 == 0 ? 0.5 : 0.2));
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicClothing(minLevel: 2, maxLevel: 3, chance: ix - 1 == 0 ? 0.1 : 0.5));
                            for (int ix = Utility.RandomMinMax(1, 2)/*?*/; ix > 0; ix--)
                                cont.DropItem(Loot.RandomMagicJewelry(minLevel: 2, maxLevel: 3, chance: ix - 1 == 0 ? 0.1 : 0.5));
                            // crystal ball: wtf, sounds stupid.
                        }
                        break;
                }
            }
        }
        public static void PackScroll(LockableContainer cont, int minCircle, int maxCircle)
        {
            PackScroll(cont, Utility.RandomMinMax(minCircle, maxCircle));
        }

        public static void PackScroll(LockableContainer cont, int circle)
        {
            int min = (circle - 1) * 8;

            cont.DropItem(Loot.RandomScroll(min, min + 7, SpellbookType.Regular));
        }

        public static void PackMagicItem(LockableContainer cont, int minLevel, int maxLevel, double chance)
        {
            if (chance <= Utility.RandomDouble())
                return;

            Item item = Loot.RandomClothingOrJewelry(must_support_magic: true);

            if (item == null)
                return;

            if (item is BaseClothing)
                ((BaseClothing)item).SetRandomMagicEffect(minLevel, maxLevel);
            else if (item is BaseJewel)
                ((BaseJewel)item).SetRandomMagicEffect(minLevel, maxLevel);

            cont.DropItem(item);
        }

        private ArrayList m_Lifted = new ArrayList();

        private bool CheckLoot(Mobile m, bool criminalAction)
        {
            if (m_Owner == null || m == m_Owner)
                return true;

            Party p = Party.Get(m_Owner);

            if (p != null && p.Contains(m))
                return true;

            Map map = this.Map;

            if (map != null && (map.Rules & MapRules.HarmfulRestrictions) == 0)
            {
                if (criminalAction)
                    m.CriminalAction(true);
                else
                    m.SendLocalizedMessage(1010630); // Taking someone else's treasure is a criminal offense!

                return true;
            }

            m.SendLocalizedMessage(1010631); // You did not discover this chest!
            return false;
        }

        public override bool CheckItemUse(Mobile from, Item item)
        {
            return CheckLoot(from, item != this) && base.CheckItemUse(from, item);
        }

        public override bool CheckLift(Mobile from, Item item, ref LRReason reject)
        {   // Thwart lift macros
            if (LiftMemory.Recall(from))
            {   // throttle
                from.SendMessage("You thrust your hand into the chest but come up empty handed.");
                reject = LRReason.Inspecific;
                return false;
            }
            else
                LiftMemory.Remember(from, 1.8);

            return CheckLoot(from, true) && base.CheckLift(from, item, ref reject);
        }

        public override void OnItemLifted(Mobile from, Item item)
        {
            bool notYetLifted = !m_Lifted.Contains(item);
            BaseCreature witness = null;

            if (notYetLifted)
            {
                // Adam: Insure IOB wearers are not stealing from their kin
                if (Core.RuleSets.AngelIslandRules() && Engines.IOBSystem.IOBSystem.IsIOBAligned(from) && CheckThief(from, out witness))
                {
                    from.SendMessage("You have been discovered stealing from your kin!");
                    if (from.Hidden)
                        from.RevealingAction();

                    // attack kin to make them come after you
                    from.DoHarmful(witness);
                }
                else if (Core.RuleSets.AngelIslandRules() && Utility.RandomDouble() < 0.30 && CheckGuardian(from, out witness))
                {
                    from.SendMessage("You have been discovered stealing pirate booty!");
                    if (from.Hidden)
                        from.RevealingAction();

                    // attack kin to make them come after you
                    from.DoHarmful(witness);
                }
                // adam: we want to reveal the looter about level * 3% of the time (per item looted)
                // for chest levels 1-5, this works out to: 3%, 6%, 9%, 12%, 15%
                else if ((from.Hidden) && (Utility.RandomDouble() < (0.025 * m_Level)))
                {
                    from.SendMessage("You have been revealed!");
                    from.RevealingAction();
                }
            }

            base.OnItemLifted(from, item);
        }

        public bool CheckThief(Mobile from, out BaseCreature witness)
        {
            witness = null;

            if (from == null || !(from is PlayerMobile))
                return false;

            PlayerMobile pm = (PlayerMobile)from;

            IPooledEnumerable eable = pm.GetMobilesInRange(12);
            foreach (Mobile m in eable)
            {
                if (m == null || !(m is BaseCreature))
                    continue;

                BaseCreature bc = (BaseCreature)m;
                if (bc.Controlled == true)
                    continue;

                if (pm.IOBAlignment == bc.IOBAlignment)
                {
                    witness = bc;
                    eable.Free();
                    return true;
                }
            }
            eable.Free();

            return false;
        }

        /*
		 * Check to see if there is a guardian nearby, and if so, provide a good chance
		 * that he will catch you stealing booty. The chance to 'catch you' is based on the number
		 * of monsters near you. Each nearby monster will decrease the chance to catch you by 10%.
		 * This is not much as you start at 100% and it would take 3+ monsters to even give you a small chance at 
		 * slipping past the guardian 'on this lift' <-- Remember, it's a per lift check
		 */
        private bool CheckGuardian(Mobile from, out BaseCreature witness)
        {
            witness = null;

            if (from == null || !(from is PlayerMobile))
                return false;

            PlayerMobile pm = (PlayerMobile)from;

            // start with a 100% chance to get attacked by pirate
            double chance = 1.0;
            IPooledEnumerable eable = pm.GetMobilesInRange(12);
            foreach (Mobile m in eable)
            {
                if (m == null || !(m is BaseCreature))
                    continue;

                BaseCreature bc = (BaseCreature)m;
                if (bc.Controlled == true || bc.Summoned == true)
                    continue;

                if (from.CanSee(bc) == false)
                    continue;

                // if it carries the Guardian Use property, it's a Guardian
                if (Property.FindUse(bc, Use.IsGuardian))
                    witness = bc;
                else
                    // reduce chance by 10% for each nearby mobile that is not a guardian
                    chance *= .9;
            }
            eable.Free();

            // see if user gets a pass
            if (Utility.RandomDouble() > chance)
                witness = null;

            return witness == null ? false : true;
        }

        private static object[] m_Arguments = new object[1];

        private static void AddItems(Container cont, int[] amounts, Type[] types)
        {
            for (int i = 0; i < amounts.Length && i < types.Length; ++i)
            {
                if (amounts[i] > 0)
                {
                    try
                    {
                        m_Arguments[0] = amounts[i];
                        Item item = (Item)Activator.CreateInstance(types[i], m_Arguments);

                        cont.DropItem(item);
                    }
                    catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                }
            }
        }

        public override void OnAfterSpawn()
        {
            if (Utility.Random(100) < CoreAI.DungeonChestBuryRate && m_Level >= CoreAI.DungeonChestBuryMinLevel)
                m_XItem = XMarksTheSpot.Bury(this, m_Level);
        }

        public DungeonTreasureChest(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            writer.Write(m_XItem);

            // version 2
            writer.Write(m_Guardian);

            // version 1
            writer.Write(m_Owner);

            // version 0
            writer.Write((int)m_Level);
            writer.WriteDeltaTime(m_DeleteTime);
            writer.WriteItemList(m_Lifted, true);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_XItem = reader.ReadItem();

                        goto case 2;
                    }
                case 2:
                    {
                        m_Guardian = reader.ReadMobile();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Owner = reader.ReadMobile();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Level = reader.ReadInt();
                        m_DeleteTime = reader.ReadDeltaTime();
                        m_Lifted = reader.ReadItemList();

                        m_Timer = new DeleteTimer(this, m_DeleteTime);
                        m_Timer.Start();

                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;

            if (m_XItem != null)
                m_XItem.Delete();

            base.OnAfterDelete();
        }

        private class DeleteTimer : Timer
        {
            private Item m_Item;

            public DeleteTimer(Item item, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_Item = item;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                DeleteGuardian(m_Item as DungeonTreasureChest);
                // we log all IntMapStorage objects, so to avoid useless logging
                //  unset this now.
                if (m_Item.IsIntMapStorage)
                    m_Item.IsIntMapStorage = false;
                m_Item.Delete();
                Stop();
            }
        }

        public static void DeleteGuardian(DungeonTreasureChest tc)
        {
            if (tc == null || (tc as DungeonTreasureChest).Guardian == null || (tc as DungeonTreasureChest).Guardian.Alive == false || (tc as DungeonTreasureChest).Guardian.Deleted == true)
                return;

            if ((tc as DungeonTreasureChest).Guardian as BaseCreature == null)
                return;

            // say something!
            switch (Utility.Random(4))
            {
                case 0:
                    (tc as DungeonTreasureChest).Guardian.Say("Thar be nothing left for me here.");
                    break;
                case 1:
                    (tc as DungeonTreasureChest).Guardian.Say("I done me best!");
                    break;
                case 2:
                    (tc as DungeonTreasureChest).Guardian.Say("Arr, me work be done here.");
                    break;
                case 3:
                    (tc as DungeonTreasureChest).Guardian.Say("Arr, I got to get back to me ale!");
                    break;
            }

            // Frozen while casting
            (tc as DungeonTreasureChest).Guardian.CantWalkLand = true;

            // fake recall
            new NpcRecallSpell((tc as DungeonTreasureChest).Guardian, null, new Point3D(0, 0, 0)).Cast();

            // delete him
            DateTime DeleteTime = DateTime.UtcNow + TimeSpan.FromSeconds(3.0);
            new DeleteGuardianTimer((tc as DungeonTreasureChest).Guardian, DeleteTime).Start();
        }

        private class DeleteGuardianTimer : Timer
        {
            private Mobile m_mob;

            public DeleteGuardianTimer(Mobile m, DateTime time)
                : base(time - DateTime.UtcNow)
            {
                m_mob = m;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                if (m_mob == null || m_mob.Deleted == true || m_mob.Alive == false)
                    return;

                m_mob.Delete();
            }
        }

        public override void OnTelekinesis(Mobile from)
        {
            //Do nothing, telekinesis doesn't work on a treasure chest.
            from.SendLocalizedMessage(501857); // This spell won't work on that!
        }

        public override bool AutoResetTrap
        {
            /* 5/21/23, Yoar: Treasure chest traps auto-reset on AI/MO
             * Remove trap is necessary to open these chests
             */
            get { return SkillHandlers.RemoveTrap.EraAI; }
        }

        public override bool ExecuteTrap(Mobile from, bool bAutoReset)
        {
            bool bReturn = base.ExecuteTrap(from, bAutoReset);

            // adam: reset decay timer when the trap is messed with
            DecayChest(from);

            if (m_Level > 5)
            {
                LogHelper logger = new LogHelper("Level6DungeonChestOpened.log", World.GetAdminAcct(), false, true);
                logger.Log(LogType.Mobile, from, string.Format("Opened box {0}.", this));
                logger.Finish();
            }

            return bReturn;
        }

        public void DecayChest(Mobile from)
        {
            /* Exploit mitigation: 
             * Not much of an exploit, but a dirty trick.
             * Players are going around Magic Locking Dungeon chests making them inaccessible to dungeon pickers.
             * MagicLock now invokes this decay timer should the chest be locked in this way
             */
            if (m_Timer != null)
            {
                m_Timer.Stop();

                m_Timer = null;

                // adam: once the trap has magic locked, it decays in 15 minutes
                m_DeleteTime = DateTime.UtcNow + TimeSpan.FromMinutes(15.0);

                // anti-spam
                if (m_HasStartedQuickDecay == false)
                    from.SendMessage("The chest begins to decay.");
                m_HasStartedQuickDecay = true;

                m_Timer = new DeleteTimer(this, m_DeleteTime);
                m_Timer.Start();
            }
        }

        public override void LockPick(Mobile from)
        {
            base.LockPick(from);

            // call data recorder here to attribute this picker with the 'earned gold' points
            DataRecorder.LockPick(from, this);

            if (Core.RuleSets.AngelIslandRules())
            {
                if (m_Level >= 3)
                    m_Guardian = SpawnGuardian("Pirate", m_Level, from.Skills[SkillName.Hiding].Value);

                if (m_Guardian != null)
                    m_Guardian.AggressiveAction(from);
            }

            // 5/21/23, Adam: Once lockpicked, start the decay
            DecayChest(from);
        }

        public Mobile SpawnGuardian(string name, int level, double PlayersHidingSkill)
        {
            Type type = ScriptCompiler.FindTypeByName(name);
            BaseCreature c = null;

            if (type != null)
            {
                try
                {
                    object o = Activator.CreateInstance(type);

                    if (o is BaseCreature)
                    {
                        c = o as BaseCreature;

                        // decay time of a chest once it's opened
                        c.Lifespan = TimeSpan.FromMinutes(15);

                        // reset the alignment
                        c.IOBAlignment = IOBAlignment.None;
                        c.GuildAlignment = AlignmentType.None;

                        // Can chase you and can reveal you if you be hiding!
                        c.CanRun = true;
                        c.CanReveal = true;

                        // stats based on chest level
                        double factor = 1.0;
                        if (level == 3)
                            factor = .3;
                        if (level == 4)
                            factor = .5;
                        if (level == 5)
                            factor = 1.0;

                        c.SetMana((int)(c.ManaMax * factor));
                        c.SetStr((int)(c.RawStr * factor));
                        c.SetDex((int)(c.RawDex * factor));
                        c.SetInt((int)(c.RawInt * factor));
                        c.SetHits((int)(((c.HitsMax / 100.0) * 60.0) * factor));

                        // these guys can reveal - set the Detect Hidden to match the players hiding so that they have a fighting chance
                        c.SetSkill(SkillName.DetectHidden, PlayersHidingSkill * factor);

                        // nerf their magery so that they 
                        // Sixth	20	52.1	100
                        c.SetSkill(SkillName.Magery, 52.1 * factor);

                        // only attack aggressors
                        c.FightMode = FightMode.Aggressor;

                        // maybe 6 tiles? Keep him near by
                        c.RangeHome = 6;

                        // the chest is the home of the guardian
                        c.Home = this.Location;

                        // we are not bardable
                        c.BardImmune = true;

                        // make them a guardian
                        c.AddItem(new Property(Use.IsGuardian, null));

                        // give them shite speak if they are calmed
                        c.AddItem(new Quip("Arr, but that be a pretty tune .. can you play me another?"));
                        c.AddItem(new Quip("Thar be no time for singing and dancin' now matey."));
                        c.AddItem(new Quip("That be a downright lovely tune ye be playing thar."));
                        c.AddItem(new Quip("Har! Me thinks a cutlass would be a better choice!"));

                        // show them
                        //Point3D loc = GetSpawnPosition(c.RangeHome);
                        Point3D loc = Spawner.GetSpawnPosition(this.Map, this.Location, c.RangeHome, SpawnFlags.None /*SpawnFlags.ClearPath*/, c);
                        c.MoveToWorld(loc, this.Map);

                        // teleport
                        Effects.SendLocationParticles(EffectItem.Create(c.Location, c.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);
                        Effects.PlaySound(c.Location, c.Map, 0x1FE);

                        Timer.DelayCall(TimeSpan.FromSeconds(1.5), new TimerStateCallback(ShiteTalk_Callback), c);

                    }

                }
                catch (Exception exc)
                {
                    LogHelper.LogException(exc);
                    System.Console.WriteLine("Exception caught in Spawner.Refresh: " + exc.Message);
                    System.Console.WriteLine(exc.StackTrace);
                }
            }

            return c as Mobile;
        }

        public virtual void ShiteTalk_Callback(object state)
        {
            Mobile guardian = state as Mobile;

            if (guardian == null || guardian.Alive == false || guardian.Deleted == true)
                return;

            // shite talking
            switch (Utility.Random(3))
            {
                case 0: guardian.Say("Arr. Ye best be steppin' away from that thar chest matey."); break;
                case 1: guardian.Say("Avast Ye, Scallywag! I be watching over that thar booty."); break;
                case 2: guardian.Say("I know ye be 'round here somewhere!"); break;
            }
        }

        private Point3D GetSpawnPosition(int HomeRange)
        {
            Map map = Map;

            if (map == null)
                return Location;

            // Try 10 times to find a Spawnable location.
            for (int i = 0; i < 10; i++)
            {
                int x = Location.X + (Utility.Random((HomeRange * 2) + 1) - HomeRange);
                int y = Location.Y + (Utility.Random((HomeRange * 2) + 1) - HomeRange);
                int z = Map.GetAverageZ(x, y);

                if (Map.CanSpawnLandMobile(new Point2D(x, y), this.Z))
                    return new Point3D(x, y, this.Z);
                else if (Map.CanSpawnLandMobile(new Point2D(x, y), z))
                    return new Point3D(x, y, z);
            }

            return this.Location;
        }
    }
}