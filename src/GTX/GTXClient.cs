using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Chromia.Postchain.Client.GTX
{
    public struct PostchainErrorControl
    {
        public bool Error;
        public string ErrorMessage;
    }

    [Serializable]
    public class QueryContent
    {
        public bool __postchainerror__;
        public string message;
    }

    public class GTXClient
    {
        private RESTClient RestApiClient;
        private string BlockchainRID;

        ///<summary>
        ///Create new GTXClient object.
        ///</summary>
        ///<param name = "restApiClient">Initialized RESTCLient.</param>
        ///<param name = "blockchainRID">RID of blockchain.</param>
        public GTXClient(RESTClient restApiClient, string blockchainRID)
        {
            this.RestApiClient = restApiClient;
            this.BlockchainRID = blockchainRID;
        }

        ///<summary>
        ///Create a new Transaction.
        ///</summary>
        ///<param name = "signers">Array of signers (can be null).</param>
        ///<returns>New Transaction object.</returns>
        public Transaction NewTransaction(byte[][] signers)
        {
            Gtx newGtx = new Gtx(this.BlockchainRID);

            foreach(byte[] signer in signers)
            {
                newGtx.AddSignerToGtx(signer);
            }

            Transaction req = new Transaction(newGtx, this.RestApiClient);
            
            return req;
        }

        ///<summary>
        ///Send a query to the node.
        ///</summary>
        ///<param name = "queryName">Name of the query to be called.</param>
        ///<param name = "queryObject">List of parameter pairs of query parameter name and its value. For example {"city", "Hamburg"}.</param>
        ///<returns>Task, which returns the query return content.</returns>
        public async Task<(object content, PostchainErrorControl control)> Query (string queryName, params object[] queryObject)
        {
            PostchainErrorControl queryError = new PostchainErrorControl();
            var jsonStr = "";
            try
            {
                var queryContent = await this.RestApiClient.Query(queryName, queryObject);
                jsonStr = JsonUtility.ToJson(queryContent);
                //QueryContent query = JsonUtility.FromJson<QueryContent>(jsonStr);
            } catch (Exception e)
            {
                queryError.Error = true;
                queryError.ErrorMessage = e.Message;
            }
            
           
            return (jsonStr, new PostchainErrorControl());
        } 


    }
}