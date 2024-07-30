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

/* Scripts\Engines\ChampionSpawn\Champs\BaseChampion.cs
 * ChangeLog:
 *  4/4/2024, Adam (m_TreatAsChamp)
 *      Allow champ mobs to be used in ways other than as a champion. I.e., no crazy loot
 *	3/15/08, Adam
 *		Move DistributedLoot() to Scripts/Misc/ChampLoot.cs to be used as a shared facility.
 *			i.e., new ChampLootPack(this).DistributedLoot()
 *  3/19/07, Adam
 *      Pacakge up loot generation and move it into BaseCreature.cs
 *      We want to be able to designate any creature as a champ via our Mobile Factory
 *  03/09/07, plasma    
 *      Removed cannedevil namespace reference
 *  1/11/07, Adam
 *      Check the the skull type is not None before dropping it.
 *  01/05/07, plasma
 *      Changed CannedEvil namespace to ChampionSpawn for cleanup!
 *  8/16/05, Rhiannon
 *		Added constructors to allow champions to have individual min and max speeds.
 *	12/6/05, Adam
 *		Cleanup:
 *			1. get rid of funky OnDeath() override
 *			2. Check OnDeath() for creature: (this is AdamsCat || this is CraZyLucY)
 *				don't drop skull
 *	12/23/04, Adam
 *		Remove the check to see if we're in felucca before dropping champ skull.
 *		Search string: c.DropItem( new ChampionSkull( SkullType ) );
 *	12/15/04, Adam
 *		While we're in Shame III, we want to hard code the gold drop location - OnBeforeDeath()
 *			to return to normal operation, see the comments in OnBeforeDeath()
 *	10/17/04, Adam
 *		Increase gold drop from: Gold( 400, 600 )
 *			to: Gold( 800, 1200 )
 *		Old drop was like ~10K, now it will be ~30K
 *		(25 piles * 1200 = 30K gold)
 *	7/1/04, Adam
 *		Adam's Cat uses base champ code, but doesn't want the ChampionSkull drop
 * 		Add new OnDeath() method that skips the skull drop
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/29/04, mith
 *		Removed Justice rewards now that virtues are disabled.
 *		Modified the amount of items given. Modified weapon rewards to also reward with armor.
 *	3/23/04 code changes by mith:
 *		OnBeforeDeath() - replaced GivePowerScrolls with GiveMagicItems
 *		GiveMagicItems() - new function to award players with magic items upon death of champion
 *		CreateWeapon()/CreateArmor() - called by GiveMagicItems to create random item to be awarded to player
 *	3/17/04 code changes by mith:
 *		OnBeforeDeath() - Decreased radius of gold drop
 *		GoodiesTimer.OnClick() - Decreased amount of random gold dropped
 */

