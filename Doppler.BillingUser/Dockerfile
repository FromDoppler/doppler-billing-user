#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0.13-bullseye-slim AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0.403-bullseye-slim AS build
WORKDIR /src
COPY ["Doppler.BillingUser/Doppler.BillingUser.csproj", "Doppler.BillingUser/"]
RUN dotnet restore "Doppler.BillingUser/Doppler.BillingUser.csproj"
COPY . .
WORKDIR "/src/Doppler.BillingUser"
RUN dotnet build "Doppler.BillingUser.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Doppler.BillingUser.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Doppler.BillingUser.dll"]
