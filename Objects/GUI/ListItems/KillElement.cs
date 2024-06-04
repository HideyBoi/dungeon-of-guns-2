using Godot;
using System;

public partial class KillElement : RichTextLabel
{
	[Export] AnimationPlayer player;

	public void SetupPrivateKill(string name) {
		Text = $"[center]Killed [color=red]{name}";
		player.Play("PopIn");
	}

	public void SetupPublicKill(string victim, string killer, string gun) {
		Text = $"[color=red]{victim}[/color] was killed by [color=cyan]{killer}[/color] using [color=gold]{gun}[/color]";
		player.Play("PopIn");
	}
}
