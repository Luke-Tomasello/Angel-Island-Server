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

/* Scripts/Engines/Crafting/DefAlchemy.cs
 * ChangeLog:
 *  5/4/23, Yoar
 *      Conditioned dye crafts for AI/MO
 *  4/1/23, Yoar
 *      Set skill requirement of RefreshPotion to [15.0, 65.0], according to uostratics
 *  10/12/21, Yoar
 *      Changed return type of CraftSystem.PlayEndingEffect to "object" to support strings.
 *      The fail message for dye crafts is now displayed in the craft gump.
 *  9/20/21, Yoar
 *      Added 4 more craftable special dyes: Red, Blue, Green and Yellow.
 *      These colors normally come with RunUO's leather/runebook dye tubs.
 *  9/19/21, Yoar
 *      Rewrote special dye tub crafting.
 *      Dye crafts no longer give skill gains!
 *	10/15/05, erlein
 *		Re-worked special dye handling to accommodate new dye tub based craft model.
 *	10/15/05, erlein
 *		Added special dyes.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */


using Server.Items;
using Server.Targeting;
using System;

namespace Server.Engines.Craft
{
    public class DefAlchemy : CraftSystem
    {
        public override SkillName MainSkill
        {
            get { return SkillName.Alchemy; }
        }

        public override TextDefinition GumpTitle
        {
            get { return 1044001; } // <CENTER>ALCHEMY MENU</CENTER>
        }

        private static CraftSystem m_CraftSystem;

        public static CraftSystem CraftSystem
        {
            get
            {
                if (m_CraftSystem == null)
                    m_CraftSystem = new DefAlchemy();

                return m_CraftSystem;
            }
        }

        public override double GetChanceAtMin(CraftItem item)
        {
            return 0.0; // 0%
        }

        private DefAlchemy()
            : base(1, 1, 1.25)// base( 1, 1, 3.1 )
        {
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
            from.PlaySound(0x242);
        }

        public override TextDefinition PlayEndingEffect(Mobile from, bool failed, bool lostMaterial, bool toolBroken, int quality, bool makersMark, CraftItem item)
        {
            if (toolBroken)
                from.SendLocalizedMessage(1044038); // You have worn out your tool

            if (item.ItemType == typeof(SpecialDyeCraft) || item.ItemType == typeof(ToneCraft))
            {
                if (failed)
                    return "You fail to mix the dye correctly.";
                else
                    return null; // success messages are sent in the CustomCraft objects
            }

            if (failed)
            {
                from.AddToBackpack(new Bottle());

                return 500287; // You fail to create a useful potion.
            }
            else
            {
                from.PlaySound(0x240); // Sound of a filling bottle

                if (quality == -1)
                    return 1048136; // You create the potion and pour it into a keg.
                else
                    return 500279; // You pour the potion into a bottle...
            }
        }

        public override bool DoesCraftEffect(CraftItem item)
        {
            if (item.ItemType == typeof(SpecialDyeCraft) || item.ItemType == typeof(ToneCraft))
                return false; // no craft effect for dye crafts

            return true;
        }

        public override bool DoesSkillGain(CraftItem item)
        {
            if (item.ItemType == typeof(SpecialDyeCraft) || item.ItemType == typeof(ToneCraft))
                return false; // no skill gain for dye crafts

            return true;
        }

