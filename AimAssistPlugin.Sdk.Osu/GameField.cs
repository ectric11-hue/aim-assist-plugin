using System.Numerics;
using System.Runtime.InteropServices;

namespace AimAssistPlugin.Sdk.Osu;

internal class GameField
{
	private static class ScreenInfo
	{
		public static int ScreenWidth => GetSystemMetrics(0);

		public static int ScreenHeight => GetSystemMetrics(1);

		public static int VirtualX => GetSystemMetrics(76);

		public static int VirtualY => GetSystemMetrics(77);

		[DllImport("user32.dll")]
		private static extern int GetSystemMetrics(int nIndex);
	}

	public static Vector2 CalculateScreenOffset()
	{
		float x = -ScreenInfo.VirtualX;
		float y = -ScreenInfo.VirtualY;
		return new Vector2(x, y);
	}

	private static float GetWidth()
	{
		return ScreenInfo.ScreenWidth;
	}

	private static float GetHeight()
	{
		return ScreenInfo.ScreenHeight;
	}

	public static float GetRatio()
	{
		return GetHeight() * 0.8f / 384f;
	}

	private static Vector2 GetOffset()
	{
		float num = GetHeight() * 0.8f;
		float num2 = 1.3333334f * num;
		float x = (GetWidth() - num2) / 2f;
		float y = (GetHeight() - num) / 2f + 0.02f * GetHeight();
		return new Vector2(x, y);
	}

	public static Vector2 DisplayToField(Vector2 display)
	{
		return (display - GetOffset()) / GetRatio();
	}

	public static Vector2 FieldToDisplay(Vector2 field)
	{
		return field * GetRatio() + GetOffset();
	}
}
