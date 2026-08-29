using System;
using System.ComponentModel;

namespace iReverse_Unisoc_Ultimate.UniFlash.Worker
{
    internal abstract class WorkerBase
    {
        protected BackgroundWorker Worker;

        protected WorkerBase(BackgroundWorker worker)
        {
            Worker = worker;
        }

        protected void ThrowIfCancelled(DoWorkEventArgs e)
        {
            if (Worker.CancellationPending)
            {
                Cleanup();
                e.Cancel = true;
                throw new OperationCanceledException();
            }
        }

        protected abstract void Cleanup();

        protected void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled by user.");
            }
        }

        protected void Execute(Action action, Action finallyAction)
        {
            try
            {
                action();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Operation cancelled by user.");
            }
            finally
            {
                finallyAction?.Invoke();
            }
        }
    }
}
