#!/bin/bash

if [ -z "$1" ]; then
  echo "Użycie: $0 <NazwaMigracji>"
  echo "Przykład: ./migrate.sh InitialCreate"
  exit 1
fi

echo "--- Sprawdzanie narzędzia dotnet-ef ---"
dotnet tool install --global dotnet-ef || true

echo "--- Dodawanie migracji: $1 ---"
dotnet ef migrations add "$1" --project ./budget-api.csproj

echo "--- Aktualizacja bazy danych ---"
dotnet ef database update --project ./budget-api.csproj
