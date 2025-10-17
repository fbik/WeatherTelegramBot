FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем project file и восстанавливаем зависимости
COPY *.csproj .
RUN dotnet restore

# Копируем весь код и собираем
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Финальный образ
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Открываем порт
EXPOSE 8080
ENV ASPNETCORE_URLS=http://*:8080

# Запускаем приложение
ENTRYPOINT ["dotnet", "WeatherTelegramBot.dll"]
