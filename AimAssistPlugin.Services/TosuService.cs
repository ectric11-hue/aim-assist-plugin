using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AimAssistPlugin.Services;

public class TosuService
{
	private static readonly HttpClient HttpClient = new HttpClient();

	private static readonly TimeSpan UpdateInterval = TimeSpan.FromMilliseconds(1.0);

	private static readonly SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);

	private static bool _osuRunning = false;

	private static bool _tosuRunning = false;

	private static bool _isRunning = false;

	private static bool _isInitializing = false;

	private static dynamic? _latestResponse = null;

	private static dynamic? _latestPreciseResponse = null;

	private static CancellationTokenSource _cts = new CancellationTokenSource();

	private static Task? _updateTask;

	public static bool IsRunning
	{
		get
		{
			return _isRunning;
		}
		private set
		{
			_isRunning = value;
		}
	}

	public static dynamic? LatestResponse
	{
		get
		{
			return _latestResponse;
		}
		private set
		{
			_latestResponse = value;
		}
	}

	public static dynamic? LatestPreciseResponse
	{
		get
		{
			return _latestPreciseResponse;
		}
		private set
		{
			_latestPreciseResponse = value;
		}
	}

	public static void Start()
	{
		_cts = new CancellationTokenSource();
		Task.Run(() => MonitorProcessesAsync(_cts.Token));
		_updateTask = Task.Run(() => UpdateTosuDataAsync(_cts.Token));
		IsRunning = true;
	}

	private static async Task MonitorProcessesAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			if (_isInitializing)
			{
				await Task.Delay(1000, ct);
				continue;
			}
			Process process = Process.GetProcessesByName("osu!").FirstOrDefault();
			Process process2 = Process.GetProcessesByName("tosu").FirstOrDefault();
			_osuRunning = process != null;
			_tosuRunning = process2 != null;
			if (_osuRunning && !_tosuRunning)
			{
				await StartTosuAsync();
			}
			else if (!_osuRunning && _tosuRunning)
			{
				try
				{
					process2?.Kill();
				}
				catch
				{
				}
			}
			await Task.Delay(2000, ct);
		}
	}

	private static async Task UpdateTosuDataAsync(CancellationToken ct)
	{
		while (!ct.IsCancellationRequested)
		{
			if (_osuRunning && _tosuRunning)
			{
				try
				{
					Task<string> task1 = HttpClient.GetStringAsync("http://localhost:24050/json/v2", ct);
					Task<string> task2 = HttpClient.GetStringAsync("http://localhost:24050/json/v2/precise", ct);
					await Task.WhenAll<string>(task1, task2);
					LatestResponse = JsonConvert.DeserializeObject(task1.Result);
					LatestPreciseResponse = JsonConvert.DeserializeObject(task2.Result);
				}
				catch
				{
					LatestResponse = null;
					LatestPreciseResponse = null;
				}
			}
			await Task.Delay(UpdateInterval, ct);
		}
	}

	private static async Task StartTosuAsync()
	{
		if (!(await _startLock.WaitAsync(0)))
		{
			return;
		}
		_isInitializing = true;
		string text = Path.Combine(AppContext.BaseDirectory, "userdata");
		string pluginFolder = (Directory.Exists(text) ? Path.Combine(text, "Plugins", "Reconstructor") : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenTabletDriver", "Plugins", "Reconstructor"));
		if (!Directory.Exists(pluginFolder))
		{
			return;
		}
		string tosuPath = Path.Combine(pluginFolder, "tosu.exe");
		string tosuConfigPath = Path.Combine(pluginFolder, "tosu.env");
		if (!File.Exists(tosuPath))
		{
			await DownloadLatestTosuAsync(tosuPath);
		}
		if (!File.Exists(tosuConfigPath))
		{
			await File.WriteAllTextAsync(tosuConfigPath, "DEBUG_LOG=false\r\nENABLE_AUTOUPDATE=false\r\nOPEN_DASHBOARD_ON_STARTUP=false\r\n\r\nSHOW_MP_COMMANDS=false\r\nCALCULATE_PP=false\r\n\r\nENABLE_KEY_OVERLAY=false\r\nENABLE_INGAME_OVERLAY=false\r\n\r\nPOLL_RATE=15\r\nPRECISE_DATA_POLL_RATE=15\r\n\r\nINGAME_OVERLAY_KEYBIND=Control + Shift + Space\r\nINGAME_OVERLAY_MAX_FPS=60\r\n\r\nSERVER_IP=127.0.0.1\r\nSERVER_PORT=24050\r\nALLOWED_IPS=127.0.0.1,localhost,absolute\r\n\r\nSTATIC_FOLDER_PATH=./static");
		}
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = tosuPath,
				WorkingDirectory = pluginFolder
			});
			await Task.Delay(500);
		}
		finally
		{
			_startLock.Release();
			_isInitializing = false;
		}
	}

	private static async Task DownloadLatestTosuAsync(string tosuPath)
	{
		_ = 3;
		try
		{
			using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/tosuapp/tosu/releases/latest");
			request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TosuAutoUpdater", "1.0"));
			using HttpResponseMessage response = await HttpClient.SendAsync(request);
			response.EnsureSuccessStatusCode();
			JToken obj = JObject.Parse(await response.Content.ReadAsStringAsync())["assets"];
			JToken val = ((IEnumerable<JToken>)((obj is JArray) ? obj : null))?.FirstOrDefault(delegate(JToken a)
			{
				JToken obj2 = a[(object)"name"];
				return obj2 != null && ((object)obj2).ToString().Contains("windows", StringComparison.OrdinalIgnoreCase) && (((object)a[(object)"name"])?.ToString().EndsWith(".zip") ?? false);
			});
			if (val == null)
			{
				return;
			}
			string requestUri = ((object)val[(object)"browser_download_url"])?.ToString() ?? string.Empty;
			string tempZip = Path.Combine(Path.GetTempPath(), "tosu_latest.zip");
			await File.WriteAllBytesAsync(tempZip, await HttpClient.GetByteArrayAsync(requestUri));
			using (ZipArchive zipArchive = ZipFile.OpenRead(tempZip))
			{
				foreach (ZipArchiveEntry entry in zipArchive.Entries)
				{
					if (entry.Name.Equals("tosu.exe", StringComparison.OrdinalIgnoreCase))
					{
						entry.ExtractToFile(tosuPath, overwrite: true);
						break;
					}
				}
			}
			File.Delete(tempZip);
		}
		catch (Exception)
		{
		}
	}
}
