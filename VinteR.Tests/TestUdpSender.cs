﻿using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Ninject;
using NUnit.Framework;
using VinteR.Configuration;
using VinteR.Model;
using VinteR.OutputAdapter;
using VinteR.Serialization;

namespace VinteR.Tests
{
    [TestFixture]
    public class TestUdpSender
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private UdpSender _udpSender;

        [OneTimeSetUp]
        public void OnSetupFixture()
        {
            var ninjectKernel = new StandardKernel(new VinterNinjectTestModule());
            _udpSender = new UdpSender(ninjectKernel.Get<IConfigurationService>(), ninjectKernel.Get<ISerializer>())
            {
                UdpReceivers = new List<UdpReceiver>()
                {
                    new UdpReceiver() {Port = 5080, Ip = "127.0.0.1"}
                }
            };
            _udpSender.Start(new Session("udp test"));
        }

        [Test]
        public void TestSend()
        {

            foreach (var receiver in _udpSender.UdpReceivers)
            {
                Logger.Debug("{0}:{1}", receiver.Ip, receiver.Port);
            }
            var client = new UdpClient(5080);
            var serverAddress = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5060);

            _udpSender.OnDataReceived(new MocapFrame("root", "optitrack")
            {
                ElapsedMillis = 30,
                Latency = 5.3f,
                Gesture = "",
                Bodies = new List<Body>()
                {
                    new Body()
                    {
                        BodyType = Body.EBodyType.Marker,
                        Rotation = Quaternion.Identity,
                        Centroid = Vector3.One,
                        Points = new List<Point>()
                        {
                            new Point(-3.2f, 4.0f, 5.657f)
                            {
                                Name = "",
                                State = ""
                            }
                        },
                        Name = "dynamite"
                    }
                }
            });
            var received = client.Receive(ref serverAddress);

            var frame = Model.Gen.MocapFrame.Parser.ParseFrom(received);
            Assert.AreEqual(5.3f, frame.Latency);

            var centroid = frame.Bodies[0].Centroid;
            Assert.AreEqual(Vector3.One, new Vector3(centroid.X, centroid.Y, centroid.Z));

            Assert.AreEqual("dynamite", frame.Bodies[0].Name);
        }
    }
}