using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace CricketLinking
{
    class PrepareDataForLingAnalysis
    {
        public static string dir=Global.baseDir;
        static void Main(string[] args)
        {

            //article files
            StreamReader sr = new StreamReader(dir+"matchArticles.txt");
            StreamWriter sw = new StreamWriter(dir+"matchArticles.tsv");
            string str = "";
            int worker = 0;
            int count = 0;
            while((str=sr.ReadLine())!=null)
            {
                if(count==63)
                {
                    count = 0;
                    worker++;
                }
                string[] toks = str.Split('\t');
                string text=Regex.Replace(toks[6], "</?m[0-9]+>", "");
                string [] paras = Regex.Split(text, "#p#");
                foreach (string p in paras)
                {
                    if (!p.Equals(""))
                    {
                        sw.WriteLine(toks[0] + "_" + toks[1] + "\t" + worker + "\t" + p);
                        count++;
                        if (count == 63)
                        {
                            count = 0;
                            worker++;
                        }
                    }
                }
            }
            sr.Close();
            sw.Close();

            //commentary files
            sr = new StreamReader(dir + "matchCommentary.txt");
            sw = new StreamWriter(dir + "matchCommentary.tsv");
            worker = 0;
            count = 0;
            while ((str = sr.ReadLine()) != null)
            {
                if (count == 255)
                {
                    count = 0;
                    worker++;
                }
                string[] toks = str.Split('\t');
                if (!toks[24].EndsWith(".") && !toks[24].EndsWith("?") && !toks[24].EndsWith("!"))
                    toks[24] = toks[24] + ".";
                sw.WriteLine(toks[0] + "_" + toks[1] + "_"+toks[2]+ "\t" + worker + "\t" + toks[24]);
                count++;
            }
            sr.Close();
            sw.Close();

            //coreferenced article files
            sr = new StreamReader(dir + "matchArticlesCoreferenced.txt");
            sw = new StreamWriter(dir + "matchArticlesCoreferenced.tsv");
            worker = 0;
            count = 0;
            while ((str = sr.ReadLine()) != null)
            {
                if (count == 63)
                {
                    count = 0;
                    worker++;
                }
                string[] toks = str.Split('\t');
                string text = Regex.Replace(toks[6], "</?m[0-9]+>", "");
                string[] paras = Regex.Split(text, "#p#");
                foreach (string p in paras)
                {
                    if (!p.Trim().Equals(""))
                    {
                        sw.WriteLine(toks[0] + "_" + toks[1] + "\t" + worker + "\t" + p);
                        count++;
                        if (count == 63)
                        {
                            count = 0;
                            worker++;
                        }
                    }
                }
            }
            sr.Close();
            sw.Close();

        }
    }
}
