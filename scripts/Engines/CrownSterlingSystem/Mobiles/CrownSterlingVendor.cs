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

/* Scripts\Engines\CrownSterlingSystem\Mobiles\CrownSterlingVendor.cs
 * CHANGELOG:
 * 6/24/2024. Adam
 *   First time check in
 */

using Server.Engines.CrownSterlingSystem;
using Server.Mobiles;
using Server.Network;
using Server.Prompts;
using Server.Targeting;
using System;
using System.Collections.Generic;

namespace Server.Engines.CrownSterlingSystem
{
    public class CrownSterlingVendor : BaseTownsFolk
    {
        public static List<CrownSterlingVendor> Instances = new();
        public static bool Sells(CrownSterlingSystem.RewardSet set)
        {
            foreach (var vendor in Instances)
                if (vendor.RewardSet == set)
                    return true;

            return false;
        }

        [Constructable]
        public CrownSterlingVendor()
            : base("the royal vendor")
        {
            Instances.Add(this);
            IsInvulnerable = true;
        }
        CrownSterlingSystem.RewardSet m_RewardSet = CrownSterlingSystem.RewardSet.Test;
        [CommandProperty(AccessLevel.GameMaster)]
        public CrownSterlingSystem.RewardSet RewardSet
        {
            get { return m_RewardSet; }
            set
            {
                m_RewardSet = value;
                Title = '[' + Utility.SplitCamelCase(m_RewardSet.ToString()) + "]";
                Name = Name.Split(' ', StringSplitOptions.TrimEntries)[0] + " the royal vendor";
            }
        }
        public CrownSterlingVendor(Serial serial)
            : base(serial)
        {
            Instances.Add(this);
        }
        public override void Delete()
        {
            Instances.Remove(this);
            base.Delete();
        }
        public override bool HandlesOnSpeech(Mobile from)
        {
            return (from.InRange(this, 3));
        }
        public override void OnSpeech(SpeechEventArgs e)
        {
            base.OnSpeech(e);

            Mobile from = e.Mobile;

            if (from.InRange(this, 4) && !e.Handled)
            {
                if (this.Combatant != null)
                {
                    e.Handled = true;

                    // I am too busy fighting to deal with thee!
                    this.PublicOverheadMessage(MessageType.Regular, 0x3B2, 501482);
                }
                else if (e.HasKeyword(0x14D)) // *vendor sell*
                {
                    e.Handled = true;

                    Say(true, "You have nothing I would be interested in.");
                }
                else if (e.HasKeyword(0x3C)) // *vendor buy*
                {
                    e.Handled = true;

                    VendorBuy(from);
                    if (AIObject != null)
                        AIObject.m_Mobile.FocusMob = from;
                }
                else if (e.WasNamed(this) && e.HasKeyword(0x171)/*buy*/)
                {
                    e.Handled = true;

                    VendorBuy(from);
                    if (AIObject != null)
                        AIObject.m_Mobile.FocusMob = from;
                }
                else if ((e.WasNamed(this) || e.HasKeyword("vendor")) && e.HasKeyword("add item") && from.AccessLevel >= AccessLevel.Seer)
                {
                    e.Handled = true;

                    VendorAddItem(from);
                    if (AIObject != null)
                        AIObject.m_Mobile.FocusMob = from;
                }
                else if ((e.WasNamed(this) || e.HasKeyword("vendor")) && e.HasKeyword("remove item") && from.AccessLevel >= AccessLevel.Seer)
                {
                    e.Handled = true;

                    VendorRemoveItem(from);
                    if (AIObject != null)
                        AIObject.m_Mobile.FocusMob = from;
                }
                else if ((e.WasNamed(this) || e.HasKeyword("vendor")) && e.HasKeyword("add target") && from.AccessLevel >= AccessLevel.Seer)
                {
                    e.Handled = true;

                    VendorTargetItem(from);
                    if (AIObject != null)
                        AIObject.m_Mobile.FocusMob = from;
                }
                else if ((e.WasNamed(this) || e.HasKeyword("vendor")) && e.HasKeyword("clear remove") && from.AccessLevel >= AccessLevel.Seer)
                {
                    e.Handled = true;

                    foreach (var r in RemoveFromRewardsDatabase)
                        if (r.ItemO is Item item)
                            item.Delete();

                    RemoveFromRewardsDatabase.Clear();
                    from.SendMessage("Ok.");
                    if (AIObject != null)
                        AIObject.m_Mobile.FocusMob = from;
                }
                else if ((e.WasNamed(this) || e.HasKeyword("vendor")) && e.HasKeyword("clear add") && from.AccessLevel >= AccessLevel.Seer)
                {
                    e.Handled = true;

                    foreach (var r in AddToRewardsDatabase)
                        if (r.ItemO is Item item)
                            item.Delete();

                    AddToRewardsDatabase.Clear();
                    from.SendMessage("Ok.");
                    if (AIObject != null)
                        AIObject.m_Mobile.FocusMob = from;
                }
                else if ((e.WasNamed(this) || e.HasKeyword("vendor")) && e.HasKeyword("help") && from.AccessLevel >= AccessLevel.GameMaster)
                {
                    e.Handled = true;
                    from.SendMessage("vendor buy (everyone).");
                    from.SendMessage("vendor add item (seer).");
                    from.SendMessage("vendor remove item (seer).");
                    from.SendMessage("vendor add target (seer).");
                    from.SendMessage("vendor clear remove (seer). Clears the Remove list");
                    from.SendMessage("vendor clear add (seer). Clears the Add list");
                    if (AIObject != null)
                        AIObject.m_Mobile.FocusMob = from;
                }
            }
        }
        private void VendorBuy(Mobile from)
        {
            CrownSterlingSystem system;
            if ((system = CrownSterlingSystem.Factory(this, m_RewardSet)) == null)
            {
                SendSystemMessage($"Cannot load RewardSet '{m_RewardSet}'");
                return;
            }
            SayTo(from, 500186); // Greetings.  Have a look around.
            from.CloseGump(typeof(CrownSterlingRewardsGump));
            from.SendGump(new CrownSterlingRewardsGump(system, from, new object[] { AddToRewardsDatabase, RemoveFromRewardsDatabase }));
        }
        List<CrownSterlingReward> AddToRewardsDatabase = new();
        List<CrownSterlingReward> RemoveFromRewardsDatabase = new();
        private void VendorAddItem(Mobile from)
        {
            from.SendMessage("Type an ItemID, base price, hue, and a label (Optional)");
            from.Prompt = new VendorAddPrompt(this, AddToRewardsDatabase, RemoveFromRewardsDatabase);
        }
        private void VendorRemoveItem(Mobile from)
        {
            from.SendMessage("Type an ItemID, base price, hue, and a label (Optional)");
            from.Prompt = new VendorRemovePrompt(this, AddToRewardsDatabase, RemoveFromRewardsDatabase);
        }
        private void VendorTargetItem(Mobile from)
        {
            from.SendMessage("Target the item you wish to sell...");
            from.Target = new ItemTarget(this, AddToRewardsDatabase, RemoveFromRewardsDatabase);
        }
        public class ItemTarget : Target
        {
            private CrownSterlingVendor m_Vendor;
            private List<CrownSterlingReward> m_AddToRewardsDatabase;
            private List<CrownSterlingReward> m_RemoveFromRewardsDatabase;
            public ItemTarget(CrownSterlingVendor vendor, List<CrownSterlingReward> addDb, List<CrownSterlingReward> removeDb)
                : base(17, true, TargetFlags.None)
            {
                m_Vendor = vendor;
                m_AddToRewardsDatabase = addDb;
                m_RemoveFromRewardsDatabase = removeDb;
            }

