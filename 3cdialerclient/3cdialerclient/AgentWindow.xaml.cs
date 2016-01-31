﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace _cdialerclient
{
	/// <summary>
	/// Interaction logic for AgentWindow.xaml
	/// </summary>
	public partial class AgentWindow : Window
	{
        public ServerHandler serverHandler;
        bool listRequested = false;
        bool dial = false;
        //Timer timer;
		public AgentWindow(ServerHandler serverHandler)
		{
			this.InitializeComponent();
			AddKeyShortcuts();
			// Insert code required on object creation below this point.
            this.serverHandler = serverHandler;
            if (listRequested = serverHandler.RequestCallList())
            {
                SetDialCard();
            }
		}
        //populate dialcard with details of next call;
        private void SetDialCard()
        {
            if (!serverHandler.endReached)
            {
                try
                {
                    List<DialCardItem> listdc = DialCard.Create(serverHandler.CurrentCall);
                    lv_dialcard.ItemsSource = listdc;
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
            else
            {
                MessageBox.Show("Current List Calls Completed","Well Done!");
            }
        }

		protected void AddKeyShortcuts()
		{
			lv_shortList.Items.Add(new {KShort = "Space bar", Function = "End call"});
			lv_shortList.Items.Add(new {KShort = "CTRL + Y", Function = "Redial Last Call"});
			lv_shortList.Items.Add(new {KShort = "F6", Function = "Skip to Next Call"});
			lv_shortList.Items.Add(new {KShort = "Space bar", Function = "End call"});
			lv_shortList.Items.Add(new {KShort = "Space bar", Function = "End call"});
		}

        private void menu_exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void StartCalls(object sender, RoutedEventArgs e)
        {
            dial = true;
            btn_Stopcalls.IsEnabled = true;
            btn_Startcalls.IsEnabled = false;
        }

        private void StopCalls(object sender, RoutedEventArgs e)
        {
            dial = false;
            btn_Stopcalls.IsEnabled = false;
            btn_Startcalls.IsEnabled = true;
        }
	}
}