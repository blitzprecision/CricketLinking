using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CricketLinking
{
    class ComputeSingleBallAccuracySeqProx
    {
        static string dir = Global.baseDir;
        static Dictionary<string, string> idealBalls = new Dictionary<string, string>();
        static Dictionary<string, Dictionary<string, string>> subClass2IdealBalls = new Dictionary<string, Dictionary<string, string>>();
        static Dictionary<string, List<string>> predictedBalls = new Dictionary<string, List<string>>();
        static Dictionary<string, List<string>> backFillBalls = new Dictionary<string, List<string>>();
        static void Main(string[] args)
        {
            string[] data2 = File.ReadAllLines(dir + "SB_StructuredSim_TFIDF_NoCoreference_0CommContext_mentionOnly_mention2Balls.txt");
            foreach (string s in data2)
            {
                string[] toks = s.Split('\t');
                List<string> l = new List<string>();
                for (int i = 1; i <= 10; i++)
                    l.Add(toks[2 * i - 1]);
                backFillBalls[toks[0]] = l;
            }
            StreamWriter sw = new StreamWriter(dir + "SB_SeqProxAcc.txt");
            loadIdealBalls();
            string[] methods = { "minDiff", "minRankDiff", "minScoreReciprocalDiff" };
            for (int topk = 1; topk <= 10; topk++)
            {
                foreach (string subclass in subClass2IdealBalls.Keys)
                {
                    foreach (string method in methods)
                    {
                        string filename = dir + "SB_SeqProx_" + method + "_"+topk+".txt";
                        bool success = false;
                        try
                        {
                            string[] data = File.ReadAllLines(filename);
                            success = true;
                        }
                        catch { success = false; }
                        if (!success)
                            continue;
                        sw.Write(subclass + "\t"+method + "\t"+topk+"\t");
                        loadPredictedBalls(filename);
                        double[] precision = new double[10];
                        foreach (string s in subClass2IdealBalls[subclass].Keys)
                        {
                            List<string> l = new List<string>();
                            if (predictedBalls.ContainsKey(s))
                                l = predictedBalls[s];
                            else
                                l = backFillBalls[s];
                            for (int i = 0; i < l.Count(); i++)
                            {
                                if (l[i].Equals(idealBalls[s]))
                                {
                                    for (int j = i; j < l.Count(); j++)
                                        precision[j] += 1;
                                    break;
                                }
                            }
                        }
                        foreach (double d in precision)
                            sw.Write(d / predictedBalls.Count() + "\t");
                        sw.WriteLine();
                    }
                }
            }
            sw.Close();
        }

        private static void loadPredictedBalls(string fileName)
        {
            predictedBalls = new Dictionary<string, List<string>>();
            string[] data = File.ReadAllLines(fileName);
            foreach (string s in data)
            {
                string[] toks = s.Split(new char[]{'\t'}, StringSplitOptions.RemoveEmptyEntries);
                List<string> l = new List<string>();
                List<string> l2 = backFillBalls[toks[0]];
                int start = -1;
                for (int i = 1; i <= 10; i++)
                {
                    if (2 * i - 1 < toks.Length)
                        l.Add(toks[2 * i - 1]);
                    else
                    {
                        if (start == -1)
                            start = i;
                        int j = i-start;
                        while (l.Contains(l2[j]))
                        {
                            j++;
                            start--;
                        }
                        l.Add(l2[j]);
                    }
                }
                predictedBalls[toks[0]] = l;
            }
        }

        private static void loadIdealBalls()
        {
            string[] data = File.ReadAllLines(dir + "goldenLabels.txt");
            foreach (string s in Global.singleClasses)
                subClass2IdealBalls[s] = new Dictionary<string, string>();
            subClass2IdealBalls["all"] = new Dictionary<string, string>();
            foreach (string s in data)
            {
                string[] toks = s.Split('\t');
                if (toks[3].Contains(","))
                    continue;
                string instance = toks[0] + "_" + toks[1] + "_" + toks[2].Replace("m", "");
                idealBalls[instance] = toks[3];
            }
            string[] dataLabels = File.ReadAllLines(dir + "linkedLabels.tsv");
            foreach (string s in dataLabels)
            {
                string[] toks = s.Split('\t');
                string subclass = toks[10];
                if (toks[8].Equals("S"))
                {
                    string instance = toks[0] + "_" + toks[1] + "_" + toks[2].Replace("m", "");
                    subClass2IdealBalls[subclass][instance] = idealBalls[instance];
                    subClass2IdealBalls["all"][instance] = idealBalls[instance];
                }
            }
        }
    }
}
