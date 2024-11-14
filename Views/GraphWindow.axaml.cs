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
using Avalonia.VisualTree;
using Avalonia.Styling;
using Avalonia.Media;
using Avalonia;

namespace SimpleGraph.Views;


public partial class GraphWindow : Window
{
    public ObservableCollection<double> bid = [];
    public ObservableCollection<double> ask = [];
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
            dbg.Text = "Connecting started.";
            await client.ConnectAsync(uri, CancellationToken.None);
            if (client.State == WebSocketState.Open){
                conBut.IsEnabled = true;
                dbg.Text = "Connected";
            }

            string msg = "{\"method\": \"SUBSCRIBE\",\"params\": [\"bnb" + c1 + "@bookTicker\"],\"id\": 1}";
            byte[] query = Encoding.UTF8.GetBytes(msg);
            dbg.Text = "Message sending.";
            await client.SendAsync(query, WebSocketMessageType.Text, true, CancellationToken.None);
            dbg.Text = "Sended.";
            byte[] result = new byte[1024];
            WebSocketReceiveResult rs = await client.ReceiveAsync(result, CancellationToken.None);
            dbg.Text = "First message recieved";
            timer = new()
            {
                Interval = TimeSpan.FromSeconds(ticks)
            };
            timer.Tick += async (sender, e) =>
            {
                dbg.Text = "New tick.";
                try
                {
                    rs = await client.ReceiveAsync(result, CancellationToken.None);
                    dbg.Text = "Message recieved.";
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
                bid.Add(double.Parse(md.b!));
                dbg.Text = "New bid added";
                ask.Add(double.Parse(md.a!));
                dbg.Text = "New ask added";
                }
                catch (Exception ex)
                {
                    dbg.Text = ex.Message;
                }
            };
            timer.Start();
        }
        catch (Exception ex)
        {
            dbg.Text = ex.Message;
        }
    }

    public void StopConnection()
    {
        dbg.Text = "Connection closing";
        timer.Stop();
        client.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).Wait();
        dbg.Text = "Connection closed";
    }


    public void btnClick(object source, RoutedEventArgs args)
    {
        conBut.Content = "Disconnect";
        if (client.State == WebSocketState.Open)
        {
            StopConnection();
            if (client.State == WebSocketState.Closed) {
                conBut.Content = "Connect";
            }
        }
        else if (client.State == WebSocketState.Closed || client.State == WebSocketState.None)
        {
            conBut.IsEnabled = false;
            ticksMax = int.Parse(tickMax.Text!);
            LoadData(int.Parse(tickBox.Text!), cur1.Text!);
        }
    }

    void Ask_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
                {
                    dbg.Text = "Chart collection update";
                    chart.Series = new ObservableCollection<ISeries>
                    {
                        new LineSeries<double>
                        {
                            Values = ask,
                            //new ObservableCollection<double>(ask.Concat([(double)e.NewItems![0]!])),
                            Fill = null,
                            Stroke = new SolidColorPaint(SKColors.Fuchsia),
                            LineSmoothness = 0,
                            Name = "Ask",
                    }.OnPointMeasured(point => point.Visual!.Fill = new SolidColorPaint(SKColors.Fuchsia)),

                        new LineSeries<double>
                        {
                            Values = bid,
                            Fill = null,
                            Stroke = new SolidColorPaint(SKColors.Yellow),
                            LineSmoothness = 0,
                            Name = "Bid",
                        }.OnPointMeasured(point => point.Visual!.Fill = new SolidColorPaint(SKColors.Yellow))
                    };

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