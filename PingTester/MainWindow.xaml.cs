using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PingTester
{
	public partial class MainWindow : Window
	{
		PlatformID pid = Environment.OSVersion.Platform;

		int minRate, maxRate;
		long averageRate;
		int c1msLess, c1msOver, c20msOver, c50msOver, c100msOver, c150msOver, c200msOver, timeoutCount;
		int totalCount;

		Process process;
		Thread runningThread;

		public void Reset ()
		{
			minRate = 9999;
			maxRate = 0;
			averageRate = 0;
			timeoutCount = 0;
			c1msLess = c1msOver = c20msOver = c100msOver = c150msOver = c200msOver = 0;
			totalCount = 0;
		}

		public MainWindow ()
		{
			InitializeComponent ();

			string gateway = GetGateway ();
			comboBoxHost.Text = gateway;

			Reset ();
		}

		private string GetGateway ()
		{
			var card = NetworkInterface.GetAllNetworkInterfaces ().FirstOrDefault ();
			if ( card == null ) return null;
			var address = card.GetIPProperties ().GatewayAddresses.FirstOrDefault ();
			return address.Address.ToString ();
		}

		private void Window_Closing ( object sender, System.ComponentModel.CancelEventArgs e )
		{
			if ( runningThread != null )
				runningThread.Abort ();
		}

		private void Button_Click ( object sender, RoutedEventArgs e )
		{
			if ( runningThread == null )
			{
				string host = comboBoxHost.Text;
				runningThread = new Thread ( () =>
				{
					process = new Process ();
					process.StartInfo = new ProcessStartInfo (
						"ping", pid == PlatformID.Win32NT ? string.Format ( "{0} -t", host ) : host
					) { CreateNoWindow = true, RedirectStandardOutput = true, UseShellExecute = false };
					process.Start ();

					StreamReader reader = process.StandardOutput;
					Regex exp = new Regex (
						pid == PlatformID.Win32NT ?
						"([0-9]*).([0-9]*).([0-9]*).([0-9]*)(.*): (.*)=([0-9]*) (.*)[<|>|=]([0-9]*)ms TTL=([0-9]*)" :
						"([0-9]*) bytes from ([0-9]*).([0-9]*).([0-9]*).([0-9]*): icmp_req=([0-9]*) ttl=([0-9]*) time[<|>|=]([0-9]*).([0-9]*) ms"
					);
					reader.ReadLine ();
					if ( pid == PlatformID.Win32NT ) reader.ReadLine ();
					while ( true )
					{
						string line = reader.ReadLine ();
						Match match = exp.Match ( line, 0 );
						if ( !match.Success ) ++timeoutCount;
						else
						{
							GroupCollection groups = match.Groups;
							int time = 9999;
							if ( pid == PlatformID.Win32NT ) time = int.Parse ( groups [ 9 ].Value );
							else time = int.Parse ( groups [ 9 ].Value );

							if ( time >= 200 ) ++c200msOver;
							else if ( time >= 150 ) ++c150msOver;
							else if ( time >= 100 ) ++c100msOver;
							else if ( time >= 50 ) ++c50msOver;
							else if ( time >= 20 ) ++c20msOver;
							else if ( time > 1 ) ++c1msOver;
							else if ( time <= 1 ) ++c1msLess;

							minRate = Math.Min ( minRate, time );
							maxRate = Math.Max ( maxRate, time );
							averageRate += time;
							++totalCount;
						}

						Dispatcher.BeginInvoke ( new Action ( () =>
						{
							textBlockMinMax.Text = string.Format ( "{0}ms / {1}ms", minRate, maxRate );
							textBlockAverage.Text = string.Format ( "{0:0.00}ms", ( totalCount != 0 ) ? averageRate / ( float ) totalCount : 0 );
							textBlockTimeoutCount.Text = timeoutCount.ToString ();
							listBoxConsole.Items.Insert ( 0, line );

							textBlock1msLess.Text = c1msLess.ToString ();
							textBlock1msOver.Text = c1msOver.ToString ();
							textBlock10msOver.Text = c20msOver.ToString ();
							textBlock50msOver.Text = c50msOver.ToString ();
							textBlock100msOver.Text = c100msOver.ToString ();
							textBlock200msOver.Text = c150msOver.ToString ();
							textBlock300msOver.Text = c200msOver.ToString ();
						} ) );

						Thread.Sleep ( 1 );
					}
				} );
				listBoxConsole.Items.Clear ();
				buttonStart.Content = "테스트 종료";
				runningThread.Start ();
			}
			else
			{
				runningThread.Abort ();
				runningThread = null;
				process.Kill ();
				process = null;
				Reset ();
				buttonStart.Content = "테스트 시작";
			}
		}
	}
}
