#!/bin/bash

echo "🧹 Очистка проекта..."
dotnet clean

echo "🔨 Сборка проекта..."
dotnet build

echo "🚀 Запуск бота..."
WeatherApiSettings__ApiKey="c54a096cea954198b9a211005251411" dotnet run
