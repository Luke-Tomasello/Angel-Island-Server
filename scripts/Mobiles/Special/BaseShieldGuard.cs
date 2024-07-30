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

/* Scripts/Mobiles/Special/BaseShieldGuard.cs
 * CHANGELOG
 *  4/15/23, Yoar
 *      Order/Chaos shields are now also usable by Order/Chaos guild-aligned players.
 *      Added a special check so that these players may also acquire a shield.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Engines.Alignment;
using Server.Guilds;
using Server.Items;

namespace Server.Mobiles
{
    public abstract class BaseShieldGuard : BaseCreature
    {
        public BaseShieldGuard()
            : base(AIType.AI_Melee, FightMode.Aggressor, 14, 1, 0.8, 1.6)
        {
            InitStats(1000, 1000, 1000);
            Title = "the guard";

            SpeechHue = Utility.RandomSpeechHue();

            Hue = Utility.RandomSkinHue();

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");

                AddItem(new FemalePlateChest());
                AddItem(new PlateArms());
                AddItem(new PlateLegs());

                switch (Utility.Random(2))
                {
                    case 0: AddItem(new Doublet(Utility.RandomNondyedHue())); break;
                    case 1: AddItem(new BodySash(Utility.RandomNondyedHue())); break;
                }

                switch (Utility.Random(2))
                {
                    case 0: AddItem(new Skirt(Utility.RandomNondyedHue())); break;
                    case 1: AddItem(new Kilt(Utility.RandomNondyedHue())); break;
                }
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");

                AddItem(new PlateChest());
                AddItem(new PlateArms());
                AddItem(new PlateLegs());

                switch (Utility.Random(3))
                {
                    case 0: AddItem(new Doublet(Utility.RandomNondyedHue())); break;
                    case 1: AddItem(new Tunic(Utility.RandomNondyedHue())); break;
                    case 2: AddItem(new BodySash(Utility.RandomNondyedHue())); break;
                }
            }

            Item hair = new Item(Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2047, 0x2049, 0x204A));

            hair.Hue = Utility.RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;

            AddItem(hair);

            if (Utility.RandomBool() && !this.Female)
            {
                Item beard = new Item(Utility.RandomList(0x203E, 0x203F, 0x2040, 0x2041, 0x204B, 0x204C, 0x204D));

                beard.Hue = hair.Hue;
                beard.Layer = Layer.FacialHair;
                beard.Movable = false;

                AddItem(beard);
            }

            VikingSword weapon = new VikingSword();
            weapon.Movable = false;
            AddItem(weapon);

            BaseShield shield = Shield;
            shield.Movable = false;
            AddItem(shield);

            PackGold(250, 500);

            Skills[SkillName.Anatomy].Base = 120.0;
            Skills[SkillName.Tactics].Base = 120.0;
            Skills[SkillName.Swords].Base = 120.0;
            Skills[SkillName.MagicResist].Base = 120.0;
            Skills[SkillName.DetectHidden].Base = 100.0;
        }

        public abstract int Keyword { get; }
        new public abstract BaseShield Shield { get; }
        public abstract int SignupNumber { get; }
        public abstract GuildType Type { get; }

        public override bool HandlesOnSpeech(Mobile from)
        {
            if (from.InRange(this.Location, 2))
                return true;

            return base.HandlesOnSpeech(from);
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.HasKeyword(Keyword) && e.Mobile.InRange(this.Location, 2))
            {
                e.Handled = true;

                Mobile from = e.Mobile;
                Guild g = from.Guild as Guild;

                bool valid = (g != null && g.Type == Type);

                #region Alignment
                if (!valid && AlignmentSystem.Enabled)
                {
                    switch (Type)
                    {
                        case GuildType.Order: valid = AlignmentSystem.Find(e.Mobile) == AlignmentType.Order; break;
                        case GuildType.Chaos: valid = AlignmentSystem.Find(e.Mobile) == AlignmentType.Chaos; break;
                    }
                }
                #endregion

                if (!valid)
                {
                    Say(SignupNumber);
                }
                else
                {
                    Container pack = from.Backpack;
                    BaseShield shield = Shield;
                    Item twoHanded = from.FindItemOnLayer(Layer.TwoHanded);

                    if ((pack != null && pack.FindItemByType(shield.GetType()) != null) || (twoHanded != null && shield.GetType().IsAssignableFrom(twoHanded.GetType())))
                    {
                        Say(1007110); // Why dost thou ask about virtue guards when thou art one?
                        shield.Delete();
                    }
                    else if (from.PlaceInBackpack(shield))
                    {
                        Say(Utility.Random(1007101, 5));
                        Say(1007139); // I see you are in need of our shield, Here you go.
                        from.AddToBackpack(shield);
                    }
                    else
                    {
                        from.SendLocalizedMessage(502868); // Your backpack is too full.
                        shield.Delete();
                    }
                }
            }

            base.OnSpeech(e);
        }

        public BaseShieldGuard(Serial serial)
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