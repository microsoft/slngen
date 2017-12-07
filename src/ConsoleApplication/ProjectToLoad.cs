using System;
using System.Collections.Generic;

namespace SlnGen
{
    internal sealed class ProjectToLoad
    {
        private static readonly char[] KeyValueSeparator = { '=' };
        private static readonly char[] PropertyDefinitionSeparator = { ';' };

        public ProjectToLoad(string fullPath, string additionalProperties, int level)
        {
            FullPath = fullPath;
            AdditionalProperties = ParseProperties(additionalProperties);
            Level = level;
        }

        public IDictionary<string, string> AdditionalProperties { get; }

        public string FullPath { get; }

        public int Level { get; }

        /// <remarks>
        /// Matches MSBuild parsing behavior
        ///
        /// For example following project:
        ///     &lt;Target Name="Build"&gt;
        ///         &lt;Message Text="Foo='$(Foo)'" /&gt;
        ///         &lt;Message Text="Bar='$(Bar)'" /&gt;
        ///         &lt;Message Text="Baz='$(Baz)'" /&gt;
        ///         &lt;MSBuild Projects="$(MSBuildThisFileFullPath)" Properties="Stop=true; Foo = Bar; Baz= Humf    ;;;Bar=
        ///     BAR" Condition="'$(Stop)'==''"/&gt;
        ///     &lt;/Target&gt;
        ///
        /// Returns:
        ///
        ///     Foo='Bar'
        ///     Bar='BAR'
        ///     Baz='Humf'
        /// </remarks>
        private IDictionary<string, string> ParseProperties(string properties)
        {
            if (String.IsNullOrEmpty(properties))
            {
                return null;
            }

            Dictionary<string, string> result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            // split on ;
            foreach (string propertiesToken in properties.Split(PropertyDefinitionSeparator))
            {
                // case where props="p1=v1;   ;  ;p2=v2"
                if (String.IsNullOrWhiteSpace(propertiesToken))
                {
                    continue;
                }

                // split on =
                string[] propertyTokens = propertiesToken.Split(KeyValueSeparator);

                // case where props="p1=v1;WAT;p2=v2" or "p1=v1;foo=bar=WAT;p2=v2"
                // or
                // case where props="p1=v1;=WAT;p2=v2"
                if (propertyTokens.Length != 2 || String.IsNullOrWhiteSpace(propertyTokens[0]))
                {
                    SlnError.ReportError(SlnError.ErrorId.BadAdditionalProperties, FullPath, properties);
                    return null;
                }
                // trim on both sides for both key and value; case where props="p1=   v1 ;   p2   =   v2   ".
                result[propertyTokens[0].Trim()] = propertyTokens[1].Trim();
            }
            return result;
        }
    }
}