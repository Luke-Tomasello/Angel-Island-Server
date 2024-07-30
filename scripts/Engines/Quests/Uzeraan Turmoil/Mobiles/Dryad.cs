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

using Server.Items;
using Server.Mobiles;
using System.Collections;

namespace Server.Engines.Quests.Haven
{
    public class Dryad : BaseQuester
    {
        public override bool IsActiveVendor { get { return true; } }
        public override bool DisallowAllMoves { get { return false; } }
        public override bool ClickTitle { get { return true; } }
        public override bool CanTeach { get { return Core.RuleSets.StandardShardRules() ? false : true; } }

        [Constructable]
        public Dryad()
            : base("the Dryad")
        {
            SetSkill(SkillName.Peacemaking, 80.0, 100.0);
            SetSkill(SkillName.Cooking, 80.0, 100.0);
            SetSkill(SkillName.Provocation, 80.0, 100.0);
            SetSkill(SkillName.Musicianship, 80.0, 100.0);
            SetSkill(SkillName.Poisoning, 80.0, 100.0);
            SetSkill(SkillName.Archery, 80.0, 100.0);
            SetSkill(SkillName.Tailoring, 80.0, 100.0);
        }

        public Dryad(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = 0x85A7;

            Female = true;
            Body = 0x191;
            Name = "Anwin Brenna";
        }

        public override void InitOutfit()
        {
            AddItem(new Kilt(0x301));
            AddItem(new FancyShirt(0x300));

            AddItem(new PonyTail(0x22));

            Bow bow = new Bow();
            bow.Movable = false;
            AddItem(bow);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBDryad());
        }

        public override int GetAutoTalkRange(PlayerMobile pm)
        {
            return 4;
        }

        public override bool CanTalkTo(PlayerMobile to)
        {
            UzeraanTurmoilQuest qs = to.Quest as UzeraanTurmoilQuest;

            return (qs != null && qs.FindObjective(typeof(FindDryadObjective)) != null);
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
            QuestSystem qs = player.Quest;

            if (qs is UzeraanTurmoilQuest)
            {
                if (UzeraanTurmoilQuest.HasLostFertileDirt(player))
                {
                    FocusTo(player);
                    qs.AddConversation(new LostFertileDirtConversation(false));
                }
                else
                {
                    QuestObjective obj = qs.FindObjective(typeof(FindDryadObjective));

                    if (obj != null && !obj.Completed)
                    {
                        FocusTo(player);

                        Item fertileDirt = new QuestFertileDirt();

                        if (!player.PlaceInBackpack(fertileDirt))
                        {
                            fertileDirt.Delete();
                            player.SendLocalizedMessage(1046260); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                        }
                        else
                        {
                            obj.Complete();
                        }
                    }
                    else if (contextMenu)
                    {
                        FocusTo(player);
                        SayTo(player, 1049357); // I have nothing more for you at this time.
                    }
                }
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            PlayerMobile player = from as PlayerMobile;

            if (player != null)
            {
                UzeraanTurmoilQuest qs = player.Quest as UzeraanTurmoilQuest;

                if (qs != null && dropped is Apple && UzeraanTurmoilQuest.HasLostFertileDirt(from))
                {
                    FocusTo(from);

                    Item fertileDirt = new QuestFertileDirt();

                    if (!player.PlaceInBackpack(fertileDirt))
                    {
                        fertileDirt.Delete();
                        player.SendLocalizedMessage(1046260); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                        return false;
                    }
                    else
                    {
                        dropped.Consume();
                        qs.AddConversation(new DryadAppleConversation());
                        return dropped.Deleted;
                    }
                }
            }

            return base.OnDragDrop(from, dropped);
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

    public class SBDryad : SBInfo
    {
        private ArrayList m_BuyInfo = new InternalBuyInfo();
        private IShopSellInfo m_SellInfo = new InternalSellInfo();

        public SBDryad()
        {
        }

        public override IShopSellInfo SellInfo { get { return m_SellInfo; } }
        public override ArrayList BuyInfo { get { return m_BuyInfo; } }

        public class InternalBuyInfo : ArrayList
        {
            public InternalBuyInfo()
            {
                Add(new GenericBuyInfo(typeof(Bandage), 5, 20, 0xE21, 0));
                Add(new GenericBuyInfo(typeof(Ginseng), 3, 20, 0xF85, 0));
                Add(new GenericBuyInfo(typeof(Garlic), 3, 20, 0xF84, 0));
                Add(new GenericBuyInfo(typeof(Bloodmoss), 5, 20, 0xF7B, 0));
                Add(new GenericBuyInfo(typeof(Nightshade), 3, 20, 0xF88, 0));
                Add(new GenericBuyInfo(typeof(SpidersSilk), 3, 20, 0xF8D, 0));
                Add(new GenericBuyInfo(typeof(MandrakeRoot), 3, 20, 0xF86, 0));
            }
        }

        public class InternalSellInfo : GenericSellInfo
        {
            public InternalSellInfo()
            {
                Add(typeof(Bandage), BaseVendor.PlayerPays(typeof(Bandage)));
                Add(typeof(Garlic), BaseVendor.PlayerPays(typeof(Garlic)));
                Add(typeof(Ginseng), BaseVendor.PlayerPays(typeof(Ginseng)));
                Add(typeof(Bloodmoss), BaseVendor.PlayerPays(typeof(Bloodmoss)));
                Add(typeof(Nightshade), BaseVendor.PlayerPays(typeof(Nightshade)));
                Add(typeof(SpidersSilk), BaseVendor.PlayerPays(typeof(SpidersSilk)));
                Add(typeof(MandrakeRoot), BaseVendor.PlayerPays(typeof(MandrakeRoot)));
            }
        }
    }
}