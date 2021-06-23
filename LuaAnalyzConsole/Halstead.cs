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
        protected string identifier = "[a-zA-Z_][a-zA-Z0-9_]*";//general pattern
        protected string[] pOperators;//patterns operators
        protected string[] pOperands;//patterns operands
        protected string[] pIOArguments;//patterns input-output arguments

        protected Dictionary<string, uint> tableOperators;//results
        protected Dictionary<string, uint> tableOperands;//results
        protected uint[] tableIO = new uint[2];//results

        virtual public void PrintTableOperators(){}
        virtual public void PrintTableOperands(){}
        virtual public void PrintTableAgruments(){}
        virtual public void PrintRating() { }
    }

    class LuaHalstead : AbstractHalstead
    {
        private string Exceptions = "(\\-\\-(\\[\\[[^\\]]+\\]\\]))|(\\-\\-([^\n\\[]*)\n?)|(\"([^\"\n]*)\")";//"(\\-\\-\\[\\[[^\\]]+\\]\\]|--([^\n\\[]+)\n|\"([^\"]+)\")";
        private string OperatorWords = "for|while|function|end|then|if|in|or|and|local|else|elseif|do|return|not|until|repeat|break|dofile|require";
        private string OperandWords = "nil|true|false";
        private string OperandNumbers = "(\\s|\\+|\\-|\\*|/|,|=|<|>|\\(|\\)|\\[|\\]|\\{|\\}|\\.\\.)\\-?(\\d+\\.\\d+|\\d+|0[XxХх]\\d+)";

        private bool isNotWordOperator(string str)
        {
            /*for (int i = 0; i < pOperators.Length; i++)
                if ((new Regex("^"+pOperators[i]+"$")).IsMatch(str)) 
                    return false;
            return true;*/
            return !(new Regex("^(" + OperatorWords + ")$").IsMatch(str));
        }
        
        private bool isNotWordLua(string word)//for names var
        {
            string pattern = "^("+OperatorWords + "|"+OperandWords+")$";
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

        private void countingIO(string data, int id, int idGroup)
        {
            if (id>=0&&id<tableIO.Length)
            {
                Regex pattern = new Regex(pIOArguments[id]);
                foreach (Match m in pattern.Matches(data))
                {
                    if (m.Groups.Count > idGroup)
                        tableIO[id] += (uint)countingArguments(m.Groups[idGroup].Value, id==0);
                }
            }
        }

        private void TableChangeValue(Dictionary<string, uint> table, bool isAdd, string key, uint count)//isAdd: true - inc, false - derc
        {
            if (isAdd)
            {
                if (table.ContainsKey(key))
                    table[key] += count;
                else
                    table.Add(key, count);
            }
            else
            {
                if (table.ContainsKey(key))
                {
                    if ((int)(table[key] - count) <= 0)
                        table.Remove(key);
                    else
                        table[key] -= count;
                }
            }
        }

        private void SearchPattern(string text, Regex pattern, Dictionary<string, uint> table, bool isAdd)
        {
            foreach (Match m in pattern.Matches(text))
            {
                int index;
                for (index = m.Groups.Count-1; index>0; index--)
                    if (m.Groups[index].Value!="") break;
                TableChangeValue(table, isAdd, m.Groups[index].Value, 1);
            }
        }

        private void SearchWords(string text, string wordPattern, Dictionary<string, uint> table, bool isAdd)
        {
            Regex pattern = new Regex("^(" + wordPattern + ")\\s|[^a-zA-Z](" + wordPattern + ")$|[^a-zA-Z](" + wordPattern + ")\\s|[^a-zA-Z](" + wordPattern + ")[^a-zA-Z0-9]");//^(if)\s|[^a-zA-Z](if)$|[^a-zA-Z](if)\s
            SearchPattern(text, pattern, table, isAdd);
        }

        private void SearchOperators(string text, /*Dictionary<string, uint> table, string[] patterns,*/ bool isAdd=true)
        {
            for (int i = 0; i < pOperators.Length; i++)
            {
                Regex pattern = new Regex(pOperators[i]);
                uint count = (uint)pattern.Matches(text).Count;
                if (count > 0)
                {
                    TableChangeValue(tableOperators, isAdd, pOperators[i], count);
                }
            }
            SearchWords(text, OperatorWords, tableOperators, isAdd);
        }

        private void SearchOperands(string text, Dictionary<string, uint> table, string[] patterns, bool isAdd = true)
        {//search identifiers..
            for (int i = 0; i < patterns.Length; i++)
            {
                Regex pattern = new Regex("("+patterns[i]+")");//"(" + this.identifier + ")");
                foreach (Match m in pattern.Matches(text))
                {
                    if (m.Groups.Count > 1)
                    {
                        string value = m.Groups[1].Value.ToString();
                        if (isNotWordLua(value))
                            TableChangeValue(tableOperands, isAdd, value, 1);
                    }
                }
            }

            SearchWords(text, OperandWords, tableOperands, isAdd);
            SearchPattern(text, (new Regex(this.OperandNumbers)), tableOperands, isAdd);
        }

       /* private void RemoveOther()//called once
        {
            //decr in identifiers...(experimental)
            /*string text = "";
            foreach (var key in tableOperands.Keys)
                text += key+" ";
            SearchOpers(text, tableOperators, pOperators, false);
        * /
            foreach (var key in tableOperands.Keys)
                SearchOpers(key, tableOperators, pOperators, false);
        }*/

        public LuaHalstead()
        {
            tableOperators = new Dictionary<string, uint>();
            tableOperands = new Dictionary<string, uint>();
            tableIO[0]=0; tableIO[1] = 0;
            base.pOperators = new string[]{ @"(\-\-\[\[[^\]]+\]\])|(--[^\n]+\n)", "[^<=>]=[^<=>]", "[^<=>]>[^<=>]","[^<=>]<[^<=>]","==",">=","<=",
                                            "\\^","#","\\*","/","[a-zA-Z]?\\-[a-zA-Z]|[a-zA-Z0-9\\s]+\\-[^0-9\\-]","\\+",",",":","[^\\.0-9]\\.[^\\.0-9]","\\.\\.","%",
                                            "\\(", "\\{", "\\[[^]\n]*\\]"//(), {}, []
                                            //"\"","\\(","\\)",";",
                                            //"\\{","\\}","\\[","\\]","--[^\n]+\n",/*"return","in",
                                            //"for","while","if","then","end","local","function", "else", "elseif", "do", "dofile", "require", "not", "until", "repeat"
                                           };
            base.pOperands = new string[] { identifier, "\"[^\"\n]+\""};//{ "nil", "true", "false", "[0-9]+\\.[0-9]+|0[XxХх][0-9]+|\\-?[0-9]+"/*"[0-9]+"*/, "\"[^\"\n]+\"", identifier};
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
                string data = File.ReadAllText(script.FullName);

                SearchOperators(data);//, tableOperators, pOperators);
                //SearchOpers(data, tableOperands, pOperands);
                SearchOperands(data, tableOperands, pOperands);

                countingIO(data, 0, 2);
                countingIO(data, 1, 2);

                //decr exceptions..
                //for (int i=0; i<Exceptions.Length; i++)
                {
                    Regex pattern = new Regex(Exceptions);
                    foreach (Match m in pattern.Matches(data))
                    {
                        int index = m.Groups.Count;
                        for (; index >= 0; index--)
                            if (m.Groups[index].Value != "")
                                break;
                        string value = m.Groups[index].Value.ToString();
                        //string value = m.Groups[0].Value.ToString();
                        if (value != "")
                        {
                            SearchOperators(value, false);//, tableOperators, pOperators, false);
                            //SearchOpers(value, tableOperands, pOperands, false);
                            SearchOperands(value, tableOperands,pOperands, false);
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
            //RemoveOther();
        }
        public void RestartDir(DirectoryInfo dir)
        {
            Clear();
            Run(dir);
            //RemoveOther();
        }

        override  public void PrintTableOperators()
        {
            Console.WriteLine("Table operators...");
            foreach (var key in tableOperators.Keys)
            {
                Console.WriteLine(key+":"+tableOperators[key]);

            }
        }
        override public void PrintTableOperands()
        {
            Console.WriteLine("Table operands...");
            foreach (var key in tableOperands.Keys)
            {
                Console.WriteLine(key + ":" + tableOperands[key]);
            }
        }
        override public void PrintTableAgruments()
        {
            Console.WriteLine("Table arguments...");
            Console.WriteLine("Input args: "+tableIO[0]);
            Console.WriteLine("Output args: "+tableIO[1]);
        }
        override public void PrintRating()
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
