using Godot;

public partial class InventoryItem : Resource {
    public ushort itemId = 0;
    [Export] public string itemName;
    [Export] public Texture2D itemSprite;

}