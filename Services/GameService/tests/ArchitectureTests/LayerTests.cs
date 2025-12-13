using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using Xunit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace GameService.Tests.ArchitectureTests;

/*
public class LayerTests
{
    private readonly Architecture _architecture;
    private readonly IObjectProvider<IType> _domainLayer;
    private readonly IObjectProvider<IType> _applicationLayer;

    public LayerTests()
    {
        var domainAssembly = System.Reflection.Assembly.Load("GameService.Core");
        var applicationAssembly = System.Reflection.Assembly.Load("GameService.Application");

        _architecture = new ArchLoader().LoadAssemblies(
            domainAssembly,
            applicationAssembly).Build();

        _domainLayer = Types().That().ResideInAssembly(domainAssembly).As("Domain Layer");
        _applicationLayer = Types().That().ResideInAssembly(applicationAssembly).As("Application Layer");
    }

    [Fact]
    public void DomainLayer_Should_NotDependOnAnyOtherLayer()
    {
        Types().That().Are(_domainLayer).Should().NotDependOnAny(_applicationLayer).Check(_architecture);
    }
}
*/
