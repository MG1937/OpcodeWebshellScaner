using System;
using System.Collections.Generic;
using System.Text;

namespace OpcodeWebshellScan.OpcodeHandler.Handlers.OpcodeAnalyse
{
    /// <summary>
    /// 逻辑类的操作码无论如何,
    /// 最终的产生的可能只有true或false.
    /// 或许对这些操作码的模拟根本没有必要?
    /// 20220204
    /// </summary>
    class LogicOpcodeHandler : BaseOpcodeHandler
    {
        public void handleZendOpCode(ZendOpArray opArray, AnalyseHandler handler)
        {
            string reg_or_var = opArray.RESULT;
            string var1 = opArray.VAR1;
            string var2 = opArray.VAR2;

            string result = "";

            switch (opArray.OPCODE_NAME)
            {
                case "BOOL_NOT":
                    result = "!" + var1;
                    break;
                case "IS_IDENTICAL":
                    result = var1 + "===" + var2;
                    break;
                case "IS_NOT_IDENTICAL":
                    result = var1 + "!==" + var2;
                    break;
                case "IS_EQUAL":
                    result = var1 + "==" + var2;
                    break;
                case "IS_NOT_EQUAL":
                    result = var1 + "!=" + var2;
                    break;
                case "IS_SMALLER":
                    result = var1 + "<" + var2;
                    break;
                case "IS_SMALLER_OR_EQUAL":
                    result = var1 + "<=" + var2;
                    break;
            }

            if (BaseOpcodeHandler.isRegister(reg_or_var)) handler.registerSaver.saveRegister(reg_or_var, "(" + result + ")");
            else handler.varSaver.saveVar(reg_or_var, "(" + result + ")", opArray.FUNC_NAME, opArray.CLAZZ_NAME);
        }
    }
}
