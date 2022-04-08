using OpcodeWebshellScan.OpcodeHandler;
using OpcodeWebshellScan.OpcodeHandler.Analyse;
using OpcodeWebshellScan.Utils;
using OpcodeWebshellScan.WebshellChecker;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace OpcodeWebshellScan
{
    class Program
    {
        static Dictionary<string, List<OpCodeSaver>> opcodeSaver = new Dictionary<string, List<OpCodeSaver>>();

        private static StringBuilder output = new StringBuilder();
        static void Main(string[] args)
        {
            
            VldEnvironment.setPhpPath("php.exe");
            VldEnvironment.setPhpdbgPath("phpdbg.exe");
            VldInstance instance = new VldInstance();
            instance.createVldInstance("C:/Users/Administrator/Desktop/test/test.php");
            opcodeSaver = instance.getOpCodeSaver();

            AnalyseHandler handler = new AnalyseHandler();

            foreach (string k in opcodeSaver.Keys)
            {
                foreach (OpCodeSaver o in opcodeSaver[k])
                {
                    handler.analyseOpcodeBunch(o);
                }
            }

            string msg = "";
            foreach (string tmpNum in handler.doCallHandler.tmp_funcs.Keys)
            {
                List<Function> functions = handler.doCallHandler.tmp_funcs[tmpNum];
                foreach (Function function in functions)
                {
                    Console.WriteLine(tmpNum + "::" + function.FUNC_NAME + ":: Return");
                    foreach (string r in handler.doCallHandler.getFuncReturns(tmpNum))
                    {
                        Console.WriteLine(r);
                    }
                }
                
            }
            
            Console.WriteLine("All Done!!");
            

            //Console.WriteLine(Regex.IsMatch("as12_dsa","^([a-zA-z0-9_])+$"));
            //Console.WriteLine(CmdUtils.getExecOutput("phpdbg.exe"));
            //string opline = CmdUtils.getExecOutput("php.exe", " -dvld.active=1 -dvld.execute=0 -dvld.verbosity=0 C:/Users/Administrator/Desktop/test/test.php");
            //Console.WriteLine("  12 ".IndexOf("12"));
            //string opline = CmdUtils.getExecOutput("C:/Users/Administrator/vld.bat");
            //Console.WriteLine("::::::::::");
            //Console.WriteLine(opline);
            //String opcode = "L4    #0     RETURN                  \"ys \\ntem\"";
            //Array a = (opcode.Split(" "));
            //String command = CmdUtils.getExecOutput("phpdbg.exe", "-p* C:/Users/Administrator/Desktop/test/test.php");
            //Console.WriteLine(command);
            //Console.WriteLine("OtherThing...");
        }
    }
}
