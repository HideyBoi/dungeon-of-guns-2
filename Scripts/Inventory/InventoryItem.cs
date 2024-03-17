using Godot;

public partial class InventoryItem : Resource {
    [Export] public ushort itemId = 0;
    [Export] public string itemName;
    [Export] public Texture2D itemSprite;

}