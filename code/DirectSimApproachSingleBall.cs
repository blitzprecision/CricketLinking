﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CricketLinking
{
    class DirectSimApproachSingleBall
    {
        static string dir = Global.baseDir;

        public static Dictionary<int, List<string>> match2innings1BatsmenNames = new Dictionary<int, List<string>>();
        public static Dictionary<int, List<string>> match2innings2BatsmenNames = new Dictionary<int, List<string>>();
        public static Dictionary<int, string> match2firstCountry = new Dictionary<int, string>();
        public static Dictionary<int, string> match2secondCountry = new Dictionary<int, string>();
        public static Dictionary<int, string> match2winningCountry = new Dictionary<int, string>();
        static Dictionary<string, Dictionary<string, Dictionary<string, string>>> match2country2String2StdName = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        public static Dictionary<string, string> player2FullName = new Dictionary<string, string>();
        public static Dictionary<int, HashSet<string>> match2Team1Tokens = new Dictionary<int, HashSet<string>>();
        public static Dictionary<int, HashSet<string>> match2Team2Tokens = new Dictionary<int, HashSet<string>>();
        public static Dictionary<int, HashSet<string>> match2CountryTokens = new Dictionary<int, HashSet<string>>();
        public static Dictionary<string, HashSet<string>> country2Players = new Dictionary<string, HashSet<string>>();

        public static Dictionary<int, List<string>> matchNum2Commentary = new Dictionary<int, List<string>>();
        public static Dictionary<string, string> instance2POSTaggedMentionText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2POSTaggedCoreferencedMentionText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2MentionText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2CoreferencedMentionText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2MentionSentText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2CoreferencedMentionSentText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2Label = new Dictionary<string, string>();

        public static Dictionary<string, double> idf = new Dictionary<string, double>();
        static void Main(string[] args)
        {
            loadPlayerNames();
            Console.WriteLine("Done loading player names");
            loadCommentary();
            Console.WriteLine("Done loading commentary");
            loadLabels();
            Console.WriteLine("Done loading labels");
            loadMentionText();
            Console.WriteLine("Done loading mention text");
            computeIDF();
            //compute best ball for each mention
            string[] corefs = new string[] { "NoCoreference", "Coreference" };//
            string[] sims = new string[] {"Jaccard", "TFIDF"};//Jaccard/TFIDF
            string[] methods = new string[]{"DirSim", "StructuredSim"};//DirSim/StructuredSim
            string[] commentaryContexts = { "0CommContext", "1CommContext", "2CommContext", "overCommContext" };
            string[] mentionContexts = { "mentionOnly", "mentionSentence" };//, "mentionOnly","mentionSentence"
            foreach (string mc in mentionContexts)
            {
                foreach (string commentaryContext in commentaryContexts)
                {
                    foreach (string coref in corefs)
                    {
                        foreach (string sim in sims)
                        {
                            foreach (string method in methods)
                            {
                                StreamWriter sw = new StreamWriter(dir + "SB_" + method + "_" + sim + "_" + coref + "_"+commentaryContext+"_"+mc+"_mention2Balls.txt");
                                int mentionCount = 0;
                                foreach (string instance in instance2Label.Keys)
                                {
                                    mentionCount++;
                                    Console.WriteLine("MC:"+mc+" CommContext: " + commentaryContext + " Coref: " + coref + " Sim Measure: " + sim + " Method: " + method + " Processing mention: " + instance + " No: " + mentionCount);
                                    int match = int.Parse(instance.Split('_')[0]);
                                    if (instance2Label[instance].Equals("M"))
                                        continue;
                                    string mention = "";
                                    if (mc.Equals("mentionOnly"))
                                    {
                                        if (coref.Equals("NoCoreference"))
                                            mention = instance2MentionText[instance];
                                        else
                                            mention = instance2CoreferencedMentionText[instance];
                                    }
                                    else if (mc.Equals("mentionSentence"))
                                    {
                                        if (coref.Equals("NoCoreference"))
                                            mention = instance2MentionSentText[instance];
                                        else
                                            mention = instance2CoreferencedMentionSentText[instance];
                                    }
                                    Dictionary<string, double> dict = new Dictionary<string, double>();
                                    for (int ballIndex = 0; ballIndex < matchNum2Commentary[match].Count(); ballIndex++)
                                    {
                                        string b = matchNum2Commentary[match][ballIndex];
                                        string commentary = b.Split('\t')[26]; // b.Replace('\t', ' ');//b.Split('\t')[26];
                                        if (method.Equals("DirSim"))
                                            commentary = b.Split('\t')[26];
                                        else if (method.Equals("StructuredSim"))
                                            commentary = b.Replace('\t', ' ');
                                        if (commentaryContext.Equals("1CommContext"))
                                        {
                                            if (method.Equals("DirSim"))
                                            {
                                                if (ballIndex > 0)
                                                    commentary += matchNum2Commentary[match][ballIndex - 1].Split('\t')[26];
                                                if (ballIndex < matchNum2Commentary[match].Count() - 1)
                                                    commentary += matchNum2Commentary[match][ballIndex + 1].Split('\t')[26];
                                            }
                                            else if (method.Equals("StructuredSim"))
                                            {
                                                if (ballIndex > 0)
                                                    commentary += matchNum2Commentary[match][ballIndex - 1].Replace('\t', ' ');
                                                if (ballIndex < matchNum2Commentary[match].Count() - 1)
                                                    commentary += matchNum2Commentary[match][ballIndex + 1].Replace('\t', ' ');
                                            }
                                        }
                                        if (commentaryContext.Equals("2CommContext"))
                                        {
                                            if (method.Equals("DirSim"))
                                            {
                                                if (ballIndex > 1)
                                                    commentary += matchNum2Commentary[match][ballIndex - 2].Split('\t')[26];
                                                if (ballIndex < matchNum2Commentary[match].Count() - 2)
                                                    commentary += matchNum2Commentary[match][ballIndex + 2].Split('\t')[26];
                                            }
                                            else if (method.Equals("StructuredSim"))
                                            {
                                                if (ballIndex > 1)
                                                    commentary += matchNum2Commentary[match][ballIndex - 2].Replace('\t', ' ');
                                                if (ballIndex < matchNum2Commentary[match].Count() - 2)
                                                    commentary += matchNum2Commentary[match][ballIndex + 2].Replace('\t', ' ');
                                            }
                                        }
                                        if (commentaryContext.Equals("overCommContext"))
                                        {
                                            string curOver = matchNum2Commentary[match][ballIndex].Split('\t')[2].Split('.')[0];
                                            if (method.Equals("DirSim"))
                                            {
                                                for (int y = ballIndex - 1; y >= 0; y--)
                                                    if (matchNum2Commentary[match][y].Split('\t')[2].Split('.')[0].Equals(curOver))
                                                        commentary += matchNum2Commentary[match][y].Split('\t')[26];
                                                    else
                                                        break;
                                                for (int y = ballIndex + 1; y < matchNum2Commentary[match].Count(); y++)
                                                    if (matchNum2Commentary[match][y].Split('\t')[2].Split('.')[0].Equals(curOver))
                                                        commentary += matchNum2Commentary[match][y].Split('\t')[26];
                                                    else
                                                        break;
                                            }
                                            else if (method.Equals("StructuredSim"))
                                            {
                                                for (int y = ballIndex - 1; y >= 0; y--)
                                                    if (matchNum2Commentary[match][y].Split('\t')[2].Split('.')[0].Equals(curOver))
                                                        commentary += matchNum2Commentary[match][y].Replace('\t', ' ');
                                                    else
                                                        break;
                                                for (int y = ballIndex + 1; y < matchNum2Commentary[match].Count(); y++)
                                                    if (matchNum2Commentary[match][y].Split('\t')[2].Split('.')[0].Equals(curOver))
                                                        commentary += matchNum2Commentary[match][y].Replace('\t', ' ');
                                                    else
                                                        break;
                                            }
                                        }
                                        string ballNum = b.Split('\t')[1] + ":" + b.Split('\t')[2];
                                        if (sim.Equals("Jaccard"))
                                            dict[ballNum] = computeJaccardSimilarity(mention, commentary);
                                        else
                                            dict[ballNum] = computeTFIDFCosineSimilarity(mention, commentary);
                                    }
                                    List<KeyValuePair<string, double>> myList = dict.ToList();
                                    myList.Sort((x, y) => y.Value.CompareTo(x.Value));
                                    sw.Write(instance + "\t");
                                    for (int i = 0; i < 10; i++)
                                        sw.Write(myList[i].Key + "\t" + myList[i].Value + "\t");
                                    sw.WriteLine();
                                }
                                sw.Close();
                            }
                        }
                    }
                }
            }
        }

        private static void computeIDF()
        {
            for (int i = 1; i <= 30; i++)
            {
                List<string> commentary = matchNum2Commentary[i];
                foreach (string b in commentary)
                {
                    string[] commentaryToks = b.Split('\t')[26].ToLower().Split(' ');
                    foreach (string c in commentaryToks)
                        if (idf.ContainsKey(c))
                            idf[c]++;
                        else
                            idf[c] = 1;
                }
            }
            Dictionary<string, double> tmpIDF = new Dictionary<string, double>();
            foreach(string s in idf.Keys)
                tmpIDF[s] = Math.Log(1+ 1.0 / idf[s]);
            idf = tmpIDF;
        }
        private static double computeJaccardSimilarity(string mention, string commentary)
        {
            HashSet<string> set1 = new HashSet<string>(mention.ToLower().Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries));
            HashSet<string> set2 = new HashSet<string>(commentary.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
            double intersection = 0;
            foreach (string s in set1)
                if (set2.Contains(s))
                    intersection++;
            foreach (string s in set1)
                set2.Add(s);
            int union = set2.Count();
            return intersection/union;
        }
        private static double computeTFIDFCosineSimilarity(string mention, string commentary)
        {
            Dictionary<string, double> mentionTFIDF = new Dictionary<string, double>();
            Dictionary<string, double> commentaryTFIDF = new Dictionary<string, double>();
            string[] toks = mention.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in toks)
            {
                if (mentionTFIDF.ContainsKey(t))
                    mentionTFIDF[t]++;
                else
                    mentionTFIDF[t] = 1;
            }
            toks = commentary.ToLower().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in toks)
            {
                if (commentaryTFIDF.ContainsKey(t))
                    commentaryTFIDF[t]++;
                else
                    commentaryTFIDF[t] = 1;
            }
            List<double> l1 = new List<double>();
            List<double> l2 = new List<double>();
            HashSet<string> unionSet = new HashSet<string>(mentionTFIDF.Keys);
            unionSet.UnionWith(commentaryTFIDF.Keys);
            foreach (string t in unionSet)
            {
                if (!idf.ContainsKey(t))
                    idf[t] = Math.Log(1 + 1);//assume that it was present in only 1 document.
                if (mentionTFIDF.ContainsKey(t))
                    l1.Add(mentionTFIDF[t] * idf[t]);
                else
                    l1.Add(0);
                if (commentaryTFIDF.ContainsKey(t))
                    l2.Add(commentaryTFIDF[t] * idf[t]);
                else
                    l2.Add(0);
            }
            return GetCosineSimilarity(l1, l2);
        }
        public static double GetCosineSimilarity(List<double> V1, List<double> V2)
        {
            int N = 0;
            N = ((V2.Count < V1.Count) ? V2.Count : V1.Count);
            double dot = 0.0d;
            double mag1 = 0.0d;
            double mag2 = 0.0d;
            for (int n = 0; n < N; n++)
            {
                dot += V1[n] * V2[n];
                mag1 += Math.Pow(V1[n], 2);
                mag2 += Math.Pow(V2[n], 2);
            }

            return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }
        private static void loadLabels()
        {
            StreamReader sr = new StreamReader(dir + "linkedLabels.tsv");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                string[] toks = str.Split('\t');
                int matchNumber = int.Parse(toks[0]);
                string articleNumber = toks[1];
                string mentionNum = toks[2].Replace("m", "");
                string mention = toks[3];
                string classLabel = toks[8];
                if (!classLabel.Trim().Equals(""))
                    instance2Label[matchNumber + "_" + articleNumber + "_" + mentionNum] = classLabel;
            }
            sr.Close();
        }
        private static string refineMentions(string text)
        {
            //look for numbers tags and replace all numbers with text. 
            string modified = "";
            string[] toks = text.Split(' ');
            for (int i = 0; i < toks.Length;i++)
            {
                if(toks[i].Contains("_") && toks[i].Split('_')[2].Equals("NUMBER"))
                {
                    string tmp = toks[i]+" ";
                    string tmp1 = toks[i].Split('_')[0];
                    i++;
                    while (i < toks.Length && toks[i].Contains("_") && toks[i].Split('_')[2].Equals("NUMBER"))
                    {
                        tmp += toks[i] + " ";
                        tmp1 += " " + toks[i].Split('_')[0];
                        i++;
                    }
                    int num = replaceWordsByNumbers(tmp1.Trim());
                    if (num == -1)
                        modified += tmp.Trim() + " ";
                    else
                        modified += num + "_CD_NUMBER ";
                    i--;
                }
                else
                {
                    modified += toks[i] + " ";
                }
            }
            return modified.Trim();
        }
        private static int replaceWordsByNumbers(string text)
        {
            if (text.Equals("hundred") || text.Equals("century"))
                text = "one hundred";
            if (text.Equals("half-century") || text.Equals("half century"))
                text = "fifty";
            try
            {
                string[] units = new string[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                string[] tens = new string[] { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
                string[] scales = new string[] { "hundred", "thousand", "million", "billion", "trillion" };

                Dictionary<string, ScaleIncrementPair> numWord = new Dictionary<string, ScaleIncrementPair>();
                numWord.Add("and", new ScaleIncrementPair(1, 0));
                for (int i = 0; i < units.Length; i++)
                {
                    numWord.Add(units[i], new ScaleIncrementPair(1, i));
                }

                for (int i = 1; i < tens.Length; i++)
                {
                    numWord.Add(tens[i], new ScaleIncrementPair(1, i * 10));
                }

                for (int i = 0; i < scales.Length; i++)
                {
                    if (i == 0)
                        numWord.Add(scales[i], new ScaleIncrementPair(100, 0));
                    else
                        numWord.Add(scales[i], new ScaleIncrementPair((int)Math.Pow(10, (i * 3)), 0));
                }

                int current = 0;
                int result = 0;

                foreach (var word in text.Split(new char[] { ' ', '-', '—' }))
                {
                    ScaleIncrementPair scaleIncrement = numWord[word];
                    current = current * scaleIncrement.scale + scaleIncrement.increment;
                    if (scaleIncrement.scale > 100)
                    {
                        result += current;
                        current = 0;
                    }
                }
                return result + current;
            }
            catch
            {
                return -1;
            }
        }

        public struct ScaleIncrementPair
        {
            public int scale;
            public int increment;
            public ScaleIncrementPair(int s, int i)
            {
                scale = s;
                increment = i;
            }
        }
        private static void loadMentionText()
        {
            StreamReader sr = new StreamReader(dir + "matchArticlesCoreferencedPOSTagged.txt");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                string match_article = str.Split('\t')[0] + "_" + str.Split('\t')[1];
                int match = int.Parse(str.Split('\t')[0]);
                string firstBatCountry = match2firstCountry[match];
                string secondBatCountry = match2secondCountry[match];
                string text = str.Split('\t')[6];
                string origText = text;
                string mentionNum = "";
                string mention = "";
                if (int.Parse(str.Split('\t')[0]) > 30)
                    continue;
                text = text.Replace("<m", "\n<m").Replace("</m", "\n</m");
                string[] textToks = text.Split('\n');
                foreach (string t in textToks)
                {
                    if (!t.StartsWith("<m"))
                        continue;
                    mentionNum = t.Split('>')[0].Replace("<m", "");
                    mention = t.Replace("<m" + mentionNum + ">", "");
                    instance2POSTaggedCoreferencedMentionText[match_article + "_" + mentionNum] = refineMentions(mention.Trim());
                    string[] mentionToks = instance2POSTaggedCoreferencedMentionText[match_article + "_" + mentionNum].Split(' ');
                    string tmp = "";
                    string prevStdName = "";
                    foreach (string m in mentionToks)
                    {
                        string mt = m.Split('_')[0];
                        if ((match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt.Split('_')[0]) || match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt.Split('_')[0])))
                        {
                            string stdPlayerName = "";
                            if (match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt.Split('_')[0]))
                                stdPlayerName = match2country2String2StdName[match + ""][firstBatCountry][mt.Split('_')[0]].Replace(' ', '_');
                            if (match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt.Split('_')[0]))
                                stdPlayerName = match2country2String2StdName[match + ""][secondBatCountry][mt.Split('_')[0]].Replace(' ', '_');
                            if (!stdPlayerName.Equals(prevStdName))
                            {
                                tmp += stdPlayerName + " ";
                                prevStdName = stdPlayerName;
                            }
                        }
                        else
                        {
                            tmp += m.Split('_')[0] + " ";
                            prevStdName = "";
                        }
                    }
                    instance2CoreferencedMentionText[match_article + "_" + mentionNum] = tmp;
                }

                //get the mention Sentences
                mentionNum = "";
                text = origText.Replace("#p#", ".").Replace("_CD_NUMBER", "CDNUMBER");
                string tmpText = "";
                foreach (string t in text.Split(' '))
                    tmpText += t.Split('_')[0] + " ";
                text = tmpText.Trim();
                textToks = text.Split(new char []{'.', '?', '!'}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string t in textToks)
                {
                    string sentence = t;
                    //find mentions.
                    Regex r2 = new Regex(@"<m\d+>");
                    if (!r2.IsMatch(sentence))
                        continue;
                    List<string> mentions = new List<string>();
                    foreach (Match a in r2.Matches(sentence))
                        mentions.Add(a.Value.Replace("<m", "").Replace(">", ""));
                    sentence = refineMentions(Regex.Replace(Regex.Replace(sentence.Replace("CDNUMBER", "_CD_NUMBER"), "<m[^>]+>", ""), "</m[^>]+>", "").Trim());
                    sentence = sentence.Replace("_CD_NUMBER", "");
                    string[] sentenceToks = sentence.Split(' ');
                    string tmp = "";
                    string prevStdName = "";
                    foreach (string m in sentenceToks)
                    {
                        string mt = m;
                        if ((match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt) || match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt)))
                        {
                            string stdPlayerName = "";
                            if (match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt))
                                stdPlayerName = match2country2String2StdName[match + ""][firstBatCountry][mt].Replace(' ', '_');
                            if (match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt))
                                stdPlayerName = match2country2String2StdName[match + ""][secondBatCountry][mt].Replace(' ', '_');
                            if (!stdPlayerName.Equals(prevStdName))
                            {
                                tmp += stdPlayerName + " ";
                                prevStdName = stdPlayerName;
                            }
                        }
                        else
                        {
                            tmp += m + " ";
                            prevStdName = "";
                        }
                    }
                    foreach (string mm in mentions)
                        instance2CoreferencedMentionSentText[match_article + "_" + mm] = tmp;
                }
            }
            sr.Close();
            sr = new StreamReader(dir + "matchArticlesPOSTagged.txt");
            while ((str = sr.ReadLine()) != null)
            {
                string match_article = str.Split('\t')[0] + "_" + str.Split('\t')[1];
                int match = int.Parse(str.Split('\t')[0]);
                string firstBatCountry = match2firstCountry[match];
                string secondBatCountry = match2secondCountry[match];
                string text = str.Split('\t')[6];
                string origText = text;
                string mentionNum = "";
                string mention = "";
                if (int.Parse(str.Split('\t')[0]) > 30)
                    continue;
                text = text.Replace("<m", "\n<m").Replace("</m", "\n</m");
                string[] textToks = text.Split('\n');
                foreach (string t in textToks)
                {
                    if (!t.StartsWith("<m"))
                        continue;
                    mentionNum = t.Split('>')[0].Replace("<m", "");
                    mention = t.Replace("<m" + mentionNum + ">", "");
                    instance2POSTaggedMentionText[match_article + "_" + mentionNum] = refineMentions(mention.Trim());
                    string[] mentionToks = instance2POSTaggedMentionText[match_article + "_" + mentionNum].Split(' ');
                    string tmp = "";
                    foreach (string m in mentionToks)
                    {
                        string mt = m.Split('_')[0];
                        string prevStdName = "";
                        if ((match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt.Split('_')[0])||match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt.Split('_')[0])))
                        {
                            string stdPlayerName = "";
                            if(match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt.Split('_')[0]))
                                stdPlayerName=match2country2String2StdName[match + ""][firstBatCountry][mt.Split('_')[0]].Replace(' ', '_');
                            if(match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt.Split('_')[0]))
                                stdPlayerName = match2country2String2StdName[match + ""][secondBatCountry][mt.Split('_')[0]].Replace(' ', '_');
                            if (!stdPlayerName.Equals(prevStdName))
                            {
                                tmp += stdPlayerName + " ";
                                prevStdName = stdPlayerName;
                            }
                        }
                        else
                            tmp += m.Split('_')[0] + " ";
                    }
                    instance2MentionText[match_article + "_" + mentionNum] = tmp;
                }
                mentionNum = "";
                text = origText.Replace("#p#", ".").Replace("_CD_NUMBER", "CDNUMBER");
                string tmpText = "";
                foreach (string t in text.Split(' '))
                    tmpText += t.Split('_')[0] + " ";
                text = tmpText.Trim();
                textToks = text.Split(new char[] { '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string t in textToks)
                {
                    string sentence = t;
                    //find mentions.
                    Regex r2 = new Regex(@"<m\d+>");
                    if (!r2.IsMatch(sentence))
                        continue;
                    List<string> mentions = new List<string>();
                    foreach (Match a in r2.Matches(sentence))
                        mentions.Add(a.Value.Replace("<m", "").Replace(">", ""));
                    sentence = refineMentions(Regex.Replace(Regex.Replace(sentence.Replace("CDNUMBER", "_CD_NUMBER"), "<m[^>]+>", ""), "</m[^>]+>", "").Trim());
                    sentence = sentence.Replace("_CD_NUMBER", "");
                    string[] sentenceToks = sentence.Split(' ');
                    string tmp = "";
                    string prevStdName = "";
                    foreach (string m in sentenceToks)
                    {
                        string mt = m;
                        if ((match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt) || match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt)))
                        {
                            string stdPlayerName = "";
                            if (match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt))
                                stdPlayerName = match2country2String2StdName[match + ""][firstBatCountry][mt].Replace(' ', '_');
                            if (match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt))
                                stdPlayerName = match2country2String2StdName[match + ""][secondBatCountry][mt].Replace(' ', '_');
                            if (!stdPlayerName.Equals(prevStdName))
                            {
                                tmp += stdPlayerName + " ";
                                prevStdName = stdPlayerName;
                            }
                        }
                        else
                        {
                            tmp += m + " ";
                            prevStdName = "";
                        }
                    }
                    foreach (string mm in mentions)
                        instance2MentionSentText[match_article + "_" + mm] = tmp;
                }
            }
            sr.Close();
        }

        public static void loadCommentary()
        {
            StreamReader sr = new StreamReader(dir + "matchCommentary.txt");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                str = str.Replace(",", " ,");
                int match = int.Parse(str.Split('\t')[0]);
                if (match > 30)
                    continue;
                if (!matchNum2Commentary.ContainsKey(match))
                    matchNum2Commentary[match] = new List<string>();
                string[] toks = str.Split('\t');
                string[] mentionToks = toks[26].Split(' ');
                string firstBatCountry = toks[3];
                string secondBatCountry = toks[4];
                string tmp = "";
                for (int i = 0; i <= 25; i++)
                    tmp += toks[i] + "\t";
                string prevStdName = "";
                foreach (string m in mentionToks)
                {
                    string mt = m.Split('_')[0];
                    if ((match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt.Split('_')[0]) || match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt.Split('_')[0])))
                    {
                        string stdPlayerName = "";
                        if (match2country2String2StdName[match + ""][firstBatCountry].ContainsKey(mt.Split('_')[0]))
                            stdPlayerName = match2country2String2StdName[match + ""][firstBatCountry][mt.Split('_')[0]].Replace(' ', '_');
                        if (match2country2String2StdName[match + ""][secondBatCountry].ContainsKey(mt.Split('_')[0]))
                            stdPlayerName = match2country2String2StdName[match + ""][secondBatCountry][mt.Split('_')[0]].Replace(' ', '_');
                        if (!stdPlayerName.Equals(prevStdName))
                        {
                            tmp += stdPlayerName + " ";
                            prevStdName = stdPlayerName;
                        }
                    }
                    else
                    {
                        tmp += m.Split('_')[0] + " ";
                        prevStdName = "";
                    }
                }
                matchNum2Commentary[match].Add(tmp.Trim());
            }
            sr.Close();
        }
        private static void loadPlayerNames()
        {
            StreamReader sr2 = new StreamReader(dir + "playerURLCountryName.txt");
            string str = "";
            while ((str = sr2.ReadLine()) != null)
            {
                string[] toks = str.Split('\t');
                string player = toks[0];
                string country = toks[2];
                if (!country2Players.ContainsKey(country))
                    country2Players[country] = new HashSet<string>();
                country2Players[country].Add(player);
                player2FullName[player] = toks[3].Replace('(', ' ').Replace(')', ' ');
            }
            sr2.Close();
            for (int i = 1; i <= Global.numMatches; i++)
            {
                getPlayerNames(i);
                Dictionary<string, Dictionary<string, string>> country2String2StdName = new Dictionary<string, Dictionary<string, string>>();
                StreamReader sr = new StreamReader(dir + "playerURLCountryName.txt");
                while ((str = sr.ReadLine()) != null)
                {
                    string[] toks = str.Split('\t');
                    string stdName = toks[0];
                    if (!match2innings1BatsmenNames[i].Contains(stdName) && !match2innings2BatsmenNames[i].Contains(stdName))
                        continue;
                    string country = toks[2];
                    string fullName = toks[3];
                    string fullName2 = "";
                    if(fullName.Contains('('))
                    {
                        fullName2 = fullName.Split('(')[1].Split(')')[0].Trim();
                        fullName = fullName.Split('(')[0].Trim();
                    }
                    string[] fullNameToks = fullName.Split(' ');
                    Dictionary<string, string> string2StdPlayerName = new Dictionary<string, string>();
                    if (country2String2StdName.ContainsKey(country))
                        string2StdPlayerName = country2String2StdName[country];
                    foreach (string s in fullNameToks)
                        string2StdPlayerName[s] = stdName;
                    string2StdPlayerName[fullName] = stdName;
                    string2StdPlayerName[stdName] = stdName;
                    if (fullNameToks.Count() == 3)
                        string2StdPlayerName[fullNameToks[1] + " " + fullNameToks[2]] = stdName;
                    if (!fullName2.Equals(""))
                    {
                        string[] fullNameToks2 = fullName2.Split(' ');
                        foreach (string s in fullNameToks2)
                            string2StdPlayerName[s] = stdName;
                        string2StdPlayerName[fullName2] = stdName;
                        if (fullNameToks2.Count() == 3)
                            string2StdPlayerName[fullNameToks2[1] + " " + fullNameToks2[2]] = stdName;
                    }
                    country2String2StdName[country] = string2StdPlayerName;
                }
                sr.Close();
                match2country2String2StdName[i + ""] = country2String2StdName;
            }
        }
        private static void getPlayerNames(int match)
        {
            match2innings1BatsmenNames[match] = new List<string>();
            match2innings2BatsmenNames[match] = new List<string>();
            match2firstCountry[match] = "";
            match2secondCountry[match] = "";
            match2winningCountry[match] = "";
            //get batsmen and Country from scorecard files.
            StreamReader sr2 = new StreamReader(dir + "match" + match + "Scorecard.html");
            string str = "";
            int innings1Done = 0;
            HashSet<string> countries = new HashSet<string>();
            while ((str = sr2.ReadLine()) != null)
            {
                if (str.Contains("<p class=\"statusText\">"))
                    match2winningCountry[match] = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "won")[0].Trim();
                if (str.Contains("(50 overs maximum)"))
                    match2firstCountry[match] = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "innings")[0].Trim();
                if (str.Contains("> v <a href"))
                {
                    string tmp = GetCleanedCommentaryFiles.convertHTMLToText(str);
                    countries.Add(Regex.Split(tmp, " v ")[0].Trim());
                    countries.Add(Regex.Split(tmp, " v ")[1].Trim());
                }
                if (str.Contains("view the player profile for") && (str.Contains("<td width=\"192\"") || str.Contains("<span><a href")))
                {
                    str = GetCleanedCommentaryFiles.convertHTMLToText(str).Replace("Did not bat", "");
                    str = Regex.Replace(str, "[^a-zA-Z-' ]", "");
                    if (innings1Done == 0)
                        match2innings1BatsmenNames[match].Add(str.Trim());
                    else
                        match2innings2BatsmenNames[match].Add(str.Trim());
                }
                if (str.Contains("Fall of wickets"))
                    innings1Done = 1;
                if (str.Contains("(target: "))
                {
                    innings1Done = 1;
                    match2secondCountry[match] = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "innings")[0].Trim();
                }
            }
            sr2.Close();
            if (match2secondCountry[match].Equals(""))
            {
                foreach (string c in countries)
                    if (!match2firstCountry[match].Equals(c))
                        match2secondCountry[match] = c;
            }
            //HashSet<string> set1 = new HashSet<string>();
            //HashSet<string> set2 = new HashSet<string>();
            //foreach (string s in match2innings1BatsmenNames[match])
            //    foreach (string t in player2FullName[s].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            //        set1.Add(t.ToLower());
            //foreach (string s in match2innings2BatsmenNames[match])
            //    foreach (string t in player2FullName[s].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            //        set2.Add(t.ToLower());
            //match2Team1Tokens[match] = set1;
            //match2Team2Tokens[match] = set2;
            //HashSet<string> set = new HashSet<string>();
            //foreach (string t in match2firstCountry[match].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            //    set.Add(t.ToLower());
            //foreach (string t in match2secondCountry[match].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            //    set.Add(t.ToLower());
            //match2CountryTokens[match] = set;
        }
    }
}
