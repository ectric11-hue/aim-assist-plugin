using System;
using System.Numerics;
using AimAssistPlugin.Sdk.Input;
using AimAssistPlugin.Sdk.Player;
using AimAssistPlugin.Services;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace AimAssistPlugin;

[PluginName("Reconstructor")]
public class AimAssistPlugin : IPositionedPipelineElement<IDeviceReport>, IPipelineElement<IDeviceReport>
{
	private readonly object _syncLock = new object();

	public PipelinePosition Position => (PipelinePosition)2;

	[Property("EMA Weight")]
	[DefaultPropertyValue(0.5f)]
	[ToolTip("Default: 0.5\n\nDefines the weight of the latest sample against previous ones [Range: 0.0 - 1.0]\n  Lower == More hardware smoothing removed\n  1 == No effect")]
	public float Power
	{
		get
		{
			return AimAssistService.Power;
		}
		set
		{
			AimAssistService.Power = value;
		}
	}

	public event Action<IDeviceReport>? Emit;

	private static float RoundOff(float val)
	{
		return (float)Math.Floor(val + 0.5f * (float)Math.Sign(val));
	}

	private static Vector2 RoundVec(Vector2 v)
	{
		return new Vector2(RoundOff(v.X), RoundOff(v.Y));
	}

	public void Consume(IDeviceReport report)
	{
		ITabletReport val = (ITabletReport)(object)((report is ITabletReport) ? report : null);
		if (val == null)
		{
			this.Emit?.Invoke(report);
			return;
		}
		if (!TosuService.IsRunning)
		{
			TosuService.Start();
		}
		if (Player.IsPlaying)
		{
			if (HitObjectManager.hitObjects.Count == 0)
			{
				AimAssistService.Initialize();
			}
			lock (_syncLock)
			{
				try
				{
					Vector2 offset = AimAssistService.GetOffset(((IAbsolutePositionReport)val).Position);
					Vector2 v = ((IAbsolutePositionReport)val).Position + offset;
					((IAbsolutePositionReport)val).Position = RoundVec(v);
					return;
				}
				catch (Exception ex)
				{
					Log.Write("Aim Assist", "Error: " + ex.Message, (LogLevel)3, false, false);
					return;
				}
				finally
				{
					this.Emit?.Invoke((IDeviceReport)(object)val);
				}
			}
		}
		HitObjectManager.hitObjects.Clear();
		Vector2 lastCursorPosition = InputManager.GetLastCursorPosition();
		Vector2 accumulatedOffset = InputManager.GetAccumulatedOffset();
		AimAssistService.Reset();
		Vector2 position = ((IAbsolutePositionReport)val).Position;
		if (lastCursorPosition != Vector2.Zero)
		{
			InputManager.SetAccumulatedOffset(InputManager.Resync(lastCursorPosition - position, accumulatedOffset, 0.5f));
		}
		Vector2 vector = RoundVec(InputManager.GetAccumulatedOffset());
		((IAbsolutePositionReport)val).Position = ((IAbsolutePositionReport)val).Position + vector;
		this.Emit?.Invoke((IDeviceReport)(object)val);
	}
}
