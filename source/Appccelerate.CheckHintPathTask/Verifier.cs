// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Verifier.cs" company="Appccelerate">
//   Copyright (c) 2008-2014
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Appccelerate.CheckHintPathTask
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    public class Verifier
    {
        public const string MissingHintPath = "missing HintPath";
        public const string HintPathDoesNotContainReferenceId = "HintPath does not contain reference id";
        public const string HintPathWithWrongPrefix = "HintPath does not start with known prefix";
        public const string HintPathDoesNotExistOnFileSystem = "the file referenced by the HintPath does not exist";
        
        private readonly IFileVerifier fileVerifier;

        public Verifier(IFileVerifier fileVerifier)
        {
            this.fileVerifier = fileVerifier;
        }

        public IReadOnlyCollection<Violation> Verify(
            XDocument projectFile, 
            string projectFolder,
            IReadOnlyCollection<string> excludedPrefixes,
            IReadOnlyCollection<string> knownHintPathPrefixes)
        {
            var violations = new List<Violation>();

            IEnumerable<XElement> references = GetReferences(projectFile);
            foreach (XElement reference in references)
            {
                string hintPath = reference.GetHintPath();
                string referenceId = reference.GetAttributeValue("Include");

                violations.AddRange(this.CheckHintPath(projectFolder, excludedPrefixes, knownHintPathPrefixes, hintPath, referenceId));
            }

            return violations;
        }

        private IEnumerable<Violation> CheckHintPath(string projectFolder, IReadOnlyCollection<string> excludedPrefixes, IReadOnlyCollection<string> knownHintPathPrefixes, string hintPath, string id)
        {
            var violations = new List<Violation>();

            if (IsExcludedReference(excludedPrefixes, id))
            {
                return violations;
            }

            if (hintPath == null)
            {
                violations.Add(new Violation(id, null, MissingHintPath));
                return violations;
            }

            violations.AddRange(this.CheckCorrectHintPathPrefix(knownHintPathPrefixes, hintPath, id));
            violations.AddRange(this.CheckHintPathContainsReferenceId(hintPath, id));
            violations.AddRange(this.CheckHintPathExistsOnFileSystem(projectFolder, hintPath, id));
            
            return violations;
        }

        private static bool IsExcludedReference(IReadOnlyCollection<string> excludedPrefixes, string id)
        {
            return excludedPrefixes.Any(id.StartsWith);
        }

        private static IEnumerable<XElement> GetReferences(XDocument projectFile)
        {
            XNamespace ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");

            IEnumerable<XElement> references = projectFile.Descendants(ns + "Reference");
            return references;
        }

        private IEnumerable<Violation> CheckCorrectHintPathPrefix(IReadOnlyCollection<string> knownHintPathPrefixes, string hintPath, string id)
        {
            if (knownHintPathPrefixes.All(prefix => !hintPath.StartsWith(prefix)))
            {
                yield return new Violation(id, hintPath, HintPathWithWrongPrefix);
            }
        }

        private IEnumerable<Violation> CheckHintPathContainsReferenceId(string hintPath, string id)
        {
            if (!hintPath.Contains(id))
            {
                yield return new Violation(id, hintPath, HintPathDoesNotContainReferenceId);
            }
        }

        private IEnumerable<Violation> CheckHintPathExistsOnFileSystem(string projectFolder, string hintPath, string id)
        {
            if (!this.fileVerifier.DoesFileExist(Path.Combine(projectFolder, hintPath)))
            {
                yield return new Violation(id, hintPath, HintPathDoesNotExistOnFileSystem);
            }
        }
    }
}