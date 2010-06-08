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
 * EcotectProject class
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
using System.Threading;
using System.IO;
using System.Windows.Forms;

namespace Bentley.GenerativeComponents.Features
{
    public class EcotectProject : Feature
    {
        private static string mProjectTitle = string.Empty;
        private static string mClientInfo = string.Empty;
        private static double mNorthClockwiseOffset = 0;
        private static double mLengthOfNorthLine = 0;
        private static double mAltitude = 0;
        private static EcotectTerrain mLocalTerrain;
        private static EcotectWeatherFile mWeatherData;
        private static EcotectBuildingType mBuildingType;

        private static bool[] mEditHasOcccured = new bool[8] { false, false, false,false, false, false, false, false };

        private char[] delimiterChars = { '\0', ',', '\"' };
        private string[] words;


        public EcotectProject
        (
        )
            : base()
        {
        }

        public EcotectProject
        (
        Feature parentFeature
        )
            : base(parentFeature)
        {
        }

        /// <categories>Project</categories>
        [Update]
        public bool ByTitle
        (
        FeatureUpdateContext updateContext,
        [ParentModel]string ProjectTitle,
        [SpecializedExpressionBuilder("ProjectSettingsWizard")]string ClientInfo,
        [DefaultExpression("0")] double NorthClockwiseOffset,
        [DefaultExpression("100.0")] double LengthOfNorthLine,
        [DefaultExpression("0")] double Altitude,
        [DefaultExpression("EcotectTerrain.Urban")] EcotectTerrain LocalTerrain,
        [DefaultExpression("EcotectWeatherFile.UK_LondonE")] EcotectWeatherFile WeatherData,
        [DefaultExpression("EcotectBuildingType.Unknown")] EcotectBuildingType BuildingType
        )
        {

            if (mProjectTitle != ProjectTitle || updateContext.UpdatePurpose == GraphUpdatePurpose.TransactionFileGraphChangeTransaction)
            {
                try
                {
                    //minimize ecotect
                    Ecotect.Executer("app.minimise true");

                    //Delete all objects
                    Ecotect.Executer("select.all 0");
                    Ecotect.Executer("selection.delete");

                    //Delete all zones
                    bool z = true;

                    while (z)
                    {
                        string tempZoneIdString = Ecotect.Requester("get.zone.name 1");
                        words = tempZoneIdString.Split(delimiterChars);
                        if (words[0] == "<<Varies>>" || words[1] == "<<Varies>>") z = false;
                        else
                        {
                            Ecotect.Executer("zone.delete 1");
                        }
                    }
                }
                catch (Exception e)
                {
                    if (e.Message == "Index was outside the bounds of the array")
                        throw new GCException("ERROR - \'ECOTECT\' rejected request. Index was outside the bounds of the array");
                }

                DisplayNorthLine(NorthClockwiseOffset,LengthOfNorthLine);

                mEditHasOcccured = new bool[8] { true, true, true, true, true, true, true, true};

            }
            else
            {

                mEditHasOcccured = CheckIfUserEditHasOccured(ProjectTitle, ClientInfo, NorthClockwiseOffset, LengthOfNorthLine, Altitude, LocalTerrain, WeatherData, BuildingType);
            }

            if (mEditHasOcccured[0]) Ecotect.Executer("set.project.title " + ProjectTitle);

            if (mEditHasOcccured[1]) Ecotect.Executer("set.project.client " + ClientInfo);

            if (mEditHasOcccured[2])
            {
                Ecotect.Executer("set.project.north " + NorthClockwiseOffset.ToString());
                DisplayNorthLine(NorthClockwiseOffset,LengthOfNorthLine);
            }

            if (mEditHasOcccured[3])
            {
                DisplayNorthLine(NorthClockwiseOffset, LengthOfNorthLine);
            }

            if (mEditHasOcccured[4]) Ecotect.Executer("set.project.altitude " + Altitude.ToString());

            if (mEditHasOcccured[5]) Ecotect.Executer("set.project.terrain " + LocalTerrain.ToString());

            if (mEditHasOcccured[6])
            {
                string fileName = Enum.GetName(typeof(EcotectWeatherFile), (EcotectWeatherFile)WeatherData);

                fileName = fileName.Replace("_", "-");

                fileName = fileName + ".wea";

                Ecotect.Executer("weather.load " + fileName);
            }

            if (mEditHasOcccured[7]) Ecotect.Executer("set.project.type " + ((int)BuildingType).ToString());

            UpdateStaticFields(ProjectTitle, ClientInfo, NorthClockwiseOffset, LengthOfNorthLine, Altitude, LocalTerrain, WeatherData, BuildingType);

            this.SymbologyAndLevelUsage = SymbologyAndLevelUsageOption.AssignToElement;

            return true;
        }

