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
	public class QuantVueSqueeze : Strategy
	{
		private NinjaTrader.NinjaScript.Indicators.AUN_Indi.SqueezeMomentumIndicator SqueezeMomentumIndicator1;
		private DateTime 								startTime 			= DateTime.Parse("9:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime		 						endTime 			= DateTime.Parse("13:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime 								lunchstartTime 		= DateTime.Parse("11:00:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime		 						lunchendTime 		= DateTime.Parse("12:30:00", System.Globalization.CultureInfo.InvariantCulture);

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "QuantVueSqueeze";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
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
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
				TP = 300;
				SL = 200;
				DQ = 10;
			}
			else if (State == State.Configure)
			{
				DefaultQuantity = DQ;
			}
			else if (State == State.DataLoaded)
			{				
				SqueezeMomentumIndicator1 = SqueezeMomentumIndicator(Close, 20, 2, 20, 2, Brushes.ForestGreen, Brushes.Red, Brushes.Maroon, Brushes.RoyalBlue, Brushes.MintCream);
			}
		}

		protected override void OnBarUpdate()
		{
			if (BarsInProgress != 0) 
				return;

			if (CurrentBars[0] < 1)
				return;

			 // Set 1
			if (((SqueezeMomentumIndicator1.SqueezeDef[0] > SqueezeMomentumIndicator1.SqueezeDef[1])
				 && SqueezeMomentumIndicator1.SqueezeDef[0] > 0
				 && (Close[0] > Open[0])
				 && (Close[1] < Open[1]))
				 && ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || restrictTime == false)
				 && ((ToTime(Time[0]) <= ToTime(lunchstartTime) || ToTime(Time[0]) >= ToTime(lunchendTime)) || restrictLunch == false)
				)
			{
				EnterLong(DefaultQuantity, "GoLong");
				SetProfitTarget("GoLong", CalculationMode.Currency, TP);
				SetStopLoss("GoLong", CalculationMode.Currency, SL, false);
			}
			
			 // Set 2
			if (((SqueezeMomentumIndicator1.SqueezeDef[0] < SqueezeMomentumIndicator1.SqueezeDef[1])
				 && SqueezeMomentumIndicator1.SqueezeDef[0] < 0
				 && (Close[0] < Open[0])
				 && (Close[1] > Open[1]))
				 && ((ToTime(Time[0]) >= ToTime(startTime) && ToTime(Time[0]) <= ToTime(endTime)) || restrictTime == false)
				 && ((ToTime(Time[0]) <= ToTime(lunchstartTime) || ToTime(Time[0]) >= ToTime(lunchendTime)) || restrictLunch == false)
				)
			{
				EnterShort(DefaultQuantity, "GoShort");
				SetProfitTarget("GoShort", CalculationMode.Currency, TP);
				SetStopLoss("GoShort", CalculationMode.Currency, SL, false);
			}
			
		}
		
		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Restrict Trading Time?", Order=3, GroupName="1. Time Parameters")]
		public bool restrictTime
		{ get; set; }
		
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
        [NinjaScriptProperty]
        [Display(Name = "Time Range-Start", GroupName = "1. Time Parameters", Order = 1)]
        public DateTime StartTime 
		{
			get { return startTime; }
			set { startTime = value; }
		}
		
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
       	[NinjaScriptProperty]
       	[Display(Name = "Time Range-End", GroupName = "1. Time Parameters", Order = 2)]
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
		[Range(1, int.MaxValue)]
		[Display(Name="TP", Order=1, GroupName="2. Order Parameters")]
		public int TP
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="SL", Order=2, GroupName="2. Order Parameters")]
		public int SL
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="DQ", Order=3, GroupName="2. Order Parameters")]
		public int DQ
		{ get; set; }
		#endregion
	}
}

