/*
 * EcotectCalcDayLight class
 * Version: EcotectLink V8i_R3
 * Author: Kaustuv DeBiswas
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Bentley.Interop.MicroStationDGN;
using System.Collections.Specialized;
using Bentley.GenerativeComponents.GeneralPurpose;
using Bentley.GenerativeComponents;
using Bentley.GenerativeComponents.MicroStation;
using Bentley.Geometry;
using Bentley.GenerativeComponents.GCScript;
using Bentley.GenerativeComponents.Features;
using Bentley.GenerativeComponents.Features.Specific;
using System.Threading;
using System.IO;

namespace Bentley.GenerativeComponents.Features
{

    /// <summary>Perfomance Analysis via Ecotect</summary>
    //[HideInheritedUpdateMethods]
    public class EcotectCalcDaylight : Feature
    {
        #region field

        //private static EcotectPointObject mEcoPointObject;
        private static EcotectProject mEcoProject;
        private static DPlane3d mInfoDisplayPlane;
        private static EcotectSkyIlluminance mSkyIlluminance;
        private static int[] mCalcDays = new int[]{0,0};
        private static double[] mCalcTime = new double[]{0,0};
        private static int mPrecision;
        private static EcotectDayLightAttribute mAttribIndex;
        private static bool mInDynamics;
        private static bool mWriteToFile;
        private static string mFileName;
        private static double mMarkerSize;
        private static bool mMarkerOn;
        private static bool[] mEditFlag = new bool[] { false, false, false, false, false, false, false, false, false, false, false, false, false };
        private static double mDisplayScale = 1;

        private static string mAttribName;
        private static string mAttribUnits;
        private static double mAttribMax;
        private static double mAttribMin;

        private static double[] mValueList;
        private static double[][] mValueGrid;

        private static bool mGCRedisplay = false;
        private static bool mGCWriteToFile = false;

        private char[] delimiterChars = { '\0', ',', '\"' };
        private string[] words;
        private bool firstUse = true;

        #endregion

        #region constructor

        public EcotectCalcDaylight
        (
        )
            : base()
        {
        }

        public EcotectCalcDaylight
        (
        Feature parentFeature
        )
            : base(parentFeature)
        {
        }

        #endregion

        #region update method

        [Update]
        public virtual bool DaylightCalculationByPoints
        (
        FeatureUpdateContext updateContext,
        [ParentModel, Replicatable]EcotectPointObject EcoPointObject,
        EcotectProject EcoProject,
        Plane InfoDisplayPlane,
        EcotectSkyIlluminance SkyIlluminance,
        [DefaultExpression("{0,364}")]  int[] CalcDays,
        [DefaultExpression("{8,19}")]   double[] CalcTime,
        [DefaultExpression("3")] int Precision,
        [DefaultExpression("EcotectDayLightAttribute.DaylightFactor")] EcotectDayLightAttribute AttributeIndex,
        [DefaultExpression("false")] bool InDynamics,
        [DefaultExpression("false")] bool WriteToFile,
        [DefaultExpression("C://ecotectDaylight.txt")] string FileName,
        [DefaultExpression("1.0")] double MarkerSize,
        [DefaultExpression("true")] bool MarkerOn,
        [DefaultExpression("1")] double DisplayScale,
        [Out] ref string AttributeName,
        [Out] ref string AttributeUnits,
        [Out] ref double AttributeMaxValue,
        [Out] ref double AttributeMinValue,
        [Out] ref double[] ValueList,
        [Out] ref double[][] ValueGrid
         )
        {
            List<int> mObjId = new List<int>();

            string ecoObjIndexString = string.Empty;

            bool checkUserEdits = ReadValuesAndCheckWhetherCriticalUserEditsOccured(EcoPointObject, EcoProject, InfoDisplayPlane, SkyIlluminance, CalcDays, CalcTime, Precision, AttributeIndex, InDynamics, WriteToFile, FileName, MarkerSize, MarkerOn, DisplayScale);
            
            if ((InDynamics &&
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

                switch (SkyIlluminance)
                {
                    case EcotectSkyIlluminance.CIE_Overcast_Sky_3000:
                        Ecotect.Executer("set.calc.sky 0 3000");
                        break;
                    case EcotectSkyIlluminance.CIE_Overcast_Sky_4500:
                        Ecotect.Executer("set.calc.sky 0 4500");
                        break;
                    case EcotectSkyIlluminance.CIE_Overcast_Sky_6000:
                        Ecotect.Executer("set.calc.sky 0 6000");
                        break;
                    case EcotectSkyIlluminance.CIE_Overcast_Sky_9000:
                        Ecotect.Executer("set.calc.sky 0 9000");
                        break;
                    case EcotectSkyIlluminance.CIE_Uniform_Sky_3000:
                        Ecotect.Executer("set.calc.sky 1 3000");
                        break;
                    case EcotectSkyIlluminance.CIE_Uniform_Sky_4500:
                        Ecotect.Executer("set.calc.sky 1 4500");
                        break;
                    case EcotectSkyIlluminance.CIE_Uniform_Sky_6000:
                        Ecotect.Executer("set.calc.sky 1 6000");
                        break;
                    case EcotectSkyIlluminance.CIE_Uniform_Sky_9000:
                        Ecotect.Executer("set.calc.sky 1 9000");
                        break;
                    default:
                        break;
                }


                //select objects in list
                switch (EcoPointObject.EcoObjectType)
                {
                    case EcotectObjectType.PointList:
                        for (int i = 0; i < EcoPointObject.NoOfObjectsInList; i++)
                        {
                            Ecotect.Executer("set.object.selected " + EcoPointObject.EcoObjectID[i] + " true");
                        }
                        break;

                    case EcotectObjectType.PointGrid:
                        for (int i = 0; i < EcoPointObject.NoOfObjectsInGrid[0]; i++)
                        {
                            for (int j = 0; j < EcoPointObject.NoOfObjectsInGrid[1]; j++)
                            {
                                Ecotect.Executer("set.object.selected " + EcoPointObject.EcoGridObjectID[i][j] + " true");
                            }
                        }
                        break;

                    case EcotectObjectType.Unknown:
                        break;
                }

                Ecotect.Executer("calc.lighting.points daylight true 0");

                //calculation done!
                EcoPointObject.EcotectRequiresEval = false;

                GetResultDataFromEcotect(EcoPointObject, (int)mAttribIndex, updateContext);
                if (WriteToFile) WriteCalculationResultsToFile_Point(EcoPointObject, FileName);
                DisplayResults(InfoDisplayPlane.DPlane3d, EcoPointObject, MarkerSize, MarkerOn, DisplayScale);

                #endregion critical update
            }

            #region GC update (no Ecotect Recalculate necessary)

            if (InfoDisplayPlane.Visible && mGCRedisplay)
            {
                GetResultDataFromEcotect(EcoPointObject, (int)mAttribIndex, updateContext);
                DisplayResults(InfoDisplayPlane.DPlane3d, EcoPointObject, MarkerSize, MarkerOn, DisplayScale);
                mGCRedisplay = false;
            }

            if (WriteToFile && mGCWriteToFile)
            {
                WriteCalculationResultsToFile_Point(EcoPointObject, FileName);
                mWriteToFile = false;
            }
            

            AttributeName = mAttribName;
            AttributeUnits = mAttribUnits;
            AttributeMaxValue = mAttribMax;
            AttributeMinValue = mAttribMin;

            ValueList = mValueList;
            ValueGrid = mValueGrid;

            #endregion 

            this.SymbologyAndLevelUsage = SymbologyAndLevelUsageOption.AssignToElement;
            return true;
        }

        #endregion

        #region update helpers

        private void DisplayResults(DPlane3d InfoDisplayPlane, EcotectPointObject EcoObject, double MarkerSize, bool MarkerOn, double dispScale)
        {
            if(EcoObject.TextStyle != null)
            MSApp.ActiveSettings.TextStyle = EcoObject.TextStyle.MSTextStyle; //.textStyle;
          

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
                case EcotectObjectType.PointList:

                    EcotectPointObject EcoPObject = (EcotectPointObject)EcoObject;

                    oDisplayableElementArray = new Element[EcoPObject.NoOfObjectsInList * 2 + 23];

                    //Create text elements
                    for (int i = 0; i < EcoObject.NoOfObjectsInList; i++)
                    {
                        DPoint3d centroid = EcoPObject.mObjectList[i];
                        Point3d text_origin = MSApp.Point3dFromXYZ(centroid.x, centroid.y, centroid.z + MarkerSize / 4);


                        Point3d[] vertices = new Point3d[4];
                        vertices[0] = MSApp.Point3dFromXYZ(centroid.X - MarkerSize, centroid.Y - MarkerSize, centroid.Z + MarkerSize / 5);
                        vertices[1] = MSApp.Point3dFromXYZ(centroid.X - MarkerSize, centroid.Y + MarkerSize, centroid.Z + MarkerSize / 5);
                        vertices[2] = MSApp.Point3dFromXYZ(centroid.X + MarkerSize, centroid.Y + MarkerSize, centroid.Z + MarkerSize / 5);
                        vertices[3] = MSApp.Point3dFromXYZ(centroid.X + MarkerSize, centroid.Y - MarkerSize, centroid.Z + MarkerSize / 5);

                        double aRange = (mAttribMax - mAttribMin);
                        double aVal = mValueList[i];
                        displayString = aVal.ToString() + " " + mAttribUnits;

                        double rVal = Math.Min(1, 2 * (aVal - mAttribMin) / aRange) * 255;
                        double gVal = Math.Max(0, 2 * (aVal - mAttribMin) / aRange - 1) * 255;
                        double bVal = Math.Max(0, 1 - 2 * (aVal - mAttribMin) / aRange) * 255;
                        
                        if (MarkerOn)
                        {
                            oDisplayableElementArray[countElement] = (Element)MSApp.CreateShapeElement1(TemplateElement, ref vertices, MsdFillMode.Filled);
                            oDisplayableElementArray[countElement].Color = RGB((int)rVal, (int)gVal, (int)bVal);
                            countElement++;
                        }

                        oDisplayableElementArray[countElement] = (Element)createTextOrTextNode(text_origin, dRotMat, displayString, EcoPObject.TextStyle);
                        countElement++;
                    }
                    break;

                case EcotectObjectType.PointGrid:

                    EcoPObject = (EcotectPointObject)EcoObject;

                    oDisplayableElementArray = new Element[EcoPObject.NoOfObjectsInGrid[0] * EcoPObject.NoOfObjectsInGrid[1] * 2 + 23];

                    for (int i = 0; i < EcoPObject.NoOfObjectsInGrid[0]; i++)
                    {
                        for (int j = 0; j < EcoPObject.NoOfObjectsInGrid[1]; j++)
                        {
                            DPoint3d centroid = EcoPObject.mObjectGrid[i][j];
                            Point3d text_origin = MSApp.Point3dFromXYZ(centroid.x, centroid.y, centroid.z + MarkerSize / 4);

                            Point3d[] vertices = new Point3d[4];
                            vertices[0] = MSApp.Point3dFromXYZ(centroid.X - MarkerSize, centroid.Y - MarkerSize, centroid.Z + MarkerSize / 5);
                            vertices[1] = MSApp.Point3dFromXYZ(centroid.X - MarkerSize, centroid.Y + MarkerSize, centroid.Z + MarkerSize / 5);
                            vertices[2] = MSApp.Point3dFromXYZ(centroid.X + MarkerSize, centroid.Y + MarkerSize, centroid.Z + MarkerSize / 5);
                            vertices[3] = MSApp.Point3dFromXYZ(centroid.X + MarkerSize, centroid.Y - MarkerSize, centroid.Z + MarkerSize / 5);

                            double aRange = (mAttribMax - mAttribMin);
                            double aVal = mValueGrid[i][j];
                            displayString = aVal.ToString() + " " + mAttribUnits;

                            double rVal = Math.Min(1, 2 * (aVal - mAttribMin) / aRange) * 255;
                            double gVal = Math.Max(0, 2 * (aVal - mAttribMin) / aRange - 1) * 255;
                            double bVal = Math.Max(0, 1 - 2 * (aVal - mAttribMin) / aRange) * 255;
                            if (MarkerOn)
                            {
                                oDisplayableElementArray[countElement] = (Element)MSApp.CreateShapeElement1(TemplateElement, ref vertices, MsdFillMode.Filled);
                                oDisplayableElementArray[countElement].Color = RGB((int)rVal, (int)gVal, (int)bVal);
                                countElement++;
                            }
                            oDisplayableElementArray[countElement] = (Element)createTextOrTextNode(text_origin, dRotMat, displayString, EcoPObject.TextStyle);
                            countElement++;
                        }
                    }
                    break;

                case EcotectObjectType.Unknown:
                    break;

                default:
                    oDisplayableElementArray = new Element[23];
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

                oDisplayableElementArray[i + countElement] = (Element)MSApp.CreateShapeElement1(TemplateElement, ref sVertices, MsdFillMode.Filled);
                oDisplayableElementArray[i + countElement].Color = RGB((int)rVal, (int)gVal, (int)bVal);

                oDisplayableElementArray[i + countElement + 1] = (Element)MSApp.CreateTextElement1(TemplateElement, displayString, ref displayStringVert, ref dRotMat);
            }

            displayStringVert = MSApp.Point3dFromTransform3dTimesXYZ(ref t3d, oLow.X, oLow.Y + (oHigh.Y - oLow.Y) / 20 * (22 / 2 + 1) - 2 * dispScale, oLow.Z);

            displayString = mAttribName + " | ValueRange[" + mAttribMin + " , " + mAttribMax + "]";
            oDisplayableElementArray[22 + countElement] = (Element)MSApp.CreateTextElement1(TemplateElement, displayString, ref displayStringVert, ref dRotMat);

            Element[] ElementArray = oDisplayableElementArray;

            SetElement(MSApp.CreateCellElement1(" ", ref ElementArray, ref oLow, false));
        }

        private static Element createTextOrTextNode(Point3d com_origin, Matrix3d com_orientation, string TextString, Bentley.GenerativeComponents.Features.Specific.TextStyle TextStyle)
        {
            try
            {
                List<string> strings = new List<string>();
                
                strings.AddFromLineBreakDelimitedString(TextString, false);

                if (strings.Count > 1)
                {
                    Bentley.Interop.MicroStationDGN.TextStyle saveTextStyle = MSApp.ActiveSettings.TextStyle;
                    if (TextStyle != null)
                    {
                        MSApp.ActiveSettings.TextStyle = TextStyle.MSTextStyle; //.textStyle;
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
                    if (TextStyle != null)
                    {
                        text.TextStyle = TextStyle.MSTextStyle; //.textStyle;
                    }
                    return (Element)text;
                }
            }
            catch
            {
            }
            return null;
        }
    
        private void WriteCalculationResultsToFile_Point(EcotectPointObject EcoObject, string FileName)
        {
            string attribValString;
            FileStream strmA;
            string a, b, c;
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
                    case EcotectObjectType.PointList:
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

                            wtr.WriteLine(String.Format("Object{0},  {1,10},{2,10},{3,10}", EcoObject.EcoObjectID[i], a, b, c));
                        }
                        break;

                    case EcotectObjectType.PointGrid:
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

        private static bool ReadValuesAndCheckWhetherCriticalUserEditsOccured(EcotectPointObject EcoObject,
                                                      EcotectProject EcoProject,
                                                      Plane InfoDisplayPlane,
                                                      EcotectSkyIlluminance SkyIlluminance,
                                                      int[] CalcDays,
                                                      double[] CalcTime,
                                                      int Precision,
                                                      EcotectDayLightAttribute AttributeIndex,
                                                      bool InDynamics,
                                                      bool WriteToFile,
                                                      string FilePath,
                                                      double MarkerSize,
                                                      bool MarkerOn,
                                                      double dispScale)
        {
            bool criticalEdit = false;

            if (EcoObject.EcotectRequiresEval) criticalEdit = true;

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


            if (SkyIlluminance != mSkyIlluminance)
            {
                mSkyIlluminance = SkyIlluminance;
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
            if (MarkerSize != mMarkerSize) { mMarkerSize = MarkerSize; mGCRedisplay = true; }
            if (MarkerOn != mMarkerOn) { mMarkerOn = MarkerOn; mGCRedisplay = true; }
            if (mDisplayScale != dispScale) { mDisplayScale = dispScale; mGCRedisplay = true; }

            return criticalEdit;
        }

        private void GetResultDataFromEcotect(EcotectPointObject EcoObject, int AttributeIndex, FeatureUpdateContext updateContext)
        {
            //clear old
            this.DeleteConstituentFeatures(updateContext);

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
                case EcotectObjectType.PointList:

                    EcotectPointObject EcoPObject = (EcotectPointObject)EcoObject;

                    mValueList = new double[EcoPObject.NoOfObjectsInList];
                   
                    //Create the colored points
                    for (int i = 0; i < EcoObject.NoOfObjectsInList; i++)
                    {
                        DPoint3d EcoPoint = EcoPObject.mObjectList[i];

                        switch (AttributeIndex)
                        {
                            case 0:
                                attribValString = Ecotect.Requester("get.object.attr1 " + EcoPObject.EcoObjectID[i]);
                                break;
                            case 1:
                                attribValString = Ecotect.Requester("get.object.attr2 " + EcoPObject.EcoObjectID[i]);
                                break;
                            case 2:
                                attribValString = Ecotect.Requester("get.object.attr3 " + EcoPObject.EcoObjectID[i]);
                                break;
                            default:
                                attribValString = Ecotect.Requester("get.object.attr1 " + EcoPObject.EcoObjectID[i]);
                                break;
                        }

                        words = attribValString.Split(delimiterChars);
                        attribVal = double.Parse(words[0]);
                        mValueList[i] = attribVal;

                        ecoObjID++;
                    }

                    break;

                case EcotectObjectType.PointGrid:

                    EcoPObject = (EcotectPointObject)EcoObject;

                    mValueGrid = new double[EcoPObject.NoOfObjectsInGrid[0]][];

                    for (int i = 0; i < EcoPObject.NoOfObjectsInGrid[0]; i++)
                    {
                        mValueGrid[i] = new double[EcoPObject.NoOfObjectsInGrid[1]];

                        for (int j = 0; j < EcoPObject.NoOfObjectsInGrid[1]; j++)
                        {

                            DPoint3d EcoPoint = EcoPObject.mObjectGrid[i][j];

                            switch (AttributeIndex)
                            {
                                case 0:
                                    attribValString = Ecotect.Requester("get.object.attr1 " + EcoPObject.EcoGridObjectID[i][j]);
                                    break;
                                case 1:
                                    attribValString = Ecotect.Requester("get.object.attr2 " + EcoPObject.EcoGridObjectID[i][j]);
                                    break;
                                case 2:
                                    attribValString = Ecotect.Requester("get.object.attr3 " + EcoPObject.EcoGridObjectID[i][j]);
                                    break;
                                default:
                                    attribValString = Ecotect.Requester("get.object.attr1 " + EcoPObject.EcoGridObjectID[i][j]);
                                    break;
                            }

                            words = attribValString.Split(delimiterChars);
                            attribVal = double.Parse(words[0]);

                            mValueGrid[i][j] = attribVal;

                            ecoObjID++;
                        }
                    }
                    break;

                case EcotectObjectType.Unknown:
                    break;
            } //switch

            this.ConstructionsVisible = false;

        }//method

#endregion update helpers

    }
}