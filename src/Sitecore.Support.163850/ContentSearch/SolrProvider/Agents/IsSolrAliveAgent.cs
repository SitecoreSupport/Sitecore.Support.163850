// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IsSolrAliveAgent.cs" company="Sitecore">
//   Copyright (c) Sitecore. All rights reserved.
// </copyright>
// <summary>
//   Defines the IsSolrAliveAgent type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Sitecore.Support.ContentSearch.SolrProvider.Agents
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Web.Hosting;
  using Sitecore.ContentSearch.Diagnostics;
  using Sitecore.Diagnostics;
  using Sitecore.StringExtensions;
  using Sitecore.Tasks;
  using Sitecore.ContentSearch.SolrProvider;
  using Sitecore.ContentSearch;

  // Got this class from one of support ticket on issue 391039.
  [UsedImplicitly]
  public class IsSolrAliveAgent : BaseAgent
  {
    [Obsolete("The field is no longer in use and will be removed in later release.")]
    public const string InitialFailState = "InitialFail";

    [Obsolete("The field is no longer in use and will be removed in later release.")]
    public const string AlwaysState = "Always";

    [Obsolete("The field is no longer in use and will be removed in later release.")]
    public const string OffState = "Off";

    private const string StatusRestart = "restart";
    private const string StatusSolrOk = "solrok";
    private const string StatusSolrFail = "solrfail";

    protected virtual AbstractLog Log { get; }

    [Obsolete("The property is no longer in use and will be removed in later release.")]
    protected virtual string ConnectionRecoveryStrategy
    {
      get
      {
        return SolrContentSearchManager.ConnectionRecoveryStrategy;
      }
    }

    public IsSolrAliveAgent() : this(CrawlingLog.Log)
    {
    }

    public IsSolrAliveAgent([NotNull] AbstractLog log)
    {
      Assert.ArgumentNotNull(log, nameof(log));
      this.Log = log;
    }

    [UsedImplicitly]
    public void Run()
    {
      var indexesCount = SolrStatus.GetIndexesForInitialization().Count;

      if (indexesCount <= 0)
      {
        this.Log.Debug("IsSolrAliveAgent: No indexes are pending for re-initialization. Terminating execution");
        return;
      }

      this.Log.Info("IsSolrAliveAgent: {0} indexes are pending for re-initialization. Checking SOLR status...".FormatWith(indexesCount));

      bool currentStatus = SolrStatus.OkSolrStatus();

      if (!currentStatus)
      {
        this.Log.Info("IsSolrAliveAgent: SOLR is unavailable. Terminating execution");
        return;
      }

      this.Log.Debug("IsSolrAliveAgent: Start indexes re-initialization");
      var reinitializedIndexes = new List<ISearchIndex>();
      foreach (var index in SolrStatus.GetIndexesForInitialization())
      {
        try
        {
          this.Log.Debug(" - Re-initializing index '{0}' ...".FormatWith(index.Name));
          index.Initialize();
          // Sitecore.Support.163850
          if ((index as SolrSearchIndex) == null)
          {
            Log.Debug(string.Format("Sitecore.Support.163850: '{0}' index is not SolrSearchIndex", index.Name));
          }
          else if ((index as SolrSearchIndex).IsInitialized)
          {
            this.Log.Debug(" - DONE");
            reinitializedIndexes.Add(index);
          }
        }
        catch (Exception ex)
        {
          this.Log.Warn("{0} index intialization failed".FormatWith(index.Name), ex);
        }
      }

      foreach (var index in reinitializedIndexes)
      {
        this.Log.Debug("IsSolrAliveAgent: Un-registering {0} index after successfull re-initialization...".FormatWith(index.Name));
        SolrStatus.UnsetIndexForInitialization(index);
        this.Log.Debug("IsSolrAliveAgent: DONE");
      }

      var unInitializedIndexes = SolrStatus.GetIndexesForInitialization();
      this.Log.Info("IsSolrAliveAgent: {0} indexes have been re-initialized, {1} still need to be initialized.".FormatWith(reinitializedIndexes.Count, unInitializedIndexes.Count));

      this.MessageUninitializedIndexesState(unInitializedIndexes);
    }

    /// <summary>
    /// Outputs details to logs about what indexes have not been initialized.
    /// </summary>
    /// <param name="uninitializedIndexes">A list of indexes which have not been initialized yet.</param>
    protected virtual void MessageUninitializedIndexesState([NotNull] List<ISearchIndex> uninitializedIndexes)
    {
      Debug.ArgumentNotNull(uninitializedIndexes, nameof(uninitializedIndexes));

      if (uninitializedIndexes.Count == 0)
      {
        this.Log.Debug("IsSolrAliveAgent: All indexes have been initialized.");
        return;
      }

      this.Log.Debug(() =>
      {
        var indexList = string.Join(", ", uninitializedIndexes.Select(ind => ind.Name));

        return "IsSolrAliveAgent: Indexes which require initialization: {0}".FormatWith(indexList);
      }
      );
    }

    [Obsolete("The method is no longer in use and will be removed in later release.")]
    protected virtual void StatusLogging(string parameter)
    {
      if (parameter == StatusRestart)
      {
        this.Log.Warn("Solr connection was restored. The restart is initiated to initialize Solr provider inside <initilize> pipeline.");
      }
      else if ((parameter != StatusSolrOk) && (parameter == StatusSolrFail))
      {
        this.Log.Warn("Solr connection failed.");
      }
    }

    [Obsolete("The method is no longer in use and will be removed in later release.")]
    protected virtual void RestartTheProcess()
    {
      this.Log.Warn("IsSolrAliveAgent: Initiating shutdown...");
      HostingEnvironment.InitiateShutdown();
    }
  }
}
