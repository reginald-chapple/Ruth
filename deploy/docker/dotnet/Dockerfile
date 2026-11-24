FROM mcr.microsoft.com/dotnet/sdk:10.0.404@sha256:de4fe90a98845d75910b9769662465c84de1a934665c431279d1382b2bddc02b AS build
WORKDIR /source
COPY Directory.Build.props Directory.Build.targets global.json Ruth.slnx ./
COPY src/ ./src/
RUN dotnet publish src/Ruth.Host/Ruth.Host.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0.15@sha256:de0ee358cdd672f7017a003bed21e9ac9ed48a27c922a59b18175b6e82fa7fee
WORKDIR /app
COPY --from=build /out/ ./
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Ruth.Host.dll"]
