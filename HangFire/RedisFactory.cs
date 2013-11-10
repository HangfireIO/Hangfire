// This file is part of HangFire.
// Copyright © 2013 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HangFire.  If not, see <http://www.gnu.org/licenses/>.

using System;
using ServiceStack.Redis;

namespace HangFire
{
    public static class RedisFactory
    {
        private static readonly Lazy<IRedisClientsManager> _pooledManager;
        private static readonly Lazy<IRedisClientsManager> _basicManager;

        static RedisFactory()
        {
            Host = String.Format("{0}:{1}", RedisNativeClient.DefaultHost, RedisNativeClient.DefaultPort);
            Db = (int) RedisNativeClient.DefaultDb;

            _pooledManager = new Lazy<IRedisClientsManager>(
                () => new PooledRedisClientManager(Db, Host));
            
            _basicManager = new Lazy<IRedisClientsManager>(() => 
                new BasicRedisClientManager(Db, Host));
        }

        /// <summary>
        /// Gets or sets Redis hostname. Default: "localhost:6379".
        /// </summary>
        public static string Host { get; set; }

        /// <summary>
        /// Gets or sets Redis database number. Default: 0.
        /// </summary>
        public static int Db { get; set; }

        public static IRedisClientsManager PooledManager
        {
            get { return _pooledManager.Value; }
        }

        public static IRedisClientsManager BasicManager
        {
            get { return _basicManager.Value; }
        }
    }
}
