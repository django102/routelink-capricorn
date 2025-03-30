FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/FraudDetectionService/FraudDetectionService.csproj", "FraudDetectionService/"]
COPY ["src/Common/Common.csproj", "Common/"]
RUN dotnet restore "FraudDetectionService/FraudDetectionService.csproj"
COPY . .
WORKDIR "/src/FraudDetectionService"
RUN dotnet build "FraudDetectionService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FraudDetectionService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FraudDetectionService.dll"]