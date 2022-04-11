using OpcodeWebshellScan.OpcodeHandler.Analyse.OpcodeAnalyse;
using OpcodeWebshellScan.WebshellChecker;
using System;
using System.Collections.Generic;

namespace OpcodeWebshellScan.OpcodeHandler.Analyse
{
    /// <summary>
    /// 用以储存所有"变量"的结构体.
    /// 即使在操作码实际操作过程中控制的量为常量
    /// 由于PHP语言的特性,即使是常量也有可能在
    /// 执行流程被操纵的情况下使得常量有多个可能的值,
    /// 而这对于整个操作码分析流程来说,此时的常量成为了"变量"!
    /// </summary>
    public struct Var
    {
        public bool IS_SOURCE;//变量是否可控
        public string VAR_NAME;//变量名
        public List<string> RESULTS;//变量可能产生的结果集
    }

    /// <summary>
    /// 变量保存器
    /// </summary>
    public class VarSaver : Dictionary<string, Var>
    {
        /// <summary>
        /// 储存变量的值
        /// </summary>
        /// <param name="varName">变量名</param>
        /// <param name="belongFunc">变量隶属方法</param>
        /// <param name="result">变量赋值结果</param>
        /// <param name="belongClazz">变量隶属类</param>
        public void saveVar(string varName, string result, string belongFunc = "{main}", string belongClazz = "")
        {
            /*
             * E.G. classA::funcA::varA
             */
            string tmpName = belongClazz + "::" + belongFunc + "::" + varName;
            Var var = this.GetValueOrDefault(tmpName, new Var());
            List<string> results = var.RESULTS == null ? new List<string>() : var.RESULTS;

            if (results.Count > AnalyseHandler.MAX_RESULT_SAVE) return;//判断结果集的长度是否大于限制值
            if (results.Contains(result)) return;

            results.Add(result);
            var.VAR_NAME = varName;
            var.RESULTS = results;

            this[tmpName] = var;
        }

        /// <summary>
        /// 获取变量
        /// </summary>
        /// <param name="varName">变量名</param>
        /// <param name="belongFunc">变量隶属方法</param>
        /// <param name="belongClazz">变量隶属类</param>
        /// <returns></returns>
        public Var getVar(string varName, string belongFunc = "{main}", string belongClazz = "")
        {
            try
            {
                /*
                 * E.G. classA::funcA::varA
                 */
                string tmpName = belongClazz + "::" + belongFunc + "::" + varName;
                Var var = this.GetValueOrDefault(tmpName, new Var());
                return var;
            }
            catch (Exception)
            {
                return new Var();
            }
        }

        /// <summary>
        /// 认定一个变量为可控点
        /// 注意:该操作原则上是不可逆的!
        /// </summary>
        /// <param name="varName"></param>
        /// <param name="belongFunc"></param>
        /// <param name="belongClazz"></param>
        public void setSource(string varName, string belongFunc = "{main}", string belongClazz = "")
        {
            string tmpName = belongClazz + "::" + belongFunc + "::" + varName;
            Var var = this.GetValueOrDefault(tmpName, new Var());
            var.IS_SOURCE = true;
            this[tmpName] = var;
        }

        public bool isSource(string varName, string belongFunc = "{main}", string belongClazz = "")
        {
            string tmpName = belongClazz + "::" + belongFunc + "::" + varName;
            Var var = this.GetValueOrDefault(tmpName, new Var());
            return var.IS_SOURCE;
        }
    }

    public struct Register
    {
        public bool IS_SOURCE;//寄存器是否可控
        public string REG_NAME;//寄存器名称
        public List<string> RESULTS;//寄存器结果集
    }

    /// <summary>
    /// 临时寄存器保存器
    /// </summary>
    public class RegisterSaver : Dictionary<string, Register>
    {
        /*
         * 寄存器样式如下(以VLD输出的格式为例)
         * CONCAT ~1 $a,'b'
         * 那么~1则可以看作寄存器
         */

        /// <summary>
        /// 储存寄存器与其值
        /// 由于寄存器不同于变量
        /// 寄存器并没有隶属于某类或某方法之说
        /// 对于多次的操作码串处理,次间的寄存器都将是相互独立的
        /// </summary>
        /// <param name="registerName">寄存器名</param>
        /// <param name="value">寄存器值</param>
        public void saveRegister(string registerName, string value)
        {
            Register register = this.GetValueOrDefault(registerName, new Register());
            List<string> resutls = register.RESULTS == null ? new List<string>() : register.RESULTS;
            resutls.Add(value);
            register.RESULTS = resutls;
            register.REG_NAME = registerName;

            this[registerName] = register;
        }

