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

/* Scripts/Items/Addons/TourneyStoneAddon.cs
 * ChangeLog :
 *  02/28/07, weaver
 *		Incorrect rule handling fix.
 *	02/25/07, weaver
 *		- Fixed multiple message alert
 *		- Fixed rule validation to correctly validate sub classes
 *		- Separated classes into different source files (under TourneyStone/)
 *	05/22/06, weaver
 *		Fixed co-owner access so accessible via rune as well as base.
 *	05/17/06, weaver
 *		Added access for co-owners!
 *	04/25/06, weaver
 *		- Added access for Counselors.
 *		- Changed comment instances of 'erlein' to 'weaver'.
 *	07/29/05, weaver
 *		Added calls to ClassNameTranslator to better deal with item name approximation.
 *	05/28/05, weaver
 *		Fixed problem with Limit of condition handling in Item RuleCondition.
 *	05/27/05, weaver
 *		- Rewrote rule system to allow multiple conditions to be specified for
 *		each rule.
 *			1) Added RuleCondition class
 *			2) Derived sub condition classes from this and repositioned validation
 *			3) Altered deserialize, serialize and the ruleset loaders accordingly
 *		- Fixed a problem with rune + base components. New ones were being added
 *		to the AddonComponent upon each deserialization.
 *		- Fixed a problem with multiple items not being checked properly
 *		due to shared object reference.
 *		- Added dynamic label filling function to centralize template filling
 *		operations (complicated by conditions, makes it worthwhile)
 *	05/19/05, weaver
 *		Fixed access problem so house owners can access the stone.
 *	05/19/05, weaver
 *		Made it drop all held items before search !
 *	05/19/05, weaver
 *		Fixed referencing problem between base, rune and stone on deserialize
 *		(using internal AddonComponent reference now).
 *	05/18/05, weaver
 *		Changed that to alter hue of base if valid to white, red if invalid
 *		maintaining yellow rune colour throughout (!)
 *	05/18/05, weaver
 *		Change hueing to apply to rune only, not the base and hue yellow for
 *		pass, red for invalid.
 *	05/18/05, weaver
 *		- Changed sound to something nicer.
 *		- Altered Kal Ort Ora to make it scan rather than toggle activated flag.
 *		- Fixed gump handling.
 *	05/17/05, weaver
 *		- Removed whitespace prefix in type name display.
 *		- Changed XML tag from 'Version' to 'version'.
 *		- Made ruleset version attribute double instead of integer.
 *		- Added rule count check in XML read for more stability.
 *		- Added a new property to define whether or not rule is configurable.
 *		- Added switch on + off functionality via command 'Kal Ort Ora'.
 *	05/15/05, weaver
 *		- Updated property gathering calls so work with users.
 *		- Fixed overhead messages so users can see them too.
 *		- Added check to make sure stone accessor is house owner or GM+ AccessLevel.
 *	05/15/05, weaver
 *		Initial creation.
 */

using Server;
using Server.Accounting;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Multis;
using System;
using System.Collections;
using System.IO;
using System.Xml;

namespace Server.Items
{

    public class TourneyStoneAddon : BaseAddon
    {
        public override bool BlocksDoors { get { return false; } }

        public static int myinstance;

        // RULE VALIDATOR =============================
        public class RuleValidator
        {
            // Holds the playermobile we're dealing with in
            // validation check
            private PlayerMobile m_Player;
            public PlayerMobile Player
            {
                get
                {
                    return m_Player;
                }
                set
                {
                    m_Player = value;
                }
            }

            // Manages all the items held
            public class HeldStuff
            {
                // Holds all the items
                private ArrayList m_Contents;

                public ArrayList Contents
                {
                    get
                    {
                        return m_Contents;
                    }
                    set
                    {
                        m_Contents = value;
                    }
                }

