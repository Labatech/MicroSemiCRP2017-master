using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicrosEmiCRP
{
    static class WriteLogger
    {

        private static readonly object _syncObject = new object();
        static bool FileCreated = false;
        static int z = 0;
        public static void WriteLog(string strLog)
        {
            lock (_syncObject)
            {
                StreamWriter log;
                FileStream fileStream = null;
                DirectoryInfo logDirInfo = null;
                FileInfo logFileInfo;



                string logFilePath = "AxonLab\\Log\\";
                logFilePath = logFilePath + "Log-" + System.DateTime.Today.ToString("dd-MM-yyyy") + "_" + z + "." + "txt";
                logFileInfo = new FileInfo(logFilePath);
                logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);

                if (!logDirInfo.Exists)
                {
                    logDirInfo.Create();
                }

                while (!(!logFileInfo.Exists || FileCreated))
                {
                    z++;
                    logFilePath = "AxonLab\\Log\\";
                    logFilePath = logFilePath + "Log-" + System.DateTime.Today.ToString("dd-MM-yyyy") + "_" + z + "." + "txt";
                    logFileInfo = new FileInfo(logFilePath);
                }
                if (FileCreated)
                {
                    fileStream = new FileStream(logFilePath, FileMode.Append);
                }
                else
                {
                    fileStream = logFileInfo.Create();
                    FileCreated = true;
                }


                log = new StreamWriter(fileStream);
                log.WriteLine(strLog);
                log.Close();
            }
        }

        
    }
}
