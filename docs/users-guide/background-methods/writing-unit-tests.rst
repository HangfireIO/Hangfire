Writing unit tests
===================

I will not tell you anything related to unit testing background methods, because Hangfire does not add any specific changes to them (except ``IJobCancellationToken`` interface parameter). Use your favourite tools and write unit tests for them as usual. This section describes how to test that background jobs were created.

All the code examples use the static ``BackgroundJob`` class to tell you how to do this or that stuff, because it is simple for demonstrational purposes. But when you want to test a method that invokes static methods, it becomes a pain.

But don't worry – the ``BackgroundJob`` class is just a facade for the ``IBackgroundJobClient`` interface and its default implementation – ``BackgroundJobClient`` class. If you want to write unit tests, use them. For example, consider the following controller that is being used to enqueue background jobs:

.. code-block:: c#

    public class HomeController : Controller
    {
        private readonly IBackgroundJobClient _jobClient;

        // For ASP.NET MVC
        public HomeController()
            : this(new BackgroundJobClient())
        {
        }

        // For unit tests
        public HomeController(IBackgroundJobClient jobClient)
        {
            _jobClient = jobClient;
        }

        public ActionResult Create(Comment comment)
        {
            ...
            _jobClient.Enqueue(() => CheckForSpam(comment.Id));
            ...
        }
    }

Simple, yeah. Now you can use any mocking framework, for example, `Moq <https://github.com/Moq/moq4>`_ to provide mocks and check the invocations. The ``IBackgroundJobClient`` interface provides only one method for creating a background job – the ``Create`` method, that takes a ``Job`` class instance, that represents the information about the invocation, and a ``IState`` interface implementation to know the creating job's state.

.. code-block:: c#

    [TestMethod]
    public void CheckForSpamJob_ShouldBeEnqueued()
    {
        // Arrange
        var client = new Mock<IBackgroundJobClient>();
        var controller = new HomeController(client.Object);
        var comment = CreateComment();

        // Act
        controller.Create(comment);

        // Assert
        _client.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == "CheckForSpam" && job.Arguments[0] == comment.Id.ToString()),
            It.IsAny<EnqueuedState>());
    }