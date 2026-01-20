using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;

namespace WCCG.eReferralsService.Unit.Tests.Extensions;

public static class AutoFixtureExtensions
{
    public static T CreateWithFrozen<T>(this IFixture fixture)
    {
        var constructorInfo = typeof(T).GetConstructors().Single();
        var parameterInfos = constructorInfo.GetParameters();
        foreach (var parameterInfo in parameterInfos)
        {
            var type = parameterInfo.ParameterType;
            var mockType = typeof(Mock<>).MakeGenericType(type);
            typeof(FixtureFreezer).GetMethod("Freeze", [typeof(IFixture)])!
                .MakeGenericMethod(mockType)
                .Invoke(null, [fixture]);
        }

        return fixture.Create<T>();
    }

    public static IFixture WithCustomizations(this IFixture fixture)
    {
        return fixture
            .Customize(new AutoMoqCustomization())
            .Customize(new OmitRecursionCustomization());
    }

    public static Mock<T> Mock<T>(this IFixture fixture) where T : class
    {
        return fixture.Freeze<Mock<T>>();
    }
}
