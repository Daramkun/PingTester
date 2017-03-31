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
			var card = NetworkInterface.GetAllNetworkInterfaces ().FirstOrDefault ();
			if ( card == null ) return "";
			var address = card.GetIPProperties ().GatewayAddresses.FirstOrDefault ();
			if ( address == null ) return "";
			return address.Address.ToString ();
		}

		public static string ReviewCalculation (
			float c1msLess, float c1msOver, float c20msOver, float c50msOver, float c100msOver, float c150msOver, float c200msOver, float timeout,
			TimeSpan elapsedTime
			)
		{
			if ( elapsedTime < new TimeSpan ( 0, 0, 30 ) )
				return "평가 하기에 충분한 데이터가 쌓이지 않았습니다.";

			float totalCount = c1msLess + c1msOver + c20msOver + c50msOver + c100msOver + c150msOver + c200msOver + timeout;
			c1msLess /= totalCount;
			c1msOver /= totalCount;
			c20msOver /= totalCount;
			c50msOver /= totalCount;
			c100msOver /= totalCount;
			c150msOver /= totalCount;
			c200msOver /= totalCount;
			timeout /= totalCount;

			if ( timeout > 0.3f )
				return "Wi-Fi 통신 상태가 아닌 경우 회선 점검이 필요합니다.";
			else if ( ( c100msOver + c150msOver + c200msOver ) > 0.5f )
				return "통신 상태가 나쁩니다.";
			else if ( ( c20msOver + c50msOver ) > 0.5f )
				return "통신 상태 보통입니다.";
			else return "통신 상태가 최상입니다.";
		}
	}
}
