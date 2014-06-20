Tracking the progress
======================

There are two ways to implement this task: polling and pushing. Polling is easier to understand, but server push is a more comfortable way, because it help you to avoid unnecessary calls to server. Plus, `SignalR <http://signalr.net>`_ greatly simplifies the latter task.

I'll show you a simple example, where client needs only to check for a job completion. You can see the full sample in `HangFire.Highlighter <https://github.com/odinserj/HangFire.Highlighter>`_ project. 

Highlighter has the following background job that calls an external web service to highlight code snippets:

.. code-block:: c#

    public void Highlight(int snippetId)
    {
        var snippet = _dbContext.CodeSnippets.Find(snippetId);
        if (snippet == null) return;

        snippet.HighlightedCode = HighlightSource(snippet.SourceCode);
        snippet.HighlightedAt = DateTime.UtcNow;

        _dbContext.SaveChanges();
    }

Polling for a job status
-------------------------

When can we say that this job is incomplete? When the ``HighlightedCode`` property value *is null*. When can we say it was completed? When the specified property *has value* – this example is simple enough.

So, when we are rendering the code snippet that is not highlighted yet, we need to render a JavaScript that makes ajax calls with some interval to some controller action that returns the job status (completed or not) until the job was finished.

.. code-block:: c#

    public ActionResult CheckHighlighted(int snippetId)
    {
        var snippet = _db.Snippets.Find(snippetId);

        return snippet.HighlightedCode == null
            ? new HttpStatusCodeResult(HttpStatusCode.NoContent)
            : Content(snippet.HighlightedCode);
    }

When code snippet become highlighted, we can stop the polling and show the highlighted code. But if you want to track progress of your job, you need to perform extra steps:

* Add a column ``Status`` to the snippets table.
* Update this column during background work.
* Check this column in polling action.

But there is a better way.

Using server push with SignalR
-------------------------------

Why we need to poll our server? It can say when the snippet become highlighted himself. And `SignalR <http://signalr.net>`_, an awesome library to perform server push, will help us. If you don't know about this library, look at it, and you'll love it. Really.

I don't want to include all the code snippets here (you can look at the sources of this sample). I'll show you only the two changes that you need, and they are incredibly simple.

First, you need to add a hub:

.. code-block:: c#

    public class SnippetHub : Hub
    {
        public async Task Subscribe(int snippetId)
        {
            await Groups.Add(Context.ConnectionId, GetGroup(snippetId));

            // When a user subscribes a snippet that was already 
            // highlighted, we need to send it immediately, because
            // otherwise she will listen for it infinitely.
            using (var db = new HighlighterDbContext())
            {
                var snippet = await db.CodeSnippets
                    .Where(x => x.Id == snippetId && x.HighlightedCode != null)
                    .SingleOrDefaultAsync();

                if (snippet != null)
                {
                    Clients.Client(Context.ConnectionId)
                        .highlight(snippet.Id, snippet.HighlightedCode);
                }
            }
        }

        public static string GetGroup(int snippetId)
        {
            return "snippet:" + snippetId;
        }
    }

And second, you need to make a small change to your background job method:

.. code-block:: c#

    public void Highlight(int snippetId)
    {
        ...
        _dbContext.SaveChanges();

        var hubContext = GlobalHost.ConnectionManager
            .GetHubContext<SnippetHub>();

        hubContext.Clients.Group(SnippetHub.GetGroup(snippet.Id))
            .highlight(snippet.HighlightedCode);
    }

And that's all! When user opens a page that contains unhighlighted code snippet, his browser connects to the server, subscribes for code snippet notification and waits for update notifications. When background job is about to be done, it sends the highlighted code to all subscribed users.

If you want to add a progress tracking, just add it. No additional tables and columns required, only JavaScript function. This is example of real and reliable asynchrony for ASP.NET applications without taking much effort to it.