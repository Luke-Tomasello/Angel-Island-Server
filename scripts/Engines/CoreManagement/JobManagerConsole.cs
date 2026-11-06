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

/* Scripts/Engines/CoreManagement/JobManagerConsole.cs
 * Changelog
 *	01/01/06 Taran Kain
 *		Initial version.
 *  3/18/26, Luke
 *      Refactored to inherit from BaseConsole.
 */

namespace Server.Engines.Consoles
{
    public class JobManagerConsole : BaseConsole
    {
        [Constructable]
        public JobManagerConsole()
            : base("JobManager Console")
        {
        }

        public JobManagerConsole(Serial s)
            : base(s)
        {
        }

#if JobManager
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int CurrentThreadCount => JobManager.CurrentThreadCount;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int IdleThreadLifespan
        {
            get => JobManager.IdleThreadLifespan;
            set => JobManager.IdleThreadLifespan = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxThreadCount
        {
            get => JobManager.MaxThreadCount;
            set => JobManager.MaxThreadCount = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MinThreadCount
        {
            get => JobManager.MinThreadCount;
            set => JobManager.MinThreadCount = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int PriorityPromotionDelay
        {
            get => JobManager.PriorityPromotionDelay;
            set => JobManager.PriorityPromotionDelay = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int QueueLengthLimit
        {
            get => JobManager.QueueLengthLimit;
            set => JobManager.QueueLengthLimit = value;
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int ReadyThreadCount => JobManager.ReadyThreadCount;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int RunningThreadCount => JobManager.RunningThreadCount;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int TotalEnqueuedJobs => JobManager.TotalEnqueuedJobs;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int TotalJobsRun => JobManager.TotalJobsRun;

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan TotalJobTime => JobManager.TotalJobTime;

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version

            writer.Write(IdleThreadLifespan);
            writer.Write(MaxThreadCount);
            writer.Write(MinThreadCount);
            writer.Write(PriorityPromotionDelay);
            writer.Write(QueueLengthLimit);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    IdleThreadLifespan = reader.ReadInt();
                    MaxThreadCount = reader.ReadInt();
                    MinThreadCount = reader.ReadInt();
                    PriorityPromotionDelay = reader.ReadInt();
                    QueueLengthLimit = reader.ReadInt();
                    break;
            }
        }
#else
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(1); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
#endif
    }
}