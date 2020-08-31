// Copyright (c) Microsoft Corporation.
//
// Licensed under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Microsoft.VisualStudio.SlnGen
{
    /// <summary>
    /// Represents a Visual Studio (c) Solution Filter file.
    /// </summary>
    public class SlnfFile
    {
        /// <summary>
        /// Gets or sets a <see cref="ICollection{String}" /> of projects to add to the solution filter.
        /// </summary>
        public ICollection<string> Projects { get; set; }

        /// <summary>
        /// Gets or sets the full path to the Visual Studio (c) Solution file.
        /// </summary>
        public string SolutionFilePath { get; set; }

        /// <summary>
        /// Saves the current Solution filter file to the specified path.
        /// </summary>
        /// <param name="path">The path to save the solution filter file to.</param>
        public void Save(string path)
        {
            using (FileStream stream = File.Create(path))
            {
                Save(stream);
            }
        }

        /// <summary>
        /// Saves the current Solution filter file to the specified stream.
        /// </summary>
        /// <param name="stream">The <see cref="Stream" /> to save the Solution filter to.</param>
        public void Save(Stream stream)
        {
            using (XmlDictionaryWriter writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, ownsStream: false, indent: true))
            {
                new XDocument(
                        new XElement(
                            "root",
                            new XAttribute("type", "object"),
                            new XElement(
                                "solution",
                                new XAttribute("type", "object"),
                                new XElement(
                                    "path",
                                    new XText(SolutionFilePath)),
                                new XElement(
                                    "projects",
                                    new XAttribute("type", "array"),
                                    Projects.Select(i => new XElement("item", i))))))
                    .Save(writer);
            }
        }
    }
}