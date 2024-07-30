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

/* Scripts\Engines\Ethics\Core\Ethic.cs
 * Changelog:
 *  1/9/23, Yoar
 *      Added SetEthic, ClearEthic helper methods
 *      Misc. cleanups
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 */

using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.Ethics
{
    public abstract class Ethic
    {
        public static bool Enabled
        {
            // if !Core.NewEthics, use standalone ethics
            // otherwise use faction ethics
            get { return (Core.Ethics && (!Core.NewEthics || Core.Factions)); }
        }

        public static bool AutoJoin { get { return Core.NewEthics; } }

        public static Ethic Find(Item item)
        {
            if ((item.SavedFlags & 0x100) != 0)
            {
                if (item.Hue == Hero.Definition.PrimaryHue)
                    return Hero;

                item.SavedFlags &= ~0x100;
            }

            if ((item.SavedFlags & 0x200) != 0)
            {
                if (item.Hue == Evil.Definition.PrimaryHue)
                    return Evil;

                item.SavedFlags &= ~0x200;
            }

            if (item.VileBlade)
                return Evil;

            if (item.HolyBlade)
                return Hero;

            return null;
        }

        public static bool CheckTrade(Mobile from, Mobile to, Mobile newOwner, Item item)
        {
            Ethic itemEthic = Find(item);

            if (itemEthic == null)
                itemEthic = EthicBless.GetBlessedFor(item);

            if (itemEthic == null || Find(newOwner) == itemEthic)
                return true;

            else if (itemEthic == Hero)
                (from == newOwner ? to : from).SendMessage("Only heros may receive this item.");
            else if (itemEthic == Evil)
                (from == newOwner ? to : from).SendMessage("Only the evil may receive this item.");

            return false;
        }

        public static bool CheckEquip(Mobile from, Item item)
        {
            Ethic itemEthic = Find(item);

            if (itemEthic == null)
                itemEthic = EthicBless.GetBlessedFor(item);

            if (itemEthic == null || Find(from) == itemEthic)
                return true;

            if (from.AccessLevel >= AccessLevel.GameMaster && (itemEthic == Hero || itemEthic == Evil))
            {
                from.SendMessage("That item may only be equipped by someone ethically aligned, but your godly powers allow it.");
                return true;
            }

            if (itemEthic == Hero && item is BaseWeapon)
                from.SendMessage("Only heroes may equip a holy item."); // 1045115: Only Paladins may equip a holy item (I believe Paladin is a later faction term)
            else if (itemEthic == Evil && item is BaseWeapon)
                from.SendMessage("Only the evil may equip an unholy item."); // 1045116: Only evils may equip an unholy item
            else if (itemEthic == Hero)
                from.SendMessage("Only heros may wear this item.");
            else if (itemEthic == Evil)
                from.SendMessage("Only the evil may wear this item.");

            return false;
        }

        public static bool IsImbued(Item item)
        {
            return IsImbued(item, false);
        }

        public static bool IsImbued(Item item, bool recurse)
        {
            if (Find(item) != null)
                return true;

            if (recurse)
            {
                foreach (Item child in item.Items)
                {
                    if (IsImbued(child, true))
                        return true;
                }
            }

            return false;
        }

        public static void Initialize()
        {
            EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
            EventSink.Login += EventSink_OnLogin;

            CommandHandlers.Register("RemoveEthic", AccessLevel.GameMaster, new CommandEventHandler(RemoveEthic_OnCommand));
            CommandHandlers.Register("MakeHero", AccessLevel.GameMaster, new CommandEventHandler(MakeHero_OnCommand));
            CommandHandlers.Register("MakeEvil", AccessLevel.GameMaster, new CommandEventHandler(MakeEvil_OnCommand));
        }

        #region Commands

        [Usage("RemoveEthic")]
        [Description("Removes the ethic alignment from the targeted player.")]
        public static void RemoveEthic_OnCommand(CommandEventArgs e)
        {
            if (e.Target == null)
            {
                e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(RemoveEthic_OnTarget));
                e.Mobile.SendMessage("Target the player to remove from the ethics system.");
            }
            else
                RemoveEthic_OnTarget(e.Mobile, e.Target);
        }

        private static void RemoveEthic_OnTarget(Mobile from, object obj)
        {
            PlayerMobile pm = obj as PlayerMobile;

            if (pm == null)
                from.SendMessage("That is not a player.");
            else if (pm.EthicPlayer == null)
                from.SendMessage("That player does not seem to be in the ethics system.");
            else
            {
                Leave(pm);
                from.SendMessage("That player has been removed from the ethics system.");
            }
        }

        [Usage("MakeHero")]
        [Description("Forces the ethic alignment of the targeted player to Hero.")]
        public static void MakeHero_OnCommand(CommandEventArgs e)
        {
            if (e.Target == null)
            {
                e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(MakeHero_OnTarget));
                e.Mobile.SendMessage("Target the player to make a Hero.");
            }
            else
                MakeHero_OnTarget(e.Mobile, e.Target);
        }

        private static void MakeHero_OnTarget(Mobile from, object obj)
        {
            PlayerMobile pm = obj as PlayerMobile;

            if (pm == null)
                from.SendMessage("That is not a player.");
            else
            {
                Join(pm, Hero);
                from.SendMessage("That player has been added to the ethics system.");
            }
        }

        [Usage("MakeEvil")]
        [Description("Forces the ethic alignment of the targeted player to Evil.")]
        public static void MakeEvil_OnCommand(CommandEventArgs e)
        {
            if (e.Target == null)
            {
                e.Mobile.BeginTarget(-1, false, TargetFlags.None, new TargetCallback(MakeEvil_OnTarget));
                e.Mobile.SendMessage("Target the player to make a Evil.");
            }
            else
                MakeEvil_OnTarget(e.Mobile, e.Target);
        }

        private static void MakeEvil_OnTarget(Mobile from, object obj)
        {
            PlayerMobile pm = obj as PlayerMobile;

            if (pm == null)
                from.SendMessage("That is not a player.");
            else
            {
                Join(pm, Evil);
                from.SendMessage("That player has been added to the ethics system.");
            }
        }

        #endregion

        public static void EventSink_Speech(SpeechEventArgs e)
        {
            if (!Enabled || e.Blocked || e.Handled)
                return;

            #region Dueling
            if (e.Mobile is PlayerMobile && (e.Mobile as PlayerMobile).DuelContext != null)
                return;
            #endregion

            Player pl = Player.Find(e.Mobile);

            if (pl == null)
            {
                for (int i = 0; i < Ethics.Length; ++i)
                {
                    Ethic ethic = Ethics[i];

                    if (!Insensitive.Equals(ethic.Definition.JoinPhrase.String, e.Speech))
                        continue;

                    e.Handled = true;

                    if (Core.NewEthics)
                    {
                        if (!ethic.IsEligible(e.Mobile))
                            break;
                    }
                    else
                    {
                        #region Old Ethics

                        if ((e.Mobile.Created + TimeSpan.FromHours(24)) > DateTime.UtcNow)
                        {
                            e.Mobile.SendLocalizedMessage(502593); // Thou art too young to choose this fate.
                            break;
                        }

                        // don't allow tarnished players to join as Heros

                        // no murder counts if Hero. Evil ok
                        // 8/10/22 Adam: I don't think we should check Core.RedsInTown in this location. 
                        if (!(ethic == Ethic.Evil || (e.Mobile.ShortTermMurders == 0 && !e.Mobile.Red)))
                        {   // not an official H/E string. I wish I knew what it was!
                            e.Mobile.SendMessage("Your murderous ways prevent you from taking this path.");
                            break;
                        }

                        // Heros can't belong to the Thieves guild and gain life force. Evil ok
                        if (!(ethic == Ethic.Evil || ((e.Mobile is PlayerMobile) && (e.Mobile as PlayerMobile).NpcGuild != NpcGuild.ThievesGuild)))
                        {   // not an official H/E string. I wish I knew what it was!
                            e.Mobile.SendMessage("You must first quit the thieves' guild.");
                            break;
                        }

                        // Just in case, check ethic specifics
                        if (!ethic.IsEligible(e.Mobile))
                            break;

                        #endregion
                    }

                    bool isNearAnkh = false;

                    foreach (Item item in e.Mobile.GetItemsInRange(2))
                    {
                        if (item is Items.AnkhNorth || item is Items.AnkhWest)
                        {
                            isNearAnkh = true;
                            break;
                        }
                    }

                    if (!isNearAnkh)
                        continue;

                    Join(e.Mobile, ethic);

                    break;
                }
            }
            else
            {
                Ethic ethic = pl.Ethic;

                if (Insensitive.Equals(ethic.Definition.InvokePhrase.String, e.Speech))
                {
                    e.Handled = true;

                    if (Factions.NewGumps.FactionGumps.Enabled)
                    {
                        Factions.NewGumps.FactionGumps.CloseGumps(e.Mobile, typeof(Factions.NewGumps.Ethics.EthicsGump));
                        e.Mobile.SendGump(new Factions.NewGumps.Ethics.EthicsGump(pl));

                        return;
                    }

                    // TODO: OSI gump

                    return;
                }

                for (int i = 0; i < ethic.Definition.Powers.Length; ++i)
                {
                    Power power = ethic.Definition.Powers[i];

                    if (!Insensitive.Equals(power.Definition.Phrase.String, e.Speech))
                        continue;

                    e.Handled = true;

                    if (!power.CheckInvoke(pl))
                        continue;

                    power.BeginInvoke(pl);

                    break;
                }
            }
        }

        private static void EventSink_OnLogin(LoginEventArgs e)
        {
            if (!Enabled)
                return;

            if (AutoJoin)
                CheckJoin(e.Mobile);
        }

        public static void CheckJoin(Mobile mob)
        {
            Player pl = Player.Find(mob);

            if (pl != null)
                return;

            foreach (Ethic ethic in Ethics)
            {
                if (ethic.IsEligible(mob))
                {
                    Join(mob, ethic);
                    break;
                }
            }
        }

        public static void Leave(Mobile mob)
        {
            Player pl = Player.Find(mob);

            if (pl == null)
                return;

            pl.Ethic.OnLeave(mob);

            pl.Detach();
            mob.Delta(MobileDelta.Noto);
        }

        public static void Join(Mobile mob, Ethic ethic)
        {
            Leave(mob);

            Player pl = new Player(ethic, mob);

            pl.Attach();
            mob.Delta(MobileDelta.Noto);

            mob.FixedEffect(0x373A, 10, 30);
            mob.PlaySound(0x209);

            ethic.OnJoin(mob);
        }

        // only applies to Old Ethics
        public static void HandleDeath(Mobile victim, Mobile killer)
        {
            if (killer == null)
                killer = victim.FindMostRecentDamager(true);

            if (killer == null)
                return;             // happens if GM [kill'ed

            // creature points

            if (victim is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)victim;

                if (bc.Map == Factions.Faction.Facet && bc.GetEthicAllegiance(killer) == BaseCreature.Allegiance.Enemy)
                {
                    Ethics.Player killerEPL = Server.Ethics.Player.Find(killer);

                    if (killerEPL != null && (100 - killerEPL.Power) > Utility.Random(100))
                    {   // killer belongs to an ethic
                        if (killer is PlayerMobile)
                        {   // sanity
                            // 8/10/22 Adam: I don't think we should check Core.RedsInTown in this location. 
                            if ((killerEPL.Ethic == Ethic.Evil && killer.ShortTermMurders == 0) || (killerEPL.Ethic == Ethic.Hero && killer.ShortTermMurders == 0 && !killer.Red))
                            {   // no murder counts if Hero. Evil ok to have longs (this is how I read the docs)
                                if (killerEPL.Ethic == Ethic.Evil || ((killer as PlayerMobile).NpcGuild != NpcGuild.ThievesGuild))
                                {   // Heros can't belong to the Thieves guild and gain life force. Evil ok
                                    // from the evil/hero system doc
                                    // Note that in everything below, "kills" is defined by "the person who did the most damage when the 
                                    //	person was killed." (I assum this applied to creatures as well.)
                                    if (Mobile.MostDamage(victim, killer))
                                    {
                                        if (killerEPL.Power < 100)
                                        {
                                            ++killerEPL.Power;
                                            ++killerEPL.History;
                                            killer.SendLocalizedMessage(1045100); // You have gained LifeForce
                                        }
                                        else
                                            killer.SendLocalizedMessage(1045101); // Your LifeForce is at the maximum
                                    }
                                }
                            }
                        }
                    }
                }

                return;
            }
            else
            {
                // player points

                Ethics.Player killerEPL = Server.Ethics.Player.Find(killer, true);
                Ethics.Player victimEPL = Server.Ethics.Player.Find(victim);

                // evil kill an innocent 

                // When evils kill innocents that they attacked, all the evil's stats and skills fall by 50% for five minutes . 
                //	They also lose lifeforce. If the evil has 0 lifeforce, then the evil can also be reported for murder. 
                //	Lastly, they cannot hide, recall, or gate away for those five minutes. 
                if (killer.Evil && !victim.Hero && !victim.Evil)
                {
                    if (ComputeMurder(victim, killer))
                    {   // stats and skills
                        ApplySkillLoss(killer);
                        ApplyStatLoss(killer);

                        // loss of life force (no idea how much)
                        killerEPL.Power -= Math.Min(killerEPL.Power, Utility.Random(10, 50));

                        // loss of sphere
                        killerEPL.History -= Math.Min(killerEPL.History, Utility.Random(10, 50));

                        // set the EvilCrim state which will prevent gate and hide for 5 minutes
                        killer.ExpirationFlags.Add(new Mobile.ExpirationFlag(killer, Mobile.ExpirationFlagID.EvilCrim, TimeSpan.FromMinutes(5)));

                        killer.SendLocalizedMessage(1045108); // You have lost lifeforce
                        killer.LocalOverheadMessage(Server.Network.MessageType.Regular, 0x3B2, 500913); // The life force of this victim was not pure.  You stumble back in pain.
                    }
                }

                // good kill an innocent 

                // Innocents who die to heroes are covered under the standard reputation system. In addition, the heroes are stripped of their status. 
                // If you lose too much sphere or lifeforce, you become a "fallen hero." You are still gray to evils for an additional five minutes. 
                // If a hero is reported for murder, all lifeforce and sphere immediately vanishes, and you become a fallen hero
                if (killer.Hero && !victim.Hero && !victim.Evil)
                {
                    if (ComputeMurder(victim, killer))
                    {   // the killer murdered the victim

                        // loss of life force (no idea how much)
                        killerEPL.Power -= Math.Min(killerEPL.Power, Utility.Random(10, 50));

                        // loss of sphere
                        killerEPL.History -= Math.Min(killerEPL.History, Utility.Random(10, 50));

                        if (killerEPL.Power == 0 || killerEPL.History == 0)
                        {
                            // set the FallenHero state which make you gray to evil for 5 minutes (what does it mean for a hero to be gray to an evil?)
                            killer.ExpirationFlags.Add(new Mobile.ExpirationFlag(killer, Mobile.ExpirationFlagID.FallenHero, TimeSpan.FromMinutes(5)));
                        }

                        killer.SendLocalizedMessage(1045108); // You have lost lifeforce
                    }
                }

                // transfer lifeforce

                if (Mobile.MostDamage(victim, killer))
                {   // we did the most damage
                    if (killerEPL != null && victimEPL != null && victimEPL.Power > 0)
                    {   // two participants in the ethics system
                        if (killer is PlayerMobile)
                        {   // sanity
                            // 8/10/22 Adam: I don't think we should check Core.RedsInTown in this location. 
                            if ((killerEPL.Ethic == Ethic.Evil && killer.ShortTermMurders == 0) || (killerEPL.Ethic == Ethic.Hero && killer.ShortTermMurders == 0 && !killer.Red))
                            {   // no murder counts if Hero. Evil ok to have longs (this is how I read the docs)
                                if (killerEPL.Ethic == Ethic.Evil || ((killer as PlayerMobile).NpcGuild != NpcGuild.ThievesGuild))
                                {   // Heros can't belong to the Thieves guild and gain life force. Evil ok
                                    if (victim.CheckState(Mobile.ExpirationFlagID.NoPoints) == false)
                                    {   //  [un]holy word - No sphere nor lifeforce is granted for the kill.
                                        if (killerEPL.Ethic != victimEPL.Ethic)
                                        {   // heros can't gain life force by killing other heros (we'll apply this to evils as well)

                                            // 20% of victim's power
                                            int powerTransfer = victimEPL.Power / 5;

                                            if (powerTransfer > (100 - killerEPL.Power))
                                                powerTransfer = 100 - killerEPL.Power;

                                            if (powerTransfer > 0)
                                            {
                                                victimEPL.Power -= powerTransfer;
                                                killerEPL.Power += powerTransfer;

                                                killerEPL.History += powerTransfer;

                                                killer.SendLocalizedMessage(1045100); // You have gained LifeForce
                                                victim.SendLocalizedMessage(1045108); // You have lost lifeforce
                                            }
                                            else
                                                killer.SendLocalizedMessage(1045101); // Your LifeForce is at the maximum
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // award special kill points to pre ethic enrollment participants that kill evil
                // to qualify we will use the rules for gaining life force for Hero i.e.,
                // You cannot gain points life force while you are red, not while you have any short murder counts, 
                //	nor if you are a member of the Thieves' Guild. 
                if (killerEPL == null && victimEPL != null && victimEPL.Ethic == Ethic.Evil)
                {   // killer not in ethics && victim is Evil
                    // 8/10/22 Adam: I don't think we should check Core.RedsInTown in this location. 
                    if (killer.ShortTermMurders == 0 && !killer.Red)
                    {   // no murder counts
                        if (killer is PlayerMobile && (killer as PlayerMobile).NpcGuild != NpcGuild.ThievesGuild)
                        {   // not in the Thieves' Guild
                            PlayerMobile pm = (killer as PlayerMobile);
                            // check to make sure we have not killed thie evil before
                            bool goodKill = true;
                            foreach (PlayerMobile.EthicKillsLog ekl in pm.EthicKillsLogList)
                            {
                                if (ekl.Serial == victim.Serial && !ekl.Expired)
                                {   // we've killed this guy within the last 5 days
                                    goodKill = false;
                                    break;
                                }
                            }
                            if (goodKill)
                            {
                                pm.EthicKillsLogList.Add(new PlayerMobile.EthicKillsLog(victim.Serial, DateTime.UtcNow));
                                if (pm.EthicPoints >= 5)
                                {   // Add to Good alignment (EthicPoints log will be wipped on next serialization)
                                    if (Ethic.Hero.IsEligible(killer))  // make sure they are old enough
                                    {
                                        Player pl = new Player(Ethic.Hero, killer);

                                        pl.Attach();
                                        pm.Delta(MobileDelta.Noto);

                                        killer.FixedEffect(0x373A, 10, 30);
                                        killer.PlaySound(0x209);

                                        killer.SendLocalizedMessage(501994); // For your heroic deeds you are granted the title of hero.
                                    }
                                }
                                else
                                {   // tell them they are on the path to hero!
                                    killer.SendLocalizedMessage(502598); // Strive to continue on the path of benevolence.
                                }
                            }
                        }
                    }
                }
            }
        }

        #region ComputeMurder
        public static bool ComputeMurder(Mobile victim, Mobile killer)
        {
            if (victim.Player)
                foreach (AggressorInfo ai in victim.Aggressors)
                {
                    if (ai.Attacker.Player && ai.CanReportMurder && !ai.InitialAggressionInNoCountZone)
                    {
                        if (ai.Attacker == killer)
                            return true;
                    }
                }

            return false;
        }
        #endregion

        #region Skill Loss
        public const double SkillLossFactor = 1.0 / 2;
        public static readonly TimeSpan SkillLossPeriod = TimeSpan.FromMinutes(5.0);

        private static Hashtable m_SkillLoss = new Hashtable();

        private class SkillLossContext
        {
            public Timer m_Timer;
            public ArrayList m_Mods;
        }

        public static void ApplySkillLoss(Mobile mob)
        {
            SkillLossContext context = (SkillLossContext)m_SkillLoss[mob];

            if (context != null)
                return;

            context = new SkillLossContext();
            m_SkillLoss[mob] = context;

            ArrayList mods = context.m_Mods = new ArrayList();

            for (int i = 0; i < mob.Skills.Length; ++i)
            {
                Skill sk = mob.Skills[i];
                double baseValue = sk.Base;

                if (baseValue > 0)
                {
                    SkillMod mod = new DefaultSkillMod(sk.SkillName, true, -(baseValue * SkillLossFactor));

                    mods.Add(mod);
                    mob.AddSkillMod(mod);
                }
            }

            context.m_Timer = Timer.DelayCall(SkillLossPeriod, new TimerStateCallback(ClearSkillLoss_Callback), mob);
        }

        public static void ApplyStatLoss(Mobile m)
        {
            int strBonus = m.Str / 2;
            int dexBonus = m.Dex / 2;
            int intBonus = m.Int / 2;
            string modName = "[Evil Penalty]";
            m.AddStatMod(new StatMod(StatType.Str, modName + "Str", -strBonus, SkillLossPeriod));
            m.AddStatMod(new StatMod(StatType.Dex, modName + "Dex", -dexBonus, SkillLossPeriod));
            m.AddStatMod(new StatMod(StatType.Int, modName + "Int", -intBonus, SkillLossPeriod));
        }

        private static void ClearSkillLoss_Callback(object state)
        {
            ClearSkillLoss((Mobile)state);
        }

        public static bool ClearSkillLoss(Mobile mob)
        {
            SkillLossContext context = (SkillLossContext)m_SkillLoss[mob];

            if (context == null)
            {
                return false;
            }

            m_SkillLoss.Remove(mob);

            ArrayList mods = context.m_Mods;

            for (int i = 0; i < mods.Count; ++i)
                mob.RemoveSkillMod((SkillMod)mods[i]);

            context.m_Timer.Stop();

            mob.SendLocalizedMessage(500912);   // You have recovered from the impure life force absorbtion.

            return true;
        }
        #endregion

        protected EthicDefinition m_Definition;

        protected PlayerCollection m_Players;

        public EthicDefinition Definition
        {
            get { return m_Definition; }
        }

        public PlayerCollection Players
        {
            get { return m_Players; }
        }

        public static Ethic Find(Mobile mob)
        {
            return Find(mob, false, false);
        }

        public static Ethic Find(Mobile mob, bool inherit)
        {
            return Find(mob, inherit, false);
        }

        public static Ethic Find(Mobile mob, bool inherit, bool allegiance)
        {
            Player pl = Player.Find(mob);

            if (pl != null)
                return pl.Ethic;

            if (inherit && mob is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)mob;

                if (bc.Controlled)
                    return Find(bc.ControlMaster, false);
                else if (bc.Summoned)
                    return Find(bc.SummonMaster, false);
                else if (allegiance)
                    return bc.EthicAllegiance;
            }

            return null;
        }

        public Ethic()
        {
            m_Players = new PlayerCollection();
        }

        public abstract bool IsEligible(Mobile mob);

        public virtual void OnJoin(Mobile mob)
        {
        }

        public virtual void OnLeave(Mobile mob)
        {
        }

        public virtual void Deserialize(GenericReader reader)
        {
            int version = reader.ReadEncodedInt();

            switch (version)
            {
                case 0:
                    {
                        int playerCount = reader.ReadEncodedInt();

                        for (int i = 0; i < playerCount; ++i)
                        {
                            Player pl = new Player(this, reader);

                            if (pl.Mobile != null)
                                Timer.DelayCall(TimeSpan.Zero, new TimerCallback(pl.CheckAttach));
                        }

                        break;
                    }
            }
        }

        public virtual void Serialize(GenericWriter writer)
        {
            writer.WriteEncodedInt(0); // version

            writer.WriteEncodedInt(m_Players.Count);

            for (int i = 0; i < m_Players.Count; ++i)
                m_Players[i].Serialize(writer);
        }

        public static readonly Ethic Hero = new Hero.HeroEthic();
        public static readonly Ethic Evil = new Evil.EvilEthic();

        public static readonly Ethic[] Ethics = new Ethic[]
            {
                Hero,
                Evil
            };
    }
}