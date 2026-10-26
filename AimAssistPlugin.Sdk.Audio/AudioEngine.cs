using AimAssistPlugin.Services;

namespace AimAssistPlugin.Sdk.Audio;

internal class AudioEngine
{
	public static int GetTime()
	{
		return TosuService.LatestPreciseResponse?.currentTime ?? ((object)0);
	}
}
