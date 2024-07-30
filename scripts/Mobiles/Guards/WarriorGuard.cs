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

/* Scripts/Mobiles/Guards/WarriorGuard.cs
 * ChangeLog
 *  7/31/22, Adam
 *      Guards on Mortalis don't guardwhack, they send you to AI Prison for 1 hour.
 *  7/24/2021, Adam
 *      suppress chatter if we are hidden
 *	2/15/11, Adam
 *		Return insta-kill guards for UOMO
 *	11/15/10, Adam
 *		Return insta-kill guards for UOSP
 *	7/12/10, Adam
 *		o Switch over to a more normal hybrid ai and handle reveal as per that AI (remove the special guard reveal ai)
 *			but leave the the messages and even add some.
 *		o guard ai is smart about when to use sowrds vs magic .. could probably be smarter even, see PreferMagic in this file
 *			and in the GuardAI.
 *	6/21/10, adam
 *		o Have guards use the detect hidden skill to reveal hidden players that they believe are there.
 *		o move PatrolGuard code here, then base PatrolGuard on WarriorGuard
 *	04/19/05, Kit
 *		updated bank closeing code to not have guard change direction of player(was causeing them to be paralyzed) 
 *  7/26/04, Old Salty
 * 		Added a few lines (96-99) to make the criminal turn, closing the bankbox, when the guard attacks.
 *  6/21/04, Old Salty
 * 		Commented out the previous change (in IdleTimer) because it is now handled in BaseGuard.cs.
 * 		Modified the search/reveal code so that guards can reveal players.
 *  6/20/04, Old Salty
 * 		Modified IdleTimer so that guards defend themselves properly.
 *	6/10/04, mith
 *		Modified for the new guard non-insta-kill guards.
 */

using Server.Items;
using Server.Network;
using System;

namespace Server.Mobiles
{
    public class WarriorGuard : BaseGuard
    {
        private Timer m_AttackTimer, m_IdleTimer;
        int m_version = 0;                          // used in subclass to initialize
        private Mobile m_Focus;
        private Mobile m_LastFocus;

        [Constructable]
        public WarriorGuard()
            : this(null)
        {
        }

        public WarriorGuard(Mobile target)
            : base(target)
        {
        }

        public WarriorGuard(Serial serial)
            : base(serial)
        {
        }

        // does this guard auto 'poof' when en no longer needed?
        public virtual bool PoofingGuard { get { return true; } }

        public override void AlterMeleeDamageTo(Mobile to, ref int damage)
        {
            if (!to.Player)
                damage = (int)(to.HitsMax * .6);
        }

        public override bool OnBeforeDeath()
        {
            if (m_Focus != null && m_Focus.Alive)
                new AvengeTimer(m_Focus).Start(); // If a guard dies, three more guards will spawn

            return base.OnBeforeDeath();
        }

        protected int Version { get { return m_version; } }

