using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Versioning;

namespace PingoMeter
{
	/// <summary>
	/// Result from probing a single hop once.
	/// </summary>
	public class TracerouteHopResult
	{
		public int HopNumber { get; set; }
		public IPAddress? Address { get; set; }
		public long Latency { get; set; }
		public bool IsComplete { get; set; }
		public bool IsTimeout { get; set; }
	}

	/// <summary>
	/// Represents a single hop in the traceroute path.
	/// </summary>
	[SupportedOSPlatform("windows")]
	public class TracerouteHop : INotifyPropertyChanged
	{
		public readonly struct TracerouteProbeSample
		{
			public TracerouteProbeSample(long latency, bool isLoss)
			{
				Latency = latency;
				IsLoss = isLoss;
			}

			public long Latency { get; }
			public bool IsLoss { get; }
		}

		private string? hostName;
		private IPAddress? address;
		private int timeOuts;
		private int packetsLost;
		private long minLatency = long.MaxValue;
		private long maxLatency;
		private float lossPercentage;

		public int HopNumber { get; set; }
		public string? HostName
		{
			get => hostName;
			set
			{
				if (hostName == value)
					return;
				hostName = value;
				OnPropertyChanged(nameof(HostName));
				OnPropertyChanged(nameof(HostDisplay));
			}
		}
		public IPAddress? Address
		{
			get => address;
			set
			{
				if (Equals(address, value))
					return;
				address = value;
				OnPropertyChanged(nameof(Address));
				OnPropertyChanged(nameof(HostDisplay));
			}
		}
		public List<long> Latencies { get; } = new();
		public List<TracerouteProbeSample> Samples { get; } = new();
		public int TimeOuts
		{
			get => timeOuts;
			set
			{
				if (timeOuts == value)
					return;
				timeOuts = value;
				OnPropertyChanged(nameof(TimeOuts));
			}
		}
		public int PacketsLost
		{
			get => packetsLost;
			set
			{
				if (packetsLost == value)
					return;
				packetsLost = value;
				OnPropertyChanged(nameof(PacketsLost));
			}
		}
		public long MinLatency
		{
			get => minLatency;
			set
			{
				if (minLatency == value)
					return;
				minLatency = value;
				OnPropertyChanged(nameof(MinLatency));
				OnPropertyChanged(nameof(MinDisplay));
			}
		}
		public long MaxLatency
		{
			get => maxLatency;
			set
			{
				if (maxLatency == value)
					return;
				maxLatency = value;
				OnPropertyChanged(nameof(MaxLatency));
				OnPropertyChanged(nameof(MaxDisplay));
			}
		}
		public long AvgLatency => Latencies.Any() ? (long)Latencies.Average() : 0;
		public float LossPercentage
		{
			get => lossPercentage;
			set
			{
				if (Math.Abs(lossPercentage - value) < 0.01f)
					return;
				lossPercentage = value;
				OnPropertyChanged(nameof(LossPercentage));
				OnPropertyChanged(nameof(LossDisplay));
			}
		}

		public string HostDisplay => HostName ?? (Address?.ToString() ?? "(no response)");
		public string LossDisplay => $"{LossPercentage:F1}%";
		public string RecvSentDisplay
		{
			get
			{
				int sentCount = Latencies.Count + PacketsLost;
				int recvCount = Latencies.Count;
				return sentCount > 0 ? $"{recvCount}/{sentCount}" : "---";
			}
		}
		public string MinDisplay => MinLatency == long.MaxValue ? "---" : $"{MinLatency}ms";
		public string AvgDisplay => AvgLatency > 0 ? $"{AvgLatency}ms" : "---";
		public string MaxDisplay => MaxLatency > 0 ? $"{MaxLatency}ms" : "---";

		public event PropertyChangedEventHandler? PropertyChanged;

