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

/* scripts\Mobiles\Special\MoongateWizard.cs
 * CHANGELOG 
 *  6/8/23, Yoar (Refactor)
 *      Global oracle destinations can now be added using 'RegisterDestination'
 *      Gate costs can now be configured and required
 *      Added option to spawn two-way moongates
 *  11/29/21, Adam
 *      Add Fire and Ice dungeons
 *  11/24/21, Adam
 *      Add support for townships
 *	8/30/21, Adam
 *		Created.
 */

using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Server.Mobiles
{
    [Flags]
    public enum OracleFlag : ushort
    {
        None = 0x0000,

        Moongate = 0x0001,
        BlueTown = 0x0002,
        RedTown = 0x0004,
        Dungeon = 0x0008,
        Township = 0x0010,
        Default = Moongate | RedTown | Dungeon | Township, // legacy oracle behavior
        MoongateTownship = Moongate | Township,
    }

    public interface IOracleDestination
    {
        Point3D DestLoc { get; }
        Map DestMap { get; }
        OracleFlag Flag { get; }

        bool Validate(MoonGateWizard oracle, Mobile from);
        bool WasNamed(SpeechEventArgs e);
        string Format();
    }

    public class MoonGateWizard : PlayerBarkeeper
    {
        const int GateTime = 30;            // how long the gate is open
        const int MemoryTime = 30;          // how long (seconds) we remember this player
        const int SensoryPerception = 4;

        public static new void Initialize()
        {
            CommandSystem.Register("OracleGen", AccessLevel.Administrator, new CommandEventHandler(OracleGen_OnCommand));

            // moongates
            RegisterDestination("Britain", new Point3D(1336, 1997, 5), Map.Felucca, OracleFlag.Moongate);
            RegisterDestination("Moonglow", new Point3D(4467, 1283, 5), Map.Felucca, OracleFlag.Moongate);
            RegisterDestination("Magincia", new Point3D(3563, 2139, 34), Map.Felucca, OracleFlag.Moongate);
            RegisterDestination("Skara Brae", new Point3D(643, 2067, 5), Map.Felucca, OracleFlag.Moongate, "Skara");
            RegisterDestination("Trinsic", new Point3D(1828, 2948, -20), Map.Felucca, OracleFlag.Moongate);
            RegisterDestination("Minoc", new Point3D(2701, 692, 5), Map.Felucca, OracleFlag.Moongate, "Vesper");
            RegisterDestination("Yew", new Point3D(771, 752, 5), Map.Felucca, OracleFlag.Moongate);
            RegisterDestination("Jhelom", new Point3D(1499, 3771, 5), Map.Felucca, OracleFlag.Moongate);

            // red towns
            RegisterDestination("Buccaneer's Den", new Point3D(2711, 2234, 0), Map.Felucca, OracleFlag.RedTown, "Buccaneers Den", "Bucc's Den", "Buccs Den", "Buccs");

            // dungeons
            RegisterDestination("Covetous", new Point3D(2499, 919, 0), Map.Felucca, OracleFlag.Dungeon);
            RegisterDestination("Deceit", new Point3D(4111, 432, 5), Map.Felucca, OracleFlag.Dungeon);
            RegisterDestination("Despise", new Point3D(1298, 1080, 0), Map.Felucca, OracleFlag.Dungeon);
            RegisterDestination("Destard", new Point3D(1176, 2637, 0), Map.Felucca, OracleFlag.Dungeon);
            RegisterDestination("Hythloth", new Point3D(4721, 3822, 0), Map.Felucca, OracleFlag.Dungeon);
            RegisterDestination("Shame", new Point3D(514, 1561, 0), Map.Felucca, OracleFlag.Dungeon);
            RegisterDestination("Wrong", new Point3D(2043, 238, 10), Map.Felucca, OracleFlag.Dungeon);
            RegisterDestination("Fire", new Point3D(2923, 3408, 8), Map.Felucca, OracleFlag.Dungeon);
            RegisterDestination("Ice", new Point3D(2000, 81, 4), Map.Felucca, OracleFlag.Dungeon);
        }

        private OracleFlag m_DestinationFlags;
        private bool m_UsePublicMoongates;

        private int m_Cost;
        private int m_DangerCost;
        private bool m_RequiresFunds;

        private bool m_TwoWay;

        private Timer m_CastTimer;      // our current cast timer
        private Timer m_GateTimer;      // our current gate timer
        private Moongate m_GateSource;  // our temporary moongate (source)
        private Moongate m_GateTarget;  // our temporary moongate (target)

        public override bool CanHearGhosts { get => true; set => base.CanHearGhosts = value; }
        public override bool CallsGuards { get { return false; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public OracleFlag DestinationFlags
        {
            get { return m_DestinationFlags; }
            set { m_DestinationFlags = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool UsePublicMoongates
        {
            get { return m_UsePublicMoongates; }
            set { m_UsePublicMoongates = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int Cost
        {
            get { return m_Cost; }
            set { m_Cost = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DangerCost
        {
            get { return m_DangerCost; }
            set { m_DangerCost = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool RequiresFunds
        {
            get { return m_RequiresFunds; }
            set { m_RequiresFunds = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool TwoWay
        {
            get { return m_TwoWay; }
            set { m_TwoWay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasGate
        {
            get { return (m_GateTimer != null); }
            set
            {
                if (!value)
                    ClearGates();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Moongate GateSource
        {
            get { return m_GateSource; }
            set { m_GateSource = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Moongate GateTarget
        {
            get { return m_GateTarget; }
            set { m_GateTarget = value; }
        }

        [Constructable]
        public MoonGateWizard()
        {
            Title = "the gatekeeper";
            RangePerception = SensoryPerception;            // how far I can see and hear
            SetSkill(SkillName.EvalInt, 80.0, 100.0);
            SetSkill(SkillName.Inscribe, 80.0, 100.0);
            SetSkill(SkillName.Magery, 80.0, 100.0);
            SetSkill(SkillName.Meditation, 80.0, 100.0);
            SetSkill(SkillName.MagicResist, 80.0, 100.0);

            m_DestinationFlags = OracleFlag.Default;
            m_UsePublicMoongates = true;

            m_DangerCost = 35;
        }

        public MoonGateWizard(Serial serial)
            : base(serial)
        {
        }

        public override bool CanTeach { get { return false; } }
        // some moongate wizards may wonder around where is no existing/permanent moongate
        //  like those found around certain desolate dungeon entrances.
        public override bool DisallowAllMoves { get { return (m_CastTimer != null); } }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            return;
        }
        public override int AudibleRange => base.RangePerception;
        #region Spam Prevention

        private Dictionary<Mobile, Spam> spamDic = new Dictionary<Mobile, Spam>();
        private class Spam
        {
            public Spam()
            {
                m_lastSpam = DateTime.UtcNow;
                m_strikes = 0;
            }
            public int m_strikes;
            public DateTime m_lastSpam;
        };

        private bool IgnoreSpammer(Mobile m)
        {
            if (spamDic.ContainsKey(m) == false)
                return false;
            if (DateTime.UtcNow > spamDic[m].m_lastSpam)
            {
                spamDic[m].m_lastSpam = DateTime.UtcNow + TimeSpan.FromSeconds(4);
                spamDic[m].m_strikes = 0;
                return false;
            }
            if (spamDic[m].m_strikes >= 3)
                return true;
            return false;
        }

        private bool SpamManager(Mobile m)
        {
            if (spamDic.ContainsKey(m) == false)
            {
                spamDic.Add(m, new Spam());
                return false;
            }
            if (DateTime.UtcNow > spamDic[m].m_lastSpam)
            {
                spamDic[m].m_lastSpam = DateTime.UtcNow + TimeSpan.FromSeconds(4);
                spamDic[m].m_strikes = 0;
                return false;
            }
            if (spamDic[m].m_strikes++ >= 3)
            {
                spamDic[m].m_lastSpam = DateTime.UtcNow + TimeSpan.FromSeconds(4 * 60);    // 4 minute timeout for spammers
                SayTo(m, "I'm ignoring you...");
                return true;
            }
            return false;
        }

        #endregion
        public override void OnSpeech(SpeechEventArgs e)
        {
            if (SpamManager(e.Mobile) == true)
                return;

            base.OnSpeech(e);

            if (!e.Handled && InRange(e.Mobile, 3))
            {
                m_PlayerMemory.Refresh(e.Mobile);               // each time the player speaks, remember him (so we don't ask him dumb questions)
                Direction = GetDirectionTo(e.Mobile);           // face the player speaking

                List<IOracleDestination> list = GetDestinationsFor(e.Mobile);
                IOracleDestination dest;

                if (e.Speech.ToLower().Contains("where"))
                {
                    Where(list);
                }
                else if ((dest = Find(list, e)) == null)
                {
                    Say("I am sorry, I do not know that destination.");
                }
                else if (AlreadyThere(dest))
                {
                    Say("But you are already there.");
                }
                else if (m_GateTimer != null)
                {
                    Say("That gate is currently in use, one moment please.");
                }
                else if (Charge(e.Mobile, dest))
                {
                    DoGate(dest);
                }
            }
        }

        public List<IOracleDestination> GetDestinationsFor(Mobile from)
        {
            List<IOracleDestination> list = new List<IOracleDestination>();

            foreach (OracleDestination dest in m_Destinations)
            {
                if (dest.Validate(this, from))
                    list.Add(dest);
            }

            foreach (IOracleDestination ts in TownshipStone.AllTownshipStones)
            {
                if (ts.Validate(this, from))
                    list.Add(ts);
            }

            return list;
        }

        public void Where(List<IOracleDestination> list)
        {
            // TODO: Sound less robotic by saying "also", ..., "finally"
            Where(list, "I can provide gates to the following moongates", OracleFlag.Moongate);
            Where(list, "I can provide gates to the following dangerous places", OracleFlag.RedTown | OracleFlag.Dungeon);
            Where(list, "The following townships are known to me", OracleFlag.Township);
            Where(list, "The following other locations are known to me", OracleFlag.None);
        }

        private void Where(List<IOracleDestination> list, string message, OracleFlag flags)
        {
            List<IOracleDestination> dests = new List<IOracleDestination>();

            foreach (IOracleDestination dest in list)
            {
                if (flags == OracleFlag.None || flags.HasFlag(dest.Flag))
                    dests.Add(dest);
            }

            if (dests.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();

            sb.Append(message);
            sb.Append(": ");

            for (int i = 0; i < dests.Count; i++)
            {
                list.Remove(dests[i]); // only mention this destination once

                if (i != 0)
                {
                    if (i == dests.Count - 1)
                        sb.Append(" and ");
                    else
                        sb.Append(", ");
                }

                sb.Append(dests[i].Format());
            }

            sb.Append(".");

            Say(sb.ToString());
        }

        private static IOracleDestination Find(List<IOracleDestination> list, SpeechEventArgs e)
        {
            foreach (IOracleDestination dest in list)
            {
                if (dest.WasNamed(e))
                    return dest;
            }

            return null;
        }

        private bool AlreadyThere(IOracleDestination dest)
        {
            return (this.Map == dest.DestMap && this.GetDistanceToSqrt(dest.DestLoc) < 16);
        }

        private bool Charge(Mobile m, IOracleDestination dest)
        {
            int cost;

            if (dest.Flag.HasFlag(OracleFlag.RedTown) || dest.Flag.HasFlag(OracleFlag.Dungeon))
                cost = m_DangerCost;
            else
                cost = m_Cost;

            if (cost <= 0)
                return true;

            Container pack = m.Backpack;
            Container bank = m.BankBox;

            int total = 0;

            total += UnemploymentCheck.GetAmount(m);

            if (pack != null)
                total += pack.GetAmount(typeof(Gold));

            if (bank != null)
                total += bank.GetAmount(typeof(Gold));

            if (total < cost)
            {
                if (m_RequiresFunds)
                {
                    SayTo(m, String.Format("You lack the necessary funds of {0} gp.", cost));
                    return false;
                }
                else
                {
                    SayTo(m, "You can pay me next time.");
                    return true;
                }
            }

            SayTo(m, String.Format("I have withdrawn {0} gp from your account.", cost));

            BountySystem.BountyKeeper.LBFund += cost;

            cost -= UnemploymentCheck.ConsumeUpTo(m, cost);

            if (pack != null && cost > 0)
                cost -= pack.ConsumeUpTo(typeof(Gold), cost);

            if (bank != null && cost > 0)
                cost -= bank.ConsumeUpTo(typeof(Gold), cost);

            return true;
        }

        private void DoGate(IOracleDestination dest)
        {
            PublicMoongate pm = null;

            if (m_UsePublicMoongates)
            {
                foreach (Item item in GetItemsInRange(7))
                {
                    if (item is PublicMoongate)
                    {
                        pm = (PublicMoongate)item;
                        break;
                    }
                }
            }

            FakeCast(dest, pm);
        }

        private void FakeCast(IOracleDestination dest, PublicMoongate pm)
        {
            if (pm != null)
                Direction = GetDirectionTo(pm);           // face the moongate
            Say("Kal Vas Por");                             // Summon Great Movement
            this.FixedParticles(0, 10, 5, 9032, EffectLayer.LeftHand);
            this.FixedParticles(0, 10, 5, 9032, EffectLayer.RightHand);
            double aniDelay = 1.0;
            Timer.DelayCall(TimeSpan.FromSeconds(aniDelay), new TimerCallback(DoAnimation));
            Timer.DelayCall(TimeSpan.FromSeconds(aniDelay * 2), new TimerCallback(DoAnimation));
            m_CastTimer = Timer.DelayCall(TimeSpan.FromSeconds(aniDelay * 2.2), new TimerStateCallback(DoGate), new object[] { dest, pm });
        }

        private void DoAnimation()
        {
            this.Animate(263, 7, 1, true, false, 0);
        }

        private void DoGate(object state)
        {
            object[] array = (object[])state;
            IOracleDestination dest = (IOracleDestination)array[0];
            PublicMoongate pm = (PublicMoongate)array[1];

            EndCast();
            ClearGates();

            Point3D loc = (pm == null ? this.Location : pm.Location);

            TogglePublicMoongate(loc, this.Map, false);
            m_GateSource = new Moongate(dest.DestLoc, dest.DestMap);
            m_GateSource.Hue = GetHue(dest);
            m_GateSource.MoveToWorld(loc, this.Map);

            Effects.PlaySound(loc, this.Map, 0x20E);

            if (m_TwoWay)
            {
                TogglePublicMoongate(dest.DestLoc, dest.DestMap, false);
                m_GateTarget = new Moongate(loc, this.Map);
                m_GateTarget.Hue = GetHue(dest);
                m_GateTarget.MoveToWorld(dest.DestLoc, dest.DestMap);

                Effects.PlaySound(dest.DestLoc, dest.DestMap, 0x20E);
            }

            Say("Safe travels my friend.");

            m_GateTimer = Timer.DelayCall(TimeSpan.FromSeconds(GateTime), new TimerCallback(ClearGates));
        }

        private void EndCast()
        {
            if (m_CastTimer != null)
            {
                m_CastTimer.Stop();
                m_CastTimer = null;
            }
        }

        private void ClearGates()
        {
            if (m_GateTimer != null)
            {
                m_GateTimer.Stop();
                m_GateTimer = null;
            }

            if (m_GateSource != null)
            {
                TogglePublicMoongate(m_GateSource.Location, m_GateSource.Map, true);
                m_GateSource.Delete();
                m_GateSource = null;
            }

            if (m_GateTarget != null)
            {
                TogglePublicMoongate(m_GateTarget.Location, m_GateTarget.Map, true);
                m_GateTarget.Delete();
                m_GateTarget = null;
            }
        }

        private static void TogglePublicMoongate(Point3D loc, Map map, bool value)
        {
            if (map == null)
                return;

            foreach (Item item in map.GetItemsInRange(loc, 0))
            {
                if (item is PublicMoongate)
                    item.Visible = value;
            }
        }

        private int GetHue(IOracleDestination dest)
        {
            if (dest.Flag.HasFlag(OracleFlag.RedTown) || dest.Flag.HasFlag(OracleFlag.Dungeon))
                return Utility.RandomRedHue();
            else
                return Utility.RandomBlueHue();
        }

        private Memory m_PlayerMemory = new Memory();       // memory used to remember if we a saw a player in the area

        public override void OnSee(Mobile m)
        {
            // yeah
            if (m is PlayerMobile == false)
                return;

            // sanity
            //if (m.Deleted || m.Hidden || !m.Alive || /*m.AccessLevel > this.AccessLevel ||*/ !this.CanSee(m))
            bool canSee = (this.CanSee(m) || (this.CanSeeGhosts && !m.Alive)) && !m.Hidden;
            if (!canSee)
                return;

            // too far away

            double distance = this.GetDistanceToSqrt(m);
            if (distance > this.RangePerception || m.Region != this.Region)
                return;

            // if we're not busy casting, remember this player
            if (m_PlayerMemory.Recall(m) == false && m_CastTimer == null)
            {   // we haven't seen this player yet
                m_PlayerMemory.Remember(m, TimeSpan.FromSeconds(MemoryTime).TotalSeconds);   // remember him for this long
                if (IgnoreSpammer(m) == false)
                {
                    Direction = GetDirectionTo(m);           // face the player you see
                    Say("Where shall I send you friend?");
                }
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);

            if (Utility.RandomBool())
                AddItem(new Shoes(Utility.RandomBlueHue()));
            else
                AddItem(new Sandals(Utility.RandomBlueHue()));

            Item EvilMageRobe = new Robe();
            EvilMageRobe.Hue = Utility.RandomMetalHue();
            EvilMageRobe.LootType = LootType.Newbied;
            AddItem(EvilMageRobe);

            Item EvilWizHat = new WizardsHat();
            EvilWizHat.Hue = Utility.RandomBlueHue();
            EvilWizHat.LootType = LootType.Newbied;
            AddItem(EvilWizHat);

            Item Cloak = new Cloak();
            Cloak.Hue = EvilWizHat.Hue;
            Cloak.LootType = LootType.Newbied;
            AddItem(Cloak);

            Item Bracelet = new GoldBracelet();
            Bracelet.LootType = LootType.Newbied;
            AddItem(Bracelet);

            Item Ring = new GoldRing();
            Ring.LootType = LootType.Newbied;
            AddItem(Ring);

            Item hair = new LongHair();
            hair.Hue = 0x47E;
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            Item beard = new Goatee();
            beard.Hue = 0x47E;
            beard.Movable = false;
            AddItem(beard);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((bool)m_TwoWay);
            writer.Write((Item)m_GateTarget);

            writer.Write((int)m_Cost);
            writer.Write((int)m_DangerCost);
            writer.Write((bool)m_RequiresFunds);

            writer.Write((ushort)m_DestinationFlags);
            writer.Write((bool)m_UsePublicMoongates);

            writer.Write((Item)m_GateSource);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 3:
                    {
                        m_TwoWay = reader.ReadBool();
                        m_GateTarget = reader.ReadItem() as Moongate;

                        goto case 2;
                    }
                case 2:
                    {
                        m_Cost = reader.ReadInt();
                        m_DangerCost = reader.ReadInt();
                        m_RequiresFunds = reader.ReadBool();

                        goto case 1;
                    }
                case 1:
                    {
                        m_DestinationFlags = (OracleFlag)reader.ReadUShort();
                        m_UsePublicMoongates = reader.ReadBool();

                        m_GateSource = reader.ReadItem() as Moongate;

                        break;
                    }
            }

            if (version < 2)
                m_DangerCost = 35;

            if (version < 1)
            {
                m_DestinationFlags = OracleFlag.Default;
                m_UsePublicMoongates = true;
            }
            base.RangePerception = SensoryPerception;
            ClearGates();
        }

        private static readonly List<OracleDestination> m_Destinations = new List<OracleDestination>();

        public List<OracleDestination> Destinations { get { return m_Destinations; } }

        public static void RegisterDestination(string name, Point3D loc, Map map, OracleFlag flags)
        {
            RegisterDestination(name, loc, map, flags, new string[0]);
        }

        public static void RegisterDestination(string name, Point3D loc, Map map, OracleFlag flags, params string[] aliases)
        {
            string[] keywords = new string[1 + aliases.Length];

            keywords[0] = name;

            Array.Copy(aliases, 0, keywords, 1, aliases.Length);

            m_Destinations.Add(new OracleDestination(name, loc, map, flags, keywords));
        }

        #region OracleGen

        [Usage("OracleGen")]
        [Description("Generates moongate oracles. Removes all old oracles.")]
        public static void OracleGen_OnCommand(CommandEventArgs e)
        {
            //DeleteAll();
            int count = 0;
            count += OracleGen(PMList.Felucca);
            World.Broadcast(0x35, true, "{0} moongates oracles generated.", count);
        }

        private static int OracleGen(PMList list)
        {
            foreach (PMEntry entry in list.Entries)
            {
                // first delete an existing oracle + spawner
                List<Mobile> mobiles = new List<Mobile>();
                List<Spawner> spawners = new List<Spawner>();
                IPooledEnumerable eable = list.Map.GetMobilesInRange(entry.Location, 7);
                foreach (Mobile m in eable)
                {
                    if (m is MoonGateWizard)
                    {
                        MoonGateWizard mgw = m as MoonGateWizard;
                        mobiles.Add(mgw);
                        if (mgw.Spawner != null)
                            spawners.Add(mgw.Spawner);
                    }

                }
                eable.Free();

                foreach (Mobile m in mobiles)
                    m.Delete();

                foreach (Spawner s in spawners)
                    s.Delete();

                Spawner oracle = new Spawner(1, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), 0, 0, new ArrayList { "MoonGateWizard" });

                Point3D loc = new Point3D(entry.Location);
                if (Utility.RandomBool())
                    loc.X -= 2;
                else
                    loc.X += 2;

                if (Utility.RandomBool())
                    loc.Y -= 2;
                else
                    loc.Y += 2;

                oracle.MoveToWorld(loc, list.Map);
            }

            return list.Entries.Length;
        }

        #endregion
    }

    public class OracleDestination : IOracleDestination
    {
        private string m_Name;
        private Point3D m_DestLoc;
        private Map m_DestMap;
        private OracleFlag m_Flag;
        private string[] m_Keywords;

        public string Name { get { return m_Name; } }
        public Point3D DestLoc { get { return m_DestLoc; } }
        public Map DestMap { get { return m_DestMap; } }
        public OracleFlag Flag { get { return m_Flag; } }
        public string[] Keywords { get { return m_Keywords; } }

        public OracleDestination(string name, Point3D loc, Map map, OracleFlag flag, string[] keywords)
        {
            m_Name = name;
            m_DestLoc = loc;
            m_DestMap = map;
            m_Flag = flag;
            m_Keywords = keywords;
        }

        public bool Validate(MoonGateWizard oracle, Mobile from)
        {
            return oracle.DestinationFlags.HasFlag(m_Flag);
        }

        public bool WasNamed(SpeechEventArgs e)
        {
            foreach (string keyword in m_Keywords)
            {
                if (Insensitive.Equals(e.Speech, keyword))
                    return true;
            }

            return false;
        }

        public string Format()
        {
            return m_Name;
        }
    }
}