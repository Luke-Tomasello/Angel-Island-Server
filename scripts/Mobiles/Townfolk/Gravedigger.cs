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
// Gravedigger NPC
//
// Lore:
// A long-serving, peaceful caretaker of the dead.
// Known to the townsfolk as a quiet retiree who tends the graveyards
// and asks nothing of the realm.
//
// Design intent:
// The Gravedigger is intentionally harmless and non-hostile.
// Killing him is treated as a profound social transgression.
//
// Mechanics:
// - The Gravedigger carries no Fame or Karma himself.
// - Any player who participates in his death is penalized by
//   dropping to the next lower Fame tier.
// - This penalty is tier-based (not numeric) and cannot be farmed.
//
// Rationale:
// This NPC exists to reinforce social norms and restraint.
// The correct choice is to leave him in peace.

/* Scripts/Mobiles/Townfolk/Gravedigger.cs
 *
 * ChangeLog
 *  2/9/2026, Adam
 *      Created
 */

using Server.Engines.PartySystem;
using Server.Items;
using Server.Misc;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class Gravedigger : BaseCreature
    {

        [Constructable]
        public Gravedigger()
            : base(AIType.AI_Melee, FightMode.Aggressor, 22, 1, 0.2, 1.0)
        {
            InitBody();
            InitOutfit();
            Title = "the Gravedigger";

            SetSkill(SkillName.Wrestling, 75.1, 85.0);

            SetDamage(5, 7);

            Fame = 0;   // handled in OnDeath to apply the gravedigger penalty to participants
            Karma = 0;  // handled in OnDeath to apply the gravedigger penalty to participants
        }

        public override void InitBody()
        {
            SetStr(90, 100);
            SetDex(90, 100);
            SetInt(15, 25);

            // a manly job!
            Hue = 33770;
            Body = 400;
            Name = NameList.RandomName("male");
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            int tunicAndHat = Utility.RandomNeutralHue();
            AddItem(new Tunic(tunicAndHat));
            AddItem(new StrawHat(tunicAndHat));
            AddItem(new LongPants(Utility.RandomNeutralHue()));
            AddItem(new Boots(Utility.RandomNeutralHue()));
            AddItem(new ReceedingHair());

            PackItem(new Shovel());
            PackGold(5, 10);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            // Build the unique list of participants who have looting rights (with party fanout).
            List<DamageStore> list = GetLootingRights(this.DamageEntries, this.HitsMax);
            List<Mobile> participants = new List<Mobile>();

            for (int i = 0; i < list.Count; ++i)
            {
                DamageStore ds = list[i];

                if (!ds.m_HasRight || ds.m_Mobile == null)
                    continue;

                Party? party = Engines.PartySystem.Party.Get(ds.m_Mobile);

                if (party != null && party.Members != null && party.Members.Count > 0)
                {
                    for (int j = 0; j < party.Members.Count; ++j)
                    {
                        PartyMemberInfo? info = party.Members[j] as PartyMemberInfo;

                        if (info == null || info.Mobile == null)
                            continue;

                        if (participants.IndexOf(info.Mobile) == -1)
                            participants.Add(info.Mobile);
                    }
                }
                else
                {
                    if (participants.IndexOf(ds.m_Mobile) == -1)
                        participants.Add(ds.m_Mobile);
                }
            }

            // Apply the gravedigger penalty: drop each participant to the next lower fame tier.
            for (int i = 0; i < participants.Count; ++i)
            {
                Mobile m = participants[i];

                int oldFame = m.Fame;
                int newFame = Titles.GetNextLowerFameCap(oldFame);

                if (newFame < oldFame)
                    m.Fame = newFame;
            }
        }

        public Gravedigger(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}