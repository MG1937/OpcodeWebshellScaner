using OpcodeWebshellScan.OpcodeHandler.Analyse;
using OpcodeWebshellScan.Utils;
using System.Collections.Generic;

/*
 * 该命名空间下的模块原则上应模拟处理函数调用的问题
 * 即在模拟处理函数调用的同时判断Source与Sink是否结合为Webshell
 * 至于为什么既然该命名空间下是处理函数调用的模块,
 * 但储存函数或其他关键数据的模块不在该命名空间下,
 * 而在AnalyseHandler类中,
 * 这是因为储存关键数据和处理关键数据为不同的职能,
 * 而且AnalyseHandler设计时的初衷就是为了储存关键数据的
 * 而该命名空间下的模块又是处理数据的
 */
namespace OpcodeWebshellScan.WebshellChecker
{
    public struct ClazzObject
    {
        public string CLAZZ_NAME;
        public List<List<string>> ARGS;
    }

    public struct Function
    {
        public List<string> TMP_CLZOBJS;//储存NEW操作码产生的临时表达式集

        //20220403 该成员存疑:没有必要存在
        public bool STATIC;//若方法为对象中的方法,则应该记录该方法是否被static修饰

        public string FUNC_NAME;
        /*
         * ARGS = ['arg1_1','arg1_2'...],['arg2_1','arg2_2'...]
         */
        public List<List<string>> ARGS_PARAMS;//参数串的集合
    }

    public class DoCallHandler
    {
        public static int TMP_DOCALL_NUM = 0;

        public static int LOOP_LIMIT = 10;//遍历深度限制

        private AnalyseHandler handler;

        public DoCallHandler(AnalyseHandler handler)
        {
            this.handler = handler;
        }

        public void saveSendArgs(List<string> args, string tmpNum)
        {
            if (tmpNum.StartsWith("${tmp_func"))
            {
                saveSendInitFunc(args, tmpNum);
            }
            else if (tmpNum.StartsWith("${tmp_clzobj"))
            {
                saveSendToClzObj(args, tmpNum);
            }
        }

        /* +================+
         * |New Object Block|
         * +================+
         */
        public Dictionary<string, List<ClazzObject>> tmp_clzobjs = new Dictionary<string, List<ClazzObject>>();

        public string getTmpClzObjNum()
        {
            TMP_DOCALL_NUM += 1;
            return "${tmp_clzobj" + TMP_DOCALL_NUM + "}";
        }

        public void createClazzObject(List<string> clzNames, string tmpClzObjNum)
        {
            List<ClazzObject> objs = new List<ClazzObject>();
            foreach (string n in clzNames)
            {
                //clzName可能的形式:TEXT TMP_CLZNUM
                ClazzObject clzObject = new ClazzObject();
                clzObject.CLAZZ_NAME = n;
                objs.Add(clzObject);
            }
            tmp_clzobjs[tmpClzObjNum] = objs;
        }

        /// <summary>
        /// 处理NEW操作码下的SEND类操作码
        /// </summary>
        /// <param name="args"></param>
        /// <param name="tmpClzObjNum"></param>
        public void saveSendToClzObj(List<string> args, string tmpClzObjNum)
        {
            List<ClazzObject> clzObjects = tmp_clzobjs.GetValueOrDefault(tmpClzObjNum, new List<ClazzObject>());
            for (int i = 0; i < clzObjects.Count; i++)
            {
                ClazzObject clzObject = clzObjects[i];
                List<List<string>> tmp_args = clzObject.ARGS == null ? new List<List<string>>() : clzObject.ARGS;
                tmp_args.Add(args);
                clzObject.ARGS = tmp_args;
                clzObjects[i] = clzObject;
            }
            tmp_clzobjs[tmpClzObjNum] = clzObjects;
        }

