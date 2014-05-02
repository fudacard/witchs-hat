using Sgry.Azuki.WinForms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace WitchsHat
{
    public class SuggestionManager
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCaretPos(out Point point);

        AzukiControl azuki;
        ListBox listBox;
        Form1 form;
        PopupWindow popup;
        TabPage tabPage;

        public Dictionary<string, JSType> types;
        List<Token> tokens;
        Dictionary<string, JSType> localVarTypes;
        int MaxSize = 20000;
        int InputBegin = 0;
        int InputEnd = 0;
        string NewInputString = "";
        List<string> CurrentList;
        Dictionary<string, JSMember> typedata = new Dictionary<string, JSMember>();

        public bool Enable { get; set; }

        public SuggestionManager(TabPage tabPage, AzukiControl azuki, ListBox listBox, Form1 form, PopupWindow popup)
        {
            this.tabPage = tabPage;
            this.azuki = azuki;
            this.listBox = listBox;
            this.form = form;
            this.popup = popup;

            types = new Dictionary<string, JSType>();
            ReadXml();
            azuki.KeyPress += this.Azuki_KeyPress;
            azuki.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (!Enable)
                {
                    return;
                }
                if (listBox.Visible && ((e.KeyData & Keys.Right) == Keys.Right))
                {
                    Console.WriteLine("Right");
                    listBox.Visible = false;
                    popup.Visible = false;
                }
                else if (listBox.Visible && ((e.KeyData & Keys.Left) == Keys.Left))
                {
                    Console.WriteLine("Left");
                    listBox.Visible = false;
                    popup.Visible = false;
                }
                else if (listBox.Visible && ((e.KeyData & Keys.Up) == Keys.Up))
                {
                    Console.WriteLine("Up");
                    if (listBox.SelectedIndex > 0)
                    {
                        listBox.SelectedIndex--;
                    }
                    e.Handled = true;
                }
                else if (listBox.Visible && (e.KeyData & Keys.Down) == Keys.Down)
                {
                    if (listBox.SelectedIndex < listBox.Items.Count - 1)
                    {
                        listBox.SelectedIndex++;
                    }
                    e.Handled = true;
                }
            };
            azuki.MouseClick += delegate(object sender, MouseEventArgs e)
            {
                listBox.Visible = false;
                popup.Visible = false;
            };
            azuki.LostFocus += delegate(object sender, EventArgs e)
            {
                listBox.Visible = false;
                popup.Visible = false;

            };
            listBox.MouseDoubleClick += delegate
            {
                if (listBox.SelectedIndex >= 0)
                {
                    Console.WriteLine(listBox.SelectedItem);
                    azuki.Document.Replace((string)listBox.SelectedItem, InputBegin, InputEnd);
                    listBox.Visible = false;
                }
            };
            listBox.SelectedIndexChanged += this.listBox_SelectedIndexChanged;
        }

        public void Azuki_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Enable)
            {
                return;
            }
            if (tokens == null)
            {
                Analyze();
            }

            if (listBox.Visible)
            {

                if (e.KeyChar == '\r' || e.KeyChar == '\t')
                {
                    // エンターキーまたはタブキー
                    if (listBox.SelectedIndex >= 0)
                    {
                        e.Handled = true;
                        azuki.Document.Replace((string)listBox.SelectedItem, InputBegin, InputEnd);
                    }
                    listBox.Visible = false;
                    popup.Visible = false;
                }
                else if (e.KeyChar == ' ' && (Control.ModifierKeys & Keys.Control) != Keys.Control)
                {
                    // スペースキー
                    if (listBox.SelectedIndex >= 0)
                    {
                        azuki.Document.Replace((string)listBox.SelectedItem, InputBegin, InputEnd);
                    }
                    listBox.Visible = false;
                    popup.Visible = false;
                }
                else if (e.KeyChar == '.')
                {
                    if (listBox.SelectedIndex >= 0)
                    {
                        azuki.Document.Replace((string)listBox.SelectedItem, InputBegin, InputEnd);
                    }
                    listBox.Visible = false;
                    popup.Visible = false;
                }
                else if (e.KeyChar == ';')
                {
                    listBox.Visible = false;
                    popup.Visible = false;
                    Analyze();
                }
                else
                {
                    if (e.KeyChar != 0x08)
                    {
                        NewInputString += e.KeyChar;
                        InputEnd++;
                    }
                    else if (e.KeyChar == 0x08)
                    {
                        if (NewInputString.Length > 0)
                        {
                            NewInputString = NewInputString.Substring(0, NewInputString.Length - 1);
                            InputEnd--;
                        }
                        else
                        {
                            listBox.Visible = false;
                            popup.Visible = false;
                        }
                    }
                    // 入力候補を探して候補がある場合のみリスト更新
                    List<string> NewList = new List<string>();
                    foreach (string member in CurrentList)
                    {
                        if (member.StartsWith(NewInputString) || NewInputString.Length == 1)
                        {
                            NewList.Add(member);
                        }
                    }
                    listBox.SelectedIndex = -1;
                    if (NewInputString != "" && NewList.Count > 0)
                    {
                        listBox.Items.Clear();
                        for (int i = 0; i < NewList.Count; i++)
                        {
                            listBox.Items.Add(NewList[i]);
                            if (listBox.SelectedIndex == -1 && NewList[i].StartsWith(NewInputString))
                            {
                                listBox.SelectedIndex = i;
                            }
                        }
                    }
                    else
                    {
                        listBox.SelectedIndex = -1;
                    }
                }
            }


            if (!listBox.Visible)
            {
                if (e.KeyChar == '.')
                {
                    string src = azuki.Text.Substring(0, azuki.CaretIndex);
                    string token = lastToken(src, 1);
                    Console.WriteLine(token);
                    //string className = GetClass(src, token);
                    string className;

                    Dictionary<string, JSMember> OriginalList = GetMembers(src, azuki.CaretIndex);
                    if (OriginalList != null && OriginalList.Count > 0)
                    {


                        // 入力候補取得、表示
                        //OriginalList = GetMembers(className);

                        CurrentList = new List<string>(OriginalList.Keys);
                        CurrentList.Sort();
                        listBox.Items.Clear();
                        foreach (string member in CurrentList)
                        {
                            listBox.Items.Add(member);
                        }
                        listBox.SelectedIndex = 0;
                        int lineIndex, columnIndex;
                        azuki.GetSelection(out lineIndex, out columnIndex);
                        InputBegin = lineIndex + 1;
                        InputEnd = InputBegin;
                        NewInputString = "";

                        typedata = new Dictionary<string, JSMember>();
                        foreach (KeyValuePair<string, JSMember> pair in OriginalList)
                        {
                            if (pair.Value.Hint != null)
                            {
                                typedata.Add(pair.Value.Name, pair.Value);
                            }
                        }

                        ShowListBox(7);
                    }
                }
                else if (('A' <= e.KeyChar && e.KeyChar <= 'Z') || ('a' <= e.KeyChar && e.KeyChar <= 'z'))
                {

                    string src = azuki.Text.Substring(0, azuki.CaretIndex);
                    string token = lastToken(src, 1);
                    Console.WriteLine("[" + token + "]");
                    if (";(){}+-*/=new".IndexOf(token) >= 0)
                    {
                        CurrentList = GetLocalWords(src);

                        if (CurrentList.Count > 0)
                        {

                            // リストボックスを所定の位置に移動
                            Point apiPoint;
                            GetCaretPos(out apiPoint);
                            listBox.Left = apiPoint.X;
                            listBox.Top = apiPoint.Y + (azuki.LineHeight + 2);

                            // 入力候補取得、表示

                            CurrentList.Sort();
                            listBox.Items.Clear();
                            foreach (string member in CurrentList)
                            {
                                listBox.Items.Add(member);
                            }
                            int lineIndex, columnIndex;
                            azuki.GetSelection(out lineIndex, out columnIndex);
                            InputBegin = lineIndex;
                            InputEnd = InputBegin + 1;
                            NewInputString = "" + e.KeyChar;

                            ShowListBox(0);
                        }
                    }
                }
                else if (e.KeyChar == ' ' && (Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    string src = azuki.Text.Substring(0, azuki.CaretIndex);
                    string token = lastToken(src, 1);
                    Console.WriteLine("[" + token + "]");
                    if (token == ".")
                    {

                        Dictionary<string, JSMember> OriginalList = GetMembers(src.Substring(0, azuki.CaretIndex - 1), azuki.CaretIndex);
                        if (OriginalList != null && OriginalList.Count > 0)
                        {


                            // 入力候補取得、表示
                            //OriginalList = GetMembers(className);

                            CurrentList = new List<string>(OriginalList.Keys);
                            CurrentList.Sort();
                            listBox.Items.Clear();
                            foreach (string member in CurrentList)
                            {
                                listBox.Items.Add(member);
                            }
                            listBox.SelectedIndex = 0;
                            int lineIndex, columnIndex;
                            azuki.GetSelection(out lineIndex, out columnIndex);
                            InputBegin = lineIndex + 1;
                            InputEnd = InputBegin;
                            NewInputString = "";

                            typedata = new Dictionary<string, JSMember>();
                            foreach (KeyValuePair<string, JSMember> pair in OriginalList)
                            {
                                if (pair.Value.Hint != null)
                                {
                                    typedata.Add(pair.Value.Name, pair.Value);
                                }
                            }

                            e.Handled = true;
                            ShowListBox(7);
                        }
                    }
                    else if (";(){}+-*/=new".IndexOf(token) >= 0)
                    {
                        CurrentList = GetLocalWords(src);

                        if (CurrentList.Count > 0)
                        {

                            // リストボックスを所定の位置に移動
                            Point apiPoint;
                            GetCaretPos(out apiPoint);
                            listBox.Left = apiPoint.X;
                            listBox.Top = apiPoint.Y + (azuki.LineHeight + 2);

                            // 入力候補取得、表示

                            CurrentList.Sort();
                            listBox.Items.Clear();
                            foreach (string member in CurrentList)
                            {
                                listBox.Items.Add(member);
                            }
                            int lineIndex, columnIndex;
                            azuki.GetSelection(out lineIndex, out columnIndex);
                            InputBegin = lineIndex;
                            InputEnd = InputBegin;
                            NewInputString = "";

                            e.Handled = true;
                            ShowListBox(0);
                        }
                    }
                }
            }
        }

        private void UpdateListBoxPos(int offset)
        {
            Point apiPoint;
            GetCaretPos(out apiPoint);
            listBox.Left = apiPoint.X + tabPage.Left + azuki.Left + 3 + offset;
            listBox.Top = apiPoint.Y + tabPage.Top + azuki.Top + (azuki.LineHeight + 2) + 3;

            UpdatePopupPos();
        }

        private void UpdatePopupPos()
        {
            Rectangle clientRect = form.ClientRectangle;
            Point winP = form.PointToClient(form.Bounds.Location);
            popup.Left = form.GetListBoxLeft() - winP.X - popup.Width;
            if (listBox.SelectedIndex >= 0)
            {
                popup.Top = form.GetListBoxTop() - winP.Y + (listBox.SelectedIndex - listBox.TopIndex) * listBox.ItemHeight;
            }
            else
            {
                popup.Top = form.GetListBoxTop() - winP.Y;
            }
        }

        private void ShowListBox(int offset)
        {
            listBox.Visible = true;

            if (popup == null)
            {
                popup = new PopupWindow();
            }
            if (!popup.Visible)
            {
                //popup.Show(form);
            }

            popup.Size = new Size(400, 80);

            UpdateListBoxPos(offset);

            azuki.Focus();

        }

        private void ReadXml()
        {
            string path = Path.Combine(Application.StartupPath, @"Data\types.xml");
            string currentClass = "";
            string currentMember = "";
            string target = "Class";
            using (XmlReader reader = XmlReader.Create(path))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "Class":
                                target = "Class";
                                break;
                            case "Members":
                                target = "Members";
                                break;
                            case "Name":
                                if (target == "Class")
                                {
                                    currentClass = reader.ReadString();
                                    if (!types.ContainsKey(currentClass))
                                    {
                                        types[currentClass] = new JSType(currentClass);
                                    }
                                    //result[currentClass].Name = reader.ReadString();
                                }
                                else
                                {
                                    currentMember = reader.ReadString();
                                    types[currentClass].Members[currentMember] = new JSMember();
                                    types[currentClass].Members[currentMember].Name = currentMember;
                                }
                                break;
                            case "Type":
                                string type = reader.ReadString();
                                if (!types.ContainsKey(type))
                                {
                                    types[type] = new JSType(type);
                                }
                                types[currentClass].Members[currentMember].Type = types[type];

                                break;
                            case "Super":
                                string superClass = reader.ReadString();
                                if (!types.ContainsKey(superClass))
                                {
                                    types[superClass] = new JSType(superClass);
                                }
                                types[currentClass].Super = types[superClass];
                                break;
                            case "Hint":
                                string hint = reader.ReadString();
                                if (target == "Class")
                                {
                                    types[currentClass].Hint = hint;
                                }
                                else
                                {
                                    types[currentClass].Members[currentMember].Hint = hint;
                                }
                                break;
                        }

                    }
                }
            }
        }

        public void Analyze()
        {
            if (azuki.Text.Length > MaxSize)
            {
                return;
            }
            string src = azuki.Text;
            tokens = Tokenize(src);
            localVarTypes = new Dictionary<string, JSType>();

            for (int j = 0; j < tokens.Count - 3; j++)
            {
                string className;
                if (tokens[j + 1].body == "=" && tokens[j + 2].body == "new" && (className = GetClassName(j + 3)) != null)
                {
                    localVarTypes[tokens[j].body] = types[className];
                }
            }
        }

        public static List<Token> Tokenize(string src)
        {
            Match m;
            List<Token> tokens = new List<Token>();
            int count = 0;
            int i = 0;
            while (i < src.Length)
            {
                if (src[i] == '/' && src[i + 1] == '*')
                {
                    while (!(src[i] == '*' && src[i + 1] == '/'))
                    {
                        i++;
                    }
                    i += 2;
                }
                else if (src[i] == '/' && src[i] == '/')
                {
                    while (src[i] != '\n')
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
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), "^\".*?\"")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^=+")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
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
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^[(){},.\!']")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    tokens.Add(token);
                    i += m.Value.Length;
                }
                else if ((m = Regex.Match(src.Substring(i), @"^\w+")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
                    tokens.Add(token);
                    i += m.Value.Length;
                    count++;
                    if (count > 2000)
                    {
                        //    break;
                    }
                }
                else if ((m = Regex.Match(src.Substring(i), @";")).Success)
                {
                    //Console.WriteLine("[" + m.Value + "]");
                    Token token = new Token();
                    token.body = m.Value;
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

        private string GetClassName(int index)
        {
            if (types.ContainsKey(tokens[index].body) && types[tokens[index].body] != null)
            {
                if (index + 1 < tokens.Count && tokens[index + 1].body == ".")
                {
                    return GetClassName(index + 2);
                }
                else
                {
                    return tokens[index].body;
                }
            }
            return null;
        }

        private string lastToken(string str, int count)
        {
            Match m = null;
            for (int i = 0; i < count; i++)
            {
                if ((m = Regex.Match(str, @"\w+$")).Success)
                {
                    str = str.Substring(0, m.Index);
                }
                else if ((m = Regex.Match(str, @"\.$")).Success)
                {
                    str = str.Substring(0, m.Index);
                }
                else if ((m = Regex.Match(str, @"\s+$")).Success)
                {
                    str = str.Substring(0, m.Index);
                    i--;
                }
                else
                {
                    Console.WriteLine();
                }
            }

            return m.Value;
        }

        private Dictionary<string, JSMember> GetMembers(string src, int length)
        {
            string last = lastToken(src, 1);
            Dictionary<string, JSMember> result = new Dictionary<string, JSMember>();

            string className;
            if (types.ContainsKey(last))
            {
                // 型名
                className = last;
            }
            else
            {
                // 変数名
                className = GetClassName(src);
            }
            if (className != null && types.ContainsKey(className))
            {
                result = types[className].GetMembers();
            }
            return result;
        }

        public string GetClassName(string src)
        {
            string token = lastToken(src, 1);
            string token2 = lastToken(src, 2);
            if (token2 == ".")
            {
                string token3 = lastToken(src, 3);
                string className = GetClass(src, token3);
                if (className != null)
                {
                    JSType t = types[className];
                    //return t.Members[token].Name;
                    return t.GetMembers()[token].Name;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                string className = GetClass(src, token);
                if (className != null)
                {
                    return types[className].Name;
                }
                else
                {
                    return null;
                }
            }

        }

        private string GetClass(string src, string Name)
        {
            List<Token> tokens = Tokenize(src);

            for (int j = 0; j < tokens.Count - 3; j++)
            {
                string className;
                if (tokens[j + 1].body == "=" && tokens[j + 2].body == "new" && (className = GetClassName(j + 3)) != null)
                {
                    if (tokens[j].body == Name)
                    {
                        return className;
                    }
                }
            }

            return null;
        }

        private List<string> GetLocalWords(string halfSrc)
        {

            List<Token> tokens = Tokenize(halfSrc);
            List<string> words = new List<string>();

            for (int j = 0; j < tokens.Count - 1; j++)
            {
                if (tokens[j].body == "var")
                {
                    words.Add(tokens[j + 1].body);
                }
            }

            words.Add("if");
            words.Add("function");
            words.Add("while");
            words.Add("return");
            words.Add("var");
            words.Add("new");
            foreach (var a in types["enchant"].Members)
            {
                words.Add(a.Key);
            }
            words.Sort();
            return words;
        }

        private void listBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox.SelectedItem != null && typedata.ContainsKey((string)listBox.SelectedItem))
            {
                if (typedata[(string)listBox.SelectedItem].Hint != null)
                {
                    popup.Visible = true;
                    popup.Controls[0].Text = typedata[(string)listBox.SelectedItem].Hint;
                    Console.WriteLine(popup.Controls[0].Text);
                }
                else
                {
                    popup.Visible = false;
                    popup.Controls[0].Text = "ヒント";
                }

            }
            else
            {
                popup.Visible = false;
                popup.Controls[0].Text = "";
            }

            UpdatePopupPos();
        }
    }
}
