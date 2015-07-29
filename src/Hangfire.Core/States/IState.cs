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

using System.Collections.Generic;
using Hangfire.Annotations;

namespace Hangfire.States
{
    /// <summary>
    /// Предоставляет базовые члены для описания состояния фоновой 
    /// задачию.
    /// </summary>
    /// <remarks>
    /// Описывает, в каком состоянии находилась или находится фоновая задача,
    /// реализатор может содержать любые пользовательские данные и сохранять
    /// их через соответствующий метод. 
    /// 
    /// Состояния позволяют отследить весь жизненный цикл задачи.
    /// 
    /// Фильтры состояний позволяют добавлять дополнительную логику при
    /// смене состояний, например, записывать или удалять то или иное значение
    /// в хранилище в пределах транзакции.
    /// 
    /// Состояние определяет процесс дальнейшей обработки фоновой задачи. Так,
    /// при смене состояния на EnqueuedState, текущая задача записывается в 
    /// соответствующую очередь, которая прослушивается воркером.
    /// </remarks>
    public interface IState
    {
        /// <summary>
        /// Получает строковый идентификатор состояния. Должен быть уникальным,
        /// регистро-независимым. Определяет текущее состояние.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the human-readable reason of a state transition.
        /// </summary>
        /// <remarks>
        /// <para>The reason is usually displayed in the Dashboard UI to simplify 
        /// the understanding of a background job lifecycle by providing a 
        /// human-readable text that explains why a background job is moved
        /// to the corresponding state. Here are some examples:</para>
        /// <list type="bullet">
        ///     <item>
        ///         <i>Can not change the state of a job to 'Enqueued': target 
        ///         method was not found</i>
        ///     </item>
        ///     <item><i>Exceeded the maximum number of retry attempts</i></item>
        /// </list>
        /// Reason value is usually not hard-coded in a state implementation,
        /// allowing users to change it when creating an instance of a state.
        /// </remarks>
        [CanBeNull]
        string Reason { get; }

        /// <summary>
        /// Determines 
        /// Объявляет состояние конечным. Задача в конечном состоянии помечается
        /// на удаление через определенный интервал времени, который настраивается
        /// в фильтрах.
        /// </summary>
        bool IsFinal { get; }

        /// <summary>
        /// Объявляет, будет ли выбрасываться исключение при попытке
        /// перевести задачу в данное состояние, если тип или метод
        /// задачи не найден.
        /// </summary>
        bool IgnoreJobLoadException { get; }

        /// <summary>
        /// Получает словарь с сериализованными значениями свойств свойствами состояния.
        /// состояния. Полученные данные используются в мониторинге для предоставления
        /// отладочной информации, а также в некоторых других случаях, например, для
        /// работы токенов отмены. Записанные данные доступны через метод GetStateData.
        /// </summary>
        /// <returns></returns>
        Dictionary<string, string> SerializeData();
    }
}
