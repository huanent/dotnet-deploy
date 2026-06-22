using DotnetDeploy.Infrastructure;
using DotnetDeploy.Projects;

namespace DotnetDeployTest;

[TestClass]
public class DeployOptionsTests
{
    [TestMethod]
    public void Get_ShouldUseConfiguredIncludeFiles_WhenCliOptionIsNotProvided()
    {
        var command = new PublishCommand();
        var parseResult = command.Parse([]);
        var options = new DeployOptions
        {
            IncludeFiles = ["appsettings.Production.json"]
        };

        var result = options.Get(null, parseResult);

        CollectionAssert.AreEqual(new[] { "appsettings.Production.json" }, result.IncludeFiles);
    }

    [TestMethod]
    public void Get_ShouldUseCliIncludeFiles_WhenCliOptionIsProvided()
    {
        var command = new PublishCommand();
        var parseResult = command.Parse(["--include-files", "A.txt"]);
        var options = new DeployOptions
        {
            IncludeFiles = ["appsettings.Production.json"]
        };

        var result = options.Get(null, parseResult);

        CollectionAssert.AreEqual(new[] { "A.txt" }, result.IncludeFiles);
    }
}
