using System;
using System.Collections.Generic;
using System.Text;

namespace OpcodeWebshellScan.Utils
{
    class HandlerUtils
    {
        public static int TMP_FUNC_EXP = 0x00000001;

        public static int TMP_NEWOBJ_EXP = 0x00000002;

        /// <summary>
        /// 目标字符串中是否仍包含临时表达式
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static int containsTmpExp(string obj)
        {
            int tmp = 0;
            if (obj.Contains("${tmp_func")) tmp |= TMP_FUNC_EXP;
            if (obj.Contains("${tmp_newobj")) tmp |= TMP_NEWOBJ_EXP;
            return tmp;
        }

        public static bool containsFuncExp(int tmpNum)
        {
            return (tmpNum & TMP_FUNC_EXP) != 0;
        }

        public static bool containsTmpNewObjExp(int tmpNum)
        {
            return (tmpNum & TMP_NEWOBJ_EXP) != 0;
        }
    }
}
