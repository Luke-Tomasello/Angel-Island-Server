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

/* Scripts/Skills/AnimalLore.cs
 *	ChangeLog :
 *	1/25/24, Yoar
 *	    Added special hireling skill display
 *	1/17/2024, Adam,
 *	    Enable the animal lore gump for hirelings
 *	    * no skill needed (when CanLore virtual property is set)
 *	7/6/23, Yoar
 *	    The combat trait now shows an average of wrestling + anatomy + tactics skills
 *	    The magic trait now shows an average of magery + meditation + eval int skills
 *	    Trait messages are now displayed as a system message
 *	6/25/23, Yoar
 *	    Animal lore messages are now displayed overhead the targeted creature
 *	5/30/23, Yoar
 *	    Rewrote pre-Pub16 animal lore entirely
 *	    Lore-able traits are now defined by "BaseTrait" objects
 *	    Now always displaying 2 random traits, followed by the loyalty trait
 *	12/26/21, Yoar
 *	    Added StrCap, DexCap, IntCap information (1st page).
 *	    Added StatCap information (1st page).
 *	    Moved gender information to the 3th page.
 *	12/17/21, Yoar
 *	    Fixed HTML colors of genetics stuff.
 *	    Added growth stage information.
 *	9/30/21, Adam: replace the Happiness Rating calc with our PetLoyaltyIndex
        there was a disconnect between this calculated value and the value displayed in Command Properties
 *	03/28/07 Taran Kain
 *		Added custom pages section - nonfunctional, needs redesign
 *	12/08/06 Taran Kain
 *		Made the gump closable.
 *	12/07/06 Taran Kain
 *		Added skill and stat locks.
 *		Changed the way the gump handles pages.
 *		Changed "---" to only display when skill == 0.0 (prev showed when skill < 10.0)
 *		Added TC-only code to allow players to see all genes.
 *	11/20/06 Taran Kain
 *		Made the gumps play nice with new loyalty values.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 5 lines removed.
 *  6/6/04 - Old Salty
 *		Altered necessary skill levels to match 100 max skill rather than 120.  I left in a chance to fail at 100.0 
 */

