using Godot;
using Riptide;

public partial class Minimap : CanvasLayer
{
	public static Minimap I;

	[Export] PackedScene miniMapElement;
	MinimapElement[,] elements;
	[Export] GridContainer grid;

	float distance;
	Vector2I offset;

	Room[,] map;

	CharacterBody2D player;

    public override void _Ready()
    {
		I = this;
		distance = DungeonGenerator.I.distance;
		player = GetParent<CharacterBody2D>();

        Assemble();
    }

	MinimapElement lastRoom;

	public override void _PhysicsProcess(double delta) {
		if (Input.IsActionJustPressed("map"))
			Visible = !Visible;

		if (!Visible)
			return;

		Vector2I pos = (Vector2I)(player.GlobalPosition / distance);
		pos -= offset;

		if (pos.X < 0 || pos.X > map.GetLength(0) || pos.Y < 0 || pos.Y > map.GetLength(1)) {
			return;
		}

		lastRoom?.ChangePresence(false);

		lastRoom = elements[pos.X, pos.Y];
		lastRoom.ChangePresence(true);
	}

    public void Assemble() {
		GD.Print("Starting");

		map = DungeonGenerator.I.GameMap;
		map = TrimMap(map);

		grid.Columns = map.GetLength(0);

		elements = new MinimapElement[map.GetLength(0), map.GetLength(1)];

		for (int y = 0; y < map.GetLength(1); y++)
		{
			for (int x = 0; x < map.GetLength(0); x++)
			{
				MinimapElement element = miniMapElement.Instantiate<MinimapElement>();				
				grid.AddChild(element);
				elements[x,y] = element;
				if (map[x,y] != null)
					element.Setup(map[x,y].sideStates);
			}
		}
	}

	Room[,] TrimMap(Room[,] map) {
		int xMin = 0;

		for (int x = 0; x < map.GetLength(0); x++)
		{
			bool found = false;

			for (int y = 0; y < map.GetLength(1); y++)
			{
				if (map[x,y] != null) {
					found = true;
				}
			}

			if (found) {
				xMin = x;
				break;
			}
		}

		int xMax = 0;

		for (int x = map.GetLength(0) - 1; x > -1; x--)
		{
			bool found = false;

			for (int y = 0; y < map.GetLength(1); y++)
			{
				if (map[x,y] != null) {
					found = true;
				}
			}

			if (found) {
				xMax = x;
				break;
			}
		}

		int yMin = 0;

		for (int y = 0; y < map.GetLength(1); y++)
		{
			bool found = false;

			for (int x = 0; x < map.GetLength(0); x++)
			{
				if (map[x,y] != null) {
					found = true;
				}
			}

			if (found) {
				yMin = y;
				break;
			}
		}

		int yMax = 0;

		for (int y = map.GetLength(1) - 1; y > -1; y--)
		{
			bool found = false;

			for (int x = 0; x < map.GetLength(0); x++)
			{
				if (map[x,y] != null) {
					found = true;
				}
			}

			if (found) {
				yMax = y;
				break;
			}
		}

		int xDiff = xMax - xMin + 1;
		int yDiff = yMax - yMin + 1;

		offset = new(xMin, yMin);

		Room[,] newMap = new Room[xDiff, yDiff];

		for (int y = 0; y < newMap.GetLength(1); y++)
		{
			for (int x = 0; x < newMap.GetLength(0); x++)
			{
				newMap[x,y] = map[x+xMin, y+yMin];
			}
		}

		return newMap;
	}
}
