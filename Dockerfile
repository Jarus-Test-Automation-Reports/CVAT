# =============================
# BUILD IMAGE
# =============================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy ONLY the csproj first
COPY CAT.AID.Web.csproj ./

# restore dependencies
RUN dotnet restore CAT.AID.Web.csproj --disable-parallel

# now copy full project
COPY . .

# publish
RUN dotnet publish CAT.AID.Web.csproj -c Release -o /app/publish

# =============================
# RUNTIME IMAGE
# =============================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "CAT.AID.Web.dll"]