            protected override void OnTarget(Mobile from, object target)
            {
                Item duped = null;
                if (target is Item item)
                {
                    duped = Utility.DeepDupe(item);
                    if (duped != null)
                    {
                        from.SendMessage("Type a base price, hue, and a label (Optional)");
                        from.Prompt = new VendorAddTargetPrompt(m_Vendor, duped, m_AddToRewardsDatabase, m_RemoveFromRewardsDatabase);
                    }
                }
                else
                {
                    from.SendMessage("That is not an item.");
                    return;
                }
            }
        }
        private void CleanOutLists()
        {
            foreach (var r in AddToRewardsDatabase)
                if (r.ItemO is Item item)
                    item.Delete();

            foreach (var r in RemoveFromRewardsDatabase)
                if (r.ItemO is Item item)
                    item.Delete();
        }
        public override void OnDelete()
        {
            CleanOutLists();
            base.OnDelete();
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write((int)m_RewardSet);

            writer.Write(AddToRewardsDatabase.Count);
            foreach (var r in AddToRewardsDatabase)
                r.Serialize(writer);

            writer.Write(RemoveFromRewardsDatabase.Count);
            foreach (var r in RemoveFromRewardsDatabase)
                r.Serialize(writer);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_RewardSet = (CrownSterlingSystem.RewardSet)reader.ReadInt();
                        int count = reader.ReadInt();
                        for (int ix = 0; ix < count; ix++)
                            AddToRewardsDatabase.Add(new CrownSterlingReward(reader));

                        count = reader.ReadInt();
                        for (int ix = 0; ix < count; ix++)
                            RemoveFromRewardsDatabase.Add(new CrownSterlingReward(reader));
                        break;
                    }
            }
        }
    }
}
namespace Server.Prompts
{
    public class VendorAddPrompt : Prompt
    {
        private CrownSterlingVendor m_Vendor;
        private List<CrownSterlingReward> m_AddToRewardsDatabase;
        private List<CrownSterlingReward> m_RemoveFromRewardsDatabase;
        public VendorAddPrompt(CrownSterlingVendor vendor, List<CrownSterlingReward> addDb, List<CrownSterlingReward> removeDb)
        {
            m_Vendor = vendor;
            m_AddToRewardsDatabase = addDb;
            m_RemoveFromRewardsDatabase = removeDb;
        }

