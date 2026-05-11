# Cirth — Design System

> *"A man that flies from his fear may find that he has only taken a short cut to meet it."* — Tolkien

A identidade visual do Cirth é "arquivo de Gondor cruzado com Bodleian Library moderna". Pergaminho-escuro elegante, ouro de selo, contenção ornamental, máxima legibilidade. Sem cosplay medieval, sem clichê de fantasia.

---

## 1. Filosofia visual

Três princípios guiam toda decisão visual:

1. **Reverência**: o conhecimento merece ser exibido com gravidade. Sem festividade, sem distração.
2. **Legibilidade**: nada deve atrapalhar a leitura. Contraste, tamanho, espaçamento sempre acima do mínimo de acessibilidade.
3. **Contenção ornamental**: detalhes decorativos existem, mas são raros e intencionais. Um separador rúnico aqui, um glow dourado ali. Nunca uma página inteira de ornamento.

## 2. Paleta de cores

Tokens CSS variables em `wwwroot/css/cirth-tokens.css`:

```css
:root {
  /* Backgrounds */
  --cirth-bg-deep: #0F0D0A;        /* pergaminho queimado */
  --cirth-bg-surface: #1A1410;     /* madeira escura */
  --cirth-bg-elevated: #241C16;    /* couro envelhecido */
  --cirth-bg-overlay: #2E2519;     /* modal, popover */

  /* Primary (ouro) */
  --cirth-gold-primary: #C9A961;   /* ouro de selo */
  --cirth-gold-accent: #D4AF37;    /* ouro folha */
  --cirth-gold-muted: #8C7140;     /* ouro envelhecido */

  /* Semantic */
  --cirth-rune-red: #8B2500;       /* selo quebrado, erro */
  --cirth-rune-red-soft: #A63F1A;
  --cirth-moss-green: #5D7B3F;     /* musgo, sucesso */
  --cirth-moss-green-soft: #7A9A5C;
  --cirth-ink-blue: #3D5A6C;       /* tinta de pergaminho, info */
  --cirth-amber-warning: #B8860B;  /* selo de cera, atenção */

  /* Text */
  --cirth-text-primary: #E8DCC4;   /* pergaminho creme */
  --cirth-text-secondary: #A89B7D; /* tinta desbotada */
  --cirth-text-muted: #6B604C;     /* tinta apagada */
  --cirth-text-on-gold: #1A1410;   /* texto sobre fundo dourado */

  /* Borders */
  --cirth-border-faint: #2A2218;
  --cirth-border-default: #3A2F23;
  --cirth-border-strong: #5A4838;
  --cirth-border-gold: #8C7140;

  /* Shadows */
  --cirth-shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.4);
  --cirth-shadow-md: 0 4px 12px rgba(0, 0, 0, 0.5);
  --cirth-shadow-gold-glow: 0 0 12px rgba(201, 169, 97, 0.25);

  /* Radii */
  --cirth-radius-sm: 2px;
  --cirth-radius-md: 4px;
  --cirth-radius-lg: 8px;
}
```

### Aplicação semântica

| Uso | Token |
|---|---|
| Background da página | `--cirth-bg-deep` |
| Cards, painéis principais | `--cirth-bg-surface` |
| Modais, dropdowns | `--cirth-bg-elevated` |
| Botão primário, link de ação | `--cirth-gold-primary` |
| Botão primário hover | `--cirth-gold-accent` |
| Texto principal | `--cirth-text-primary` |
| Texto auxiliar (labels, metadata) | `--cirth-text-secondary` |
| Erro, destrutivo | `--cirth-rune-red` |
| Sucesso, indexado | `--cirth-moss-green` |
| Info, neutro | `--cirth-ink-blue` |
| Atenção, quota próxima do limite | `--cirth-amber-warning` |

## 3. Tipografia

```css
:root {
  --cirth-font-display: 'Cinzel', 'Trajan Pro', serif;
  --cirth-font-body: 'Inter', system-ui, sans-serif;
  --cirth-font-mono: 'JetBrains Mono', 'Cascadia Code', monospace;
}
```

