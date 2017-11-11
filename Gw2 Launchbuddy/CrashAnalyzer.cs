﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;

namespace Gw2_Launchbuddy
{
    public class CrashFilter
    {
        public string[] keywords;
        public string solution;
        public CrashFilter(string[] Keywords,string Solution)
        {
            keywords = Keywords;
            solution = Solution;
        }
        public int Matchcount(string assertion)
        {
            int matches = 0;
            foreach (string key in keywords)
            {
                matches += Regex.Matches(assertion, key).Count;
            }
            return matches;
        }
    }

    static public class CrashLibrary
    {
        public static List<CrashFilter> CrashFilters = new List<CrashFilter> {
            new CrashFilter(new string[] { "c0000005", "Memory at address","could not be read" }, "mem_read"),
            new CrashFilter(new string[] { "c0000005", "Memory at address","could not be written" }, "mem_write")
        };

        public static Dictionary<string, string> SolutionInfo = new Dictionary<string, string>
        {
            { "unknown","Cooooo...\nQuaggan doesn't know what to do with this crash. :(" },
            { "mem_read","Cooo!\nSeems like a memory read error happended to you!\nQuaggan knows that this sometimes happens when your Gw2.dat file gets corrupted.\nSometimes using the -repair argument will help youuuu!" },
            { "mem_write","Cooo!\nSeems like a memory write error happended to you!\nQuaggan knows that this sometimes happens when your Gw2.dat file gets corrupted.\nSometimes using the -repair argument will help youuuu!" },
        };


        static public string ClassifyCrash(Crashlog log)
        {
            int highestmatch = 0;
            int hm_index=0;
            for (int i = 0; i <= CrashLibrary.CrashFilters.Count - 1; i++)
            {
                if (CrashLibrary.CrashFilters[i].Matchcount(log.Assertion) > highestmatch && CrashLibrary.CrashFilters[i].Matchcount(log.Assertion) != 0)
                {
                    highestmatch = CrashLibrary.CrashFilters[i].Matchcount(log.Assertion);
                    hm_index = i;
                }
            }
            if (highestmatch>1)
            return CrashFilters[hm_index].solution;
            return "unknown";
        }

        public static void ApplySolution(string solutionkey)
        {
            switch (solutionkey)
            {
                case "mem_read":
                    mem_read();
                    break;
                case "mem_write":
                    mem_write();
                    break;
            }
        }

        //Solutions
        private static void mem_read()
        {
            System.Windows.Forms.MessageBox.Show("Placeholder mem_read");
        }
        private static void mem_write()
        {
            System.Windows.Forms.MessageBox.Show("Placeholder mem_write");
        }
    }

    static public class CrashAnalyzer
    {
       
        //Reading/Managing Crashlogs
        public static ObservableCollection<Crashlog> Crashlogs= new ObservableCollection<Crashlog>();

        static public Crashlog GetLatestCrashlog()
        {
            return Crashlogs[Crashlogs.Count - 1];
        }

        static public Crashlog GetCrashByIndex(int index)
        {
            return Crashlogs[index];
        }

        static public void ReadCrashLogs(string path = null)
        {
            Crashlogs.Clear();
           if (path == null)
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Guild Wars 2\\Arenanet.log";
            }

            try
            {
                string[] data = Regex.Split(File.ReadAllText(path), @"\*--> Crash <--\*");
                for (int i = 1; i < data.Length; i++)
                {
                    Crashlogs.Add(new Crashlog(data[i]));
                }

                Crashlogs = new ObservableCollection<Crashlog>(from i in Crashlogs orderby i.CrashTime select i);
                Crashlogs = new ObservableCollection<Crashlog>(Crashlogs.Reverse());

                //Clean up Crashlog   
                /*          
                 if (Crashlogs.Count >= 25)
                {
                    try
                    {
                        string logs = "";

                        for (int i = 2; i < data.Length; i++)
                        {
                            logs += @"*--> Crash <--*" + data[i];
                        }

                        File.Delete(path);
                        File.WriteAllText(path, logs);
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show(e.Message);
                    }
                }
                */
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show("Could not find Crashlog!\n" + e.Message);
            }
        }
    }

    public class Crashlog
    {
        // Crashinfos
        public string Assertion;
        string Filename;
        string Exename;
        uint Pid;
        string[] Arguments;
        string BaseAddr;
        string ProgramID;
        public string Build { set; get; }
        string crashtime;
        public string CrashTime
        {
            get { return crashtime; }
            set
            {
                Match match = Regex.Match(value, @"(?<Date>\d{4}-\d\d-\d\d)T(?<Time>\d\d:\d\d:\d\d)\+");
                crashtime = match.Groups["Date"].Value + " " + match.Groups["Time"].Value;
            }
        }

        private string UpTime;

        //System
        private string Username;

        private string IPAdress;
        private string Processors;
        private string OS;

        //DLLS
        private string[] DllList;

        //Outputs
        public string Quickinfo
        {
            get {
                string tmp = "Crashtime: " + CrashTime;
                tmp += "\nBuild: " + Build;
                tmp += "\nAssertion: " + Assertion;
                return tmp;
            }
        }

        //Solution
        public string Solutioninfo;
        public string Solutionkey { set; get; }
        public void Solve() { if (IsSolveable) CrashLibrary.ApplySolution(Solutionkey); }
        public bool IsSolveable {
            get { if (Solutionkey != "unknown") return true;
                return false;
            } }

        public Crashlog(string crashdata)
        {
            //Only use splitted Crash data!
            Assertion = Regex.Match(crashdata, @"Assertion: ?(?<data>.*)").Groups["data"].Value;
            if (Assertion=="") Assertion= Regex.Match(crashdata, @"Exception: ?(?<data>.*\n.*)").Groups["data"].Value;

            Filename = Regex.Match(crashdata, @"File: ?(?<data>.*)").Groups["data"].Value;
            Exename = Regex.Match(crashdata, @"App: ?(?<data>.*)").Groups["data"].Value;
            Pid = UInt16.Parse(Regex.Match(crashdata, @"Pid: ?(?<data>.*)").Groups["data"].Value);
            Arguments = Regex.Match(crashdata, @"Cmdline: ?(?<data>.*)").Groups["data"].Value.Split('-');
            BaseAddr = Regex.Match(crashdata, @"BaseAddr: ?(?<data>.*)").Groups["data"].Value;
            ProgramID = Regex.Match(crashdata, @"ProgramId: ?(?<data>.*)").Groups["data"].Value;
            Build = Regex.Match(crashdata, @"Build: ?(?<data>.*)").Groups["data"].Value;
            CrashTime = Regex.Match(crashdata, @"When: ?(?<data>.*)").Groups["data"].Value;
            UpTime = Regex.Match(crashdata, @"Uptime: ?(?<data>.*)").Groups["data"].Value;

            Username = Regex.Match(crashdata, @"Name: ?(?<data>.*)").Groups["data"].Value;
            IPAdress = Regex.Match(crashdata, @"IpAddr: ?(?<data>.*)").Groups["data"].Value;
            Processors = Regex.Match(crashdata, @"Processors: ?(?<data>.*)").Groups["data"].Value;
            OS = Regex.Match(crashdata, @"OSVersion: ?(?<data>.*)").Groups["data"].Value;

            DllList = Regex.Matches(crashdata, @"\w:\\.*.dll").Cast<Match>().Select(m => m.Value).ToArray();
            Solutionkey = CrashLibrary.ClassifyCrash(this);
            Solutioninfo = CrashLibrary.SolutionInfo[Solutionkey];
        }
    }
}