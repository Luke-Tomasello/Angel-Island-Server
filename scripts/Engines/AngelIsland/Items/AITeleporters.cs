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

/* Scripts/Engines/AngelIsland/AITeleporters.cs
 * ChangeLog
 *  11/13/2023,s Adam (Exit Cave)
 *      When exiting the cave (escape), allow rares and smuggled items to be in sub-containers.
 *  11/5/2023, Adam (OnMoveOver/Backpack/Bankbox overload)
 *      If a player is trying to drag something into prison on their cursor, deny entry if
 *          all worn + drag items won't fit in backpack
 *          all worn + drag + backpack items +1 won't fit in bank box
 *  6/7/2023, Adam (MoveItemsToBank)
 *      m.SendEverything() is needed for OSI clients. They get confused with this automated
 *      movement of items, then a teleport. We need to inform them of the changes before
 *      we teleport
 *  8/16/22, Adam
 *      RemoveItem() from backpack before dropping elsewhere.
 *      I think this was causing problems with item weight calcs
 *  8/12/22, Adam
 *      - remove extra death robe
 *      - set the 'prison issued' backpack to Backpack.FeatureBits.PrisonIssue if Mortalis
 *  7/31/22, Adam
 *      AIEntrance tp: Add the ability to set the Inmate flag on a player manually, then invoke the teleporter to send them to AI Prison.
 *      Add the notion of 'pm.MinimumSentence'. This is so that players can be sent to AIP regardless if they have murder counts
 *  8/3/21, Adam new teleporter (AISpawnEntrance)
 *      We create a an array of destinations so inmates don't all pile up at the same location
 *      These teleporters don't get deleted.
 *  5/30/2021, Adam
 *		Allow escapees access to the lighthouse, and they can head to thge brit sewers from there.
 *	3/17/10, adam
 *		Add a KeepStartup parameter to the EmptyPackOnExit() function.
 *			if KeepStartup is true, you will keep a blessed stinger and your spellbook.
 *			this is never sent when exiting the prison, but instead set OnLogon() in PlayerMobile to reset your backpack
 *			see comments in PlayerMobile:OnLogon
 *	3/12/10, adam
 *		Add suppord for ShortCriminalCounts. Like murder counts, but not reduced if you escape
 *		ShortCriminalCounts decay at the same rate as short term murder counts in prison only
 *	3/10/10, adam
 *		Lots of cleanup.
 *		Add common function for cleaning the players backpack on exit
 *		add 1 in 5000 chance to keep your light housepass
 *		delete regs, weapons, etc. on exit
 *	07/27/08, weaver
 *		Correctly remove gumps from the NetState object on CloseGump() (integrated RunUO fix).
 *	3/8/06, Pix
 *		Now shuts down all AITeleportQueryGump on teleport.
 *	2/10/06, Adam
 *		Reuse the Moongate.RestrictedItem() check as it is the same item list
 *	10/11/05, Pix
 *		Added DropHolding code to AIEscapeExit (to make sure nobody sneaks out a blessed stinger)
 *	4/29/05, Adam
 *		In AIParoleExit()
 *		1. Allow prisoners to walk out if Server Wars are on
 *		2. Flip logic for not deleted you lighthouse passes unless it's going to let you leave.
 *	04/05/05, erlein
 *		Added the same check for actual dropping to force the items
 *		to land in the bank on teleport.
 *	04/05/05, erlein
 *		Added check for Mobile.Alive property in MoveItemsToBank.
 *	2/26/05, mith
 *		AIEscapeExit, modified to reduce escapee's counts by half without going below 0.
 * 1/27/05, Darva
 *		This time -really- check. :P
 * 1/25/05, Darva
 *		check to make sure bank can hold the items put in it, refuse transportation to the island if not.
 * 12/29/04, Pix
 *		Made sure we always move the player to Felucca (Changed from directly setting the location
 *		to calling MoveToWorld instead)
 * 10/12/04, Adam
 *		Add a comment to explain the count reduction logic.
 *		From the code it is not obvious that the intent is to prohibit the STC from dropping below 5
 *		This is as per design.
 * 10/06/04, Pix
 *		Fixed short term murder count reduction in AIEscapeExit.
 *	9/28/04, Pix
 *		Made anything held (on cursor) bounce back before we check the pack for LHPasses/etc.
 *	9/26/04, Pix
 *		AICaveEntrance and AIEscapeExit shouldn't be persistent when the world loads.
 *		If they exist when the world loads, we now delete them.
 *	9/24/04, Pix
 *		Changed so Stingers don't get unblessed via the AICaveEntrance, instead they get unblessed
 *		using the AIEscapeExit.
 *		Now armed stingers get unblessed too.
 *	9/2/04, Pix
 *		Changed lighthousepass deleting mechanism - now it uses (container).FindItemsByType to recursively
 *		search players' backpacks.
 *		Changed AIStingers to LootType.Regular on teleport with these teleporters.
 *	8/12/04, mith
 *		AIParoleExit.OnMoveOver(): Copied code from AICaveEntrance to delete Lighthouse Passes from players packs after they get paroled.
 *	6/10/04, mith
 *		Modified AICaveEntrance.OnMoveOver() to delete all passes from a players pack, not just the first one it finds.
 *	5/10/04, mith
 *		Modified CaveEntrance.OnMoveOver() to only check for LHPasses if player is AccessLevel.Player. 
 *		GMs and Admins may use the teleporter without having a LHPass.
 *	4/30/04, mith
 *		Fixed a bug where if player is teleported to AI while dead, ResetKillTime() call fails and they don't get the benefit of shorter count timers.
 *	4/27/04, mith
 *		Added ResurrectGump calls to AIEntrance.OnMoveOver. This should pop-up the gump after the player has teleported to AI.
 *		Modified the way we handle dead/alive players and their robe/shroud. Shroud is not deleted on a living player.
 *	4/20/04, mith
 *		Small change to the giving out of deathrobes on entrance due to a bug I found.
 *	4/19/04, mith
 *		Modified Serialize/Deserialize to work with older versions of teleporters.
 *		Teleporters will be updated to "version 3" on next server restart without having to be replaced.
 *	4/15/04, mith
 *		Streamlined the code, removed variables that weren't used and tried to make this generally more readable.
 *	4/13/04 changes by smerX
 *		Must posess LightHousePass to use AICaveEntrance
 *	4/9/04 changes by mith
 *		Added code to ResetKillTime on entrance and exit, so that counts decay faster on AI.
 *	4/8/04 changes by smerX
 *		Added AICaveEntrance class.
 *	4/7/04, changes by mith
 *		AIEntrance.DoTeleport(), replaced call to TeleportPets with call to StablePets.
 *	4/1/04
 *		Modified formula for successful escape to give diminishing returns on count decrease.
 *	3/31/04
 *		Added code to take all of a player's equipment and put it in a bag in their bankbox if they use the entrance teleporter.
 *		Moved code to create aiStinger as well as a robe, into the OnMoveOver event (since we want to be able to return true/false if moving items fails)
 *		Added code to create empty spellbook on entrance.
 *	3/30/04
 *		Added ParoleExit and EscapeExit teleporters, renamed file
 *		Tweaked "access denied" error messages to be more descriptive (can't use 
 *		this when dead, can't use this with less than x number of counts, etc)
 *		Added message to escape teleporter to let escapee know new number of ST counts.
 *		Removed code to verify that the person using the entrance teleporter is a ghost. 
 *		Living/Dead stipulations for exit teleporters still apply.
 *	3/29/04 changes by mith
 *		Changed default destination to put user in Warden's office.
 *		Added code to generate dagger on use of teleporter rather than upon resurrection.
 *		Added code to set PlayerFlag.Inmate = true when player teleports into AI.
 *	3/27/04 changes by mith
 *		TODO: Something needs to be done with pets, either during teleport, or right after. Thinking of putting a
 *		special stablemaster next to the healer, that will res/stable pets, but not allow claiming.
 *		Moved the status checking out of StartTeleport and into OnMoveOver, which returns true if teleporter doesn't work.
 *	3/26/04 Created by mith
 *		Modification of the current Teleporter.cs file that allows us to check the user's kills and whether they are dead or not.
 *		Automagically set the location point and map to put the user inside the cellblock on AI.
 */
