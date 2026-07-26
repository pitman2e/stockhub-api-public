FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build-env
WORKDIR /app

#Set timezone to HK
ENV TZ=Asia/Hong_Kong
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Copy csproj and restore as distinct layers
COPY api/*.csproj ./api/
COPY ServiceDefaults/*.csproj ./ServiceDefaults/
RUN dotnet restore ./api/

# Copy everything else and build
COPY ServiceDefaults/ ./ServiceDefaults/
COPY api/ ./api/

RUN dotnet publish ./api/ -c Release -o /app/out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "StockHub.dll"]

#HEALTHCHECK --interval=300s --timeout=4s CMD curl --fail http://localhost/swagger/index.html || exit 1