using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.SkillHandlers
{
    public class AnimalLore
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.AnimalLore].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            m.Target = new InternalTarget();

            m.SendLocalizedMessage(500328); // What animal should I look at?

            return TimeSpan.FromSeconds(1.0);
        }

        private class InternalTarget : Target
        {
            public InternalTarget()
                : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.Alive)
                {
                    from.SendLocalizedMessage(500331); // The spirits of the dead are not the province of animal lore.
                }
                else if (targeted is BaseCreature)
                {
                    BaseCreature c = (BaseCreature)targeted;

                    if (!c.IsDeadPet)
                    {
                        if (c.Body.IsAnimal || c.Body.IsMonster || c.Body.IsSea || c.CanLore)
                        {
                            if (!c.CanLore && ((!c.Controlled || !c.Tamable) && from.Skills[SkillName.AnimalLore].Base < 80.0)) //changed to 80 from 100 by Old Salty
                            {
                                from.SendLocalizedMessage(1049674); // At your skill level, you can only lore tamed creatures.
                            }
                            else if (!c.CanLore && (!c.Tamable && from.Skills[SkillName.AnimalLore].Base < 90.0)) //changed to 90 from 110 by Old Salty
                            {
                                from.SendLocalizedMessage(1049675); // At your skill level, you can only lore tamed or tameable creatures.
                            }
                            else if (!c.CanLore && !from.CheckTargetSkill(SkillName.AnimalLore, c, 0.0, 120.0, new object[2] { c, null } /*contextObj*/)) //unchanged by Old Salty to allow failure at GM skill
                            {
                                from.SendLocalizedMessage(500334); // You can't think of anything you know offhand.
                            }
                            else
                            {
                                if (PublishInfo.Publish >= 16 || Core.RuleSets.AngelIslandRules() || CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.BreedingEnabled))
                                {
                                    from.CloseGump(typeof(AnimalLoreGump));
                                    from.SendGump(new AnimalLoreGump(c, from));
                                }
                                else
                                {
                                    DescribeTraits(from, c);
                                }
                            }
                        }
                        else
                        {
                            from.SendLocalizedMessage(500329); // That's not an animal!
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(500331); // The spirits of the dead are not the province of animal lore.
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500329); // That's not an animal!
                }
            }
        }

        public static void DescribeTraits(Mobile from, BaseCreature bc)
        {
            Utility.Shuffle(m_RandomTraits);

            int count = 0;

            for (int i = 0; i < m_RandomTraits.Length && count < 2; i++)
            {
                BaseTrait trait = m_RandomTraits[i];

                if (trait.Validate(bc))
                {
                    DisplayTo(from, bc, trait.Format(bc));
                    count++;
                }
            }

            if (LoyaltyTrait.Instance.Validate(bc))
                DisplayTo(from, bc, LoyaltyTrait.Instance.Format(bc));
        }

        private static void DisplayTo(Mobile m, Mobile target, TextDefinition def)
        {
            /* 7/6/23, Yoar: Arms lore gives system messages. Then so should animal lore.
             * Furthermore, system messages are parsable by Razor. This helps players who
             * are training their pets.
             */
#if false
            if (def.Number > 0)
                target.PrivateOverheadMessage(MessageType.Regular, 0x3B2, def.Number, m.NetState);
            else if (!string.IsNullOrEmpty(def.String))
                target.PrivateOverheadMessage(MessageType.Regular, 0x3B2, false, def.String, m.NetState);
#else
            if (def.Number > 0)
                m.SendLocalizedMessage(def.Number);
            else if (!string.IsNullOrEmpty(def.String))
                m.SendMessage(def.String);
#endif
        }

        private static readonly BaseTrait[] m_RandomTraits = new BaseTrait[] // this array is being shuffled
            {
                // TODO: Hunger trait?
                DietTrait.Instance,
                CombatTrait.Instance,
                MagicTrait.Instance,
                OwnerTrait.Instance,
                UsageTrait.Instance,
            };

        #region Trait Definitions

        private class DietTrait : BaseTrait
        {
            public static readonly DietTrait Instance = new DietTrait();

            private DietTrait()
                : base()
            {
            }

            public override TextDefinition Format(BaseCreature bc)
            {
                List<TextDefinition> choices = new List<TextDefinition>();

                //choices.Add(1042903); // You sense that it likes to eat grass.

                if (bc.FavoriteFood.HasFlag(FoodType.FruitsAndVegies))
                {
                    choices.Add(1042904); // You sense that it would delight in fruit for a meal.
                    choices.Add(1042948); // This creature will eat various crops.
                }

                if (bc.FavoriteFood.HasFlag(FoodType.GrainsAndHay))
                {
                    choices.Add(1042905); // You sense that it likes to eat hay.
                    choices.Add(1042906); // This creature likes to eat grains.
                }

                if (bc.FavoriteFood.HasFlag(FoodType.Meat))
                    choices.Add(1042907); // This creature devours meat for its meals.

                if (bc.FavoriteFood.HasFlag(FoodType.Fish))
                    choices.Add(1042908); // This creature will eat fish.

                if (bc.FavoriteFood.HasFlag(FoodType.Leather))
                    choices.Add(1043328); // Strangely enough, this animal will eat leather.

                if (bc.FavoriteFood.HasFlag(FoodType.Eggs))
                    choices.Add("This creature will eat eggs.");

                if (bc.FavoriteFood.HasFlag(FoodType.Gold))
                    choices.Add("This creature will eat gold.");

                if (choices.Count == 0)
                    return 1042949; // You can't think of anything you could feed it.

                return choices[Utility.Random(choices.Count)];
            }
        }

        private class CombatTrait : BaseTrait
        {
            public static readonly CombatTrait Instance = new CombatTrait();

            private CombatTrait()
                : base()
            {
            }

            public override TextDefinition Format(BaseCreature bc)
            {
                double value = 0.0;
                value += bc.Skills.Wrestling.Base;
                value += bc.Skills.Anatomy.Base;
                value += bc.Skills.Tactics.Base;
                value /= 3.0;

                if (value >= 100.0)
                    return 1042932; // It has mastered the art of war.
                else if (value >= 86.0)
                    return 1042931; // It has nearly mastered the art of war.
                else if (value >= 75.0)
                    return 1042930; // It has learned nearly all there is in the ways of combat.
                else if (value >= 61.0)
                    return 1042929; // It has superior combat training.
                else if (value >= 41.0)
                    return 1042928; // It has excellent combat training.
                else if (value >= 26.0)
                    return 1042927; // It appears fairly trained in the ways of combat.
                else if (value >= 11.0)
                    return 1042926; // It is somewhat trained in the art of war.
                else
                    return 1042925; // It has just begun its combat training.
            }
        }

        private class MagicTrait : BaseTrait
        {
            public static readonly MagicTrait Instance = new MagicTrait();

            private MagicTrait()
                : base()
            {
            }

            public override TextDefinition Format(BaseCreature bc)
            {
                double value = 0.0;
                value += bc.Skills.Magery.Base;
                value += bc.Skills.Meditation.Base;
                value += bc.Skills.EvalInt.Base;
                value /= 3.0;

                if (value >= 100.0)
                    return 1042939; // It has mastered the secrets of magic.
                else if (value >= 86.0)
                    return 1042938; // It has nearly mastered the secrets of magic.
                else if (value >= 75.0)
                    return 1042937; // It has extremely powerful magical abilities.
                else if (value >= 61.0)
                    return 1042936; // It has strong magical abilities.
                else if (value >= 41.0)
                    return 1042935; // It has rather well developed magical abilities.
                else if (value >= 26.0)
                    return 1042934; // It has some magical abilities.
                else if (value >= 11.0)
                    return 1042933; // It has only minor magical abilities.
                else
                    return 1042947; // It lacks any true magical abilities.
            }
        }

        private class OwnerTrait : BaseTrait
        {
            public static readonly OwnerTrait Instance = new OwnerTrait();

            private OwnerTrait()
                : base()
            {
            }

            public override bool Validate(BaseCreature bc)
            {
                return bc.Tamable;
            }

            public override TextDefinition Format(BaseCreature bc)
            {
                int count = bc.Owners.Count;

                if (count >= 6)
                    return 1042945; // It is weary of human companionship.
                else if (count == 5)
                    return 1042944; // It appears infuriated to have known five masters.
                else if (count == 4)
                    return 1042943; // It appears angry to have known four masters.
                else if (count == 3)
                    return 1042942; // It appears annoyed at having known three masters.
                else if (count == 2)
                    return 1042941; // It seems to have known two masters in it's life.
                else if (count == 1)
                    return 1042940; // It appears to have known only one master in its life.
                else
                    return 1042946; // This animal appears to have never been tamed.
            }
        }

        private class UsageTrait : BaseTrait
        {
            public static readonly UsageTrait Instance = new UsageTrait();

            private UsageTrait()
                : base()
            {
            }

            public override TextDefinition Format(BaseCreature bc)
            {
                List<TextDefinition> choices = new List<TextDefinition>();

                if (bc.Meat > 0)
                    choices.Add(1042909); // You could slaughter this creature for meat.

                if (bc.Hides > 0)
                    choices.Add(1042910); // If this creature were dead you could use its hides for leather.

                if (bc is PackHorse || bc is PackLlama || bc is Beetle)
                    choices.Add(1042911); // This creature does well at carrying heavy loads.

                if (bc is Sheep)
                    choices.Add(1042912); // You could use this creature for its wool.

                if (bc is Reaper || bc is Bogling || bc is BogThing || bc is Corpser)
                    choices.Add(1042914); // It is sometimes used for it's wood.

                if (bc.Feathers > 0)
                    choices.Add(1048138); // This creature is sometimes used for its feathers.

                if (choices.Count == 0)
                    return 1042915; // Although you know this creature, you can't seem to think of any uses for it.

                return choices[Utility.Random(choices.Count)];
            }
        }

        private class LoyaltyTrait : BaseTrait
        {
            public static readonly LoyaltyTrait Instance = new LoyaltyTrait();

            private LoyaltyTrait()
                : base()
            {
            }

            public override bool Validate(BaseCreature bc)
            {
                return bc.Controlled;
            }

            public override TextDefinition Format(BaseCreature bc)
            {
                PetLoyalty loyalty = bc.LoyaltyDisplay;

                if (loyalty == PetLoyalty.Confused)
                    return 1043271; // Your pet looks confused
                else if (loyalty == PetLoyalty.ExtremelyUnhappy)
                    return 1043272; // Your pet looks extremely unhappy
                else if (loyalty == PetLoyalty.RatherUnhappy)
                    return 1043273; // Your pet looks rather unhappy
                else if (loyalty == PetLoyalty.Unhappy)
                    return 1043274; // Your pet looks unhappy
                else if (loyalty == PetLoyalty.SomewhatContent)
                    return 1043275; // Your pet looks somewhat content
                else if (loyalty == PetLoyalty.Content)
                    return 1043276; // Your pet looks content
                else if (loyalty == PetLoyalty.Happy)
                    return 1043277; // Your pet looks happy
                else if (loyalty == PetLoyalty.RatherHappy)
                    return 1043278; // Your pet looks rather happy
                else if (loyalty == PetLoyalty.VeryHappy)
                    return 1043279; // Your pet looks very happy
                else if (loyalty == PetLoyalty.ExtremelyHappy)
                    return 1043280; // Your pet looks extremely happy
                else if (loyalty == PetLoyalty.WonderfullyHappy)
                    return 1043281; // Your pet looks wonderfully happy

                return 0; // shouldn't happen
            }
        }

        private abstract class BaseTrait
        {
            protected BaseTrait()
            {
            }

            public virtual bool Validate(BaseCreature bc)
            {
                return true;
            }

            public abstract TextDefinition Format(BaseCreature bc);
        }

        #endregion
    }

    public class AnimalLoreGump : Gump
    {
        private Mobile m_User;
        private BaseCreature m_Target;
        private int m_Page;

        private enum ButtonID
        {
            NextPage = 100,
            PrevPage = 102,
            StrLock = 1001,
            DexLock = 1002,
            IntLock = 1003,
            SkillLock = 2000
        }

        private static string FormatSkill(BaseCreature c, SkillName name)
        {
            Skill skill = c.Skills[name];

            if (skill.Base == 0)
                return "<div align=right>---</div>";

            return string.Format("<div align=right>{0:F1}</div>", skill.Base);
        }

        private static string FormatAttributes(int cur, int max)
        {
            if (max == 0)
                return "<div align=right>---</div>";

            return string.Format("<div align=right>{0}/{1}</div>", cur, max);
        }

        private static string FormatStat(int val)
        {
            if (val == 0)
                return "<div align=right>---</div>";

            return string.Format("<div align=right>{0}</div>", val);
        }

        private static string FormatElement(int val)
        {
            if (val <= 0)
                return "<div align=right>---</div>";

            return string.Format("<div align=right>{0}%</div>", val);
        }

        private const int LabelColor = 0x24E5;

        private const int NumStaticPages = 3;

        private int NumTotalPages
        {
            get
            {
                int genes = 0;
                foreach (PropertyInfo pi in m_Target.GetType().GetProperties())
                {
                    GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(pi, typeof(GeneAttribute), true);
                    if (attr == null)
                        continue;
                    if (m_User.AccessLevel < AccessLevel.Counselor && !Server.Misc.TestCenter.Enabled)
                    {
                        if (attr.Visibility == GeneVisibility.Invisible)
                            continue;
                        if (attr.Visibility == GeneVisibility.Tame && m_User != m_Target.ControlMaster)
                            continue;
                    }

                    genes++;
                }

                return NumStaticPages + (int)Math.Ceiling(genes / 9.0);
            }
        }

        public AnimalLoreGump(BaseCreature c, Mobile user)
            : this(c, user, 0)
        {
        }

        public AnimalLoreGump(BaseCreature c, Mobile user, int page)
            : base(250, 50)
        {
            m_User = user;
            m_Target = c;
            m_Page = page;
            if (m_Page < 0)
                m_Page = 0;
            if (m_Page >= NumTotalPages)
                m_Page = NumTotalPages - 1;

            AddPage(0);

            AddImage(100, 100, 2080);
            AddImage(118, 137, 2081);
            AddImage(118, 207, 2081);
            AddImage(118, 277, 2081);
            AddImage(118, 347, 2083);

            AddHtml(147, 108, 210, 18, string.Format("<center><i>{0}</i></center>", c.Name), false, false);

            AddButton(240, 77, 2093, 2093, 2, GumpButtonType.Reply, 0);

            AddImage(140, 138, 2091);
            AddImage(140, 335, 2091);

            AddPage(0);
            switch (m_Page)
            {
                case 0:
                    {
                        #region Attributes
                        AddImage(128, 152, 2086);
                        AddHtmlLocalized(147, 150, 160, 18, 1049593, 200, false, false); // Attributes

                        AddHtmlLocalized(153, 168, 160, 18, 1049578, LabelColor, false, false); // Hits
                        AddHtml(280, 168, 75, 18, FormatAttributes(c.Hits, c.HitsMax), false, false);

                        AddHtmlLocalized(153, 186, 160, 18, 1049579, LabelColor, false, false); // Stamina
                        AddHtml(280, 186, 75, 18, FormatAttributes(c.Stam, c.StamMax), false, false);

                        AddHtmlLocalized(153, 204, 160, 18, 1049580, LabelColor, false, false); // Mana
                        AddHtml(280, 204, 75, 18, FormatAttributes(c.Mana, c.ManaMax), false, false);

                        AddHtmlLocalized(153, 222, 160, 18, 1028335, LabelColor, false, false); // Strength
                        AddHtml(280, 222, 75, 18, FormatAttributes(c.Str, c.StrMax), false, false);
                        AddStatLock(355, 222, c.StrLock, ButtonID.StrLock);

                        AddHtmlLocalized(153, 240, 160, 18, 3000113, LabelColor, false, false); // Dexterity
                        AddHtml(280, 240, 75, 18, FormatAttributes(c.Dex, c.DexMax), false, false);
                        AddStatLock(355, 240, c.DexLock, ButtonID.DexLock);

                        AddHtmlLocalized(153, 258, 160, 18, 3000112, LabelColor, false, false); // Intelligence
                        AddHtml(280, 258, 75, 18, FormatAttributes(c.Int, c.IntMax), false, false);
                        AddStatLock(355, 258, c.IntLock, ButtonID.IntLock);

                        AddHtmlLocalized(153, 276, 160, 18, 1077422, LabelColor, false, false); // Stat Cap
                        AddHtml(280, 276, 75, 18, FormatAttributes(c.RawStatTotal, c.StatCap), false, false);

                        AddImage(128, 296, 2086);
                        AddHtmlLocalized(147, 294, 160, 18, 3001016, 200, false, false); // Miscellaneous

                        AddHtmlLocalized(153, 310, 160, 18, 1049581, LabelColor, false, false); // Armor Rating
                        AddHtml(320, 310, 35, 18, FormatStat(c.VirtualArmor), false, false);

                        break;
                        #endregion
                    }
                case 1:
                    {
                        // 1/25/24, Yoar: Added special hireling skill display
                        if (c is BaseHire)
                        {
                            AddHirelingSkills(c);
                            break;
                        }

                        #region Skills
                        AddImage(128, 152, 2086);
                        AddHtmlLocalized(147, 150, 160, 18, 3001030, 200, false, false); // Combat Ratings

                        AddHtmlLocalized(153, 168, 160, 18, 1044103, LabelColor, false, false); // Wrestling
                        AddHtml(320, 168, 35, 18, FormatSkill(c, SkillName.Wrestling), false, false);
                        AddSkillLock(355, 168, c, SkillName.Wrestling, ButtonID.SkillLock + (int)SkillName.Wrestling);

                        AddHtmlLocalized(153, 186, 160, 18, 1044087, LabelColor, false, false); // Tactics
                        AddHtml(320, 186, 35, 18, FormatSkill(c, SkillName.Tactics), false, false);
                        AddSkillLock(355, 186, c, SkillName.Tactics, ButtonID.SkillLock + (int)SkillName.Tactics);

                        AddHtmlLocalized(153, 204, 160, 18, 1044086, LabelColor, false, false); // Magic Resistance
                        AddHtml(320, 204, 35, 18, FormatSkill(c, SkillName.MagicResist), false, false);
                        AddSkillLock(355, 204, c, SkillName.MagicResist, ButtonID.SkillLock + (int)SkillName.MagicResist);

                        AddHtmlLocalized(153, 222, 160, 18, 1044061, LabelColor, false, false); // Anatomy
                        AddHtml(320, 222, 35, 18, FormatSkill(c, SkillName.Anatomy), false, false);
                        AddSkillLock(355, 222, c, SkillName.Anatomy, ButtonID.SkillLock + (int)SkillName.Anatomy);

                        AddHtmlLocalized(153, 240, 160, 18, 1044090, LabelColor, false, false); // Poisoning
                        AddHtml(320, 240, 35, 18, FormatSkill(c, SkillName.Poisoning), false, false);
                        AddSkillLock(355, 240, c, SkillName.Poisoning, ButtonID.SkillLock + (int)SkillName.Poisoning);

                        AddImage(128, 260, 2086);
                        AddHtmlLocalized(147, 258, 160, 18, 3001032, 200, false, false); // Lore & Knowledge

                        AddHtmlLocalized(153, 276, 160, 18, 1044085, LabelColor, false, false); // Magery
                        AddHtml(320, 276, 35, 18, FormatSkill(c, SkillName.Magery), false, false);
                        AddSkillLock(355, 276, c, SkillName.Magery, ButtonID.SkillLock + (int)SkillName.Magery);

                        AddHtmlLocalized(153, 294, 160, 18, 1044076, LabelColor, false, false); // Evaluating Intelligence
                        AddHtml(320, 294, 35, 18, FormatSkill(c, SkillName.EvalInt), false, false);
                        AddSkillLock(355, 294, c, SkillName.EvalInt, ButtonID.SkillLock + (int)SkillName.EvalInt);

                        AddHtmlLocalized(153, 312, 160, 18, 1044106, LabelColor, false, false); // Meditation
                        AddHtml(320, 312, 35, 18, FormatSkill(c, SkillName.Meditation), false, false);
                        AddSkillLock(355, 312, c, SkillName.Meditation, ButtonID.SkillLock + (int)SkillName.Meditation);

                        break;
                        #endregion
                    }
                case 2:
                    {
                        #region Misc
                        AddImage(128, 152, 2086);
                        AddHtmlLocalized(147, 150, 160, 18, 1049563, 200, false, false); // Preferred Foods

                        int foodPref = 3000340;

                        if ((c.FavoriteFood & FoodType.FruitsAndVegies) != 0)
                            foodPref = 1049565; // Fruits and Vegetables
                        else if ((c.FavoriteFood & FoodType.GrainsAndHay) != 0)
                            foodPref = 1049566; // Grains and Hay
                        else if ((c.FavoriteFood & FoodType.Fish) != 0)
                            foodPref = 1049568; // Fish
                        else if ((c.FavoriteFood & FoodType.Meat) != 0)
                            foodPref = 1049564; // Meat

                        AddHtmlLocalized(153, 168, 160, 18, foodPref, LabelColor, false, false);

                        AddImage(128, 188, 2086);
                        AddHtmlLocalized(147, 186, 160, 18, 1049569, 200, false, false); // Pack Instincts

                        int packInstinct = 3000340;

                        if ((c.PackInstinct & PackInstinct.Canine) != 0)
                            packInstinct = 1049570; // Canine
                        else if ((c.PackInstinct & PackInstinct.Ostard) != 0)
                            packInstinct = 1049571; // Ostard
                        else if ((c.PackInstinct & PackInstinct.Feline) != 0)
                            packInstinct = 1049572; // Feline
                        else if ((c.PackInstinct & PackInstinct.Arachnid) != 0)
                            packInstinct = 1049573; // Arachnid
                        else if ((c.PackInstinct & PackInstinct.Daemon) != 0)
                            packInstinct = 1049574; // Daemon
                        else if ((c.PackInstinct & PackInstinct.Bear) != 0)
                            packInstinct = 1049575; // Bear
                        else if ((c.PackInstinct & PackInstinct.Equine) != 0)
                            packInstinct = 1049576; // Equine
                        else if ((c.PackInstinct & PackInstinct.Bull) != 0)
                            packInstinct = 1049577; // Bull

                        AddHtmlLocalized(153, 204, 160, 18, packInstinct, LabelColor, false, false);

                        AddImage(128, 224, 2086);
                        AddHtmlLocalized(147, 222, 160, 18, 1049594, 200, false, false); // Loyalty Rating
                        AddHtmlLocalized(153, 240, 160, 18, (!c.Controlled || c.PetLoyaltyCalc() == PetLoyalty.None) ? 1061643 : 1049594 + c.PetLoyaltyIndex(), LabelColor, false, false);

                        AddImage(128, 260, 2086);
                        AddHtmlLocalized(147, 258, 160, 18, 3000120, 200, false, false); // Gender
                        AddHtmlLocalized(153, 276, 160, 18, c.Female ? 1015328 : 1015327, LabelColor, false, false); // Female | Male

                        if (c.BreedingParticipant)
                        {
                            AddImage(128, 296, 2086);
                            AddHtml(147, 294, 160, 18, "<basefont color=#003142>Growth Stage</basefont>", false, false);
                            AddHtml(153, 312, 160, 18, string.Format("<basefont color=#4A3929>{0:G3}</basefont>", c.Maturity.ToString()), false, false);
                        }

                        break;
                        #endregion
                    }
                default: // rest of the pages are filled with genes - be sure to adjust "pg" calc in here when adding pages
                    {
                        int nextpage = 3;

                        // idea for later - flesh out custom pages more, a string[] is hackish

                        //List<string[]> custompages = c.GetAnimalLorePages();
                        //if (custompages != null && page >= nextpage && page < (nextpage + custompages.Count))
                        //{
                        //    foreach (string[] s in custompages)
                        //    {
                        //        for (int i = 0; i < s.Length; i++)
                        //        {
                        //            AddHtml(153, 168 + 18 * i, 150, 18, s[i], false, false);
                        //        }
                        //    }

                        //    nextpage += custompages.Count;
                        //}

                        #region Genetics
                        if (page >= nextpage)
                        {
                            List<PropertyInfo> genes = new List<PropertyInfo>();

                            foreach (PropertyInfo pi in c.GetType().GetProperties())
                            {
                                GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(pi, typeof(GeneAttribute), true);
                                if (attr == null)
                                    continue;
                                if (m_User.AccessLevel < AccessLevel.Counselor && !Server.Misc.TestCenter.Enabled)
                                {
                                    if (attr.Visibility == GeneVisibility.Invisible)
                                        continue;
                                    if (attr.Visibility == GeneVisibility.Tame && m_User != c.ControlMaster)
                                        continue;
                                }

                                genes.Add(pi);
                            }

                            int pg = m_Page - nextpage;

                            AddImage(128, 152, 2086);
                            AddHtml(147, 150, 160, 18, "<basefont color=#003142>Genetics</basefont>", false, false);

                            for (int i = 0; i < 9; i++)
                            {
                                if (pg * 9 + i >= genes.Count)
                                    break;

                                GeneAttribute attr = (GeneAttribute)Attribute.GetCustomAttribute(genes[pg * 9 + i], typeof(GeneAttribute), true);
                                AddHtml(153, 168 + 18 * i, 120, 18, string.Format("<basefont color=#4A3929>{0}</basefont>", attr.Name), false, false);
                                AddHtml(240, 168 + 18 * i, 115, 18, string.Format("<div align=right>{0:G3}</div>", c.DescribeGene(genes[pg * 9 + i], attr)), false, false);
                            }
                        }
                        break;
                        #endregion
                    }
            }

            if (m_Page < NumTotalPages - 1)
                AddButton(340, 358, 5601, 5605, (int)ButtonID.NextPage, GumpButtonType.Reply, 0);
            if (m_Page > 0)
                AddButton(317, 358, 5603, 5607, (int)ButtonID.PrevPage, GumpButtonType.Reply, 0);
        }

        // 1/25/24, Yoar: Added special hireling skill display
        private void AddHirelingSkills(BaseCreature c)
        {
            int y = 150;

            AddImage(128, y + 2, 2086);
            AddHtmlLocalized(147, y, 160, 18, 3001030, 200, false, false); // Combat Ratings
            y += 18;

            for (int i = 0; i < m_HirelingSkills.Length; i++)
            {
                SkillName skill = m_HirelingSkills[i];

                AddHtmlLocalized(153, y, 160, 18, 1044060 + (int)skill, false, false);
                AddHtml(320, y, 35, 18, FormatSkill(c, skill), false, false);
                AddSkillLock(355, y, c, skill, ButtonID.SkillLock + (int)skill);
                y += 18;
            }
        }

        private static readonly SkillName[] m_HirelingSkills = new SkillName[]
            {
                SkillName.Tactics,
                SkillName.Anatomy,
                SkillName.Parry,
                SkillName.Swords,
                SkillName.Macing,
                SkillName.Archery,
                SkillName.Fencing,
                SkillName.MagicResist
            };

        private void AddSkillLock(int x, int y, BaseCreature c, SkillName skill, ButtonID buttonID)
        {
            if (m_Target.ControlMaster != m_User && m_User.AccessLevel < AccessLevel.GameMaster)
                return; // no fooling around with wild/other people's critters!

            Skill sk = c.Skills[skill];

            if (sk != null)
            {
                int buttonID1, buttonID2;
                int xOffset, yOffset;

                switch (sk.Lock)
                {
                    default:
                    case SkillLock.Up: buttonID1 = 0x983; buttonID2 = 0x983; xOffset = 3; yOffset = 4; break;
                    case SkillLock.Down: buttonID1 = 0x985; buttonID2 = 0x985; xOffset = 3; yOffset = 4; break;
                    case SkillLock.Locked: buttonID1 = 0x82C; buttonID2 = 0x82C; xOffset = 2; yOffset = 2; break;
                }

                AddButton(x + xOffset, y + yOffset, buttonID1, buttonID2, (int)buttonID, GumpButtonType.Reply, 0);
            }
        }

        private void AddStatLock(int x, int y, StatLockType setting, ButtonID buttonID)
        {
            if (m_Target.ControlMaster != m_User && m_User.AccessLevel < AccessLevel.GameMaster)
                return; // no fooling around with wild/other people's critters!

            int buttonID1, buttonID2;
            int xOffset, yOffset;

            switch (setting)
            {
                default:
                case StatLockType.Up: buttonID1 = 0x983; buttonID2 = 0x983; xOffset = 3; yOffset = 4; break;
                case StatLockType.Down: buttonID1 = 0x985; buttonID2 = 0x985; xOffset = 3; yOffset = 4; break;
                case StatLockType.Locked: buttonID1 = 0x82C; buttonID2 = 0x82C; xOffset = 2; yOffset = 2; break;
            }

            AddButton(x + xOffset, y + yOffset, buttonID1, buttonID2, (int)buttonID, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(Server.Network.NetState sender, RelayInfo info)
        {
            switch ((ButtonID)info.ButtonID)
            {
                case ButtonID.NextPage:
                    {
                        m_Page++;
                        break; // gump will be resent at end of OnResponse
                    }
                case ButtonID.PrevPage:
                    {
                        m_Page--;
                        break; // gump will be resent at end of OnResponse
                    }
                case ButtonID.StrLock:
                    {
                        switch (m_Target.StrLock)
                        {
                            case StatLockType.Down: m_Target.StrLock = StatLockType.Locked; break;
                            case StatLockType.Locked: m_Target.StrLock = StatLockType.Up; break;
                            case StatLockType.Up: m_Target.StrLock = StatLockType.Down; break;
                        }
                        break;
                    }
                case ButtonID.DexLock:
                    {
                        switch (m_Target.DexLock)
                        {
                            case StatLockType.Down: m_Target.DexLock = StatLockType.Locked; break;
                            case StatLockType.Locked: m_Target.DexLock = StatLockType.Up; break;
                            case StatLockType.Up: m_Target.DexLock = StatLockType.Down; break;
                        }
                        break;
                    }
                case ButtonID.IntLock:
                    {
                        switch (m_Target.IntLock)
                        {
                            case StatLockType.Down: m_Target.IntLock = StatLockType.Locked; break;
                            case StatLockType.Locked: m_Target.IntLock = StatLockType.Up; break;
                            case StatLockType.Up: m_Target.IntLock = StatLockType.Down; break;
                        }
                        break;
                    }
                default:
                    {
                        if (info.ButtonID >= (int)ButtonID.SkillLock)
                        {
                            int skill = info.ButtonID - (int)ButtonID.SkillLock;
                            Skill sk = null;

                            if (skill >= 0 && skill < m_Target.Skills.Length)
                                sk = m_Target.Skills[skill];

                            if (sk != null)
                            {
                                switch (sk.Lock)
                                {
                                    case SkillLock.Up: sk.SetLockNoRelay(SkillLock.Down); sk.Update(); break;
                                    case SkillLock.Down: sk.SetLockNoRelay(SkillLock.Locked); sk.Update(); break;
                                    case SkillLock.Locked: sk.SetLockNoRelay(SkillLock.Up); sk.Update(); break;
                                }
                            }
                        }
                        else
                            return;

                        break;
                    }
            }


            m_User.SendGump(new AnimalLoreGump(m_Target, m_User, m_Page));
        }
    }
}