        private void SetInfo(Mobile from, int itemId, int basePrice, int hue, string label)
        {
            CrownSterlingReward reward = new CrownSterlingReward(itemId, basePrice, hue, label);

            if (m_RemoveFromRewardsDatabase.Contains(reward))
                m_RemoveFromRewardsDatabase.Remove(reward);

            // remove actual items by matching ItemIDs
            // currently the remove database cannot contain actual items.
            for (int i = m_RemoveFromRewardsDatabase.Count - 1; i > -1; i--)
                if (m_RemoveFromRewardsDatabase[i].ItemO is Item item2 && item2.ItemID == itemId)
                {
                    (m_RemoveFromRewardsDatabase[i].ItemO as Item).Delete();
                    m_RemoveFromRewardsDatabase.RemoveAt(i);
                }

            if (!m_AddToRewardsDatabase.Contains(reward))
            {
                m_AddToRewardsDatabase.Add(reward);
                from.SendSystemMessage($"Item added: ItemID:{itemId} BasePrice:{basePrice} Hue:{hue} Label:{reward.Label}");
            }
            else
                from.SendSystemMessage($"Item database already contains ItemID:{itemId} BasePrice:{basePrice} Hue:{hue} Label:{reward.Label}");
        }
        public override void OnResponse(Mobile from, string text)
        {
            int itemID = 0;
            int basePrice = 0;
            int hue = 0;
            string label = null;
            try
            {
                string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length < 3)
                    throw new ApplicationException();

                if (Utility.StringToInt(tokens[0], ref itemID) == false)
                    throw new ApplicationException();

                if (Utility.StringToInt(tokens[1], ref basePrice) == false)
                    throw new ApplicationException();

                if (Utility.StringToInt(tokens[2], ref hue) == false)
                    throw new ApplicationException();

                if (tokens.Length > 3)
                {
                    for (int i = 3; i < tokens.Length; i++)
                        label += tokens[i] + " ";
                    label = label.Trim();
                }

                //if (string.IsNullOrEmpty(label))
                //{
                //    ItemData id = TileData.ItemTable[itemID & 0x3FFF];
                //    label = id.Name;
                //}
            }
            catch
            {
                from.SendSystemMessage("Usage: <itemid> <base price> <hue> [label]");
                return;
            }

            SetInfo(from, itemID, basePrice, hue, label);
        }

