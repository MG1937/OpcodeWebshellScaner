using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace OpcodeWebshellScan.Utils
{
    class CmdUtils
    {
        /// <summary>
        /// Execute system command
        /// 执行系统命令
        /// </summary>
        /// <param name="exe">the file want to execute</param>
        /// <param name="arg">set arguments</param>
        /// <param name="timeout">set Command Timeout</param>
        /// <returns></returns>
        public static string getExecOutput(string exe,string arg=null,int timeout=4000) {
            string result = "Error";
            try
            {
                bool timeOut = false;
                bool isDone = false;
                Process p = new Process();
                p.StartInfo.FileName = exe;
                p.StartInfo.Arguments = arg;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;
                p.Start();

                new Thread(new ThreadStart(()=> {
                    Thread.Sleep(timeout);
                    if (!isDone)
                    {
                        timeOut = true;
                        p.Kill();
                    }
                })).Start();
                
                p.StandardInput.AutoFlush = true;

                string strOuput = p.StandardError.ReadLine();
                result = "";
                while ((strOuput) != null)
                {
                    Console.WriteLine(strOuput);
                    result += (strOuput) + "\n";
                    strOuput = p.StandardError.ReadLine();
                    //if (strOuput != null) result += "\n";
                }

                strOuput = p.StandardOutput.ReadLine();
                while ((strOuput) != null) {
                    //Console.WriteLine(strOuput + " :: " + strOuput.Length);
                    result += (strOuput);
                    strOuput = p.StandardOutput.ReadLine();
                    if (strOuput != null) result += "\n";
                }

                p.Close();
                isDone = true;
                return ((timeOut&&result.Equals(""))?"TimeOut":result);
            }
            catch (Exception)
            {
                return result;
            }
        }
    }
}
