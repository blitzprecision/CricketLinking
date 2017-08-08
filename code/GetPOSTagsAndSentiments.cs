using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using edu.stanford.nlp.trees;

namespace CricketLinking
{
    class GetPOSTagsAndSentiments
    {
        static string dir = Global.baseDir;
        static bool coreferenced = false;
        public static List<HashSet<string>> countryPlayerNames = new List<HashSet<string>>();
        public static void loadCountryPlayerNames()
        {
            Dictionary<string, HashSet<string>> country2Tokens = new Dictionary<string, HashSet<string>>();
            StreamReader sr = new StreamReader(dir + "playerURLCountryName.txt");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                string[] toks = str.Split('\t');
                string country = toks[2].ToLower().Trim();
                string player = toks[3].ToLower().Trim();
                if (!country2Tokens.ContainsKey(country))
                    country2Tokens[country] = new HashSet<string>();
                foreach (string s in country.Split(' '))
                    country2Tokens[country].Add(s);
                foreach (string s in player.Split(' '))
                    country2Tokens[country].Add(s);
            }
            sr.Close();
            for (int i = 1; i <= Global.numMatches; i++)
            {
                StreamReader sr2 = new StreamReader(dir + "match" + i + "Scorecard.html");
                HashSet<string> countries = new HashSet<string>();
                while ((str = sr2.ReadLine()) != null)
                {
                    if (str.Contains("> v <a href"))
                    {
                        string tmp = GetCleanedArticleFiles.convertHTMLToText(str);
                        HashSet<string> set = new HashSet<string>();
                        string country1 = Regex.Split(tmp, " v ")[0].Trim().ToLower();
                        string country2 = Regex.Split(tmp, " v ")[1].Trim().ToLower();
                        foreach (string s in country2Tokens[country1])
                            set.Add(s);
                        foreach (string s in country2Tokens[country2])
                            set.Add(s);
                        countryPlayerNames.Add(set);
                        break;
                    }
                }
                sr2.Close();
            }

        }