        public List<string> getRegister(string registerName)
        {
            try
            {
                Register register = this.GetValueOrDefault(registerName, new Register());
                List<string> resutls = register.RESULTS == null ? new List<string>() : register.RESULTS;
                //寄存器被取出时则认为该寄存器此时应该被释放
                //通常认为在ZEND中寄存器不会被复用
                this.Remove(registerName);
                return resutls;
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public void setSource(string registerName)
        {
            Register register = this.GetValueOrDefault(registerName, new Register());
            register.IS_SOURCE = true;
            this[registerName] = register;
        }

        public bool isSource(string registerName)
        {
            Register register = this.GetValueOrDefault(registerName, new Register());
            return register.IS_SOURCE;
        }
    }

    public struct Func
    {
        public bool IS_SOURCE;//是否有潜在的可控点
        public string FUNC_NAME;//方法名
        public string CLAZZ_NAME;//隶属类
        public List<string> ARGS;//参数集
        public List<string> RESULTS;//结果集
    }

    /// <summary>
    /// 储存方法返回值
    /// 由于PHP语言的特性与方法执行过程中的高动态性
    /// 储存的返回值原则上应尽量储存可以预知的"常量"
    /// 而非夹杂其他不可预知的变量的值.
    /// Key:CLAZZ::FUNC
    /// </summary>
    public class FuncReturnSaver : Dictionary<string, Func>
    {
        //弃用?
        public List<Func> getAllFuncByName(string func_name)
        {
            List<Func> funcs = new List<Func>();
            foreach (string k in this.Keys)
            {
                if (this[k].FUNC_NAME.Equals(func_name))
                {
                    funcs.Add(this[k]);
                }
            }
            return funcs;
        }

        public Func getCurrentFunc(string func_name, string clazz_name = "")
        {
            return this[clazz_name + "::" + func_name];
        }

        public void setArgName(string arg_name, string func_name, string clazz_name = "")
        {
            string tmp = clazz_name + "::" + func_name;
            Func func = this.GetValueOrDefault(tmp, new Func());
            List<string> args = func.ARGS == null ? new List<string>() : func.ARGS;
            args.Add(arg_name);
            func.ARGS = args;
            this[tmp] = func;
        }

        public void saveResult(string result, string func_name, string clazz_name = "")
        {
            Func func = this.GetValueOrDefault(clazz_name + "::" + func_name, new Func());
            List<string> results = func.RESULTS == null ? new List<string>() : func.RESULTS;
            if (!result.Equals("null")) results.Add(result);
            func.RESULTS = results;
            func.CLAZZ_NAME = clazz_name;
            func.FUNC_NAME = func_name;

            this[clazz_name + "::" + func_name] = func;
        }

        public void setSource(string func_name, string clazz_name = "")
        {
            Func func = this.GetValueOrDefault(clazz_name + "::" + func_name, new Func());
            func.IS_SOURCE = true;

            this[clazz_name + "::" + func_name] = func;
        }

        public bool isSource(string func_name, string clazz_name = "")
        {
            Func func = this.GetValueOrDefault(clazz_name + "::" + func_name, new Func());
            return func.IS_SOURCE;
        }
    }

    /// <summary>
    /// 分析句柄主体
    /// </summary>
    public class AnalyseHandler
    {
        /// <summary>
        /// 每个可变量或函数返回值的最大储存量
        /// </summary>
        public static int MAX_RESULT_SAVE = 50;

        /// <summary>
        /// 临时寄存器集
        /// </summary>
        public RegisterSaver registerSaver = new RegisterSaver();

        /// <summary>
        /// 变量集储存器
        /// </summary>
        public VarSaver varSaver = new VarSaver();

        /// <summary>
        /// 方法返回值储存器
        /// </summary>
        public FuncReturnSaver funcSaver = new FuncReturnSaver();

        /// <summary>
        /// DO_CALL类型操作码处理器
        /// </summary>
        public DoCallHandler doCallHandler;// = new WCheckerUtils(this);

        /// <summary>
        /// 寄存即将调用DO_CALL类型操作码的相关表达式
        /// </summary>
        public List<string> doCallRecord = new List<string>();

        public AnalyseHandler()
        {
            doCallHandler = new DoCallHandler(this);
        }

        /// <summary>
        /// 分析操作码串的正式入口函数
        /// </summary>
        /// <param name="opcodeArray"></param>
        public void analyseOpcodeBunch(OpCodeSaver opcodeArray)
        {
            BaseOpcodeHandler handler = null;
            foreach (ZendOpArray opArray in opcodeArray)
            {
                switch (opArray.OPCODE_NAME)
                {
                    default:
                        continue;
                    case "QM_ASSIGN":
                    case "ASSIGN_CONCAT":
                    case "ASSIGN":
                    case "BOOL":
                    case "ROPE_INIT":
                    case "ROPE_ADD":
                    case "ROPE_END":
                    case "CONCAT":
                    case "CLONE":
                    case "CAST":
                    case "FETCH_CLASS":
                    case "FAST_CONCAT":
                    case "INIT_DYNAMIC_CALL":
                    case "INIT_FCALL_BY_NAME":
                    case "INIT_FCALL":
                    case "SEND_VAR_EX":
                    case "SEND_VAR":
                    case "SEND_VAL":
                    case "SEND_VAR_NO_REF_EX":
                    case "SEND_VAL_EX":
                    case "SEND_REF":
                    case "NEW":
                    case "INIT_METHOD_CALL":
                    case "DO_ICALL":
                    case "DO_FCALL":
                    case "DO_FCALL_BY_NAME":
                    case "DO_UCALL":
                    case "DO_UCALL_BY_NAME":
                    case "RECV":
                    case "RECV_INIT":
                    case "RETURN":
                        handler = new OperateOpcodeHandler();
                        break;
                    case "ADD":
                    case "SUB":
                    case "MUL":
                    case "DIV":
                    case "MOD":
                    case "SL":
                    case "SR":
                    case "BW_OR":
                    case "BW_AND":
                    case "BW_XOR":
                    case "BOOL_XOR":
                    case "ASSIGN_ADD":
                    case "ASSIGN_SUB":
                    case "ASSIGN_MUL":
                    case "ASSIGN_DIV":
                    case "ASSIGN_MOD":
                    case "ASSIGN_SL":
                    case "ASSIGN_SR":
                    case "POST_INC":
                    case "PRE_INC":
                    case "POST_DEC":
                    case "PRE_DEC":
                        handler = new CalcOpcodeHandler();
                        break;
                }

                handler.handleZendOpCode(opArray, this);
            }

            //Opcode Handle Done.
            VarSaver tmp = varSaver;
        }
    }
}
