# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project folder (avoid copying whole 340MB repo!)
COPY CAT.AID.Web/ ./CAT.AID.Web/

# Restore + publish
WORKDIR /src/CAT.AID.Web
RUN dotnet publish -c Release -o /out

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /out .

ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
