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

/* Engines/Craft/DefTinkering.cs
 * CHANGELOG:
 *  10/12/21, Yoar
 *      Changed return type of CraftSystem.PlayEndingEffect to "object" to support strings
 *  9/19/21, Yoar
 *      CustomCraft.CompleteCraft: Changed "out int message" to "out object message" to support strings 
 *	8/03/06, weaver
 *		Corrected resource error messages to correctly reflect resource 
 *		missing from those required.
 *	7/22/06, weaver
 *		Corrected tent craft entry to create type SiegeTentBag as opposed to
 *		TentBag (left over from previous version which allowed construction of
 *		tents).
 *	5/22/06, weaver
 *		Added tent poles, changed siege tents to require tent poles.
 *	5/03/06, weaver
 *		Added tent bags as a craftable (95% success at GM).
 *		Changed comment instances of 'erlein' to 'weaver'.
 *	8/18/05, weaver
 *		Added gold + silver rings + bracelets.
 *	8/1/05, weaver
 *		Fixed mega cliloc error.
 *	8/1/05, weaver
 *		Added black staff and pitchfork.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *  5/18/2004, pixie
 *		Added Tinker Traps
 *	5/02/2004, pixie
 *		Added KeyRing and Key
 *	6,april,2004 by sam
 *		commented out line 183 
 */

using Server.Factions;
using Server.Items;
using Server.Targeting;
using System;

namespace Server.Engines.Craft
{
    public class DefTinkering : CraftSystem
    {
        public override SkillName MainSkill
        {
            get { return SkillName.Tinkering; }
        }

        public override TextDefinition GumpTitle
        {
            get { return 1044007; } // <CENTER>TINKERING MENU</CENTER>
        }

        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DefTinkering();

