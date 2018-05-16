﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Diagnostics;

namespace VinteR.KinectAdapter
{
    class KinectAdapter
    {
        private KinectSensor sensor;
        private KinectEventHandler kinectHandler;
        private Stopwatch syncroWatch;

        public KinectAdapter(Stopwatch synchroWatch)
        {

            // Create the Kinect Handler

            this.syncroWatch = synchroWatch;
            this.kinectHandler = new KinectEventHandler(this.syncroWatch);

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Update the SensorData - register EventHandler
                this.sensor.SkeletonFrameReady += this.kinectHandler.SensorSkeletonFrameReady;

                // Further EventListener can be appended here, currently no support for depth frame etc. intended.
                
                // Start the sensor!
                try
                {
                    this.sensor.Start();
                   
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                throw new Exception("The Kinect is not ready! Please check the cables etc. and restart the system!");
            }

        }

       /*
       * Write all Data out using the File Based Writers
       */
        public void flushData(string path)
        {
            // Write all Frames to the given JSON File
            this.kinectHandler.flushFrames(path);
        }


}
}
