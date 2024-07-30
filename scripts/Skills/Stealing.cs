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

/* Scripts/Skills/Stealing.cs
 * ChangeLog:
 *  5/7/2024, Adam (caught)
 *      Add the LOS check here. Players shouldn't report things they cannot see
 *  1/5/24, Yoar
 *      You can no longer steal items that are inside a locked container.
 *  8/4/2023, Adam
 *      Update stealing to understand the stealing of multi-component addons
 *  7/15/2023, Adam: (RaresSkillPreCheck / GetWeight)
 *      Since 'weight' is the difficulty factor in stealing, We use a modified weight when stealing
 *          rares. We don't much care what the weight is, but only that is consistent across items of the 
 *          same type.
 *  7/13/2023, Adam: 
 *      Update to allow stealing stealable items off the ground
 *      More Info: RaresSpawners now allows the setting of the MustSteal flag.
 *      This requires the item to be stolen.
 *      Also, after an item is stolen, we call stolen.OnAfterStolen(from).
 *          This allows us to morph one item into another, like from an item => addon deed.
 *  4/29/23, Yoar (Hot Items)
 *      * Problem: Blue players cannot access their bank box for a duration of 2 minutes after having
 *        successfully stolen an item from a creature.
 *      * Cause: Special property on PlayerMobile "LastStoleAt" is set when successfully stealing an
 *        item from any Mobile. This prevents the thief from opening their bank box.
 *      * New approach: Hot items. Items that were stolen from players become "hot". Players that are
 *        holding hot items cannot access their bank box nor can they recall. Furthermore, hot items
 *        cannot be stored in bank boxes. These restrictions do not apply to the original owner of
 *        the item. Hot items "cool down" after 2 minutes.
 *  8/30/22, Yoar
 *      Merge with RunUO 2.3
 *  8/28/22, Yoar
 *      Added stealing of sigils.
 *  11/28/21, Adam (CheckReversePickpocket)
 *      Add a memory object which prevents thieves from a ReversePickpocket overload exploit.
 *      We limit the 'plant' to one per hour on the same mobile
 *	5/27/10, adam
 *		special case: disallow stealing from someones stonebag
 *		We could generalize thos say you cannot steal from a blessed container if need be
 *	06/01/09, plasma
 *		Implement reverse pickpocket method
 *	4/9/09, Adam
 *		Add CheckStealing() to check the region rules re looting. Stealing off a corpse is considered looting
 *	1/7/09, Adam
 *		Add "You can't steal from them." message when CanBeHarmful == false
 *	07/23/08, weaver
 *		Automated IPooledEnumerable optimizations. 1 loops updated.
 *	11/17/07, Pix.
 *		Removed code bleed from who knows what I was trying to do!
 *	6/7/07, Pix.
 *		Removed check from stealing from targets in the thieves guild, which allowed
 *		thieves to steal from any other thief without going crim or permagrey.
 *  11/23/06, plasma
 *      Modified to only apply skill delay OnTraget()
 *  03/01/06, Kit
 *		Added check to close bankbox on item steal if open.
 *  02/28/06, weaver
 *		Added reset of next skill use time on steal target.
 *  01/02/05, Pix
 *		Now thief must be in the thieves guild to steal from players.
 *  12/13/04, Pigpen
 *		System message will now be played upon failure of item theft.
 *  10/16/04, Darva
 *		Added logic to update the players LastStoleAt value.
 *	7/31/04, mith
 *		OnTarget() final IF statement, added check to see if the item was successfully stolen before making thief perma-grey to victim.
 *  6/12/04, Old Salty
 * 		OrcishKinMasks now explode on stealing from orcs
 *	6/10/04, mith
 *		modified minimum amount stealable when stealing stacks of items
 *	4/12/04, changes by mith
 *		Set ClassicMode = true to re-enable perma-grey.
 */

