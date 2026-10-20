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
                var result = await linkService.CreateAsync(
                    request.TargetUrl,
                    request.Code,
                    cancellationToken);

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
                    LinkCreationResult.InvalidCode => Results.Problem(
                        statusCode: StatusCodes.Status400BadRequest,
                        title: "Código inválido",
                        detail: "O código deve ter de 4 a 20 caracteres e conter apenas letras maiúsculas de A a Z e números de 1 a 9."),
                    LinkCreationResult.DuplicateCode => Results.Problem(
                        statusCode: StatusCodes.Status409Conflict,
                        title: "Código já está em uso",
                        detail: "Escolha um código diferente."),
                    _ => throw new InvalidOperationException("Resultado de criação de link não reconhecido.")
                };
            });

        endpoints.MapGet(
            "/{code}",
            async (string code, LinkService linkService, CancellationToken cancellationToken) =>
            {
                var result = await linkService.GetRedirectAsync(code, cancellationToken);

                return result switch
                {
                    LinkRedirectResult.Redirect redirect => Results.Redirect(redirect.Link.TargetUrl),
                    LinkRedirectResult.NotFound => Results.NotFound(),
                    LinkRedirectResult.Expired => Results.StatusCode(StatusCodes.Status410Gone),
                    _ => throw new InvalidOperationException("Resultado de redirecionamento de link não reconhecido.")
                };
            });

        endpoints.MapGet(
            "/api/links/{code}/stats",
            async (
                string code,
                LinkService linkService,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var managementKeyHeader = httpContext.Request.Headers["X-Management-Key"];

                var managementKey = managementKeyHeader.Count == 1
                    ? managementKeyHeader[0]
                    : null;

                var result = await linkService.GetStatisticsAsync(
                    code,
                    managementKey,
                    cancellationToken);

                return result switch
                {
                    LinkStatisticsResult.Authorized authorized => Results.Ok(
                        new LinkStatisticsResponse(authorized.Code, authorized.AccessCount)),
                    LinkStatisticsResult.Unauthorized => Results.Unauthorized(),
                    LinkStatisticsResult.NotFound => Results.NotFound(),
                    _ => throw new InvalidOperationException("Resultado de estatísticas de link não reconhecido.")
                };
            });

        return endpoints;
    }

    private sealed record CreateLinkRequest(string? TargetUrl, string? Code);

    private sealed record CreateLinkResponse(string ShortUrl, string Code, string ManagementKey);

    private sealed record LinkStatisticsResponse(string Code, long AccessCount);
}
