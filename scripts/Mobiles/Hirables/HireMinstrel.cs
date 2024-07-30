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

/* Scripts\Mobiles\Hirables\HireMinstrel.cs
 * ChangeLog
 *  1/3/2024, Adam
 *		Initial Version.
 */

using Server.Commands;
using Server.Items;
using Server.Misc;
using System;

namespace Server.Mobiles
{
    public class HireMinstrel : BaseHire
    {

        [Constructable]
        public HireMinstrel()
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.175, 0.5)
        {
            SpeechHue = Utility.RandomSpeechHue();
            NameHue = Notoriety.GetHue(Notoriety.Invulnerable);
            Title = "the minstrel";
            Hue = Utility.RandomSkinHue();

            SetStr(86, 100);
            SetDex(81, 95);
            SetInt(61, 75);

            SetDamage(10, 23);

            SetSkill(SkillName.Musicianship, 66.0, 97.5);
            SetSkill(SkillName.Provocation, 65.0, 87.5);
            SetSkill(SkillName.Discordance, 25.0, 47.5);
            SetSkill(SkillName.Wrestling, 15.0, 37.5);

            InitBody();
            InitOutfit();

            MusicBox mb = new MusicBox();
            mb.Name = GetMusicBoxName(mb);
            mb.Map = Map.Internal;
            AddItem(mb);
            PackItem(Loot.RandomInstrument(), lootType: LootType.UnStealable);

        }

