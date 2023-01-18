using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dlib_Sample_Project
{
    class loger
    {
        public static string PathSeparator = "\\";
        public static void WriteLog(string log)
        {
            try
            {
                string text = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Dlib_Sample_Project";
                string str = PathSeparator + DateTime.Now.ToString("yyyyMMddHH") + ".txt";
                if (!Directory.Exists(text))
                {
                    Directory.CreateDirectory(text);
                }
                File.AppendAllText(text + str, Environment.NewLine + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + log);
            }
            catch
            {
            }
        }

        public static void WriteLog(string logtype, string log)
        {
            try
            {
                if (logtype == "sts")
                {
                    string text = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Dlib_Sample_Project";
                    string str = PathSeparator + DateTime.Now.ToString("yyyyMMddHH") + ".txt";
                    if (!Directory.Exists(text))
                    {
                        Directory.CreateDirectory(text);
                    }
                    File.AppendAllText(text + str, Environment.NewLine + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + "Status" + "\t" + log);
                    return;
                }

                if (logtype == "err")
                {
                    string text = AppDomain.CurrentDomain.BaseDirectory + "\\Logs\\Dlib_Sample_Project";
                    string str = PathSeparator + DateTime.Now.ToString("yyyyMMddHH") + ".txt";
                    if (!Directory.Exists(text))
                    {
                        Directory.CreateDirectory(text);
                    }
                    File.AppendAllText(text + str, Environment.NewLine + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "\t" + "Error" + "\t" + log);
                    return;
                }

            }
            catch
            {
            }
        }
    }
}
