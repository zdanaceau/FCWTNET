﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FCWTNET;
using OxyPlot;

namespace FCWTNET
{
    /// <summary>
    /// Class to encapsulate the CWT and all relevant parameters
    /// Currently includes:
    /// CWT Calculation
    /// Separation of real and imaginary components of the CWT
    /// Calculation of the modulus of the CWT
    /// Calculation of the phase of the CWT
    /// </summary>
    public class CWTObject
    {
        public double[] InputData { get; }
        public int Psoctave { get; }
        public int Pendoctave { get; }
        public int Pnbvoice { get; }
        public float C0 { get; }
        public int Nthreads { get; }
        public bool Use_Optimization_Schemes { get; }
        public int? SamplingRate { get; }

        public CWTOutput? OutputCWT { get; private set; }
        public CWTFrequencies? FrequencyAxis { get; private set; }
        public double[]? TimeAxis { get; private set; }
        public string? WorkingPath { get; }

        public CWTObject(double[] inputData, int psoctave, int pendoctave, int pnbvoice, float c0, int nthreads, bool use_optimization_schemes, int? samplingRate = null, string? workingPath = null)
        {
            InputData = inputData;
            Psoctave = psoctave;
            Pendoctave = pendoctave;
            Pnbvoice = pnbvoice;
            C0 = c0;
            Nthreads = nthreads;
            Use_Optimization_Schemes = use_optimization_schemes;
            SamplingRate = samplingRate;
            OutputCWT = null;
            FrequencyAxis = null;
            TimeAxis = null;
            WorkingPath = workingPath;
        }
        /// <summary>
        /// Function to perform the calculation of the CWT and return it as a double[,] in the CWTObject class called OutputCWT
        /// Inverts the original CWT output
        /// </summary>
        public void PerformCWT()
        {
            FCWTAPI.CWT(InputData, Psoctave, Pendoctave, Pnbvoice, C0, Nthreads, Use_Optimization_Schemes, 
                out double[][] real, out double[][] imag);
            OutputCWT = new CWTOutput(real, imag); 
        }
        
