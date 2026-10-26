using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using AimAssistPlugin.Sdk.Audio;
using AimAssistPlugin.Services;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Decoders;

namespace AimAssistPlugin.Sdk.Player;

internal class HitObjectManager
{
	public static List<HitObject> hitObjects = new List<HitObject>();

	private const float PLAYFIELD_HEIGHT = 384f;

	public static void CacheHitObjects()
	{
		if (hitObjects.Count > 0)
		{
			ClearHitObjects();
		}
		hitObjects = GetAndTransformHitObjects();
	}

	public static void ClearHitObjects()
	{
		hitObjects.Clear();
	}

	public static HitObject GetHitObject(int index)
	{
		return hitObjects[index];
	}

	public static int GetPreEmpt()
	{
		if (TosuService.LatestResponse != null)
		{
			return (int)MapDifficultyRange(TosuService.LatestResponse.beatmap.stats.ar.converted, 180.0, 1200.0, 450.0, adjustToMods: false);
		}
		return 0;
	}

	public static float GetHitObjectRadius()
	{
		if (TosuService.LatestResponse != null)
		{
			double num = TosuService.LatestResponse.beatmap.stats.cs.original;
			List<string> list = ((string)TosuService.LatestResponse.play.mods.name).Select((char c) => c.ToString()).ToList();
			if (list.Contains("EZ"))
			{
				num *= 0.5;
			}
			else if (list.Contains("HR"))
			{
				num *= 1.3;
			}
			return 54.4f - 4.48f * (float)num;
		}
		return 0f;
	}

	public static int GetCurrentHitObjectIndex()
	{
		int time = AudioEngine.GetTime();
		for (int i = 0; i < hitObjects.Count; i++)
		{
			if (hitObjects[i].StartTime > time)
			{
				return Math.Max(0, i - 1);
			}
		}
		if (hitObjects.Count <= 0)
		{
			return 0;
		}
		return hitObjects.Count - 1;
	}

	public static int GetHitObjectsCount()
	{
		return hitObjects.Count;
	}

	public static double MapDifficultyRange(double difficulty, double min, double mid, double max, bool adjustToMods)
	{
		if (adjustToMods && ((TosuService.LatestResponse != null) ? true : false))
		{
			List<string> list = ((string)TosuService.LatestResponse.play.mods.name).Select((char c) => c.ToString()).ToList();
			if (list.Contains("EZ"))
			{
				difficulty = Math.Max(0.0, difficulty * 0.5);
			}
			else if (list.Contains("HR"))
			{
				difficulty = Math.Min(10.0, difficulty * 1.4);
			}
		}
		if (difficulty > 5.0)
		{
			return mid + (max - mid) * (difficulty - 5.0) / 5.0;
		}
		if (difficulty < 5.0)
		{
			return mid - (mid - min) * (5.0 - difficulty) / 5.0;
		}
		return mid;
	}

	private static List<HitObject> GetAndTransformHitObjects()
	{
		if (TosuService.LatestResponse == null)
		{
			return new List<HitObject>();
		}
		string path = TosuService.LatestResponse.folders.songs.ToString();
		string path2 = TosuService.LatestResponse.directPath.beatmapFile.ToString();
		string path3 = Path.Combine(path, path2);
		if (!File.Exists(path3))
		{
			return new List<HitObject>();
		}
		HashSet<string> hashSet = ParseMods(TosuService.LatestResponse.play.mods.name);
		List<HitObject> list = BeatmapDecoder.Decode(path3).HitObjects;
		if (hashSet.Contains("HR"))
		{
			foreach (HitObject item in list)
			{
				item.Position = MirrorHardRockPosition(item.Position);
			}
		}
		return list;
	}

	private static Vector2 MirrorHardRockPosition(Vector2 pos)
	{
		return new Vector2(pos.X, 384f - pos.Y);
	}

	private static HashSet<string> ParseMods(string mods)
	{
		if (string.IsNullOrWhiteSpace(mods))
		{
			return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}
		return (from m in Regex.Matches(mods.ToUpperInvariant(), "[A-Z]{2}")
			select m.Value).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
	}
}
