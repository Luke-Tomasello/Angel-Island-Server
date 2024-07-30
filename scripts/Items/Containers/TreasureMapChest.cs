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

/* Items/Containers/TreasureMapChest.cs
 * ChangeLog:
 *  9/15/21, Adam (CoreAI.MagicGearDropDowngrade)
 *      throttle magic gear via global MagicGearDropDowngrade
 *      This is intended to decrease weapon and armor drops quality to improve desirability of player crafted items
 *      Obsolete: CoreAI.MagicGearDropChance
 *  9/10/21, Adam (CoreAI.MagicGearDropChance)
 *      throttle magic gear via global MagicGearDropChance
 *      This is intended to decrease weapon and armor drops quantity to improve desirability of player crafted items
 *      Add a 12.5% chance at a rare for level > 3
 *	2/8/11, Adam
 *		Non AI shards get standard RunUO loot.
 *		Not sure this is exactly correct for Siege era, but it's got to be close.
 *	8/12/10, adam
 *		Because chest spawn randomly around the map, and because there is a chance of a rare under the chest,
 *		we now require that the chest be emptied before removal.
 *	5/23/10, Adam
 *		In CheckLift(), thwart lift macros by checking the per-player 'lift memory'
 *	11/12/08, Adam
 *		- Thwart �fast lifting� in CheckLift: �You thrust your hand into the chest but come up empty handed.�
 *	11/10/08, Adam
 *		- Replace hard coded drops rate fopr magic weapoins and armor with the new function MagicArmsThrottle(level)
 *		- Have MagicArmsThrottle() effectively 1/2 the current drop rate for magic weapons.
 *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loop updated.
 *  2/28/07, Adam
 *      Allow either explosion OR poison for the trap
 *  1/31/07, Adam
 *      - change the spawn rate from a flat 10% to 0.1 * (Level * .5);
 *      - Don't allow the milking of the chest by lifting one item from a stack
 *  11/27/06, Kit
 *      Fixed bug with chest theme type not being serialized
 *	1/21/06, Adam
 *		It's no longer required that an aligned player be wearing their IOB to get caught stealing from their kin.
 *		Detail: Removed the requirement for pm.IOBEquipped in CheckThief()
 *	1/20/06, Adam
 *		1. Add in new theme loot
 *		2. cleanup the treasure chest code by moving the theme loot into AddThemeLoot()
 *		3. Do not lock/trap Overland Themed Chests. RP. the Treasure Hunter NPC has unlocked it for you.
 *	7/13/05, erlein
 *		Added SDrop chance to weapon + armor dropping section.
 *	4/11/05, Adam
 *		Move the hueing code to Loot.ImbueWeaponOrArmor
 *		Add back normal magic weapons/armor for themed chests, but at reduced rate.
 *	04/10/05, Kitaras
 *		Removed normal magic weapons/armor from spawning in themed chests.
 *	04/09/05, Kitaras
 *		Implemented special weapon loot drops for themed chests
 *	04/07/05, Kitaras
 *		Implemented Initial themed loot drops
 *	03/31/05, Kitaras
 *		Added themed treasure chest support
 *	03/30/05, Kitaras
 *		Added code to OnItemLifted to spawn twice the normal amount of mobs
 *		for themed chests, added theme property and value to treasuechests.
 *	03/28/05, Kitaras
 *		Added Check to CheckThief to prevent controled pets with iob alignment
 *		from setting off "you have been noticed stealing from you kin"
 *	3/28/05, Adam
 *		Move weighted table selection code for weapon/armor attr to Loot.cs
 *	11/20/04, Adam
 *		Add CheckThief() method to OnItemLifted() to see if you are stealing from your kin!
 *	9/8/04, Adam
 *		decrease the chances to get the max level attribute for this level
 *		Create an unevenly weighted table for chance resolution
 *	9/6/04, Adam
 *		decrease the chances to get the max level attribute for this level
 *		from (1 in level+1) to (1 in 2 * (level+1))
 *  8/9/04, Pixie
 *		Changed the damage done when the trap is tripped on a disarm failure.
 *  8/8/04, Pixie
 *		Added functionality for tripping the trap if you fail to disarm it.
 *  7/23/04, Adam
 *		1. add a 5% chance at a slayer weapon
 * 		2. add a 10% chance at a weapon upgrade
 *	7/12/04, Adam
 *		1. Changed drop to drop 1/2 of the original number or weapons / armor.
 *  6/29/04, Adam
 *		Changed to drop scrolls appropriate for the level.
 *		Added PackScroll procedures
 *	5/19/04, pixie
 *		Modifies so the trap resets when it is tripped.
 *		Now the only way to access the items inside is by removing the
 *		trap with the Remove Trap skill.
 *	4/30/04, mith
 *		modified the chances for getting high end weapons/armor based on treasure chest level.
 *   4/27/2004, pixie
 *     Changed so telekinesis doesn't trip the trap
 */

using Server.ContextMenus;
using Server.Engines.DataRecorder;
using Server.Engines.PartySystem;
using Server.Engines.Plants;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Network;
using System;
using System.Collections;

