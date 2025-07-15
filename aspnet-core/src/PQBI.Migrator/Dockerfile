#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/PQBI.Migrator/PQBI.Migrator.csproj", "src/PQBI.Migrator/"]
COPY ["src/PQBI.EntityFrameworkCore/PQBI.EntityFrameworkCore.csproj", "src/PQBI.EntityFrameworkCore/"]
COPY ["src/PQBI.Core/PQBI.Core.csproj", "src/PQBI.Core/"]
COPY ["src/PQBI.Core.Shared/PQBI.Core.Shared.csproj", "src/PQBI.Core.Shared/"]
RUN dotnet restore "src/PQBI.Migrator/PQBI.Migrator.csproj"
COPY . .
WORKDIR "/src/src/PQBI.Migrator"
RUN dotnet build "PQBI.Migrator.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PQBI.Migrator.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PQBI.Migrator.dll", "-s"]
