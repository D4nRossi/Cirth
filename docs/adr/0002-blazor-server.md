# ADR-0002 — Blazor Server como frontend único

**Status**: Aceito
**Data**: 2026-05-11
**Contexto**: Cirth V1

## Contexto

Cirth precisa de uma UI rica com chat em tempo real (streaming de tokens do LLM), upload de arquivos com feedback assíncrono, e busca interativa. As opções foram Razor Pages, Blazor Server, Blazor WASM, ou SPA externa (React/Vue) consumindo API .NET.

## Decisão

Adotamos **Blazor Server** com **MudBlazor** como component library e **SignalR** para push de eventos (já embutido no Blazor Server).

## Consequências

**Positivas**:
- 100% C# na stack. Sem context-switching para JS.
- Streaming de tokens do LLM via SignalR é trivial (já é a infra do Blazor Server).
- Notificações push para "documento indexado" são naturais.
- Estado em servidor: não precisa serializar para cliente, simplifica auth e ACL.
- Time-to-market alto: MudBlazor pronto, sem montar design system do zero.

**Negativas**:
- Cada usuário ativo consome memória no servidor (irrelevante para V1 com até dezenas de usuários).
- Latência de rede afeta UX em conexões ruins (mitigado por VM próxima geograficamente).
- WebSocket bloqueado em alguns ambientes corporativos (irrelevante para uso pessoal/amigos).

## Alternativas consideradas

- **Razor Pages**: rejeitado por insuficiência para chat com streaming em tempo real.
- **Blazor WASM**: rejeitado pelo tamanho de payload inicial e complexidade de gerenciar auth/state em cliente.
- **React/Vue + API .NET**: rejeitado por dobrar a stack e ferir o requisito "stack única".
