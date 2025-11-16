using static Lintellect.Api.FunctionalTests.Testing;

namespace Lintellect.Api.FunctionalTests;


[TestFixture]
public abstract class BaseTestFixture
{
    [SetUp]
    public async Task TestSetUp()
    {
        await ResetStateAsync();
    }
}