using Server.Diagnostics;
using Server.Engines.ChampionSpawn;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public abstract class BaseChampion : BaseCreature
    {
        public BaseChampion(AIType aiType)
            : this(aiType, FightMode.All | FightMode.Closest)
        {
        }

        public BaseChampion(AIType aiType, FightMode mode)
            : base(aiType, mode, 18, 1, 0.1, 0.2)
        {
        }

        public BaseChampion(AIType aiType, double dActiveSpeed, double dPassiveSpeed)
            : base(aiType, FightMode.All | FightMode.Closest, 18, 1, dActiveSpeed, dPassiveSpeed)
        {
        }

        public BaseChampion(AIType aiType, FightMode mode, double dActiveSpeed, double dPassiveSpeed)
            : base(aiType, mode, 18, 1, dActiveSpeed, dPassiveSpeed)
        {
        }

        public BaseChampion(Serial serial)
            : base(serial)
        {
        }

        //private bool m_TreatAsChamp = true;
        [CommandProperty(AccessLevel.Seer)]
        public bool TreatAsChamp { get { return !NoKillAwards; } set { NoKillAwards = !value; } }

        public override bool IsChampion { get { return TreatAsChamp; } }

        // Adam; we don't want our champs to stop healing when the players leave the sector (a trick for killing monsters)
        public override bool CanDeactivate { get { return Hits == HitsMax; } }
        // Adam; we don't care if we cannot access them, we will probably have an ability to deal with it
        public override bool IgnoreCombatantAccessibility { get { return true; } }
        // Adam; and why the f' can't champs break crates laid on the ground?
        public override bool CanDestroyObstacles { get { return true; } }
        public override bool IAIOkToInvestigate(Mobile playerToInvestigate)
        {   // This limits how often we will investigate a player
            // But since we are a champion, we won't be dissuaded
            return true;
        }
        public abstract ChampionSkullType SkullType { get; }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            // version 2 - removes m_TreatAsChamp

            // version 1 - obsolete
            //writer.Write(m_TreatAsChamp);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {   // version 2 - removes m_TreatAsChamp
                        goto case 1;
                    }
                case 1:
                    {
                        if (version == 1)
                            /*m_TreatAsChamp = */
                            reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    break;
            }
        }

        public override bool OnBeforeDeath()
        {
            return base.OnBeforeDeath();
        }

        public override void OnDeath(Container c)
        {
            // adam's cat, crazy lucy, and Azothu don't have skulls
            if (IsChampion)
                if (SkullType != ChampionSkullType.None)
                    c.DropItem(new ChampionSkull(SkullType));

            base.OnDeath(c);
        }
        public override int GoldSplashPile { get { return Utility.RandomMinMax(800, 1200); } }
        public override void DistributeLoot()
        {
            if (IsChampion)
            {
                if (this.Map != null)
                {
                    GiveMagicItems(this, magicItems: ChampLootPack.GetChampMagicItems(), specialRewards: ChampLootPack.GetChampSpecialRewards());
                    DoGoodies(this);
                }
            }
        }

        #region INSERT
        public static void DoGoodies(BaseCreature champ)
        {
            // Adam: While we're in Shame III, we want to hard code the gold drop location.
            //	To return to normal operation, delete the next two lines + the if() block
            int X = champ.X;
            int Y = champ.Y;
            Map map = champ.Map;
            if (map != null)
            {
                Region reg = champ.Region;
                if (reg != map.DefaultRegion)
                {
                    if (reg.Name == "Shame")
                    {
                        X = 5609;
                        Y = 193;
                    }
                }
            }

            if (map != null)
            {
                for (int x = -2; x <= 2; ++x)
                {
                    for (int y = -2; y <= 2; ++y)
                    {
                        double dist = Math.Sqrt(x * x + y * y);

                        if (dist <= 12)
                            new GoodiesTimer(champ, map, X + x, Y + y).Start();
                    }
                }
            }
        }
        private class GoodiesTimer : Timer
        {
            private Mobile m_Mobile;
            private Map m_Map;
            private int m_X, m_Y;

            public GoodiesTimer(Mobile m, Map map, int x, int y)
                : base(TimeSpan.FromSeconds(Utility.RandomDouble() * 10.0))
            {
                m_Mobile = m;
                m_Map = map;
                m_X = x;
                m_Y = y;
            }

            protected override void OnTick()
            {
                int z = m_Map.GetAverageZ(m_X, m_Y);
                bool canFit = Utility.CanFit(m_Map, m_X, m_Y, z, 6, Utility.CanFitFlags.requireSurface);

                for (int i = -3; !canFit && i <= 3; ++i)
                {
                    canFit = Utility.CanFit(m_Map, m_X, m_Y, z + i, 6, Utility.CanFitFlags.requireSurface);

                    if (canFit)
                        z += i;
                }

                if (!canFit)
                    return;

                if (m_Mobile is BaseCreature bc && bc.GoldSplashPile != 0)
                {
                    // Adam: 25 piles * 1200 = 30K gold
                    Gold g = new Gold(bc.GoldSplashPile);

                    g.MoveToWorld(new Point3D(m_X, m_Y, z), m_Map);

                    if (0.5 >= Utility.RandomDouble())
                    {
                        switch (Utility.Random(3))
                        {
                            case 0: // Fire column
                                {
                                    Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x3709, 10, 30, 5052);
                                    Effects.PlaySound(g, g.Map, 0x208);

                                    break;
                                }
                            case 1: // Explosion
                                {
                                    Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36BD, 20, 10, 5044);
                                    Effects.PlaySound(g, g.Map, 0x307);

                                    break;
                                }
                            case 2: // Ball of fire
                                {
                                    Effects.SendLocationParticles(EffectItem.Create(g.Location, g.Map, EffectItem.DefaultDuration), 0x36FE, 10, 10, 5052);

                                    break;
                                }
                        }
                    }
                }
            }
        }
        public static void GiveMagicItems(BaseCreature champ, List<Item> magicItems, List<Item> specialRewards)
        {
            int item_number = 1;
            BaseCreature creature = champ as BaseCreature;
            if (creature == null) return;
            LogHelper logger = new LogHelper("ChampLoot.log", false);
            try
            {
                ArrayList toGive = WhoGetsWhat(champ, logger);

                logger.Log(LogType.Text, string.Format("{0} slayed at location {1} on {2} ", champ, champ.Location, DateTime.UtcNow));

                BaseCreature.LogPercentageThisMobileIsEntitled(logger, toGive.Count, magicItems.Count + specialRewards.Count);

                if (toGive.Count > 0)
                {
                    for (int i = 0; i < magicItems.Count; ++i)
                    {
                        Mobile m = (Mobile)toGive[i % toGive.Count];

                        Item reward = magicItems[i];

                        BaseCreature.LogLootLevelForThisPlayer(logger, item_number, m, reward);

                        if (reward != null)
                        {
                            // Drop the new weapon into their backpack and send them a message.
                            m.SendMessage("You have received a special item!");

                            if (reward.GetFlag(LootType.Rare))
                                m.RareAcquisitionLog(reward, "Champ loot");

                            m.AddToBackpack(reward);

                            logger.Log(LogType.Mobile, m, "alive:" + m.Alive.ToString());
                            logger.Log(LogType.Item, reward, string.Format("Hue:{0}:Rare:{1}",
                                reward.Hue,
                                (reward is BaseWeapon || reward is BaseArmor || reward is BaseClothing || reward is BaseJewel) ? "False" : "True"));

                            BaseCreature.LogWhoGotWhat(logger, item_number, m, reward);
                        }

                        item_number++;
                    }

                    for (int i = 0; i < specialRewards.Count; ++i)
                    {
                        Mobile m = (Mobile)toGive[i % toGive.Count];

                        Item reward = specialRewards[i];

                        if (reward != null)
                        {
                            // Drop the new weapon into their backpack and send them a message.
                            m.SendMessage("You have received a special item!");

                            if (reward.GetFlag(LootType.Rare))
                                m.RareAcquisitionLog(reward, "Champ loot");

                            m.AddToBackpack(reward);

                            logger.Log(LogType.Mobile, m, "alive:" + m.Alive.ToString());
                            logger.Log(LogType.Item, reward, string.Format("Hue:{0}:Rare:{1}",
                                reward.Hue,
                                (reward is BaseWeapon || reward is BaseArmor || reward is BaseClothing || reward is BaseJewel) ? "False" : "True"));

                            LogWhoGotWhat(logger, item_number, m, reward);
                        }

                        item_number++;
                    }
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
            finally
            {
                // close the log file
                logger.Finish();
            }
        }

        #endregion INSERT
    }
}