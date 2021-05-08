using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Titanium.Web.Proxy.EventArguments;
using Titanium.Web.Proxy.Exceptions;
using Titanium.Web.Proxy.Helpers;
using Titanium.Web.Proxy.Http;
using Titanium.Web.Proxy.Models;
using Titanium.Web.Proxy.StreamExtended.Network;
using Titanium.Web.Proxy;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Converters;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;



public class TitaniumProxyController
{
    private readonly SemaphoreSlim @lock = new SemaphoreSlim(1);
    private readonly ProxyServer proxyServer;
    private ExplicitProxyEndPoint explicitEndPoint;
    public string RequestBodySelected = null;
    public string PartStringRequest = null;
    public string PartStringResponse = null;
    public string PartStringAfterResponse = null;
    public string SearchInResponseBody = null;
    public HttpWebClient Output;
    public Boolean CaptureCompleted = false;
    public TitaniumProxyController()
    {
        proxyServer = new ProxyServer();
        proxyServer.CertificateManager.CreateRootCertificate(true);
        proxyServer.CertificateManager.TrustRootCertificate();
        proxyServer.ExceptionFunc = async exception =>
        {
            if (exception is ProxyHttpException phex)
            {
                await writeToConsole(exception.Message + ": " + phex.InnerException?.Message, ConsoleColor.Red);
            }
            else
            {
                await writeToConsole(exception.Message, ConsoleColor.Red);
            }
        };
        proxyServer.TcpTimeWaitSeconds = 10;
        proxyServer.ConnectionTimeOutSeconds = 15;
        proxyServer.ReuseSocket = false;
        proxyServer.EnableConnectionPool = false;
        proxyServer.CertificateManager.SaveFakeCertificates = true;
    }


    public void StartProxy()
    {
        proxyServer.BeforeRequest += onRequest;
        proxyServer.BeforeResponse += onResponse;
        proxyServer.AfterResponse += onAfterResponse;
        proxyServer.ServerCertificateValidationCallback += OnCertificateValidation;
        proxyServer.ClientCertificateSelectionCallback += OnCertificateSelection;
        explicitEndPoint = new ExplicitProxyEndPoint(IPAddress.Any, 8000);
        explicitEndPoint.BeforeTunnelConnectRequest += onBeforeTunnelConnectRequest;
        explicitEndPoint.BeforeTunnelConnectResponse += onBeforeTunnelConnectResponse;
        proxyServer.AddEndPoint(explicitEndPoint);
        proxyServer.Start();
        foreach (var endPoint in proxyServer.ProxyEndPoints)
        {
            Console.WriteLine("Listening on '{0}' endpoint at Ip {1} and port: {2} ", endPoint.GetType().Name,
                endPoint.IpAddress, endPoint.Port);
        }
        if (RunTime.IsWindows)
        {
            proxyServer.SetAsSystemProxy(explicitEndPoint, ProxyProtocolType.AllHttp);
        }
    }
    public void Stop()
    {
        explicitEndPoint.BeforeTunnelConnectRequest -= onBeforeTunnelConnectRequest;
        explicitEndPoint.BeforeTunnelConnectResponse -= onBeforeTunnelConnectResponse;
        proxyServer.BeforeRequest -= onRequest;
        proxyServer.BeforeResponse -= onResponse;
        proxyServer.ServerCertificateValidationCallback -= OnCertificateValidation;
        proxyServer.ClientCertificateSelectionCallback -= OnCertificateSelection;
        proxyServer.Stop();
        //proxyServer.CertificateManager.RemoveTrustedRootCertificate();
    }