		public void NotifyCalculatedFields()
		{
			OnPropertyChanged(nameof(AvgLatency));
			OnPropertyChanged(nameof(RecvSentDisplay));
			OnPropertyChanged(nameof(AvgDisplay));
			OnPropertyChanged(nameof(MinDisplay));
			OnPropertyChanged(nameof(MaxDisplay));
			OnPropertyChanged(nameof(LossDisplay));
		}

		public void NotifySamplesChanged()
		{
			OnPropertyChanged(nameof(Samples));
		}

		private void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}

	/// <summary>
	/// Async ICMP-based traceroute runner using TTL to discover network path.
	/// Each hop has its own refresh task for real-time updates.
	/// </summary>
	[SupportedOSPlatform("windows")]
	public class TracerouteRunner : IDisposable
	{
		private readonly IPAddress targetAddress;
		private readonly int maxHops = 30;
		private readonly int timeoutMs;
		private readonly int intervalMs;
		private readonly byte[] buffer = new byte[32];
		private readonly object targetLock = new object();
		private readonly object hopTasksLock = new object();
		private readonly object hopUpdateLock = new object();
		private readonly SynchronizationContext? syncContext;
		private readonly HashSet<IPAddress> resolvingHosts = new HashSet<IPAddress>();
		private readonly Dictionary<int, Task> hopTasks = new Dictionary<int, Task>();
		private readonly Dictionary<int, CancellationTokenSource> hopCts = new Dictionary<int, CancellationTokenSource>();
		private volatile bool isRunning = false;
		private CancellationTokenSource? cts;
		private BindingList<TracerouteHop> hops;
		private int? reachedTargetHop;

		public event EventHandler<string>? StatusChanged;

		public TracerouteRunner(IPAddress targetAddress, int? intervalMs = null, SynchronizationContext? syncContext = null)
		{
			this.targetAddress = targetAddress;
			timeoutMs = Config.Timeout;
			this.intervalMs = Math.Max(200, intervalMs ?? Config.Interval);
			this.syncContext = syncContext ?? SynchronizationContext.Current;
			hops = new BindingList<TracerouteHop>();
		}

		public bool IsRunning => isRunning;
		public BindingList<TracerouteHop> Hops => hops;

		/// <summary>
		/// Start the persistent traceroute. Each hop runs in its own loop.
		/// Sends an initial ping to estimate hop count before starting parallel traceroute.
		/// </summary>
		public void Start()
		{
			if (isRunning)
				return;

			isRunning = true;
			cts = new CancellationTokenSource();
			reachedTargetHop = null;

			NotifyStatusChanged("Starting traceroute...");
			ExecuteHopUpdate(SeedInitialPlaceholder);
			_ = Task.Run(() => StartWithInitialPingAsync(cts.Token));
		}

		private async Task StartWithInitialPingAsync(CancellationToken ct)
		{
			try
			{
				NotifyStatusChanged("Sending initial ping to estimate hop count...");
				var initialPing = await SendInitialPingAsync(ct);

				if (ct.IsCancellationRequested)
					return;

				NotifyStatusChanged($"Estimated {initialPing.EstimatedHops} hops, starting traceroute...");
				ExecuteHopUpdate(() => PromoteInitialHop(initialPing.Result, initialPing.EstimatedHops));

				for (int hop = 1; hop <= initialPing.EstimatedHops; hop++)
				{
					if (ct.IsCancellationRequested)
						return;

					int hopNumber = hop;
					var hopToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
					lock (hopTasksLock)
					{
						hopCts[hopNumber] = hopToken;
						hopTasks[hopNumber] = Task.Run(() => RunHopLoopAsync(hopNumber, hopToken.Token));
					}
				}
			}
			catch (Exception ex)
			{
				NotifyStatusChanged($"Error during startup: {ex.Message}");
				var fallbackResult = new TracerouteHopResult
				{
					HopNumber = 0,
					Address = null,
					Latency = 0,
					IsComplete = false,
					IsTimeout = true
				};

				ExecuteHopUpdate(() => PromoteInitialHop(fallbackResult, maxHops));
				for (int hop = 1; hop <= maxHops; hop++)
				{
					if (ct.IsCancellationRequested)
						return;

					int hopNumber = hop;
					var hopToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
					lock (hopTasksLock)
					{
						hopCts[hopNumber] = hopToken;
						hopTasks[hopNumber] = Task.Run(() => RunHopLoopAsync(hopNumber, hopToken.Token));
					}
				}
			}
		}

