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

/* scripts\Engines\AngelIsland\Mobiles\AIWarden.cs
 * ChangeLog
 *  1/2/23, Adam
 *		First time checkin
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
    public class AIWarden : BaseCreature
    {

        private Memory m_PlayerMemory = new Memory();       // memory used to remember if we a saw a player in the area
        const int MemoryTime = 60;          // how long (seconds) we remember this player

        [Constructable]
        public AIWarden()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            SpeechHue = Utility.RandomSpeechHue();
            Title = "the warden";
            Hue = Utility.RandomSkinHue();

            SetStr(96, 115);
            SetDex(86, 105);
            SetInt(51, 65);

            SetDamage(23, 27);

            SetSkill(SkillName.Wrestling, 90.0, 92.5);
            SetSkill(SkillName.Tactics, 90.0, 92.5);
            SetSkill(SkillName.MagicResist, 97.5, 99.9);

            InitBody();
            InitOutfit();

            Fame = 1000;
            Karma = -1000;

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);

            Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(LookAround), new object[] { null });
        }
        public override bool ClickTitle { get { return false; } }
        public override bool AlwaysMurderer { get { return false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanRummageCorpses { get { return false; } }
        public override bool CanBandage { get { return true; } }
        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, this.RangePerception))
                return true;

            return base.HandlesOnSpeech(from);
        }
        public override TimeSpan BandageDelay { get { return Core.RuleSets.AngelIslandRules() ? TimeSpan.FromSeconds(Utility.RandomMinMax(10, 13)) : base.BandageDelay; } }

        public AIWarden(Serial serial)
            : base(serial)
        {
            Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(LookAround), new object[] { null });
        }
        public override void InitBody()
        {
            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            AddItem(new LongPants(0x322), LootType.Newbied);
            AddItem(new Shoes(Utility.GetRandomHue()));
            AddItem(new FancyShirt(0x47E), LootType.Newbied);   // don't want these getting out into the world.
            AddItem(new GoldRing());
            AddItem(new FloppyHat(Utility.GetRandomHue()));
            Runebook runebook = new Runebook();
            runebook.Hue = Utility.RandomNondyedHue();
            runebook.Name = "Inmates";
            AddItem(runebook, LootType.Newbied);
        }
        public override void GenerateLoot()
        {
            // bupkis
        }
        public void LookAround(object o)
        {
            if (Map == null || Map == Map.Internal)
            {
                Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(LookAround), new object[] { null });
                return;
            }

            IPooledEnumerable eable = this.GetMobilesInRange(this.RangePerception);
            foreach (Mobile m in eable)
            {
                if (m is PlayerMobile)
                    FoundPlayer(m);
            }
            eable.Free();

            Timer.DelayCall(TimeSpan.FromSeconds(2), new TimerStateCallback(LookAround), new object[] { null });
        }
        public void FoundPlayer(Mobile m)
        {
            try
            {
                // yeah
                if (m is PlayerMobile == false)
                    return;

                // put him in the queue
                if (!ConvoQueue.Contains(m))
                    ConvoQueue.Add(m);

                // tidy the list
                List<Mobile> cleanup = new(ConvoQueue);
                foreach (Mobile mx in cleanup)
                {
                    // sanity
                    if (mx.Deleted || mx.Hidden || !mx.Alive || !this.CanSee(mx))
                        ConvoQueue.Remove(mx);

                    // too far away
                    double distance = this.GetDistanceToSqrt(mx);
                    if (distance > this.RangePerception || mx.Region != this.Region)
                        ConvoQueue.Remove(mx);

                    // we ignore players we have talked to recently
                    if (!m_PlayerMemory.Recall(m) == false)
                        ConvoQueue.Remove(mx);
                }

                if (Combatant == null && ConvoMob == null && ConvoQueue.Count > 0)
                {
                    ConvoMob = ConvoQueue[0];
                    ConvoQueue.RemoveAt(0);
                    // messages were muted in PlayerMobile when they were sent to prison to prevent
                    //  all the login blather. The Warden has things to say!
                    if ((ConvoMob as PlayerMobile).MuteMessages == true)
                        (ConvoMob as PlayerMobile).MuteMessages = false;
                    m_PlayerMemory.Remember(m, TimeSpan.FromSeconds(MemoryTime).TotalSeconds);   // remember him for this long
                    SpeechHue = Utility.RandomSpeechHue();
                    Direction = GetDirectionTo(ConvoMob);   // face the player you see
                    Timer.DelayCall(TimeSpan.FromSeconds(1.5), new TimerStateCallback(Welcome), new object[] { 0 });
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        private Mobile ConvoMob = null;
        private List<Mobile> ConvoQueue = new List<Mobile>();
        private void Welcome(object state)
        {
            object[] aState = (object[])state;
            int message = (int)aState[0];
            PlayerMobile pm = ConvoMob as PlayerMobile;
            double delay = 2.5;
            try
            {
                if (Combatant == null && !Unavailable(pm))
                {
                    Accounting.Account acct = pm.Account as Accounting.Account;
                    if (acct != null && acct.InfractionStatus != Accounting.Account.AccountInfraction.none)
                    {
                        int max_accounts = acct.InfractionStatus == Accounting.Account.AccountInfraction.totalIPLimiter ? CoreAI.MaxAccountsPerIP : CoreAI.MaxAccountsPerMachine;
                        switch (acct.InfractionStatus)
                        {
                            default:
                                // we won't see these, handled elsewhere
                                ConvoMob = null;
                                break;
#if false
                            case Accounting.Account.AccountInfraction.fastwalk:
                                switch (message)
                                {
                                    default:
                                        //  we're done
                                        ConvoMob = null;
                                        return;
                                    case 0:
                                        Say("Welcome to Angel Island Prison {0}.", pm.Name);
                                        break;
                                    case 1:
                                        Say("You were caught 'fastwalking' on this server.");
                                        break;
                                    case 2:
                                        if (Core.RuleSets.TestCenterRules())
                                            Say("Luckily, this is test center, see the parole officer to exit.");
                                        break;
                                    case 3:
                                        TimeSpan delta = pm.MinimumSentence - DateTime.UtcNow;
                                        int days = delta.Days; int hours = delta.Hours; int minutes = delta.Minutes;
                                        days = Math.Max(0, days); hours = Math.Max(0, hours); minutes = Math.Max(0, minutes);
                                        Say("Your sentence will be {0} days, {1} hours, and {2} minutes.", days, hours, minutes);
                                        break;
                                    case 4:
                                        Say("While you're here, look around. You'll find some interesting books to read.");
                                        break;
                                    case 5:
                                        Say("Stay out of trouble and have a nice day.");
                                        break;
                                }
                                break;
#endif
                            case Accounting.Account.AccountInfraction.totalIPLimiter:
                            case Accounting.Account.AccountInfraction.totalHardwareLimiter:
                                switch (message)
                                {
                                    default:
                                        //  we're done
                                        ConvoMob = null;
                                        return;
                                    case 0:
                                        Say("Welcome to Angel Island Prison {0}.", pm.Name);
                                        break;
                                    case 1:
                                        Say("You may not have more than {0} account(s) on this server.", max_accounts);
                                        break;
                                    case 2:
                                        Say("Therefore, you are now mortal. Death will be permanent.");
                                        break;
                                    case 3:
                                        TimeSpan delta = pm.MinimumSentence - DateTime.UtcNow;
                                        int days = delta.Days;
                                        int hours = delta.Hours;
                                        int minutes = delta.Minutes;
                                        Say("Your sentence will be {0} days, {1} hours, and {2} minutes.", days, hours, minutes);
                                        break;
                                    case 4:
                                        SayTo(pm, "You may legally login with one of the following accounts.");
                                        foreach (var s in AcctNameList(acct))
                                            SayTo(pm, s);
                                        break;
                                    case 5:
                                        Say("You'll also find some interesting books to read.");
                                        break;
                                    case 6:
                                        Say("Stay out of trouble and have a nice day.");
                                        break;
                                }
                                break;
                            case Accounting.Account.AccountInfraction.concurrentIPLimiter:
                                switch (message)
                                {
                                    default:
                                        //  we're done
                                        ConvoMob = null;
                                        return;
                                    case 0:
                                        Say("Welcome to Angel Island Prison {0}.", pm.Name);
                                        break;
                                    case 1:
                                        Say("You may not have more than {0} connections(s) on this server.", CoreAI.MaxConcurrentAddresses);
                                        break;
                                    case 2:
                                        Say("You will be able to leave this place once your other account(s) logout.");
                                        break;
                                    case 3:
                                        Say("While you're here, you'll find some interesting books to read.");
                                        break;
                                    case 4:
                                        Say("Stay out of trouble and have a nice day.");
                                        break;
                                }
                                break;

                            case Accounting.Account.AccountInfraction.TorExitNode:
                                switch (message)
                                {
                                    default:
                                        //  we're done
                                        ConvoMob = null;
                                        return;
                                    case 0:
                                        Say("Welcome to Angel Island Prison {0}.", pm.Name);
                                        break;
                                    case 1:
                                        Say("An illegal connection type was detected.");
                                        break;
                                    case 2:
                                        Say("Your prison sentence is indefinite.");
                                        break;
                                    case 3:
                                        Say("You may petition for release with staff.");
                                        break;
                                    case 4:
                                        Say("While you're here, you'll find some interesting books to read.");
                                        break;
                                    case 5:
                                        Say("Stay out of trouble and have a nice day.");
                                        break;
                                }
                                break;
                        }
                    }
                    else if (pm.PrisonVisitor)
                    {
                        switch (message)
                        {
                            default:
                                //  we're done
                                ConvoMob = null;
                                return;
                            case 0:
                                Say("Welcome to Angel Island Prison {0}.", pm.Name);
                                break;
                            case 1:
                                Say("Your quest begins now!");
                                break;
                            case 2:
                                Say("You'll find supplies in your cell.");
                                break;
                            case 3:
                                Say("You'll also find some interesting books to read.");
                                break;
                            case 4:
                                Say("Other's have made it out and lived to tell the tale.");
                                break;
                            case 5:
                                Say("Do you have what it takes?");
                                break;
                            case 6:
                                Say("Enjoy your adventure!");
                                break;
                        }
                    }
                    else
                    {
                        switch (message)
                        {
                            default:
                                //  we're done
                                ConvoMob = null;
                                return;
                            case 0:
                                Say("Welcome to Angel Island Prison {0}.", pm.Name);
                                break;
                            case 1:
                                Say("You'll find supplies in your cell.");
                                break;
                            case 2:
                                Say("You'll also find some interesting books to read.");
                                break;
                            case 3:
                                Say("Stay out of trouble and have a nice day.");
                                break;
                        }
                    }
                }
                else
                {
                    ConvoMob = null;
                    return;   // stop talking if we get in a fight or the player moves away
                }

                // more messages await
                Timer.DelayCall(TimeSpan.FromSeconds(delay), new TimerStateCallback(Welcome), new object[] { ++message });
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        private List<string> AcctNameList(Account acct)
        {
            // get a list of accounts associated with this machine ascending sorted by date created that match the acct in question
            List<Account> list_by_machine = Misc.AccountHardwareLimiter.GetAccountsByMachine(acct);

            // trim it to the first N accounts (N = MaxAccountsPerMachine)
            // Accounts after MaxAccountsPerMachine were likely created without machine info,
            //  then the account's machine info was updated sometime after creation
            list_by_machine = list_by_machine.Skip(0).Take(CoreAI.MaxAccountsPerMachine).ToList();

            List<string> nameList = new();
            foreach (Account account in list_by_machine)
                nameList.Add(account.Username);

            NetState ns = NetState;
            if (ns != null)
            {
                List<Accounting.Account> list_by_ip = Misc.AccountTotalIPLimiter.GetAccountsByIP(acct, ns.Address);

                // trim it to the first N accounts (N = MaxAccountsPerIP)
                list_by_ip = list_by_ip.Skip(0).Take(CoreAI.MaxAccountsPerIP).ToList();

                foreach (Account account in list_by_ip)
                    if (!nameList.Contains(account.Username))
                        nameList.Add(account.Username);
            }

            return nameList;
        }
        private bool Unavailable(Mobile m)
        {   // yeah
            if (m.Deleted || m.Hidden || !m.Alive || !this.CanSee(m))
                return true;
            // too far away
            double distance = this.GetDistanceToSqrt(m);
            if (distance > this.RangePerception || m.Region != this.Region)
                return true;
            return false;
        }
        private Memory m_aggressionMemory = new Memory();       // memory used to remember if we a saw a player in the area
        const int AggressionMemoryTime = 120;                    // how long (seconds) we remember this player
        public override void OnHarmfulAction(Mobile aggressor, bool isCriminal, object source = null)
        {   // call for reinforcements!
            base.OnHarmfulAction(aggressor, isCriminal, source);
            if (m_aggressionMemory.Recall(aggressor) == false)
            {   // we haven't seen this player yet
                m_aggressionMemory.Remember(aggressor, TimeSpan.FromSeconds(AggressionMemoryTime).TotalSeconds);   // remember him for this long
                IPooledEnumerable eable = this.GetMobilesInRange(this.RangePerception);
                foreach (Mobile m in eable)
                {
                    if (m is AIHealer)
                    {   // cause a ruckus
                        Yell("{0}, help!", m.Name);
                        aggressor.DoHarmful(m);
                        break;
                    }
                }
                eable.Free();
            }
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }
    }
}