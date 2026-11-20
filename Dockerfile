# Stage 1: Build RaceDayMVC
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy the project file
COPY RaceDayMVC.csproj ./

# Restore NuGet packages
RUN dotnet restore RaceDayMVC.csproj

# Copy the remaining source files
COPY . ./

# Publish the application
RUN dotnet publish RaceDayMVC.csproj -c Release -o /app/publish --no-restore

# Stage 2: Run RaceDayMVC
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# Copy the published files from the build stage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "RaceDayMVC.dll"]