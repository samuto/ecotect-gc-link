/***************************************************************************************
 *   Copyright: (c) 2010 Bentley Systems, Incorporated.
 *   Author: Kaustuv DeBiswas
 ***************************************************************************************
 *  
 *  This file is part of ecotect-gc-link.
 *  
 *  ecotect-gc-link is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 2 of the License, or
 *  (at your option) any later version.
 *  
 *  ecotect-gc-link is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License
 *  along with ecotect-gc-link.  If not, see <http://www.gnu.org/licenses/>
 *  
 ***************************************************************************************/

/*
 * EcotectCalcSolar class
 * Version: ecotect-gc-link 1.0
 * Author: Kaustuv DeBiswas
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bentley.Interop.MicroStationDGN;
using System.Collections.Specialized;
using Bentley.GenerativeComponents.GeneralPurpose;
using Bentley.GenerativeComponents;
using Bentley.GenerativeComponents.MicroStation;  // added to make GeometryTools work
using Bentley.Geometry;
using Bentley.GenerativeComponents.GCScript;
using Bentley.GenerativeComponents.Features;
using Bentley.GenerativeComponents.Features.Specific;
using System.Threading;
using System.IO;

namespace Bentley.GenerativeComponents.Features
{

    /// <summary>Perfomance Analysis via Ecotect</summary>
    public class EcotectCalcSolar : Feature
    {
        #region field

        //private static EcotectPolygonObject mEcoPolyObject;
        private static EcotectProject mEcoProject;
        private static DPlane3d mInfoDisplayPlane;
        private static EcotectAccumulationType mCalculationType;
        private static int[] mCalcDays = new int[]{0,0};
        private static double[] mCalcTime = new double[]{0,0};
        private static int mPrecision;
        private static EcotectRadiationAttribute mAttribIndex;
        private static bool mInDynamics;
        private static bool mWriteToFile;
        private static string mFileName;
        private static double mDisplayScale;
        private static List<Element> mDisplayableElements = new List<Element>();

        private static string mAttribName;
        private static string mAttribUnits;
        private static double mAttribMax;
        private static double mAttribMin;

        private static double[] mValueList;
        private static double[][] mValueGrid;

        private char[] delimiterChars = { '\0', ',', '\"' };
        private string[] words;
        //private double dispScale = 100;
        private bool firstUse = true;
        private static bool mGCRedisplay = false;
        private static bool mGCWriteToFile = false;

        #endregion field

        #region constructors

        public EcotectCalcSolar
        (
        )
            : base()
        {
        }

        public EcotectCalcSolar
        (
        Feature parentFeature
        )
            : base(parentFeature)
        {
        }

        #endregion constructors

        #region update method by polygons

        [Update]
        public virtual bool IncidentSolarRadiationByPolygons
        (
        FeatureUpdateContext updateContext,
        [ParentModel, Replicatable]EcotectPolygonObject EcoObject,
        EcotectProject EcoProject,
        Plane InfoDisplayPlane,
        EcotectAccumulationType CalculationType,
        [DefaultExpression("{0,364}")]  int[] CalcDays,
        [DefaultExpression("{8,19}")]   double[] CalcTime,
        [DefaultExpression("4")] int Precision,
        [DefaultExpression("EcotectRadiationAttribute.Total")] EcotectRadiationAttribute AttributeIndex,
        [DefaultExpression("false")] bool InDynamics,
        [DefaultExpression("false")] bool WriteToFile,
        [DefaultExpression("c://ecotectInsolation.txt")] string FileName,
        [DefaultExpression("1.0")] double DisplayScale,
        [Out] ref string AttributeName,
        [Out] ref string AttributeUnits,
        [Out] ref double AttributeMaxValue,
        [Out] ref double AttributeMinValue,
        [Out] ref double[] ValueList,
        [Out] ref double[][] ValueGrid)
        {
            this.UpdateDeferral = FeatureUpdateDeferralOption.UntilUpdateAll;

            List<int> mObjId = new List<int>();

            string ecoObjIndexString = string.Empty;

            bool checkUserEdits = ReadValuesAndCheckWhetherCriticalUserEditsOccured(EcoObject, EcoProject, InfoDisplayPlane, CalculationType, CalcDays, CalcTime, Precision, AttributeIndex, InDynamics, WriteToFile, FileName, DisplayScale);

            if ((
                (updateContext.UpdatePurpose == GraphUpdatePurpose.TransactionFileGraphChangeTransaction ||
                updateContext.UpdatePurpose == GraphUpdatePurpose.ForApplyingUserEditsToFeature ||
                checkUserEdits)) ||
                updateContext.UpdatePurpose == GraphUpdatePurpose.UpdateAll ||
                firstUse)
            {
                #region Ecotect Calculate pass

                firstUse = false;

                Ecotect.Executer("set.calc.dates " + CalcDays[0].ToString() + " " + CalcDays[1].ToString());
                Ecotect.Executer("set.calc.times " + CalcTime[0].ToString() + " " + CalcTime[1].ToString());
                Ecotect.Executer("set.calc.precision " + Precision.ToString());

                //select objects in list
                switch (EcoObject.EcoObjectType)
                {
                    case EcotectObjectType.PolygonList:
                        for (int i = 0; i < EcoObject.NoOfObjectsInList; i++)
                        {
                            //Ecotect.Executer("select.object " + EcoObject.mObjectID[i]);
                            Ecotect.Executer("set.object.selected " + EcoObject.EcoObjectID[i] + " true");
                        }
                        break;
                    case EcotectObjectType.PolygonGrid:
                        for (int i = 0; i < EcoObject.NoOfObjectsInGrid[0]; i++)
                        {
                            for (int j = 0; j < EcoObject.NoOfObjectsInGrid[1]; j++)
                            {
                                //Ecotect.Executer("select.object " + EcoObject.mGridObjectID[i][j]);
                                Ecotect.Executer("set.object.selected " + EcoObject.EcoGridObjectID[i][j] + " true");
                            }
                        }
                        break;
                    case EcotectObjectType.PointGrid:
                        for (int i = 0; i < EcoObject.NoOfObjectsInGrid[0]; i++)
                        {
                            for (int j = 0; j < EcoObject.NoOfObjectsInGrid[1]; j++)
                            {
                                //Ecotect.Executer("select.object " + EcoObject.mGridObjectID[i][j]);
                                Ecotect.Executer("set.object.selected " + EcoObject.EcoGridObjectID[i][j] + " true");
                            }
                        }
                        break;
                    case EcotectObjectType.Unknown:
                        break;
                }

                switch (EcotectCalcSolar.mCalculationType)
                {
                    //calc.insolation.objects type selected accumulation [metric]
                    //type->
                    //incidence 0 Incident Solar Radiation on Points & Surfaces 
                    //absorption 1 Solar Absorbtion/Transmission of Object Surfaces 
                    //skyfactor 2 Sky Factor & Photosynthetically Active Radiation 
                    //shading 3 Shading, Overshadowing and Sunlight Hours 
                    //reference 4 COMPARE VALUE- Reference (Before) 
                    //comparison 5 COMPARE VALUE- Comparison (After) 
                    //selected -> 0,1

                    case EcotectAccumulationType.Cumulative:
                        Ecotect.Executer("calc.insolation.objects 0 0 0");
                        break;
                    case EcotectAccumulationType.AverageDaily:
                        Ecotect.Executer("calc.insolation.objects 0 0 1");
                        break;
                    case EcotectAccumulationType.AverageHourly:
                        Ecotect.Executer("calc.insolation.objects 0 0 2");
                        break;
                    case EcotectAccumulationType.Peak:
                        Ecotect.Executer("calc.insolation.objects 0 0 3");
                        break;
                    default:
                        break;
                }

                //calculation done!
                EcoObject.mEcotectRequiresEval = false;

                mDisplayableElements.Clear();
                GetResultDataFromEcotect(EcoObject, (int)AttributeIndex, updateContext);
                if (WriteToFile) WriteCalculationResultsToFile(EcoObject, FileName);
                DisplayResults(InfoDisplayPlane.DPlane3d, EcoObject, DisplayScale);
                Point3d origin = MSApp.Point3dFromXYZ(0, 0, 0);
                Element[] elemArray = mDisplayableElements.ToArray();
                SetElement(MSApp.CreateCellElement1("cell", ref elemArray, ref origin, false));

                #endregion
            }

            #region GC update (no Ecotect Recalculate necessary)


            if (InfoDisplayPlane.Visible && mGCRedisplay)
            {
                mDisplayableElements.Clear();
                GetResultDataFromEcotect(EcoObject, (int)mAttribIndex, updateContext);
                DisplayResults(InfoDisplayPlane.DPlane3d, EcoObject, DisplayScale);
                Point3d origin = MSApp.Point3dFromXYZ(0, 0, 0);
                Element[] elemArray = mDisplayableElements.ToArray();
                SetElement(MSApp.CreateCellElement1("cell", ref elemArray, ref origin, false));
                mGCRedisplay = false;
            }

            if (WriteToFile && mGCWriteToFile)
            {
                WriteCalculationResultsToFile(EcoObject, FileName);
                mWriteToFile = false;
            }



            AttributeName = mAttribName;
            AttributeUnits = mAttribUnits;
            AttributeMaxValue = mAttribMax;
            AttributeMinValue = mAttribMin;

            ValueList = mValueList;
            ValueGrid = mValueGrid;

            this.SymbologyAndLevelUsage = SymbologyAndLevelUsageOption.AssignToElement;
            return true;

            #endregion
        }

        private void DisplayResults(DPlane3d InfoDisplayPlane, EcotectPolygonObject EcoObject, double dispScale)
        {
            if(EcoObject.mTextStyle != null)
            MSApp.ActiveSettings.TextStyle = EcoObject.mTextStyle.MSTextStyle; //.textStyle;

            int countElement = 0;
            string displayString = "";

            Point3d oLow = MSApp.Point3dFromXYZ(0, 0, 0);
            Point3d oHigh = MSApp.Point3dFromXYZ(10 * dispScale, 100 * dispScale, 0);

            DPlane3d dRefPlane = InfoDisplayPlane;
            DVector3d dOut = dRefPlane.Normal;

            //get transforms from reference plane
            DTransform3d Tplane2world = default(DTransform3d);
            DTransform3d Tworld2plane = default(DTransform3d);
            dRefPlane.GetTransforms(out Tplane2world, out Tworld2plane);

            //get origin 
            Point3d origin = MSApp.Point3dFromXYZ(dRefPlane.Origin.X, dRefPlane.Origin.Y, dRefPlane.Origin.Z);

            //create a Transform3D object from the DTransform3D object and the origin (there must be a more elegant method).
            Transform3d t3d = default(Transform3d);
            t3d.RowX = MSApp.Point3dFromXYZ(Tplane2world.RowX.X, Tplane2world.RowX.Y, Tplane2world.RowX.Z);
            t3d.RowY = MSApp.Point3dFromXYZ(Tplane2world.RowY.X, Tplane2world.RowY.Y, Tplane2world.RowY.Z);
            t3d.RowZ = MSApp.Point3dFromXYZ(Tplane2world.RowZ.X, Tplane2world.RowZ.Y, Tplane2world.RowZ.Z);
            t3d.TranslationX = dRefPlane.Origin.X;
            t3d.TranslationY = dRefPlane.Origin.Y;
            t3d.TranslationZ = dRefPlane.Origin.Z;

            //extract rotation matrix
            Matrix3d dRotMat = MSApp.Matrix3dFromTransform3d(ref t3d);

            Element[] oDisplayableElementArray = null;

            switch (EcoObject.EcoObjectType)
            {
                case EcotectObjectType.PolygonList:

                    EcotectPolygonObject EcoPolyObject = (EcotectPolygonObject)EcoObject;
                    oDisplayableElementArray = new Element[EcoPolyObject.NoOfObjectsInList + 23];
                    
                    //Create text elements
                    for (int i = 0; i < EcoObject.NoOfObjectsInList; i++)
                    {
                        DPoint3d centroid = EcoPolyObject.CentroidAsDPoint3D(EcoPolyObject.mObjectList[i]);
                        Point3d com_centroid = DgnTools.ToPoint3d(centroid);

                        //oDisplayableElementArray[i] = (Element)createTextOrTextNode(com_centroid, dRotMat, mValueList[i].ToString(), EcoPolyObject.mTextStyle.MSTextStyle);
                        Element t = (Element)createTextOrTextNode(com_centroid, dRotMat, mValueList[i].ToString(), EcoPolyObject.mTextStyle.MSTextStyle);
                        mDisplayableElements.Add(t);

                        countElement++;
                    }
                    break;

                case EcotectObjectType.PolygonGrid:

                    EcoPolyObject = (EcotectPolygonObject)EcoObject;

                    oDisplayableElementArray = new Element[EcoPolyObject.NoOfObjectsInGrid[0] * EcoPolyObject.NoOfObjectsInGrid[1] + 23];

                    for (int i = 0; i < EcoPolyObject.NoOfObjectsInGrid[0]; i++)
                    {
                        for (int j = 0; j < EcoPolyObject.NoOfObjectsInGrid[1]; j++)
                        {
                            DPoint3d centroid = EcoPolyObject.CentroidAsDPoint3D(EcoPolyObject.mObjectGrid[i][j]);
                            Point3d com_centroid = DgnTools.ToPoint3d(centroid);
                            
                            //oDisplayableElementArray[countElement] = (Element) createTextOrTextNode(com_centroid, dRotMat, mValueGrid[i][j].ToString(), EcoPolyObject.mTextStyle.MSTextStyle);
                            Element t = (Element)createTextOrTextNode(com_centroid, dRotMat, mValueGrid[i][j].ToString(), EcoPolyObject.mTextStyle.MSTextStyle);
                            mDisplayableElements.Add(t);
                            countElement++;
                        }
                    }

                    break;

                case EcotectObjectType.PointGrid:
                    //cannot color points
                    break;

                case EcotectObjectType.PointList:
                    //cannot color points
                    break;

                case EcotectObjectType.Unknown:
                    break;

                default:
                    //oDisplayableElementArray = new Element[23];
                    break;
            }

            //Setting the display to the plane

            Point3d[] sVertices = new Point3d[5];
            Point3d displayStringVert = new Point3d();


            for (int i = 0; i < 22; i = i + 2)
            {
                sVertices[0] = MSApp.Point3dFromTransform3dTimesXYZ(ref t3d, oLow.X, oLow.Y + (oHigh.Y - oLow.Y) / 20 * i / 2, oLow.Z);
                sVertices[1] = MSApp.Point3dFromTransform3dTimesXYZ(ref t3d, oHigh.X, oLow.Y + (oHigh.Y - oLow.Y) / 20 * i / 2, oLow.Z);
                sVertices[2] = MSApp.Point3dFromTransform3dTimesXYZ(ref t3d, oHigh.X, oLow.Y + (oHigh.Y - oLow.Y) / 20 * (i / 2 + 1), oLow.Z);
                sVertices[3] = MSApp.Point3dFromTransform3dTimesXYZ(ref t3d, oLow.X, oLow.Y + (oHigh.Y - oLow.Y) / 20 * (i / 2 + 1), oLow.Z);
                sVertices[4] = sVertices[0];

                displayStringVert = MSApp.Point3dFromTransform3dTimesXYZ(ref t3d, oHigh.X + dispScale, oLow.Y + (oHigh.Y - oLow.Y) / 20 * (i / 2 + 1) - 2 * dispScale, oLow.Z);

                double aRange = (mAttribMax - mAttribMin);
                double aVal = mAttribMin + aRange / 10 * i / 2;
                displayString = aVal.ToString() + " " + mAttribUnits;

                double rVal = Math.Min(1, 2 * (aVal - mAttribMin) / aRange) * 255;
                double gVal = Math.Max(0, 2 * (aVal - mAttribMin) / aRange - 1) * 255;
                double bVal = Math.Max(0, 1 - 2 * (aVal - mAttribMin) / aRange) * 255;

                //oDisplayableElementArray[i + countElement] = (Element)MSApp.CreateShapeElement1(TemplateElement, ref sVertices, MsdFillMode.Filled);
                //oDisplayableElementArray[i + countElement].Color = RGB((int)rVal, (int)gVal, (int)bVal);
                Element t = (Element)MSApp.CreateShapeElement1(TemplateElement, ref sVertices, MsdFillMode.Filled);
                t.Color = RGB((int)rVal, (int)gVal, (int)bVal);
                mDisplayableElements.Add(t);
                
                //oDisplayableElementArray[i + countElement + 1] = (Element)MSApp.CreateTextElement1(TemplateElement, displayString, ref displayStringVert, ref dRotMat);
                Element t1 = (Element)MSApp.CreateTextElement1(TemplateElement, displayString, ref displayStringVert, ref dRotMat);
                mDisplayableElements.Add(t1);

            }

            displayStringVert = MSApp.Point3dFromTransform3dTimesXYZ(ref t3d, oLow.X , oLow.Y + (oHigh.Y - oLow.Y) / 20 * 12 - 2 * dispScale, oLow.Z);

            displayString = mAttribName + " | ValueRange[" + mAttribMin + " , " + mAttribMax + "]";
            //oDisplayableElementArray[22 + countElement] = (Element)MSApp.CreateTextElement1(TemplateElement, displayString, ref displayStringVert, ref dRotMat);
            Element t2 = (Element)MSApp.CreateTextElement1(TemplateElement, displayString, ref displayStringVert, ref dRotMat);
            mDisplayableElements.Add(t2);
        }

        private static Element createTextOrTextNode(Point3d com_origin, Matrix3d com_orientation, string TextString, Bentley.Interop.MicroStationDGN.TextStyle TxtStyle)
        {
            try
            {
                List<string> strings = new List<string>();
                strings.AddFromLineBreakDelimitedString(TextString, false);
                //TextTools.AddFromLineBreakDelimitedString(strings, TextString, false);
                if (strings.Count > 1)
                {
                    Bentley.Interop.MicroStationDGN.TextStyle saveTextStyle = MSApp.ActiveSettings.TextStyle;
                    if (TxtStyle != null)
                    {
                        MSApp.ActiveSettings.TextStyle = TxtStyle; //.textStyle;
                    }
                    TextNodeElement textNodeElement = MSApp.CreateTextNodeElement1(null, ref com_origin, ref com_orientation);

                    foreach (string s in strings)
                    {
                        textNodeElement.AddTextLine(s);
                    }

                    MSApp.ActiveSettings.TextStyle = saveTextStyle;
                    return (Element)textNodeElement;
                }
                else
                {
                    TextElement text = MSApp.CreateTextElement1(null, TextString, ref com_origin, ref com_orientation);
                    if (TxtStyle != null)
                    {
                        text.TextStyle = TxtStyle; //.textStyle;
                    }
                    return (Element)text;
                }
            }
            catch
            {
            }
            return null;
        }
        
        private void WriteCalculationResultsToFile(EcotectPolygonObject EcoObject, string FileName)
        {
            string attribValString;
            FileStream strmA;
            string a,b,c;
            double tempD;

            try
            {

                strmA = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.Write);
                StreamWriter wtr = new StreamWriter(strmA);
                wtr.WriteLine("-- GC.EcotectCalc.FileOutput [UNDER CONSTRUCTION]");
                wtr.WriteLine("-- " + DateTime.Now.ToString());
                wtr.WriteLine(wtr.NewLine);
                wtr.WriteLine("Calculation Data [" + mAttribUnits + "]");
                wtr.WriteLine("Range [" + mAttribMax + "," + mAttribMin + "]");
                wtr.WriteLine("Dates [" + mCalcDays[0] + "," + mCalcDays[1] + "]");
                wtr.WriteLine("Time  [" + mCalcTime[0] + "," + mCalcTime[1] + "]");
                attribValString = Ecotect.Requester("get.attribute.name 0");
                words = attribValString.Split(delimiterChars);
                wtr.WriteLine("Attrib1 :" + words[1]);
                attribValString = Ecotect.Requester("get.attribute.name 1");
                words = attribValString.Split(delimiterChars);
                wtr.WriteLine("Attrib2 :" + words[1]);
                attribValString = Ecotect.Requester("get.attribute.name 2");
                words = attribValString.Split(delimiterChars);
                wtr.WriteLine("Attrib3 :" + words[1]);

                wtr.WriteLine(wtr.NewLine);
                wtr.WriteLine(wtr.NewLine);
                wtr.WriteLine("-------------------------------------------------");
                wtr.WriteLine(String.Format("          {0,10}{1,10}{2,10}", "Attrib1", "Attrib2", "Attrib3"));
                wtr.WriteLine("-------------------------------------------------");
                

                switch (EcoObject.EcoObjectType)
                {
                    case EcotectObjectType.PolygonList:
                        for (int i = 0; i < EcoObject.EcoObjectID.Length; i++)
                        {
                            attribValString = Ecotect.Requester("get.object.attr1 " + EcoObject.EcoObjectID[i]);
                            words = attribValString.Split(delimiterChars);
                            tempD = double.Parse(words[0]);
                            a = string.Format("{0:#.##}", tempD);
                            
                            attribValString = Ecotect.Requester("get.object.attr2 " + EcoObject.EcoObjectID[i]);
                            words = attribValString.Split(delimiterChars);
                            tempD = double.Parse(words[0]);
                            b = string.Format("{0:#.##}", tempD);

                            attribValString = Ecotect.Requester("get.object.attr3 " + EcoObject.EcoObjectID[i]);
                            words = attribValString.Split(delimiterChars);
                            tempD = double.Parse(words[0]);
                            c = string.Format("{0:#.##}", tempD);
                            
                            wtr.WriteLine(String.Format("Object{0},  {1,10},{2,10},{3,10}", EcoObject.EcoObjectID[i],a,b,c));
                        }
                        break;

                    case EcotectObjectType.PolygonGrid:
                        for (int i = 0; i < EcoObject.EcoGridObjectID.GetLength(0); i++)
                        {
                            for (int j = 0; j < EcoObject.EcoGridObjectID[i].Length; j++)
                            {
                                attribValString = Ecotect.Requester("get.object.attr1 " + EcoObject.EcoGridObjectID[i][j]);
                                words = attribValString.Split(delimiterChars);
                                tempD = double.Parse(words[0]);
                                a = string.Format("{0:#.##}", tempD);

                                attribValString = Ecotect.Requester("get.object.attr2 " + EcoObject.EcoGridObjectID[i][j]);
                                words = attribValString.Split(delimiterChars);
                                tempD = double.Parse(words[0]);
                                b = string.Format("{0:#.##}", tempD);

                                attribValString = Ecotect.Requester("get.object.attr3 " + EcoObject.EcoGridObjectID[i][j]);
                                words = attribValString.Split(delimiterChars);
                                tempD = double.Parse(words[0]);
                                c = string.Format("{0:#.##}", tempD);

                                wtr.WriteLine("Object " + EcoObject.EcoGridObjectID[i][j] + "->  " + a + ",  " + b + ",  " + c);
                            }
                        }

                        break;
                    default:
                        break;
                }

                wtr.Close();
            }
            catch (Exception ex)
            {
                throw new GCException(ex.Message);
            }
        }

        private static bool ReadValuesAndCheckWhetherCriticalUserEditsOccured(EcotectPolygonObject EcoObject,
                                                      EcotectProject EcoProject,
                                                      Plane InfoDisplayPlane,
                                                      EcotectAccumulationType CalculationType,
                                                      int[] CalcDays,
                                                      double[] CalcTime,
                                                      int Precision,
                                                      EcotectRadiationAttribute AttributeIndex,
                                                      bool InDynamics,
                                                      bool WriteToFile,
                                                      string FilePath,
                                                      double DisplayScale)
        {
            bool criticalEdit = false;
            mGCRedisplay = false;
            mGCWriteToFile = false;

            if (EcoObject.mEcotectRequiresEval) criticalEdit = true;

            if (EcoProject != mEcoProject)
            {
                mEcoProject = EcoProject;
                criticalEdit = true;
            }

            if (InfoDisplayPlane.HasChanged)
            {
                DPlane3d dp = InfoDisplayPlane.DPlane3d;
                mInfoDisplayPlane = new DPlane3d(dp.Origin.X, dp.Origin.Y, dp.Origin.z, dp.Normal.X, dp.Normal.Y, dp.Normal.Z);
                criticalEdit = false;
                mGCRedisplay = true;
            }

            if (CalculationType != mCalculationType)
            {
                mCalculationType = CalculationType;
                criticalEdit = true;
            }

            if (CalcDays[0] != mCalcDays[0] || CalcDays[1] != mCalcDays[1])
            {
                mCalcDays[0] = CalcDays[0];
                mCalcDays[1] = CalcDays[1];
                criticalEdit = true;
            }

            if (CalcTime[0] != mCalcTime[0] || CalcTime[1] != mCalcTime[1])
            {
                mCalcTime[0] = CalcTime[0];
                mCalcTime[1] = CalcTime[1];
                criticalEdit = true;
            }


            if (Precision != mPrecision)
            {
                mPrecision = Precision;
                criticalEdit = true;
            }

            if (AttributeIndex != mAttribIndex) { mAttribIndex = AttributeIndex; mGCRedisplay = true; }
            if (InDynamics != mInDynamics) { mInDynamics = InDynamics; if (mInDynamics) criticalEdit = true; }
            if (WriteToFile != mWriteToFile) { mWriteToFile = WriteToFile; mGCWriteToFile = true; }
            if (FilePath != mFileName) { mFileName = FilePath; mGCWriteToFile = true; }
            if (DisplayScale != mDisplayScale) { mDisplayScale = DisplayScale; mGCRedisplay = true; }
            
            return criticalEdit;
        }


        private void GetResultDataFromEcotect(EcotectPolygonObject EcoObject, int AttributeIndex, FeatureUpdateContext updateContext)
        {

            int ecoObjID = 0;
            string attribValString = string.Empty;
            double attribVal = 0;
            string attribScaleString = string.Empty;
            double attribRange;

            //update ecotect display -IMP
            Ecotect.Executer("app.menu display.attributes " + AttributeIndex);

            string tempAttribInfo = Ecotect.Requester("get.attribute.name " + AttributeIndex.ToString());
            mAttribName = (tempAttribInfo.Split(delimiterChars))[1];
            //attrib units
            mAttribUnits = ((Ecotect.Requester("get.attribute.units " + AttributeIndex.ToString())).Split(delimiterChars))[1];
            //attribute scale information for scaling the colors
            attribScaleString = Ecotect.Requester("get.attribute.scale");
            words = attribScaleString.Split(delimiterChars); //remove the '\0' or null escape sequences
            mAttribMin = double.Parse(words[0]); 
            mAttribMax = double.Parse(words[1]);
            attribRange = (mAttribMax - mAttribMin);


   
            switch (EcoObject.EcoObjectType)
            {
                case EcotectObjectType.PolygonList:

                    EcotectPolygonObject EcoPolyObject = (EcotectPolygonObject)EcoObject;

                    mValueList = new double[EcoPolyObject.NoOfObjectsInList];

                    //Create the colored polygons
                    for (int i = 0; i < EcoObject.NoOfObjectsInList; i++)
                    {
                        
                        DPoint3d[] vertices = EcoPolyObject.mObjectList[i].verts;
                        
                        Point3d[] v = new Point3d[EcoPolyObject.mObjectList[i].verts.Length];
                        int count = 0;
                        foreach (DPoint3d dV in EcoPolyObject.mObjectList[i].verts)
                        {
                            v[count] = MSApp.Point3dFromXYZ(dV.X, dV.Y, dV.Z);
                            count++;
                        }

                        switch (AttributeIndex)
                        {
                            case 0:
                                attribValString = Ecotect.Requester("get.object.attr1 " + EcoPolyObject.EcoObjectID[i]);
                                break;
                            case 1:
                                attribValString = Ecotect.Requester("get.object.attr2 " + EcoPolyObject.EcoObjectID[i]);
                                break;
                            case 2:
                                attribValString = Ecotect.Requester("get.object.attr3 " + EcoPolyObject.EcoObjectID[i]);
                                break;
                            default:
                                attribValString = Ecotect.Requester("get.object.attr1 " + EcoPolyObject.EcoObjectID[i]);
                                break;
                        }

                        words = attribValString.Split(delimiterChars);
                        attribVal = double.Parse(words[0]);
                        mValueList[i] = attribVal;
                        Element poly = (Element)MSApp.CreateShapeElement1(TemplateElement, ref v, MsdFillMode.Filled);

                        double rVal = Math.Min(1, 2 * (attribVal - mAttribMin) / attribRange) * 255;
                        double gVal = Math.Max(0, 2 * (attribVal - mAttribMin) / attribRange - 1) * 255;
                        double bVal = Math.Max(0, 1 - 2 * (attribVal - mAttribMin) / attribRange) * 255;

                        poly.Color = RGB((int)rVal, (int)gVal, (int)bVal);
                        mDisplayableElements.Add(poly);

                        ecoObjID++;
                    }
                   
                    break;

                case EcotectObjectType.PolygonGrid:

                    EcoPolyObject = (EcotectPolygonObject)EcoObject;

                    mValueGrid = new double[EcoPolyObject.NoOfObjectsInGrid[0]][];

                    for (int i = 0; i < EcoPolyObject.NoOfObjectsInGrid[0]; i++)
                    {
                        mValueGrid[i] = new double[EcoPolyObject.NoOfObjectsInGrid[1]];

                        for (int j = 0; j < EcoPolyObject.NoOfObjectsInGrid[1]; j++)
                        {

                            DPoint3d[] vertices = EcoPolyObject.mObjectGrid[i][j].verts;

                            Point3d[] v = new Point3d[EcoPolyObject.mObjectList[i].verts.Length];
                            int count = 0;
                            foreach (DPoint3d dV in EcoPolyObject.mObjectList[i].verts)
                            {
                                v[count] = MSApp.Point3dFromXYZ(dV.X, dV.Y, dV.Z);
                                count++;
                            }

                            switch (AttributeIndex)
                            {
                                case 0:
                                    attribValString = Ecotect.Requester("get.object.attr1 " + EcoPolyObject.EcoGridObjectID[i][j]);
                                    break;
                                case 1:
                                    attribValString = Ecotect.Requester("get.object.attr2 " + EcoPolyObject.EcoGridObjectID[i][j]);
                                    break;
                                case 2:
                                    attribValString = Ecotect.Requester("get.object.attr3 " + EcoPolyObject.EcoGridObjectID[i][j]);
                                    break;
                                default:
                                    attribValString = Ecotect.Requester("get.object.attr1 " + EcoPolyObject.EcoGridObjectID[i][j]);
                                    break;
                            }

                            words = attribValString.Split(delimiterChars);
                            attribVal = double.Parse(words[0]);

                            mValueGrid[i][j] = attribVal;

                            Element poly = (Element)MSApp.CreateShapeElement1(TemplateElement, ref v, MsdFillMode.Filled);

                            double rVal = Math.Min(1, 2 * (attribVal - mAttribMin) / attribRange) * 255;
                            double gVal = Math.Max(0, 2 * (attribVal - mAttribMin) / attribRange - 1) * 255;
                            double bVal = Math.Max(0, 1 - 2 * (attribVal - mAttribMin) / attribRange) * 255;

                            poly.Color = RGB((int)rVal, (int)gVal, (int)bVal);
                            mDisplayableElements.Add(poly);
                            ecoObjID++;
                        }
                    }
                    break;
                case EcotectObjectType.PointGrid:
                    //cannot color points
                    //maybe create a marker here
                    break;
                case EcotectObjectType.PointList:
                    //cannot color points
                    //maybe create a marker here
                    break;

                case EcotectObjectType.Unknown:
                    break;
            } //switch

            this.ConstructionsVisible = false;

        }//method
        #endregion solar radiation


    } //class
}//namespace

