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

/* Engines/Township/Items/TownshipItem.cs
 * CHANGELOG:
 *  9/13/2023, Adam (item.IsLockedDown)
 *      Set/clear item.IsLockedDown for township lockdowns/items.
 *      This allows items to be queried on a global level, essentially, if they are a house or township lockdown
 * 3/26/22, Adam (BeginDamage)
 *      Add null check on variable 'res' in BeginDamage.
 * 3/24/22, Yoar
 *      Redid weapon requirement message. Now dynamically inserting ItemData.Name into this message.
 *      Players now have ownership of any AddonComponent that is part of an owned addon.
 * 3/24/22, Adam (BeginDamage)
 *      Update BeginDamage to understand XMLAdddons
 * 3/17/22, Adam
 *  Update OnBuild to use new parm list
 * 3/12/22, Adam
 *      Add messaging for damaging a hedge, i.e., "You'll need to equip an axe to damage this hedge."
 * 2/6/22, Adam (BeginDamage)
 *      Add additional logging to try and determine why we are getting crashes in Smite
 * 2/6/22, Yoar
 *      Wrapped BeginDamage in a try-catch.
 * 1/13/22, Yoar
 *      Changed the way township item ownership is handled.
 *      
 *      The owner/placement date is no longer stored on the township item itself. Instead,
 *      a TownshipItemContext is attached to the item which contains the owner/placement
 *      date information. The contexts are accessible through the TownshipItemRegistry that
 *      is attached to the TownshipStone.
 *      
 *      This way, we can claim ownership of items that do *not* implement the ITownshipItem
 *      interface. Example: Addons.
 * 11/24/21, Yoar
 *      - Added TownshipItem.Register, TownshipItem.Unregister, TownshipItem.UpdateRegistry (wip)
 *      - Attacking township items now plays the weapon hit sound
 * 11/23/21, Yoar
 *	    Initial version.
 */

