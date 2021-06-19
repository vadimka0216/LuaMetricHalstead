using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.IO;
namespace LuaAnalyzConsole
{
    class AbstractHalstead
    {
        protected const int S = 18;//depends on the human factor (5<=S<=20)
        protected string identifier = "[a-zA-Z][a-zA-Z0-9]*";//general pattern
        protected string[] pOperators;//patterns operators
        protected string[] pOperands;//patterns operands
        protected string[] pIOArguments;//patterns input-output arguments

        virtual public void PrintTableOperators(){}
        virtual public void PrintTableOperands(){}
        virtual public void PrintTableAgruments(){}
        virtual public void PrintRating() { }
    }

    class LuaHalstead : AbstractHalstead
    {
        private string Exceptions = "(\\-\\-\\[\\[[^\\]]+\\]\\]|--([^\n\\[]+)\n|\"([^\"]+)\")";
        private Dictionary<string, uint> tableOperators;
        private Dictionary<string, uint> tableOperands;
        private uint[] tableIO=new uint[2];

        private bool isNotOperator(string str)
        {
            for (int i = 0; i < pOperators.Length; i++)
                if ((new Regex("^"+pOperators[i]+"$")).IsMatch(str)) 
                    return false;
            return true;
        }
        private bool isNotWordLua(string word)//for names var
        {
            string pattern = "^(for|while|function|end|then|if|in|or|and|nil|true|false|local|else|elseif|do|return|not|until|repeat|break|dofile|require)$";
            return !(new Regex(pattern)).IsMatch(word);
        }

        private int countingArguments(string outputString, bool isInput=false)
        {
            string delimiter="..";
            if (isInput)
                delimiter = ",";
            int count = 0;
            int start = 0;
            do
            {
                count++;
                start = outputString.IndexOf(delimiter, start) + 1;
            } while (start > 0);
            return count;
        }

