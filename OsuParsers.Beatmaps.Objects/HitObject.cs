using System;
using System.Numerics;
using OsuParsers.Enums.Beatmaps;

namespace OsuParsers.Beatmaps.Objects;

public class HitObject
{
	public Vector2 Position { get; set; } = Vector2.Zero;

	public int StartTime { get; set; }

	public int EndTime { get; set; }

	public HitSoundType HitSound { get; set; }

	public Extras Extras { get; set; } = new Extras();

	public bool IsNewCombo { get; set; }

	public int ComboOffset { get; set; }

	public TimeSpan StartTimeSpan => TimeSpan.FromMilliseconds(StartTime);

	public TimeSpan EndTimeSpan => TimeSpan.FromMilliseconds(EndTime);

	public TimeSpan TotalTimeSpan => TimeSpan.FromMilliseconds(EndTime - StartTime);

	public HitObject()
	{
	}

	public HitObject(Vector2 position, int startTime, int endTime, HitSoundType hitSound, Extras extras, bool isNewCombo, int comboOffset)
	{
		Position = position;
		StartTime = startTime;
		EndTime = endTime;
		HitSound = hitSound;
		Extras = extras;
		IsNewCombo = isNewCombo;
		ComboOffset = comboOffset;
	}

	public float DistanceFrom(HitObject otherObject)
	{
		return Vector2.Distance(Position, otherObject.Position);
	}
}
