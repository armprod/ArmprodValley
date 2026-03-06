using Godot;
using System;

public partial class FishBar : CanvasLayer
{
	private TextureProgressBar _progressBar;
	private Area2D _cursorArea; 
	private RigidBody2D _playerArea; 

	// PŘIDÁNO: Definice pro uzel hráče, aby k němu měly přístup všechny metody
	private Node2D _player; 
	private AnimationPlayer _animPlayer; // Toto je ten z hráče

	[ExportGroup("Difficulty settings")]
	[Export] public float FillSpeed = 40f;
	[Export] public float DrainSpeed = 20f;

	[Export] public int CaughtFishCount = 0; 
	private bool _isFinished = false; 

	public override void _Ready()
	{
		// Najdeme hráče v grupě "player"
		var players = GetTree().GetNodesInGroup("player");
		if (players.Count > 0)
		{
			_player = (Node2D)players[0];
			// Uložíme si jeho AnimationPlayer do naší proměnné _animPlayer
			_animPlayer = _player.GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
			
			if (_animPlayer == null) GD.PrintErr("FishBar: Hráč nemá AnimationPlayer!");
		}
		else 
		{
			GD.PrintErr("FishBar: Žádný objekt v grupě 'player' nebyl nalezen!");
		}

		_progressBar = GetNodeOrNull<TextureProgressBar>("MainContainer/TextureProgressBar");
		_cursorArea = GetNodeOrNull<Area2D>("MainContainer/fish/Area2D"); 
		_playerArea = GetNodeOrNull<RigidBody2D>("MainContainer/outside/GreenCursor");

		if (_progressBar != null) _progressBar.Value = 0;
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_isFinished || _progressBar == null || _cursorArea == null || _playerArea == null) return;

		// 1. Získáme směr pohybu KURZORU (pro animaci nahoru/dolů)
		Vector2 playerDir = _playerArea.LinearVelocity;
		string directionName = GetDirectionName(playerDir);

		// 2. Určíme stav (up/down)
		bool isFishInside = _cursorArea.OverlapsBody(_playerArea);
		string state = isFishInside ? "up" : "down";

		// 3. Sestavíme název animace
		string animName = $"fish_catched_{state}_{directionName}";

		// 4. PUSTÍME ANIMACI NA HRÁČI (používáme tu proměnnou _animPlayer z Ready)
		if (_animPlayer != null && _animPlayer.HasAnimation(animName))
		{
			if (_animPlayer.CurrentAnimation != animName)
			{
				_animPlayer.Play(animName);
			}
		}

		// Logika progressu
		_progressBar.Value += (isFishInside ? FillSpeed : -DrainSpeed) * (float)delta;
		_progressBar.Value = Mathf.Clamp(_progressBar.Value, 0, 100);

		if (_progressBar.Value >= 100)
		{
			WinGame();
		}
		else if (_progressBar.Value <= 0)
		{
			LoseGame();
		}
		
		if (_animPlayer != null && _animPlayer.HasAnimation(animName))
		{
			if (_animPlayer.CurrentAnimation != animName)
			{
				GD.Print("TEĎ POUŠTÍM: " + animName); // Píše se toto v konzoli?
				_animPlayer.Play(animName);
			}
		}
	}

	private string GetDirectionName(Vector2 dir)
	{
		// Pokud kurzor skoro stojí, vrátíme 'right' nebo 'up' (záleží co máš jako default)
		if (dir.Length() < 5.0f) return "right"; 

		if (Mathf.Abs(dir.X) > Mathf.Abs(dir.Y))
			return dir.X > 0 ? "right" : "left";
		return dir.Y > 0 ? "down" : "up";
	}

	private async void WinGame()
	{       
		if (_isFinished) return;
		_isFinished = true;
		CaughtFishCount++;

		// 1. Zjistíme, jakým směrem hráč právě nahazuje (předpokládáme animace cast_left, cast_right...)
		string finalDirection = "down"; // Výchozí

		if (_animPlayer != null)
	{
		// AssignedAnimation si pamatuje název, i když animace už dohrála do konce
		string currentAnim = _animPlayer.AssignedAnimation; 

		GD.Print("Aktuálně detekovaná animace v Playerovi: " + currentAnim);

		if (currentAnim.Contains("right")) finalDirection = "right";
		else if (currentAnim.Contains("left")) finalDirection = "left";
		else if (currentAnim.Contains("down")) finalDirection = "down";
		else if (currentAnim.Contains("up")) finalDirection = "up";
	}

		// 2. Sestavíme název animace pro chycení (např. "fish_catched_left")
		// Tady si dej pozor na to "c" v catched/cathed, musí to být stejné jako v AnimationPlayeru
		string finalAnim = $"fish_catched_{finalDirection}";

		// 3. Pustíme vítěznou animaci na HRÁČI
		if (_animPlayer != null && _animPlayer.HasAnimation(finalAnim))
		{
			_animPlayer.Play(finalAnim);
			GD.Print($"Z nahození {_animPlayer.CurrentAnimation} přecházíme na {finalAnim}");
		}
		else
		{
			GD.PrintErr($"CHYBA: Animace {finalAnim} nebyla v Playerovi nalezena!");
		}

		// 4. Krátká pauza, aby scéna nezmizela v milisekundě
		await ToSignal(GetTree().CreateTimer(0.8f), "timeout");

		// 5. SMAŽEME RYBÁŘSKÝ BAR
		this.QueueFree();
	}
	
	private async void LoseGame()
	{
		if (_isFinished) return;
		_isFinished = true;

		GD.Print("Ryba utekla!");

		if (_animPlayer != null)
		{
			string currentAnim = _animPlayer.AssignedAnimation;
			string direction = "up";
			if (currentAnim.Contains("right")) direction = "right";
			else if (currentAnim.Contains("left")) direction = "left";
			else if (currentAnim.Contains("down")) direction = "down";
			
			string failAnim = $"idle_{direction}"; 
			_animPlayer.Play(failAnim);
		}

		await ToSignal(GetTree().CreateTimer(0.5f), "timeout");

		if (_player != null)
		{
			_player.Set("IsFishing", false);
		}

		this.QueueFree();
	}
}
