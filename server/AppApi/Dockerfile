FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Common/Common.csproj Common/
COPY AppApi/AppApi.csproj AppApi/
RUN dotnet restore AppApi/AppApi.csproj

COPY Common/ Common/
COPY AppApi/ AppApi/

RUN dotnet publish AppApi/AppApi.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8081

ENTRYPOINT ["dotnet", "AppApi.dll"]