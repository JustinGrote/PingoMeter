using System.ComponentModel;
using System.Net;
using System.Runtime.Versioning;

namespace PingoMeter
{
	[SupportedOSPlatform("windows")]
	public partial class TracerouteForm : Form
	{
		private const string SparklineColumnName = "Sparkline";
		private const int SparklineMaxPoints = 30;
		private const int SparklineColumnWidth = 140;

		private TracerouteRunner? tracerouteRunner;
		private readonly IPAddress targetAddress;
		private readonly string? targetName;
		private DataGridView? hopGrid;
		private Label? statusLabel;
		private BindingList<TracerouteHop>? hopBinding;
		private readonly HashSet<TracerouteHop> hookedHops = new();

		public TracerouteForm(IPAddress targetAddress, string? targetName = null)
		{
			this.targetAddress = targetAddress;
			this.targetName = targetName ?? targetAddress.ToString();

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
				DataPropertyName = nameof(TracerouteHop.LossDisplay),
				HeaderText = "Loss%",
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
				HeaderText = "Sparkline",
				Width = SparklineColumnWidth,
				SortMode = DataGridViewColumnSortMode.NotSortable
			});
			hopGrid.CellPainting += HopGrid_CellPainting;

			Controls.Add(hopGrid);

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

			var closeButton = new Button
			{
				Text = "Close",
				Width = 100,
				Height = 30,
				Location = new Point(120, 10),
				FlatStyle = FlatStyle.System
			};
			closeButton.Click += (s, e) => Close();

			buttonPanel.Controls.Add(copyButton);
			buttonPanel.Controls.Add(closeButton);
			Controls.Add(buttonPanel);
			Controls.Add(statusLabel);

		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			// Start traceroute after form is shown
			StartTraceroute();
		}

		private void StartTraceroute()
		{
			tracerouteRunner = new TracerouteRunner(targetAddress, Config.Delay, SynchronizationContext.Current);
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
			if (hop != null && hop.Samples.Count > 0)
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
			float xStep = window.Count > 1 ? (float)width / (window.Count - 1) : width;

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
						float x1 = left + lastIndex * xStep;
						float y1 = top + height - (float)lastLatency / maxValue * height;
						float x2 = left + i * xStep;
						float y2 = top + height - (float)sample.Latency / maxValue * height;

						g.DrawLine(pen, x1, y1, x2, y2);
					}

					lastIndex = i;
					lastLatency = sample.Latency;
				}
			}

			for (int i = 0; i < window.Count; i++)
			{
				if (!window[i].IsLoss)
					continue;

				float x = left + i * xStep - 2f;
				g.FillRectangle(Brushes.Red, x, top, 4f, height);
			}

			int lastLatencyIndex = window.FindLastIndex(sample => !sample.IsLoss);
			if (lastLatencyIndex >= 0)
			{
				var lastSample = window[lastLatencyIndex];
				float lastX = left + lastLatencyIndex * xStep;
				float lastY = top + height - (float)lastSample.Latency / maxValue * height;
				g.FillEllipse(Brushes.Red, lastX - 2, lastY - 2, 4, 4);
				g.DrawEllipse(Pens.DarkRed, lastX - 2, lastY - 2, 4, 4);
			}
		}

		private void CopyToClipboard()
		{
			var sb = new System.Text.StringBuilder();
			sb.AppendLine($"Traceroute to {targetName}");
			sb.AppendLine();
			sb.AppendLine("Hop\tHost\t\tLoss%\tRecv/Sent\tMin\tAvg\tMax");
			sb.AppendLine(new string('-', 60));

			IEnumerable<TracerouteHop> hopsToCopy = hopBinding?.ToList() ?? Enumerable.Empty<TracerouteHop>();
			foreach (var hop in hopsToCopy)
			{
				string host = hop.HostDisplay;
				sb.AppendLine($"{hop.HopNumber}\t{host}\t{hop.LossDisplay}\t{hop.RecvSentDisplay}\t{hop.MinLatency}\t{hop.AvgLatency}\t{hop.MaxLatency}");
			}

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Traceroute data copied to clipboard", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			tracerouteRunner?.Stop();
			tracerouteRunner?.Dispose();
		}
	}
}
