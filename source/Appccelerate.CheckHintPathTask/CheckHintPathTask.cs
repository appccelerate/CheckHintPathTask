// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CheckHintPathTask.cs" company="Appccelerate">
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
    using Microsoft.Build.Framework;
    using Microsoft.Build.Utilities;

    public class CheckHintPathTask : Task, IFileVerifier
    {
        [Required]
        public string ProjectFileFullPath { get; set; }

        [Required]
        public string ProjectFolder { get; set; }

        [Required]
        public string ExcludedReferencePrefixes { get; set; }

        [Required]
        public string KnownHintPathPrefixes { get; set; }

        [Required]
        public bool TreatWarningsAsErrors { get; set; }

        public override bool Execute()
        {
            var projectFile = XDocument.Load(this.ProjectFileFullPath);

            var verifier = new Verifier(this);

            var excludedReferencePrefixes = string.IsNullOrWhiteSpace(this.ExcludedReferencePrefixes)
                ? Enumerable.Empty<string>()
                : this.ExcludedReferencePrefixes.Split(',').Select(x => x.Trim());

            var knownHintPathPrefixes = string.IsNullOrWhiteSpace(this.KnownHintPathPrefixes)
                ? Enumerable.Empty<string>()
                : this.KnownHintPathPrefixes.Split(',').Select(x => x.Trim());

            IReadOnlyCollection<Violation> violations = verifier.Verify(
                projectFile,
                this.ProjectFolder,
                excludedReferencePrefixes.ToList(),
                knownHintPathPrefixes.ToList());

            foreach (Violation violation in violations)
            {
                this.LogViolation(violation, this.ProjectFileFullPath);
            }

            bool continueBuild = !(violations.Any() && this.TreatWarningsAsErrors);
            return continueBuild;
        }

        public bool DoesFileExist(string path)
        {
            return File.Exists(path);
        }

        private void LogViolation(Violation violation, string projectFile)
        {
            string message = 
                violation.Message 
                + " in .csproj " 
                + projectFile 
                + " for reference " 
                + violation.Reference 
                + " and HintPath " 
                + violation.HintPath
                + ". ProjectFolder "
                + this.ProjectFolder
                + ". ExcludedReferencePrefixes"
                + this.ExcludedReferencePrefixes
                + ".  KnownHintPathPrefixes = "
                + this.KnownHintPathPrefixes;

            if (this.TreatWarningsAsErrors)
            {
                this.Log.LogError(message);
            }
            else
            {
                this.Log.LogWarning(message);
            }
        }
    }
}