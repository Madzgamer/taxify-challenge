# Taxify-Challenge
Entry for the Taxify's Self-driving Fleet Optimization Challenge.
by Oliver Olmaru and Randal Annus
Thanks to Ingmar Olmaru

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
To change the depos, copy the depos file (named depots.csv) to
```
\taxify-challenge-master\self-driving-fleet\bin\Debug\depots.csv
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
Insert the number of threads, for optimal performace thr number should be equal to the number of cores on the computers processor (ex. 4). If the number of cores is not known 4 should be entered. The number of of threads must be >= 1 and <= 16.
For Example:
```
4
```
and press enter.
