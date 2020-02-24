using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace Chromia.Postchain.Client
{


    public class RESTClient
    {
        private string UrlBase;
        private string BlockchainRID;

        ///<summary>
        ///Create new RESTClient object.
        ///</summary>
        ///<param name = "urlBase">URL to rest server.</param>
        ///<param name = "blockchainRID">RID of blockchain.</param>
        public RESTClient(string urlBase, string blockchainRID)
        {
            if (urlBase.Last() != '/') urlBase = urlBase + "/";
            this.UrlBase = urlBase;
            this.BlockchainRID = blockchainRID;
        }

        public async Task<object> PostTransaction(string serializedTransaction)
        {
            string jsonString = String.Format(@"{{""tx"": ""{0}""}}", serializedTransaction);

            return await Post(this.UrlBase, "tx/" + this.BlockchainRID, jsonString);
        }

        private async Task<object> Status(string messageHash)
        {
            ValidateMessageHash(messageHash);
            return await Get(this.UrlBase, "tx/" + this.BlockchainRID + "/" + messageHash + "/status");
        }

        public async Task<object> getDebug()
        {
            var ret = await Get(this.UrlBase, "_debug/");
            return ret;
        }

        public string testQuery(string queryName, params object[] queryObject)
        {
            string queryString = BuildQuery(queryObject);
            queryString = AppendQueryName(queryName, queryString);
            return queryString;
        }

        public async Task<object> Query(string queryName, params object[] queryObject)
        {
            string queryString = BuildQuery(queryObject);
            queryString = AppendQueryName(queryName, queryString);
            
            return await Post(this.UrlBase, "query/" + this.BlockchainRID, queryString);
        }

        private string AppendQueryName(string queryName, string queryString)
        {
            if (!String.IsNullOrEmpty(queryString))
            {
                queryString = queryString.Remove(queryString.Length - 1);
                queryString += String.Format(@", ""type"": ""{0}""", queryName);
                return queryString + " }";
            }
            else
            {
                return String.Format(@"{{""type"": ""{0}""}}", queryName);
            }
        }

        private static string BuildQuery(object queryObject, int layer = 0)
        {
            
            if (IsTuple(queryObject.GetType()))
            {
                Tuple<object, object> tuple = (Tuple<object, object>)queryObject;
                if (layer < 2)
                {
                    return String.Format(@"""{0}"": {1}", tuple.Item1, BuildQuery(tuple.Item2, layer + 1));
                }
                else
                {
                    string queryString = "[";
                    var queryItems = ToEnumerable(queryObject);
                    foreach (var queryItem in queryItems)
                    {
                        queryString += BuildQuery(queryItem, layer + 1) + ", ";
                    }
                    queryString = queryString.Remove(queryString.Length - 2) + "]";
                    return queryString;
                }
            }
            else if (queryObject is byte[])
            {
                return String.Format(@"""{0}""", Util.ByteArrayToString((byte[])queryObject));
            }
            else if (queryObject is System.Array)
            {
                System.Array arrayObj = (System.Array)queryObject;
                if (layer == 0 && arrayObj.Length == 0)
                {
                    return "";
                }
                else if (layer != 0 && arrayObj.Length == 0)
                {
                    return "[]";
                }

                string queryString = "";
                if (layer == 0)
                {
                    queryString = "{";
                }
                else
                {
                    queryString = "[";
                }

                foreach (var subQueryParam in arrayObj)
                {
                    queryString += BuildQuery(subQueryParam, layer + 1) + ", ";
                }

                if (layer == 0)
                {
                    queryString = queryString.Remove(queryString.Length - 2) + "}";
                }
                else
                {
                    queryString = queryString.Remove(queryString.Length - 2) + "]";
                }
                return queryString;
            }
            else if (queryObject is System.Int32)
            {
                return queryObject.ToString();
            }
            else if (queryObject is string)
            {
                return String.Format(@"""{0}""", (string)queryObject);
            }
            else
            {
                throw new Exception("Unknown query data type " + queryObject.GetType());
            }
        }

        private static IEnumerable<object> ToEnumerable(object tuple)
        {
            if (IsTuple(tuple.GetType()))
            {
                foreach (var prop in tuple.GetType()
                    .GetFields()
                    .Where(x => x.Name.StartsWith("Item")))
                {
                    yield return prop.GetValue(tuple);
                }
            }
            else
            {
                throw new ArgumentException("Not a tuple!");
            }
        }

        public static bool IsTuple(Type tuple)
        {
            if (!tuple.IsGenericType)
                return false;
            var openType = tuple.GetGenericTypeDefinition();
            return openType == typeof(ValueTuple<>)
                || openType == typeof(ValueTuple<,>)
                || openType == typeof(ValueTuple<,,>)
                || openType == typeof(ValueTuple<,,,>)
                || openType == typeof(ValueTuple<,,,,>)
                || openType == typeof(ValueTuple<,,,,,>)
                || openType == typeof(ValueTuple<,,,,,,>)
                || (openType == typeof(ValueTuple<,,,,,,,>) && IsTuple(tuple.GetGenericArguments()[7]));
        }

        public async Task<GTX.PostchainErrorControl> WaitConfirmation(string txRID)
        {
            
            var status = await this.Status(txRID);
            /*
            var statusString = status.status.ToObject<string>();
            switch (statusString)
            {
                case "confirmed":
                    return new GTX.PostchainErrorControl() { Error = false, ErrorMessage = "" };
                case "rejected":
                    return new GTX.PostchainErrorControl() { Error = true, ErrorMessage = "Message was rejected" };
                case "unknown":
                    await Task.Delay(511);
                    return await this.WaitConfirmation(txRID);
                case "waiting":
                    await Task.Delay(511);
                    return await this.WaitConfirmation(txRID);
                case "exception":
                    return new GTX.PostchainErrorControl() { Error = true, ErrorMessage = "HTTP Exception: " + status.message };
                default:
                    return new GTX.PostchainErrorControl() { Error = true, ErrorMessage = "Got unexpected response from server: " + statusString };
            }*/
            return new GTX.PostchainErrorControl();
        }

        public async Task<GTX.PostchainErrorControl> PostAndWaitConfirmation(string serializedTransaction, string txRID)
        {
            await this.PostTransaction(serializedTransaction);

            return await this.WaitConfirmation(txRID);
        }

        public async Task<object> Get(string urlBase, string path)
        {
            
            var rq = UnityWebRequest.Get(urlBase +  path);
            await rq.SendWebRequest();
            if (rq.isNetworkError)
            {
                return JsonConvert.DeserializeObject("{ 'status': 'exception', 'message': '" + rq.error + "' }");
            }
            else return JsonConvert.DeserializeObject(rq.downloadHandler.text);
            
        }

        private async Task<object> Post(string urlBase, string path, string jsonString)
        {
            var rq = UnityWebRequest.Post(urlBase + path, jsonString);
            await rq.SendWebRequest();
            if (rq.isNetworkError)
            {
                return JsonConvert.DeserializeObject("{ 'status': 'exception', 'message': '" + rq.error + "' }");
            }
            else return JsonConvert.DeserializeObject(rq.downloadHandler.text);
            
        }

        private void ValidateMessageHash(string messageHash)
        {
            if (messageHash == null)
            {
                throw new Exception("messageHash is not a Buffer");
            }

            if (messageHash.Length != 64)
            {
                throw new Exception("expected length 64 of messageHash, but got " + messageHash.Length);
            }
        }

        private string StringToHex(string stringValue)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in stringValue)
            {
                sb.Append(Convert.ToInt32(c).ToString("X"));
            }
            return sb.ToString();
        }
    }
}