#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Js.LedgerEs/Js.LedgerEs.csproj", "Js.LedgerEs/"]
RUN dotnet restore "Js.LedgerEs/Js.LedgerEs.csproj"
COPY . .
WORKDIR "/src/Js.LedgerEs"
RUN dotnet build "Js.LedgerEs.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Js.LedgerEs.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Js.LedgerEs.dll"]