    private async Task onBeforeTunnelConnectRequest(object sender, TunnelConnectSessionEventArgs e)
    {
        string hostname = e.HttpClient.Request.RequestUri.Host;
        var clientLocalIp = e.ClientLocalEndPoint.Address;
        if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
        {
            e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);
        }
        if (hostname.Contains("something"))
        {
            e.DecryptSsl = true;
        }
    }
    private void WebSocket_DataSent(object sender, DataEventArgs e)
    {
        var args = (SessionEventArgs)sender;
        WebSocketDataSentReceived(args, e, true);
    }
    private void WebSocket_DataReceived(object sender, DataEventArgs e)
    {
        var args = (SessionEventArgs)sender;
        WebSocketDataSentReceived(args, e, false);
    }
    private void WebSocketDataSentReceived(SessionEventArgs args, DataEventArgs e, bool sent)
    {
        var color = sent ? ConsoleColor.Green : ConsoleColor.Blue;
        foreach (var frame in args.WebSocketDecoderReceive.Decode(e.Buffer, e.Offset, e.Count))
        {
            if (frame.OpCode == WebsocketOpCode.Binary)
            {
                var data = frame.Data.ToArray();
                string str = string.Join(",", data.ToArray().Select(x => x.ToString("X2")));
                //writeToConsole(str, color).Wait();
            }
            if (frame.OpCode == WebsocketOpCode.Text)
            {
                //writeToConsole(frame.GetText(), color).Wait();
            }
        }
    }
    private Task onBeforeTunnelConnectResponse(object sender, TunnelConnectSessionEventArgs e)
    {
        //e.GetState().PipelineInfo.AppendLine(nameof(onBeforeTunnelConnectResponse) + ":" + e.HttpClient.Request.RequestUri);
        return Task.CompletedTask;
    }
    // intercept & cancel redirect or update requests
    private async Task onRequest(object sender, SessionEventArgs e)
    {
        if (PartStringRequest != null)
        {
            //e.GetState().PipelineInfo.AppendLine(nameof(onRequest) + ":" + e.HttpClient.Request.RequestUri);
            var clientLocalIp = e.ClientLocalEndPoint.Address;
            if (!clientLocalIp.Equals(IPAddress.Loopback) && !clientLocalIp.Equals(IPAddress.IPv6Loopback))
            {
                e.HttpClient.UpStreamEndPoint = new IPEndPoint(clientLocalIp, 0);
            }
            if (e.HttpClient.Request.Url.Contains("yahoo.com"))
            {
                e.CustomUpStreamProxy = new ExternalProxy("localhost", 8888);
            }
            if (e.HttpClient.Request.Url.Contains(PartStringRequest))
            {
                RequestBodySelected = e.GetRequestBodyAsString().GetAwaiter().GetResult();
            }
        }
    }
    // Modify response
    private async Task multipartRequestPartSent(object sender, MultipartRequestPartSentEventArgs e)
    {
        //e.GetState().PipelineInfo.AppendLine(nameof(multipartRequestPartSent));
        var session = (SessionEventArgs)sender;
        await writeToConsole("Multipart form data headers:");
        foreach (var header in e.Headers)
        {
            await writeToConsole(header.ToString());
        }
    }
    private async Task onResponse(object sender, SessionEventArgs e)
    {
        //Console.WriteLine(e.HttpClient.Request.Url);
        if (PartStringResponse != null)
        {
            if (SearchInResponseBody == null)
            {
                if (e.HttpClient.Request.Url.ToLowerInvariant().Contains(PartStringResponse))
                {
                    e.GetResponseBodyAsString().GetAwaiter().GetResult();

                    var body = e.HttpClient.Response.BodyString;
                    Output = e.HttpClient;
                    CaptureCompleted = true;
                }
            }
            else
            {
                if (e.HttpClient.Request.Url.ToLowerInvariant().Contains(PartStringResponse))
                {
                    e.GetResponseBodyAsString().GetAwaiter().GetResult();

                    var body = e.HttpClient.Response.BodyString;
                    if (body.IndexOf(SearchInResponseBody) >= 0)
                    {
                        Output = e.HttpClient;
                        CaptureCompleted = true;
                    }
                }

            }
            //e.GetState().PipelineInfo.AppendLine(nameof(onResponse));
            if (e.HttpClient.ConnectRequest?.TunnelType == TunnelType.Websocket)
            {
                e.DataSent += WebSocket_DataSent;
                e.DataReceived += WebSocket_DataReceived;
            }
            //await writeToConsole("Active Server Connections:" + ((ProxyServer)sender).ServerConnectionCount);
            string ext = System.IO.Path.GetExtension(e.HttpClient.Request.RequestUri.AbsolutePath);

            if (e.HttpClient.Request.Url.ToLowerInvariant().Contains(PartStringResponse))
            {
                //var body = e.HttpClient.Response.BodyString;
                //var a = "";
            }

        }
    }
    private async Task onAfterResponse(object sender, SessionEventArgs e)
    {
        if (PartStringAfterResponse != null)
        {

        }
    }
    /// <summary>
    ///     Allows overriding default certificate validation logic
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public Task OnCertificateValidation(object sender, CertificateValidationEventArgs e)
    {

        if (e.SslPolicyErrors == SslPolicyErrors.None)
        {
            e.IsValid = true;
        }
        return Task.CompletedTask;
    }
    /// <summary>
    ///     Allows overriding default client certificate selection logic during mutual authentication
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public Task OnCertificateSelection(object sender, CertificateSelectionEventArgs e)
    {
        //e.GetState().PipelineInfo.AppendLine(nameof(OnCertificateSelection));
        // set e.clientCertificate to override
        return Task.CompletedTask;
    }
    private async Task writeToConsole(string message, ConsoleColor? consoleColor = null)
    {
        await @lock.WaitAsync();
        if (consoleColor.HasValue)
        {
            ConsoleColor existing = Console.ForegroundColor;
            Console.ForegroundColor = consoleColor.Value;
            //Console.WriteLine(message);
            Console.ForegroundColor = existing;
        }
        else
        {
            Console.WriteLine(message);
        }
        @lock.Release();
    }
}
public class HttpCapturer
{
    public  Tuple<HttpWebClient, string> WaitForCaptureAndReturnOutput(string PartStringResponse, string message, string _SearchInResponseBody = null)
    {
        TitaniumProxyController controller = new TitaniumProxyController();
        controller.PartStringResponse = PartStringResponse;
        controller.SearchInResponseBody = _SearchInResponseBody;
        controller.StartProxy();
        System.Threading.Thread.Sleep(3000);
        Console.WriteLine(message);
        bool DoJob = true;
        /*try
        {
            do
            {
                if (controller.Output != null)
                {
                    if (controller.Output.Response != null)
                    {
                        if (controller.Output.Response.Body != null)
                        {
                            DoJob = false;
                        }
                    }
                }

            } while (DoJob);
        }
        catch
        {
            controller.Stop();
            return null;
        }*/
        do {

        } while (!controller.CaptureCompleted);
        string body = controller.Output.Response.BodyString;
        controller.Stop();
        var result = new Tuple<HttpWebClient, string>(controller.Output, body);

        return result;
    }
}