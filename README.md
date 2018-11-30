# Taxify-Challenge
Entry for the Taxify's Self-driving Fleet Optimization Challenge.
by Oliver Olmaru and Randal Annus.
Thanks to Ingmar Olmaru.

## Getting Started
Follow these steps to get a copy of the project up and running on your local machine.

### Prerequisites
A computer running Windows 7 or newer.

### Installing
Download the project and extract it to taxify-challenge-master. It can be wherever you want (ex. Desktop)
Copy the demand data (named robotex2.csv) to
```
\taxify-challenge-master\self-driving-fleet\bin\Debug\robotex2.csv
```
To change the preset depos, copy the depos file (named robotex-depos.csv) to
```
\taxify-challenge-master\self-driving-fleet\bin\Debug\robotex-depos.csv
```

### Deployment
Launch the program as administrator at
```
\taxify-challenge-master\self-driving-fleet\bin\Debug\self-driving-fleet.exe
```
A console window pops up asking for the number of car threads.
```
Please insert the number of car threads
```
Insert the number of threads, for optimal performace the number should be equal to the number of cores on the computers processor (ex. 4). If the number of cores is not known 4 should be entered. The number of threads must be >= 1 and <= 16.
For Example:
```
4
```
and press enter.
During the final a computer with 8 coores was used, thus the number of threads was 8.

Next the console asks for the running time in minutes. This must be set to the number of minutes given for the program to complete its task - 3. For example: if the time given for the machine to complete its task (meaning outputting a log file) is 60 minutes, then the number eneterd must be 57.
The number eneter must be >= 1.0
For example:
```
57
```
and press Enter.
During the final the tunning time was set to 57 minutes.

After that the program starts  calculating. It will output text to the console which is irrelevant to the end result. The program will finish in about the time given it to by the user (+- 1 minute). The program is finished when it outputs the total amount of money made and the total amount of time spent on the task. For example:
```
Total money: 1111
Total execution time was 22222 ms
```

The output log is at
```
\taxify-challenge-master\self-driving-fleet\bin\Debug\output.txt
```
