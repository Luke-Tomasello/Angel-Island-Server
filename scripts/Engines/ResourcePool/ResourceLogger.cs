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

/* Scripts/Engines/ResourcePool/ResourceLogger.cs
 * ChangeLog
 *  04/27/05 TK
 *		Removed annoying "Logging!" message each time a transaction was logged.
 *  02/11/05 Taran Kain
 *		Made sure all files get closed so that backups occur successfully.
 *	03/02/05 Taran Kain
 *		Consolidated from ResourcePool.cs
 */

using Server.Diagnostics;
using System;
using System.Collections;
using System.IO;

namespace Server.Engines.ResourcePool
{
    [Flags]
    public enum TransactionType
    {
        Sale,
        Purchase,
        Payment
    }

    public class ResourceTransaction
    {
        private long m_TransactionID;
        private DateTime m_Date;
        private TransactionType m_TransType;

        public string ResName;
        public int Amount;
        public double Price;
        public int NewAmount;
        public Serial VendorID;

        public long TransactionID { get { return m_TransactionID; } }
        public DateTime Date { get { return m_Date; } }
        public TransactionType TransType { get { return m_TransType; } }

        public ResourceTransaction(TransactionType ttype)
        {
            m_TransType = ttype;
            m_TransactionID = ResourceLogger.GetTransID();
            m_Date = DateTime.UtcNow;
            ResName = "";
            Amount = 0;
            Price = 0;
            NewAmount = 0;
            VendorID = 0;
        }

