using edu.stanford.nlp.trees;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rockstar2.Studio
{
    public class CodeNode
    {
        public string Name { get; set; }
        public string[] Roots { get; set; }

        public override string ToString()
        {

            var t= Name + ":" + Subject + ":" + RootWord;
            foreach (var dobj in DirectObjects)
                t += ":" + dobj;
            return t+":::" + Parse.toString();
        }

        public CodeNode Clone()
        {
            return new CodeNode() { Name = Name };
        }

        public Tree Parse { get; set; }

        public string RootWord {get;set;}

        public List<string> DirectObjects { get; set; }

        public string Subject { get; set; }

    }

}
