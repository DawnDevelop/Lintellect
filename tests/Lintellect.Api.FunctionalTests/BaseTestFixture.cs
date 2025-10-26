using System;
using System.Collections.Generic;
using System.Text;

namespace Lintellect.Api.functionaltests;

using static Testing;

[TestFixture]
public abstract class BaseTestFixture
{
    [SetUp]
    public async Task TestSetUp()
    {
        await ResetStateAsync();
    }
}
