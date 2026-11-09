FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ExceptionInspector.slnx Directory.Build.props global.json ./
COPY src/ExceptionInspector.Core/ExceptionInspector.Core.csproj src/ExceptionInspector.Core/
COPY src/ExceptionInspector.Api/ExceptionInspector.Api.csproj src/ExceptionInspector.Api/
RUN dotnet restore src/ExceptionInspector.Api/ExceptionInspector.Api.csproj

COPY src/ src/
RUN dotnet publish src/ExceptionInspector.Api/ExceptionInspector.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --property:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ExceptionInspector.Api.dll"]
