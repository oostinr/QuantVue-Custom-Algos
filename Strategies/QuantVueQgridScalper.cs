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
        public class QuantVueQgridScalper : Strategy
        {
			private iGRID_EVO iGRID_EVO1;
			private iGRID_EVO iGRID_EVO2;
			private	CustomEnumNamespaceQgridScalp.TimeMode	TimeModeSelect		= CustomEnumNamespaceQgridScalp.TimeMode.Restricted;
			private DateTime 								startTime 			= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
			private DateTime		 						endTime 			= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
			private double									longMid;
			private double									shortMid;
			private int										grid1Flip;
			private int										tickCount			= 1;
			private int										addEntryCount		= 1;
			private double									longStepMA;
			private double									shortStepMA;
			private double 									currentPnL;
			private Moneyball 								Moneyball1;
			


                protected override void OnStateChange()
                {
                        if (State == State.SetDefaults)
                        {
							Description										= @"This strategy enters on Qgrids add alerts for quick points.";
							Name											= "QuantVueQgridScalper";
							Calculate										= Calculate.OnEachTick;
							
							EntryHandling									= EntryHandling.AllEntries;
 							IsExitOnSessionCloseStrategy					= true;
							ExitOnSessionCloseSeconds						= 30;
 							IsFillLimitOnTouch								= false;
							MaximumBarsLookBack								= MaximumBarsLookBack.TwoHundredFiftySix;
							OrderFillResolution								= OrderFillResolution.Standard;
							Slippage										= 0;
							StartBehavior									= StartBehavior.ImmediatelySubmitSynchronizeAccount;
 							TimeInForce										= TimeInForce.Gtc;
							TraceOrders										= false;
							RealtimeErrorHandling							= RealtimeErrorHandling.IgnoreAllErrors;
							StopTargetHandling								= StopTargetHandling.PerEntryExecution;
							BarsRequiredToTrade								= 20;
							// Disable this property for performance gains in Strategy Analyzer optimizations
							// See the Help Guide for additional information
							IsInstantiatedOnEachOptimizationIteration        = true;
							IsUnmanaged = false;
                                
							TP = 60;
							DQ = 2;
							rrRatio = 3;
							maxDailyProfit = false;
							maxDailyProfitAmount = 500;
							maxDailyLoss = false;
							maxDailyLossAmount = 500;
							grid1Period1 = 55;
							grid1omaL = 19;
							grid1omaS = 2.9;
							grid1omaA = true;
							grid1Sensitivity = 2;
							grid1StepSize = 50;
							grid1Period2 = 8;
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
                            iGRID_EVO1 = iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2);
							Moneyball1 = Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false);                            
                            AddChartIndicator(iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2));
							AddChartIndicator(Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, true, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false));


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

					longStepMA = iGRID_EVO1.StepMA[0];
					shortStepMA = iGRID_EVO1.StepMA[0];
					
					
					if (iGRID_EVO1.FlipSignal[0] == 1)
					{
						grid1Flip = 1;
					}
					else if (iGRID_EVO1.FlipSignal[0] == -1)
					{
						grid1Flip = 2;
					}
					

						if(Position.AveragePrice != 0)
						{
							if(Position.MarketPosition == MarketPosition.Long)
							{
								//SetStopLoss("GoLong", CalculationMode.Price, (longStepMA - (stepMASL * (Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize))), false);
								if (iGRID_EVO1.FlipSignal[0] == -1)
								{
									ExitLong("GoLong");
								}
							}
							else if(Position.MarketPosition == MarketPosition.Short)
							{
								//SetStopLoss("GoShort", CalculationMode.Price, (shortStepMA + (stepMASL * (Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize))), false);
								if (iGRID_EVO1.FlipSignal[0] == 1)
								{
									ExitShort("GoShort");
								}
							}
						}
						
						if(Position.MarketPosition == MarketPosition.Flat)
						{
							
							if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceQgridScalp.TimeMode.Unrestricted)
							{
                        	 	// Set 1
                       	 		if (iGRID_EVO1.AddSignal[0] == 1 && grid1Flip == 1 && Moneyball1.VBar[0] >= mb_uThreshold)
                        		{
									EnterLong(DQ, "GoLong");
									SetProfitTarget("GoLong", CalculationMode.Ticks, TP);
									SetStopLoss("GoLong",CalculationMode.Ticks, TP / rrRatio, false);
                        		}
                        
                         		// Set 2
                        		if (iGRID_EVO1.AddSignal[0] == -1 && grid1Flip == 2 && Moneyball1.VBar[0] <= mb_lThreshold)
                        		{
									EnterShort(DQ, "GoShort");								
									SetProfitTarget("GoShort", CalculationMode.Ticks, TP);
									SetStopLoss("GoShort",CalculationMode.Ticks, TP / rrRatio, false);
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
		[Display(ResourceType = typeof(Custom.Resource), Name = "Trading Hour Restriction", GroupName = "Parameters", Order = 0)]
		public CustomEnumNamespaceQgridScalp.TimeMode TIMEMODESelect
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
		[Display(Name="Take Profit, ticks", Order=9, GroupName="Parameters")]
		public int TP
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Risk Reward Ratio (x):1", Order=10, GroupName="Parameters")]
		public int rrRatio
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Default Contracts", Order=11, GroupName="Parameters")]
		 public int DQ
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Max Daily Profit", Order=12, GroupName="Parameters")]
		public bool maxDailyProfit
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Daily Profit (Currency)", Order=13, GroupName="Parameters")]
		public int maxDailyProfitAmount
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Max Daily Loss", Order=14, GroupName="Parameters")]
		public bool maxDailyLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Max Daily Loss (Currency)", Order=15, GroupName="Parameters")]
		public int maxDailyLossAmount
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

namespace CustomEnumNamespaceQgridScalp
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
	

}