using Server.Engines.CrownSterlingSystem;
using Server.Engines.DataRecorder;
using Server.Guilds;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Server.Items
{
    public class AIEntrance : Item
    {
        private bool m_Active, m_SourceEffect, m_DestEffect;
        private int m_SoundID;
        private bool m_CheckMurders = true;
        private TimeSpan m_Delay;
        private Context m_Context;
        private bool m_Visitor = false;
        private bool SentToPrison { get { return (m_Context & Context.PrisonCommand) != 0; } }

        #region Props
        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }

        public override int LabelNumber { get { return 1026095; } } // teleporter
        #endregion Props

        [Flags]
        public enum Context
        {
            None = 0x00,
            PrisonCommand = 0x01,
        }
        [Constructable]
        public AIEntrance(bool checkMurders = true, Context context = Context.None)
            : base(0x1BC3)
        {
            m_Active = true;
            m_CheckMurders = checkMurders;
            m_Context = context;
            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Entrance");
            else
                LabelTo(from, "(inactive)");
        }
        public virtual void StartTeleport(Mobile m)
        {
            //shut down all AITeleportQueryGumps
            m.CloseGumps(typeof(AITeleportQueryGump));

            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }
        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }
        public virtual void DoTeleport(Mobile m)
        {
            Utility.StablePets(m, 13);

            // Adam, 8/6/21: Hue the death robe orange. kind of a funky orange hue. Look for a better one
            if (!HasDeathRobe(m))
            {
                DeathRobe robe = new Server.Items.DeathRobe();
                robe.Hue = 0x2b;
                if (!m.AddToBackpack(robe))
                    robe.Delete();
            }

            if (m_SourceEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            // give them access to the prison
            if (m.Player && m.AccessLevel == AccessLevel.Player)
            {
                if (m_Visitor)
                    ((PlayerMobile)m).PrisonVisitor = true;
                else
                    ((PlayerMobile)m).PrisonInmate = true;
            }

            // This puts them in the Warden's office on Angel Island.
            Server.Point3D location = new Point3D(355, 836, 20);
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);
        }
        private bool AlreadyAPrisoner(Mobile m)
        {
            if (m != null && m.Region != null && m.Region.IsAngelIslandRules)
                return true;

            return false;
        }
        public override bool OnMoveOver(Mobile m)
        {
            if (m_Active)
            {
                PlayerMobile pm = m as PlayerMobile;
                if (pm == null || pm.Backpack == null || pm.BankBox == null || AlreadyAPrisoner(m))
                    return true;

                // they're walking in (murderer or a guest)
                if (!SentToPrison)
                {
                    byte bags = 0;
                    if (!Teleporter.CheckBank(pm, ref bags))
                    {
                        // count up all the items
                        int count = 0;
                        int equipment = Utility.GetEquippedItems(m).Count;
                        equipment += m.Holding == null ? 0 : 1; // account for items on the cursor
                        int belongings = m.Backpack.GetDeepItems().Count;
                        count = equipment + belongings;

                        // they are alive and walking in. We simply refuse entry at this point
                        m.SendMessage("Your bank box cannot store the {0} items you are trying to bring in.", count);
                        m.SendMessage("Come back when you have emptied your bank box a bit or carrying fewer items.");
                        return true;
                    }
                }

                m_Visitor = HoldingQuestTicket(m);

                // Murders can play at the prison quest like everyone else
                //  However, you cannot use a prison quest ticket to get out (visitor status) if you were sent to prison
                if (m_Visitor && SentToPrison)
                {   // void their ticket
                    m_Visitor = false;
                    DeleteQuestTicket(m);
                }

                if (m.AccessLevel == AccessLevel.Player)
                {   // skip these checks if your were sent to prison via [prison command
                    if (!SentToPrison)
                    {
                        if (m_Visitor)
                        {
                            m.SendMessage("You may enter.");
                        }
                        else if (!pm.PrisonInmate && m_CheckMurders && m.ShortTermMurders < 5)
                        {
                            // We want to make sure we're teleporting a PK and not some random blubie.
                            m.SendMessage("You must have at least five short term murder counts to use this.");
                            return true;
                        }
                    }

                    // dismount player
                    IMount mt = m.Mount;
                    if (mt != null)
                        mt.Rider = null;

                    // drop what they are holding
                    Utility.DropHolding(m);

                    // delete contraband. For now, lighthouse passes count as contraband
                    ArrayList items = new ArrayList(m.Backpack.Items);
                    foreach (Item i in items)
                    {
                        if (i is LightHousePass)
                            i.Delete();
                    }

                    // attempt to move items to bank.
                    if (Teleporter.BankEverything(m) == false)
                    {
                        if (SentToPrison)
                        {   // sorry to say, we're dropping your stuff here
                            Teleporter.DropEverything(m);
                            m.SendMessage("Your items cannot be deposited in your bank box because it is full.");
                            m.SendMessage("So they were lost.");
                        }
                        else
                        {   // they are alive and walking in
                            m.SendMessage("Your items cannot be deposited in your bank box because it is full.");
                            m.SendMessage("Come back when you have emptied your bank box a bit.");
                            return true;
                        }
                    }
                    else
                    {
                        m.SendMessage("Your worldly possessions have been placed in your bank for safekeeping.");
                    }

                    // robe, stinger, spellbook;
                    EquipInmate(m);
                }

                StartTeleport(m);

                if (!m.Alive && m.NetState != null)
                {
                    m.CloseGump(typeof(ResurrectGump));
                    m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
                }

                return false;
            }

            return true;
        }
        private void EquipInmate(Mobile m)
        {
            // Give them a Deathrobe, Stinger dagger, and a blank spell book
            if (m.Alive)
            {
                RemoveDeathRobes(m);
                // Adam, 8/6/21: Hue the death robe orange. kind of a funky orange hue. Look for a better one
                DeathRobe robe = new Server.Items.DeathRobe();
                robe.Hue = 0x2b;
                if (!m.EquipItem(robe))
                    robe.Delete();
            }

            Item aiStinger = new AIStinger();
            if (!m.AddToBackpack(aiStinger))
                aiStinger.Delete();

            Item spellbook = new Spellbook();
            if (!m.AddToBackpack(spellbook))
                spellbook.Delete();
        }
        private bool HasDeathRobe(Mobile m)
        {
            if (m != null && m.Items != null)
                foreach (Item item in m.Items)
                    if (item is DeathRobe)
                        return true;

            Item dr = m.FindItemOnLayer(Layer.OuterTorso);
            if (dr != null && dr is DeathRobe)
                return true;

            return false;
        }
        private void RemoveDeathRobes(Mobile m)
        {
            if (m != null && m.Items != null)
            {
                List<Item> list = new List<Item>(m.Items);
                foreach (Item item in list)
                {
                    if (item is DeathRobe)
                    {
                        if (item.Parent is Mobile parent)
                            parent.RemoveItem(item);
                        else if (item.Parent is Container container)
                            container.RemoveItem(item);

                        item.Delete();
                    }
                }
            }
        }
        public bool HoldingQuestTicket(Mobile m, bool check_bank = false)
        {
            if (m == null) return false;

            Container cont = check_bank ? m.BankBox : m.Backpack;

            if (!check_bank)
                Utility.DropHolding(m);

            // find all of their tickets
            List<Ticket> list = new List<Ticket>();
            if (cont != null)
                foreach (object o in cont.GetDeepItems())
                    if (o is Ticket ticket)
                        list.Add(ticket);

            // see if they have any tickets to this venue
            bool found = false;
            foreach (Ticket ticket in list)
                if (!string.IsNullOrEmpty(ticket.Passcode))
                    if ("F6F23E61".Equals(ticket.Passcode, StringComparison.OrdinalIgnoreCase))
                        if (ticket.Expired == false)
                            found = true;

            // did not have a ticket to this venue
            if (found == false)
            {
                m.SendMessage("You must have a valid ticket to attend this quest, you cannot pass.");
                return false;
            }

            return true;
        }
        public void DeleteQuestTicket(Mobile m, bool check_bank = false)
        {
            if (m == null) return;

            Container cont = check_bank ? m.BankBox : m.Backpack;

            if (!check_bank)
                Utility.DropHolding(m);

            // find all of their tickets
            List<Ticket> list = new List<Ticket>();
            if (cont != null)
                foreach (object o in cont.GetDeepItems())
                    if (o is Ticket ticket)
                        list.Add(ticket);

            // see if they have any tickets to this venue
            bool found = false;
            foreach (Ticket ticket in list)
                if (!string.IsNullOrEmpty(ticket.Passcode))
                    if ("F6F23E61".Equals(ticket.Passcode, StringComparison.OrdinalIgnoreCase))
                        if (ticket.Expired == false)
                        {
                            if (ticket.Parent is Container c)
                                c.RemoveItem(ticket);
                            if (ticket.Parent is Mobile parent)
                                parent.RemoveItem(ticket);

                            ticket.Delete();
                        }

        }
        public AIEntrance(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }
        }
    }

    public class AIParoleExit : Item
    {
        private bool m_Active;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }


        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public AIParoleExit()
            : base(0x1BC3)
        {
            m_Active = true;
            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Parole Exit");
            else
                LabelTo(from, "(inactive)");
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }

        public virtual void DoTeleport(Mobile m)
        {
            if (m_SourceEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            Server.Point3D location = new Point3D(818, 1087, 0);
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);

            if (m.Player && m.AccessLevel == AccessLevel.Player)
            {
                AITeleportHelper.EmptyPackOnExit(m, true);
                ((PlayerMobile)m).PrisonInmate = false;
                ((PlayerMobile)m).PrisonVisitor = false;
                if (!Core.RuleSets.MortalisRules())
                    ((PlayerMobile)m).Mortal = false;
                ((PlayerMobile)m).ResetKillTime();
                ((PlayerMobile)m).MinimumSentence = DateTime.MinValue;
                (m.Account as Accounting.Account).InfractionStatus = Accounting.Account.AccountInfraction.none;
                (m.Account as Accounting.Account).SetFlag(Accounting.Account.AccountFlag.ExceedsMachineInfoLimit, false);
                Server.Engines.Chat.ChatHelper.SetChatBan(((PlayerMobile)m), false);
                UnGuildFromOutcast(((PlayerMobile)m));
                AIPUtils.DecolorizeRobe(m);
                AIPUtils.KillPrisonPets(m);
            }
        }
        private void UnGuildFromOutcast(PlayerMobile pm)
        {
            // remove them from The Outcast guild
            if (pm != null && pm.Guild != null && pm.Guild.Abbreviation.Contains("ToC", StringComparison.OrdinalIgnoreCase))
                ((Guild)pm.Guild).ResignMember(pm, force: true);
        }
        public override bool OnMoveOver(Mobile m)
        {
            // prisoners get out free during server wars.
            bool bServerWars = (Server.Misc.AutoSave.SavesEnabled == false && Server.Misc.AutoRestart.Restarting == true);
            Accounting.Account acct = m.Account as Accounting.Account;

            if (m_Active)
            {
                if (!m.Player || acct == null)
                    return true;

                if (m.AccessLevel == AccessLevel.Player)
                {
                    if (bServerWars == true)
                    {
                        m.SendMessage("During Server Wars all prisoners go free!");
                        return true;
                    }

                    // Make sure they've worked off their counts
                    if (m is PlayerMobile pm)
                    {
                        if (pm.ShortTermCriminalCounts > 0)
                        {
                            // We don't want people leaving on parole if they've not worked off their counts
                            m.SendMessage("You still have {0} criminal counts against you. You must have 0 to be paroled.", (m as PlayerMobile).ShortTermCriminalCounts);
                            return true;
                        }

                        if (DateTime.UtcNow < pm.MinimumSentence)
                        {
                            // We don't want people leaving on parole if they've not served their minimum sentence
                            TimeSpan delta = pm.MinimumSentence - DateTime.UtcNow;
                            int days = delta.Days;
                            int hours = delta.Hours;
                            int minutes = delta.Minutes;
                            m.SendMessage("Your sentence will be up in {0} days, {1} hours, and {2} minutes.", days, hours, minutes);
                            return true;
                        }

                        if (ExceedsIPLimit(pm))
                        {
                            m.SendMessage("You must wait for your other account(s) to logout.");
                            return true;
                        }

                        // You're never leaving prison
                        if (TorExitNode(pm))
                        {
                            m.SendMessage("You entered this shard via an illegal connection type. You will never leave prison.");
                            return true;
                        }
                    }

                    // Make sure they've worked off their counts
                    if (m.ShortTermMurders > 4 && Core.RuleSets.AngelIslandRules())
                    {
                        // We don't want people leaving on parole if they've not worked out of stat-loss
                        m.SendMessage("You must have less than 5 short term murder counts to be paroled.");
                        return true;
                    }

                    if (!m.Alive)
                    {
                        // and we don't want them leaving as a ghost either.
                        m.SendMessage("You must be alive to use this.");
                        return true;
                    }
                }

                StartTeleport(m);
                return false;
            }

            return true;
        }
        public bool ExceedsIPLimit(PlayerMobile pm)
        {
            Accounting.Account acct = pm.Account as Accounting.Account;
            if (acct != null && pm.NetState != null)
            {
                IPAddress ip = pm.NetState.Address;
                return !AccountConcurrentIPLimiter.IsOk(acct, ip);
            }
            return false;
        }
        public bool TorExitNode(PlayerMobile pm)
        {
            Accounting.Account acct = pm.Account as Accounting.Account;
            if (acct != null && pm.NetState != null)
                return acct.InfractionStatus == Accounting.Account.AccountInfraction.TorExitNode || acct.GetFlag(Accounting.Account.IPFlags.IsTorExitNode);
            return false;
        }
        public AIParoleExit(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }
        }
    }

    public class AIEscapeExit : Item
    {
        private bool m_Active;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }


        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public AIEscapeExit()
            : base(0x1BC3)
        {
            m_Active = true;

            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Escape Exit");
            else
                LabelTo(from, "(inactive)");
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }

        public virtual void DoTeleport(Mobile m)
        {
            if (m_SourceEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            Server.Point3D location = new Point3D(5671, 2391, 50);  // lighthouse
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);

            if (m.Player && m.AccessLevel == AccessLevel.Player)
            {
                // will preserve Rare and Smuggled items
                AITeleportHelper.EmptyPackOnExit(m, false);

                // release the lock on global chat
                Server.Engines.Chat.ChatHelper.SetChatBan(((PlayerMobile)m), false);

                // Adam: The following code prohibits the STC from dropping below 5
                //	as per design.
                int oldSTC = ((PlayerMobile)m).ShortTermMurders;
                int newSTC = ((PlayerMobile)m).ShortTermMurders;

                if (((PlayerMobile)m).ShortTermMurders >= 10)
                    newSTC = ((PlayerMobile)m).ShortTermMurders / 2;
                else if (((PlayerMobile)m).ShortTermMurders > 5)
                    newSTC = 5;

                ((PlayerMobile)m).ShortTermMurders = newSTC;

                if (oldSTC - newSTC > 0)
                    ((PlayerMobile)m).SendMessage("Your short term murders have been reduced to {0}", ((PlayerMobile)m).ShortTermMurders);

                ((PlayerMobile)m).PrisonInmate = false;
                ((PlayerMobile)m).PrisonVisitor = false;

                ((PlayerMobile)m).ResetKillTime();

                AIPUtils.DecolorizeRobe(m);

                // drop everything held to backpack
                //AIPUtils.DroptoBackpack(m);

                // drop all but robe in backpack
                AIPUtils.Undress(m);

                AIPUtils.KillPrisonPets(m);

                // leader board baby!
                DataRecorder.RecordPQuestPoints(m, escapes: 1, rares: CountRares(m), sterling: m.Backpack.GetAmount(typeof(Sterling)));
            }
        }

        private int CountRares(Mobile m)
        {
            if (m != null && m.Backpack != null)
            {
                List<Item> list = new(m.Backpack.GetDeepItems().Cast<Item>().ToList());
                if (m.Items != null && list.Count > 0)
                    list.AddRange(m.Items.Cast<Item>().ToList());
                int count = 0;
                foreach (Item item in list)
                    if (item.GetFlag(LootType.Rare))
                        count++;

                return count;
            }
            return 0;
        }
        public override bool OnMoveOver(Mobile m)
        {
            if (m_Active)
            {
                if (!m.Player)
                    return true;

                if (!m.Alive && m.AccessLevel == AccessLevel.Player)
                {
                    // We want to make sure we don't have ghosties camping the escape exit.
                    m.SendMessage("You must be alive to use this.");
                    return true;
                }

                StartTeleport(m);
                return false;
            }

            return true;
        }

        public AIEscapeExit(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }

            //Pix: the AIEscapeExit shouldn't be saved... so we should delete it if
            // it's there on world load.
            System.Console.WriteLine("Deleting AIEscapeExit on world load!");
            this.Delete();
        }
    }

    public class AICaveEntrance : Item
    {
        private bool m_Active;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }


        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public AICaveEntrance()
            : base(0x1BC3)
        {
            m_Active = true;
            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Spirit Spawn");
            else
                LabelTo(from, "(inactive)");
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }

        public virtual void DoTeleport(Mobile m)
        {
            Server.Point3D location = new Point3D(311, 787, -24);

            Server.Mobiles.BaseCreature.TeleportPets(m, location, Map.Felucca);
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);

            // not sure if this is needed
            Utility.DropHolding(m);

            // okay, delete the lighthouse passes
            Container backpack = m.Backpack;
            if (backpack != null)
            {
                Item[] lhpasses = backpack.FindItemsByType(typeof(LightHousePass), true);
                if (lhpasses != null && lhpasses.Length > 0)
                {
                    for (int i = 0; i < lhpasses.Length; i++)
                    {   // 1 in 5000 chance to keep this puppy
                        if (lhpasses[i] is LightHousePass && Utility.Random(5000) != 1)
                            lhpasses[i].Delete();
                    }
                }
            }
        }

        public override bool OnMoveOver(Mobile m)
        {
            bool HasPass = false;

            if (m_Active)
            {
                if (!m.Player)
                    return true;

                if (m.AccessLevel == AccessLevel.Player)
                {
                    if (!m.Alive)
                    {
                        // no ghosts are allowed into the escape route
                        m.SendMessage("You must be alive to use this.");
                        return true;
                    }

                    Container backpack = m.Backpack;
                    if (backpack != null)
                    {
                        Item[] lhpasses = backpack.FindItemsByType(typeof(LightHousePass), true);
                        if (lhpasses != null && lhpasses.Length > 0)
                            HasPass = true;
                    }

                    if (!HasPass)
                    {
                        m.SendMessage("You require a lighthouse pass to go there");
                        return true;
                    }
                    else
                        // we delete it in DoTeleport
                        m.SendMessage("Your lighthouse pass disappears as you're teleported");
                }

                StartTeleport(m);
                return false;
            }

            return true;
        }

        public AICaveEntrance(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }

            //Pix: the AICaveEntrance shouldn't be saved... so we should delete it if
            // it's there on world load.
            System.Console.WriteLine("Deleting AICaveEntrance on world load!");
            this.Delete();
        }
    }

    public class AISpawnEntrance : Item
    {
        private bool m_Active;
        private bool m_SourceEffect;
        private bool m_DestEffect;
        private int m_SoundID;
        private TimeSpan m_Delay;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool SourceEffect
        {
            get { return m_SourceEffect; }
            set { m_SourceEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool DestEffect
        {
            get { return m_DestEffect; }
            set { m_DestEffect = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan Delay
        {
            get { return m_Delay; }
            set { m_Delay = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; InvalidateProperties(); }
        }


        public override int LabelNumber { get { return 1026095; } } // teleporter

        [Constructable]
        public AISpawnEntrance()
            : base(0x1BC3)
        {
            m_Active = true;
            Movable = false;
            Visible = false;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_Active)
                list.Add(1060742); // active
            else
                list.Add(1060743); // inactive
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Active)
                LabelTo(from, "Angel Island Spawn Entrance");
            else
                LabelTo(from, "(inactive)");
        }

        public virtual void StartTeleport(Mobile m)
        {
            if (m_Delay == TimeSpan.Zero)
                DoTeleport(m);
            else
                Timer.DelayCall(m_Delay, new TimerStateCallback(DoTeleport_Callback), m);
        }

        private void DoTeleport_Callback(object state)
        {
            DoTeleport((Mobile)state);
        }
        // We create a an array of destinations so inmates don't all pile up at the same location
        static int location_ndx = 0;
        static Server.Point3D[] locations = { new Point3D(5748, 362, 15), new Point3D(5749, 362, 15), new Point3D(5750, 362, 15), new Point3D(5751, 362, 15) };
        public virtual void DoTeleport(Mobile m)
        {
            Server.Point3D location = locations[location_ndx++]; if (location_ndx == locations.Length) location_ndx = 0;
            m.MoveToWorld(location, Map.Felucca);

            if (m_DestEffect)
                Effects.SendLocationEffect(m.Location, m.Map, 0x3728, 10, 10);

            if (m_SoundID > 0)
                Effects.PlaySound(m.Location, m.Map, m_SoundID);
        }
        public override bool OnMoveOver(Mobile m)
        {
            if (m_Active)
            {
                StartTeleport(m);
                return false;
            }

            return true;
        }
        public AISpawnEntrance(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_SourceEffect);
            writer.Write((bool)m_DestEffect);
            writer.Write((TimeSpan)m_Delay);
            writer.WriteEncodedInt((int)m_SoundID);

            writer.Write(m_Active);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    m_SourceEffect = reader.ReadBool();
                    m_DestEffect = reader.ReadBool();
                    m_Delay = reader.ReadTimeSpan();
                    m_SoundID = reader.ReadEncodedInt();

                    m_Active = reader.ReadBool();

                    break;
                case 2:
                    {
                        m_SourceEffect = reader.ReadBool();
                        m_DestEffect = reader.ReadBool();
                        m_Delay = reader.ReadTimeSpan();
                        m_SoundID = reader.ReadEncodedInt();

                        goto case 1;
                    }
                case 1:
                    {
                        bool m_Creatures = reader.ReadBool();

                        goto case 0;
                    }
                case 0:
                    {
                        m_Active = reader.ReadBool();
                        Point3D m_PointDest = reader.ReadPoint3D();
                        Map m_MapDest = reader.ReadMap();

                        break;
                    }
            }
        }
    }
    public class AITeleportHelper
    {
        /*public static void EmptyPackOnExit(Mobile m, bool ParoleExit)
		{
			EmptyPackOnExit(m, ParoleExit);
		}*/

        public static void EmptyPackOnExit(Mobile m, bool ParoleExit)
        {

            Container backpack = m.Backpack;
            if (backpack == null)
                return;

            // drop everything held to backpack
            AIPUtils.DroptoBackpack(m);

            // process items as follows
            //	delete everything but stinger, and rares, and sometimes LightHousePass (explained below)
            //	change stinger and lighthouse pass to 'Rare' loot
            //	Keep all rares acquired on In Prison
            //	If you escape, you get to keep your LightHousePass, it is now LootType.Rare. However, they cannot 
            //		be brought back in. If you leave via the parole exit, no pass for you!
            //	delete spellbook which was given. players original spell book was placed into bank.
            // we don't delete EVERYTHING because we may allow some items to be found here: LootType.Rare, LootType.Smuggled
            ArrayList stuff = backpack.FindAllItems();
            if (stuff != null && stuff.Count > 0)
            {
                // mark some stuff as LootType Rare/Smuggled
                for (int ix = 0; ix < stuff.Count; ix++)
                {
                    Item item = stuff[ix] as Item;

                    // no lighthouse pass rare for those that just wait out their time
                    if (item is LightHousePass && ParoleExit != true)
                        item.LootType = LootType.Smuggled;
                }

                // Sterling is always Smuggled
                for (int ix = 0; ix < stuff.Count; ix++)
                    if (stuff[ix] is Sterling sterling)
                        sterling.LootType = LootType.Smuggled;

                // what we can take out and what we cannot
                for (int ix = 0; ix < stuff.Count; ix++)
                {
                    Item item = stuff[ix] as Item;

                    // don't delete the containers
                    if (item is Container)
                        continue;

                    // delete everything else but Rare and Smuggled items, unless they are leaving via the parole exit
                    if (ParoleExit == true)
                        item.Delete();
                    else if (item.LootType != LootType.Rare && item.LootType != LootType.Smuggled)
                        item.Delete();
                }

                if (ParoleExit == false)
                {   // give them a souvenir of Angel Island
                    backpack.AddItem(AISouvenir());
                }
            }

            return;
        }

        public static Dagger AISouvenir()
        {
            Dagger dagger = new Dagger();
            dagger.LootType = LootType.Rare;
            dagger.Name = "I survived Angel Island";
            return dagger;
        }
    }
}