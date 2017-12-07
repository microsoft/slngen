using System;
using System.Collections.Generic;

namespace SlnGen
{
    internal class SlnError
    {
        private static readonly ErrorId[] AllEnabled =
        {
            ErrorId.MissingFile,
            ErrorId.LoadFailed,
            ErrorId.MissingOrBadGuid,
            ErrorId.MissingVersion,
            ErrorId.MissingAssemblyName,
            ErrorId.MismatchedAssemblyName,
            ErrorId.DuplicateGuid,
            ErrorId.DuplicateProjectName,
            ErrorId.ProjectsOverlapped,
            ErrorId.BadAdditionalProperties,
        };

        private static readonly ErrorId[] DefaultEnabled =
        {
            ErrorId.MissingFile,
            ErrorId.LoadFailed,
            ErrorId.MissingOrBadGuid,
            ErrorId.MissingAssemblyName,
            ErrorId.DuplicateGuid,
            ErrorId.DuplicateProjectName,
            ErrorId.BadAdditionalProperties,
        };

        public SlnError(ErrorId theId, string theSource, params string[] theExtraInfo)
        {
            Id = theId;
            Source = theSource;
            ExtraInfo = theExtraInfo;
        }

        public enum ErrorId
        {
            NoErrors,
            DefaultErrors,
            AllErrors,
            MissingFile,
            LoadFailed,
            MissingOrBadGuid,
            MissingVersion,
            MissingAssemblyName,
            MismatchedAssemblyName,
            DuplicateGuid,
            DuplicateProjectName,
            ProjectsOverlapped,
            BadAdditionalProperties,
        }

        public static Dictionary<ErrorId, ErrorId> Enabled { get; private set; }

        public static List<SlnError> Errors { get; private set; }

        public static List<string> Names { get; private set; }

        public static Dictionary<string, ErrorId> Translate { get; private set; }

        public string[] ExtraInfo { get; }

        public ErrorId Id { get; }

        public string Source { get; }

        public static bool ChangeEnabled(string name)
        {
            if (!Translate.ContainsKey(name))
            {
                return false;
            }

            ErrorId id = Translate[name];
            bool enable = !name.StartsWith("-", StringComparison.Ordinal);

            switch (id)
            {
                case ErrorId.NoErrors:
                    Enabled.Clear();
                    break;

                case ErrorId.DefaultErrors:
                    Enabled.Clear();
                    foreach (ErrorId toEnable in DefaultEnabled)
                    {
                        Enabled[toEnable] = toEnable;
                    }
                    break;

                case ErrorId.AllErrors:
                    Enabled.Clear();
                    foreach (ErrorId toEnable in AllEnabled)
                    {
                        Enabled[toEnable] = toEnable;
                    }
                    break;

                default:
                    if (enable)
                    {
                        Enabled[id] = id;
                    }
                    else
                        Enabled.Remove(id);
                    break;
            }

            return true;
        }

        public static void Init()
        {
            Enabled = new Dictionary<ErrorId, ErrorId>();
            foreach (ErrorId id in DefaultEnabled)
            {
                Enabled[id] = id;
            }

            Names = new List<string>();
            foreach (ErrorId id in AllEnabled)
            {
                Names.Add(id.ToString());
            }

            Translate = new Dictionary<string, ErrorId>(StringComparer.OrdinalIgnoreCase);
            foreach (ErrorId id in AllEnabled)
            {
                string name = id.ToString();
                Translate[name] = id;
                Translate[$"-{name}"] = id;
            }

            Translate["none"] = ErrorId.NoErrors;
            Translate["default"] = ErrorId.DefaultErrors;
            Translate["all"] = ErrorId.AllErrors;

            Errors = new List<SlnError>();
        }

        public static void PrintErrors()
        {
            if (Errors.Count == 0)
            {
                return;
            }

            Log.Error($"{Errors.Count} errors were found.");

            foreach (SlnError error in Errors)
            {
                Log.Info($"Error {error.Id} in {error.Source}");

                if (error.ExtraInfo == null)
                {
                    continue;
                }

                foreach (string extra in error.ExtraInfo)
                {
                    Log.Info($"\t{extra}");
                }
            }
        }

        public static void ReportError(ErrorId id, string theSource, params string[] theExtraInfo)
        {
            if (!Enabled.ContainsKey(id))
            {
                return;
            }

            Errors.Add(new SlnError(id, theSource, theExtraInfo));
        }
    }
}