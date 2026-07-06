# ---- Etapa de build ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish --no-self-contained

# ---- Etapa final (runtime, mucho más ligera) ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8000

ENTRYPOINT ["dotnet", "TorBoxTinfoilServer.dll"]