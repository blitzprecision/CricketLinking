using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CricketLinking
{
    class SequentialProximity
    {
        public static Dictionary<string, Dictionary<string, string>> instance2Candidates2CentralBall = new Dictionary<string, Dictionary<string, string>>();
        public static Dictionary<string, int> instance2Paras = new Dictionary<string, int>();
        public static Dictionary<string, List<string>> para2InstanceList = new Dictionary<string, List<string>>();
        public static Dictionary<string, Dictionary<string, double>> instance2Candidates2Score = new Dictionary<string, Dictionary<string, double>>();
        public static Dictionary<string, List<string>> instance2CandidateList = new Dictionary<string, List<string>>();
        public static string baseDir = Global.baseDir;
        public static string[] methods = { "minDiff", "minRankDiff", "minScoreReciprocalDiff" };
        public static string bestSingleFile = baseDir+"SB_SubClassSemantics_TFIDF_Coreference_mentionOnly_hardType_0.1_mention2Balls.txt";
        public static string bestMultiFile = baseDir + "MB_SubClassSemanticsIterator_Coreference_mentionOnly_hardType_mention2Balls.txt";
        public static Dictionary<string, string> instance2Label = new Dictionary<string, string>();
        public static StreamWriter swSB = new StreamWriter(baseDir + "SB_SeqProx.txt");
        public static StreamWriter swMB = new StreamWriter(baseDir + "MB_SeqProx.txt");


        public static int getIntForBall(string ball)
        {
            int over = int.Parse(ball.Split(':')[1].Split('.')[0]);
            int ballNum = int.Parse(ball.Split(':')[1].Split('.')[1]);
            return over * 6 + ballNum;
        }
        static void Main(string[] args)
        {
            loadInstance2Paras();
            loadInstance2Candidates2Score();
            for (int topk = 1; topk <= 10; topk++)
            {
                foreach (string method in methods)
                {
                    swSB = new StreamWriter(baseDir + "SB_SeqProx_" + method +"_"+topk+ ".txt");
                    swMB = new StreamWriter(baseDir + "MB_SeqProx_" + method + "_" + topk + ".txt");
                    foreach (string para in para2InstanceList.Keys)
                    {
                        List<string> instances = para2InstanceList[para];

                        if (instances.Count() < 2)
                        {
                            writeOut(instances);
                            continue;
                        }
                        int breakout = 0;
                        foreach (string instance in instances)
                            if (instance2CandidateList[instance].Count() == 0)
                            {
                                breakout = 1;
                                break;
                            }
                        if (breakout == 1)
                        {
                            writeOut(instances);
                            continue;
                        }
                        string[][] list = new string[topk][];
                        int[][] mat = new int[topk][];
                        double[][] dynprog = new double[topk][];
                        for (int i = 0; i < topk; i++)
                        {
                            dynprog[i] = new double[instances.Count()];
                            list[i] = new string[instances.Count()];
                            mat[i] = new int[instances.Count()];
                        }
                        for (int i = 0; i < instances.Count(); i++)
                        {
                            for (int j = 0; j < topk; j++)
                            {
                                if (j >= instance2Candidates2CentralBall[instances[i]].Count())
                                    mat[j][i] = 1000000;//mat[j - 1][i];
                                else
                                    mat[j][i] = getIntForBall(instance2Candidates2CentralBall[instances[i]][instance2CandidateList[instances[i]][j]]);
                            }
                        }
                        //run dynamic programming algorithm
                        for (int i = 0; i < instances.Count(); i++)
                        {
                            for (int j = 0; j < topk; j++)
                            {
                                if (i == 0)
                                {
                                    dynprog[j][i] = 0;
                                    if (method.Equals("minRankDiff"))
                                        dynprog[j][i] = j + 1;
                                    if (method.Equals("minScoreReciprocalDiff"))
                                    {
                                        if (j > instance2CandidateList[instances[i]].Count() - 1)
                                            dynprog[j][i] = 1.0/(1+instance2Candidates2Score[instances[i]][instance2CandidateList[instances[i]][instance2CandidateList[instances[i]].Count() - 1]]);
                                        else
                                            dynprog[j][i] = 1.0/(1+instance2Candidates2Score[instances[i]][instance2CandidateList[instances[i]][j]]);
                                    }
                                    list[j][i] = j + " ";
                                    continue;
                                }
                                double min = int.MaxValue;
                                int minIndex = -1;
                                for (int k = 0; k < topk; k++)
                                {
                                    double val = dynprog[k][i - 1] + (Math.Abs(mat[j][i] - mat[k][i - 1]));
                                    if (method.Equals("minRankDiff"))
                                        val = dynprog[k][i - 1] + (j + 1) * (Math.Abs(mat[j][i] - mat[k][i - 1]));
                                    if (method.Equals("minScoreReciprocalDiff"))
                                    {
                                        double tmp = 0;
                                        if (j > instance2CandidateList[instances[i]].Count() - 1)
                                            tmp = 1.0/(1+instance2Candidates2Score[instances[i]][instance2CandidateList[instances[i]][instance2CandidateList[instances[i]].Count() - 1]]);
                                        else
                                            tmp = 1.0/(1+instance2Candidates2Score[instances[i]][instance2CandidateList[instances[i]][j]]);
                                        val = dynprog[k][i - 1] + tmp * (Math.Abs(mat[j][i] - mat[k][i - 1]));
                                    }
                                    if (val < min)
                                    {
                                        min = val;
                                        minIndex = k;
                                    }
                                }
                                dynprog[j][i] = min;
                                list[j][i] = list[minIndex][i - 1] + " " + j;
                            }
                        }
                        double finalMin = double.MaxValue;
                        string finalList = "";
                        for (int i = 0; i < topk; i++)
                        {
                            if (dynprog[i][instances.Count() - 1] < finalMin)
                            {
                                finalMin = dynprog[i][instances.Count() - 1];
                                finalList = list[i][instances.Count() - 1];
                            }
                        }
                        //Console.WriteLine(finalList + " Min:" + finalMin);
                        string[] finalListToks = finalList.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        finalList = "";
                        for (int i = 0; i < instances.Count(); i++)
                        {
                            if (int.Parse(finalListToks[i]) > instance2Candidates2CentralBall[instances[i]].Count() - 1)
                                finalListToks[i] = (instance2Candidates2CentralBall[instances[i]].Count() - 1) + "";
                            finalList += finalListToks[i] + " ";
                        }
                        writeOut(instances, finalList);
                    }
                    swSB.Close();
                    swMB.Close();
                }
            }
        }

        private static void writeOut(List<string> instances, string finalList)
        {
            Dictionary<string, int> finalIndex = new Dictionary<string, int>();
            string [] finalListToks=finalList.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < instances.Count(); i++)
                finalIndex[instances[i]] = int.Parse(finalListToks[i]);
            foreach (string instance in instances)
            {
                if (instance2Label[instance].Equals("S"))
                {
                    swSB.Write(instance + "\t");
                    int index=finalIndex[instance];
                    swSB.Write(instance2CandidateList[instance][index] + "\t" + instance2Candidates2Score[instance][instance2CandidateList[instance][index]] + "\t");
                    foreach (string s in instance2CandidateList[instance])
                    {
                        if (s.Equals(instance2CandidateList[instance][index]))
                            continue;
                        swSB.Write(s + "\t" + instance2Candidates2Score[instance][s] + "\t");
                    }
                    swSB.WriteLine();
                }
                if (instance2Label[instance].Equals("M"))
                {
                    swMB.Write(instance + "\t");
                    int index=finalIndex[instance];
                    swMB.Write(instance2CandidateList[instance][index] + "\t" + instance2Candidates2Score[instance][instance2CandidateList[instance][index]] + "\t");
                    foreach (string s in instance2CandidateList[instance])
                    {
                        if (s.Equals(instance2CandidateList[instance][index]))
                            continue;
                        swMB.Write(s + "\t" + instance2Candidates2Score[instance][s] + "\t");
                    }
                    swMB.WriteLine();
                }
            }
        }

        private static void writeOut(List<string> instances)
        {
            foreach (string instance in instances)
            {
                if (instance2Label[instance].Equals("S"))
                {
                    swSB.Write(instance + "\t");
                    foreach (string s in instance2CandidateList[instance])
                        swSB.Write(s + "\t" + instance2Candidates2Score[instance][s] + "\t");
                    swSB.WriteLine();
                }
                if (instance2Label[instance].Equals("M"))
                {
                    swMB.Write(instance + "\t");
                    foreach (string s in instance2CandidateList[instance])
                        swMB.Write(s + "\t" + instance2Candidates2Score[instance][s] + "\t");
                    swMB.WriteLine();
                }
            }
        }

        private static void loadInstance2Candidates2Score()
        {
            StreamReader sr = new StreamReader(bestSingleFile);
            string str = "";
            while ((str = sr.ReadLine()) != null)
            {
                string[] toks = str.Split(new char[]{'\t'}, StringSplitOptions.RemoveEmptyEntries);
                string instance = toks[0];
                instance2Candidates2Score[instance] = new Dictionary<string, double>();
                instance2CandidateList[instance] = new List<string>();
                instance2Label[instance] = "S";
                instance2Candidates2CentralBall[instance] = new Dictionary<string,string>();
                for (int i = 1; i < toks.Count(); i += 2)
                {
                    instance2Candidates2Score[instance][toks[i]] = double.Parse(toks[i + 1]);
                    instance2Candidates2CentralBall[instance][toks[i]] = toks[i];
                    instance2CandidateList[instance].Add(toks[i]);
                }
            }
            sr.Close();
            sr = new StreamReader(bestMultiFile);
            while ((str = sr.ReadLine()) != null)
            {
                string[] toks = str.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
                string instance = toks[0];
                instance2Candidates2Score[instance] = new Dictionary<string, double>();
                instance2Candidates2CentralBall[instance] = new Dictionary<string, string>();
                instance2CandidateList[instance] = new List<string>();
                instance2Label[instance] = "M";
                for (int i = 1; i < toks.Count(); i += 2)
                {
                    instance2Candidates2Score[instance][toks[i]] = double.Parse(toks[i + 1]);
                    instance2CandidateList[instance].Add(toks[i]);
                    string[] toks2 = toks[i].Split(',');
                    string middleBall = toks2[toks2.Length / 2];
                    instance2Candidates2CentralBall[instance][toks[i]] = middleBall;
                }
            }
            sr.Close();
        }

        private static int distanceBetweenBalls(string ball1, string ball2)
        {
            int over1=int.Parse(ball1.Split(':')[1].Split('.')[0]);
            int over2 = int.Parse(ball2.Split(':')[1].Split('.')[0]);
            int balls=6-int.Parse(ball1.Split(':')[1].Split('.')[1])+int.Parse(ball2.Split(':')[1].Split('.')[1])-1;
            return ((over2 - over1) - 1)*6+balls;
        }

        private static void loadInstance2Paras()
        {
            StreamReader sr = new StreamReader(baseDir + "matchArticlesCoreferencedPOSTagged.txt");
            string str = "";
            int paraCount = 0;
            while ((str = sr.ReadLine()) != null)
            {
                string match_article = str.Split('\t')[0] + "_" + str.Split('\t')[1];
                int match = int.Parse(str.Split('\t')[0]);
                if (match > Global.numMatches)
                    break;
                string text = str.Split('\t')[6];
                string[] paras = Regex.Split(text, "#p#");
                string mentionNum = "";
                foreach(string para in paras)
                {
                    paraCount++;
                    para2InstanceList[paraCount+""] = new List<string>();
                    string paraText = para.Replace("<m", "\n<m").Replace("</m", "\n</m");
                    string[] paraToks = paraText.Split('\n');
                    foreach (string t in paraToks)
                    {
                        if (!t.StartsWith("<m"))
                            continue;
                        mentionNum = t.Split('>')[0].Replace("<m", "");
                        string instance = match_article + "_" + mentionNum;
                        instance2Paras[instance] = paraCount;
                        para2InstanceList[paraCount + ""].Add(instance);
                    }
                }
            }
            sr.Close();
        }
    }
}
