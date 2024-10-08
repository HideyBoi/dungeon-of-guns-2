using Godot;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;

public partial class Inventory : Node2D
{
	[Export] CanvasLayer visualRoot;
	[Export] TextureRect[] inactiveSprites;
	[Export] Label[] inactiveAmmoLabels;
	[Export] TextureRect[] activeSprites;
	[Export] Label[] activeAmmoLabels;
	[Export] Label[] activeTotalAmmoLabels;
	[Export] Control normalUi;
	[Export] Control syringeUi;
	[Export] Label syringeCountLabel;
	[Export] Control medkitUi;
	[Export] Label medkitCountLabel;
	[Export] Control grenadeUi;
	[Export] TextureRect grenadeTexture;
	[Export] Label grenadeCountLabel;
	[Export] InventoryGuiDrop bigUi;
	[Export] TextureProgressBar interactBar;
	[Export] PackedScene itemObject;
	[Export] float reach = 600;
	[Export] Material[] itemMats;
	[Export(PropertyHint.Layers2DPhysics)] uint itemLayerMask;

	[ExportCategory("Fist Item")]
	[Export] public Texture2D fistIcon;
	[Export] Weapon.Rarity rarity;


	[ExportCategory("Inventory Data")]
	[Export] ItemManager itemManager;
	int weaponsIndex = 0;
	public Weapon[] weapons = new Weapon[4];
	public int[] ammoCounts = new int[4];
	public int[] heals = new int[2];
	public Grenade currentGrenade = null;

	[ExportCategory("Debug")]
	[Export] bool debug = false;
	[Export] int debugWeapon1 = -1;
	[Export] int debugWeapon2 = -1;
	[Export] int debugWeapon3 = -1;
	[Export] int debugWeapon4 = -1;

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

		UpdateUi();

