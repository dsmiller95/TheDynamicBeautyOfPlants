using Cysharp.Threading.Tasks;
using Dman.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts.ReactiveInputExtensions
{
    public static class ReactiveInputExtensions
    {
        public static IObservable<InputAction.CallbackContext> ObservePerformed(this InputActionReference reference)
        {
            var action = reference.action;
            return Observable.FromEvent<InputAction.CallbackContext>(
                sub => action.performed += sub,
                sub => action.performed -= sub);
        }
        public static IObservable<T> ObservePerformed<T>(this InputActionReference reference) where T : struct
        {
            var action = reference.action;
            return Observable.FromEvent<InputAction.CallbackContext>(
                sub => action.performed += sub,
                sub => action.performed -= sub)
                .Select(context => context.ReadValue<T>());
        }

        struct ValueAtFrame<T>
        {
            public T value;
            public int frame;
            public ValueAtFrame(T val)
            {
                value = val;
                frame = Time.frameCount;
            }
        }

        /// <summary>
        /// Reads input from the action ref, allowing for aggregation of multiple performed events per frame. Will emit exactly 1 event per frame.
        /// </summary>
        /// <remarks>
        /// If more than one event is emitted in a single frame, will use <paramref name="aggregator"/> to reduce to one value.
        /// If no events are emitted but the action is still in <see cref="InputActionPhase.Performed"/>, then will read value directly from input
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="actionRef"></param>
        /// <param name="loopTiming"></param>
        /// <param name="aggregator"></param>
        /// <returns></returns>
        public static IObservable<T> ReadInputSticky<T>(
            this InputActionReference actionRef,
            PlayerLoopTiming loopTiming,
            Func<List<T>, T> aggregator)
            where T : struct
        {
            return new StickyInputObservable<T>(actionRef, loopTiming, aggregator);
        }

        private class StickyInputObservable<T> : IObservable<T> where T : struct
        {
            InputAction action;
            PlayerLoopTiming sampleFrequency;
            Func<List<T>, T> aggregator;
            public StickyInputObservable(
                InputActionReference actionReference,
                PlayerLoopTiming sampleFrequency,
                Func<List<T>, T> aggregator)
            {
                this.action = actionReference.action;
                this.sampleFrequency = sampleFrequency;
                this.aggregator = aggregator;
            }

            private async UniTask SubscribeAsyncPoll(
                IObserver<T> observer,
                PlayerLoopTiming loopTiming,
                CancellationToken cancel)
            {
                var aggregatedPerformed = new List<T>(6);
                void QueueItem(InputAction.CallbackContext item)
                {
                    aggregatedPerformed.Add(item.ReadValue<T>());
                }
                try
                {
                    action.performed += QueueItem;
                    while (true)
                    {
                        await UniTask.NextFrame(loopTiming, cancel);
                        if (aggregatedPerformed.Count > 0)
                        {
                            observer.OnNext(aggregatedPerformed.Count == 1 ? aggregatedPerformed[0] : aggregator(aggregatedPerformed));
                            aggregatedPerformed.Clear();
                            continue;
                        }
                        //Debug.Log($"Action phase: {action.phase,20}");
                        if (action.inProgress)
                        {
                            observer.OnNext(action.ReadValue<T>());
                            continue;
                        }
                    }
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    observer.OnError(e);
                }
                finally
                {
                    action.performed -= QueueItem;
                }
                observer.OnCompleted();
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                var cancellationSource = new CancellationTokenSource();
                SubscribeAsyncPoll(
                    observer,
                    sampleFrequency,
                    cancellationSource.Token).Forget();
                return new DisposableAbuse.LambdaDispose(() => {
                    cancellationSource.Cancel();
                    cancellationSource.Dispose();
                });
            }
        }
    }
}
