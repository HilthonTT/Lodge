﻿namespace Application.IntegrationTests.Abstractions;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestWebFactory>
{
}