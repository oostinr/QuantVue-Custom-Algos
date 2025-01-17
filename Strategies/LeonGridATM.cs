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
        public class LeonGridATM : Strategy
        {
                private iGRID_EVO iGRID_EVO1;
				private iGRID_EVO iGRID_EVO2;
				private QMomentum QMomentum1;
				private Moneyball Moneyball1;
				private Qcloud Qcloud1;
				private string									longAtmId					= string.Empty; // Atm Id for long.
				private string									longOrderId					= string.Empty; // Order Id for long.
				private string									shortAtmId					= string.Empty; // Atm Id for short.
				private string									shortOrderId				= string.Empty; // Order Id for short.
				private bool 									isLongAtmStrategyCreated 	= false;
				private bool									isShortAtmStrategyCreated	= false;
				private int 									priorTradesCount 			= 0;
				private double 									priorTradesCumProfit		= 0;
				private double 									currentPnL;
				private	CustomEnumNamespaceLeonGridATM.TimeMode	TimeModeSelect		= CustomEnumNamespaceLeonGridATM.TimeMode.Restricted;
				private DateTime 							startTime 			= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
				private DateTime		 					endTime 			= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
				private double								longMid;
				private double								shortMid;
				private int									grid1Flip;
				private double								StepMAAdd;
				public Series<double> 						StepMAOffset;
				public int									addCountdown;
				public int									maOffsetCountdown;
				private int									entryDelayCounter				= 0;
				private double								maxProfitLevel;
				private bool 								trailingLossHit;


                protected override void OnStateChange()
                {
                        if (State == State.SetDefaults)
                        {
                                Description                                                                        = @"Enter the description for your new custom Strategy here.";
                                Name                                                                                = "LeonGridATM";
                                Calculate                                                                        = Calculate.OnBarClose;
                                EntriesPerDirection                                                        = 1;
                                EntryHandling                                                                = EntryHandling.AllEntries;
                                IsExitOnSessionCloseStrategy                                = true;
                                ExitOnSessionCloseSeconds                                        = 30;
                                IsFillLimitOnTouch                                                        = false;
                                MaximumBarsLookBack                                                        = MaximumBarsLookBack.TwoHundredFiftySix;
                                OrderFillResolution                                                        = OrderFillResolution.Standard;
                                Slippage                                                                        = 0;
                                StartBehavior                                                                = StartBehavior.ImmediatelySubmitSynchronizeAccount;
                                TimeInForce                                                                        = TimeInForce.Gtc;
                                TraceOrders                                                                        = true;
                                RealtimeErrorHandling                                                = RealtimeErrorHandling.IgnoreAllErrors;
                                StopTargetHandling                                                        = StopTargetHandling.PerEntryExecution;
                                BarsRequiredToTrade                                                        = 20;
                                // Disable this property for performance gains in Strategy Analyzer optimizations
                                // See the Help Guide for additional information
                                IsInstantiatedOnEachOptimizationIteration        = true;
                                


							addLookback = 5;
							maOffsetLookback = 5;
							mb_Nb_bars = 15;
							mb_period = 10;
							mb_zero = true;
							mb_uThreshold = 0.35;
							mb_lThreshold = -0.35;
							mb_Sensitivity = 0.1;
							atmName = "LeonGridATM";
							maxDailyProfit = false;
							maxDailyProfitAmount = 500;
							maxDailyLoss = false;
							maxDailyLossAmount = 500;
							grid1Period1 = 19;
							grid1omaL = 19;
							grid1omaS = 2.9;
							grid1omaA = true;
							grid1Sensitivity = 2;
							grid1StepSize = 50;
							grid1Period2 = 7;
							restrictMoneyball = false;
							qCloud1p1 = 39;
							qCloud1p2 = 49;
							qCloud1p3 = 59;
							qCloud1p4 = 69;
							qCloud1p5 = 79;
							qCloud1p6 = 129;
							maOffsetSize = 20;
							entryDelayInput = 10;
                        }
                        else if (State == State.Configure)
                        {
                                
                        }
                        else if (State == State.DataLoaded)
                        {                                
                            iGRID_EVO1 = iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2);
							Qcloud1 = Qcloud(Close, Brushes.Blue, Brushes.Cyan, qCloud1p1, qCloud1p2, qCloud1p3, qCloud1p4, qCloud1p5, qCloud1p6, false);
                            AddChartIndicator(iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2));
							AddChartIndicator(Qcloud(Close, Brushes.Blue, Brushes.Cyan, qCloud1p1, qCloud1p2, qCloud1p3, qCloud1p4, qCloud1p5, qCloud1p6, false));
							Moneyball1 = Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false);
							AddChartIndicator(Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false));
							StepMAOffset = new Series<double>(this, MaximumBarsLookBack.Infinite);

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
					if(State == State.Historical)
						return;			
			
					if (Bars.IsFirstBarOfSession)
					{
						currentPnL = 0;
						maxProfitLevel = 0;
						trailingLossHit = false;
					}
			
					if (BarsInProgress != 0) 
						return;

					if (CurrentBars[0] < BarsRequiredToTrade)
						return;
					
					if (Position.MarketPosition != MarketPosition.Flat)
					{
						entryDelayCounter = entryDelayInput;
						maOffsetCountdown = 0;
						addCountdown = 0;
					}
					else if(Position.MarketPosition == MarketPosition.Flat && entryDelayCounter > 0)
					{
						entryDelayCounter --;
					}
						
					if (iGRID_EVO1.FlipSignal[1] == 1)
					{
						grid1Flip = 1;
						entryDelayCounter = entryDelayInput;
						Print(string.Format("Grid Flip = {0}", grid1Flip));
						if(Position.MarketPosition != MarketPosition.Flat)
						{
							AtmStrategyClose(atmName);
						}
					}
					else if (iGRID_EVO1.FlipSignal[1] == -1)
					{
						grid1Flip = 2;
						entryDelayCounter = entryDelayInput;
						Print(string.Format("Grid Flip = {0}", grid1Flip));
						if(Position.MarketPosition != MarketPosition.Flat)
						{
							AtmStrategyClose(atmName);
						}
					}
					
					if (addCountdown > 0)
					{
						addCountdown --;
						Print(string.Format("Add Countdown = {0}", addCountdown));
					}
					
					if (maOffsetCountdown > 0)
					{
						maOffsetCountdown --;
						Print(string.Format("Offset Countdown = {0}", maOffsetCountdown));
					}
					
					Print(string.Format("StepMA = {0}", iGRID_EVO1.StepMA[0]));
					
					if (grid1Flip == 1)
					{
						StepMAOffset[0] = iGRID_EVO1.StepMA[0] + (maOffsetSize * TickSize);
						Print(string.Format("StepMA Offset = {0}", StepMAOffset[0]));
					}
					else if (grid1Flip == 2)
					{
						StepMAOffset[0] = iGRID_EVO1.StepMA[0] - (maOffsetSize * TickSize);
						Print(string.Format("StepMA Offset = {0}", StepMAOffset[0]));
					}
					
					if ((iGRID_EVO1.AddSignal[1] == -1 || iGRID_EVO1.AddSignal[1] == 1) && Position.MarketPosition == MarketPosition.Flat)
					{
						addCountdown = addLookback;
					}
					
					
					if ((grid1Flip == 1 && StepMAOffset[1] > Close[1]) && Position.MarketPosition == MarketPosition.Flat)
					{
						maOffsetCountdown = maOffsetLookback;
					}
					else if ((grid1Flip == 2 && StepMAOffset[1] < Close[1]) && Position.MarketPosition == MarketPosition.Flat)
					{
						maOffsetCountdown = maOffsetLookback;
					}
					
					if (((Position.MarketPosition == MarketPosition.Long) || (Position.MarketPosition == MarketPosition.Short)) &&  (  (((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) <= -maxDailyLossAmount) && maxDailyLoss == true) || (((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) >= maxDailyProfitAmount) && maxDailyProfit == true)  )) ///If unrealized goes under maxDailyLossAmount 'OR' Above maxDailyProfitAmount    
				
					{
						AtmStrategyClose(atmName);
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
				
				entryDelayCounter = entryDelayInput;
						maOffsetCountdown = 0;
						addCountdown = 0;
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
				
				entryDelayCounter = entryDelayInput;
						maOffsetCountdown = 0;
						addCountdown = 0;
			}
			// End check.
			
			// Entries.
			// **** YOU MUST HAVE AN ATM STRATEGY TEMPLATE NAMED 'IcebergATM' CREATED IN NINJATRADER (SUPERDOM FOR EXAMPLE) FOR THIS TO WORK ****
			// Enter long if Close is greater than Open.
			if (((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceLeonGridATM.TimeMode.Unrestricted) && Position.MarketPosition == MarketPosition.Flat && entryDelayCounter == 0)
			{
				if ((currentPnL <= maxDailyProfitAmount || maxDailyProfit == false) || (currentPnL >= -maxDailyLossAmount || maxDailyLoss == false))
				{
			
					if(iGRID_EVO1.FlipSignal[0] == 0 && grid1Flip == 1 && addCountdown > 0 && maOffsetCountdown > 0 && Qcloud1.V1[0] > Qcloud1.V6[0] && (Moneyball1.VBar[0] > mb_uThreshold || restrictMoneyball == false))
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
					if(iGRID_EVO1.FlipSignal[0] == 0 && grid1Flip == 2 && addCountdown > 0 && maOffsetCountdown > 0 && Qcloud1.V1[0] < Qcloud1.V6[0] && (Moneyball1.VBar[0] < mb_lThreshold || restrictMoneyball == false))
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
			
			Draw.TextFixed(this, "Label1", "Add Countdown: " + addCountdown + " MA Countdown: " + maOffsetCountdown + " Delay Countdown: " + entryDelayCounter + " Max Profit: $" + maxProfitLevel + "Trailing Loss Hit? " + trailingLossHit,
        TextPosition.BottomLeft, Brushes.Black, new NinjaTrader.Gui.Tools.SimpleFont("Arial ", 10) { Size = 12, Bold = true },
        Brushes.Transparent, Brushes.DimGray, 100);

			//Draw.TextFixed(this, "addCountdown", addCountdown.ToString("G"), TextPosition.TopRight);
		}
		
		protected override void OnPositionUpdate(Position position, double averagePrice, int quantity, MarketPosition marketPosition)
		{
			if (Position.MarketPosition == MarketPosition.Flat && SystemPerformance.AllTrades.Count > 0)
			{
				// when a position is closed, add the last trade's Profit to the currentPnL
				currentPnL += SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1].ProfitCurrency;
				
				if (currentPnL > maxProfitLevel)
				{
					maxProfitLevel = currentPnL;
				}
				
				if (currentPnL < (maxProfitLevel - trailingLossAmount) && useTrailingLoss == true && trailingLossAmount > 0)
				{
					trailingLossHit = true;
				}

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
		[Display(ResourceType = typeof(Custom.Resource), Name = "Trading Hour Restriction", GroupName = "1. Time Parameters", Order = 0)]
		public CustomEnumNamespaceLeonGridATM.TimeMode TIMEMODESelect
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
		[Display(Name="Max Daily Profit", Order=3, GroupName="2. PnL Parameters")]
		public bool maxDailyProfit
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Daily Profit (Currency)", Order=4, GroupName="2. PnL Parameters")]
		public int maxDailyProfitAmount
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Max Daily Loss", Order=5, GroupName="2. PnL Parameters")]
		public bool maxDailyLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Daily Loss (Currency)", Order=6, GroupName="2. PnL Parameters")]
		public int maxDailyLossAmount
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Trailing Daily Stoploss (Currency)", Order=7, GroupName="2. PnL Parameters")]
		public int trailingLossAmount
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Trailing Daily SL?", Order=8, GroupName="2. PnL Parameters")]
		public bool useTrailingLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="ATM Name (No Spaces)", Order=0, GroupName="3. Entry Parameters")]
		public string atmName
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Add Alert Lookback", Order=1, GroupName="3. Entry Parameters")]
		public int addLookback
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StepMA Offset Lookback", Order=2, GroupName="3. Entry Parameters")]
		public int maOffsetLookback
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="StepMA Offset Size", Order=3, GroupName="3. Entry Parameters")]
		public int maOffsetSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Restrict Entry With Moneyball", Order=4, GroupName="3. Entry Parameters")]
		public bool restrictMoneyball
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Delay Between Trades", Order=5, GroupName="3. Entry Parameters")]
		public int entryDelayInput
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
		[Range(0, int.MaxValue)]
		[Display(Name="HA Smooth Period 1", Order=1, GroupName="Qgrid 1 Parameters")]
		public int grid1Period1
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="OMA Length", Order=2, GroupName="Qgrid 1 Parameters")]
		public int grid1omaL
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="OMA Speed", Order=3, GroupName="Qgrid 1 Parameters")]
		public double grid1omaS
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Adaptive OMA", Order=4, GroupName="Qgrid 1 Parameters")]
		public bool grid1omaA
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Sensitivity", Order=5, GroupName="Qgrid 1 Parameters")]
		public double grid1Sensitivity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Step Size", Order=6, GroupName="Qgrid 1 Parameters")]
		public double grid1StepSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="HA Smooth Period 2", Order=7, GroupName="Qgrid 1 Parameters")]
		public int grid1Period2
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Qcloud Period 1", Order=0, GroupName="Qcloud 1 Parameters")]
		public int qCloud1p1
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Qcloud Period 2", Order=1, GroupName="Qcloud 1 Parameters")]
		public int qCloud1p2
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Qcloud Period 3", Order=2, GroupName="Qcloud 1 Parameters")]
		public int qCloud1p3
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Qcloud Period 4", Order=3, GroupName="Qcloud 1 Parameters")]
		public int qCloud1p4
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Qcloud Period 5", Order=4, GroupName="Qcloud 1 Parameters")]
		public int qCloud1p5
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Qcloud Period 6", Order=5, GroupName="Qcloud 1 Parameters")]
		public int qCloud1p6
		{ get; set; }
		
               #endregion
        }				
		
}

namespace CustomEnumNamespaceLeonGridATM
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
}