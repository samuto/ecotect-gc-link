# List of GC-Ecotect Features #

## 1. EcotectProject ##

**Description:** To make connection with Ecotect and start a project. There can be only one EcotectProject feature in a particular GC transaction file.

**UpdateMethod:** ByProjectTitle

**Parameters:**

string              ProjectTitle <br>
string              ClientInfo<br>
double              NorthClockwiseOffset <br>
double              LengthOfNorthLine<br>
double              Altitude<br>
EcotectTerrain      LocalTerrain<br>
EcotectWeatherFile  WeatherData<br>
EcotectBuildingType BuildingType<br>


<h2>2. EcotectPointObject</h2>

<b>Description:</b> To create ecotect points for calculation<br>
<br>
<b>UpdateMethod:</b> FromPointGrid<br>
<br>
<b>Parameters:</b>

Point<a href='.md'>.md</a><a href='.md'>.md</a>       PointGrid<br>
bool            ShowMarker<br>
double          MarkerSize<br>
TextStyle       TxtStyle<br>

<b>UpdateMethod:</b> FromPointList<br>
<br>
<b>Parameters:</b>

Point<a href='.md'>.md</a>         PointList<br>
bool            ShowMarker<br>
double          MarkerSize<br>
TextStyle       TxtStyle<br>

<h2>3. EcotectPolygonObject</h2>

<b>Description:</b> To create ecotect polygon for calculation. <b>NOTE:</b> The polygons must be planar triangular!<br>
<br>
<b>UpdateMethod:</b> FromPolygonGrid<br>
<br>
<b>Parameters:</b>

Polygon<a href='.md'>.md</a><a href='.md'>.md</a>     PolygonGrid<br>
EcotectMaterial Material<br>
bool            ReverseNormal<br>
double          LengthOfNormal<br>
bool            ShowNormals<br>
TextStyle       TxtStyle<br>

<b>UpdateMethod:</b> FromPolygonList<br>
<br>
<b>Parameters:</b>

Polygon<a href='.md'>.md</a>       PolygonList<br>
EcotectMaterial Material<br>
bool            ReverseNormal<br>
double          LengthOfNormal<br>
bool            ShowNormals<br>
TextStyle       TxtStyle<br>

<h2>4. EcotectCalcSolar</h2>

<b>Description:</b> To perform solar analysis<br>
<br>
<b>UpdateMethod:</b> IncidentSolarRadiationByPolygons<br>
<br>
<b>Parameters:</b>

EcotectPolygonObject      EcoObject<br>
EcotectProject            EcoProject<br>
Plane                     InfoDisplayPlane<br>
EcotectAccumulationType   CalculationType<br>
int<a href='.md'>.md</a>                     CalcDays<br>
double<a href='.md'>.md</a>                  CalcTime<br>
int                       Precision<br>
EcotectRadiationAttribute AttributeIndex<br>
bool                      InDynamics<br>
bool                      WriteToFile<br>
string                    FileName<br>
double                    DisplayScale<br>

<h2>5. EcotectCalcDaylight</h2>

<b>Description:</b> To perform daylight analysis<br>
<br>
<b>UpdateMethod:</b> DaylightCalculationByPoints<br>
<br>
<b>Parameters:</b>

EcotectPointObject        EcoPointObject<br>
EcotectProject            EcoProject<br>
Plane                     InfoDisplayPlane<br>
EcotectSkyIlluminance     SkyIlluminance<br>
int<a href='.md'>.md</a>                     CalcDays<br>
double<a href='.md'>.md</a>                  CalcTime<br>
int                       Precision<br>
EcotectRadiationAttribute AttributeIndex<br>
bool                      InDynamics<br>
bool                      WriteToFile<br>
string                    FileName<br>
double                    MarkerSize<br>
bool                      MarkerOn<br>
double                    DisplayScale<br>