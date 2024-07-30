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

/* Scripts\Engines\CTFSystem\Items\CTFControl.cs
 * CHANGELOG:
 * 5/23/10, Adam
 *		o New AllowRespawnMana player rule option.
 *			by default you respawn with the mana you died with
 *		o Call RefreshPlayers() from NewRound() to refresh the player stats between rounds.	
 * 5/2/10, adam
 *		Add DropHolding logic before Colorization and teleportation
 * 4/10/10, adam
 *		initial framework.
 */

using Server.Items;
using Server.Mobiles;
using Server.Regions;
using System;
using System.Collections.Generic;

namespace Server.Engines
{
    public partial class CTFControl : Server.Items.CustomRegionControl
    {
        // The following properties are exposted to the player in book form.
        // all properties marked as access level Player are exposed in this way
        #region PLAYER PROPERTIES
        // Region Properties
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowPotions { get { return CustomRegion.CanUsePotions; } set { CustomRegion.CanUsePotions = value; } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public int Rounds { get { return m_IntData[(int)IntNdx.Rounds]; } set { m_IntData[(int)IntNdx.Rounds] = value; } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public int RoundMinutes { get { return m_IntData[(int)IntNdx.RoundMinutes]; } set { m_IntData[(int)IntNdx.RoundMinutes] = value; } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public int RespawnSeconds { get { return m_IntData[(int)IntNdx.RespawnSeconds]; } set { m_IntData[(int)IntNdx.RespawnSeconds] = value; } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public int FlagHPDamage { get { return m_IntData[(int)IntNdx.FlagHPDamage]; } set { m_IntData[(int)IntNdx.FlagHPDamage] = value; } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public int EvalIntCap { get { return m_IntData[(int)IntNdx.EvalIntCap]; } set { m_IntData[(int)IntNdx.EvalIntCap] = value; } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowRespawnMana { get { return GetFlag(BoolData.AllowRespawnMana); } set { SetFlag(BoolData.AllowRespawnMana, value); } }


        // Spells and Skills
        // for spell indexes, see: Register( 21, typeof( Third.TeleportSpell ) );
        // Scripts\Spells\Initializer.cs
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowStealth { get { return !CustomRegion.IsRestrictedSkill(SkillName.Stealth); } set { CustomRegion.SetRestrictedSkill((int)SkillName.Stealth, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowHiding { get { return !CustomRegion.IsRestrictedSkill(SkillName.Hiding); } set { CustomRegion.SetRestrictedSkill((int)SkillName.Hiding, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowMagery { get { return GetFlag(BoolData.AllowMagery); } set { SetFlag(BoolData.AllowMagery, value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowTeleport { get { return !CustomRegion.IsRestrictedSpell(21); } set { CustomRegion.SetRestrictedSpell(21, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowStealing { get { return !CustomRegion.IsRestrictedSkill(SkillName.Stealing); } set { CustomRegion.SetRestrictedSkill((int)SkillName.Stealing, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowReveal { get { return !CustomRegion.IsRestrictedSpell(47); } set { CustomRegion.SetRestrictedSpell(47, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowInvisibility { get { return !CustomRegion.IsRestrictedSpell(43); } set { CustomRegion.SetRestrictedSpell(43, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowEarthquake { get { return !CustomRegion.IsRestrictedSpell(56); } set { CustomRegion.SetRestrictedSpell(56, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowEnergyVortex { get { return !CustomRegion.IsRestrictedSpell(57); } set { CustomRegion.SetRestrictedSpell(57, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowAirElemental { get { return !CustomRegion.IsRestrictedSpell(59); } set { CustomRegion.SetRestrictedSpell(59, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowSummonDaemon { get { return !CustomRegion.IsRestrictedSpell(60); } set { CustomRegion.SetRestrictedSpell(60, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowEarthElemental { get { return !CustomRegion.IsRestrictedSpell(61); } set { CustomRegion.SetRestrictedSpell(61, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowFireElemental { get { return !CustomRegion.IsRestrictedSpell(62); } set { CustomRegion.SetRestrictedSpell(62, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowWaterElemental { get { return !CustomRegion.IsRestrictedSpell(63); } set { CustomRegion.SetRestrictedSpell(63, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowFireField { get { return !CustomRegion.IsRestrictedSpell(27); } set { CustomRegion.SetRestrictedSpell(27, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowPoisonField { get { return !CustomRegion.IsRestrictedSpell(38); } set { CustomRegion.SetRestrictedSpell(38, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowParalyzeField { get { return !CustomRegion.IsRestrictedSpell(46); } set { CustomRegion.SetRestrictedSpell(46, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowBladeSpirits { get { return !CustomRegion.IsRestrictedSpell(32); } set { CustomRegion.SetRestrictedSpell(32, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowChainLightning { get { return !CustomRegion.IsRestrictedSpell(48); } set { CustomRegion.SetRestrictedSpell(48, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowEnergyField { get { return !CustomRegion.IsRestrictedSpell(49); } set { CustomRegion.SetRestrictedSpell(49, !value); } }
        [CommandProperty(AccessLevel.Player, AccessLevel.Administrator)]
        public bool AllowMeteorSwarm { get { return !CustomRegion.IsRestrictedSpell(54); } set { CustomRegion.SetRestrictedSpell(54, !value); } }

        #endregion PLAYER PROPERTIES

        #region SYSTEM PROPERTIES
        private Point3D m_RedBase = Point3D.Zero;
        [CommandProperty(AccessLevel.Administrator)]
        public Point3D RedBase { get { return m_RedBase; } set { m_RedBase = value; } }
        private Point3D m_BlueBase = Point3D.Zero;
        [CommandProperty(AccessLevel.Administrator)]
        public Point3D BlueBase { get { return m_BlueBase; } set { m_BlueBase = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int RedTeamClothing { get { return m_IntData[(int)IntNdx.RedTeamClothing]; } set { m_IntData[(int)IntNdx.RedTeamClothing] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int BlueTeamClothing { get { return m_IntData[(int)IntNdx.BlueTeamClothing]; } set { m_IntData[(int)IntNdx.BlueTeamClothing] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int SystemMessageColor { get { return m_IntData[(int)IntNdx.SystemMessageColor]; } set { m_IntData[(int)IntNdx.SystemMessageColor] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int RedTeamPoints { get { return m_IntData[(int)IntNdx.RedTeamPoints]; } set { m_IntData[(int)IntNdx.RedTeamPoints] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int BlueTeamPoints { get { return m_IntData[(int)IntNdx.BlueTeamPoints]; } set { m_IntData[(int)IntNdx.BlueTeamPoints] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int FlagColor { get { return m_IntData[(int)IntNdx.FlagColor]; } set { m_IntData[(int)IntNdx.FlagColor] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public Item Flag { get { return m_Items[(int)ItemNdx.Flag]; } set { m_Items[(int)ItemNdx.Flag] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public Item BlueBaseScore { get { return m_Items[(int)ItemNdx.BlueBaseScore]; } set { if (value is CTFScorePlate) { m_Items[(int)ItemNdx.BlueBaseScore] = value; (value as CTFScorePlate).Setup(this, Team.Blue); } } }
        [CommandProperty(AccessLevel.Administrator)]
        public Item RedBaseScore { get { return m_Items[(int)ItemNdx.RedBaseScore]; } set { if (value is CTFScorePlate) { m_Items[(int)ItemNdx.RedBaseScore] = value; (value as CTFScorePlate).Setup(this, Team.Red); } } }
        [CommandProperty(AccessLevel.Administrator)]
        public Team Defense { get { return (Team)m_IntData[(int)IntNdx.Defense]; } set { m_IntData[(int)IntNdx.Defense] = (int)value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int Round { get { return m_IntData[(int)IntNdx.Round]; } set { m_IntData[(int)IntNdx.Round] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int BeepSoundID { get { return m_IntData[(int)IntNdx.BeepSoundID]; } set { m_IntData[(int)IntNdx.BeepSoundID] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int FlagRespawnSeconds { get { return m_IntData[(int)IntNdx.FlagRespawnSeconds]; } set { m_IntData[(int)IntNdx.FlagRespawnSeconds] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int AfterPartyMinutes { get { return m_IntData[(int)IntNdx.AfterPartyMinutes]; } set { m_IntData[(int)IntNdx.AfterPartyMinutes] = value; } }
        [CommandProperty(AccessLevel.Administrator)]
        public Item RedBaseChest { get { return m_Items[(int)ItemNdx.RedBaseChest]; } set { if (value is BaseContainer) { m_Items[(int)ItemNdx.RedBaseChest] = value; } } }
        [CommandProperty(AccessLevel.Administrator)]
        public Item BlueBaseChest { get { return m_Items[(int)ItemNdx.BlueBaseChest]; } set { if (value is BaseContainer) { m_Items[(int)ItemNdx.BlueBaseChest] = value; } } }
        #endregion SYSTEM PROPERTIES

        #region SESSION_STATE
        // session state stuff
        //	When you add values here, make sure to also add reset logic to the Reset function.

        private enum MobileNdx { Captain1, Captain2, Broker }
        private Mobile[] m_Mobiles = { null, null, null };

        private enum IntNdx { CurrentState, BoolData, SessionId, RedTeamClothing, BlueTeamClothing, WaitTeleportCount, SystemMessageColor, RedTeamPoints, BlueTeamPoints, FlagColor, Defense, Round, Rounds, RoundMinutes, RespawnSeconds, BeepSoundID, FlagRespawnSeconds, FlagHPDamage, AfterPartyMinutes, EvalIntCap }
        private int[] m_IntData = { (int)States.Quiescent, (int)BoolData.None, 204, 1236, 1364, 0, 0x35, 0, 0, 0x4EA, (int)Team.Blue, 0, 4, 5, 5, 976, 60, 30, 15, 9999 };

        private enum ItemNdx { Flag, BlueBaseScore, RedBaseScore, RedBaseChest, BlueBaseChest }
        private Item[] m_Items = { null, null, null, null, null };

        // who we are talking to (captain team 1)
        private Mobile Captain1 { get { return m_Mobiles[(int)MobileNdx.Captain1]; } set { m_Mobiles[(int)MobileNdx.Captain1] = value; } }
        // who is talking (NPC Fightbroker)
        private Mobile Broker { get { return m_Mobiles[(int)MobileNdx.Broker]; } set { m_Mobiles[(int)MobileNdx.Broker] = value; } }
        // who to be the Captain of team 2 - set by targeting system
        public Mobile Captain2 { get { return m_Mobiles[(int)MobileNdx.Captain2]; } set { m_Mobiles[(int)MobileNdx.Captain2] = value; } }
        // serial number to prevent stale target cursors from affecting new sessions
        public int SessionId { get { return m_IntData[(int)IntNdx.SessionId]; } set { m_IntData[(int)IntNdx.SessionId] = value; } }
        // the player can drop a rule book on us any after saying register
        private bool BookReceived { get { return GetFlag(BoolData.BookReceived); } set { SetFlag(BoolData.BookReceived, value); } }
        // 1 minute remaining warning
        private bool OneMinuteWarn { get { return GetFlag(BoolData.OneMinuteWarn); } set { SetFlag(BoolData.OneMinuteWarn, value); } }
        // 10 second remaining warning
        private bool TenSecondWarn { get { return GetFlag(BoolData.TenSecondWarn); } set { SetFlag(BoolData.TenSecondWarn, value); } }
        // countdown to teleport
        private int WaitTeleportCount { get { return m_IntData[(int)IntNdx.WaitTeleportCount]; } set { m_IntData[(int)IntNdx.WaitTeleportCount] = value; } }
        #endregion SESSION_STATE

        private HashSet<int> m_DefaultsSpellConfig = null;
        public HashSet<int> DefaultsSpellConfig { get { return m_DefaultsSpellConfig; } }

        // the teams
        private Dictionary<PlayerMobile, PlayerContextData> m_RedTeam = new Dictionary<PlayerMobile, PlayerContextData>();
        private Dictionary<PlayerMobile, PlayerContextData> m_BlueTeam = new Dictionary<PlayerMobile, PlayerContextData>();

        [Flags]
        private enum BoolData
        {
            None = 0x00000000,
            BookReceived = 0x00000001,
            OneMinuteWarn = 0x00000002,
            TenSecondWarn = 0x00000004,
            AllowMagery = 0x00000008,
            AllowRespawnMana = 0x00000010,
        }

        private static List<CTFControl> m_CtfGames = new List<CTFControl>();
        public static List<CTFControl> CtfGames { get { return m_CtfGames; } }

        [Constructable]
        public CTFControl()
            : base()
        {
            //initial values for the controller
            this.Name = "a Capture The Flag controller";
            this.Movable = false;
            this.Visible = false;
            this.ItemID = 0xED4;

            // setup for the region
            CustomRegion.Name = "Capture The Flag";
            CustomRegion.Map = Map.Felucca;
            CustomRegion.IsGuarded = false;

            Reset();

            m_ActionTimer = new ActionTimer(this, TimeSpan.FromSeconds(1));
            m_ActionTimer.Start();

            CtfGames.Add(this);
        }

        public CTFControl(Serial serial)
            : base(serial)
        {
        }

        public override CustomRegion CreateRegion(CustomRegionControl controller)
        {
            return new CTFRegion(controller);
        }

        public void ForceResurrect(Mobile m)
        {
            bool canRessurect = CustomRegion.CanRessurect; // make sure the system can ressurect the player
            CustomRegion.CanRessurect = true;
            m.Resurrect();
            CustomRegion.CanRessurect = canRessurect; // restore the setting
        }

        public void ResurrectMobile(Mobile m)
        {
            m.PlaySound(0x214);
            m.FixedEffect(0x376A, 10, 16);
            m.Hidden = true;                // hidden
            ForceResurrect(m);              // alive
            m.Frozen = false;               // can now move
            m.Hits = m.HitsMax;             // regen hits 
            m.Stam = m.StamMax;             // regen stam 

            PlayerContextData pcd = GetPlayerContextData(m);
            if (pcd != null && this.AllowRespawnMana == false)
            {   // we totally refill HITS and STAM,. but mana should only return to what it was before death unless they turned on the option to refill on death
                m.Mana = pcd.SaveMana;
            }
            else
                m.Mana = m.ManaMax;

            // make sure this mobile is still a player. (maybe they resed after the game ended.)
            if ((m_RedTeam.ContainsKey(m as PlayerMobile) || m_BlueTeam.ContainsKey(m as PlayerMobile)) && pcd != null)
            {
                // colorize this players deathrobe
                Item item = m.FindItemOnLayer(Layer.OuterTorso);
                if (item != null && item is DeathRobe)
                    (item as BaseClothing).PushHue(TeamColor(GetTeam(m)));

                // offense always see the flag arrow and defense sees the flag arrow if the flag is away (some one is carrying it or it is greater than 6 tiles from the home base)
                //	the reason this player may not have a quest arrow is because they had the flag and the arrow was explicitly turned off for them
                if (GetTeam(m) == Offense || IsFlagHome() == false)
                    if (m.QuestArrow == null || m.QuestArrow.Running == false)
                        ShowPlayerArrow(m);
            }
        }

        public bool OnRegionEquipItem(Mobile m, Item item)
        {
            if (item is BaseClothing)
            {
                (item as BaseClothing).PopHue();    // make sure to clear opposing team colrs (cross dressing)
                (item as BaseClothing).PushHue(TeamColor(GetTeam(m)));
            }

            if (item is BaseArmor)
            {
                (item as BaseArmor).PopHue();       // make sure to clear opposing team colrs (cross dressing)
                (item as BaseArmor).PushHue(TeamColor(GetTeam(m)));
            }

            return true;
        }

        public bool OnRegionCheckAccessibility(Item i, Mobile m)
        {
            // loosers have been kicked, winners have access
            if (CurrentState > States.GameEnd)
                return true;

            // it the item is one of the chests and it's not our teams, then access is denied
            if ((i == RedBaseChest || i == BlueBaseChest) && GetTeamChest(m) != i)
                return false;

            return true;
        }

        public override void OnDelete()
        {
            Stop("CTF Going down", KickReason.Canceled);    // return all players and decolor their stuff
            Reset();                                        // delete the flag

            if (RedBaseScore != null)
                RedBaseScore.Delete();

            if (BlueBaseScore != null)
                BlueBaseScore.Delete();

            if (RedBaseChest != null)
                RedBaseChest.Delete();

            if (BlueBaseChest != null)
                BlueBaseChest.Delete();

            if (CtfGames != null)
                CtfGames.Remove(this);

            base.OnDelete();
        }

        // passed up from the region when a player dies
        public void OnRegionAfterDeath(Mobile m)
        {
            if (m != null && GetTeam(m) != Team.None)
            {
                m.Frozen = true;

                // credit the killer
                if (GetTeam(m.LastKiller) != Team.None)
                {
                    if (GetTeam(m.LastKiller) != GetTeam(m))
                    {
                        Dictionary<PlayerMobile, PlayerContextData> team = GetTeam(GetTeam(m.LastKiller));
                        if (team != null)
                        {
                            team[m.LastKiller as PlayerMobile].Points++;
                        }
                    }
                }

            }
        }

        // Called from ScorePlate.OnPlateMoveOver
        public void OnPlateMoveOver(Mobile m, Team team)
        {   // must be alive and playing the game (ignores staff) to score
            if (m.Alive && GetTeam(m) != Team.None)
                OnScore(m, team);
        }

        // Called from Flag.OnFlagMoveOver
        public void OnFlagMoveOver(Mobile m)
        {   // player must be alive and playing the game (ignores staff) to pick up the flag
            if (Flag.Parent == null && m.Alive && GetTeam(m) != Team.None)
            {
                if (GetTeam(m) == Offense)
                {   // only offense can pick up the flag
                    ClearHands(m);
                    Flag.Movable = true;
                    m.EquipItem(Flag);
                    Flag.Movable = false;
                    OnFlagPickedUp(m);
                }
            }
        }

        // Called from Region.Ondeath
        public void OnDeath(Mobile m)
        {
            // sanity
            if (Flag == null)
            {
                CurrentState = States.Invalid;
                return;
            }

            // make sure the FLAG is dropped from the corpse
            if (HasFlag(m) && GetTeam(m) != Team.None)
            {
                Flag.MoveToWorld(m.Location);
                OnFlagDropped(m);

                // respawn the flag at home if it's more than 6 tiles away and there is enough time for the respawn
                if (!IsFlagHome() && GetRoundRemainingTime() > GetFlagRespawnTime())
                {
                    m_FlagRespawnTimer = new FlagRespawnTimer(this);
                    m_FlagRespawnTimer.Start();
                }
            }

            // respawn the mobile
            PlayerContextData pcd = GetPlayerContextData(m);
            if (pcd != null)
                pcd.Respawn(RespawnSeconds);

            // save the players pre-death mana for restoration later
            if (pcd != null)
            {   // we totally refill HITS and STAM,. but mana should only returnm to what it was at thie time (before death)
                pcd.SaveMana = m.Mana;
            }
        }

        public void OnFlagDropped(Mobile m)
        {
            if (m is PlayerMobile == false)
                return;

            Team t = GetTeam(m);
            if (t == Defense)
            {   // Defense dropped their flag
                BroadcastMessage(GetTeam(t), SystemMessageColor, "Flag dropped.");  // Nick!
                BroadcastMessage(GetOpposingTeam(t), SystemMessageColor, "Flag dropped.");  // Nick!
            }
            else
            {   // Offense got the flag
                BroadcastMessage(GetTeam(t), SystemMessageColor, "Flag dropped.");  // Nick!
                BroadcastMessage(GetOpposingTeam(t), SystemMessageColor, "Flag dropped.");  // Nick!
            }
        }

        // called from the flag when a player picks it up
        public void OnFlagPickedUp(Mobile m)
        {
            if (m is PlayerMobile == false)
                return;

            Team t = GetTeam(m);
            if (t == Defense)
            {   // Defense cannot pickup their flag, so no message here
                //BroadcastMessage(GetTeam(t), SystemMessageColor, "Flag reacquired.");		// Hugh!
                //BroadcastMessage(GetOpposingTeam(t), SystemMessageColor, "");				// Hugh!
            }
            else
            {   // Offense got the flag
                BroadcastMessage(GetTeam(t), SystemMessageColor, "Flag taken.");            // Hugh!
                BroadcastMessage(GetOpposingTeam(t), SystemMessageColor, "Flag stolen.");   // Hugh!

                // show flag arrow to defense
                BroadcastArrow(GetTeam(Defense));

                // kill the arrow on the player now carrying the flag
                ShowPlayerArrow(m);
            }

            // kill any outstanding flag respawn timers
            if (m_FlagRespawnTimer != null && m_FlagRespawnTimer.Running)
            {
                m_FlagRespawnTimer.Stop();
                m_FlagRespawnTimer.Flush();
                m_FlagRespawnTimer = null;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version 

            // version 1

            // write the generic item data
            writer.Write((int)m_Items.Length);
            foreach (Item ix in m_Items)
                writer.Write(ix);

            // write the state timers 
            writer.Write(m_StateTimer.Count);
            foreach (KeyValuePair<States, DateTime> kvp in m_StateTimer)
            {
                writer.Write((int)kvp.Key);
                writer.WriteDeltaTime(kvp.Value);
            }

            // write the base locations
            writer.Write(m_RedBase);
            writer.Write(m_BlueBase);

            // write the blue team and their context data
            writer.Write(m_BlueTeam.Count);
            foreach (KeyValuePair<PlayerMobile, PlayerContextData> kvp in m_BlueTeam)
            {
                writer.Write(kvp.Key);
                kvp.Value.Serialize(writer);
            }

            // write the red team and their context data
            writer.Write(m_RedTeam.Count);
            foreach (KeyValuePair<PlayerMobile, PlayerContextData> kvp in m_RedTeam)
            {
                writer.Write(kvp.Key);
                kvp.Value.Serialize(writer);
            }

            // write the generic int data
            writer.Write((int)m_IntData.Length);
            foreach (int id in m_IntData)
                writer.Write(id);

            // write the generic mobile data
            writer.Write((int)m_Mobiles.Length);
            foreach (Mobile m in m_Mobiles)
                writer.Write(m);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        // read the generic item data
                        int icount = reader.ReadInt();
                        for (int ix = 0; ix < icount; ix++)
                            m_Items[ix] = reader.ReadItem();

                        // read the state timers
                        int kvpcount = reader.ReadInt();
                        for (int ix = 0; ix < kvpcount; ix++)
                            m_StateTimer[(States)reader.ReadInt()] = reader.ReadDeltaTime();

                        // read the base locations
                        m_RedBase = reader.ReadPoint3D();
                        m_BlueBase = reader.ReadPoint3D();

                        // read the blue team and thier context data
                        int bcount = reader.ReadInt();
                        for (int ix = 0; ix < bcount; ix++)
                            m_BlueTeam[reader.ReadMobile() as PlayerMobile] = new PlayerContextData(reader);

                        // read the blue team and thier context data
                        int rcount = reader.ReadInt();
                        for (int ix = 0; ix < rcount; ix++)
                            m_RedTeam[reader.ReadMobile() as PlayerMobile] = new PlayerContextData(reader);

                        // read the generic integer data
                        int count = reader.ReadInt();
                        for (int ix = 0; ix < count; ix++)
                            m_IntData[ix] = reader.ReadInt();

                        // read the generic mobile data
                        int mcount = reader.ReadInt();
                        for (int ix = 0; ix < mcount; ix++)
                            m_Mobiles[ix] = reader.ReadMobile();

                        goto case 0;
                    }
                case 0:
                    goto default;

                default:
                    break;
            }

            // our state machine
            m_ActionTimer = new ActionTimer(this, TimeSpan.FromSeconds(1));
            m_ActionTimer.Start();

            // if there is a flag, and the game is on, and the flag is not home
            //	start a flag respawn timer
            if (Flag != null && Round != 0 && Flag.Location != GetTeamBase(Defense))
            {   // and the flag is more than 6 tiles from home and there is enough time remaining for the respawn
                if (!IsFlagHome() && GetRoundRemainingTime() > GetFlagRespawnTime())
                {
                    m_FlagRespawnTimer = new FlagRespawnTimer(this);
                    m_FlagRespawnTimer.Start();
                }
            }

            // add this CTF game to our global list available to the fight brokers
            CtfGames.Add(this);
        }

        /// <summary>
        ///  Reset:
        ///		Reset only restores game defaults and cleans up game items like the flag.
        ///		Use Stop() for dealing with players clothes, frozen, location, etc
        /// </summary>
        private void Reset()
        {   // cleanup state variables
            Captain1 = null;                        // who we are talking to (captain team 1)
            Broker = null;                          // who is talking (NPC Fightbroker)
            Captain2 = null;                        // who to be the Captain of team 2
            SessionId++;                            // serial number to prevent stale target cursors from affecting new sessions
            Round = 0;                              // initialize to invalid round #
            RedTeamPoints = 0;                      // points
            BlueTeamPoints = 0;                     // points
            ClearBoolData();                        // clear all bools

            // delete the flag
            if (Flag != null && Flag.Deleted == false)
            {
                Flag.Delete();
                Flag = null;
            }

            // basic region setup
            // these are not changable by the player
            CustomRegion.EnableStuckMenu = false;
            CustomRegion.EnableHousing = false;
            CustomRegion.AllowTravelSpellsInRegion = true;
            CustomRegion.NoGateInto = true;
            CustomRegion.NoRecallInto = true;
            CustomRegion.UseDungeonRules = false;
            CustomRegion.UseHouseRules = false;
            CustomRegion.CannotEnter = false;
            CustomRegion.ShowEnterMessage = false;
            CustomRegion.ShowExitMessage = false;
            CustomRegion.NoMurderCounts = true;
            CustomRegion.IOBZone = false;
            CustomRegion.ShowIOBMessage = false;
            CustomRegion.IsGuarded = false;
            CustomRegion.IsIsolated = false;
            CustomRegion.IsMagicIsolated = false;
            CustomRegion.RestrictCreatureMagic = false;
            CustomRegion.NoExternalHarmful = false;
            CustomRegion.GhostBlindness = false;
            CustomRegion.CaptureArea = false;
            CustomRegion.BlockLooting = true;
            CustomRegion.EnableMusic = true;
            CustomRegion.Music = MusicName.BTCastle;               // nice and creepy

            // turn all magic
            AllowMageryImp = true;

            // restricted spells - cannot change
            CustomRegion.SetRestrictedSpell(58, true);                       // ResurrectionSpell: damages the game too much
            CustomRegion.SetRestrictedSpell(31, true);                       // RecallSpell: nope
            CustomRegion.SetRestrictedSpell(51, true);                       // GateTravelSpell: nope
            CustomRegion.SetRestrictedSpell(44, true);                       // MarkSpell: nope
            CustomRegion.SetRestrictedSpell(34, true);                       // IncognitoSpell: would change clothes-colors

            // restricted skills - cannot change
            CustomRegion.SetRestrictedSkill((int)SkillName.Camping, true);   // Camping: how lame would this be?

            // reset PLAYER ACCESSABLE RULES CTF defaults
            this.AllowPotions = true;
            this.AllowStealth = false;
            this.AllowHiding = false;
            this.AllowMagery = true;
            this.AllowTeleport = false;
            this.AllowStealing = false;
            this.AllowReveal = false;
            this.AllowInvisibility = false;
            this.AllowFireField = false;
            this.AllowPoisonField = false;
            this.AllowParalyzeField = false;
            this.AllowBladeSpirits = false;
            this.AllowEarthquake = false;
            this.AllowChainLightning = false;
            this.AllowEnergyField = false;
            this.AllowMeteorSwarm = false;

            Rounds = 5;                             // number of rounds in game
            RoundMinutes = 5;                       // minutes per round
            EvalIntCap = 9999;                      // big number means no limit
            FlagHPDamage = 70;                      // amount of hitpoint damage from the flag

            // because we have an AllowMagery setting that can turn on/off all magery, we need to keep a copy
            //	of the Default setup in case the player drops multiple books on the Fightbroker. For instance:
            // Book1 turns off magery, then book2 turns on Teleport, we would endup with a teleport only spell config.
            m_DefaultsSpellConfig = new HashSet<int>(CustomRegion.RestrictedSpells);
        }

        private void DropHolding(Mobile m)
        {
            try
            {
                Item held = m.Holding;
                if (held != null)
                {
                    held.ClearBounce();
                    if (m.Backpack != null)
                    {
                        m.Backpack.DropItem(held);
                    }
                }
                m.Holding = null;
            }
            catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
        }

        // make sure to remove the player from the appropriate team list after the kick
        //	we don't want to do it here since this function will often be called from an enumeration
        private enum KickReason { Kicked, Exited, GameOver, Error, Canceled }
        private void KickPlayer(Mobile m, PlayerContextData pcd, string text, KickReason reason)
        {
            if (m != null)
            {
                // drop the flag
                if (HasFlag(m))
                    Flag.MoveToWorld(m.Location, CustomRegion.Map);

                // tell them what just happened
                m.SendMessage(SystemMessageColor, text);

                // tell their team what has happened
                if (reason == KickReason.Exited)
                    BroadcastMessage(GetTeam(GetTeam(m)), SystemMessageColor, string.Format("{0} has left the game.", m.Name));

                if (reason == KickReason.Kicked)
                    BroadcastMessage(GetTeam(GetTeam(m)), SystemMessageColor, string.Format("{0} has been kicked!", m.Name));

                // kill any arrows
                if (m.QuestArrow != null && m.QuestArrow.Running)
                    m.QuestArrow.Stop();

                // drop holding
                DropHolding(m);

                // reset their clothes to pregame values
                DeColorizePlayer(m);

                // there's no place like home
                TeleportPlayer(m, pcd.Map, pcd.Location);

                // unfreeze them
                m.Frozen = false;

                // clear things that could get the player guard whacked
                m.Criminal = false;

                // i'm no longer mad ant anyone
                m.Aggressed.Clear();

                // nobody is mad at me
                m.Aggressors.Clear();

                // clear anyone we may currently be fighting
                m.Combatant = null;

            }
        }

        /// <summary>
        ///  Stop:
        ///		called to send players home and reset their clothes etc.
        ///		Use Reset to restore game defaults and clean up game items like the flag.
        /// </summary>
        private void Stop(string text, KickReason reason)
        {
            // kick each player with a message as to why
            Dictionary<PlayerMobile, PlayerContextData>[] teams = new Dictionary<PlayerMobile, PlayerContextData>[2] { m_RedTeam, m_BlueTeam };
            Stop(teams, text, reason);
        }

        private void Stop(Dictionary<PlayerMobile, PlayerContextData>[] teams, string text, KickReason reason)
        {
            if (teams == null)
                return;

            // kick each player with a message as to why
            for (int ix = 0; ix < teams.Length; ix++)
                if (teams[ix] != null)
                    foreach (KeyValuePair<PlayerMobile, PlayerContextData> kvp in teams[ix])
                        KickPlayer(kvp.Key, kvp.Value, text, reason);

            // lastly, clear the teams
            for (int ix = 0; ix < teams.Length; ix++)
                teams[ix].Clear();
        }

        public void OnCancel()
        {
            CurrentState = States.Cancel;
        }

        public void OnScore(Mobile m, Team plate)
        {
            if (CurrentState == States.PlayRound)
            {
                if (Flag == null || Flag.Deleted)
                {
                    LogStatus("Flag no longer exists. Stopping game");
                    CurrentState = States.Invalid;
                    return;
                }

                // someone holding the flag stepped on a score plate
                if (HasFlag(m))
                {
                    // see if there is a score
                    Team t = GetTeam(m);
                    if (t == Offense && t == plate)
                    {
                        // score!
                        OffenseScore++;

                        // team scores
                        BroadcastMessage(null, SystemMessageColor, string.Format("Flag captured."));

                        // Offense just past Defense
                        if (OffenseScore == DefenseScore + 1)
                            BroadcastMessage(GetTeam(Offense), SystemMessageColor, string.Format("In the lead."));

                        // end of game?
                        if (Round >= Rounds)
                        {
                            CurrentState = States.GameEnd;
                            return;
                        }
                        else if ((Round + 1 >= Rounds) == false)
                            // not the last round and not the end of game
                            BroadcastMessage(null, SystemMessageColor, string.Format("Round over."));

                        // start a new round
                        Round++;
                        if (Round >= Rounds)
                            BroadcastMessage(null, SystemMessageColor, string.Format("Final round."));

                        // new round
                        NewRound();
                    }
                }
            }
        }

        private void NewRound()
        {
            // new round timer
            m_StateTimer[States.PlayRound] = DateTime.UtcNow + TimeSpan.FromMinutes(RoundMinutes);

            // swap sides
            Defense = Defense == Team.Red ? Team.Blue : Team.Red;

            // send players to their home base
            TeleportTeamCTF(m_RedTeam, m_RedBase);
            TeleportTeamCTF(m_BlueTeam, m_BlueBase);

            // move flag to the defense home
            Flag.MoveToWorld(GetTeamBase(Defense), CustomRegion.Map);

            BroadcastMessage(GetTeam(Defense), SystemMessageColor, "Defense.");
            BroadcastMessage(GetTeam(Offense), SystemMessageColor, "Offense.");

            // show flag arrow to offense
            BroadcastArrow(GetTeam(Offense));

            // kill the flag arrow for defense
            BroadcastArrow(GetTeam(Defense), false);

            // refresh all player stats
            RefreshPlayers();
        }

        // the states
        // all good states must be > Quiescent - all bad states must be < Quiescent 
        public enum States { Invalid, Cancel, Quiescent, Registration, Registration_wait, QueryAcceptChallenge, WaitAcceptChallenge, GiveBook, WaitBookReturn, DoTeleport, WaitTeleport, GameStart, PlayRound, GameEnd, AfterParty, AfterPartyVictory, AfterPartyStones, AfterPartyMessage };
        // the length of time we stay in the state
        private Dictionary<States, DateTime> m_StateTimer = new Dictionary<States, DateTime>();

        // current state
        [CommandProperty(AccessLevel.Administrator)]
        public States CurrentState { get { return (States)m_IntData[(int)IntNdx.CurrentState]; } set { if ((States)m_IntData[(int)IntNdx.CurrentState] != value) DebugSay(string.Format("Entering state {0}", value.ToString())); m_IntData[(int)IntNdx.CurrentState] = (int)value; } }

        // state machine timer
        private Timer m_ActionTimer = null;

        // event handler
        public void OnEvent()
        {
            switch (CurrentState)
            {
                case States.AfterParty:
                    CurrentState = AfterParty();
                    break;

                case States.GameEnd:
                    CurrentState = GameEnd();
                    break;

                case States.PlayRound:
                    CurrentState = PlayRound();
                    break;

                case States.GameStart:
                    CurrentState = GameStart();
                    break;

                case States.WaitTeleport:
                    CurrentState = WaitTeleport();
                    break;

                case States.DoTeleport:
                    CurrentState = DoTeleport();
                    break;

                case States.GiveBook:
                    CurrentState = GiveBook();
                    break;

                case States.WaitBookReturn:
                    CurrentState = WaitBookReturn();
                    break;

                case States.WaitAcceptChallenge:
                    CurrentState = WaitAcceptChallenge();
                    break;

                case States.QueryAcceptChallenge:
                    CurrentState = QueryAcceptChallenge();
                    break;

                case States.Quiescent:
                    CurrentState = Quiescent();
                    break;

                case States.Registration:
                    CurrentState = Registration();
                    break;

                case States.Registration_wait:
                    CurrentState = Registration_wait();
                    break;

                case States.Invalid:
                    CurrentState = Invalid();
                    break;

                case States.Cancel:
                    CurrentState = Cancel();
                    break;
            }
        }

        private States Invalid()
        {   // cleanup
            Stop("There was a game error, and the game is ending.", KickReason.Error);
            Reset();
            Say("Invalid state detected, resetting...");
            return States.Quiescent;
        }

        private States Cancel()
        {   // cleanup
            Stop("The game was canceled.", KickReason.Canceled);
            Reset();
            Say("State canceled, resetting...");
            return States.Quiescent;
        }

        private States Quiescent()
        {   // do nothing
            return CurrentState;
        }

        // someone is trying to register a CTF game.
        private States Registration()
        {   // 30 seconds to target captain 2 until registration timeout
            m_StateTimer[States.Registration_wait] = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            SessionId++;            // a new session has begun. - used to detect stale target cursors
            Captain2 = null;
            Captain1.SendMessage("Please target the Captain of the opposing team.");
            Captain1.Target = new CTFTarget(this, SessionId);

            return States.Registration_wait;
        }

        // waiting the the player to target the other captain
        private States Registration_wait()
        {   // sanity - we must have a state time for the registration state
            if (m_StateTimer.ContainsKey(States.Registration_wait) == false)
                return States.Invalid;

            // see if the player quit, or disconnected, etc
            if (!MobileOk(Captain1) || !MobileOk(Broker) || Captain1.Backpack == null)
                return States.Cancel;

            // okay, continue to next state
            if (MobileOk(Captain2) && Captain1 != Captain2)
                return States.QueryAcceptChallenge;
            else if (Captain2 != null)
            {   // the targeted individual is a criminal or something
                Broker.SayTo(Captain1, "There is something I do not trust about {0}. Maybe wait a bit?.", Captain2.Name);
                Captain2 = null;
                return States.Cancel;
            }

            if (DateTime.UtcNow > m_StateTimer[States.Registration_wait])
            {
                Broker.SayTo(Captain1, "You have taken too long, your CTF match has been canceled.");
                return States.Cancel;
            }

            return States.Registration_wait;
        }

        private States QueryAcceptChallenge()
        {
            Broker.SayTo(Captain2, "{0} challenges you to a Capture the Flag match!", Captain1.Name);
            Broker.SayTo(Captain2, "If you agree to this match say 'yes' or 'accept'.");
            Broker.SayTo(Captain1, "Ah! good choice.");
            Broker.SayTo(Captain1, "But I think you can take him.");

            // 30 seconds to target captain 2 until registration timeout
            m_StateTimer[States.WaitAcceptChallenge] = DateTime.UtcNow + TimeSpan.FromSeconds(30);

            return States.WaitAcceptChallenge;
        }

        private States WaitAcceptChallenge()
        {
            // sanity - we must have a state time for the registration state
            if (m_StateTimer.ContainsKey(States.WaitAcceptChallenge) == false)
                return States.Invalid;

            // see if the player quit, or disconnected, etc
            if (!MobileOk(Captain1) || !MobileOk(Captain2) || !MobileOk(Broker))
                return States.Cancel;

            if (DateTime.UtcNow > m_StateTimer[States.WaitAcceptChallenge])
            {
                Broker.SayTo(Captain1, "{0} did not agree to the challenge so the match has been called off.", Captain2.Name);
                Broker.SayTo(Captain2, "You did not agree to the challenge so the match has been called off.");
                Broker.Say("I suspect cowardice.");
                return States.Cancel;
            }

            return States.WaitAcceptChallenge;
        }

        private States GiveBook()
        {
            if (BookReceived == true)
            {   // we already have our rule book, lets get on with it
                return States.DoTeleport;
            }

            if (PlayerHasBook(Captain1, BaseBook.BookSubtype.CTFRules, 1.0) == false)
            {   // give the player a rule book and give them a moment to review and return it to us
                BaseBook book = WriteRuleBook();
                Captain1.Backpack.AddItem(book);
                Broker.SayTo(Captain1, "I have placed a rule book in your backpack.");
                Broker.SayTo(Captain1, "Hand it back to me with the rules you would like to play by.");
                Broker.SayTo(Captain2, "{0} is reviewing the rules for this contest.", Captain1.Name);
            }
            else
            {
                Broker.SayTo(Captain1, "You already have a rule book in your backpack.");
                Broker.SayTo(Captain1, "Hand it back to me with the rules you would like to play by.");
                Broker.SayTo(Captain2, "{0} is reviewing the rules for this contest.", Captain1.Name);
            }

            // 2 minutes to return the rule book
            m_StateTimer[States.WaitBookReturn] = DateTime.UtcNow + TimeSpan.FromSeconds(120);

            return States.WaitBookReturn;
        }

        private States WaitBookReturn()
        {   // sanity - we must have a state time for this state
            if (m_StateTimer.ContainsKey(States.WaitBookReturn) == false)
                return States.Invalid;

            // see if the player quit, or disconnected, etc
            if (!MobileOk(Captain1) || !MobileOk(Captain2) || !MobileOk(Broker))
                return States.Cancel;

            if (BookReceived == true)
            {   // we got our rule book, lets get on with it
                return States.DoTeleport;
            }

            if (DateTime.UtcNow > m_StateTimer[States.WaitBookReturn])
            {
                Broker.SayTo(Captain2, "{0} did not provide me with the rules so the match has been called off.", Captain2.Name);
                Broker.SayTo(Captain1, "You did not provide me with the rules so the match has been called off.");
                Broker.Say("Next time return the rule book to me in a reasonable timeframe.");
                return States.Cancel;
            }

            return States.WaitBookReturn;
        }

        private States DoTeleport()
        {   // should this go to party members that may not be in the area?
            Broker.Say("You and your parties will be teleported to {0} shortly...", this.Name);

            // clear teams
            m_RedTeam.Clear();
            m_BlueTeam.Clear();

            // see if the player quit, or disconnected, etc
            if (!MobileOk(Captain1) || !MobileOk(Captain2))
                return States.Cancel;

            Mobile RedTeamCaptain, BlueTeamCaptain;

            // add the members to their teams
            if (Utility.RandomBool())
            {
                RedTeamCaptain = Captain1; BlueTeamCaptain = Captain2;
                if (Captain1.Party != null) { if (!LoadTeams(Captain1 as PlayerMobile, m_RedTeam)) return States.Cancel; } else m_RedTeam[Captain1 as PlayerMobile] = new PlayerContextData(Captain1, this);
                if (Captain2.Party != null) { if (!LoadTeams(Captain2 as PlayerMobile, m_BlueTeam)) return States.Cancel; } else m_BlueTeam[Captain2 as PlayerMobile] = new PlayerContextData(Captain2, this);
            }
            else
            {
                RedTeamCaptain = Captain2; BlueTeamCaptain = Captain1;
                if (Captain2.Party != null) { if (!LoadTeams(Captain2 as PlayerMobile, m_RedTeam)) return States.Cancel; } else m_RedTeam[Captain2 as PlayerMobile] = new PlayerContextData(Captain2, this);
                if (Captain1.Party != null) { if (!LoadTeams(Captain1 as PlayerMobile, m_BlueTeam)) return States.Cancel; } else m_BlueTeam[Captain1 as PlayerMobile] = new PlayerContextData(Captain1, this);
            }

            // check the eval int cap
            if (CheckTeam(m_RedTeam) == false || CheckTeam(m_BlueTeam) == false)
            {
                RedTeamCaptain.SendMessage("{1}team exceeds the {0} point Eval Int cap.", EvalIntCap, !CheckTeam(m_RedTeam) ? "Your " : "The opposing ");
                BlueTeamCaptain.SendMessage("{1}team exceeds the {0} point Eval Int cap.", EvalIntCap, !CheckTeam(m_BlueTeam) ? "Your " : "The opposing ");
                m_RedTeam.Clear();
                m_BlueTeam.Clear();
                return States.Cancel;
            }

            // 5 seconds until we teleport
            m_StateTimer[States.WaitTeleport] = DateTime.UtcNow + TimeSpan.FromSeconds(5);

            // countdown timer to teleport
            WaitTeleportCount = 0;

            return States.WaitTeleport;
        }

        private States WaitTeleport()
        {
            // sanity - we must have a state time for this state
            if (m_StateTimer.ContainsKey(States.WaitTeleport) == false)
                return States.Invalid;

            // see if the player quit, or disconnected, etc
            if (!MobileOk(Captain1) || !MobileOk(Captain2))
            {
                if (MobileOk(Broker))
                    BroadcastMessage(null, 2, string.Format("{0} has disconnected, your CTF match has been canceled.", MobileOk(Captain1) ? Captain2.Name : Captain1.Name));
                return States.Cancel;
            }

            if (++WaitTeleportCount > 5)
            {
                // go baby, go!
                return States.GameStart;
            }
            else
            {   // teleport in 5, 4, 3 ,2 ,1
                if (WaitTeleportCount == 1)
                    BroadcastMessage(null, SystemMessageColor, string.Format("Teleport in {0}", 5));
                else
                    BroadcastMessage(null, SystemMessageColor, string.Format("{0}", 5 - (WaitTeleportCount - 1)));

                BroadcastBeep(null, BeepSoundID);
            }

            return States.WaitTeleport;
        }

        private States GameStart()
        {
            // N minutes for this round
            m_StateTimer[States.PlayRound] = DateTime.UtcNow + TimeSpan.FromMinutes(RoundMinutes);

            // see if the player quit, or disconnected, etc
            if (!MobileOk(Captain1) || !MobileOk(Captain2))
            {
                if (MobileOk(Broker))
                    BroadcastMessage(null, 2, string.Format("{0} has disconnected, your CTF match has been canceled.", MobileOk(Captain1) ? Captain2.Name : Captain1.Name));
                return States.Cancel;
            }

            // flip coin
            if (Utility.RandomBool())
                Defense = Team.Red;
            else
                Defense = Team.Blue;

            // create the flag
            Flag = new CTFFlag();
            (Flag as CTFFlag).Setup(this);
            Flag.MoveToWorld(Defense == Team.Blue ? BlueBase : RedBase, CustomRegion.Map);

            // create the score plates
            if (RedBaseScore == null || RedBaseScore.Deleted == true)
            {
                RedBaseScore = new CTFScorePlate();
                RedBaseScore.Hue = RedTeamClothing;
                (RedBaseScore as CTFScorePlate).Setup(this, Team.Red);
                RedBaseScore.MoveToWorld(RedBase, CustomRegion.Map);
            }
            if (BlueBaseScore == null || BlueBaseScore.Deleted == true)
            {
                BlueBaseScore = new CTFScorePlate();
                BlueBaseScore.Hue = BlueTeamClothing;
                (BlueBaseScore as CTFScorePlate).Setup(this, Team.Blue);
                BlueBaseScore.MoveToWorld(BlueBase, CustomRegion.Map);
            }

            // create the team chests 
            if (RedBaseChest == null || RedBaseChest.Deleted == true)
            {
                RedBaseChest = new MetalChest();
                RedBaseChest.Hue = RedTeamClothing;
                (RedBaseChest as MetalChest).MaxItems = 1024;
                //(RedBaseChest as MetalChest).MaxWeight = 1024; // readonly? :(
                RedBaseChest.MoveToWorld(RedBase, CustomRegion.Map);
                RedBaseChest.Movable = false;
            }
            if (BlueBaseChest == null || BlueBaseChest.Deleted == true)
            {
                BlueBaseChest = new MetalChest();
                BlueBaseChest.Hue = BlueTeamClothing;
                (BlueBaseChest as MetalChest).MaxItems = 1024;
                //(RedBaseChest as MetalChest).MaxWeight = 1024; // readonly? :(
                BlueBaseChest.MoveToWorld(BlueBase, CustomRegion.Map);
                BlueBaseChest.Movable = false;
            }

            // move the teams into location
            TeleportTeamCTF(m_RedTeam, m_RedBase);
            TeleportTeamCTF(m_BlueTeam, m_BlueBase);

            // okay, the teams have been made, colorize the teams
            ColorizeTeam(m_RedTeam, RedTeamClothing);
            ColorizeTeam(m_BlueTeam, BlueTeamClothing);

            // initial message
            BroadcastMessage(GetTeam(Defense), SystemMessageColor, "Defense.");
            BroadcastMessage(GetTeam(Offense), SystemMessageColor, "Offense.");
            BroadcastMessage(null, SystemMessageColor, "Capture the Flag.");

            // show flag arrow to offense
            BroadcastArrow(GetTeam(Offense));

            // round 1, begin
            Round = 1;

            return States.PlayRound;
        }

        private States PlayRound()
        {
            // sanity - we must have a state time for this state
            if (m_StateTimer.ContainsKey(States.PlayRound) == false)
                return States.Invalid;

            // not enough players to continue
            if (m_RedTeam.Count == 0 || m_BlueTeam.Count == 0)
                return States.Cancel;

            if (DateTime.UtcNow > m_StateTimer[States.PlayRound])
            {
                if (Round >= Rounds)
                    return States.GameEnd;

                BroadcastMessage(null, SystemMessageColor, string.Format("Round over."));

                // start a new round
                Round++;
                if (Round >= Rounds)
                    BroadcastMessage(null, SystemMessageColor, string.Format("Final round."));

                NewRound();
            }
            else
            {   // final round
                if (Round >= Rounds && !OneMinuteWarn && GetRoundRemainingTime().TotalMinutes <= 1.0)
                {
                    OneMinuteWarn = true;
                    BroadcastMessage(null, SystemMessageColor, string.Format("1 minute remaining."));
                }
                else if (Round >= Rounds && !TenSecondWarn && GetRoundRemainingTime().TotalSeconds <= 10.0)
                {
                    TenSecondWarn = true;
                    BroadcastMessage(null, SystemMessageColor, string.Format("10 seconds remaining."));
                }
            }

            return States.PlayRound;
        }

        private States GameEnd()
        {
            // send final score
            SendFinalScore();
            Dictionary<PlayerMobile, PlayerContextData>[] teams = null;

            // it's a tie
            if (DefenseScore == OffenseScore)
            {   // when it's a tie, boot everyone and end the game
                teams = new Dictionary<PlayerMobile, PlayerContextData>[2] { m_RedTeam, m_BlueTeam };
                Stop(teams, "Game over.", KickReason.GameOver);
                Reset();
                return States.Quiescent;
            }
            else
            {   // when we have a winner, the winner gets to go to the After Party

                // kick the losing team
                teams = new Dictionary<PlayerMobile, PlayerContextData>[1] { GetLosingTeam() };
                Stop(teams, "Game over.", KickReason.GameOver);

                //
                // the winning team moves on to the After Party

                // the After Party total time
                m_StateTimer[States.AfterParty] = DateTime.UtcNow + TimeSpan.FromMinutes(AfterPartyMinutes);

                // the after party Victory music
                m_StateTimer[States.AfterPartyVictory] = DateTime.UtcNow + TimeSpan.FromSeconds(16);

                // play stones baby!
                m_StateTimer[States.AfterPartyStones] = DateTime.MaxValue;

                m_StateTimer[States.AfterPartyMessage] = DateTime.UtcNow + TimeSpan.FromSeconds(6);

                // set the mood music (15 seconds)
                CustomRegion.Music = MusicName.Victory;

                return States.AfterParty;
            }
        }

        private States AfterParty()
        {
            // sanity - we must have a state time for this state
            if (!m_StateTimer.ContainsKey(States.AfterParty) ||
                !m_StateTimer.ContainsKey(States.AfterPartyVictory) ||
                !m_StateTimer.ContainsKey(States.AfterPartyStones) ||
                !m_StateTimer.ContainsKey(States.AfterPartyMessage))
                return States.Invalid;

            // not enough players to continue
            if (GetWinningTeam().Count == 0)
                return States.Cancel;

            // see if it's time to send the winners their message
            if (DateTime.UtcNow > m_StateTimer[States.AfterPartyMessage])
            {
                if (GetLosingTeamChest() != null && GetLosingTeamChest().Items.Count > 0)
                {
                    BroadcastMessage(GetWinningTeam(), SystemMessageColor, "The enemy's base can now be looted.");
                }
                m_StateTimer[States.AfterPartyMessage] = DateTime.MaxValue;                     // turn this one off
            }

            // see if it's time to put on the good old UO Stones tune
            if (DateTime.UtcNow > m_StateTimer[States.AfterPartyVictory])
            {   // yeah baby
                CustomRegion.Music = MusicName.Stones2;                                            // play this 
                m_StateTimer[States.AfterPartyVictory] = DateTime.MaxValue;                     // turn this one off
                m_StateTimer[States.AfterPartyStones] = DateTime.UtcNow + new TimeSpan(0, 2, 15);  // length of stones
            }

            // see if it's time to put on the good old UO Stones tune
            if (DateTime.UtcNow > m_StateTimer[States.AfterPartyStones])
            {   // okay, wind up with this lovely classic UO tune
                CustomRegion.Music = MusicName.Magincia;                                           // play this 
                m_StateTimer[States.AfterPartyStones] = DateTime.MaxValue;                      // turn this one off
            }

            // see if the after party is over
            if (DateTime.UtcNow > m_StateTimer[States.AfterParty])
            {
                // now kick the winning team
                Dictionary<PlayerMobile, PlayerContextData>[] teams = new Dictionary<PlayerMobile, PlayerContextData>[1] { GetWinningTeam() };
                Stop(teams, "Game over.", KickReason.GameOver);
                Reset();
                return States.Quiescent;
            }

            return States.AfterParty;
        }
    }
}