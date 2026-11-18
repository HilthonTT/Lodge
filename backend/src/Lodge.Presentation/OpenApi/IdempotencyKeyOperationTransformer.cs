using Lodge.Presentation.Infrastructure;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Lodge.Presentation.OpenApi;

/// <summary>
/// Documents the <c>X-Idempotency-Key</c> header as a required GUID. Endpoints bind it as an optional string so that
/// they can answer a missing or malformed key with <c>400 Request.InvalidIdempotencyKey</c>.
/// </summary>
internal sealed class IdempotencyKeyOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (IOpenApiParameter parameter in operation.Parameters ?? [])
        {
            if (parameter is OpenApiParameter { In: ParameterLocation.Header } header &&
                string.Equals(header.Name, IdempotencyKey.HeaderName, StringComparison.OrdinalIgnoreCase))
            {
                header.Required = true;
                header.Description = "A GUID identifying the user action; a retry with the same key replays the first result.";
                header.Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" };
            }
        }

        return Task.CompletedTask;
    }
}
