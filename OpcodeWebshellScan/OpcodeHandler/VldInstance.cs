using OpcodeWebshellScan.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace OpcodeWebshellScan.OpcodeHandler
{
    /// <summary>
    /// Opcode结构
    /// </summary>
    public struct ZendOpArray
    {
        public string CLAZZ_NAME;//操作码串隶属类
        public string FUNC_NAME;//操作码串隶属方法
        public string PHP_LINE;//该操作码在PHP文件中对应的行数
        public string OP_LINE;//该操作码在操作码串中对应的行数
        public string OPCODE_NAME;//操作码名
        public string VAR1, VAR2;//操作码操纵的参数
        public string RESULT;//寄存器
    }

    /// <summary>
    /// Opcode串保存器
    /// 原则上保存分析文件的方法对应的Opcode串
    /// </summary>
    public class OpCodeSaver : List<ZendOpArray> {

        public string CLAZZ_NAME = "";//操作码串隶属类

        public string FUNC_NAME = "{main}";//操作码串隶属方法

        public void AddOpArrays(ZendOpArray opArrays) {
            base.Add(opArrays);
        }
    }

    /// <summary>
    /// Phpdbg实体,原则上每个PHP源文件
    /// 都应有各自的Phpdbg实体对象对文件进行解析
    /// </summary>
    class VldInstance
    {
        /// <summary>
        /// 储存每个方法的操作码串
        /// </summary>
        public Dictionary<string, List<OpCodeSaver>> opcodeSaver = new Dictionary<string, List<OpCodeSaver>>();

        public Dictionary<string, List<OpCodeSaver>> getOpCodeSaver() 
        {
            return opcodeSaver;
        }

        /// <summary>
        /// 替换compiled vars
        /// </summary>
        /// <returns></returns>
        private string replaceCompiledVar(Dictionary<string,string> compiledVars,string data)
        {
            foreach (string compiledvar in compiledVars.Keys)
            {
                data = data.Replace(compiledvar, compiledVars.GetValueOrDefault(compiledvar,""));
                //TODO:May have bug?? 20220201
            }
            return data;
        }

        public void createVldInstance(string phpFile) 
        {
            string opcodes = CmdUtils.getExecOutput(VldEnvironment.getPhpPath(), "-dvld.active=1 -dvld.execute=0 -dvld.verbosity=0 -f " + phpFile) + "\n";

           /*
            * 处理操作码串的一些基本信息 
            */
            string clazz = "";
            string func = "{main}";
            string lineNum = "1";
            Dictionary<string, string> compiledVars = new Dictionary<string, string>();

            bool recordOpcode = false;

            OpCodeSaver tmpOpcodeSaver = new OpCodeSaver();//临时操作码串储存器

            foreach (string line in opcodes.Split("\n"))
            {
                /*
                 * VLD输出操作码串样式如下
                Class TestClazz:
                Function testfunc:
                filename:       C:\Users\Administrator\Desktop\test\test.php
                function name:  testFunc
                number of ops:  8
                compiled vars:  !0 = $t, !1 = $cv1, !2 = $f
                line      #* E I O op                           fetch          ext  return  operands
                -------------------------------------------------------------------------------------
                    3     0  E >   RECV                                             !0
                    4     1        ASSIGN                                                   !0, 'test1'
                    5     2        ASSIGN                                                   !1, 'CV'
                    6     3        CONCAT                                           ~5      !1, !0
                          4        INIT_DYNAMIC_CALL                                        ~5
                          5        DO_FCALL                                      0  $6
                          6        ASSIGN                                                   !2, $6
                    7     7      > RETURN                                                   null
                */

                if (line.StartsWith("-")||recordOpcode)
                {
                    if (!recordOpcode)
                    {
                        recordOpcode = true;//开始录制操作码串
                        continue;
                    }

                    ZendOpArray opArrays = new ZendOpArray();

                    int baseIndex = 0;//提取操作码以及其他参数的基础偏移量
                    string[] tmp = line.Trim().Split(" ");
                    string tmp_lineNum = tmp[0];//先默认第一个引索为行号
                    /*
                     * 行号以及操作码引索提取
                     * 并且将在检测到行号或操作码引索时设置基础偏移量
                     */
                    for (int i = 1; i < tmp.Length; i++) 
                    {
                        int tmp_i;
                        string indexStr = tmp[i];
                        if (indexStr.Equals("")) continue;
                        if (int.TryParse(indexStr.Replace("*", ""), out tmp_i))
                        {
                            opArrays.PHP_LINE = tmp_lineNum;
                            opArrays.OP_LINE = indexStr.Replace("*", "");
                            baseIndex = line.IndexOf(" " + indexStr + " ") + indexStr.Length;
                            int tmp_baseIndex = (line.Substring(baseIndex,20).IndexOf(" " + indexStr + " "));
                            if (tmp_baseIndex != -1) baseIndex = baseIndex + tmp_baseIndex + 1 + indexStr.Length;
                            lineNum = tmp_lineNum;
                            break;
                        }
                        else
                        {
                            opArrays.PHP_LINE = lineNum;
                            opArrays.OP_LINE = tmp_lineNum.Replace("*", "");
                            baseIndex = line.IndexOf(" " + tmp_lineNum + " ") + tmp_lineNum.Length;
                            break;
                        }
                    }

                    /*
                     * 操作码结束判断
                     */
                    if (tmp.Length == 1)//当前方法的操作码串全部录制完毕时进入分支
                    {
                        string tmp_key = (clazz.Equals("") ? "" : clazz + "::") + func;
                        List <OpCodeSaver> lists = opcodeSaver.GetValueOrDefault(tmp_key, new List<OpCodeSaver>());
                        lists.Add(tmpOpcodeSaver);
                        opcodeSaver[tmp_key] = lists;
                        recordOpcode = false;//停止录制
                        func = "";
                        //clazz = "";//FixBug 20220406 修订Vld对Clazz的作用域
                        lineNum = "";
                        compiledVars.Clear();
                        tmpOpcodeSaver = new OpCodeSaver();//初始化操作码串保存器
                        //初始化必要值
                        continue;
                    }

                    /*
                     * Zend_Op_array正式处理
                     * 根据前文设置的基础偏移量提取相应信息
                     */
                    opArrays.OPCODE_NAME = line.Substring(baseIndex + 8, 24).Trim();
                    opArrays.RESULT = replaceCompiledVar(compiledVars, line.Substring(baseIndex + 57, 4).Trim());
                    string[] vars = line.Substring(baseIndex + 65).Trim().Split(",");
                    //TODO:May have bug?? 20220201
                    opArrays.VAR1 = replaceCompiledVar(compiledVars, vars[0]).Trim();
                    opArrays.VAR2 = vars.Length>1?replaceCompiledVar(compiledVars, vars[1]).Trim():"";

                    if (opArrays.VAR1.StartsWith("'") && opArrays.VAR1.EndsWith("'"))
                    {
                        opArrays.VAR1 = "'" + HttpUtility.UrlDecode(opArrays.VAR1.Replace("'", "")) + "'";
                    }

                    if (opArrays.VAR2.StartsWith("'") && opArrays.VAR2.EndsWith("'"))
                    {
                        opArrays.VAR2 = "'" + HttpUtility.UrlDecode(opArrays.VAR2.Replace("'", "")) + "'";
                    }

                    opArrays.CLAZZ_NAME = clazz;
                    opArrays.FUNC_NAME = func;

                    tmpOpcodeSaver.AddOpArrays(opArrays);//储存临时操作码串

                }
                else 
                {
                    //记录类与方法名以及提取变量
                    if (line.StartsWith("Class "))
                    {
                        clazz = line.Trim().Substring(6).Replace(":", "");
                        tmpOpcodeSaver.CLAZZ_NAME = clazz;//在本轮操作码解析前保存类名
                        continue;
                    }
                    else if (line.StartsWith("End of class "))
                    {
                        clazz = "";//Clazz作用域此处为结束
                        continue;
                    }
                    else if (line.StartsWith("compiled vars:") && line.IndexOf(" none") == -1)
                    {
                        compiledVars.Clear();//清空上一次预存的信息
                        foreach (string data in line.Trim().Substring(14).Split(","))
                        {
                            string tmpVar = data.Split(" = ")[0].Trim();
                            string phpVar = data.Split(" = ")[1].Trim();
                            compiledVars.Add(tmpVar, phpVar);
                        }
                        continue;
                    }
                    else if (line.StartsWith("Function "))
                    {
                        tmpOpcodeSaver.FUNC_NAME = line.Trim().Substring(9).Replace(":", "");//在本轮操作码解析前保存方法名
                        if (tmpOpcodeSaver.FUNC_NAME.Contains("%00") && !tmpOpcodeSaver.FUNC_NAME.Contains("closure"))
                        {
                            tmpOpcodeSaver.FUNC_NAME = tmpOpcodeSaver.FUNC_NAME.Substring(3, tmpOpcodeSaver.FUNC_NAME.IndexOf("%3A") - 4);
                        }
                        func = tmpOpcodeSaver.FUNC_NAME;
                        continue;
                    }
                }
            }
        }
    }
}
