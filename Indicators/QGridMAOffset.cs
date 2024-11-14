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
	public class QGridMAOffset : Indicator
	{
		private iGRID_EVO iGRID_EVO1;
		private iGRID_EVO iGRID_EVO2;
		private double StepMA1;
		private int									grid1Flip;

		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "QGridMAOffset";
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
				grid1Period1 = 19;
				grid1omaL = 19;
				grid1omaS = 2.5;
				grid1omaA = true;
				grid1Sensitivity = 2;
				grid1StepSize = 50;
				grid1Period2 = 7;
				AddPlot(new Stroke(Brushes.Goldenrod), PlotStyle.Line, "StepMA Offset");
				stepMAOffsetTick = 20;
			}
			else if (State == State.Configure)
			{
				

			}
			else if (State == State.DataLoaded)
			{                                
				iGRID_EVO1 = iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2);
				///iGRID_EVO2 = iGRID_EVO(Close, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2);
                iGRID_EVO1.FlipAlertSound = NinjaTrader.Core.Globals.InstallDir + @"\sounds\Silent.wav";
				iGRID_EVO1.AddAlertSound = NinjaTrader.Core.Globals.InstallDir + @"\sounds\Silent.wav";         
			}
		}

		protected override void OnBarUpdate()
		{
			
			if (iGRID_EVO1.FlipSignal[0] == 1)
					{
						grid1Flip = 1;
					}
					else if (iGRID_EVO1.FlipSignal[0] == -1)
					{
						grid1Flip = 2;
					}
			
			if (grid1Flip == 1)
					{
						Value[0] = iGRID_EVO1.StepMA[0] + (stepMAOffsetTick * TickSize);
					}
			else if (grid1Flip == 2)
					{
						Value[0] = iGRID_EVO1.StepMA[0] - (stepMAOffsetTick * TickSize);
					}
		}
		
		#region Properties
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
		[Display(Name="StepMA Offset (Ticks)", Order=8, GroupName="Qgrid 1 Parameters")]
		public int stepMAOffsetTick
		{ get; set; }
		
		
		#endregion
	}
	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private QGridMAOffset[] cacheQGridMAOffset;
		public QGridMAOffset QGridMAOffset(int grid1Period1, int grid1omaL, double grid1omaS, bool grid1omaA, double grid1Sensitivity, double grid1StepSize, int grid1Period2, int stepMAOffsetTick)
		{
			return QGridMAOffset(Input, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2, stepMAOffsetTick);
		}

		public QGridMAOffset QGridMAOffset(ISeries<double> input, int grid1Period1, int grid1omaL, double grid1omaS, bool grid1omaA, double grid1Sensitivity, double grid1StepSize, int grid1Period2, int stepMAOffsetTick)
		{
			if (cacheQGridMAOffset != null)
				for (int idx = 0; idx < cacheQGridMAOffset.Length; idx++)
					if (cacheQGridMAOffset[idx] != null && cacheQGridMAOffset[idx].grid1Period1 == grid1Period1 && cacheQGridMAOffset[idx].grid1omaL == grid1omaL && cacheQGridMAOffset[idx].grid1omaS == grid1omaS && cacheQGridMAOffset[idx].grid1omaA == grid1omaA && cacheQGridMAOffset[idx].grid1Sensitivity == grid1Sensitivity && cacheQGridMAOffset[idx].grid1StepSize == grid1StepSize && cacheQGridMAOffset[idx].grid1Period2 == grid1Period2 && cacheQGridMAOffset[idx].stepMAOffsetTick == stepMAOffsetTick && cacheQGridMAOffset[idx].EqualsInput(input))
						return cacheQGridMAOffset[idx];
			return CacheIndicator<QGridMAOffset>(new QGridMAOffset(){ grid1Period1 = grid1Period1, grid1omaL = grid1omaL, grid1omaS = grid1omaS, grid1omaA = grid1omaA, grid1Sensitivity = grid1Sensitivity, grid1StepSize = grid1StepSize, grid1Period2 = grid1Period2, stepMAOffsetTick = stepMAOffsetTick }, input, ref cacheQGridMAOffset);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.QGridMAOffset QGridMAOffset(int grid1Period1, int grid1omaL, double grid1omaS, bool grid1omaA, double grid1Sensitivity, double grid1StepSize, int grid1Period2, int stepMAOffsetTick)
		{
			return indicator.QGridMAOffset(Input, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2, stepMAOffsetTick);
		}

		public Indicators.QGridMAOffset QGridMAOffset(ISeries<double> input , int grid1Period1, int grid1omaL, double grid1omaS, bool grid1omaA, double grid1Sensitivity, double grid1StepSize, int grid1Period2, int stepMAOffsetTick)
		{
			return indicator.QGridMAOffset(input, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2, stepMAOffsetTick);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.QGridMAOffset QGridMAOffset(int grid1Period1, int grid1omaL, double grid1omaS, bool grid1omaA, double grid1Sensitivity, double grid1StepSize, int grid1Period2, int stepMAOffsetTick)
		{
			return indicator.QGridMAOffset(Input, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2, stepMAOffsetTick);
		}

		public Indicators.QGridMAOffset QGridMAOffset(ISeries<double> input , int grid1Period1, int grid1omaL, double grid1omaS, bool grid1omaA, double grid1Sensitivity, double grid1StepSize, int grid1Period2, int stepMAOffsetTick)
		{
			return indicator.QGridMAOffset(input, grid1Period1, grid1omaL, grid1omaS, grid1omaA, grid1Sensitivity, grid1StepSize, grid1Period2, stepMAOffsetTick);
		}
	}
}

#endregion
