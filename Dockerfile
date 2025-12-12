# ============================
# 1) Build Stage
# ============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

# Copy csproj FIRST (better cache)
COPY *.csproj ./

# Restore dependencies
RUN dotnet restore --disable-parallel

# Copy entire project
COPY . .

# Publish in Release mode
RUN dotnet publish -c Release -o /app/publish


# ============================
# 2) Runtime Stage
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

WORKDIR /app

# Copy published output to final runtime image
COPY --from=build /app/publish .

# Ensure static files (images, css, js) are included
RUN ls -R /app/wwwroot || echo "wwwroot not found!"

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Start application
ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
