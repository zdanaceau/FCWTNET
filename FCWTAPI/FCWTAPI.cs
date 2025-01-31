﻿using System.Runtime.InteropServices; 
namespace FCWTNET
{
    public class FCWTAPI
    {
        // TODO: If the size of the nvoices * noctaves * 2 is greater than max integer, split the 
        // calculation into multiple chunks. 
        [DllImport("fCWT.dll", EntryPoint = "?cwt@fcwt@@YAXPEAMH0HHHMH_N@Z")]
        private static extern void _cwt([In] float[] input, int inputsize, [In, Out] float[] output,
            int pstoctave, int pendoctave, int pnbvoice, float c0, int nthreads, bool use_optimization_schemes);

        public static void CWT(float[] input, int psoctave, int pendoctave,
            int pnbvoice, float c0, int nthreads, bool use_optimization_schemes, 
            out double[][] real, out double[][] imag)
        {
            int inputSize = input.Length;
            int noctaves = pendoctave - psoctave + 1;
            float[] output = GenerateOutputArray(inputSize, noctaves, pnbvoice);
            _cwt(input, inputSize, output, psoctave, pendoctave, pnbvoice, c0, nthreads, use_optimization_schemes);
            SplitCWTOutput(output, inputSize, out double[][] realArray, out double[][] imagArray);
            real = realArray; imag = imagArray; 
        }
        public static float[] CWT_Base(float[] input, int psoctave, int pendoctave, 
            int pnbvoice, float c0, int nthreads, bool use_optimization_schemes)
        {
            float[] output = GenerateOutputArray(input.Length, pendoctave - psoctave + 1, pnbvoice);
            _cwt(input, input.Length, output, psoctave, pendoctave, pnbvoice, c0, nthreads, use_optimization_schemes);
            return output; 
        }
        public static void SplitCWTOutput(float[] output, int signalLength, 
            out double[][] realArray, out double[][] imagArray)
        {
            double[] real1D = new double[output.Length / 2]; 
            double[] imag1D = new double[output.Length / 2];

            // convert the float to double
            double[] outputd = output.Select(i => (double)i).ToArray();

            int j = 0;
            int k = 0;

            for (int i = 0; i < outputd.Length; i++)
            {
                if (i % 2 == 0)
                {
                    real1D[j] = outputd[i];
                    j++;
                }
                else
                {
                    imag1D[k] = outputd[i];
                    k++;
                }
                if (k >= imag1D.Length)
                {
                    break;
                }
            }
            int rowsJagged = real1D.Length / signalLength;
            int offset = signalLength;
            int bytesToCopy = offset * sizeof(double);
            realArray = new double[rowsJagged][];
            imagArray = new double[rowsJagged][]; 

            for(int i = 0; i < rowsJagged; i++)
            {
                realArray[i] = new double[offset];
                imagArray[i] = new double[offset];

                Buffer.BlockCopy(real1D, offset * i * sizeof(double), realArray[i], 0, bytesToCopy);
                Buffer.BlockCopy(imag1D, offset * i * sizeof(double), imagArray[i], 0, bytesToCopy);
            }
        }
        /// <summary>
        /// Performs a fast continous wavelet transform with a morlet wavelet. 
        /// </summary>
        /// <param name="input"></param><summary>Input signal to be transformed.</summary>
        /// <param name="psoctave"></param><summary>Start octave (power of two) to calculate.</summary>
        /// <param name="pendoctave"></param><summary>Final octave. </summary>
        /// <param name="pnbvoice"></param><summary>Number of voices per octave.</summary>
        /// <param name="c0"></param><summary>Central frequency of the morlet wavelet.</summary>
        /// <param name="nthreads"></param><summary>Number of threads to use. </summary>
        /// <param name="use_optimization_schemes"></param><summary>fCWT optimization scheme to use.</summary>
        /// <returns></returns>
        public static void CWT(double[] input, int psoctave, int pendoctave,
            int pnbvoice, float c0, int nthreads, bool use_optimization_schemes,
            out double[][] real, out double[][] imag)
        {
            float[] fInput = ConvertDoubleToFloat(input);
            CWT(fInput, psoctave, pendoctave, pnbvoice, c0, nthreads, use_optimization_schemes, 
                out double[][] realTemp, out double[][] imagTemp);
            real = realTemp; imag = imagTemp; 
        }
        public static float[] ConvertDoubleToFloat(double[] dArray)
        {
            float[] fArray = new float[dArray.Length];
            for (int i = 0; i < dArray.Length; i++)
            {
                fArray[i] = ToFloat(dArray[i]);
            }
            return fArray;
        }
        private static float ToFloat(double value)
        {
            return (float)value;
        }
        private static float[] GenerateOutputArray(int size, int noctave, int nvoice)
        {
            return new float[size * noctave * nvoice * 2];
        }
        /// <summary>
        /// Moves the 1D array to a 2D jagged array. 
        /// </summary>
        /// <param name="array1D"></param>
        /// <param name="size"></param>
        /// <param name="noctave"></param>
        /// <param name="nvoice"></param>
        /// <returns></returns>
        public static float[][] FixOutputArray(float[] array1D, int size, int noctave, int nvoice)
        {
            // From the original fCWT library code 
            int numberCols = noctave * nvoice * 2;
            int numberRows = size; 
            // Creates the final with freq as higher dim
            float[][] fixedResults = new float[numberCols][];
            for(int i = 0; i < numberCols; i += 2)
            {
                float[] imagtemp = new float[size];
                float[] realtemp = new float[size]; 
                for (int j = 0; j < numberRows; j++)
                {
                    if(j % 2 == 0)
                    {
                        realtemp[j] = array1D[j];
                    }
                    else
                    {
                        imagtemp[j] = array1D[j]; 
                    }
                }
                fixedResults[i] = realtemp;
                fixedResults[i + 1] = imagtemp; 
            }
            return fixedResults;
        }

