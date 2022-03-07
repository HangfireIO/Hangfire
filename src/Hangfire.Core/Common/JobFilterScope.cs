// This file is part of Hangfire. Copyright © 2013-2014 Sergey Odinokov.
// 
// Permission to use, copy, modify, and/or distribute this software for any
// purpose with or without fee is hereby granted.
// 
// THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES WITH
// REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF MERCHANTABILITY
// AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY SPECIAL, DIRECT,
// INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES WHATSOEVER RESULTING FROM
// LOSS OF USE, DATA OR PROFITS, WHETHER IN AN ACTION OF CONTRACT, NEGLIGENCE OR
// OTHER TORTIOUS ACTION, ARISING OUT OF OR IN CONNECTION WITH THE USE OR
// PERFORMANCE OF THIS SOFTWARE.

using Hangfire.Client;
using Hangfire.Server;
using Hangfire.States;

namespace Hangfire.Common
{
    /// <summary>
    /// Defines values that specify the order in which Hangfire filters 
    /// run within the same filter type and filter order.
    /// </summary>
    /// 
    /// <remarks>
    /// Hangfire supports the following types of filters:
    /// 
    /// <list type="number">
    ///     <item>
    ///         <description>
    ///             Client / Server filters, which implement
    ///             <see cref="IClientFilter"/> and <see cref="IServerFilter"/>
    ///             interfaces respectively.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             State changing filters, which implement the
    ///             <see cref="IElectStateFilter"/> interface.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             State changed filters, which implement the
    ///             <see cref="IApplyStateFilter"/> interface.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             Client / Server exception filters, which implement
    ///             <see cref="IClientExceptionFilter"/> or 
    ///             <see cref="IServerExceptionFilter"/> interfaces
    ///             respectively.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// Порядок запуска указанных типов фильтров строго фиксирован, например,
    /// фильтры исключений всегда выполняются после всех остальных фильтров,
    /// а фильтры состояний всегда запускаются внутри клиентских и серверных
    /// фильтров.
    /// 
    /// Внутри же одного типа фильтров, порядок выполнения сначала определяется
    /// значением Order, а затем значением Scope. Перечисление <see cref="JobFilterScope"/> 
    /// определяет следующие значения (в порядке, в котором они будут выполнены):
    /// 
    /// <list type="number">
    ///     <item>
    ///         <description>
    ///             <see cref="JobFilterScope.Global"/>.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <see cref="Type"/>.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             <see cref="Method"/>.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// Для примера, клиентский фильтр, у которого свойство Order имеет значение 0,
    /// а значение filter scope равно <see cref="JobFilterScope.Global"/>,
    /// будет выполнен раньше фильтра с тем же самым значением Order,
    /// но c filter scope, равным <see cref="Type"/>.
    /// 
    /// Значения Scope задаются, в основном, в реализациях интерфейса
    /// <see cref="IJobFilterProvider"/>. Так, класс <see cref="JobFilterCollection"/>
    /// определяет значение Scope как <see cref="JobFilterScope.Global"/>.
    /// 
    /// Порядок выполнения фильтров одинакового типа, с одинаковым значением
    /// Order и с одинаковым scope, не оговаривается.
    /// </remarks>
    public enum JobFilterScope
    {
        /// <summary>
        /// Specifies an order before the <see cref="Type"/>.
        /// </summary>
        Global = 10,

        /// <summary>
        /// Specifies an order after the <see cref="Global"/> and
        /// before the <see cref="Method"/>.
        /// </summary>
        Type = 20,

        /// <summary>
        /// Specifies an order after the <see cref="Type"/>.
        /// </summary>
        Method = 30,
    }
}