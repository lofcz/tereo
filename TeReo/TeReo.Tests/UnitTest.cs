using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing.NUnit;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TeReo.Analyzer;

namespace TeReo.Tests;

[TestClass]
public class UnitTest : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new MyAnalyzer();
    }

    [TestMethod]
    public void StringComparisonIsMissing_ShouldReportDiagnostic()
    {
        var test = @"
class TypeName
{
    public void Test()
    {
        var a = ""test"";
        System.String.Equals(a, ""v"");
    }
}";
        var expected = new DiagnosticResult
        {
            Id = "SampleAnalyzer",
            Message = "StringComparison is missing",
            Severity = DiagnosticSeverity.Warning,
            Locations =
                new[]
                {
                    // Test0.cs is the name of the file created by VerifyCSharpDiagnostic
                    new DiagnosticResultLocation("Test0.cs", line: 7, column: 9)
                }
        };

        VerifyCSharpDiagnostic(test, expected);
    }
}