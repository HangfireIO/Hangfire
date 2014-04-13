using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using HangFire.Sample.Highlighter.Models;
using PagedList;
using StackExchange.Profiling;

namespace HangFire.Sample.Highlighter.Controllers
{
    public class HomeController : Controller
    {
        private readonly HighlighterDbContext _db = new HighlighterDbContext();

        [HttpGet]
        public ActionResult Index(int? page)
        {
            var pageNumber = page ?? 1;
            var snippets = _db.Snippets
                .OrderByDescending(x => x.CreatedAt)
                .ToPagedList(pageNumber, 10);

            return View(snippets);
        }

        [HttpGet]
        public ActionResult Snippet(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var snippet = _db.Snippets.Find(id);

            if (snippet == null)
            {
                return HttpNotFound();
            }

            return View(snippet);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var snippet = new Snippet();
            snippet.Source = @"[HttpGet]
public ActionResult Details(int? id)
{
    if (id == null)
    {
        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
    }

    var snippet = _context.Snippets.Find(id);

    if (snippet == null)
    {
        return HttpNotFound();
    }

    return View(snippet);
}
";

            return View(snippet);
        }

        [HttpPost, ActionName("Create")]
        [MultipleButton(Name = "method", Argument = "Sync")]
        public ActionResult Create(Snippet snippet)
        {
            if (ModelState.IsValid)
            {
                snippet.CreatedAt = DateTime.UtcNow;

                using (MiniProfiler.StepStatic("Web service sync call"))
                {
                    snippet.HighlightedSource = HighlightSource(snippet.Source);
                    snippet.HighlightedAt = DateTime.UtcNow;
                }

                _db.Snippets.Add(snippet);
                _db.SaveChanges();

                return RedirectToAction("Snippet", new { id = snippet.Id });
            }

            return View("Create", snippet);
        }

        [HttpPost, ActionName("Create")]
        [MultipleButton(Name = "method", Argument = "Async")]
        public async Task<ActionResult> CreateAsync(Snippet snippet)
        {
            if (ModelState.IsValid)
            {
                snippet.CreatedAt = DateTime.UtcNow;

                using (MiniProfiler.StepStatic("Web service async call"))
                {
                    snippet.HighlightedSource = await HighlightSourceAsync(snippet.Source);
                    snippet.HighlightedAt = DateTime.UtcNow;
                }

                _db.Snippets.Add(snippet);
                _db.SaveChanges();

                return RedirectToAction("Snippet", new { id = snippet.Id });
            }

            return View("Create", snippet);
        }

        [HttpPost, ActionName("Create")]
        [MultipleButton(Name = "method", Argument = "ThreadPool")]
        public ActionResult CreateThreadPool(Snippet snippet)
        {
            if (ModelState.IsValid)
            {
                snippet.CreatedAt = DateTime.UtcNow;
                _db.Snippets.Add(snippet);
                _db.SaveChanges();

                var snippetId = snippet.Id;
                Task.Run(() => HighlightSnippet(snippetId));

                return RedirectToAction("Snippet", new { id = snippet.Id });
            }

            return View("Create", snippet);
        }

        [HttpPost, ActionName("Create")]
        [MultipleButton(Name = "method", Argument = "BackgroundJob")]
        public ActionResult CreateBackgroundJob(Snippet snippet)
        {
            if (ModelState.IsValid)
            {
                snippet.CreatedAt = DateTime.UtcNow;
                _db.Snippets.Add(snippet);
                _db.SaveChanges();

                BackgroundJob.Enqueue(() => HighlightSnippet(snippet.Id));

                return RedirectToAction("Snippet", new { id = snippet.Id });
            }

            return View("Create", snippet);
        }

        public static void HighlightSnippet(int snippetId)
        {
            using (var context = new HighlighterDbContext())
            {
                var snippet = context.Snippets.Find(snippetId);
                snippet.HighlightedSource = HighlightSource(snippet.Source);
                snippet.HighlightedAt = DateTime.UtcNow;

                context.SaveChanges();
            }
        }

        private static string HighlightSource(string source)
        {
            // Microsoft.Net.Http does not provide synchronous API,
            // so we are using Nito.AsyncEx package to simplify
            // sync-over-async calls.
            // Consider this line as simple async call.
            return RunSync(() => HighlightSourceAsync(source));
        }

        private static async Task<string> HighlightSourceAsync(string source)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    @"http://hilite.me/api",
                    new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        { "lexer", "c#" },
                        { "style", "vs" },
                        { "code", source }
                    }));

                return await response.Content.ReadAsStringAsync();
            }
        }

        private static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return Task.Run<Task<TResult>>(func).Unwrap().GetAwaiter().GetResult();
        }

        [HttpPost]
        public ActionResult Clear()
        {
            var objectContext = ((IObjectContextAdapter)_db).ObjectContext;
            objectContext.ExecuteStoreCommand("TRUNCATE TABLE [Snippet]");

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}