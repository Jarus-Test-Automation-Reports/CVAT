# ===============================
# Build Stage
# ===============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project file first (cache restore)
COPY CAT.AID.Web.csproj ./
RUN dotnet restore CAT.AID.Web.csproj --disable-parallel

# Copy everything (NOW wwwroot will be included)
COPY . .

# Publish
RUN dotnet publish CAT.AID.Web.csproj -c Release -o /app/publish

# ===============================
# Final Stage (runtime)
# ===============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Expose port
EXPOSE 8080

ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
