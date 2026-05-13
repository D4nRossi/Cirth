# ADR-0002 — Blazor Server como frontend único

**Status**: **Revertido em 2026-05-12** (ver "Atualização" no fim deste ADR)
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

---

## Atualização — 2026-05-12: revertido para Razor Pages + HTMX

Após primeira semana de uso, ficou claro que o circuito SignalR do Blazor Server é frágil: qualquer exceção não-tratada em event handler (ex.: `Mediator.Send` levantando por DB ou IA indisponível) desconecta o circuito e a página vira inacessível. Os sintomas reportados ("botões que não funcionam", "tela trava") eram quase sempre circuito desconectado, não código quebrado.

Outras dores:
- State em memória nos componentes Razor era difícil de raciocinar — bugs de re-render frequentes.
- HTMX SSE resolve o requisito de streaming de chat com mais simplicidade e robustez do que o circuito Blazor.
- Antiforgery, SSE, error boundaries — todos os pontos sutis precisavam ser revisitados, e o frio cálculo foi: o tempo gasto consertando o Blazor seria suficiente pra rewrite limpo.

**Decisão revisada**: Razor Pages + HTMX (server-rendered, partial swaps), CSS custom sem framework UI. SSE substitui o SignalR para chat streaming. SignalR mantido apenas para notificações de "documento indexado".

A rejeição original de "Razor Pages: insuficiente para chat streaming" se baseava em premissa errada — HTMX SSE preenche essa lacuna com simplicidade maior do que o circuito do Blazor.

Detalhes da migração no commit `180b487` e na seção "Frontend: Razor Pages + HTMX" de `docs/ARCHITECTURE.md`.
