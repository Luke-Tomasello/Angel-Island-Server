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

/* Scripts/Engines/Township/TownshipNPCDeed.cs
 * CHANGELOG
 *  2/19/22, Yoar
 *      Rewrote 'CanPlace' logic.
 *      Added 'NeedsHouse' abstract getter; if false, the NPC must be placed *outside* a house.
 *  2/18/22, Adam
 *      Fix the TSAnimalTrainer Vendor { get { return new TSAnimalTrainer(); } } to return a TSAnimalTrainer and not a banker ;)
 *  1/12/22, Yoar
 *      Township cleanups.
 *	10/19/08, Pix
 *		Added additional checks and messages to TSEvocatorDeed and TSEmissaryDeed
 *	8/3/08, Pix
 *		Change for CanExtend() call - now returns a reason.
 *	5/11/08, Adam
 *		Performance Conversion: Regions now use HashTables instead of ArrayLists
 *	4/13/07 Pix
 *		Now correctly sets lookouts to non-walking.
 */

using Server.Guilds;
using Server.Mobiles;
using System;

namespace Server.Items
{
    public abstract class TownshipNPCDeed : Item
    {
        public override string DefaultName { get { return "a township contract of employment"; } }

        public abstract Type NPCType { get; }

        private Guild m_Guild;
        private Serial m_RestorationMobile = Serial.Zero;    // used when redeeding a township vendor

        [CommandProperty(AccessLevel.GameMaster)]
        public Guild Guild
        {
            get { return m_Guild; }
            set { m_Guild = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Serial RestorationMobile
        {
            get { return m_RestorationMobile; }
            set { m_RestorationMobile = value; }
        }

        public TownshipNPCDeed()
            : base(0x14F0)
        {
            Hue = Township.TownshipSettings.Hue;
            LootType = LootType.Blessed;
        }

        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Guild != null)
                LabelTo(from, string.Format("{0} [{1}]", TownshipNPCHelper.GetNPCName(NPCType), m_Guild.Abbreviation));
            else
                LabelTo(from, TownshipNPCHelper.GetNPCName(NPCType));
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            else if (m_Guild != null && !m_Guild.IsMember(from))
                from.SendMessage("Only guild members can place this vendor.");
            else if (TownshipNPCHelper.CheckPlaceNPC(from, NPCType, this.RestorationMobile))
                Delete();
        }

        public override void OnDelete()
        {
            if (m_RestorationMobile != Serial.Zero)
            {
                Mobile m = World.FindMobile(m_RestorationMobile);
                if (m != null && m.IsIntMapStorage)
                    m.Delete();
            }

            base.OnDelete();
        }

        public TownshipNPCDeed(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 4;
            writer.Write(version); // version

            // version 4
            writer.Write(m_RestorationMobile);

            // version 3
            writer.Write((BaseGuild)m_Guild);

        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 4:
                    {
                        m_RestorationMobile = (Serial)reader.ReadInt();
                        goto case 3;
                    }
                case 3:
                    {
                        m_Guild = reader.ReadGuild() as Guild;

                        break;
                    }
                case 2:
                case 1:
                case 0:
                    {
                        if (version < 1)
                            reader.ReadString(); // guild abbreviation

                        if (version < 2)
                            m_Guild = reader.ReadGuild() as Guild;

                        break;
                    }
            }