        private bool[] CheckIfUserEditHasOccured(string projectTitle,
                                                    string clientInfo,
                                                    double northClockwiseOffest,
            double lengthOfNorthLine,
                                                    double altitude,
                                                    EcotectTerrain localTerrain,
                                                    EcotectWeatherFile weatherData,
                                                    EcotectBuildingType buildingType
                                                    )
        {
            bool[] editHasOccured = new bool[8] { false, false, false,false, false, false, false, false };

            if (projectTitle != mProjectTitle) editHasOccured[0] = true;
            if (clientInfo != mClientInfo) editHasOccured[1] = true;
            if (northClockwiseOffest != mNorthClockwiseOffset) editHasOccured[2] = true;
            if (lengthOfNorthLine != mLengthOfNorthLine) editHasOccured[3] = true;
            if (altitude != mAltitude) editHasOccured[4] = true;
            if (localTerrain != mLocalTerrain) editHasOccured[5] = true;
            if (weatherData != mWeatherData) editHasOccured[6] = true;
            if (buildingType != mBuildingType) editHasOccured[7] = true;

            return editHasOccured;
        }

        private void UpdateStaticFields(
                                        string projectTitle,
                                        string clientInfo,
                                        double northClockwiseOffest,
            double lengthOfNorthLine,
                                        double altitude,
                                        EcotectTerrain localTerrain,
                                        EcotectWeatherFile weatherData,
                                        EcotectBuildingType buildingType
                                         )
        {
            mProjectTitle = projectTitle;
            mClientInfo = clientInfo;
            mNorthClockwiseOffset = northClockwiseOffest;
            mLengthOfNorthLine = lengthOfNorthLine;
            mAltitude = altitude;
            mLocalTerrain = localTerrain;
            mWeatherData = weatherData;
            mBuildingType = buildingType;

            ResetEditHasOccuredFlag();
        }

        private void ResetEditHasOccuredFlag()
        {
            mEditHasOcccured = new bool[] { false, false, false, false,false, false, false, false };
        }

        private void DisplayNorthLine(double clockwiseOffset, double length)
        {
            double factor = (2*Math.PI/360);
            double rad = length;
            double xCood = rad * Math.Cos((clockwiseOffset-90)*factor);
            double yCood = -rad * Math.Sin((clockwiseOffset - 90) * factor);

            double xArrow1 = (rad *0.8) * Math.Cos((clockwiseOffset -90 - 10)*factor);
            double yArrow1 = -(rad * 0.8) * Math.Sin((clockwiseOffset - 90 - 10) * factor);

            double xArrow2 = (rad * 0.8) * Math.Cos((clockwiseOffset - 90 + 10) * factor);
            double yArrow2 = -(rad * 0.8) * Math.Sin((clockwiseOffset - 90 + 10) * factor);

            
            Point3d origin = MSApp.Point3dFromXYZ(0, 0, 0);

            Point3d[] mainLine = new Point3d[2];
            mainLine[0] = MSApp.Point3dFromXYZ(origin.X + 0,origin.Y + 0, 0);
            mainLine[1] = MSApp.Point3dFromXYZ(origin.X + xCood, origin.Y + yCood, 0);


            Point3d[] arrowLine1 = new Point3d[2];
            arrowLine1[0] = MSApp.Point3dFromXYZ(origin.X + xCood, origin.Y + yCood, 0);
            arrowLine1[1] = MSApp.Point3dFromXYZ(origin.X + xArrow1, origin.Y + yArrow1, 0);
            
            Point3d[] arrowLine2 = new Point3d[2];
            arrowLine2[0] = MSApp.Point3dFromXYZ(origin.X + xCood, origin.Y + yCood, 0);
            arrowLine2[1] = MSApp.Point3dFromXYZ(origin.X + xArrow2, origin.Y + yArrow2, 0);

            Point3d textPt = new Point3d();
            textPt = MSApp.Point3dFromXYZ((origin.X + xCood)/2, (origin.Y + yCood)/2, 0);

            Element[] oDisplayElements = new Element[4];

            oDisplayElements[0] = (Element)MSApp.CreateLineElement1(TemplateElement,ref mainLine);
            oDisplayElements[1] = (Element)MSApp.CreateLineElement1(TemplateElement,ref arrowLine1);
            oDisplayElements[2] = (Element)MSApp.CreateLineElement1(TemplateElement,ref arrowLine2);

            Matrix3d mat = new Matrix3d();
            mat = MSApp.Matrix3dFromAxisAndRotationAngle(0,0);
            oDisplayElements[3] = (Element)MSApp.CreateTextElement1(TemplateElement, "N",ref textPt,ref mat);

            SetElement(MSApp.CreateCellElement1(" ", ref oDisplayElements, ref origin, false));
        }
    }
}
