#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class QuantVueQwaveTriple : Strategy
	{
		private Qwave Qwave1;
		private double entryPrice = 0.0;
        private bool isBreakevenSet = false; // Flag to check if breakeven is already set
		private Order stopLossOrderLong;
		private Order stopLossOrderShort;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "QuantVueQwaveTriple";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.ImmediatelySubmitSynchronizeAccount;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				RealtimeErrorHandling						= RealtimeErrorHandling.IgnoreAllErrors;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				TP = 30;
				SL = 20;
				DQ = 1;
				SMAPeriod = 21;
			}
			else if (State == State.Configure)
			{
				DefaultQuantity = DQ;
			}
			else if (State == State.DataLoaded)
			{				
				Qwave1 = Qwave(Close, 55, 256, 1.5, 0.1, 9, false, false, Brushes.Transparent, false);
				SetProfitTarget(CalculationMode.Currency, TP);
			}
		}
		
		protected override void OnOrderUpdate(Order order, double limitPrice, double stopPrice, int quantity, int filled, double averageFillPrice,
                                    OrderState orderState, DateTime time, ErrorCode error, string nativeError)
		{
		  if (error != ErrorCode.NoError) 
		  {
			ExitLong();
			ExitShort();
		  }
		}
		
		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0) 
				return;

			if (CurrentBars[0] < 1)
				return;
			
			 // Set 1
			if ((CrossAbove(Qwave1.K1, Qwave1.VLow, 5) && CrossAbove(Qwave1.K1, Qwave1.V1, 3) && CrossAbove(Qwave1.K1, Qwave1.VHigh, 1)) && Close[0] > SMA(Close, SMAPeriod)[0])
			{
				EnterLong(Convert.ToInt32(DefaultQuantity), "GoLong");
				SetStopLoss("GoLong", CalculationMode.Currency, SL, false);
			}
			
			 // Set 2
			if ((CrossBelow(Qwave1.K1, Qwave1.VHigh, 5) && CrossBelow(Qwave1.K1, Qwave1.V1, 3)&& CrossBelow(Qwave1.K1, Qwave1.VLow, 1)) && Close[0] < SMA(Close, SMAPeriod)[0])
			{
				EnterShort(Convert.ToInt32(DefaultQuantity), "GoShort");
				SetStopLoss("GoShort", CalculationMode.Currency, SL, false);
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="TP", Order=1, GroupName="Parameters")]
		public int TP
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SL", Order=2, GroupName="Parameters")]
		public int SL
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="DQ", Order=3, GroupName="Parameters")]
		public int DQ
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SMA Period", Order=4, GroupName="Parameters")]
		public int SMAPeriod
		{ get; set; }
		#endregion

	}
}


