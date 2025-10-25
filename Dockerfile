FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Development
WORKDIR /src

COPY ["HW1.Api/HW1.Api.csproj", "HW1.Api/"]
RUN dotnet restore "HW1.Api/HW1.Api.csproj"

COPY . .
WORKDIR "/src/HW1.Api"
RUN dotnet build "HW1.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Development
RUN dotnet publish "HW1.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HW1.Api.dll"]