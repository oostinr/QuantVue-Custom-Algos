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
	
	public class ChopChop : Strategy
	{
		private Bollinger bb;
        private ChoppinessIndex choppiness;
		
		private bool isBreakevenSet = false;
		private CustomEnumNamespaceChopChop.TimeMode TimeModeSelect = CustomEnumNamespaceChopChop.TimeMode.Restricted;
		private DateTime startTime = DateTime.Parse("09:35:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime endTime = DateTime.Parse("14:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private CustomEnumNamespaceChopChop.StopMode StopModeSelect = CustomEnumNamespaceChopChop.StopMode.BEOnly;
		private int tickCount = 1;
		private int priorTradesCount = 0;
		private double priorTradesCumProfit = 0;
		private double currentPnL;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "ChopChop";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
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
				
				TP = 350;
				SL = 100;
				DQ = 6;
				BreakevenProfit = 250;
				maxDailyProfit = false;
				maxDailyProfitAmount = 500;
				maxDailyLoss = false;
				maxDailyLossAmount = 150;
				slFrequency = 5;
				slStepSize = 10;
				bollingerStd = 1.9;
				bollingerPeriod = 20;
				choppinessLevel = 52;
				choppinessPeriod = 2;
			}
			 else if (State == State.Configure)
            {
                // Initialize Bollinger Bands with default settings
                bb = Bollinger(bollingerStd, bollingerPeriod);
                AddChartIndicator(bb);

                // Initialize Choppiness Index with a period of 14
                choppiness = ChoppinessIndex(choppinessPeriod);
                AddChartIndicator(choppiness);
				
				DefaultQuantity = DQ;
            }
		}

		protected override void OnBarUpdate()
        {
            if (CurrentBar < 20)
                return;

            double upperBand = bb.Upper[0];
            double middleBand = bb.Middle[0];
            double lowerBand = bb.Lower[0];

            // Ensure Choppiness Index is greater than 38.2
            if (choppiness[0] <= choppinessLevel)
                return;
			
			if (Bars.IsFirstBarOfSession)
			{
				currentPnL = 0;
			}

			if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceChopChop.TimeMode.Unrestricted && Position.MarketPosition == MarketPosition.Flat)
			{
				if ((currentPnL <= maxDailyProfitAmount || maxDailyProfit == false) || (currentPnL >= -maxDailyLossAmount || maxDailyLoss == false))
				{
		            // Long Entry: Price touches the lower Bollinger Band and shows bullish reversal
		            if (Close[0] > lowerBand && Close[1] < lowerBand)
		            {
		                EnterLong(DefaultQuantity, "GoLong");
						SetStopLoss("GoLong", CalculationMode.Currency, SL, false);
						SetProfitTarget("GoLong", CalculationMode.Currency, TP);
						isBreakevenSet = false;
		            }
		
		            // Short Entry: Price touches the upper Bollinger Band and shows bearish reversal
		            if (Close[0] < upperBand && Close[1] > upperBand)
		            {
		                EnterShort(DefaultQuantity, "GoShort");
						SetStopLoss("GoShort", CalculationMode.Currency, SL, false);
						SetProfitTarget("GoShort", CalculationMode.Currency, TP);
						isBreakevenSet = false;
		            }
				}
			}

            // Exit Long: When price reaches the middle or upper Bollinger Band
            if (Position.MarketPosition == MarketPosition.Long)
            {
                if (Close[0] >= upperBand)
                {
                    ExitLong("GoLong");
                }
            }

            // Exit Short: When price reaches the middle or lower Bollinger Band
            if (Position.MarketPosition == MarketPosition.Short)
            {
                if (Close[0] <= middleBand)
                {
                    ExitShort("GoShort");
                }
            }
			
			
			// BE Only Stop Loss Mode //

			if (BreakevenProfit > 0 && StopModeSelect == CustomEnumNamespaceChopChop.StopMode.BEOnly || StopModeSelect == CustomEnumNamespaceChopChop.StopMode.StepSL)
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

			if (Position.MarketPosition != MarketPosition.Flat && StopModeSelect == CustomEnumNamespaceChopChop.StopMode.StepSL && isBreakevenSet == true)
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
		public CustomEnumNamespaceChopChop.TimeMode TIMEMODESelect
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
		[Range(0, int.MaxValue)]
		[Display(Name = "Bollinger Std. Deviation", Order = 8, GroupName = "Trade Parameters")]
		public double bollingerStd
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Bollinger Period", Order = 9, GroupName = "Trade Parameters")]
		public int bollingerPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name = "Choppiness Level", Order = 10, GroupName = "Trade Parameters")]
		public double choppinessLevel
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name = "Choppiness Period", Order = 11, GroupName = "Trade Parameters")]
		public int choppinessPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Stoploss Mode Select", GroupName = "SL Parameters", Order = 0)]
		public CustomEnumNamespaceChopChop.StopMode STOPMODESelect
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
		#endregion
	}
}


namespace CustomEnumNamespaceChopChop
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