        public override void OnCancel(Mobile from)
        {

        }
    }
    public class VendorRemovePrompt : Prompt
    {
        private CrownSterlingVendor m_Vendor;
        private List<CrownSterlingReward> m_AddToRewardsDatabase;
        private List<CrownSterlingReward> m_RemoveFromRewardsDatabase;
        public VendorRemovePrompt(CrownSterlingVendor vendor, List<CrownSterlingReward> addDb, List<CrownSterlingReward> removeDb)
        {
            m_Vendor = vendor;
            m_AddToRewardsDatabase = addDb;
            m_RemoveFromRewardsDatabase = removeDb;
        }

        private void SetInfo(Mobile from, int itemId, int basePrice, int hue, string label)
        {
            CrownSterlingReward reward = new CrownSterlingReward(itemId, basePrice, hue, label);

            if (m_AddToRewardsDatabase.Contains(reward))
                m_AddToRewardsDatabase.Remove(reward);

            // remove actual items by matching ItemIDs
            for (int i = m_AddToRewardsDatabase.Count - 1; i > -1; i--)
                if (m_AddToRewardsDatabase[i].ItemO is Item item && item.ItemID == itemId
                    && m_AddToRewardsDatabase[i].Hue == hue
                    && item.OldSchoolName().Equals(m_AddToRewardsDatabase[i].Label, StringComparison.OrdinalIgnoreCase))
                {
                    (m_AddToRewardsDatabase[i].ItemO as Item).Delete();
                    m_AddToRewardsDatabase.RemoveAt(i);
                }

            if (!m_RemoveFromRewardsDatabase.Contains(reward))
            {
                m_RemoveFromRewardsDatabase.Add(reward);
                from.SendSystemMessage($"Item removed: ItemID:{itemId} BasePrice:{basePrice} Hue:{hue} Label:{reward.Label}");
            }
            else
                from.SendSystemMessage($"Item remove database already contains ItemID:{itemId} BasePrice:{basePrice} Hue:{hue} Label:{reward.Label}");
        }
        public override void OnResponse(Mobile from, string text)
        {
            int itemID = 0;
            int basePrice = 0;
            int hue = 0;
            string label = null;
            try
            {
                string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length < 3)
                    throw new ApplicationException();

                if (Utility.StringToInt(tokens[0], ref itemID) == false)
                    throw new ApplicationException();

                if (Utility.StringToInt(tokens[1], ref basePrice) == false)
                    throw new ApplicationException();

                if (Utility.StringToInt(tokens[2], ref hue) == false)
                    throw new ApplicationException();

                if (tokens.Length > 3)
                {
                    for (int i = 3; i < tokens.Length; i++)
                        label += tokens[i] + " ";
                    label = label.Trim();
                }

                //if (string.IsNullOrEmpty(label))
                //{
                //    ItemData id = TileData.ItemTable[itemID & 0x3FFF];
                //    label = id.Name;
                //}
            }
            catch
            {
                from.SendSystemMessage("Usage: <itemid> <base price> <hue> [label]");
                return;
            }

            SetInfo(from, itemID, basePrice, hue, label);
        }

        public override void OnCancel(Mobile from)
        {

        }
    }
    public class VendorAddTargetPrompt : Prompt
    {
        private CrownSterlingVendor m_Vendor;
        private Item m_Item;
        private List<CrownSterlingReward> m_AddToRewardsDatabase;
        private List<CrownSterlingReward> m_RemoveFromRewardsDatabase;
        public VendorAddTargetPrompt(CrownSterlingVendor vendor, Item item, List<CrownSterlingReward> addDb, List<CrownSterlingReward> removeDb)
        {
            m_Vendor = vendor;
            m_Item = item;
            m_AddToRewardsDatabase = addDb;
            m_RemoveFromRewardsDatabase = removeDb;
        }

        private void SetInfo(Mobile from, Item item, int basePrice, int hue, string label)
        {   // sets DeleteOnRestart, we will explicitly clear this
            CrownSterlingReward reward = new CrownSterlingReward(item, basePrice, hue, label);

            if (m_RemoveFromRewardsDatabase.Contains(reward))
                m_RemoveFromRewardsDatabase.Remove(reward);

            // remove actual items by matching ItemIDs
            // currently the remove database cannot contain actual items.
            for (int i = m_RemoveFromRewardsDatabase.Count - 1; i > -1; i--)
                if (m_RemoveFromRewardsDatabase[i].ItemO is Item item2 && item2.ItemID == item.ItemID)
                {
                    (m_RemoveFromRewardsDatabase[i].ItemO as Item).Delete();
                    m_RemoveFromRewardsDatabase.RemoveAt(i);
                }

            if (!m_AddToRewardsDatabase.Contains(reward))
            {
                m_AddToRewardsDatabase.Add(reward);
                from.SendSystemMessage($"Item added: ItemID:{item} BasePrice:{basePrice} Hue:{hue} Label:{reward.Label}");
                item.MoveToIntStorage();
                // by default, this item will be deleted on server restart because these inventory items are usually created on server up
                //  but when vendors have a customized list, the vendor will clear this DeleteOnRestart flag since we will serialize
                item.SetItemBool(Item.ItemBoolTable.DeleteOnRestart, false);
            }
            else
            {
                from.SendSystemMessage($"Item database already contains ItemID:{item} BasePrice:{basePrice} Hue:{hue} Label:{reward.Label}");
                item.Delete();
            }
        }
        public override void OnResponse(Mobile from, string text)
        {
            int itemID = 0;
            int basePrice = 0;
            int hue = 0;
            string label = null;
            try
            {
                string[] tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (tokens.Length < 2)
                    throw new ApplicationException();

                if (Utility.StringToInt(tokens[0], ref basePrice) == false)
                    throw new ApplicationException();

                if (Utility.StringToInt(tokens[1], ref hue) == false)
                    throw new ApplicationException();

                if (tokens.Length > 2)
                {
                    for (int i = 2; i < tokens.Length; i++)
                        label += tokens[i] + " ";
                    label = label.Trim();
                }
            }
            catch
            {
                from.SendSystemMessage("Usage: <base price> <hue> [label]");
                return;
            }

            SetInfo(from, m_Item, basePrice, hue, label);
        }

        public override void OnCancel(Mobile from)
        {

        }
    }
}