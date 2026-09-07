using Xunit.Abstractions;
using Xunit.Sdk;

namespace UcpAgent.Integration.Tests;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class TestPriorityAttribute : Attribute
{
    public int Priority { get; }
    public TestPriorityAttribute(int priority) => Priority = priority;
}

public class PriorityOrderer : ITestCaseOrderer
{
    public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
    {
        return testCases.OrderBy(tc =>
            tc.TestMethod.Method
              .GetCustomAttributes(typeof(TestPriorityAttribute).AssemblyQualifiedName!)
              .FirstOrDefault()
              ?.GetNamedArgument<int>("Priority") ?? 0);
    }
}
