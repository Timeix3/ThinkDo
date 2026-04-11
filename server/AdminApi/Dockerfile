FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src


COPY Common/Common.csproj Common/
COPY AdminApi/AdminApi.csproj AdminApi/
RUN dotnet restore AdminApi/AdminApi.csproj

COPY Common/ Common/
COPY AdminApi/ AdminApi/

RUN dotnet publish AdminApi/AdminApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8000

ENTRYPOINT ["dotnet", "AdminApi.dll"]