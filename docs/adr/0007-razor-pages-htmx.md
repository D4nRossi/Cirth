# ADR-0007 — Razor Pages + HTMX no lugar de Blazor Server

**Status**: Aceito
**Data**: 2026-05-12
**Substitui**: ADR-0002

## Contexto

Após uma semana de uso do Blazor Server (ver ADR-0002), o circuito SignalR provou-se frágil em ambiente real: qualquer exceção não-tratada em event handler (`Mediator.Send` levantando por DB indisponível, IA fora do ar, erros transientes de rede) desconecta o circuito e a página inteira fica inacessível até reload manual. Quase todos os bugs reportados pelo usuário eram circuito desconectado, não código quebrado.

Outros pontos de dor:
- State-in-memory dentro dos componentes Razor era difícil de raciocinar; re-renders fora de ordem geravam bugs sutis.
- `ErrorBoundary` cobre só erros na render phase, não event handlers — ficava obrigatório envolver cada `Mediator.Send` em try/catch e converter pra snackbar manualmente.
- Antiforgery, SSE, contraste de CSS, fluxos de upload — vários pontos sutis precisavam de cuidado especial no Blazor.

## Decisão

Migrar o frontend de **Blazor Server + MudBlazor** para **Razor Pages + HTMX + CSS custom**.

- **Razor Pages** (`Pages/*.cshtml` + `Pages/*.cshtml.cs`): páginas server-rendered, navegação tradicional via GET, POST via form submission.
- **HTMX 2.x** (vendored em `wwwroot/js/`): partial swaps, debounced filters, `hx-post` em forms e elementos avulsos, `hx-trigger="every 5s"` pra auto-refresh.
- **HTMX SSE extension** (`htmx-sse 2.2.2`): chat streaming via Server-Sent Events. Substitui o `IAsyncEnumerable` no circuito Blazor por uma rota dedicada `/Chat/Stream/{streamId}` que emite eventos `progress`, `token`, `actions`, `done`.
- **CSS custom** (`wwwroot/css/cirth.css`): design tokens Cirth em `:root`, classes utilitárias (`.btn`, `.card`, `.chip`, `.grid-N`, `.flex`, `.bubble`). Sem framework UI.
- **SignalR mantido** apenas para notificações de "documento indexado" via `INotificationHub` no Worker — não pra chat (SSE faz melhor).

## Consequências

**Positivas**:
- Render determinístico. Sem circuito frágil.
- Stateless por natureza: cada request renderiza do zero (com cache no `IMemoryCache` onde faz sentido).
- HTMX é trivial de debugar: inspect Network → ver request/response em HTML.
- Performance previsível. Sem overhead de SignalR pra cada interação.
- CSS custom sem framework deixa o tema Cirth exato.

**Negativas**:
- Form posts não-HTMX exigem antiforgery token explícito — bug real já mordido, documentado em `CLAUDE.md`.
- Streaming via SSE precisa de cuidados específicos: padding inicial pra forçar flush HTTP/2, `IHttpResponseBodyFeature.DisableBuffering()`, retornar `EmptyResult` pra evitar `PageResult` implícito.
- HTMX `data-hx-*` attributes são auto-processados — não dá pra usar pra metadados de JS custom. Documentado.
- Vários pequenos bugs encontrados durante a migração viraram regras em `CLAUDE.md` (typed-ID em LINQ, EF TakeLast, etc.).

## Alternativas consideradas

- **Blazor Static SSR + Interactive islands** (.NET 8+): só componentes específicos viram interactive. Mantém MudBlazor. Tradeoff: convivência de dois modelos mentais no mesmo projeto.
- **ASP.NET Core MVC clássico**: estável mas verboso; chat fica desconfortável sem JS adicional.
- **React/Vue + API .NET**: rejeitado pelo mesmo motivo do ADR-0002 — dobra a stack.
- **Tornar Blazor Server robusto** com error boundary em todo handler: rejeitado porque o tempo de fix seria comparável ao rewrite, e o resultado ainda dependeria do circuito.

## Implementação

Commit principal: `180b487` (migração inicial). Fixes subsequentes: antiforgery (`7c2f286`), endpoints duais Azure AI (`5162661`), BM25 column (`7d47d9b`), SSE polish (`75e86b8`).

Layout de pastas após a migração:

```
src/Cirth.Web/
  Pages/
    Index.cshtml(.cs)              # /
    Documents/{Index, Upload, Detail}.cshtml(.cs)
    Search.cshtml(.cs)
    Tags/Index.cshtml(.cs)
    Collections/Index.cshtml(.cs)
    Saved/Index.cshtml(.cs)
    Chat/{Index, Stream}.cshtml(.cs)
    Admin/{Index, Logs}.cshtml(.cs)
    ApiKeys.cshtml(.cs)
    Error.cshtml(.cs)
    Shared/_Layout.cshtml
    _ViewImports.cshtml
    _ViewStart.cshtml
  Infrastructure/
    CirthPageModel.cs               # base com Toast, SendAsync, HxRedirect, IsHtmx
  wwwroot/
    css/cirth.css
    js/{htmx.min.js, htmx-sse.js, cirth.js}
```
