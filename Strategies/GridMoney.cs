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
	public class GridMoney : Strategy
	{
		private Moneyball Moneyball1;
		private iGRID_EVO iGRID_EVO1;
		private	CustomEnumNamespaceGridMoney.TimeMode	TimeModeSelect		= CustomEnumNamespaceGridMoney.TimeMode.Restricted;
		private DateTime 								startTime 			= DateTime.Parse("9:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime		 						endTime 			= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime 								lunchstartTime 		= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime		 						lunchendTime 		= DateTime.Parse("12:30:00", System.Globalization.CultureInfo.InvariantCulture);
		private double currentPnL;
		private int										grid1Flip;
		private int										bullbearHA2;
		private bool								okToTrade;
		private double								maxProfitLevel;
		private bool 								trailingLossHit;
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
		private bool								gotoBELong;
		private bool								gotoBEShort;
		private bool								longEntrySet;
		private bool								shortEntrySet;


		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "GridMoney";
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
				
				DQ = 1;
				mb_Nb_bars = 15;
				mb_period = 10;
				mb_zero = true;
				mb_uThreshold = 0.35;
				mb_lThreshold = -0.35;
				mb_Sensitivity = 0.1;
				grid1Period1 = 55;
				grid1omaL = 19;
				grid1omaS = 2.9;
				grid1omaA = true;
				grid1Sensitivity = 2;
				grid1StepSize = 50;
				grid1Period2 = 8;
				maxDailyProfit = false;
				maxDailyProfitAmount = 500;
				maxDailyLoss = false;
				maxDailyLossAmount = 500;
				restrictLunch = false;
				useMoneyballEntry = true;
				useHA2Entry = true;
				useqGridEntry = true;
				useExitTarget = false;
				exitTargetTicks = 0;
				useExitHA2 = true;
			}
			else if (State == State.Configure)
			{
				DefaultQuantity = DQ;
			}
			else if (State == State.DataLoaded)
			{				
				Moneyball1 = Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false);
				AddChartIndicator(Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, true, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false));
				iGRID_EVO1 = iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2);
                AddChartIndicator(iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2));
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
						okToTrade = true;
						previousRunningProfit = SystemPerformance.AllTrades.TradesPerformance.Currency.CumProfit;
					}
			
			if (BarsInProgress != 0) 
				return;

			if (CurrentBars[0] < 5)
				return;
			
			if (iGRID_EVO1.FlipSignal[0] == 1)
			{
				grid1Flip = 1;
			}
			else if (iGRID_EVO1.FlipSignal[0] == -1)
			{
				grid1Flip = 2;
			}
			
			if (iGRID_EVO1.HA2Close[0] < iGRID_EVO1.HA2Open[0])
			{
				bullbearHA2 = 2;
				Print(string.Format("BullBearHA2 = {0}", bullbearHA2));
				longEntrySet = false;
				if(Position.MarketPosition == MarketPosition.Flat)
				{
					gotoBELong = false;
				}
			}
			else if (iGRID_EVO1.HA2Close[0] > iGRID_EVO1.HA2Open[0])
			{
				bullbearHA2 = 1;
				Print(string.Format("BullBearHA2 = {0}", bullbearHA2));
				shortEntrySet = false;
				if(Position.MarketPosition == MarketPosition.Flat)
				{
					gotoBEShort = false;
				}
			}
			
			if (Position.MarketPosition == MarketPosition.Long && (bullbearHA2 == 2 || useExitHA2 == false) && (Moneyball1.VBar[0] < exit_mb_uThreshold || useMoneyballExit == false))
			{
				ExitLong("GoLong");
			}
			else if (Position.MarketPosition == MarketPosition.Short && (bullbearHA2 == 1 || useExitHA2 == false) && (Moneyball1.VBar[0] > exit_mb_lThreshold || useMoneyballExit == false))
			{
				ExitShort("GoShort");
			}
			
			
			if (((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceGridMoney.TimeMode.Unrestricted) && trailingLossHit == false && okToTrade == true && Position.MarketPosition == MarketPosition.Flat && ((currentPnL <= maxDailyProfitAmount || maxDailyProfit == false) && (currentPnL >= -maxDailyLossAmount || maxDailyLoss == false)) && pauseTrades == false)
			{
				
				/*
				// Set 1
				if (CrossAbove(Moneyball1.VBar, mb_uThreshold, 1) && grid1Flip == 1 && bullbearHA2 == 1)
				{
					EnterLong(DefaultQuantity, "GoLong");
					//SetProfitTarget("GoLong", CalculationMode.Currency, TP);
					//SetStopLoss("GoLong",CalculationMode.Currency, SL, false);
				}
				
				if (CrossBelow(Moneyball1.VBar, mb_lThreshold, 1) && grid1Flip == 2 && bullbearHA2 == 2)
				{
					EnterShort(DefaultQuantity, "GoShort");
					//SetProfitTarget("GoShort", CalculationMode.Currency, TP);
					//SetStopLoss("GoShort",CalculationMode.Currency, SL, false);
				}
				*/
				
				// Set 1
				if ((Moneyball1.VBar[0] > mb_uThreshold || useMoneyballEntry == false) && (grid1Flip == 1 || useqGridEntry == false) && (bullbearHA2 == 1 || useHA2Entry == false) && allowLongs == true && gotoBELong == false && longEntrySet == false)
				{
					EnterLong(DefaultQuantity, "GoLong");
					gotoBEShort = false;
					if(useExitTarget == true)
					{
						SetProfitTarget("GoLong", CalculationMode.Ticks, exitTargetTicks);
						longEntrySet = true;
					}
					//SetStopLoss("GoLong",CalculationMode.Currency, SL, false);
				}
				
				if ((Moneyball1.VBar[0] < mb_lThreshold || useMoneyballEntry == false) && (grid1Flip == 2 || useqGridEntry == false) && (bullbearHA2 == 2 || useHA2Entry == false) && allowShorts == true & gotoBEShort == false && shortEntrySet == false)
				{
					EnterShort(DefaultQuantity, "GoShort");
					gotoBELong = false;
					if(useExitTarget == true)
					{
						SetProfitTarget("GoShort", CalculationMode.Ticks, exitTargetTicks);
						shortEntrySet = true;
					}
					//SetProfitTarget("GoShort", CalculationMode.Currency, TP);
					//SetStopLoss("GoShort",CalculationMode.Currency, SL, false);
				}
				
			}
			
			if ((Position.MarketPosition == MarketPosition.Long) && ((((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) <= -maxDailyLossAmount) && maxDailyLoss == true) || (((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) >= maxDailyProfitAmount) && maxDailyProfit == true))) ///If unrealized goes under maxDailyLossAmount 'OR' Above maxDailyProfitAmount
			{
				//Print((currentPnL+Position.GetProfitLoss(Close[0], PerformanceUnit.Currency)) + " - " + -maxDailyLossAmount);
				// print to the output window if the daily limit is hit in the middle of a trade
				Print("daily limit hit, exiting order " + Time[0].ToString());
				ExitLong("Daily Limit Exit", "GoLong");
				okToTrade = false;
			}
			
			if ((Position.MarketPosition == MarketPosition.Short) && ((((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) <= -maxDailyLossAmount) && maxDailyLoss == true) || (((currentPnL + Position.GetUnrealizedProfitLoss(PerformanceUnit.Currency, Close[0])) >= maxDailyProfitAmount) && maxDailyProfit == true))) ///If unrealized goes under maxDailyLossAmount 'OR' Above maxDailyProfitAmount    
				
			{
				//Print((currentPnL+Position.GetProfitLoss(Close[0], PerformanceUnit.Currency)) + " - " + -maxDailyLossAmount);
				// print to the output window if the daily limit is hit in the middle of a trade
				Print("daily limit hit, exiting order " + Time[0].ToString());
				ExitShort("Daily Limit Exit", "GoShort");
				okToTrade = false;
			}
			
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
		
		
		/*
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
				Print(" Button Press " + Time[0].ToString() + " Marketposition = " + Position.MarketPosition + " Average Price = " + Position.AveragePrice + " Close = " + Bars.GetBid(CurrentBar));
				if (Position.MarketPosition == MarketPosition.Short && Position.AveragePrice != 0 && Bars.GetAsk(CurrentBar) < Position.AveragePrice)
				{
					gotoBEShort = true;
					ExitShortStopMarket(0, true, DefaultQuantity, Position.AveragePrice, "BE Exit", "GoShort");
					//SetStopLoss(@"AlphaShort",CalculationMode.Price, Position.AveragePrice,false);
					Print("Go to BE" + Time[0].ToString());
				}
				else if(Position.MarketPosition == MarketPosition.Long && Position.AveragePrice != 0 && Bars.GetBid(CurrentBar) > Position.AveragePrice)
				{
					gotoBELong = true;
					ExitLongStopMarket(0, true, DefaultQuantity, Position.AveragePrice, "BE Exit", "GoLong");
					//SetStopLoss(@"AlphaLong",CalculationMode.Price, Position.AveragePrice, false);
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
		public CustomEnumNamespaceGridMoney.TimeMode TIMEMODESelect
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
		[Display(Name="Number of Contracts", Order=0, GroupName="3. Trade Parameters")]
		public int DQ
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Moneyball for entry?", Order=1, GroupName="3. Trade Parameters")]
		public bool useMoneyballEntry
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use HA2 for entry?", Order=2, GroupName="3. Trade Parameters")]
		public bool useHA2Entry
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use qGrid for entry?", Order=3, GroupName="3. Trade Parameters")]
		public bool useqGridEntry
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Profit Target for exit?", Order=4, GroupName="3. Trade Parameters")]
		public bool useExitTarget
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Profit Target, ticks", Order=5, GroupName="3. Trade Parameters")]
		public int exitTargetTicks
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use HA2 for exit?", Order=6, GroupName="3. Trade Parameters")]
		public bool useExitHA2
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Use Moneyball for exit?", Order=7, GroupName="3. Trade Parameters")]
		public bool useMoneyballExit
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(.001, 1.0)]
		[Display(Name="Moneyball Exit Upper Threshold", Order=8, GroupName="3. Trade Parameters")]
		public double exit_mb_uThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(-1.0, -.001)]
		[Display(Name="Moneyball Exit Lower Threshold", Order=9, GroupName="3. Trade Parameters")]
		public double exit_mb_lThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Number of bars between signals", Order=0, GroupName="4. Moneyball Parameters")]
		public int mb_Nb_bars
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Order=1, GroupName="4. Moneyball Parameters")]
		public int mb_period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="All Zero", Order=2, GroupName="4. Moneyball Parameters")]
		public bool mb_zero
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(.001, 1.0)]
		[Display(Name="Upper Threshold", Order=4, GroupName="4. Moneyball Parameters")]
		public double mb_uThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(-1.0, -.001)]
		[Display(Name="Lower Threshold", Order=5, GroupName="4. Moneyball Parameters")]
		public double mb_lThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0.001, double.MaxValue)]
		[Display(Name="Sensitivity", Order=6, GroupName="4. Moneyball Parameters")]
		public double mb_Sensitivity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="HA Smooth Period 1", Order=1, GroupName="5. Qgrid Parameters")]
		public int grid1Period1
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="OMA Length", Order=2, GroupName="5. Qgrid Parameters")]
		public int grid1omaL
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="OMA Speed", Order=3, GroupName="5. Qgrid Parameters")]
		public double grid1omaS
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Adaptive OMA", Order=4, GroupName="5. Qgrid Parameters")]
		public bool grid1omaA
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Sensitivity", Order=5, GroupName="5. Qgrid Parameters")]
		public double grid1Sensitivity
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Step Size", Order=6, GroupName="5. Qgrid Parameters")]
		public double grid1StepSize
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="HA Smooth Period 2", Order=7, GroupName="5. Qgrid Parameters")]
		public int grid1Period2
		{ get; set; }
		
		#endregion
		
	}
}

namespace CustomEnumNamespaceGridMoney
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
}
