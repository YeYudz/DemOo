using YokiFrame;

namespace GameUI
{
    public class PanelStateData : IUIData
    {
        public string SelectedCharacterId { get; set; }
        public string CurrentTab { get; set; }
        public string CurrentRelicSlot { get; set; }
        public int CurrentStarLevel { get; set; } = 0;
    }
}
