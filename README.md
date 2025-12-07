# pipsi2-backend
Część backendowa projektu z projektowania i programowania systemów internetowych II

## Temat:
System zarządzania domowym budżetem

## Lista członków grupy:
- Dawid Nowak
- Wład Rogowski
- Krzysztof Sułkowski
- Anna Trybel
- Aneta Walczak

## Podział ról w projekcie:
programista backendu - Dawid Nowak, Anna Trybel

programista frontendu - Wład Rogowski, Aneta Walczak

tester - Krzysztof Sułkowski

lider projektu - Krzysztof Sułkowski

project manager - cały zespół

inżynier devops - cały zespół

## Język programowania (backend):
C#

## Język programowania (frontend):
HTML, CSS

## Framework:
.NET

## Uruchomienie projektu przy użyciu Makefile

W projekcie dostępny jest plik `makefile`, który zawiera przydatne komendy do uruchomienia poszczególnych elementów projektu oraz obsługi migracji bazy danych.

### Wymagania dla systemu Windows

1. **Instalacja Chocolatey**
   Aby korzystać z poleceń make, najpierw należy zainstalować menedżera pakietów Chocolatey. Otwórz PowerShell jako administrator i wykonaj poniższe polecenie:
   ```powershell
   Set-ExecutionPolicy Bypass -Scope Process -Force; `
   [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; `
   iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))
   ```

2. **Instalacja make**
   Po zainstalowaniu Chocolatey, zainstaluj narzędzie `make` przez wykonanie poniższego polecenia (uruchomionego jako administrator):
   ```powershell
   choco install make
   ```

3. **Używanie komend z Makefile**
   W terminalu przejdź do katalogu, w którym znajduje się plik makefile (np. pipsi2-backend/budget-api), następnie możesz korzystać z komend:
   
  - `make run-db`  
     Uruchamia tylko bazę danych w Dockerze
   
  - `make run-containers`  
     Uruchamia API oraz Bazę w kontenerach w tle.

  - `make run-app`
     Uruchamia bazę w tle (Docker), a aplikację w konsoli.

  - `make stop`
     Zatrzymuje i usuwa wszystkie kontenery projektu.

  - `make migrate-add NAME=TwojaNazwaMigracji`  
     Dodaje nową migrację Entity Framework Core o podanej nazwie (np. `NAME=InitialCreate`). Upewnij się, że nazwa migracji jest podana.
   
  - `make migrate-update`  
     Aktualizuje bazę danych, stosując wszystkie oczekujące migracje.