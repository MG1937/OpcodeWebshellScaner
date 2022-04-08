using OpcodeWebshellScan.OpcodeHandler;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpcodeWebshellScan.Utils
{
    class PhpUtils
    {
        public static string getExecOutput(string eval_code) 
        {
            eval_code = "function owserr(){echo 'error';}"
                +"set_error_handler('owserr');"
                +"set_exception_handler('owserr');"
                +"eval(base64_decode('" + EncodeBase64(eval_code) + "'));";
            string result = CmdUtils.getExecOutput(VldEnvironment.getPhpPath(), "-r \"" + eval_code + "\"");
            if (result.IndexOf("error") != -1) return "error";
            return result;
        }

        private static string EncodeBase64(string code)
        {
            string encode = "";
            byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(code);
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = code;
            }
            return encode;
        }
    }
}
