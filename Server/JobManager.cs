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

#if JobManager
namespace Server
{
	/// <summary>
	/// The worker function to call asynchronously.
	/// ALL communication between the worker and the rest of the server must be through the parameter and return value.
	/// </summary>
	/// <param name="args">A state object to pass to the worker function. Must be deep copies of everything to be referenced.</param>
	/// <returns>An object, can be null. The results of the work.</returns>
	public delegate object JobWorker(object args);

	/// <summary>
	/// A function that will be called in the main thread once the asynchronous work has been completed.
	/// </summary>
	/// <param name="job">The job that just completed.</param>
	public delegate void JobCompletedCallback(ThreadJob job);

	public enum JobPriority : int
	{
		Idle		= 0,
		Low			= 1,
		Normal		= 2,
		High		= 3,
		Critical	= 4
	}

	public enum JobStatus
	{
		New,
		Enqueued,
		Running,
		Finished,
		Aborted,
		Error
	}

	internal enum ThreadStatus : int
	{
		Running,
		ReadyToDie,
		Dead,
        Error
	}

	internal class ThreadObject
	{
		private ThreadStatus m_Status;
		private ThreadJob m_Job;
		private readonly Thread m_Thread;
		private readonly AutoResetEvent m_Signal;
		private readonly object SyncRoot;
		private static int m_NextID = 1;

		public AutoResetEvent Signal
		{
			get
			{	// no lock here because AutoResetEvent is fully thread-safe, and readonly
				return m_Signal;
			}
		}


