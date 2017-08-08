wget http://www.espncricinfo.com/ci/engine/series/381449.html -O seriesHome.html
grep "view=commentary" seriesHome.html |cut -d'"' -f4|sed 's/^/http:\/\/www.espncricinfo.com/g' > innings1Urls.txt
i=1; while read line; do wget "$line" -O match${i}innings1Commentary.html; i=$(($i+1));done< innings1Urls.txt
sed 's/innings=1/innings=2/g' innings1Urls.txt|sed 's/view=commentary/page=1;view=commentary/g' > innings2Urls.txt
i=1; while read line; do wget "$line" -O match${i}innings2Commentary.html; i=$(($i+1));done< innings2Urls.txt
grep "Scorecard</a>" seriesHome.html|cut -d'"' -f6|sed 's/^/http:\/\/www.espncricinfo.com/g' > scorecardUrls.txt
i=1; while read line; do wget "$line" -O match${i}Scorecard.html; i=$(($i+1));done< scorecardUrls.txt
grep "Article index" seriesHome.html|cut -d'"' -f4|sed 's/^/http:\/\/www.espncricinfo.com/g' > articleIndexUrls.txt
i=1; while read line; do wget "$line" -O match${i}ArticleIndex.html; i=$(($i+1));done< articleIndexUrls.txt
for i in `seq 1 49`; do grep "^  <p class=\"SpecialsHead\"" match${i}ArticleIndex.html |grep "/story/"|cut -d'"' -f4|sed 's/^/http:\/\/www.espncricinfo.com/g' > match${i}ArticlesUrls.txt; done
for j in `seq 1 49`; do while read line; do i=`echo ${line}|cut -d"/" -f7`; wget "$line" -O match${j}Article${i}; done< match${j}ArticlesUrls.txt; done

#get player profiles.
rm -rf tmp.txt;
for j in `seq 1 49`; 
do
echo "Processing match $j";
grep "view the player profile for" match${j}Scorecard.html |grep "<td width=\"192\"\|<span><a href"|sed 's/<[^>]*>//g'|sed 's/Did not bat//g'|sed 's/&[^;]*;//g' |sed 's/[^a-zA-Z ]//g'|sed 's/^ *//;s/ *$//' > tmp1.txt;
grep "view the player profile for" match${j}Scorecard.html |grep "<td width=\"192\"\|<span><a href"|awk -F"\"/icc" '{print "http://www.espncricinfo.com/icc"$2}'|cut -d'"' -f1 > tmp2.txt;
paste tmp1.txt tmp2.txt >> tmp.txt
done
rm -rf tmp1.txt tmp2.txt
sort tmp.txt |uniq > playerProfileURLs.txt;
cut -f2 playerProfileURLs.txt> tmp.txt;
j=1;
while read line; do 
wget "${line}" -O p${j}.html;
j=$(($j+1));
done< tmp.txt
j=1;
while read line; do 
grep "PlayersSearchLink" p${j}.html |sed 's/<[^>]*>//g'|sed 's/\t*//' >> tmp1.txt;
j=$(($j+1));
done < tmp.txt
rm -rf tmp.txt;
paste playerProfileURLs.txt tmp1.txt > playerURLCountry.txt;
rm -rf playerProfileURLs.txt tmp1.txt