        public override void InitCraftList()
        {
            int index = -1;

            // Refresh Potion
            index = AddCraft(typeof(RefreshPotion), 1044530, 1044538, 15.0, 65.0, typeof(BlackPearl), 1044353, 1, 1044361);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(TotalRefreshPotion), 1044530, 1044539, 25.0, 75.0, typeof(BlackPearl), 1044353, 5, 1044361);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Agility Potion
            index = AddCraft(typeof(AgilityPotion), 1044531, 1044540, 15.0, 65.0, typeof(Bloodmoss), 1044354, 1, 1044362);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterAgilityPotion), 1044531, 1044541, 35.0, 85.0, typeof(Bloodmoss), 1044354, 3, 1044362);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Nightsight Potion
            index = AddCraft(typeof(NightSightPotion), 1044532, 1044542, -25.0, 25.0, typeof(SpidersSilk), 1044360, 1, 1044368);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Heal Potion
            index = AddCraft(typeof(LesserHealPotion), 1044533, 1044543, -25.0, 25.0, typeof(Ginseng), 1044356, 1, 1044364);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(HealPotion), 1044533, 1044544, 15.0, 65.0, typeof(Ginseng), 1044356, 3, 1044364);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterHealPotion), 1044533, 1044545, 55.0, 105.0, typeof(Ginseng), 1044356, 7, 1044364);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Strength Potion
            index = AddCraft(typeof(StrengthPotion), 1044534, 1044546, 25.0, 75.0, typeof(MandrakeRoot), 1044357, 2, 1044365);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterStrengthPotion), 1044534, 1044547, 45.0, 95.0, typeof(MandrakeRoot), 1044357, 5, 1044365);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Poison Potion
            index = AddCraft(typeof(LesserPoisonPotion), 1044535, 1044548, -5.0, 45.0, typeof(Nightshade), 1044358, 1, 1044366);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(PoisonPotion), 1044535, 1044549, 15.0, 65.0, typeof(Nightshade), 1044358, 2, 1044366);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterPoisonPotion), 1044535, 1044550, 55.0, 105.0, typeof(Nightshade), 1044358, 4, 1044366);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(DeadlyPoisonPotion), 1044535, 1044551, 90.0, 140.0, typeof(Nightshade), 1044358, 8, 1044366);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Cure Potion
            index = AddCraft(typeof(LesserCurePotion), 1044536, 1044552, -10.0, 40.0, typeof(Garlic), 1044355, 1, 1044363);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(CurePotion), 1044536, 1044553, 25.0, 75.0, typeof(Garlic), 1044355, 3, 1044363);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterCurePotion), 1044536, 1044554, 65.0, 115.0, typeof(Garlic), 1044355, 6, 1044363);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            // Explosion Potion
            index = AddCraft(typeof(LesserExplosionPotion), 1044537, 1044555, 5.0, 55.0, typeof(SulfurousAsh), 1044359, 3, 1044367);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(ExplosionPotion), 1044537, 1044556, 35.0, 85.0, typeof(SulfurousAsh), 1044359, 5, 1044367);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);
            index = AddCraft(typeof(GreaterExplosionPotion), 1044537, 1044557, 65.0, 115.0, typeof(SulfurousAsh), 1044359, 10, 1044367);
            AddRes(index, typeof(Bottle), 1044529, 1, 500315);

            if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
            {
                // erl: special dyes!
                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Violet", 80.0, 113.33, typeof(SulfurousAsh), "Sulfurous Ash", 10, 1044367, 1230);
                AddRes(index, typeof(BlackPearl), "Black Pearl", 10, 1044361);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Tan", 80.0, 113.33, typeof(Ginseng), "Ginseng", 20, 1044364, 1501);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Brown", 80.0, 113.33, typeof(MandrakeRoot), "Mandrake Root", 20, 2013);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Dark Blue", 80.0, 113.33, typeof(BlackPearl), "Black Pearl", 20, 1044361, 1303);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Forest Green", 80.0, 113.33, typeof(BlackPearl), "Black Pearl", 10, 1044361, 1420);
                AddRes(index, typeof(Nightshade), "Nightshade", 10, 1044366);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Pink", 80.0, 113.33, typeof(Bloodmoss), "Blood Moss", 10, 1044362, 1619);
                AddRes(index, typeof(Garlic), "Garlic", 10, 1044363);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Crimson", 80.0, 113.33, typeof(Bloodmoss), "Blood Moss", 20, 1044362, 1640); // yoar: renamed from "Red" to "Crimson"
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Olive", 80.0, 113.33, typeof(Garlic), "Garlic", 10, 1044363, 2001);
                AddRes(index, typeof(Nightshade), "Nightshade", 10, 1044366);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                // yoar: added 4 more colors, from the RunUO leather dye tub
                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Red", 80.0, 113.33, typeof(Ruby), "Ruby", 20, 1044253, 2113);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Blue", 80.0, 113.33, typeof(Sapphire), "Sapphire", 20, 1044253, 2119);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Green", 80.0, 113.33, typeof(Emerald), "Emerald", 20, 1044253, 2126);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(SpecialDyeCraft), "Mix Dye", "Yellow", 80.0, 113.33, typeof(Amber), "Amber", 20, 1044253, 2213);
                SetNeedWater(index, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                /* Yoar
                 * 
                 * The crafts below "tone" (a.k.a lighten/darken) the color of special dye tubs.
                 * 
                 * Remember, special dye tubs can have more uses/charges.
                 * 
                 * In the old system, tone craft would consume more resources if the targeted
                 * dye tub had more charges. However, it didn't actually require you to have
                 * enough resources in your pack to tone all the charges! So, you could tone
                 * a dye tub of 100 charges with just 2 reagents.
                 * 
                 * Let's ignore this complication... Now, every tone craft, regardless of the
                 * number of charges on the targeted tub, requires just 2 reagents.
                 */
                index = AddCraft(typeof(ToneCraft), "Mix Dye", "> Lighten the mix", 80.0, 100.00, typeof(SulfurousAsh), "Sulfurous Ash", 2, 1044367, false);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);

                index = AddCraft(typeof(ToneCraft), "Mix Dye", "> Darken the mix", 80.0, 100.00, typeof(BlackPearl), "Black Pearl", 2, 1044361, true);
                AddSkill(index, SkillName.Tailoring, 80.0, 113.33);
            }
        }

        #region Dye Craft

        // based on Server.Engines.Craft.TrapCraft
        [CraftItemIDAttribute(0xFAB)]
        public class SpecialDyeCraft : CustomCraft
        {
            private DyeTub m_Tub;
            private int m_Hue;

            public SpecialDyeCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality, int hue)
                : base(from, craftItem, craftSystem, typeRes, tool, quality)
            {
                m_Hue = hue;
            }

            private TextDefinition Verify(DyeTub tub)
            {
                if (tub != null)
                {
                    Type type = tub.GetType(); // let's restrict this craft to exactly 2 types

                    if (type == typeof(DyeTub))
                    {
                        if (!tub.IsChildOf(From.Backpack))
                            return "The dye tub must be in your pack for you to use it.";
                        else if (tub.DyedHue != 0)
                            return "You cannot mix two different colors.";
                        else
                            return null;
                    }
                    else if (type == typeof(SpecialDyeTub))
                    {
                        if (!tub.IsChildOf(From.Backpack))
                            return "The dye tub must be in your pack for you to use it.";
                        else if (tub.DyedHue != m_Hue)
                            return "You cannot mix two different colors.";
                        else if (((SpecialDyeTub)tub).UsesRemaining >= 100)
                            return "You cannot pour any more dye into that tub.";
                        else
                            return null;
                    }
                }

                return "You can only pour this into regular dye tubs or special dye tubs.";
            }

            private bool Acquire(object targeted, out TextDefinition message)
            {
                DyeTub tub = targeted as DyeTub;

                message = Verify(tub);

                if (message != null)
                {
                    return false;
                }
                else
                {
                    m_Tub = tub;
                    return true;
                }
            }

            public override void EndCraftAction()
            {
                DyeTub found = null;

                if (From.Backpack != null)
                {
                    foreach (Item item in From.Backpack.Items)
                    {
                        if (item is DyeTub)
                        {
                            DyeTub tub = (DyeTub)item;

                            bool ok = false;

                            Type type = tub.GetType(); // let's restrict this craft to exactly 2 types

                            if (type == typeof(DyeTub))
                                ok = (tub.DyedHue == 0);
                            else if (type == typeof(SpecialDyeTub))
                                ok = (tub.DyedHue == m_Hue && ((SpecialDyeTub)tub).UsesRemaining < 100);

                            if (ok)
                            {
                                // if we find 2 valid items, give the player a target instead
                                if (found != null)
                                {
                                    found = null;
                                    break;
                                }

                                found = tub;
                            }
                        }
                    }
                }

                if (found != null)
                {
                    m_Tub = found;
                    CraftItem.CompleteCraft(Quality, false, From, CraftSystem, TypeRes, Tool, this);
                }
                else
                {
                    From.SendMessage("Select the dye tub to pour this dye into.");
                    From.Target = new DyeTubTarget(this);
                }
            }

            private class DyeTubTarget : Target
            {
                private SpecialDyeCraft m_Craft;

                public DyeTubTarget(SpecialDyeCraft craft)
                    : base(-1, false, TargetFlags.None)
                {
                    m_Craft = craft;
                }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    TextDefinition message;

                    if (m_Craft.Acquire(targeted, out message))
                        m_Craft.CraftItem.CompleteCraft(m_Craft.Quality, false, m_Craft.From, m_Craft.CraftSystem, m_Craft.TypeRes, m_Craft.Tool, m_Craft);
                    else
                        Failure(message);
                }

                protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
                {
                    if (cancelType == TargetCancelType.Canceled)
                        Failure(null);
                }

                private void Failure(TextDefinition message)
                {
                    Mobile from = m_Craft.From;
                    BaseTool tool = m_Craft.Tool;

                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                        from.SendGump(new CraftGump(from, m_Craft.CraftSystem, tool, message));
                    else
                        TextDefinition.SendMessageTo(from, message);
                }
            }

            public override Item CompleteCraft(out TextDefinition message)
            {
                message = Verify(m_Tub);

                if (message == null)
                {
                    if (m_Tub is SpecialDyeTub)
                    {
                        ((SpecialDyeTub)m_Tub).UsesRemaining++;

                        message = "You mix the dye and add it to an existing batch.";
                    }
                    else
                    {
                        SpecialDyeTub specialDyeTub = new SpecialDyeTub();

                        specialDyeTub.SetDyedHue(m_Hue);

                        m_Tub.ReplaceWith(specialDyeTub);

                        message = "You successfully mix the dye.";
                    }
                }

                return null;
            }
        }

        // based on Server.Engines.Craft.TrapCraft
        [CraftItemIDAttribute(0xFA9)]
        public class ToneCraft : CustomCraft
        {
            private Item m_Target;
            private bool m_Darken;

            public ToneCraft(Mobile from, CraftItem craftItem, CraftSystem craftSystem, Type typeRes, BaseTool tool, int quality, bool darken)
                : base(from, craftItem, craftSystem, typeRes, tool, quality)
            {
                m_Darken = darken;
            }

            private TextDefinition Verify(Item item)
            {
                ITonable tonable = item as ITonable;

                if (tonable == null || (!tonable.CanDarken && !tonable.CanLighten))
                    return "You cannot tone that.";
                else if (!item.IsChildOf(From.Backpack))
                    return "The dye tub must be in your pack for you to use it.";

                return null;
            }

            private bool Acquire(object targeted, out TextDefinition message)
            {
                Item target = targeted as Item;

                message = Verify(target);

                if (message != null)
                {
                    return false;
                }
                else
                {
                    m_Target = target;
                    return true;
                }
            }

            public override void EndCraftAction()
            {
                Item found = null;

                if (From.Backpack != null)
                {
                    foreach (Item item in From.Backpack.Items)
                    {
                        if (item is ITonable)
                        {
                            ITonable tonable = (ITonable)item;

                            if (tonable.CanDarken || tonable.CanLighten)
                            {
                                // if we find 2 valid items, give the player a target instead
                                if (found != null)
                                {
                                    found = null;
                                    break;
                                }

                                found = item;
                            }
                        }
                    }
                }

                if (found != null)
                {
                    m_Target = found;
                    CraftItem.CompleteCraft(Quality, false, From, CraftSystem, TypeRes, Tool, this);
                }
                else
                {
                    From.SendMessage("Select the dye tub you wish to tone.");
                    From.Target = new DyeTubTarget(this);
                }
            }

            private class DyeTubTarget : Target
            {
                private ToneCraft m_Craft;

                public DyeTubTarget(ToneCraft craft)
                    : base(-1, false, TargetFlags.None)
                {
                    m_Craft = craft;
                }

                protected override void OnTarget(Mobile from, object targeted)
                {
                    TextDefinition message;

                    if (m_Craft.Acquire(targeted, out message))
                        m_Craft.CraftItem.CompleteCraft(m_Craft.Quality, false, m_Craft.From, m_Craft.CraftSystem, m_Craft.TypeRes, m_Craft.Tool, m_Craft);
                    else
                        Failure(message);
                }

                protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
                {
                    if (cancelType == TargetCancelType.Canceled)
                        Failure(null);
                }

                private void Failure(TextDefinition message)
                {
                    Mobile from = m_Craft.From;
                    BaseTool tool = m_Craft.Tool;

                    if (tool != null && !tool.Deleted && tool.UsesRemaining > 0)
                        from.SendGump(new CraftGump(from, m_Craft.CraftSystem, tool, message));
                    else
                        TextDefinition.SendMessageTo(from, message);
                }
            }

            public override Item CompleteCraft(out TextDefinition message)
            {
                message = Verify(m_Target);

                if (message == null)
                {
                    ITonable tonable = (ITonable)m_Target;

                    if (m_Darken)
                    {
                        if (tonable.DarkenMix())
                            message = "You darken the mix with black pearl...";
                        else
                            message = "You attempt to darken the mix, but it will go no darker.";
                    }
                    else
                    {
                        if (tonable.LightenMix())
                            message = "You lighten the mix with sulfurous ash...";
                        else
                            message = "You attempt to lighten the mix, but it will go no lighter.";
                    }
                }

                return null;
            }
        }

        #endregion
    }
}