namespace Server.Items
{
    [FlipableAttribute(0xE41, 0xE40)]
    public class TreasureMapChest : LockableContainer
    {
        private int m_Level;
        private DateTime m_DeleteTime;
        private Timer m_Timer;
        private Mobile m_Owner;
        private bool m_IsThemed;
        private ChestThemeType m_ThemeType;

        // We generate our own items on dupe, therefore DeepDupe need not dupe our items
        public override bool AutoFills { get { return true; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Level { get { return m_Level; } set { m_Level = value; } }

        //set theme type of chest
        [CommandProperty(AccessLevel.GameMaster)]
        public ChestThemeType ThemeType { get { return m_ThemeType; } set { m_ThemeType = value; } }

        //set if chest is themed or not
        [CommandProperty(AccessLevel.GameMaster)]
        public bool Themed { get { return m_IsThemed; } set { m_IsThemed = value; } }

        //[CommandProperty(AccessLevel.GameMaster)]
        //public Mobile Owner { get { return m_Owner; } set { m_Owner = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime DeleteTime { get { return m_DeleteTime; } }

        public override double TrapSensitivity { get { return 1.5; } }

        //standered [add treasuremapchest <level>
        [Constructable]
        public TreasureMapChest(int level)
            : this(null, level, false, ChestThemeType.None)
        {
        }
        // new [add treasuremapchest <level> <theme>
        [Constructable]
        public TreasureMapChest(int level, ChestThemeType type)
            : this(null, level, true, type)
        {
        }

        public TreasureMapChest(Mobile owner, int level, bool themed, ChestThemeType type)
            : base(0xE41)
        {
            m_Owner = owner;
            m_Level = level;
            m_IsThemed = themed;
            m_ThemeType = type;
            m_DeleteTime = DateTime.UtcNow + TimeSpan.FromHours(3.0);

            m_Timer = new DeleteTimer(this, m_DeleteTime);
            m_Timer.Start();

            Fill(this, level, m_IsThemed, type);
        }

        private static void GetRandomAOSStats(out int attributeCount, out int min, out int max)
        {
            int rnd = Utility.Random(15);

            if (rnd < 1)
            {
                attributeCount = Utility.RandomMinMax(2, 6);
                min = 20; max = 70;
            }
            else if (rnd < 3)
            {
                attributeCount = Utility.RandomMinMax(2, 4);
                min = 20; max = 50;
            }
            else if (rnd < 6)
            {
                attributeCount = Utility.RandomMinMax(2, 3);
                min = 20; max = 40;
            }
            else if (rnd < 10)
            {
                attributeCount = Utility.RandomMinMax(1, 2);
                min = 10; max = 30;
            }
            else
            {
                attributeCount = 1;
                min = 10; max = 20;
            }
        }

        public override int DefaultGumpID { get { return 0x42; } }
        public override int DefaultDropSound { get { return 0x42; } }

        public override Rectangle2D Bounds
        {
            get { return new Rectangle2D(18, 105, 144, 73); }
        }

        const int STATUES = 12; //12 new statue types to drop
        private static MonsterStatuetteType[] m_Monster = new MonsterStatuetteType[STATUES]
        {
            MonsterStatuetteType.SolenWorker,
            MonsterStatuetteType.TerathanAvenger,
            MonsterStatuetteType.GiantRat,
            MonsterStatuetteType.HordeDemon,
            MonsterStatuetteType.BillyGoat,
            MonsterStatuetteType.GrizzlyBear,
            MonsterStatuetteType.Ghost,
            MonsterStatuetteType.Ghoul,
            MonsterStatuetteType.SeaHorse,
            MonsterStatuetteType.Genie,
            MonsterStatuetteType.Pixie,
            MonsterStatuetteType.Unicorn,
        };

        private static object[] m_Arguments = new object[1];

        //override fill function to keep fishing and other scripts using old fill function not needing updated.
        public static void Fill(LockableContainer cont, int level)
        {
            Fill(cont, level, false, ChestThemeType.None);
        }
        public override void LockPick(Mobile from)
        {
            base.LockPick(from);

            // call data recorder here to attribute this picker with the 'earned gold' points
            DataRecorder.LockPick(from, this);

        }

        public static void Fill(LockableContainer cont, int level, bool IsThemed, ChestThemeType type)
        {
            cont.Movable = false;

            if (Core.RuleSets.AngelIslandRules())
            {

                // the special Overland Treasure Hunter NPC 'unlocks' the chest for you!
                if (TreasureTheme.IsOverlandTheme(type) == false)
                {
                    cont.TrapType = Utility.RandomBool() ? TrapType.PoisonTrap : TrapType.ExplosionTrap;
                    cont.TrapPower = level * 25;
                    cont.TrapLevel = level;
                    cont.Locked = true;
                }

                // 12.5% chance at a rare
                if (level > 3)
                {
                    Item rare = Loot.RareFactoryItem(0.125);
                    if (rare != null)
                        cont.DropItem(rare);
                }

                switch (level)
                {
                    case 1: cont.RequiredSkill = 36; break;
                    case 2: cont.RequiredSkill = 76; break;
                    case 3: cont.RequiredSkill = 84; break;
                    case 4: cont.RequiredSkill = 92; break;
                    case 5: cont.RequiredSkill = 100; break;
                }

                cont.LockLevel = cont.RequiredSkill - 10;
                cont.MaxLockLevel = cont.RequiredSkill + 40;

                // add theme loot
                AddThemeLoot(cont, level, type);

                // now for the gold
                cont.DropItem(new Gold(level * 1000));

                //if not a undead or pirate chest add scrolls
                if (type != ChestThemeType.Pirate || type != ChestThemeType.Undead)
                {
                    // adam: Changed to drop scrolls appropriate for the level.
                    for (int i = 0; i < level * 5; ++i)
                    {
                        int minCircle = level;
                        int maxCircle = (level + 3);
                        PackScroll(cont, minCircle, maxCircle);
                    }

                }

                // magic armor and weapons
                int count = ((level * 6) / 2);              // calc amount of magic armor and weapons to drop
                if (IsThemed == true) count /= 2;           // adam: Less loot if a themed chest because they get other goodies.
                for (int i = 0; i < count; ++i)
                {
                    Item item;
                    item = Loot.RandomArmorOrShieldOrWeapon();
                    item = Loot.ImbueWeaponOrArmor(noThrottle: false, item, (Loot.ImbueLevel)level, 0.05, false);
                    cont.DropItem(item);
                }

                PackRegs(cont, level * 20);
                PackGems(cont, level * 10);
            }
            else
            {
                if (level == 0)
                {   // "Youthful" Treasure map
                    cont.LockLevel = 0; // Can't be unlocked

                    cont.DropItem(new Gold(Utility.RandomMinMax(50, 100)));

                    if (Utility.RandomDouble() < 0.75)
                        cont.DropItem(new TreasureMap(0, Map.Trammel));
                }
                else
                {
                    // the special Overland Treasure Hunter NPC 'unlocks' the chest for you!
                    if (TreasureTheme.IsOverlandTheme(type) == false)
                    {
                        cont.TrapType = TrapType.ExplosionTrap;
                        cont.TrapPower = level > 5 ? 5 * 25 : level * 25;
                        cont.TrapLevel = level > 5 ? 5 : level;
                        cont.Locked = true;
                    }

                    #region RequiredSkill
                    switch (level)
                    {
                        case 1: cont.RequiredSkill = 36; break;
                        case 2: cont.RequiredSkill = 76; break;
                        case 3: cont.RequiredSkill = 84; break;
                        case 4: cont.RequiredSkill = 92; break;
                        case 5: cont.RequiredSkill = 100; break;
                        case 6: cont.RequiredSkill = 100; break;
                    }
                    #endregion RequiredSkill

                    cont.LockLevel = cont.RequiredSkill - 10;
                    cont.MaxLockLevel = cont.RequiredSkill + 40;

                    // add theme loot
                    AddThemeLoot(cont, level, type);

                    #region Gold
                    cont.DropItem(new Gold(level * 1000));
                    #endregion Gold

                    #region Scrolls
                    for (int i = 0; i < level * 5; ++i)
                        cont.StackItem(Loot.RandomScroll(0, 63, SpellbookType.Regular));
                    #endregion Scrolls

                    #region Magic Items
                    Loot.ImbueLevel imbueLevel = Loot.TreasureMapLevelToImbueLevel(level);
                    int[] minmax = Loot.ImbueLevelToMagicEnchantment(imbueLevel);
                    for (int i = 0; i < level * 6; ++i)
                    {
                        Item item = null;
                        #region Item
#if false
                        if (Core.RuleSets.AOSRules())
                            item = Loot.RandomArmorOrShieldOrWeaponOrJewelry();
                        else
                            item = Loot.RandomArmorOrShieldOrWeapon();
#else
                        #region proof
                        /* THE TREASURE
                        Below is a list of what you can expect to get from a Level 3 map. And keep in mind while reading this list that the treasure from Level 4 and Level 5 maps are that much more gratifying. =)
                        Items below are the combination of the Treasure Chest and loot from the monsters that spawned.

                        5000+ Gold
                        22 Scrolls
                        35 Jewels
                        46 Reagents
                        ---
                        Ring of Night Eyes - 48 ch
                        Necklace of Protection - 19 ch
                        Bracelet of Night Eyes - 38 ch
                        Wooden Shield of Strengh & Defense 19 ch
                        Suubstantial Wooden Shield of Defense
                        Durable Buckler of Hardening
                        Wand of Feeblemindedness 6 ch
                        Wand of Fireballs 3 ch
                        Wand of Great Healing 9 ch
                        Substantial, Surpassingly Accurate Bow of Ruin
                        Ringmail Sleeves of Defense
                        Indestructable Leather Gloves of Defense
                        Accurate Club of Ruin
                        Substantial Accurate Quarter Staff
                        Dagger of Might
                        Durable Helmet of Fortification
                        Durable, Accurate Cleaver
                        Cloak of Night Eyes 70 ch
                        Fortified Axe
                        Substantial, Accurate Long Sword
                        Feathered Hat of Night Eyes
                        Body Sash of Agility
                        https://web.archive.org/web/20011007123405/http://uo.stratics.com/strat/treashunt.shtml
                         */
                        #endregion Proof

                        switch (Utility.Random(4))
                        {
                            case 0:
                            case 1:
                            case 2:
                                // magic wand chance (currently 2%)
                                if (Utility.Chance(CoreAI.MagicWandDropChance))
                                {   // wand
                                    if ((item = new Wand()) != null)
                                    {
                                        ((Wand)item).SetRandomMagicEffect(minmax[0], minmax[1]);
                                        cont.DropItem(item);
                                    }
                                }
                                else
                                {   // weapon or armor
                                    if (Utility.RandomBool())
                                        item = Loot.RandomArmorOrShield();
                                    else
                                        item = Loot.RandomWeapon();
                                    if (item != null)
                                    {
                                        item = Loot.ImbueWeaponOrArmor(item, level);
                                        cont.DropItem(item);
                                    }
                                }
                                break;
                            case 3:
                                // clothing or jewelry
                                if ((item = Loot.RandomClothingOrJewelry(must_support_magic: true)) != null)
                                {
                                    if (item is BaseClothing)
                                        ((BaseClothing)item).SetRandomMagicEffect(minmax[0], minmax[1]);
                                    else if (item is BaseJewel)
                                        ((BaseJewel)item).SetRandomMagicEffect(minmax[0], minmax[1]);

                                    if (item != null)
                                        cont.DropItem(item);
                                }
                                break;


                        }
#endif
                        #endregion Item
                        #region Obsolete
#if false
                        if (item is BaseWeapon)
                        {
                            BaseWeapon weapon = (BaseWeapon)item;

                            if (Core.RuleSets.AOSRules())
                            {
                                int attributeCount;
                                int min, max;

                                GetRandomAOSStats(out attributeCount, out min, out max);

                                BaseRunicTool.ApplyAttributesTo(weapon, attributeCount, min, max);
                            }
                            else
                            {
                                weapon.DamageLevel = Loot.GetGearForLevel<WeaponDamageLevel>(level, upgrade_chance: 0.2);
                                weapon.AccuracyLevel = Utility.RandomEnumValue<WeaponAccuracyLevel>();
                                weapon.DurabilityLevel = Utility.RandomEnumValue<WeaponDurabilityLevel>();
                            }

                            cont.DropItem(item);
                        }
                        else if (item is BaseArmor)
                        {
                            BaseArmor armor = (BaseArmor)item;

                            if (Core.RuleSets.AOSRules())
                            {
                                int attributeCount;
                                int min, max;

                                GetRandomAOSStats(out attributeCount, out min, out max);

                                BaseRunicTool.ApplyAttributesTo(armor, attributeCount, min, max);
                            }
                            else
                            {
                                armor.ProtectionLevel = Loot.GetGearForLevel<ArmorProtectionLevel>(level, upgrade_chance: 0.2);
                                armor.DurabilityLevel = Utility.RandomEnumValue<ArmorDurabilityLevel>();
                            }

                            cont.DropItem(item);
                        }
                        else if (item is BaseHat)
                        {
                            BaseHat hat = (BaseHat)item;

                            if (Core.RuleSets.AOSRules())
                            {
                                int attributeCount;
                                int min, max;

                                GetRandomAOSStats(out attributeCount, out min, out max);

                                // Adam: we can add this if we ever decide to enable AOS (lol)
                                //BaseRunicTool.ApplyAttributesTo(hat, attributeCount, min, max);
                            }

                            cont.DropItem(item);
                        }
                        else if (item is BaseJewel)
                        {
                            int attributeCount;
                            int min, max;

                            GetRandomAOSStats(out attributeCount, out min, out max);

                            BaseRunicTool.ApplyAttributesTo((BaseJewel)item, attributeCount, min, max);

                            cont.DropItem(item);
                        }
#endif
                        #endregion Obsolete
                    }
                    #endregion Magic Items
                }

                #region Reagents
                {
                    for (int i = 0; i < level * 20; ++i)
                        cont.StackItem(Loot.RandomPossibleReagent());
                }
                #endregion Reagents

                #region Gems
                {
                    for (int i = 0; i < ((level == 0) ? 2 : level * 10); ++i)
                        cont.StackItem(Loot.RandomGem());
                }
                #endregion Gems

                // Adam: we can add this if we ever decide to enable AOS (lol)
                //if (level == 6 && Core.AOS)
                //cont.DropItem((Item)Activator.CreateInstance(m_Artifacts[Utility.Random(m_Artifacts.Length)]));

                return;
            }
        }

        public static int[] m_Gems = new int[9];
        public static Type[] m_GemTypes = new Type[]
        {
            typeof( Amber ), typeof( Amethyst ),
            typeof( Citrine ), typeof( Diamond ),
            typeof( Emerald ), typeof( Ruby ),
            typeof( Sapphire ), typeof( StarSapphire ),
            typeof( Tourmaline )
        };

        //rageant type array for loot generation
        public static int[] m_Regs = new int[8];
        public static Type[] m_RegTypes = new Type[]
        {
            typeof( BlackPearl ), typeof( Bloodmoss ),
            typeof( Garlic ), typeof( Ginseng ),
            typeof( MandrakeRoot ), typeof( Nightshade ),
            typeof( SpidersSilk ), typeof( SulfurousAsh )
        };

        public static void PackGems(LockableContainer cont, int count)
        {
            ClearAmounts(TreasureMapChest.m_Gems);

            for (int i = 0; i < count; ++i)
                m_Gems[Utility.Random(TreasureMapChest.m_Gems.Length)]++;

            AddItems(cont, TreasureMapChest.m_Gems, TreasureMapChest.m_GemTypes);
        }

        public static void PackRegs(LockableContainer cont, int count)
        {
            ClearAmounts(TreasureMapChest.m_Regs);

            for (int i = 0; i < count; ++i)
                m_Regs[Utility.Random(TreasureMapChest.m_Regs.Length)]++;

            AddItems(cont, TreasureMapChest.m_Regs, TreasureMapChest.m_RegTypes);
        }

        private static void AddThemeLoot(LockableContainer cont, int level, ChestThemeType type)
        {
            MonsterStatuette mx = null;

            //switch to add in theme treasures
            switch (type)
            {
                case ChestThemeType.Solen:
                    {

                        //drop are special weapon
                        QuarterStaff special = new QuarterStaff();
                        special.Name = "Chitanous Staff";
                        cont.DropItem(Loot.ImbueWeaponOrArmor(noThrottle: true, special, Loot.ImbueLevel.Level6 /*6*/, 0, true));

                        //go into dropping normal loot

                        int onlyonedrop = Utility.RandomMinMax(0, 1);

                        if (onlyonedrop == 0) cont.DropItem(new HedgeSeed()); //new solen seed
                        if (onlyonedrop == 1) cont.DropItem(new WaterBucket()); //new waterbucket

                        if (Utility.RandomDouble() <= 0.30) //30% chance to drop a statue
                        {
                            int whichone = Utility.RandomMinMax(0, 1);
                            if (whichone == 0) mx = new MonsterStatuette(m_Monster[0]);
                            if (whichone == 1) mx = new MonsterStatuette(m_Monster[1]);
                            mx.LootType = LootType.Regular;     // not blessed
                            cont.DropItem(mx);          // drop it baby!
                        }
                        break;
                    }

                case ChestThemeType.Brigand:
                    {
                        //drop are special weapon
                        Katana special = new Katana();
                        special.Name = "Bandit's Blade";
                        cont.DropItem(Loot.ImbueWeaponOrArmor(noThrottle: true, special, Loot.ImbueLevel.Level6 /*6*/, 0, true));

                        int onlyonedrop = Utility.RandomMinMax(0, 1);

                        if (onlyonedrop == 0) cont.DropItem(new Brazier(true)); //new movable brazier
                        if (onlyonedrop == 1) cont.DropItem(new DecorativeBow(Utility.RandomMinMax(0, 3))); //random decorative bow type

                        if (Utility.RandomDouble() <= 0.30) //30% chance to drop a statue
                        {
                            int whichone = Utility.RandomMinMax(0, 1);
                            if (whichone == 0) mx = new MonsterStatuette(m_Monster[2]);
                            if (whichone == 1) mx = new MonsterStatuette(m_Monster[3]);
                            mx.LootType = LootType.Regular;     // not blessed
                            cont.DropItem(mx);          // drop it baby!
                        }
                        break;
                    }

                case ChestThemeType.Savage:
                    {
                        //drop are special weapon
                        ShortSpear special = new ShortSpear();
                        special.Name = "Ornate Ritual Spear";
                        cont.DropItem(Loot.ImbueWeaponOrArmor(noThrottle: true, special, Loot.ImbueLevel.Level6 /*6*/, 0, true));

                        int rug = Utility.RandomMinMax(0, 1);
                        int onlyonedrop = Utility.RandomMinMax(0, 1);

                        if (onlyonedrop == 0) cont.DropItem(new SkullPole()); //new skull pole

                        if (onlyonedrop == 1)
                        {
                            if (rug == 0) cont.DropItem(new BrownBearRugEastDeed()); //new rug east
                            if (rug == 1) cont.DropItem(new BrownBearRugSouthDeed()); //new rug south
                        }

                        if (Utility.RandomDouble() <= .30) //30% chance to drop a statue
                        {
                            int whichone = Utility.RandomMinMax(0, 1);
                            if (whichone == 0) mx = new MonsterStatuette(m_Monster[4]);
                            if (whichone == 1) mx = new MonsterStatuette(m_Monster[5]);
                            mx.LootType = LootType.Regular;         // not blessed
                            cont.DropItem(mx);              // drop it baby!
                        }
                        break;
                    }

                case ChestThemeType.Undead:
                    {
                        Halberd special = new Halberd();
                        special.Name = "Soul Reaver";
                        cont.DropItem(Loot.ImbueWeaponOrArmor(noThrottle: true, special, Loot.ImbueLevel.Level6 /*6*/, 0, true));

                        int onlyonedrop = Utility.RandomMinMax(0, 1);
                        if (onlyonedrop == 0) cont.DropItem(new BoneContainer(Utility.RandomList(3789, 3790, 3792)));
                        if (onlyonedrop == 1) cont.DropItem(new Gravestone(Utility.RandomList(4466, 4476, 4473, 4477)));

                        if (Utility.RandomDouble() <= 0.30) //30% chance to drop a statue
                        {
                            int whichone = Utility.RandomMinMax(0, 1);
                            if (whichone == 0) mx = new MonsterStatuette(m_Monster[6]);
                            if (whichone == 1) mx = new MonsterStatuette(m_Monster[7]);
                            mx.LootType = LootType.Regular;         // not blessed
                            cont.DropItem(mx);              // drop it baby!
                        }
                        break;
                    }

                case ChestThemeType.Pirate:
                    {

                        Bow special = new Bow();
                        special.Name = "Bow of the Buccaneer";
                        cont.DropItem(Loot.ImbueWeaponOrArmor(noThrottle: true, special, Loot.ImbueLevel.Level6 /*6*/, 0, true));

                        int onlyonedrop = Utility.RandomMinMax(0, 1);
                        PirateHat hat = new PirateHat();
                        hat.Hue = 0x1;
                        int oars = Utility.RandomMinMax(0, 1); //2 oar types

                        if (onlyonedrop == 0)
                        {
                            if (oars == 0) cont.DropItem(new Oars1());
                            if (oars == 1) cont.DropItem(new Oars2());
                        }

                        if (onlyonedrop == 1) cont.DropItem(new GenieBottle(false)); //lamp currently disabled genie not done
                        if (Utility.RandomDouble() <= 0.50) cont.DropItem(hat); // 50% chance at black piratehat

                        if (Utility.RandomDouble() <= 0.30) //30% chance to drop a statue
                        {
                            int whichone = Utility.RandomMinMax(0, 1);
                            if (whichone == 0) mx = new MonsterStatuette(m_Monster[8]);
                            if (whichone == 1) mx = new MonsterStatuette(m_Monster[9]);
                            mx.LootType = LootType.Regular;                 // not blessed
                            cont.DropItem(mx);                      // drop it baby!
                        }
                        break;
                    }

                case ChestThemeType.Dragon:
                    {
                        WarFork special = new WarFork();
                        special.Name = "Claw of the Dragon";
                        cont.DropItem(Loot.ImbueWeaponOrArmor(noThrottle: true, special, Loot.ImbueLevel.Level6 /*6*/, 0, true));

                        int onlyonedrop = Utility.RandomMinMax(0, 1);
                        //new dragonhead trophydeed type
                        if (onlyonedrop == 0) cont.DropItem(new TrophyDeed(8757, 8756, "a dragon head trophy", "a dragon head trophy", 10));
                        int armor = Utility.RandomMinMax(0, 2); // drop 1 piece of dragonarmor

                        if (onlyonedrop == 1)
                        {
                            if (armor == 0) cont.DropItem(new HangingDragonChest());
                            if (armor == 1) cont.DropItem(new HangingDragonLegs());
                            if (armor == 2) cont.DropItem(new HangingDragonArms());
                        }

                        if (Utility.RandomDouble() <= 0.30) //30% chance to drop a statue
                        {
                            int whichone = Utility.RandomMinMax(0, 1);
                            if (whichone == 0) mx = new MonsterStatuette(m_Monster[10]);
                            if (whichone == 1) mx = new MonsterStatuette(m_Monster[11]);
                            mx.LootType = LootType.Regular;         // not blessed
                            cont.DropItem(mx);              // drop it baby!
                        }
                        break;
                    }

                case ChestThemeType.Lizardmen:
                    {
                        if (Utility.RandomBool())
                            cont.DropItem(new LizardmansStaff());
                        else
                            cont.DropItem(new LizardmansMace());
                    }
                    break;

                case ChestThemeType.Ettin:
                    {
                        cont.DropItem(new EttinHammer());
                    }
                    break;

                case ChestThemeType.Ogre:
                    {
                        cont.DropItem(new OgresClub());
                    }
                    break;

                case ChestThemeType.Ophidian:
                    {
                        cont.DropItem(new OphidianBardiche());
                    }
                    break;

                case ChestThemeType.Skeleton:
                    {
                        switch (Utility.Random(3))
                        {
                            case 0: cont.DropItem(new SkeletonScimitar()); break;
                            case 1: cont.DropItem(new SkeletonAxe()); break;
                            case 2: cont.DropItem(new BoneMageStaff()); break;
                        }
                    }
                    break;

                case ChestThemeType.Ratmen:
                    {
                        if (Utility.RandomBool())
                            cont.DropItem(new RatmanSword());
                        else
                            cont.DropItem(new RatmanAxe());
                    }
                    break;

                case ChestThemeType.Orc:
                    {
                        switch (Utility.Random(3))
                        {
                            case 0: cont.DropItem(new OrcClub()); break;
                            case 1: cont.DropItem(new OrcMageStaff()); break;
                            case 2: cont.DropItem(new OrcLordBattleaxe()); break;
                        }
                    }
                    break;

                case ChestThemeType.Terathan:
                    {
                        switch (Utility.Random(3))
                        {
                            case 0: cont.DropItem(new TerathanStaff()); break;
                            case 1: cont.DropItem(new TerathanSpear()); break;
                            case 2: cont.DropItem(new TerathanMace()); break;
                        }
                    }
                    break;

                case ChestThemeType.FrostTroll:
                    {
                        switch (Utility.Random(3))
                        {
                            case 0: cont.DropItem(new FrostTrollClub()); break;
                            case 1: cont.DropItem(new TrollAxe()); break;
                            case 2: cont.DropItem(new TrollMaul()); break;
                        }
                    }
                    break;

            }//end switch

        }

        private static void ClearAmounts(int[] list)
        {
            for (int i = 0; i < list.Length; ++i)
                list[i] = 0;
        }

        private static void PackScroll(LockableContainer cont, int minCircle, int maxCircle)
        {
            PackScroll(cont, Utility.RandomMinMax(minCircle, maxCircle));
        }

        private static void PackScroll(LockableContainer cont, int circle)
        {
            int min = (circle - 1) * 8;

            cont.DropItem(Loot.RandomScroll(min, min + 7, SpellbookType.Regular));
        }

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

        private ArrayList m_Lifted = new ArrayList();

        private bool CheckLoot(Mobile m, bool criminalAction)
        {
            if (m_Owner == null || m == m_Owner)
                return true;

            Party p = Party.Get(m_Owner);

            if (p != null && p.Contains(m))
                return true;

            if (TreasureTheme.IsOverlandTheme(m_ThemeType) == true)
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

        private DateTime lastLift = DateTime.UtcNow;
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
            from.RevealingAction();

            // prevent the player from milking the chest by removing 'one from a stack'
            if (notYetLifted)
            {
                ArrayList tx = new ArrayList(FindItemsByType(item.GetType(), true));
                tx.Remove(item);
                if (tx.Count > 0)
                {
                    foreach (Item ix in tx)
                    {
                        if (ix.Amount > 1)
                        {   // player looks to be removing one from a stack
                            // disqualify the one they lifted
                            m_Lifted.Add(item);
                            notYetLifted = false;
                            break;
                        }
                    }

                    // if they lifted >= 1, and left 1 disqualify the one they lifted
                    if (notYetLifted)
                        if (item.Amount > 1)
                        {
                            m_Lifted.Add(item);
                            notYetLifted = false;
                        }
                }
            }

            if (notYetLifted)
            {
                m_Lifted.Add(item);
                double chance = 0.1 * (Level * .5);

                if (m_IsThemed == true && 0.1 >= Utility.RandomDouble())
                {
                    // 10% chance to spawn 2 monsters as is a Themed chest
                    TreasureTheme.Spawn(m_Level, GetWorldLocation(), Map, from, m_IsThemed, m_ThemeType, false, false);
                    TreasureTheme.Spawn(m_Level, GetWorldLocation(), Map, from, m_IsThemed, m_ThemeType, false, false);
                }
                else if (m_IsThemed == false && chance >= Utility.RandomDouble())
                    TreasureTheme.Spawn(m_Level, GetWorldLocation(), Map, from, m_IsThemed, m_ThemeType, false, false);

                // Adam: Insure IOB wearers are not stealing from their kin
                BaseCreature witness = null;
                if (CheckThief(from, out witness))
                {
                    from.SendMessage("You have been discovered stealing from your kin!");
                    if (from.Hidden)
                        from.RevealingAction();

                    // attack kin to make them come after you
                    from.DoHarmful(witness);
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

            if (pm.IOBAlignment == IOBAlignment.None)
                return false;

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


        public override bool OnDragDropInto(Mobile from, Item item, Point3D p)
        {
            from.SendLocalizedMessage(1048122, "", 0x8A5); // The chest refuses to be filled with treasure again.

            return false;
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            from.SendLocalizedMessage(1048122, "", 0x8A5); // The chest refuses to be filled with treasure again.

            return false;
        }


        public TreasureMapChest(Serial serial)
            : base(serial)
        {
        }

        [Flags]
        public enum iFlags
        {
            None = 0x00000000,
            Hides = 0x00000001,     // this chests hides a rare underneath
        }

        public void SetFlag(iFlags flag, bool value)
        {
            if (value)
                m_flags |= flag;
            else
                m_flags &= ~flag;
        }

        public bool GetFlag(iFlags flag)
        {
            return ((m_flags & flag) != 0);
        }

        private iFlags m_flags = iFlags.None;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            // version 3
            writer.Write((int)m_flags);

            // version 2
            writer.Write(m_IsThemed);
            writer.Write((int)m_ThemeType);

            writer.Write(m_Owner);

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
                        m_flags = (iFlags)reader.ReadInt();
                        goto case 2;
                    }

                case 2:
                    {
                        m_IsThemed = reader.ReadBool();
                        m_ThemeType = (ChestThemeType)reader.ReadInt();
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

            base.OnAfterDelete();
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive)
                list.Add(new RemoveEntry(from, this));
        }

        public void BeginRemove(Mobile from)
        {
            if (!from.Alive)
                return;

            //from.CloseGump( typeof( RemoveGump ) );
            //from.SendGump( new RemoveGump( from, this ) );
            if (this.Items.Count > 0)
                from.SendMessage("That chest is not yet empty.");
            else
                EndRemove(from);
        }

        public void EndRemove(Mobile from)
        {
            if (Deleted || !from.InRange(GetWorldLocation(), 3))
                return;

            from.SendLocalizedMessage(1048124, "", 0x8A5); // The old, rusted chest crumbles when you hit it.
            if (GetFlag(iFlags.Hides))
            {
                Item rare = UnderRare();
                if (rare != null)
                {
                    if (rare is MetalGoldenChest)
                        from.SendMessage(0x8A5, "The chest seems to now be movable.");
                    else
                        from.SendMessage(0x8A5, "You see something underneath the chest.");
                    rare.MoveToWorld(this.Location, this.Map);
                }
            }
            this.Delete();
        }


        private Item UnderRare()
        {
            Item item = null;
            switch (Utility.Random(3))
            {
                case 0: // 1 in 10
                    if (Utility.RandomChance(10))
                        switch (Utility.Random(5))
                        {
                            case 0: item = new Item(2463); break;           // (0x099) Bottle of ale � renamed �lost pirate rum�
                            case 1: item = new Item(2464); break;           // (0x0A0) Bottles of ale � renamed �lost pirate rum�
                            case 2: item = new Item(2465); break;           // (0x0A1) Bottles of ale � renamed �lost pirate rum�
                            case 3: item = new Item(2466); break;           // (0x0A2) Bottles of ale � renamed �lost pirate rum�
                            case 4: item = new MetalGoldenChest(); break;   // treasure chest style 
                        }
                    if (item != null && item is MetalGoldenChest == false)
                    {
                        item.Weight = 1;
                        item.Name = "lost pirate rum";
                    }
                    break;

                case 1: // 1 in 20
                    if (Utility.RandomChance(5))
                        switch (Utility.Random(4))
                        {   // these names will result in a completed item called for instance "skeleton"
                            case 0: item = new Item(7562); item.Name = "mummy"; break; // (0x1D8A) mummy head
                            case 1: item = new Item(7563); item.Name = "mummy"; break; // (0x1D8B) mummy legs
                            case 2: item = new Item(7567); break; // (0x1D8F) skeleton head
                            case 3: item = new Item(7566); break; // (0x1D8E) skeleton legs
                        }
                    if (item != null)
                        item.Weight = 6;
                    break;

                case 2: // 1 in 100
                    if (Utility.RandomChance(1))
                        switch (Utility.Random(5))
                        {
                            case 0: item = new Item(7967); item.Weight = 3; item.Name = "soul jar"; item.Hue = Utility.RandomBirdHue(); break; // (0x1F1F) head � renamed �soul jar�
                            case 1: item = new Item(7960); item.Weight = 7; item.Name = "grotesque skull"; item.Hue = Utility.RandomSnakeHue(); break; // (0x1F18) statue � renamed �grotesque skull�
                            case 2: item = new Item(8700); item.Weight = 6; break; // (0x21FC) skull spikes
                            case 3: item = new Item(5367); item.Weight = 9; break; // (0x14F7) Anchor (east)
                            case 4: item = new Item(5369); item.Weight = 9; break; // (0x14F9) Anchor (south)
                        }
                    break;
            }

            return item;
        }

        private class RemoveGump : Gump
        {
            private Mobile m_From;
            private TreasureMapChest m_Chest;

            public RemoveGump(Mobile from, TreasureMapChest chest)
                : base(15, 15)
            {
                m_From = from;
                m_Chest = chest;

                Closable = false;
                Disposable = false;

                AddPage(0);

                AddBackground(30, 0, 240, 240, 2620);

                AddHtmlLocalized(45, 15, 200, 80, 1048125, 0xFFFFFF, false, false); // When this treasure chest is removed, any items still inside of it will be lost.
                AddHtmlLocalized(45, 95, 200, 60, 1048126, 0xFFFFFF, false, false); // Are you certain you're ready to remove this chest?

                AddButton(40, 153, 4005, 4007, 1, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 155, 180, 40, 1048127, 0xFFFFFF, false, false); // Remove the Treasure Chest

                AddButton(40, 195, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddHtmlLocalized(75, 197, 180, 35, 1006045, 0xFFFFFF, false, false); // Cancel
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                if (info.ButtonID == 1)
                    m_Chest.EndRemove(m_From);
            }
        }

        private class RemoveEntry : ContextMenuEntry
        {
            private Mobile m_From;
            private TreasureMapChest m_Chest;

            public RemoveEntry(Mobile from, TreasureMapChest chest)
                : base(6149, 3)
            {
                m_From = from;
                m_Chest = chest;

                Enabled = (from == chest.Owner || chest.Items.Count == 0);
            }

            public override void OnClick()
            {
                if (m_Chest.Deleted || !m_From.CheckAlive())
                    return;

                m_Chest.BeginRemove(m_From);
            }
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
                m_Item.Delete();
            }
        }

        public override void OnTelekinesis(Mobile from)
        {
            //Do nothing, telekinesis doesn't work on a TMap.
        }

        public override bool AutoResetTrap
        {
            /* 5/21/23, Yoar: Treasure chest traps auto-reset on AI/MO
             * Remove trap is necessary to open these chests
             */
            get { return SkillHandlers.RemoveTrap.EraAI; }
        }
    }
}