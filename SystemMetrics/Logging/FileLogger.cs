using System;
using System.IO;

namespace SystemMetrics.Util
{
    public class FileLogger
    {
        public void Log(string Message, Level level)
        {
            Message = "[" + DateTime.UtcNow.ToString("MM-dd-yyyy HH:mm") + " - " + level.ToString() + "] " + Message;
            string path = AppDomain.CurrentDomain.BaseDirectory + "\\Logs";
            
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string filepath = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\ServiceLog_" + DateTime.Now.Date.ToShortDateString().Replace('/', '_') + ".log";
            if (!File.Exists(filepath))
            {
                // Create a file to write to.   
                using (StreamWriter sw = File.CreateText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(filepath))
                {
                    sw.WriteLine(Message);
                }
            }
        }
    }
}
