using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace CricketLinking
{
    class SingleBallClassifier
    {
        public static string dir = Global.baseDir;
        static Dictionary<string, Dictionary<string, string>> match2String2StdName = new Dictionary<string, Dictionary<string, string>>();

        public static Dictionary<int, HashSet<string>> match2Team1Tokens = new Dictionary<int, HashSet<string>>();
        public static Dictionary<int, HashSet<string>> match2Team2Tokens = new Dictionary<int, HashSet<string>>();
        public static Dictionary<int, HashSet<string>> match2CountryTokens = new Dictionary<int, HashSet<string>>();
        public static Dictionary<string, string> instance2Label = new Dictionary<string, string>();
        public static Dictionary<string, List<double>> instance2Features = new Dictionary<string, List<double>>();
        public static List<string> featureNames = new List<string>();
        public static Dictionary<string, string> instance2MentionText = new Dictionary<string, string>();
        public static Dictionary<string, HashSet<string>> country2Players = new Dictionary<string, HashSet<string>>();
        public static Dictionary<string, string> player2Country = new Dictionary<string, string>();
        public static Dictionary<string, string> player2FullName = new Dictionary<string, string>();

        public static Dictionary<int, List<string>> innings1BatsmenNames = new Dictionary<int, List<string>>();
        public static Dictionary<int, List<string>> innings2BatsmenNames = new Dictionary<int, List<string>>();
        public static Dictionary<int,string> firstCountry = new Dictionary<int,string>();
        public static Dictionary<int,string> secondCountry =new Dictionary<int,string>();
        public static Dictionary<int, string> winningCountry = new Dictionary<int, string>();

        public static Dictionary<int, List<string>> matchNum2Commentary = new Dictionary<int, List<string>>();
        public static Dictionary<string, int> report2NumMentions = new Dictionary<string, int>();
        public static Dictionary<string, double> mention2Sentiment = new Dictionary<string, double>();
        private static void loadPlayerNames()
        {
            getPlayerNames();
            for (int i = 1; i <= Global.numMatches; i++)
            {
                StreamReader sr = new StreamReader(dir + "playerURLCountryName.txt");
                string str = "";
                Dictionary<string, string> string2StdPlayerName = new Dictionary<string, string>();
                while ((str = sr.ReadLine()) != null)
                {
                    string[] origToks = str.Split('\t');
                    string[] toks = str.ToLower().Split('\t');
                    string stdName = origToks[0];
                    if (!innings1BatsmenNames[i].Contains(stdName) && !innings2BatsmenNames[i].Contains(stdName))
                        continue;
                    string country = toks[2];
                    string fullName = toks[3];
                    string fullName2 = "";
                    if (fullName.Contains('('))
                    {
                        fullName2 = fullName.Split('(')[1].Split(')')[0].Trim();
                        fullName = fullName.Split('(')[0].Trim();
                    }
                    string[] fullNameToks = fullName.Split(' ');

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
                }
                sr.Close();
                match2String2StdName[i + ""] = string2StdPlayerName;
            }
        }

        public static void loadMentionSentiment()
        {
            StreamReader sr = new StreamReader(dir + "mentionSentiments.txt");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                string[] toks = str.Split('\t');
                string key = toks[0] + "_" + toks[1] + "_" + toks[2].Replace("<m","").Replace(">","");
                toks=toks[4].Trim().Split(' ');
                double value = double.Parse(toks[1]) * (-10) + double.Parse(toks[2]) * (-5) + double.Parse(toks[3]) * (0) + double.Parse(toks[4]) * (5) + double.Parse(toks[5]) * (10);
                mention2Sentiment[key] = value;
            }
            sr.Close();
        }
        public static void loadCommentary()
        {
            StreamReader sr = new StreamReader(dir + "matchCommentary.txt");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                int matchNum = int.Parse(str.Split('\t')[0]);
                if (!matchNum2Commentary.ContainsKey(matchNum))
                    matchNum2Commentary[matchNum] = new List<string>();
                matchNum2Commentary[matchNum].Add(str);
            }
            sr.Close();
        }
        static void Main(string[] args)
        {
            StreamReader sr = new StreamReader(dir + "linkedLabels.tsv");
            string str = "";
            StreamWriter sw = new StreamWriter(dir + "singleMentionClassifier.tsv");
            while((str=sr.ReadLine())!=null)
            {
                string[] toks = str.Split('\t');
                int matchNumber = int.Parse(toks[0]);
                string articleNumber = toks[1];
                string mentionNum = toks[2].Replace("m","");
                string mention = toks[3];
                string classLabel = toks[8];
                if (!classLabel.Trim().Equals(""))
                {
                    if (report2NumMentions.ContainsKey(matchNumber + "_" + articleNumber))
                        report2NumMentions[matchNumber + "_" + articleNumber]++;
                    else
                        report2NumMentions[matchNumber + "_" + articleNumber] = 1;
                }
                if (!classLabel.Equals("S"))
                    continue;
                instance2Label[matchNumber + "_" + articleNumber + "_" + mentionNum] = toks[10];
            }
            sr.Close();
            loadCommentary();
            loadMentionSentiment();
            //load country and player names
            StreamReader sr2 = new StreamReader(dir + "playerURLCountryName.txt");
            while ((str = sr2.ReadLine()) != null)
            {
                string[] toks = str.Split('\t');
                string player = toks[0];
                string country = toks[2];
                if (!country2Players.ContainsKey(country))
                    country2Players[country] = new HashSet<string>();
                country2Players[country].Add(player);
                player2Country[player] = country;
                player2FullName[player] = toks[3].Replace('(', ' ').Replace(')', ' ');
            }
            sr2.Close();
            loadPlayerNames();

            //load the POS tagged mentions
            sr = new StreamReader(dir + "matchArticlesPOSTagged.txt");
            while((str=sr.ReadLine())!=null)
            {
                string match_article = str.Split('\t')[0] + "_" + str.Split('\t')[1];
                string text = str.Split('\t')[6];
                string mentionNum = "";
                string mention = "";
                //int mentionStart = 0;
                if (int.Parse(str.Split('\t')[0]) > 30)
                    continue;
                text = text.Replace("<m", "\n<m").Replace("</m","\n</m");
                string[] textToks = text.Split('\n');
                foreach(string t in textToks)
                {
                    if (!t.StartsWith("<m"))
                        continue;
                    mentionNum = t.Split('>')[0].Replace("<m", "");
                    mention = t.Replace("<m" + mentionNum + ">", "");
                    instance2MentionText[match_article + "_" + mentionNum] = mention.Trim();
                }
            }
            sr.Close();
            featureNames.Add("Number of Player Names (Team1)");
            featureNames.Add("Number of Player Names (Team2)");
            //featureNames.Add("Number of Player Names (Both)");
            featureNames.Add("Number of Country Names");

            featureNames.Add("Number of BALL Words");
            featureNames.Add("Number of DROPPED Words");
            featureNames.Add("Number of FOUR Words");
            featureNames.Add("Number of LASTBALL Words");
            featureNames.Add("Number of SIX Words");
            featureNames.Add("Number of OUT Words");
            featureNames.Add("Number of REFERRAL Words");
            featureNames.Add("Number of OTHER Words");

            featureNames.Add("(Overall) Max Similarity with any Ball");
            featureNames.Add("(Overall) Max Similarity with any Ball/Second best similarity");
            featureNames.Add("(Overall) Max Similarity with any Ball - Second best similarity");
            featureNames.Add("(Structured Match Only) Max Similarity with any Ball");
            featureNames.Add("(Structured Match Only) Max Similarity with any Ball/Second best similarity");
            featureNames.Add("(Structured Match Only) Max Similarity with any Ball - Second best similarity");
            featureNames.Add("(Unstructured Match Only) Max Similarity with any Ball");
            featureNames.Add("(Unstructured Match Only) Max Similarity with any Ball/Second best similarity");
            featureNames.Add("(Unstructured Match Only) Max Similarity with any Ball - Second best similarity");
            featureNames.Add("Similarity to Players in out Balls");
            featureNames.Add("Number of Shot words");
            featureNames.Add("Number of Bowler action words");
            featureNames.Add("Number of partnership words");
            featureNames.Add("Number of Single Mention Words");
            featureNames.Add("Number of Multiple Mention Words");
            featureNames.Add("Position of Mention in Report");
            featureNames.Add("Contains Score (in format XXX for YY)");
            featureNames.Add("Number of Extras Words");
            featureNames.Add("Mention Length in Characters");
            featureNames.Add("Mention Length in Words");
            featureNames.Add("Number of Numeric Values");
            featureNames.Add("Number of Person Entities");
            featureNames.Add("Number of Date Entities");
            featureNames.Add("Number of Location Entities");
            featureNames.Add("Number of Organization Entities");
            featureNames.Add("Number of all Entities");
            featureNames.Add("Number of Powerplay words");
            featureNames.Add("Sentiment score of the mention");
            featureNames.Add("Number of Plural Words");
            featureNames.Add("Similarity to Derived Entities");

            //compute the feature values
            foreach (string instance in instance2Label.Keys)
            {
                string mention = instance2MentionText[instance].ToLower();
                string[] mentionToks = mention.Split(' ');
                int match = int.Parse(instance.Split('_')[0]);
                instance2Features[instance] = new List<double>();
                //Features: 
                //1. number of player names Team1
                HashSet<string> stdPlayerSet1 = new HashSet<string>();
                foreach (string mt in mentionToks)
                    if (match2Team1Tokens[match].Contains(mt.Split('_')[0].ToLower()) && match2String2StdName[match + ""].ContainsKey(mt.Split('_')[0].ToLower()))
                        stdPlayerSet1.Add(match2String2StdName[match + ""][mt.Split('_')[0].ToLower()]);
                instance2Features[instance].Add(stdPlayerSet1.Count());
                //1. number of player names Team1
                HashSet<string> stdPlayerSet2 = new HashSet<string>();
                foreach (string mt in mentionToks)
                    if (match2Team2Tokens[match].Contains(mt.Split('_')[0].ToLower()) && match2String2StdName[match + ""].ContainsKey(mt.Split('_')[0].ToLower()))
                        stdPlayerSet2.Add(match2String2StdName[match + ""][mt.Split('_')[0].ToLower()]);
                instance2Features[instance].Add(stdPlayerSet2.Count());
                //instance2Features[instance].Add(numPlayers2);
                //2. number of country names 
                int numCountries = 0;
                foreach (string mt in mentionToks)
                {
                    if (match2CountryTokens[match].Contains(mt.Split('_')[0]))
                        numCountries++;
                }
                instance2Features[instance].Add(numCountries);
                //3. event words
                int BALLwords = 0;
                int DROPPEDwords = 0;
                int FOURwords = 0;
                int LASTBALLwords = 0;
                int SIXwords = 0;
                int OUTwords = 0;
                int REFERRALwords = 0;
                int OTHERwords = 0;
                foreach (string mt in mentionToks)
                {
                    if (Global.BALLwords.Contains(mt.Split('_')[0]))
                        BALLwords++;
                    if (Global.DROPPEDwords.Contains(mt.Split('_')[0]))
                        DROPPEDwords++;
                    if (Global.FOURwords.Contains(mt.Split('_')[0]))
                        FOURwords++;
                    if (Global.LASTBALLwords.Contains(mt.Split('_')[0]))
                        LASTBALLwords++;
                    if (Global.SIXwords.Contains(mt.Split('_')[0]))
                        SIXwords++;
                    if (Global.OUTwords.Contains(mt.Split('_')[0]))
                        OUTwords++;
                    if (Global.REFERRALwords.Contains(mt.Split('_')[0]))
                        REFERRALwords++;
                    if (Global.OTHERwords.Contains(mt.Split('_')[0]))
                        OTHERwords++;
                }

                instance2Features[instance].Add(BALLwords);
                instance2Features[instance].Add(DROPPEDwords);
                instance2Features[instance].Add(FOURwords);
                instance2Features[instance].Add(LASTBALLwords);
                instance2Features[instance].Add(SIXwords);
                instance2Features[instance].Add(OUTwords);
                instance2Features[instance].Add(REFERRALwords);
                instance2Features[instance].Add(OTHERwords);

                //4. maximum similarity score (TFIDF) with any ball
                string selection = "";
                foreach (string mt in mentionToks)
                    selection += mt.Split('_')[0] + " ";
                selection = selection.Trim();
                Dictionary<string, double> dict1 = new Dictionary<string, double>();
                Dictionary<string, double> dict2 = new Dictionary<string, double>();
                Dictionary<string, double> dict3 = new Dictionary<string, double>();
                foreach (string s in matchNum2Commentary[match])
                {
                    string tmpScore=computeScore(s, selection);
                    dict1[s] = double.Parse(tmpScore.Split(' ')[0]) + double.Parse(tmpScore.Split(' ')[1]);
                    dict2[s] = double.Parse(tmpScore.Split(' ')[0]);
                    dict3[s] = double.Parse(tmpScore.Split(' ')[1]);
                }
                List<KeyValuePair<string, double>> myList1 = dict1.ToList();
                myList1.Sort((x, y) => y.Value.CompareTo(x.Value));
                List<KeyValuePair<string, double>> myList2 = dict2.ToList();
                myList2.Sort((x, y) => y.Value.CompareTo(x.Value));
                List<KeyValuePair<string, double>> myList3 = dict3.ToList();
                myList3.Sort((x, y) => y.Value.CompareTo(x.Value));

                instance2Features[instance].Add(myList1[0].Value);
                instance2Features[instance].Add(myList1[1].Value / (myList1[0].Value + 1));
                instance2Features[instance].Add(myList1[1].Value - myList1[0].Value);

                instance2Features[instance].Add(myList2[0].Value);
                instance2Features[instance].Add(myList2[1].Value / (myList2[0].Value + 1));
                instance2Features[instance].Add(myList2[1].Value - myList2[0].Value);

                instance2Features[instance].Add(myList3[0].Value);
                instance2Features[instance].Add(myList3[1].Value / (myList3[0].Value + 1));
                instance2Features[instance].Add(myList3[1].Value - myList3[0].Value);

                //similarity to players in the out ball
                int sim = 0;
                foreach (string s in matchNum2Commentary[match])
                {
                    string[] toks = s.Split('\t');
                    if (!toks[18].Equals("out"))
                        continue;
                    int hasOutGuy=0;
                    int hasBowler=0;
                    string outGuy=player2FullName[toks[25]].ToLower();
                    foreach(string o in outGuy.Split(' '))
                        if(selection.ToLower().Contains(o))
                            hasOutGuy=1;
                    string bowler = player2FullName[toks[9]].ToLower();
                    foreach(string b in bowler.Split(' '))
                        if(selection.ToLower().Contains(b))
                            hasBowler=1;
                    if(hasBowler==1 && hasOutGuy==1)
                        sim=1;
                }
                instance2Features[instance].Add(sim);
                //5. number of batsman action words
                int shotWords = 0;
                foreach (string mt in mentionToks)
                {
                    if (Global.shotKeywords.Contains(mt.Split('_')[0]) || Global.shots.Contains(mt.Split('_')[0]))
                        shotWords++;
                }
                instance2Features[instance].Add(shotWords);
                //6. number of bowler action words
                int ballWords = 0;
                foreach (string mt in mentionToks)
                {
                    if (Global.balls.Contains(mt.Split('_')[0]))
                        ballWords++;
                }
                instance2Features[instance].Add(ballWords);
                //7. number of partnership action words
                int partnershipWords = 0;
                foreach (string mt in mentionToks)
                {
                    if (Global.partnershipWords.Contains(mt.Split('_')[0]))
                        partnershipWords++;
                }
                instance2Features[instance].Add(partnershipWords);
                //8. number of words found in a list of words found frequently in single/multiple mentions
                int singleMentionWords = 0;
                int multipleMentionWords = 0;
                foreach (string mt in mentionToks)
                {
                    if (Global.discriminativeSingleMentionWords.Contains(mt.Split('_')[0]))
                        singleMentionWords++;
                    if (Global.discriminativeMultipleMentionWords.Contains(mt.Split('_')[0]))
                        multipleMentionWords++;
                }
                instance2Features[instance].Add(singleMentionWords);
                instance2Features[instance].Add(multipleMentionWords);
                //9. position of mention in the report (which tile out of 10 of the report is the mention located in)
                instance2Features[instance].Add((int)((double.Parse(instance.Split('_')[2]) * 10) / report2NumMentions[instance.Split('_')[0] + "_" + instance.Split('_')[1]]));
                //10. contains score
                int containsScore = 0;
                string tmp = "";
                foreach (string mt in mentionToks)
                    tmp += mt.Split('_')[0] + " ";
                tmp = tmp.Trim();
                if (Regex.Matches(tmp, "[0-9]+ for [0-9]+").Count != 0)
                    containsScore=1;
                instance2Features[instance].Add(containsScore);
                //11. number of words denoting extra balls
                int extrasWords = 0;
                foreach (string mt in mentionToks)
                {
                    if (Global.extrasWords.Contains(mt.Split('_')[0]))
                        extrasWords++;
                }
                instance2Features[instance].Add(extrasWords);
                //Mention Length in words and characters
                instance2Features[instance].Add(selection.Length);
                instance2Features[instance].Add(selection.Split(' ').Count());
                //12. Dependency features
                
                //13. number of numeric values.
                int numNumeric = 0;
                foreach (string mt in mentionToks)
                {
                    if (Regex.Matches(mt.Split('_')[0],"[0-9]+").Count!=0)
                        numNumeric++;
                }
                instance2Features[instance].Add(numNumeric);
                //14. NER entities.
                int persons = 0;
                int dates=0;
                int locations=0;
                int organizations=0;
                int allEntities=0;
                foreach (string mt in mentionToks)
                {
                    if (mt.Split('_').Count()>2)
                    {
                        if (!mt.Split('_')[2].Equals("o"))
                            allEntities++;
                        if(mt.Split('_')[2].Equals("person"))
                           persons++;
                        if(mt.Split('_')[2].Equals("date"))
                           dates++;
                        if(mt.Split('_')[2].Equals("location"))
                           locations++;
                        if(mt.Split('_')[2].Equals("organization"))
                           organizations++;
                    }
                }
                instance2Features[instance].Add(persons);
                instance2Features[instance].Add(dates);
                instance2Features[instance].Add(locations);
                instance2Features[instance].Add(organizations);
                instance2Features[instance].Add(allEntities);
                //16. number of powerplay words.
                int powerPlayWords = 0;
                foreach (string mt in mentionToks)
                {
                    if (Global.powerPlayWords.Contains(mt.Split('_')[0]))
                        powerPlayWords++;
                }
                instance2Features[instance].Add(powerPlayWords);
                //17. maximum similarity score (TFIDF) with any ball: WordNet and Cricket Vocabulary words only.
                //19. sentiment of the mention.
                instance2Features[instance].Add(mention2Sentiment[instance]);
                //20. Presence of plural in the mention.
                int numPlurals = 0;
                foreach (string mt in mentionToks)
                {
                    if (mt.Split('_')[1].Equals("nns") || mt.Split('_')[1].Equals("nnps"))
                        numPlurals++;
                }
                instance2Features[instance].Add(numPlurals);
                //21. Similarity to derived entity names.
                int derivedEntityWords = 0;
                foreach (string mt in mentionToks)
                {
                    if (Global.derivedEntities.Contains(mt.Split('_')[0]))
                        derivedEntityWords++;
                }
                instance2Features[instance].Add(derivedEntityWords);
            }
            sw.Write("InstanceID\tLabel");
            foreach (string s in featureNames)
                sw.Write("\t"+s);
            sw.WriteLine();
            //write out the feature values
            foreach(string instance in instance2Features.Keys)
            {
                List<double> features = instance2Features[instance];
                string classLabel="";
                string label = instance2Label[instance];
                for (int i = 0; i < Global.singleClasses.Count();i++)
                    if (label.Equals(Global.singleClasses[i]))
                        classLabel = i+"";
                sw.Write("_"+instance+"\t" + classLabel);
                foreach (double d in features)
                    sw.Write("\t" + d);
                sw.Write("\n");
            }
            sw.Close();

            //perform 10 fold cross validation
            string dataFile = "singleMentionClassifier.tsv";
            ProcessStartInfo start = new ProcessStartInfo();
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            start.FileName = @"C:\Users\gmanish\Downloads\TLC_2.4.197.0.Single\TL.exe";
            start.Arguments = dir + dataFile + @" /k=3 /strat=+ /inst=TextInstances{header=+} /cacheinst=- /cl=OVA{p=FastRankBinaryClassification} /summary=+ /pr=" + dir + @"\singleMentionClassifier.pr.txt /o=" + dir + @"\singleMentionClassifier.inst.txt /t=+ /trsess=1";
            StreamWriter outFile = new StreamWriter(dir + "singleMentionClassifier.out.txt");
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    outFile.WriteLine(result);
                    Console.WriteLine(result);
                }
            }
            outFile.Close();

            //generate error cases to be analyzed later.
            sr = new StreamReader(dir + "singleMentionClassifier.inst.txt");
            StreamWriter sw1 = new StreamWriter(dir + "singleMentionClassifier.wrong.txt");
            StreamWriter sw2 = new StreamWriter(dir + "singleMentionClassifier.right.txt");
            sr.ReadLine();
            sw1.Write("InstanceID\tTrue\tAssigned\tProbability\tMentionText");
            sw2.Write("InstanceID\tTrue\tAssigned\tProbability\tMentionText");
            foreach (string s in featureNames)
            {
                sw1.Write("\t" + s);
                sw2.Write("\t" + s);
            }
            sw1.WriteLine();
            sw2.WriteLine();
            while((str=sr.ReadLine())!=null)
            {
                string[] toks = str.Split('\t');
                string instance = toks[0];
                if (!toks[1].Equals(toks[2]))
                {
                    sw1.Write(instance + "\t" + toks[1] + "\t" + toks[2] + "\t" + toks[4] + "\t" + instance2MentionText[toks[0]]);
                    List<double> features = instance2Features[instance];
                    foreach (double d in features)
                        sw1.Write("\t" + d);
                    sw1.Write("\n");
                }
                else
                {
                    sw2.Write(instance + "\t" + toks[1] + "\t" + toks[2] + "\t" + toks[4] + "\t" + instance2MentionText[toks[0]]);
                    List<double> features = instance2Features[instance];
                    foreach (double d in features)
                        sw2.Write("\t" + d);
                    sw2.Write("\n");
                }
            }
            sw1.Close();
            sw2.Close();
            sr.Close();
        }
        private static string computeScore(string s, string selection)
        {
            double score2 = 0;
            double score1 = 0;
            string[] toks = s.Split('\t');
            HashSet<string> selectionSet = new HashSet<string>(selection.Split(' '));
            string[] toks2 = new string[toks.Length - 4];
            for (int i = 3; i < toks.Length - 1; i++)
                toks2[i - 3] = toks[i];
            HashSet<string> metaDataSet = new HashSet<string>(toks2);
            HashSet<string> ballCommentarySet = new HashSet<string>(toks[toks.Length - 1].Split(' '));
            foreach (string m in metaDataSet)
                if (selectionSet.Contains(m))
                    score1 += 100;
            foreach (string m in ballCommentarySet)
                if (selectionSet.Contains(m))
                    score2 += 1;
            return score1+" "+score2;
        }
        private static void getPlayerNames()
        {
            for (int match = 1; match <= Global.numMatches; match++)
            {
                innings1BatsmenNames[match] = new List<string>();
                innings2BatsmenNames[match] = new List<string>();
                firstCountry[match] = "";
                secondCountry[match] = "";
                winningCountry[match] = "";
                //get batsmen and Country from scorecard files.
                StreamReader sr2 = new StreamReader(dir + "match" + match + "Scorecard.html");
                string str = "";
                int innings1Done = 0;
                HashSet<string> countries = new HashSet<string>();
                while ((str = sr2.ReadLine()) != null)
                {
                    if (str.Contains("<p class=\"statusText\">"))
                        winningCountry[match] = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "won")[0].Trim();
                    if (str.Contains("(50 overs maximum)"))
                        firstCountry[match] = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "innings")[0].Trim();
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
                            innings1BatsmenNames[match].Add(str.Trim());
                        else
                            innings2BatsmenNames[match].Add(str.Trim());
                    }
                    if (str.Contains("Fall of wickets"))
                        innings1Done = 1;
                    if (str.Contains("(target: "))
                    {
                        innings1Done = 1;
                        secondCountry[match] = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "innings")[0].Trim();
                    }
                }
                sr2.Close();
                if (secondCountry[match].Equals(""))
                {
                    foreach (string c in countries)
                        if (!firstCountry[match].Equals(c))
                            secondCountry[match] = c;
                }
                HashSet<string> set1 = new HashSet<string>();
                HashSet<string> set2 = new HashSet<string>();
                foreach (string s in innings1BatsmenNames[match])
                    foreach (string t in player2FullName[s].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                        set1.Add(t.ToLower());
                foreach (string s in innings2BatsmenNames[match])
                    foreach (string t in player2FullName[s].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                        set2.Add(t.ToLower());
                match2Team1Tokens[match] = set1;
                match2Team2Tokens[match] = set2;
                HashSet<string> set = new HashSet<string>();
                foreach (string t in firstCountry[match].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    set.Add(t.ToLower());
                foreach (string t in secondCountry[match].Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    set.Add(t.ToLower());
                match2CountryTokens[match] = set;
            }
        }
    }
}
