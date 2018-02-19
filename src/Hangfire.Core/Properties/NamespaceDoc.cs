// ReSharper disable CheckNamespace

using System.Runtime.CompilerServices;

namespace Hangfire
{
    /// <summary>
    /// The <see cref="Hangfire"/> namespace contains high-level types for configuring,
    /// creating and processing background jobs, such as <see cref="GlobalConfiguration"/>,
    /// <see cref="BackgroundJob"/> and <see cref="BackgroundJobServer"/>.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Annotations
{
    /// <summary>
    /// The <see cref="Hangfire.Annotations"/> namespace contains attributes that enable
    /// additional code inspections in design time with JetBrains ReSharper.
    /// </summary>
    /// <remarks>
    /// To enable annotations, open ReSharper options → Code Inspections → Code Annotations 
    /// and add the <see cref="Hangfire.Annotations"/> namespace to the corresponding list.
    /// </remarks>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Client
{
    /// <summary>
    /// The <see cref="Hangfire.Client"/> namespace contains types that allow you to
    /// customize the background job creation pipeline using the <see cref="IClientFilter"/>,
    /// or define your own creation process by implementing the <see cref="IBackgroundJobFactory"/>
    /// interface.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Common
{
    /// <summary>
    /// The <see cref="Hangfire.Common"/> namespace provides base types for background
    /// job filters, such as <see cref="JobFilterAttribute"/>, and some helper classes.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Dashboard
{
    /// <summary>
    /// The <see cref="Hangfire.Dashboard"/> namespace contains types that allow you to
    /// restrict an access to the Dashboard UI by implementing the <see cref="IDashboardAuthorizationFilter"/>
    /// interface, as well as customize it by adding new pages, menu items, metrics, routes.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Dashboard.Pages
{
    /// <summary>
    /// The <see cref="Hangfire.Dashboard.Pages"/> namespace contains the <see cref="LayoutPage"/>
    /// class, layout for all the Dashboard UI pages.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Logging
{
    /// <summary>
    /// The Hangfire.Logging namespaces contain types that allow you to 
    /// integrate Hangfire's logging with your projects as well as use it 
    /// to log custom messages.
    /// </summary>
    [CompilerGenerated]
    class NamespaceGroupDoc
    {
    }

    /// <summary>
    /// The <see cref="Hangfire.Logging"/> namespace contains types that allow you to 
    /// integrate Hangfire's logging with your projects as well as use it 
    /// to log custom messages.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Logging.LogProviders
{
    /// <summary>
    /// The <see cref="Hangfire.Logging.LogProviders"/> namespace contains types for 
    /// supporting most popular logging frameworks to simplify the logging integration 
    /// with your projects.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Server
{
    /// <summary>
    /// The <see cref="Hangfire.Server"/> namespace contains types that are responsible
    /// for background processing. You may use them to customize your processing pipeline
    /// by implementing the <see cref="IServerFilter"/> interface or define your own 
    /// continuously-running background processes by implementing the <see cref="IBackgroundProcess"/> 
    /// as well as create completely custom instances of <see cref="BackgroundProcessingServer"/>.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.States
{
    /// <summary>
    /// The <see cref="Hangfire.States"/> namespace contains types that describe
    /// background job states and the transitions between them. You can implement
    /// custom <see cref="IElectStateFilter"/> or <see cref="IApplyStateFilter"/>
    /// to customize the state changing pipeline, or define your own state by 
    /// implementing the  <see cref="IState"/> interface.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Storage
{
    /// <summary>
    /// The Hangfire.Storage namespaces contain abstract types like <see cref="JobStorage"/>,
    /// <see cref="IStorageConnection"/> and <see cref="IWriteOnlyTransaction"/> for
    /// querying and modifying the underlying background job storage. 
    /// These types are also used to implement support for other persistent storages.
    /// </summary>
    [CompilerGenerated]
    class NamespaceGroupDoc
    {
    }

    /// <summary>
    /// The Hangfire.Storage namespaces contain abstract types like <see cref="JobStorage"/>,
    /// <see cref="IStorageConnection"/> and <see cref="IWriteOnlyTransaction"/> for
    /// querying and modifying the underlying background job storage. 
    /// These types are also used to implement support for other persistent storages.
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}

namespace Hangfire.Storage.Monitoring
{
    /// <summary>
    /// The <see cref="Hangfire.Storage.Monitoring"/> provides data transfer objects 
    /// for the <see cref="IMonitoringApi"/> interface. 
    /// </summary>
    /// <remarks>
    /// I have no idea why I placed these types to a separate namespace, they should 
    /// be moved to the parent <see cref="Hangfire.Storage"/> namespace in version 2.0.
    /// </remarks>
    [CompilerGenerated]
    class NamespaceDoc
    {
    }
}