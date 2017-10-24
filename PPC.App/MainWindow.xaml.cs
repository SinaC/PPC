﻿using System;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using EasyIoc;
using PPC.Common;
using PPC.Data.Articles;
using PPC.Data.Players;
using PPC.Log;
using PPC.Services.Popup;

namespace PPC.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Singletons
            IocContainer.Default.RegisterInstance<ILog>(new NLogger());
            IocContainer.Default.RegisterInstance<IArticleDb>(new ArticlesDb());
            IocContainer.Default.RegisterInstance<IPlayersDb>(new PlayersDb());
            IocContainer.Default.RegisterInstance<IPopupService>(new PopupService(this));

            // Initialize log
            IocContainer.Default.Resolve<ILog>().Initialize(ConfigurationManager.AppSettings["logpath"], "${shortdate}.log");
            IocContainer.Default.Resolve<ILog>().Info("Application started");

            //Loaded += OnLoaded;

            double? fontSize = PPCConfigurationManager.FontSize;
            if (fontSize.HasValue)
                TextElement.SetFontSize(this, fontSize.Value);

            // Create MainViewModel
            DataContext = new MainWindowViewModel();

            //FocusManager.AddGotFocusHandler(this, GotFocusHandler);
        }

        //private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        //{
        //    Loaded -= OnLoaded;

        //    // Create MainViewModel
        //    DataContext = new MainWindowViewModel();
        //}

        private void GotFocusHandler(object sender, RoutedEventArgs routedEventArgs)
        {
            StackTrace t = new StackTrace();
            Debug.WriteLine("GotFocus: source:" + routedEventArgs.Source + " original:" + routedEventArgs.OriginalSource + Environment.NewLine + "stack:" + t);
        }

        private void MainWindow_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MainWindow_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                double actualFontSize = TextElement.GetFontSize(this);
                double newFontSize = actualFontSize * Math.Pow(1.3, e.Delta / 120F);
                TextElement.SetFontSize(this, newFontSize);

                // MainWindow is master window, if we change zoom on this window,  every other windows will be zoomed
                PPCConfigurationManager.FontSize = newFontSize; // set for other window

                e.Handled = true;
            }
        }
    }
}
