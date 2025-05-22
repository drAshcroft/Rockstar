using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using java.io;
using edu.stanford.nlp.process;
using edu.stanford.nlp.ling;
using edu.stanford.nlp.trees;
using edu.stanford.nlp.parser.lexparser;

namespace Rockstar2.Studio
{
    public class Singer
    {
        private string program = @"
Midnight takes your heart and your soul
While your heart is as high as your soul
Put your heart without your soul into your heart

Give back your heart


Desire is a lovestruck ladykiller
My world is nothing 
Fire is ice
Hate is water
Until my world is Desire,
Build my world up
If Midnight taking my world, Fire is nothing and Midnight taking my world, Hate is nothing
Shout ""FizzBuzz!""
Take it to the top

If Midnight taking my world, Fire is nothing
Shout ""Fizz!""
Take it to the top

If Midnight taking my world, Hate is nothing
Say ""Buzz!""
Take it to the top

Whisper my world
";
        public void LearnSong(string lyrics)
        {
            // Path to models extracted from `stanford-parser-3.7.0-models.jar`
            var jarRoot = @"C:\Anaconda3\stanford-parser-full-2018-02-27\stanford-parser-3.9.1-models\";
            var modelsDirectory = jarRoot + @"\edu\stanford\nlp\models";

            // Loading english PCFG parser from file
            var lp = LexicalizedParser.loadModel(modelsDirectory + @"\lexparser\englishPCFG.ser.gz");
            var sentences = program.Split(new string[] { "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

            List<CodeNode> thoughts = new List<CodeNode>();
            foreach (var sent in sentences)
            {

                // This option shows loading and using an explicit tokenizer
                var tokenizerFactory = PTBTokenizer.factory(new CoreLabelTokenFactory(), "americanize=true,invertible=true,unicodeQuotes=true,unicodeEllipsis=true");
                var sent2Reader = new StringReader(sent);
                var rawWords2 = tokenizerFactory.getTokenizer(sent2Reader).tokenize();

                var rawWordsA = rawWords2.toArray();
                sent2Reader.close();

                List<CoreLabel> words = new List<CoreLabel>();
                for (int i = 0; i < rawWordsA.Length; i++)
                    words.Add((CoreLabel)rawWordsA[i]);

                string simplifiedSent = "";
                bool newSent = false;
                for (int i = 0; i < rawWordsA.Length - 1; i++)
                {
                    if (words[i] != null)
                    {
                        var bigram = (words[i] + " " + words[i + 1]).ToLower();
                        if (WordEmbedding.ContainsKey(bigram))
                        {
                            simplifiedSent += words[i + 1];
                            words[i + 1] = null;
                            newSent = true;
                        }
                        else
                            simplifiedSent += words[i] + " ";
                    }
                }
                if (words[words.Count - 1] != null)
                    simplifiedSent += words[words.Count - 1];


                if (newSent)
                {
                    sent2Reader = new StringReader(simplifiedSent);
                    rawWords2 = tokenizerFactory.getTokenizer(sent2Reader).tokenize();
                    rawWordsA = rawWords2.toArray();
                    sent2Reader.close();
                }
                var tree2 = lp.apply(rawWords2);
                List<Tree> VerbPhrases = new List<Tree>();
                GetPhrases("VB", tree2, ref VerbPhrases);

                var word2 = PTBTokenizer.ptbToken2Text(VerbPhrases[0].toString());
                var word = VerbPhrases[0].yieldWords().toString().Replace("[", "").Replace("]", "").ToLower();
                if (word == "is")
                {
                    List<Tree> sbar = new List<Tree>();
                    GetPhrases("SBAR", tree2, ref sbar);
                    if (sbar.Count > 0)
                    {
                        List<Tree> IN = new List<Tree>();
                        GetPhrases("IN", sbar[0], ref IN);
                        if (IN.Count > 0)
                        {
                            word = IN[0].yieldWords().toString().Replace("[", "").Replace("]", "").ToLower();
                        }
                    }
                }
                double[] vec = null;
                if (WordEmbedding.ContainsKey(word))
                    vec = WordEmbedding[word];
                else if (word.EndsWith("s"))
                {
                    word = word.Substring(0, word.Length - 1);
                    if (WordEmbedding.ContainsKey(word))
                        vec = WordEmbedding[word];
                }

                if (vec == null)
                    System.Diagnostics.Debug.Print("");
                var wordNode = CosSimularity(vec);
                wordNode.RootWord = word;
                wordNode.Parse = tree2;
                wordNode.DirectObjects = new List<string>();

                // Extract dependencies from lexical tree
                var tlp = new PennTreebankLanguagePack();
                var gsf = tlp.grammaticalStructureFactory();
                var gs = gsf.newGrammaticalStructure(tree2);
                var tdl = gs.typedDependenciesCCprocessed().toArray();

                foreach (TypedDependency o in tdl)
                {
                    string reln = o.reln().toString();
                    if (reln == "nsubj")
                        wordNode.Subject = o.dep().word();
                    if (reln == "dobj")
                        wordNode.DirectObjects.Add(o.dep().word());
                }

                // Extract collapsed dependencies from parsed tree
                var tp = new TreePrint("penn,typedDependenciesCollapsed");
                tp.printTree(tree2);

                thoughts.Add(wordNode);
            }
        }

        private CodeNode CosSimularity(double[] word)
        {
            CodeNode minNode = null;
            double globalMin = 0;
            foreach (var key in Keywords)
            {
                double minSim = 0;
                foreach (var root in key.Roots)
                {
                    double sim = 0;
                    var rootV = WordEmbedding[root];
                    for (int i = 0; i < word.Length; i++)
                        sim += rootV[i] * word[i];
                    if (sim > minSim)
                    {
                        minSim = sim;
                    }
                }
                if (minSim > globalMin)
                {
                    minNode = key;
                    globalMin = minSim;
                }
            }
            return minNode.Clone();
        }

        static Dictionary<string, double[]> WordEmbedding = new Dictionary<string, double[]>();

        static List<CodeNode> Keywords = new List<CodeNode>();

        static Singer()
        {
            var fileName = @"C:\Users\ochen\source\repos\Rockstar2\Rockstar2\Conceptnet\numberbatch-en.txt";
            string line;
            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(fileName);
            while ((line = file.ReadLine()) != null)
            {
                var parts = line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 301
                    && parts[0].Contains("#") == false
                    && parts[0].Contains("_") == false
                    && parts[0].StartsWith("1") == false
                    && parts[0].StartsWith("2") == false
                    && parts[0].StartsWith("3") == false
                    && parts[0].StartsWith("4") == false
                    && parts[0].StartsWith("5") == false
                    && parts[0].StartsWith("6") == false
                    && parts[0].StartsWith("7") == false
                    && parts[0].StartsWith("8") == false
                    && parts[0].StartsWith("9") == false
                    && parts[0].StartsWith("0") == false
                    )
                {
                    double[] vec = new double[300];
                    for (int i = 0; i < 300; i++)
                    {
                        vec[i] = double.Parse(parts[i + 1]);
                    }
                    WordEmbedding.Add(parts[0].Replace("_", " "), vec);
                }
            }


            Keywords.Add(new CodeNode() { Name = "Method", Roots = new string[] { "take", "takes", "taken", "requires", "require", "need", "needs" } });
            Keywords.Add(new CodeNode() { Name = "Return", Roots = new string[] { "give", "return", "throw" } });
            Keywords.Add(new CodeNode() { Name = "Assign", Roots = new string[] { "put", "shove", "insert" } });
            Keywords.Add(new CodeNode() { Name = "null", Roots = new string[] { "nothing", "nowhere", "nobody", "empty", "gone" } });
            Keywords.Add(new CodeNode() { Name = "true", Roots = new string[] { "right", "yes", "ok", "valid", "true" } });
            Keywords.Add(new CodeNode() { Name = "false", Roots = new string[] { "wrong", "no", "lies", "false" } });
            Keywords.Add(new CodeNode() { Name = "undefined", Roots = new string[] { "mysterious", "magical" } });
            Keywords.Add(new CodeNode() { Name = "AssignPoetic", Roots = new string[] { "is", "holds", "has" } });
            Keywords.Add(new CodeNode() { Name = "increment", Roots = new string[] { "build", "increase", "increment","raise" } });
            Keywords.Add(new CodeNode() { Name = "decrement", Roots = new string[] { "knock", "lower", "falling","down" } });
            Keywords.Add(new CodeNode() { Name = "if", Roots = new string[] { "while", "when", "if","until" } });
            Keywords.Add(new CodeNode() { Name = "output", Roots = new string[] { "say", "whisper", "shout" } });


        }

        public void Sing()
        {

        }

        public void GetPhrases(string tag, Tree phrase, ref List<Tree> tagphrases)
        {
            if (phrase.label().ToString().ToLower().Contains(tag.ToLower()))
            {
                tagphrases.Add(phrase);
            }
            foreach (var child in phrase.children())
            {
                GetPhrases(tag, child, ref tagphrases);
            }
        }
    }
}
