using Godot;
using System;

public partial class InventoryGuiDrop : Control
{
    int hovering = -1;
    [Export] Inventory inventory;
    [Export] Label[] names;
    [Export] TextureRect[] images;
    [Export] Label[] ammoCounts;

    public void SetHoverLocation(int index) {
        GD.Print("Hovering " + index);
        hovering = index;
    }

    public void StopHover() {
        if (hovering == -1)
            return;
        
        GD.Print("No longer hovering " + hovering);
        hovering = -1;
    }

    public override void _PhysicsProcess(double delta)
    {   
        if (Input.IsActionJustPressed("show_inventory")) {
            for (int i = 0; i < inventory.weapons.Length; i++)
            {
                string name;
                string ammoCount;
                Texture2D weaponSprite;
                
                if (inventory.weapons[i] == null) {
                    name = "Fist";
                    ammoCount = "---";
                    weaponSprite = inventory.fistIcon;
                } else {
                    name = inventory.weapons[i].itemName;
                    ammoCount = inventory.weapons[i].currentAmmo.ToString();
                    weaponSprite = inventory.weapons[i].itemSprite;
                }

                names[i].Text = name;
                ammoCounts[i].Text = ammoCount;
                images[i].Texture = weaponSprite;
            }
        }
        if (Input.IsActionJustReleased("show_inventory"))
        {
            if (hovering == -1) {
                Visible = false;
                return;
            }
            
            GD.Print("Dropping " + hovering);
            inventory.DropWeapon(hovering);

            hovering = -1;
            Visible = false;
        }
    }
}
