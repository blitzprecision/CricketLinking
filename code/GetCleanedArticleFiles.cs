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
    /// Generates structured file for match articles: Generate matchArticles.txt -- match#, article#, title, head, author, date, story.
    /// </summary>
    class GetCleanedArticleFiles
    {
        public static string dir = Global.baseDir;
        static void Main(string[] args)
        {
            StreamWriter sw = new StreamWriter(dir+ "matchArticles.txt");
            StreamWriter sw2 = new StreamWriter(dir + "articlesCorpus.tsv");
            int lines = 0;
            int workerID = -1;
            for (int i = 1; i <= Global.numMatches; i++)
            {
                string[] files = Directory.GetFiles(dir, "match"+i+"Article*.html");
                foreach (string f in files)
                {
                    if (f.Contains("ArticleIndex"))
                        continue;
                    StreamReader sr = new StreamReader(f);
                    string str = "";
                    string title = "";
                    string head = "";
                    string story = "";
                    string author="";
                    string date="";
                    int authorStart=0;
                    int storyStart = 0;
                    int storyStart2 = 0;
                    int dateStart = 0;
                    string [] toks=f.Split('\\');
                    int articleNum = int.Parse(Regex.Replace(toks[toks.Length-1].Split('.')[0],"match.*Article",""));
                    while((str=sr.ReadLine())!=null)
                    {
                        if (storyStart == 0)
                        {
                            if ((str.Contains("magTitle") && str.Contains("<h3"))||str.Contains("<span class=\"sub-headline"))
                                title = convertHTMLToText(str);
                            if ((str.Contains("magHead") && str.Contains("<h1"))||str.Contains("<h1 class=\"col-1-1"))
                                head = convertHTMLToText(str);
                            if(str.Contains("<p class=\"magAthr\">"))
                                authorStart=1;
                            if (str.Contains("<p class=\"magDate\""))
                                date = convertHTMLToText(str);
                            if (str.Contains("<div id=\"storyTxt\""))
                                storyStart = 1;
                        }
                        if (str.Contains("<span class=\"date"))
                            dateStart = 1;
                        if(dateStart==1)
                        {
                            date += str + " ";
                            if (str.Contains("</span>"))
                            {
                                date = convertHTMLToText(date);
                                dateStart = 0;
                            }
                        }
                        if (str.Contains("story-content-travel\">"))
                            storyStart2 = 1;
                        if(storyStart2!=0)
                        {
                            story += str + " ";
                            if (str.Contains("<section class=\"video-section"))
                                storyStart2++;
                            if (str.Contains("</section>") && !str.Contains("story-content-travel\">"))
                            {
                                story = convertHTMLToText(story);
                                storyStart2--;
                            }
                        }
                        if (authorStart == 1)
                        {
                            author += str + " ";
                            if (str.Contains("</p>"))
                            {
                                author = convertHTMLToText(author);
                                authorStart = 0;
                            }
                        }
                        if(storyStart==1)
                        {
                            story += str + " ";
                            if (str.Contains("body area ends here"))
                            {
                                story = convertHTMLToText(story).Replace('\t',' ');
                                storyStart = 0;
                            }
                        }
                    }
                    sr.Close();
                    story = removeTabNewLines(story).Replace("\".", "\"").Replace("Watch out for….", " ").Replace("…", " ");
                    story=Regex.Replace(story, "(#p#[.\\| ]*)+", "#p#");
                    sw.WriteLine(i + "\t" + articleNum + "\t" + removeTabNewLines(title.Replace("#p#", "")) + "\t" + removeTabNewLines(head.Replace("#p#", "")) + "\t" + removeTabNewLines(author.Replace("#p#", "")) + "\t" +
                        removeTabNewLines(date.Replace("#p#", "")) + "\t" + story);
                    string[] storyToks = Regex.Split(story, "#p#");
                    int m=1;
                    foreach(string s in storyToks)
                        if (!s.Trim().Equals(""))
                        {
                            if (lines % 16 == 0)
                                workerID++;
                            lines++;
                            sw2.WriteLine(i + "_" + articleNum + "_" + m + "\t" + workerID + "\t" + s);
                            m++;
                        }
                    Console.WriteLine(f);
                }
            }
            sw.Close();
            sw2.Close();
        }
        public static string removeTabNewLines(string str)
        {
            string retStr=str.Replace('\t', ' ').Replace('\n', ' ').Replace('\r', ' ');
            //cleanDotsAndSpaces
            for (int i = 0; i < 5; i++)
                retStr = retStr.Replace(". . ", ". ");
            retStr = retStr.Replace(". .", ".");
            retStr = retStr.Replace(" .", ".").Trim();
            if (retStr.IndexOf(". ") == 0)
                retStr = retStr.Substring(2);
            return retStr;
        }
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
                //Regex rgx = new Regex(pattern);
                strb = new StringBuilder(Regex.Replace(strb+"","<p[^>]*?>", "\n#p#"));
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
            str2 = Regex.Replace(str2, "<a href=[^>]*>", "");
            str2 = Regex.Replace(str2, "</a>", "");
            str2 = Regex.Replace(str2, "<([^\'\">]*((\'[^\']*\')|(\"[^\"]*\")))*[^\'\">]*>", " ");
            str2 = Regex.Replace(str2, "<[^>]*>", ". ");
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
            retStr = retStr.Replace("#@#@#", "$").Replace("\\s+"," ");
            return Regex.Replace(retStr, "\\s+"," ").Trim();
        }
    }
}