using Server.Factions;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;
using Server.Spells.Fifth;
using Server.Spells.Seventh;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.SkillHandlers
{
    public class Stealing
    {

        public static void Initialize()
        {
            SkillInfo.Table[33].Callback = new SkillUseCallback(OnUse);
        }

        public static readonly bool ClassicMode = true;
        public static readonly bool SuspendOnMurder = false;

        public static bool IsInGuild(Mobile m)
        {
            return (m is PlayerMobile && ((PlayerMobile)m).NpcGuild == NpcGuild.ThievesGuild);
        }

        public static bool IsInnocentTo(Mobile from, Mobile to)
        {
            return (Notoriety.Compute(from, (Mobile)to) == Notoriety.Innocent);
        }

        private class StealingTarget : Target
        {
            private Mobile m_Thief;

            public StealingTarget(Mobile thief) : base(1, false, TargetFlags.None)
            {
                m_Thief = thief;

                AllowNonlocal = true;
            }

            private bool CheckStealing(Corpse corpse)
            {
                if (!corpse.IsCriminalAction(m_Thief))
                    return true;

                #region Static Region
                StaticRegion sr = StaticRegion.FindStaticRegion(corpse);
                if (sr != null && sr.BlockLooting && m_Thief.AccessLevel == AccessLevel.Player)
                    return false;
                #endregion

                return true;
            }

            private bool CanStealFrom(object root)
            {
                if (!IsEmptyHanded(m_Thief))
                {
                    m_Thief.SendLocalizedMessage(1005584); // Both hands must be free to steal.
                }
                else if (root == m_Thief)
                {
                    m_Thief.SendLocalizedMessage(502704); // You catch yourself red-handed.
                }
                else if (root is Mobile && ((Mobile)root).Player && !IsInGuild(m_Thief))
                {
                    m_Thief.SendLocalizedMessage(1005596); // You must be in the thieves guild to steal from other players.
                }
                else if (SuspendOnMurder && root is Mobile && ((Mobile)root).Player && IsInGuild(m_Thief) && m_Thief.LongTermMurders > 0)
                {
                    m_Thief.SendLocalizedMessage(502706); // You are currently suspended from the thieves guild.
                }
                else if (root is BaseVendor && ((BaseVendor)root).IsInvulnerable)
                {
                    m_Thief.SendLocalizedMessage(1005598); // You can't steal from shopkeepers.
                }
                else if (root is PlayerVendor)
                {
                    m_Thief.SendLocalizedMessage(502709); // You can't steal from vendors.
                }
                else if (root is Mobile && ((Mobile)root).AccessLevel > AccessLevel.Player)
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (root is Mobile && !m_Thief.CanBeHarmful((Mobile)root))
                {
                    m_Thief.SendMessage("You can't steal from them.");
                }
                else if (root is Corpse && !CheckStealing((Corpse)root)) // stealing off a corpse is looting
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else
                {
                    return true;
                }

                return false;
            }

            private Item TryStealItem(Item toSteal, ref bool caught)
            {
                //Zen make bankbox close when stealing!
                BankBox box = m_Thief.FindBankNoCreate();
                if (box != null && box.Opened)
                {
                    box.Close();
                    m_Thief.Send(new MobileUpdate(m_Thief));
                }

                Item stolen = null;

                object root = toSteal.RootParent;

                if (!m_Thief.CanSee(toSteal))
                {
                    m_Thief.SendLocalizedMessage(500237); // Target can not be seen.
                }
                else if (m_Thief.Backpack == null || !m_Thief.Backpack.CheckHold(m_Thief, toSteal, false, true))
                {
                    m_Thief.SendLocalizedMessage(1048147); // Your backpack can't hold anything else.
                }
                #region Sigils
                else if (toSteal is Sigil)
                {
                    PlayerState pl = PlayerState.Find(m_Thief);
                    Faction faction = (pl == null ? null : pl.Faction);

                    Sigil sig = (Sigil)toSteal;

                    if (!m_Thief.InRange(toSteal.GetWorldLocation(), 1))
                    {
                        m_Thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
                    }
                    else if (root != null) // not on the ground
                    {
                        m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                    }
                    else if (faction != null)
                    {
                        if (!m_Thief.CanBeginAction(typeof(IncognitoSpell)))
                        {
                            m_Thief.SendLocalizedMessage(1010581); //	You cannot steal the sigil when you are incognito
                        }
                        else if (DisguiseGump.IsDisguised(m_Thief))
                        {
                            m_Thief.SendLocalizedMessage(1010583); //	You cannot steal the sigil while disguised
                        }
                        else if (!m_Thief.CanBeginAction(typeof(PolymorphSpell)))
                        {
                            m_Thief.SendLocalizedMessage(1010582); //	You cannot steal the sigil while polymorphed				
                        }
                        /*else if (TransformationSpellHelper.UnderTransformation(m_Thief))
                        {
                            m_Thief.SendLocalizedMessage(1061622); // You cannot steal the sigil while in that form.
                        }
                        else if (AnimalForm.UnderTransformation(m_Thief))
                        {
                            m_Thief.SendLocalizedMessage(1063222); // You cannot steal the sigil while mimicking an animal.
                        }*/
                        else if (pl.IsLeaving)
                        {
                            m_Thief.SendLocalizedMessage(1005589); // You are currently quitting a faction and cannot steal the town sigil
                        }
                        else if (sig.IsBeingCorrupted && sig.LastMonolith.Faction == faction)
                        {
                            m_Thief.SendLocalizedMessage(1005590); //	You cannot steal your own sigil
                        }
                        else if (sig.IsPurifying)
                        {
                            m_Thief.SendLocalizedMessage(1005592); // You cannot steal this sigil until it has been purified
                        }
                        else if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, 80.0, 80.0, new object[2] /*contextObj*/))
                        {
                            if (Sigil.ExistsOn(m_Thief))
                            {
                                m_Thief.SendLocalizedMessage(1010258); //	The sigil has gone back to its home location because you already have a sigil.
                            }
                            else if (m_Thief.Backpack == null || !m_Thief.Backpack.CheckHold(m_Thief, sig, false, true))
                            {
                                m_Thief.SendLocalizedMessage(1010259); //	The sigil has gone home because your backpack is full
                            }
                            else
                            {
                                if (sig.IsBeingCorrupted)
                                    sig.GraceStart = DateTime.UtcNow; // begin grace period

                                m_Thief.SendLocalizedMessage(1010586); // YOU STOLE THE SIGIL!!!   (woah, calm down now)

                                if (sig.LastMonolith != null && sig.LastMonolith.Sigil != null)
                                {
                                    sig.LastMonolith.Sigil = null;
                                    sig.LastStolen = DateTime.UtcNow;
                                }

                                return sig;
                            }
                        }
                        else
                        {
                            m_Thief.SendLocalizedMessage(1005594); //	You do not have enough skill to steal the sigil
                        }
                    }
                    else
                    {
                        m_Thief.SendLocalizedMessage(1005588); //	You must join a faction to do that
                    }
                }
                #endregion
                // 7/13/2023, Adam: Update to allow stealing stealable items off the ground
                else if ((toSteal.Parent == null || !toSteal.Movable) && !MustSteal(toSteal))
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                // 7/13/2023, Adam: Update to allow stealing stealable items off the ground
                else if ((Item.IsLootTypeSet(toSteal, LootType.UnStealable) && toSteal.Parent != null && !MustSteal(toSteal)) || toSteal.CheckBlessed(root))
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (toSteal.Parent is Stonebag) // disallow stealing from someones stonebag
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }
                else if (!m_Thief.InRange(toSteal.GetWorldLocation(), 1))
                {
                    m_Thief.SendLocalizedMessage(502703); // You must be standing next to an item to steal it.
                }
                else if (toSteal.Parent is Mobile)
                {
                    m_Thief.SendLocalizedMessage(1005585); // You cannot steal items which are equiped.
                }
                else
                {
                    double w = toSteal.Weight + toSteal.TotalWeight;

                    if (w > 10)
                    {
                        m_Thief.SendMessage("That is too heavy to steal.");
                    }
                    // you need to pass this core 'pre check' before you can steal a Stealable Item
                    else if (!StealableItemsSkillPreCheck(toSteal))
                    {
                        m_Thief.SendLocalizedMessage(502723); // You fail to steal the item.
                    }
                    else
                    {
                        if (toSteal.Stackable && toSteal.Amount > 1)
                        {
                            int minAmount = (int)((m_Thief.Skills[SkillName.Stealing].Value / 25.0) / toSteal.Weight);
                            int maxAmount = (int)((m_Thief.Skills[SkillName.Stealing].Value / 10.0) / toSteal.Weight);

                            if (minAmount < 1)
                                minAmount = 1;

                            if (maxAmount < 1)
                                maxAmount = 1;
                            else if (maxAmount > toSteal.Amount)
                                maxAmount = toSteal.Amount;

                            int amount = Utility.RandomMinMax(minAmount, maxAmount);

                            if (amount >= toSteal.Amount)
                            {
                                int pileWeight = (int)Math.Ceiling(toSteal.Weight * toSteal.Amount);
                                pileWeight *= 10;

                                if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, pileWeight - 22.5, pileWeight + 27.5, new object[2] { toSteal.RootParent, null } /*contextObj*/))
                                    stolen = toSteal;
                            }
                            else
                            {
                                int pileWeight = (int)Math.Ceiling(toSteal.Weight * amount);
                                pileWeight *= 10;

                                if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, pileWeight - 22.5, pileWeight + 27.5, new object[2] { toSteal.RootParent, null } /*contextObj*/))
                                {
                                    stolen = toSteal.Dupe(amount);
                                    toSteal.Amount -= amount;
                                }
                            }
                        }
                        else
                        {
                            int iw = (int)Math.Ceiling(w);
                            iw *= 10;

                            if (m_Thief.CheckTargetSkill(SkillName.Stealing, toSteal, iw - 22.5, iw + 27.5, new object[2] { toSteal.RootParent, null } /*contextObj*/))
                                stolen = toSteal;
                        }

                        if (stolen != null)
                            m_Thief.SendLocalizedMessage(502724); // You successfully steal the item.
                        else
                            m_Thief.SendLocalizedMessage(502723); // You fail to steal the item.

                        caught = (m_Thief.Skills[SkillName.Stealing].Value < Utility.Random(150));

                        if (stolen != null && stolen.GetFlag(LootType.Rare))
                            m_Thief.RareAcquisitionLog(stolen);

                        if (stolen != null)
                        {
                            if (stolen.GetItemBool(Item.ItemBoolTable.RemoveHueOnLift))
                                stolen.Hue = 0;
                            if (stolen.GetItemBool(Item.ItemBoolTable.NormalizeOnLift))
                                Utility.NormalizeOnLift(stolen);
                        }

                        // Monster ignore	Velgo K'balc
                        // This ability makes even aggressive monsters ignore the evil player for a time, unless they were already engaged in combat with them. 
                        // Attacking or stealing from a monster will shatter the spell.
                        if ((m_Thief is PlayerMobile) && root is Mobile && m_Thief.Evil && ((PlayerMobile)m_Thief).CheckState(Mobile.ExpirationFlagID.MonsterIgnore))
                            ((PlayerMobile)m_Thief).RemoveState(Mobile.ExpirationFlagID.MonsterIgnore);
                    }
                }

                // wea: reset next skill time
                m_Thief.NextSkillTime = Core.TickCount + (int)TimeSpan.FromSeconds(10.0).TotalMilliseconds;

                return stolen;
            }
            #region Stealable Items
            bool StealableItemsSkillPreCheck(Item item)
            {
                int iw = (int)Math.Ceiling(GetWeight(item));
                iw *= 10;

                if (m_Thief.CheckTargetSkill(SkillName.Stealing, item, iw - 22.5, iw + 27.5, new object[2] { item.RootParent, null } /*contextObj*/))
                    return true;

                return false;
            }
            double GetWeight(Item item)
            {
                if (MustSteal(item))
                {   // give stealable items a weight
                    int[] table = new[] { 4, 5, 6, 7, 8 };
                    int index = Math.Abs(Utility.GetStableHashCode(item.ItemID.ToString()));
                    return table[index % table.Length];
                }
                return item.Weight;
            }
            #endregion Stealable Items
            protected bool MustSteal(Item item)
            {
                if (item.GetItemBool(Item.ItemBoolTable.MustSteal))
                    return true;

                if (item is AddonComponent ac && ac.Addon != null && ac.Addon.GetItemBool(Item.ItemBoolTable.MustSteal))
                    return true;

                return false;
            }
            protected void OnAfterStolen(Mobile from, Item item)
            {
                if (item is AddonComponent ac && ac.Addon != null)
                    ac.Addon.OnAfterStolen(from);       // steal the addon
                else if (item.BaseCamp != null)
                {
                    item.BaseCamp.OnAfterStolen(from);  // steal the camp component, and corrupt the camp (starts delete timer)
                    item.BaseCamp.StealCampItem(item);  // remove item from camp inventory
                }

                item.OnAfterStolen(from);               // steal the item
                item.Movable = true;
            }
            protected override void OnTarget(Mobile from, object target)
            {
                from.RevealingAction();

                Item stolen = null;
                object root = null;
                bool caught = false;

                if (target is Item)
                {
                    root = ((Item)target).RootParent;

                    if (IsInsideLockedContainer((Item)target))
                        m_Thief.SendLocalizedMessage(500447); // That is not accessible.
                    else if (CanStealFrom(root))
                        stolen = TryStealItem((Item)target, ref caught);
                }
                else if (target is Mobile)
                {
                    if (CanStealFrom(target))
                    {
                        Container pack = ((Mobile)target).Backpack;

                        // adam, TODO: we need to change this to be any kin 
                        Item hat = from.FindItemOnLayer(Layer.Helm);      // Added by OldSalty 6/12/04 from here...
                        if (hat is OrcishKinMask && (target is Orc || target is OrcBomber || target is OrcBrute || target is OrcCaptain || target is OrcishLord || target is OrcishMage))
                        {
                            AOS.Damage(from, 50, 0, 100, 0, 0, 0, hat);
                            hat.Delete();
                            from.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
                            from.PlaySound(0x307);
                        }                                                   // . . . to here

                        if (pack != null && pack.Items.Count > 0)
                        {
                            m_Thief.SendLocalizedMessage(1010579); // You reach into the backpack... and try to take something.

                            int randomIndex = Utility.Random(pack.Items.Count);

                            root = target;
                            stolen = TryStealItem((Item)pack.Items[randomIndex], ref caught);
                        }
                        else
                        {
                            m_Thief.SendLocalizedMessage(1010578); // You reach into the backpack... but find it's empty.
                        }
                    }
                }
                else
                {
                    m_Thief.SendLocalizedMessage(502710); // You can't steal that!
                }

                if (stolen != null)
                {
                    from.AddToBackpack(stolen);

                    // Some stolen items morph into something else. Like the scarecrow morphs into an addondeed
                    OnAfterStolen(from, stolen);

                    #region Hot Items
                    if (root is Mobile && ((Mobile)root).Player)
                    {
                        BankBox bankBox = from.FindBankNoCreate();

                        if (bankBox != null)
                            bankBox.Close();

                        SetHotItem(stolen, (Mobile)root);
                    }
                    #endregion
                }

                if (caught)
                {
                    if (root == null)
                    {
                        m_Thief.CriminalAction(false);
                    }
                    else if (root is Corpse && ((Corpse)root).IsCriminalAction(m_Thief))
                    {
                        m_Thief.CriminalAction(false);
                    }
                    else if (root is Mobile)
                    {
                        Mobile mobRoot = (Mobile)root;

                        if (!IsInGuild(mobRoot) && IsInnocentTo(m_Thief, mobRoot))
                            m_Thief.CriminalAction(false);

                        string message = String.Format("You notice {0} trying to steal from {1}.", m_Thief.Name, mobRoot.Name);

                        foreach (NetState ns in m_Thief.GetClientsInRange(8))
                        {   // 5/6/2024, Adam: Add the LOS check here. Players shouldn't report things they cannot see
                            if (ns.Mobile != m_Thief && ns.Mobile.InLOS(m_Thief))
                                ns.Mobile.SendMessage(message);
                        }
                    }
                }
                else if (root is Corpse && ((Corpse)root).IsCriminalAction(m_Thief))
                {
                    m_Thief.CriminalAction(false);
                }

                if (root is Mobile && ((Mobile)root).Player && m_Thief is PlayerMobile && IsInnocentTo(m_Thief, (Mobile)root) && !IsInGuild((Mobile)root))
                {
                    PlayerMobile pm = (PlayerMobile)m_Thief;

                    pm.PermaFlags.Add((Mobile)root);
                    pm.Delta(MobileDelta.Noto);
                }

                //PIX: 11/17/07 - WTF is this?  Why did I have this in my code?
                //if (stolen != null)
                //{
                //	if (root is Mobile)
                //	{
                //		((Mobile)root).AggressiveAction(m_Thief, false);
                //	}
                //}
            }
        }

        public static bool IsEmptyHanded(Mobile from)
        {
            if (from.FindItemOnLayer(Layer.OneHanded) != null)
                return false;

            if (from.FindItemOnLayer(Layer.TwoHanded) != null)
                return false;

            return true;
        }

        public static bool IsInsideLockedContainer(Item item)
        {
            Container cont = item.Parent as Container;

            while (cont != null)
            {
                if (cont is LockableContainer && ((LockableContainer)cont).Locked)
                    return true;

                cont = cont.Parent as Container;
            }

            return false;
        }

        public static TimeSpan OnUse(Mobile m)
        {
            if (!IsEmptyHanded(m))
            {
                m.SendLocalizedMessage(1005584); // Both hands must be free to steal.
            }
            else
            {
                m.Target = new Stealing.StealingTarget(m);
                m.RevealingAction();

                m.SendLocalizedMessage(502698); // Which item do you want to steal?
            }

            //pla : changed this to 1 second by default. 
            //The 10 second delay is only after a steal attempt
            return TimeSpan.FromSeconds(1.0);
        }

        public static bool CheckReversePickpocket(Mobile from, Item item, Item target, Memory memory)
        {
            if (from == null || from.Deleted ||
                    item == null || !(item is BaseBook) || item.Deleted ||
                    target == null || target.Deleted)
                return false;

            if (!(from is PlayerMobile) || !(target is Container))
                return false;

            Container c = ((Container)target);
            PlayerMobile mark = null;

            if (!(c.Parent is PlayerMobile))
                return false;

            mark = ((PlayerMobile)c.Parent);

            if (mark.Deleted)
                return false;

            PlayerMobile thief = (PlayerMobile)from;

            //Must be close
            if (thief.GetDistanceToSqrt(mark) > 1) return false;

            //Need 100 steal and snoop to attempt
            if (thief.Skills.Stealing.Base < 100.0 || thief.Skills.Snooping.Base < 100.0)
                return false;

            //Check perma flags
            if (!thief.PermaFlags.Contains(mark))
            {
                thief.SendMessage("You many only attempt to plant books on marks who you are already criminal to.");
                return false;
            }

            //Weight..
            if (!c.CheckHold(mark, item, false))
            {
                thief.SendMessage("That person cannot hold the weight!");
                return false;
            }

            //All we need to do here is the criminal action, since they will already be perma
            if (IsInnocentTo(thief, mark))
            {
                thief.CriminalAction(false);
            }

            //Normal check, 75% chance to plant book successfully
            bool checkSkill = thief.CheckSkill(SkillName.Stealing, 0.75, contextObj: new object[2]);

            if (checkSkill && memory.Recall(mark) == true)
            {
                thief.SendMessage("You may not plant another book in so short a time.");
                return false;
            }
            else
                // only allow one book per hour to be planted - otherwise it's just annoying/overload exploit
                memory.Remember(mark, 3600);

            //Normal check, 75% chance to plant book successfully
            if (checkSkill)
            {
                thief.SendMessage("You sucessfully slip them the book.");
                mark.SendMessage(string.Format("You have been slipped {0} !", ((BaseBook)item).Title));
                IPooledEnumerable eable = thief.GetClientsInRange(6);
                if (eable != null)
                {
                    foreach (NetState ns in eable)
                    {
                        if (ns != thief.NetState && ns != mark.NetState)
                            ns.Mobile.SendMessage("You notice {0} slipping {1} to {2}", thief.Name, ((BaseBook)item).Title, mark.Name);
                    }
                    eable.Free(); eable = null;
                }
                return true;
            }
            else
            {
                thief.SendMessage("You fail to slip them the book.");
                IPooledEnumerable eable = thief.GetClientsInRange(6);
                if (eable != null)
                {
                    foreach (NetState ns in eable)
                    {
                        if (ns != thief.NetState && ns != mark.NetState)
                            ns.Mobile.SendMessage("You notice {0} attempting to slip {1} a book", thief.Name, mark.Name);
                    }
                    eable.Free(); eable = null;
                }
            }
            return false;
        }

        #region Hot Items

        public static TimeSpan HotItemDuration = TimeSpan.FromMinutes(2.0);

        private static readonly Dictionary<Item, HotItemTimer> m_HotItems = new Dictionary<Item, HotItemTimer>();

        public static Dictionary<Item, HotItemTimer> HotItems { get { return m_HotItems; } }

        public static bool IsHotItem(Item item, Mobile from)
        {
            HotItemTimer timer;

            return (m_HotItems.TryGetValue(item, out timer) && timer.Victim != from);
        }

        public static bool HasHotItem(Mobile from)
        {
            foreach (KeyValuePair<Item, HotItemTimer> kvp in m_HotItems)
            {
                if (kvp.Key.IsChildOf(from.Backpack) && kvp.Value.Victim != from)
                    return true;
            }

            return false;
        }

        public static void SetHotItem(Item item, Mobile victim)
        {
            RemoveHotItem(item);

            (m_HotItems[item] = new HotItemTimer(item, victim, HotItemDuration)).Start();
        }

        public static void RemoveHotItem(Item item)
        {
            HotItemTimer timer;

            if (m_HotItems.TryGetValue(item, out timer))
            {
                timer.Stop();
                m_HotItems.Remove(item);
            }
        }

        public class HotItemTimer : Timer
        {
            private Item m_Item;
            private Mobile m_Victim;

            public Mobile Victim { get { return m_Victim; } }

            public HotItemTimer(Item item, Mobile victim, TimeSpan delay)
                : base(delay)
            {
                m_Item = item;
                m_Victim = victim;
            }

            protected override void OnTick()
            {
                RemoveHotItem(m_Item);
            }
        }

        #endregion
    }
}