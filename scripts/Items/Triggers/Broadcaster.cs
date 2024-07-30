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

/* Items/Triggers/Broadcaster.cs
 * CHANGELOG:
 *  6/5/2024, Adam
 *      SystemMessage: Old behavior: generated SysMessage unconditionally (did not factor in a targeted item, mobile, or spawner.)
 *      New behavior: if a targeted item, mobile, or spawner is specified, that thing must exist for the message to be generated.
 *  4/25/2024, Adam (Macros)
 *      We now support the following macros in the Broadcast Controller:
 *      {name} {title} {guild_short} {guild_long}
 *      These macros allow our NPCs to 'speak' more personalized messages.
 * 	3/8/23, Yoar
 * 		Rewrote entirely:
 * 		* Now only broadcasts up to 1 message.
 * 		* Removed party, range, region global messages (use TriggerTRansformer instead).
 * 		* Added private overhead messages.
 * 		* Added custom sound effect support.
 * 	2/11/22, Yoar
 * 		Initial version.
 */

using Server.Mobiles;
using Server.Network;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Server.Items.Triggers
{
    [NoSort]
    [TypeAlias("Server.Items.Broadcaster")]
    public class Broadcaster : Item, ITriggerable
    {
        public enum BcMessageType : byte
        {
            System,
            OverheadPublic,
            OverheadPrivate,
        }

        public override string DefaultName { get { return "Broadcaster"; } }

        private string m_Message;
        private int m_MessageHue;
        private bool m_MessageAscii;

        private BcMessageType m_MessageType;
        private IEntity m_Target;

        private int m_SoundID;

        private string m_SoundEffect;

        [CommandProperty(AccessLevel.GameMaster)]
        public string Message
        {
            get { return m_Message; }
            set
            {
                m_Message = value;
                SendSystemMessage("Supported macros:");
                SendSystemMessage("{name} {title} {guild_short} {guild_long}");
            }
        }

        [CommandProperty(AccessLevel.GameMaster), Hue]
        public int MessageHue
        {
            get { return m_MessageHue; }
            set { m_MessageHue = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool MessageAscii
        {
            get { return m_MessageAscii; }
            set { m_MessageAscii = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public BcMessageType MessageType
        {
            get { return m_MessageType; }
            set { m_MessageType = value; }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Item TargetItem
        {
            get { return m_Target as Item; }
            set { m_Target = value; }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile TargetMobile
        {
            get { return m_Target as Mobile; }
            set { m_Target = value; }
        }

        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.GameMaster)]
        public Spawner TargetSpawner
        {
            get { return m_Target as Spawner; }
            set { m_Target = value; }
        }

        [CopyableAttribute(CopyType.Copy)]  // CopyProperties will copy this one and not the DoNotCopy ones
        public IEntity Target { get { return m_Target; } set { m_Target = value; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SoundID
        {
            get { return m_SoundID; }
            set { m_SoundID = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string SoundEffect
        {
            get { return m_SoundEffect; }
            set { m_SoundEffect = value; }
        }

        [Constructable]
        public Broadcaster()
            : base(0x1B72)
        {
            Movable = false;
            Visible = false;

            m_SoundID = -1;
        }
        public override void OnDoubleClick(Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.Administrator)
            {
                m.SendGump(new Server.Gumps.PropertiesGump(m, this));
            }
        }
        public bool CanTrigger(Mobile from)
        {
            if (!TriggerSystem.CheckEvent(this))    // Make sure we are not event blocked
                return false;

            return true;
        }

        public void OnTrigger(Mobile from)
        {
            Broadcast(from, m_Message, m_MessageHue, m_SoundID);
        }
        private bool ValdateTarget(IEntity ent)
        {
            if (ent == null) return false;
            else if (ent is Mobile m && m.Deleted == false)
                return true;
            else if (ent is Spawner s && s.Deleted == false)
            {
                s.Defrag();
                return GetSpawnerEntities(ent).Count > 0;
            }
            else if (ent is Item i && i.Deleted == false)
                return true;

            return false;
        }
        public void Broadcast(Mobile from, string text, int hue, int soundID)
        {
            if (from != null && text != null)
                text = Utility.ExpandMacros(from, item: null, text);

            // 6/5/2024, Adam... I'm seeing this called for a woodpecker! lol.
            //  probably and and all creatures/NPCs trigger. We probably need to filter on Player or something.
            //  For the woodpecker, "You noticed the kegs with ale in the camp..."
            switch (m_MessageType)
            {
                case BcMessageType.System:
                    {

                        if (from == null)
                            break;

                        bool sendMessage = (m_Target != null && ValdateTarget(m_Target)) || (m_Target == null);

                        if (sendMessage)
                        {
                            if (text != null)
                                from.SendMessage(hue, text);

                            if (soundID != -1)
                                from.SendSound(soundID);

                            if (m_SoundEffect != null)
                                Engines.PlaySoundEffect.Play(from, m_SoundEffect);
                        }

                        break;
                    }
                case BcMessageType.OverheadPublic:
                    {
                        IEntity ent = m_Target;

                        if (ent == null)
                            ent = from;

                        if (ent is Spawner)
                        {
                            IEntity save = m_Target;
                            foreach (var sent in GetSpawnerEntities(ent))
                            {
                                m_Target = sent;
                                Broadcast(from, text, hue, soundID);
                            }
                            m_Target = save;
                            break;
                        }

                        if (ent == null)
                            break;

                        if (ent is Item)
                            ent = GetRootEntity((Item)ent);

                        if (text != null)
                            PublicOverheadMessage(ent, text, hue);

                        if (soundID != -1)
                            Effects.PlaySound(ent.Location, ent.Map, soundID);

                        if (m_SoundEffect != null && from != null)
                            Engines.PlaySoundEffect.Play(from, m_SoundEffect);

                        break;
                    }
                case BcMessageType.OverheadPrivate:
                    {
                        if (from == null)
                            break;

                        IEntity ent = m_Target;

                        if (ent == null)
                            ent = from;

                        if (ent is Spawner)
                        {
                            IEntity save = m_Target;
                            foreach (var sent in GetSpawnerEntities(ent))
                            {
                                m_Target = sent;
                                Broadcast(from, text, hue, soundID);
                            }
                            m_Target = save;
                            break;
                        }

                        if (ent == null)
                            break;

                        if (ent is Item)
                            ent = GetRootEntity((Item)ent);

                        if (text != null)
                            PrivateOverheadMessage(ent, text, hue, from.NetState);

                        if (soundID != -1 && from.NetState != null)
                            from.Send(new PlaySound(soundID, ent.Location));

                        if (m_SoundEffect != null && from != null)
                            Engines.PlaySoundEffect.Play(from, m_SoundEffect);

                        break;
                    }
            }
        }
        List<IEntity> GetSpawnerEntities(IEntity ent)
        {
            if (ent is Spawner spawner)
            {
                if (spawner.Objects != null)
                    return spawner.Objects.Cast<IEntity>().ToList();
            }
            return null;
        }
        private static IEntity GetRootEntity(Item item)
        {
            IEntity ent = item.RootParent as IEntity;

            return (ent == null ? item : ent);
        }

        private static void PublicOverheadMessage(IEntity ent, string text, int hue)
        {
            if (ent is Item)
                ((Item)ent).PublicOverheadMessage(Network.MessageType.Regular, hue == -1 ? 0x3B2 : hue, false, text);
            else if (ent is Mobile)
                ((Mobile)ent).PublicOverheadMessage(Network.MessageType.Regular, hue == -1 ? 0x3B2 : hue, false, text);
        }

        private static void PrivateOverheadMessage(IEntity ent, string text, int hue, NetState ns)
        {
            if (ent is Item)
                ((Item)ent).PrivateOverheadMessage(Network.MessageType.Regular, hue == -1 ? 0x3B2 : hue, false, text, ns);
            else if (ent is Mobile)
                ((Mobile)ent).PrivateOverheadMessage(Network.MessageType.Regular, hue == -1 ? 0x3B2 : hue, false, text, ns);
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (from.AccessLevel >= AccessLevel.GameMaster)
                list.Add(new TriggerSystem.ActivateCME(CanTrigger(from)));
        }

        public Broadcaster(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((string)m_SoundEffect);

            writer.Write((string)m_Message);
            writer.Write((int)m_MessageHue);
            writer.Write((bool)m_MessageAscii);

            writer.Write((byte)m_MessageType);

            if (m_Target is Item)
                writer.Write((Item)m_Target);
            else if (m_Target is Mobile)
                writer.Write((Mobile)m_Target);
            else
                writer.Write(Serial.MinusOne);

            writer.Write((int)m_SoundID);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_SoundEffect = reader.ReadString();

                        goto case 1;
                    }
                case 1:
                    {
                        m_Message = reader.ReadString();
                        m_MessageHue = reader.ReadInt();
                        m_MessageAscii = reader.ReadBool();

                        m_MessageType = (BcMessageType)reader.ReadByte();
                        m_Target = World.FindEntity(reader.ReadInt()) as IEntity;

                        m_SoundID = reader.ReadInt();

                        break;
                    }
                case 0:
                    {
                        #region Legacy

                        int count = reader.ReadInt();

                        for (int i = 0; i < count; i++)
                        {
                            if (i == 0)
                                m_Message = reader.ReadString();
                            else
                                reader.ReadString(); // Message
                        }

                        if (reader.ReadByte() == 5)
                        {
                            m_MessageType = BcMessageType.OverheadPublic;
                            m_Target = this;
                        }

                        reader.ReadInt(); // Range
                        m_MessageHue = reader.ReadInt();
                        m_SoundID = reader.ReadInt();
                        reader.ReadTimeSpan(); // Delay
                        reader.ReadTimeSpan(); // Interval
                        reader.ReadItem(); // Link

                        if (reader.ReadBool()) // Running
                        {
                            reader.ReadMobile(); // From
                            reader.ReadInt(); // Index
                        }

                        #endregion

                        break;
                    }
            }
        }

        #region Programmatic use
        public string SetMessage
        {
            set { m_Message = value; }
        }
        #endregion Programmatic use
    }
}