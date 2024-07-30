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

/* Scripts/Skill Items/Musical Instruments/BaseInstrument.cs
 * Docs: http://uo.stratics.com/content/skills/bardingdifficulty.php
 *		See text at the bottom of this document:
 * ChangeLog
 *  7/7/2023, Adam (GetInstrument/SetInstrument)
 *      We now sort based on most recently used instrument. 
 *  6/29/2023, Adam (GetInstrument)
 *      Changed the hashtable for the players instruments to a dictionary.
 *      This allows a player to have more then one instrument. 
 *      When barding, GetInstrument() now returns a non-RSM instrument. 
 *  04/02/22, Yoar
 *      Added MakersMark struct that stored both the crafter's serial and name as string.
 *      This way, the crafter tag persists after the mobile has been deleted.
 *  1/2/21, Adam (InstrumentPickedCallback)
 *      Add the following default parameters to InstrumentPickedCallback:
 *          (..., bool requiresBackpack = true, bool musicChatter = true, bool checkMusicianship = true)
 *      This allows more flexible configuration of the music Player interface
 *  12/7/21, Adam (ConsumeUse)
 *      Make virtual so that reward instruments will last forever.
 *  11/14/21, Yoar
 *      Added Resource.
 *	9/1/07, Adam
 *		- Update barding difficulty logic to RunUO 2.0
 *		- Make adjustments to bring it inline with Stratics (fix a bug)
 *		- Add [bardtest command to allow GMs to quickly determine if we are calculating difficulty correctly
 *		- Convert Hits to AOS style hits so that the barding difficulty calc works.
 *	8/31/07, Adam
 *		- give paragon's a hard coded starting difficulty .. something like a 15% chance to bard.
 *			this will then become harder as things like magery and firebreath are added in.
 *		- Add in the added difficulty for creatures with an Aura.
 *	8/30/07, Adam
 *		- Update IsMageryCreature() to check for classes derived from Mage_AI
 *		- make humanoid a bit harder to bard
 *	3/18/07, weaver
 *		Added new static function to return difficulty, managing all 
 *		generic difficulty modifications (non instrument based).
 *	4/18/05, Adam
 *		Increase difficulty for the BaseHealer
 *	11/18/04, Adam
 *		Increase difficulty for the WraithRiderWarrior and WraithRiderMage
 *  7/25/04, Adam
 *		1. add another 10% for exceptional instruments
 *		2. add a global 20% bonus to address 120 skill based formula
 *		3. Up Slayer bonus to 30% from 20% 
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 * 3/25/04 changes by mith
 *	modified CheckSkill call to use max value of 100.0 instead of 120.0.
 */

/* HISTORY 
	Jade. (4:06 PM) : 
	prior to AOS, barding was a little different...
	peacemaking was more of an area effect, not targetted... it was just an escape tool that would make monsters stop fighting for a few seconds (which i thought was fine)
	provoking was as it is now, making two monsters fight each other
	BUT your provocation skill was your success rate
	so if you had 100 provoke you succeeded 100% of the time
	now.... with AOS, they were introducing powerscrolls, and they wanted to give all these skills a reason to need to go up to 120
	and why would you need to improve on a 100% success rate
	Adam Ant (4:09 PM) : 
	I'm confused .. what do powerscrolls have to do with the barding changes .. seems unrelated (and just a more interesting system.)
	Jade. (4:10 PM) : 
	well it does though, because they nerfed provoke to make room for a need to raise provoke to 120
	then to "balance" the "nerf" on provoke they made peacemaking more powerful by making it a target-based skill that kept creatures out of fight mode for much longer than the traditional 1-3 seconds
	i didn't really care that much about peacemaking, because i never played the peace tamer character, i liked provoking
	but we didn't want that easy system of provoking the one where 100 skill meant 100% success
	so we went with the barding difficulties instead
	it was more about provoke than peace
 */

