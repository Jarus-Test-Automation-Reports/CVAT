# ===========================
# 1️⃣ Build Stage
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project file first (better caching)
COPY CAT.AID.Web.csproj ./ 
RUN dotnet restore CAT.AID.Web.csproj --disable-parallel

# Copy full solution
COPY . .

# Publish the app
RUN dotnet publish CAT.AID.Web.csproj -c Release -o /app/publish

# ===========================
# 2️⃣ Runtime Stage
# ===========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Disable HTTPS (Render containers do not provide certs)
ENV ASPNETCORE_URLS=http://+:10000
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Copy published app
COPY --from=build /app/publish .

# Expose Render port
EXPOSE 10000

ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
