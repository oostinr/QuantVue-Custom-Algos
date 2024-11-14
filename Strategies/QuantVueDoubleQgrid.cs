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
        public class QuantVueDoubleQgrid : Strategy
        {
			private iGRID_EVO iGRID_EVO1;
			private iGRID_EVO iGRID_EVO2;
			private	CustomEnumNamespaceDoubleGrid.TimeMode	TimeModeSelect		= CustomEnumNamespaceDoubleGrid.TimeMode.Restricted;
			private DateTime 								startTime 			= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
			private DateTime		 						endTime 			= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
			private double									longMid;
			private double									shortMid;
			private int										grid2Flip;
			private	CustomEnumNamespaceDoubleGrid.QgridMode	QgridModeSelect		= CustomEnumNamespaceDoubleGrid.QgridMode.Official;
			private int										tickCount			= 1;
			private int										addEntryCount		= 1;
			private double									longStepMA;
			private double									shortStepMA;
			private double 									currentPnL;
			private bool									scaleSell			= false;
			private int										contractCount		= 0;

			


                protected override void OnStateChange()
                {
                        if (State == State.SetDefaults)
                        {
							Description										= @"This strategy adds a second slower qgrid to minimize chop entries.";
							Name											= "QuantVueDoubleQgrid";
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
                                
							TP = 80;
							SL = 50;
							DQ = 2;
							flipEntryQ = 2;
							addEntryQ = 1;
							addEntryCount = 0;
							maxEntries = 3;
							stepMASL = 200;
							profitTarget = 500;
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
							grid2Period1 = 200;
							grid2omaL = 110;
							grid2omaS = 5;
							grid2omaA = true;
							grid2Sensitivity = 10;
							grid2StepSize = 500;
							grid2Period2 = 20;
							
                        }
                        else if (State == State.Configure)
                        {
							DefaultQuantity = DQ;
							EntriesPerDirection	= flipEntryQ + (addEntryQ * maxEntries);
                        }
                        else if (State == State.DataLoaded)
                        {                                
                            iGRID_EVO1 = iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2);
							iGRID_EVO2 = iGRID_EVO(Close, grid2Period1, grid2omaL, grid2omaS, grid2omaA, grid2Sensitivity, grid2StepSize, grid2Period2);
                            
                            AddChartIndicator(iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2));
							AddChartIndicator(iGRID_EVO(Close, grid2Period1, grid2omaL, grid2omaS, grid2omaA, grid2Sensitivity, grid2StepSize, grid2Period2));

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

					
					
					if (iGRID_EVO2.FlipSignal[0] == 1)
					{
						grid2Flip = 1;
					}
					else if (iGRID_EVO2.FlipSignal[0] == -1)
					{
						grid2Flip = 2;
					}
					
					if(QgridModeSelect == CustomEnumNamespaceDoubleGrid.QgridMode.Official)
					{
						if(Position.AveragePrice != 0)
						{
							if(Position.MarketPosition == MarketPosition.Long)
							{
								if(addEntryCount == 0)
								{
									SetProfitTarget("GoLong", CalculationMode.Price, Position.AveragePrice + (profitTarget * (Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize)));
								}
								//SetStopLoss("GoLong", CalculationMode.Price, (iGRID_EVO1.StepMA[0] - (stepMASL * (Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize))), false);
								if (iGRID_EVO1.FlipSignal[0] == -1)
								{
									ExitLong("GoLong");
								}
								if (addEntryCount < maxEntries && iGRID_EVO1.AddSignal[0] == 1)
								{
									EnterLong(addEntryQ, "GoLong");
									addEntryCount ++;
								}
							}
							else if(Position.MarketPosition == MarketPosition.Short)
							{
								if(addEntryCount == 0)
								{
									SetProfitTarget("GoShort", CalculationMode.Price, Position.AveragePrice - (profitTarget * (Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize)));
								}
								//SetStopLoss("GoShort", CalculationMode.Price, (iGRID_EVO1.StepMA[0] + (stepMASL * (Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize))), false);
								if (iGRID_EVO1.FlipSignal[0] == 1)
								{
									ExitShort("GoShort");
								}
								if (addEntryCount < maxEntries && iGRID_EVO1.AddSignal[0] == -1)
								{
									EnterShort(addEntryQ, "GoShort");
									addEntryCount ++;
								}
							}
						}
						
						if(Position.MarketPosition == MarketPosition.Flat)
						{
							
							if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceDoubleGrid.TimeMode.Unrestricted)
							{
                        	 	// Set 1
                       	 		if (iGRID_EVO1.FlipSignal[0] == 1 && grid2Flip == 1)
                        		{
									EnterLong(flipEntryQ, "GoLong");
									longStepMA = iGRID_EVO1.StepMA[0];
									Print(string.Format("LongStepMA is {0}", longStepMA));
									//SetProfitTarget(CalculationMode.Ticks, profitTarget);
									addEntryCount = 0;
                        		}
                        
                         		// Set 2
                        		if (iGRID_EVO1.FlipSignal[0] == -1 && grid2Flip == 2)
                        		{
									EnterShort(flipEntryQ, "GoShort");
									shortStepMA = iGRID_EVO1.StepMA[0];
									Print(string.Format("ShortStepMA is {0}", shortStepMA));
									//SetProfitTarget(CalculationMode.Ticks, profitTarget);
									addEntryCount = 0;
								}
							}
						}
					}
					
					if(QgridModeSelect == CustomEnumNamespaceDoubleGrid.QgridMode.CurrencySL)
					{

						//   SL Move to buy entry
						if(Position.AveragePrice != 0)
						{
							if(Position.MarketPosition == MarketPosition.Long)
							{
								longMid = Instrument.MasterInstrument.RoundToTickSize(Position.AveragePrice + ((TP / Instrument.MasterInstrument.PointValue) * 0.25));
								Print(string.Format("Long Mid is {0}", longMid));
								if(Close[0] > longMid)
								{
									SetStopLoss(CalculationMode.Price, Position.AveragePrice);
								}
							}
							else if(Position.MarketPosition == MarketPosition.Short)
							{
								shortMid = Instrument.MasterInstrument.RoundToTickSize(Position.AveragePrice - ((TP / Instrument.MasterInstrument.PointValue) * 0.25));
								Print(string.Format("Short Mid is {0}", shortMid));
								if(Close[0] < shortMid)
								{
									SetStopLoss(CalculationMode.Price, Position.AveragePrice);
								}
							}
						}
								
						if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceDoubleGrid.TimeMode.Unrestricted)
						{
                         	// Set 1
                        	if (iGRID_EVO1.FlipSignal[0] == 1 && grid2Flip == 1)
                        	{
								EnterLong(Convert.ToInt32(DefaultQuantity), "");
								SetStopLoss(CalculationMode.Currency, SL);
								SetProfitTarget(CalculationMode.Currency, TP);
                        	}
                        
                         	// Set 2
                        	if (iGRID_EVO1.FlipSignal[0] == -1 && grid2Flip == 2)
                        	{
                                EnterShort(Convert.ToInt32(DefaultQuantity), "");
								SetStopLoss(CalculationMode.Currency, SL);
								SetProfitTarget(CalculationMode.Currency, TP);
                        	}
                        }
					}
					
					if(QgridModeSelect == CustomEnumNamespaceDoubleGrid.QgridMode.Scaling)
					{
						if(Position.AveragePrice != 0)
						{
							if(addEntryCount == maxEntries)
							{
								scaleSell = true;
							}
							
							if(Position.MarketPosition == MarketPosition.Long)
							{
								if(addEntryCount == 0)
								{
									SetProfitTarget("GoLong", CalculationMode.Price, Position.AveragePrice + (profitTarget * (Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize)));
								}
								
								if (iGRID_EVO1.FlipSignal[0] == -1)
								{
									ExitLong("GoLong");
								}
								
								if (addEntryCount < maxEntries && iGRID_EVO1.AddSignal[0] == 1 && scaleSell == false)
								{
									EnterLong(addEntryQ, "GoLong");
									addEntryCount ++;
									contractCount ++;
								}
								
								if (addEntryCount == maxEntries && iGRID_EVO1.AddSignal[0] == 1 && scaleSell == true && contractCount > 1)
								{
									ExitShort(1, "ExitLong", "GoLong");
									contractCount --;
								}
							}
							else if(Position.MarketPosition == MarketPosition.Short)
							{
								if(addEntryCount == 0)
								{
									SetProfitTarget("GoShort", CalculationMode.Price, Position.AveragePrice - (profitTarget * (Instrument.MasterInstrument.PointValue * Instrument.MasterInstrument.TickSize)));
								}
								
								if (iGRID_EVO1.FlipSignal[0] == 1)
								{
									ExitShort("GoShort");
								}
								
								if (addEntryCount < maxEntries && iGRID_EVO1.AddSignal[0] == -1 && scaleSell == false)
								{
									EnterShort(addEntryQ, "GoShort");
									addEntryCount ++;
									contractCount ++;
								}
								
								if (addEntryCount == maxEntries && iGRID_EVO1.AddSignal[0] == -1 && scaleSell == true && contractCount > 1)
								{
									ExitShort(1, "ExitShort", "GoShort");
									contractCount --;
								}
							}
						}
						
						if(Position.MarketPosition == MarketPosition.Flat)
						{
							
							if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceDoubleGrid.TimeMode.Unrestricted)
							{
                        	 	// Set 1
                       	 		if (iGRID_EVO1.FlipSignal[0] == 1 && grid2Flip == 1)
                        		{
									EnterLong(flipEntryQ, "GoLong");
									longStepMA = iGRID_EVO1.StepMA[0];
									Print(string.Format("LongStepMA is {0}", longStepMA));
									addEntryCount = 0;
									contractCount = flipEntryQ;
									scaleSell = false;
                        		}
                        
                         		// Set 2
                        		if (iGRID_EVO1.FlipSignal[0] == -1 && grid2Flip == 2)
                        		{
									EnterShort(flipEntryQ, "GoShort");
									shortStepMA = iGRID_EVO1.StepMA[0];
									Print(string.Format("ShortStepMA is {0}", shortStepMA));
									addEntryCount = 0;
									contractCount = flipEntryQ;
									scaleSell = false;
								}
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
		public CustomEnumNamespaceDoubleGrid.TimeMode TIMEMODESelect
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
		[Display(ResourceType = typeof(Custom.Resource), Name = "Qgrid Mode", GroupName = "Parameters", Order = 3)]
		public CustomEnumNamespaceDoubleGrid.QgridMode QGRIDMODESelect
		{
			get { return QgridModeSelect; }
			set { QgridModeSelect = value; }
		}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Official - Flip Entry Quantity", Order=4, GroupName="Parameters")]
		public int flipEntryQ
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Official - > Add Entry Quantity", Order=5, GroupName="Parameters")]
		public int addEntryQ
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Official - Max Entries", Order=6, GroupName="Parameters")]
		public int maxEntries
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Official - Stop Distance from Step MA, ticks", Order=7, GroupName="Parameters")]
		public int stepMASL
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Official - Profit Target, ticks", Order=8, GroupName="Parameters")]
		public int profitTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="CurrencySL - TP", Order=9, GroupName="Parameters")]
		public int TP
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="CurrencySL - SL", Order=10, GroupName="Parameters")]
		public int SL
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="CurrencySL - DQ", Order=11, GroupName="Parameters")]
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
		[Range(0, int.MaxValue)]
		[Display(Name="HA Smooth Period 1", Order=1, GroupName="Qgrid 2 Parameters")]
		public int grid2Period1
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="OMA Length", Order=2, GroupName="Qgrid 2 Parameters")]
		public int grid2omaL
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="OMA Speed", Order=3, GroupName="Qgrid 2 Parameters")]
		public double grid2omaS
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Adaptive OMA", Order=4, GroupName="Qgrid 2 Parameters")]
		public bool grid2omaA
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Sensitivity", Order=5, GroupName="Qgrid 2 Parameters")]
		public double grid2Sensitivity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Step Size", Order=6, GroupName="Qgrid 2 Parameters")]
		public double grid2StepSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="HA Smooth Period 2", Order=7, GroupName="Qgrid 2 Parameters")]
		public int grid2Period2
		{ get; set; }

                #endregion
        }
}

namespace CustomEnumNamespaceDoubleGrid
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
	
	public enum QgridMode
	{
		CurrencySL,
		Official,
		Scaling
	}
}