Importar via Google Fonts no `App.razor`:

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@400;500;600;700&family=Inter:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;600&display=swap" rel="stylesheet">
```

### Escala tipográfica

| Token | Família | Tamanho | Peso | Uso |
|---|---|---|---|---|
| `h1` | Cinzel | 2.5rem | 600 | Título de página principal |
| `h2` | Cinzel | 2rem | 500 | Seção major |
| `h3` | Cinzel | 1.5rem | 500 | Subseção |
| `h4` | Inter | 1.25rem | 600 | Cabeçalho de card |
| `body-lg` | Inter | 1.125rem | 400 | Texto enfatizado |
| `body` | Inter | 1rem | 400 | Texto padrão |
| `body-sm` | Inter | 0.875rem | 400 | Metadata, labels |
| `caption` | Inter | 0.75rem | 500 | Tags, badges |
| `code` | JetBrains Mono | 0.9rem | 400 | Inline code, ids |

Cinzel só em títulos `h1`-`h3`. Tudo mais é Inter. Resista ao impulso de usar Cinzel em botões ou parágrafos: vira ilegível.

## 4. Espaçamento

Sistema 4px base:

```css
:root {
  --cirth-space-1: 4px;
  --cirth-space-2: 8px;
  --cirth-space-3: 12px;
  --cirth-space-4: 16px;
  --cirth-space-6: 24px;
  --cirth-space-8: 32px;
  --cirth-space-12: 48px;
  --cirth-space-16: 64px;
}
```

Padding padrão de card: `--cirth-space-6`. Gap entre cards: `--cirth-space-4`.

## 5. Componentes

### Botão primário

```css
.cirth-btn-primary {
  background: var(--cirth-gold-primary);
  color: var(--cirth-text-on-gold);
  border: 1px solid var(--cirth-gold-accent);
  padding: var(--cirth-space-2) var(--cirth-space-6);
  font-family: var(--cirth-font-body);
  font-weight: 600;
  border-radius: var(--cirth-radius-md);
  transition: all 200ms ease;
}
.cirth-btn-primary:hover {
  background: var(--cirth-gold-accent);
  box-shadow: var(--cirth-shadow-gold-glow);
}
```

### Card

```css
.cirth-card {
  background: var(--cirth-bg-surface);
  border: 1px solid var(--cirth-border-default);
  border-radius: var(--cirth-radius-lg);
  padding: var(--cirth-space-6);
  box-shadow: var(--cirth-shadow-md);
}
```

### Separador rúnico (ornamento contido)

Para separar seções importantes em uma página de detalhe de documento ou em uma resposta longa do chat. Uma linha fina dourada com um pequeno losango central:

```html
<div class="cirth-divider">
  <span class="cirth-divider-line"></span>
  <span class="cirth-divider-rune">◆</span>
  <span class="cirth-divider-line"></span>
