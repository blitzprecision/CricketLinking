using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Average number of balls for multi-ball mentions: 68.3046566692976
//Total number of contiguous multi-ball mentions: 471
//Average contiguity for multi-ball mentions: 0.579740637807692

namespace CricketLinking
{
    class CheckContiguity
    {
        static string dir = Global.baseDir;
        static Dictionary<string, List<string>> idealBalls = new Dictionary<string, List<string>>();
        static Dictionary<string, Dictionary<string, List<string>>> subClass2IdealBalls = new Dictionary<string, Dictionary<string, List<string>>>();
        public static Dictionary<int, List<string>> matchNum2Commentary = new Dictionary<int, List<string>>();
        public static void loadCommentary()
        {
            StreamReader sr = new StreamReader(dir + "matchCommentary.txt");
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                int match = int.Parse(str.Split('\t')[0]);
                if (match > 30)
                    continue;
                if (!matchNum2Commentary.ContainsKey(match))
                    matchNum2Commentary[match] = new List<string>();
                string[] toks = str.Split('\t');
                string tmp = toks[1]+":"+toks[2];
                matchNum2Commentary[match].Add(tmp.Trim());
            }
            sr.Close();
        }

        private static void loadIdealBalls()
        {
            string[] data = File.ReadAllLines(dir + "goldenLabels.txt");
            foreach (string s in data)
            {
                string[] toks = s.Split('\t');
                if (!toks[3].Contains(","))
                    continue;
                string instance = toks[0] + "_" + toks[1] + "_" + toks[2].Replace("m", "");
                idealBalls[instance] = new List<string>(toks[3].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
            }
        }

        static void Main(string[] args)
        {
            loadIdealBalls();
            loadCommentary();
            double numBalls = 0;
            int contiguousMentions = 0;
            double contiguity = 0;
            foreach(string instance in idealBalls.Keys)
            {
                List<string> balls = idealBalls[instance];
                List<ball> l = new List<ball>();
                foreach (string b in balls)
                    l.Add(new ball(b));
                l.Sort(delegate(ball b1, ball b2) { return (b1.innings * 1e5 + b1.over * 1000 + b1.ballNum * 10 + b1.repeat).CompareTo(b2.innings * 1e5 + b2.over * 1000 + b2.ballNum * 10 + b2.repeat); });
                numBalls += l.Count();
                int match = int.Parse(instance.Split('_')[0]);
                List<string> allBalls = matchNum2Commentary[match];
                string first = l[0].getStr();
                string last = l[l.Count() - 1].getStr();
                int startIndex = 0;
                int endIndex=0;
                for(int i=0;i<allBalls.Count();i++)
                {
                    if (allBalls[i].Equals(first))
                        startIndex = i;
                    if (allBalls[i].Equals(last))
                        endIndex = i;
                }
                int missed = (endIndex - startIndex + 1) - balls.Count();
                if (missed == 0)
                    contiguousMentions++;
                contiguity += (double)balls.Count() / (double)(endIndex - startIndex + 1);
            }
            Console.WriteLine("Average number of balls for multi-ball mentions: "+(numBalls/idealBalls.Count()));
            Console.WriteLine("Total number of contiguous multi-ball mentions: " + contiguousMentions);
            Console.WriteLine("Average contiguity for multi-ball mentions: " + contiguity / idealBalls.Count());
        }
        class ball
        {
            public int innings;
            public int over;
            public int ballNum;
            public int repeat;
            public string getStr()
            {
                return innings + ":" + over + "." + ballNum + "." + repeat;
            }
            public ball(string b)
            {
                innings = int.Parse(b.Split(':')[0]);
                over = int.Parse(b.Split(':')[1].Split('.')[0]);
                ballNum = int.Parse(b.Split(':')[1].Split('.')[1]);
                repeat = int.Parse(b.Split(':')[1].Split('.')[2]);
                over = int.Parse(b.Split(':')[1].Split('.')[0]);
            }
        }
    }
}
