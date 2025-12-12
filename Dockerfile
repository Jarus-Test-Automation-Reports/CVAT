# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything into the container
COPY . .

# Publish the project
RUN dotnet publish "CAT.AID.Web.csproj" -c Release -o /out

# Final container
FROM base AS final
WORKDIR /app
COPY --from=build /out .

ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
