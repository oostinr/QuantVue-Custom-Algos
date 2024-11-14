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
	public class QuantVueJayceAndNahuel : Strategy
	{
		private iGRID_EVO iGRID_EVO1;
		private Qwave Qwave1;
		private Moneyball Moneyball1;
		private int		grid1Flip;
		private	CustomEnumNamespaceJnN.TimeMode	TimeModeSelect		= CustomEnumNamespaceJnN.TimeMode.Restricted;
		private DateTime 								startTime 			= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime		 						endTime 			= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"A QuantVue TradingView strategy ported over to NT for your enjoyment";
				Name										= "QuantVueJayceAndNahuel";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 3;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				TP = 400;
				SL = 100;
				DQ = 2;
				mb_Nb_bars = 15;
				mb_period = 10;
				mb_zero = true;
				mb_uThreshold = 0.35;
				mb_lThreshold = -0.35;
				mb_Sensitivity = 0.1;
				LookbackPeriod = 5;
			}
			else if (State == State.Configure)
			{
				DefaultQuantity = DQ;
			}
			else if (State == State.DataLoaded)
			{
				iGRID_EVO1 = iGRID_EVO(Close, 19, 19, 2.5, true, 2, 50, 7); // Qgrid
				Qwave1 = Qwave(Close, 55, 256, 1.5, 0.1, 9, false, false, Brushes.Transparent, false);
				AddChartIndicator( iGRID_EVO(Close, 19, 19, 2.5, true, 2, 50, 7));
				AddChartIndicator(Qwave(Close, 55, 256, 1.5, 0.1, 9, false, false, Brushes.Transparent, false));
				Moneyball1 = Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false);
				AddChartIndicator(Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false));
			}
			
		}

		protected override void OnBarUpdate()
		{
			
			if (iGRID_EVO1.FlipSignal[0] == 1)
			{
				grid1Flip = 1;
			}
			else if (iGRID_EVO1.FlipSignal[0] == -1)
			{
				grid1Flip = 2;
			}

			
			if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceJnN.TimeMode.Unrestricted && Position.MarketPosition == MarketPosition.Flat)
			{
				//if (CrossAbove(Close, Qwave1.VHigh, 1) && iGRID_EVO1.StepMA[0] < Close[0])
				if (CrossAbove(Close, Qwave1.VHigh, 1) && grid1Flip == 1 && (CrossAbove(Moneyball1.VBar, mb_uThreshold, LookbackPeriod) || includeMoneyball == false))
				{
					EnterLong(Convert.ToInt32(DefaultQuantity), "GoLong");
					SetStopLoss("GoLong", CalculationMode.Currency, SL, false);
					SetProfitTarget("GoLong", CalculationMode.Currency, TP);
				}
				
				//if (CrossBelow(Close, Qwave1.VLow, 1) && iGRID_EVO1.StepMA[0] > Close[0])
				if (CrossBelow(Close, Qwave1.VLow, 1) && grid1Flip == 2 && (CrossBelow(Moneyball1.VBar, mb_uThreshold, LookbackPeriod) || includeMoneyball == false))
				{
					EnterShort(Convert.ToInt32(DefaultQuantity), "GoShort");
					SetStopLoss("GoShort", CalculationMode.Currency, SL, false);
					SetProfitTarget("GoShort", CalculationMode.Currency, TP);
				}	
			}
			
			if (Position.MarketPosition == MarketPosition.Long)
			{
				//if (CrossBelow(Close, Qwave1.VHigh, 1) || Close[0] < Open[0])
				if (Close[0] < Qwave1.K1[0])	
				{
					ExitLong();
				}
			}
			
			if (Position.MarketPosition == MarketPosition.Short)
			{
				//if (CrossAbove(Close, Qwave1.VLow, 1) || Close[0] > Open[0])
				if (Close[0] > Qwave1.K1[0])	
				{
					ExitShort();
				}
			}
		}
		
		#region Properties
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Trading Hour Restriction", GroupName = "Parameters", Order = 0)]
		public CustomEnumNamespaceJnN.TimeMode TIMEMODESelect
		{
			get { return TimeModeSelect; }
			set { TimeModeSelect = value; }
		}
				
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [NinjaScriptProperty]
        [Display(Name = "Opening Range-Start", GroupName = "Parameters", Order = 1)]
        public DateTime StartTime 
		{
			get { return startTime; }
			set { startTime = value; }
		}
		
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
       	[NinjaScriptProperty]
       	[Display(Name = "Opening Range-End", GroupName = "Parameters", Order = 2)]
        public DateTime EndTime
		{
			get { return endTime; }
			set { endTime = value; }
		}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="TP", Order=3, GroupName="Parameters")]
		public int TP
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SL", Order=4, GroupName="Parameters")]
		public int SL
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="DQ", Order=5, GroupName="Parameters")]
		public int DQ
		{ get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Lookback Period", Order=6, GroupName="Parameters")]
        public int LookbackPeriod 
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Number of bars between signals", Order=0, GroupName="Moneyball Parameters")]
		public int mb_Nb_bars
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Order=1, GroupName="Moneyball Parameters")]
		public int mb_period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="All Zero", Order=2, GroupName="Moneyball Parameters")]
		public bool mb_zero
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(.001, 1.0)]
		[Display(Name="Upper Threshold", Order=4, GroupName="Moneyball Parameters")]
		public double mb_uThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(-1.0, -.001)]
		[Display(Name="Lower Threshold", Order=5, GroupName="Moneyball Parameters")]
		public double mb_lThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0.001, double.MaxValue)]
		[Display(Name="Sensitivity", Order=6, GroupName="Moneyball Parameters")]
		public double mb_Sensitivity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Include Moneyball?", Order=7, GroupName="Moneyball Parameters")]
		public bool includeMoneyball
		{ get; set; }
		#endregion
	}
}

namespace CustomEnumNamespaceJnN
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
}
