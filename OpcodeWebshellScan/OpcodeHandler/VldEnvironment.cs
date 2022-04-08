using System;
using System.Collections.Generic;
using System.Text;

namespace OpcodeWebshellScan.OpcodeHandler
{
    /// <summary>
    /// 初步解析Vld输出的Opcode
    /// </summary>
    class VldEnvironment
    {
        public static string PHPDBG_PATH { set; get; }
        public static string PHP_PATH { set; get; }

        /// <summary>
        /// set Path of phpdbg
        /// 设置phpdbg路径
        /// </summary>
        /// <param name="path">path</param>
        public static void setPhpdbgPath(string path) {
            PHPDBG_PATH = path;
        }

        /// <summary>
        /// set Path of php
        /// 设置php路径
        /// </summary>
        /// <param name="path">path</param>
        public static void setPhpPath(string path) {
            PHP_PATH = path;
        }

        public static string getPhpdbgPath() {
            return PHPDBG_PATH;
        }

        public static string getPhpPath() {
            return PHP_PATH;
        }
    }
}
