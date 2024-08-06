using Api.FunctionalTests.Contracts;
using Lodge.Contracts.Authentication;
using Lodge.Domain.Core.Guards;
using System.Net.Http.Json;

namespace Api.FunctionalTests.Extensions;

public static class HttpResponseMessageExtensions
{
    public static async Task<CustomProblemDetails> GetProblemDetailsAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Successful response");
        }

        CustomProblemDetails? problemDetails = await response
            .Content
            .ReadFromJsonAsync<CustomProblemDetails>();

        Ensure.NotNull(problemDetails);

        return problemDetails;
    }

    public static async Task<TokenResponse> GetTokenAsync(this HttpResponseMessage response)
    {
        TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>();

        Ensure.NotNull(token);

        return token;
    }
}
