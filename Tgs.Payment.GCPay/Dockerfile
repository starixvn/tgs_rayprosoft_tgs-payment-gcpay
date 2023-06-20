#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.GCPay/Tgs.Payment.GCPay.csproj", "Tgs.Payment.GCPay/"]
COPY ["GCPay.Core/GCPay.Core.csproj", "GCPay.Core/"]
COPY ["GCPay.Client/GCPay.Client.csproj", "GCPay.Client/"]
RUN dotnet restore "Tgs.Payment.GCPay/Tgs.Payment.GCPay.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.GCPay"
RUN dotnet build "Tgs.Payment.GCPay.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Payment.GCPay.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.GCPay.dll"]