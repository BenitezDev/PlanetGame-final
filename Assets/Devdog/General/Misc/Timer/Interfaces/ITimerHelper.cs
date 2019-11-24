using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Devdog.General
{
    public interface ITimerHelper : IDisposable
    {
//        void StopAllTimers();
        void StopTimer(int id);
        int StartTimer(float time, System.Action callbackContinous, System.Action callbackWhenTimeIsOver);
        int StartTimer(float time, System.Action callbackWhenTimeIsOver);
    }
}