                // Adds items to held list dynamically
                public void AddItem(object o)
                {
                    // Handle contained objects
                    if (o is BaseContainer)
                        foreach (Item item in ((BaseContainer)o).Items)
                            AddItem(item);

                    // Handle this object
                    HeldItem hi = new HeldItem(o.GetType());

                    // If it has quantity, we want to reflect that in our
                    // instance
                    if (((Item)o).Amount > 0)
                        hi.m_Count = ((Item)o).Amount;

                    // Make sure there are no others like this
                    foreach (HeldItem hit in m_Contents)
                    {
                        // It exists, so increment and return
                        if (hit.m_Type == hi.m_Type)
                        {
                            hit.m_Count += hi.m_Count;

                            // Add the reference to this object
                            hit.Ref.Add(o);
                            return;
                        }
                    }

                    // Reference the base object here - this could
                    // provide some nifty future functionality, but for now
                    // is used to check property values
                    hi.Ref.Add(o);

                    // It doesn't exist, so add it
                    m_Contents.Add(hi);
                }

                public HeldStuff(PlayerMobile pm)
                {
                    // Init the contents
                    Contents = new ArrayList();

                    // Drop to pack any held items
                    Item held = pm.Holding;
                    if (held != null)
                    {
                        held.ClearBounce();
                        if (pm.Backpack != null)
                        {
                            pm.Backpack.DropItem(held);
                        }
                    }
                    pm.Holding = null;

                    // Populate from PlayerMobile's pack + equipped items
                    ArrayList PackItems = new ArrayList(pm.Items);

                    foreach (Item pi in PackItems)
                        AddItem(pi);

                    // Now populated!
                }
            }

            // Holds our stone reference
            private TourneyStoneAddon m_TourneyStone;
            public TourneyStoneAddon TourneyStone
            {
                get
                {
                    return m_TourneyStone;
                }
                set
                {
                    m_TourneyStone = value;
                }
            }

            // Construct RuleValidator object
            public RuleValidator(TourneyStoneAddon tstone)
            {
                // Set stone reference (we need the rules it has stored)
                TourneyStone = tstone;
            }


            // Validate the mobile passed and add into arraylist
            // passed by reference
            public void Validate(PlayerMobile pm, ref ArrayList fp)
            {

                // Make sure we have a ruleset
                if (TourneyStone.Ruleset.Count == 0)
                    return;

                Player = pm;

                ArrayList failures = new ArrayList();
                ArrayList Rules = new ArrayList();
                ArrayList Fallthroughs = new ArrayList();

                // Grab the ruleset
                Rules = TourneyStone.Ruleset;

                int RulesetCount = Rules.Count;

                // Create a HeldStuff instance to figure out
                // what they have

                HeldStuff CurrentHeld = new HeldStuff(pm);

                // Loop through each rule & condition... deal

                for (int rpos = 0; rpos < RulesetCount; rpos++)
                {
                    Rule RuleChecking = (Rule)Rules[rpos];

                    if (RuleChecking.Conditions.Count == 0 || RuleChecking.Active == false)
                        continue;

                    foreach (RuleCondition rc in RuleChecking.Conditions)
                    {
                        // What kind of condition are we dealing with here?
                        if (rc is ItemCondition)
                        {
                            // ITEM CONDITION
                            // (validate entire held item list against rule)

                            string FailText = RuleChecking.FailText;
                            string sDynFails = "";
                            bool bFails = false;

                            // wea: 25/Feb/2007 Modified call to pass CurrentHeld rather
                            // than one item at a time
                            if (!rc.Guage(CurrentHeld.Contents, ref Fallthroughs))
                            {
                                sDynFails += string.Format("{0}{1}", (sDynFails == "" ? "" : ", "), RuleChecking.FailTextDyn);
                                bFails = true;
                            }


                            if (bFails)
                            {
                                if (sDynFails != "")
                                    FailText += " You have : " + sDynFails;

                                FailText = RuleChecking.DynFill(FailText);
                                failures.Add(FailText);
                            }
                        }
                        else if (rc is ItemPropertyCondition)          // wea: 28/Feb/2007 Incorrect rule handling fix
                        {

                            // ITEM PROPERTY CONDITION

                            string FailText = RuleChecking.FailText;
                            string sDynFails = "";
                            bool bFails = false;

                            foreach (HeldItem hi in CurrentHeld.Contents)
                            {
                                if (!rc.Guage(hi, ref Fallthroughs))
                                {
                                    sDynFails += string.Format("{0}{1}", (sDynFails == "" ? "" : ", "), RuleChecking.FailTextDyn);
                                    bFails = true;
                                }
                            }

                            if (bFails)
                            {
                                if (sDynFails != "")
                                    FailText += " You have : " + sDynFails;

                                FailText = RuleChecking.DynFill(FailText);
                                failures.Add(FailText);
                            }
                        }
                        else if (rc is PropertyCondition)
                        {
                            // MOBILE PROPERTY CONDITION
                            // (validate using the mobile)

                            if (!rc.Guage(Player))
                            {
                                string FailText = RuleChecking.FailText + " " + RuleChecking.FailTextDyn;
                                FailText = RuleChecking.DynFill(FailText);
                                failures.Add(FailText);
                            }
                        }
                    }
                }

                fp = failures;
            }
        }
        // END RULE VALIDATOR==========================


