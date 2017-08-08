﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CricketLinking
{
    class ComputeMultiBallAccuracySubClassesSemantics
    {
        static string dir = Global.baseDir;
        static Dictionary<string, List<string>> idealBalls = new Dictionary<string, List<string>>();
        static Dictionary<string, Dictionary<string, List<string>>> subClass2IdealBalls = new Dictionary<string, Dictionary<string, List<string>>>();
        static Dictionary<string, List<string>> predictedBalls = new Dictionary<string, List<string>>();
        static Dictionary<string, List<string>> backFillBalls = new Dictionary<string, List<string>>();
        static void Main(string[] args)
        {
            string[] data2 = File.ReadAllLines(dir + "MB_StructuredSim_Jaccard_Coreference_overCommContext_mentionOnly_knee_mention2Balls.txt");
            foreach (string s in data2)
            {
                string[] toks = s.Split('\t');
                List<string> l = new List<string>();
                for (int i = 1; i <= toks.Length / 2; i++)
                    l.Add(toks[2 * i - 1]);
                backFillBalls[toks[0]] = l;
            }

            StreamWriter sw = new StreamWriter(dir + "MB_SubClassesSemantics.txt");
            loadIdealBalls();
            string[] corefs = new string[] { "Coreference", "NoCoreference" };//0,1
            string[] methods = new string[] { "SubClassSemantics"};//, "SubClassSemanticsIterator" };//DirSim/StructuredSim
            string[] mentionContexts = { "mentionSentence", "mentionOnly" };//
            string[] types = { "typeIgnorant", "hardType", "softType_NoBallFilter", "softType_BallFilter" };
            //string[] types = { "hardType", "softType"};
            foreach (string subclass in subClass2IdealBalls.Keys)
            {
                foreach (string mc in mentionContexts)
                {
                    foreach (string coref in corefs)
                    {
                        foreach (string method in methods)
                        {
                            foreach (string type in types)
                            {
                                string filename = dir + "MB_" + method + "_" +coref + "_" + mc + "_"+type+"_mention2Balls.txt";
                                bool success = false;
                                try
                                {
                                    string[] data = File.ReadAllLines(filename);
                                    success = true;
                                }
                                catch { success = false; }
                                if (!success)
                                    continue;
                                sw.Write(subclass + "\t" + mc + "\t" + coref + "\t" + type + "\t" + method + "\t");
                                loadPredictedBalls(filename);
                                int count = subClass2IdealBalls[subclass].Count();
                                double overallPrec = 0;
                                double overallRec = 0;
                                foreach (string s in subClass2IdealBalls[subclass].Keys)
                                {
                                    List<string> p = predictedBalls[s];
                                    if (p.Count() == 0)
                                        p = backFillBalls[s];
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
                string[] toks1 = toks[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                List<string> l = new List<string>();
                foreach(string t in toks1)
                    l.Add(t);
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
