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

/* Engines/Crafting/DefInscription.cs
 * CHANGELOG:
 *  1/8/22, Yoar
 *      Added blank recall rune resource requirement to runebook craft.
 *      This used to be handled in CraftItem as a special case.
 *  10/28/21, Yoar
 *      BulkOrderBooks are now craftable on AI if the bulk order system is enabled.
 *  10/12/21, Yoar
 *      Changed return type of CraftSystem.PlayEndingEffect to "object" to support strings
 *	8/1/05, Pix
 *		Added magery requirement for making scrolls.
 *	8/1/05, erlein
 *		Added 'MarkOption = true' in initialization of craft list, so engine
 *		draws the button.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Items;
using System;

namespace Server.Engines.Craft
{
    public class DefInscription : CraftSystem
    {
        public override SkillName MainSkill
        {
            get { return SkillName.Inscribe; }
        }

        public override TextDefinition GumpTitle
        {
            get { return 1044009; } // <CENTER>INSCRIPTION MENU</CENTER>
        }

        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DefInscription();

                return m_CraftSystem;
            }
        }

        public override double GetChanceAtMin(CraftItem item)
        {
            return 0.0; // 0%
        }

        private DefInscription()
            : base(1, 1, 1.25)// base( 1, 1, 3.0 )
        {
        }

        public override TextDefinition CanCraft(Mobile from, BaseTool tool, Type typeItem)
        {
            if (tool.Deleted || tool.UsesRemaining < 0)
                return 1044038; // You have worn out your tool!
            else if (!BaseTool.CheckAccessible(tool, from))
                return 1044263; // The tool must be on your person to use.

            if (typeItem != null)
            {
                object o = Activator.CreateInstance(typeItem);

                if (o is SpellScroll)
                {
                    SpellScroll scroll = (SpellScroll)o;
                    Spellbook book = Spellbook.Find(from, scroll.SpellID);

                    bool hasSpell = (book != null && book.HasSpell(scroll.SpellID));

                    scroll.Delete();

                    //return ( hasSpell ? 0 : 1042404 ); // null : You don't have that spell!
                    if (hasSpell)
                    {
                        //GetCastSkills
                        Server.Spells.Spell spell = Server.Spells.SpellRegistry.NewSpell(scroll.SpellID, from, null);
                        if (spell == null)
                        {
                            return 0;
                        }
                        else
                        {
                            double minmagery = 0.0;
                            double maxmagery = 0.0;
                            spell.GetCastSkills(out minmagery, out maxmagery);
                            if (minmagery > from.Skills.Magery.Value)
                            {
                                return 1044153; // You don't have the required skills to attempt this item.
                            }
                            else
                            {
                                return 0;
                            }
                        }
                    }
                    else
                    {
                        return 1042404; // You don't have that spell!
                    }
                }
                else if (o is Item)
                {
                    ((Item)o).Delete();
                }
            }

            return 0;
        }

        public override bool ConsumeAttributeOnFailure(Mobile from)
        {
            // adam: added 5/27/11 to allow consumption of attributes like mana on a failure.
            // this is the case for some early shards like publish 5 Siege
            // Currently we're not restoring the bug "don�t have the proper materials" case
            // http://www.uoguide.com/Publish_15
            // BUG No:	"Players will no longer lose mana when attempting to inscribe a scroll they don�t have the proper materials for."
            // BUG Yes:	"Characters will no longer lose mana when they fail to make a scroll."
            if ((!Core.RuleSets.MortalisRules() && !Core.RuleSets.AngelIslandRules()) && PublishInfo.Publish < 15)
                return true;
            else
                return false;
        }

        public override void PlayCraftEffect(Mobile from, object obj = null)
        {
            from.PlaySound(0x249);
        }

        public override TextDefinition PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken)
                from.SendLocalizedMessage(1044038); // You have worn out your tool

            if (item.Name == 1041267 || item.Name == 1028793) //  not a scroll
            {
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
            else
            {
                if (failed)
                    return 501630; // You fail to inscribe the scroll, and the scroll is ruined.
                else
                    return 501629; // You inscribe the spell and put the scroll in your backpack.
            }
        }

        private int m_Circle, m_Mana;

        private enum Reg { BlackPearl, Bloodmoss, Garlic, Ginseng, MandrakeRoot, Nightshade, SulfurousAsh, SpidersSilk }

        private Type[] m_RegTypes = new Type[]
            {
                typeof( BlackPearl ),
                typeof( Bloodmoss ),
                typeof( Garlic ),
                typeof( Ginseng ),
                typeof( MandrakeRoot ),
                typeof( Nightshade ),
                typeof( SulfurousAsh ),
                typeof( SpidersSilk )
            };

        private int m_Index;

        private void AddSpell(Type type, params Reg[] regs)
        {
            double minSkill, maxSkill;
            double minMagery;

            switch (m_Circle)
            {
                default:
                case 0: minSkill = -25.0; maxSkill = 25.0; break;
                case 1: minSkill = -10.8; maxSkill = 39.2; break;
                case 2: minSkill = 03.5; maxSkill = 53.5; break;
                case 3: minSkill = 17.8; maxSkill = 67.8; break;
                case 4: minSkill = 32.1; maxSkill = 82.1; break;
                case 5: minSkill = 46.4; maxSkill = 96.4; break;
                case 6: minSkill = 60.7; maxSkill = 110.7; break;
                case 7: minSkill = 75.0; maxSkill = 125.0; break;
            }

            switch (m_Circle)
            {
                default:
                case 0: minMagery = 1.1; break;
                case 1: minMagery = 6.1; break;
                case 2: minMagery = 16.1; break;
                case 3: minMagery = 26.1; break;
                case 4: minMagery = 36.1; break;
                case 5: minMagery = 51.8; break;
                case 6: minMagery = 66.1; break;
                case 7: minMagery = 80.1; break;
            }


            int index = AddCraft(type, 1044369 + m_Circle, 1044381 + m_Index++, minSkill, maxSkill, m_RegTypes[(int)regs[0]], 1044353 + (int)regs[0], 1, 1044361 + (int)regs[0]);

            AddSkill(index, SkillName.Magery, minMagery, minMagery); //Pix: Note minMagery as last parameter is correct here, otherwise magery would affect the success rate

            for (int i = 1; i < regs.Length; ++i)
                AddRes(index, m_RegTypes[(int)regs[i]], 1044353 + (int)regs[i], 1, 1044361 + (int)regs[i]);

            AddRes(index, typeof(BlankScroll), 1044377, 1, 1044378);

            SetManaReq(index, m_Mana);
        }

        public override void InitCraftList()
        {
            m_Circle = 0;
            m_Mana = 4;

            AddSpell(typeof(ReactiveArmorScroll), Reg.Garlic, Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(ClumsyScroll), Reg.Bloodmoss, Reg.Nightshade);
            AddSpell(typeof(CreateFoodScroll), Reg.Garlic, Reg.Ginseng, Reg.MandrakeRoot);
            AddSpell(typeof(FeeblemindScroll), Reg.Nightshade, Reg.Ginseng);
            AddSpell(typeof(HealScroll), Reg.Garlic, Reg.Ginseng, Reg.SpidersSilk);
            AddSpell(typeof(MagicArrowScroll), Reg.SulfurousAsh);
            AddSpell(typeof(NightSightScroll), Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(WeakenScroll), Reg.Garlic, Reg.Nightshade);

            m_Circle = 1;
            m_Mana = 6;

            AddSpell(typeof(AgilityScroll), Reg.Bloodmoss, Reg.MandrakeRoot);
            AddSpell(typeof(CunningScroll), Reg.Nightshade, Reg.MandrakeRoot);
            AddSpell(typeof(CureScroll), Reg.Garlic, Reg.Ginseng);
            AddSpell(typeof(HarmScroll), Reg.Nightshade, Reg.SpidersSilk);
            AddSpell(typeof(MagicTrapScroll), Reg.Garlic, Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(MagicUnTrapScroll), Reg.Bloodmoss, Reg.SulfurousAsh);
            AddSpell(typeof(ProtectionScroll), Reg.Garlic, Reg.Ginseng, Reg.SulfurousAsh);
            AddSpell(typeof(StrengthScroll), Reg.Nightshade, Reg.MandrakeRoot);

            m_Circle = 2;
            m_Mana = 9;

            AddSpell(typeof(BlessScroll), Reg.Garlic, Reg.MandrakeRoot);
            AddSpell(typeof(FireballScroll), Reg.BlackPearl);
            AddSpell(typeof(MagicLockScroll), Reg.Bloodmoss, Reg.Garlic, Reg.SulfurousAsh);
            AddSpell(typeof(PoisonScroll), Reg.Nightshade);
            AddSpell(typeof(TelekinisisScroll), Reg.Garlic, Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(TeleportScroll), Reg.Bloodmoss, Reg.MandrakeRoot);
            AddSpell(typeof(UnlockScroll), Reg.Bloodmoss, Reg.SulfurousAsh);
            AddSpell(typeof(WallOfStoneScroll), Reg.Bloodmoss, Reg.Garlic);

            m_Circle = 3;
            m_Mana = 11;

            AddSpell(typeof(ArchCureScroll), Reg.Garlic, Reg.Ginseng, Reg.MandrakeRoot);
            AddSpell(typeof(ArchProtectionScroll), Reg.Garlic, Reg.Ginseng, Reg.MandrakeRoot, Reg.SulfurousAsh);
            AddSpell(typeof(CurseScroll), Reg.Garlic, Reg.Nightshade, Reg.SulfurousAsh);
            AddSpell(typeof(FireFieldScroll), Reg.BlackPearl, Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(GreaterHealScroll), Reg.Garlic, Reg.SpidersSilk, Reg.MandrakeRoot, Reg.Ginseng);
            AddSpell(typeof(LightningScroll), Reg.MandrakeRoot, Reg.SulfurousAsh);
            AddSpell(typeof(ManaDrainScroll), Reg.BlackPearl, Reg.SpidersSilk, Reg.MandrakeRoot);
            AddSpell(typeof(RecallScroll), Reg.BlackPearl, Reg.Bloodmoss, Reg.MandrakeRoot);

            m_Circle = 4;
            m_Mana = 14;

            AddSpell(typeof(BladeSpiritsScroll), Reg.BlackPearl, Reg.Nightshade, Reg.MandrakeRoot);
            AddSpell(typeof(DispelFieldScroll), Reg.BlackPearl, Reg.Garlic, Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(IncognitoScroll), Reg.Bloodmoss, Reg.Garlic, Reg.Nightshade);
            AddSpell(typeof(MagicReflectScroll), Reg.Garlic, Reg.MandrakeRoot, Reg.SpidersSilk);
            AddSpell(typeof(MindBlastScroll), Reg.BlackPearl, Reg.MandrakeRoot, Reg.Nightshade, Reg.SulfurousAsh);
            AddSpell(typeof(ParalyzeScroll), Reg.Garlic, Reg.MandrakeRoot, Reg.SpidersSilk);
            AddSpell(typeof(PoisonFieldScroll), Reg.BlackPearl, Reg.Nightshade, Reg.SpidersSilk);
            AddSpell(typeof(SummonCreatureScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SpidersSilk);

            m_Circle = 5;
            m_Mana = 20;

            AddSpell(typeof(DispelScroll), Reg.Garlic, Reg.MandrakeRoot, Reg.SulfurousAsh);
            AddSpell(typeof(EnergyBoltScroll), Reg.BlackPearl, Reg.Nightshade);
            AddSpell(typeof(ExplosionScroll), Reg.Bloodmoss, Reg.MandrakeRoot);
            AddSpell(typeof(InvisibilityScroll), Reg.Bloodmoss, Reg.Nightshade);
            AddSpell(typeof(MarkScroll), Reg.Bloodmoss, Reg.BlackPearl, Reg.MandrakeRoot);
            AddSpell(typeof(MassCurseScroll), Reg.Garlic, Reg.MandrakeRoot, Reg.Nightshade, Reg.SulfurousAsh);
            AddSpell(typeof(ParalyzeFieldScroll), Reg.BlackPearl, Reg.Ginseng, Reg.SpidersSilk);
            AddSpell(typeof(RevealScroll), Reg.Bloodmoss, Reg.SulfurousAsh);

            m_Circle = 6;
            m_Mana = 40;

            AddSpell(typeof(ChainLightningScroll), Reg.BlackPearl, Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SulfurousAsh);
            AddSpell(typeof(EnergyFieldScroll), Reg.BlackPearl, Reg.MandrakeRoot, Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(FlamestrikeScroll), Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(GateTravelScroll), Reg.BlackPearl, Reg.MandrakeRoot, Reg.SulfurousAsh);
            AddSpell(typeof(ManaVampireScroll), Reg.BlackPearl, Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SpidersSilk);
            AddSpell(typeof(MassDispelScroll), Reg.BlackPearl, Reg.Garlic, Reg.MandrakeRoot, Reg.SulfurousAsh);
            AddSpell(typeof(MeteorSwarmScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SulfurousAsh, Reg.SpidersSilk);
            AddSpell(typeof(PolymorphScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SpidersSilk);

            m_Circle = 7;
            m_Mana = 50;

            AddSpell(typeof(EarthquakeScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.Ginseng, Reg.SulfurousAsh);
            AddSpell(typeof(EnergyVortexScroll), Reg.BlackPearl, Reg.Bloodmoss, Reg.MandrakeRoot, Reg.Nightshade);
            AddSpell(typeof(ResurrectionScroll), Reg.Bloodmoss, Reg.Garlic, Reg.Ginseng);
            AddSpell(typeof(SummonAirElementalScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SpidersSilk);
            AddSpell(typeof(SummonDaemonScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(SummonEarthElementalScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SpidersSilk);
            AddSpell(typeof(SummonFireElementalScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SpidersSilk, Reg.SulfurousAsh);
            AddSpell(typeof(SummonWaterElementalScroll), Reg.Bloodmoss, Reg.MandrakeRoot, Reg.SpidersSilk);

            // Runebook
            // Publish 15
            // Runebooks now only require 8 blank scrolls to make instead of the previous 10.
            int scrolls = (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules() || PublishInfo.Publish >= 15) ? 8 : 10;
            int index = AddCraft(typeof(Runebook), 1044294, 1041267, 45.0, 95.0, typeof(BlankScroll), 1044377, scrolls, 1044378);
            AddRes(index, typeof(RecallScroll), 1044445, 1, 1044253);
            AddRes(index, typeof(GateTravelScroll), 1044446, 1, 1044253);
            AddRes(index, typeof(RecallRune), 1044447, 1, 1044253, item => !((RecallRune)item).Marked);

            if (Core.RuleSets.AOSRules() || BulkOrders.BulkOrderSystem.Enabled)
            {
                // Bulk order book
                AddCraft(typeof(BulkOrders.BulkOrderBook), 1044294, 1028793, 65.0, 115.0, typeof(BlankScroll), 1044377, 10, 1044378);
            }

            MarkOption = true;
        }
    }
}