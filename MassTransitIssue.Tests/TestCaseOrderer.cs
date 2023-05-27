using Xunit.Abstractions;
using Xunit.Sdk;

namespace MassTransitIssue.Tests;

public class TestCaseOrdererAscending : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase => testCases.OrderBy(t => t.DisplayName);
}

public class TestCaseOrdererDescending : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase => testCases.OrderByDescending(t => t.DisplayName);
}
