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

/* Scripts/Mobiles/Special/TreeOfKnowledge.cs
 * ChangeLog:
 *  10/13/21, Adam
 *      Added Tenjin's Saw. Tenjin's Saw is used for magic crafting for carpenters. 
 *  09/17/21, Yoar
 *      Changes to TOK services:
 *	    - Completely rewrote this system, cleaning up a lot of copy pasta and
 *	      resulting in more consistent behavior across the board.
 *	    - Redid price calculation. It's no longer based on MinTameSkill. Now,
 *	      it's based off of various factors (similar to how barding difficulty
 *	      is calculated). Essentially, the stronger the pet is, the more
 *	      expensive the services are.
 *	    - Added resurrect service. Warns player of pet skill loss.
 *	    - Re-enabled bonding service. But now it insta-bonds you with your pet.
 *	      This service is very expensive! Around 90-100k gold for a freshly tamed
 *	      dragon.
 *	    - Added logging to TOK transactions (tok.log).
 *	    - Added support to deal with multiple pets that have the same name. This
 *	      is done by calling TOKService.Validate in TOKService.FindPet.
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 2 loops updated.
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  10/10/06, Kit
 *		b1 revert, dont bond pets anymore :/
 *  05/08/06, Kit
 *		Added releaseing bonded pet functionality to TOK, changed bonded msg to "I wish to forge a bond with the spirit of"
 *		Changed msg for release function to warn on bonded pet it will result in death, *2 cost for bonded pet release.
 *  05/07/06, Kit
 *		Extended TOK to now initiate pet bonding, and release unbonded pets, rewrote ValidatePet
 *		function to test based on switch and request type vs if/else if/else.
 *	02/11/06, Adam
 *		Make common the formatting of sextant coords.
 *  1/01/06, Kit
 *		Fixed problem with tok returning stabled pet first and preventing return of pet with same name in world.
 *	9/27/05, Adam
 *		a. Add timer start for the DrainTimer in Deserialize
 *		b. Add Succubus style Aura damage and DrainLife
 *		We now have TWO drain systems + aura. This guy is not really meant to be farmed
 *		And so this level of protection should discourage all but the most persistent.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 7 lines removed.
 *	5/7/05, Pix
 *		Fix for crash - the spiritlist and locatelist weren't being instantiated
 *		when the TOK was deserialized.
 *	4/24/05, Adam
 *		Add the Angry() function so that the tree will attack only mobiles that it's angery at.
 *			That is, on it's agro list.
 *	4/21/05, Adam
 *		1. Rename
 *		2. Switch armor/wep generation to ImbueWeaponOrArmor()
 *	4/07/05 Created by smerX
 */

