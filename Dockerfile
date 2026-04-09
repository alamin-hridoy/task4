FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Task4UserManager.csproj", "./"]
RUN dotnet restore "Task4UserManager.csproj"

COPY . .
RUN dotnet publish "Task4UserManager.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Task4UserManager.dll"]