        [CommandProperty(AccessLevel.GameMaster)]
        public override Mobile Focus
        {
            get
            {
                return m_Focus;
            }
            set
            {
                if (Deleted)
                    return;

                m_LastFocus = m_Focus;

                if (m_LastFocus != value)
                {
                    m_Focus = value;

                    if (value != null)
                    {
                        this.AggressiveAction(value);
                        if (value.BankBox != null)
                            value.BankBox.Close();
                        value.Send(new MobileUpdate(value)); //send a update packet to let client know BB is closed.
                    }

                    Combatant = value;

                    if (m_LastFocus != null && !m_LastFocus.Alive)
                        if (m_LastFocus.Body.IsHuman)
                            Say("Thou hast suffered thy punishment, scoundrel.");
                        else
                            Say("Thou hast suffered thy punishment, vile beast.");

                    if (value != null)
                        if (value.Body.IsHuman)
                            Say(500131); // Thou wilt regret thine actions, swine!
                        else
                            Say("There is place for you here, vile beast.");

                    if (m_AttackTimer != null)
                    {
                        m_AttackTimer.Stop();
                        m_AttackTimer = null;
                    }

                    if (m_IdleTimer != null)
                    {
                        m_IdleTimer.Stop();
                        m_IdleTimer = null;
                    }

                    if (m_Focus != null)
                    {
                        m_AttackTimer = new AttackTimer(this);
                        m_AttackTimer.Start();
                        ((AttackTimer)m_AttackTimer).DoOnTick();
                    }
                    else
                    {
                        m_IdleTimer = new IdleTimer(this, m_LastFocus, PoofingGuard);
                        m_IdleTimer.Start();
                    }
                }
                else if (m_Focus == null && m_IdleTimer == null)
                {
                    m_IdleTimer = new IdleTimer(this, m_LastFocus, PoofingGuard);
                    m_IdleTimer.Start();
                }
            }
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (e.Mobile is PlayerMobile && e.Mobile.Alive)
            {
                if (e.Speech.ToLower() == this.Name.ToLower())
                {
                    this.Say("Did you want to talk to me?");
                    e.Handled = true;
                }

                if (e.HasKeyword(0x003b)) // *hi*
                {
                    this.Say("Hello to thee, {0}.", e.Mobile.Name);
                    e.Handled = true;
                }

                if (e.HasKeyword(0x00fa)) // *bye*
                {
                    this.Say("Fare thee well, {0}.", e.Mobile.Name);
                    e.Handled = true;
                }

                if (e.HasKeyword(0x0016)) // *help* 
                {

                    this.Say("Tryeth asking me a specific question.");
                    e.Handled = true;
                    /*switch (Utility.Random(5))
                    {
                        case 0:
                            this.Say("I would assist thee, but I am tired."); break;
                        case 1:
                            this.Say("Alas, I cannot help thee, I am on my break."); break;
                        case 2:
                            this.Say("No help for thee today."); break;
                        case 3:
                            this.Say("We shall protect thee."); break;
                        case 4:
                            this.Say("What is my help worth to thee?"); break;
                    }*/
                }

                if (e.Handled == false)
                    base.OnSpeech(e);
            }
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {   // 8/21/21, Adam: I'm expanding calling guards to apply to monsters in town too
            //  Currently, guards wait for the creature to go criminal before attacking, which in the case of an EV
            //  it could be too late
            if (/*m.Player && */m.Region.IsGuarded && m.Region is Regions.GuardedRegion)
                (m.Region as Regions.GuardedRegion).CheckGuardCandidate(m, m.Player);

            base.OnMovement(m, oldLocation);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            //	dummy version to allow the PatrolGuard subclass to initialize

            // version 0
            writer.Write(m_Focus);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            m_version = reader.ReadInt();

            switch (m_version)
            {
                case 1:
                    {
                        goto case 0;
                    }
                case 0:
                    {
                        m_Focus = reader.ReadMobile();

                        if (m_Focus != null)
                        {
                            m_AttackTimer = new AttackTimer(this);
                            m_AttackTimer.Start();
                        }
                        else
                        {
                            m_IdleTimer = new IdleTimer(this, m_LastFocus, PoofingGuard);
                            m_IdleTimer.Start();
                        }

                        break;
                    }
            }
        }

        public override void OnAfterDelete()
        {
            if (m_AttackTimer != null)
            {
                m_AttackTimer.Stop();
                m_AttackTimer = null;
            }

            if (m_IdleTimer != null)
            {
                m_IdleTimer.Stop();
                m_IdleTimer = null;
            }

            base.OnAfterDelete();
        }

        private class AvengeTimer : Timer
        {
            private Mobile m_Focus;

            public AvengeTimer(Mobile focus)
                : base(TimeSpan.FromSeconds(2.5), TimeSpan.FromSeconds(1.0), 3)
            {
                m_Focus = focus;
            }

            protected override void OnTick()
            {
                BaseGuard.Spawn(m_Focus, m_Focus, 1, true);
            }
        }

        private class AttackTimer : Timer
        {
            private WarriorGuard m_Owner;
            private DateTime m_NextRevealChatter = DateTime.UtcNow - TimeSpan.FromMilliseconds(850);
            private bool bWasHidden = false;

            public AttackTimer(WarriorGuard owner)
                : base(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.1))
            {
                m_Owner = owner;
            }

            public void DoOnTick()
            {
                OnTick();
            }

            protected void Imprison(Mobile target, TimeSpan duration)
            {
                // BreakLOS
                target.Hidden = true;

                // Effect
                Effects.SendLocationEffect(new Point3D(target.X + 1, target.Y, target.Z + 4), target.Map, 0x3728, 13);
                Effects.SendLocationEffect(new Point3D(target.X + 1, target.Y, target.Z), target.Map, 0x3728, 13);
                Effects.SendLocationEffect(new Point3D(target.X + 1, target.Y, target.Z - 4), target.Map, 0x3728, 13);
                Effects.SendLocationEffect(new Point3D(target.X, target.Y + 1, target.Z + 4), target.Map, 0x3728, 13);
                Effects.SendLocationEffect(new Point3D(target.X, target.Y + 1, target.Z), target.Map, 0x3728, 13);
                Effects.SendLocationEffect(new Point3D(target.X, target.Y + 1, target.Z - 4), target.Map, 0x3728, 13);

                Effects.SendLocationEffect(new Point3D(target.X + 1, target.Y + 1, target.Z + 11), target.Map, 0x3728, 13);
                Effects.SendLocationEffect(new Point3D(target.X + 1, target.Y + 1, target.Z + 7), target.Map, 0x3728, 13);
                Effects.SendLocationEffect(new Point3D(target.X + 1, target.Y + 1, target.Z + 3), target.Map, 0x3728, 13);
                Effects.SendLocationEffect(new Point3D(target.X + 1, target.Y + 1, target.Z - 1), target.Map, 0x3728, 13);

                // transport setup
                (target as PlayerMobile).PrisonInmate = true;
                (target as PlayerMobile).MinimumSentence = DateTime.UtcNow + duration;
                Item aiEntrance = new AIEntrance();
                aiEntrance.OnMoveOver(target);
                aiEntrance.Delete();
            }

            protected override void OnTick()
            {
                if (m_Owner.Deleted)
                {
                    Stop();
                    return;
                }

                m_Owner.Criminal = false;
                m_Owner.LongTermMurders = 0;
                m_Owner.Stam = m_Owner.StamMax;

                Mobile target = m_Owner.Focus;

                if (target != null && (target.Deleted || !target.Alive || !m_Owner.CanBeHarmful(target)))
                {
                    m_Owner.Focus = null;
                    Stop();
                    return;
                }
                else if (m_Owner.Weapon is Fists)
                {
                    m_Owner.Kill();
                    Stop();
                    return;
                }

                if (target != null && m_Owner.Combatant != target)
                    m_Owner.Combatant = target;

                if (target == null)
                {
                    Stop();
                }
                else if (Core.RuleSets.SiegeStyleRules())
                {// <instakill>
                    TeleportTo(target);
                    target.BoltEffect(0);

                    if (target is BaseCreature)
                        ((BaseCreature)target).NoKillAwards = true;

                    target.Damage(target.HitsMax, m_Owner, this);
                    target.Kill(); // just in case, maybe Damage is overriden on some shard

                    if (target.Corpse != null && !target.Player)
                        target.Corpse.Delete();

                    m_Owner.Focus = null;
                    Stop();
                }// </instakill>
                else if (Core.RuleSets.MortalisRules())
                {
                    TeleportTo(target);
                    target.BoltEffect(0);

                    if (target is BaseCreature)
                    {
                        ((BaseCreature)target).NoKillAwards = true;

                        target.Damage(target.HitsMax, m_Owner, this);
                        target.Kill(); // just in case, maybe Damage is overriden on some shard

                        if (target.Corpse != null && !target.Player)
                            target.Corpse.Delete();
                    }
                    else
                    {   // no guardwhack on Mortalis - go to prison
                        Imprison(target, TimeSpan.FromHours(1));
                    }

                    m_Owner.Focus = null;
                    Stop();
                }
                else if (!m_Owner.InRange(target, 20))
                {
                    m_Owner.Focus = null;
                }
                else if ((!m_Owner.InRange(target, 10) || !m_Owner.InLOS(target)) && !PreferMagic())
                {
                    TeleportTo(target);
                }
                else if (!m_Owner.InRange(target, 1) && !PreferMagic())
                {
                    if (!m_Owner.Move(m_Owner.GetDirectionTo(target) | Direction.Running))
                        TeleportTo(target);
                }
                else if (!m_Owner.CanSee(target) && DateTime.UtcNow > m_NextRevealChatter)
                {
                    bWasHidden = true;
                    m_NextRevealChatter = DateTime.UtcNow + TimeSpan.FromMilliseconds(1500);
                    switch (Utility.Random(4))
                    {
                        case 0: m_Owner.Say("Reveal yourself!"); break;
                        case 1: m_Owner.Say("Reveal!"); break;
                        case 2: m_Owner.Say("I know you are here somewhere!"); break;
                        case 3: m_Owner.Say("I'll find you!"); break;
                    }
                }
                else if (m_Owner.CanSee(target) && bWasHidden)
                {
                    bWasHidden = false;
                    switch (Utility.Random(4))
                    {
                        case 0: m_Owner.Say("Ah ha! I have found you"); break;
                        case 1: m_Owner.Say("There you are you wretch!"); break;
                        case 2: m_Owner.Say("You can run but you cannot hide."); break;
                        case 3: m_Owner.Say("Gotcha!"); break;
                    }
                }
            }

            public bool PreferMagic()
            {
                if (m_Owner != null && m_Owner.AIObject != null)
                {
                    if (m_Owner.AIObject is BaseHybridAI)
                        (m_Owner.AIObject as BaseHybridAI).PreferMagic();
                }

                return false;
            }

            private string m_LastMessage;
            private void Yelp(string text)
            {
                if (m_LastMessage != text)
                {
                    m_LastMessage = text;
                    m_Owner.Say(text);
                }
            }

            private void TeleportTo(Mobile target)
            {
                Point3D from = m_Owner.Location;
                Point3D to = target.Location;

                m_Owner.Location = to;

                Effects.SendLocationParticles(EffectItem.Create(from, m_Owner.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                Effects.SendLocationParticles(EffectItem.Create(to, m_Owner.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 5023);

                m_Owner.PlaySound(0x1FE);
            }
        }

        private class IdleTimer : Timer
        {
            private WarriorGuard m_Owner;
            private int m_Stage;
            private bool m_AutoDelete;
            private Mobile m_Mobile;
            private int[] said = new int[5];

            public IdleTimer(WarriorGuard owner, Mobile m, bool AutoDelete)
                : base(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.5))
            {
                m_Owner = owner;
                m_Mobile = m;
                m_AutoDelete = AutoDelete;
            }

            protected override void OnTick()
            {
                if (m_Owner.Deleted)
                {
                    Stop();
                    return;
                }

                if (m_AutoDelete)
                {
                    if ((m_Stage++ % 4) == 0 || !m_Owner.Move(m_Owner.Direction))
                        m_Owner.Direction = (Direction)Utility.Random(8);

                    if (m_Stage > 16)
                    {
                        Stop();
                        Effects.SendLocationParticles(EffectItem.Create(m_Owner.Location, m_Owner.Map, EffectItem.DefaultDuration), 0x3728, 10, 10, 2023);
                        m_Owner.PlaySound(0x1FE);
                        m_Owner.Delete();
                    }
                }
                else
                {
                    if (m_Mobile == null)
                        Stop();
                    else if (m_Stage++ % 4 == 0)
                    {
                        if (m_Owner.Hidden == false)
                        {
                            if (Utility.RandomBool())
                                switch (Utility.Random(5))
                                {   // say things just once
                                    case 0: if (said[0]++ == 0) m_Owner.Say("Let that be a lesson to you {0}!", m_Mobile.Name); break;
                                    case 1: if (said[1]++ == 0) m_Owner.Say("Take heed citizens."); break;
                                    case 2: if (said[2]++ == 0) m_Owner.Say("{0} won�t be giving us any more trouble.", m_Mobile.Female ? "She" : "He"); break;
                                    case 3: if (said[3]++ == 0) m_Owner.Say("{0} > {1}", m_Owner.Name, m_Mobile.Name); break;
                                    case 4: if (said[4]++ == 0) m_Owner.Whisper("That'll teach {0}.", m_Mobile.Female ? "her" : "him"); break;
                                }
                        }
                    }
                    else if (m_Stage > 16)
                    {   // salute
                        if (m_Owner.Hidden == false)
                            m_Owner.Animate(33, 5, 1, true, false, 0);
                        Stop();
                    }
                }
            }
        }
    }
}