// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VerifierFacts.cs" company="Appccelerate">
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
    using System.Xml.Linq;
    using FakeItEasy;
    using FluentAssertions;
    using Xunit;

    public class VerifierFacts
    {
        private const string ProjectFolder = "c:\\folder\\";

        private readonly Verifier testee;
        private readonly IFileVerifier fileVerifier;

        public VerifierFacts()
        {
            this.fileVerifier = A.Fake<IFileVerifier>();
            A.CallTo(() => this.fileVerifier.DoesFileExist(A<string>._)).Returns(true);

            this.testee = new Verifier(this.fileVerifier);
        }

        [Fact]
        public void ReturnsNoViolation_WhenThereAreNoReferences()
        {
            XDocument projectFile = ProjectBuilder
                .Create()
                .Build();

            var result = this.testee.Verify(projectFile, ProjectFolder, IgnoreExcludedPrefix, IgnoreHintPathPrefix);

            result.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsNoViolation_WhenAllReferencesHaveHintPathsStartingWithKnownPrefixAndContainingReferenceId()
        {
            const string Reference = "Foo";
            const string Prefix = "..\\";
            const string HintPath = Prefix + Reference + "\\reference.dll";

            XDocument projectFile = ProjectBuilder
                .Create()
                .WithReferences()
                .AddReference(Reference, HintPath)
                .Build();

            IReadOnlyCollection<Violation> result = this.testee.Verify(projectFile, ProjectFolder, IgnoreExcludedPrefix, HintPathPrefixes(Prefix));

            result.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsNoViolation_WhenReferenceIsExcluded()
        {
            const string ReferencePrefix = "Foo.";
            const string Reference = ReferencePrefix + "Bar";
            
            XDocument projectFile = ProjectBuilder
                .Create()
                .WithReferences()
                .AddReference(Reference)
                .Build();

            IReadOnlyCollection<Violation> result = this.testee.Verify(
                projectFile, 
                ProjectFolder, 
                ExcludedReferencePrefixes(ReferencePrefix),
                IgnoreHintPathPrefix);

            result.Should().BeEmpty();
        }

        [Fact]
        public void ReturnsViolation_WhenHintPathIsMissing()
        {
            const string Reference = "Foo";

            XDocument projectFile = ProjectBuilder
                .Create()
                    .WithReferences()
                        .AddReference(Reference)
                .Build();

            IReadOnlyCollection<Violation> result = this.testee.Verify(projectFile, ProjectFolder, IgnoreExcludedPrefix, IgnoreHintPathPrefix);

            result.Should().BeEquivalentTo(new Violation(Reference, null, Verifier.MissingHintPath));
        }

        [Fact]
        public void ReturnsViolation_WhenReferenceIdIsNotInHintPath()
        {
            const string Reference = "Foo";
            const string HintPath = "..\\folder\\reference.dll";

            XDocument projectFile = ProjectBuilder
                .Create()
                .WithReferences()
                .AddReference(Reference, HintPath)
                .Build();

            IReadOnlyCollection<Violation> result = this.testee.Verify(projectFile, ProjectFolder, IgnoreExcludedPrefix, IgnoreHintPathPrefix);

            result.Should().BeEquivalentTo(new Violation(Reference, HintPath, Verifier.HintPathDoesNotContainReferenceId));
        }

        [Fact]
        public void ReturnsViolation_WhenHintPathWithWrongPrefix()
        {
            const string Reference = "Foo";
            const string HintPath = "..\\notAllowed\\" + Reference + "\\reference.dll";

            XDocument projectFile = ProjectBuilder
                .Create()
                .WithReferences()
                .AddReference(Reference, HintPath)
                .Build();

            IReadOnlyCollection<Violation> result = this.testee.Verify(projectFile, ProjectFolder, IgnoreExcludedPrefix, HintPathPrefixes("..\\Allowed"));

            result.Should().BeEquivalentTo(new Violation(Reference, HintPath, Verifier.HintPathWithWrongPrefix));
        }

        [Fact]
        public void ReturnsViolation_WhenHintPathDoesNotExistOnFileSystem()
        {
            const string Reference = "Foo";
            const string HintPath = "..\\" + Reference + "\\reference.dll";

            A.CallTo(() => this.fileVerifier.DoesFileExist(ProjectFolder + HintPath)).Returns(false);

            XDocument projectFile = ProjectBuilder
                .Create()
                .WithReferences()
                .AddReference(Reference, HintPath)
                .Build();

            IReadOnlyCollection<Violation> result = this.testee.Verify(projectFile, ProjectFolder, IgnoreExcludedPrefix, IgnoreHintPathPrefix);

            result.Should().BeEquivalentTo(new Violation(Reference, HintPath, Verifier.HintPathDoesNotExistOnFileSystem));
        }

        private static IReadOnlyCollection<string> IgnoreExcludedPrefix
        {
            get { return new List<string> { "SomePrefixThatIsNeverUsedInAnyFact" }; }
        }

        private static IReadOnlyCollection<string> ExcludedReferencePrefixes(params string[] prefixes)
        {
            return prefixes;
        }

        private static IReadOnlyCollection<string> IgnoreHintPathPrefix
        {
            get { return new List<string> { string.Empty }; }
        }

        private static IReadOnlyCollection<string> HintPathPrefixes(params string[] prefixes)
        {
            return prefixes;
        }
    }
}