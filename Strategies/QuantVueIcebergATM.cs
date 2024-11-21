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
	public class QuantVueIcebergATM : Strategy
	{
		
		#region Variables		
        private string									longAtmId					= string.Empty; // Atm Id for long.
		private string									longOrderId					= string.Empty; // Order Id for long.
		private string									shortAtmId					= string.Empty; // Atm Id for short.
		private string									shortOrderId				= string.Empty; // Order Id for short.
		private bool 									isLongAtmStrategyCreated 	= false;
		private bool									isShortAtmStrategyCreated	= false;
		private int 									priorTradesCount 			= 0;
		private double 									priorTradesCumProfit		= 0;
		private double 									currentPnL;
		private bool									okToTrade;
		private double									maxProfitLevel;
		private bool 									trailingLossHit;
		private	CustomEnumNamespaceIceATM.TimeMode		TimeModeSelect				= CustomEnumNamespaceIceATM.TimeMode.Restricted;
		private DateTime 								startTime 					= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime		 						endTime 					= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private QMomentum QMomentum1;
		private Moneyball Moneyball1;
		private Qcloud Qcloud1;
		private Qwave Qwave1;
		private MACD MACD1;
		private int										macdBar;
		private int										entryDelayCounter			= 0;
        #endregion
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"This automated strategy takes entries like the famous Iceberg.";
				Name										= "QuantVueIcebergATM";
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
				
				// Default values
				LookbackPeriod = 5;
				DQ = 10;
				FastPeriod = 12;  
                SlowPeriod  = 26;  
                SignalPeriod  = 9;
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
				userSMA = 21;
				atmName = "IcebergATM";
				MACDLookback = 10;
				MACDLimit = 2;
				MACDRestrict = false;
				entryDelayInput = 10;
				MACDUse = true;
				
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
				
				QMomentum1 = QMomentum(22, QMomentumMAType.EMA, true, true, true, true, true, false, Brushes.Teal, Brushes.Red, Brushes.Red, Brushes.RoyalBlue, Brushes.Green, Brushes.LimeGreen, Brushes.Yellow, Brushes.Red);
				//AddChartIndicator(QMomentum(22, QMomentumMAType.EMA, true, true, true, true, true, false, Brushes.Teal, Brushes.Red, Brushes.Red, Brushes.RoyalBlue, Brushes.Green, Brushes.LimeGreen, Brushes.Yellow, Brushes.Red));
				
				
			}
		}

		#region Program Variables //variables that are handled programatically
			
		double currentDayProfit;		
		double previousRunningProfit;	
		bool eodUpkeep = true; //flag used to ensure that end of day upkeep only happens once per day
			
		#endregion
		
		protected override void OnBarUpdate()
		{
			// HELP DOCUMENTATION REFERENCE: Please see the Help Guide section "Using ATM Strategies"

			// Make sure this strategy does not execute against historical data
			if(State == State.Historical)
				return;			
			
			if (Bars.IsFirstBarOfSession)
			{
				currentPnL = 0;
				maxProfitLevel = 0;
				trailingLossHit = false;
				okToTrade = true;
				previousRunningProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
			}
			
			if (BarsInProgress != 0) 
				return;

			if (CurrentBars[0] < 1)
				return;
			
			// Test code
			
			if (GetAtmStrategyMarketPosition(longAtmId) != Cbi.MarketPosition.Flat || GetAtmStrategyMarketPosition(shortAtmId) != Cbi.MarketPosition.Flat)
			{
				entryDelayCounter = entryDelayInput;
			}
			else if(GetAtmStrategyMarketPosition(longAtmId) != Cbi.MarketPosition.Flat && GetAtmStrategyMarketPosition(shortAtmId) == Cbi.MarketPosition.Flat && entryDelayCounter > 0)
			{
				entryDelayCounter --;
			}
			
			/*
			if (Position.MarketPosition != MarketPosition.Flat)
			{
				entryDelayCounter = entryDelayInput;
			}
			else if(Position.MarketPosition == MarketPosition.Flat && entryDelayCounter > 0)
			{
				entryDelayCounter --;
			}
			*/
			
			if (CrossAbove(MACD1.Default, MACD1.Avg, 1) || CrossBelow(MACD1.Default, MACD1.Avg, 1))
			{
				macdBar = CurrentBar;
			}
			
			
			// Check any pending long or short orders by their Order Id and if the ATM has terminated.
			// Check for a pending long order.
			if (longOrderId.Length > 0)
			{
				// If the status call can't find the order specified, the return array length will be zero otherwise it will hold elements.
				string[] status = GetAtmStrategyEntryOrderStatus(longOrderId);
				if (status.GetLength(0) > 0)
				{
					// If the order state is terminal, reset the order id value.
					if (status[2] == "Filled" || status[2] == "Cancelled" || status[2] == "Rejected")
						longOrderId = string.Empty;
				}
			} // If the strategy has terminated reset the strategy id.
			else if (longAtmId.Length > 0 && GetAtmStrategyMarketPosition(longAtmId) == Cbi.MarketPosition.Flat)
			{
				longAtmId = string.Empty;
				isLongAtmStrategyCreated = false;
			}
			
			// Check for a pending short order.
			if (shortOrderId.Length > 0)
			{
				// If the status call can't find the order specified, the return array length will be zero otherwise it will hold elements.
				string[] status = GetAtmStrategyEntryOrderStatus(shortOrderId);
				if (status.GetLength(0) > 0)
				{
					// If the order state is terminal, reset the order id value.
					if (status[2] == "Filled" || status[2] == "Cancelled" || status[2] == "Rejected")
						shortOrderId = string.Empty;
				}
			} // If the strategy has terminated reset the strategy id.
			else if (shortAtmId.Length > 0 && GetAtmStrategyMarketPosition(shortAtmId) == Cbi.MarketPosition.Flat)
			{
				shortAtmId = string.Empty;
				isShortAtmStrategyCreated = false;
			}
			// End check.
			
			/* -------------------------------------
			
				Daily PnL Block - Before Entries
			
			--------------------------------------*/
			
			if (((Position.MarketPosition == MarketPosition.Long) || (Position.MarketPosition == MarketPosition.Short)) 
				&& trailingLossHit == true
				|| (((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) <= -maxDailyLossAmount) && maxDailyLoss == true)
				|| (((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) >= maxDailyProfitAmount) && maxDailyProfit == true)) ///If unrealized goes under maxDailyLossAmount 'OR' Above maxDailyProfitAmount    
			{
				if(shortAtmId.Length != 0 && isShortAtmStrategyCreated)
						{
							AtmStrategyClose(shortAtmId);
							isShortAtmStrategyCreated = false;
						}
				if(longAtmId.Length != 0  && isLongAtmStrategyCreated)
						{
							AtmStrategyClose(longAtmId);
							isLongAtmStrategyCreated = false;
						}
				okToTrade = false;
			}
			
			// Entries.
			// **** YOU MUST HAVE AN ATM STRATEGY TEMPLATE NAMED 'IcebergATM' CREATED IN NINJATRADER (SUPERDOM FOR EXAMPLE) FOR THIS TO WORK ****
			// Enter long if Close is greater than Open.
			if (((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceIceATM.TimeMode.Unrestricted) && Position.MarketPosition == MarketPosition.Flat && entryDelayCounter == 0)
			{
				if (((currentPnL <= maxDailyProfitAmount || maxDailyProfit == false) || (currentPnL >= -maxDailyLossAmount || maxDailyLoss == false)) && okToTrade == true && trailingLossHit == false)
				{
			
					//if(CrossAbove(Moneyball1.VBar, mb_uThreshold, LookbackPeriod) && CrossAbove(Qcloud1.V1, Qcloud1.V6, LookbackPeriod) && CrossAbove(Qwave1.K1, Qwave1.VHigh, LookbackPeriod) && CrossAbove(MACD1.Default, MACD1.Avg, MACDLookback) && MACD1.Default[0] > mb_uThreshold && Close[0] > SMA(Close, userSMA)[0])
					if(CrossAbove(Moneyball1.VBar, mb_uThreshold, LookbackPeriod) && CrossAbove(Qcloud1.V1, Qcloud1.V6, LookbackPeriod) && CrossAbove(Qwave1.K1, Qwave1.VHigh, LookbackPeriod) && ((CrossAbove(MACD1.Default, MACD1.Avg, MACDLookback) && (MACD1.Default[CurrentBar - macdBar] < -MACDLimit || MACDRestrict == false)) || MACDUse == false)  && Close[0] > SMA(Close, userSMA)[0])
					{
					//	Print("Long condition at : "+Time[0]);
						// If there is a short ATM Strategy running close it.
						if(shortAtmId.Length != 0 && isShortAtmStrategyCreated)
						{
							AtmStrategyClose(shortAtmId);
							isShortAtmStrategyCreated = false;
						}
						// Ensure no other long ATM Strategy is running.
						if(longOrderId.Length == 0 && longAtmId.Length == 0 && !isLongAtmStrategyCreated)
						{
							longOrderId = GetAtmStrategyUniqueId();
							longAtmId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Market, 0, 0, TimeInForce.Day, longOrderId, atmName, longAtmId, (atmCallbackErrorCode, atmCallBackId) => { 
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == longAtmId) 
									isLongAtmStrategyCreated = true;
							});
						}
					}
			
					// 
					//if(CrossBelow(Moneyball1.VBar, mb_lThreshold, LookbackPeriod) && CrossBelow(Qcloud1.V1, Qcloud1.V6, LookbackPeriod) && CrossBelow(Qwave1.K1, Qwave1.VLow, LookbackPeriod) && CrossBelow(MACD1.Default, MACD1.Avg, MACDLookback) && MACD1.Default[0] > mb_lThreshold && Close[0] < SMA(Close, userSMA)[0])
					if(CrossBelow(Moneyball1.VBar, mb_lThreshold, LookbackPeriod) && CrossBelow(Qcloud1.V1, Qcloud1.V6, LookbackPeriod) && CrossBelow(Qwave1.K1, Qwave1.VLow, LookbackPeriod) && (CrossBelow(MACD1.Default, MACD1.Avg, MACDLookback) && (MACD1.Default[CurrentBar - macdBar] > MACDLimit || MACDRestrict == false) || MACDUse == false) && Close[0] < SMA(Close, userSMA)[0])
					{
						Print("Short condition at " + Time[0]);
						// If there is a long ATM Strategy running close it.
						if(longAtmId.Length != 0  && isLongAtmStrategyCreated)
						{
							AtmStrategyClose(longAtmId);
							isLongAtmStrategyCreated = false;
						}
						// Ensure no other short ATM Strategy is running.
						if(shortOrderId.Length == 0 && shortAtmId.Length == 0  && !isShortAtmStrategyCreated)
						{
							shortOrderId = GetAtmStrategyUniqueId();
							shortAtmId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.SellShort, OrderType.Market, 0, 0, TimeInForce.Day, shortOrderId, atmName, shortAtmId, (atmCallbackErrorCode, atmCallBackId) => { 
								//check that the atm strategy create did not result in error, and that the requested atm strategy matches the id in callback
								if (atmCallbackErrorCode == ErrorCode.NoError && atmCallBackId == shortAtmId) 
									isShortAtmStrategyCreated = true;
							});
						}
					}
					// End entries.
				}
			}
			
			Draw.TextFixed(this, "Label1", " Delay Countdown: " + entryDelayCounter + " Current PnL: $" + Math.Round(currentPnL, 2) + " Max Profit: $" + Math.Round(maxProfitLevel, 2) + " Trailing Loss Hit? " + trailingLossHit,
        TextPosition.BottomLeft, Brushes.Black, new NinjaTrader.Gui.Tools.SimpleFont("Arial ", 10) { Size = 12, Bold = true },
        Brushes.Transparent, Brushes.DimGray, 100);
			
			/* -------------------------------------
			
				Daily PnL Block - After Entries
			
			--------------------------------------*/
			
			
			currentPnL = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit - previousRunningProfit; // update daily profit
			
			// Store daily max profit level
			if (currentPnL > maxProfitLevel)
				{
					maxProfitLevel = currentPnL;
				}
				
			// Check if trailing daily loss has hit
			if ((maxProfitLevel > trailingLossAmount || maxDailyLoss == false) && (currentPnL < (maxProfitLevel - trailingLossAmount)) && useTrailingLoss == true && trailingLossAmount > 0)
				{
					trailingLossHit = true;
				}
			
			// Check if Daily PT or SL has been hit
			if ((currentPnL >= maxDailyProfitAmount && maxDailyProfit == true) || (currentPnL <= -maxDailyLossAmount && maxDailyLoss == true))
				{
					okToTrade = false;
					Print("daily limit hit, no new orders" + Time[0].ToString());
				}
				
				
		}
		
		
		
		/* Old PnL
		
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
		
		*/
		
		#region Properties
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Trading Hour Restriction", GroupName = "Time Parameters", Order = 0)]
		public CustomEnumNamespaceIceATM.TimeMode TIMEMODESelect
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
		[Display(Name="ATM Name (No Spaces)", Order=0, GroupName="Trade Parameters")]
		public string atmName
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Default Quantity (Contracts)", Order=1, GroupName="Trade Parameters")]
		public int DQ
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Delay Between Trades", Order=2, GroupName="Trade Parameters")]
		public int entryDelayInput
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Max Daily Profit", Order=3, GroupName="Trade Parameters")]
		public bool maxDailyProfit
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Daily Profit (Currency)", Order=4, GroupName="Trade Parameters")]
		public int maxDailyProfitAmount
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Max Daily Loss", Order=5, GroupName="Trade Parameters")]
		public bool maxDailyLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Daily Loss (Currency)", Order=6, GroupName="Trade Parameters")]
		public int maxDailyLossAmount
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Trailing Daily SL?", Order=7, GroupName="Trade Parameters")]
		public bool useTrailingLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Trailing Daily Stoploss (Currency)", Order=8, GroupName="Trade Parameters")]
		public int trailingLossAmount
		{ get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Lookback Period", Order=9, GroupName="Trade Parameters")]
        public int LookbackPeriod 
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use MACD Condition?", Order=10, GroupName="Trade Parameters")]
		public bool MACDUse
		{ get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="MACD Lookback Period", Order=11, GroupName="Trade Parameters")]
        public int MACDLookback 
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Restrict MACD?", Order=12, GroupName="Trade Parameters")]
		public bool MACDRestrict
		{ get; set; }
		
		[NinjaScriptProperty]
        [Range(1, double.MaxValue)]
        [Display(Name="MACD Limit", Order=13, GroupName="Trade Parameters")]
        public double MACDLimit 
		{ get; set; }
						
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Fast Period", Order=14, GroupName="Trade Parameters")]
        public int FastPeriod 
		{ get; set; }

		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Slow Period", Order=15, GroupName="Trade Parameters")]
        public int SlowPeriod 
		{ get; set; }

		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="Signal Period", Order=16, GroupName="Trade Parameters")]
        public int SignalPeriod 
		{ get; set; }
		
		[NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name="SMA Period", Order=17, GroupName="Trade Parameters")]
        public int userSMA 
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
		
        #endregion
	}
}

namespace CustomEnumNamespaceIceATM
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
}

