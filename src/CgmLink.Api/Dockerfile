FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN groupadd -r appgroup && useradd -r -g appgroup appuser
RUN chown -R appuser:appgroup /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["CgmLink.Api/CgmLink.Api.csproj", "CgmLink.Api/"]
RUN dotnet restore "CgmLink.Api/CgmLink.Api.csproj"
COPY ["CgmLink.Api/", "CgmLink.Api/"]
COPY ["CgmLink.Data.Migrators.MSSQL/", "CgmLink.Data.Migrators.MSSQL/"]
COPY ["CgmLink.Data/", "CgmLink.Data/"]
COPY ["CgmLink.Identity/", "CgmLink.Identity/"]
COPY ["CgmLink.AspNetCore", "CgmLink.AspNetCore"]
COPY ["CgmLink.Mail", "CgmLink.Mail"]
COPY ["CgmLink.LibreLinkClient", "CgmLink.LibreLinkClient"]
COPY ["CgmLink.Sync.LibreLink", "CgmLink.Sync.LibreLink"]
COPY ["CgmLink.Resources", "CgmLink.Resources"]
COPY ["CgmLink.Nutrition", "CgmLink.Nutrition"]
COPY ["CgmLink.Nutrition.Data", "CgmLink.Nutrition.Data"]
WORKDIR "/src/CgmLink.Api"
RUN dotnet build "CgmLink.Api.csproj" -c "$BuildConfiguration" -o /app/build

FROM build AS publish
ARG BuildConfiguration=Release
RUN dotnet publish "CgmLink.Api.csproj" -c "$BuildConfiguration" -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER appuser

ENTRYPOINT ["dotnet", "CgmLink.Api.dll"]