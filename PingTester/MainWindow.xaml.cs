using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
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
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		int minrate, maxrate, totalcount;
		long avgrate;
		float average;
		int c1msLess, c1msOver, c20msOver, c50msOver, c100msOver, c150msOver, c200msOver, timeoutCount;
		string review;

		public int MinimumRate { get { return minrate; } set { minrate = value; PC (); } }
		public int MaximumRate { get { return maxrate; } set { maxrate = value; PC (); } }
		public long AverageRate { get { return avgrate; } set { avgrate = value; PC (); } }
		public float Average { get { return average; } set { average = value; PC (); } }
		public int TotalCount { get { return totalcount; } set { totalcount = value; PC (); } }
		public int Time1msLess { get { return c1msLess; } set { c1msLess = value;PC (); } }
		public int Time1msOver { get { return c1msOver; } set { c1msOver = value; PC (); } }
		public int Time20msOver { get { return c20msOver; } set { c20msOver = value; PC (); } }
		public int Time50msOver { get { return c50msOver; } set { c50msOver = value; PC (); } }
		public int Time100msOver { get { return c100msOver; } set { c100msOver = value; PC (); } }
		public int Time150msOver { get { return c150msOver; } set { c150msOver = value; PC (); } }
		public int Time200msOver { get { return c200msOver; } set { c200msOver = value; PC (); } }
		public int TimeoutCount { get { return timeoutCount; } set { timeoutCount = value; PC (); } }
		public string Review { get { return review; } set { review = value; PC (); } }

		Process process;
		Thread runningThread;
		Stopwatch stopwatch = new Stopwatch ();

		public TimeSpan ElapsedTime { get { return stopwatch.Elapsed; } }

		public event PropertyChangedEventHandler PropertyChanged;
		private void PC ( [CallerMemberName] string name = "" ) { PropertyChanged?.Invoke ( this, new PropertyChangedEventArgs ( name ) ); }

		public void Reset ()
		{
			MinimumRate = 9999;
			MaximumRate = 0;
			TotalCount = 0;
			AverageRate = 0;
			Average = 0;
			Time1msLess = Time1msOver = Time20msOver = Time50msOver = Time100msOver = Time150msOver = Time200msOver = TimeoutCount = 0;
			stopwatch.Reset ();
		}

		public MainWindow ()
		{
			InitializeComponent ();

			DataContext = this;
			
			comboBoxHost.Text = Utilities.GetGateway ();

			Reset ();
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
					var pid = Environment.OSVersion.Platform;
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
						if ( !match.Success ) ++TimeoutCount;
						else
						{
							GroupCollection groups = match.Groups;
							int time = 9999;
							if ( pid == PlatformID.Win32NT ) time = int.Parse ( groups [ 9 ].Value );
							else time = int.Parse ( groups [ 9 ].Value );

							if ( time >= 200 ) ++Time200msOver;
							else if ( time >= 150 ) ++Time150msOver;
							else if ( time >= 100 ) ++Time100msOver;
							else if ( time >= 50 ) ++Time50msOver;
							else if ( time >= 20 ) ++Time20msOver;
							else if ( time > 1 ) ++Time1msOver;
							else if ( time <= 1 ) ++Time1msLess;

							MinimumRate = Math.Min ( MinimumRate, time );
							MaximumRate = Math.Max ( MaximumRate, time );
							AverageRate += time;
							++TotalCount;

							Average = ( TotalCount != 0 ) ? AverageRate / ( float ) TotalCount : 0;
						}

						Dispatcher.BeginInvoke ( new Action ( () => listBoxConsole.Items.Insert ( 0, line ) ) );

						Review = Utilities.ReviewCalculation ( c1msLess, c1msOver, c20msOver, c50msOver, c100msOver, c150msOver, c200msOver, timeoutCount, stopwatch.Elapsed );
						PC ( "ElapsedTime" );

						Thread.Sleep ( 1 );
					}
				} );
				Reset ();
				listBoxConsole.Items.Clear ();
				buttonStart.Content = "테스트 종료";
				runningThread.Start ();
				stopwatch.Start ();
			}
			else
			{
				stopwatch.Stop ();
				runningThread.Abort ();
				runningThread = null;
				process.Kill ();
				process = null;
				buttonStart.Content = "테스트 시작";
			}
		}
	}
}
