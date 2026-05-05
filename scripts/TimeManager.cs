using Godot;
using System;

public partial class TimeManager : Node
{
	public static TimeManager Instance { get; private set; }

	[Signal] public delegate void TimeChangedEventHandler(int day, int hour, int minute);

	[Export] public float TimeScale = 60.0f; // Jak rychle plyne čas (1.0 = reálný čas), 1.Hod = 1.Min
	
	private float _totalSeconds = 0;
	private int _minutes, _hours, _days = 1;
	public int CurrentDay => _days;
	public float TotalSeconds => _totalSeconds;

	public override void _Ready()
	{
		Instance = this;
		_totalSeconds = 8 * 3600; // 8:00
	}

	public override void _Process(double delta)
	{
		_totalSeconds += (float)delta * TimeScale;

		int newTotalMinutes = (int)(_totalSeconds / 60);
		int newMinutes = newTotalMinutes % 60;
		int newHours = (newTotalMinutes / 60) % 24;
		int newDays = (newTotalMinutes / (60 * 24)) + 1;

		if (newMinutes != _minutes)
		{
			_minutes = newMinutes;
			_hours = newHours;
			_days = newDays;
			EmitSignal(SignalName.TimeChanged, _days, _hours, _minutes);
		}
	}

	public string GetTimeString() => $"{_hours:D2}:{_minutes:D2}";
	
	public float GetFloatTime()
	{
		return _hours + (_minutes / 60.0f);
	}
	
	public void LoadTime(int day, float totalSeconds)
	{
		_days = day;
		_totalSeconds = totalSeconds;
		
		_minutes = (int)(_totalSeconds / 60) % 60;
		_hours = (int)(_totalSeconds / 3600) % 24;
		
		EmitSignal(SignalName.TimeChanged, _days, _hours, _minutes);
	}
	
}
