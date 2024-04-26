using Godot;
using System;

public partial class ItemManager : Marker2D
{
	[Export] Inventory inventory;
	[Export] Node2D rotation;
	[Export] Marker2D rotationHelper;
	[Export] Sprite2D gunSprite;
	[Export(PropertyHint.Range, "0,16")] float maxRotOffset = 8;
	public Weapon gun;

	public override void _Process(double delta)
	{
		Vector2 mousePos = GetGlobalMousePosition();
		rotationHelper.LookAt(mousePos);

		float angle = rotationHelper.GlobalRotationDegrees;
		float absAngle = MathF.Abs(angle);

		rotation.GlobalRotationDegrees = angle;
		float offset = maxRotOffset * ((absAngle - 1)/(90 - 1));
		rotation.Position = new(offset, rotation.Position.Y);

		if (GlobalPosition.X - mousePos.X < 0) {
            Scale = new(1, 1);
        } else {
            Scale = new(-1, 1);
			rotation.Position = new(rotation.Position.X + (maxRotOffset * -2), rotation.Position.Y);
        }
    }

	public void UpdateHolding(int newGunIndex) {
		gun = inventory.weapons[newGunIndex];
		if (gun != null) {
			gunSprite.Texture = gun.itemSprite;
		} else {
			gunSprite.Texture = null;
		}
	}
}
