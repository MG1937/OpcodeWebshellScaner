using OpcodeWebshellScan.Utils;
using System.Collections.Generic;

namespace OpcodeWebshellScan.OpcodeHandler.Analyse.OpcodeAnalyse
{
    class OperateOpcodeHandler : BaseOpcodeHandler
    {
        public void handleZendOpCode(ZendOpArray opArray, AnalyseHandler handler)
        {
            string save_target = opArray.RESULT;
            string var1 = opArray.VAR1;
            string var2 = opArray.VAR2;
            bool is_source = false;//是否标记为污点

            List<string> save_values = new List<string>();

            /*
             * 根据操作码具体对寄存器和欲操作的参数进行处理
             * 最终由handler的寄存器/变量储存器储存最终结果
             */
            switch (opArray.OPCODE_NAME)
            {
                case "QM_ASSIGN":
                    save_values = getValueAnyway(var1, handler, opArray);
                    is_source = isSource(var1, handler, opArray);
                    break;
                case "ASSIGN_CONCAT":
                    {
                        save_target = var1;
                        List<string> v1s = getValueAnyway(var1, handler, opArray);
                        if (v1s.Count == 0) v1s.Add("");
                        List<string> v2s = getValueAnyway(var2, handler, opArray);
                        if (v2s.Count == 0) v2s.Add("");
                        foreach (string v1 in v1s)
                        {
                            foreach (string v2 in v2s)
                            {
                                save_values.Add("(" + v1 + ").(" + v2 + ")");
                            }
                        }
                        break;
                    }
                case "ASSIGN":
                    save_values = getValueAnyway(var2, handler, opArray);
                    save_target = var1;
                    is_source = isSource(var2, handler, opArray);
                    break;
                case "BOOL":
                    if (var1.Equals("<true>"))
                    {
                        save_values.Add("1");
                        break;
                    }
                    else if (var1.Equals("<false>"))
                    {
                        save_values.Add("");
                        break;
                    }
                    break;
                case "ROPE_INIT":
                    save_values = getValueAnyway(var1, handler, opArray);
                    is_source = isSource(var1, handler, opArray);
                    break;
                case "ROPE_ADD":
                case "ROPE_END":
                    {
                        List<string> v1s = getValueAnyway(var1, handler, opArray);
                        is_source = isSource(var1, handler, opArray);

                        List<string> v2s = getValueAnyway(var2, handler, opArray);
                        is_source = is_source ? true : isSource(var2, handler, opArray);

                        foreach (string v1 in v1s)
                        {
                            foreach (string v2 in v2s)
                            {
                                save_values.Add("(" + v1 + ")" + "." + "(" + v2 + ")");
                            }
                        }
                        break;
                    }
                case "CAST":
                case "CLONE":
                    {
                        save_values.AddRange(getValueAnyway(var1, handler, opArray));
                        is_source = isSource(var1, handler, opArray);
                        break;
                    }
                case "CONCAT":
                case "FAST_CONCAT":
                    {
                        List<string> v1s = getValueAnyway(var1, handler, opArray);
                        is_source = isSource(var1, handler, opArray);

                        List<string> v2s = getValueAnyway(var2, handler, opArray);
                        is_source = is_source ? true : isSource(var2, handler, opArray);

                        foreach (string v1 in v1s)
                        {
                            foreach (string v2 in v2s)
                            {
                                save_values.Add("(" + v1 + ")" + "." + "(" + v2 + ")");
                            }
                        }
                        break;
                    }
                case "FETCH_CLASS":
                case "NEW":
                    {
                        //需要考虑到构造函数的情况
                        List<string> clazzs = getValueAnyway(var1, handler, opArray);
                        string tmpClzObjNum = handler.doCallHandler.getTmpClzObjNum();
                        handler.doCallHandler.createClazzObject(clazzs, tmpClzObjNum);
                        if (isSource(var1, handler, opArray)) tmpClzObjNum += "_Source";
                        handler.doCallRecord.Add(tmpClzObjNum);
                        save_values.Add(tmpClzObjNum);
                        break;
                    }
                //TODO:case "INIT_STATIC_METHOD_CALL":
                case "INIT_METHOD_CALL":
                    {
                        //TODO:对象内调用本对象函数的情况仍需要改进!!!!
                        List<string> objs = var2.Equals("") ? new List<string> { "${tmp_clzobj_this}" } : getValueAnyway(var1, handler, opArray);
                        List<string> fcallNames = getValueAnyway(var2.Equals("") ? var1 : var2, handler, opArray);
                        string tmpFcallNum = handler.doCallHandler.getTmpFuncNum();
                        handler.doCallHandler.createInitMethod(objs, fcallNames, tmpFcallNum);

                        string source_mark = "";
                        if (isSource(var1, handler, opArray) || isSource(var2, handler, opArray)) source_mark = "_Source";
                        if (source_mark.Equals(""))
                        {
                            foreach (string f in fcallNames)
                            {
                                if (handler.funcSaver.isSource(f))
                                {
                                    source_mark = "_Source";
                                    break;
                                }
                            }
                        }

                        handler.doCallRecord.Add(tmpFcallNum + source_mark);
                        return;
                    }
                case "INIT_DYNAMIC_CALL":
                case "INIT_FCALL_BY_NAME":
                case "INIT_FCALL":
                    {
                        List<string> fcallNames = getValueAnyway(var1, handler, opArray);
                        string tmpFcallNum = handler.doCallHandler.getTmpFuncNum();
                        handler.doCallHandler.createInitFunc(fcallNames, tmpFcallNum);

                        string source_mark = "";
                        if (isSource(var1, handler, opArray)) source_mark = "_Source";
                        if (source_mark.Equals(""))
                        {
                            foreach (string f in fcallNames)
                            {
                                if (handler.funcSaver.isSource(f))
                                {
                                    source_mark = "_Source";
                                    break;
                                }
                            }
                        }

                        handler.doCallRecord.Add(tmpFcallNum + source_mark);
                        return;
                    }
                case "SEND_VAR_EX":
                case "SEND_VAR":
                case "SEND_VAL":
                case "SEND_VAL_EX":
                case "SEND_VAR_NO_REF_EX":
                case "SEND_REF":
                    {
                        //获取最后一次预调用的临时函数名
                        string tmpFcallNum = handler.doCallRecord[handler.doCallRecord.Count - 1];
                        List<string> args = getValueAnyway(var1, handler, opArray);
                        handler.doCallHandler.saveSendArgs(args, tmpFcallNum);
                        if (isSource(var1, handler, opArray))//判断传参是否可控
                        {
                            //TODO:可控点判断!!!!
                            tmpFcallNum += "_Source";//标记可控
                            handler.doCallRecord.RemoveAt(handler.doCallRecord.Count - 1);
                            handler.doCallRecord.Add(tmpFcallNum);
                        }
                        return;
                    }
                case "DO_FCALL":
                case "DO_FCALL_BY_NAME":
                case "DO_UCALL":
                case "DO_ICALL":
                case "DO_UCALL_BY_NAME":
                    {
                        string tmpFcallNum = handler.doCallRecord[handler.doCallRecord.Count - 1];
                        if (!opArray.RESULT.Equals(""))
                        {
                            BaseOpcodeHandler.saveResultAnyway(opArray.RESULT, tmpFcallNum.Replace("_Source", ""), handler, opArray, (tmpFcallNum.EndsWith("_Source")));
                        }
                        handler.doCallRecord.RemoveAt(handler.doCallRecord.Count - 1);
                        return;
                    }
                case "RECV":
                    {
                        string arg_name = "{" + opArray.RESULT + "}";
                        handler.funcSaver.setArgName(arg_name, opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                        BaseOpcodeHandler.saveResultAnyway(opArray.RESULT, arg_name, handler, opArray);
                        return;
                    }
                case "RECV_INIT":
                    {
                        string arg_name = "{" + opArray.RESULT + "}";
                        handler.funcSaver.setArgName(arg_name, opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                        BaseOpcodeHandler.saveResultAnyway(opArray.RESULT, arg_name, handler, opArray);
                        save_values.Add(var1);
                        break;
                    }
                case "RETURN"://此处有必要对是否可控进行判断
                    {
                        if (var1.Equals("null"))
                        {
                            handler.funcSaver.saveResult("null", opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                            return;
                        }
                        List<string> returns = getValueAnyway(var1, handler, opArray);
                        foreach (string r in returns)
                        {
                            handler.funcSaver.saveResult(r, opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                        }
                        is_source = is_source ? true : isSource(var1, handler, opArray);
                        if (is_source) handler.funcSaver.setSource(opArray.FUNC_NAME, opArray.CLAZZ_NAME);
                        return;
                    }
            }

            foreach (string r in save_values)
            {
                string exec = HandlerUtils.formatStrByPhp(r);
                if (exec == null)
                {
                    BaseOpcodeHandler.saveResultAnyway(save_target, r, handler, opArray, is_source);
                }
                else
                {
                    /*
                     * 若执行成功,那么执行的结果必然为一串可显示的字符
                     */
                    BaseOpcodeHandler.saveResultAnyway(save_target, "'" + exec + "'", handler, opArray, is_source ? true : BaseOpcodeHandler.cotainsSourcePoint(exec));
                }
            }
        }

        private bool isSource(string objName, AnalyseHandler handler, ZendOpArray opArray)
        {
            if (objName.StartsWith("'") && objName.EndsWith("'")) return BaseOpcodeHandler.cotainsSourcePoint(objName);

            if (BaseOpcodeHandler.isRegister(objName)) return handler.registerSaver.isSource(objName);

            if (BaseOpcodeHandler.isVar(objName)) return handler.varSaver.isSource(objName, opArray.FUNC_NAME, opArray.CLAZZ_NAME);

            return false;
        }

        /// <summary>
        /// 获取对象的值,无论对象是寄存器,变量还是对象本身
        /// </summary>
        /// <param name="objName"></param>
        /// <returns></returns>
        private List<string> getValueAnyway(string objName, AnalyseHandler handler, ZendOpArray opArray)
        {
            List<string> tmp = new List<string>();

            if ((objName.StartsWith("'") && objName.EndsWith("'")) || objName.StartsWith("${tmp_func") || objName.StartsWith("{$"))
            {
                tmp.Add(objName);
                return tmp;
            }

            if (BaseOpcodeHandler.isVar(objName))
            {
                tmp = handler.varSaver.getVar(objName, opArray.FUNC_NAME, opArray.CLAZZ_NAME).RESULTS;
                if (tmp == null)
                {
                    tmp = new List<string>();
                    tmp.Add("0");
                }
                return tmp;
            }
            else if (BaseOpcodeHandler.isRegister(objName))
            {
                tmp = handler.registerSaver.getRegister(objName);
                if (tmp.Count == 0) tmp.Add("0");
                return tmp;
            }
            tmp.Add(objName);
            return tmp;
        }
    }
}
