using System;
using System.Collections;

namespace MixLibrary
{
    public abstract class Coroutine
    {
        IEnumerator enumerator = null;
        bool started = false;
        bool completed = false;

        public void Restart()
        {
            enumerator = null;
            started = false;
            completed = false;
        }

        public virtual void Start()
        {
            started = true;
            enumerator = OnCoroutineUpdate();
        }

        public virtual bool UpdateCoroutine()
        {
            if (completed)
                return false;

            if (started == false)
                Start();

            bool result = Process(enumerator);

            if (!result && !completed)
            {
                completed = true;
                OnCoroutineComplete();
            }

            return result;
        }

        protected abstract IEnumerator OnCoroutineUpdate();
        protected abstract void OnCoroutineComplete();

        public IEnumerator WaitForSeconds(float seconds)
        {
            DateTime startedWaiting = DateTime.Now;

            while ((DateTime.Now - startedWaiting).TotalSeconds < seconds)
            {
                yield return true;
            }
        }

        public IEnumerator WaitForTimeStamp(long milliseconds)
        {
            long startedWaiting = DateTimeUtil.GetTimeStamp();

            while ((DateTimeUtil.GetTimeStamp() - startedWaiting) < milliseconds)
            {
                yield return true;
            }
        }

        private bool Process(IEnumerator enumerator)
        {
            bool result = false;
            if (enumerator != null)
            {
                IEnumerator subEnumerator = enumerator.Current as IEnumerator;
                if (subEnumerator != null)
                {
                    result = Process(subEnumerator);
                    if (!result)
                    {
                        result = enumerator.MoveNext();
                    }
                }
                else
                {
                    result = enumerator.MoveNext();
                }
            }

            return result;
        }
    }
}