using Server.Engines.Craft;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    public delegate void InstrumentPickedCallback(Mobile from, BaseInstrument instrument);

    public enum InstrumentQuality
    {
        Low,
        Regular,
        Exceptional
    }

    public abstract class BaseInstrument : Item, ICraftable/*, todo ISlayer*/
    {
        private int m_WellSound, m_BadlySound;
        private SlayerName m_Slayer;
        private int m_UsesRemaining;

        private MakersMark m_Crafter;
        private InstrumentQuality m_Quality;
        private CraftResource m_Resource;

        [CommandProperty(AccessLevel.GameMaster)]
        public int SuccessSound
        {
            get { return m_WellSound; }
            set { m_WellSound = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FailureSound
        {
            get { return m_BadlySound; }
            set { m_BadlySound = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public SlayerName Slayer
        {
            get { return m_Slayer; }
            set { m_Slayer = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public InstrumentQuality Quality
        {
            get { return m_Quality; }
            set { UnscaleUses(); m_Quality = value; InvalidateProperties(); ScaleUses(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public MakersMark Crafter
        {
            get { return m_Crafter; }
            set { m_Crafter = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public CraftResource Resource
        {
            get { return m_Resource; }
            set { m_Resource = value; Hue = CraftResources.GetHue(value); InvalidateProperties(); }
        }

        public virtual int InitMinUses { get { return 350; } }
        public virtual int InitMaxUses { get { return 450; } }

        [CommandProperty(AccessLevel.Owner)]
        public int UsesRemaining
        {
            get { return m_UsesRemaining; }
            set { m_UsesRemaining = value; InvalidateProperties(); }
        }

        public void ScaleUses()
        {
            m_UsesRemaining = (m_UsesRemaining * GetUsesScalar()) / 100;
            InvalidateProperties();
        }

        public void UnscaleUses()
        {
            m_UsesRemaining = (m_UsesRemaining * 100) / GetUsesScalar();
        }

        public int GetUsesScalar()
        {
            if (m_Quality == InstrumentQuality.Exceptional)
                return 200;

            return 100;
        }

        public virtual void ConsumeUse(Mobile from)
        {
            // TODO: Confirm what must happen here?

            if (UsesRemaining > 1)
            {
                --UsesRemaining;
            }
            else
            {
                if (from != null)
                    from.SendLocalizedMessage(502079); // The instrument played its last tune.

                Delete();
            }
        }

        private static Dictionary<Item, Mobile> m_Instruments = new();

        public static BaseInstrument GetInstrument(Mobile from)
        {

            // defrag
            List<Item> defrag = new();
            foreach (var kvp in m_Instruments)
                if (kvp.Key.Deleted || kvp.Value.Deleted || (kvp.Value == from && !kvp.Key.IsChildOf(from.Backpack)))
                    defrag.Add(kvp.Key);

            foreach (var item in defrag)
                m_Instruments.Remove(item);

            // find all suitable instruments
            List<BaseInstrument> suitableInstruments = new();
            foreach (var kvp in m_Instruments)
                if (kvp.Value == from && kvp.Key is not RazorInstrument && kvp.Key.IsChildOf(from.Backpack))
                    suitableInstruments.Add(kvp.Key as BaseInstrument);

            // pick the most recently used
            BaseInstrument selected = null;
            foreach (var temp in suitableInstruments)
                if (selected == null)
                    selected = temp;
                else if (temp.LastAccessed > selected.LastAccessed)
                    selected = temp;

            return selected;
        }
        public static void SetInstrument(Mobile from, BaseInstrument item)
        {   // set last accessed so we can select the most recently used instrument
            item.LastAccessed = DateTime.UtcNow;

            if (!m_Instruments.ContainsKey(item))
                m_Instruments.Add(item, from);
        }
        public static int GetBardRange(Mobile bard, SkillName skill)
        {
            return 8 + (int)(bard.Skills[skill].Value / 15);
        }

        public static void PickInstrument(Mobile from, InstrumentPickedCallback callback)
        {
            BaseInstrument instrument = GetInstrument(from);

            if (instrument != null)
            {
                if (callback != null)
                    callback(from, instrument);
            }
            else
            {
                from.SendLocalizedMessage(500617); // What instrument shall you play?
                from.BeginTarget(1, false, TargetFlags.None, new TargetStateCallback(OnPickedInstrument), callback);
            }
        }

        public static void OnPickedInstrument(Mobile from, object targeted, object state)
        {
            BaseInstrument instrument = targeted as BaseInstrument;

            if (instrument == null || instrument is RazorInstrument)
            {
                from.SendLocalizedMessage(500619); // That is not a musical instrument.
            }
            else
            {
                if (instrument.IsChildOf(from.Backpack))
                {
                    SetInstrument(from, instrument);

                    InstrumentPickedCallback callback = state as InstrumentPickedCallback;

                    if (callback != null)
                        callback(from, instrument);
                }
                else
                    from.SendLocalizedMessage(1062334); // This item must be in your backpack to be used.
            }
        }

        public static bool IsMageryCreature(BaseCreature bc)
        {
            return (bc != null && bc.AIObject is MageAI && bc.Skills[SkillName.Magery].Base > 5.0);
        }

        public static bool IsFireBreathingCreature(BaseCreature bc)
        {
            if (bc == null)
                return false;

            return bc.HasBreath;
        }

        public static bool IsPoisonImmune(BaseCreature bc)
        {
            return (bc != null && bc.PoisonImmune != null);
        }

        public static int GetPoisonLevel(BaseCreature bc)
        {
            if (bc == null)
                return 0;

            Poison p = bc.HitPoison;

            if (p == null)
                return 0;

            return p.Level + 1;
        }

        // wea: 18/Mar/2007 Added new static function to return difficulty
        public static double GetCreatureDifficulty(BaseCreature bc)
        {   // sanity
            if (Misc.Diagnostics.Assert(bc != null, "bc == null", Utility.FileInfo()) == false || bc.Deleted)
                return 0.0;

            // convert the hits value back to a AOS value for the purpose of this calc
            // note: we use pre-aos value for hitpoints, but want the post-aos barding logic
            int hits = (bc.Hits / 100) * 60;

            double val = (hits * 1.6) + bc.Stam + bc.Mana;

            val += bc.SkillsTotal / 10;

            if (bc.Paragon == true)
                val = val > 2800 ? val : 2800;      // paragon's are ALL hard and result in like a 10% chance to bard

            if (IsMageryCreature(bc))
                val += 100;

            if (IsFireBreathingCreature(bc))
                val += 100;

            if (IsPoisonImmune(bc))
                val += 100;

            if (bc is VampireBat || bc is VampireBatFamiliar)
                val += 100;

            if (bc.MyAura == AuraType.None)
                val += 100;

            if (bc.Body.IsHuman)
                val += 200;

            if (bc is WraithRiderWarrior)
                val += 200;

            if (bc is WraithRiderMage)
                val += 100;

            if (bc is BaseHealer)
                val += 300;

            val += GetPoisonLevel(bc) * 20;

            if (val > 700)
                val = 700 + (int)((val - 700) * (3.0 / 11));

            val /= 10;

            return val;
        }

        public double GetDifficultyFor(Mobile from, Mobile targ)
        {
            if (!(targ is BaseCreature))
                return 0.0;

            BaseCreature bc = (BaseCreature)targ;

            double val = GetCreatureDifficulty(bc);

            // Instrument specific modifications

            if (m_Quality == InstrumentQuality.Exceptional)
                val -= 5.0; // 10%

            val += GetSlayerBonus(m_Slayer, targ);

            if (HolyWater.UnderEffect(from))
                val += GetSlayerBonus(SlayerName.Silver, targ);

            return val;
        }

        private static double GetSlayerBonus(SlayerName slayer, Mobile targ)
        {
            if (slayer != SlayerName.None)
            {
                SlayerEntry entry = SlayerGroup.GetEntryByName(slayer);

                if (entry != null)
                {
                    if (entry.Slays(targ))
                        return -10.0; // 20%
                    else if (entry.Group.Opposition.Super.Slays(targ))
                        return +10.0; // -20%
                }
            }

            return 0;
        }

        public BaseInstrument(int itemID, int wellSound, int badlySound)
            : base(itemID)
        {
            m_WellSound = wellSound;
            m_BadlySound = badlySound;
            m_UsesRemaining = Utility.RandomMinMax(InitMinUses, InitMaxUses);
        }

        public override void AddNameProperties(ObjectPropertyList list)
        {
            base.AddNameProperties(list);

            if (m_Resource >= CraftResource.OakWood && m_Resource <= CraftResource.Frostwood)
                list.Add(CraftResources.GetLocalizationNumber(m_Resource));
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Crafter != null)
                list.Add(1050043, m_Crafter.Name); // crafted by ~1_NAME~

            if (m_Quality == InstrumentQuality.Exceptional)
                list.Add(1060636); // exceptional

            if (m_Slayer != SlayerName.None)
                list.Add(1017383 + (int)m_Slayer);

            list.Add(1060584, m_UsesRemaining.ToString()); // uses remaining: ~1_val~
        }

        public override void OnSingleClick(Mobile from)
        {
            if (this.HideAttributes == true || (Name == null && UseOldNames))
            {
                base.OnSingleClick(from);
                return;
            }

            ArrayList attrs = new ArrayList();

            if (DisplayLootType)
            {
                if (GetFlag(LootType.Blessed))
                    attrs.Add(new EquipInfoAttribute(1038021)); // blessed
                else if (GetFlag(LootType.Cursed))
                    attrs.Add(new EquipInfoAttribute(1049643)); // cursed
            }

            if (m_Quality == InstrumentQuality.Exceptional)
                attrs.Add(new EquipInfoAttribute(1018305 - (int)m_Quality));

            // TODO: Must this support item identification?
            if (m_Slayer != SlayerName.None)
                attrs.Add(new EquipInfoAttribute(1017383 + (int)m_Slayer));

            int number;

            if (Name == null)
            {
                number = LabelNumber;
            }
            else
            {
                this.LabelTo(from, Name);
                number = 1041000;
            }

            if (attrs.Count == 0 && Crafter == null && Name != null)
                return;

            EquipmentInfo eqInfo = new EquipmentInfo(number, m_Crafter, false, (EquipInfoAttribute[])attrs.ToArray(typeof(EquipInfoAttribute)));

            from.Send(new DisplayEquipmentInfo(this, eqInfo));
        }

        public override string GetOldPrefix(ref Article article)
        {
            string prefix = "";

            if (!HideAttributes && m_Quality == InstrumentQuality.Exceptional)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.An;

                prefix += "exceptional ";
            }

            if (!HideAttributes && m_Slayer == SlayerName.Silver)
            {
                if ((article == Article.A || article == Article.An) && prefix.Length == 0)
                    article = Article.A;

                prefix += "silver ";
            }

            return prefix;
        }

        public override string GetOldSuffix()
        {
            string suffix = "";

            if (!HideAttributes && m_Slayer != SlayerName.None && m_Slayer != SlayerName.Silver)
            {
                if (suffix.Length == 0)
                    suffix += " of ";
                else
                    suffix += " and ";

                suffix += SlayerLabel.GetSuffix(m_Slayer).ToLower();
            }

            if (m_Crafter != null)
                suffix += " crafted by " + m_Crafter.Name;

            return suffix;
        }

        public BaseInstrument(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.WriteEncodedInt((int)m_Resource);

            m_Crafter.Serialize(writer);

            writer.WriteEncodedInt((int)m_Quality);
            writer.WriteEncodedInt((int)m_Slayer);

            writer.WriteEncodedInt((int)m_UsesRemaining);

            writer.WriteEncodedInt((int)m_WellSound);
            writer.WriteEncodedInt((int)m_BadlySound);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                case 2:
                    {
                        m_Resource = (CraftResource)reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        if (version >= 3)
                            m_Crafter.Deserialize(reader);
                        else
                            m_Crafter = reader.ReadMobile();

                        m_Quality = (InstrumentQuality)reader.ReadEncodedInt();
                        m_Slayer = (SlayerName)reader.ReadEncodedInt();

                        m_UsesRemaining = reader.ReadEncodedInt();

                        m_WellSound = reader.ReadEncodedInt();
                        m_BadlySound = reader.ReadEncodedInt();

                        break;
                    }
                case 0:
                    {
                        m_WellSound = reader.ReadInt();
                        m_BadlySound = reader.ReadInt();
                        m_UsesRemaining = Utility.RandomMinMax(InitMinUses, InitMaxUses);

                        break;
                    }
            }

            BaseCraftableItem.PatchResourceHue(this, m_Resource);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 1))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
            }
            else if (from.BeginAction(typeof(BaseInstrument)))
            {
                SetInstrument(from, this);

                // Delay of 7 second before beign able to play another instrument again
                new InternalTimer(from).Start();

                if (CheckMusicianship(from))
                    PlayInstrumentWell(from);
                else
                    PlayInstrumentBadly(from);
            }
            else
            {
                from.SendLocalizedMessage(500119); // You must wait to perform another action
            }
        }

        public static bool CheckMusicianship(Mobile m)
        {
            m.CheckSkill(SkillName.Musicianship, 0.0, 100.0, contextObj: new object[2]);

            return ((m.Skills[SkillName.Musicianship].Value / 100) > Utility.RandomDouble());
        }

        public void PlayInstrumentWell(Mobile from)
        {
            from.PlaySound(m_WellSound);
        }

        public void PlayInstrumentBadly(Mobile from)
        {
            from.PlaySound(m_BadlySound);
        }

        private class InternalTimer : Timer
        {
            private Mobile m_From;

            public InternalTimer(Mobile from)
                : base(TimeSpan.FromSeconds(6.0))
            {
                m_From = from;
                Priority = TimerPriority.TwoFiftyMS;
            }

            protected override void OnTick()
            {
                m_From.EndAction(typeof(BaseInstrument));
            }
        }

        public static void Initialize()
        {
            Server.CommandSystem.Register("BardTest", AccessLevel.GameMaster, new CommandEventHandler(BardTest_OnCommand));
        }

        [Usage("BardTest")]
        [Description("Test barding given an instrument and a target creature.")]
        private static void BardTest_OnCommand(CommandEventArgs e)
        {
            try
            {
                BaseInstrument.PickInstrument(e.Mobile, new InstrumentPickedCallback(OnPickedInstrument));
            }
            catch (Exception ex)
            {
                Server.Diagnostics.LogHelper.LogException(ex);
            }
        }

        public static void OnPickedInstrument(Mobile from, BaseInstrument instrument)
        {
            from.Target = new InternalTarget(from, instrument);
        }

        private class InternalTarget : Target
        {
            private BaseInstrument m_Instrument;
            private bool m_SetSkillTime = false;

            public InternalTarget(Mobile from, BaseInstrument instrument)
                : base(BaseInstrument.GetBardRange(from, SkillName.Peacemaking), false, TargetFlags.None)
            {
                m_Instrument = instrument;
            }

            protected override void OnTargetFinish(Mobile from)
            {
                if (m_SetSkillTime)
                    from.NextSkillTime = Core.TickCount;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!(targeted is Mobile))
                {
                    from.SendLocalizedMessage(1049528); // You cannot calm that!
                }
                else
                {
                    Mobile targ = (Mobile)targeted;
                    int success = 0, fail = 0;

                    for (int ix = 0; ix < 100; ix++)
                    {
                        double diff = m_Instrument.GetDifficultyFor(from, targ) - 10.0;
                        double music = from.Skills[SkillName.Musicianship].Value;

                        if (music > 100.0)
                            diff -= (music - 100.0) * 0.5;

                        if (!from.CheckTargetSkill(SkillName.Peacemaking, targ, diff - 25.0, diff + 25.0, new object[2] { targ, null } /*contextObj*/))
                            fail++;
                        else
                            success++;
                    }

                    from.SendMessage(String.Format("{0} successes {1} failures", success, fail));
                }
            }
        }

        #region ICraftable Members

        public int OnCraft(int quality, bool makersMark, Mobile from, CraftSystem craftSystem, Type typeRes, BaseTool tool, CraftItem craftItem, int resHue)
        {
            Quality = (InstrumentQuality)quality;

            if (makersMark)
                Crafter = from;

            Type resourceType = typeRes;

            if (resourceType == null)
                resourceType = craftItem.Resources.GetAt(0).ItemType;

            Resource = CraftResources.GetFromType(resourceType);

            CraftContext context = craftSystem.GetContext(from);

            if (context != null && context.DoNotColor)
                Hue = 0;

            return quality;
        }

        #endregion
    }
}

/*

Difficulty ratings vary per creature and fall within +/- 2% of the values given in the calculator above. 
The Difficulty rating is the skill level at which the bard has a 50% chance of success versus the creature. At this skill level -25.0, the bard has no chance of success and can't make the attempt. At this skill level +25.0, the bard is guaranteed success and cannot gain skill against this creature. 

Various modifiers have an effect on the final difficulty. You start with base difficulty and then add or subtract depending on skill levels and instrument used. 
- Using an exceptional instrument reduces base difficulty by 5
- Using an aligned "slayer" instrument reduces base diffficulty by 10
- Using an opposite aligned "slayer" instrument increases base difficulty by 10
- If Musicianship skill > 100 then bonus = (Musicianship skill - 100) / 2
- Subtract musicianship bonus from base difficulty
- When provoking, subtract 5 from base difficulty
- When discording or targeted peacemaking, subtract 10 from base difficulty 

The % chance of success is 50 + 2 * (Bard Skill - Resulting Difficulty)
Example: At 120.0 skill, a bard with a dragon-slaying instrument has approx. 60% chance of success to use an ability against an Ancient Wyrm, or to provoke two Ancient Wyrms to fight. At 100.0 skill, with the same instrument, the chance is approx. 20%. 

There are a few creatures that do not follow the same rules for Bard Difficulty though, those are called "Non-Bardable", which basically means that they are completely immune to all Bard Skill effects, no matter how high your skills are. The list on the right lists all the creatures that are currently known to be "Non-Bardable". 
 Non-Bardable Creatures 
Dark Wolf (Familiar)
Death Adder (Familiar)
Elite Ninja
Horde Minion (Familiar)
Mercenary
Noble
Revenant
Ronin
Seeker of Adventure
Shadow Wisp (Familiar)
Vampire Bat (Familiar)
 
An approximate barding difficulty can be calculated by using the following method: 
Use the Animal Lore skill on the creature 
Add up the following attributes: 
Max Hit Points * 1.6 
Max Stamina 
Max Mana 
all Skills 
Add another 100 points for each of the following abilities: 
Spell Casting 
Fire Breath 
Radiation or Aura Damage (Heat, Cold etc.) 
Resistance to Poison 
Lifeforce Draining 
Add another 20 points for each level of poison attack the creature can do. This ranges from 20 for a level 1 poison attack to 100 for a level 5 poison attack 
If the creature's stats and skills total is 700 or less, continue with step 7 
If the creature's stats and skills total is greater than 700, subtract 700 from the end result of step 4, then multiply the result with 0.275, and finally add 700 to that result. 
Drop the numbers after the decimal point and then divide by 10. The result is the creature's barding difficulty. 
If the result of the difficulty calculation is higher than 160, then the creatures barding difficulty is lowered to 160.  
*/