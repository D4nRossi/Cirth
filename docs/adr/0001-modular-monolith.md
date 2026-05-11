# ADR-0001 — Modular Monolith em Clean Architecture

**Status**: Aceito
**Data**: 2026-05-11
**Contexto**: Cirth V1

## Contexto

Cirth precisa ser deployável, manutenível e extensível por uma única pessoa (Danirock) na V1, com perspectiva de crescer para SaaS leve. As opções consideradas foram microsserviços, monolito tradicional, e modular monolith.

## Decisão

Adotamos **modular monolith em Clean Architecture**, um único deployable (`Cirth.Web` + `Cirth.Mcp` + `Cirth.Worker` como hosts irmãos, compartilhando `Cirth.Application` e `Cirth.Infrastructure`).

Os módulos funcionais (Documents, Search, Chat, Identity, Tags, Collections, SavedAnswers, Quotas) vivem como pastas dentro de `Cirth.Application/Features/`, com fronteiras claras via interfaces e comunicação por handlers MediatR.

## Consequências

**Positivas**:
- Operação simples: um `docker compose up` traz a aplicação inteira.
- Refactoring entre módulos é trivial (mesmo solution, mesmo deploy).
- Dívida arquitetural mínima na V1.
- Path de evolução claro: se um módulo precisar virar serviço independente na V3+, a fronteira já está desenhada.

**Negativas**:
- Escalabilidade horizontal por módulo não é possível sem refactor.
- Falha em um módulo (ex.: pipeline de embedding) pode impactar a aplicação inteira se não houver isolamento de threads/recursos.
- Tentação de tomar atalhos arquiteturais (chamar repositório direto do Web) precisa ser combatida com disciplina e code review.

## Alternativas consideradas

- **Microsserviços**: rejeitado por overhead operacional desproporcional ao escopo V1.
- **Monolito tradicional (sem Clean Arch)**: rejeitado porque sacrifica testabilidade e clareza de fronteiras.