        // Active stone ruleset is held in Rules
        private ArrayList m_Ruleset;
        public ArrayList Ruleset
        {
            get
            {
                return m_Ruleset;
            }
            set
            {
                m_Ruleset = value;
            }
        }

        public double RulesetVersion;

        public override BaseAddonDeed Deed
        {
            get
            {
                return new TourneyStoneAddonDeed();
            }
        }

        // This allows separate hueing of rune + base
        public override bool ShareHue { get { return false; } }

        [Constructable]
        public TourneyStoneAddon()
        {
            // Constructed from classes for flexibility, as per flagstone
            AddComponent(new TourneyStoneBase(), 0, 0, 0);
            AddComponent(new TourneyStoneRune(), 0, 0, 0);

            // Load the ruleset from XML
            RulesetLoader();
        }

        public TourneyStoneAddon(Serial serial)
            : base(serial)
        {
        }


        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // Version

            // Ruleset version of this stone
            writer.Write(RulesetVersion);

            // Ruleset count, for reading back
            writer.Write(Ruleset.Count);

            // Write out the ruleset
            foreach (Rule rule in Ruleset)
            {
                writer.Write(rule.Active);

                // How many conditions?
                writer.Write(rule.Conditions.Count);

                // Loop through conditions
                foreach (RuleCondition rc in rule.Conditions)
                {
                    writer.Write(rc.Configurable);

                    if (rc.Configurable)
                    {
                        // If it's configurable, write out the Quantity, then the PropertyVal
                        writer.Write(rc.Quantity);
                        writer.Write(rc.PropertyVal);
                    }
                }
            }

            // Serialize any new properties here
            // ..

            // ..
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            // Ruleset version of this saved stone
            double SavedVersion = reader.ReadDouble();

            // Load XML ruleset base
            RulesetLoader();

            int RuleCount = reader.ReadInt();

            // Version 0 - pre RuleCondition class, load differently
            // to future versions
            // --
            if (version == 0)
            {
                // Ignore
                for (int rpos = 0; rpos < RuleCount; rpos++)
                {
                    reader.ReadBool();
                    bool configurable = reader.ReadBool();
                    if (configurable)
                    {
                        reader.ReadInt();
                        reader.ReadString();
                    }
                }
            }
            // --
            else
            {
                // Compare versions.. ignore any out of date saved data

                if (RulesetVersion == SavedVersion && RuleCount == Ruleset.Count)
                {
                    // Same XML ruleset, so load custom data
                    //
                    for (int rpos = 0; rpos < RuleCount; rpos++)
                    {
                        Rule TestRule = (Rule)Ruleset[rpos];
                        TestRule.Active = reader.ReadBool();

                        // How many conditions are we dealing with?
                        int ConCount = reader.ReadInt();

                        for (int cpos = 0; cpos < ConCount; cpos++)
                        {
                            // Match up each condition - they should all exist
                            RuleCondition TestCon = (RuleCondition)TestRule.Conditions[cpos];

                            TestCon.Configurable = reader.ReadBool();
                            if (TestCon.Configurable == true)
                            {
                                TestCon.Quantity = reader.ReadInt();
                                TestCon.PropertyVal = reader.ReadString();
                            }
                        }
                    }
                }
                else
                {
                    // New XML ruleset, serialized data is out of date, so just deserialize
                    // and leave the default activisation + customization data.
                    //
                    for (int rpos = 0; rpos < RuleCount; rpos++)
                    {
                        reader.ReadBool();
                        int ConCount = reader.ReadInt();

                        for (int cpos = 0; cpos < ConCount; cpos++)
                        {
                            bool configurable = reader.ReadBool();
                            if (configurable)
                            {
                                reader.ReadInt();
                                reader.ReadString();
                            }
                        }
                    }
                }
            }

