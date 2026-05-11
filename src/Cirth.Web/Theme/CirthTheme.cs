using MudBlazor;

namespace Cirth.Web.Theme;

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
            DrawerIcon = "#C9A961",
            AppbarBackground = "#0F0D0A",
            AppbarText = "#E8DCC4",
            Primary = "#C9A961",
            PrimaryContrastText = "#1A1410",
            Secondary = "#8C7140",
            Tertiary = "#5D7B3F",
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
            ActionDisabled = "#6B604C",
            Divider = "#3A2F23"
        },
        Typography = new Typography
        {
            Default = new Default
            {
                FontFamily = ["Inter", "system-ui", "sans-serif"],
                FontSize = "1rem",
                FontWeight = 400,
                LineHeight = 1.5
            },
            H1 = new H1 { FontFamily = ["Cinzel", "serif"], FontSize = "2.5rem", FontWeight = 600, LineHeight = 1.2 },
            H2 = new H2 { FontFamily = ["Cinzel", "serif"], FontSize = "2rem",   FontWeight = 500, LineHeight = 1.25 },
            H3 = new H3 { FontFamily = ["Cinzel", "serif"], FontSize = "1.5rem", FontWeight = 500, LineHeight = 1.3 },
            H4 = new H4 { FontFamily = ["Inter",  "sans-serif"], FontSize = "1.25rem", FontWeight = 600 },
            Button = new Button { FontFamily = ["Inter", "sans-serif"], FontWeight = 600, TextTransform = "none" },
            Body1 = new Body1 { FontFamily = ["Inter", "sans-serif"], FontSize = "1rem", FontWeight = 400, LineHeight = 1.6 },
            Body2 = new Body2 { FontFamily = ["Inter", "sans-serif"], FontSize = "0.875rem", FontWeight = 400, LineHeight = 1.5 }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "4px",
            DrawerWidthLeft = "260px"
        }
    };
}
