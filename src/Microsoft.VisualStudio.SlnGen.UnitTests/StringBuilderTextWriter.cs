// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    /// <summary>
    /// Extends <see cref="TextWriter" /> for unit tests to capture writing text.
    /// </summary>
    public class StringBuilderTextWriter : TextWriter
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