        public ResourceTransaction(GenericReader reader)
        {
            Deserialize(reader);
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            // version 0
            writer.Write((long)m_TransactionID);
            writer.Write((DateTime)m_Date);
            writer.Write((int)m_TransType);
            writer.Write((string)ResName);
            writer.Write((int)Amount);
            writer.Write((double)Price);
            writer.Write((int)NewAmount);
            writer.Write((int)VendorID.Value);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_TransactionID = reader.ReadLong();
                        m_Date = reader.ReadDateTime();
                        m_TransType = (TransactionType)reader.ReadInt();
                        ResName = reader.ReadString();
                        Amount = reader.ReadInt();
                        Price = reader.ReadDouble();
                        NewAmount = reader.ReadInt();
                        VendorID = (Serial)reader.ReadInt();
                        break;
                    }
                default:
                    {
                        Console.WriteLine("Error: Invalid save version for Resource Transaction.");
                        break;
                    }
            }
        }

        public override string ToString()
        {
            switch (m_TransType)
            {
                case TransactionType.Payment:
                    {
                        return "Recieved " + (int)(Price * Amount) + "gp for the sale of " + Amount + " " + ResName + ((Amount == 1) ? "" : "s") + ".";
                    }
                case TransactionType.Purchase:
                    {
                        return "Paid " + (int)(Price * Amount) + "gp for " + Amount + " " + ResName + ((Amount == 1) ? "" : "s") + ".";
                    }
                case TransactionType.Sale:
                    {
                        return "Consigned " + Amount + " " + ResName + ((Amount == 1) ? "" : "s") + " at " + Price + "gp each.";
                    }
                default:
                    {
                        return "Cooked the books but definitely got caught. Find Taran Kain!";
                    }
            }
        }
    }


    public class ResourceLogger
    {
        private static int m_LogLevel;
        private static int m_TransactionStack;
        private static long m_TransID; // pllllenty of transactions available
        private static Hashtable m_History;

        public static int LogLevel
        {
            get { return m_LogLevel; }
            set { if (m_LogLevel >= 0) { m_LogLevel = ((value >= 0) ? value : 0); } }
        }

        public static ICollection History
        {
            get { return m_History.Values; }
        }

        public static void Initialize()
        {
            //Server.Commands.Register("rplog", AccessLevel.GameMaster, new CommandEventHandler(RPLog_Handler));
        }

        public static void Configure()
        {
            EventSink.WorldLoad += new WorldLoadEventHandler(OnLoad);
            EventSink.WorldSave += new WorldSaveEventHandler(OnSave);

            m_TransactionStack = 0;
            m_TransID = 0;
            m_History = new Hashtable();

            m_LogLevel = 1;
        }

        public static void OnSave(WorldSaveEventArgs e)
        {
            Console.WriteLine("ResourceLogger saving...");

            if (!Directory.Exists("Saves/ResourcePool"))
                Directory.CreateDirectory("Saves/ResourcePool");
            if (!Directory.Exists("Saves/ResourcePool/TransactionHistories"))
                Directory.CreateDirectory("Saves/ResourcePool/TransactionHistories");

            BinaryFileWriter writer = new BinaryFileWriter(new FileStream("Saves/ResourcePool/Logger.dat", FileMode.Create, FileAccess.Write), true);

            writer.Write((int)0); // version

            // version 0
            writer.Write((int)m_LogLevel);
            writer.Write((long)m_TransID);

            writer.Close();

            foreach (Mobile m in m_History.Keys)
            {
                ArrayList list = (ArrayList)m_History[m];
                writer = new BinaryFileWriter(new FileStream("Saves/ResourcePool/TransactionHistories/" + m.Serial.Value.ToString() + ".dat", FileMode.Create, FileAccess.Write), true);
                writer.Write((int)list.Count);
                foreach (ResourceTransaction rt in list)
                    rt.Serialize(writer);
                writer.Close();
            }
        }

        public static void OnLoad()
        {
            Console.WriteLine("ResourceLogger loading...");
            try
            {
                if (!Directory.Exists("Saves/ResourcePool"))
                    Directory.CreateDirectory("Saves/ResourcePool");
                if (!Directory.Exists("Saves/ResourcePool/TransactionHistories"))
                    Directory.CreateDirectory("Saves/ResourcePool/TransactionHistories");

                FileStream fs = new FileStream("Saves/ResourcePool/Logger.dat", FileMode.Open, FileAccess.Read);
                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(fs));

                int version = reader.ReadInt();

                switch (version)
                {
                    case 0:
                        {
                            m_LogLevel = reader.ReadInt();
                            m_TransID = reader.ReadInt();
                            break;
                        }
                    default:
                        {
                            throw new Exception("Invalid save version for ResourceLogger.");
                        }
                }

                fs.Close();
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("Warning: ResourceLogger savefile not found. Reverting to defaults.");
            }
        }

        public static long GetTransID()
        {
            m_TransactionStack++;
            return m_TransID;
        }

        public static ArrayList GetTransactions(string nameFilter)
        {
            ArrayList hist = new ArrayList();
            foreach (ArrayList al in m_History.Values)
            {
                foreach (ResourceTransaction rt in al)
                {
                    if (rt.ResName == nameFilter || nameFilter == "all")
                        hist.Add(rt);
                }
            }

            return hist;
        }

        public static bool Add(ResourceTransaction rt, Mobile player)
        {
            if (!(m_History[player] is ArrayList))
                LoadHistory(player);

            int count = 0;
            foreach (ResourceTransaction r in (ArrayList)m_History[player])
            {
                if (r.TransactionID == rt.TransactionID && r.TransType == rt.TransType)
                    break;
                count++;
            }

            // fear not, this is just a sh!tload of casts and indexes
            if (count < ((ArrayList)m_History[player]).Count)
                ((ResourceTransaction)((ArrayList)m_History[player])[count]).Amount += rt.Amount;
            else
                ((ArrayList)m_History[player]).Insert(0, rt);
            if (((ArrayList)m_History[player]).Count > 50)
                ((ArrayList)m_History[player]).RemoveRange(49, ((ArrayList)m_History[player]).Count - 50);

            m_TransactionStack--;
            if (m_TransactionStack == 0)
                m_TransID++;

            // log
            if (m_LogLevel > 0)
            {
                try
                {
                    if (!Directory.Exists("Saves/ResourcePool"))
                        Directory.CreateDirectory("Saves/ResourcePool");
                    if (!Directory.Exists("Saves/ResourcePool/TransactionHistories"))
                        Directory.CreateDirectory("Saves/ResourcePool/TransactionHistories");

                    BinaryFileWriter writer = new BinaryFileWriter(new FileStream("Saves/ResourcePool/MasterHistory.dat", FileMode.Append, FileAccess.Write, FileShare.Read), true);
                    rt.Serialize(writer);
                    writer.Close();
                }
                catch (Exception e)
                {
                    LogHelper.LogException(e);
                    Console.WriteLine("ResourceLogger error: Failed to open MasterHistory.dat for writing.");
                    Console.WriteLine(e.ToString());
                }
            }

            return true;
        }

        public static string GetHistory(Mobile player)
        {
            if (!(m_History[player] is ArrayList))
                LoadHistory(player);

            string history = "";
            foreach (ResourceTransaction rt in ((ArrayList)m_History[player]))
                history += rt.ToString() + "\n";

            return history;
        }

        public static void LoadHistory(Mobile player)
        {
            m_History.Remove(player);
            m_History[player] = new ArrayList();

            try
            {
                if (!Directory.Exists("Saves/ResourcePool"))
                    Directory.CreateDirectory("Saves/ResourcePool");
                if (!Directory.Exists("Saves/ResourcePool/TransactionHistories"))
                    Directory.CreateDirectory("Saves/ResourcePool/TransactionHistories");

                string pathName = "Saves/ResourcePool/TransactionHistories/" + player.Serial.Value.ToString() + ".dat";
                if (File.Exists(pathName))
                {
                    FileStream fs = new FileStream(pathName, FileMode.Open, FileAccess.Read);
                    BinaryFileReader reader = new BinaryFileReader(new BinaryReader(fs));
                    for (int i = reader.ReadInt(); i > 0; i--)
                        ((ArrayList)m_History[player]).Add(new ResourceTransaction(reader));
                    fs.Close();
                }
                // else, this player simply has no transaction history.
                // FYI this routine used to catch the FileNotFound exception. A file not found is hardly worthy an exception.
                //  Now we just handle the 'normal' case, and catch only 'true' exceptions.
            }
            catch (Exception ex) { LogHelper.LogException(ex); }
        }

        /*[Usage("RPLog")]
		[Description("Prints the ResourcePool master history to console.")]
		public static void RPLog_Handler(CommandEventArgs e)
		{
			ArrayList transactions = new ArrayList();
			
			if (!Directory.Exists("Saves/ResourcePool"))
				Directory.CreateDirectory("Saves/ResourcePool");
			if (!Directory.Exists("Saves/ResourcePool/TransactionHistories"))
				Directory.CreateDirectory("Saves/ResourcePool/TransactionHistories");
			
			FileStream fs = new FileStream("Saves/ResourcePool/MasterHistory.dat", FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
			BinaryFileReader reader = new BinaryFileReader(new BinaryReader(fs));

			while (!reader.End())
				transactions.Add(new ResourceTransaction(reader));

			fs.Close();
			
			foreach (ResourceTransaction rt in transactions)
				Console.WriteLine(rt.ToString());
		}*/
    }
}