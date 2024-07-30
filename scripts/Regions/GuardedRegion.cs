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

/* Scripts/Regions/GuardedRegion.cs
 * ChangeLog
 *  5/7/2024, Adam (DidAnybodySeeThis)
 *      1. Make the option for vendors to see through walls default off. Now relies on LOS only
 *      2. Like MoonGate wizards, hirelings do not call guards
 *  1/15/24, Yoar
 *      Reds may bow buy from vendors in unguarded regions (e.g. townships)
 *  6/28/2023, Adam
 *      Add missing base.OnExit(m) from Guarded Region OnExit.
 *      Problem: Regions were piling up all the mobiles that entered, but they would never leave.
 *      Kinda like Hotel California. 
 *  5/25/2003, Adam
 *      Criminals are only guard whackable for 10 seconds.
 *      https://web.archive.org/web/20020806202758/http://uo.stratics.com/content/reputation/flags.shtml
 *  5/3/23, Adam (RedsBuyRule)
 *      Reds can make purchases in Buc's Den
 *      House of Commons from 2002-12-11
 *      Lord?Xanthor* Tarnipuss*Will reds be able to use item insurance? And if so how as they currently only have access to 1 town - Buc's Den (that is assuming it is something that must be purchased from a town)
 *      https://wiki.stratics.com/index.php?title=UO:House_of_Commons_from_2002-12-11
 *  12/8/22, Adam (OnGotBenificialAction) Healing reds [Siege]
 *      Healing a red character is a criminal offense, but will not get you guard whacked.
 *      https://www.uoguide.com/Siege_Perilous
 *      Note: I Put a check in there that if this is siege, we do not call CheckGuardCandidate() if you are healing a red in town.
 *      However, it IS a criminal act, and while it's not an instant guard-whack (CheckGuardCandidate), you will
 *          get whacked if an NPC (or player) is nearby to "call guards!"
 *  9/15/22, Yoar
 *      Implemented Serialize, Deserialize
 * 3/19/22, Adam (OnSpeech)
 *      Adam: we need base class processing to allow region speech to make it's way to registered items.
 * 1/18/22, Adam (MakeGuards)
 *      When we set: bc.LoyaltyValue = PetLoyalty.Confused;
 *      We now also: bc.LoyaltyCheck = DateTime.UtcNow + TimeSpan.FromHours(1.0);
 * 1/8/22, Adam (public record Witness)
 *      Change public class Witness to public record Witness
 *      more appropriate use of the C# construct
 * 1/6/22, Adam (DidAnybodySeeThis/Witnesses)
 *      Improved the reliability/believability of NPCs reporting a crime by ordering the Witnesses by LOS and distance.
 * 8/21/21, Adam (IsGuardCandidate() now calls GuardAI.GuardedRegionEvilMonsterRule()) 
 * 	Guards now dispatch monsters when they are in a guarded region, they are not controlled, and they have an aggressive AI. 
 *      We accomplish this by expanding IsGuardCandidate() to include the GuardAI.GuardedRegionEvilMonsterRule()
 *  7/30/2021, adam
 *      Staff are now guard whackable if not blessed. this makes both testing easier and can imporve events (for evil staff characters.)
 *	2/15/11, Adam
 *		Reds are now allowed in town UOMO
 *	2/12/11, Adam
 *		Issue the message to reds "Guards can no longer be called on you." if reds are allowed in town
 *	2/10/11, Adam
 *		Update IsGuardCandidate to allow for reds in town (UOSP)
 *	6/27/10, Adam
 *		Add the notion of Smart Guards to defend against in-town griefing (exp pots, and melee attacks)
 *	6/21/10, adam
 *		In CheckGuardCandidate() we now check to see if the mobile 'remembers' the criminal.
 *		The old code used to simply enumerate the nearby mobiles to see if one was in range. If one was in range, it is assumed they were seen.
 *		The new code checks to see if the mobile actiually saw the player .. this allows guard whacks at WBB for players letting loose spells 
 *		while hidden while at the same time allowing reds to stealth and hidden recall into town (the NPCs will not have seen them.)
 *	07/23/08, weaver
 *		Added Free() before return in IPooledEnumerable loop.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 3 loops updated.
 *	9/1/07, Adam
 *		change [SetGuarded from AccessLevel Administrator to Seer
 *  03/31/06 Taran Kain
 *		Changed GuardTimer, CallGuards to only display "Guards cannot be called on you" if mobile is not red
 *  04/23/05, erlein
 *    Changed ToggleGuarded command to Seer level access.
 *	04/19/05, Kit
 *		Added check to IsGuardCandidate( ) to not make hidden players canadites.
 *	10/28/04, Pix
 *		In CheckGuardCandidate() ruled out the case where a player can recall to a guardzone before
 *		explosion hits and the person that casts explosion gets guardwhacked.
 *	8/12/04, mith
 *		IsGuardCandidate(): Modified to that player vendors will not call guards.
 *  6/21/04, Old Salty
 *  	Added a little code to CallGuards to close the bankbox of a criminal when the guards come
 *  6/20/04, Old Salty
 * 		Fixed IsGuardCandidate so that guards react properly 
 * 
 *	6/10/04, mith
 *		Modified to work with the new non-insta-kill guards.
 */

