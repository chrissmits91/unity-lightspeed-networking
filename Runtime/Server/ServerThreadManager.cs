using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lightspeed.Server
{
    public class ServerThreadManager: MonoBehaviour
    {
        private static readonly List<Action> ExecuteOnMainThreadActions = new List<Action>();
        private static readonly List<Action> ExecuteCopiedOnMainThreadActions = new List<Action>();
        private static bool actionToExecuteOnMainThread;

        private void FixedUpdate()
        {
            UpdateMain();
        }

        /// <summary>Sets an action to be executed on the main thread.</summary>
        /// <param name="_action">The action to be executed on the main thread.</param>
        public static void ExecuteOnMainThread(Action _action)
        {
            if (_action == null)
            {
                Debug.Log("No action to execute on main thread!");
                return;
            }

            lock (ExecuteOnMainThreadActions)
            {
                ExecuteOnMainThreadActions.Add(_action);
                actionToExecuteOnMainThread = true;
            }
        }

        /// <summary>Executes all code meant to run on the main thread. NOTE: Call this ONLY from the main thread.</summary>
        private static void UpdateMain()
        {
            if (!actionToExecuteOnMainThread) return;
            ExecuteCopiedOnMainThreadActions.Clear();
            lock (ExecuteOnMainThreadActions)
            {
                ExecuteCopiedOnMainThreadActions.AddRange(ExecuteOnMainThreadActions);
                ExecuteOnMainThreadActions.Clear();
                actionToExecuteOnMainThread = false;
            }

            foreach (var _t in ExecuteCopiedOnMainThreadActions)
            {
                _t();
            }
        }
    }
}
