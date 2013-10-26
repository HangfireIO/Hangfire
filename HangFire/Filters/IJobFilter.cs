using System.Diagnostics.CodeAnalysis;

namespace HangFire.Filters
{
    /// <summary>
    /// Acts as a marker for all the filters.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces")]
    public interface IJobFilter
    {
    }
}