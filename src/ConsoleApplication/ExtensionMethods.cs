using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SlnGen
{
    internal static class ExtensionMethods
    {
        private static readonly char[] QuoteChars = { '"' };

        public static List<string> ExtractQuoted(this string toExtract)
        {
            // TODO: Return IEnumerable
            List<string> result = new List<string>();

            if (String.IsNullOrEmpty(toExtract))
            {
                return result;
            }

            string[] quotedSplit = toExtract.Split(QuoteChars);

            for (int i = 0; i < quotedSplit.Length; i++)
            {
                // if we've seen an even number of quotes, we are outside of a quoted region otherwise, we are inside. If we are outside, split on spaces.
                if (i % 2 == 0)
                {
                    result.AddRange(quotedSplit[i].Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
                }
                else
                {
                    result.Add(quotedSplit[i]);
                }
            }

            return result;
        }

        public static bool IsFatal(this Exception exception)
        {
            // The following logic was inspired by System.Runtime.Fx.IsFatal() in System.ServiceModel.Internals.dll.
            while (exception != null)
            {
                if (exception is OutOfMemoryException && !(exception is InsufficientMemoryException) || exception is ThreadAbortException)
                {
                    return true;
                }

                if (exception is AggregateException aggregate)
                {
                    return aggregate.InnerExceptions.Any(inner => inner.IsFatal());
                }
                exception = exception.InnerException;
            }
            return false;
        }
    }
}
