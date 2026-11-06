/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\Seasons\Mobiles\GypsySeer.cs
 * ChangeLog
 *	7/11/2026, adam
 *		created
 */

using Server.Diagnostics;
using Server.Engines;
using Server.Engines.Alignment;
using Server.Engines.TerritoryControl;
using Server.Guilds;
using Server.Items;
using Server.Regions;
using System;
using static Server.Utility;

namespace Server.Mobiles
{
    public class GypsySeer : BaseCreature
    {
        public override AlignmentType DefaultGuildAlignment { get { return AlignmentSystem.GetDynamicAlignment(this, new AlignmentType[] { AlignmentType.Orc }); } }
        public override bool AlwaysAttackable => true;

        [CommandProperty(AccessLevel.GameMaster)]
        public int QualifiedHeads
        {
            get { return GetQualifiedHeadCount(); }
        }

        [Constructable]
        public GypsySeer()
            : base(AIType.AI_Melee, FightMode.Aggressor, 22, 1, 0.2, 1.0)
        {
            InitBody();
            InitOutfit();
            Title = "the Gypsy Seer";
        }

        public override void InitBody()
        {
            SetStr(90, 100);
            SetDex(90, 100);
            SetInt(15, 25);

            // she's a little bit badass
            SetDamage(23, 27);

            SetSkill(SkillName.Macing, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 57.5, 80.0);
            SetSkill(SkillName.Tactics, 60.0, 82.5);

            Hue = 33770;

            Female = true;
            Body = 401;
            Name = NameList.RandomName("female");
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            int shirtAndBandanaHue = Utility.RandomNeutralHue();
            AddItem(new Shirt(shirtAndBandanaHue));
            AddItem(new ShortPants(Utility.RandomNeutralHue()));
            AddItem(new Shoes(Utility.RandomNeutralHue()));

            AddItem(new LongHair());

            PackGold(666);

            var staff = new GnarledStaff();
            AddItem(staff);

            AddItem(new GoldNecklace());

            AddItem(new Bandana(shirtAndBandanaHue));
        }

        public override void GenerateLoot()
        {
            // Gypsy (town NPC / vendor)
            // No Stratics hunter entry (not a creature)
            // Loot: None
            // Cut Up: None

            if (Spawning)
            {
                // No gold
            }
            else
            {
                // No loot
            }
        }

        public override void OnThink()
        {
            base.OnThink();

            TrySummonGuardian();
        }

