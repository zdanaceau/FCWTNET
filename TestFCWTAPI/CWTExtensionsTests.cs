﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using FCWTNET;

namespace TestFCWTAPI
{
    public class CWTExtensionsTests
    {
        [Test]
        public static void TestTime2DArrayCompression()
        {
            double[,] testData = new double[,]
            {
                {1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                {1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
                {1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }
            };
            double perfectDivTestPoint = (3D + 4D) / 2D;
            double rem2DivTestEdgePoint = (9D + 10D) / 2D;
            double primeMaxSizeTestPoint = (5D + 6D + 7D + 8D) / 4D;
            double[,] perfectDivision;
            double[,] rem2Division;
            double[,] emptyArray;
            CWTExtensions.Time2DArrayCompression(testData, out perfectDivision, 5);
            CWTExtensions.Time2DArrayCompression(testData, out rem2Division, 3);
            Assert.Throws<ArgumentException>(() => CWTExtensions.Time2DArrayCompression(testData, out emptyArray, -5));
            Assert.AreEqual(perfectDivTestPoint, perfectDivision[0, 1], 0.001);
            Assert.AreEqual(rem2DivTestEdgePoint, rem2Division[0, 2], 0.001);
            Assert.AreEqual(3, rem2Division.GetLength(1));
            Assert.AreEqual(5, perfectDivision.GetLength(1));
        }
        [Test]
        public static void TestTimeAxisCompression()
        {
            double[] testAxis = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            double perfectDivTestPoint = (3D + 4D) / 2D;
            double rem2DivTestEdgePoint = (9D + 10D) / 2D;
            double[] compressedPerfectDivAxis;
            double[] compressedRem2Axis;
            double[] emptyArray;
            CWTExtensions.TimeAxisCompression(testAxis, out compressedRem2Axis, 3);
            CWTExtensions.TimeAxisCompression(testAxis, out compressedPerfectDivAxis, 5);
            Assert.Throws<ArgumentException>(() => CWTExtensions.TimeAxisCompression(testAxis, out emptyArray, -5));
            Assert.AreEqual(3, compressedRem2Axis.Length);
            Assert.AreEqual(5, compressedPerfectDivAxis.Length);
            Assert.AreEqual(perfectDivTestPoint, compressedPerfectDivAxis[1]);
            Assert.AreEqual(rem2DivTestEdgePoint, compressedRem2Axis[2]);

        }
        [Test]
        public static void TestTimeWindowing()
        {
            double[,] testData = new double[,]
            {
                {1, 2, 3, 4, 5, 6},
                {7, 8, 9, 10, 11, 12},
                {13, 14, 15, 16, 17, 18}
            };
            double[] testTimeAxis = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5, 0.6 };
            double[,] windowedData;
            double[] windowedTimeAxis;
            CWTExtensions.TimeWindowing(0.2, 0.38, testTimeAxis, testData, out windowedTimeAxis, out windowedData);
            Assert.AreEqual(testData[1, 3], windowedData[1, 2]);
            Assert.AreEqual(3, windowedData.GetLength(1));
            Assert.AreEqual(3, windowedTimeAxis.Length);
            Assert.Throws<ArgumentException>(() => CWTExtensions.TimeWindowing(0.42, 0.2, testTimeAxis, testData, out windowedTimeAxis, out windowedData));
            Assert.Throws<ArgumentOutOfRangeException>(() => CWTExtensions.TimeWindowing(0.01, 0.2, testTimeAxis, testData, out windowedTimeAxis, out windowedData));
            Assert.Throws<ArgumentOutOfRangeException>(() => CWTExtensions.TimeWindowing(0.2, 0.9, testTimeAxis, testData, out windowedTimeAxis, out windowedData));
            double[] badTimeAxis = new double[] { 0.1, 0.2, 0.3, 0.4, 0.5 };
            Assert.Throws<ArgumentException>(() => CWTExtensions.TimeWindowing(0.2, 0.5, badTimeAxis, testData, out windowedTimeAxis, out windowedData));
        }
    }
}
