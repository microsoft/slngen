// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.VisualStudio.SlnGen.Tasks
{
    /// <summary>
    /// Represents a task that logs an error from the specified resource string.
    /// </summary>
    public sealed class ErrorFromResources : Task
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorFromResources"/> class.
        /// </summary>
        public ErrorFromResources()
            : base(Strings.ResourceManager)
        {
        }

        /// <summary>
        /// Gets or sets optional arguments for formatting the loaded string.
        /// </summary>
        public string[] Args { get; set; }

        /// <summary>
        /// Gets or sets an optional error code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the name of the string resource containing the error message.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <inheritdoc/>
        public override bool Execute()
        {
            Log.LogErrorFromResources(
                subcategoryResourceName: null,
                errorCode: Code,
                helpKeyword: null,
                file: null,
                lineNumber: 0,
                columnNumber: 0,
                endLineNumber: 0,
                endColumnNumber: 0,
                messageResourceName: Name,
                messageArgs: Args);

            return false;
        }
    }
}