using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace WindowsInstallerLib
{
    /// <summary>
    /// This class handles the priority, the state, etcetera of threads.
    /// </summary>
    internal sealed class ThreadManager
    {
        /// <summary>
        /// Gets the current priority of the thread.
        /// </summary>
        /// <param name="thread"></param>
        /// <returns>
        /// BelowNormal, Normal, AboveNormal or Highest
        /// </returns>
        private static ThreadPriority GetThreadPriority(Thread thread) { return thread.Priority; }
        private static ThreadPriority SetThreadPriority(Thread thread, ThreadPriority threadPriority) { return thread.Priority = threadPriority;  }
        private static ThreadState GetThreadState(Thread thread) { return thread.ThreadState; }
        private static ApartmentState GetThreadApartmentState(Thread thread) { return thread.GetApartmentState(); }
        private static bool TrySetThreadApartmentState(Thread thread, ApartmentState apartmentState)
        {
            try
            {
                return thread.TrySetApartmentState(apartmentState);
            }
            catch (PlatformNotSupportedException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (ThreadStateException)
            {
                throw;
            }
        }
        private static Thread? GetCurrentThread() {  return Thread.CurrentThread; }
        internal static bool IsBackground(Thread thread) { return thread.IsBackground; }
        internal static bool IsThreadAlive(Thread thread) { return thread.IsAlive; }

        internal static Thread CreateThread(Action action)
        {
            Thread thread = new(() =>
                {
                    action.Invoke();
                }
            );

            if (GetThreadState(thread) == ThreadState.Running)
            {
                //throw new InvalidOperationException($"Cannot modify the state of a thread when it is already running.");
                thread.Join();
            }

            try
            {
                thread.IsBackground = true;
            }
            catch (ThreadStateException ex)
            {
                if (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            }
            catch
            {
                throw;
            }

            try
            {
                if (GetThreadPriority(thread) == ThreadPriority.Normal)
                {
                    SetThreadPriority(thread, ThreadPriority.AboveNormal);
                }
            }
            catch
            {
                throw;
            }

            try
            {
                TrySetThreadApartmentState(thread, ApartmentState.MTA);
            }
            catch
            {
                throw;
            }

            try
            {
                if (GetCurrentThread != null &&
                    GetThreadState(thread) == ThreadState.Unstarted)
                {
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                switch (ex.InnerException != null)
                {
                    case true:
                        throw ex.InnerException;
                    case false:
                        throw;
                }
            }
            return thread;
        }
    }
}