        public List<string> getClzObjClazzes(string tmpClzObjNum)
        {
            //可能存在的形式:TEXT FETCH_CONSTANT {TMP_FUNCNUM/NEW(__toString)}
            List<string> clzNames = new List<string>();
            List<ClazzObject> clzObjs = tmp_clzobjs.GetValueOrDefault(tmpClzObjNum, new List<ClazzObject>());
            foreach (ClazzObject clz in clzObjs)
            {
                int type = HandlerUtils.containsTmpExp(clz.CLAZZ_NAME);
                if (HandlerUtils.containsFuncExp(type))
                {
                    //存在临时函数表达式或表达式与明文混合
                    foreach (string name in getAllFuncReturnFromTample(clz.CLAZZ_NAME))
                    {
                        //MAY HAVE BUG??20220411
                        string tmp = HandlerUtils.formatStrByPhp(name);
                        if(tmp != null) clzNames.Add(tmp);
                    }
                }
                else if (HandlerUtils.containsTmpClzObjExp(type))
                {
                    clzNames.AddRange(getClzObjClazzes(clz.CLAZZ_NAME));
                }
                else
                {
                    //默认情况
                    clzNames.Add(clz.CLAZZ_NAME);
                }
            }

            return clzNames;
        }

        /* 
         * +==============+
         * |Function Block|
         * +==============+
         */

        //储存临时表达式可能的函数集
        public Dictionary<string, List<Function>> tmp_funcs = new Dictionary<string, List<Function>>();

        //储存方法静态解释模拟的结果集
        public Dictionary<string, List<string>> tmp_func_returns = new Dictionary<string, List<string>>();

        /// <summary>
        /// 获取临时的函数表达式
        /// </summary>
        /// <returns></returns>
        public string getTmpFuncNum()
        {
            TMP_DOCALL_NUM += 1;
            return "${tmp_func" + TMP_DOCALL_NUM + "}";
        }

        public void createInitMethod(List<string> tmpObjNums, List<string> funcNames, string tmpFuncNum, bool is_static = false)
        {
            List<Function> functions = new List<Function>();
            foreach (string f in funcNames)
            {
                Function function = new Function();
                function.FUNC_NAME = f;
                function.STATIC = is_static;
                function.TMP_CLZOBJS = tmpObjNums;
                functions.Add(function);
            }
            tmp_funcs[tmpFuncNum] = functions;
        }

        public void createInitFunc(List<string> funcNames, string tmpFuncNum)
        {
            List<Function> functions = new List<Function>();
            foreach (string f in funcNames)
            {
                Function function = new Function();
                function.FUNC_NAME = f;
                functions.Add(function);
            }
            tmp_funcs[tmpFuncNum] = functions;
        }

        /// <summary>
        /// INIT_FCALL...
        /// SEND_VAR...
        /// </summary>
        /// <param name="funcName"></param>
        /// <param name="args"></param>
        public void saveSendInitFunc(List<string> args, string tmpFuncNum)
        {
            List<Function> functions = tmp_funcs.GetValueOrDefault(tmpFuncNum, new List<Function>());
            for (int i = 0; i < functions.Count; i++)
            {
                Function function = functions[i];
                List<List<string>> tmp_args = function.ARGS_PARAMS == null ? new List<List<string>>() : function.ARGS_PARAMS;
                tmp_args.Add(args);
                function.ARGS_PARAMS = tmp_args;
                functions[i] = function;
            }
            tmp_funcs[tmpFuncNum] = functions;
        }

        /*
         * TODO:
         * getFuncReturns函数的部分职能可能与getAllFuncReturnFromTample函数的职能存在冲突.
         * getFuncReturns函数的搜索深度原则上为MAX,但是不处理tmpFuncNum以外的表达式.
         * getAllFuncReturnFromTample函数的搜索深度原则上应为1,但可处理tmpFuncNum与text混合的表达式.
         * 以上两个函数应为互补关系,但在实际编写过程中getAllFuncReturnFromTample函数的搜索职能逐渐趋向于深度MAX,
         * 但由于getFuncReturns函数已在搜索职能上实现MAX,这或许会让整个表达式的处理过程经历多个搜索深度为MAX的流程,
         * 严重拖慢了整个处理流程,需要优化
         * 20220411
         */