        /// <summary>
        /// Method to calculate the modulus of the CWT
        /// </summary>
        /// <returns name="outputArray">double[,] containing the result of the calculation</returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public double[,] ModulusCalculation()
        {
            if (OutputCWT == null)
            {
                throw new ArgumentNullException("CWT must be performed before operating on it");
            }
            int rows = OutputCWT.RealArray.GetLength(0);
            int cols = OutputCWT.ImagArray.GetLength(1); 
            double[,] output = new double[rows, cols]; 
            for(int i = 0; i < rows; i++)
            {
                for(int j = 0; j < cols; j++)
                {
                    output[i, j] = Math.Sqrt(Math.Pow(OutputCWT.RealArray[i, j], 2) + Math.Pow(OutputCWT.ImagArray[i, j], 2)); 
                }
            }
            return output;
        }
        /// <summary>
        /// Method to calculate the phase of the CWT
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public double[,] PhaseCalculation()
        {
            if (OutputCWT == null)
            {
                throw new ArgumentNullException("CWT must be performed before performing an operation on it");
            }
            int rows = OutputCWT.RealArray.GetLength(0);
            int cols = OutputCWT.RealArray.GetLength(1);

            double[,] output = new double[rows, cols]; 
            for(int i = 0; i < rows; i++)
            {
                for(int j = 0; j < cols; j++)
                {
                    double realImRatio = OutputCWT.RealArray[i,j] / OutputCWT.ImagArray[i,j];
                    output[i, j] = Math.Atan(realImRatio); 
                }
            }
            return output; 
        }
        public enum CWTComponent
        {
            Real,
            Imaginary,
            Both
        }
        /// <summary>
        /// Generates an array corresponding to the characteristic frequencies of the analyzing wavelet
        /// FrequencyAxis[0] corresponds to f for the analyzing wavelet at OutputCWT[0, ]
        /// </summary>
        public void CalculateFrequencyAxis()
        {
            int octaveNum = 1 + Pendoctave - Psoctave;
            double deltaA = 1 / Convert.ToDouble(Pnbvoice);
            double[] freqArray = new double[octaveNum * Pnbvoice];
            for (int i = 1 ; i <= octaveNum * Pnbvoice; i++)
            {
                freqArray[^i] = C0 / Math.Pow(2, (1 + (i + 1) * deltaA));
            }
            FrequencyAxis = new CWTFrequencies(freqArray, Pnbvoice, C0);            
        }
        /// <summary>
        /// Generates an array corresponding to the individual timepoints of the transient operated on by the CWT
        /// Timepoints are given in milliseconds
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void CalculateTimeAxis()
        {
            if (SamplingRate == null)
            {
                throw new ArgumentNullException("SamplingRate", "SamplingRate must be provided to calculate a time axis");
            }
            if (SamplingRate <= 0)
            {
                throw new ArgumentException("SamplingRate", "SamplingRate must be a positive, non-zero integer");

            }
            if (OutputCWT == null)
            {
                throw new ArgumentNullException("OutputCWT", "Output CWT must be calculated prior to calculating a time axis for it");
            }
            double [] timeArray = new double[OutputCWT.RealArray.GetLength(1)];
            double timeStep = 1 / (double)SamplingRate;
            double currentTime = 0;
            for (int i = 0; i < OutputCWT.RealArray.GetLength(1); i++)
            {
                timeArray[i] = currentTime;
                currentTime += timeStep;
            }
            TimeAxis = timeArray;

        }
        public enum CWTFeatures
        {
            Imaginary,
            Real,
            Modulus,
            Phase
        }
        /// <summary>
        /// Generates a heatmap of a paticular CWT Feature
        /// </summary>
        /// <param name="cwtFeature">Enumerable to select whether the Modulus, Phase, Real or Imaginary component of CWT should be plotted</param>
        /// <param name="fileName">File to write the resulting plot to</param>
        /// <param name="dataName">Optional name of the data to pass into the title of the plot</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void GenerateHeatMap(CWTFeatures cwtFeature, string fileName, string? dataName = null)
        {
            if (TimeAxis == null)
            {
                throw new ArgumentNullException(nameof(TimeAxis), "TimeAxis cannot be null");

            }
            if (OutputCWT == null)
            {
                throw new ArgumentNullException(nameof(OutputCWT), "OutputCWT cannot be null");
            }
            if (FrequencyAxis == null)
            {
                throw new ArgumentNullException(nameof(FrequencyAxis), "FrequencyAxis cannot be null");
            }
            if (Path.GetExtension(fileName) != ".pdf")
            {
                throw new ArgumentException(nameof(fileName), "fileName must have the .pdf extension");
            }
            double[,] data;
            if (cwtFeature == CWTFeatures.Imaginary)
            {
                data = GetComponent(CWTComponent.Imaginary, OutputCWT);
            }
            else if (cwtFeature == CWTFeatures.Real)
            {
                data = GetComponent(CWTComponent.Real, OutputCWT);
            }
            else if (cwtFeature == CWTFeatures.Modulus)
            {
                data = ModulusCalculation();
            }
            else
            {
                data = PhaseCalculation();
            }
            string title;
            if (cwtFeature == CWTFeatures.Imaginary || cwtFeature == CWTFeatures.Real)
            {
                title = cwtFeature.ToString() + "Component Plot";
            }
            else
            {
                title = cwtFeature.ToString() + "Plot";
            }
            if (dataName != null)
            {
                title = dataName + title;
            }
            // Reflects data about the xy axis to plot CWT data with Freqeuncy in the y-axis and time in the x-axis
            double[,] xyReflectedData = new double[data.GetLength(1), data.GetLength(0)];
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    xyReflectedData[j, i] = data[i, j];
                }
            }
            PlotModel cwtPlot = PlottingUtils.GenerateCWTHeatMap(xyReflectedData, title, TimeAxis, FrequencyAxis.WaveletCenterFrequencies);
            string filePath = Path.Combine(WorkingPath, fileName);
            PlottingUtils.ExportPlotPDF(cwtPlot, filePath);
        }

        /// <summary>
        /// Method to generate different 2D XY Plots from the CWT along frequency bands
        /// Allows for the generation of composite, evolution and single frequency band plots
        /// </summary>
        /// <param name="cwtFeature">Feature of the CWT to be plotted</param>
        /// <param name="fileName">File to write the plot to</param>
        /// <param name="plotMode">Specifies the type of plot to generate</param>
        /// <param name="startFrequency">Starting frequency to sample for the plot</param>
        /// <param name="endFrequency">End frequency to sample for the plot</param>
        /// <param name="sampleNumber">Number of frequencies to sample</param>
        /// <param name="dataName">Name of the data to enter</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public void GenerateXYPlot(CWTFeatures cwtFeature, string fileName, PlottingUtils.XYPlotOptions plotMode, double startFrequency, double? endFrequency = null, int? sampleNumber = null, string? dataName = null)
        {
            if (TimeAxis == null)
            {
                throw new ArgumentNullException(nameof(TimeAxis), "TimeAxis cannot be null");

            }
            if (OutputCWT == null)
            {
                throw new ArgumentNullException(nameof(OutputCWT), "OutputCWT cannot be null");
            }
            if (FrequencyAxis == null)
            {
                throw new ArgumentNullException(nameof(FrequencyAxis), "FrequencyAxis cannot be null");
            }
            if (Path.GetExtension(fileName) != ".pdf")
            {
                throw new ArgumentException(nameof(fileName), "fileName must have the .pdf extension");
            }
            double[,] data;
            if (cwtFeature == CWTFeatures.Imaginary)
            {
                data = GetComponent(CWTComponent.Imaginary, OutputCWT);
            }
            else if (cwtFeature == CWTFeatures.Real)
            {
                data = GetComponent(CWTComponent.Real, OutputCWT);
            }
            else if (cwtFeature == CWTFeatures.Modulus)
            {
                data = ModulusCalculation();
            }
            else
            {
                data = PhaseCalculation();
            }
            string title;
            if (cwtFeature == CWTFeatures.Imaginary || cwtFeature == CWTFeatures.Real)
            {
                title = cwtFeature.ToString() + "Component " + plotMode.ToString() + " Plot";
            }
            else
            {
                title = cwtFeature.ToString() + " " + plotMode.ToString() + " Plot";
            }
            if (dataName != null)
            {
                title = dataName + title;
            }

            int[] indFrequencies;
            if (plotMode == PlottingUtils.XYPlotOptions.Evolution || plotMode == PlottingUtils.XYPlotOptions.Composite)
            {

                if(endFrequency != null && sampleNumber != null)
                {
                    (int, int) freqIndices = GetIndicesForFrequencyRange((double)startFrequency, (double)endFrequency);
                    int maxFrequencies = freqIndices.Item2 - freqIndices.Item1;                    
                    if (sampleNumber < (maxFrequencies))
                    {
                        indFrequencies = new int[(int)sampleNumber];
                        double virtualLocation = 0;
                        double stepSize = ((double)(freqIndices.Item2 - freqIndices.Item1) - 1) / ((double)sampleNumber - 1);
                        for (int i = 0; i < sampleNumber; i++)
                        {
                            if (i < sampleNumber - 1)
                            {
                                indFrequencies[i] = freqIndices.Item1 + Convert.ToInt32(Math.Floor(virtualLocation));
                            }
                            else
                            {
                                indFrequencies[i] = freqIndices.Item2;
                            }
                            virtualLocation += stepSize;
                        }
                    }
                    else
                    {
                        indFrequencies = new int[maxFrequencies];
                        for (int i = 0; i < maxFrequencies; i++)
                        {
                            indFrequencies[i] = freqIndices.Item1 + i;
                        }
                    }
                }
                else
                {
                    throw new ArgumentNullException("Neither startFreqeuncy nor endFrequency may be null");
                }
                
            }
            else
            {
                int rawFrequencyIndex = Array.BinarySearch(FrequencyAxis.WaveletCenterFrequencies, startFrequency);
                int frequencyIndex = rawFrequencyIndex < 0 ? -rawFrequencyIndex + 1 : rawFrequencyIndex;
                indFrequencies = new int[] {frequencyIndex};
            }
            double[,] xyReflectedData = new double[data.GetLength(1), data.GetLength(0)];
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    xyReflectedData[j, i] = data[i, j];
                }
            }
            PlotModel cwtPlot = PlottingUtils.GenerateXYPlotCWT(xyReflectedData, indFrequencies, TimeAxis, FrequencyAxis.WaveletCenterFrequencies, PlottingUtils.PlotTitles.Custom, plotMode, title);

            string filePath = Path.Combine(WorkingPath, fileName);
            PlottingUtils.ExportPlotPDF(cwtPlot, filePath);

        }
        // This is a temporary method, I want to get all of the plotting stuff integrated first, and then I will work on moving this to CWTFrequencies.
        public (int, int) GetIndicesForFrequencyRange(double startFrequency, double endFrequency)
        {
            
            if (FrequencyAxis == null)
            {
                throw new ArgumentNullException(nameof(FrequencyAxis), "FrequencyAxis cannot be null");
            }
            if (FrequencyAxis.WaveletCenterFrequencies[0] > startFrequency)
            {
                throw new ArgumentException(nameof(startFrequency), "startFrequency cannot be less than the minimum frequency");
            }
            if (startFrequency >= endFrequency)
            {
                throw new ArgumentException(nameof(endFrequency), "endFrequency must be greater than startFrequency");
            }
            if (FrequencyAxis.WaveletCenterFrequencies[^1] < endFrequency)
            {
                throw new ArgumentException(nameof(endFrequency), "endFrequency must not be greater than the maximum CWT frequency");
            }
            if(FrequencyAxis.WaveletCenterFrequencies[^2] <= startFrequency)
            {
                return (FrequencyAxis.WaveletCenterFrequencies.Length - 2, FrequencyAxis.WaveletCenterFrequencies.Length - 1);
            }
            else
            {
                int rawStartIndex = Array.BinarySearch(FrequencyAxis.WaveletCenterFrequencies, startFrequency);
                double axisStartFrequency;
                int axisStartIndex;
                int positiveStartIndex;
                if (rawStartIndex < 0)
                {
                    positiveStartIndex = rawStartIndex * -1 - 1;
                }
                else
                {
                    positiveStartIndex = rawStartIndex;
                }

                if (FrequencyAxis.WaveletCenterFrequencies[positiveStartIndex] > startFrequency)
                {
                    axisStartIndex = positiveStartIndex - 1;
                }
                else
                {
                    axisStartIndex = positiveStartIndex;
                }
                axisStartFrequency = FrequencyAxis.WaveletCenterFrequencies[positiveStartIndex];
                double deltaA = 1 / Convert.ToDouble(Pnbvoice);
                int numFreqs = Convert.ToInt32(Math.Ceiling(Math.Log2(endFrequency / axisStartFrequency) / deltaA));
                int axisEndIndex = axisStartIndex + numFreqs;
                return (axisStartIndex, axisEndIndex);
                
            }
            
        }           
    }
}
