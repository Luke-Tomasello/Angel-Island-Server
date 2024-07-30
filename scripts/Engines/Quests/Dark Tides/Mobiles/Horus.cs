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

using Server.ContextMenus;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using System.Collections;

namespace Server.Engines.Quests.Necro
{
    public class Horus : BaseQuester
    {
        [Constructable]
        public Horus()
            : base("the Guardian")
        {
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = 0x83F3;
            Female = false;
            Body = 0x190;

            Name = "Horus";
        }

        public override void InitOutfit()
        {
            AddItem(SetHue(new PlateLegs(), 0x849));
            AddItem(SetHue(new PlateChest(), 0x849));
            AddItem(SetHue(new PlateArms(), 0x849));
            AddItem(SetHue(new PlateGloves(), 0x849));
            AddItem(SetHue(new PlateGorget(), 0x849));

            AddItem(SetHue(new Bardiche(), 0x482));

            AddItem(SetHue(new Boots(), 0x001));
            AddItem(SetHue(new Cloak(), 0x482));

            AddItem(Server.Items.Hair.CreateByID(Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2047, 0x2049, 0x204A), 0x45D));
            AddItem(Server.Items.Beard.CreateByID(Utility.RandomList(0x203E, 0x203F, 0x2040, 0x2041, 0x204B, 0x204C, 0x204D), 0x45D));
        }

        public override int GetAutoTalkRange(PlayerMobile m)
        {
            return 3;
        }

        public override bool CanTalkTo(PlayerMobile to)
        {
            QuestSystem qs = to.Quest;

            return (qs is DarkTidesQuest && qs.IsObjectiveInProgress(typeof(FindCrystalCaveObjective)));
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
            QuestSystem qs = player.Quest;

            if (qs is DarkTidesQuest)
            {
                QuestObjective obj = qs.FindObjective(typeof(FindCrystalCaveObjective));

                if (obj != null && !obj.Completed)
                    obj.Complete();
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (InRange(m.Location, 2) && !InRange(oldLocation, 2) && m is PlayerMobile)
            {
                PlayerMobile pm = (PlayerMobile)m;
                QuestSystem qs = pm.Quest;

                if (qs is DarkTidesQuest)
                {
                    QuestObjective obj = qs.FindObjective(typeof(ReturnToCrystalCaveObjective));

                    if (obj != null && !obj.Completed)
                        obj.Complete();
                    else
                    {
                        obj = qs.FindObjective(typeof(FindHorusAboutRewardObjective));

                        if (obj != null && !obj.Completed)
                        {
                            Container cont = GetNewContainer();

                            cont.DropItem(new Gold(500));

                            BaseJewel jewel = new GoldBracelet();
                            if (Core.RuleSets.AOSRules())
                                BaseRunicTool.ApplyAttributesTo(jewel, 3, 20, 40);
                            cont.DropItem(jewel);

                            if (!pm.PlaceInBackpack(cont))
                            {
                                cont.Delete();
                                pm.SendLocalizedMessage(1046260); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                            }
                            else
                            {
                                obj.Complete();
                            }
                        }
                    }
                }
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.Alive)
            {
                PlayerMobile pm = from as PlayerMobile;

                if (pm != null)
                {
                    QuestSystem qs = pm.Quest;

                    if (qs is DarkTidesQuest)
                    {
                        QuestObjective obj = qs.FindObjective(typeof(SpeakCavePasswordObjective));
                        bool enabled = (obj != null && !obj.Completed);

                        list.Add(new SpeakPasswordEntry(this, pm, enabled));
                    }
                }
            }
        }

        public virtual void OnPasswordSpoken(PlayerMobile from)
        {
            QuestSystem qs = from.Quest;

            if (qs is DarkTidesQuest)
            {
                QuestObjective obj = qs.FindObjective(typeof(SpeakCavePasswordObjective));

                if (obj != null && !obj.Completed)
                {
                    obj.Complete();
                    return;
                }
            }

            from.SendLocalizedMessage(1060185); // Horus ignores you.
        }

        private class SpeakPasswordEntry : ContextMenuEntry
        {
            private Horus m_Horus;
            private PlayerMobile m_From;

            public SpeakPasswordEntry(Horus horus, PlayerMobile from, bool enabled)
                : base(6193, 3)
            {
                m_Horus = horus;
                m_From = from;

                if (!enabled)
                    Flags |= CMEFlags.Disabled;
            }

            public override void OnClick()
            {
                if (m_From.Alive)
                    m_Horus.OnPasswordSpoken(m_From);
            }
        }

        public Horus(Serial serial)
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