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

/* Scripts/Items/Addons/HomeHearth.cs
 * ChangeLog
 *	12/10/23, Yoar
 *		Initial version
 */

using Server.Network;
using Server.Targeting;
using System;

namespace Server.Items
{
    public class HomeHearthAddon : BaseAddon, IFireplace
    {
        public override BaseAddonDeed Deed { get { return new HomeHearthDeed(); } }
        public override bool Redeedable { get { return true; } }

        private int m_Fuel;
        private Timer m_BurnTimer;

        [CommandProperty(AccessLevel.GameMaster)]
        public int Fuel
        {
            get { return m_Fuel; }
            set { m_Fuel = value; }
        }

        public Timer BurnTimer
        {
            get { return m_BurnTimer; }
            set { m_BurnTimer = value; }
        }

        [Constructable]
        public HomeHearthAddon()
            : this(0)
        {
        }

        [Constructable]
        public HomeHearthAddon(bool east)
            : this(east ? 1 : 0)
        {
        }

        public HomeHearthAddon(int type)
        {
            switch (type)
            {
                case 0: // south
                    {
                        AddComponent(new FireplaceComponent(0x235F, 0x2360, LightType.Circle150), 0, 0, 0);
                        AddComponent(new FireplaceComponent(0x235E, 0x2366, LightType.Circle150), -1, 0, 0);
                        break;
                    }
                case 1: // east
                    {
                        AddComponent(new FireplaceComponent(0x2350, 0x2352, LightType.Circle150), 0, 0, 0);
                        AddComponent(new FireplaceComponent(0x2351, 0x2358, LightType.Circle150), 0, -1, 0);
                        break;
                    }
            }
        }

        public override void OnComponentDoubleClick(AddonComponent c, Mobile from)
        {
            if (!from.InRange(c.GetWorldLocation(), 2) || !from.InLOS(c))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            FireplaceHelper.BeginTarget(this, from);
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            FireplaceHelper.StopTimer(this);
        }

        public HomeHearthAddon(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_Fuel);
            writer.Write((bool)FireplaceHelper.IsBurning(this));
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Fuel = reader.ReadInt();

