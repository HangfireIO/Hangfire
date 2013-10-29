namespace HangFire.Filters
{
    /// <summary>
    /// Defines values that specify the order in which HangFire filters 
    /// run within the same filter type and filter order.
    /// </summary>
    /// 
    /// <remarks>
    /// HangFire supports the following types of filters:
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
    ///             <see cref="IStateChangingFilter"/> interface.
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <description>
    ///             State changed filters, which implement the
    ///             <see cref="IStateChangedFilter"/> interface.
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
    ///             <see cref="JobFilterScope.Invoke"/>.
    ///         </description>
    ///     </item>
    /// </list>
    /// 
    /// Для примера, клиентский фильтр, у которого свойство Order имеет значение 0,
    /// а значение filter scope равно <see cref="JobFilterScope.Global"/>,
    /// будет выполнен раньше фильтра с тем же самым значением Order,
    /// но c filter scope, равным <see cref="JobFilterScope.Invoke"/>.
    /// 
    /// Значения Scope задаются, в основном, в реализациях интерфейса
    /// <see cref="IJobFilterProvider"/>. Так, класс <see cref="GlobalJobFilterCollection"/>
    /// определяет значение Scope как <see cref="JobFilterScope.Global"/>.
    /// 
    /// Порядок выполнения фильтров одинакового типа, с одинаковым значением
    /// Order и с одинаковым scope, не оговаривается.
    /// </remarks>
    public enum JobFilterScope
    {
        /// <summary>
        /// Specifies an order before the <see cref="Invoke"/>.
        /// </summary>
        Global = 10,

        /// <summary>
        /// Specifies an order after the <see cref="Global"/>.
        /// </summary>
        Invoke = 20,
    }
}