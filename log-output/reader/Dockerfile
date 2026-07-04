FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY log-output-reader.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "log-output-reader.dll"]
