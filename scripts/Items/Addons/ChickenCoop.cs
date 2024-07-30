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

/* Scripts\Items\Addons\ChickenCoop.cs
 * CHANGELOG:
 *  1/20/2024, Adam
 *      Add stable domain, i.e., IsCoopStabled = true;
 *      Add these stabled pets to the global pet cache. I.e., Mobile.PetCache.add(pet))
 *  12/19/23, Yoar
 *      On delete, the chickens are now expelled from the coop instead of being deleted.
 *  12/4/23, Yoar
 *      Initial version
 */

using Server.ContextMenus;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Items
{
    [Township.TownshipAddon]
    public class ChickenCoopAddon : BaseAddon
    {

        public static int UseRange = 12;
        public static int MaxSlots = 4;
        public static List<ChickenCoopAddon> CoopRegistry = new();
        public override BaseAddonDeed Deed { get { return new ChickenCoopDeed(); } }

        private List<BaseCreature> m_Stabled;

        public List<BaseCreature> Stabled { get { Defrag(); return m_Stabled; } }

        #region Stabled Accessors

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Stabled1 { get { return Get(0); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Stabled2 { get { return Get(1); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Stabled3 { get { return Get(2); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Stabled4 { get { return Get(3); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Stabled5 { get { return Get(4); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Stabled6 { get { return Get(5); } set { } }

        private BaseCreature Get(int index)
        {
            if (index >= 0 && index < m_Stabled.Count)
                return m_Stabled[index];

            return null;
        }

        #endregion

        [Constructable]
        public ChickenCoopAddon()
            : base()
        {
            CoopRegistry.Add(this);
            // structure
            AddComponent(new AddonComponent(0x13), -1, -1, 0);
            AddComponent(new AddonComponent(0x12), 0, -1, 0);
            AddComponent(new AddonComponent(0x12), 1, -1, 0);
            AddComponent(new AddonComponent(0x11), -1, 0, 0);
            AddComponent(new AddonComponent(0x11), 1, 0, 0);
            AddComponent(new AddonComponent(0x11), -1, 1, 0);
            AddComponent(new AddonComponent(0x10), 1, 1, 0);

            // gate
            AddComponent(new AddonComponent(0x866), 0, 1, 0);

            // roof
            AddComponent(new AddonComponent(0x26FA), -1, -1, 1);
            AddComponent(new AddonComponent(0x26F8), 0, -1, 4);
            AddComponent(new AddonComponent(0x26F9), 1, -1, 1);
            AddComponent(new AddonComponent(0x26FA), -1, 0, 1);
            AddComponent(new AddonComponent(0x26F8), 0, 0, 4);
            AddComponent(new AddonComponent(0x26F9), 1, 0, 1);
            AddComponent(new AddonComponent(0x26FA), -1, 1, 1);
            AddComponent(new AddonComponent(0x26F8), 0, 1, 4);
            AddComponent(new AddonComponent(0x26F9), 1, 1, 1);

            m_Stabled = new List<BaseCreature>();
        }

        public override void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
            BeginClaimList(from);
        }

        public override bool HandlesOnSpeech { get { return true; } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!e.Handled && from.InRange(this, UseRange) && from.Alive && IsAccessibleTo(from))
            {
                if (!e.Handled && e.HasKeyword(0x0008)) // *stable*
                {
                    e.Handled = true;

                    from.CloseGump(typeof(ClaimListGump));

                    BeginStable(from);
                }
                else if (!e.Handled && e.HasKeyword(0x0009)) // *claim*
                {
                    e.Handled = true;

                    from.CloseGump(typeof(ClaimListGump));

                    BeginClaimList(from);
                }
            }

            base.OnSpeech(e);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            // Clilocs are not supported
#if false
            if (from.Alive)
            {
                list.Add(new StableCME());
                list.Add(new ClaimCME());
            }
#endif

            base.GetContextMenuEntries(from, list);
        }

        private class StableCME : ContextMenuEntry
        {
            public StableCME()
                : base(1112556, UseRange) // Stable a chicken 
            {
            }

            public override void OnClick()
            {
                ((ChickenCoopAddon)Owner.Target).BeginStable(Owner.From);
            }
        }

        private class ClaimCME : ContextMenuEntry
        {
            public ClaimCME()
                : base(1112557, UseRange) // Claim a chicken
            {
            }

            public override void OnClick()
            {
                ((ChickenCoopAddon)Owner.Target).BeginClaimList(Owner.From);
            }
        }

        private void BeginStable(Mobile from)
        {
            if (Deleted || from.Map != this.Map || !from.InRange(this, UseRange) || !from.CheckAlive() || !IsAccessibleTo(from))
                return;

            from.SendMessage("Which chicken do you wish to stable?"); // Cliloc: 1112559
            from.Target = new StableTarget(this);
        }

        private void EndStable(Mobile from, object targeted)
        {
            if (Deleted || from.Map != this.Map || !from.InRange(this, UseRange) || !from.CheckAlive() || !IsAccessibleTo(from))
                return;

            BaseCreature pet = targeted as BaseCreature;

            if (pet == null)
            {
                from.SendLocalizedMessage(1048053); // You can't stable that!
            }
            else if (!pet.Controlled || pet.ControlMaster != from)
            {
                from.SendLocalizedMessage(1042562); // You do not own that pet!
            }
            else if (!(pet is Chicken))
            {
                from.SendMessage("You may only stable chickens in the chicken coop."); // Cliloc: 1112558
            }
            else if (Stabled.Count >= MaxSlots)
            {
                from.SendMessage("The chicken coop is full!");
            }
            else
            {
                pet.Internalize();

                pet.SetControlMaster(null);

                m_Stabled.Add(pet);

                pet.IsCoopStabled = true;
            }
        }

        private void BeginClaimList(Mobile from)
        {
            if (Deleted || from.Map != this.Map || !from.InRange(this, UseRange) || !from.CheckAlive() || !IsAccessibleTo(from))
                return;

            from.CloseGump(typeof(ClaimListGump));
            from.SendGump(new ClaimListGump(this));
        }

        private void EndClaimList(Mobile from, BaseCreature pet)
        {
            if (Deleted || from.Map != this.Map || !from.InRange(this, UseRange) || !from.CheckAlive() || !IsAccessibleTo(from) || !Stabled.Contains(pet))
                return;

            if (from.FollowerCount + pet.ControlSlots > from.FollowersMax)
            {
                from.SendMessage("{0} remained in the coop because you have too many followers.", pet.Name);
            }
            else
            {
                pet.SetControlMaster(from);
                pet.ControlTarget = from;
                pet.ControlOrder = OrderType.Follow;

                pet.MoveToWorld(from.Location, from.Map);
                m_Stabled.Remove(pet);

                pet.IsCoopStabled = false;
                // don't remove from the pet cache here as the previous call to SetControlMaster just set it
            }
        }

        private class StableTarget : Target
        {
            private ChickenCoopAddon m_Owner;

            public StableTarget(ChickenCoopAddon owner)
                : base(UseRange, false, TargetFlags.None)
            {
                m_Owner = owner;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                m_Owner.EndStable(from, targeted);
            }
        }

        private class ClaimListGump : Gump
        {
            private ChickenCoopAddon m_Owner;

            public ClaimListGump(ChickenCoopAddon owner)
                : base(50, 50)
            {
                m_Owner = owner;

                List<BaseCreature> list = m_Owner.Stabled;

                AddBackground(0, 0, 325, 50 + 20 * list.Count, 9250);
                AddAlphaRegion(5, 5, 315, 40 + 20 * list.Count);

                AddHtml(15, 15, 275, 20, "<BASEFONT COLOR=#FFFFFF>Select a pet to retrieve from the coop:</BASEFONT>", false, false);

                for (int i = 0; i < list.Count; i++)
                {
                    BaseCreature pet = list[i];

                    AddButton(15, 39 + 20 * i, 10006, 10006, i + 1, GumpButtonType.Reply, 0);
                    AddHtml(32, 35 + 20 * i, 275, 18, String.Format("<BASEFONT COLOR=#C0C0EE>{0}</BASEFONT>", pet.Name), false, false);
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                List<BaseCreature> list = m_Owner.Stabled;

                int index = info.ButtonID - 1;

                if (index >= 0 && index < list.Count)
                    m_Owner.EndClaimList(sender.Mobile, list[index]);
            }
        }

        public override void OnDelete()
        {
            CoopRegistry.Remove(this);
            for (int i = m_Stabled.Count - 1; i >= 0; i--)
            {
                BaseCreature pet = m_Stabled[i];

                pet.IsCoopStabled = false;

                pet.MoveToWorld(Location, Map);

                m_Stabled.RemoveAt(i);
            }

            base.OnDelete();
        }

        private void Defrag()
        {
            if (m_Stabled != null)
                for (int i = m_Stabled.Count - 1; i >= 0; i--)
                {
                    BaseCreature pet = m_Stabled[i];

                    if (pet.Deleted)
                    {
                        pet.IsCoopStabled = false;
                        m_Stabled.RemoveAt(i);
                    }
                }
        }

        public ChickenCoopAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.WriteMobileList(m_Stabled);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Stabled = reader.ReadStrongMobileList<BaseCreature>();
                        break;
                    }
            }

            foreach (BaseCreature bc in m_Stabled)
                bc.IsCoopStabled = true;

            CoopRegistry.Add(this);
        }
    }

    public class ChickenCoopDeed : BaseAddonDeed
    {
        public override BaseAddon Addon { get { return new ChickenCoopAddon(); } }
        public override string DefaultName { get { return "a chicken coop deed"; } }

        [Constructable]
        public ChickenCoopDeed()
            : base()
        {
        }

        public ChickenCoopDeed(Serial serial)
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
        }
    }
}