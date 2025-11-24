namespace Tests;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        var test = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    }
}
