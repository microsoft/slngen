// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System;
using System.IO;

namespace Microsoft.VisualStudio.SlnGen.UnitTests
{
    internal class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            string tempPath = Path.GetTempPath();

            string tempFileName = Path.GetTempFileName();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(tempFileName);

            this.DirectoryPath = Path.Combine(tempPath, fileNameWithoutExtension);
            File.Delete(tempFileName);
            Directory.CreateDirectory(this.DirectoryPath);
        }

        /// <summary>
        /// Gets the directory path.
        /// </summary>
        public string DirectoryPath { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(this.DirectoryPath))
                {
                    Directory.Delete(this.DirectoryPath, true);
                }
            }
            catch (Exception ex)
            {
                // do not raise exceptions because this is called in a finalizer and may swallow other exceptions
                Console.Error.WriteLine($"Failed to delete directory: {this.DirectoryPath}\r\n{ex}");
            }
        }

        public string GetTempFileName(string extension = null)
        {
            return Path.Combine(this.DirectoryPath, $"{Path.GetRandomFileName()}{extension ?? string.Empty}");
        }
    }
}
