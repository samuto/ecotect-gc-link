/*
 * EcotectPolygonObject class
 * version 09.06.26
 * author: Kaustuv DeBiswas
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
    /// <summary>Ways to construct Ecotect Polygon Object</summary>

    public struct LightPolygon
    {
        public DPoint3d[] verts;

        public LightPolygon(DPoint3d a, DPoint3d b, DPoint3d c)
        {
            verts = new DPoint3d[3] { a, b, c };
        }
    }

    public class EcotectPolygonObject : Feature
    {
        #region field

        public LightPolygon[]                       mObjectList;
        private int[]                               mObjectID;
        private bool[]                              mPropertyHasChanged;

        public LightPolygon[][]                     mObjectGrid;
        private int[][]                             mObjectIDGrid;
        private bool[][]                            mPropertyHasChangedGrid;

        private EcotectMaterial                     mMaterial;
        private bool                                mReverseNormal;
        public Bentley.GenerativeComponents.Features.Specific.TextStyle mTextStyle;
        private EcotectObjectType                   mObjectType = EcotectObjectType.Unknown;
        //private int                               mZoneID = 0;

        public bool                                 mEcotectRequiresEval = false;

        private string ecoObjIndexString = string.Empty;
        private char[] delimiterChars = { '\0', ',', '\"' };
        private string[] words;

        #endregion

        #region  properties

        public EcotectMaterial MaterialProperty
        {
            get { return mMaterial; }
            set { mMaterial = value; }
        } 
        public bool ReverseNormalProperty
        {
            get { return mReverseNormal; }
            set { mReverseNormal = value; }
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
        public int NoOfObjectsInList
        {
            get { return mObjectID.Length; }
        }      
        public int[] NoOfObjectsInGrid
        {
            get { return new int[] { mObjectIDGrid.GetLength(0), mObjectIDGrid[0].Length }; }
        }
        public EcotectObjectType EcoObjectType
        {
            get { return mObjectType; }
            set { mObjectType = value; }
        }

        #endregion

        #region constructors

        public EcotectPolygonObject
        (
        ): base()
        {
        }

        public EcotectPolygonObject
        (
        Feature parentFeature
        ): base(parentFeature)
        {
        }

        #endregion

        protected override Feature NewInstance() { return new EcotectPolygonObject(this); }

        public DPoint3d CentroidAsDPoint3D(Polygon pp)
        {
            double x = 0, y = 0, z = 0;
            DPoint3d[] vert = pp.VerticesAsDPoint3ds;
            for (int i = 0; i < vert.Length; i++)
            {
                x += vert[i].X;
                y += vert[i].Y;
                z += vert[i].Z;
            }
            x = x / vert.Length;
            y = y / vert.Length;
            z = z / vert.Length;

            return new DPoint3d(x, y, z);
        }
        public Point3d CentroidAsPoint3D(Polygon pp)
        {
            double x = 0, y = 0, z = 0;
            DPoint3d[] vert = pp.VerticesAsDPoint3ds;
            for (int i = 0; i < vert.Length; i++)
            {
                x += vert[i].X;
                y += vert[i].Y;
                z += vert[i].Z;
            }
            x = x / vert.Length;
            y = y / vert.Length;
            z = z / vert.Length;
            return MSApp.Point3dFromXYZ(x, y, z);
        }
        public DPoint3d CentroidAsDPoint3D(LightPolygon pp)
        {
            double x = 0, y = 0, z = 0;
            DPoint3d[] vert = pp.verts;
            for (int i = 0; i < vert.Length; i++)
            {
                x += vert[i].X;
                y += vert[i].Y;
                z += vert[i].Z;
            }
            x = x / vert.Length;
            y = y / vert.Length;
            z = z / vert.Length;

            return new DPoint3d(x, y, z);
        }

        #region update by polygonlist

        [Update]
        public virtual bool EcotectObjectFromPolygonList
        (
        FeatureUpdateContext updateContext,
        [ParentModel]Polygon[] PolygonList,
        [DefaultExpression("EcotectMaterial.ConcBlockPlaster")]EcotectMaterial Material,
        [DefaultExpression("false")]  bool ReverseNormals,
        [DefaultExpression("100")] double LengthOfNormals,
        [DefaultExpression("true")] bool ShowNormals,
        [DefaultExpression("null")]Bentley.GenerativeComponents.Features.Specific.TextStyle TxtStyle,
        [Out] ref int[] ObjectID
        )
        {
            this.UpdateDeferral = FeatureUpdateDeferralOption.DuringDynamics;

            ResetAllPolygonPropertyEditFlags(PolygonList);

            if (HasNumOfPolygonsChangedOrNormalsReversed(PolygonList, ReverseNormals))
            {
                #region Ecotect Create Object Pass

                //clean up the objects from ecotect
                RemoveAllPolygons();

                mObjectList = new LightPolygon[PolygonList.Length];

                for (int i = 0; i < PolygonList.Length; i++)
                {
                    DPoint3d a = new DPoint3d(PolygonList[i].Vertices[0].X, PolygonList[i].Vertices[0].Y, PolygonList[i].Vertices[0].Z);
                    DPoint3d b = new DPoint3d(PolygonList[i].Vertices[1].X, PolygonList[i].Vertices[1].Y, PolygonList[i].Vertices[1].Z);
                    DPoint3d c = new DPoint3d(PolygonList[i].Vertices[2].X, PolygonList[i].Vertices[2].Y, PolygonList[i].Vertices[2].Z);
                    mObjectList[i] = new LightPolygon(a, b, c);
                }

                mObjectID = new int[PolygonList.GetLength(0)];

                //set to right zone (currently zones are not used - everything is dumped into a default zone)
                //Ecotect.Executer("set.zone.current " + ZoneID);

                //Add a new set of polygons
                for (int i = 0; i < PolygonList.Length; i++)
                {
                    ecoObjIndexString = Ecotect.Requester("add.object 7 12"); //return a large string 
                    words = ecoObjIndexString.Split(delimiterChars); //remove the '\0' or null escape sequences
                    mObjectID[i] = int.Parse(words[0]);//select the first word

                    DPoint3d[] vertices = PolygonList[i].VerticesAsDPoint3ds;

                    int ecoNodeID = 0;

                    if (!ReverseNormals)
                    {
                        for (int count = 0; count < vertices.Length; count++)
                        {
                            Ecotect.Requester("add.node " + mObjectID[i] + " " + ecoNodeID + " " + vertices[count].X + " " + vertices[count].Y + " " + vertices[count].Z);
                            ecoNodeID++;
                        }
                    }
                    else
                    {
                        for (int count = vertices.Length - 1; count >= 0; count--)
                        {
                            Ecotect.Requester("add.node " + mObjectID[i] + " " + ecoNodeID + " " + vertices[count].X + " " + vertices[count].Y + " " + vertices[count].Z);
                            ecoNodeID++;
                        }
                    }

                    ecoNodeID = 0;
                    Ecotect.Executer("object.done");


                    //try
                    //{
                    //    set object to zone (currently zones are not used - everything is dumped into a default zone)
                    //    Ecotect.Executer("set.object.zone " + mObjectID[i] + " " + mZoneID);
                    //}
                    //catch (Exception ex)
                    //{
                    //    throw new GCException(ex.Message);
                    //}

                    //set object material
                    string matName = Enum.GetName(typeof(EcotectMaterial), (EcotectMaterial)Material);
                    string ecoMatIndexString = Ecotect.Requester("get.material.index " + matName); //return a large string 
                    words = ecoMatIndexString.Split(delimiterChars); //remove the '\0' or null escape sequences
                    int ecoMatIndex = int.Parse(words[0]);//select the first word
                    Ecotect.Executer("set.object.material " + mObjectID[i] + " " + ecoMatIndex);
                }

                mEcotectRequiresEval = true;
                #endregion
            }
            else if (HavePolygonPropertiesChanged(PolygonList))
            {
                #region Ecotect Object Modification Pass

                for (int i = 0; i < PolygonList.Length; i++)
                {
                    if (mPropertyHasChanged[i])
                    {

                        DPoint3d a = new DPoint3d(PolygonList[i].Vertices[0].X, PolygonList[i].Vertices[0].Y, PolygonList[i].Vertices[0].Z);
                        DPoint3d b = new DPoint3d(PolygonList[i].Vertices[1].X, PolygonList[i].Vertices[1].Y, PolygonList[i].Vertices[1].Z);
                        DPoint3d c = new DPoint3d(PolygonList[i].Vertices[2].X, PolygonList[i].Vertices[2].Y, PolygonList[i].Vertices[2].Z);
                        mObjectList[i] = new LightPolygon(a, b, c);

                        Ecotect.Executer("set.object.node.position " + mObjectID[i] + " 0 " + a.X + " " + a.Y + " " + a.Z);
                        Ecotect.Executer("set.object.node.position " + mObjectID[i] + " 1 " + b.X + " " + b.Y + " " + b.Z);
                        Ecotect.Executer("set.object.node.position " + mObjectID[i] + " 2 " + c.X + " " + c.Y + " " + c.Z);
                    }
                }

                mEcotectRequiresEval = true;

                #endregion
            }

            #region Apply User Edits to Feature (Ecotect Update not required)

            if (mMaterial != Material)
            {
                for (int i = 0; i < PolygonList.Length; i++)
                {
                    //Material
                    string matName = Enum.GetName(typeof(EcotectMaterial), (EcotectMaterial)Material);
                    string ecoMatIndexString = Ecotect.Requester("get.material.index " + matName); //return a large string 
                    words = ecoMatIndexString.Split(delimiterChars); //remove the '\0' or null escape sequences
                    int ecoMatIndex = int.Parse(words[0]);//select the first word
                    Ecotect.Executer("set.object.material " + mObjectID[i] + " " + ecoMatIndex);
                }
            }

            if (mTextStyle != TxtStyle || TxtStyle.HasChanged)
            {
                mTextStyle = TxtStyle;
            }

            mReverseNormal = ReverseNormals;

            #endregion

            //out vars
            ObjectID = mObjectID;
            mObjectType = EcotectObjectType.PolygonList;
            this.SymbologyAndLevelUsage = SymbologyAndLevelUsageOption.AssignToElement;
            Element[] elementArray = (ShowNormals) ? listOfDiplayableElements(PolygonList, LengthOfNormals, ReverseNormals) :null;
            Point3d origin = MSApp.Point3dFromXYZ(0,0,0);
            SetElement(MSApp.CreateCellElement1("normals",ref elementArray , ref origin,false));
            return true;
        }

        #region display
        Element[] listOfDiplayableElements(Polygon[] listOfPolygons, double lengthOfNormals, bool reverse)
        {
            //init display list
            List<Element> listOfDisplayableElements = new List<Element>();

            foreach (Polygon poly in listOfPolygons)
            {
                //find centroid
                Point3d centroid = CentroidAsPoint3D(poly);
                Point3d v0 = MSApp.Point3dFromXYZ(poly.VerticesAsDPoint3ds[0].X,poly.VerticesAsDPoint3ds[0].Y,poly.VerticesAsDPoint3ds[0].Z);
                Point3d v1 = MSApp.Point3dFromXYZ(poly.VerticesAsDPoint3ds[1].X,poly.VerticesAsDPoint3ds[1].Y,poly.VerticesAsDPoint3ds[1].Z);
                Point3d v2 = MSApp.Point3dFromXYZ(poly.VerticesAsDPoint3ds[2].X,poly.VerticesAsDPoint3ds[2].Y,poly.VerticesAsDPoint3ds[2].Z);
                //normal
                Vector3d refDirection1, refDirection2 ;
                if (!reverse)
                {
                    refDirection1 = MSApp.Vector3dSubtractPoint3dPoint3d(ref centroid, ref v0);
                    refDirection2 = MSApp.Vector3dSubtractPoint3dPoint3d(ref centroid, ref v1);
                }
                else
                {
                    refDirection1 = MSApp.Vector3dSubtractPoint3dPoint3d(ref centroid, ref v1);
                    refDirection2 = MSApp.Vector3dSubtractPoint3dPoint3d(ref centroid, ref v0);
                }

                Vector3d crossDirection = MSApp.Vector3dCrossProduct(ref refDirection1, ref refDirection2);
                Vector3d normalizedCrossDirection = MSApp.Vector3dNormalize(ref crossDirection);
                Vector3d scaledCrossDirection = MSApp.Vector3dScale(ref normalizedCrossDirection,lengthOfNormals);
                Point3d NormalVector = MSApp.Point3dFromVector3d(ref scaledCrossDirection);
                Point3d NormalPt = MSApp.Point3dAdd(ref centroid, ref NormalVector);
                //create line element
                Element norm = MSApp.CreateLineElement2(TemplateElement, ref centroid, ref NormalPt);
                //add to display list
                listOfDisplayableElements.Add(norm);
            }

            return listOfDisplayableElements.ToArray();

        }

        #endregion display

        #region update helpers

        private bool HasNumOfPolygonsChangedOrNormalsReversed(Polygon[] PolygonList, bool reverseNormal)
        {
            if (mObjectList == null) return true;
            if (PolygonList.Length != mObjectList.Length) return true;
            if (mReverseNormal != reverseNormal) return true;
            return false;
        }

        private bool HavePolygonPropertiesChanged(Polygon[] PolygonList)
        {
            bool critical = false;

            for (int i = 0; i < PolygonList.Length; i++)
            {
                if (PolygonList[i].HasChanged)
                {
                    mPropertyHasChanged[i] = true;
                    critical = true;
                }
            }

            return critical;
        }

        private void RemoveAllPolygons()
        {
            if (mObjectID != null)
            {
                for (int i = 0; i < mObjectID.Length; i++)
                {
                    //Ecotect.Executer("set.object.selected " + mObjectID[i] + " true");
                    Ecotect.Executer("object.delete " + (mObjectID[i]- i));
                    //(mObjectID[i]- i) to negotiate with ecotects automatic count decrement
                }
                //Ecotect.Executer("selection.delete");
            }
        }

        private void ResetAllPolygonPropertyEditFlags(Polygon[] PolygonList)
        {
            mPropertyHasChanged = new bool[PolygonList.Length];
            for (int i = 0; i < PolygonList.Length; i++)
            {
                mPropertyHasChanged[i] = false;
            }
        }

        #endregion

        #endregion

        #region update methods polygon grid

        [Update]
        public virtual bool EcotectObjectFromPolygonGrid
        (
        FeatureUpdateContext updateContext,
        [ParentModel] Polygon[][] PolygonGrid,
        [DefaultExpression("EcotectMaterial.ConcBlockPlaster")]EcotectMaterial Material,
        [DefaultExpression("false")]  bool ReverseNormals,
        [DefaultExpression("100")] double LengthOfNormals,
        [DefaultExpression("true")] bool ShowNormals,
        [DefaultExpression("null")]Bentley.GenerativeComponents.Features.Specific.TextStyle TextStyle,
        [Out] ref int[][] ObjectID
        )
        {
            this.UpdateDeferral = FeatureUpdateDeferralOption.DuringDynamics;

            ResetAllPolygonPropertyEditFlags(PolygonGrid);

            if (HasNumOfPolygonsChangedOrNormalsReversed(PolygonGrid, ReverseNormals) || (updateContext.UpdatePurpose == GraphUpdatePurpose.UpdateAll))
            {
                #region Ecotect Create Object Pass

                RemoveAllPolygonGrid();

                mObjectGrid = new LightPolygon[PolygonGrid.Length][];

                for (int i = 0; i < PolygonGrid.Length; i++)
                {
                    mObjectGrid[i] = new LightPolygon[PolygonGrid[i].Length];
                    for (int j = 0; j < PolygonGrid[i].Length; j++)
                    {
                        DPoint3d a = new DPoint3d(PolygonGrid[i][j].Vertices[0].X, PolygonGrid[i][j].Vertices[0].Y, PolygonGrid[i][j].Vertices[0].Z);
                        DPoint3d b = new DPoint3d(PolygonGrid[i][j].Vertices[1].X, PolygonGrid[i][j].Vertices[1].Y, PolygonGrid[i][j].Vertices[1].Z);
                        DPoint3d c = new DPoint3d(PolygonGrid[i][j].Vertices[2].X, PolygonGrid[i][j].Vertices[2].Y, PolygonGrid[i][j].Vertices[2].Z);
                        mObjectGrid[i][j] = new LightPolygon(a, b, c);
                    }
                }

                //if update all has occured the entire project has been cleaned already
                //if (updateContext.UpdatePurpose != GraphUpdatePurpose.UpdateAll) RemoveAllPolygonGrid();

                //set to right zone
                //Ecotect.Executer("set.zone.current " + ZoneID);

                //Add a new set of polygons
                mObjectIDGrid = new int[PolygonGrid.GetLength(0)][];

                for (int i = 0; i < PolygonGrid.GetLength(0); i++)
                {
                    mObjectIDGrid[i] = new int[PolygonGrid[i].Length];
                }

                for (int i = 0; i < PolygonGrid.GetLength(0); i++)
                {
                    for (int j = 0; j < PolygonGrid[i].Length; j++)
                    {
                        ecoObjIndexString = Ecotect.Requester("add.object 7 12"); //return a large string 
                        words = ecoObjIndexString.Split(delimiterChars); //remove the '\0' or null escape sequences
                        mObjectIDGrid[i][j] = int.Parse(words[0]);//select the first word

                        DPoint3d[] vertices = PolygonGrid[i][j].VerticesAsDPoint3ds;

                        if (!ReverseNormals)
                        {
                            for (int count = 0; count < vertices.Length; count++)
                            {
                                {
                                    Ecotect.Requester("add.node " + mObjectIDGrid[i][j] + " 0 " + vertices[count].X + " " + vertices[count].Y + " " + vertices[count].Z);
                                }
                            }
                        }
                        else
                        {
                            for (int count = vertices.Length - 1; count >= 0; count--)
                            {
                                Ecotect.Requester("add.node " + mObjectIDGrid[i][j] + " 0 " + vertices[count].X + " " + vertices[count].Y + " " + vertices[count].Z);
                            }
                        }

                        Ecotect.Executer("object.done");

                        //try
                        //{
                        //    set object to zone
                        //    Ecotect.Executer("set.object.zone " + mObjectIDGrid[i][j] + " " + mZoneID);
                        //}
                        //catch (Exception ex)
                        //{
                        //    throw new GCException(ex.Message);

                        //}
                        //set object material
                        string matName = Enum.GetName(typeof(EcotectMaterial), (EcotectMaterial)Material);
                        string ecoMatIndexString = Ecotect.Requester("get.material.index " + matName); //return a large string 
                        words = ecoMatIndexString.Split(delimiterChars); //remove the '\0' or null escape sequences
                        int ecoMatIndex = int.Parse(words[0]);//select the first word
                        Ecotect.Executer("set.object.material " + mObjectIDGrid[i][j] + " " + ecoMatIndex);

                    }
                }

                mEcotectRequiresEval = true;
                #endregion
            }
            else if (HasObjectPropertiesChanged(PolygonGrid))
            {
                #region Ecotect Object Modification Pass

                for (int i = 0; i < PolygonGrid.GetLength(0); i++)
                {
                    mPropertyHasChangedGrid[i] = new bool[PolygonGrid[i].Length];

                    for (int j = 0; j < PolygonGrid[i].Length; j++)
                    {
                        if (mPropertyHasChangedGrid[i][j] == true)
                        {
                            DPoint3d a = new DPoint3d(PolygonGrid[i][j].Vertices[0].X, PolygonGrid[i][j].Vertices[0].Y, PolygonGrid[i][j].Vertices[0].Z);
                            DPoint3d b = new DPoint3d(PolygonGrid[i][j].Vertices[1].X, PolygonGrid[i][j].Vertices[1].Y, PolygonGrid[i][j].Vertices[1].Z);
                            DPoint3d c = new DPoint3d(PolygonGrid[i][j].Vertices[2].X, PolygonGrid[i][j].Vertices[2].Y, PolygonGrid[i][j].Vertices[2].Z);
                            mObjectGrid[i][j] = new LightPolygon(a, b, c);

                            Ecotect.Executer("set.object.node.position " + mObjectIDGrid[i][j] + " 0 " + a.X + " " + a.Y + " " + a.Z);
                            Ecotect.Executer("set.object.node.position " + mObjectIDGrid[i][j] + " 1 " + b.X + " " + b.Y + " " + b.Z);
                            Ecotect.Executer("set.object.node.position " + mObjectIDGrid[i][j] + " 2 " + c.X + " " + c.Y + " " + c.Z);
                        }
                    }
                }

                mEcotectRequiresEval = true;
                #endregion
            }

            #region Apply User Edits to Feature (Ecotect Update not required)

            if (mMaterial != Material)
            {
                for (int i = 0; i < PolygonGrid.GetLength(0); i++)
                {
                    for (int j = 0; j < PolygonGrid[i].Length; j++)
                    {

                        //Material
                        string matName = Enum.GetName(typeof(EcotectMaterial), (EcotectMaterial)Material);
                        string ecoMatIndexString = Ecotect.Requester("get.material.index " + matName); //return a large string 
                        words = ecoMatIndexString.Split(delimiterChars); //remove the '\0' or null escape sequences
                        int ecoMatIndex = int.Parse(words[0]);//select the first word
                        Ecotect.Executer("set.object.material " + mObjectIDGrid[i][j] + " " + ecoMatIndex);
                    }
                }
            }

            if (mTextStyle != TextStyle)
            {
                mTextStyle = TextStyle;
            }

            mReverseNormal = ReverseNormals;

            #endregion EcotectPass

            ObjectID = mObjectIDGrid;
            mObjectType = EcotectObjectType.PolygonGrid;
            this.SymbologyAndLevelUsage = SymbologyAndLevelUsageOption.AssignToElement;
            Element[] elementArray = (ShowNormals) ? listOfDiplayableElements(PolygonGrid, LengthOfNormals, ReverseNormals) : null;
            Point3d origin = MSApp.Point3dFromXYZ(0, 0, 0);
            SetElement(MSApp.CreateCellElement1("normals", ref elementArray, ref origin, false));
            return true;
        }

        #region display
        Element[] listOfDiplayableElements(Polygon[][] listOflistOfPolygons, double lengthOfNormals, bool reverse)
        {
            //init display list
            List<Element> listOfDisplayableElements = new List<Element>();

            foreach (Polygon[] listOfPoly in listOflistOfPolygons)
            {
                foreach (Polygon poly in listOfPoly)
                {
                    //find centroid
                    Point3d centroid = CentroidAsPoint3D(poly);
                    Point3d v0 = MSApp.Point3dFromXYZ(poly.VerticesAsDPoint3ds[0].X, poly.VerticesAsDPoint3ds[0].Y, poly.VerticesAsDPoint3ds[0].Z);
                    Point3d v1 = MSApp.Point3dFromXYZ(poly.VerticesAsDPoint3ds[1].X, poly.VerticesAsDPoint3ds[1].Y, poly.VerticesAsDPoint3ds[1].Z);
                    Point3d v2 = MSApp.Point3dFromXYZ(poly.VerticesAsDPoint3ds[2].X, poly.VerticesAsDPoint3ds[2].Y, poly.VerticesAsDPoint3ds[2].Z);
                    //normal
                    Vector3d refDirection1, refDirection2;
                    if (!reverse)
                    {
                        refDirection1 = MSApp.Vector3dSubtractPoint3dPoint3d(ref centroid, ref v0);
                        refDirection2 = MSApp.Vector3dSubtractPoint3dPoint3d(ref centroid, ref v1);
                    }
                    else
                    {
                        refDirection1 = MSApp.Vector3dSubtractPoint3dPoint3d(ref centroid, ref v1);
                        refDirection2 = MSApp.Vector3dSubtractPoint3dPoint3d(ref centroid, ref v0);
                    }

                    Vector3d crossDirection = MSApp.Vector3dCrossProduct(ref refDirection1, ref refDirection2);
                    Vector3d normalizedCrossDirection = MSApp.Vector3dNormalize(ref crossDirection);
                    Vector3d scaledCrossDirection = MSApp.Vector3dScale(ref normalizedCrossDirection, lengthOfNormals);
                    Point3d NormalVector = MSApp.Point3dFromVector3d(ref scaledCrossDirection);
                    Point3d NormalPt = MSApp.Point3dAdd(ref centroid, ref NormalVector);
                    //create line element
                    Element norm = MSApp.CreateLineElement2(TemplateElement, ref centroid, ref NormalPt);
                    //add to display list
                    listOfDisplayableElements.Add(norm);
                }
            }
            return listOfDisplayableElements.ToArray();
        }

        #endregion display


        #region update helpers

        private bool HasObjectPropertiesChanged(Polygon[][] PolygonGrid)
        {
            bool critical = false;

            for (int i = 0; i < PolygonGrid.GetLength(0); i++)
            {
                for (int j = 0; j < PolygonGrid[i].Length; j++)
                {
                    if (PolygonGrid[i][j].HasChanged)
                    {
                        mPropertyHasChangedGrid[i][j] = true;
                        critical = true;
                    }
                }
            }
            return critical;
        }

        private void RemoveAllPolygonGrid()
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

        private bool HasNumOfPolygonsChangedOrNormalsReversed(Polygon[][] PolygonGrid, bool reverseNormal)
        {
            if (mObjectGrid == null) return true;
            if (PolygonGrid.GetLength(0) != mObjectGrid.GetLength(0)) return true;

            for (int i = 0; i < PolygonGrid.Length; i++)
            {
                if (PolygonGrid[i].GetLength(0) != mObjectGrid[i].GetLength(0)) return true;
            }
            if (mReverseNormal != reverseNormal) return true;
            return false;
        }

        private void ResetAllPolygonPropertyEditFlags(Polygon[][] PolygonGrid)
        {
            mPropertyHasChangedGrid = new bool[PolygonGrid.Length][];
            for (int i = 0; i < PolygonGrid.Length; i++)
            {
                mPropertyHasChangedGrid[i] = new bool[PolygonGrid[i].Length];
                for (int j = 0; j < PolygonGrid[i].Length; j++)
                {
                    mPropertyHasChangedGrid[i][j] = false;
                }
            }
        }

        #endregion

        #endregion
    }
}