		public ThreadStatus Status
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(SyncRoot))
                    {
                        return m_Status;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return ThreadStatus.Error;
                }
			}
			set
			{
                try
                {
                    using (TimedLock.Lock(SyncRoot))
                    {
                        // once we're dead, we're dead, no resurrecting!
                        if (m_Status != ThreadStatus.Dead)
                            m_Status = value;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
			}
		}

		public Thread Thread
		{
			get
			{	// no lock here because Thread is fully thread-safe, and readonly
				return m_Thread;
			}
		}

		public ThreadJob Job
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(SyncRoot))
                    {
                        return m_Job;
                    }
                }
                catch (Exception e)
                {
				    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
				    System.Console.WriteLine(e.StackTrace);
                    return null;
                }
			}
			set
			{
                try
                {
                    using (TimedLock.Lock(SyncRoot))
                    {
                        m_Job = value;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
			}
		}

		public ThreadObject()
		{
			SyncRoot = new object();
			m_Thread = Thread.CurrentThread;
			m_Thread.Name = string.Format("JobWorker {0}", m_NextID++);
			m_Job = null;
			m_Status = ThreadStatus.Running;
			m_Signal = new AutoResetEvent(false);
		}
	}

	/// <summary>
	/// An asynchronous job object for use with Server.JobManager.
	/// </summary>
	public sealed class ThreadJob
	{
		private DateTime m_Enqueued;
		private JobWorker m_Worker;
		private JobCompletedCallback m_Completed;
		private object m_JobArgs;
		private object m_JobResults;
		private JobStatus m_Status;

		private readonly object SyncRoot;

		/// <summary>
		/// Returns the time the job was enqueued.
		/// Locks: this.SyncRoot
		/// </summary>
		internal DateTime Enqueued
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
                    {
                        return m_Enqueued;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return DateTime.MaxValue;
                }
			}
		}

		/// <summary>
		/// Returns the status of the job.
		/// Locks: this.SyncRoot
		/// </summary>
		public JobStatus Status
		{
			get
			{
				try
				{
					using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
					{
						return m_Status;
					}
				}
				catch (LockTimeoutException e)
				{
					Console.WriteLine("\n\nDeadlock detected!\n");
					Console.WriteLine(e.ToString());
					return JobStatus.Error;
				}
			}
		}

		/// <summary>
		/// Returns the results of the job. If the job's Status is Error, this returns the exception thrown.
		/// Locks: this.SyncRoot
		/// </summary>
		public object Results
		{
			get
			{
				try
				{
					using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
					{
						return m_JobResults;
					}
				}
				catch (LockTimeoutException e)
				{
					Console.WriteLine("\n\nDeadlock detected!\n");
					Console.WriteLine(e.ToString());
					return null;
				}
			}
		}

		/// <summary>
		/// Initializes a new ThreadJob.
		/// </summary>
		/// <param name="worker">A delegate that will be called to perform the actual work.</param>
		/// <param name="workerArgs">A parameter that will be passed to the worker delegate. Can be null.</param>
		public ThreadJob(JobWorker worker, object workerArgs)
		{
			SyncRoot = new object();

			m_Worker = worker;
			m_JobArgs = workerArgs;
			m_Completed = null;
			m_Enqueued = DateTime.MinValue;
			m_Status = JobStatus.New;
		}

		/// <summary>
		/// Initializes a new ThreadJob.
		/// </summary>
		/// <param name="worker">A delegate that will be called to perform the actual work.</param>
		/// <param name="workerArgs">A parameter that will be passed to the worker delegate.</param>
		/// <param name="completedcallback">A delegate that will be called in the main thread when the job has completed.</param>
		public ThreadJob(JobWorker worker, object workerArgs, JobCompletedCallback completedcallback)
		{
			SyncRoot = new object();

			m_Worker = worker;
			m_Completed = completedcallback;
			m_JobArgs = workerArgs;
			m_Enqueued = DateTime.MinValue;
			m_Status = JobStatus.New;
		}
		
		/// <summary>
		/// Enqueues this job into the JobManager system.
		/// Locks: this.SyncRoot->JobManager.m_ThreadStatsLock
		/// </summary>
		/// <param name="priority">The priority at which to enqueue this job.</param>
		/// <returns>True on success, false if the JobManager was full or if this job was already started.</returns>
		public bool Start(JobPriority priority)
		{
			try
			{
				using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
				{
					if (m_Status == JobStatus.New)
					{
						m_Enqueued = DateTime.UtcNow;
						if (JobManager.Enqueue(this, priority))
						{
							m_Status = JobStatus.Enqueued;
							return true;
						}
						else
							return false;
					}
					else
						return false;
				}
			}
			catch (LockTimeoutException e)
			{
				Console.WriteLine("\n\nDeadlock detected!\n");
				Console.WriteLine(e.ToString());
				return false;
			}
		}

		/// <summary>
		/// Removes this job from the JobManager, if it has not yet began working. There is no way to restart an aborted job, a new one must be created.
		/// Locks: this.SyncRoot->JobManager.m_ThreadStatsLock
		/// </summary>
		public void Abort()
		{
			try
			{
				using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
				{
					m_Worker = null;
					m_Status = JobStatus.Aborted;
					JobManager.AbortedJobs++;
				}
			}
			catch (LockTimeoutException e)
			{
				Console.WriteLine("\n\nDeadlock detected!\n");
				Console.WriteLine(e.ToString());
			}
		}

		/// <summary>
		/// Resets the Enqueued time for this job. Used for promotion algorithm.
		/// Locks: this.SyncRoot
		/// </summary>
		internal void ResetEnqueued()
		{
            try
            {
                using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
                {
                    m_Enqueued = DateTime.UtcNow;
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
		}

		/// <summary>
		/// Calls the supplied worker delegate and takes care of housekeeping.
		/// Locks: this.SyncRoot->JobManager.m_ThreadStatsLock
		/// </summary>
		/// <returns>True if job ran, false if job was aborted.</returns>
		internal bool DoWork()
		{
			JobWorker worker;
            try
            {
                using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
                {
                    if (m_Worker == null)
                    {
                        JobManager.AbortedJobs--;
                        return false;
                    }

                    worker = m_Worker;
                    m_Status = JobStatus.Running;
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
                return false;
            }

			object results;
			try
			{
				results = worker(m_JobArgs);
			}
			catch (Exception e)
			{
				results = e;
			}
            
            try
            {
                using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
                {
                    m_JobResults = results;
                    if (results is Exception)
                        m_Status = JobStatus.Error;
                    else
                        m_Status = JobStatus.Finished;
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
                return false;
            }

			return true;
		}

		/// <summary>
		/// Calls the work-completed delegate.
		/// Locks: this->SyncRoot
		/// </summary>
		internal void DoCompleted()
		{
            try
            {
                JobCompletedCallback cb;
                using (TimedLock.Lock(SyncRoot)) // this.SyncRoot
                {
                    if (m_Completed == null)
                        return;

                    cb = m_Completed;
                }

                cb(this);
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
		}
	}

	/// <summary>
	/// A prioritized threadpool for use with ThreadJob.
	/// </summary>
	public sealed class JobManager
	{
		private static int m_MinThreadCount = 20;
		private static int m_MaxThreadCount = 100;
		private static int m_IdleThreadLifespan = 5000;
		private static int m_PriorityPromotionDelay = 5000;
		private static int m_AbortedJobs = 0;
		private static int m_QueueLengthLimit = 128;
		private static int m_TotalJobsRun = 0;
		private static TimeSpan m_TotalJobTime = TimeSpan.Zero;
		private static int m_LastError = 0;
		private static volatile bool m_Running = false;
		private static readonly object m_ThreadStatsLock = new object();
		
		private static Queue[] m_Jobs = new Queue[5]
			{
				Queue.Synchronized(new Queue()),
				Queue.Synchronized(new Queue()),
				Queue.Synchronized(new Queue()),
				Queue.Synchronized(new Queue()),
				Queue.Synchronized(new Queue())
			};
		private static Queue m_Completed = Queue.Synchronized(new Queue());
		
		private static ArrayList m_RunningArray = ArrayList.Synchronized(new ArrayList(20));
		private static ArrayList m_ReadyStack = ArrayList.Synchronized(new ArrayList(20));
		private static Thread m_Scheduler = null;

		/// <summary>
		/// Gets or sets the JobManager's minimum pooled thread count.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static int MinThreadCount
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        return m_MinThreadCount;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return 0;
                }
			}
			set
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        if (value >= 0 && value <= m_MaxThreadCount)
                            m_MinThreadCount = value;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
			}
		}

		/// <summary>
		/// Gets or sets the JobManager's maximum pooled thread count.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static int MaxThreadCount
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        return m_MaxThreadCount;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return 100;
                }
			}
			set
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        if (value >= 0 && value >= m_MaxThreadCount)
                            m_MaxThreadCount = value;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
			}
		}

		/// <summary>
		/// Gets or sets the lifespan, in milliseconds, of an idle thread.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static int IdleThreadLifespan
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        return m_IdleThreadLifespan;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return 5000;
                }
			}
			set
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        if (value >= 0)
                            m_IdleThreadLifespan = value;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
			}
		}

		/// <summary>
		/// Gets or sets the amount of time, in ms, a job must wait before it is promoted to the next-higher priority.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static int PriorityPromotionDelay
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        return m_PriorityPromotionDelay;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return 5000;
                }
			}
			set
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        if (value >= 0)
                            m_PriorityPromotionDelay = value;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
			}
		}

		/// <summary>
		/// Gets the current number of threads in the JobManager.
		/// </summary>
		public static int CurrentThreadCount
		{
			get
			{
				return m_RunningArray.Count + m_ReadyStack.Count;
			}
		}

		/// <summary>
		/// Gets the current number of threads that are processing work.
		/// </summary>
		public static int RunningThreadCount
		{
			get
			{
				return m_RunningArray.Count;
			}
		}

		/// <summary>
		/// Gets the current numnber of threads that are waiting for work.
		/// </summary>
		public static int ReadyThreadCount
		{
			get
			{
				return m_ReadyStack.Count;
			}
		}

		/// <summary>
		/// Gets the total number of jobs that are waiting to be processed.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static int TotalEnqueuedJobs
		{
			get
			{
				int count = 0;

				for (int i = (int)JobPriority.Critical; i >= (int)JobPriority.Idle; i--)
					count += m_Jobs[i].Count;
				
				return count - AbortedJobs;
			}
		}

		/// <summary>
		/// Gets or sets the number of enqueued jobs that are aborted.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		internal static int AbortedJobs
		{
			get
			{
                try
                {

                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        return m_AbortedJobs;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return 0;
                }
			}
			set
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        if (value >= 0)
                            m_AbortedJobs = value;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
			}
		}

		/// <summary>
		/// Gets or sets the total number of jobs that the JobManager will allow at any one time.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static int QueueLengthLimit
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        return m_QueueLengthLimit;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return 1;
                }
			}
			set
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        if (value > 0)
                            m_QueueLengthLimit = value;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
			}
		}

		/// <summary>
		/// Gets the total number of jobs processed in the JobManager, since it was started.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static int TotalJobsRun
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        return m_TotalJobsRun;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return 0;
                }
			}
		}

		/// <summary>
		/// Gets the total time spent in worker delegates since the JobManager was started.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static TimeSpan TotalJobTime
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        return m_TotalJobTime;
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return TimeSpan.Zero;
                }
			}
		}

		/// <summary>
		/// Gets a string representing the last error that occurred in the JobManager.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		public static string LastError
		{
			get
			{
                try
                {
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        switch (m_LastError)
                        {
                            case 0:
                                return "None";
                            case 1:
                                return "Queue length exceeds half of hard limit.";
                            case 2:
                                return "Rejected job because queue was full.";
                            default:
                                return "Invalid LastError value.";
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return "TimedLock exception.";
                }
			}
		}

		/// <summary>
		/// Adds a job to the JobManager.
		/// Locks: JobManager.m_ThreadStatsLock
		/// </summary>
		/// <param name="job">The job to enqueue.</param>
		/// <param name="priority">The priority of the job.</param>
		/// <returns>True if added, false if queue was full.</returns>
		internal static bool Enqueue(ThreadJob job, JobPriority priority)
		{
			if (TotalEnqueuedJobs >= QueueLengthLimit && priority != JobPriority.Critical)
			{
                try
                {
                    bool print = false;
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        if (m_LastError != 2)
                            print = true;
                        m_LastError = 2;
                    }
                    if (print)
                        Console.WriteLine("{0} JM Error: Rejected job because queue was full.", DateTime.UtcNow);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                }
				return false;
			}

			if (TotalEnqueuedJobs >= QueueLengthLimit / 2)
			{
                try
                {
                    bool print = false;
                    using (TimedLock.Lock(m_ThreadStatsLock))
                    {
                        if (m_LastError != 1)
                            print = true;
                        m_LastError = 1;
                    }
                    if (print)
                        Console.WriteLine("{0} JM Warning: Queue length exceeds half of hard limit.", DateTime.UtcNow);
                }
                catch (Exception e)
                {
                    System.Console.WriteLine("Exception Caught in JobManager code: " + e.Message);
                    System.Console.WriteLine(e.StackTrace);
                    return false;
                }
			}

			m_Jobs[(int)priority].Enqueue(job);

			return true;
		}

		/// <summary>
		/// Called each frame of processing. Handles job-completed callbacks.
		/// Locks: None
		/// </summary>
		private static void MainThread_CallbackSlice()
		{
			ThreadJob job;

			try
			{
				// adam: avoid exception by explicitly handling case
				if (m_Completed == null || m_Completed.Count == 0)
					return;

				job = (ThreadJob)m_Completed.Dequeue();
			}
			catch (InvalidOperationException)
			{
				return;
			}

			job.DoCompleted();
		}

		/// <summary>
		/// Assigns work to threads and keeps house.
		/// Locks: m_ThreadStatsLock, threadlock, ThreadJob.SyncRoot
		/// </summary>
		private static void ThreadScheduler()
		{
			try
			{
				while (true)
				{
					// move finished threads back into ready stack
					for (int i = m_RunningArray.Count - 1; i >= 0; i--)
					{
						if (i >= m_RunningArray.Count)
							continue;

						ThreadObject thread = m_RunningArray[i] as ThreadObject;
						
						// make sure the thread is alive, sanity check
						if (!thread.Thread.IsAlive)
						{
							if (m_Running) // dead threads in running array is expected when we're shutting down
								Console.WriteLine("Dead thread found in JobManager.m_RunningArray: {0}", thread.Thread.Name);
							m_RunningArray.Remove(thread);
							continue;
						}
						
						if (thread.Job == null)
						{
							m_RunningArray.Remove(thread);
							m_ReadyStack.Add(thread);
						}
					}

					// kill off threads that need to die
					for (int i = m_ReadyStack.Count - 1; i >= 0 && CurrentThreadCount > MinThreadCount; i--)
					{
						if (i >= m_ReadyStack.Count)
							continue;

						ThreadObject thread = m_ReadyStack[i] as ThreadObject;
						m_ReadyStack.Remove(thread);
						if (thread.Status == ThreadStatus.ReadyToDie)
						{
							thread.Status = ThreadStatus.Dead;
							thread.Signal.Set(); // wake the thread up so it can see it needs to die
						}
					}

					// if we're shutting down, pulse all the waiting threads so they shut down
					if (!m_Running)
					{
						if (m_RunningArray.Count == 0 && m_ReadyStack.Count == 0)
							break; // all the workers are dead, now we die

						while (m_ReadyStack.Count > 0)
						{
							ThreadObject thread = m_ReadyStack[0] as ThreadObject;
							
							thread.Status = ThreadStatus.Dead;
							thread.Signal.Set(); // wake the thread up so it can see it needs to die
						}

						continue;
					}
					
					// promote old jobs to a higher priority - no higher than High
					// only one promotion per priority per iteration
					DateTime promotion  = DateTime.UtcNow - TimeSpan.FromMilliseconds(PriorityPromotionDelay);
					for (int i = (int)JobPriority.Low; i < (int)JobPriority.High; i++)
					{
						if (m_Jobs[i].Count  == 0)
							continue;

						ThreadJob job = m_Jobs[i].Peek() as ThreadJob;
						if (job.Enqueued < promotion)
						{
							m_Jobs[i + 1].Enqueue(m_Jobs[i].Dequeue());
							job.ResetEnqueued();
						}
					}

					// assign work
					for (int i = (int)JobPriority.Critical; i >= (int)JobPriority.Idle; i--)
					{
						while (m_ReadyStack.Count > 0 && m_Jobs[i].Count > 0)
						{
							ThreadObject thread = m_ReadyStack[m_ReadyStack.Count - 1] as ThreadObject;
							m_ReadyStack.Remove(thread); // don't use m_ReadyStack.Count - 1, possible to change
							m_RunningArray.Add(thread);
							thread.Job = m_Jobs[i].Dequeue() as ThreadJob;
							thread.Signal.Set(); // wake thread up
						}
					}

					// make sure we've got the minimum # of threads
					if (CurrentThreadCount < MinThreadCount)
					{
						Thread worker = new Thread(new ThreadStart(ThreadWorker));
						worker.IsBackground = true;
						worker.Start();
					}

					// see if we should create some more threads for critical tasks
					for (int i = 0; i < m_Jobs[(int)JobPriority.Critical].Count; i++)
					{
						Thread worker = new Thread(new ThreadStart(ThreadWorker));
						worker.IsBackground = true;
						worker.Start();
					}

					// lastly, create one more thread if work is getting backed up long enough
					if (m_Jobs[(int)JobPriority.High].Count > 0 && // make sure there's a job in the first place
						((ThreadJob)m_Jobs[(int)JobPriority.High].Peek()).Enqueued < promotion && // check if it's overdue
						CurrentThreadCount < MaxThreadCount) // make sure we're not at max - main diff between this and promotion, crit gets new thread always
					{
						Thread worker = new Thread(new ThreadStart(ThreadWorker));
						worker.IsBackground = true;
						worker.Start();
					}

					Thread.Sleep(100);
				}
			}
			finally
			{
				m_Running = false;
			}
		}
		
		/// <summary>
		/// Workhorse of the JobManager. Waits until a job is assigned to it and then runs the work.
		/// Locks: m_ThreadStatsLock, threadlock, ThreadJob.SyncRoot
		/// </summary>
		private static void ThreadWorker()
		{
			ThreadObject me = new ThreadObject();
			try
			{
				m_ReadyStack.Add(me);

				while (true)
				{
					if (!me.Signal.WaitOne(IdleThreadLifespan, false))
					{
						me.Status = ThreadStatus.ReadyToDie;
						continue;
					}

					if (!m_Running || me.Status == ThreadStatus.Dead)
						break; // quit

					try
					{
						ThreadJob job = me.Job;
						if (job == null) // sanity check
							continue;

						me.Status = ThreadStatus.Running;
					
						if (job.DoWork())
						{
							m_Completed.Enqueue(job);
						}
					}
					finally
					{
						me.Job = null;
					}
				}
			}
			finally
			{
				if (me.Job != null)
					Console.WriteLine("Worker thread dying with non-null job!!");
				
				me.Job = null;
			}
		}

		/// <summary>
		/// Starts the JobManager system, if it's not already running.
		/// </summary>
		public static void Start()
		{
			m_Running = true;

			if (m_Scheduler == null)
			{
				Console.WriteLine("Starting JobManager.");
				m_Scheduler = new Thread(new ThreadStart(ThreadScheduler));
				m_Scheduler.IsBackground = true;
				// Adam: should probably run one step below main thread.
				m_Scheduler.Priority = ThreadPriority.BelowNormal;
				m_Scheduler.Start();
				Server.Core.Slice += new Slice(MainThread_CallbackSlice);
			}
		}

		/// <summary>
		/// Stops the JobManager system. Attempts to shut down threads gracefully.
		/// </summary>
		/// <param name="timeout">The amount of time to wait for scheduler thread to exit itself before hard aborting it.</param>
		public static void Stop(TimeSpan timeout)
		{
			m_Running = false;

			Console.Write("Waiting for JM Scheduler to finish...");
			if (m_Scheduler != null)
			{
				if (m_Scheduler.Join(timeout))
					Console.WriteLine("stopped gracefully.");
				else
				{
					m_Scheduler.Abort();
					Console.WriteLine("aborted.");
				}
			}
		}
	}
}
#endif