FROM mcr.microsoft.com/dotnet/sdk:10.0.404@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /source
COPY Directory.Build.props Directory.Build.targets Directory.Packages.props CodeMetricsConfig.txt .editorconfig global.json Ruth.slnx ./
COPY src/ ./src/
RUN dotnet publish src/Ruth.Host/Ruth.Host.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble-chiseled-extra@sha256:c8dd08a50d8ffd19bd8da72edee05a7ac15b486a5b1869cf46fdb370bf1fbc76 AS runtime
LABEL org.opencontainers.image.title="Ruth Host" \
      org.opencontainers.image.description="Hardened multi-tenant nonprofit operations platform runtime" \
      org.opencontainers.image.vendor="Ruth"
WORKDIR /app
COPY --from=build /out/ ./
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TMPDIR=/tmp
EXPOSE 8080
USER $APP_UID
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD ["dotnet", "/app/Ruth.Host.dll", "--healthcheck"]
ENTRYPOINT ["dotnet", "/app/Ruth.Host.dll"]
