using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OpcodeWebshellScan.OpcodeHandler.Analyse.OpcodeAnalyse
{
    interface BaseOpcodeHandler
    {
        public static List<string> source = new List<string> {"_GET","_POST","_COOKIE","_REQUEST","_FILE","_SESSION"};

        void handleZendOpCode(ZendOpArray opArray, AnalyseHandler handler);

        /// <summary>
        /// 通常用以判断result对象是否为寄存器
        /// </summary>
        /// <param name="objName"></param>
        /// <returns></returns>
        public static bool isRegister(string objName) 
        {
            /*
             * 寄存器样式:~1,~2,@1,@2
             */
            try
            {
                if (char.ToString(objName[0]) == "~" || char.ToString(objName[0]) == "@") return true;
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }

        public static bool isVar(string objName)
        {
            try
            {
                if (char.ToString(objName[0]) == "$")
                {
                    return Regex.IsMatch(objName.Substring(1), "^([a-zA-z0-9_])+$");
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void saveResultAnyway(string reg_or_var, string result, AnalyseHandler handler, ZendOpArray opArray, bool is_source = false)
        {
            if (BaseOpcodeHandler.isRegister(reg_or_var))
            {
                handler.registerSaver.saveRegister(reg_or_var, (result));
                if (is_source) handler.registerSaver.setSource(reg_or_var);
            }
            else
            {
                handler.varSaver.saveVar(reg_or_var, (result), opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                if (is_source||cotainsSourcePoint(result))
                {
                    handler.varSaver.setSource(reg_or_var, opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                }
            }
        }

        /// <summary>
        /// 判断储存内容是否为潜在的可控点
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool cotainsSourcePoint(string value) 
        {
            foreach (string s in source)
            {
                if (value.IndexOf(s) != -1) return true;
            }
            return false;
        }
    }
}
