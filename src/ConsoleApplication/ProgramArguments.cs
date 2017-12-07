using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SlnGen
{
    internal class ProgramArguments
    {
        private static readonly IProgramArgumentConverter DefaultConverter = new TypeBasedConverter();

        private static readonly char[] ReservedArgNameCharacters = {'-', '+', ':', '=', '/'};

        private readonly ProgramArgumentInfo[] _infos;

        private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        private Exception _parseException;

        public ProgramArguments(string[] args, ProgramArgumentInfo[] infos)
        {
            _infos = infos ?? throw new ArgumentNullException(nameof(infos));

            foreach (ProgramArgumentInfo info in infos)
            {
                foreach (char reserved in ReservedArgNameCharacters)
                {
                    if (info.Name.Contains(reserved))
                    {
                        throw new ArgumentException(String.Format(ProgramArgumentsStrings.ReservedCharacterInArgument, reserved, info.Name));
                    }
                }
            }

            ProcessAllArguments(args ?? throw new ArgumentNullException(nameof(args)));
        }

        public interface IProgramArgumentConverter
        {
            object Convert(object value, Type targetType);

            object ConvertBack(object value, Type targetType);
        }

        public static string Logo
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                Assembly assem = Assembly.GetEntryAssembly();

                object[] attr = assem.GetCustomAttributes(true);

                AssemblyTitleAttribute titleAttrib = attr.OfType<AssemblyTitleAttribute>().FirstOrDefault();

                AssemblyCopyrightAttribute copyrightAttrib = attr.OfType<AssemblyCopyrightAttribute>().FirstOrDefault();

                AssemblyDescriptionAttribute descrAttrib = attr.OfType<AssemblyDescriptionAttribute>().FirstOrDefault();

                if (!String.IsNullOrEmpty(titleAttrib?.Title))
                {
                    sb.Append(titleAttrib.Title);
                }

                if (sb.Length != 0)
                {
                    sb.Append(' ');
                }

                AssemblyFileVersionAttribute ver = attr.OfType<AssemblyFileVersionAttribute>().FirstOrDefault();
                if (ver != null)
                {
                    sb.Append($"v{ver.Version}");
                }

                if (sb.Length != 0)
                {
                    sb.AppendLine();
                }

                if (!String.IsNullOrEmpty(copyrightAttrib?.Copyright))
                {
                    sb.AppendLine(copyrightAttrib.Copyright.Replace("©", "(c)"));
                }
                if (!String.IsNullOrEmpty(descrAttrib?.Description))
                {
                    sb.AppendLine(descrAttrib.Description);
                }
                return sb.ToString();
            }
        }

        public bool ShouldShowUsageAndExit { get; internal set; }

        public string Usage
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(Logo);

                if (_parseException != null)
                {
                    sb.AppendLine();
                    sb.AppendLine(_parseException.Message);
                }

                string exe = Path.GetFileName(SplitArguments(Environment.CommandLine).First());
                StringChunk shortForms = new StringChunk(7, 80);
                shortForms.Append($"{ProgramArgumentsStrings.Usage}{exe} [@argfile] [/help|h|?]");

                // short-form args
                foreach (ProgramArgumentInfo info in _infos)
                {
                    shortForms.Append(' ');
                    if (info.HasDefault)
                    {
                        shortForms.Append('[');
                    }

                    if (!info.IsPositional)
                    {
                        shortForms.Append('/');
                    }

                    shortForms.Append(info.Name);
                    foreach (string sf in info.ShortForms)
                    {
                        shortForms.Append($"|{sf}");
                    }

                    if (!info.IsPositional)
                    {
                        shortForms.Append(GetValueUsageSnippet(info));
                    }

                    if (info.HasDefault)
                    {
                        shortForms.Append(']');
                    }
                }

                sb.AppendLine();
                sb.Append(shortForms);

                List<StringChunk> longForms = new List<StringChunk>();
                for (int i = 0; i != _infos.Length + 2; ++i)
                {
                    longForms.Add(new StringChunk(22, 80));
                }

                longForms[0].Append($"@argfile              {ProgramArgumentsStrings.ArgFileArgumentDescription}");

                int longFormIndex = 2;
                foreach (ProgramArgumentInfo info in _infos)
                {
                    StringChunk line = longForms[longFormIndex];

                    if (!info.IsPositional)
                    {
                        line.Append('/');
                    }

                    line.Append(info.Name);
                    if (!info.IsPositional)
                    {
                        line.Append(GetValueUsageSnippet(info));
                    }

                    int lineLength = line.ToString().Length;
                    if (lineLength < 22)
                    {
                        line.Append(new string(' ', 22 - lineLength));
                    }
                    else
                    {
                        line.Append(' ');
                    }

                    if (!String.IsNullOrEmpty(info.Description))
                    {
                        line.Append($"{info.Description} ");
                    }

                    if (info.HasDefault && info.Default != null && !info.Type.HasElementType)
                    {
                        IProgramArgumentConverter converter = DefaultConverter;
                        line.Append($"{ProgramArgumentsStrings.Default} {(string) converter.ConvertBack(info.Default, typeof(string))}. ");
                    }

                    if (info.ShortForms.Count != 0)
                    {
                        int count = info.ShortForms.Count;
                        line.Append(
                            count == 1 ? ProgramArgumentsStrings.ShortForm : ProgramArgumentsStrings.ShortForms);

                        for (int i = 0; i != count; ++i)
                        {
                            if (i != 0)
                            {
                                line.Append(", ");
                            }

                            line.Append($"/{info.ShortForms[i]}");
                        }
                        line.Append(".");
                    }

                    ++longFormIndex;
                }

                sb.AppendLine();
                sb.AppendLine();
                foreach (StringChunk longForm in longForms)
                {
                    sb.AppendLine(longForm.ToString());
                }

                return sb.ToString();
            }
        }

        public Exception UsageException
        {
            get => _parseException;

            set
            {
                _parseException = value;
                ShouldShowUsageAndExit = true;
            }
        }

        public T GetValue<T>(string name)
        {
            if (_values.ContainsKey(name))
            {
                object value = _values[name];
                if (typeof(IEnumerable).IsAssignableFrom(typeof(T)) && typeof(T) != typeof(string))
                {
                    if (typeof(Array).IsAssignableFrom(typeof(T)))
                    {
                        ArrayList arrayList = (ArrayList) value;
                        return (T) (object) arrayList.ToArray(arrayList[0].GetType());
                    }

                    throw new NotImplementedException(ProgramArgumentsStrings.ArrayCollectionsSupported);
                }
                return (T) value;
            }

            ProgramArgumentInfo info = _infos.First(i => String.Compare(i.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
            if (info.HasDefault)
            {
                return (T) info.Default;
            }

            throw new ArgumentException(String.Format(ProgramArgumentsStrings.RequiredArgumentNotSet, name));
        }

        private static string GetValueUsageSnippet(ProgramArgumentInfo info)
        {
            StringBuilder sb = new StringBuilder();
            if (typeof(Enum).IsAssignableFrom(info.Type))
            {
                IProgramArgumentConverter converter = DefaultConverter;
                foreach (object value in Enum.GetValues(info.Type))
                {
                    if (sb.Length == 0)
                    {
                        sb.Append(":(");
                    }
                    else
                    {
                        sb.Append('|');
                    }

                    sb.Append(converter.ConvertBack(value, typeof(string)));
                }
                sb.Append(')');
            }
            else if (!info.IsPositional && (info.Type == typeof(bool) || info.Type == typeof(bool[])))
            {
                sb.Append("[+|-]");
            }
            else
            {
                sb.Append(":value");
            }

            return sb.ToString();
        }

        private static int IndexOfCloseQuote(string s, int openQuoteIndex)
        {
            for (int i = openQuoteIndex + 1; i < s.Length; i++)
            {
                if (s[i] != '"')
                {
                    continue;
                }

                if (i == s.Length - 1)
                {
                    return i;
                }

                if (s[i + 1] != '"')
                {
                    return i;
                }

                // we're on an escape, so advance the cursor past the second "
                i++;
            }

            // if we get here, then we didn't find a matching close quote
            return -1;
        }

        private static bool IsSwitch(string arg)
        {
            // Allow argument escaping, e.g. //foo or --bar is really "/foo" or "-bar" as positional arg
            if (arg.StartsWith("-", StringComparison.Ordinal) && !arg.StartsWith("--", StringComparison.Ordinal))
            {
                return true;
            }

            if (arg.StartsWith("/", StringComparison.Ordinal) && !arg.StartsWith("//", StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        // Split things like "/foo=bar" and "-baz:quux" into "/foo" and "bar" and "-baz" and "quux" before
        // we process them, so all flags with values look at "next arg" uniformly
        private static IEnumerable<string> SplitArgumentNames(IEnumerable<string> args)
        {
            foreach (string arg in args)
            {
                char[] seps = {'=', ':'};
                if (IsSwitch(arg))
                {
                    int sepIndex = arg.IndexOfAny(seps);
                    if (sepIndex == -1)
                    {
                        yield return arg;
                    }
                    else
                    {
                        yield return arg.Substring(0, sepIndex).TrimEnd();
                        if (sepIndex + 1 != arg.Length)
                        {
                            yield return arg.Substring(sepIndex + 1).TrimStart();
                        }
                    }
                }
                else
                {
                    yield return arg;
                }
            }
        }

        private static IEnumerable<string> SplitArguments(string line)
        {
            StringBuilder currentArg = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];
                switch (currentChar)
                {
                    // whitespace
                    case ' ':
                    case '\t':
                        if (currentArg.Length != 0)
                        {
                            // we're in an arg, terminate it and reset currentArg buffer
                            yield return currentArg.ToString();
                            currentArg = new StringBuilder();
                        }
                        else
                        {
                            // we're not in an arg, so just strip whitespace
                        }
                        break;

                    // quote
                    case '"':
                        int closeQuoteIndex = IndexOfCloseQuote(line, i);
                        if (closeQuoteIndex == -1)
                        {
                            throw new Exception(
                                String.Format(ProgramArgumentsStrings.ArgumentContainsUnmatchedQuote, currentArg));
                        }

                        string stringVal = line.Substring(i + 1, closeQuoteIndex - i - 1);
                        // quoted strings may have a prefix (e.g., /out:"Foo Bar.dll") so append to any existing prefix
                        currentArg.Append(stringVal);
                        // however, quoted strings may not appear themselves as a
                        // prefix (e.g., /out:"Foo Bar".dll) so terminate - this is consistent with CSC.exe
                        yield return currentArg.ToString();
                        currentArg = new StringBuilder();
                        // eat the chars
                        i = closeQuoteIndex + 1;

                        break;

                    default:
                        // simply append the current char
                        currentArg.Append(currentChar);
                        break;
                }
            }

            // deal with the trailing argument
            if (currentArg.Length > 0)
            {
                yield return currentArg.ToString();
            }
        }

        private void ProcessAllArguments(string[] args)
        {
            try
            {
                // Process arguments given
                ProcessArguments(args);

                // Check that all required args have been found
                foreach (ProgramArgumentInfo info in _infos)
                {
                    if (!info.HasDefault && !_values.ContainsKey(info.Name))
                    {
                        throw new ArgumentException(String.Format(ProgramArgumentsStrings.RequiredArgumentNotSet, info.Name));
                    }
                }

                // Check for help
                if (_values.ContainsKey("help") && (bool) _values["help"])
                {
                    ShouldShowUsageAndExit = true;
                }
            }
            catch (ArgumentException ex)
            {
                UsageException = ex;
            }
        }

        private void ProcessArgumentFile(string argFilePath)
        {
            foreach (string line in File.ReadAllLines(argFilePath))
            {
                // strip off # comments
                int poundOffset = line.IndexOf('#');
                string argLine = poundOffset == -1 ? line : line.Substring(0, poundOffset);
                ProcessArguments(SplitArguments(argLine));
            }
        }

        private void ProcessArguments(IEnumerable<string> argsEnum)
        {
            string[] args = SplitArgumentNames(argsEnum).ToArray();

            int i = 0;
            while (i != args.Length)
            {
                bool nextArgUsed;
                ProcessSingleArgument(args[i], i + 1 == args.Length ? null : args[i + 1], out nextArgUsed);
                if (nextArgUsed)
                {
                    ++i;
                }

                ++i;
            }
        }

        // This method assumes quote handling and whitespace stripping has been done already by the shell or
        // SplitArguments, so don't call it directly
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Low pri issue in legacy code, but don't want to disable the rule broadly")]
        private void ProcessSingleArgument(string arg, string nextArg, out bool nextArgUsed)
        {
            // Assume we won't use the next arg as the value for this one
            nextArgUsed = false;

            if (arg[0] == '@')
            {
                // Strip off leading @
                ProcessArgumentFile(arg.Substring(1, arg.Length - 1));
                return;
            }

            ProgramArgumentInfo info;
            object boolOrStringValue;

            if (IsSwitch(arg))
            {
                // Switch based arg, e.g. /foo or -bar
                arg = arg.Substring(1);

                // Check for explicit +/- on a bool flag, e.g. /foo+ or /bar-
                bool? explicitBoolValue = null;
                if (arg.EndsWith("+", StringComparison.Ordinal) || arg.EndsWith("-", StringComparison.Ordinal))
                {
                    explicitBoolValue = arg.EndsWith("+", StringComparison.Ordinal);
                    arg = arg.Substring(0, arg.Length - 1);
                }

                info = _infos.FirstOrDefault(i =>
                {
                    if (i.IsPositional)
                    {
                        return false;
                    }

                    if (String.Compare(i.Name, arg, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        return true;
                    }

                    return i.ShortForms.Any(sf => String.Compare(sf, arg, StringComparison.OrdinalIgnoreCase) == 0);
                });

                if (info == null)
                {
                    throw new ArgumentException(String.Format(ProgramArgumentsStrings.UnknownArgument, arg));
                }

                // At this point, we only have a string or a bool
                if (!info.IsPositional && info.Type != typeof(bool) && info.Type != typeof(bool[]))
                {
                    // value'd argument
                    nextArgUsed = true;
                    boolOrStringValue = nextArg ?? throw new ArgumentException(String.Format(ProgramArgumentsStrings.MissingSwitchValue, arg));
                }
                else
                {
                    // just a flag
                    boolOrStringValue = explicitBoolValue ?? true;
                }
            }
            else
            {
                // Positional arg
                info = _infos.FirstOrDefault(i =>
                {
                    if (!i.IsPositional)
                    {
                        return false;
                    }

                    if (!_values.ContainsKey(i.Name))
                    {
                        return true;
                    }

                    return _values[i.Name] is IEnumerable;
                });

                if (info == null)
                {
                    throw new ArgumentException(String.Format(ProgramArgumentsStrings.NoMatchingPositionalArgument, arg));
                }

                boolOrStringValue = arg;
            }

            // Convert and set the value
            object value = DefaultConverter.Convert(
                boolOrStringValue, info.Type.HasElementType ? info.Type.GetElementType() : info.Type);

            if (typeof(IEnumerable).IsAssignableFrom(info.Type) && info.Type != typeof(string))
            {
                if (!_values.ContainsKey(info.Name))
                {
                    _values[info.Name] = new ArrayList();
                }

                ((ArrayList) _values[info.Name]).Add(value);
            }
            else
            {
                _values[info.Name] = value;
            }
        }

        public class ProgramArgumentInfo
        {
            private object _defaultValue;

            public ProgramArgumentInfo()
            {
                ShortForms = new List<string>();
                Type = typeof(bool);
            }

            public object Default
            {
                get => _defaultValue;

                set
                {
                    _defaultValue = value;
                    HasDefault = true;
                }
            }

            public string Description { get; set; }
            public bool HasDefault { get; internal set; }
            public bool IsPositional { get; set; }
            public string Name { get; set; }

            public IList<string> ShortForms { get; internal set; }

            public Type Type { get; set; }
        }

        public class TypeBasedConverter : IProgramArgumentConverter
        {
            public object Convert(object value, Type targetType)
            {
                if (value != null && targetType.IsInstanceOfType(value))
                {
                    return value;
                }

                return TypeDescriptor.GetConverter(targetType).ConvertFrom(value);
            }

            public object ConvertBack(object value, Type targetType)
            {
                if (value == null)
                {
                    return null;
                }
                if (targetType.IsInstanceOfType(value))
                {
                    return value;
                }
                return TypeDescriptor.GetConverter(value.GetType()).ConvertTo(value, targetType);
            }
        }

        private class StringChunk
        {
            private readonly List<StringBuilder> _lines = new List<StringBuilder>();
            private readonly string _spaces;
            private readonly int _width;

            public StringChunk(int outdent, int width)
            {
                _width = width;
                _spaces = new string(' ', outdent);
                _lines.Add(new StringBuilder());
            }

            public void Append(object value)
            {
                StringBuilder sb = _lines[_lines.Count - 1];
                sb.Append(value);
                if (sb.Length > _width)
                {
                    bool foundNonSpace = false;
                    for (int i = Math.Min(sb.Length - 1, _width); i >= 0; --i)
                    {
                        if (sb[i] == ' ' && foundNonSpace)
                        {
                            int wrapCount = sb.Length - i - 1;
                            StringBuilder nextBuilder = new StringBuilder(_spaces);
                            nextBuilder.Append(sb.ToString(), i + 1, wrapCount);
                            sb.Remove(i, wrapCount + 1);
                            _lines.Add(nextBuilder);
                            break;
                        }
                        foundNonSpace = true;
                    }
                }
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (StringBuilder line in _lines)
                {
                    if (sb.Length != 0)
                    {
                        sb.AppendLine();
                    }

                    sb.Append(line);
                }

                return sb.ToString();
            }
        }
    }
}