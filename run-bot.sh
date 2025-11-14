#!/bin/bash

# Скрипт для запуска Weather Telegram Bot
echo "🚀 Запуск Weather Telegram Bot..."

# Проверяем что мы в правильной директории
if [ ! -f "WeatherTelegramBot.csproj" ]; then
    echo "❌ Ошибка: Запускайте скрипт из папки проекта WeatherTelegramBot"
    exit 1
fi

# Запускаем бота с API ключом
echo "✅ Используется API ключ: c54a096cea954198b9a211005251411"
WeatherApiSettings__ApiKey="c54a096cea954198b9a211005251411" dotnet run

# Если бот завершился с ошибкой
if [ $? -ne 0 ]; then
    echo "❌ Бот завершился с ошибкой"
    echo "💡 Попробуйте: dotnet clean && dotnet build"
fi
