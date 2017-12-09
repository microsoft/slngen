using System.IO;
using System.Text;
using SlnGen.Build.Tasks.Internal;

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