		HealthManager.OnChange += SetUiVisibility;
    }

	void SetUiVisibility(int healthState) {
		if (healthState == 0) {
			visualRoot.Show();
		} else {
			visualRoot.Hide();
		}
	}

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("scroll_up")) {
			weaponsIndex--;
			
			if (weaponsIndex < 0)
				weaponsIndex = weapons.Length - 1;

			UpdateUi();
		}
		
        if (Input.IsActionJustPressed("scroll_down")) {
			weaponsIndex++;
			
			if (weaponsIndex >= weapons.Length)
				weaponsIndex = 0;
			
			UpdateUi();
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

		/*
		if (Input.IsActionJustPressed("debug1")) {
			Ammo medium = (Ammo)GameManager.GetNewInventoryItem(1);
			medium.count = ammoCounts[1];
			ammoCounts[1] = 0;

			DropItem(medium);

			UpdateWeaponUI();
		}
		
		if (Input.IsActionJustPressed("debug1")) {
			Healable healable = (Healable)GameManager.GetNewInventoryItem(4);
			healable.count = 1;
			DropItem(healable);
		}

		if (Input.IsActionJustPressed("debug2")) {
			Healable healable = (Healable)GameManager.GetNewInventoryItem(5);
			healable.count = 1;
			DropItem(healable);
		}

		if (Input.IsActionJustPressed("debug3")) {
			Grenade grenade = (Grenade)GameManager.GetNewInventoryItem(6);
			grenade.count = 9;
			DropItem(grenade);
		}

		if (Input.IsActionJustPressed("debug4")) {
			Grenade grenade = (Grenade)GameManager.GetNewInventoryItem(7);
			grenade.count = 9;
			DropItem(grenade);
		}
		*/
		if (Input.IsActionJustPressed("debug1")) {
			Healable healable = (Healable)GameManager.GetNewInventoryItem(4);
			healable.count = 1;
			DropItem(healable);
		}

		if (Input.IsActionJustPressed("debug2")) {
			Healable healable = (Healable)GameManager.GetNewInventoryItem(5);
			healable.count = 1;
			DropItem(healable);
		}

		if (Input.IsActionJustPressed("debug3")) {
			Grenade grenade = (Grenade)GameManager.GetNewInventoryItem(6);
			grenade.count = 9;
			DropItem(grenade);
		}

		if (Input.IsActionJustPressed("debug4")) {
			Grenade grenade = (Grenade)GameManager.GetNewInventoryItem(7);
			grenade.count = 9;
			DropItem(grenade);
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
            if (collider is InventoryItemObject itemObject) {
                itemObject.Interact();

                if (Input.IsActionJustPressed("interact")) {
                    PickupItem(itemObject);
                }
            }
            if (collider is Chest chest){
                chest.Interact();

                if (Input.IsActionPressed("interact")) {
                    if (crateInteractCounter > 0.5) {
						chest.Open();
						crateInteractCounter = 0;
					} else {
						crateInteractCounter += delta;
					}
                } else {
					crateInteractCounter = 0;
				}
            } else if (collider is AmmoCrate ammo){
                ammo.Interact();

                if (Input.IsActionPressed("interact")) {
                    if (crateInteractCounter > 0.5) {
						ammo.Open();
						crateInteractCounter = 0;
					} else {
						crateInteractCounter += delta;
					}
                } else {
					crateInteractCounter = 0;
				}
            } else {
				crateInteractCounter = 0;
			}
        } else {
			crateInteractCounter = 0;
		}

		interactBar.Value = crateInteractCounter;
		if (interactBar.Value == 0) {
			interactBar.Hide();
		} else {
			interactBar.Show();
		}
	}
	double crateInteractCounter;

	public void ObjectEnteredItemZone(Node2D body) {
		if (!bool.Parse(ConfigManager.CurrentGameSettings["pickup_heals_ammo"]))
			return;

		if (body is InventoryItemObject inventoryItemObject) {
			if (inventoryItemObject.Item is Ammo or Healable) {
				PickupItem(inventoryItemObject);
			}
			if (inventoryItemObject.Item is Grenade grenade) {
				if (currentGrenade != null) {
					if (currentGrenade.itemId == grenade.itemId) {
						PickupItem(inventoryItemObject);
					}
				} else {
					PickupItem(inventoryItemObject);
				}
			}
		}
	}

	void PickupItem(InventoryItemObject itemObject) {
		InventoryItem item = itemObject.Item;

		switch (item) {
			case Weapon weapon:
				if (weapons[weaponsIndex] == null) {
					weapons[weaponsIndex] = weapon;
				} else {
					DropWeapon(weaponsIndex);
					weapons[weaponsIndex] = weapon;
				}
				break;
			case Ammo ammo:
				switch (ammo.ammoType)
				{
					case Weapon.AmmoType.Light:
						ammoCounts[0] += ammo.count;
						break;
					case Weapon.AmmoType.Medium:
						ammoCounts[1] += ammo.count;
						break;
					case Weapon.AmmoType.Heavy:
						ammoCounts[2] += ammo.count;
						break;
					case Weapon.AmmoType.Shell:
						ammoCounts[3] += ammo.count;
						break;
				}
				break;
			case Healable healable:
				heals[(int)healable.healType] += healable.count;
				break;
			case Grenade grenade:
				if (currentGrenade != null) {
					if (currentGrenade.itemId == grenade.itemId) {
						currentGrenade.count += grenade.count;
					} else {
						DropItem(currentGrenade);
						currentGrenade = grenade;
					}
				} else {
					currentGrenade = grenade;
				}
				break;
		}

		itemObject.Pickup();

		UpdateUi();
	}

    public void DropWeapon(int indexToDrop) {
		if (weapons[indexToDrop] == null)
			return;
		
		DropItem(weapons[indexToDrop]);
		weapons[indexToDrop] = null;

		UpdateUi();
	}

	void DropItem(InventoryItem item) {
		InventoryItemObject itemObj = itemObject.Instantiate<InventoryItemObject>();
		itemObj.GlobalPosition = GlobalPosition;

		float radians = Mathf.DegToRad(Tools.RandFloatRange(0, 360));
		Vector2 dir = new Vector2((float)Math.Sin(radians), -(float)Math.Cos(radians));

		itemObj.Setup(item, NetworkManager.I.Client.Id, dir, Tools.RandFloatRange(0.7f, 1f));

		GameManager.I.AddChild(itemObj);
	}

	public void UpdateUi (bool fromItemManager = false) {
		
		for (int i = 0; i < 4; i++)
		{
			// Update active gui
			if (i == weaponsIndex) {
				activeSprites[i].GetParent<Control>().Show();
				inactiveSprites[i].GetParent<Control>().Hide();

				if (weapons[i] == null) {
					activeSprites[i].Texture = fistIcon;
					activeSprites[i].Material = itemMats[0];
					activeAmmoLabels[i].Text = "";
					activeTotalAmmoLabels[i].Text = "";
				} else {
					activeSprites[i].Texture = weapons[i].itemSprite;
					activeAmmoLabels[i].Text = weapons[i].currentAmmo.ToString();
					activeSprites[i].Material = itemMats[(int)weapons[i].rarity];
					activeTotalAmmoLabels[i].Text = GetAmmoCount(weapons[i].ammoType).ToString();
				}
			// Update inactive gui
			} else {
				activeSprites[i].GetParent<Control>().Hide();
				inactiveSprites[i].GetParent<Control>().Show();

				if (weapons[i] == null) {
					inactiveSprites[i].Texture = fistIcon;
					inactiveSprites[i].Material = itemMats[0];
					inactiveAmmoLabels[i].Text = "---";
				} else {
					inactiveSprites[i].Texture = weapons[i].itemSprite;
					inactiveSprites[i].Material = itemMats[(int)weapons[i].rarity];
					inactiveAmmoLabels[i].Text = weapons[i].currentAmmo.ToString();
				}
			}
		}

		if (heals[1] == 0) {
			syringeUi.Hide();
		} else {
			syringeUi.Show();
			syringeCountLabel.Text = $"{heals[1]} x";
		}
		
		if (heals[0] == 0) {
			medkitUi.Hide();
		} else {
			medkitUi.Show();
			medkitCountLabel.Text = $"{heals[0]} x";
		}


		if (currentGrenade == null) {
			grenadeUi.Hide();
		} else {
			if (currentGrenade.count <= 0) {
				grenadeUi.Hide();
				currentGrenade = null;
			} else {
				grenadeUi.Show();
				grenadeCountLabel.Text = $"{currentGrenade.count} x";
				grenadeTexture.Texture = currentGrenade.itemSprite;
			}
			
		}

		if (!fromItemManager)
			itemManager.UpdateHolding(weaponsIndex, currentGrenade);
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
