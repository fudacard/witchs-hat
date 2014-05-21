using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WitchsHat
{
    class WHAnalyzer
    {
        List<Token> tokens;
        public Dictionary<string, WHClass> classes;

        public void Analyze(string src)
        {
            classes = new Dictionary<string, WHClass>();
            tokens = Tokenize(src);
            string currentClass = null;
            int nest = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                if (MatchToken(tokens, i + 1, "=", "Class", ".", "create")
                    || MatchToken(tokens, i + 1, "=", "enchant", ".", "Class", ".", "create"))
                {
                    //Console.WriteLine("class define: " + tokens[i].body);
                    WHClass whclass = new WHClass(tokens[i].body);
                    whclass.Position = tokens[i].Position;
                    classes[whclass.Name] = whclass;
                    currentClass = tokens[i].body;
                    while (i < tokens.Count && tokens[i].body != "(")
                    {
                        i++;
                    }
                    i++;
                    if (i >= tokens.Count)
                    {
                        break;
                    }
                    if (tokens[i].body != "{")
                    {
                        while (i + 1 < tokens.Count && tokens[i + 1].body == ".")
                        {
                            i += 2;
                        }
                        //Console.WriteLine("super: " + tokens[i].body);
                        whclass.SuperClass = tokens[i].body;
                    }
                    nest++;
                }
                else if (tokens[i].body == "{")
                {
                    nest++;
                }
                else if (tokens[i].body == "}")
                {
                    nest--;
                    if (nest == 0)
                    {
                        currentClass = null;
                    }
                }
                else if (MatchToken(tokens, i + 1, ":", "function", "("))
                {
                    //Console.WriteLine("function define: " + tokens[i].body);
                    //Console.WriteLine("this:" + currentClass);
                    WHFunction whfunction = new WHFunction(tokens[i].body);
                    whfunction.Position = tokens[i].Position;
                    if (currentClass != null)
                    {
                        classes[currentClass].Functions.Add(whfunction);
                    }
                }
            }
        }

        private static bool MatchToken(List<Token> tokens, int index, params string[] tokenBodys)
        {
            if (tokens.Count <= index + tokenBodys.Length)
            {
                return false;
            }
            for (int i = 0; i < tokenBodys.Length; i++)
            {
                if (tokens[index + i].body != tokenBodys[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static List<Token> Tokenize(string src)
        {
            Match m;
            List<Token> tokens = new List<Token>();
            int count = 0;
            int i = 0;
            while (i < src.Length)
            {
                if (src[i] == '/' && i + 1 < src.Length && src[i + 1] == '*')
                {
                    while (!(src[i] == '*' && i + 1 < src.Length && src[i + 1] == '/'))
                    {
                        i++;
                    }
                    i += 2;
                }
                else if (src[i] == '/' && i + 1 < src.Length && src[i + 1] == '/')
                {
                    while (i < src.Length && src[i] != '\n')
                    {
                        i++;
                    }
                    i++;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^'.*?'")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    token.Position = i;
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), "^\".*?\"")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    token.Position = i;
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^=+")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    token.Position = i;
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^\s+")).Success)
                {
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^!==")).Success)
                {
                    // Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    token.Position = i;
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^[(){},.\!':]")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    token.Position = i;
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^\w+")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    token.Position = i;
                    tokens.Add(token);
                    i += m.Value.Length;
                    count++;
                }
                else if ((m = Regex.Match(src.Substring(i), @";")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    token.Position = i;
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else
                {
                    Console.WriteLine(src[i]);
                    i++;
                }
            }

            return tokens;
        }

        public string GetThisClass(int position)
        {
            string prevClass = null;
            foreach (var pair in classes)
            {
                if (pair.Value.Position > position)
                {
                    return prevClass;
                }
                else
                {
                    prevClass = pair.Value.Name;
                }
            }
            return prevClass;
        }
    }

    class WHClass
    {
        public string Name { get; set; }
        public int Position { get; set; }
        public string SuperClass { get; set; }
        public List<WHFunction> Functions { get; set; }

        public WHClass(string name)
        {
            this.Name = name;
            this.Functions = new List<WHFunction>();
        }

    }

    class WHFunction
    {
        public string Name { get; set; }
        public int Position { get; set; }

        public WHFunction(string name)
        {
            this.Name = name;
        }
    }
}