            if (reader.ReadBool())
                FireplaceHelper.StartTimer(this);
        }
    }

    public class HomeHearthDeed : BaseChoiceAddonDeed
    {
        public override BaseAddon Addon { get { return new HomeHearthAddon(m_Type); } }
        public override string DefaultName { get { return "home hearth"; } }

        private static readonly TextEntry[] m_Choices = new TextEntry[]
            {
                "Home Hearth (South)",
                "Home Hearth (East)",
            };

        public override TextEntry[] Choices { get { return m_Choices; } }

        [Constructable]
        public HomeHearthDeed()
        {
        }

        public HomeHearthDeed(Serial serial)
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

    public class FireplaceComponent : AddonComponent
    {
        private int m_UnlitItemID;
        private int m_LitItemID;

        [CommandProperty(AccessLevel.GameMaster)]
        public int UnlitItemID
        {
            get { return m_UnlitItemID; }
            set { m_UnlitItemID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int LitItemID
        {
            get { return m_LitItemID; }
            set { m_LitItemID = value; }
        }

        public FireplaceComponent(int unlitItemID, int litItemID, LightType light = 0)
            : base(unlitItemID)
        {
            m_UnlitItemID = unlitItemID;
            m_LitItemID = litItemID;
            Light = light;
        }

        public FireplaceComponent(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)m_UnlitItemID);
            writer.Write((int)m_LitItemID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_UnlitItemID = reader.ReadInt();
            m_LitItemID = reader.ReadInt();
        }
    }

    public interface IFireplace : IEntity
    {
        int Fuel { get; set; }
        Timer BurnTimer { get; set; }
    }

    public static class FireplaceHelper
    {
        public static int MaxFuel = 120;
        public static TimeSpan BurnInterval = TimeSpan.FromMinutes(1.0);

        public static void BeginTarget(IFireplace fireplace, Mobile from)
        {
            from.SendLocalizedMessage(1062013); // What do you want to put into the fireplace?
            from.Target = new BurnTarget(fireplace);
        }

        public static void AddFuel(IFireplace fireplace, int fuel)
        {
            Effects.PlaySound(fireplace.Location, fireplace.Map, 0x54);

            fireplace.Fuel += fuel;

            if (fireplace.Fuel > MaxFuel)
                fireplace.Fuel = MaxFuel;

            if (fireplace is BaseAddon)
                SetComponents((BaseAddon)fireplace, true);

            StartTimer(fireplace);
        }

        public static void Douse(IFireplace fireplace)
        {
            Effects.PlaySound(fireplace.Location, fireplace.Map, 0x4BB);

            fireplace.Fuel = 0;

            if (fireplace is BaseAddon)
                SetComponents((BaseAddon)fireplace, false);

            StopTimer(fireplace);
        }

        public static void SetComponents(BaseAddon addon, bool burning)
        {
            foreach (AddonComponent ac in addon.Components)
            {
                if (ac is FireplaceComponent)
                {
                    FireplaceComponent fp = (FireplaceComponent)ac;

                    if (burning)
                        fp.ItemID = fp.LitItemID;
                    else
                        fp.ItemID = fp.UnlitItemID;
                }
            }
        }

        public static bool IsBurning(IFireplace fireplace)
        {
            return (fireplace.BurnTimer != null && fireplace.BurnTimer.Running);
        }

        public static void StartTimer(IFireplace fireplace)
        {
            StopTimer(fireplace);

            (fireplace.BurnTimer = new BurnTimer(fireplace)).Start();
        }

        public static void StopTimer(IFireplace fireplace)
        {
            if (fireplace.BurnTimer != null)
            {
                fireplace.BurnTimer.Stop();
                fireplace.BurnTimer = null;
            }
        }

        private class BurnTarget : Target
        {
            private IFireplace m_Fireplace;

            public BurnTarget(IFireplace fireplace)
                : base(2, false, TargetFlags.None)
            {
                m_Fireplace = fireplace;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (!from.CheckAlive())
                    return;

                if (from.Map != m_Fireplace.Map || !from.InRange(m_Fireplace.Location, 3))
                {
                    from.SendLocalizedMessage(500295); // You are too far away to do that.
                    return;
                }

                if (targeted is Kindling || targeted is BaseBoard || targeted is BaseLog)
                {
                    if (m_Fireplace.Fuel >= MaxFuel && IsBurning(m_Fireplace))
                    {
                        from.SendLocalizedMessage(1062018); // The fireplace is full!  Wait for the wood to burn down before trying to add more.
                    }
                    else if (targeted is Kindling)
                    {
                        ((Item)targeted).Consume();
                        from.SendLocalizedMessage(1062015); // You put some kindling on the fire.  Logs would burn longer.

                        AddFuel(m_Fireplace, 15);
                    }
                    else if (targeted is BaseBoard)
                    {
                        ((Item)targeted).Consume();
                        from.SendLocalizedMessage(1062016); // You put some lumber on the fire.  Logs would burn longer...

                        AddFuel(m_Fireplace, 30);
                    }
                    else if (targeted is BaseLog)
                    {
                        ((Item)targeted).Consume();
                        from.SendLocalizedMessage(1062017); // You put a log on the fire.

                        AddFuel(m_Fireplace, 60);
                    }
                }
                else if (targeted is BaseBeverage)
                {
                    BaseBeverage bvrg = (BaseBeverage)targeted;

                    if (m_Fireplace.Fuel > 0 && bvrg.Content == BeverageType.Water && bvrg.Quantity >= 5)
                    {
                        bvrg.Quantity = 0;
                        from.SendLocalizedMessage(1062019); // You douse the flames.

                        Douse(m_Fireplace);
                    }
                    else
                    {
                        from.SendLocalizedMessage(1062020); // That has no effect.
                    }
                }
                else
                {
                    from.SendLocalizedMessage(1062014); // You can't burn that!
                }
            }
        }

        private class BurnTimer : Timer
        {
            private IFireplace m_Fireplace;

            public BurnTimer(IFireplace fireplace)
                : base(BurnInterval, BurnInterval)
            {
                m_Fireplace = fireplace;
            }

            protected override void OnTick()
            {
                if (m_Fireplace.Deleted)
                {
                    Stop();
                    return;
                }

                if (--m_Fireplace.Fuel <= 0)
                {
                    m_Fireplace.Fuel = 0;

                    if (m_Fireplace is BaseAddon)
                        SetComponents((BaseAddon)m_Fireplace, false);

                    StopTimer(m_Fireplace);
                }
            }
        }
    }
}