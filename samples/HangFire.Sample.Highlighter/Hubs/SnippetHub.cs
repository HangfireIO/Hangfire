using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using HangFire.Sample.Highlighter.Models;
using Microsoft.AspNet.SignalR;

namespace HangFire.Sample.Highlighter.Hubs
{
    public class SnippetHub : Hub
    {
        public async Task Subscribe(List<string> snippets)
        {
            var snippetIds = snippets.Select(int.Parse).ToArray();
            var groups = snippetIds.Select(GetGroup).ToArray();

            foreach (var @group in groups)
            {
                await Groups.Add(Context.ConnectionId, group);
            }

            using (var db = new HighlighterDbContext())
            { 
                var highlighted = await db.Snippets
                    .Where(x => snippetIds.Contains(x.Id) && x.HighlightedSource != null)
                    .ToListAsync();

                foreach (var snippet in highlighted)
                {
                    Clients.Client(Context.ConnectionId)
                        .highlight(snippet.Id, snippet.HighlightedSource);
                }
            }
        }

        public static string GetGroup(int snippetId)
        {
            return "snippet:" + snippetId;
        }
    }

}