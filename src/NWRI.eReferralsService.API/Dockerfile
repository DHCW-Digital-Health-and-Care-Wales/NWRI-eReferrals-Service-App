FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["./WCCG.eReferralsService.API.csproj", "WCCG.eReferralsService.API/"]
RUN dotnet restore "./WCCG.eReferralsService.API/WCCG.eReferralsService.API.csproj"
COPY . "./WCCG.eReferralsService.API"
WORKDIR "/src/WCCG.eReferralsService.API"
RUN dotnet build "./WCCG.eReferralsService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WCCG.eReferralsService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WCCG.eReferralsService.API.dll"]