        [CommandProperty(AccessLevel.Administrator)]
        public double BasePay
        {
            get { return CoreAI.MinstrelBasePay; }
            set { CoreAI.MinstrelBasePay = value; }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public MusicBox MusicBox
        {
            get { return GetMusicBox(); }
        }
        [CommandProperty(AccessLevel.GameMaster)]
        public string MusicBoxName
        {
            get { return GetMusicBoxName(GetMusicBox()); }
        }
        public override int RequiredPay()
        {
            double pay = BasePay;

            if (Core.RuleSets.SiegeStyleRules())
                pay *= 3;

            return (int)pay;
        }
        public override bool IsInvulnerable { get { return true; } }
        public override bool ClickTitle { get { return true; } }
        private MusicBox GetMusicBox()
        {
            foreach (Item item in Items)
                if (item is MusicBox mb)
                {
                    mb.Name = Name; // in case the mobile was renammed
                    return mb;
                }
            return null;
        }
        private string GetMusicBoxName(MusicBox mx = null)
        {
            MusicBox mb = mx ?? GetMusicBox();
            if (mb != null)
                return Name;
            return mb.Name;
        }
        public override void OnSpeech(SpeechEventArgs e)
        {
            if (ControlMaster == e.Mobile && !e.Handled && !IsBaseCommand(e))
            {
                MusicBox mb = GetMusicBox();
                bool wasNamed = false;
                // YouTalkingToMe filters for real MB commands, and you must use the mobiles name
                if (mb != null && mb.YouTalkingToMe(e, out wasNamed))
                {
                    // musicbox commands
                    //  Be aware 'stop' is both a music box command and pet command. We take precedence, therefore, hireable mobiles that 'have a' musicbox
                    //  will not see or process the 'stop' command, and to process both would be awkward because your hireable would stop following you for instance
                    //  you CAN issue an 'all stop' as that doesn't use the NPC name
                    SpeechEventArgs mySpeech = new SpeechEventArgs(this, e.Speech, Network.MessageType.Regular, SpeechHue, new int[0], true);
                    mb.OnSpeech(mySpeech);  // this mobile will use their own musicbox profile for playlists, crediting authors, etc
                    e.Handled = true;       // we were handed a valid command, regardless if we could comply
                }
                else
                {   // process native controlled commands
                    base.OnSpeech(e);
                }
            }
            else
            {   // process native uncontrolled commands
                base.OnSpeech(e);
            }
        }
        public void HandleTryTownshipExit()
        {
            bool inside = Regions.TownshipRegion.GetTownshipAt(this) != null;
            if (inside && ControlMaster != null && (ControlOrder == OrderType.Follow || ControlOrder == OrderType.Come))
            {
                Utility.SendOverheadMessage(this, second_timeout: 10, "I'm sorry, I can't leave the township.");

                if (Utility.Chance(0.1))
                    EmoteCommand.OnCommand(new CommandEventArgs(this, "e", "sigh", new string[] { "sigh" }));

                ControlOrder = OrderType.Stop;
            }
        }
        private bool IsBaseCommand(SpeechEventArgs e)
        {
            int[] keywords = e.Keywords;
            string speech = e.Speech;
            for (int i = 0; i < keywords.Length; ++i)
            {
                int keyword = keywords[i];

                switch (keyword)
                {
                    case 0x155: // *come
                        {
                            if (AIObject.WasNamed(speech))
                                return true;
                            return false;
                        }
                    case 0x15A: // *follow
                    case 0x163: // *follow me
                        {
                            if (AIObject.WasNamed(speech))
                                return true;

                            return false;
                        }
                    // our Hire Minstrel process 'stop', so we exclude it here
                    //case 0x161: // *stop
                    //    {
                    //        if (AIObject.WasNamed(speech))
                    //            return true;

                    //        return false;
                    //    }
                    case 0x15D: // *kill
                    case 0x15E: // *attack
                        {
                            if (AIObject.WasNamed(speech))
                                return true;

                            return false;
                        }
                    case 0x16F: // *stay
                        {
                            if (AIObject.WasNamed(speech))
                                return true;
                            return false;
                        }
                }
            }

            return false;
        }
        public override void HandleIncrementalPayMessage(Mobile from, Item dropped, int requiredPay)
        {
            if (dropped.Amount > requiredPay * 10)
            {
                SayTo(from, "Baby, I'm yours!");
                CoreMusicPlayer.PlaySound(this, 0x3D, 20, true);
            }
            else if (dropped.Amount > requiredPay * 5)
            {
                SayTo(from, "Now my momma can get that kidney operation she's been needing!");
                CoreMusicPlayer.PlaySound(this, 1457, 20, true);
            }
            else if (dropped.Amount > requiredPay * 3)
            {
                ;
            }
            else
                ;
        }
        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return false;
        }
        public override bool AllowEquipFrom(Mobile from)
        {
            return false;
        }
        public override void ProcessPay()
        {
            base.ProcessPay();

            if (HoldGold <= 0)
            {
                OnBeforeRelease(ControlMaster);
            }
        }
        public override bool OnBeforeRelease(Mobile controlMaster)
        {
            if (controlMaster != null)
            {
                this.SetDirection(GetDirectionTo(controlMaster));
                EmoteCommand.OnCommand(new CommandEventArgs(this, "e", "bow", new string[] { "bow" }));
                switch (Utility.Random(6))
                {
                    case 0:
                        Say("fare-thee-well");
                        break;
                    case 1:
                        Say("fare well");
                        break;
                    case 2:
                        Say("adieu");
                        break;
                    case 3:
                        Say("very well");
                        break;
                    case 4:
                        Say("very well then");
                        break;
                    case 5:
                        Say("was it my performance?");
                        break;
                }
            }
            if (this is ITownshipNPC tsnpc && !CantWalkLand)
                RangeHome = tsnpc.WanderRange;

            return base.OnBeforeRelease(controlMaster);
        }
        // timer callback to complain about moongate travel
        public virtual void tcMoongate()
        {
            this.Say("I'm sorry, but magic scares me and I do not wish to travel this way.");
        }
        public override TeleportResult OnMagicTravel()
        {
            // BaseCreature OnMagicTravel() call this for gate travel:
            // the OnMagicTravel() override calls tcMoongate() on a delayed callabck
            //	this is so the player escorting will see the message
            //	when they return through the gate to find their NPC
            if (this is ITownshipNPC)
            {   // township minstrels don't travel like this

                Mobile mob = ControlMaster;
                if (mob != null)
                {
                    int save = this.SpeechHue;
                    this.SpeechHue = 0x23F; // this.SpeechHue = 0x3B2;
                    SayTo(mob, "Wait! Please come back!");
                    this.SpeechHue = save;
                }

                // complain about moongate travel
                Timer.DelayCall(TimeSpan.FromSeconds(5.0), new TimerCallback(tcMoongate));
                return TeleportResult.AnyRejected;
            }

            return base.OnMagicTravel();
        }
        //public override bool CheckMovement(Direction d, out int newZ)
        //{
        //    return (CheckNPCMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        //}
        //public bool CheckNPCMovement(Mobile npc, Direction d, out int newZ)
        //{
        //    int newX = npc.X;
        //    int newY = npc.Y;
        //    newZ = npc.Z;

