using EnterpriseAgenticRag.Application;

namespace EnterpriseAgenticRag.UnitTests;

public sealed class QueryPlannerTests
{
    [Fact]
    public async Task PlanAsyncBoundsDecomposedQueriesToThree()
    {
        var planner = new BoundedQueryPlanner();
        IReadOnlyList<string> queries = await planner.PlanAsync(
            "What is remote work and what is the hardware budget and also what VPN is required and then who approves it?",
            CancellationToken.None);

        Assert.Equal(3, queries.Count);
    }
}