        static void Main(string[] args)
        {
            StreamWriter swSentiment = new StreamWriter(dir+"mentionSentiments.txt");
                        loadCountryPlayerNames();
            //get coreferenced articles
            Dictionary<string, List<string>> lingOutput = new Dictionary<string, List<string>>();
            List<string> rawArticles = new List<string>();
            StreamReader sr1 = new StreamReader(dir + "outputArticles"+(coreferenced?"Coreferenced":"")+".tsv");
            string str = "";
            while ((str = sr1.ReadLine()) != null)
            {
                if (!str.Contains("Sentence"))
                    continue;
                string match = str.Split('\t')[0];
                if (!lingOutput.ContainsKey(match))
                    lingOutput[match] = new List<string>();
                lingOutput[match].Add(str);
            }
            sr1.Close();
            sr1 = new StreamReader(dir + "matchArticles"+(coreferenced?"Coreferenced":"")+".txt");
            while ((str = sr1.ReadLine()) != null)
                rawArticles.Add(str);
            sr1.Close();
            StreamWriter sw = new StreamWriter(dir + "matchArticles"+(coreferenced?"Coreferenced":"")+"POSTagged.txt");
            for (int i = 0; i < rawArticles.Count();i++)
            {
                Console.WriteLine("Processing Article: "+i);
                string[] toks = rawArticles[i].Split('\t');
                int match=int.Parse(toks[0])-1;
                HashSet<string> impTokens = countryPlayerNames[match];
                for (int j = 0; j < 6; j++)
                    sw.Write(toks[j] + "\t");
                string modifiedText = "";
                string [] rawTexts = Regex.Split(toks[6], "#p#");
                List<string> rawText = new List<string>();
                for (int r = 0; r < rawTexts.Length; r++)
                    if (!rawTexts[r].Trim().Equals(""))
                        rawText.Add(rawTexts[r].Trim());
                List<string> output = lingOutput[toks[0]+"_"+toks[1]];
                for (int oCount = 0; oCount < output.Count();oCount++)
                {
                    string o = output[oCount];
                    string r = rawText[oCount];
                    string[] lines = Regex.Split(o, "#NEWLINE#");
                    int count = 0;
                    List<List<string>> sentences = new List<List<string>>();
                    List<List<string>> sentence2SentimentData = new List<List<string>>();
                    List<string> coreferences = new List<string>();
                    List<List<string>> newSentences = new List<List<string>>();
                    List<Dictionary<int, int>> sentWord2NewWordPos = new List<Dictionary<int, int>>();
                    foreach (string line in lines)
                    {
                        if (line.Contains("Sentence #"))
                        {
                            string[] sentenceToks = Regex.Split(lines[count + 2], "\\[Text=");
                            List<string> sentToks = new List<string>();
                            foreach (string s in sentenceToks)
                            {
                                if (s.Equals(""))
                                    continue;
                                string pos = s.Split(' ')[3].Split('=')[1];
                                string entity = s.Split(' ')[5].Split('=')[1].Replace("]","");
                                sentToks.Add(s.Split(' ')[0]+"_"+pos+"_"+entity);
                            }
                            sentences.Add(sentToks);
                        }
                        if (line.Contains("ANNOTATEDTREE"))
                        {
                            List<string> sentimentData = new List<string>();
                            sentimentData.Add(line);
                            int iCount = 1;
                            while (!lines[count+iCount].Contains("SpeakerInfo"))
                            {
                                sentimentData.Add(lines[count + iCount]);
                                iCount++;
                            }
                            sentence2SentimentData.Add(sentimentData);
                        }
                        if (line.Contains(" -> ") && line.Contains("that is:"))
                        {
                            int good = 0;
                            string[] target = line.Split('"')[3].ToLower().Split(' ');
                            string source = line.Split('"')[1].ToLower();
                            foreach (string t in target)
                            {
                                if (impTokens.Contains(t) && !source.Contains(t))
                                {
                                    good = 1;
                                    break;
                                }
                            }
                            if (good == 1)
                                coreferences.Add(line);
                        }
                        count++;
                    }
                    int currentPos = 0;
                    foreach(List<string> sentence in sentences)
                    {
                        Dictionary<int, int> pos2NewWordPos = new Dictionary<int, int>();
                        List<string> newSentence = new List<string>();
                        int wpos = 0;
                        int newWpos = 0;
                        foreach(string word1 in sentence)
                        {
                            string word = word1.Split('_')[0];
                            string tag = word1.Split('_')[1] + "_" + word1.Split('_')[2];
                            if (word.Equals("-LRB-"))
                                word = "(";
                            if (word.Equals("-RRB-"))
                                word = ")";
                            if (word.Equals("-LSB-"))
                                word = "[";
                            if (word.Equals("-RSB-"))
                                word = "]";
                            if (word.Equals("``"))
                                word = "\"";
                            if (word.Equals("`"))
                                word = "'";
                            if (word.Equals("''"))
                                word = "\"";
                            string origWord = word+"_"+tag;
                            
                            int pos = r.IndexOf(word, currentPos);
                            string extraWord = r.Substring(currentPos,pos-currentPos).Trim();
                            if (!extraWord.Equals(""))
                            {
                                newWpos++;
                                newSentence.Add(extraWord);
                            }
                            newSentence.Add(origWord);
                            currentPos = pos + word.Length;
                            pos2NewWordPos[wpos] = newWpos;
                            wpos++;
                            newWpos++;
                        }
                        newSentences.Add(newSentence);
                        sentWord2NewWordPos.Add(pos2NewWordPos);
                    }
                    //adjust for mention tags after the text.
                    if (r.Length - 1 - currentPos > 0)
                    {
                        string extraWord2 = r.Substring(currentPos, r.Length - currentPos).Trim();
                        if (!extraWord2.Equals(""))
                            newSentences[newSentences.Count()-1].Add(extraWord2);
                    }
                    //get the mentions list

                    int sentID = 0;
                    Dictionary<string, string> mentions2sentIDs = new Dictionary<string, string>();
                    int mentionStart = 0;
                    string tmpMention = "";
                    int sentStart=0;
                    int sentEnd=0;
                    foreach (List<string> l in newSentences)
                    {
                        foreach (string s in l)
                        {
                            if (s.Contains("</m"))
                            {
                                sentEnd=sentID;
                                tmpMention=tmpMention.Replace("(", "-LRB-").Replace(")", "-RRB-").Replace("[", "-LSB-").Replace("]", "-RSB-");
                                if(coreferenced)
                                    tmpMention=tmpMention.Replace("\"","''");
                                else
                                    tmpMention = tmpMention.Replace("\"", "``");
                                mentions2sentIDs[tmpMention] = sentStart + "_" + sentEnd;
                                tmpMention = "";
                                mentionStart = 0;
                            }
                            if (mentionStart == 1)
                                tmpMention += s + " ";
                            if (s.Contains("<m"))
                            {
                                mentionStart = 1;
                                sentStart=sentID;
                                int pos = s.IndexOf("<m");
                                tmpMention = s.Substring(pos) + "\t";
                            }
                        }
                        sentID++;
                    }
                    sentID = 0;
                    foreach (List<string> l in newSentences)
                    {
                        //get corresponding sentiments and parse tree.
                        string tree = sentence2SentimentData[sentID][0].Replace("ANNOTATEDTREE::", "");
                        Tree t = Tree.valueOf(tree);
                        List<Tree> treeList = new List<Tree>();
                        int pointer=0;
                        treeList.Add(t);
                        while(pointer!=treeList.Count())
                        {
                            Tree[] c = treeList[pointer].children();
                            foreach (Tree t1 in c)
                            {
                                if (t1.size() == 1)
                                    continue;
                                treeList.Add(t1);
                            }
                            pointer++;
                        }
                        foreach (string m in mentions2sentIDs.Keys)
                        {
                            string sRange = mentions2sentIDs[m];
                            int sentStart2 = int.Parse(sRange.Split('_')[0]);
                            int sentEnd2 = int.Parse(sRange.Split('_')[1]);
                            int maxNodeNumber = -1;
                            if (sentStart2 <= sentID && sentEnd2 >= sentID && sentStart2 != sentEnd2)
                                maxNodeNumber = 0;
                            else if (sentStart2 == sentID && sentEnd2 == sentID)
                            {
                                for (int z = 0; z < treeList.Count(); z++)
                                {
                                    Tree t1 = treeList[z];
                                    java.util.List list = t1.getLeaves();
                                    int currentNodeNumber = int.Parse(t1.label() + "");
                                    if (contains(m.Split('\t')[1], list) && currentNodeNumber > maxNodeNumber)
                                        maxNodeNumber = currentNodeNumber;
                                }
                            }
                            else
                                continue;
                            swSentiment.WriteLine(toks[0]+"\t"+toks[1]+"\t"+m+"\t"+sentence2SentimentData[sentID][maxNodeNumber+1]);
                        }
                        sentID++;
                    }
                    sentences = newSentences;
                    foreach(List<string> l in sentences)
                        foreach(string s in l)
                            modifiedText += s+" ";
                    modifiedText += "#p# ";
                }
                sw.WriteLine(Regex.Replace(modifiedText, "\\s+", " "));
            }
            sw.Close();
            swSentiment.Close();
        }

        private static bool contains(string m, java.util.List list)
        {
            HashSet<string> set = new HashSet<string>(m.Split(' '));
            HashSet<string> set2 = new HashSet<string>();
            for (int i = 0; i < list.size(); i++)
                set2.Add((string)(list.get(i).ToString()));
                foreach (string s in set)
                {
                    if (s.Equals(""))
                        continue;
                    if (!set2.Contains(s.Split('_')[0]))
                        return false;
                }
            return true;
        }
    }
}
