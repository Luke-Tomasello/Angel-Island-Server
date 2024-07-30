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

/* Scripts\Engines\Invasion\InvasionSystem.cs
 * Changelog:
 *  10/7/23, Yoar
 *      Initial version.
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Engines.PartySystem;
using Server.Gumps;
using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Server.Engines.Invasion
{
    public enum InvasionType
    {
        None,
        Undead,
    }

    public abstract class InvasionSystem
    {
        private static readonly Dictionary<InvasionType, InvasionSystem> m_Registry = new Dictionary<InvasionType, InvasionSystem>();

        public static Dictionary<InvasionType, InvasionSystem> Registry { get { return m_Registry; } }

        public static void Configure()
        {
            #region Dynamic Registration

            foreach (Assembly asm in ScriptCompiler.Assemblies)
            {
                foreach (Type type in asm.GetTypes())
                {
                    if (typeof(InvasionSystem).IsAssignableFrom(type) && !type.IsAbstract)
                    {
                        InvasionSystem system = null;

                        try
                        {
                            system = Activator.CreateInstance(type) as InvasionSystem;
                        }
                        catch
                        {
                        }

                        if (system != null)
                            Register(system);
                    }
                }
            }

            #endregion
        }

        public static void Initialize()
        {
            InvasionPersistence.EnsureExistence();

            CommandSystem.Register("InvasionState", AccessLevel.GameMaster, new CommandEventHandler(InvasionState_OnCommand));
            CommandSystem.Register("InvasionClear", AccessLevel.Administrator, new CommandEventHandler(InvasionClear_OnCommand));

            TargetCommands.Register(new InvasionPlayerCommand());
        }

        #region Commands

        [Usage("InvasionState <type>")]
        [Description("Displays the invasion state object.")]
        private static void InvasionState_OnCommand(CommandEventArgs e)
        {
            InvasionType type;
            InvasionSystem system;

            if (!Enum.TryParse(e.GetString(0), out type) || !m_Registry.TryGetValue(type, out system))
            {
                e.Mobile.SendMessage("Invalid invasion type.");
                return;
            }

            e.Mobile.SendGump(new PropertiesGump(e.Mobile, system.State));
        }

        [Usage("InvasionClear <type>")]
        [Description("Clears the invasion state object.")]
        private static void InvasionClear_OnCommand(CommandEventArgs e)
        {
            InvasionType type;
            InvasionSystem system;

            if (!Enum.TryParse(e.GetString(0), out type) || !m_Registry.TryGetValue(type, out system))
            {
                e.Mobile.SendMessage("Invalid invasion type.");
                return;
            }

            system.State.Clear();
            e.Mobile.SendMessage("Cleared invasion state.");
        }

        private class InvasionPlayerCommand : BaseCommand
        {
            public InvasionPlayerCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "InvasionPlayer" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "InvasionPlayer";
                Description = "Displays the invasion player object for a targeted mobile.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                Mobile m = obj as Mobile;

                if (m == null)
                    return; // sanity

                InvasionType type;
                InvasionSystem system;

                if (!Enum.TryParse(e.GetString(0), out type) || !m_Registry.TryGetValue(type, out system))
                {
                    LogFailure("Invalid invasion type.");
                    return;
                }

                InvasionPlayer player = system.State.GetPlayer(m, false);

                if (player == null)
                {
                    LogFailure("They have no invasion player object.");
                    return;
                }

                e.Mobile.SendGump(new PropertiesGump(e.Mobile, player));
            }
        }

        #endregion

        public static void Register(InvasionSystem system)
        {
            m_Registry[system.Type] = system;
        }

        public static InvasionSystem Get(InvasionType type)
        {
            InvasionSystem system;

            if (!m_Registry.TryGetValue(type, out system))
                return null;

            return system;
        }

        public abstract InvasionType Type { get; }

        private int m_PetDamageScalar;
        private int m_SummonDamageScalar;
        private int m_BardDamageScalar;
        private int m_FactionScalar;
        private double m_ArtifactChance;
        private string m_ArtifactMessage;
        private double m_LootChance;
        private bool m_FameScaling;
        private bool m_PartySharing;

        private int[] m_Tiers;
        private CreatureDefinition[] m_CreatureDefs;
        private ArtifactDefinition[] m_ArtifactDefs;
        private LootDefinition[] m_LootDefs;

        private InvasionState m_State;

        public int PetDamageScalar { get { return m_PetDamageScalar; } set { m_PetDamageScalar = value; } }
        public int SummonDamageScalar { get { return m_SummonDamageScalar; } set { m_SummonDamageScalar = value; } }
        public int BardDamageScalar { get { return m_BardDamageScalar; } set { m_BardDamageScalar = value; } }
        public int FactionScalar { get { return m_FactionScalar; } set { m_FactionScalar = value; } }
        public double ArtifactChance { get { return m_ArtifactChance; } set { m_ArtifactChance = value; } }
        public string ArtifactMessage { get { return m_ArtifactMessage; } set { m_ArtifactMessage = value; } }
        public double LootChance { get { return m_LootChance; } set { m_LootChance = value; } }
        public bool FameScaling { get { return m_FameScaling; } set { m_FameScaling = value; } }
        public bool PartySharing { get { return m_PartySharing; } set { m_PartySharing = value; } }

        public int[] Tiers { get { return m_Tiers; } set { m_Tiers = value; } }
        public CreatureDefinition[] CreatureDefs { get { return m_CreatureDefs; } set { m_CreatureDefs = value; } }
        public ArtifactDefinition[] ArtifactDefs { get { return m_ArtifactDefs; } set { m_ArtifactDefs = value; } }
        public LootDefinition[] LootDefs { get { return m_LootDefs; } set { m_LootDefs = value; } }

        public InvasionState State { get { return m_State; } }

        public InvasionSystem()
        {
            m_PetDamageScalar = 100;
            m_SummonDamageScalar = 100;
            m_BardDamageScalar = 100;
            m_FactionScalar = 100;

            m_Tiers = new int[0];
            m_CreatureDefs = new CreatureDefinition[0];
            m_ArtifactDefs = new ArtifactDefinition[0];
            m_LootDefs = new LootDefinition[0];

            m_State = new InvasionState();
        }

        public int GetTier(int kills)
        {
            int tier = 0;

            while (tier < m_Tiers.Length && kills >= m_Tiers[tier])
            {
                kills -= m_Tiers[tier];
                tier++;
            }

            return tier;
        }

        public int GetCreatureTier(Type creatureType)
        {
            for (int i = 0; i < m_CreatureDefs.Length; i++)
            {
                CreatureDefinition def = m_CreatureDefs[i];

                if (def.CreatureType.IsAssignableFrom(creatureType))
                    return def.Tier;
            }

            return -1;
        }

        public void HandleKill(BaseCreature killed, List<Mobile> eligible)
        {
            List<DamageResult> results = new List<DamageResult>();
            int totalDamage = 0;

            foreach (DamageEntry de in killed.DamageEntries)
            {
                if (de.Damager == null || de.HasExpired)
                    continue;

                Mobile damager = de.Damager;
                int damage = de.DamageGiven;

                Mobile master = damager.GetDamageMaster(killed);

                if (master != null)
                {
                    if (damager is BaseCreature)
                    {
                        BaseCreature bc = (BaseCreature)damager;

                        int scalar;

                        if (bc.Controlled)
                            scalar = m_PetDamageScalar;
                        else if (bc.Summoned)
                            scalar = m_SummonDamageScalar;
                        else if (bc.BardProvoked)
                            scalar = m_BardDamageScalar;
                        else
                            scalar = 100;

                        damage = scalar * damage / 100;
                    }

                    damager = master;
                }

                if (m_FactionScalar != 100 && IsFactioner(damager))
                    damage = m_FactionScalar * damage / 100;

                if (damage > 0)
                {
                    DamageResult result = null;

                    for (int i = 0; result == null && i < results.Count; i++)
                    {
                        DamageResult check = results[i];

                        if (check.Mobile == damager)
                            result = check;
                    }

                    if (result == null)
                        results.Add(result = new DamageResult(damager));

                    result.Damage += damage;
                    totalDamage += damage;
                }
            }

            if (totalDamage <= 0 || results.Count == 0)
                return;

            int totalDamageCap = 2 * killed.HitsMaxSeed;

            double damageScalar = 1.0;

            if (totalDamage > totalDamageCap)
                damageScalar *= (double)totalDamageCap / totalDamage;

            double artifactScalar = (1.0 / results.Count);

            if (m_FameScaling)
                artifactScalar *= GetFameScalar(killed.Fame);

            foreach (DamageResult result in results)
            {
                int damagePoints = (int)(damageScalar * result.Damage);

                if (damagePoints > 0)
                {
                    InvasionPlayer player = m_State.GetPlayer(result.Mobile, true);

                    if (player != null)
                        player.DamagePoints += damagePoints;
                }

                double artifactPoints = artifactScalar * ((double)result.Damage / totalDamage) * m_ArtifactChance;

                artifactPoints = 1.482 * Math.Log(artifactPoints * artifactPoints + 1.0);

                if (m_PartySharing && result.Mobile.Party is Party)
                {
                    Party p = (Party)result.Mobile.Party;

                    List<Mobile> share = new List<Mobile>();

                    foreach (PartyMemberInfo mi in p.Members)
                    {
                        if (mi.Mobile == result.Mobile || mi.Mobile.InRange(result.Mobile, 18))
                            share.Add(mi.Mobile);
                    }

                    artifactPoints /= share.Count;

                    if (artifactPoints > 0.0)
                    {
                        foreach (Mobile m in share)
                        {
                            InvasionPlayer player = m_State.GetPlayer(m, true);

                            if (player != null)
                            {
                                player.ArtifactPoints += artifactPoints;

                                if (!eligible.Contains(m))
                                    eligible.Add(m);
                            }
                        }
                    }
                }
                else if (artifactPoints > 0.0)
                {
                    InvasionPlayer player = m_State.GetPlayer(result.Mobile, true);

                    if (player != null)
                    {
                        player.ArtifactPoints += artifactPoints;

                        if (!eligible.Contains(result.Mobile))
                            eligible.Add(result.Mobile);
                    }
                }
            }
        }

        private static bool IsFactioner(Mobile m)
        {
            return (Factions.Faction.Find(m) != null || Alignment.AlignmentSystem.Find(m) != Alignment.AlignmentType.None);
        }

        private class DamageResult
        {
            public Mobile Mobile;
            public int Damage;

            public DamageResult(Mobile m)
            {
                Mobile = m;
            }
        }

        public void DistributeArtifacts(List<Mobile> eligible, int tier)
        {
            /* The following causes one artifact role per party.
             * This reduces artifact rate for parties.
             */
