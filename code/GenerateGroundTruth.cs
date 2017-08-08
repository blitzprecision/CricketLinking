using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections;

namespace CricketLinking
{
    class GenerateGroundTruth
    {
        static Dictionary<string, List<string>> derivedEntity2Balls = new Dictionary<string, List<string>>();
        static string dir = Global.baseDir;
        static Dictionary<string, Dictionary<string, Dictionary<string, string>>> match2country2String2StdName = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        static Dictionary<string, List<string>> match2Commentary = new Dictionary<string, List<string>>();
        public static Regex regex = new Regex(@"\d+:\d+\.\d+\.\d+");
        static void Main(string[] args)
        {
            loadDerivedEntities();
            loadPlayerNames();
            loadCommentary();
            StreamReader sr = new StreamReader(dir+"linkedLabels.tsv");
            StreamWriter sw = new StreamWriter(dir + "goldenLabels.txt");
            string str = "";
            while((str=sr.ReadLine())!=null)
            {
                string[] toks = str.Split('\t');
                //if (toks[0].Equals("11") && toks[1].Equals("503292") && toks[2].Equals("m32"))
                //    Console.WriteLine();
                string label=getBall(toks[0], toks[4], toks[10], toks[8], str);
                if(!label.Equals("-1"))
                    sw.WriteLine(toks[0]+"\t"+toks[1]+"\t"+toks[2]+"\t"+getBall(toks[0], toks[4], toks[10], toks[8], str));
            }
            sr.Close();
            sw.Close();
        }

