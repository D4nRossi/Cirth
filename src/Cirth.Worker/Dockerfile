FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY Directory.Build.props .
COPY Cirth.Shared/Cirth.Shared.csproj Cirth.Shared/
COPY Cirth.Domain/Cirth.Domain.csproj Cirth.Domain/
COPY Cirth.Application/Cirth.Application.csproj Cirth.Application/
COPY Cirth.Infrastructure/Cirth.Infrastructure.csproj Cirth.Infrastructure/
COPY Cirth.Worker/Cirth.Worker.csproj Cirth.Worker/

RUN dotnet restore Cirth.Worker/Cirth.Worker.csproj

COPY Cirth.Shared/ Cirth.Shared/
COPY Cirth.Domain/ Cirth.Domain/
COPY Cirth.Application/ Cirth.Application/
COPY Cirth.Infrastructure/ Cirth.Infrastructure/
COPY Cirth.Worker/ Cirth.Worker/

RUN dotnet publish Cirth.Worker/Cirth.Worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Cirth.Worker.dll"]
