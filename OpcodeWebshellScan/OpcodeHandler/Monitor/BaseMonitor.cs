using System;
using System.Collections.Generic;
using System.Text;

namespace OpcodeWebshellScan.OpcodeHandler.Monitor
{
    /// <summary>
    /// 监听管道
    /// 原则上用以处理表达式为数据,处于编译末期
    /// </summary>
    interface BaseMonitor
    {
        /// <summary>
        /// 处理输入的数据流
        /// </summary>
        /// <param name="data">具有标识性的数据,一般依此作为处理的依据</param>
        /// <param name="param">数据的附带流</param>
        /// <returns></returns>
        public int dataReceive(string data, params object[] param);
    }
}
