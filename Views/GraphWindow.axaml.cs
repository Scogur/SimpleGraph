using System;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using System.Net.WebSockets;
using System.Threading;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.ConditionalDraw;
using System.Collections.Generic;

namespace SimpleGraph.Views;


public partial class GraphWindow : Window
{
    public ObservableCollection<double> bid = [];
    public ObservableCollection<double> ask = [];
    public ObservableCollection<string> time = [];
    ClientWebSocket client = new();
    DispatcherTimer timer = new();
    int ticksMax;
    public GraphWindow()
    {
        InitializeComponent();
        ask.CollectionChanged += Ask_CollectionChanged;
        chart.AnimationsSpeed = TimeSpan.FromMilliseconds(0.1);
    }

    public async void LoadData(int ticks, string c1)
    {
        Uri uri = new("wss://data-stream.binance.vision:9443/ws");
        client = new();
        chart.Series = new ObservableCollection<ISeries> { };
        bid.Clear();
        ask.Clear();
        try
        {
            await client.ConnectAsync(uri, CancellationToken.None);
            string msg = "{\"method\": \"SUBSCRIBE\",\"params\": [\"bnb" + c1 + "@bookTicker\"],\"id\": 1}";
            byte[] query = Encoding.UTF8.GetBytes(msg);

            await client.SendAsync(query, WebSocketMessageType.Text, true, CancellationToken.None);
            byte[] result = new byte[1024];
            WebSocketReceiveResult rs = await client.ReceiveAsync(result, CancellationToken.None);

            timer = new()
            {
                Interval = TimeSpan.FromSeconds(ticks)
            };
            timer.Tick += async (sender, e) =>
            {
                rs = await client.ReceiveAsync(result, CancellationToken.None);
                string resText = Encoding.UTF8.GetString(result, 0, rs.Count);
                MarketData md = JsonSerializer.Deserialize<MarketData>(resText)!;

                if (ask.Count == ticksMax)
                {
                    ask.RemoveAt(0);
                }
                if (bid.Count == ticksMax)
                {
                    bid.RemoveAt(0);
                }
                if (time.Count == ticksMax)
                {
                    time.RemoveAt(0);
                }
                time.Add(DateTime.Now.TimeOfDay.ToString());
                bid.Add(double.Parse(md.b!));
                ask.Add(double.Parse(md.a!));
            };
            timer.Start();
        }
        catch (Exception ex)
        {
            txt.Text += ex.Message;
        }
    }

    public void StopConnection()
    {
        timer.Stop();
        client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
        txt.Text += "Disconnected\n";
        txt.Text += client.State.ToString() + "\n";
    }


    public void btnClick(object source, RoutedEventArgs args)
    {
        txt.Text = client.State.ToString();
        conBut.Content = "Disconnect";
        if (client.State == WebSocketState.Open)
        {
            StopConnection();
            if (client.State == WebSocketState.Closed) conBut.Content = "Connect";
        }
        else if (client.State == WebSocketState.Closed || client.State == WebSocketState.None)
        {
            txt.Text += "Connecting\n";
            ticksMax = int.Parse(tickMax.Text!);
            LoadData(int.Parse(tickBox.Text!), cur1.Text!);
            if (client.State == WebSocketState.Open) conBut.Content = "Disconnect";
        }
    }

    void Ask_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:

                {
                    chart.Series = new ObservableCollection<ISeries>
                    {
                        new LineSeries<double>
                        {
                            Values = ask,
                            //new ObservableCollection<double>(ask.Concat([(double)e.NewItems![0]!])),
                            Fill = null,
                            Stroke = new SolidColorPaint(SKColors.DarkCyan),
                            LineSmoothness = 0,
                            Name = "Ask",
                    }.OnPointMeasured(point => point.Visual.Fill = new SolidColorPaint(SKColors.DarkCyan)),

                        new LineSeries<double>
                        {
                            Values = bid,
                            Fill = null,
                            Stroke = new SolidColorPaint(SKColors.Yellow),
                            LineSmoothness = 0,
                            Name = "Bid",
                        }.OnPointMeasured(point => point.Visual.Fill = new SolidColorPaint(SKColors.Yellow))
                    };

                    /*chart.XAxes = new List<Axis>
                    {
                        new() {
                            Labels = time,
                        }
                    };*/

                }
                break;
        }
    }

    public class MarketData
    {
        public string? b { get; set; }
        public string? a { get; set; }
    }
}