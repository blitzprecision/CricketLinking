using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CricketLinking
{
    /// <summary>
    /// Generates structured files for commentaries: Generate matchCommentary.txt -- match#, innings#, ball#, commentary.
    /// </summary>
    class GetCleanedCommentaryFiles
    {
        public static string dir = Global.baseDir;
        public static Dictionary<string, string> batsmanStatsMap = new Dictionary<string, string>();
        public static Dictionary<string, string> bowlerStatsMap = new Dictionary<string, string>();
        public static string convertHTMLToText(string text)
        {
            text = text.Replace("\\\\", "");
            text = text.Replace("\\\"", "");
            text = text.Replace("\\\'", "");
            text = text.Replace("$", "#@#@#");
            text = text.Replace("</script>", "$");
            //handle the " " and ' ' within the script tags
            text = Regex.Replace(text, "<script([^\'\"$]*((\'[^\']*\')|(\"[^\"]*\")))*[^\'\"$]*\\$", " ");
            text = text.Replace("$", "</script>");
            string str = "";
            List<string> buffer1 = new List<string>();
            string[] toks = text.Split('\n');
            foreach (string input in toks)
            {
                str = input.Replace("@", "");
                str = str.Replace("-->", "@");
                str = Regex.Replace(str, "<!--[^@]*@", "");
                str = str.Replace("@", "-->");
                StringBuilder strb = new StringBuilder(str);
                strb.Replace("<!--", "\n<!--");
                strb.Replace("<![CDATA", "\n<![CDATA");
                strb.Replace("<script", "\n<script");
                strb.Replace("<style", "\n<style");
                strb.Replace("<br>", "\n");
                strb.Replace("<hr>", "\n");
                strb.Replace("</style>", "</style>\n");
                strb.Replace("script>", "script>\n");
                strb.Replace("]]>", "]]>\n");
                strb.Replace("-->", "-->\n");
                strb.Replace("<p", "\n<p");
                strb.Replace("</p>", "</p>\n");
                string[] tokens = (strb + "").Split('\n');
                for (int i = 0; i < tokens.Length; i++)
                    buffer1.Add(tokens[i]);
            }

            List<string> buffer2 = new List<string>();
            str = "";
            int scriptStart = 0;
            int styleStart = 0;
            int commentStart = 0;
            int cdataStart = 0;
            foreach (string s2 in buffer1)
            {
                str = s2;
                int printLittle = 0;
                int begin = 0;
                int end = str.Length;
                if (str.Contains("<!--") && scriptStart == 0)
                {
                    commentStart++;
                }
                if (str.Contains("<![cdata") && scriptStart == 0)
                {
                    cdataStart++;
                }
                if (commentStart == 0 && cdataStart == 0)
                {
                    if (str.Contains("<script"))
                    {
                        scriptStart++;
                    }
                    if (scriptStart == 0 && str.Contains("<style"))
                    {
                        styleStart++;
                    }
                    if (scriptStart == 0 && styleStart == 0 && cdataStart == 0 && commentStart == 0)
                    {
                        if (!str.Trim().Equals(""))
                            buffer2.Add(str);
                    }
                    if (printLittle == 1)
                    {
                        if (end > begin)
                            buffer2.Add(str.Substring(begin, end - begin));
                        printLittle = 0;
                    }
                }

                end = str.Length;

                if (commentStart == 0 && cdataStart == 0)
                {
                    if (str.Contains("</script>"))
                    {
                        if (str.IndexOf("</script>") > str.IndexOf("-->") && str.IndexOf("</script>") > str.IndexOf("]]>"))
                        {
                            scriptStart--;
                        }
                    }
                    if (str.Contains("</\"+\"script>"))
                        if (str.IndexOf("</\"+\"script>") > str.IndexOf("-->") && str.IndexOf("</\"+\"script>") > str.IndexOf("]]>"))
                            scriptStart--;

                    if (scriptStart == 0 && str.Contains("</style>"))
                    {
                        if (str.IndexOf("</style>") > str.IndexOf("-->") && str.IndexOf("</style>") > str.IndexOf("]]>"))
                        {
                            styleStart--;
                        }
                    }
                    if (printLittle == 1)
                    {
                        if (end > begin)
                            buffer2.Add(str.Substring(begin, end - begin));
                        printLittle = 0;
                    }
                }
                if (str.Contains("-->") && scriptStart == 0)
                {
                    commentStart--;
                }
                if (str.Contains("]]>") && scriptStart == 0)
                {
                    cdataStart--;
                }
            }

            string str2 = "";
            foreach (string str3 in buffer2)
                str2 += (str3 + "\n");
            str2 = Regex.Replace(str2, "<([^\'\">]*((\'[^\']*\')|(\"[^\"]*\")))*[^\'\">]*>", " ");
            str2 = Regex.Replace(str2, "<[^>]*>", " ");
            str2 = Regex.Replace(str2, "&[#a-z0-9]*;", " ");

            string[] tokens2 = str2.Split('\n');
            string retStr = "";
            foreach (string s in tokens2)
            {
                if (!s.Trim().Equals(""))
                {
                    retStr += (s + "\n");
                }
            }
            retStr = retStr.Replace("#@#@#", "$");
            return Regex.Replace(retStr.Trim(), "\\s+", " ");
        }

        private static string getEventType(string commentary, List<string> players, string bowler)
        {
            string eventIndicator = commentary.Split(',')[1].Trim().ToLower();
            string secondaryEventIndicator = "";
            if (commentary.Split(',').Count()>2) 
                secondaryEventIndicator= commentary.Split(',')[2].Trim().ToLower();
            if (eventIndicator.Equals("out") || secondaryEventIndicator.Trim().Equals("out"))
                return "out";
            if (eventIndicator.Contains("four"))
                return "four";
            if (eventIndicator.Contains("wide"))
                return "wide";
            if (eventIndicator.Contains("leg bye"))
                return "leg bye";
            else if (eventIndicator.Contains("bye"))
                return "bye";
            if (eventIndicator.Contains("six"))
                return "six";
            if (eventIndicator.Contains("no ball"))
                return "no ball";
            if (commentary.ToLower().Contains("dropped") && !(commentary.ToLower().Contains("dropped in") || commentary.ToLower().Contains("dropped behind") || commentary.ToLower().Contains("dropped down")))
            {
                int found=0;
                //should contain at least one player name different from bowler.
                foreach(string s in commentary.Split(' '))
                {
                    if(!bowler.Contains(s))
                    {
                        foreach(string p in players)
                            if(p.Contains(s))
                            {
                                found = 1;
                                break;
                            }
                    }
                }
                if (found == 1)
                    return "dropped";
            }
            if (commentary.ToLower().Contains("missed catch"))
                return "dropped";
            if (commentary.ToLower().Contains("run out chance") || commentary.ToLower().Contains("run-out chance"))
                return "run out chance";
            if (commentary.ToLower().Contains("caught and bowled chance") || commentary.ToLower().Contains("caught-and-bowled chance"))
                return "caught and bowled chance";
            if (commentary.ToLower().Contains("stumping chance"))
                return "stumping chance";
            return "";
        }
        private static bool isFreeHit(string commentary, string commentaryPrevBall)
        {
            if (getEventType(commentary, new List<string>(), "").Equals("no ball"))
                return false;
            //to handle cases like -- here comes the free hit.
            if (!commentaryPrevBall.Equals("") && getEventType(commentaryPrevBall, new List<string>(), "").Equals("no ball") && (commentaryPrevBall.ToLower().Contains("free-hit") || commentaryPrevBall.ToLower().Contains("free hit")))
                return true;
            if (commentary.ToLower().Contains("free-hit") || commentary.ToLower().Contains("free hit"))
                return true;
            return false;
        }
        private static bool wasReviewed(string commentary)
        {
            string[] comToks = commentary.ToLower().Split(',');
            if (comToks.Count() < 3)
                return false;
            //ignore first 2 tokens.
            string newCommentary = "";
            for (int i = 2; i < comToks.Count(); i++)
                newCommentary += comToks[i] + " ";
            commentary = Regex.Replace(Regex.Replace(newCommentary.Replace("third umpire", "third_umpire"), "[^a-z0-9'_ ]", " "), "\\s+", " ");
            string[] commentaryToks = commentary.Split(' ');
            string window = "";
            int count = 0;
            //get position of the review word and get 4 words before and after the review word.
            foreach (string t in commentaryToks)
            {
                foreach (string w in Global.reviewWords)
                {
                    if (t.Equals(w))
                    {
                        int val1 = count - 4;
                        int val2 = count + 4;
                        if (val1 < 0)
                            val1 = 0;
                        if (val2 >= commentaryToks.Count())
                            val2 = commentaryToks.Count() - 1;
                        for (int i = val1; i <= val2; i++)
                            window += commentaryToks[i] + " ";
                        break;
                    }
                }
                count++;
            }
            window = " " + window.Trim() + " ";
            //if no negative word is present within 3-4 words from the review word
            if (commentary.Contains("out of reviews"))
                return false;
            if (!window.Trim().Equals("") && !window.Contains("n't") && !window.Contains(" not ") && !window.Contains(" no ") && !window.Contains(" nopes ") && !window.Contains(" nope "))
                return true;
            return false;
        }

        private static int computeRuns(string commentary)
        {
            int runs = 0;
            string runIndicator = commentary.Split(',')[1].Trim().ToLower();
            if (runIndicator.Equals("four"))
                return 4;
            if (runIndicator.Equals("2 runs"))
                return 2;
            if (runIndicator.Equals("no run"))
                return 0;
            if (runIndicator.Equals("1 wide"))
                return 1;
            if (runIndicator.Equals("1 run"))
                return 1;
            if (runIndicator.Equals("1 leg bye"))
                return 1;
            if (runIndicator.Equals("six"))
                return 6;
            if (runIndicator.Equals("out"))
                return 0;
            if (runIndicator.Equals("(no ball) 1 run"))
                return 1;
            if (runIndicator.Equals("(no ball) 2 runs"))
                return 2;
            if (runIndicator.Equals("(no ball) 3 runs"))
                return 3;
            if (runIndicator.Equals("(no ball) four"))
                return 5;
            if (runIndicator.Equals("1 bye"))
                return 1;
            if (runIndicator.Equals("1 no ball"))
                return 1;
            if (runIndicator.Equals("2 byes"))
                return 2;
            if (runIndicator.Equals("2 leg byes"))
                return 2;
            if (runIndicator.Equals("2 no balls"))
                return 2;
            if (runIndicator.Equals("2 wides"))
                return 2;
            if (runIndicator.Equals("3 leg byes"))
                return 3;
            if (runIndicator.Equals("3 wides"))
                return 3;
            if (runIndicator.Equals("4 byes"))
                return 4;
            if (runIndicator.Equals("4 leg byes"))
                return 4;
            if (runIndicator.Equals("4 wides"))
                return 4;
            if (runIndicator.Equals("5 no balls"))
                return 5;
            if (runIndicator.Equals("5 wides"))
                return 5;
            if (runIndicator.Equals("5 runs"))
                return 5;
            if (runIndicator.Equals("3 runs"))
                return 3;
            return runs;
        }
        public static void Main(string[] args)
        {
            StreamWriter sw = new StreamWriter(dir + "matchCommentary.txt");
            for (int i = 1; i <= Global.numMatches; i++)
            {
                List<string> innings1BatsmenNames = new List<string>();
                List<string> innings2BatsmenNames = new List<string>();
                Dictionary<string, string> inningsBall2Wickets = new Dictionary<string, string>();
                string firstCountry = "";
                string secondCountry = "";
                string winningCountry = "";
                //get batsmen and Country from scorecard files.
                StreamReader sr2 = new StreamReader(dir + "match"+i+"Scorecard.html");
                string str = "";
                int innings1Done = 0;
                HashSet<string> countries = new HashSet<string>();
                while((str=sr2.ReadLine())!=null)
                {
                    if (str.Contains("<p class=\"statusText\">"))
                        winningCountry = Regex.Split(convertHTMLToText(str), "won")[0].Trim();
                    if(str.Contains("(50 overs maximum)"))
                        firstCountry = Regex.Split(convertHTMLToText(str), "innings")[0].Trim();
                    if(str.Contains("> v <a href"))
                    {
                        string tmp = convertHTMLToText(str);
                        countries.Add(Regex.Split(tmp, " v ")[0].Trim());
                        countries.Add(Regex.Split(tmp, " v ")[1].Trim());
                    }
                    if (str.Contains("view the player profile for") && (str.Contains("<td width=\"192\"") || str.Contains("<span><a href")))
                    {
                        if(str.Contains("dagger"))
                            Console.WriteLine();
                        str = convertHTMLToText(str).Replace("Did not bat", "");
                        str = Regex.Replace(str, "[^a-zA-Z-' ]", "");
                        if (innings1Done == 0)
                            innings1BatsmenNames.Add(str.Trim());
                        else
                            innings2BatsmenNames.Add(str.Trim());
                    }
                    if (str.Contains("Fall of wickets"))
                    {
                        string []tmp = Regex.Replace(str, "<[^>]*>", "").Split('(');
                        foreach(string t in tmp)
                        {
                            if (!t.Contains("ov)"))
                                continue;
                            string ball = t.Split(',')[1].Trim().Split(' ')[0];
                            string player = t.Split(',')[0];
                            if (innings1Done == 0)
                                inningsBall2Wickets["1_" + ball] = player;
                            else
                                inningsBall2Wickets["2_" + ball] = player;
                        }
                        innings1Done = 1;
                    }
                    if (str.Contains("(target: "))
                    {
                        innings1Done = 1;
                        secondCountry = Regex.Split(convertHTMLToText(str), "innings")[0].Trim();
                    }
                }
                sr2.Close();
                if(secondCountry.Equals(""))
                {
                    foreach (string c in countries)
                        if (!firstCountry.Equals(c))
                            secondCountry = c;
                }
                StreamWriter sw2 = new StreamWriter(dir+"matchCommentary.schema");
                sw2.WriteLine("Match#\nInnings#\nBall#\nFirst Bat Country\nSecond Bat Country\nWinning Country\nStriker\nOther Batsman\nRuns scored from this ball");
                sw2.WriteLine("Bowler Name\nTotal Runs so far\nTotal Wickets so far\nRun Rate\nRequired Run Rate\nPrevious Bowler");
                sw2.WriteLine("Striker Batsman Stats\nOther Batsman Stats\nBowler Stats\nEvent Type\nWas Reviewed\nFree-hit\nBall Type\nShot Type");
                sw2.WriteLine("Fielding Position\nCricket Vocabulary Words\nOut Player\nCommentary");
                sw2.Close();
                for(int j=1;j<=Global.numInningsPerMatch;j++)
                {
                    StreamReader sr = new StreamReader(dir+"match"+i+"innings"+j+"Commentary.html");
                    string ballNum = "";
                    string prevBallNum = "";
                    int count = 0;
                    string commentary = "";
                    int start = 0;
                    int totalRuns = 0;
                    int totalWickets = 0;
                    double rr = 0.0;
                    double rrr = 0.0;
                    string bowler = "";
                    string batsman1 = "";
                    HashSet<string> batters = new HashSet<string>();
                    if (j == 1)
                    {
                        batters.Add(innings1BatsmenNames[0]);
                        batters.Add(innings1BatsmenNames[1]);
                    }
                    else
                    {
                        batters.Add(innings2BatsmenNames[0]);
                        batters.Add(innings2BatsmenNames[1]);
                    }
                    batsmanStatsMap = new Dictionary<string, string>();
                    bowlerStatsMap = new Dictionary<string, string>();
                    if (j == 1)
                    {
                        foreach (string s in innings1BatsmenNames)
                            batsmanStatsMap[s] = "";
                        foreach (string s in innings2BatsmenNames)
                            bowlerStatsMap[s] = "";
                    }
                    else
                    {
                        foreach (string s in innings2BatsmenNames)
                            batsmanStatsMap[s] = "";
                        foreach (string s in innings1BatsmenNames)
                            bowlerStatsMap[s] = "";
                    }
                    string prevBowler = "";
                    string prevCommentary = "";
                    while((str=sr.ReadLine())!=null)
                    {
                        if (str.Contains("commsText") && str.Contains("<td w"))
                            start = 1;
                        if (start == 1 && str.Contains("</tr>"))
                            start = 0;
                        if(str.Contains("<td class=\"bbover\">"))
                        {
                            string person = "";
                            if(str.Contains("<b>"))
                            {
                                person = convertHTMLToText(str);
                                str=sr.ReadLine();
                                str=sr.ReadLine();
                                string stat = convertHTMLToText(str);
                                int found = 0;
                                foreach (string s in batsmanStatsMap.Keys)
                                    if (s.ToLower().Contains(person.ToLower()))
                                    {
                                        batsmanStatsMap[s] = stat;
                                        batters.Add(s);
                                        found = 1;
                                        break;
                                    }
                                if(found==0)
                                    foreach (string s in bowlerStatsMap.Keys)
                                        if (s.ToLower().Contains(person.ToLower()))
                                        {
                                            bowlerStatsMap[s] = stat;
                                            found = 1;
                                            break;
                                        }
                            }
                        }
                        if(str.Contains("<span style=\"color:#123863;"))
                        {
                            string[] toks = str.Split('>')[1].Split('/')[0].Split(' ');
                            totalRuns = int.Parse(toks[toks.Length-1]);
                            totalWickets= int.Parse(str.Split('>')[1].Split('/')[1].Split('<')[0]);
                            if (str.Contains("RR"))
                                rr = double.Parse(Regex.Split(str, "RR:")[1].Split(')')[0].Split(',')[0]);
                            else
                                rr = 0;
                            if (str.Contains("RRR"))
                                rrr = double.Parse(Regex.Split(str, "RRR:")[1].Split(')')[0]);
                            else
                                rrr = 0;
                            prevBowler = bowler;
                        }
                        if(start==1)
                        {
                            if (str.Contains("commsText") && str.Contains("<td w"))
                                ballNum = str.Split('>')[2].Split('<')[0];
                            else
                                commentary += " " + Regex.Replace(str, "<[^>]*>", "");
                        }
                        if (start == 0 && !ballNum.Equals(""))
                        {
                            if (ballNum.Equals(prevBallNum))
                                count++;
                            else
                                count = 0;
                            string first=Regex.Split(commentary," to ")[0].Trim();
                            string second=Regex.Split(commentary,"to")[1].Split(',')[0].Trim();
                            if (j == 1)
                            {
                                foreach (string s in innings2BatsmenNames)
                                    if (s.ToLower().Contains(first.ToLower()))
                                    {
                                        bowler = s;
                                        break;
                                    }

                                foreach (string s in innings1BatsmenNames)
                                    if (s.ToLower().Contains(second.ToLower()))
                                    {
                                        batsman1 = s;
                                        batters.Add(s);
                                        break;
                                    }
                            }
                            else
                            {
                                foreach (string s in innings1BatsmenNames)
                                    if (s.ToLower().Contains(first.ToLower()))
                                    {
                                        bowler = s;
                                        break;
                                    }

                                foreach (string s in innings2BatsmenNames)
                                    if (s.ToLower().Contains(second.ToLower()))
                                    {
                                        batsman1 = s;
                                        batters.Add(s);
                                        break;
                                    }
                            }

                            int runs=computeRuns(commentary);
                            totalRuns += runs;
                            string eventType = getEventType(commentary, j==1?innings2BatsmenNames:innings1BatsmenNames, bowler);
                            if (eventType.Equals("out"))
                            {
                                totalWickets += 1;
                                batters.Remove(batsman1);
                                if (totalWickets != 10)
                                {
                                    if (j == 1)
                                        batters.Add(innings1BatsmenNames[totalWickets + 1]);
                                    else
                                        batters.Add(innings2BatsmenNames[totalWickets + 1]);
                                }
                            }
                            string batsman2 = "";
                            foreach (string s in batters)
                                if (!s.Equals(batsman1))
                                    batsman2 = s;
                            //updateBatsmanStats(batsman1, runs, eventType);
                            //eventType could be wide, out, no, four, six
                            updateBatsmanStats(batsman1,runs,eventType);
                            updateBowlerStats(bowler, runs, eventType);
                            string outPlayer = "";
                            if(eventType.Equals("out"))
                            {
                                outPlayer = inningsBall2Wickets[j + "_" + ballNum];
                                //get standard name of the player
                                if(j==1)
                                {
                                    foreach (string b in innings1BatsmenNames)
                                        if (b.Contains(outPlayer))
                                            outPlayer = b;
                                }
                                else
                                {
                                    foreach (string b in innings2BatsmenNames)
                                        if (b.Contains(outPlayer))
                                            outPlayer = b;
                                }
                            }
                            
                            sw.WriteLine(i + "\t" + j + "\t" + ballNum + "." + count + "\t" + firstCountry + "\t" + secondCountry + "\t" + winningCountry + "\t" + batsman1 + "\t" + batsman2 + "\t" + runs + "\t" + bowler + "\t" + totalRuns + "\t" + totalWickets + "\t" + rr + "\t" + rrr + "\t" + prevBowler + "\t" + batsmanStatsMap[batsman1] + "\t" + batsmanStatsMap[batsman2] + "\t" + bowlerStatsMap[bowler] + "\t" + eventType + "\t" + wasReviewed(commentary) + "\t" +(isFreeHit(commentary, prevCommentary)?"Free Hit":"")+ "\t" + getBallType(commentary) + "\t" + getShotType(commentary) + "\t" + getFieldingPosition(commentary) + "\t" + getImportantCricketWords(commentary) + "\t" + outPlayer+"\t"+commentary.Trim());
                            prevBallNum = ballNum;
                            prevCommentary = commentary;
                            commentary = "";
                            ballNum = "";
                        }
                    }
                    sr.Close();
                }
            }
            sw.Close();
        }

        private static void updateBowlerStats(string bowler, int runs, string eventType)
        {
            string overs = "0";
            int maidens = 0;
            int curRuns = 0;
            int wickets = 0;
            if(!bowlerStatsMap[bowler].Equals(""))
            {
                string[] toks = bowlerStatsMap[bowler].Split('-');
                overs = toks[0];
                maidens = int.Parse(toks[1]);
                curRuns = int.Parse(toks[2]);
                wickets = int.Parse(toks[3]);
            }
            if (eventType.Equals("out"))
                wickets++;
            curRuns += runs;
            bowlerStatsMap[bowler] = overs + "-" + maidens + "-" + curRuns + "-" + wickets;
        }

        private static void updateBatsmanStats(string batsman1, int runs, string eventType)
        {
            string stats = batsmanStatsMap[batsman1];
            int currentRuns = 0;
            if(!stats.Equals(""))
                currentRuns = int.Parse(stats.Split(' ')[0].Replace("*",""));
            int curFours = 0;
            if (!stats.Equals("") && stats.Contains("x4"))
            {
                string[] toks = stats.Split(' ');
                foreach (string t in toks)
                    if (t.Contains("x4"))
                        curFours = int.Parse(t.Split('x')[0]);
            }
            int curSixes = 0;
            if (!stats.Equals("") && stats.Contains("x6"))
            {
                string[] toks = stats.Split(' ');
                foreach (string t in toks)
                    if (t.Contains("x6"))
                        curSixes = int.Parse(t.Split('x')[0]);
            }
            int curBalls = 0;
            if(!stats.Equals(""))
                curBalls = int.Parse(stats.Split('(')[1].Split('b')[0]);
            if (!(eventType.Equals("wide") || eventType.Equals("no ball") || eventType.Equals("bye") || eventType.Equals("leg bye")))
                currentRuns += runs;
            if (eventType.Equals("four"))
                curFours++;
            if (eventType.Equals("six"))
                curSixes++;
            curBalls++;
            batsmanStatsMap[batsman1] = currentRuns+"* ("+curBalls+"b "+curFours+"x4 "+curSixes+"x6)";
            //Did not mess around with the * because we are unsure who is out.
            //if(eventType.Equals("out"))
            //    batsmanStatsMap[batsman1] = currentRuns+" ("+curBalls+"b "+curFours+"x4 "+curSixes+"x6)";
        }

        private static string getShotType(string commentary)
        {
            string shotType = "";
            string[] comToks = commentary.ToLower().Split(',');
            if (comToks.Count() < 3)
                return "";
            //ignore first 2 tokens.
            HashSet<string> set = new HashSet<string>(Global.shots);
            foreach (string s in Global.shotKeywords)
                set.Add(s); 
            for (int i = 2; i < comToks.Count(); i++)
            {
                string[] toks = comToks[i].Trim().Split(' ');
                foreach (string s in set)
                    if (comToks[i].Contains(s))
                        shotType += s + ";";
            }
            return shotType;
        }

        private static string getBallType(string commentary)
        {
            string ballType = "";
            string[] comToks = commentary.ToLower().Split(',');
            if (comToks.Count() < 3)
                return "";
            //ignore first 2 tokens.
            HashSet<string> set = new HashSet<string>(Global.balls);
            for (int i = 2; i < comToks.Count(); i++)
            {
                string[] toks = comToks[i].Trim().Split(' ');
                foreach (string s in set)
                    if (comToks[i].Contains(s))
                        ballType += s + ";";
            }
            return ballType;
        }

        private static string getFieldingPosition(string commentary)
        {
            string fieldPos = "";
            string[] comToks = commentary.ToLower().Split(',');
            if (comToks.Count() < 3)
                return "";
            //ignore first 2 tokens.
            HashSet<string> set = new HashSet<string>(Global.fieldPos);
            for (int i = 2; i < comToks.Count(); i++)
            {
                string[] toks = comToks[i].Trim().Split(' ');
                foreach (string s in set)
                    if (comToks[i].Contains(s))
                        fieldPos += s + ";";
            }
            return fieldPos;
        }
        private static string getImportantCricketWords(string commentary)
        {
            string impCricWords = "";
            string[] comToks = commentary.ToLower().Split(',');
            if (comToks.Count() < 3)
                return "";
            //ignore first 2 tokens.
            HashSet<string> set = new HashSet<string>(Global.cricketVocab);
            for (int i = 2; i < comToks.Count(); i++)
            {
                string[] toks = comToks[i].Trim().Split(' ');
                foreach (string s in set)
                    if (comToks[i].Contains(s))
                        impCricWords +=s + ";";
            }
            return impCricWords;
        }
    }
}
