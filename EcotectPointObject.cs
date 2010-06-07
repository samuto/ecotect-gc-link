/*
 * EcotectPointObject class
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
using Bentley.GenerativeComponents.MicroStation;  // added to make GeometryTools work
using Bentley.Geometry;
using Bentley.GenerativeComponents.GCScript;
using Bentley.GenerativeComponents.Features;
using Bentley.GenerativeComponents.Features.Specific;
using System.Threading;

namespace Bentley.GenerativeComponents.Features
{
    /// <summary>Construct Ecotect Point Object</summary>

    public class EcotectPointObject : Feature
    {
        #region fields
        //point list containers
        public DPoint3d[]                       mObjectList;
        private int[]                           mObjectID;
        private bool[]                          mPropertyHasChanged;

        //point grid containers
        public DPoint3d[][]                     mObjectGrid;
        private int[][]                         mObjectIDGrid;
        private bool[][]                        mPropertyHasChangedGrid;

        //general
        //private int                           mZoneID = 0;
        private Bentley.GenerativeComponents.Features.Specific.TextStyle mTextStyle;
        private EcotectObjectType               mObjectType = EcotectObjectType.Unknown;
        public bool                             mEcotectRequiresEval;

        #endregion

        #region constructors

        public EcotectPointObject
        (
        )
            : base()
        {
        }

        public EcotectPointObject
        (
        Feature parentFeature
        )
            : base(parentFeature)
        {
        }

        protected override Feature NewInstance() { return new EcotectPointObject(this); }

        #endregion

        #region properties

        public Bentley.GenerativeComponents.Features.Specific.TextStyle TextStyle
        {
            get { return mTextStyle; }
            set { mTextStyle = value; }
        }

        public bool EcotectRequiresEval
        {
            get { return mEcotectRequiresEval; }
            set { mEcotectRequiresEval = value; }
        }

        public int[] EcoObjectID
        {
            get { return mObjectID; }
            set { mObjectID = value; }
        }

        public int[][] EcoGridObjectID
        {
            get { return mObjectIDGrid; }
            set { mObjectIDGrid = value; }
        }

        public EcotectObjectType EcoObjectType
        {
            get { return mObjectType; }
            set { mObjectType = value; }
        }

        public int NoOfObjectsInList
        {
            get { return mObjectID.Length; }
        }

        public int[] NoOfObjectsInGrid
        {
            get { return new int[] { mObjectIDGrid.GetLength(0), mObjectIDGrid[0].Length }; }
        }

        public DPoint3d CentroidAsDPoint3D(Point pp)
        {
            return pp.DPoint3d;
        }

        #endregion

        #region update method for point list

        [Update]
        public virtual bool EcotectObjectFromPointList
        (
        FeatureUpdateContext updateContext,
        [ParentModel]Point[] PointList,
        [DefaultExpression("true")] bool ShowMarker,
        [DefaultExpression("10.0")] double MarkerSize,
        [DefaultExpression("null")]Bentley.GenerativeComponents.Features.Specific.TextStyle TxtStyle,
        [Out] ref int[] ObjectID
        )
        {
            this.UpdateDeferral = FeatureUpdateDeferralOption.DuringDynamics;

            ResetObjectPropertyChangeFlags(PointList);

            if (HasObjectsChanged(PointList) || (updateContext.UpdatePurpose == GraphUpdatePurpose.UpdateAll))
            {
                #region Ecotect Create Object Pass

                mObjectList = new DPoint3d[PointList.Length];

                for (int i = 0; i < PointList.Length; i++)
                {
                    mObjectList[i] = new DPoint3d(PointList[i].X, PointList[i].Y, PointList[i].Z);
                }

                //if update all has occured the entire project has been cleaned already
                if (updateContext.UpdatePurpose != GraphUpdatePurpose.UpdateAll) RemoveAllObjects();

                mObjectID = new int[PointList.Length];

                //set to right zone
                //Ecotect.Executer("set.zone.current " + mZoneID);

                //bake the cake 

                string ecoObjIndexString = string.Empty;
                char[] delimiterChars = { '\0', ',', '\"' };
                string[] words;

                for (int i = 0; i < mObjectList.Length; i++)
                {
                    ecoObjIndexString = Ecotect.Requester("add.object 9 1"); //return a large string 
                    words = ecoObjIndexString.Split(delimiterChars); //remove the '\0' or null escape sequences
                    mObjectID[i] = int.Parse(words[0]);//select the first word

                    Ecotect.Requester("add.node " + mObjectID[i] + " 0 " + mObjectList[i].X + " " + mObjectList[i].Y + " " + mObjectList[i].Z);

                    Ecotect.Executer("object.done");

                    try
                    {
                        //set object to zone
                        //Ecotect.Executer("set.object.zone " + mObjectID[i] + " " + mZoneID);
                    }
                    catch (Exception ex)
                    {
                        throw new GCException(ex.Message);

                    }
                }
                mEcotectRequiresEval = true;
                #endregion Ecotect Critical Change Pass
            }
            else if (HasObjectPropertiesChanged(PointList))
            {
                #region Ecotect Object Modification Pass
                for (int i = 0; i < PointList.Length; i++)
                {
                    if (mPropertyHasChanged[i])
                    {
                        mObjectList[i] = new DPoint3d(PointList[i].X, PointList[i].Y, PointList[i].Z);
                        Ecotect.Executer("set.object.center " + mObjectID[i] + " " + PointList[i].X + " " + PointList[i].Y + " " + PointList[i].Z);
                    }
                }

                mEcotectRequiresEval = true;
                #endregion
            }

            #region Apply User Edits to Feature (Ecotect Update not required)
            if (TxtStyle != mTextStyle)
            {
                mTextStyle = TxtStyle;
            }
            #endregion

            //out vars
            ObjectID = mObjectID;
            mObjectType = EcotectObjectType.PointList;
            this.SymbologyAndLevelUsage = SymbologyAndLevelUsageOption.AssignToElement;
            Element[] elementArray = (ShowMarker) ? listOfDiplayableElements(PointList, MarkerSize) :null;
            Point3d origin = MSApp.Point3dFromXYZ(0,0,0);
            SetElement(MSApp.CreateCellElement1("normals",ref elementArray , ref origin,false));
            return true;
        }

        #region display

        Element[] listOfDiplayableElements(Point[] listOfPoints, double markerSize)
        {
            //init display list
            List<Element> listOfDisplayableElements = new List<Element>();

            foreach (Point p in listOfPoints)
            {
                //create matrix
                Matrix3d mat = MSApp.Matrix3dFromAxisAndRotationAngle(0,0);
                Point3d p3d = MSApp.Point3dFromXYZ(p.X, p.Y, p.Z);
                //create ellipse element
                Element ell = MSApp.CreateEllipseElement2(TemplateElement, ref p3d, markerSize,markerSize, ref mat, MsdFillMode.Outlined);
                //add to display list
                listOfDisplayableElements.Add(ell);
            }

            return listOfDisplayableElements.ToArray();

        }

        #endregion display

        #region update method helpers

        private bool HasObjectPropertiesChanged(Point[] PointList)
        {
            bool critical = false;

            for (int i = 0; i < PointList.Length; i++)
            {
                if (PointList[i].HasChanged)
                {
                    mPropertyHasChanged[i] = true;
                    critical = true;
                }
                else mPropertyHasChanged[i] = false;
            }

            return critical;
        }

        private bool HasObjectsChanged(Point[] PointList)
        {
            if (mObjectList == null) return true; //first pass
            if (PointList.Length != mObjectList.Length) return true;
            return false;
        }

        private void ResetObjectPropertyChangeFlags(Point[] PointList)
        {
            mPropertyHasChanged = new bool[PointList.Length];
            for (int i = 0; i < PointList.Length; i++)
            {
                mPropertyHasChanged[i] = false;
            }
        }

        private void RemoveAllObjects()
        {
            if (mObjectID != null)
            {
                for (int i = 0; i < mObjectID.Length; i++)
                {
                    //Ecotect.Executer("set.object.selected " + mObjectID[i] + " true");
                    Ecotect.Executer("object.delete " + (mObjectID[i] - i));
                    //(mObjectID[i]- i) to negotiate with ecotects automatic count decrement
                }
                //Ecotect.Executer("selection.delete");
            }
        }

        #endregion

        #endregion

        #region update method for point grid

        [Update]
        public virtual bool EcotectObjectFromPointGrid
        (
        FeatureUpdateContext updateContext,
        [ParentModel] Point[][] PointGrid,
        [DefaultExpression("true")] bool ShowMarker,
        [DefaultExpression("10.0")] double MarkerSize,
        [DefaultExpression("null")]Bentley.GenerativeComponents.Features.Specific.TextStyle TxtStyle,
        [Out] ref int[][] ObjectID
        )
        {
            this.UpdateDeferral = FeatureUpdateDeferralOption.DuringDynamics;

            ResetObjectPropertyEditFlags(PointGrid);

            if (HasObjectsChanged(PointGrid) ||(updateContext.UpdatePurpose == GraphUpdatePurpose.UpdateAll))
            {
                #region Ecotect Create Object Pass

                mObjectGrid = new DPoint3d[PointGrid.Length][];
                for (int i = 0; i < PointGrid.Length; i++)
                {
                    mObjectGrid[i] = new DPoint3d[PointGrid[i].Length];
                    for (int j = 0; j < PointGrid[i].Length; j++)
                    {
                        mObjectGrid[i][j] = new DPoint3d(PointGrid[i][j].X, PointGrid[i][j].Y, PointGrid[i][j].Z);
                    }
                }

                //if UpdateAll , the zones have been deleted already, along with the objects
                if (updateContext.UpdatePurpose != GraphUpdatePurpose.UpdateAll) RemoveAllGridObjects();

                //set to right zone
                //Ecotect.Executer("set.zone.current " + mZoneID);

                mObjectIDGrid = new int[mObjectGrid.GetLength(0)][];
                for (int i = 0; i < mObjectGrid.GetLength(0); i++)
                {
                    mObjectIDGrid[i] = new int[mObjectGrid[i].Length];
                }

                //bake the cake 
                string ecoObjIndexString = string.Empty;
                char[] delimiterChars = { '\0', ',', '\"' };
                string[] words;

                for (int i = 0; i < mObjectGrid.GetLength(0); i++)
                {
                    for (int j = 0; j < mObjectGrid[i].Length; j++)
                    {
                        ecoObjIndexString = Ecotect.Requester("add.object 9 1"); //return a large string 
                        words = ecoObjIndexString.Split(delimiterChars); //remove the '\0' or null escape sequences
                        mObjectIDGrid[i][j] = int.Parse(words[0]);//select the first word

                        Ecotect.Requester("add.node " + mObjectIDGrid[i][j] + " 0 " + mObjectGrid[i][j].X + " " + mObjectGrid[i][j].Y + " " + mObjectGrid[i][j].Z);

                        Ecotect.Executer("object.done");

                        try
                        {
                            //set object to zone
                            //Ecotect.Executer("set.object.zone " + mObjectIDGrid[i][j] + " " + mZoneID);
                            //mZoneName = EcotectZoneList.ZoneNameFromID(mZoneID);
                        }
                        catch (Exception ex)
                        {
                            throw new GCException(ex.Message);

                        }
                    }
                }

                mEcotectRequiresEval = true;
                #endregion
            }
            else if (HasObjectsPropertiesChanged(PointGrid))
            {
                #region Ecotect Object Modification Pass

                for (int i = 0; i < mObjectList.GetLength(0); i++)
                {
                    for (int j = 0; j < mObjectGrid[i].Length; j++)
                    {
                        if (mPropertyHasChangedGrid[i][j])
                        {
                            mObjectGrid[i][j] = new DPoint3d(PointGrid[i][j].X, PointGrid[i][j].Y, PointGrid[i][j].Z);
                            Ecotect.Executer("set.object.center " + mObjectIDGrid[i][j] + " " + PointGrid[i][j].X + " " + PointGrid[i][j].Y + " " + PointGrid[i][j].Z);
                        }
                    }
                }

                mEcotectRequiresEval = true;

                #endregion
            }

            #region Apply User Edits to Feature (Ecotect Update not required)

            if (TxtStyle != mTextStyle)
            {
                mTextStyle = TxtStyle;
            }

            #endregion

            ObjectID = mObjectIDGrid;
            mObjectType = EcotectObjectType.PointGrid;
            this.SymbologyAndLevelUsage = SymbologyAndLevelUsageOption.AssignToElement;
            Element[] elementArray = (ShowMarker) ? listOfDiplayableElements(PointGrid, MarkerSize) :null;
            Point3d origin = MSApp.Point3dFromXYZ(0,0,0);
            SetElement(MSApp.CreateCellElement1("normals",ref elementArray , ref origin,false));
            return true;
        }


        #region display
        Element[] listOfDiplayableElements(Point[][] listOflistOfPoints, double markerSize)
        {
            //init display list
            List<Element> listOfDisplayableElements = new List<Element>();

            foreach (Point[] listOfPoints in listOflistOfPoints)
            {
                foreach (Point p in listOfPoints)
                {
                    //create matrix
                    Matrix3d mat = MSApp.Matrix3dFromAxisAndRotationAngle(0, 0);
                    Point3d p3d = MSApp.Point3dFromXYZ(p.X, p.Y, p.Z);
                    //create ellipse element
                    Element ell = MSApp.CreateEllipseElement2(TemplateElement, ref p3d, markerSize, markerSize, ref mat, MsdFillMode.Outlined);
                    //add to display list
                    listOfDisplayableElements.Add(ell);
                }
            }

            return listOfDisplayableElements.ToArray();
        }

        #endregion display


        #region update method helpers

        private bool HasObjectsPropertiesChanged(Point[][] PointGrid)
        {
            bool critical = false;

            for (int i = 0; i < PointGrid.GetLength(0); i++)
            {
                for (int j = 0; j < PointGrid[i].Length; j++)
                {
                    if (PointGrid[i][j].HasChanged)
                    {
                        mPropertyHasChangedGrid[i][j] = true;
                        critical = true;
                    }
                    else mPropertyHasChangedGrid[i][j] = false;
                }
            }
            return critical;
        }

        private bool HasObjectsChanged(Point[][] PointGrid)
        {
            if (mObjectGrid == null) return true;
            if (PointGrid.GetLength(0) != mObjectGrid.GetLength(0)) return true;
            for (int i = 0; i < PointGrid.GetLength(0); i++)
            {
                if (PointGrid[i].Length != mObjectGrid[i].Length) return true;
            }
            return false;
        }

        private void ResetObjectPropertyEditFlags(Point[][] PointGrid)
        {
            mPropertyHasChangedGrid = new bool[PointGrid.Length][];
            for (int i = 0; i < PointGrid.Length; i++)
            {
                mPropertyHasChangedGrid[i] = new bool[PointGrid[i].Length];
                for(int j = 0; j < PointGrid[i].Length; j++)
                {
                mPropertyHasChangedGrid[i][j] = false;
                }
            }
        }

        private void RemoveAllGridObjects()
        {
            int delIndex = 0; //to negotiate with ecotects automatic count decrement

            if (mObjectIDGrid != null)
            {
                for (int i = 0; i < mObjectIDGrid.GetLength(0); i++)
                {
                    for (int j = 0; j < mObjectIDGrid[i].Length; j++)
                    {
                        //Ecotect.Executer("set.object.selected " + mGridObjectID[i][j] + " true");
                        Ecotect.Executer("object.delete " + (mObjectIDGrid[i][j] - delIndex));
                        delIndex++;
                    }
                }
                //Ecotect.Executer("selection.delete");
            }
        }

        #endregion

        #endregion
    }
}