            if (version < 2)
                Name = null;
        }
    }

    public class TSBankerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSBanker); } }

        [Constructable]
        public TSBankerDeed()
            : base()
        {
        }

        #region Serialization

        public TSBankerDeed(Serial serial)
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

        #endregion
    }

    public class TSAnimalTrainerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSAnimalTrainer); } }

        [Constructable]
        public TSAnimalTrainerDeed()
            : base()
        {
        }

        #region Serialization

        public TSAnimalTrainerDeed(Serial serial)
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

        #endregion
    }
    public class TSStableMasterDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSStableMaster); } }

        [Constructable]
        public TSStableMasterDeed()
            : base()
        {
        }

        #region Serialization

        public TSStableMasterDeed(Serial serial)
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

        #endregion
    }

    public class TSMageDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSMage); } }

        [Constructable]
        public TSMageDeed()
            : base()
        {
        }

        #region Serialization

        public TSMageDeed(Serial serial)
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

        #endregion
    }

    public class TSAlchemistDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSAlchemist); } }

        [Constructable]
        public TSAlchemistDeed()
            : base()
        {
        }

        #region Serialization

        public TSAlchemistDeed(Serial serial)
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

        #endregion
    }

    public class TSProvisionerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSProvisioner); } }

        [Constructable]
        public TSProvisionerDeed()
            : base()
        {
        }

        #region Serialization

        public TSProvisionerDeed(Serial serial)
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

        #endregion
    }

    public class TSArmsTrainerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSArmsTrainer); } }

        [Constructable]
        public TSArmsTrainerDeed()
            : base()
        {
        }

        #region Serialization

        public TSArmsTrainerDeed(Serial serial)
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

        #endregion
    }

    public class TSMageTrainerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSMageTrainer); } }

        [Constructable]
        public TSMageTrainerDeed()
            : base()
        {
        }

        #region Serialization

        public TSMageTrainerDeed(Serial serial)
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

        #endregion
    }

    public class TSRogueTrainerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSRogueTrainer); } }

        [Constructable]
        public TSRogueTrainerDeed()
            : base()
        {
        }

        #region Serialization

        public TSRogueTrainerDeed(Serial serial)
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

        #endregion
    }

    public class TSEmissaryDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSEmissary); } }

        [Constructable]
        public TSEmissaryDeed()
            : base()
        {
        }

        #region Serialization

        public TSEmissaryDeed(Serial serial)
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

        #endregion
    }

    public class TSEvocatorDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSEvocator); } }

        [Constructable]
        public TSEvocatorDeed()
            : base()
        {
        }

        #region Serialization

        public TSEvocatorDeed(Serial serial)
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

        #endregion
    }

    public class TSInnkeeperDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSInnKeeper); } }

        [Constructable]
        public TSInnkeeperDeed()
            : base()
        {
        }

        #region Serialization

        public TSInnkeeperDeed(Serial serial)
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

        #endregion
    }

    public class TSTownCrierDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSTownCrier); } }

        [Constructable]
        public TSTownCrierDeed()
            : base()
        {
        }

        #region Serialization

        public TSTownCrierDeed(Serial serial)
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

        #endregion
    }

    public class TSLookoutDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSLookout); } }

        [Constructable]
        public TSLookoutDeed()
            : base()
        {
        }

        #region Serialization

        public TSLookoutDeed(Serial serial)
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

        #endregion
    }

    [TypeAlias("Server.Items.TSFightbrokerDeed")]
    public class TSFightBrokerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSFightBroker); } }

        [Constructable]
        public TSFightBrokerDeed()
            : base()
        {
        }

        #region Serialization

        public TSFightBrokerDeed(Serial serial)
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

        #endregion
    }

    public class TSMinstrelDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSMinstrel); } }

        [Constructable]
        public TSMinstrelDeed()
            : base()
        {
        }

        #region Serialization

        public TSMinstrelDeed(Serial serial)
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

        #endregion
    }

    public class TSNecromancerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSNecromancer); } }

        [Constructable]
        public TSNecromancerDeed()
            : base()
        {
        }

        #region Serialization

        public TSNecromancerDeed(Serial serial)
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

        #endregion
    }

    public class TSRancherDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSRancher); } }

        [Constructable]
        public TSRancherDeed()
            : base()
        {
        }

        #region Serialization

        public TSRancherDeed(Serial serial)
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

        #endregion
    }

    public class TSFarmerDeed : TownshipNPCDeed
    {
        public override Type NPCType { get { return typeof(TSFarmer); } }

        [Constructable]
        public TSFarmerDeed()
            : base()
        {
        }

        #region Serialization

        public TSFarmerDeed(Serial serial)
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

        #endregion
    }
}