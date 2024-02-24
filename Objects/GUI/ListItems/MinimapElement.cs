using Godot;

public partial class MinimapElement : Control
{
    [Export] Panel basePanel;
    [Export] Panel herePanel;
    [Export] Panel[] sidePanels;

    public void Setup(bool[] sides) {
        basePanel.Visible = true;

        for (int i = 0; i < sides.Length; i++)
        {
            sidePanels[i].Visible = sides[i];
        }
    }

    public void ChangePresence(bool isHere) {
        herePanel.Visible = isHere;
    }
}