        //First element corresponds to the first jagged array dimension (voices), second element corresponds to the second dim (timepoints)

        /// <summary>
        /// Converts jagged 2D arrays to formal 2D arrays
        /// The first dimension of the resulting 2D array corresponds to the voices
        /// The second dimension corresponds to the time points of the input signal
        /// </summary>
        /// <param name="jaggedTwoD">2D jagged array to be converted</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public static float[,] ToTwoDArray(float[][] jaggedTwoD)
        {

            int arrayCount = jaggedTwoD.Length;
            int arrayLength = jaggedTwoD[0].Length;
            float[,] twodOutput = new float[arrayCount, arrayLength];
            for (int i = 0; i < arrayCount; i++)
            {
                if(i > 0)
                {
                    if (jaggedTwoD[i].Length != jaggedTwoD[i - 1].Length)
                    {
                        arrayLength = jaggedTwoD[i].Length + 1;
                    }
                }
                try
                {
                    for (int j = 0; j < arrayLength; j++)
                    {
                        twodOutput[i, j] = jaggedTwoD[i][j];
                    }
                }
                catch(IndexOutOfRangeException)
                {
                    string rowError = String.Format("Invalid array length in row {0}", i);
                    throw new IndexOutOfRangeException(rowError);
                }
                
            }
            return twodOutput;
        }
        public static double[,] ToTwoDArray(double[][] jaggedTwoD)
        {
            int arrayCount = jaggedTwoD.Length;
            int arrayLength = jaggedTwoD[0].Length;
            double[,] twodOutput = new double[arrayCount, arrayLength];
            for (int i = 0; i < arrayCount; i++)
            {
                if (i > 0)
                {
                    if (jaggedTwoD[i].Length != jaggedTwoD[i - 1].Length)
                    {
                        arrayLength = jaggedTwoD[i].Length + 1;
                    }
                }
                try
                {
                    for (int j = 0; j < arrayLength; j++)
                    {
                        twodOutput[i, j] = jaggedTwoD[i][j];
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    string rowError = String.Format("Invalid array length in row {0}", i);
                    throw new IndexOutOfRangeException(rowError);
                }

            }
            return twodOutput;
        }

        /// <summary>
        /// Used to split the imaginary and the real arrays when using a complex wavelet, i.e. the morlet. 
        /// </summary>
        /// <param name="combinedArray"></param>
        /// <param name="realArray"></param>
        /// <param name="imaginaryArray"></param>
        public static void SplitIntoRealAndImaginary(float[][] combinedArray, out float[][] realArray, out float[][] imaginaryArray)
        {
            // every other row is the opposite
            int rowNumber = combinedArray.GetLength(0);
            int colNumber = combinedArray[0].Length;
            // row number will always be even when using a complex wavelet

            realArray = new float[colNumber / 2][];
            imaginaryArray = new float[colNumber / 2][];

            int realIndexer = 0;
            int imagIndexer = 0;
            int combinedIndexer = 0;
            while (combinedIndexer < colNumber)
            {
                int colLength = combinedArray[combinedIndexer].Length;
                if (combinedIndexer % 2 == 0)
                {
                    realArray[realIndexer] = new float[colLength];
                    realArray[realIndexer] = combinedArray[combinedIndexer];
                    realIndexer++;
                }
                else
                {
                    imaginaryArray[imagIndexer] = new float[colLength];
                    imaginaryArray[imagIndexer] = combinedArray[combinedIndexer];
                    imagIndexer++;
                }
                combinedIndexer++;
            }
        }
        /// <summary>
        /// Calculates the phase of the continuous wavelet transform output using a morlet wavelet. 
        /// </summary>
        /// <param name="realArray"></param><summary>The real part of the complex morlet CWT.</summary>
        /// <param name="imagArray"></param><summary>The imaginary part of the complex morlet CWT. </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static float[][] CalculatePhase(float[][] realArray, float[][] imagArray)
        {
            int realRows = realArray.GetLength(0);
            int imagRows = imagArray.GetLength(0);  

            int realCols = realArray[0].Length;
            if(realRows != imagRows)
            {
                throw new ArgumentException("Real and imaginary arrays have unequal lengths"); 
            }

            float[][] phaseArray = new float[realRows][]; 

            for(int i = 0; i < realRows; i++)
            {
                float[] temp = new float[realCols]; 
                for(int j =0; j < realCols; j++)
                {
                    float val = imagArray[i][j] / realArray[i][j];
                    double v = (Math.Atan((double)val));
                    temp[j] = 1F / (float)v; 
                }
                phaseArray[i] = temp; 
            }
            return phaseArray; 
        }
        /// <summary>
        /// Calculates the modulus of the complex morlet continuous wavelet transform. 
        /// </summary>
        /// <param name="realArray"></param>
        /// <param name="imagArray"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static float[][] CalculateModulus(float[][] realArray, float[][] imagArray)
        {
            int realRows = realArray.GetLength(0);
            int imagRows = imagArray.GetLength(0);

            int realCols = realArray[0].Length;
            if (realRows != imagRows)
            {
                throw new ArgumentException("Real and imaginary arrays have unequal lengths");
            }

            float[][] modArray = new float[realRows][];

            for (int i = 0; i < realRows; i++)
            {
                float[] temp = new float[realCols];
                for (int j = 0; j < realCols; j++)
                {
                    float val = realArray[i][j]*realArray[i][j] + imagArray[i][j]*imagArray[i][j];
                    temp[j] = (float)Math.Sqrt((double)val); 
                }
                modArray[i] = temp; 
            }
            return modArray;
        }

        

    }
}