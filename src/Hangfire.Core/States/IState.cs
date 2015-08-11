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
    /// Provides the essential members for describing a background job state.
    /// </summary>
    /// <remarks>
    /// <para>Background processing in Hangfire is all about moving a background job
    /// from one state to another. States are used to clearly decide what to do
    /// with a background job. For example, <see cref="EnqueuedState"/> tells
    /// Hangfire that a job should be processed by a <see cref="Hangfire.Server.Worker"/>,
    /// and <see cref="FailedState"/> tells Hangfire that a job should be investigated 
    /// by a developer.</para>
    /// 
    /// <para>Each state have some essential properties like <see cref="Name"/>,
    /// <see cref="IsFinal"/> and a custom ones that are exposed through
    /// the <see cref="SerializeData"/> method.</para>
    /// 
    /// 
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
        /// Gets the unique name of the state.
        /// </summary>
        /// <remarks>
        /// <para>Since states determine the current processing pipeline of a 
        /// background job, we should be able to distinguish one state
        /// from another.</para>
        /// 
        /// <para>In Hangfire we are distinguishing one state from another using 
        /// the state name.</para>
        /// 
        /// <note type="implement">
        /// The returning value should be hard-coded, no modifications of
        /// this property should be allowed to a user. Implementors should
        /// not add a public setter on this property.
        /// </note> 
        /// 
        /// Since states are used to determine the processing pipeline,
        /// State names are used to distinguish one state from each other,
        /// not
        /// State names are used to distinguish the state between each other.
        /// Implementors are 
        /// </remarks>
        [NotNull] string Name { get; }

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
        ///         <i>Can not change the state to 'Enqueued': target 
        ///         method was not found</i>
        ///     </item>
        ///     <item><i>Exceeded the maximum number of retry attempts</i></item>
        /// </list>
        /// The reason value is usually not hard-coded in a state implementation,
        /// allowing users to change it when creating an instance of a state.
        /// </remarks>
        [CanBeNull] string Reason { get; }

        /// <summary>
        /// Gets if the current state is a final one.
        /// </summary>
        /// <remarks>
        /// When a background job is moved to a final state, state machine sets
        /// it expiration time to a non-zero value. Final states are considered
        /// as a termination states, in which the background job lifecycle is 
        /// finished.
        /// </remarks>
        bool IsFinal { get; }

        /// <summary>
        /// Объявляет, будет ли выбрасываться исключение при попытке
        /// перевести задачу в данное состояние, если тип или метод
        /// задачи не найден.
        /// </summary>
        /// <remarks>
        /// During a state transition, state machine fetches and de-serializes the
        /// job information, such as type, method info and so on. Some times, due
        /// to different reasons, for example, absent assembly, this process throws
        /// an exception, leading to the inability of deserialize a job.
        /// </remarks>
        bool IgnoreJobLoadException { get; }

        /// <summary>
        /// Получает словарь с сериализованными значениями свойств свойствами состояния.
        /// состояния. Полученные данные используются в мониторинге для предоставления
        /// отладочной информации, а также в некоторых других случаях, например, для
        /// работы токенов отмены. Записанные данные доступны через метод GetStateData.
        /// </summary>
        /// <returns></returns>
        [NotNull] Dictionary<string, string> SerializeData();
    }
}
