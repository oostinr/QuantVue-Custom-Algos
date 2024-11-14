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
	public class QuantVueMoneyball : Strategy
	{
		private Moneyball Moneyball1;
		private	CustomEnumNamespaceMoneyball.TimeMode	TimeModeSelect		= CustomEnumNamespaceMoneyball.TimeMode.Restricted;
		private DateTime 								startTime 			= DateTime.Parse("9:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime		 						endTime 			= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private double currentPnL;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "QuantVueMoneyball";
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
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				TP = 30;
				SL = 20;
				DQ = 1;
				mb_Nb_bars = 15;
				mb_period = 10;
				mb_zero = true;
				mb_uThreshold = 0.35;
				mb_lThreshold = -0.35;
				mb_Sensitivity = 0.1;
			}
			else if (State == State.Configure)
			{
				DefaultQuantity = DQ;
			}
			else if (State == State.DataLoaded)
			{				
				Moneyball1 = Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false);
				AddChartIndicator(Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, true, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false));
				
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0) 
				return;

			if (CurrentBars[0] < 5)
				return;
			
			if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceMoneyball.TimeMode.Unrestricted)
			{
				 // Set 1
				if (CrossAbove(Moneyball1.VBar, mb_uThreshold, 1))
				{
					EnterLong(DefaultQuantity, "GoLong");
					SetProfitTarget("GoLong", CalculationMode.Currency, TP);
					SetStopLoss("GoLong",CalculationMode.Currency, SL, false);
				}
				
				if (CrossBelow(Moneyball1.VBar, mb_lThreshold, 1))
				{
					EnterShort(DefaultQuantity, "GoShort");
					SetProfitTarget("GoShort", CalculationMode.Currency, TP);
					SetStopLoss("GoShort",CalculationMode.Currency, SL, false);
				}
			}
			
		}
		
		
		#region Properties
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Trading Hour Restriction", GroupName = "1. Time Parameters", Order = 0)]
		public CustomEnumNamespaceMoneyball.TimeMode TIMEMODESelect
		{
			get { return TimeModeSelect; }
			set { TimeModeSelect = value; }
		}
				
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [NinjaScriptProperty]
        [Display(Name = "Opening Range-Start", GroupName = "1. Time Parameters", Order = 1)]
        public DateTime StartTime 
		{
			get { return startTime; }
			set { startTime = value; }
		}
		
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
       	[NinjaScriptProperty]
       	[Display(Name = "Opening Range-End", GroupName = "1. Time Parameters", Order = 2)]
        public DateTime EndTime
		{
			get { return endTime; }
			set { endTime = value; }
		}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="TP", Order=1, GroupName="2. Trade Parameters")]
		public int TP
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SL", Order=2, GroupName="2. Trade Parameters")]
		public int SL
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="DQ", Order=3, GroupName="2. Trade Parameters")]
		public int DQ
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Number of bars between signals", Order=0, GroupName="3. Moneyball Parameters")]
		public int mb_Nb_bars
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Order=1, GroupName="3. Moneyball Parameters")]
		public int mb_period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="All Zero", Order=2, GroupName="3. Moneyball Parameters")]
		public bool mb_zero
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(.001, 1.0)]
		[Display(Name="Upper Threshold", Order=4, GroupName="3. Moneyball Parameters")]
		public double mb_uThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(-1.0, -.001)]
		[Display(Name="Lower Threshold", Order=5, GroupName="3. Moneyball Parameters")]
		public double mb_lThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0.001, double.MaxValue)]
		[Display(Name="Sensitivity", Order=6, GroupName="3. Moneyball Parameters")]
		public double mb_Sensitivity
		{ get; set; }
		
		#endregion
		
	}
}

namespace CustomEnumNamespaceMoneyball
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
}
