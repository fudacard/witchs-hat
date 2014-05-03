using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitchsHat
{
    public class JSType
    {
        public string Name;
        public Dictionary<string, JSMember> Members;
        public JSType Super;
        public string Hint { get; set; }

        public JSType()
        {
            Members = new Dictionary<string, JSMember>();
        }

        public JSType(string name)
        {
            this.Name = name;
            Members = new Dictionary<string, JSMember>();
        }

        public Dictionary<string, JSMember> GetMembers()
        {
            Dictionary<string, JSMember> result = new Dictionary<string, JSMember>(Members);
            if (Super != null)
            {
                Dictionary<string, JSMember> superList = Super.GetMembers();
                foreach (var pair in superList)
                {
                    if (!result.ContainsKey(pair.Key))
                    {
                        result.Add(pair.Key, pair.Value);
                    }
                }
            }
            return result;
        }
    }
}