using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Server.Regions
{
    public class GuardedRegion : Region
    {
        private static object[] m_GuardParams = new object[1];
        private Type m_GuardType;

        public override bool IsGuarded { get { return false; } set {; } }
        public override bool IsSmartGuards { get { return false; } set {; } }

        public new static void Initialize()
        {
            CommandSystem.Register("CheckGuarded", AccessLevel.GameMaster, new CommandEventHandler(CheckGuarded_OnCommand));
            CommandSystem.Register("SetGuarded", AccessLevel.Seer, new CommandEventHandler(SetGuarded_OnCommand));
            CommandSystem.Register("ToggleGuarded", AccessLevel.Seer, new CommandEventHandler(ToggleGuarded_OnCommand));
        }

        [Usage("CheckGuarded")]
        [Description("Returns a value indicating if the current region is guarded or not.")]
        private static void CheckGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            GuardedRegion reg = from.Region as GuardedRegion;

            if (reg == null)
                from.SendMessage("You are not in a guardable region.");
            else if (reg.IsGuarded == false)
                from.SendMessage("The guards in this region have been disabled.");
            else
                from.SendMessage("This region is actively guarded.");
        }

        [Usage("SetGuarded <true|false>")]
        [Description("Enables or disables guards for the current region.")]
        private static void SetGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (e.Length == 1)
            {
                GuardedRegion reg = from.Region as GuardedRegion;

                if (reg == null)
                {
                    from.SendMessage("You are not in a guardable region.");
                }
                else
                {
                    reg.IsGuarded = e.GetBoolean(0);

                    if (reg.IsGuarded == false)
                        from.SendMessage("The guards in this region have been disabled.");
                    else
                        from.SendMessage("The guards in this region have been enabled.");
                }
            }
            else
            {
                from.SendMessage("Format: SetGuarded <true|false>");
            }
        }

        [Usage("ToggleGuarded")]
        [Description("Toggles the state of guards for the current region.")]
        private static void ToggleGuarded_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            GuardedRegion reg = from.Region as GuardedRegion;

            if (reg == null)
            {
                from.SendMessage("You are not in a guardable region.");
            }
            else
            {
                reg.IsGuarded = !reg.IsGuarded;

                if (reg.IsGuarded == false)
                    from.SendMessage("The guards in this region have been disabled.");
                else
                    from.SendMessage("The guards in this region have been enabled.");
            }
        }

        public virtual bool CheckVendorAccess(BaseVendor vendor, Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
                return true;

            return (from.LongTermMurders < 5 || RedsBuyRule());
        }

        // I think this is valid for all shards
        private bool RedsBuyRule()
        {   // House of Commons from 2002-12-11
            // Lord?Xanthor* Tarnipuss*Will reds be able to use item insurance? And if so how as they currently only have access to 1 town - Buc's Den (that is assuming it is something that must be purchased from a town)
            // 1/15/24, Yoar: Reds may bow buy from vendors in unguarded regions (e.g. townships)
            // https://wiki.stratics.com/index.php?title=UO:House_of_Commons_from_2002-12-11
            return (!IsGuarded || IsPartOf("Buccaneer's Den"));
        }

        public GuardedRegion(string prefix, string name, Map map, Type guardType)
            : base(prefix, name, map)
        {
            m_GuardType = guardType;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            int baseVersion = PeekInt(reader);

            base.Deserialize(reader);

            if (baseVersion == 0)
                return; // we have no version

            int version = reader.ReadInt();
        }

        private static int PeekInt(GenericReader reader)
        {
            int result = reader.ReadInt();
            reader.Seek(-4, System.IO.SeekOrigin.Current);
            return result;
        }

        public override bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            if (IsGuarded && !s.OnCastInTown(this))
            {
                m.SendLocalizedMessage(500946); // You cannot cast this in town!
                return false;
            }

            return base.OnBeginSpellCast(m, s);
        }

        public override bool AllowHousing(Point3D p)
        {
            return false;
        }

        public void MakeGuards(Mobile focus)
        {
            // also generate guards for the criminals pets
            ArrayList pets = new ArrayList();
            foreach (Mobile m in World.Mobiles.Values)
            {
                BaseCreature bc = m as BaseCreature;
                if (bc != null && bc.Controlled && bc.ControlMaster == focus && bc.ControlOrder == OrderType.Attack)
                {   // okay, looks bad for this pet...
                    if (bc.Region == focus.Region && bc.Map == focus.Map)
                    {   // okay, that's good enough for me.
                        pets.Add(bc);
                    }
                }
                else if (bc != null && bc.Controlled && bc.ControlMaster == focus && bc.ControlOrder != OrderType.Attack)
                {   // Siege has insta death, so it's not applicable
                    if (Core.RuleSets.InstaDeathGuards())
                    {   // it's possible to command the rest of your pets to 'all kill' after the guards have been called.
                        //	the protect from this case, we will reduce all your pets Loyalty to almost nothing.
                        bc.LoyaltyValue = PetLoyalty.Confused;
                        // restart the clock
                        bc.LoyaltyCheck = DateTime.UtcNow + TimeSpan.FromHours(1.0);
                        focus.SendMessage(string.Format("{0} is greatly confused by what's going on.", bc.Name));
                    }
                }
            }

            for (int ix = 0; ix < pets.Count; ix++)
                MakeGuard(pets[ix] as Mobile);

            MakeGuard(focus);
        }

        public override void MakeGuard(Mobile focus)
        {
            BaseGuard useGuard = null;

            IPooledEnumerable eable = focus.GetMobilesInRange(8);
            foreach (Mobile m in eable)
            {
                if (m is BaseGuard)
                {
                    BaseGuard g = (BaseGuard)m;

                    if (g.Focus == null) // idling
                    {
                        useGuard = g;
                        break;
                    }
                }
            }
            eable.Free();

            if (useGuard != null)
            {
                useGuard.Focus = focus;
            }
            else
            {
                m_GuardParams[0] = focus;

                Activator.CreateInstance(m_GuardType, m_GuardParams);
            }
        }

        public override void OnEnter(Mobile m)
        {
            base.OnEnter(m);
            if (IsGuarded == false)
            { return; }

            //m.SendLocalizedMessage( 500112 ); // You are now under the protection of the town guards.

            /* Seems to be over kill
             * Let the town's people call guards if they see him.
            if (m.Murderer)
                CheckGuardCandidate(m, m.Player);

            if (GuardAI.GuardedRegionEvilMonsterRule(null, this, m))
                CheckGuardCandidate(m, m.Player); 
            */
        }

        public override void OnExit(Mobile m)
        {
            base.OnExit(m);
            if (IsGuarded == false)
                return;

            //m.SendLocalizedMessage( 500113 ); // You have left the protection of the town guards.
        }

        public override void OnSpeech(SpeechEventArgs args)
        {
            // 3/19/2022, Adam: we need base class processing to allow region speech to make it's way to registered items.
            base.OnSpeech(args);

            if (IsGuarded == false)
                return;

            if (!PetGuard.Match(args.Speech).Success)
                if (args.Mobile.Alive && args.HasKeyword(0x0007)) // *guards*
                    CallGuards(args.Mobile.Location);
        }
        // ignore players telling their pets to guard them. "all guard me" should not call guards
        private static Regex PetGuard = new Regex(@"^[a-zA-Z]+\sguard\s[a-zA-Z]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override void OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)
        {
            base.OnAggressed(aggressor, aggressed, criminal);

            if (IsGuarded && aggressor != aggressed && criminal)
                CheckGuardCandidate(aggressor, aggressor.Player);
        }

        public override void OnGotBenificialAction(Mobile helper, Mobile helped)
        {
            base.OnGotBenificialAction(helper, helped);

            if (IsGuarded == false)
                return;

            int noto = Notoriety.Compute(helper, helped);
            // Healing a red character is a criminal offence, but will not get you guard whacked.
            //  https://www.uoguide.com/Siege_Perilous
            //  (Adam) However, it IS a criminal act, and while it's not an instant guard-whack (CheckGuardCandidate), you will
            //      get whacked if an NPC (or player) is nearby to "call guards!"
            if (helper != helped && (noto == Notoriety.Criminal || (!Core.RuleSets.SiegeStyleRules() && noto == Notoriety.Murderer)))
                CheckGuardCandidate(helper, helper.Player);
        }

        public override void OnCriminalAction(Mobile m, bool message)
        {
            base.OnCriminalAction(m, message);

            if (IsGuarded)
                CheckGuardCandidate(m, m.Player);
        }

        private Hashtable m_GuardCandidates = new Hashtable();

        public Mobile DidAnybodySeeThis(Mobile suspect, bool canSeeThroughWalls = false)
        {
            Mobile fakeCall = null;
            // about 1 screen. we can't use BaseCreature's RangePerception, since things like vendors have a RP of like 2
            int rangePerception = 13;
            // 8.06 tiles if you don't have LOS (maybe they're looking through a window)
            // 5/7/2024, Adam: Make the default 'can't see through walls' due to player complaints
            double perceptionPenalty = canSeeThroughWalls ? rangePerception * .62 : 0;
            bool MurdererPenalty = suspect.Red && !Core.RedsInTown;

            List<Witness> witnesses = Witnesses(suspect);
            foreach (Witness witness in witnesses)
            {
                // it was requested by players that MoonGate Wizards do not call guards.
                // in the case of Hire Fighters etc., we don't call guards (on our master especially)
                if (witness.Mobile is BaseCreature bc && bc.CallsGuards == false)
                {
                    // Emote here is no good in the case of thieves, it tips nearby players off
                    witness.Mobile.SayTo(suspect, ascii: true, "*looks the other way*");
                    continue;
                }

                bool hasLOSOrCloseEnough = (witness.HasLOS || witness.DistanceToSqrt <= perceptionPenalty);
                bool atSceneOfTheCrime = witness.Mobile.GetDistanceToSqrt(suspect.SceneOfTheCrime) <= (witness.HasLOS ? rangePerception : perceptionPenalty);
                // we got a murderer here!
                //  We don't care about the SceneOfTheCrime, they're a murderer!
                if (MurdererPenalty && hasLOSOrCloseEnough)
                {
                    fakeCall = witness.Mobile;
                    break;
                }
                // we got a criminal here!
                // Adam, we don't care how close they are now to the criminal, but rather how close they were to the crime
                //  townspeople should not be reporting things they didn't see in the first place.
                if (!MurdererPenalty && hasLOSOrCloseEnough && atSceneOfTheCrime)
                {
                    fakeCall = witness.Mobile;
                    break;
                }
            }

            return fakeCall;
        }
        public record Witness
        {
            public Mobile Mobile;
            public bool HasLOS;
            public double DistanceToSqrt;
            public Witness(Mobile m, bool hasLOS, double distanceToSqrt)
            {
                Mobile = m;
                HasLOS = hasLOS;
                DistanceToSqrt = distanceToSqrt;
            }
        }
        private List<Witness> Witnesses(Mobile suspect)
        {
            int RangePerception = 13;                                   // about 1 screen. we can't use (v as BaseCreature).RangePerception,
                                                                        // since things like vendors have a RP of like 2
            List<Witness> witnesses = new List<Witness>();
            IPooledEnumerable eable = suspect.GetMobilesInRange(RangePerception);
            foreach (Mobile witness in eable)
            {
                // make sure the towns person remembers the mobile in question
                //  by remembering, we simply mean that they have recently 'seen' them
                if (!witness.Player && witness.Body.IsHuman && witness != suspect && !IsGuardCandidate(witness) && witness.Remembers(suspect))
                {
                    //Pixie 10/28/04: checking whether v is in the region fixes the problem
                    // where player1 recalls to a guardzone before player2's explosion hits ...
                    if (this.Contains(witness.Location))
                    {
                        witnesses.Add(new Witness(witness, witness.InLOS(suspect), witness.GetDistanceToSqrt(suspect)));
                    }
                }
            }
            eable.Free();

            List<Witness> sorted = witnesses
                .OrderByDescending(x => x.HasLOS)
                .ThenBy(x => x.DistanceToSqrt)
                .ToList();

            //List<Witness> sorted = witnesses.OrderBy(x => x.m_hasLOS).ThenBy(x => x.m_distanceToSqrt).ToList();

            return sorted;
        }
        public void FakeCall(Mobile m, Mobile fakeCall)
        {
            if (fakeCall != null)
            {
                fakeCall.Say(Utility.RandomList(1007037, 501603, 1013037, 1013038, 1013039, 1013041, 1013042, 1013043, 1013052));
                MakeGuards(m);
            }
        }
        public void CheckGuardCandidate(Mobile m, bool player)
        {
            if (IsGuarded == false)
                return;
            if (m.Map != null)
                if (IsGuardCandidate(m))
                {
                    GuardTimer timer = (GuardTimer)m_GuardCandidates[m];

                    if (timer == null)
                    {
                        timer = new GuardTimer(m, m_GuardCandidates);
                        timer.Start();

                        m_GuardCandidates[m] = timer;
                        //m.SendLocalizedMessage(502275); // Guards can now be called on you!

                        // okay, look for a nearby mobile that may have seen the crime .. no complaint, no investigation!
                        Mobile fakeCall = DidAnybodySeeThis(m);     // did anybody see this?
                        if (fakeCall != null)
                        {
                            FakeCall(m, fakeCall);                      // if so, call the guards!
                            m_GuardCandidates.Remove(m);            // guards have been called
                        }
                        //m.SendLocalizedMessage(502276);           // Guards can no longer be called on you.
                    }
                    else
                    {
                        timer.Stop();
                        timer.Start();
                    }
                }
        }

        public void CallGuards(Point3D p)
        {
            if (IsGuarded == false)
                return;

            IPooledEnumerable eable = Map.GetMobilesInRange(p, 14);

            foreach (Mobile m in eable)
            {
                // for guarded region events that contain evil, we set GuardIgnore
                //  this allows for non-PK events in town
                if (m is BaseCreature bc)
                    if (bc.GuardIgnore)
                        continue;

                // we already check m.Murderer in IsGuardCandidate, this check is redundant and breaks the reds_in_town rule.
                //if (IsGuardCandidate(m) && ((/*m.Murderer &&*/ Mobiles.ContainsKey(m.Serial)) || m_GuardCandidates.Contains(m)))
                if (IsGuardCandidate(m) || m_GuardCandidates.Contains(m))
                {
                    if (m.BankBox != null) // Old Salty - Added to close the bankbox of a criminal on GuardCall
                        m.BankBox.Close();

                    MakeGuards(m);
                    m_GuardCandidates.Remove(m);
                    //m.SendLocalizedMessage(502276); // Guards can no longer be called on you.

                    break;
                }
            }

            eable.Free();
        }

        public bool AlreadyGuardCandidate(Mobile m)
        {
            // first check if he is already a guard Candidate
            IPooledEnumerable eable = m.GetMobilesInRange(10);
            foreach (Mobile check in eable)
            {
                BaseGuard guard = check as BaseGuard;
                if (guard != null && guard.Focus == m)
                {   // guards are already on him
                    eable.Free();
                    return true;
                }
            }
            eable.Free();

            return false;
        }
        public bool IsGuardCandidate(Mobile m)
        {
            // 7/30/2021, Adam: Staff are now guard whackable if not blessed. this makes both testing easier and can improve events (for evil staff characters.)
            if (m is BaseGuard || m is PlayerVendor || !m.Alive /*|| m.AccessLevel > AccessLevel.Player*/ || m.Blessed || !IsGuarded)
                return false;

            // first check if he is already a guard Candidate
            if (AlreadyGuardCandidate(m))
                return false;

            // 5/25/2003, Adam: Criminals are only guard whackable for 10 seconds.
            // https://web.archive.org/web/20020806202758/http://uo.stratics.com/content/reputation/flags.shtml
            if ((m.Criminal && m.IsGuardWackable) || GuardAI.GuardedRegionEvilMonsterRule(null, this, m))
                return true;

            // 8/10/22, Adam: Core.RedsInTown handled. 
            if (m.Red)
                if (Core.RedsInTown)
                    return false;
                else
                    return true;

            return false;
        }

        private class GuardTimer : Timer
        {
            private Mobile m_Mobile;
            private Hashtable m_Table;

            public GuardTimer(Mobile m, Hashtable table)
                : base(TimeSpan.FromSeconds(15.0))
            {
                Priority = TimerPriority.TwoFiftyMS;

                m_Mobile = m;
                m_Table = table;
            }

            protected override void OnTick()
            {
                if (m_Table.Contains(m_Mobile))
                {
                    m_Table.Remove(m_Mobile);
                    // Adam: I'm moving this logic to criminality begin/end
                    //if (m_Mobile.LongTermMurders < 5)
                    //m_Mobile.SendLocalizedMessage(502276); // Guards can no longer be called on you.
                }
            }
        }
    }
}