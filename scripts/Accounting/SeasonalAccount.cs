using System;
using System.Xml;

namespace Server.Accounting
{
    [NoSort]
    public class SeasonalAccount
    {
        public uint AccountGUID { get; private set; }

        // --- Display Identity ---
        [CommandProperty(AccessLevel.Administrator)]
        public string? DisplayName { get; set; }

        // --- Core tracking ---
        [CommandProperty(AccessLevel.Administrator)]
        public int SeasonPoints { get; set; }

        [CommandProperty(AccessLevel.Administrator)]
        public int LifetimeEarnedPoints { get; set; }

        [CommandProperty(AccessLevel.Administrator)]
        public int SeasonSpentPoints { get; set; }

        [CommandProperty(AccessLevel.Administrator)]
        public TimeSpan TotalPlayTime { get; set; }

        // --- Admin controls ---
        [CommandProperty(AccessLevel.Administrator)]
        public bool SeasonReset
        {
            get => false;
            set
            {
                if (value)
                {
                    SeasonPoints = 0;
                    SeasonSpentPoints = 0;

                    Server.Engines.Seasons.SeasonalEarningBaselineState.Reset(AccountGUID);

                    Console.WriteLine($"SeasonalAccount for GUID {AccountGUID} has been season-reset.");
                }
            }
        }

        [CommandProperty(AccessLevel.Administrator)]
        public bool FullReset
        {
            get => false;
            set
            {
                if (value)
                {
                    SeasonPoints = 0;
                    LifetimeEarnedPoints = 0;
                    SeasonSpentPoints = 0;
                    TotalPlayTime = TimeSpan.Zero;
                    DisplayName = null; // optional: clears identity on full reset

                    Server.Engines.Seasons.SeasonalEarningBaselineState.Reset(AccountGUID);

                    Console.WriteLine($"SeasonalAccount for GUID {AccountGUID} has been fully reset.");
                }
            }
        }

        // --- Ctors ---
        public SeasonalAccount(uint guid)
        {
            AccountGUID = guid;
        }

        public SeasonalAccount(XmlElement node)
        {
            AccountGUID = uint.Parse(node.GetAttribute("guid"));

            SeasonPoints = Utility.ToInt32(node.GetAttribute("seasonPoints"));
            LifetimeEarnedPoints = Utility.ToInt32(node.GetAttribute("lifetimeEarned"));
            SeasonSpentPoints = Utility.ToInt32(node.GetAttribute("seasonSpent"));

            TotalPlayTime = TimeSpan.FromSeconds(
                Utility.ToDouble(node.GetAttribute("playtime"))
            );

            // NEW (safe: missing attribute => null/empty)
            DisplayName = node.GetAttribute("displayName");
            if (string.IsNullOrWhiteSpace(DisplayName))
                DisplayName = null;
        }

        // --- Serialization ---
        public void Save(XmlTextWriter xml)
        {
            xml.WriteStartElement("account");

            xml.WriteAttributeString("guid", AccountGUID.ToString());
            xml.WriteAttributeString("seasonPoints", SeasonPoints.ToString());
            xml.WriteAttributeString("lifetimeEarned", LifetimeEarnedPoints.ToString());
            xml.WriteAttributeString("seasonSpent", SeasonSpentPoints.ToString());
            xml.WriteAttributeString("playtime", TotalPlayTime.TotalSeconds.ToString());

            // NEW (only write if present to keep XML clean)
            if (!string.IsNullOrWhiteSpace(DisplayName))
                xml.WriteAttributeString("displayName", DisplayName);

            xml.WriteEndElement();
        }
    }
}