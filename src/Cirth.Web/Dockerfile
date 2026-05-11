FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Directory.Build.props .
COPY Cirth.Shared/Cirth.Shared.csproj Cirth.Shared/
COPY Cirth.Domain/Cirth.Domain.csproj Cirth.Domain/
COPY Cirth.Application/Cirth.Application.csproj Cirth.Application/
COPY Cirth.Infrastructure/Cirth.Infrastructure.csproj Cirth.Infrastructure/
COPY Cirth.Web/Cirth.Web.csproj Cirth.Web/

RUN dotnet restore Cirth.Web/Cirth.Web.csproj

COPY Cirth.Shared/ Cirth.Shared/
COPY Cirth.Domain/ Cirth.Domain/
COPY Cirth.Application/ Cirth.Application/
COPY Cirth.Infrastructure/ Cirth.Infrastructure/
COPY Cirth.Web/ Cirth.Web/

RUN dotnet publish Cirth.Web/Cirth.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -fsS http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Cirth.Web.dll"]
