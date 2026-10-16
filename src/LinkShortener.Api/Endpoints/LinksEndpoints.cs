using LinkShortener.Api.Application;

namespace LinkShortener.Api.Endpoints;

public static class LinksEndpoints
{
    public static IEndpointRouteBuilder MapLinksEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(
            "/api/links",
            async (
                CreateLinkRequest request,
                LinkService linkService,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await linkService.CreateAsync(request.TargetUrl, cancellationToken);

                return result switch
                {
                    LinkCreationResult.Created created => Results.Created(
                        $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{created.Link.Code}",
                        new CreateLinkResponse(
                            $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{created.Link.Code}",
                            created.Link.Code,
                            created.ManagementKey)),
                    LinkCreationResult.InvalidTargetUrl => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "URL de destino inválida",
                        detail: "A URL de destino deve ser um endereço HTTP ou HTTPS válido com até 2048 caracteres."),
                    _ => throw new InvalidOperationException("Resultado de criação de link não reconhecido.")
                };
            });

        return endpoints;
    }

    private sealed record CreateLinkRequest(string? TargetUrl);

    private sealed record CreateLinkResponse(string ShortUrl, string Code, string ManagementKey);
}
