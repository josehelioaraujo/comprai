FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 5030

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/UcpAgent.McpServer/UcpAgent.McpServer.csproj",       "src/UcpAgent.McpServer/"]
COPY ["src/UcpAgent.SharedKernel/UcpAgent.SharedKernel.csproj", "src/UcpAgent.SharedKernel/"]
RUN dotnet restore "src/UcpAgent.McpServer/UcpAgent.McpServer.csproj"
COPY . .
WORKDIR "/src/src/UcpAgent.McpServer"
RUN dotnet build "UcpAgent.McpServer.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UcpAgent.McpServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UcpAgent.McpServer.dll"]
