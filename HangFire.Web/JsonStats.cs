using System.Linq;
using System.Text;
using System.Web;
using ServiceStack.Text;

namespace HangFire.Web
{
    internal class JsonStats : GenericHandler
    {
        public override void ProcessRequest()
        {
            var response = JobStorage.GetStatistics();

            using (JsConfig.With(emitCamelCaseNames: true))
            {
                var serialized = JsonSerializer.SerializeToString(response);
                Response.ContentType = "application/json";
                Response.ContentEncoding = Encoding.UTF8;
                Response.Write(serialized);
                // TODO: use Response.End();
            }
        }
    }
}
