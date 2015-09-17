// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MessageQueueExtensions.cs" company="Rolosoft Ltd">
//   © Rolosoft Ltd
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#region License

// Copyright 2013 Rolosoft Ltd
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

namespace Rsft.Lib.Msmq.MessageCounter
{
    #region Usings

    using System.Messaging;
    using System.Runtime.InteropServices;
    using System.Threading;

    #endregion

    /// <summary>
    ///     The message queue extensions.
    /// </summary>
    public static class MessageQueueExtensions
    {
        #region Constants

        /// <summary>
        ///     The propi d_ mgm t_ queu e_ messag e_ count.
        /// </summary>
        private const int PROPID_MGMT_QUEUE_MESSAGE_COUNT = 7;

        /// <summary>
        ///     The v t_ null.
        /// </summary>
        private const byte VT_NULL = 1;

        /// <summary>
        ///     The v t_ u i 4.
        /// </summary>
        private const byte VT_UI4 = 19;

        #endregion

        #region Fields

        /// <summary>
        /// The lock object
        /// </summary>
        private static readonly object LockObject = new object();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The get count.
        /// </summary>
        /// <param name="queue">
        /// The queue.
        /// </param>
        /// <returns>
        /// The <see cref="uint"/>.
        /// </returns>
        public static uint GetCount(this MessageQueue queue)
        {
            return GetCount(queue.Path);
        }

        #endregion

        #region Methods

        /// <summary>
        /// The get count.
        /// </summary>
        /// <param name="path">
        /// The path.
        /// </param>
        /// <returns>
        /// The <see cref="uint"/>.
        /// </returns>
        private static unsafe uint GetCount(string path)
        {
            if (!MessageQueue.Exists(path))
            {
                return 0;
            }

            var props = new MQMGMTPROPS();
            props.cProp = 1;

            var aPropId = PROPID_MGMT_QUEUE_MESSAGE_COUNT;
            props.aPropID = &aPropId;

            var aPropVar = new MQPROPVariant();
            aPropVar.vt = VT_NULL;
            props.aPropVar = &aPropVar;

            var status = 0;
            props.status = &status;

            var objectName = Marshal.StringToBSTR("queue=Direct=OS:" + path);
            try
            {
                uint rtn;

                lock (LockObject)
                {
                    var result = MQMgmtGetInfo(null, (char*) objectName, &props);
                    if (result != 0 || *props.status != 0 || props.aPropVar->vt != VT_UI4)
                    {
                        rtn = 0;
                    }
                    else
                    {
                        rtn = props.aPropVar->ulVal;
                    }
                }

                return rtn;
            }
            finally
            {
                Marshal.FreeBSTR(objectName);
            }
        }

        /// <summary>
        /// The mq mgmt get info.
        /// </summary>
        /// <param name="computerName">
        /// The computer name.
        /// </param>
        /// <param name="objectName">
        /// The object name.
        /// </param>
        /// <param name="mgmtProps">
        /// The mgmt props.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        [DllImport("mqrt.dll")]
        private static extern unsafe int MQMgmtGetInfo(char* computerName, char* objectName, MQMGMTPROPS* mgmtProps);

        #endregion

        /// <summary>
        ///     The mqmgmtprops.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct MQMGMTPROPS
        {
            /// <summary>
            ///     The c prop.
            /// </summary>
            public uint cProp;

            /// <summary>
            ///     The a prop id.
            /// </summary>
            public int* aPropID;

            /// <summary>
            ///     The a prop var.
            /// </summary>
            public MQPROPVariant* aPropVar;

            /// <summary>
            ///     The status.
            /// </summary>
            public int* status;
        }

        /// <summary>
        ///     The mqprop variant.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MQPROPVariant
        {
            /// <summary>
            ///     The vt.
            /// </summary>
            public byte vt; // 0

            /// <summary>
            ///     The spacer.
            /// </summary>
            public readonly byte spacer; // 1

            /// <summary>
            ///     The spacer 2.
            /// </summary>
            public readonly short spacer2; // 2

            /// <summary>
            ///     The spacer 3.
            /// </summary>
            public readonly int spacer3; // 4

            /// <summary>
            ///     The ul val.
            /// </summary>
            public readonly uint ulVal; // 8

            /// <summary>
            ///     The spacer 4.
            /// </summary>
            public readonly int spacer4; // 12
        }
    }
}