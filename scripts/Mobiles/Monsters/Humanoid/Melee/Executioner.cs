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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Executioner.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 *  7/02/06, Kit
 *		InitBody/InitOutfit additions
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *  9/26/04, Jade
 *      Decreased gold drop from (750, 800) to (300, 450)
 *	7/6/04, Adam
 *		1. implement Jade's new Category Based Drop requirements
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;

namespace Server.Mobiles
{
    public class Executioner : BaseCreature
    {
        [Constructable]
        public Executioner()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            SpeechHue = Utility.RandomSpeechHue();
            Title = "the executioner";
            Hue = Utility.RandomSkinHue();

            if (Core.RuleSets.AngelIslandRules())
            {
                SetStr(386, 400);
                SetDex(70, 90);
                SetInt(161, 175);

                SetDamage(20, 30);
            }
            else
            {
                SetStr(386, 400);
                SetDex(151, 165);
                SetInt(161, 175);

                SetDamage(8, 10);
            }

            SetSkill(SkillName.Anatomy, 125.0);
            SetSkill(SkillName.Fencing, 46.0, 77.5);
            SetSkill(SkillName.Macing, 35.0, 57.5);
            SetSkill(SkillName.Poisoning, 60.0, 82.5);
            SetSkill(SkillName.MagicResist, 83.5, 92.5);
            SetSkill(SkillName.Swords, 125.0);
            SetSkill(SkillName.Tactics, 125.0);
            SetSkill(SkillName.Lumberjacking, 125.0);

            InitBody();
            InitOutfit();

            Fame = 5000;
            Karma = -5000;

            VirtualArmor = 40;
        }

        public override int Meat { get { return 1; } }
        public override bool AlwaysMurderer { get { return true; } }

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
            if (Female)
                AddItem(new Skirt(Utility.RandomNeutralHue()));
            else
                AddItem(new ShortPants(Utility.RandomNeutralHue()));

            Item hair = new Item(Utility.RandomList(0x203B, 0x2049, 0x2048, 0x204A));
            hair.Hue = Utility.RandomNondyedHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            AddItem(new ThighBoots(Utility.RandomRedHue()));
            AddItem(new Surcoat(Utility.RandomRedHue()));
            AddItem(new ExecutionersAxe());
        }

        public override void OnGaveMeleeAttack(Mobile target)
        {
            if (Core.RuleSets.AngelIslandRules())
                if (0.25 >= Utility.RandomDouble() && target is PlayerMobile)
                    target.Damage(Utility.RandomMinMax(15, 25), this, this);

            base.OnGaveMeleeAttack(target);
        }
        public override void OnThink()
        {   // beware when you see a brigand wielding one. Early reports tell that executioners have the reflective armor spell on permanent.
            // https://web.archive.org/web/20010608170555/http://uo.stratics.com/hunters/executioner.shtml

            if (Combatant != null && this.MagicDamageAbsorb < 1)
            {   // borrowed from MeerCaptain
                // MeerCaptain: Utility.RandomMinMax(5, 7);
                this.MagicDamageAbsorb = Utility.RandomMinMax(3, 7);    // hand tuned to be more reasonable
                this.FixedParticles(0x375A, 10, 15, 5037, EffectLayer.Waist);
                this.PlaySound(0x1E9);
            }

            base.OnThink();
        }
        public Executioner(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                if (Spawning) { }
                PackGold(300, 450);
                // Category 2 MID
                PackMagicItem(1, 1, 0.05);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // http://web.archive.org/web/20020606054853/uo.stratics.com/hunters/executioner.shtml
                    // 750 - 800 Gold, Magic Items
                    if (Spawning)
                    {
                        PackGold(750, 800);
                    }
                    else
                    {

                        PackMagicStuff(1, 1, 0.05);
                    }
                }
                else
                {
                    AddLoot(LootPack.FilthyRich);
                    AddLoot(LootPack.Meager);
                }
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
        }
    }
}