using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using AimAssistPlugin.Sdk.Audio;
using AimAssistPlugin.Sdk.Input;
using AimAssistPlugin.Sdk.Osu;
using AimAssistPlugin.Sdk.Player;
using OsuParsers.Beatmaps.Objects;

namespace AimAssistPlugin.Services;

public class AimAssistService
{
	private readonly struct AimFrameCache
	{
		public readonly float ScaledRadiusSquared;

		public readonly float FovSquared;

		public readonly Vector2 TargetPosition;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public AimFrameCache(float scaledRadius, float fovRadius, Vector2 targetPosition)
		{
			ScaledRadiusSquared = scaledRadius * scaledRadius;
			FovSquared = fovRadius * fovRadius;
			TargetPosition = targetPosition;
		}
	}

	public static float Power = 0f;

	private static readonly Vector2 monitorOffsets = GameField.CalculateScreenOffset();

	private static int preEmpt;

	private static float hitObjectRadius;

	private static int lastAudioTime = -1;

	private static int currentIndex;

	private static HitObject? currentHitObject;

	private static HitObject? previousHitObject;

	public static void Initialize()
	{
		HitObjectManager.CacheHitObjects();
		preEmpt = HitObjectManager.GetPreEmpt();
		hitObjectRadius = HitObjectManager.GetHitObjectRadius();
		lastAudioTime = -1;
		currentIndex = 0;
		currentHitObject = HitObjectManager.GetHitObject(0);
		previousHitObject = HitObjectManager.GetHitObject(1);
		InputManager.SetAccumulatedOffset(Vector2.Zero);
		InputManager.SetLastCursorPosition(Vector2.Zero);
	}

	public static void Reset()
	{
		lastAudioTime = -1;
		currentIndex = 0;
		currentHitObject = null;
		previousHitObject = null;
	}

	private static Vector2 ApplyAimAssist(Vector2 cursorPosition)
	{
		int time = AudioEngine.GetTime();
		if (Power <= 0f)
		{
			return InputManager.GetAccumulatedOffset();
		}
		float ratio = GameField.GetRatio();
		float num = hitObjectRadius * ratio;
		float num2 = Power * 16f * ratio;
		float num3 = ((Power <= 1f) ? (Power * 0.7f) : ((Power - 1f) * 0.15f + 0.7f));
		float fovRadius = Math.Max(1.2f, 4f * num3) * num;
		HitObject hitObject = currentHitObject;
		AimFrameCache aimFrameCache = new AimFrameCache(num, fovRadius, hitObject.Position);
		Vector2 vector = InputManager.GetAccumulatedOffset();
		Vector2 vector2 = cursorPosition + vector;
		Vector2 vector3 = aimFrameCache.TargetPosition - vector2;
		float num4 = Vector2.DistanceSquared(vector2, aimFrameCache.TargetPosition);
		float num5 = ((num4 <= aimFrameCache.ScaledRadiusSquared) ? 0.65f : 1f);
		Vector2 displacement = InputManager.GetLastCursorPosition() - cursorPosition;
		if (hitObject is Spinner)
		{
			InputManager.SetAccumulatedOffset(vector);
			return vector;
		}
		bool num6 = Vector2.DistanceSquared(cursorPosition, aimFrameCache.TargetPosition) <= aimFrameCache.FovSquared;
		bool flag = time > hitObject.StartTime - preEmpt;
		if (num6 && flag)
		{
			if (hitObject is Slider slider)
			{
				float num7 = num * 0.35f;
				float num8 = num7 * num7;
				if (num4 <= num8)
				{
					float num9 = vector3.Length();
					if (num9 > float.Epsilon)
					{
						num3 *= Math.Min(num9 / num7, 1f);
					}
				}
				if (time > slider.StartTime)
				{
					num3 *= Power / 2.5f;
				}
			}
			float num10 = vector3.LengthSquared();
			float num11 = displacement.LengthSquared();
			if (num10 > float.Epsilon && num11 > float.Epsilon)
			{
				float num12 = MathF.Sqrt(num10);
				float val = MathF.Sqrt(num11);
				float num13 = Math.Min(num12, val);
				Vector2 vector4 = vector3 / num12 * (num5 * num3 * num13);
				vector += vector4;
				vector = new Vector2(Math.Clamp(vector.X, 0f - num2, num2), Math.Clamp(vector.Y, 0f - num2, num2));
			}
		}
		else
		{
			vector = InputManager.Resync(displacement, vector, 0.6f * num3);
		}
		InputManager.SetAccumulatedOffset(vector);
		return vector;
	}

	public static Vector2 GetOffset(Vector2 cursorPosition)
	{
		int time = AudioEngine.GetTime();
		if (lastAudioTime != -1 && time < lastAudioTime)
		{
			Initialize();
		}
		lastAudioTime = time;
		if (currentHitObject != null && time > currentHitObject.EndTime)
		{
			int num = currentIndex + 1;
			if (num >= HitObjectManager.GetHitObjectsCount())
			{
				Vector2 accumulatedOffset = InputManager.GetAccumulatedOffset();
				Vector2 vector = InputManager.Resync(InputManager.GetLastCursorPosition() - cursorPosition, accumulatedOffset, 0.5f);
				InputManager.SetAccumulatedOffset(vector);
				InputManager.SetLastCursorPosition(cursorPosition);
				return vector;
			}
			currentIndex = num;
			currentHitObject = HitObjectManager.GetHitObject(currentIndex);
			previousHitObject = HitObjectManager.GetHitObject(currentIndex - 1);
			currentHitObject.Position = GameField.FieldToDisplay(currentHitObject.Position) + monitorOffsets;
			previousHitObject.Position = GameField.FieldToDisplay(previousHitObject.Position) + monitorOffsets;
		}
		Vector2 accumulatedOffset2 = InputManager.GetAccumulatedOffset();
		if (currentIndex == 0)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return accumulatedOffset2;
		}
		if (((previousHitObject != null) ? (currentHitObject.StartTime - previousHitObject.EndTime) : int.MaxValue) > preEmpt * 2)
		{
			InputManager.SetLastCursorPosition(cursorPosition);
			return accumulatedOffset2;
		}
		Vector2 result = ((currentHitObject is HitCircle) ? ApplyAimAssist(cursorPosition) : ((!(currentHitObject is Slider slider)) ? accumulatedOffset2 : ((time < slider.StartTime) ? ApplyAimAssist(cursorPosition) : accumulatedOffset2)));
		InputManager.SetLastCursorPosition(cursorPosition);
		return result;
	}
}