                return m_CraftSystem;
            }
        }

        private DefTinkering()
            : base(1, 1, 1.25)// base( 1, 1, 3.0 )
        {
        }

        public override double GetChanceAtMin(CraftItem item)
        {
            if (item.Name == 1044258) // potion keg
                return 0.5; // 50%

            return 0.0; // 0%
        }

        public override TextDefinition CanCraft(Mobile from, BaseTool tool, Type itemType)
        {
            if (tool.Deleted || tool.UsesRemaining < 0)
                return 1044038; // You have worn out your tool!
            else if (!BaseTool.CheckAccessible(tool, from))
                return 1044263; // The tool must be on your person to use.

            return 0;
        }

        public override void PlayCraftEffect(Mobile from, object obj = null)
        {
            // no sound
            //from.PlaySound( 0x241 );
        }

        private static Type[] m_TinkerColorables = new Type[]
            {
                typeof( ForkLeft ), typeof( ForkRight ),
                typeof( SpoonLeft ), typeof( SpoonRight ),
                typeof( KnifeLeft ), typeof( KnifeRight ),
                typeof( Plate ),
                typeof( Goblet ), typeof( PewterMug ),
                typeof( KeyRing ), typeof( Key ),
                typeof( Candelabra ), typeof( Scales ),
                typeof( Spyglass ), typeof( Lantern ),
                typeof( HeatingStand ), typeof( Globe ),
            };

        public override bool RetainsColorFrom(CraftItem item, Type type)
        {
            if (!type.IsSubclassOf(typeof(BaseIngot)))
                return false;

            type = item.ItemType;

            bool contains = false;

            for (int i = 0; !contains && i < m_TinkerColorables.Length; ++i)
                contains = (m_TinkerColorables[i] == type);

            return contains;
        }

        public override TextDefinition PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken)
                from.SendLocalizedMessage(1044038); // You have worn out your tool

            if (failed)
            {
                if (lostMaterial)
                    return 1044043; // You failed to create the item, and some of your materials are lost.
                else
                    return 1044157; // You failed to create the item, but no materials were lost.
            }
            else
            {
                if (quality == 0)
                    return 502785; // You were barely able to make this item.  It's quality is below average.
                else if (makersMark && quality == 2)
                    return 1044156; // You create an exceptional quality item and affix your maker's mark.
                else if (quality == 2)
                    return 1044155; // You create an exceptional quality item.
                else
                    return 1044154; // You create the item.
            }
        }

        public override bool ConsumeResOnFailure(Mobile from, Type resourceType, CraftItem craftItem)
        {
            if (resourceType == typeof(Silver))
                return false;

            return base.ConsumeResOnFailure(from, resourceType, craftItem);
        }

        public void AddJewelrySet(GemType gemType, Type itemType)
        {
            int offset = (int)gemType - 1;

            int index = AddCraft(typeof(GoldRing), 1044049, 1044176 + offset, 40.0, 90.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddRes(index, itemType, 1044231 + offset, 1, 1044240);

            index = AddCraft(typeof(SilverBeadNecklace), 1044049, 1044185 + offset, 40.0, 90.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddRes(index, itemType, 1044231 + offset, 1, 1044240);

            index = AddCraft(typeof(GoldNecklace), 1044049, 1044194 + offset, 40.0, 90.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddRes(index, itemType, 1044231 + offset, 1, 1044240);

            index = AddCraft(typeof(GoldEarrings), 1044049, 1044203 + offset, 40.0, 90.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddRes(index, itemType, 1044231 + offset, 1, 1044240);

            index = AddCraft(typeof(GoldBeadNecklace), 1044049, 1044212 + offset, 40.0, 90.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddRes(index, itemType, 1044231 + offset, 1, 1044240);

            index = AddCraft(typeof(GoldBracelet), 1044049, 1044221 + offset, 40.0, 90.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddRes(index, itemType, 1044231 + offset, 1, 1044240);
        }

        public override void InitCraftList()
        {
            // Wooden items
            AddCraft(typeof(JointingPlane), 1044042, 1024144, 0.0, 50.0, typeof(Log), 1044041, 4, 1044351);
            AddCraft(typeof(MouldingPlane), 1044042, 1024140, 0.0, 50.0, typeof(Log), 1044041, 4, 1044351);
            AddCraft(typeof(SmoothingPlane), 1044042, 1024146, 0.0, 50.0, typeof(Log), 1044041, 4, 1044351);
            AddCraft(typeof(ClockFrame), 1044042, 1024173, 0.0, 50.0, typeof(Log), 1044041, 6, 1044351);
            AddCraft(typeof(Axle), 1044042, 1024187, -25.0, 25.0, typeof(Log), 1044041, 2, 1044351);
            AddCraft(typeof(RollingPin), 1044042, 1024163, 0.0, 50.0, typeof(Log), 1044041, 5, 1044351);
            AddCraft(typeof(BlackStaff), 1044042, 1023568, 55.0, 100.0, typeof(Log), 1044041, 12, 1044351);

            // Tools
            AddCraft(typeof(Scissors), 1044046, 1023998, 5.0, 55.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(MortarPestle), 1044046, 1023739, 20.0, 70.0, typeof(IronIngot), 1044036, 3, 1044037);
            AddCraft(typeof(Scorp), 1044046, 1024327, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(TinkerTools), 1044046, 1044164, 10.0, 60.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(Hatchet), 1044046, 1023907, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(DrawKnife), 1044046, 1024324, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(SewingKit), 1044046, 1023997, 10.0, 70.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(Saw), 1044046, 1024148, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(DovetailSaw), 1044046, 1024136, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(Froe), 1044046, 1024325, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(Shovel), 1044046, 1023898, 40.0, 90.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(Hammer), 1044046, 1024138, 30.0, 80.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(Tongs), 1044046, 1024028, 35.0, 85.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(SmithHammer), 1044046, 1025091, 40.0, 90.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(Inshave), 1044046, 1024326, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(Pickaxe), 1044046, 1023718, 40.0, 90.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(Lockpick), 1044046, 1025371, 45.0, 95.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(Skillet), 1044046, 1044567, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(FlourSifter), 1044046, 1024158, 50.0, 100.0, typeof(IronIngot), 1044036, 3, 1044037);
            if (Core.RuleSets.StandardShardRules())
            {
                AddCraft(typeof(FletcherTools), 1044046, 1044166, 35.0, 85.0, typeof(IronIngot), 1044036, 3, 1044037);
            }
            AddCraft(typeof(MapmakersPen), 1044046, 1044167, 25.0, 75.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(ScribesPen), 1044046, 1044168, 25.0, 75.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(Pitchfork), 1044046, 1023719, 40.0, 90.0, typeof(IronIngot), 1044036, 6, 1044037);

            // Parts
            AddCraft(typeof(Gears), 1044047, 1024179, 5.0, 55.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(ClockParts), 1044047, 1024175, 25.0, 75.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(BarrelTap), 1044047, 1024100, 35.0, 85.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(Springs), 1044047, 1024189, 5.0, 55.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(SextantParts), 1044047, 1024185, 30.0, 80.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(BarrelHoops), 1044047, 1024321, -15.0, 35.0, typeof(IronIngot), 1044036, 5, 1044037);
            AddCraft(typeof(Hinge), 1044047, 1024181, 5.0, 55.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(BolaBall), 1044047, 1023699, 45.0, 95.0, typeof(IronIngot), 1044036, 10, 1044037);

            // Utensils
            AddCraft(typeof(ButcherKnife), 1044048, 1025110, 25.0, 75.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(SpoonLeft), 1044048, 1044158, 0.0, 50.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(SpoonRight), 1044048, 1044159, 0.0, 50.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(Plate), 1044048, 1022519, 0.0, 50.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(ForkLeft), 1044048, 1044160, 0.0, 50.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(ForkRight), 1044048, 1044161, 0.0, 50.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(Cleaver), 1044048, 1023778, 20.0, 70.0, typeof(IronIngot), 1044036, 3, 1044037);
            AddCraft(typeof(KnifeLeft), 1044048, 1044162, 0.0, 50.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(KnifeRight), 1044048, 1044163, 0.0, 50.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddCraft(typeof(Goblet), 1044048, 1022458, 10.0, 60.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(PewterMug), 1044048, 1024097, 10.0, 60.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(SkinningKnife), 1044048, 1023781, 25.0, 75.0, typeof(IronIngot), 1044036, 2, 1044037);

            // Misc
            AddCraft(typeof(KeyRing), 1044050, 1024113, 10.0, 60.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(Candelabra), 1044050, 1022599, 55.0, 105.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(Scales), 1044050, 1026225, 60.0, 110.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(Key), 1044050, 1024112, 20.0, 70.0, typeof(IronIngot), 1044036, 3, 1044037, KeyType.Iron);
            AddCraft(typeof(Globe), 1044050, 1024167, 55.0, 105.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(Spyglass), 1044050, 1025365, 60.0, 110.0, typeof(IronIngot), 1044036, 4, 1044037);
            AddCraft(typeof(Lantern), 1044050, 1022597, 30.0, 80.0, typeof(IronIngot), 1044036, 2, 1044037);
            AddCraft(typeof(HeatingStand), 1044050, 1026217, 60.0, 110.0, typeof(IronIngot), 1044036, 4, 1044037);

            int index = 0;
            if (Core.RuleSets.AngelIslandRules())
            {
                // wea: added siege tent
                index = AddCraft(typeof(TentPole), 1044050, "tent pole", 75.0, 101.32, typeof(Board), "Board", 15, 1044351);
                AddRes(index, typeof(IronIngot), 1044036, 10, 1044037);

                index = AddCraft(typeof(SiegeTentBag), 1044050, "siege tent", 75.0, 101.32, typeof(Cloth), "Cloth", 100, 1044287);
                AddRes(index, typeof(TentPole), "Tent Pole", 15);
            }

            // Weapons

            if (Core.RuleSets.EnchantedScrollsEnabled())
                AddCraft(typeof(Wand), 1044566, 1023570, 60.0, 110.0, typeof(IronIngot), 1044036, 6, 1044037);

            // Jewelry

            AddCraft(typeof(Necklace), 1044049, "necklace", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);

            AddCraft(typeof(GoldRing), 1044049, "gold ring", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);
            AddCraft(typeof(GoldBracelet), 1044049, "gold bracelet", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);
            AddCraft(typeof(GoldNecklace), 1044049, "gold necklace", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);
            AddCraft(typeof(GoldEarrings), 1044049, "gold earrings", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);

            AddCraft(typeof(SilverRing), 1044049, "silver ring", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);
            AddCraft(typeof(SilverBracelet), 1044049, "silver bracelet", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);
            AddCraft(typeof(SilverNecklace), 1044049, "silver necklace", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);
            AddCraft(typeof(SilverEarrings), 1044049, "silver earrings", 40.0, 90.0, typeof(IronIngot), "Ingots", 2, 1044037);

            AddJewelrySet(GemType.StarSapphire, typeof(StarSapphire));
            AddJewelrySet(GemType.Emerald, typeof(Emerald));
            AddJewelrySet(GemType.Sapphire, typeof(Sapphire));
            AddJewelrySet(GemType.Ruby, typeof(Ruby));
            AddJewelrySet(GemType.Citrine, typeof(Citrine));
            AddJewelrySet(GemType.Amethyst, typeof(Amethyst));
            AddJewelrySet(GemType.Tourmaline, typeof(Tourmaline));
            AddJewelrySet(GemType.Amber, typeof(Amber));
            AddJewelrySet(GemType.Diamond, typeof(Diamond));

            // Multi-Component Items
            index = AddCraft(typeof(AxleGears), 1044051, 1024177, 0.0, 0.0, typeof(Axle), 1044169, 1, 1044253);
            AddRes(index, typeof(Gears), 1044254, 1, 1044253);

            index = AddCraft(typeof(ClockParts), 1044051, 1024175, 0.0, 0.0, typeof(AxleGears), 1044170, 1, 1044253);
            AddRes(index, typeof(Springs), 1044171, 1, 1044253);

            index = AddCraft(typeof(SextantParts), 1044051, 1024185, 0.0, 0.0, typeof(AxleGears), 1044170, 1, 1044253);
            AddRes(index, typeof(Hinge), 1044172, 1, 1044253);

            index = AddCraft(typeof(ClockRight), 1044051, 1044257, 0.0, 0.0, typeof(ClockFrame), 1044174, 1, 1044253);
            AddRes(index, typeof(ClockParts), 1044173, 1, 1044253);

            index = AddCraft(typeof(ClockLeft), 1044051, 1044256, 0.0, 0.0, typeof(ClockFrame), 1044174, 1, 1044253);
            AddRes(index, typeof(ClockParts), 1044173, 1, 1044253);

            AddCraft(typeof(Sextant), 1044051, 1024183, 0.0, 0.0, typeof(SextantParts), 1044175, 1, 1044253);

            index = AddCraft(typeof(Bola), 1044051, 1046441, 60.0, 110.0, typeof(BolaBall), 1046440, 4, 1042613);
            AddRes(index, typeof(Leather), 1044462, 3, 1044463);

            index = AddCraft(typeof(PotionKeg), 1044051, 1044258, 75.0, 100.0, typeof(Keg), 1044255, 1, 1044253);
            AddRes(index, typeof(BarrelTap), 1044252, 1, 1044253);
            AddRes(index, typeof(BarrelLid), 1044251, 1, 1044253);
            AddRes(index, typeof(Bottle), 1044250, 10, 1044253);

            #region Traps
#if old
            #region OLD
			//dart
			//index = AddCraft(typeof(DartTinkerTrap), "Traps", "Dart trap", 30, 80, typeof(IronIngot), "Ingots", 1);
			//AddRes(index, typeof(Bolt), "Bolt", 1);
			index = AddCraft(typeof(DartTinkerTrap), 1044052, 1024396, 30.0, 80.0, typeof(IronIngot), 1044036, 1, 1044037);
			AddRes(index, typeof(Bolt), 1044570, 1, 1044253);

			//poison
			index = AddCraft(typeof(PoisonTinkerTrap), "Traps", "Poison trap", 30, 80, typeof(IronIngot), "Ingots", 1);
			AddRes(index, typeof(BasePoisonPotion), "Poison Potion", 1);

			//explosion
			index = AddCraft(typeof(ExplosionTinkerTrap), "Traps", "Explosion trap", 55, 105, typeof(IronIngot), "Ingots", 1);
			AddRes(index, typeof(BaseExplosionPotion), "Purple Potion", 1);
            #endregion
#else
            // Dart Trap
            index = AddCraft(typeof(DartTrapCraft), 1044052, 1024396, 30.0, 80.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddRes(index, typeof(Bolt), 1044570, 1, 1044253);

            // Poison Trap
            index = AddCraft(typeof(PoisonTrapCraft), 1044052, 1044593, 30.0, 80.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddRes(index, typeof(BasePoisonPotion), 1044571, 1, 1044253);

            // Explosion Trap
            index = AddCraft(typeof(ExplosionTrapCraft), 1044052, 1044597, 55.0, 105.0, typeof(IronIngot), 1044036, 1, 1044037);
            AddRes(index, typeof(BaseExplosionPotion), 1044569, 1, 1044253);
#endif
            if (Core.Factions)
            {
                // Faction Gas Trap
                index = AddCraft(typeof(FactionGasTrapDeed), 1044052, 1044598, 65.0, 115.0, typeof(Silver), 1044572, Core.RuleSets.AOSRules() ? 250 : 1000, 1044253);
                AddRes(index, typeof(IronIngot), 1044036, 10, 1044037);
                AddRes(index, typeof(BasePoisonPotion), 1044571, 1, 1044253);

                // Faction explosion Trap
                index = AddCraft(typeof(FactionExplosionTrapDeed), 1044052, 1044599, 65.0, 115.0, typeof(Silver), 1044572, Core.RuleSets.AOSRules() ? 250 : 1000, 1044253);
                AddRes(index, typeof(IronIngot), 1044036, 10, 1044037);
                AddRes(index, typeof(BaseExplosionPotion), 1044569, 1, 1044253);

                // Faction Saw Trap
                index = AddCraft(typeof(FactionSawTrapDeed), 1044052, 1044600, 65.0, 115.0, typeof(Silver), 1044572, Core.RuleSets.AOSRules() ? 250 : 1000, 1044253);
                AddRes(index, typeof(IronIngot), 1044036, 10, 1044037);
                AddRes(index, typeof(Gears), 1044254, 1, 1044253);

                // Faction Spike Trap			
                index = AddCraft(typeof(FactionSpikeTrapDeed), 1044052, 1044601, 65.0, 115.0, typeof(Silver), 1044572, Core.RuleSets.AOSRules() ? 250 : 1000, 1044253);
                AddRes(index, typeof(IronIngot), 1044036, 10, 1044037);
                AddRes(index, typeof(Springs), 1044171, 1, 1044253);

                // Faction trap removal kit
                index = AddCraft(typeof(FactionTrapRemovalKit), 1044052, 1046445, 90.0, 115.0, typeof(Silver), 1044572, 500, 1044253);
                AddRes(index, typeof(IronIngot), 1044036, 10, 1044037);
            }
            #endregion

            // Set the overidable material
            SetSubRes(typeof(IronIngot), 1044022);

            // Add every material you want the player to be able to chose from
            // This will overide the overidable material
            AddSubRes(typeof(IronIngot), 1044022, 00.0, 1044036, 1044267);
            AddSubRes(typeof(DullCopperIngot), 1044023, 65.0, 1044036, 1044268);
            AddSubRes(typeof(ShadowIronIngot), 1044024, 70.0, 1044036, 1044268);
            AddSubRes(typeof(CopperIngot), 1044025, 75.0, 1044036, 1044268);
            AddSubRes(typeof(BronzeIngot), 1044026, 80.0, 1044036, 1044268);
            AddSubRes(typeof(GoldIngot), 1044027, 85.0, 1044036, 1044268);
            AddSubRes(typeof(AgapiteIngot), 1044028, 90.0, 1044036, 1044268);
            AddSubRes(typeof(VeriteIngot), 1044029, 95.0, 1044036, 1044268);
            AddSubRes(typeof(ValoriteIngot), 1044030, 99.0, 1044036, 1044268);

            MarkOption = true;
            Repair = true;
        }
    }

    public abstract class TrapCraft : CustomCraft
    {
        private LockableContainer m_Container;

        public LockableContainer Container { get { return m_Container; } }

        public abstract TrapType TrapType { get; }

        public TrapCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
        }

        private int Verify(LockableContainer container)
        {
            if (container == null || container.KeyValue == 0)
                return 1005638; // You can only trap lockable chests.
            if (From.Map != container.Map || !From.InRange(container.GetWorldLocation(), 2))
                return 500446; // That is too far away.
            if (!container.Movable)
                return 502944; // You cannot trap this item because it is locked down.
            if (!container.IsAccessibleTo(From))
                return 502946; // That belongs to someone else.
            if (container.Locked)
                return 502943; // You can only trap an unlocked object.
            if (container.TrapType != TrapType.None)
                return 502945; // You can only place one trap on an object at a time.

            return 0;
        }

        private bool Acquire(object target, out int message)
        {
            LockableContainer container = target as LockableContainer;

            message = Verify(container);

            if (message > 0)
            {
                return false;
            }
            else
            {
                m_Container = container;
                return true;
            }
        }

        public override void EndCraftAction()
        {
            From.SendLocalizedMessage(502921); // What would you like to set a trap on?
            From.Target = new ContainerTarget(this);
        }

        private class ContainerTarget : Target
        {
            private TrapCraft m_TrapCraft;

            public ContainerTarget(TrapCraft trapCraft)
                : base(-1, false, TargetFlags.None)
            {
                m_TrapCraft = trapCraft;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                int message;

                if (m_TrapCraft.Acquire(targeted, out message))
                    m_TrapCraft.CraftItem.CompleteCraft(m_TrapCraft.Quality, false, m_TrapCraft.From, m_TrapCraft.CraftSystem, m_TrapCraft.TypeRes, m_TrapCraft.Tool, m_TrapCraft);
                else
                    Failure(message);
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (cancelType == TargetCancelType.Canceled)
                    Failure(0);
            }

            private void Failure(int message)
            {
                Mobile from = m_TrapCraft.From;
                BaseTool tool = m_TrapCraft.Tool;

                if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                    from.SendGump(new CraftGump(from, m_TrapCraft.CraftSystem, tool, message));
                else if (message > 0)
                    from.SendLocalizedMessage(message);
            }
        }

        public override Item CompleteCraft(out TextDefinition message)
        {
            int num;

            message = num = Verify(this.Container);

            if (num == 0)
            {
                int trapLevel = (int)(From.Skills.Tinkering.Value / 10);

                Container.TrapType = this.TrapType;
                Container.TrapPower = trapLevel * 9;
                Container.TrapLevel = trapLevel;
                Container.TrapEnabled = false;

                if (Core.OldStyleTinkerTrap)
                    Container.Trapper = CalcTrapper();      // who takes responsibility for any deaths

                message = 1005639; // Trap is disabled until you lock the chest.
            }

            return null;
        }

        /* from stratics - 2002, tinker trap essay
		 * (OLD-STYLE TRAPS)
		 * Creating the trap safely
		 * Trapping a moveable (player made, highlights yellow) chest that is located in either a building or boat that you own is not a crime of any sort
		 *	(keeping the key in your pack while creating the box helps ensure this). This is a defensive measure for players to protect their possessions. 
		 *	Taking the chest outside of the building makes no difference on criminal behavior. In fact, any chest created inside a building or boat that
		 *	you own has no 'ownership' at all, and can be used without fear of criminal consequence.
		 * Note: the chest must be on the floor when you trap it in order to avoid responsibility for the trap.
		 * Note: currently, "the boat that you own" means that you are carrying a key to the boat in your backpack, in the upper level. 
		 *	Do not bury the key several packs down.
		 * Trapping a non-moveable (dungeon, town, monster camp etc.) chest or a player made chest that is not in a building or boat that you own 
		 *	will set the trapper as the controller of the chest. If the trap damages an Innocent the trapper will be flagged as a Criminal and Aggressor
		 *	(to the victim). If the Innocent dies, he/she can report the trapper as a Murderer.
		 * Adam's Note: stratics says a houes that you own, but the UOSecondAge website says at least a friend.
		 * "The tinker that makes a trap cannot be given a murder count (when an innocent is killed by it) ONLY IF he traps the box while it is on 
		 *	the floor of a house which he is at least friended to. If he traps while it is on the ground outside of his house, or if it is in 
		 *	another container a murder count can be given to him."
		 * 
		 * (NEW-STYLE TRAPS)
		 * 
		 * From UO.
		 * Tinker Traps
		 * 	Tinker traps will be modified. Their purpose will be to protect containers and their creation will no longer give the maker a murder count.
		 * A trapped chest cannot be opened until the trap is disarmed and the lock removed, except:
		 * The owner of the chest can access the chest automatically without firing the trap. The owner is defined as the last person to lock the chest.
		 * Using the key on the chest will bypass the trap.
		 * The lock on a trapped chest cannot be picked until the trap is disarmed.
		 * A failed disarm attempt will result in setting off the trap.
		 * A trap that is set off will reset itself automatically indefinitely.
		 * Damage from a trap will not cause the maker of the trap to be a candidate for a murder count.
		 * Disarming the chest will not automatically unlock it.
		 * 
		 * ** More info supporting the notion that 
		 * (1)the chest cannot be picked successfully, and so it remains locked, 
		 * (2)that the owner can open the chest without unlocking it.
		 * 
		 * A tinker can successfully trap a container. The next person who illegally tries to open the container sets off the trap and takes the damage. 
		 * The container may be safely opened by the owner (the last person to lock the chest), or by using the proper key. 
		 * The lock on a trapped chest cannot be picked until the trap has been disarmed. Be careful with giving away trapped containers, 
		 * it might bring murder counts for the tinker if the trap kills innocent players. The strength of your trap is directly related to your 
		 * tinkering skill.
		 * http://www.gatecentral.com/ultima/wissen/sk_tinkering_l12.php
		 */
        public Mobile CalcTrapper()
        {
            // on the floor of your house or on the deck of your boat are the only safe places to trap this container.
            Multis.BaseBoat boat = Multis.BaseBoat.FindBoatAt(From);
            Multis.BaseHouse house = Multis.BaseHouse.FindHouseAt(From);

            if (boat != null)
            {   // they are on a boat
                // if they have the boat key, they are considered the owner.
                if (boat.HasKey(From) && Container.Parent == null /*!Container.IsChildOf(From.Backpack)*/)
                    return null;        // safe!
            }
            else if (house != null)
            {   // they are in their house
                if (house.IsFriend(From) && Container.Parent == null /*!Container.IsChildOf(From.Backpack)*/)
                    return null;        // safe!
            }
            else
                // they failed all tests, so they are Guilty!
                return From;

            // they were in a house or on a boat but failed the tests
            return From;
        }
    }

    [CraftItemID(0x1BFC)]
    public class DartTrapCraft : TrapCraft
    {
        public override TrapType TrapType { get { return TrapType.DartTrap; } }

        public DartTrapCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
        }
    }

    [CraftItemID(0x113E)]
    public class PoisonTrapCraft : TrapCraft
    {
        public override TrapType TrapType { get { return TrapType.PoisonTrap; } }

        public PoisonTrapCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
        }
    }

    [CraftItemID(0x370C)]
    public class ExplosionTrapCraft : TrapCraft
    {
        public override TrapType TrapType { get { return TrapType.ExplosionTrap; } }

        public ExplosionTrapCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality)
            : base(from, craftItem, craftSystem, typeRes, tool, quality)
        {
        }
    }
}