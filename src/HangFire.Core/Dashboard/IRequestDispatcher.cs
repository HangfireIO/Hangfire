using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace HangFire.Dashboard
{
    public interface IRequestDispatcher
    {
        Task Dispatch(IOwinContext context, Match match);
    }
}
