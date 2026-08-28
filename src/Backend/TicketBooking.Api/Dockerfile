FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Directory.Build.props", "Directory.Packages.props", "./"]
COPY ["src/Backend/TicketBooking.Api/TicketBooking.Api.csproj", "src/Backend/TicketBooking.Api/"]
COPY ["src/Backend/Modules/TicketBooking.Identity.Core/TicketBooking.Identity.Core.csproj", "src/Backend/Modules/TicketBooking.Identity.Core/"]
RUN dotnet restore "src/Backend/TicketBooking.Api/TicketBooking.Api.csproj"
COPY . .
WORKDIR "/src/src/Backend/TicketBooking.Api"
RUN dotnet build "./TicketBooking.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./TicketBooking.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TicketBooking.Api.dll"]
