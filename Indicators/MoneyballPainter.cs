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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class MoneyballPainter : Indicator
	{
		private Moneyball Moneyball1;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "MoneyballPainter";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				mb_Nb_bars = 15;
				mb_period = 10;
				mb_zero = true;
				mb_uThreshold = 0.35;
				mb_lThreshold = -0.35;
				mb_Sensitivity = 0.1;
			}
			else if (State == State.Configure)
			{
				

			}
			else if (State == State.DataLoaded)
			{                                
				Moneyball1 = Moneyball(Close, Brushes.RoyalBlue, Brushes.Blue, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity, MoneyballMode.M, false);       
			}
		}

		protected override void OnBarUpdate()
		{
			Brush mbUpBrush = new SolidColorBrush(Colors.RoyalBlue);
			mbUpBrush.Opacity = 0.25;
			mbUpBrush.Freeze();
			
			Brush mbDownBrush = new SolidColorBrush(Colors.Blue);
			mbDownBrush.Opacity = 0.25;
			mbDownBrush.Freeze();
			
			
			BackBrush = null;
			if (Moneyball1.VBar[0] > mb_uThreshold)
			{
				BackBrush = mbUpBrush;
			}
			else if (Moneyball1.VBar[0] < mb_lThreshold)
			{
				BackBrush = mbDownBrush;
			}
			else
			{
				BackBrush = null;
			}
			
			
		}
		
		#region Properties
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

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private MoneyballPainter[] cacheMoneyballPainter;
		public MoneyballPainter MoneyballPainter(int mb_Nb_bars, int mb_period, bool mb_zero, double mb_uThreshold, double mb_lThreshold, double mb_Sensitivity)
		{
			return MoneyballPainter(Input, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity);
		}

		public MoneyballPainter MoneyballPainter(ISeries<double> input, int mb_Nb_bars, int mb_period, bool mb_zero, double mb_uThreshold, double mb_lThreshold, double mb_Sensitivity)
		{
			if (cacheMoneyballPainter != null)
				for (int idx = 0; idx < cacheMoneyballPainter.Length; idx++)
					if (cacheMoneyballPainter[idx] != null && cacheMoneyballPainter[idx].mb_Nb_bars == mb_Nb_bars && cacheMoneyballPainter[idx].mb_period == mb_period && cacheMoneyballPainter[idx].mb_zero == mb_zero && cacheMoneyballPainter[idx].mb_uThreshold == mb_uThreshold && cacheMoneyballPainter[idx].mb_lThreshold == mb_lThreshold && cacheMoneyballPainter[idx].mb_Sensitivity == mb_Sensitivity && cacheMoneyballPainter[idx].EqualsInput(input))
						return cacheMoneyballPainter[idx];
			return CacheIndicator<MoneyballPainter>(new MoneyballPainter(){ mb_Nb_bars = mb_Nb_bars, mb_period = mb_period, mb_zero = mb_zero, mb_uThreshold = mb_uThreshold, mb_lThreshold = mb_lThreshold, mb_Sensitivity = mb_Sensitivity }, input, ref cacheMoneyballPainter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MoneyballPainter MoneyballPainter(int mb_Nb_bars, int mb_period, bool mb_zero, double mb_uThreshold, double mb_lThreshold, double mb_Sensitivity)
		{
			return indicator.MoneyballPainter(Input, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity);
		}

		public Indicators.MoneyballPainter MoneyballPainter(ISeries<double> input , int mb_Nb_bars, int mb_period, bool mb_zero, double mb_uThreshold, double mb_lThreshold, double mb_Sensitivity)
		{
			return indicator.MoneyballPainter(input, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MoneyballPainter MoneyballPainter(int mb_Nb_bars, int mb_period, bool mb_zero, double mb_uThreshold, double mb_lThreshold, double mb_Sensitivity)
		{
			return indicator.MoneyballPainter(Input, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity);
		}

		public Indicators.MoneyballPainter MoneyballPainter(ISeries<double> input , int mb_Nb_bars, int mb_period, bool mb_zero, double mb_uThreshold, double mb_lThreshold, double mb_Sensitivity)
		{
			return indicator.MoneyballPainter(input, mb_Nb_bars, mb_period, mb_zero, mb_uThreshold, mb_lThreshold, mb_Sensitivity);
		}
	}
}

#endregion
