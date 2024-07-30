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

/* Items/Deeds/CertificateOfIdentity.cs
 * ChangeLog:
 *	12/22/08, Adam
 *		Created
 */

using Server.Mobiles;
using Server.Targeting;
using System;

namespace Server.Items
{
    public class TicketTarget : Target // Create our targeting class (which we derive from the base target class)
    {
        private CertificateOfIdentity m_Cert;

        public TicketTarget(CertificateOfIdentity cert)
            : base(7, false, TargetFlags.None)
        {
            m_Cert = cert;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is Item)
            {
                Item item = (Item)target;
                if (item.PlayerCrafted == false)
                    from.SendMessage("This item is not player crafted.");
                else if (item is BaseClothing || item is BaseArmor || item is BaseWeapon)
                {
                    Mobile Crafter = null;
                    if ((item is BaseClothing) && (item as BaseClothing).Crafter != null)
                        Crafter = (item as BaseClothing).Crafter.Mobile;
                    if ((item is BaseArmor) && (item as BaseArmor).Crafter != null)
                        Crafter = (item as BaseArmor).Crafter.Mobile;
                    if ((item is BaseWeapon) && (item as BaseWeapon).Crafter != null)
                        Crafter = (item as BaseWeapon).Crafter.Mobile;

                    if (Crafter == null)
                        from.SendMessage("This item does not carry a Makers Mark.");
                    else if (Crafter.Deleted)
                        from.SendMessage("It is a sad day, but this player no longer exists in this world.");
                    else
                    {
                        // create the identity
                        m_Cert.Used = true;
                        m_Cert.Mobile = Crafter;
                        m_Cert.Name = String.Format("a certificate of identity for {0}", Crafter.Name);
                        from.SendMessage("You have created {0}", m_Cert.Name);
                    }
                }

            }
            else if (target is PlayerMobile)
            {
                PlayerMobile pm = target as PlayerMobile;
                // create the identity
                m_Cert.Used = true;
                m_Cert.Mobile = pm;
                m_Cert.Name = String.Format("a certificate of identity for {0}", pm.Name);
                from.SendMessage("You have created {0}", m_Cert.Name);
            }
            else
            {
                from.SendMessage("You may create an identity from players and items.");
            }
        }
    }

    public class ValidateIdentityTarget : Target // Create our targeting class (which we derive from the base target class)
    {
        private CertificateOfIdentity m_Cert;

        public ValidateIdentityTarget(CertificateOfIdentity cert)
            : base(7, false, TargetFlags.None)
        {
            m_Cert = cert;
        }

        protected override void OnTarget(Mobile from, object target) // Override the protected OnTarget() for our feature
        {
            if (target is Item)
            {
                Item item = (Item)target;
                if (item.PlayerCrafted == false)
                    from.SendMessage("This item is not player crafted.");
                else if (item is BaseClothing || item is BaseArmor || item is BaseWeapon)
                {
                    Mobile Crafter = null;
                    if ((item is BaseClothing) && (item as BaseClothing).Crafter != null)
                        Crafter = (item as BaseClothing).Crafter.Mobile;
                    if ((item is BaseArmor) && (item as BaseArmor).Crafter != null)
                        Crafter = (item as BaseArmor).Crafter.Mobile;
                    if ((item is BaseWeapon) && (item as BaseWeapon).Crafter != null)
                        Crafter = (item as BaseWeapon).Crafter.Mobile;

                    if (Crafter == null)
                        from.SendMessage("This item does not carry a Makers Mark.");
                    else if (m_Cert.Valid(Crafter))
                        from.SendMessage("The Makers Mark of this item matches the identity of this certificate.");
                    else
                        from.SendMessage("The Makers Mark of this item does not match the identity of this certificate.");
                }
            }
            else if (target is PlayerMobile)
            {
                PlayerMobile pm = target as PlayerMobile;
                if (m_Cert.Valid(pm))
                    from.SendMessage("The player you are targeting matches the identity of this certificate.");
                else
                    from.SendMessage("The player you are targeting does not match the identity of this certificate.");
            }
            else
            {
                from.SendMessage("You may only validate the identity of players and items.");
            }
        }
    }

    public class CertificateOfIdentity : Item // Create the item class which is derived from the base item class
    {
        private Mobile m_Mobile = null;
        private iFlags m_flags = iFlags.None;

        [Flags]
        private enum iFlags
        {
            None = 0x00,
            Used = 0x01
        }

        [Constructable]
        public CertificateOfIdentity()
            : base(0x14F0)
        {
            Weight = 1.0;
            Name = "a certificate of identity";
            LootType = LootType.Blessed;
        }

        public CertificateOfIdentity(Serial serial)
            : base(serial)
        {
        }

        public bool Valid(Mobile m)
        {
            return m_Mobile != null && m != null && m_Mobile.Serial == m.Serial;
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Used
        {
            get { return (m_flags & iFlags.Used) > 0; }
            set { if (value == true) m_flags |= iFlags.Used; else m_flags &= ~iFlags.Used; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Mobile
        {
            get { return m_Mobile; }
            set { m_Mobile = value; }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_Mobile);
            writer.Write((int)m_flags);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    m_Mobile = reader.ReadMobile();
                    m_flags = (iFlags)reader.ReadInt();
                    goto default;

                default:
                    break;
            }

            if (m_Mobile == null && (m_flags & iFlags.Used) > 0)
            {
                Name = String.Format("a certificate of death");
            }
        }

        public override bool DisplayLootType { get { return false; } }

        public override void OnDoubleClick(Mobile from) // Override double click of the deed to call our target
        {
            if (!IsChildOf(from.Backpack)) // Make sure its in their pack
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
            }
            else
            {
                if (m_Mobile != null && m_Mobile.Deleted == false)
                {
                    from.SendMessage("Target the item or player you would like to validate.");
                    from.Target = new ValidateIdentityTarget(this); // Call our target
                }
                else
                {
                    if ((m_flags & iFlags.Used) > 0)
                    {
                        from.SendMessage("It is a sad day, but this player no longer exists in this world.");
                        Name = String.Format("a certificate of death");
                    }
                    else
                    {
                        from.SendMessage("Target the item or player you would like to create a certificate for.");
                        from.Target = new TicketTarget(this); // Call our target
                    }
                }
            }
        }
    }
}