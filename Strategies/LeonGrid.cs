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
        public class LeonGrid : Strategy
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
				private	CustomEnumNamespaceLeonGrid.TimeMode	TimeModeSelect		= CustomEnumNamespaceLeonGrid.TimeMode.Restricted;
				private DateTime 							startTime 			= DateTime.Parse("09:40:00", System.Globalization.CultureInfo.InvariantCulture);
				private DateTime		 					endTime 			= DateTime.Parse("16:00:00", System.Globalization.CultureInfo.InvariantCulture);
				private DateTime 							lunchstartTime 		= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
				private DateTime		 					lunchendTime 		= DateTime.Parse("12:30:00", System.Globalization.CultureInfo.InvariantCulture);
				private double								longMid;
				private double								shortMid;
				private int									grid1Flip;
				private double								StepMAAdd;
				private Series<double> 						StepMAOffset;
				public int									addCountdown;
				public int									maOffsetCountdown;
				private int									entryDelayCounter				= 0;
				private double								maxProfitLevel;
				private bool 								trailingLossHit;
				private int									orderTQ;
				private bool								pt1Set;
				private bool								pt2Set;
				private bool 								isBreakevenSet;
				private int									tickCount;
				private int									tradesCount;
				private bool								okToTrade;
				private new System.Windows.Controls.Button 	btnAllowLongs;
				private new System.Windows.Controls.Button 	btnAllowShorts;
				private new System.Windows.Controls.Button 	btnPauseTrades;
				private new System.Windows.Controls.Button 	btnBE;
				private new System.Windows.Controls.Button 	btnFlatten;
				private bool IsToolBarButtonAdded;
				private Chart chartWindow;
				private bool								allowLongs						=true;
				private bool								allowShorts						=true;
				private bool								pauseTrades						=false;
				private bool								gotoBE;


                protected override void OnStateChange()
                {
                        if (State == State.SetDefaults)
                        {
                                Description                                                                        = @"Enter the description for your new custom Strategy here.";
                                Name                                                                                = "LeonGrid";
                                Calculate                                                                        = Calculate.OnBarClose;
                                EntriesPerDirection                                                        = 3;
                                EntryHandling                                                                = EntryHandling.AllEntries;
                                IsExitOnSessionCloseStrategy                                = true;
                                ExitOnSessionCloseSeconds                                        = 30;
                                IsFillLimitOnTouch                                                        = false;
                                MaximumBarsLookBack                                                        = MaximumBarsLookBack.TwoHundredFiftySix;
                                OrderFillResolution                                                        = OrderFillResolution.Standard;
                                Slippage                                                                        = 0;
                                StartBehavior                                                                = StartBehavior.WaitUntilFlat;
                                TimeInForce                                                                        = TimeInForce.Gtc;
                                TraceOrders                                                                        = true;
                                RealtimeErrorHandling                                                = RealtimeErrorHandling.IgnoreAllErrors;
                                StopTargetHandling                                                        = StopTargetHandling.PerEntryExecution;
                                BarsRequiredToTrade                                                        = 20;
                                // Disable this property for performance gains in Strategy Analyzer optimizations
                                // See the Help Guide for additional information
                                IsInstantiatedOnEachOptimizationIteration        = true;
                                


							addLookback = 10;
							maOffsetLookback = 15;
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
							order1Q = 2;
							order2Q = 1;
							order3Q = 1;
							orderSL = 75;
							orderTP1 = 80;
							orderTP2 = 120;
							slStepSize = 80;
							slFrequency = 10;
							restrictLunch = false;
							
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
							orderTQ = order1Q + order2Q + order3Q;
                        }
						else if (State == State.Realtime)
						{
							//Call the custom method in State.Historical or State.Realtime to ensure it is only done when applied to a chart not when loaded in the Indicators window				
							if (ChartControl != null && !IsToolBarButtonAdded)
							{
							    ChartControl.Dispatcher.InvokeAsync((Action)(() => // Use this.Dispatcher to ensure code is executed on the proper thread
							    {
									AddButtonToToolbar();
								}));
							}
						}
						else if (State == State.Terminated)
						{
							if (chartWindow != null)
							{
						        ChartControl.Dispatcher.InvokeAsync((Action)(() => //Dispatcher used to Assure Executed on UI Thread
						        {	
									DisposeCleanUp();
								}));
							}
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

				#region Program Variables //variables that are handled programatically
			
				double currentDayProfit;		
				double previousRunningProfit;	
				bool eodUpkeep = true; //flag used to ensure that end of day upkeep only happens once per day
			
				#endregion

                protected override void OnBarUpdate()
                {	
					if (Bars.IsFirstBarOfSession)
					{
						currentPnL = 0;
						maxProfitLevel = 0;
						trailingLossHit = false;
						tradesCount = 0;
						okToTrade = true;
						if(order1Q > 0)
						{
							tradesCount++;
						}
						if(order2Q > 0)
						{
							tradesCount++;
						}
						if(order3Q > 0)
						{
							tradesCount++;
						}
						previousRunningProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
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
						gotoBE = false;
					}
					else if(Position.MarketPosition == MarketPosition.Flat && entryDelayCounter > 0)
					{
						entryDelayCounter --;
					}
					
					if (iGRID_EVO1.FlipSignal[0] == 1)
					{
						grid1Flip = 1;
						entryDelayCounter = entryDelayInput;
						Print(string.Format("Grid Flip = {0}", grid1Flip));
						if(Position.MarketPosition != MarketPosition.Flat)
						{
							ExitShort();
						}
					}
					else if (iGRID_EVO1.FlipSignal[0] == -1)
					{
						grid1Flip = 2;
						entryDelayCounter = entryDelayInput;
						Print(string.Format("Grid Flip = {0}", grid1Flip));
						if(Position.MarketPosition != MarketPosition.Flat)
						{
							ExitLong();
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
					
					if (((Position.MarketPosition == MarketPosition.Long) || (Position.MarketPosition == MarketPosition.Short)) 
						&& trailingLossHit == true
						|| (((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) <= -maxDailyLossAmount) && maxDailyLoss == true)
						|| (((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) >= maxDailyProfitAmount) && maxDailyProfit == true)) ///If unrealized goes under maxDailyLossAmount 'OR' Above maxDailyProfitAmount    
					{
						ExitLong();
						ExitShort();
						okToTrade = false;
					}
					
					//Stops and Profit Targets
					if (Position.MarketPosition == MarketPosition.Long && Position.AveragePrice != 0)
					{
						if ((Close[0] > Position.AveragePrice + (orderTP1 * TickSize) && isBreakevenSet == false) || gotoBE == true)
						{
							SetStopLoss(CalculationMode.Price, Position.AveragePrice);
							Print("Average Price at BE"+ Position.AveragePrice);
							isBreakevenSet = true;
						}
						if (Close[0] > Position.AveragePrice + ((slStepSize + (slFrequency * tickCount)) * TickSize) && isBreakevenSet == true) // adjust higher each time by tickCount
						{
							SetStopLoss(CalculationMode.Price, Position.AveragePrice + ((slFrequency * tickCount) * TickSize));
							tickCount ++; // increment to next tick
						}
						
					}
					
					if (Position.MarketPosition == MarketPosition.Short && Position.AveragePrice != 0)
					{
						if ((Close[0] < Position.AveragePrice - (orderTP1 * TickSize) && isBreakevenSet == false) || gotoBE == true)
						{
							SetStopLoss(CalculationMode.Price, Position.AveragePrice);
							Print("Average Price at BE"+ Position.AveragePrice);
							isBreakevenSet = true;
						}
						if (Close[0] < Position.AveragePrice - ((slStepSize + (slFrequency * tickCount)) * TickSize) && isBreakevenSet == true) // adjust higher each time by tickCount
						{
							SetStopLoss(CalculationMode.Price, Position.AveragePrice - ((slFrequency * tickCount) * TickSize));
							tickCount ++; // increment to next tick
						}
						
					}
			
			// Entries.

			if (((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceLeonGrid.TimeMode.Unrestricted) && Position.MarketPosition == MarketPosition.Flat && entryDelayCounter == 0 && ((ToTime(Time[0]) <= ToTime(lunchstartTime) || ToTime(Time[0]) >= ToTime(lunchendTime)) || restrictLunch == false))
			{
				if (okToTrade == true && (useTrailingLoss == false || trailingLossHit == false) && pauseTrades == false)
				{
					
						if(iGRID_EVO1.FlipSignal[0] == 0 && grid1Flip == 1 && addCountdown > 0 && maOffsetCountdown > 0 && Qcloud1.V1[0] > Qcloud1.V6[0] && (Moneyball1.VBar[0] > mb_uThreshold || restrictMoneyball == false) && allowLongs == true)
						{
							Print("Long condition at : "+Time[0]);
							
							if(order1Q > 0)
							{
								EnterLong(order1Q, "LongEntry1");
								SetProfitTarget("LongEntry1", CalculationMode.Ticks, orderTP1); 
							}
							if(order2Q > 0)
							{
								EnterLong(order2Q, "LongEntry2");
								SetProfitTarget("LongEntry2", CalculationMode.Ticks, orderTP2); 
							}
							if(order3Q > 0)
							{
								EnterLong(order3Q, "LongEntry3");
							}
							SetStopLoss(CalculationMode.Ticks, orderSL);
							tickCount = 1;
							isBreakevenSet = false;
						}
					
			
					
						if(iGRID_EVO1.FlipSignal[0] == 0 && grid1Flip == 2 && addCountdown > 0 && maOffsetCountdown > 0 && Qcloud1.V1[0] < Qcloud1.V6[0] && (Moneyball1.VBar[0] < mb_lThreshold || restrictMoneyball == false) && allowShorts == true)
						{
							Print("Short condition at " + Time[0]);
							if(order1Q > 0)
							{
								EnterShort(order1Q, "ShortEntry1");
								SetProfitTarget("ShortEntry1", CalculationMode.Ticks, orderTP1); 
							}
							if(order2Q > 0)
							{
								EnterShort(order2Q, "ShortEntry2");
								SetProfitTarget("ShortEntry2", CalculationMode.Ticks, orderTP2); 
							}
							if(order3Q > 0)
							{
								EnterShort(order3Q, "ShortEntry3");
							}
							SetStopLoss(CalculationMode.Ticks, orderSL);
							tickCount = 1;
							isBreakevenSet = false;

						}
					
				
					// End entries.
				}
			}
			
			Draw.TextFixed(this, "Label1", "Add Countdown: " + addCountdown + " MA Countdown: " + maOffsetCountdown + " Delay Countdown: " + entryDelayCounter + " Current PnL: $" + Math.Round(currentPnL, 2) + " Max Profit: $" + Math.Round(maxProfitLevel, 2) + " Trailing Loss Hit? " + trailingLossHit + " Longs Allowed = " + allowLongs + " Shorts Allowed = " + allowShorts + " Trades Paused = " + pauseTrades + " BE = " + gotoBE,
        TextPosition.BottomLeft, Brushes.Black, new NinjaTrader.Gui.Tools.SimpleFont("Arial ", 10) { Size = 12, Bold = true },
        Brushes.Transparent, Brushes.DimGray, 100);
			
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
				
				
		private void AddButtonToToolbar()
		{
				//Obtain the Chart on which the indicator is configured
				chartWindow = Window.GetWindow(this.ChartControl.Parent) as Chart;
		        if (chartWindow == null)
		        {
		            Print("chartWindow == null");
		            return;
		        }
				
				/*
				// subscribe chartwindow to keypress events
				if (chartWindow != null)
				{
					chartWindow.KeyUp += OnKeyUp;
					chartWindow.MouseLeftButtonDown += OnMouseLeftDown;					
					chartWindow.PreviewMouseWheel += OnMouseWheel;
					chartWindow.MouseEnter += OnMouseEnter;
					chartWindow.MouseLeave += OnMouseLeave;
				}
				*/
				
				// Create a style to apply to the button
		        Style btnStyle = new Style();
		        btnStyle.TargetType = typeof(System.Windows.Controls.Button);
				
		        btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontSizeProperty, 11.0));
		        btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontFamilyProperty, new FontFamily("Arial")));
		        btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.FontWeightProperty, FontWeights.Bold));
				btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.MarginProperty, new Thickness(2, 0, 2, 0)));
				btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.PaddingProperty, new Thickness(4, 2, 4, 2)));
				btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.ForegroundProperty, Brushes.WhiteSmoke));
				btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.BackgroundProperty, Brushes.DimGray));
				btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.IsEnabledProperty, true));
				btnStyle.Setters.Add(new Setter(System.Windows.Controls.Button.HorizontalAlignmentProperty, HorizontalAlignment.Center));
				
		        // Instantiate the buttons
		        btnAllowLongs = new System.Windows.Controls.Button();
				btnAllowShorts = new System.Windows.Controls.Button();
				btnPauseTrades = new System.Windows.Controls.Button();
				btnBE = new System.Windows.Controls.Button();
				btnFlatten = new System.Windows.Controls.Button();
				
				// Set button names
				btnAllowLongs.Content = "Allowing Longs";
				btnAllowShorts.Content = "Allowing Shorts";
				btnPauseTrades.Content = "Trades Allowed";
				btnBE.Content = "BE";
				btnFlatten.Content = "Flatten";
								
		        // Set Button style            
		        btnAllowLongs.Style = btnStyle;
				btnAllowShorts.Style = btnStyle;
				btnPauseTrades.Style = btnStyle;
				btnBE.Style = btnStyle;
				btnFlatten.Style = btnStyle;
				
				// Add the Buttons to the chart's toolbar
				chartWindow.MainMenu.Add(btnAllowLongs);
				chartWindow.MainMenu.Add(btnAllowShorts);
				chartWindow.MainMenu.Add(btnPauseTrades);
				chartWindow.MainMenu.Add(btnBE);
				chartWindow.MainMenu.Add(btnFlatten);
				
				// Set button visibility
				btnAllowLongs.Visibility = Visibility.Visible;
				btnAllowShorts.Visibility = Visibility.Visible;
				btnPauseTrades.Visibility = Visibility.Visible;
				btnBE.Visibility = Visibility.Visible;
				btnFlatten.Visibility = Visibility.Visible;
				
				// Subscribe to click events
				btnAllowLongs.Click += btnAllowLongsClick;
				btnAllowShorts.Click += btnAllowShortsClick;
				btnPauseTrades.Click += btnPauseTradesClick;
		 		btnBE.Click += btnBEClick;
				btnFlatten.Click += btnFlattenClick;
				
				// Set this value to true so it doesn't add the
				// toolbar multiple times if NS code is refreshed
		        IsToolBarButtonAdded = true;
		}		
		
		private void btnAllowLongsClick(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			if (button != null)
			{								
				if (button == btnAllowLongs && button.Content == "Allowing Longs")
				{
					button.Content = "Disallowing Longs";
					allowLongs = false;
					Print("Allow Longs = False" + Time[0].ToString());
					return;
				}
				else if (button == btnAllowLongs && button.Content == "Disallowing Longs")
				{
					button.Content = "Allowing Longs";
					allowLongs = true;
					Print("Allow Longs = True" + Time[0].ToString());
					return;
				}
			}
		}	
		
		private void btnAllowShortsClick(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			if (button != null)
			{								
				if (button == btnAllowShorts && button.Content == "Allowing Shorts")
				{
					button.Content = "Disallowing Shorts";
					allowShorts = false;
					Print("Allow Shorts = False" + Time[0].ToString());
					return;
				}
				else if (button == btnAllowShorts && button.Content == "Disallowing Shorts")
				{
					button.Content = "Allowing Shorts";
					allowShorts = true;
					Print("Allow Shorts = True" + Time[0].ToString());
					return;
				}
			}
		}	
		
		private void btnPauseTradesClick(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			if (button != null)
			{								
				if (button == btnPauseTrades && button.Content == "Trades Allowed")
				{
					button.Content = "Trades Paused";
					pauseTrades = true;
					Print("Trades Paused" + Time[0].ToString());
					return;
				}
				else if (button == btnPauseTrades && button.Content == "Trades Paused")
				{
					button.Content = "Trades Allowed";
					pauseTrades = false;
					Print("Trades Allowed" + Time[0].ToString());
					return;
				}
			}
		}	
		
		private void btnBEClick(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			if (button != null)
			{								
				if (Position.MarketPosition == MarketPosition.Short && Position.AveragePrice != 0 && Close[0] < Position.AveragePrice)
				{
					gotoBE = true;
					SetStopLoss(CalculationMode.Price, Position.AveragePrice);
					Print("Go to BE" + Time[0].ToString());
				}
				else if(Position.MarketPosition == MarketPosition.Long && Position.AveragePrice != 0 && Close[0] > Position.AveragePrice)
				{
					gotoBE = true;
					SetStopLoss(CalculationMode.Price, Position.AveragePrice);
					Print("Go to BE" + Time[0].ToString());
				}
			}
		}	
		
		private void btnFlattenClick(object sender, RoutedEventArgs e)
		{
			System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
			if (button != null)
			{								
				ExitLong();
				ExitShort();
				Print("Flatten" + Time[0].ToString());
			}
		}	
		
		private void DisposeCleanUp()
		{
			/*
		    // remove toolbar items and unsubscribe from events
			chartWindow.KeyUp -= OnKeyUp;
			chartWindow.MouseLeftButtonDown -= OnMouseLeftDown;
			chartWindow.PreviewMouseWheel -= OnMouseWheel;
			chartWindow.MouseEnter -= OnMouseEnter;
			chartWindow.MouseLeave -= OnMouseLeave;
			*/
						
            if (btnAllowLongs != null) chartWindow.MainMenu.Remove(btnAllowLongs);
				btnAllowLongs.Click -= btnAllowLongsClick;
			if (btnAllowShorts != null) chartWindow.MainMenu.Remove(btnAllowShorts);
				btnAllowShorts.Click -= btnAllowShortsClick;
			if (btnPauseTrades != null) chartWindow.MainMenu.Remove(btnPauseTrades);
				btnPauseTrades.Click -= btnPauseTradesClick;
			if (btnBE != null) chartWindow.MainMenu.Remove(btnBE);
				btnBE.Click -= btnBEClick;
			if (btnFlatten != null) chartWindow.MainMenu.Remove(btnFlatten);
				btnFlatten.Click -= btnFlattenClick;
		}
		
		
				
		#region Properties
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Trading Hour Restriction", GroupName = "1. Time Parameters", Order = 0)]
		public CustomEnumNamespaceLeonGrid.TimeMode TIMEMODESelect
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
		[Display(Name="Restrict Lunch Trading?", Order=3, GroupName="1. Time Parameters")]
		public bool restrictLunch
		{ get; set; }
				
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [NinjaScriptProperty]
        [Display(Name = "Lunch Range-Start", GroupName = "1. Time Parameters", Order = 4)]
        public DateTime lunchStartTime 
		{
			get { return lunchstartTime; }
			set { lunchstartTime = value; }
		}
		
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
       	[NinjaScriptProperty]
       	[Display(Name = "Lunch Range-End", GroupName = "1. Time Parameters", Order = 5)]
        public DateTime lunchEndTime
		{
			get { return lunchendTime; }
			set { lunchendTime = value; }
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
		[Range(0, int.MaxValue)]
		[Display(Name="Order 1 Quantity", Order=6, GroupName="3. Entry Parameters")]
		public int order1Q
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Order 1 Profit Target (Ticks)", Order=7, GroupName="3. Entry Parameters")]
		public int orderTP1
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Order 2 Quantity", Order=8, GroupName="3. Entry Parameters")]
		public int order2Q
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Order 2 Profit Target (Ticks)", Order=9, GroupName="3. Entry Parameters")]
		public int orderTP2
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Order 3 Quantity", Order=10, GroupName="3. Entry Parameters")]
		public int order3Q
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Order Stoploss", Order=1, GroupName="4. Stoploss Parameters")]
		public int orderSL
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Stoploss Step Distance (Ticks)", GroupName = "4. Stoploss Parameters", Order = 2)]
		public int slStepSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Stoploss Step Frequency (Ticks)", GroupName = "4. Stoploss Parameters", Order = 3)]
		public int slFrequency
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

namespace CustomEnumNamespaceLeonGrid
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
}