using System;

// TODO: Use log4net

namespace SlnGen
{
    public enum LogVerbosity
    {
        Quiet = -1,
        Normal = 0,
        Verbose = 1,
        ReallyVerbose = 2,
    }

    internal static class Log
    {
        private static LogVerbosity Verbosity { get; set; } = LogVerbosity.Normal;

        public static void Error(Exception e)
        {
            Add(LogVerbosity.Quiet, "Error: {0}", e.Message);
            Add(LogVerbosity.Verbose, e.StackTrace);
        }

        public static void Error()
        {
            Add(LogVerbosity.Quiet);
        }

        public static void Error(string message, params object[] args)
        {
            Add(LogVerbosity.Quiet, message, args);
        }

        public static void Info(string message, params object[] args)
        {
            Add(LogVerbosity.Normal, message, args);
        }

        public static void Info()
        {
            Add(LogVerbosity.Normal);
        }

        public static void ReallyVerbose(string message, params object[] args)
        {
            Add(LogVerbosity.ReallyVerbose, message, args);
        }

        public static void SetVerbosity(int verbose)
        {
            Verbosity = LogVerbosity.Normal;
            if (verbose < 0)
            {
                Verbosity = LogVerbosity.Quiet;
            }
            else if (verbose == 1)
            {
                Verbosity = LogVerbosity.Verbose;
            }
            else if (verbose > 1)
            {
                Verbosity = LogVerbosity.ReallyVerbose;
            }
        }

        public static void Verbose()
        {
            Add(LogVerbosity.Verbose);
        }

        public static void Verbose(string message, params object[] args)
        {
            Add(LogVerbosity.Verbose, message, args);
        }

        private static void Add(LogVerbosity verbosity, string message, params object[] args)
        {
            if (verbosity <= Verbosity)
            {
                Console.WriteLine(message, args);
            }
        }

        private static void Add(LogVerbosity verbosity)
        {
            if (verbosity <= Verbosity)
            {
                Console.WriteLine();
            }
        }
    }
}