        public override bool OnBeforeDeath()
        {
            bool obd = base.OnBeforeDeath();

            // Don't allow profitable farming of blue townsfolk from regions that are normally guarded.
            // Note: murderers already receive only 1/3 creature loot, making this an additional deterrent.
            if (obd && Core.RuleSets.MortalisRules() && !SpawnedInGuardedRegion())
            {
                // first find out how much gold this creature is dropping
                int MobGold = this.GetGold();

                // reds get 1/3 of usual gold
                int NewGold = MobGold / 3;

                // first delete all dropped gold
                Container pack = this.Backpack;
                if (pack != null)
                {
                    // how much gold is on the creature?
                    Item[] golds = pack.FindItemsByType(typeof(Gold), true);
                    foreach (Item g in golds)
                    {
                        pack.RemoveItem(g);
                        g.Delete();
                    }

                    this.PackGold(NewGold);
                }
            }

            return obd;
        }
        private bool TrySummonGuardian()
        {
            if (m_CastTimer != null)
                return false;

            if (GetQualifiedHeadCount() < 10)
                return false;

            TerritoryDistrict? district = GetCurrentDistrict();

            if (district == null || district.PercentComplete() < 50.0)
                return false;

            foreach (Item item in Backpack.FindItemsByType(typeof(Head), true))
                item.Delete();

            StartSummon();

            return true;
        }
        private bool SpawnedInGuardedRegion()
        {
            if (Spawner == null)
                return false;

            Region region = Region.Find(Spawner.Location, Spawner.Map);

            return region is GuardedRegion gr && gr.IsGuarded;
        }
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            try
            {
                Head? head = dropped as Head;

                PlayerMobile? pm = from as PlayerMobile;

                // face the player
                if (pm != null)
                    Direction = GetDirectionTo(pm);

                int result = IsQualifiedHead(from, head);

                switch (result)
                {
                    case 1:
                        // Qualified head.
                        break;

                    case -1:
                        // Internal error: invalid caller or missing head.
                        Say("Something is amiss...");
                        return false;

                    case -2:
                        // Not a regular player head.
                        Say("I have no use for this offering.");
                        return false;

                    case -3:
                        // Anti-exploit failure: self, same account, shared account,
                        // guildmate, allied guild, friendly fire, etc.
                        Say("The spirits cannot be deceived.");
                        return false;

                    case -4:
                        // Collector is not formally Orc-aligned.
                        if (pm != null && pm.FindItemOnLayer(Layer.Helm) is OrcishKinMask)
                            Say("The mask may fool the Orcs, but not the spirits. Only those truly aligned with the Orcs may make this offering.");
                        else
                            Say("Only those who stand with the Orcs may make this offering.");

                        return false;

                    case -5:
                        // A head from this player was already accepted within the repeat delay.
                        Say("The spirits remember this offering. Bring me another.");
                        return false;

                    case -6:
                        // Head belongs to an Orc-aligned player.
                        Say("Only the blood of our enemies may feed the ritual.");
                        return false;

                    default:
                        // Unexpected qualification result.
                        Say("Begone.");
                        return false;
                }

                head.Movable = false;
                Backpack.DropItem(head);

                if (GetQualifiedHeadCount() < 10)
                {
                    Say("I shall take this offering.");
                }
                else if (!TrySummonGuardian())
                {
                    TerritoryDistrict? district = GetCurrentDistrict();

                    if (district == null)
                    {
                        Say("The spirits cannot find this town.");
                    }
                    else if (district.PercentComplete() < 50.0)
                    {
                        Say("The offering is complete, but the humans' hold upon this town is not yet strong enough. We must wait.");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                Console.WriteLine("Error (nonfatal) in GypsySeer.OnDragDrop(): " + ex.Message);
                Console.WriteLine(ex.StackTrace);

                return false;
            }
        }
        private static readonly TimeSpan HeadRepeatDelay = TimeSpan.FromMinutes(30);

        private bool IsRecentDuplicate(Head head)
        {
            if (head == null || Backpack == null)
                return false;

            foreach (Item item in Backpack.Items)
            {
                Head? accepted = item as Head;

                if (accepted == null)
                    continue;

                if (!string.Equals(
                        ParsePlayerName(accepted.Name),
                        ParsePlayerName(head.Name),
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Math.Abs((head.Created - accepted.Created).TotalMinutes) < HeadRepeatDelay.TotalMinutes)
                    return true;
            }

            return false;
        }
        private static string? ParsePlayerName(string headName)
        {
            const string Prefix = "the head of ";

            if (string.IsNullOrEmpty(headName))
                return null;

            if (!headName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
                return null;

            return headName.Substring(Prefix.Length);
        }
        private int GetQualifiedHeadCount()
        {
            if (Backpack == null)
                return 0;

            int count = 0;

            foreach (Item item in Backpack.Items)
            {
                if (item is Head)
                    count++;
            }

            return count;
        }
        /*
         *  1  Acceptable head
         * -1  Invalid caller or missing head
         * -2  Not a regular player head
         * -3  Treachery: self, shared account, guildmate, ally, friendly fire, etc.
         * -4  Collector is not Orc-aligned
         * -5  A head from this player was already accepted within the repeat delay
         * -6  Head belongs to an Orc-aligned player
         */
        private int IsQualifiedHead(Mobile from, Head? head)
        {
            PlayerMobile? collector = from as PlayerMobile;

            if (collector == null || head == null)
                return -1;

            if (!head.IsPlayerHead || head.HeadType != HeadType.Regular)
                return -2;

            Guild? guild = collector.Guild as Guild;
            AlignmentType alignment = (guild == null ? AlignmentType.None : guild.Alignment);

            if (alignment != AlignmentType.Orc)
                return -4;

            if (head.GuildAlignment == AlignmentType.Orc)
                return -6;

            if (!BountyKeeper.CanCollectReward(collector, head))
                return -3;

#if !DEBUG
    if (IsRecentDuplicate(head))
        return -5;
#endif

            return 1;
        }
        public GypsySeer(Serial serial)
            : base(serial)
        {
        }

        private void StartSummon()
        {
            FakeCast();
        }

        private Timer? m_CastTimer;      // our current cast timer

        private void FakeCast()
        {
            Say("I summon forth the town guardian..."); // Summon Great Movement
            this.FixedParticles(0, 10, 5, 9032, EffectLayer.LeftHand);
            this.FixedParticles(0, 10, 5, 9032, EffectLayer.RightHand);
            double aniDelay = 1.0;
            Timer.DelayCall(TimeSpan.FromSeconds(aniDelay), new TimerCallback(DoAnimation));
            Timer.DelayCall(TimeSpan.FromSeconds(aniDelay * 2), new TimerCallback(DoAnimation));

            m_CastTimer = Timer.DelayCall(TimeSpan.FromSeconds(aniDelay * 2.2), new TimerStateCallback(DoSummon), new object[] { this });
        }

        private void DoAnimation()
        {
            this.Animate(263, 7, 1, true, false, 0);
        }

        private void DoSummon(object state)
        {
            try
            {
                object[] array = (object[])state;
                if (array[0] is not GypsySeer seer)
                    return;

                if (seer.Map == null || seer.Map == Map.Internal)
                    return;

                TerritoryDistrict? district = GetCurrentDistrict();

                if (district != null)
                {
                    var champ = new TownGuardian(district.Name);

                    SpawnFlags flags = SpawnFlags.Concentric | SpawnFlags.SpawnFar | SpawnFlags.ForceZ |
                                       SpawnFlags.NoBlock | SpawnFlags.ClearPath;

                    Point3D where = seer.Location;

                    for (int i = 0; i < 25; i++)
                    {
                        where = Spawner.GetSpawnPosition(seer.Map, seer.Location, 5, flags, champ);

                        champ.MoveToWorld(where, seer.Map);

                        if (champ.InLOS(seer))
                            break;
                    }

                    champ.Home = seer.Location;
                    champ.RangeHome = 10;

                    TerritoryMessenger.ReportDistrictChallenged(district.Name, district.Map);

                    // Strike the guardian with lightning as he materializes.
                    Effects.SendBoltEffect(champ);
                    Effects.PlaySound(champ.Location, champ.Map, 0x29);

                    Timer.DelayCall(
                        TimeSpan.FromSeconds(2.2),
                        new TimerStateCallback(DoGuardianAddress),
                        new object[] { seer, champ });
                }
            }
            finally
            {
                EndCast();
            }
        }
        private TerritoryDistrict? GetCurrentDistrict()
        {
            Region region = Region.Find(Location, Map);

            if (region == null || string.IsNullOrEmpty(region.Name))
                return null;

            return TerritorySystem.FindDistrict(region.Name);
        }
        private void DoGuardianAddress(object state)
        {
            object[] array = (object[])state;
            if (array[0] is not GypsySeer seer)
                return;

            if (array[1] is not TownGuardian guardian)
                return;

            if (seer.Deleted || guardian.Deleted)
                return;

            if (seer.Map == null || seer.Map == Map.Internal)
                return;

            guardian.Say($"So, thou hast summoned me, {seer.Name}?");
            guardian.Say("So be it. I begin with thee!");

            Timer.DelayCall(TimeSpan.FromSeconds(2.2), new TimerStateCallback(DoGuardianAttack), new object[] { seer, guardian });
        }
        private void DoGuardianAttack(object state)
        {
            object[] array = (object[])state;
            if (array[0] is not GypsySeer seer)
                return;

            if (array[1] is not TownGuardian guardian)
                return;

            if (seer.Deleted || guardian.Deleted)
                return;

            if (seer.Map == null || seer.Map == Map.Internal)
                return;

            // mutual hate
            seer.DoHarmful(guardian);
            guardian.DoHarmful(seer);
        }
        private void EndCast()
        {
            if (m_CastTimer != null)
            {
                m_CastTimer.Stop();
                m_CastTimer = null;
            }
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

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }
    }
}