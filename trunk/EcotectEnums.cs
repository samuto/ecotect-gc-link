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
 * Ecotect Enums 
 * Version: ecotect-gc-link 1.0
 * Author: Kaustuv DeBiswas
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Bentley.GenerativeComponents.Features
{
    public enum EcotectMaterial
    {
        AcousticTileSuspended,
        BarFridge140L,
        BrickCavityConcBlockPlaster,
        BrickConcBlockPlaster,
        BrickPlaster,
        BrickTimberFrame,
        Camera_Normal,
        Camera_Parallel,
        Camera_WideAngle,
        Cardiod_Microphone,
        ClayTiledRoof,
        ClayTiledRoof_Ref_Foil_Gyproc,
        ColumnSpeakers_1000Hz,
        ColumnSpeakers_500Hz,
        ComputerAndMonitor,
        ConcBlockPlaster,
        ConcBlockRender,
        ConcFlr_Carpeted_Suspended,
        ConcFlr_Suspended,
        ConcFlr_Tiles_Suspended,
        ConcFlr_Timber_Suspended,
        ConcreteRoof_Asphalt,
        ConcSlab_Carpeted_OnGround,
        ConcSlab_OnGround,
        ConcSlab_Tiles_OnGround,
        ConcSlab_Timber_OnGround,
        ConstructionLine,
        Cork,
        CorrugatedMetalRoof,
        CorrugatedMetalRoof1,
        DoubleBrickCavityPlaster,
        DoubleBrickCavityRender,
        DoubleBrickSolidPlaster,
        DoubleGlazed_AlumFrame,
        DoubleGlazed_LowE_AlumFrame,
        DoubleGlazed_LowE_TimberFrame,
        DoubleGlazed_TimberFrame,
        Downpipe,
        ExposedGround,
        ExternalPaving,
        Fabric,
        FaxMachine,
        Figure8_Microphone,
        FloodlightNoShielding,
        FlouroRecessedDroppedDiffuser,
        FluoroFlatPrismaticLense,
        FluoroLampStripUnit,
        FoamCore_Plywood,
        Framed_Plasterboard_Partition,
        Framed_Plywood_Partition,
        FramedPlasterboard,
        FramedTimberPlaster,
        FridgeFreezer440L,
        FridgeFreezer690L,
        GenericCable,
        Glass,
        GlassSlidingDoor,
        HalogenUplight,
        HighBayNarrowBeam,
        HollowCore_Plywood,
        IncandescentBareGlobe,
        IncandescentPendantDiffuseSphere,
        Linoleum,
        LowBayLenseReflector,
        MetalDeck,
        MetalDeck_Insulated,
        Mirror,
        Photocopier,
        Plaster_Foil_HeatRetention_CeramicTile,
        Plaster_Insulation_Suspended,
        Plaster_Joists_Suspended,
        Plastic,
        Plywood,
        Point_Receiver,
        PoolWater,
        RammedEarth_300mm,
        RammedEarth_500mm,
        ReverseBrickVeneer_R15,
        ReverseBrickVeneer_R20,
        SimpleLight,
        SingleGlazed_AlumFrame,
        SingleGlazed_AlumFrame_Blinds,
        SingleGlazed_TimberFrame,
        Slate,
        SolarCollector,
        SolidCore_OakTimber,
        SolidCore_PineTimber,
        SolidTimber,
        StainlessSteel,
        SuspendedConcreteCeiling,
        TimberCladMasonry,
        TimberFlr_Suspended,
        TimberFlrCarpeted_Suspended,
        Translucent_Skylight,
        Void,
        WashingMachine6kg,
    }

    public enum EcotectElementType
    {
        Void,
        Roof,
        Floor,
        Ceiling,
        Wall,
        Partition,
        Window,
        Panel,
        Door,
        Point,
        Speaker,
        Light,
        Appliance,
        Line,
        SolarCollector,
        Camera
    }

    public enum EcotectWeatherFile
    {
        Australia_AdelaideSA_1,
        Australia_BrisbaneQU_1,
        Australia_MelbourneVIC_1,
        Australia_PerthWA_1,
        Australia_SydneyNSW_1,
        Canada_MontrealQU,
        Canada_TorontoOT,
        Canada_VancouverBC_1,
        China_FushunXian,
        China_HongKong,
        Germany_Berlin,
        Germany_Munich,
        Greece_Athens,
        India_NewDelhi,
        Italy_Milano,
        Italy_Rome,
        Italy_Venice,
        Russia_Moscow,
        SaudiArabia_Riyadh,
        UK_AberdeenS,
        UK_AberporthW_1,
        UK_AberporthW_2,
        UK_AldergroveNI,
        UK_BirminghamE,
        UK_BrightonE,
        UK_BristolE,
        UK_CamborneE,
        UK_CambridgeE,
        UK_CardiffW,
        UK_CornwallE,
        UK_DundeeS,
        UK_EdinburghS,
        UK_EskdalemuirS_1,
        UK_EskdalemuirS_2,
        UK_ExeterE,
        UK_GlasgowS,
        UK_HeathrowE,
        UK_KewE_1,
        UK_KewE_2,
        UK_KewE_3,
        UK_KewE_4,
        UK_LancashireE,
        UK_LerwickS,
        UK_LondonE,
        UK_ManchesterE,
        UK_NewcastleE,
        UK_NorwichE,
        UK_SheffieldE,
        UK_YorkE,
        USA_AtlantaGeorgia,
        USA_DenverColorado,
        USA_LasVegasNevada,
        USA_LosAngelesCalifornia,
        USA_NewYorkNewYork,
        USA_SanFranciscoCalifornia
    }

    public enum EcotectTerrain
    {
        Exposed,
        Rural,
        Suburban,
        Urban
    }

    public enum EcotectBuildingType
    {
        AutomotiveFacility,
        ConventionCenter,
        Courthouse,
        DiningBarLoungeOrLeisure,
        DiningCafeteriaFastFood,
        DiningFamily,
        Dormitory,
        ExcerciseCenter,
        FireStation,
        Gymnasium,
        HospitalOrHealthCare,
        Hotel,
        Library,
        Manufacturing,
        Motel,
        MotionPictureTheater,
        MultiFamily,
        Museum,
        Office,
        ParkingGarage,
        Penitentiary,
        PerformingArtsTheater,
        PoliceStation,
        PostOffice,
        Religious,
        Retail,
        SchoolOrUniv,
        SingleFamily,
        SportsArena,
        TownHall,
        Transportation,
        Unknown,
        Warehouse,
        Workshop
    }

    public enum EcotectObjectType
    {
        PolygonList,
        PolygonGrid,
        PointList,
        PointGrid,
        Unknown
    }

    public enum EcotectAccumulationType
    {
        Cumulative,
        AverageDaily,
        AverageHourly,
        Peak
    }

    public enum EcotectDayLightAttribute
    {
        DaylightFactor,
        DaylightLevel,
        SkyComponent
    }

    public enum EcotectRadiationAttribute
    {
        Total,
        Direct,
        Diffuse
    }

    public enum EcotectInsolationAttributeType
    {
        AverageDailyTotal,
        AverageDailyDirect,
        AverageDailyDiffuse
    }

    public enum EcotectDaylightAttributeType
    {
        DaylightFactor,
        DaylightLevel,
        SkyComponent
    }

    public enum EcotectSkyIlluminance
    {
        CIE_Overcast_Sky_3000,
        CIE_Overcast_Sky_4500,
        CIE_Overcast_Sky_6000,
        CIE_Overcast_Sky_9000,
        CIE_Uniform_Sky_3000,
        CIE_Uniform_Sky_4500,
        CIE_Uniform_Sky_6000,
        CIE_Uniform_Sky_9000,
    }
    
    //    Australia-AdelaideSA-1.wea,
    //    Australia-BrisbaneQU-1.wea,
    //    Australia-MelbourneVIC-1.wea
    //    Australia-PerthWA-1.wea
    //    Australia-SydneyNSW-1.wea
    //    Canada-MontrealQU.wea
    //    Canada-TorontoOT.wea
    //    Canada-VancouverBC-1.wea
    //    China-FushunXian.wea
    //    China-HongKong.wea
    //    Germany-Berlin.wea
    //    Germany-Munich.wea
    //    Greece-Athens.wea
    //    India-NewDelhi.wea
    //    Italy-Milano.wea
    //    Italy-Rome.wea
    //    Italy-Venice.wea
    //    Russia-Moscow.wea
    //    SaudiArabia-Riyadh.wea
    //    UK-AberdeenS.wea
    //    UK-AberporthW-1.wea
    //    UK-AberporthW-2.wea
    //    UK-AldergroveNI.wea
    //    UK-BirminghamE.wea
    //    UK-BrightonE.wea
    //    UK-BristolE.wea
    //    UK-CamborneE.wea
    //    UK-CambridgeE.wea
    //    UK-CardiffW.wea
    //    UK-CornwallE.wea
    //    UK-DundeeS.wea
    //    UK-EdinburghS.wea
    //    UK-EskdalemuirS-1.wea
    //    UK-EskdalemuirS-2.wea
    //    UK-ExeterE.wea
    //    UK-GlasgowS.wea
    //    UK-HeathrowE.wea
    //    UK-KewE-1.wea
    //    UK-KewE-2.wea
    //    UK-KewE-3.wea
    //    UK-KewE-4.wea
    //    UK-LancashireE.wea
    //    UK-LerwickS.wea
    //    UK-LondonE.wea
    //    UK-ManchesterE.wea
    //    UK-NewcastleE.wea
    //    UK-NorwichE.wea
    //    UK-SheffieldE.wea
    //    UK-YorkE.wea
    //    USA-AtlantaGeorgia.wea
    //    USA-DenverColorado.wea
    //    USA-LasVegasNevada.wea
    //    USA-LosAngelesCalifornia.wea
    //    USA-NewYorkNewYork.wea
    //    USA-SanFranciscoCalifornia.wea

}