        private static void loadDerivedEntities()
        {
            StreamReader sr = new StreamReader(dir + "derivedEntities.txt");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                string match="";
                string innings="";
                string entityName = "";
                string[] toks = str.Split('\t');
                for(int i=0;i<toks.Count();i++)
                {
                    if (toks[i].Contains("Match="))
                        match = toks[i].Split('=')[1];
                    if (toks[i].Contains("Innings="))
                        innings = toks[i].Split('=')[1];
                    if (toks[i].Contains("EntityName="))
                        entityName = toks[i].Split('=')[1];
                }
                derivedEntity2Balls[match + "_" + innings + "_" + entityName] = new List<string>(toks[toks.Count() - 1].Split('=')[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
            sr.Close();
        }

        private static void loadPlayerNames()
        {
            for (int i = 1; i <= Global.numMatches; i++)
            {
                getPlayerNames(i);
                Dictionary<string, Dictionary<string, string>> country2String2StdName = new Dictionary<string, Dictionary<string, string>>();
                StreamReader sr = new StreamReader(dir + "playerURLCountryName.txt");
                string str = "";
                while ((str = sr.ReadLine()) != null)
                {
                    string[] toks = str.Split('\t');
                    string stdName = toks[0];
                    if (!innings1BatsmenNames.Contains(stdName) && !innings2BatsmenNames.Contains(stdName))
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
        private static string getBall(string match, string condition, string subClass, string classType, string entry)
        {
            if (classType.Equals("S"))
                return getSingleBall(match, condition, subClass, classType, entry);
            if (classType.Equals("M"))
                return getMultiBall(match, condition, subClass, classType, entry);
            return "-1";
        }

        private static string getMultiBall(string match, string condition, string subClass, string classType, string entry)
        {
            string ret = "";
            List<string> finalList = new List<string>();
            List<string> balls = match2Commentary[match];
            if (subClass.Equals("BAT") || subClass.Equals("BOWL") || subClass.Equals("BATBOWL") || subClass.Equals("WICKETS") || subClass.Equals("PARTNERSHIP") || subClass.Equals("OVERS") || subClass.Equals("POWERPLAY") || subClass.Equals("FOUR") || subClass.Equals("SIX") || subClass.Equals("OTHERS") || subClass.Equals("EXTRAS") || subClass.Equals("REFERRAL-DROPPED")) 
            {
                finalList = new List<string>();
                string[] condToks = { condition };
                if (condition.Contains(" UNION "))
                    condToks = Regex.Split(condition," UNION ");
                foreach (string ct in condToks)
                {
                    if (regex.Match(ct).Success)
                        finalList.Add(ct);
                    else if (ct.Contains("AND"))
                    {
                        string[] conditions = Regex.Split(ct, " AND ");
                        List<List<string>> lists = new List<List<string>>();
                        foreach (string c in conditions)
                            lists.Add(evaluate(match, c, balls, entry));
                        finalList.AddRange(intersection(lists));
                    }
                    else
                        finalList.AddRange(evaluate(match, ct, balls, entry));
                }
            }
            foreach (string f in finalList)
                ret += f + ",";
            return ret;
        }
        private static string getSingleBall(string match, string condition, string subClass, string classType, string entry)
        {
            if (!classType.Equals("S"))
                return "";
            string ret = "";
            List<string> balls = match2Commentary[match];
            if(subClass.Equals("LASTBALL"))
            {
                string [] arguments = getArgs(condition);
                if (arguments[0].Equals("Match"))
                    ret = balls[balls.Count() - 1].Split('\t')[1]+":"+balls[balls.Count() - 1].Split('\t')[2];
                else if(!arguments[0].Equals("INNINGS1") && !arguments[0].Equals("INNINGS2"))
                {
                    string firstBatCountry = balls[0].Split('\t')[3];
                    string secondBatCountry = balls[0].Split('\t')[4];
                    if(arguments[0].Equals(firstBatCountry))
                        arguments[0]="INNINGS1";
                    if(arguments[0].Equals(secondBatCountry))
                        arguments[0]="INNINGS2";
                }
                if (arguments[0].Equals("INNINGS1"))
                {
                    foreach (string s in balls)
                        if (s.Split('\t')[1].Equals("1"))
                            ret = s.Split('\t')[1]+":"+s.Split('\t')[2];
                }
                else if (arguments[0].Equals("INNINGS2"))
                {
                    foreach (string s in balls)
                        if (s.Split('\t')[1].Equals("2"))
                            ret = s.Split('\t')[1] + ":" + s.Split('\t')[2];
                }
            }
            if(subClass.Equals("OUT"))
            {
                string[] arguments = getArgs(condition);
                string firstBatCountry = balls[0].Split('\t')[3];
                string secondBatCountry = balls[0].Split('\t')[4];
                Dictionary<string, string> dict1 = match2country2String2StdName[match][firstBatCountry];
                Dictionary<string, string> dict2 = match2country2String2StdName[match][secondBatCountry];
                string stdPlayerName = "";
                if(dict1.ContainsKey(arguments[0]))
                    stdPlayerName=dict1[arguments[0]];
                if (dict2.ContainsKey(arguments[0]))
                    stdPlayerName = dict2[arguments[0]];
                foreach (string s in balls)
                    if (s.Split('\t')[18].Equals("out") && s.Split('\t')[25].Equals(stdPlayerName))
                        ret = s.Split('\t')[1] + ":" + s.Split('\t')[2];
            }
            if (subClass.Equals("BALL"))
            {

                if (regex.Match(condition).Success)
                    ret = condition;
                else
                {
                    string[] arguments = getArgs(condition);
                    string firstBatCountry = balls[0].Split('\t')[3];
                    string secondBatCountry = balls[0].Split('\t')[4];
                    if (arguments[0].Equals(firstBatCountry))
                        arguments[0] = "INNINGS1";
                    if (arguments[0].Equals(secondBatCountry))
                        arguments[0] = "INNINGS2";
                    if (arguments[0].StartsWith("INNINGS"))
                    {
                        if (arguments[1].Equals("SCORE"))
                        {
                            string score = arguments[2];
                            string innings = "";
                            if (arguments[0].Equals("INNINGS1"))
                                innings = "1";
                            if (arguments[0].Equals("INNINGS2"))
                                innings = "2";
                            foreach (string s in balls)
                                if (s.Split('\t')[1].Equals(innings) && s.Split('\t')[10].Equals(score))
                                {
                                    ret = s.Split('\t')[1] + ":" + s.Split('\t')[2];
                                    break;
                                }
                        }
                        if (arguments[1].Equals("RRR"))
                        {
                            double rrr = double.Parse(arguments[2]);
                            foreach (string s in balls)
                                if (s.Split('\t')[1].Equals("2") && double.Parse(s.Split('\t')[13]) >= rrr)
                                {
                                    ret = s.Split('\t')[1] + ":" + s.Split('\t')[2];
                                    break;
                                }
                        }
                    }
                    else
                    {
                        int score = int.Parse(arguments[2].Trim());
                        Dictionary<string, string> dict1 = match2country2String2StdName[match][firstBatCountry];
                        Dictionary<string, string> dict2 = match2country2String2StdName[match][secondBatCountry];
                        string innings = "";
                        string stdPlayerName = "";
                        if (dict1.ContainsKey(arguments[0]))
                        {
                            stdPlayerName = dict1[arguments[0]];
                            innings = "1";
                        }
                        if (dict2.ContainsKey(arguments[0]))
                        {
                            stdPlayerName = dict2[arguments[0]];
                            innings = "2";
                        }
                        foreach (string s in balls)
                            if (s.Split('\t')[1].Equals(innings) && s.Split('\t')[6].Equals(stdPlayerName) && int.Parse(s.Split('\t')[15].Split(' ')[0].Replace("*", "")) >= score)
                            {
                                ret = s.Split('\t')[1] + ":" + s.Split('\t')[2];
                                break;
                            }
                    }
                }
            }
            if (subClass.Equals("DROPPED"))
                ret = condition;
            if (subClass.Equals("REFERRAL"))
                ret = condition;
            if (subClass.Equals("FOUR"))
            {
                if (regex.Match(condition).Success)
                    ret = condition;
                else
                {
                    List<string> finalList = new List<string>();
                    if (condition.Contains("AND"))
                    {
                        string[] conditions = Regex.Split(condition, " AND ");
                        List<List<string>> lists = new List<List<string>>();
                        foreach (string c in conditions)
                            lists.Add(evaluate(match, c, balls, entry));
                        finalList = intersection(lists);
                    }
                    else
                        finalList = evaluate(match, condition, balls, entry);
                    ret = finalList[0];
                }
            }
            if (subClass.Equals("SIX"))
            {
                if (regex.Match(condition).Success)
                    ret = condition;
                else
                {
                    List<string> finalList = new List<string>();
                    if (condition.Contains("AND"))
                    {
                        string[] conditions = Regex.Split(condition, " AND ");
                        List<List<string>> lists = new List<List<string>>();
                        foreach (string c in conditions)
                            lists.Add(evaluate(match, c, balls, entry));
                        finalList = intersection(lists);
                    }
                    else
                        finalList = evaluate(match, condition, balls, entry);
                    ret = finalList[0];
                }
            }
            if (subClass.Equals("OTHERS"))
            {
                if (regex.Match(condition).Success)
                    ret = condition;
                else
                {
                    List<string> finalList = new List<string>();
                    if (condition.Contains("AND"))
                    {
                        string[] conditions = Regex.Split(condition, " AND ");
                        List<List<string>> lists = new List<List<string>>();
                        foreach (string c in conditions)
                            lists.Add(evaluate(match, c, balls, entry));
                        finalList = intersection(lists);
                    }
                    else
                        finalList = evaluate(match, condition, balls, entry);
                    ret = finalList[0];
                }
            }
            return ret;
        }

        private static List<string> evaluate(string match, string c, List<string> balls, string entry)
        {
            List<string> finalList = new List<string>();
            string iterator = "";
            if (c.Contains(" of "))
            {
                iterator = Regex.Split(c, " of ")[0].Trim();
                c = Regex.Split(c, " of ")[1];
            }
            if (c.StartsWith("NOBALL"))
            {
                finalList.AddRange(derivedEntity2Balls[match + "_1_NOBALL"]);
                finalList.AddRange(derivedEntity2Balls[match + "_2_NOBALL"]);
            }
            if (c.StartsWith("FREEHIT"))
            {
                finalList.AddRange(derivedEntity2Balls[match + "_1_FREEHIT"]);
                finalList.AddRange(derivedEntity2Balls[match + "_2_FREEHIT"]);
            }
            if (c.StartsWith("DOTBALLS(INNINGS1)"))
                finalList.AddRange(derivedEntity2Balls[match + "_1_DOTBALLS"]);
            else if (c.StartsWith("DOTBALLS(INNINGS2)"))
                finalList.AddRange(derivedEntity2Balls[match + "_2_DOTBALLS"]);
            else if (c.StartsWith("DOTBALLS"))
            {
                finalList.AddRange(derivedEntity2Balls[match + "_1_DOTBALLS"]);
                finalList.AddRange(derivedEntity2Balls[match + "_2_DOTBALLS"]);
            }
            if (c.StartsWith("EXTRAS(INNINGS1)"))
                finalList.AddRange(derivedEntity2Balls[match + "_1_EXTRAS"]);
            else if (c.StartsWith("EXTRAS(INNINGS2)"))
                finalList.AddRange(derivedEntity2Balls[match + "_2_EXTRAS"]);
            else if (c.StartsWith("EXTRAS"))
            {
                finalList.AddRange(derivedEntity2Balls[match + "_1_EXTRAS"]);
                finalList.AddRange(derivedEntity2Balls[match + "_2_EXTRAS"]);
            }
            if (c.StartsWith("WIDES(INNINGS1)"))
                finalList.AddRange(derivedEntity2Balls[match + "_1_WIDES"]);
            else if (c.StartsWith("WIDES(INNINGS2)"))
                finalList.AddRange(derivedEntity2Balls[match + "_2_WIDES"]);
            else if (c.StartsWith("WIDES"))
            {
                finalList.AddRange(derivedEntity2Balls[match + "_1_WIDES"]);
                finalList.AddRange(derivedEntity2Balls[match + "_2_WIDES"]);
            }
            if (c.StartsWith("SINGLES(INNINGS1)"))
                finalList.AddRange(derivedEntity2Balls[match + "_1_SINGLES"]);
            else if (c.StartsWith("SINGLES(INNINGS2)"))
                finalList.AddRange(derivedEntity2Balls[match + "_2_SINGLES"]);
            else if (c.StartsWith("SINGLES"))
            {
                finalList.AddRange(derivedEntity2Balls[match + "_1_SINGLES"]);
                finalList.AddRange(derivedEntity2Balls[match + "_2_SINGLES"]);
            }
            if (c.StartsWith("TWOS(INNINGS1)"))
                finalList.AddRange(derivedEntity2Balls[match + "_1_TWOS"]);
            else if (c.StartsWith("TWOS(INNINGS2)"))
                finalList.AddRange(derivedEntity2Balls[match + "_2_TWOS"]);
            else if (c.StartsWith("TWOS"))
            {
                finalList.AddRange(derivedEntity2Balls[match + "_1_TWOS"]);
                finalList.AddRange(derivedEntity2Balls[match + "_2_TWOS"]);
            }

            if(c.StartsWith("POWERPLAY"))
            {
                string[] arguments = getArgs(c);
                int innings=1;
                if(arguments[0].Equals("INNINGS1"))
                    innings=1;
                if(arguments[0].Equals("INNINGS2"))
                    innings=2;
                string tmp = derivedEntity2Balls[match + "_"+innings+"_POWERPLAY(" + arguments[0] + ", "+arguments[1]+")"][0];
                finalList.AddRange(derivedEntity2Balls[match + "_" + innings + "_POWERPLAY(" + arguments[0] + ", " + arguments[1] + ")"]);// = getBallsRange(tmp, balls);
            }
            if(c.StartsWith("OUT("))
            {
                string[] arguments = getArgs(c);
                string firstBatCountry = balls[0].Split('\t')[3];
                string secondBatCountry = balls[0].Split('\t')[4];
                Dictionary<string, string> dict1 = match2country2String2StdName[match][firstBatCountry];
                Dictionary<string, string> dict2 = match2country2String2StdName[match][secondBatCountry];
                string stdPlayerName = "";
                if (dict1.ContainsKey(arguments[0]))
                    stdPlayerName = dict1[arguments[0]];
                if (dict2.ContainsKey(arguments[0]))
                    stdPlayerName = dict2[arguments[0]];
                foreach (string s in balls)
                    if (s.Split('\t')[18].Equals("out") && s.Split('\t')[25].Equals(stdPlayerName))
                        finalList.Add(s.Split('\t')[1] + ":" + s.Split('\t')[2]);
            }
            if(c.StartsWith("BALL("))
            {
                string[] arguments = getArgs(c);
                string firstBatCountry = balls[0].Split('\t')[3];
                string secondBatCountry = balls[0].Split('\t')[4];
                if (arguments[0].Equals(firstBatCountry))
                    arguments[0] = "INNINGS1";
                if (arguments[0].Equals(secondBatCountry))
                    arguments[0] = "INNINGS2";
                if (arguments[0].StartsWith("INNINGS"))
                {
                    if (arguments[1].Equals("SCORE"))
                    {
                        string score = arguments[2];
                        string innings = "";
                        if (arguments[0].Equals("INNINGS1"))
                            innings = "1";
                        if (arguments[0].Equals("INNINGS2"))
                            innings = "2";
                        foreach (string s in balls)
                            if (s.Split('\t')[1].Equals(innings) && s.Split('\t')[10].Equals(score))
                            {
                                finalList.Add(s.Split('\t')[1] + ":" + s.Split('\t')[2]);
                                break;
                            }
                    }
                    if (arguments[1].Equals("RRR"))
                    {
                        double rrr = double.Parse(arguments[2]);
                        foreach (string s in balls)
                            if (s.Split('\t')[1].Equals("2") && double.Parse(s.Split('\t')[13]) >= rrr)
                            {
                                finalList.Add(s.Split('\t')[1] + ":" + s.Split('\t')[2]);
                                break;
                            }
                    }
                }
                else
                {
                    int score = int.Parse(arguments[2].Trim());
                    Dictionary<string, string> dict1 = match2country2String2StdName[match][firstBatCountry];
                    Dictionary<string, string> dict2 = match2country2String2StdName[match][secondBatCountry];
                    string innings = "";
                    string stdPlayerName = "";
                    if (dict1.ContainsKey(arguments[0]))
                    {
                        stdPlayerName = dict1[arguments[0]];
                        innings = "1";
                    }
                    if (dict2.ContainsKey(arguments[0]))
                    {
                        stdPlayerName = dict2[arguments[0]];
                        innings = "2";
                    }
                    foreach (string s in balls)
                        if (s.Split('\t')[1].Equals(innings) && s.Split('\t')[6].Equals(stdPlayerName) && int.Parse(s.Split('\t')[15].Split(' ')[0].Replace("*", "")) >= score)
                        {
                            finalList.Add(s.Split('\t')[1] + ":" + s.Split('\t')[2]);
                            break;
                        }
                }
            }
            if (c.StartsWith("FOUR(") || c.StartsWith("BOWL(") || c.StartsWith("BAT(") || c.StartsWith("SIX(") || c.StartsWith("LASTOVER(") || (c.StartsWith("OVERS(")&&!c.Contains(":")) || c.StartsWith("REVERSESWEEPS(") || c.StartsWith("YORKERS(") || c.StartsWith("SWEEPS(") || c.StartsWith("WICKETS("))
            {
                string function = c.Split('(')[0];
                string[] arguments = getArgs(c);
                string firstBatCountry = balls[0].Split('\t')[3];
                string secondBatCountry = balls[0].Split('\t')[4];
                Dictionary<string, string> dict1 = match2country2String2StdName[match][firstBatCountry];
                Dictionary<string, string> dict2 = match2country2String2StdName[match][secondBatCountry];
                string stdPlayerName = "";
                if (arguments[0].Equals(firstBatCountry))
                    arguments[0] = "INNINGS1";
                if (arguments[0].Equals(secondBatCountry))
                    arguments[0] = "INNINGS2";
                if (dict1.ContainsKey(arguments[0]))
                    stdPlayerName = dict1[arguments[0]];
                if (dict2.ContainsKey(arguments[0]))
                    stdPlayerName = dict2[arguments[0]];
                if (arguments[0].StartsWith("INNINGS"))
                    stdPlayerName = arguments[0];
                if (stdPlayerName.Equals(""))
                    stdPlayerName = arguments[0];
                if (derivedEntity2Balls.ContainsKey(match + "_1_" + function + "(" + stdPlayerName + ")"))
                    finalList= derivedEntity2Balls[match + "_1_"+function+"(" + stdPlayerName + ")"];
                if (derivedEntity2Balls.ContainsKey(match + "_2_" + function + "(" + stdPlayerName + ")"))
                    finalList = derivedEntity2Balls[match + "_2_" + function + "(" + stdPlayerName + ")"];
            }
            else if(c.StartsWith("PARTNERSHIP("))
            {
                string[] arguments = getArgs(c);
                string firstBatCountry = balls[0].Split('\t')[3];
                string secondBatCountry = balls[0].Split('\t')[4];
                Dictionary<string, string> dict1 = match2country2String2StdName[match][firstBatCountry];
                Dictionary<string, string> dict2 = match2country2String2StdName[match][secondBatCountry];
                string stdPlayerName1 = "";
                if (dict1.ContainsKey(arguments[0]))
                    stdPlayerName1 = dict1[arguments[0]];
                if (dict2.ContainsKey(arguments[0]))
                    stdPlayerName1 = dict2[arguments[0]];
                string stdPlayerName2 = "";
                if (dict1.ContainsKey(arguments[1]))
                    stdPlayerName2 = dict1[arguments[1]];
                if (dict2.ContainsKey(arguments[1]))
                    stdPlayerName2 = dict2[arguments[1]];
                if (derivedEntity2Balls.ContainsKey(match + "_1_PARTNERSHIP(" + stdPlayerName1 + "," + stdPlayerName2 + ")"))
                    finalList = derivedEntity2Balls[match + "_1_PARTNERSHIP(" + stdPlayerName1 + "," + stdPlayerName2 + ")"];
                if (derivedEntity2Balls.ContainsKey(match + "_2_PARTNERSHIP(" + stdPlayerName1 + "," + stdPlayerName2 + ")"))
                    finalList = derivedEntity2Balls[match + "_2_PARTNERSHIP(" + stdPlayerName1 + "," + stdPlayerName2 + ")"];
                if (derivedEntity2Balls.ContainsKey(match + "_1_PARTNERSHIP(" + stdPlayerName2 + "," + stdPlayerName1 + ")"))
                    finalList = derivedEntity2Balls[match + "_1_PARTNERSHIP(" + stdPlayerName2 + "," + stdPlayerName1 + ")"];
                if (derivedEntity2Balls.ContainsKey(match + "_2_PARTNERSHIP(" + stdPlayerName2 + "," + stdPlayerName1 + ")"))
                    finalList = derivedEntity2Balls[match + "_2_PARTNERSHIP(" + stdPlayerName2 + "," + stdPlayerName1 + ")"];
            }
            else if(c.StartsWith("OVERS("))
            {
                int innings = int.Parse(c.Split('(')[1].Split(':')[0]);
                int from = 0;
                int to = 0;
                if (c.Contains("TO"))
                {
                    from = int.Parse(Regex.Split(c.Split('(')[1].Split(':')[1],"TO")[0]);
                    to = int.Parse(c.Split('(')[1].Split(':')[2].Split(')')[0]);
                }
                else
                {
                    from = int.Parse(c.Split('(')[1].Split(':')[1].Split(')')[0]);
                    to=from;
                }
                if (innings == 1)
                    finalList = derivedEntity2Balls[match + "_1_OVERS(INNINGS1)"];
                if (innings == 2)
                    finalList = derivedEntity2Balls[match + "_2_OVERS(INNINGS2)"];
                Dictionary<int, List<string>> tmp = new Dictionary<int, List<string>>();
                foreach (string s in finalList)
                {
                    int over = int.Parse(s.Split(':')[1].Split('.')[0]);
                    if (!tmp.ContainsKey(over))
                        tmp[over] = new List<string>();
                    tmp[over].Add(s);
                }
                finalList = new List<string>();
                for (int i = from; i <= to;i++)
                    finalList.AddRange(tmp[i-1]);
                return finalList;
            }
            if (iterator.Equals(""))
                return finalList;
            else
            {
                int values = int.Parse(Regex.Match(iterator, @"\d+").Value);
                if (!iterator.Contains(' '))//i.e. it is about a ball or set of balls
                {
                    if (!iterator.Contains("TO")) // 1 ball only.
                    {
                        List<string> tmp = new List<string>();
                        int index = values - 1;
                        if (iterator.Contains("-"))
                            index = finalList.Count() - values;
                        tmp.Add(finalList[index]);
                        return tmp;
                    }
                    else // set of balls
                    {
                        List<string> tmp = new List<string>();
                        int indexStart = int.Parse(Regex.Split(iterator, "TO")[0]);
                        int indexEnd = int.Parse(Regex.Split(iterator, "TO")[1]);
                        if(indexStart<0)
                        {
                            indexStart = finalList.Count() + indexStart+1;
                            indexEnd = finalList.Count() + indexEnd+1;
                        }
                        for (int ii = indexStart; ii <= indexEnd; ii++)
                            tmp.Add(finalList[ii-1]);
                        return tmp;
                    }
                }
                else if (iterator.Split(' ')[1].Equals("OVER") || iterator.Split(' ')[1].Equals("OVERS")) //it is about an over
                {
                    //split list into overs
                    List<int> overNumbers = new List<int>();
                    Dictionary<int, List<string>> tmp = new Dictionary<int, List<string>>();
                    foreach (string s in finalList)
                    {
                        int over = int.Parse(s.Split(':')[1].Split('.')[0]);
                        if (!tmp.ContainsKey(over))
                        {
                            tmp[over] = new List<string>();
                            overNumbers.Add(over);
                        }
                        tmp[over].Add(s);
                    }
                    if (!iterator.Contains("TO"))  //one over
                    {
                        int index = values - 1;
                        if (iterator.Contains("-"))
                            index = overNumbers.Count() - values;
                        return tmp[overNumbers[index]];
                    }
                    else // set of overs
                    {
                        List<string> tmp2 = new List<string>();
                        int indexStart = int.Parse(Regex.Split(iterator.Split(' ')[0], "TO")[0]);
                        int indexEnd = int.Parse(Regex.Split(iterator.Split(' ')[0], "TO")[1]);
                        if (indexStart < 0)
                        {
                            indexStart = overNumbers.Count() + indexStart+1;
                            indexEnd = overNumbers.Count() + indexEnd+1;
                        }
                        for (int ii = indexStart; ii <= indexEnd; ii++)
                            tmp2.AddRange(tmp[overNumbers[ii-1]]);
                        return tmp2;
                    }
                }
            }
            return finalList;
        }

        private static List<string> getBallsRange(string tmp, List<string> balls)
        {
            int innings = int.Parse(tmp.Split(':')[0]);
            string from=tmp.Split('-')[0];
            string to=tmp.Split('-')[1];
            List<string> list = new List<string>();
            int start = 0;
            foreach (string s in balls)
            {
                string ballName = s.Split('\t')[1] + ":" + s.Split('\t')[2];
                if(ballName.Equals(from))
                    start = 1;
                if (start == 1)
                    list.Add(ballName);
                if (ballName.Equals(to))
                    break;
            }
            return list;
        }

        private static List<string> intersection(List<List<string>> lists)
        {
            List<string> finalList = new List<string>();
            List<string> list1 = lists[0];
            foreach(string l in list1)
            {
                int found = 1;
                foreach(List<string> ll in lists)
                {
                    if(!ll.Contains(l))
                    {
                        found = 0;
                        break;
                    }
                }
                if (found == 1)
                    finalList.Add(l);
            }
            return finalList;
        }
        public static List<string> innings1BatsmenNames = new List<string>();
        public static List<string> innings2BatsmenNames = new List<string>();
        public static string firstCountry = "";
        public static string secondCountry = "";
        public static string winningCountry = "";

        private static void getPlayerNames(int match)
        {
            innings1BatsmenNames = new List<string>();
            innings2BatsmenNames = new List<string>();
            firstCountry = "";
            secondCountry = "";
            winningCountry = "";
            //get batsmen and Country from scorecard files.
            StreamReader sr2 = new StreamReader(dir + "match" + match + "Scorecard.html");
            string str = "";
            int innings1Done = 0;
            HashSet<string> countries = new HashSet<string>();
            while ((str = sr2.ReadLine()) != null)
            {
                if (str.Contains("<p class=\"statusText\">"))
                    winningCountry = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "won")[0].Trim();
                if (str.Contains("(50 overs maximum)"))
                    firstCountry = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "innings")[0].Trim();
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
                        innings1BatsmenNames.Add(str.Trim());
                    else
                        innings2BatsmenNames.Add(str.Trim());
                }
                if (str.Contains("Fall of wickets"))
                    innings1Done = 1;
                if (str.Contains("(target: "))
                {
                    innings1Done = 1;
                    secondCountry = Regex.Split(GetCleanedCommentaryFiles.convertHTMLToText(str), "innings")[0].Trim();
                }
            }
            sr2.Close();
            if (secondCountry.Equals(""))
            {
                foreach (string c in countries)
                    if (!firstCountry.Equals(c))
                        secondCountry = c;
            }
        }

        private static string[] getArgs(string condition)
        {
            string[] toks = condition.Split('(')[1].Split(')')[0].Split(',');
            for (int i = 0; i < toks.Length; i++)
                toks[i] = toks[i].Trim();
            return toks;
        }
        private static void loadCommentary()
        {
            for (int i = 1; i <= Global.numMatches; i++)
                match2Commentary[i + ""] = new List<string>();
            StreamReader sr = new StreamReader(dir + "matchCommentary.txt");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                int match = int.Parse(str.Split('\t')[0]);
                if (match > Global.numMatches)
                    break;
                match2Commentary[str.Split('\t')[0]].Add(str);
            }
            sr.Close();
        }
    }
}
