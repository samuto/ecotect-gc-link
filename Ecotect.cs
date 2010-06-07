/*
 * Ecotect class
 * Version: EcotectLink V8i_R3
 * Author: Kaustuv DeBiswas
 */

using System;
using System.Diagnostics;
using System.ComponentModel;
using Bentley.Interop.MicroStationDGN;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Forms;
using Bentley.GenerativeComponents.GeneralPurpose;
using Bentley.GenerativeComponents;
using Bentley.GenerativeComponents.MicroStation;  // added to make GeometryTools work
using Bentley.Geometry;
using Bentley.GenerativeComponents.GCScript;
using Bentley.GenerativeComponents.Features;
using Bentley.GenerativeComponents.Features.Specific;
using Bentley.MicroStation.Application;
using System.Threading;
using NDde.Client;

namespace Bentley.GenerativeComponents.Features
{
    public class Ecotect : Feature
    {

        // Class properties.
        private static DdeClient client = null;
        private static int iTimeout = Int32.MaxValue; //this is the limit, if ecotect takes more time it will be timed out.

        // State variables.
        private static bool bConnected = false;
        private static bool bError     = false;

        //Debug mode
        private static int iDebugLevel = 0;

        private delegate int MyDelegate(string s);
        private static AsyncCallback cb = new AsyncCallback(whatToDoNext);

        static void whatToDoNext(IAsyncResult ar)
        {
            if (iDebugLevel > 0) MessageBox.Show(ar.IsCompleted.ToString());
            //MyDelegate x = (MyDelegate)(I
        }

        static Ecotect
        (
        )
        {
            dde_Init();
        }

        // Constructor.
        public Ecotect
        (
        ): base()
        {
            // dde_Init();
        }

        // Constructor.
        public Ecotect
        (
        Feature parentFeature
        ): base(parentFeature)
        {
            // dde_Init();
        }

        // ECOTECT - DDE Connection Management.

        /// <summary>Initialises DDE connection with ECOTECT.</summary>
        private  static void dde_Init()
        {
            // Initialise connection object.
            client = new DdeClient("Ecotect", "request");

            bConnected = false;
            bError = false;
            
            // Start connection.
            dde_Connect();
        }

        /// <summary>Attempts to contact DDE server.</summary>
        private static void dde_Connect()
        {

            try
            {
                // Actually make connection...
                client.Connect();

                // No exception so must be okay...
                bConnected = true;

            }

            catch (Exception ex)
            {
                
                // Search for ECOTECT and run it.
                
                // Get GC to display and arror message.
                Bentley.MicroStation.Application.MessageCenter.ShowErrorMessage("ERROR - \'ECOTECT\' did not respond: " + ex.Message, "ERROR - \'ECOTECT\' did not respond: " + ex.Message, false);
                // SendErrorReport("ERROR - Service \'ECOTECT\' did not respond: " + ex.Message);
                bConnected = false;
            }

            bError = !bConnected;

        }

        // GC - Accessible functions.

        /// <summary>Sets a value/property in ECOTECT.</summary>
        [Update]
        public bool Execute
        (
            FeatureUpdateContext updateContext,
            string Executor,
            [DefaultExpression(null)] IGCObject Driver
        )
        {
            return Executer(Executor);
        }

        
        

        public static bool Executer
        (
            string Executor
        )
        {
            try
            {
                //popup message
                if (iDebugLevel > 0) MessageBox.Show("Execute:" + Executor + "  " + client.IsConnected, "alert", MessageBoxButtons.OK);
                
                // Send command string in asynchronous mode            
                IAsyncResult pendingOp = client.BeginExecute(Executor, null, null);
                int ii = 0;
                while (!pendingOp.IsCompleted)
                {
                    ii++;
                }

                if (iDebugLevel > 0) MessageBox.Show("Execute took: " + ii + "ticks");
                client.EndExecute(pendingOp);
                return true;

            }

            catch (Exception ex)
            {

                // Send error report
                Bentley.MicroStation.Application.MessageCenter.ShowErrorMessage("ERROR - \'ECOTECT\' rejected command: " + ex.Message, "ERROR - \'ECOTECT\' rejected command: " + ex.Message,false);
                //SendErrorReport("ERROR - \'ECOTECT\' rejected command: " + ex.Message);

            }

            return false;

        }

        public static string Requester
        (
        string Requestor
        )
        {
            try
            {
                //popup message
                if (iDebugLevel > 0) MessageBox.Show("Execute:" + Requestor + "  " + client.IsConnected, "alert", MessageBoxButtons.OK);

                // Send request and collect string result.            
                return client.Request(Requestor, iTimeout);
            }

            catch (Exception ex)
            {

                // Send error report
                Bentley.MicroStation.Application.MessageCenter.ShowErrorMessage("ERROR - \'ECOTECT\' rejected request: " + ex.Message, "ERROR - \'ECOTECT\' rejected request: " + ex.Message, false);
                //SendErrorReport("ERROR - \'ECOTECT\' rejected request: " + ex.Message);
            
            }

            return "";
        }

        /// <summary>Sets a value/property in ECOTECT.</summary>
        [Update]
        public bool Request
        (
            FeatureUpdateContext updateContext,
            string Requestor,
            [DefaultExpression(null)] IGCObject Driver,
            [Out] ref string Result
        )
        {
            Result = Requester(Requestor);
            
            return true;
        }

    }
}
