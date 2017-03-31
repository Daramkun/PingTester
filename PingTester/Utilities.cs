using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace PingTester
{
	public static class Utilities
	{
		public static string GetGateway ()
		{
			foreach ( var card in NetworkInterface.GetAllNetworkInterfaces () )
			{
				if ( card == null )
					continue;

				if ( card.OperationalStatus == OperationalStatus.Up )
				{
					foreach ( var address in card.GetIPProperties ().GatewayAddresses )
					{
						if ( address == null )
							continue;
						return address.Address.ToString ();
					}
				}
			}

			return "";
		}

		public static string ReviewCalculation (
			float c1msLess, float c1msOver, float c20msOver, float c50msOver, float c100msOver, float c150msOver, float c200msOver, float timeout,
			TimeSpan elapsedTime
			)
		{
			if ( elapsedTime < new TimeSpan ( 0, 0, 15 ) )
				return "평가 하기에 충분한 데이터가 쌓이지 않았습니다.";

			c1msLess = c1msLess * 0.080f;
			c1msOver = c1msOver * 0.090f;
			c20msOver = c20msOver * 0.110f;
			c50msOver = c50msOver * 0.130f;
			c100msOver = c100msOver * 0.130f;
			c150msOver = c150msOver * 0.140f;
			c200msOver = c200msOver * 0.150f;
			timeout = timeout * 0.170f;

			float totalCount = c1msLess + c1msOver + c20msOver + c50msOver + c100msOver + c150msOver + c200msOver + timeout;
			c1msLess = c1msLess / totalCount;
			c1msOver = c1msOver / totalCount;
			c20msOver = c20msOver / totalCount;
			c50msOver = c50msOver / totalCount;
			c100msOver = c100msOver / totalCount;
			c150msOver = c150msOver / totalCount;
			c200msOver = c200msOver / totalCount;
			timeout = timeout / totalCount;

			System.Diagnostics.Debug.WriteLine ( $"{c1msLess}, {c1msOver}, {c20msOver}, {c50msOver}, {c100msOver}, {c150msOver}, {c200msOver}, {timeout}" );

			List<KeyValuePair<int, float>> tier = new List<KeyValuePair<int, float>>
			{
				new KeyValuePair<int, float> ( 0, c1msLess + c1msOver ),
				new KeyValuePair<int, float> ( 1, c20msOver + c50msOver ),
				new KeyValuePair<int, float> ( 2, c100msOver + c150msOver ),
				new KeyValuePair<int, float> ( 3, c200msOver ),
				new KeyValuePair<int, float> ( 4, timeout ),
			};
			tier.Sort ( ( a, b ) => -a.Value.CompareTo ( b.Value ) );

			switch ( tier[0].Key )
			{
				case 0: return "실시간 통신 게임의 호스트가 되어도 무리가 없는 환경으로 보입니다.";
				case 1: return "실시간 통신을 사용하는 게임을 이용하는데 무리 없는 환경으로 보입니다.";
				case 2: return "실시간 통신을 사용하는 게임을 이용하는데 무리가 있습니다.";
				case 3: return "인터넷을 이용한 멀티미디어 이용에 무리가 있을 수 있습니다.";
				case 4: return "회선 점검 또는 무선 인터넷 환경 점검이 필요합니다.";
				default: return "";
			}
		}
	}
}
