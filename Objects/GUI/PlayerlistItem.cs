using Godot;
using System;

public partial class PlayerlistItem : Panel
{
    [Export] Label nameLabel;
    [Export] Button kickButton;
    [Export] ConfirmationDialog popup;

    string myName;
    ushort id;


    public void Setup(string name, ushort id) {
        myName = name;
        Name = id.ToString();
        this.id = id;

        nameLabel.Text = name;

        if (!NetworkManager.I.Server.IsRunning)
            return;
        
        if (NetworkManager.I.Client.Id != id)
            kickButton.Show();
    }

    public void TryKick() {
        if (!NetworkManager.I.Server.IsRunning)
            return;
        
        popup.DialogText = popup.DialogText.Replace("[x]", myName);
        popup.Show();
    }

    public void KickPlayer() {
        if (!NetworkManager.I.Server.IsRunning)
            return;
        
        NetworkManager.I.Server.DisconnectClient(id);
    }
}
