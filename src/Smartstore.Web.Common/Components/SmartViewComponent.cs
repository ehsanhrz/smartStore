﻿#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Events;

namespace Smartstore.Web.Components
{
    public abstract class SmartViewComponent : ViewComponent
    {
        private ILogger _logger = default!;
        private Localizer _localizer = default!;
        private ICommonServices _services = default!;

        private static readonly ContentViewComponentResult _emptyResult = new(string.Empty);

        public ILogger Logger
        {
            get => _logger ??= HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger(GetType());
        }

        public Localizer T
        {
            get => _localizer ??= HttpContext.RequestServices.GetRequiredService<IText>().Get;
        }

        public ICommonServices Services
        {
            get => _services ??= HttpContext.RequestServices.GetRequiredService<ICommonServices>();
        }

        /// <summary>
        /// Gets a value indicating whether view rendering events should be published.
        /// </summary>
        protected virtual bool PublishEvents { get; } = true;

        #region Results

        /// <summary>
        /// Returns a result which will render HTML encoded text.
        /// </summary>
        /// <param name="content">The content, will be HTML encoded before output.</param>
        /// <returns>A <see cref="ContentViewComponentResult"/>.</returns>
        public new IViewComponentResult Content(string content)
        {
            IViewComponentResult result = base.Content(content);
            PublishResultExecutingEvent(ref result);
            return result;
        }

        /// <summary>
        /// Returns a result which will render raw (unencoded) HTML content.
        /// </summary>
        /// <param name="content">The HTML content.</param>
        /// <returns>A <see cref="HtmlContentViewComponentResult"/>.</returns>
        public IViewComponentResult HtmlContent(string content)
        {
            IViewComponentResult result = new HtmlContentViewComponentResult(new HtmlString(content));
            PublishResultExecutingEvent(ref result);
            return result;
        }

        /// <summary>
        /// Returns a result which will render raw (unencoded) HTML content.
        /// </summary>
        /// <param name="content">The HTML content.</param>
        /// <returns>A <see cref="HtmlContentViewComponentResult"/>.</returns>
        public IViewComponentResult HtmlContent(IHtmlContent content)
        {
            IViewComponentResult result = new HtmlContentViewComponentResult(content);
            PublishResultExecutingEvent(ref result);
            return result;
        }

        /// <summary>
        /// Returns a result which will render the partial view with name <c>&quot;Default&quot;</c>.
        /// </summary>
        /// <returns>A <see cref="ViewViewComponentResult"/>.</returns>
        public new IViewComponentResult View()
        {
            IViewComponentResult result = base.View();
            PublishResultExecutingEvent(ref result);
            return result;
        }

        /// <summary>
        /// Returns a result which will render the partial view with name <paramref name="viewName"/>.
        /// </summary>
        /// <param name="viewName">The name of the partial view to render.</param>
        /// <returns>A <see cref="ViewViewComponentResult"/>.</returns>
        public new IViewComponentResult View(string? viewName)
        {
            IViewComponentResult result = base.View(viewName);
            PublishResultExecutingEvent(ref result);
            return result;
        }

        /// <summary>
        /// Returns a result which will render the partial view with name <c>&quot;Default&quot;</c>.
        /// </summary>
        /// <param name="model">The model object for the view.</param>
        /// <returns>A <see cref="ViewViewComponentResult"/>.</returns>
        public new IViewComponentResult View<TModel>(TModel? model)
        {
            IViewComponentResult result = base.View(model);
            PublishResultExecutingEvent(ref result);
            return result;
        }

        /// <summary>
        /// Returns a result which will render the partial view with name <paramref name="viewName"/>.
        /// </summary>
        /// <param name="viewName">The name of the partial view to render.</param>
        /// <param name="model">The model object for the view.</param>
        /// <returns>A <see cref="ViewViewComponentResult"/>.</returns>
        public new IViewComponentResult View<TModel>(string? viewName, TModel? model)
        {
            IViewComponentResult result = base.View(viewName, model);
            PublishResultExecutingEvent(ref result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected IViewComponentResult Empty() => _emptyResult;

        private void PublishResultExecutingEvent(ref IViewComponentResult result)
        {
            // Give integrators the chance to react to component rendering.
            if (PublishEvents)
            {
                var e = new ViewComponentResultExecutingEvent(ViewComponentContext, result);
                
                Services.EventPublisher.Publish(e);
                if (e.Result != result && e.Result != null) 
                {
                    result = e.Result;
                }
            }
        }

        #endregion

        #region Notify

        /// <summary>
        /// Pushes an info message to the notification queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="durable">A value indicating whether the message should be persisted for the next request</param>
        protected virtual void NotifyInfo(string message, bool durable = true)
        {
            Services.Notifier.Information(message, durable);
        }

        /// <summary>
        /// Pushes a warning message to the notification queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="durable">A value indicating whether the message should be persisted for the next request</param>
        protected virtual void NotifyWarning(string message, bool durable = true)
        {
            Services.Notifier.Warning(message, durable);
        }

        /// <summary>
        /// Pushes a success message to the notification queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="durable">A value indicating whether the message should be persisted for the next request</param>
        protected virtual void NotifySuccess(string message, bool durable = true)
        {
            Services.Notifier.Success(message, durable);
        }

        /// <summary>
        /// Pushes an error message to the notification queue
        /// </summary>
        /// <param name="message">Message</param>
        /// <param name="durable">A value indicating whether the message should be persisted for the next request</param>
        protected virtual void NotifyError(string message, bool durable = true)
        {
            Services.Notifier.Error(message, durable);
        }

        /// <summary>
        /// Pushes an error message to the notification queue
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="durable">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="logException">A value indicating whether the exception should be logged</param>
        protected virtual void NotifyError(Exception exception, bool durable = true, bool logException = true)
        {
            if (logException)
            {
                LogException(exception);
            }

            Services.Notifier.Error(exception.ToAllMessages().HtmlEncode(), durable);
        }

        /// <summary>
        /// Pushes an error message to the notification queue that the access to a resource has been denied
        /// </summary>
        /// <param name="durable">A value indicating whether a message should be persisted for the next request</param>
        /// <param name="log">A value indicating whether the message should be logged</param>
        protected virtual void NotifyAccessDenied(bool durable = true, bool log = true)
        {
            var message = T("Admin.AccessDenied.Description");

            if (log)
            {
                Logger.Error(message);
            }

            Services.Notifier.Error(message, durable);
        }

        #endregion

        #region Exceptions

        /// <summary>
        /// Logs an exception
        /// </summary>
        private void LogException(Exception ex)
        {
            Logger.Error(ex);
        }

        #endregion
    }
}
