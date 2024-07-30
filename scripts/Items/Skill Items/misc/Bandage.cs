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

/* Scripts/Skill Items/Misc/Bandage.cs
 * ChangeLog
 *  7/28/2023, Adam (AbortBandage)
 *      Expanded to handle either of the healer or patient dying during the healing process
 *  7/27/2023, Adam (AbortBandage)
 *      Called from Mobile.Kill, AbortBandage aborts any bandage queued for this mobile
 *  3/14/22, Yoar
 *      Implementation of RunUO's ConPVP system.
 *	11/27/07, Adam
 *		Add 'blocked path' checking to ProximityCheck() function.
 *		e.g. bookcase blocking a doorway; door open patient in room, healer standing next to bookcase (not in front of) can heal AROUND bookcase
 *		Notes: Current restrictions to minimize world-wide impact:
 *			limit to Resurrection, and in houses. These restrictions can be changes as needed.
 *  11/25/07, Adam
 *		Replace simple 18 unit Z order check with new ProximityCheck() function.
 *		Check proximity and report exploitive behavior when healing with bandages (or the spell.)
 *	11/24/07, Adam
 *		Add more 18 unit Z order check for healing.
 *		This fixes a new "resurrect from above" exploit.
 *	7/9/05, Pix
 *		Now must be in your backpack to use.
 *	3/17/05, Adam
 *		Add a 18 unit Z order check for healing.
 *		This fixes the "resurrect on patio" exploit.
 *	1/16/05, Adam
 *		Add new function IsFollower() to differentiate between pets (Veterinary) and followers (healing)
 *		This is important for healing orc followers as they usually appear as a monster and would require Veterinary.
 *		Caution: This code assumes that a Tamable will never be a follower.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 * 3/25/04 changes by mith
 *	modified CheckSkill call to use max value of 100.0 instead of 120.0.
 */

using Server.Commands;
using Server.Diagnostics;
using Server.Gumps;
using Server.Mobiles;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using static Server.Utility;

namespace Server.Items
{
    public class Bandage : Item, IDyable
    {
        [Constructable]
        public Bandage()
            : this(1)
        {
        }

        [Constructable]
        public Bandage(int amount)
            : base(0xE21)
        {
            Stackable = true;
            Weight = 0.1;
            Amount = amount;
        }

        public Bandage(Serial serial)
            : base(serial)
        {
        }

        // Check proximity and report exploitive behavior
        public static bool ProximityCheck(Mobile healer, Mobile patient, int distance)
        {
            if (healer is Mobile == false || patient is Mobile == false)
                return false;

            try
            {
                // normal error, nothing suspicious
                if (healer.InRange(patient, distance) == false)
                {
                    //if (patient is PlayerMobile) (patient as PlayerMobile).Say("Normal: too far");
                    return false;
                }

                // z axis exploit
                if (Math.Abs(patient.Z - healer.Z) > 18)
                {
                    //if (patient is PlayerMobile) (patient as PlayerMobile).Say("Exploit: Z axis");
                    // log the exploiter and possible accomplices
                    RecordCheater.Cheater(healer, "Bandage resurrection with > 18 Z axis.", true);
                    return false;
                }

                // Possible through door or other blocking tile exploit
                if (patient is PlayerMobile && patient.Alive == false) // Resurrection
                    if (CanSpawnLandMobile(patient.Map, patient.Location, CanFitFlags.checkMobiles | CanFitFlags.ignoreDeadMobiles | CanFitFlags.requireSurface) == false)
                    {
                        //if (patient is PlayerMobile) (patient as PlayerMobile).Say("Exploit: through door");
                        // log the exploiter and possible accomplices
                        RecordCheater.Cheater(healer, "Possible through door or other blocking tile exploit.", true);
                        return false;
                    }

                // Ghost must remain in LOS
                if (Server.Commands.Diagnostics.LineOfSight(healer, patient) == false)
                {
                    //if (patient is PlayerMobile) (patient as PlayerMobile).Say("Exploit: not in LOS");
                    // log the exploiter and possible accomplices
                    RecordCheater.Cheater(healer, "not in LOS.", true);
                    return false;
                }

                // Possible blocked path ..
                //	e.g. bookcase blocking a doorway; door open patient in room, healer standing next to bookcase (not infront of)
                //	can heal AROUND bookcase
                // Notes: Current restrictions to minimize world-wide impact:
                //	limit to Resurrection, and in houses. These restrictions can be changes as needed.
                int newZ = 0;
                if (patient is PlayerMobile && patient.Alive == false) // Resurrection
                    if (Multis.BaseHouse.FindHouseAt(patient) != null) // if they are in a house
                        if (healer.CheckMovement(healer.GetDirectionTo(patient.Location), out newZ) == false)
                        {
                            RecordCheater.Cheater(healer, "Blocked path.", true);
                            return false;
                        }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                return false;
            }

            return true;
        }

