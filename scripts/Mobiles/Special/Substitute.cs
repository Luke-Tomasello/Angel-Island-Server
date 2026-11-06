using Server.Engines;
using Server.Engines.DataRecorder;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Mobiles
{
    public class Substitute : PlayerMobile
    {
        public new static List<Substitute> Instances = new List<Substitute>();

        public Substitute(string name, uint accountGuid)
        {
            // Mortalis-only.
            // Substitute ownership is account-based rather than player-based.
            // This is appropriate for Mortalis' permadeath model, but is generally
            // incorrect on shards where multiple characters may exist on the same account.
            if (!Core.RuleSets.MortalisRules())
                throw new InvalidOperationException("Substitute mobiles are only supported on Mortalis.");

            Name = name;
            OwnerGUID = accountGuid;

            MoveToIntStorage();
            Instances.Add(this);

            Utility.Monitor.DebugOut($"{this} created.", ConsoleColor.Magenta);
        }

        public Substitute(Serial serial)
            : base(serial)
        {
            Instances.Add(this);
        }

        public override void OnDelete()
        {
            base.OnDelete();
            Instances.Remove(this);
            Utility.Monitor.DebugOut($"{this} deleted.", ConsoleColor.Magenta);
        }

        public static HashSet<Substitute> InUse()
        {
            HashSet<Substitute> inUse = new HashSet<Substitute>();

            // Bounty system
            foreach (Bounty b in BountyKeeper.Bounties.Cast<Bounty>())
            {
                if (b.Placer is Substitute sp && !sp.Deleted)
                    inUse.Add(sp);

                if (b.WantedPlayer is Substitute sw && !sw.Deleted)
                    inUse.Add(sw);
            }

            // DataRecorder: any recorded legacy bucket
            foreach (var kvp in DataRecorder.Database)
            {
                if (kvp.Key is Substitute sub && !sub.Deleted)
                {
                    foreach (RecorderDomain domain in Enum.GetValues(typeof(RecorderDomain)))
                    {
                        if (DataRecorder.Get(sub, domain) > 0)
                        {
                            inUse.Add(sub);
                            break;
                        }
                    }
                }
            }

            return inUse;
        }

        public static HashSet<Substitute> InUseFor(PlayerMobile player)
        {
            HashSet<Substitute> result = new HashSet<Substitute>();

            if (player == null)
                return result;

            uint ownerGuid = player.AcctGUID;

            if (ownerGuid == 0)
                return result;

            foreach (Substitute sub in InUse())
            {
                if (sub == null || sub.Deleted)
                    continue;

                if (sub.OwnerGUID == ownerGuid)
                    result.Add(sub);
            }

            return result;
        }

        public static Substitute? Find(uint accountGuid)
        {
            if (accountGuid == 0)
                return null;

            foreach (Substitute sub in Instances)
            {
                if (sub == null || sub.Deleted)
                    continue;

                if (sub.OwnerGUID == accountGuid)
                    return sub;
            }

            return null;
        }

        private static uint ParseAccountGuidFromLegacyIdentity(string? identity)
        {
            if (string.IsNullOrWhiteSpace(identity))
                return 0;

            int dot = identity.IndexOf('.');

            string accountGuidText = dot >= 0
                ? identity.Substring(0, dot)
                : identity;

            uint accountGuid;

            if (uint.TryParse(accountGuidText, out accountGuid))
                return accountGuid;

            return 0;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2);

            // version 2
            // OwnerGUID is serialized by Mobile.
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    break;

                case 1:
                    {
                        string? legacyIdentity = reader.ReadString();
                        OwnerGUID = ParseAccountGuidFromLegacyIdentity(legacyIdentity);
                        break;
                    }
            }
        }
    }
}