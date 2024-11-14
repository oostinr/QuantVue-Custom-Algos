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
	public class QuantVueIceberg : Strategy
	{

		private Moneyball Moneyball1;
		private Qcloud Qcloud1;
		private Qwave Qwave1;
		private MACD MACD1;
		private bool isBreakevenSet = false;
		private CustomEnumNamespaceIce.TimeMode TimeModeSelect = CustomEnumNamespaceIce.TimeMode.Restricted;
		private DateTime startTime = DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime endTime = DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private CustomEnumNamespaceIce.StopMode StopModeSelect = CustomEnumNamespaceIce.StopMode.BEOnly;
		private int tickCount = 1;
		private int priorTradesCount = 0;
		private double priorTradesCumProfit = 0;
		private double currentPnL;


		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description = @"Enter the description for your new custom Strategy here.";
				Name = "QuantVueIceberg";
				Calculate = Calculate.OnEachTick;
				EntriesPerDirection = 1;
				EntryHandling = EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy = true;
				ExitOnSessionCloseSeconds = 30;
				IsFillLimitOnTouch = false;
				MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution = OrderFillResolution.Standard;
				Slippage = 0;
				StartBehavior = StartBehavior.ImmediatelySubmit;
				TimeInForce = TimeInForce.Gtc;
				TraceOrders = false;
				RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling = StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade = 30;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration = true;

				TP = 300;
				SL = 100;
				DQ = 10;
				LookbackPeriod = 10;
				BreakevenProfit = 0;
				FastPeriod = 12;
				SlowPeriod = 26;
				SignalPeriod = 9;
				mb_Nb_bars = 15;
				mb_period = 10;
				mb_zero = true;
				mb_uThreshold = 0.35;
				mb_lThreshold = -0.35;
				mb_Sensitivity = 0.1;
				maxDailyProfit = false;
				maxDailyProfitAmount = 500;
				maxDailyLoss = false;
				maxDailyLossAmount = 500;
				slFrequency = 1;
				slStepSize = 1;
			}
			else if (State == State.Configure)
			{
				DefaultQuantity = DQ;
			}
			else if (State == State.DataLoaded)
			{
				Moneyball1 = Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false);
				Qcloud1 = Qcloud(Close, Brushes.Red, Brushes.Green, 19, 29, 49, 59, 69, 99, false);
				Qwave1 = Qwave(Close, 55, 256, 1.5, 0.1, 9, false, false, Brushes.Transparent, false);
				MACD1 = MACD(Close, FastPeriod, SlowPeriod, SignalPeriod);
				AddChartIndicator(Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, true, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false));
				AddChartIndicator(Qcloud(Close, Brushes.Red, Brushes.Green, 19, 29, 49, 59, 69, 99, false));
				AddChartIndicator(Qwave(Close, 55, 256, 1.5, 0.1, 9, false, false, Brushes.Transparent, false));
				AddChartIndicator(MACD(Close, FastPeriod, SlowPeriod, SignalPeriod));

			}
		}

		protected override void OnBarUpdate()
		{

			if (BarsInProgress != 0 || CurrentBar < BarsRequiredToTrade)
				return;

			if (Bars.IsFirstBarOfSession)
			{
				currentPnL = 0;
			}

			if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceIce.TimeMode.Unrestricted && Position.MarketPosition == MarketPosition.Flat)
			{
				if ((currentPnL <= maxDailyProfitAmount || maxDailyProfit == false) || (currentPnL >= -maxDailyLossAmount || maxDailyLoss == false))
				{
					if (CrossAbove(Moneyball1.VBar, mb_uThreshold, LookbackPeriod) && CrossAbove(Qcloud1.V1, Qcloud1.V6, LookbackPeriod) && CrossAbove(Qwave1.K1, Qwave1.VHigh, LookbackPeriod) && CrossAbove(MACD1.Default, MACD1.Avg, LookbackPeriod) && Close[0] > SMA(Close, 21)[0])
					{
						EnterLong(Convert.ToInt32(DefaultQuantity), "GoLong");
						SetStopLoss("GoLong", CalculationMode.Currency, SL, false);
						SetProfitTarget("GoLong", CalculationMode.Currency, TP);
						isBreakevenSet = false;
					}

					if (CrossBelow(Moneyball1.VBar, mb_lThreshold, LookbackPeriod) && CrossBelow(Qcloud1.V1, Qcloud1.V6, LookbackPeriod) && CrossBelow(Qwave1.K1, Qwave1.VLow, LookbackPeriod) && CrossBelow(MACD1.Default, MACD1.Avg, LookbackPeriod) && Close[0] < SMA(Close, 21)[0])
					{
						EnterShort(Convert.ToInt32(DefaultQuantity), "GoShort");
						SetStopLoss("GoShort", CalculationMode.Currency, SL, false);
						SetProfitTarget("GoShort", CalculationMode.Currency, TP);
						isBreakevenSet = false;
					}
				}
			}

			// BE Only Stop Loss Mode //

			if (BreakevenProfit > 0 && StopModeSelect == CustomEnumNamespaceIce.StopMode.BEOnly || StopModeSelect == CustomEnumNamespaceIce.StopMode.StepSL)
			{
				if (Position.MarketPosition != MarketPosition.Flat && (Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0]) >= BreakevenProfit) && !isBreakevenSet)
				{
					if (Position.MarketPosition == MarketPosition.Long)
					{
						SetStopLoss("GoLong", CalculationMode.Price, Position.AveragePrice, false);
						isBreakevenSet = true;
						tickCount = 1;
					}
					else if (Position.MarketPosition == MarketPosition.Short)
					{
						SetStopLoss("GoShort", CalculationMode.Price, Position.AveragePrice, false);
						isBreakevenSet = true;
						tickCount = 1;
					}
				}
			}

			// Step Stop Loss Mode //

			if (Position.MarketPosition != MarketPosition.Flat && StopModeSelect == CustomEnumNamespaceIce.StopMode.StepSL && isBreakevenSet == true)
			{

				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (Close[0] > Position.AveragePrice + ((slStepSize + (slFrequency * tickCount)) * TickSize)) // adjust higher each time by tickCount
					{
						SetStopLoss("GoLong", CalculationMode.Price, Position.AveragePrice + (((slFrequency * tickCount) - slStepSize) * TickSize), false);
						tickCount++; // increment to next tick
					}
				}

				if (Position.MarketPosition == MarketPosition.Short)
				{
					if (Close[0] < Position.AveragePrice - ((slStepSize + (slFrequency * tickCount)) * TickSize)) // adjust higher each time by tickCount
					{
						SetStopLoss("GoShort", CalculationMode.Price, Position.AveragePrice - (((slFrequency * tickCount) - slStepSize) * TickSize), false);
						tickCount++; // increment to next tick
					}
				}

			}

			/* Tick Trailing SL Mode Old
			
			if (Position.MarketPosition != MarketPosition.Flat && StopModeSelect == CustomEnumNamespaceIce.StopMode.TrailingSL && isBreakevenSet == true)
			{
							
				if (Position.MarketPosition == MarketPosition.Long)
				{
					if (Close[0] > Position.AveragePrice + ((slStepSize + tickCount) * TickSize)) // adjust higher each time by tickCount
					{
						SetStopLoss("GoLong", CalculationMode.Price, Position.AveragePrice + (tickCount * TickSize), false);
						tickCount ++; // increment to next tick
					}
				}

				if (Position.MarketPosition == MarketPosition.Short)
				{
					if (Close[0] < Position.AveragePrice - ((slStepSize + tickCount) * TickSize)) // adjust higher each time by tickCount
					{
						SetStopLoss("GoShort", CalculationMode.Price, Position.AveragePrice - (tickCount * TickSize), false);
						tickCount ++; // increment to next tick
					}
				}
				
			}
			
			*/

		}

		protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
		{
			if (Position.MarketPosition == MarketPosition.Flat && SystemPerformance.AllTrades.Count > 0)
			{
				// when a position is closed, add the last trade's Profit to the currentPnL
				currentPnL += SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitCurrency;

				// print to output window if the daily limit is hit
				if (currentPnL <= -maxDailyLossAmount)
				{
					Print("daily limit hit, no new orders" + Time[0].ToString());
				}

				if (currentPnL >= maxDailyProfitAmount)
				{
					Print("daily Profit limit hit, no new orders" + Time[0].ToString()); ///Prints message to output
				}

				if (currentPnL >= -maxDailyLossAmount && currentPnL <= maxDailyProfitAmount)
				{
					Print(string.Format("Daily Profit = {0}", currentPnL)); ///Prints message to output
				}
			}
		}

		#region Properties

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Trading Hour Restriction", GroupName = "Time Parameters", Order = 0)]
		public CustomEnumNamespaceIce.TimeMode TIMEMODESelect
		{
			get { return TimeModeSelect; }
			set { TimeModeSelect = value; }
		}

		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[NinjaScriptProperty]
		[Display(Name = "Opening Range-Start", GroupName = "Time Parameters", Order = 1)]
		public DateTime StartTime
		{
			get { return startTime; }
			set { startTime = value; }
		}

		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[NinjaScriptProperty]
		[Display(Name = "Opening Range-End", GroupName = "Time Parameters", Order = 2)]
		public DateTime EndTime
		{
			get { return endTime; }
			set { endTime = value; }
		}

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "TP (Currency)", Order = 1, GroupName = "Trade Parameters")]
		public int TP
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "SL (Currency)", Order = 2, GroupName = "Trade Parameters")]
		public int SL
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Default Quantity (Contracts)", Order = 3, GroupName = "Trade Parameters")]
		public int DQ
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Breakeven Profit (Currency)", Order = 4, GroupName = "Trade Parameters")]
		public int BreakevenProfit
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Max Daily Profit", Order = 5, GroupName = "Trade Parameters")]
		public bool maxDailyProfit
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Max Daily Profit (Currency)", Order = 6, GroupName = "Trade Parameters")]
		public int maxDailyProfitAmount
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "Max Daily Loss", Order = 7, GroupName = "Trade Parameters")]
		public bool maxDailyLoss
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Max Daily Loss (Currency)", Order = 7, GroupName = "Trade Parameters")]
		public int maxDailyLossAmount
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Lookback Period", Order = 8, GroupName = "Trade Parameters")]
		public int LookbackPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Fast Period", Order = 9, GroupName = "Trade Parameters")]
		public int FastPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Slow Period", Order = 10, GroupName = "Trade Parameters")]
		public int SlowPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Signal Period", Order = 11, GroupName = "Trade Parameters")]
		public int SignalPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Stoploss Mode Select", GroupName = "SL Parameters", Order = 0)]
		public CustomEnumNamespaceIce.StopMode STOPMODESelect
		{
			get { return StopModeSelect; }
			set { StopModeSelect = value; }
		}

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Stoploss Step Distance (Ticks)", GroupName = "SL Parameters", Order = 1)]
		public int slStepSize
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Stoploss Step Frequency (Ticks)", GroupName = "SL Parameters", Order = 2)]
		public int slFrequency
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Number of bars between signals", Order = 0, GroupName = "Moneyball Parameters")]
		public int mb_Nb_bars
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name = "Period", Order = 1, GroupName = "Moneyball Parameters")]
		public int mb_period
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name = "All Zero", Order = 2, GroupName = "Moneyball Parameters")]
		public bool mb_zero
		{ get; set; }

		[NinjaScriptProperty]
		[Range(.001, 1.0)]
		[Display(Name = "Upper Threshold", Order = 4, GroupName = "Moneyball Parameters")]
		public double mb_uThreshold
		{ get; set; }

		[NinjaScriptProperty]
		[Range(-1.0, -.001)]
		[Display(Name = "Lower Threshold", Order = 5, GroupName = "Moneyball Parameters")]
		public double mb_lThreshold
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.001, double.MaxValue)]
		[Display(Name = "Sensitivity", Order = 6, GroupName = "Moneyball Parameters")]
		public double mb_Sensitivity
		{ get; set; }

		#endregion
	}
}

namespace CustomEnumNamespaceIce
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}

	public enum StopMode
	{
		BEOnly,
		StepSL,
		//IceburgATM
	}
}