#region Wizard settings, neither change nor remove
/*@
<?xml version="1.0"?>
<ScriptProperties xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Calculate>OnBarClose</Calculate>
  <ConditionalActions>
    <ConditionalAction>
      <Actions />
      <AnyOrAll>All</AnyOrAll>
      <Conditions>
        <WizardConditionGroup>
          <AnyOrAll>Any</AnyOrAll>
          <Conditions>
            <WizardCondition>
              <LeftItem xsi:type="WizardConditionItem">
                <IsExpanded>false</IsExpanded>
                <IsSelected>true</IsSelected>
                <Name>SqueezeMomentumIndicator</Name>
                <OffsetType>Arithmetic</OffsetType>
                <AssignedCommand>
                  <Command>SqueezeMomentumIndicator</Command>
                  <Parameters>
                    <string>AssociatedIndicator</string>
                    <string>BarsAgo</string>
                    <string>OffsetBuilder</string>
                  </Parameters>
                </AssignedCommand>
                <AssociatedIndicator>
                  <AcceptableSeries>Indicator DataSeries CustomSeries DefaultSeries</AcceptableSeries>
                  <CustomProperties>
                    <item>
                      <key>
                        <string>LengthBB</string>
                      </key>
                      <value>
                        <anyType xsi:type="NumberBuilder">
                          <LiveValue xsi:type="xsd:string">20</LiveValue>
                          <BindingValue xsi:type="xsd:string">20</BindingValue>
                          <DefaultValue>0</DefaultValue>
                          <IsInt>true</IsInt>
                          <IsLiteral>true</IsLiteral>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>MultBB</string>
                      </key>
                      <value>
                        <anyType xsi:type="NumberBuilder">
                          <LiveValue xsi:type="xsd:string">2</LiveValue>
                          <BindingValue xsi:type="xsd:string">2</BindingValue>
                          <DefaultValue>0</DefaultValue>
                          <IsInt>false</IsInt>
                          <IsLiteral>true</IsLiteral>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>LengthKC</string>
                      </key>
                      <value>
                        <anyType xsi:type="NumberBuilder">
                          <LiveValue xsi:type="xsd:string">20</LiveValue>
                          <BindingValue xsi:type="xsd:string">20</BindingValue>
                          <DefaultValue>0</DefaultValue>
                          <IsInt>true</IsInt>
                          <IsLiteral>true</IsLiteral>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>MultKC</string>
                      </key>
                      <value>
                        <anyType xsi:type="NumberBuilder">
                          <LiveValue xsi:type="xsd:string">2</LiveValue>
                          <BindingValue xsi:type="xsd:string">2</BindingValue>
                          <DefaultValue>0</DefaultValue>
                          <IsInt>false</IsInt>
                          <IsLiteral>true</IsLiteral>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>BrushUpEnd</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>34</R>
                            <G>139</G>
                            <B>34</B>
                            <ScA>1</ScA>
                            <ScR>0.0159962941</ScR>
                            <ScG>0.258182883</ScG>
                            <ScB>0.0159962941</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>BrushDownBegin</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>255</R>
                            <G>0</G>
                            <B>0</B>
                            <ScA>1</ScA>
                            <ScR>1</ScR>
                            <ScG>0</ScG>
                            <ScB>0</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>BrushDownEnd</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>128</R>
                            <G>0</G>
                            <B>0</B>
                            <ScA>1</ScA>
                            <ScR>0.215860531</ScR>
                            <ScG>0</ScG>
                            <ScB>0</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>IsSqueeze</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>65</R>
                            <G>105</G>
                            <B>225</B>
                            <ScA>1</ScA>
                            <ScR>0.05286065</ScR>
                            <ScG>0.141263291</ScG>
                            <ScB>0.7529422</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>NoSqueeze</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>245</R>
                            <G>255</G>
                            <B>250</B>
                            <ScA>1</ScA>
                            <ScR>0.913098633</ScR>
                            <ScG>1</ScG>
                            <ScB>0.9559733</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                  </CustomProperties>
                  <IndicatorHolder>
                    <IndicatorName>SqueezeMomentumIndicator</IndicatorName>
                    <Plots>
                      <Plot>
                        <IsOpacityVisible>false</IsOpacityVisible>
                        <BrushSerialize>&lt;SolidColorBrush xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"&gt;#FF90EE90&lt;/SolidColorBrush&gt;</BrushSerialize>
                        <DashStyleHelper>Solid</DashStyleHelper>
                        <Opacity>100</Opacity>
                        <Width>2</Width>
                        <AutoWidth>false</AutoWidth>
                        <Max>1.7976931348623157E+308</Max>
                        <Min>-1.7976931348623157E+308</Min>
                        <Name>SqueezeDef</Name>
                        <PlotStyle>Bar</PlotStyle>
                      </Plot>
                      <Plot>
                        <IsOpacityVisible>false</IsOpacityVisible>
                        <BrushSerialize>&lt;SolidColorBrush xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"&gt;#FF808080&lt;/SolidColorBrush&gt;</BrushSerialize>
                        <DashStyleHelper>Solid</DashStyleHelper>
                        <Opacity>100</Opacity>
                        <Width>2</Width>
                        <AutoWidth>false</AutoWidth>
                        <Max>1.7976931348623157E+308</Max>
                        <Min>-1.7976931348623157E+308</Min>
                        <Name>IsSqueezes</Name>
                        <PlotStyle>Dot</PlotStyle>
                      </Plot>
                    </Plots>
                  </IndicatorHolder>
                  <IsExplicitlyNamed>false</IsExplicitlyNamed>
                  <IsPriceTypeLocked>false</IsPriceTypeLocked>
                  <PlotOnChart>false</PlotOnChart>
                  <PriceType>Close</PriceType>
                  <SeriesType>Indicator</SeriesType>
                  <SelectedPlot>SqueezeDef</SelectedPlot>
                </AssociatedIndicator>
                <BarsAgo>0</BarsAgo>
                <CurrencyType>Currency</CurrencyType>
                <Date>2024-08-11T14:49:36.5620913</Date>
                <DayOfWeek>Sunday</DayOfWeek>
                <EndBar>0</EndBar>
                <ForceSeriesIndex>false</ForceSeriesIndex>
                <LookBackPeriod>0</LookBackPeriod>
                <MarketPosition>Long</MarketPosition>
                <Period>0</Period>
                <ReturnType>Series</ReturnType>
                <StartBar>0</StartBar>
                <State>Undefined</State>
                <Time>0001-01-01T00:00:00</Time>
              </LeftItem>
              <Lookback>1</Lookback>
              <Operator>Greater</Operator>
              <RightItem xsi:type="WizardConditionItem">
                <IsExpanded>false</IsExpanded>
                <IsSelected>true</IsSelected>
                <Name>SqueezeMomentumIndicator</Name>
                <OffsetType>Arithmetic</OffsetType>
                <AssignedCommand>
                  <Command>SqueezeMomentumIndicator</Command>
                  <Parameters>
                    <string>AssociatedIndicator</string>
                    <string>BarsAgo</string>
                    <string>OffsetBuilder</string>
                  </Parameters>
                </AssignedCommand>
                <AssociatedIndicator>
                  <AcceptableSeries>Indicator DataSeries CustomSeries DefaultSeries</AcceptableSeries>
                  <CustomProperties>
                    <item>
                      <key>
                        <string>LengthBB</string>
                      </key>
                      <value>
                        <anyType xsi:type="NumberBuilder">
                          <LiveValue xsi:type="xsd:string">20</LiveValue>
                          <BindingValue xsi:type="xsd:string">20</BindingValue>
                          <DefaultValue>0</DefaultValue>
                          <IsInt>true</IsInt>
                          <IsLiteral>true</IsLiteral>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>MultBB</string>
                      </key>
                      <value>
                        <anyType xsi:type="NumberBuilder">
                          <LiveValue xsi:type="xsd:string">2</LiveValue>
                          <BindingValue xsi:type="xsd:string">2</BindingValue>
                          <DefaultValue>0</DefaultValue>
                          <IsInt>false</IsInt>
                          <IsLiteral>true</IsLiteral>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>LengthKC</string>
                      </key>
                      <value>
                        <anyType xsi:type="NumberBuilder">
                          <LiveValue xsi:type="xsd:string">20</LiveValue>
                          <BindingValue xsi:type="xsd:string">20</BindingValue>
                          <DefaultValue>0</DefaultValue>
                          <IsInt>true</IsInt>
                          <IsLiteral>true</IsLiteral>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>MultKC</string>
                      </key>
                      <value>
                        <anyType xsi:type="NumberBuilder">
                          <LiveValue xsi:type="xsd:string">2</LiveValue>
                          <BindingValue xsi:type="xsd:string">2</BindingValue>
                          <DefaultValue>0</DefaultValue>
                          <IsInt>false</IsInt>
                          <IsLiteral>true</IsLiteral>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>BrushUpEnd</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>34</R>
                            <G>139</G>
                            <B>34</B>
                            <ScA>1</ScA>
                            <ScR>0.0159962941</ScR>
                            <ScG>0.258182883</ScG>
                            <ScB>0.0159962941</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>BrushDownBegin</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>255</R>
                            <G>0</G>
                            <B>0</B>
                            <ScA>1</ScA>
                            <ScR>1</ScR>
                            <ScG>0</ScG>
                            <ScB>0</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>BrushDownEnd</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>128</R>
                            <G>0</G>
                            <B>0</B>
                            <ScA>1</ScA>
                            <ScR>0.215860531</ScR>
                            <ScG>0</ScG>
                            <ScB>0</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>IsSqueeze</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>65</R>
                            <G>105</G>
                            <B>225</B>
                            <ScA>1</ScA>
                            <ScR>0.05286065</ScR>
                            <ScG>0.141263291</ScG>
                            <ScB>0.7529422</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                    <item>
                      <key>
                        <string>NoSqueeze</string>
                      </key>
                      <value>
                        <anyType xsi:type="SolidColorBrush">
                          <Opacity>1</Opacity>
                          <Transform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </Transform>
                          <RelativeTransform xsi:type="MatrixTransform">
                            <Matrix>
                              <M11>1</M11>
                              <M12>0</M12>
                              <M21>0</M21>
                              <M22>1</M22>
                              <OffsetX>0</OffsetX>
                              <OffsetY>0</OffsetY>
                            </Matrix>
                          </RelativeTransform>
                          <Color>
                            <A>255</A>
                            <R>245</R>
                            <G>255</G>
                            <B>250</B>
                            <ScA>1</ScA>
                            <ScR>0.913098633</ScR>
                            <ScG>1</ScG>
                            <ScB>0.9559733</ScB>
                          </Color>
                        </anyType>
                      </value>
                    </item>
                  </CustomProperties>
                  <IndicatorHolder>
                    <IndicatorName>SqueezeMomentumIndicator</IndicatorName>
                    <Plots>
                      <Plot>
                        <IsOpacityVisible>false</IsOpacityVisible>
                        <BrushSerialize>&lt;SolidColorBrush xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"&gt;#FF90EE90&lt;/SolidColorBrush&gt;</BrushSerialize>
                        <DashStyleHelper>Solid</DashStyleHelper>
                        <Opacity>100</Opacity>
                        <Width>2</Width>
                        <AutoWidth>false</AutoWidth>
                        <Max>1.7976931348623157E+308</Max>
                        <Min>-1.7976931348623157E+308</Min>
                        <Name>SqueezeDef</Name>
                        <PlotStyle>Bar</PlotStyle>
                      </Plot>
                      <Plot>
                        <IsOpacityVisible>false</IsOpacityVisible>
                        <BrushSerialize>&lt;SolidColorBrush xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"&gt;#FF808080&lt;/SolidColorBrush&gt;</BrushSerialize>
                        <DashStyleHelper>Solid</DashStyleHelper>
                        <Opacity>100</Opacity>
                        <Width>2</Width>
                        <AutoWidth>false</AutoWidth>
                        <Max>1.7976931348623157E+308</Max>
                        <Min>-1.7976931348623157E+308</Min>
                        <Name>IsSqueezes</Name>
                        <PlotStyle>Dot</PlotStyle>
                      </Plot>
                    </Plots>
                  </IndicatorHolder>
                  <IsExplicitlyNamed>false</IsExplicitlyNamed>
                  <IsPriceTypeLocked>false</IsPriceTypeLocked>
                  <PlotOnChart>false</PlotOnChart>
                  <PriceType>Close</PriceType>
                  <SeriesType>Indicator</SeriesType>
                  <SelectedPlot>SqueezeDef</SelectedPlot>
                </AssociatedIndicator>
                <BarsAgo>1</BarsAgo>
                <CurrencyType>Currency</CurrencyType>
                <Date>2024-08-11T14:49:37.0448591</Date>
                <DayOfWeek>Sunday</DayOfWeek>
                <EndBar>0</EndBar>
                <ForceSeriesIndex>false</ForceSeriesIndex>
                <LookBackPeriod>0</LookBackPeriod>
                <MarketPosition>Long</MarketPosition>
                <Period>0</Period>
                <ReturnType>Series</ReturnType>
                <StartBar>0</StartBar>
                <State>Undefined</State>
                <Time>0001-01-01T00:00:00</Time>
              </RightItem>
            </WizardCondition>
          </Conditions>
          <IsGroup>false</IsGroup>
          <DisplayName>SqueezeMomentumIndicator(20, 2, 20, 2, Brushes.ForestGreen, Brushes.Red, Brushes.Maroon, Brushes.RoyalBlue, Brushes.MintCream).SqueezeDef[0] &gt; SqueezeMomentumIndicator(20, 2, 20, 2, Brushes.ForestGreen, Brushes.Red, Brushes.Maroon, Brushes.RoyalBlue, Brushes.MintCream).SqueezeDef[1]</DisplayName>
        </WizardConditionGroup>
        <WizardConditionGroup>
          <AnyOrAll>Any</AnyOrAll>
          <Conditions>
            <WizardCondition>
              <LeftItem xsi:type="WizardConditionItem">
                <IsExpanded>false</IsExpanded>
                <IsSelected>true</IsSelected>
                <Name>Close</Name>
                <OffsetType>Arithmetic</OffsetType>
                <AssignedCommand>
                  <Command>{0}</Command>
                  <Parameters>
                    <string>Series1</string>
                    <string>BarsAgo</string>
                    <string>OffsetBuilder</string>
                  </Parameters>
                </AssignedCommand>
                <BarsAgo>0</BarsAgo>
                <CurrencyType>Currency</CurrencyType>
                <Date>2024-08-11T14:50:39.9876142</Date>
                <DayOfWeek>Sunday</DayOfWeek>
                <EndBar>0</EndBar>
                <ForceSeriesIndex>false</ForceSeriesIndex>
                <LookBackPeriod>0</LookBackPeriod>
                <MarketPosition>Long</MarketPosition>
                <Period>0</Period>
                <ReturnType>Series</ReturnType>
                <StartBar>0</StartBar>
                <State>Undefined</State>
                <Time>0001-01-01T00:00:00</Time>
              </LeftItem>
              <Lookback>1</Lookback>
              <Operator>Greater</Operator>
              <RightItem xsi:type="WizardConditionItem">
                <IsExpanded>false</IsExpanded>
                <IsSelected>true</IsSelected>
                <Name>Open</Name>
                <OffsetType>Arithmetic</OffsetType>
                <AssignedCommand>
                  <Command>{0}</Command>
                  <Parameters>
                    <string>Series1</string>
                    <string>BarsAgo</string>
                    <string>OffsetBuilder</string>
                  </Parameters>
                </AssignedCommand>
                <BarsAgo>0</BarsAgo>
                <CurrencyType>Currency</CurrencyType>
                <Date>2024-08-11T14:50:40.492214</Date>
                <DayOfWeek>Sunday</DayOfWeek>
                <EndBar>0</EndBar>
                <ForceSeriesIndex>false</ForceSeriesIndex>
                <LookBackPeriod>0</LookBackPeriod>
                <MarketPosition>Long</MarketPosition>
                <Period>0</Period>
                <ReturnType>Series</ReturnType>
                <Series1>
                  <AcceptableSeries>DataSeries DefaultSeries</AcceptableSeries>
                  <CustomProperties />
                  <IsExplicitlyNamed>false</IsExplicitlyNamed>
                  <IsPriceTypeLocked>true</IsPriceTypeLocked>
                  <PlotOnChart>false</PlotOnChart>
                  <PriceType>Open</PriceType>
                  <SeriesType>DefaultSeries</SeriesType>
                </Series1>
                <StartBar>0</StartBar>
                <State>Undefined</State>
                <Time>0001-01-01T00:00:00</Time>
              </RightItem>
            </WizardCondition>
          </Conditions>
          <IsGroup>false</IsGroup>
          <DisplayName>Default input[0] &gt; Open[0]</DisplayName>
        </WizardConditionGroup>
        <WizardConditionGroup>
          <AnyOrAll>Any</AnyOrAll>
          <Conditions>
            <WizardCondition>
              <LeftItem xsi:type="WizardConditionItem">
                <IsExpanded>false</IsExpanded>
                <IsSelected>true</IsSelected>
                <Name>Close</Name>
                <OffsetType>Arithmetic</OffsetType>
                <AssignedCommand>
                  <Command>{0}</Command>
                  <Parameters>
                    <string>Series1</string>
                    <string>BarsAgo</string>
                    <string>OffsetBuilder</string>
                  </Parameters>
                </AssignedCommand>
                <BarsAgo>1</BarsAgo>
                <CurrencyType>Currency</CurrencyType>
                <Date>2024-08-11T14:51:05.674356</Date>
                <DayOfWeek>Sunday</DayOfWeek>
                <EndBar>0</EndBar>
                <ForceSeriesIndex>false</ForceSeriesIndex>
                <LookBackPeriod>0</LookBackPeriod>
                <MarketPosition>Long</MarketPosition>
                <Period>0</Period>
                <ReturnType>Series</ReturnType>
                <StartBar>0</StartBar>
                <State>Undefined</State>
                <Time>0001-01-01T00:00:00</Time>
              </LeftItem>
              <Lookback>1</Lookback>
              <Operator>Less</Operator>
              <RightItem xsi:type="WizardConditionItem">
                <IsExpanded>false</IsExpanded>
                <IsSelected>true</IsSelected>
                <Name>Open</Name>
                <OffsetType>Arithmetic</OffsetType>
                <AssignedCommand>
                  <Command>{0}</Command>
                  <Parameters>
                    <string>Series1</string>
                    <string>BarsAgo</string>
                    <string>OffsetBuilder</string>
                  </Parameters>
                </AssignedCommand>
                <BarsAgo>1</BarsAgo>
                <CurrencyType>Currency</CurrencyType>
                <Date>2024-08-11T14:51:06.1053149</Date>
                <DayOfWeek>Sunday</DayOfWeek>
                <EndBar>0</EndBar>
                <ForceSeriesIndex>false</ForceSeriesIndex>
                <LookBackPeriod>0</LookBackPeriod>
                <MarketPosition>Long</MarketPosition>
                <Period>0</Period>
                <ReturnType>Series</ReturnType>
                <Series1>
                  <AcceptableSeries>DataSeries DefaultSeries</AcceptableSeries>
                  <CustomProperties />
                  <IsExplicitlyNamed>false</IsExplicitlyNamed>
                  <IsPriceTypeLocked>true</IsPriceTypeLocked>
                  <PlotOnChart>false</PlotOnChart>
                  <PriceType>Open</PriceType>
                  <SeriesType>DefaultSeries</SeriesType>
                </Series1>
                <StartBar>0</StartBar>
                <State>Undefined</State>
                <Time>0001-01-01T00:00:00</Time>
              </RightItem>
            </WizardCondition>
          </Conditions>
          <IsGroup>false</IsGroup>
          <DisplayName>Default input[1] &lt; Open[1]</DisplayName>
        </WizardConditionGroup>
      </Conditions>
      <SetName>Set 1</SetName>
      <SetNumber>1</SetNumber>
    </ConditionalAction>
  </ConditionalActions>
  <CustomSeries />
  <DataSeries />
  <Description>Enter the description for your new custom Strategy here.</Description>
  <DisplayInDataBox>true</DisplayInDataBox>
  <DrawHorizontalGridLines>true</DrawHorizontalGridLines>
  <DrawOnPricePanel>true</DrawOnPricePanel>
  <DrawVerticalGridLines>true</DrawVerticalGridLines>
  <EntriesPerDirection>1</EntriesPerDirection>
  <EntryHandling>AllEntries</EntryHandling>
  <ExitOnSessionClose>true</ExitOnSessionClose>
  <ExitOnSessionCloseSeconds>30</ExitOnSessionCloseSeconds>
  <FillLimitOrdersOnTouch>false</FillLimitOrdersOnTouch>
  <InputParameters />
  <IsTradingHoursBreakLineVisible>true</IsTradingHoursBreakLineVisible>
  <IsInstantiatedOnEachOptimizationIteration>true</IsInstantiatedOnEachOptimizationIteration>
  <MaximumBarsLookBack>TwoHundredFiftySix</MaximumBarsLookBack>
  <MinimumBarsRequired>20</MinimumBarsRequired>
  <OrderFillResolution>Standard</OrderFillResolution>
  <OrderFillResolutionValue>1</OrderFillResolutionValue>
  <OrderFillResolutionType>Minute</OrderFillResolutionType>
  <OverlayOnPrice>false</OverlayOnPrice>
  <PaintPriceMarkers>true</PaintPriceMarkers>
  <PlotParameters />
  <RealTimeErrorHandling>StopCancelClose</RealTimeErrorHandling>
  <ScaleJustification>Right</ScaleJustification>
  <ScriptType>Strategy</ScriptType>
  <Slippage>0</Slippage>
  <StartBehavior>WaitUntilFlat</StartBehavior>
  <StopsAndTargets />
  <StopTargetHandling>PerEntryExecution</StopTargetHandling>
  <TimeInForce>Gtc</TimeInForce>
  <TraceOrders>false</TraceOrders>
  <UseOnAddTradeEvent>false</UseOnAddTradeEvent>
  <UseOnAuthorizeAccountEvent>false</UseOnAuthorizeAccountEvent>
  <UseAccountItemUpdate>false</UseAccountItemUpdate>
  <UseOnCalculatePerformanceValuesEvent>true</UseOnCalculatePerformanceValuesEvent>
  <UseOnConnectionEvent>false</UseOnConnectionEvent>
  <UseOnDataPointEvent>true</UseOnDataPointEvent>
  <UseOnFundamentalDataEvent>false</UseOnFundamentalDataEvent>
  <UseOnExecutionEvent>false</UseOnExecutionEvent>
  <UseOnMouseDown>true</UseOnMouseDown>
  <UseOnMouseMove>true</UseOnMouseMove>
  <UseOnMouseUp>true</UseOnMouseUp>
  <UseOnMarketDataEvent>false</UseOnMarketDataEvent>
  <UseOnMarketDepthEvent>false</UseOnMarketDepthEvent>
  <UseOnMergePerformanceMetricEvent>false</UseOnMergePerformanceMetricEvent>
  <UseOnNextDataPointEvent>true</UseOnNextDataPointEvent>
  <UseOnNextInstrumentEvent>true</UseOnNextInstrumentEvent>
  <UseOnOptimizeEvent>true</UseOnOptimizeEvent>
  <UseOnOrderUpdateEvent>false</UseOnOrderUpdateEvent>
  <UseOnPositionUpdateEvent>false</UseOnPositionUpdateEvent>
  <UseOnRenderEvent>true</UseOnRenderEvent>
  <UseOnRestoreValuesEvent>false</UseOnRestoreValuesEvent>
  <UseOnShareEvent>true</UseOnShareEvent>
  <UseOnWindowCreatedEvent>false</UseOnWindowCreatedEvent>
  <UseOnWindowDestroyedEvent>false</UseOnWindowDestroyedEvent>
  <Variables />
  <Name>QuantVueSqueeze</Name>
</ScriptProperties>
@*/
#endregion