        static private void SearchOpers(string text, Dictionary<string, uint> table, string[] array, bool isAdd=true)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Regex pattern = new Regex(array[i]);
                uint count = (uint)pattern.Matches(text).Count;
                if (count > 0)
                {
                    if (isAdd)
                    {
                        if (table.ContainsKey(array[i]))
                            table[array[i]] += count;
                        else
                            table.Add(array[i], count);
                    }
                    else
                    {
                        if (table.ContainsKey(array[i]))
                        {
                            if ((int)(table[array[i]] - count)<= 0)
                                table.Remove(array[i]);
                            else
                                table[array[i]] -= count;
                        }
                    }
                }
            }
        }

        private void SearchIdentifiers(string text, Dictionary<string, uint> table, bool isAdd=true)
        {//search identifiers..
            Regex pattern = new Regex("(" + this.identifier + ")");
            foreach (Match m in pattern.Matches(text))
            {
                if (m.Groups.Count > 1)
                {
                    string value = m.Groups[1].Value.ToString();
                    if (isAdd)
                    {
                        if (isNotWordLua(value))
                        {
                            if (table.ContainsKey(value))
                                table[value]++;
                            else
                                table.Add(value, 1);
                            //tableOperands[value]++;
                        }
                    }
                    else if (table.ContainsKey(value))
                    {
                        table[value]--;
                        if (table[value] == 0)
                            table.Remove(value);
                    }
                }
            }
        }

        public LuaHalstead()
        {
            tableOperators = new Dictionary<string, uint>();
            tableOperands = new Dictionary<string, uint>();
            tableIO[0]=0; tableIO[1] = 0;
            base.pOperators = new string[]{ @"\-\-\[\[[^\]]+\]\]", "[^<=>]=[^<=>]", "[^<=>]>[^<=>]","[^<=>]<[^<=>]","==",">=","<=","or","and",
                                            "\\^","#","\\*","/","\\-","\\+","return","in",
                                            "for","while","if","then","end","local","function",",",":","[^\\.0-9]\\.[^\\.0-9]","\\.\\.","%","\"","\\(","\\)",
                                            "\\{","\\}","\\[","\\]",";","--[^\n]+\n", "else", "elseif", "do", "dofile", "require"
                                           };
            base.pOperands = new string[] { "nil", "true", "false", /*"[^\\.]?\\-?[0-9]+|[0-9]+.[0-9]+"*/"[0-9]+", "\"[^\"]+\""/*, identifier*/ };
            base.pIOArguments = new string[2] { @"(local)?\s+(([a-zA-Z][a-zA-Z0-9]+,\s*)*[a-zA-Z][a-zA-Z0-9]+)\s*=\s*(get|sampGet)" /*"sampGet"*/, @"(print|printLog|sampAddChatMessage)\((.*)\)" };//0 - input, 1 - output
        }
        ~LuaHalstead()
        {
            Clear();
        }
        private void Clear()
        {
            if (tableOperands != null)
                tableOperands.Clear();
            if (tableOperators != null)
                tableOperators.Clear();
            tableIO[0] = tableIO[1] = 0;
        }
        private void Run(FileInfo script)
        {
            if (script.Exists)
            {
                string data=File.ReadAllText(script.FullName);
                SearchOpers(data, tableOperators, pOperators);
                SearchOpers(data, tableOperands, pOperands);
                SearchIdentifiers(data, tableOperands);

                {
                    Regex pattern = new Regex(pIOArguments[0]);
                    foreach (Match m in pattern.Matches(data))
                    {
                        if (m.Groups.Count > 2)
                            tableIO[0] += (uint)countingArguments(m.Groups[2].Value, true);
                    }
                    //tableIO[0] += (uint)pattern.Matches(data).Count;
                }

                {
                    Regex pattern = new Regex(pIOArguments[1]);
                    foreach (Match m in pattern.Matches(data))
                    {
                        if (m.Groups.Count > 2)
                            tableIO[1] += (uint)countingArguments(m.Groups[2].Value);
                    }
                }

                //decr exceptions..
                //for (int i=0; i<Exceptions.Length; i++)
                {
                    Regex pattern = new Regex(Exceptions);
                    foreach (Match m in pattern.Matches(data))
                    {
                        if (m.Groups.Count > 1)
                        {
                            string value = m.Groups[1].Value.ToString();
                            SearchOpers(value, tableOperators, pOperators, false);
                            SearchOpers(value, tableOperands, pOperands, false);
                            SearchIdentifiers(value, tableOperands, false);
                        }
                    }
                }
                
            }
        }
        private void Run(DirectoryInfo dir)
        {
            foreach (DirectoryInfo curr in dir.GetDirectories())
                Run(curr);
            foreach (FileInfo file in dir.GetFiles())
            {
                if (file.FullName.IndexOf(".lua") >= 0)
                {
                    Run(file);
                }
            }
        }

        public void RestartFile(FileInfo script)
        {
            Clear();
            Run(script);
        }
        public void RestartDir(DirectoryInfo dir)
        {
            Clear();
            Run(dir);
        }

        public void PrintTableOperators()
        {
            Console.WriteLine("Table operators...");
            foreach (var key in tableOperators.Keys)
            {
                Console.WriteLine(key+":"+tableOperators[key]);

            }
        }
        public void PrintTableOperands()
        {
            Console.WriteLine("Table operands...");
            foreach (var key in tableOperands.Keys)
            {
                Console.WriteLine(key + ":" + tableOperands[key]);
            }
        }
        public void PrintTableAgruments()
        {
            Console.WriteLine("Table arguments...");
            Console.WriteLine("Input args: "+tableIO[0]);
            Console.WriteLine("Output args: "+tableIO[1]);
        }
        public void PrintRating()
        {
            Console.WriteLine("\n:::::::::::::::::::::::::::\nTotal rating with S=" + S + "\n:::::::::::::::::::::::::::\n");
            int n1 = tableOperators.Count;//number of simple operators
            int n2 = tableOperands.Count;//number of simple operands
            uint N1 = 0;//summ operators
            foreach (uint val in tableOperators.Values)
                N1 += val;
            uint N2 = 0;//summ operands
            foreach (uint val in tableOperands.Values)
                N2 += val;
            uint args = tableIO[0] + tableIO[1];//summ in-out put args
            uint n = (uint)(n1 + n2);//wordbook programm
            uint N = N1 + N2;//lenght release
            double lenN = n1 * Math.Log(n1, 2) + n2 * Math.Log(n2, 2);//lenght programm
            double V = (N1 + N2) * Math.Log(n1 + n2, 2);//bit size
            double aV=(args+2)*Math.Log(args+2, 2);//about bit size
            double L = aV / V;//Level release
            double La = L * aV;//level lang
            double I = L * V;//intellectual content
            double E = V / L;//rating programming
            double B0 = V / 3000;//count errors
            double T = (aV * aV * aV) / (S * La * La);//development time
            Console.WriteLine("number of simple operators (n1): " + n1);
            Console.WriteLine("number of simple operands (n2): " + n2);
            Console.WriteLine("summ operators (N1): " + N1);
            Console.WriteLine("summ operands (N2): " + N2);
            Console.WriteLine("summ input-output arguments (args): " + args);
            Console.WriteLine("wordbook programm (n): " + n);
            Console.WriteLine("lenght release (N): " + N);
            Console.WriteLine("lenght programm (lenN): " + lenN);
            Console.WriteLine("bit size (V): " + V);
            Console.WriteLine("potential bit size (aV): " + aV);
            Console.WriteLine("Level release (L): " + L);
            Console.WriteLine("level lang (La): " + La);
            Console.WriteLine("intellectual content (I): " + I);
            Console.WriteLine("rating programming (E): " + E);
            Console.WriteLine("number of transmitted errors (B0): " + B0);
            Console.WriteLine("development time (T): " + T + " (sec) = " + T / 60 + " (mins)");
        }
    }
}