        //    Movement.Movement.Offset(d, ref newX, ref newY);

        //    if (npc is ITownshipNPC)
        //        if (npc is BaseCreature bc && bc.ControlMaster != null && npc.GetDistanceToSqrt(bc.ControlMaster) < 15 && npc.Map == bc.ControlMaster.Map)
        //            if (BaseBoat.FindBoatAt(bc.ControlMaster) != null || FindPlankAt(new Point3D(newX, newY, newZ)) != null)
        //            {
        //                Utility.SendOverheadMessage(npc, second_timeout: 10, 
        //                    "Eww, I don't like boats.");

        //                if (Utility.Chance(0.009))
        //                    EmoteCommand.OnCommand(new CommandEventArgs(this, "e", "puke", new string[] { "puke" }));

        //                return false;
        //            }
        //    return true;
        //}

        public override void InitBody()
        {
            if (this.Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }
        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            if (Female)
            {
                if (Utility.RandomBool())
                {
                    AddItem(new Skirt(Utility.RandomSpecialHue()));
                    AddItem(new Shirt(Utility.RandomSpecialHue()));
                    AddItem(new StrawHat(Utility.RandomSpecialHue()));

                    Lantern lantern = new Lantern();
                    lantern.Duration = TimeSpan.FromDays(365);
                    lantern.Ignite();
                    AddItem(lantern);
                }
                else
                {
                    AddItem(new Kilt(Utility.RandomSpecialHue()));
                    AddItem(new FancyShirt(Utility.RandomSpecialHue()));
                    AddItem(new Doublet(Utility.RandomSpecialHue()));
                    AddItem(new Bonnet(Utility.RandomSpecialHue()));

                    Torch torch = new Torch();
                    torch.Duration = TimeSpan.FromDays(365);
                    torch.Ignite();
                    AddItem(torch);
                }

                AddItem(new Sandals(Utility.RandomSpecialHue()));
            }
            else
            {
                if (Utility.RandomBool())
                {
                    AddItem(new LongPants(Utility.RandomSpecialHue()));
                    AddItem(new Doublet(Utility.RandomSpecialHue()));
                    AddItem(new FloppyHat(Utility.RandomSpecialHue()));
                }
                else
                {
                    AddItem(new ShortPants(Utility.RandomSpecialHue()));
                    AddItem(new JesterSuit(Utility.RandomSpecialHue()));
                    AddItem(new JesterHat(Utility.RandomSpecialHue()));
                }

                AddItem(new FancyShirt(Utility.RandomSpecialHue()));
                AddItem(new Shoes(Utility.RandomSpecialHue()));

                Candle candle = new Candle();
                candle.Duration = TimeSpan.FromDays(365);
                candle.Ignite();
                AddItem(candle);
            }

            AddItem(new Cloak(Utility.RandomSpecialHue()));
        }
        public HireMinstrel(Serial serial)
            : base(serial)
        {
        }
        public override void OnDelete()
        {
            base.OnDelete();
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

            #region Speed up older versions
            if (CurrentSpeed == ActiveSpeed)
            {
                CurrentSpeed = ActiveSpeed = 0.175;
                PassiveSpeed = 0.5;
            }
            else if (CurrentSpeed == PassiveSpeed)
            {
                CurrentSpeed = PassiveSpeed = 0.5;
                ActiveSpeed = 0.175;
            }
            else
            {
                ActiveSpeed = 0.175;
                CurrentSpeed = PassiveSpeed = 0.5;
            }
            #endregion Speed up older versions
        }
    }
}