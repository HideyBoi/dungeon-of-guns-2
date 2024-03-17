using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

public partial class Inventory : Node2D
{
	[Export] TextureRect[] inactiveSprites;
	[Export] Label[] inactiveAmmoLabels;
	[Export] TextureRect activeSprite;
	[Export] Label activeAmmoLabel;
	[Export] Label activeTotalAmmoLabel;
	[Export] Control normalUi;
	[Export] InventoryGuiDrop bigUi;
	[Export] PackedScene itemObject;
	[Export] float reach = 600;
	[Export(PropertyHint.Layers2DPhysics)] uint itemLayerMask;

	[ExportCategory("Fist Item")]
	[Export] public Texture2D fistIcon;
	[Export] Weapon.Rarity rarity;

	[ExportCategory("Debug")]
	[Export] bool debug = false;
	[Export] int debugWeapon1 = -1;
	[Export] int debugWeapon2 = -1;
	[Export] int debugWeapon3 = -1;
	[Export] int debugWeapon4 = -1;
	
	int weaponsIndex = 0;
	public Weapon[] weapons = new Weapon[4];
	int[] ammoCounts = new int[4];

    public override void _Ready()
    {
		if (debug) {
			if (debugWeapon1 != -1)
				weapons[0] = (Weapon)GameManager.I.possibleItems[debugWeapon1].Duplicate();
			if (debugWeapon2 != -1)
				weapons[1] = (Weapon)GameManager.I.possibleItems[debugWeapon2].Duplicate();
			if (debugWeapon3 != -1)
				weapons[2] = (Weapon)GameManager.I.possibleItems[debugWeapon3].Duplicate();
			if (debugWeapon4 != -1)
				weapons[3] = (Weapon)GameManager.I.possibleItems[debugWeapon4].Duplicate();
		}

		// TODO: Initialize ammo counts via Gamerules
		ammoCounts[0] = 100;
		ammoCounts[1] = 200;
		ammoCounts[2] = 300;
		ammoCounts[3] = 400;

		UpdateWeaponUI();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("scroll_up")) {
			weaponsIndex++;
			if (weaponsIndex >= weapons.Length)
				weaponsIndex = 0;

			UpdateWeaponUI();
		}
		
        if (Input.IsActionJustPressed("scroll_down")) {
			weaponsIndex--;
			if (weaponsIndex < 0)
				weaponsIndex = weapons.Length - 1;
			
			UpdateWeaponUI();
		}

		if (Input.IsActionJustPressed("show_inventory")) {
			normalUi.Visible = false;
			bigUi.Visible = true;
		}

		if (Input.IsActionJustReleased("show_inventory")) {
			normalUi.Visible = true;
		}

		if (Input.IsActionJustReleased("drop_current_weapon")) {
			DropWeapon(weaponsIndex);
		}
    }

    public override void _PhysicsProcess(double delta)
    {
		Vector2 aimDir = GetLocalMousePosition().Normalized();

		PhysicsDirectSpaceState2D state = GetWorld2D().DirectSpaceState;
		PhysicsRayQueryParameters2D parameters = PhysicsRayQueryParameters2D.Create(GlobalPosition, GlobalPosition + (aimDir * reach));
		//parameters.CollisionMask = itemLayerMask;
		parameters.Exclude = new Godot.Collections.Array<Rid> { GetParent<CharacterBody2D>().GetRid() };
		Godot.Collections.Dictionary result = state.IntersectRay(parameters);

		if (result.Count > 0) {
			Node2D collider = (Node2D)result["collider"];
			if (collider is InventoryItemObject) {
				InventoryItemObject itemObject = (InventoryItemObject)collider;
				itemObject.Interact();

				if (Input.IsActionJustPressed("interact")) {
					PickupWeapon(itemObject);
				}
			}
		}
	}

	void PickupWeapon(InventoryItemObject itemObject) {
		Weapon weapon = (Weapon)itemObject.Item;

		if (weapons[weaponsIndex] == null) {
			weapons[weaponsIndex] = weapon;
		} else {
			DropWeapon(weaponsIndex);
			weapons[weaponsIndex] = weapon;
		}

		itemObject.Pickup();

		UpdateWeaponUI();
	}

    public void DropWeapon(int indexToDrop) {
		if (weapons[indexToDrop] == null)
			return;
		
		InventoryItemObject item = itemObject.Instantiate<InventoryItemObject>();
		item.GlobalPosition = GlobalPosition;

		float radians = 3.14f / 180 * Tools.RandFloatRange(0, 360);
		Vector2 dir = new Vector2((float)Math.Sin(radians), -(float)Math.Cos(radians));

		item.Setup(weapons[indexToDrop], NetworkManager.I.Client.Id, dir, Tools.RandFloatRange(0.7f, 1f));

		GameManager.I.AddChild(item);

		weapons[indexToDrop] = null;

		UpdateWeaponUI();
	}

	void UpdateWeaponUI () {
		int currentPos = weaponsIndex;
		
		for (int i = 0; i < 4; i++)
		{
			// Update active gui
			if (i == 0) {
				if (weapons[currentPos] == null) {
					activeSprite.Texture = fistIcon;
					activeAmmoLabel.Text = "";
					activeTotalAmmoLabel.Text = "";
				} else {
					activeSprite.Texture = weapons[currentPos].itemSprite;
					activeAmmoLabel.Text = weapons[currentPos].currentAmmo.ToString();
					activeTotalAmmoLabel.Text = GetAmmoCount(weapons[currentPos].ammoType).ToString();
				}
			// Update inactive gui
			} else {
				if (weapons[currentPos] == null) {
					inactiveSprites[i].Texture = fistIcon;
					inactiveAmmoLabels[i].Text = "---";
				} else {
					inactiveSprites[i].Texture = weapons[currentPos].itemSprite;
					inactiveAmmoLabels[i].Text = weapons[currentPos].currentAmmo.ToString();
				}
			}

			currentPos++;

			if (currentPos >= 4) {
				currentPos = 0;
			}
		}
	}

	int GetAmmoCount(Weapon.AmmoType ammoType) {
		switch (ammoType)
		{
			case Weapon.AmmoType.Light:
				return ammoCounts[0];
			case Weapon.AmmoType.Medium:
				return ammoCounts[1];
			case Weapon.AmmoType.Heavy:
				return ammoCounts[2];
			case Weapon.AmmoType.Shell:
				return ammoCounts[3];
			default:
				GD.PrintErr("The fuck did you put in here???");
				return 0;
		}
	}
}
