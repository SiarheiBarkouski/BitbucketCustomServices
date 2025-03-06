FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["BitbucketCustomServices.csproj", "./"]
RUN dotnet restore "BitbucketCustomServices.csproj"

# Copy everything else and build
COPY . .
RUN dotnet build "BitbucketCustomServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BitbucketCustomServices.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

USER root

# Create directory for database
RUN mkdir -p ./wwwroot/data && chmod 755 ./wwwroot/data

EXPOSE 7799
ENV ASPNETCORE_URLS=http://+:7799

# Copy published app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "BitbucketCustomServices.dll"]