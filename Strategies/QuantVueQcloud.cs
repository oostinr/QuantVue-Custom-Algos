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
	public class QuantVueQcloud : Strategy
	{
		private Qcloud Qcloud1;
		private	CustomEnumNamespaceCloud.TimeMode		TimeModeSelect		= CustomEnumNamespaceCloud.TimeMode.Restricted;
		private DateTime 					startTime 		= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime		 			endTime 		= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "QuantVueQcloud";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 5;
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
				RealtimeErrorHandling						= RealtimeErrorHandling.IgnoreAllErrors;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				TP = 100;
				SL = 40;
				DQ = 2;
			}
			else if (State == State.Configure)
			{
				DefaultQuantity = DQ;
			}
			else if (State == State.DataLoaded)
			{				
				Qcloud1 = Qcloud(Close, Brushes.Red, Brushes.Green, 19, 29, 49, 59, 69, 99, false);
				AddChartIndicator(Qcloud(Close, Brushes.Red, Brushes.Green, 19, 29, 49, 59, 69, 99, false));
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
			
			if ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || TimeModeSelect == CustomEnumNamespaceCloud.TimeMode.Unrestricted)
			{
				// Set 1
				if (CrossAbove(Qcloud1.V1, Qcloud1.V6, 1))
				{
					EnterLong(Convert.ToInt32(DefaultQuantity), "GoLong");
					SetStopLoss("GoLong", CalculationMode.Currency, SL, false);
					SetProfitTarget("GoLong", CalculationMode.Currency, TP);
				}
			
			 	// Set 2
				if (CrossBelow(Qcloud1.V1, Qcloud1.V6, 1))
				{
					EnterShort(Convert.ToInt32(DefaultQuantity), "GoShort");
					SetStopLoss("GoShort", CalculationMode.Currency, SL, false);
					SetProfitTarget("GoShort", CalculationMode.Currency, TP);
				}
			}
			
		}
		

		
		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="TP", Order=1, GroupName="Parameters")]
		public int TP
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SL", Order=2, GroupName="Parameters")]
		public int SL
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="DQ", Order=3, GroupName="Parameters")]
		public int DQ
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Trading Hour Restriction", GroupName = "Time Parameters", Order = 0)]
		public CustomEnumNamespaceCloud.TimeMode TIMEMODESelect
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
		#endregion
	}
}

namespace CustomEnumNamespaceCloud
{
	public enum TimeMode
	{
		Restricted,
		Unrestricted
	}
}