        public bool Dye(Mobile from, DyeTub sender)
        {
            if (Deleted)
                return false;

            Hue = sender.DyedHue;

            return true;
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

        public override void OnDoubleClick(Mobile from)
        {
            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); //This must be in your backpack
            }
            else if (from.InRange(GetWorldLocation(), Core.RuleSets.AOSRules() ? 2 : 1))
            {
                from.RevealingAction();

                from.SendLocalizedMessage(500948); // Who will you use the bandages on?

                from.Target = new InternalTarget(this);
            }
            else
            {
                from.SendLocalizedMessage(500295); // You are too far away to do that.
            }
        }

        public override Item Dupe(int amount)
        {
            return base.Dupe(new Bandage(), amount);
        }

        private class InternalTarget : Target
        {
            private Bandage m_Bandage;

            public InternalTarget(Bandage bandage)
                : base(1, false, TargetFlags.Beneficial)
            {
                m_Bandage = bandage;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Bandage.Deleted)
                    return;

                if (targeted is Mobile)
                {
                    Mobile m = targeted as Mobile;

                    if (Bandage.ProximityCheck(from, m, Core.RuleSets.AOSRules() ? 2 : 1) == false)
                    {
                        from.SendLocalizedMessage(501043); // Target is not close enough.
                    }
                    else if (from.InRange(m_Bandage.GetWorldLocation(), Core.RuleSets.AOSRules() ? 2 : 1))
                    {
                        if (BandageContext.BeginHeal(from, (Mobile)targeted) != null)
                        {
                            if (!Engines.ConPVP.DuelContext.IsFreeConsume(from))
                                m_Bandage.Consume();
                        }
                    }
                    else
                    {
                        from.SendLocalizedMessage(500295); // You are too far away to do that.
                    }
                }
                else
                {
                    from.SendLocalizedMessage(500970); // Bandages can not be used on that.
                }
            }
        }
    }

    public class BandageContext
    {
        private Mobile m_Healer;
        private Mobile m_Patient;
        private int m_Slips;
        private Timer m_Timer;

        public Mobile Healer { get { return m_Healer; } }
        public Mobile Patient { get { return m_Patient; } }
        public int Slips { get { return m_Slips; } set { m_Slips = value; } }
        public Timer Timer { get { return m_Timer; } }

        public void Slip()
        {
            m_Healer.SendLocalizedMessage(500961); // Your fingers slip!
            ++m_Slips;
        }

        public BandageContext(Mobile healer, Mobile patient, TimeSpan delay)
        {
            m_Healer = healer;
            m_Patient = patient;

            m_Timer = new InternalTimer(this, delay);
            m_Timer.Start();
        }

        public void StopHeal()
        {
            m_Table.Remove(m_Healer);

            if (m_Timer != null)
                m_Timer.Stop();

            m_Timer = null;
        }

        public static void AbortBandage(Mobile died)
        {
            // if the either the healer or patient is killed
            List<BandageContext> list = new();
            foreach (DictionaryEntry context in m_Table)
                if (context.Value is BandageContext bc)
                    if (bc.Patient == died || bc.Healer == died)
                        // for canceling
                        list.Add(bc);

            // cancel the heals and notify informed parties
            foreach (var context in list)
            {
                context.StopHeal();

                // if the Patient died
                if (context.Patient == died)
                {   // the patient was being healed by someone else
                    if (context.Healer != context.Patient)
                    {
                        context.Patient.SendMessage("{0} was trying to heal you, but you died!.", context.Healer.Name);
                        context.Healer.SendMessage("They died and the bandage has no effect.");
                    }
                    else
                        context.Patient.SendLocalizedMessage(500962); // You were unable to finish your work before you died.
                }
                // the healer died
                else
                {   // the healer was healing someone else
                    if (context.Healer != context.Patient)
                    {
                        context.Patient.SendMessage("{0} died and the bandage has no effect.", context.Healer.Name);
                        context.Healer.SendLocalizedMessage(500962); // You were unable to finish your work before you died.
                    }
                    else
                        context.Healer.SendLocalizedMessage(500962); // You were unable to finish your work before you died.
                }
            }
        }
        private static Hashtable m_Table = new Hashtable();

        public static BandageContext GetContext(Mobile healer)
        {
            return (BandageContext)m_Table[healer];
        }

        // Adam: Followers get healed with bandages!
        private static bool IsFollower(Mobile m)
        {
            if (m != null && m is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)m;
                if (bc.Tamable == false && bc.IOBAlignment != IOBAlignment.None)
                    return true;
            }
            return false;
        }

        public static SkillName GetPrimarySkill(Mobile m)
        {
            if (!m.Player && (m.Body.IsMonster || m.Body.IsAnimal) && !IsFollower(m))
                return SkillName.Veterinary;
            else
                return SkillName.Healing;
        }

        public static SkillName GetSecondarySkill(Mobile m)
        {
            if (!m.Player && (m.Body.IsMonster || m.Body.IsAnimal) && !IsFollower(m))
                return SkillName.AnimalLore;
            else
                return SkillName.Anatomy;
        }

        public void EndHeal()
        {
            StopHeal();

            int healerNumber = -1, patientNumber = -1;
            bool playSound = true;
            bool checkSkills = false;

            SkillName primarySkill = GetPrimarySkill(m_Patient);
            SkillName secondarySkill = GetSecondarySkill(m_Patient);

            BaseCreature petPatient = m_Patient as BaseCreature;

            if (!m_Healer.Alive)
            {
                healerNumber = 500962; // You were unable to finish your work before you died.
                patientNumber = -1;
                playSound = false;
            }
            else if (Bandage.ProximityCheck(m_Healer, m_Patient, Core.RuleSets.AOSRules() ? 2 : 1) == false)
            {
                healerNumber = 500963; // You did not stay close enough to heal your target.
                patientNumber = -1;
                playSound = false;
            }
            else if (!m_Patient.Alive || (petPatient != null && petPatient.IsDeadPet))
            {
                double healing = m_Healer.Skills[primarySkill].Value;
                double anatomy = m_Healer.Skills[secondarySkill].Value;
                double chance = ((healing - 68.0) / 50.0) - (m_Slips * 0.02);

                if ((checkSkills = (healing >= 80.0 && anatomy >= 80.0)) && chance > Utility.RandomDouble())
                {
                    if (m_Patient.Map == null || !Utility.CanFit(m_Patient.Map, m_Patient.Location, 16, Utility.CanFitFlags.requireSurface))
                    {
                        healerNumber = 501042; // Target can not be resurrected at that location.
                        patientNumber = 502391; // Thou can not be resurrected there!
                    }
                    else
                    {
                        healerNumber = 500965; // You are able to resurrect your patient.
                        patientNumber = -1;

                        m_Patient.PlaySound(0x214);
                        m_Patient.FixedEffect(0x376A, 10, 16);

                        if (petPatient != null && petPatient.IsDeadPet)
                        {
                            Mobile master = petPatient.ControlMaster;

                            if (master != null && master.InRange(petPatient, 3))
                            {
                                healerNumber = 503255; // You are able to resurrect the creature.
                                master.SendGump(new PetResurrectGump(m_Healer, petPatient));
                            }
                            else
                            {
                                healerNumber = 1049670; // The pet's owner must be nearby to attempt resurrection.
                            }
                        }
                        else
                        {
                            m_Patient.SendGump(new ResurrectGump(m_Patient, m_Healer));
                        }
                    }
                }
                else
                {
                    if (petPatient != null && petPatient.IsDeadPet)
                        healerNumber = 503256; // You fail to resurrect the creature.
                    else
                        healerNumber = 500966; // You are unable to resurrect your patient.

                    patientNumber = -1;
                }
            }
            else if (m_Patient.Poisoned)
            {
                m_Healer.SendLocalizedMessage(500969); // You finish applying the bandages.

                double healing = m_Healer.Skills[primarySkill].Value;
                double anatomy = m_Healer.Skills[secondarySkill].Value;
                double chance = ((healing - 30.0) / 50.0) - (m_Patient.Poison.Level * 0.1) - (m_Slips * 0.02);

                if (!m_Patient.Incurable && (checkSkills = (healing >= 60.0 && anatomy >= 60.0)) && chance > Utility.RandomDouble())
                {
                    if (m_Patient.CurePoison(m_Healer))
                    {
                        healerNumber = (m_Healer == m_Patient) ? -1 : 1010058; // You have cured the target of all poisons.
                        patientNumber = 1010059; // You have been cured of all poisons.
                    }
                    else
                    {
                        healerNumber = -1;
                        patientNumber = -1;
                    }
                }
                else
                {
                    healerNumber = 1010060; // You have failed to cure your target!
                    patientNumber = -1;
                }
            }
            else if (BleedAttack.IsBleeding(m_Patient))
            {
                healerNumber = -1;
                patientNumber = 1060167; // The bleeding wounds have healed, you are no longer bleeding!

                BleedAttack.EndBleed(m_Patient, true);
            }
            else if (MortalStrike.IsWounded(m_Patient))
            {
                healerNumber = (m_Healer == m_Patient ? 1005000 : 1010398);
                patientNumber = -1;
                playSound = false;
            }
            else if (m_Patient.Hits == m_Patient.HitsMax)
            {
                healerNumber = 500967; // You heal what little damage your patient had.
                patientNumber = -1;
            }
            else
            {
                checkSkills = true;
                patientNumber = -1;

                double healing = m_Healer.Skills[primarySkill].Value;
                double anatomy = m_Healer.Skills[secondarySkill].Value;
                double chance = ((healing + 10.0) / 100.0) - (m_Slips * 0.02);

                if (chance > Utility.RandomDouble())
                {
                    healerNumber = 500969; // You finish applying the bandages.

                    double min, max;

                    if (Core.RuleSets.AOSRules())
                    {
                        min = (anatomy / 8.0) + (healing / 5.0) + 4.0;
                        max = (anatomy / 6.0) + (healing / 2.5) + 4.0;
                    }
                    else
                    {
                        min = (anatomy / 5.0) + (healing / 5.0) + 3.0;
                        max = (anatomy / 5.0) + (healing / 2.0) + 10.0;
                    }

                    double toHeal = min + (Utility.RandomDouble() * (max - min));

                    if (m_Patient.Body.IsMonster || m_Patient.Body.IsAnimal)
                        toHeal += m_Patient.HitsMax / 100;

                    if (Core.RuleSets.AOSRules())
                        toHeal -= toHeal * m_Slips * 0.35; // TODO: Verify algorithm
                    else
                        toHeal -= m_Slips * 4;

                    if (toHeal < 1)
                    {
                        toHeal = 1;
                        healerNumber = 500968; // You apply the bandages, but they barely help.
                    }

                    m_Patient.Heal((int)toHeal);
                }
                else
                {
                    healerNumber = 500968; // You apply the bandages, but they barely help.
                    playSound = false;
                }
            }

            if (healerNumber != -1)
                m_Healer.SendLocalizedMessage(healerNumber);

            if (patientNumber != -1)
                m_Patient.SendLocalizedMessage(patientNumber);

            if (playSound)
                m_Patient.PlaySound(0x57);

            if (checkSkills)
            {
                m_Healer.CheckSkill(secondarySkill, 0.0, 100.0, contextObj: new object[2]);
                m_Healer.CheckSkill(primarySkill, 0.0, 100.0, contextObj: new object[2]);
            }
        }

        private class InternalTimer : Timer
        {
            private BandageContext m_Context;

            public InternalTimer(BandageContext context, TimeSpan delay)
                : base(delay)
            {
                m_Context = context;
                Priority = TimerPriority.FiftyMS;
            }

            protected override void OnTick()
            {
                m_Context.EndHeal();
            }
        }

        public static BandageContext BeginHeal(Mobile healer, Mobile patient)
        {
            bool isDeadPet = (patient is BaseCreature && ((BaseCreature)patient).IsDeadPet);

            if (patient is Golem)
            {
                healer.SendLocalizedMessage(500970); // Bandages cannot be used on that.
            }
            else if (patient is BaseCreature && ((BaseCreature)patient).IsAnimatedDead)
            {
                healer.SendLocalizedMessage(500951); // You cannot heal that.
            }
            else if (!patient.Poisoned && patient.Hits == patient.HitsMax && !BleedAttack.IsBleeding(patient) && !isDeadPet)
            {
                healer.SendLocalizedMessage(500955); // That being is not damaged!
            }
            else if (!patient.Alive && (patient.Map == null || !Utility.CanFit(patient.Map, patient.Location, 16, Utility.CanFitFlags.requireSurface)))
            {
                healer.SendLocalizedMessage(501042); // Target cannot be resurrected at that location.
            }
            else if (healer.CanBeBeneficial(patient, true, true))
            {
                healer.DoBeneficial(patient);

                bool onSelf = (healer == patient);
                int dex = healer.Dex;

                double seconds;
                double resDelay = (patient.Alive ? 0.0 : 5.0);

                if (onSelf)
                {
                    if (Core.RuleSets.AOSRules())
                        seconds = 5.0 + (0.5 * ((double)(120 - dex) / 10)); // TODO: Verify algorithm
                    else
                        seconds = 9.4 + (0.6 * ((double)(120 - dex) / 10));
                }
                else
                {
                    if (Core.RuleSets.AOSRules() && GetPrimarySkill(patient) == SkillName.Veterinary)
                    {
                        if (dex >= 40)
                            seconds = 2.0;
                        else
                            seconds = 3.0;
                    }
                    else
                    {
                        if (dex >= 100)
                            seconds = 3.0 + resDelay;
                        else if (dex >= 40)
                            seconds = 4.0 + resDelay;
                        else
                            seconds = 5.0 + resDelay;
                    }
                }

                BandageContext context = GetContext(healer);

                if (context != null)
                    context.StopHeal();

                context = new BandageContext(healer, patient, TimeSpan.FromSeconds(seconds));

                m_Table[healer] = context;

                if (!onSelf)
                    patient.SendLocalizedMessage(1008078, false, healer.Name); //  : Attempting to heal you.

                healer.SendLocalizedMessage(500956); // You begin applying the bandages.
                return context;
            }

            return null;
        }
    }
}