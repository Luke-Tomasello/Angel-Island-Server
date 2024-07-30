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

using Server.Spells;
using Server.Spells.First;
using System;

namespace Server.Items
{
    [FlipableAttribute(0xDF1, 0xDF0)]
    public class GlacialStaff : BaseStaff
    {
        public override WeaponAbility PrimaryAbility { get { return WeaponAbility.WhirlwindAttack; } }
        public override WeaponAbility SecondaryAbility { get { return WeaponAbility.ParalyzingBlow; } }

        /*public override int AosStrengthReq { get { return 30; } }
		public override int AosMinDamage { get { return 15; } }
		public override int AosMaxDamage { get { return 18; } }
		public override int AosSpeed { get { return 40; } }*/

        public override int OldStrengthReq { get { return 30; } }
        public override int OldMinDamage { get { return 5; } }
        public override int OldMaxDamage { get { return 8; } }
        public override int OldSpeed { get { return 33; } }

        public override int InitMinHits { get { return 35; } }
        public override int InitMaxHits { get { return 75; } }
        public int Iceballact = 0;
        public int Freezeact = 0;
        public int IceStrikeact = 0;

        [Constructable]
        public GlacialStaff()
            : base(0xDF1)
        {
            Name = "Glacial Staff";
            Hue = 1152;
            Weight = 0.0;

            // 
            //WeaponAttributes.MageWeapon = 21;

            //----->
            //int Iceballact = 0; // old vars ... need 2 do them Public =P
            //int Freezeact = 0;
            //int IceStrikeact = 0;
            string first = "bug";
            string second = "bug";

            switch (Utility.Random(3))
            {
                case 0: Iceballact = 1; first = " [Ice Ball /"; break;
                case 1: Freezeact = 1; first = " [Freezing /"; break;
                case 2: IceStrikeact = 1; first = " [Ice Strike /"; break;
            }

            if (Iceballact == 1)
            {
                switch (Utility.Random(2))
                {
                    case 0: IceStrikeact = 1; second = " Ice Strike]"; break;
                    case 1: Freezeact = 1; second = " Freezing]"; break;
                }
            }

            else if (Freezeact == 1)
            {
                switch (Utility.Random(2))
                {
                    case 0: IceStrikeact = 1; second = " Ice Strike]"; break;
                    case 1: Iceballact = 1; second = " Ice Ball]"; break;
                }
            }

            else if (IceStrikeact == 1)
            {
                switch (Utility.Random(2))
                {
                    case 0: Iceballact = 1; second = " Ice Ball]"; break;
                    case 1: Freezeact = 1; second = " Freezing]"; break;
                }
            }


            String Spellsact = "Glacial Staff" + first + second;
            Name = Spellsact;
            //int IceStrikeact = 1;

            //<----
            /*
			switch (Utility.Random(3))
			{
				case 0: WeaponAttributes.HitHarm = 25; break;
				case 1: WeaponAttributes.HitHarm = 35; break;
				case 2: WeaponAttributes.HitHarm = 45; break;
			}
			switch (Utility.Random(3))
			{
				case 0: Attributes.WeaponDamage = 25; break;
				case 1: Attributes.WeaponDamage = 35; break;
				case 2: Attributes.WeaponDamage = 45; break;
			}*/
        }
        public override void GetDamageTypes(Mobile wielder, out int phys, out int fire, out int cold, out int pois, out int nrgy)
        {
            phys = fire = pois = nrgy = 0;
            cold = 100;
        }

        public GlacialStaff(Serial serial)
            : base(serial)
        {
        }
        //---------------------->
        public override bool HandlesOnSpeech { get { return true; } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled)
            {
                Mobile m = e.Mobile;
                int m_Keyword = 1;
                if (this.Parent == m)
                {
                    switch (2)
                    {
                        case 2:
                            {
                                //Iceball ---------------------------------------
                                if (Iceballact == 0)
                                {
                                    goto case 1;
                                }

                                bool isMatch = false;
                                string m_Substring = "Des Corp Del";

                                if (m_Keyword >= 0 && e.HasKeyword(m_Keyword))
                                    isMatch = true;
                                else if (m_Substring != null && e.Speech.ToLower().IndexOf(m_Substring.ToLower()) >= 0)
                                    isMatch = true;

                                if (!isMatch)
                                    goto case 1;
                                if (m.BeginAction(typeof(GlacialStaff)))
                                {

                                    Spell iceball = new IceBallSpell(m, this);
                                    iceball.Cast();
                                }
                                else
                                {
                                    m.SendMessage("The Staff is too Cold to use");
                                }
                                Timer.DelayCall(TimeSpan.FromSeconds(4.0), new TimerStateCallback(ReleasecastLock), m);

                                break;

                            }
                        case 1:
                            {
                                //Freezeparalyze ---------------------------------
                                if (Freezeact == 0)
                                {
                                    goto case 0;
                                }

                                bool isMatch = false;
                                string m_Substring = "An Ex Del";

                                if (m_Keyword >= 0 && e.HasKeyword(m_Keyword))
                                    isMatch = true;
                                else if (m_Substring != null && e.Speech.ToLower().IndexOf(m_Substring.ToLower()) >= 0)
                                    isMatch = true;

                                if (!isMatch)
                                    goto case 0;
                                if (m.BeginAction(typeof(GlacialStaff)))
                                {
                                    Spell FreezeParalyze = new FreezeParalyzeSpell(m, this);
                                    FreezeParalyze.Cast();
                                }
                                else
                                {
                                    m.SendMessage("The Staff is too Cold to use");
                                }
                                Timer.DelayCall(TimeSpan.FromSeconds(15.0), new TimerStateCallback(ReleasecastLock), m);
                                break;
                            }
                        case 0:
                            {
                                //Ice Strike -------------------------------------
                                if (IceStrikeact == 0)
                                {
                                    break;
                                }

                                bool isMatch = false;
                                string m_Substring = "In Corp Del";

                                if (m_Keyword >= 0 && e.HasKeyword(m_Keyword))
                                    isMatch = true;
                                else if (m_Substring != null && e.Speech.ToLower().IndexOf(m_Substring.ToLower()) >= 0)
                                    isMatch = true;

                                if (!isMatch)
                                    break;
                                if (m.BeginAction(typeof(GlacialStaff)))
                                {
                                    Spell IceStrike = new IceStrikeSpell(m, this);
                                    IceStrike.Cast();
                                }
                                else
                                {
                                    m.SendMessage("The Staff is too Cold to use");
                                }
                                Timer.DelayCall(TimeSpan.FromSeconds(15.0), new TimerStateCallback(ReleasecastLock), m);
                                break;
                            }
                    }
                }
            }
        }


        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("The Magicals Words are : Des Corp Del, Iceball / An Ex Del, Freeze / In Corp Del, Ice Strike.");
            //	from.SendLocalizedMessage( 502434 ); // What should I use these scissors on?
            //	Spell iceball = new IceBallSpell( from, this );
            //	Spell FreezeParalyze = new FreezeParalyzeSpell( from, this );
            //	iceball.Cast();
        }
        //<----------------------
        private static void ReleasecastLock(object state)
        {
            ((Mobile)state).EndAction(typeof(GlacialStaff));
        }
        //<----------------------------------------------------

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((int)Iceballact);
            writer.Write((int)Freezeact);
            writer.Write((int)IceStrikeact);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            Iceballact = reader.ReadInt();
            Freezeact = reader.ReadInt();
            IceStrikeact = reader.ReadInt();
        }
    }
}