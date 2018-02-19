// This file is part of Hangfire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// Hangfire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// Hangfire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with Hangfire. If not, see <http://www.gnu.org/licenses/>.

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