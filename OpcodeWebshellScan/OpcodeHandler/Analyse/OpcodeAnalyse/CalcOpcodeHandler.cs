using OpcodeWebshellScan.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcodeWebshellScan.OpcodeHandler.Analyse.OpcodeAnalyse
{
    class CalcOpcodeHandler : BaseOpcodeHandler
    {
        public void handleZendOpCode(ZendOpArray opArray, AnalyseHandler handler)
        {
            string calc = "";

            string reg_or_var = opArray.RESULT;
            string var1 = opArray.VAR1;
            string var2 = opArray.VAR2;

            bool var1notRegOrVar = false;
            bool var2notRegOrVar = false;

            switch (opArray.OPCODE_NAME)
            {
                case "ADD":
                    calc = "+";
                    break;
                case "SUB":
                    calc = "-";
                    break;
                case "MUL":
                    calc = "*";
                    break;
                case "DIV":
                    calc = "/";
                    break;
                case "MOD":
                    calc = "%";
                    break;
                case "SL":
                    calc = "<<";
                    break;
                case "SR":
                    calc = ">>";
                    break;
                case "BW_OR":
                    calc = "|";
                    break;
                case "BW_AND":
                    calc = "&";
                    break;
                case "BW_XOR":
                    calc = "^";
                    break;
                case "BOOL_XOR":
                    calc = "xor";
                    break;
                case "ASSIGN_ADD":
                    reg_or_var = var1;
                    calc = "+";
                    break;
                case "ASSIGN_SUB":
                    reg_or_var = var1;
                    calc = "-";
                    break;
                case "ASSIGN_MUL":
                    reg_or_var = var1;
                    calc = "*";
                    break;
                case "ASSIGN_DIV":
                    reg_or_var = var1;
                    calc = "/";
                    break;
                case "ASSIGN_MOD":
                    reg_or_var = var1;
                    calc = "%";
                    break;
                case "ASSIGN_SL":
                    reg_or_var = var1;
                    calc = "<<";
                    break;
                case "ASSIGN_SR":
                    reg_or_var = var1;
                    calc = ">>";
                    break;
                case "POST_INC":
                case "PRE_INC":
                    calc = "+";
                    var2 = "1";
                    var2notRegOrVar = true;
                    break;
                case "POST_DEC":
                case "PRE_DEC":
                    calc = "-";
                    var2 = "1";
                    var2notRegOrVar = true;
                    break;
            }

            //TODO:以下代码只是临时性的,仍需要进一步优化和修改 
            //20220204

            bool is_source = false;

            /*
             * 获取var1所有可能值
             */
            List<string> var1s = new List<string>();
            if (BaseOpcodeHandler.isRegister(var1))
            {
                var1s = handler.registerSaver.getRegister(var1);
                is_source = is_source ? true : handler.registerSaver.isSource(var1);
                if (var1s.Count == 0) var1s.Add("0");
            }
            else
            {
                List<string> tmp = handler.varSaver.getVar(var1, opArray.FUNC_NAME, opArray.CLAZZ_NAME).RESULTS;
                if (tmp == null)
                {
                    tmp = new List<string>();
                    tmp.Add("0");
                    tmp.Add(var1);
                }
                else
                {
                    is_source = is_source ? true : handler.varSaver.isSource(var1, opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                }
                var1s.AddRange(tmp);
            }

            /*
             * 获取var2所有可能值
             */
            List<string> var2s = new List<string>();
            if (var2notRegOrVar)
            {
                var2s.Add(var2);
            }
            else
            {
                if (BaseOpcodeHandler.isRegister(var2))
                {
                    var2s = handler.registerSaver.getRegister(var2);
                    is_source = is_source ? true : handler.registerSaver.isSource(var2);
                    if (var2s.Count == 0) var1s.Add("0");
                }
                else
                {
                    List<string> tmp = handler.varSaver.getVar(var2, opArray.FUNC_NAME, opArray.CLAZZ_NAME).RESULTS;
                    if (tmp == null)
                    {
                        tmp = new List<string>();
                        tmp.Add("0");
                        tmp.Add(var2);
                    }
                    else
                    {
                        is_source = is_source ? true : handler.varSaver.isSource(var2, opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                    }

                    var2s.AddRange(tmp);
                }
            }

            /*
             * 产生并储存所有可能
             */
            foreach (string v1 in var1s) 
            {
                foreach (string v2 in var2s)
                {
                    string tmp = "(" + v1 + " " + calc + " " + v2 + ")";
                    string exec = PhpUtils.getExecOutput("echo " + tmp + ";");
                    if (exec.IndexOf("error") != -1)
                    {
                        BaseOpcodeHandler.saveResultAnyway(reg_or_var, tmp, handler, opArray, is_source);
                    }
                    else
                    {
                        BaseOpcodeHandler.saveResultAnyway(reg_or_var, exec, handler, opArray, is_source);
                    }
                }
            }


            
        }
    }
}
