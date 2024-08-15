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

/* Scripts\Mobiles\Monsters\Polymorphs\Doppelganger.cs
 * ChangeLog
 *  10/12/21, Adam
 *      added [Doppelganger command
 *  9/18/21, Adam
 *      Created.
 */

using Server.Diagnostics;
using Server.Items;
using Server.Targeting;
using System;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Mobiles
{
    public class Doppelganger : BasePolymorphic
    {
        public new static void Initialize()
        {
            Server.CommandSystem.Register("Doppelganger", AccessLevel.Owner, new CommandEventHandler(Doppelganger_OnCommand));
        }

        [Usage("Doppelganger <target from> <target to>")]
        [Description("Copies all clothes and features from one mobile to another.")]
        private static void Doppelganger_OnCommand(CommandEventArgs e)
        {
            try
            {
                e.Mobile.Target = new TargetFrom();
                e.Mobile.SendMessage("Target the mobile to copy from.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }

        private class TargetFrom : Target
        {
            public TargetFrom()
                : base(15, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is Mobile)
                {
                    from.Target = new TargetTo(targ);
                    from.SendMessage("Target the mobile to copy to.");
                }
                else
                {
                    from.SendMessage("That is not a mobile.");
                }
                return;
            }
        }

        private class TargetTo : Target
        {
            Mobile m_from = null;
            public TargetTo(object targ)
                : base(15, false, TargetFlags.None)
            {
                m_from = targ as Mobile;
            }

            protected override void OnTarget(Mobile from, object targ)
            {
                if (targ is Mobile)
                {
                    Mobile m_to = targ as Mobile;
                    // copy the clothes, jewelry, everything
                    Utility.CopyLayers(m_to, m_from, CopyLayerFlags.Default);

                    // now everything else
                    m_to.Name = m_from.Name;
                    m_to.Hue = m_from.Hue;
                    m_to.Body = (m_from.Female) ? 401 : 400;    // get the correct body
                    m_to.Female = m_from.Female;                // get the correct sex
                    m_to.Title = m_from.Title;                  // get the title
                }
                else
                {
                    from.SendMessage("That is not a mobile.");
                }
                return;
            }
        }

        public static void Configure()
        {
            EventSink.WorldLoad += new Server.WorldLoadEventHandler(OnWorldLoad);
        }
        public static List<PlayerMobile> m_players = new List<PlayerMobile>();
        private static PlayerMobile m_PlayerToCopy;
        private static PlayerMobile PlayerToCopy { get { return m_PlayerToCopy; } }
        private static int m_nextPlayer = 0;
        public static PlayerMobile GetPlayer
        {
            get
            {
                if (m_nextPlayer >= Players.Count)
                    m_nextPlayer = 0;
                return Players[m_nextPlayer++];
            }
        }
        public static List<PlayerMobile> Players { get { return m_players; } }
        public static void OnWorldLoad()
        {
            Console.WriteLine("Loading Doppelganger database...");
            foreach (Mobile m in World.Mobiles.Values)
            {
                if (!(m is PlayerMobile)) continue;                                     // must be a player
                //if (!m.Alive) continue;                                               // must be alive
                if (m.AccessLevel > AccessLevel.Player) continue;                       // no staff
                if (m.FindItemOnLayer(Layer.OuterTorso) == null) continue;              // we want clothes
                //if (m.FindItemOnLayer(Layer.OuterTorso) is DeathRobe) continue;       // come on
                if ((m as PlayerMobile).GameTime < TimeSpan.FromHours(14)) continue;    // eliminate those that don't play
                Players.Add(m as PlayerMobile);
            }
            Utility.Shuffle(Players);
            return;
        }

        [Constructable]
        public Doppelganger()
            : base()
        {
            SpeechHue = Utility.RandomSpeechHue();

            Fame = 1000;
            Karma = -1000;

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);

            m_PlayerToCopy = Doppelganger.GetPlayer;
        }

        public Doppelganger(Serial serial)
            : base(serial)
        {
        }

        public override void InitStats()
        {
            base.InitStats();
        }
        public override void InitBody()
        {
            // copy the clothes, jewelry, everything
            Utility.CopyLayers(this, PlayerToCopy, CopyLayerFlags.Default);

            // make sure they stay on the corpse
            DeNewbieLayers(this);

            // now everything else
            this.Name = PlayerToCopy.Name;
            this.Hue = PlayerToCopy.Hue;
            this.Body = (PlayerToCopy.Female) ? 401 : 400;   // get the correct body
            this.Female = PlayerToCopy.Female;               // get the correct death sound
        }
        public override void InitOutfit()
        {
            Item item = this.FindItemOnLayer(Layer.OneHanded);
            if (item != null)
                this.Backpack.DropItem(item);   // might be a rare gear. Who knows!
            item = this.FindItemOnLayer(Layer.TwoHanded);
            if (item != null)
                this.Backpack.DropItem(item);   // might be a rare gear. Who knows!

            if (AI == AIType.AI_Melee)
                switch (Utility.Random(7))
                {
                    case 0: AddItem(new Longsword()); break;
                    case 1: AddItem(new Cutlass()); break;
                    case 2: AddItem(new Broadsword()); break;
                    case 3: AddItem(new Axe()); break;
                    case 4: AddItem(new Club()); break;
                    case 5: AddItem(new Dagger()); break;
                    case 6: AddItem(new Spear()); break;
                }
            else if (AI == AIType.AI_Archer)
                switch (Utility.Random(6))
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        AddItem(new Bow());
                        break;
                    case 4:
                        AddItem(new Crossbow());
                        break;
                    case 5:
                        AddItem(new HeavyCrossbow());
                        break;
                }
            else if (AI == AIType.AI_BaseHybrid)
            {
                PackItem(new Bandage(Utility.RandomMinMax(VirtualArmor, VirtualArmor * 2)), lootType: LootType.UnStealable);
                PackStrongPotions(6, 12);
                PackItem(new Pouch(), lootType: LootType.UnStealable);
            }
        }

        private static int m_totalGold = 0;
        private static int m_magicGear = 0;
        public override void GenerateLoot()
        {
            LogHelper logger = null;
            // we don't use GenerateLoot() for Doppelgangers, we pass the player's loot to the Doppelganger!
            //  Certain things are inappropriate to pass along, like books, keys, bankchecks, etc..
            //  So we just pass along a constrained set of items.
            if (PlayerToCopy != null && PlayerToCopy.Backpack != null)
                foreach (Item item in PlayerToCopy.Backpack.Items)
                {
                    if (item is Gold)
                    {   // throttle gold
                        if (m_totalGold >= 30000)
                            continue;

                        m_totalGold += item.Amount;
                        if (logger == null)
                            logger = new LogHelper("DoppelgangerLoot.log", false);
                        logger.Log(LogType.Text, string.Format("{1} dropped {2} gold. Total gold thus far {0}", m_totalGold, PlayerToCopy, item.Amount));
                    }

                    if (item is BaseWeapon)
                    {
                        // throttle magic gear
                        if (m_magicGear >= 20)
                            continue;

                        BaseWeapon bw = item as BaseWeapon;
                        if (bw.DamageLevel != WeaponDamageLevel.Regular || bw.AccuracyLevel != WeaponAccuracyLevel.Regular || bw.DurabilityLevel != WeaponDurabilityLevel.Regular)
                        {
                            m_magicGear += 1;
                            if (logger == null)
                                logger = new LogHelper("DoppelgangerLoot.log", false);
                            logger.Log(LogType.Text, string.Format("{1} dropped weapon {2}/{3}. Magic gear thus far {0}.", m_magicGear, PlayerToCopy, bw.DamageLevel.ToString(), bw.AccuracyLevel.ToString()));
                        }

                    }

                    if (item is BaseArmor)
                    {
                        // throttle magic gear
                        if (m_magicGear >= 20)
                            continue;

                        BaseArmor ba = item as BaseArmor;
                        if (ba.ProtectionLevel != ArmorProtectionLevel.Regular || ba.DurabilityLevel != ArmorDurabilityLevel.Regular)
                        {
                            m_magicGear += 1;
                            if (logger == null)
                                logger = new LogHelper("DoppelgangerLoot.log", false);
                            logger.Log(LogType.Text, string.Format("{1} dropped armor {2}. Magic gear thus far {0}.: ArmorProtectionLevel:{2}", m_magicGear, PlayerToCopy, ba.ProtectionLevel.ToString()));
                        }
                    }

                    if (item is BaseClothing || item is BaseWeapon || item is BaseArmor || item is Gold)
                    {
                        Item copy = Utility.Dupe(item);
                        this.Backpack.DropItem(copy);
                    }
                }

            if (this.Backpack != null)
                foreach (Item item in this.Backpack.Items)
                {
                    if (item != null && item is Bandage)
                        // for rares fun
                        item.Hue = Utility.RandomDyedHue();
                }

            if (logger != null)
                logger.Finish();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 1;
            writer.Write(version); // version

            switch (version)
            {
                case 1:
                    writer.Write(m_PlayerToCopy);
                    break;
                case 0:
                    break;
            }
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    m_PlayerToCopy = (PlayerMobile)reader.ReadMobile();
                    break;
                case 0:
                    break;
            }

            if (m_PlayerToCopy == null)
                m_PlayerToCopy = new PlayerMobile();
        }
    }
}