        /// <summary>
        /// 获取指定临时函数表达式的所有返回结果
        /// 搜索深度:MAX
        /// </summary>
        /// <param name="tmpFuncNum"></param>
        /// <returns></returns>
        public List<string> getFuncReturns(string tmpFuncNum, Function ex_function = new Function(), Func ex_func = new Func())
        {
            /*
             * ex_function为上一层的Function对象
             * ex_func为上一层的Func对象
             * 这两个对象主要用于参数传播
             */
            if (tmp_func_returns.ContainsKey(tmpFuncNum) && ex_function.ARGS_PARAMS == null) return tmp_func_returns[tmpFuncNum];
            Function t_ex_function = new Function();
            Func t_ex_func = new Func();

            if (ex_function.ARGS_PARAMS != null)
            {
                t_ex_function.ARGS_PARAMS = new List<List<string>>();
                //ext_function.ARGS.ForEach(s => t_ext_function.ARGS.Add(s));
                t_ex_func.ARGS = new List<string>();
                //arg_func.ARGS.ForEach(s => t_arg_func.ARGS.Add(s));
            }

            List<string> returnResults = new List<string>();
            foreach (Function function in tmp_funcs[tmpFuncNum])
            {
                List<string> funcNames = new List<string>();
                string tmpfuncName = PhpUtils.getExecOutput("echo (" + function.FUNC_NAME + ");");
                if (!tmpfuncName.Contains("error"))
                {
                    funcNames.Add(tmpfuncName);
                }
                else
                {
                    tmpfuncName = function.FUNC_NAME;
                    /*
                     * 获取完整的函数名集
                     */
                    List<string> tmpF = getAllFuncReturnFromTample(tmpfuncName);
                    if (tmpF.Count == 0) continue;
                    List<string> temp = new List<string>();
                    int loop = 0;

                    do
                    {
                        temp.ForEach(s => tmpF.Add(s));
                        temp.Clear();
                        tmpF.ForEach(s =>
                        {
                            string tmp = PhpUtils.getExecOutput("echo (" + s + ");");
                            int tmpNum = 0;
                            if ((tmp.Contains("error")) && ((tmpNum = HandlerUtils.containsTmpExp(s)) != 0))
                            {
                                //若字符串无法被echo正确输出且仍存在临时表达式时
                                //则认为字符串仍需要进行进一步处理
                                if (HandlerUtils.containsFuncExp(tmpNum))
                                {
                                    temp.AddRange(getAllFuncReturnFromTample(s));
                                }
                            }
                            else if (!tmp.Contains("error"))
                            {
                                funcNames.Add(tmp);
                            }
                            else
                            {
                                funcNames.Add(s);
                            }
                        });
                        tmpF.Clear();
                        loop += 1;
                    }
                    while (temp.Count != 0 && loop < LOOP_LIMIT);
                }

                /*
                 * 针对INIT_METHOD_CALL类操作码具体操作的对象
                 */
                List<string> tmpobjs = new List<string>();
                if (function.TMP_CLZOBJS != null)
                {
                    foreach (string tmp in function.TMP_CLZOBJS)
                    {
                        int tmpNum = HandlerUtils.containsTmpExp(tmp);
                        if (HandlerUtils.containsTmpClzObjExp(tmpNum))
                        {
                            tmpobjs.Add(tmp);
                        }
                        else if (HandlerUtils.containsFuncExp(tmpNum))
                        {
                            foreach (string r in getFuncReturns(tmp))
                            {
                                if (HandlerUtils.containsTmpClzObjExp(HandlerUtils.containsTmpExp(r)))
                                {
                                    tmpobjs.Add(r);
                                }
                            }
                        }
                    }

                    /*
                     * 若当前处理的函数为静态修饰且为对象内函数,则认为仅需要收集对象名
                     * 而不需要处理传入对象的参数
                     */
                    //if (function.STATIC && newobjs.Count != 0)
                    //{
                    //    List<string> tempFuncs = new List<string>();
                    //    funcNames.ForEach(s =>
                    //    {
                    //        foreach (string t in newobjs)
                    //        {
                    //            foreach (NewObject n in tmp_newobjs[t])
                    //            {
                    //            }
                    //        }
                    //    });
                    //}
                }

                foreach (string funcName in funcNames)
                {
                    List<Func> funcs = new List<Func>();
                    if (function.TMP_CLZOBJS == null)
                    {
                        funcs = handler.funcSaver.getAllFuncByName(funcName);
                    }
                    else
                    {
                        //函数拥有对象时
                        foreach (string t in tmpobjs)
                        {
                            foreach (string c in getClzObjClazzes(t))
                            {
                                funcs.Add(handler.funcSaver.getCurrentFunc(funcName, c));
                            }
                        }
                    }

                    /*
                     * 当getAllFuncByName函数有相应返回Func对象集时
                     * 代表本地声明的方法集中拥有目标方法
                     * 若返回集合为空则认为目标方法为内置函数或根本没有声明该方法
                     */
                    if ((funcs.Count == 0) && (function.TMP_CLZOBJS == null))
                    {
                        int argIndex = 0;
                        List<string> execs = new List<string>();
                        List<string> tmpExecs = new List<string>();
                        string exec = funcName + "(";
                        execs.Add(exec);
                        if (function.ARGS_PARAMS != null)
                        {
                            foreach (List<string> args in (ex_function.ARGS_PARAMS == null) ? function.ARGS_PARAMS : ex_function.ARGS_PARAMS)
                            {
                                foreach (string e in execs)
                                {
                                    foreach (string arg in args)
                                    {
                                        foreach (string a in getAllFuncReturnFromTample(arg))
                                        {
                                            string tmp = e + ((argIndex == 0) ? "" : ",") + a;
                                            if (!tmpExecs.Contains(tmp)) tmpExecs.Add(tmp);
                                        }
                                    }
                                }
                                execs.Clear();
                                tmpExecs.ForEach(s => execs.Add(s));
                                tmpExecs.Clear();
                            }
                        }

                        foreach (string e in execs)
                        {
                            string texec = "'" + PhpUtils.getExecOutput("echo (" + e + "));") + "'";
                            if (!texec.Contains("error"))
                            {
                                if (!returnResults.Contains(texec)) returnResults.Add(texec);
                            }
                        }
                        continue;
                    }
                    /*
                     * 若获取的Func对象集为空,但TMP_NEWOBJS不为空(即DO_CALL的函数为某个对象内的方法)时
                     * 则认为该函数不存在或其对象为内置对象,但此处认为调用内置对象的函数没有任何意义.
                     */
                    else if ((funcs.Count == 0) && (function.TMP_CLZOBJS != null))
                    {
                        continue;
                    }

                    foreach (Func f in funcs)
                    {
                        int argIndex = 0;
                        List<string> tmpTamples = new List<string>();
                        f.RESULTS.ForEach(s => tmpTamples.Add(s));
                        List<string> tmpReturns = new List<string>();
                        if (f.ARGS == null)
                        {
                            //若没有传入参数则直接将已有的结果集写入returnResults
                            //TODO:将来仍须完善此处
                            tmpTamples.ForEach(s =>
                            {
                                if (!returnResults.Contains(s)) returnResults.Add(s);
                            });
                            continue;
                            //return tmpTamples;
                            //FixBug 20220224
                            //不要直接返回结果集,这将导致Function对象集在遍历完全前就导致处理结束
                        }

                        foreach (string arg in f.ARGS)
                        {
                            foreach (string tample in tmpTamples)
                            {
                                tmpReturns.Add(tample);
                                bool spreadArg = false;
                                if (!tample.Contains(arg))
                                {
                                    if (tample.Contains("${"))
                                    {
                                        spreadArg = true;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }

                                if (spreadArg && ex_function.ARGS_PARAMS == null)
                                {
                                    //需要传播参数但此时为传参链最底层时
                                    List<string> args = function.ARGS_PARAMS[argIndex];
                                    //fillReturns(tmpReturns, args, arg, function, f);

                                    tmpReturns.AddRange(getAllFuncReturnFromTample(tample, function, f));
                                    continue;
                                }
                                else if (spreadArg || ex_function.ARGS_PARAMS != null)
                                {
                                    //需要传播参数且在传参链中非底层
                                    List<string> arg_params = new List<string>();
                                    function.ARGS_PARAMS[argIndex].ForEach(a =>
                                    {
                                        //FixBug 20220408 修复未考虑参数为函数表达式的情况
                                        if (HandlerUtils.containsFuncExp(HandlerUtils.containsTmpExp(a)))
                                        {
                                            arg_params.AddRange(getAllFuncReturnFromTample(a));
                                        }
                                        else
                                        {
                                            arg_params.Add(a);
                                        }
                                    });//拷贝对象
                                    List<int> argNameIndexs = new List<int>();
                                    //探索此层参数是否含有上层参数的模板
                                    for (int i = 0; i < ex_func.ARGS.Count; i++)
                                    {
                                        foreach (string t in arg_params)
                                        {
                                            if (t.Contains(ex_func.ARGS[i]))
                                            {
                                                if (!argNameIndexs.Contains(i)) argNameIndexs.Add(i);
                                            }
                                        }
                                    }

                                    //若当前处理的Function对象的参数内存在上层参数,则代表参数需要传播
                                    if (argNameIndexs.Count != 0)
                                    {
                                        foreach (int index in argNameIndexs)
                                        {
                                            fillReturns(arg_params, ex_function.ARGS_PARAMS[index], ex_func.ARGS[index]);
                                        }
                                        arg_params.ForEach(s =>
                                        {
                                            while (t_ex_function.ARGS_PARAMS.Count < argIndex + 1)
                                            {
                                                t_ex_function.ARGS_PARAMS.Add(new List<string>());
                                            }
                                            if (!t_ex_function.ARGS_PARAMS[argIndex].Contains(s)) t_ex_function.ARGS_PARAMS[argIndex].Add(s);
                                        });
                                        //t_ext_function.ARGS[argIndex].AddRange(args);
                                        //args.AddRange(ext_function.ARGS[argIndex]);
                                        fillReturns(tmpReturns, arg_params, arg, t_ex_function, f);
                                    }
                                    else
                                    {
                                        foreach (string t in arg_params)
                                        {
                                            (getAllFuncReturnFromTample(t, ex_function, ex_func)).ForEach(s =>
                                            {
                                                if (!tmpReturns.Contains(s)) tmpReturns.Add(s);
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    List<string> args = function.ARGS_PARAMS[argIndex];
                                    fillReturns(tmpReturns, args, arg);
                                }

                                /*
                                if (arg_func.ARGS != null)
                                {
                                    fillReturns(tmpReturns, new List<string> { arg_func.ARGS[argIndex] }, arg, ext_function, arg_func);
                                }
                                else if (spreadArg && arg_func.ARGS == null)
                                {
                                    List<string> args = function.ARGS[argIndex];
                                    fillReturns(tmpReturns, args, arg, function, f);
                                    fillReturns(tmpReturns, args, arg, function, f);
                                }
                                else
                                {
                                    List<string> args = function.ARGS[argIndex];
                                    fillReturns(tmpReturns, args, arg);
                                }
                                */
                            }
                            tmpTamples.Clear();
                            tmpReturns.ForEach(s => tmpTamples.Add(s));
                            tmpReturns.Clear();
                            argIndex += 1;
                        }
                        tmpTamples.ForEach(s =>
                        {
                            if (!returnResults.Contains(s)) returnResults.Add(s);
                        });
                    }
                }

            }

            tmp_func_returns[tmpFuncNum] = returnResults;
            return returnResults;
        }



        /*
         * 将所有带有临时函数表达式的'模板'转为结果集
         */
        public List<string> getAllFuncReturnFromTample(string tample, Function extFunction = new Function(), Func extFunc = new Func())
        {
            //List<string> returns = new List<string>();
            List<string> tmps = new List<string>();
            List<string> tmpReturns = new List<string>();
            tmpReturns.Add(tample);
            foreach (string tmpFNum in tmp_funcs.Keys)
            {
                foreach (string tmpTample in tmpReturns)
                {
                    if (tmpTample.Contains(tmpFNum))
                    {
                        foreach (string r in getFuncReturns(tmpFNum, extFunction, extFunc))
                        {
                            string tmp = tmpTample.Replace(tmpFNum, r);
                            if (!tmps.Contains(tmp)) tmps.Add(tmp);
                        }
                    }
                    else
                    {
                        tmps.Add(tmpTample);
                    }
                }
                tmpReturns.Clear();
                tmps.ForEach(s => tmpReturns.Add(s));
                tmps.Clear();
            }
            return tmpReturns;
        }

        /// <summary>
        /// 填充参数到返回集模板中
        /// </summary>
        /// <param name="tamples"></param>
        /// <param name="args"></param>
        /// <param name="targetArg"></param>
        private void fillReturns(List<string> tamples, List<string> args, string targetArg, Function extFunction = new Function(), Func extFunc = new Func())
        {
            List<string> tmpTamples = new List<string>();
            foreach (string t in tamples)
            {
                foreach (string a in args)
                {
                    string tmp = t.Replace(targetArg, a);
                    (getAllFuncReturnFromTample(tmp, extFunction, extFunc)).ForEach(s =>
                      {
                          if (!tmpTamples.Contains(s)) tmpTamples.Add(s);
                      });
                }
            }

            //foreach (string t in tmpTamples)
            //{
            //    if (!tamples.Contains(t)) tamples.Add(t);
            //}
            tamples.Clear();
            tamples.AddRange(tmpTamples);
        }
    }
}
