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

/* Scripts/Mobiles/Healers/BaseHealer.cs
 * ChangeLog
 *	06/28/06, Adam
 *		Logic cleanup
 *  06/27/06, Kit
 *		Changed IsInvunerable to constructor as is no longer a virtual function.
 *	9/15/05, Adam
 *		Remove the call to CheckWork() from OnThink().
 *		The heartbeat already calls OnThink() if there is work that needs to be done.
 *		Having OnThink() also call CheckWork() could get into a funky recall loop if 
 *		there was some problem with the NPC getting home.
 *	7/26/05, erlein
 *		Automated removal of AoS resistance related function calls. 6 lines removed.
 *	6/8/05, erlein
 *		Added check to see if res is allowed via PlayerMobile::SpiritCohesive()
 *	5/15/05, Kit
 *		Changed AI to New evilmageAI type
 *	5/12/05, Adam
 *		Add CheckTravel() to CheckWork() to insure that we can recall FROM where we are
 *		and TO where we want to go.
 *	5/11/05, Kit
 *		Updated healers to use spawner location vs home location.
 *	5/11/05, Kit
 *		Made healers recall home if out of there home region for longer then 15 minutes.
 *	4/18/04, Adam
 *		Add the following messages when trying to use barding on a healer:
 *		Provoke attempted: Say( "You will regret having provoked me!" );
 *		Peace attempted: Say( "Leave me alone!" );
 *	7/16/04, mith
 *		Added check to verify that Res Target is not inside a house.
 *	6/14/04, Pix
 *		Commented out where the healer closes RessurectGumps on the
 *		person he's going to offer ressurection to before he sends the
 *		gump.  In the case where two healers both offer ressurection at
 *		the same time, this would close the ressurection gumps and the
 *		dead player wouldn't be offered a resurrection.
 *		Having more than one resurrect gump open at once seems to cause
 *		no problems.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Gumps;
using Server.Items;
using Server.Multis;
using Server.Spells;
using Server.Spells.Fourth;
using System;
using System.Collections;
namespace Server.Mobiles
{
    public abstract class BaseHealer : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override bool IsActiveVendor { get { return false; } }

        private DateTime m_NextRecallHomeTime;
        private static TimeSpan RecallDelay = TimeSpan.FromMinutes(15.0);

        private bool RecallTimerSet;

        public override bool Unprovokable
        {
            get
            {
                if (Hits > 1)
                    Say("You will regret having provoked me!");

                return BardImmune;
            }
        }

        public override bool Uncalmable
        {
            get
            {
                if (Hits > 1)
                    Say("Leave me alone!");

                return BardImmune;
            }
        }

        public override void InitSBInfo()
        {
        }

        public BaseHealer()
            : base(null)
        {
            SpeechHue = 0;

            SetStr(304, 400);
            SetDex(102, 150);
            SetInt(204, 300);

            SetDamage(10, 23);

            SetSkill(SkillName.Anatomy, 75.0, 97.5);
            SetSkill(SkillName.EvalInt, 82.0, 100.0);
            SetSkill(SkillName.Healing, 75.0, 97.5);
            SetSkill(SkillName.Magery, 82.0, 100.0);
            SetSkill(SkillName.MagicResist, 82.0, 100.0);
            SetSkill(SkillName.Tactics, 82.0, 100.0);

            Fame = 1000;
            Karma = 10000;

            PackItem(new Bandage(Utility.RandomMinMax(5, 10)), lootType: LootType.UnStealable);
            PackItem(new HealPotion());
            PackItem(new CurePotion());

            m_NextRecallHomeTime = DateTime.UtcNow + RecallDelay;

            Timer.DelayCall(TimeSpan.FromSeconds(15.0), new TimerCallback(CheckWork));
        }

        public BaseHealer(Serial serial)
            : base(serial)
        {
            Timer.DelayCall(TimeSpan.FromMinutes(15.0), new TimerCallback(CheckWork));
        }

        public override VendorShoeType ShoeType { get { return VendorShoeType.Sandals; } }

        public virtual int GetRobeColor()
        {
            return Utility.RandomYellowHue();
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Robe(GetRobeColor()));
        }

        public virtual bool CheckResurrect(Mobile m)
        {
            return true;
        }

        private DateTime m_NextResurrect;
        private static TimeSpan ResurrectDelay = TimeSpan.FromSeconds(2.0);

        public override void OnThink()
        {
            // Healers try to recall home if they were lured away or trapped
            if (RecallTimerSet == true && DateTime.UtcNow >= m_NextRecallHomeTime)
            {
                DebugSay(DebugFlags.AI, "I am recalling home");
                this.Say("You must have forgotten that I am magical.");
                new NpcRecallSpell(this, null, SpawnerLocation).Cast();
                RecallTimerSet = false;
            }

            base.OnThink();
        }

        public void CheckWork()
        {
            // if we are out of our home range, recall home!
            Point3D start; Point3D end;
            if (Spawner != null && SpawnerLocation != Point3D.Zero && RecallTimerSet == false)
            {
                // can we Recall FROM here?
                if (!SpellHelper.CheckTravel(this, TravelCheckType.RecallFrom))
                    goto exit_now;

                // can we Recall TO here?
                if (!SpellHelper.CheckTravel(this.Region.Map, SpawnerLocation, TravelCheckType.RecallTo, this))
                    goto exit_now;

                start = SpawnerLocation;
                end = SpawnerLocation;
                //are we inside are home area ?
                if (Location.X >= start.X - RangeHome && Location.Y >= start.Y - RangeHome && Location.X < end.X + RangeHome && Location.Y < end.Y + RangeHome)
                {   //reset timer were back in are home area
                    RecallTimerSet = false;
                    m_NextRecallHomeTime = DateTime.UtcNow + RecallDelay;
                    goto exit_now;
                }
                else
                {   //start are timer - we need to get home!
                    if (RecallTimerSet == false)
                    {
                        DebugSay(DebugFlags.AI, "I am planning my escape");
                        m_NextRecallHomeTime = DateTime.UtcNow + RecallDelay;
                        RecallTimerSet = true;
                        this.OnThink();
                        this.AIObject.Think();
                    }
                    goto exit_now;
                }
            }

        exit_now:
            Timer.DelayCall(TimeSpan.FromSeconds(15.0), new TimerCallback(CheckWork));
        }

        public virtual void OfferResurrection(Mobile m)
        {
            Direction = GetDirectionTo(m);
            Say(501224); // Thou hast strayed from the path of virtue, but thou still deservest a second chance.

            m.PlaySound(0x214);
            m.FixedEffect(0x376A, 10, 16);

            //m.CloseGump( typeof( ResurrectGump ) );
            m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {

            if (!m.Frozen && !m.Alive && DateTime.UtcNow >= m_NextResurrect && InRange(m, 4) && !InRange(oldLocation, 4) && InLOS(m))
            {
                m_NextResurrect = DateTime.UtcNow + ResurrectDelay;

                BaseHouse house = BaseHouse.FindHouseAt(m);

                if (m.Map == null || !Utility.CanFit(m.Map, m.Location, 16, Utility.CanFitFlags.requireSurface) || house != null)
                {
                    m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
                }
                else if (CheckResurrect(m))
                {
                    // erl: Check SpiritCohesion level!!
                    if (m is PlayerMobile)
                        if (!((PlayerMobile)m).SpiritCohesive())
                        {
                            m.SendMessage("Your spirit lacks the cohesion necessary for resurrection.");
                            return;
                        }


                    OfferResurrection(m);
                }
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
        }
    }
}