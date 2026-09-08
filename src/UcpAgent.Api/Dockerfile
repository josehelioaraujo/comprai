FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5020
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
ARG BUILD_VERSION=1.0.0
ARG BUILD_COMMIT=local
ARG BUILD_DATE=unknown
WORKDIR /src
COPY ["src/UcpAgent.Api/UcpAgent.Api.csproj", "src/UcpAgent.Api/"]
COPY ["src/UcpAgent.Application/UcpAgent.Application.csproj", "src/UcpAgent.Application/"]
COPY ["src/UcpAgent.Domain/UcpAgent.Domain.csproj", "src/UcpAgent.Domain/"]
COPY ["src/UcpAgent.SharedKernel/UcpAgent.SharedKernel.csproj", "src/UcpAgent.SharedKernel/"]
COPY ["src/UcpAgent.Infrastructure/UcpAgent.Infrastructure.csproj", "src/UcpAgent.Infrastructure/"]
COPY ["src/plugins/UcpAgent.Catalog.MercadoLivre/UcpAgent.Catalog.MercadoLivre.csproj", "src/plugins/UcpAgent.Catalog.MercadoLivre/"]
COPY ["src/plugins/UcpAgent.Catalog.VtexCatalog/UcpAgent.Catalog.VtexCatalog.csproj", "src/plugins/UcpAgent.Catalog.VtexCatalog/"]
COPY ["src/plugins/UcpAgent.Catalog.VtexSearch/UcpAgent.Catalog.VtexSearch.csproj", "src/plugins/UcpAgent.Catalog.VtexSearch/"]
COPY ["src/plugins/UcpAgent.Catalog.OpenFoodFacts/UcpAgent.Catalog.OpenFoodFacts.csproj", "src/plugins/UcpAgent.Catalog.OpenFoodFacts/"]
COPY ["src/plugins/UcpAgent.Catalog.MercadoLivreOrders/UcpAgent.Catalog.MercadoLivreOrders.csproj", "src/plugins/UcpAgent.Catalog.MercadoLivreOrders/"]
COPY ["src/plugins/UcpAgent.Catalog.Shopify/UcpAgent.Catalog.Shopify.csproj", "src/plugins/UcpAgent.Catalog.Shopify/"]
COPY ["src/plugins/UcpAgent.FakeCatalog/UcpAgent.FakeCatalog.csproj", "src/plugins/UcpAgent.FakeCatalog/"]
COPY ["src/plugins/UcpAgent.PriceWatcher/UcpAgent.PriceWatcher.csproj", "src/plugins/UcpAgent.PriceWatcher/"]
RUN dotnet restore "src/UcpAgent.Api/UcpAgent.Api.csproj"
COPY . .
WORKDIR "/src/src/UcpAgent.Api"
RUN dotnet build "UcpAgent.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build
FROM build AS publish
RUN dotnet publish "UcpAgent.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false \
    /p:Version=$BUILD_VERSION \
    /p:InformationalVersion="$BUILD_VERSION+$BUILD_COMMIT"
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UcpAgent.Api.dll"]