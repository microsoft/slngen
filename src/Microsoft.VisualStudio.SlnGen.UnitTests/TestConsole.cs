// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    /// <summary>
    /// Represents an implementation of <see cref="IConsole" /> for unit tests to capture console output.
    /// </summary>
    public class TestConsole : IConsole
    {
        private readonly StringBuilder _error = new StringBuilder();
        private readonly List<string> _errorLines = new List<string>();
        private readonly StringBuilder _output = new StringBuilder();

        private readonly List<string> _outputLines = new List<string>();

        public TestConsole()
        {
            Error = new StringBuilderTextWriter(_error, _errorLines);

            Out = new StringBuilderTextWriter(_output, _outputLines);
        }

        public event ConsoleCancelEventHandler CancelKeyPress
        {
            add { }
            remove { }
        }

        public ConsoleColor BackgroundColor { get; set; }

        public TextWriter Error { get; set; }

        public IReadOnlyCollection<string> ErrorLines => _errorLines;

        public string ErrorOutput => _error.ToString();

        public string AllOutput => $"{Output}{Environment.NewLine}{ErrorOutput}";

        public ConsoleColor ForegroundColor { get; set; }

        public TextReader In => throw new NotImplementedException();

        public bool IsErrorRedirected => true;

        public bool IsInputRedirected => throw new NotImplementedException();

        public bool IsOutputRedirected => true;

        public TextWriter Out { get; set; }

        public string Output => _output.ToString();

        public IReadOnlyCollection<string> OutputLines => _outputLines;

        public void ResetColor()
        {
        }

        private class StringBuilderTextWriter : TextWriter
        {
            private readonly List<string> _lines;
            private readonly StringBuilder _lineStringBuilder = new StringBuilder();
            private readonly StringBuilder _stringBuilder;

            public StringBuilderTextWriter(StringBuilder stringBuilder, List<string> lines)
            {
                _stringBuilder = stringBuilder;
                _lines = lines;
            }

            public override Encoding Encoding => Encoding.Unicode;

            public override void Write(char value)
            {
                _lineStringBuilder.Append(value);

                _stringBuilder.Append(value);

                if (value == '\n')
                {
                    _lines.Add(_lineStringBuilder.ToString());

                    _lineStringBuilder.Clear();
                }
            }
        }
    }
}