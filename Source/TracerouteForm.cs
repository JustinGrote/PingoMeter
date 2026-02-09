using System.ComponentModel;
using System.Net;

namespace PingoMeter
{

	public partial class TracerouteForm : Form
	{
		private const string SparklineColumnName = "History";
		private const int SparklineMaxPoints = 30;
		private const int SparklineColumnWidth = 140;

		private TracerouteRunner? tracerouteRunner;
		private IPAddress targetAddress;
		private string? targetName;
		private DataGridView? hopGrid;
		private Label? statusLabel;
		private ComboBox? ipSelector;
		private BindingList<TracerouteHop>? hopBinding;
		private readonly HashSet<TracerouteHop> hookedHops = new();

		public TracerouteForm(IPAddress targetAddress, string? targetName = null)
		{
			this.targetAddress = targetAddress;
			this.targetName = targetName ?? targetAddress.ToString();
			Config.AddIPToHistory(targetAddress.ToString());

			SetupUI();
		}

		private void SetupUI()
		{
			Text = $"Traceroute - {targetName}";
			Width = 1000;
			Height = 600;
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Color.FromArgb(245, 245, 247);
			Font = new Font("Segoe UI", 9.5f);
			AutoScaleMode = AutoScaleMode.Font;

			// Top status bar
			statusLabel = new Label
			{
				Name = "statusLabel",
				Text = "Starting traceroute...",
				Dock = DockStyle.Bottom,
				Height = 30,
				BackColor = Color.FromArgb(236, 236, 238),
				Padding = new Padding(10, 5, 10, 5),
				Font = new Font("Segoe UI", 9.5f)
			};

			hopGrid = new DataGridView
			{
				Name = "hopGrid",
				Dock = DockStyle.Fill,
				ReadOnly = true,
				AllowUserToAddRows = false,
				AllowUserToDeleteRows = false,
				AllowUserToResizeRows = false,
				AllowUserToResizeColumns = true,
				SelectionMode = DataGridViewSelectionMode.CellSelect,
				MultiSelect = false,
				AutoGenerateColumns = false,
				ColumnHeadersVisible = true,
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
				RowHeadersVisible = false,
				BackgroundColor = Color.White,
				BorderStyle = BorderStyle.None,
				GridColor = Color.FromArgb(230, 230, 232),
				EnableHeadersVisualStyles = false,
				CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
				ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
			};
			hopGrid.DefaultCellStyle.BackColor = Color.White;
			hopGrid.DefaultCellStyle.ForeColor = Color.FromArgb(32, 32, 32);
			hopGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 255);
			hopGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(32, 32, 32);
			hopGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 248, 250);
			hopGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 242);
			hopGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(32, 32, 32);
			hopGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5f);

			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(TracerouteHop.HopNumber),
				HeaderText = "Hop",
				Width = 50,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(TracerouteHop.HostDisplay),
				HeaderText = "Host",
				Width = 220,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(TracerouteHop.Address),
				HeaderText = "Address",
				Width = 140,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(TracerouteHop.SuccessDisplay),
				HeaderText = "Success%",
				Width = 70,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(TracerouteHop.RecvSentDisplay),
				HeaderText = "Recv/Sent",
				Width = 90,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(TracerouteHop.MinDisplay),
				HeaderText = "Min",
				Width = 70,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(TracerouteHop.AvgDisplay),
				HeaderText = "Avg",
				Width = 70,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				DataPropertyName = nameof(TracerouteHop.MaxDisplay),
				HeaderText = "Max",
				Width = 70,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = SparklineColumnName,
				HeaderText = "History",
				Width = SparklineColumnWidth,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.CellPainting += HopGrid_CellPainting;

			// Top IP selector panel
			var topPanel = new Panel
			{
				Dock = DockStyle.Top,
				Height = 50,
				Padding = new Padding(10),
				BackColor = Color.FromArgb(245, 245, 247),
				BorderStyle = BorderStyle.FixedSingle
			};

			var ipLabel = new Label
			{
				Text = "Target IP:",
				AutoSize = true,
				Location = new Point(10, 15),
				Font = new Font("Segoe UI", 9.5f)
			};

			ipSelector = new ComboBox
			{
				Name = "ipSelector",
				Location = new Point(80, 12),
				Width = 200,
				DropDownStyle = ComboBoxStyle.DropDown,
				Font = new Font("Segoe UI", 9.5f)
			};

			foreach (var ip in Config.IPAddressHistory)
				ipSelector.Items.Add(ip);

			ipSelector.Text = targetAddress.ToString();
			ipSelector.KeyDown += IpSelector_KeyDown;

			topPanel.Controls.Add(ipLabel);
			topPanel.Controls.Add(ipSelector);

			Controls.Add(hopGrid);
			Controls.Add(topPanel);

			// Bottom button panel
			var buttonPanel = new Panel
			{
				Dock = DockStyle.Bottom,
				Height = 50,
				Padding = new Padding(10),
				BackColor = Color.FromArgb(245, 245, 247)
			};

			var copyButton = new Button
			{
				Text = "Copy",
				Width = 100,
				Height = 30,
				Location = new Point(10, 10),
				FlatStyle = FlatStyle.System
			};
			copyButton.Click += (s, e) => CopyToClipboard();

			var settingsButton = new Button
			{
				Text = "Settings",
				Width = 100,
				Height = 30,
				Location = new Point(copyButton.Right + 10, 10),
				FlatStyle = FlatStyle.System
			};
			settingsButton.Click += (s, e) => ShowSettings();

			buttonPanel.Controls.Add(copyButton);
			buttonPanel.Controls.Add(settingsButton);
			Controls.Add(buttonPanel);
			Controls.Add(statusLabel);

		}

		private void IpSelector_KeyDown(object? sender, KeyEventArgs e)
		{
			if (e.KeyCode != Keys.Return)
				return;

			e.Handled = true;

			if (ipSelector == null || string.IsNullOrWhiteSpace(ipSelector.Text))
				return;

			string input = ipSelector.Text.Trim();
			IPAddress? newAddress = null;
			string? resolvedName = input;

			// Try to parse as IP address first
			if (IPAddress.TryParse(input, out IPAddress? parsedAddress) && parsedAddress != null)
			{
				newAddress = parsedAddress;
				resolvedName = input;
			}
			else
			{
				// Try to resolve as DNS name
				try
				{
					var hostEntry = Dns.GetHostEntry(input);
					if (hostEntry.AddressList.Length > 0)
					{
						// Use the first IPv4 address, or first address if no IPv4
						newAddress = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
							?? hostEntry.AddressList[0];
						resolvedName = input;
					}
				}
				catch
				{
					// DNS resolution failed
					newAddress = null;
				}
			}

			if (newAddress == null)
			{
				MessageBox.Show($"Invalid IP address or could not resolve DNS name: {input}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			if (newAddress.Equals(targetAddress))
				return;

			targetAddress = newAddress;
			targetName = resolvedName;
			Text = $"Traceroute - {targetName}";

			Config.AddIPToHistory(newAddress.ToString());

			if (!ipSelector.Items.Contains(input))
				ipSelector.Items.Insert(0, input);
			if (hopBinding != null)
				hopBinding.Clear();

			StartTraceroute();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Start traceroute after form is shown
			StartTraceroute();
		}

		private void StartTraceroute()
		{
			tracerouteRunner = new TracerouteRunner(targetAddress, Config.Interval, SynchronizationContext.Current);
			hopBinding = tracerouteRunner.Hops;
			if (hopGrid != null)
			{
				hopGrid.DataSource = hopBinding;
				HookHopBinding();
			}

			tracerouteRunner.StatusChanged += (s, status) =>
			{
				if (statusLabel == null || statusLabel.IsDisposed)
					return;
				if (statusLabel.InvokeRequired)
					statusLabel.BeginInvoke(new Action(() => statusLabel.Text = status));
				else
					statusLabel.Text = status;
			};

			try
			{
				tracerouteRunner.Start();
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Traceroute error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void HookHopBinding()
		{
			if (hopBinding == null)
				return;

			foreach (var hop in hopBinding)
			{
				AttachHop(hop);
			}

			hopBinding.ListChanged += (s, e) =>
			{
				if (hopBinding == null)
					return;
				if (e.ListChangedType == ListChangedType.ItemAdded && e.NewIndex >= 0 && e.NewIndex < hopBinding.Count)
				{
					AttachHop(hopBinding[e.NewIndex]);
				}
				else if (e.ListChangedType == ListChangedType.Reset)
				{
					hookedHops.Clear();
					foreach (var hop in hopBinding)
					{
						AttachHop(hop);
					}
				}

				InvalidateHopGrid();
			};
		}

		private void AttachHop(TracerouteHop hop)
		{
			if (hookedHops.Contains(hop))
				return;

			hookedHops.Add(hop);
			hop.PropertyChanged += Hop_PropertyChanged;
		}

		private void Hop_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			InvalidateHopGrid();
		}

		private void InvalidateHopGrid()
		{
			if (hopGrid == null || hopGrid.IsDisposed)
				return;
			if (hopGrid.InvokeRequired)
				hopGrid.BeginInvoke(new Action(() => hopGrid.Invalidate()));
			else
				hopGrid.Invalidate();
		}

		private void HopGrid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
		{
			if (hopGrid == null)
				return;
			if (e.RowIndex < 0)
				return;
			if (hopGrid.Columns[e.ColumnIndex].Name != SparklineColumnName)
				return;

			e.PaintBackground(e.CellBounds, true);
			var hop = hopGrid.Rows[e.RowIndex].DataBoundItem as TracerouteHop;
			if (hop != null && hop.Samples.Count > 0 && e.Graphics != null)
			{
				DrawSparkline(e.Graphics, hop.Samples, e.CellBounds);
			}
			e.Handled = true;
		}

		private void DrawSparkline(Graphics g, List<TracerouteHop.TracerouteProbeSample> samples, Rectangle bounds)
		{
			int left = bounds.X + 4;
			int top = bounds.Y + 4;
			int width = Math.Max(0, bounds.Width - 8);
			int height = Math.Max(0, bounds.Height - 8);

			if (width < 4 || height < 4)
				return;

			var window = samples.Skip(Math.Max(0, samples.Count - SparklineMaxPoints)).ToList();
			if (window.Count == 0)
				return;

			var latencyValues = window.Where(s => !s.IsLoss).Select(s => s.Latency).ToList();
			long maxValue = latencyValues.Count > 0 ? Math.Max(latencyValues.Max(), 1) : 1;
			int maxPoints = SparklineMaxPoints;
			int offsetPoints = Math.Max(0, maxPoints - window.Count);
			float xStep = maxPoints > 1 ? (float)width / (maxPoints - 1) : width;

			using (var pen = new Pen(Color.FromArgb(66, 135, 245), 1.5f))
			{
				int lastIndex = -1;
				long lastLatency = 0;
				for (int i = 0; i < window.Count; i++)
				{
					var sample = window[i];
					if (sample.IsLoss)
						continue;

					if (lastIndex >= 0)
					{
						float x1 = left + (offsetPoints + lastIndex) * xStep;
						float y1 = top + height - (float)lastLatency / maxValue * height;
						float x2 = left + (offsetPoints + i) * xStep;
						float y2 = top + height - (float)sample.Latency / maxValue * height;

						g.DrawLine(pen, x1, y1, x2, y2);
					}

					lastIndex = i;
					lastLatency = sample.Latency;
				}
			}

			int segmentCount = window.Count;
			int lossStart = -1;
			for (int i = 0; i < segmentCount; i++)
			{
				bool isLoss = window[i].IsLoss;
				if (isLoss && lossStart == -1)
					lossStart = i;
				if (!isLoss && lossStart != -1)
				{
					FillLossRun(g, left, top, width, height, maxPoints, offsetPoints, lossStart, i - 1);
					lossStart = -1;
				}
			}
			if (lossStart != -1)
				FillLossRun(g, left, top, width, height, maxPoints, offsetPoints, lossStart, segmentCount - 1);

			int lastLatencyIndex = window.FindLastIndex(sample => !sample.IsLoss);
			if (lastLatencyIndex >= 0)
			{
				var lastSample = window[lastLatencyIndex];
				float lastX = left + (offsetPoints + lastLatencyIndex) * xStep;
				float lastY = top + height - (float)lastSample.Latency / maxValue * height;
				g.FillEllipse(Brushes.LimeGreen, lastX - 2, lastY - 2, 4, 4);
				g.DrawEllipse(Pens.DarkGreen, lastX - 2, lastY - 2, 4, 4);
			}
		}

		private static void FillLossRun(Graphics g, int left, int top, int width, int height, int maxPoints, int offsetPoints, int startIndex, int endIndex)
		{
			double segmentWidth = maxPoints > 0 ? (double)width / maxPoints : width;
			int xStart = left + (int)Math.Floor((offsetPoints + startIndex) * segmentWidth);
			int xEnd = left + (int)Math.Ceiling((offsetPoints + endIndex + 1) * segmentWidth);
			int clampedEnd = Math.Min(left + width, xEnd);
			int fillWidth = Math.Max(1, clampedEnd - xStart);
			g.FillRectangle(Brushes.Red, xStart, top, fillWidth, height);
		}

		private void CopyToClipboard()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"Traceroute to {targetName}");
			sb.AppendLine();
			sb.AppendLine("Hop\tHost\t\tSuccess%\tRecv/Sent\tMin\tAvg\tMax");
			sb.AppendLine(new string('-', 60));

			IEnumerable<TracerouteHop> hopsToCopy = hopBinding?.ToList() ?? Enumerable.Empty<TracerouteHop>();
			foreach (var hop in hopsToCopy)
			{
				string host = hop.HostDisplay;
				sb.AppendLine($"{hop.HopNumber}\t{host}\t{hop.SuccessDisplay}\t{hop.RecvSentDisplay}\t{hop.MinLatency}\t{hop.AvgLatency}\t{hop.MaxLatency}");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Traceroute data copied to clipboard", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ShowSettings()
		{
			using (var settingsForm = new Setting())
			{
				settingsForm.ShowDialog(this);
			}
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			tracerouteRunner?.Stop();
			tracerouteRunner?.Dispose();
		}
	}
}
