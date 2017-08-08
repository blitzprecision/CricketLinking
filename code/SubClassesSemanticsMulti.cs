﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CricketLinking
{
    class SubClassesSemanticsMulti
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
        public static Dictionary<int, Dictionary<string, List<string>>> match2DerivedEntityName2Balls = new Dictionary<int, Dictionary<string, List<string>>>();
        public static Dictionary<string, string> instance2POSTaggedMentionText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2POSTaggedCoreferencedMentionText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2MentionText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2CoreferencedMentionText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2MentionSentText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2CoreferencedMentionSentText = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2Label = new Dictionary<string, string>();
        public static Dictionary<string, string> instance2subClassLabel = new Dictionary<string, string>();
        public static Dictionary<string, List<double>> instance2subClassProb = new Dictionary<string, List<double>>();
        public static Dictionary<string, HashSet<string>> instance2MentionSlots = new Dictionary<string, HashSet<string>>();
        public static Dictionary<string, HashSet<string>> instance2MentionSlotsAttrIndex = new Dictionary<string, HashSet<string>>();
        public static Dictionary<string, double> idf = new Dictionary<string, double>();
        static void Main(string[] args)
        {
            loadPlayerNames();
            Console.WriteLine("Done loading player names");
            loadCommentary();
            Console.WriteLine("Done loading commentary");
            loadDerivedEntities();
            Console.WriteLine("Done loading derived entities");
            loadLabels();
            Console.WriteLine("Done loading labels");
            computeType2DerivedEntities();
            Console.WriteLine("Done computing type 2 derived entities");
            loadMentionText();
            Console.WriteLine("Done loading mention text");
            //compute best derived entity for each mention
            string[] corefs = new string[] { "NoCoreference", "Coreference" };
            string[] methods = new string[]{"SubClassSemantics"};
            string[] mentionContexts = { "mentionOnly", "mentionSentence" };//, "mentionOnly","mentionSentence"
            foreach (string mc in mentionContexts)
            {
                foreach (string coref in corefs)
                {
                    computeMentionSlots(coref, mc);
                    foreach (string method in methods)
                    {
                        Console.WriteLine("Done computing mention slots");
                        int mentionCount = 0;
                        foreach (string instance in instance2Label.Keys)
                        {
                            mentionCount++;
                            Console.WriteLine("MC:" + mc + " Coref: " + coref + " Method: " + method + " Processing mention: " + instance + " No: " + mentionCount);
                            int match = int.Parse(instance.Split('_')[0]);
                            if (instance2Label[instance].Equals("S"))
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
                            Dictionary<string, double> slotMatchScore = new Dictionary<string, double>();
                            foreach (string entityName in match2DerivedEntityName2Balls[match].Keys)
                                slotMatchScore[entityName] = computeSlotMatchScore(instance, mention, entityName);
                            StreamWriter sw = null;
                            if (mentionCount == 1)
                                sw = new StreamWriter(dir + "MB_" + method + "_"  + coref + "_" + mc + "_typeIgnorant_mention2Balls.txt", false);
                            else
                                sw = new StreamWriter(dir + "MB_" + method + "_" + coref + "_" + mc + "_typeIgnorant_mention2Balls.txt", true);

                            List<KeyValuePair<string, double>> myList = slotMatchScore.ToList();
                            myList.Sort((x, y) => y.Value.CompareTo(x.Value));
                            sw.WriteLine(instance + "\t" + string.Join(",", match2DerivedEntityName2Balls[match][myList[0].Key]));
                            sw.Close();
                            //find balls of current hard mention type only and perform match with hard type of the mention.
                            if (mentionCount == 1)
                                sw = new StreamWriter(dir + "MB_" + method + "_" + coref + "_" + mc + "_hardType_mention2Balls.txt", false);
                            else
                                sw = new StreamWriter(dir + "MB_" + method + "_" + coref + "_" + mc + "_hardType_mention2Balls.txt", true);
                            string subClass = instance2subClassLabel[instance];
                            List<string> ballsOfThisSubClass = match2type2DerivedEntities[match][subClass];
                            Dictionary<string, double>  dict2 = new Dictionary<string, double>();
                            foreach (string bInstance in ballsOfThisSubClass)
                                dict2[bInstance] = slotMatchScore[bInstance];
                            myList = dict2.ToList();
                            myList.Sort((x, y) => y.Value.CompareTo(x.Value));
                            sw.WriteLine(instance + "\t" + string.Join(",", match2DerivedEntityName2Balls[match][myList[0].Key]));
                            sw.Close();
                            dict2 = new Dictionary<string, double>();
                            //perform match with soft mention type.
                            if (mentionCount == 1)
                                sw = new StreamWriter(dir + "MB_" + method + "_" + coref + "_" + mc + "_softType_NoBallFilter_mention2Balls.txt", false);
                            else
                                sw = new StreamWriter(dir + "MB_" + method + "_" + coref + "_" + mc + "_softType_NoBallFilter_mention2Balls.txt", true);
                            subClass = instance2subClassLabel[instance];
                            foreach (string bInstance in ballsOfThisSubClass)
                                dict2[bInstance] = 0;
                            for (int i = 0; i < Global.multipleClasses.Length; i++)
                                foreach (string bInstance in ballsOfThisSubClass)
                                    dict2[bInstance] += instance2subClassProb[instance][i] * (slotMatchScore[bInstance]);
                            myList = dict2.ToList();
                            myList.Sort((x, y) => y.Value.CompareTo(x.Value));
                            sw.WriteLine(instance + "\t" + string.Join(",", match2DerivedEntityName2Balls[match][myList[0].Key]));
                            sw.Close();

                            dict2 = new Dictionary<string, double>();
                            if (mentionCount == 1)
                                sw = new StreamWriter(dir + "MB_" + method +"_" + coref + "_" + mc + "_softType_BallFilter_mention2Balls.txt", false);
                            else
                                sw = new StreamWriter(dir + "MB_" + method +"_" + coref + "_" + mc + "_softType_BallFilter_mention2Balls.txt", true);
                            subClass = instance2subClassLabel[instance];
                            foreach (string bInstance in ballsOfThisSubClass)
                                dict2[bInstance] = 0;
                            ballsOfThisSubClass = match2type2DerivedEntities[match][subClass];
                            foreach (string bInstance in ballsOfThisSubClass)
                                dict2[bInstance] = slotMatchScore[bInstance];

                            for (int i = 0; i < Global.multipleClasses.Length; i++)
                                foreach (string bInstance in ballsOfThisSubClass)
                                    if (match2type2DerivedEntities[match][i + ""].Contains(bInstance))
                                        dict2[bInstance] += instance2subClassProb[instance][i] * (slotMatchScore[bInstance]);
                            myList = dict2.ToList();
                            myList.Sort((x, y) => y.Value.CompareTo(x.Value));
                            sw.WriteLine(instance + "\t" + string.Join(",", match2DerivedEntityName2Balls[match][myList[0].Key]));
                            sw.Close();
                        }
                    }
                }
            }
        }
        public static Dictionary<int, Dictionary<string, List<string>>> match2type2DerivedEntities = new Dictionary<int, Dictionary<string, List<string>>>();
        private static void computeType2DerivedEntities()
        {
            match2type2DerivedEntities = new Dictionary<int, Dictionary<string, List<string>>>();
            for (int match = 1; match <= Global.numMatches; match++)
            {
                match2type2DerivedEntities[match] = new Dictionary<string, List<string>>();
                for (int i = 0; i < Global.multipleClasses.Length; i++)
                    match2type2DerivedEntities[match][i + ""] = new List<string>();
                foreach(string d in match2DerivedEntityName2Balls[match].Keys)
                {
                    //"BAT","BOWL","BATBOWL","WICKETS","PARTNERSHIP","EXTRAS","FOUR", "SIX", "OVERS", "OTHERS", "POWERPLAY", "REFERRAL-DROPPED"
                    if (d.Contains("BAT("))
                        match2type2DerivedEntities[match]["0"].Add(d);
                    if (d.Contains("BOWL("))
                        match2type2DerivedEntities[match]["1"].Add(d);
                    if (d.Contains("BATBOWL("))
                        match2type2DerivedEntities[match]["2"].Add(d);
                    if (d.Contains("WICKETS("))
                        match2type2DerivedEntities[match]["3"].Add(d);
                    if (d.Contains("PARTNERSHIP("))
                        match2type2DerivedEntities[match]["4"].Add(d);
                    if (d.Contains("EXTRAS"))
                        match2type2DerivedEntities[match]["5"].Add(d);
                    if (d.Contains("FOUR("))
                        match2type2DerivedEntities[match]["6"].Add(d);
                    if (d.Contains("SIX("))
                        match2type2DerivedEntities[match]["7"].Add(d);
                    if (d.Contains("OVERS("))
                        match2type2DerivedEntities[match]["8"].Add(d);
                    if (d.Contains("POWERPLAY("))
                        match2type2DerivedEntities[match]["10"].Add(d);
                    if (d.Contains("REFERRALS("))
                        match2type2DerivedEntities[match]["11"].Add(d);
                    if (d.Contains("DROPPED("))
                        match2type2DerivedEntities[match]["11"].Add(d);
                    match2type2DerivedEntities[match]["9"].Add(d);
                }
            }
        }

        private static void loadDerivedEntities()
        {
            StreamReader sr = new StreamReader(dir+"derivedEntities.txt");
            string str="";
            while((str=sr.ReadLine())!=null)
            {
                string [] toks = str.Split('\t');
                int match=int.Parse(toks[1].Split('=')[1]);
                string entityName = toks[3].Split('=')[1];
                if(entityName.Contains("POWERPLAY"))
                    entityName += toks[4].Split('=')[1];
                List<string> balls = new List<string>(toks[toks.Length-1].Split('=')[1].Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));
                Dictionary<string, List<string>> name2Balls = new Dictionary<string,List<string>>();
                if(match2DerivedEntityName2Balls.ContainsKey(match))
                    name2Balls=match2DerivedEntityName2Balls[match];
                name2Balls[entityName] = balls;
                match2DerivedEntityName2Balls[match] = name2Balls;
            }
            sr.Close();
        }

        private static void computeMentionSlots(string coref, string mc)
        {
            foreach(string instance in instance2Label.Keys)
            {
                int match = int.Parse(instance.Split('_')[0]);
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
                string subClass = instance2subClassLabel[instance];
                List<string> countries=getCountries(mention, match);
                List<string> persons = getPersons(mention, match);
                List<string> numbers = getNumbers(mention);
                instance2MentionSlots[instance] = new HashSet<string>();
                instance2MentionSlotsAttrIndex[instance] = new HashSet<string>();
                //{ "BAT","BOWL","BATBOWL","WICKETS","PARTNERSHIP","EXTRAS","FOUR", "SIX", "OVERS", "OTHERS", "POWERPLAY", "REFERRAL-DROPPED"};
                for (int i = 0; i < 9;i++)
                    if (subClass.Equals(i + ""))
                        instance2MentionSlots[instance].Add(Global.multipleClasses[i]);
                if (subClass.Equals("10"))
                    instance2MentionSlots[instance].Add("POWERPLAY");
                if (subClass.Equals("11"))
                {
                    instance2MentionSlots[instance].Add("REFERRAL");
                    instance2MentionSlots[instance].Add("DROPPED");
                }


                if (subClass.Equals("0") || subClass.Equals("1") || subClass.Equals("2") || subClass.Equals("3") || subClass.Equals("4")
                    || subClass.Equals("5") || subClass.Equals("6") || subClass.Equals("7"))
                {
                    foreach (string c in persons)
                        instance2MentionSlots[instance].Add(c);
                }
                if(subClass.Equals("8"))
                {
                    foreach (string c in countries)
                        instance2MentionSlots[instance].Add(c);
                }
                if(subClass.Equals("10"))
                {
                    if(mention.ToLower().Contains("batting"))
                        instance2MentionSlots[instance].Add("Batting");
                    if(mention.ToLower().Contains("bowling"))
                        instance2MentionSlots[instance].Add("Bowling");
                    if(mention.ToLower().Contains("mandatory"))
                        instance2MentionSlots[instance].Add("Mandatory");
                    if(mention.ToLower().Contains("first"))
                        instance2MentionSlots[instance].Add("1");
                    if(mention.ToLower().Contains("second"))
                        instance2MentionSlots[instance].Add("2");
                    if(!mention.ToLower().Contains("first")&&!mention.ToLower().Contains("second")&&!mention.ToLower().Contains("bowling")&&!mention.ToLower().Contains("mandatory"))
                        instance2MentionSlots[instance].Add("3");
                    foreach (string c in numbers)
                        instance2MentionSlots[instance].Add(c);
                }
            }
        }
        private static List<string> getPersons(string mention, int match)
        {
            List<string> cList = new List<string>();
            foreach (string m in match2innings1BatsmenNames[match])
                if (mention.Contains(m.Replace(' ', '_')))
                    cList.Add(m);
            foreach (string m in match2innings2BatsmenNames[match])
                if (mention.Contains(m.Replace(' ', '_')))
                    cList.Add(m);
            return cList;
        }

        private static List<string> getNumbers(string mention)
        {
            Regex tmpRegex = new Regex(@"\d+");
            List<string> cList = new List<string>();
            foreach (string s in mention.Split(' '))
                if (tmpRegex.IsMatch(s))
                    cList.Add(s);
            return cList;
        }
        private static List<string> getCountries(string mention, int match)
        {
            List<string> cList = new List<string> ();
            if (mention.Contains(match2firstCountry[match]))
                cList.Add(match2firstCountry[match]);
            if(mention.Contains(match2secondCountry[match]))
                cList.Add(match2secondCountry[match]);
            return cList;
        }
        private static double computeSlotMatchScore(string instance, string mention, string entityName)
        {
            HashSet<string> curMentionSlots = instance2MentionSlots[instance];
            double denominator = curMentionSlots.Count();
            int match = 0;
            foreach (string s in curMentionSlots)
                if (entityName.Contains(s))
                    match++;
            if (match / denominator < 1)
                return match / denominator;
            else
                return 1;
        }

        private static void loadLabels()
        {
            Dictionary<string, string> subClassLabel2Index = new Dictionary<string, string>();
            for (int i = 0; i < Global.multipleClasses.Length; i++)
                subClassLabel2Index[Global.multipleClasses[i]] = i + "";
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
                string subClassLabel = toks[10];
                if (classLabel.Trim().Equals("M"))
                {
                    instance2Label[matchNumber + "_" + articleNumber + "_" + mentionNum] = classLabel;
                    instance2subClassLabel[matchNumber + "_" + articleNumber + "_" + mentionNum] = subClassLabel2Index[subClassLabel];
                }
            }
            sr.Close();
            sr = new StreamReader(dir + "multipleMentionClassifier.inst.txt");
            sr.ReadLine();
            while ((str = sr.ReadLine()) != null)
            {
                string[] toks = str.Split('\t');
                List<double> dict = new List<double>();
                double remaining = (1 - double.Parse(toks[4]) - double.Parse(toks[6]) - double.Parse(toks[8])) / (Global.multipleClasses.Length - 3);
                for (int i = 0; i < Global.multipleClasses.Length; i++)
                    dict.Add(remaining);
                dict[int.Parse(toks[3])] = double.Parse(toks[4]);
                dict[int.Parse(toks[5])] = double.Parse(toks[6]);
                dict[int.Parse(toks[7])] = double.Parse(toks[8]);
                instance2subClassProb[toks[0]] = dict;
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
                if (match > Global.numMatches)
                    break;
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
                if (match > Global.numMatches)
                    break;
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
