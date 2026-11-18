namespace Lodge.Api.FunctionalTests.Abstractions;

[CollectionDefinition(nameof(FunctionalTestCollection))]
public sealed class FunctionalTestCollection : ICollectionFixture<FunctionalTestWebAppFactory>;