using Server.ContextMenus;
using Server.Engines.Craft;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Regions;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Township
{
    public interface ITownshipItem : IEntity
    {
        int HitsMax { get; set; }
        int Hits { get; set; }

        DateTime LastRepair { get; set; }
        DateTime LastDamage { get; set; }

        void OnBuild(Mobile m);
        bool CanDestroy(Mobile m);
    }

    public static class TownshipItemHelper
    {
        private static readonly List<ITownshipItem> m_AllTownshipItems = new List<ITownshipItem>();

        public static List<ITownshipItem> AllTownshipItems { get { return m_AllTownshipItems; } }

        public static void Register(ITownshipItem tsi)
        {
            AllTownshipItems.Add(tsi);
            if (tsi is Item item)
                item.IsLockedDown = true;
        }

        public static void Unregister(ITownshipItem tsi)
        {
            AllTownshipItems.Remove(tsi);
        }

        public static void Initialize()
        {
            CommandSystem.Register("TSItem", AccessLevel.GameMaster, new CommandEventHandler(TSItem_OnCommand));
        }

        #region Commands

        [Usage("TSItem")]
        [Description("Gets the township item context for the targeted item.")]
        public static void TSItem_OnCommand(CommandEventArgs e)
        {
            e.Mobile.BeginTarget(-1, false, TargetFlags.None, TSItem_OnTarget);
        }

        private static void TSItem_OnTarget(Mobile from, object targeted)
        {
            Item item = targeted as Item;

            if (item == null)
            {
                from.SendMessage("That is not an item.");
                return;
            }

            TownshipItemContext c = LookupContext(item);

            if (c == null && item is AddonComponent)
            {
                AddonComponent ac = (AddonComponent)item;

                if (ac.Addon != null)
                    c = LookupContext(ac.Addon);
            }

            if (c == null)
            {
                from.SendMessage("That has no township item context.");
                return;
            }

            from.SendGump(new PropertiesGump(from, c));
        }

        #endregion

        public static TownshipItemContext LookupContext(Item item)
        {
            foreach (TownshipStone ts in TownshipStone.AllTownshipStones)
            {
                TownshipItemContext c = ts.ItemRegistry.Lookup(item, false);

                if (c != null)
                    return c;
            }

            return null;
        }

        public static void SetOwnership(Item item, Mobile owner)
        {
            SetOwnership(item, owner, DateTime.UtcNow);
        }

        public static void SetOwnership(Item item, Mobile owner, DateTime placed)
        {
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(item.GetWorldLocation(), item.Map);

            if (tsr != null && tsr.TStone != null)
                tsr.TStone.SetItemOwner(item, owner, placed);
        }

        public static bool IsOwner(Item item, Mobile m)
        {
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(item.GetWorldLocation(), item.Map);

            return (tsr != null && tsr.TStone != null && tsr.TStone.IsItemOwner(m, item));
        }

        public static void OnLocationChange(ITownshipItem tsi, Point3D oldLocation)
        {
            // do something?
        }

        public static void OnMapChange(ITownshipItem tsi)
        {
            // do something?
        }

        public static void AddContextMenuEntries(Item item, Mobile from, ArrayList list)
        {
            ITownshipItem tsi = item as ITownshipItem;

            if (tsi == null)
                return;

            if (tsi.HitsMax > 0)
                list.Add(new InspectCME());

            if (tsi.CanDestroy(from))
                list.Add(new DestroyCME());

            if (tsi.HitsMax > 0)
                list.Add(new DamageCME());
        }

        #region Inspect

        private class InspectCME : ContextMenuEntry
        {
            public InspectCME()
                : base(6121, 6) // Look At
            {
            }

            public override void OnClick()
            {
                Inspect(Owner.From, (ITownshipItem)Owner.Target);
            }
        }

        public static void Inspect(Mobile from, ITownshipItem tsi)
        {
            Item item = tsi as Item;

            if (item == null)
                return;

            int hitsPerc = 0;

            if (tsi.HitsMax > 0) // don't divide by zero
                hitsPerc = 100 * tsi.Hits / tsi.HitsMax;

            string message;

            if (hitsPerc >= 100)
                message = "The item is in perfect condition.";
            else if (hitsPerc >= 75)
                message = "The item is in great condition.";
            else if (hitsPerc >= 50)
                message = "The item is in good condition.";
            else if (hitsPerc >= 25)
                message = "The item has taken quite a bit of damage.";
            else
                message = "The item is close to collapsing.";

            from.SendMessage(message);
        }

        #endregion

        #region Damage

        public static TimeSpan DamageDelay { get { return (Core.UOTC_CFG ? TimeSpan.FromMinutes(1.0) : TownshipSettings.WallDamageDelay); } }

        private class DamageCME : ContextMenuEntry
        {
            public DamageCME()
                : base(5009, 2) // Smite
            {
            }

            public override void OnClick()
            {
                BeginDamage(Owner.From, (ITownshipItem)Owner.Target);
            }
        }

        public static bool LikelyDamager(Mobile m, ITownshipItem tsi)
        {
            WeaponType weaponType = GetReqWeapon(tsi);

            if (HasWeapon(m, weaponType))
                return true;

            return false;
        }

        public static bool CheckDamageTarget(Mobile from, object targeted)
        {
            ITownshipItem tsi = targeted as ITownshipItem;

            if (tsi == null)
                return false;

            if (tsi.HitsMax <= 0)
                return false;

            // destroy takes precedence over damage
            if (tsi.CanDestroy(from))
                return false;

            if (!LikelyDamager(from, tsi))
                return false;

            if (targeted is Item && !from.InRange((Item)targeted, 2))
                return false;

            BeginDamageDelayed(from, tsi);
            return true;
        }

        public static void BeginDamageDelayed(Mobile m, ITownshipItem tsi)
        {
            Timer.DelayCall(TimeSpan.Zero, delegate { BeginDamage(m, tsi); });
        }

        public static void BeginDamage(Mobile m, ITownshipItem tsi)
        {
            try
            {
                BeginDamageInternal(m, tsi);
            }
            catch (Exception ex)
            {
                EventSink.InvokeLogException(new LogExceptionEventArgs(ex));
            }
        }

        private static void BeginDamageInternal(Mobile m, ITownshipItem tsi)
        {
            if (AFKCheck(m))
                return;

            if (!m.CanBeginAction(typeof(DamageTimer)))
            {
                m.SendLocalizedMessage(500119); // You must wait to perform another action.
                return;
            }

            if (!CanDamage(m, tsi, true))
                return;

            if (DateTime.UtcNow < tsi.LastDamage + TownshipSettings.WallDamageDelay)
                SendTCNotice(m);

            m.BeginAction(typeof(DamageTimer));

            HitItem(m, tsi);

            new DamageTimer(m, tsi).Start();
        }

        private static void OnDamaging(Mobile m, ITownshipItem tsi)
        {
            HitItem(m, tsi);
        }

        private static void EndDamage(Mobile m, ITownshipItem tsi)
        {
            tsi.LastDamage = DateTime.UtcNow;
            tsi.Hits -= GetWeaponDamage(m);

            if (tsi.Hits <= 0)
            {
                m.SendLocalizedMessage(500461); // You destroy the item.

                Effects.PlaySound(m.Location, m.Map, 0x11C);

                tsi.Delete();
            }
            else
            {
                m.SendMessage("You damage the {0}.", GetObjectName(tsi));
            }
        }

        private static void HitItem(Mobile m, ITownshipItem tsi)
        {
            m.Direction = m.GetDirectionTo(tsi);
            m.RevealingAction();
            m.Emote("*hits the {0}*", GetObjectName(tsi));
            m.CriminalAction(!m.Criminal);

            DoWeaponAnimation(m);
            OnUseWeapon(m);

            NotifyOfDamager(m, tsi);
        }

        private static bool CanDamage(Mobile m, ITownshipItem tsi, bool message)
        {
            if (tsi.HitsMax <= 0)
                return false;

            if (!m.InRange(tsi, 2))
            {
                if (message)
                    m.SendLocalizedMessage(500446); // That is too far away.

                return false;
            }

            if (DateTime.UtcNow < tsi.LastDamage + DamageDelay)
            {
                if (message)
                    m.SendMessage("That has already been damaged recently.");

                return false;
            }

            WeaponType weaponType = GetReqWeapon(tsi);

            if (!HasWeapon(m, weaponType))
            {
                if (message)
                    m.SendMessage("You'll need to wield {0} to damage this {1}.", GetWeaponName(weaponType), GetObjectName(tsi));

                return false;
            }

            return true;
        }

        private static WeaponType GetReqWeapon(ITownshipItem tsi)
        {
            CraftRes res = DefTownshipCraft.LookupPrimaryResource(tsi);

            Type resType = (res == null ? null : res.ItemType);

            if (resType == typeof(Log) || resType == typeof(FertileDirt) || tsi is Engines.Plants.PlantAddon)
                return WeaponType.Axe;
            else
                return WeaponType.Bashing;
        }

        private static string GetWeaponName(WeaponType weaponType)
        {
            switch (weaponType)
            {
                case WeaponType.Axe: return "an axe";
                case WeaponType.Slashing: return "a sword";
                case WeaponType.Staff: return "a staff";
                case WeaponType.Bashing: return "a mace";
                case WeaponType.Piercing: return "a spear";
                case WeaponType.Polearm: return "a polearm";
                case WeaponType.Ranged: return "a bow";
                case WeaponType.Fists: return "your fists";
            }

            return null;
        }

        private static string GetObjectName(ITownshipItem tsi)
        {
            if (tsi is Item)
            {
                Item item = (Item)tsi;

                string itemName = item.GetBaseOldName();

                if (itemName != "nodraw")
                    return itemName;
            }

            return "structure";
        }

        private static void DoWeaponAnimation(Mobile m)
        {
            BaseWeapon weapon = m.Weapon as BaseWeapon;

            if (weapon != null)
            {
                weapon.PlaySwingAnimation(m);

                m.PlaySound(weapon.HitSound);
            }
        }

        private static void OnUseWeapon(Mobile m)
        {
            BaseWeapon weapon = m.Weapon as BaseWeapon;

            if (weapon != null && weapon.MaxHitPoints > 0 && Utility.Random(25) == 0)
            {
                if (weapon.HitPoints > 1)
                    --weapon.HitPoints;
                else
                    weapon.Delete();
            }
        }

        private class DamageTimer : WorkTimer
        {
            private ITownshipItem m_Item;

            public DamageTimer(Mobile m, ITownshipItem tsi)
                : base(m, TownshipSettings.WallDamageTicks)
            {
                m_Item = tsi;
            }

            protected override bool Validate()
            {
                return CanDamage(Mobile, m_Item, false);
            }

            protected override void OnWork()
            {
                OnDamaging(Mobile, m_Item);
            }

            protected override void OnFinished()
            {
                EndDamage(Mobile, m_Item);

                Mobile.EndAction(typeof(DamageTimer));
            }

            protected override void OnFailed()
            {
                Mobile.EndAction(typeof(DamageTimer));
            }
        }

        private static bool HasWeapon(Mobile m, WeaponType weaponType)
        {
            BaseWeapon weapon = m.Weapon as BaseWeapon;

            return (weapon != null && weapon.Type == weaponType);
        }

        private static int GetWeaponDamage(Mobile m)
        {
            BaseWeapon weapon = m.Weapon as BaseWeapon;

            if (weapon == null)
                return 1;

            int damageMin, damageMax;

            weapon.GetBaseDamageRange(m, out damageMin, out damageMax);

            return Math.Max(1, (int)weapon.ScaleDamageOld(m, Utility.RandomMinMax(damageMin, damageMax), true, true));
        }

        private static Memory m_Damagers = new Memory();

        private static void NotifyOfDamager(Mobile damager, ITownshipItem tsi)
        {
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(tsi.Location, tsi.Map);

            if (tsr == null || tsr.TStone == null || tsr.TStone.IsMember(damager))
                return;

            if (m_Damagers.Recall(damager))
                return;

            m_Damagers.Remember(damager, 300.0);

            tsr.TStone.SendMessage(String.Format("{0} at {1} is damaging your township's property!", damager.Name, damager.Location));
        }

        #endregion

        #region Destroy

        private class DestroyCME : ContextMenuEntry
        {
            public DestroyCME()
                : base(6275, 2) // Demolish
            {
            }

            public override void OnClick()
            {
                OnChop((Item)Owner.Target, Owner.From);
            }
        }

        public static void OnChop(Item item, Mobile from)
        {
            ITownshipItem tsi = item as ITownshipItem;

            if (tsi != null && tsi.CanDestroy(from))
            {
                from.CloseGump(typeof(ConfirmDestroyItemGump));
                from.SendGump(new ConfirmDestroyItemGump(item));
            }
        }

        #endregion

        public static bool AFKCheck(Mobile m)
        {
            return TownshipSettings.AFKCheck && m is PlayerMobile && !(((PlayerMobile)m).RTT("AFK township work check."));
        }

        private static readonly Memory m_TCNoticeMemory = new Memory();

        public static void SendTCNotice(Mobile m)
        {
            if (!m_TCNoticeMemory.Recall(m))
            {
                m_TCNoticeMemory.Remember(m, 300.0);

                m.SendMessage("Note: Township work rates are accelerated on Test Center.");
            }
        }

        public static int Atrophy()
        {
            int deleted = 0;

            for (int i = m_AllTownshipItems.Count - 1; i >= 0; i--)
            {
                ITownshipItem tsi = m_AllTownshipItems[i];

                if (tsi.Map != null && tsi.Map != Map.Internal)
                {
                    bool destroyed = false;

                    if (TownshipSettings.WallHitsDecay != 0)
                    {
                        tsi.Hits -= TownshipSettings.WallHitsDecay;

                        destroyed = (tsi.Hits <= 0);
                    }

                    if (destroyed || TownshipRegion.GetTownshipAt(tsi.Location, tsi.Map) == null)
                    {
                        tsi.Delete();

                        deleted++;
                    }
                }
            }

            return deleted;
        }
    }
}