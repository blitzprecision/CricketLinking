using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CricketLinking
{
    class GetCoreferencedArticles
    {
        static string dir = Global.baseDir;
        public static List<HashSet<string>> countryPlayerNames = new List<HashSet<string>>();
        public static void loadCountryPlayerNames()
        {
            Dictionary<string, HashSet<string>> country2Tokens = new Dictionary<string, HashSet<string>>();
            StreamReader sr = new StreamReader(dir + "playerURLCountryName.txt");
            string str = "";
            while((str=sr.ReadLine())!=null)
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
                        string country2=Regex.Split(tmp, " v ")[1].Trim().ToLower();
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
            loadCountryPlayerNames();
            //get coreferenced articles
            Dictionary<string, List<string>> lingOutput = new Dictionary<string, List<string>>();
            List<string> rawArticles = new List<string>();
            StreamReader sr1 = new StreamReader(dir + "outputArticles.tsv");
            string str = "";
            while ((str = sr1.ReadLine()) != null)
            {
                string match = str.Split('\t')[0];
                if (!lingOutput.ContainsKey(match))
                    lingOutput[match] = new List<string>();
                lingOutput[match].Add(str);
            }
            sr1.Close();
            sr1 = new StreamReader(dir + "matchArticles.txt");
            while ((str = sr1.ReadLine()) != null)
                rawArticles.Add(str);
            sr1.Close();
            StreamWriter sw = new StreamWriter(dir+"matchArticlesCoreferenced.txt");
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
                    List<string> coreferences = new List<string>();
                    List<List<string>> newSentences = new List<List<string>>();
                    List<Dictionary<int, int>> sentWord2NewWordPos = new List<Dictionary<int, int>>();
                    HashSet<string> PRPTokens = new HashSet<string>();
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
                                sentToks.Add(s.Split(' ')[0]);
                                if (pos.Equals("PRP") || pos.Equals("PRP$") || pos.Equals("WP") || pos.Equals("WP$"))
                                    PRPTokens.Add(s.Split(' ')[0]);
                            }
                            sentences.Add(sentToks);
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
                            good = 0;
                            foreach (string t in source.Split(' '))
                            {
                                if (PRPTokens.Contains(t))
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
                            string word = word1;
                            if (word1.Equals("-LRB-"))
                                word = "(";
                            if (word1.Equals("-RRB-"))
                                word = ")";
                            if (word1.Equals("-LSB-"))
                                word = "[";
                            if (word1.Equals("-RSB-"))
                                word = "]";
                            if (word1.Equals("``"))
                                word = "\"";
                            if (word1.Equals("`"))
                                word = "'";
                            if (word1.Equals("''"))
                                word = "\"";
                            int pos = r.IndexOf(word, currentPos);
                            string extraWord = r.Substring(currentPos,pos-currentPos).Trim();
                            if (!extraWord.Equals(""))
                            {
                                newWpos++;
                                newSentence.Add(extraWord);
                            }
                            newSentence.Add(word);
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
                    //perform coreference resolution
                    sentences = new List<List<string>>();
                    foreach (List<string> l in newSentences)
                    { 
                        sentences.Add(new List<string>());
                        foreach (string s in l)
                            sentences[sentences.Count() - 1].Add(s);
                    }
                    foreach (string c in coreferences)
                    {
                        int fromSentence = int.Parse(c.Split(',')[0].Split('(')[1]) - 1;
                        int toSentence = int.Parse(c.Split(',')[3].Split('(')[1]) - 1;
                        int fromWordStart = sentWord2NewWordPos[fromSentence][int.Parse(c.Split(',')[2].Split('[')[1]) - 1];
                        int fromWordEnd = sentWord2NewWordPos[fromSentence][int.Parse(c.Split(',')[3].Split(']')[0]) - 2];
                        int toWordStart = sentWord2NewWordPos[toSentence][int.Parse(c.Split(',')[5].Split('[')[1]) - 1];
                        int toWordEnd = sentWord2NewWordPos[toSentence][int.Parse(c.Split(',')[6].Split(']')[0]) - 2];
                        string tmp = "";
                        for (int n = fromWordStart; n <= fromWordEnd; n++)
                            tmp += newSentences[fromSentence][n]+" ";
                        if (tmp.Contains("<m") || tmp.Contains("</m"))
                            continue;
                        for (int n = fromWordStart + 1; n <= fromWordEnd; n++)
                            sentences[fromSentence][n] = "";
                        tmp = "";
                        for (int n = toWordStart; n <= toWordEnd; n++)
                            tmp += Regex.Replace(newSentences[toSentence][n], "<[^>]*>","") + " ";
                        sentences[fromSentence][fromWordStart] = tmp;
                    }
                    foreach(List<string> l in sentences)
                        foreach(string s in l)
                            modifiedText += s+" ";
                    modifiedText += "#p# ";
                }
                sw.WriteLine(Regex.Replace(modifiedText, "\\s+", " "));
            }
            sw.Close();
        }
    }
}
