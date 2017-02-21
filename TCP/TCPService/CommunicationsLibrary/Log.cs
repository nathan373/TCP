using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;


namespace CommunicationsLibrary
{
    public class Log : ILog
    {
        private bool consoleWatch;
        private object lockerForLog = new object();
        internal string saveFile;
        StreamWriter writer;

        public Log(bool consoleWatch)
        {
            this.consoleWatch = consoleWatch;
            //We create a new log file every time we run the app.
            this.saveFile = GetSaveFileName();
            
        }

        private string GetSaveFileName()
        {
            string saveDirectory = @"c:\Temp\";

            try
            {
                if (Directory.Exists(saveDirectory) == false)
                {
                    Directory.CreateDirectory(saveDirectory);
                }
            }
            catch
            {
                Console.WriteLine("Could not create save directory for log. See TestFileWriter.cs."); Console.ReadLine();
            }

            string assemblyFullName = Assembly.GetExecutingAssembly().FullName;
            int index = assemblyFullName.IndexOf(',');
            string saveFile = assemblyFullName.Substring(0, index);
            string dt = DateTime.Now.ToString("yyMMddHHmmss");
            //Save directory is created in ConfigFileHandler
            saveFile = saveDirectory + saveFile + "-" + dt + ".txt";
            return saveFile;
        }

        public void WriteLine(string lineToWrite)
        {
            if (consoleWatch == true)
            {
                Console.WriteLine(lineToWrite);
            }

            lock (this.lockerForLog)
            {
                using (writer = File.AppendText(this.saveFile))
                {
                    writer.WriteLine("[" + DateTime.Now.ToString() + "] " + lineToWrite);
                }
            }
        }

        public void Close()
        {
            using (writer = File.AppendText(this.saveFile))
            {
                writer.WriteLine("Closed");
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("This session was logged to " + saveFile);
            Console.WriteLine();
            Console.WriteLine();
        }

    }
}
