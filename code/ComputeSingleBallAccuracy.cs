using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace CricketLinking
{
    class ComputeSingleBallAccuracy
    {
        static string dir = Global.baseDir;
        static Dictionary<string, string> idealBalls = new Dictionary<string, string>();
        static Dictionary<string, Dictionary<string, string>> subClass2IdealBalls = new Dictionary<string, Dictionary<string, string>>();
        static Dictionary<string, List<string>> predictedBalls = new Dictionary<string, List<string>>();
        static void Main(string[] args)
        {
            StreamWriter sw = new StreamWriter(dir + "SB_DirStructuredSim.txt");
            loadIdealBalls();
            string []corefs = new string[] {"Coreference","NoCoreference"};//0,1
            string[] sims = new string[] {"Jaccard", "TFIDF"};//Jaccard/TFIDF
            string[] methods = new string[]{"DirSim", "StructuredSim"};//DirSim/StructuredSim
            string[] commentaryContexts = { "0CommContext", "1CommContext", "2CommContext", "overCommContext" };
            string[] mentionContexts = { "mentionSentence", "mentionOnly"};//
            foreach (string subclass in subClass2IdealBalls.Keys)
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
                                    sw.Write(subclass+"\t"+commentaryContext + "\t" + mc + "\t" + coref + "\t" + sim + "\t" + method + "\t");
                                    string filename = dir + "SB_" + method + "_" + sim + "_" + coref + "_" + commentaryContext + "_" + mc + "_mention2Balls.txt";
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
                                    double[] precision = new double[10];
                                    foreach (string s in subClass2IdealBalls[subclass].Keys)
                                    {
                                        List<string> l = predictedBalls[s];
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
                                    //sw.Write("\t#Mentions:\t" + predictedBalls.Count());
                                    sw.WriteLine();
                                }
                            }
                        }
                    }
                }
            }
            sw.Close();
        }

        private static void loadPredictedBalls(string fileName)
        {
            predictedBalls = new Dictionary<string, List<string>>();
            string[] data = File.ReadAllLines(fileName);
            foreach(string s in data)
            {
                string[] toks = s.Split('\t');
                List<string> l = new List<string>();
                for (int i = 1; i <= 10; i++)
                    l.Add(toks[2*i-1]);
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
            foreach(string s in dataLabels)
            {
                string[] toks = s.Split('\t');
                string subclass = toks[10];
                if(toks[8].Equals("S"))
                {
                    string instance=toks[0] + "_" + toks[1] + "_" + toks[2].Replace("m", "");
                    subClass2IdealBalls[subclass][instance]=idealBalls[instance];
                    subClass2IdealBalls["all"][instance] = idealBalls[instance];
                }
            }
        }
    }
}
