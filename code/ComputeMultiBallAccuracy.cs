using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CricketLinking
{
    class ComputeMultiBallAccuracy
    {
        static string dir = Global.baseDir;
        static Dictionary<string, List<string>> idealBalls = new Dictionary<string, List<string>>();
        static Dictionary<string, Dictionary<string, List<string>>> subClass2IdealBalls = new Dictionary<string, Dictionary<string, List<string>>>();
        static Dictionary<string, List<string>> predictedBalls = new Dictionary<string, List<string>>();
        static void Main(string[] args)
        {
            StreamWriter sw = new StreamWriter(dir + "MB_DirStructuredSim.txt");
            loadIdealBalls();
            string[] corefs = new string[] { "Coreference", "NoCoreference" };//0,1
            string[] sims = new string[] { "Jaccard", "TFIDF" };//Jaccard/TFIDF
            string[] methods = new string[] { "DirSim", "StructuredSim" };//DirSim/StructuredSim
            string[] commentaryContexts = { "0CommContext", "1CommContext", "2CommContext", "overCommContext" };
            string[] mentionContexts = { "mentionSentence", "mentionOnly" };//
            string[] multiBallChoiceAlgos = { "maxSubArrayAvg_4.0", "maxSubArrayAvg_3.0", "maxSubArrayAvg_2.0", "maxSubArrayAvg_1.5", "knee" };
            foreach (string subclass in subClass2IdealBalls.Keys)
            {
                foreach (string multiBallChoiceAlgo in multiBallChoiceAlgos)
                {
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
                                        sw.Write(subclass + "\t" + commentaryContext + "\t" + mc + "\t" + coref + "\t" + sim + "\t" + method + "\t"+ multiBallChoiceAlgo + "\t");
                                        string filename = dir + "MB_" + method + "_" + sim + "_" + coref + "_" + commentaryContext + "_" + mc + "_" + multiBallChoiceAlgo + "_mention2Balls.txt";
                                        bool success = false;
                                        try
                                        {
                                            string[] data = File.ReadAllLines(filename);
                                            success = true;
                                        }
                                        catch { success = false; }
                                        if (!success)
                                            continue;
                                        loadPredictedBalls(filename);
                                        int count = subClass2IdealBalls[subclass].Count();
                                        double overallPrec = 0;
                                        double overallRec = 0;
                                        foreach (string s in subClass2IdealBalls[subclass].Keys)
                                        {
                                            List<string> p = predictedBalls[s];
                                            List<string> i = idealBalls[s];
                                            int intersection = intersect(p, i);
                                            double precision = (double)intersection / (double)p.Count();
                                            double recall = (double)intersection / (double)i.Count();
                                            overallPrec += precision;
                                            overallRec += recall;
                                        }
                                        sw.Write((overallPrec / count) + "\t" + (overallRec / count) + "\t");
                                        sw.WriteLine();
                                    }
                                }
                            }
                        }
                    }
                }
            }
            sw.Close();
        }

        private static int intersect(List<string> p, List<string> i)
        {
            int count = 0;
            foreach (string s in p)
                if (i.Contains(s))
                    count++;
            return count;
        }

        private static void loadPredictedBalls(string fileName)
        {
            predictedBalls = new Dictionary<string, List<string>>();
            string[] data = File.ReadAllLines(fileName);
            foreach (string s in data)
            {
                string[] toks = s.Split('\t');
                List<string> l = new List<string>();
                for (int i = 1; i <= toks.Length/2; i++)
                    l.Add(toks[2 * i - 1]);
                predictedBalls[toks[0]] = l;
            }
        }

        private static void loadIdealBalls()
        {
            string[] data = File.ReadAllLines(dir + "goldenLabels.txt");
            foreach (string s in Global.multipleClasses)
                subClass2IdealBalls[s] = new Dictionary<string, List<string>>();
            subClass2IdealBalls["all"] = new Dictionary<string, List<string>>();
            foreach (string s in data)
            {
                string[] toks = s.Split('\t');
                if (!toks[3].Contains(","))
                    continue;
                string instance = toks[0] + "_" + toks[1] + "_" + toks[2].Replace("m", "");
                idealBalls[instance] = new List<string>(toks[3].Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));
            }
            string[] dataLabels = File.ReadAllLines(dir + "linkedLabels.tsv");
            foreach (string s in dataLabels)
            {
                string[] toks = s.Split('\t');
                string subclass = toks[10];
                if (toks[8].Equals("M"))
                {
                    string instance = toks[0] + "_" + toks[1] + "_" + toks[2].Replace("m", "");
                    subClass2IdealBalls[subclass][instance] = idealBalls[instance];
                    subClass2IdealBalls["all"][instance] = idealBalls[instance];
                }
            }
        }
    }
}
