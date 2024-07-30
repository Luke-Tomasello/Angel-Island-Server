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

/* Engines/AngelIsland/ParoleOfficer.cs, last modified 4/11/04 by Pixie.
 * ChangeLog:
 * 5/05/04, Pixie
 *	Added Sandals.
 * 5/05/04, Pixie
 *	Changed to Mage AI, changed to always female, changed clothing, skills, staff.
 *	Added message on attack.
 * 4/30/04, Pixie
 *  changed it so the parole officer uses Say instead or the private SayTo
 * 4/11/04 change by Pixie
 *  changed clothing, changed to carrying staff.
 * Created by Pixie 4/8/04
 */


using Server.Items;
using System;

namespace Server.Mobiles
{
    public class ParoleOfficer : BaseCreature
    {

        [Constructable]
        public ParoleOfficer()
            //: base( AIType.AI_Mage, FightMode.Aggressor, 2, 1, 1, 2 )
            : base(AIType.AI_Mage, FightMode.Aggressor, 10, 5, 0.2, 0.4)
        {
            SetStr(81, 105);
            SetDex(191, 215);
            SetInt(126, 150);
            SetHits(450, 550);

            Title = "the parole officer";

            SpeechHue = Utility.RandomSpeechHue();

            Hue = Utility.RandomSkinHue();

            Female = true;
            Body = 0x191;
            Name = NameList.RandomName("female");

            FloppyHat hat = new FloppyHat(Utility.RandomNondyedHue());
            hat.Movable = false;
            AddItem(hat);

            Shirt shirt = new Shirt(Utility.RandomNondyedHue());
            shirt.Movable = false;
            AddItem(shirt);

            Kilt kilt = new Kilt(Utility.RandomNondyedHue());
            kilt.Movable = false;
            AddItem(kilt);

            Sandals sandals = new Sandals(Utility.RandomNondyedHue());
            sandals.Movable = false;
            AddItem(sandals);

            Item hair = new Item(0x203C);//long hair
            hair.Hue = Utility.RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;

            AddItem(hair);

            QuarterStaff weapon = new QuarterStaff();
            weapon.Movable = false;
            AddItem(weapon);

            Container pack = new Backpack();

            pack.Movable = false;

            pack.DropItem(new Gold(10, 25));

            AddItem(pack);

            SetSkill(SkillName.DetectHidden, 100.0);
            SetSkill(SkillName.EvalInt, 80.2, 100.0);
            SetSkill(SkillName.Magery, 95.1, 100.0);
            SetSkill(SkillName.Meditation, 100.0);
            SetSkill(SkillName.MagicResist, 77.5, 100.0);
            SetSkill(SkillName.Tactics, 95.0, 100.0);
            SetSkill(SkillName.Anatomy, 95.0, 100.0);
            SetSkill(SkillName.Macing, 100.0);
            SetSkill(SkillName.Wrestling, 90.0, 100.0);
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 2))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!e.Handled &&
                 from is PlayerMobile &&
                 from.InRange(this.Location, 2) &&
                 e.HasKeyword(0x9E)) //*time*
            {
                PlayerMobile pm = (PlayerMobile)from;

                string response;
                response = "Time is gold, ";
                if (from.Female)
                {
                    response += "little lassy";
                }
                else
                {
                    response += "my lad";
                }
                Say(response);

                e.Handled = true;
            }

            base.OnSpeech(e);
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            if (!Server.Misc.AttackMessage.CheckAggressions(aggressor, this))
            {
                if (Utility.RandomDouble() < .5)
                {
                    Say("Warden! " + aggressor.Name + " is being bad again!");
                }
                else
                {
                    Say("Guards, help me! " + aggressor.Name + " is out of control!");
                }
            }
            base.AggressiveAction(aggressor, criminal);
        }

        public override bool OnGoldGiven(Mobile from, Gold dropped)
        {
            if (from is PlayerMobile && dropped.Amount >= 500)
            {
                PlayerMobile pm = (PlayerMobile)from;

                //reduce Timer by X for each 500 gold drop by 1/2 hour
                double timeReduced = 0;
                for (int i = 500; i <= dropped.Amount; i += 500)
                {
                    pm.ReduceKillTimersByHours(.5);
                    timeReduced += .5;
                }
                if (pm.ShortTermMurders == 0 && pm.LongTermMurders == 0 && pm.MinimumSentence > System.DateTime.UtcNow)
                {
                    TimeSpan delta = pm.MinimumSentence - DateTime.UtcNow;
                    int days = delta.Days;
                    int hours = delta.Hours;
                    int minutes = delta.Minutes;
                    Say("The courts have ordered a sentence of {0} days, {1} hours, and {2} minutes.", days, hours, minutes);
                    Say("The Warden is watching me. I cannot help you.");
                    Say("But thank you for the gold!");
                }
                else
                {
                    Say("My thanks!  We'll see what we can do about your time here");
                    from.SendMessage("Your time has been reduced by " + timeReduced + " hours.");
                }
                dropped.Delete();
            }
            else
            {
                Say("My thanks, I'll be sure to use this wisely");
                dropped.Delete();
            }

            return true;
        }

        public ParoleOfficer(Serial serial)
            : base(serial)
        {
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
        }


    }
}