</div>
```

```css
.cirth-divider {
  display: flex;
  align-items: center;
  gap: var(--cirth-space-4);
  margin: var(--cirth-space-8) 0;
}
.cirth-divider-line {
  flex: 1;
  height: 1px;
  background: linear-gradient(to right, transparent, var(--cirth-gold-muted), transparent);
}
.cirth-divider-rune {
  color: var(--cirth-gold-primary);
  font-size: 0.75rem;
}
```

### Tag/badge

```css
.cirth-tag {
  display: inline-flex;
  align-items: center;
  gap: var(--cirth-space-1);
  background: var(--cirth-bg-elevated);
  color: var(--cirth-text-secondary);
  border: 1px solid var(--cirth-border-default);
  border-radius: var(--cirth-radius-sm);
  padding: 2px var(--cirth-space-2);
  font-size: 0.75rem;
  font-weight: 500;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
```

## 6. Tema MudBlazor

Em `Cirth.Web/Theme/CirthTheme.cs`:

```csharp
public static class CirthTheme
{
    public static MudTheme Build() => new()
    {
        PaletteDark = new PaletteDark
        {
            Black = "#0F0D0A",
            Background = "#0F0D0A",
            Surface = "#1A1410",
            DrawerBackground = "#1A1410",
            DrawerText = "#E8DCC4",
            AppbarBackground = "#0F0D0A",
            AppbarText = "#E8DCC4",
            Primary = "#C9A961",
            PrimaryContrastText = "#1A1410",
            Secondary = "#8C7140",
            Success = "#5D7B3F",
            Error = "#8B2500",
            Warning = "#B8860B",
            Info = "#3D5A6C",
            TextPrimary = "#E8DCC4",
            TextSecondary = "#A89B7D",
            TextDisabled = "#6B604C",
            LinesDefault = "#3A2F23",
            LinesInputs = "#5A4838",
            TableLines = "#2A2218",
            TableHover = "#241C16",
            ActionDefault = "#C9A961",
            ActionDisabled = "#6B604C"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = new[] { "Inter", "system-ui", "sans-serif" },
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            H1 = new H1Typography { FontFamily = new[] { "Cinzel", "serif" }, FontSize = "2.5rem", FontWeight = "600" },
            H2 = new H2Typography { FontFamily = new[] { "Cinzel", "serif" }, FontSize = "2rem", FontWeight = "500" },
            H3 = new H3Typography { FontFamily = new[] { "Cinzel", "serif" }, FontSize = "1.5rem", FontWeight = "500" },
            Button = new ButtonTypography { FontFamily = new[] { "Inter", "sans-serif" }, FontWeight = "600", TextTransform = "none" }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px",
            DrawerWidthLeft = "280px"
        }
    };
}
```

## 7. Ícones

Biblioteca: **Lucide** (via `Blazored.LucideIcons` ou SVGs inline). Stroke-width padrão `1.5`. Tamanho padrão `20px`. Não usar emojis em UI core (apenas em conteúdo do usuário).

Ícones-chave do produto:
- Documento: `file-text`
- PDF: `file`
- Busca: `search`
- Chat: `message-square`
- Tag: `tag`
- Coleção: `folder`
- Versão: `history`
- Salvar: `bookmark`
- Configurações: `settings`
- Admin: `shield`
- API Key: `key`

## 8. Animações

Sutis e curtas. Tudo entre 150ms e 300ms. Easing `ease` ou `ease-out`.

- **Hover de botão**: cor + glow em 200ms.
- **Modal entrando**: fade + scale de 0.98 para 1.0 em 200ms.
- **Token de chat aparecendo**: fade-in de 100ms por token (ou nenhum, deixa o cursor piscante indicar).
- **Loading**: cursor piscante no chat. Para uploads, barra de progresso com gradiente sutil dourado.

Evite: bounces, spins exagerados, parallax. Cirth é uma biblioteca, não um carrossel.

## 9. Logo

SVG da runa Cirth representando o som "K" (escolhido por kirth/cirth) sobre um pequeno escudo dourado:

```html
<svg viewBox="0 0 64 64" fill="none" xmlns="http://www.w3.org/2000/svg">
  <path d="M32 4 L56 16 L56 40 C56 50 44 58 32 60 C20 58 8 50 8 40 L8 16 Z" 
        stroke="#C9A961" stroke-width="1.5" fill="#1A1410"/>
  <path d="M24 20 L24 44 M24 32 L40 20 M24 32 L40 44" 
        stroke="#C9A961" stroke-width="2" stroke-linecap="round" fill="none"/>
</svg>
```

Esse glifo é uma aproximação estilizada do `cirth #29` (kᵃ). Não precisa ser arqueologicamente preciso, precisa ser memorável.

## 10. Layout de página padrão

```
┌──────────────────────────────────────────────┐
│  Logo Cirth   [Search ───────]    [User Avatar]│  ← AppBar (Cinzel para logo)
├────────┬─────────────────────────────────────┤
│        │                                      │
│ Docs   │   Conteúdo da página                 │
│ Chat   │                                      │
│ Tags   │                                      │
│ Colls  │                                      │
│        │                                      │
│ Admin  │                                      │
│        │                                      │
└────────┴──────────────────────────────────────┘
```

Drawer sempre visível em desktop, colapsa em mobile. Largura 280px.
