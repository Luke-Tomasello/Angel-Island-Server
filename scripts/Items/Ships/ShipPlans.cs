using Server.Gumps;
using Server.Multis;
using System;

namespace Server.Items
{
    public enum BoatType
    {
        SmallBoat,
        SmallDragonBoat,
        MediumBoat,
        MediumDragonBoat,
        LargeBoat,
        LargeDragonBoat
    }

    [Flags]
    public enum ShipAssemblyParts
    {
        None = 0x0000,

        Hull = 0x0001,
        Sails = 0x0002,
        Rigging = 0x0004,
        Hardware = 0x0008,
        Sealant = 0x0010,
        Navigation = 0x0020,
    }

    [Flipable(5361, 5362)]
    public class ShipPlans : Item
    {
        private BoatType m_BoatType;
        private ShipAssemblyParts m_Parts;

        [CommandProperty(AccessLevel.GameMaster)]
        public BoatType BoatType
        {
            get { return m_BoatType; }
            set { m_BoatType = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public ShipAssemblyParts Parts
        {
            get { return m_Parts; }
            set { m_Parts = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsComplete
        {
            get { return (m_Parts & RequiredParts) == RequiredParts; }
        }

        public ShipAssemblyParts RequiredParts
        {
            get { return GetRequiredParts(m_BoatType); }
        }

        [Constructable]
        public ShipPlans()
            : this(BoatType.SmallBoat)
        {
        }

        [Constructable]
        public ShipPlans(BoatType boatType)
            : base(5361)
        {
            //Name = "ship plans";
            Name = string.Format("{0} plans", GetBoatTypeName(boatType));
            Weight = 1.0;

            m_BoatType = boatType;
            m_Parts = ShipAssemblyParts.None;
        }

        public bool RequiresPart(ShipAssemblyParts part)
        {
            return (RequiredParts & part) != 0;
        }

        public bool HasPart(ShipAssemblyParts part)
        {
            return (m_Parts & part) != 0;
        }

        public bool NeedsPart(ShipAssemblyParts part)
        {
            return RequiresPart(part) && !HasPart(part);
        }

        public bool AddPart(ShipAssemblyParts part)
        {
            if (!NeedsPart(part))
                return false;

            m_Parts |= part;

            InvalidateProperties();

            return true;
        }

        public static ShipAssemblyParts GetRequiredParts(BoatType boatType)
        {
            switch (boatType)
            {
                case BoatType.SmallBoat:
                case BoatType.SmallDragonBoat:
                    return ShipAssemblyParts.Hull
                         | ShipAssemblyParts.Sails
                         | ShipAssemblyParts.Hardware;

                case BoatType.MediumBoat:
                case BoatType.MediumDragonBoat:
                    return ShipAssemblyParts.Hull
                         | ShipAssemblyParts.Sails
                         | ShipAssemblyParts.Rigging
                         | ShipAssemblyParts.Hardware
                         | ShipAssemblyParts.Sealant;

                case BoatType.LargeBoat:
                case BoatType.LargeDragonBoat:
                    return ShipAssemblyParts.Hull
                         | ShipAssemblyParts.Sails
                         | ShipAssemblyParts.Rigging
                         | ShipAssemblyParts.Hardware
                         | ShipAssemblyParts.Sealant
                         | ShipAssemblyParts.Navigation;
            }

            return ShipAssemblyParts.None;
        }
        // ===================================================================
        // In ShipPlans class
        // ===================================================================

        public override void OnSingleClick(Mobile from)
        {
            if (from == null)
                return;

            string boatName = GetBoatTypeName(m_BoatType);
            string status;

            if (IsComplete)
                status = "(completed)";
            else if (m_Parts == ShipAssemblyParts.None)
                status = "(not started)";
            else
                status = $"({CompletionPercent}% complete)";

            LabelTo(from, $"ship plans for a {boatName}");
            LabelTo(from, status);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!from.InRange(GetWorldLocation(), 3))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            from.SendGump(new ShipPlansGump(this));
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int CompletionPercent
        {
            get
            {
                if (IsComplete)
                    return 100;

                ShipAssemblyParts required = RequiredParts;
                if (required == ShipAssemblyParts.None)
                    return 0;

                int totalParts = 0;
                int completedParts = 0;

                foreach (ShipAssemblyParts part in Enum.GetValues(typeof(ShipAssemblyParts)))
                {
                    if (part == ShipAssemblyParts.None)
                        continue;

                    if ((required & part) != 0)
                    {
                        totalParts++;
                        if ((m_Parts & part) != 0)
                            completedParts++;
                    }
                }

                return totalParts == 0 ? 0 : (completedParts * 100) / totalParts;
            }
        }

        private static string GetBoatTypeName(BoatType type)
        {
            return type switch
            {
                BoatType.SmallBoat => "small boat",
                BoatType.SmallDragonBoat => "small dragon boat",
                BoatType.MediumBoat => "medium boat",
                BoatType.MediumDragonBoat => "medium dragon boat",
                BoatType.LargeBoat => "large boat",
                BoatType.LargeDragonBoat => "large dragon boat",
                _ => "boat"
            };
        }
        public ShipPlans(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write(0); // version

            writer.Write((int)m_BoatType);
            writer.Write((int)m_Parts);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    m_BoatType = (BoatType)reader.ReadInt();
                    m_Parts = (ShipAssemblyParts)reader.ReadInt();
                    break;
            }
        }

        #region Costs
        public static int GetRequiredAmount(BoatType? boatType, ShipAssemblyParts part, Type resourceType)
        {
            switch (part)
            {
                case ShipAssemblyParts.Sails:
                    if (resourceType == typeof(Cloth))
                    {
                        switch (boatType)
                        {
                            case BoatType.SmallBoat: return 500;
                            case BoatType.SmallDragonBoat: return 650;
                            case BoatType.MediumBoat: return 800;
                            case BoatType.MediumDragonBoat: return 950;
                            case BoatType.LargeBoat: return 1100;
                            case BoatType.LargeDragonBoat: return 1300;
                        }

                        throw new ArgumentOutOfRangeException(nameof(boatType), boatType, "Unsupported boat type for sails.");
                    }

                    if (resourceType == typeof(SpoolOfThread))
                    {
                        switch (boatType)
                        {
                            case BoatType.SmallBoat: return 5;
                            case BoatType.SmallDragonBoat: return 6;
                            case BoatType.MediumBoat: return 8;
                            case BoatType.MediumDragonBoat: return 10;
                            case BoatType.LargeBoat: return 12;
                            case BoatType.LargeDragonBoat: return 15;
                        }

                        throw new ArgumentOutOfRangeException(nameof(boatType), boatType, "Unsupported boat type for sails.");
                    }

                    break;

                case ShipAssemblyParts.Hull:
                    if (resourceType == typeof(Board))
                    {
                        switch (boatType)
                        {
                            case BoatType.SmallBoat: return 500;
                            case BoatType.SmallDragonBoat: return 650;
                            case BoatType.MediumBoat: return 800;
                            case BoatType.MediumDragonBoat: return 950;
                            case BoatType.LargeBoat: return 1100;
                            case BoatType.LargeDragonBoat: return 1300;
                        }

                        throw new ArgumentOutOfRangeException(nameof(boatType), boatType, "Unsupported boat type for hull.");
                    }

                    break;

                case ShipAssemblyParts.Rigging:
                    if (resourceType == typeof(MarineRope))
                        return 25;

                    if (resourceType == typeof(MarinePulley))
                        return 8;

                    break;

                case ShipAssemblyParts.Hardware:
                    if (resourceType == typeof(IronIngot))
                        return 75;

                    if (resourceType == typeof(Board))
                        return 10;

                    break;

                case ShipAssemblyParts.Sealant:
                    if (resourceType == typeof(Beeswax))
                        return 10;

                    if (resourceType == typeof(SpidersSilk))
                        return 25;

                    break;

                case ShipAssemblyParts.Navigation:
                    if (resourceType == typeof(WorldMap))
                        return 1;

                    if (resourceType == typeof(Gears))
                        return 10;

                    if (resourceType == typeof(SextantParts))
                        return 1;

                    if (resourceType == typeof(ClockParts))
                        return 1;

                    break;
            }

            throw new ArgumentException(
                string.Format("Resource type {0} is not valid for ship part {1}.", resourceType.Name, part),
                nameof(resourceType));
        }

        public static double GetMinSkill(BoatType? boatType, ShipAssemblyParts part)
        {
            if (boatType == null)
            {
                switch (part)
                {
                    case ShipAssemblyParts.Rigging: return 70.0;
                    case ShipAssemblyParts.Hardware: return 75.0;
                    case ShipAssemblyParts.Sealant: return 80.0;
                    case ShipAssemblyParts.Navigation: return 85.0;
                }

                return 0.0;
            }

            switch (boatType.Value)
            {
                case BoatType.SmallBoat: return 65.0;
                case BoatType.SmallDragonBoat: return 72.5;
                case BoatType.MediumBoat: return 80.0;
                case BoatType.MediumDragonBoat: return 87.5;
                case BoatType.LargeBoat: return 95.0;
                case BoatType.LargeDragonBoat: return 99.0;
            }

            return 0.0;
        }

        public static double GetMaxSkill(BoatType? boatType, ShipAssemblyParts part)
        {
            if (boatType == null)
            {
                switch (part)
                {
                    case ShipAssemblyParts.Rigging: return 95.0;
                    case ShipAssemblyParts.Hardware: return 100.0;
                    case ShipAssemblyParts.Sealant: return 105.0;
                    case ShipAssemblyParts.Navigation: return 110.0;
                }

                return 100.0;
            }

            switch (boatType.Value)
            {
                case BoatType.SmallBoat: return 90.0;
                case BoatType.SmallDragonBoat: return 95.0;
                case BoatType.MediumBoat: return 100.0;
                case BoatType.MediumDragonBoat: return 105.0;
                case BoatType.LargeBoat: return 110.0;
                case BoatType.LargeDragonBoat: return 120.0;
            }

            return 100.0;
        }
        #endregion Costs

        #region Craft

        public static ShipPlans? Find(Mobile from, BoatType? boatType, ShipAssemblyParts needShipPart)
        {
            if (from == null || from.Backpack == null)
                return null;

            foreach (Item item in from.Backpack.Items)
            {
                ShipPlans? plans = item as ShipPlans;

                if (plans == null)
                    continue;

                if (boatType != null && plans.BoatType != boatType.Value)
                    continue;

                if (plans.NeedsPart(needShipPart))
                    return plans;
            }

            return null;
        }

        public static ShipPlans? FindCompleted(Mobile from)
        {
            if (from == null || from.Backpack == null)
                return null;

            foreach (Item item in from.Backpack.Items)
            {
                ShipPlans? plans = item as ShipPlans;

                if (plans == null)
                    continue;

                if (plans.IsComplete)
                    return plans;
            }

            return null;
        }

        public bool Finish(Mobile from)
        {
            if (from == null || from.Backpack == null)
                return false;

            if (!IsComplete)
                return false;

            Item ship = CreateShipModel();

            if (ship == null)
                return false;

            from.Backpack.DropItem(ship);

            from.SendMessage("You assemble the completed ship.");

            Delete();

            return true;
        }

        private Item CreateShipModel()
        {
            BaseBoat? boat = null;
            BaseDockedBoat? model = null;

            switch (m_BoatType)
            {
                case BoatType.SmallBoat:
                    boat = new SmallBoat();
                    model = new SmallDockedBoat(boat);
                    break;

                case BoatType.SmallDragonBoat:
                    boat = new SmallDragonBoat();
                    model = new SmallDockedDragonBoat(boat);
                    break;

                case BoatType.MediumBoat:
                    boat = new MediumBoat();
                    model = new MediumDockedBoat(boat);
                    break;

                case BoatType.MediumDragonBoat:
                    boat = new MediumDragonBoat();
                    model = new MediumDockedDragonBoat(boat);
                    break;

                case BoatType.LargeBoat:
                    boat = new LargeBoat();
                    model = new LargeDockedBoat(boat);
                    break;

                case BoatType.LargeDragonBoat:
                    boat = new LargeDragonBoat();
                    model = new LargeDockedDragonBoat(boat);
                    break;
            }

            if (boat != null)
                boat.Delete();

            return model;
        }
        #endregion Craft

        #region Gump

        private class ShipPlansGump : Gump
        {
            public ShipPlansGump(ShipPlans plans) : base(100, 100)
            {
                AddPage(0);
                AddBackground(0, 0, 320, 240, 5054);
                AddBackground(10, 10, 300, 220, 3000);

                // Title - Title Case + Centered
                string title = GetTitleCaseBoatName(plans.BoatType);
                AddHtml(20, 20, 280, 30, $"<center>{title} Plans</center>", false, false);

                int y = 65;

                AddPartLine(plans, ShipAssemblyParts.Hull, "Hull", ref y);
                AddPartLine(plans, ShipAssemblyParts.Sails, "Sails", ref y);
                AddPartLine(plans, ShipAssemblyParts.Rigging, "Rigging", ref y);
                AddPartLine(plans, ShipAssemblyParts.Hardware, "Hardware", ref y);
                AddPartLine(plans, ShipAssemblyParts.Sealant, "Sealant", ref y);
                AddPartLine(plans, ShipAssemblyParts.Navigation, "Navigation", ref y);

                // Footer - Right justified
                AddHtml(20, 200, 280, 20,
                    $"<div align=\"right\">Completion: {plans.CompletionPercent}%</div>",
                    false, false);
            }

            private void AddPartLine(ShipPlans plans, ShipAssemblyParts part, string name, ref int y)
            {
                if (!plans.RequiresPart(part))
                    return;

                bool has = plans.HasPart(part);
                string status = has
                    ? "[<basefont color=#00FF00>Complete</basefont>]"
                    : "[<basefont color=#FF0000>Needed</basefont>]";

                AddHtml(40, y, 240, 20, $"{status} {name}", false, false);
                y += 25;
            }

            private static string GetTitleCaseBoatName(BoatType type)
            {
                return type switch
                {
                    BoatType.SmallBoat => "Small Boat",
                    BoatType.SmallDragonBoat => "Small Dragon Boat",
                    BoatType.MediumBoat => "Medium Boat",
                    BoatType.MediumDragonBoat => "Medium Dragon Boat",
                    BoatType.LargeBoat => "Large Boat",
                    BoatType.LargeDragonBoat => "Large Dragon Boat",
                    _ => "Boat"
                };
            }
        }
        #endregion Gump
    }
}