PROJECT_NAME = budget-api.csproj
DOCKER_PATH = ../docker-compose.yml

.PHONY: run-db run-docker run-app stop migrate-add migrate-update

# Uruchamia bazę danych
run-db:
	docker-compose -f $(DOCKER_PATH) up -d db

# Uruchamia całe środowisko
run-containers:
	docker-compose -f $(DOCKER_PATH) up -d

# Uruchamia bazę w Dockerze + Aplikację lokalnie
run-app:
	$(MAKE) run-db
	dotnet run --project $(PROJECT_NAME)

# Zatrzymuje kontenery
stop:
	docker-compose -f $(DOCKER_PATH) down

# Tworzenie migracji
# Użycie: make migrate-add NAME=Inicjalizacja
migrate-add:
ifndef NAME
	$(error Musisz podac nazwe migracji, np: make migrate-add NAME=Inicjalizacja)
endif
	@echo "Dodawanie migracji: $(NAME)"
	dotnet ef migrations add $(NAME) --project $(PROJECT_NAME)

# Aktualizacja bazy danych
migrate-update:
	@echo "Aktualizacja bazy danych..."
	dotnet ef database update --project $(PROJECT_NAME)