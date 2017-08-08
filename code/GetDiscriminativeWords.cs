using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CricketLinking
{
    class GetDiscriminativeWords
    {
        static Dictionary<string, Dictionary<string, int>> class2Word2Freq = new Dictionary<string, Dictionary<string, int>>();
        static void Main(string[] args)
        {
            StreamReader sr = new StreamReader(Global.baseDir+"linkedLabels.tsv");
            string str = "";
            while((str=sr.ReadLine())!=null)
            {
                string[] toks = str.Split('\t');
                string className = toks[10];
                string mention = toks[3].ToLower();
                if(!className.Trim().Equals(""))
                {
                    if (!class2Word2Freq.ContainsKey(className))
                        class2Word2Freq[className] = new Dictionary<string, int>();
                    Dictionary<string, int> dict = class2Word2Freq[className];
                    foreach(string m in mention.Split(' '))
                    {
                        if (dict.ContainsKey(m))
                            dict[m]++;
                        else
                            dict[m] = 1;
                    }
                }
            }
            sr.Close();
            for(int i=0;i<Global.multipleClasses.Count();i++)
            {
                StreamWriter sw = new StreamWriter(Global.baseDir+"multi"+Global.multipleClasses[i]+".txt");
                Dictionary<string, int> thisClassDict = new Dictionary<string, int>();
                Dictionary<string, int> otherClassDict = new Dictionary<string, int>();
                thisClassDict = class2Word2Freq[Global.multipleClasses[i]];
                for (int j = 0; j < Global.multipleClasses.Count(); j++)
                {
                    if(i!=j)
                    {
                        foreach (string m in class2Word2Freq[Global.multipleClasses[j]].Keys)
                        {
                            if (otherClassDict.ContainsKey(m))
                                otherClassDict[m] += class2Word2Freq[Global.multipleClasses[j]][m];
                            else
                                otherClassDict[m] = class2Word2Freq[Global.multipleClasses[j]][m];
                        }
                    }
                }
                Dictionary<string, double> newDict = new Dictionary<string, double>();
                foreach(string m in thisClassDict.Keys)
                {
                    if (otherClassDict.ContainsKey(m))
                        newDict[m] = (double)thisClassDict[m] / otherClassDict[m];
                    else
                        newDict[m] = thisClassDict[m];
                }
                List<KeyValuePair<string, double>> myList1 = newDict.ToList();
                myList1.Sort((x, y) => y.Value.CompareTo(x.Value));
                foreach(KeyValuePair<string, double> kv in myList1)
                    sw.WriteLine(kv.Key+"\t"+kv.Value);
                sw.Close();
            }
        }
    }
}
