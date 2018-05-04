// Copyright (c) Jeff Kluge. All rights reserved.
//
// Licensed under the MIT license.

using SlnGen.Build.Tasks.Internal;
using System.IO;
using System.Text;

namespace SlnGen.Build.Tasks.UnitTests
{
    internal static class ExtensionMethods
    {
        public static string GetText(this SlnFile solutionFile)
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (StringWriter writer = new StringWriter(stringBuilder))
            {
                solutionFile.Save(writer);
            }

            return stringBuilder.ToString();
        }
    }
}