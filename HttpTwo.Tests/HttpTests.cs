using NUnit.Framework;
using System;
using System.Net.Http;
using System.Collections.Specialized;
using System.Threading;
using HttpTwo.Internal;

namespace HttpTwo.Tests
{
    [TestFixture]
    public class HttpTests
    {
        const bool UseInternalHttpRunner = true;

        NodeHttp2Runner node;

        [TestFixtureSetUp]
        public void Setup ()
        {
            // Setup logger 
            Log.Logger = new ConsoleLogger { Level = LogLevel.Info };

            if (UseInternalHttpRunner) {
                node = new NodeHttp2Runner ();
                //node.LogHandler = Console.WriteLine;
            
                node.StartServer ();
                // Wait for the server to initialize
                Thread.Sleep (2000);
            }
        }

        [TestFixtureTearDown]
        public void Teardown ()
        {     
            if (UseInternalHttpRunner)
                node.StopServer ();
        }

        [Test]
        public async void Get_Single_Html_Page ()
        {
            //var http2MsgHandler = new Http2MessageHandler ();
            //var http = new HttpClient (http2MsgHandler);

            //var data = await http.GetStringAsync ("http://localhost:8999/index.html");

            //Assert.IsNotNullOrEmpty (data);
            //Assert.IsTrue (data.Contains ("Hello World"));
        }

        //[Test]
        public async void Get_Single_Html_Page_Https ()
        {
            //var http2MsgHandler = new Http2MessageHandler ();
            //var http = new HttpClient (http2MsgHandler);

            //var data = await http.GetStringAsync ("https://localhost:8999/index.html");

            //Assert.IsNotNullOrEmpty (data);
            //Assert.IsTrue (data.Contains ("Hello World"));
        }

        [Test]
        public async void Get_Multiple_Html_Pages ()
        {
            //var http2MsgHandler = new Http2MessageHandler ();
            //var http = new HttpClient (http2MsgHandler);

            //for (int i = 0; i < 3; i++) {
            //    var data = await http.GetStringAsync ("http://localhost:8999/index.html");

            //    Assert.IsNotNullOrEmpty (data);
            //    Assert.IsTrue (data.Contains ("Hello World"));
            //}
        }


        [Test]
        public async void Settings_Disable_Push_Promise ()
        {
            var url = new Uri ("http://localhost:8999/index.html");
            var settings = new Http2ConnectionSettings (url) { DisablePushPromise = true };
            var http = new Http2Client (settings);

            http.Connect ();

            var didAck = false;
            var semaphoreSettings = new SemaphoreSlim (0);
            var cancelTokenSource = new CancellationTokenSource ();

            var connectionStream = http.StreamManager.Get (0);
            connectionStream.OnFrameReceived += (frame) => {
                // Watch for an ack'd settings frame after we sent the frame with no push promise
                if (frame.Type == FrameType.Settings) {
                    if ((frame as SettingsFrame).Ack) {
                        didAck = true;
                        semaphoreSettings.Release ();
                    }
                }
            };

            cancelTokenSource.CancelAfter (TimeSpan.FromSeconds (2));

            await semaphoreSettings.WaitAsync (cancelTokenSource.Token);

            Assert.IsTrue (didAck);
        }


        [Test]
        public void Get_Send_Headers_With_Continuation ()
        {
            var uri = new Uri ("http://localhost:8999/index.html");
            var http = new Http2Client (uri);

            // Generate some gibberish custom headers
            var headers = new NameValueCollection ();
            for (int i = 0; i < 1000; i++)
                headers.Add ("custom-" + i, "HEADER-VALUE-" + i);

            var response = http.Send (uri, Http2Client.HttpMethod.Get, headers, new byte[0]);

            var data = System.Text.Encoding.ASCII.GetString (response.Body);

            Assert.IsNotNullOrEmpty (data);
            Assert.IsTrue (data.Contains ("Hello World"));
        }

        [Test]
        public void Ping ()
        {
            var uri = new Uri ("http://localhost:8999/index.html");
            var http = new Http2Client (uri);

            var data = System.Text.Encoding.ASCII.GetBytes ("PINGPONG");

            var cancelTokenSource = new CancellationTokenSource ();
            cancelTokenSource.CancelAfter (TimeSpan.FromSeconds (2));

            var pong = http.Ping(data);

            Assert.IsTrue (pong);
        }

        [Test]
        public void GoAway ()
        {
            var uri = new Uri ("http://localhost:8999/index.html");
            var http = new Http2Client (uri);

            http.Connect ();

            var cancelTokenSource = new CancellationTokenSource ();
            cancelTokenSource.CancelAfter (TimeSpan.FromSeconds (2));

            var sentGoAway = http.Disconnect ();

            Assert.IsTrue (sentGoAway);
        }
    }
}