		/// <summary>
		/// Send a single ping to estimate the number of hops to the target.
		/// Uses the TTL of the reply to calculate hop count and returns the ping result.
		/// </summary>
		private async Task<(TracerouteHopResult Result, int EstimatedHops)> SendInitialPingAsync(CancellationToken ct)
		{
			using var ping = new Ping();
			// Use a high TTL to ensure the ping reaches the destination
			PingOptions options = new PingOptions(128, false);
			TracerouteHopResult result;

			try
			{
				var stopwatch = Stopwatch.StartNew();
				PingReply reply = await ping.SendPingAsync(targetAddress, timeoutMs, buffer, options);
				stopwatch.Stop();

				result = new TracerouteHopResult
				{
					HopNumber = 0,
					Address = reply.Address,
					Latency = stopwatch.ElapsedMilliseconds,
					IsComplete = reply.Status == IPStatus.Success,
					IsTimeout = reply.Status == IPStatus.TimedOut
				};

				if (reply.Status == IPStatus.Success && reply.Options != null)
				{
					// The reply TTL is the remaining TTL from the response packet
					int replyTtl = reply.Options.Ttl;

					// Estimate initial TTL from common values (64, 128, 255)
					int estimatedInitialTtl;
					if (replyTtl <= 64)
						estimatedInitialTtl = 64;
					else if (replyTtl <= 128)
						estimatedInitialTtl = 128;
					else
						estimatedInitialTtl = 255;

					int estimatedHops = estimatedInitialTtl - replyTtl;

					// Add a small buffer (3 hops) in case our estimate is slightly off
					// and ensure we have at least 1 hop and don't exceed maxHops
					return (result, Math.Max(1, Math.Min(maxHops, estimatedHops + 3)));
				}
			}
			catch
			{
				result = new TracerouteHopResult
				{
					HopNumber = 0,
					Address = null,
					Latency = 0,
					IsComplete = false,
					IsTimeout = true
				};
			}

			// Default to full range if estimation fails
			return (result, maxHops);
		}

		private void SeedInitialPlaceholder()
		{
			if (hops.Count > 0)
				return;

			hops.Add(new TracerouteHop
			{
				HopNumber = 0,
				HostName = "Initial ping"
			});
		}

		private void PromoteInitialHop(TracerouteHopResult result, int hopCount)
		{
			hops.Clear();
			for (int hop = 1; hop <= hopCount; hop++)
			{
				hops.Add(new TracerouteHop { HopNumber = hop });
			}

			var promoted = result;
			promoted.HopNumber = hopCount;
			ApplyHopResult(promoted);
		}

		/// <summary>
		/// Refresh a single hop immediately.
		/// </summary>
		public Task RefreshHopAsync(int hop)
		{
			if (!isRunning || cts == null)
				return Task.CompletedTask;

			return RefreshHopAsync(hop, cts.Token);
		}

