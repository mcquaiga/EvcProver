﻿using System;
using System.ComponentModel;
using Caliburn.Micro;
using Action = System.Action;

//taken from http://caliburnmicro.codeplex.com/discussions/391929

namespace Prover.GUI.Common.BackgroundWork
{
    public class BackgroundWork : IResult
    {
        private readonly Action<Exception> _onFail;
        private readonly Action _onSuccess;
        private readonly Action _work;

        public BackgroundWork(Action work) : this(work, () => { }, e => { })
        {
        }

        public BackgroundWork(Action work, Action onSuccess, Action<Exception> onFail)
        {
            _work = work;
            _onSuccess = onSuccess;
            _onFail = onFail;
        }

        #region Implementation of IResult

        public void Execute(CoroutineExecutionContext context)
        {
            Exception error = null;
            var worker = new BackgroundWorker();

            worker.DoWork += (s, e) =>
            {
                try
                {
                    _work();
                }
                catch (Exception ex)
                {
                    error = ex;
                }
            };

            worker.RunWorkerCompleted += (s, e) =>
            {
                if (error == null && _onSuccess != null)
                    _onSuccess.OnUIThread();

                if (error != null && _onFail != null)
                    Caliburn.Micro.Execute.OnUIThread(() => _onFail(error));

                Completed(this, new ResultCompletionEventArgs {Error = error});
            };
            worker.RunWorkerAsync();
        }

        public event EventHandler<ResultCompletionEventArgs> Completed = delegate { };

        #endregion
    }
}