using Server.Diagnostics;
using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    [CorpseName("an ancient tree")]
    public class TreeOfKnowledge : BaseCreature
    {
        private DrainTimer m_Timer;

        [Constructable]
        public TreeOfKnowledge()
            : base(AIType.AI_Mage, FightMode.Aggressor, 18, 1, 0.2, 0.4)
        {
            Name = "tree of knowledge";
            BodyValue = 47;
            BardImmune = true;

            SetStr(900, 1000);
            SetDex(125, 135);
            SetInt(1000, 1200);

            SetFameLevel(4);
            SetKarmaLevel(4);

            VirtualArmor = 60;

            SetSkill(SkillName.Wrestling, 93.9, 96.5);
            SetSkill(SkillName.Tactics, 96.9, 102.2);
            SetSkill(SkillName.MagicResist, 131.4, 140.8);
            SetSkill(SkillName.Magery, 156.2, 161.4);
            SetSkill(SkillName.EvalInt, 100.0);
            SetSkill(SkillName.Meditation, 120.0);

            AddItem(new LightSource());

            m_Timer = new DrainTimer(this);
            m_Timer.Start();
        }

        public override bool DisallowAllMoves { get { return true; } }
        // Auto-dispel is UOR - http://forums.uosecondage.com/viewtopic.php?f=8&t=6901
        public override bool AutoDispel { get { return Core.RuleSets.AutoDispelChance(); } }
        public override Poison PoisonImmune { get { return Poison.Lethal; } }

        public override bool CanBandage { get { return true; } }
        public override TimeSpan BandageDelay { get { return TimeSpan.FromSeconds(15.0); } }
        public override int BandageMin { get { return 40; } }
        public override int BandageMax { get { return 85; } }

        public override AuraType MyAura { get { return AuraType.Hate; } }
        public override int AuraRange { get { return 5; } }
        public override int AuraMin { get { return 5; } }
        public override int AuraMax { get { return 10; } }
        public override TimeSpan NextAuraDelay { get { return TimeSpan.FromSeconds(4.0); } }

        public void DrainLife()
        {
            ArrayList list = new ArrayList();

            IPooledEnumerable eable = this.GetMobilesInRange(2);
            foreach (Mobile m in eable)
            {
                // exclude these cases
                if (m == this || !CanBeHarmful(m)) continue;
                if (AuraTarget(m) == false) continue;

                if (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned || ((BaseCreature)m).Team != this.Team))
                    list.Add(m);
                else if (m.Player)
                    list.Add(m);
            }
            eable.Free();

            foreach (Mobile m in list)
            {
                DoHarmful(m);

                m.FixedParticles(0x374A, 10, 15, 5013, 0x496, 0, EffectLayer.Waist);
                m.PlaySound(0x231);

                m.SendMessage("You feel the life drain out of you!");

                int toDrain = Utility.RandomMinMax(10, 40);

                Hits += toDrain;
                m.Damage(toDrain, this, this);
            }
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (0.1 >= Utility.RandomDouble())
                DrainLife();
        }

        public override void OnGotMeleeAttack(Mobile attacker)
        {
            base.OnGotMeleeAttack(attacker);

            if (0.1 >= Utility.RandomDouble())
                DrainLife();
        }

        public override bool AuraTarget(Mobile aggressor)
        {
            if (aggressor == this)
                return false;

            List<AggressorInfo> list = this.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker == aggressor)
                {
                    return true;
                }
            }

            list = aggressor.Aggressors;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Attacker == this)
                {
                    return true;
                }
            }

            list = this.Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Defender == aggressor)
                {
                    return true;
                }
            }

            list = aggressor.Aggressed;

            for (int i = 0; i < list.Count; ++i)
            {
                AggressorInfo info = (AggressorInfo)list[i];

                if (info.Defender == this)
                {
                    return true;
                }
            }

            return false;
        }

        private class DrainTimer : Timer
        {
            private TreeOfKnowledge m_Owner;

            public DrainTimer(TreeOfKnowledge owner)
                : base(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0))
            {
                m_Owner = owner;
                Priority = TimerPriority.TwoFiftyMS;
            }

            private static ArrayList m_ToDrain = new ArrayList();

            protected override void OnTick()
            {
                if (m_Owner.Deleted)
                {
                    Stop();
                    return;
                }

                if (0.2 < Utility.RandomDouble())
                    return;

                if (m_Owner.Combatant != null)
                {
                    IPooledEnumerable eable = m_Owner.GetMobilesInRange(8);
                    foreach (Mobile m in eable)
                    {   // exclude the obvious
                        if (m == m_Owner) continue;
                        if (m.AccessLevel != AccessLevel.Player) continue;

                        if (m_Owner.CanBeHarmful(m))
                            if (m_Owner.AuraTarget(m) == true)
                                m_ToDrain.Add(m);
                    }
                    eable.Free();

                    foreach (Mobile m in m_ToDrain)
                    {
                        m_Owner.DoHarmful(m);

                        m.FixedParticles(0x374A, 10, 15, 5013, 0x455, 0, EffectLayer.Waist);
                        m.PlaySound(0x231);

                        m.SendMessage("You feel a sharp pain in your head!");

                        if (m_Owner != null)
                            m_Owner.Hits += 20;

                        m.Damage(20, m_Owner, this);
                    }

                    m_ToDrain.Clear();
                }
            }
            /*
						private bool Angry( Mobile target )
						{
							if ( target == m_Owner )
								return false;

							ArrayList list = m_Owner.Aggressors;

							for ( int i = 0; i < list.Count; ++i )
							{
								AggressorInfo ai = (AggressorInfo)list[i];

								if ( ai.Attacker == target )
									return true;
							}

							return false;
						}
			*/
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int HitsMax { get { return 30000; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override int ManaMax { get { return 5000; } }

        public TreeOfKnowledge(Serial serial)
            : base(serial)
        {
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

            // restart the timer on load!
            m_Timer = new DrainTimer(this);
            m_Timer.Start();
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for Core.RuleSets.AngelIslandRules()
                PackGold(28000, 32000);
                PackItem(new Log(Utility.Random(2500, 3500)));
                GenerateRegs();

                // Tenjin's Saw is used for magic carpentry
                if (CoreAI.IsDynamicFeatureSet(CoreAI.FeatureBits.MagicCraftSystem) == true)
                    PackItem(new TenjinsSaw());

                // Use our unevenly weighted table for chance resolution
                Item item;
                item = Loot.RandomArmorOrShieldOrWeapon();
                PackItem(Loot.ImbueWeaponOrArmor(noThrottle: false, item, Loot.ImbueLevel.Level6 /*6*/, 0, false));
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // ai special
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {   // Standard RunUO
                    // ai special
                }
            }
        }

        private void GenerateRegs()
        {
            PackItem(new BlackPearl(Utility.Random(80, 150)));
            PackItem(new Garlic(Utility.Random(80, 150)));
            PackItem(new Bloodmoss(Utility.Random(80, 150)));
            PackItem(new Ginseng(Utility.Random(95, 190)));
            PackItem(new MandrakeRoot(Utility.Random(95, 190)));
            PackItem(new SulfurousAsh(Utility.Random(80, 150)));
            PackItem(new SpidersSilk(Utility.Random(80, 150)));
            PackItem(new Nightshade(Utility.Random(80, 150)));
        }

        public override void OnAfterDelete()
        {
            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;

            UnregisterAll();

            base.OnAfterDelete();
        }

        public static bool AllowShortCommands = true; // can we use short commands, e.g. "release <petname>"?
        public static double PriceScalarGlobal = 1.0; // global price scalar for all services
        public static double PriceScalarBonding = 2.0; // special price scalar for bonding

        public enum ServiceType
        {
            Spirit,
            Locate,
            Bond,
            Release,
            Resurrect
        }

        private static readonly TOKService[] m_Services = new TOKService[]
            {
                new TOKSpirit(),
                new TOKLocate(),
                new TOKBond(),
                new TOKRelease(),
                new TOKResurrect()
            };

        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from.Alive)
            {
                foreach (TOKService service in m_Services)
                {
                    if (service.HandleSpeech(this, from, e))
                    {
                        //e.Handled = true;
                        return;
                    }
                }
            }

            base.OnSpeech(e);
        }

        public override bool OnGoldGiven(Mobile from, Gold dropped)
        {
            foreach (TOKService service in m_Services)
            {
                if (service.HandleGoldGiven(this, from, dropped.Amount))
                {
                    dropped.Delete();
                    return true;
                }
            }

            return base.OnGoldGiven(from, dropped);
        }

        // since we may be dealing with large amounts of gold, let's accept bank checks too
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is BankCheck)
            {
                BankCheck check = (BankCheck)dropped;

                foreach (TOKService service in m_Services)
                {
                    if (service.HandleGoldGiven(this, from, check.Worth))
                    {
                        dropped.Delete();
                        return true;
                    }
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        private class TOKSpirit : TOKService
        {
            public override ServiceType Service { get { return ServiceType.Spirit; } }

            public TOKSpirit()
                : base()
            {
                PriceBase = 2000;
                PriceScalar = 40.0;
                Commands = new string[] { "i wish the return of", "i wish to return the spirit of" };
                ShortCommands = new string[] { "return", "spirit" };
                PropositionMessage = "I will return the spirit of your pet to you for the penance of {1:N0} gp.";
            }

            public override bool Validate(TreeOfKnowledge tok, Mobile master, BaseCreature pet, bool message)
            {
                if (!base.Validate(tok, master, pet, message))
                    return false;

                if (!pet.IsBonded)
                {
                    if (message)
                        tok.SayTo(master, "You have not bonded with that pet, and so I cannot reach its spirit through you.");

                    return false;
                }
                else if (master.InRange(pet, 12))
                {
                    if (message)
                        tok.SayTo(master, "Your pet is not far from here, and thus you do not require my assistance.");

                    return false;
                }

                return true;
            }

            public override void OnPurchase(TreeOfKnowledge tok, Mobile master, BaseCreature pet)
            {
                Effects.SendLocationParticles(EffectItem.Create(pet.Location, pet.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);

                if (pet.Alive)
                    pet.Kill(); // we're returning the spirit of our pet - not its flesh :(

                pet.MoveToWorld(master.Location, master.Map);

                Effects.SendLocationParticles(EffectItem.Create(master.Location, master.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
            }
        }

        private class TOKLocate : TOKService
        {
            public override ServiceType Service { get { return ServiceType.Locate; } }

            public TOKLocate()
                : base()
            {
                PriceBase = 2000;
                PriceScalar = 60.0;
                Commands = new string[] { "i wish to locate", "i wish to find" };
                ShortCommands = new string[] { "locate", "find" };
                PropositionMessage = "I will tell you the location of your pet for the penance of {1:N0} gp.";
            }

            public override bool Validate(TreeOfKnowledge tok, Mobile master, BaseCreature pet, bool message)
            {
                if (!base.Validate(tok, master, pet, message))
                    return false;

                if (master.InRange(pet, 12))
                {
                    if (message)
                        tok.SayTo(master, "Your pet is not far from here, you do not require my assistance.");

                    return false;
                }

                return true;
            }

            public override void OnPurchase(TreeOfKnowledge tok, Mobile master, BaseCreature pet)
            {
                tok.SayTo(master, String.Format("Your pet, \"{0}\", is at {1}.", pet.Name, GetSextantLocation(pet)));

                if (0.05 > Utility.RandomDouble())
                {
                    if (pet.Alive)
                        tok.SayTo(master, "He seems to be relatively healthy.");
                    else
                        tok.SayTo(master, "Your pet is dead.");
                }
            }

            private static string GetSextantLocation(Mobile m)
            {
                if (m.Deleted)
                    return "Pet Heaven";

                Map map = m.Map;

                if (map == null || map == Map.Internal)
                    return "???";

                int xLong = 0, yLat = 0, xMins = 0, yMins = 0;
                bool xEast = false, ySouth = false;
                bool valid = Sextant.Format(m.Location, map, ref xLong, ref yLat, ref xMins, ref yMins, ref xEast, ref ySouth);

                string location;

                if (valid)
                    location = Sextant.Format(xLong, yLat, xMins, yMins, xEast, ySouth);
                else
                    location = String.Format("{0} {1}", m.X, m.Y);

                Region reg = m.Region;

                if (reg != map.DefaultRegion)
                    location = String.Format("{0} in {1}", location, reg);

                return location;
            }
        }

        private class TOKBond : TOKService
        {
            public override ServiceType Service { get { return ServiceType.Bond; } }

            public TOKBond()
                : base()
            {
                PriceBase = 5000;
                PriceScalar = 500.0;
                Commands = new string[] { "i wish to bond", "i wish to forge a bond with the spirit of" };
                ShortCommands = new string[] { "bond" };
                PropositionMessage = "I will forever link the spirit of {0} with you for the penance of {1:N0} gp.";
            }

            public override bool Validate(TreeOfKnowledge tok, Mobile master, BaseCreature pet, bool message)
            {
                if (!base.Validate(tok, master, pet, message))
                    return false;

                if (pet.IsBonded)
                {
                    if (message)
                        tok.SayTo(master, "Your pet is bonded to you already, and thus you do not require my assistance.");

                    return false;
                }
                else if (!master.InRange(pet, 6))
                {
                    if (message)
                        tok.SayTo(master, "Your pet is too far away from here, and so the bonding process cannot begin.");

                    return false;
                }
#if old
                else if (pet.BondingBegin != DateTime.MinValue)
                {
                    if (message)
                        tok.SayTo(master, "The bonding of {0}'s spirit has already begun, you must now wait some time for it to complete.", pet.Name);

                    return false;
                }
#endif
                else if (pet.MinTameSkill >= 29.1 && master.Skills[SkillName.AnimalTaming].Value < pet.MinTameSkill)
                {
                    if (message)
                        tok.SayTo(master, "Your connection and control over your pet is too weak, and so you may not bond with it.");

                    return false;
                }

                return true;
            }

            public override void ScalePrice(ref double dprice, BaseCreature pet)
            {
                dprice *= PriceScalarBonding;
            }

            public override void OnPurchase(TreeOfKnowledge tok, Mobile master, BaseCreature pet)
            {
#if old
                tok.SayTo(master, "The bonding of your pet's spirit to you has now begun.");

                pet.BondingBegin = DateTime.UtcNow; // TODO: Insta-bond
#else
                tok.SayTo(master, "The bonding of your pet's spirit is complete.");

                pet.IsBonded = true;
#endif
            }
        }

        private class TOKRelease : TOKService
        {
            public override ServiceType Service { get { return ServiceType.Release; } }

            public TOKRelease()
                : base()
            {
                PriceBase = 1000;
                PriceScalar = 25.0;
                Commands = new string[] { "i wish to release" };
                ShortCommands = new string[] { "release" };
                PropositionMessage = "I will sever your connection with {0} for the penance of {1:N0} gp.";
            }

            public override bool Validate(TreeOfKnowledge tok, Mobile master, BaseCreature pet, bool message)
            {
                if (!base.Validate(tok, master, pet, message))
                    return false;

                if (master.InRange(pet, 12))
                {
                    if (message)
                        tok.SayTo(master, "Your pet is not far from here, and thus you do not require my assistance.");

                    return false;
                }

                return true;
            }

            public override void ScalePrice(ref double dprice, BaseCreature pet)
            {
                if (pet.IsBonded)
                    dprice *= 2.0;
            }

            public override void OnPurchase(TreeOfKnowledge tok, Mobile master, BaseCreature pet)
            {
                tok.SayTo(master, "Your connection with {0} has now been severed.", pet.Name);

                pet.AIObject.DoOrderRelease();
            }
        }

        private class TOKResurrect : TOKService
        {
            public override ServiceType Service { get { return ServiceType.Resurrect; } }

            public TOKResurrect()
                : base()
            {
                PriceBase = 500;
                PriceScalar = 20.0;
                Commands = new string[] { "i wish to resurrect" };
                ShortCommands = new string[] { "resurrect", "res" };
                PropositionMessage = "I will resurrect {0} for the penance of {1:N0} gp.";
            }

            public override bool Validate(TreeOfKnowledge tok, Mobile master, BaseCreature pet, bool message)
            {
                if (!base.Validate(tok, master, pet, message))
                    return false;

                if (!pet.IsDeadPet)
                {
                    if (message)
                        tok.SayTo(master, "Your pet is alive, and thus you do not require my assistance.");

                    return false;
                }
                else if (!pet.IsBonded)
                {
                    if (message)
                        tok.SayTo(master, "You have not bonded with that pet, and so I cannot return its spirit to the living.");

                    return false;
                }
                else if (!master.InRange(pet, 6))
                {
                    if (message)
                        tok.SayTo(master, "Your pet is too far away from here, and so the resurrection process cannot begin.");

                    return false;
                }

                return true;
            }

            public override void OnProposition(TreeOfKnowledge tok, Mobile from, BaseCreature pet, int price)
            {
                base.OnProposition(tok, from, pet, price);

                if (PetResurrectGump.SuffersSkillLoss(pet))
                    from.PrivateOverheadMessage(MessageType.Regular, 0, false, PetResurrectGump.SkillLossWarning, from.NetState);
            }

            public override void OnPurchase(TreeOfKnowledge tok, Mobile master, BaseCreature pet)
            {
                pet.PlaySound(0x214);
                pet.FixedEffect(0x376A, 10, 16);
                pet.ResurrectPet();

                PetResurrectGump.CheckApplySkillLoss(pet);
            }
        }

        private abstract class TOKService
        {
            public abstract ServiceType Service { get; }

            protected int PriceBase;
            protected double PriceScalar;
            protected string[] Commands;
            protected string[] ShortCommands;
            protected string PropositionMessage;
            protected string ConcurrentMessage;

            public TOKService()
            {
                Commands = ShortCommands = new string[0];
                ConcurrentMessage = "I'll just wait here for that {1:N0} gp...";
            }

            public virtual bool Validate(TreeOfKnowledge tok, Mobile master, BaseCreature pet, bool message)
            {
                if (master.Mount == pet)
                {
                    if (message)
                        tok.SayTo(master, "You are riding that creature...");

                    return false;
                }
                else if (pet.IsAnyStabled)
                {
                    if (message)
                        tok.SayTo(master, "That creature is in your stables. If you wish to see it, talk to your {0}.", pet.GetStableMasterName);

                    return false;
                }

                return true;
            }

            public int GetPrice(BaseCreature pet)
            {
                double dprice = PriceBase + PriceScalar * CalculateLevel(pet);

                dprice *= PriceScalarGlobal;

                ScalePrice(ref dprice, pet);

                int price = Convert.ToInt32(dprice);

                if (price < 1)
                    price = 1;

                return price;
            }

            public virtual void ScalePrice(ref double dprice, BaseCreature pet)
            {
            }

            public bool HandleSpeech(TreeOfKnowledge tok, Mobile from, SpeechEventArgs e)
            {
                string speech = e.Speech;

                string petName;

                bool match = Match(speech, Commands, out petName);

                if (!match && AllowShortCommands)
                    match = Match(speech, ShortCommands, out petName);

                if (!match)
                    return false;

                if (String.IsNullOrEmpty(petName))
                {
                    tok.SayTo(from, "You did not name a pet!");
                    return true;
                }

                BaseCreature pet = FindPet(tok, from, petName);

                if (pet == null)
                {
                    tok.SayTo(from, "You have no pets by that name.");
                    return true;
                }

                if (!Validate(tok, from, pet, true))
                    return true;

                int price = GetPrice(pet);

                TOKRequest request;

                if (tok.m_Requests.TryGetValue(from, out request) && request.Price == price)
                {
                    OnConcurrent(tok, from, pet, price);
                    return true;
                }

                tok.Register(Service, from, pet, price);

                OnProposition(tok, from, pet, price);
                return true;
            }

            private bool Match(string speech, string[] commands, out string petName)
            {
                petName = null;

                foreach (string command in commands)
                {
                    if (Insensitive.StartsWith(speech, command))
                    {
                        if (speech.Length > command.Length)
                        {
                            if (speech[command.Length] != ' ')
                                continue; // must be followed by a whitespace

                            if (speech.Length > command.Length + 1)
                                petName = speech.Substring(command.Length + 1).Trim();
                        }

                        return true;
                    }
                }

                return false;
            }

            private BaseCreature FindPet(TreeOfKnowledge tok, Mobile master, string petName)
            {
                BaseCreature found = null;

                foreach (Mobile m in World.Mobiles.Values)
                {
                    if (m is BaseCreature)
                    {
                        BaseCreature bc = (BaseCreature)m;

                        if (bc.Controlled && bc.ControlMaster == master && Insensitive.Equals(bc.Name, petName))
                        {
                            if (found == null || !Validate(tok, master, found, false))
                                found = bc;
                        }
                    }
                }

                if (found != null)
                    return found; // let's prioritize pets in the wild

                if (AnimalTrainer.Table.ContainsKey(master))
                    foreach (Mobile m in AnimalTrainer.Table[master])
                    {
                        if (m is BaseCreature)
                        {
                            BaseCreature bc = (BaseCreature)m;

                            if (Insensitive.Equals(bc.Name, petName))
                            {
                                if (found == null || !Validate(tok, master, found, false))
                                    found = bc;
                            }
                        }
                    }

                return found;
            }

            public virtual void OnProposition(TreeOfKnowledge tok, Mobile from, BaseCreature pet, int price)
            {
                tok.SayTo(from, PropositionMessage, pet.Name, price);
            }

            public virtual void OnConcurrent(TreeOfKnowledge tok, Mobile from, BaseCreature pet, int price)
            {
                tok.SayTo(from, ConcurrentMessage, pet.Name, price);
            }

            public virtual bool HandleGoldGiven(TreeOfKnowledge tok, Mobile from, int given)
            {
                TOKRequest request;

                if (!tok.m_Requests.TryGetValue(from, out request) || request.Service != Service)
                    return false;

                BaseCreature pet = request.Pet;

                if (AnimalTrainer.Table.ContainsKey(from))
                    if (pet.Deleted || ((!pet.Controlled || pet.ControlMaster != from) && AnimalTrainer.Table[from].IndexOf(pet) == -1))
                    {
                        tok.Unregister(from);
                        return false;
                    }

                if (!Validate(tok, from, pet, true))
                {
                    tok.Unregister(from);
                    return false;
                }

                int price = GetPrice(pet);

                if (request.Price != price)
                {
                    tok.Unregister(from);
                    return false;
                }

                if (given != price)
                    return false;

                OnPurchase(tok, from, pet);

                LogHelper Logger = new LogHelper("tok.log", false, true);

                Logger.Log(LogType.Mobile, from, String.Format("Used TOK to {0} {1} for {2:N0} gp.", Service, pet, price));

                Logger.Finish();

                tok.Unregister(from);
                return true;
            }

            public abstract void OnPurchase(TreeOfKnowledge tok, Mobile master, BaseCreature pet);
        }

        private readonly Dictionary<Mobile, TOKRequest> m_Requests = new Dictionary<Mobile, TOKRequest>();

        public void Register(ServiceType service, Mobile master, BaseCreature pet, int price)
        {
            Unregister(master);

            (m_Requests[master] = new TOKRequest(this, service, master, pet, price)).Start();
        }

        public void UnregisterAll()
        {
            foreach (TOKRequest request in m_Requests.Values)
                request.Stop();

            m_Requests.Clear();
        }

        public void Unregister(Mobile m)
        {
            TOKRequest request;

            if (m_Requests.TryGetValue(m, out request))
                request.Stop();

            m_Requests.Remove(m);
        }

        private class TOKRequest : Timer
        {
            private TreeOfKnowledge m_TOK;
            private ServiceType m_Service;
            private Mobile m_Master;
            private BaseCreature m_Pet;
            private int m_Price;

            public TreeOfKnowledge TOK { get { return m_TOK; } }
            public ServiceType Service { get { return m_Service; } }
            public Mobile Master { get { return m_Master; } }
            public BaseCreature Pet { get { return m_Pet; } }
            public int Price { get { return m_Price; } }

            public TOKRequest(TreeOfKnowledge tok, ServiceType service, Mobile master, BaseCreature pet, int price)
                : base(TimeSpan.FromMinutes(5.0))
            {
                m_TOK = tok;
                m_Service = service;
                m_Master = master;
                m_Pet = pet;
                m_Price = price;
            }

            protected override void OnTick()
            {
                m_TOK.Unregister(m_Master);
            }
        }

        /// <summary>
        /// Calculate the toughness 'level' of <paramref name="bc"></paramref>.
        /// Based on BaseInstrument.GetCreatureDifficulty.
        /// </summary>
        /// <param name="bc"></param>
        /// <returns></returns>
        public static int CalculateLevel(BaseCreature bc)
        {
            double val = 0.0;

            val += 1.6 * Scale(bc.HitsMax, 700.0);
            val += 1.0 * Scale(bc.StamMax, 125.0);
            val += 1.0 * Scale(bc.ManaMax, 125.0);
            val += 1.0 * Scale(bc.SkillsTotal / 10.0, 700.0);

            if (bc.AI == AIType.AI_Mage && bc.Skills[SkillName.Magery].Base > 5.0)
                val += Scale(bc.Skills[SkillName.Magery].Base, 120.0);

            if (bc.HasBreath)
                val += 100;

            if (bc.PoisonImmune != null)
                val += 100;

            if (bc.HitPoison != null)
                val += 20 * (bc.HitPoison.Level + 1);

            if (bc.Paragon)
                val += 400.0;

            return Math.Max(1, Math.Min(199, (int)(val / 20.0)));
        }

        private static double Scale(double value, double cap)
        {
            if (value > cap)
                return cap + (value - cap) * (3.0 / 11.0);

            return value;
        }
    }
}