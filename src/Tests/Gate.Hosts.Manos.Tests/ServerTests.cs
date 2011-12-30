﻿using System;
using System.IO;
using System.Net;
using System.Text;
using Gate.Helpers;
using Gate.Owin;
using NUnit.Framework;

namespace Gate.Hosts.Manos.Tests
{
    [TestFixture]
    public class ServerTests
    {
        [Test]
        public void ServerCanBeCreatedAndDisposed()
        {
            var server = Server.Create((env, result, fault) => { throw new NotImplementedException(); }, 9089, "");
            server.Dispose();
        }

        [Test]
        public void ServerWillRespondToRequests()
        {
            using (Server.Create(Wilson.App(), 9090))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:9090");
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var text = reader.ReadToEnd();
                        Assert.That(text, Is.StringContaining("hello world"));
                    }
                }
            }
        }

        [Test]
        public void ExceptionsWillReturnStatus500()
        {
            using (Server.Create(Wilson.App(), 9091))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:9091/?flip=crash");

                var exception = Assert.Throws<WebException>(() => request.GetResponse().Close());
                var response = (HttpWebResponse)exception.Response;
                Assert.That((int)response.StatusCode, Is.EqualTo(500));
            }
        }

        [Test]
        public void RequestsMayHavePostBody()
        {
            var requestData = new MemoryStream();
            AppDelegate app = (env, result, fault) =>
            {
                var body = (BodyDelegate)env[OwinConstants.RequestBody];
                body((data, continuation) =>
                {
                    requestData.Write(data.Array, data.Offset, data.Count);
                    return false;
                },
                fault,
                () => Wilson.App().Invoke(env, result, fault));
            };

            using (Server.Create(app, 9092))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:9092/");
                request.Method = "POST";
                using (var requestStream = request.GetRequestStream())
                {
                    var bytes = Encoding.Default.GetBytes("This is a test");
                    requestStream.Write(bytes, 0, bytes.Length);
                }

                request.GetResponse().Close();

                var requestText = Encoding.Default.GetString(requestData.ToArray());
                Assert.That(requestText, Is.EqualTo("This is a test"));
            }
        }

        [Test]
        public void StartupNameMayBeUsedAsParameterToCreate()
        {
            using (Server.Create("Gate.Hosts.Manos.Tests.Startup.Custom", 9093))
            {
                var request = (HttpWebRequest)WebRequest.Create("http://localhost:9093");
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var text = reader.ReadToEnd();
                        Assert.That(text, Is.StringContaining("This is a custom page"));
                    }
                }
            }
        }
    }
}
