#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["src/PQBI.Web.Public/PQBI.Web.Public.csproj", "src/PQBI.Web.Public/"]
COPY ["src/PQBI.Web.Core/PQBI.Web.Core.csproj", "src/PQBI.Web.Core/"]
COPY ["src/PQBI.Application/PQBI.Application.csproj", "src/PQBI.Application/"]
COPY ["src/PQBI.Application.Shared/PQBI.Application.Shared.csproj", "src/PQBI.Application.Shared/"]
COPY ["src/PQBI.Core.Shared/PQBI.Core.Shared.csproj", "src/PQBI.Core.Shared/"]
COPY ["src/PQBI.Core/PQBI.Core.csproj", "src/PQBI.Core/"]
COPY ["src/PQBI.EntityFrameworkCore/PQBI.EntityFrameworkCore.csproj", "src/PQBI.EntityFrameworkCore/"]
COPY ["src/PQBI.GraphQL/PQBI.GraphQL.csproj", "src/PQBI.GraphQL/"]
RUN dotnet restore "src/PQBI.Web.Public/PQBI.Web.Public.csproj"
COPY . .
WORKDIR "/src/src/PQBI.Web.Public"
RUN dotnet build "PQBI.Web.Public.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PQBI.Web.Public.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PQBI.Web.Public.dll"]
