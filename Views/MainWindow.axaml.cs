using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SimpleGraph.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void LoadData(object source, RoutedEventArgs args)
    {
        Debug.WriteLine("Loading data");
        Console.WriteLine("Loading data");
        loadbtn.Text = loadbtn.Text + "weewoo";
    }
}