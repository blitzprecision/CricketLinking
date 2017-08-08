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
    /// Creates derived entities at innings level or player level as follows:
    /// Innings level: POWERPLAY,WICKETS,EXTRAS,WIDES,SINGLES,TWOS,LASTOVER,NOBALLS,FREEHITS,DOT BALLS
    /// Players level: BAT,BOWL,SIX,FOUR,YORKERS,REVERSE SWEEPS
    /// Pair of players level: PARTNERSHIP
    /// </summary>
    class DerivedEntitiesComputation
    {
        public static string dir = Global.baseDir;
        public static Dictionary<string, HashSet<string>> country2Players = new Dictionary<string, HashSet<string>>();
        public static Dictionary<string, string> player2Country = new Dictionary<string, string>();
        public static List<string> innings1BatsmenNames = new List<string>();
        public static List<string> innings2BatsmenNames = new List<string>();
        public static string firstCountry = "";
        public static int entityID = 0;
        public static string secondCountry = "";
        public static string winningCountry = "";
        public static List<string> entities = new List<string>();
        public static List<List<string>> commentaryForMatch = new List<List<string>>();

        static void Main(string[] args)
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
                player2Country[player] = country;
            }
            sr2.Close();
            
            StreamWriter sw = new StreamWriter(dir + "derivedEntities.txt");
            StreamReader sr = new StreamReader(dir+"matchCommentary.txt");
            while((str=sr.ReadLine())!=null)
            {
                int match = int.Parse(str.Split('\t')[0]);
                if (commentaryForMatch.Count() <= match-1)
                    commentaryForMatch.Add(new List<string>());
                commentaryForMatch[match-1].Add(str);
            }
            sr.Close();
            for (int match = 1; match <= Global.numMatches;match++)
            {
                //Entities must be listed in a key value form.
                getPlayerNames(match); //get names of players for both innings
                getPowerplayEntities(match);//read scorecard file and generate powerplay entities: entityID, MatchNumber, InningsNum, EntityName=Powerplay, PowerplayNumber, Powerplay Highlights, List of Ball Numbers, Wickets lost, Fours, Sixes, #runs.
                getExtraEntities(match);
                getWideEntities(match);
                getSinglesEntities(match);
                getTwosEntities(match);
                getLastoverEntities(match);
                getNoballEntities(match);
                getFreehitEntities(match);
                getDotballsEntities(match);
                generateBATEntities(match);
                generateBOWLEntities(match);
                generateOVERSEntities(match);
                generateBOWLBATEntities(match);
                generateWicketEntities(match);
                generateSixEntities(match);
                generateFourEntities(match);
                generateYorkerEntities(match);
                generateReverseSweepEntities(match);
                generateSweepEntities(match);
                generatePartnershipEntities(match);
                generateReferralEntities(match);
                generateDroppedEntities(match);

                //for each player
                //  generate BAT entity: entityID, MatchNumber, InningsNum, EntityName=BAT(Player name), Total Runs, #Sixes, #Fours, List of Ball Numbers.
                //  generate BOWL entity: entityID, MatchNumber, InningsNum, EntityName=BOWL(Player name), Total Runs, #Maiden overs, #Wickets, Run rate conceded, List of Ball Numbers.
                //  generate WICKETS entity: entityID, MatchNumber, InningsNum, EntityName=WICKETS(Player name), Names of players out, #wickets, List of Ball Numbers.
                //  generate SIX entity: entityID, MatchNumber, InningsNum, EntityName=SIX(Player name), #SIX, List of Ball Numbers.
                //  generate FOUR entity: entityID, MatchNumber, InningsNum, EntityName=FOUR(Player name), #FOUR, List of Ball Numbers.
                //  generate YORKERS entity: entityID, MatchNumber, InningsNum, EntityName=YORKERS(Player name), List of Ball Numbers.
                //  generate REVERSE SWEEPS entity: entityID, MatchNumber, InningsNum, EntityName=REVERSE SWEEPS(Player name), List of Ball Numbers.
                //end for

                //for each pair of players
                // generate PARTNERSHIP entity: entityID, MatchNumber, InningsNum, EntityName=PARTNERSHIP(Player names), Total Runs, #Sixes, #Fours, List of Ball Numbers.
                //end for
            }
            foreach (string e in entities)
                sw.WriteLine(e);
                sw.Close();
        }

        private static void generateReferralEntities(int match)
        {
            string innings1ToBalls = "";
            string innings2ToBalls = "";
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (!toks[19].Equals("True"))
                    continue;
                if (toks[1].Equals("1"))
                    innings1ToBalls += toks[2] + ",";
                else if (toks[1].Equals("2"))
                    innings2ToBalls += toks[2] + ",";
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=REFERRALS(INNINGS1)\tListOfBalls=" + print(1, innings1ToBalls));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=REFERRALS(INNINGS2)\tListOfBalls=" + print(2, innings2ToBalls));
            entityID++;
        }

        private static void generateDroppedEntities(int match)
        {
            string innings1ToBalls = "";
            string innings2ToBalls = "";
            string[] droppedWords = new string[] { "dropped", "shelled", "sitter", "dropping", "spilled" };
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                int val = 0;
                foreach (string d in droppedWords)
                    if (toks[26].Contains(d))
                        val = 1;
                if (val != 1)
                    continue;
                if (toks[1].Equals("1"))
                    innings1ToBalls += toks[2] + ",";
                else if (toks[1].Equals("2"))
                    innings2ToBalls += toks[2] + ",";
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=DROPPED(INNINGS1)\tListOfBalls=" + print(1, innings1ToBalls));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=DROPPED(INNINGS2)\tListOfBalls=" + print(2, innings2ToBalls));
            entityID++;
        }

        private static void generateSweepEntities(int match)
        {
            //  generate SWEEP entity: entityID, MatchNumber, InningsNum, EntityName=SWEEPS(Player name), List of Ball Numbers.
            Dictionary<string, string> batsman2Balls = new Dictionary<string, string>();
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string batsman = toks[6];
                string shotType = toks[22];
                if (shotType.Contains("sweep"))
                {
                    if (batsman2Balls.ContainsKey(batsman))
                        batsman2Balls[batsman] += toks[2] + ",";
                    else
                        batsman2Balls[batsman] = toks[2] + ",";
                }
            }
            foreach (string batsman in batsman2Balls.Keys)
            {
                if (innings1BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=SWEEPS(" + batsman + ")\tListOfBalls=" + print(1, batsman2Balls[batsman]));
                else if (innings2BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=SWEEPS(" + batsman + ")\tListOfBalls=" + print(2, batsman2Balls[batsman]));
                entityID++;
            }
        }

        private static void generateOVERSEntities(int match)
        {
            string firstBatCountry = commentaryForMatch[match - 1][0].Split('\t')[3];
            string secondBatCountry = commentaryForMatch[match - 1][0].Split('\t')[4];
            Dictionary<string, string> entity2Balls = new Dictionary<string, string>();
            entity2Balls[firstBatCountry] = "";
            entity2Balls[secondBatCountry] = "";
            entity2Balls["INNINGS1"] = "";
            entity2Balls["INNINGS2"] = "";
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if(toks[1].Equals("1"))
                {
                    entity2Balls["INNINGS1"] += toks[2] + ",";
                    entity2Balls[firstBatCountry] += toks[2] + ",";
                }
                else
                {
                    entity2Balls["INNINGS2"] += toks[2] + ",";
                    entity2Balls[secondBatCountry] += toks[2] + ",";
                }
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=OVERS(INNINGS1)\tListOfBalls=" + print(1,entity2Balls["INNINGS1"]));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=OVERS(INNINGS2)\tListOfBalls=" + print(2,entity2Balls["INNINGS2"]));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=OVERS(" + firstBatCountry + ")\tListOfBalls=" + print(1,entity2Balls[firstBatCountry]));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=OVERS(" + secondBatCountry + ")\tListOfBalls=" + print(2,entity2Balls[secondBatCountry]));
            entityID++;
        }

        private static void generatePartnershipEntities(int match)
        {
        // generate PARTNERSHIP entity: entityID, MatchNumber, InningsNum, EntityName=PARTNERSHIP(Player names), Total Runs, #Sixes, #Fours, List of Ball Numbers.
            Dictionary<string, string> partnership2Balls = new Dictionary<string, string>();
            Dictionary<string, int> partnership2Sixes = new Dictionary<string, int>();
            Dictionary<string, int> partnership2Fours = new Dictionary<string, int>();
            Dictionary<string, int> partnership2Runs = new Dictionary<string, int>();

            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string batsman1 = toks[6];
                string batsman2 = toks[7];
                string eventType = toks[18];
                string stats = toks[15];
                int runs = int.Parse(stats.Split(' ')[0].Replace("*", ""));
                int sixes = 0;
                int fours = 0;
                if (!stats.Equals("") && stats.Contains("x4"))
                {
                    string[] toks2 = stats.Split(' ');
                    foreach (string t in toks2)
                        if (t.Contains("x4"))
                            fours = int.Parse(t.Split('x')[0]);
                }
                if (!stats.Equals("") && stats.Contains("x6"))
                {
                    string[] toks2 = stats.Split(' ');
                    foreach (string t in toks2)
                        if (t.Contains("x6"))
                            sixes = int.Parse(t.Split('x')[0]);
                }
                if(batsman1.GetHashCode()>batsman2.GetHashCode())
                {
                    string tmp = batsman2;
                    batsman2 = batsman1;
                    batsman1 = tmp;
                }
                string key = batsman1 + "_" + batsman2;
                if (partnership2Balls.ContainsKey(key))
                    partnership2Balls[key] += toks[2] + ",";
                else
                    partnership2Balls[key] = toks[2] + ",";
                partnership2Sixes[key] = sixes;
                partnership2Fours[key] = fours;
                partnership2Runs[key] = runs;
            }
            foreach (string key in partnership2Balls.Keys)
            {
                if (innings1BatsmenNames.Contains(key.Split('_')[0]))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=PARTNERSHIP(" + key.Replace('_', ',') + ")\tTotal Runs=" + partnership2Runs[key] + "\tSixes=" + partnership2Sixes[key] + "\tFours=" + partnership2Fours[key] + "\tListOfBalls=" + print(1,partnership2Balls[key]));
                else if (innings2BatsmenNames.Contains(key.Split('_')[0]))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=PARTNERSHIP(" + key.Replace('_', ',') + ")\tTotal Runs=" + partnership2Runs[key] + "\tSixes=" + partnership2Sixes[key] + "\tFours=" + partnership2Fours[key] + "\tListOfBalls=" + print(2,partnership2Balls[key]));
                entityID++;
            }
        }

        private static void generateReverseSweepEntities(int match)
        {
            //  generate REVERSE SWEEPS entity: entityID, MatchNumber, InningsNum, EntityName=REVERSE SWEEPS(Player name), List of Ball Numbers.
            Dictionary<string, string> batsman2Balls = new Dictionary<string, string>();
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string batsman = toks[6];
                string shotType = toks[22];
                if (shotType.Contains("reverse sweep"))
                {
                    if (batsman2Balls.ContainsKey(batsman))
                        batsman2Balls[batsman] += toks[2] + ",";
                    else
                        batsman2Balls[batsman] = toks[2] + ",";
                }
            }
            foreach (string batsman in batsman2Balls.Keys)
            {
                if (innings1BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=REVERSESWEEPS(" + batsman + ")\tListOfBalls=" + print(1, batsman2Balls[batsman]));
                else if (innings2BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=REVERSESWEEPS(" + batsman + ")\tListOfBalls=" + print(2, batsman2Balls[batsman]));
                entityID++;
            }
        }

        private static void generateYorkerEntities(int match)
        {
            //  generate YORKERS entity: entityID, MatchNumber, InningsNum, EntityName=YORKERS(Player name), List of Ball Numbers.
            Dictionary<string, string> bowler2Balls = new Dictionary<string, string>();
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string bowler = toks[9];
                string ballType = toks[21];
                if (ballType.Contains("yorker"))
                {
                    if (bowler2Balls.ContainsKey(bowler))
                        bowler2Balls[bowler] += toks[2] + ",";
                    else
                        bowler2Balls[bowler] = toks[2] + ",";
                }
            }
            foreach (string bowler in bowler2Balls.Keys)
            {
                if (innings2BatsmenNames.Contains(bowler))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=YORKERS(" + bowler + ")\tListOfBalls=" + print(1,bowler2Balls[bowler]));
                else if (innings1BatsmenNames.Contains(bowler))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=YORKERS(" + bowler + ")\tListOfBalls=" + print(2,bowler2Balls[bowler]));
                entityID++;
            }
        }

        private static void generateFourEntities(int match)
        {
            //  generate FOUR entity: entityID, MatchNumber, InningsNum, EntityName=FOUR(Player name), #FOUR, List of Ball Numbers.
            Dictionary<string, string> batsman2Balls = new Dictionary<string, string>();
            Dictionary<string, int> batsman2Fours = new Dictionary<string, int>();
            string fours1Balls = "";
            string fours2Balls = "";

            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string batsman = toks[6];
                string eventType = toks[18];
                string stats = toks[15];
                int fours = 0;
                if (!stats.Equals("") && stats.Contains("x4"))
                {
                    string[] toks2 = stats.Split(' ');
                    foreach (string t in toks2)
                        if (t.Contains("x4"))
                            fours = int.Parse(t.Split('x')[0]);
                }
                if (eventType.Equals("four"))
                {
                    if (batsman2Balls.ContainsKey(batsman))
                        batsman2Balls[batsman] += toks[2] + ",";
                    else
                        batsman2Balls[batsman] =  toks[2] + ",";
                    if (innings1BatsmenNames.Contains(batsman))
                        fours1Balls += (toks[2] + ",");
                    if (innings2BatsmenNames.Contains(batsman))
                        fours2Balls += (toks[2] + ",");
                }
                batsman2Fours[batsman] = fours;
            }
            foreach (string batsman in batsman2Balls.Keys)
            {
                if (innings1BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=FOUR(" + batsman + ")\tFours=" + batsman2Fours[batsman] + "\tListOfBalls=" + print(1, batsman2Balls[batsman]));
                else if (innings2BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=FOUR(" + batsman + ")\tFours=" + batsman2Fours[batsman] + "\tListOfBalls=" + print(2, batsman2Balls[batsman]));
                entityID++;
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=FOUR(INNINGS1)\tListOfBalls=" + print(1, fours1Balls));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=FOUR(INNINGS2)\tListOfBalls=" + print(2, fours2Balls));
            entityID++;
        }

        private static void generateSixEntities(int match)
        {
            //  generate SIX entity: entityID, MatchNumber, InningsNum, EntityName=SIX(Player name), #SIX, List of Ball Numbers.
            Dictionary<string, string> batsman2Balls = new Dictionary<string, string>();
            Dictionary<string, int> batsman2Sixes = new Dictionary<string, int>();
            string six1Balls = "";
            string six2Balls = "";

            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string batsman = toks[6];
                string eventType = toks[18];
                string stats = toks[15];
                int sixes = 0;
                if (!stats.Equals("") && stats.Contains("x6"))
                {
                    string[] toks2 = stats.Split(' ');
                    foreach (string t in toks2)
                        if (t.Contains("x6"))
                            sixes = int.Parse(t.Split('x')[0]);
                }
                if (eventType.Equals("six"))
                {
                    if (batsman2Balls.ContainsKey(batsman))
                        batsman2Balls[batsman] += toks[2] + ",";
                    else
                        batsman2Balls[batsman] =  toks[2] + ",";
                    if (innings1BatsmenNames.Contains(batsman))
                        six1Balls += (toks[2] + ",");
                    if (innings2BatsmenNames.Contains(batsman))
                        six2Balls += (toks[2] + ",");
                }
                batsman2Sixes[batsman] = sixes;
            }
            foreach (string batsman in batsman2Balls.Keys)
            {
                if (innings1BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=SIX(" + batsman + ")\tSixes=" + batsman2Sixes[batsman] + "\tListOfBalls=" + print(1, batsman2Balls[batsman]));
                else if (innings2BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=SIX(" + batsman + ")\tSixes=" + batsman2Sixes[batsman] + "\tListOfBalls=" + print(2, batsman2Balls[batsman]));
                entityID++;
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=SIX(INNINGS1)\tListOfBalls=" + print(1, six1Balls));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=SIX(INNINGS2)\tListOfBalls=" + print(2, six2Balls));
            entityID++;
        }

        private static void generateWicketEntities(int match)
        {
            //  generate WICKETS entity: entityID, MatchNumber, InningsNum, EntityName=WICKETS(Player name/INNINGS1/INNINGS2/Country Name), Names of players out, #wickets, List of Ball Numbers.
            Dictionary<string, int> bowler2Wickets = new Dictionary<string, int>();
            Dictionary<string, string> bowler2WicketsNames = new Dictionary<string, string>();
            Dictionary<string, string> bowler2Balls = new Dictionary<string, string>();
            string firstBatCountry = "";
            string secondBatCountry = "";
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string bowler = toks[9];
                string country = "";
                int innings=1;
                if (innings2BatsmenNames.Contains(bowler))
                {
                    firstBatCountry=toks[3];
                    country = toks[3];
                }
                else
                {
                    secondBatCountry = toks[4];
                    country = toks[4];
                    innings = 2;
                }
                string eventType = toks[18];
                string stats = toks[17];
                int wickets = 0;
                if (!stats.Equals(""))
                    wickets = int.Parse(stats.Split('-')[3]);
                if (eventType.Equals("out"))
                {
                    if (bowler2Balls.ContainsKey(bowler))
                    {
                        bowler2Balls[bowler] += toks[2] + ",";
                        bowler2WicketsNames[bowler] += toks[25] + ",";
                    }
                    else
                    {
                        bowler2Balls[bowler] = toks[2] + ",";
                        bowler2WicketsNames[bowler] = toks[25] + ",";
                    }
                    if (bowler2Balls.ContainsKey(country))
                    {
                        bowler2Balls[country] += toks[2] + ",";
                        bowler2WicketsNames[country] += toks[25] + ",";
                    }
                    else
                    {
                        bowler2Balls[country] = toks[2] + ",";
                        bowler2WicketsNames[country] = toks[25] + ",";
                    }
                    if (bowler2Balls.ContainsKey("INNINGS"+innings))
                    {
                        bowler2Balls["INNINGS" + innings] += toks[2] + ",";
                        bowler2WicketsNames["INNINGS" + innings] += toks[25] + ",";
                    }
                    else
                    {
                        bowler2Balls["INNINGS" + innings] = toks[2] + ",";
                        bowler2WicketsNames["INNINGS" + innings] = toks[25] + ",";
                    }
                    if (bowler2Wickets.ContainsKey(country))
                        bowler2Wickets[country]++;
                    else
                        bowler2Wickets[country] = 1;
                    if (bowler2Wickets.ContainsKey("INNINGS" + innings))
                        bowler2Wickets["INNINGS" + innings]++;
                    else
                        bowler2Wickets["INNINGS" + innings] = 1;
                }
                bowler2Wickets[bowler] = wickets;
            }
            foreach (string bowler in bowler2Balls.Keys)
            {
                if(innings2BatsmenNames.Contains(bowler)||bowler.Equals("INNINGS1")||bowler.Equals(firstBatCountry))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=WICKETS(" + bowler + ")\tTotal Wickets=" + bowler2Wickets[bowler] +
                    "\tNames of Players out=" + bowler2WicketsNames[bowler] + "\tListOfBalls=" + print(1,bowler2Balls[bowler]));
                else if (innings1BatsmenNames.Contains(bowler) || bowler.Equals("INNINGS2") || bowler.Equals(secondBatCountry))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=WICKETS(" + bowler + ")\tTotal Wickets=" + bowler2Wickets[bowler] +
                    "\tNames of Players out=" + bowler2WicketsNames[bowler] + "\tListOfBalls=" + print(2, bowler2Balls[bowler]));
                entityID++;
            }
        }

        private static string print(int p1, string p2)
        {
            if (p2.Trim().Equals(""))
                return "";
            string str="";
            string[] toks = p2.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string t in toks)
                str += p1 + ":" + t.Trim() + ",";
            str = str.Substring(0, str.Length - 1);
            return str;
        }

        private static void generateBOWLBATEntities(int match)
        {
            Dictionary<string, string> batsmanbowler2Balls = new Dictionary<string, string>();
            Dictionary<string, int> batsmanbowler2Sixes = new Dictionary<string, int>();
            Dictionary<string, int> batsmanbowler2Fours = new Dictionary<string, int>();
            Dictionary<string, int> batsmanbowler2Runs = new Dictionary<string, int>();

            //generate BOWLBAT entity: entityID, MatchNumber, InningsNum, EntityName=BAT(Bowler name,Batsman name), Total Runs, #Sixes, #Fours, List of Ball Numbers.
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string bowler = toks[9];
                string batsman = toks[6];
                string eventType = toks[18];
                string stats = toks[15];
                int runs = int.Parse(toks[8]);
                if (batsmanbowler2Balls.ContainsKey(batsman + "_" + bowler))
                {
                    batsmanbowler2Balls[batsman + "_" + bowler] += toks[2] + ",";
                    if(eventType.Equals("four"))
                        batsmanbowler2Fours[batsman + "_" + bowler]++;
                    if (eventType.Equals("six"))
                        batsmanbowler2Sixes[batsman + "_" + bowler]++;
                    batsmanbowler2Runs[batsman + "_" + bowler] += runs;
                }
                else
                {
                    batsmanbowler2Balls[batsman + "_" + bowler] = toks[2] + ",";
                    batsmanbowler2Fours[batsman + "_" + bowler] = batsmanbowler2Sixes[batsman + "_" + bowler] = 0;
                    if (eventType.Equals("four"))
                        batsmanbowler2Fours[batsman + "_" + bowler]=1;
                    if (eventType.Equals("six"))
                        batsmanbowler2Sixes[batsman + "_" + bowler]=1;
                    batsmanbowler2Runs[batsman + "_" + bowler] = runs;
                }
            }
            foreach (string batsmanbowler in batsmanbowler2Balls.Keys)
            {
                string batsman = batsmanbowler.Split('_')[0];
                string bowler = batsmanbowler.Split('_')[1];
                if (innings1BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=BATBOWL(" + batsman + "," + bowler + ")\tTotal Runs=" + batsmanbowler2Runs[batsmanbowler] + "\tSixes=" + batsmanbowler2Sixes[batsmanbowler] + "\tFours=" + batsmanbowler2Fours[batsmanbowler] + "\tListOfBalls=" + print(1,batsmanbowler2Balls[batsmanbowler]));
                else if (innings2BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=BATBOWL(" + batsman + "," + bowler + ")\tTotal Runs=" + batsmanbowler2Runs[batsmanbowler] + "\tSixes=" + batsmanbowler2Sixes[batsmanbowler] + "\tFours=" + batsmanbowler2Fours[batsmanbowler] + "\tListOfBalls=" + print(2,batsmanbowler2Balls[batsmanbowler]));
                entityID++;
            }
        }


        private static void generateBOWLEntities(int match)
        {
            //  generate BOWL entity: entityID, MatchNumber, InningsNum, EntityName=BOWL(Player name), Total Runs, #Maiden overs, #Wickets, List of Ball Numbers.
            Dictionary<string, int> bowler2Maidens = new Dictionary<string, int>();
            Dictionary<string, string> bowler2Overs = new Dictionary<string, string>();
            Dictionary<string, int> bowler2Wickets = new Dictionary<string, int>();
            Dictionary<string, int> bowler2Runs = new Dictionary<string, int>();
            Dictionary<string, string> bowler2Balls = new Dictionary<string, string>();

            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string bowler = toks[9];
                string eventType = toks[18];
                string stats = toks[17];
                int runs = 0;
                int maidens = 0;
                int wickets = 0;
                string overs = "";
                if (!stats.Equals(""))
                {
                    overs = stats.Split('-')[0];
                    maidens = int.Parse(stats.Split('-')[1]);
                    runs = int.Parse(stats.Split('-')[2]);
                    wickets = int.Parse(stats.Split('-')[3]);
                }
                if (bowler2Balls.ContainsKey(bowler))
                    bowler2Balls[bowler] += toks[2] + ",";
                else
                    bowler2Balls[bowler] = toks[2] + ",";
                bowler2Maidens[bowler] = maidens;
                bowler2Overs[bowler] = overs;
                bowler2Wickets[bowler] = wickets;
                bowler2Runs[bowler] = runs;
            }
            foreach (string bowler in bowler2Balls.Keys)
            {
                if (innings2BatsmenNames.Contains(bowler))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=BOWL(" + bowler + ")\tTotal Runs=" + bowler2Runs[bowler] + 
                    "\tMaidens=" + bowler2Maidens[bowler] + "\tOvers=" + bowler2Overs[bowler] + "\tWickets=" + bowler2Wickets[bowler] + 
                    "\tListOfBalls=" + print(1,bowler2Balls[bowler]));
                else if(innings1BatsmenNames.Contains(bowler))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=BOWL(" + bowler + ")\tTotal Runs=" + bowler2Runs[bowler] +
                    "\tMaidens=" + bowler2Maidens[bowler] + "\tOvers=" + bowler2Overs[bowler] + "\tWickets=" + bowler2Wickets[bowler] +
                    "\tListOfBalls=" + print(2,bowler2Balls[bowler]));
                entityID++;
            }
        }

        private static void generateBATEntities(int match)
        {
            Dictionary<string, string> batsman2Balls = new Dictionary<string, string>();
            Dictionary<string, int> batsman2Sixes = new Dictionary<string, int>();
            Dictionary<string, int> batsman2Fours = new Dictionary<string, int>();
            Dictionary<string, int> batsman2Runs = new Dictionary<string, int>();

            //generate BAT entity: entityID, MatchNumber, InningsNum, EntityName=BAT(Player name), Total Runs, #Sixes, #Fours, List of Ball Numbers.
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                string batsman = toks[6];
                string eventType = toks[18];
                string stats = toks[15];
                int runs = int.Parse(stats.Split(' ')[0].Replace("*", ""));
                int sixes = 0;
                int fours = 0;
                if (!stats.Equals("") && stats.Contains("x4"))
                {
                    string[] toks2 = stats.Split(' ');
                    foreach (string t in toks2)
                        if (t.Contains("x4"))
                            fours = int.Parse(t.Split('x')[0]);
                }
                if (!stats.Equals("") && stats.Contains("x6"))
                {
                    string[] toks2 = stats.Split(' ');
                    foreach (string t in toks2)
                        if (t.Contains("x6"))
                            sixes = int.Parse(t.Split('x')[0]);
                }
                if(batsman2Balls.ContainsKey(batsman))
                    batsman2Balls[batsman] += toks[2] + ",";
                else
                    batsman2Balls[batsman] = toks[2] + ",";
                batsman2Sixes[batsman] = sixes;
                batsman2Fours[batsman] = fours;
                batsman2Runs[batsman] = runs;
            }
            foreach(string batsman in batsman2Balls.Keys)
            {
                if (innings1BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=BAT(" + batsman + ")\tTotal Runs=" + batsman2Runs[batsman] + "\tSixes=" + batsman2Sixes[batsman] + "\tFours="+batsman2Fours[batsman]+"\tListOfBalls=" + print(1,batsman2Balls[batsman]));
                else if (innings2BatsmenNames.Contains(batsman))
                    entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=BAT(" + batsman + ")\tTotal Runs=" + batsman2Runs[batsman] + "\tSixes=" + batsman2Sixes[batsman] + "\tFours=" + batsman2Fours[batsman] + "\tListOfBalls=" + print(2,batsman2Balls[batsman]));
                entityID++;
            }
        }

        private static void getDotballsEntities(int match)
        {
            //generate DOT BALLS entity: entityID, MatchNumber, InningsNum, EntityName=Dotballs, List of Ball Numbers.
            string listOfBallsInnings1 = "";
            string listOfBallsInnings2 = "";
            int innings1 = 0;
            int innings2 = 0;
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (toks[8].Equals("0"))
                {
                    if (toks[1].Equals("1"))
                    {
                        listOfBallsInnings1 += toks[2] + ",";
                        innings1++;
                    }
                    else
                    {
                        listOfBallsInnings2 += toks[2] + ",";
                        innings2++;
                    }
                }
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=DOTBALLS\tDot Balls=" + innings1 + "\tListOfBalls=" + print(1,listOfBallsInnings1));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=DOTBALLS\tDot Balls=" + innings2 + "\tListOfBalls=" + print(2, listOfBallsInnings2));
            entityID++;
        }

        private static void getFreehitEntities(int match)
        {
            //generate FREEHIT entity: entityID, MatchNumber, InningsNum, EntityName=Freehit, List of Ball Numbers.
            string listOfBallsInnings1 = "";
            string listOfBallsInnings2 = "";
            int innings1 = 0;
            int innings2 = 0;
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (toks[20].Equals("Free Hit"))
                {
                    if (toks[1].Equals("1"))
                    {
                        listOfBallsInnings1 += toks[2] + ",";
                        innings1++;
                    }
                    else
                    {
                        listOfBallsInnings2 += toks[2] + ",";
                        innings2++;
                    }
                }
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=FREEHIT\tFree Hits=" + innings1 + "\tListOfBalls=" + print(1,listOfBallsInnings1));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=FREEHIT\tFree Hits=" + innings2 + "\tListOfBalls=" + print(2, listOfBallsInnings2));
            entityID++;
        }

        private static void getNoballEntities(int match)
        {
            //generate NOBALL entity: entityID, MatchNumber, InningsNum, EntityName=Noball, List of Ball Numbers.
            string listOfBallsInnings1 = "";
            string listOfBallsInnings2 = "";
            int innings1 = 0;
            int innings2 = 0;
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (toks[18].Equals("no ball"))
                {
                    if (toks[1].Equals("1"))
                    {
                        listOfBallsInnings1 += toks[2] + ",";
                        innings1++;
                    }
                    else
                    {
                        listOfBallsInnings2 += toks[2] + ",";
                        innings2++;
                    }
                }
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=NOBALL\tNo balls=" + innings1 + "\tListOfBalls=" + print(1,listOfBallsInnings1));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=NOBALL\tNo balls=" + innings2 + "\tListOfBalls=" + print(2, listOfBallsInnings2));
            entityID++;
        }

        private static void getLastoverEntities(int match)
        {
            //generate LASTOVER entity: entityID, MatchNumber, InningsNum, EntityName=Lastover, List of Ball Numbers.
            string listOfBallsInnings1 = "";
            string listOfBallsInnings2 = "";
            int lastOver1 = 0;
            int lastOver2 = 0;
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (toks[1].Equals("1"))
                    lastOver1 = int.Parse(toks[2].Split('.')[0]);
                if (toks[1].Equals("2"))
                    lastOver2 = int.Parse(toks[2].Split('.')[0]);
            }
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (toks[1].Equals("1") && int.Parse(toks[2].Split('.')[0])==lastOver1)
                    listOfBallsInnings1 += toks[2] + ",";
                if (toks[1].Equals("2") && int.Parse(toks[2].Split('.')[0]) == lastOver2)
                    listOfBallsInnings2 += toks[2] + ",";
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=LASTOVER(INNINGS1)\tListOfBalls=" + print(1,listOfBallsInnings1));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=LASTOVER(INNINGS2)\tListOfBalls=" + print(2,listOfBallsInnings2));
            entityID++;
        }


        private static void getTwosEntities(int match)
        {
            //generate TWOS entity: entityID, MatchNumber, InningsNum, EntityName=Twos, List of Ball Numbers.
            string listOfBallsInnings1 = "";
            string listOfBallsInnings2 = "";
            int innings1 = 0;
            int innings2 = 0;
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (toks[8].Equals("2"))
                {
                    if (toks[1].Equals("1"))
                    {
                        listOfBallsInnings1 += toks[2] + ",";
                        innings1++;
                    }
                    else
                    {
                        listOfBallsInnings2 += toks[2] + ",";
                        innings2++;
                    }
                }
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=TWOS\tTwos=" + innings1 + "\tListOfBalls=" + print(1,listOfBallsInnings1));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=TWOS\tTwos=" + innings2 + "\tListOfBalls=" + print(2, listOfBallsInnings2));
            entityID++;
        }

        private static void getSinglesEntities(int match)
        {
            //generate SINGLES entity: entityID, MatchNumber, InningsNum, EntityName=Singles, List of Ball Numbers.
            string listOfBallsInnings1 = "";
            string listOfBallsInnings2 = "";
            int innings1 = 0;
            int innings2 = 0;
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (toks[8].Equals("1"))
                {
                    if (toks[1].Equals("1"))
                    {
                        listOfBallsInnings1 += toks[2] + ",";
                        innings1++;
                    }
                    else
                    {
                        listOfBallsInnings2 += toks[2] + ",";
                        innings2++;
                    }
                }
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=SINGLES\tSingles=" + innings1 + "\tListOfBalls=" + print(1,listOfBallsInnings1));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=SINGLES\tSingles=" + innings2 + "\tListOfBalls=" + print(2, listOfBallsInnings2));
            entityID++;
        }

        private static void getWideEntities(int match)
        {
            //generate WIDES entity: entityID, MatchNumber, InningsNum, EntityName=Wides, List of Ball Numbers.
            string listOfBallsInnings1 = "";
            string listOfBallsInnings2 = "";
            int innings1Wides = 0;
            int innings2Wides = 0;
            foreach (string ball in commentaryForMatch[match - 1])
            {
                string[] toks = ball.Split('\t');
                if (toks[18].Equals("wide"))
                {
                    if (toks[1].Equals("1"))
                    {
                        listOfBallsInnings1 += toks[2] + ",";
                        innings1Wides++;
                    }
                    else
                    {
                        listOfBallsInnings2 += toks[2] + ",";
                        innings2Wides++;
                    }
                }
            }
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=1\tEntityName=WIDES\tWides=" + innings1Wides + "\tListOfBalls=" + print(1,listOfBallsInnings1));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=WIDES\tWides=" + innings2Wides + "\tListOfBalls=" + print(2,listOfBallsInnings2));
            entityID++;
        }

        private static void getExtraEntities(int match)
        {
            //generate EXTRAS entity: entityID, MatchNumber, InningsNum, EntityName=Extras, distribution of extras, List of Ball Numbers.
            string listOfBallsInnings1 = "";
            string listOfBallsInnings2 = "";
            int innings1Wides=0;
            int innings1NoBalls=0;
            int innings1Byes=0;
            int innings1LegByes=0;
            int innings2Wides=0;
            int innings2NoBalls=0;
            int innings2Byes=0;
            int innings2LegByes=0;
            foreach(string ball in commentaryForMatch[match-1])
            {
                string[] toks = ball.Split('\t');
                if(toks[18].Equals("leg bye")||toks[18].Equals("bye")||toks[18].Equals("wide")||toks[18].Equals("no ball"))
                {
                    if (toks[1].Equals("1"))
                    {
                        listOfBallsInnings1 += toks[2] + ",";
                        if (toks[18].Equals("leg bye")) { innings1LegByes++; }
                        if (toks[18].Equals("bye")) { innings1Byes++; }
                        if (toks[18].Equals("wide")) { innings1Wides++; }
                        if (toks[18].Equals("no ball")) { innings1NoBalls++; }
                    }
                    else
                    {
                        listOfBallsInnings2 += toks[2] + ",";
                        if (toks[18].Equals("leg bye")) { innings2LegByes++; }
                        if (toks[18].Equals("bye")) { innings2Byes++; }
                        if (toks[18].Equals("wide")) { innings2Wides++; }
                        if (toks[18].Equals("no ball")) { innings2NoBalls++; }
                    }
                }
            }
            entities.Add(entityID+"\tMatch="+match+"\tInnings=1\tEntityName=EXTRAS\tWides="+innings1Wides+" NoBalls="+innings1NoBalls+" Byes="+innings1Byes+" LegByes="+innings1LegByes+"\tListOfBalls="+print(1,listOfBallsInnings1));
            entityID++;
            entities.Add(entityID + "\tMatch=" + match + "\tInnings=2\tEntityName=EXTRAS\tWides=" + innings2Wides + " NoBalls=" + innings2NoBalls + " Byes=" + innings2Byes + " LegByes=" + innings2LegByes + "\tListOfBalls=" + print(2, listOfBallsInnings2));
            entityID++;
        }

        private static void getPowerplayEntities(int match)
        {
            //read scorecard file and generate powerplay entities: entityID, MatchNumber, InningsNum, EntityName=Powerplay, PowerplayNumber, Powerplay Highlights, List of Ball Numbers.
            StreamReader sr = new StreamReader(dir+"match"+match+"Scorecard.html");
            string str = "";
            int innings=0;
            int powerplay=1;
            while((str=sr.ReadLine())!=null)
            {
                if(str.Contains("<li><b>") && str.Contains("innings"))
                {
                    innings++;
                    if(innings==2)
                        powerplay=1;
                }
                if(str.Contains("<li>Powerplay"))
                {
                    string tmp = entityID + "\tMatch=" + match + "\tInnings=" + innings + "\tEntityName=POWERPLAY(INNINGS"+innings+", "+powerplay+")\tPowerplay Highlights=" + str.Split('(')[1].Split(')')[0] + "\tBalls=";
                    string from = innings+":"+str.Trim().Split(' ')[3] + ".0";
                    string to = innings+":"+str.Trim().Split(' ')[5].Split('.')[0] + ".1.0";
                    List<string> commentaryForThisMatch = commentaryForMatch[match-1];
                    int start = 0;
                    string range="";
                    foreach(string b in commentaryForThisMatch)
                    {
                        string ballNum = b.Split('\t')[1] + ":" + b.Split('\t')[2];
                        if (ballNum.Equals(from))
                            start = 1;
                        if (ballNum.Equals(to))
                            break;
                        if (start == 1)
                            range += ballNum + ",";
                    }
                    tmp += range;
                    entityID++;
                    powerplay++;
                    entities.Add(tmp);
                }
            }
            sr.Close();
        }

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
    }
}