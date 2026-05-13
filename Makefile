SLN     := src/Cirth.sln
WEB     := src/Cirth.Web/Cirth.Web.csproj
INFRA_P := src/Cirth.Infrastructure
UNIT    := tests/Cirth.Domain.Tests tests/Cirth.Application.Tests
INT     := tests/Cirth.Integration.Tests

.PHONY: build test test-unit test-integration test-all migration watch worker logs logs-web logs-worker secrets-help

## Build

build:
	dotnet build $(SLN)

## Tests

# Default: fast unit tests only (no Docker required)
test: test-unit

test-unit:
	dotnet test $(SLN) --filter "FullyQualifiedName!~Integration"

test-integration:
	@echo "→ Requires Docker. Starting integration tests via Testcontainers..."
	dotnet test $(INT)

# All tests (unit + integration). Docker must be running.
test-all:
	dotnet test $(SLN)

## Database

migration:
	@test -n "$(NAME)" || (echo "Usage: make migration NAME=<MigrationName>" && exit 1)
	dotnet ef migrations add $(NAME) -p $(INFRA_P) -s $(WEB)

db-update:
	dotnet ef database update -p $(INFRA_P) -s $(WEB)

db-drop:
	dotnet ef database drop -p $(INFRA_P) -s $(WEB) --force

## Dev server

watch:
	dotnet watch --project $(WEB)

worker:
	dotnet run --project src/Cirth.Worker

## Logs (Serilog rolling files; uses the most recent dated file for each)

logs: logs-web

logs-web:
	@LATEST=$$(ls -t src/Cirth.Web/logs/cirth-web-*.log 2>/dev/null | head -1); \
	if [ -z "$$LATEST" ]; then echo "No web log yet — start the server first (make watch)"; exit 1; fi; \
	echo "→ Tailing $$LATEST (Ctrl+C to stop)"; \
	tail -F -n 200 "$$LATEST"

logs-worker:
	@LATEST=$$(ls -t src/Cirth.Worker/logs/cirth-worker-*.log 2>/dev/null | head -1); \
	if [ -z "$$LATEST" ]; then echo "No worker log yet — start the worker first (make worker)"; exit 1; fi; \
	echo "→ Tailing $$LATEST (Ctrl+C to stop)"; \
	tail -F -n 200 "$$LATEST"

## Secrets

secrets-help:
	@echo "Set local dev secrets (never committed):"
	@echo "  dotnet user-secrets set AzureAi:ApiKey      '<key>'  --project $(WEB)"
	@echo "  dotnet user-secrets set EntraId:ClientSecret '<secret>' --project $(WEB)"
	@echo ""
	@echo "List current secrets:"
	@echo "  dotnet user-secrets list --project $(WEB)"
