using Sitecore;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.Diagnostics;
using Sitecore.Tasks;
using System;
using System.Collections.Generic;
using System.Web.Hosting;

namespace Sitecore.Support.ContentSearch.SolrProvider.Agents
{
    [UsedImplicitly]
    public class IsSolrAliveAgent : BaseAgent
    {
        [Obsolete("The field is no longer in use and will be removed in later release.")]
        public const string AlwaysState = "Always";
        [Obsolete("The field is no longer in use and will be removed in later release.")]
        public const string InitialFailState = "InitialFail";
        [Obsolete("The field is no longer in use and will be removed in later release.")]
        public const string OffState = "Off";
        private const string StatusRestart = "restart";
        private const string StatusSolrFail = "solrfail";
        private const string StatusSolrOk = "solrok";

        [Obsolete("The method is no longer in use and will be removed in later release.")]
        protected virtual void RestartTheProcess()
        {
            HostingEnvironment.InitiateShutdown();
        }

        [UsedImplicitly]
        public void Run()
        {
            int count = SolrStatus.GetIndexesForInitialization().Count;
            if (count <= 0)
            {
                Log.Info("IsSolrAliveAgent: No indexes are pending for re-initialization. Terminating execution", this);
            }
            else
            {
                Log.Info(string.Format("IsSolrAliveAgent: {0} indexes are pending for re-initialization. Checking SOLR status...", count), this);
                if (!SolrStatus.OkSolrStatus())
                {
                    Log.Info("IsSolrAliveAgent: SOLR is unavailable. Terminating execution", this);
                }
                else
                {
                    Log.Debug("IsSolrAliveAgent: Start indexes re-initialization");
                    List<ISearchIndex> list = new List<ISearchIndex>();
                    foreach (ISearchIndex index in SolrStatus.GetIndexesForInitialization())
                    {
                        try
                        {
                            Log.Debug(string.Format(" - Re-initializing index '{0}' ...", index.Name), this);
                            index.Initialize();
                            // Sitecore.Support.163850
                            if ((index as SolrSearchIndex) == null)
                            {
                                Log.Debug(string.Format("Sitecore.Support.163850: '{0}' index is not SolrSearchIndex", index.Name), this);
                            }
                            else if ((index as SolrSearchIndex).IsInitialized)
                            {
                                Log.Debug(" - DONE", this);
                                list.Add(index);
                            }
                        }
                        catch (Exception exception)
                        {
                            Log.Warn(string.Format("{0} index intialization failed", index.Name), exception, this);
                        }
                    }
                    foreach (ISearchIndex index2 in list)
                    {
                        Log.Debug(string.Format("IsSolrAliveAgent: Un-registering {0} index after successfull re-initialization...", index2.Name), this);
                        SolrStatus.UnsetIndexForInitialization(index2);
                        Log.Debug("IsSolrAliveAgent: DONE", this);
                    }
                }
            }
        }

        [Obsolete("The method is no longer in use and will be removed in later release.")]
        protected virtual void StatusLogging(string parameter)
        {
            if (parameter == "restart")
            {
                Log.Warn("Solr connection was restored. The restart is initiated to initialize Solr provider inside <initilize> pipeline.", this);
            }
            else if ((parameter != "solrok") && (parameter == "solrfail"))
            {
                Log.Warn("Solr connection failed.", this);
            }
        }

        [Obsolete("The property is no longer in use and will be removed in later release.")]
        protected virtual string ConnectionRecoveryStrategy
        {
            get
            {
                return SolrContentSearchManager.ConnectionRecoveryStrategy;
            }
        }
    }
}