using System.Net.Http.Json;
using Lodge.Api.FunctionalTests.Contracts;
using Lodge.Contracts.Authentication;

namespace Lodge.Api.FunctionalTests.Extensions;

public static class HttpResponseMessageExtensions
{
    public static async Task<CustomProblemDetails> GetProblemDetailsAsync(this HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Successful response");
        }

        CustomProblemDetails? problemDetails = await response.Content.ReadFromJsonAsync<CustomProblemDetails>();

        return problemDetails ?? throw new InvalidOperationException("The response has no problem details.");
    }

    public static async Task<TokenResponse> GetTokenAsync(this HttpResponseMessage response)
    {
        TokenResponse? token = await response.Content.ReadFromJsonAsync<TokenResponse>();

        return token ?? throw new InvalidOperationException("The response has no token.");
    }
}