		private async Task RunHopLoopAsync(int hop, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				if (ShouldStopHop(hop))
					return;

				var stopwatch = Stopwatch.StartNew();
				await RefreshHopAsync(hop, ct);
				stopwatch.Stop();

				long elapsedMs = stopwatch.ElapsedMilliseconds;
				long remainingMs = Math.Max(0, intervalMs - elapsedMs);

				try
				{
					await Task.Delay((int)remainingMs, ct);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}

		private async Task RefreshHopAsync(int hop, CancellationToken ct)
		{
			if (ShouldStopHop(hop))
				return;

			TracerouteHopResult result = await ProbeHopOnceAsync(hop, ct);
			if (ShouldStopHop(hop))
				return;
			ExecuteHopUpdate(() =>
			{
				if (ShouldStopHop(result.HopNumber))
					return;
				ApplyHopResult(result);
				HandleTargetReached(result);
			});
		}

		private bool ShouldStopHop(int hop)
		{
			int? targetHop;
			lock (targetLock)
			{
				targetHop = reachedTargetHop;
			}

			return targetHop.HasValue && hop > targetHop.Value;
		}

		private void HandleTargetReached(TracerouteHopResult result)
		{
			if (result.Address == null)
				return;

			if (!result.Address.Equals(targetAddress))
				return;

			bool shouldNotify = false;
			lock (targetLock)
			{
				if (!reachedTargetHop.HasValue || result.HopNumber < reachedTargetHop.Value)
				{
					reachedTargetHop = result.HopNumber;
					shouldNotify = true;
				}
			}

			if (shouldNotify)
			{
				CancelHopsBeyondTarget(result.HopNumber);
				TrimHopsBeyondTarget(result.HopNumber);
				NotifyStatusChanged($"Target reached at hop {result.HopNumber}");
			}
		}

		private void CancelHopsBeyondTarget(int targetHop)
		{
			List<(int Hop, Task Task, CancellationTokenSource Cts)> canceled;

			lock (hopTasksLock)
			{
				canceled = hopCts
					.Where(entry => entry.Key > targetHop)
					.Select(entry => (entry.Key, hopTasks[entry.Key], entry.Value))
					.ToList();
			}

			foreach (var item in canceled)
			{
				item.Cts.Cancel();
			}

			_ = Task.Run(async () =>
			{
				try
				{
					await Task.WhenAll(canceled.Select(item => item.Task));
				}
				catch
				{
					// Ignore task cancellations and failures during teardown.
				}
				finally
				{
					lock (hopTasksLock)
					{
						foreach (var item in canceled)
						{
							hopTasks.Remove(item.Hop);
							hopCts.Remove(item.Hop);
							item.Cts.Dispose();
						}
					}
				}
			});
		}

		private void TrimHopsBeyondTarget(int targetHop)
		{
			for (int i = hops.Count - 1; i >= 0; i--)
			{
				if (hops[i].HopNumber > targetHop)
					hops.RemoveAt(i);
			}
		}

		/// <summary>
		/// Probe a single hop exactly once with a single ICMP echo request.
		/// </summary>
		private async Task<TracerouteHopResult> ProbeHopOnceAsync(int ttl, CancellationToken ct)
		{
			using var ping = new Ping();
			PingOptions options = new PingOptions(ttl, false);

			try
			{
				var stopwatch = Stopwatch.StartNew();
				PingReply reply = await ping.SendPingAsync(targetAddress, timeoutMs, buffer, options);
				stopwatch.Stop();

				return new TracerouteHopResult
				{
					HopNumber = ttl,
					Address = reply.Address,
					Latency = stopwatch.ElapsedMilliseconds,
					IsComplete = reply.Status == IPStatus.Success,
					IsTimeout = reply.Status == IPStatus.TimedOut
				};
			}
			catch (PingException)
			{
				return new TracerouteHopResult
				{
					HopNumber = ttl,
					Address = null,
					Latency = 0,
					IsComplete = false,
					IsTimeout = true
				};
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception)
			{
				return new TracerouteHopResult
				{
					HopNumber = ttl,
					Address = null,
					Latency = 0,
					IsComplete = false,
					IsTimeout = true
				};
			}
		}

		private void ApplyHopResult(TracerouteHopResult result)
		{
			if (ShouldStopHop(result.HopNumber))
				return;

			TracerouteHop? hopData = hops.FirstOrDefault(h => h.HopNumber == result.HopNumber);
			if (hopData == null)
			{
				hopData = new TracerouteHop { HopNumber = result.HopNumber };
				hops.Add(hopData);
			}

			if (result.Address != null && hopData.Address == null)
			{
				hopData.Address = result.Address;
				TryResolveHostName(result.Address);
			}


			if (result.IsTimeout)
			{
				hopData.TimeOuts++;
				hopData.PacketsLost++;
				hopData.Samples.Add(new TracerouteHop.TracerouteProbeSample(0, true));
			}
			else
			{
				hopData.Latencies.Add(result.Latency);
				hopData.Samples.Add(new TracerouteHop.TracerouteProbeSample(result.Latency, false));
				if (result.Latency < hopData.MinLatency)
					hopData.MinLatency = result.Latency;
				if (result.Latency > hopData.MaxLatency)
					hopData.MaxLatency = result.Latency;
			}

			int totalProbes = hopData.Latencies.Count + hopData.PacketsLost;
			hopData.LossPercentage = totalProbes > 0 ? ((float)hopData.PacketsLost / totalProbes) * 100f : 0;
			hopData.NotifyCalculatedFields();
			hopData.NotifySamplesChanged();
		}

		private void TryResolveHostName(IPAddress address)
		{
			if (resolvingHosts.Contains(address))
				return;

			resolvingHosts.Add(address);
			_ = ResolveHostNameAsync(address);
		}

		private async Task ResolveHostNameAsync(IPAddress address)
		{
			try
			{
				var entry = await Dns.GetHostEntryAsync(address).ConfigureAwait(false);
				string? hostName = entry.HostName;
				if (!string.IsNullOrWhiteSpace(hostName))
				{
					ExecuteHopUpdate(() => ApplyResolvedHostName(address, hostName));
				}
			}
			catch
			{
				// Ignore DNS resolution failures.
			}
			finally
			{
				ExecuteHopUpdate(() => resolvingHosts.Remove(address));
			}
		}

		private void ApplyResolvedHostName(IPAddress address, string hostName)
		{
			foreach (var hop in hops)
			{
				if (hop.Address == null)
					continue;
				if (!hop.Address.Equals(address))
					continue;
				if (!string.IsNullOrEmpty(hop.HostName))
					continue;

				hop.HostName = hostName;
			}
		}

		private void ExecuteHopUpdate(Action action)
		{
			if (syncContext == null || SynchronizationContext.Current == syncContext)
			{
				lock (hopUpdateLock)
				{
					action();
				}
				return;
			}

			syncContext.Post(_ =>
			{
				lock (hopUpdateLock)
				{
					action();
				}
			}, null);
		}

		private void NotifyStatusChanged(string status)
		{
			if (syncContext == null || SynchronizationContext.Current == syncContext)
			{
				StatusChanged?.Invoke(this, status);
				return;
			}

			syncContext.Post(_ => StatusChanged?.Invoke(this, status), null);
		}

		/// <summary>
		/// Stop the traceroute.
		/// </summary>
		public void Stop()
		{
			if (!isRunning)
				return;

			isRunning = false;
			cts?.Cancel();
			_ = StopAsync();
		}

		public async Task StopAsync()
		{
			List<(int Hop, Task Task, CancellationTokenSource Cts)> active;

			lock (hopTasksLock)
			{
				active = hopCts
					.Select(entry => (entry.Key, hopTasks[entry.Key], entry.Value))
					.ToList();
			}

			foreach (var item in active)
			{
				item.Cts.Cancel();
			}

			try
			{
				await Task.WhenAll(active.Select(item => item.Task));
			}
			catch
			{
				// Ignore task cancellations and failures during teardown.
			}
			finally
			{
				lock (hopTasksLock)
				{
					foreach (var item in active)
					{
						hopTasks.Remove(item.Hop);
						hopCts.Remove(item.Hop);
						item.Cts.Dispose();
					}
				}
			}
		}

		public void Dispose()
		{
			Stop();
			cts?.Dispose();
		}
	}
}