#if false
            if (m_PartySharing)
            {
                List<Party> parties = new List<Party>();

                foreach (Mobile m in eligible)
                {
                    Party p = m.Party as Party;

                    if (p != null && !parties.Contains(p))
                        parties.Add(p);
                }

                foreach (Party p in parties)
                {
                    double totalArtifactPoints = 0.0;

                    foreach (PartyMemberInfo mi in p.Members)
                    {
                        InvasionPlayer player = m_State.GetPlayer(mi.Mobile, false);

                        if (player != null)
                            totalArtifactPoints += player.ArtifactPoints;
                    }

                    if (totalArtifactPoints > 0.0 && Utility.RandomDouble() < totalArtifactPoints)
                    {
                        ArtifactDefinition def = RandomArtifact(tier);

                        if (def != null)
                        {
                            foreach (PartyMemberInfo mi in p.Members)
                            {
                                InvasionPlayer player = m_State.GetPlayer(mi.Mobile, false);

                                if (player != null)
                                    player.ArtifactPoints = 0.0;
                            }

                            Mobile toGive = ((PartyMemberInfo)p.Members[Utility.Random(p.Members.Count)]).Mobile;

                            GiveArtifact(toGive, def);
                        }
                    }
                }
            }
#endif

            foreach (Mobile m in eligible)
            {
                InvasionPlayer player = m_State.GetPlayer(m, false);

                if (player != null && player.ArtifactPoints > 0.0 && Utility.RandomDouble() < player.ArtifactPoints)
                {
                    ArtifactDefinition def = RandomArtifact(tier);

                    if (def != null)
                    {
                        player.ArtifactPoints = 0.0;

                        GiveArtifact(m, def);
                    }
                }
            }
        }

        private void GiveArtifact(Mobile m, ArtifactDefinition def)
        {
            Item artifact = def.Construct();

            if (artifact == null)
                return;

            m.AddToBackpack(artifact);

            if (m_ArtifactMessage != null)
                m.SendMessage(m_ArtifactMessage);

            LogHelper logger = new LogHelper("Invasion.log", false, true);
            logger.Log(LogType.Mobile, m, String.Format("Received an artifact: {0} ({1}).", artifact.ToString(), artifact.GetType().Name));
            logger.Finish();
        }

        public ArtifactDefinition RandomArtifact(int tier)
        {
            int total = 0;

            for (int i = 0; i < m_ArtifactDefs.Length; i++)
            {
                ArtifactDefinition def = m_ArtifactDefs[i];

                if (tier >= def.ReqTier)
                    total += def.Weight;
            }

            if (total <= 0)
                return null;

            int rnd = Utility.Random(total);

            for (int i = 0; i < m_ArtifactDefs.Length; i++)
            {
                ArtifactDefinition def = m_ArtifactDefs[i];

                if (tier >= def.ReqTier)
                {
                    if (rnd < def.Weight)
                        return def;
                    else
                        rnd -= def.Weight;
                }
            }

            return null;
        }

        public void GenerateLoot(BaseCreature bc)
        {
            int tier = GetCreatureTier(bc.GetType());

            if (tier == -1)
                return;

            double lootChance = m_LootChance;

            if (m_FameScaling)
                lootChance *= GetFameScalar(bc.Fame);

            bool giveLoot = (Utility.RandomDouble() < lootChance);

            if (!giveLoot)
                return;

            LootDefinition def = RandomLoot(tier);

            if (def == null)
                return;

            Item loot = def.Construct();

            if (loot != null)
            {
                bc.PackItem(loot);

                LogHelper logger = new LogHelper("Invasion.log", false, true);
                logger.Log(LogType.Mobile, bc, String.Format("Received pack loot: {0} ({1}).", loot.ToString(), loot.GetType().Name));
                logger.Finish();
            }
        }

        public LootDefinition RandomLoot(int tier)
        {
            int total = 0;

            for (int i = 0; i < m_LootDefs.Length; i++)
            {
                LootDefinition def = m_LootDefs[i];

                if (tier >= def.ReqTier)
                    total += def.Weight;
            }

            if (total <= 0)
                return null;

            int rnd = Utility.Random(total);

            for (int i = 0; i < m_LootDefs.Length; i++)
            {
                LootDefinition def = m_LootDefs[i];

                if (tier >= def.ReqTier)
                {
                    if (rnd < def.Weight)
                        return def;
                    else
                        rnd -= def.Weight;
                }
            }

            return null;
        }

        private static double GetFameScalar(int fame)
        {
            return Math.Max(0.01, Math.Min(1.0, (double)fame / 24000));
        }

        public static T Construct<T>(Type type)
        {
            if (!typeof(T).IsAssignableFrom(type))
                return default(T);

            T result;

            try
            {
                result = (T)Activator.CreateInstance(type);
            }
            catch
            {
                result = default(T);
            }

            return result;
        }
    }

    public class CreatureDefinition
    {
        private Type m_CreatureType;
        private int m_Tier;
        private int m_Weight;

        public Type CreatureType { get { return m_CreatureType; } }
        public int Tier { get { return m_Tier; } }
        public int Weight { get { return m_Weight; } }

        public CreatureDefinition(int tier, int weight, Type creatureType)
        {
            m_CreatureType = creatureType;
            m_Tier = tier;
            m_Weight = weight;
        }

        public virtual BaseCreature Construct()
        {
            return InvasionSystem.Construct<BaseCreature>(m_CreatureType);
        }
    }

    public class ArtifactDefinition
    {
        private Type m_ItemType;
        private int m_ReqTier;
        private int m_Weight;

        public Type ItemType { get { return m_ItemType; } }
        public int ReqTier { get { return m_ReqTier; } }
        public int Weight { get { return m_Weight; } }

        public ArtifactDefinition(int reqTier, int weight, Type itemType)
        {
            m_ItemType = itemType;
            m_ReqTier = reqTier;
            m_Weight = weight;
        }

        public virtual Item Construct()
        {
            return InvasionSystem.Construct<Item>(m_ItemType);
        }
    }

    public class LootDefinition
    {
        private Type m_ItemType;
        private int m_ReqTier;
        private int m_Weight;

        public Type ItemType { get { return m_ItemType; } }
        public int ReqTier { get { return m_ReqTier; } }
        public int Weight { get { return m_Weight; } }

        public LootDefinition(int reqTier, int weight, Type itemType)
        {
            m_ItemType = itemType;
            m_ReqTier = reqTier;
            m_Weight = weight;
        }

        public virtual Item Construct()
        {
            return InvasionSystem.Construct<Item>(m_ItemType);
        }
    }

    [PropertyObject]
    public class InvasionState
    {
        private Dictionary<Mobile, InvasionPlayer> m_Players;
        private InvasionResults m_Results;

        public Dictionary<Mobile, InvasionPlayer> Players
        {
            get { return m_Players; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResults Results
        {
            get { return m_Results; }
            set { }
        }

        public InvasionState()
        {
            m_Players = new Dictionary<Mobile, InvasionPlayer>();
            m_Results = new InvasionResults(this);
        }

        public InvasionPlayer GetPlayer(Mobile m, bool create)
        {
            if (!m.Player)
                return null;

            InvasionPlayer player;

            if (m_Players.TryGetValue(m, out player))
                return player;

            if (create)
                m_Players[m] = player = new InvasionPlayer(m);

            return player;
        }

        public void Clear()
        {
            m_Players.Clear();
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write((int)m_Players.Count);

            foreach (KeyValuePair<Mobile, InvasionPlayer> kvp in m_Players)
            {
                writer.Write((Mobile)kvp.Key);
                kvp.Value.Serialize(writer);
            }
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        int playerCount = reader.ReadInt();

                        for (int i = 0; i < playerCount; i++)
                        {
                            Mobile m = reader.ReadMobile();

                            InvasionPlayer player = new InvasionPlayer(m);

                            player.Deserialize(reader);

                            if (m != null)
                                m_Players[m] = player;
                        }

                        break;
                    }
            }
        }

        public override string ToString()
        {
            return "...";
        }
    }

    [PropertyObject]
    public class InvasionPlayer
    {
        private Mobile m_Mobile;
        private int m_DamagePoints;
        private double m_ArtifactPoints;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Mobile
        {
            get { return m_Mobile; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DamagePoints
        {
            get { return m_DamagePoints; }
            set { m_DamagePoints = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double ArtifactPoints
        {
            get { return m_ArtifactPoints; }
            set { m_ArtifactPoints = value; }
        }

        public InvasionPlayer(Mobile m)
        {
            m_Mobile = m;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write((int)m_DamagePoints);
            writer.Write((double)m_ArtifactPoints);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_DamagePoints = reader.ReadInt();
                        m_ArtifactPoints = reader.ReadDouble();

                        break;
                    }
            }
        }

        public override string ToString()
        {
            return String.Format("{0}: {1}", m_Mobile, m_DamagePoints);
        }
    }

    [PropertyObject]
    public class InvasionResults
    {
        private InvasionState m_State;
        private InvasionResult[] m_Results;

        public InvasionState State { get { return m_State; } }
        public InvasionResult[] Results { get { return m_Results; } }

        #region Result Accessors

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result0 { get { return GetResult(0); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result1 { get { return GetResult(1); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result2 { get { return GetResult(2); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result3 { get { return GetResult(3); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result4 { get { return GetResult(4); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result5 { get { return GetResult(5); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result6 { get { return GetResult(6); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result7 { get { return GetResult(7); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result8 { get { return GetResult(8); } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public InvasionResult Result9 { get { return GetResult(9); } set { } }

        private InvasionResult GetResult(int index)
        {
            if (index >= 0 && index < m_Results.Length)
                return m_Results[index];

            return InvasionResult.Empty;
        }

        #endregion

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Manual_Redraw
        {
            get { return false; }
            set
            {
                if (value)
                    Redraw();
            }
        }

        public InvasionResults(InvasionState state)
        {
            m_State = state;
            m_Results = new InvasionResult[0];
        }

        public void Redraw()
        {
            List<InvasionResult> list = new List<InvasionResult>();

            foreach (KeyValuePair<Mobile, InvasionPlayer> kvp in m_State.Players)
                list.Add(new InvasionResult(kvp.Key, kvp.Value.DamagePoints));

            list.Sort();

            m_Results = list.ToArray();
        }

        public override string ToString()
        {
            return "...";
        }
    }

    [PropertyObject]
    public struct InvasionResult : IComparable<InvasionResult>
    {
        public static readonly InvasionResult Empty = new InvasionResult();

        private Mobile m_Mobile;
        private int m_Score;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Mobile { get { return m_Mobile; } set { } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Score { get { return m_Score; } }

        public InvasionResult(Mobile m, int score)
        {
            m_Mobile = m;
            m_Score = score;
        }

        int IComparable<InvasionResult>.CompareTo(InvasionResult other)
        {
            return other.Score.CompareTo(m_Score);
        }

        public override string ToString()
        {
            return String.Format("{0}: {1}", m_Mobile, m_Score);
        }
    }
}