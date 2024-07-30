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

/* Scripts\Mobiles\Vendors\SBInfo\SBBlacksmith.cs
 * ChangeLog
 *  11/8/22, Adam: (Vendor System)
 *  Complete overhaul of the vendor system:
 * - DisplayCache:
 *   Display cache objects are now strongly typed and there is a separate list for each.
 *   I still dislike the fact it is a �Container�, but we can live with that.
 *   Display cache objects are serialized and are deleted on each server restart as the vendors rebuild the list.
 *   Display cache objects are marked as �int map storage� so Cron doesn�t delete them.
 * - ResourcePool:
 *   Properly exclude all ResourcePool interactions when resource pool is not being used. (Buy/Sell now works correctly without ResourcePool.)
 *   Normalize/automate all ResourcePool resources for purchase/sale within a vendor. If a vendor Sells X, he will Buy X. 
 *   At the top of each SB<merchant> there is a list of ResourcePool types. This list is uniformly looped over for creating all ResourcePool buy/sell entries.
 * - Standard Pricing Database
 *   No longer do we hardcode in what we believe the buy/sell price of X is. We now use a Standard Pricing Database for assigning these prices.
 *   I.e., BaseVendor.PlayerPays(typeof(Drums)), or BaseVendor.VendorPays(typeof(Drums))
 *   This database was derived from RunUO 2.6 first and added items not covered from Angel Island 5.
 *   The database was then filtered, checked, sorted and committed. 
 * - Make use of Rule Sets as opposed to hardcoding shard configurations everywhere.
 *   Exampes:
 *   if (Core.UOAI_SVR) => if (Core.RuleSets.AngelIslandRules())
 *   if (Server.Engines.ResourcePool.ResourcePool.Enabled) => if (Core.RuleSets.ResourcePoolRules())
 *   etc. In this way we centrally adjust who sell/buys what when. And using the SPD above, for what price.
 *  3/12/11, Adam
 *		First time checkin
 *		moved from runuo 2.0
 *		updated to add Balanced buyback and to eliminate cash buyback on Siege
 */

using Server.Engines.ResourcePool;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class SBBlacksmith : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();
        private static List<Type> BBSTypes = new List<Type>() { typeof(IronIngot), typeof(DullCopperIngot), typeof(ShadowIronIngot), typeof(CopperIngot), typeof(BronzeIngot), typeof(GoldIngot), typeof(AgapiteIngot), typeof(VeriteIngot), typeof(ValoriteIngot), typeof(IronOre), typeof(DullCopperOre), typeof(ShadowIronOre), typeof(CopperOre), typeof(BronzeOre), typeof(GoldOre), typeof(AgapiteOre), typeof(VeriteOre), typeof(ValoriteOre) };

        public SBBlacksmith()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback
                    foreach (Type type in BBSTypes)
                        if (ResourcePool.IsPooledResource(type))
                            Add(new GenericBuyInfo(type));
#if false
                    Add(new GenericBuyInfo(typeof(IronIngot)));
                    Add(new GenericBuyInfo(typeof(DullCopperIngot)));
                    Add(new GenericBuyInfo(typeof(ShadowIronIngot)));
                    Add(new GenericBuyInfo(typeof(CopperIngot)));
                    Add(new GenericBuyInfo(typeof(BronzeIngot)));
                    Add(new GenericBuyInfo(typeof(GoldIngot)));
                    Add(new GenericBuyInfo(typeof(AgapiteIngot)));
                    Add(new GenericBuyInfo(typeof(VeriteIngot)));
                    Add(new GenericBuyInfo(typeof(ValoriteIngot)));
