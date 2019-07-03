using Kotsh.Models;
using Leaf.xNet;
using System.Collections.Generic;
using System.Linq;

namespace Kotsh.Blocks
{
    /// <summary>
    /// Request block allows you to call URLs
    /// </summary>
    public class Request
    {
        /// <summary>
        /// Available methods
        /// </summary>
        private string[] methods = {
            "GET",
            "POST"
        };

        /// <summary>
        /// Request builder (public to let user do custom things)
        /// </summary>
        public HttpRequest request;

        /// <summary>
        /// Target URL
        /// </summary>
        private string URL;

        /// <summary>
        /// HTTP Method
        /// </summary>
        private string method;

        /// <summary>
        /// POST body
        /// </summary>
        private StringContent body;

        /// <summary>
        /// Block instance
        /// </summary>
        private Block Block;

        /// <summary>
        /// Initialize class by storing Block instance
        /// </summary>
        /// <param name="block"></param>
        public Request(Block block)
        {
            // Store instance
            this.Block = block;
        }

        /// <summary>
        /// Initialize Request class
        /// </summary>
        /// <param name="URL">Target URL</param>
        /// <param name="timeout">Set default timeout</param>
        public void Build(string URL, int timeout = 10000)
        {
            // Reset all
            this.URL = default;
            this.method = default;
            this.body = new StringContent("");
            Block.Source.Reset();

            // Initialize HttpRequest
            request = new HttpRequest();

            // Ignore protocol errors
            request.IgnoreProtocolErrors = true;

            // Set connect timeout
            request.ConnectTimeout = timeout;

            // Store URL
            this.URL = URL;
        }

        /// <summary>
        /// Add a method
        /// </summary>
        /// <param name="method">Method (GET, POST, etc...)</param>
        public void Method(string method)
        {
            // Make value uppercase
            method = method.ToUpper();

            // Check if method is available
            if (methods.Any(method.Contains))
            {
                // Store method
                this.method = method;
            }
            else
            {
                System.Console.WriteLine("-> Method is not available: " + method);
            }
        }

        /// <summary>
        /// Add a single param input
        /// </summary>
        /// <param name="body">POST body</param>
        /// <param name="content_type">HTTP Content-Type header</param>
        public void AddBody(string body, ContentType content_type)
        {
            // Append body
            this.body = new StringContent(Block.StringUtil.ReplaceValues(body), System.Text.Encoding.UTF8);

            // Set corresponding content type
            switch (content_type)
            {
                case ContentType.PLAIN:
                    AddHeader("Content-Type", "text/plain");
                    break;

                case ContentType.JSON:
                    AddHeader("Content-Type", "application/json");
                    break;

                case ContentType.FORM:
                    AddHeader("Content-Type", "application/x-www-form-urlencoded");
                    break;
            }
        }

        /// <summary>
        /// Add header
        /// </summary>
        /// <param name="key">Header key</param>
        /// <param name="value">Header value</param>
        public void AddHeader(string key, string value)
        {
            // Add header
            request[key] = value;
        }

        /// <summary>
        /// Make cookie jar and add as header
        /// </summary>
        /// <param name="cookies">Cookie Dictionnary as key;value</param>
        public void AddCookies(Dictionary<string, string> cookies)
        {
            // Prepare header
            string header = "";

            // Foreach cookies
            foreach (var cookie in cookies)
            {
                header += cookie.Key + "=" + cookie.Value + "&";
            }

            // Trim last '&'
            header = header.Trim('&');

            // Send header
            this.AddHeader("Cookie", header);
        }

        /// <summary>
        /// Set proxy (if defined) and execute action
        /// </summary>
        /// <param name="is_retry">If true, it will retry</param>
        /// <param name="can_be_null">If true, it will accept blank responses</param>
        /// <param name="auto_retry">Will retry the request on exception</param>
        public void Execute(bool is_retry = false, bool can_be_null = false, bool auto_retry = true)
        {
            // Check if proxies are used
            if (Block.core.Proxies.Count > 0)
            {
                // Get proxy type
                string type = Block.core.runSettings["ProxyProtocol"];

                // Get proxy
                string proxy;
                if (is_retry)
                {
                    proxy = Block.core.Tasker.GetProxy(true);
                }
                else
                {
                    proxy = Block.core.Tasker.GetProxy();
                }

                // Select proxy
                switch (type)
                {
                    case "HTTP":
                        request.Proxy = HttpProxyClient.Parse(proxy);
                        break;
                    case "SOCKS4":
                        request.Proxy = Socks4ProxyClient.Parse(proxy);
                        break;
                    case "SOCKS4A":
                        request.Proxy = Socks4AProxyClient.Parse(proxy);
                        break;
                    case "SOCKS5":
                        request.Proxy = Socks5ProxyClient.Parse(proxy);
                        break;
                }
            }

            // Handle errors
            try
            {
                // Handle response
                HttpResponse response;

                // Sort method
                if (method == "GET")
                {
                    response = request.Get(URL);
                }
                else
                {
                    response = request.Post(URL, body);
                }

                // Assign responses
                AssignResponses(response);

                // Check for errors
                if (!can_be_null && response.ToString().Length < 1)
                {
                    // Increment retry
                    Block.core.RunStatistics.Increment(Type.RETRY);

                    // Response is null, relaunching it
                    Execute(true);
                }
            }
            catch
            {
                if (auto_retry)
                {
                    // Increment retry
                    Block.core.RunStatistics.Increment(Type.RETRY);

                    // Response is null, relaunching it
                    Execute();
                }
            }
        }

        private void AssignResponses(HttpResponse res)
        {
            // Data response (JSON, HTML, XML, etc...)
            Block.Source.data = res.ToString();

            // Status code
            Block.Source.status = res.StatusCode.ToString();

            // Full response
            Block.Source.full = res;
        }
    }
}