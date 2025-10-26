
using static Lintellect.Api.functionaltests.Testing;

namespace Lintellect.Api.functionaltests;

[TestFixture]
public abstract class BaseTestFixture
{
    [SetUp]
    public async Task TestSetUp()
    {
        await ResetStateAsync();
    }
}