#endif
                }
                else if (Core.RuleSets.ShopkeepersSellResourcesRules())
                    Add(new GenericBuyInfo(typeof(IronIngot), BaseVendor.PlayerPays(typeof(IronIngot)), 16, 0x1BF2, 0));

                if (Core.RuleSets.AngelIslandRules())
                    Add(new GenericBuyInfo(typeof(MiniBossArmorBlessDeed), BaseVendor.PlayerPays(typeof(MiniBossArmorBlessDeed)), 20, 0x14F0, 0));

                Add(new GenericBuyInfo(typeof(Tongs), BaseVendor.PlayerPays(typeof(Tongs)), 14, 0xFBB, 0));

                Add(new GenericBuyInfo(typeof(BronzeShield), BaseVendor.PlayerPays(typeof(BronzeShield)), 20, 0x1B72, 0));
                Add(new GenericBuyInfo(typeof(Buckler), BaseVendor.PlayerPays(typeof(Buckler)), 20, 0x1B73, 0));
                Add(new GenericBuyInfo(typeof(MetalKiteShield), BaseVendor.PlayerPays(typeof(MetalKiteShield)), 20, 0x1B74, 0));
                Add(new GenericBuyInfo(typeof(HeaterShield), BaseVendor.PlayerPays(typeof(HeaterShield)), 20, 0x1B76, 0));
                Add(new GenericBuyInfo(typeof(WoodenKiteShield), BaseVendor.PlayerPays(typeof(WoodenKiteShield)), 20, 0x1B78, 0));
                Add(new GenericBuyInfo(typeof(MetalShield), BaseVendor.PlayerPays(typeof(MetalShield)), 20, 0x1B7B, 0));

                Add(new GenericBuyInfo(typeof(WoodenShield), BaseVendor.PlayerPays(typeof(WoodenShield)), 20, 0x1B7A, 0));

                Add(new GenericBuyInfo(typeof(PlateGorget), BaseVendor.PlayerPays(typeof(PlateGorget)), 20, 0x1413, 0));
                Add(new GenericBuyInfo(typeof(PlateChest), BaseVendor.PlayerPays(typeof(PlateChest)), 20, 0x1415, 0));
                Add(new GenericBuyInfo(typeof(PlateLegs), BaseVendor.PlayerPays(typeof(PlateLegs)), 20, 0x1411, 0));
                Add(new GenericBuyInfo(typeof(PlateArms), BaseVendor.PlayerPays(typeof(PlateArms)), 20, 0x1410, 0));
                Add(new GenericBuyInfo(typeof(PlateGloves), BaseVendor.PlayerPays(typeof(PlateGloves)), 20, 0x1414, 0));

                Add(new GenericBuyInfo(typeof(PlateHelm), BaseVendor.PlayerPays(typeof(PlateHelm)), 20, 0x1412, 0));
                Add(new GenericBuyInfo(typeof(CloseHelm), BaseVendor.PlayerPays(typeof(CloseHelm)), 20, 0x1408, 0));
                Add(new GenericBuyInfo(typeof(CloseHelm), BaseVendor.PlayerPays(typeof(CloseHelm)), 20, 0x1409, 0));
                Add(new GenericBuyInfo(typeof(Helmet), BaseVendor.PlayerPays(typeof(Helmet)), 20, 0x140A, 0));
                Add(new GenericBuyInfo(typeof(Helmet), BaseVendor.PlayerPays(typeof(Helmet)), 20, 0x140B, 0));
                Add(new GenericBuyInfo(typeof(NorseHelm), BaseVendor.PlayerPays(typeof(NorseHelm)), 20, 0x140E, 0));
                Add(new GenericBuyInfo(typeof(NorseHelm), BaseVendor.PlayerPays(typeof(NorseHelm)), 20, 0x140F, 0));
                Add(new GenericBuyInfo(typeof(Bascinet), BaseVendor.PlayerPays(typeof(Bascinet)), 20, 0x140C, 0));
                Add(new GenericBuyInfo(typeof(PlateHelm), BaseVendor.PlayerPays(typeof(PlateHelm)), 20, 0x1419, 0));

                Add(new GenericBuyInfo(typeof(ChainCoif), BaseVendor.PlayerPays(typeof(ChainCoif)), 20, 0x13BB, 0));
                Add(new GenericBuyInfo(typeof(ChainChest), BaseVendor.PlayerPays(typeof(ChainChest)), 20, 0x13BF, 0));
                Add(new GenericBuyInfo(typeof(ChainLegs), BaseVendor.PlayerPays(typeof(ChainLegs)), 20, 0x13BE, 0));

                Add(new GenericBuyInfo(typeof(RingmailChest), BaseVendor.PlayerPays(typeof(RingmailChest)), 20, 0x13ec, 0));
                Add(new GenericBuyInfo(typeof(RingmailLegs), BaseVendor.PlayerPays(typeof(RingmailLegs)), 20, 0x13F0, 0));
                Add(new GenericBuyInfo(typeof(RingmailArms), BaseVendor.PlayerPays(typeof(RingmailArms)), 20, 0x13EE, 0));
                Add(new GenericBuyInfo(typeof(RingmailGloves), BaseVendor.PlayerPays(typeof(RingmailGloves)), 20, 0x13eb, 0));

                Add(new GenericBuyInfo(typeof(ExecutionersAxe), BaseVendor.PlayerPays(typeof(ExecutionersAxe)), 20, 0xF45, 0));
                Add(new GenericBuyInfo(typeof(Bardiche), BaseVendor.PlayerPays(typeof(Bardiche)), 20, 0xF4D, 0));
                Add(new GenericBuyInfo(typeof(BattleAxe), BaseVendor.PlayerPays(typeof(BattleAxe)), 20, 0xF47, 0));
                Add(new GenericBuyInfo(typeof(TwoHandedAxe), BaseVendor.PlayerPays(typeof(TwoHandedAxe)), 20, 0x1443, 0));
                Add(new GenericBuyInfo(typeof(Bow), BaseVendor.PlayerPays(typeof(Bow)), 20, 0x13B2, 0));
                Add(new GenericBuyInfo(typeof(ButcherKnife), BaseVendor.PlayerPays(typeof(ButcherKnife)), 20, 0x13F6, 0));
                Add(new GenericBuyInfo(typeof(Crossbow), BaseVendor.PlayerPays(typeof(Crossbow)), 20, 0xF50, 0));
                Add(new GenericBuyInfo(typeof(HeavyCrossbow), BaseVendor.PlayerPays(typeof(HeavyCrossbow)), 20, 0x13FD, 0));
                Add(new GenericBuyInfo(typeof(Cutlass), BaseVendor.PlayerPays(typeof(Cutlass)), 20, 0x1441, 0));
                Add(new GenericBuyInfo(typeof(Dagger), BaseVendor.PlayerPays(typeof(Dagger)), 20, 0xF52, 0));
                Add(new GenericBuyInfo(typeof(Halberd), BaseVendor.PlayerPays(typeof(Halberd)), 20, 0x143E, 0));
                Add(new GenericBuyInfo(typeof(HammerPick), BaseVendor.PlayerPays(typeof(HammerPick)), 20, 0x143D, 0));
                Add(new GenericBuyInfo(typeof(Katana), BaseVendor.PlayerPays(typeof(Katana)), 20, 0x13FF, 0));
                Add(new GenericBuyInfo(typeof(Kryss), BaseVendor.PlayerPays(typeof(Kryss)), 20, 0x1401, 0));
                Add(new GenericBuyInfo(typeof(Broadsword), BaseVendor.PlayerPays(typeof(Broadsword)), 20, 0xF5E, 0));
                Add(new GenericBuyInfo(typeof(Longsword), BaseVendor.PlayerPays(typeof(Longsword)), 20, 0xF61, 0));
                Add(new GenericBuyInfo(typeof(ThinLongsword), BaseVendor.PlayerPays(typeof(ThinLongsword)), 20, 0x13B8, 0));
                Add(new GenericBuyInfo(typeof(VikingSword), BaseVendor.PlayerPays(typeof(VikingSword)), 20, 0x13B9, 0));
                Add(new GenericBuyInfo(typeof(Cleaver), BaseVendor.PlayerPays(typeof(Cleaver)), 20, 0xEC3, 0));
                Add(new GenericBuyInfo(typeof(Axe), BaseVendor.PlayerPays(typeof(Axe)), 20, 0xF49, 0));
                Add(new GenericBuyInfo(typeof(DoubleAxe), BaseVendor.PlayerPays(typeof(DoubleAxe)), 20, 0xF4B, 0));
                Add(new GenericBuyInfo(typeof(Pickaxe), BaseVendor.PlayerPays(typeof(Pickaxe)), 20, 0xE86, 0));
                Add(new GenericBuyInfo(typeof(Pitchfork), BaseVendor.PlayerPays(typeof(Pitchfork)), 20, 0xE87, 0));
                Add(new GenericBuyInfo(typeof(Scimitar), BaseVendor.PlayerPays(typeof(Scimitar)), 20, 0x13B6, 0));
                Add(new GenericBuyInfo(typeof(SkinningKnife), BaseVendor.PlayerPays(typeof(SkinningKnife)), 20, 0xEC4, 0));
                Add(new GenericBuyInfo(typeof(LargeBattleAxe), BaseVendor.PlayerPays(typeof(LargeBattleAxe)), 20, 0x13FB, 0));
                Add(new GenericBuyInfo(typeof(WarAxe), BaseVendor.PlayerPays(typeof(WarAxe)), 20, 0x13B0, 0));

                if (Core.RuleSets.AOSRules())
                {
                    Add(new GenericBuyInfo(typeof(BoneHarvester), BaseVendor.PlayerPays(typeof(BoneHarvester)), 20, 0x26BB, 0));
                    Add(new GenericBuyInfo(typeof(CrescentBlade), BaseVendor.PlayerPays(typeof(CrescentBlade)), 20, 0x26C1, 0));
                    Add(new GenericBuyInfo(typeof(DoubleBladedStaff), BaseVendor.PlayerPays(typeof(DoubleBladedStaff)), 20, 0x26BF, 0));
                    Add(new GenericBuyInfo(typeof(Lance), BaseVendor.PlayerPays(typeof(Lance)), 20, 0x26C0, 0));
                    Add(new GenericBuyInfo(typeof(Pike), BaseVendor.PlayerPays(typeof(Pike)), 20, 0x26BE, 0));
                    Add(new GenericBuyInfo(typeof(Scythe), BaseVendor.PlayerPays(typeof(Scythe)), 20, 0x26BA, 0));
                    Add(new GenericBuyInfo(typeof(CompositeBow), BaseVendor.PlayerPays(typeof(CompositeBow)), 20, 0x26C2, 0));
                    Add(new GenericBuyInfo(typeof(RepeatingCrossbow), BaseVendor.PlayerPays(typeof(RepeatingCrossbow)), 20, 0x26C3, 0));
                }

                Add(new GenericBuyInfo(typeof(BlackStaff), BaseVendor.PlayerPays(typeof(BlackStaff)), 20, 0xDF1, 0));
                Add(new GenericBuyInfo(typeof(Club), BaseVendor.PlayerPays(typeof(Club)), 20, 0x13B4, 0));
                Add(new GenericBuyInfo(typeof(GnarledStaff), BaseVendor.PlayerPays(typeof(GnarledStaff)), 20, 0x13F8, 0));
                Add(new GenericBuyInfo(typeof(Mace), BaseVendor.PlayerPays(typeof(Mace)), 20, 0xF5C, 0));
                Add(new GenericBuyInfo(typeof(Maul), BaseVendor.PlayerPays(typeof(Maul)), 20, 0x143B, 0));
                Add(new GenericBuyInfo(typeof(QuarterStaff), BaseVendor.PlayerPays(typeof(QuarterStaff)), 20, 0xE89, 0));
                Add(new GenericBuyInfo(typeof(ShepherdsCrook), BaseVendor.PlayerPays(typeof(ShepherdsCrook)), 20, 0xE81, 0));
                Add(new GenericBuyInfo(typeof(SmithHammer), BaseVendor.PlayerPays(typeof(SmithHammer)), 20, 0x13E3, 0));
                Add(new GenericBuyInfo(typeof(ShortSpear), BaseVendor.PlayerPays(typeof(ShortSpear)), 20, 0x1403, 0));
                Add(new GenericBuyInfo(typeof(Spear), BaseVendor.PlayerPays(typeof(Spear)), 20, 0xF62, 0));
                Add(new GenericBuyInfo(typeof(WarHammer), BaseVendor.PlayerPays(typeof(WarHammer)), 20, 0x1439, 0));
                Add(new GenericBuyInfo(typeof(WarMace), BaseVendor.PlayerPays(typeof(WarMace)), 20, 0x1407, 0));

                if (Core.RuleSets.AOSRules())
                {
                    Add(new GenericBuyInfo(typeof(Scepter), BaseVendor.PlayerPays(typeof(Scepter)), 20, 0x26BC, 0));
                    Add(new GenericBuyInfo(typeof(BladedStaff), BaseVendor.PlayerPays(typeof(BladedStaff)), 20, 0x26BD, 0));
                }
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                if (Core.RuleSets.ResourcePoolRules())
                {   // balanced buyback system
                    foreach (Type type in BBSTypes)
                        AddToResourcePool(type);
#if false
                    AddToResourcePool(typeof(IronIngot));
                    AddToResourcePool(typeof(DullCopperIngot));
                    AddToResourcePool(typeof(ShadowIronIngot));
                    AddToResourcePool(typeof(CopperIngot));
                    AddToResourcePool(typeof(BronzeIngot));
                    AddToResourcePool(typeof(GoldIngot));
                    AddToResourcePool(typeof(AgapiteIngot));
                    AddToResourcePool(typeof(VeriteIngot));
                    AddToResourcePool(typeof(ValoriteIngot));
                    AddToResourcePool(typeof(IronOre));
                    AddToResourcePool(typeof(DullCopperOre));
                    AddToResourcePool(typeof(ShadowIronOre));
                    AddToResourcePool(typeof(CopperOre));
                    AddToResourcePool(typeof(BronzeOre));
                    AddToResourcePool(typeof(GoldOre));
                    AddToResourcePool(typeof(AgapiteOre));
                    AddToResourcePool(typeof(VeriteOre));
                    AddToResourcePool(typeof(ValoriteOre));
#endif
                }
                else if (Core.RuleSets.ShopkeepersBuyResourcesRules())
                {   // cash buyback
                    Add(typeof(IronIngot), BaseVendor.VendorPays(typeof(IronIngot)));
                }

                if (Core.RuleSets.ShopkeepersBuyItemsRules())
                {   // cash buyback
                    Add(typeof(Tongs), BaseVendor.VendorPays(typeof(Tongs)));
                    Add(typeof(Buckler), BaseVendor.VendorPays(typeof(Buckler)));
                    Add(typeof(BronzeShield), BaseVendor.VendorPays(typeof(BronzeShield)));
                    Add(typeof(MetalShield), BaseVendor.VendorPays(typeof(MetalShield)));
                    Add(typeof(MetalKiteShield), BaseVendor.VendorPays(typeof(MetalKiteShield)));
                    Add(typeof(HeaterShield), BaseVendor.VendorPays(typeof(HeaterShield)));
                    Add(typeof(WoodenKiteShield), BaseVendor.VendorPays(typeof(WoodenKiteShield)));

                    Add(typeof(WoodenShield), BaseVendor.VendorPays(typeof(WoodenShield)));

                    Add(typeof(PlateArms), BaseVendor.VendorPays(typeof(PlateArms)));
                    Add(typeof(PlateChest), BaseVendor.VendorPays(typeof(PlateChest)));
                    Add(typeof(PlateGloves), BaseVendor.VendorPays(typeof(PlateGloves)));
                    Add(typeof(PlateGorget), BaseVendor.VendorPays(typeof(PlateGorget)));
                    Add(typeof(PlateLegs), BaseVendor.VendorPays(typeof(PlateLegs)));

                    Add(typeof(FemalePlateChest), BaseVendor.VendorPays(typeof(FemalePlateChest)));
                    Add(typeof(FemaleLeatherChest), BaseVendor.VendorPays(typeof(FemaleLeatherChest)));
                    Add(typeof(FemaleStuddedChest), BaseVendor.VendorPays(typeof(FemaleStuddedChest)));
                    Add(typeof(LeatherShorts), BaseVendor.VendorPays(typeof(LeatherShorts)));
                    Add(typeof(LeatherSkirt), BaseVendor.VendorPays(typeof(LeatherSkirt)));
                    Add(typeof(LeatherBustierArms), BaseVendor.VendorPays(typeof(LeatherBustierArms)));
                    Add(typeof(StuddedBustierArms), BaseVendor.VendorPays(typeof(StuddedBustierArms)));

                    Add(typeof(Bascinet), BaseVendor.VendorPays(typeof(Bascinet)));
                    Add(typeof(CloseHelm), BaseVendor.VendorPays(typeof(CloseHelm)));
                    Add(typeof(Helmet), BaseVendor.VendorPays(typeof(Helmet)));
                    Add(typeof(NorseHelm), BaseVendor.VendorPays(typeof(NorseHelm)));
                    Add(typeof(PlateHelm), BaseVendor.VendorPays(typeof(PlateHelm)));

                    Add(typeof(ChainCoif), BaseVendor.VendorPays(typeof(ChainCoif)));
                    Add(typeof(ChainChest), BaseVendor.VendorPays(typeof(ChainChest)));
                    Add(typeof(ChainLegs), BaseVendor.VendorPays(typeof(ChainLegs)));

                    Add(typeof(RingmailArms), BaseVendor.VendorPays(typeof(RingmailArms)));
                    Add(typeof(RingmailChest), BaseVendor.VendorPays(typeof(RingmailChest)));
                    Add(typeof(RingmailGloves), BaseVendor.VendorPays(typeof(RingmailGloves)));
                    Add(typeof(RingmailLegs), BaseVendor.VendorPays(typeof(RingmailLegs)));

                    Add(typeof(BattleAxe), BaseVendor.VendorPays(typeof(BattleAxe)));
                    Add(typeof(DoubleAxe), BaseVendor.VendorPays(typeof(DoubleAxe)));
                    Add(typeof(ExecutionersAxe), BaseVendor.VendorPays(typeof(ExecutionersAxe)));
                    Add(typeof(LargeBattleAxe), BaseVendor.VendorPays(typeof(LargeBattleAxe)));
                    Add(typeof(Pickaxe), BaseVendor.VendorPays(typeof(Pickaxe)));
                    Add(typeof(TwoHandedAxe), BaseVendor.VendorPays(typeof(TwoHandedAxe)));
                    Add(typeof(WarAxe), BaseVendor.VendorPays(typeof(WarAxe)));
                    Add(typeof(Axe), BaseVendor.VendorPays(typeof(Axe)));

                    Add(typeof(Bardiche), BaseVendor.VendorPays(typeof(Bardiche)));
                    Add(typeof(Halberd), BaseVendor.VendorPays(typeof(Halberd)));

                    Add(typeof(ButcherKnife), BaseVendor.VendorPays(typeof(ButcherKnife)));
                    Add(typeof(Cleaver), BaseVendor.VendorPays(typeof(Cleaver)));
                    Add(typeof(Dagger), BaseVendor.VendorPays(typeof(Dagger)));
                    Add(typeof(SkinningKnife), BaseVendor.VendorPays(typeof(SkinningKnife)));

                    Add(typeof(Club), BaseVendor.VendorPays(typeof(Club)));
                    Add(typeof(HammerPick), BaseVendor.VendorPays(typeof(HammerPick)));
                    Add(typeof(Mace), BaseVendor.VendorPays(typeof(Mace)));
                    Add(typeof(Maul), BaseVendor.VendorPays(typeof(Maul)));
                    Add(typeof(WarHammer), BaseVendor.VendorPays(typeof(WarHammer)));
                    Add(typeof(WarMace), BaseVendor.VendorPays(typeof(WarMace)));

                    Add(typeof(HeavyCrossbow), BaseVendor.VendorPays(typeof(HeavyCrossbow)));
                    Add(typeof(Bow), BaseVendor.VendorPays(typeof(Bow)));
                    Add(typeof(Crossbow), BaseVendor.VendorPays(typeof(Crossbow)));

                    if (Core.RuleSets.AOSRules())
                    {
                        Add(typeof(CompositeBow), BaseVendor.VendorPays(typeof(CompositeBow)));
                        Add(typeof(RepeatingCrossbow), BaseVendor.VendorPays(typeof(RepeatingCrossbow)));
                        Add(typeof(Scepter), BaseVendor.VendorPays(typeof(Scepter)));
                        Add(typeof(BladedStaff), BaseVendor.VendorPays(typeof(BladedStaff)));
                        Add(typeof(Scythe), BaseVendor.VendorPays(typeof(Scythe)));
                        Add(typeof(BoneHarvester), BaseVendor.VendorPays(typeof(BoneHarvester)));
                        Add(typeof(Scepter), BaseVendor.VendorPays(typeof(Scepter)));
                        Add(typeof(BladedStaff), BaseVendor.VendorPays(typeof(BladedStaff)));
                        Add(typeof(Pike), BaseVendor.VendorPays(typeof(Pike)));
                        Add(typeof(DoubleBladedStaff), BaseVendor.VendorPays(typeof(DoubleBladedStaff)));
                        Add(typeof(Lance), BaseVendor.VendorPays(typeof(Lance)));
                        Add(typeof(CrescentBlade), BaseVendor.VendorPays(typeof(CrescentBlade)));
                    }

                    Add(typeof(Spear), BaseVendor.VendorPays(typeof(Spear)));
                    Add(typeof(Pitchfork), BaseVendor.VendorPays(typeof(Pitchfork)));
                    Add(typeof(ShortSpear), BaseVendor.VendorPays(typeof(ShortSpear)));

                    Add(typeof(BlackStaff), BaseVendor.VendorPays(typeof(BlackStaff)));
                    Add(typeof(GnarledStaff), BaseVendor.VendorPays(typeof(GnarledStaff)));
                    Add(typeof(QuarterStaff), BaseVendor.VendorPays(typeof(QuarterStaff)));
                    Add(typeof(ShepherdsCrook), BaseVendor.VendorPays(typeof(ShepherdsCrook)));

                    Add(typeof(SmithHammer), BaseVendor.VendorPays(typeof(SmithHammer)));

                    Add(typeof(Broadsword), BaseVendor.VendorPays(typeof(Broadsword)));
                    Add(typeof(Cutlass), BaseVendor.VendorPays(typeof(Cutlass)));
                    Add(typeof(Katana), BaseVendor.VendorPays(typeof(Katana)));
                    Add(typeof(Kryss), BaseVendor.VendorPays(typeof(Kryss)));
                    Add(typeof(Longsword), BaseVendor.VendorPays(typeof(Longsword)));
                    Add(typeof(Scimitar), BaseVendor.VendorPays(typeof(Scimitar)));
                    Add(typeof(ThinLongsword), BaseVendor.VendorPays(typeof(ThinLongsword)));
                    Add(typeof(VikingSword), BaseVendor.VendorPays(typeof(VikingSword)));
                }
            }
        }
    }
}