            switch (version)
            {
                case 2:
                    // deserialize the properties added with version change
                    goto case 1;
                case 1:
                    break;
            }


            // Add the components

            //AddComponent( new TourneyStoneBase(),0,0,0);
            //AddComponent( new TourneyStoneRune(),0,0,0);
        }

        public override bool HandlesOnSpeech { get { return true; } }
        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!from.InRange(this, 0))
                return;

            if (e.Speech.ToLower() == "kal ort ora")
            {
                // Perform check
                if (from is PlayerMobile)
                {
                    // Sparklies!! yay
                    Effects.SendLocationParticles(EffectItem.Create(
                                                    Location, Map, EffectItem.DefaultDuration
                                                                    ), 0x376A, 9, 32, 5020);
                    new ConditionCheckTimer((PlayerMobile)from, this).Start();
                    from.PlaySound(0x1F9);
                }
            }
        }

        // Checks the conditions stored in the stone against the playermobile
        // passed it and reports publicly + with stone hue

        public void CheckConditions(PlayerMobile pm)
        {
            ArrayList FailurePoints = new ArrayList();
            RuleValidator rv = new RuleValidator(this);

            rv.Validate(pm, ref FailurePoints);

            if (FailurePoints.Count > 0)
            {
                // Tell them what + why
                pm.PublicOverheadMessage(Network.MessageType.Regular, 0, false, string.Format("You failed the test for {0} reason{1} :", FailurePoints.Count, FailurePoints.Count != 1 ? "s" : ""));

                foreach (string strFailure in FailurePoints)
                    pm.PublicOverheadMessage(Network.MessageType.Regular, 0, false, strFailure);

                // Hue the rune invalid
                ((AddonComponent)Components[0]).Hue = 137;
                new RehueTimer(pm, this).Start();
            }
            else
            {
                pm.PublicOverheadMessage(Network.MessageType.Regular, 0, false, "You've passed the test!");

                // Hue the rune valid
                ((AddonComponent)Components[0]).Hue = 1000;
                new RehueTimer(pm, this).Start();
            }
        }

        // Condition check timer

        private class ConditionCheckTimer : Timer
        {
            private TourneyStoneAddon m_TourneyStone;
            private PlayerMobile m_Player;

            public ConditionCheckTimer(PlayerMobile from, TourneyStoneAddon tsaddon)
                : base(TimeSpan.FromSeconds(2.0))
            {
                Priority = TimerPriority.TwoFiftyMS;
                m_TourneyStone = tsaddon;
                m_Player = from;
            }

            protected override void OnTick()
            {
                m_TourneyStone.CheckConditions(m_Player);
            }
        }

        // Hue change back timer

        private class RehueTimer : Timer
        {
            private TourneyStoneAddon m_TourneyStone;

            public RehueTimer(PlayerMobile from, TourneyStoneAddon tsaddon)
                : base(TimeSpan.FromSeconds(5.0))
            {
                Priority = TimerPriority.TwoFiftyMS;
                m_TourneyStone = tsaddon;
            }

            protected override void OnTick()
            {
                ((AddonComponent)m_TourneyStone.Components[0]).Hue = 937;
            }
        }

        // Loader routine to handle XML

        public void RulesetLoader()
        {
            Ruleset = new ArrayList();
            XmlDocument xdoc = new XmlDocument();

            string filePath = Path.Combine(Core.DataDirectory, "TourneyStone.xml");
            if (!File.Exists(filePath))
                throw (new FileNotFoundException());

            xdoc.Load(filePath);
            XmlElement root = xdoc["Ruleset"];

            RulesetVersion = Convert.ToDouble(Accounts.GetAttribute(root, "version", "0"));

            // Rules

            foreach (XmlElement node in root.GetElementsByTagName("Rule"))
            {
                try
                {
                    Rule ReadRule = new Rule();

                    ReadRule.Desc = Accounts.GetText(node["Desc"], "");
                    ReadRule.FailText = Accounts.GetText(node["FailText"], "");

                    // Conditions

                    ReadRule.Conditions = new ArrayList();

                    foreach (XmlElement ConNode in node.GetElementsByTagName("Condition"))
                    {
                        string rDef = Accounts.GetText(ConNode["Typ"], "");
                        RuleCondition RuleCon;

                        if (rDef == "Property")
                        {
                            RuleCon = new PropertyCondition();
                        }
                        else if (rDef == "ItemProperty")
                        {
                            RuleCon = new ItemPropertyCondition();
                        }
                        else if (rDef == "Item")
                        {
                            RuleCon = new ItemCondition();
                        }
                        else
                            continue;


                        RuleCon.Quantity = Accounts.GetInt32(Accounts.GetText(ConNode["Quantity"], ""), 0);
                        RuleCon.Property = Accounts.GetText(ConNode["Property"], "");
                        RuleCon.PropertyVal = Accounts.GetText(ConNode["PropertyVal"], "");
                        RuleCon.Limit = Accounts.GetInt32(Accounts.GetText(ConNode["Limit"], ""), 0);
                        string sItemType = Accounts.GetText(ConNode["ItemType"], "");
                        string Configurable = Accounts.GetText(ConNode["Configurable"], "");

                        if (Configurable.ToUpper() == "TRUE")
                            RuleCon.Configurable = true;
                        else
                            RuleCon.Configurable = false;

                        // Divine the type from the string if there is one


                        if (sItemType != "")
                        {
                            Type tItemType = ScriptCompiler.FindTypeByName(sItemType);

                            if (tItemType != null)
                                RuleCon.ItemType = tItemType;
                        }

                        RuleCon.Rule = ReadRule;
                        ReadRule.Conditions.Add(RuleCon);
                    }

                    // Default activation to false (set through gump +
                    // deserialization process)
                    ReadRule.Active = false;

                    // Add to the stone's RuleSet
                    Ruleset.Add(ReadRule);
                }
                catch (Exception e)
                {
                    Console.WriteLine("TourneyStoneaddon : Exception reading XML - {0}", e);
                }
            }
        }
    }

    // The deed is derived from the BaseAddonDeed

    public class TourneyStoneAddonDeed : BaseAddonDeed
    {
        public override BaseAddon Addon
        {
            get
            {
                return new TourneyStoneAddon();
            }
        }

        [Constructable]
        public TourneyStoneAddonDeed()
        {
            Name = "a tourney stone deed";
        }

        public TourneyStoneAddonDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // Version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

public class TourneyStoneBase : AddonComponent
{
    [Constructable]
    public TourneyStoneBase()
        : this(1306)
    {
        Name = "a tourney stone";
        Hue = 937;
    }

    public TourneyStoneBase(int itemID)
        : base(itemID)
    {
    }

    public TourneyStoneBase(Serial serial)
        : base(serial)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        BaseHouse house = BaseHouse.FindHouseAt(this);

        if (from.InRange(this, 2))
        {
            if ((from.AccessLevel >= AccessLevel.Counselor ||
                (house != null && (house.IsOwner(from) || house.IsCoOwner(from)) && house.Contains(this))))
            {
                from.CloseGump(typeof(TourneyStoneUseGump));
                from.SendGump(new TourneyStoneUseGump(from, ((TourneyStoneAddon)Addon)));
            }
            else
                from.SendMessage("You must be the house owner to access that.");
        }
        else
            from.SendMessage("You must be closer. ");


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

public class TourneyStoneRune : AddonComponent
{
    [Constructable]
    public TourneyStoneRune()
        : this(3686)
    {
        Name = "a tourney stone";
        Hue = 153;
    }

    public TourneyStoneRune(int itemID)
        : base(itemID)
    {
    }

    public TourneyStoneRune(Serial serial)
        : base(serial)
    {
    }

    public override void OnDoubleClick(Mobile from)
    {
        BaseHouse house = BaseHouse.FindHouseAt(this);

        if (from.InRange(this, 2))
        {
            if ((from.AccessLevel >= AccessLevel.Counselor ||
                (house != null && (house.IsOwner(from) || house.IsCoOwner(from)) && house.Addons.Contains(this))))
            {
                from.CloseGump(typeof(TourneyStoneUseGump));
                from.SendGump(new TourneyStoneUseGump(from, ((TourneyStoneAddon)Addon)));
            }
            else
                from.SendMessage("You must be the house owner to access that.");
        }
        else
            from.SendMessage("You must be closer. ");
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