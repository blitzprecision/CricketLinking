rem run.cmd {in:LESDiffReport:input1} {in:GenericTSV:input2} (workerID) {out:GenericTSV:output1} (workersPerMachine)
hostname
del 0.txt 1.txt 2.txt 3.txt 4.txt 5.txt 6.txt 7.txt 
wmic cpu get NumberOfCores
unzip %1 -d 0
unzip %1 -d 1
unzip %1 -d 2
unzip %1 -d 3
unzip %1 -d 4
unzip %1 -d 5
unzip %1 -d 6
unzip %1 -d 7

start StanfordCoreNLPLinguisticAnalysis.exe %5 %2 %3 out0.txt
@set /a c=%3+1                              
start StanfordCoreNLPLinguisticAnalysis.exe %5 %2 %c% out1.txt
@set /a c=%3+2                              
start StanfordCoreNLPLinguisticAnalysis.exe %5 %2 %c% out2.txt
@set /a c=%3+3                              
start StanfordCoreNLPLinguisticAnalysis.exe %5 %2 %c% out3.txt
@set /a c=%3+4                              
start StanfordCoreNLPLinguisticAnalysis.exe %5 %2 %c% out4.txt
@set /a c=%3+5                              
start StanfordCoreNLPLinguisticAnalysis.exe %5 %2 %c% out5.txt
@set /a c=%3+6                              
start StanfordCoreNLPLinguisticAnalysis.exe %5 %2 %c% out6.txt
@set /a c=%3+7                              
start StanfordCoreNLPLinguisticAnalysis.exe %5 %2 %c% out7.txt

:waitForSpawned
if exist 0.txt (echo "Worker 0 finished")
if exist 1.txt (echo "Worker 1 finished")
if exist 2.txt (echo "Worker 2 finished")
if exist 3.txt (echo "Worker 3 finished")
if exist 4.txt (echo "Worker 4 finished")
if exist 5.txt (echo "Worker 5 finished")
if exist 6.txt (echo "Worker 6 finished")
if exist 7.txt (echo "Worker 7 finished")

more 0.log
more 1.log
more 2.log
more 3.log
more 4.log
more 5.log
more 6.log
more 7.log
wmic os get freephysicalmemory

if exist 0.txt if exist 1.txt if exist 2.txt if exist 3.txt if exist 4.txt if exist 5.txt if exist 6.txt if exist 7.txt (
copy /b out*.txt %4
del 0.txt 1.txt 2.txt 3.txt 4.txt 5.txt 6.txt 7.txt 
del 0.log 1.log 2.log 3.log 4.log 5.log 6.log 7.log 
exit 
)
>nul PING localhost -n 200 -w 1000
goto:waitForSpawned