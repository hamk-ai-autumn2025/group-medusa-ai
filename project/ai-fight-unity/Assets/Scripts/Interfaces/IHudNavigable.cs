using dev.susybaka.TurnBasedGame.UI;

namespace dev.susybaka.TurnBasedGame.Interfaces
{
    public enum HudNavCommand { Previous, Next, Submit, Back }

    public interface IHudNavigable
    {
        // Called when the window becomes the active/focused one.
        void OnFocus(HudNavigationHandler nav);

        // Called when the window loses focus (e.g., we push another on top or pop it).
        void OnBlur();

        // Called by the navigator when inputs occur (Prev/Next/Submit/Back).
        void OnNavCommand(HudNavCommand cmd);
    }
}