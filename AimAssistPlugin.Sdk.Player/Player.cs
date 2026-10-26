using AimAssistPlugin.Services;

namespace AimAssistPlugin.Sdk.Player;

internal class Player
{
	public static bool IsPlaying => (TosuService.LatestResponse?.state.name == "play") ?? ((object)false);
}
